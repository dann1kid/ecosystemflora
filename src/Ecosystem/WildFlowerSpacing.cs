using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Minimum horizontal distance (Chebyshev blocks) when spreading.</summary>
    internal static class WildFlowerSpacing
    {
        public readonly struct Profile
        {
            /// <summary>Min distance to same species; 0 = patch-forming (touch allowed at 1 block).</summary>
            public readonly int SameSpecies;
            /// <summary>Default min distance to any other flower species.</summary>
            public readonly int OtherSpecies;
            public readonly IReadOnlyDictionary<string, int> FromSpecies;

            public Profile(int sameSpecies, int otherSpecies, IReadOnlyDictionary<string, int> fromSpecies = null)
            {
                SameSpecies = sameSpecies;
                OtherSpecies = otherSpecies;
                FromSpecies = fromSpecies;
            }
        }

        static readonly Dictionary<string, Profile> BySpecies = BuildProfiles();

        static Dictionary<string, Profile> BuildProfiles()
        {
            var map = new Dictionary<string, Profile>
            {
                // Patch colonizers — tight same-species clumps
                ["horsetail"] = new Profile(0, 1),
                ["heather"] = new Profile(0, 1),
                ["westerngorse"] = new Profile(0, 1),
                ["mugwort"] = new Profile(1, 1),
                ["cowparsley"] = new Profile(1, 1),
                ["catmint"] = new Profile(1, 1),
                ["bluebell"] = new Profile(1, 2, new Dictionary<string, int>
                {
                    ["wilddaisy"] = 3,
                    ["catmint"] = 3,
                    ["cornflower"] = 3,
                }),
                ["cornflower"] = new Profile(2, 1),
                ["wilddaisy"] = new Profile(2, 1),
                ["forgetmenot"] = new Profile(2, 1),
                ["woad"] = new Profile(1, 1),
                ["lupine"] = new Profile(0, 1),
                ["lilyofthevalley"] = new Profile(2, 2, new Dictionary<string, int>
                {
                    ["wilddaisy"] = 3,
                    ["horsetail"] = 2,
                }),
                ["daffodil"] = new Profile(3, 1),
                ["ghostpipewhite"] = new Profile(3, 2, new Dictionary<string, int> { ["horsetail"] = 4 }),
                ["ghostpipepink"] = new Profile(3, 2, new Dictionary<string, int> { ["horsetail"] = 4 }),
                ["ghostpipered"] = new Profile(3, 2, new Dictionary<string, int> { ["horsetail"] = 4 }),
                ["orangemallow"] = new Profile(3, 2),
                ["edelweiss"] = new Profile(4, 2, new Dictionary<string, int>
                {
                    ["heather"] = 4,
                    ["horsetail"] = 3,
                }),
                ["goldenpoppy"] = new Profile(4, 2, new Dictionary<string, int>
                {
                    ["heather"] = 3,
                    ["orangemallow"] = 2,
                }),
                ["croton"] = new Profile(1, 2),
                ["rafflesiabrown"] = new Profile(4, 2),
                ["rafflesiared"] = new Profile(4, 2),
            };

            return map;
        }

        public static bool TryGet(string species, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.TryGetValue(species, out profile);
        }
    }
}
