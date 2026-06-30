using Vintagestory.API.Common;

using Vintagestory.API.MathTools;



namespace WildFarming.Ecosystem

{

    public enum FoliageCellKind

    {

        None = 0,

        LogGrown = 1,

        BranchyLeaf = 2,

        RegularLeaf = 3,

    }



    /// <summary>Per-cell seasonal rules: log → branchy, branchy → leaf, leaf → air.</summary>

    internal static class CanopyFoliageRules

    {

        static readonly int[] NeighborDx = { 1, -1, 0, 0, 0, 0 };

        static readonly int[] NeighborDy = { 0, 0, 1, -1, 0, 0 };

        static readonly int[] NeighborDz = { 0, 0, 0, 0, 1, -1 };



        public static bool IsSeasonalFoliageBlock(Block block)

        {

            return Classify(block) != FoliageCellKind.None;

        }



        public static FoliageCellKind Classify(Block block)

        {

            if (block?.Code == null) return FoliageCellKind.None;



            if (PlantCodeHelper.IsTreeLogGrownBlock(block))

            {

                string wood = PlantCodeHelper.GetTreeWood(block);

                return CanopyBlockHelper.IsDeciduousTreeWood(wood)

                    ? FoliageCellKind.LogGrown

                    : FoliageCellKind.None;

            }



            string foliageWood = CanopyBlockHelper.GetWoodFromFoliageBlock(block);

            if (string.IsNullOrEmpty(foliageWood) || !CanopyBlockHelper.IsDeciduousTreeWood(foliageWood))

            {

                return FoliageCellKind.None;

            }



            if (CanopyBlockHelper.IsBranchyLeaf(block)) return FoliageCellKind.BranchyLeaf;

            if (CanopyBlockHelper.IsRegularLeaf(block)) return FoliageCellKind.RegularLeaf;

            return FoliageCellKind.None;

        }



        public static bool CanActThisSeason(ICoreAPI api, BlockPos pos, Block block)

        {

            if (api == null || pos == null || block == null) return false;



            FoliageCellKind kind = Classify(block);

            if (kind == FoliageCellKind.None) return false;



            string wood = kind == FoliageCellKind.LogGrown

                ? PlantCodeHelper.GetTreeWood(block)

                : CanopyBlockHelper.GetWoodFromFoliageBlock(block);

            if (string.IsNullOrEmpty(wood)) return false;



            CanopySeasonPhase phase = CanopyEcology.ResolvePhase(api, pos, wood, out float activity);

            IBlockAccessor acc = api.World?.BlockAccessor;
            if (kind == FoliageCellKind.LogGrown
                && ShouldRestoreBareSkeletonOnLog(api, acc, pos, wood))
            {
                return true;
            }

            if (phase == CanopySeasonPhase.Idle || activity <= 0f) return false;

            return kind switch

            {

                FoliageCellKind.RegularLeaf => phase == CanopySeasonPhase.Autumn,

                FoliageCellKind.BranchyLeaf => phase == CanopySeasonPhase.Spring

                    || (phase == CanopySeasonPhase.Autumn && ShouldStripBranchyInAutumn(activity)),

                FoliageCellKind.LogGrown => phase == CanopySeasonPhase.Spring
                    || ShouldRestoreBareSkeletonOnLog(api, acc, pos, wood),

                _ => false,

            };

        }



        static bool ShouldStripBranchyInAutumn(float activity)

        {

            float threshold = EcosystemConfig.Loaded.FoliagePeakAutumnBranchyStripActivity;

            return threshold > 0f && activity >= threshold;

        }



        /// <summary>Chunk-load catch-up: strip regular leaves only (branchy skeleton stays).</summary>

        public static bool TryCatchUpStripOnScan(

            ICoreAPI api,

            IBlockAccessor acc,

            BlockPos pos,

            Block block)

        {

            if (api == null || acc == null || pos == null || block == null) return false;

            if (!EcosystemConfig.Loaded.EnableSeasonalFoliage) return false;

            if (!EcosystemConfig.Loaded.FoliageCatchUpOnChunkLoad) return false;

            if (Classify(block) != FoliageCellKind.RegularLeaf) return false;



            string wood = CanopyBlockHelper.GetWoodFromFoliageBlock(block);

            if (string.IsNullOrEmpty(wood)) return false;

            if (!ShouldCatchUpStripRegularLeaf(api, pos, wood, out float activity)) return false;



            int gameYear = CanopyEcology.GameYear(api.World.Calendar);

            return TryStripFoliage(api, acc, pos, wood, activity, gameYear, FoliageCellKind.RegularLeaf, index: null);

        }



        /// <summary>Chunk-load catch-up: branchy → leaves-grown (species-scaled patchiness).</summary>

        public static bool TryCatchUpBudOnScan(

            ICoreAPI api,

            IBlockAccessor acc,

            BlockPos pos,

            Block block,

            FoliageCellIndex index)

        {

            if (api == null || acc == null || pos == null || block == null) return false;

            if (!EcosystemConfig.Loaded.EnableSeasonalFoliage) return false;

            if (!EcosystemConfig.Loaded.FoliageCatchUpOnChunkLoad) return false;



            FoliageCellKind kind = Classify(block);

            if (kind != FoliageCellKind.BranchyLeaf) return false;



            string wood = CanopyBlockHelper.GetWoodFromFoliageBlock(block);

            if (string.IsNullOrEmpty(wood)) return false;



            if (!ShouldCatchUpBud(api, pos, wood, kind, out float activity)) return false;



            WildCanopySeason.Profile profile = WildCanopySeason.Resolve(wood);

            activity *= profile.LeafCatchUpScale;



            int gameYear = CanopyEcology.GameYear(api.World.Calendar);

            if (!CanopyEcology.RollCatchUpBudAttempt(api, pos, wood, activity, gameYear)) return false;



            return TryBudFromSource(

                api, acc, pos, block, wood, activity, gameYear, index,

                budBranchy: false,

                forcePlace: true);

        }



        internal static bool ShouldCatchUpBud(

            ICoreAPI api,

            BlockPos pos,

            string wood,

            FoliageCellKind kind,

            out float activity)

        {

            activity = 0f;

            if (kind != FoliageCellKind.LogGrown && kind != FoliageCellKind.BranchyLeaf) return false;



            CanopySeasonPhase phase = CanopyEcology.ResolvePhase(api, pos, wood, out activity);

            if (phase == CanopySeasonPhase.Spring && activity >= 0.08f) return true;



            if (phase != CanopySeasonPhase.Idle || api?.World?.Calendar == null) return false;



            IGameCalendar cal = api.World.Calendar;

            float yearProgress = cal.DayOfYearf / cal.DaysPerYear;

            WildCanopySeason.Profile profile = WildCanopySeason.Resolve(wood);

            EcosystemConfig cfg = EcosystemConfig.Loaded;



            float defol = profile.DefoliateInterpolated(yearProgress) * cfg.CanopyActivityScale;

            float bud = profile.BudInterpolated(yearProgress) * cfg.CanopyActivityScale;

            float latMult = CanopyEcology.LatitudeMultiplierForCatchUp(api, pos, cfg.CanopyLatitudeInfluence);

            defol *= latMult;

            bud *= latMult;



            if (bud > 0.12f && bud > defol)

            {

                activity = bud;

                return true;

            }



            return false;

        }



        internal static bool NeedsSpringCatchUp(IBlockAccessor acc, BlockPos pos, string wood, FoliageCellKind kind)

        {

            WildCanopySeason.Profile profile = WildCanopySeason.Resolve(wood);



            if (kind == FoliageCellKind.LogGrown)

            {

                return CountFoliageInRadius(acc, pos, wood, FoliageCellKind.BranchyLeaf, 2)

                    < profile.MaxBranchyNearLog;

            }



            if (kind == FoliageCellKind.BranchyLeaf)

            {

                return CountFoliageInRadius(acc, pos, wood, FoliageCellKind.RegularLeaf, 2)

                    < profile.MaxRegularNearBranchy;

            }



            return false;

        }



        static int CountFoliageInRadius(

            IBlockAccessor acc,

            BlockPos center,

            string wood,

            FoliageCellKind kind,

            int radius)

        {

            int count = 0;

            var scratch = new BlockPos(0);

            for (int dx = -radius; dx <= radius; dx++)

            {

                for (int dy = -radius; dy <= radius; dy++)

                {

                    for (int dz = -radius; dz <= radius; dz++)

                    {

                        if (dx == 0 && dy == 0 && dz == 0) continue;



                        scratch.Set(center.X + dx, center.Y + dy, center.Z + dz);

                        if (!acc.IsValidPos(scratch)) continue;



                        Block neighbor = acc.GetBlock(scratch);

                        if (Classify(neighbor) != kind) continue;



                        string neighborWood = kind == FoliageCellKind.LogGrown

                            ? PlantCodeHelper.GetTreeWood(neighbor)

                            : CanopyBlockHelper.GetWoodFromFoliageBlock(neighbor);

                        if (neighborWood == wood) count++;

                    }

                }

            }



            return count;

        }



        internal static bool IsBareCrownSeason(ICoreAPI api, BlockPos pos, string wood)
        {
            CanopySeasonPhase phase = CanopyEcology.ResolvePhase(api, pos, wood, out _);
            if (phase == CanopySeasonPhase.Autumn || phase == CanopySeasonPhase.Spring) return false;
            if (phase != CanopySeasonPhase.Idle || api?.World?.Calendar == null) return false;

            IGameCalendar cal = api.World.Calendar;
            float yearProgress = cal.DayOfYearf / cal.DaysPerYear;
            return IsBareCrownSeasonForProgress(yearProgress, phase, EcosystemConfig.Loaded.CanopyActivityScale);
        }

        /// <summary>Winter dormant window — not active autumn defoliation.</summary>
        internal static bool IsBareCrownSeasonForProgress(float yearProgress, CanopySeasonPhase phase, float activityScale)
        {
            if (phase == CanopySeasonPhase.Autumn || phase == CanopySeasonPhase.Spring) return false;
            if (phase != CanopySeasonPhase.Idle) return false;

            WildCanopySeason.Profile profile = WildCanopySeason.Resolve("oak");
            float defol = profile.DefoliateInterpolated(yearProgress) * activityScale;
            float bud = profile.BudInterpolated(yearProgress) * activityScale;
            return bud < 0.1f && defol < 0.1f;
        }

        internal static bool ShouldRestoreBareSkeletonOnLog(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos logPos,
            string wood)
        {
            if (!EcosystemConfig.Loaded.FoliageRestoreBareSkeleton) return false;
            if (api == null || acc == null || logPos == null || string.IsNullOrEmpty(wood)) return false;
            if (!IsBareCrownSeason(api, logPos, wood)) return false;
            return IsLogInCrownZone(acc, logPos, wood);
        }

        /// <summary>Skip lower trunk — branchy repair only in the crown column.</summary>
        internal static bool IsLogInCrownZone(IBlockAccessor acc, BlockPos logPos, string wood)
        {
            if (acc == null || logPos == null || string.IsNullOrEmpty(wood)) return false;

            BlockPos basePos = PlantCodeHelper.GetTreeTrunkBase(acc, logPos);
            int heightAboveBase = logPos.Y - basePos.Y;
            if (heightAboveBase < 2) return false;

            int trunkTop = GetColumnTrunkTopY(acc, logPos.X, logPos.Z, wood, basePos.Y);
            int trunkHeight = trunkTop - basePos.Y;
            if (trunkHeight <= 0) return true;

            return heightAboveBase >= trunkHeight / 3;
        }

        static int GetColumnTrunkTopY(IBlockAccessor acc, int x, int z, string wood, int baseY)
        {
            int top = baseY;
            var scratch = new BlockPos(x, baseY, z);
            for (int y = baseY + 1; y < acc.MapSizeY; y++)
            {
                scratch.Set(x, y, z);
                if (!acc.IsValidPos(scratch)) break;

                Block block = acc.GetBlock(scratch);
                if (!PlantCodeHelper.IsTreeLogGrownBlock(block)) break;
                if (PlantCodeHelper.GetTreeWood(block) != wood) break;

                top = y;
            }

            return top;
        }



        internal static bool ShouldCatchUpStripRegularLeaf(

            ICoreAPI api,

            BlockPos pos,

            string wood,

            out float activity)

        {

            activity = 0f;

            CanopySeasonPhase phase = CanopyEcology.ResolvePhase(api, pos, wood, out activity);

            if (phase == CanopySeasonPhase.Autumn) return true;



            if (phase != CanopySeasonPhase.Idle || api?.World?.Calendar == null) return false;



            IGameCalendar cal = api.World.Calendar;

            float yearProgress = cal.DayOfYearf / cal.DaysPerYear;

            WildCanopySeason.Profile profile = WildCanopySeason.Resolve(wood);

            EcosystemConfig cfg = EcosystemConfig.Loaded;



            float defol = profile.DefoliateInterpolated(yearProgress) * cfg.CanopyActivityScale;

            float bud = profile.BudInterpolated(yearProgress) * cfg.CanopyActivityScale;

            float latMult = CanopyEcology.LatitudeMultiplierForCatchUp(api, pos, cfg.CanopyLatitudeInfluence);

            defol *= latMult;

            bud *= latMult;



            if (bud < 0.1f && defol < 0.1f)

            {

                activity = 0.8f;

                return true;

            }



            return false;

        }



        /// <returns>True when a block was placed or removed.</returns>

        public static bool TickCell(ICoreAPI api, IBlockAccessor acc, BlockPos pos, Block block, FoliageCellIndex index)

        {

            if (api == null || acc == null || pos == null || block == null) return false;

            if (!EcosystemConfig.Loaded.EnableSeasonalFoliage) return false;



            FoliageCellKind kind = Classify(block);

            if (kind == FoliageCellKind.None) return false;



            string wood = kind == FoliageCellKind.LogGrown

                ? PlantCodeHelper.GetTreeWood(block)

                : CanopyBlockHelper.GetWoodFromFoliageBlock(block);

            if (string.IsNullOrEmpty(wood)) return false;

            if (TreeSenescence.BlocksSeasonalCanopy(api, acc, pos, wood)) return false;



            CanopySeasonPhase phase = CanopyEcology.ResolvePhase(api, pos, wood, out float activity);
            int gameYear = CanopyEcology.GameYear(api.World.Calendar);

            if (kind == FoliageCellKind.LogGrown
                && ShouldRestoreBareSkeletonOnLog(api, acc, pos, wood))
            {
                if (activity <= 0f)
                {
                    ShouldCatchUpStripRegularLeaf(api, pos, wood, out activity);
                }

                if (activity > 0f)
                {
                    return TryRestoreBareSkeleton(api, acc, pos, block, wood, activity, gameYear, index);
                }
            }

            if (phase == CanopySeasonPhase.Idle || activity <= 0f) return false;

            switch (kind)

            {

                case FoliageCellKind.RegularLeaf when phase == CanopySeasonPhase.Autumn:

                    return TryStripFoliage(api, acc, pos, wood, activity, gameYear, FoliageCellKind.RegularLeaf, index);



                case FoliageCellKind.BranchyLeaf when phase == CanopySeasonPhase.Autumn

                    && ShouldStripBranchyInAutumn(activity):

                    return TryStripFoliage(api, acc, pos, wood, activity, gameYear, FoliageCellKind.BranchyLeaf, index);



                case FoliageCellKind.BranchyLeaf when phase == CanopySeasonPhase.Spring:

                    return TryBudFromSource(

                        api, acc, pos, block, wood, activity, gameYear, index,

                        budBranchy: false);



                default:

                    return false;

            }

        }



        static bool TryRestoreBareSkeleton(

            ICoreAPI api,

            IBlockAccessor acc,

            BlockPos logPos,

            Block logBlock,

            string wood,

            float activity,

            int gameYear,

            FoliageCellIndex index)

        {

            if (HasAdjacentBranchy(acc, logPos, wood)) return false;

            if (!CanopyEcology.RollSkeletonRestoreAttempt(api, logPos, wood, activity, gameYear)) return false;



            return TryBudFromSource(

                api, acc, logPos, logBlock, wood, activity, gameYear, index,

                budBranchy: true,

                forcePlace: true);

        }



        static bool HasAdjacentBranchy(IBlockAccessor acc, BlockPos center, string wood)
        {
            return HasAdjacentBranchyLeaf(acc, center, wood);
        }

        internal static bool HasAdjacentBranchyLeaf(IBlockAccessor acc, BlockPos center, string wood)
        {
            var scratch = new BlockPos(0);
            for (int i = 0; i < 6; i++)
            {
                scratch.Set(
                    center.X + NeighborDx[i],
                    center.Y + NeighborDy[i],
                    center.Z + NeighborDz[i]);
                if (!acc.IsValidPos(scratch)) continue;

                Block neighbor = acc.GetBlock(scratch);
                if (Classify(neighbor) == FoliageCellKind.BranchyLeaf
                    && CanopyBlockHelper.GetWoodFromFoliageBlock(neighbor) == wood)
                {
                    return true;
                }
            }

            return false;
        }

        static bool TryStripFoliage(

            ICoreAPI api,

            IBlockAccessor acc,

            BlockPos pos,

            string wood,

            float activity,

            int gameYear,

            FoliageCellKind kind,

            FoliageCellIndex index)

        {

            if (!LandClaimGuard.AllowsEcologyChange(api, pos)) return false;



            float stripActivity = activity;
            if (kind == FoliageCellKind.RegularLeaf)
            {
                stripActivity *= CanopyCrownBias.StripActivityScale(acc, pos, wood);
            }

            bool roll = kind == FoliageCellKind.BranchyLeaf

                ? CanopyEcology.RollBranchyStripAttempt(api, pos, wood, stripActivity, gameYear)

                : CanopyEcology.RollStripAttempt(api, pos, wood, stripActivity, gameYear);

            if (!roll) return false;



            acc.SetBlock(0, pos);

            acc.MarkBlockDirty(pos);

            index?.Remove(pos);

            CanopyFallenSticks.TryDropFromStrip(api, acc, pos, wood, activity, gameYear, kind);

            return true;

        }



        static bool TryBudFromSource(

            ICoreAPI api,

            IBlockAccessor acc,

            BlockPos sourcePos,

            Block sourceBlock,

            string wood,

            float activity,

            int gameYear,

            FoliageCellIndex index,

            bool budBranchy,

            bool forcePlace = false)

        {
            if (!LandClaimGuard.AllowsEcologyChange(api, sourcePos)) return false;

            if (TreeSenescence.BlocksSeasonalCanopy(api, acc, sourcePos, wood)) return false;
            if (CanopyBurnGuard.SuppressesFoliagePlacement(acc, sourcePos)) return false;

            if (budBranchy)
            {
                activity *= CanopyTreeAgeBoost.SpringBranchyBudMultiplier(api, acc, sourcePos, wood);
            }

            if (budBranchy && !forcePlace && Classify(sourceBlock) == FoliageCellKind.LogGrown)
            {
                return false;
            }

            var scratch = new BlockPos(0);

            int start = api.World.Rand.Next(6);



            for (int n = 0; n < 6; n++)

            {

                int i = (start + n) % 6;

                scratch.Set(

                    sourcePos.X + NeighborDx[i],

                    sourcePos.Y + NeighborDy[i],

                    sourcePos.Z + NeighborDz[i]);

                if (!acc.IsValidPos(scratch)) continue;

                if (!LandClaimGuard.AllowsEcologyChange(api, scratch)) continue;

                if (CanopyBurnGuard.SuppressesBudTarget(acc, scratch)) continue;



                Block space = acc.GetBlock(scratch);

                if (!PlantVacancyRules.IsVacantPlantSpace(space)) continue;

                if (BlocksPlayerClearedVacancy(api, scratch)) continue;



                float budRollActivity = activity;
                if (!budBranchy)
                {
                    budRollActivity *= CanopyCrownBias.BudActivityScale(acc, scratch, wood);
                }

                if (!forcePlace && !CanopyEcology.RollBudAttempt(api, scratch, wood, budRollActivity, gameYear)) continue;



                Block placed = budBranchy

                    ? CanopyBlockHelper.ResolveBranchyLeafBlock(api.World, wood, scratch, sourcePos, sourceBlock)

                    : CanopyBlockHelper.ResolveGrownLeafBlock(api.World, wood, scratch, sourcePos, sourceBlock);



                if (placed == null || placed.Id == 0) continue;



                acc.SetBlock(placed.BlockId, scratch);

                acc.MarkBlockDirty(scratch);

                index?.Add(scratch);

                return true;

            }



            return false;

        }

        internal static bool TryStripForced(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos pos,
            FoliageCellIndex index,
            string wood = null,
            float autumnActivity = 0f,
            int gameYear = 0,
            FoliageCellKind strippedKind = FoliageCellKind.None)
        {
            if (!LandClaimGuard.AllowsEcologyChange(api, pos)) return false;

            acc.SetBlock(0, pos);
            acc.MarkBlockDirty(pos);
            index?.Remove(pos);

            if (!string.IsNullOrEmpty(wood) && strippedKind == FoliageCellKind.BranchyLeaf)
            {
                CanopyFallenSticks.TryDropFromStrip(api, acc, pos, wood, autumnActivity, gameYear, strippedKind);
            }

            return true;
        }

        internal static bool TryPlaceSeasonBudDeterministic(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos sourcePos,
            Block sourceBlock,
            string wood,
            bool budBranchy,
            float activity,
            int gameYear,
            FoliageCellIndex index)
        {
            if (!LandClaimGuard.AllowsEcologyChange(api, sourcePos)) return false;
            if (CanopyBurnGuard.SuppressesFoliagePlacement(acc, sourcePos)) return false;

            float budActivity = activity;
            if (budBranchy)
            {
                budActivity *= CanopyTreeAgeBoost.SpringBranchyBudMultiplier(api, acc, sourcePos, wood);
            }

            var scratch = new BlockPos(0);
            for (int i = 0; i < 6; i++)
            {
                scratch.Set(
                    sourcePos.X + NeighborDx[i],
                    sourcePos.Y + NeighborDy[i],
                    sourcePos.Z + NeighborDz[i]);
                if (!acc.IsValidPos(scratch)) continue;
                if (!LandClaimGuard.AllowsEcologyChange(api, scratch)) continue;
                if (CanopyBurnGuard.SuppressesBudTarget(acc, scratch)) continue;

                Block space = acc.GetBlock(scratch);
                if (!PlantVacancyRules.IsVacantPlantSpace(space)) continue;

                if (BlocksPlayerClearedVacancy(api, scratch)) continue;

                float noise = 0.55f + CanopyBlockHelper.DeterministicNoise(scratch, wood, gameYear) * 0.45f;
                float threshold = budActivity * noise * 0.78f;
                if (!budBranchy)
                {
                    threshold *= CanopyCrownBias.BudActivityScale(acc, scratch, wood);
                }
                if (threshold > 1f) threshold = 1f;
                float gate = CanopyBlockHelper.DeterministicNoise(scratch, wood, gameYear + 500 + i);
                if (gate >= threshold) continue;

                Block placed = budBranchy
                    ? CanopyBlockHelper.ResolveBranchyLeafBlock(api.World, wood, scratch, sourcePos, sourceBlock)
                    : CanopyBlockHelper.ResolveGrownLeafBlock(api.World, wood, scratch, sourcePos, sourceBlock);

                if (placed == null || placed.Id == 0) continue;

                acc.SetBlock(placed.BlockId, scratch);
                acc.MarkBlockDirty(scratch);
                index?.Add(scratch);
                return true;
            }

            return false;
        }

        internal static bool BlocksPlayerClearedVacancy(ICoreAPI api, BlockPos target)
        {
            if (api?.World?.Calendar == null || target == null) return false;

            FoliagePlayerVacancySuppressor suppressor = EcosystemSystem.Instance?.FoliagePlayerVacancies;
            return suppressor != null
                && suppressor.BlocksBudAt(target, api.World.Calendar.TotalHours);
        }

    }

}
