﻿using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using static MyLittleUI.MyLittleUI;
using UnityEngine.UI;
using System.Diagnostics;

namespace MyLittleUI
{
    public static class MultiCraft
    {
        public const string panelName = "MLUI_Multicraft";
        public const string buttonIncreaseName = "Increase";
        public const string buttonDecreaseName = "Decrease";
        public const string textAmountName = "Amount";

        private static readonly Recipe tempRecipe = ScriptableObject.CreateInstance<Recipe>();
        private static readonly StringBuilder sb = new StringBuilder(10);
        private static readonly Dictionary<Recipe, Tuple<string, int>> cachedAmount = new Dictionary<Recipe, Tuple<string, int>>();
        private static readonly List<TMP_Text> resAmountElements = new List<TMP_Text>();

        private static RectTransform panel;
        private static RectTransform craftButton;
        private static Button buttonIncrease;
        private static Button buttonDecrease;
        private static TMP_Text textAmount;
        private static TMP_Text textCrafting;

        private static int amount = 1;
        private static bool showPanel;

        public static int lastScrollTriggerFrame;
        public const int minScrollDeltaFrames = 2;

        private static bool IsMulticraftEnabled()
        {
            return modEnabled.Value && showMulticraftButtons.Value;
        }

        private static int GetMaximumAmount(Recipe recipe, Player player)
        {
            if (player.NoCostCheat())
                return 99;

            CraftingStation currentCraftingStation = player.GetCurrentCraftingStation();
            bool haveStation = !recipe.GetRequiredStation(1) || ((bool)currentCraftingStation && currentCraftingStation.CheckUsable(player, showMessage: false));

            if (!haveStation)
                return 0;

            if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
                return 99;

            if (!recipe.m_requireOnlyOneIngredient)
                return GetMaxCraftAmount(player, recipe);

            // Vanilla logic only counts max amount on single resource usage
            // To get proper max amount calculate maximum amount of every resource available
            // Iterate through every resource one by one and get max amount
            tempRecipe.m_item = recipe.m_item;
            tempRecipe.m_amount = recipe.m_amount;
            tempRecipe.m_enabled = recipe.m_enabled;
            tempRecipe.m_qualityResultAmountMultiplier = recipe.m_qualityResultAmountMultiplier;
            tempRecipe.m_craftingStation = recipe.m_craftingStation;
            tempRecipe.m_repairStation = recipe.m_repairStation;
            tempRecipe.m_minStationLevel = recipe.m_minStationLevel;
            tempRecipe.m_listSortWeight = recipe.m_listSortWeight;
            tempRecipe.m_requireOnlyOneIngredient = false;
            tempRecipe.m_resources = new Piece.Requirement[1];

            int result = 0;
            for (int i = 0; i < recipe.m_resources.Length; i++)
            {
                Piece.Requirement requirement = recipe.m_resources[i];
                if (!player.IsKnownMaterial(requirement.m_resItem.m_itemData.m_shared.m_name) || requirement.m_amount < 1)
                    continue;

                tempRecipe.m_resources[0] = JsonUtility.FromJson<Piece.Requirement>(JsonUtility.ToJson(requirement));

                result += GetMaxCraftAmount(player, tempRecipe);

                if (result >= 99)
                {
                    result = 99;
                    break;
                }
            }

            return result;
        }

        private static int GetMaxCraftAmount(Player player, Recipe recipe)
        {
            if (!HaveRequirements(1))
                return 0;

            int left = 1, right = 99;
            while (left < right)
            {
                int mid = (left + right + 1) / 2;
                if (HaveRequirements(mid))
                    left = mid;
                else
                    right = mid - 1;
            }

            return left;

            bool HaveRequirements(int amount) => player.HaveRequirements(recipe, discover: false, qualityLevel: 1, amount: amount);
        }

        private static void CreateMulticraftPanel()
        {
            if (!craftButton)
                return;

            textCrafting = InventoryGui.instance.m_craftProgressPanel?.Find("Text")?.GetComponent<TMP_Text>();

            panel = new GameObject(panelName, typeof(RectTransform)).GetComponent<RectTransform>();

            panel.SetParent(craftButton.parent, false);
            panel.anchorMin = new Vector2(0.75f, 0);
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = new Vector2(0f, -5f);

            panel.gameObject.AddComponent<AmountScrollHandler>();

            GameObject increase = UnityEngine.Object.Instantiate(craftButton.gameObject, panel);
            increase.name = buttonIncreaseName;

            buttonIncrease = increase.GetComponent<Button>();
            buttonIncrease.onClick.RemoveAllListeners();
            buttonIncrease.onClick.AddListener(OnIncreaseButtonPressed);
            buttonIncrease.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = InventoryGui.instance.m_tabCraft.GetComponent<ButtonSfx>().m_sfxPrefab;

            UIGamePad increaseGamePad = buttonIncrease.GetComponent<UIGamePad>();
            increaseGamePad.m_zinputKey = "JoyRStickUp";
            increaseGamePad.m_keyCode = KeyCode.UpArrow;

            UnityEngine.Object.Destroy(increaseGamePad.m_hint);
            increaseGamePad.m_hint = null;

            RectTransform rtIncrease = increase.GetComponent<RectTransform>();
            rtIncrease.anchorMin = new Vector2(0.5f, 0.5f);
            rtIncrease.anchorMax = Vector2.one;
            rtIncrease.offsetMin = Vector2.zero;
            rtIncrease.offsetMax = Vector2.zero;

            TMP_Text textIncrease = rtIncrease.Find("Text").GetComponent<TMP_Text>();
            textIncrease.SetText("+");
            RectTransform rtTextIncrease = textIncrease.GetComponent<RectTransform>();
            rtTextIncrease.offsetMin = new Vector2(0, 2f);
            rtTextIncrease.offsetMax = Vector2.zero;

            GameObject decrease = UnityEngine.Object.Instantiate(craftButton.gameObject, panel);
            decrease.name = buttonDecreaseName;

            buttonDecrease = decrease.GetComponent<Button>();
            buttonDecrease.onClick.RemoveAllListeners();
            buttonDecrease.onClick.AddListener(OnDecreaseButtonPressed);
            buttonDecrease.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = InventoryGui.instance.m_tabCraft.GetComponent<ButtonSfx>().m_sfxPrefab;

            UIGamePad decreaseGamePad = buttonDecrease.GetComponent<UIGamePad>();
            decreaseGamePad.m_zinputKey = "JoyRStickDown";
            decreaseGamePad.m_keyCode = KeyCode.DownArrow;

            UnityEngine.Object.Destroy(decreaseGamePad.m_hint);
            decreaseGamePad.m_hint = null;

            RectTransform rtDecrease = decrease.GetComponent<RectTransform>();
            rtDecrease.anchorMin = new Vector2(0.5f, 0f);
            rtDecrease.anchorMax = new Vector2(1f, 0.5f);
            rtDecrease.offsetMin = Vector2.zero;
            rtDecrease.offsetMax = Vector2.zero;

            TMP_Text textDecrease = rtDecrease.Find("Text").GetComponent<TMP_Text>();
            textDecrease.SetText("-");
            RectTransform rtTextDecrease = textDecrease.GetComponent<RectTransform>();
            rtTextDecrease.offsetMin = new Vector2(0, 2f);
            rtTextDecrease.offsetMax = Vector2.zero;

            GameObject amount = UnityEngine.Object.Instantiate(craftButton.transform.Find("Text").gameObject, panel);
            amount.name = textAmountName;
            RectTransform rtAmount = amount.GetComponent<RectTransform>();
            rtAmount.anchorMin = Vector2.zero;
            rtAmount.anchorMax = new Vector2(0.5f, 1f);
            rtAmount.offsetMin = Vector2.zero;
            rtAmount.offsetMax = Vector2.zero;
            rtAmount.sizeDelta = new Vector2(-4f, 0f);

            textAmount = amount.GetComponent<TMP_Text>();
            textAmount.SetText("99");
            textAmount.fontSizeMax = 32f;

            LogInfo("Multicraft panel initialized");
        }

        internal static void UpdateMulticraftPanel()
        {
            if (!InventoryGui.instance)
                return;

            showPanel = IsMulticraftEnabled() && InventoryGui.instance.m_selectedRecipe.Recipe != null 
                                              && InventoryGui.instance.m_selectedRecipe.ItemData == null 
                                              && (InventoryGui.instance.m_selectedRecipe.CanCraft || Player.m_localPlayer.NoCostCheat());

            panel?.gameObject.SetActive(showPanel && InventoryGui.instance.m_craftButton.isActiveAndEnabled);

            if (craftButton)
                craftButton.anchorMax = showPanel ? new Vector2(0.75f, 1f) : Vector2.one;
        }

        private static void OnIncreaseButtonPressed()
        {
            ChangeAmount(1);
        }

        private static void OnDecreaseButtonPressed()
        {
            ChangeAmount(-1);
        }

        private static void ChangeAmount(int direction)
        {
            int delta = 1;
            if (UnityInput.Current.GetKey(KeyCode.LeftControl))
                delta = 99;
            else if (UnityInput.Current.GetKey(KeyCode.LeftShift))
                delta = 10;

            amount += direction * delta;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        public static class InventoryGui_UpdateRecipe_MulticraftShowButtons
        {
            private static bool isCrafting;

            private static bool IsCrafting(InventoryGui __instance) => __instance.m_craftTimer != -1f;

            private static bool queueNextCraft;

            public static void Prefix(InventoryGui __instance)
            {
                isCrafting = IsCrafting(__instance);
            }

            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter("Azumatt.AzuCraftyBoxes", "aedenthorn.CraftFromContainers", "org.bepinex.plugins.valheim_plus")]
            public static void Postfix(InventoryGui __instance)
            {
                UpdateMulticraftPanel();

                textCrafting.SetText(Localization.instance.Localize("$inventory_craftingprog"));

                if (!showPanel)
                    return;

                if (buttonIncrease)
                    buttonIncrease.interactable = __instance.m_craftButton.interactable;

                if (buttonDecrease)
                    buttonDecrease.interactable = __instance.m_craftButton.interactable;

                textAmount?.SetText("0");

                if (!__instance.m_craftButton.interactable)
                    return;

                if (isCrafting && !IsCrafting(__instance))
                {
                    amount--;
                    queueNextCraft = amount > 0;
                }

                int maxAmount = GetMaxAmountCached();

                if (AmountScrollHandler.hovered && ZInput.GetMouseScrollWheel() != 0 && Time.frameCount - lastScrollTriggerFrame > 2)
                {
                    if (ZInput.GetMouseScrollWheel() > 0)
                    {
                        if (buttonIncrease && CanIncrease())
                            buttonIncrease.onClick.Invoke();
                    }
                    else
                    {
                        if (buttonDecrease && CanDecrease())
                            buttonDecrease.onClick.Invoke();
                    }

                    lastScrollTriggerFrame = Time.frameCount;
                }

                amount = Mathf.Clamp(amount, isCrafting ? 0 : 1, maxAmount);
                textAmount?.SetText(amount.ToString());

                if (buttonIncrease)
                    buttonIncrease.interactable = CanIncrease();

                if (buttonDecrease)
                    buttonDecrease.interactable = CanDecrease();

                if (amount > 1)
                    textCrafting.SetText(Localization.instance.Localize($"$inventory_craftingprog ({amount})"));

                if (queueNextCraft)
                    __instance.m_craftButton.onClick.Invoke();

                queueNextCraft = false;

                bool CanIncrease() => amount < maxAmount;
                bool CanDecrease() => amount > 1;
            }

            private static string CombinedResAmount()
            {
                if (resAmountElements.Count == 0)
                    for (int i = 0; i < InventoryGui.instance.m_recipeRequirementList.Length; i++)
                        resAmountElements.Add(InventoryGui.instance.m_recipeRequirementList[i].transform.Find("res_amount")?.GetComponent<TMP_Text>());

                if (resAmountElements.Count == 0)
                    return "";

                bool clearAndRestart = false;
                sb.Clear(); 
                foreach (TMP_Text text in resAmountElements)
                {
                    if (text == null)
                    {
                        clearAndRestart = true;
                        break;
                    }

                    if (text.isActiveAndEnabled)
                        sb.Append(text.text);
                }

                if (clearAndRestart)
                {
                    resAmountElements.Clear();
                    return CombinedResAmount();
                }
                    
                return sb.ToString();
            }

            private static string GetNumbersString()
            {
                string combinedResources = CombinedResAmount();
                if (combinedResources.IsNullOrWhiteSpace())
                    return combinedResources;

                // Clear tags and get only numbers from string
                string numbers = Regex.Replace(Regex.Replace(combinedResources, "<.*?>", string.Empty), @"[^\d]", string.Empty);
                if (InventoryGui.instance.m_selectedRecipe.Recipe.m_requireOnlyOneIngredient)
                    numbers += Mathf.Sin(Time.time * 5f) > 0f;

                return numbers;
            }

            private static int GetMaxAmountCached()
            {
                string numbers = GetNumbersString();
                if (numbers.IsNullOrWhiteSpace())
                    return 0;

                if (cachedAmount.TryGetValue(InventoryGui.instance.m_selectedRecipe.Recipe, out Tuple<string, int> tuple) && tuple.Item1 == numbers)
                    return tuple.Item2;

                Stopwatch stopwatch = Stopwatch.StartNew();
                int maxAmount = GetMaximumAmount(InventoryGui.instance.m_selectedRecipe.Recipe, Player.m_localPlayer);
                LogInfo($"Multicraft max amount calculated in {(double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000d:F2} ms");
                stopwatch.Stop();

                cachedAmount[InventoryGui.instance.m_selectedRecipe.Recipe] = Tuple.Create(numbers, maxAmount);
                return maxAmount;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class InventoryGui_Awake_MulticraftCreateButtons
        {
            public static void Postfix(InventoryGui __instance)
            {
                craftButton = __instance.m_craftButton?.GetComponent<RectTransform>();

                CreateMulticraftPanel();

                UpdateMulticraftPanel();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        public static class InventoryGui_OnDestroy_MulticraftOnDestroy
        {
            public static void Postfix()
            {
                panel = null;
                craftButton = null;
                buttonIncrease = null;
                buttonDecrease = null;
                textAmount = null;
                resAmountElements.Clear();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ToggleNoPlacementCost))]
        public static class Player_ToggleNoPlacementCost_MulticraftUpdateMaxAmount
        {
            public static void Postfix()
            {
                cachedAmount.Clear();
            }
        }
    }
}