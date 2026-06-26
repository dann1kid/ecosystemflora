namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Registry of the spread-maturation policies. Order matters for cooldown application: fern is
    /// tried before flower, matching the original fern-then-flower fallback (a species belongs to at
    /// most one policy, so only one ever applies).
    /// </summary>
    internal static class SpreadMaturationPolicies
    {
        public static readonly SpreadMaturationPolicy[] All =
        {
            WildFernSpread.Policy,
            WildFlowerMaturation.Policy,
        };
    }
}
