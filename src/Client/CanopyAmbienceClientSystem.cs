using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;

namespace WildFarming.Client
{
    /// <summary>Client-only seasonal particles under tall deciduous canopy.</summary>
    public class CanopyAmbienceClientSystem : ModSystem
    {
        const float MinDensityGate = 0.2f;

        ICoreClientAPI capi;
        long tickListenerId;
        CanopyAmbienceSample lastSample;
        double nextSampleAt;
        double nextMoteAt;
        double nextDriftAt;
        readonly Random rand = new Random();

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            tickListenerId = api.Event.RegisterGameTickListener(OnClientTick, 200);
        }

        public override void Dispose()
        {
            if (capi?.Event != null && tickListenerId != 0)
            {
                capi.Event.UnregisterGameTickListener(tickListenerId);
            }

            tickListenerId = 0;
            capi = null;
            base.Dispose();
        }

        void OnClientTick(float dt)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableSeasonalFoliage || !cfg.EnableCanopyAmbience) return;
            if (capi?.World?.BlockAccessor == null || capi.World.Player?.Entity == null) return;
            if (!capi.World.AmbientParticles) return;

            int particleLevel = capi.Settings.Int["particleLevel"];
            if (particleLevel <= 0) return;

            double now = capi.World.Calendar.ElapsedSeconds;
            if (now >= nextSampleAt)
            {
                nextSampleAt = now + Math.Max(0.5, cfg.CanopyAmbienceSampleIntervalSeconds);
                lastSample = SampleCanopy(cfg);
            }

            if (!lastSample.HasCanopy || lastSample.Density < MinDensityGate) return;

            IGameCalendar cal = capi.World.Calendar;
            float yearProgress = cal.DayOfYearf / cal.DaysPerYear;
            int month = CanopyAmbienceSeasonCurves.MonthFromYearProgress(yearProgress);
            float moteSeason = CanopyAmbienceSeasonCurves.MoteRate(month);
            float driftSeason = CanopyAmbienceSeasonCurves.DriftRate(month);

            EntityPlayer player = capi.World.Player.Entity;
            BlockPos feet = player.Pos.AsBlockPos;
            ClimateCondition climate = capi.World.BlockAccessor.GetClimateAt(
                feet,
                EnumGetClimateMode.NowValues);
            float weather = CanopyAmbienceSeasonCurves.WeatherAttenuation(
                climate?.Rainfall ?? 0f,
                cfg.CanopyAmbienceSuppressInRain);

            float particleScale = particleLevel / 100f;
            moteSeason *= weather * cfg.CanopyAmbienceMoteRate * lastSample.Density * particleScale;
            driftSeason *= weather * cfg.CanopyAmbienceLeafDriftRate * lastSample.Density * particleScale;

            // Autumn leaf drift dominates; keep green motes subtle so colours stay readable.
            if (driftSeason > 0.4f)
            {
                moteSeason *= 0.2f;
            }

            if (moteSeason > 0f && now >= nextMoteAt)
            {
                nextMoteAt = now + JitterInterval(4.0, 8.0, moteSeason);
                SpawnMotes(lastSample, month, CanopyAmbienceWind.Sample());
            }

            if (driftSeason > 0f && now >= nextDriftAt)
            {
                nextDriftAt = now + JitterInterval(1.5, 3.0, driftSeason);
                SpawnDriftBurst(lastSample, CanopyAmbienceWind.Sample());
            }
        }

        CanopyAmbienceSample SampleCanopy(EcosystemConfig cfg)
        {
            EntityPlayer player = capi.World.Player.Entity;
            BlockPos feet = player.Pos.AsBlockPos;
            return CanopyAmbienceSampler.Sample(
                capi.World.BlockAccessor,
                feet.X,
                feet.Y,
                feet.Z,
                cfg.CanopyAmbienceMinHeightBlocks);
        }

        double JitterInterval(double minSeconds, double maxSeconds, float rateScale)
        {
            if (rateScale <= 0f) return double.MaxValue;

            double span = maxSeconds - minSeconds;
            double baseInterval = minSeconds + rand.NextDouble() * span;
            return baseInterval / rateScale;
        }

        void SpawnMotes(CanopyAmbienceSample sample, int month, CanopyAmbienceWind wind)
        {
            int count = 1 + rand.Next(0, 2);
            for (int i = 0; i < count; i++)
            {
                SpawnMoteParticle(sample, month, wind);
            }
        }

        void SpawnDriftBurst(CanopyAmbienceSample sample, CanopyAmbienceWind wind)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            EntityPlayer player = capi.World.Player.Entity;
            BlockPos feet = player.Pos.AsBlockPos;
            int burstCount = 2 + rand.Next(0, 3);
            int viewDistance = capi.Settings.Int["viewDistance"];

            if (!CanopyAmbienceFoliageSpawn.TryPickSpawnPoints(
                    capi.World.BlockAccessor,
                    feet.X,
                    feet.Y,
                    feet.Z,
                    viewDistance,
                    cfg.CanopyAmbienceMinHeightBlocks,
                    burstCount,
                    rand,
                    out FoliageSpawnPoint[] points))
            {
                return;
            }

            for (int i = 0; i < points.Length; i++)
            {
                SpawnDriftParticle(points[i], wind);
            }
        }

        void SpawnMoteParticle(CanopyAmbienceSample sample, int month, CanopyAmbienceWind wind)
        {
            Vec3d pos = RandomMoteSpawnPos(sample);
            int color = CanopyAmbienceSeasonCurves.ResolveMoteColor(month, rand);
            capi.World.SpawnParticles(CanopyAmbienceParticles.CreateMote(pos, color, wind));
        }

        void SpawnDriftParticle(FoliageSpawnPoint spawn, CanopyAmbienceWind wind)
        {
            if (CanopyAmbienceParticles.TrySpawnLeafDrift(
                    capi,
                    spawn,
                    wind,
                    rand,
                    out IParticlePropertiesProvider props))
            {
                capi.World.SpawnParticles(props);
                return;
            }

            int color = CanopyAmbienceSeasonCurves.ResolveDriftColor(spawn.Wood, rand);
            capi.World.SpawnParticles(CanopyAmbienceParticles.CreateDriftFallback(spawn.Pos, color, wind, rand));
        }

        Vec3d RandomMoteSpawnPos(CanopyAmbienceSample sample)
        {
            EntityPlayer player = capi.World.Player.Entity;
            double px = player.Pos.X;
            double py = sample.CanopyY - 1.2 - rand.NextDouble() * 0.8;
            double pz = player.Pos.Z;

            int viewDistance = capi.Settings.Int["viewDistance"];
            double spread = rand.NextDouble() * viewDistance;
            double angle = rand.NextDouble() * Math.PI * 2.0;
            px += Math.Cos(angle) * spread;
            pz += Math.Sin(angle) * spread;

            return new Vec3d(px, py, pz);
        }
    }
}
