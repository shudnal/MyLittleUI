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

        internal static void AddBlock(GameObject parentObject)
        {
            if (parentObject == null)
                return;

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

        public static void UpdateStats()
        {
            if (!Player.m_localPlayer)
                return;

            UpdateGradient();

            totalWeight = Mathf.FloorToInt(Player.m_localPlayer.GetInventory().GetTotalWeight());
            maxWeight = Mathf.FloorToInt(Player.m_localPlayer.GetMaxCarryWeight());

            GetCurrentSlotsAmount(out emptySlots, out maxSlots);
        }

        public static void UpdateGradient()
        {
            gradient ??= new Gradient();
            gradient.SetKeys(new GradientColorKey[4]
                                {
                                        new GradientColorKey(weightSlotsFine.Value, 0.0f),
                                        new GradientColorKey(weightSlotsHalf.Value, 0.5f),
                                        new GradientColorKey(weightSlotsALot.Value, 0.75f),
                                        new GradientColorKey(weightSlotsFull.Value, 1.0f)
                                },
                             Array.Empty<GradientAlphaKey>());
        }

        public static void UpdateConfigurableValues()
        {
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
            if (!Player.m_localPlayer)
                return;

            UpdateConfigurableValues();

            if ((bool)weight && weight.gameObject.activeInHierarchy)
            {
                int currentWeight = showWeightLeft.Value ? maxWeight - totalWeight : totalWeight;

                weightText?.SetText(maxWeight <= 0 ? currentWeight.ToFastString() : string.Format(GetFormatString(totalWeight > maxWeight), currentWeight, maxWeight));

                if (weightBar)
                {
                    weightBar.SetMaxValue(maxWeight);
                    weightBar.SetValue(totalWeight);

                    if (gradient != null && weightBar.m_maxValue != 0f)
                        weightBar.SetColor(gradient.Evaluate(Mathf.Clamp01(weightBar.m_value / weightBar.m_maxValue)));
                }
            }

            if ((bool)slots && slots.gameObject.activeInHierarchy)
            {
                slotsText?.SetText(string.Format(GetFormatString(emptySlots <= 0), (showSlotsTaken.Value ? maxSlots - emptySlots : emptySlots).ToFastString(), maxSlots.ToFastString()));

                if (slotsBar)
                {
                    slotsBar.SetMaxValue(maxSlots);
                    slotsBar.SetValue(maxSlots - emptySlots);
                    if (gradient != null && slotsBar.m_maxValue != 0f)
                        slotsBar.SetColor(gradient.Evaluate(Mathf.Clamp01(slotsBar.m_value / slotsBar.m_maxValue)));
                }
            }

            static string GetFormatString(bool condition) => condition && Mathf.Sin(Time.time * 10f) > 0f ? "<color=red>{0}</color>/{1}" : "{0}/{1}";
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
            emptySlots = slotsAmount - Player.m_localPlayer.GetInventory().m_inventory.Where(item => item.m_gridPos.x < width && item.m_gridPos.y < height).Count();

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
                if (!modEnabled.Value)
                    return;

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
                if (!modEnabled.Value)
                    return;

                UpdateVisuals();
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), typeof(StatusEffect), typeof(bool), typeof(int), typeof(float))]
        public static class SEManAddStatusEffect_UpdateStats
        {
            public static void Postfix(StatusEffect __result)
            {
                if (!modEnabled.Value)
                    return;

                if (__result is SE_Stats se && se.m_addMaxCarryWeight > 0)
                    UpdateVisuals();
            }
        }
    }
}
