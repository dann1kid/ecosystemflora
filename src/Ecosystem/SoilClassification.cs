using Vintagestory.API.Common;
using WildFarming.Network;

namespace WildFarming.Ecosystem
{
    public static class SoilClassification
    {
        public static SoilKind Classify(Block ground)
        {
            if (ground == null || ground.Id == 0) return SoilKind.None;

            string path = ground.Code?.Path ?? "";
            SoilKind kinds = SoilKind.None;

            if (WildSoilGroundRules.IsUnplantableGroundPath(path))
            {
                return SoilKind.Barren;
            }

            if (path.StartsWith("farmland"))
            {
                kinds |= SoilKind.MediumFert | SoilKind.HighFert | SoilKind.LowFert;
            }
            else if (path.StartsWith("soil-high") || path.StartsWith("soil-compost"))
            {
                kinds |= SoilKind.HighFert;
            }
            else if (path.StartsWith("soil-medium"))
            {
                kinds |= SoilKind.MediumFert;
            }
            else if (path.StartsWith("soil-low") || path.StartsWith("soil-verylow"))
            {
                kinds |= SoilKind.LowFert;
            }
            else if (path == "forestfloor")
            {
                kinds |= SoilKind.ForestFloor;
            }
            else if (path == "peat")
            {
                kinds |= SoilKind.Peat;
            }
            else if (path.StartsWith("sand") || ground.BlockMaterial == EnumBlockMaterial.Sand)
            {
                kinds |= SoilKind.Sand;
            }
            else if (path.Contains("clay") || path.StartsWith("rawclay"))
            {
                kinds |= SoilKind.Clay;
            }
            else if (path.Contains("gravel") || path.StartsWith("bony") || path == "cob")
            {
                kinds |= SoilKind.Gravel;
            }
            else if (ground.Fertility <= 0)
            {
                kinds |= SoilKind.Barren;
            }
            else if (ground.Fertility >= 250)
            {
                kinds |= SoilKind.HighFert;
            }
            else if (ground.Fertility >= 180)
            {
                kinds |= SoilKind.MediumFert;
            }
            else if (ground.Fertility >= 80)
            {
                kinds |= SoilKind.LowFert;
            }
            else
            {
                kinds |= SoilKind.Barren;
            }

            return kinds;
        }

        public static bool MeetsSoilRequirements(PlantRequirements req, SoilKind groundKinds, int groundFertility, bool skipMaxFertility = false)
        {
            if (req == null) return true;

            if (req.AllowedSoilKinds != SoilKind.None)
            {
                if ((groundKinds & req.AllowedSoilKinds) == 0) return false;
            }

            if (req.MinGroundFertility > 0 && groundFertility < req.MinGroundFertility) return false;
            if (!skipMaxFertility && req.MaxGroundFertility > 0 && groundFertility > req.MaxGroundFertility) return false;

            return true;
        }

        public static string DescribeSoilFailure(PlantRequirements req, SoilKind groundKinds, int groundFertility, bool skipMaxFertility = false)
        {
            if (req == null) return null;

            if (req.AllowedSoilKinds != SoilKind.None && (groundKinds & req.AllowedSoilKinds) == 0)
            {
                return "Ground type not allowed (got " + groundKinds + ", need " + req.AllowedSoilKinds + ").";
            }

            if (req.MinGroundFertility > 0 && groundFertility < req.MinGroundFertility)
            {
                return "Soil not fertile enough (" + groundFertility + " < " + req.MinGroundFertility + ").";
            }

            if (!skipMaxFertility && req.MaxGroundFertility > 0 && groundFertility > req.MaxGroundFertility)
            {
                return "Soil too rich (" + groundFertility + " > " + req.MaxGroundFertility + ").";
            }

            return null;
        }

        /// <inheritdoc cref="DescribeSoilFailure"/>
        internal static InspectLineLite TryInspectSoilFailureLine(
            PlantRequirements req,
            SoilKind groundKinds,
            int groundFertility,
            bool skipMaxFertility = false)
        {
            if (req == null) return null;

            if (req.AllowedSoilKinds != SoilKind.None && (groundKinds & req.AllowedSoilKinds) == 0)
            {
                return new InspectLineLite
                {
                    Key = "ecosystemflora:inspect-survival-fail-soil-type",
                    Args =
                    [
                        EcologyInspectLineFormat.SoilIntPrefix + (int)groundKinds,
                        EcologyInspectLineFormat.SoilIntPrefix + (int)req.AllowedSoilKinds,
                    ],
                };
            }

            if (req.MinGroundFertility > 0 && groundFertility < req.MinGroundFertility)
            {
                return new InspectLineLite
                {
                    Key = "ecosystemflora:inspect-survival-fail-soil-low-fert",
                    Args =
                    [
                        groundFertility.ToString(),
                        req.MinGroundFertility.ToString(),
                    ],
                };
            }

            if (!skipMaxFertility && req.MaxGroundFertility > 0 && groundFertility > req.MaxGroundFertility)
            {
                return new InspectLineLite
                {
                    Key = "ecosystemflora:inspect-survival-fail-soil-high-fert",
                    Args =
                    [
                        groundFertility.ToString(),
                        req.MaxGroundFertility.ToString(),
                    ],
                };
            }

            return null;
        }

        /// <summary>Maps worldgen patch fertility (0–1) to vanilla block.Fertility scale.</summary>
        public static int WorldgenFertilityToBlock(float worldgen)
        {
            if (worldgen <= 0f) return 0;
            return (int)(worldgen * 320f);
        }
    }
}
