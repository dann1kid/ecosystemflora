using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    [ProtoContract]
    internal sealed class TreeCalendarAgeSaveRoot
    {
        [ProtoMember(1)]
        public List<TreeCalendarAgeRecord> Records { get; set; } = new List<TreeCalendarAgeRecord>();
    }

    [ProtoContract]
    internal sealed class TreeCalendarAgeRecord
    {
        [ProtoMember(1)] public int X;
        [ProtoMember(2)] public int Y;
        [ProtoMember(3)] public int Z;
        [ProtoMember(4)] public int Dimension;
        [ProtoMember(5)] public int AgeYears;
        [ProtoMember(6)] public int LastGrowthYear;
        [ProtoMember(7)] public string Wood;
        [ProtoMember(8)] public int SenescencePhase;
    }

    /// <summary>Persists wild-tree calendar age across save/load (trunk base key).</summary>
    internal sealed class TreeCalendarAgeStore
    {
        const string SaveKey = "ecosystemflora-tree-calendar-age-v1";
        const int ColumnScanBelow = 4;
        const int ColumnScanAbove = 48;

        readonly Dictionary<string, TreeCalendarAgeRecord> records = new Dictionary<string, TreeCalendarAgeRecord>();

        ICoreServerAPI sapi;
        ReproducerRegistry registry;

        public void Bind(ICoreServerAPI serverApi, ReproducerRegistry reproducerRegistry)
        {
            sapi = serverApi;
            registry = reproducerRegistry;
            serverApi.Event.SaveGameLoaded += OnSaveGameLoaded;
            serverApi.Event.GameWorldSave += OnGameWorldSave;
        }

        public void Unbind(ICoreServerAPI serverApi)
        {
            if (serverApi == null) return;
            serverApi.Event.SaveGameLoaded -= OnSaveGameLoaded;
            serverApi.Event.GameWorldSave -= OnGameWorldSave;
            sapi = null;
            registry = null;
        }

        public void Clear()
        {
            records.Clear();
        }

        public bool TryRestore(ReproducerEntry entry, BlockPos trunkBase, string wood)
        {
            if (entry == null || trunkBase == null || string.IsNullOrEmpty(wood)) return false;
            if (!records.TryGetValue(Key(trunkBase), out TreeCalendarAgeRecord rec)) return false;
            if (!WoodMatches(rec.Wood, wood)) return false;

            entry.TreeAgeYears = rec.AgeYears < 0 ? 0 : rec.AgeYears;
            entry.LastTreeGrowthYear = rec.LastGrowthYear;
            entry.TreeSenescencePhase = (TreeSenescencePhase)rec.SenescencePhase;
            return true;
        }

        public void Capture(ReproducerEntry entry, string wood)
        {
            if (entry?.Origin == null || string.IsNullOrEmpty(wood)) return;

            BlockPos pos = entry.Origin;
            records[Key(pos)] = new TreeCalendarAgeRecord
            {
                X = pos.X,
                Y = pos.Y,
                Z = pos.Z,
                Dimension = pos.dimension,
                AgeYears = entry.TreeAgeYears < 0 ? 0 : entry.TreeAgeYears,
                LastGrowthYear = entry.LastTreeGrowthYear,
                Wood = wood,
                SenescencePhase = (int)entry.TreeSenescencePhase,
            };
        }

        public void Remove(BlockPos trunkBase)
        {
            if (trunkBase == null) return;
            records.Remove(Key(trunkBase));
        }

        public void TryRemoveIfTreeGone(IBlockAccessor acc, BlockPos brokenPos, string wood)
        {
            if (acc == null || brokenPos == null || string.IsNullOrEmpty(wood)) return;
            if (HasArborealInColumn(acc, brokenPos, wood)) return;

            string removeKey = null;
            bool found = false;
            foreach (KeyValuePair<string, TreeCalendarAgeRecord> kv in records)
            {
                TreeCalendarAgeRecord rec = kv.Value;
                if (!WoodMatches(rec.Wood, wood)) continue;
                if (rec.Dimension != brokenPos.dimension) continue;
                if (Math.Abs(rec.X - brokenPos.X) > 1 || Math.Abs(rec.Z - brokenPos.Z) > 1) continue;
                if (brokenPos.Y < rec.Y - ColumnScanBelow || brokenPos.Y > rec.Y + ColumnScanAbove) continue;

                removeKey = kv.Key;
                found = true;
                break;
            }

            if (found)
            {
                records.Remove(removeKey);
            }
        }

        public void SyncFromRegistry(ReproducerRegistry reg)
        {
            if (reg == null) return;

            int count = reg.Count;
            for (int i = 0; i < count; i++)
            {
                ReproducerEntry entry = reg.GetEntry(i);
                if (entry?.Requirements?.Habitat != EcologyHabitat.TerrestrialTree
                    && entry?.Requirements?.Habitat != EcologyHabitat.Ferntree) continue;
                if (entry.Origin == null) continue;

                string wood = entry.Requirements.Habitat == EcologyHabitat.Ferntree
                    ? WildFerntreeEcology.Species
                    : PlantCodeHelper.GetTreeWood(entry.MatureBlockCode);
                if (string.IsNullOrEmpty(wood)) continue;

                Capture(entry, wood);
            }
        }

        internal static string Key(BlockPos pos) => pos.dimension + ":" + pos.X + ":" + pos.Y + ":" + pos.Z;

        internal int RecordCount => records.Count;

        internal bool TryGetRecord(BlockPos trunkBase, out TreeCalendarAgeRecord record)
        {
            return records.TryGetValue(Key(trunkBase), out record);
        }

        internal byte[] SerializeForTests()
        {
            return SerializerUtil.Serialize(BuildSaveRoot());
        }

        internal void LoadFromBytes(byte[] data)
        {
            records.Clear();
            if (data == null || data.Length == 0) return;

            TreeCalendarAgeSaveRoot root = SerializerUtil.Deserialize<TreeCalendarAgeSaveRoot>(data);
            if (root?.Records == null) return;

            foreach (TreeCalendarAgeRecord rec in root.Records)
            {
                if (rec == null) continue;
                var pos = new BlockPos(rec.X, rec.Y, rec.Z, rec.Dimension);
                records[Key(pos)] = rec;
            }
        }

        void OnSaveGameLoaded()
        {
            records.Clear();
            if (sapi?.WorldManager?.SaveGame == null) return;

            byte[] data = sapi.WorldManager.SaveGame.GetData(SaveKey);
            LoadFromBytes(data);
        }

        void OnGameWorldSave()
        {
            if (sapi?.WorldManager?.SaveGame == null) return;

            SyncFromRegistry(registry);
            sapi.WorldManager.SaveGame.StoreData(SaveKey, SerializerUtil.Serialize(BuildSaveRoot()));
        }

        TreeCalendarAgeSaveRoot BuildSaveRoot()
        {
            var root = new TreeCalendarAgeSaveRoot();
            foreach (TreeCalendarAgeRecord rec in records.Values)
            {
                root.Records.Add(rec);
            }

            return root;
        }

        static bool WoodMatches(string stored, string current)
        {
            if (string.IsNullOrEmpty(stored)) return true;
            return string.Equals(stored, current, StringComparison.OrdinalIgnoreCase);
        }

        static bool HasArborealInColumn(IBlockAccessor acc, BlockPos origin, string wood)
        {
            if (WildFerntreeEcology.IsSpecies(wood))
            {
                return FerntreeStructure.HasFerntreeInColumn(acc, origin);
            }

            return HasLogGrownInColumn(acc, origin, wood);
        }

        static bool HasLogGrownInColumn(IBlockAccessor acc, BlockPos origin, string wood)
        {
            int yMin = origin.Y - ColumnScanBelow;
            int yMax = origin.Y + ColumnScanAbove;
            var scan = new BlockPos(origin.X, yMin, origin.Z, origin.dimension);
            for (int y = yMin; y <= yMax; y++)
            {
                scan.Y = y;
                Block block = acc.GetBlock(scan);
                if (!PlantCodeHelper.IsTreeLogGrownBlock(block)) continue;
                if (string.Equals(PlantCodeHelper.GetTreeWood(block), wood, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
