using Dalamud.Game.Text;
using Lumina.Text;

namespace SalanthTweaks.Extensions;

public static class SeStringBuilderExtensions
{
    public static SeStringBuilder AppendIcon(this SeStringBuilder othis, SeIconChar icon) => othis.Append(icon.ToIconString()); 

    public static SeStringBuilder AppendSalanthTweaksPrefix(this SeStringBuilder othis) => othis.PushColorType(9).AppendIcon(SeIconChar.BoxedLetterS).PopColorType().Append(" ");
}
