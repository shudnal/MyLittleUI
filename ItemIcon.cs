using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using static MyLittleUI.MyLittleUI;
using BepInEx;
using System.Linq;

namespace MyLittleUI
{
    internal static class ItemIcon
    {
        private static Vector3 itemIconScaleOriginal = Vector3.zero;
        private static Color itemEquippedColorOriginal = Color.clear;
        private static Gradient gradient;

        private static void UpdateItemIcon(GuiBar durability, Image icon, Image equiped)
        {
            if (itemIconScaleOriginal == Vector3.zero)
                itemIconScaleOriginal = icon.transform.localScale;

            icon.transform.localScale = itemIconScaleOriginal * Mathf.Clamp(itemIconScale.Value, 0.2f, 2f);

            if (equiped != null)
            {
                if (itemEquippedColorOriginal == Color.clear)
                    itemEquippedColorOriginal = equiped.color;

                if (itemEquippedColor.Value != Color.clear)
                    equiped.color = itemEquippedColor.Value;
                else if (itemEquippedColorOriginal != Color.clear)
                    equiped.color = itemEquippedColorOriginal;
            }

            if (durabilityEnabled.Value && durability != null)
            {
                if (!durability.m_barImage && durability.m_bar)
                    durability.m_barImage = durability.m_bar.GetComponent<Image>();

                UpdateGradient(force: false);

                if (durability.GetSmoothValue() >= 1f)
                {
                    if (durability.GetColor() == Color.red)
                        durability.SetColor(durabilityBroken.Value);
                    else if (durability.GetColor() != Color.clear)
                        durability.gameObject.SetActive(false);
                }
                else
                {
                    durability.SetColor(gradient.Evaluate(durability.GetSmoothValue()));
                }
            }
        }

        public static void UpdateGradient(bool force = true)
        {
            if (gradient == null || force)
            {
                gradient = new Gradient();
                gradient.SetKeys(new GradientColorKey[4]
                                    {
                                        new GradientColorKey(durabilityBroken.Value, 0.0f),
                                        new GradientColorKey(durabilityAtRisk.Value, 0.33f),
                                        new GradientColorKey(durabilityWorn.Value, 0.66f),
                                        new GradientColorKey(durabilityFine.Value, 1.0f)
                                    },
                                 Array.Empty<GradientAlphaKey>());
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
            private static readonly HashSet<ItemDrop.ItemData> filterItemQuality = new HashSet<ItemDrop.ItemData>();
            private static readonly HashSet<ItemDrop.ItemData> hideItemQuality = new HashSet<ItemDrop.ItemData>();

            private static bool IsEAQSEquipmentGrid(InventoryGrid grid) => grid.name == "EquipmentSlotGrid";
            private static bool IsEAQSQuickSlotGrid(InventoryGrid grid) => grid.name == "QuickSlotGrid";

            private static IEnumerable<ItemDrop.ItemData> GetAzuEPIEqupmentSlotsItems()
            {
                return AzuExtendedPlayerInventory.API.GetSlots().GetItemFuncs.Where(func => func != null).Select(func => func.Invoke(Player.m_localPlayer)).Where(item => item != null);
            }

            private static void FillItemsToFilter()
            {
                if (AzuExtendedPlayerInventory.API.IsLoaded())
                {
                    if (itemQualityIgnoreCustomSlots.Value)
                        GetAzuEPIEqupmentSlotsItems().Do(item => filterItemQuality.Add(item));
                    else if (itemQualityIgnoreCustomEquipmentSlots.Value)
                        GetAzuEPIEqupmentSlotsItems().DoIf(item => item.IsEquipable(), item => filterItemQuality.Add(item));
                }

                if (ExtraSlotsAPI.API.IsReady())
                {
                    if (itemQualityIgnoreCustomSlots.Value)
                        ExtraSlotsAPI.API.GetAllExtraSlotsItems().Do(item => filterItemQuality.Add(item));
                    else if (itemQualityIgnoreCustomEquipmentSlots.Value)
                        ExtraSlotsAPI.API.GetEquipmentSlotsItems().Do(item => filterItemQuality.Add(item));
                }
            }

            private static void FillItemsToHide()
            {
                if (AzuExtendedPlayerInventory.API.IsLoaded())
                    GetAzuEPIEqupmentSlotsItems().Do(item => hideItemQuality.Add(item));

                if (ExtraSlotsAPI.API.IsReady())
                    ExtraSlotsAPI.API.GetEquipmentSlotsItems().Do(item => hideItemQuality.Add(item));
            }

            private static bool IgnoreItemQuality(InventoryGrid grid, ItemDrop.ItemData item)
            {
                return filterItemQuality.Contains(item) ||
                      itemQualityIgnoreCustomEquipmentSlots.Value && IsEAQSEquipmentGrid(grid) ||
                      itemQualityIgnoreCustomSlots.Value && (IsEAQSQuickSlotGrid(grid) || IsEAQSEquipmentGrid(grid));
            }

            private static bool HideEquipmentSlotsQuality(InventoryGrid grid, ItemDrop.ItemData item)
            {
                return IsEAQSEquipmentGrid(grid) || hideItemQuality.Contains(item);
            }

            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter("Azumatt.AzuExtendedPlayerInventory", "shudnal.ExtraSlots")]
            private static void Postfix(InventoryGrid __instance, Inventory ___m_inventory, List<InventoryGrid.Element> ___m_elements)
            {
                if (!modEnabled.Value)
                    return;

                filterItemQuality.Clear();
                if (itemQualityIgnoreCustomEquipmentSlots.Value || itemQualityIgnoreCustomSlots.Value)
                    FillItemsToFilter();

                hideItemQuality.Clear();
                if (itemQualityHideCustomEquipmentSlots.Value)
                    FillItemsToHide();

                int width = ___m_inventory.GetWidth();
                foreach (ItemDrop.ItemData item in ___m_inventory.GetAllItems())
                {
                    int index = item.m_gridPos.y * width + item.m_gridPos.x;
                    if (0 <= index && index < ___m_elements.Count)
                    {
                        InventoryGrid.Element element = ___m_elements[index];
                        UpdateItemIcon(element.m_durability, element.m_icon, element.m_equiped);

                        if (HideEquipmentSlotsQuality(__instance, item) || (itemQualityHideLvl1.Value && item.m_quality < 2))
                            element.m_quality.SetText("");
                        else if (!IgnoreItemQuality(__instance, item))
                            UpdateItemQuality(element.m_quality, item.m_quality);

                        if (inventoryHideLongStack.Value && element.m_amount.text.Length > 7)
                            element.m_amount.text = element.m_amount.text.Substring(0, element.m_amount.text.IndexOf('/'));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        private class HotkeyBar_UpdateIcons_DurabilityAndScale
        {
            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter("Azumatt.AzuExtendedPlayerInventory", "shudnal.ExtraSlots")]
            private static void Postfix(HotkeyBar __instance, Player player)
            {
                if (!modEnabled.Value)
                    return;

                if (!player || player.IsDead())
                    return;

                foreach(HotkeyBar.ElementData element in __instance.m_elements)
                {
                    UpdateItemIcon(element.m_durability, element.m_icon, itemEquippedColor.Value != Color.clear ? element.m_equiped.GetComponent<Image>() : null);
                    if (inventoryHideLongStack.Value && element.m_amount.text.Length > 9)
                        element.m_amount.text = element.m_stackText.ToFastString();
                }
            }
        }
    }
}