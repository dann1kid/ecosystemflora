using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using WildFarming.Ecosystem;

namespace WildFarming
{
    public class WildSeed : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null || byEntity?.World == null) return;

            handling = EnumHandHandling.PreventDefault;

            if (byEntity.World.Side != EnumAppSide.Server) return;

            IWorldAccessor world = byEntity.World;
            BlockPos groundPos = blockSel.Position;
            BlockPos plantPos = groundPos.UpCopy();

            AssetLocation wildPlantCode = PlantCodeHelper.WildPlantCodeFromSeed(slot.Itemstack.Collectible);
            Block wildPlant = wildPlantCode == null ? null : world.GetBlock(wildPlantCode);

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            if (!world.Claims.TryAccess(byPlayer, plantPos, EnumBlockAccessFlags.BuildOrBreak)) return;
            if (wildPlant == null) return;

            Block ground = world.BlockAccessor.GetBlock(groundPos);
            Block space = world.BlockAccessor.GetBlock(plantPos);

            if (!ground.SideSolid[blockSel.Face.Index]) return;
            if (space.Replaceable < 9500) return;

            world.BlockAccessor.SetBlock(wildPlant.BlockId, plantPos);
            world.PlaySoundAt(new AssetLocation("sounds/block/plant"), plantPos.X, plantPos.Y, plantPos.Z, byPlayer);

            if (byPlayer?.WorldData?.CurrentGameMode != EnumGameMode.Creative)
            {
                slot.TakeOut(1);
                slot.MarkDirty();
            }
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity?.World?.Side == EnumAppSide.Client && secondsUsed > 0.1f)
            {
                ((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            AssetLocation code = PlantCodeHelper.WildPlantCodeFromSeed(this);
            Block block = code == null ? null : world.GetBlock(code);
            if (block == null) return;

            dsc.AppendLine("Average Grow Time: " + block.Attributes["hours"].AsFloat(192f) / 24);
            dsc.AppendLine("Maximum Growing Temperature: " + block.Attributes["maxTemp"].AsFloat(50f));
            dsc.AppendLine("Minimum Growing Temperature: " + block.Attributes["minTemp"].AsFloat(-5f));
            dsc.AppendLine("Can be planted anywhere; may die if climate is unsuitable.");
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-plant",
                    MouseButton = EnumMouseButton.Right,
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
