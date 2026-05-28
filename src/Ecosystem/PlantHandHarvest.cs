using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Hand harvest for meadow plants. Vanilla flower/tallgrass drops are tool-gated only;
    /// knife/scythe drygrass for flowers is patched in <see cref="FlowerDrygrassDrops"/>.
    /// Empty-hand pickup is applied in <see cref="EcosystemSystem"/> DidBreakBlock (server).
    /// </summary>
    internal static class PlantHandHarvest
    {
        internal static bool TryGiveBareHandDrop(ICoreAPI api, IServerPlayer byPlayer, Block brokenBlock, BlockPos pos)
        {
            if (api == null || byPlayer == null || brokenBlock?.Code == null || pos == null)
                return false;

            if (!EcosystemConfig.Loaded.EnableFlowerDrygrass)
                return false;

            if (!IsBareHand(byPlayer))
                return false;

            if (!TryResolveBareHandDrop(brokenBlock, out ItemStack drop))
                return false;

            if (byPlayer.InventoryManager.TryGiveItemstack(drop))
                return true;

            api.World.SpawnItemEntity(drop, pos);
            return true;
        }

        internal static bool IsBareHand(IPlayer player)
        {
            if (player?.InventoryManager == null)
                return false;

            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
            return slot == null || slot.Empty;
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

        static bool TryResolveBareHandDrop(Block block, out ItemStack drop)
        {
            drop = null;
            if (!IsMeadowPlant(block))
                return false;

            drop = new ItemStack(block, 1);
            return true;
        }
    }
}
