using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.ReadOnly;
using Newtonsoft.Json;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using SalanthTweaks.Services;
using ClassJob = Lumina.Excel.Sheets.ClassJob;
// using WKSMissionReward = SalanthTweaks.CustomSheets.WKSMissionReward;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class CosmicDatasetProgress(IDataManager dataManager) : ITweak
{
    public string DisplayName => "CosmicDatasetProgress";
    public TweakStatus Status { get; set; }

    public void OnInitialize() { }

    public void Dispose()
    {
        setIntDataHook?.Dispose();
        AgentWKSMissionReceiveEventHook.Dispose();
    }

    private const int JobCount = 11;
    private const int TypeCount = 6;

    private readonly Progress[,] ResearchProgress = new Progress[JobCount, TypeCount];

    public List<Progress> GetCurrentJobProgress()
    {
        lock (ResearchProgress)
        {
            var p = new List<Progress>();
            var jobId = Service.Get<IObjectTable>().LocalPlayer?.ClassJob.RowId ?? 0;
            // 8-19 is the id range for DoH/DoL
            if (jobId is < 8 or > 19) return p;
            jobId -= 8;
            for (var typ = 0; typ < TypeCount; typ++)
                p.Add(ResearchProgress[jobId, typ]);
            return p;
        }
    }


    public void OnEnable()
    {
        InitResearchProgress();
        needUpdate = true;
        Update();
        var fw = Service.Get<IFramework>();
        fw.Update += OnTick;
    }

    [AutoHook]
    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 45 32 D2 48 8B 49 28 ",
               DetourName = nameof(AgentWKSMissionReceiveEventDetour))]
    private Hook<AgentWKSMission.Delegates.ReceiveEvent> AgentWKSMissionReceiveEventHook = null!;

    private unsafe AtkValue* AgentWKSMissionReceiveEventDetour(
        AgentWKSMission* thisPtr, AtkValue* returnValue, AtkValue* values, uint valueCount, ulong eventKind)
    {
        try
        {
            var sb = new StringBuilder();
            sb.Append($"AgentWKSMissionReceiveEvent: {eventKind:X} @{valueCount}:\n");
            var valueSpan = new Span<AtkValue>(values, (int)valueCount);
            foreach (var value in valueSpan)
            {
                sb.Append($"{value.ToString()}\n");
            }

            Service.Get<IPluginLog>().Info(sb.ToString());
        }
        catch (Exception e)
        {
            Service.Get<IPluginLog>().Error(e.ToString());
        }

        return AgentWKSMissionReceiveEventHook.Original.Invoke(thisPtr, returnValue, values, valueCount, eventKind);
    }

    public void OnDisable()
    {
        Service.Get<IFramework>().Update -= OnTick;
        AgentWKSMissionReceiveEventHook?.Disable();
    }

    public void OnTick(IFramework framework)
    {
        if (needUpdate) Update();
        CheckKeypress();
    }


    private unsafe void CheckKeypress()
    {
        var ks = Service.Get<IKeyState>();
        if (!(ks[VirtualKey.CONTROL] && ks[VirtualKey.SHIFT] && ks[VirtualKey.M])) return;
        ks[VirtualKey.M] = false;


        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager == null) return;
        var hud = unitManager->GetAddonByName("WKSHud");
        if (hud == null) return;

        var buttonNode = (AtkComponentNode*)hud->GetNodeById(7);
        if (buttonNode == null) return;
        var registeredEvent = buttonNode->AtkEventManager.Event;
        if (registeredEvent == null) return;
        for (; registeredEvent != null; registeredEvent = registeredEvent->NextEvent)
        {
            if (registeredEvent->State.EventType == AtkEventType.ButtonClick) break;
        }

        if (registeredEvent == null) return;
        hud->ReceiveEvent(registeredEvent->State.EventType, (int)registeredEvent->Param, registeredEvent);
    }

    private void InitResearchProgress()
    {
        var toolSheet = Service.Get<IDataManager>().Excel.GetSheet<WKSCosmoToolClass>();
        lock (ResearchProgress)
        {
            for (uint job = 0; job < JobCount; job++)
            {
                var r = toolSheet.GetRow(job + 1);
                for (uint typ = 0; typ < TypeCount; typ++)
                    ResearchProgress[job, typ].IconId = r.Types[(int)typ].Icon;
            }
        }
    }

    private unsafe void Update()
    {
        var mgr = WKSManager.Instance();
        if (mgr == null) return;
        var research = mgr->ResearchModule;
        if (research == null) return;
        needUpdate = false;
 
        lock (ResearchProgress)
        {
            for (var job = 0; job < JobCount; job++)
            for (var typ = 0; typ < TypeCount; typ++)
            {
                ref var s = ref ResearchProgress[job, typ];
                var toolClass = (byte)(job + 1);
                var resType = (byte)(typ + 1);
                s.Show = research->IsTypeAvailable(toolClass, resType);
                s.Current = research->GetCurrentAnalysis(toolClass, resType);
                s.Needed = research->GetNeededAnalysis(toolClass, resType);
                s.Max = research->GetMaxAnalysis(toolClass, resType);
            }
        }
    }


    [AutoHook]
    [Signature("40 53 48 83 EC 20 48 8B D9 81 EA ?? ?? ?? ??", DetourName = nameof(ResearchModuleSetIntDataDetour))]
    private Hook<WKSResearchModule.Delegates.SetIntData> setIntDataHook = null!;

    private volatile bool needUpdate;

    public unsafe bool ResearchModuleSetIntDataDetour(
        WKSResearchModule* thisPtr, int a2, int a3, int a4, int a5, int a6, int a7)
    {
        needUpdate = true;
        return setIntDataHook.Original.Invoke(thisPtr, a2, a3, a4, a5, a6, a7);
    }

    public struct Progress
    {
        public bool Show;
        public uint IconId;

        public ushort Current;
        public ushort Needed;
        public ushort Max;

        public float Percentage => (float)Current / Needed;

        public string Overlay => $"{Current} / {Needed} [{Max}]";

        public bool Complete => Current >= Needed;
        public bool Capped => Current >= Max;
        public bool NearCap => Complete && Percentage >= 0.9f;

        public void Draw()
        {
            if (!Show) return;
            ImGui.Image(Service.Get<ITextureProvider>().GetFromGameIcon(IconId).GetWrapOrEmpty().Handle,
                        new Vector2(32f, 32f));
            ImGui.SameLine();
            using var barColor = ImRaii.PushColor(ImGuiCol.PlotHistogram,
                                                  Dalamud.Interface.Colors.ImGuiColors.HealerGreen, Complete);
            using var textColor = ImRaii.PushColor(ImGuiCol.Text,
                                                   Dalamud.Interface.Colors.ImGuiColors.DalamudRed, NearCap);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
            ImGui.ProgressBar(Percentage, new Vector2(-1, 24f), Overlay);
        }
    }

    // ReSharper disable once InconsistentNaming
    [AddonPreRefresh("WKSMission")]
    public unsafe void WKSMissionPreRefresh(AddonEvent ev, AddonArgs bargs)
    {
        var args = (AddonRefreshArgs)bargs;
        var atkValues = new Span<AtkValue>(args.AtkValues.ToPointer(), (int)args.AtkValueCount);
        
        if (atkValues[0].Int != 1) return;
        var itemCount = atkValues[32];
        const int itemSize = 6;
        const int itemArrayBase = 33;
        const int labelArrayBase = 801;
        var currentJobProgress = GetCurrentJobProgress();

        uint maxValue = 0;
        HashSet<int> indexes = [];
        HashSet<int> okayIndexes = [];
        for (var i = 0; i < itemCount.UInt; i++)
        {
            var questId = atkValues[itemArrayBase + (itemSize * i) + 2].UInt;
            if (questId == 0) continue;
            var value = ValueForMission(questId, currentJobProgress);
            if (value == 0) continue;

            okayIndexes.Add(i);

            if (value > maxValue)
            {
                maxValue = value;
                indexes.Clear();
            }

            if (value == maxValue) indexes.Add(i);
        }

        foreach (var idx in indexes)
        {
            ref var labelValue = ref atkValues[labelArrayBase + (idx * 2)];
            var str = new SeStringBuilder().PushEdgeColorType(65)
                                       .Append(labelValue.String)
                                       .PopEdgeColorType().GetViewAsSpan();
            labelValue.SetManagedString(str);
            okayIndexes.Remove(idx);
        }

        foreach (var idx in okayIndexes)
        {
            ref var labelValue = ref atkValues[labelArrayBase + (idx * 2)];
            var str = new SeStringBuilder().PushEdgeColorType(34)
                                           .Append(labelValue.String)
                                           .PopEdgeColorType().GetViewAsSpan();
            labelValue.SetManagedString(str);
        }
    }

    public uint ValueForMission(uint missionId, List<Progress> currentJobProgress)
    {
        var missionUnit = dataManager.Excel.GetSheet<WKSMissionUnit>().GetRow(missionId);
        var rewardRowId = missionUnit.MissionReward.RowId;
        var mission = dataManager.Excel.GetSheet<WKSMissionReward>().GetRow(rewardRowId);
        uint value = 0;
        for (var i = 0; i < 3; i++)
        {
            if (mission.TypeIndex[i] == 0) continue;
            var prog = currentJobProgress[mission.TypeIndex[i] - 1];
            if (prog.Complete) continue;
            value += mission.ResearchReward[i];
        }

        return value;
    }
}
