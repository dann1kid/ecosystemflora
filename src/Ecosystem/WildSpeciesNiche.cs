using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-species moisture + light preferences (v2.2 niche).</summary>
    internal static class WildSpeciesNiche
    {
        public readonly struct Profile
        {
            public readonly MoistureLevel PreferredMoisture;
            public readonly LightLevel PreferredLight;
            public readonly float NicheBonus;

            public Profile(MoistureLevel moisture, LightLevel light, float nicheBonus = 1.25f)
            {
                PreferredMoisture = moisture;
                PreferredLight = light;
                NicheBonus = nicheBonus;
            }
        }

        static readonly Dictionary<string, Profile> BySpecies = Build();

        static Dictionary<string, Profile> Build()
        {
            return new Dictionary<string, Profile>
            {
                // Open meadow colonizers
                ["wilddaisy"] = new Profile(MoistureLevel.Mesic, LightLevel.Open, 1.2f),
                ["cornflower"] = new Profile(MoistureLevel.Mesic, LightLevel.Open, 1.15f),
                ["forgetmenot"] = new Profile(MoistureLevel.Mesic, LightLevel.Open, 1.1f),
                ["goldenpoppy"] = new Profile(MoistureLevel.Dry, LightLevel.Open, 1.2f),
                ["heather"] = new Profile(MoistureLevel.Dry, LightLevel.Open, 1.25f),
                ["westerngorse"] = new Profile(MoistureLevel.Dry, LightLevel.Open, 1.2f),
                ["redtopgrass"] = new Profile(MoistureLevel.Mesic, LightLevel.Open, 1.15f),
                ["tallgrass"] = new Profile(MoistureLevel.Mesic, LightLevel.Open, 1.1f),

                // Wet shade understory — horsetail without symbiosis-only gate
                ["horsetail"] = new Profile(MoistureLevel.Wet, LightLevel.Shade, 1.35f),

                // Forest floor
                ["lilyofthevalley"] = new Profile(MoistureLevel.Wet, LightLevel.DeepShade, 1.4f),
                ["bluebell"] = new Profile(MoistureLevel.Mesic, LightLevel.Shade, 1.3f),
                ["ghostpipewhite"] = new Profile(MoistureLevel.Wet, LightLevel.DeepShade, 1.2f),
                ["ghostpipepink"] = new Profile(MoistureLevel.Wet, LightLevel.DeepShade, 1.2f),
                ["ghostpipered"] = new Profile(MoistureLevel.Wet, LightLevel.DeepShade, 1.2f),

                // Edge / partial light
                ["catmint"] = new Profile(MoistureLevel.Mesic, LightLevel.Partial, 1.2f),
                ["edelweiss"] = new Profile(MoistureLevel.Mesic, LightLevel.Partial, 1.25f),
                ["cowparsley"] = new Profile(MoistureLevel.Mesic, LightLevel.Partial, 1.1f),

                // Ferns
                ["eaglefern"] = new Profile(MoistureLevel.Wet, LightLevel.Shade, 1.35f),
                ["cinnamonfern"] = new Profile(MoistureLevel.Wet, LightLevel.Shade, 1.3f),
                ["deerfern"] = new Profile(MoistureLevel.Wet, LightLevel.Shade, 1.3f),
                ["hartstongue"] = new Profile(MoistureLevel.Mesic, LightLevel.Shade, 1.2f),
                ["tallfern"] = new Profile(MoistureLevel.Mesic, LightLevel.Partial, 1.15f),
                [WildFerntreeEcology.Species] = new Profile(MoistureLevel.Wet, LightLevel.Partial, 1.2f),
            };
        }

        public static bool TryGet(string species, out Profile profile)
        {
            if (string.IsNullOrEmpty(species))
            {
                profile = default;
                return false;
            }

            return BySpecies.TryGetValue(species, out profile);
        }

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || string.IsNullOrEmpty(req.Species)) return;

            if (!TryGet(req.Species, out Profile profile))
            {
                req.HasNicheProfile = false;
                return;
            }

            req.HasNicheProfile = true;
            req.PreferredMoisture = profile.PreferredMoisture;
            req.PreferredLight = profile.PreferredLight;
            req.NicheBonus = profile.NicheBonus;
        }
    }
}
