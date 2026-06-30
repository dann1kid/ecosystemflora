using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace WildFarming.Ecosystem
{
    /// <summary>Blocks ecology-driven foliage placement while fire is active nearby.</summary>
    internal static class CanopyBurnGuard
    {
        /// <summary>Early bail when the bud source sits inside an active burn zone.</summary>
        internal const int SourceRadius = 3;

        /// <summary>Per-candidate check before placing into a vacant cell.</summary>
        internal const int CandidateRadius = 2;

        /// <summary>True when an active fire block lies within <paramref name="radius"/> of <paramref name="center"/>.</summary>
        public static bool SuppressesFoliagePlacement(IBlockAccessor acc, BlockPos center, int radius = SourceRadius)
        {
            if (acc == null || center == null || radius < 0) return false;

            var scratch = new BlockPos(0);
            int x0 = center.X - radius;
            int y0 = center.Y - radius;
            int z0 = center.Z - radius;
            int x1 = center.X + radius;
            int y1 = center.Y + radius;
            int z1 = center.Z + radius;

            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        scratch.Set(x, y, z);
                        if (!acc.IsValidPos(scratch)) continue;
                        if (IsActiveFireBlock(acc.GetBlock(scratch)))
                        {
                            NoteFireNear(scratch);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsActiveFireBlock(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (block.BlockMaterial == EnumBlockMaterial.Fire) return true;

            string path = block.Code?.Path;
            if (string.IsNullOrEmpty(path)) return false;

            if (path == "fire" || path.StartsWithOrdinal("fire-")) return true;

            int colon = path.LastIndexOf(':');
            if (colon >= 0)
            {
                path = path.Substring(colon + 1);
                return path == "fire" || path.StartsWithOrdinal("fire-");
            }

            return false;
        }

        public static bool SuppressesBudTarget(IBlockAccessor acc, BlockPos target) =>
            SuppressesFoliagePlacement(acc, target, CandidateRadius);

        static void NoteFireNear(BlockPos firePos)
        {
            EcosystemSystem system = EcosystemSystem.Instance;
            ICoreAPI api = system?.ServerApi;
            if (system?.FoliageCells == null || api?.World?.Calendar == null || firePos == null) return;

            system.FoliageCells.NoteFireTouched(firePos, api.World.Calendar.TotalHours);
        }
    }
}
