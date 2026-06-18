using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Autumn branchy defoliation drops vanilla <c>loosestick-free</c> on the ground below.</summary>
    internal static class CanopyFallenSticks
    {
        const int MaxDropScan = 48;

        public static void TryDropFromStrip(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos foliagePos,
            string wood,
            float autumnActivity,
            int gameYear,
            FoliageCellKind strippedKind)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableCanopyFallenSticks) return;
            if (strippedKind != FoliageCellKind.BranchyLeaf) return;
            if (api == null || acc == null || foliagePos == null || string.IsNullOrEmpty(wood)) return;
            if (autumnActivity <= 0f) return;

            float noise = 0.55f + CanopyBlockHelper.DeterministicNoise(foliagePos, wood, gameYear + 41) * 0.45f;
            float chance = cfg.CanopyFallenStickChance * autumnActivity * noise;
            if (chance > 1f) chance = 1f;
            if (chance < 0f) chance = 0f;

            float gate = CanopyBlockHelper.DeterministicNoise(foliagePos, wood, gameYear + 1041);
            if (gate >= chance) return;

            if (!TryFindGroundStickCell(acc, foliagePos, out BlockPos stickPos)) return;
            if (!LandClaimGuard.AllowsEcologyChange(api, stickPos)) return;

            Block stick = api.World.GetBlock(new AssetLocation("game:loosestick-free"));
            if (stick == null || stick.Id == 0) return;

            if (!SurfacePlacement.IsValidPlantSite(acc, stickPos)) return;

            acc.SetBlock(stick.BlockId, stickPos);
            acc.MarkBlockDirty(stickPos);
        }

        internal static bool TryFindGroundStickCell(IBlockAccessor acc, BlockPos from, out BlockPos stickPos) =>
            SurfacePlacement.TryFindSurfaceCellBelow(acc, from, MaxDropScan, out stickPos);
    }
}
