using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Ground-cell gates for new vanilla mycelium anchors.</summary>
    internal static class MyceliumPlacement
    {
        internal const int MinGroundFertility = 10;

        public static bool CanSpreadInto(
            ICoreAPI api,
            BlockPos groundPos,
            MyceliumNiche spreadingNiche,
            out string rejectReason)
        {
            rejectReason = null;
            if (api?.World?.BlockAccessor == null || groundPos == null)
            {
                rejectReason = "null";
                return false;
            }

            IBlockAccessor acc = api.World.BlockAccessor;

            if (!LandClaimGuard.AllowsEcologyChange(api, groundPos))
            {
                rejectReason = "land claim";
                return false;
            }

            Block ground = acc.GetBlock(groundPos);
            if (!PlantVacancyRules.IsSupportingGround(ground))
            {
                rejectReason = "ground not solid";
                return false;
            }

            if (WildSoilGroundRules.IsFarmland(ground))
            {
                rejectReason = "farmland";
                return false;
            }

            if (MyceliumEcology.IsTrunkAnchor(ground))
            {
                rejectReason = "trunk anchor only via worldgen";
                return false;
            }

            int fertility = (int)ground.Fertility;
            if (fertility < MinGroundFertility)
            {
                rejectReason = "low fertility";
                return false;
            }

            BlockPos above = groundPos.UpCopy();
            Block space = acc.GetBlock(above);
            bool spaceOk = PlantVacancyRules.IsVacantPlantSpace(space);
            if (!spaceOk
                && !(spreadingNiche == MyceliumNiche.MeadowOpen
                    && MyceliumCoexistence.IsMeadowPlantBlock(space)))
            {
                rejectReason = "space not air";
                return false;
            }

            if (PlantVacancyRules.TouchesSpreadBlockingFluid(space, ground, acc.GetBlock(above, BlockLayersAccess.Fluid), null))
            {
                rejectReason = "fluid";
                return false;
            }

            return true;
        }
    }
}
