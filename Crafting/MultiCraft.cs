using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    public static class MultiCraft
    {
        public const string panelName = "MLUI_Multicraft";
        public const string buttonIncreaseName = "Increase";
        public const string buttonDecreaseName = "Decrease";
        public const string textAmountName = "Amount";
        private const int maximumAmount = 99;

        private static Recipe tempRecipe;
        private static Recipe tempRecipeSource;
        private static Recipe cachedRecipe;
        private static Player cachedPlayer;
        private static CraftingStation cachedStation;
        private static bool cachedNoCost;
        private static int cachedMaximum;
        private static float cacheUntil;

        private static RectTransform panel;
        private static RectTransform craftButton;
        private static Vector2 craftButtonAnchorMax;
        private static Button buttonIncrease;
        private static Button buttonDecrease;
        private static TMP_Text textAmount;
        private static TMP_Text textCrafting;
        private static int amount = 1;
        private static bool showPanel;

        private static InventoryGui queueGui;
        private static Player queuePlayer;
        private static Recipe queueRecipe;
        private static CraftingStation queueStation;
        private static int queueVariant;
        private static bool queueNextCraft;
        private static CraftAttempt activeAttempt;

        private sealed class CraftAttempt
        {
            public CraftAttempt Previous;
            public InventoryGui Gui;
            public Player Player;
            public Recipe Recipe;
            public bool Owned;
            public bool ProducedItem;
        }

        public static int lastScrollTriggerFrame;
        public const int minScrollDeltaFrames = 2;
        public static bool IsMulticraftEnabled => modEnabled.Value && showMulticraftButtons.Value && !AAA_Crafting;

        private static bool IsCrafting(InventoryGui gui) => gui && gui.m_craftTimer >= 0f;
        private static bool NativeBatchRequested(InventoryGui gui) => gui &&
            (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyLStick") || gui.m_touchMultiCrafting);

        private static bool CanQueue(InventoryGui gui)
        {
            return IsMulticraftEnabled && gui && gui == InventoryGui.instance && Player.m_localPlayer
                && !Player.m_localPlayer.IsDead() && InventoryGui.IsVisible() && gui.InCraftTab()
                && gui.m_selectedRecipe.Recipe && gui.m_selectedRecipe.ItemData == null
                && !NativeBatchRequested(gui) && !(IsCrafting(gui) && gui.m_multiCrafting);
        }

        private static bool QueueContextMatches()
        {
            return queueGui && queuePlayer && queuePlayer == Player.m_localPlayer && CanQueue(queueGui)
                && queueGui.m_selectedRecipe.Recipe == queueRecipe
                && queuePlayer.GetCurrentCraftingStation() == queueStation
                && queueGui.m_selectedVariant == queueVariant
                && (!IsCrafting(queueGui) || (queueGui.m_craftRecipe == queueRecipe
                    && queueGui.m_craftUpgradeItem == null && queueGui.m_craftVariant == queueVariant));
        }

        private static void StopQueue(bool resetAmount = true)
        {
            queueGui = null;
            queuePlayer = null;
            queueRecipe = null;
            queueStation = null;
            queueNextCraft = false;
            amount = resetAmount ? 1 : Mathf.Clamp(amount, 1, maximumAmount);
        }

        private static int GetMaximumAmount(Recipe recipe, Player player)
        {
            if (!recipe || !recipe.m_item || !player)
                return 0;
            if (player.NoCostCheat())
                return maximumAmount;

            CraftingStation station = player.GetCurrentCraftingStation();
            if (recipe.GetRequiredStation(1) && (!station || !station.CheckUsable(player, showMessage: false)))
                return 0;
            if (ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
                return maximumAmount;
            if (!recipe.m_requireOnlyOneIngredient)
                return GetMaxCraftAmount(player, recipe);

            // Clone the recipe, not serialized Unity references in its requirements.
            // Future recipe fields are retained and the original resources remain read-only.
            if (!tempRecipe || tempRecipeSource != recipe)
            {
                if (tempRecipe)
                    UnityEngine.Object.Destroy(tempRecipe);
                tempRecipe = UnityEngine.Object.Instantiate(recipe);
                tempRecipeSource = recipe;
                tempRecipe.m_requireOnlyOneIngredient = false;
                tempRecipe.m_resources = new Piece.Requirement[1];
            }

            int result = 0;
            foreach (Piece.Requirement requirement in recipe.m_resources)
            {
                if (requirement?.m_resItem == null || requirement.GetAmount(1) < 1
                    || !player.IsKnownMaterial(requirement.m_resItem.m_itemData.m_shared.m_name))
                    continue;
                tempRecipe.m_resources[0] = requirement;
                result = Math.Min(maximumAmount, result + GetMaxCraftAmount(player, tempRecipe));
                if (result == maximumAmount)
                    break;
            }
            return result;
        }

        private static int GetMaxCraftAmount(Player player, Recipe recipe)
        {
            if (!HaveRequirements(1))
                return 0;
            int left = 1, right = maximumAmount;
            while (left < right)
            {
                int mid = (left + right + 1) / 2;
                if (HaveRequirements(mid))
                    left = mid;
                else
                    right = mid - 1;
            }
            return left;

            bool HaveRequirements(int count) => player.HaveRequirements(recipe, discover: false, qualityLevel: 1, amount: count);
        }

        private static int GetMaximumCached(Recipe recipe, Player player)
        {
            if (!recipe || !player)
                return 0;
            CraftingStation station = player.GetCurrentCraftingStation();
            bool noCost = player.NoCostCheat() || (ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost));
            if (cachedRecipe != recipe || cachedPlayer != player || cachedStation != station
                || cachedNoCost != noCost || Time.unscaledTime >= cacheUntil)
            {
                cachedRecipe = recipe;
                cachedPlayer = player;
                cachedStation = station;
                cachedNoCost = noCost;
                cachedMaximum = GetMaximumAmount(recipe, player);
                cacheUntil = Time.unscaledTime + 0.2f;
            }
            return cachedMaximum;
        }

        private static Button CreateAmountButton(string name, string label, bool increase)
        {
            GameObject clone = UnityEngine.Object.Instantiate(craftButton.gameObject, panel);
            clone.name = name;
            Button button = clone.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => ChangeAmount(increase ? 1 : -1));
            foreach (EventTrigger trigger in clone.GetComponentsInChildren<EventTrigger>(true))
                trigger.triggers = new List<EventTrigger.Entry>();

            UIGamePad gamepad = clone.GetComponent<UIGamePad>();
            if (gamepad)
            {
                gamepad.m_zinputKey = increase ? "JoyRStickUp" : "JoyRStickDown";
                gamepad.m_keyCode = increase ? KeyCode.UpArrow : KeyCode.DownArrow;
                if (gamepad.m_hint && gamepad.m_hint.transform.IsChildOf(clone.transform))
                    UnityEngine.Object.Destroy(gamepad.m_hint);
                gamepad.m_hint = null;
            }

            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, increase ? 0.5f : 0f);
            rect.anchorMax = new Vector2(1f, increase ? 1f : 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            TMP_Text text = clone.GetComponentInChildren<TMP_Text>(true);
            if (text)
            {
                text.SetText(label);
                text.rectTransform.offsetMin = new Vector2(0f, 2f);
                text.rectTransform.offsetMax = Vector2.zero;
            }
            return button;
        }

        private static void CreateMulticraftPanel()
        {
            if (!craftButton || panel || !InventoryGui.instance.m_craftButton)
                return;
            TMP_Text template = craftButton.GetComponentInChildren<TMP_Text>(true);
            if (!template)
                return;

            textCrafting = InventoryGui.instance.m_craftProgressPanel?.Find("Text")?.GetComponent<TMP_Text>();
            panel = new GameObject(panelName, typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(craftButton.parent, false);
            panel.anchorMin = new Vector2(0.75f, 0f);
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = new Vector2(0f, -5f);
            panel.gameObject.AddComponent<AmountScrollHandler>();
            buttonIncrease = CreateAmountButton(buttonIncreaseName, "+", increase: true);
            buttonDecrease = CreateAmountButton(buttonDecreaseName, "-", increase: false);
            textAmount = UnityEngine.Object.Instantiate(template, panel);
            textAmount.name = textAmountName;
            RectTransform rect = textAmount.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.sizeDelta = new Vector2(-4f, 0f);
            textAmount.fontSizeMax = 32f;
            textAmount.SetText("1");
        }

        internal static void UpdateMulticraftPanel()
        {
            InventoryGui gui = InventoryGui.instance;
            showPanel = panel && CanQueue(gui) && (gui.m_selectedRecipe.CanCraft || Player.m_localPlayer.NoCostCheat());
            if (panel)
                panel.gameObject.SetActive(showPanel && gui.m_craftButton && gui.m_craftButton.isActiveAndEnabled);
            if (craftButton)
                craftButton.anchorMax = showPanel ? new Vector2(0.75f, craftButtonAnchorMax.y) : craftButtonAnchorMax;
        }

        private static void ChangeAmount(int direction)
        {
            InventoryGui gui = InventoryGui.instance;
            if (!showPanel || !CanQueue(gui))
                return;
            int delta = UnityInput.Current.GetKey(KeyCode.LeftControl) || UnityInput.Current.GetKey(KeyCode.RightControl) ? maximumAmount
                : UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift) ? 10 : 1;
            int maximum = GetMaximumCached(gui.m_selectedRecipe.Recipe, Player.m_localPlayer);
            amount = Mathf.Clamp(amount + direction * delta, 1, Math.Max(1, maximum));
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
        private static class InventoryGui_OnCraftPressed_StartQueue
        {
            private static void Prefix(InventoryGui __instance, out bool __state) => __state = IsCrafting(__instance);

            private static void Postfix(InventoryGui __instance, bool __state)
            {
                if (__state || !IsCrafting(__instance) || __instance.m_multiCrafting || !CanQueue(__instance)
                    || __instance.m_craftUpgradeItem != null || __instance.m_craftRecipe != __instance.m_selectedRecipe.Recipe)
                {
                    StopQueue();
                    return;
                }
                queueGui = __instance;
                queuePlayer = Player.m_localPlayer;
                queueRecipe = __instance.m_craftRecipe;
                queueStation = queuePlayer.GetCurrentCraftingStation();
                queueVariant = __instance.m_craftVariant;
                queueNextCraft = false;
            }

            private static void Finalizer(Exception __exception)
            {
                if (__exception != null)
                    StopQueue();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static class InventoryGui_DoCrafting_ObserveCompletion
        {
            private static void Prefix(InventoryGui __instance, Player player, out CraftAttempt __state)
            {
                __state = new CraftAttempt
                {
                    Previous = activeAttempt,
                    Gui = __instance,
                    Player = player,
                    Recipe = __instance.m_craftRecipe,
                    Owned = queueGui == __instance && QueueContextMatches()
                };
                activeAttempt = __state;
            }

            private static void Postfix(CraftAttempt __state)
            {
                if (!__state.Owned || queueGui != __state.Gui)
                    return;
                if (!__state.ProducedItem || !QueueContextMatches())
                {
                    StopQueue();
                    return;
                }
                amount = Math.Max(0, amount - 1);
                queueNextCraft = amount > 0;
                cacheUntil = 0f;
                if (!queueNextCraft)
                    StopQueue(resetAmount: false);
            }

            private static void Finalizer(CraftAttempt __state, Exception __exception)
            {
                if (__state != null)
                    activeAttempt = __state.Previous;
                if (__exception != null)
                    StopQueue();
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int), typeof(int),
            typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool))]
        private static class Inventory_AddItem_ObserveCraftedOutput
        {
            private static void Postfix(Inventory __instance, string name, ItemDrop.ItemData __result)
            {
                CraftAttempt attempt = activeAttempt;
                if (attempt != null && attempt.Owned && attempt.Player && attempt.Recipe && attempt.Recipe.m_item
                    && __result != null && __instance == attempt.Player.GetInventory() && name == attempt.Recipe.m_item.gameObject.name)
                    attempt.ProducedItem = true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        public static class InventoryGui_UpdateRecipe_MulticraftShowButtons
        {
            internal static bool isCrafting;

            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter("Azumatt.AzuCraftyBoxes", "aedenthorn.CraftFromContainers", "org.bepinex.plugins.valheim_plus")]
            public static void Postfix(InventoryGui __instance)
            {
                isCrafting = IsCrafting(__instance);
                if (queueGui && !QueueContextMatches())
                    StopQueue();
                UpdateMulticraftPanel();
                if (!IsMulticraftEnabled)
                    return;

                if (textCrafting && queueGui == __instance && amount > 1)
                    textCrafting.SetText(Localization.instance.Localize($"$inventory_craftingprog ({amount})"));
                if (!showPanel)
                    return;

                int maximum = GetMaximumCached(__instance.m_selectedRecipe.Recipe, Player.m_localPlayer);
                amount = Mathf.Clamp(amount, 1, Math.Max(1, maximum));
                if (AmountScrollHandler.hovered && panel.gameObject.activeInHierarchy
                    && Time.frameCount - lastScrollTriggerFrame > minScrollDeltaFrames)
                {
                    float scroll = ZInput.GetMouseScrollWheel();
                    if (scroll != 0f)
                    {
                        ChangeAmount(scroll > 0f ? 1 : -1);
                        lastScrollTriggerFrame = Time.frameCount;
                    }
                }
                if (textAmount)
                    textAmount.SetText(maximum > 0 ? amount.ToString() : "0");
                if (buttonIncrease)
                    buttonIncrease.interactable = __instance.m_craftButton.interactable && amount < maximum;
                if (buttonDecrease)
                    buttonDecrease.interactable = __instance.m_craftButton.interactable && amount > 1;

                if (!queueNextCraft || isCrafting)
                    return;
                queueNextCraft = false;
                // Revalidate actual requirements; display cache entries never authorize crafting.
                if (QueueContextMatches() && __instance.m_craftButton.interactable && GetMaximumAmount(queueRecipe, queuePlayer) > 0)
                    __instance.m_craftButton.onClick.Invoke();
                else
                    StopQueue();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftCancelPressed))]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        private static class InventoryGui_Cancel_StopQueue
        {
            private static void Prefix() => StopQueue();
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetRecipe))]
        private static class InventoryGui_SetRecipe_ValidateQueue
        {
            private static void Postfix()
            {
                cacheUntil = 0f;
                if (queueGui && !QueueContextMatches())
                    StopQueue();
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
        private static class Inventory_Changed_InvalidateAmount
        {
            private static void Postfix(Inventory __instance)
            {
                if (Player.m_localPlayer && __instance == Player.m_localPlayer.GetInventory())
                    cacheUntil = 0f;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ToggleNoPlacementCost))]
        private static class Player_ToggleNoPlacementCost_InvalidateAmount
        {
            private static void Postfix() => cacheUntil = 0f;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        private static class InventoryGui_Awake_CreateButtons
        {
            private static void Postfix(InventoryGui __instance)
            {
                if (AAA_Crafting)
                    return;
                craftButton = __instance.m_craftButton ? __instance.m_craftButton.GetComponent<RectTransform>() : null;
                if (craftButton)
                    craftButtonAnchorMax = craftButton.anchorMax;
                CreateMulticraftPanel();
                UpdateMulticraftPanel();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        private static class InventoryGui_OnDestroy_ClearState
        {
            private static void Postfix()
            {
                StopQueue();
                panel = craftButton = null;
                buttonIncrease = buttonDecrease = null;
                textAmount = textCrafting = null;
                cachedRecipe = tempRecipeSource = null;
                cachedPlayer = null;
                cachedStation = null;
                cacheUntil = 0f;
                showPanel = false;
                AmountScrollHandler.hovered = false;
                InventoryGui_UpdateRecipe_MulticraftShowButtons.isCrafting = false;
                if (tempRecipe)
                    UnityEngine.Object.Destroy(tempRecipe);
                tempRecipe = null;
            }
        }
    }
}
