using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Tracks mod-placed saplings until vanilla treegen produces log-grown (then registers trunk base).</summary>
    internal sealed class PendingTreeSaplings
    {
        readonly List<Entry> entries = new List<Entry>();
        readonly Dictionary<BlockPos, int> indexByPos = new Dictionary<BlockPos, int>();

        struct Entry
        {
            public BlockPos Pos;
            public string Wood;
            public double RegisteredHours;
        }

        const double TimeoutHours = 120 * 24;
        const int MaxVerticalSearch = 28;

        public void Add(BlockPos saplingPos, string wood, double nowHours)
        {
            if (saplingPos == null || string.IsNullOrEmpty(wood)) return;

            if (indexByPos.ContainsKey(saplingPos))
            {
                return;
            }

            indexByPos[saplingPos] = entries.Count;
            entries.Add(new Entry
            {
                Pos = saplingPos.Copy(),
                Wood = wood,
                RegisteredHours = nowHours,
            });
        }

        public void Process(ICoreAPI api, EcosystemSystem ecosystem, double nowHours, int maxChecks)
        {
            if (entries.Count == 0 || api == null || ecosystem == null || maxChecks <= 0) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            int checkedCount = 0;
            var remove = new List<BlockPos>();

            for (int i = entries.Count - 1; i >= 0 && checkedCount < maxChecks; i--)
            {
                Entry entry = entries[i];
                checkedCount++;

                if (nowHours - entry.RegisteredHours > TimeoutHours)
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                Block at = acc.GetBlock(entry.Pos);
                if (PlantCodeHelper.IsTreeSaplingBlock(at))
                {
                    continue;
                }

                if (TryFindMatureBase(acc, entry.Pos, entry.Wood, out BlockPos basePos, out Block baseBlock))
                {
                    if (EcosystemParticipant.TryFromBlock(baseBlock, out IEcosystemParticipant participant))
                    {
                        ecosystem.RegisterReproducer(basePos, participant, spawnBurst: false);
                    }

                    remove.Add(entry.Pos);
                }
            }

            for (int r = 0; r < remove.Count; r++)
            {
                Remove(remove[r]);
            }
        }

        static bool TryFindMatureBase(IBlockAccessor acc, BlockPos origin, string wood, out BlockPos basePos, out Block baseBlock)
        {
            basePos = null;
            baseBlock = null;

            int x = origin.X;
            int z = origin.Z;
            int yStart = origin.Y - 2;
            int yEnd = origin.Y + MaxVerticalSearch;
            if (yStart < 0) yStart = 0;

            BlockPos found = null;
            Block foundBlock = null;

            for (int y = yStart; y <= yEnd; y++)
            {
                BlockPos test = new BlockPos(x, y, z);
                Block block = acc.GetBlock(test);
                if (!PlantCodeHelper.IsTreeLogGrownBlock(block)) continue;
                if (PlantCodeHelper.GetTreeWood(block) != wood) continue;

                if (found == null || y < found.Y)
                {
                    found = test.Copy();
                    foundBlock = block;
                }
            }

            if (found == null) return false;

            basePos = PlantCodeHelper.GetTreeTrunkBase(acc, found);
            baseBlock = acc.GetBlock(basePos);
            return PlantCodeHelper.IsTreeLogGrownBlock(baseBlock);
        }

        void Remove(BlockPos pos)
        {
            if (!indexByPos.TryGetValue(pos, out int index)) return;

            int last = entries.Count - 1;
            if (index != last)
            {
                Entry moved = entries[last];
                entries[index] = moved;
                indexByPos[moved.Pos] = index;
            }

            entries.RemoveAt(last);
            indexByPos.Remove(pos);
        }
    }
}
