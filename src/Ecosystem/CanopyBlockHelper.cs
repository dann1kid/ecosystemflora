using System.Collections.Generic;using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    internal static class CanopyBlockHelper
    {
        public static bool IsDeciduousTreeWood(string wood) =>
            MyceliumTreeHost.IsDeciduousWood(wood);

        public static bool IsSkeletonBlock(Block block, string wood)
        {
            if (block?.Code == null || string.IsNullOrEmpty(wood)) return false;
            if (PlantCodeHelper.IsTreeLogGrownBlock(block))
            {
                return PlantCodeHelper.GetTreeWood(block) == wood;
            }

            if (IsBranchyLeaf(block))
            {
                return GetWoodFromFoliageBlock(block) == wood;
            }

            if (IsRegularLeaf(block))
            {
                return GetWoodFromFoliageBlock(block) == wood;
            }

            return false;
        }

        public static bool IsBudAnchorBlock(Block block, string wood)
        {
            if (block?.Code == null || string.IsNullOrEmpty(wood)) return false;
            if (PlantCodeHelper.IsTreeLogGrownBlock(block))
            {
                return PlantCodeHelper.GetTreeWood(block) == wood;
            }

            if (IsBranchyLeaf(block) && GetWoodFromFoliageBlock(block) == wood) return true;
            return IsRegularLeaf(block) && GetWoodFromFoliageBlock(block) == wood;
        }

        public static bool IsRegularLeaf(Block block)
        {
            if (block?.Code == null) return false;
            if (IsBranchyLeaf(block)) return false;
            string path = block.Code.Path;
            return path != null && path.StartsWith("leaves-");
        }

        public static bool IsRegularLeafPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.StartsWith("leavesbranchy-")) return false;
            return path.StartsWith("leaves-");
        }

        public static bool IsBranchyLeaf(Block block)
        {
            if (block?.Code == null) return false;
            string path = block.Code.Path;
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith("leavesbranchy-")) return true;
                // leaves-grown-* / leaves-placed-* are regular foliage regardless of block class.
                if (path.StartsWith("leaves-")) return false;
            }

            return block.Class == "BlockLeavesBranchy" || block.Class == "leavesbranchy";
        }

        public static string GetWoodFromFoliageBlock(Block block)
        {
            return GetWoodFromFoliagePath(block?.Code?.Path);
        }

        /// <summary>Parses vanilla foliage codes: leaves-grown-oak, leaves-grown3-birch, leaves-oak-grown-n, etc.</summary>
        public static string GetWoodFromFoliagePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (path.StartsWith("leavesbranchy-"))
            {
                path = path.Substring("leavesbranchy-".Length);
            }
            else if (path.StartsWith("leaves-"))
            {
                path = path.Substring("leaves-".Length);
            }
            else
            {
                return null;
            }

            return ParseWoodFromFoliageRest(path);
        }

        static string ParseWoodFromFoliageRest(string rest)
        {
            if (string.IsNullOrEmpty(rest)) return null;

            if (rest.StartsWith("grown", System.StringComparison.Ordinal))
            {
                string wood = WoodAfterVariantPrefix(rest, "grown".Length);
                if (wood != null) return wood;
            }

            if (rest.StartsWith("placed", System.StringComparison.Ordinal))
            {
                string wood = WoodAfterVariantPrefix(rest, "placed".Length);
                if (wood != null) return wood;
            }

            const string grownSuffix = "-grown";
            int grownIdx = rest.IndexOf(grownSuffix, System.StringComparison.Ordinal);
            if (grownIdx > 0)
            {
                string wood = rest.Substring(0, grownIdx);
                return WildTreeEcology.TryGet(wood, out _) ? wood : null;
            }

            return TokenWood(rest);
        }

        /// <summary>After "grown" / "placed": -oak, 1-oak, 7-birch, etc.</summary>
        static string WoodAfterVariantPrefix(string rest, int prefixLen)
        {
            if (rest.Length <= prefixLen) return null;

            string tail = rest.Substring(prefixLen);
            if (tail.StartsWith("-", System.StringComparison.Ordinal))
            {
                return TokenWood(tail.Substring(1));
            }

            if (tail.Length > 0 && char.IsDigit(tail[0]))
            {
                int i = 0;
                while (i < tail.Length && char.IsDigit(tail[i])) i++;
                if (i < tail.Length && tail[i] == '-')
                {
                    return TokenWood(tail.Substring(i + 1));
                }
            }

            return null;
        }

        static string TokenWood(string rest)
        {
            if (string.IsNullOrEmpty(rest)) return null;
            int dash = rest.IndexOf('-');
            if (dash > 0) rest = rest.Substring(0, dash);
            return WildTreeEcology.TryGet(rest, out _) ? rest : null;
        }

        public static bool WithinScanBounds(
            BlockPos basePos,
            BlockPos pos,
            int horizRadius,
            int vertUp,
            int vertDown)
        {
            if (basePos == null || pos == null) return false;
            if (pos.dimension != basePos.dimension) return false;
            int dy = pos.Y - basePos.Y;
            if (dy > vertUp || dy < -vertDown) return false;
            int dx = pos.X - basePos.X;
            int dz = pos.Z - basePos.Z;
            return System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dz)) <= horizRadius;
        }

        public static Block ResolveGrownLeafBlock(
            IWorldAccessor world,
            string wood,
            BlockPos targetPos,
            BlockPos anchorPos,
            Block anchorBlock)
        {
            return ResolveFoliageBlock(world, wood, targetPos, anchorPos, anchorBlock, branchy: false);
        }

        public static Block ResolveBranchyLeafBlock(
            IWorldAccessor world,
            string wood,
            BlockPos targetPos,
            BlockPos anchorPos,
            Block anchorBlock)
        {
            return ResolveFoliageBlock(world, wood, targetPos, anchorPos, anchorBlock, branchy: true);
        }

        static Block ResolveFoliageBlock(
            IWorldAccessor world,
            string wood,
            BlockPos targetPos,
            BlockPos anchorPos,
            Block anchorBlock,
            bool branchy)
        {
            if (world == null || string.IsNullOrEmpty(wood) || targetPos == null || anchorPos == null) return null;

            string face = FaceTowardAnchor(targetPos, anchorPos);
            Block fromTemplate = PickFoliageBlock(world, wood, branchy, anchorBlock, face);
            if (fromTemplate != null) return fromTemplate;

            IList<Block> blocks = world.Blocks;
            if (blocks != null)
            {
                for (int i = 0; i < blocks.Count; i++)
                {
                    Block candidate = blocks[i];
                    if (candidate?.Code == null || candidate.Id == 0) continue;
                    if (!MatchesFoliageWood(candidate.Code.Path, wood, branchy)) continue;
                    if (branchy ? IsBranchyLeaf(candidate) : IsRegularLeaf(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        static bool MatchesFoliageWood(string path, string wood, bool branchy)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(wood)) return false;
            string prefix = branchy ? "leavesbranchy-" : "leaves-";
            if (!path.StartsWith(prefix)) return false;
            return GetWoodFromFoliagePath(path) == wood;
        }

        static Block PickFoliageBlock(
            IWorldAccessor world,
            string wood,
            bool branchy,
            Block template,
            string preferredFace)
        {
            if (world == null || string.IsNullOrEmpty(wood)) return null;

            if (template != null)
            {
                Block converted = branchy
                    ? BranchyBlockFromTemplate(world, template, wood, preferredFace)
                    : LeafBlockFromTemplate(world, template, wood, preferredFace);
                if (converted != null) return converted;

                if (branchy ? IsBranchyLeaf(template) : IsRegularLeaf(template))
                {
                    if (GetWoodFromFoliageBlock(template) == wood) return template;
                }
            }

            if (!string.IsNullOrEmpty(preferredFace))
            {
                Block oriented = TryGetLeafByFace(world, wood, preferredFace, branchy);
                if (oriented != null) return oriented;
            }

            return null;
        }

        static Block TryGetBranchyByFace(IWorldAccessor world, string wood, string face) =>
            TryGetLeafByFace(world, wood, face, branchy: true);

        static Block BranchyBlockFromTemplate(IWorldAccessor world, Block template, string wood, string preferredFace)
        {
            if (template?.Code == null) return null;

            string path = template.Code.Path;
            if (path.StartsWith("leavesbranchy-"))
            {
                // keep path
            }
            else if (path.StartsWith("leaves-"))
            {
                path = "leavesbranchy-" + path.Substring("leaves-".Length);
            }
            else if (path.StartsWith("log-grown-"))
            {
                return TryGetBranchyByFace(world, wood, preferredFace);
            }
            else
            {
                return null;
            }

            if (!PathContainsWood(path, wood)) return null;

            if (!string.IsNullOrEmpty(preferredFace) && HasOrientationSuffix(path))
            {
                int dash = path.LastIndexOf('-');
                if (dash > 0)
                {
                    path = path.Substring(0, dash + 1) + preferredFace;
                }
            }

            Block candidate = world.GetBlock(new AssetLocation(template.Code.Domain, path));
            if (candidate != null && candidate.Id != 0 && IsBranchyLeaf(candidate)) return candidate;
            return null;
        }

        static bool PathContainsWood(string path, string wood) =>
            GetWoodFromFoliagePath(path) == wood;

        static bool HasOrientationSuffix(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            int dash = path.LastIndexOf('-');
            if (dash <= 0 || dash >= path.Length - 1) return false;
            string tail = path.Substring(dash + 1);
            if (tail.Length == 0 || char.IsDigit(tail[0])) return false;
            return tail.Length <= 5;
        }

        static Block TryGetLeafByFace(IWorldAccessor world, string wood, string face, bool branchy)
        {
            string prefix = branchy ? "leavesbranchy-" : "leaves-";
            foreach (string candidate in FaceCodeCandidates(face))
            {
                Block oriented = world.GetBlock(new AssetLocation("game", prefix + "grown-" + wood + "-" + candidate));
                if (oriented != null && oriented.Id != 0 && (branchy ? IsBranchyLeaf(oriented) : IsRegularLeaf(oriented)))
                {
                    return oriented;
                }

                oriented = world.GetBlock(new AssetLocation("game", prefix + wood + "-grown-" + candidate));
                if (oriented != null && oriented.Id != 0 && (branchy ? IsBranchyLeaf(oriented) : IsRegularLeaf(oriented)))
                {
                    return oriented;
                }
            }

            return null;
        }

        static IEnumerable<string> FaceCodeCandidates(string face)
        {
            if (string.IsNullOrEmpty(face)) yield break;
            yield return face;
            switch (face)
            {
                case "n": yield return "north"; break;
                case "e": yield return "east"; break;
                case "s": yield return "south"; break;
                case "w": yield return "west"; break;
                case "u": yield return "up"; break;
                case "d": yield return "down"; break;
                case "north": yield return "n"; break;
                case "east": yield return "e"; break;
                case "south": yield return "s"; break;
                case "west": yield return "w"; break;
                case "up": yield return "u"; break;
                case "down": yield return "d"; break;
            }
        }

        public static string FaceTowardAnchor(BlockPos targetPos, BlockPos anchorPos)
        {
            if (targetPos == null || anchorPos == null) return null;
            int dx = anchorPos.X - targetPos.X;
            int dy = anchorPos.Y - targetPos.Y;
            int dz = anchorPos.Z - targetPos.Z;
            if (dx == 1) return "e";
            if (dx == -1) return "w";
            if (dy == 1) return "u";
            if (dy == -1) return "d";
            if (dz == 1) return "s";
            if (dz == -1) return "n";
            return null;
        }

        static Block LeafBlockFromTemplate(IWorldAccessor world, Block template, string wood, string preferredFace)
        {
            if (template?.Code == null) return null;

            string path = template.Code.Path;
            if (path.StartsWith("leavesbranchy-"))
            {
                path = "leaves-" + path.Substring("leavesbranchy-".Length);
            }
            else if (!path.StartsWith("leaves-"))
            {
                return null;
            }

            if (!PathContainsWood(path, wood)) return null;

            if (!string.IsNullOrEmpty(preferredFace) && HasOrientationSuffix(path))
            {
                int dash = path.LastIndexOf('-');
                if (dash > 0)
                {
                    path = path.Substring(0, dash + 1) + preferredFace;
                }
            }

            Block candidate = world.GetBlock(new AssetLocation(template.Code.Domain, path));
            if (candidate != null && candidate.Id != 0 && IsRegularLeaf(candidate)) return candidate;
            return null;
        }

        public static float DeterministicNoise(BlockPos pos, string wood, int gameYear)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)pos.X) * 16777619u;
                h = (h ^ (uint)pos.Y) * 16777619u;
                h = (h ^ (uint)pos.Z) * 16777619u;
                h = (h ^ (uint)gameYear) * 16777619u;
                if (wood != null)
                {
                    for (int i = 0; i < wood.Length; i++)
                    {
                        h = (h ^ wood[i]) * 16777619u;
                    }
                }

                return (h & 0xFFFF) / 65535f;
            }
        }
    }
}
