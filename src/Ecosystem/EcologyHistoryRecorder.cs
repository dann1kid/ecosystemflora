using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Network;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Short-lived ecology events per block for the history hint UX (server-side ring buffer).
    /// </summary>
    internal static class EcologyHistoryRecorder
    {
        const int MaxEventsPerBlock = 3;
        const double TtlHours = 24 * 14;

        struct EventRecord
        {
            public string LangKey;
            public string[] Args;
            public double AtHours;
        }

        static readonly Dictionary<long, List<EventRecord>> byPos = new Dictionary<long, List<EventRecord>>();

        public static void Record(ICoreAPI api, BlockPos pos, string langKey, params string[] args)
        {
            if (api?.World?.Calendar == null || pos == null || string.IsNullOrEmpty(langKey)) return;

            double now = api.World.Calendar.TotalHours;
            long key = PosKey(pos);
            if (!byPos.TryGetValue(key, out List<EventRecord> list))
            {
                list = new List<EventRecord>(MaxEventsPerBlock);
                byPos[key] = list;
            }

            list.Add(new EventRecord
            {
                LangKey = langKey,
                Args = args ?? Array.Empty<string>(),
                AtHours = now,
            });

            PruneList(list, now);
            while (list.Count > MaxEventsPerBlock)
            {
                list.RemoveAt(0);
            }
        }

        public static void RecordOrphanDieback(ICoreAPI api, BlockPos pos, string species) =>
            Record(api, pos, "ecosystemflora:history-orphan-dieback", species ?? string.Empty);

        public static void RecordStressDieback(ICoreAPI api, BlockPos pos, string species) =>
            Record(api, pos, "ecosystemflora:history-stress-dieback", species ?? string.Empty);

        public static void RecordStressDeath(ICoreAPI api, BlockPos pos, string species) =>
            Record(api, pos, "ecosystemflora:history-stress-death", species ?? string.Empty);

        public static void RecordPhenologySenescence(ICoreAPI api, BlockPos pos, string species) =>
            Record(api, pos, "ecosystemflora:history-phenology-senescence", species ?? string.Empty);

        public static void RecordSpread(ICoreAPI api, BlockPos pos, string species) =>
            Record(api, pos, "ecosystemflora:history-spread", species ?? string.Empty);

        public static bool TryBuildHintLines(ICoreAPI api, BlockPos pos, List<InspectLineLite> output, int maxLines = 2)
        {
            if (api?.World?.Calendar == null || pos == null || output == null) return false;

            long key = PosKey(pos);
            if (!byPos.TryGetValue(key, out List<EventRecord> list) || list.Count == 0) return false;

            double now = api.World.Calendar.TotalHours;
            PruneList(list, now);
            if (list.Count == 0)
            {
                byPos.Remove(key);
                return false;
            }

            int start = Math.Max(0, list.Count - maxLines);
            for (int i = start; i < list.Count; i++)
            {
                EventRecord ev = list[i];
                output.Add(new InspectLineLite
                {
                    Key = ev.LangKey,
                    Args = ev.Args,
                });
            }

            return output.Count > 0;
        }

        public static void Remove(BlockPos pos)
        {
            if (pos == null) return;
            byPos.Remove(PosKey(pos));
        }

        static void PruneList(List<EventRecord> list, double nowHours)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (nowHours - list[i].AtHours > TtlHours)
                {
                    list.RemoveAt(i);
                }
            }
        }

        static long PosKey(BlockPos pos)
        {
            unchecked
            {
                return ((long)pos.X << 42) | ((long)(pos.Y & 0x1FFF) << 29) | (uint)pos.Z;
            }
        }
    }
}
