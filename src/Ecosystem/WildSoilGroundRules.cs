using System.Linq;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    internal static class WildSoilGroundRules
    {
        public static bool IsFarmland(Block ground)
        {
            if (ground?.Code == null || ground.Id == 0) return false;
            string path = ground.Code.Path;
            return !string.IsNullOrEmpty(path) && path.StartsWith("farmland");
        }

        public static bool IsMyceliumHost(Block ground)
        {
            if (ground == null || ground.Id == 0) return false;
            return ground.BlockBehaviors != null
                && ground.BlockBehaviors.Any(b => b.GetType().Name == "BlockBehaviorMyceliumHost");
        }

        public static bool IsWildSpreadGround(Block ground)
        {
            if (ground == null || ground.Id == 0) return false;
            if (IsFarmland(ground)) return false;
            return WildSoilBlockMapper.IsSuccessionTarget(ground);
        }
    }
}
