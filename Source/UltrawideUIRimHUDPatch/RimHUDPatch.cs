using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using RimWorld.Planet;
using System.Linq;
using System.Net;

namespace UltrawideUI
{
    [StaticConstructorOnStartup]
    public class RimHUDPatch
    {
        static RimHUDPatch()
        {
            var harmony = new Harmony("UltrawideUI.RimHUDPatch");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        // Gear, logs, health,etc tabs for RimHUD
        [HarmonyPatch(typeof(RimHUD.Interface.Screen.InspectPaneTabs), "Draw")]
        public static class RimHUD_InspectPaneTabs_Draw_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool didNum = false;
                int counter = 0;
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (!didNum && codes[i].opcode == OpCodes.Ldloc_1)
                    {
                        didNum = true;
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.ScreenWidthFieldInfo));
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.LeftMultiplierFieldInfo));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 5, new CodeInstruction(OpCodes.Mul));
                        codes.Insert(i + 6, new CodeInstruction(OpCodes.Add));
                    }
                    else if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0f && ++counter == 2)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.ScreenWidthFieldInfo);
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.LeftMultiplierFieldInfo));
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                        break;
                    }
                }
                //Log.Error("RimHUD patched");
                return codes.AsEnumerable();
            }
        }
    }
}
