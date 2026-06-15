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

            if (cfg.EnableMyceliumEcology
                && MyceliumInspect.IsMushroomBlock(block)
                && TryBuildMyceliumReport(api, pos, block, cfg, out report))
            {
                if (cfg.RespectLandClaims && report != null
                    && !LandClaimGuard.AllowsEcologyChange(api, new BlockPos(report.X, report.Y, report.Z)))
                {
                    errorLangKey = "ecosystemflora:inspect-error-claim";
                    report = new EcologyInspectReportPacket { ErrorLangKey = errorLangKey };
                    return false;
                }

                MarkCooldown(player);
                return true;
            }

            if (cfg.EnableMyceliumEcology
                && MyceliumInspect.TryGetAnchorContext(acc, pos, out BlockPos soilAnchorPos, out _, out PlantRequirements soilReq)
                && TryBuildMyceliumReportFromAnchor(api, soilAnchorPos, soilReq, cfg, out report))
            {
                if (cfg.RespectLandClaims && !LandClaimGuard.AllowsEcologyChange(api, soilAnchorPos))
                {
                    errorLangKey = "ecosystemflora:inspect-error-claim";
                    report = new EcologyInspectReportPacket { ErrorLangKey = errorLangKey };
                    return false;
                }

                MarkCooldown(player);
                return true;
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
            AppendMatSpreadProfile(api, pos, req, inspectLines);

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
            else if (WildAquaticEcology.TryGet(species, out WildAquaticEcology.Profile aquatic))
                spreadRate = aquatic.SpreadRate;
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

                AppendTreeAgingInspect(api, entry, lines);

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

            if (cfg.EnableMyceliumNiche && req.Habitat == EcologyHabitat.Terrestrial)
            {
                AppendMyceliumInspect(api, pos, req, lines);
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

        static void AppendMatSpreadProfile(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements req,
            List<InspectLineLite> lines)
        {
            if (req == null || api?.World?.BlockAccessor == null) return;

            if (req.UsesRhizomeSpread)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-spread-mode-rhizome");
            }
            else if (req.UsesSurfaceMatSpread)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-spread-mode-surfacemat");
            }
            else if (req.Habitat == EcologyHabitat.ReedNearWater
                     || req.Habitat == EcologyHabitat.WaterSurface)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-spread-mode-independent");
            }
            else
            {
                return;
            }

            if (req.UsesRhizomeSpread || req.UsesSurfaceMatSpread)
            {
                IBlockAccessor acc = api.World.BlockAccessor;
                int verticalReach = req.UsesRhizomeSpread
                    ? RhizomeSpread.DefaultVerticalReach
                    : SurfaceMatSpread.DefaultVerticalReach;

                bool frontier = req.UsesRhizomeSpread
                    ? RhizomeSpread.IsFrontier(acc, pos, req.Species, verticalReach)
                    : SurfaceMatSpread.IsFrontier(acc, pos, req.Species, verticalReach);

                AddInspectLine(
                    lines,
                    frontier
                        ? "ecosystemflora:inspect-line-mat-frontier-yes"
                        : "ecosystemflora:inspect-line-mat-frontier-no");

                EcosystemConfig cfg = EcosystemConfig.Loaded;
                if (cfg != null && cfg.RhizomeSeedDispersalEnabled)
                {
                    float seedChance = req.UsesRhizomeSpread
                        ? RhizomeSpread.EffectiveSeedDispersalChance(req)
                        : SurfaceMatSpread.EffectiveSeedDispersalChance(req);

                    if (seedChance > 0f)
                    {
                        AddInspectLine(
                            lines,
                            "ecosystemflora:inspect-line-seed-dispersal-chance",
                            (seedChance * 100f).ToString("0.#"));
                    }
                }
            }
        }

        static bool TryBuildMyceliumReport(
            ICoreAPI api,
            BlockPos pos,
            Block capBlock,
            EcosystemConfig cfg,
            out EcologyInspectReportPacket report)
        {
            report = null;
            if (api?.World?.BlockAccessor == null || capBlock?.Code == null) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos ground = pos.DownCopy();

            if (MyceliumInspect.TryGetAnchorContext(acc, pos, out BlockPos anchorPos, out _, out PlantRequirements anchorReq))
            {
                return TryBuildMyceliumReportFromAnchor(api, anchorPos, anchorReq, cfg, out report);
            }

            if (!MyceliumEcology.TryBuildRequirements(capBlock.Code, acc.GetBlock(ground), out PlantRequirements capReq))
            {
                return false;
            }

            var lines = new List<InspectLineLite>();
            AddInspectLine(lines, "ecosystemflora:inspect-line-mycelium-no-be");
            AppendMyceliumCapState(api, ground, capReq, lines, hasAnchorBe: false);

            report = new EcologyInspectReportPacket
            {
                X = ground.X,
                Y = ground.Y,
                Z = ground.Z,
                Species = capReq.Species,
                InRegistry = false,
                InspectLines = lines.ToArray(),
                ScanRadius = cfg.EcologyInspectScanRadius,
            };
            return true;
        }

        static bool TryBuildMyceliumReportFromAnchor(
            ICoreAPI api,
            BlockPos anchorPos,
            PlantRequirements anchorReq,
            EcosystemConfig cfg,
            out EcologyInspectReportPacket report)
        {
            report = null;
            if (api == null || anchorPos == null || anchorReq == null) return false;

            var lines = new List<InspectLineLite>();
            AppendMyceliumCapState(api, anchorPos, anchorReq, lines, hasAnchorBe: true);

            report = new EcologyInspectReportPacket
            {
                X = anchorPos.X,
                Y = anchorPos.Y,
                Z = anchorPos.Z,
                Species = anchorReq.Species,
                InRegistry = EcosystemSystem.Instance?.RegistryContains(anchorPos) ?? false,
                InspectLines = lines.ToArray(),
                ScanRadius = cfg.EcologyInspectScanRadius,
            };
            return true;
        }

        static void AppendMyceliumCapState(
            ICoreAPI api,
            BlockPos anchorPos,
            PlantRequirements req,
            List<InspectLineLite> lines,
            bool hasAnchorBe)
        {
            AppendMyceliumAnchorState(api, anchorPos, req, lines);

            if (!hasAnchorBe) return;

            if (WildSoilGroundRules.HasActiveMycelium(api.World.BlockAccessor, anchorPos))
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-mycelium-anchor");
            }
        }

        static void AppendMyceliumAnchorState(
            ICoreAPI api,
            BlockPos anchorPos,
            PlantRequirements req,
            List<InspectLineLite> lines)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            Block anchorBlock = api.World.BlockAccessor.GetBlock(anchorPos);
            MyceliumNiche niche = MyceliumEcology.GetNicheForRequirements(req, anchorBlock);

            AddInspectLine(lines, MyceliumEcology.NicheLangKey(niche));

            ReproducerEntry entry = null;
            if (EcosystemSystem.Instance != null
                && EcosystemSystem.Instance.TryGetReproducer(anchorPos, out entry))
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-mycelium-registered");

                if (entry.FailedSurvivalChecks > 0)
                {
                    AddInspectLine(
                        lines,
                        "ecosystemflora:inspect-line-stress",
                        entry.FailedSurvivalChecks.ToString(),
                        cfg.MaxFailedSurvivalChecks.ToString());
                }
                else
                {
                    AddInspectLine(lines, "ecosystemflora:inspect-line-stress-ok");
                }
            }
            else
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-mycelium-unregistered");
            }

            if (MyceliumStressEvaluator.MeetsSurvival(api, anchorPos, req))
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-mycelium-survival-ok");
            }
            else
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-mycelium-survival-bad");
            }

            if (cfg.EnableMyceliumNetworkSpread
                && MyceliumEcology.GetNicheForRequirements(req, anchorBlock) != MyceliumNiche.TrunkPolypore)
            {
                IBlockAccessor acc = api.World.BlockAccessor;
                bool frontier = MyceliumNetworkSpread.IsNetworkFrontier(
                    api, acc, anchorPos, niche);
                AddInspectLine(
                    lines,
                    frontier
                        ? "ecosystemflora:inspect-line-mycelium-frontier-yes"
                        : "ecosystemflora:inspect-line-mycelium-frontier-no");

                if (entry != null && api.World?.Calendar != null)
                {
                    double hoursLeft = entry.NextAttemptHours - api.World.Calendar.TotalHours;
                    if (hoursLeft < 0) hoursLeft = 0;
                    double daysLeft = hoursLeft / api.World.Calendar.HoursPerDay;
                    AddInspectLine(lines, "ecosystemflora:inspect-line-mycelium-next-spread", daysLeft.ToString("0.#"));
                }
            }
        }

        static void AppendTreeAgingInspect(
            ICoreAPI api,
            ReproducerEntry entry,
            List<InspectLineLite> lines)
        {
            if (entry?.Requirements?.Habitat != EcologyHabitat.TerrestrialTree) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableTreeAging || api?.World?.BlockAccessor == null) return;

            Block block = api.World.BlockAccessor.GetBlock(entry.Origin);
            string wood = PlantCodeHelper.GetTreeWood(block);
            if (string.IsNullOrEmpty(wood)) return;

            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            TreeStructureMetrics metrics = TreeStructureProbe.Measure(
                api.World.BlockAccessor,
                entry.Origin,
                wood);

            int maxTrunk = TreeGrowthTargets.MaxTargetTrunkHeight(profile, cfg.TreeGrowthActivityScale);
            int maxCrown = TreeGrowthTargets.MaxTargetCrownRadius(profile, cfg.TreeGrowthActivityScale);
            int maturityPct = TreeGrowthTargets.MaturityPercent(
                metrics.TrunkHeight,
                metrics.CrownRadius,
                profile);

            AddInspectLine(
                lines,
                "ecosystemflora:inspect-line-tree-maturity",
                maturityPct.ToString(),
                metrics.TrunkHeight.ToString(),
                maxTrunk.ToString(),
                metrics.CrownRadius.ToString(),
                maxCrown.ToString());
        }

        static void AppendMyceliumInspect(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements req,
            List<InspectLineLite> lines)
        {
            if (api?.World?.BlockAccessor == null || req == null || pos == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            int radius = cfg.MyceliumZoneRadius > 0 ? cfg.MyceliumZoneRadius : MyceliumZone.VanillaGrowRange;
            BlockPos groundPos = pos.DownCopy();

            if (WildSoilGroundRules.HasActiveMycelium(api.World.BlockAccessor, groundPos))
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-mycelium-anchor");
                return;
            }

            if (!MyceliumZone.TryGetNearestAnchorNiche(
                api.World.BlockAccessor, groundPos, radius, out int distance, out MyceliumNiche nearestNiche))
            {
                return;
            }

            if (!WildSpeciesSoilSuccession.TryGetRole(req.Species, out PlantSoilRole role)) return;

            float mult = MyceliumZone.SpreadMultiplierForRole(
                role,
                distance,
                radius,
                nearestNiche,
                cfg.MyceliumMeadowSpreadPenalty,
                cfg.MyceliumForestSpreadBonus);

            if (System.Math.Abs(mult - 1f) < 0.02f) return;

            AddInspectLine(
                lines,
                mult < 1f
                    ? "ecosystemflora:inspect-line-mycelium-meadow-penalty"
                    : "ecosystemflora:inspect-line-mycelium-forest-bonus",
                distance.ToString(),
                mult.ToString("0.##"));
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
