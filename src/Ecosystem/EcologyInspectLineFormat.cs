using System.Text;
using Vintagestory.API.Config;
using WildFarming.Network;

namespace WildFarming.Ecosystem
{
    internal static class EcologyInspectLineFormat
    {
        internal const string SoilIntPrefix = "I:";

        internal static string FormatInspectLine(InspectLineLite line)
        {
            if (line == null || string.IsNullOrEmpty(line.Key)) return string.Empty;

            if (line.Args == null || line.Args.Length == 0)
            {
                return Lang.Get(line.Key);
            }

            var resolved = new object[line.Args.Length];
            for (int i = 0; i < line.Args.Length; i++)
            {
                resolved[i] = ResolveArg(line.Args[i]);
            }

            return Lang.Get(line.Key, resolved);
        }

        static string ResolveArg(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            if (raw.StartsWith("L:"))
            {
                return Lang.Get(raw.Substring(2));
            }

            if (raw.StartsWith(SoilIntPrefix))
            {
                if (!int.TryParse(raw.Substring(SoilIntPrefix.Length), out int mask))
                {
                    return raw;
                }

                return DescribeSoilKinds((SoilKind)mask);
            }

            return raw;
        }

        /// <summary>Shows a human list of set soil kind flags (player language).</summary>
        internal static string DescribeSoilKinds(SoilKind bits)
        {
            if (bits == SoilKind.None)
            {
                return Lang.Get("ecosystemflora:soilkind-none");
            }

            var sb = new StringBuilder();
            bool first = true;
            AppendIfSet(ref first, sb, bits, SoilKind.HighFert, "ecosystemflora:soilkind-highfert");
            AppendIfSet(ref first, sb, bits, SoilKind.MediumFert, "ecosystemflora:soilkind-mediumfert");
            AppendIfSet(ref first, sb, bits, SoilKind.LowFert, "ecosystemflora:soilkind-lowfert");
            AppendIfSet(ref first, sb, bits, SoilKind.ForestFloor, "ecosystemflora:soilkind-forestfloor");
            AppendIfSet(ref first, sb, bits, SoilKind.Peat, "ecosystemflora:soilkind-peat");
            AppendIfSet(ref first, sb, bits, SoilKind.Sand, "ecosystemflora:soilkind-sand");
            AppendIfSet(ref first, sb, bits, SoilKind.Clay, "ecosystemflora:soilkind-clay");
            AppendIfSet(ref first, sb, bits, SoilKind.Gravel, "ecosystemflora:soilkind-gravel");
            AppendIfSet(ref first, sb, bits, SoilKind.Barren, "ecosystemflora:soilkind-barren");

            return sb.Length == 0 ? bits.ToString() : sb.ToString();
        }

        static void AppendIfSet(ref bool first, StringBuilder sb, SoilKind bits, SoilKind flag, string langKey)
        {
            if ((bits & flag) == 0) return;

            if (!first)
            {
                sb.Append(Lang.Get("ecosystemflora:inspect-list-separator"));
            }

            first = false;
            sb.Append(Lang.Get(langKey));
        }

        /// <summary>Ecosystem species id (e.g. catmint) → display name in current language.</summary>
        internal static string FormatSpeciesEcho(string species)
        {
            if (string.IsNullOrEmpty(species)) return "?";

            string key = "ecosystemflora:species-" + species;
            string localized = Lang.Get(key);
            if (localized != key) return localized;

            if (species.Length == 1)
            {
                return species.ToUpperInvariant();
            }

            return char.ToUpperInvariant(species[0]) + species.Substring(1);
        }
    }
}
