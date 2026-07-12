using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Advances establishing tallgrass toward a condition-based target height, then registers.</summary>
    internal sealed class PendingTallgrassPromotion
    {
        readonly List<Entry> entries = new List<Entry>();
        readonly Dictionary<BlockPos, int> indexByPos = new Dictionary<BlockPos, int>();

        struct Entry
        {
            public BlockPos Pos;
            public int TargetStageIndex;
            public double NextAdvanceAtHours;
        }

        const double TimeoutHours = 60 * 24 * 14;

        public void Add(ICoreAPI api, BlockPos pos)
        {
            if (api == null || pos == null) return;

            Block block = api.World.BlockAccessor.GetBlock(pos);
            if (!TallgrassEstablishment.NeedsEstablishment(api, pos, block, out int targetStageIndex))
            {
                return;
            }

            if (indexByPos.ContainsKey(pos)) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            double nextAt = api.World.Calendar.TotalHours
                + WildTallgrassMaturation.StageAdvanceHours(api, pos, cfg);

            indexByPos[pos] = entries.Count;
            entries.Add(new Entry
            {
                Pos = pos.Copy(),
                TargetStageIndex = targetStageIndex,
                NextAdvanceAtHours = nextAt,
            });
        }

        public void Remove(BlockPos pos)
        {
            if (pos == null || !indexByPos.TryGetValue(pos, out int index)) return;

            int last = entries.Count - 1;
            BlockPos removed = entries[index].Pos;
            if (index != last)
            {
                Entry moved = entries[last];
                entries[index] = moved;
                indexByPos[moved.Pos] = index;
            }

            entries.RemoveAt(last);
            indexByPos.Remove(removed);
        }

        public bool TryGetQueuedEntry(BlockPos pos, out int targetStageIndex, out double nextAdvanceAtHours)
        {
            targetStageIndex = -1;
            nextAdvanceAtHours = 0;
            if (pos == null || !indexByPos.TryGetValue(pos, out int index)) return false;

            Entry entry = entries[index];
            targetStageIndex = entry.TargetStageIndex;
            nextAdvanceAtHours = entry.NextAdvanceAtHours;
            return true;
        }

        public void Process(ICoreAPI api, EcosystemSystem ecosystem, double nowHours, int maxChecks)
        {
            if (entries.Count == 0 || api == null || ecosystem == null || maxChecks <= 0) return;
            if (!TallgrassEstablishment.UsesEstablishment(EcosystemConfig.Loaded)) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            var remove = new List<BlockPos>();
            int checkedCount = 0;

            for (int i = entries.Count - 1; i >= 0 && checkedCount < maxChecks; i--)
            {
                Entry entry = entries[i];
                BlockPos pos = entry.Pos;
                checkedCount++;

                if (nowHours - entry.NextAdvanceAtHours > TimeoutHours)
                {
                    remove.Add(pos);
                    continue;
                }

                Block block = acc.GetBlock(pos);
                if (block == null || block.Id == 0
                    || PlantCodeHelper.ResolveEcologySpecies(block) != "tallgrass")
                {
                    remove.Add(pos);
                    continue;
                }

                if (!LandClaimGuard.AllowsEcologyChange(api, pos))
                {
                    continue;
                }

                if (TallgrassEstablishment.IsReadyToRegister(block, entry.TargetStageIndex, api, pos))
                {
                    TryRegister(ecosystem, acc, pos, remove);
                    continue;
                }

                if (!TallgrassEstablishment.NeedsEstablishment(api, pos, block, out int refreshedTarget))
                {
                    TryRegister(ecosystem, acc, pos, remove);
                    continue;
                }

                if (refreshedTarget > entry.TargetStageIndex)
                {
                    entry.TargetStageIndex = refreshedTarget;
                }

                if (nowHours < entry.NextAdvanceAtHours)
                {
                    entries[i] = entry;
                    continue;
                }

                if (!TallgrassSpreadHeight.TryAdvanceOneStage(api, acc, pos))
                {
                    entry.NextAdvanceAtHours = nowHours + WildTallgrassMaturation.StageAdvanceHours(api, pos, cfg);
                    entries[i] = entry;
                    continue;
                }

                ecosystem.InvalidateEnvironmentAround(pos);
                block = acc.GetBlock(pos);
                if (TallgrassEstablishment.IsReadyToRegister(block, entry.TargetStageIndex, api, pos))
                {
                    TryRegister(ecosystem, acc, pos, remove);
                }
                else
                {
                    entry.NextAdvanceAtHours = nowHours + WildTallgrassMaturation.StageAdvanceHours(api, pos, cfg);
                    entries[i] = entry;
                }
            }

            for (int r = 0; r < remove.Count; r++)
            {
                Remove(remove[r]);
            }
        }

        static bool TryRegister(EcosystemSystem ecosystem, IBlockAccessor acc, BlockPos pos, List<BlockPos> remove)
        {
            if (ecosystem.RegistryContains(pos))
            {
                remove.Add(pos);
                return true;
            }

            Block block = acc.GetBlock(pos);
            if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant))
            {
                remove.Add(pos);
                return true;
            }

            if (!ecosystem.RegisterReproducer(pos, participant, spawnBurst: false)
                || !ecosystem.RegistryContains(pos))
            {
                return false;
            }

            ecosystem.InvalidateEnvironmentAround(pos);
            remove.Add(pos);
            return true;
        }
    }
}
