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
    public class FoodAlertPatch
    {

        static Type targetType = AccessTools.TypeByName("HarmonyPatches");
        static MethodInfo targetMethod = AccessTools.Method(targetType, "FoodCounter_NearDatePostfix");

        static FoodAlertPatch()
        {
            var harmony = new Harmony("UltrawideUI.FoodAlertPatch");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }
        static Type TargetMethod()
        {
            return AccessTools.TypeByName("HarmonyPatches");
        }

        // Days of food left
        [HarmonyPatch]
        public static class FoodAlert_HarmonyPatches_FoodCounter_NearDatePostfix_Patch
        {
            static MethodInfo TargetMethod()
            {
                var targetType = AccessTools.TypeByName("FoodAlert.HarmonyPatches");
                return targetType
                    .GetMethod("FoodCounter_NearDatePostfix",
                               BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(typeof(bool));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fieldInfo)
                    {
                        if (fieldInfo.Name == "screenWidth" && fieldInfo.DeclaringType.FullName == "Verse.UI")
                        {
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }
    }
}
