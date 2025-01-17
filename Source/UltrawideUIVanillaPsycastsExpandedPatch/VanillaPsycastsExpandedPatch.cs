using HarmonyLib;
using System.Reflection;
using UnityEngine;
using VanillaPsycastsExpanded.UI;
using Verse;

namespace UltrawideUI
{
    [StaticConstructorOnStartup]
    public class VanillaPsycastsExpandedPatch
    {
        static VanillaPsycastsExpandedPatch()
        {
            var harmony = new Harmony("VanillaExpanded.VPsycastsE");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        [HarmonyPatch(typeof(VanillaPsycastsExpanded.UI.ITab_Pawn_Psycasts), "OnOpen")]
        public static class VanillaPsycastsExpanded_UI_ITab_Pawn_Psycasts
        {
            [HarmonyPrefix]
            public static void Prefix(ITab_Pawn_Psycasts __instance)
            {
                FieldInfo sizeField = AccessTools.Field(typeof(ITab_Pawn_Psycasts), "size");
                Vector2 size = (Vector2)sizeField.GetValue(__instance);
                size.x = UI.screenWidth * UltrawideUI.UIWidth;
                sizeField.SetValue(__instance, size);
            }
        }
    }
}
