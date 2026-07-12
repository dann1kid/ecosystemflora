using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Resolve spread block ids that exist in the block registry (shape/cover variants).</summary>
    internal static class EcologySpreadBlockResolver
    {
        public static Block Resolve(ICoreAPI api, AssetLocation spreadCode, BlockPos origin, Block liveBlock)
        {
            if (api == null || spreadCode == null) return null;

            Block block = api.World.GetBlock(spreadCode);
            if (IsValid(block)) return block;

            if (liveBlock?.Code != null && PlantSnowCover.BlockHasCoverVariant(liveBlock.Code.Path))
            {
                bool snow = PlantSnowCover.PathHasSnowCover(liveBlock.Code.Path);
                AssetLocation withCover = PlantSnowCover.CodeWithCover(spreadCode, snow);
                block = api.World.GetBlock(withCover);
                if (IsValid(block)) return block;
            }

            string species = PlantCodeHelper.GetEcologySpecies(spreadCode);
            if (species != null)
            {
                foreach (AssetLocation candidate in CandidateCodes(species))
                {
                    block = api.World.GetBlock(candidate);
                    if (IsValid(block)) return block;
                }
            }

            if (liveBlock != null
                && IsValid(liveBlock)
                && PlantCodeHelper.ResolveEcologySpecies(liveBlock) != null
                && PlantCodeHelper.IsEcologyPlant(liveBlock))
            {
                return liveBlock;
            }

            return null;
        }

        static bool IsValid(Block block) => block != null && block.Id != 0;

        static IEnumerable<AssetLocation> CandidateCodes(string species)
        {
            if (EcologyFernSpecies.IsKnown(species))
            {
                foreach (string path in FernCandidatePaths(species))
                {
                    yield return new AssetLocation("game", path);
                }

                yield break;
            }

            if (EcologyBerrySpecies.IsKnown(species))
            {
                foreach (string path in BerryCandidatePaths(species))
                {
                    yield return new AssetLocation("game", path);
                }

                yield break;
            }

            foreach (AssetLocation code in FlowerCandidateCodes(species))
            {
                if (code != null) yield return code;
            }
        }

        static IEnumerable<string> FernCandidatePaths(string species)
        {
            if (species == "tallfern")
            {
                yield return "tallfern";
                yield break;
            }

            yield return "fern-" + species + "-normal-free";
            yield return "fern-" + species + "-normal-snow";
            yield return "fern-" + species + "-short-free";
            yield return "fern-" + species + "-short-snow";
            yield return "fern-" + species + "-short-normal-free";
            yield return "fern-" + species + "-short-normal-snow";
            yield return "fern-" + species + "-free";
            yield return "fern-" + species + "-snow";
            yield return "fern-" + species;
        }

        static IEnumerable<string> BerryCandidatePaths(string species)
        {
            yield return "fruitingbush-wild-" + species + "-free";
            yield return "fruitingbush-wild-" + species + "-snow";
            yield return "fruitingbush-wild-" + species;
        }

        static IEnumerable<AssetLocation> FlowerCandidateCodes(string species)
        {
            AssetLocation free = FlowerJuvenileBlocks.MatureVanillaCode(species);
            if (free == null) yield break;

            yield return free;
            yield return PlantSnowCover.CodeWithCover(free, snow: true);
        }
    }
}
