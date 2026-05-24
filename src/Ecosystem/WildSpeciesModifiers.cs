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
                // Fast spread, weak hold — lose occupied cells to climax species
                ["horsetail"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.65f),
                ["heather"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.7f),
                ["westerngorse"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.7f),
                ["redtopgrass"] = new Profile(FloraContextAffinity.Open, 1.05f, 0.35f, 0.65f),
                ["mugwort"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.68f),
                ["cowparsley"] = new Profile(FloraContextAffinity.Open, 1f, 0.35f, 0.75f),
                ["catmint"] = new Profile(FloraContextAffinity.Open, 1.1f, 0.35f, 0.8f),
                ["lupine"] = new Profile(FloraContextAffinity.Open, 1.15f, 0.35f, 0.72f),

                ["cornflower"] = Open(1.2f, hold: 1.05f),
                ["wilddaisy"] = Open(1.1f, hold: 1.05f),
                ["forgetmenot"] = Open(1.05f, hold: 1.1f),
                ["woad"] = Open(1.15f, hold: 1f),
                ["daffodil"] = Open(1f, hold: 1.15f),

                ["bluebell"] = Forest(2f, hold: 1.25f),
                ["lilyofthevalley"] = Forest(2.5f, hold: 1.3f),
                ["ghostpipewhite"] = Forest(1.2f, 0.5f, 1.2f),
                ["ghostpipepink"] = Forest(1.2f, 0.5f, 1.2f),
                ["ghostpipered"] = Forest(1.2f, 0.5f, 1.2f),

                ["orangemallow"] = Open(1f, 0.4f, 1.1f),
                ["edelweiss"] = Edge(1.3f, hold: 1.2f),
                ["goldenpoppy"] = Open(1f, 0.45f, 1.15f),

                ["eaglefern"] = Edge(2.5f, hold: 1.15f),
                ["cinnamonfern"] = Edge(2.2f, hold: 1.1f),
                ["deerfern"] = Edge(2.2f, hold: 1.1f),
                ["hartstongue"] = Edge(1.8f, hold: 1.05f),
                ["tallfern"] = Edge(2f, hold: 1.1f),

                ["tallgrass"] = new Profile(FloraContextAffinity.Open, 1.1f, 0.4f, 0.85f),

                ["blueberry"] = Edge(2f, hold: 1.2f),
                ["blackcurrant"] = Edge(1.6f, hold: 1.15f),
                ["redcurrant"] = Edge(1.5f, hold: 1.1f),
                ["whitecurrant"] = Edge(1.5f, hold: 1.1f),
                ["cranberry"] = Edge(1.4f, hold: 1.1f),
                ["strawberry"] = Open(1.2f, hold: 0.85f),
                ["beautyberry"] = Edge(1.5f, hold: 1.1f),
                ["cloudberry"] = Open(1f, 0.4f, 1f),
                ["blackberry"] = Edge(1.7f, hold: 1.15f),
                ["raspberry"] = Edge(1.6f, hold: 1.1f),
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
