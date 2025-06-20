using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Havok.Common.Serialize.Util;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;


[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public partial class HideMapInCombat(IFramework Framework, IClientState ClientState) : ITweak
{
    public string DisplayName => "Hide map in combat";
    public TweakStatus Status { get; set; }

    private bool StoredState { get; set; }
    private bool InCombat { get; set; }

    public void OnInitialize()
    {
        LoadConfig();
    }

    public void Dispose()
    {
    }


    public void OnEnable()
    {
        Framework.Update += OnFrameworkUpdate;
    }

    private unsafe bool IsInFate()
    {
        var fm = FateManager.Instance();
        return fm != null && fm->GetCurrentFateId() != 0;
    }

    private unsafe bool IsInDynamicEvent()
    {
        var ef = EventFramework.Instance();
        if (ef is null) return false;
        var pcd = ef->GetPublicContentDirector();
        if (pcd is null) return false;
        var de = pcd->DynamicEvents;
        return de != null && de->CurrentEventId != 0;
    }

    private bool ShouldHide()
    {
        return (ClientState.LocalPlayer?.StatusFlags & StatusFlags.InCombat) != 0 && (IsInFate() || IsInDynamicEvent());
    }
    private unsafe void OnFrameworkUpdate(IFramework _)
    {
        var shouldHide = ShouldHide();
        if (shouldHide == InCombat) return;
        InCombat = shouldHide;
        var mapAgent = AgentMap.Instance();
        if (mapAgent == null) return;

        if (InCombat)
        {
            StoredState = mapAgent->IsAddonShown();
            if (StoredState)
                mapAgent->HideAddon();
            return;
        }

        // it's only ever Hidden if we did it
        if (StoredState && mapAgent->IsAddonHidden())
            mapAgent->ShowAddon();

        StoredState = false;
    }

    public void OnDisable()
    {
        Framework.Update -= OnFrameworkUpdate;
    }
}
