using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Export-only defaults for <c>flower_phenology_life_cycles</c> (dieback entries before senescence).
    /// Runtime prefers the CSV registry; <c>0</c> / missing falls back to <see cref="EcosystemConfig.MaxFlowerPhenologyLifeCycles"/>.
    /// </summary>
    internal static class WildFlowerPhenologyLife
    {
        public const int DefaultPerennial = 4;

        static readonly Dictionary<string, int> BySpecies = new Dictionary<string, int>
        {
            // Short-lived / fragile
            ["cowparsley"] = 2,
            ["ghostpipewhite"] = 2,
            ["ghostpipepink"] = 2,
            ["ghostpipered"] = 2,
            ["rafflesiabrown"] = 2,
            ["rafflesiared"] = 2,
            ["cornflower"] = 3,
            ["forgetmenot"] = 3,
            ["goldenpoppy"] = 3,
            ["woad"] = 3,

            // Typical meadow perennials
            ["catmint"] = 4,
            ["wilddaisy"] = 4,
            ["mugwort"] = 4,
            ["croton"] = 4,
            ["orangemallow"] = 4,
            ["lupine"] = 5,
            ["daffodil"] = 5,
            ["bluebell"] = 5,
            ["edelweiss"] = 5,

            // Clonal / rhizomatous
            ["lilyofthevalley"] = 6,
            ["horsetail"] = 6,
            [EcologyGrassColonizerSpecies.Redtopgrass] = 5,

            // Woody / long-lived heath
            ["westerngorse"] = 7,
            ["heather"] = 8,
        };

        public static bool TryGet(string species, out int lifeCycles)
        {
            lifeCycles = 0;
            if (string.IsNullOrEmpty(species)) return false;
            if (!BySpecies.TryGetValue(species, out lifeCycles) || lifeCycles <= 0) return false;
            return true;
        }

        /// <summary>Value for CSV export; flowers without an override get <see cref="DefaultPerennial"/>.</summary>
        public static int ResolveForExport(string species, string taxon)
        {
            if (TryGet(species, out int cycles)) return cycles;

            if (taxon == "flower" || taxon == "grass_colonizer")
            {
                return DefaultPerennial;
            }

            return 0;
        }
    }
}
