using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem.SpeciesEcology;
using WildFarming.Network;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

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

            AppendRecentHistory(api, pos, cfg, inspectLines);

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

        static void AppendRecentHistory(ICoreAPI api, BlockPos pos, EcosystemConfig cfg, List<InspectLineLite> lines)
        {
            if (!cfg.EnableEcologyHistoryHint) return;

            var history = new List<InspectLineLite>();
            if (!EcologyHistoryRecorder.TryBuildHintLines(api, pos, history, maxLines: 3)) return;

            AddInspectLine(lines, "ecosystemflora:inspect-line-history-header");
            lines.AddRange(history);
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

        static bool TryAppendTallgrassEstablishmentInspect(
            ICoreAPI api,
            BlockPos pos,
            Block block,
            List<InspectLineLite> lines)
        {
            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null) return false;
            if (!TallgrassEstablishmentInspect.TryBuild(api, pos, block, eco, out TallgrassEstablishmentInspect.Snapshot snap))
            {
                return false;
            }

            string current = TallgrassEstablishmentInspect.StageLabel(snap.CurrentStageIndex);
            string registerAt = TallgrassEstablishmentInspect.StageLabel(snap.RegisterStageIndex);
            string target = TallgrassEstablishmentInspect.StageLabel(snap.TargetStageIndex);

            AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-queue-header");

            switch (snap.Phase)
            {
                case TallgrassEstablishmentInspect.Phase.Growing:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-queue-active");
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-growing");
                    break;
                case TallgrassEstablishmentInspect.Phase.WaitingForScan:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-queue-not-enqueued");
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-waiting-scan");
                    break;
                case TallgrassEstablishmentInspect.Phase.RegistrationPending:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-queue-finished");
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-registration-pending");
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-height-now", current);
                    return true;
            }

            AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-height-now", current);
            AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-height-register", registerAt);
            AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-height-target", target);

            int stepsLeft = snap.RegisterStageIndex - snap.CurrentStageIndex;
            if (stepsLeft > 0)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-steps-left", stepsLeft.ToString());
            }

            if (snap.Phase == TallgrassEstablishmentInspect.Phase.Growing
                && snap.HoursUntilNextStage >= 0
                && api.World?.Calendar != null)
            {
                double days = snap.HoursUntilNextStage / api.World.Calendar.HoursPerDay;
                AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-next-stage", days.ToString("0.#"));
            }

            return true;
        }

        static void AppendStaticProfile(List<InspectLineLite> lines, string species)
        {
            AddInspectLine(lines, "ecosystemflora:inspect-line-dominance", "L:" + SpeciesEcologyDisplay.GetDominanceLabelLangKey(species));

            float hold = SpeciesEcologyDisplay.ResolveHoldStrength(species);
            string holdKey = hold < 0.8f
                ? "ecosystemflora:hold-weak"
                : hold > 1.1f
                    ? "ecosystemflora:hold-strong"
                    : "ecosystemflora:hold-moderate";
            AddInspectLine(lines, "ecosystemflora:inspect-line-hold", "L:" + holdKey);
        }

        internal static string GetDominanceLabelLangKey(string species) =>
            SpeciesEcologyDisplay.GetDominanceLabelLangKey(species);

        static BlockPos ResolveInspectContextPos(IBlockAccessor acc, BlockPos pos, Block block, PlantRequirements req)
        {
            if (acc == null || pos == null) return pos;
            if (req?.Habitat == EcologyHabitat.TerrestrialTree
                && PlantCodeHelper.IsTreeLogGrownBlock(block))
            {
                return PlantCodeHelper.GetTreeTrunkBase(acc, pos);
            }

            if (req?.Habitat == EcologyHabitat.Ferntree
                && PlantCodeHelper.IsFerntreeEcologyBlock(block))
            {
                return FerntreeStructure.GetTrunkBase(acc, pos);
            }

            return pos;
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
            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos contextPos = ResolveInspectContextPos(acc, pos, block, req);

            bool tallgrassMaturation = species == "tallgrass"
                && TallgrassSpreadMaturation.UsesMaturation(cfg);
            bool tallgrassSpreadReady = !tallgrassMaturation
                || TallgrassSpreadMaturation.CanReproduceFrom(block, api, contextPos);

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco != null && api.Side == EnumAppSide.Server && !eco.RegistryContains(pos))
            {
                if (tallgrassMaturation && !tallgrassSpreadReady)
                {
                    if (TallgrassEstablishment.ShouldQueueAfterPlacement(api, pos, block))
                    {
                        eco.TryQueueTallgrassPromotionAtInspect(pos, block);
                    }
                }
                else
                {
                    eco.TryRegisterEligiblePlantAtInspect(pos, block);
                }
            }

            ReproducerEntry registryEntry = null;
            bool inRegistry = eco != null && eco.TryGetReproducer(pos, out registryEntry);

            if (FlowerJuvenileBlocks.IsJuvenileBlock(block) && !inRegistry)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-flower-establishing");

                if (api.World?.Calendar != null
                    && EcosystemSystem.Instance != null
                    && EcosystemSystem.Instance.TryGetFlowerMaturationHoursLeft(pos, out double hoursLeft))
                {
                    double daysLeft = hoursLeft / api.World.Calendar.HoursPerDay;
                    AddInspectLine(
                        lines,
                        "ecosystemflora:inspect-line-flower-maturing",
                        daysLeft.ToString("0.#"));
                }

                return;
            }

            if (FernJuvenileBlocks.IsJuvenileBlock(block) && !inRegistry)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-flower-establishing");

                if (api.World?.Calendar != null
                    && EcosystemSystem.Instance != null
                    && EcosystemSystem.Instance.TryGetFernMaturationHoursLeft(pos, out double hoursLeft))
                {
                    double daysLeft = hoursLeft / api.World.Calendar.HoursPerDay;
                    AddInspectLine(
                        lines,
                        "ecosystemflora:inspect-line-flower-maturing",
                        daysLeft.ToString("0.#"));
                }

                return;
            }

            if (ShoreSedgeJuvenileBlocks.IsJuvenileBlock(block) && !inRegistry)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-flower-establishing");

                if (api.World?.Calendar != null
                    && EcosystemSystem.Instance != null
                    && EcosystemSystem.Instance.TryGetShoreSedgeMaturationHoursLeft(pos, out double hoursLeft))
                {
                    double daysLeft = hoursLeft / api.World.Calendar.HoursPerDay;
                    AddInspectLine(
                        lines,
                        "ecosystemflora:inspect-line-flower-maturing",
                        daysLeft.ToString("0.#"));
                }

                return;
            }

            if (species == "tallgrass"
                && tallgrassMaturation
                && !tallgrassSpreadReady
                && TryAppendTallgrassEstablishmentInspect(api, pos, block, lines))
            {
                return;
            }

            if (inRegistry)
            {
                ReproducerEntry entry = registryEntry;
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
                AppendFerntreeAgingInspect(api, entry, lines);

                if (api.World?.Calendar != null)
                {
                    double now = api.World.Calendar.TotalHours;
                    double hoursLeft = entry.NextAttemptHours - now;
                    if (hoursLeft < 0) hoursLeft = 0;

                    double spawnCooldownLeft = entry.NextSpawnAllowedAtHours - now;
                    if (spawnCooldownLeft > hoursLeft)
                    {
                        hoursLeft = spawnCooldownLeft;
                    }

                    double daysLeft = hoursLeft / api.World.Calendar.HoursPerDay;
                    AddInspectLine(lines, "ecosystemflora:inspect-line-next-spread", daysLeft.ToString("0.#"));

                    if (spawnCooldownLeft > 0
                        && SpreadMaturationPolicies.UsesPostSpreadAttemptCooldown(cfg, entry.Requirements?.Species))
                    {
                        double cooldownDays = spawnCooldownLeft / api.World.Calendar.HoursPerDay;
                        AddInspectLine(
                            lines,
                            "ecosystemflora:inspect-line-spawn-cooldown",
                            cooldownDays.ToString("0.#"));
                    }
                }

                AppendLastSpreadAttempt(entry, lines);

                if (FlowerPhenology.UsesPhenology(cfg, entry.Requirements))
                {
                    AppendFlowerPhenologyInspect(api, entry, cfg, lines);
                }
                else if (FernPhenology.UsesPhenology(cfg, entry.Requirements))
                {
                    AppendFernPhenologyInspect(api, entry, cfg, lines);
                }
                else if (TallgrassPhenology.UsesPhenology(cfg, entry.Requirements))
                {
                    AppendTallgrassPhenologyInspect(entry, lines);
                }
                else if (WildFernSpread.UsesSporulationGate(cfg, entry.Requirements))
                {
                    bool sporulating = WildFernSpread.CanSpread(api, entry, cfg);
                    AddInspectLine(
                        lines,
                        sporulating
                            ? "ecosystemflora:inspect-line-fern-sporulating"
                            : "ecosystemflora:inspect-line-fern-dormant");
                }
            }
            else
            {
                if (eco != null && eco.IsRegistrationPendingAt(pos))
                {
                    AddInspectLine(lines, "ecosystemflora:inspect-line-registration-pending");
                }
                else if (eco != null
                         && !eco.IsChunkRegistrationFinished(ReproducerRegistry.ToChunkCoord(pos)))
                {
                    AddInspectLine(lines, "ecosystemflora:inspect-line-waiting-scan");
                }
                else
                {
                    AddInspectLine(lines, "ecosystemflora:inspect-line-not-registered");
                    if (block?.Code != null)
                    {
                        AddInspectLine(
                            lines,
                            "ecosystemflora:inspect-line-block-code",
                            block.Code.Domain + ":" + block.Code.Path);
                    }
                }
            }

            if (cfg.UseSeasonalEcology && api.World?.Calendar != null)
            {
                float seasonMult = SeasonEcology.SpreadActivityMultiplier(api, contextPos, req);
                AddInspectLine(lines, "ecosystemflora:inspect-line-season-now", seasonMult.ToString("0.##"));
            }

            if (SpeciesEcologyDisplay.HasNicheProfile(species, req))
            {
                NicheSampler nicheSampler = EcosystemSystem.Instance?.Niche;
                if (nicheSampler != null && cfg.UseNicheContext)
                {
                    LocalNiche local = nicheSampler.GetNiche(api, contextPos);
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

            EnvironmentalContext ctx = EnvironmentalContext.SampleForSurvival(api, contextPos, req);

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

        static void AppendFlowerPhenologyInspect(
            ICoreAPI api,
            ReproducerEntry entry,
            EcosystemConfig cfg,
            List<InspectLineLite> lines)
        {
            if (entry == null || lines == null) return;

            switch (entry.PhenologyPhase)
            {
                case FlowerPhenologyPhase.Dormant:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-flower-phase-dormant");
                    break;
                case FlowerPhenologyPhase.Vegetative:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-flower-phase-vegetative");
                    break;
                case FlowerPhenologyPhase.Bloom:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-flower-phase-bloom");
                    break;
                case FlowerPhenologyPhase.Dieback:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-flower-phase-dieback");
                    break;
            }

            AddInspectLine(
                lines,
                "ecosystemflora:inspect-line-flower-energy",
                (entry.PhenologyEnergy / Math.Max(0.01f, cfg.FlowerBloomEnergyThreshold)).ToString("0.##"));

            if (entry.PhenologyPhase == FlowerPhenologyPhase.Vegetative && api?.World?.Calendar != null)
            {
                double hoursLeft = EstimateBloomHoursRemaining(api, entry, cfg);
                if (hoursLeft > 0)
                {
                    double daysLeft = hoursLeft / api.World.Calendar.HoursPerDay;
                    AddInspectLine(
                        lines,
                        "ecosystemflora:inspect-line-flower-bloom-eta",
                        daysLeft.ToString("0.#"));
                }
            }
        }

        static void AppendFernPhenologyInspect(
            ICoreAPI api,
            ReproducerEntry entry,
            EcosystemConfig cfg,
            List<InspectLineLite> lines)
        {
            if (entry == null || lines == null) return;

            switch (entry.FernPhenologyPhase)
            {
                case FernPhenologyPhase.Dormant:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-fern-phase-dormant");
                    break;
                case FernPhenologyPhase.Sporulating:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-fern-phase-sporulating");
                    break;
                case FernPhenologyPhase.Dieback:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-fern-phase-dieback");
                    break;
            }

            if (FernPhenology.ShouldUseDieback(api.World.BlockAccessor, entry, cfg))
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-symbiosis-missing");
            }
        }

        static void AppendTallgrassPhenologyInspect(ReproducerEntry entry, List<InspectLineLite> lines)
        {
            if (entry == null || lines == null) return;

            switch (entry.TallgrassPhenologyPhase)
            {
                case TallgrassPhenologyPhase.Dormant:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-phase-dormant");
                    break;
                case TallgrassPhenologyPhase.Active:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-phase-active");
                    break;
                case TallgrassPhenologyPhase.Dieback:
                    AddInspectLine(lines, "ecosystemflora:inspect-line-tallgrass-phase-dieback");
                    break;
            }
        }

        static double EstimateBloomHoursRemaining(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null || cfg == null || api?.World?.Calendar == null) return 0;

            WildSpeciesSeason.Profile profile = WildSpeciesSeason.Resolve(entry.Requirements.Species);
            float yearProgress = api.World.Calendar.DayOfYearf / api.World.Calendar.DaysPerYear;
            float season = profile.SpreadMultiplierInterpolated(yearProgress);
            if (season < 0.05f) return 0;

            float energyGap = cfg.FlowerBloomEnergyThreshold - entry.PhenologyEnergy;
            if (energyGap <= 0) return 0;

            float dailyGain = cfg.FlowerPhenologyEnergyGainPerDay * season;
            if (dailyGain <= 0.001f) return 0;

            double days = energyGap / dailyGain;
            return days * api.World.Calendar.HoursPerDay;
        }

        static void AppendLastSpreadAttempt(ReproducerEntry entry, List<InspectLineLite> lines)
        {
            if (entry == null || entry.LastSpreadAttemptAtHours <= 0) return;

            if (entry.LastSpreadPlaced)
            {
                switch (entry.LastSpreadCollectMode)
                {
                    case MatSpreadCollectMode.SeedDispersal:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-last-spread-seed");
                        break;
                    case MatSpreadCollectMode.MatEdge:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-last-spread-rhizome");
                        break;
                    default:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-last-spread-independent");
                        break;
                }

                return;
            }

            if (!string.IsNullOrEmpty(entry.LastSpreadFailureReason))
            {
                AddInspectLine(
                    lines,
                    "ecosystemflora:inspect-line-last-spread-failed",
                    entry.LastSpreadFailureReason);
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
            else if (req.UsesFernRhizomeSpread)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-spread-mode-fern-rhizome");
            }
            else if (req.UsesBerryColonySpread)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-spread-mode-berry-colony");
            }
            else if (req.UsesShoreSedgeMatSpread)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-spread-mode-shore-sedge");
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

            if (req.UsesRhizomeSpread || req.UsesSurfaceMatSpread || req.UsesFernRhizomeSpread
                || req.UsesBerryColonySpread || req.UsesShoreSedgeMatSpread)
            {
                IBlockAccessor acc = api.World.BlockAccessor;
                bool frontier = MatSpreadDispatch.IsFrontier(acc, pos, req, verticalSearch: 0);

                AddInspectLine(
                    lines,
                    frontier
                        ? "ecosystemflora:inspect-line-mat-frontier-yes"
                        : "ecosystemflora:inspect-line-mat-frontier-no");

                EcosystemConfig cfg = EcosystemConfig.Loaded;
                if (cfg != null && cfg.RhizomeSeedDispersalEnabled
                    && (req.UsesRhizomeSpread || req.UsesSurfaceMatSpread || req.UsesBerryColonySpread
                        || req.UsesShoreSedgeMatSpread))
                {
                    float seedChance = req.UsesRhizomeSpread
                        ? RhizomeSpread.EffectiveSeedDispersalChance(req)
                        : req.UsesBerryColonySpread
                            ? BerryColonySpread.EffectiveSeedDispersalChance(req)
                            : req.UsesShoreSedgeMatSpread
                                ? ShoreSedgeMatSpread.EffectiveSeedDispersalChance(req)
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
            AppendRecentHistory(api, pos, cfg, lines);

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
            AppendRecentHistory(api, anchorPos, cfg, lines);

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

            if (WildTreeEcology.TryGet(wood, out WildTreeEcology.Profile treeProfile))
            {
                string roleKey = treeProfile.SeralRole switch
                {
                    TreeSeralRole.Pioneer => "ecosystemflora:inspect-line-tree-seral-pioneer",
                    TreeSeralRole.Mid => "ecosystemflora:inspect-line-tree-seral-mid",
                    _ => "ecosystemflora:inspect-line-tree-seral-climax",
                };
                AddInspectLine(lines, roleKey);
            }

            int sizePct = TreeGrowthTargets.SizeIndexPercent(
                metrics.TrunkHeight,
                metrics.CrownRadius,
                profile);

            AddInspectLine(
                lines,
                "ecosystemflora:inspect-line-tree-age",
                entry.TreeAgeYears.ToString(),
                profile.SenescenceAgeYears.ToString());

            if (TreeSenescence.IsPastHorizon(entry.TreeAgeYears, profile, EcosystemConfig.Loaded))
            {
                switch (entry.TreeSenescencePhase)
                {
                    case TreeSenescencePhase.Declining:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-tree-senescence-declining");
                        break;
                    case TreeSenescencePhase.DeadCrown:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-tree-senescence-dead-crown");
                        break;
                    case TreeSenescencePhase.Snag:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-tree-senescence-snag");
                        break;
                    default:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-tree-senescent");
                        break;
                }
            }
            else if (entry.TreeAgeYears + 1 >= profile.SenescenceAgeYears
                && EcosystemConfig.Loaded.EnableTreeSenescence)
            {
                AddInspectLine(lines, "ecosystemflora:inspect-line-tree-senescence-soon");
            }

            AddInspectLine(
                lines,
                "ecosystemflora:inspect-line-tree-size",
                metrics.TrunkHeight.ToString(),
                metrics.CrownRadius.ToString(),
                sizePct.ToString(),
                profile.ReferenceTrunkHeight.ToString(),
                profile.ReferenceCrownRadius.ToString());
        }

        static void AppendFerntreeAgingInspect(
            ICoreAPI api,
            ReproducerEntry entry,
            List<InspectLineLite> lines)
        {
            if (entry?.Requirements?.Habitat != EcologyHabitat.Ferntree) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableFerntreeEcology || !cfg.EnableTreeAging || api?.World?.BlockAccessor == null) return;

            WildFerntreeEcology.Profile profile = WildFerntreeEcology.Resolve();
            int segments = FerntreeStructure.MeasureTrunkSegmentCount(api.World.BlockAccessor, entry.Origin);
            BlockPos topPos = FerntreeStructure.FindTopPos(api.World.BlockAccessor, entry.Origin);
            FerntreeTopMaturity maturity = FerntreeStructure.ParseTopMaturity(
                api.World.BlockAccessor.GetBlock(topPos));

            AddInspectLine(
                lines,
                "ecosystemflora:inspect-line-ferntree-age",
                entry.TreeAgeYears.ToString(),
                profile.SenescenceAgeYears.ToString());

            AddInspectLine(
                lines,
                "ecosystemflora:inspect-line-ferntree-size",
                segments.ToString(),
                maturity.ToString().ToLowerInvariant());

            if (FerntreeSenescence.IsPastHorizon(entry.TreeAgeYears, profile, cfg))
            {
                switch (entry.TreeSenescencePhase)
                {
                    case TreeSenescencePhase.Declining:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-ferntree-senescence-foliage");
                        break;
                    case TreeSenescencePhase.DeadCrown:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-ferntree-senescence-top");
                        break;
                    case TreeSenescencePhase.Snag:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-ferntree-senescence-snag");
                        break;
                    default:
                        AddInspectLine(lines, "ecosystemflora:inspect-line-ferntree-senescence");
                        break;
                }
            }
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
