using System.Collections.Generic;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Per-species monthly spread activity and stress curves (v2.3 → v2.9 monthly polish).
    /// Month indices: 0=Jan .. 11=Dec. Spread values are multipliers on base chance/interval.
    /// Stress values are per-check seasonal die-off probability (0 = no seasonal stress).
    /// </summary>
    internal static class WildSpeciesSeason
    {
        public readonly struct Profile
        {
            readonly float[] spread;
            readonly float[] stress;

            public Profile(float[] monthlySpread, float[] monthlyStress)
            {
                spread = monthlySpread;
                stress = monthlyStress;
            }

            /// <summary>Legacy 4-season constructor — expands to 12 months with smooth transitions.</summary>
            public Profile(
                float springSpread,
                float summerSpread,
                float fallSpread,
                float winterSpread,
                float winterSurvival,
                float fallDieoffChance = 0f)
            {
                float winterStress = winterSurvival >= 1f ? 0f : (1f - winterSurvival);
                spread = ExpandSeasons(springSpread, summerSpread, fallSpread, winterSpread);
                stress = ExpandStress(fallDieoffChance, winterStress);
            }

            public float SpreadMultiplier(int month)
            {
                if (spread == null || spread.Length != 12) return 1f;
                return spread[Clamp(month, 0, 11)];
            }

            public float SpreadMultiplierInterpolated(float yearProgress)
            {
                if (spread == null || spread.Length != 12) return 1f;
                float monthF = yearProgress * 12f;
                int m0 = ((int)monthF) % 12;
                int m1 = (m0 + 1) % 12;
                float t = monthF - (int)monthF;
                return spread[m0] * (1f - t) + spread[m1] * t;
            }

            public float StressChance(int month)
            {
                if (stress == null || stress.Length != 12) return 0f;
                return stress[Clamp(month, 0, 11)];
            }

            public float SpreadMultiplier(EnumSeason season)
            {
                switch (season)
                {
                    case EnumSeason.Spring: return spread != null ? spread[3] : 1f;
                    case EnumSeason.Summer: return spread != null ? spread[6] : 1f;
                    case EnumSeason.Fall: return spread != null ? spread[9] : 1f;
                    default: return spread != null ? spread[0] : 1f;
                }
            }

            static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;

            static float[] ExpandSeasons(float spring, float summer, float fall, float winter)
            {
                return new float[]
                {
                    winter,                             // Jan
                    winter * 0.6f + spring * 0.4f,      // Feb (transition)
                    spring * 0.85f + winter * 0.15f,    // Mar (early spring)
                    spring,                             // Apr
                    spring * 0.6f + summer * 0.4f,      // May (transition)
                    summer,                             // Jun
                    summer,                             // Jul
                    summer * 0.7f + fall * 0.3f,        // Aug (late summer)
                    fall,                               // Sep
                    fall * 0.6f + winter * 0.4f,        // Oct (transition)
                    winter * 0.7f + fall * 0.3f,        // Nov
                    winter,                             // Dec
                };
            }

            static float[] ExpandStress(float fallDieoff, float winterStress)
            {
                return new float[]
                {
                    winterStress,                         // Jan
                    winterStress * 0.8f,                  // Feb
                    0f,                                   // Mar
                    0f,                                   // Apr
                    0f,                                   // May
                    0f,                                   // Jun
                    0f,                                   // Jul
                    0f,                                   // Aug
                    fallDieoff * 0.3f,                    // Sep (early fall)
                    fallDieoff,                           // Oct
                    fallDieoff * 0.5f + winterStress * 0.5f, // Nov (transition)
                    winterStress,                         // Dec
                };
            }
        }

        // --- Monthly spread curves (0=Jan..11=Dec) ---
        // Values are multipliers: 1.0 = base rate, >1 = faster, <1 = slower, 0 = dormant

        // Early bloomers: daffodil (Mar-Apr peak)
        static readonly Profile EarlySpring = new Profile(
            new float[] { 0f, 0.1f, 1.4f, 2.4f, 1.8f, 0.8f, 0.4f, 0.2f, 0.1f, 0f, 0f, 0f },
            new float[] { 0.9f, 0.6f, 0f, 0f, 0f, 0f, 0f, 0f, 0.2f, 0.5f, 0.7f, 0.9f });

        // Classic meadow annuals: cornflower, daisy, poppy (May-Jul peak)
        static readonly Profile MeadowSummer = new Profile(
            new float[] { 0f, 0f, 0.2f, 0.8f, 1.8f, 2.2f, 2.0f, 1.2f, 0.4f, 0.1f, 0f, 0f },
            new float[] { 1.0f, 0.8f, 0f, 0f, 0f, 0f, 0f, 0f, 0.3f, 0.5f, 0.8f, 1.0f });

        // Late summer bloomers: heather, gorse (Jul-Sep peak)
        static readonly Profile LateSummer = new Profile(
            new float[] { 0f, 0f, 0.1f, 0.3f, 0.7f, 1.2f, 2.0f, 2.4f, 1.8f, 0.6f, 0.1f, 0f },
            new float[] { 0.8f, 0.6f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.3f, 0.6f, 0.8f });

        // Fast colonizers: horsetail, mugwort, redtopgrass (Apr-Jun burst, die fast)
        static readonly Profile Colonizer = new Profile(
            new float[] { 0f, 0f, 0.5f, 2.0f, 2.8f, 2.4f, 1.0f, 0.3f, 0.1f, 0f, 0f, 0f },
            new float[] { 1.0f, 0.9f, 0f, 0f, 0f, 0f, 0f, 0.2f, 0.5f, 0.7f, 0.9f, 1.0f });

        // Perennials: catmint, cowparsley, lupine (May-Aug, moderate winter hardiness)
        static readonly Profile MeadowPerennial = new Profile(
            new float[] { 0.05f, 0.1f, 0.4f, 0.9f, 1.5f, 1.4f, 1.2f, 0.9f, 0.5f, 0.2f, 0.05f, 0.02f },
            new float[] { 0.7f, 0.5f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.15f, 0.4f, 0.7f });

        // Hardy grass: tallgrass (broad activity Mar-Oct, survives winter)
        static readonly Profile HardyGrass = new Profile(
            new float[] { 0.15f, 0.2f, 0.6f, 1.0f, 1.4f, 1.5f, 1.5f, 1.3f, 0.9f, 0.5f, 0.2f, 0.15f },
            new float[] { 0.4f, 0.3f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.2f, 0.4f });

        // Forest understory: bluebell, lily-of-valley, ghost pipe (Apr-Jun under canopy, before full leaf-out)
        static readonly Profile ForestSpring = new Profile(
            new float[] { 0f, 0.05f, 0.5f, 1.4f, 1.8f, 1.5f, 0.8f, 0.5f, 0.3f, 0.1f, 0f, 0f },
            new float[] { 0.65f, 0.5f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.15f, 0.4f, 0.6f });

        // Ferns: broad season under shade (May-Sep), fairly hardy
        static readonly Profile FernSeason = new Profile(
            new float[] { 0.05f, 0.1f, 0.3f, 0.7f, 1.3f, 1.5f, 1.5f, 1.3f, 0.9f, 0.4f, 0.1f, 0.05f },
            new float[] { 0.6f, 0.4f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.1f, 0.35f, 0.55f });

        // Edge species: edelweiss, tallfern (Jun-Aug peak, exposed to cold)
        static readonly Profile EdgeSummer = new Profile(
            new float[] { 0.05f, 0.1f, 0.3f, 0.6f, 1.1f, 1.5f, 1.5f, 1.2f, 0.7f, 0.3f, 0.08f, 0.05f },
            new float[] { 0.7f, 0.55f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.15f, 0.45f, 0.7f });

        // Aquatic warm-season: reeds, lily, crowfoot (May-Sep, dormant in cold).
        // Keep seasonality feel, but avoid "fills a whole shoreline in a day" spikes on fast aquatic species.
        static readonly Profile AquaticSeason = new Profile(
            new float[] { 0.08f, 0.12f, 0.3f, 0.6f, 0.95f, 1.15f, 1.15f, 1.05f, 0.8f, 0.4f, 0.18f, 0.08f },
            new float[] { 0.4f, 0.3f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.2f, 0.35f });

        // Trees: Apr-Aug saplings; no winter spread (lake ice / snow footing).
        static readonly Profile TreeSeason = new Profile(
            new float[] { 0f, 0f, 0.3f, 0.8f, 1.2f, 1.3f, 1.2f, 1.0f, 0.6f, 0.2f, 0f, 0f },
            new float[] { 0.5f, 0.35f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.25f, 0.45f });

        // Woad (late spring biennial, Jun-Jul peak)
        static readonly Profile Biennial = new Profile(
            new float[] { 0f, 0f, 0.3f, 0.8f, 1.5f, 2.0f, 2.0f, 1.2f, 0.5f, 0.1f, 0f, 0f },
            new float[] { 0.9f, 0.7f, 0f, 0f, 0f, 0f, 0f, 0f, 0.2f, 0.4f, 0.7f, 0.9f });

        static readonly Profile DefaultProfile = MeadowPerennial;

        static readonly Dictionary<string, Profile> BySpecies = Build();

        static Dictionary<string, Profile> Build()
        {
            return new Dictionary<string, Profile>
            {
                // Early spring
                ["daffodil"] = EarlySpring,
                ["forgetmenot"] = EarlySpring,

                // Classic meadow summer
                ["wilddaisy"] = MeadowSummer,
                ["cornflower"] = MeadowSummer,
                ["goldenpoppy"] = MeadowSummer,
                ["orangemallow"] = MeadowSummer,

                // Late summer
                ["heather"] = LateSummer,
                ["westerngorse"] = LateSummer,

                // Colonizers
                ["mugwort"] = Colonizer,
                ["horsetail"] = Colonizer,
                ["redtopgrass"] = Colonizer,

                // Perennials
                ["cowparsley"] = MeadowPerennial,
                ["catmint"] = MeadowPerennial,
                ["lupine"] = MeadowPerennial,
                ["edelweiss"] = EdgeSummer,

                // Biennial
                ["woad"] = Biennial,

                // Hardy grass
                ["tallgrass"] = HardyGrass,

                // Forest understory (spring ephemeral window)
                ["bluebell"] = ForestSpring,
                ["lilyofthevalley"] = ForestSpring,
                ["ghostpipewhite"] = ForestSpring,
                ["ghostpipepink"] = ForestSpring,
                ["ghostpipered"] = ForestSpring,

                // Ferns
                ["eaglefern"] = FernSeason,
                ["cinnamonfern"] = FernSeason,
                ["deerfern"] = FernSeason,
                ["hartstongue"] = FernSeason,
                ["tallfern"] = EdgeSummer,

                // Aquatic
                ["coopersreed"] = AquaticSeason,
                ["tule"] = AquaticSeason,
                ["papyrus"] = AquaticSeason,
                ["waterlily"] = AquaticSeason,
                ["watercrowfoot"] = AquaticSeason,

                // Berries (meadow summer pattern)
                ["strawberry"] = MeadowSummer,
                ["cloudberry"] = MeadowSummer,
            };
        }

        public static bool TryGet(string species, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(species)) return false;

            if (BySpecies.TryGetValue(species, out profile)) return true;

            if (WildTreeEcology.TryGet(species, out _))
            {
                profile = TreeSeason;
                return true;
            }

            if (WildBerryEcology.TryGet(species, out _))
            {
                profile = MeadowPerennial;
                return true;
            }

            if (WildFernEcology.TryGet(species, out _))
            {
                profile = FernSeason;
                return true;
            }

            return false;
        }

        public static Profile Resolve(string species)
        {
            if (TryGet(species, out Profile profile)) return profile;
            return DefaultProfile;
        }
    }
}
