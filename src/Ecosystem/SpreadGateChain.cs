using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>A single pre-spawn condition that can veto a reproduce attempt.</summary>
    internal interface ISpreadGate
    {
        bool BlocksSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg);
    }

    /// <summary>Tree/ferntree senescence suppresses spread once aged past its horizon.</summary>
    internal sealed class SenescenceGate : ISpreadGate
    {
        public bool BlocksSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            return TreeSenescence.SuppressesSpread(entry, cfg);
        }
    }

    /// <summary>
    /// Young wild trees should not spread immediately. This primarily targets the mod's log-grown seedlings;
    /// worldgen-sized trees register at age 0 and are allowed to spread once large enough.
    /// </summary>
    internal sealed class YoungTreeSpreadGate : ISpreadGate
    {
        public bool BlocksSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (cfg == null || entry?.Requirements == null) return false;
            if (entry.Requirements.Habitat != EcologyHabitat.TerrestrialTree) return false;

            return !TreeSpreadMaturity.AllowsSpread(api, entry, cfg);
        }
    }

    /// <summary>Flowers with phenology only spread while in bloom.</summary>
    internal sealed class PhenologyGate : ISpreadGate
    {
        public bool BlocksSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            return FlowerPhenology.UsesPhenology(cfg, entry.Requirements) && !FlowerPhenology.CanSpread(entry);
        }
    }

    /// <summary>Ferns only spread during active phenology/sporulation windows.</summary>
    internal sealed class FernSpreadGate : ISpreadGate
    {
        public bool BlocksSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (FernPhenology.UsesPhenology(cfg, entry.Requirements))
            {
                return !FernPhenology.CanSpread(api, entry, cfg);
            }

            return WildFernSpread.UsesSporulationGate(cfg, entry.Requirements)
                && !WildFernSpread.CanSpread(api, entry, cfg);
        }
    }

    /// <summary>Tallgrass phenology suppresses spread in dormant/dieback.</summary>
    internal sealed class TallgrassPhenologyGate : ISpreadGate
    {
        public bool BlocksSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            return TallgrassPhenology.UsesPhenology(cfg, entry.Requirements)
                && !TallgrassPhenology.CanSpread(entry, cfg);
        }
    }

    /// <summary>
    /// Ordered set of pre-spawn gates evaluated once. Previously the phenology + sporulation checks
    /// were duplicated in both the spread tick callback and <c>TrySpawnOffspring</c>; senescence sat
    /// only in the latter. Evaluating the shared chain at both sites is behavior-preserving: the gates
    /// are side-effect free, and senescence already short-circuited the same entries with no spawn.
    ///
    /// These are <em>position-independent</em> gates: each decides purely from the entry + config, so it
    /// can run on <c>entry.Origin</c> before the reproduce anchor is resolved. The symbiosis check is the
    /// one veto that does not belong here — it needs the resolved spawn anchor and records a failed
    /// attempt when it blocks, so it lives as a spawn-site gate in <c>EcosystemSystem.SpawnBlockedBySymbiosis</c>.
    /// </summary>
    internal sealed class SpreadGateChain
    {
        public static readonly SpreadGateChain PreSpawn = new SpreadGateChain(
            new SenescenceGate(),
            new YoungTreeSpreadGate(),
            new PhenologyGate(),
            new FernSpreadGate(),
            new TallgrassPhenologyGate());

        readonly ISpreadGate[] gates;

        SpreadGateChain(params ISpreadGate[] gates)
        {
            this.gates = gates;
        }

        public bool BlocksSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null) return false;

            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i].BlocksSpread(api, entry, cfg)) return true;
            }

            return false;
        }
    }
}
