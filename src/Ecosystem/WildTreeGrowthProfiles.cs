using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Species size targets for wild tree maturation (grown blocks only).</summary>
    internal static class WildTreeGrowthProfiles
    {
        public readonly struct Profile
        {
            public Profile(int maxTrunkHeight, int maxCrownRadius)
            {
                MaxTrunkHeight = maxTrunkHeight;
                MaxCrownRadius = maxCrownRadius;
            }

            public int MaxTrunkHeight { get; }
            public int MaxCrownRadius { get; }
        }

        static readonly Profile Default = new Profile(22, 5);

        static readonly Dictionary<string, Profile> ByWood = new Dictionary<string, Profile>
        {
            ["oak"] = new Profile(34, 8),
            ["birch"] = new Profile(26, 6),
            ["maple"] = new Profile(28, 7),
            ["crimsonkingmaple"] = new Profile(24, 7),
            ["pine"] = new Profile(38, 5),
            ["larch"] = new Profile(32, 5),
            ["redwood"] = new Profile(48, 6),
            ["baldcypress"] = new Profile(36, 6),
            ["greenspirecypress"] = new Profile(30, 4),
            ["acacia"] = new Profile(22, 7),
            ["kapok"] = new Profile(40, 9),
            ["ebony"] = new Profile(28, 7),
            ["purpleheart"] = new Profile(30, 7),
            ["walnut"] = new Profile(30, 7),
        };

        public static Profile Resolve(string wood)
        {
            if (string.IsNullOrEmpty(wood)) return Default;
            return ByWood.TryGetValue(wood, out Profile profile) ? profile : Default;
        }
    }
}
