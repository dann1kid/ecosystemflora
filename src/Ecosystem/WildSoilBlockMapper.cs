using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class WildSoilBlockMapper
    {
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
            bool wasForestFloor = path.StartsWith("forestfloor");
            string grassCoverage = ExtractGrassCoverage(path);
            AssetLocation code;

            if (role == PlantSoilRole.WetlandHerb && composition.Moisture >= 75f)
            {
                code = new AssetLocation("game:peat");
            }
            else if (wasForestFloor || role.IsForestRole())
            {
                bool humusDeathReclaim = wasForestFloor
                    && evt == SoilSuccessionEvent.Death
                    && role.ProducesHumus()
                    && impact.FertilityTierDelta > 0f;

                if (humusDeathReclaim)
                {
                    string fert = SoilFertilityTierExtensions.ToSoilPathSegment(composition.FertilityTier);
                    code = new AssetLocation("game:soil-" + fert + "-" + grassCoverage);
                }
                else
                {
                    int variant = ((int)composition.FertilityTier + (int)composition.Moisture) % 8;
                    code = new AssetLocation("game:forestfloor-" + variant);
                }
            }
            else
            {
                string fert = SoilFertilityTierExtensions.ToSoilPathSegment(composition.FertilityTier);
                code = new AssetLocation("game:soil-" + fert + "-" + grassCoverage);
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

            string path = ground.Code.Path;
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
