using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Tree host removed: wake nearby ecology so forest mycelium anchors fail stress on the
    /// normal recheck cadence (<see cref="MyceliumStressEvaluator"/>), not instant removal.
    /// </summary>
    internal static class MyceliumTreeCascade
    {
        public static void OnTreeRemoved(ICoreAPI api, BlockPos treePos, Block hostBlock)
        {
            if (api == null || treePos == null || hostBlock == null) return;
            if (!EcosystemConfig.Loaded.EnableMyceliumEcology) return;
            if (!PlantCodeHelper.IsTreeLogGrownBlock(hostBlock)) return;

            FloraSymbiosis.NotifyHostRemoved(api, treePos, hostBlock);
        }
    }
}
