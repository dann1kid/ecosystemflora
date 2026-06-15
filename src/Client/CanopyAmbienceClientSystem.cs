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
        int recentSpawns;
        double recentSpawnWindowStart;
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

            moteSeason *= weather * cfg.CanopyAmbienceMoteRate * lastSample.Density;
            driftSeason *= weather * cfg.CanopyAmbienceLeafDriftRate * lastSample.Density;

            if (moteSeason > 0f && now >= nextMoteAt)
            {
                nextMoteAt = now + JitterInterval(4.0, 8.0, moteSeason);
                SpawnMotes(lastSample, month);
            }

            if (driftSeason > 0f && now >= nextDriftAt)
            {
                nextDriftAt = now + JitterInterval(1.5, 3.0, driftSeason);
                SpawnDrift(lastSample);
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

        bool CanSpawnMore(EcosystemConfig cfg)
        {
            double now = capi.World.Calendar.ElapsedSeconds;
            if (now - recentSpawnWindowStart > 2.0)
            {
                recentSpawnWindowStart = now;
                recentSpawns = 0;
            }

            if (recentSpawns >= cfg.CanopyAmbienceMaxParticles) return false;

            recentSpawns++;
            return true;
        }

        void SpawnMotes(CanopyAmbienceSample sample, int month)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!CanSpawnMore(cfg)) return;

            int count = 1 + rand.Next(0, 2);
            for (int i = 0; i < count; i++)
            {
                if (!CanSpawnMore(cfg)) break;
                SpawnMoteParticle(sample, month);
            }
        }

        void SpawnDrift(CanopyAmbienceSample sample)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!CanSpawnMore(cfg)) return;

            int count = 1 + rand.Next(0, 2);
            for (int i = 0; i < count; i++)
            {
                if (!CanSpawnMore(cfg)) break;
                SpawnDriftParticle(sample);
            }
        }

        void SpawnMoteParticle(CanopyAmbienceSample sample, int month)
        {
            Vec3d pos = RandomSpawnPos(sample);
            int color = CanopyAmbienceSeasonCurves.ResolveMoteColor(month, rand);

            var props = new SimpleParticleProperties(
                1, 1, color,
                pos, pos.Add(0.6, 0.4, 0.6),
                new Vec3f(-0.03f, -0.06f, -0.03f),
                new Vec3f(0.03f, -0.02f, 0.03f),
                lifeLength: 2.2f,
                gravityEffect: 0.04f,
                minSize: 0.06f,
                maxSize: 0.11f,
                model: EnumParticleModel.Quad);

            props.Async = true;
            props.WindAffectednes = 0.25f;
            capi.World.SpawnParticles(props);
        }

        void SpawnDriftParticle(CanopyAmbienceSample sample)
        {
            Vec3d pos = RandomSpawnPos(sample);
            int color = CanopyAmbienceSeasonCurves.ResolveDriftColor(sample.DominantWood, rand);

            var props = new SimpleParticleProperties(
                1, 1, color,
                pos, pos.Add(0.8, 0.5, 0.8),
                new Vec3f(-0.04f, -0.12f, -0.04f),
                new Vec3f(0.04f, -0.06f, 0.04f),
                lifeLength: 3.8f,
                gravityEffect: 0.14f,
                minSize: 0.10f,
                maxSize: 0.18f,
                model: EnumParticleModel.Quad);

            props.Async = true;
            props.WindAffectednes = 0.45f;
            capi.World.SpawnParticles(props);
        }

        Vec3d RandomSpawnPos(CanopyAmbienceSample sample)
        {
            EntityPlayer player = capi.World.Player.Entity;
            double px = player.Pos.X;
            double py = sample.CanopyY - 0.2 - rand.NextDouble() * 1.2;
            double pz = player.Pos.Z;

            double spread = 2.0 + rand.NextDouble() * 2.0;
            double angle = rand.NextDouble() * Math.PI * 2.0;
            px += Math.Cos(angle) * spread;
            pz += Math.Sin(angle) * spread;

            return new Vec3d(px, py, pz);
        }
    }
}
