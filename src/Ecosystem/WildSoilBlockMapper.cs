using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class WildSoilBlockMapper
    {
        /// <summary>
        /// Plants never create forest floor from meadow soil — litter layer is canopy-driven (future) / worldgen.
        /// Existing forest floor may shift variant or reclaim to soil on humus death.
        /// </summary>
        internal static bool TryResolveGroundCode(
            string currentGroundPath,
            WildSoilComposition composition,
            PlantSoilRole role,
            SoilSuccessionEvent evt,
            SoilImpact impact,
            out AssetLocation code)
        {
            code = null;
            if (string.IsNullOrEmpty(currentGroundPath) || !IsSuccessionTargetPath(currentGroundPath))
            {
                return false;
            }

            bool wasForestFloor = currentGroundPath.StartsWith("forestfloor");
            string grassCoverage = ExtractGrassCoverage(currentGroundPath);

            if (wasForestFloor)
            {
                bool humusDeathReclaim = evt == SoilSuccessionEvent.Death
                    && impact.FertilityTierDelta > 0f
                    && (role.ProducesHumus() || role == PlantSoilRole.ForestUnderstory);

                if (humusDeathReclaim)
                {
                    string fert = SoilFertilityTierExtensions.ToSoilPathSegment(composition.FertilityTier);
                    code = new AssetLocation("game:soil-" + fert + "-" + grassCoverage);
                    return true;
                }

                int variant = ((int)composition.FertilityTier + (int)composition.Moisture) % 8;
                code = new AssetLocation("game:forestfloor-" + variant);
                return true;
            }

            string soilFert = SoilFertilityTierExtensions.ToSoilPathSegment(composition.FertilityTier);
            code = new AssetLocation("game:soil-" + soilFert + "-" + grassCoverage);
            return true;
        }

        public static bool TryPickGroundBlock(
            ICoreAPI api,
            Block currentGround,
            WildSoilComposition composition,
            PlantSoilRole role,
            SoilSuccessionEvent evt,
            SoilImpact impact,
            out Block newBlock)
        {
            newBlock = null;
            if (api == null || currentGround == null || currentGround.Id == 0) return false;
            if (!IsSuccessionTarget(currentGround)) return false;

            string path = currentGround.Code?.Path ?? "";
            if (!TryResolveGroundCode(path, composition, role, evt, impact, out AssetLocation code))
            {
                return false;
            }

            Block block = api.World.GetBlock(code);
            if (block == null || block.Id == 0) return false;
            if (block.Id == currentGround.Id) return false;

            newBlock = block;
            return true;
        }

        public static bool IsSuccessionTarget(Block ground)
        {
            if (ground?.Code == null || ground.Id == 0) return false;
            return IsSuccessionTargetPath(ground.Code.Path);
        }

        internal static bool IsSuccessionTargetPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            if (path.StartsWith("soil-")) return true;
            if (path.StartsWith("forestfloor")) return true;
            if (path == "peat") return true;

            return false;
        }

        static string ExtractGrassCoverage(string path)
        {
            if (string.IsNullOrEmpty(path)) return "none";

            int lastDash = path.LastIndexOf('-');
            if (lastDash < 0 || lastDash >= path.Length - 1) return "none";

            string tail = path.Substring(lastDash + 1);
            if (tail == "none" || tail == "verysparse" || tail == "sparse" || tail == "normal")
            {
                return tail;
            }

            return "none";
        }
    }
}
