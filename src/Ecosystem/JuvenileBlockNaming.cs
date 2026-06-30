using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Shared naming for ecosystemflora juvenile spread blocks (<c>juvenile-{kind}-{species}-free</c>).
    /// Flower and fern juvenile helpers differ only by the prefix; the encode/decode logic lives here.
    /// </summary>
    internal static class JuvenileBlockNaming
    {
        public const string Domain = "ecosystemflora";
        public const string FreeSuffix = "-free";
        public const string SnowSuffix = "-snow";

        public static AssetLocation CodeForSpecies(string prefix, string species, bool snow = false)
        {
            if (string.IsNullOrEmpty(species)) return null;
            string suffix = snow ? SnowSuffix : FreeSuffix;
            return new AssetLocation(Domain, prefix + species + suffix);
        }

        public static string SpeciesFromCode(string prefix, AssetLocation code)
        {
            if (code == null) return null;
            if (!Domain.Equals(code.Domain, System.StringComparison.OrdinalIgnoreCase)) return null;
            string path = code.Path ?? "";
            if (!path.StartsWith(prefix)) return null;

            string inner;
            if (path.EndsWith(FreeSuffix))
            {
                inner = path.Substring(prefix.Length, path.Length - prefix.Length - FreeSuffix.Length);
            }
            else if (path.EndsWith(SnowSuffix))
            {
                inner = path.Substring(prefix.Length, path.Length - prefix.Length - SnowSuffix.Length);
            }
            else
            {
                return null;
            }

            return inner.Length > 0 ? inner : null;
        }
    }
}
