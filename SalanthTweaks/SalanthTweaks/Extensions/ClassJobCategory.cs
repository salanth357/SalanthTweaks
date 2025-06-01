using SalanthTweaks.Enums;

namespace SalanthTweaks.Extensions;

public static class EnumExtensions
{
    public static Role GetRole(this ClassJob classJob)
    {
        return classJob switch
        {
            ClassJob.Viper or
                ClassJob.Pugilist or
                ClassJob.Monk or
                ClassJob.Lancer or
                ClassJob.Dragoon or
                ClassJob.Rogue or
                ClassJob.Ninja or
                ClassJob.Samurai or
                ClassJob.Reaper => Role.MeleeDps,

            ClassJob.Archer or
                ClassJob.Bard or
                ClassJob.Machinist or
                ClassJob.Dancer => Role.RangedPhysicalDps,

            ClassJob.Gladiator or
                ClassJob.Paladin or
                ClassJob.Marauder or
                ClassJob.Warrior or
                ClassJob.DarkKnight or
                ClassJob.Gunbreaker => Role.Tank,

            ClassJob.Conjurer or
                ClassJob.WhiteMage or
                ClassJob.Scholar or
                ClassJob.Astrologian or
                ClassJob.Sage => Role.Healer,

            ClassJob.Thaumaturge or
                ClassJob.BlackMage or
                ClassJob.Arcanist or
                ClassJob.Summoner or
                ClassJob.RedMage or
                ClassJob.Pictomancer or
                ClassJob.BlueMage => Role.MagicDps,

            ClassJob.Carpenter or
                ClassJob.Blacksmith or
                ClassJob.Armorer or
                ClassJob.Goldsmith or
                ClassJob.Leatherworker or
                ClassJob.Weaver or
                ClassJob.Alchemist or
                ClassJob.Culinarian => Role.Crafter,

            ClassJob.Miner or
                ClassJob.Botanist or
                ClassJob.Fisher => Role.Gatherer,
            
            ClassJob.Adventurer => Role.None,
            _ => throw new ArgumentOutOfRangeException(nameof(classJob), classJob, null)
        };
    }
}
