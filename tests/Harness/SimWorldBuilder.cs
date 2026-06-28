using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem.Testing;

namespace WildFarming.Tests.Harness
{
    internal sealed class SimWorldBuilder
    {
        readonly EcologyTestBlockAccessor accessor;
        int chunkX;
        int chunkZ;

        internal SimWorldBuilder(EcologyTestBlockAccessor accessor)
        {
            this.accessor = accessor;
        }

        public SimWorldBuilder Chunk(int cx, int cz)
        {
            chunkX = cx;
            chunkZ = cz;
            accessor.GetOrCreateMapChunk(cx, cz);
            return this;
        }

        public SimWorldBuilder RainHeight(int lx, int lz, int y)
        {
            int cs = GlobalConstants.ChunkSize;
            accessor.SetRainHeight(chunkX * cs + lx, chunkZ * cs + lz, y);
            return this;
        }

        public SimWorldBuilder Column(int lx, int lz, System.Action<SimColumnBuilder> build)
        {
            var column = new SimColumnBuilder(accessor, chunkX, chunkZ, lx, lz);
            build(column);
            return this;
        }

        public void Build()
        {
        }
    }

    internal sealed class SimColumnBuilder
    {
        readonly EcologyTestBlockAccessor accessor;
        readonly int chunkX;
        readonly int chunkZ;
        readonly int lx;
        readonly int lz;

        internal SimColumnBuilder(
            EcologyTestBlockAccessor accessor,
            int chunkX,
            int chunkZ,
            int lx,
            int lz)
        {
            this.accessor = accessor;
            this.chunkX = chunkX;
            this.chunkZ = chunkZ;
            this.lx = lx;
            this.lz = lz;
        }

        public SimColumnBuilder At(int y, string blockCode)
        {
            int cs = GlobalConstants.ChunkSize;
            var pos = new BlockPos(chunkX * cs + lx, y, chunkZ * cs + lz);
            accessor.SetBlockCode(blockCode, pos);
            return this;
        }

        public SimColumnBuilder Soil(int y) => At(y, "game:soil-medium-normal");
        public SimColumnBuilder Air(int y) => At(y, "game:air");
        public SimColumnBuilder Flower(string species, int y) => At(y, "game:flower-" + species + "-free");
        public SimColumnBuilder Tallgrass(string stage, int y) => At(y, "game:tallgrass-" + stage + "-free");
    }
}
