using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
namespace WildFarming.Ecosystem
{
    public class EcosystemSystem : ModSystem
    {
        public static EcosystemSystem Instance { get; private set; }

        ICoreAPI api;
        readonly List<ReproducerEntry> reproducers = new List<ReproducerEntry>();
        long reproduceListenerId;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            Instance = this;

            try
            {
                EcosystemConfig fromDisk = api.LoadModConfig<EcosystemConfig>("wildfarming-ecosystem.json");
                if (fromDisk != null) EcosystemConfig.Loaded = fromDisk;
                else api.StoreModConfig(EcosystemConfig.Loaded, "wildfarming-ecosystem.json");
            }
            catch
            {
                api.StoreModConfig(EcosystemConfig.Loaded, "wildfarming-ecosystem.json");
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.api = api;

            if (api.Side == EnumAppSide.Server && EcosystemConfig.Loaded.EcosystemEnabled)
            {
                reproduceListenerId = api.Event.RegisterGameTickListener(OnReproduceTick, 8000);
            }
        }

        public override void Dispose()
        {
            if (api != null && reproduceListenerId != 0)
            {
                api.Event.UnregisterGameTickListener(reproduceListenerId);
            }
            reproducers.Clear();
            Instance = null;
            base.Dispose();
        }

        public EnvironmentalContext Sample(BlockPos plantPos)
        {
            return EnvironmentalContext.Sample(api, plantPos);
        }

        public float ScoreForPlant(BlockPos plantPos, PlantRequirements requirements)
        {
            IEnvironmentalContext ctx = Sample(plantPos);
            return SuitabilityEvaluator.Score(requirements, ctx, EcosystemConfig.Loaded.HarshWildPlants);
        }

        /// <summary>Can this plant live here (climate, soil)? Used after planting — not for blocking the player.</summary>
        public bool CanSurviveAt(BlockPos plantPos, PlantRequirements requirements)
        {
            return SuitabilityEvaluator.MeetsHardRequirements(
                requirements,
                Sample(plantPos),
                EcosystemConfig.Loaded.HarshWildPlants);
        }

        public bool CanReproduceAt(BlockPos plantPos, PlantRequirements requirements)
        {
            return SuitabilityEvaluator.CanReproduce(
                requirements,
                Sample(plantPos),
                EcosystemConfig.Loaded.HarshWildPlants,
                EcosystemConfig.Loaded.MinFitness);
        }

        public void RegisterReproducer(BlockPos origin, AssetLocation juvenileBlockCode, PlantRequirements requirements)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;
            if (api.Side != EnumAppSide.Server) return;

            for (int i = reproducers.Count - 1; i >= 0; i--)
            {
                if (reproducers[i].Origin.Equals(origin))
                {
                    reproducers.RemoveAt(i);
                }
            }

            reproducers.Add(new ReproducerEntry(
                origin.Copy(),
                juvenileBlockCode,
                requirements,
                api.World.Calendar.TotalHours + EcosystemConfig.Loaded.ReproduceIntervalHours));
        }

        void OnReproduceTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            double now = api.World.Calendar.TotalHours;
            EcosystemConfig cfg = EcosystemConfig.Loaded;

            for (int i = reproducers.Count - 1; i >= 0; i--)
            {
                ReproducerEntry entry = reproducers[i];
                Block block = acc.GetBlock(entry.Origin);

                if (block.Id == 0 || !IsMaturePlant(block, entry))
                {
                    reproducers.RemoveAt(i);
                    continue;
                }

                if (now < entry.NextAttemptHours) continue;

                entry.NextAttemptHours = now + cfg.ReproduceIntervalHours;
                TryReproduce(entry);
            }
        }

        static bool IsMaturePlant(Block block, ReproducerEntry entry)
        {
            return block.Code != null
                && block.Code.Domain == "game"
                && block.Code.Path.StartsWith("flower-");
        }

        void TryReproduce(ReproducerEntry entry)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (api.World.Rand.NextDouble() > cfg.ReproduceChance) return;

            Block juvenile = api.World.GetBlock(entry.JuvenileBlockCode);
            if (juvenile == null) return;

            int radius = cfg.ReproduceRadius;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                int dx = api.World.Rand.Next(-radius, radius + 1);
                int dz = api.World.Rand.Next(-radius, radius + 1);
                if (dx == 0 && dz == 0) continue;

                BlockPos ground = entry.Origin.AddCopy(dx, -1, dz);
                BlockPos plantPos = ground.UpCopy();

                if (!CanReproduceAt(plantPos, entry.Requirements)) continue;

                accSetPlant(plantPos, juvenile);
                return;
            }
        }

        void accSetPlant(BlockPos plantPos, Block juvenile)
        {
            api.World.BlockAccessor.SetBlock(juvenile.Id, plantPos);
        }

        sealed class ReproducerEntry : IReproducible
        {
            public BlockPos Origin { get; }
            public AssetLocation JuvenileBlockCode { get; }
            public PlantRequirements Requirements { get; }
            public double NextAttemptHours { get; set; }

            public ReproducerEntry(BlockPos origin, AssetLocation juvenileBlockCode, PlantRequirements requirements, double nextAttemptHours)
            {
                Origin = origin;
                JuvenileBlockCode = juvenileBlockCode;
                Requirements = requirements;
                NextAttemptHours = nextAttemptHours;
            }
        }
    }
}
