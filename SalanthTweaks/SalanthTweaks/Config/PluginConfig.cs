using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using SalanthTweaks.Services;

namespace SalanthTweaks.Config;

[Serializable]
public partial class PluginConfig : IPluginConfiguration
{

    [JsonIgnore]
    public const int CurrentVersion = 0;

    [JsonIgnore]
    private static IDalamudPluginInterface? PluginInterface;


    public static PluginConfig Load(IServiceProvider serviceProvider)
    {
        PluginInterface = serviceProvider.GetRequiredService<IDalamudPluginInterface>();

        if (PluginInterface.GetPluginConfig() is not PluginConfig cfg)
        {
            cfg = new PluginConfig();
        }

        if (cfg.Upgrade()) cfg.Save();
        return cfg;
    }

    private bool Upgrade()
    {
        // Update globals
     //    Service.Get<TweakManager>().UpgradeConfigs();
        return false;
    }
    
    public void Save() => PluginInterface!.SavePluginConfig(this);
}

public partial class PluginConfig
{
    public int Version { get; set; } = 0;
    public HashSet<string> EnabledTweaks = [];
}
