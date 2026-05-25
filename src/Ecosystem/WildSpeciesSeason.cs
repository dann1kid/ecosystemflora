using System.Collections.Generic;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-species seasonal spread activity and winter/end-season survival (v2.3).</summary>
    internal static class WildSpeciesSeason
    {
        public readonly struct Profile
        {
            public readonly float SpringSpread;
            public readonly float SummerSpread;
            public readonly float FallSpread;
            public readonly float WinterSpread;
            /// <summary>0 = dies every winter stress pass; 1 = never seasonal winter kill.</summary>
            public readonly float WinterSurvival;
            /// <summary>Extra stress failure roll in fall (end-of-season die-off).</summary>
            public readonly float FallDieoffChance;

            public Profile(
                float springSpread,
                float summerSpread,
                float fallSpread,
                float winterSpread,
                float winterSurvival,
                float fallDieoffChance = 0f)
            {
                SpringSpread = springSpread;
                SummerSpread = summerSpread;
                FallSpread = fallSpread;
                WinterSpread = winterSpread;
                WinterSurvival = winterSurvival;
                FallDieoffChance = fallDieoffChance;
            }

            public float SpreadMultiplier(EnumSeason season)
            {
                switch (season)
                {
                    case EnumSeason.Spring: return SpringSpread;
                    case EnumSeason.Summer: return SummerSpread;
                    case EnumSeason.Fall: return FallSpread;
                    default: return WinterSpread;
                }
            }
        }

        static readonly Profile MeadowAnnual = new Profile(2.2f, 1f, 0.35f, 0.02f, 0f, 0.4f);
        static readonly Profile MeadowColonizer = new Profile(2.6f, 0.55f, 0.2f, 0f, 0f, 0.55f);
        static readonly Profile MeadowPerennial = new Profile(1.45f, 1.1f, 0.65f, 0.12f, 0.3f, 0.2f);
        static readonly Profile WinterHardyGrass = new Profile(1.55f, 1f, 0.75f, 0.22f, 0.55f, 0.15f);
        static readonly Profile ForestPerennial = new Profile(1.35f, 1f, 0.8f, 0.08f, 0.35f, 0.15f);
        static readonly Profile EdgePerennial = new Profile(1.4f, 1.05f, 0.7f, 0.1f, 0.28f, 0.18f);
        static readonly Profile AquaticWarm = new Profile(1.5f, 1.2f, 0.85f, 0.25f, 0.6f, 0f);
        static readonly Profile TreeSapling = new Profile(1.25f, 1f, 0.9f, 0.05f, 0.5f, 0f);
        static readonly Profile DefaultMeadow = MeadowPerennial;

        static readonly Dictionary<string, Profile> BySpecies = Build();

        static Dictionary<string, Profile> Build()
        {
            return new Dictionary<string, Profile>
            {
                ["wilddaisy"] = MeadowAnnual,
                ["cornflower"] = MeadowAnnual,
                ["goldenpoppy"] = MeadowAnnual,
                ["heather"] = MeadowAnnual,
                ["westerngorse"] = MeadowAnnual,
                ["forgetmenot"] = MeadowAnnual,
                ["orangemallow"] = MeadowAnnual,
                ["woad"] = MeadowAnnual,
                ["mugwort"] = MeadowColonizer,
                ["horsetail"] = MeadowColonizer,
                ["redtopgrass"] = MeadowColonizer,

                ["cowparsley"] = MeadowPerennial,
                ["daffodil"] = MeadowPerennial,
                ["catmint"] = EdgePerennial,
                ["edelweiss"] = EdgePerennial,
                ["lupine"] = MeadowPerennial,

                ["tallgrass"] = WinterHardyGrass,

                ["bluebell"] = ForestPerennial,
                ["lilyofthevalley"] = ForestPerennial,
                ["ghostpipewhite"] = ForestPerennial,
                ["ghostpipepink"] = ForestPerennial,
                ["ghostpipered"] = ForestPerennial,
                ["eaglefern"] = ForestPerennial,
                ["cinnamonfern"] = ForestPerennial,
                ["deerfern"] = ForestPerennial,
                ["hartstongue"] = ForestPerennial,
                ["tallfern"] = EdgePerennial,

                ["coopersreed"] = AquaticWarm,
                ["papyrus"] = AquaticWarm,
                ["waterlily"] = AquaticWarm,
                ["watercrowfoot"] = AquaticWarm,

                ["strawberry"] = MeadowAnnual,
                ["cloudberry"] = MeadowAnnual,
            };
        }

        public static bool TryGet(string species, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(species)) return false;

            if (BySpecies.TryGetValue(species, out profile)) return true;

            if (WildTreeEcology.TryGet(species, out _))
            {
                profile = TreeSapling;
                return true;
            }

            if (WildBerryEcology.TryGet(species, out _))
            {
                profile = EdgePerennial;
                return true;
            }

            if (WildFernEcology.TryGet(species, out _))
            {
                profile = ForestPerennial;
                return true;
            }

            return false;
        }

        public static Profile Resolve(string species)
        {
            if (TryGet(species, out Profile profile)) return profile;
            return DefaultMeadow;
        }
    }
}
