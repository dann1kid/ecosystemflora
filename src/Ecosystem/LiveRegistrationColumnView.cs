using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal sealed class LiveRegistrationColumnView : IRegistrationColumnView
    {
        readonly IBlockAccessor acc;
        static readonly BlockPos scratch = new BlockPos(0);

        public LiveRegistrationColumnView(IBlockAccessor acc)
        {
            this.acc = acc;
        }

        public bool SupportsFoliageMutation => true;

        public int MapSizeY => acc?.MapSizeY ?? 0;

        public IMapChunk GetMapChunk(Vec2i chunkCoord) =>
            acc?.GetMapChunk(chunkCoord.X, chunkCoord.Y);

        public bool IsValidPos(int x, int y, int z)
        {
            scratch.Set(x, y, z);
            return acc != null && acc.IsValidPos(scratch);
        }

        public Block GetBlock(int x, int y, int z)
        {
            scratch.Set(x, y, z);
            return acc?.GetBlock(scratch);
        }
    }
}
