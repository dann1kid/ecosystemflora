using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Gate for Harmony extension of vanilla <c>BlockEntityMycelium</c> cap placement.
    /// Vanilla regrowth only uses air after the first fruiting cycle; meadow flora may block caps.
    /// </summary>
    internal static class MyceliumCapPlacement
    {
        public static bool PassesVanillaPlacementGate(Block hereBlock, double mushroomsGrownTotalDays)
        {
            if (hereBlock == null) return false;
            if (hereBlock.Id == 0) return true;
            if (mushroomsGrownTotalDays == 0 && hereBlock.Replaceable >= 6000) return true;
            return false;
        }

        /// <summary>Meadow grass/flowers may be displaced by vanilla cap regrowth (Harmony path).</summary>
        public static bool PassesModDisplacementGate(Block hereBlock)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg == null || !cfg.EnableMyceliumEcology || !cfg.EnableMyceliumCapDisplacement)
            {
                return false;
            }

            return MyceliumCoexistence.IsMeadowPlantBlock(hereBlock);
        }

        public static bool PassesExtendedPlacementGate(Block hereBlock, double mushroomsGrownTotalDays)
        {
            return PassesVanillaPlacementGate(hereBlock, mushroomsGrownTotalDays)
                || PassesModDisplacementGate(hereBlock);
        }
    }
}
