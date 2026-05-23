using System;

namespace WildFarming.Ecosystem
{
    [Flags]
    public enum SoilKind
    {
        None = 0,
        /// <summary>soil-high, soil-compost (block fertility 250+).</summary>
        HighFert = 1 << 0,
        /// <summary>soil-medium (~200).</summary>
        MediumFert = 1 << 1,
        /// <summary>soil-low, soil-verylow (~100–150).</summary>
        LowFert = 1 << 2,
        /// <summary>forestfloor, peat.</summary>
        ForestFloor = 1 << 3,
        Peat = 1 << 4,
        /// <summary>sand, sandstone topsoil.</summary>
        Sand = 1 << 5,
        /// <summary>rawclay / clay soils.</summary>
        Clay = 1 << 6,
        /// <summary>muddygravel, bony, cob — wet or rocky margins.</summary>
        Gravel = 1 << 7,
        /// <summary>packed dirt, trampled earth, zero fertility.</summary>
        Barren = 1 << 8,
    }

    public static class SoilKindSets
    {
        public const SoilKind Meadow =
            SoilKind.HighFert | SoilKind.MediumFert | SoilKind.LowFert | SoilKind.ForestFloor | SoilKind.Peat;

        public const SoilKind ForestUnderstory =
            SoilKind.MediumFert | SoilKind.LowFert | SoilKind.ForestFloor | SoilKind.Peat;

        public const SoilKind Poor =
            SoilKind.LowFert | SoilKind.ForestFloor | SoilKind.Peat | SoilKind.Gravel;

        public const SoilKind AllNatural =
            Meadow | SoilKind.Sand | SoilKind.Gravel;
    }
}
