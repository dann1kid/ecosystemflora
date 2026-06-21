using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Top-down column scan for ecology parents (flowers, tallgrass, …).</summary>
    internal static class RegistrationColumnFlowerScan
    {
        static readonly BlockPos scratch = new BlockPos(0);

        public static bool TryFindTopReproducer(
            IRegistrationColumnView view,
            ICoreAPI api,
            int x,
            int z,
            int scanTopY,
            out Block block,
            out BlockPos pos,
            out bool needsEstablishment)
        {
            block = null;
            pos = null;
            needsEstablishment = false;

            if (view == null || scanTopY < 0) return false;

            for (int y = scanTopY; y >= 0; y--)
            {
                scratch.Set(x, y, z);
                block = view.GetBlock(x, y, z);
                if (block == null || block.Id == 0) continue;

                if (TallgrassEstablishment.UsesEstablishment(EcosystemConfig.Loaded)
                    && PlantCodeHelper.ResolveEcologySpecies(block) == "tallgrass"
                    && TallgrassEstablishment.NeedsEstablishment(api, scratch, block, out _))
                {
                    pos = scratch.Copy();
                    needsEstablishment = true;
                    return true;
                }

                if (EcologyAttributes.ReproduceEnabled(block))
                {
                    pos = scratch.Copy();
                    return true;
                }

                if (!PlantVacancyRules.IsPassThroughForColumnScan(block))
                {
                    return false;
                }
            }

            block = null;
            return false;
        }
    }
}
