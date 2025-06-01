using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Logging;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using SalanthTweaks.Structs;
using ClassJob = SalanthTweaks.Enums.ClassJob;
using Role = SalanthTweaks.Enums.Role;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class MainScenarioRoleQuests(ILogger<MainScenarioRoleQuests> Log) : ITweak
{

    private record QuestDetail
    {
        internal ushort Id { get; init; }
        internal ushort RequiredLevel { get; init; }

        internal QuestDetail(Quest q)
        {
            Id = (ushort)(q.RowId & 0xFFFF);
            RequiredLevel = q.ClassJobLevel.FirstOrDefault();
        }
    }

    private unsafe delegate byte CalculateQuestsDelegate(AgentScenarioTree* thisPtr);

    [AutoHook]
    [Signature("40 56 41 55 41 57 48 83 EC 30 4C 8B 69 28", DetourName = nameof(CalculateQuestsDetour))]
    private Hook<CalculateQuestsDelegate>? calculateQuestHook = null;

    public void Dispose()
    {
        calculateQuestHook?.Dispose();
        GC.SuppressFinalize(this);
    }

    public string DisplayName => "Main Scenario UI Show Role Quests";
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    { }
    
    public void OnEnable() { }

    public void OnDisable() { }

    private unsafe byte CalculateQuestsDetour(AgentScenarioTree* thisPtr)
    {
        var data = (AgentScenarioTreeDataExtended*)thisPtr->Data;
        var ret = calculateQuestHook!.Original(thisPtr);

        // treetips
        // 2- GC unlocks
        // 3- ARR primals
        // 4- Crystal Tower (ARR lock)
        // 5- Gold Saucer 
        // 6- Crystal Tower (ShB)
        // 7- classjob
        // 8- my feisty little chocobo
        // 9- starting quests

        try
        {
            if (data->TreeTipRowID == 7 && data->CurrentJobQuest == 0)
                data->CurrentJobQuest = LookupRoleQuest();
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error resolving role quest");
        }

        return ret;
    }

    private unsafe ushort LookupRoleQuest()
    {
        var lvl = PlayerState.Instance()->CurrentLevel;
        var jobId = (ClassJob)PlayerState.Instance()->CurrentClassJobId;
        if (jobId == ClassJob.BlueMage) return 0;
        Log.LogInformation("JobId {jobId}", jobId);
        var role = jobId.GetRole();

        foreach (var q in roleQuests.Value.GetValueOrDefault(role, []))
        {
            if (q.RequiredLevel <= lvl && !QuestManager.IsQuestComplete(q.Id))
                return q.Id;
        }

        return 0;
    }

    private readonly Lazy<Dictionary<Role, List<QuestDetail>>> roleQuests = new(() =>
    {
        var questSheet = Service.Get<IDataManager>().GetExcelSheet<Quest>();
        return RoleQuestIds.Select(pair =>
                                       new KeyValuePair<Role, List<QuestDetail>>(
                                           pair.Key,
                                           pair.Value.Select(u => new QuestDetail(questSheet.GetRow(u))).ToList())
        ).ToDictionary();
    });

    private static readonly Dictionary<Role, List<uint>> RoleQuestIds = new()
    {
        [Role.Tank] =
        [
            68779, 68780, 68781, 68782, 68783, 68784, 69638, 69639, 69640, 69641, 69642, 69643, 70354, 70355, 70356,
            70357, 70358, 70359
        ],
        [Role.Healer] =
        [
            68803, 68804, 68805, 68806, 68807, 68808, 69644, 69645, 69646, 69647, 69648, 69649, 70360, 70361, 70362,
            70363, 70364, 70365
        ],
        [Role.MeleeDps] =
        [
            68809, 68810, 68811, 68812, 68813, 68814, 69650, 69651, 69652, 69653, 69654, 69655, 70366, 70367, 70368,
            70369, 70370, 70371
        ],
        [Role.RangedPhysicalDps] =
        [
            68809, 68810, 68811, 68812, 68813, 68814, 69656, 69657, 69658, 69659, 69660, 69661, 70372, 70373, 70374,
            70375, 70376, 70377
        ],
        [Role.MagicDps] =
        [
            69159, 69160, 69161, 69162, 69163, 69164, 69662, 69663, 69664, 69665, 69666, 69667, 70378, 70379, 70380,
            70381, 70382, 70383
        ]
    };

    [AddonPreRefresh("ScenarioTree")]
    private void ScenarioTreePreRefresh(AddonEvent type, AddonArgs baseArgs)
    {
        var args = (AddonRefreshArgs)baseArgs;
        Log.LogInformation("prerefresh {valCount}", args.AtkValueCount);
        foreach (var atkValue in args.AtkValueSpan)
        {
            Log.LogInformation("{x}", atkValue.ToString());
        }
    }
}
