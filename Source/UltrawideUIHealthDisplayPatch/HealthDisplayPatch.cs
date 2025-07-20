using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace UltrawideUI
{
    [StaticConstructorOnStartup]
    public class HealthDisplayPatch
    {

        static Type targetType = AccessTools.TypeByName("HarmonyPatches");
        static MethodInfo targetMethod = AccessTools.Method(targetType, "HealthDisplayWindow_UpdateWindowSize");

        static HealthDisplayPatch()
        {
            var harmony = new Harmony("UltrawideUI.HealthDisplayPatch");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }
        static Type TargetMethod()
        {
            return AccessTools.TypeByName("HarmonyPatches");
        }

        [HarmonyPatch]
        public static class HealthDisplay_HarmonyPatches_HealthDisplayWindow_UpdateWindowSize_Patch
        {
            static MethodInfo TargetMethod()
            {
                var targetType = AccessTools.TypeByName("GTHealthDisplay.HealthDisplayWindow");
                return targetType.GetMethod("UpdateWindowSize", BindingFlags.NonPublic | BindingFlags.Static);
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0f)
                    {
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.ScreenWidthFieldInfo));
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.LeftMultiplierFieldInfo));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 5, new CodeInstruction(OpCodes.Mul));
                        codes.RemoveAt(i);
                        break;
                    }
                }
                return codes.AsEnumerable();
            }
        }
    }
}
