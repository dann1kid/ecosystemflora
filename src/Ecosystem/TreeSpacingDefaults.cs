using System;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Per-wood Chebyshev trunk spacing grounded in typical mature crown size
    /// (<see cref="WildTreeGrowthProfiles.Profile.ReferenceCrownRadius"/>) and seral habit.
    /// Trees must never use same-species spacing 0 (that means meadow patch clumps).
    /// </summary>
    internal static class TreeSpacingDefaults
    {
        public static bool IsTreeLike(EcologyHabitat habitat) =>
            habitat == EcologyHabitat.TerrestrialTree
            || habitat == EcologyHabitat.Ferntree;

        /// <summary>
        /// Resolve same/other spacing for a wood or ferntree species code.
        /// Prefer hand-tuned <see cref="WildTreeEcology"/> / ferntree rows when present and &gt; 0;
        /// otherwise derive from reference crown (+ open-canopy bias for pioneers).
        /// </summary>
        public static void Resolve(string woodOrSpecies, out int sameSpecies, out int otherSpecies)
        {
#pragma warning disable CS0618
            if (WildFerntreeEcology.IsSpecies(woodOrSpecies))
            {
                WildFerntreeEcology.Profile fern = WildFerntreeEcology.Resolve();
                sameSpecies = fern.SameSpeciesSpacing > 0 ? fern.SameSpeciesSpacing : 8;
                otherSpecies = fern.OtherSpeciesSpacing > 0 ? fern.OtherSpeciesSpacing : 5;
                return;
            }

            if (WildTreeEcology.TryGet(woodOrSpecies, out WildTreeEcology.Profile eco)
                && eco.SameSpeciesSpacing > 0)
            {
                sameSpecies = eco.SameSpeciesSpacing;
                otherSpecies = eco.OtherSpeciesSpacing > 0
                    ? eco.OtherSpeciesSpacing
                    : Math.Max(2, sameSpecies - 1);
                return;
            }

            TreeSeralRole role = TreeSeralRole.Mid;
            if (WildTreeEcology.TryGet(woodOrSpecies, out eco))
            {
                role = eco.SeralRole;
            }
#pragma warning restore CS0618

            WildTreeGrowthProfiles.Profile growth = WildTreeGrowthProfiles.Resolve(woodOrSpecies);
            int crown = Math.Max(2, growth.ReferenceCrownRadius);
            int bias = role == TreeSeralRole.Pioneer ? 2 : role == TreeSeralRole.Mid ? 1 : 0;

            sameSpecies = Math.Max(3, crown + bias);
            otherSpecies = Math.Max(2, crown);
        }

        /// <summary>Fill missing (≤0) tree spacings on an already-built requirements row.</summary>
        public static void EnsureOn(PlantRequirements req)
        {
            if (req == null || !IsTreeLike(req.Habitat)) return;

            if (req.SameSpeciesSpacing > 0 && req.OtherSpeciesSpacing > 0) return;

            Resolve(req.Species, out int same, out int other);
            if (req.SameSpeciesSpacing <= 0) req.SameSpeciesSpacing = same;
            if (req.OtherSpeciesSpacing <= 0) req.OtherSpeciesSpacing = other;
        }
    }
}
