using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using ImGuiNET;
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
        public List<uint> TerritoryTypes { get; set; }

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
}
