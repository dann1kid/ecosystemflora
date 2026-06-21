using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Suppresses canopy budding into cells the player recently cleared.</summary>
    internal sealed class FoliagePlayerVacancySuppressor
    {
        /// <summary>Game hours before a player-cleared cell can receive seasonal buds again.</summary>
        internal const double DefaultSuppressHours = 240;

        const int MaxEntries = 8192;

        static readonly int[] NeighborDx = { 1, -1, 0, 0, 0, 0 };
        static readonly int[] NeighborDy = { 0, 0, 1, -1, 0, 0 };
        static readonly int[] NeighborDz = { 0, 0, 0, 0, 1, -1 };

        readonly Dictionary<long, double> untilHours = new Dictionary<long, double>();

        public void NotePlayerBreak(BlockPos pos, double nowHours, double durationHours = DefaultSuppressHours)
        {
            if (pos == null || durationHours <= 0) return;

            PruneIfNeeded(nowHours);
            double until = nowHours + durationHours;
            NotePos(pos.X, pos.Y, pos.Z, until);

            for (int i = 0; i < 6; i++)
            {
                NotePos(pos.X + NeighborDx[i], pos.Y + NeighborDy[i], pos.Z + NeighborDz[i], until);
            }
        }

        public bool BlocksBudAt(BlockPos pos, double nowHours)
        {
            if (pos == null) return false;

            long key = PosKey(pos.X, pos.Y, pos.Z);
            if (!untilHours.TryGetValue(key, out double until)) return false;

            if (nowHours >= until)
            {
                untilHours.Remove(key);
                return false;
            }

            return true;
        }

        public void Clear()
        {
            untilHours.Clear();
        }

        void NotePos(int x, int y, int z, double until)
        {
            untilHours[PosKey(x, y, z)] = until;
        }

        void PruneIfNeeded(double nowHours)
        {
            if (untilHours.Count < MaxEntries) return;

            var remove = new List<long>();
            foreach (KeyValuePair<long, double> kv in untilHours)
            {
                if (nowHours >= kv.Value) remove.Add(kv.Key);
            }

            for (int i = 0; i < remove.Count; i++)
            {
                untilHours.Remove(remove[i]);
            }

            if (untilHours.Count >= MaxEntries)
            {
                untilHours.Clear();
            }
        }

        static long PosKey(int x, int y, int z)
        {
            return ((long)x << 40) | ((long)(y & 0xFFFFFF) << 16) | (uint)(z & 0xFFFF);
        }
    }
}
