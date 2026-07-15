using System;
using System.Collections.Generic;
using System.Globalization;

namespace WildFarming.Ecosystem.Config
{
    public static class EcosystemConfigValidator
    {
        /// <summary>
        /// Clamps numeric fields to schema min/max and maps string enums to the nearest allowed value.
        /// Call before apply/persist so out-of-range UI/JSON values are corrected instead of rejected.
        /// </summary>
        public static int NormalizeInPlace(EcosystemConfig cfg)
        {
            if (cfg == null) return 0;

            int changed = 0;
            foreach (EcosystemConfigFieldDescriptor field in EcosystemConfigSchema.Fields)
            {
                object raw = field.GetValue(cfg);
                if (raw == null)
                {
                    object fallback = DefaultForField(field);
                    if (fallback != null)
                    {
                        field.SetValue(cfg, fallback);
                        changed++;
                    }

                    continue;
                }

                switch (field.Kind)
                {
                    case ConfigFieldKind.Boolean:
                        break;

                    case ConfigFieldKind.String:
                        if (NormalizeString(field, raw.ToString(), out string nextString))
                        {
                            field.SetValue(cfg, nextString);
                            changed++;
                        }

                        break;

                    case ConfigFieldKind.Integer:
                    case ConfigFieldKind.Float:
                    case ConfigFieldKind.Double:
                        if (NormalizeNumber(field, Convert.ToDouble(raw, CultureInfo.InvariantCulture), out double nextNumber))
                        {
                            field.SetValue(cfg, nextNumber);
                            changed++;
                        }

                        break;
                }
            }

            return changed;
        }

        public static bool TryValidate(EcosystemConfig cfg, out string[] errors)
        {
            var list = new List<string>();
            if (cfg == null)
            {
                errors = new[] { "config-null" };
                return false;
            }

            foreach (EcosystemConfigFieldDescriptor field in EcosystemConfigSchema.Fields)
            {
                object raw = field.GetValue(cfg);
                if (raw == null)
                {
                    list.Add(field.Name + ":null");
                    continue;
                }

                switch (field.Kind)
                {
                    case ConfigFieldKind.Boolean:
                        break;

                    case ConfigFieldKind.String:
                        ValidateString(field, raw.ToString(), list);
                        break;

                    case ConfigFieldKind.Integer:
                        ValidateNumber(field, Convert.ToDouble(raw, CultureInfo.InvariantCulture), list, integer: true);
                        break;

                    case ConfigFieldKind.Float:
                    case ConfigFieldKind.Double:
                        ValidateNumber(field, Convert.ToDouble(raw, CultureInfo.InvariantCulture), list, integer: false);
                        break;
                }
            }

            errors = list.ToArray();
            return list.Count == 0;
        }

        static bool NormalizeString(EcosystemConfigFieldDescriptor field, string value, out string next)
        {
            next = value;
            if (field.AllowedValues == null || field.AllowedValues.Length == 0) return false;

            foreach (string allowed in field.AllowedValues)
            {
                if (string.Equals(value, allowed, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(value, allowed, StringComparison.Ordinal)) return false;
                    next = allowed;
                    return true;
                }
            }

            next = NearestAllowed(value, field.AllowedValues);
            return !string.Equals(value, next, StringComparison.Ordinal);
        }

        static bool NormalizeNumber(EcosystemConfigFieldDescriptor field, double value, out double next)
        {
            next = value;
            bool changed = false;

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                next = FallbackNumber(field);
                return true;
            }

            if (field.Kind == ConfigFieldKind.Integer)
            {
                double rounded = Math.Round(value);
                if (Math.Abs(value - rounded) > 0.0001)
                {
                    next = rounded;
                    value = rounded;
                    changed = true;
                }
            }

            if (!double.IsNaN(field.Min) && value < field.Min)
            {
                next = field.Min;
                value = next;
                changed = true;
            }

            if (!double.IsNaN(field.Max) && value > field.Max)
            {
                next = field.Max;
                changed = true;
            }

            return changed;
        }

        static double FallbackNumber(EcosystemConfigFieldDescriptor field)
        {
            object def = DefaultForField(field);
            if (def != null)
            {
                return Convert.ToDouble(def, CultureInfo.InvariantCulture);
            }

            if (!double.IsNaN(field.Min) && !double.IsNaN(field.Max))
            {
                return (field.Min + field.Max) * 0.5;
            }

            if (!double.IsNaN(field.Min)) return field.Min;
            if (!double.IsNaN(field.Max)) return field.Max;
            return 0;
        }

        static object DefaultForField(EcosystemConfigFieldDescriptor field)
        {
            if (field?.Property == null) return null;
            return field.Property.GetValue(new EcosystemConfig());
        }

        static string NearestAllowed(string value, string[] allowed)
        {
            if (allowed == null || allowed.Length == 0) return value ?? string.Empty;
            if (string.IsNullOrEmpty(value)) return allowed[0];

            int bestDistance = int.MaxValue;
            string best = allowed[0];
            for (int i = 0; i < allowed.Length; i++)
            {
                string candidate = allowed[i];
                if (candidate == null) continue;

                if (candidate.StartsWith(value, StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    int prefixBoost = Math.Abs(candidate.Length - value.Length);
                    if (prefixBoost < bestDistance)
                    {
                        bestDistance = prefixBoost;
                        best = candidate;
                    }

                    continue;
                }

                int d = Levenshtein(value, candidate);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = candidate;
                }
            }

            return best;
        }

        static int Levenshtein(string a, string b)
        {
            a ??= string.Empty;
            b ??= string.Empty;
            int n = a.Length;
            int m = b.Length;
            if (n == 0) return m;
            if (m == 0) return n;

            var prev = new int[m + 1];
            var cur = new int[m + 1];
            for (int j = 0; j <= m; j++) prev[j] = j;

            for (int i = 1; i <= n; i++)
            {
                cur[0] = i;
                char ca = char.ToLowerInvariant(a[i - 1]);
                for (int j = 1; j <= m; j++)
                {
                    int cost = ca == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
                    cur[j] = Math.Min(
                        Math.Min(cur[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + cost);
                }

                int[] swap = prev;
                prev = cur;
                cur = swap;
            }

            return prev[m];
        }

        static void ValidateString(EcosystemConfigFieldDescriptor field, string value, List<string> list)
        {
            if (field.AllowedValues == null || field.AllowedValues.Length == 0) return;

            foreach (string allowed in field.AllowedValues)
            {
                if (string.Equals(value, allowed, StringComparison.OrdinalIgnoreCase)) return;
            }

            list.Add(field.Name + ":allowed");
        }

        static void ValidateNumber(EcosystemConfigFieldDescriptor field, double value, List<string> list, bool integer)
        {
            if (integer && Math.Abs(value - Math.Round(value)) > 0.0001)
            {
                list.Add(field.Name + ":integer");
            }

            if (!double.IsNaN(field.Min) && value < field.Min)
            {
                list.Add(field.Name + ":min");
            }

            if (!double.IsNaN(field.Max) && value > field.Max)
            {
                list.Add(field.Name + ":max");
            }
        }
    }
}
