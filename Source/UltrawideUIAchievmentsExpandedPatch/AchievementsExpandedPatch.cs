using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace UltrawideUI
{
    [StaticConstructorOnStartup]
    public class AchievementsExpandedPatch
    {
        static AchievementsExpandedPatch()
        {
            var harmony = new Harmony("UltrawideUI.AchievementsExpandedPatch");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        [HarmonyPatch(typeof(AchievementsExpanded.MainTabWindow_Achievements), "DoWindowContents")]
        public static class AchievementExpanded_MainTabWindow_Achievements_DoWindowContents
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fieldInfo)
                    {
                        if (fieldInfo.Name == "screenWidth" && fieldInfo.DeclaringType.FullName == "Verse.UI")
                        {
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.UIWidthFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(AchievementsExpanded.MainTabWindow_Achievements), "PreOpen")]
        public static class AchievementExpanded_MainTabWindow_Achievements_PreOpen
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fieldInfo)
                    {
                        if (fieldInfo.Name == "screenWidth" && fieldInfo.DeclaringType.FullName == "Verse.UI")
                        {
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.UIWidthFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }
        [HarmonyPatch(typeof(AchievementsExpanded.MainTabWindow_Achievements), "get_InitialSize")]
        public static class AchievementExpanded_MainTabWindow_Achievements_get_InitialSize
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fieldInfo)
                    {
                        if (fieldInfo.Name == "screenWidth" && fieldInfo.DeclaringType.FullName == "Verse.UI")
                        {
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, UltrawideUI.UIWidthFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }
        [HarmonyPatch(typeof(AchievementsExpanded.AchievementNotification), "get_WindowPosition")]
        public static class AchievementExpanded_AchievementNotification_get_WindowPosition
        {
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
