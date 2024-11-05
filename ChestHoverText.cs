using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    internal class ChestItem
    {
        public int m_stack;
        public int m_value;
        public string m_name;
        public float m_weight;
        public ItemDrop.ItemData.ItemType m_itemType;

        public ChestItem(ItemDrop.ItemData itemData)
        {
            m_name = itemData.m_shared.m_name;
            m_itemType = itemData.m_shared.m_itemType;
        }
    }

    internal static class ChestHoverText
    {
        private static Container textInputForContainer;
        private static readonly Dictionary<string, string> hoverTextCache = new Dictionary<string, string>();

        internal static void ResetCache(Container container)
        {
            if (container == null || container.m_nview == null || !container.m_nview.IsValid())
                return;

            hoverTextCache.Remove(container.m_nview.GetZDO().ToString());
        }

        internal static void ResetHoverCache()
        {
            hoverTextCache.Clear();
        }

        [HarmonyPatch(typeof(Container), nameof(Container.OnContainerChanged))]
        private static class Container_OnContainerChanged_HoverCacheReset
        {
            private static void Postfix(Container __instance)
            {
                if (!modEnabled.Value)
                    return;

                ResetCache(__instance);
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
        private static class Container_GetHoverText_Duration
        {
            private static readonly StringBuilder result = new StringBuilder();

            private static void ReorderItemList(ref List<ChestItem> itemsInChest)
            {
                if (chestContentSortType.Value == ContentSortType.Position)
                    return;

                if (chestContentSortDir.Value == ContentSortDir.Desc)
                {
                    if (chestContentSortType.Value == ContentSortType.Name)
                        itemsInChest = itemsInChest.OrderByDescending(item => item.m_name).ToList();
                    else if (chestContentSortType.Value == ContentSortType.Weight)
                        itemsInChest = itemsInChest.OrderByDescending(item => item.m_weight).ToList();
                    else if (chestContentSortType.Value == ContentSortType.Amount)
                        itemsInChest = itemsInChest.OrderByDescending(item => item.m_stack).ToList();
                    else if (chestContentSortType.Value == ContentSortType.Value)
                        itemsInChest = itemsInChest.OrderByDescending(item => item.m_value).ToList();
                }
                else
                {
                    if (chestContentSortType.Value == ContentSortType.Name)
                        itemsInChest = itemsInChest.OrderBy(item => item.m_name).ToList();
                    else if (chestContentSortType.Value == ContentSortType.Weight)
                        itemsInChest = itemsInChest.OrderBy(item => item.m_weight).ToList();
                    else if (chestContentSortType.Value == ContentSortType.Amount)
                        itemsInChest = itemsInChest.OrderBy(item => item.m_stack).ToList();
                    else if (chestContentSortType.Value == ContentSortType.Value)
                        itemsInChest = itemsInChest.OrderBy(item => item.m_value).ToList();
                }
            }

            private static void AddChestContent(Container container)
            {
                if (container.GetInventory().NrOfItems() == 0)
                    return;

                result.Append("\n");

                List<ChestItem> itemsInChest = new List<ChestItem>();
                foreach (ItemDrop.ItemData itemData in container.GetInventory().GetAllItemsInGridOrder())
                {
                    ChestItem item = itemsInChest.Find(item => item.m_name == itemData.m_shared.m_name);
                    if (item == null)
                    {
                        item = new ChestItem(itemData);
                        itemsInChest.Add(item);
                    }
                        
                    item.m_stack += itemData.m_stack;
                    item.m_value += itemData.GetValue();
                    item.m_weight += itemData.GetWeight();
                }

                ReorderItemList(ref itemsInChest);

                string itemFormat = $"<color=#{ColorUtility.ToHtmlStringRGBA(chestContentItemColor.Value)}>{{0}}</color>";
                string amountFormat = $"<color=#{ColorUtility.ToHtmlStringRGBA(chestContentAmountColor.Value)}>{{0}}</color>";

                int misc = 0; int armor = 0; int consumable = 0; int weapon = 0; int material = 0;
                for (int i = 0; i < itemsInChest.Count; i++)
                {
                    ChestItem item = itemsInChest[i];
                    if (i < chestContentLinesToShow.Value)
                    {
                        AddItemLine(item.m_name, item.m_stack);
                        continue;
                    }

                    switch (item.m_itemType)
                    {
                        case ItemDrop.ItemData.ItemType.Consumable:
                            consumable++;
                            break;
                        case ItemDrop.ItemData.ItemType.Material:
                            material++;
                            break;
                        case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                        case ItemDrop.ItemData.ItemType.Bow:
                        case ItemDrop.ItemData.ItemType.Shield:
                        case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                        case ItemDrop.ItemData.ItemType.Torch:
                        case ItemDrop.ItemData.ItemType.Tool:
                        case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                        case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                            weapon++;
                            break;
                        case ItemDrop.ItemData.ItemType.Helmet:
                        case ItemDrop.ItemData.ItemType.Chest:
                        case ItemDrop.ItemData.ItemType.Customization:
                        case ItemDrop.ItemData.ItemType.Legs:
                        case ItemDrop.ItemData.ItemType.Hands:
                        case ItemDrop.ItemData.ItemType.Shoulder:
                        case ItemDrop.ItemData.ItemType.Utility:
                            armor++;
                            break;
                        default:
                            misc++;
                            break;
                    }
                }

                if (misc + armor + consumable + weapon + material == 0)
                    return;

                result.Append("\n+");

                if (weapon != 0)
                    AddItemLine("$radial_handitems", weapon);

                if (armor != 0)
                    AddItemLine("$radial_armor_utility", armor);
                
                if (consumable != 0)
                    AddItemLine("$radial_consumables", consumable);
                
                if (material != 0)
                    AddItemLine("$hud_crafting", material);

                if (misc != 0)
                    AddItemLine("$hud_misc", misc);

                void AddItemLine(string itemName, int amount)
                {
                    result.AppendFormat("\n" + chestContentEntryFormat.Value, string.Format(itemFormat, itemName), string.Format(amountFormat, amount));
                }
            }

            private static void Postfix(Container __instance, ref string __result, bool ___m_checkGuardStone, string ___m_name, Inventory ___m_inventory)
            {
                if (!modEnabled.Value)
                    return;

                if (chestHoverName.Value == ChestNameHover.Vanilla && chestHoverItems.Value == ChestItemsHover.Vanilla)
                    return;

                if (__instance.m_nview == null || !__instance.m_nview.IsValid())
                    return;

                if (___m_checkGuardStone && !PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false))
                    return;

                string containerID = __instance.m_nview.GetZDO().ToString();

                if (hoverTextCache.TryGetValue(containerID, out string hoverText))
                {
                    __result = hoverText;
                    return;
                }

                result.Clear();

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

                if (__instance.CheckAccess(Game.instance.GetPlayerProfile().GetPlayerID()))
                {
                    result.Append(" ");

                    if (chestHoverItems.Value == ChestItemsHover.Percentage)
                        result.Append($"{___m_inventory.SlotsUsedPercentage():F0}%");
                    else if (chestHoverItems.Value == ChestItemsHover.FreeSlots)
                        result.Append($"{___m_inventory.GetEmptySlots()}");
                    else if (chestHoverItems.Value == ChestItemsHover.ItemsMaxRoom)
                        result.Append($"{___m_inventory.NrOfItems()}/{___m_inventory.GetWidth() * ___m_inventory.GetHeight()}");
                    else if (___m_inventory.NrOfItems() == 0)
                        result.Append("( $piece_container_empty )");

                    result.Append("\n[<color=#ffff00ff><b>$KEY_Use</b></color>] $piece_container_open");

                    if (chestShowHoldToStack.Value)
                        result.Append(" $msg_stackall_hover");

                    if (chestShowRename.Value)
                        if (!ZInput.IsNonClassicFunctionality() || !ZInput.IsGamepadActive())
                            result.Append("\n[<color=#ffff00ff><b>$KEY_AltPlace + $KEY_Use</b></color>] $hud_rename");
                        else
                            result.Append("\n[<color=#ffff00ff><b>$KEY_JoyAltKeys + $KEY_Use</b></color>] $hud_rename");

                    if (chestContentEnabled.Value)
                        AddChestContent(__instance);
                }

                __result = Localization.instance.Localize(result.ToString());

                hoverTextCache.Add(containerID, __result);
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
        private class Container_Interact_ChestRename
        {
            private static bool Prefix(Container __instance, Humanoid character, bool hold, bool alt, bool ___m_checkGuardStone)
            {
                if (!modEnabled.Value)
                    return true;

                if (!chestCustomName.Value)
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
            private static void Postfix(string text)
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
    }
}