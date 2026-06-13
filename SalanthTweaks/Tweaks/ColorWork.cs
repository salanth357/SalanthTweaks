using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Extensions;
using Lumina.Text.ReadOnly;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public partial class ColorWork : ITweak
{
    [Sheet("QuestDialogue")]
    public readonly struct QuestDialogue(RawRow row) : IExcelRow<QuestDialogue>
    {
        public uint RowId => row.RowId;

        public ReadOnlySeString Key => row.ReadStringColumn(0);

        public ReadOnlySeString Value => row.ReadStringColumn(1);

        static QuestDialogue IExcelRow<QuestDialogue>.Create(ExcelPage page, uint offset, uint row) =>
            new(new RawRow(page, offset, row));

        public ExcelPage ExcelPage => row.ExcelPage;

        public uint RowOffset => row.RowOffset;
    }

    public string DisplayName => "ColorWork";
    public TweakStatus Status { get; set; }

    public void OnInitialize() { }

    public void Dispose() { }

    public void OnEnable()
    {
        var excel = Service.Get<IDataManager>().Excel;
        var pl = Service.Get<IPluginLog>();

        var qd = excel.GetSheet<QuestDialogue>(name: "quest/039/BanDwa108_03909").FirstOrNull(x => false);
        pl.Info("valval: {0}", qd?.Value.ToString() ?? "33");
        SortedSet<string> sayKeys = [];
        foreach (var shtName in excel.SheetNames)
        {
            if (!shtName.StartsWith("quest/")) continue;

            var sheet = excel.GetSheet<RawRow>(name: shtName);
            var chatModeRows = sheet.Where(r => r.ReadStringColumn(1).ToString().Contains("the chat mode"))
                                    .Select(r => r.ReadStringColumn(1).ToString());
            var header = false;
            foreach (var sayRow in chatModeRows)
            {
                if (!header)
                {
                    header = true;
                }

                var match = findMatch(sayRow, sheet);
                if (match != "")
                {
                    sayKeys.Add(match);
                }
            }
        }

        foreach (var sayKey in sayKeys)
            pl.Info(sayKey);


        //
        // foreach (var lang in (Language[])[Language.English, Language.French, Language.German, Language.Japanese])
        // {
        //     var sheet = excel.GetSheet<RawRow>(name: "quest/052/BanMam115_05275", language: lang);
        //     var haystack = sheet.GetRow(24).ReadStringColumn(1).ToString();
        //     var needle = sheet.GetRow(76).ReadStringColumn(1).ToString();
        //
        //     var re = lang switch
        //     {
        //         Language.English => EnFrRegex(),
        //         Language.French => EnFrRegex(),
        //         Language.Japanese => JpRegex(),
        //         Language.German => DeRegex(),
        //         _ => null
        //     };
        //     if (re == null) continue;
        //
        //     var found = re.Matches(haystack).Any(m => m.Captures[0].Value.Contains(needle));
        //     
        //     Service.Get<IPluginLog>().Information($"Found: {lang} = {found}");
        //     if (!found)
        //     {
        //         Service.Get<IPluginLog>().Information($"needle: {needle}");
        //         Service.Get<IPluginLog>().Information($"haystk: {haystack}");
        //     }
        // }
        // var haystack = Service.Get<ISeStringEvaluator>().EvaluateMacroString("<sheet(quest/052/BanMam115_05275,24,1)>").ToString();
        // var needle = Service.Get<ISeStringEvaluator>().EvaluateMacroString("<sheet(quest/052/BanMam115_05275,76,1)>").ToString();
        // var found = haystack.Contains($"“{needle}”");
    }

    private string findMatch(string haystack, ExcelSheet<RawRow> sheet)
    {
        var matches = EnFrRegex().Matches(haystack);
        return sheet.FirstOrNull(r =>
        {
            var needle = r.ReadStringColumn(1);
            return needle != "" && matches.Any(m => m.Captures[0].Value.Contains(needle.ToString()));
        })?.ReadStringColumn(0).ToString() ?? string.Empty;
    }

    public void OnDisable() { }


    private Vector3 addBack = Vector3.Zero;
    private Vector3 multiplyBack = new(100f / 255);

    private Vector3 addFront = Vector3.Zero;
    private Vector3 multiplyFront = new(100f / 255);

    public void DrawConfig()
    {
        var sit = Service.Get<ITextureProvider>().GetFromGame("ui/uld/TargetCursor_hr1.tex");
        var tex = sit.GetWrapOrEmpty();
        ImGui.Text($"{tex.Size.X}x{tex.Size.Y}");

        var back = colorPicker(ref multiplyBack, ref addBack, "back");
        var front = colorPicker(ref multiplyFront, ref addFront, "front");

        var curp = ImGui.GetCursorPos();
        ImGui.Image(tex.Handle, new Vector2(144, 248), new Vector2(204 / tex.Size.X, 0),
                    new Vector2((204 + 144) / tex.Size.X, 248 / tex.Size.Y), back);
        ImGui.SetCursorPos(curp);
        ImGui.Image(tex.Handle, new Vector2(144, 248), new Vector2(348 / tex.Size.X, 0),
                    new Vector2((348 + 144) / tex.Size.X, 248 / tex.Size.Y), front);
    }

    private Vector4 colorPicker(ref Vector3 mult, ref Vector3 add, string label)
    {
        ImGui.SetNextItemWidth(151);
        ImGui.ColorEdit3($"Multiply##{label}", ref mult, ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.DisplayHex);
        ImGui.SetNextItemWidth(124);
        ImGui.DragFloat3($"##add{label}", ref add, 1, -255, 255, "%.0f");
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (4 * ImGuiHelpers.GlobalScale));

        var addTF = (add / 510f) + new Vector3(0.5f);
        ImGui.ColorEdit3($"##ce{label}", ref addTF,
                         ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoInputs);


        add = (addTF * 510f) - new Vector3(255f);
        ImGui.SameLine();
        ImGui.Text("Add");
        ImGui.Text($"{add.X}, {add.Y}, {add.Z}");
        return new Vector4(mult, 1).Add(new Vector4(addTF, 1)).Clamp();
    }

    [GeneratedRegex(@"“([^”]+)”")]
    private static partial Regex EnFrRegex();

    [GeneratedRegex(@"「([^」])+」|『([^』]+)』")]
    private static partial Regex JpRegex();

    [GeneratedRegex(@"„([^“]+)“")]
    private static partial Regex DeRegex();
}
