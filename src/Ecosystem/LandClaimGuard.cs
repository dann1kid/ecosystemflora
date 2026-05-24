using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Blocks wild ecology block changes inside player land claims (server only).</summary>
    internal static class LandClaimGuard
    {
        public static bool AllowsEcologyChange(ICoreAPI api, BlockPos pos)
        {
            if (api == null || pos == null) return false;
            if (api.Side != EnumAppSide.Server) return true;
            if (!EcosystemConfig.Loaded.RespectLandClaims) return true;

            ILandClaimAPI claims = api.World?.Claims;
            if (claims == null) return true;

            LandClaim[] at = claims.Get(pos);
            if (at == null || at.Length == 0) return true;

            for (int i = 0; i < at.Length; i++)
            {
                LandClaim claim = at[i];
                if (claim != null && claim.PositionInside(pos))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
