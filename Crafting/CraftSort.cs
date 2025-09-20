using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MyLittleUI.MyLittleUI;


namespace MyLittleUI
{
    public static class CraftSort
    {
        public class FilteringState
        {
            public int position;
            public bool selectable;
            public bool enabled;

            public string category;
            public string tooltip;
            public Sprite icon;
            public string name;
            public Color imageColor;

            public RectTransform element;
            public Button button;
            public Image image;
            public GameObject active;

            public Transform panel;

            public Comparison<InventoryGui.RecipeDataPair> sort;
            public Func<ItemDrop.ItemData, bool> filter;
            public Func<ItemDrop.ItemData, bool> check;

            public FilteringState()
            {
                filteringStates.Add(this);
            }

            public void CreateElement()
            {
                if (element)
                    return;

                element = UnityEngine.Object.Instantiate(elementPrefab, panel);
                element.name = name;
                element.gameObject.SetActive(true);
                element.GetComponent<UITooltip>().m_text = tooltip;
                image = element.Find("icon").GetComponent<Image>();
                if (image)
                {
                    image.overrideSprite = icon;
                    image.color = imageColor;
                    image.transform.localScale *= 0.9f;
                }
                button = element.GetComponent<Button>();
                active = element.Find("active")?.gameObject;

                UpdatePosition();
            }

            public void UpdatePosition()
            {
                element.anchoredPosition = new Vector2(12f + (position % 3) * (32f + 2f), -8f + (position / 3) * (32f + 2f));
            }
        }

        public static readonly List<FilteringState> filteringStates = new List<FilteringState>();

        public static GameObject parentObject;

        public static RectTransform sortPanel;
        public static RectTransform selectedFrame = null;
        public static RectTransform elementPrefab;

        public static Sprite foodSprite;

        public static bool IsCraftingFilterEnabled => modEnabled.Value && craftingSortingEnabled.Value && !ForceDisableCraftingWindow;

        public static void UpdateVisibility()
        {
            bool isVisible = InventoryGui.instance?.m_animator.GetBool("visible") == true;

            sortPanel.gameObject.SetActive(isVisible);

        }

        internal static void InitElementPrefab(Transform parent)
        {
            if (elementPrefab)
                return;

            elementPrefab = UnityEngine.Object.Instantiate(InventoryGui.instance.m_playerGrid.m_elementPrefab, parent).GetComponent<RectTransform>();
            for (int i = elementPrefab.childCount - 1; i >= 0; i--)
            {
                Transform child = elementPrefab.GetChild(i);
                child.gameObject.SetActive(false);
                switch (child.name)
                {
                    case "foodicon":
                        foodSprite ??= child.GetComponent<Image>().sprite;
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                        break;
                    case "amount":
                    case "durability":
                    case "binding":
                    case "quality":
                    case "noteleport":
                    case "equiped":
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                        break;
                    case "icon":
                        child.gameObject.SetActive(true);
                        break;
                    case "queued":
                        child.name = "active";
                        break;
                }
            }

            elementPrefab.name = "sortingElement";
            elementPrefab.sizeDelta = Vector2.one * 32f;
            elementPrefab.gameObject.SetActive(false);
            elementPrefab.GetComponent<UITooltip>().m_tooltipPrefab = InventoryGui.instance.m_repairButton.GetComponent<UITooltip>().m_tooltipPrefab;
        }

        internal static void InitSortingPanel()
        {
            if (ForceDisableCraftingWindow)
                return;

            sortPanel = UnityEngine.Object.Instantiate(InventoryGui.instance.m_repairPanel as RectTransform, InventoryGui.instance.m_repairPanel.transform.parent);
            sortPanel.name = "MLUI_SortingPanel";
            sortPanel.SetSiblingIndex(InventoryGui.instance.m_repairPanel.GetSiblingIndex() + 1);

            sortPanel.anchoredPosition = new Vector2(42f, -250f);
            sortPanel.sizeDelta = new Vector2(166f, 48f);

            sortPanel.gameObject.SetActive(false);

            InitElementPrefab(sortPanel);

            selectedFrame = UnityEngine.Object.Instantiate(InventoryGui.instance.m_repairPanelSelection as RectTransform, InventoryGui.instance.m_repairPanelSelection.transform.parent);
            selectedFrame.name = "selected (MLUI_SortingFood)"; // TODO

            new FilteringState()
            {
                panel = sortPanel,
                name = "food1",
                category = "$hud_food",
                tooltip = "$item_food_health",
                icon = foodSprite,
                imageColor = InventoryGui.instance.m_playerGrid.m_foodHealthColor,
                position = 0,
                
                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.ItemData.m_shared.m_food.CompareTo(a.ItemData.m_shared.m_food);
                },
                filter = (ItemDrop.ItemData item) => item.m_shared.m_food > 0,
                check = (ItemDrop.ItemData item) => item.m_shared.m_food != 0,
            };

            new FilteringState()
            {
                panel = sortPanel,
                name = "food2",
                category = "$hud_food",
                tooltip = "$item_food_stamina",
                icon = foodSprite,
                imageColor = InventoryGui.instance.m_playerGrid.m_foodStaminaColor,
                position = 1,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.ItemData.m_shared.m_foodStamina.CompareTo(a.ItemData.m_shared.m_foodStamina);
                },
                filter = (ItemDrop.ItemData item) => item.m_shared.m_foodStamina > 0,
                check = (ItemDrop.ItemData item) => item.m_shared.m_foodStamina != 0,
            };

            new FilteringState()
            {
                panel = sortPanel,
                name = "food3",
                category = "$hud_food",
                tooltip = "$item_food_eitr",
                icon = foodSprite,
                imageColor = InventoryGui.instance.m_playerGrid.m_foodEitrColor,
                position = 2,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.ItemData.m_shared.m_foodEitr.CompareTo(a.ItemData.m_shared.m_foodEitr);
                },
                filter = (ItemDrop.ItemData item) => item.m_shared.m_foodEitr > 0,
                check = (ItemDrop.ItemData item) => item.m_shared.m_foodEitr != 0,
            };
            
            filteringStates.ForEach(fs => fs.CreateElement());

            /*RectTransform recipeList = InventoryGui.instance.m_recipeListScroll.transform.parent as RectTransform;

            // Add filter field on the bottom of crafting list
            GameObject filterField = UnityEngine.Object.Instantiate(TextInput.instance.m_inputField.gameObject, recipeList.parent);
            filterField.name = "MLUI_FilterSorting";
            filterField.transform.SetSiblingIndex(recipeList.GetSiblingIndex() + 1);

            InventoryGui.instance.m_playerGrid.m_elementPrefab*/


            //GUIFramework.GuiToggle
            //Texture tex;
            //UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.InstanceID).Where(img => img.mainTexture == tex && img.sprite.name == "selection_frame").Do(img => ZLog.Log($"{img.name} {img.sprite.textureRect} {Utils.GetPath(img.transform)}"))

            // Используем Hotkeyelement
            // формируем группы с фильтрами-сортировкой
            // нажатие левой - фильтр (желтый фон), нажатие правой - сортировка и фильтр (синий фон), геймпад - повторное нажатие
            // нажатие на заголовок группы - активирует по всем
            // три колонки
            // еда
            // оружие по скилам
            // боеприпасы: стрелы, болты, бомбы, наживки
            // шлем тело ноги
            // плащ, утилиты, тринки
        }

        internal static void FilterRecipes(List<Recipe> recipes)
        {

        }

        internal static void SortRecipes(List<Recipe> recipes)
        {

        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class StoreGui_Awake_InitializePanel
        {
            [HarmonyPriority(Priority.First)]
            public static void Postfix()
            {
                InitSortingPanel();
                UpdateVisibility();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        public static class InventoryGui_Update_SortingPanelsVisibility
        {
            [HarmonyPriority(Priority.First)]
            public static void Postfix()
            {
                UpdateVisibility();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
        public static class InventoryGui_UpdateRecipeList_SortingPanelsVisibility
        {
            [HarmonyPriority(Priority.First)]
            public static void Prefix(List<Recipe> recipes) => FilterRecipes(recipes);
            
            [HarmonyPriority(Priority.First)]
            public static void Postfix(List<Recipe> recipes) => SortRecipes(recipes);
        }
    }
}
