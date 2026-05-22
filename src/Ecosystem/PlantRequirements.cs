using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace WildFarming.Ecosystem
{
    public class PlantRequirements
    {
        public float MinTemp { get; set; } = -5f;
        public float MaxTemp { get; set; } = 50f;
        public int MinFertility { get; set; } = 1;
        public int MinReplaceable { get; set; } = 9501;

        public static PlantRequirements FromBlock(Block block)
        {
            if (block?.Attributes == null) return new PlantRequirements();

            return new PlantRequirements
            {
                MinTemp = block.Attributes["minTemp"].AsFloat(-5f),
                MaxTemp = block.Attributes["maxTemp"].AsFloat(50f),
                MinFertility = block.Attributes["minFertility"].AsInt(1),
                MinReplaceable = block.Attributes["minReplaceable"].AsInt(9501),
            };
        }
    }
}
