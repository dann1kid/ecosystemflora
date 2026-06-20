using System;
using System.Reflection;

namespace WildFarming.Ecosystem.Config
{
    public sealed class EcosystemConfigFieldDescriptor
    {
        public PropertyInfo Property { get; init; }

        public string Name { get; init; }

        public ConfigFieldKind Kind { get; init; }

        public string Category { get; init; }

        public int Order { get; init; }

        public ConfigFieldScope Scope { get; init; }

        public double Min { get; init; }

        public double Max { get; init; }

        public string[] AllowedValues { get; init; }

        public bool IsPresetField { get; init; }

        public object GetValue(EcosystemConfig cfg)
        {
            return Property?.GetValue(cfg);
        }

        public void SetValue(EcosystemConfig cfg, object value)
        {
            if (Property == null || cfg == null) return;

            object converted = ConvertValue(value);
            Property.SetValue(cfg, converted);
        }

        object ConvertValue(object value)
        {
            Type target = Property.PropertyType;
            if (value == null) return GetDefault(target);

            if (target == typeof(bool))
            {
                if (value is bool b) return b;
                if (value is string s && bool.TryParse(s, out bool pb)) return pb;
                return Convert.ToBoolean(value);
            }

            if (target == typeof(int))
            {
                if (value is int i) return i;
                if (value is string si && int.TryParse(si, out int pi)) return pi;
                return Convert.ToInt32(Math.Round(Convert.ToDouble(value)));
            }

            if (target == typeof(float))
            {
                if (value is float f) return f;
                if (value is string sf && float.TryParse(sf, out float pf)) return pf;
                return Convert.ToSingle(value);
            }

            if (target == typeof(double))
            {
                if (value is double d) return d;
                if (value is string sd && double.TryParse(sd, out double pd)) return pd;
                return Convert.ToDouble(value);
            }

            if (target == typeof(string))
            {
                return value.ToString();
            }

            return value;
        }

        static object GetDefault(Type type)
        {
            if (type == typeof(bool)) return false;
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(double)) return 0d;
            if (type == typeof(string)) return string.Empty;
            return null;
        }
    }
}
