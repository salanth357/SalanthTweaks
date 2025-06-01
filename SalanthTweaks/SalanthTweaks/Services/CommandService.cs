using System.Collections.Generic;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using SalanthTweaks.Attributes;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Services;

[RegisterSingleton]
public class CommandService(ICommandManager CommandManager, IEnumerable<ITweak> tweaks) : IDisposable
{
    internal class CommandHandler(CommandAttribute attr, IReadOnlyCommandInfo.HandlerDelegate dg)
    {
           
        internal CommandInfo Info => new (dg)
        {
            ShowInHelp = attr.ShowInHelp,
            HelpMessage = attr.HelpMessage
        };

        internal string Command => attr.Command;
    }

    private readonly Dictionary<string, CommandHandler> commands = [];
    public void Initialize()
    {
        foreach (var tweak in tweaks)
        {
            foreach (var methodInfo in tweak.GetType().GetMethods())
            {
                var attr = methodInfo.GetCustomAttribute<CommandAttribute>();
                if (attr == null) continue;
                var dg = methodInfo.CreateDelegate<IReadOnlyCommandInfo.HandlerDelegate>(tweak);
                Register(attr, dg);
            }
        }
    }

    public void Register(IReadOnlyCommandInfo.HandlerDelegate dg)
    {
        var attr = dg.Method.GetCustomAttribute<CommandAttribute>() ?? throw new Exception($"Missing CommandAttribute on {dg.Method.Name}");
        Register(attr, dg);        
    }

    private void Register(CommandAttribute attr, IReadOnlyCommandInfo.HandlerDelegate dg)
    {
        var cmd = new CommandHandler(attr, dg);
        commands.Add(attr.Command, cmd);
        if (attr.AutoEnable)
            Enable(cmd);
    }

    public void Enable(IReadOnlyCommandInfo.HandlerDelegate dg) => Enable(dg.Method.GetCustomAttribute<CommandAttribute>() ?? throw new Exception($"Missing CommandAttribute on {dg.Method.Name}"));
        public void Enable(CommandAttribute attr) => Enable(commands[attr.Command]); 
    public void Enable(string command) => Enable(commands[command]);

    private void Enable(CommandHandler cmd)
    {
        CommandManager.AddHandler(cmd.Command, cmd.Info);
    }

    public void Disable(IReadOnlyCommandInfo.HandlerDelegate dg) => Disable(dg.Method.GetCustomAttribute<CommandAttribute>() ?? throw new Exception($"Missing CommandAttribute on {dg.Method.Name}"));
    public void Disable(CommandAttribute attr) => Disable(commands[attr.Command]); 
    public void Disable(string command) => Disable(commands[command]);

    private void Disable(CommandHandler cmd)
    {
        CommandManager.RemoveHandler(cmd.Command);
    }

    public void Dispose()
    {
        foreach (var cmd in commands.Values)
        {
            Disable(cmd);
        }
        GC.SuppressFinalize(this);
    }
}
