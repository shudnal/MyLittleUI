﻿using GUIFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    internal static class CraftFilter
    {
        private const float fieldHeight = 32f;

        private static readonly Dictionary<Recipe, string> recipeCache = new Dictionary<Recipe, string>();

        private static GuiInputField playerFilter;

        private static string[] filterString = Array.Empty<string>();

        private static readonly StringBuilder sb = new StringBuilder();
        private static readonly StringBuilder sbItem = new StringBuilder();

        private static GuiInputField InitFilterField(RectTransform parent)
        {
            // Add filter field on the bottom of crafting list
            GameObject filterField = UnityEngine.Object.Instantiate(TextInput.instance.m_inputField.gameObject, parent);
            filterField.name = "FilterField";

            RectTransform playerFilterRT = filterField.GetComponent<RectTransform>();
            playerFilterRT.anchorMin = new Vector2(0.5f, 0.00f);
            playerFilterRT.anchorMax = new Vector2(0.5f, 0.00f);
            playerFilterRT.sizeDelta = new Vector2(parent.rect.width - 3, fieldHeight);
            playerFilterRT.anchoredPosition = new Vector2(0, (fieldHeight / 2) + 1);

            GuiInputField filter = filterField.GetComponent<GuiInputField>();
            filter.VirtualKeyboardTitle = "$menu_filter";
            filter.transform.Find("Text Area/Placeholder").GetComponent<TMP_Text>().SetText(Localization.instance.Localize("$menu_filter"));

            filter.restoreOriginalTextOnEscape = false;

            return filter;
        }

        public static void UpdateVisibility()
        {
            playerFilter.gameObject.SetActive(modEnabled.Value && craftingFilterEnabled.Value);
        }

        public static void UpdateFilterString() 
        {
            filterString = playerFilter.text.ToLower().Split(new char[] { ' ' }, StringSplitOptions.None);
        }

        private static string GetItemFullString(ItemDrop itemDrop)
        {
            sbItem.Clear();
            sbItem.Append(itemDrop.name);
            sbItem.Append(' ');
            sbItem.Append(itemDrop.m_itemData.m_shared.m_itemType);
            sbItem.Append(' ');
            sbItem.Append(itemDrop.m_itemData.m_shared.m_setName);
            sbItem.Append(' ');
            sbItem.Append(itemDrop.m_itemData.m_shared.m_name);
            sbItem.Append(' ');
            sbItem.Append(Localization.instance.Localize(itemDrop.m_itemData.m_shared.m_name));
            sbItem.Append(' ');
            sbItem.Append(itemDrop.m_itemData.GetTooltip());

            return sbItem.ToString();
        }

        private static void CacheRecipe(Recipe recipe)
        {
            if (recipeCache.ContainsKey(recipe))
                return;

            sb.Clear();
            sb.Append(recipe.name);
            sb.Append(' ');

            sb.Append(GetItemFullString(recipe.m_item));
            sb.Append(' ');

            recipe.m_resources.Do(req => { sb.Append(GetItemFullString(req.m_resItem)); sb.Append(' '); });

            recipeCache[recipe] = sb.ToString().ToLower();
        }

        private static bool FitsFilterString(Recipe recipe)
        {
            return recipeCache.ContainsKey(recipe) && filterString.All(substr => recipeCache[recipe].Contains(substr));
        }

        private static void StartPanelUpdate()
        {
            instance.CancelInvoke("UpdateCraftingPanel");
            instance.Invoke("UpdateCraftingPanel", recipeCache.Count == 0f ? 0.4f : 0.2f);
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class StoreGui_Awake_InitializePanel
        {
            [HarmonyPriority(Priority.First)]
            static void Postfix()
            {
                playerFilter = InitFilterField(InventoryGui.instance.m_recipeListRoot.parent as RectTransform);
                playerFilter.onValueChanged.AddListener(delegate
                {
                    UpdateFilterString();
                    StartPanelUpdate();
                });

                UpdateVisibility();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        public static class InventoryGui_Show_ClearCache
        {
            public static void Postfix()
            {
                recipeCache.Clear();
                playerFilter.text = "";
            }
        }

        [HarmonyPatch(typeof(Chat), nameof(Chat.HasFocus))]
        public static class Chat_HasFocus_FocusOverride
        {
            public static void Postfix(ref bool __result)
            {
                __result = __result || modEnabled.Value && craftingFilterEnabled.Value && playerFilter.isFocused;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetAvailableRecipes))]
        public static class Player_GetAvailableRecipes_FilterRecipeList
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(ref List<Recipe> available)
            {
                if (!modEnabled.Value || !craftingFilterEnabled.Value)
                    return;

                if (filterString.Length == 0)
                    return;

                Stopwatch stopwatch = Stopwatch.StartNew();

                available.Do(CacheRecipe);

                LogInfo($"Recipe cache: verified {recipeCache.Count} in {(double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000d:F2} ms");
                
                stopwatch.Restart();

                LogInfo($"Recipe filter: removed {available.RemoveAll(recipe => !FitsFilterString(recipe))} in {(double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000d:F2} ms");
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        public static class InventoryGui_UpdateRecipe_HandleFieldFocus
        {
            public static void Postfix()
            {
                if (!modEnabled.Value || !craftingFilterEnabled.Value)
                    return;

                bool flag = ZInput.InputLayout == InputLayout.Alternative1;
                bool button = ZInput.GetButton("JoyLBumper");
                bool button2 = ZInput.GetButton("JoyLTrigger");

                if (playerFilter.isFocused)
                {
                    if (ZInput.GetButtonDown("Chat") || ZInput.GetButtonDown("Block") || ZInput.GetButtonDown("Console") || ZInput.GetButtonDown("Escape") || ZInput.GetButtonDown("Inventory") || (ZInput.GetButtonDown("JoyChat") && ZInput.GetButton("JoyAltKeys") && !(flag && button2) && !(!flag && button)))
                        playerFilter.DeactivateInputField();
                }
                else if (Player.m_localPlayer != null && !Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && !Menu.IsVisible())
                {
                    if (ZInput.GetButtonDown("Chat") || (ZInput.GetButtonDown("JoyChat") && ZInput.GetButton("JoyAltKeys") && !(flag && button2) && !(!flag && button)))
                        playerFilter.ActivateInputField();
                }
            }
        }
    }
}
