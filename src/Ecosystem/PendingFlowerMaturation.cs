using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Juvenile spread blocks mature into vanilla parents on a calendar timer.</summary>
    internal sealed class PendingFlowerMaturation : PendingMaturationQueue
    {
        protected override bool IsJuvenileBlock(Block block) => FlowerJuvenileBlocks.IsJuvenileBlock(block);

        protected override string SpeciesFromJuvenile(Block block) => FlowerJuvenileBlocks.SpeciesFromJuvenile(block);

        protected override void OnMature(
            ICoreAPI api,
            EcosystemSystem ecosystem,
            IBlockAccessor acc,
            BlockPos pos,
            string species,
            Block mature,
            AssetLocation matureCode)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            bool phenology = FlowerPhenology.UsesPhenology(
                cfg,
                new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial });

            if (!phenology)
            {
                acc.SetBlock(mature.BlockId, pos);
                acc.MarkBlockDirty(pos);
            }

            if (!EcosystemParticipant.TryFromBlock(mature, out IEcosystemParticipant participant))
            {
                return;
            }

            if (phenology)
            {
                ecosystem.RegisterReproducer(
                    pos,
                    participant.SpreadBlockCode,
                    participant.MatureBlockCode,
                    participant.Requirements,
                    spawnBurst: false,
                    flowerSpreadEstablished: true);
            }
            else
            {
                acc.SetBlock(mature.BlockId, pos);
                acc.MarkBlockDirty(pos);
                ecosystem.RegisterReproducer(pos, participant, spawnBurst: false);
            }

            ecosystem.InvalidateEnvironmentAround(pos);
        }
    }
}
