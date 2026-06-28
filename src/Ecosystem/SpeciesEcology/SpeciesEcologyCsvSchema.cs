namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesEcologyCsvSchema
    {
        public const string SpeciesColumn = "species";

        public static readonly string[] Columns =
        {
            "species", "taxon",
            "min_temp", "max_temp", "min_rain", "max_rain", "min_forest", "max_forest",
            "spread_rate",
            "spread_mode", "mat_connectivity",
            "seed_dispersal_chance", "seed_dispersal_radius",
            "mat_spread_radius", "independent_spread_radius", "spread_radius",
            "same_species_spacing", "other_species_spacing", "spacing_from_species", "min_sunlight",
            "habitat",
            "water_max_depth", "water_min_depth", "water_vertical_blocks", "water_exact_depth",
            "soil_kinds", "soil_min_fertility", "soil_max_fertility",
            "context_affinity", "context_bonus", "forest_interior_penalty", "hold_strength",
            "moisture", "light", "niche_bonus",
            "season_explicit",
            "flower_maturation_h", "flower_cooldown_h",
            "fern_maturation_h", "fern_cooldown_h",
            "berry_maturation_h",
            "tree_seral_role", "soil_succession_role",
        };
    }
}
