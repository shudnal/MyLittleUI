using GUIFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    public static class CraftFilter
    {
        private const float fieldHeight = 32f;

        private static Vector2 listAnchorMin = new Vector2(-1f, -1f);

        private static readonly Dictionary<Recipe, string> recipeCache = new Dictionary<Recipe, string>();

        private static GuiInputField playerFilter;

        private static string[] filterString = Array.Empty<string>();
        private static bool applyFilter;

        private static readonly StringBuilder sb = new StringBuilder();
        private static readonly StringBuilder sbItem = new StringBuilder();

        public static bool IsCraftingFilterEnabled => modEnabled.Value && craftingFilterEnabled.Value && !AAA_Crafting && !ZenUI;

        private static void InitFilterField()
        {
            if (AAA_Crafting || ZenUI || playerFilter)
                return;

            InventoryGui gui = InventoryGui.instance;
            if (!gui || !gui.m_recipeListScroll || !TextInput.instance || !TextInput.instance.m_inputField
                || !gui.m_splitDialog || !gui.m_splitDialog.m_splitOkButton)
                return;

            RectTransform recipeList = gui.m_recipeListScroll.transform.parent as RectTransform;
            if (!recipeList)
                return;

            // Add filter field on the bottom of crafting list.
            GameObject filterField = UnityEngine.Object.Instantiate(TextInput.instance.m_inputField.gameObject, recipeList.parent);
            filterField.name = "MLUI_FilterField";
            filterField.transform.SetSiblingIndex(recipeList.GetSiblingIndex() + 1);

            RectTransform playerFilterRT = filterField.GetComponent<RectTransform>();
            playerFilterRT.anchorMin = Vector2.zero;
            playerFilterRT.anchorMax = Vector2.zero;
            playerFilterRT.sizeDelta = new Vector2(recipeList.rect.width, fieldHeight);
            playerFilterRT.anchoredPosition = new Vector2(recipeList.anchoredPosition.x, 4);
            playerFilterRT.pivot = Vector2.zero;

            playerFilter = filterField.GetComponent<GuiInputField>();
            playerFilter.VirtualKeyboardTitle = "$menu_filter";
            playerFilter.transform.Find("Text Area/Placeholder")?.GetComponent<TMP_Text>()?.SetText(Localization.instance.Localize("$menu_filter"));
            playerFilter.restoreOriginalTextOnEscape = false;

            Button clearButton = UnityEngine.Object.Instantiate(gui.m_splitDialog.m_splitOkButton, filterField.transform);
            clearButton.name = "ClearTextButton";

            // Do not retain persistent or runtime actions from the split dialog.
            clearButton.onClick = new Button.ButtonClickedEvent();
            clearButton.onClick.AddListener(ClearText);

            RectTransform clearButtonRT = clearButton.GetComponent<RectTransform>();
            clearButtonRT.anchorMin = new Vector2(1f, 0.5f);
            clearButtonRT.anchorMax = new Vector2(1f, 0.5f);
            clearButtonRT.sizeDelta = Vector2.one * 28f;
            clearButtonRT.anchoredPosition = new Vector2(-16f, 0f);

            TMP_Text text = clearButton.GetComponentInChildren<TMP_Text>(true);
            if (text)
            {
                text.SetText("✖");
                text.margin = Vector4.one * -2f;
            }

            UIGamePad gamepad = clearButton.GetComponent<UIGamePad>();
            if (gamepad)
            {
                gamepad.m_zinputKey = "";
                gamepad.m_keyCode = KeyCode.None;
                if (gamepad.m_hint)
                    gamepad.m_hint.SetActive(false);
            }

            playerFilter.onValueChanged.AddListener(delegate
            {
                UpdateFilterString();
                StartPanelUpdate();
            });
        }

        public static void UpdateVisibility()
        {
            if (AAA_Crafting || ZenUI || !InventoryGui.instance)
                return;

            if (playerFilter)
            {
                if (!IsCraftingFilterEnabled && playerFilter.isFocused)
                    playerFilter.DeactivateInputField();
                playerFilter.gameObject.SetActive(IsCraftingFilterEnabled);
            }

            if (InventoryGui.instance.m_recipeListScroll)
            {
                RectTransform recipeList = InventoryGui.instance.m_recipeListScroll.transform.parent as RectTransform;
                if (!recipeList)
                    return;

                if (listAnchorMin.x == -1f)
                    listAnchorMin = recipeList.anchorMin;

                recipeList.anchorMin = playerFilter && playerFilter.isActiveAndEnabled ? listAnchorMin + new Vector2(0f, 0.05f) : listAnchorMin;
            }
        }

        public static void UpdateFilterString()
        {
            applyFilter = !string.IsNullOrWhiteSpace(playerFilter?.text);
            filterString = applyFilter ? playerFilter.text.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
        }

        public static void ClearText()
        {
            applyFilter = false;
            filterString = Array.Empty<string>();
            if (playerFilter)
                playerFilter.text = "";
        }

        private static string GetItemFullString(ItemDrop itemDrop)
        {
            if (!itemDrop)
                return "";

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
            if (!recipe || recipeCache.ContainsKey(recipe))
                return;

            sb.Clear();
            sb.Append(recipe.name);
            sb.Append(' ');

            sb.Append(GetItemFullString(recipe.m_item));
            sb.Append(' ');

            foreach (Piece.Requirement requirement in recipe.m_resources)
            {
                if (requirement == null)
                    continue;

                sb.Append(GetItemFullString(requirement.m_resItem));
                sb.Append(' ');
            }

            recipeCache[recipe] = sb.ToString().ToLowerInvariant();
        }

        private static bool FitsFilterString(Recipe recipe)
        {
            return recipe && recipeCache.ContainsKey(recipe) && filterString.All(substr => recipeCache[recipe].Contains(substr));
        }

        private static void StartPanelUpdate()
        {
            instance.CancelInvoke("UpdateCraftingPanel");
            instance.Invoke("UpdateCraftingPanel", recipeCache.Count == 0 ? 0.4f : 0.2f);
        }

        public static void UpdateCraftingPanel()
        {
            if (!InventoryGui.instance || !Player.m_localPlayer)
                return;

            InventoryGui.instance.UpdateCraftingPanel(focusView: true);
            InventoryGui.instance.m_moveItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class StoreGui_Awake_InitializePanel
        {
            [HarmonyPriority(Priority.First)]
            static void Postfix()
            {
                InitFilterField();
                UpdateVisibility();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        public static class InventoryGui_Show_ClearCache
        {
            public static void Postfix()
            {
                ClearText();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        public static class InventoryGui_OnDestroy_ClearCache
        {
            public static void Postfix()
            {
                instance.CancelInvoke("UpdateCraftingPanel");
                recipeCache.Clear();
                playerFilter = null;
                applyFilter = false;
                filterString = Array.Empty<string>();
                listAnchorMin = new Vector2(-1f, -1f);
            }
        }

        [HarmonyPatch(typeof(Chat), nameof(Chat.HasFocus))]
        public static class Chat_HasFocus_FocusOverride
        {
            public static void Postfix(ref bool __result)
            {
                __result = __result || IsCraftingFilterEnabled && playerFilter && playerFilter.isFocused;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetAvailableRecipes))]
        public static class Player_GetAvailableRecipes_FilterRecipeList
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(ref List<Recipe> available)
            {
                if (!IsCraftingFilterEnabled || !applyFilter)
                    return;

                Stopwatch stopwatch = Stopwatch.StartNew();
                available.Do(CacheRecipe);
                LogInfo($"Recipe cache: verified {recipeCache.Count} in {(double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000d:F2} ms");

                stopwatch.Restart();
                int removed = available.RemoveAll(recipe => !FitsFilterString(recipe));
                LogInfo($"Recipe filter: removed {removed} in {(double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000d:F2} ms");
                stopwatch.Stop();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        public static class InventoryGui_UpdateRecipe_HandleFieldFocus
        {
            public static void Postfix()
            {
                if (!IsCraftingFilterEnabled || !playerFilter || !playerFilter.isActiveAndEnabled)
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
