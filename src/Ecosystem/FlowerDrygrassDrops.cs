using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Rewrites flower block drops so that knife/scythe → drygrass only (LastDrop).
    /// Whole-plant pickup for other tools/hands → <see cref="PlantHandHarvest"/> on DidBreakBlock.
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

                BlockDropItemStack knifeDrygrass = CreateDrygrassDrop(EnumTool.Knife);
                BlockDropItemStack scytheDrygrass = CreateDrygrassDrop(EnumTool.Scythe);
                knifeDrygrass.Resolve(api.World, "ecosystemflora:FlowerDrygrassDrops", block.Code);
                scytheDrygrass.Resolve(api.World, "ecosystemflora:FlowerDrygrassDrops", block.Code);
                block.Drops = new[] { knifeDrygrass, scytheDrygrass };
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
