using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace MyLittleUI
{
    internal static class StationRepair
    {
        private static readonly WaitForSeconds wait = new WaitForSeconds(0.1f);
        private const float delay = 0.4f;
        private static IEnumerator worker;

        public static IEnumerator RepairOnHold()
        {
            InventoryGui gui = InventoryGui.instance;
            Player player = Player.m_localPlayer;
            CraftingStation station = player ? player.GetCurrentCraftingStation() : null;
            try
            {
                yield return new WaitForSeconds(delay);
                int itemsRepaired = 0;
                while (CanContinue() && gui.HaveRepairableItems())
                {
                    gui.RepairOneItem();
                    itemsRepaired++;
                    yield return wait;
                }
                if (!CanContinue())
                    yield break;
                if (itemsRepaired == 0)
                    player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$settings_inventory $msg_doesnotneedrepair", itemsRepaired.ToString()));
                else
                    player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_repaired", itemsRepaired.ToString()));
                gui.Hide();
            }
            finally
            {
                worker = null;
            }

            bool CanContinue() => MyLittleUI.modEnabled.Value && MyLittleUI.hoverHoldToMassRepair.Value && gui
                && gui == InventoryGui.instance && InventoryGui.IsVisible() && player && player == Player.m_localPlayer
                && !player.IsDead() && station && player.GetCurrentCraftingStation() == station
                && ZInput.GetButton("Use") && ZInput.GetButtonPressedTimer("Use") >= delay;
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
        public static class CraftingStation_Interact_RepairOnHold
        {
            public static void Postfix(CraftingStation __instance, bool __result)
            {
                if (__result || !MyLittleUI.modEnabled.Value || !MyLittleUI.hoverHoldToMassRepair.Value || worker != null
                    || !Player.m_localPlayer || Player.m_localPlayer.GetCurrentCraftingStation() != __instance)
                    return;
                if (InventoryGui.IsVisible())
                {
                    worker = RepairOnHold();
                    MyLittleUI.instance.StartCoroutine(worker);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        private static class InventoryGui_OnDestroy_StopWorker
        {
            private static void Prefix()
            {
                if (worker != null && MyLittleUI.instance)
                    MyLittleUI.instance.StopCoroutine(worker);
                worker = null;
            }
        }
    }
}
