using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Reads dominant plant role from blocks above/near ground (no RAM store).</summary>
    internal static class WildSoilAgroSampler
    {
        public static PlantSoilRole SampleDominantRole(ICoreAPI api, BlockPos groundPos)
        {
            if (api == null || groundPos == null) return PlantSoilRole.MeadowPerennial;

            IBlockAccessor acc = api.World.BlockAccessor;
            PlantSoilRole bestRole = PlantSoilRole.MeadowPerennial;
            int bestScore = 0;

            void ScorePos(BlockPos pos, int weight)
            {
                Block block = acc.GetBlock(pos);
                if (block == null || block.Id == 0) return;

                string species = PlantCodeHelper.GetEcologySpecies(block.Code);
                if (string.IsNullOrEmpty(species)) return;
                if (!WildSpeciesSoilSuccession.TryGetRole(species, out PlantSoilRole role)) return;

                int score = weight;
                if (role == PlantSoilRole.NitrogenFixer) score += 2;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestRole = role;
                }
            }

            BlockPos up = groundPos.UpCopy();
            ScorePos(up, 3);

            ScorePos(new BlockPos(up.X, up.Y, up.Z - 1), 1);
            ScorePos(new BlockPos(up.X, up.Y, up.Z + 1), 1);
            ScorePos(new BlockPos(up.X - 1, up.Y, up.Z), 1);
            ScorePos(new BlockPos(up.X + 1, up.Y, up.Z), 1);

            return bestRole;
        }
    }
}
