using System.Collections.Generic;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>Contract species keys and taxon labels for ecology CSV export.</summary>
    internal static class SpeciesEcologyCatalog
    {
        public readonly struct Entry
        {
            public readonly string Species;
            public readonly string Taxon;

            public Entry(string species, string taxon)
            {
                Species = species;
                Taxon = taxon;
            }
        }

        public static IReadOnlyList<Entry> All()
        {
            var list = new List<Entry>(72);
            Add(list, EcologyFlowerSpecies.All, "flower");
            Add(list, EcologyFernSpecies.All, "fern");
            Add(list, EcologyBerrySpecies.All, "berry");
            Add(list, EcologyTreeSpecies.AllWoods, "tree");
            Add(list, EcologyTallgrassSpecies.All, "tallgrass");
            Add(list, EcologyGrassColonizerSpecies.All, "grass_colonizer");
            Add(list, EcologyShoreSedgeSpecies.All, "shore_sedge");
            Add(list, EcologyDesertSpecies.All, "desert");
            Add(list, AquaticSpecies, "aquatic");
            list.Add(new Entry(EcologyFerntreeSpecies.Ferntree, "ferntree"));
            list.Add(new Entry(WildVineHelper.TemperateSpecies, "vine"));
            list.Add(new Entry(WildVineHelper.TropicalSpecies, "vine"));
            list.Sort((a, b) => string.CompareOrdinal(a.Species, b.Species));
            return list;
        }

        static readonly string[] AquaticSpecies =
        {
            "coopersreed", "tule", "papyrus", "waterlily", "watercrowfoot",
        };

        static void Add(List<Entry> list, IReadOnlyList<string> species, string taxon)
        {
            for (int i = 0; i < species.Count; i++)
            {
                list.Add(new Entry(species[i], taxon));
            }
        }
    }
}
