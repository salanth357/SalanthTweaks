
using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.ReadOnly;

namespace SalanthTweaks.Services;

[RegisterSingleton]
public class ItemService
{
    public uint GetItemRarityColorType(uint itemId, bool isEdgeColor = false)
    {
        if (IsEventItem(itemId))
            return GetItemRarityColorType(1, isEdgeColor);

        if (!Service.Get<IDataManager>().Excel.TryGetRow<Item>(GetBaseItemId(itemId), out var item))
            return GetItemRarityColorType(1, isEdgeColor);

        return (isEdgeColor ? 548u : 547u) + (item.Rarity * 2u);
    }
    
    public uint GetItemRarityColorType(Item item, bool isEdgeColor = false)
    {
        if (IsEventItem(item.RowId))
            return GetItemRarityColorType(1, isEdgeColor);
        
        return (isEdgeColor ? 548u : 547u) + (item.Rarity * 2u);
    }

    public ReadOnlySeString GetItemLink(uint itemId, ClientLanguage? language = null)
    {
        if (!Service.Get<IDataManager>().Excel.TryGetRow<Item>(itemId, out var item))
        {
            return new ReadOnlySeString(string.Empty);
        }

        return GetItemLink(item, language);
    }
    public ReadOnlySeString GetItemLink(Item item, ClientLanguage? language = null)
    {

        var itemName = item.Name.ExtractText();

        if (IsHighQuality(item.RowId))
            itemName += " \uE03C";
        else if (IsCollectible(item.RowId))
            itemName += " \uE03D";

        var itemLink = new SeStringBuilder()
                       .PushColorType(GetItemRarityColorType(item))
                       .PushEdgeColorType(GetItemRarityColorType(item, true))
                       .PushLinkItem(item.RowId, itemName)
                       .Append(itemName)
                       .PopLink()
                       .PopEdgeColorType()
                       .PopColorType()
                       .ToReadOnlySeString();

#pragma warning disable SeStringEvaluator
        return Service.Get<ISeStringEvaluator>().EvaluateFromAddon(371, [itemLink], language);
#pragma warning restore SeStringEvaluator
    }
}
