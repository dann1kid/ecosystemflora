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

    /// <summary>Flowers with phenology only spread while in bloom.</summary>
    internal sealed class PhenologyGate : ISpreadGate
    {
        public bool BlocksSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            return FlowerPhenology.UsesPhenology(cfg, entry.Requirements) && !FlowerPhenology.CanSpread(entry);
        }
    }

    /// <summary>Ferns only spread during the seasonal sporulation window.</summary>
    internal sealed class SporulationGate : ISpreadGate
    {
        public bool BlocksSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            return WildFernSpread.UsesSporulationGate(cfg, entry.Requirements) && !WildFernSpread.CanSpread(api, entry, cfg);
        }
    }

    /// <summary>
    /// Ordered set of pre-spawn gates evaluated once. Previously the phenology + sporulation checks
    /// were duplicated in both the spread tick callback and <c>TrySpawnOffspring</c>; senescence sat
    /// only in the latter. Evaluating the shared chain at both sites is behavior-preserving: the gates
    /// are side-effect free, and senescence already short-circuited the same entries with no spawn.
    /// </summary>
    internal sealed class SpreadGateChain
    {
        public static readonly SpreadGateChain PreSpawn = new SpreadGateChain(
            new SenescenceGate(),
            new PhenologyGate(),
            new SporulationGate());

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
