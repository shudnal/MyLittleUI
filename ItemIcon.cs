using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using static MyLittleUI.MyLittleUI;
using BepInEx;

namespace MyLittleUI
{
    internal static class ItemIcon
    {
        private static Vector3 itemIconScaleOriginal = Vector3.zero;

        private static void UpdateItemIcon(GuiBar durability, Image icon, ItemDrop.ItemData item)
        {
            if (itemIconScaleOriginal == Vector3.zero)
                itemIconScaleOriginal = icon.transform.localScale;

            if (itemIconScale.Value != 1f)
                icon.transform.localScale = itemIconScaleOriginal * Mathf.Clamp(itemIconScale.Value, 0.2f, 2f);

            if (durabilityEnabled.Value && item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability())
            {
                if (item.m_durability <= 0f)
                {
                    durability.SetValue(1f);
                    durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? durabilityBroken.Value : new Color(0f, 0f, 0f, 0f));
                }
                else
                {
                    float percentage = item.GetDurabilityPercentage();
                    durability.SetValue(percentage);
                    if (percentage >= 0.75f)
                        durability.SetColor(durabilityFine.Value);
                    else if (percentage >= 0.50f)
                        durability.SetColor(durabilityWorn.Value);
                    else if (percentage >= 0.25f)
                        durability.SetColor(durabilityAtRisk.Value);
                    else
                        durability.SetColor(durabilityBroken.Value);
                }
            }
        }

        private static char GetQualitySymbol()
        {
            return itemQualitySymbol.Value.IsNullOrWhiteSpace() ? '★' : itemQualitySymbol.Value[0];
        }

        private static readonly Dictionary<int, string> qualityCache = new Dictionary<int, string>();
        public static void FillItemQualityCache()
        {
            StringBuilder sb = new StringBuilder();

            qualityCache.Clear();
            int maxSymbols = Math.Min(itemQualityMax.Value, itemQualityColumns.Value * itemQualityRows.Value);
            for (int i = 1; i <= maxSymbols; i++)
            {
                sb.Append(GetQualitySymbol());

                qualityCache.Add(i, sb.ToString());

                if (i % (itemQualityColumns.Value) == 0)
                    sb.Append("\n");
            }
        }

        private static class DefaultQualityStyle
        {
            public static bool initialized = false;
            public static TextWrappingModes textWrappingMode;
            public static float fontSize;
            public static Color color;
            public static bool isRightToLeftText;
            public static float lineSpacing;
            public static float characterSpacing;

            public static void Save(TMP_Text quality)
            {
                initialized = true;
                textWrappingMode = quality.textWrappingMode;
                fontSize = quality.fontSize;
                color = quality.color;
                isRightToLeftText = quality.isRightToLeftText;
                lineSpacing = quality.lineSpacing;
                characterSpacing = quality.characterSpacing;
            }

            public static void Load(TMP_Text quality)
            {
                quality.textWrappingMode = textWrappingMode;
                quality.fontSize = fontSize;
                quality.color = color;
                quality.isRightToLeftText = isRightToLeftText;
                quality.lineSpacing = lineSpacing;
                quality.characterSpacing = characterSpacing;
            }

            public static bool IsTextWasChanged(TMP_Text quality)
            {
                return quality.textWrappingMode != textWrappingMode
                     || quality.fontSize != fontSize
                     || quality.color != color
                     || quality.isRightToLeftText != isRightToLeftText
                     || quality.lineSpacing != lineSpacing
                     || quality.lineSpacing != characterSpacing;
            }
        };

        private static void UpdateItemQuality(TMP_Text quality, int m_quality)
        {
            if (!itemQuality.Value || !quality.enabled)
                return;

            if (!DefaultQualityStyle.initialized)
                DefaultQualityStyle.Save(quality);

            if (!qualityCache.ContainsKey(m_quality))
            {
                if (DefaultQualityStyle.IsTextWasChanged(quality))
                    DefaultQualityStyle.Load(quality);
                return;
            }

            quality.text = qualityCache[m_quality];
            quality.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
            quality.fontSize = itemQualitySymbolSize.Value;
            quality.color = itemQualitySymbolColor.Value;
            quality.isRightToLeftText = true;
            quality.lineSpacing = itemQualityLineSpacing.Value;
            quality.characterSpacing = itemQualityCharacterSpacing.Value;
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        private class InventoryGrid_UpdateGui_DurabilityAndScale
        {
            private static void Postfix(Inventory ___m_inventory, List<InventoryGrid.Element> ___m_elements)
            {
                if (!modEnabled.Value)
                    return;

                int width = ___m_inventory.GetWidth();

                foreach (ItemDrop.ItemData item in ___m_inventory.GetAllItems())
                {
                    int index = item.m_gridPos.y * width + item.m_gridPos.x;
                    if (0 <= index && index < ___m_elements.Count)
                    {
                        InventoryGrid.Element element = ___m_elements[index];
                        UpdateItemIcon(element.m_durability, element.m_icon, item);
                        UpdateItemQuality(element.m_quality, item.m_quality);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        private class HotkeyBar_UpdateIcons_DurabilityAndScale
        {
            private static void Postfix(Player player, List<ItemDrop.ItemData> ___m_items, List<HotkeyBar.ElementData> ___m_elements)
            {
                if (!modEnabled.Value)
                    return;

                if (!player || player.IsDead())
                    return;

                for (int j = 0; j < ___m_items.Count; j++)
                {
                    ItemDrop.ItemData item = ___m_items[j];
                    if (item != null && 0 <= item.m_gridPos.x && item.m_gridPos.x < ___m_elements.Count)
                    {
                        HotkeyBar.ElementData element = ___m_elements[item.m_gridPos.x];
                        UpdateItemIcon(element.m_durability, element.m_icon, item);
                    }
                }
            }
        }
    }
}
