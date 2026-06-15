using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Species targets for annual wild tree maturation (grown blocks only).</summary>
    internal static class WildTreeGrowthProfiles
    {
        public readonly struct Profile
        {
            public Profile(int maxAgeYears, int maxTrunkHeight, int maxCrownRadius)
            {
                MaxAgeYears = maxAgeYears;
                MaxTrunkHeight = maxTrunkHeight;
                MaxCrownRadius = maxCrownRadius;
            }

            public int MaxAgeYears { get; }
            public int MaxTrunkHeight { get; }
            public int MaxCrownRadius { get; }
        }

        static readonly Profile Default = new Profile(80, 22, 5);

        static readonly Dictionary<string, Profile> ByWood = new Dictionary<string, Profile>
        {
            ["oak"] = new Profile(120, 34, 8),
            ["birch"] = new Profile(90, 26, 6),
            ["maple"] = new Profile(100, 28, 7),
            ["crimsonkingmaple"] = new Profile(90, 24, 7),
            ["pine"] = new Profile(110, 38, 5),
            ["larch"] = new Profile(100, 32, 5),
            ["redwood"] = new Profile(140, 48, 6),
            ["baldcypress"] = new Profile(110, 36, 6),
            ["greenspirecypress"] = new Profile(90, 30, 4),
            ["acacia"] = new Profile(80, 22, 7),
            ["kapok"] = new Profile(110, 40, 9),
            ["ebony"] = new Profile(100, 28, 7),
            ["purpleheart"] = new Profile(100, 30, 7),
            ["walnut"] = new Profile(110, 30, 7),
        };

        public static Profile Resolve(string wood)
        {
            if (string.IsNullOrEmpty(wood)) return Default;
            return ByWood.TryGetValue(wood, out Profile profile) ? profile : Default;
        }
    }
}
