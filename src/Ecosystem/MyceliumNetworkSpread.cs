using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Orthogonal mycelium mat edge spread (one ground anchor per step).</summary>
    internal static class MyceliumNetworkSpread
    {
        static readonly int[][] OrthogonalDirs = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };

        readonly struct Candidate
        {
            public BlockPos GroundPos { get; }
            public float Fitness { get; }
            public bool Displacing { get; }

            public Candidate(BlockPos groundPos, float fitness, bool displacing)
            {
                GroundPos = groundPos;
                Fitness = fitness;
                Displacing = displacing;
            }
        }

        static readonly List<Candidate> scratchCandidates = new List<Candidate>();

        public static bool IsNetworkFrontier(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos anchorPos,
            MyceliumNiche spreadingNiche)
        {
            if (api == null || acc == null || anchorPos == null) return false;

            for (int i = 0; i < OrthogonalDirs.Length; i++)
            {
                BlockPos neighbor = anchorPos.AddCopy(OrthogonalDirs[i][0], 0, OrthogonalDirs[i][1]);
                if (!MyceliumPlacement.CanSpreadInto(api, neighbor, spreadingNiche, out _)) continue;

                if (!WildSoilGroundRules.HasActiveMycelium(acc, neighbor)) return true;

                BlockEntity be = acc.GetBlockEntity(neighbor);
                if (MyceliumAnchorReader.TryReadMushroomCode(be, out AssetLocation incumbentCode)
                    && MyceliumEcology.TryBuildRequirements(incumbentCode, acc.GetBlock(neighbor), out PlantRequirements incumbentReq)
                    && incumbentReq.Species != null)
                {
                    if (EcosystemSystem.Instance != null
                        && EcosystemSystem.Instance.TryGetReproducer(anchorPos, out ReproducerEntry source)
                        && source.Requirements != null
                        && source.Requirements.Species != incumbentReq.Species)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TrySpread(EcosystemSystem eco, ReproducerEntry entry, bool logFailures)
        {
            if (eco == null || entry?.Requirements == null || entry.Origin == null) return false;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableMyceliumEcology || !cfg.EnableMyceliumNetworkSpread) return false;

            ICoreAPI api = eco.ServerApi;
            if (api == null) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            if (!WildSoilGroundRules.HasActiveMycelium(acc, entry.Origin)) return false;

            Block anchorBlock = acc.GetBlock(entry.Origin);
            MyceliumNiche niche = MyceliumEcology.GetNicheForRequirements(entry.Requirements, anchorBlock);
            if (niche == MyceliumNiche.TrunkPolypore) return false;

            MyceliumNiche spreadingNiche = MyceliumEcology.GetNicheForRequirements(
                entry.Requirements,
                anchorBlock);

            if (!IsNetworkFrontier(api, acc, entry.Origin, spreadingNiche)) return false;

            float chance = MyceliumSpreadTiming.EffectiveChance(cfg, entry.Requirements);
            if (api.World.Rand.NextDouble() > chance) return false;

            scratchCandidates.Clear();
            CollectCandidates(api, acc, entry, cfg, spreadingNiche);

            if (scratchCandidates.Count == 0)
            {
                if (logFailures)
                {
                    api.Logger.Notification(
                        "[ecosystemflora] mycelium spread {0} at {1}: no candidates",
                        entry.Requirements.Species,
                        entry.Origin);
                }

                return false;
            }

            Candidate chosen = scratchCandidates[api.World.Rand.Next(scratchCandidates.Count)];
            if (chosen.Fitness < cfg.MyceliumSpreadMinFitness) return false;

            AssetLocation mushroomCode = entry.MatureBlockCode ?? entry.JuvenileBlockCode;
            if (mushroomCode == null) return false;

            if (chosen.Displacing)
            {
                eco.RemoveMyceliumAnchor(chosen.GroundPos, "displaced");
            }

            if (!MyceliumAnchorSpawner.TrySpawnAnchor(api, chosen.GroundPos, mushroomCode, out string fail))
            {
                if (logFailures)
                {
                    api.Logger.Notification(
                        "[ecosystemflora] mycelium spread fail {0} -> {1}: {2}",
                        entry.Origin,
                        chosen.GroundPos,
                        fail ?? "?");
                }

                return false;
            }

            if (MyceliumEcology.TryBuildRequirements(mushroomCode, acc.GetBlock(chosen.GroundPos), out PlantRequirements offspringReq))
            {
                eco.RegisterMyceliumAnchor(chosen.GroundPos, mushroomCode, offspringReq);
            }

            eco.InvalidateEnvironmentAround(chosen.GroundPos);

            if (logFailures)
            {
                api.Logger.Notification(
                    "[ecosystemflora] mycelium {0} {1} -> {2}",
                    chosen.Displacing ? "displaced" : "spread",
                    entry.Requirements.Species,
                    chosen.GroundPos);
            }

            return true;
        }

        static void CollectCandidates(
            ICoreAPI api,
            IBlockAccessor acc,
            ReproducerEntry entry,
            EcosystemConfig cfg,
            MyceliumNiche spreadingNiche)
        {
            PlantRequirements challenger = entry.Requirements;
            BlockPos origin = entry.Origin;

            for (int i = 0; i < OrthogonalDirs.Length; i++)
            {
                BlockPos target = origin.AddCopy(OrthogonalDirs[i][0], 0, OrthogonalDirs[i][1]);
                if (target.Equals(origin)) continue;

                if (WildSoilGroundRules.HasActiveMycelium(acc, target))
                {
                    TryAddDisplacementCandidate(api, acc, target, challenger, cfg);
                    continue;
                }

                if (!MyceliumPlacement.CanSpreadInto(api, target, spreadingNiche, out _)) continue;

                float fitness = MyceliumSpreadFitness.Score(api, target, challenger);
                if (fitness <= 0f) continue;

                scratchCandidates.Add(new Candidate(target.Copy(), fitness, displacing: false));
            }
        }

        static bool TryAddDisplacementCandidate(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos target,
            PlantRequirements challenger,
            EcosystemConfig cfg)
        {
            if (!cfg.UseCellDisplacement) return false;

            BlockEntity be = acc.GetBlockEntity(target);
            if (!MyceliumAnchorReader.TryReadMushroomCode(be, out AssetLocation incumbentCode)) return false;

            if (!MyceliumEcology.TryBuildRequirements(incumbentCode, acc.GetBlock(target), out PlantRequirements incumbentReq)) return false;
            if (incumbentReq.Species == challenger.Species) return false;

            float challengerScore = MyceliumSpreadFitness.Score(api, target, challenger);
            float incumbentScore = MyceliumSpreadFitness.Score(api, target, incumbentReq);
            if (challengerScore <= 0f) return false;

            float margin = cfg.DisplacementHoldMargin > 0f ? cfg.DisplacementHoldMargin : 1.18f;
            if (challengerScore < incumbentScore * margin) return false;

            scratchCandidates.Add(new Candidate(target.Copy(), challengerScore, displacing: true));
            return true;
        }
    }
}
