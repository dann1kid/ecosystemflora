using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    public class PlantRequirements
    {
        public float MinTemp { get; set; } = -5f;
        public float MaxTemp { get; set; } = 50f;
        public int MinFertility { get; set; } = 100;
        public int MinReplaceable { get; set; } = 9500;

        public static PlantRequirements FromBlock(Block block)
        {
            if (block?.Attributes == null) return new PlantRequirements();

            float minTemp = block.Attributes["minTemp"].AsFloat(float.NaN);
            float maxTemp = block.Attributes["maxTemp"].AsFloat(float.NaN);

            if ((float.IsNaN(minTemp) || float.IsNaN(maxTemp))
                && block.Variant != null
                && block.Variant.TryGetValue("flower", out string species)
                && WildFlowerClimate.TryGet(species, out float tableMin, out float tableMax, out _))
            {
                if (float.IsNaN(minTemp)) minTemp = tableMin;
                if (float.IsNaN(maxTemp)) maxTemp = tableMax;
            }

            if (float.IsNaN(minTemp)) minTemp = 10f;
            if (float.IsNaN(maxTemp)) maxTemp = 22f;

            return new PlantRequirements
            {
                MinTemp = minTemp,
                MaxTemp = maxTemp,
                MinFertility = block.Attributes["minFertility"].AsInt(100),
                MinReplaceable = block.Attributes["minReplaceable"].AsInt(9500),
            };
        }
    }
}
