using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ImGuiNET;
using SalanthTweaks.Config;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using SalanthTweaks.Services;

namespace SalanthTweaks.Windows;

[RegisterSingleton]
public class ConfigWindow : Window, IDisposable
{

    private readonly TweakManager TweakManager;
    private ITweak[] Tweaks;
    private ITweak? SelectedTweak;
    
    
    public ConfigWindow(
        TweakManager tweakManager,
        WindowManager windowManager,
        IEnumerable<ITweak> tweaks) : base("SalTweaksConfig###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize;
        AllowClickthrough = false;
        AllowPinning = false;
         
        Size = new Vector2(700, 575);
        SizeCondition = ImGuiCond.Appearing;
        
        TweakManager = tweakManager;
        Tweaks = tweaks.ToArray();
        windowManager.AddWindow(this);
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        DrawSidebar();
        ImGui.SameLine();
        DrawConfig();
    }

    private void DrawSidebar()
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var section = ImRaii.Child("##Sidebar", new Vector2(250 * scale, -1), true);
        
        using var tbl = ImRaii.Table("##SBTable", 2, ImGuiTableFlags.NoSavedSettings);
        
        ImGui.TableSetupColumn("Check", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Tweak", ImGuiTableColumnFlags.WidthStretch);

        foreach (var tweak in Tweaks)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            
            var status = tweak.Status;
            
            using var labelStyle = status switch
            {
                TweakStatus.InitializationFailed or TweakStatus.Outdated => ImRaii.PushColor(
                    ImGuiCol.Text, ImGuiColors.DalamudRed),
                TweakStatus.Enabled => ImRaii.PushColor(ImGuiCol.Border, 0, false),
                TweakStatus.Uninitialized or TweakStatus.Initialized or TweakStatus.Disabled or TweakStatus.Disposed =>
                    ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey),
                _ => ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey)
            };
            
            if (status is TweakStatus.InitializationFailed or TweakStatus.Outdated)
            {
                Vector2 tl, br;
                using (ImRaii.Disabled())
                {
                    var fh = ImGui.GetFrameHeight();
                    var sz = new Vector2(fh, fh);
                    using (ImRaii.PushFont(UiBuilder.IconFont))
                        ImGui.Button(FontAwesomeIcon.Times.ToIconString(), sz);
                    tl = ImGui.GetItemRectMin();
                    ImGui.TableNextColumn();
                    ImGui.Text(tweak.DisplayName);
                    br = ImGui.GetItemRectMax();
                    br.Y = tl.Y+ImGui.GetFrameHeight();
                }
                if (ImGui.IsMouseHoveringRect(tl, br, false))
                    using (ImRaii.Tooltip())
                        ImGui.TextUnformatted(Enum.GetName(status));
            }
            else
            {
                var enabled = status == TweakStatus.Enabled;
                if (ImGui.Checkbox($"##Enabled_{tweak.InternalName}", ref enabled))
                {
                    if (!enabled) TweakManager.DisableTweak(tweak);
                    else TweakManager.EnableTweak(tweak);
                }
                ImGui.TableNextColumn();
                if (ImGui.Selectable($"{tweak.DisplayName}##Selectable_{tweak.InternalName}",
                                     SelectedTweak != null && SelectedTweak == tweak))
                {
                    if (SelectedTweak == null || SelectedTweak != tweak)
                    {
                        // if we have a selected tweak, handle any closing we need to do
                        SelectedTweak = tweak;
                        // handle opening new tweak config
                    }
                    else
                    {
                        SelectedTweak = null;
                    }
                }
            }


            ImGui.AlignTextToFramePadding();  
        }
    }

    private void DrawConfig()
    {
        using var section = ImRaii.Child("##ConfigDisplay", new Vector2(-1 * ImGuiHelpers.GlobalScale, -1), true);

        if (SelectedTweak == null)
        {
            var cursorPos = ImGui.GetCursorPos();
            var contentAvail = ImGui.GetContentRegionAvail();

            using (Service.Get<FontHelper>().HeaderFont.Push())
                ImGuiHelpers.CenteredText("SalanthTweaks!");
            
            
            var version = GetType().Assembly.GetName().Version?.ToString();
            version = null;
            var label = $"v{version ?? "Unk"}";
#if DEBUG
            label += "-dev";
#endif
            var lnk = "https://github.com/salanth357/SalanthTweaks/";
            if (version != null)
            {
                lnk += $"releases/tag/v{version}";
            }

            ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize(label));
            ImGui.TextUnformatted(label);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                using (ImRaii.Tooltip())
                {
                    ImGui.TextUnformatted(lnk);
                }
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                Task.Run(() => Util.OpenLink(lnk));
            }
            return;
        }

        using (Service.Get<FontHelper>().HeaderFont.Push())
            ImGuiHelpers.CenteredText(SelectedTweak.DisplayName);
        
        if (SelectedTweak is IConfigurableTweak tweak)
        {
            tweak.DrawConfig();
        }
    }
}
