using System.Reflection;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace SalanthTweaks.UI;

public static class ObjectPrinter
{
    public static void DrawObject(object obj)
    {
        var typ = obj.GetType();
        using var tbl = ImRaii.Table("Obj", 2);
        var fields = typ.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(field.Name);
            ImGui.TableNextColumn();
            var val = "NULL";
            try
            {
                val = field.GetValue(obj)?.ToString() ?? "NULL";
            }
            catch
            {
                // ignored
            }

            ImGui.Text(val);
        }
        
        var props = typ.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var prop in props)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(prop.Name);
            ImGui.TableNextColumn();
            var val = "NULL";
            try
            {
                val = prop.GetValue(obj)?.ToString() ?? "NULL";
            }
            catch
            {
                // ignored
            }

            ImGui.Text(val);
        }
    }
}
