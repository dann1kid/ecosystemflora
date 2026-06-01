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
            isEmpty = PlantVacancyRules.IsVacantPlantSpace(snap.Space);

            if (acc == null || plantPos == null || requirements == null)
            {
                return false;
            }

            switch (requirements.Habitat)
            {
                case EcologyHabitat.Terrestrial:
                    return PassesTerrestrialPhysical(acc, plantPos, in snap, requirements, isEmpty);

                case EcologyHabitat.TerrestrialTree:
                    return PassesTerrestrialPhysical(acc, plantPos, in snap, requirements, isEmpty);

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
            in CellBlockSnapshot snap,
            PlantRequirements requirements,
            bool isEmpty)
        {
            if (!isEmpty && !PlantCodeHelper.IsEcologySpreadParent(snap.Space))
            {
                return false;
            }

            if (PlantVacancyRules.TouchesSpreadBlockingFluid(in snap))
            {
                return false;
            }

            if (!PlantVacancyRules.IsSupportingGround(snap.Ground))
            {
                return false;
            }

            BlockPos groundPos = plantPos.DownCopy();
            if (WildSoilGroundRules.HasActiveMycelium(acc, groundPos))
            {
                return false;
            }

            SoilKind groundKinds = SoilClassification.Classify(snap.Ground);
            int fertility = WildSoilGroundRules.IsFarmland(snap.Ground)
                ? 150
                : (int)snap.Ground.Fertility;
            if (!SoilClassification.MeetsSoilRequirements(
                requirements, groundKinds, fertility, skipMaxFertility: true))
            {
                return false;
            }

            return true;
        }
    }
}
