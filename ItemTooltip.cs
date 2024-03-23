using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    public class ItemTooltip
    {
        private const string projectileTooltipGroup = "projectile";

        private static readonly Dictionary<string, string> localizedTooltipTokens = new Dictionary<string, string>();
        private static readonly List<string> tails = new List<string>();
        private static readonly List<string> tokens = new List<string>();

        private static readonly StringBuilder sb = new StringBuilder();
        private static readonly Dictionary<string, List<int>> tokenPositions = new Dictionary<string, List<int>>();
        private static readonly List<string> arrResult = new List<string>();

        public static void Initialize()
        {
            FillTooltipTails();
            InitTooltipTokens();
            InitializeTokenGroups();
        }

        private static void FillTooltipTails()
        {
            tails.Clear();
            if (epicLootPlugin != null)
            {
                var methodGetRarityColor = AccessTools.Method(epicLootPlugin.GetType(), "GetRarityColor");
                var enumItemRarity = AccessTools.TypeByName("EpicLoot.ItemRarity");
                if (methodGetRarityColor != null && enumItemRarity != null)
                    foreach (var rarity in enumItemRarity.GetEnumValues())
                    {
                        tails.Add($"<color={methodGetRarityColor.Invoke(methodGetRarityColor, new[] { rarity })}>\n");
                        tails.Add($"\n<color={methodGetRarityColor.Invoke(methodGetRarityColor, new[] { rarity })}>");
                    }
            }
            tails.Add("\n\n$item_seteffect");
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
                "$inventory_spirit",
                "$se_staminaregen"
            };

            foreach (string token in tokens)
            {
                localizedTooltipTokens[Localization.instance.Localize(token)] = token;
                localizedTooltipTokens[token] = token;
            }
        }

        private static void InitializeTokenGroups()
        {
            tokens.Clear();
            tokens.Add("$item_dlc");
            tokens.Add("$item_onehanded");
            tokens.Add("$item_twohanded");
            tokens.Add("$item_noteleport");
            tokens.Add("$item_value");

            tokens.Add("");

            tokens.Add("$item_food_health");
            tokens.Add("$item_food_stamina");
            tokens.Add("$item_food_eitr");
            tokens.Add("$item_food_duration");
            tokens.Add("$item_food_regen");
            tokens.Add("$se_staminaregen");

            tokens.Add("");

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

            tokens.Add("");

            tokens.Add("$item_blockpower");
            tokens.Add("$item_blockarmor");
            tokens.Add("$item_blockforce");
            tokens.Add("$item_deflection");
            tokens.Add("$item_parrybonus");

            tokens.Add(projectileTooltipGroup);
            
            tokens.Add("$item_armor");
            tokens.Add("$inventory_dmgmod");

            tokens.Add("");

            tokens.Add("$item_durability");
            tokens.Add("$item_repairlevel");

            tokens.Add("");

            tokens.Add("$item_movement_modifier");
            tokens.Add("$item_eitrregen_modifier");
            tokens.Add("$base_item_modifier");

            tokens.Add("");

            tokens.Add("$item_weight");
            tokens.Add("$item_crafter");
            tokens.Add("$item_quality");
        }

        private static void ReorderTooltip(ItemDrop.ItemData item, int m_quality, int m_worldLevel)
        {
            bool addDelimiter = false;
            foreach (string token in tokens)
            {
                if (token == "" && addDelimiter)
                {
                    sb.Append('\n');
                    addDelimiter = false;
                }
                else if (token == projectileTooltipGroup)
                {
                    string projectileTooltip = item.GetProjectileTooltip(m_quality);
                    if (projectileTooltip.Length > 0)
                    {
                        addDelimiter = true;
                        sb.Append("\n\n");
                        sb.Append(projectileTooltip);
                    }

                    if (addDelimiter)
                        sb.Append('\n');

                    addDelimiter = false;
                }
                else
                    addDelimiter = AppendToken(token) || addDelimiter;
            }
        }

        private static bool AppendToken(string key)
        {
            if (!tokenPositions.ContainsKey(key))
                return false;

            List<int> tokPos = tokenPositions[key];
            for (int i = 0; i < tokPos.Count; i++)
                sb.AppendFormat("\n{0}", arrResult[tokPos[i]]);

            return true;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        private class InventoryGui_Awake_ItemTooltipCraftingFontSize
        {
            private static void Postfix(InventoryGui __instance)
            {
                if (__instance.m_recipeDecription == null)
                    return;

                TMPro.TextMeshProUGUI description = __instance.m_recipeDecription.GetComponent<TMPro.TextMeshProUGUI>();
                description.fontSizeMin = 12;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float))]
        private class ItemDropItemData_GetTooltip_ItemTooltip
        {
            [HarmonyPriority(Priority.First)]
            private static void Postfix(ItemDrop.ItemData item, ref string __result, int ___m_quality, int ___m_worldLevel)
            {
                if (!modEnabled.Value)
                    return;

                if (!itemTooltip.Value)
                    return;

                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    return;

                if (item == null)
                    return;

                int descriptionEnd = __result.IndexOf("\n\n", StringComparison.InvariantCulture);
                if (descriptionEnd == -1)
                    return;

                sb.Clear();

                // Decription is not needed to be touched, anything that is before first \n\n considered description
                string description = __result.Substring(0, descriptionEnd + 2);
                __result = __result.Substring(description.Length);
                sb.Append(description);

                // End of tooltip is not needed to be touched, anything that is after first status effect, EpicLoot magic tooltip or item set info
                int footerIndex = -1;
                string statusEffect = item.GetStatusEffectTooltip(___m_quality, Player.m_localPlayer.GetSkillLevel(item.m_shared.m_skillType));
                if (!String.IsNullOrEmpty(statusEffect))
                    tails.Insert(0, "\n\n" + statusEffect.Substring(0, statusEffect.IndexOf("</color>\n", StringComparison.OrdinalIgnoreCase)));

                foreach (string tailString in tails)
                {
                    footerIndex = __result.IndexOf(tailString, StringComparison.InvariantCulture);
                    if (footerIndex != -1)
                        break;
                }

                if (!String.IsNullOrEmpty(statusEffect))
                    tails.RemoveAt(0);

                string footer = "";
                if (footerIndex != -1)
                {
                    footer = __result.Substring(footerIndex);
                    __result = __result.Substring(0, footerIndex);
                }

                tokenPositions.Clear();
                arrResult.Clear();

                // Result now stripped of description and footer and should only consist of tokens
                arrResult.AddRange(__result.Split(new char[] { '\n' }, StringSplitOptions.None).ToList());

                for (int i = 0; i < arrResult.Count; i++)
                {
                    if (arrResult[i] == "\n")
                        continue;

                    var tokens = localizedTooltipTokens.Where(kvp => arrResult[i].IndexOf(kvp.Key) > -1).ToList();

                    if (tokens.Count() > 0)
                    {
                        if (tokenPositions.ContainsKey(tokens[0].Value))
                            tokenPositions[tokens[0].Value].Add(i);
                        else
                            tokenPositions.Add(tokens[0].Value, new List<int> { i });
                    }
                    else
                    {
                        // if string doesn't have known token - add it to last added token
                        if (tokenPositions.Count > 0)
                            tokenPositions.Last().Value.Add(i);
                        else
                        {
                            // if there is no tokens yet - add it to resulting string directly
                            sb.Append(arrResult[i]);
                            sb.Append("\n");
                            arrResult.RemoveAt(i);
                            i--;
                        }
                    }
                }

                // Regroup tokens by new order respecting original string formats
                ReorderTooltip(item, ___m_quality, ___m_worldLevel);

                if (footerIndex != -1)
                    sb.Append(footer);

                __result = sb.ToString();
                
                // Use hex code for EpicLoot to not change it to lightblue
                __result = __result.Replace("<color=orange>", itemTooltipColored.Value ? "<color=#ffa500ff>" : "<color=#add8e6ff>");
                __result = __result.Replace("<color=yellow>", itemTooltipColored.Value ? "<color=#ffff00ff>" : "<color=#add8e6ff>");
            }
        }

    }
}
