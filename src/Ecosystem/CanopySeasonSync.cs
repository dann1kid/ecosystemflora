using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Deterministic per-cell season sync during chunk column passes.</summary>
    internal static class CanopySeasonSync
    {
        public static bool TrySyncCell(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos pos,
            Block block,
            FoliageCellIndex index,
            int gameYear,
            out bool isFoliageCell)
        {
            isFoliageCell = false;
            if (api == null || acc == null || pos == null || block == null) return false;
            if (!EcosystemConfig.Loaded.EnableSeasonalFoliage) return false;

            FoliageCellKind kind = CanopyFoliageRules.Classify(block);
            if (kind == FoliageCellKind.None) return false;

            isFoliageCell = true;

            string wood = kind == FoliageCellKind.LogGrown
                ? PlantCodeHelper.GetTreeWood(block)
                : CanopyBlockHelper.GetWoodFromFoliageBlock(block);
            if (string.IsNullOrEmpty(wood)) return false;

            if (TreeSenescence.BlocksSeasonalCanopy(api, acc, pos, wood)) return false;

            WildCanopySeason.Profile profile = WildCanopySeason.Resolve(wood);

            if (kind == FoliageCellKind.RegularLeaf)
            {
                if (!CanopyFoliageRules.ShouldCatchUpStripRegularLeaf(api, pos, wood, out float activity)) return false;

                CanopySeasonPhase phase = CanopyEcology.ResolvePhase(api, pos, wood, out _);
                int month = CalendarMonthIndex(api);

                // Winter idle / Dec–Feb: force strip remaining leaves-grown (bare skeleton season).
                // Never force-strip on summer Idle gaps — that was nuking warm-season canopy.
                if (phase == CanopySeasonPhase.Idle)
                {
                    if (!CanopyFoliageRules.IsWinterBareMonth(month)) return false;
                    return CanopyFoliageRules.TryStripForced(api, acc, pos, index);
                }

                if (phase != CanopySeasonPhase.Autumn) return false;

                if (CanopyFoliageRules.IsWinterBareMonth(month))
                {
                    return CanopyFoliageRules.TryStripForced(api, acc, pos, index);
                }

                // Active autumn (typically Aug–Nov): patchy strip. Ignore tiny curve ramps
                // so early-warm months do not drip-strip a full crown.
                if (activity < MinPatchyAutumnStripActivity) return false;
                if (!PassesDeterministicGate(api, acc, pos, wood, gameYear, activity, stripScale: 0.55f)) return false;
                return CanopyFoliageRules.TryStripForced(api, acc, pos, index);
            }

            if (kind == FoliageCellKind.BranchyLeaf)
            {
                CanopySeasonPhase phase = CanopyEcology.ResolvePhase(api, pos, wood, out float activity);

                if (phase == CanopySeasonPhase.Autumn
                    && EcosystemConfig.Loaded.FoliagePeakAutumnBranchyStripActivity > 0f
                    && activity >= EcosystemConfig.Loaded.FoliagePeakAutumnBranchyStripActivity
                    && PassesDeterministicGate(api, acc, pos, wood, gameYear + 3, activity, stripScale: 0.22f))
                {
                    return CanopyFoliageRules.TryStripForced(
                        api, acc, pos, index, wood, activity, gameYear, FoliageCellKind.BranchyLeaf);
                }

                if (!CanopyFoliageRules.ShouldCatchUpBud(api, pos, wood, kind, out activity)) return false;
                if (!CanopyFoliageRules.NeedsSpringCatchUp(acc, pos, wood, FoliageCellKind.BranchyLeaf)) return false;

                activity *= profile.LeafCatchUpScale;
                return CanopyFoliageRules.TryPlaceSeasonBudDeterministic(
                    api, acc, pos, block, wood, budBranchy: false, activity, gameYear, index);
            }

            if (kind == FoliageCellKind.LogGrown)
            {
                if (EcosystemConfig.Loaded.FoliageRestoreBareSkeleton
                    && CanopyFoliageRules.IsBareCrownSeason(api, pos, wood)
                    && CanopyFoliageRules.IsLogInCrownZone(acc, pos, wood)
                    && !CanopyFoliageRules.HasAdjacentBranchyLeaf(acc, pos, wood))
                {
                    CanopyEcology.ResolvePhase(api, pos, wood, out float bareActivity);
                    if (bareActivity <= 0f)
                    {
                        CanopyFoliageRules.ShouldCatchUpStripRegularLeaf(api, pos, wood, out bareActivity);
                    }

                    if (bareActivity > 0f
                        && PassesDeterministicGate(api, acc, pos, wood, gameYear + 11, bareActivity, stripScale: 0.14f))
                    {
                        return CanopyFoliageRules.TryPlaceSeasonBudDeterministic(
                            api, acc, pos, block, wood, budBranchy: true, bareActivity, gameYear, index);
                    }
                }

                // Spring: grow branchy scaffold from crown logs so branchy→leaf dress has enough anchors.
                // (Without this, only sparse winter repair / aging feed leaves — crowns stay thin.)
                CanopySeasonPhase logPhase = CanopyEcology.ResolvePhase(api, pos, wood, out float logActivity);
                if (logPhase == CanopySeasonPhase.Spring
                    && logActivity >= 0.08f
                    && CanopyFoliageRules.IsLogInCrownZone(acc, pos, wood)
                    && CanopyFoliageRules.NeedsSpringCatchUp(acc, pos, wood, FoliageCellKind.LogGrown))
                {
                    logActivity *= profile.BranchyCatchUpScale;
                    return CanopyFoliageRules.TryPlaceSeasonBudDeterministic(
                        api, acc, pos, block, wood, budBranchy: true, logActivity, gameYear, index);
                }

                return false;
            }

            return false;
        }

        /// <summary>Below this autumn activity, month sync does not strip leaves (avoids tiny Jul ramps).</summary>
        internal const float MinPatchyAutumnStripActivity = 0.28f;

        /// <summary>
        /// Patchy strip only during active autumn months. Dec–Feb force-strip is handled separately
        /// via winter Idle / winter autumn residual — never via this helper returning false for summer Idle.
        /// </summary>
        internal static bool ShouldUsePatchyRegularLeafStrip(ICoreAPI api, CanopySeasonPhase phase)
        {
            if (api?.World?.Calendar == null) return phase == CanopySeasonPhase.Autumn;

            float yearProgress = api.World.Calendar.DayOfYearf / api.World.Calendar.DaysPerYear;
            int month = ((int)(yearProgress * 12f)) % 12;
            if (month < 0) month += 12;

            return ShouldUsePatchyRegularLeafStripForMonth(phase, month);
        }

        internal static bool ShouldUsePatchyRegularLeafStripForMonth(CanopySeasonPhase phase, int month)
        {
            if (phase != CanopySeasonPhase.Autumn) return false;
            if (CanopyFoliageRules.IsWinterBareMonth(month)) return false;
            return true;
        }

        static int CalendarMonthIndex(ICoreAPI api)
        {
            if (api?.World?.Calendar == null) return 0;
            float yearProgress = api.World.Calendar.DayOfYearf / api.World.Calendar.DaysPerYear;
            int month = ((int)(yearProgress * 12f)) % 12;
            if (month < 0) month += 12;
            return month;
        }

        static bool PassesDeterministicGate(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos pos,
            string wood,
            int salt,
            float activity,
            float stripScale)
        {
            if (activity <= 0f) return false;

            if (acc != null && pos != null)
            {
                activity *= CanopyCrownBias.StripActivityScale(acc, pos, wood);
            }

            float noise = 0.55f + CanopyBlockHelper.DeterministicNoise(pos, wood, salt) * 0.45f;
            float threshold = activity * noise * stripScale;
            if (threshold > 1f) threshold = 1f;
            if (threshold < 0f) threshold = 0f;

            float gate = CanopyBlockHelper.DeterministicNoise(pos, wood, salt + 1000);
            return gate < threshold;
        }
    }
}
