using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Owns the deferred maturation/promotion queues (tree saplings, flower, fern, tallgrass) so the
    /// ecosystem tick no longer manages four separate fields. Routing on placement and per-queue tick
    /// budgets stay with the caller; this type centralizes ownership, removal, and lookups.
    /// </summary>
    internal sealed class MaturationQueueSet
    {
        readonly PendingTreeSaplings treeSaplings = new PendingTreeSaplings();
        readonly PendingFlowerMaturation flowerMaturation = new PendingFlowerMaturation();
        readonly PendingFernMaturation fernMaturation = new PendingFernMaturation();
        readonly PendingTallgrassPromotion tallgrassPromotion = new PendingTallgrassPromotion();

        public void AddTreeSapling(BlockPos pos, string species, double nowHours)
            => treeSaplings.Add(pos, species, nowHours);

        public void AddFern(BlockPos pos, AssetLocation matureCode, string species, double matureAtHours)
            => fernMaturation.Add(pos, matureCode, species, matureAtHours);

        public void AddFlower(BlockPos pos, AssetLocation matureCode, string species, double matureAtHours)
            => flowerMaturation.Add(pos, matureCode, species, matureAtHours);

        public void AddTallgrassPromotion(ICoreAPI api, BlockPos pos)
            => tallgrassPromotion.Add(api, pos);

        /// <summary>Drop a position from every queue whose entries are invalidated when a block changes.
        /// Tree saplings are intentionally not cleared here (matches prior behavior).</summary>
        public void Remove(BlockPos pos)
        {
            flowerMaturation.Remove(pos);
            fernMaturation.Remove(pos);
            tallgrassPromotion.Remove(pos);
        }

        public bool TryGetFlowerHoursLeft(BlockPos pos, double nowHours, out double hoursLeft)
            => flowerMaturation.TryGetHoursUntilMature(pos, nowHours, out hoursLeft);

        public bool TryGetFernHoursLeft(BlockPos pos, double nowHours, out double hoursLeft)
            => fernMaturation.TryGetHoursUntilMature(pos, nowHours, out hoursLeft);

        public void ProcessTreeSaplings(ICoreAPI api, EcosystemSystem ecosystem, double nowHours, int maxChecks)
            => treeSaplings.Process(api, ecosystem, nowHours, maxChecks);

        public void ProcessFlower(ICoreAPI api, EcosystemSystem ecosystem, double nowHours, int maxChecks)
            => flowerMaturation.Process(api, ecosystem, nowHours, maxChecks);

        public void ProcessFern(ICoreAPI api, EcosystemSystem ecosystem, double nowHours, int maxChecks)
            => fernMaturation.Process(api, ecosystem, nowHours, maxChecks);

        public void ProcessTallgrass(ICoreAPI api, EcosystemSystem ecosystem, double nowHours, int maxChecks)
            => tallgrassPromotion.Process(api, ecosystem, nowHours, maxChecks);
    }
}
