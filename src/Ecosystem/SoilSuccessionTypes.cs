namespace WildFarming.Ecosystem
{
    public enum PlantSoilRole
    {
        MeadowColonizer,
        MeadowPerennial,
        ForestUnderstory,
        ForestEdge,
        WetlandHerb,
        GrassMatrix,
        /// <summary>Legume-style wild plants (e.g. lupine) — strong N bonus when soil is tilled.</summary>
        NitrogenFixer,
        /// <summary>Heath/dry colonizers that lean on poor soil (slight depletion while living).</summary>
        SoilDepleter,
    }

    public enum SoilSuccessionEvent
    {
        Spread,
        Death,
        Trampled,
    }

    public struct SoilImpact
    {
        public float MoistureDelta;
        public float FertilityTierDelta;
        public bool IsForestFloor;

        public static SoilImpact Combine(SoilImpact a, SoilImpact b)
        {
            return new SoilImpact
            {
                MoistureDelta = a.MoistureDelta + b.MoistureDelta,
                FertilityTierDelta = a.FertilityTierDelta + b.FertilityTierDelta,
                IsForestFloor = a.IsForestFloor || b.IsForestFloor,
            };
        }
    }
}
