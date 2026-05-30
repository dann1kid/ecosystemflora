using Vintagestory.API.Common;
using WildFarming.Ecosystem;
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
            api.RegisterBlockBehaviorClass("ecosystemHandbook", typeof(EcologyHandbookBehavior));
            EcosystemConfig.TryLoadFromDisk(api, createDefaultIfMissing: api.Side == EnumAppSide.Server);

            if (api.Side == EnumAppSide.Server)
            {
                ecosystem = new EcosystemSystem();
                ecosystem.InitPre(api);
            }
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);
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
                    "[ecosystemflora] Ecosystem v2.7 — enabled={0}, displacement={1}, stress={2}, symbiosis={3}, landClaims={4}, seasonal={5}, verbose={6}",
                    cfg.EcosystemEnabled,
                    cfg.UseCellDisplacement,
                    cfg.EnableStressDeath,
                    cfg.EnableSymbiosis,
                    cfg.RespectLandClaims,
                    cfg.UseSeasonalEcology,
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
