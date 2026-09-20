using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Valheim.UI;

namespace MyLittleUI
{
    internal static class RadialMenuHover
    {
        private const string UseKey = "$KEY_Use";
        private const string RadialKey = "$KEY_OpenRadial";
        private const string RadialHint = "[<color=yellow><b>$KEY_OpenRadial</b></color>] $settings_open_radial";

        private static readonly HashSet<Type> CanUseErrorTypes = new HashSet<Type>();
        private static RectTransform hoverTextTransform;
        private static float originalHoverTextWidth;
        private static readonly MethodInfo LocalizeMethod = AccessTools.Method(
            typeof(Localization),
            nameof(Localization.Localize),
            new[] { typeof(string) });
        private static readonly MethodInfo LocalizeHoverTextMethod = AccessTools.Method(
            typeof(RadialMenuHover),
            nameof(LocalizeHoverText));
        private static readonly MethodInfo SetMousePositionMethod = AccessTools.Method(
            typeof(ZInput),
            nameof(ZInput.SetMousePosition),
            new[] { typeof(Vector2) });
        private static readonly MethodInfo SetInitialMousePositionMethod = AccessTools.Method(
            typeof(RadialMenuHover),
            nameof(SetInitialMousePosition),
            new[] { typeof(Vector2), typeof(RadialBase) });

        private static bool CanOpenContextualRadial(GameObject hoverObject)
        {
            if (!hoverObject)
                return false;

            bool requiresSwitch = hoverObject.TryGetComponentInParent(out Catapult _)
                || hoverObject.TryGetComponentInParent(out ShieldGenerator _)
                || hoverObject.TryGetComponentInParent(out Chair _);

            if (requiresSwitch && !hoverObject.TryGetComponent(out Switch _))
                return false;

            return hoverObject.GetComponentInParent<Interactable>() != null;
        }

        private static bool CanUseHoveredItems(Player player, GameObject hoverObject)
        {
            Type hoverMenuType = null;

            try
            {
                MonoBehaviour hoverMenu = hoverObject
                    ? hoverObject.GetComponentsInParent<MonoBehaviour>()
                        .FirstOrDefault(component => component is IHasHoverMenu || component is IHasHoverMenuExtended)
                    : null;
                hoverMenuType = hoverMenu?.GetType();
                return RadialMenuItemSearch.CanUseItems(player, hoverObject, sendErrorMessage: false);
            }
            catch (Exception exception)
            {
                if (hoverMenuType == null || CanUseErrorTypes.Add(hoverMenuType))
                {
                    MyLittleUI.LogWarning(
                        $"Failed to evaluate contextual radial availability for '{hoverMenuType?.FullName ?? "unknown type"}'.{Environment.NewLine}{exception}");
                }
            }

            return false;
        }

        internal static string AddRadialHint(string hoverText)
        {
            if (!MyLittleUI.modEnabled.Value
                || MyLittleUI.hoverRadialMenuHint?.Value != true
                || string.IsNullOrWhiteSpace(hoverText)
                || hoverText.IndexOf(RadialKey, StringComparison.Ordinal) >= 0)
            {
                return hoverText;
            }

            Player player = Player.m_localPlayer;
            GameObject hoverObject = player ? player.GetHoverObject() : null;
            if (!player
                || !CanOpenContextualRadial(hoverObject)
                || !CanUseHoveredItems(player, hoverObject))
            {
                return hoverText;
            }

            int useKeyIndex = hoverText.IndexOf(UseKey, StringComparison.Ordinal);
            if (useKeyIndex < 0)
                return hoverText;

            int lineEnd = hoverText.IndexOf('\n', useKeyIndex);
            if (lineEnd < 0)
                return hoverText + "\n" + RadialHint;

            return hoverText.Insert(lineEnd + 1, RadialHint + "\n");
        }

        private static string LocalizeHoverText(Localization localization, string hoverText)
        {
            return localization.Localize(AddRadialHint(hoverText));
        }

        private static void SetInitialMousePosition(Vector2 selectedPosition, RadialBase radial)
        {
            float distance = MyLittleUI.modEnabled.Value && MyLittleUI.radialMenuInitialCursorDistance != null
                ? Mathf.Clamp(MyLittleUI.radialMenuInitialCursorDistance.Value, 0f, 2f)
                : 1f;
            Vector2 radialCenter = radial && radial.m_elementInfo && radial.m_elementInfo.m_title
                ? (Vector2)radial.m_elementInfo.m_title.transform.position
                : selectedPosition;
            ZInput.SetMousePosition(Vector2.LerpUnclamped(radialCenter, selectedPosition, distance));
        }

        private static IEnumerable<MethodInfo> GetArmorStandHoverMethods()
        {
            const BindingFlags flags = BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly;

            IEnumerable<Type> nestedTypes = typeof(ArmorStand).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
            return typeof(ArmorStand).GetMethods(flags)
                .Concat(nestedTypes.SelectMany(type => type.GetMethods(flags)))
                .Where(method => method.Name.IndexOf("<Awake>", StringComparison.Ordinal) >= 0
                    && method.ReturnType == typeof(string)
                    && method.GetParameters().Length == 0);
        }

        private static IEnumerable<MethodBase> GetKnownHoverMethods()
        {
            MethodBase[] methods =
            {
                AccessTools.Method(typeof(Fireplace), nameof(Fireplace.GetHoverText)),
                AccessTools.Method(typeof(CookingStation), nameof(CookingStation.GetHoverText)),
                AccessTools.Method(typeof(CookingStation), nameof(CookingStation.OnHoverFuelSwitch)),
                AccessTools.Method(typeof(Smelter), nameof(Smelter.OnHoverAddFuel)),
                AccessTools.Method(typeof(Smelter), nameof(Smelter.OnHoverAddOre)),
                AccessTools.Method(typeof(ShieldGenerator), nameof(ShieldGenerator.OnHoverAddFuel)),
                AccessTools.Method(typeof(Turret), nameof(Turret.GetHoverText)),
                AccessTools.Method(typeof(Switch), nameof(Switch.GetHoverText)),
            };

            return methods
                .Where(method => method != null)
                .Concat(GetArmorStandHoverMethods())
                .Concat(RadialMenuItemReceiver.GetHoverTextMethods())
                .Distinct();
        }

        private static IEnumerable<CodeInstruction> ReplaceLocalizationCall(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (LocalizeMethod != null
                    && LocalizeHoverTextMethod != null
                    && instruction.Calls(LocalizeMethod))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = LocalizeHoverTextMethod;
                }

                yield return instruction;
            }
        }

        internal static void ApplyHoverTextWidth(Hud hud = null)
        {
            hud ??= Hud.instance;
            if (!hud || !hud.m_hoverName || MyLittleUI.hoverTextWidth == null)
                return;

            RectTransform rectTransform = hud.m_hoverName.rectTransform;
            if (hoverTextTransform != rectTransform)
            {
                hoverTextTransform = rectTransform;
                originalHoverTextWidth = rectTransform.rect.width;
            }

            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                MyLittleUI.modEnabled.Value ? MyLittleUI.hoverTextWidth.Value : originalHoverTextWidth);
        }

        [HarmonyPatch]
        private static class KnownHoverMethods_Localize_AddRadialHint
        {
            private static IEnumerable<MethodBase> TargetMethods() => GetKnownHoverMethods();

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                => ReplaceLocalizationCall(instructions);
        }

        [HarmonyPatch(typeof(RadialBase), nameof(RadialBase.SelectStartElement))]
        private static class RadialBase_SelectStartElement_AdjustInitialMousePosition
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    if (SetMousePositionMethod != null
                        && SetInitialMousePositionMethod != null
                        && instruction.Calls(SetMousePositionMethod))
                    {
                        CodeInstruction loadRadial = new CodeInstruction(OpCodes.Ldarg_0);
                        loadRadial.labels.AddRange(instruction.labels);
                        instruction.labels.Clear();
                        yield return loadRadial;

                        instruction.opcode = OpCodes.Call;
                        instruction.operand = SetInitialMousePositionMethod;
                    }

                    yield return instruction;
                }
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        private static class Hud_Awake_ApplyHoverTextWidth
        {
            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter("Azumatt.MinimalUI")]
            private static void Postfix(Hud __instance) => ApplyHoverTextWidth(__instance);
        }

        [HarmonyPatch(typeof(OpenRadialConfig), nameof(OpenRadialConfig.TryOpenNonDefaultRadials))]
        private static class OpenRadialConfig_TryOpenNonDefaultRadials_UseSafeItemSearch
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(OpenRadialConfig __instance, RadialBase radial, ref bool __result)
            {
                if (!MyLittleUI.modEnabled.Value)
                    return true;

                Player player = Player.m_localPlayer;
                GameObject hoverObject = player ? player.GetHoverObject() : null;
                if (!player
                    || !CanOpenContextualRadial(hoverObject)
                    || !RadialMenuItemSearch.IsSearchTarget(hoverObject))
                {
                    return true;
                }

                if (!RadialMenuItemSearch.TryGetItems(
                    player,
                    hoverObject,
                    out List<string> items,
                    sendErrorMessage: true))
                {
                    __result = false;
                    return false;
                }

                if (items == null || items.Count <= 0)
                {
                    if (RadialData.SO.OpenNormalRadialWhenHoverMenuFails)
                    {
                        __result = false;
                    }
                    else
                    {
                        radial?.QueuedClose();
                        __result = true;
                    }

                    return false;
                }

                __instance.OpenItemMenu(radial, player, items, hoverObject);
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ItemGroupConfig), nameof(ItemGroupConfig.AddElement))]
        private static class ItemGroupConfig_AddElement_UseSafeHoverCloseCheck
        {
            private static void Postfix(List<RadialMenuElement> elements, RadialBase radial)
            {
                if (radial == null
                    || !radial.IsHoverMenu
                    || !radial.HoverObject
                    || !RadialMenuItemSearch.IsSearchTarget(radial.HoverObject)
                    || elements == null
                    || elements.Count == 0
                    || !(elements[elements.Count - 1] is ItemElement itemElement))
                {
                    return;
                }

                itemElement.AdvancedCloseOnInteract = (currentRadial, _) =>
                {
                    Player player = Player.m_localPlayer;
                    return !player
                        || !currentRadial.HoverObject
                        || !RadialMenuItemSearch.CanUseItems(
                            player,
                            currentRadial.HoverObject,
                            sendErrorMessage: false);
                };
            }
        }

        [HarmonyPatch(typeof(OpenRadialConfig), nameof(OpenRadialConfig.TryOpenNonDefaultRadials))]
        private static class OpenRadialConfig_TryOpenNonDefaultRadials_SuppressDefaultRadial
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(RadialBase radial, ref bool __result)
            {
                if (__result
                    || !MyLittleUI.modEnabled.Value
                    || MyLittleUI.hoverRadialMenuSuppressDefault?.Value != true)
                {
                    return;
                }

                Player player = Player.m_localPlayer;
                if (!player || !player.GetHoverObject())
                    return;

                radial?.QueuedClose();
                __result = true;
            }
        }
    }
}
