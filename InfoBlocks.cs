using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    internal class InfoBlocks
    {
        private const string objectRootName = "MyLittleUI_Parent";
        private const string objectClockName = "Clock";
        private const string objectClockDayName = "Day";
        private const string objectClockTimeName = "Time";

        public static GameObject parentObject;
        public static GameObject clockObject;
        public static GameObject clockDayObject;
        public static GameObject clockTimeObject;

        public static TMP_Text dayText;
        public static TMP_Text timeText;

        private static Image minimapBackground;
        private static Image clockBackground;

        private static readonly int layerUI = LayerMask.NameToLayer("UI");

        private static void AddInfoBlocks(Transform parentTransform)
        {
            // Parent object to set visibility
            parentObject = new GameObject(objectRootName, typeof(RectTransform))
            {
                layer = layerUI
            };
            parentObject.transform.SetParent(parentTransform, false);

            // Parent rect with size of fullscreen
            RectTransform pRectTransform = parentObject.GetComponent<RectTransform>();
            pRectTransform.anchoredPosition = Vector2.zero;
            pRectTransform.anchorMin = Vector2.zero;
            pRectTransform.anchorMax = Vector2.one;
            pRectTransform.sizeDelta = Vector2.zero;

            // Clock
            clockObject = new GameObject(objectClockName, typeof(RectTransform))
            {
                layer = layerUI
            };
            clockObject.transform.SetParent(parentObject.transform, false);

            // Background
            UpdateDayTimeBackground();

            clockDayObject = UnityEngine.Object.Instantiate(Minimap.instance.m_biomeNameSmall.gameObject, clockObject.transform);
            clockDayObject.name = objectClockDayName;

            dayText = clockDayObject.GetComponent<TMP_Text>();
            dayText.text = "Day 0";
            dayText.textWrappingMode = TextWrappingModes.NoWrap;

            clockTimeObject = UnityEngine.Object.Instantiate(Minimap.instance.m_biomeNameSmall.gameObject, clockObject.transform);
            clockTimeObject.name = objectClockTimeName;

            timeText = clockTimeObject.GetComponent<TMP_Text>();
            timeText.text = "Time";
            timeText.textWrappingMode = TextWrappingModes.NoWrap;

            LogInfo("Info blocks added to hud");
        }

        internal static void UpdateDayTimeBackground()
        {
            if (clockObject == null)
                return;

            if (minimapBackground == null)
                minimapBackground = Minimap.instance.m_smallRoot.GetComponent<Image>();

            if (clockBackground == null)
                clockBackground = clockObject.AddComponent<Image>();

            clockBackground.color   = clockBackgroundColor.Value == Color.clear ? minimapBackground.color : clockBackgroundColor.Value;
            clockBackground.sprite  = minimapBackground.sprite;
            clockBackground.type    = minimapBackground.type;

            clockBackground.enabled = clockShowBackground.Value;
        }

        internal static void UpdateDayTimeText()
        {
            if (clockObject == null)
                return;

            clockObject.SetActive(clockShowTime.Value || clockShowDay.Value);
            clockTimeObject.SetActive(clockShowTime.Value);
            clockDayObject.SetActive(clockShowDay.Value);

            timeText.alignment = clockShowDay.Value ? (clockSwapDayTime.Value ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight) : TextAlignmentOptions.Midline;
            timeText.color = clockFontColor.Value == Color.clear ? Minimap.instance.m_biomeNameSmall.GetComponent<TMP_Text>().color : clockFontColor.Value;
            timeText.fontSizeMin = clockFontSize.Value == 0f ? Minimap.instance.m_biomeNameSmall.GetComponent<TMP_Text>().fontSizeMin : clockFontSize.Value;
            timeText.fontSize = clockFontSize.Value == 0f ? timeText.fontSizeMin : clockFontSize.Value;

            dayText.alignment = clockShowTime.Value ? (clockSwapDayTime.Value ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft) : TextAlignmentOptions.Midline;
            dayText.color = clockFontColor.Value == Color.clear ? timeText.color : clockFontColor.Value;
            dayText.fontSizeMin = clockFontSize.Value == 0f ? timeText.fontSizeMin : clockFontSize.Value;
            dayText.fontSize = clockFontSize.Value == 0f ? dayText.fontSizeMin : clockFontSize.Value;

            RectTransform rtClock = clockObject.GetComponent<RectTransform>();
            rtClock.anchorMin = Vector2.one;
            rtClock.anchorMax = Vector2.one;
            rtClock.anchoredPosition = clockPosition.Value;
            rtClock.sizeDelta = clockSize.Value;

            RectTransform rtClockDay = clockDayObject.GetComponent<RectTransform>();
            rtClockDay.anchorMin = Vector2.zero;
            rtClockDay.anchorMax = Vector2.one;
            rtClockDay.anchoredPosition = new Vector2((clockSwapDayTime.Value ? -1 : 1) * clockTextPadding.Value, -1f);
            rtClockDay.sizeDelta = Vector2.zero;

            RectTransform rtClockTime = clockTimeObject.GetComponent<RectTransform>();
            rtClockTime.anchorMin = Vector2.zero;
            rtClockTime.anchorMax = Vector2.one;
            rtClockTime.anchoredPosition = new Vector2((clockSwapDayTime.Value ? 1 : -1) * clockTextPadding.Value, 0f);
            rtClockTime.sizeDelta = Vector2.zero;
        }

        internal static void UpdateVisibility()
        {
            parentObject?.SetActive(modEnabled.Value);

            UpdateDayTimeText();
        }

        internal static void UpdateTime()
        {
            if (!EnvMan.instance || timeText == null)
                return;

            float smoothDayFraction = EnvMan.instance.m_smoothDayFraction;
            int hour = Mathf.CeilToInt(smoothDayFraction * 24);
            int minute = 5 * (Mathf.CeilToInt((smoothDayFraction * 24 - hour) * 60) / 5);

            timeText.SetText(DateTime.MinValue.AddMonths(2).AddDays(EnvMan.instance.GetCurrentDay()).AddHours(hour).AddMinutes(minute).ToString(clockTimeFormat24h.Value ? "HH:mm" : "hh:mm tt"));
            UpdateDayTimeBackground();
        }

        internal static void UpdateDay()
        {
            if (!EnvMan.instance || dayText == null)
                return;

            dayText.SetText(Localization.instance.Localize("$msg_newday", EnvMan.instance.GetCurrentDay().ToString()));
        }

        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.UpdateTriggers))]
        public static class EnvMan_UpdateTriggers_UpdateDayTime
        {
            private static int Fraction(float dayFraction)
            {
                return (int)(dayFraction * 1000);
            }

            public static void Postfix(EnvMan __instance, float oldDayFraction, float newDayFraction, Heightmap.Biome biome)
            {
                if (!modEnabled.Value)
                    return;

                if (Player.m_localPlayer == null || biome == Heightmap.Biome.None)
                    return;

                if (Fraction(oldDayFraction) != Fraction(newDayFraction))
                    UpdateTime();

                if (newDayFraction < oldDayFraction)
                    UpdateDay();
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        public static class Hud_Awake_AddInfoBlocks
        {
            public static void Postfix(Hud __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (!__instance.m_rootObject.transform.Find(objectRootName))
                    AddInfoBlocks(__instance.m_rootObject.transform);

                UpdateVisibility();
            }
        }
        
        private static string TimerString(double seconds)
        {
            if (seconds < 60)
                return DateTime.FromBinary(599266080000000000).AddSeconds(seconds).ToString(@"ss\s");

            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours > 24)
                return string.Format("{0:d2}:{1:d2}:{2:d2}", (int)span.TotalHours, span.Minutes, span.Seconds);
            else
                return span.ToString(span.Hours > 0 ? @"hh\:mm\:ss" : @"mm\:ss");
        }
    }
}
