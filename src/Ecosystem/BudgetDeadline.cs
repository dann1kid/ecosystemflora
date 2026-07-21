using System.Diagnostics;

namespace WildFarming.Ecosystem
{
    /// <summary>Stopwatch-based wall-clock deadline helpers for tick budgets.</summary>
    internal static class BudgetDeadline
    {
        /// <summary>
        /// Builds a deadline timestamp. Returns 0 when <paramref name="budgetMs"/> is &lt;= 0 (unlimited).
        /// </summary>
        public static long FromBudgetMs(int budgetMs, long startTimestamp = 0)
        {
            if (budgetMs <= 0) return 0;
            long start = startTimestamp != 0 ? startTimestamp : Stopwatch.GetTimestamp();
            return start + budgetMs * Stopwatch.Frequency / 1000;
        }

        /// <summary>True when a non-zero deadline has passed.</summary>
        public static bool IsExpired(long deadlineTimestamp)
        {
            if (deadlineTimestamp <= 0) return false;
            return Stopwatch.GetTimestamp() >= deadlineTimestamp;
        }
    }
}
