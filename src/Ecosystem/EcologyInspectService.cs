using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Network;

namespace WildFarming.Ecosystem
{
    /// <summary>Server-side snapshot for ecology inspect UI (one plant + optional area scan).</summary>
    internal static class EcologyInspectService
    {
        static readonly Dictionary<string, long> lastInspectUtcMs = new Dictionary<string, long>();

        public static bool TryBuildReport(
            ICoreAPI api,
            IPlayer player,
            BlockPos pos,
            out EcologyInspectReportPacket report,
            out string errorLangKey)
        {
            report = null;
            errorLangKey = null;

            if (api == null || player == null || pos == null)
            {
                errorLangKey = "ecosystemflora:inspect-error-noplant";
                report = new EcologyInspectReportPacket { ErrorLangKey = errorLangKey };
                return false;
            }

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableEcologyInspect)
            {
                errorLangKey = "ecosystemflora:inspect-error-disabled";
                report = new EcologyInspectReportPacket { ErrorLangKey = errorLangKey };
                return false;
            }

            if (!cfg.EcosystemEnabled)
            {
                errorLangKey = "ecosystemflora:inspect-error-ecosystem-off";
                report = new EcologyInspectReportPacket { ErrorLangKey = errorLangKey };
                return false;
            }

            if (!CheckCooldown(player, cfg))
            {
                errorLangKey = "ecosystemflora:inspect-error-cooldown";
                report = new EcologyInspectReportPacket { ErrorLangKey = errorLangKey };
                return false;
            }

            IBlockAccessor acc = api.World.BlockAccessor;
            Block block = acc.GetBlock(pos);
            if (block == null || block.Id == 0)
            {
                errorLangKey = "ecosystemflora:inspect-error-noplant";
                report = new EcologyInspectReportPacket { ErrorLangKey = errorLangKey };
                return false;
            }

            string species = PlantCodeHelper.ResolveEcologySpecies(block);
            if (string.IsNullOrEmpty(species))
            {
                errorLangKey = "ecosystemflora:inspect-error-noplant";
                report = new EcologyInspectReportPacket { ErrorLangKey = errorLangKey };
                return false;
            }

            if (cfg.RespectLandClaims && !LandClaimGuard.AllowsEcologyChange(api, pos))
            {
                errorLangKey = "ecosystemflora:inspect-error-claim";
                report = new EcologyInspectReportPacket { ErrorLangKey = errorLangKey };
                return false;
            }

            MarkCooldown(player);

            var inspectLines = new List<InspectLineLite>();
            PlantRequirements req = PlantRequirements.FromBlock(block);

            AppendStaticProfile(inspectLines, species);
            AppendLiveState(api, pos, species, block, req, inspectLines);

            EcologySpacingIndex spacing = EcosystemSystem.Instance?.SpacingIndex;
            int radius = cfg.EcologyInspectScanRadius;
            if (radius < 4) radius = 4;
            if (radius > 32) radius = 32;

            string[] scanSpecies = null;
            int[] scanCounts = null;
            int scanTotal = 0;

            if (cfg.EnableEcologyAreaScan && spacing != null)
            {
                EcologyAreaScanner.Scan(
                    spacing, pos, radius, cfg.SpacingVerticalSearch, out scanSpecies, out scanCounts, out scanTotal);
                AppendAreaScan(inspectLines, radius, scanSpecies, scanCounts, scanTotal);
            }

            report = new EcologyInspectReportPacket
            {
                X = pos.X,
                Y = pos.Y,
                Z = pos.Z,
                Species = species,
                InRegistry = EcosystemSystem.Instance?.RegistryContains(pos) ?? false,
                InspectLines = inspectLines.ToArray(),
                ScanSpecies = scanSpecies,
                ScanCounts = scanCounts,
                ScanTotal = scanTotal,
                ScanRadius = radius,
            };

            return true;
        }

        static bool CheckCooldown(IPlayer player, EcosystemConfig cfg)
        {
            if (cfg.EcologyInspectCooldownSeconds <= 0) return true;
            if (!lastInspectUtcMs.TryGetValue(player.PlayerUID, out long lastMs)) return true;

            long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastMs;
            return elapsed >= (long)(cfg.EcologyInspectCooldownSeconds * 1000);
        }

        static void MarkCooldown(IPlayer player)
        {
            lastInspectUtcMs[player.PlayerUID] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        static void AddInspectLine(List<InspectLineLite> list, string key, params string[] args)
        {
            if (args == null || args.Length == 0)
            {
                list.Add(new InspectLineLite { Key = key });
                return;
            }

            list.Add(new InspectLineLite { Key = key, Args = args });
        }

        static void AppendStaticProfile(List<InspectLineLite> lines, string species)
        {
            AddInspectLine(lines, "ecosystemflora:inspect-line-dominance", "L:" + GetDominanceLabelLangKey(species));

            if (WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile mod))
            {
                string holdKey = mod.HoldStrength < 0.8f
                    ? "ecosystemflora:hold-weak"
                    : mod.HoldStrength > 1.1f
                        ? "ecosystemflora:hold-strong"
                        : "ecosystemflora:hold-moderate";
                AddInspectLine(lines, "ecosystemflora:inspect-line-hold", "L:" + holdKey);
            }
        }

        internal static string GetDominanceLabelLangKey(string species)
        {
            float spreadRate = 1f;
            if (WildFlowerClimate.TryGet(species, out WildFlowerClimate.EcologyEntry flower))
                spreadRate = flower.SpreadRate;
            else if (WildFernEcology.TryGet(species, out WildFernEcology.EcologyEntry fern))
                spreadRate = fern.SpreadRate;
            else if (WildBerryEcology.TryGet(species, out WildBerryEcology.Profile berry))
                spreadRate = berry.SpreadRate;
            else if (WildTreeEcology.TryGet(species, out WildTreeEcology.Profile tree))
                spreadRate = tree.SpreadRate;
            else if (WildTallgrassEcology.TryGet(species, out WildTallgrassEcology.EcologyEntry grass))
                spreadRate = grass.SpreadRate;

            if (species == "tallgrass") return "ecosystemflora:dominance-matrix";

            if (WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile mod))
            {
                if (mod.HoldStrength < 0.8f && spreadRate >= 1.5f)
                    return "ecosystemflora:dominance-colonizer";
                if (mod.HoldStrength > 1.1f)
                    return "ecosystemflora:dominance-climax";
            }

            return "ecosystemflora:dominance-stable";
        }

        static void AppendLiveState(
            ICoreAPI api,
            BlockPos pos,
            string species,
            Block block,
            PlantRequirements req,
            List<InspectLineLite> lines)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            bool harsh = cfg.HarshWildPlants;

            if (EcosystemSystem.Instance != null
                && EcosystemSystem.Instance.TryGetReproducer(pos, out ReproducerEntry entry))
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-registered");

                if (cfg.EnableStressDeath && entry.FailedSurvivalChecks > 0)
                {
                    AddInspectLine(
                        lines,
                        "ecosystemflora:inspect-line-stress",
                        entry.FailedSurvivalChecks.ToString(),
                        cfg.MaxFailedSurvivalChecks.ToString());
                }
                else if (cfg.EnableStressDeath)
                {
                    AddInspectLine(lines, "ecosystemflora:inspect-line-stress-ok");
                }

                if (cfg.EnableTrampling && entry.TramplingExposure > 0)
                {
                    AddInspectLine(
                        lines,
                        "ecosystemflora:inspect-line-trample",
                        entry.TramplingExposure.ToString(),
                        cfg.TramplingStressThreshold.ToString());
                }

                if (api.World?.Calendar != null)
                {
                    double hoursLeft = entry.NextAttemptHours - api.World.Calendar.TotalHours;
                    if (hoursLeft < 0) hoursLeft = 0;
                    double daysLeft = hoursLeft / api.World.Calendar.HoursPerDay;
                    AddInspectLine(lines, "ecosystemflora:inspect-line-next-spread", daysLeft.ToString("0.#"));
                }
            }
            else
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-not-registered");
            }

            if (cfg.UseSeasonalEcology && api.World?.Calendar != null)
            {
                float seasonMult = SeasonEcology.SpreadActivityMultiplier(api, pos, req);
                AddInspectLine(lines, "ecosystemflora:inspect-line-season-now", seasonMult.ToString("0.##"));
            }

            if (WildSpeciesNiche.TryGet(species, out _))
            {
                NicheSampler nicheSampler = EcosystemSystem.Instance?.Niche;
                if (nicheSampler != null && cfg.UseNicheContext)
                {
                    LocalNiche local = nicheSampler.GetNiche(api, pos);
                    float nicheMult = EcologySpreadFitness.NicheMultiplierFor(req, local);
                    AddInspectLine(
                        lines,
                        nicheMult < cfg.NicheStressThreshold
                            ? "ecosystemflora:inspect-line-niche-bad"
                            : "ecosystemflora:inspect-line-niche-ok",
                        nicheMult.ToString("0.##"));
                }
            }

            if (FloraSymbiosis.TryGetRule(species, out _))
            {
                bool host = FloraSymbiosis.HasRequiredHost(api.World.BlockAccessor, pos, species);
                AddInspectLine(
                    lines,
                    host
                        ? "ecosystemflora:inspect-line-symbiosis-ok"
                        : "ecosystemflora:inspect-line-symbiosis-missing");
            }

            EnvironmentalContext ctx = EnvironmentalContext.SampleForSurvival(api, pos, req);

            if (!ctx.HasClimate)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-no-climate");
            }
            else if (!SuitabilityEvaluator.MeetsSurvivalRequirements(req, ctx, harsh))
            {
                InspectLineLite failLine = SuitabilityEvaluator.TryInspectSurvivalFailureLine(req, ctx, harsh);
                if (failLine != null)
                {
                    lines.Add(failLine);
                }
            }
            else
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-survival-ok");
            }
        }

        static void AppendAreaScan(
            List<InspectLineLite> lines,
            int radius,
            string[] scanSpecies,
            int[] scanCounts,
            int scanTotal)
        {
            AddInspectLine(lines, "ecosystemflora:inspect-line-scan-header", radius.ToString());

            if (scanTotal <= 0 || scanSpecies == null || scanCounts == null)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-scan-empty");
                return;
            }

            int show = System.Math.Min(3, scanSpecies.Length);
            for (int i = 0; i < show; i++)
            {
                int pct = (int)System.Math.Round(100.0 * scanCounts[i] / scanTotal);

                AddInspectLine(
                    lines,
                    "ecosystemflora:inspect-line-scan-row",
                    (i + 1).ToString(),
                    "L:ecosystemflora:species-" + scanSpecies[i],
                    pct.ToString());
            }
        }
    }
}
