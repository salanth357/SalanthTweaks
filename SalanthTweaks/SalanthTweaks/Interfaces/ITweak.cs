using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using SalanthTweaks.Enums;

namespace SalanthTweaks.Interfaces;

public interface ITweak : IDisposable
{
    public string InternalName => GetType().Name;
    public string DisplayName { get; }


    public string Description => "";
    TweakStatus Status { get; set; }
    void OnInitialize();
    void OnEnable();
    void OnDisable();

    void DrawConfig()
    {
        ImGuiHelpers.CompileSeStringWrapped(Description);
    }
    
    protected IPluginLog Log => Service.Get<IPluginLog>();
}
