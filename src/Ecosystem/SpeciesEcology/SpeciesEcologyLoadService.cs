using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>Loads or reloads merged species CSV registries from disk.</summary>
    internal static class SpeciesEcologyLoadService
    {
        public static string ResolveModRoot() =>
            Path.GetDirectoryName(typeof(SpeciesEcologyLoadService).Assembly.Location);

        public static void LoadAll(ICoreAPI api, string modRoot, bool syncUserFiles)
        {
            SpeciesEcologyRegistry.TryLoadFromDisk(api, modRoot, syncUserFiles);
            SpeciesSeasonRegistry.TryLoadFromDisk(api, modRoot, syncUserFiles);
        }

        public static bool TryReload(ICoreAPI api, out int ecologyCount, out int seasonCount)
        {
            ecologyCount = 0;
            seasonCount = 0;
            if (api == null) return false;

            string modRoot = ResolveModRoot();
            bool syncUserFiles = api.Side == EnumAppSide.Server;
            LoadAll(api, modRoot, syncUserFiles);

            ecologyCount = CountLoadedEcology();
            seasonCount = CountLoadedSeason();
            return SpeciesEcologyRegistry.IsLoaded && SpeciesSeasonRegistry.IsLoaded;
        }

        static int CountLoadedEcology()
        {
            int count = 0;
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            for (int i = 0; i < catalog.Count; i++)
            {
                if (SpeciesEcologyRegistry.TryGet(catalog[i].Species, out _))
                {
                    count++;
                }
            }

            return count;
        }

        static int CountLoadedSeason()
        {
            int count = 0;
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            for (int i = 0; i < catalog.Count; i++)
            {
                if (SpeciesSeasonRegistry.TryGet(catalog[i].Species, out _))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
