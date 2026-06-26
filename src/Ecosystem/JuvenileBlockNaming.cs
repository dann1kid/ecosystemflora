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
        public const string Suffix = "-free";

        public static AssetLocation CodeForSpecies(string prefix, string species)
        {
            if (string.IsNullOrEmpty(species)) return null;
            return new AssetLocation(Domain, prefix + species + Suffix);
        }

        public static string SpeciesFromCode(string prefix, AssetLocation code)
        {
            if (code == null) return null;
            if (!Domain.Equals(code.Domain, System.StringComparison.OrdinalIgnoreCase)) return null;
            string path = code.Path ?? "";
            if (!path.StartsWith(prefix) || !path.EndsWith(Suffix)) return null;
            string inner = path.Substring(prefix.Length, path.Length - prefix.Length - Suffix.Length);
            return inner.Length > 0 ? inner : null;
        }
    }
}
