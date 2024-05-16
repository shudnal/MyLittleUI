using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyLittleUI
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    [BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("randyknapp.mods.auga")]
    public class MyLittleUI : BaseUnityPlugin
    {
        const string pluginID = "shudnal.MyLittleUI";
        const string pluginName = "My Little UI";
        const string pluginVersion = "1.0.7";

        private Harmony _harmony;

        public static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> loggingEnabled;

        private static ConfigEntry<bool> showAvailableItemsAmount;
        private static ConfigEntry<Color> availableItemsAmountColor;

        public static ConfigEntry<bool> durabilityEnabled;
        public static ConfigEntry<Color> durabilityFine;
        public static ConfigEntry<Color> durabilityWorn;
        public static ConfigEntry<Color> durabilityAtRisk;
        public static ConfigEntry<Color> durabilityBroken;

        public static ConfigEntry<float> itemIconScale;

        public static ConfigEntry<bool> itemTooltip;
        public static ConfigEntry<bool> itemTooltipColored;

        public static ConfigEntry<bool> itemQuality;
        public static ConfigEntry<string> itemQualitySymbol;
        public static ConfigEntry<Color> itemQualitySymbolColor;
        public static ConfigEntry<float> itemQualitySymbolSize;
        public static ConfigEntry<int> itemQualityMax;
        public static ConfigEntry<int> itemQualityRows;
        public static ConfigEntry<int> itemQualityColumns;
        public static ConfigEntry<float> itemQualityLineSpacing;
        public static ConfigEntry<float> itemQualityCharacterSpacing;

        private static ConfigEntry<float> inventoryOpenCloseAnimationSpeed;

        private static ConfigEntry<bool> statsMainMenu;
        private static ConfigEntry<bool> statsMainMenuAdvanced;
        private static ConfigEntry<bool> statsMainMenuAll;

        public static ConfigEntry<bool> statsCharacterArmor;
        public static ConfigEntry<bool> statsCharacterEffects;
        public static ConfigEntry<bool> statsCharacterEffectsMagic;

        private static ConfigEntry<StationHover> hoverFermenter;
        private static ConfigEntry<StationHover> hoverPlant;
        private static ConfigEntry<StationHover> hoverCooking;
        private static ConfigEntry<StationHover> hoverBeeHive;
        private static ConfigEntry<bool> hoverBeeHiveTotal;

        public static ConfigEntry<StationHover> hoverCharacter;
        public static ConfigEntry<bool> hoverCharacterGrowth;
        public static ConfigEntry<bool> hoverCharacterProcreation;
        public static ConfigEntry<bool> hoverCharacterEggGrow;

        private static ConfigEntry<StationHover> hoverTame;
        private static ConfigEntry<bool> hoverTameTimeToTame;
        private static ConfigEntry<bool> hoverTameTimeToFed;

        private static ConfigEntry<bool> hoverSmelterEstimatedTime;
        private static ConfigEntry<bool> hoverSmelterShowFuelAndItem;
        private static ConfigEntry<bool> hoverSmelterShowQueuedItems;

        internal static ConfigEntry<ChestItemsHover> chestHoverItems;
        internal static ConfigEntry<ChestNameHover> chestHoverName;
        internal static ConfigEntry<bool> chestCustomName;
        internal static ConfigEntry<bool> chestShowHoldToStack;
        internal static ConfigEntry<bool> chestShowRename;

        internal static ConfigEntry<bool> chestContentEnabled;
        internal static ConfigEntry<int> chestContentLinesToShow;
        internal static ConfigEntry<ContentSortType> chestContentSortType;
        internal static ConfigEntry<ContentSortDir> chestContentSortDir;
        internal static ConfigEntry<string> chestContentEntryFormat;
        internal static ConfigEntry<Color> chestContentItemColor;
        internal static ConfigEntry<Color> chestContentAmountColor;

        public static ConfigEntry<bool> statusEffectsPositionEnabled;
        public static ConfigEntry<Vector2> statusEffectsPositionAnchor;
        public static ConfigEntry<StatusEffectDirection> statusEffectsFillingDirection;
        public static ConfigEntry<int> statusEffectsPositionSpacing;

        public static ConfigEntry<bool> statusEffectsElementEnabled;
        public static ConfigEntry<int> statusEffectsElementSize;

        public static ConfigEntry<bool> statusEffectsPositionEnabledNomap;
        public static ConfigEntry<Vector2> statusEffectsPositionAnchorNomap;
        public static ConfigEntry<StatusEffectDirection> statusEffectsFillingDirectionNomap;
        public static ConfigEntry<int> statusEffectsPositionSpacingNomap;

        public static ConfigEntry<bool> statusEffectsElementEnabledNomap;
        public static ConfigEntry<int> statusEffectsElementSizeNomap;

        public static ConfigEntry<bool> sailingIndicatorEnabled;
        public static ConfigEntry<Vector2> sailingIndicatorPowerIconPosition;
        public static ConfigEntry<float> sailingIndicatorPowerIconScale;
        public static ConfigEntry<Vector2> sailingIndicatorWindIndicatorPosition;
        public static ConfigEntry<float> sailingIndicatorWindIndicatorScale;

        public static ConfigEntry<bool> sailingIndicatorEnabledNomap;
        public static ConfigEntry<Vector2> sailingIndicatorPowerIconPositionNomap;
        public static ConfigEntry<float> sailingIndicatorPowerIconScaleNomap;
        public static ConfigEntry<Vector2> sailingIndicatorWindIndicatorPositionNomap;
        public static ConfigEntry<float> sailingIndicatorWindIndicatorScaleNomap;

        private static readonly Dictionary<string, string> characterNames = new Dictionary<string, string>();

        private static MyLittleUI instance;

        public static Component epicLootPlugin;

        public enum StationHover
        {
            Vanilla,
            Percentage,
            MinutesSeconds
        }

        public enum ChestItemsHover
        {
            Vanilla,
            Percentage,
            ItemsMaxRoom,
            FreeSlots
        }

        public enum ChestNameHover
        {
            Vanilla,
            CustomName,
            TypeThenCustomName,
            CustomNameThenType
        }

        public enum StatusEffectDirection
        {
            RightToLeft,
            LeftToRight,
            TopToBottom,
            BottomToTop
        }

        public enum ContentSortType
        {
            Position,
            Name,
            Weight,
            Amount,
            Value
        }

        public enum ContentSortDir
        {
            Asc,
            Desc
        }

        private void Awake()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Logger.LogWarning("Dedicated server. Loading skipped.");
                return;
            }

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), pluginID);

            instance = this;

            ConfigInit();

            epicLootPlugin = GetComponent("EpicLoot");

            ItemTooltip.Initialize();

            Game.isModded = true;
        }

        private void OnDestroy()
        {
            Config.Save();
            _harmony?.UnpatchSelf();
        }

        public static void LogInfo(object data)
        {
            if (loggingEnabled.Value)
                instance.Logger.LogInfo(data);
        }

        private void ConfigInit()
        {
            Config.Bind("General", "NexusID", 2562, "Nexus mod ID for updates");

            modEnabled = Config.Bind("General", "Enabled", defaultValue: true, "Enable the mod.");
            loggingEnabled = Config.Bind("General", "Logging enabled", defaultValue: false, "Enable logging.");

            showAvailableItemsAmount = Config.Bind("Item - Available resources amount", "Enabled", defaultValue: true, "Show amount of available resources for crafting in requirements list");
            availableItemsAmountColor = Config.Bind("Item - Available resources amount", "Color", defaultValue: new Color(0.68f, 0.85f, 0.90f), "Color of amount of available resources.");

            durabilityEnabled = Config.Bind("Item - Durability", "0 - Enabled", defaultValue: true, "Enable color of durability.");
            durabilityFine = Config.Bind("Item - Durability", "1 - Fine", defaultValue: new Color(0.11765f, 0.72941f, 0.03529f, 1f), "Color of durability > 75%.");
            durabilityWorn = Config.Bind("Item - Durability", "2 - Worn", defaultValue: new Color(0.72941f, 0.72941f, 0.03529f, 1f), "Color of durability > 50%.");
            durabilityAtRisk = Config.Bind("Item - Durability", "3 - At risk", defaultValue: new Color(0.72941f, 0.34902f, 0.03529f, 1f), "Color of durability > 25%.");
            durabilityBroken = Config.Bind("Item - Durability", "4 - Broken", defaultValue: new Color(0.72941f, 0.03529f, 0.03529f, 1f), "Color of durability >= 0%.");

            itemIconScale = Config.Bind("Item - Icon", "Icon scale", defaultValue: 1.0f, "Relative scale size of item icons.");

            itemTooltip = Config.Bind("Item - Tooltip", "Enabled", defaultValue: true, "Updated item tooltip. Hold Alt to see original tooltip");
            itemTooltipColored = Config.Bind("Item - Tooltip", "Colored numbers", defaultValue: true, "Orange and yellow value numbers in tooltip, light blue if disabled");

            itemQuality = Config.Bind("Item - Quality", "Enabled", defaultValue: false, "Show item quality as symbol");
            itemQualitySymbol = Config.Bind("Item - Quality", "Symbol", defaultValue: "★", "Symbol to show.");
            itemQualitySymbolColor = Config.Bind("Item - Quality", "Symbol Color", defaultValue: new Color(1f, 0.65f, 0f, 1f), "Symbol color");
            itemQualitySymbolSize = Config.Bind("Item - Quality", "Symbol Size", defaultValue: 10f, "Symbol size");
            itemQualityMax = Config.Bind("Item - Quality", "Maximum symbols", defaultValue: 8, "Maximum amount of symbols to show.");
            itemQualityRows = Config.Bind("Item - Quality", "Maximum rows", defaultValue: 2, "Maximum amount of rows to show.");
            itemQualityColumns = Config.Bind("Item - Quality", "Maximum columns", defaultValue: 4, "Maximum amount of columns to show.");
            itemQualityLineSpacing = Config.Bind("Item - Quality", "Space between lines", defaultValue: -35.0f, "Line spacing.");
            itemQualityCharacterSpacing = Config.Bind("Item - Quality", "Space between characters", defaultValue: 8f, "Character spacing.");

            itemQualitySymbol.SettingChanged += (sender, args) => itemQualitySymbol.Value = itemQualitySymbol.Value[0].ToString();

            itemQualitySymbol.SettingChanged += (sender, args) => ItemIcon.FillItemQualityCache();
            itemQualityMax.SettingChanged += (sender, args) => ItemIcon.FillItemQualityCache();
            itemQualityRows.SettingChanged += (sender, args) => ItemIcon.FillItemQualityCache();
            itemQualityColumns.SettingChanged += (sender, args) => ItemIcon.FillItemQualityCache();

            ItemIcon.FillItemQualityCache();

            inventoryOpenCloseAnimationSpeed = Config.Bind("Inventory", "Animation speed", defaultValue: 1f, "Inventory show/close animation speed");

            inventoryOpenCloseAnimationSpeed.SettingChanged += (sender, args) => SetInventoryAnimationSpeed();

            statsMainMenu = Config.Bind("Stats - Main menu", "Show stats in main menu", defaultValue: true, "Show character statistics in main menu");
            statsMainMenuAdvanced = Config.Bind("Stats - Main menu", "Show advanced stats in main menu", defaultValue: true, "Show advanced character statistics in main menu");
            statsMainMenuAll = Config.Bind("Stats - Main menu", "Show all stats in main menu", defaultValue: false, "Show all character statistics in main menu");

            statsCharacterArmor = Config.Bind("Stats - Character", "Show character stats on armor hover", defaultValue: true, "Show character stats in armor tooltip");
            statsCharacterEffects = Config.Bind("Stats - Character", "Show character active effects on weight hover", defaultValue: true, "Show character active effects in weight tooltip");
            statsCharacterEffectsMagic = Config.Bind("Stats - Character", "Show character active magic effects (EpicLoot) on weight hover", defaultValue: true, "Show character active magic effects in weight tooltip");

            statsCharacterArmor.SettingChanged += (sender, args) => InventoryCharacterStats.UpdateTooltipState();
            statsCharacterEffects.SettingChanged += (sender, args) => InventoryCharacterStats.UpdateTooltipState();

            hoverCharacter = Config.Bind("Hover - Character", "Character Hover", defaultValue: StationHover.Vanilla, "Format of baby development's total needed time/percent.");
            hoverCharacterGrowth = Config.Bind("Hover - Character", "Show baby growth", true, "Show total growth percentage/remaining for babies.");
            hoverCharacterProcreation = Config.Bind("Hover - Character", "Show offspring", true, "Show percentage/remaining for new offspring.");
            hoverCharacterEggGrow = Config.Bind("Hover - Character", "Show egg hatching", true, "Show percentage/remaining for egg hatching.");

            hoverFermenter = Config.Bind("Hover - Stations", "Fermenter Hover", defaultValue: StationHover.Vanilla, "Hover text for fermenter.");
            hoverPlant = Config.Bind("Hover - Stations", "Plants Hover", defaultValue: StationHover.Vanilla, "Hover text for plants.");
            hoverCooking = Config.Bind("Hover - Stations", "Cooking stations Hover", defaultValue: StationHover.Vanilla, "Hover text for cooking stations.");
            hoverBeeHive = Config.Bind("Hover - Stations", "Bee Hive Hover", defaultValue: StationHover.Vanilla, "Hover text for bee hive.");
            hoverBeeHiveTotal = Config.Bind("Hover - Stations", "Bee Hive Show total", defaultValue: true, "Show total needed time/percent for bee hive.");

            hoverTame = Config.Bind("Hover - Tameable", "Tameable Hover", defaultValue: StationHover.Vanilla, "Format of total needed time/percent to tame or to stay fed.");
            hoverTameTimeToTame = Config.Bind("Hover - Tameable", "Show time to tame", defaultValue: true, "Show total needed time/percent to tame.");
            hoverTameTimeToFed = Config.Bind("Hover - Tameable", "Show time to stay fed", defaultValue: true, "Show total needed time/percent to stay fed.");

            hoverSmelterEstimatedTime = Config.Bind("Hover - Smelters", "Show estimated time", defaultValue: true, "Show estimated end time for a smelter station (charcoal kiln, forge, etc. including non vanilla).");
            hoverSmelterShowFuelAndItem = Config.Bind("Hover - Smelters", "Always show fuel and item", defaultValue: true, "Show current smelting item and fuel loaded on both fuel and ore switches.");
            hoverSmelterShowQueuedItems = Config.Bind("Hover - Smelters", "Show queued items", defaultValue: true, "Show queued items currently being smelted. Doesn't show the list if there is only one item to smelt.");

            chestCustomName = Config.Bind("Hover - Chests", "Enable custom names", defaultValue: true, "Enable custom names for chests.");
            chestHoverItems = Config.Bind("Hover - Chests", "Hover items format", defaultValue: ChestItemsHover.Vanilla, "Chest items details format to be shown in hover.");
            chestHoverName = Config.Bind("Hover - Chests", "Hover name format", defaultValue: ChestNameHover.TypeThenCustomName, "Chest name format to be shown in hover.");
            chestShowRename = Config.Bind("Hover - Chests", "Show rename hint in hover", defaultValue: true, "Show rename hotkey hint. You can hide it to make it less noisy.");
            chestShowHoldToStack = Config.Bind("Hover - Chests", "Show hold to stack hint in hover", defaultValue: true, "Show hold to stack hint. You can hide it to make it less noisy.");

            chestContentEnabled = Config.Bind("Hover - Chests - Content", "Enable chest content", defaultValue: true, "Enable custom names for chests.");
            chestContentLinesToShow = Config.Bind("Hover - Chests - Content", "Lines to show", defaultValue: 11, "Amount of lines to be shown.");
            chestContentSortType = Config.Bind("Hover - Chests - Content", "Sorting type", defaultValue: ContentSortType.Position, "Sorting type. Position means item position in chest grid.");
            chestContentSortDir = Config.Bind("Hover - Chests - Content", "Sorting direction", defaultValue: ContentSortDir.Asc, "Sorting direction.");
            chestContentEntryFormat = Config.Bind("Hover - Chests - Content", "Entry format", defaultValue: "{1} {0}", "0 for item name, 1 for total amount");
            chestContentAmountColor = Config.Bind("Hover - Chests - Content", "Entry amount color", defaultValue: new Color(1f, 1f, 0f, 0.6f), "Color for amount");
            chestContentItemColor = Config.Bind("Hover - Chests - Content", "Entry item name color", defaultValue: new Color(0.75f, 0.75f, 0.75f, 0.6f), "Color for item name");

            chestContentEnabled.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentLinesToShow.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentSortType.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentSortDir.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentEntryFormat.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentItemColor.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentAmountColor.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();

            statusEffectsPositionEnabled = Config.Bind("Status effects - Map - List", "Enable", defaultValue: true, "Enable repositioning of status effect list.");
            statusEffectsPositionAnchor = Config.Bind("Status effects - Map - List", "Position", defaultValue: new Vector2(-170f, -240f), "Anchored position of list.");
            statusEffectsFillingDirection = Config.Bind("Status effects - Map - List", "Direction", defaultValue: StatusEffectDirection.TopToBottom, "Direction of filling");
            statusEffectsPositionSpacing = Config.Bind("Status effects - Map - List", "Spacing", defaultValue: 8, "Spacing between status effects");

            statusEffectsElementEnabled = Config.Bind("Status effects - Map - List element", "Custom element enabled", defaultValue: true, "Enables using of horizontal status effect element");
            statusEffectsElementSize = Config.Bind("Status effects - Map - List element", "Size", defaultValue: 32, "Vertical capsule size");

            modEnabled.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsPositionEnabled.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsPositionAnchor.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsElementEnabled.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsElementSize.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();

            sailingIndicatorEnabled = Config.Bind("Status effects - Map - Sailing indicator", "Enabled", defaultValue: true, "Enable changing of sailing indicator");
            sailingIndicatorPowerIconPosition = Config.Bind("Status effects - Map - Sailing indicator", "Sail power indicator position", defaultValue: new Vector2(-350f, -290f), "Sail size and rudder indicator position");
            sailingIndicatorPowerIconScale = Config.Bind("Status effects - Map - Sailing indicator", "Sail power indicator scale", defaultValue: 1.0f, "Sail size and rudder indicator scale");
            sailingIndicatorWindIndicatorPosition = Config.Bind("Status effects - Map - Sailing indicator", "Wind indicator position", defaultValue: new Vector2(-350f, -140f), "Wind indicator (ship and wind direction) position");
            sailingIndicatorWindIndicatorScale = Config.Bind("Status effects - Map - Sailing indicator", "Wind indicator scale", defaultValue: 1.0f, "Wind indicator (ship and wind direction) scale");

            modEnabled.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorEnabled.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorPowerIconPosition.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorPowerIconScale.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorWindIndicatorPosition.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorWindIndicatorScale.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();

            statusEffectsPositionEnabledNomap = Config.Bind("Status effects - Nomap - List", "Enable", defaultValue: true, "Enable repositioning of status effect list.");
            statusEffectsPositionAnchorNomap = Config.Bind("Status effects - Nomap - List", "Position", defaultValue: new Vector2(-170f, -70f), "Anchored position of list.");
            statusEffectsFillingDirectionNomap = Config.Bind("Status effects - Nomap - List", "Direction", defaultValue: StatusEffectDirection.TopToBottom, "Direction of filling");
            statusEffectsPositionSpacingNomap = Config.Bind("Status effects - Nomap - List", "Spacing", defaultValue: 10, "Spacing between status effects");

            statusEffectsElementEnabledNomap = Config.Bind("Status effects - Nomap - List element", "Custom element enabled", defaultValue: true, "Enables using of horizontal status effect element");
            statusEffectsElementSizeNomap = Config.Bind("Status effects - Nomap - List element", "Size", defaultValue: 36, "Vertical capsule size");

            statusEffectsPositionEnabledNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsPositionAnchorNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsElementEnabledNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsElementSizeNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();

            sailingIndicatorEnabledNomap = Config.Bind("Status effects - Nomap - Sailing indicator", "Enabled", defaultValue: true, "Enable changing of sailing indicator");
            sailingIndicatorPowerIconPositionNomap = Config.Bind("Status effects - Nomap - Sailing indicator", "Sail power indicator position", defaultValue: new Vector2(-350f, -320f), "Sail size and rudder indicator position");
            sailingIndicatorPowerIconScaleNomap = Config.Bind("Status effects - Nomap - Sailing indicator", "Sail power indicator scale", defaultValue: 1.1f, "Sail size and rudder indicator scale");
            sailingIndicatorWindIndicatorPositionNomap = Config.Bind("Status effects - Nomap - Sailing indicator", "Wind indicator position", defaultValue: new Vector2(-350f, -170f), "Wind indicator (ship and wind direction) position");
            sailingIndicatorWindIndicatorScaleNomap = Config.Bind("Status effects - Nomap - Sailing indicator", "Wind indicator scale", defaultValue: 1.1f, "Wind indicator (ship and wind direction) scale");

            sailingIndicatorEnabledNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorPowerIconPositionNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorPowerIconScaleNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorWindIndicatorPositionNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorWindIndicatorScaleNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
        }

        private static string FromSeconds(double seconds)
        {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            return ts.ToString(ts.Hours > 0 ? @"h\:mm\:ss" : @"m\:ss");
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetHoverText))]
        private class Fermenter_GetHoverText_Duration
        {
            private static void Postfix(Fermenter __instance, ref string __result, bool ___m_exposed)
            {
                if (!modEnabled.Value)
                    return;

                if (hoverFermenter.Value == StationHover.Vanilla)
                    return;

                if (__result.IsNullOrWhiteSpace())
                    return;

                if (___m_exposed)
                    return;

                if (!PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false))
                    return;

                string contentName = Localization.instance.Localize(__instance.GetContentName());
                if (!string.IsNullOrEmpty(contentName) && __result.Contains(contentName + ", "))
                    __result = __result.Replace(contentName + ", ", "") + $"\n{contentName}";

                if (__instance.GetStatus() == Fermenter.Status.Fermenting)
                    if (hoverFermenter.Value == StationHover.Percentage)
                        __result += $"\n{__instance.GetFermentationTime() / __instance.m_fermentationDuration:P0}";
                    else if (hoverFermenter.Value == StationHover.MinutesSeconds)
                        __result += $"\n{FromSeconds(__instance.m_fermentationDuration - __instance.GetFermentationTime())}";
            }
        }

        [HarmonyPatch(typeof(Plant), nameof(Plant.GetHoverText))]
        private class Plant_GetHoverText_Duration
        {
            private static void Postfix(Plant __instance, ref string __result)
            {
                if (!modEnabled.Value)
                    return;

                if (hoverPlant.Value == StationHover.Vanilla)
                    return;

                if (__result.IsNullOrWhiteSpace())
                    return;

                if (__instance.GetStatus() != Plant.Status.Healthy)
                    return;

                if (hoverPlant.Value == StationHover.Percentage)
                    __result += $"\n{__instance.TimeSincePlanted() / __instance.GetGrowTime():P0}";
                else if (hoverPlant.Value == StationHover.MinutesSeconds)
                    __result += $"\n{FromSeconds(__instance.GetGrowTime() - __instance.TimeSincePlanted())}";
            }
        }

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.GetHoverText))]
        private class Beehive_GetHoverText_Duration
        {
            private static void Postfix(Beehive __instance, ref string __result)
            {
                if (!modEnabled.Value)
                    return;

                if (hoverBeeHive.Value == StationHover.Vanilla)
                    return;

                if (__result.IsNullOrWhiteSpace())
                    return;

                int honeyLevel = __instance.GetHoneyLevel();

                if (!PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false) || honeyLevel == __instance.m_maxHoney)
                    return;

                float product = __instance.m_nview.GetZDO().GetFloat("product");

                if (hoverBeeHive.Value == StationHover.Percentage)
                    __result += $"\n{product / __instance.m_secPerUnit:P0}";
                else if (hoverBeeHive.Value == StationHover.MinutesSeconds)
                    __result += $"\n{FromSeconds(__instance.m_secPerUnit - product)}";

                if (hoverBeeHiveTotal.Value && honeyLevel < 3)
                    if (hoverBeeHive.Value == StationHover.Percentage)
                        __result += $"\n{(product + __instance.m_secPerUnit * honeyLevel) / (__instance.m_secPerUnit * __instance.m_maxHoney):P0}";
                    else if (hoverBeeHive.Value == StationHover.MinutesSeconds)
                        __result += $"\n{FromSeconds((__instance.m_secPerUnit * __instance.m_maxHoney) - (product + (__instance.m_secPerUnit * honeyLevel)))}";
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
        private class CookingStation_UpdateCooking_Duration
        {
            private static readonly StringBuilder sb = new StringBuilder();

            private static string GetItemName(CookingStation __instance, string currentItem, out bool itemReady, out CookingStation.ItemConversion conversion)
            {
                string itemName = currentItem;

                conversion = __instance.GetItemConversion(currentItem);
                if (conversion != null)
                {
                    itemReady = conversion.m_to.gameObject.name == itemName;
                    itemName = itemReady ? conversion.m_to.GetHoverName() : conversion.m_from.GetHoverName();
                }
                else
                {
                    itemReady = false;
                    List<ItemDrop> itemsList = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, currentItem);
                    if (itemsList.Count > 0)
                        itemName = itemsList[0].GetHoverName();
                }

                return itemName;
            }

            private static string HoverText(CookingStation __instance, string ___m_name, string ___m_addItemTooltip, ZNetView ___m_nview)
            {
                sb.Clear();

                if (___m_nview.IsOwner())
                {

                    for (int slot = 0; slot < __instance.m_slots.Length; slot++)
                    {
                        __instance.GetSlot(slot, out string itemName, out float cookedTime, out _);
                        if (itemName == "")
                            continue;

                        sb.Append("\n");

                        string itemListName = GetItemName(__instance, itemName, out bool itemReady, out CookingStation.ItemConversion itemConversion);

                        if (itemConversion == null || itemName == __instance.m_overCookedItem.name)
                        {
                            sb.Append(itemListName);
                            continue;
                        }

                        sb.Append(itemListName);
                        sb.Append(" ");

                        bool colorRed = itemReady && Mathf.Sin(Time.time * 10f) > 0f;
                        if (colorRed)
                            sb.Append("<color=red>");

                        if (hoverCooking.Value == StationHover.Percentage)
                            sb.Append($"{(cookedTime - (itemReady ? itemConversion.m_cookTime : 0)) / itemConversion.m_cookTime:P0}");
                        else if (hoverCooking.Value == StationHover.MinutesSeconds)
                            sb.Append(FromSeconds(itemConversion.m_cookTime - (cookedTime - (itemReady ? itemConversion.m_cookTime : 0))));

                        if (colorRed)
                            sb.Append("</color>");
                    }
                }

                return ___m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + ___m_addItemTooltip + (ZInput.GamepadActive ? "" : ("\n[<color=yellow><b>1-8</b></color>] " + ___m_addItemTooltip)) + Localization.instance.Localize(sb.ToString());
            }

            private static void Postfix(CookingStation __instance, Switch ___m_addFoodSwitch, string ___m_addItemTooltip, string ___m_name, ZNetView ___m_nview)
            {
                if (!modEnabled.Value)
                    return;

                if (hoverCooking.Value == StationHover.Vanilla)
                    return;

                if ((bool)___m_addFoodSwitch)
                    ___m_addFoodSwitch.m_hoverText = HoverText(__instance, ___m_name, ___m_addItemTooltip, ___m_nview);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.HoverText))]
        private class CookingStation_HoverText_Duration
        {
            private static readonly StringBuilder sb = new StringBuilder();

            private static string GetItemName(CookingStation __instance, string currentItem, out bool itemReady, out CookingStation.ItemConversion conversion)
            {
                string itemName = currentItem;

                conversion = __instance.GetItemConversion(currentItem);
                if (conversion != null)
                {
                    itemReady = conversion.m_to.gameObject.name == itemName;
                    itemName = itemReady ? conversion.m_to.GetHoverName() : conversion.m_from.GetHoverName();
                }
                else
                {
                    itemReady = false;
                    List<ItemDrop> itemsList = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, currentItem);
                    if (itemsList.Count > 0)
                        itemName = itemsList[0].GetHoverName();
                }

                return itemName;
            }

            private static void Postfix(CookingStation __instance, ref string __result, ZNetView ___m_nview)
            {
                if (!modEnabled.Value)
                    return;

                if (hoverCooking.Value == StationHover.Vanilla)
                    return;

                if (__result.IsNullOrWhiteSpace())
                    return;

                if (!___m_nview.IsOwner())
                    return;

                sb.Clear();

                for (int slot = 0; slot < __instance.m_slots.Length; slot++)
                {
                    __instance.GetSlot(slot, out string itemName, out float cookedTime, out _);
                    if (itemName == "")
                        continue;

                    sb.Append("\n");

                    string itemListName = GetItemName(__instance, itemName, out bool itemReady, out CookingStation.ItemConversion itemConversion);

                    if (itemConversion == null || itemName == __instance.m_overCookedItem.name)
                    {
                        sb.Append(itemListName);
                        continue;
                    }

                    sb.Append(itemListName);
                    sb.Append(" ");

                    if (itemReady && Mathf.Sin(Time.time * 10f) > 0f)
                        sb.Append("<color=red>");

                    if (hoverCooking.Value == StationHover.Percentage)
                        sb.Append($"{(cookedTime - (itemReady ? itemConversion.m_cookTime : 0)) / itemConversion.m_cookTime:P0}");
                    else if (hoverCooking.Value == StationHover.MinutesSeconds)
                        sb.Append(FromSeconds(itemConversion.m_cookTime - (cookedTime - (itemReady ? itemConversion.m_cookTime : 0))));

                    if (itemReady && Mathf.Sin(Time.time * 10f) > 0f)
                        sb.Append("</color>");
                }

                __result += Localization.instance.Localize(sb.ToString());
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddFuel))]
        private class Smelter_OnHoverAddFuel_SmelterHover
        {
            private static readonly StringBuilder sb = new StringBuilder();

            private static void Postfix(Smelter __instance, ref string __result, string ___m_name, ItemDrop ___m_fuelItem, int ___m_maxFuel, int ___m_maxOre, int ___m_fuelPerProduct, float ___m_secPerProduct, Windmill ___m_windmill)
            {
                if (!modEnabled.Value)
                    return;

                if (!hoverSmelterShowFuelAndItem.Value && !hoverSmelterEstimatedTime.Value)
                    return;

                float fuel = __instance.GetFuel();
                int queueSize = __instance.GetQueueSize();

                sb.Clear();
                sb.Append(___m_name);
                if (hoverSmelterShowFuelAndItem.Value && ___m_maxOre > 0)
                {
                    sb.Append($" ({queueSize}/{___m_maxOre})");
                }

                sb.Append(" (");
                sb.Append(___m_fuelItem.m_itemData.m_shared.m_name);
                sb.Append($" {Mathf.CeilToInt(fuel)}/{___m_maxFuel})");

                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_smelter_add ");
                sb.Append(___m_fuelItem.m_itemData.m_shared.m_name);

                if (hoverSmelterEstimatedTime.Value && __instance.IsActive() && ___m_maxOre > 0)
                {
                    sb.Append($"\n");

                    float power = Mathf.Max(___m_windmill != null ? ___m_windmill.GetPowerOutput() : 1f, 0.0001f);

                    float estTime = (___m_fuelPerProduct != 0 ? Math.Min(Mathf.FloorToInt(fuel / ___m_fuelPerProduct), queueSize) : queueSize) * ___m_secPerProduct;
                    sb.Append(FromSeconds((___m_fuelPerProduct == 0 || (fuel / ___m_fuelPerProduct) >= queueSize) ? (estTime - __instance.GetBakeTimer()) / power : ___m_secPerProduct * fuel / ___m_fuelPerProduct));
                }

                __result = Localization.instance.Localize(sb.ToString());
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddOre))]
        private class Smelter_OnHoverAddOre_SmelterHover
        {
            private static readonly StringBuilder sb = new StringBuilder();

            private static string GetItemName(Smelter __instance, string currentItem, ref bool nonconversionItem)
            {
                string itemName = currentItem;

                Smelter.ItemConversion conversion = __instance.GetItemConversion(currentItem);
                if (conversion != null)
                {
                    itemName = conversion.m_from.m_itemData.m_shared.m_name;
                }
                else
                {
                    nonconversionItem = true;
                    List<ItemDrop> itemsList = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, currentItem);
                    if (itemsList.Count > 0)
                        itemName = itemsList[0].m_itemData.m_shared.m_name;
                }

                return itemName;
            }

            private static void Postfix(Smelter __instance, ref string __result, string ___m_name, ItemDrop ___m_fuelItem, int ___m_maxFuel, int ___m_maxOre, int ___m_fuelPerProduct,
                                            float ___m_secPerProduct, bool ___m_requiresRoof, bool ___m_haveRoof, string ___m_addOreTooltip, ZNetView ___m_nview, List<Smelter.ItemConversion> ___m_conversion, Windmill ___m_windmill)
            {
                if (!modEnabled.Value)
                    return;

                if (!hoverSmelterShowFuelAndItem.Value && !hoverSmelterEstimatedTime.Value && !hoverSmelterShowQueuedItems.Value)
                    return;

                float fuel = __instance.GetFuel();
                int queueSize = __instance.GetQueueSize();

                sb.Clear();
                sb.Append(___m_name);
                sb.Append($" ({queueSize}/{___m_maxOre})");

                if (hoverSmelterShowFuelAndItem.Value && ___m_fuelItem != null && ___m_maxFuel > 0)
                {
                    sb.Append(" (");
                    sb.Append(___m_fuelItem.m_itemData.m_shared.m_name);
                    sb.Append($" {Mathf.CeilToInt(fuel)}/{___m_maxFuel})");
                }

                if (___m_requiresRoof && !___m_haveRoof && Mathf.Sin(Time.time * 10f) > 0f)
                {
                    sb.Append(" <color=yellow>$piece_smelter_reqroof</color>");
                }

                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] " + ___m_addOreTooltip);

                if (hoverSmelterEstimatedTime.Value && __instance.IsActive() && ___m_maxOre > 0)
                {
                    sb.Append($"\n");

                    float power = Mathf.Max(___m_windmill != null ? ___m_windmill.GetPowerOutput() : 1f, 0.0001f);

                    float estTime = (___m_fuelPerProduct != 0 ? Math.Min(Mathf.FloorToInt(fuel / ___m_fuelPerProduct), queueSize) : queueSize) * ___m_secPerProduct;
                    sb.Append(FromSeconds((___m_fuelPerProduct == 0 || (fuel / ___m_fuelPerProduct) >= queueSize) ? (estTime - __instance.GetBakeTimer()) / power : ___m_secPerProduct * fuel / ___m_fuelPerProduct));
                }

                if (hoverSmelterShowQueuedItems.Value && __instance.GetQueuedOre() != "")
                {
                    List<string> items = new List<string>();

                    int appended = 0; string currentItem = ""; bool nonconversionItem = false;
                    for (int i = 0; i < queueSize; i++)
                    {
                        string item = ___m_nview.GetZDO().GetString($"item{i}");
                        if (item == "")
                            break;
                        else if (currentItem == "")
                        {
                            appended++;
                            currentItem = item;
                        }
                        else if (item == currentItem)
                            appended++;
                        else
                        {
                            items.Add($"{GetItemName(__instance, currentItem, ref nonconversionItem)} x{appended}");
                            appended = 1;
                            currentItem = item;
                        }
                    }

                    if (currentItem != "" && appended > 0)
                    {
                        items.Add($"{GetItemName(__instance, currentItem, ref nonconversionItem)} x{appended}");
                    }

                    if (___m_conversion.Count > 1 || items.Count > 1 || nonconversionItem)
                    {
                        sb.Append($"\n");
                        sb.Append($"\n");
                        sb.Append(String.Join("\n", items));
                    }

                }

                __result = Localization.instance.Localize(sb.ToString());
            }
        }

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdateCharacterList))]
        private class FejdStartup_UpdateCharacterList_MainMenuStats
        {
            private static readonly StringBuilder sb = new StringBuilder();
            private static PlayerProfile playerProfile;
            private static int statCount = 0;

            private static string GetStatName(PlayerStatType stat)
            {
                string statName = Enum.GetName(typeof(PlayerStatType), stat);

                StringBuilder builder = new StringBuilder();

                foreach (char c in statName)
                {
                    if (Char.IsUpper(c) && builder.Length > 0)
                        builder.Append(' ');

                    builder.Append(c);
                }

                return builder.ToString();
            }

            private static void AddStat(PlayerStatType stat, string statName = "", bool showIfZero = false)
            {
                if (!playerProfile.m_playerStats.m_stats.ContainsKey(stat))
                    return;

                float counter = playerProfile.m_playerStats.m_stats[stat];

                if (counter == 0f && !showIfZero)
                    return;

                sb.Append("\n");
                sb.Append($"{(statName != "" ? statName : GetStatName(stat))}: {counter}");

                statCount++;
            }

            private static void AddLine()
            {
                if (statCount > 0)
                    sb.Append("\n");

                statCount = 0;
            }

            private static void Postfix(TMPro.TMP_Text ___m_csSourceInfo, List<PlayerProfile> ___m_profiles, int ___m_profileIndex, GameObject ___m_playerInstance)
            {
                if (!modEnabled.Value)
                    return;

                if (!statsMainMenu.Value)
                    return;

                if (!(bool)___m_playerInstance)
                    return;

                playerProfile = ___m_profiles[___m_profileIndex];

                sb.Append(Localization.instance.Localize(((playerProfile.m_fileSource == FileHelpers.FileSource.Legacy) ? "$menu_legacynotice \n\n" : "") + ((!FileHelpers.m_cloudEnabled) ? "$menu_cloudsavesdisabled" : "")));

                if (statsMainMenuAll.Value)
                {
                    ___m_csSourceInfo.transform.GetComponent<RectTransform>().anchorMax = Vector2.one;
                    ___m_csSourceInfo.transform.localPosition = new Vector3(___m_csSourceInfo.transform.localPosition.x, 0, ___m_csSourceInfo.transform.localPosition.z);

                    foreach (PlayerStatType stat in Enum.GetValues(typeof(PlayerStatType)))
                        AddStat(stat);
                }
                else
                {
                    AddStat(PlayerStatType.EnemyKills, "Kills", showIfZero: true);
                    AddStat(PlayerStatType.Deaths, showIfZero: true);
                    AddStat(PlayerStatType.CraftsOrUpgrades, "Crafts", showIfZero: true);
                    AddStat(PlayerStatType.Builds, showIfZero: true);

                    if (statsMainMenuAdvanced.Value)
                    {
                        AddLine();

                        AddStat(PlayerStatType.PlayerHits);
                        AddStat(PlayerStatType.PlayerKills);
                        AddStat(PlayerStatType.BossKills);
                        AddStat(PlayerStatType.UseGuardianPower);

                        AddLine();

                        AddStat(PlayerStatType.Jumps);
                        AddStat(PlayerStatType.Cheats);
                        AddStat(PlayerStatType.Sleep);
                        AddStat(PlayerStatType.FoodEaten);

                        AddLine();

                        AddStat(PlayerStatType.Tree);
                        AddStat(PlayerStatType.Logs);
                        AddStat(PlayerStatType.Mines);
                        AddStat(PlayerStatType.BeesHarvested);
                        AddStat(PlayerStatType.SapHarvested);
                        AddStat(PlayerStatType.CreatureTamed);

                        AddLine();

                        AddStat(PlayerStatType.SkeletonSummons);
                        AddStat(PlayerStatType.PortalsUsed);
                        AddStat(PlayerStatType.ArrowsShot);
                        AddStat(PlayerStatType.TombstonesOpenedOwn);
                        AddStat(PlayerStatType.TombstonesOpenedOther);
                    }
                }

                ___m_csSourceInfo.text = sb.ToString();

                sb.Clear();
            }
        }

        [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
        private class Tameable_GetHoverText_Tameable
        {
            private static void Postfix(Tameable __instance, ZNetView ___m_nview, ref string __result)
            {
                if (!modEnabled.Value)
                    return;

                if (hoverTame.Value == StationHover.Vanilla)
                    return;

                if (!___m_nview.IsValid())
                    return;

                if (!__instance.m_character.IsTamed())
                {
                    if (__instance.m_tamingTime != 0 || !hoverTameTimeToTame.Value)
                    {
                        float timeLeftToTame = __instance.GetRemainingTime();
                        if (timeLeftToTame != __instance.m_tamingTime)

                            if (hoverTame.Value == StationHover.Percentage)
                                __result += Localization.instance.Localize($"\n$hud_tame: {(__instance.m_tamingTime - timeLeftToTame) / __instance.m_tamingTime:P0}");
                            else if (hoverTame.Value == StationHover.MinutesSeconds)
                                __result += Localization.instance.Localize($"\n$hud_tame: {FromSeconds(timeLeftToTame)}");
                    }
                    return;
                }

                if (!hoverTameTimeToFed.Value)
                    return;

                DateTime dateTime = new DateTime(___m_nview.GetZDO().GetLong(ZDOVars.s_tameLastFeeding, 0L));
                double totalSeconds = (ZNet.instance.GetTime() - dateTime).TotalSeconds;

                if (totalSeconds >= __instance.m_fedDuration)
                    return;

                double timeLeft = __instance.m_fedDuration - totalSeconds;

                if (hoverTame.Value == StationHover.Percentage)
                    __result += Localization.instance.Localize($"\n$hud_tamehappy: {timeLeft / __instance.m_fedDuration:P0}");
                else if (hoverTame.Value == StationHover.MinutesSeconds)
                    __result += Localization.instance.Localize($"\n$hud_tamehappy: {FromSeconds(timeLeft)}");
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
        private class Character_GetHoverText_GrowUpDevelopment
        {
            private static void Postfix(Character __instance, ZNetView ___m_nview, ref string __result)
            {
                if (!modEnabled.Value)
                    return;

                if (!___m_nview.IsValid())
                    return;

                if (hoverCharacter.Value == StationHover.Vanilla)
                    return;

                if (hoverCharacterGrowth.Value && __instance.TryGetComponent(out Growup growup) && growup.m_growTime != 0f)
                {
                    double timeSinceSpawned = growup.m_baseAI.GetTimeSinceSpawned().TotalSeconds;
                    if (timeSinceSpawned > growup.m_growTime)
                        return;

                    string grownup = growup.GetPrefab().name;
                    if (!characterNames.ContainsKey(grownup))
                        characterNames.Add(grownup, growup.GetPrefab().GetComponent<Character>()?.m_name);

                    switch (hoverCharacter.Value)
                    {
                        case StationHover.Percentage:
                            __result += $"\n{Localization.instance.Localize(characterNames[grownup])}: {timeSinceSpawned / growup.m_growTime:P0}";
                            return;
                        case StationHover.MinutesSeconds:
                            __result += $"\n{Localization.instance.Localize(characterNames[grownup])}: {FromSeconds(growup.m_growTime - timeSinceSpawned)}";
                            return;
                        default:
                            return;
                    }
                }

                if (hoverCharacterProcreation.Value && __instance.TryGetComponent(out Procreation procreation) && procreation.IsPregnant())
                {
                    long @long = ___m_nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L);
                    double timeSincePregnant = (ZNet.instance.GetTime() - new DateTime(@long)).TotalSeconds;
                    if (timeSincePregnant > procreation.m_pregnancyDuration)
                        return;

                    string offspring = procreation.m_offspring.name;
                    if (!characterNames.ContainsKey(offspring))
                        characterNames.Add(offspring, procreation.m_offspring.GetComponent<Character>()?.m_name);

                    switch (hoverCharacter.Value)
                    {
                        case StationHover.Percentage:
                            __result += $"\n{Localization.instance.Localize(characterNames[offspring])}: {timeSincePregnant / procreation.m_pregnancyDuration:P0}";
                            return;
                        case StationHover.MinutesSeconds:
                            __result += $"\n{Localization.instance.Localize(characterNames[offspring])}: {FromSeconds(procreation.m_pregnancyDuration - timeSincePregnant)}";
                            return;
                        default:
                            return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GetHoverText))]
        private class EggGrow_GetHoverText_EggGrow
        {
            private static void Postfix(EggGrow __instance, ItemDrop ___m_item, ZNetView ___m_nview, ref string __result)
            {
                if (!modEnabled.Value)
                    return;

                if (hoverCharacter.Value == StationHover.Vanilla)
                    return;

                if (!___m_item)
                    return;

                if (!___m_nview || !___m_nview.IsValid())
                    return;

                var growStart = ___m_nview.GetZDO().GetFloat(ZDOVars.s_growStart);
                bool isWarm = growStart > 0f;
                
                if (hoverCharacterEggGrow.Value && isWarm)
                {
                    double timeSinceGrowStart = ZNet.instance.GetTimeSeconds() - growStart;
                    if (timeSinceGrowStart > __instance.m_growTime)
                        return;

                    var growupPrefab = __instance.m_grownPrefab;
                    string offspring = growupPrefab.name;

                    if (!characterNames.ContainsKey(offspring))
                        characterNames.Add(offspring, growupPrefab.GetComponent<Character>()?.m_name);

                    switch (hoverCharacter.Value)
                    {
                        case StationHover.Percentage:
                            __result += $"\n{Localization.instance.Localize(characterNames[offspring])}: {timeSinceGrowStart/__instance.m_growTime:P0}";
                            return;
                        case StationHover.MinutesSeconds:
                            __result += $"\n{Localization.instance.Localize(characterNames[offspring])}: {FromSeconds(__instance.m_growTime - timeSinceGrowStart)}";
                            return;
                        default:
                            return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
        public static class InventoryGui_SetupRequirement_AddAvailableAmount
        {
            public static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, bool __result)
            {
                if (!modEnabled.Value)
                    return;

                if (!showAvailableItemsAmount.Value)
                    return;

                if (!__result)
                    return;

                if (UnityInput.Current.GetKey(KeyCode.LeftAlt) || UnityInput.Current.GetKey(KeyCode.RightAlt))
                    return;

                TMPro.TMP_Text component3 = elementRoot.transform.Find("res_amount").GetComponent<TMPro.TMP_Text>();
                if (component3 == null)
                    return;

                component3.SetText(component3.text + $" <color=#{ColorUtility.ToHtmlStringRGBA(availableItemsAmountColor.Value)}>({player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name)})</color>");
            }
        }

        private static void SetInventoryAnimationSpeed()
        {
            if (InventoryGui.instance && InventoryGui.instance.m_animator)
                InventoryGui.instance.m_animator.speed = Mathf.Max(inventoryOpenCloseAnimationSpeed.Value, 0f);
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class InventoryGui_Awake_AddAvailableAmount
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                if (inventoryOpenCloseAnimationSpeed.Value != 1f)
                {
                    SetInventoryAnimationSpeed();
                }
            }
        }
        
    }
}

