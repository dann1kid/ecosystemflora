using System;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesEcologyApplier
    {
        public static void Apply(PlantRequirements req, SpeciesEcologyCsvRow row)
        {
            if (req == null || row == null) return;

            req.MinTemp = row.MinTemp;
            req.MaxTemp = row.MaxTemp;
            req.MinRain = row.MinRain;
            req.MaxRain = row.MaxRain;
            req.MinForest = row.MinForest;
            req.MaxForest = row.MaxForest;
            req.SpreadRate = row.SpreadRate;
            req.SameSpeciesSpacing = row.SameSpeciesSpacing;
            req.OtherSpeciesSpacing = row.OtherSpeciesSpacing;

            if (!string.IsNullOrEmpty(row.SpacingFromSpecies))
            {
                req.SpacingFromSpecies = SpacingFromSpeciesCodec.Parse(row.SpacingFromSpecies);
            }

            if (row.MinSunlight > 0)
            {
                req.MinSunlight = row.MinSunlight;
            }

            if (!string.IsNullOrEmpty(row.Habitat)
                && Enum.TryParse(row.Habitat, ignoreCase: true, out EcologyHabitat habitat))
            {
                req.Habitat = habitat;
            }

            if (row.WaterMaxDepth > 0)
            {
                req.MaxWaterDepth = row.WaterMaxDepth;
            }

            req.MinWaterDepth = row.WaterMinDepth;
            if (row.WaterVerticalBlocks > 0)
            {
                req.VerticalBlocks = row.WaterVerticalBlocks;
            }

            if (row.WaterExactDepth >= 0)
            {
                req.ExactWaterDepth = row.WaterExactDepth;
            }

            if (!string.IsNullOrEmpty(row.SoilKinds))
            {
                req.AllowedSoilKinds = SoilKindParser.Parse(row.SoilKinds);
            }

            if (row.SoilMinFertility > 0)
            {
                req.MinGroundFertility = row.SoilMinFertility;
            }

            if (row.SoilMaxFertility > 0)
            {
                req.MaxGroundFertility = row.SoilMaxFertility;
            }

            if (!string.IsNullOrEmpty(row.ContextAffinity)
                && Enum.TryParse(row.ContextAffinity, ignoreCase: true, out FloraContextAffinity affinity))
            {
                req.ContextAffinity = affinity;
            }

            if (row.ContextBonus > 0f)
            {
                req.ContextBonus = row.ContextBonus;
            }

            if (row.ForestInteriorPenalty > 0f)
            {
                req.ForestInteriorPenalty = row.ForestInteriorPenalty;
            }

            if (row.HoldStrength > 0f)
            {
                req.HoldStrength = row.HoldStrength;
            }

            if (!string.IsNullOrEmpty(row.Moisture)
                && Enum.TryParse(row.Moisture, ignoreCase: true, out MoistureLevel moisture))
            {
                req.HasNicheProfile = true;
                req.PreferredMoisture = moisture;
            }

            if (!string.IsNullOrEmpty(row.Light)
                && Enum.TryParse(row.Light, ignoreCase: true, out LightLevel light))
            {
                req.HasNicheProfile = true;
                req.PreferredLight = light;
            }

            if (row.NicheBonus > 0f)
            {
                req.NicheBonus = row.NicheBonus;
            }

            if (!string.IsNullOrEmpty(row.SpreadMode)
                && Enum.TryParse(row.SpreadMode, ignoreCase: true, out SpreadMode spreadMode))
            {
                req.SpreadMode = spreadMode;
            }

            if (row.SeedDispersalChance > 0f)
            {
                req.SeedDispersalChance = row.SeedDispersalChance;
            }

            if (row.SeedDispersalRadius > 0)
            {
                req.SeedDispersalRadius = row.SeedDispersalRadius;
            }

            if (row.SpreadRadius > 0)
            {
                req.SpreadRadius = row.SpreadRadius;
            }
            else if (row.MatSpreadRadius > 0)
            {
                req.SpreadRadius = row.MatSpreadRadius;
            }
            else if (row.IndependentSpreadRadius > 0)
            {
                req.SpreadRadius = row.IndependentSpreadRadius;
            }

            TreeSpacingDefaults.EnsureOn(req);
        }
    }
}
