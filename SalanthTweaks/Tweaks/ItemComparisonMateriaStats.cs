using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.Exd;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.ReadOnly;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class ItemComparisonMateriaStats : ITweak
{
    public string DisplayName => "ItemComparisonMateriaStats";
    public TweakStatus Status { get; set; }

    public void OnInitialize() { }

    public void Dispose()
    {
        ReceiveEventHook?.Dispose();
    }

    public void OnEnable() { }

    public void OnDisable() { }

    [AddonPreRefresh("ItemDetailCompare")]
    public unsafe void OnItemDetailComparePreRefresh(
        AddonEvent ev, AddonArgs args)
    {
        var sargs = (AddonRefreshArgs)args;
        var avSpan = new Span<AtkValue>((void*)sargs.AtkValues, (int)sargs.AtkValueCount);
        UpdateValues(avSpan);
    }

    [AddonPreSetup("ItemDetailCompare")]
    public void OnItemDetailComparePreSetup(AddonEvent ev, AddonArgs args)
    {
        UpdateValues(args.AtkValues());
    }


    [AutoHook]
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 41 8B D8 ", DetourName = nameof(ReceiveEventDetour))]
    private Hook<AtkEventListener.Delegates.ReceiveEvent> ReceiveEventHook = null;


    public unsafe void ReceiveEventDetour(
        AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent,
        AtkEventData* atkEventData)
    {
        if (eventType == (AtkEventType)74)
        {
            if (atkEventData != null)
                Dalamud.Utility.Util.DumpMemory((IntPtr)atkEventData, 0x40);
        }

        ReceiveEventHook.Original.Invoke(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

    public unsafe void UpdateValues(Span<AtkValue> avSpan)
    {
        const int blockSize = 86;
        int[] blockStartIndexes = [0, 86, 172, 258];

        var agent = AgentItemComp.Instance();
        if (agent->ItemData == null) return;

        var excel = Service.Get<IDataManager>().Excel;
        var itemSheet = excel.GetSheet<Item>();
        var materiaSheet = excel.GetSheet<Materia>();


        for (var idx = 0; idx < agent->ItemData->Items.Length; idx++)
        {
            SortedDictionary<uint, bool> baseParams = [];
            Dictionary<uint, ReadOnlySeString> paramNames = [];
            Dictionary<uint, uint> paramValues = [];
            Dictionary<uint, uint> paramLimits = [];
            Dictionary<uint, uint> materiaValues = [];

            ref var item = ref agent->ItemData->Items[idx];
            if (item.Item.IsEmpty()) continue;

            var itemRow = itemSheet.GetRow(item.Item.GetBaseItemId());
            var ilvlRow = itemRow.LevelItem.Value;

            var nativeItemRow = ExdModule.GetItemRowById(item.Item.ItemId);
            foreach (var rbp in itemRow.BaseParam)
            {
                if (!rbp.IsValid || rbp.RowId == 0) continue;
                var bp = rbp.Value;

                baseParams.Add(bp.RowId, true);
                paramNames[bp.RowId] = bp.Name;

                paramLimits[bp.RowId] = InventoryItem.GetParameterMaxValue(bp.RowId, nativeItemRow);
                fixed (InventoryItem* ip = &item.Item)
                {
                    paramValues[bp.RowId] =
                        InventoryItem.GetParameterValue(bp.RowId, ip, false, true, false, false);
                }
            }

            for (byte materiaIndex = 0; materiaIndex < item.Item.GetMateriaCount(); materiaIndex++)
            {
                var materiaId = item.Item.GetMateriaId(materiaIndex);
                var materiaGrade = item.Item.GetMateriaGrade(materiaIndex);

                if (materiaId == 0 || materiaGrade == 0) continue;

                var materia = materiaSheet.GetRow(materiaId);
                var bp = materia.BaseParam.Value;
                if (baseParams.TryAdd(bp.RowId, true))
                {
                    // TODO: Consider modifying this for UI clarity
                    paramNames[bp.RowId] = bp.Name;
                    paramLimits[bp.RowId] = InventoryItem.GetParameterMaxValue(bp.RowId, nativeItemRow);
                    paramValues[bp.RowId] = 0;
                }

                if (materiaGrade < materia.Value.Count)
                {
                    materiaValues.TryAdd(bp.RowId, 0);
                    materiaValues[bp.RowId] += (uint)materia.Value[materiaGrade];
                }
            }

            var blockSpan = avSpan.Slice(blockStartIndexes[idx], blockSize);

            var firstBlockCount = uint.Min((uint)baseParams.Count, 8);
            var secondBlockCount = (uint)int.Max(baseParams.Count - 8, 0);
            secondBlockCount = uint.Min(secondBlockCount, 8);

            blockSpan[8].SetUInt(firstBlockCount);
            blockSpan[9].SetUInt(secondBlockCount);


            var keys = baseParams.Keys.ToArray();
            for (var bonusIdx = 0; bonusIdx < int.Min(16, keys.Length); bonusIdx++)
            {
                var bpId = keys[bonusIdx];
                var sb = new SeStringBuilder();
                sb.Append(paramNames[bpId]);
                sb.Append($" +{paramValues[bpId]}");
                if (materiaValues.TryGetValue(bpId, out var matValue))
                {
                    sb.Append(" [");
                    var total = paramValues[bpId] + matValue;
                    sb.PushColorType((uint)(total > paramLimits[bpId] ? 14 : 500));
                    sb.Append($"{uint.Min(total, paramLimits[bpId])}");
                    sb.PopColorType();
                    sb.Append("]");
                }

                blockSpan[40 + bonusIdx].SetManagedString(sb.GetViewAsSpan());
            }
        }
    }
}
