namespace WildFarming.Ecosystem
{
    /// <summary>How a species picks spread target cells.</summary>
    public enum SpreadMode
    {
        /// <summary>Each mature plant searches the normal reproduce radius.</summary>
        Independent = 0,

        /// <summary>Shore reeds: only mat edge, one orthogonal step (rhizome front).</summary>
        RhizomeMat = 1,
    }
}
