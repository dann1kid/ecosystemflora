using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Juvenile fern blocks mature into vanilla parents on a calendar timer.</summary>
    internal sealed class PendingFernMaturation : PendingMaturationQueue
    {
        protected override bool IsJuvenileBlock(Block block) => FernJuvenileBlocks.IsJuvenileBlock(block);

        protected override string SpeciesFromJuvenile(Block block) => FernJuvenileBlocks.SpeciesFromJuvenile(block);

        protected override bool OnMature(
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

            Block live = acc.GetBlock(pos);
            if (!EcosystemParticipant.TryCreateForRegistration(api, pos, live, out IEcosystemParticipant participant))
            {
                return true;
            }

            if (!ecosystem.RegisterReproducer(pos, participant, spawnBurst: false)
                || !ecosystem.RegistryContains(pos))
            {
                return false;
            }

            ecosystem.InvalidateEnvironmentAround(pos);
            return true;
        }
    }
}
