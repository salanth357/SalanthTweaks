using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs;
using KamiToolKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SalanthTweaks.Logger;

namespace SalanthTweaks;

public static class Service
{
    public static IServiceCollection Collection { get; } = new ServiceCollection();

    public static ServiceProvider? Provider { get; private set;  }
    public static void BuildProvider() => Provider = Collection.BuildServiceProvider();

    public static void Dispose() => Provider?.Dispose();

    public static T Get<T>() where T : notnull
        => Provider!.GetRequiredService<T>();
    
    public static bool TryGet<T>([NotNullWhen(returnValue: true)] out T? service)
    {
        if (Provider == null)
        {
            service = default;
            return false;
        }

        try
        {
            service = Provider.GetService<T>();
            return service != null;
        }
        catch // might catch ObjectDisposedException here
        {
            service = default;
            return false;
        }
    }
    
    public static IServiceCollection AddDalamud(this IServiceCollection collection, IDalamudPluginInterface pluginInterface)
    {
        collection
            .AddSingleton(pluginInterface)
            .AddSingleton(DalamudServiceFactory<IAddonEventManager>)
            .AddSingleton(DalamudServiceFactory<IAddonLifecycle>)
            .AddSingleton(DalamudServiceFactory<IAetheryteList>)
            .AddSingleton(DalamudServiceFactory<IBuddyList>)
            .AddSingleton(DalamudServiceFactory<IChatGui>)
            .AddSingleton(DalamudServiceFactory<IClientState>)
            .AddSingleton(DalamudServiceFactory<ICommandManager>)
            .AddSingleton(DalamudServiceFactory<ICondition>)
            .AddSingleton(DalamudServiceFactory<IContextMenu>)
            .AddSingleton(DalamudServiceFactory<IDataManager>)
            .AddSingleton(DalamudServiceFactory<IDtrBar>)
            .AddSingleton(DalamudServiceFactory<IDutyState>)
            .AddSingleton(DalamudServiceFactory<IFateTable>)
            .AddSingleton(DalamudServiceFactory<IFlyTextGui>)
            .AddSingleton(DalamudServiceFactory<IFramework>)
            .AddSingleton(DalamudServiceFactory<IGameConfig>)
            .AddSingleton(DalamudServiceFactory<IGameGui>)
            .AddSingleton(DalamudServiceFactory<IGameInteropProvider>)
            .AddSingleton(DalamudServiceFactory<IGameInventory>)
            .AddSingleton(DalamudServiceFactory<IGameLifecycle>)
            .AddSingleton(DalamudServiceFactory<IGameNetwork>)
            .AddSingleton(DalamudServiceFactory<IGamepadState>)
            .AddSingleton(DalamudServiceFactory<IJobGauges>)
            .AddSingleton(DalamudServiceFactory<IKeyState>)
            .AddSingleton(DalamudServiceFactory<IMarketBoard>)
            .AddSingleton(DalamudServiceFactory<INotificationManager>)
            .AddSingleton(DalamudServiceFactory<IObjectTable>)
            .AddSingleton(DalamudServiceFactory<IPartyFinderGui>)
            .AddSingleton(DalamudServiceFactory<IPartyList>)
            .AddSingleton(DalamudServiceFactory<IPluginLog>)
            .AddSingleton(DalamudServiceFactory<ISigScanner>)
            .AddSingleton(DalamudServiceFactory<ITargetManager>)
            .AddSingleton(DalamudServiceFactory<ITextureProvider>)
            .AddSingleton(DalamudServiceFactory<ITextureReadbackProvider>)
            .AddSingleton(DalamudServiceFactory<ITextureSubstitutionProvider>)
            .AddSingleton(DalamudServiceFactory<ITitleScreenMenu>)
            .AddSingleton(DalamudServiceFactory<IToastGui>)
            .AddSingleton(_ => new NativeController(pluginInterface))
#pragma warning disable SeStringEvaluator
            .AddSingleton(DalamudServiceFactory<ISeStringEvaluator>)
#pragma warning restore SeStringEvaluator
            .AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.Services.AddSingleton<ILoggerProvider>(serviceProvider => new DalamudLoggerProvider(serviceProvider.GetRequiredService<IPluginLog>()) );
            });

        return collection;

        T DalamudServiceFactory<T>(IServiceProvider serviceProvider) => new Services.DalamudServiceWrapper<T>(pluginInterface).Service;
    }
}
