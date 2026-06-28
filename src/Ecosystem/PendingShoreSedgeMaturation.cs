using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Juvenile shore sedge blocks mature into vanilla tallplant parents on a calendar timer.</summary>
    internal sealed class PendingShoreSedgeMaturation : PendingMaturationQueue
    {
        protected override bool IsJuvenileBlock(Block block) => ShoreSedgeJuvenileBlocks.IsJuvenileBlock(block);

        protected override string SpeciesFromJuvenile(Block block) => ShoreSedgeJuvenileBlocks.SpeciesFromJuvenile(block);

        protected override void OnMature(
            ICoreAPI api,
            EcosystemSystem ecosystem,
            IBlockAccessor acc,
            BlockPos pos,
            string species,
            Block mature,
            AssetLocation matureCode)
        {
            acc.SetBlock(mature.BlockId, pos);
            acc.MarkBlockDirty(pos);

            if (!EcosystemParticipant.TryFromBlock(mature, out IEcosystemParticipant participant))
            {
                return;
            }

            ecosystem.RegisterReproducer(pos, participant, spawnBurst: false);
            ecosystem.InvalidateEnvironmentAround(pos);
        }
    }
}
