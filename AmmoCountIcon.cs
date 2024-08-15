using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
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
            public ItemDrop.ItemData m_item;

            public void UpdateState(ElementData elementData)
            {
                if (m_item == null)
                {
                    m_ammo?.gameObject.SetActive(false);
                    m_icon?.gameObject.SetActive(false);
                    m_bait?.gameObject.SetActive(false);
                    return;
                }

                ItemDrop.ItemData ammoItem = GetCurrentAmmo();
                int amount = ammoItem == null ? 0 : Player.m_localPlayer.GetInventory().CountItems(ammoItem.m_shared.m_name);
                m_ammo?.gameObject.SetActive(ammoCountEnabled.Value && amount > 0);

                if (amount > 0)
                    m_ammo?.SetText(amount.ToString());

                Sprite sprite = ammoItem?.GetIcon();
                m_icon?.gameObject.SetActive(ammoIconEnabled.Value && sprite != null);

                if (sprite != null && m_icon != null)
                    m_icon.overrideSprite = sprite;

                ItemDrop.ItemData baitItem = GetCurrentBait();

                Sprite baitSprite = baitItem?.GetIcon();
                m_bait?.gameObject.SetActive(baitIconEnabled.Value && baitSprite != null);

                if (baitSprite != null && m_bait != null)
                    m_bait.overrideSprite = baitSprite;

                int baits = baitItem == null ? 0 : Player.m_localPlayer.GetInventory().CountItems(baitItem.m_shared.m_name);
                if (baits > 0 && baitCountEnabled.Value && !elementData.m_amount.gameObject.activeSelf)
                {
                    elementData.m_amount.gameObject.SetActive(true);
                    elementData.m_amount.SetText(baits.ToString());
                }
            }

            private ItemDrop.ItemData GetCurrentAmmo()
            {
                if (m_item == null || m_item.m_shared.m_ammoType.IsNullOrWhiteSpace() || IsBaitAmmo() || IsAmmo())
                    return null;

                ItemDrop.ItemData ammo = Player.m_localPlayer.GetAmmoItem();
                if (ammo == null || ammo.m_shared.m_ammoType != m_item.m_shared.m_ammoType || !Player.m_localPlayer.GetInventory().ContainsItem(ammo))
                    ammo = Player.m_localPlayer.GetInventory().GetAmmoItem(m_item.m_shared.m_ammoType);

                return ammo;
            }

            private ItemDrop.ItemData GetCurrentBait()
            {
                if (m_item == null || m_item.m_shared.m_ammoType.IsNullOrWhiteSpace() || !IsBaitAmmo() || IsAmmo())
                    return null;

                ItemDrop.ItemData ammo = Player.m_localPlayer.GetAmmoItem();
                if (ammo == null || ammo.m_shared.m_ammoType != m_item.m_shared.m_ammoType || !Player.m_localPlayer.GetInventory().ContainsItem(ammo))
                    ammo = Player.m_localPlayer.GetInventory().GetAmmoItem(m_item.m_shared.m_ammoType);

                return ammo;
            }

            private bool IsAmmo()
            {
                return m_item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || m_item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable || m_item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable;
            }

            private bool IsBaitAmmo()
            {
                return m_item.m_shared.m_ammoType == "fishingbait";
            }
        }

        internal const string objectAmmoName = "MLUI_AmmoAmount";
        internal const string objectIconName = "MLUI_AmmoIcon";
        internal const string objectBaitName = "MLUI_AmmoBait";

        internal static GameObject elementPrefab;

        internal static GameObject ammoCount;
        internal static GameObject ammoIcon;
        internal static GameObject ammoBait;

        internal static readonly Dictionary<HotkeyBar, bool> isDirty = new Dictionary<HotkeyBar, bool>();

        internal static readonly Dictionary<HotkeyBar, Dictionary<ElementData, ElementExtraData>> elementExtraDatas = new Dictionary<HotkeyBar, Dictionary<ElementData, ElementExtraData>>();

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

            foreach (HotkeyBar bar in isDirty.Keys.ToList())
                isDirty[bar] = true;
        }
        
        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.OnEnable))]
        public static class HotkeyBar_OnEnable_AddCustomElementsToHotkeyPrefab
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
            [HarmonyPriority(Priority.Last)]
            [HarmonyBefore("Azumatt.AzuExtendedPlayerInventory")]
            public static void Prefix(HotkeyBar __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (isDirty.GetValueSafe(__instance) == true)
                {
                    isDirty[__instance] = false;
                    if (__instance.m_elements.Count > 0)
                        __instance.UpdateIcons(null);
                }
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        public static class HotkeyBar_UpdateIcons_UpdateAmmoCountAndIcon
        {
            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter("Azumatt.AzuExtendedPlayerInventory")]
            public static void Postfix(HotkeyBar __instance, Player player)
            {
                if (!modEnabled.Value)
                    return;

                if (!elementExtraDatas.ContainsKey(__instance))
                    elementExtraDatas.Add(__instance, new Dictionary<ElementData, ElementExtraData>());

                Dictionary<ElementData, ElementExtraData> elementExtras = elementExtraDatas[__instance];

                if (!player || player.IsDead())
                {
                    elementExtras.Clear();
                    return;
                }

                if (elementExtras.Count != __instance.m_elements.Count)
                    elementExtras.Clear();

                if (elementExtras.Count > 0 && __instance.m_elements.Count > 0 && !elementExtras.ContainsKey(__instance.m_elements[0]))
                    elementExtras.Clear();

                if (elementExtras.Count == 0)
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

                        elementExtras.Add(elementData, extraData);
                    }
                }

                int itemIndex = 0;
                for (int i = 0; i < __instance.m_elements.Count; i++)
                {
                    ElementData elementData = __instance.m_elements[i];
                    ElementExtraData extraData = elementExtras[elementData];
                    if (extraData == null)
                        continue;

                    if (!elementData.m_used || itemIndex > __instance.m_items.Count)
                        extraData.m_item = null;
                    else
                    {
                        extraData.m_item = __instance.m_items[itemIndex];
                        if (extraData.m_item.GetIcon() != elementData.m_icon.sprite)
                            extraData.m_item = null;
                        itemIndex++;
                    }

                    extraData.UpdateState(elementData);
                }
            }
        }
    }
}
