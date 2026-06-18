using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Read-only column access for ecology registration scans (live accessor or chunk snapshot).</summary>
    internal interface IRegistrationColumnView
    {
        bool SupportsFoliageMutation { get; }

        int MapSizeY { get; }

        IMapChunk GetMapChunk(Vec2i chunkCoord);

        bool IsValidPos(int x, int y, int z);

        Block GetBlock(int x, int y, int z);
    }
}
