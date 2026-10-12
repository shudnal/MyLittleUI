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
    internal static class AmmoCountIcon
    {
        internal const string objectAmmoName = "MLUI_AmmoAmount";
        internal const string objectIconName = "MLUI_AmmoIcon";
        internal const string objectBaitName = "MLUI_AmmoBait";
        internal const string objectMeadName = "MLUI_AmmoMead";

        private static readonly Dictionary<HotkeyBar, AmmoCountBarState> bars = new Dictionary<HotkeyBar, AmmoCountBarState>();

        internal sealed class ElementExtraData
        {
            internal readonly GameObject Cell;
            internal ElementData Element;
            internal bool Seen;
            internal TMP_Text m_ammo;
            internal Image m_icon;
            internal Image m_bait;
            internal Image m_mead;
            internal ItemDrop.ItemData m_item;

            private int styleRevision = -1;
            private TMP_Text baitAmount;
            private string previousAmountText;
            private string baitAmountText;

            internal ElementExtraData(ElementData element)
            {
                Cell = element.m_go;
                Element = element;
                // A mod can clone an already rendered bar. Adopt only our named
                // decorations, once per cell, instead of adding duplicate nodes.
                m_ammo = Cell.transform.Find(objectAmmoName)?.GetComponent<TMP_Text>();
                m_icon = Cell.transform.Find(objectIconName)?.GetComponent<Image>();
                m_bait = Cell.transform.Find(objectBaitName)?.GetComponent<Image>();
                m_mead = Cell.transform.Find(objectMeadName)?.GetComponent<Image>();
            }

            internal void Bind(ElementData element)
            {
                if (ReferenceEquals(Element, element))
                    return;

                RestoreBaitCount();
                Element = element;
                m_item = null;
                styleRevision = -1;
            }

            internal void ApplyStyle(AmmoCountTemplates templates)
            {
                if (!templates.Ready || styleRevision == templates.Revision)
                    return;

                templates.ApplyAmount(m_ammo);
                templates.ApplyIcon(m_icon, 0);
                templates.ApplyIcon(m_bait, 1);
                templates.ApplyIcon(m_mead, 2);
                styleRevision = templates.Revision;
                PlaceDecorations();
            }

            private void PlaceDecorations()
            {
                if (!Cell || !Element.m_icon)
                    return;

                int index = Element.m_icon.transform.GetSiblingIndex() + 1;
                if (m_ammo)
                    m_ammo.transform.SetSiblingIndex(index++);
                if (m_icon)
                    m_icon.transform.SetSiblingIndex(index++);
                if (m_bait)
                    m_bait.transform.SetSiblingIndex(index++);
                if (m_mead)
                    m_mead.transform.SetSiblingIndex(index);
            }

            private T CreateDecoration<T>(T template) where T : Graphic
            {
                // The template is inactive. Set data before enabling the copy.
                T copy = Object.Instantiate(template, Cell.transform, false);
                copy.name = template.name;
                return copy;
            }

            internal void UpdateState(AmmoCountTemplates templates, Player player)
            {
                if (!modEnabled.Value || !Cell || !Cell.activeInHierarchy
                    || !player || player.IsDead() || m_item?.m_shared == null)
                {
                    Hide();
                    return;
                }

                ApplyStyle(templates);
                Inventory inventory = player.GetInventory();
                ItemDrop.ItemData ammoItem = GetCurrentAmmo(player);
                int amount = ammoItem == null ? 0 : inventory.CountItems(ammoItem.m_shared.m_name);
                bool showAmount = templates.Ready && ammoCountEnabled.Value && amount > 0;
                if (showAmount)
                {
                    if (!m_ammo)
                    {
                        m_ammo = CreateDecoration(templates.Amount);
                        PlaceDecorations();
                    }
                    m_ammo.SetText(amount.ToString());
                }
                SetVisible(m_ammo, showAmount);

                Sprite sprite = ammoItem?.GetIcon();
                UpdateIcon(ref m_icon, templates.Icon, sprite,
                    templates.Ready && ammoIconEnabled.Value && sprite);

                ItemDrop.ItemData baitItem = GetCurrentBait(player);
                Sprite baitSprite = baitItem?.GetIcon();
                UpdateIcon(ref m_bait, templates.Bait, baitSprite,
                    templates.Ready && baitIconEnabled.Value && baitSprite);

                int baits = baitItem == null ? 0 : inventory.CountItems(baitItem.m_shared.m_name);
                if (baits > 0 && baitCountEnabled.Value && Element.m_amount
                    && !Element.m_amount.gameObject.activeSelf)
                {
                    // Borrow the hidden vanilla amount label, never a visible stack
                    // count. Restore it before the next vanilla or modded update.
                    baitAmount = Element.m_amount;
                    previousAmountText = baitAmount.text;
                    baitAmountText = baits.ToString();
                    baitAmount.SetText(baitAmountText);
                    baitAmount.gameObject.SetActive(true);
                }

                Sprite meadSprite = GetCurrentMead(player)?.GetIcon();
                UpdateIcon(ref m_mead, templates.Mead, meadSprite,
                    templates.Ready && meadIconEnabled.Value && meadSprite);
            }

            private void UpdateIcon(ref Image image, Image template, Sprite sprite, bool visible)
            {
                if (visible)
                {
                    if (!image)
                    {
                        image = CreateDecoration(template);
                        PlaceDecorations();
                    }
                    image.overrideSprite = sprite;
                }
                SetVisible(image, visible);
            }

            private static void SetVisible(Graphic graphic, bool visible)
            {
                if (graphic && graphic.gameObject.activeSelf != visible)
                    graphic.gameObject.SetActive(visible);
            }

            internal void RestoreBaitCount()
            {
                if (baitAmount && baitAmount.text == baitAmountText)
                {
                    baitAmount.SetText(previousAmountText);
                    baitAmount.gameObject.SetActive(false);
                    if (Element != null && Element.m_amount == baitAmount)
                        Element.m_stackText = -1;
                }
                baitAmount = null;
                previousAmountText = null;
                baitAmountText = null;
            }

            internal void Hide()
            {
                RestoreBaitCount();
                SetVisible(m_ammo, false);
                SetVisible(m_icon, false);
                SetVisible(m_bait, false);
                SetVisible(m_mead, false);
                m_item = null;
            }

            internal void Dispose()
            {
                Hide();
                if (m_ammo)
                    Object.Destroy(m_ammo.gameObject);
                if (m_icon)
                    Object.Destroy(m_icon.gameObject);
                if (m_bait)
                    Object.Destroy(m_bait.gameObject);
                if (m_mead)
                    Object.Destroy(m_mead.gameObject);
                m_ammo = null;
                m_icon = null;
                m_bait = null;
                m_mead = null;
                Element = null;
            }

            private ItemDrop.ItemData GetCurrentAmmo(Player player)
            {
                if (m_item == null || m_item.m_shared.m_ammoType.IsNullOrWhiteSpace() || IsBaitAmmo() || IsAmmo() || IsMeadAmmo())
                    return null;

                ItemDrop.ItemData ammo = player.GetAmmoItem();
                if (ammo == null || ammo.m_shared.m_ammoType != m_item.m_shared.m_ammoType || !player.GetInventory().ContainsItem(ammo))
                    ammo = player.GetInventory().GetAmmoItem(m_item.m_shared.m_ammoType);

                return ammo;
            }

            private ItemDrop.ItemData GetCurrentBait(Player player)
            {
                if (m_item == null || m_item.m_shared.m_ammoType.IsNullOrWhiteSpace() || !IsBaitAmmo() || IsAmmo())
                    return null;

                ItemDrop.ItemData ammo = player.GetAmmoItem();
                if (ammo == null || ammo.m_shared.m_ammoType != m_item.m_shared.m_ammoType || !player.GetInventory().ContainsItem(ammo))
                    ammo = player.GetInventory().GetAmmoItem(m_item.m_shared.m_ammoType);

                return ammo;
            }

            private ItemDrop.ItemData GetCurrentMead(Player player)
            {
                if (m_item == null || m_item.m_shared.m_ammoType.IsNullOrWhiteSpace() || IsBaitAmmo() || IsAmmo() || !IsMeadAmmo())
                    return null;

                return player.GetInventory().GetAmmoItem(m_item.m_shared.m_ammoType);
            }

            private bool IsAmmo()
            {
                return m_item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || m_item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable || m_item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable;
            }

            private bool IsBaitAmmo() => m_item.m_shared.m_ammoType == "$item_fishingbait";

            private bool IsMeadAmmo() => m_item.m_shared.m_ammoType == "mead";
        }

        private static AmmoCountBarState GetState(HotkeyBar bar)
        {
            if (!bars.TryGetValue(bar, out AmmoCountBarState state) || !state)
            {
                state = bar.GetComponent<AmmoCountBarState>();
                if (!state)
                    state = bar.gameObject.AddComponent<AmmoCountBarState>();
                state.Initialize(bar);
                bars[bar] = state;
            }
            return state;
        }

        internal static void Unregister(HotkeyBar bar, AmmoCountBarState state)
        {
            if (!ReferenceEquals(bar, null) && bars.TryGetValue(bar, out AmmoCountBarState current)
                && ReferenceEquals(current, state))
                bars.Remove(bar);
        }

        public static void UpdateVisibility()
        {
            foreach (AmmoCountBarState state in bars.Values)
                if (state)
                    state.UpdateSettings();
        }

        internal static void Shutdown()
        {
            foreach (AmmoCountBarState state in new List<AmmoCountBarState>(bars.Values))
            {
                if (!state)
                    continue;
                state.Dispose();
                Object.Destroy(state);
            }
            bars.Clear();
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.OnEnable))]
        private static class HotkeyBar_OnEnable_PrepareAmmoTemplates
        {
            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter("Azumatt.AzuExtendedPlayerInventory", "shudnal.ExtraSlots")]
            private static void Postfix(HotkeyBar __instance)
                => GetState(__instance).Refresh(Player.m_localPlayer, refreshDonors: true);
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.OnDisable))]
        private static class HotkeyBar_OnDisable_HideAmmoDecorations
        {
            private static void Postfix(HotkeyBar __instance)
            {
                if (bars.TryGetValue(__instance, out AmmoCountBarState state) && state)
                    state.Hide();
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        private static class HotkeyBar_UpdateIcons_UpdateAmmoCountAndIcon
        {
            [HarmonyPriority(Priority.First)]
            [HarmonyBefore("Azumatt.AzuExtendedPlayerInventory", "shudnal.ExtraSlots")]
            private static void Prefix(HotkeyBar __instance)
            {
                if (bars.TryGetValue(__instance, out AmmoCountBarState state) && state)
                    state.RestoreBaitCounts();
            }

            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter("Azumatt.AzuExtendedPlayerInventory", "shudnal.ExtraSlots")]
            private static void Postfix(HotkeyBar __instance, Player player)
                => GetState(__instance).Refresh(player);
        }
    }

    // HotkeyBar has no OnDestroy method. This component supplies the lifetime
    // callback without patching a nonexistent game method or adding an Update loop.
    [DisallowMultipleComponent]
    internal sealed class AmmoCountBarState : MonoBehaviour
    {
        private HotkeyBar bar;
        private readonly AmmoCountTemplates templates = new AmmoCountTemplates();
        private readonly Dictionary<GameObject, AmmoCountIcon.ElementExtraData> cells = new Dictionary<GameObject, AmmoCountIcon.ElementExtraData>();
        private readonly List<GameObject> removedCells = new List<GameObject>();

        internal void Initialize(HotkeyBar owner) => bar = owner;

        internal void Refresh(Player player, bool refreshDonors = false)
        {
            if (!bar || !gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            templates.Refresh(bar.m_elementPrefab, refreshDonors);
            RestoreBaitCounts();
            foreach (AmmoCountIcon.ElementExtraData data in cells.Values)
                data.Seen = false;

            bool showItems = player && !player.IsDead();
            Inventory inventory = showItems ? player.GetInventory() : null;
            int itemIndex = 0;
            for (int i = 0; i < bar.m_elements.Count; i++)
            {
                ElementData element = bar.m_elements[i];
                if (element == null)
                    continue;

                // Both lists describe the rendered bar. Walk its used elements,
                // not inventory row zero or a presumed number of visible slots.
                ItemDrop.ItemData item = element.m_used && itemIndex < bar.m_items.Count
                    ? bar.m_items[itemIndex++] : null;
                if (!element.m_go)
                    continue;

                if (!cells.TryGetValue(element.m_go, out AmmoCountIcon.ElementExtraData data))
                {
                    data = new AmmoCountIcon.ElementExtraData(element);
                    cells.Add(element.m_go, data);
                }
                data.Bind(element);
                data.Seen = true;
                data.m_item = showItems && item != null && inventory.ContainsItem(item) ? item : null;
                data.UpdateState(templates, player);
            }

            // Reconcile every cell identity, including replacements with the same
            // element count and a reused ElementData wrapper with a new GameObject.
            removedCells.Clear();
            foreach (KeyValuePair<GameObject, AmmoCountIcon.ElementExtraData> entry in cells)
            {
                if (entry.Key && entry.Value.Seen)
                    continue;
                entry.Value.Dispose();
                removedCells.Add(entry.Key);
            }
            foreach (GameObject cell in removedCells)
                cells.Remove(cell);
            removedCells.Clear();
        }

        internal void RestoreBaitCounts()
        {
            foreach (AmmoCountIcon.ElementExtraData data in cells.Values)
                data.RestoreBaitCount();
        }

        internal void UpdateSettings()
        {
            templates.ApplySettings();
            foreach (AmmoCountIcon.ElementExtraData data in cells.Values)
                data.ApplyStyle(templates);
            Refresh(Player.m_localPlayer);
        }

        internal void Hide()
        {
            foreach (AmmoCountIcon.ElementExtraData data in cells.Values)
                data.Hide();
        }

        internal void Dispose()
        {
            foreach (AmmoCountIcon.ElementExtraData data in cells.Values)
                data.Dispose();
            cells.Clear();
            removedCells.Clear();
            templates.Dispose();
            AmmoCountIcon.Unregister(bar, this);
            bar = null;
        }

        private void OnDisable() => Hide();

        private void OnDestroy() => Dispose();
    }
}
