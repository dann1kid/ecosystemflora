using System;
using System.IO;
using HarmonyLib;
using Vintagestory.GameContent;
using WildFarming.Ecosystem.Harmony;
using Xunit;

namespace WildFarming.Tests
{
    public class MyceliumHarmonyPatchTests
    {
        [Fact]
        public void Harmony_mycelium_transpiler_applies_on_installed_game_dll()
        {
            string survivalDll = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Vintagestory", "Mods", "VSSurvivalMod.dll");

            if (!File.Exists(survivalDll))
            {
                return;
            }

            var harmony = new HarmonyLib.Harmony("wildfarming.tests.mycelium." + Guid.NewGuid());
            harmony.CreateClassProcessor(typeof(MyceliumCapGrowthPatches)).Patch();

            var method = AccessTools.DeclaredMethod(typeof(BlockEntityMycelium), "generateUpGrowingMushrooms");
            Assert.NotNull(method);
        }
    }
}
