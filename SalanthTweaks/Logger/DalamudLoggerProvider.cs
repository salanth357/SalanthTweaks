using System;
using System.Collections.Concurrent;
using System.Linq;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;

namespace SalanthTweaks.Logger;

public class DalamudLoggerProvider(IPluginLog pluginLog) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DalamudLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public ILogger CreateLogger(string moduleName)
        => _loggers.GetOrAdd(moduleName.Split('.').Last(), name => new DalamudLogger(name, pluginLog));

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
