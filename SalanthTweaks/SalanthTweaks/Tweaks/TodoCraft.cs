using System.Linq;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using ClassJob = Lumina.Excel.Sheets.ClassJob;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class TodoCraft : ITweak
{
    public string DisplayName => "TodoCraft";
    public TweakStatus Status { get; set; }

    public void OnInitialize() { }

    public void Dispose()
    {
        
        receiveEventHook?.Dispose();
    }


    private Hook<AddonToDoList.Delegates.ReceiveEvent> receiveEventHook;
    public unsafe void OnEnable()
    {
        var atl = RaptureAtkUnitManager.Instance()->GetAddonByName("_ToDoList");
        if (atl == null) return;
        receiveEventHook = Service.Get<IGameInteropProvider>().HookFromAddress<AddonToDoList.Delegates.ReceiveEvent>((nint)atl->VirtualTable->ReceiveEvent, ReceiveEventDetour);
        receiveEventHook?.Enable();
    }

    public void OnDisable()
    {
        receiveEventHook?.Disable();
        receiveEventHook?.Dispose();
    }

    private unsafe void ReceiveEventDetour(AddonToDoList* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        if (!HandleEvent(thisPtr, eventType, eventParam, atkEvent, atkEventData))
            receiveEventHook.Original(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

    private unsafe bool HandleEvent(
        AddonToDoList* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        var l = Service.Get<IPluginLog>();
        if (eventType != AtkEventType.MouseUp || eventParam != 1 || atkEventData->MouseData.ButtonId == 1)
            return false;

        // Figure out which quest entry this is
        var targetNode = (AtkResNode*)atkEvent->Target;
        if (targetNode == null || targetNode->ParentNode == null)
        {
            return false;
        }

        var parentId = targetNode->ParentNode->NodeId;
        if (parentId <= 70000)
        {
            return false;
        }
        if (atkEvent->Node == null)
        {
            return false;
        }

        
        var param = *(uint*)atkEvent->Node;
        var paramType = param >> 24;
        if (paramType != 1) return false;

        var excel = Service.Get<IDataManager>().Excel;

        var itemID = param & 0xFFFFFF;
        if (!excel.GetSheet<Item>().TryGetRow((uint)itemID, out var item)) return false;


        var classJob = excel.GetSheet<ClassJob>().GetRow(PlayerState.Instance()->CurrentClassJobId);
        if (classJob.DohDolJobIndex < 0) return false;
        

        if (!Service.Get<IDataManager>().Excel.GetSheet<Recipe>().TryGetFirst(r => r.ItemResult.RowId == itemID && r.CraftType.RowId == classJob.DohDolJobIndex, out var recipe)) return false;

        AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipe.RowId);
        
        return true;
    }
}

