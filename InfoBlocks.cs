using HarmonyLib;
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

            RectTransform rtClock = clockObject.GetComponent<RectTransform>();
            rtClock.anchorMin = Vector2.one;
            rtClock.anchorMax = Vector2.one;
            rtClock.anchoredPosition = new Vector2(-140f, -25f);
            rtClock.sizeDelta = new Vector2(200f, 25f);

            // Background
            Image small = Minimap.instance.m_smallRoot.GetComponent<Image>();
            Image image = clockObject.AddComponent<Image>();
            image.color = small.color;
            image.sprite = small.sprite;
            image.type = small.type;

            clockDayObject = UnityEngine.Object.Instantiate(Minimap.instance.m_biomeNameSmall.gameObject, clockObject.transform);
            clockDayObject.name = objectClockDayName;

            RectTransform rtClockDay = clockDayObject.GetComponent<RectTransform>();
            rtClockDay.anchorMin = Vector2.zero;
            rtClockDay.anchorMax = Vector2.one;
            rtClockDay.anchoredPosition = new Vector2(5f, -1f);
            rtClockDay.sizeDelta = Vector2.zero;

            dayText = clockDayObject.GetComponent<TMP_Text>();
            dayText.text = "Day 0";
            dayText.alignment = TextAlignmentOptions.MidlineLeft;
            dayText.textWrappingMode = TextWrappingModes.NoWrap;

            clockTimeObject = UnityEngine.Object.Instantiate(Minimap.instance.m_biomeNameSmall.gameObject, clockObject.transform);
            clockTimeObject.name = objectClockTimeName;
            
            RectTransform rtClockTime = clockTimeObject.GetComponent<RectTransform>();
            rtClockTime.anchorMin = Vector2.zero;
            rtClockTime.anchorMax = Vector2.one;
            rtClockTime.anchoredPosition = new Vector2(-5f, 0f);
            rtClockTime.sizeDelta = Vector2.zero;

            timeText = clockTimeObject.GetComponent<TMP_Text>();
            timeText.text = "Time";
            timeText.alignment = TextAlignmentOptions.MidlineRight;
            timeText.textWrappingMode = TextWrappingModes.NoWrap;

            parentObject.SetActive(true);

            LogInfo("Info blocks added to hud");
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
            }
        }

    }
}
