using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    // Each live bar owns one set. The container is outside the HUD hierarchy so
    // cloning a bar cannot copy templates or references to another bar.
    internal sealed class AmmoCountTemplates
    {
        private GameObject container;
        private GameObject sourcePrefab;
        private TMP_Text sourceAmount;
        private Image sourceIcon;
        private TextAppearance textAppearance;
        private ImageAppearance imageAppearance;

        internal TMP_Text Amount { get; private set; }
        internal Image Icon { get; private set; }
        internal Image Bait { get; private set; }
        internal Image Mead { get; private set; }
        internal bool Ready { get; private set; }
        internal int Revision { get; private set; }

        internal void Refresh(GameObject prefab, bool refreshDonors = false)
        {
            if (sourcePrefab != prefab || !sourceAmount || !sourceIcon || refreshDonors)
            {
                sourcePrefab = prefab;
                sourceAmount = prefab ? prefab.transform.Find("amount")?.GetComponent<TMP_Text>() : null;
                sourceIcon = prefab ? prefab.transform.Find("icon")?.GetComponent<Image>() : null;
            }

            Ready = sourceAmount && sourceAmount.font && sourceIcon && instance;
            if (!Ready)
                return;

            if (container && Amount && Icon && Bait && Mead
                && textAppearance != null && textAppearance.Matches(sourceAmount)
                && imageAppearance != null && imageAppearance.Matches(sourceIcon))
                return;

            textAppearance = new TextAppearance(sourceAmount);
            imageAppearance = new ImageAppearance(sourceIcon);

            if (!container)
            {
                container = new GameObject("MLUI_AmmoTemplates");
                container.SetActive(false);
                container.transform.SetParent(instance.transform, false);
            }

            // Build only the required rendering components. Do not clone donor
            // behaviours, event handlers, child decorations, or owner references.
            if (!Amount)
                Amount = CreateTemplate<TextMeshProUGUI>(AmmoCountIcon.objectAmmoName);
            if (!Icon)
                Icon = CreateTemplate<Image>(AmmoCountIcon.objectIconName);
            if (!Bait)
                Bait = CreateTemplate<Image>(AmmoCountIcon.objectBaitName);
            if (!Mead)
                Mead = CreateTemplate<Image>(AmmoCountIcon.objectMeadName);

            Amount.text = string.Empty;
            ApplySettings();
        }

        private T CreateTemplate<T>(string name) where T : Graphic
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            go.transform.SetParent(container.transform, false);
            return go.AddComponent<T>();
        }

        internal void ApplySettings()
        {
            if (textAppearance == null || imageAppearance == null)
                return;

            ApplyAmount(Amount);
            ApplyIcon(Icon, 0);
            ApplyIcon(Bait, 1);
            ApplyIcon(Mead, 2);
            if (Amount)
                Amount.gameObject.SetActive(false);
            if (Icon)
                Icon.gameObject.SetActive(false);
            if (Bait)
                Bait.gameObject.SetActive(false);
            if (Mead)
                Mead.gameObject.SetActive(false);
            Revision++;
        }

        internal void ApplyAmount(TMP_Text text)
        {
            if (!text || textAppearance == null)
                return;

            textAppearance.Apply(text);
            text.rectTransform.anchoredPosition = ammoCountPosition.Value;
            text.fontSize = ammoCountFontSize.Value;
            text.color = ammoCountColor.Value == Color.clear ? textAppearance.Color : ammoCountColor.Value;
            text.horizontalAlignment = ammoCountAlignment.Value;
            text.raycastTarget = false;
        }

        internal void ApplyIcon(Image image, int kind)
        {
            if (!image || imageAppearance == null)
                return;

            imageAppearance.Apply(image);
            image.raycastTarget = false;
            if (kind == 0)
            {
                image.rectTransform.anchoredPosition = ammoIconPosition.Value;
                image.rectTransform.sizeDelta = ammoIconSize.Value;
            }
            else if (kind == 1)
            {
                image.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                image.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            }
            else
            {
                image.rectTransform.anchorMin = new Vector2(0.6f, 0.6f);
                image.rectTransform.anchorMax = new Vector2(1f, 1f);
            }
        }

        internal void Dispose()
        {
            Ready = false;
            if (container)
                Object.Destroy(container);
            container = null;
            sourcePrefab = null;
            sourceAmount = null;
            sourceIcon = null;
            Amount = null;
            Icon = null;
            Bait = null;
            Mead = null;
            textAppearance = null;
            imageAppearance = null;
        }

        // These snapshots contain only appearance values and shared UI resources.
        // They never hold a cell, a player, an item, or an event callback.
        private sealed class RectAppearance
        {
            private readonly Vector2 anchorMin;
            private readonly Vector2 anchorMax;
            private readonly Vector2 pivot;
            private readonly Vector2 sizeDelta;
            private readonly Vector3 anchoredPosition3D;
            private readonly Vector3 localScale;
            private readonly Quaternion localRotation;

            internal RectAppearance(RectTransform source)
            {
                anchorMin = source.anchorMin;
                anchorMax = source.anchorMax;
                pivot = source.pivot;
                sizeDelta = source.sizeDelta;
                anchoredPosition3D = source.anchoredPosition3D;
                localScale = source.localScale;
                localRotation = source.localRotation;
            }

            internal bool Matches(RectTransform source)
            {
                return anchorMin == source.anchorMin
                    && anchorMax == source.anchorMax
                    && pivot == source.pivot
                    && sizeDelta == source.sizeDelta
                    && anchoredPosition3D == source.anchoredPosition3D
                    && localScale == source.localScale
                    && localRotation == source.localRotation;
            }

            internal void Apply(RectTransform target)
            {
                target.anchorMin = anchorMin;
                target.anchorMax = anchorMax;
                target.pivot = pivot;
                target.sizeDelta = sizeDelta;
                target.anchoredPosition3D = anchoredPosition3D;
                target.localScale = localScale;
                target.localRotation = localRotation;
            }
        }

        private sealed class TextAppearance
        {
            private readonly RectAppearance rect;
            private readonly int layer;
            private readonly TMP_FontAsset font;
            private readonly Material fontSharedMaterial;
            private readonly TMP_SpriteAsset spriteAsset;
            private readonly FontStyles fontStyle;
            private readonly FontWeight fontWeight;
            private readonly float fontSize;
            private readonly float fontSizeMin;
            private readonly float fontSizeMax;
            private readonly bool enableAutoSizing;
            private readonly TextAlignmentOptions alignment;
            private readonly Color color;
            private readonly bool enableVertexGradient;
            private readonly VertexGradient colorGradient;
            private readonly TMP_ColorGradient colorGradientPreset;
            private readonly float characterSpacing;
            private readonly float wordSpacing;
            private readonly float lineSpacing;
            private readonly float paragraphSpacing;
            private readonly Vector4 margin;
            private readonly TextOverflowModes overflowMode;
            private readonly TextWrappingModes textWrappingMode;
            private readonly OTL_FeatureTag[] fontFeatures;
            private readonly bool richText;
            private readonly bool extraPadding;
            private readonly bool isRightToLeftText;
            private readonly bool maskable;

            internal Color Color => color;

            internal TextAppearance(TMP_Text source)
            {
                rect = new RectAppearance(source.rectTransform);
                layer = source.gameObject.layer;
                font = source.font;
                fontSharedMaterial = source.fontSharedMaterial;
                spriteAsset = source.spriteAsset;
                fontStyle = source.fontStyle;
                fontWeight = source.fontWeight;
                fontSize = source.fontSize;
                fontSizeMin = source.fontSizeMin;
                fontSizeMax = source.fontSizeMax;
                enableAutoSizing = source.enableAutoSizing;
                alignment = source.alignment;
                color = source.color;
                enableVertexGradient = source.enableVertexGradient;
                colorGradient = source.colorGradient;
                colorGradientPreset = source.colorGradientPreset;
                characterSpacing = source.characterSpacing;
                wordSpacing = source.wordSpacing;
                lineSpacing = source.lineSpacing;
                paragraphSpacing = source.paragraphSpacing;
                margin = source.margin;
                overflowMode = source.overflowMode;
                textWrappingMode = source.textWrappingMode;
                // Keep an independent snapshot so in-place donor changes are detected.
                fontFeatures = source.fontFeatures?.ToArray() ?? System.Array.Empty<OTL_FeatureTag>();
                richText = source.richText;
                extraPadding = source.extraPadding;
                isRightToLeftText = source.isRightToLeftText;
                maskable = source.maskable;
            }

            internal bool Matches(TMP_Text source)
            {
                return rect.Matches(source.rectTransform)
                    && layer == source.gameObject.layer
                    && font == source.font
                    && fontSharedMaterial == source.fontSharedMaterial
                    && spriteAsset == source.spriteAsset
                    && fontStyle == source.fontStyle
                    && fontWeight == source.fontWeight
                    && fontSize == source.fontSize
                    && fontSizeMin == source.fontSizeMin
                    && fontSizeMax == source.fontSizeMax
                    && enableAutoSizing == source.enableAutoSizing
                    && alignment == source.alignment
                    && color == source.color
                    && enableVertexGradient == source.enableVertexGradient
                    && colorGradient.topLeft == source.colorGradient.topLeft
                    && colorGradient.topRight == source.colorGradient.topRight
                    && colorGradient.bottomLeft == source.colorGradient.bottomLeft
                    && colorGradient.bottomRight == source.colorGradient.bottomRight
                    && colorGradientPreset == source.colorGradientPreset
                    && characterSpacing == source.characterSpacing
                    && wordSpacing == source.wordSpacing
                    && lineSpacing == source.lineSpacing
                    && paragraphSpacing == source.paragraphSpacing
                    && margin == source.margin
                    && overflowMode == source.overflowMode
                    && textWrappingMode == source.textWrappingMode
                    && MatchesFontFeatures(source.fontFeatures)
                    && richText == source.richText
                    && extraPadding == source.extraPadding
                    && isRightToLeftText == source.isRightToLeftText
                    && maskable == source.maskable;
            }

            private bool MatchesFontFeatures(List<OTL_FeatureTag> features)
            {
                int count = features?.Count ?? 0;
                if (fontFeatures.Length != count)
                    return false;

                for (int i = 0; i < count; i++)
                    if (fontFeatures[i] != features[i])
                        return false;

                return true;
            }

            internal void Apply(TMP_Text target)
            {
                rect.Apply(target.rectTransform);
                target.gameObject.layer = layer;
                target.font = font;
                target.fontSharedMaterial = fontSharedMaterial;
                target.spriteAsset = spriteAsset;
                target.fontStyle = fontStyle;
                target.fontWeight = fontWeight;
                target.fontSize = fontSize;
                target.fontSizeMin = fontSizeMin;
                target.fontSizeMax = fontSizeMax;
                target.enableAutoSizing = enableAutoSizing;
                target.alignment = alignment;
                target.color = color;
                target.enableVertexGradient = enableVertexGradient;
                target.colorGradient = colorGradient;
                target.colorGradientPreset = colorGradientPreset;
                target.characterSpacing = characterSpacing;
                target.wordSpacing = wordSpacing;
                target.lineSpacing = lineSpacing;
                target.paragraphSpacing = paragraphSpacing;
                target.margin = margin;
                target.overflowMode = overflowMode;
                target.textWrappingMode = textWrappingMode;
                // Each text component owns its list; never share the snapshot or donor list.
                target.fontFeatures = new List<OTL_FeatureTag>(fontFeatures);
                target.richText = richText;
                target.extraPadding = extraPadding;
                target.isRightToLeftText = isRightToLeftText;
                target.maskable = maskable;
            }
        }

        private sealed class ImageAppearance
        {
            private readonly RectAppearance rect;
            private readonly int layer;
            private readonly Material material;
            private readonly Sprite sprite;
            private readonly Color color;
            private readonly Image.Type type;
            private readonly bool preserveAspect;
            private readonly bool fillCenter;
            private readonly Image.FillMethod fillMethod;
            private readonly int fillOrigin;
            private readonly float fillAmount;
            private readonly bool fillClockwise;
            private readonly float pixelsPerUnitMultiplier;
            private readonly bool useSpriteMesh;
            private readonly bool maskable;

            internal ImageAppearance(Image source)
            {
                rect = new RectAppearance(source.rectTransform);
                layer = source.gameObject.layer;
                material = source.material;
                sprite = source.sprite;
                color = source.color;
                type = source.type;
                preserveAspect = source.preserveAspect;
                fillCenter = source.fillCenter;
                fillMethod = source.fillMethod;
                fillOrigin = source.fillOrigin;
                fillAmount = source.fillAmount;
                fillClockwise = source.fillClockwise;
                pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                useSpriteMesh = source.useSpriteMesh;
                maskable = source.maskable;
            }

            internal bool Matches(Image source)
            {
                return rect.Matches(source.rectTransform)
                    && layer == source.gameObject.layer
                    && material == source.material
                    && sprite == source.sprite
                    && color == source.color
                    && type == source.type
                    && preserveAspect == source.preserveAspect
                    && fillCenter == source.fillCenter
                    && fillMethod == source.fillMethod
                    && fillOrigin == source.fillOrigin
                    && fillAmount == source.fillAmount
                    && fillClockwise == source.fillClockwise
                    && pixelsPerUnitMultiplier == source.pixelsPerUnitMultiplier
                    && useSpriteMesh == source.useSpriteMesh
                    && maskable == source.maskable;
            }

            internal void Apply(Image target)
            {
                rect.Apply(target.rectTransform);
                target.gameObject.layer = layer;
                target.material = material;
                target.sprite = sprite;
                target.color = color;
                target.type = type;
                target.preserveAspect = preserveAspect;
                target.fillCenter = fillCenter;
                target.fillMethod = fillMethod;
                target.fillOrigin = fillOrigin;
                target.fillAmount = fillAmount;
                target.fillClockwise = fillClockwise;
                target.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
                target.useSpriteMesh = useSpriteMesh;
                target.maskable = maskable;
            }
        }
    }
}
