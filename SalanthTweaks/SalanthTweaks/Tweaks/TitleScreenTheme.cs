using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class TitleScreenTheme : ITweak
{
    public string DisplayName => "TitleScreenTheme";

    public string Description =>
        "Overrides the theme on the title screen so that the context menus/etc load in ClearBlue";

    public TweakStatus Status { get; set; }

    [Signature("40 57 48 83 EC 20 83 79 70 00")]
    private readonly unsafe delegate* unmanaged<AtkUIColorHolder*, byte, void> colorHolderRegen = null;

    private Hook<AddonContextMenu.Delegates.Initialize> ctxInitHook;

    private unsafe void InitializeDetour(AddonContextMenu* thisPtr)
    {
        if (RaptureAtkModule.Instance()->CurrentUISceneString != "LobbyMain")
        {
            Service.Get<IPluginLog>().Info("InitDetour {0}", RaptureAtkModule.Instance()->CurrentUISceneString);
            ctxInitHook.Original(thisPtr);
            return;
        }

        var uch = AtkStage.Instance()->AtkUIColorHolder;
        if (uch == null)
        {
            ctxInitHook.Original(thisPtr);
            return;
        }

        if (uch->ActiveColorThemeType != 3)
        {
            uch->ActiveColorThemeType = 3;
            colorHolderRegen(uch, 3);
        }

        ctxInitHook.Original(thisPtr);
    }

    public unsafe void OnInitialize()
    {
        ctxInitHook = Service.Get<IGameInteropProvider>()
                             .HookFromAddress<AddonContextMenu.Delegates.Initialize>(
                                 AddonContextMenu.StaticVirtualTablePointer->Initialize, InitializeDetour);
    }

    public void Dispose() { }

    public void OnEnable()
    {
        ctxInitHook.Enable();
    }

    public void OnDisable()
    {
        ctxInitHook.Disable();
        ctxInitHook.Dispose();
    }
}
