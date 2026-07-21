using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Registers vanilla mycelium BE anchors when chunks finish loading.</summary>
    internal static class MyceliumChunkRegistrar
    {
        public static void ScheduleScanColumn(ICoreAPI api, Vec2i chunkCoord)
        {
            if (api?.World?.BlockAccessor == null) return;
            if (!EcosystemConfig.Loaded.EnableMyceliumEcology) return;

            Vec2i coord = chunkCoord.Copy();
            // Later than remap base so mass loads do not stack BE walk + surface remap.
            api.Event.RegisterCallback(_ => ScanColumnAt(api, coord), ChunkLoadDeferral.MyceliumDelayMs(coord));
        }

        static void ScanColumnAt(ICoreAPI api, Vec2i chunkCoord)
        {
            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null || api?.World?.BlockAccessor == null) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            foreach (BlockPos pos in CollectMyceliumPositions(acc, chunkCoord))
            {
                if (eco.RegistryContains(pos)) continue;

                BlockEntity be = acc.GetBlockEntity(pos);
                if (!MyceliumAnchorReader.TryReadMushroomCode(be, out AssetLocation mushroomCode)) continue;

                Block anchorBlock = acc.GetBlock(pos);
                if (!MyceliumEcology.TryBuildRequirements(mushroomCode, anchorBlock, out PlantRequirements req)) continue;

                eco.RegisterMyceliumAnchor(pos, mushroomCode, req);
            }
        }

        static List<BlockPos> CollectMyceliumPositions(IBlockAccessor acc, Vec2i chunkCoord)
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
                    if (MyceliumAnchorReader.IsMyceliumBlockEntity(kv.Value))
                    {
                        positions.Add(kv.Key.Copy());
                    }
                }
            }

            return positions;
        }
    }
}
