using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using ServerSync;
using UnityEngine.InputSystem;

namespace MyLittleUI
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    [BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Azumatt.AzuExtendedPlayerInventory", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("shudnal.ExtraSlots", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("randyknapp.mods.equipmentandquickslots", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("randyknapp.mods.auga")]
    public class MyLittleUI : BaseUnityPlugin
    {
        public const string pluginID = "shudnal.MyLittleUI";
        public const string pluginName = "My Little UI";
        public const string pluginVersion = "1.1.21";

        private readonly Harmony harmony = new Harmony(pluginID);

        internal static readonly ConfigSync configSync = new ConfigSync(pluginID) { DisplayName = pluginName, CurrentVersion = pluginVersion, MinimumRequiredVersion = pluginVersion };
        
        public static MyLittleUI instance;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> configLocked;
        public static ConfigEntry<bool> loggingEnabled;
        public static ConfigEntry<bool> nonlocalizedButtons;
        public static ConfigEntry<bool> fixStatusEffectAndForecastPosition;

        public static ConfigEntry<bool> clockShowDay;
        public static ConfigEntry<bool> clockShowTime;
        public static ConfigEntry<bool> clockTimeFormat24h;
        public static ConfigEntry<bool> clockShowBackground;
        public static ConfigEntry<Color> clockBackgroundColor;
        public static ConfigEntry<float> clockTextPadding;
        public static ConfigEntry<Vector2> clockPosition;
        public static ConfigEntry<Vector2> clockSize;
        public static ConfigEntry<bool> clockSwapDayTime;
        public static ConfigEntry<float> clockFontSize;
        public static ConfigEntry<Color> clockFontColor;
        public static ConfigEntry<ClockTimeType> clockTimeType;
        public static ConfigEntry<string> clockFuzzy;

        public static ConfigEntry<bool> forecastEnabled;
        public static ConfigEntry<bool> forecastShowBackground;
        public static ConfigEntry<Color> forecastBackgroundColor;
        public static ConfigEntry<Vector2> forecastPosition;
        public static ConfigEntry<Vector2> forecastPositionNomap;
        public static ConfigEntry<Vector2> forecastSize;
        public static ConfigEntry<float> forecastFontSize;
        public static ConfigEntry<Color> forecastFontColor;
        public static ConfigEntry<float> forecastTextPadding;

        public static ConfigEntry<string> forecastListRain;
        public static ConfigEntry<string> forecastListSnow;
        public static ConfigEntry<string> forecastListThunder;
        public static ConfigEntry<string> forecastListMist;
        public static ConfigEntry<string> forecastListRainCinder;

        public static ConfigEntry<bool> windsEnabled;
        public static ConfigEntry<bool> windsShowBackground;
        public static ConfigEntry<bool> windsShowProgress;
        public static ConfigEntry<Color> windsBackgroundColor;
        public static ConfigEntry<Color> windsProgressColor;
        public static ConfigEntry<Color> windsArrowColor;
        public static ConfigEntry<float> windsMinimumAlpha;
        public static ConfigEntry<bool> windsAlphaIntensity;

        public static ConfigEntry<Vector2> windsPosition;
        public static ConfigEntry<Vector2> windsPositionNomap;
        public static ConfigEntry<Vector2> windsSize;
        public static ConfigEntry<Vector2> windsSizeNomap;
        public static ConfigEntry<int> windsCount;
        public static ConfigEntry<int> windsCountNomap;
        public static ConfigEntry<float> windsPositionSpacing;
        public static ConfigEntry<float> windsPositionSpacingNomap;
        public static ConfigEntry<ListDirection> windsFillingDirection;
        public static ConfigEntry<ListDirection> windsFillingDirectionNomap;

        public static ConfigEntry<bool> ammoCountEnabled;
        public static ConfigEntry<Color> ammoCountColor;
        public static ConfigEntry<Vector2> ammoCountPosition;
        public static ConfigEntry<int> ammoCountFontSize;
        public static ConfigEntry<HorizontalAlignmentOptions> ammoCountAlignment;
        public static ConfigEntry<bool> ammoIconEnabled;
        public static ConfigEntry<Vector2> ammoIconPosition;
        public static ConfigEntry<Vector2> ammoIconSize;
        public static ConfigEntry<bool> baitIconEnabled;
        public static ConfigEntry<bool> baitCountEnabled;

        public static ConfigEntry<bool> showAvailableItemsAmount;
        public static ConfigEntry<Color> availableItemsAmountColor;
        public static ConfigEntry<bool> showMulticraftButtons;

        public static ConfigEntry<bool> craftingFilterEnabled;

        public static ConfigEntry<bool> durabilityEnabled;
        public static ConfigEntry<Color> durabilityFine;
        public static ConfigEntry<Color> durabilityWorn;
        public static ConfigEntry<Color> durabilityAtRisk;
        public static ConfigEntry<Color> durabilityBroken;

        public static ConfigEntry<float> itemIconScale;
        public static ConfigEntry<Color> itemEquippedColor;

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
        public static ConfigEntry<bool> itemQualityIgnoreCustomEquipmentSlots;
        public static ConfigEntry<bool> itemQualityIgnoreCustomSlots;
        
        public static ConfigEntry<bool> itemQualityHideLvl1;
        public static ConfigEntry<bool> itemQualityHideCustomEquipmentSlots;

        public static ConfigEntry<float> inventoryOpenCloseAnimationSpeed;

        public static ConfigEntry<bool> statsMainMenu;
        public static ConfigEntry<bool> statsMainMenuAdvanced;
        public static ConfigEntry<bool> statsMainMenuAll;

        public static ConfigEntry<bool> statsCharacterArmor;
        public static ConfigEntry<bool> statsCharacterEffects;
        public static ConfigEntry<bool> statsCharacterEffectsMagic;

        public static ConfigEntry<bool> hoverFermenterEnabled;
        public static ConfigEntry<bool> hoverPlantEnabled;
        public static ConfigEntry<bool> hoverCookingEnabled;
        public static ConfigEntry<bool> hoverBeeHiveEnabled;
        public static ConfigEntry<StationHover> hoverFermenter;
        public static ConfigEntry<StationHover> hoverPlant;
        public static ConfigEntry<StationHover> hoverCooking;
        public static ConfigEntry<StationHover> hoverBeeHive;
        public static ConfigEntry<bool> hoverBeeHiveTotal;

        public static ConfigEntry<StationHover> hoverCharacter;
        public static ConfigEntry<bool> hoverCharacterGrowth;
        public static ConfigEntry<bool> hoverCharacterProcreation;
        public static ConfigEntry<bool> hoverCharacterEggGrow;
        public static ConfigEntry<bool> hoverCharacterLovePoints;

        public static ConfigEntry<StationHover> hoverTame;
        public static ConfigEntry<bool> hoverTameTimeToTame;
        public static ConfigEntry<bool> hoverTameTimeToFed;

        public static ConfigEntry<bool> hoverSmelterEstimatedTime;
        public static ConfigEntry<bool> hoverSmelterShowFuelAndItem;
        public static ConfigEntry<bool> hoverSmelterShowQueuedItems;

        public static ConfigEntry<ChestItemsHover> chestHoverItems;
        public static ConfigEntry<ChestNameHover> chestHoverName;
        public static ConfigEntry<bool> chestCustomName;
        public static ConfigEntry<bool> chestShowHoldToStack;
        public static ConfigEntry<bool> chestShowRename;

        public static ConfigEntry<bool> chestContentEnabled;
        public static ConfigEntry<int> chestContentLinesToShow;
        public static ConfigEntry<ContentSortType> chestContentSortType;
        public static ConfigEntry<ContentSortDir> chestContentSortDir;
        public static ConfigEntry<string> chestContentEntryFormat;
        public static ConfigEntry<Color> chestContentItemColor;
        public static ConfigEntry<Color> chestContentAmountColor;

        public static ConfigEntry<bool> statusEffectsPositionEnabled;
        public static ConfigEntry<Vector2> statusEffectsPositionAnchor;
        public static ConfigEntry<ListDirection> statusEffectsFillingDirection;
        public static ConfigEntry<int> statusEffectsPositionSpacing;

        public static ConfigEntry<bool> statusEffectsElementEnabled;
        public static ConfigEntry<int> statusEffectsElementSize;

        public static ConfigEntry<bool> statusEffectsPositionEnabledNomap;
        public static ConfigEntry<Vector2> statusEffectsPositionAnchorNomap;
        public static ConfigEntry<ListDirection> statusEffectsFillingDirectionNomap;
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

        public static ConfigEntry<bool> showWeight;
        public static ConfigEntry<bool> showSlots;
        public static ConfigEntry<Color> weightBackgroundColor;
        public static ConfigEntry<Color> slotsBackgroundColor;
        public static ConfigEntry<bool> showWeightLeft;
        public static ConfigEntry<bool> showSlotsTaken;

        public static ConfigEntry<Color> weightSlotsFine;
        public static ConfigEntry<Color> weightSlotsHalf;
        public static ConfigEntry<Color> weightSlotsALot;
        public static ConfigEntry<Color> weightSlotsFull;

        public static ConfigEntry<Vector2> weightPosition;
        public static ConfigEntry<Vector2> slotsPosition;

        public static ConfigEntry<Color> weightFontColor;
        public static ConfigEntry<Color> slotsFontColor;



        private static readonly Dictionary<string, string> characterNames = new Dictionary<string, string>();

        public static Component epicLootPlugin;

        public static readonly int layerUI = LayerMask.NameToLayer("UI");

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

        public enum ListDirection
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
        public enum ClockTimeType
        {
            GameTime,
            Fuzzy,
            RealTime
        }

        private void Awake()
        {
            harmony.PatchAll();

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);

            epicLootPlugin = GetComponent("EpicLoot");

            ItemTooltip.Initialize();

            LoadIcons();

            Game.isModded = true;
        }

        private void OnDestroy()
        {
            Config.Save();
            harmony?.UnpatchSelf();
        }

        public static void LogInfo(object data)
        {
            if (loggingEnabled.Value)
                instance.Logger.LogInfo(data);
        }

        private void ConfigInit()
        {
            Config.Bind("General", "NexusID", 2562, "Nexus mod ID for updates");

            modEnabled = config("General", "Enabled", defaultValue: true, "Enable the mod. [Synced with Server]", synchronizedSetting: true);
            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only. [Synced with Server]", synchronizedSetting: true);
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging.");
            nonlocalizedButtons = config("General", "Nonlocalized button keys", defaultValue: true, "Keyboard keys A-Z will not be localized in the current keyboard layout. If changed while in game then time should pass for some cached localization strings to be cleared.");
            fixStatusEffectAndForecastPosition = config("General", "Status effects and forecast position fix", defaultValue: true, "If status effect position was not changed prior to 1.0.11 version - fix status effect list position for forecast.");

            modEnabled.SettingChanged += (s, e) => { InfoBlocks.UpdateVisibility(); CustomStatusEffectsList.InitializeStatusEffectTemplate(); CustomStatusEffectsList.ChangeSailingIndicator(); ZInput_GetBoundKeyString_NonlocalizedButtons.OnChange(); };
            nonlocalizedButtons.SettingChanged += (s, e) => ZInput_GetBoundKeyString_NonlocalizedButtons.OnChange();

            clockShowDay = config("Info - Clock", "Show day", defaultValue: true, "Enable day number [Synced with Server]", synchronizedSetting: true);
            clockShowTime = config("Info - Clock", "Show time", defaultValue: true, "Enable time [Synced with Server]", synchronizedSetting: true);
            clockTimeFormat24h = config("Info - Clock", "Time format 24h", defaultValue: true, "Show time in HH:mm format");
            clockShowBackground = config("Info - Clock", "Background enabled", defaultValue: false, "Show clock background");
            clockBackgroundColor = config("Info - Clock", "Background color", defaultValue: Color.clear, "Clock background color. If not set - minimap background color is used.");
            clockTextPadding = config("Info - Clock", "Padding", defaultValue: 5f, "Left and right indentation for text if both time and day used");
            clockPosition = config("Info - Clock", "Position", defaultValue: new Vector2(-140f, -25f), "anchoredPosition of clock object transform");
            clockSize = config("Info - Clock", "Size", defaultValue: new Vector2(200f, 25f), "sizeDelta of clock object transform");
            clockSwapDayTime = config("Info - Clock", "Swap day and time", defaultValue: false, "Swap day and time positions");
            clockFontSize = config("Info - Clock", "Font size", defaultValue: 0f, "If not set - value is taken from minimap small biome label");
            clockFontColor = config("Info - Clock", "Font color", defaultValue: Color.clear, "If not set - value is taken from minimap small biome label");
            clockTimeType = config("Info - Clock", "Time type", defaultValue: ClockTimeType.GameTime, "Time to show");
            clockFuzzy = config("Info - Clock", "Time fuzzy words", defaultValue: "Midnight,Early Morning,Before Dawn,Dawn,Morning,Late Morning,Midday,Early Afternoon,Afternoon,Evening,Night,Late Night", "The length of the day will be divided into equal periods of time according to the number of words specified.");

            clockShowBackground.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeBackground();
            clockBackgroundColor.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeBackground();
            clockShowDay.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeText();
            clockShowTime.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeText();
            clockTextPadding.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeText();
            clockPosition.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeText();
            clockSize.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeText();
            clockSwapDayTime.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeText();
            clockFontSize.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeText();
            clockFontColor.SettingChanged += (sender, args) => InfoBlocks.UpdateDayTimeText();
            clockTimeFormat24h.SettingChanged += (sender, args) => InfoBlocks.UpdateClock();
            clockTimeType.SettingChanged += (sender, args) => InfoBlocks.UpdateClock();
            clockFuzzy.SettingChanged += (sender, args) => InfoBlocks.FuzzyWordsOnChange();

            forecastEnabled = config("Info - Forecast", "Enabled", defaultValue: true, "Enable next change of weather [Synced with Server]", synchronizedSetting: true);
            forecastShowBackground = config("Info - Forecast", "Weather background enabled", defaultValue: false, "Show forecast background");
            forecastBackgroundColor = config("Info - Forecast", "Weather background color", defaultValue: Color.clear, "Forecast background color. If not set - minimap background color is used.");
            forecastPosition = config("Info - Forecast", "Position", defaultValue: new Vector2(-78f, -255f), "anchoredPosition of forecast object transform");
            forecastPositionNomap = config("Info - Forecast", "Position in nomap", defaultValue: new Vector2(-78f, -55f), "anchoredPosition of forecast object transform in nomap mode");
            forecastSize = config("Info - Forecast", "Size", defaultValue: new Vector2(75f, 25f), "sizeDelta of forecast object transform");
            forecastFontSize = config("Info - Forecast", "Font size", defaultValue: 0f, "If not set - value is taken from minimap small biome label");
            forecastFontColor = config("Info - Forecast", "Font color", defaultValue: Color.clear, "If not set - value is taken from minimap small biome label");
            forecastTextPadding = config("Info - Forecast", "Text padding", defaultValue: 2f, "Margin between icon and text");

            forecastShowBackground.SettingChanged += (sender, args) => InfoBlocks.UpdateForecastBackground();
            forecastBackgroundColor.SettingChanged += (sender, args) => InfoBlocks.UpdateForecastBackground();

            forecastEnabled.SettingChanged += (sender, args) => InfoBlocks.UpdateForecastBlock();
            forecastPosition.SettingChanged += (sender, args) => InfoBlocks.UpdateForecastBlock();
            forecastPositionNomap.SettingChanged += (sender, args) => InfoBlocks.UpdateForecastBlock();
            forecastSize.SettingChanged += (sender, args) => InfoBlocks.UpdateForecastBlock();
            forecastFontSize.SettingChanged += (sender, args) => InfoBlocks.UpdateForecastBlock();
            forecastFontColor.SettingChanged += (sender, args) => InfoBlocks.UpdateForecastBlock();
            forecastTextPadding.SettingChanged += (sender, args) => InfoBlocks.UpdateForecastBlock();

            forecastListRain = config("Info - Forecast - Lists", "Rain", defaultValue: "Rain,LightRain,MistlandsRain,SlimeRain", "Comma separated list of m_psySystems or m_envObject names associated with Rain environments");
            forecastListSnow = config("Info - Forecast - Lists", "Snow", defaultValue: "SnowStorm", "Comma separated list of m_psySystems or m_envObject names associated with Snow environments");
            forecastListThunder = config("Info - Forecast - Lists", "Thunder", defaultValue: "Thunder,MistlandsThunder,AshlandsThunder", "Comma separated list of m_psySystems or m_envObject names associated with Thunder environments");
            forecastListMist = config("Info - Forecast - Lists", "Mist", defaultValue: "Mist,Ashlands_Misty", "Comma separated list of m_psySystems or m_envObject names associated with Mist environments");
            forecastListRainCinder = config("Info - Forecast - Lists", "Ash Rain", defaultValue: "Ashlands_RainCinder,Ashlands_CinderRain", "Comma separated list of m_psySystems or m_envObject names associated with Ash Cinder Rain environments");

            windsEnabled = config("Info - Winds", "Enabled", defaultValue: true, "Enable next winds [Synced with Server]", synchronizedSetting: true);
            windsShowBackground = config("Info - Winds", "Winds background enabled", defaultValue: true, "Show winds background");
            windsBackgroundColor = config("Info - Winds", "Winds background color", defaultValue: Color.clear, "Winds background color. If not set - minimap background color is used.");
            windsShowProgress = config("Info - Winds", "Progress enabled", defaultValue: true, "Show winds progress");
            windsProgressColor = config("Info - Winds", "Progress color", defaultValue: Color.clear, "Winds progress color. If not set - minimap background color is used.");
            windsArrowColor = config("Info - Winds", "Winds arrow color", defaultValue: Color.white, "Winds arrow color.");
            windsMinimumAlpha = config("Info - Winds", "Minimum wind arrow alpha", defaultValue: 0.5f, "Amount of winds to forecast");
            windsAlphaIntensity = config("Info - Winds", "Set wind arrow alpha as intensity", defaultValue: true, "If enabled - wind arrow will be more transparent with less wind intensity");

            windsShowBackground.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBackground();
            windsBackgroundColor.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBackground();
            windsShowProgress.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBackground();
            windsProgressColor.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBackground();

            windsEnabled.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBlock();
            windsArrowColor.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBlock();
            windsMinimumAlpha.SettingChanged += (sender, args) => WeatherForecast.UpdateNextWinds(forceRebuildList: true);
            windsAlphaIntensity.SettingChanged += (sender, args) => WeatherForecast.UpdateNextWinds(forceRebuildList: true);

            windsCount = config("Info - Winds - List", "Wind arrows amount", defaultValue: 5, "Amount of winds to forecast");
            windsFillingDirection = config("Info - Winds - List", "Direction", defaultValue: ListDirection.LeftToRight, "Direction of filling");
            windsPositionSpacing = config("Info - Winds - List", "Spacing", defaultValue: 1f, "Spacing between arrows");
            windsPosition = config("Info - Winds - List", "Position", defaultValue: new Vector2(-180f, -255f), "anchoredPosition of winds object transform");
            windsSize = config("Info - Winds - List", "Size", defaultValue: new Vector2(119f, 25f), "sizeDelta of winds object transform");

            windsCountNomap = config("Info - Winds - List", "Nomap Wind arrows amount", defaultValue: 5, "Amount of winds to forecast");
            windsFillingDirectionNomap = config("Info - Winds - List", "Nomap Direction", defaultValue: ListDirection.LeftToRight, "Direction of filling");
            windsPositionSpacingNomap = config("Info - Winds - List", "Nomap Spacing", defaultValue: 1f, "Spacing between arrows");
            windsPositionNomap = config("Info - Winds - List", "Nomap Position", defaultValue: new Vector2(-180f, -55f), "anchoredPosition of winds object transform in nomap mode");
            windsSizeNomap = config("Info - Winds - List", "Nomap Size", defaultValue: new Vector2(119f, 25f), "sizeDelta of winds object transform");

            windsPosition.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBlock();
            windsPositionNomap.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBlock();
            windsSize.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBlock(forceRebuildList: true);
            windsSizeNomap.SettingChanged += (sender, args) => InfoBlocks.UpdateWindsBlock(forceRebuildList: true);

            windsCount.SettingChanged += (sender, args) => WeatherForecast.UpdateNextWinds();
            windsCountNomap.SettingChanged += (sender, args) => WeatherForecast.UpdateNextWinds();

            windsFillingDirection.SettingChanged += (sender, args) => WeatherForecast.UpdateNextWinds(forceRebuildList: true);
            windsFillingDirectionNomap.SettingChanged += (sender, args) => WeatherForecast.UpdateNextWinds(forceRebuildList: true);
            windsPositionSpacing.SettingChanged += (sender, args) => WeatherForecast.UpdateNextWinds(forceRebuildList: true);
            windsPositionSpacingNomap.SettingChanged += (sender, args) => WeatherForecast.UpdateNextWinds(forceRebuildList: true);

            ammoCountEnabled = config("Item - Ammo icon and count", "Ammo count Enabled", defaultValue: true, "Show amount of available ammo and ammo icon for weapon in hotbar  [Synced with Server]", synchronizedSetting: true);
            ammoCountColor = config("Item - Ammo icon and count", "Ammo count Color", defaultValue: Color.clear, "Color of available ammo for weapon in hotbar");
            ammoCountPosition = config("Item - Ammo icon and count", "Ammo count Position", defaultValue: new Vector2(0f, -14f), "Position of available ammo for weapon in hotbar");
            ammoCountFontSize = config("Item - Ammo icon and count", "Ammo count FontSize", defaultValue: 14, "Show amount of available ammo and ammo icon for weapon in hotbar");
            ammoCountAlignment = config("Item - Ammo icon and count", "Ammo count Alignment", defaultValue: HorizontalAlignmentOptions.Center, "Text horizontal alignment of available ammo for weapon in hotbar");
            ammoIconEnabled = config("Item - Ammo icon and count", "Ammo icon Enabled", defaultValue: true, "Show icon of available ammo for weapon in hotbar [Synced with Server]", synchronizedSetting: true);
            ammoIconPosition = config("Item - Ammo icon and count", "Ammo icon Position", defaultValue: new Vector2(0f, -40f), "Position of ammo icon for weapon in hotbar");
            ammoIconSize = config("Item - Ammo icon and count", "Ammo icon Size", defaultValue: new Vector2(-10f, -10f), "Ammo icon for weapon in hotbar");

            baitIconEnabled = config("Item - Ammo icon and count", "Bait icon Enabled", defaultValue: true, "Show amount of available ammo and ammo icon for weapon in hotbar [Synced with Server]", synchronizedSetting: true);
            baitCountEnabled = config("Item - Ammo icon and count", "Bait ammo Enabled", defaultValue: true, "Show amount of available ammo and ammo icon for weapon in hotbar [Synced with Server]", synchronizedSetting: true);

            ammoCountEnabled.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();
            ammoCountColor.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();
            ammoCountPosition.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();
            ammoCountFontSize.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();
            ammoCountAlignment.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();
            ammoIconEnabled.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();
            ammoIconPosition.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();
            ammoIconSize.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();
            baitIconEnabled.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();
            baitCountEnabled.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();

            showAvailableItemsAmount = config("Item - Available resources amount", "Enabled", defaultValue: true, "Show amount of available resources for crafting in requirements list (player inventory only) [Synced with Server]", synchronizedSetting: true);
            availableItemsAmountColor = config("Item - Available resources amount", "Color", defaultValue: new Color(0.68f, 0.85f, 0.90f), "Color of amount of available resources.");
            showMulticraftButtons = config("Item - Available resources amount", "Multicraft", defaultValue: true, "Show multicraft buttons [Synced with Server]", synchronizedSetting: true);
            
            showMulticraftButtons.SettingChanged += (sender, args) => MultiCraft.UpdateMulticraftPanel();

            craftingFilterEnabled = config("Item - Crafting - Filter", "Enabled", defaultValue: true, "Enable filtering of craft list. [Synced with Server]", synchronizedSetting: true);

            craftingFilterEnabled.SettingChanged += (s, e) => CraftFilter.UpdateVisibility();

            durabilityEnabled = config("Item - Durability", "0 - Enabled", defaultValue: true, "Enable color of durability. [Synced with Server]", synchronizedSetting: true);
            durabilityFine = config("Item - Durability", "1 - Fine", defaultValue: new Color(0.11765f, 0.72941f, 0.03529f, 1f), "Color of durability > 75%.");
            durabilityWorn = config("Item - Durability", "2 - Worn", defaultValue: new Color(0.72941f, 0.72941f, 0.03529f, 1f), "Color of durability > 50%.");
            durabilityAtRisk = config("Item - Durability", "3 - At risk", defaultValue: new Color(0.72941f, 0.34902f, 0.03529f, 1f), "Color of durability > 25%.");
            durabilityBroken = config("Item - Durability", "4 - Broken", defaultValue: new Color(0.72941f, 0.03529f, 0.03529f, 1f), "Color of durability >= 0%.");

            durabilityFine.SettingChanged += (sender, args) => ItemIcon.UpdateGradient();
            durabilityWorn.SettingChanged += (sender, args) => ItemIcon.UpdateGradient();
            durabilityAtRisk.SettingChanged += (sender, args) => ItemIcon.UpdateGradient();
            durabilityBroken.SettingChanged += (sender, args) => ItemIcon.UpdateGradient();

            itemIconScale = config("Item - Icon", "Icon scale", defaultValue: 1.0f, "Relative scale size of item icons.");
            itemEquippedColor = config("Item - Icon", "Equipped color", defaultValue: Color.clear, "Override for color of equipped items.");

            itemTooltip = config("Item - Tooltip", "Enabled", defaultValue: true, "Updated item tooltip. Hold Alt to see original tooltip");
            itemTooltipColored = config("Item - Tooltip", "Colored numbers", defaultValue: true, "Orange and yellow value numbers in tooltip, light blue if disabled");

            itemQuality = config("Item - Quality", "Enabled", defaultValue: true, "Show item quality as symbol");
            itemQualitySymbol = config("Item - Quality", "Symbol", defaultValue: "★", "Symbol to show.");
            itemQualitySymbolColor = config("Item - Quality", "Symbol Color", defaultValue: new Color(1f, 0.65f, 0f, 1f), "Symbol color");
            itemQualitySymbolSize = config("Item - Quality", "Symbol Size", defaultValue: 10f, "Symbol size");
            itemQualityMax = config("Item - Quality", "Maximum symbols", defaultValue: 8, "Maximum amount of symbols to show.");
            itemQualityRows = config("Item - Quality", "Maximum rows", defaultValue: 2, "Maximum amount of rows to show.");
            itemQualityColumns = config("Item - Quality", "Maximum columns", defaultValue: 4, "Maximum amount of columns to show.");
            itemQualityLineSpacing = config("Item - Quality", "Space between lines", defaultValue: -35.0f, "Line spacing.");
            itemQualityCharacterSpacing = config("Item - Quality", "Space between characters", defaultValue: 8f, "Character spacing.");
            itemQualityIgnoreCustomEquipmentSlots = config("Item - Quality", "Ignore equipment slots", defaultValue: false, "Ignore custom equipment slots added by AzuEPI or EaQS. Quick slot items will remain.");
            itemQualityIgnoreCustomSlots = config("Item - Quality", "Ignore any custom slot", defaultValue: false, "Ignore every custom slot outside of shown inventory rows.");
            
            itemQualitySymbol.SettingChanged += (sender, args) => itemQualitySymbol.Value = itemQualitySymbol.Value[0].ToString();

            itemQualitySymbol.SettingChanged += (sender, args) => ItemIcon.FillItemQualityCache();
            itemQualityMax.SettingChanged += (sender, args) => ItemIcon.FillItemQualityCache();
            itemQualityRows.SettingChanged += (sender, args) => ItemIcon.FillItemQualityCache();
            itemQualityColumns.SettingChanged += (sender, args) => ItemIcon.FillItemQualityCache();

            ItemIcon.FillItemQualityCache();

            itemQualityHideLvl1 = config("Item - Quality - Display", "Hide quality on lvl 1 items", defaultValue: false, "Hide lvl 1 quality if item lvl is 1.");
            itemQualityHideCustomEquipmentSlots = config("Item - Quality - Display", "Hide quality on equipment slots", defaultValue: false, "Hide quality if item is in equipment slots.");
           
            itemQualityHideLvl1.SettingChanged += (sender, args) => AmmoCountIcon.UpdateVisibility();

            inventoryOpenCloseAnimationSpeed = config("Inventory", "Animation speed", defaultValue: 1f, "Inventory show/close animation speed");

            inventoryOpenCloseAnimationSpeed.SettingChanged += (sender, args) => SetInventoryAnimationSpeed();

            statsMainMenu = config("Stats - Main menu", "Show stats in main menu", defaultValue: true, "Show character statistics in main menu");
            statsMainMenuAdvanced = config("Stats - Main menu", "Show advanced stats in main menu", defaultValue: true, "Show advanced character statistics in main menu");
            statsMainMenuAll = config("Stats - Main menu", "Show all stats in main menu", defaultValue: false, "Show all character statistics in main menu");

            statsCharacterArmor = config("Stats - Character", "Show character stats on armor hover", defaultValue: true, "Show character stats in armor tooltip");
            statsCharacterEffects = config("Stats - Character", "Show character active effects on weight hover", defaultValue: true, "Show character active effects in weight tooltip");
            statsCharacterEffectsMagic = config("Stats - Character", "Show character active magic effects (EpicLoot) on weight hover", defaultValue: true, "Show character active magic effects in weight tooltip");

            statsCharacterArmor.SettingChanged += (sender, args) => InventoryCharacterStats.UpdateTooltipState();
            statsCharacterEffects.SettingChanged += (sender, args) => InventoryCharacterStats.UpdateTooltipState();

            hoverCharacter = config("Hover - Character", "Character Hover", defaultValue: StationHover.Vanilla, "Format of baby development's total needed time/percent.");
            hoverCharacterGrowth = config("Hover - Character", "Show baby growth", true, "Show total growth percentage/remaining for babies. [Synced with Server]", synchronizedSetting: true);
            hoverCharacterProcreation = config("Hover - Character", "Show offspring", true, "Show percentage/remaining for new offspring. [Synced with Server]", synchronizedSetting: true);
            hoverCharacterEggGrow = config("Hover - Character", "Show egg hatching", true, "Show percentage/remaining for egg hatching. [Synced with Server]", synchronizedSetting: true);
            hoverCharacterLovePoints = config("Hover - Character", "Show love points", true, "Show how many love points creature has (likeliness to be pregnant). [Synced with Server]", synchronizedSetting: true);

            hoverFermenterEnabled = config("Hover - Stations", "Fermenter Hover Enabled", defaultValue: true, "Enable Hover text for fermenter. [Synced with Server]", synchronizedSetting: true);
            hoverPlantEnabled = config("Hover - Stations", "Plants Hover Enabled", defaultValue: true, "Enable Hover text for plants. [Synced with Server]", synchronizedSetting: true);
            hoverCookingEnabled = config("Hover - Stations", "Cooking stations Hover Enabled", defaultValue: true, "Enable Hover text for cooking stations. [Synced with Server]", synchronizedSetting: true);
            hoverBeeHiveEnabled = config("Hover - Stations", "Bee Hive Hover Enabled", defaultValue: true, "Enable Hover text for bee hive. [Synced with Server]", synchronizedSetting: true);
            hoverFermenter = config("Hover - Stations", "Fermenter Hover", defaultValue: StationHover.Vanilla, "Hover text for fermenter.");
            hoverPlant = config("Hover - Stations", "Plants Hover", defaultValue: StationHover.Vanilla, "Hover text for plants.");
            hoverCooking = config("Hover - Stations", "Cooking stations Hover", defaultValue: StationHover.Vanilla, "Hover text for cooking stations.");
            hoverBeeHive = config("Hover - Stations", "Bee Hive Hover", defaultValue: StationHover.Vanilla, "Hover text for bee hive.");
            hoverBeeHiveTotal = config("Hover - Stations", "Bee Hive Show total", defaultValue: true, "Show total needed time/percent for bee hive.");

            hoverTame = config("Hover - Tameable", "Tameable Hover", defaultValue: StationHover.Vanilla, "Format of total needed time/percent to tame or to stay fed.");
            hoverTameTimeToTame = config("Hover - Tameable", "Show time to tame", defaultValue: true, "Show total needed time/percent to tame. [Synced with Server]", synchronizedSetting: true);
            hoverTameTimeToFed = config("Hover - Tameable", "Show time to stay fed", defaultValue: true, "Show total needed time/percent to stay fed. [Synced with Server]", synchronizedSetting: true);

            hoverSmelterEstimatedTime = config("Hover - Smelters", "Show estimated time", defaultValue: true, "Show estimated end time for a smelter station (charcoal kiln, forge, etc. including non vanilla). [Synced with Server]", synchronizedSetting: true);
            hoverSmelterShowFuelAndItem = config("Hover - Smelters", "Always show fuel and item", defaultValue: true, "Show current smelting item and fuel loaded on both fuel and ore switches.");
            hoverSmelterShowQueuedItems = config("Hover - Smelters", "Show queued items", defaultValue: true, "Show queued items currently being smelted. Doesn't show the list if there is only one item to smelt. [Synced with Server]", synchronizedSetting: true);

            chestCustomName = config("Hover - Chests", "Enable custom names", defaultValue: true, "Enable custom names for chests. [Synced with Server]", synchronizedSetting: true);
            chestHoverItems = config("Hover - Chests", "Hover items format", defaultValue: ChestItemsHover.Vanilla, "Chest items details format to be shown in hover.");
            chestHoverName = config("Hover - Chests", "Hover name format", defaultValue: ChestNameHover.TypeThenCustomName, "Chest name format to be shown in hover.");
            chestShowRename = config("Hover - Chests", "Show rename hint in hover", defaultValue: true, "Show rename hotkey hint. You can hide it to make it less noisy.");
            chestShowHoldToStack = config("Hover - Chests", "Show hold to stack hint in hover", defaultValue: true, "Show hold to stack hint. You can hide it to make it less noisy.");

            chestContentEnabled = config("Hover - Chests - Content", "Enable chest content", defaultValue: true, "Enable custom names for chests. [Synced with Server]", synchronizedSetting: true);
            chestContentLinesToShow = config("Hover - Chests - Content", "Lines to show", defaultValue: 11, "Amount of lines to be shown.");
            chestContentSortType = config("Hover - Chests - Content", "Sorting type", defaultValue: ContentSortType.Position, "Sorting type. Position means item position in chest grid.");
            chestContentSortDir = config("Hover - Chests - Content", "Sorting direction", defaultValue: ContentSortDir.Asc, "Sorting direction.");
            chestContentEntryFormat = config("Hover - Chests - Content", "Entry format", defaultValue: "{1} {0}", "0 for item name, 1 for total amount");
            chestContentAmountColor = config("Hover - Chests - Content", "Entry amount color", defaultValue: new Color(1f, 1f, 0f, 0.6f), "Color for amount");
            chestContentItemColor = config("Hover - Chests - Content", "Entry item name color", defaultValue: new Color(0.75f, 0.75f, 0.75f, 0.6f), "Color for item name");

            chestContentEnabled.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentLinesToShow.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentSortType.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentSortDir.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentEntryFormat.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentItemColor.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();
            chestContentAmountColor.SettingChanged += (sender, args) => ChestHoverText.ResetHoverCache();

            statusEffectsPositionEnabled = config("Status effects - Map - List", "Enable", defaultValue: true, "Enable repositioning of status effect list. [Synced with Server]", synchronizedSetting: true);
            statusEffectsPositionAnchor = config("Status effects - Map - List", "Position", defaultValue: new Vector2(-170f, -265f), "Anchored position of list.");
            statusEffectsFillingDirection = config("Status effects - Map - List", "Direction", defaultValue: ListDirection.TopToBottom, "Direction of filling");
            statusEffectsPositionSpacing = config("Status effects - Map - List", "Spacing", defaultValue: 8, "Spacing between status effects");

            statusEffectsElementEnabled = config("Status effects - Map - List element", "Custom element enabled", defaultValue: true, "Enables using of horizontal status effect element [Synced with Server]", synchronizedSetting: true);
            statusEffectsElementSize = config("Status effects - Map - List element", "Size", defaultValue: 32, "Vertical capsule size");

            statusEffectsPositionEnabled.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsPositionAnchor.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsElementEnabled.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsElementSize.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();

            sailingIndicatorEnabled = config("Status effects - Map - Sailing indicator", "Enabled", defaultValue: true, "Enable changing of sailing indicator [Synced with Server]", synchronizedSetting: true);
            sailingIndicatorPowerIconPosition = config("Status effects - Map - Sailing indicator", "Sail power indicator position", defaultValue: new Vector2(-350f, -290f), "Sail size and rudder indicator position");
            sailingIndicatorPowerIconScale = config("Status effects - Map - Sailing indicator", "Sail power indicator scale", defaultValue: 1.0f, "Sail size and rudder indicator scale");
            sailingIndicatorWindIndicatorPosition = config("Status effects - Map - Sailing indicator", "Wind indicator position", defaultValue: new Vector2(-350f, -140f), "Wind indicator (ship and wind direction) position");
            sailingIndicatorWindIndicatorScale = config("Status effects - Map - Sailing indicator", "Wind indicator scale", defaultValue: 1.0f, "Wind indicator (ship and wind direction) scale");

            sailingIndicatorEnabled.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorPowerIconPosition.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorPowerIconScale.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorWindIndicatorPosition.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorWindIndicatorScale.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();

            statusEffectsPositionEnabledNomap = config("Status effects - Nomap - List", "Enable", defaultValue: true, "Enable repositioning of status effect list. [Synced with Server]", synchronizedSetting: true);
            statusEffectsPositionAnchorNomap = config("Status effects - Nomap - List", "Position", defaultValue: new Vector2(-170f, -70f), "Anchored position of list.");
            statusEffectsFillingDirectionNomap = config("Status effects - Nomap - List", "Direction", defaultValue: ListDirection.TopToBottom, "Direction of filling");
            statusEffectsPositionSpacingNomap = config("Status effects - Nomap - List", "Spacing", defaultValue: 10, "Spacing between status effects");

            statusEffectsElementEnabledNomap = config("Status effects - Nomap - List element", "Custom element enabled", defaultValue: true, "Enables using of horizontal status effect element [Synced with Server]", synchronizedSetting: true);
            statusEffectsElementSizeNomap = config("Status effects - Nomap - List element", "Size", defaultValue: 36, "Vertical capsule size");

            statusEffectsPositionEnabledNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsPositionAnchorNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsElementEnabledNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();
            statusEffectsElementSizeNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.InitializeStatusEffectTemplate();

            sailingIndicatorEnabledNomap = config("Status effects - Nomap - Sailing indicator", "Enabled", defaultValue: true, "Enable changing of sailing indicator [Synced with Server]", synchronizedSetting: true);
            sailingIndicatorPowerIconPositionNomap = config("Status effects - Nomap - Sailing indicator", "Sail power indicator position", defaultValue: new Vector2(-350f, -320f), "Sail size and rudder indicator position");
            sailingIndicatorPowerIconScaleNomap = config("Status effects - Nomap - Sailing indicator", "Sail power indicator scale", defaultValue: 1.1f, "Sail size and rudder indicator scale");
            sailingIndicatorWindIndicatorPositionNomap = config("Status effects - Nomap - Sailing indicator", "Wind indicator position", defaultValue: new Vector2(-350f, -170f), "Wind indicator (ship and wind direction) position");
            sailingIndicatorWindIndicatorScaleNomap = config("Status effects - Nomap - Sailing indicator", "Wind indicator scale", defaultValue: 1.1f, "Wind indicator (ship and wind direction) scale");

            sailingIndicatorEnabledNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorPowerIconPositionNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorPowerIconScaleNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorWindIndicatorPositionNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();
            sailingIndicatorWindIndicatorScaleNomap.SettingChanged += (sender, args) => CustomStatusEffectsList.ChangeSailingIndicator();

            showWeight = config("Info - Inventory", "Show current weight", defaultValue: true, "Enable showing weight (current / max) [Synced with Server]", synchronizedSetting: true);
            showSlots = config("Info - Inventory", "Show current slots", defaultValue: true, "Enable current slots (emtpy / max) [Synced with Server]", synchronizedSetting: true);
            weightBackgroundColor = config("Info - Inventory", "Weight background", defaultValue: new Color(0f, 0f, 0f, 0.5f), "Color of weight block background");
            slotsBackgroundColor = config("Info - Inventory", "Slots background", defaultValue: new Color(0f, 0f, 0f, 0.5f), "Color of slots block background");
            showWeightLeft = config("Info - Inventory", "Show weight left until encumbered", defaultValue: false, "Invert weight value. Show how much weight you can take until encumbered.");
            showSlotsTaken = config("Info - Inventory", "Show slots space taken", defaultValue: false, "Invern current slots. Show how much space is taken.");

            showWeightLeft.SettingChanged += (sender, args) => InventoryPanel.UpdateStats();
            showSlotsTaken.SettingChanged += (sender, args) => InventoryPanel.UpdateStats();

            weightSlotsFine = config("Info - Inventory - Colors", "1 - Fine", defaultValue: new Color(0.11765f, 0.72941f, 0.03529f, 1f), "Color of amount < 50%.");
            weightSlotsHalf = config("Info - Inventory - Colors", "2 - Half", defaultValue: new Color(0.72941f, 0.72941f, 0.03529f, 1f), "Color of amount > 50%.");
            weightSlotsALot = config("Info - Inventory - Colors", "3 - A lot", defaultValue: new Color(0.72941f, 0.34902f, 0.03529f, 1f), "Color of amount > 75%.");
            weightSlotsFull = config("Info - Inventory - Colors", "4 - Full", defaultValue: new Color(0.72941f, 0.03529f, 0.03529f, 1f), "Color of amount >= 100%.");

            weightSlotsFine.SettingChanged += (sender, args) => InventoryPanel.UpdateGradient();
            weightSlotsHalf.SettingChanged += (sender, args) => InventoryPanel.UpdateGradient();
            weightSlotsALot.SettingChanged += (sender, args) => InventoryPanel.UpdateGradient();
            weightSlotsFull.SettingChanged += (sender, args) => InventoryPanel.UpdateGradient();

            weightPosition = config("Info - Inventory", "Weight position", defaultValue: new Vector2(-898f, -276.1f), "Position from middle of the screen");
            slotsPosition = config("Info - Inventory", "Slots position", defaultValue: new Vector2(-898f, -209.9f), "Position from middle of the screen");

            weightFontColor = config("Info - Inventory", "Weight font color", defaultValue: new Color(1f, 0.848f, 0, 1f), "Font color.");
            slotsFontColor = config("Info - Inventory", "Slots font color", defaultValue: new Color(1f, 0.848f, 0, 1f), "Font color.");
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = false)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, defaultValue, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = false) => config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

        private void LoadIcons()
        {
            LoadIcon("Clear.png", ref WeatherForecast.iconClear);
            LoadIcon("Rain.png", ref WeatherForecast.iconRain);
            LoadIcon("Snow.png", ref WeatherForecast.iconSnow);
            LoadIcon("Thunder.png", ref WeatherForecast.iconThunder);
            LoadIcon("Mist.png", ref WeatherForecast.iconMist);
            LoadIcon("RainCinder.png", ref WeatherForecast.iconRainCinder);
        }

        internal static void LoadIcon(string filename, ref Sprite icon)
        {
            Texture2D tex = new Texture2D(2, 2);
            if (LoadTexture(filename, ref tex))
                icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }

        internal static bool LoadTexture(string filename, ref Texture2D tex)
        {
            string fileInConfigFolder = Path.Combine(Paths.PluginPath, filename);
            if (File.Exists(fileInConfigFolder))
            {
                LogInfo($"Loaded image: {fileInConfigFolder}");
                return tex.LoadImage(File.ReadAllBytes(fileInConfigFolder));
            }

            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            string name = executingAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));

            Stream resourceStream = executingAssembly.GetManifestResourceStream(name);

            byte[] data = new byte[resourceStream.Length];
            resourceStream.Read(data, 0, data.Length);

            tex.name = Path.GetFileNameWithoutExtension(filename);

            return tex.LoadImage(data, true);
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

                if (!hoverFermenterEnabled.Value || hoverFermenter.Value == StationHover.Vanilla)
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

                if (!hoverPlantEnabled.Value || hoverPlant.Value == StationHover.Vanilla)
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

                if (!hoverBeeHiveEnabled.Value || hoverBeeHive.Value == StationHover.Vanilla)
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

                if (!hoverCookingEnabled.Value || hoverCooking.Value == StationHover.Vanilla)
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

                if (!hoverCookingEnabled.Value || hoverCooking.Value == StationHover.Vanilla)
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

                if (hoverCharacter.Value != StationHover.Vanilla && hoverCharacterGrowth.Value && __instance.TryGetComponent(out Growup growup) && growup.m_growTime != 0f)
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

                if (__instance.TryGetComponent(out Procreation procreation))
                {
                    if (hoverCharacterLovePoints.Value && procreation.m_requiredLovePoints > 0)
                    {
                        int lovePoints = ___m_nview.GetZDO().GetInt(ZDOVars.s_lovePoints);
                        __result += $"\n♥: {lovePoints}/{procreation.m_requiredLovePoints}";
                    }

                    if (hoverCharacter.Value != StationHover.Vanilla && hoverCharacterProcreation.Value && procreation.IsPregnant())
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

                TMP_Text text = elementRoot.transform.Find("res_amount")?.GetComponent<TMP_Text>();
                if (int.TryParse(text.text, out _))
                    text?.SetText(text.text + $" <color=#{ColorUtility.ToHtmlStringRGBA(availableItemsAmountColor.Value)}>({player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name)})</color>");
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

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetBoundKeyString))]
        public static class ZInput_GetBoundKeyString_NonlocalizedButtons
        {
            public static bool mapUpdated = false;
            public static readonly List<string> keyCodeValues = Enum.GetValues(typeof(Key)).OfType<Key>().Where(key => 15 <= (int)key && (int)key <= 40).Select(key => key.ToString()).ToList();

            public static void OnChange()
            {
                mapUpdated = false;
            }

            public static void Prefix()
            {
                if (!modEnabled.Value)
                    return;

                if (mapUpdated)
                    return;

                mapUpdated = true;

                if (modEnabled.Value && nonlocalizedButtons.Value)
                    keyCodeValues.Do(key => ZInput.s_keyLocalizationMap[key] = key);
                else
                    keyCodeValues.Do(key => ZInput.s_keyLocalizationMap.Remove(key));
            }
        }

        public void UpdateCraftingPanel() => CraftFilter.UpdateCraftingPanel();
    }
}

