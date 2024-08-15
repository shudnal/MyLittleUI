using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HotkeyBar;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    internal class AmmoCountIcon
    {
        public class ElementExtraData
        {
            public TMP_Text m_ammo;
            public Image m_icon;
            public Image m_bait;
        }

        internal const string objectAmmoName = "MLUI_AmmoAmount";
        internal const string objectIconName = "MLUI_AmmoIcon";
        internal const string objectBaitName = "MLUI_AmmoBait";

        internal static GameObject elementPrefab;

        internal static GameObject ammoCount;
        internal static GameObject ammoIcon;
        internal static GameObject ammoBait;
        internal static bool isDirty;

        internal static readonly Dictionary<ElementData, ElementExtraData> elementExtraDatas = new Dictionary<ElementData, ElementExtraData>();

        public static void UpdateVisibility()
        {
            RectTransform rtAmmo = ammoCount.GetComponent<RectTransform>();
            rtAmmo.anchoredPosition = ammoCountPosition.Value;

            TMP_Text text = ammoCount.GetComponent<TMP_Text>();
            text.fontSize = ammoCountFontSize.Value;
            text.color = ammoCountColor.Value == Color.clear ? elementPrefab.transform.Find("amount").GetComponent<TMP_Text>().color : ammoCountColor.Value;
            text.horizontalAlignment = ammoCountAlignment.Value;

            RectTransform rtIcon = ammoIcon.GetComponent<RectTransform>();
            rtIcon.anchoredPosition = ammoIconPosition.Value;
            rtIcon.sizeDelta = ammoIconSize.Value;

            RectTransform rtBait = ammoBait.GetComponent<RectTransform>();
            rtBait.anchorMin = new Vector2(0.5f, 0f);
            rtBait.anchorMax = new Vector2(1f, 0.5f);

            isDirty = true;
        }

        private static void UpdateItemState(ItemDrop.ItemData item, ElementData elementData)
        {
            if (elementData == null)
                return;

            GameObject element = elementData.m_go;

            if (element == null)
                return;

            ElementExtraData extraData = elementExtraDatas[elementData];
            if (extraData == null)
                return;

            ItemDrop.ItemData ammoItem = GetCurrentAmmo(item);
            int amount = ammoItem == null ? 0 : Player.m_localPlayer.GetInventory().CountItems(ammoItem.m_shared.m_name);
            extraData.m_ammo?.gameObject.SetActive(ammoCountEnabled.Value && amount > 0);

            if (amount > 0)
                extraData.m_ammo?.SetText(amount.ToString());

            Sprite sprite = ammoItem?.GetIcon();
            extraData.m_icon?.gameObject.SetActive(ammoIconEnabled.Value && sprite != null);

            if (sprite != null && extraData.m_icon != null)
                extraData.m_icon.overrideSprite = sprite;

            ItemDrop.ItemData baitItem = GetCurrentBait(item);

            Sprite baitSprite = baitItem?.GetIcon();
            extraData.m_bait?.gameObject.SetActive(baitIconEnabled.Value && baitSprite != null);

            if (baitSprite != null && extraData.m_bait != null)
                extraData.m_bait.overrideSprite = baitSprite;

            int baits = baitItem == null ? 0 : Player.m_localPlayer.GetInventory().CountItems(baitItem.m_shared.m_name);
            if (baits > 0 && baitCountEnabled.Value && !elementData.m_amount.gameObject.activeSelf)
            {
                elementData.m_amount.gameObject.SetActive(true);
                elementData.m_amount.SetText(baits.ToString());
            }
        }

        private static ItemDrop.ItemData GetCurrentAmmo(ItemDrop.ItemData weapon)
        {
            if (weapon == null || weapon.m_shared.m_ammoType.IsNullOrWhiteSpace() || IsBaitAmmo(weapon) || IsAmmo(weapon))
                return null;

            ItemDrop.ItemData ammo = Player.m_localPlayer.GetAmmoItem();
            if (ammo == null || ammo.m_shared.m_ammoType != weapon.m_shared.m_ammoType || !Player.m_localPlayer.GetInventory().ContainsItem(ammo))
                ammo = Player.m_localPlayer.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType);

            return ammo;
        }

        private static ItemDrop.ItemData GetCurrentBait(ItemDrop.ItemData weapon)
        {
            if (weapon == null || weapon.m_shared.m_ammoType.IsNullOrWhiteSpace() || !IsBaitAmmo(weapon) || IsAmmo(weapon))
                return null;

            ItemDrop.ItemData ammo = Player.m_localPlayer.GetAmmoItem();
            if (ammo == null || ammo.m_shared.m_ammoType != weapon.m_shared.m_ammoType || !Player.m_localPlayer.GetInventory().ContainsItem(ammo))
                ammo = Player.m_localPlayer.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType);

            return ammo;
        }

        private static bool IsAmmo(ItemDrop.ItemData item)
        {
            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable;
        }

        private static bool IsBaitAmmo(ItemDrop.ItemData item)
        {
            return item.m_shared.m_ammoType == "fishingbait";
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.OnEnable))]
        public static class Game_UpdateNoMap_UpdateForecastPosition
        {
            private static void FillFields(RectTransform rtSource, RectTransform rtDestination)
            {
                rtDestination.anchoredPosition = rtSource.anchoredPosition;
                rtDestination.sizeDelta = rtSource.sizeDelta;
            }

            public static void Postfix(HotkeyBar __instance)
            {
                if (__instance.m_elementPrefab == null)
                    return;

                elementPrefab = __instance.m_elementPrefab;

                int index = __instance.m_elementPrefab.transform.Find("icon").GetSiblingIndex() + 1;

                ammoCount = __instance.m_elementPrefab.transform.Find(objectAmmoName)?.gameObject;

                if (ammoCount == null)
                {
                    GameObject ammo = __instance.m_elementPrefab.transform.Find("amount").gameObject;
                    ammoCount = UnityEngine.Object.Instantiate(ammo);
                    ammoCount.name = objectAmmoName;
                    ammoCount.transform.SetParent(__instance.m_elementPrefab.transform);
                    ammoCount.transform.SetSiblingIndex(index);
                    index++;

                    ammoCount.SetActive(false);

                    FillFields(ammo.GetComponent<RectTransform>(), ammoCount.GetComponent<RectTransform>());
                }

                ammoIcon = __instance.m_elementPrefab.transform.Find(objectIconName)?.gameObject;

                if (ammoIcon == null)
                {
                    GameObject icon = __instance.m_elementPrefab.transform.Find("icon").gameObject;
                    ammoIcon = UnityEngine.Object.Instantiate(icon);
                    ammoIcon.name = objectIconName;
                    ammoIcon.transform.SetParent(__instance.m_elementPrefab.transform);
                    ammoIcon.transform.SetSiblingIndex(index);
                    index++;

                    ammoIcon.SetActive(false);

                    FillFields(icon.GetComponent<RectTransform>(), ammoIcon.GetComponent<RectTransform>());
                }

                ammoBait = __instance.m_elementPrefab.transform.Find(objectBaitName)?.gameObject;

                if (ammoBait == null)
                {
                    GameObject icon = __instance.m_elementPrefab.transform.Find("icon").gameObject;
                    ammoBait = UnityEngine.Object.Instantiate(icon);
                    ammoBait.name = objectBaitName;
                    ammoBait.transform.SetParent(__instance.m_elementPrefab.transform);
                    ammoBait.transform.SetSiblingIndex(index);

                    ammoBait.SetActive(false);

                    FillFields(icon.GetComponent<RectTransform>(), ammoBait.GetComponent<RectTransform>());
                }

                UpdateVisibility();
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
        public static class HotkeyBar_Update_UpdateAmmoIconCountDirtyState
        {
            public static void Prefix(HotkeyBar __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (isDirty)
                {
                    isDirty = false;
                    if (__instance.m_elements.Count > 0)
                        __instance.UpdateIcons(null);
                }
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        public static class HotkeyBar_UpdateIcons_UpdateAmmoCountAndIcon
        {
            public static void Postfix(HotkeyBar __instance, Player player)
            {
                if (!modEnabled.Value)
                    return;

                if (!player || player.IsDead())
                {
                    elementExtraDatas.Clear();
                    return;
                }

                if (elementExtraDatas.Count != __instance.m_elements.Count)
                    elementExtraDatas.Clear();

                if (elementExtraDatas.Count > 0 && __instance.m_elements.Count > 0 && !elementExtraDatas.ContainsKey(__instance.m_elements[0]))
                    elementExtraDatas.Clear();

                if (elementExtraDatas.Count == 0)
                {
                    for (int i = 0; i < __instance.m_elements.Count; i++)
                    {
                        ElementData elementData = __instance.m_elements[i];

                        ElementExtraData extraData = new ElementExtraData()
                        {
                            m_ammo = elementData.m_go.transform.Find(objectAmmoName).GetComponent<TMP_Text>(),
                            m_icon = elementData.m_go.transform.Find(objectIconName).GetComponent<Image>(),
                            m_bait = elementData.m_go.transform.Find(objectBaitName).GetComponent<Image>()
                        };

                        elementExtraDatas.Add(elementData, extraData);
                    }
                }

                foreach (ElementExtraData extraData in elementExtraDatas.Values)
                {
                    extraData.m_ammo?.gameObject.SetActive(false);
                    extraData.m_icon?.gameObject.SetActive(false);
                    extraData.m_bait?.gameObject.SetActive(false);
                }

                for (int i = 0; i < __instance.m_items.Count; i++)
                {
                    ItemDrop.ItemData item = __instance.m_items[i];
                    if (item != null && 0 <= item.m_gridPos.x && item.m_gridPos.x < __instance.m_elements.Count)
                        UpdateItemState(item, __instance.m_elements[item.m_gridPos.x]);
                }
            }
        }
    }
}
