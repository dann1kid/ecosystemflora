using System.Text;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.Config
{
    public static class EcosystemConfigCopier
    {
        public static EcosystemConfig Clone(EcosystemConfig source)
        {
            if (source == null) return new EcosystemConfig();

            var clone = new EcosystemConfig();
            CopyFields(source, clone);
            return clone;
        }

        public static void CopyFields(EcosystemConfig source, EcosystemConfig target)
        {
            if (source == null || target == null) return;

            foreach (EcosystemConfigFieldDescriptor field in EcosystemConfigSchema.Fields)
            {
                field.SetValue(target, field.GetValue(source));
            }
        }

        public static void CopyScope(EcosystemConfig source, EcosystemConfig target, ConfigFieldScope scope)
        {
            if (source == null || target == null) return;

            foreach (EcosystemConfigFieldDescriptor field in EcosystemConfigSchema.Fields)
            {
                if (field.Scope != scope) continue;
                field.SetValue(target, field.GetValue(source));
            }
        }

        public static string ToJson(EcosystemConfig cfg)
        {
            byte[] bytes = JsonUtil.ToBytes(cfg);
            return Encoding.UTF8.GetString(bytes);
        }

        public static EcosystemConfig FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new EcosystemConfig();
            return JsonUtil.FromBytes<EcosystemConfig>(Encoding.UTF8.GetBytes(json));
        }
    }
}
