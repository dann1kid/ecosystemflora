using System.Collections.Generic;

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

            // Trees — forest soils, not clay/sand/barren
            ["birch"] = new Profile(SoilKindSets.ForestUnderstory | SoilKind.HighFert | SoilKind.MediumFert, 100, 0),
            ["oak"] = new Profile(SoilKindSets.ForestUnderstory | SoilKind.HighFert | SoilKind.MediumFert, 100, 0),
            ["maple"] = new Profile(SoilKindSets.ForestUnderstory | SoilKind.HighFert | SoilKind.MediumFert, 100, 0),
            ["pine"] = new Profile(SoilKindSets.ForestUnderstory | SoilKind.LowFert | SoilKind.MediumFert, 80, 0),
            ["larch"] = new Profile(SoilKind.LowFert | SoilKind.MediumFert | SoilKind.ForestFloor | SoilKind.Gravel, 80, 220),
            ["acacia"] = new Profile(SoilKind.LowFert | SoilKind.Sand | SoilKind.MediumFert, 80, 200),
            ["kapok"] = new Profile(SoilKindSets.ForestUnderstory | SoilKind.HighFert, 120, 0),
            ["crimsonkingmaple"] = new Profile(SoilKindSets.Meadow, 100, 0),
            ["redwood"] = new Profile(SoilKindSets.ForestUnderstory | SoilKind.HighFert, 120, 0),
            ["baldcypress"] = new Profile(SoilKind.LowFert | SoilKind.MediumFert | SoilKind.Gravel | SoilKind.Peat, 80, 0),
            ["greenspirecypress"] = new Profile(SoilKindSets.Meadow, 100, 0),
            ["ebony"] = new Profile(SoilKind.MediumFert | SoilKind.HighFert | SoilKind.LowFert, 120, 0),
            ["purpleheart"] = new Profile(SoilKind.MediumFert | SoilKind.HighFert | SoilKind.LowFert, 120, 0),
            ["walnut"] = new Profile(SoilKindSets.Meadow, 100, 0),
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
                else
                {
                    profile = DefaultMeadow;
                }
            }

            if (req.AllowedSoilKinds == SoilKind.None) req.AllowedSoilKinds = profile.Allowed;
            if (req.MinGroundFertility <= 0 && profile.MinBlockFertility > 0) req.MinGroundFertility = profile.MinBlockFertility;
            if (req.MaxGroundFertility <= 0 && profile.MaxBlockFertility > 0) req.MaxGroundFertility = profile.MaxBlockFertility;

            // Legacy MinFertility aligns with soil floor when unset on block attrs.
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
