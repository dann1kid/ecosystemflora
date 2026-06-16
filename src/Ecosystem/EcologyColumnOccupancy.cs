using System.Collections.Generic;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-chunk XZ occupancy for ecology plants (spread empty-first fast path).</summary>
    internal sealed class EcologyColumnOccupancy
    {
        readonly Dictionary<Vec2i, uint[]> byChunk = new Dictionary<Vec2i, uint[]>();

        public void Clear()
        {
            byChunk.Clear();
        }

        public void OnPlantAdded(BlockPos pos)
        {
            if (pos == null) return;
            SetOccupied(pos.X, pos.Z, occupied: true);
        }

        public void OnPlantRemoved(BlockPos pos, IEnumerable<BlockPos> remainingInChunk)
        {
            if (pos == null) return;

            int x = pos.X;
            int z = pos.Z;
            if (remainingInChunk != null)
            {
                foreach (BlockPos other in remainingInChunk)
                {
                    if (other != null && other.X == x && other.Z == z)
                    {
                        return;
                    }
                }
            }

            SetOccupied(x, z, occupied: false);
        }

        public void RemoveChunk(Vec2i chunkCoord)
        {
            byChunk.Remove(chunkCoord);
        }

        public bool IsOccupied(int worldX, int worldZ)
        {
            if (!TryGetLocal(worldX, worldZ, out Vec2i chunk, out int lx, out int lz))
            {
                return false;
            }

            if (!byChunk.TryGetValue(chunk, out uint[] rows))
            {
                return false;
            }

            return (rows[lz] & (1u << lx)) != 0;
        }

        void SetOccupied(int worldX, int worldZ, bool occupied)
        {
            if (!TryGetLocal(worldX, worldZ, out Vec2i chunk, out int lx, out int lz))
            {
                return;
            }

            if (!byChunk.TryGetValue(chunk, out uint[] rows))
            {
                if (!occupied) return;

                rows = new uint[GlobalConstants.ChunkSize];
                byChunk[chunk] = rows;
            }

            if (occupied)
            {
                rows[lz] |= 1u << lx;
            }
            else
            {
                rows[lz] &= ~(1u << lx);
            }
        }

        static bool TryGetLocal(int worldX, int worldZ, out Vec2i chunk, out int lx, out int lz)
        {
            chunk = default;
            lx = 0;
            lz = 0;

            int cs = GlobalConstants.ChunkSize;
            int cx = worldX >= 0 ? worldX / cs : (worldX + 1) / cs - 1;
            int cz = worldZ >= 0 ? worldZ / cs : (worldZ + 1) / cs - 1;
            lx = worldX - cx * cs;
            lz = worldZ - cz * cs;

            if (lx < 0 || lz < 0 || lx >= cs || lz >= cs)
            {
                return false;
            }

            chunk = new Vec2i(cx, cz);
            return true;
        }
    }
}
