using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Advances establishing tallgrass toward full environment target height.
    /// Registers for spread at half-target, but keeps promoting until full target.
    /// </summary>
    internal sealed class PendingTallgrassPromotion
    {
        readonly List<Entry> entries = new List<Entry>();
        readonly Dictionary<BlockPos, int> indexByPos = new Dictionary<BlockPos, int>();
        int nextScanIndex;

        struct Entry
        {
            public BlockPos Pos;
            public int TargetStageIndex;
            public double NextAdvanceAtHours;
            /// <summary>Set while due work keeps failing (claim / SetBlock); cleared on success.</summary>
            public double StuckSinceHours;
        }

        const double TimeoutHours = 60 * 24 * 14;

        public int Count => entries.Count;

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
                StuckSinceHours = 0,
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

            if (nextScanIndex > entries.Count)
            {
                nextScanIndex = 0;
            }
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
            int count = entries.Count;
            int start = nextScanIndex % count;
            if (start < 0) start = 0;
            int walked = 0;

            // Round-robin: not-due cells do not burn the advance budget, so dense meadows
            // cannot starve older establishing grass (which previously timed out short).
            for (; walked < count && checkedCount < maxChecks; walked++)
            {
                int i = (start + walked) % count;
                Entry entry = entries[i];
                BlockPos pos = entry.Pos;

                Block block = acc.GetBlock(pos);
                if (block == null || block.Id == 0
                    || PlantCodeHelper.ResolveEcologySpecies(block) != "tallgrass")
                {
                    remove.Add(pos);
                    checkedCount++;
                    continue;
                }

                if (!LandClaimGuard.AllowsEcologyChange(api, pos))
                {
                    if (nowHours >= entry.NextAdvanceAtHours
                        && MarkStuck(ref entry, nowHours, TimeoutHours))
                    {
                        remove.Add(pos);
                    }

                    entries[i] = entry;
                    if (nowHours >= entry.NextAdvanceAtHours) checkedCount++;
                    continue;
                }

                // Spread registry opens at half target; height promotion continues to full target.
                EnsureRegisteredIfReady(ecosystem, acc, pos, block, entry.TargetStageIndex);

                if (!TallgrassEstablishment.NeedsEstablishment(api, pos, block, out int refreshedTarget))
                {
                    EnsureRegisteredIfReady(ecosystem, acc, pos, block, entry.TargetStageIndex);
                    remove.Add(pos);
                    checkedCount++;
                    continue;
                }

                if (refreshedTarget > entry.TargetStageIndex)
                {
                    entry.TargetStageIndex = refreshedTarget;
                }

                if (nowHours < entry.NextAdvanceAtHours)
                {
                    entry.StuckSinceHours = 0;
                    entries[i] = entry;
                    continue;
                }

                checkedCount++;

                if (!TallgrassSpreadHeight.TryAdvanceOneStage(api, acc, pos))
                {
                    if (MarkStuck(ref entry, nowHours, TimeoutHours))
                    {
                        remove.Add(pos);
                    }
                    else
                    {
                        entry.NextAdvanceAtHours = nowHours + WildTallgrassMaturation.StageAdvanceHours(api, pos, cfg);
                        entries[i] = entry;
                    }

                    continue;
                }

                entry.StuckSinceHours = 0;
                ecosystem.InvalidateEnvironmentAround(pos);
                block = acc.GetBlock(pos);
                EnsureRegisteredIfReady(ecosystem, acc, pos, block, entry.TargetStageIndex);

                if (!TallgrassEstablishment.NeedsEstablishment(api, pos, block, out _))
                {
                    remove.Add(pos);
                }
                else
                {
                    entry.NextAdvanceAtHours = nowHours + WildTallgrassMaturation.StageAdvanceHours(api, pos, cfg);
                    entries[i] = entry;
                }
            }

            nextScanIndex = count == 0 ? 0 : (start + walked) % count;

            for (int r = 0; r < remove.Count; r++)
            {
                Remove(remove[r]);
            }
        }

        static bool MarkStuck(ref Entry entry, double nowHours, double timeoutHours)
        {
            if (entry.StuckSinceHours <= 0)
            {
                entry.StuckSinceHours = nowHours;
                return false;
            }

            return IsStuckPastTimeout(entry.StuckSinceHours, nowHours, timeoutHours);
        }

        /// <summary>Test/helper: removal only after due work keeps failing, not after queue starvation.</summary>
        internal static bool IsStuckPastTimeout(double stuckSinceHours, double nowHours, double timeoutHours) =>
            stuckSinceHours > 0 && nowHours - stuckSinceHours > timeoutHours;

        static void EnsureRegisteredIfReady(
            EcosystemSystem ecosystem,
            IBlockAccessor acc,
            BlockPos pos,
            Block block,
            int targetStageIndex)
        {
            if (ecosystem.RegistryContains(pos)) return;
            if (!TallgrassEstablishment.IsReadyToRegister(block, targetStageIndex, null, pos)) return;

            TryRegister(ecosystem, acc, pos);
        }

        static bool TryRegister(EcosystemSystem ecosystem, IBlockAccessor acc, BlockPos pos)
        {
            if (ecosystem.RegistryContains(pos)) return true;

            Block block = acc.GetBlock(pos);
            if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant))
            {
                return false;
            }

            if (!ecosystem.RegisterReproducer(pos, participant, spawnBurst: false)
                || !ecosystem.RegistryContains(pos))
            {
                return false;
            }

            ecosystem.InvalidateEnvironmentAround(pos);
            return true;
        }
    }
}
