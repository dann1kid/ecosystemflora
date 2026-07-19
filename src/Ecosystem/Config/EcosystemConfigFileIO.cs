using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.Config
{
    /// <summary>Read/write category JSON slices of <see cref="EcosystemConfig"/>.</summary>
    public static class EcosystemConfigFileIO
    {
        public static Dictionary<string, object> ExtractCategory(EcosystemConfig cfg, string category)
        {
            var dict = new Dictionary<string, object>(StringComparer.Ordinal);
            if (cfg == null || string.IsNullOrEmpty(category)) return dict;

            foreach (EcosystemConfigFieldDescriptor field in EcosystemConfigSchema.GetCategoryFields(category))
            {
                object value = field.GetValue(cfg);
                if (value != null) dict[field.Name] = value;
            }

            return dict;
        }

        public static Dictionary<string, object> ExtractMeta(EcosystemConfig cfg)
        {
            var dict = new Dictionary<string, object>(StringComparer.Ordinal);
            if (cfg == null) return dict;

            dict[nameof(EcosystemConfig.SetupWizardCompleted)] = cfg.SetupWizardCompleted;
            dict[nameof(EcosystemConfig.LastAutoTuneTier)] = cfg.LastAutoTuneTier ?? "";
            dict[nameof(EcosystemConfig.LastAutoTuneOpsPerMs)] = cfg.LastAutoTuneOpsPerMs;
            dict[nameof(EcosystemConfig.LastAutoTuneElapsedMs)] = cfg.LastAutoTuneElapsedMs;
            dict[nameof(EcosystemConfig.LastAutoTuneUtc)] = cfg.LastAutoTuneUtc ?? "";
            return dict;
        }

        public static void ApplyCategory(EcosystemConfig cfg, Dictionary<string, object> values)
        {
            if (cfg == null || values == null) return;

            foreach (KeyValuePair<string, object> pair in values)
            {
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(pair.Key);
                if (field == null)
                {
                    ApplyMetaKey(cfg, pair.Key, pair.Value);
                    continue;
                }

                object coerced = Coerce(pair.Value, field.Kind);
                if (coerced != null) field.SetValue(cfg, coerced);
            }
        }

        public static void ApplyMeta(EcosystemConfig cfg, Dictionary<string, object> values)
        {
            if (cfg == null || values == null) return;
            foreach (KeyValuePair<string, object> pair in values)
            {
                ApplyMetaKey(cfg, pair.Key, pair.Value);
            }
        }

        static void ApplyMetaKey(EcosystemConfig cfg, string key, object value)
        {
            if (cfg == null || string.IsNullOrEmpty(key) || value == null) return;

            if (KeysEqual(key, nameof(EcosystemConfig.SetupWizardCompleted)))
            {
                cfg.SetupWizardCompleted = ToBool(value);
                return;
            }

            if (KeysEqual(key, nameof(EcosystemConfig.LastAutoTuneTier)))
            {
                cfg.LastAutoTuneTier = value.ToString() ?? "";
                return;
            }

            if (KeysEqual(key, nameof(EcosystemConfig.LastAutoTuneOpsPerMs)))
            {
                cfg.LastAutoTuneOpsPerMs = ToDouble(value);
                return;
            }

            if (KeysEqual(key, nameof(EcosystemConfig.LastAutoTuneElapsedMs)))
            {
                cfg.LastAutoTuneElapsedMs = ToInt(value);
                return;
            }

            if (KeysEqual(key, nameof(EcosystemConfig.LastAutoTuneUtc)))
            {
                cfg.LastAutoTuneUtc = value.ToString() ?? "";
            }
        }

        static bool KeysEqual(string a, string b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        public static void WriteJsonDict(string path, Dictionary<string, object> values)
        {
            if (string.IsNullOrEmpty(path)) return;
            string dir = Path.GetDirectoryName(path);
            EcosystemConfigPaths.EnsureDirectory(dir);

            byte[] bytes = JsonUtil.ToBytes(values ?? new Dictionary<string, object>());
            File.WriteAllBytes(path, bytes);
        }

        public static Dictionary<string, object> ReadJsonDict(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0) return null;

            try
            {
                return JsonUtil.FromBytes<Dictionary<string, object>>(bytes);
            }
            catch
            {
                // Fallback: full EcosystemConfig fragment (single-file legacy).
                try
                {
                    EcosystemConfig fragment = JsonUtil.FromBytes<EcosystemConfig>(bytes);
                    if (fragment == null) return null;
                    var dict = new Dictionary<string, object>(StringComparer.Ordinal);
                    foreach (EcosystemConfigFieldDescriptor field in EcosystemConfigSchema.Fields)
                    {
                        object value = field.GetValue(fragment);
                        if (value != null) dict[field.Name] = value;
                    }

                    dict[nameof(EcosystemConfig.SetupWizardCompleted)] = fragment.SetupWizardCompleted;
                    dict[nameof(EcosystemConfig.LastAutoTuneTier)] = fragment.LastAutoTuneTier ?? "";
                    dict[nameof(EcosystemConfig.LastAutoTuneOpsPerMs)] = fragment.LastAutoTuneOpsPerMs;
                    dict[nameof(EcosystemConfig.LastAutoTuneElapsedMs)] = fragment.LastAutoTuneElapsedMs;
                    dict[nameof(EcosystemConfig.LastAutoTuneUtc)] = fragment.LastAutoTuneUtc ?? "";
                    return dict;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static void WriteFullConfig(string path, EcosystemConfig cfg)
        {
            if (string.IsNullOrEmpty(path) || cfg == null) return;
            string dir = Path.GetDirectoryName(path);
            EcosystemConfigPaths.EnsureDirectory(dir);
            byte[] bytes = Encoding.UTF8.GetBytes(EcosystemConfigCopier.ToJson(cfg));
            File.WriteAllBytes(path, bytes);
        }

        public static EcosystemConfig ReadFullConfig(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            string json = File.ReadAllText(path, Encoding.UTF8);
            return EcosystemConfigCopier.FromJson(json);
        }

        static object Coerce(object value, ConfigFieldKind kind)
        {
            if (value == null) return null;

            switch (kind)
            {
                case ConfigFieldKind.Boolean:
                    return ToBool(value);
                case ConfigFieldKind.Integer:
                    return ToInt(value);
                case ConfigFieldKind.Float:
                    return (float)ToDouble(value);
                case ConfigFieldKind.Double:
                    return ToDouble(value);
                case ConfigFieldKind.String:
                    return value.ToString();
                default:
                    return value;
            }
        }

        static bool ToBool(object value)
        {
            if (value is bool b) return b;
            if (value is long l) return l != 0;
            if (value is int i) return i != 0;
            if (value is double d) return Math.Abs(d) > 0.0001;
            if (value is float f) return Math.Abs(f) > 0.0001f;
            if (value == null) return false;

            string s = value.ToString();
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (bool.TryParse(s, out bool parsed)) return parsed;
            if (s == "1") return true;
            if (s == "0") return false;

            // Newtonsoft JValue / similar
            try
            {
                var prop = value.GetType().GetProperty("Value");
                if (prop != null)
                {
                    object inner = prop.GetValue(value);
                    if (inner != null && !ReferenceEquals(inner, value))
                    {
                        return ToBool(inner);
                    }
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        static int ToInt(object value)
        {
            switch (value)
            {
                case int i: return i;
                case long l: return (int)l;
                case double d: return (int)d;
                case float f: return (int)f;
                default:
                    return int.TryParse(
                        value.ToString(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int parsed)
                        ? parsed
                        : 0;
            }
        }

        static double ToDouble(object value)
        {
            switch (value)
            {
                case double d: return d;
                case float f: return f;
                case int i: return i;
                case long l: return l;
                default:
                    return double.TryParse(
                        value.ToString(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double parsed)
                        ? parsed
                        : 0;
            }
        }
    }
}
