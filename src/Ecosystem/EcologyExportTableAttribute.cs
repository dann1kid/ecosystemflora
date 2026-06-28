using System;

namespace WildFarming.Ecosystem
{
    /// <summary>Marks a static C# table used only to seed CSV export / legacy fallback.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    internal sealed class EcologyExportTableAttribute : Attribute
    {
    }
}
