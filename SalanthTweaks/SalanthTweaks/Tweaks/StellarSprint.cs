using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using Serilog;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class StellarSprint : ITweak
{
    public string DisplayName => "StellarSprint";
    public TweakStatus Status { get; set; }

    public void OnInitialize() { }

    public void Dispose()
    {
        usableHook?.Dispose();
        executeSlotHook?.Dispose();
    }
    
    [AutoHook]
    [Signature("E8 ?? ?? ?? ?? 88 47 ?? EB ?? 80 BB", DetourName = nameof(IsUsableDetour))]
    private Hook<RaptureHotbarModule.HotbarSlot.Delegates.IsSlotUsable> usableHook;
    
    [AutoHook]
    [Signature("E9 ?? ?? ?? ?? 73 25", DetourName = nameof(ExecuteSlotDetour))]
    private Hook<RaptureHotbarModule.Delegates.ExecuteSlot> executeSlotHook;


    public void OnEnable()
    {
        
    }

    private unsafe bool IsUsableDetour(
        RaptureHotbarModule.HotbarSlot* thisPtr, RaptureHotbarModule.HotbarSlotType slotType, uint actionId)
    {
        if (slotType == RaptureHotbarModule.HotbarSlotType.Action && actionId == 43357 && hasStellarSprint()) return false;
        return usableHook.Original.Invoke(thisPtr, slotType, actionId);
    }

    private unsafe bool hasStellarSprint()
    {
        var chara = Control.GetLocalPlayer();
        if (chara == null) return false;


        foreach (var s in chara->StatusManager.Status)
            if (s.StatusId == 4398) return true;

        return false;
    }

    private unsafe byte ExecuteSlotDetour( RaptureHotbarModule* thisPtr, RaptureHotbarModule.HotbarSlot* hotbarSlot)
    {
        if (hotbarSlot->ApparentSlotType == RaptureHotbarModule.HotbarSlotType.Action && hotbarSlot->ApparentActionId == 43357 && hasStellarSprint()) return 0;
        return executeSlotHook.Original.Invoke(thisPtr, hotbarSlot);
        
    }
    

    public void OnDisable() { }
}

