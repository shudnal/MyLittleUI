using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    public static class InventoryPanel
    {
        private const string objectWeightName = "Weight";
        private const string objectSlotsName = "Slots";

        public static RectTransform weight;
        public static RectTransform slots;

        public static TMP_Text weightText;
        public static TMP_Text slotsText;

        public static GuiBar weightBar;
        public static GuiBar slotsBar;

        private static Image weightBackground;
        private static Image slotsBackground;

        private static int totalWeight;
        private static int maxWeight;

        private static int emptySlots;
        private static int maxSlots;

        private static Gradient gradient;

        private static ConfigFile subscribedConfig;
        private static bool layoutDirty = true;
        private static bool statsDirty = true;
        private static int gradientRevision;
        private static readonly GradientColorKey[] gradientKeys = new GradientColorKey[4];
        private static readonly PanelRenderState weightState = new PanelRenderState();
        private static readonly PanelRenderState slotsState = new PanelRenderState();

        private sealed class PanelRenderState
        {
            internal bool Valid;
            internal int Current;
            internal int Maximum;
            internal int Value;
            internal bool Blink;
            internal int GradientRevision = -1;
        }

        private static void OnPanelSettingChanged(object sender, SettingChangedEventArgs args)
        {
            string section = args.ChangedSetting.Definition.Section;
            if (!section.StartsWith("Info - Inventory", StringComparison.Ordinal) && section != "General")
                return;
            layoutDirty = true;
            statsDirty = true;
        }

        private static void SubscribeSettings()
        {
            ConfigFile config = instance.Config;
            if (ReferenceEquals(subscribedConfig, config))
                return;
            if (subscribedConfig != null)
                subscribedConfig.SettingChanged -= OnPanelSettingChanged;
            subscribedConfig = config;
            subscribedConfig.SettingChanged += OnPanelSettingChanged;
        }

        internal static void AddBlock(GameObject parentObject)
        {
            if (parentObject == null)
                return;

            SubscribeSettings();
            layoutDirty = statsDirty = true;
            weightState.Valid = slotsState.Valid = false;

            weight = new GameObject(objectWeightName, typeof(RectTransform))
            {
                layer = layerUI
            }.GetComponent<RectTransform>();

            weight.SetParent(parentObject.transform, false);
            weight.SetAnchor(ElementAnchor.BottomLeft);
            weight.sizeDelta = Vector2.one * 64f;
            weight.anchoredPosition = new Vector2(62f, 332f);

            RectTransform weightIcon = UnityEngine.Object.Instantiate(InventoryGui.instance.m_weight?.transform.parent.Find("weight_icon")?.transform as RectTransform, weight);
            weightIcon.name = "Icon";
            weightIcon.anchorMin = Vector2.one * 0.5f;
            weightIcon.anchorMax = Vector2.one * 0.5f;
            weightIcon.sizeDelta = Vector2.one * 32f;
            weightIcon.anchoredPosition = new Vector2(0f, 12f);

            RectTransform weightTextRT = UnityEngine.Object.Instantiate(InventoryGui.instance.m_weight?.transform as RectTransform, weight);
            weightTextRT.name = "Text";
            weightTextRT.anchorMin = Vector2.one * 0.5f;
            weightTextRT.anchorMax = Vector2.one * 0.5f;
            weightTextRT.sizeDelta = new Vector2(64f, 22f);
            weightTextRT.anchoredPosition = new Vector2(0f, -11f);

            weightText = weightTextRT.GetComponent<TMP_Text>();

            RectTransform weightBarRT = UnityEngine.Object.Instantiate(InventoryGui.instance.m_playerGrid.m_elementPrefab?.transform.Find("durability"), weight)?.GetComponent<RectTransform>();
            weightBarRT.name = "Bar";
            weightBarRT.anchorMin = Vector2.one * 0.5f;
            weightBarRT.anchorMax = Vector2.one * 0.5f;
            weightBarRT.sizeDelta = new Vector2(54f, 5f);
            weightBarRT.anchoredPosition = new Vector2(0f, -25f);

            weightBar = weightBarRT.GetComponent<GuiBar>();

            Image bkg = InventoryGui.instance.m_playerGrid.m_elementPrefab.GetComponent<Image>();

            weightBackground = weight.gameObject.AddComponent<Image>();
            weightBackground.sprite = bkg.sprite;
            weightBackground.color = new Color(0f, 0f, 0f, 0.5f);

            slots = UnityEngine.Object.Instantiate(weight, parentObject.transform);
            slots.name = objectSlotsName;
            slots.anchoredPosition = new Vector2(62f, 265f);

            RectTransform slotsIcon = slots.Find("Icon") as RectTransform;
            slotsIcon.GetComponent<Image>().sprite = Minimap.instance.m_locationIcons.Select(loc => loc.m_icon).FirstOrDefault(icon => icon.name == "mapicon_trader");
            slotsIcon.anchoredPosition = new Vector2(0f, 12.5f);
            slotsIcon.sizeDelta = Vector2.one * 38f;

            slotsText = slots.Find("Text").GetComponent<TMP_Text>();

            slotsBar = slots.Find("Bar").GetComponent<GuiBar>();

            slotsBackground = slots.GetComponent<Image>();
        }

        // Inventory/equipment notifications can arrive in bursts. Resolve the final state
        // once before rendering instead of rebuilding it for every intermediate notification.
        public static void UpdateStats() => statsDirty = true;

        private static void RefreshStats()
        {
            if (!Player.m_localPlayer)
                return;

            statsDirty = false;
            UpdateGradient();
            totalWeight = Mathf.FloorToInt(Player.m_localPlayer.GetInventory().GetTotalWeight());
            GetCurrentSlotsAmount(out emptySlots, out maxSlots);
        }

        public static void UpdateGradient()
        {
            Color fine = weightSlotsFine.Value;
            Color half = weightSlotsHalf.Value;
            Color lot = weightSlotsALot.Value;
            Color full = weightSlotsFull.Value;
            if (gradient != null && gradientKeys[0].color == fine && gradientKeys[1].color == half
                && gradientKeys[2].color == lot && gradientKeys[3].color == full)
                return;

            gradient ??= new Gradient();
            gradientKeys[0] = new GradientColorKey(fine, 0f);
            gradientKeys[1] = new GradientColorKey(half, 0.5f);
            gradientKeys[2] = new GradientColorKey(lot, 0.75f);
            gradientKeys[3] = new GradientColorKey(full, 1f);
            gradient.SetKeys(gradientKeys, Array.Empty<GradientAlphaKey>());
            gradientRevision++;
        }

        public static void UpdateConfigurableValues()
        {
            layoutDirty = false;
            weightState.Valid = slotsState.Valid = false;
            if (weightBackground)
                weightBackground.color = weightBackgroundColor.Value;

            if (slotsBackground)
                slotsBackground.color = slotsBackgroundColor.Value;

            if (weight)
            {
                weight.gameObject.SetActive(modEnabled.Value && showWeight.Value);
                if (weight.gameObject.activeInHierarchy)
                {
                    weight.SetAnchor(weightPositionAnchor.Value);
                    weight.anchoredPosition = weightPosition.Value;
                    if (weightText)
                        weightText.color = weightFontColor.Value;
                }
            }

            if (slots)
            {
                slots.gameObject.SetActive(modEnabled.Value && showSlots.Value);
                if (slots.gameObject.activeInHierarchy)
                {
                    slots.SetAnchor(slotsPositionAnchor.Value);
                    slots.anchoredPosition = slotsPosition.Value;
                    if (slotsText)
                        slotsText.color = slotsFontColor.Value;
                }
            }
        }

        public static void UpdateVisuals()
        {
            Player player = Player.m_localPlayer;
            if (!player)
                return;

            if (layoutDirty)
                UpdateConfigurableValues();
            bool weightVisible = weight && weight.gameObject.activeInHierarchy;
            bool slotsVisible = slots && slots.gameObject.activeInHierarchy;
            if (!weightVisible && !slotsVisible)
                return;

            if (statsDirty)
                RefreshStats();

            if (weightVisible)
            {
                // Carry capacity can change without Inventory.Changed (status effects and
                // other mods), so keep this effective getter live rather than caching it.
                maxWeight = Mathf.FloorToInt(player.GetMaxCarryWeight());
                int current = showWeightLeft.Value ? maxWeight - totalWeight : totalWeight;
                RenderPanel(weightState, weightText, weightBar, current, maxWeight, totalWeight, totalWeight > maxWeight, true);
            }
            if (slotsVisible)
            {
                int current = showSlotsTaken.Value ? maxSlots - emptySlots : emptySlots;
                RenderPanel(slotsState, slotsText, slotsBar, current, maxSlots, maxSlots - emptySlots, emptySlots <= 0, false);
            }
        }

        private static void RenderPanel(PanelRenderState state, TMP_Text text, GuiBar bar, int current, int maximum, int value, bool warning, bool hideNonpositiveMaximum)
        {
            bool blink = warning && Mathf.Sin(Time.time * 10f) > 0f;
            bool valueChanged = !state.Valid || state.Maximum != maximum || state.Value != value;
            if (text && (!state.Valid || state.Current != current || state.Maximum != maximum || state.Blink != blink))
            {
                string formatted = hideNonpositiveMaximum && maximum <= 0 ? current.ToFastString()
                    : blink ? $"<color=red>{current}</color>/{maximum}" : $"{current}/{maximum}";
                text.SetText(formatted);
            }
            if (bar)
            {
                if (valueChanged)
                {
                    bar.SetMaxValue(maximum);
                    bar.SetValue(value);
                }
                if ((valueChanged || state.GradientRevision != gradientRevision) && gradient != null && bar.m_maxValue != 0f)
                    bar.SetColor(gradient.Evaluate(Mathf.Clamp01(bar.m_value / bar.m_maxValue)));
            }

            state.Valid = true;
            state.Current = current;
            state.Maximum = maximum;
            state.Value = value;
            state.Blink = blink;
            state.GradientRevision = gradientRevision;
        }

        public static void GetCurrentSlotsAmount(out int emptySlots, out int slotsAmount)
        {
            int width = Player.m_localPlayer.GetInventory().GetWidth();
            int height = Player.m_localPlayer.GetInventory().GetHeight();
            if (AzuExtendedPlayerInventory.API.IsLoaded())
                height -= AzuExtendedPlayerInventory.API.GetAddedRows(width);
            else if (ExtraSlotsAPI.API.IsReady())
                height = ExtraSlotsAPI.API.GetInventoryHeightPlayer();

            slotsAmount = width * height;
            emptySlots = slotsAmount;
            List<ItemDrop.ItemData> items = Player.m_localPlayer.GetInventory().m_inventory;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                if (item != null && item.m_gridPos.x >= 0 && item.m_gridPos.y >= 0 && item.m_gridPos.x < width && item.m_gridPos.y < height)
                    emptySlots--;
            }

            if (AzuExtendedPlayerInventory.API.IsLoaded())
            {
                int quickslots = AzuExtendedPlayerInventory.API.GetQuickSlots().SlotNames.Length;
                emptySlots += quickslots - AzuExtendedPlayerInventory.API.GetQuickSlotsItems().Count;
                slotsAmount += quickslots;
            }
            else if (ExtraSlotsAPI.API.IsReady())
            {
                int quickslots = ExtraSlotsAPI.API.GetQuickSlots().Count(slot => slot.IsActive);
                emptySlots += quickslots - ExtraSlotsAPI.API.GetQuickSlotsItems().Count;
                slotsAmount += quickslots;
            }
            else if (Chainloader.PluginInfos.TryGetValue("randyknapp.mods.equipmentandquickslots", out PluginInfo eaqs) && eaqs.Instance.Config.TryGetEntry("Toggles", "Enable Quick Slots", out ConfigEntry<bool> entry) && entry.Value)
            {
                slotsAmount += 3;
                emptySlots = Player.m_localPlayer.GetInventory().GetEmptySlots();
            }
        }
        
        [HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
        public static class Hud_OnDestroy_Clear
        {
            public static void Postfix()
            {
                if (subscribedConfig != null)
                    subscribedConfig.SettingChanged -= OnPanelSettingChanged;
                subscribedConfig = null;
                layoutDirty = statsDirty = true;
                weightState.Valid = slotsState.Valid = false;
                totalWeight = maxWeight = emptySlots = maxSlots = 0;
                weight = null;
                slots = null;

                weightText = null;
                slotsText = null;

                weightBar = null;
                slotsBar = null;

                weightBackground = null;
                slotsBackground = null;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnInventoryChanged))]
        public static class Player_OnInventoryChanged_UpdateStats
        {
            public static void Postfix(Player __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (__instance != Player.m_localPlayer || __instance.m_isLoading)
                    return;

                UpdateStats();
            }
        }

        [HarmonyPatch]
        public static class Humanoid_UpdateStats
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem));
                yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UnequipItem));
            }

            private static void Postfix(Humanoid __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (__instance != Player.m_localPlayer || (__instance as Player).m_isLoading)
                    return;

                UpdateStats();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public static class Player_OnSpawned_UpdateStats
        {
            public static void Postfix(Player __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (__instance != Player.m_localPlayer)
                    return;

                UpdateStats();
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
        public static class Hud_Update_UpdateVisuals
        {
            public static void Postfix()
            {
                UpdateVisuals();
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), typeof(StatusEffect), typeof(bool), typeof(int), typeof(float), typeof(short))]
        public static class SEManAddStatusEffect_UpdateStats
        {
            public static void Postfix(SEMan __instance, StatusEffect __result)
            {
                if (!modEnabled.Value || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetSEMan())
                    return;

                if (__result is SE_Stats se && se.m_addMaxCarryWeight != 0f)
                    UpdateStats();
            }
        }
    }
}
