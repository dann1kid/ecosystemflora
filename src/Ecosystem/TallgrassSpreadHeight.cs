using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Picks a vanilla tallgrass growth stage at spread time from local conditions (not parent clone).
    /// Preserves cover type (fern, etc.) and snow/free suffix from the parent block.
    /// </summary>
    internal static class TallgrassSpreadHeight
    {
        internal static readonly string[] HeightStages =
        {
            "veryshort", "short", "mediumshort", "medium", "tall", "verytall",
        };

        static readonly HashSet<string> HeightStageSet = new HashSet<string>(HeightStages);

        internal readonly struct TallgrassPathParts
        {
            public readonly string Prefix;
            public readonly string Cover;
            public readonly string Height;
            public readonly bool HasFree;
            public readonly bool HasSnow;

            public TallgrassPathParts(string prefix, string cover, string height, bool hasFree, bool hasSnow)
            {
                Prefix = prefix;
                Cover = cover;
                Height = height;
                HasFree = hasFree;
                HasSnow = hasSnow;
            }

            public TallgrassPathParts WithHeight(string height) =>
                new TallgrassPathParts(Prefix, Cover, height, HasFree, HasSnow);

            public TallgrassPathParts WithoutCover() =>
                new TallgrassPathParts(Prefix, null, Height, HasFree, HasSnow);
        }

        internal readonly struct TallgrassHeightContext
        {
            public readonly int SunLightLevel;
            public readonly float LocalForestCover;
            public readonly int GroundFertility;
            public readonly float WorldgenRainfall;
            public readonly LightLevel NicheLight;
            public readonly MoistureLevel NicheMoisture;
            public readonly float SeasonGrowthFactor;

            public TallgrassHeightContext(
                int sunLightLevel,
                float localForestCover,
                int groundFertility,
                float worldgenRainfall,
                LightLevel nicheLight,
                MoistureLevel nicheMoisture,
                float seasonGrowthFactor)
            {
                SunLightLevel = sunLightLevel;
                LocalForestCover = localForestCover;
                GroundFertility = groundFertility;
                WorldgenRainfall = worldgenRainfall;
                NicheLight = nicheLight;
                NicheMoisture = nicheMoisture;
                SeasonGrowthFactor = seasonGrowthFactor;
            }
        }

        public static Block ResolveSpreadBlock(
            ICoreAPI api,
            BlockPos plantPos,
            Block parentBlock,
            PlantRequirements requirements,
            System.Random rand)
        {
            if (api == null || parentPosInvalid(parentBlock) || plantPos == null) return parentBlock;

            string path = parentBlock.Code.Path;
            if (!TryParsePath(path, out TallgrassPathParts parts)) return parentBlock;

            TallgrassHeightContext ctx = BuildContext(api, plantPos, requirements);
            int stageIdx = PickStageIndex(in ctx, rand ?? api.World.Rand);
            return ResolveBlock(api, parentBlock, parts, stageIdx);
        }

        /// <summary>Spread offspring at minimum height; cover/snow/free preserved from parent.</summary>
        public static Block ResolveVeryshortSpreadBlock(ICoreAPI api, Block parentBlock)
        {
            if (api == null || parentPosInvalid(parentBlock)) return parentBlock;

            if (!TryParsePath(parentBlock.Code.Path, out TallgrassPathParts parts)) return parentBlock;

            return ResolveBlock(api, parentBlock, parts.WithHeight(HeightStages[0]), 0);
        }

        internal static int GetHeightStageIndex(string height)
        {
            if (string.IsNullOrEmpty(height)) return -1;

            for (int i = 0; i < HeightStages.Length; i++)
            {
                if (HeightStages[i] == height) return i;
            }

            return -1;
        }

        static bool parentPosInvalid(Block parentBlock) =>
            parentBlock?.Code?.Path == null;

        static TallgrassHeightContext BuildContext(ICoreAPI api, BlockPos plantPos, PlantRequirements requirements)
        {
            IBlockAccessor acc = api.World.BlockAccessor;
            EnvironmentalContext env = EnvironmentalContext.SampleForSpread(api, plantPos, requirements);

            int sun = acc.GetLightLevel(plantPos, EnumLightLevelType.OnlySunLight);

            LightLevel nicheLight = LightLevel.Partial;
            MoistureLevel nicheMoisture = MoistureLevel.Mesic;
            NicheSampler niche = EcosystemSystem.Instance?.Niche;
            if (niche != null)
            {
                LocalNiche local = niche.GetNiche(api, plantPos);
                nicheLight = local.Light;
                nicheMoisture = local.Moisture;
            }

            float seasonGrowth = 1f;
            if (EcosystemConfig.Loaded.UseSeasonalEcology && api.World.Calendar != null)
            {
                WildSpeciesSeason.Profile profile = WildSpeciesSeason.Resolve("tallgrass");
                IGameCalendar cal = api.World.Calendar;
                seasonGrowth = profile.SpreadMultiplierInterpolated(cal.DayOfYearf / cal.DaysPerYear);
            }

            return new TallgrassHeightContext(
                sun,
                env.LocalForestCover,
                env.GroundFertility,
                env.WorldgenRainfall,
                nicheLight,
                nicheMoisture,
                seasonGrowth);
        }

        internal static int PickStageIndex(in TallgrassHeightContext ctx, System.Random rand)
        {
            rand ??= new System.Random(0);

            float sun = Clamp(ctx.SunLightLevel / 24f, 0f, 1f);
            float open = 1f - Clamp(ctx.LocalForestCover, 0f, 1f);
            float fertility = Clamp(ctx.GroundFertility / 100f, 0f, 1f);
            float rain = Clamp((ctx.WorldgenRainfall - 0.1f) / 0.9f, 0f, 1f);

            float nicheLight = ctx.NicheLight switch
            {
                LightLevel.Open => 1f,
                LightLevel.Partial => 0.65f,
                LightLevel.Shade => 0.35f,
                LightLevel.DeepShade => 0.15f,
                _ => 0.5f,
            };

            float nicheMoist = ctx.NicheMoisture switch
            {
                MoistureLevel.Wet => 0.85f,
                MoistureLevel.Shoreline => 0.75f,
                MoistureLevel.Mesic => 0.65f,
                MoistureLevel.Dry => 0.35f,
                _ => 0.5f,
            };

            float season = Clamp(ctx.SeasonGrowthFactor / 1.5f, 0f, 1f);

            float score =
                sun * 0.28f
                + open * 0.18f
                + fertility * 0.12f
                + rain * 0.08f
                + nicheLight * 0.18f
                + nicheMoist * 0.08f
                + season * 0.08f;

            score += (float)(rand.NextDouble() * 0.34 - 0.17);
            score = Clamp(score, 0f, 0.98f);

            int idx = (int)(score * HeightStages.Length);
            if (idx >= HeightStages.Length) idx = HeightStages.Length - 1;
            if (idx < 0) idx = 0;

            if (sun < 0.3f && nicheLight < 0.4f)
            {
                idx = Math.Min(idx, 2);
            }

            if (sun > 0.65f && open > 0.5f && score > 0.55f)
            {
                idx = Math.Max(idx, 3);
            }

            return idx;
        }

        static Block ResolveBlock(ICoreAPI api, Block parentBlock, TallgrassPathParts parts, int stageIdx)
        {
            string domain = parentBlock.Code.Domain ?? "game";

            for (int i = stageIdx; i >= 0; i--)
            {
                TallgrassPathParts candidate = parts.WithHeight(HeightStages[i]);
                Block block = TryGetBlock(api, domain, candidate);
                if (block != null) return block;

                if (!string.IsNullOrEmpty(candidate.Cover))
                {
                    block = TryGetBlock(api, domain, candidate.WithoutCover());
                    if (block != null) return block;
                }
            }

            return parentBlock;
        }

        static Block TryGetBlock(ICoreAPI api, string domain, TallgrassPathParts parts)
        {
            string path = BuildPath(parts);
            if (string.IsNullOrEmpty(path)) return null;

            Block block = api.World.GetBlock(new AssetLocation(domain, path));
            if (block == null || block.Id == 0) return null;
            return block;
        }

        internal static bool TryParsePath(string path, out TallgrassPathParts parts)
        {
            parts = default;
            if (string.IsNullOrEmpty(path)) return false;

            string prefix;
            if (path.StartsWith("frostedtallgrass-"))
            {
                prefix = "frostedtallgrass";
            }
            else if (path.StartsWith("tallgrass-"))
            {
                prefix = "tallgrass";
            }
            else
            {
                return false;
            }

            if (path.StartsWith("tallgrass-eaten") || path.Contains("-eaten-")) return false;

            string rest = path.Substring(prefix.Length + 1);
            bool hasSnow = rest.EndsWith("-snow");
            bool hasFree = rest.EndsWith("-free");
            if (hasSnow)
            {
                rest = rest.Substring(0, rest.Length - "-snow".Length);
            }
            else if (hasFree)
            {
                rest = rest.Substring(0, rest.Length - "-free".Length);
            }

            string height = null;
            var coverParts = new List<string>();
            if (rest.Length > 0)
            {
                string[] segments = rest.Split('-');
                for (int i = 0; i < segments.Length; i++)
                {
                    string segment = segments[i];
                    if (string.IsNullOrEmpty(segment)) continue;

                    if (HeightStageSet.Contains(segment))
                    {
                        height = segment;
                    }
                    else
                    {
                        coverParts.Add(segment);
                    }
                }
            }

            string cover = coverParts.Count > 0 ? string.Join("-", coverParts) : null;
            parts = new TallgrassPathParts(prefix, cover, height, hasFree, hasSnow);
            return true;
        }

        internal static string BuildPath(TallgrassPathParts parts)
        {
            if (string.IsNullOrEmpty(parts.Prefix) || string.IsNullOrEmpty(parts.Height)) return null;

            string suffix = parts.HasSnow ? "-snow" : parts.HasFree ? "-free" : "";
            if (string.IsNullOrEmpty(parts.Cover))
            {
                return parts.Prefix + "-" + parts.Height + suffix;
            }

            return parts.Prefix + "-" + parts.Cover + "-" + parts.Height + suffix;
        }

        static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
