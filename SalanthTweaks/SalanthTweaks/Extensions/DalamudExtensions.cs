using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SalanthTweaks.Extensions;

public static class DalamudExtensions
{
    public static unsafe Span<AtkValue> AtkValues(this AddonArgs args)
    {
        return args switch
        {
            AddonRefreshArgs a => new Span<AtkValue>((void*)a.AtkValues, (int)a.AtkValueCount),
            AddonSetupArgs a => new Span<AtkValue>((void*)a.AtkValues, (int)a.AtkValueCount),
            _ => throw new ArgumentOutOfRangeException(nameof(args))
        };
    }
}
