using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    public enum MeadowHarvestHandleResult
    {
        Pass,
        Handled,
    }

    public readonly struct MeadowHarvestBreakArgs
    {
        public MeadowHarvestBreakArgs(
            ICoreAPI api,
            IServerPlayer player,
            Block brokenBlock,
            BlockPos pos,
            ItemSlot activeSlot,
            bool isMowTool)
        {
            Api = api;
            Player = player;
            BrokenBlock = brokenBlock;
            Pos = pos;
            ActiveSlot = activeSlot;
            IsMowTool = isMowTool;
        }

        public ICoreAPI Api { get; }
        public IServerPlayer Player { get; }
        public Block BrokenBlock { get; }
        public BlockPos Pos { get; }
        public ItemSlot ActiveSlot { get; }
        public bool IsMowTool { get; }
    }

    public delegate MeadowHarvestHandleResult MeadowHarvestBreakHandler(MeadowHarvestBreakArgs args);

    /// <summary>
    /// Break-time meadow harvest hooks for herbalism / partial-harvest mods.
    /// Partial harvest on use (right-click) should use vanilla <c>DidUseBlock</c> or block behaviors instead.
    /// </summary>
    public static class MeadowHarvestRegistry
    {
        static readonly List<MeadowHarvestBreakHandler> handlers = new List<MeadowHarvestBreakHandler>();

        public static void Register(MeadowHarvestBreakHandler handler)
        {
            if (handler == null) return;
            handlers.Add(handler);
        }

        internal static MeadowHarvestHandleResult Invoke(MeadowHarvestBreakArgs args)
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                if (handlers[i](args) == MeadowHarvestHandleResult.Handled)
                {
                    return MeadowHarvestHandleResult.Handled;
                }
            }

            return MeadowHarvestHandleResult.Pass;
        }
    }

    internal static class MeadowHarvestModes
    {
        internal const string Whole = "whole";
        internal const string Delegate = "delegate";
        internal const string None = "none";

        internal static string Read(Block block)
        {
            if (block?.Attributes == null) return Whole;
            return block.Attributes["ecologyMeadowHarvest"].AsString(Whole)?.Trim() ?? Whole;
        }

        internal static bool SkipsModHarvest(string mode)
        {
            return mode.Equals(None, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool AllowsDefaultWholeDrop(string mode)
        {
            return mode.Equals(Whole, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(mode);
        }
    }
}
