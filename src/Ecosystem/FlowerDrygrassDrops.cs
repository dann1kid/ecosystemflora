using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Rewrites flower block drops so that knife/scythe → drygrass only (LastDrop).
    /// Whole-plant pickup for other tools/hands → <see cref="PlantHandHarvest"/> on DidBreakBlock.
    /// Syncs after world config load so per-world EnableFlowerDrygrass wins over the template.
    /// </summary>
    internal static class FlowerDrygrassDrops
    {
        static readonly Dictionary<int, BlockDropItemStack[]> originalDropsByBlockId =
            new Dictionary<int, BlockDropItemStack[]>();

        internal static void Apply(ICoreAPI api)
        {
            if (api == null || api.Side != EnumAppSide.Server) return;
            if (api.World?.Blocks == null) return;

            bool enabled = EcosystemConfig.Loaded.EnableFlowerDrygrass;
            int patched = 0;
            int restored = 0;

            foreach (Block block in api.World.Blocks)
            {
                if (block?.Code == null || block.Id == 0) continue;
                if (!ShouldPatchBreakDrops(block.Code)) continue;

                if (enabled)
                {
                    if (TryPatchBlock(api, block)) patched++;
                }
                else if (TryRestoreBlock(block))
                {
                    restored++;
                }
            }

            if (EcosystemConfig.Loaded.VerboseLogging && (patched > 0 || restored > 0))
            {
                api.Logger.Notification(
                    "[ecosystemflora] FlowerDrygrassDrops: patched={0} restored={1} enabled={2}",
                    patched,
                    restored,
                    enabled);
            }
        }

        /// <summary>Test hook: clear remembered originals between cases.</summary>
        internal static void ClearOriginalsForTests() => originalDropsByBlockId.Clear();

        /// <summary>Test hook: patch/restore a single block without resolving drop stacks against a world.</summary>
        internal static void SyncBlockForTests(Block block, bool enabled, BlockDropItemStack[] drygrassDrops)
        {
            if (block == null || block.Id == 0) return;
            if (enabled)
            {
                RememberOriginal(block);
                block.Drops = drygrassDrops;
                return;
            }

            TryRestoreBlock(block);
        }

        static bool TryPatchBlock(ICoreAPI api, Block block)
        {
            RememberOriginal(block);

            BlockDropItemStack knifeDrygrass = CreateDrygrassDrop(EnumTool.Knife);
            BlockDropItemStack scytheDrygrass = CreateDrygrassDrop(EnumTool.Scythe);
            knifeDrygrass.Resolve(api.World, "ecosystemflora:FlowerDrygrassDrops", block.Code);
            scytheDrygrass.Resolve(api.World, "ecosystemflora:FlowerDrygrassDrops", block.Code);
            block.Drops = new[] { knifeDrygrass, scytheDrygrass };
            return true;
        }

        static void RememberOriginal(Block block)
        {
            if (originalDropsByBlockId.ContainsKey(block.Id)) return;
            originalDropsByBlockId[block.Id] = block.Drops;
        }

        static bool TryRestoreBlock(Block block)
        {
            if (!originalDropsByBlockId.TryGetValue(block.Id, out BlockDropItemStack[] original))
            {
                return false;
            }

            block.Drops = original;
            return true;
        }

        internal static bool ShouldPatchBreakDrops(AssetLocation code)
        {
            if (code == null) return false;

            if (code.Domain.Equals("game") && code.Path.StartsWith("flower-"))
                return true;

            // Mature brownsedge is vanilla BlockReeds (knife → harvested state, reed drops).
            // Replacing its drops breaks client OnGettingBroken — patch juvenile spread blocks only.

            if (!code.Domain.Equals(JuvenileBlockNaming.Domain)) return false;

            return code.Path.StartsWith("juvenile-flower-")
                || code.Path.StartsWith("flowerphase-")
                || code.Path.StartsWith("juvenile-sedge-");
        }

        static BlockDropItemStack CreateDrygrassDrop(EnumTool tool)
        {
            return new BlockDropItemStack
            {
                Type = EnumItemClass.Item,
                Code = new AssetLocation("game", "drygrass"),
                Quantity = NatFloat.createUniform(1f, 0.3f),
                Tool = tool,
                LastDrop = true,
            };
        }
    }
}
