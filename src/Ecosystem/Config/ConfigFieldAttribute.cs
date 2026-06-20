using System;

namespace WildFarming.Ecosystem.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigFieldAttribute : Attribute
    {
        public string Category { get; set; }

        public int Order { get; set; } = 100;

        public bool Hide { get; set; }

        public ConfigFieldScope Scope { get; set; } = ConfigFieldScope.Server;

        public double Min { get; set; } = double.NaN;

        public double Max { get; set; } = double.NaN;

        public string[] AllowedValues { get; set; }
    }
}
