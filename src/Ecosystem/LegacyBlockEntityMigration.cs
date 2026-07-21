using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// One-time migration for saves that still have mod block entities on ecology blocks.
    /// Ecology registers via chunk scan (<see cref="RegistrationScanQueue"/>, optional background worker);
    /// no BE is attached to new plants.
    /// </summary>
    internal static class LegacyBlockEntityMigration
    {
        public static void Register(ICoreAPI api)
        {
            api.RegisterBlockEntityClass("EcoSystemLife", typeof(LegacyModBlockEntityStripper));
            api.RegisterBlockEntityClass("EcosystemPlant", typeof(LegacyModBlockEntityStripper));
        }

        /// <summary>
        /// Schedule legacy BE removal after the column finishes deserializing block entities.
        /// Positions are collected in the callback — not at load time, when BEs may not exist yet.
        /// Delay is staggered per chunk so mass loads do not strip every column in one frame.
        /// </summary>
        public static void ScheduleStripColumn(ICoreAPI api, Vec2i chunkCoord)
        {
            if (api?.World?.BlockAccessor == null) return;

            Vec2i coord = chunkCoord.Copy();
            api.Event.RegisterCallback(_ => StripColumnAt(api, coord), ChunkLoadDeferral.StripDelayMs(coord));
        }

        static void StripColumnAt(ICoreAPI api, Vec2i chunkCoord)
        {
            IBlockAccessor acc = api.World?.BlockAccessor;
            if (acc == null) return;

            List<BlockPos> positions = CollectLegacyPositions(acc, chunkCoord);
            StripAt(acc, positions);
        }

        static List<BlockPos> CollectLegacyPositions(IBlockAccessor acc, Vec2i chunkCoord)
        {
            var positions = new List<BlockPos>();
            int cx = chunkCoord.X;
            int cz = chunkCoord.Y;
            int maxCy = acc.MapSizeY / GlobalConstants.ChunkSize;

            for (int cy = 0; cy < maxCy; cy++)
            {
                IWorldChunk chunk = acc.GetChunk(cx, cy, cz);
                if (chunk?.BlockEntities == null || chunk.BlockEntities.Count == 0) continue;

                foreach (KeyValuePair<BlockPos, BlockEntity> kv in chunk.BlockEntities)
                {
                    if (kv.Value is LegacyModBlockEntityStripper)
                        positions.Add(kv.Key.Copy());
                }
            }

            return positions;
        }

        static void StripAt(IBlockAccessor acc, List<BlockPos> positions)
        {
            if (acc == null || positions == null || positions.Count == 0) return;

            foreach (BlockPos pos in positions)
            {
                try
                {
                    if (acc.GetBlockEntity(pos) is LegacyModBlockEntityStripper)
                        acc.RemoveBlockEntity(pos);
                }
                catch { /* chunk may already be unloading */ }
            }
        }
    }

    /// <summary>Deserializes legacy saves, then drops the BE so blocks stay vanilla.</summary>
    internal class LegacyModBlockEntityStripper : BlockEntity
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            BlockPos pos = Pos.Copy();
            int cx = pos.X / GlobalConstants.ChunkSize;
            int cz = pos.Z / GlobalConstants.ChunkSize;
            int delayMs = ChunkLoadDeferral.StripDelayMs(new Vec2i(cx, cz));
            api.Event.RegisterCallback(_ =>
            {
                try
                {
                    IBlockAccessor acc = api.World?.BlockAccessor;
                    if (acc?.GetBlockEntity(pos) is LegacyModBlockEntityStripper)
                        acc.RemoveBlockEntity(pos);
                }
                catch { /* chunk may already be unloaded */ }
            }, delayMs);
        }
    }
}
