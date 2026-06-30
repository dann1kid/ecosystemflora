using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class FoliageChunkSyncPass
    {
        public readonly struct Result
        {
            public readonly int Indexed;
            public readonly int Changed;
            public readonly int ResumeLx;
            public readonly int ResumeLz;
            public readonly int ResumeY;
            public readonly bool Completed;

            public Result(int indexed, int changed, int resumeLx, int resumeLz, int resumeY, bool completed)
            {
                Indexed = indexed;
                Changed = changed;
                ResumeLx = resumeLx;
                ResumeLz = resumeLz;
                ResumeY = resumeY;
                Completed = completed;
            }
        }

        public static Result Run(
            ICoreAPI api,
            IBlockAccessor acc,
            Vec2i chunkCoord,
            FoliageCellIndex index,
            int resumeLx,
            int resumeLz,
            int resumeY,
            long budgetDeadlineTicks,
            FoliageChunkPassState passState = null)
        {
            ChunkEcologyColumnPass.Result pass = ChunkEcologyColumnPass.Run(
                api,
                acc,
                chunkCoord,
                new ChunkEcologyColumnPass.Request
                {
                    MaxFlowerHits = 0,
                    MaxTreeHits = 0,
                    SyncFoliage = true,
                    FoliageIndex = index,
                    PassState = passState,
                },
                resumeLx,
                resumeLz,
                resumeY,
                onTreeFound: null,
                budgetDeadlineTicks);

            return new Result(
                pass.FoliageIndexed,
                pass.FoliageChanged,
                pass.ResumeLx,
                pass.ResumeLz,
                pass.ResumeY,
                pass.Completed);
        }
    }
}
