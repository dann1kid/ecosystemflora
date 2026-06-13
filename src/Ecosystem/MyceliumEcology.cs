using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Classifies vanilla mushroom types and builds anchor <see cref="PlantRequirements"/>.</summary>
    internal static class MyceliumEcology
    {
        static readonly HashSet<string> MeadowTypes = BuildSet(
            "fieldmushroom", "puffball", "redwinecap");

        static readonly HashSet<string> DeciduousTypes = BuildSet(
            "orangeoakbolete", "devilsbolete", "violetwebcap", "golddropmilkcap",
            "blacktrumpet", "witchhatmushroom", "witchhat", "shiitake", "reishi",
            "beardedtooth", "chickenofthewoods", "tinderhoof", "dryadsaddle");

        static readonly HashSet<string> ConiferTypes = BuildSet(
            "earthball", "saffronmilkcap", "devilstooth", "elfinsaddle",
            "laughingjim", "sickener", "funeralbell", "whiteoyster");

        static HashSet<string> BuildSet(params string[] items)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string item in items)
            {
                if (!string.IsNullOrEmpty(item)) set.Add(item);
            }

            return set;
        }

        public static string ParseMushroomType(AssetLocation mushroomCode)
        {
            string path = mushroomCode?.Path;
            if (string.IsNullOrEmpty(path)) return null;

            if (!path.StartsWith("mushroom-", StringComparison.OrdinalIgnoreCase)) return null;

            string rest = path.Substring("mushroom-".Length);
            int dash = rest.IndexOf('-');
            return dash > 0 ? rest.Substring(0, dash) : rest;
        }

        public static MyceliumNiche ClassifyNiche(AssetLocation mushroomCode, Block anchorBlock)
        {
            if (IsTrunkAnchor(anchorBlock)) return MyceliumNiche.TrunkPolypore;

            string type = ParseMushroomType(mushroomCode);
            if (string.IsNullOrEmpty(type)) return MyceliumNiche.ForestAnyTree;

            if (MeadowTypes.Contains(type)) return MyceliumNiche.MeadowOpen;
            if (DeciduousTypes.Contains(type)) return MyceliumNiche.ForestDeciduous;
            if (ConiferTypes.Contains(type)) return MyceliumNiche.ForestConifer;

            if (mushroomCode != null && mushroomCode.Path.Contains("-side-", StringComparison.OrdinalIgnoreCase))
            {
                return MyceliumNiche.TrunkPolypore;
            }

            return MyceliumNiche.ForestAnyTree;
        }

        public static bool IsTrunkAnchor(Block block)
        {
            if (block?.Code == null) return false;
            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path)) return false;
            return path.StartsWith("log-", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("logsection-", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("log-grown-", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryBuildRequirements(
            AssetLocation mushroomCode,
            Block anchorBlock,
            out PlantRequirements requirements)
        {
            requirements = null;
            if (mushroomCode == null) return false;

            string species = ParseMushroomType(mushroomCode);
            if (string.IsNullOrEmpty(species)) species = mushroomCode.Path ?? "mushroom";

            MyceliumNiche niche = ClassifyNiche(mushroomCode, anchorBlock);
            requirements = new PlantRequirements
            {
                Species = species,
                Habitat = EcologyHabitat.MyceliumAnchor,
                SpreadMode = SpreadMode.MyceliumNetwork,
                SpreadRate = 0.12f,
                ContextAffinity = NicheToContextAffinity(niche),
            };

            return true;
        }

        static FloraContextAffinity NicheToContextAffinity(MyceliumNiche niche)
        {
            switch (niche)
            {
                case MyceliumNiche.MeadowOpen:
                    return FloraContextAffinity.Open;
                case MyceliumNiche.TrunkPolypore:
                case MyceliumNiche.ForestDeciduous:
                case MyceliumNiche.ForestConifer:
                case MyceliumNiche.ForestAnyTree:
                    return FloraContextAffinity.Forest;
                default:
                    return FloraContextAffinity.Edge;
            }
        }

        public static string NicheLangKey(MyceliumNiche niche)
        {
            switch (niche)
            {
                case MyceliumNiche.MeadowOpen:
                    return "ecosystemflora:mycelium-niche-meadow";
                case MyceliumNiche.ForestDeciduous:
                    return "ecosystemflora:mycelium-niche-deciduous";
                case MyceliumNiche.ForestConifer:
                    return "ecosystemflora:mycelium-niche-conifer";
                case MyceliumNiche.TrunkPolypore:
                    return "ecosystemflora:mycelium-niche-trunk";
                default:
                    return "ecosystemflora:mycelium-niche-forest";
            }
        }

        public static MyceliumNiche GetNicheForRequirements(PlantRequirements req, Block anchorBlock)
        {
            if (req == null) return MyceliumNiche.ForestAnyTree;
            if (IsTrunkAnchor(anchorBlock)) return MyceliumNiche.TrunkPolypore;
            if (MeadowTypes.Contains(req.Species)) return MyceliumNiche.MeadowOpen;
            if (DeciduousTypes.Contains(req.Species)) return MyceliumNiche.ForestDeciduous;
            if (ConiferTypes.Contains(req.Species)) return MyceliumNiche.ForestConifer;
            return MyceliumNiche.ForestAnyTree;
        }
    }
}
