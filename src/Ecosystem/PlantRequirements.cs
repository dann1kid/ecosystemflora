using System.Collections.Generic;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    public class PlantRequirements
    {
        public string Species { get; set; }

        public EcologyHabitat Habitat { get; set; } = EcologyHabitat.Terrestrial;

        public int MaxWaterDepth { get; set; } = 1;

        public int MinWaterDepth { get; set; }

        public int VerticalBlocks { get; set; } = 1;

        /// <summary>If &gt;= 0, in-water reeds need exactly this many water blocks above muddy gravel.</summary>
        public int ExactWaterDepth { get; set; } = -1;

        public float MinTemp { get; set; } = -5f;
        public float MaxTemp { get; set; } = 50f;
        public float MinRain { get; set; } = 0f;
        public float MaxRain { get; set; } = 1f;
        public float MinForest { get; set; } = 0f;
        public float MaxForest { get; set; } = 1f;
        public int MinFertility { get; set; } = 100;
        public int MinReplaceable { get; set; } = 9500;

        /// <summary>Relative spread vigor (1 = config baseline).</summary>
        public float SpreadRate { get; set; } = 1f;

        /// <summary>Min horizontal blocks to same species (0 = allow adjacent clumps).</summary>
        public int SameSpeciesSpacing { get; set; }

        /// <summary>Default min horizontal blocks to other flower species.</summary>
        public int OtherSpeciesSpacing { get; set; }

        /// <summary>Per-other-species minimum distance overrides.</summary>
        public Dictionary<string, int> SpacingFromSpecies { get; set; }

        public int GetRequiredSpacingTo(string otherSpecies, EcosystemConfig cfg)
        {
            if (string.IsNullOrEmpty(otherSpecies)) return 0;

            int required;
            if (otherSpecies == Species)
            {
                // 0 = patch-forming species allows tight clumps (colonizers).
                return SameSpeciesSpacing;
            }

            if (SpacingFromSpecies != null && SpacingFromSpecies.TryGetValue(otherSpecies, out int specific))
            {
                return specific;
            }

            required = OtherSpeciesSpacing;
            if (required <= 0 && cfg != null) required = cfg.DefaultOtherSpeciesSpacing;

            return required;
        }

        public int GetSpacingSearchRadius(EcosystemConfig cfg)
        {
            if (cfg == null || !cfg.PlantSpacingEnabled) return 0;

            int max = SameSpeciesSpacing;
            int otherBase = OtherSpeciesSpacing > 0 ? OtherSpeciesSpacing : cfg.DefaultOtherSpeciesSpacing;
            if (otherBase > max) max = otherBase;

            if (SpacingFromSpecies != null)
            {
                foreach (KeyValuePair<string, int> pair in SpacingFromSpecies)
                {
                    if (pair.Value > max) max = pair.Value;
                }
            }

            return max;
        }

        public static PlantRequirements FromBlock(Block block)
        {
            if (block == null) return new PlantRequirements();

            var attrs = block.Attributes;
            float minTemp = attrs != null ? attrs["minTemp"].AsFloat(float.NaN) : float.NaN;
            float maxTemp = attrs != null ? attrs["maxTemp"].AsFloat(float.NaN) : float.NaN;
            float minRain = attrs != null ? attrs["minRain"].AsFloat(float.NaN) : float.NaN;
            float maxRain = attrs != null ? attrs["maxRain"].AsFloat(float.NaN) : float.NaN;
            float minForest = attrs != null ? attrs["minForest"].AsFloat(float.NaN) : float.NaN;
            float maxForest = attrs != null ? attrs["maxForest"].AsFloat(float.NaN) : float.NaN;
            float spreadRate = attrs != null ? attrs["ecologySpreadRate"].AsFloat(float.NaN) : float.NaN;
            string species = PlantCodeHelper.GetEcologySpecies(block.Code);
            EcologyHabitat habitat = EcologyHabitat.Terrestrial;
            int maxWaterDepth = 0;
            int minWaterDepth = 0;
            int verticalBlocks = 1;
            int exactWaterDepth = -1;
            int sameSpacing = -1;
            int otherSpacing = -1;
            Dictionary<string, int> spacingFrom = null;
            int minFertility = attrs != null ? attrs["minFertility"].AsInt(100) : 100;

            if (!string.IsNullOrEmpty(species) && WildAquaticEcology.TryGet(species, out WildAquaticEcology.Profile aquatic))
            {
                habitat = aquatic.Habitat;
                maxWaterDepth = aquatic.MaxWaterDepth;
                minWaterDepth = aquatic.MinWaterDepth;
                verticalBlocks = aquatic.VerticalBlocks;
                exactWaterDepth = aquatic.ExactWaterDepth;
                if (float.IsNaN(minTemp)) minTemp = aquatic.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = aquatic.MaxTemp;
                if (float.IsNaN(minRain)) minRain = aquatic.MinRain;
                if (float.IsNaN(maxRain)) maxRain = aquatic.MaxRain;
                if (float.IsNaN(spreadRate)) spreadRate = aquatic.SpreadRate;
                minForest = 0f;
                maxForest = 1f;
                minFertility = 0;
                sameSpacing = aquatic.SameSpeciesSpacing;
                otherSpacing = aquatic.OtherSpeciesSpacing;
            }
            else if (!string.IsNullOrEmpty(species) && WildFlowerClimate.TryGet(species, out WildFlowerClimate.EcologyEntry ecology))
            {
                if (float.IsNaN(minTemp)) minTemp = ecology.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = ecology.MaxTemp;
                if (float.IsNaN(minRain)) minRain = ecology.MinRain;
                if (float.IsNaN(maxRain)) maxRain = ecology.MaxRain;
                if (float.IsNaN(minForest)) minForest = ecology.MinForest;
                if (float.IsNaN(maxForest)) maxForest = ecology.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = ecology.SpreadRate;

                if (WildFlowerSpacing.TryGet(species, out WildFlowerSpacing.Profile spacing))
                {
                    sameSpacing = spacing.SameSpecies;
                    otherSpacing = spacing.OtherSpecies;
                    if (spacing.FromSpecies != null && spacing.FromSpecies.Count > 0)
                    {
                        spacingFrom = new Dictionary<string, int>(spacing.FromSpecies);
                    }
                }
            }

            if (attrs != null)
            {
                int attrSame = attrs["ecologySameSpeciesSpacing"].AsInt(-1);
                if (attrSame >= 0) sameSpacing = attrSame;
                int attrOther = attrs["ecologyOtherSpeciesSpacing"].AsInt(-1);
                if (attrOther >= 0) otherSpacing = attrOther;
            }

            if (float.IsNaN(minTemp)) minTemp = 10f;
            if (float.IsNaN(maxTemp)) maxTemp = 22f;
            if (float.IsNaN(minRain)) minRain = 0f;
            if (float.IsNaN(maxRain)) maxRain = 1f;
            if (float.IsNaN(minForest)) minForest = 0f;
            if (float.IsNaN(maxForest)) maxForest = 1f;
            if (float.IsNaN(spreadRate)) spreadRate = 1f;

            return new PlantRequirements
            {
                Species = species,
                Habitat = habitat,
                MaxWaterDepth = maxWaterDepth > 0 ? maxWaterDepth : 1,
                MinWaterDepth = minWaterDepth,
                VerticalBlocks = verticalBlocks > 0 ? verticalBlocks : 1,
                ExactWaterDepth = exactWaterDepth,
                MinTemp = minTemp,
                MaxTemp = maxTemp,
                MinRain = minRain,
                MaxRain = maxRain,
                MinForest = minForest,
                MaxForest = maxForest,
                SpreadRate = spreadRate,
                SameSpeciesSpacing = sameSpacing < 0 ? 0 : sameSpacing,
                OtherSpeciesSpacing = otherSpacing < 0 ? 0 : otherSpacing,
                SpacingFromSpecies = spacingFrom,
                MinFertility = minFertility,
                MinReplaceable = attrs != null ? attrs["minReplaceable"].AsInt(9500) : 9500,
            };
        }
    }
}
