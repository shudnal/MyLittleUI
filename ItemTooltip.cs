using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static ClutterSystem;
using static ItemDrop;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    public class ItemTooltip
    {
        private const string projectileTooltipGroup = "projectile";

        private static readonly Dictionary<string, string> localizedTooltipTokens = new Dictionary<string, string>();
        private static readonly List<string> tails = new List<string>();
        private static readonly List<string> tokens = new List<string>();
        private static readonly HashSet<string> craftingTokens = new HashSet<string>();

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
                "$se_staminaregen",
                "$item_newgameplusitem",
                "$item_tamedonly"
            };

            foreach (string token in tokens)
            {
                localizedTooltipTokens[Localization.instance.Localize(token)] = token;
                localizedTooltipTokens[token] = token;
            }

            craftingTokens.Clear();
            craftingTokens.Add("$item_weight");
            craftingTokens.Add("$item_quality");
            craftingTokens.Add("$item_durability");
            craftingTokens.Add("$item_repairlevel");
            craftingTokens.Add("$item_blockpower");
            craftingTokens.Add("$item_blockarmor");
            craftingTokens.Add("$item_blockforce");

            craftingTokens.Add("$inventory_damage");
            craftingTokens.Add("$inventory_blunt");
            craftingTokens.Add("$inventory_slash");
            craftingTokens.Add("$inventory_pierce");
            craftingTokens.Add("$inventory_fire");
            craftingTokens.Add("$inventory_frost");
            craftingTokens.Add("$inventory_lightning");
            craftingTokens.Add("$inventory_poison");
            craftingTokens.Add("$inventory_spirit");
        }

        private static void InitializeTokenGroups()
        {
            tokens.Clear();
            tokens.Add("$item_dlc");
            tokens.Add("$item_newgameplusitem");
            tokens.Add("$item_onehanded");
            tokens.Add("$item_twohanded");
            tokens.Add("$item_noteleport");
            tokens.Add("$item_value");
            tokens.Add("$item_tamedonly");

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

        private static void ReorderTooltip(ItemDrop.ItemData item, int m_quality, float m_worldLevel, bool upgradingTooltip)
        {
            bool addDelimiter = false;
            foreach (string token in tokens)
            {
                if (token == "")
                {
                    if (addDelimiter)
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
                    addDelimiter = AppendToken(token, item, m_quality, m_worldLevel, upgradingTooltip) || addDelimiter;
            }
        }

        private static bool AppendToken(string token, ItemDrop.ItemData item, int m_quality, float m_worldLevel, bool upgrading)
        {
            if (!tokenPositions.ContainsKey(token))
                return false;

            List<int> tokPos = tokenPositions[token];
            for (int i = 0; i < tokPos.Count; i++)
                sb.AppendFormat("\n{0}", GetTokenString(tokPos, i, token, item, m_quality, m_worldLevel, upgrading));

            return true;
        }

        private static string GetTokenString(List<int> tokPos, int i, string token, ItemDrop.ItemData item, int m_quality, float m_worldLevel, bool upgrading)
        {
            if (!upgrading || i > 0 || !craftingTokens.Contains(token))
                return arrResult[tokPos[i]];

            string statString = arrResult[tokPos[i]];
            int index = statString.IndexOf(": ");
            if (index == -1)
                return statString;

            index += 2;

            if (token.StartsWith("$inventory_"))
            {
                HitData.DamageTypes damagesNew = item.GetDamage(m_quality, m_worldLevel);
                HitData.DamageTypes damagesCurrent = item.GetDamage(m_quality - 1, m_worldLevel);
                Player.m_localPlayer.GetSkills().GetRandomSkillRange(out var min, out var max, item.m_shared.m_skillType);

                float damage = GetDamageByToken(token, damagesCurrent);
                if (damage != -1f && damage != GetDamageByToken(token, damagesNew))
                    return statString.Insert(index, GetStringUpgradeFrom(damagesCurrent.DamageRange(damage, min, max)));
            }
            else if (token == "$item_weight")
            {
                if (item.m_shared.m_scaleWeightByQuality != 0f)
                {
                    int currentQuality = item.m_quality;
                    item.m_quality = m_quality - 1;
                    string weight = item.GetWeight().ToString("0.0");
                    item.m_quality = currentQuality;
                    
                    return statString.Insert(index, GetStringUpgradeFrom(weight));
                }
            }
            else if (token == "$item_quality")
                return statString.Insert(index, GetStringUpgradeFrom(String.Format("<color=orange>{0}</color>", m_quality - 1)));
            else if (token == "$item_durability" && item.GetMaxDurability(m_quality) != item.GetMaxDurability(m_quality - 1))
                return statString.Insert(index, GetStringUpgradeFrom(String.Format("<color=orange>{0}</color>", item.GetMaxDurability(m_quality - 1))));
            else if (token == "$item_blockarmor" && item.GetBaseBlockPower(m_quality) != item.GetBaseBlockPower(m_quality - 1))
                return statString.Insert(index, GetStringUpgradeFrom(String.Format("<color=orange>{0}</color> <color=yellow>({1})</color>", item.GetBaseBlockPower(m_quality - 1), item.GetBlockPowerTooltip(m_quality - 1).ToString("0"))));
            else if (token == "$item_blockforce" && item.GetDeflectionForce(m_quality) != item.GetDeflectionForce(m_quality - 1))
                return statString.Insert(index, GetStringUpgradeFrom(String.Format("<color=orange>{0}</color>", item.GetDeflectionForce(m_quality - 1))));

            return statString;


            string GetStringUpgradeFrom(string value)
            {
                return $"{value.Replace("<color=yellow>", "<color=silver>").Replace("<color=orange>", "<color=lightblue>")} → ";
            }
        }

        private static float GetDamageByToken(string token, HitData.DamageTypes damages)
        {
            return token switch
            {
                "$inventory_damage" => damages.m_damage,
                "$inventory_blunt" => damages.m_blunt,
                "$inventory_slash" => damages.m_slash,
                "$inventory_pierce" => damages.m_pierce,
                "$inventory_fire" => damages.m_fire,
                "$inventory_frost" => damages.m_frost,
                "$inventory_lightning" => damages.m_lightning,
                "$inventory_poison" => damages.m_poison,
                "$inventory_spirit" => damages.m_spirit,
                _ => -1f,
            };
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        private class InventoryGui_Awake_ItemTooltipCraftingFontSize
        {
            private static void Postfix(InventoryGui __instance)
            {
                TMPro.TextMeshProUGUI description = __instance.m_recipeDecription?.GetComponent<TMPro.TextMeshProUGUI>();
                if (description != null)
                    description.fontSizeMin = 12;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float))]
        private class ItemDropItemData_GetTooltip_ItemTooltip
        {
            [HarmonyPriority(Priority.First)]
            private static void Postfix(ItemDrop.ItemData item, int qualityLevel, bool crafting, float worldLevel, ref string __result)
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
                string statusEffect = item.GetStatusEffectTooltip(qualityLevel, Player.m_localPlayer.GetSkillLevel(item.m_shared.m_skillType));
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
                ReorderTooltip(item, qualityLevel, worldLevel, upgradingTooltip: crafting && 1 < qualityLevel && qualityLevel <= item.m_shared.m_maxQuality);

                if (footerIndex != -1)
                    sb.Append(footer);

                __result = sb.ToString();
                
                // Use hex code for EpicLoot to not change it to lightblue
                __result = __result.Replace("<color=orange>", itemTooltipColored.Value ? "<color=#ffa500ff>" : "<color=#add8e6ff>")
                                   .Replace("<color=yellow>", itemTooltipColored.Value ? "<color=#ffff00ff>" : "<color=#c0c0c0ff>")
                                   .Replace("<color=silver>", "<color=#c0c0c0ff>")
                                   .Replace("<color=lightblue>", "<color=#add8e6ff>")
                                   .Replace("\n\n\n", "\n\n");
                ;
            }
        }

    }
}
