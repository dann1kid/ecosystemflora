using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Shared calendar-timer queue for juvenile spread blocks maturing into vanilla parents.
    /// Storage, dedupe, removal, and the due-scan loop are common; subclasses supply the juvenile
    /// block identity and the divergent maturation step (e.g. flower phenology vs direct fern set).
    /// </summary>
    internal abstract class PendingMaturationQueue
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

        protected abstract bool IsJuvenileBlock(Block block);

        protected abstract string SpeciesFromJuvenile(Block block);

        /// <summary>Called once a juvenile is due, validated, and its mature block resolved. The
        /// entry is always removed afterwards, mirroring the original per-species behavior.</summary>
        protected abstract void OnMature(
            ICoreAPI api,
            EcosystemSystem ecosystem,
            IBlockAccessor acc,
            BlockPos pos,
            string species,
            Block mature,
            AssetLocation matureCode);

        public void Add(BlockPos pos, AssetLocation matureCode, string species, double matureAtHours)
        {
            if (pos == null || matureCode == null || string.IsNullOrEmpty(species)) return;
            if (indexByPos.ContainsKey(pos)) return;

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

                Block current = acc.GetBlock(entry.Pos);
                if (IsJuvenileBlock(current))
                {
                    PlantSnowCoverSync.TrySyncCover(api, entry.Pos, current);
                }

                if (nowHours < entry.MatureAtHours) continue;
                if (!IsJuvenileBlock(current))
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                string species = SpeciesFromJuvenile(current);
                if (species != entry.Species)
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                if (!LandClaimGuard.AllowsEcologyChange(api, entry.Pos)) continue;

                bool snow = PlantSnowCover.ResolveWantsSnowCover(api, entry.Pos);
                AssetLocation matureLoc = PlantSnowCover.CodeWithCover(entry.MatureCode, snow);
                Block mature = api.World.GetBlock(matureLoc);
                if (mature == null || mature.Id == 0)
                {
                    mature = api.World.GetBlock(entry.MatureCode);
                }

                if (mature == null)
                {
                    remove.Add(entry.Pos);
                    continue;
                }

                OnMature(api, ecosystem, acc, entry.Pos, species, mature, entry.MatureCode);
                remove.Add(entry.Pos);
            }

            for (int r = 0; r < remove.Count; r++)
            {
                Remove(remove[r]);
            }
        }
    }
}
