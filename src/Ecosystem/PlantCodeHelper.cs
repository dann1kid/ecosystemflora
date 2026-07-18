using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public static class PlantCodeHelper
    {
        /// <summary>True when JSON declares <c>ecologyParticipant</c> on the block type.</summary>
        public static bool HasDeclaredEcologyParticipant(Block block)
        {
            if (!EcosystemConfig.Loaded.EnableThirdPartyParticipants || block?.Attributes == null || block.Code == null) return false;
            if (!block.Attributes["ecologyParticipant"].AsBool(false)) return false;
            if (string.IsNullOrWhiteSpace(block.Attributes["ecologySpecies"].AsString(null))) return false;
            return !string.IsNullOrWhiteSpace(block.Attributes["ecologySpreadBlock"].AsString(null));
        }

        /// <summary>True when attributes declare a reproducible ecology plant from any mod domain (v3.1).</summary>
        public static bool IsThirdPartyEcologyBlock(Block block)
        {
            if (!EcosystemConfig.Loaded.EnableThirdPartyParticipants || block?.Code == null) return false;
            if (HasDeclaredEcologyParticipant(block)) return true;
            return WildcraftFruitBerryEcology.IsWildBerryBlock(block);
        }

        /// <summary>Reads <c>ecologySpecies</c> when <c>ecologyParticipant</c> is enabled; otherwise vanilla path rules.</summary>
        public static string ResolveEcologySpecies(Block block)
        {
            if (block?.Attributes != null && EcosystemConfig.Loaded.EnableThirdPartyParticipants
                && block.Attributes["ecologyParticipant"].AsBool(false))
            {
                string declared = block.Attributes["ecologySpecies"].AsString(null)?.Trim();
                if (!string.IsNullOrEmpty(declared)) return declared;
            }

            if (EcosystemConfig.Loaded.EnableThirdPartyParticipants
                && WildcraftFruitBerryEcology.TryGetEcologySpecies(block, out string wcSpecies))
            {
                return wcSpecies;
            }

            return GetEcologySpecies(block?.Code);
        }

        /// <summary>Parses <c>ecologyHabitat</c> attribute values (English enum names).</summary>
        public static EcologyHabitat ParseEcologyHabitat(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return EcologyHabitat.Terrestrial;
            raw = raw.Trim();
            return Enum.TryParse(raw, ignoreCase: true, out EcologyHabitat h) ? h : EcologyHabitat.Terrestrial;
        }

        /// <summary><c>domain:path</c> or path with <paramref name="defaultDomain"/>.</summary>
        public static AssetLocation ResolveEcologyAsset(string raw, string defaultDomain)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            raw = raw.Trim().Replace('\\', '/');
            int colon = raw.IndexOf(':');
            if (colon > 0)
            {
                string dom = raw.Substring(0, colon).Trim();
                string path = raw.Substring(colon + 1).Trim();
                return new AssetLocation(dom, path);
            }

            defaultDomain ??= "game";
            return new AssetLocation(defaultDomain, raw);
        }

        /// <summary>Habitat from third-party attrs or inferred from vanilla block code.</summary>
        public static EcologyHabitat GetEcologyHabitat(Block block)
        {
            if (block?.Attributes != null && EcosystemConfig.Loaded.EnableThirdPartyParticipants
                && block.Attributes["ecologyParticipant"].AsBool(false))
            {
                return ParseEcologyHabitat(block.Attributes["ecologyHabitat"].AsString("Terrestrial"));
            }

            return GetEcologyHabitat(block?.Code);
        }

        public static bool IsEcologyPlant(Block block)
        {
            if (block?.Code == null) return false;
            if (IsThirdPartyEcologyBlock(block)) return true;
            if (FlowerJuvenileBlocks.IsJuvenileBlock(block)) return true;
            if (FernJuvenileBlocks.IsJuvenileBlock(block)) return true;
            if (ShoreSedgeJuvenileBlocks.IsJuvenileBlock(block)) return true;
            if (FlowerPhenologyBlocks.IsPhaseBlock(block)) return true;
            if (FernPhenologyBlocks.IsPhaseBlock(block)) return true;
            if (SedgePhenologyBlocks.IsPhaseBlock(block)) return true;
            if (TallgrassPhenologyBlocks.IsPhaseBlock(block)) return true;
            if (block.Code.Domain != "game") return false;
            if (TryGetEcologySpecies(block.Code, out _)) return true;
            return IsTreeSaplingBlock(block) || IsWildBerryBushBlock(block);
        }

        /// <summary>
        /// Vanilla <see cref="Blocks.BlockReedsSafe"/> mow: normal brown sedge → harvested stubble (block remains).
        /// DidBreakBlock still fires; ecology must not treat this as removal or wake the mat.
        /// </summary>
        public static bool IsBrownsedgeMowHarvestTransition(Block oldBlock, Block newBlock)
        {
            string oldPath = oldBlock?.Code?.Path;
            string newPath = newBlock?.Code?.Path;
            if (oldPath == null || newPath == null) return false;
            if (!oldPath.StartsWith("tallplant-brownsedge-", StringComparison.Ordinal)) return false;
            if (!oldPath.Contains("-normal-", StringComparison.Ordinal)) return false;
            return newPath.Contains("-harvested-", StringComparison.Ordinal);
        }

        /// <summary>
        /// Ecology plant broken to air that should clear registry / wake neighbors.
        /// Excludes mown brown sedge stubble and eaten tallgrass (not removed from the world).
        /// </summary>
        public static bool CountsAsEcologyPlantRemovalForWake(Block block)
        {
            if (!IsEcologyPlant(block)) return false;

            string path = block.Code.Path;
            if (path != null && path.Contains("-harvested-", StringComparison.Ordinal)) return false;
            if (path != null && path.Contains("-eaten-", StringComparison.Ordinal)) return false;
            return true;
        }

        /// <summary>Vanilla game-domain ecology only (excludes declared third-party JSON participants).</summary>
        public static bool IsVanillaEcologyPlant(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            if (TryGetEcologySpecies(block.Code, out _)) return true;
            return IsTreeSaplingBlock(block) || IsWildBerryBushBlock(block);
        }

        public static bool TryGetEcologySpecies(AssetLocation blockCode, out string species)
        {
            species = GetEcologySpecies(blockCode);
            return species != null;
        }

        public static string GetEcologySpecies(AssetLocation blockCode)
        {
            string path = blockCode?.Path;
            if (string.IsNullOrEmpty(path)) return null;

            string juvenileSpecies = FlowerJuvenileBlocks.SpeciesFromJuvenileCode(blockCode);
            if (juvenileSpecies != null) return juvenileSpecies;

            juvenileSpecies = FernJuvenileBlocks.SpeciesFromJuvenileCode(blockCode);
            if (juvenileSpecies != null) return juvenileSpecies;

            juvenileSpecies = ShoreSedgeJuvenileBlocks.SpeciesFromJuvenileCode(blockCode);
            if (juvenileSpecies != null) return juvenileSpecies;

            string phaseSpecies = FlowerPhenologyBlocks.SpeciesFromPhaseCode(blockCode);
            if (phaseSpecies != null) return phaseSpecies;

            phaseSpecies = FernPhenologyBlocks.SpeciesFromPhaseCode(blockCode);
            if (phaseSpecies != null) return phaseSpecies;

            if (blockCode?.Domain == "ecosystemflora"
                && blockCode.Path != null
                && blockCode.Path.StartsWith("tallgrassphase-"))
            {
                return "tallgrass";
            }

            if (blockCode?.Domain == "ecosystemflora"
                && blockCode.Path != null
                && blockCode.Path.StartsWith("sedgephase-"))
            {
                return EcologyShoreSedgeSpecies.Brownsedge;
            }

            if (path.StartsWith("flower-"))
            {
                string rest = path.Substring("flower-".Length);
                if (rest.EndsWith("-free")) rest = rest.Substring(0, rest.Length - "-free".Length);
                else if (rest.EndsWith("-snow")) rest = rest.Substring(0, rest.Length - "-snow".Length);
                if (rest.StartsWith("lupine")) return "lupine";
                if (rest.StartsWith("croton")) return "croton";
                if (rest.StartsWith("rafflesia-red")) return "rafflesiared";
                if (rest.StartsWith("rafflesia-brown")) return "rafflesiabrown";
                return rest;
            }

            if (path.StartsWith("tallplant-brownsedge")) return EcologyShoreSedgeSpecies.Brownsedge;

            if (path.StartsWith("barrelcactus")) return EcologyDesertSpecies.Barrelcactus;
            if (path == "silvertorchcactus") return EcologyDesertSpecies.Silvertorchcactus;

            if (path.StartsWith("tallplant-coopersreed")) return "coopersreed";
            if (path.StartsWith("tallplant-tule")) return "tule";
            if (path.StartsWith("tallplant-papyrus")) return "papyrus";
            if (path == "waterlily") return "waterlily";
            if (path.StartsWith("aquatic-watercrowfoot")) return "watercrowfoot";

            if (path.StartsWith("fruitingbush-wild-"))
            {
                string berry = ParseBerryType(path);
                if (berry != null) return berry;
            }

            if (path == "tallfern") return "tallfern";

            if (path.StartsWith("tallgrass-"))
            {
                if (path.StartsWith("tallgrass-eaten")) return null;
                return "tallgrass";
            }

            if (path.StartsWith("frostedtallgrass-"))
            {
                if (path.Contains("-eaten-")) return null;
                return "tallgrass";
            }

            if (path.StartsWith("fern-"))
            {
                string species = VanillaFernSpeciesParser.TryParseSpeciesFromPath(path);
                if (species != null) return species;
            }

            if (path != null && path.StartsWith("ferntree-normal-"))
            {
                return EcologyFerntreeSpecies.Ferntree;
            }

            if (path != null && path.StartsWith("wildvine-tropical-"))
            {
                return WildVineHelper.TropicalSpecies;
            }

            if (path != null && path.StartsWith("wildvine-"))
            {
                return WildVineHelper.TemperateSpecies;
            }

            string wood = GetTreeWood(blockCode);
            if (wood != null) return wood;

            return null;
        }

        public static bool IsTreeLogGrownBlock(Block block)
        {
            if (!IsAnyLogGrownTrunkBlock(block)) return false;
            return GetTreeWood(block) != null;
        }

        /// <summary>
        /// Living trunk wood in the world: normal <c>log-grown-*</c> and redwood 2×2
        /// <c>logsection-grown-*</c> quarters (vanilla treegen trunkSegmentBase).
        /// </summary>
        public static bool IsAnyLogGrownTrunkBlock(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path)) return false;

            if (path.StartsWith("logsection-grown-")) return true;

            if (!path.StartsWith("log-grown-")) return false;
            return !path.StartsWith("log-grown-aged");
        }

        /// <summary>Vanilla 2×2 trunk quarter (<c>logsection-grown-{wood}-{ne|se|sw|nw}-*</c>), e.g. redwood.</summary>
        public static bool IsLogSectionGrownBlock(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            string path = block.Code.Path;
            return path != null && path.StartsWith("logsection-grown-");
        }

        public static bool IsFerntreeTrunkBlock(Block block) =>
            FerntreeStructure.IsTrunkBlock(block);

        public static bool IsFerntreeEcologyBlock(Block block) =>
            FerntreeStructure.IsFerntreeBlock(block);

        public static bool IsFruitTreeTrunkBlock(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            string path = block.Code.Path;
            return path != null
                && (path.StartsWith("fruittree-stem-") || path.StartsWith("fruittree-young-"));
        }

        /// <summary>Blocks meadow spread must never occupy or displace (player-planted saplings included).</summary>
        public static bool IsArborealHostBlock(Block block) =>
            IsAnyLogGrownTrunkBlock(block)
            || IsFerntreeTrunkBlock(block)
            || IsTreeSaplingBlock(block)
            || IsFruitTreeTrunkBlock(block);

        public static bool IsTreeSaplingBlock(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("sapling-")) return false;
            if (!path.EndsWith("-free")) return false;
            if (path.Contains("bambooshoots") || path.Contains("bamboo-")) return false;
            return GetTreeWood(block) != null;
        }

        /// <summary>True for mature tree parents, flowers, reeds, etc. Saplings count for spacing only.</summary>
        public static bool IsEcologySpreadParent(Block block)
        {
            if (block == null) return false;
            if (IsTreeSaplingBlock(block)) return false;
            if (IsThirdPartyEcologyBlock(block)) return true;
            if (IsWildBerryBushBlock(block)) return true;
            if (IsTreeLogGrownBlock(block)) return true;
            if (IsFerntreeTrunkBlock(block)) return true;
            if (WildVineHelper.IsVineBlock(block)) return true;
            if (!IsEcologyPlant(block)) return false;

            if (ResolveEcologySpecies(block) == "tallgrass"
                && TallgrassSpreadMaturation.UsesMaturation(EcosystemConfig.Loaded)
                && !TallgrassSpreadMaturation.CanReproduceFrom(block))
            {
                return false;
            }

            return true;
        }

        public static bool IsWildBerryBushBlock(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            string path = block.Code.Path;
            return path != null
                && path.StartsWith("fruitingbush-wild-")
                && ParseBerryType(path) != null;
        }

        static string ParseBerryType(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("fruitingbush-wild-")) return null;

            string rest = path.Substring("fruitingbush-wild-".Length);
            int free = rest.IndexOf("-free");
            if (free > 0) rest = rest.Substring(0, free);
            else
            {
                int dash = rest.IndexOf('-');
                if (dash > 0) rest = rest.Substring(0, dash);
            }

            return EcologyBerrySpecies.IsKnown(rest) ? rest : null;
        }

        public static string GetTreeWood(Block block)
        {
            if (block?.Variant != null && block.Variant.TryGetValue("wood", out string variantWood))
            {
                if (EcologyTreeSpecies.IsKnown(variantWood)) return variantWood;
            }

            return GetTreeWood(block?.Code);
        }

        public static string GetTreeWood(AssetLocation blockCode)
        {
            if (blockCode == null || blockCode.Domain != "game") return null;

            string path = blockCode.Path;
            if (string.IsNullOrEmpty(path)) return null;

            if (path.StartsWith("log-grown-"))
            {
                string rest = path.Substring("log-grown-".Length);
                int dash = rest.IndexOf('-');
                if (dash > 0) rest = rest.Substring(0, dash);
                if (rest == "aged") return null;
                // Floral Zones and other content mods extend game:log-grown-* woods without updating our
                // vanilla wood allowlist. Treat any non-aged wood id as a valid tree species id.
                return rest;
            }

            // Redwood 2×2 trunk: logsection-grown-redwood-ne-ud
            if (path.StartsWith("logsection-grown-"))
            {
                string rest = path.Substring("logsection-grown-".Length);
                int dash = rest.IndexOf('-');
                if (dash > 0) rest = rest.Substring(0, dash);
                return string.IsNullOrEmpty(rest) ? null : rest;
            }

            if (path.StartsWith("sapling-"))
            {
                string rest = path.Substring("sapling-".Length);
                int dash = rest.IndexOf('-');
                if (dash > 0) rest = rest.Substring(0, dash);
                return rest;
            }

            return null;
        }

        public static BlockPos GetTreeTrunkBase(IBlockAccessor acc, BlockPos logPos)
        {
            BlockPos scan = logPos.Copy();
            string wood = GetTreeWood(acc.GetBlock(scan)?.Code);
            if (wood == null) return logPos.Copy();

            while (true)
            {
                BlockPos below = scan.DownCopy();
                Block belowBlock = acc.GetBlock(below);
                if (!IsTreeLogGrownBlock(belowBlock)) break;
                if (GetTreeWood(belowBlock) != wood) break;
                scan.Set(below);
            }

            return scan;
        }

        public static bool IsReedBlock(Block block)
        {
            if (block != null && IsThirdPartyEcologyBlock(block) && GetEcologyHabitat(block) == EcologyHabitat.ReedNearWater)
                return true;

            string species = GetEcologySpecies(block?.Code);
            return species == "coopersreed" || species == "tule" || species == "papyrus";
        }

        public static bool IsWatercrowfoot(AssetLocation code)
        {
            return GetEcologySpecies(code) == "watercrowfoot";
        }

        /// <summary>Lowest block of a water-crowfoot column (for reproduce origin).</summary>
        public static BlockPos GetColumnBase(IBlockAccessor acc, BlockPos pos)
        {
            BlockPos scan = pos.Copy();
            while (IsWatercrowfoot(acc.GetBlock(scan.DownCopy())?.Code))
            {
                scan.Down();
            }

            return scan;
        }

        public static BlockPos GetReproduceAnchor(IBlockAccessor acc, BlockPos pos, AssetLocation blockCode)
        {
            Block at = acc?.GetBlock(pos);
            string speciesResolved = at != null ? ResolveEcologySpecies(at) : GetEcologySpecies(blockCode);
            if (speciesResolved == "watercrowfoot")
            {
                return GetColumnBase(acc, pos);
            }

            if (IsTreeLogGrownBlock(acc.GetBlock(pos)))
            {
                return GetTreeTrunkBase(acc, pos);
            }

            if (IsFerntreeEcologyBlock(acc.GetBlock(pos)))
            {
                return FerntreeStructure.GetTrunkBase(acc, pos);
            }

            if (WildVineHelper.IsVineBlock(at)
                && WildVineHelper.TryResolveSpreadAnchor(acc, null, pos, out BlockPos vineAnchor))
            {
                return vineAnchor;
            }

            return pos.Copy();
        }

        public static EcologyHabitat GetEcologyHabitat(AssetLocation blockCode)
        {
            string species = GetEcologySpecies(blockCode);
            if (species == null) return EcologyHabitat.Terrestrial;

            if (EcologyAquaticSpecies.IsKnown(species))
            {
                return EcologyAquaticSpecies.GetHabitat(species);
            }

            if (EcologyTreeSpecies.IsKnown(species))
            {
                return EcologyHabitat.TerrestrialTree;
            }

            if (EcologyFerntreeSpecies.IsKnown(species))
            {
                return EcologyHabitat.Ferntree;
            }

            if (WildVineHelper.IsKnown(species))
            {
                return EcologyHabitat.WildVine;
            }

            return EcologyHabitat.Terrestrial;
        }

        public static AssetLocation SpreadBlockCode(Block block)
        {
            if (block?.Code == null) return null;

            if (IsThirdPartyEcologyBlock(block))
            {
                if (HasDeclaredEcologyParticipant(block))
                {
                    AssetLocation spread = ResolveEcologyAsset(block.Attributes["ecologySpreadBlock"].AsString(""), block.Code.Domain);
                    if (spread?.Path?.Length > 0) return spread;
                }

                if (WildcraftFruitBerryEcology.TryGetSpreadBlock(block, out AssetLocation wcSpread))
                {
                    return wcSpread;
                }

                return null;
            }

            string wood = GetTreeWood(block);
            if (wood != null && IsTreeLogGrownBlock(block))
            {
                // Juvenile spread target is a log-grown seedling (not vanilla sapling treegen).
                return new AssetLocation("game", "log-grown-" + wood + "-ud");
            }

            if (IsFerntreeTrunkBlock(block))
            {
                return new AssetLocation("game:ferntree-normal-trunk");
            }

            if (WildVineHelper.TryParse(block, out WildVineInfo vine))
            {
                string path = vine.Tropical
                    ? "wildvine-tropical-end-" + vine.Facing.Code
                    : "wildvine-end-" + vine.Facing.Code;
                return new AssetLocation("game", path);
            }

            if (FlowerPhenologyBlocks.SpeciesFromPhaseBlock(block) != null)
            {
                return block.Code;
            }

            if (FernPhenologyBlocks.SpeciesFromPhaseCode(block.Code) != null)
            {
                return block.Code;
            }

            if (!IsEcologyPlant(block)) return null;

            if (IsTreeSaplingBlock(block)) return null;

            if (ResolveEcologySpecies(block) == "watercrowfoot")
            {
                return new AssetLocation("game:aquatic-watercrowfoot-section");
            }

            return block.Code;
        }

        /// <summary>Pick land-normal vs water-normal from target cell (vanilla habitat).</summary>
        public static Block ResolveReedSpreadBlock(ICoreAPI api, BlockPos plantPos, Block parentBlock)
        {
            if (parentBlock == null) return null;

            EcologyHabitat h = GetEcologyHabitat(parentBlock);
            string species = ResolveEcologySpecies(parentBlock);
            bool vanillaReed = species == "coopersreed" || species == "tule" || species == "papyrus";
            if (!vanillaReed && h != EcologyHabitat.ReedNearWater)
                return parentBlock;

            if (!vanillaReed)
            {
                // Third-party reed: mod block is already the correct land/water variant for its type.
                return parentBlock;
            }

            IBlockAccessor acc = api.World.BlockAccessor;
            string habitat = BlockFluidHelper.IsDedicatedWaterCell(acc, plantPos) ? "water" : "land";
            string path = parentBlock.Code.Path ?? "";
            string cover = path.Contains("-snow") ? "snow" : "free";
            string code = "tallplant-" + species + "-" + habitat + "-normal-" + cover;
            Block block = api.World.GetBlock(new AssetLocation("game:" + code));
            return block ?? parentBlock;
        }

        public static bool SameEcologySpecies(AssetLocation a, AssetLocation b)
        {
            string sa = GetEcologySpecies(a);
            string sb = GetEcologySpecies(b);
            return sa != null && sa == sb;
        }

        public static AssetLocation MatureBlockLocation(Block block)
        {
            if (IsThirdPartyEcologyBlock(block))
            {
                JsonObject attrs = block.Attributes;
                string matureRaw = attrs?["ecologyMatureBlock"].AsString(null);
                if (!string.IsNullOrWhiteSpace(matureRaw))
                {
                    AssetLocation mature = ResolveEcologyAsset(matureRaw, block.Code.Domain);
                    if (mature?.Path?.Length > 0) return mature;
                }

                // Third-party default: the block itself is the "mature identity" in the registry.
                // If a mod wants a separate sprout/juvenile block, it should set ecologyMatureBlock on that sprout.
                return block.Code;
            }

            AssetLocation vanilla = SpreadBlockCode(block);
            if (vanilla != null) return vanilla;

            string path = block?.Code?.Path;
            if (string.IsNullOrEmpty(path)) return null;

            const string prefix = "wildplant-";
            if (path.StartsWith(prefix))
            {
                return new AssetLocation("game:" + path.Substring(prefix.Length));
            }

            return null;
        }
    }
}
