using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Rewrites flower block drops so that:
    ///   knife/scythe → drygrass only  (LastDrop stops further drops),
    ///   bare hand    → flower block itself  (vanilla pickup preserved).
    /// </summary>
    internal static class FlowerDrygrassDrops
    {
        internal static void Apply(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;
            if (!EcosystemConfig.Loaded.EnableFlowerDrygrass) return;

            int patched = 0;

            foreach (Block block in api.World.Blocks)
            {
                if (block?.Code == null) continue;
                if (!block.Code.Domain.Equals("game")) continue;
                if (!block.Code.Path.StartsWith("flower-")) continue;

                var drygrassDrops = new BlockDropItemStack[]
                {
                    CreateDrygrassDrop(EnumTool.Knife),
                    CreateDrygrassDrop(EnumTool.Scythe),
                };

                BlockDropItemStack[] tailDrops = block.Drops;
                if (tailDrops == null || tailDrops.Length == 0)
                {
                    var selfDrop = new BlockDropItemStack
                    {
                        Type = EnumItemClass.Block,
                        Code = block.Code.Clone(),
                        Quantity = NatFloat.createUniform(1f, 0f),
                    };
                    selfDrop.Resolve(api.World, "ecosystemflora:FlowerDrygrassDrops", block.Code);
                    tailDrops = new[] { selfDrop };
                }

                foreach (var drop in drygrassDrops)
                {
                    drop.Resolve(api.World, "ecosystemflora:FlowerDrygrassDrops", block.Code);
                }

                block.Drops = drygrassDrops.Concat(tailDrops).ToArray();
                patched++;
            }

            if (EcosystemConfig.Loaded.VerboseLogging)
            {
                api.Logger.Notification(
                    "[ecosystemflora] FlowerDrygrassDrops: patched {0} flower blocks",
                    patched);
            }
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
