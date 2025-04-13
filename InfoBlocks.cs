using HarmonyLib;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    internal static class InfoBlocks
    {
        private const string objectRootName = "MyLittleUI_Parent";
        private const string objectClockName = "Clock";
        private const string objectClockDayName = "Day";
        private const string objectClockTimeName = "Time";
        private const string objectForecastName = "Forecast";
        private const string objectForecastWeatherText = "weather_text";
        private const string objectForecastWeatherIcon = "weather_icon";
        private const string objectWindsName = "Winds";
        private const string objectWindsTemplateName = "WindElement";
        private const string objectWindsProgressName = "WindProgress";

        public static GameObject parentObject;

        public static GameObject clockObject;
        public static GameObject clockDayObject;
        public static GameObject clockTimeObject;

        public static TMP_Text dayText;
        public static TMP_Text timeText;

        private static Image minimapBackground;
        private static Image clockBackground;
        private static Image forecastBackground;
        private static Image windsBackground;
        private static Image windsProgressBackground;

        public static GameObject forecastObject;
        public static GameObject windsObject;
        public static GameObject windTemplate;
        
        private static GameObject windsProgress;
        public static RectTransform windsProgressRect;
        public static RectTransform windsObjectRect;

        public static TMP_Text weatherText;
        public static Image weatherIcon;

        private static string[] fuzzyTime;

        private static void AddInfoBlocks(Transform parentTransform)
        {
            if (!Minimap.instance?.m_biomeNameSmall)
            {
                LogInfo("Minimap is not initialized, skipping clock init");
                return;
            }

            // Parent object to set global visibility
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
            dayText.verticalAlignment = VerticalAlignmentOptions.Middle;

            clockTimeObject = UnityEngine.Object.Instantiate(Minimap.instance.m_biomeNameSmall.gameObject, clockObject.transform);
            clockTimeObject.name = objectClockTimeName;

            timeText = clockTimeObject.GetComponent<TMP_Text>();
            timeText.text = "Time";
            timeText.textWrappingMode = TextWrappingModes.NoWrap;
            timeText.verticalAlignment = VerticalAlignmentOptions.Middle;

            // Forecast
            forecastObject = new GameObject(objectForecastName, typeof(RectTransform))
            {
                layer = layerUI
            };
            forecastObject.transform.SetParent(parentObject.transform, false);

            // Background
            UpdateForecastBackground();

            weatherText = UnityEngine.Object.Instantiate(Minimap.instance.m_biomeNameSmall.gameObject, forecastObject.transform).GetComponent<TMP_Text>();
            weatherText.text = "0:00";
            weatherText.textWrappingMode = TextWrappingModes.NoWrap;
            weatherText.verticalAlignment = VerticalAlignmentOptions.Middle;
            weatherText.horizontalAlignment = HorizontalAlignmentOptions.Left;
            weatherText.gameObject.name = objectForecastWeatherText;

            weatherIcon = UnityEngine.Object.Instantiate(Minimap.instance.m_windMarker.gameObject, forecastObject.transform).GetComponent<Image>();
            weatherIcon.gameObject.name = objectForecastWeatherIcon;
            
            // Winds
            windsObject = new GameObject(objectWindsName, typeof(RectTransform))
            {
                layer = layerUI
            };
            windsObject.transform.SetParent(parentObject.transform, false);

            windsObjectRect = windsObject.GetComponent<RectTransform>();

            windTemplate = UnityEngine.Object.Instantiate(Minimap.instance.m_windMarker.gameObject, parentObject.transform);
            windTemplate.name = objectWindsTemplateName;

            UpdateWindsBackground();

            UpdateWindsBlock();

            LogInfo("Info blocks added to hud");
        }

        internal static void UpdateForecastBackground()
        {
            if (!forecastObject)
                return;

            if (minimapBackground == null)
                minimapBackground = Minimap.instance?.m_smallRoot?.GetComponent<Image>();

            if (forecastBackground == null)
                forecastBackground = forecastObject.AddComponent<Image>();

            forecastBackground.enabled = minimapBackground != null && forecastShowBackground.Value;

            if (!forecastBackground.enabled)
                return;

            forecastBackground.color = GetBackgroundColor(forecastBackgroundColor.Value);
            forecastBackground.sprite = minimapBackground.sprite;
            forecastBackground.type = minimapBackground.type;
        }

        internal static void UpdateForecastBlock()
        {
            if (!forecastObject)
                return;

            forecastObject.SetActive(forecastEnabled.Value);

            weatherText.color = forecastFontColor.Value == Color.clear ? Minimap.instance.m_biomeNameSmall.GetComponent<TMP_Text>().color : forecastFontColor.Value;
            weatherText.fontSizeMin = forecastFontSize.Value == 0f ? Minimap.instance.m_biomeNameSmall.GetComponent<TMP_Text>().fontSizeMin : forecastFontSize.Value;
            weatherText.fontSize = forecastFontSize.Value == 0f ? weatherText.fontSizeMin : forecastFontSize.Value;

            RectTransform rtForecast = forecastObject.GetComponent<RectTransform>();
            rtForecast.SetAnchor(forecastPositionAnchor.Value);
            rtForecast.anchoredPosition = Game.m_noMap ? forecastPositionNomap.Value : forecastPosition.Value;
            rtForecast.sizeDelta = forecastSize.Value;

            float height = forecastSize.Value.y;

            RectTransform rtWeather = weatherText.GetComponent<RectTransform>();
            rtWeather.anchorMin = new Vector2(0f, 0.5f);
            rtWeather.anchorMax = new Vector2(0f, 0.5f);
            rtWeather.anchoredPosition = new Vector2(height + forecastTextPadding.Value, 1f);
            rtWeather.sizeDelta = Vector2.zero;

            RectTransform rtWeatherIcon = weatherIcon.GetComponent<RectTransform>();
            rtWeatherIcon.anchorMin = new Vector2(0f, 0.5f);
            rtWeatherIcon.anchorMax = new Vector2(0f, 0.5f);
            rtWeatherIcon.sizeDelta = Vector2.one * height;
            rtWeatherIcon.anchoredPosition = new Vector2(height / 2f, 0f);
        }

        internal static void UpdateWindsBackground()
        {
            if (!windsObject)
                return;

            if (minimapBackground == null)
                minimapBackground = Minimap.instance?.m_smallRoot?.GetComponent<Image>();

            if (windsBackground == null)
                windsBackground = windsObject.AddComponent<Image>();

            windsBackground.enabled = minimapBackground != null && windsShowBackground.Value;

            if (windsBackground.enabled)
            {
                windsBackground.color = GetBackgroundColor(windsBackgroundColor.Value);
                windsBackground.sprite = minimapBackground.sprite;
                windsBackground.type = minimapBackground.type;
            }

            if (windsProgressBackground == null)
            {
                windsProgress = new GameObject(objectWindsProgressName, typeof(RectTransform))
                {
                    layer = layerUI
                };
                windsProgress.transform.SetParent(windsObject.transform, false);

                windsProgressRect = windsProgress.GetComponent<RectTransform>();
                windsProgressRect.anchoredPosition = Vector2.zero;
                windsProgressRect.anchorMin = Vector2.zero;
                windsProgressRect.anchorMax = Vector2.one;
                windsProgressRect.sizeDelta = Vector2.zero;

                windsProgressBackground = windsProgress.AddComponent<Image>();
            }

            windsProgressBackground.enabled = minimapBackground != null && windsShowProgress.Value;

            if (windsProgressBackground.enabled)
            {
                windsProgressBackground.color = GetBackgroundColor(windsProgressColor.Value);
                windsProgressBackground.sprite = minimapBackground.sprite;
                windsProgressBackground.type = minimapBackground.type;
            }
        }

        private static Color GetBackgroundColor(Color color)
        {
            if (color == Color.clear)
                return minimapBackground.color;

            if (color.a != 0f && color.r == 0 && color.g == 0 && color.b == 0)
                return new Color(minimapBackground.color.r, minimapBackground.color.g, minimapBackground.color.b, color.a);

            return color;
        }

        internal static void UpdateWindsBlock(bool forceRebuildList = false)
        {
            if (!windsObject)
                return;

            windsObject.SetActive(windsEnabled.Value);
            windTemplate.SetActive(false);

            windsObjectRect.SetAnchor(ElementAnchor.TopRight);
            windsObjectRect.anchoredPosition = GetWindsPosition();
            windsObjectRect.sizeDelta = GetWindsSize();

            RectTransform rtWindTemplate = windTemplate.GetComponent<RectTransform>();
            rtWindTemplate.anchorMin = Vector2.zero;
            rtWindTemplate.anchorMax = Vector2.zero;

            Image image = windTemplate.GetComponent<Image>();
            if (image.color != windsArrowColor.Value)
            {
                image.color = windsArrowColor.Value;
                forceRebuildList = true;
            }

            if (forceRebuildList)
                WeatherForecast.UpdateNextWinds(forceRebuildList);
        }

        internal static Vector2 GetWindsSize()
        {
            return (Game.m_noMap ? windsSizeNomap.Value : windsSize.Value);
        }

        private static Vector2 GetWindsPosition()
        {
            return Game.m_noMap ? windsPositionNomap.Value : windsPosition.Value;
        }

        internal static void UpdateDayTimeBackground()
        {
            if (!clockObject)
                return;

            if (minimapBackground == null)
                minimapBackground = Minimap.instance?.m_smallRoot?.GetComponent<Image>();

            if (clockBackground == null)
                clockBackground = clockObject.AddComponent<Image>();
            
            clockBackground.enabled = minimapBackground != null && clockShowBackground.Value;

            if (!clockBackground.enabled)
                return;

            clockBackground.color   = GetBackgroundColor(clockBackgroundColor.Value);
            clockBackground.sprite  = minimapBackground.sprite;
            clockBackground.type    = minimapBackground.type;
        }

        internal static void UpdateDayTimeText()
        {
            if (!clockObject)
                return;

            clockObject.SetActive(clockShowTime.Value || clockShowDay.Value);
            clockTimeObject.SetActive(clockShowTime.Value);
            clockDayObject.SetActive(clockShowDay.Value);

            timeText.horizontalAlignment = clockShowDay.Value ? (clockSwapDayTime.Value ? HorizontalAlignmentOptions.Left : HorizontalAlignmentOptions.Right) : HorizontalAlignmentOptions.Center;
            timeText.color = clockFontColor.Value == Color.clear ? Minimap.instance.m_biomeNameSmall.GetComponent<TMP_Text>().color : clockFontColor.Value;
            timeText.fontSizeMin = clockFontSize.Value == 0f ? Minimap.instance.m_biomeNameSmall.GetComponent<TMP_Text>().fontSizeMin : clockFontSize.Value;
            timeText.fontSize = clockFontSize.Value == 0f ? timeText.fontSizeMin : clockFontSize.Value;

            dayText.horizontalAlignment = clockShowTime.Value ? (clockSwapDayTime.Value ? HorizontalAlignmentOptions.Right : HorizontalAlignmentOptions.Left) : HorizontalAlignmentOptions.Center;
            dayText.color = clockFontColor.Value == Color.clear ? timeText.color : clockFontColor.Value;
            dayText.fontSizeMin = clockFontSize.Value == 0f ? timeText.fontSizeMin : clockFontSize.Value;
            dayText.fontSize = clockFontSize.Value == 0f ? dayText.fontSizeMin : clockFontSize.Value;

            RectTransform rtClock = clockObject.GetComponent<RectTransform>();
            rtClock.SetAnchor(clockPositionAnchor.Value);
            rtClock.anchoredPosition = clockPosition.Value;
            rtClock.sizeDelta = clockSize.Value;

            RectTransform rtClockDay = clockDayObject.GetComponent<RectTransform>();
            rtClockDay.anchorMin = Vector2.zero;
            rtClockDay.anchorMax = Vector2.one;
            rtClockDay.anchoredPosition = new Vector2((clockSwapDayTime.Value ? -1 : 1) * (clockShowDay.Value && clockShowTime.Value ? clockTextPadding.Value : 0f), 1f);
            rtClockDay.sizeDelta = Vector2.zero;

            RectTransform rtClockTime = clockTimeObject.GetComponent<RectTransform>();
            rtClockTime.anchorMin = Vector2.zero;
            rtClockTime.anchorMax = Vector2.one;
            rtClockTime.anchoredPosition = new Vector2((clockSwapDayTime.Value ? 1 : -1) * (clockShowDay.Value && clockShowTime.Value ? clockTextPadding.Value : 0f), 1f);
            rtClockTime.sizeDelta = Vector2.zero;
        }

        internal static void UpdateInfoBlocksVisibility()
        {
            parentObject?.SetActive(modEnabled.Value && Minimap.instance && Minimap.instance.m_mode != Minimap.MapMode.Large);
        }

        internal static void UpdateVisibility()
        {
            UpdateInfoBlocksVisibility();

            UpdateDayTimeText();

            UpdateClock();

            UpdateForecastBlock();

            UpdateWindsBlock();

            WeatherForecast.UpdateWeather();
            WeatherForecast.UpdateNextWinds();
        }

        internal static void UpdateClock()
        {
            if (!EnvMan.instance)
                return;

            if (!clockObject || !clockObject.activeSelf)
                return;

            dayText?.SetText(Localization.instance.Localize("$msg_newday", EnvMan.instance.GetCurrentDay().ToString()));
            timeText?.SetText(GetTimeString());
            UpdateDayTimeBackground();
        }

        internal static void FuzzyWordsOnChange()
        {
            fuzzyTime = null;
            UpdateClock();
        }

        internal static string GetRealTime() => DateTime.Now.ToString(clockTimeFormat24h.Value ? "HH:mm" : "hh:mm tt");

        internal static string GetGameTime()
        {
            float smoothDayFraction = EnvMan.instance.m_smoothDayFraction;
            int hour = Mathf.CeilToInt(smoothDayFraction * 24);
            int minute = 5 * (Mathf.CeilToInt((smoothDayFraction * 24 - hour) * 60) / 5);

            return DateTime.MinValue.AddMonths(2).AddDays(EnvMan.instance.GetCurrentDay()).AddHours(hour).AddMinutes(minute).ToString(clockTimeFormat24h.Value ? "HH:mm" : "hh:mm tt");
        }

        internal static string GetTimeString()
        {
            if (clockTimeType.Value == ClockTimeType.Fuzzy)
            {
                fuzzyTime ??= clockFuzzy.Value.Split(',').Select(str => str.Trim()).ToArray();

                if (fuzzyTime.Length > 0)
                {
                    float fraction = EnvMan.instance.m_smoothDayFraction + (1 / (fuzzyTime.Length * 2));
                    return fraction >= 1 ? fuzzyTime[0] : fuzzyTime[Mathf.Clamp((int)(fraction * fuzzyTime.Length), 0, fuzzyTime.Length - 1)];
                }
            }
            else if (clockTimeType.Value == ClockTimeType.RealTime)
                return GetRealTime();
            else if (clockTimeType.Value == ClockTimeType.GameTime)
                return GetGameTime();

            return string.Format(clockFormatGameAndRealTime.Value, GetGameTime(), GetRealTime());
        }

        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.UpdateTriggers))]
        public static class EnvMan_UpdateTriggers_UpdateDayTime
        {
            private static int Fraction(float dayFraction)
            {
                return (int)(dayFraction * (clockTimeType.Value == ClockTimeType.Fuzzy && fuzzyTime != null && fuzzyTime.Length != 0 ? 40 : 1000));
            }

            public static void Postfix(float oldDayFraction, float newDayFraction, Heightmap.Biome biome)
            {
                if (!modEnabled.Value)
                    return;

                if (Player.m_localPlayer == null || biome == Heightmap.Biome.None)
                    return;

                if (Fraction(oldDayFraction) != Fraction(newDayFraction) || newDayFraction < oldDayFraction)
                    UpdateClock();
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        public static class Hud_Awake_AddInfoBlocks
        {
            [HarmonyAfter("Azumatt.MinimalUI", "org.bepinex.plugins.passivepowers")]
            public static void Postfix(Hud __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (!__instance.m_rootObject.transform.Find(objectRootName))
                {
                    AddInfoBlocks(__instance.m_rootObject.transform);
                    InventoryPanel.AddBlock(parentObject);
                }

                UpdateVisibility();
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
        public static class Hud_OnDestroy_Clear
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                WeatherForecast.windList.Clear();
                WeatherForecast.winds.Clear();
                WeatherForecast.windsTransition.Clear();
                
                parentObject = null;

                clockObject = null;
                clockDayObject = null;
                clockTimeObject = null;

                dayText = null;
                timeText = null;

                forecastObject = null;
                windsObject = null;
                windTemplate = null;

                windsProgress = null;
                windsProgressRect = null;
                windsObjectRect = null;

                weatherText = null;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public static class Player_OnSpawned_UpdateInfoBlocksVisibility
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                UpdateVisibility();
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
        public static class Minimap_SetMapMode_UpdateInfoBlocksVisibility
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                UpdateInfoBlocksVisibility();
            }
        }
    }
}
