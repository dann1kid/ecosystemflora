using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace WildFarming.Ecosystem
{
    public class PlantRequirements
    {
        public float MinTemp { get; set; } = -5f;
        public float MaxTemp { get; set; } = 50f;
        /// <summary>Matches vanilla soil tiers (verylow=100, low=150, …).</summary>
        public int MinFertility { get; set; } = 100;
        public int MinReplaceable { get; set; } = 9500;

        public static PlantRequirements FromBlock(Block block)
        {
            if (block?.Attributes == null) return new PlantRequirements();

            return new PlantRequirements
            {
                MinTemp = block.Attributes["minTemp"].AsFloat(-5f),
                MaxTemp = block.Attributes["maxTemp"].AsFloat(50f),
                MinFertility = block.Attributes["minFertility"].AsInt(100),
                MinReplaceable = block.Attributes["minReplaceable"].AsInt(9500),
            };
        }
    }
}
