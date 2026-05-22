using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Detects enclosed greenhouse rooms when survival mod systems are present.</summary>
    public static class GreenhouseHelper
    {
        public static bool IsGreenhouse(ICoreAPI api, BlockPos pos)
        {
            ModSystem sys = api.ModLoader.GetModSystem("RoomRegistry");
            if (sys == null) return false;

            // RoomRegistry lives in game assemblies; resolved at runtime to avoid a hard mod reference.
            System.Reflection.MethodInfo method = sys.GetType().GetMethod("GetRoomForPosition");
            if (method == null) return false;

            object room = method.Invoke(sys, new object[] { pos });
            if (room == null) return false;

            System.Type roomType = room.GetType();
            int skylight = (int)(roomType.GetProperty("SkylightCount")?.GetValue(room) ?? 0);
            int nonSkylight = (int)(roomType.GetProperty("NonSkylightCount")?.GetValue(room) ?? 0);
            int exits = (int)(roomType.GetProperty("ExitCount")?.GetValue(room) ?? 1);

            return skylight > nonSkylight && exits == 0;
        }
    }
}
