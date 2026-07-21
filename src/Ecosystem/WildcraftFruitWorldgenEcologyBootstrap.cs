using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using StjJsonObject = System.Text.Json.Nodes.JsonObject;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Berries are handled by <see cref="WildcraftFruitEcologyBootstrap"/> (special-case Herbarium blocktypes).
    /// This bootstrap adds ecology attributes for other WildcraftFruit worldgen plants (e.g. cacti, fruittrees)
    /// using the extracted worldgen climate table.
    /// </summary>
    internal static class WildcraftFruitWorldgenEcologyBootstrap
    {
        public static void Apply(ICoreAPI api)
        {
            // Attrs always; participation gated at runtime by EnableThirdPartyParticipants.
            if (api == null || api.Side != EnumAppSide.Server) return;

            int injected = 0;
            int mergeFailed = 0;
            foreach (Block block in api.World.Blocks)
            {
                if (!TryInject(block, out bool fail)) continue;
                injected++;
                if (fail) mergeFailed++;
            }

            if (injected > 0)
            {
                api.Logger.Notification(
                    "[ecosystemflora] WildcraftFruit worldgen ecology attrs injected on {0} blocktypes ({1} merge fallbacks; berry bushes use dedicated bootstrap)",
                    injected,
                    mergeFailed);
            }
        }

        internal static bool TryInject(Block block, out bool mergeFailed)
        {
            mergeFailed = false;
            if (block?.Code == null || block.Id == 0) return false;
            if (PlantCodeHelper.HasDeclaredEcologyParticipant(block)) return false;
            if (block.Code.Domain != "wildcraftfruit") return false;
            if (WildcraftFruitFruitingVineEcology.IsFruitingVineBlock(block)) return false;
            if (!TryBuildInjection(block, out JsonObject ecology)) return false;
            if (!MergeEcology(block, ecology)) mergeFailed = true;
            return true;
        }

        static bool TryBuildInjection(Block block, out JsonObject ecology)
        {
            ecology = null;
            string fullCode = block?.Code?.ToString();
            if (string.IsNullOrEmpty(fullCode)) return false;

            if (!WildcraftFruitWorldgenPlants.TryGet(fullCode, out WildcraftFruitWorldgenPlants.Entry entry))
            {
                return false;
            }

            // Skip berries: the dedicated bootstrap is authoritative there.
            // We still keep them in the table for debugging / completeness.
            if (entry.SourceFile != null && entry.SourceFile.Contains("berrybush.json")) return false;

            // Skip harvested variants.
            if (block.Code.Path != null && block.Code.Path.Contains("-harvested-")) return false;

            string species = ResolveSpecies(block);
            if (string.IsNullOrEmpty(species)) return false;

            // Fruit trees: override climate per tree type from fruittreebranch.json attributes.worldgen list.
            if (block.Code.Path != null && block.Code.Path.StartsWith("fruittree-", System.StringComparison.Ordinal)
                && WildcraftFruitFruittreeWorldgen.TryGet(species, out WildcraftFruitFruittreeWorldgen.Entry tree))
            {
                entry = new WildcraftFruitWorldgenPlants.Entry(
                    tree.MinTemp,
                    tree.MaxTemp,
                    tree.MinRain,
                    tree.MaxRain,
                    entry.MinForest,
                    entry.MaxForest,
                    entry.Habitat,
                    entry.SpreadRate,
                    entry.SourceFile);
            }

            string json = string.Format(
                CultureInfo.InvariantCulture,
                @"{{
  ""ecologyParticipant"": true,
  ""ecologySpecies"": ""{0}"",
  ""ecologyHabitat"": ""{1}"",
  ""ecologySpreadBlock"": ""{2}:{3}"",
  ""ecologyMeadowHarvest"": ""none"",
  ""minTemp"": {4},
  ""maxTemp"": {5},
  ""minRain"": {6},
  ""maxRain"": {7},
  ""minForest"": {8},
  ""maxForest"": {9},
  ""ecologySpreadRate"": {10},
  ""ecologySameSpeciesSpacing"": {11},
  ""ecologyOtherSpeciesSpacing"": {12}
}}",
                species,
                entry.Habitat,
                block.Code.Domain,
                block.Code.Path,
                entry.MinTemp,
                entry.MaxTemp,
                entry.MinRain,
                entry.MaxRain,
                entry.MinForest,
                entry.MaxForest,
                entry.SpreadRate,
                entry.Habitat == EcologyHabitat.TerrestrialTree ? 10 : 2,
                entry.Habitat == EcologyHabitat.TerrestrialTree ? 6 : 3);

            ecology = JsonObject.FromJson(json);
            return ecology != null;
        }

        static string ResolveSpecies(Block block)
        {
            if (block?.Code == null) return null;

            // Reuse helper for common patterns (flower-*, etc.)
            string viaHelper = PlantCodeHelper.GetEcologySpecies(block.Code);
            if (!string.IsNullOrEmpty(viaHelper)) return viaHelper;

            string path = block.Code.Path ?? "";

            // Cactus: cactus-<stage><type>-<state> → type
            if (path.StartsWith("cactus-"))
            {
                // examples: cactus-topsaguaro-ripe, cactus-barrelcactus-empty
                string rest = path.Substring("cactus-".Length);
                // strip leading stage tokens (top/bottom/young) if present
                foreach (string stage in new[] { "top", "bottom", "young" })
                {
                    if (rest.StartsWith(stage))
                    {
                        rest = rest.Substring(stage.Length);
                        break;
                    }
                }

                // now rest begins with type e.g. saguaro-ripe or barrelcactus-empty
                int dash = rest.IndexOf('-');
                return dash > 0 ? rest.Substring(0, dash) : rest;
            }

            // Fruittree dynamic branch blocks: fruittree-branch-<type>, fruittree-stem-<type>, fruittree-foliage-<type>-<phase>
            if (path.StartsWith("fruittree-"))
            {
                string[] parts = path.Split('-');
                // Expect at least: fruittree, (branch|stem|foliage), type
                if (parts.Length >= 3)
                {
                    return parts[2];
                }

                // Fallback: keep legacy key if unexpected.
                return "fruittree";
            }

            return null;
        }

        static bool MergeEcology(Block block, JsonObject ecology)
        {
            if (block.Attributes == null)
            {
                block.Attributes = ecology;
                return true;
            }

            string merged = TryMergeJson(block.Attributes.ToString(), ecology.ToString());
            if (merged == null) return false;

            block.Attributes = JsonObject.FromJson(merged);
            return true;
        }

        static string TryMergeJson(string baseJson, string overlayJson)
        {
            if (string.IsNullOrWhiteSpace(baseJson)) return overlayJson;
            if (string.IsNullOrWhiteSpace(overlayJson)) return baseJson;

            try
            {
                if (System.Text.Json.Nodes.JsonNode.Parse(baseJson) is not StjJsonObject baseObj
                    || System.Text.Json.Nodes.JsonNode.Parse(overlayJson) is not StjJsonObject overlayObj)
                {
                    return null;
                }

                foreach (var pair in overlayObj)
                {
                    baseObj[pair.Key] = pair.Value?.DeepClone();
                }

                return baseObj.ToJsonString();
            }
            catch
            {
                return null;
            }
        }
    }
}

