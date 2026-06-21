using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class SpreadSolvePreflight
    {
        public static bool PassesTerrestrial(
            in SpreadSolveCell cell,
            PlantRequirements requirements,
            IList<Block> blocks,
            out bool isEmpty)
        {
            isEmpty = cell.IsEmpty;
            if (requirements == null) return false;

            if (!isEmpty)
            {
                Block space = ResolveBlock(blocks, cell.SpaceBlockId);
                if (!PlantCodeHelper.IsEcologySpreadParent(space)) return false;
            }

            if (cell.TouchesFluid) return false;

            if (!cell.GroundSideSolid) return false;

            if (!SoilClassification.MeetsSoilRequirements(
                    requirements, cell.GroundSoilKinds, cell.GroundFertility, skipMaxFertility: true))
            {
                return false;
            }

            return true;
        }

        public static bool PassesCrowfoot(
            in SpreadSolveCell cell,
            PlantRequirements requirements,
            IList<Block> blocks,
            out bool isEmpty)
        {
            isEmpty = cell.IsEmpty;
            if (requirements == null || requirements.Habitat != EcologyHabitat.UnderwaterColumn) return false;
            if (!cell.MatVacancyOk) return false;
            if (!isEmpty) return false;

            int minDepth = requirements.MinWaterDepth > 0 ? requirements.MinWaterDepth : 2;
            if (cell.WaterColumnDepth < minDepth || cell.WaterColumnDepth > requirements.MaxWaterDepth)
            {
                return false;
            }

            if (!SoilClassification.MeetsSoilRequirements(
                    requirements, cell.GroundSoilKinds, cell.GroundFertility, skipMaxFertility: true))
            {
                return false;
            }

            return true;
        }

        public static bool PassesMat(
            in SpreadSolveCell cell,
            PlantRequirements requirements,
            IList<Block> blocks,
            out bool isEmpty)
        {
            isEmpty = cell.IsEmpty;
            if (requirements == null) return false;

            if (!SoilClassification.MeetsSoilRequirements(
                    requirements, cell.GroundSoilKinds, cell.GroundFertility, skipMaxFertility: true))
            {
                return false;
            }

            if (isEmpty) return true;

            Block space = ResolveBlock(blocks, cell.SpaceBlockId);
            switch (requirements.Habitat)
            {
                case EcologyHabitat.ReedNearWater:
                    return cell.MatVacancyOk && !PlantCodeHelper.IsReedBlock(space);
                case EcologyHabitat.WaterSurface:
                    return cell.MatVacancyOk;
                default:
                    return false;
            }
        }

        static Block ResolveBlock(IList<Block> blocks, int id)
        {
            if (blocks == null || id <= 0 || id >= blocks.Count) return null;
            return blocks[id];
        }
    }
}
