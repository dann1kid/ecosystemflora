using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-wood monthly defoliate and bud activity (0=Jan..11=Dec). Scales with world calendar length.</summary>
    internal static class WildCanopySeason
    {
        public readonly struct Profile
        {
            readonly float[] defoliate;
            readonly float[] bud;
            readonly float branchyCatchUpScale;
            readonly float leafCatchUpScale;
            readonly int maxBranchyNearLog;
            readonly int maxRegularNearBranchy;

            public Profile(
                float[] monthlyDefoliate,
                float[] monthlyBud,
                float branchyCatchUpScale = 0.58f,
                float leafCatchUpScale = 0.68f,
                int maxBranchyNearLog = 8,
                int maxRegularNearBranchy = 4)
            {
                defoliate = monthlyDefoliate;
                bud = monthlyBud;
                this.branchyCatchUpScale = branchyCatchUpScale;
                this.leafCatchUpScale = leafCatchUpScale;
                this.maxBranchyNearLog = maxBranchyNearLog;
                this.maxRegularNearBranchy = maxRegularNearBranchy;
            }

            public float DefoliateInterpolated(float yearProgress)
            {
                return InterpolateCurve(defoliate, yearProgress);
            }

            public float BudInterpolated(float yearProgress)
            {
                return InterpolateCurve(bud, yearProgress);
            }

            /// <summary>Spring chunk catch-up: log → branchy attempt scale (species bushy-ness).</summary>
            public float BranchyCatchUpScale => branchyCatchUpScale;

            /// <summary>Spring chunk catch-up: branchy → leaves-grown attempt scale.</summary>
            public float LeafCatchUpScale => leafCatchUpScale;

            /// <summary>Max branchy blocks within 2 blocks of a log before catch-up skips it.</summary>
            public int MaxBranchyNearLog => maxBranchyNearLog;

            /// <summary>Max regular leaves within 2 blocks of branchy before leaf catch-up skips.</summary>
            public int MaxRegularNearBranchy => maxRegularNearBranchy;

            static float InterpolateCurve(float[] curve, float yearProgress)
            {
                if (curve == null || curve.Length != 12) return 0f;
                float monthF = yearProgress * 12f;
                int m0 = ((int)monthF) % 12;
                if (m0 < 0) m0 += 12;
                int m1 = (m0 + 1) % 12;
                float t = monthF - (int)monthF;
                return curve[m0] * (1f - t) + curve[m1] * t;
            }
        }

        // Early deciduous: birch, maples — Sep drop, Mar bud; airy crown, early fill-in
        static readonly Profile EarlyDeciduous = new Profile(
            new float[] { 0f, 0f, 0f, 0f, 0f, 0f, 0.1f, 0.5f, 1.4f, 1.8f, 1.2f, 0.4f },
            new float[] { 0f, 0.15f, 1.6f, 1.8f, 1.2f, 0.5f, 0.1f, 0f, 0f, 0f, 0f, 0f },
            branchyCatchUpScale: 0.74f,
            leafCatchUpScale: 0.84f,
            maxBranchyNearLog: 7,
            maxRegularNearBranchy: 4);

        // Mid: oak, walnut — Oct–Nov drop, Apr bud; bushy oak skeleton
        static readonly Profile MidDeciduous = new Profile(
            new float[] { 0f, 0f, 0f, 0f, 0f, 0f, 0.05f, 0.25f, 0.9f, 1.6f, 1.8f, 0.8f },
            new float[] { 0f, 0f, 0.4f, 1.2f, 1.8f, 1.3f, 0.4f, 0.1f, 0f, 0f, 0f, 0f },
            branchyCatchUpScale: 0.58f,
            leafCatchUpScale: 0.68f,
            maxBranchyNearLog: 10,
            maxRegularNearBranchy: 5);

        // Late / warm-tolerant: acacia, kapok — Nov–Dec, May bud; sparser late crown
        static readonly Profile LateDeciduous = new Profile(
            new float[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.15f, 0.5f, 1.2f, 1.7f, 1.5f },
            new float[] { 0f, 0f, 0.2f, 0.7f, 1.4f, 1.9f, 1.2f, 0.4f, 0f, 0f, 0f, 0f },
            branchyCatchUpScale: 0.46f,
            leafCatchUpScale: 0.54f,
            maxBranchyNearLog: 6,
            maxRegularNearBranchy: 3);

        static readonly Dictionary<string, Profile> ByWood = Build();

        static Dictionary<string, Profile> Build()
        {
            return new Dictionary<string, Profile>
            {
                ["birch"] = EarlyDeciduous,
                ["maple"] = EarlyDeciduous,
                ["crimsonkingmaple"] = EarlyDeciduous,

                ["oak"] = MidDeciduous,
                ["walnut"] = MidDeciduous,

                ["acacia"] = LateDeciduous,
                ["kapok"] = LateDeciduous,
                ["ebony"] = LateDeciduous,
                ["purpleheart"] = LateDeciduous,
            };
        }

        public static bool TryGet(string wood, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(wood)) return false;
            if (!MyceliumTreeHost.IsDeciduousWood(wood)) return false;
            return ByWood.TryGetValue(wood, out profile);
        }

        public static Profile Resolve(string wood)
        {
            if (TryGet(wood, out Profile profile)) return profile;
            return MidDeciduous;
        }
    }
}
