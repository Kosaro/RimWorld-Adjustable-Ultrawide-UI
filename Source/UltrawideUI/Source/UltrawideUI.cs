
using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using System.Reflection.Emit;
using System.Reflection;
using RimWorld.Planet;
using System.Linq;
using HugsLib;

namespace UltrawideUI
{
    [StaticConstructorOnStartup]
    public class UltrawideUI : ModBase
    {
        public static float UIWidth = 1f;
        public static float LeftMultiplier = 0f;
        public static float RightMultiplier = 1f;
        private static float ResourceListMultiplier = 0f;

        public static readonly FieldInfo UIWidthFieldInfo = typeof(UltrawideUI).GetField(nameof(UltrawideUI.UIWidth), BindingFlags.Static | BindingFlags.Public);
        public static readonly FieldInfo LeftMultiplierFieldInfo = typeof(UltrawideUI).GetField(nameof(UltrawideUI.LeftMultiplier), BindingFlags.Static | BindingFlags.Public);
        public static readonly FieldInfo RightMultiplierFieldInfo = typeof(UltrawideUI).GetField(nameof(UltrawideUI.RightMultiplier), BindingFlags.Static | BindingFlags.Public);
        public static readonly FieldInfo ScreenWidthFieldInfo = typeof(UI).GetField(nameof(UI.screenWidth), BindingFlags.Static | BindingFlags.Public);
        private static readonly FieldInfo ResourceListMultiplierFieldInfo = typeof(UltrawideUI).GetField(nameof(UltrawideUI.ResourceListMultiplier), BindingFlags.Static | BindingFlags.NonPublic);

        private static HugsLib.Settings.SettingHandle<bool> IgnoreResourceList;

        public override string ModIdentifier
        {
            get { return "AdjustableUltrawideUI"; }
        }

        public override void DefsLoaded()
        {
            var UIWidthHandle = Settings.GetHandle<float>("UIWidth", "UI width", "Percentage of the screen's width that the UI uses", 1f);
            IgnoreResourceList = Settings.GetHandle<bool>("IgnoreResourceList", "Ignore resource list", "Turn on to keep the resource list on the edge of the screen", false);

            UpdateVariables(UIWidthHandle.Value);
            UIWidthHandle.ValueChanged += handle =>
            {
                UpdateVariables(UIWidthHandle.Value);
            };
            UIWidthHandle.CustomDrawer = rect =>
            {
                var textFieldWidth = 40f;
                UIWidthHandle.Value = Widgets.HorizontalSlider(new Rect(rect.x, rect.y, rect.width - textFieldWidth, rect.height), UIWidthHandle, .33f, 1f);
                //UIWidthHandle.Value = Widgets.HorizontalSlider_NewTemp(new Rect(rect.x, rect.y, rect.width - textFieldWidth, rect.height), UIWidthHandle, .33f, 1f);
                var labelText = String.Format("{0,5}", (int)(UIWidthHandle.Value * 100) + "%");
                Widgets.Label(new Rect(rect.x + rect.width - textFieldWidth, rect.y, textFieldWidth, rect.height), labelText);
                return false;
            };

            IgnoreResourceList.ValueChanged += handle =>
            {
                ResourceListMultiplier = IgnoreResourceList ? 0f : LeftMultiplier;
            };
        }
        public static void UpdateVariables(float uiWidth)
        {
            UIWidth = uiWidth;
            LeftMultiplier = 0.5f - uiWidth / 2;
            RightMultiplier = 0.5f + uiWidth / 2;
            ResourceListMultiplier = IgnoreResourceList ? 0f : LeftMultiplier;
        }

        // Tab after pressing button on bottom bar
        [HarmonyPatch(typeof(RimWorld.MainTabWindow), "SetInitialSizeAndPosition")]
        public static class MainTabWindow_SetInitialSizeAndPosition_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(MainTabWindow __instance)
            {
                if (__instance.Anchor == MainTabWindowAnchor.Left)
                {
                    __instance.windowRect.x = UI.screenWidth * LeftMultiplier;
                }
                else
                {
                    __instance.windowRect.x = (float)UI.screenWidth * RightMultiplier - __instance.windowRect.width;
                }
            }
        }

        // Research pane
        [HarmonyPatch(typeof(RimWorld.MainTabWindow_Research), "DoWindowContents")]
        public static class MainTabWindow_Research_DoWindowContents_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(MainTabWindow __instance)
            {
                __instance.windowRect.width = UI.screenWidth * UIWidth;
            }
        }

        // Botttom bar
        [HarmonyPatch(typeof(RimWorld.MainButtonsRoot), "DoButtons")]
        public static class MainButtonsRoot_DoButtons_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool didXOffset = false;
                int xOffsetCounter = 0;
                int widthCounter = 0;
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (!didXOffset && codes[i].opcode == OpCodes.Ldc_I4_0)
                    {
                        if (++xOffsetCounter == 2)
                        {
                            didXOffset = true;
                            codes[i] = new CodeInstruction(OpCodes.Ldsfld, ScreenWidthFieldInfo);
                            codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, LeftMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            codes.Insert(i + 5, new CodeInstruction(OpCodes.Conv_I4));
                            i += 6;
                        }
                    }
                    else if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fieldInfo)
                    {
                        if (fieldInfo.Name == "screenWidth" && fieldInfo.DeclaringType.FullName == "Verse.UI")
                        {
                            widthCounter++;
                            if (widthCounter == 1)
                            {
                                codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, UIWidthFieldInfo));
                                codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            }
                            else if (widthCounter == 2)
                            {
                                codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                                codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                                codes.Insert(i + 5, new CodeInstruction(OpCodes.Conv_I4));
                                break;
                            }
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Skill menu and other stuff
        [HarmonyPatch(typeof(Verse.InspectTabBase), "get_TabRect")]
        public static class InspectTabBase_GetTabRect_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ref Rect __result)
            {
                __result.x = UI.screenWidth * LeftMultiplier;
            }
        }

        // World inspect pane
        [HarmonyPatch(typeof(RimWorld.Planet.WorldInspectPane), "SetInitialSizeAndPosition")]
        public static class WorldInspectPane_SetInitialSizeAndPosition_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(WorldInspectPane __instance)
            {
                __instance.windowRect.x = UI.screenWidth * LeftMultiplier;
            }
        }

        // Gizmos (draft, buildable items, etc)
        [HarmonyPatch(typeof(Verse.GizmoGridDrawer), "DrawGizmoGrid")]
        public static class GizmoGridDrawer_DrawGizmoGrid_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(ref float startX)
            {
                startX += UI.screenWidth * LeftMultiplier;
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
                            codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            codes.Insert(i + 6, new CodeInstruction(OpCodes.Conv_R4));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Global controls bottom right
        [HarmonyPatch(typeof(RimWorld.GlobalControls), "GlobalControlsOnGUI")]
        public static class GlobalControls_GlobalControlsOnGUI_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                int counter = 0;
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fieldInfo)
                    {
                        if (fieldInfo.Name == "screenWidth" && fieldInfo.DeclaringType.FullName == "Verse.UI")
                        {
                            if (++counter == 1)
                            {
                                codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                                codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            }
                            else if (counter == 2)
                            {
                                codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                                codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                                codes.Insert(i + 5, new CodeInstruction(OpCodes.Conv_I4));
                            }
                            else if (counter == 3)
                            {
                                codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                                codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                                break;
                            }
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }


        // Global controls bottom right world
        [HarmonyPatch(typeof(RimWorld.Planet.WorldGlobalControls), "WorldGlobalControlsOnGUI")]
        public static class WorldGlobalControls_WorldGlobalControlsOnGUI_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var counter = 0;
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fieldInfo)
                    {
                        if (fieldInfo.Name == "screenWidth" && fieldInfo.DeclaringType.FullName == "Verse.UI")
                        {
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            if (++counter == 2)
                            {
                                break;
                            }
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Global route planner button
        [HarmonyPatch(typeof(RimWorld.Planet.WorldRoutePlanner), "DoRoutePlannerButton")]
        public static class WorldRoutePlanner_DoRoutePlannerButton_Patch
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
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Resources top left
        [HarmonyPatch(typeof(RimWorld.ResourceReadout), "ResourceReadoutOnGUI")]
        public static class ResourceReadout_ResourceReadoutOnGUI_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var didList = false;
                var didCategorized = false;
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (didList && didCategorized)
                    {
                        break;
                    }
                    if (!didList && codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 7f)
                    {
                        didList = true;
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, ScreenWidthFieldInfo));
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Ldsfld, ResourceListMultiplierFieldInfo));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 5, new CodeInstruction(OpCodes.Mul));
                        codes.Insert(i + 6, new CodeInstruction(OpCodes.Add));
                    }
                    if (!didCategorized && codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 2f)
                    {
                        didCategorized = true;
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, ScreenWidthFieldInfo));
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Ldsfld, ResourceListMultiplierFieldInfo));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 5, new CodeInstruction(OpCodes.Mul));
                        codes.Insert(i + 6, new CodeInstruction(OpCodes.Add));
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Toggle options bottom right
        [HarmonyPatch(typeof(RimWorld.GlobalControlsUtility), "DoPlaySettings")]
        public static class GlobalControlsUtility_DoPlaySettings_Patch
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
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Alerts, white text on  right side e.g. break risk
        [HarmonyPatch(typeof(RimWorld.Alert), "DrawAt")]
        public static class Alert_DrawAt_Patch
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
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Alerts info pane
        [HarmonyPatch(typeof(RimWorld.Alert), "DrawInfoPane")]
        public static class Alert_DrawInfoPane_Patch
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
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Alerts info pane shadow
        [HarmonyPatch(typeof(RimWorld.AlertsReadout), "AlertsReadoutOnGUI")]
        public static class AlertsReadout_AlertsReadout_Patch
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
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Mouseover info bottom right
        [HarmonyPatch(typeof(Verse.MouseoverReadout), "MouseoverReadoutOnGUI")]
        public static class MouseoverReadout_MouseOverReadoutOnGUI_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var counter = 0;
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsflda)
                    {
                        if (counter++ % 2 == 0)
                        {
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, ScreenWidthFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldsfld, LeftMultiplierFieldInfo));
                            codes.Insert(i + 5, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 6, new CodeInstruction(OpCodes.Mul));
                            codes.Insert(i + 7, new CodeInstruction(OpCodes.Add));
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Gear, logs, health,etc tabs
        [HarmonyPatch(typeof(RimWorld.InspectPaneUtility), "DoTabs")]
        public static class InspectPaneUtility_DoTabs_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool didTabs = false;
                int counter = 0;
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (!didTabs && codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 72f)
                    {
                        didTabs = true;
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, ScreenWidthFieldInfo));
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldsfld, LeftMultiplierFieldInfo));
                        codes.Insert(i + 5, new CodeInstruction(OpCodes.Conv_R4));
                        codes.Insert(i + 6, new CodeInstruction(OpCodes.Mul));
                        codes.Insert(i + 7, new CodeInstruction(OpCodes.Add));
                    }
                    else if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0f)
                    {
                        if (++counter == 2)
                        {
                            codes[i] = new CodeInstruction(OpCodes.Ldsfld, ScreenWidthFieldInfo);
                            codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, LeftMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));

                            codes.Insert(i + 9, new CodeInstruction(OpCodes.Ldsfld, ScreenWidthFieldInfo));
                            codes.Insert(i + 10, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 11, new CodeInstruction(OpCodes.Ldsfld, LeftMultiplierFieldInfo));
                            codes.Insert(i + 12, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 13, new CodeInstruction(OpCodes.Mul));
                            codes.Insert(i + 14, new CodeInstruction(OpCodes.Sub));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Learning helper
        [HarmonyPatch(typeof(RimWorld.LearningReadout), "LearningReadoutOnGUI")]
        public static class LearningReadout_LearningReadoutOnGUI_Patch
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
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }


        // Learning helper info pane
        [HarmonyPatch(typeof(RimWorld.LearningReadout), "DrawInfoPane")]
        public static class LearningReadout_DrawInfoPane_Patch
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
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Notification button
        [HarmonyPatch(typeof(Verse.Letter), "DrawButtonAt")]
        public static class Letter_DrawButtonAt_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var counter = 0;
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fieldInfo)
                    {
                        if (fieldInfo.Name == "screenWidth" && fieldInfo.DeclaringType.FullName == "Verse.UI")
                        {
                            if (++counter < 4)
                            {
                                codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                                codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            }
                            else
                            {
                                codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                                codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                                codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                                codes.Insert(i + 6, new CodeInstruction(OpCodes.Conv_R4));
                                break;
                            }
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Notification button mouse over
        [HarmonyPatch(typeof(Verse.Letter), "CheckForMouseOverTextAt")]

        public static class Letter_CheckForMouseOverTextAt_Patch
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
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Conv_R4));
                            codes.Insert(i + 4, new CodeInstruction(OpCodes.Mul));
                            break;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        // Architect info
        [HarmonyPatch(typeof(RimWorld.ArchitectCategoryTab), "DoInfoBox")]
        public static class ArchitectCategoryTab_DoInfoBox_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(ref Rect infoRect)
            {
                infoRect.x += UI.screenWidth * LeftMultiplier;
            }
        }

        // Message top left
        [HarmonyPatch(typeof(Verse.Message), "Draw")]
        public static class Message_Draw_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(ref int xOffset)
            {
                xOffset += (int)(UI.screenWidth * LeftMultiplier);
            }
        }

        // Map search
        [HarmonyPatch(typeof(RimWorld.Dialog_MapSearch), "SetInitialSizeAndPosition")]
        public static class Dialog_MapSearch_SetInitialSizeAndPosition_Patch
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
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, RightMultiplierFieldInfo));
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
