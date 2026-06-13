using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Meadow mycelium vs meadow flora — shared columns and spread zones.</summary>
    internal static class MyceliumCoexistence
    {
        public static bool TryGetAnchorNiche(IBlockAccessor acc, BlockPos groundPos, out MyceliumNiche niche)
        {
            niche = MyceliumNiche.ForestAnyTree;
            if (acc == null || groundPos == null) return false;
            if (!WildSoilGroundRules.HasActiveMycelium(acc, groundPos)) return false;

            BlockEntity be = acc.GetBlockEntity(groundPos);
            if (!MyceliumAnchorReader.TryReadMushroomCode(be, out AssetLocation code)) return false;

            niche = MyceliumEcology.ClassifyNiche(code, acc.GetBlock(groundPos));
            return true;
        }

        public static bool IsForestMyceliumNiche(MyceliumNiche niche)
        {
            return niche == MyceliumNiche.ForestAnyTree
                || niche == MyceliumNiche.ForestDeciduous
                || niche == MyceliumNiche.ForestConifer
                || niche == MyceliumNiche.TrunkPolypore;
        }

        public static bool IsMeadowPlantBlock(Block block)
        {
            if (block?.Code == null) return false;
            if (block.Id == 0 && string.IsNullOrEmpty(block.Code.Path)) return false;

            string species = PlantCodeHelper.ResolveEcologySpecies(block);
            if (string.IsNullOrEmpty(species)) return false;
            if (!WildSpeciesSoilSuccession.TryGetRole(species, out PlantSoilRole role)) return false;

            return role.IsMeadowRole();
        }

        public static bool IsMeadowTerrestrialPlant(PlantRequirements req)
        {
            if (req == null || req.Habitat != EcologyHabitat.Terrestrial) return false;
            if (string.IsNullOrEmpty(req.Species)) return false;
            if (!WildSpeciesSoilSuccession.TryGetRole(req.Species, out PlantSoilRole role)) return false;

            return role.IsMeadowRole();
        }

        /// <summary>Meadow flowers/grass may spread onto ground with meadow mycelium BE underfoot.</summary>
        public static bool AllowsMeadowFloraOverMycelium(
            IBlockAccessor acc,
            BlockPos groundPos,
            PlantRequirements req)
        {
            if (!IsMeadowTerrestrialPlant(req)) return false;
            if (!TryGetAnchorNiche(acc, groundPos, out MyceliumNiche niche)) return false;

            return niche == MyceliumNiche.MeadowOpen;
        }

        public static bool AllowsSpaceForMeadowMycelium(IBlockAccessor acc, BlockPos groundPos)
        {
            if (acc == null || groundPos == null) return false;

            Block space = acc.GetBlock(groundPos.UpCopy());
            if (PlantVacancyRules.IsVacantPlantSpace(space)) return true;

            return IsMeadowPlantBlock(space);
        }
    }
}
