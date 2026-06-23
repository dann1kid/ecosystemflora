using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Juvenile spread blocks mature into vanilla parents on a calendar timer.</summary>
    internal sealed class PendingFlowerMaturation
    {
        readonly List<Entry> entries = new List<Entry>();
        readonly Dictionary<BlockPos, int> indexByPos = new Dictionary<BlockPos, int>();

        struct Entry
        {
            public BlockPos Pos;
            public AssetLocation MatureCode;
            public string Species;
            public double MatureAtHours;
        }

        public void Add(BlockPos pos, AssetLocation matureCode, string species, double matureAtHours)
        {
            if (pos == null || matureCode == null || string.IsNullOrEmpty(species)) return;

            if (indexByPos.ContainsKey(pos))
            {
                return;
            }

            indexByPos[pos] = entries.Count;
            entries.Add(new Entry
            {
                Pos = pos.Copy(),
                MatureCode = matureCode.Clone(),
                Species = species,
                MatureAtHours = matureAtHours,
            });
        }

        public void Remove(BlockPos pos)
        {
            if (pos == null || !indexByPos.TryGetValue(pos, out int index)) return;

            int last = entries.Count - 1;
            Entry removed = entries[index];
            if (index != last)
            {
                Entry moved = entries[last];
                entries[index] = moved;
                indexByPos[moved.Pos] = index;
            }

            entries.RemoveAt(last);
            indexByPos.Remove(removed.Pos);
        }

        public bool TryGetHoursUntilMature(BlockPos pos, double nowHours, out double hoursLeft)
        {
            hoursLeft = 0;
            if (pos == null || !indexByPos.TryGetValue(pos, out int index)) return false;

            hoursLeft = entries[index].MatureAtHours - nowHours;
            if (hoursLeft < 0) hoursLeft = 0;
            return true;
        }

        public void Process(ICoreAPI api, EcosystemSystem ecosystem, double nowHours, int maxChecks)
        {
            if (entries.Count == 0 || api == null || ecosystem == null || maxChecks <= 0) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            var remove = new List<BlockPos>();
            int checkedCount = 0;

            for (int i = entries.Count - 1; i >= 0 && checkedCount < maxChecks; i--)
            {
                Entry entry = entries[i];
                checkedCount++;

                if (nowHours < entry.MatureAtHours)
                {
                    continue;
                }

                Block current = acc.GetBlock(entry.Pos);
                if (!FlowerJuvenileBlocks.IsJuvenileBlock(current))
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                string species = FlowerJuvenileBlocks.SpeciesFromJuvenile(current);
                if (species != entry.Species)
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                if (!LandClaimGuard.AllowsEcologyChange(api, entry.Pos))
                {
                    continue;
                }

                Block mature = api.World.GetBlock(entry.MatureCode);
                if (mature == null)
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                acc.SetBlock(mature.BlockId, entry.Pos);
                acc.MarkBlockDirty(entry.Pos);

                if (EcosystemParticipant.TryFromBlock(acc.GetBlock(entry.Pos), out IEcosystemParticipant participant))
                {
                    ecosystem.RegisterReproducer(entry.Pos, participant, spawnBurst: false);
                }

                ecosystem.InvalidateEnvironmentAround(entry.Pos);
                remove.Add(entry.Pos);
            }

            for (int r = 0; r < remove.Count; r++)
            {
                Remove(remove[r]);
            }
        }
    }
}
