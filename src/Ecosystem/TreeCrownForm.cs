using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Species silhouette for yearly crown growth (vanilla foliage only).</summary>
    public enum TreeCrownForm
    {
        /// <summary>Wide upper shelf, clearer bole — oak / maple / walnut.</summary>
        Spreading = 0,

        /// <summary>Mid-height oval crown — birch and similar.</summary>
        Oval = 1,

        /// <summary>Flat wide top, high crown break — acacia / kapok.</summary>
        Umbrella = 2,

        /// <summary>Narrow vertical crown — cypress / pine / larch.</summary>
        Column = 3,

        /// <summary>Heavy upper tiers — redwood / baldcypress.</summary>
        Tiered = 4,
    }

    /// <summary>Target crown envelope for a species form (radius vs height along the trunk).</summary>
    internal static class TreeCrownEnvelope
    {
        /// <summary>Fraction of trunk height below which foliage growth is discouraged.</summary>
        public static float CrownBaseFraction(TreeCrownForm form)
        {
            switch (form)
            {
                case TreeCrownForm.Spreading: return 0.48f;
                case TreeCrownForm.Oval: return 0.28f;
                case TreeCrownForm.Umbrella: return 0.58f;
                case TreeCrownForm.Column: return 0.18f;
                case TreeCrownForm.Tiered: return 0.38f;
                default: return 0.35f;
            }
        }

        /// <summary>Extra blocks above trunk tip still inside the crown envelope.</summary>
        public static int TipExtraBlocks(TreeCrownForm form)
        {
            switch (form)
            {
                case TreeCrownForm.Umbrella: return 2;
                case TreeCrownForm.Spreading: return 3;
                case TreeCrownForm.Tiered: return 4;
                case TreeCrownForm.Oval: return 2;
                default: return 1;
            }
        }

        /// <summary>
        /// Target radius scale at crown progress <paramref name="t"/> (0 = crown base, 1 = tip).
        /// Multiplies the species soft crown radius.
        /// </summary>
        public static float RadiusScaleAtCrownT(TreeCrownForm form, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;

            switch (form)
            {
                case TreeCrownForm.Spreading:
                    // Narrow at crown break, widest near the tip.
                    return 0.35f + 0.65f * (t * t);

                case TreeCrownForm.Oval:
                    // Peak near mid-crown, taper toward base and tip.
                    float mid = 1f - System.Math.Abs(t - 0.55f) * 1.6f;
                    return System.Math.Max(0.4f, mid);

                case TreeCrownForm.Umbrella:
                    // Stay tight until high, then open a flat shelf.
                    if (t < 0.65f) return 0.2f + 0.25f * (t / 0.65f);
                    float u = (t - 0.65f) / 0.35f;
                    return 0.45f + 0.55f * u;

                case TreeCrownForm.Column:
                    return 0.45f + 0.15f * t;

                case TreeCrownForm.Tiered:
                    // Wide upper third, moderate mid, thin lower crown.
                    if (t < 0.4f) return 0.3f + 0.35f * (t / 0.4f);
                    if (t < 0.7f) return 0.65f + 0.2f * ((t - 0.4f) / 0.3f);
                    return 0.85f + 0.15f * ((t - 0.7f) / 0.3f);

                default:
                    return 0.7f;
            }
        }

        public static int CrownStartY(BlockPos trunkBase, TreeStructureMetrics metrics, TreeCrownForm form)
        {
            int bole = System.Math.Max(2, (int)(metrics.TrunkHeight * CrownBaseFraction(form) + 0.5f));
            return trunkBase.Y + bole;
        }

        public static int CrownTopY(TreeStructureMetrics metrics, TreeCrownForm form)
        {
            return metrics.TrunkTop.Y + TipExtraBlocks(form);
        }

        /// <summary>Allowed Chebyshev-ish horizontal radius at world Y for this form.</summary>
        public static int AllowedRadiusAtY(
            WildTreeGrowthProfiles.Profile profile,
            TreeStructureMetrics metrics,
            BlockPos trunkBase,
            int y)
        {
            TreeCrownForm form = profile.CrownForm;
            int startY = CrownStartY(trunkBase, metrics, form);
            int topY = CrownTopY(metrics, form);
            if (y < startY) return 0;
            if (topY <= startY) return System.Math.Max(1, profile.ReferenceCrownRadius / 2);

            float t = (y - startY) / (float)(topY - startY);
            int softMax = System.Math.Max(
                profile.ReferenceCrownRadius,
                System.Math.Min(TreeStructureProbe.MaxCrownScanRadius, metrics.CrownRadius + 2));
            float scale = RadiusScaleAtCrownT(form, t);
            int allowed = (int)System.Math.Ceiling(softMax * scale);
            if (allowed < 1) allowed = 1;
            return System.Math.Min(TreeStructureProbe.MaxCrownScanRadius, allowed);
        }

        public static bool AllowsCell(
            WildTreeGrowthProfiles.Profile profile,
            TreeStructureMetrics metrics,
            BlockPos trunkBase,
            BlockPos cell)
        {
            if (cell == null || trunkBase == null) return false;
            int allowed = AllowedRadiusAtY(profile, metrics, trunkBase, cell.Y);
            if (allowed <= 0) return false;

            int dx = System.Math.Abs(cell.X - trunkBase.X);
            int dz = System.Math.Abs(cell.Z - trunkBase.Z);
            // Slightly elliptical: Chebyshev works well with blocky crowns.
            return System.Math.Max(dx, dz) <= allowed;
        }

        /// <summary>Score for ordering yearly anchors — higher = try first.</summary>
        public static int AnchorPriority(
            TreeCrownForm form,
            TreeStructureMetrics metrics,
            BlockPos trunkBase,
            BlockPos anchor)
        {
            int startY = CrownStartY(trunkBase, metrics, form);
            int topY = CrownTopY(metrics, form);
            float t = topY <= startY
                ? 1f
                : (anchor.Y - startY) / (float)(topY - startY);
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;

            switch (form)
            {
                case TreeCrownForm.Spreading:
                case TreeCrownForm.Umbrella:
                case TreeCrownForm.Tiered:
                    return (int)(t * 100f);

                case TreeCrownForm.Oval:
                    // Prefer mid-crown band.
                    return (int)((1f - System.Math.Abs(t - 0.55f)) * 100f);

                case TreeCrownForm.Column:
                    // Mild top preference; mostly fill near the column.
                    return (int)(40f + t * 40f);

                default:
                    return (int)(t * 100f);
            }
        }

        /// <summary>When true, yearly growth prefers horizontal neighbors (widen shelf).</summary>
        public static bool PreferHorizontalFill(TreeCrownForm form, bool crownLags)
        {
            if (crownLags) return true;
            return form == TreeCrownForm.Spreading
                || form == TreeCrownForm.Umbrella
                || form == TreeCrownForm.Tiered;
        }

        /// <summary>Umbrella / spreading tip dress may place a cell above the tip; column less so.</summary>
        public static bool DressAboveTip(TreeCrownForm form) =>
            form != TreeCrownForm.Column;
    }
}
