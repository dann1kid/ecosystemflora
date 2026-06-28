using System;

#pragma warning disable CS0618

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>Handbook + inspect helpers that prefer merged CSV registry over C# tables.</summary>
    internal static class SpeciesEcologyDisplay
    {
        public static bool TryGetRow(string species, out SpeciesEcologyCsvRow row)
        {
            row = null;
            if (string.IsNullOrEmpty(species)) return false;
            return SpeciesEcologyRegistry.IsLoaded && SpeciesEcologyRegistry.TryGet(species, out row);
        }

        public static float ResolveSpreadRate(string species, PlantRequirements req = null)
        {
            if (TryGetRow(species, out SpeciesEcologyCsvRow row)) return row.SpreadRate;
            if (req != null) return req.SpreadRate;
            return ResolveSpreadRateLegacy(species);
        }

        public static float ResolveHoldStrength(string species, PlantRequirements req = null)
        {
            if (TryGetRow(species, out SpeciesEcologyCsvRow row) && row.HoldStrength > 0f) return row.HoldStrength;
            if (req != null && req.HoldStrength > 0f) return req.HoldStrength;
            if (WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile mod)) return mod.HoldStrength;
            return 1f;
        }

        public static string GetDominanceLabelLangKey(string species, PlantRequirements req = null)
        {
            if (TryGetRow(species, out SpeciesEcologyCsvRow row))
            {
                return GetDominanceFromRow(row, req);
            }

            return GetDominanceLabelLangKeyLegacy(species, req);
        }

        public static bool HasNicheProfile(string species, PlantRequirements req = null)
        {
            if (req != null && req.HasNicheProfile) return true;
            if (TryGetRow(species, out SpeciesEcologyCsvRow row))
            {
                return !string.IsNullOrEmpty(row.Moisture) || !string.IsNullOrEmpty(row.Light);
            }

            return WildSpeciesNiche.TryGet(species, out _);
        }

        static string GetDominanceFromRow(SpeciesEcologyCsvRow row, PlantRequirements req)
        {
            float spreadRate = row.SpreadRate;
            float holdStrength = row.HoldStrength > 0f ? row.HoldStrength : ResolveHoldStrength(row.Species, req);

            switch (row.Taxon)
            {
                case "tallgrass":
                    return "ecosystemflora:dominance-matrix";
                case "grass_colonizer":
                    return "ecosystemflora:dominance-grass-colonizer";
                case "shore_sedge":
                    return "ecosystemflora:dominance-climax";
                case "tree":
                    if (string.Equals(row.TreeSeralRole, TreeSeralRole.Pioneer.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return "ecosystemflora:dominance-colonizer";
                    }

                    if (string.Equals(row.TreeSeralRole, TreeSeralRole.Climax.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return "ecosystemflora:dominance-climax";
                    }

                    break;
            }

            if (holdStrength < 0.8f && spreadRate >= 1.5f) return "ecosystemflora:dominance-colonizer";
            if (holdStrength > 1.1f) return "ecosystemflora:dominance-climax";
            return "ecosystemflora:dominance-stable";
        }

        static string GetDominanceLabelLangKeyLegacy(string species, PlantRequirements req)
        {
            float spreadRate = ResolveSpreadRateLegacy(species);
            if (req != null && req.SpreadRate > 0f) spreadRate = req.SpreadRate;

            if (WildTreeEcology.TryGet(species, out WildTreeEcology.Profile tree))
            {
                switch (tree.SeralRole)
                {
                    case TreeSeralRole.Pioneer:
                        return "ecosystemflora:dominance-colonizer";
                    case TreeSeralRole.Climax:
                        return "ecosystemflora:dominance-climax";
                }
            }

            if (species == "tallgrass") return "ecosystemflora:dominance-matrix";
            if (WildGrassColonizerEcology.IsSpecies(species)) return "ecosystemflora:dominance-grass-colonizer";
            if (WildShoreSedgeEcology.IsSpecies(species)) return "ecosystemflora:dominance-climax";

            if (WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile mod))
            {
                if (mod.HoldStrength < 0.8f && spreadRate >= 1.5f) return "ecosystemflora:dominance-colonizer";
                if (mod.HoldStrength > 1.1f) return "ecosystemflora:dominance-climax";
            }

            return "ecosystemflora:dominance-stable";
        }

        static float ResolveSpreadRateLegacy(string species)
        {
            if (WildFlowerClimate.TryGet(species, out WildFlowerClimate.EcologyEntry flower)) return flower.SpreadRate;
            if (WildFernEcology.TryGet(species, out WildFernEcology.EcologyEntry fern)) return fern.SpreadRate;
            if (WildBerryEcology.TryGet(species, out WildBerryEcology.Profile berry)) return berry.SpreadRate;
            if (WildTreeEcology.TryGet(species, out WildTreeEcology.Profile tree)) return tree.SpreadRate;
            if (WildAquaticEcology.TryGet(species, out WildAquaticEcology.Profile aquatic)) return aquatic.SpreadRate;
            if (WildTallgrassEcology.TryGet(species, out WildTallgrassEcology.EcologyEntry grass)) return grass.SpreadRate;
            if (WildGrassColonizerEcology.TryGet(species, out WildGrassColonizerEcology.EcologyEntry colonizer)) return colonizer.SpreadRate;
            if (WildShoreSedgeEcology.TryGet(species, out WildShoreSedgeEcology.EcologyEntry shoreSedge)) return shoreSedge.SpreadRate;
            if (WildDesertEcology.TryGet(species, out WildDesertEcology.EcologyEntry desert)) return desert.SpreadRate;
            return 1f;
        }
    }
}

#pragma warning restore CS0618
