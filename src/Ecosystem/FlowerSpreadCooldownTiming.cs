namespace WildFarming.Ecosystem
{
    /// <summary>When post-spread cooldown runs relative to placement (testable policy).</summary>
    internal static class FlowerSpreadCooldownTiming
    {
        /// <summary>
        /// True when cooldown should wait for <see cref="EcosystemSystem.OnSpreadPlaced"/>
        /// or a completed background solve with no winners.
        /// </summary>
        public static bool ShouldDeferCooldownToPlacement(bool backgroundQueued, int placementOrEnqueueCount) =>
            backgroundQueued || placementOrEnqueueCount > 0;
    }
}
