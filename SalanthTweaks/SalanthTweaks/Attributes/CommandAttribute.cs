using System;
using JetBrains.Annotations;

namespace SalanthTweaks.Attributes;

[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class CommandAttribute(string Command, string helpMessage, bool ShowInHelp = true, bool AutoEnable = false) : Attribute
{
    public string Command { get; } = Command;
    public string HelpMessage { get; } = helpMessage;
    public bool ShowInHelp { get; } = ShowInHelp;
    public bool AutoEnable { get; } = AutoEnable;
}
