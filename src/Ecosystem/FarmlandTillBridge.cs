using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Seeds farmland N/P/K from plants above ground + soil tier at till (block-only, no RAM).</summary>
    internal static class FarmlandTillBridge
    {
        static BlockPos lastPos;
        static long lastApplyMs;

        public static void TryApplyAfterTill(
            ICoreAPI api,
            BlockPos farmlandPos,
            PlantSoilRole sampledRole,
            SoilFertilityTier sampledTier)
        {
            if (api == null || farmlandPos == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EcosystemEnabled || !cfg.UseFarmlandNutrientBridge) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            Block ground = acc.GetBlock(farmlandPos);
            if (!WildSoilGroundRules.IsFarmland(ground)) return;

            long now = api.World.ElapsedMilliseconds;
            if (lastPos != null && lastPos.Equals(farmlandPos) && now - lastApplyMs < 80)
            {
                return;
            }

            IFarmlandBlockEntity farmland = acc.GetBlockEntity(farmlandPos) as IFarmlandBlockEntity;
            if (farmland?.Nutrients == null || farmland.Nutrients.Length < 3) return;

            float strength = cfg.FarmlandNutrientBridgeStrength;
            ApplyRoleBonus(sampledRole, farmland.Nutrients, strength);
            ApplyTierBonus(sampledTier, farmland.Nutrients, strength);

            if (acc.GetBlockEntity(farmlandPos) is BlockEntity be)
            {
                be.MarkDirty(true);
            }

            lastPos = farmlandPos.Copy();
            lastApplyMs = now;
        }

        static void ApplyRoleBonus(PlantSoilRole role, float[] nutrients, float strength)
        {
            float n = 0f;
            float p = 0f;
            float k = 0f;

            switch (role)
            {
                case PlantSoilRole.MeadowColonizer:
                    n = 5f; p = 2f; k = 5f;
                    break;
                case PlantSoilRole.MeadowPerennial:
                    n = 12f; p = 8f; k = 6f;
                    break;
                case PlantSoilRole.GrassMatrix:
                    n = 8f; p = 4f; k = 10f;
                    break;
                case PlantSoilRole.ForestUnderstory:
                case PlantSoilRole.ForestEdge:
                    n = 6f; p = 10f; k = 4f;
                    break;
                case PlantSoilRole.WetlandHerb:
                    n = 10f; p = 6f; k = 2f;
                    break;
                case PlantSoilRole.NitrogenFixer:
                    n = 25f; p = 5f; k = 5f;
                    break;
            }

            AddNutrient(nutrients, 0, n * strength);
            AddNutrient(nutrients, 1, p * strength);
            AddNutrient(nutrients, 2, k * strength);
        }

        static void ApplyTierBonus(SoilFertilityTier tier, float[] nutrients, float strength)
        {
            float nBonus = 0f;
            switch (tier)
            {
                case SoilFertilityTier.Compost:
                    nBonus = 5f;
                    break;
                case SoilFertilityTier.High:
                    nBonus = 8f;
                    break;
            }

            if (nBonus > 0f)
            {
                AddNutrient(nutrients, 0, nBonus * strength);
            }
        }

        static void AddNutrient(float[] nutrients, int index, float delta)
        {
            nutrients[index] = Math.Min(100f, nutrients[index] + delta);
        }
    }
}
