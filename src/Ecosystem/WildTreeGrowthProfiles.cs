using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Species reference size (typical worldgen mature) and calendar senescence horizon.</summary>
    internal static class WildTreeGrowthProfiles
    {
        public readonly struct Profile
        {
            public Profile(int referenceTrunkHeight, int referenceCrownRadius, int senescenceAgeYears, int spreadMaturityAgeYears = 0)
            {
                ReferenceTrunkHeight = referenceTrunkHeight;
                ReferenceCrownRadius = referenceCrownRadius;
                SenescenceAgeYears = senescenceAgeYears;
                SpreadMaturityAgeYears = spreadMaturityAgeYears;
            }

            /// <summary>Typical mature worldgen trunk height — display / growth pacing only, not a cap.</summary>
            public int ReferenceTrunkHeight { get; }

            /// <summary>Typical mature worldgen crown radius — display / growth pacing only, not a cap.</summary>
            public int ReferenceCrownRadius { get; }

            /// <summary>Calendar years at registration when senescence may begin (future death).</summary>
            public int SenescenceAgeYears { get; }

            /// <summary>
            /// Calendar years at registration before the tree may spread offspring (0 = use config fallback).
            /// Intended to approximate real-world age of first effective seed-bearing per species.
            /// </summary>
            public int SpreadMaturityAgeYears { get; }
        }

        static readonly Profile Default = new Profile(12, 4, 80);

        static readonly Dictionary<string, Profile> ByWood = new Dictionary<string, Profile>
        {
            // SpreadMaturityAgeYears is the age where we allow "effective" wild spread to begin.
            // Values chosen from silvics / extension references; see docs/ for tuning rationale.
            ["birch"] = new Profile(12, 4, 75, spreadMaturityAgeYears: 15),
            ["oak"] = new Profile(14, 5, 120, spreadMaturityAgeYears: 25),
            ["maple"] = new Profile(13, 5, 100, spreadMaturityAgeYears: 20),
            ["crimsonkingmaple"] = new Profile(12, 5, 85, spreadMaturityAgeYears: 10),
            ["pine"] = new Profile(16, 3, 110, spreadMaturityAgeYears: 15),
            ["larch"] = new Profile(14, 3, 100, spreadMaturityAgeYears: 15),
            ["redwood"] = new Profile(20, 4, 140, spreadMaturityAgeYears: 20),
            ["baldcypress"] = new Profile(15, 4, 105, spreadMaturityAgeYears: 25),
            ["greenspirecypress"] = new Profile(14, 3, 88, spreadMaturityAgeYears: 10),
            ["acacia"] = new Profile(10, 4, 68, spreadMaturityAgeYears: 5),
            ["kapok"] = new Profile(16, 6, 115, spreadMaturityAgeYears: 6),
            ["ebony"] = new Profile(13, 4, 105, spreadMaturityAgeYears: 8),
            ["purpleheart"] = new Profile(14, 4, 95, spreadMaturityAgeYears: 10),
            ["walnut"] = new Profile(14, 5, 108, spreadMaturityAgeYears: 7),
        };

        public static Profile Resolve(string wood)
        {
            if (string.IsNullOrEmpty(wood)) return Default;
            return ByWood.TryGetValue(wood, out Profile profile) ? profile : Default;
        }
    }
}

