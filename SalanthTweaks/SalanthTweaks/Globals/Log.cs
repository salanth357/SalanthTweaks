using Dalamud.Plugin.Services;

namespace SalanthTweaks.Globals;

public static class LogHelper
{
    public static IPluginLog Log => Service.Get<IPluginLog>();
}

