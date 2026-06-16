using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Empty farmland directly below a wild plant slowly regains nutrients (fallow process).
    /// Called during stress checks for healthy plants. Cost: 1 GetBlock per plant (early-out).
    /// </summary>
    internal static class FallowRestoration
    {
        const float MaxNutrientPerCheck = 2.5f;
        const float BaseNPerCheck = 1.5f;
        const float BasePPerCheck = 1.0f;
        const float BaseKPerCheck = 1.2f;

        static readonly BlockPos scratchPos = new BlockPos(0);

        const float SpreadDripFactor = 0.35f;

        public static void TryRestoreNear(ICoreAPI api, BlockPos plantPos)
        {
            if (api == null || plantPos == null) return;
            if (!EcosystemConfig.Loaded.EnableFallowRestoration) return;

            PlantSoilRole role = ResolvePlantRole(api.World.BlockAccessor, plantPos);
            TryApplyNutrients(api, plantPos, role, EcosystemConfig.Loaded.FallowRestorationStrength);
        }

        /// <summary>Slow N/P/K drip when a wild plant spreads onto empty farmland.</summary>
        public static void TryApplySpreadDrip(ICoreAPI api, BlockPos plantPos, PlantSoilRole role)
        {
            if (api == null || plantPos == null) return;
            if (!EcosystemConfig.Loaded.EnableFallowRestoration) return;

            float strength = EcosystemConfig.Loaded.FallowRestorationStrength * SpreadDripFactor;
            TryApplyNutrients(api, plantPos, role, strength);
        }

        static void TryApplyNutrients(ICoreAPI api, BlockPos plantPos, PlantSoilRole role, float strength)
        {
            if (strength <= 0f) return;

            IBlockAccessor acc = api.World.BlockAccessor;

            scratchPos.Set(plantPos.X, plantPos.Y - 1, plantPos.Z);
            Block ground = acc.GetBlock(scratchPos);
            if (!WildSoilGroundRules.IsFarmland(ground)) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg.RespectLandClaims && !LandClaimGuard.AllowsEcologyChange(api, scratchPos))
                return;

            BlockEntity be = acc.GetBlockEntity(scratchPos);
            if (be is not IFarmlandBlockEntity farmland) return;
            if (farmland.Nutrients == null || farmland.Nutrients.Length < 3) return;

            if (HasCrop(acc)) return;

            ApplyFallowBonus(farmland.Nutrients, role, strength);
            be.MarkDirty(true);
        }

        static PlantSoilRole ResolvePlantRole(IBlockAccessor acc, BlockPos plantPos)
        {
            Block plantBlock = acc.GetBlock(plantPos);
            if (plantBlock == null || plantBlock.Id == 0) return PlantSoilRole.MeadowPerennial;

                string species = PlantCodeHelper.ResolveEcologySpecies(plantBlock);
            if (string.IsNullOrEmpty(species)) return PlantSoilRole.MeadowPerennial;

            WildSpeciesSoilSuccession.TryGetRole(species, out PlantSoilRole role);
            return role;
        }

        static bool HasCrop(IBlockAccessor acc)
        {
            scratchPos.Up();
            Block above = acc.GetBlock(scratchPos);
            if (above == null || above.Id == 0) return false;
            return above.CropProps != null;
        }

        static void ApplyFallowBonus(float[] nutrients, PlantSoilRole role, float strength)
        {
            float n = BaseNPerCheck;
            float p = BasePPerCheck;
            float k = BaseKPerCheck;

            switch (role)
            {
                case PlantSoilRole.NitrogenFixer:
                    n = 2.5f; p = 0.8f; k = 0.8f;
                    break;
                case PlantSoilRole.MeadowColonizer:
                    n = 0.8f; p = 0.5f; k = 1.0f;
                    break;
                case PlantSoilRole.MeadowPerennial:
                    n = 1.5f; p = 1.0f; k = 1.2f;
                    break;
                case PlantSoilRole.GrassMatrix:
                    n = 1.0f; p = 0.6f; k = 1.5f;
                    break;
                case PlantSoilRole.GrassColonizer:
                    n = 0.9f; p = 0.55f; k = 1.3f;
                    break;
                case PlantSoilRole.ForestUnderstory:
                case PlantSoilRole.ForestEdge:
                    n = 0.8f; p = 1.5f; k = 0.6f;
                    break;
                case PlantSoilRole.WetlandHerb:
                    n = 1.5f; p = 0.8f; k = 0.5f;
                    break;
                case PlantSoilRole.SoilDepleter:
                    n = 0.4f; p = 0.3f; k = 0.5f;
                    break;
            }

            float cap = MaxNutrientPerCheck * strength;
            nutrients[0] = Math.Min(100f, nutrients[0] + Math.Min(n * strength, cap));
            nutrients[1] = Math.Min(100f, nutrients[1] + Math.Min(p * strength, cap));
            nutrients[2] = Math.Min(100f, nutrients[2] + Math.Min(k * strength, cap));
        }
    }
}
