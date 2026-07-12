using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WildFarming.Ecosystem.Harmony
{
    /// <summary>
    /// Extends vanilla cap regrowth so meadow flora can be displaced (grass/flowers do not block caps).
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityMycelium), "generateUpGrowingMushrooms")]
    internal static class MyceliumCapGrowthPatches
    {
        static readonly MethodInfo GetIdMethod = AccessTools.PropertyGetter(typeof(CollectibleObject), nameof(CollectibleObject.Id));

        static readonly MethodInfo DisplacementGateMethod = AccessTools.Method(
            typeof(MyceliumCapPlacement),
            nameof(MyceliumCapPlacement.PassesModDisplacementGate));

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            if (!TryPatchPlacementGate(codes, generator))
            {
                EcosystemHarmony.MarkTranspilerMiss("generateUpGrowingMushrooms");
            }

            return codes;
        }

        static bool TryPatchPlacementGate(List<CodeInstruction> codes, ILGenerator generator)
        {
            int setBlockIdx = FindMushroomSetBlockIndex(codes);
            if (setBlockIdx < 0) return false;

            if (!TryFindOccupiedBranch(codes, setBlockIdx, out int branchIdx, out Label skipLabel))
            {
                return false;
            }

            if (!TryFindHereBlockLocal(codes, branchIdx, out CodeInstruction loadHereBlock))
            {
                return false;
            }

            int setBlockEntryIdx = branchIdx + 1;
            var setBlockLabel = generator.DefineLabel();
            codes[setBlockEntryIdx].WithLabels(setBlockLabel);

            // Vanilla: brtrue skip when Id != 0. Invert: Id == 0 -> setblock; else try displacement.
            codes[branchIdx].opcode = OpCodes.Brfalse;
            codes[branchIdx].operand = setBlockLabel;

            codes.InsertRange(
                branchIdx + 1,
                new[]
                {
                    loadHereBlock,
                    new CodeInstruction(OpCodes.Call, DisplacementGateMethod),
                    new CodeInstruction(OpCodes.Brtrue, setBlockLabel),
                    new CodeInstruction(OpCodes.Br, skipLabel),
                });

            return true;
        }

        static int FindMushroomSetBlockIndex(List<CodeInstruction> codes)
        {
            for (int i = 0; i < codes.Count; i++)
            {
                if (!IsSetBlockCall(codes[i])) continue;
                if (!LoadsMushroomBlockId(codes, i)) continue;
                return i;
            }

            return -1;
        }

        static bool LoadsMushroomBlockId(List<CodeInstruction> codes, int setBlockIndex)
        {
            for (int j = setBlockIndex - 1; j >= 0 && j >= setBlockIndex - 8; j--)
            {
                if (codes[j].Calls(GetIdMethod))
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsSetBlockCall(CodeInstruction ins)
        {
            if (ins?.opcode != OpCodes.Callvirt && ins?.opcode != OpCodes.Call) return false;
            if (ins.operand is not MethodInfo mi) return false;
            if (mi.Name != nameof(IBlockAccessor.SetBlock)) return false;

            ParameterInfo[] ps = mi.GetParameters();
            return ps.Length == 2
                && ps[0].ParameterType == typeof(int)
                && ps[1].ParameterType == typeof(BlockPos);
        }

        static bool TryFindOccupiedBranch(
            List<CodeInstruction> codes,
            int setBlockIndex,
            out int branchIdx,
            out Label skipLabel)
        {
            branchIdx = -1;
            skipLabel = default;

            for (int j = setBlockIndex - 1; j >= 0 && j >= setBlockIndex - 12; j--)
            {
                if (codes[j].opcode != OpCodes.Brtrue && codes[j].opcode != OpCodes.Brtrue_S)
                {
                    continue;
                }

                if (!LoadsBlockIdBefore(codes, j))
                {
                    continue;
                }

                if (codes[j].operand is not Label label)
                {
                    continue;
                }

                branchIdx = j;
                skipLabel = label;
                return true;
            }

            return false;
        }

        static bool LoadsBlockIdBefore(List<CodeInstruction> codes, int branchIndex)
        {
            for (int k = branchIndex - 1; k >= 0 && k >= branchIndex - 4; k--)
            {
                if (codes[k].Calls(GetIdMethod))
                {
                    return true;
                }
            }

            return false;
        }

        static bool TryFindHereBlockLocal(List<CodeInstruction> codes, int beforeIndex, out CodeInstruction loadHereBlock)
        {
            loadHereBlock = null;
            MethodInfo getBlockMethod = AccessTools.Method(
                typeof(IBlockAccessor),
                nameof(IBlockAccessor.GetBlock),
                new[] { typeof(BlockPos) });

            for (int j = beforeIndex - 1; j >= 0 && j >= beforeIndex - 40; j--)
            {
                if (!codes[j].Calls(getBlockMethod)) continue;
                if (!TryCreateMatchingLoad(codes[j + 1], out loadHereBlock)) continue;
                return true;
            }

            return false;
        }

        static bool TryCreateMatchingLoad(CodeInstruction store, out CodeInstruction load)
        {
            load = null;
            if (store == null) return false;

            if (store.opcode == OpCodes.Stloc_S)
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, store.operand);
                return true;
            }

            if (store.opcode == OpCodes.Stloc)
            {
                load = new CodeInstruction(OpCodes.Ldloc, store.operand);
                return true;
            }

            return false;
        }
    }
}
