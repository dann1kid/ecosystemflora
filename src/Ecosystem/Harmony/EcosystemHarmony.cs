using System;
using HarmonyLib;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.Harmony
{
    internal static class EcosystemHarmony
    {
        const string HarmonyId = "ecosystemflora.mycelium";

        static bool applied;
        static bool transpilerMiss;

        public static void TryApply(ICoreAPI api)
        {
            if (applied || api == null || api.Side != EnumAppSide.Server) return;
            if (!EcosystemConfig.Loaded.EnableMyceliumEcology) return;

            try
            {
                var harmony = new HarmonyLib.Harmony(HarmonyId);
                harmony.CreateClassProcessor(typeof(MyceliumCapGrowthPatches)).Patch();
                applied = true;

                api.Logger.Notification(
                    "[ecosystemflora] Harmony: mycelium cap displacement patch applied (EnableMyceliumCapDisplacement={0})",
                    EcosystemConfig.Loaded.EnableMyceliumCapDisplacement);

                if (transpilerMiss)
                {
                    api.Logger.Warning(
                        "[ecosystemflora] Harmony: mycelium transpiler did not match game IL — cap displacement may be inactive");
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error(
                    "[ecosystemflora] Harmony patch failed ({0}): {1}",
                    HarmonyId,
                    ex);
            }
        }

        internal static void MarkTranspilerMiss(string methodName)
        {
            transpilerMiss = true;
        }
    }
}
