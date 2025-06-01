using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace SalanthTweaks.Globals;

public static class ItemHelpers
{
    private static int? EventItemRowCount;

    public static uint GetBaseItemId(uint id)
    {
        if (IsEventItem(id)) return id; // uses EventItem sheet
        if (IsHighQuality(id)) return id - 1_000_000;
        if (IsCollectible(id)) return id - 500_000;
        return id;
    }

    public static bool IsNormalItem(uint itemId)
    {
        return itemId < 500_000;
    }

    public static bool IsCollectible(uint itemId)
    {
        return itemId is >= 500_000 and < 1_000_000;
    }

    public static bool IsHighQuality(uint itemId)
    {
        return itemId is >= 1_000_000 and < 2_000_000;
    }

    public static bool IsEventItem(uint itemId)
    {
        return itemId >= 2_000_000 && itemId - 2_000_000 < (EventItemRowCount ??= Service.Get<IDataManager>().Excel.GetSheet<EventItem>().Count);
    }
}
