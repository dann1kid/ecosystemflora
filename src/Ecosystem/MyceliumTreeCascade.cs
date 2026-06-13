using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Fast mycelium die-off when a supporting tree trunk is removed.</summary>
    internal static class MyceliumTreeCascade
    {
        public static void OnTreeRemoved(ICoreAPI api, BlockPos treePos, Block hostBlock)
        {
            if (api == null || treePos == null || hostBlock == null) return;
            if (!EcosystemConfig.Loaded.EnableMyceliumEcology) return;
            if (!PlantCodeHelper.IsTreeLogGrownBlock(hostBlock)) return;

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null) return;

            int radius = EcosystemConfig.Loaded.SymbiosisCascadeRadius;
            if (radius <= 0) radius = 4;

            IBlockAccessor acc = api.World.BlockAccessor;
            var scan = new BlockPos(treePos.dimension);
            int hostRadius = EcosystemConfig.Loaded.MyceliumTreeHostRadius > 0
                ? EcosystemConfig.Loaded.MyceliumTreeHostRadius
                : 4;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        scan.Set(treePos.X + dx, treePos.Y + dy, treePos.Z + dz);
                        if (scan.Equals(treePos)) continue;

                        if (!eco.TryGetReproducer(scan, out ReproducerEntry entry)) continue;
                        if (entry.Requirements?.Habitat != EcologyHabitat.MyceliumAnchor) continue;

                        Block anchorBlock = acc.GetBlock(scan);
                        MyceliumNiche niche = MyceliumEcology.GetNicheForRequirements(entry.Requirements, anchorBlock);
                        if (niche == MyceliumNiche.MeadowOpen || niche == MyceliumNiche.TrunkPolypore) continue;

                        if (!MyceliumTreeHost.HasHostInRange(
                            acc,
                            scan,
                            hostRadius,
                            MyceliumTreeHost.HostKindForNiche(niche)))
                        {
                            eco.RemoveMyceliumAnchor(scan, "deforestation");
                        }
                    }
                }
            }
        }
    }
}
