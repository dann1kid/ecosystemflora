using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Empty farmland near wild ecosystem plants slowly regains nutrients (fallow process).
    /// Called periodically from the stress tick — iterates nearby farmland for each checked plant.
    /// </summary>
    internal static class FallowRestoration
    {
        const int SearchRadius = 2;
        const float MaxNutrientPerCheck = 2.5f;
        const float BaseNPerCheck = 1.5f;
        const float BasePPerCheck = 1.0f;
        const float BaseKPerCheck = 1.2f;

        static readonly BlockPos scratchPos = new BlockPos(0);

        public static void TryRestoreNear(ICoreAPI api, BlockPos plantPos)
        {
            if (api == null || plantPos == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableFallowRestoration) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            float strength = cfg.FallowRestorationStrength;

            string species = null;
            Block plantBlock = acc.GetBlock(plantPos);
            if (plantBlock != null && plantBlock.Id != 0)
            {
                species = PlantCodeHelper.GetEcologySpecies(plantBlock.Code);
            }

            PlantSoilRole role = PlantSoilRole.MeadowPerennial;
            if (!string.IsNullOrEmpty(species))
            {
                WildSpeciesSoilSuccession.TryGetRole(species, out role);
            }

            for (int dx = -SearchRadius; dx <= SearchRadius; dx++)
            {
                for (int dz = -SearchRadius; dz <= SearchRadius; dz++)
                {
                    scratchPos.Set(plantPos.X + dx, plantPos.Y - 1, plantPos.Z + dz);

                    Block ground = acc.GetBlock(scratchPos);
                    if (!WildSoilGroundRules.IsFarmland(ground)) continue;

                    if (cfg.RespectLandClaims && !LandClaimGuard.AllowsEcologyChange(api, scratchPos))
                        continue;

                    BlockEntity be = acc.GetBlockEntity(scratchPos);
                    if (be is not IFarmlandBlockEntity farmland) continue;
                    if (farmland.Nutrients == null || farmland.Nutrients.Length < 3) continue;

                    if (HasCrop(acc, scratchPos)) continue;

                    ApplyFallowBonus(farmland.Nutrients, role, strength);

                    be.MarkDirty(true);
                }
            }
        }

        static bool HasCrop(IBlockAccessor acc, BlockPos farmlandPos)
        {
            scratchPos.Set(farmlandPos.X, farmlandPos.Y + 1, farmlandPos.Z);
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
                case PlantSoilRole.ForestUnderstory:
                case PlantSoilRole.ForestEdge:
                    n = 0.8f; p = 1.5f; k = 0.6f;
                    break;
                case PlantSoilRole.WetlandHerb:
                    n = 1.5f; p = 0.8f; k = 0.5f;
                    break;
            }

            float cap = MaxNutrientPerCheck * strength;
            nutrients[0] = Math.Min(100f, nutrients[0] + Math.Min(n * strength, cap));
            nutrients[1] = Math.Min(100f, nutrients[1] + Math.Min(p * strength, cap));
            nutrients[2] = Math.Min(100f, nutrients[2] + Math.Min(k * strength, cap));
        }
    }
}
