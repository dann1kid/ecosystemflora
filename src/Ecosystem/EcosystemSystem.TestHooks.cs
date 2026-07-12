using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using WildFarming.Ecosystem.SpeciesEcology;

namespace WildFarming.Ecosystem
{
    /// <summary>Internal entry points for in-process integration tests (WildFarming.Tests).</summary>
    public partial class EcosystemSystem
    {
        internal int Test_RegistryCount => registry.Count;

        internal bool Test_TryGetRegistryEntry(BlockPos pos, out ReproducerEntry entry) =>
            registry.TryGetEntry(pos, out entry);

        internal bool Test_IsChunkRegistrationFinished(Vec2i chunkCoord) =>
            IsChunkRegistrationFinished(chunkCoord);

        internal void Test_EnqueueChunkScan(Vec2i chunkCoord, bool highPriority = false) =>
            EnqueueChunkScan(chunkCoord, highPriority: highPriority);

        internal void Test_ReproduceTick(float dt = 0) => OnReproduceTick(dt);

        internal void Test_ChunkScanTick(float dt = 0) => OnChunkScanTick(dt);

        internal void Test_StressTick(float dt = 0) => OnStressTick(dt);

        internal void Test_TryRegisterPlacedBlock(BlockPos pos) => TryRegisterPlacedBlock(pos);

        internal void Test_AddTallgrassPromotion(BlockPos pos) =>
            maturationQueues.AddTallgrassPromotion(api, pos);

        internal void Test_FlushRegistration()
        {
            if (api?.World?.BlockAccessor == null) return;
            DrainPendingRegistrations(EcosystemConfig.Loaded, api.World.BlockAccessor, null);
        }

        /// <summary>Server init without game tick listeners or world event hooks.</summary>
        internal void InitForSimulation(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;

            this.api = api;
            Instance = this;

            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;

            FloraContext = new FloraContextSampler();
            Niche = new NicheSampler();
            SpacingIndex = new EcologySpacingIndex();
            ColumnCache = new EnvironmentalColumnCache();
            EcologyColumns = new EcologyColumnState();
            spreadCooldown = new SpreadCooldownService(api, registry);

            EcosystemConfig tickCfg = EcosystemConfig.Loaded;
            if (tickCfg.EnableBackgroundRegistrationScan)
            {
                backgroundRegistration.Start(api.World.Blocks, tickCfg.RegistrationWorkerCount);
            }

            if (tickCfg.EnableBackgroundSpreadSolve)
            {
                backgroundSpread.Start(api.World.Blocks, tickCfg.SpreadWorkerCount);
            }

            foliageCells.RequestEcologyScan = coord => EnqueueChunkScan(coord);

            if (api is ICoreServerAPI sapi)
            {
                treeCalendarAgeStore.Bind(sapi, registry);
                stumpDecayScheduler.Bind(sapi);
            }

            SpeciesEcologyLegacyAccess.LogMissingContractSpecies(api);
        }
    }
}
