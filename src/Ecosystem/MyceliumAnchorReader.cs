using System;
using System.Reflection;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Reads vanilla <c>BlockEntityMycelium</c> without a hard reference to game assemblies.</summary>
    internal static class MyceliumAnchorReader
    {
        static readonly object InitLock = new object();
        static FieldInfo mushroomBlockCodeField;

        public static bool IsMyceliumBlockEntity(BlockEntity be)
        {
            if (be == null) return false;
            string name = be.GetType().Name;
            return name == "BlockEntityMycelium" || name.Contains("Mycelium");
        }

        public static bool TryReadMushroomCode(BlockEntity be, out AssetLocation mushroomCode)
        {
            mushroomCode = null;
            if (!IsMyceliumBlockEntity(be)) return false;
            if (!EnsureResolved(be)) return false;

            object raw = mushroomBlockCodeField?.GetValue(be);
            if (raw is AssetLocation code && !code.Path.Contains("unknown", StringComparison.OrdinalIgnoreCase))
            {
                mushroomCode = code;
                return true;
            }

            return false;
        }

        static bool EnsureResolved(BlockEntity sample)
        {
            if (mushroomBlockCodeField != null) return true;

            lock (InitLock)
            {
                if (mushroomBlockCodeField != null) return true;

                Type type = sample.GetType();
                mushroomBlockCodeField = type.GetField(
                    "mushroomBlockCode",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                return mushroomBlockCodeField != null;
            }
        }
    }
}
