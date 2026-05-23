using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    public static class EcologyAttributes
    {
        /// <summary>Block participates in wild ecology (capabilities via <see cref="IEcosystemParticipant"/>).</summary>
        public static bool ReproduceEnabled(Block block) => EcosystemParticipant.TryFromBlock(block, out _);

        public static bool CodeMatchesPattern(string codePath, string pattern)
        {
            if (string.IsNullOrEmpty(codePath) || string.IsNullOrEmpty(pattern)) return false;
            if (pattern == "*") return true;

            string[] parts = pattern.Split('*');
            int index = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                int found = codePath.IndexOf(parts[i], index, System.StringComparison.Ordinal);
                if (found < 0) return false;
                index = found + parts[i].Length;
            }

            return true;
        }
    }
}
