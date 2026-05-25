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
            in CellBlockSnapshot snap,
            out bool isEmpty)
        {
            isEmpty = snap.Space == null || snap.Space.Id == 0;

            if (acc == null || plantPos == null || requirements == null)
            {
                return false;
            }

            switch (requirements.Habitat)
            {
                case EcologyHabitat.Terrestrial:
                    return PassesTerrestrialPhysical(in snap, requirements, isEmpty);

                case EcologyHabitat.TerrestrialTree:
                case EcologyHabitat.WaterSurface:
                case EcologyHabitat.ReedNearWater:
                case EcologyHabitat.UnderwaterColumn:
                    return true;
            }

            return false;
        }

        static bool PassesTerrestrialPhysical(
            in CellBlockSnapshot snap,
            PlantRequirements requirements,
            bool isEmpty)
        {
            if (!isEmpty && !PlantCodeHelper.IsEcologySpreadParent(snap.Space))
            {
                return false;
            }

            if (snap.TouchesFluid)
            {
                return false;
            }

            if (!snap.Ground.SideSolid[BlockFacing.UP.Index])
            {
                return false;
            }

            if (WildSoilGroundRules.IsFarmland(snap.Ground))
            {
                return false;
            }

            if (isEmpty && snap.Space.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable)
            {
                return false;
            }

            SoilKind groundKinds = SoilClassification.Classify(snap.Ground);
            if (!SoilClassification.MeetsSoilRequirements(
                requirements, groundKinds, (int)snap.Ground.Fertility))
            {
                return false;
            }

            return true;
        }
    }
}
