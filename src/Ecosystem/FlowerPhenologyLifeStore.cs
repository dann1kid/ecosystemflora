using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace WildFarming.Ecosystem
{
    [ProtoContract]
    internal sealed class FlowerPhenologyLifeSaveRoot
    {
        [ProtoMember(1)]
        public List<FlowerPhenologyLifeRecord> Records { get; set; } = new List<FlowerPhenologyLifeRecord>();
    }

    [ProtoContract]
    internal sealed class FlowerPhenologyLifeRecord
    {
        [ProtoMember(1)] public int X;
        [ProtoMember(2)] public int Y;
        [ProtoMember(3)] public int Z;
        [ProtoMember(4)] public int Dimension;
        [ProtoMember(5)] public string Species;
        [ProtoMember(6)] public int LifeCycles;
        [ProtoMember(7)] public float Stress;
        [ProtoMember(8)] public int Phase;
    }

    /// <summary>Persists meadow-flower phenology stress / life-cycle debt across save and chunk churn.</summary>
    internal sealed class FlowerPhenologyLifeStore
    {
        const string SaveKey = "ecosystemflora-flower-phenology-life-v1";

        readonly Dictionary<string, FlowerPhenologyLifeRecord> records =
            new Dictionary<string, FlowerPhenologyLifeRecord>();

        ICoreServerAPI sapi;

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

        public bool TryRestore(ReproducerEntry entry)
        {
            if (entry?.Origin == null || entry.Requirements == null) return false;
            if (!records.TryGetValue(Key(entry.Origin), out FlowerPhenologyLifeRecord rec)) return false;
            if (!string.Equals(rec.Species, entry.Requirements.Species, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            entry.PhenologyLifeCycles = rec.LifeCycles < 0 ? 0 : rec.LifeCycles;
            entry.PhenologyStress = rec.Stress < 0f ? 0f : rec.Stress;
            if (rec.Phase >= 0 && rec.Phase <= (int)FlowerPhenologyPhase.Dieback)
            {
                entry.PhenologyPhase = (FlowerPhenologyPhase)rec.Phase;
            }

            return true;
        }

        public void Capture(ReproducerEntry entry)
        {
            if (entry?.Origin == null || entry.Requirements == null) return;
            BlockPos pos = entry.Origin;
            records[Key(pos)] = new FlowerPhenologyLifeRecord
            {
                X = pos.X,
                Y = pos.Y,
                Z = pos.Z,
                Dimension = pos.dimension,
                Species = entry.Requirements.Species,
                LifeCycles = entry.PhenologyLifeCycles < 0 ? 0 : entry.PhenologyLifeCycles,
                Stress = entry.PhenologyStress < 0f ? 0f : entry.PhenologyStress,
                Phase = (int)entry.PhenologyPhase,
            };
        }

        public void Remove(BlockPos pos)
        {
            if (pos == null) return;
            records.Remove(Key(pos));
        }

        void OnSaveGameLoaded()
        {
            records.Clear();
            if (sapi?.WorldManager?.SaveGame == null) return;
            byte[] data = sapi.WorldManager.SaveGame.GetData(SaveKey);
            if (data == null || data.Length == 0) return;

            try
            {
                FlowerPhenologyLifeSaveRoot root = SerializerUtil.Deserialize<FlowerPhenologyLifeSaveRoot>(data);
                if (root?.Records == null) return;
                for (int i = 0; i < root.Records.Count; i++)
                {
                    FlowerPhenologyLifeRecord rec = root.Records[i];
                    if (rec == null || string.IsNullOrEmpty(rec.Species)) continue;
                    records[Key(rec.X, rec.Y, rec.Z, rec.Dimension)] = rec;
                }
            }
            catch (System.Exception ex)
            {
                sapi.Logger.Warning("[ecosystemflora] Flower phenology life store load failed: {0}", ex.Message);
            }
        }

        void OnGameWorldSave()
        {
            if (sapi?.WorldManager?.SaveGame == null) return;
            var root = new FlowerPhenologyLifeSaveRoot();
            foreach (KeyValuePair<string, FlowerPhenologyLifeRecord> kv in records)
            {
                root.Records.Add(kv.Value);
            }

            sapi.WorldManager.SaveGame.StoreData(SaveKey, SerializerUtil.Serialize(root));
        }

        static string Key(BlockPos pos) => Key(pos.X, pos.Y, pos.Z, pos.dimension);

        static string Key(int x, int y, int z, int dimension) =>
            x + "|" + y + "|" + z + "|" + dimension;
    }
}
