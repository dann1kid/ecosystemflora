using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using WildFarming.Ecosystem;

namespace WildFarming.Handbook
{
    public class EcologyHandbookBehavior : BlockBehavior
    {
        public EcologyHandbookBehavior(Block block) : base(block) { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            Block block = inSlot?.Itemstack?.Block;
            if (block == null) return;

            string species = PlantCodeHelper.ResolveEcologySpecies(block);
            if (string.IsNullOrEmpty(species)) return;

            PlantRequirements req = PlantRequirements.FromBlock(block);

            dsc.AppendLine();
            dsc.AppendLine("<strong>" + Lang.Get("ecosystemflora:handbook-ecology-header") + "</strong>");

            AppendSpreadRate(dsc, req.SpreadRate);
            AppendClimate(dsc, species, req, block);
            AppendContext(dsc, species);
            AppendHoldStrength(dsc, species);
            AppendDominanceHint(dsc, species, req);
            AppendNiche(dsc, species);
            AppendSeason(dsc, species);
            AppendSymbiosis(dsc, species);
        }

        static void AppendSpreadRate(StringBuilder dsc, float rate)
        {
            string label = GetSpreadLabel(rate);
            dsc.AppendLine(Lang.Get("ecosystemflora:handbook-spread", label));
        }

        static string GetSpreadLabel(float rate)
        {
            if (rate >= 2.2f) return Lang.Get("ecosystemflora:spread-veryfast");
            if (rate >= 1.5f) return Lang.Get("ecosystemflora:spread-fast");
            if (rate >= 0.9f) return Lang.Get("ecosystemflora:spread-moderate");
            if (rate >= 0.5f) return Lang.Get("ecosystemflora:spread-slow");
            return Lang.Get("ecosystemflora:spread-veryslow");
        }

        static void AppendClimate(StringBuilder dsc, string species, PlantRequirements req, Block block)
        {
            if (block != null && PlantCodeHelper.IsThirdPartyEcologyBlock(block) && req != null)
            {
                dsc.AppendLine(Lang.Get("ecosystemflora:handbook-climate",
                    req.MinTemp.ToString("0"), req.MaxTemp.ToString("0")));
                dsc.AppendLine(Lang.Get("ecosystemflora:handbook-rainfall",
                    req.MinRain.ToString("0.##"), req.MaxRain.ToString("0.##")));
                if (req.MinForest > 0.01f || req.MaxForest < 0.99f)
                {
                    dsc.AppendLine(Lang.Get("ecosystemflora:handbook-forest",
                        req.MinForest.ToString("0.##"), req.MaxForest.ToString("0.##")));
                }

                return;
            }

            if (WildFlowerClimate.TryGet(species, out WildFlowerClimate.EcologyEntry entry))
            {
                dsc.AppendLine(Lang.Get("ecosystemflora:handbook-climate",
                    entry.MinTemp.ToString("0"), entry.MaxTemp.ToString("0")));
                dsc.AppendLine(Lang.Get("ecosystemflora:handbook-rainfall",
                    entry.MinRain.ToString("0.##"), entry.MaxRain.ToString("0.##")));
                if (entry.MinForest > 0.01f || entry.MaxForest < 0.99f)
                {
                    dsc.AppendLine(Lang.Get("ecosystemflora:handbook-forest",
                        entry.MinForest.ToString("0.##"), entry.MaxForest.ToString("0.##")));
                }
            }
        }

        static void AppendContext(StringBuilder dsc, string species)
        {
            if (!WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile profile)) return;

            string label = profile.ContextAffinity switch
            {
                FloraContextAffinity.Open => Lang.Get("ecosystemflora:context-open"),
                FloraContextAffinity.Edge => Lang.Get("ecosystemflora:context-edge"),
                FloraContextAffinity.Forest => Lang.Get("ecosystemflora:context-forest"),
                _ => null
            };

            if (label != null)
            {
                dsc.AppendLine(Lang.Get("ecosystemflora:handbook-context", label));
            }
        }

        static void AppendHoldStrength(StringBuilder dsc, string species)
        {
            if (!WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile profile)) return;

            string label;
            if (profile.HoldStrength < 0.8f) label = Lang.Get("ecosystemflora:hold-weak");
            else if (profile.HoldStrength > 1.1f) label = Lang.Get("ecosystemflora:hold-strong");
            else label = Lang.Get("ecosystemflora:hold-moderate");

            dsc.AppendLine(Lang.Get("ecosystemflora:handbook-hold", label));
        }

        static void AppendDominanceHint(StringBuilder dsc, string species, PlantRequirements req)
        {
            // Lightweight UX: explain what kind of "dominant" this plant tends to be in succession.
            // This is not a territory scanner; it helps players interpret spread/competition behavior.
            float spreadRate = req != null ? req.SpreadRate : 1f;

            if (!WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile mod))
            {
                dsc.AppendLine(Lang.Get("ecosystemflora:handbook-dominance", Lang.Get("ecosystemflora:dominance-stable")));
                return;
            }

            string label;
            if (species == "tallgrass")
            {
                label = Lang.Get("ecosystemflora:dominance-matrix");
            }
            else if (mod.HoldStrength < 0.8f && spreadRate >= 1.5f)
            {
                label = Lang.Get("ecosystemflora:dominance-colonizer");
            }
            else if (mod.HoldStrength > 1.1f)
            {
                label = Lang.Get("ecosystemflora:dominance-climax");
            }
            else
            {
                label = Lang.Get("ecosystemflora:dominance-stable");
            }

            dsc.AppendLine(Lang.Get("ecosystemflora:handbook-dominance", label));
        }

        static void AppendNiche(StringBuilder dsc, string species)
        {
            if (!WildSpeciesNiche.TryGet(species, out WildSpeciesNiche.Profile profile)) return;

            string moisture = profile.PreferredMoisture switch
            {
                MoistureLevel.Dry => Lang.Get("ecosystemflora:moisture-dry"),
                MoistureLevel.Mesic => Lang.Get("ecosystemflora:moisture-mesic"),
                MoistureLevel.Wet => Lang.Get("ecosystemflora:moisture-wet"),
                MoistureLevel.Shoreline => Lang.Get("ecosystemflora:moisture-shoreline"),
                _ => null
            };

            string light = profile.PreferredLight switch
            {
                LightLevel.DeepShade => Lang.Get("ecosystemflora:light-deepshade"),
                LightLevel.Shade => Lang.Get("ecosystemflora:light-shade"),
                LightLevel.Partial => Lang.Get("ecosystemflora:light-partial"),
                LightLevel.Open => Lang.Get("ecosystemflora:light-open"),
                _ => null
            };

            if (moisture != null) dsc.AppendLine(Lang.Get("ecosystemflora:handbook-niche-moisture", moisture));
            if (light != null) dsc.AppendLine(Lang.Get("ecosystemflora:handbook-niche-light", light));
        }

        static void AppendSeason(StringBuilder dsc, string species)
        {
            if (!WildSpeciesSeason.TryGet(species, out WildSpeciesSeason.Profile profile)) return;

            dsc.AppendLine(Lang.Get("ecosystemflora:handbook-season-spread",
                profile.SpreadMultiplier(Vintagestory.API.Common.EnumSeason.Spring).ToString("0.#"),
                profile.SpreadMultiplier(Vintagestory.API.Common.EnumSeason.Summer).ToString("0.#"),
                profile.SpreadMultiplier(Vintagestory.API.Common.EnumSeason.Fall).ToString("0.#"),
                profile.SpreadMultiplier(Vintagestory.API.Common.EnumSeason.Winter).ToString("0.#")));

            float winterStress = profile.StressChance(0);
            if (winterStress > 0f)
            {
                float survival = (1f - winterStress) * 100f;
                dsc.AppendLine(Lang.Get("ecosystemflora:handbook-winter-survival",
                    survival.ToString("0")));
            }
        }

        static void AppendSymbiosis(StringBuilder dsc, string species)
        {
            if (FloraSymbiosis.TryGetRule(species, out _))
            {
                dsc.AppendLine(Lang.Get("ecosystemflora:handbook-symbiosis"));
            }
        }
    }
}
