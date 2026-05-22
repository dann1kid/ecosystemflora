using Vintagestory.API.Common;
using WildFarming.Ecosystem;

namespace WildFarming
{
    public class WildFarming : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("wildseed", typeof(WildSeed));
            api.RegisterBlockEntityClass("WildPlant", typeof(WildPlantBlockEntity));
        }
    }
}
