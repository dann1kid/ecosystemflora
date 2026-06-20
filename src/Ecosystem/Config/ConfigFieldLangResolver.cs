using Vintagestory.API.Config;

namespace WildFarming.Ecosystem.Config
{
    /// <summary>Resolves config UI title/description from lang files, with builder fallback.</summary>
    public static class ConfigFieldLangResolver
    {
        public static string GetTitle(EcosystemConfigFieldDescriptor field)
        {
            if (field == null) return string.Empty;

            string key = "ecosystemflora:config-field-" + field.Name;
            if (Lang.HasTranslation(key, findWildcarded: false, logErrors: false))
            {
                return Lang.Get(key);
            }

            ConfigFieldLangText text = ConfigFieldLangBuilder.Build(field);
            return IsRussian() ? text.TitleRu : text.TitleEn;
        }

        public static string GetDescription(EcosystemConfigFieldDescriptor field)
        {
            if (field == null) return string.Empty;

            string key = "ecosystemflora:config-field-" + field.Name + "-desc";
            if (Lang.HasTranslation(key, findWildcarded: false, logErrors: false))
            {
                return Lang.Get(key);
            }

            ConfigFieldLangText text = ConfigFieldLangBuilder.Build(field);
            return IsRussian() ? text.DescRu : text.DescEn;
        }

        static bool IsRussian() =>
            string.Equals(Lang.CurrentLocale, "ru", System.StringComparison.OrdinalIgnoreCase);
    }
}
