using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Cheap block-only gates before climate/niche spread sampling.</summary>
    internal static class SpreadPreflight
    {
        public static bool PassesPhysicalGate(
            IBlockAccessor acc,
            BlockPos plantPos,
            PlantRequirements requirements,
            Block occupant,
            out bool isEmpty)
        {
            isEmpty = occupant == null || occupant.Id == 0;

            if (acc == null || plantPos == null || requirements == null)
            {
                return false;
            }

            switch (requirements.Habitat)
            {
                case EcologyHabitat.Terrestrial:
                    return PassesTerrestrialPhysical(acc, plantPos, requirements, isEmpty);

                case EcologyHabitat.TerrestrialTree:
                case EcologyHabitat.WaterSurface:
                case EcologyHabitat.ReedNearWater:
                case EcologyHabitat.UnderwaterColumn:
                    return true;
            }

            return false;
        }

        static bool PassesTerrestrialPhysical(
            IBlockAccessor acc,
            BlockPos plantPos,
            PlantRequirements requirements,
            bool isEmpty)
        {
            if (!isEmpty && !PlantCodeHelper.IsEcologySpreadParent(acc.GetBlock(plantPos)))
            {
                return false;
            }

            Block space = acc.GetBlock(plantPos);
            Block ground = acc.GetBlock(plantPos.DownCopy());

            if (BlockFluidHelper.TouchesFluid(acc, plantPos))
            {
                return false;
            }

            if (!ground.SideSolid[BlockFacing.UP.Index])
            {
                return false;
            }

            if (isEmpty && space.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable)
            {
                return false;
            }

            SoilKind groundKinds = SoilClassification.Classify(ground);
            if (!SoilClassification.MeetsSoilRequirements(
                requirements, groundKinds, (int)ground.Fertility))
            {
                return false;
            }

            return true;
        }
    }
}
