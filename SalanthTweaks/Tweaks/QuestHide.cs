using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class QuestHide : ITweak
{
    public string DisplayName => "QuestHide";
    public TweakStatus Status { get; set; }

    public void OnInitialize() { }

    public void Dispose() { }

    public void OnEnable() { }

    public void OnDisable() { }

    [AddonPreSetup("AreaMap")]
    public void OnMapPreSetup(AddonEvent ev, AddonArgs bargs)
    {
        Service.Get<IPluginLog>().Info("map mod");
        ModArgs(true);

    }

    [AddonPreRequestedUpdate("AreaMap")]
    public void OnAreaMapPreRequestedUpdate(
        AddonEvent ev, AddonArgs bargs)
    {
        ModArgs();
    }

    public unsafe void ModArgs(bool force = false)
    {
            var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.AreaMap2);
            var extendArray = AtkStage.Instance()->GetExtendArrayData(ExtendArrayType.AreaMapSubZones);
            
            // Check if should force change

            var total = numberArray->IntArray[31];

            for (var i = 0; i < total; i++)
            {
                if (i >= extendArray->Size) return;
                var extendItem = (MapMarkerBase*)extendArray->DataArray[i];
                if (extendItem == null) continue;
                if (extendItem->IconId == 71021)    
                {
                    extendItem->IconId = 0;
                    extendItem->IconFlags = 0;
                }
            }
    }
} 

