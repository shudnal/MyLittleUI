using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
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
        const string pluginVersion = "1.0.0";

        private Harmony _harmony;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> loggingEnabled;

        private static ConfigEntry<bool> durabilityEnabled;
        private static ConfigEntry<Color> durabilityFine;
        private static ConfigEntry<Color> durabilityWorn;
        private static ConfigEntry<Color> durabilityAtRisk;
        private static ConfigEntry<Color> durabilityBroken;

        private static ConfigEntry<float> itemIconScale;

        private static ConfigEntry<bool> itemTooltip;

        private static ConfigEntry<bool> statsMainMenu;
        private static ConfigEntry<bool> statsMainMenuAdvanced;
        private static ConfigEntry<bool> statsMainMenuAll;

        private static ConfigEntry<bool> statsCharacter;

        private static ConfigEntry<StationHover> hoverFermenter;
        private static ConfigEntry<StationHover> hoverPlant;
        private static ConfigEntry<StationHover> hoverCooking;
        private static ConfigEntry<StationHover> hoverBeeHive;
        private static ConfigEntry<bool> hoverBeeHiveTotal;

        private static ConfigEntry<bool> hoverSmelterEstimatedTime;
        private static ConfigEntry<bool> hoverSmelterShowFuelAndItem;
        private static ConfigEntry<bool> hoverSmelterShowQueuedItems;

        private static ConfigEntry<ChestItemsHover> chestHoverItems;
        private static ConfigEntry<ChestNameHover> chestHoverName;
        private static ConfigEntry<bool> chestCustomName;
        private static ConfigEntry<bool> chestShowHoldToStack;
        private static ConfigEntry<bool> chestShowRename;

        private static Vector3 itemIconScaleOriginal = Vector3.zero;
        private static Container textInputForContainer;

        private static MyLittleUI instance;

        private static UITooltip characterStatsTooltip;
        private static UITooltip characterEffectsTooltip;

        private static readonly Dictionary<string, string> localizedTooltipTokens = new Dictionary<string, string>();

        private static bool epicLootEnabled = false;

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

            var EpicLootPlugin = GetComponent("EpicLoot");
            if (EpicLootPlugin != null)
            {
                epicLootEnabled = true;

                /*var EpicLootPluginType = EpicLootPlugin.GetType();
                var IsAdventureModeEnabledMethod = AccessTools.Method(EpicLootPluginType, "IsAdventureModeEnabled");
                if (IsAdventureModeEnabledMethod != null)
                {*/
                    // (bool)MethodInvoker.GetHandler(IsAdventureModeEnabledMethod)(null);
                    LogInfo($"EpicLoot found.");
                
            }

            InitTooltipTokens();

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

            durabilityEnabled = Config.Bind("Item - Durability", "0 - Enabled", defaultValue: true, "Enable color of durability.");
            durabilityFine = Config.Bind("Item - Durability", "1 - Fine", defaultValue: new Color(0.11765f, 0.72941f, 0.03529f, 1f), "Color of durability > 75%.");
            durabilityWorn = Config.Bind("Item - Durability", "2 - Worn", defaultValue: new Color(0.72941f, 0.72941f, 0.03529f, 1f), "Color of durability > 50%.");
            durabilityAtRisk = Config.Bind("Item - Durability", "3 - At risk", defaultValue: new Color(0.72941f, 0.34902f, 0.03529f, 1f), "Color of durability > 25%.");
            durabilityBroken = Config.Bind("Item - Durability", "4 - Broken", defaultValue: new Color(0.72941f, 0.03529f, 0.03529f, 1f), "Color of durability >= 0%.");

            itemIconScale = Config.Bind("Item - Icon", "Icon scale", defaultValue: 1.0f, "Relative scale size of item icons.");

            itemTooltip = Config.Bind("Item - Tooltip", "Enabled", defaultValue: true, "Updated item tooltip");

            statsMainMenu = Config.Bind("Stats - Main menu", "Show stats in main menu", defaultValue: true, "Show character statistics in main menu");
            statsMainMenuAdvanced = Config.Bind("Stats - Main menu", "Show advanced stats in main menu", defaultValue: true, "Show advanced character statistics in main menu");
            statsMainMenuAll = Config.Bind("Stats - Main menu", "Show all stats in main menu", defaultValue: false, "Show all character statistics in main menu");

            statsCharacter = Config.Bind("Stats - Character", "Show character stats on armor hover", defaultValue: true, "Show character stats in armor tooltip");

            hoverFermenter = Config.Bind("Hover - Stations", "Fermenter Hover", defaultValue: StationHover.Vanilla, "Hover text for fermenter.");
            hoverPlant = Config.Bind("Hover - Stations", "Plants Hover", defaultValue: StationHover.Vanilla, "Hover text for plants.");
            hoverCooking = Config.Bind("Hover - Stations", "Cooking stations Hover", defaultValue: StationHover.Vanilla, "Hover text for cooking stations.");
            hoverBeeHive = Config.Bind("Hover - Stations", "Bee Hive Hover", defaultValue: StationHover.Vanilla, "Hover text for bee hive.");
            hoverBeeHiveTotal = Config.Bind("Hover - Stations", "Bee Hive Show total", defaultValue: true, "Show total needed time/percent for bee hive.");

            hoverSmelterEstimatedTime = Config.Bind("Hover - Smelters", "Show estimated time", defaultValue: true, "Show estimated end time for a smelter station (charcoal kiln, forge, etc. including non vanilla).");
            hoverSmelterShowFuelAndItem = Config.Bind("Hover - Smelters", "Always show fuel and item", defaultValue: true, "Show current smelting item and fuel loaded on both fuel and ore switches.");
            hoverSmelterShowQueuedItems = Config.Bind("Hover - Smelters", "Show queued items", defaultValue: true, "Show queued items currently being smelted. Doesn't show the list if there is only one item to smelt.");

            chestCustomName = Config.Bind("Hover - Chests", "Enable custom names", defaultValue: true, "Enable custom names for chests.");
            chestHoverItems = Config.Bind("Hover - Chests", "Hover items format", defaultValue: ChestItemsHover.Vanilla, "Chest items details format to be shown in hover.");
            chestHoverName = Config.Bind("Hover - Chests", "Hover name format", defaultValue: ChestNameHover.TypeThenCustomName, "Chest name format to be shown in hover.");
            chestShowRename = Config.Bind("Hover - Chests", "Show rename hint in hover", defaultValue: true, "Show rename hotkey hint. You can hide it to make it less noisy.");
            chestShowHoldToStack = Config.Bind("Hover - Chests", "Show hold to stack hint in hover", defaultValue: true, "Show hold to stack hint. You can hide it to make it less noisy.");
        }

        private static void InitTooltipTokens()
        {
            string[] tokens =
            {
                "$item_dlc",
                "$item_onehanded",
                "$item_twohanded",
                "$item_crafter",
                "$item_noteleport",
                "$item_value",
                "$item_weight",
                "$item_quality",
                "$item_durability",
                "$item_repairlevel",
                "$item_food_health",
                "$item_food_stamina",
                "$item_food_eitr",
                "$item_food_duration",
                "$item_food_regen",
                "$item_staminause",
                "$item_eitruse",
                "$item_healthuse",
                "$item_staminahold",
                "$item_knockback",
                "$item_backstab",
                "$item_blockpower",
                "$item_blockarmor",
                "$item_blockforce",
                "$item_deflection",
                "$item_parrybonus",
                "$item_armor",
                "$item_movement_modifier",
                "$item_eitrregen_modifier",
                "$base_item_modifier",
                "$item_seteffect",
                "$inventory_dmgmod",
                "$inventory_damage",
                "$inventory_blunt",
                "$inventory_slash",
                "$inventory_pierce",
                "$inventory_fire",
                "$inventory_frost",
                "$inventory_lightning",
                "$inventory_poison",
                "$inventory_spirit"
            };

            foreach (string token in tokens)
            {
                localizedTooltipTokens[Localization.instance.Localize(token)] = token;
                localizedTooltipTokens[token] = token;
            }
        }

        private static string FromSeconds(double seconds)
        {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            return ts.ToString(ts.Hours > 0 ? @"h\:mm\:ss" : @"m\:ss");
        }

        private static void UpdateItemIcon(GuiBar durability, Image icon, ItemDrop.ItemData item)
        {
            if (itemIconScaleOriginal == Vector3.zero)
                itemIconScaleOriginal = icon.transform.localScale;

            if (itemIconScale.Value != 1f)
                icon.transform.localScale = itemIconScaleOriginal * Mathf.Clamp(itemIconScale.Value, 0.2f, 2f);

            if (durabilityEnabled.Value && item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability())
            {
                if (item.m_durability <= 0f)
                {
                    durability.SetValue(1f);
                    durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? durabilityBroken.Value : new Color(0f, 0f, 0f, 0f));
                }
                else
                {
                    float percentage = item.GetDurabilityPercentage();
                    durability.SetValue(percentage);
                    if (percentage >= 0.75f)
                        durability.SetColor(durabilityFine.Value);
                    else if (percentage >= 0.50f)
                        durability.SetColor(durabilityWorn.Value);
                    else if (percentage >= 0.25f)
                        durability.SetColor(durabilityAtRisk.Value);
                    else
                        durability.SetColor(durabilityBroken.Value);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        private class InventoryGrid_UpdateGui_DurabilityAndScale
        {
            private static void Postfix(InventoryGrid __instance, Inventory ___m_inventory)
            {
                if (!modEnabled.Value) return;

                int width = ___m_inventory.GetWidth();

                foreach (ItemDrop.ItemData item in ___m_inventory.GetAllItems())
                {
                    InventoryGrid.Element element = __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, width);

                    UpdateItemIcon(element.m_durability, element.m_icon, item);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCharacterStats))]
        private class InventoryGui_UpdateCharacterStats_CharacterStats
        {
            private static void InitCharacterStats(InventoryGui __instance, Player player)
            {
                UITooltip prefabTooltip = __instance.m_containerGrid.m_elementPrefab.GetComponent<UITooltip>();
                FieldInfo[] fields = prefabTooltip.GetType().GetFields();

                characterStatsTooltip = __instance.m_armor.transform.parent.gameObject.AddComponent<UITooltip>();

                foreach (FieldInfo field in fields)
                {
                    field.SetValue(characterStatsTooltip, field.GetValue(prefabTooltip));
                }

                characterStatsTooltip.m_topic = player.GetPlayerName();

                characterEffectsTooltip = __instance.m_weight.transform.parent.gameObject.AddComponent<UITooltip>();

                foreach (FieldInfo field in fields)
                {
                    field.SetValue(characterEffectsTooltip, field.GetValue(prefabTooltip));
                }

                characterEffectsTooltip.m_topic = "$inventory_activeeffects";

                LogInfo("Character inventory stats patched");
            }

            private static void AddRegenStat(ref float stat, float multiplier)
            {
                if (multiplier > 1f)
                {
                    stat += multiplier - 1f;
                }
                else
                {
                    stat *= multiplier;
                }
            }

            private static bool ShouldOverride(HitData.DamageModifier a, HitData.DamageModifier b)
            {
                if (a == HitData.DamageModifier.Ignore)
                {
                    return false;
                }

                if (b == HitData.DamageModifier.Immune)
                {
                    return true;
                }

                if (a == HitData.DamageModifier.VeryResistant && b == HitData.DamageModifier.Resistant)
                {
                    return false;
                }

                if (a == HitData.DamageModifier.VeryWeak && b == HitData.DamageModifier.Weak)
                {
                    return false;
                }

                if ((a == HitData.DamageModifier.Resistant || a == HitData.DamageModifier.VeryResistant || a == HitData.DamageModifier.Immune) && (b == HitData.DamageModifier.Weak || b == HitData.DamageModifier.VeryWeak))
                {
                    return false;
                }

                return true;
            }

            private static string TooltipEffects(Player player)
            {
                StringBuilder sb = new StringBuilder();
                if (player.GetEquipmentMovementModifier() != 0f)
                {
                    string color = player.GetEquipmentMovementModifier() >= 0 ? "#00FF00" : "#FF0000";
                    sb.AppendFormat("$item_movement_modifier: <color={0}>{1}%</color>\n", color, player.GetEquipmentMovementModifier() * 100f);
                }

                if (player.GetEquipmentBaseItemModifier() != 0f)
                {
                    string color = player.GetEquipmentBaseItemModifier() >= 0 ? "#00FF00" : "#FF0000";
                    sb.AppendFormat("$base_item_modifier: <color={0}>{1}%</color>\n", color, player.GetEquipmentBaseItemModifier() * 100f);
                }

                Dictionary<Skills.SkillType, float> skills = new Dictionary<Skills.SkillType, float>();
                Dictionary<HitData.DamageType, HitData.DamageModifier> mods = new Dictionary<HitData.DamageType, HitData.DamageModifier>();

                SE_Stats stats = (SE_Stats)ScriptableObject.CreateInstance("SE_Stats");

                foreach (StatusEffect statusEffect in player.GetSEMan().GetStatusEffects().ToList())
                {
                    if (statusEffect is SE_Stats)
                    {
                        SE_Stats se = (statusEffect as SE_Stats);

                        stats.m_jumpStaminaUseModifier *= se.m_jumpStaminaUseModifier;
                        stats.m_runStaminaDrainModifier *= se.m_runStaminaDrainModifier;
                        stats.m_healthOverTime += se.m_healthOverTime;
                        stats.m_staminaOverTime += se.m_staminaOverTime;
                        stats.m_eitrOverTime += se.m_eitrOverTime;
                        AddRegenStat(ref stats.m_healthRegenMultiplier, se.m_healthRegenMultiplier);
                        AddRegenStat(ref stats.m_staminaRegenMultiplier, se.m_staminaRegenMultiplier);

                        if (player.GetMaxEitr() > 0)
                            AddRegenStat(ref stats.m_eitrRegenMultiplier, se.m_eitrRegenMultiplier);

                        stats.m_addMaxCarryWeight += se.m_addMaxCarryWeight;
                        stats.m_noiseModifier *= se.m_noiseModifier;
                        stats.m_stealthModifier *= se.m_stealthModifier;
                        stats.m_speedModifier *= se.m_speedModifier;
                        stats.m_maxMaxFallSpeed += se.m_maxMaxFallSpeed;
                        stats.m_fallDamageModifier += se.m_fallDamageModifier;

                        if (skills.ContainsKey(se.m_skillLevel))
                            skills[se.m_skillLevel] += se.m_skillLevelModifier;
                        else
                            skills.Add(se.m_skillLevel, se.m_skillLevelModifier);

                        if (skills.ContainsKey(se.m_skillLevel2))
                            skills[se.m_skillLevel2] += se.m_skillLevelModifier2;
                        else
                            skills.Add(se.m_skillLevel2, se.m_skillLevelModifier2);

                        foreach (HitData.DamageModPair mod in se.m_mods)
                            if (mods.ContainsKey(mod.m_type))
                            {
                                if (ShouldOverride(mods[mod.m_type], mod.m_modifier))
                                    mods[mod.m_type] = mod.m_modifier;
                            }
                            else
                                mods[mod.m_type] = mod.m_modifier;
                    }
                    else
                    {
                        string tooltips = statusEffect.GetTooltipString().Replace(statusEffect.m_tooltip, "");
                        if (tooltips.IsNullOrWhiteSpace())
                            continue;

                        sb.Append("\n");
                        sb.Append("<color=orange>" + statusEffect.m_name + "</color>");
                        sb.Append(tooltips);

                    }
                }

                List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>
                {
                    player.m_chestItem,
                    player.m_legItem,
                    player.m_helmetItem,
                    player.m_shoulderItem,
                    player.m_leftItem,
                    player.m_rightItem,
                    player.m_utilityItem
                };

                foreach (ItemDrop.ItemData item in items)
                {
                    if (item == null || item.m_shared.m_damageModifiers.Count == 0)
                        continue;

                    foreach (HitData.DamageModPair mod in item.m_shared.m_damageModifiers)
                        if (mods.ContainsKey(mod.m_type))
                        {
                            if (ShouldOverride(mods[mod.m_type], mod.m_modifier))
                                mods[mod.m_type] = mod.m_modifier;
                        }
                        else
                            mods[mod.m_type] = mod.m_modifier;
                }

                foreach (KeyValuePair<HitData.DamageType, HitData.DamageModifier> mod in mods)
                {
                    stats.m_mods.Add(new HitData.DamageModPair { m_modifier = mod.Value, m_type = mod.Key });
                }

                if (player.GetMaxEitr() > 0)
                    stats.m_eitrRegenMultiplier += player.GetEquipmentEitrRegenModifier();

                string tooltip = stats.GetTooltipString();
                if (!tooltip.IsNullOrWhiteSpace())
                {
                    sb.Append("\n");
                    sb.Append(tooltip);
                }

                foreach (KeyValuePair<Skills.SkillType, float> skill in skills)
                {
                    if (skill.Value != 0)
                        sb.AppendFormat("\n{0} <color=orange>{1}</color>", "$skill_" + skill.Key.ToString().ToLower(), skill.Value.ToString("+0;-0"));
                }

                return Localization.instance.Localize(sb.ToString());
            }

            private static string TooltipStats(Player player)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("$item_armor: <color=orange>{0}</color>", player.GetBodyArmor());

                ItemDrop.ItemData weapon = player.GetCurrentWeapon();
                if (weapon != null)
                {
                    HitData.DamageTypes damage = weapon.GetDamage(weapon.m_quality, Game.m_worldLevel);

                    if (weapon.m_shared.m_skillType == Skills.SkillType.Bows && player.GetAmmoItem() != null)
                    {
                        damage.Add(player.GetAmmoItem().GetDamage());
                    }

                    sb.Append("\n");
                    ItemDrop.ItemData.AddHandedTip(weapon, sb);
                    sb.Append($"{damage.GetTooltipString(weapon.m_shared.m_skillType)}");
                    sb.Append($"\n$item_knockback: <color=orange>{weapon.m_shared.m_attackForce}</color>");
                    sb.Append($"\n$item_backstab: <color=orange>{weapon.m_shared.m_backstabBonus}x</color>");
                }

                ItemDrop.ItemData shield = player.GetCurrentBlocker();
                if (shield != null)
                {
                    int qualityLevel = shield.m_quality;

                    sb.Append("\n");
                    sb.Append($"\n$item_blockpower: <color=orange>{shield.GetBlockPowerTooltip(qualityLevel).ToString("0")}</color>");
                    if (shield.m_shared.m_timedBlockBonus > 1f)
                    {
                        sb.Append($"\n$item_blockforce: <color=orange>{shield.GetDeflectionForce(qualityLevel)}</color>");
                        sb.Append($"\n$item_parrybonus: <color=orange>{shield.m_shared.m_timedBlockBonus}x</color>");
                    }

                    string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(shield.m_shared.m_damageModifiers);
                    if (damageModifiersTooltipString.Length > 0)
                    {
                        sb.Append(damageModifiersTooltipString);
                    }
                }

                return Localization.instance.Localize(sb.ToString());
            }

            private static void Postfix(InventoryGui __instance, Player player)
            {
                if (!modEnabled.Value)
                    return;

                if (!statsCharacter.Value)
                    return;

                if (characterStatsTooltip == null)
                    InitCharacterStats(__instance, player);

                characterStatsTooltip.m_text = TooltipStats(player);

                characterEffectsTooltip.m_text = TooltipEffects(player);
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        private class HotkeyBar_UpdateIcons_DurabilityAndScale
        {
            private static void Postfix(HotkeyBar __instance, Player player, List<ItemDrop.ItemData> ___m_items, List<HotkeyBar.ElementData> ___m_elements)
            {
                if (!modEnabled.Value) return;

                if (!player || player.IsDead()) return;

                for (int j = 0; j < ___m_items.Count; j++)
                {
                    ItemDrop.ItemData item = ___m_items[j];
                    HotkeyBar.ElementData element = ___m_elements[item.m_gridPos.x];

                    UpdateItemIcon(element.m_durability, element.m_icon, item);
                }
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
        private class Container_GetHoverText_Duration
        {
            private static void Postfix(Container __instance, ref string __result, bool ___m_checkGuardStone, string ___m_name, Inventory ___m_inventory)
            {
                if (!modEnabled.Value)
                    return;

                if (chestHoverName.Value == ChestNameHover.Vanilla && chestHoverItems.Value == ChestItemsHover.Vanilla)
                    return;

                if (___m_checkGuardStone && !PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false))
                    return;

                StringBuilder result = new StringBuilder();

                string chestName = __instance.m_nview.GetZDO().GetString(ZDOVars.s_text);

                if (chestHoverName.Value == ChestNameHover.Vanilla || !chestCustomName.Value || chestName.IsNullOrWhiteSpace())
                    result.Append(___m_name);
                else if (chestHoverName.Value == ChestNameHover.CustomName)
                    result.Append(chestName);
                else if (chestHoverName.Value == ChestNameHover.TypeThenCustomName)
                {
                    result.Append(___m_name);
                    result.Append(" (");
                    result.Append(chestName);
                    result.Append(")");
                }
                else if (chestHoverName.Value == ChestNameHover.CustomNameThenType)
                {
                    result.Append(chestName);
                    result.Append(" (");
                    result.Append(___m_name);
                    result.Append(")");
                }

                result.Append(" ");

                if (chestHoverItems.Value == ChestItemsHover.Percentage)
                    result.Append($"{___m_inventory.SlotsUsedPercentage():F0}%");
                else if (chestHoverItems.Value == ChestItemsHover.FreeSlots)
                    result.Append($"{___m_inventory.GetEmptySlots()}");
                else if (chestHoverItems.Value == ChestItemsHover.ItemsMaxRoom)
                    result.Append($"{___m_inventory.NrOfItems()}/{___m_inventory.GetWidth() * ___m_inventory.GetHeight()}");
                else if (___m_inventory.NrOfItems() == 0)
                    result.Append("( $piece_container_empty )");

                result.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open");

                if (chestShowHoldToStack.Value)
                    result.Append(" $msg_stackall_hover");

                long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
                if (__instance.CheckAccess(playerID) && chestShowRename.Value)
                    if (!ZInput.IsNonClassicFunctionality() || !ZInput.IsGamepadActive())
                        result.Append("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $hud_rename");
                    else
                        result.Append("\n[<color=yellow><b>$KEY_JoyAltKeys + $KEY_Use</b></color>] $hud_rename");

                __result = Localization.instance.Localize(result.ToString());
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
        private class Container_Interact_ChestRename
        {
            private static bool Prefix(Container __instance, Humanoid character, bool hold, bool alt, bool ___m_checkGuardStone)
            {
                if (!modEnabled.Value)
                    return true;

                if (!alt)
                    return true;

                if (hold)
                    return true;

                if (___m_checkGuardStone && !PrivateArea.CheckAccess(__instance.transform.position))
                {
                    character.Message(MessageHud.MessageType.Center, "$piece_noaccess");
                    return true;
                }

                long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
                if (!__instance.CheckAccess(playerID))
                {
                    character.Message(MessageHud.MessageType.Center, "$piece_noaccess");
                    return true;
                }

                textInputForContainer = __instance;
                TextInput.instance.Show("$hud_rename " + __instance.m_name, __instance.m_nview.GetZDO().GetString(ZDOVars.s_text), 32);

                return false;
            }
        }

        [HarmonyPatch(typeof(TextInput), nameof(TextInput.setText))]
        private class TextInput_setText_ChestRename
        {
            private static void Postfix(TextInput __instance, string text)
            {
                if (!modEnabled.Value)
                    return;

                if (textInputForContainer == null)
                    return;

                textInputForContainer.m_nview.GetZDO().Set(ZDOVars.s_text, text);
                textInputForContainer.OnContainerChanged();
                textInputForContainer = null;
            }
        }

        [HarmonyPatch(typeof(TextInput), nameof(TextInput.Hide))]
        private class TextInput_Hide_ChestRename
        {
            private static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                if (textInputForContainer == null)
                    return;

                textInputForContainer = null;
            }
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
                StringBuilder sb = new StringBuilder();

                if (___m_nview.IsOwner())
                {

                    for (int slot = 0; slot < __instance.m_slots.Length; slot++)
                    {
                        __instance.GetSlot(slot, out string itemName, out float cookedTime, out CookingStation.Status status);
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
                }

                return ___m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + ___m_addItemTooltip + (ZInput.GamepadActive ? "" : ("\n[<color=yellow><b>1-8</b></color>] " + ___m_addItemTooltip)) + Localization.instance.Localize(sb.ToString());
            }

            private static void Postfix(CookingStation __instance, Switch ___m_addFoodSwitch, string ___m_addItemTooltip, string ___m_name, ZNetView ___m_nview)
            {
                if (!modEnabled.Value)
                    return;

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

                StringBuilder sb = new StringBuilder();

                for (int slot = 0; slot < __instance.m_slots.Length; slot++)
                {
                    __instance.GetSlot(slot, out string itemName, out float cookedTime, out CookingStation.Status status);
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
            private static void Postfix(Smelter __instance, ref string __result, string ___m_name, ItemDrop ___m_fuelItem, int ___m_maxFuel, int ___m_maxOre, int ___m_fuelPerProduct, float ___m_secPerProduct, Windmill ___m_windmill)
            {
                if (!modEnabled.Value)
                    return;

                if (!hoverSmelterShowFuelAndItem.Value && !hoverSmelterEstimatedTime.Value)
                    return;

                float fuel = __instance.GetFuel();
                int queueSize = __instance.GetQueueSize();

                StringBuilder sb = new StringBuilder();
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

                StringBuilder sb = new StringBuilder();
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
                    if (Char.IsUpper(c) && builder.Length > 0) builder.Append(' ');
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

            private static void Postfix(FejdStartup __instance, TMPro.TMP_Text ___m_csSourceInfo, List<PlayerProfile> ___m_profiles, int ___m_profileIndex)
            {
                if (!modEnabled.Value)
                    return;

                if (!statsMainMenu.Value)
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
                        AddStat(PlayerStatType.SetGuardianPower);
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

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float))]
        private class ItemDropItemData_GetTooltip_ItemTooltip
        {
            private static readonly StringBuilder sb = new StringBuilder();
            private static readonly Dictionary<string, List<int>> tokenPositions = new Dictionary<string, List<int>>();

            private static void RebuildTooltip(string[] arrResult, ItemDrop.ItemData item, int ___m_quality, int ___m_worldLevel)
            {
                float skillLevel = Player.m_localPlayer.m_skills.GetSkillLevel(item.m_shared.m_skillType);
                string statusEffectTooltip = item.GetStatusEffectTooltip(___m_quality, skillLevel);
                bool statusEffectTooltipAdded = false;

                List<string> tokens = new List<string>();
                
                tokens.Add("$item_dlc");
                tokens.Add("$item_onehanded");
                tokens.Add("$item_twohanded");
                tokens.Add("$item_noteleport");
                tokens.Add("$item_value");

                if (AppendTokenGroup(tokens, arrResult))
                    sb.Append('\n');

                tokens.Clear();
                tokens.Add("$item_food_health");
                tokens.Add("$item_food_stamina");
                tokens.Add("$item_food_eitr");
                tokens.Add("$item_food_duration");
                tokens.Add("$item_food_regen");

                if (AppendTokenGroup(tokens, arrResult) && statusEffectTooltip.Length > 0)
                {
                    sb.Append("\n\n");
                    sb.Append(statusEffectTooltip);
                    sb.Append('\n');
                    statusEffectTooltipAdded = true;
                }

                tokens.Clear();
                tokens.Add("$inventory_damage");
                tokens.Add("$inventory_blunt");
                tokens.Add("$inventory_slash");
                tokens.Add("$inventory_pierce");
                tokens.Add("$inventory_fire");
                tokens.Add("$inventory_frost");
                tokens.Add("$inventory_lightning");
                tokens.Add("$inventory_poison");
                tokens.Add("$inventory_spirit");

                tokens.Add("$item_knockback");
                tokens.Add("$item_backstab");
                tokens.Add("$item_staminause");
                tokens.Add("$item_eitruse");
                tokens.Add("$item_healthuse");
                tokens.Add("$item_staminahold");

                if (AppendTokenGroup(tokens, arrResult))
                    sb.Append('\n');

                tokens.Clear();
                tokens.Add("$item_blockpower");
                tokens.Add("$item_blockarmor");
                tokens.Add("$item_blockforce");
                tokens.Add("$item_deflection");
                tokens.Add("$item_parrybonus");

                if (AppendTokenGroup(tokens, arrResult))
                {
                    string projectileTooltip = item.GetProjectileTooltip(___m_quality);
                    if (projectileTooltip.Length > 0)
                    {
                        sb.Append("\n\n");
                        sb.Append(projectileTooltip);
                    }
                    
                    if (statusEffectTooltip.Length > 0 && !statusEffectTooltipAdded)
                    {
                        sb.Append("\n\n");
                        sb.Append(statusEffectTooltip);
                        statusEffectTooltipAdded = true;
                    }

                    sb.Append('\n');
                }

                tokens.Clear();
                tokens.Add("$item_armor");
                tokens.Add("$inventory_dmgmod");

                if (AppendTokenGroup(tokens, arrResult) && statusEffectTooltip.Length > 0 && !statusEffectTooltipAdded)
                {
                    sb.Append("\n\n");
                    sb.Append(statusEffectTooltip);
                    sb.Append('\n');
                    statusEffectTooltipAdded = true;
                }

                tokens.Clear();
                tokens.Add("$item_durability");
                tokens.Add("$item_repairlevel");

                if (AppendTokenGroup(tokens, arrResult))
                    sb.Append('\n');

                tokens.Clear();
                tokens.Add("$item_movement_modifier");
                tokens.Add("$item_eitrregen_modifier");
                tokens.Add("$base_item_modifier");

                if (AppendTokenGroup(tokens, arrResult))
                    sb.Append('\n');

                tokens.Clear();
                tokens.Add("$item_seteffect");

                if (AppendTokenGroup(tokens, arrResult))
                    sb.Append('\n');

                tokens.Clear();
                tokens.Add("$item_weight");
                tokens.Add("$item_crafter"); 
                tokens.Add("$item_quality");

                AppendTokenGroup(tokens, arrResult);
            }

            private static bool AppendToken(string key, string[] arrResult)
            {
                if (!tokenPositions.ContainsKey(key))
                {
                    LogInfo($"no {key}");
                    return false;
                }

                List<int> tokPos = tokenPositions[key];
                for (int i = 0; i < tokPos.Count; i++)
                    sb.AppendFormat("\n{0}", arrResult[tokPos[i]]);

                return true;
            }

            private static bool AppendTokenGroup(List<string> tokens, string[] arrResult)
            {
                bool addDelimiter = false;
                for (int i = 0; i < tokens.Count; i++)
                {
                     addDelimiter = addDelimiter || AppendToken(tokens[i], arrResult);
                }

                return addDelimiter;
            }

            [HarmonyPriority(Priority.First)]
            private static void Postfix(ItemDrop.ItemData item, ref string __result, int ___m_quality, int ___m_worldLevel)
            {
                if (!modEnabled.Value)
                    return;

                if (!itemTooltip.Value)
                    return;

                if (item == null)
                    return;

                tokenPositions.Clear();
                sb.Clear();

                string[] arrResult = __result.Split(new char[] { '\n' }, StringSplitOptions.None);
                int startLine = -1;
                int endLine = 0;

                for (int i = 0; i < arrResult.Length; i++)
                {
                    if (arrResult[i] == "\n")
                        continue;

                    var tokens = localizedTooltipTokens.Where(kvp => arrResult[i].IndexOf(kvp.Key) > -1).ToList();

                    if (tokens.Count() > 0)
                    {
                        
                        if (tokenPositions.ContainsKey(tokens[0].Value))
                            tokenPositions[tokens[0].Value].Add(i);
                        else
                            tokenPositions.Add(tokens[0].Value, new List<int>{ i });

                        if (startLine == -1)
                            startLine = i;
                        endLine = i;
                    }
                    else
                    {
                        if (tokenPositions.Count > 0)
                            tokenPositions.Last().Value.Add(i);
                    }
                }

                List<string> newResult = new List<string>();

                for (int i = 0; i < startLine; i++)
                {
                    sb.Append(arrResult[i]);
                    sb.Append("\n");
                }

                RebuildTooltip(arrResult, item, ___m_quality, ___m_worldLevel);

                for (int i = endLine + 1; i < arrResult.Length; i++)
                {
                    sb.Append("\n");
                    sb.Append(arrResult[i]);
                }

                LogInfo(__result);
                LogInfo("---------------------");

                foreach (KeyValuePair<string, List<int>> token in tokenPositions)
                {
                    LogInfo($"{token.Key}:");
                    token.Value.ForEach(tok => LogInfo(arrResult[tok]));
                }
                LogInfo("_____________________");
                __result = sb.ToString();
                __result = __result.Replace("<color=orange>", "<color=#ffa500ff>");
                __result = __result.Replace("<color=yellow>", "<color=#ffff00ff>");

                LogInfo(__result);
                //LogInfo($"{startLine} - {endLine}");
                //LogInfo($"{__result}");

            }
        }
    }
}

