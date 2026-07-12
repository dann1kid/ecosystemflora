using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Wild berry bushes spread as mature blocks then reset to cutting state; register after calendar maturation.
    /// </summary>
    internal sealed class PendingBerryMaturation
    {
        readonly List<Entry> entries = new List<Entry>();
        readonly Dictionary<BlockPos, int> indexByPos = new Dictionary<BlockPos, int>();

        struct Entry
        {
            public BlockPos Pos;
            public string Species;
            public double MatureAtHours;
        }

        public void Add(BlockPos pos, string species, double matureAtHours)
        {
            if (pos == null || string.IsNullOrEmpty(species)) return;
            if (indexByPos.ContainsKey(pos)) return;

            indexByPos[pos] = entries.Count;
            entries.Add(new Entry
            {
                Pos = pos.Copy(),
                Species = species,
                MatureAtHours = matureAtHours,
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

        public void Process(ICoreAPI api, EcosystemSystem ecosystem, double nowHours, int maxChecks)
        {
            if (entries.Count == 0 || api == null || ecosystem == null || maxChecks <= 0) return;
            if (!BerrySpreadMaturation.UsesMaturation(EcosystemConfig.Loaded)) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            var remove = new List<BlockPos>();
            int checkedCount = 0;

            for (int i = entries.Count - 1; i >= 0 && checkedCount < maxChecks; i--)
            {
                Entry entry = entries[i];
                checkedCount++;

                if (nowHours < entry.MatureAtHours) continue;

                Block block = acc.GetBlock(entry.Pos);
                if (block == null || block.Id == 0
                    || PlantCodeHelper.ResolveEcologySpecies(block) != entry.Species)
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                if (!LandClaimGuard.AllowsEcologyChange(api, entry.Pos)) continue;

                if (ecosystem.RegistryContains(entry.Pos))
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant))
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                if (!ecosystem.RegisterReproducer(entry.Pos, participant, spawnBurst: false)
                    || !ecosystem.RegistryContains(entry.Pos))
                {
                    continue;
                }

                ecosystem.InvalidateEnvironmentAround(entry.Pos);
                EcologyHistoryRecorder.RecordSpread(api, entry.Pos, entry.Species);
                remove.Add(entry.Pos);
            }

            for (int r = 0; r < remove.Count; r++)
            {
                Remove(remove[r]);
            }
        }
    }
}
