using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Species reference size, crown silhouette, and calendar senescence horizon.</summary>
    internal static class WildTreeGrowthProfiles
    {
        public readonly struct Profile
        {
            public Profile(
                int referenceTrunkHeight,
                int referenceCrownRadius,
                int senescenceAgeYears,
                int spreadMaturityAgeYears = 0,
                TreeCrownForm crownForm = TreeCrownForm.Oval)
            {
                ReferenceTrunkHeight = referenceTrunkHeight;
                ReferenceCrownRadius = referenceCrownRadius;
                SenescenceAgeYears = senescenceAgeYears;
                SpreadMaturityAgeYears = spreadMaturityAgeYears;
                CrownForm = crownForm;
            }

            /// <summary>Typical mature worldgen trunk height — display / growth pacing only, not a cap.</summary>
            public int ReferenceTrunkHeight { get; }

            /// <summary>Typical mature worldgen crown radius — growth pacing soft target (broadleaf oaks aim wider).</summary>
            public int ReferenceCrownRadius { get; }

            /// <summary>Calendar years at registration when senescence may begin (future death).</summary>
            public int SenescenceAgeYears { get; }

            /// <summary>
            /// Calendar years at registration before the tree may spread offspring (0 = use config fallback).
            /// Intended to approximate real-world age of first effective seed-bearing per species.
            /// </summary>
            public int SpreadMaturityAgeYears { get; }

            /// <summary>Yearly crown growth silhouette (spreading oak vs oval birch vs umbrella acacia…).</summary>
            public TreeCrownForm CrownForm { get; }
        }

        static readonly Profile Default = new Profile(12, 4, 80, crownForm: TreeCrownForm.Oval);

        static readonly Dictionary<string, Profile> ByWood = new Dictionary<string, Profile>
        {
            // SpreadMaturityAgeYears is the age where we allow "effective" wild spread to begin.
            // CrownForm drives yearly foliage envelope — see TreeCrownEnvelope.
            ["birch"] = new Profile(12, 4, 75, spreadMaturityAgeYears: 15, crownForm: TreeCrownForm.Oval),
            ["oak"] = new Profile(14, 7, 120, spreadMaturityAgeYears: 25, crownForm: TreeCrownForm.Spreading),
            ["maple"] = new Profile(13, 6, 100, spreadMaturityAgeYears: 20, crownForm: TreeCrownForm.Spreading),
            ["crimsonkingmaple"] = new Profile(12, 6, 85, spreadMaturityAgeYears: 10, crownForm: TreeCrownForm.Spreading),
            ["pine"] = new Profile(16, 3, 110, spreadMaturityAgeYears: 15, crownForm: TreeCrownForm.Column),
            ["larch"] = new Profile(14, 3, 100, spreadMaturityAgeYears: 15, crownForm: TreeCrownForm.Column),
            ["redwood"] = new Profile(20, 8, 1000, spreadMaturityAgeYears: 20, crownForm: TreeCrownForm.Tiered),
            ["baldcypress"] = new Profile(15, 4, 105, spreadMaturityAgeYears: 25, crownForm: TreeCrownForm.Tiered),
            ["greenspirecypress"] = new Profile(14, 3, 88, spreadMaturityAgeYears: 10, crownForm: TreeCrownForm.Column),
            ["acacia"] = new Profile(10, 5, 68, spreadMaturityAgeYears: 5, crownForm: TreeCrownForm.Umbrella),
            ["kapok"] = new Profile(16, 6, 115, spreadMaturityAgeYears: 6, crownForm: TreeCrownForm.Umbrella),
            ["ebony"] = new Profile(13, 4, 105, spreadMaturityAgeYears: 8, crownForm: TreeCrownForm.Oval),
            ["purpleheart"] = new Profile(14, 4, 95, spreadMaturityAgeYears: 10, crownForm: TreeCrownForm.Oval),
            ["walnut"] = new Profile(14, 6, 108, spreadMaturityAgeYears: 7, crownForm: TreeCrownForm.Spreading),
        };

        public static Profile Resolve(string wood)
        {
            if (string.IsNullOrEmpty(wood)) return Default;
            return ByWood.TryGetValue(wood, out Profile profile) ? profile : Default;
        }
    }
}
