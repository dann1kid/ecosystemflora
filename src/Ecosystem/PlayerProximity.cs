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
        /// Snapshot of player positions for batch proximity checks within a single tick.
        /// Avoids repeated <c>AllOnlinePlayers</c> iteration and <c>AsBlockPos</c> allocations.
        /// </summary>
        internal sealed class Snapshot
        {
            int[] xs;
            int[] zs;
            int count;
            readonly HashSet<long> nearChunks = new HashSet<long>();

            public void Refresh(ICoreAPI api, int radiusBlocks)
            {
                nearChunks.Clear();

                ICoreServerAPI sapi = api as ICoreServerAPI;
                if (sapi == null) { count = 0; return; }

                var players = sapi.World.AllOnlinePlayers;
                int n = players.Length;

                if (xs == null || xs.Length < n)
                {
                    xs = new int[n];
                    zs = new int[n];
                }

                int cs = Vintagestory.API.Config.GlobalConstants.ChunkSize;
                int chunkRadius = (radiusBlocks / cs) + 1;

                count = 0;
                for (int i = 0; i < n; i++)
                {
                    IServerPlayer p = players[i] as IServerPlayer;
                    if (p?.Entity?.Pos == null) continue;
                    int px = (int)p.Entity.Pos.X;
                    int pz = (int)p.Entity.Pos.Z;
                    xs[count] = px;
                    zs[count] = pz;
                    count++;

                    int pcx = px / cs;
                    int pcz = pz / cs;
                    for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
                        for (int dz = -chunkRadius; dz <= chunkRadius; dz++)
                            nearChunks.Add(ChunkKey(pcx + dx, pcz + dz));
                }
            }

            public bool IsNearChunk(BlockPos pos)
            {
                if (nearChunks.Count == 0) return false;
                int cs = Vintagestory.API.Config.GlobalConstants.ChunkSize;
                return nearChunks.Contains(ChunkKey(pos.X / cs, pos.Z / cs));
            }

            public bool IsNear(BlockPos pos, int radiusBlocks)
            {
                if (count == 0) return false;
                long rSq = (long)radiusBlocks * radiusBlocks;
                for (int i = 0; i < count; i++)
                {
                    long dx = pos.X - xs[i];
                    long dz = pos.Z - zs[i];
                    if (dx * dx + dz * dz <= rSq) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Build a set of chunk coord keys that fall within radiusBlocks of any online player.
        /// Callers test membership via <see cref="IsActiveChunk"/>.
        /// Warning: returns a shared buffer — do not retain across another <see cref="BuildActivePlayerChunks"/>
        /// / <see cref="FillActivePlayerChunks"/> call; use <see cref="FillActivePlayerChunks"/> into a private set instead.
        /// </summary>
        public static HashSet<long> BuildActivePlayerChunks(ICoreAPI api, int radiusBlocks)
        {
            FillActivePlayerChunks(api, radiusBlocks, activeChunkKeys);
            return activeChunkKeys;
        }

        /// <summary>Clears and fills <paramref name="dest"/> with player-vicinity chunk keys.</summary>
        public static void FillActivePlayerChunks(ICoreAPI api, int radiusBlocks, HashSet<long> dest)
        {
            if (dest == null) return;
            dest.Clear();

            ICoreServerAPI sapi = api as ICoreServerAPI;
            if (sapi == null || radiusBlocks <= 0) return;

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
                        dest.Add(ChunkKey(pcx + dx, pcz + dz));
                    }
                }
            }
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
