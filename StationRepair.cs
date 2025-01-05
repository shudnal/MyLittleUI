using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace MyLittleUI
{
    internal static class StationRepair
    {
        private static readonly WaitForSeconds wait = new WaitForSeconds(0.1f);

        public static IEnumerator RepairOnHold()
        {
            if (!InventoryGui.instance)
                yield break;

            yield return new WaitForSeconds(0.4f);
            
            int itemsRepaired = 0;
            while (ZInput.GetButtonPressedTimer("Use") >= 0.4f && InventoryGui.instance.HaveRepairableItems())
            {
                InventoryGui.instance.RepairOneItem();
                itemsRepaired++;
                yield return wait;
            }

            if (itemsRepaired == 0)
                yield break;

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

                if (InventoryGui.IsVisible() && InventoryGui.instance.HaveRepairableItems())
                    __instance.StartCoroutine(RepairOnHold());
            }
        }
    }
}
