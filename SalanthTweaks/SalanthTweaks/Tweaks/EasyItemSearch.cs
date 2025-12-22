using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.Shell;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class EasyItemSearch : ITweak
{
    public string DisplayName => "EasyItemSearch";
    public TweakStatus Status { get; set; }

    public void OnInitialize() { }

    public void Dispose() { }

    public void OnEnable() { }

    public void OnDisable() { }


    [AutoHook]
    [Signature(
        "48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 48 8B FA 4D 85 C0 75 0F 41 8D 40 08 48 8B 5C 24 ?? 48 83 C4 20 5F C3 49 8B 00 48 8B CB FF 50 48 80 B8 ?? ?? ?? ?? ?? 74 10 ",
        DetourName = nameof(ItemSearchDetour))]
    private Hook<ShellCommandInterface.Delegates.ExecuteCommand> hook = null;

    private unsafe int ItemSearchDetour(
        ShellCommandInterface* thisPtr, ShellCommandInterface.CommandContext* commandContext, void* source)
    {
        if (commandContext->StringArgs.Count > 1)
        {
            var firstArg = commandContext->StringArgs.First;

            for (var i = 1; i < commandContext->StringArgs.Count; i++)
            {
                firstArg->ConcatCStr(" ");
                firstArg->ConcatCStr(commandContext->StringArgs[i]);
            }
        }

        return hook.Original.Invoke(thisPtr, commandContext, source);
    }
}
