using System;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// One pass over <see cref="IWorldAccessor.Blocks"/> for all third-party ecology injectors
    /// (avoids five full block-table scans at asset finalize).
    /// </summary>
    internal static class ThirdPartyEcologyBootstrapPass
    {
        public static void ApplyAll(ICoreAPI api)
        {
            // Always inject attrs at finalize. Participation is gated at runtime by
            // EnableThirdPartyParticipants so world config can enable after template-off.
            if (api == null || api.Side != EnumAppSide.Server) return;

            int fruit = 0, fruitFail = 0;
            int vine = 0, vineFail = 0;
            int fruitWorldgen = 0, fruitWorldgenFail = 0;
            int tree = 0, treeFail = 0;
            int floral = 0, floralFail = 0;

            foreach (Block block in api.World.Blocks)
            {
                if (block?.Code == null || block.Id == 0) continue;
                if (PlantCodeHelper.HasDeclaredEcologyParticipant(block)) continue;

                string domain = block.Code.Domain;
                if (string.Equals(domain, "wildcraftfruit", StringComparison.Ordinal))
                {
                    if (WildcraftFruitEcologyBootstrap.TryInject(block, out bool fail))
                    {
                        fruit++;
                        if (fail) fruitFail++;
                        continue;
                    }

                    if (WildcraftFruitFruitingVineEcologyBootstrap.TryInject(block, out fail))
                    {
                        vine++;
                        if (fail) vineFail++;
                        continue;
                    }

                    if (WildcraftFruitWorldgenEcologyBootstrap.TryInject(block, out fail))
                    {
                        fruitWorldgen++;
                        if (fail) fruitWorldgenFail++;
                    }

                    continue;
                }

                if (string.Equals(domain, "wildcrafttree", StringComparison.Ordinal))
                {
                    if (WildcraftTreeEcologyBootstrap.TryInject(block, out bool fail))
                    {
                        tree++;
                        if (fail) treeFail++;
                    }

                    continue;
                }

                if (domain.StartsWith("floralzones", StringComparison.Ordinal))
                {
                    if (FloralZonesEcologyBootstrap.TryInject(block, out bool fail))
                    {
                        floral++;
                        if (fail) floralFail++;
                    }
                }
            }

            LogInject(api, "Wildcraft fruit ecology attrs", fruit, fruitFail, "path rules still apply");
            LogInject(api, "Wildcraft fruitvine climate attrs", vine, vineFail, "spread disabled");
            LogInject(
                api,
                "WildcraftFruit worldgen ecology attrs",
                fruitWorldgen,
                fruitWorldgenFail,
                "berry bushes use dedicated bootstrap");
            LogInject(api, "Wildcraft tree ecology attrs", tree, treeFail, "ok");
            LogInject(api, "Floral Zones ecology attrs", floral, floralFail, "path rules still apply");
        }

        static void LogInject(ICoreAPI api, string label, int injected, int mergeFailed, string note)
        {
            if (injected <= 0) return;
            if (note == "ok")
            {
                api.Logger.Notification(
                    "[ecosystemflora] {0} injected on {1} blocktypes ({2} merge fallbacks)",
                    label,
                    injected,
                    mergeFailed);
                return;
            }

            api.Logger.Notification(
                "[ecosystemflora] {0} injected on {1} blocktypes ({2} merge fallbacks; {3})",
                label,
                injected,
                mergeFailed,
                note);
        }
    }
}
