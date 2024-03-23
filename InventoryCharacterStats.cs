using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    internal class InventoryCharacterStats
    {
        private static UITooltip characterStatsTooltip;
        private static UITooltip characterEffectsTooltip;

        private static readonly StringBuilder sb = new StringBuilder();
        private static readonly Dictionary<Skills.SkillType, float> skills = new Dictionary<Skills.SkillType, float>();
        private static readonly Dictionary<HitData.DamageType, HitData.DamageModifier> mods = new Dictionary<HitData.DamageType, HitData.DamageModifier>();

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
                stat += multiplier - 1f;
            else
                stat *= multiplier;
        }

        private static bool ShouldOverride(HitData.DamageModifier a, HitData.DamageModifier b)
        {
            if (a == HitData.DamageModifier.Ignore)
                return false;

            if (b == HitData.DamageModifier.Immune)
                return true;

            if (a == HitData.DamageModifier.VeryResistant && b == HitData.DamageModifier.Resistant)
                return false;

            if (a == HitData.DamageModifier.VeryWeak && b == HitData.DamageModifier.Weak)
                return false;

            if ((a == HitData.DamageModifier.Resistant || a == HitData.DamageModifier.VeryResistant || a == HitData.DamageModifier.Immune) && (b == HitData.DamageModifier.Weak || b == HitData.DamageModifier.VeryWeak))
                return false;

            return true;
        }

        private static string TooltipEffects(Player player)
        {
            sb.Clear();
            if (player.GetEquipmentMovementModifier() != 0f)
            {
                string color = player.GetEquipmentMovementModifier() >= 0 ? "#00FF00" : "#FF0000";
                sb.AppendFormat("$item_movement_modifier: <color={0}>{1:P0}</color>\n", color, player.GetEquipmentMovementModifier());
            }

            if (player.GetEquipmentBaseItemModifier() != 0f)
            {
                string color = player.GetEquipmentBaseItemModifier() >= 0 ? "#00FF00" : "#FF0000";
                sb.AppendFormat("$base_item_modifier: <color={0}>{1:P0}</color>\n", color, player.GetEquipmentBaseItemModifier());
            }

            skills.Clear();
            mods.Clear();

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
            sb.Clear();
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

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCharacterStats))]
        private class InventoryGui_UpdateCharacterStats_CharacterStats
        {
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
    }
}
