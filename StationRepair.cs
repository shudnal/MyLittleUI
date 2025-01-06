using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace MyLittleUI
{
    internal static class StationRepair
    {
        private static readonly WaitForSeconds wait = new WaitForSeconds(0.1f);
        private const float delay = 0.4f;
        public static IEnumerator RepairOnHold()
        {
            if (!InventoryGui.instance)
                yield break;

            yield return new WaitForSeconds(delay);
            
            int itemsRepaired = 0;
            while (ZInput.GetButtonPressedTimer("Use") >= delay && InventoryGui.instance.HaveRepairableItems())
            {
                InventoryGui.instance.RepairOneItem();
                itemsRepaired++;
                yield return wait;
            }

            if (!ZInput.GetButton("Use"))
                yield break;

            if (itemsRepaired == 0)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$settings_inventory $msg_doesnotneedrepair", itemsRepaired.ToString()));
            else
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_repaired", itemsRepaired.ToString()));
            
            InventoryGui.instance.Hide();
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
        public static class CraftingStation_Interact_RepairOnHold
        {
            public static void Postfix(CraftingStation __instance, bool __result)
            {
                if (__result)
                    return;

                if (InventoryGui.IsVisible())
                    __instance.StartCoroutine(RepairOnHold());
            }
        }
    }
}
