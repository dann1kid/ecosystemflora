using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Survival checks for registered vanilla mycelium anchors (phase 2).</summary>
    internal static class MyceliumStressEvaluator
    {
        public static bool MeetsSurvival(ICoreAPI api, BlockPos anchorPos, PlantRequirements req)
        {
            if (api == null || anchorPos == null || req == null) return false;
            if (req.Habitat != EcologyHabitat.MyceliumAnchor) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            if (!WildSoilGroundRules.HasActiveMycelium(acc, anchorPos)) return false;

            Block anchorBlock = acc.GetBlock(anchorPos);
            MyceliumNiche niche = MyceliumEcology.GetNicheForRequirements(req, anchorBlock);

            if (niche == MyceliumNiche.TrunkPolypore)
            {
                return MyceliumEcology.IsTrunkAnchor(anchorBlock);
            }

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            FloraContextSampler flora = EcosystemSystem.Instance?.FloraContext;
            FloraContext context = flora != null
                ? flora.GetContext(api, anchorPos)
                : FloraContext.Open;
            float forestCover = flora != null
                ? flora.GetLocalForestCover(api, anchorPos)
                : 0f;

            switch (niche)
            {
                case MyceliumNiche.MeadowOpen:
                    if (context == FloraContext.ForestInterior) return false;
                    if (forestCover >= cfg.MyceliumMeadowMaxForestCover) return false;
                    return true;

                case MyceliumNiche.ForestAnyTree:
                case MyceliumNiche.ForestDeciduous:
                case MyceliumNiche.ForestConifer:
                    if (forestCover < cfg.MyceliumForestMinForestCover
                        && context == FloraContext.Open)
                    {
                        return false;
                    }

                    int hostRadius = cfg.MyceliumTreeHostRadius > 0
                        ? cfg.MyceliumTreeHostRadius
                        : 4;
                    MyceliumTreeHostKind hostKind = MyceliumTreeHost.HostKindForNiche(niche);
                    return MyceliumTreeHost.HasHostInRange(acc, anchorPos, hostRadius, hostKind);

                default:
                    return true;
            }
        }

        public static bool MeetsSurvival(ICoreAPI api, ReproducerEntry entry)
        {
            if (entry == null) return false;
            return MeetsSurvival(api, entry.Origin, entry.Requirements);
        }
    }
}
