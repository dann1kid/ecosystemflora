using System;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Spawns vanilla <c>Mycelium</c> block entities for network spread.</summary>
    internal static class MyceliumAnchorSpawner
    {
        static readonly object InitLock = new object();
        static MethodInfo onGeneratedMethod;
        static bool initFailed;

        public static bool TrySpawnAnchor(
            ICoreAPI api,
            BlockPos groundPos,
            AssetLocation mushroomCode,
            out string failureReason)
        {
            failureReason = null;
            if (api?.World?.BlockAccessor == null || groundPos == null || mushroomCode == null)
            {
                failureReason = "invalid args";
                return false;
            }

            IBlockAccessor acc = api.World.BlockAccessor;
            if (WildSoilGroundRules.HasActiveMycelium(acc, groundPos))
            {
                failureReason = "already anchored";
                return false;
            }

            Block groundBlock = acc.GetBlock(groundPos);
            MyceliumNiche spreadingNiche = MyceliumEcology.ClassifyNiche(mushroomCode, groundBlock);

            if (!MyceliumPlacement.CanSpreadInto(api, groundPos, spreadingNiche, out failureReason))
            {
                return false;
            }

            Block mushroomBlock = api.World.GetBlock(mushroomCode);
            if (mushroomBlock == null || mushroomBlock.Id == 0)
            {
                failureReason = "mushroom block missing";
                return false;
            }

            if (!MyceliumStressEvaluator.MeetsSurvival(
                api,
                groundPos,
                BuildProbeRequirements(mushroomCode, acc.GetBlock(groundPos))))
            {
                failureReason = "niche unsuitable";
                return false;
            }

            acc.SpawnBlockEntity("Mycelium", groundPos);
            BlockEntity be = acc.GetBlockEntity(groundPos);
            if (!MyceliumAnchorReader.IsMyceliumBlockEntity(be))
            {
                acc.RemoveBlockEntity(groundPos);
                failureReason = "BE spawn failed";
                return false;
            }

            if (!TryInitializeGenerated(api, be, mushroomBlock))
            {
                acc.RemoveBlockEntity(groundPos);
                failureReason = "BE init failed";
                return false;
            }

            acc.MarkBlockEntityDirty(groundPos);
            return true;
        }

        static PlantRequirements BuildProbeRequirements(AssetLocation mushroomCode, Block anchorBlock)
        {
            MyceliumEcology.TryBuildRequirements(mushroomCode, anchorBlock, out PlantRequirements req);
            return req;
        }

        static bool TryInitializeGenerated(ICoreAPI api, BlockEntity be, Block mushroomBlock)
        {
            if (initFailed) return false;
            if (!EnsureResolved(be)) return false;

            try
            {
                onGeneratedMethod.Invoke(be, new object[] { api.World.BlockAccessor, api.World.Rand, mushroomBlock });
                return MyceliumAnchorReader.TryReadMushroomCode(be, out _);
            }
            catch
            {
                return false;
            }
        }

        static bool EnsureResolved(BlockEntity sample)
        {
            if (onGeneratedMethod != null) return true;
            if (initFailed) return false;

            lock (InitLock)
            {
                if (onGeneratedMethod != null) return true;
                if (initFailed) return false;

                Type type = sample.GetType();
                onGeneratedMethod = type.GetMethod(
                    "OnGenerated",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    types: new[] { typeof(IBlockAccessor), typeof(IRandom), typeof(Block) },
                    modifiers: null);

                if (onGeneratedMethod == null)
                {
                    onGeneratedMethod = type.GetMethod(
                        "OnGenerated",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                if (onGeneratedMethod == null)
                {
                    initFailed = true;
                    return false;
                }

                return true;
            }
        }
    }
}
