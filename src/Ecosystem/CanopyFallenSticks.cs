using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Autumn branchy defoliation drops vanilla <c>loosestick-free</c> on the ground below.</summary>
    internal static class CanopyFallenSticks
    {
        const int MaxDropScan = 48;
        static readonly BlockPos scanPos = new BlockPos(0);
        static readonly BlockPos groundScratch = new BlockPos(0);

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

            Block incumbent = acc.GetBlock(stickPos);
            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco != null && PlantCodeHelper.IsEcologySpreadParent(incumbent))
            {
                eco.RemoveEcologyPlant(stickPos, cascadeSymbiosis: true, reason: "fallen-stick");
            }

            acc.SetBlock(stick.BlockId, stickPos);
            acc.MarkBlockDirty(stickPos);
        }

        /// <summary>
        /// Lowest valid ground cell below foliage. Aborts when any <c>loosestick</c> is hit while scanning down.
        /// Replaces meadow flora (grass, flowers, ferns) or occupies air directly above solid ground.
        /// </summary>
        internal static bool TryFindGroundStickCell(IBlockAccessor acc, BlockPos from, out BlockPos stickPos)
        {
            stickPos = null;
            if (acc == null || from == null) return false;

            int minY = from.Y - MaxDropScan;
            if (minY < 0) minY = 0;

            BlockPos lowest = null;

            for (int y = from.Y - 1; y >= minY; y--)
            {
                scanPos.Set(from.X, y, from.Z);
                if (!acc.IsValidPos(scanPos)) break;

                Block space = acc.GetBlock(scanPos);
                if (IsLooseStickBlock(space)) return false;

                if (!HasStickSupportingGround(acc, scanPos)) continue;
                lowest = scanPos.Copy();
            }

            if (lowest == null) return false;

            stickPos = lowest;
            return true;
        }

        internal static bool IsLooseStickBlock(Block block)
        {
            string path = block?.Code?.Path;
            if (string.IsNullOrEmpty(path)) return false;
            return path.StartsWith("loosestick");
        }

        /// <summary>Air or terrestrial meadow flora that a fallen stick may replace.</summary>
        internal static bool CanStickReplaceFlora(Block block)
        {
            if (block == null) return false;
            if (IsLooseStickBlock(block)) return false;
            if (block.Id == 0) return true;
            if (PlantCodeHelper.IsTreeLogGrownBlock(block)) return false;
            if (PlantCodeHelper.IsTreeSaplingBlock(block)) return false;
            if (PlantCodeHelper.IsFerntreeTrunkBlock(block)) return false;
            if (PlantCodeHelper.IsWildBerryBushBlock(block)) return false;
            if (WildVineHelper.IsEndBlock(block)) return false;
            if (PlantCodeHelper.GetEcologyHabitat(block) != EcologyHabitat.Terrestrial) return false;

            return PlantCodeHelper.IsEcologyPlant(block);
        }

        static bool HasStickSupportingGround(IBlockAccessor acc, BlockPos pos)
        {
            Block space = acc.GetBlock(pos);
            if (!CanStickReplaceFlora(space)) return false;

            groundScratch.Set(pos.X, pos.Y - 1, pos.Z);
            Block ground = acc.GetBlock(groundScratch);

            Block fluidAt = acc.GetBlock(pos, BlockLayersAccess.Fluid);
            Block fluidBelow = acc.GetBlock(groundScratch, BlockLayersAccess.Fluid);
            if (PlantVacancyRules.TouchesSpreadBlockingFluid(space, ground, fluidAt, fluidBelow))
            {
                return false;
            }

            return PlantVacancyRules.IsSupportingGround(ground);
        }
    }
}
