using System;
using System.Collections.Generic;
using System.Globalization;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesEcologyCsvMerge
    {
        public static void ApplyFields(SpeciesEcologyCsvRow row, Dictionary<string, string> fields)
        {
            if (row == null || fields == null || fields.Count == 0) return;

            SetString(row, fields, "taxon", v => row.Taxon = v);
            SetFloat(row, fields, "min_temp", v => row.MinTemp = v);
            SetFloat(row, fields, "max_temp", v => row.MaxTemp = v);
            SetFloat(row, fields, "min_rain", v => row.MinRain = v);
            SetFloat(row, fields, "max_rain", v => row.MaxRain = v);
            SetFloat(row, fields, "min_forest", v => row.MinForest = v);
            SetFloat(row, fields, "max_forest", v => row.MaxForest = v);
            SetFloat(row, fields, "spread_rate", v => row.SpreadRate = v);
            SetString(row, fields, "spread_mode", v => row.SpreadMode = v);
            SetString(row, fields, "mat_connectivity", v => row.MatConnectivity = v);
            SetFloat(row, fields, "seed_dispersal_chance", v => row.SeedDispersalChance = v);
            SetInt(row, fields, "seed_dispersal_radius", v => row.SeedDispersalRadius = v);
            SetInt(row, fields, "mat_spread_radius", v => row.MatSpreadRadius = v);
            SetInt(row, fields, "independent_spread_radius", v => row.IndependentSpreadRadius = v);
            SetInt(row, fields, "spread_radius", v => row.SpreadRadius = v);
            SetInt(row, fields, "same_species_spacing", v => row.SameSpeciesSpacing = v);
            SetInt(row, fields, "other_species_spacing", v => row.OtherSpeciesSpacing = v);
            SetString(row, fields, "spacing_from_species", v => row.SpacingFromSpecies = v);
            SetInt(row, fields, "min_sunlight", v => row.MinSunlight = v);
            SetString(row, fields, "habitat", v => row.Habitat = v);
            SetInt(row, fields, "water_max_depth", v => row.WaterMaxDepth = v);
            SetInt(row, fields, "water_min_depth", v => row.WaterMinDepth = v);
            SetInt(row, fields, "water_vertical_blocks", v => row.WaterVerticalBlocks = v);
            SetInt(row, fields, "water_exact_depth", v => row.WaterExactDepth = v);
            SetString(row, fields, "soil_kinds", v => row.SoilKinds = v);
            SetInt(row, fields, "soil_min_fertility", v => row.SoilMinFertility = v);
            SetInt(row, fields, "soil_max_fertility", v => row.SoilMaxFertility = v);
            SetString(row, fields, "context_affinity", v => row.ContextAffinity = v);
            SetFloat(row, fields, "context_bonus", v => row.ContextBonus = v);
            SetFloat(row, fields, "forest_interior_penalty", v => row.ForestInteriorPenalty = v);
            SetFloat(row, fields, "hold_strength", v => row.HoldStrength = v);
            SetString(row, fields, "moisture", v => row.Moisture = v);
            SetString(row, fields, "light", v => row.Light = v);
            SetFloat(row, fields, "niche_bonus", v => row.NicheBonus = v);
            SetBool(row, fields, "season_explicit", v => row.SeasonExplicit = v);
            SetDouble(row, fields, "flower_maturation_h", v => row.FlowerMaturationHours = v);
            SetDouble(row, fields, "flower_cooldown_h", v => row.FlowerCooldownHours = v);
            SetDouble(row, fields, "fern_maturation_h", v => row.FernMaturationHours = v);
            SetDouble(row, fields, "fern_cooldown_h", v => row.FernCooldownHours = v);
            SetDouble(row, fields, "berry_maturation_h", v => row.BerryMaturationHours = v);
            SetString(row, fields, "tree_seral_role", v => row.TreeSeralRole = v);
            SetString(row, fields, "soil_succession_role", v => row.SoilSuccessionRole = v);
        }

        public static SpeciesEcologyCsvRow Clone(SpeciesEcologyCsvRow source)
        {
            if (source == null) return new SpeciesEcologyCsvRow();

            return new SpeciesEcologyCsvRow
            {
                Species = source.Species,
                Taxon = source.Taxon,
                MinTemp = source.MinTemp,
                MaxTemp = source.MaxTemp,
                MinRain = source.MinRain,
                MaxRain = source.MaxRain,
                MinForest = source.MinForest,
                MaxForest = source.MaxForest,
                SpreadRate = source.SpreadRate,
                SpreadMode = source.SpreadMode,
                MatConnectivity = source.MatConnectivity,
                SeedDispersalChance = source.SeedDispersalChance,
                SeedDispersalRadius = source.SeedDispersalRadius,
                MatSpreadRadius = source.MatSpreadRadius,
                IndependentSpreadRadius = source.IndependentSpreadRadius,
                SpreadRadius = source.SpreadRadius,
                SameSpeciesSpacing = source.SameSpeciesSpacing,
                OtherSpeciesSpacing = source.OtherSpeciesSpacing,
                SpacingFromSpecies = source.SpacingFromSpecies,
                MinSunlight = source.MinSunlight,
                Habitat = source.Habitat,
                WaterMaxDepth = source.WaterMaxDepth,
                WaterMinDepth = source.WaterMinDepth,
                WaterVerticalBlocks = source.WaterVerticalBlocks,
                WaterExactDepth = source.WaterExactDepth,
                SoilKinds = source.SoilKinds,
                SoilMinFertility = source.SoilMinFertility,
                SoilMaxFertility = source.SoilMaxFertility,
                ContextAffinity = source.ContextAffinity,
                ContextBonus = source.ContextBonus,
                ForestInteriorPenalty = source.ForestInteriorPenalty,
                HoldStrength = source.HoldStrength,
                Moisture = source.Moisture,
                Light = source.Light,
                NicheBonus = source.NicheBonus,
                SeasonExplicit = source.SeasonExplicit,
                FlowerMaturationHours = source.FlowerMaturationHours,
                FlowerCooldownHours = source.FlowerCooldownHours,
                FernMaturationHours = source.FernMaturationHours,
                FernCooldownHours = source.FernCooldownHours,
                BerryMaturationHours = source.BerryMaturationHours,
                TreeSeralRole = source.TreeSeralRole,
                SoilSuccessionRole = source.SoilSuccessionRole,
            };
        }

        static void SetString(SpeciesEcologyCsvRow row, Dictionary<string, string> fields, string key, Action<string> assign)
        {
            if (fields.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
            {
                assign(value);
            }
        }

        static void SetFloat(SpeciesEcologyCsvRow row, Dictionary<string, string> fields, string key, Action<float> assign)
        {
            if (fields.TryGetValue(key, out string value)
                && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                assign(parsed);
            }
        }

        static void SetDouble(SpeciesEcologyCsvRow row, Dictionary<string, string> fields, string key, Action<double> assign)
        {
            if (fields.TryGetValue(key, out string value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                assign(parsed);
            }
        }

        static void SetInt(SpeciesEcologyCsvRow row, Dictionary<string, string> fields, string key, Action<int> assign)
        {
            if (fields.TryGetValue(key, out string value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                assign(parsed);
            }
        }

        static void SetBool(SpeciesEcologyCsvRow row, Dictionary<string, string> fields, string key, Action<bool> assign)
        {
            if (!fields.TryGetValue(key, out string value) || string.IsNullOrEmpty(value)) return;

            if (bool.TryParse(value, out bool parsed))
            {
                assign(parsed);
                return;
            }

            if (value == "1") assign(true);
            else if (value == "0") assign(false);
        }
    }
}
