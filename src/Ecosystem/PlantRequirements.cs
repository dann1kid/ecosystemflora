using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    public class PlantRequirements
    {
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

            if (block.Variant != null
                && block.Variant.TryGetValue("flower", out string species)
                && WildFlowerClimate.TryGet(species, out WildFlowerClimate.EcologyEntry ecology))
            {
                if (float.IsNaN(minTemp)) minTemp = ecology.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = ecology.MaxTemp;
                if (float.IsNaN(minRain)) minRain = ecology.MinRain;
                if (float.IsNaN(maxRain)) maxRain = ecology.MaxRain;
                if (float.IsNaN(minForest)) minForest = ecology.MinForest;
                if (float.IsNaN(maxForest)) maxForest = ecology.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = ecology.SpreadRate;
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
                MinTemp = minTemp,
                MaxTemp = maxTemp,
                MinRain = minRain,
                MaxRain = maxRain,
                MinForest = minForest,
                MaxForest = maxForest,
                SpreadRate = spreadRate,
                MinFertility = attrs != null ? attrs["minFertility"].AsInt(100) : 100,
                MinReplaceable = attrs != null ? attrs["minReplaceable"].AsInt(9500) : 9500,
            };
        }
    }
}
