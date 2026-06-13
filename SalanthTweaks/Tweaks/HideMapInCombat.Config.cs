using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using SalanthTweaks.Attributes;
using SalanthTweaks.Config;

namespace SalanthTweaks.Tweaks;

public partial class HideMapInCombat
{
    [TweakConfig]
    public TConfig Config { get; set; } = null!;

    public class TConfig : TweakConfig
    {
        private const int CurrentVersion = 1;

        public bool AllowedOnly { get; set; }
        public List<uint> TerritoryTypes { get; set; } = [];

        public override bool Update()
        {
            if (Version >= CurrentVersion) return false;
            while (Version < CurrentVersion)
            {
                switch (Version)
                {
                    case 0:
                        TerritoryTypes = [];
                        AllowedOnly = false;
                        break;
                }

                Version++;
            }

            return true;
        }
    }

    private static readonly Lazy<List<TerritoryType>> TerritoryTypes =
        new(() => Service.Get<IDataManager>().GetExcelSheet<TerritoryType>().Where(tt => tt.Name != "").ToList());

    private static readonly Lazy<string[]> TerritoryNames =
        new(() => TerritoryTypes.Value.Select(tt => tt.PlaceName.Value.Name.ExtractText()).ToArray());

    private int currentSelectedTerritory;

    public void DrawConfig()
    {
        var isAllow = Config.AllowedOnly ? 1 : 0;
        bool changed = false;
        if (ImGui.RadioButton("Disable in listed locations", ref isAllow, 0))
        {
            changed = true;
            Config.AllowedOnly = false;
        }

        if (ImGui.RadioButton("Enable only in listed locations", ref isAllow, 1))
        {
            changed = true;
            Config.AllowedOnly = true;
        }

        if (ImGui.Combo("territory", ref currentSelectedTerritory, TerritoryNames.Value, TerritoryNames.Value.Length))
        {
            changed = true;
            // combo changed
        }

        if (changed)
            SaveConfig();
    }
}
