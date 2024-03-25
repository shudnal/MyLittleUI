using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
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
        private static Vector2 m_statusEffectListRootPositionOriginal = Vector3.zero;

        private static RectTransform m_statusEffectTemplate;

        private static Vector2 m_shipWindIndicatorRootPositionOriginal = Vector3.zero;
        private static Vector2 m_shipWindIconRootPositionOriginal = Vector3.zero;
       
        private static Vector3 GetStatusEffectPosition(int i)
        {
            float offset = i * (Game.m_noMap ? statusEffectsPositionSpacingNomap.Value + statusEffectsElementSizeNomap.Value : statusEffectsPositionSpacing.Value + statusEffectsElementSize.Value);
            return statusEffectsFillingDirection.Value switch
            {
                StatusEffectDirection.LeftToRight => new Vector3(offset, 0, 0),
                StatusEffectDirection.RightToLeft => new Vector3(-offset, 0, 0),
                StatusEffectDirection.TopToBottom => new Vector3(0, -offset, 0),
                StatusEffectDirection.BottomToTop => new Vector3(0, offset, 0),
                _ => new Vector3(-offset, 0, 0),
            };
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

            if (Game.m_noMap)
            {
                Hud.instance.m_statusEffectListRoot.anchoredPosition = modEnabled.Value && statusEffectsPositionEnabledNomap.Value ? statusEffectsPositionAnchorNomap.Value : m_statusEffectListRootPositionOriginal;
                Hud.instance.m_statusEffectTemplate = modEnabled.Value && statusEffectsElementEnabledNomap.Value ? m_statusEffectTemplate : m_statusEffectTemplateOriginal;
            }
            else
            {
                Hud.instance.m_statusEffectListRoot.anchoredPosition = modEnabled.Value && statusEffectsPositionEnabled.Value ? statusEffectsPositionAnchor.Value : m_statusEffectListRootPositionOriginal;
                Hud.instance.m_statusEffectTemplate = modEnabled.Value && statusEffectsElementEnabled.Value ? m_statusEffectTemplate : m_statusEffectTemplateOriginal;
            }
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

            if (Game.m_noMap)
            {
                Hud.instance.m_shipWindIndicatorRoot.anchoredPosition = modEnabled.Value && sailingIndicatorEnabledNomap.Value ? sailingIndicatorWindIndicatorPositionNomap.Value : m_shipWindIndicatorRootPositionOriginal;
                Hud.instance.m_shipWindIndicatorRoot.localScale = Vector3.one * (modEnabled.Value && sailingIndicatorEnabledNomap.Value ? sailingIndicatorWindIndicatorScaleNomap.Value : 1f);

                Hud.instance.m_shipHudRoot.transform.Find("PowerIcon").GetComponent<RectTransform>().anchoredPosition = modEnabled.Value && sailingIndicatorEnabledNomap.Value ? sailingIndicatorPowerIconPositionNomap.Value : m_shipWindIconRootPositionOriginal;
                Hud.instance.m_shipHudRoot.transform.Find("PowerIcon").GetComponent<RectTransform>().localScale = Vector3.one * (modEnabled.Value && sailingIndicatorEnabledNomap.Value ? sailingIndicatorPowerIconScaleNomap.Value : 1f);
            }
            else
            {
                Hud.instance.m_shipWindIndicatorRoot.anchoredPosition = modEnabled.Value && sailingIndicatorEnabled.Value ? sailingIndicatorWindIndicatorPosition.Value : m_shipWindIndicatorRootPositionOriginal;
                Hud.instance.m_shipWindIndicatorRoot.localScale = Vector3.one * (modEnabled.Value && sailingIndicatorEnabled.Value ? sailingIndicatorWindIndicatorScale.Value : 1f);

                Hud.instance.m_shipHudRoot.transform.Find("PowerIcon").GetComponent<RectTransform>().anchoredPosition = modEnabled.Value && sailingIndicatorEnabled.Value ? sailingIndicatorPowerIconPosition.Value : m_shipWindIconRootPositionOriginal;
                Hud.instance.m_shipHudRoot.transform.Find("PowerIcon").GetComponent<RectTransform>().localScale = Vector3.one * (modEnabled.Value && sailingIndicatorEnabled.Value ? sailingIndicatorPowerIconScale.Value : 1f);
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        public static class Hud_Awake_CustomTemplate
        {
            public static void Postfix()
            {
                InitializeStatusEffectTemplate();
                ChangeSailingIndicator();
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

                if (Game.m_noMap)
                {
                    if (!statusEffectsPositionEnabledNomap.Value)
                        return;

                    for (int i = 0; i < ___m_statusEffects.Count; i++)
                    {
                        ___m_statusEffects[i].anchoredPosition = GetStatusEffectPosition(i);
                        if (statusEffectsElementEnabledNomap.Value)
                        {
                            bool textHidden = !___m_statusEffects[i].Find("TimeText").gameObject.activeSelf;
                            ___m_statusEffects[i].Find("Name").GetComponent<TMP_Text>().verticalAlignment = textHidden ? VerticalAlignmentOptions.Middle : VerticalAlignmentOptions.Top;
                        }
                    }
                }
                else
                {
                    if (!statusEffectsPositionEnabled.Value)
                        return;

                    for (int i = 0; i < ___m_statusEffects.Count; i++)
                    {
                        ___m_statusEffects[i].anchoredPosition = GetStatusEffectPosition(i);
                        if (statusEffectsElementEnabled.Value)
                        {
                            bool textHidden = !___m_statusEffects[i].Find("TimeText").gameObject.activeSelf;
                            ___m_statusEffects[i].Find("Name").GetComponent<TMP_Text>().verticalAlignment = textHidden ? VerticalAlignmentOptions.Middle : VerticalAlignmentOptions.Top;
                        }
                    }
                }
            }
        }
    }
}
