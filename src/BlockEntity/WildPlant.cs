using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;

namespace WildFarming
{
    public class WildPlantBlockEntity : BlockEntity
    {
        double plantedAt;
        double blossomAt;
        int failedSurvivalChecks;
        PlantRequirements requirements;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            requirements = PlantRequirements.FromBlock(Block);
            RegisterGameTickListener(UpdateStep, 1200);
        }

        public override void OnBlockPlaced(ItemStack byItemStack)
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            plantedAt = Api.World.Calendar.TotalHours;
            failedSurvivalChecks = 0;
            requirements = PlantRequirements.FromBlock(block);

            if (Api.Side == EnumAppSide.Server)
            {
                double hours = block.Attributes["hours"].AsDouble(72);
                double spread = hours * 0.25;
                blossomAt = Api.World.Calendar.TotalHours + (hours * 0.75 + spread * Api.World.Rand.NextDouble());
            }
        }

        public void UpdateStep(float step)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (blossomAt > Api.World.Calendar.TotalHours) return;

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null) return;

            if (!eco.CanSurviveAt(Pos, requirements))
            {
                failedSurvivalChecks++;
                int maxFails = EcosystemConfig.Loaded.MaxFailedSurvivalChecks;
                if (failedSurvivalChecks >= maxFails)
                {
                    Api.World.BlockAccessor.BreakBlock(Pos, null);
                    return;
                }

                blossomAt += 18;
                MarkDirty();
                return;
            }

            AssetLocation matureLoc = PlantCodeHelper.MatureBlockLocation(Block);
            if (matureLoc == null)
            {
                Api.World.BlockAccessor.BreakBlock(Pos, null);
                return;
            }

            Block mature = Api.World.GetBlock(matureLoc);
            if (mature == null)
            {
                Api.World.BlockAccessor.BreakBlock(Pos, null);
                return;
            }

            Api.World.BlockAccessor.SetBlock(mature.Id, Pos);

            if (ShouldReproduce(Block))
            {
                eco.RegisterReproducer(Pos, Block.Code, requirements);
            }

            MarkDirty();
        }

        static bool ShouldReproduce(Block block)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return false;
            if (block.Attributes == null) return false;
            if (block.Attributes["ecologyReproduce"].AsBool(false)) return true;
            return block.Attributes["ecologyReproduceByType"].AsBool(false);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            double daysleft = (blossomAt - Api.World.Calendar.TotalHours) / Api.World.Calendar.HoursPerDay;
            if (daysleft >= 1) dsc.AppendLine((int)daysleft + " days until mature.");
            else dsc.AppendLine("Less than one day until mature.");

            if (!EcosystemConfig.Loaded.HarshWildPlants) return;

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null) return;

            IEnvironmentalContext ctx = eco.Sample(Pos);
            if (!eco.CanSurviveAt(Pos, requirements))
            {
                if (ctx.Temperature > requirements.MaxTemp && !ctx.InGreenhouse) dsc.AppendLine("Too hot — may die.");
                else if (ctx.Temperature < requirements.MinTemp && !ctx.InGreenhouse) dsc.AppendLine("Too cold — may die.");
                else dsc.AppendLine("Conditions poor — may die.");
            }
            else if (ctx.InGreenhouse) dsc.AppendLine("Greenhouse bonus!");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("plantedAt", plantedAt);
            tree.SetDouble("blossomAt", blossomAt);
            tree.SetInt("failedSurvivalChecks", failedSurvivalChecks);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            plantedAt = tree.GetDouble("plantedAt");
            blossomAt = tree.GetDouble("blossomAt");
            failedSurvivalChecks = tree.GetInt("failedSurvivalChecks");
            requirements = PlantRequirements.FromBlock(Block);
        }
    }
}
