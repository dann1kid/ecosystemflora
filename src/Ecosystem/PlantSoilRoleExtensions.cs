namespace WildFarming.Ecosystem
{
    internal static class PlantSoilRoleExtensions
    {
        public static bool IsMeadowRole(this PlantSoilRole role)
        {
            return role == PlantSoilRole.MeadowColonizer
                || role == PlantSoilRole.MeadowPerennial
                || role == PlantSoilRole.GrassMatrix
                || role == PlantSoilRole.NitrogenFixer;
        }

        public static bool IsForestRole(this PlantSoilRole role)
        {
            return role == PlantSoilRole.ForestUnderstory
                || role == PlantSoilRole.ForestEdge;
        }

        /// <summary>Perennial, tallgrass matrix, lupine — humus on natural death can reclaim forest floor to soil.</summary>
        public static bool ProducesHumus(this PlantSoilRole role)
        {
            return role == PlantSoilRole.MeadowColonizer
                || role == PlantSoilRole.MeadowPerennial
                || role == PlantSoilRole.GrassMatrix
                || role == PlantSoilRole.NitrogenFixer;
        }
    }
}
