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

            if (!ShouldDropFlowerBlockInWorld(brokenBlock))
                return true;

            ReproducerEntry entry = null;
            EcosystemSystem.Instance?.TryGetReproducer(pos, out entry);

            if (!TryResolveFlowerBlockDrop(api, brokenBlock, pos, entry, out ItemStack drop))
                return false;

            api.World.SpawnItemEntity(drop, pos);
            return true;
        }

        /// <summary>Hand break drops a collectible flower block (vanilla bloom), including phenology/juvenile stand-ins.</summary>
        internal static bool ShouldDropFlowerBlockInWorld(Block block)
        {
            if (DropsWholePlantBlock(block))
                return true;

            return FlowerJuvenileBlocks.IsJuvenileBlock(block)
                || FlowerPhenologyBlocks.IsPhaseBlock(block);
        }

        /// <summary>Flowers drop as blocks; tallgrass is cleared unless knife/scythe (drygrass).</summary>
        internal static bool DropsWholePlantBlock(Block block)
        {
            return IsVanillaFlowerBlock(block) && !IsGhostpipeFlower(block);
        }

        /// <summary>Any vanilla flower block code (including ghost pipe) — used when mapping phenology/juvenile to a collectible drop.</summary>
        internal static bool IsVanillaFlowerBlock(Block block)
        {
            if (block?.Code == null || !block.Code.Domain.Equals("game"))
                return false;

            return block.Code.Path.StartsWith("flower-");
        }

        static bool IsGhostpipeFlower(Block block)
        {
            string path = block?.Code?.Path;
            return path != null && path.StartsWith("flower-ghostpipe");
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
            if (block?.Code == null) return false;

            if (FlowerJuvenileBlocks.IsJuvenileBlock(block)
                || FlowerPhenologyBlocks.IsPhaseBlock(block)
                || ShoreSedgeJuvenileBlocks.IsJuvenileBlock(block))
                return true;

            if (!block.Code.Domain.Equals("game"))
                return false;

            string path = block.Code.Path;
            if (path.StartsWith("flower-"))
                return !path.StartsWith("flower-ghostpipe");

            if (path.StartsWith("tallgrass-"))
                return !path.Contains("-eaten-");

            if (path.StartsWith("tallplant-brownsedge"))
                return true;

            return path.StartsWith("frostedtallgrass-") && !path.Contains("-eaten-");
        }

        internal static bool TryResolveFlowerBlockDrop(
            ICoreAPI api,
            Block brokenBlock,
            BlockPos pos,
            ReproducerEntry entry,
            out ItemStack drop)
        {
            drop = null;
            if (brokenBlock == null)
                return false;

            if (DropsWholePlantBlock(brokenBlock))
            {
                drop = new ItemStack(brokenBlock, 1);
                return true;
            }

            Block matureBlock = ResolveMatureFlowerBlock(api, brokenBlock, pos, entry);
            if (matureBlock == null || !IsVanillaFlowerBlock(matureBlock))
                return false;

            drop = new ItemStack(matureBlock, 1);
            return true;
        }

        static Block ResolveMatureFlowerBlock(ICoreAPI api, Block brokenBlock, BlockPos pos, ReproducerEntry entry)
        {
            if (entry?.MatureBlockCode != null && api?.World != null)
            {
                Block fromEntry = api.World.GetBlock(entry.MatureBlockCode);
                if (fromEntry != null && fromEntry.Id != 0)
                    return fromEntry;
            }

            if (api?.World == null)
                return null;

            string species = FlowerJuvenileBlocks.SpeciesFromJuvenile(brokenBlock)
                ?? FlowerPhenologyBlocks.SpeciesFromPhaseBlock(brokenBlock);
            if (string.IsNullOrEmpty(species))
                return null;

            AssetLocation matureCode = api.World.BlockAccessor != null && pos != null
                ? FlowerJuvenileBlocks.ResolveMatureCode(api, pos, species)
                : FlowerJuvenileBlocks.MatureVanillaCode(species);
            if (matureCode == null)
                return null;

            Block mature = api.World.GetBlock(matureCode);
            return mature != null && mature.Id != 0 ? mature : null;
        }
    }
}
