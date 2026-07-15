using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace WildFarming.Ecosystem
{
    [ProtoContract]
    internal sealed class ColumnTrafficSaveRoot
    {
        [ProtoMember(1)]
        public List<ColumnTrafficRecord> Records { get; set; } = new List<ColumnTrafficRecord>();
    }

    [ProtoContract]
    internal sealed class ColumnTrafficRecord
    {
        [ProtoMember(1)] public int X;
        [ProtoMember(2)] public int Z;
        [ProtoMember(3)] public int Dimension;
        /// <summary>0–255 packed foot-traffic pressure.</summary>
        [ProtoMember(4)] public byte Pressure;
        [ProtoMember(5)] public double LastTouchedHours;
        [ProtoMember(6)] public double LastDecayHours;
        /// <summary>Pressure level at which soil succession was last applied.</summary>
        [ProtoMember(7)] public byte LastSoilPressure;
        [ProtoMember(8)] public byte PlantStepHits;
    }

    /// <summary>
    /// Persisted per-column (XZ) foot-traffic pressure. Environmental context for trails —
    /// not a DisturbedTracker / colonizer window.
    /// </summary>
    internal sealed class ColumnTrafficStore
    {
        const string SaveKey = "ecosystemflora-column-traffic-v1";
        const int MaxRecords = 65536;

        readonly Dictionary<long, ColumnTrafficRecord> records = new Dictionary<long, ColumnTrafficRecord>();

        ICoreServerAPI sapi;

        int deferredSyncIndex;

        public int Count => records.Count;

        public void Bind(ICoreServerAPI serverApi)
        {
            sapi = serverApi;
            serverApi.Event.SaveGameLoaded += OnSaveGameLoaded;
            serverApi.Event.GameWorldSave += OnGameWorldSave;
        }

        public void Unbind(ICoreServerAPI serverApi)
        {
            if (serverApi == null) return;
            serverApi.Event.SaveGameLoaded -= OnSaveGameLoaded;
            serverApi.Event.GameWorldSave -= OnGameWorldSave;
            sapi = null;
        }

        public void Clear() => records.Clear();

        public float GetPressure01(BlockPos pos, double nowHours, float hoursPerDay, float decayPerDay)
        {
            if (pos == null) return 0f;
            return GetPressure01(pos.X, pos.Z, pos.dimension, nowHours, hoursPerDay, decayPerDay);
        }

        public float GetPressure01(int x, int z, int dimension, double nowHours, float hoursPerDay, float decayPerDay)
        {
            long key = Key(x, z, dimension);
            if (!records.TryGetValue(key, out ColumnTrafficRecord rec)) return 0f;

            ApplyLazyDecay(rec, nowHours, hoursPerDay, decayPerDay);
            if (rec.Pressure == 0)
            {
                if (rec.LastSoilPressure == 0 && rec.PlantStepHits == 0)
                {
                    records.Remove(key);
                }

                return 0f;
            }

            return rec.Pressure / 255f;
        }

        public byte GetPressureByte(BlockPos pos, double nowHours, float hoursPerDay, float decayPerDay)
        {
            if (pos == null) return 0;
            long key = Key(pos.X, pos.Z, pos.dimension);
            if (!records.TryGetValue(key, out ColumnTrafficRecord rec)) return 0;

            ApplyLazyDecay(rec, nowHours, hoursPerDay, decayPerDay);
            if (rec.Pressure == 0)
            {
                if (rec.LastSoilPressure == 0 && rec.PlantStepHits == 0)
                {
                    records.Remove(key);
                }

                return 0;
            }

            return rec.Pressure;
        }

        /// <summary>Adds pressure from a footstep. Returns updated 0–255 pressure.</summary>
        public byte AddPressure(
            BlockPos pos,
            int amount,
            double nowHours,
            float hoursPerDay,
            float decayPerDay)
        {
            if (pos == null || amount <= 0) return 0;

            long key = Key(pos.X, pos.Z, pos.dimension);
            if (!records.TryGetValue(key, out ColumnTrafficRecord rec))
            {
                if (records.Count >= MaxRecords) EvictLowest();
                rec = new ColumnTrafficRecord
                {
                    X = pos.X,
                    Z = pos.Z,
                    Dimension = pos.dimension,
                    LastDecayHours = nowHours,
                };
                records[key] = rec;
            }
            else
            {
                ApplyLazyDecay(rec, nowHours, hoursPerDay, decayPerDay);
            }

            int next = rec.Pressure + amount;
            if (next > 255) next = 255;
            rec.Pressure = (byte)next;
            rec.LastTouchedHours = nowHours;
            return rec.Pressure;
        }

        public byte IncrementPlantHits(BlockPos pos, double nowHours, float hoursPerDay, float decayPerDay)
        {
            if (pos == null) return 0;
            EnsureRecord(pos, nowHours, hoursPerDay, decayPerDay, out ColumnTrafficRecord rec, out _);
            if (rec.PlantStepHits < 255) rec.PlantStepHits++;
            rec.LastTouchedHours = nowHours;
            return rec.PlantStepHits;
        }

        public void ClearPlantHits(BlockPos pos)
        {
            if (pos == null) return;
            long key = Key(pos.X, pos.Z, pos.dimension);
            if (records.TryGetValue(key, out ColumnTrafficRecord rec))
            {
                rec.PlantStepHits = 0;
            }
        }

        public bool TryConsumeSoilWear(
            BlockPos pos,
            byte pressure,
            byte wearStep,
            double nowHours,
            float hoursPerDay,
            float decayPerDay)
        {
            if (pos == null || wearStep == 0 || pressure < wearStep) return false;

            EnsureRecord(pos, nowHours, hoursPerDay, decayPerDay, out ColumnTrafficRecord rec, out _);
            int nextMark = rec.LastSoilPressure + wearStep;
            if (pressure < nextMark) return false;

            // Advance by one wear step (not jump to current pressure) so large
            // PressurePerStep increments still apply multiple soil wears.
            rec.LastSoilPressure = nextMark > 255 ? (byte)255 : (byte)nextMark;
            rec.LastTouchedHours = nowHours;
            return true;
        }

        public bool TryGetRecordSnapshot(
            BlockPos pos,
            double nowHours,
            float hoursPerDay,
            float decayPerDay,
            out byte pressure,
            out byte plantHits,
            out byte lastSoilPressure)
        {
            pressure = 0;
            plantHits = 0;
            lastSoilPressure = 0;
            if (pos == null) return false;

            long key = Key(pos.X, pos.Z, pos.dimension);
            if (!records.TryGetValue(key, out ColumnTrafficRecord rec)) return false;

            ApplyLazyDecay(rec, nowHours, hoursPerDay, decayPerDay);
            if (rec.Pressure == 0 && rec.PlantStepHits == 0 && rec.LastSoilPressure == 0)
            {
                records.Remove(key);
                return false;
            }

            pressure = rec.Pressure;
            plantHits = rec.PlantStepHits;
            lastSoilPressure = rec.LastSoilPressure;
            return true;
        }

        /// <summary>Test helper — seed pressure without calendar.</summary>
        internal void SetPressureForTests(int x, int z, int dimension, byte pressure)
        {
            long key = Key(x, z, dimension);
            records[key] = new ColumnTrafficRecord
            {
                X = x,
                Z = z,
                Dimension = dimension,
                Pressure = pressure,
                LastTouchedHours = 0,
                LastDecayHours = 0,
            };
        }

        void EnsureRecord(
            BlockPos pos,
            double nowHours,
            float hoursPerDay,
            float decayPerDay,
            out ColumnTrafficRecord rec,
            out long key)
        {
            key = Key(pos.X, pos.Z, pos.dimension);
            if (!records.TryGetValue(key, out rec))
            {
                if (records.Count >= MaxRecords) EvictLowest();
                rec = new ColumnTrafficRecord
                {
                    X = pos.X,
                    Z = pos.Z,
                    Dimension = pos.dimension,
                    LastDecayHours = nowHours,
                };
                records[key] = rec;
            }
            else
            {
                ApplyLazyDecay(rec, nowHours, hoursPerDay, decayPerDay);
            }
        }

        static void ApplyLazyDecay(ColumnTrafficRecord rec, double nowHours, float hoursPerDay, float decayPerDay)
        {
            if (rec == null || decayPerDay <= 0f || hoursPerDay <= 0f) return;
            if (nowHours <= rec.LastDecayHours) return;

            double days = (nowHours - rec.LastDecayHours) / hoursPerDay;
            int decay = (int)(days * decayPerDay);
            if (decay <= 0) return;

            int next = rec.Pressure - decay;
            rec.Pressure = next <= 0 ? (byte)0 : (byte)next;
            rec.LastDecayHours = nowHours;
            if (rec.Pressure == 0)
            {
                rec.PlantStepHits = 0;
                // LastSoilPressure kept until TrafficCoverageSync restores coverage (on step or save).
            }
        }

        public void SetLastSoilPressure(BlockPos pos, byte lastSoilPressure)
        {
            if (pos == null) return;
            long key = Key(pos.X, pos.Z, pos.dimension);
            if (!records.TryGetValue(key, out ColumnTrafficRecord rec)) return;
            rec.LastSoilPressure = lastSoilPressure;
        }

        public bool TryGetLastSoilPressure(BlockPos pos, out byte lastSoilPressure)
        {
            lastSoilPressure = 0;
            if (pos == null) return false;
            long key = Key(pos.X, pos.Z, pos.dimension);
            if (!records.TryGetValue(key, out ColumnTrafficRecord rec)) return false;
            lastSoilPressure = rec.LastSoilPressure;
            return true;
        }

        /// <summary>
        /// Apply lazy decay to every record; sync coverage only when the soil mark is stale
        /// (and the column chunk is loaded). Caps block syncs per call to keep autosave snappy.
        /// </summary>
        public void AgeAllAndPrune(
            ICoreAPI api,
            double nowHours,
            float hoursPerDay,
            float decayPerDay,
            bool syncCoverage,
            byte wearStep)
        {
            if (records.Count == 0) return;

            const int MaxCoverageSyncsPerSave = 256;

            var remove = new List<long>();
            int syncBudget = syncCoverage && api != null ? MaxCoverageSyncsPerSave : 0;

            foreach (KeyValuePair<long, ColumnTrafficRecord> kv in records)
            {
                ColumnTrafficRecord rec = kv.Value;
                if (rec == null)
                {
                    remove.Add(kv.Key);
                    continue;
                }

                ApplyLazyDecay(rec, nowHours, hoursPerDay, decayPerDay);

                byte targetMark = FootTrafficWear.MarkForWearIndex(
                    FootTrafficWear.TargetWearIndex(rec.Pressure, wearStep),
                    wearStep);

                if (syncBudget > 0 && rec.LastSoilPressure != targetMark)
                {
                    if (IsColumnChunkLoaded(api, rec.X, rec.Z, rec.Dimension)
                        && TrafficCoverageSync.SyncColumnXZ(
                            api,
                            rec.X,
                            rec.Z,
                            rec.Dimension,
                            rec.Pressure,
                            wearStep))
                    {
                        rec.LastSoilPressure = targetMark;
                        syncBudget--;
                    }
                }

                if (rec.Pressure == 0 && rec.PlantStepHits == 0 && rec.LastSoilPressure == 0)
                {
                    remove.Add(kv.Key);
                }
            }

            for (int i = 0; i < remove.Count; i++)
            {
                records.Remove(remove[i]);
            }
        }

        /// <summary>
        /// Spread stale soil coverage sync across ticks (replaces save-time SetBlock bursts).
        /// </summary>
        public int ProcessDeferredCoverageSync(
            ICoreAPI api,
            double nowHours,
            float hoursPerDay,
            float decayPerDay,
            byte wearStep,
            int maxSyncs)
        {
            if (maxSyncs <= 0 || api == null || records.Count == 0) return 0;
            if (!EcosystemConfig.Loaded.TramplingSoilDegradation) return 0;

            int count = records.Count;
            if (deferredSyncIndex >= count) deferredSyncIndex = 0;

            int synced = 0;
            int walked = 0;

            foreach (KeyValuePair<long, ColumnTrafficRecord> kv in records)
            {
                if (walked < deferredSyncIndex)
                {
                    walked++;
                    continue;
                }

                ColumnTrafficRecord rec = kv.Value;
                if (rec == null)
                {
                    walked++;
                    continue;
                }

                ApplyLazyDecay(rec, nowHours, hoursPerDay, decayPerDay);

                byte targetMark = FootTrafficWear.MarkForWearIndex(
                    FootTrafficWear.TargetWearIndex(rec.Pressure, wearStep),
                    wearStep);

                if (rec.LastSoilPressure != targetMark
                    && IsColumnChunkLoaded(api, rec.X, rec.Z, rec.Dimension)
                    && TrafficCoverageSync.SyncColumnXZ(
                        api,
                        rec.X,
                        rec.Z,
                        rec.Dimension,
                        rec.Pressure,
                        wearStep))
                {
                    rec.LastSoilPressure = targetMark;
                    synced++;
                }

                walked++;
                if (synced >= maxSyncs)
                {
                    deferredSyncIndex = walked >= count ? 0 : walked;
                    return synced;
                }
            }

            deferredSyncIndex = 0;
            return synced;
        }

        static bool IsColumnChunkLoaded(ICoreAPI api, int x, int z, int dimension)
        {
            IBlockAccessor acc = api?.World?.BlockAccessor;
            if (acc == null) return false;

            int y = 64;
            try
            {
                y = acc.GetRainMapHeightAt(x, z);
            }
            catch
            {
                // unloaded / no map — treat as not loaded for sync
                return false;
            }

            var pos = new BlockPos(x, y, z, dimension);
            return acc.GetChunkAtBlockPos(pos) != null;
        }

        void EvictLowest()
        {
            long worstKey = 0;
            int worstPressure = 256;
            foreach (KeyValuePair<long, ColumnTrafficRecord> kv in records)
            {
                if (kv.Value.Pressure < worstPressure)
                {
                    worstPressure = kv.Value.Pressure;
                    worstKey = kv.Key;
                    if (worstPressure == 0) break;
                }
            }

            if (worstPressure < 256) records.Remove(worstKey);
        }

        static long Key(int x, int z, int dimension) =>
            ((long)(dimension & 0xFF) << 56) | ((long)(x & 0xFFFFFFF) << 28) | (uint)(z & 0xFFFFFFF);

        void OnSaveGameLoaded()
        {
            records.Clear();
            if (sapi?.WorldManager?.SaveGame == null) return;

            byte[] data = sapi.WorldManager.SaveGame.GetData(SaveKey);
            if (data == null || data.Length == 0) return;

            try
            {
                ColumnTrafficSaveRoot root = SerializerUtil.Deserialize<ColumnTrafficSaveRoot>(data);
                if (root?.Records == null) return;
                foreach (ColumnTrafficRecord rec in root.Records)
                {
                    if (rec == null) continue;
                    if (rec.Pressure == 0 && rec.LastSoilPressure == 0) continue;
                    records[Key(rec.X, rec.Z, rec.Dimension)] = rec;
                }
            }
            catch (Exception e)
            {
                sapi.Logger.Warning("[ecosystemflora] Failed to load column traffic store: {0}", e.Message);
            }
        }

        void OnGameWorldSave()
        {
            if (sapi?.WorldManager?.SaveGame == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            IGameCalendar cal = sapi.World?.Calendar;
            if (cal != null && cfg != null)
            {
                float hoursPerDay = cal.HoursPerDay > 0 ? cal.HoursPerDay : 24f;
                bool syncSoil = cfg.EcosystemEnabled
                    && cfg.EnableTrampling
                    && cfg.TramplingSoilDegradation;
                // Decay on save only — coverage catch-up runs on footstep / deferred tick (avoids save/menu stalls).
                AgeAllAndPrune(
                    sapi,
                    cal.TotalHours,
                    hoursPerDay,
                    cfg.FootTrafficDecayPerDay,
                    syncCoverage: false,
                    FootTrafficWear.EffectiveWearStep(cfg));
            }

            var root = new ColumnTrafficSaveRoot();
            foreach (ColumnTrafficRecord rec in records.Values)
            {
                if (rec == null) continue;
                if (rec.Pressure == 0 && rec.LastSoilPressure == 0) continue;
                root.Records.Add(rec);
            }

            sapi.WorldManager.SaveGame.StoreData(SaveKey, SerializerUtil.Serialize(root));
        }
    }
}
