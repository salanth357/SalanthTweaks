using Dalamud.Interface;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Lua;
using FFXIVClientStructs.Havok.Common.Base.System.IO.IStream;

namespace SalanthTweaks.Services;

[RegisterSingleton]
public class FontHelper
{
    public IFontHandle HeaderFont { get; private set; }
    public IFontHandle LargeFont { get; private set; }

    public FontHelper(IDalamudPluginInterface iface)
    {
        using (iface.UiBuilder.FontAtlas.SuppressAutoRebuild())
        {
            HeaderFont = iface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
            {
                e.OnPreBuild(tk => tk.AddDalamudDefaultFont(-2));
            });
            LargeFont = iface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
            {
                e.OnPreBuild(tk => tk.AddDalamudDefaultFont(iface.UiBuilder.DefaultFontSpec.SizePx + 1.5f));
            });
        }
    }
}
