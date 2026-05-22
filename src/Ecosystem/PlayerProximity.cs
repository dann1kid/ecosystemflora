using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    internal static class PlayerProximity
    {
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

        public static bool ChunkNearAnyPlayer(ICoreAPI api, Vec2i chunkCoord, int radiusBlocks)
        {
            int cs = Vintagestory.API.Config.GlobalConstants.ChunkSize;
            int cx = chunkCoord.X * cs + cs / 2;
            int cz = chunkCoord.Y * cs + cs / 2;
            return IsNearAnyPlayer(api, new BlockPos(cx, 0, cz), radiusBlocks);
        }
    }
}
