using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace WildFarming.Ecosystem
{
    [ProtoContract]
    internal sealed class StumpDecaySaveRoot
    {
        [ProtoMember(1)]
        public List<StumpDecayRecord> Records { get; set; } = new List<StumpDecayRecord>();
    }

    [ProtoContract]
    internal sealed class StumpDecayRecord
    {
        [ProtoMember(1)] public int X;
        [ProtoMember(2)] public int Y;
        [ProtoMember(3)] public int Z;
        [ProtoMember(4)] public int Dimension;
        [ProtoMember(5)] public string Wood;
        [ProtoMember(6)] public double PlacedAtHours;
    }

    /// <summary>Calendar decay for vanilla stumps left after snag collapse.</summary>
    internal sealed class StumpDecayScheduler
    {
        const string SaveKey = "ecosystemflora-stump-decay-v1";

        readonly List<StumpDecayRecord> entries = new List<StumpDecayRecord>();
        readonly Dictionary<BlockPos, int> indexByPos = new Dictionary<BlockPos, int>();

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

        public void Enqueue(ICoreAPI api, BlockPos pos, string wood)
        {
            if (api?.World?.Calendar == null || pos == null || string.IsNullOrEmpty(wood)) return;
            if (indexByPos.ContainsKey(pos)) return;

            indexByPos[pos] = entries.Count;
            entries.Add(new StumpDecayRecord
            {
                X = pos.X,
                Y = pos.Y,
                Z = pos.Z,
                Dimension = pos.dimension,
                Wood = wood,
                PlacedAtHours = api.World.Calendar.TotalHours,
            });
        }

        public void Remove(BlockPos pos)
        {
            if (pos == null || !indexByPos.TryGetValue(pos, out int index)) return;

            int last = entries.Count - 1;
            BlockPos removed = ToPos(entries[index]);
            if (index != last)
            {
                StumpDecayRecord moved = entries[last];
                entries[index] = moved;
                indexByPos[ToPos(moved)] = index;
            }

            entries.RemoveAt(last);
            indexByPos.Remove(removed);
        }

        public bool TryGetYearsRemaining(ICoreAPI api, BlockPos pos, EcosystemConfig cfg, out double yearsLeft)
        {
            yearsLeft = 0;
            if (api?.World?.Calendar == null || pos == null || cfg == null) return false;
            if (!indexByPos.TryGetValue(pos, out int index)) return false;

            StumpDecayRecord rec = entries[index];
            double decayHours = StumpDecayHours(cfg, api);
            double elapsed = api.World.Calendar.TotalHours - rec.PlacedAtHours;
            yearsLeft = (decayHours - elapsed) / api.World.Calendar.HoursPerDay / api.World.Calendar.DaysPerYear;
            if (yearsLeft < 0) yearsLeft = 0;
            return true;
        }

        public void Process(ICoreAPI api, EcosystemConfig cfg, int maxChecks)
        {
            if (api == null || cfg == null || !cfg.EnableStumpDecay || maxChecks <= 0 || entries.Count == 0) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            double now = api.World.Calendar.TotalHours;
            double decayHours = StumpDecayHours(cfg, api);
            var remove = new List<BlockPos>();
            int checkedCount = 0;

            for (int i = entries.Count - 1; i >= 0 && checkedCount < maxChecks; i--)
            {
                StumpDecayRecord rec = entries[i];
                BlockPos pos = ToPos(rec);
                checkedCount++;

                if (now - rec.PlacedAtHours < decayHours) continue;

                if (!LandClaimGuard.AllowsEcologyChange(api, pos))
                {
                    continue;
                }

                Block block = acc.GetBlock(pos);
                if (block != null && block.Id != 0 && IsStumpBlock(block, rec.Wood))
                {
                    acc.SetBlock(0, pos);
                    acc.MarkBlockDirty(pos);
                }

                remove.Add(pos);
            }

            for (int r = 0; r < remove.Count; r++)
            {
                Remove(remove[r]);
            }
        }

        static double StumpDecayHours(EcosystemConfig cfg, ICoreAPI api)
        {
            double years = cfg.StumpDecayYears <= 0 ? 10 : cfg.StumpDecayYears;
            return years * api.World.Calendar.DaysPerYear * api.World.Calendar.HoursPerDay;
        }

        static bool IsStumpBlock(Block block, string wood)
        {
            if (block?.Code == null || string.IsNullOrEmpty(wood)) return false;
            string path = block.Code.Path;
            return path == "log-" + wood + "-ud" || path == "debarkedlog-" + wood + "-ud";
        }

        static BlockPos ToPos(StumpDecayRecord rec) =>
            new BlockPos(rec.X, rec.Y, rec.Z, rec.Dimension);

        void OnSaveGameLoaded()
        {
            entries.Clear();
            indexByPos.Clear();
            if (sapi?.WorldManager?.SaveGame == null) return;

            byte[] data = sapi.WorldManager.SaveGame.GetData(SaveKey);
            LoadFromBytes(data);
        }

        void OnGameWorldSave()
        {
            if (sapi?.WorldManager?.SaveGame == null) return;
            sapi.WorldManager.SaveGame.StoreData(SaveKey, SerializerUtil.Serialize(BuildSaveRoot()));
        }

        StumpDecaySaveRoot BuildSaveRoot() => new StumpDecaySaveRoot { Records = entries };

        void LoadFromBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            StumpDecaySaveRoot root = SerializerUtil.Deserialize<StumpDecaySaveRoot>(data);
            if (root?.Records == null) return;

            for (int i = 0; i < root.Records.Count; i++)
            {
                StumpDecayRecord rec = root.Records[i];
                if (rec == null) continue;
                BlockPos pos = ToPos(rec);
                indexByPos[pos] = entries.Count;
                entries.Add(rec);
            }
        }
    }
}
