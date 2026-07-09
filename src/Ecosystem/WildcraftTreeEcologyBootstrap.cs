using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using StjJsonObject = System.Text.Json.Nodes.JsonObject;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Wildcraft Trees (wildcrafttree) uses custom trunk blocks and saplings. Inject ecology attrs
    /// on wild log-grown trunks when missing so they can reproduce in the ecosystem simulation.
    /// Stage A: defaults only (no per-species tuning).
    /// </summary>
    internal static class WildcraftTreeEcologyBootstrap
    {
        public static void Apply(ICoreAPI api)
        {
            if (api == null || api.Side != EnumAppSide.Server) return;
            if (!EcosystemConfig.Loaded.EnableThirdPartyParticipants) return;

            int injected = 0;
            int mergeFailed = 0;
            foreach (Block block in api.World.Blocks)
            {
                if (block?.Code == null || block.Id == 0) continue;
                if (block.Code.Domain != "wildcrafttree") continue;
                if (PlantCodeHelper.HasDeclaredEcologyParticipant(block)) continue;

                if (!TryBuildInjection(block, out JsonObject ecology)) continue;
                if (!MergeEcology(block, ecology)) mergeFailed++;

                injected++;
            }

            if (injected > 0)
            {
                api.Logger.Notification(
                    "[ecosystemflora] Wildcraft tree ecology attrs injected on {0} blocktypes ({1} merge fallbacks)",
                    injected,
                    mergeFailed);
            }
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

        static bool TryBuildInjection(Block block, out JsonObject ecology)
        {
            ecology = null;
            if (block?.Code == null || block.Code.Domain != "wildcrafttree") return false;

            // Mature trunks only: wildcrafttree:log-grown-{wood}-{rotation}
            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("log-grown-")) return false;

            string rest = path.Substring("log-grown-".Length);
            int dash = rest.IndexOf('-');
            if (dash <= 0) return false;
            string wood = rest.Substring(0, dash);
            if (string.IsNullOrEmpty(wood)) return false;

            // Worldgen-informed defaults (stage B): use Wildcraft Trees treengenproperties when available.
            float minTemp = -5;
            float maxTemp = 35;
            float minRain = 0.35f;
            float maxRain = 1.0f;
            float minForest = 0.0f;
            float maxForest = 1.0f;

            if (WildcraftTreeWorldgenClimate.TryGetForWood(wood, out WildcraftTreeWorldgenClimate.Entry entry))
            {
                minTemp = entry.MinTemp;
                maxTemp = entry.MaxTemp;
                minRain = entry.MinRain;
                maxRain = entry.MaxRain;
                minForest = entry.MinForest;
                maxForest = entry.MaxForest;
            }

            string json = string.Format(
                CultureInfo.InvariantCulture,
                @"{{
  ""ecologyParticipant"": true,
  ""ecologySpecies"": ""{0}"",
  ""ecologyHabitat"": ""TerrestrialTree"",
  ""ecologySpreadBlock"": ""wildcrafttree:sapling-{0}-free"",
  ""ecologyMatureBlock"": ""wildcrafttree:log-grown-{0}-ud"",
  ""ecologyMinSunlight"": 10,
  ""minTemp"": {1},
  ""maxTemp"": {2},
  ""minRain"": {3},
  ""maxRain"": {4},
  ""minForest"": {5},
  ""maxForest"": {6},
  ""ecologySpreadRate"": 0.45,
  ""ecologySpreadRadius"": 9,
  ""ecologySameSpeciesSpacing"": 6,
  ""ecologyOtherSpeciesSpacing"": 4
}}",
                wood,
                minTemp,
                maxTemp,
                minRain,
                maxRain,
                minForest,
                maxForest);

            ecology = JsonObject.FromJson(json);
            return ecology != null;
        }
    }
}

