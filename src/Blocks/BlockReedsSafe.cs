using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace WildFarming.Blocks
{
    /// <summary>
    /// Null-safe <see cref="BlockReeds.OnGettingBroken"/> and scaled break FX for harvested brown sedge roots.
    /// </summary>
    public class BlockReedsSafe : BlockReeds
    {
        static readonly Cuboidf BrownsedgeHarvestedBreakBox = BrownsedgeHarvestedDecalFit.BreakBox;

        public override float OnGettingBroken(
            IPlayer player,
            BlockSelection blockSel,
            ItemSlot itemslot,
            float remainingResistance,
            float dt,
            int counter)
        {
            if (player == null || blockSel?.Position == null)
                return remainingResistance;

            string state = Variant != null ? Variant["state"] : null;

            if (state == "harvested")
            {
                dt /= 2f;
                if (player.InventoryManager?.ActiveTool == EnumTool.Shovel
                    && player.Entity?.World?.BlockAccessor != null)
                {
                    Block below = player.Entity.World.BlockAccessor.GetBlockBelow(blockSel.Position);
                    if (below != null
                        && TryGetMiningSpeed(itemslot, below.BlockMaterial, out float shovelMul))
                    {
                        dt *= shovelMul;
                    }
                }
            }
            else if (player.InventoryManager?.ActiveTool != EnumTool.Knife)
            {
                dt /= 3f;
            }
            else if (TryGetMiningSpeed(itemslot, EnumBlockMaterial.Plant, out float knifeMul))
            {
                dt *= knifeMul;
            }

            int requiredMiningTier = 0;
            if (api?.World != null)
            {
                try
                {
                    requiredMiningTier = GetRequiredMiningTier(api.World, blockSel.Position);
                }
                catch
                {
                    requiredMiningTier = 0;
                }
            }

            float resistance = requiredMiningTier == 0 ? remainingResistance - dt : remainingResistance;

            if ((counter % 5 == 0 || resistance <= 0)
                && player.Entity?.World != null
                && Sounds != null)
            {
                Vec3d hit = blockSel.HitPosition ?? new Vec3d(0.5, 0.5, 0.5);
                double posx = blockSel.Position.X + hit.X;
                double posy = blockSel.Position.InternalY + hit.Y;
                double posz = blockSel.Position.Z + hit.Z;
                int dim = blockSel.Position.dimension;
                SoundAttributes sound = resistance > 0 ? Sounds.Hit : Sounds.Break;
                if (sound != null)
                {
                    player.Entity.World.PlaySoundAt(sound, posx, posy, posz, dim, player);
                }
            }

            return resistance;
        }

        public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
        {
            if (IsBrownsedgeHarvestedRoot())
                return BrownsedgeHarvestedBreakBox;

            return base.GetParticleBreakBox(blockAccess, pos, facing);
        }

        public override void GetDecal(
            IWorldAccessor world,
            BlockPos pos,
            ITexPositionSource decalTexSource,
            ref MeshData decalModelData,
            ref MeshData blockModelData)
        {
            if (IsBrownsedgeHarvestedRoot())
            {
                if (decalModelData != null)
                    BrownsedgeHarvestedDecalFit.Apply(decalModelData);
                if (blockModelData != null)
                    BrownsedgeHarvestedDecalFit.Apply(blockModelData);
                return;
            }

            base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            if (world.Side == EnumAppSide.Server
                && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                if (Drops != null)
                {
                    foreach (BlockDropItemStack drop in Drops)
                    {
                        ItemStack stack = drop?.GetNextItemStack(dropQuantityMultiplier);
                        if (stack != null)
                            world.SpawnItemEntity(stack, pos);
                    }
                }

                if (Sounds?.Break != null)
                    world.PlaySoundAt(Sounds.Break, pos, -0.5, byPlayer);
            }

            if (byPlayer != null
                && Variant?["state"] == "normal"
                && (byPlayer.InventoryManager?.ActiveTool == EnumTool.Knife
                    || byPlayer.InventoryManager?.ActiveTool == EnumTool.Sickle
                    || byPlayer.InventoryManager?.ActiveTool == EnumTool.Scythe))
            {
                Block harvested = world.GetBlock(CodeWithVariants(new[] { "habitat", "state" }, new[] { "land", "harvested" }));
                if (harvested != null)
                    world.BlockAccessor.SetBlock(harvested.BlockId, pos);
                return;
            }

            if (IsBrownsedgeHarvestedRoot())
                SpawnSmallRootBreakParticles(world, pos, byPlayer);
            else
                SpawnBlockBrokenParticles(pos, byPlayer);

            world.BlockAccessor.SetBlock(0, pos);
        }

        bool IsBrownsedgeHarvestedRoot()
        {
            return Code?.Path != null
                && Code.Path.StartsWith("tallplant-brownsedge-")
                && Variant?["state"] == "harvested";
        }

        void SpawnSmallRootBreakParticles(IWorldAccessor world, BlockPos pos, IPlayer plr)
        {
            if (api == null)
                return;

            var props = new SmallRootBrokenParticleProps
            {
                blockdamage = new BlockDamage
                {
                    Block = this,
                    Position = pos,
                    Facing = BlockFacing.UP,
                },
                boyant = MaterialDensity < 1000,
            };
            props.Init(api);
            world.SpawnParticles(props, plr);
        }

        static bool TryGetMiningSpeed(ItemSlot itemslot, EnumBlockMaterial material, out float multiplier)
        {
            multiplier = 0f;
            if (itemslot?.Itemstack?.Collectible == null)
                return false;

            Dictionary<EnumBlockMaterial, float> speeds =
                itemslot.Itemstack.Collectible.GetMiningSpeeds(itemslot);
            if (speeds == null)
                return false;

            return speeds.TryGetValue(material, out multiplier);
        }

        sealed class SmallRootBrokenParticleProps : BlockBrokenParticleProps
        {
            public override float Size => 0.12f + (float)rand.NextDouble() * 0.18f;

            public override float Quantity => 4f + rand.Next(5);
        }
    }
}
