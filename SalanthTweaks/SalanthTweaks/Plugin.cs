using System.IO;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using SalanthTweaks.Config;
using SalanthTweaks.Windows;
using SalanthTweaks.Services;
using InteropGenerator.Runtime;
namespace SalanthTweaks;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("SalanthTweaks");

    public Plugin(IFramework framework, IDalamudPluginInterface pluginInterface, IDataManager dataManager, ISigScanner sigScanner)
    {
        
#if HAS_LOCAL_CS
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        Resolver.GetInstance.Resolve();
#endif
        
        Service.Collection.AddDalamud(PluginInterface)
               .AddSingleton(PluginConfig.Load)
               .AddSalanthTweaks();
        Service.BuildProvider();
        
        framework.RunOnFrameworkThread(() =>
        {
            Service.Get<CommandService>().Initialize();
            Service.Get<TweakManager>().Initialize();
            Service.Get<CommandService>().Register(OnCommand);
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
#if DEBUG
            ToggleConfigUi();
#endif
        });
    }

    public void Dispose()
    {
        Service.Get<TweakManager>().Shutdown();
        WindowSystem.RemoveAllWindows();
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        Service.Dispose();
    }

    [Attributes.Command("/saltweaks", "SalTweaks", AutoEnable: true)]
    private void OnCommand(string command, string args)
    {
        ToggleConfigUi();
    }
    
    public static void ToggleConfigUi()
    {
        Service.Get<ConfigWindow>().Toggle();
    }
}
