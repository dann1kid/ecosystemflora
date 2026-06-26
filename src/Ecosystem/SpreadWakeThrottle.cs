namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Baseline post-spread throttle for spreaders that no maturation policy owns (tallgrass, berries,
    /// reeds, trees, ...). The event-wake path (<see cref="ReproducerRegistry.ClassifyDueReason"/>) lets a
    /// woken entry spread <em>before</em> its calendar <c>NextAttemptHours</c>, gated only by
    /// <c>NextSpawnAllowedAtHours</c>. Flowers/ferns set that cooldown via their policies; a species without
    /// one leaves it at 0, so wake re-fires every reproduce tick and a single clump carpets its surroundings
    /// in seconds (worst for tallgrass: zero spacing + high spread rate). Flooring the spawn cooldown to the
    /// species' own calendar interval makes wake unable to outpace the scheduled cadence.
    /// </summary>
    internal static class SpreadWakeThrottle
    {
        /// <summary>
        /// Extend a parent's <see cref="ReproducerEntry.NextSpawnAllowedAtHours"/> to at least one calendar
        /// interval out. Only ever extends, so an idle plant whose last attempt is older than one interval
        /// stays immediately wake-eligible and still recolonizes promptly.
        /// </summary>
        public static void ApplyCalendarCadenceFloor(ReproducerEntry parent, double nowHours, double intervalHours)
        {
            if (parent == null || intervalHours <= 0) return;

            double earliest = nowHours + intervalHours;
            if (parent.NextSpawnAllowedAtHours < earliest)
            {
                parent.NextSpawnAllowedAtHours = earliest;
            }
        }
    }
}
