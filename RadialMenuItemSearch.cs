using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyLittleUI
{
    /// <summary>
    /// Side-effect-free item discovery for contextual radial menus.
    /// </summary>
    public static class RadialMenuItemSearch
    {
        private static bool UseNativeItemSearch => MyLittleUI.radialMenuUseNativeItemSearch?.Value == true;

        // Public by design: these are stable compatibility patch points for mods that provide additional item sources.
        // Patch these methods instead of adding side effects to Valheim lookup/check methods.
        // These methods are UI probes and must never consume, move, reserve, or otherwise mutate items.
        // Actual item consumption belongs to the normal interaction path after the player selects an item.
        public static bool CanUseItems(Player player, GameObject hoverObject, bool sendErrorMessage = true)
        {
            if (!player || !hoverObject)
                return false;

            RadialMenuItemReceiver receiver = hoverObject.GetComponentInParent<RadialMenuItemReceiver>();
            if (receiver && !receiver.IsActive)
                return false;

            if (UseNativeItemSearch && TryGetNativeHoverMenu(hoverObject, out IHasHoverMenu hoverMenu, out IHasHoverMenuExtended extendedHoverMenu))
            {
                if (hoverMenu != null)
                    return hoverMenu.CanUseItems(player, sendErrorMessage);

                return extendedHoverMenu.CanUseItems(
                    player,
                    hoverObject.GetComponent<Switch>(),
                    sendErrorMessage);
            }

            if (receiver)
                return receiver.CanUseItems(player, sendErrorMessage);

            if (!CanTryGetInventoryItems(hoverObject))
                return false;

            return CanUseInventoryItems(player, hoverObject, sendErrorMessage);
        }

        // See the compatibility contract above. Mods that intentionally extend contextual radial item sources
        // should patch both TryGetItems and CanUseItems so availability and the item list stay consistent.
        public static bool TryGetItems(
            Player player,
            GameObject hoverObject,
            out List<string> items,
            bool sendErrorMessage = true)
        {
            items = new List<string>();
            if (!player || !hoverObject)
                return false;

            RadialMenuItemReceiver receiver = hoverObject.GetComponentInParent<RadialMenuItemReceiver>();
            if (receiver && !receiver.IsActive)
                return false;

            if (UseNativeItemSearch && TryGetNativeHoverMenu(hoverObject, out IHasHoverMenu hoverMenu, out IHasHoverMenuExtended extendedHoverMenu))
            {
                if (hoverMenu != null)
                    return hoverMenu.TryGetItems(player, out items);

                return extendedHoverMenu.TryGetItems(
                    player,
                    hoverObject.GetComponent<Switch>(),
                    out items);
            }

            if (receiver)
                return receiver.TryGetItems(player, sendErrorMessage, out items);

            if (!IsInventorySearchTarget(hoverObject) || !CanTryGetInventoryItems(hoverObject))
                return false;

            if (!CanUseItems(player, hoverObject, sendErrorMessage))
                return true;

            return TryGetInventoryItems(player, hoverObject, out items);
        }

        internal static bool IsSearchTarget(GameObject hoverObject)
        {
            if (!hoverObject)
                return false;

            RadialMenuItemReceiver receiver = hoverObject.GetComponentInParent<RadialMenuItemReceiver>();
            if (receiver)
                return receiver.IsActive;

            if (UseNativeItemSearch)
                return TryGetNativeHoverMenu(hoverObject, out _, out _);

            return IsInventorySearchTarget(hoverObject)
                || TryGetNativeHoverMenu(hoverObject, out _, out _);
        }

        internal static bool HasAnyInventoryItem(Player player, IEnumerable<string> itemNames)
        {
            Inventory inventory = player?.GetInventory();
            if (inventory == null || itemNames == null)
                return false;

            foreach (string itemName in itemNames)
            {
                if (!string.IsNullOrWhiteSpace(itemName) && inventory.HaveItem(itemName))
                    return true;
            }

            return false;
        }

        private static bool TryGetNativeHoverMenu(
            GameObject hoverObject,
            out IHasHoverMenu hoverMenu,
            out IHasHoverMenuExtended extendedHoverMenu)
        {
            hoverMenu = null;
            extendedHoverMenu = null;

            MonoBehaviour[] components = hoverObject.GetComponentsInParent<MonoBehaviour>();
            hoverMenu = components
                .Where(component => !(component is RadialMenuItemReceiver))
                .OfType<IHasHoverMenu>()
                .FirstOrDefault();
            if (hoverMenu != null)
                return true;

            extendedHoverMenu = components.OfType<IHasHoverMenuExtended>().FirstOrDefault();
            return extendedHoverMenu != null;
        }

        private static bool IsInventorySearchTarget(GameObject hoverObject)
            => hoverObject.GetComponentInParent<Fireplace>()
                || hoverObject.GetComponentInParent<Turret>()
                || hoverObject.GetComponentInParent<CookingStation>()
                || hoverObject.GetComponentInParent<Smelter>()
                || hoverObject.GetComponentInParent<ArmorStand>();

        private static bool CanTryGetInventoryItems(GameObject hoverObject)
        {
            if (hoverObject.GetComponentInParent<Fireplace>() is Fireplace fireplace)
                return !fireplace.m_infiniteFuel;

            if (hoverObject.GetComponentInParent<CookingStation>() is CookingStation cookingStation)
            {
                Switch switchRef = hoverObject.GetComponent<Switch>();
                return switchRef != null || !cookingStation.m_addFoodSwitch;
            }

            if (hoverObject.GetComponentInParent<ArmorStand>() is ArmorStand armorStand)
            {
                Switch switchRef = hoverObject.GetComponent<Switch>();
                return armorStand.m_slots.Any(slot => slot.m_switch == switchRef);
            }

            return true;
        }

        private static bool TryGetInventoryItems(Player player, GameObject hoverObject, out List<string> items)
        {
            items = new List<string>();

            if (hoverObject.GetComponentInParent<Fireplace>() is Fireplace fireplace)
            {
                items.Add(fireplace.m_fuelItem.m_itemData.m_shared.m_name);
                return true;
            }

            if (hoverObject.GetComponentInParent<Turret>() is Turret turret)
            {
                if (turret.GetAmmo() > 0)
                {
                    ItemDrop.ItemData ammo = FindTurretAmmoInInventory(turret, player.GetInventory(), onlyCurrentlyLoadableType: true);
                    if (ammo != null)
                        items.Add(ammo.m_shared.m_name);
                }
                else
                {
                    items.AddRange(turret.m_allowedAmmo
                        .Select(ammoType => ammoType.m_ammo.m_itemData.m_shared.m_name));
                }
                return true;
            }

            if (hoverObject.GetComponentInParent<CookingStation>() is CookingStation cookingStation)
            {
                Switch switchRef = hoverObject.GetComponent<Switch>();
                if (!switchRef && cookingStation.m_addFoodSwitch)
                    return false;

                if (switchRef == null || switchRef == cookingStation.m_addFoodSwitch)
                {
                    items.AddRange(cookingStation.m_conversion
                        .Where(conversion => conversion?.m_from != null)
                        .Select(conversion => conversion.m_from.m_itemData.m_shared.m_name));
                }
                else if (switchRef == cookingStation.m_addFuelSwitch && cookingStation.m_fuelItem != null)
                {
                    items.Add(cookingStation.m_fuelItem.m_itemData.m_shared.m_name);
                }
                return true;
            }

            if (hoverObject.GetComponentInParent<Smelter>() is Smelter smelter)
            {
                Switch switchRef = hoverObject.GetComponent<Switch>();
                if (switchRef == smelter.m_addOreSwitch)
                {
                    items.AddRange(smelter.m_conversion
                        .Where(conversion => conversion?.m_from != null)
                        .Select(conversion => conversion.m_from.m_itemData.m_shared.m_name));
                }
                else if (switchRef == smelter.m_addWoodSwitch && smelter.m_fuelItem != null)
                {
                    items.Add(smelter.m_fuelItem.m_itemData.m_shared.m_name);
                }
                return true;
            }

            if (hoverObject.GetComponentInParent<ArmorStand>() is ArmorStand armorStand)
            {
                Switch switchRef = hoverObject.GetComponent<Switch>();
                ArmorStand.ArmorStandSlot slot = armorStand.m_slots.FirstOrDefault(candidate => candidate.m_switch == switchRef);
                if (slot == null)
                    return false;

                items.Add("type");
                items.AddRange(slot.m_supportedTypes.Select(type => type.ToString()));
                return true;
            }

            return false;
        }

        private static bool CanUseInventoryItems(Player player, GameObject hoverObject, bool sendErrorMessage)
        {
            Inventory inventory = player.GetInventory();

            if (hoverObject.GetComponentInParent<Fireplace>() is Fireplace fireplace)
            {
                if (fireplace.m_infiniteFuel)
                    return false;

                string fuelName = fireplace.m_fuelItem.m_itemData.m_shared.m_name;
                if (!inventory.HaveItem(fuelName))
                {
                    if (sendErrorMessage)
                        player.Message(MessageHud.MessageType.Center, "$msg_outof " + fuelName);
                    return false;
                }

                if (Mathf.CeilToInt(fireplace.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel)) < fireplace.m_maxFuel)
                    return true;

                if (sendErrorMessage)
                    player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantaddmore", fuelName));
                return false;
            }

            if (hoverObject.GetComponentInParent<Turret>() is Turret turret)
            {
                if (turret.GetAmmo() >= turret.m_maxAmmo)
                {
                    if (sendErrorMessage)
                        player.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                    return false;
                }

                ItemDrop.ItemData ammo = FindTurretAmmoInInventory(turret, inventory, onlyCurrentlyLoadableType: true);
                if (turret.GetAmmo() > 0 && ammo == null)
                {
                    if (sendErrorMessage)
                    {
                        ItemDrop currentAmmo = ZNetScene.instance.GetPrefab(turret.GetAmmoType())?.GetComponent<ItemDrop>();
                        if (currentAmmo != null)
                        {
                            player.Message(
                                MessageHud.MessageType.Center,
                                Localization.instance.Localize("$msg_turretotherammo")
                                + Localization.instance.Localize(currentAmmo.m_itemData.m_shared.m_name));
                        }
                    }
                    return false;
                }

                if (FindTurretAmmoInInventory(turret, inventory, onlyCurrentlyLoadableType: false) != null)
                    return true;

                if (sendErrorMessage)
                    player.Message(MessageHud.MessageType.Center, "$msg_noturretammo");
                return false;
            }

            if (hoverObject.GetComponentInParent<CookingStation>() is CookingStation cookingStation)
            {
                Switch switchRef = hoverObject.GetComponent<Switch>();
                if (switchRef == null || switchRef == cookingStation.m_addFoodSwitch)
                {
                    if (cookingStation.m_requireFire && !cookingStation.IsFireLit())
                    {
                        if (sendErrorMessage)
                            player.Message(MessageHud.MessageType.Center, "$msg_needfire");
                        return false;
                    }

                    if (cookingStation.GetFreeSlot() == -1)
                    {
                        if (sendErrorMessage)
                            player.Message(MessageHud.MessageType.Center, "$msg_nocookroom");
                        return false;
                    }

                    if (HasAnyInventoryItem(
                        player,
                        cookingStation.m_conversion
                            .Where(conversion => conversion?.m_from != null)
                            .Select(conversion => conversion.m_from.m_itemData.m_shared.m_name)))
                    {
                        return true;
                    }

                    if (sendErrorMessage)
                        player.Message(MessageHud.MessageType.Center, "$msg_nocookitems");
                    return false;
                }

                if (switchRef != cookingStation.m_addFuelSwitch || cookingStation.m_fuelItem == null)
                    return false;

                if (cookingStation.GetFuel() > cookingStation.m_maxFuel - 1f)
                {
                    if (sendErrorMessage)
                        player.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                    return false;
                }

                string fuelName = cookingStation.m_fuelItem.m_itemData.m_shared.m_name;
                if (inventory.HaveItem(fuelName))
                    return true;

                if (sendErrorMessage)
                    player.Message(MessageHud.MessageType.Center, "$msg_donthaveany " + fuelName);
                return false;
            }

            if (hoverObject.GetComponentInParent<Smelter>() is Smelter smelter)
            {
                Switch switchRef = hoverObject.GetComponent<Switch>();
                if (switchRef == smelter.m_emptyOreSwitch)
                    return false;

                if (switchRef == smelter.m_addOreSwitch)
                {
                    if (smelter.GetQueueSize() >= smelter.m_maxOre)
                    {
                        if (sendErrorMessage)
                            player.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                        return false;
                    }

                    if (HasAnyInventoryItem(
                        player,
                        smelter.m_conversion
                            .Where(conversion => conversion?.m_from != null)
                            .Select(conversion => conversion.m_from.m_itemData.m_shared.m_name)))
                    {
                        return true;
                    }

                    if (sendErrorMessage)
                        player.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems");
                    return false;
                }

                if (switchRef != smelter.m_addWoodSwitch || smelter.m_fuelItem == null)
                    return false;

                if (smelter.GetFuel() > smelter.m_maxFuel - 1f)
                {
                    if (sendErrorMessage)
                        player.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                    return false;
                }

                string fuelName = smelter.m_fuelItem.m_itemData.m_shared.m_name;
                if (inventory.HaveItem(fuelName))
                    return true;

                if (sendErrorMessage)
                    player.Message(MessageHud.MessageType.Center, "$msg_donthaveany " + fuelName);
                return false;
            }

            if (hoverObject.GetComponentInParent<ArmorStand>() is ArmorStand armorStand)
            {
                Switch switchRef = hoverObject.GetComponent<Switch>();
                return armorStand.m_slots.Any(slot => slot.m_switch == switchRef);
            }

            return false;
        }

        private static ItemDrop.ItemData FindTurretAmmoInInventory(
            Turret turret,
            Inventory inventory,
            bool onlyCurrentlyLoadableType)
        {
            if (turret == null || inventory == null)
                return null;

            string currentAmmoPrefab = onlyCurrentlyLoadableType && turret.HasAmmo()
                ? turret.GetAmmoType()
                : null;

            ItemDrop.ItemData result = null;
            int resultPosition = int.MaxValue;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item == null
                    || (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Ammo
                        && item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.AmmoNonEquipable
                        && item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
                    || item.m_shared.m_ammoType != turret.m_ammoType
                    || (currentAmmoPrefab != null && item.m_dropPrefab?.name != currentAmmoPrefab))
                {
                    continue;
                }

                int position = item.m_gridPos.y * inventory.GetWidth() + item.m_gridPos.x;
                if (position < resultPosition)
                {
                    resultPosition = position;
                    result = item;
                }
            }

            return result;
        }
    }
}
