using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static MyLittleUI.MyLittleUI;
using static Skills;

namespace MyLittleUI
{
    public static class CraftSort
    {
        public class FilteringPanel
        {
            public string name;
            public RectTransform panel;
            public RectTransform selectedFrame;
            public List<FilteringState> filters;
            public bool enabled;
            public int enabledFilters;

            public override string ToString() => name;

            public FilteringPanel(string name)
            {
                this.name = name;
                
                panel = UnityEngine.Object.Instantiate(InventoryGui.instance.m_repairPanel as RectTransform, sortPanel);
                panel.name = name;

                selectedFrame = UnityEngine.Object.Instantiate(InventoryGui.instance.m_repairPanelSelection as RectTransform, InventoryGui.instance.m_repairPanelSelection.transform.parent);
                selectedFrame.name = $"selected (MLUI_Sorting_{name})";

                filters = new List<FilteringState>();

                panels.Add(this);
            }

            public void AddFilter(FilteringState filter) => filters.Add(filter);

            public void UpdatePosition(ref int height)
            {
                int lines = Mathf.CeilToInt(enabledFilters / 3f);
                int size = (32 + 2) * lines - 2 + 6 * 2;

                int x = 0;
                if (enabledFilters == 2)
                    x = 34;
                else if (enabledFilters == 1)
                    x = 34 * 2;

                panel.sizeDelta = new Vector2(120f - x, size);
                height += size;
                panel.anchoredPosition = new Vector2(0f, -height);

                selectedFrame.sizeDelta = panel.sizeDelta + new Vector2(4f, 4f);
                selectedFrame.anchoredPosition = panel.anchoredPosition - new Vector2(panel.sizeDelta.x + 2f, 200f + 2f);
            }

            public void UpdateVisibility()
            {
                panel.gameObject.SetActive(enabled);
                selectedFrame.gameObject.SetActive(enabled);
            }
        }

        public class FilteringState
        {
            public int position;
            public bool selectable;
            public bool enabled;
            public bool selected;
            public bool lastSelected;

            public string category;
            public string tooltip;
            public Sprite icon;
            public string name;
            public Color imageColor;
            public bool unique = true;

            public RectTransform element;
            public Button button;
            public Image image;
            public GameObject active;
            public GameObject select;

            public FilteringPanel panel;

            public Comparison<InventoryGui.RecipeDataPair> sort;
            public Func<ItemDrop.ItemData, bool> filter;

            public FilteringState()
            {
                filteringStates.Add(this);
            }

            public void CreateElement()
            {
                if (element)
                    return;

                element = UnityEngine.Object.Instantiate(elementPrefab, panel.panel);
                element.name = name;
                element.gameObject.SetActive(true);
                element.GetComponent<UITooltip>().m_text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Localization.instance.Localize(tooltip));
                image = element.Find("icon").GetComponent<Image>();
                if (image)
                {
                    image.overrideSprite = icon;
                    image.color = imageColor != Color.clear ? imageColor : Color.white;
                    image.transform.localScale *= 0.9f;
                }
                button = element.GetComponent<Button>();
                active = element.Find("active")?.gameObject;
                select = element.Find("selected")?.gameObject;

                button.onClick.AddListener(OnClick);

                UpdateElementPosition();
            }

            public void UpdateElementPosition()
            {
                element.anchoredPosition = new Vector2(8f + (position % 3) * (32f + 2f), -6f - (position / 3) * (32f + 2f));
            }

            public void UpdatePosition(ref int currentPosition)
            {
                position = currentPosition;
                if (selectable)
                    currentPosition++;

                UpdateElementPosition();
            }

            public void UpdateEnabled() => active.SetActive(enabled);

            public void UpdateSelectable() => element.gameObject.SetActive(selectable);

            public void UpdateSelect() => select.SetActive(selected);

            public void SetSelected(bool selected)
            {
                this.selected = selected;
                if (selected)
                {
                    lastSelected = true;
                    filteringStates.DoIf(fs => fs != this, fs => fs.lastSelected = false);
                }
            }

            public void ClearFiltering()
            {
                selectable = false;
                enabled = false;
                selected = false;
                position = 0;

                UpdateEnabled();
                UpdateSelectable();
                UpdateSelect();
            }

            public bool IsSelectable(ItemDrop.ItemData item)
            {
                if (selectable)
                    return selectable;

                return selectable = filter(item);
            }

            public void OnClick()
            {
                enabled = !enabled;
                UpdateEnabled();
                
                if (unique)
                    filteringStates.DoIf(fs => fs != this, fs => { fs.enabled = false; fs.UpdateEnabled(); });

                InventoryGui.instance.UpdateCraftingPanel(focusView: true);
            }
        }

        public static readonly List<FilteringState> filteringStates = new List<FilteringState>();
        public static readonly List<FilteringState> tempEnabledStates = new List<FilteringState>();
        public static readonly List<FilteringPanel> panels = new List<FilteringPanel>();

        public static GameObject parentObject;

        public static RectTransform sortPanel;
        public static RectTransform elementPrefab;

        public static Sprite foodSprite;

        public static bool IsCraftingFilterEnabled => modEnabled.Value && craftingSortingEnabled.Value && !AAA_Crafting && !ZenUI;

        public static void UpdateVisibility()
        {
            if (AAA_Crafting || ZenUI)
                return;

            sortPanel?.gameObject.SetActive(InventoryGui.IsVisible());
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
            elementPrefab.GetComponent<Button>().onClick.RemoveAllListeners();
        }

        internal static void InitSortingPanel()
        {
            if (AAA_Crafting || ZenUI)
                return;

            sortPanel = UnityEngine.Object.Instantiate(InventoryGui.instance.m_repairPanel as RectTransform, InventoryGui.instance.m_repairPanel.transform.parent);
            sortPanel.name = "MLUI_SortingPanels";
            sortPanel.SetSiblingIndex(InventoryGui.instance.m_repairPanel.GetSiblingIndex() + 1);

            sortPanel.anchoredPosition = new Vector2(0f, -200f);
            sortPanel.sizeDelta = new Vector2(0f, 0f);

            sortPanel.gameObject.SetActive(false);

            InitElementPrefab(sortPanel);

            InitFoodCategory();

            InitArmorCategory();

            InitSkillsCategory();

            InitBowsCategory();

            InitCrossbowsCategory();

            InitMagicCategory();

            InitToolsCategory();

            filteringStates.ForEach(fs => fs.CreateElement());
        }

        private static void InitFoodCategory()
        {
            FilteringPanel panel = new FilteringPanel("Food");

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "food",
                category = "$hud_food",
                tooltip = "$item_food_health",
                icon = foodSprite,
                imageColor = InventoryGui.instance.m_playerGrid.m_foodHealthColor,
                unique = true,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    float foodB = b.Recipe.m_item.m_itemData.m_shared.m_appendToolTip?.m_itemData.m_shared.m_food ?? b.Recipe.m_item.m_itemData.m_shared.m_food;
                    float foodA = a.Recipe.m_item.m_itemData.m_shared.m_appendToolTip?.m_itemData.m_shared.m_food ?? a.Recipe.m_item.m_itemData.m_shared.m_food;

                    return foodB.CompareTo(foodA);
                },
                filter = item => (item.m_shared.m_appendToolTip?.m_itemData.m_shared.m_food ?? item.m_shared.m_food) > 0,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "food_stamina",
                category = "$hud_food",
                tooltip = "$item_food_stamina",
                icon = foodSprite,
                imageColor = InventoryGui.instance.m_playerGrid.m_foodStaminaColor,
                unique = true,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    float foodB = b.Recipe.m_item.m_itemData.m_shared.m_appendToolTip?.m_itemData.m_shared.m_foodStamina ?? b.Recipe.m_item.m_itemData.m_shared.m_foodStamina;
                    float foodA = a.Recipe.m_item.m_itemData.m_shared.m_appendToolTip?.m_itemData.m_shared.m_foodStamina ?? a.Recipe.m_item.m_itemData.m_shared.m_foodStamina;

                    return foodB.CompareTo(foodA);
                },
                filter = item => (item.m_shared.m_appendToolTip?.m_itemData.m_shared.m_foodStamina ?? item.m_shared.m_foodStamina) > 0,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "food_eitr",
                category = "$hud_food",
                tooltip = "$item_food_eitr",
                icon = foodSprite,
                imageColor = InventoryGui.instance.m_playerGrid.m_foodEitrColor,
                unique = true,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    float foodB = b.Recipe.m_item.m_itemData.m_shared.m_appendToolTip?.m_itemData.m_shared.m_foodEitr ?? b.Recipe.m_item.m_itemData.m_shared.m_foodEitr;
                    float foodA = a.Recipe.m_item.m_itemData.m_shared.m_appendToolTip?.m_itemData.m_shared.m_foodEitr ?? a.Recipe.m_item.m_itemData.m_shared.m_foodEitr;

                    return foodB.CompareTo(foodA);
                },
                filter = item => (item.m_shared.m_appendToolTip?.m_itemData.m_shared.m_foodEitr ?? item.m_shared.m_foodEitr) > 0,
            });

            /*int size = 32 * 1 + 8 * 2 + height;

            panel.panel.sizeDelta = new Vector2(124f, size);
            height += size;
            panel.panel.anchoredPosition = new Vector2(0f, -height);*/
        }

        private static void InitArmorCategory()
        {
            FilteringPanel panel = new FilteringPanel("Armor");

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "armor_helmet",
                category = "Armor",
                tooltip = "$radial_armor_utility",
                icon = ObjectDB.instance.GetItemPrefab("HelmetBronze").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.GetArmor().CompareTo(a.Recipe.m_item.m_itemData.GetArmor());
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet || item.m_shared.m_attachOverride == ItemDrop.ItemData.ItemType.Helmet,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "armor_chest",
                category = "Armor",
                tooltip = "$radial_armor_utility",
                icon = ObjectDB.instance.GetItemPrefab("ArmorIronChest").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.GetArmor().CompareTo(a.Recipe.m_item.m_itemData.GetArmor());
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest || item.m_shared.m_attachOverride == ItemDrop.ItemData.ItemType.Chest,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "armor_legs",
                category = "Armor",
                tooltip = "$radial_armor_utility",
                icon = ObjectDB.instance.GetItemPrefab("ArmorIronLegs").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.GetArmor().CompareTo(a.Recipe.m_item.m_itemData.GetArmor());
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs || item.m_shared.m_attachOverride == ItemDrop.ItemData.ItemType.Legs,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "armor_cape",
                category = "Armor",
                tooltip = "$radial_armor_utility",
                icon = ObjectDB.instance.GetItemPrefab("CapeDeerHide").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.GetMaxDurability().CompareTo(a.Recipe.m_item.m_itemData.GetMaxDurability());
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder || item.m_shared.m_attachOverride == ItemDrop.ItemData.ItemType.Shoulder,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "armor_utility",
                category = "Armor",
                tooltip = "$radial_armor_utility",
                icon = ObjectDB.instance.GetItemPrefab("Demister").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.GetArmor().CompareTo(a.Recipe.m_item.m_itemData.GetArmor());
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility || item.m_shared.m_attachOverride == ItemDrop.ItemData.ItemType.Utility,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "armor_trinket",
                category = "Armor",
                tooltip = "$radial_armor_utility",
                icon = ObjectDB.instance.GetItemPrefab("TrinketBronzeHealth").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.m_shared.m_maxAdrenaline.CompareTo(a.Recipe.m_item.m_itemData.m_shared.m_maxAdrenaline);
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trinket || item.m_shared.m_attachOverride == ItemDrop.ItemData.ItemType.Trinket,
            });

            /*int size = 32 * 2 + 8 * 2 + 2;

            panel.panel.sizeDelta = new Vector2(124f, size);
            height += size;
            panel.panel.anchoredPosition = new Vector2(0f, -height);*/
        }

        private static void InitSkillsCategory()
        {
            FilteringPanel panel = new FilteringPanel("Skills");

            Skills skills = Player.m_localPlayer.GetSkills();

            AddMelee(skills, SkillType.Swords);
            AddMelee(skills, SkillType.Knives);
            AddMelee(skills, SkillType.Clubs);
            AddMelee(skills, SkillType.Polearms);
            AddMelee(skills, SkillType.Spears);
            AddMelee(skills, SkillType.Axes);
            AddMelee(skills, SkillType.Unarmed);
            AddMelee(skills, SkillType.Pickaxes);

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "skill_shields",
                category = "Melee",
                tooltip = "$skill_shields",
                icon = skills.GetSkillDef(SkillType.Blocking).m_icon,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.GetBaseBlockPower().CompareTo(a.Recipe.m_item.m_itemData.GetBaseBlockPower());
                },
                filter = item => item.m_shared.m_skillType == SkillType.Blocking,
            });

            /*int size = 32 * 3 + 8 * 2 + 2 + 2;

            panel.panel.sizeDelta = new Vector2(124f, size);
            height += size;
            panel.panel.anchoredPosition = new Vector2(0f, -height);*/

            void AddMelee(Skills skills, SkillType skill)
            {
                panel.AddFilter(new FilteringState()
                {
                    panel = panel,
                    name = $"skill_{skill.ToString().ToLower()}",
                    category = "Melee",
                    tooltip = $"$skill_{skill.ToString().ToLower()}",
                    icon = skills.GetSkillDef(skill).m_icon,

                    sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                    {
                        return b.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage().CompareTo(a.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage());
                    },
                    filter = item => item.m_shared.m_skillType == skill && item.m_shared.m_attack.m_attackAnimation != "" && item.m_shared.m_damages.GetTotalDamage() > 0,
                });
            }
        }

        private static void InitBowsCategory()
        {
            FilteringPanel panel = new FilteringPanel("Bows");

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "skill_bows",
                category = "Bows",
                tooltip = "$skill_bows",
                icon = Player.m_localPlayer.GetSkills().GetSkillDef(SkillType.Bows).m_icon,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage().CompareTo(a.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage());
                },
                filter = item => item.m_shared.m_skillType == SkillType.Bows && item.m_shared.m_attack.m_attackAnimation != "" && item.m_shared.m_damages.GetTotalDamage() > 0,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "ammo_arrows",
                category = "Bows",
                tooltip = "$ammo_arrows",
                icon = ObjectDB.instance.GetItemPrefab("ArrowIron").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage().CompareTo(a.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage());
                },
                filter = item => item.m_shared.m_skillType == SkillType.Bows && item.m_shared.m_attack.m_attackAnimation == "" && item.m_shared.m_ammoType == "$ammo_arrows" && item.m_shared.m_damages.GetTotalDamage() > 0,
            });
        }

        private static void InitCrossbowsCategory()
        {
            FilteringPanel panel = new FilteringPanel("Crossbows");

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "skill_crossbows",
                category = "Crossbows",
                tooltip = "$skill_crossbows",
                icon = Player.m_localPlayer.GetSkills().GetSkillDef(SkillType.Crossbows).m_icon,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage().CompareTo(a.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage());
                },
                filter = item => item.m_shared.m_skillType == SkillType.Crossbows && item.m_shared.m_attack.m_attackAnimation != "" && item.m_shared.m_damages.GetTotalDamage() > 0,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "ammo_arrows",
                category = "Crossbows",
                tooltip = "$ammo_bolts",
                icon = ObjectDB.instance.GetItemPrefab("BoltIron").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage().CompareTo(a.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage());
                },
                filter = item => item.m_shared.m_skillType == SkillType.Crossbows && item.m_shared.m_attack.m_attackAnimation == "" && item.m_shared.m_ammoType == "$ammo_bolts" && item.m_shared.m_damages.GetTotalDamage() > 0,
            });
        }

        private static void InitMagicCategory()
        {
            FilteringPanel panel = new FilteringPanel("Magic");

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "skill_elementalmagic",
                category = "Magic",
                tooltip = "$skill_elementalmagic",
                icon = Player.m_localPlayer.GetSkills().GetSkillDef(SkillType.ElementalMagic).m_icon,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage().CompareTo(a.Recipe.m_item.m_itemData.m_shared.m_damages.GetTotalDamage());
                },
                filter = item => item.m_shared.m_skillType == SkillType.ElementalMagic && item.m_shared.m_attack.m_attackAnimation != "" && item.m_shared.m_damages.GetTotalDamage() > 0,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "skill_bloodmagic",
                category = "Magic",
                tooltip = "$skill_bloodmagic",
                icon = Player.m_localPlayer.GetSkills().GetSkillDef(SkillType.BloodMagic).m_icon,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return b.Recipe.m_item.m_itemData.m_shared.m_attack.m_drawEitrDrain.CompareTo(a.Recipe.m_item.m_itemData.m_shared.m_attack.m_drawEitrDrain);
                },
                filter = item => item.m_shared.m_skillType == SkillType.BloodMagic && item.m_shared.m_attack.m_attackAnimation != "",
            });
        }

        private static void InitToolsCategory()
        {
            FilteringPanel panel = new FilteringPanel("Tools");

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "tools_crafting",
                category = "Tools",
                tooltip = "$radial_weapons_tools",
                icon = Player.m_localPlayer.GetSkills().GetSkillDef(SkillType.Crafting).m_icon,

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return a.Recipe.m_listSortWeight.CompareTo(b.Recipe.m_listSortWeight);
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || item.m_shared.m_attack.m_attackAnimation == "throw_bomb",
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "tools_consumables",
                category = "Tools",
                tooltip = "$radial_consumables",
                icon = ObjectDB.instance.GetItemPrefab("MeadBaseTasty").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return a.Recipe.m_listSortWeight.CompareTo(b.Recipe.m_listSortWeight);
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && item.m_shared.m_consumeStatusEffect != null,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "tools_material",
                category = "Tools",
                tooltip = "$skill_crafting",
                icon = ObjectDB.instance.GetItemPrefab("Bronze").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return a.Recipe.m_listSortWeight.CompareTo(b.Recipe.m_listSortWeight);
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material && item.m_shared.m_appendToolTip == null && item.m_shared.m_consumeStatusEffect == null,
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "skill_fishing",
                category = "Tools",
                tooltip = "$skill_fishing",
                icon = ObjectDB.instance.GetItemPrefab("FishingRod").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return a.Recipe.m_listSortWeight.CompareTo(b.Recipe.m_listSortWeight);
                },
                filter = item => item.m_shared.m_skillType == SkillType.Fishing || item.m_shared.m_name == "$item_helmet_fishinghat" || item.m_shared.m_ammoType == "$item_fishingbait",
            });

            panel.AddFilter(new FilteringState()
            {
                panel = panel,
                name = "tools_misc",
                category = "Tools",
                tooltip = "$hud_misc",
                icon = ObjectDB.instance.GetItemPrefab("BoneFragments").GetComponent<ItemDrop>().m_itemData.GetIcon(),

                sort = delegate (InventoryGui.RecipeDataPair a, InventoryGui.RecipeDataPair b)
                {
                    return a.Recipe.m_listSortWeight.CompareTo(b.Recipe.m_listSortWeight);
                },
                filter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Misc || item.m_shared.m_attachOverride == ItemDrop.ItemData.ItemType.Misc || (int)item.m_shared.m_itemType > 25,
            });
        }

        internal static void FilterRecipes(List<Recipe> recipes)
        {
            tempEnabledStates.Clear();

            int height = 0;
            foreach (var panel in panels)
            {
                int position = 0;
                panel.enabled = false;
                panel.enabledFilters = 0;

                foreach (var state in panel.filters)
                {
                    state.selectable = recipes.Any(recipe => state.IsSelectable(recipe.m_item.m_itemData));
                    state.enabled = state.enabled && state.selectable;
                    state.UpdateSelectable();

                    if (state.enabled)
                        tempEnabledStates.Add(state);

                    state.UpdatePosition(ref position);

                    if (state.selectable)
                    {
                        panel.enabledFilters++;
                        panel.enabled = true;
                    }
                }

                if (panel.enabled)
                    panel.UpdatePosition(ref height);

                panel.UpdateVisibility();
            }

            if (tempEnabledStates.Count > 0)
                recipes.RemoveAll(recipe =>
                {
                    bool pass = false;
                    foreach (var state in tempEnabledStates)
                    {
                        if (state.filter(recipe.m_item.m_itemData))
                        {
                            pass = true;
                            break;
                        }
                    }
                    return !pass;
                });
        }

        internal static void SortRecipes(List<InventoryGui.RecipeDataPair> m_availableRecipes, float m_recipeListSpace)
        {
            if (tempEnabledStates.Count == 0)
                return;

            m_availableRecipes.Sort((a, b) =>
            {
                foreach (var sorter in tempEnabledStates)
                {
                    int result = sorter.sort(a, b);
                    if (result != 0)
                        return result;
                }
                return 0;
            });

            for (int j = 0; j < m_availableRecipes.Count; j++)
                (m_availableRecipes[j].InterfaceElement.transform as RectTransform).anchoredPosition = new Vector2(0f, j * (0f - m_recipeListSpace));
        }

        internal static void ClearStates() => filteringStates.Do(state => state.ClearFiltering());

        [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
        public static class Game_SpawnPlayer_InitializePanel
        {
            public static void Postfix()
            {
                InitSortingPanel();
                UpdateVisibility();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        public static class InventoryGui_Update_SortingPanelsVisibility
        {
            public static void Postfix()
            {
                UpdateVisibility();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        public static class InventoryGui_Show_ClearState
        {
            public static void Prefix() => ClearStates();
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
        public static class InventoryGui_UpdateRecipeList_SortingPanelsVisibility
        {
            [HarmonyPriority(Priority.First)]
            public static void Prefix(List<Recipe> recipes) => FilterRecipes(recipes);
            
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(InventoryGui __instance) => SortRecipes(__instance.m_availableRecipes, __instance.m_recipeListSpace);
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeGamepadInput))]
        public static class InventoryGui_UpdateRecipeGamepadInput_GamepadControls
        {
            public static FilteringState GetSelectedFilter() => filteringStates.FirstOrDefault(fs => fs.selectable && fs.selected);

            public static bool Prefix()
            {
                if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyLStickRight"))
                {
                    filteringStates.Do(fs => fs.SetSelected(false));
                    return true;
                }

                if (ZInput.GetButtonDown("JoyLStickLeft"))
                {
                    if (filteringStates.FirstOrDefault(fs => fs.selectable && fs.enabled) is FilteringState enabledFilter)
                    {
                        enabledFilter.SetSelected(true);
                        return false;
                    }
                    else if (panels.Count > 0)
                    {
                        var selectedFilter = GetSelectedFilter();
                        selectedFilter = filteringStates.FirstOrDefault(fs => fs.selectable && fs.lastSelected) ?? filteringStates.FirstOrDefault(fs => fs.selectable);
                        if (selectedFilter != null)
                        {
                            selectedFilter.SetSelected(true);
                            return false;
                        }
                    }
                }
                else if (ZInput.GetButtonDown("JoyDPadLeft"))
                {
                    var selectedFilter = GetSelectedFilter();
                    if (selectedFilter != null)
                    {
                        for (var i = selectedFilter.panel.filters.IndexOf(selectedFilter) - 1; i >= 0; i--)
                            if (selectedFilter.panel.filters[i].selectable)
                            {
                                selectedFilter.SetSelected(false);
                                selectedFilter.panel.filters[i].SetSelected(true);
                                return false;
                            }

                        int panelIndex = panels.IndexOf(selectedFilter.panel);
                        for (int j = panelIndex - 1; j >= 0; j--)
                        {
                            var filter = panels[j].filters.LastOrDefault(fs => fs.selectable);
                            if (filter != null)
                            {
                                selectedFilter.SetSelected(false);
                                filter.SetSelected(true);
                                return false;
                            }
                        }

                        if (filteringStates.LastOrDefault(fs => fs.selectable) is FilteringState lastFilter)
                        {
                            selectedFilter.SetSelected(false);
                            lastFilter.SetSelected(true);
                            return false;
                        }
                    }
                    else if (filteringStates.FirstOrDefault(fs => fs.selectable && fs.enabled) is FilteringState enabledFilter)
                    {
                        enabledFilter.SetSelected(true);
                        return false;
                    }
                    else if (panels.Count > 0)
                    {
                        selectedFilter = filteringStates.FirstOrDefault(fs => fs.selectable && fs.lastSelected) ?? filteringStates.FirstOrDefault(fs => fs.selectable);
                        if (selectedFilter != null)
                        {
                            selectedFilter.SetSelected(true);
                            return false;
                        }
                    }
                }
                else if (ZInput.GetButtonDown("JoyDPadRight"))
                {
                    var selectedFilter = GetSelectedFilter();
                    if (selectedFilter != null)
                    {
                        for (var i = selectedFilter.panel.filters.IndexOf(selectedFilter) + 1; i < selectedFilter.panel.filters.Count; i++)
                            if (selectedFilter.panel.filters[i].selectable)
                            {
                                selectedFilter.SetSelected(false);
                                selectedFilter.panel.filters[i].SetSelected(true);
                                return false;
                            }

                        int panelIndex = panels.IndexOf(selectedFilter.panel);
                        for (int j = panelIndex + 1; j < panels.Count; j++)
                        {
                            var filter = panels[j].filters.FirstOrDefault(fs => fs.selectable);
                            if (filter != null)
                            {
                                selectedFilter.SetSelected(false);
                                filter.SetSelected(true);
                                return false;
                            }
                        }

                        if (filteringStates.FirstOrDefault(fs => fs.selectable) is FilteringState firstFilter)
                        {
                            selectedFilter.SetSelected(false);
                            firstFilter.SetSelected(true);
                            return false;
                        }
                    }
                }
                else if (ZInput.GetButtonDown("JoyDPadUp"))
                {
                    var selectedFilter = GetSelectedFilter();
                    if (selectedFilter != null)
                    {
                        int skipped = 0;
                        for (int i = selectedFilter.panel.filters.IndexOf(selectedFilter); i >= 0; i--)
                            if (selectedFilter.panel.filters[i].selectable)
                            {
                                if (skipped >= 3)
                                {
                                    selectedFilter.SetSelected(false);
                                    selectedFilter.panel.filters[i].SetSelected(true);
                                    return false;
                                }
                                skipped++;
                            }

                        int panelIndex = panels.IndexOf(selectedFilter.panel);
                        for (int i = panelIndex - 1; i >= 0; i--)
                        {
                            var list = selectedFilter.panel.filters.Where(fs => fs.selectable).ToList();
                            int column = list.IndexOf(selectedFilter) % 3;
                            if (list.Count == 2)
                                column++;
                            else if (list.Count == 1)
                                column += 2;

                            list = panels[i].filters.Where(fs => fs.selectable).ToList();
                            if (list.Count == 2)
                                column = Math.Max(0, column - 1);
                            else if (list.Count == 1)
                                column = Math.Max(0, column - 2);

                            var filter = list.LastOrDefault(fs => list.IndexOf(fs) % 3 == column) ?? panels[i].filters.LastOrDefault(fs => fs.selectable);

                            if (filter != null)
                            {
                                selectedFilter.SetSelected(false);
                                filter.SetSelected(true);
                                return false;
                            }
                        }
                    }
                }
                else if (ZInput.GetButtonDown("JoyDPadDown"))
                {
                    var selectedFilter = GetSelectedFilter();
                    if (selectedFilter != null)
                    {
                        int skipped = 0;
                        for (int i = selectedFilter.panel.filters.IndexOf(selectedFilter); i < selectedFilter.panel.filters.Count; i++)
                            if (selectedFilter.panel.filters[i].selectable)
                            {
                                if (skipped >= 3)
                                {
                                    selectedFilter.SetSelected(false);
                                    selectedFilter.panel.filters[i].SetSelected(true);
                                    return false;
                                }
                                skipped++;
                            }

                        int panelIndex = panels.IndexOf(selectedFilter.panel);
                        for (int i = panelIndex + 1; i < panels.Count; i++)
                        {
                            var list = selectedFilter.panel.filters.Where(fs => fs.selectable).ToList();
                            int column = list.IndexOf(selectedFilter) % 3;
                            if (list.Count == 2)
                                column++;
                            else if (list.Count == 1)
                                column += 2;

                            list = panels[i].filters.Where(fs => fs.selectable).ToList();
                            if (list.Count == 2)
                                column = Math.Max(0, column - 1);
                            else if (list.Count == 1)
                                column = Math.Max(0, column - 2);

                            var filter = list.FirstOrDefault(fs => list.IndexOf(fs) % 3 == column) ?? panels[i].filters.FirstOrDefault(fs => fs.selectable);
                            if (filter != null)
                            {
                                selectedFilter.SetSelected(false);
                                filter.SetSelected(true);
                                return false;
                            }
                        }
                    }
                }
                else if (ZInput.GetButtonDown("JoyRStick"))
                {
                    filteringStates.DoIf(fs => fs.selected, fs => fs.OnClick());
                    return false;
                }
                else if (ZInput.GetButtonDown("JoyJump"))
                {
                    filteringStates.DoIf(fs => fs.selected, fs => fs.OnClick());
                    return false;
                }

                return true;
            }

            public static void Postfix() => filteringStates.Do(fs => { fs.UpdateSelect(); });
        }
    }
}
