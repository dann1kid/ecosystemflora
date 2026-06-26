using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Last spread attempt snapshot for ecology inspect (I).</summary>
    internal static class SpreadAttemptInspect
    {
        public static MatSpreadCollectMode ResolveCollectMode(PlantRequirements requirements, System.Random rand)
        {
            if (requirements == null || rand == null) return MatSpreadCollectMode.NotApplicable;
            if (requirements.UsesRhizomeSpread)
            {
                return RhizomeSpread.ResolveCollectMode(requirements, rand);
            }

            if (requirements.UsesFernRhizomeSpread)
            {
                return MatSpreadCollectMode.MatEdge;
            }

            if (requirements.UsesSurfaceMatSpread)
            {
                return SurfaceMatSpread.ResolveCollectMode(requirements, rand);
            }

            return MatSpreadCollectMode.NotApplicable;
        }

        public static void Record(
            ICoreAPI api,
            ReproducerEntry entry,
            MatSpreadCollectMode collectMode,
            bool placed,
            string failureReason = null)
        {
            if (entry == null) return;

            entry.LastSpreadCollectMode = collectMode;
            entry.LastSpreadPlaced = placed;
            entry.LastSpreadFailureReason = placed ? null : failureReason;
            if (api?.World?.Calendar != null)
            {
                entry.LastSpreadAttemptAtHours = api.World.Calendar.TotalHours;
            }
        }
    }
}
