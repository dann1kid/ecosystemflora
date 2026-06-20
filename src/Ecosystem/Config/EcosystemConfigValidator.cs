using System;
using System.Collections.Generic;
using System.Globalization;

namespace WildFarming.Ecosystem.Config
{
    public static class EcosystemConfigValidator
    {
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
