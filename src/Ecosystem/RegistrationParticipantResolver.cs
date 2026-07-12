using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Resolve spread/mature/requirements when <see cref="EcosystemParticipant.TryFromBlock"/>
    /// fails but the live block is still a recognizable ecology plant (path vs attrs drift).
    /// </summary>
    internal static class RegistrationParticipantResolver
    {
        public static bool TryFromLiveBlock(
            ICoreAPI api,
            BlockPos origin,
            Block liveBlock,
            ref PlantRequirements requirements,
            ref AssetLocation spreadBlockCode,
            ref AssetLocation matureBlockCode)
        {
            if (api == null || origin == null || liveBlock == null || liveBlock.Id == 0) return false;

            if (EcosystemParticipant.TryFromBlock(liveBlock, out IEcosystemParticipant participant))
            {
                if (!MeetsSpreadRegistrationGate(api, origin, liveBlock)) return false;

                spreadBlockCode = participant.SpreadBlockCode;
                matureBlockCode = participant.MatureBlockCode;
                requirements = participant.Requirements;
                return true;
            }

            if (!CanRegisterWithoutParticipant(api, origin, liveBlock)) return false;

            PlantRequirements fromBlock = PlantRequirements.FromBlock(liveBlock);
            if (fromBlock == null || string.IsNullOrEmpty(fromBlock.Species)) return false;

            AssetLocation spread = PlantCodeHelper.SpreadBlockCode(liveBlock);
            if (spread == null)
            {
                Block resolved = EcologySpreadBlockResolver.Resolve(
                    api,
                    matureBlockCode ?? liveBlock.Code,
                    origin,
                    liveBlock);
                spread = resolved?.Code;
            }

            if (spread == null) return false;

            if (!MeetsSpreadRegistrationGate(api, origin, liveBlock)) return false;

            requirements = fromBlock;
            spreadBlockCode = spread.Clone();
            matureBlockCode = liveBlock.Code?.Clone() ?? matureBlockCode;
            return true;
        }

        static bool CanRegisterWithoutParticipant(ICoreAPI api, BlockPos origin, Block block)
        {
            if (block?.Code == null || block.Id == 0) return false;
            if (block.Attributes != null && !block.Attributes["ecologyReproduce"].AsBool(true)) return false;

            string path = block.Code.Path;
            if (path != null && path.Contains("-harvested-")) return false;

            if (PlantCodeHelper.ResolveEcologySpecies(block) == null
                || !PlantCodeHelper.IsEcologyPlant(block))
            {
                return false;
            }

            return MeetsSpreadRegistrationGate(api, origin, block);
        }

        static bool MeetsSpreadRegistrationGate(ICoreAPI api, BlockPos origin, Block block)
        {
            if (block == null) return false;
            if (PlantCodeHelper.ResolveEcologySpecies(block) != "tallgrass") return true;
            if (!TallgrassSpreadMaturation.UsesMaturation(EcosystemConfig.Loaded)) return true;

            return TallgrassSpreadMaturation.CanReproduceFrom(block, api, origin);
        }
    }
}
