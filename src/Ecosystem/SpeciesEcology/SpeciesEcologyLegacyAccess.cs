using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>
    /// Fallback reads from export-only C# tables when <see cref="SpeciesEcologyRegistry"/> is not loaded (unit tests).
    /// All obsolete-table references stay in this file.
    /// </summary>
    internal static class SpeciesEcologyLegacyAccess
    {
#pragma warning disable CS0618
        public static void LogMissingContractSpecies(ICoreAPI api)
        {
            if (api == null) return;
            WildFlowerClimate.LogMissingSpecies(api);
            WildTreeEcology.LogMissingWoods(api);
            WildBerryEcology.LogMissingTypes(api);
            WildFernEcology.LogMissingSpecies(api);
            WildTallgrassEcology.LogMissingSpecies(api);
            WildGrassColonizerEcology.LogMissingSpecies(api);
            WildShoreSedgeEcology.LogMissingSpecies(api);
            WildDesertEcology.LogMissingSpecies(api);
        }

        public static bool TryGetAquaticSeedDispersal(string species, out float chance, out int radius)
        {
            chance = 0f;
            radius = 0;
            if (!WildAquaticEcology.TryGet(species, out WildAquaticEcology.Profile aquatic)) return false;
            chance = aquatic.SeedDispersalChance;
            radius = aquatic.SeedDispersalRadius;
            return chance > 0f || radius > 0;
        }

        public static void ApplyBerryColonySpreadLegacy(PlantRequirements req)
        {
            if (req == null) return;
            if (!WildBerryEcology.TryGet(req.Species, out WildBerryEcology.Profile profile)) return;

            if (!EcosystemConfig.Loaded.EnableBerryColonySpread)
            {
                if (profile.SpreadMode == SpreadMode.BerryColonyMat)
                {
                    req.SpreadMode = SpreadMode.Independent;
                }

                if (profile.IndependentSpreadRadius > 0)
                {
                    req.SpreadRadius = profile.IndependentSpreadRadius;
                }

                return;
            }

            if (profile.SpreadMode == SpreadMode.BerryColonyMat)
            {
                req.SpreadMode = SpreadMode.BerryColonyMat;
                req.SpreadRadius = profile.MatSpreadRadius > 0 ? profile.MatSpreadRadius : 1;
                if (profile.SeedDispersalChance > 0f) req.SeedDispersalChance = profile.SeedDispersalChance;
                if (profile.SeedDispersalRadius > 0) req.SeedDispersalRadius = profile.SeedDispersalRadius;
                return;
            }

            if (profile.IndependentSpreadRadius > 0)
            {
                req.SpreadRadius = profile.IndependentSpreadRadius;
            }
        }

        public static bool TryGetBerrySeedDispersal(string species, out float chance, out int radius)
        {
            chance = 0f;
            radius = 0;
            if (!WildBerryEcology.TryGet(species, out WildBerryEcology.Profile profile)) return false;
            chance = profile.SeedDispersalChance;
            radius = profile.SeedDispersalRadius;
            return chance > 0f || radius > 0;
        }

        public static bool TryGetBerryMatConnectivity(string species, out MatConnectivity connectivity)
        {
            connectivity = default;
            if (!WildBerryEcology.TryGet(species, out WildBerryEcology.Profile profile)) return false;
            connectivity = profile.MatConnectivity;
            return true;
        }

        public static bool TryGetBerrySpreadRate(string species, out float spreadRate)
        {
            spreadRate = 0f;
            if (!WildBerryEcology.TryGet(species, out WildBerryEcology.Profile profile)) return false;
            spreadRate = profile.SpreadRate;
            return true;
        }

        public static void ApplyShoreSedgeMatSpreadLegacy(PlantRequirements req)
        {
            if (req == null) return;
            if (!WildShoreSedgeEcology.TryGet(req.Species, out WildShoreSedgeEcology.EcologyEntry entry)) return;

            req.SpreadMode = SpreadMode.ShoreSedgeMat;
            req.SpreadRadius = entry.MatSpreadRadius > 0 ? entry.MatSpreadRadius : 1;
            if (entry.SeedDispersalChance > 0f) req.SeedDispersalChance = entry.SeedDispersalChance;
            if (entry.SeedDispersalRadius > 0) req.SeedDispersalRadius = entry.SeedDispersalRadius;
        }

        public static bool TryGetShoreSedgeSeedDispersal(string species, out float chance, out int radius)
        {
            chance = 0f;
            radius = 0;
            if (!WildShoreSedgeEcology.TryGet(species, out WildShoreSedgeEcology.EcologyEntry entry)) return false;
            chance = entry.SeedDispersalChance;
            radius = entry.SeedDispersalRadius;
            return chance > 0f || radius > 0;
        }

        public static bool TryGetTreeSpreadRadius(string species, out int spreadRadius)
        {
            spreadRadius = 0;
            if (!WildTreeEcology.TryGet(species, out WildTreeEcology.Profile tree)) return false;
            spreadRadius = tree.SpreadRadius;
            return spreadRadius > 0;
        }

        public static int ResolveFerntreeSpreadRadius() => WildFerntreeEcology.Resolve().SpreadRadius;

        public static float TreeSeralSpreadMultiplier(string wood, float localForestCover) =>
            WildTreeEcology.SeralSpreadMultiplier(wood, localForestCover);

        public static bool TryGetTreeModifierProfile(string species, out WildSpeciesModifiers.Profile profile) =>
            WildTreeEcology.TryGetModifierProfile(species, out profile);

        public static bool TryGetTreeProfile(string species, out WildTreeEcology.Profile profile) =>
            WildTreeEcology.TryGet(species, out profile);

        public static WildFerntreeEcology.Profile ResolveFerntreeProfile() => WildFerntreeEcology.Resolve();

        public static bool TryGetFlowerClimate(string species, out WildFlowerClimate.EcologyEntry entry) =>
            WildFlowerClimate.TryGet(species, out entry);

        public static bool TryGetBerrySoilProfile(string species, out WildPlantSoil.Profile soil)
        {
            soil = default;
            if (!WildBerryEcology.TryGet(species, out WildBerryEcology.Profile berry)) return false;
            soil = berry.Soil;
            return true;
        }
#pragma warning restore CS0618
    }
}
