using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class PlantRequirementsRegistryBuild
    {
        public static PlantRequirements Build(
            Block block,
            string species,
            SpeciesEcologyCsvRow row,
            JsonObject attrs,
            float minTemp,
            float maxTemp,
            float minRain,
            float maxRain,
            float minForest,
            float maxForest,
            float spreadRate,
            int minFertility,
            int minGroundFertility,
            int maxGroundFertility,
            int minSunlight,
            SpreadMode spreadMode,
            bool suppressRhizomeSpread,
            bool suppressSurfaceMatSpread,
            float seedDispersalChance,
            int seedDispersalRadius,
            int spreadRadius)
        {
            var requirements = new PlantRequirements
            {
                Species = species,
                MinFertility = minFertility,
                MinReplaceable = attrs != null ? attrs["minReplaceable"].AsInt(9500) : 9500,
                SuppressRhizomeSpread = suppressRhizomeSpread,
                SuppressSurfaceMatSpread = suppressSurfaceMatSpread,
            };

            SpeciesEcologyApplier.Apply(requirements, row);
            ApplyTaxonDefaults(requirements, row);
            ApplyBlockOverrides(
                requirements,
                attrs,
                minTemp, maxTemp, minRain, maxRain, minForest, maxForest, spreadRate,
                minGroundFertility, maxGroundFertility, minSunlight,
                spreadMode, seedDispersalChance, seedDispersalRadius, spreadRadius);

            ApplySpreadPolicies(requirements, row);
            return requirements;
        }

        static void ApplyTaxonDefaults(PlantRequirements req, SpeciesEcologyCsvRow row)
        {
            if (req == null || row == null) return;

            if (string.IsNullOrEmpty(row.Habitat))
            {
                req.Habitat = InferHabitat(row.Taxon);
            }

            switch (row.Taxon)
            {
                case "aquatic":
                    req.MinForest = 0f;
                    req.MaxForest = 1f;
                    req.MinFertility = 0;
                    break;
                case "vine":
                    req.MinForest = 0f;
                    req.MaxForest = 1f;
                    if (req.MinSunlight <= 0) req.MinSunlight = 6;
                    break;
                case "ferntree":
                    if (req.MinSunlight <= 0) req.MinSunlight = 10;
                    break;
            }
        }

        static EcologyHabitat InferHabitat(string taxon)
        {
            switch (taxon)
            {
                case "tree": return EcologyHabitat.TerrestrialTree;
                case "ferntree": return EcologyHabitat.Ferntree;
                case "vine": return EcologyHabitat.WildVine;
                default: return EcologyHabitat.Terrestrial;
            }
        }

        static void ApplyBlockOverrides(
            PlantRequirements req,
            JsonObject attrs,
            float minTemp,
            float maxTemp,
            float minRain,
            float maxRain,
            float minForest,
            float maxForest,
            float spreadRate,
            int minGroundFertility,
            int maxGroundFertility,
            int minSunlight,
            SpreadMode spreadMode,
            float seedDispersalChance,
            int seedDispersalRadius,
            int spreadRadius)
        {
            if (!float.IsNaN(minTemp)) req.MinTemp = minTemp;
            if (!float.IsNaN(maxTemp)) req.MaxTemp = maxTemp;
            if (!float.IsNaN(minRain)) req.MinRain = minRain;
            if (!float.IsNaN(maxRain)) req.MaxRain = maxRain;
            if (!float.IsNaN(minForest)) req.MinForest = minForest;
            if (!float.IsNaN(maxForest)) req.MaxForest = maxForest;
            if (!float.IsNaN(spreadRate)) req.SpreadRate = spreadRate;

            if (minGroundFertility > 0) req.MinGroundFertility = minGroundFertility;
            if (maxGroundFertility > 0) req.MaxGroundFertility = maxGroundFertility;
            if (minSunlight > 0) req.MinSunlight = minSunlight;

            if (spreadMode != SpreadMode.Independent) req.SpreadMode = spreadMode;
            if (seedDispersalChance > 0f) req.SeedDispersalChance = seedDispersalChance;
            if (seedDispersalRadius > 0) req.SeedDispersalRadius = seedDispersalRadius;
            if (spreadRadius > 0) req.SpreadRadius = spreadRadius;

            if (attrs != null)
            {
                int attrSame = attrs["ecologySameSpeciesSpacing"].AsInt(-1);
                if (attrSame >= 0) req.SameSpeciesSpacing = attrSame;
                int attrOther = attrs["ecologyOtherSpeciesSpacing"].AsInt(-1);
                if (attrOther >= 0) req.OtherSpeciesSpacing = attrOther;
            }
        }

        static void ApplySpreadPolicies(PlantRequirements req, SpeciesEcologyCsvRow row)
        {
            RhizomeSpread.ApplyTo(req);
            SurfaceMatSpread.ApplyTo(req);
            FernRhizomeSpread.ApplyTo(req);
            BerryColonySpread.ApplyTo(req);
            ShoreSedgeMatSpread.ApplyTo(req);
        }
    }
}
