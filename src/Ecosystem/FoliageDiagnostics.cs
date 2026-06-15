using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    /// <summary>One-shot and throttled foliage logs (no VerboseLogging required).</summary>
    internal static class FoliageDiagnostics
    {
        static bool startupLogged;
        static double lastEmptyWarnHours = -1000;

        public static void ResetOnPlayerJoin()
        {
            startupLogged = false;
            lastEmptyWarnHours = -1000;
        }

        public static void LogStartupSummary(ICoreAPI api, FoliageCellScheduler scheduler)
        {
            if (api == null || startupLogged || scheduler == null) return;
            startupLogged = true;

            IGameCalendar cal = api.World?.Calendar;
            BlockPos samplePos = TrySamplePlayerPos(api);
            string seasonHint = BuildSeasonHint(api, samplePos);
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            string mode = cfg.FoliageSyncMode ?? "chunk";
            int indexed = scheduler.GetDiagnosticsIndexedCount();
            int pending = scheduler.PendingSyncChunks;

            if (indexed > 0 || pending > 0 || cfg.EnableSeasonalFoliage)
            {
                api.Logger.Notification(
                    "[ecosystemflora] Foliage ({0}): {1} cells tracked, {2} chunk(s) queued. {3}",
                    mode,
                    indexed,
                    pending,
                    seasonHint);
            }
            else
            {
                api.Logger.Warning(
                    "[ecosystemflora] Foliage: 0 cells near player — no deciduous log-grown/leaves in range, or chunks not ready. {0}",
                    seasonHint);
            }
        }

        public static void MaybeWarnEmptyIndex(ICoreAPI api)
        {
            if (api == null || !EcosystemConfig.Loaded.EnableSeasonalFoliage) return;
            if (FoliageSyncModeHelper.Resolve(EcosystemConfig.Loaded) == FoliageSyncMode.Chunk) return;

            IGameCalendar cal = api.World?.Calendar;
            if (cal == null) return;

            double now = cal.TotalHours;
            if (now - lastEmptyWarnHours < 2) return;

            HashSet<long> chunks = PlayerProximity.BuildActivePlayerChunks(
                api, EcosystemConfig.Loaded.PlayerActivationRadiusBlocks);
            if (chunks.Count == 0) return;

            lastEmptyWarnHours = now;
            api.Logger.Warning(
                "[ecosystemflora] Foliage index still empty near player — re-scanning loaded chunks. {0}",
                BuildSeasonHint(api, TrySamplePlayerPos(api)));
        }

        static BlockPos TrySamplePlayerPos(ICoreAPI api)
        {
            if (api is ICoreServerAPI sapi)
            {
                var players = sapi.World.AllOnlinePlayers;
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i]?.Entity?.Pos != null)
                    {
                        return players[i].Entity.Pos.AsBlockPos;
                    }
                }
            }

            return new BlockPos(0);
        }

        static string BuildSeasonHint(ICoreAPI api, BlockPos pos)
        {
            IGameCalendar cal = api.World?.Calendar;
            if (cal == null) return "Calendar unavailable.";

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            float yearProgress = cal.DayOfYearf / cal.DaysPerYear;
            int month = ((int)(yearProgress * 12f)) % 12 + 1;

            CanopySeasonPhase oakPhase = CanopyEcology.ResolvePhase(api, pos, "oak", out float oakAct);
            CanopySeasonPhase birchPhase = CanopyEcology.ResolvePhase(api, pos, "birch", out float birchAct);

            string oakName = PhaseName(oakPhase, oakAct);
            string birchName = PhaseName(birchPhase, birchAct);

            return string.Format(
                "Game month ~{0} (day {1:F0}/{2}). Oak={3}, birch={4}. Chunk-sync strips/buds on column pass. {5}",
                month,
                cal.DayOfYearf,
                cal.DaysPerYear,
                oakName,
                birchName,
                cfg.RespectLandClaims
                    ? "RespectLandClaims=true — no changes inside player claims."
                    : "RespectLandClaims=false.");
        }

        static string PhaseName(CanopySeasonPhase phase, float activity)
        {
            if (phase == CanopySeasonPhase.Idle || activity <= 0.02f) return "idle";
            return phase == CanopySeasonPhase.Autumn
                ? string.Format("autumn strip ({0:F2})", activity)
                : string.Format("spring bud ({0:F2})", activity);
        }
    }
}
