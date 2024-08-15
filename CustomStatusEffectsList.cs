using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    internal static class CustomStatusEffectsList
    {
        private const string templateName = "SE_Template_MLU";

        private static RectTransform m_statusEffectTemplateOriginal;
        private static Vector2 m_statusEffectListRootPositionOriginal = Vector2.zero;

        private static RectTransform m_statusEffectTemplate;

        private static Vector2 m_shipWindIndicatorRootPositionOriginal = Vector2.zero;
        private static Vector2 m_shipWindIconRootPositionOriginal = Vector2.zero;
       
        private static Vector3 GetStatusEffectPosition(int i)
        {
            float offset = i * (Game.m_noMap ? statusEffectsPositionSpacingNomap.Value + statusEffectsElementSizeNomap.Value : statusEffectsPositionSpacing.Value + statusEffectsElementSize.Value);
            return (Game.m_noMap ? statusEffectsFillingDirectionNomap.Value : statusEffectsFillingDirection.Value) switch
            {
                ListDirection.LeftToRight => new Vector3(offset, 0, 0),
                ListDirection.RightToLeft => new Vector3(-offset, 0, 0),
                ListDirection.TopToBottom => new Vector3(0, -offset, 0),
                ListDirection.BottomToTop => new Vector3(0, offset, 0),
                _ => new Vector3(-offset, 0, 0),
            };
        }

        public static void UpdateStatusEffectList()
        {
            InitializeStatusEffectTemplate();
            ChangeSailingIndicator();
        }

        public static void InitializeStatusEffectTemplate()
        {
            if (Hud.instance == null)
                return;

            if (m_statusEffectTemplateOriginal == null)
            {
                m_statusEffectTemplateOriginal = Hud.instance.m_statusEffectTemplate;
                m_statusEffectListRootPositionOriginal = Hud.instance.m_statusEffectListRoot.anchoredPosition;
            }

            if (m_statusEffectTemplate == null)
                m_statusEffectTemplate = UnityEngine.Object.Instantiate(m_statusEffectTemplateOriginal, Hud.instance.m_statusEffectListRoot);

            m_statusEffectTemplate.gameObject.SetActive(value: false);
            m_statusEffectTemplate.name = templateName;

            int size = Game.m_noMap ? statusEffectsElementSizeNomap.Value : statusEffectsElementSize.Value;

            RectTransform icon = m_statusEffectTemplate.Find("Icon").GetComponent<RectTransform>();
            icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            icon.anchoredPosition = new Vector2(-size, 0);

            RectTransform cooldown = m_statusEffectTemplate.Find("Cooldown").GetComponent<RectTransform>();
            cooldown.sizeDelta = icon.sizeDelta;
            cooldown.anchoredPosition = icon.anchoredPosition;

            RectTransform rectName = m_statusEffectTemplate.Find("Name").GetComponent<RectTransform>();
            rectName.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size + 4);
            rectName.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
            rectName.anchorMin = Vector2.one;
            rectName.anchorMax = Vector2.one;
            rectName.localPosition = new Vector2(64, icon.localPosition.y);

            TextMeshProUGUI text = rectName.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontSizeMax = size / 2;
            text.fontSizeMin = size / 2;

            RectTransform timeText = m_statusEffectTemplate.Find("TimeText").GetComponent<RectTransform>();
            timeText.sizeDelta = rectName.sizeDelta;
            timeText.anchorMin = rectName.anchorMin;
            timeText.anchorMax = rectName.anchorMax;
            timeText.localPosition = rectName.localPosition;

            TextMeshProUGUI textTime = timeText.GetComponent<TextMeshProUGUI>();
            textTime.alignment = TextAlignmentOptions.BottomLeft;
            textTime.textWrappingMode = text.textWrappingMode;
            textTime.fontSizeMax = text.fontSizeMax;
            textTime.fontSizeMin = text.fontSizeMin;

            // Force update on settings change
            if (Hud.instance.m_statusEffects.Count > 0)
            {
                UnityEngine.Object.Destroy(Hud.instance.m_statusEffects.Last().gameObject);
                Hud.instance.m_statusEffects.RemoveAt(Hud.instance.m_statusEffects.Count - 1);
            }

            Hud.instance.m_statusEffectListRoot.anchoredPosition = modEnabled.Value && GetStatusEffectsPositionEnabled() ? GetStatusEffectsPositionAnchor() : m_statusEffectListRootPositionOriginal;
            Hud.instance.m_statusEffectTemplate = modEnabled.Value && GetStatusEffectsElementEnabled() ? m_statusEffectTemplate : m_statusEffectTemplateOriginal;
        }

        public static void ChangeSailingIndicator()
        {
            if (Hud.instance == null)
                return;

            if (m_shipWindIndicatorRootPositionOriginal == Vector2.zero)
            {
                m_shipWindIndicatorRootPositionOriginal = Hud.instance.m_shipWindIndicatorRoot.anchoredPosition;
                m_shipWindIconRootPositionOriginal = Hud.instance.m_shipHudRoot.transform.Find("PowerIcon").GetComponent<RectTransform>().anchoredPosition;
            }

            Hud.instance.m_shipWindIndicatorRoot.anchoredPosition = modEnabled.Value && GetSailingIndicatorEnabled() ? GetSailingIndicatorWindIndicatorPosition() : m_shipWindIndicatorRootPositionOriginal;
            Hud.instance.m_shipWindIndicatorRoot.localScale = Vector3.one * (modEnabled.Value && GetSailingIndicatorEnabled() ? GetSailingIndicatorWindIndicatorScale() : 1f);

            Hud.instance.m_shipHudRoot.transform.Find("PowerIcon").GetComponent<RectTransform>().anchoredPosition = modEnabled.Value && GetSailingIndicatorEnabled() ? GetSailingIndicatorPowerIconPosition() : m_shipWindIconRootPositionOriginal;
            Hud.instance.m_shipHudRoot.transform.Find("PowerIcon").GetComponent<RectTransform>().localScale = Vector3.one * (modEnabled.Value && GetSailingIndicatorEnabled() ? GetSailingIndicatorPowerIconScale() : 1f);
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        public static class Hud_Awake_CustomTemplate
        {
            public static void Postfix()
            {
                UpdateStatusEffectList();
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
        public static class Hud_OnDestroy_CustomTemplate
        {
            public static void Prefix()
            {
                m_statusEffectTemplate = null;
                m_statusEffectTemplateOriginal = null;
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateStatusEffects))]
        public static class Hud_UpdateStatusEffects_Patch
        {
            public static void Postfix(List<RectTransform> ___m_statusEffects)
            {
                if (!modEnabled.Value)
                    return;

                if (!GetStatusEffectsPositionEnabled())
                    return;

                for (int i = 0; i < ___m_statusEffects.Count; i++)
                {
                    ___m_statusEffects[i].anchoredPosition = GetStatusEffectPosition(i);
                    if (GetStatusEffectsElementEnabled())
                    {
                        bool textHidden = !___m_statusEffects[i].Find("TimeText").gameObject.activeSelf;
                        ___m_statusEffects[i].Find("Name").GetComponent<TMP_Text>().verticalAlignment = textHidden ? VerticalAlignmentOptions.Middle : VerticalAlignmentOptions.Top;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.UpdateNoMap))]
        public static class Game_UpdateNoMap_UpdateForecastPosition
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                UpdateStatusEffectList();
            }
        }

        private static Vector2 GetStatusEffectsPositionAnchor()
        {
            if (fixStatusEffectAndForecastPosition.Value && instance.Info.Metadata.Version >= new System.Version("1.0.11") && statusEffectsPositionAnchor.Value == new Vector2(-170f, -240f))
                statusEffectsPositionAnchor.Value = (Vector2)statusEffectsPositionAnchor.DefaultValue;

            return Game.m_noMap ? statusEffectsPositionAnchorNomap.Value : statusEffectsPositionAnchor.Value;
        }

        private static bool GetSailingIndicatorEnabled()
        {
            return Game.m_noMap ? sailingIndicatorEnabledNomap.Value : sailingIndicatorEnabled.Value;
        }

        private static Vector2 GetSailingIndicatorWindIndicatorPosition()
        {
            return Game.m_noMap ? sailingIndicatorWindIndicatorPositionNomap.Value : sailingIndicatorWindIndicatorPosition.Value;
        }

        private static float GetSailingIndicatorWindIndicatorScale()
        {
            return Game.m_noMap ? sailingIndicatorWindIndicatorScaleNomap.Value : sailingIndicatorWindIndicatorScale.Value;
        }

        private static Vector2 GetSailingIndicatorPowerIconPosition()
        {
            return Game.m_noMap ? sailingIndicatorPowerIconPositionNomap.Value : sailingIndicatorPowerIconPosition.Value;
        }

        private static float GetSailingIndicatorPowerIconScale()
        {
            return Game.m_noMap ? sailingIndicatorPowerIconScaleNomap.Value : sailingIndicatorPowerIconScale.Value;
        }

        private static bool GetStatusEffectsPositionEnabled()
        {
            return Game.m_noMap ? statusEffectsPositionEnabledNomap.Value : statusEffectsPositionEnabled.Value;
        }

        private static bool GetStatusEffectsElementEnabled()
        {
            return Game.m_noMap ? statusEffectsElementEnabledNomap.Value : statusEffectsElementEnabled.Value;
        }
    }
}
