using System;
using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    internal static class WildcraftFruitFruittreeWorldgen
    {
        internal readonly struct Entry
        {
            public readonly float MinTemp, MaxTemp, MinRain, MaxRain;
            public Entry(float minTemp, float maxTemp, float minRain, float maxRain)
            {
                MinTemp = minTemp; MaxTemp = maxTemp; MinRain = minRain; MaxRain = maxRain;
            }
        }

        static readonly Dictionary<string, Entry> ByType =
            new Dictionary<string, Entry>(StringComparer.Ordinal)
            {
                ["achacha"] = new Entry(20.0f, 30.0f, 0.5f, 0.8f),
                ["almond"] = new Entry(-6.0f, 16.0f, 0.4f, 0.7f),
                ["apricot"] = new Entry(-6.0f, 18.0f, 0.55f, 1.0f),
                ["aronia"] = new Entry(-12.0f, 12.0f, 0.5f, 0.8f),
                ["avocado"] = new Entry(18.0f, 45.0f, 0.6f, 1.0f),
                ["beachalmond"] = new Entry(14.0f, 32.0f, 0.6f, 0.8f),
                ["ber"] = new Entry(14.0f, 26.0f, 0.4f, 0.8f),
                ["bergamot"] = new Entry(24.0f, 38.0f, 0.45f, 0.8f),
                ["biasong"] = new Entry(24.0f, 40.0f, 0.6f, 1.0f),
                ["bitterorange"] = new Entry(23.0f, 33.0f, 0.45f, 0.8f),
                ["blackmulberry"] = new Entry(-8.0f, 14.0f, 0.45f, 0.8f),
                ["bloodorange"] = new Entry(24.0f, 30.0f, 0.55f, 0.8f),
                ["bluequandong"] = new Entry(14.0f, 30.0f, 0.5f, 1.0f),
                ["bunya"] = new Entry(22.0f, 30.0f, 0.4f, 0.7f),
                ["burgundyapple"] = new Entry(14.0f, 24.0f, 0.6f, 1.0f),
                ["burroak"] = new Entry(4.0f, 16.0f, 0.6f, 1.0f),
                ["cashew"] = new Entry(23.0f, 50.0f, 0.45f, 1.0f),
                ["cempedak"] = new Entry(22.0f, 44.0f, 0.65f, 1.0f),
                ["cherryplum"] = new Entry(-8.0f, 12.0f, 0.4f, 0.8f),
                ["chestnut"] = new Entry(-4.0f, 14.0f, 0.5f, 1.0f),
                ["chinaberry"] = new Entry(12.0f, 30.0f, 0.4f, 1.0f),
                ["citron"] = new Entry(16.0f, 36.0f, 0.6f, 1.0f),
                ["cocoa"] = new Entry(23.0f, 50.0f, 0.6f, 1.0f),
                ["commonhackberry"] = new Entry(2.0f, 16.0f, 0.2f, 0.7f),
                ["crabapple"] = new Entry(-12.0f, 16.0f, 0.5f, 1.0f),
                ["damsonplum"] = new Entry(-7.0f, 14.0f, 0.6f, 1.0f),
                ["engkala"] = new Entry(23.0f, 50.0f, 0.6f, 1.0f),
                ["falseorange"] = new Entry(8.0f, 22.0f, 0.4f, 0.8f),
                ["fig"] = new Entry(12.0f, 30.0f, 0.5f, 1.0f),
                ["ginkgo"] = new Entry(6.0f, 16.0f, 0.6f, 1.0f),
                ["grapefruit"] = new Entry(24.0f, 35.0f, 0.5f, 0.9f),
                ["greatroseapple"] = new Entry(23.0f, 48.0f, 0.5f, 1.0f),
                ["greengage"] = new Entry(8.0f, 23.0f, 0.5f, 0.8f),
                ["guajava"] = new Entry(24.0f, 40.0f, 0.6f, 1.0f),
                ["hawthorn"] = new Entry(-14.0f, 10.0f, 0.5f, 0.9f),
                ["hazelnut"] = new Entry(-6.0f, 18.0f, 0.5f, 0.8f),
                ["illawarra"] = new Entry(12.0f, 36.0f, 0.6f, 1.0f),
                ["jackfruit"] = new Entry(20.0f, 38.0f, 0.6f, 1.0f),
                ["jujube"] = new Entry(0.0f, 20.0f, 0.3f, 0.9f),
                ["kasturi"] = new Entry(23.0f, 50.0f, 0.6f, 0.9f),
                ["lemon"] = new Entry(24.0f, 38.0f, 0.5f, 1.0f),
                ["lemonaspen"] = new Entry(22.0f, 30.0f, 0.5f, 0.9f),
                ["lime"] = new Entry(24.0f, 32.0f, 0.6f, 1.0f),
                ["loquat"] = new Entry(6.0f, 20.0f, 0.6f, 1.0f),
                ["macadamia"] = new Entry(24.0f, 50.0f, 0.5f, 1.0f),
                ["makrut"] = new Entry(23.0f, 33.0f, 0.45f, 0.8f),
                ["mandarin"] = new Entry(16.0f, 26.0f, 0.5f, 0.9f),
                ["marang"] = new Entry(22.0f, 44.0f, 0.65f, 1.0f),
                ["mazzard"] = new Entry(8.0f, 22.0f, 0.5f, 1.0f),
                ["nectarine"] = new Entry(6.0f, 14.0f, 0.4f, 1.0f),
                ["pandan"] = new Entry(23.0f, 50.0f, 0.45f, 0.8f),
                ["pinklemon"] = new Entry(24.0f, 30.0f, 0.55f, 0.8f),
                ["pomelo"] = new Entry(24.0f, 35.0f, 0.55f, 0.8f),
                ["pulasan"] = new Entry(22.0f, 50.0f, 0.6f, 1.0f),
                ["purpleplum"] = new Entry(-2.0f, 16.0f, 0.6f, 1.0f),
                ["quince"] = new Entry(-8.0f, 14.0f, 0.5f, 1.0f),
                ["rambutan"] = new Entry(20.0f, 38.0f, 0.6f, 1.0f),
                ["redcherryplum"] = new Entry(2.0f, 12.0f, 0.55f, 0.9f),
                ["redmulberry"] = new Entry(0.0f, 16.0f, 0.45f, 0.8f),
                ["redplum"] = new Entry(0.0f, 18.0f, 0.5f, 0.9f),
                ["redquandong"] = new Entry(23.0f, 40.0f, 0.25f, 0.4f),
                ["roseapple"] = new Entry(18.0f, 32.0f, 0.6f, 1.0f),
                ["rowan"] = new Entry(-16.0f, 8.0f, 0.6f, 0.8f),
                ["sandpear"] = new Entry(-13.0f, 10.0f, 0.5f, 0.9f),
                ["silvernettle"] = new Entry(22.0f, 40.0f, 0.6f, 1.0f),
                ["smoothavocado"] = new Entry(22.0f, 50.0f, 0.45f, 0.9f),
                ["sorb"] = new Entry(-4.0f, 10.0f, 0.6f, 0.9f),
                ["stonepine"] = new Entry(12.0f, 25.0f, 0.35f, 0.6f),
                ["sugarhackberry"] = new Entry(12.0f, 28.0f, 0.6f, 1.0f),
                ["summeroak"] = new Entry(-8.0f, 14.0f, 0.5f, 0.9f),
                ["tangerine"] = new Entry(18.0f, 38.0f, 0.4f, 0.9f),
                ["walnut"] = new Entry(-10.0f, 12.0f, 0.45f, 0.8f),
                ["wani"] = new Entry(18.0f, 35.0f, 0.65f, 1.0f),
                ["waterapple"] = new Entry(22.0f, 44.0f, 0.6f, 1.0f),
                ["whiteoak"] = new Entry(-4.0f, 14.0f, 0.45f, 0.8f),
                ["wildapple"] = new Entry(-7.0f, 18.0f, 0.4f, 1.0f),
                ["yew"] = new Entry(-8.0f, 16.0f, 0.4f, 0.8f),
            };

        public static bool TryGet(string type, out Entry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(type)) return false;
            return ByType.TryGetValue(type, out entry);
        }
    }
}
