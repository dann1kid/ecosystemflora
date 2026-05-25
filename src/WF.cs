using Vintagestory.API.Common;
using WildFarming.Ecosystem;

namespace WildFarming
{
    public class WildFarming : ModSystem
    {
        EcosystemSystem ecosystem;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            api.RegisterBlockEntityClass("EcoSystemLife", typeof(EcoSystemLifeBlockEntity));

            if (api.Side == EnumAppSide.Server)
            {
                ecosystem = new EcosystemSystem();
                ecosystem.InitPre(api);
            }
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
                    "[wildfarming] Ecosystem v2.1 — enabled={0}, displacement={1}, stress={2}, symbiosis={3}, landClaims={4}, seasonal={5}, debug={6}",
                    cfg.EcosystemEnabled,
                    cfg.UseCellDisplacement,
                    cfg.EnableStressDeath,
                    cfg.EnableSymbiosis,
                    cfg.RespectLandClaims,
                    cfg.UseSeasonalEcology,
                    cfg.ReproduceDebug);
            }
        }

        public override void Dispose()
        {
            ecosystem?.Dispose();
            base.Dispose();
        }
    }
}
