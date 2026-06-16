using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-species context affinity, hold strength, symbiosis (unified cell competition).</summary>
    internal static class WildSpeciesModifiers
    {
        public readonly struct Profile
        {
            public readonly FloraContextAffinity ContextAffinity;
            public readonly float ContextBonus;
            public readonly float ForestInteriorPenalty;
            public readonly float HoldStrength;

            public Profile(
                FloraContextAffinity affinity,
                float contextBonus = 1f,
                float forestInteriorPenalty = 0.35f,
                float holdStrength = 1f)
            {
                ContextAffinity = affinity;
                ContextBonus = contextBonus;
                ForestInteriorPenalty = forestInteriorPenalty;
                HoldStrength = holdStrength;
            }
        }

        static readonly Profile DefaultOpen = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 1f);
        static readonly Profile DefaultEdge = new Profile(FloraContextAffinity.Edge, 1.2f, 0.45f, 1.1f);
        static readonly Profile DefaultForest = new Profile(FloraContextAffinity.Forest, 1.5f, 0.6f, 1.2f);

        static readonly Dictionary<string, Profile> BySpecies = Build();

        static Dictionary<string, Profile> Build()
        {
            return new Dictionary<string, Profile>
            {
                // Colonizers — high SpreadRate, low HoldStrength (succession gives way to perennials)
                ["horsetail"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.58f),
                ["heather"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.62f),
                ["westerngorse"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.62f),
                ["mugwort"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.62f),
                ["cowparsley"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.68f),
                ["catmint"] = new Profile(FloraContextAffinity.Open, 1.1f, 0.35f, 0.72f),
                ["lupine"] = new Profile(FloraContextAffinity.Open, 1.15f, 0.35f, 0.60f),

                // Meadow perennials — moderate hold
                ["cornflower"] = Open(1.2f, hold: 1.0f),
                ["wilddaisy"] = Open(1.1f, hold: 1.0f),
                ["forgetmenot"] = Open(1.05f, hold: 1.05f),
                ["woad"] = Open(1.15f, hold: 0.95f),

                // Slow / localized — strong hold once established
                ["daffodil"] = Open(1f, hold: 1.2f),
                ["goldenpoppy"] = Open(1f, 0.45f, 1.22f),
                ["croton"] = Edge(1.3f, hold: 1.05f),
                ["rafflesiabrown"] = Open(1f, 0.4f, 1.2f),
                ["rafflesiared"] = Open(1f, 0.4f, 1.2f),

                // Shore sedges — wetland turf invaders
                [EcologyShoreSedgeSpecies.Brownsedge] = new Profile(FloraContextAffinity.Open, 1.1f, 0.35f, 0.80f),

                // Desert cacti — slow spread, strong hold
                [EcologyDesertSpecies.Barrelcactus] = Open(1f, 0.25f, 1.18f),
                [EcologyDesertSpecies.Silvertorchcactus] = Open(1f, 0.22f, 1.15f),
                ["edelweiss"] = Edge(1.3f, hold: 1.18f),

                // Forest understory climax
                ["bluebell"] = Forest(2f, hold: 1.28f),
                ["lilyofthevalley"] = Forest(2.5f, hold: 1.32f),
                ["ghostpipewhite"] = Forest(1.2f, 0.5f, 1.15f),
                ["ghostpipepink"] = Forest(1.2f, 0.5f, 1.15f),
                ["ghostpipered"] = Forest(1.2f, 0.5f, 1.15f),
                ["orangemallow"] = Open(1f, 0.4f, 1.12f),

                // Ferns — edge/forest, hold ground under canopy
                ["eaglefern"] = Edge(2.5f, hold: 1.18f),
                ["cinnamonfern"] = Edge(2.2f, hold: 1.12f),
                ["deerfern"] = Edge(2.2f, hold: 1.12f),
                ["hartstongue"] = Edge(1.8f, hold: 1.08f),
                ["tallfern"] = Edge(2f, hold: 1.12f),

                // Grass matrix — weak hold so flowers and colonizers can replace turf
                ["tallgrass"] = new Profile(FloraContextAffinity.Open, 1.1f, 0.4f, 0.72f),

                // Grass colonizers — faster spread than matrix, stronger hold once established
                [EcologyGrassColonizerSpecies.Redtopgrass] = new Profile(FloraContextAffinity.Open, 1.1f, 0.35f, 0.82f),

                // Shore / aquatic
                ["coopersreed"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.65f),
                ["tule"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.68f),
                ["papyrus"] = new Profile(FloraContextAffinity.Open, 1.05f, 0.35f, 0.82f),
                ["waterlily"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.88f),
                ["watercrowfoot"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.95f),

                // Berries — edge; strawberry colonizes, blueberry holds
                ["blueberry"] = Edge(2f, hold: 1.22f),
                ["blackcurrant"] = Edge(1.6f, hold: 1.12f),
                ["redcurrant"] = Edge(1.5f, hold: 1.08f),
                ["whitecurrant"] = Edge(1.5f, hold: 1.08f),
                ["cranberry"] = Edge(1.4f, hold: 1.08f),
                ["strawberry"] = Open(1.2f, hold: 0.78f),
                ["beautyberry"] = Edge(1.5f, hold: 1.1f),
                ["cloudberry"] = Open(1f, 0.4f, 0.92f),
                ["blackberry"] = Edge(1.7f, hold: 1.12f),
                ["raspberry"] = Edge(1.6f, hold: 1.08f),
            };
        }

        static Profile Open(float bonus, float interiorPenalty = 0.35f, float hold = 1f)
        {
            return new Profile(FloraContextAffinity.Open, bonus, interiorPenalty, hold);
        }

        static Profile Edge(float bonus, float hold = 1.1f)
        {
            return new Profile(FloraContextAffinity.Edge, bonus, 0.45f, hold);
        }

        static Profile Forest(float bonus, float interiorPenalty = 0.55f, float hold = 1.2f)
        {
            return new Profile(FloraContextAffinity.Forest, bonus, interiorPenalty, hold);
        }

        public static bool TryGet(string species, out Profile profile)
        {
            profile = DefaultOpen;
            if (string.IsNullOrEmpty(species)) return false;
            if (BySpecies.TryGetValue(species, out profile)) return true;
            if (WildFernEcology.TryGet(species, out _)) { profile = DefaultEdge; return true; }
            if (WildFerntreeEcology.IsSpecies(species)) { profile = DefaultForest; return true; }
            if (WildTreeEcology.TryGet(species, out _)) { profile = DefaultForest; return true; }
            return true;
        }

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || string.IsNullOrEmpty(req.Species)) return;

            Profile profile;
            if (!BySpecies.TryGetValue(req.Species, out profile))
            {
                if (WildFernEcology.TryGet(req.Species, out _))
                {
                    profile = DefaultEdge;
                }
                else if (WildTreeEcology.TryGet(req.Species, out _))
                {
                    profile = DefaultForest;
                }
                else if (WildFerntreeEcology.IsSpecies(req.Species))
                {
                    profile = DefaultForest;
                }
                else
                {
                    profile = DefaultOpen;
                }
            }

            req.ContextAffinity = profile.ContextAffinity;
            req.ContextBonus = profile.ContextBonus;
            req.ForestInteriorPenalty = profile.ForestInteriorPenalty;
            req.HoldStrength = profile.HoldStrength;
        }
    }
}
