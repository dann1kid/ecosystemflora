using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    internal static class PlayerProximity
    {
        static readonly HashSet<long> activeChunkKeys = new HashSet<long>();

        public static bool IsNearAnyPlayer(ICoreAPI api, BlockPos pos, int radiusBlocks)
        {
            if (radiusBlocks <= 0) return true;

            ICoreServerAPI sapi = api as ICoreServerAPI;
            if (sapi == null) return true;

            double radiusSq = (double)radiusBlocks * radiusBlocks;
            foreach (IServerPlayer player in sapi.World.AllOnlinePlayers)
            {
                if (player?.Entity?.Pos == null) continue;
                BlockPos playerPos = player.Entity.Pos.AsBlockPos;
                double dx = pos.X - playerPos.X;
                double dz = pos.Z - playerPos.Z;
                if (dx * dx + dz * dz <= radiusSq) return true;
            }

            return false;
        }

        /// <summary>
        /// Build a set of chunk coord keys that fall within radiusBlocks of any online player.
        /// Callers test membership via <see cref="IsActiveChunk"/>.
        /// </summary>
        public static HashSet<long> BuildActivePlayerChunks(ICoreAPI api, int radiusBlocks)
        {
            activeChunkKeys.Clear();

            ICoreServerAPI sapi = api as ICoreServerAPI;
            if (sapi == null || radiusBlocks <= 0) return activeChunkKeys;

            int cs = Vintagestory.API.Config.GlobalConstants.ChunkSize;
            int chunkRadius = (radiusBlocks / cs) + 1;

            foreach (IServerPlayer player in sapi.World.AllOnlinePlayers)
            {
                if (player?.Entity?.Pos == null) continue;
                BlockPos ppos = player.Entity.Pos.AsBlockPos;
                int pcx = ppos.X / cs;
                int pcz = ppos.Z / cs;

                for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
                {
                    for (int dz = -chunkRadius; dz <= chunkRadius; dz++)
                    {
                        activeChunkKeys.Add(ChunkKey(pcx + dx, pcz + dz));
                    }
                }
            }

            return activeChunkKeys;
        }

        public static bool IsActiveChunk(HashSet<long> activeChunks, Vec2i chunkCoord)
        {
            return activeChunks.Contains(ChunkKey(chunkCoord.X, chunkCoord.Y));
        }

        static long ChunkKey(int cx, int cz)
        {
            return ((long)cx << 32) | (uint)cz;
        }
    }
}
