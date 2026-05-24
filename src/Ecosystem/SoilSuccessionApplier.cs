using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class SoilSuccessionApplier
    {
        public static void Apply(
            ICoreAPI api,
            BlockPos plantPos,
            string species,
            SoilSuccessionEvent evt)
        {
            if (api == null || plantPos == null || string.IsNullOrEmpty(species)) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.UseSoilSuccession) return;

            PlantRequirements req = PlantRequirements.FromBlock(api.World.BlockAccessor.GetBlock(plantPos));
            if (req.Habitat == EcologyHabitat.ReedNearWater
                || req.Habitat == EcologyHabitat.WaterSurface
                || req.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                return;
            }

            if (!WildSpeciesSoilSuccession.TryGetImpact(species, evt, out SoilImpact impact)) return;
            if (!WildSpeciesSoilSuccession.TryGetRole(species, out PlantSoilRole role)) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos groundPos = plantPos.DownCopy();
            Block ground = acc.GetBlock(groundPos);
            if (!WildSoilBlockMapper.IsSuccessionTarget(ground)) return;

            WildSoilStore store = EcosystemSystem.Instance?.WildSoil;
            if (store == null) return;

            store.RecordAgroEvent(groundPos, role, evt);

            WildSoilComposition composition = store.GetOrCreate(api, groundPos, ground);
            composition.ApplyImpact(impact, cfg.SoilSuccessionStrength);

            if (WildSoilBlockMapper.TryPickGroundBlock(api, ground, composition, out Block newGround))
            {
                acc.SetBlock(newGround.BlockId, groundPos);
                acc.MarkBlockDirty(groundPos);
                ground = newGround;
            }

            store.Set(groundPos, composition);
            EcosystemSystem.Instance?.InvalidateEnvironmentAround(plantPos);
        }
    }
}
