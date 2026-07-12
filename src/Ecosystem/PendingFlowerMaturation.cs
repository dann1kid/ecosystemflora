using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Juvenile spread blocks mature into vanilla parents on a calendar timer.</summary>
    internal sealed class PendingFlowerMaturation : PendingMaturationQueue
    {
        protected override bool IsJuvenileBlock(Block block) => FlowerJuvenileBlocks.IsJuvenileBlock(block);

        protected override string SpeciesFromJuvenile(Block block) => FlowerJuvenileBlocks.SpeciesFromJuvenile(block);

        protected override bool OnMature(
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

            Block live = acc.GetBlock(pos);
            if (!EcosystemParticipant.TryFromBlock(live, out IEcosystemParticipant participant))
            {
                return true;
            }

            bool registered = phenology
                ? ecosystem.RegisterReproducer(
                    pos,
                    participant.SpreadBlockCode,
                    participant.MatureBlockCode,
                    participant.Requirements,
                    spawnBurst: false,
                    flowerSpreadEstablished: true)
                : ecosystem.RegisterReproducer(pos, participant, spawnBurst: false);

            if (!registered || !ecosystem.RegistryContains(pos))
            {
                return false;
            }

            ecosystem.InvalidateEnvironmentAround(pos);
            return true;
        }
    }
}
