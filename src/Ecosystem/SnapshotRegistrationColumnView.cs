using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal sealed class SnapshotRegistrationColumnView : IRegistrationColumnView
    {
        readonly RegistrationChunkSnapshot snapshot;
        readonly System.Collections.Generic.IList<Block> blocks;

        public SnapshotRegistrationColumnView(RegistrationChunkSnapshot snapshot, System.Collections.Generic.IList<Block> blocks)
        {
            this.snapshot = snapshot;
            this.blocks = blocks;
        }

        public ushort[] RainHeightMap => snapshot?.RainHeightMap;

        public bool SupportsFoliageMutation => false;

        public int MapSizeY => snapshot?.MapSizeY ?? 0;

        public IMapChunk GetMapChunk(Vec2i chunkCoord) => null;

        public bool IsValidPos(int x, int y, int z) =>
            y >= 0 && y < MapSizeY;

        public Block GetBlock(int x, int y, int z)
        {
            Block air = ResolveAirBlock();
            if (snapshot == null || blocks == null || y < 0 || y >= MapSizeY) return air;

            int cs = RegistrationChunkSnapshot.ChunkSize;
            int lx = x - snapshot.ChunkCoord.X * cs;
            int lz = z - snapshot.ChunkCoord.Y * cs;
            if (lx < 0 || lz < 0 || lx >= cs || lz >= cs) return air;

            int id = snapshot.GetBlockId(lx, lz, y);
            if (id <= 0 || id >= blocks.Count) return air;

            Block block = blocks[id];
            return block ?? air;
        }

        Block ResolveAirBlock() =>
            blocks != null && blocks.Count > 0 ? blocks[0] : null;
    }
}
