using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace SalanthTweaks.Extensions;

public static class BaseParamExtensions
{
    public static ushort ParamPercentageForEquipSlot(this BaseParam bp, uint equipSlotRowId)
    {
        return equipSlotRowId switch
        {
            1 => bp.OneHandWeaponPercent,
            2 => bp.OffHandPercent,
            3 => bp.HeadPercent,
            4 => bp.ChestPercent,
            5 => bp.HandsPercent,
            6 => bp.WaistPercent,
            7 => bp.LegsPercent,
            8 => bp.FeetPercent,
            9 => bp.EarringPercent,
            10 => bp.NecklacePercent,
            11 => bp.BraceletPercent,
            12 => bp.RingPercent,
            13 => bp.TwoHandWeaponPercent,
            14 => bp.UnderArmorPercent,
            15 => bp.ChestHeadPercent,
            16 => bp.ChestHeadLegsFeetPercent,
            17 => bp.Unknown0,
            18 => bp.LegsFeetPercent,
            19 => bp.HeadChestHandsLegsFeetPercent,
            20 => bp.ChestLegsGlovesPercent,
            21 => bp.ChestLegsFeetPercent,
            22 => bp.Unknown1,
            23 => bp.Unknown3,
            _ => 0
        };
    }

    public static ushort ParamValueForItemLevel(this ItemLevel il, BaseParam prm)
    {
        return prm.RowId switch
        {
            1 => il.Strength,
            2U => il.Dexterity,
            3U => il.Vitality,
            4U => il.Intelligence,
            5U => il.Mind,
            6U => il.Piety,
            7U => il.HP,
            8U => il.MP,
            9U => il.TP,
            10U => il.GP,
            11U => il.CP,
            12U => il.PhysicalDamage,
            13U => il.MagicalDamage,
            14U => il.Delay,
            15U => il.AdditionalEffect,
            16U => il.AttackSpeed,
            17U => il.BlockRate,
            18U => il.BlockStrength,
            19U => il.Tenacity,
            20U => il.AttackPower,
            21U => il.Defense,
            22U => il.DirectHitRate,
            23U => il.Evasion,
            24U => il.MagicDefense,
            25U => il.CriticalHitPower,
            26U => il.CriticalHitResilience,
            27U => il.CriticalHit,
            28U => il.CriticalHitEvasion,
            29U => il.SlashingResistance,
            30U => il.PiercingResistance,
            31U => il.BluntResistance,
            32U => il.ProjectileResistance,
            33U => il.AttackMagicPotency,
            34U => il.HealingMagicPotency,
            35U => il.EnhancementMagicPotency,
            36U => il.EnfeeblingMagicPotency,
            37U => il.FireResistance,
            38U => il.IceResistance,
            39U => il.WindResistance,
            40U => il.EarthResistance,
            41U => il.LightningResistance,
            42U => il.WaterResistance,
            43U => il.MagicResistance,
            44U => il.Determination,
            45U => il.SkillSpeed,
            46U => il.SpellSpeed,
            47U => il.Haste,
            48U => il.Morale,
            49U => il.Enmity,
            50U => il.EnmityReduction,
            51U => il.CarefulDesynthesis,
            52U => il.EXPBonus,
            53U => il.Regen,
            54U => il.Refresh,
            55U => il.MovementSpeed,
            56U => il.Spikes,
            57U => il.SlowResistance,
            58U => il.PetrificationResistance,
            59U => il.ParalysisResistance,
            60U => il.SilenceResistance,
            61U => il.BlindResistance,
            62U => il.PoisonResistance,
            63U => il.StunResistance,
            64U => il.SleepResistance,
            65U => il.BindResistance,
            66U => il.HeavyResistance,
            67U => il.DoomResistance,
            68U => il.ReducedDurabilityLoss,
            69U => il.IncreasedSpiritbondGain,
            70U => il.Craftsmanship,
            71U => il.Control,
            72U => il.Gathering,
            73U => il.Perception,
            _ => 0
        };
    }
}
