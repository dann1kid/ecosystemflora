using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-chunk index of ecology plants for spacing checks (avoids brute-force block scans).</summary>
    internal sealed class EcologySpacingIndex
    {
        readonly Dictionary<BlockPos, SpacingRecord> byPos = new Dictionary<BlockPos, SpacingRecord>();
        readonly Dictionary<Vec2i, List<SpacingRecord>> byChunk = new Dictionary<Vec2i, List<SpacingRecord>>();

        struct SpacingRecord
        {
            public BlockPos Pos;
            public string Species;
            public EcologyHabitat Habitat;
            public bool IsReed;
            public int ChunkListIndex;
        }

        public void Clear()
        {
            byPos.Clear();
            byChunk.Clear();
        }

        public void AddOrUpdate(IBlockAccessor acc, BlockPos pos)
        {
            if (acc == null || pos == null) return;

            Block block = acc.GetBlock(pos);
            if (!PlantCodeHelper.IsEcologyPlant(block)) return;

            string species = PlantCodeHelper.GetEcologySpecies(block.Code);
            if (string.IsNullOrEmpty(species)) return;

            Remove(pos);

            var record = new SpacingRecord
            {
                Pos = pos.Copy(),
                Species = species,
                Habitat = PlantCodeHelper.GetEcologyHabitat(block.Code),
                IsReed = PlantCodeHelper.IsReedBlock(block),
            };

            Vec2i chunk = ReproducerRegistry.ToChunkCoord(pos);
            if (!byChunk.TryGetValue(chunk, out List<SpacingRecord> list))
            {
                list = new List<SpacingRecord>();
                byChunk[chunk] = list;
            }

            record.ChunkListIndex = list.Count;
            list.Add(record);
            byPos[pos] = record;
        }

        public void Remove(BlockPos pos)
        {
            if (pos == null || !byPos.TryGetValue(pos, out SpacingRecord record)) return;

            Vec2i chunk = ReproducerRegistry.ToChunkCoord(pos);
            if (byChunk.TryGetValue(chunk, out List<SpacingRecord> list))
            {
                int idx = record.ChunkListIndex;
                int last = list.Count - 1;
                if (idx >= 0 && idx < list.Count)
                {
                    if (idx != last)
                    {
                        SpacingRecord swapped = list[last];
                        list[idx] = swapped;
                        swapped.ChunkListIndex = idx;
                        byPos[swapped.Pos] = swapped;
                    }

                    list.RemoveAt(last);
                }

                if (list.Count == 0)
                {
                    byChunk.Remove(chunk);
                }
            }

            byPos.Remove(pos);
        }

        public void RemoveChunk(Vec2i chunkCoord)
        {
            if (!byChunk.TryGetValue(chunkCoord, out List<SpacingRecord> list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                byPos.Remove(list[i].Pos);
            }

            byChunk.Remove(chunkCoord);
        }

        public bool MeetsSpacing(
            BlockPos candidatePos,
            PlantRequirements requirements,
            EcosystemConfig cfg,
            out string failureReason)
        {
            failureReason = null;
            if (cfg == null || !cfg.PlantSpacingEnabled) return true;
            if (requirements == null || string.IsNullOrEmpty(requirements.Species)) return true;

            int searchRadius = requirements.GetSpacingSearchRadius(cfg);
            if (searchRadius <= 0) return true;

            int y0 = candidatePos.Y - cfg.SpacingVerticalSearch;
            int y1 = candidatePos.Y + cfg.SpacingVerticalSearch;
            int chunkSize = GlobalConstants.ChunkSize;

            int minCx = (candidatePos.X - searchRadius) / chunkSize;
            int maxCx = (candidatePos.X + searchRadius) / chunkSize;
            int minCz = (candidatePos.Z - searchRadius) / chunkSize;
            int maxCz = (candidatePos.Z + searchRadius) / chunkSize;

            for (int cx = minCx; cx <= maxCx; cx++)
            {
                for (int cz = minCz; cz <= maxCz; cz++)
                {
                    if (!byChunk.TryGetValue(new Vec2i(cx, cz), out List<SpacingRecord> list))
                    {
                        continue;
                    }

                    for (int i = 0; i < list.Count; i++)
                    {
                        SpacingRecord other = list[i];
                        BlockPos checkPos = other.Pos;

                        if (checkPos.X == candidatePos.X
                            && checkPos.Y == candidatePos.Y
                            && checkPos.Z == candidatePos.Z)
                        {
                            continue;
                        }

                        if (checkPos.Y < y0 || checkPos.Y > y1) continue;

                        if (!PlantSpacing.ShouldApplySpacingBetween(
                            requirements.Habitat, other.Habitat, cfg))
                        {
                            continue;
                        }

                        int required = requirements.GetRequiredSpacingTo(other.Species, cfg);
                        if (required <= 0) continue;

                        int dist = HorizontalChebyshev(candidatePos, checkPos);
                        bool sameColumn = candidatePos.X == checkPos.X && candidatePos.Z == checkPos.Z;

                        if (sameColumn && other.Species == requirements.Species && other.IsReed)
                        {
                            failureReason = "Reed already in column at y=" + checkPos.Y;
                            return false;
                        }

                        if (dist < required)
                        {
                            failureReason = "Too close to " + other.Species
                                + " (dist " + dist + ", need " + required + ")";
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        static int HorizontalChebyshev(BlockPos a, BlockPos b)
        {
            int dx = System.Math.Abs(a.X - b.X);
            int dz = System.Math.Abs(a.Z - b.Z);
            return System.Math.Max(dx, dz);
        }
    }
}
