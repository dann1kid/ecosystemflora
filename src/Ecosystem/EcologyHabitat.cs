namespace WildFarming.Ecosystem
{
    public enum EcologyHabitat
    {
        Terrestrial = 0,
        /// <summary>Reeds on muddy or rock gravel lake bed, may stand in shallow water.</summary>
        ReedNearWater = 1,
        /// <summary>Water lily on open water surface.</summary>
        WaterSurface = 2,
        /// <summary>Water crowfoot: column of section blocks, top or tip on surface.</summary>
        UnderwaterColumn = 3,
        /// <summary>Mature log-grown trunk spreads vanilla saplings; growth is vanilla treegen.</summary>
        TerrestrialTree = 4,
        /// <summary>Vanilla <c>BlockEntityMycelium</c> anchor on soil or log (network spread — later phase).</summary>
        MyceliumAnchor = 5,

        /// <summary>Vanilla <c>ferntree-normal-trunk</c> — tropical arborescent fern column.</summary>
        Ferntree = 6,

        /// <summary>Vanilla <c>wildvine-end-*</c> tips on vertical surfaces.</summary>
        WildVine = 7,
    }
}
