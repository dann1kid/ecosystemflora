namespace WildFarming.Ecosystem
{
    internal static class EcologyWake
    {
        public static int ResolveRadiusBlocks(EcosystemConfig cfg)
        {
            if (cfg == null) return 5;
            if (cfg.EcologyWakeRadiusBlocks > 0) return cfg.EcologyWakeRadiusBlocks;

            int radius = cfg.ReproduceRadius > 0 ? cfg.ReproduceRadius : 4;
            if (cfg.PlantSpacingEnabled)
            {
                int spacing = cfg.DefaultOtherSpeciesSpacing + 1;
                if (spacing > radius) radius = spacing;
            }

            if (cfg.UseFloraContext && cfg.FloraContextNeighborRadius > radius)
            {
                radius = cfg.FloraContextNeighborRadius;
            }

            return radius + 1;
        }
    }
}
