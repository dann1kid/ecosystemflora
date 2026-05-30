using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Meadow plant break harvest. Knife/scythe → drygrass via <see cref="FlowerDrygrassDrops"/> (vanilla drops).
    /// Flowers + other hands/tools → flower block in world. Tallgrass → removed with no drop (mow for drygrass).
    /// Herbalism mods: <see cref="MeadowHarvestRegistry"/> and block attr <c>ecologyMeadowHarvest</c>.
    /// </summary>
    internal static class PlantHandHarvest
    {
        internal static bool TryDropPlantBlockOnBreak(ICoreAPI api, IServerPlayer byPlayer, Block brokenBlock, BlockPos pos)
        {
            if (api == null || byPlayer == null || brokenBlock?.Code == null || pos == null)
                return false;

            if (!EcosystemConfig.Loaded.EnableFlowerDrygrass)
                return false;

            if (!IsMeadowPlant(brokenBlock))
                return false;

            string harvestMode = MeadowHarvestModes.Read(brokenBlock);
            if (MeadowHarvestModes.SkipsModHarvest(harvestMode))
                return false;

            ItemSlot activeSlot = byPlayer.InventoryManager?.ActiveHotbarSlot;
            bool isMowTool = IsMowTool(activeSlot);
            if (isMowTool)
                return false;

            var args = new MeadowHarvestBreakArgs(api, byPlayer, brokenBlock, pos, activeSlot, isMowTool);
            if (MeadowHarvestRegistry.Invoke(args) == MeadowHarvestHandleResult.Handled)
                return true;

            if (!MeadowHarvestModes.AllowsDefaultWholeDrop(harvestMode))
                return false;

            if (!DropsWholePlantBlock(brokenBlock))
                return true;

            if (!TryResolveFlowerBlockDrop(brokenBlock, out ItemStack drop))
                return false;

            api.World.SpawnItemEntity(drop, pos);
            return true;
        }

        /// <summary>Flowers drop as blocks; tallgrass is cleared unless knife/scythe (drygrass).</summary>
        internal static bool DropsWholePlantBlock(Block block)
        {
            if (block?.Code == null || !block.Code.Domain.Equals("game"))
                return false;

            string path = block.Code.Path;
            return path.StartsWith("flower-") && !path.StartsWith("flower-ghostpipe");
        }

        internal static bool IsMowTool(ItemSlot slot)
        {
            if (slot == null || slot.Empty) return false;
            return IsMowTool(slot.Itemstack?.Collectible as Item);
        }

        internal static bool IsMowTool(Item item)
        {
            if (item == null) return false;
            return item.Tool == EnumTool.Knife || item.Tool == EnumTool.Scythe;
        }

        internal static bool ShouldDropPlantBlock(ItemSlot activeSlot)
        {
            return !IsMowTool(activeSlot);
        }

        internal static bool IsMeadowPlant(Block block)
        {
            if (block?.Code == null || !block.Code.Domain.Equals("game"))
                return false;

            string path = block.Code.Path;
            if (path.StartsWith("flower-"))
                return !path.StartsWith("flower-ghostpipe");

            if (path.StartsWith("tallgrass-"))
                return !path.Contains("-eaten-");

            return path.StartsWith("frostedtallgrass-") && !path.Contains("-eaten-");
        }

        static bool TryResolveFlowerBlockDrop(Block block, out ItemStack drop)
        {
            drop = null;
            if (!DropsWholePlantBlock(block))
                return false;

            drop = new ItemStack(block, 1);
            return true;
        }
    }
}
