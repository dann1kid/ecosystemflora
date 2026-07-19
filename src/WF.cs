using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Harmony;
using WildFarming.Ecosystem.SpeciesEcology;
using WildFarming.Blocks;
using WildFarming.Handbook;

namespace WildFarming
{
    public class WildFarming : ModSystem
    {
        EcosystemSystem ecosystem;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            LegacyBlockEntityMigration.Register(api);
            api.RegisterBlockClass("BlockReedsSafe", typeof(BlockReedsSafe));
            api.RegisterCollectibleBehaviorClass("ecosystemHandbook", typeof(EcologyHandbookBehavior));
            api.RegisterBlockBehaviorClass("ecosystemHandbook", typeof(EcologyHandbookBehavior));
            api.RegisterEntityBehaviorClass(EntityBehaviorFootTraffic.BehaviorCode, typeof(EntityBehaviorFootTraffic));
            EcosystemConfig.TryLoadFromDisk(api, createDefaultIfMissing: api.Side == EnumAppSide.Server);
            EcosystemHarmony.TryApply(api);
            if (api.Side == EnumAppSide.Server)
            {
                DiscoveredSpeciesStore.Load(api);
                SpeciesEcologyCatalogIndex.SeedDiscoveredFromStore(DiscoveredSpeciesStore.All());
            }
            SpeciesEcologyLoadService.LoadAll(api, SpeciesEcologyLoadService.ResolveModRoot(), syncUserFiles: api.Side == EnumAppSide.Server);

            if (api.Side == EnumAppSide.Server)
            {
                ecosystem = new EcosystemSystem();
                ecosystem.InitPre(api);
            }
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);
            ThirdPartyEcologyBootstrapPass.ApplyAll(api);
            DynamicSpeciesAutoCurves.Apply(api);
            FlowerDrygrassDrops.Apply(api);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            if (api.Side == EnumAppSide.Server)
            {
                if (ecosystem == null)
                {
                    ecosystem = new EcosystemSystem();
                    ecosystem.InitPre(api);
                }

                ecosystem.Init(api);

                EcosystemConfig cfg = EcosystemConfig.Loaded;
                api.Logger.Notification(
                    "[ecosystemflora] Ecosystem v3.7 — enabled={0}, displacement={1}, stress={2}, symbiosis={3}, landClaims={4}, seasonal={5}, foliage={6}, treeAging={7}, treeSenescence={8}, verbose={9}",
                    cfg.EcosystemEnabled,
                    cfg.UseCellDisplacement,
                    cfg.EnableStressDeath,
                    cfg.EnableSymbiosis,
                    cfg.RespectLandClaims,
                    cfg.UseSeasonalEcology,
                    cfg.EnableSeasonalFoliage,
                    cfg.EnableTreeAging,
                    cfg.EnableTreeSenescence,
                    cfg.VerboseLogging);
            }
        }

        public override void Dispose()
        {
            ecosystem?.Dispose();
            base.Dispose();
        }
    }
}
