using System.Collections.Generic;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    /// <summary>Per-species soil preferences (block fertility + soil categories).</summary>
    internal static class WildPlantSoil
    {
        public readonly struct Profile
        {
            public readonly SoilKind Allowed;
            public readonly int MinBlockFertility;
            public readonly int MaxBlockFertility;

            public Profile(SoilKind allowed, int minBlockFertility = 0, int maxBlockFertility = 0)
            {
                Allowed = allowed;
                MinBlockFertility = minBlockFertility;
                MaxBlockFertility = maxBlockFertility;
            }
        }

        static readonly Profile DefaultMeadow = new Profile(SoilKindSets.Meadow, 100, 0);

        /// <summary>Vanilla tree seed on meadow soil (high / medium / low fertility).</summary>
        static readonly Profile VanillaOpenFieldTree =
            new Profile(SoilKind.HighFert | SoilKind.MediumFert | SoilKind.LowFert, 80, 0);

        /// <summary>Includes forest floor and peat; still allows open meadow soils.</summary>
        static readonly Profile VanillaForestTree =
            new Profile(SoilKindSets.Meadow, 80, 0);

        static readonly Dictionary<string, Profile> BySpecies = new Dictionary<string, Profile>
        {
            // Flowers — worldgen maxFertility caps
            ["lupine"] = new Profile(SoilKind.LowFert | SoilKind.MediumFert | SoilKind.Sand, 100, 165),
            ["woad"] = new Profile(SoilKindSets.Meadow | SoilKind.LowFert, 100, 200),
            ["cowparsley"] = new Profile(SoilKindSets.Meadow, 100, 200),
            ["cornflower"] = new Profile(SoilKindSets.Meadow, 100, 180),
            ["catmint"] = new Profile(SoilKindSets.Meadow, 100, 180),
            ["forgetmenot"] = new Profile(SoilKindSets.Meadow, 100, 180),
            ["wilddaisy"] = new Profile(SoilKindSets.Meadow, 100, 180),
            ["bluebell"] = new Profile(SoilKindSets.ForestUnderstory | SoilKind.HighFert, 100, 0),
            ["lilyofthevalley"] = new Profile(SoilKindSets.ForestUnderstory | SoilKind.HighFert, 120, 0),
            ["horsetail"] = new Profile(SoilKindSets.Poor | SoilKind.MediumFert | SoilKind.Gravel, 80, 220),
            ["heather"] = new Profile(SoilKindSets.Poor | SoilKind.LowFert | SoilKind.Gravel, 80, 200),
            ["westerngorse"] = new Profile(SoilKindSets.Poor | SoilKind.LowFert, 80, 200),
            ["mugwort"] = new Profile(SoilKindSets.Meadow, 100, 0),
            ["daffodil"] = new Profile(SoilKindSets.Meadow, 100, 0),
            ["ghostpipewhite"] = new Profile(SoilKindSets.ForestUnderstory, 100, 220),
            ["ghostpipepink"] = new Profile(SoilKindSets.ForestUnderstory, 100, 220),
            ["ghostpipered"] = new Profile(SoilKindSets.ForestUnderstory, 100, 220),
            ["orangemallow"] = new Profile(SoilKind.LowFert | SoilKind.Sand | SoilKind.MediumFert, 80, 150),
            ["edelweiss"] = new Profile(SoilKind.LowFert | SoilKind.Gravel | SoilKind.MediumFert | SoilKind.ForestFloor, 80, 200),
            ["goldenpoppy"] = new Profile(SoilKind.LowFert | SoilKind.Sand | SoilKind.MediumFert, 80, 180),
            ["croton"] = new Profile(SoilKindSets.ForestUnderstory | SoilKind.MediumFert | SoilKind.HighFert, 80, 0),
            ["rafflesiabrown"] = new Profile(SoilKind.LowFert | SoilKind.MediumFert | SoilKind.ForestFloor, 80, 200),
            ["rafflesiared"] = new Profile(SoilKind.LowFert | SoilKind.MediumFert | SoilKind.ForestFloor, 80, 200),
            ["tallgrass"] = new Profile(SoilKindSets.Meadow | SoilKind.LowFert | SoilKind.ForestFloor, 80, 0),

            // Trees — niche footing (vanilla: any soil; wild spread excludes barren/clay unless noted).
            ["birch"] = VanillaOpenFieldTree,
            ["oak"] = VanillaOpenFieldTree,
            ["maple"] = VanillaOpenFieldTree,
            ["crimsonkingmaple"] = VanillaOpenFieldTree,
            ["walnut"] = new Profile(SoilKind.HighFert | SoilKind.MediumFert | SoilKind.LowFert, 90, 0),
            ["greenspirecypress"] = VanillaOpenFieldTree,
            ["acacia"] = new Profile(SoilKind.LowFert | SoilKind.Sand | SoilKind.MediumFert, 80, 200),
            ["baldcypress"] = new Profile(SoilKind.LowFert | SoilKind.MediumFert | SoilKind.Gravel | SoilKind.Peat, 80, 0),
            ["pine"] = new Profile(
                SoilKind.LowFert | SoilKind.MediumFert | SoilKind.ForestFloor | SoilKind.Peat, 80, 0),
            ["larch"] = new Profile(
                SoilKind.LowFert | SoilKind.MediumFert | SoilKind.ForestFloor | SoilKind.Gravel, 80, 0),
            ["kapok"] = new Profile(
                SoilKind.HighFert | SoilKind.MediumFert | SoilKind.LowFert | SoilKind.ForestFloor | SoilKind.Peat, 90, 0),
            ["redwood"] = new Profile(
                SoilKind.HighFert | SoilKind.MediumFert | SoilKind.ForestFloor | SoilKind.Peat, 90, 0),
            ["ebony"] = new Profile(
                SoilKind.MediumFert | SoilKind.HighFert | SoilKind.LowFert | SoilKind.ForestFloor, 90, 0),
            ["purpleheart"] = new Profile(
                SoilKind.MediumFert | SoilKind.HighFert | SoilKind.LowFert | SoilKind.ForestFloor, 90, 0),
        };

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || string.IsNullOrEmpty(req.Species)) return;

            if (req.Habitat == EcologyHabitat.ReedNearWater
                || req.Habitat == EcologyHabitat.WaterSurface
                || req.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                return;
            }

            Profile profile;
            if (!BySpecies.TryGetValue(req.Species, out profile))
            {
                if (WildBerryEcology.TryGet(req.Species, out WildBerryEcology.Profile berry))
                {
                    profile = berry.Soil;
                }
                else if (WildFernEcology.TryGet(req.Species, out WildFernEcology.EcologyEntry fern))
                {
                    profile = fern.Soil;
                }
                else if (WildTallgrassEcology.TryGet(req.Species, out WildTallgrassEcology.EcologyEntry grass))
                {
                    profile = grass.Soil;
                }
                else
                {
                    profile = DefaultMeadow;
                }
            }

            if (req.AllowedSoilKinds == SoilKind.None) req.AllowedSoilKinds = profile.Allowed;
            if (req.MinGroundFertility <= 0 && profile.MinBlockFertility > 0) req.MinGroundFertility = profile.MinBlockFertility;
            if (req.MaxGroundFertility <= 0 && profile.MaxBlockFertility > 0) req.MaxGroundFertility = profile.MaxBlockFertility;

            if (req.MinGroundFertility > 0 && req.MinFertility < req.MinGroundFertility)
            {
                req.MinFertility = req.MinGroundFertility;
            }
        }

        public static bool TryGet(string species, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.TryGetValue(species, out profile);
        }
    }
}
