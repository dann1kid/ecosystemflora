using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class FlowerSpreadMaturation
    {
        public static Block ResolveSpreadBlock(
            ICoreAPI api,
            BlockPos origin,
            PlantRequirements requirements,
            Block matureSpreadBlock)
        {
            if (api == null || requirements == null || matureSpreadBlock == null) return matureSpreadBlock;
            if (requirements.Habitat != EcologyHabitat.Terrestrial) return matureSpreadBlock;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!WildFlowerMaturation.UsesMaturation(cfg, requirements.Species)) return matureSpreadBlock;

            AssetLocation juvenileCode = FlowerJuvenileBlocks.CodeForSpecies(requirements.Species);
            if (juvenileCode == null) return matureSpreadBlock;

            Block juvenile = api.World.GetBlock(juvenileCode);
            return juvenile ?? matureSpreadBlock;
        }

        public static bool ShouldQueueMaturation(Block placedBlock, PlantRequirements requirements)
        {
            if (placedBlock == null || requirements == null) return false;
            if (requirements.Habitat != EcologyHabitat.Terrestrial) return false;
            if (!WildFlowerMaturation.UsesMaturation(EcosystemConfig.Loaded, requirements.Species)) return false;
            return FlowerJuvenileBlocks.IsJuvenileBlock(placedBlock);
        }
    }
}
