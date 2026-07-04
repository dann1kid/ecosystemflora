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
                case EcologyHabitat.Ferntree:
                    return PassesTerrestrialPhysical(acc, plantPos, in snap, requirements, isEmpty);

                case EcologyHabitat.WaterSurface:
                case EcologyHabitat.ReedNearWater:
                    return true;

                case EcologyHabitat.UnderwaterColumn:
                    return PassesCrowfootPhysical(acc, plantPos, requirements);
            }

            return false;
        }

        static bool PassesCrowfootPhysical(IBlockAccessor acc, BlockPos plantPos, PlantRequirements requirements)
        {
            return WaterColumnHelper.IsValidCrowfootSpreadBase(acc, plantPos, requirements);
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

            if (!isEmpty && PlantCodeHelper.IsArborealHostBlock(snap.Space))
            {
                return false;
            }

            if (PlantVacancyRules.TouchesSpreadBlockingFluid(in snap))
            {
                return false;
            }

            if (!PlantVacancyRules.IsMeadowFooting(snap.Ground))
            {
                return false;
            }

            BlockPos groundPos = plantPos.DownCopy();
            if (WildSoilGroundRules.HasActiveMycelium(acc, groundPos)
                && !MyceliumCoexistence.AllowsMeadowFloraOverMycelium(acc, groundPos, requirements))
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

        /// <summary>Final gate before SetBlock (sync spread path and commit queue).</summary>
        public static bool PassesSpreadTargetGate(
            IBlockAccessor acc,
            BlockPos targetPos,
            PlantRequirements requirements,
            bool displacing,
            out bool isEmpty)
        {
            isEmpty = false;
            if (acc == null || targetPos == null || requirements == null) return false;

            CellBlockSnapshot snap = CellBlockSnapshot.Sample(acc, targetPos);
            if (!PassesPhysicalGate(acc, targetPos, requirements, in snap, out isEmpty))
            {
                return false;
            }

            if (requirements.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                if (displacing) return false;
                return CrowfootSpreadGuard.IsPlantableWaterCell(acc, targetPos)
                    && WaterColumnHelper.IsValidCrowfootSpreadBase(acc, targetPos, requirements);
            }

            if (displacing)
            {
                if (!PlantCodeHelper.IsEcologySpreadParent(snap.Space)
                    || PlantCodeHelper.IsArborealHostBlock(snap.Space))
                {
                    return false;
                }
            }
            else if (!isEmpty && !SpreadVacancy.CanOccupy(acc, targetPos, requirements, snap.Space, isEmpty))
            {
                return false;
            }

            return true;
        }
    }
}
