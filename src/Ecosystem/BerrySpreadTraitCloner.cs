using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// VS 1.22+ spreads berry bushes via <see cref="IBlockAccessor.SetBlock(int, BlockPos, ItemStack)"/>;
    /// wild <c>OnBlockPlaced</c> assigns random traits. Match cutting maturation:
    /// <c>BEBehaviorFruitingBush.OnGrownFromCutting(traitsCsv)</c> (Vintagestory.GameContent).
    /// </summary>
    internal static class BerrySpreadTraitCloner
    {
        const string BehaviorFullName = "Vintagestory.GameContent.BEBehaviorFruitingBush";

        static readonly object InitLock = new object();
        static Type behaviorType;
        static MethodInfo onGrownFromCuttingMethod;
        static FieldInfo bStateField;
        static FieldInfo traitsField;

        public static bool TryCloneFromParent(ICoreAPI api, BlockPos parentPos, BlockPos offspringPos)
        {
            if (api?.World?.BlockAccessor == null) return false;
            if (api.Side != EnumAppSide.Server) return false;
            if (!EnsureResolved(api)) return false;

            BlockEntity parentBe = api.World.BlockAccessor.GetBlockEntity(parentPos);
            BlockEntity offspringBe = api.World.BlockAccessor.GetBlockEntity(offspringPos);
            object parentBh = TryGetBerryBehavior(parentBe);
            object childBh = TryGetBerryBehavior(offspringBe);

            if (parentBh == null || childBh == null) return false;

            string csv = TraitsToCsv(parentBh);
            onGrownFromCuttingMethod.Invoke(childBh, new object[] { csv });
            offspringBe.MarkDirty(true);
            return true;
        }

        static string TraitsToCsv(object berryBehavior)
        {
            object state = bStateField?.GetValue(berryBehavior);
            if (state == null) return string.Empty;

            object traitsRaw = traitsField?.GetValue(state);
            string[] traits = traitsRaw as string[];
            return traits != null && traits.Length > 0 ? string.Join(",", traits) : string.Empty;
        }

        static object TryGetBerryBehavior(BlockEntity entity)
        {
            if (entity == null || behaviorType == null) return null;

            foreach (BlockEntityBehavior b in EnumerateBehaviors(entity))
            {
                if (b != null && behaviorType.IsInstanceOfType(b)) return b;
            }

            return null;
        }

        static IEnumerable<BlockEntityBehavior> EnumerateBehaviors(BlockEntity entity)
        {
            PropertyInfo prop = typeof(BlockEntity).GetProperty("Behaviors", BindingFlags.Instance | BindingFlags.Public);
            object rawMap = prop?.GetValue(entity);
            if (rawMap == null)
                return Array.Empty<BlockEntityBehavior>();

            PropertyInfo valuesProp = rawMap.GetType().GetProperty("Values", BindingFlags.Instance | BindingFlags.Public);
            if (valuesProp?.GetValue(rawMap) is IEnumerable fromValues)
            {
                var list = new List<BlockEntityBehavior>();
                foreach (object x in fromValues)
                    if (x is BlockEntityBehavior b) list.Add(b);
                return list;
            }

            PropertyInfo vals = typeof(BlockEntity).GetProperty("OrderedBehaviors", BindingFlags.Instance | BindingFlags.Public);
            if (vals?.GetValue(entity) is IEnumerable ordered)
            {
                var list = new List<BlockEntityBehavior>();
                foreach (object x in ordered)
                    if (x is BlockEntityBehavior b) list.Add(b);
                    else if (x != null)
                    {
                        PropertyInfo kvVal = x.GetType().GetProperty("Value");
                        if (kvVal?.GetValue(x) is BlockEntityBehavior b2) list.Add(b2);
                    }
                return list;
            }

            return Array.Empty<BlockEntityBehavior>();
        }

        static bool EnsureResolved(ICoreAPI api)
        {
            if (behaviorType != null && onGrownFromCuttingMethod != null
                && bStateField != null && traitsField != null) return true;

            lock (InitLock)
            {
                if (behaviorType != null && onGrownFromCuttingMethod != null
                    && bStateField != null && traitsField != null) return true;

                ResolveTypes();
                if (behaviorType == null)
                {
                    if (EcosystemConfig.Loaded.VerboseLogging)
                        api.Logger.Notification("[ecosystemflora] Berry trait clone: type {0} not found (game version?)", BehaviorFullName);
                    return false;
                }

                onGrownFromCuttingMethod = behaviorType.GetMethod(
                    "OnGrownFromCutting",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { typeof(string) },
                    modifiers: null);

                bStateField = behaviorType.GetField("BState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Type bStateType = bStateField?.FieldType;
                traitsField = bStateType?.GetField("Traits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (onGrownFromCuttingMethod == null || bStateField == null || traitsField == null)
                {
                    if (EcosystemConfig.Loaded.VerboseLogging)
                        api.Logger.Notification("[ecosystemflora] Berry trait clone: reflection bind failed for {0}", BehaviorFullName);
                    behaviorType = null;
                    onGrownFromCuttingMethod = null;
                    bStateField = null;
                    traitsField = null;
                    return false;
                }
            }

            return true;
        }

        static void ResolveTypes()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try
                {
                    t = asm.GetType(BehaviorFullName, throwOnError: false);
                }
                catch
                {
                    // ignore assembly load edge cases
                }

                if (t != null)
                {
                    behaviorType = t;
                    return;
                }
            }
        }
    }
}
