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
            api.RegisterBlockEntityClass("EcosystemPlant", typeof(EcosystemPlantBlockEntity));

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

                api.Logger.Notification(
                    "[wildfarming] Ecosystem on vanilla plants — enabled={0}, debug={1}",
                    EcosystemConfig.Loaded.EcosystemEnabled,
                    EcosystemConfig.Loaded.ReproduceDebug);
            }
        }

        public override void Dispose()
        {
            ecosystem?.Dispose();
            base.Dispose();
        }
    }
}
