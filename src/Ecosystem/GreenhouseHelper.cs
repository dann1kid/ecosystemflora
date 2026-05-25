using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Detects enclosed greenhouse rooms when survival mod systems are present.</summary>
    public static class GreenhouseHelper
    {
        static bool resolved;
        static ModSystem cachedRoomRegistry;
        static MethodInfo getRoomMethod;
        static PropertyInfo skylightProp;
        static PropertyInfo nonSkylightProp;
        static PropertyInfo exitCountProp;
        static readonly object[] invokeArgs = new object[1];

        public static bool IsGreenhouse(ICoreAPI api, BlockPos pos)
        {
            if (!resolved)
            {
                Resolve(api);
            }

            if (getRoomMethod == null) return false;

            invokeArgs[0] = pos;
            object room = getRoomMethod.Invoke(cachedRoomRegistry, invokeArgs);
            if (room == null) return false;

            int skylight = (int)(skylightProp?.GetValue(room) ?? 0);
            int nonSkylight = (int)(nonSkylightProp?.GetValue(room) ?? 0);
            int exits = (int)(exitCountProp?.GetValue(room) ?? 1);

            return skylight > nonSkylight && exits == 0;
        }

        static void Resolve(ICoreAPI api)
        {
            resolved = true;
            cachedRoomRegistry = api.ModLoader.GetModSystem("RoomRegistry");
            if (cachedRoomRegistry == null) return;

            getRoomMethod = cachedRoomRegistry.GetType().GetMethod("GetRoomForPosition");
            if (getRoomMethod == null) return;

            System.Type returnType = getRoomMethod.ReturnType;
            skylightProp = returnType.GetProperty("SkylightCount");
            nonSkylightProp = returnType.GetProperty("NonSkylightCount");
            exitCountProp = returnType.GetProperty("ExitCount");
        }
    }
}
