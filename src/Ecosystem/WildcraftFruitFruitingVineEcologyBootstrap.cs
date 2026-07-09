using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using StjJsonObject = System.Text.Json.Nodes.JsonObject;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Climate-only ecology for Wildcraft Fruit fruiting vines (Herbarium <c>BlockFruitingVines</c>).
    /// Spread is disabled; vertical growth and phenology stay in Herbarium block entities.
    /// </summary>
    internal static class WildcraftFruitFruitingVineEcologyBootstrap
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
                if (PlantCodeHelper.HasDeclaredEcologyParticipant(block)) continue;
                if (!TryBuildInjection(block, out JsonObject ecology)) continue;

                if (!MergeEcology(block, ecology))
                {
                    mergeFailed++;
                }

                injected++;
            }

            if (injected > 0)
            {
                api.Logger.Notification(
                    "[ecosystemflora] Wildcraft fruitvine climate attrs injected on {0} blocktypes ({1} merge fallbacks; spread disabled)",
                    injected,
                    mergeFailed);
            }
        }

        static bool TryBuildInjection(Block block, out JsonObject ecology)
        {
            ecology = null;
            if (!WildcraftFruitFruitingVineEcology.TryGetVineType(block, out string vineType)) return false;
            if (!WildcraftFruitFruitingVineEcology.TryGetProfile(vineType, out WildcraftFruitFruitingVineEcology.Profile profile))
            {
                return false;
            }

            if (!WildcraftFruitFruitingVineEcology.TryGetSpreadBlock(block, out AssetLocation spread))
            {
                return false;
            }

            string json = string.Format(
                CultureInfo.InvariantCulture,
                @"{{
  ""ecologyParticipant"": true,
  ""ecologySpecies"": ""{0}"",
  ""ecologyHabitat"": ""Terrestrial"",
  ""ecologySpreadBlock"": ""{1}:{2}"",
  ""ecologyMeadowHarvest"": ""none"",
  ""minTemp"": {3},
  ""maxTemp"": {4},
  ""minRain"": {5},
  ""maxRain"": {6},
  ""minForest"": {7},
  ""maxForest"": {8},
  ""ecologySpreadRate"": 0,
  ""ecologySameSpeciesSpacing"": 0,
  ""ecologyOtherSpeciesSpacing"": 0
}}",
                vineType,
                spread.Domain,
                spread.Path,
                profile.MinTemp,
                profile.MaxTemp,
                profile.MinRain,
                profile.MaxRain,
                profile.MinForest,
                profile.MaxForest);

            ecology = JsonObject.FromJson(json);
            return ecology != null;
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
