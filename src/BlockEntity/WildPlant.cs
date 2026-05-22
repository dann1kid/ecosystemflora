using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;

namespace WildFarming
{
    public class WildPlantBlockEntity : BlockEntity
    {
        const double SurvivalRecheckHours = 18;

        double plantedAt;
        double blossomAt;
        double nextStressCheckAt;
        int failedSurvivalChecks;
        bool hasMatured;
        PlantRequirements requirements;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            requirements = PlantRequirements.FromBlock(Block);
            RegisterGameTickListener(UpdateStep, 1200);

            if (api.Side == EnumAppSide.Server
                && EcosystemConfig.Loaded.ReproduceDebug
                && !hasMatured
                && Block?.Code?.Path?.StartsWith("wildplant-") == true)
            {
                api.Logger.Notification("[wildfarming] WildPlant BE at {0} ({1})", Pos, Block.Code);
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack)
        {
            InitGrowthSchedule();
        }

        public void InitFromReproduction()
        {
            InitGrowthSchedule();
        }

        void InitGrowthSchedule()
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            plantedAt = Api.World.Calendar.TotalHours;
            failedSurvivalChecks = 0;
            nextStressCheckAt = 0;
            requirements = PlantRequirements.FromBlock(block);

            if (Api.Side == EnumAppSide.Server)
            {
                double hours = block.Attributes["hours"].AsDouble(72);
                if (hours <= 0)
                {
                    hours = ResolveGrowHours(block);
                }

                double spread = hours * 0.25;
                hours *= EcosystemConfig.Loaded.GrowthHoursMultiplier;
                spread *= EcosystemConfig.Loaded.GrowthHoursMultiplier;
                blossomAt = Api.World.Calendar.TotalHours + (hours * 0.75 + spread * Api.World.Rand.NextDouble());
                MarkDirty();
            }
        }

        static double ResolveGrowHours(Block block)
        {
            if (block.Variant != null
                && block.Variant.TryGetValue("flower", out string species)
                && WildFlowerClimate.TryGet(species, out _, out _, out int growHours))
            {
                return growHours;
            }

            return 192;
        }

        public void UpdateStep(float step)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (hasMatured) return;

            double now = Api.World.Calendar.TotalHours;
            if (now < blossomAt) return;
            if (failedSurvivalChecks > 0 && now < nextStressCheckAt) return;

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null)
            {
                Api.Logger.Warning("[wildfarming] EcosystemSystem.Instance is null — plant at {0} cannot mature", Pos);
                return;
            }

            requirements = PlantRequirements.FromBlock(Block);

            if (!eco.CanSurviveAt(Pos, requirements))
            {
                failedSurvivalChecks++;
                int maxFails = EcosystemConfig.Loaded.MaxFailedSurvivalChecks;
                if (failedSurvivalChecks >= maxFails)
                {
                    Api.World.BlockAccessor.SetBlock(0, Pos);
                    return;
                }

                nextStressCheckAt = now + SurvivalRecheckHours;
                MarkDirty();
                return;
            }

            Block wildBlock = Block;
            PlantRequirements matureRequirements = PlantRequirements.FromBlock(wildBlock);
            AssetLocation matureLoc = PlantCodeHelper.MatureBlockLocation(wildBlock);
            AssetLocation juvenileCode = wildBlock.Code;

            if (matureLoc == null)
            {
                Api.World.BlockAccessor.SetBlock(0, Pos);
                return;
            }

            Block mature = Api.World.GetBlock(matureLoc);
            if (mature == null)
            {
                Api.World.BlockAccessor.SetBlock(0, Pos);
                return;
            }

            // Register BEFORE SetBlock: replacing this block destroys the BE and can abort UpdateStep.
            if (EcologyAttributes.ReproduceEnabled(wildBlock))
            {
                Api.Logger.Warning("[wildfarming] Maturing {0} at {1} — registering reproducer", wildBlock.Code, Pos);
                eco.RegisterReproducer(Pos, juvenileCode, matureRequirements);
            }

            hasMatured = true;
            Api.World.BlockAccessor.SetBlock(mature.Id, Pos);
            MarkDirty();
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            double now = Api.World.Calendar.TotalHours;
            double hoursLeft = blossomAt - now;

            if (failedSurvivalChecks > 0 && now < nextStressCheckAt)
            {
                double waitDays = (nextStressCheckAt - now) / Api.World.Calendar.HoursPerDay;
                dsc.AppendLine("Poor conditions — check again in " + (int)waitDays + " days (" + failedSurvivalChecks + "/" + EcosystemConfig.Loaded.MaxFailedSurvivalChecks + ").");
            }
            else if (hoursLeft > Api.World.Calendar.HoursPerDay)
            {
                dsc.AppendLine((int)(hoursLeft / Api.World.Calendar.HoursPerDay) + " days until mature.");
            }
            else if (hoursLeft > 0)
            {
                dsc.AppendLine("Less than one day until mature.");
            }
            else
            {
                dsc.AppendLine("Ready to mature.");
            }

            if (!EcosystemConfig.Loaded.HarshWildPlants) return;

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null) return;

            string fail = eco.DescribeSurvivalAt(Pos, requirements);
            if (fail != null) dsc.AppendLine(fail + " — may die.");
            else if (eco.Sample(Pos).InGreenhouse) dsc.AppendLine("Greenhouse bonus!");

            if (EcologyAttributes.ReproduceEnabled(Block))
            {
                dsc.AppendLine("When mature, may spread as separate seedlings nearby.");
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("plantedAt", plantedAt);
            tree.SetDouble("blossomAt", blossomAt);
            tree.SetDouble("nextStressCheckAt", nextStressCheckAt);
            tree.SetInt("failedSurvivalChecks", failedSurvivalChecks);
            tree.SetBool("hasMatured", hasMatured);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            plantedAt = tree.GetDouble("plantedAt");
            blossomAt = tree.GetDouble("blossomAt");
            nextStressCheckAt = tree.GetDouble("nextStressCheckAt");
            failedSurvivalChecks = tree.GetInt("failedSurvivalChecks");
            hasMatured = tree.GetBool("hasMatured");
            requirements = PlantRequirements.FromBlock(Block);
        }
    }
}
