using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Valheim.UI;

namespace MyLittleUI
{
    internal static class RadialMenuHover
    {
        private const char LineBreak = (char)10;
        private const string ActionMarkup = "<color=yellow><b>";
        private const string RadialHint = "[<color=yellow><b>$KEY_OpenRadial</b></color>] $settings_open_radial";

        private static readonly string LineBreakText = LineBreak.ToString();
        private static readonly HashSet<Type> HoverMenuTypes = new HashSet<Type>();
        private static readonly HashSet<Type> CanUseErrorTypes = new HashSet<Type>();
        private static readonly List<MethodBase> HoverTextMethods = new List<MethodBase>();

        private static bool initialized;

        internal static void Initialize()
        {
            if (initialized)
                return;

            CacheHoverMenuTypes();
            initialized = true;
        }

        private static void CacheHoverMenuTypes()
        {
            HoverMenuTypes.Clear();
            HoverTextMethods.Clear();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (type == null
                        || type.IsAbstract
                        || !typeof(Component).IsAssignableFrom(type)
                        || (!typeof(IHasHoverMenu).IsAssignableFrom(type)
                            && !typeof(IHasHoverMenuExtended).IsAssignableFrom(type)))
                    {
                        continue;
                    }

                    HoverMenuTypes.Add(type);

                    MethodInfo hoverText = type.GetMethod(
                        nameof(Hoverable.GetHoverText),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null,
                        types: Type.EmptyTypes,
                        modifiers: null);
                    if (hoverText != null
                        && !hoverText.IsStatic
                        && !hoverText.IsAbstract
                        && hoverText.ReturnType == typeof(string)
                        && !HoverTextMethods.Contains(hoverText))
                    {
                        HoverTextMethods.Add(hoverText);
                    }
                }
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).Cast<Type>();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        internal static IEnumerable<MethodBase> GetHoverTextMethods()
        {
            Initialize();
            return HoverTextMethods;
        }

        private static bool IsKnownHoverMenu(object hoverMenu)
        {
            return hoverMenu != null && HoverMenuTypes.Contains(hoverMenu.GetType());
        }

        private static bool CanOpenContextualRadial(GameObject hoverObject)
        {
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
                if (hoverObject.TryGetComponentInParent(out IHasHoverMenu hoverMenu)
                    && IsKnownHoverMenu(hoverMenu))
                {
                    hoverMenuType = hoverMenu.GetType();
                    return hoverMenu.CanUseItems(player, sendErrorMessage: false);
                }

                if (hoverObject.TryGetComponentInParent(out IHasHoverMenuExtended extendedHoverMenu)
                    && IsKnownHoverMenu(extendedHoverMenu))
                {
                    hoverMenuType = extendedHoverMenu.GetType();
                    return extendedHoverMenu.CanUseItems(
                        player,
                        hoverObject.GetComponent<Switch>(),
                        sendErrorMessage: false);
                }
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

        private static bool IsCurrentHoverSource(Component hoverSource, GameObject hoverObject)
        {
            if (!hoverSource || !hoverObject)
                return false;

            return hoverSource.gameObject == hoverObject
                || hoverObject.transform.IsChildOf(hoverSource.transform);
        }

        private static string InsertHint(string hoverText, string hint)
        {
            int titleEnd = hoverText.IndexOf(LineBreak);
            if (titleEnd < 0)
                return hoverText + LineBreakText + hint;

            int insertAfter = titleEnd;
            int lineStart = titleEnd + 1;

            while (lineStart < hoverText.Length)
            {
                int lineEnd = hoverText.IndexOf(LineBreak, lineStart);
                if (lineEnd < 0)
                    lineEnd = hoverText.Length;

                int lineLength = lineEnd - lineStart;
                if (hoverText.IndexOf(ActionMarkup, lineStart, lineLength, StringComparison.Ordinal) < 0)
                    break;

                insertAfter = lineEnd;
                if (lineEnd == hoverText.Length)
                    break;

                lineStart = lineEnd + 1;
            }

            if (insertAfter == hoverText.Length)
                return hoverText + LineBreakText + hint;

            return hoverText.Insert(insertAfter + 1, hint + LineBreakText);
        }

        internal static void AddHint(Component hoverSource, ref string hoverText)
        {
            Initialize();
            if (!MyLittleUI.modEnabled.Value
                || MyLittleUI.hoverRadialMenuHint?.Value != true
                || string.IsNullOrWhiteSpace(hoverText))
            {
                return;
            }

            Player player = Player.m_localPlayer;
            GameObject hoverObject = player ? player.GetHoverObject() : null;
            if (!IsCurrentHoverSource(hoverSource, hoverObject)
                || !CanOpenContextualRadial(hoverObject))
            {
                return;
            }

            string hint = Localization.instance.Localize(RadialHint);
            if (hoverText.IndexOf(hint, StringComparison.Ordinal) >= 0
                || !CanUseHoveredItems(player, hoverObject))
            {
                return;
            }

            hoverText = InsertHint(hoverText, hint);
        }

        [HarmonyPatch]
        private static class HoverMenu_GetHoverText_AddRadialHint
        {
            private static bool Prepare()
            {
                Initialize();
                return HoverTextMethods.Count > 0;
            }

            private static IEnumerable<MethodBase> TargetMethods() => GetHoverTextMethods();

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(object __instance, ref string __result)
                => AddHint(__instance as Component, ref __result);
        }

        [HarmonyPatch(typeof(Switch), nameof(Switch.GetHoverText))]
        private static class Switch_GetHoverText_AddRadialHint
        {
            private static bool Prepare()
            {
                Initialize();
                return true;
            }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Switch __instance, ref string __result)
                => AddHint(__instance, ref __result);
        }

        [HarmonyPatch(typeof(OpenRadialConfig), nameof(OpenRadialConfig.TryOpenNonDefaultRadials))]
        private static class OpenRadialConfig_TryOpenNonDefaultRadials_SuppressDefaultRadial
        {
            private static bool Prepare()
            {
                Initialize();
                return true;
            }

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
