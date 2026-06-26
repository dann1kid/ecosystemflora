using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-role soil conversion on spread (establish) and death.</summary>
    internal static class WildSpeciesSoilSuccession
    {
        public readonly struct RoleProfile
        {
            public readonly SoilImpact SpreadImpact;
            public readonly SoilImpact DeathImpact;

            public RoleProfile(SoilImpact spread, SoilImpact death)
            {
                SpreadImpact = spread;
                DeathImpact = death;
            }
        }

        static readonly Dictionary<PlantSoilRole, RoleProfile> ByRole = BuildRoles();
        static readonly Dictionary<string, PlantSoilRole> SpeciesRole = BuildSpecies();

        static Dictionary<PlantSoilRole, RoleProfile> BuildRoles()
        {
            return new Dictionary<PlantSoilRole, RoleProfile>
            {
                [PlantSoilRole.MeadowColonizer] = new RoleProfile(
                    spread: new SoilImpact
                    {
                        MoistureDelta = -2f,
                        FertilityTierDelta = 0.12f,
                    },
                    death: new SoilImpact
                    {
                        MoistureDelta = -1f,
                        FertilityTierDelta = 0.18f,
                    }),

                [PlantSoilRole.MeadowPerennial] = new RoleProfile(
                    spread: new SoilImpact
                    {
                        MoistureDelta = 0f,
                        FertilityTierDelta = 0.20f,
                    },
                    death: new SoilImpact
                    {
                        MoistureDelta = -3f,
                        FertilityTierDelta = 0.15f,
                    }),

                [PlantSoilRole.GrassMatrix] = new RoleProfile(
                    spread: new SoilImpact
                    {
                        MoistureDelta = -2f,
                        FertilityTierDelta = 0.08f,
                    },
                    death: new SoilImpact
                    {
                        MoistureDelta = -4f,
                        FertilityTierDelta = 0.05f,
                    }),

                [PlantSoilRole.GrassColonizer] = new RoleProfile(
                    spread: new SoilImpact
                    {
                        MoistureDelta = -3f,
                        FertilityTierDelta = 0.10f,
                    },
                    death: new SoilImpact
                    {
                        MoistureDelta = -3f,
                        FertilityTierDelta = 0.08f,
                    }),

                [PlantSoilRole.ForestEdge] = new RoleProfile(
                    spread: new SoilImpact
                    {
                        MoistureDelta = 2f,
                        FertilityTierDelta = 0.12f,
                    },
                    death: new SoilImpact
                    {
                        MoistureDelta = 1f,
                        FertilityTierDelta = 0.12f,
                    }),

                [PlantSoilRole.ForestUnderstory] = new RoleProfile(
                    spread: new SoilImpact
                    {
                        MoistureDelta = 6f,
                        FertilityTierDelta = 0.10f,
                    },
                    death: new SoilImpact
                    {
                        MoistureDelta = 4f,
                        FertilityTierDelta = 0.22f,
                    }),

                [PlantSoilRole.WetlandHerb] = new RoleProfile(
                    spread: new SoilImpact
                    {
                        MoistureDelta = 12f,
                        FertilityTierDelta = 0.08f,
                    },
                    death: new SoilImpact
                    {
                        MoistureDelta = 6f,
                        FertilityTierDelta = 0.12f,
                    }),

                [PlantSoilRole.SoilDepleter] = new RoleProfile(
                    spread: new SoilImpact
                    {
                        MoistureDelta = -4f,
                        FertilityTierDelta = -0.06f,
                    },
                    death: new SoilImpact
                    {
                        MoistureDelta = 0f,
                        FertilityTierDelta = 0.06f,
                    }),

                [PlantSoilRole.NitrogenFixer] = new RoleProfile(
                    spread: new SoilImpact
                    {
                        MoistureDelta = 0f,
                        FertilityTierDelta = 0.18f,
                    },
                    death: new SoilImpact
                    {
                        MoistureDelta = -2f,
                        FertilityTierDelta = 0.20f,
                    }),
            };
        }

        static Dictionary<string, PlantSoilRole> BuildSpecies()
        {
            return new Dictionary<string, PlantSoilRole>
            {
                ["wilddaisy"] = PlantSoilRole.MeadowColonizer,
                ["cornflower"] = PlantSoilRole.MeadowColonizer,
                ["goldenpoppy"] = PlantSoilRole.MeadowColonizer,
                ["heather"] = PlantSoilRole.SoilDepleter,
                ["westerngorse"] = PlantSoilRole.SoilDepleter,
                ["forgetmenot"] = PlantSoilRole.MeadowPerennial,
                ["cowparsley"] = PlantSoilRole.MeadowPerennial,
                ["catmint"] = PlantSoilRole.MeadowPerennial,
                ["edelweiss"] = PlantSoilRole.SoilDepleter,
                ["mugwort"] = PlantSoilRole.MeadowPerennial,
                ["woad"] = PlantSoilRole.MeadowColonizer,
                ["orangemallow"] = PlantSoilRole.MeadowPerennial,
                ["daffodil"] = PlantSoilRole.MeadowPerennial,
                ["lupine"] = PlantSoilRole.NitrogenFixer,
                ["horsetail"] = PlantSoilRole.WetlandHerb,
                ["bluebell"] = PlantSoilRole.ForestUnderstory,
                ["lilyofthevalley"] = PlantSoilRole.ForestUnderstory,
                ["ghostpipewhite"] = PlantSoilRole.ForestUnderstory,
                ["ghostpipepink"] = PlantSoilRole.ForestUnderstory,
                ["ghostpipered"] = PlantSoilRole.ForestUnderstory,
                ["eaglefern"] = PlantSoilRole.ForestUnderstory,
                ["cinnamonfern"] = PlantSoilRole.ForestUnderstory,
                ["deerfern"] = PlantSoilRole.ForestUnderstory,
                ["hartstongue"] = PlantSoilRole.WetlandHerb,
                ["tallfern"] = PlantSoilRole.ForestEdge,
                ["tallgrass"] = PlantSoilRole.GrassMatrix,
                [EcologyGrassColonizerSpecies.Redtopgrass] = PlantSoilRole.GrassColonizer,
                [EcologyShoreSedgeSpecies.Brownsedge] = PlantSoilRole.WetlandHerb,
                [EcologyDesertSpecies.Barrelcactus] = PlantSoilRole.SoilDepleter,
                [EcologyDesertSpecies.Silvertorchcactus] = PlantSoilRole.SoilDepleter,
                ["croton"] = PlantSoilRole.ForestEdge,
                ["rafflesiabrown"] = PlantSoilRole.MeadowPerennial,
                ["rafflesiared"] = PlantSoilRole.MeadowPerennial,
            };
        }

        public static bool TryGetRole(string species, out PlantSoilRole role)
        {
            if (string.IsNullOrEmpty(species))
            {
                role = PlantSoilRole.MeadowPerennial;
                return false;
            }

            if (SpeciesRole.TryGetValue(species, out role)) return true;

            if (WildFerntreeEcology.IsSpecies(species))
            {
                role = PlantSoilRole.ForestUnderstory;
                return true;
            }

            role = PlantSoilRole.MeadowPerennial;
            return false;
        }

        static readonly SoilImpact TrampledImpact = new SoilImpact
        {
            MoistureDelta = -8f,
            FertilityTierDelta = -0.25f,
        };

        public static bool TryGetImpact(string species, SoilSuccessionEvent evt, out SoilImpact impact)
        {
            impact = default;

            if (evt == SoilSuccessionEvent.Trampled)
            {
                impact = TrampledImpact;
                return true;
            }

            if (!TryGetRole(species, out PlantSoilRole role)) return false;
            if (!ByRole.TryGetValue(role, out RoleProfile profile)) return false;

            impact = evt == SoilSuccessionEvent.Spread ? profile.SpreadImpact : profile.DeathImpact;
            return true;
        }
    }
}
