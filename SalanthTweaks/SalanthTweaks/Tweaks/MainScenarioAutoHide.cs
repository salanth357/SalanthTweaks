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
public class MainScenarioAutoHide : ITweak
{

    public string DisplayName => "Main Scenario Auto Hide";
    public TweakStatus Status { get; set; }
    public void OnInitialize() { }

    public void OnEnable()
    {
        UpdateAddon();
        Redraw();
    }

    public void OnDisable()
    {
        UpdateAddon();
        Redraw();
    }

    public void Dispose() { }

    private bool display;

    private unsafe void UpdateAddon() => UpdateAddon((AtkUnitBase*)Service.Get<IGameGui>().GetAddonByName("ScenarioTree"));

    [AddonPostRefresh("ScenarioTree")]
    public unsafe void UpdateAddon(AddonEvent evt, AddonArgs args) => UpdateAddon((AtkUnitBase*)args.Addon);

    public unsafe void UpdateAddon(AtkUnitBase* addon)
    {
        if (addon == null) return;

        var ast = AgentScenarioTree.Instance();
        var jobQuestNode = addon->GetNodeById(7);

        display = (ast != null && ast->Data != null && ast->Data->CurrentScenarioQuest != 0) ||
                  (jobQuestNode != null && jobQuestNode->IsVisible());
    }
    
    public unsafe void Redraw() => Redraw((AtkUnitBase*)Service.Get<IGameGui>().GetAddonByName("ScenarioTree"));

    [AddonPreDraw("ScenarioTree")]
    public unsafe void Redraw(AddonEvent evt, AddonArgs args) => Redraw((AtkUnitBase*)args.Addon);

    public unsafe void Redraw(AtkUnitBase* addon)
    {
        if (addon != null) addon->IsVisible = display;
    }
}
