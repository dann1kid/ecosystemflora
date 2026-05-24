using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>In-memory wild-soil moisture/tier and agro role memory (chunk persist later).</summary>
    internal sealed class WildSoilStore
    {
        readonly Dictionary<BlockPos, WildSoilComposition> byGroundPos = new Dictionary<BlockPos, WildSoilComposition>();
        readonly Dictionary<BlockPos, WildAgroMemory> agroByGroundPos = new Dictionary<BlockPos, WildAgroMemory>();

        public WildSoilComposition GetOrCreate(ICoreAPI api, BlockPos groundPos, Block groundBlock)
        {
            if (TryGet(groundPos, out WildSoilComposition cached))
            {
                return cached;
            }

            MoistureLevel moisture = MoistureLevel.Mesic;
            NicheSampler niche = EcosystemSystem.Instance?.Niche;
            if (niche != null && api != null)
            {
                moisture = niche.GetNiche(api, groundPos.UpCopy()).Moisture;
            }

            WildSoilComposition created = WildSoilComposition.FromBlock(groundBlock, moisture);
            byGroundPos[groundPos] = created;
            return created;
        }

        public bool TryGet(BlockPos groundPos, out WildSoilComposition composition)
        {
            composition = default;
            return groundPos != null && byGroundPos.TryGetValue(groundPos, out composition);
        }

        public void Set(BlockPos groundPos, WildSoilComposition composition)
        {
            if (groundPos == null) return;
            byGroundPos[groundPos] = composition;
        }

        public void RecordAgroEvent(BlockPos groundPos, PlantSoilRole role, SoilSuccessionEvent evt)
        {
            if (groundPos == null) return;

            int weight = evt == SoilSuccessionEvent.Death ? 2 : 1;
            if (!agroByGroundPos.TryGetValue(groundPos, out WildAgroMemory mem))
            {
                mem = new WildAgroMemory { DominantRole = role, DominantScore = weight };
                agroByGroundPos[groundPos] = mem;
                return;
            }

            if (role == mem.DominantRole)
            {
                mem.DominantScore += weight;
            }
            else if (weight >= mem.DominantScore)
            {
                mem.DominantRole = role;
                mem.DominantScore = weight;
            }
            else
            {
                mem.DominantScore = System.Math.Max(1, mem.DominantScore - 1);
            }

            agroByGroundPos[groundPos] = mem;
        }

        public bool TryTakeTillContext(
            BlockPos groundPos,
            out PlantSoilRole role,
            out SoilFertilityTier tier)
        {
            role = PlantSoilRole.MeadowPerennial;
            tier = SoilFertilityTier.Medium;

            if (groundPos == null) return false;

            bool hadContext = false;
            if (agroByGroundPos.TryGetValue(groundPos, out WildAgroMemory mem))
            {
                role = mem.DominantRole;
                hadContext = true;
            }

            if (byGroundPos.TryGetValue(groundPos, out WildSoilComposition composition))
            {
                tier = composition.FertilityTier;
                hadContext = true;
            }

            agroByGroundPos.Remove(groundPos);
            byGroundPos.Remove(groundPos);
            return hadContext;
        }

        public void Clear()
        {
            byGroundPos.Clear();
            agroByGroundPos.Clear();
        }

        public void InvalidateGround(BlockPos groundPos)
        {
            if (groundPos == null) return;
            byGroundPos.Remove(groundPos);
            agroByGroundPos.Remove(groundPos);
        }
    }
}
