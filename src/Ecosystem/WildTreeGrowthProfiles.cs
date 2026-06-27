using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Species reference size (typical worldgen mature) and calendar senescence horizon.</summary>
    internal static class WildTreeGrowthProfiles
    {
        public readonly struct Profile
        {
            public Profile(int referenceTrunkHeight, int referenceCrownRadius, int senescenceAgeYears)
            {
                ReferenceTrunkHeight = referenceTrunkHeight;
                ReferenceCrownRadius = referenceCrownRadius;
                SenescenceAgeYears = senescenceAgeYears;
            }

            /// <summary>Typical mature worldgen trunk height — display / growth pacing only, not a cap.</summary>
            public int ReferenceTrunkHeight { get; }

            /// <summary>Typical mature worldgen crown radius — display / growth pacing only, not a cap.</summary>
            public int ReferenceCrownRadius { get; }

            /// <summary>Calendar years at registration when senescence may begin (future death).</summary>
            public int SenescenceAgeYears { get; }
        }

        static readonly Profile Default = new Profile(12, 4, 80);

        static readonly Dictionary<string, Profile> ByWood = new Dictionary<string, Profile>
        {
            ["birch"] = new Profile(12, 4, 75),
            ["oak"] = new Profile(14, 5, 120),
            ["maple"] = new Profile(13, 5, 100),
            ["crimsonkingmaple"] = new Profile(12, 5, 85),
            ["pine"] = new Profile(16, 3, 110),
            ["larch"] = new Profile(14, 3, 100),
            ["redwood"] = new Profile(20, 4, 140),
            ["baldcypress"] = new Profile(15, 4, 105),
            ["greenspirecypress"] = new Profile(14, 3, 88),
            ["acacia"] = new Profile(10, 4, 68),
            ["kapok"] = new Profile(16, 6, 115),
            ["ebony"] = new Profile(13, 4, 105),
            ["purpleheart"] = new Profile(14, 4, 95),
            ["walnut"] = new Profile(14, 5, 108),
        };

        public static Profile Resolve(string wood)
        {
            if (string.IsNullOrEmpty(wood)) return Default;
            return ByWood.TryGetValue(wood, out Profile profile) ? profile : Default;
        }
    }
}
