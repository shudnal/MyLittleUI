using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace MyLittleUI
{
    internal static class StationRepair
    {
        private const float delay = 0.4f;
        private const float repairInterval = 0.1f;
        private static IEnumerator worker;
        private static int workerId;

        // Keep both existing settings: the inventory option controls the feature,
        // while the hover option lets a client opt out of mass repair.
        private static bool IsEnabled => MyLittleUI.modEnabled.Value
            && MyLittleUI.inventoryEnableRepairOnHold.Value && MyLittleUI.hoverHoldToMassRepair.Value;

        private static string GetUseButton()
        {
            // Follow the configured action, not a physical controller button or the last active device.
            if (ZInput.GetButtonDown("Use"))
                return "Use";
            if (ZInput.GetButtonDown("JoyUse"))
                return "JoyUse";
            if (ZInput.GetButton("Use"))
                return "Use";
            return ZInput.GetButton("JoyUse") ? "JoyUse" : null;
        }

        private static IEnumerator RepairOnHold(InventoryGui gui, Player player, CraftingStation station, string inputButton, int sessionId)
        {
            try
            {
                float nextRepairTime = Time.time + delay;
                int itemsRepaired = 0;
                while (CanContinue())
                {
                    // Check cancellation every frame, including the initial hold delay and repair intervals.
                    if (Time.time < nextRepairTime)
                    {
                        yield return null;
                        continue;
                    }
                    if (!gui.HaveRepairableItems())
                        break;

                    gui.RepairOneItem();
                    itemsRepaired++;
                    nextRepairTime = Time.time + repairInterval;
                    yield return null;
                }
                if (!CanContinue())
                    yield break;

                if (itemsRepaired == 0)
                    player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$settings_inventory $msg_doesnotneedrepair", itemsRepaired.ToString()));
                else
                    player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_repaired", itemsRepaired.ToString()));

                // Hide also cancels externally closed sessions; do not stop this iterator from inside itself.
                worker = null;
                gui.Hide();
            }
            finally
            {
                // A nested interaction may have started a new session while this one was being stopped.
                if (sessionId == workerId)
                    worker = null;
            }

            bool CanContinue() => sessionId == workerId && IsEnabled && ZInput.GetButton(inputButton)
                && MyLittleUI.instance && MyLittleUI.instance.isActiveAndEnabled
                && gui && gui == InventoryGui.instance && gui.m_animator && gui.m_animator.GetBool("visible")
                && player && player == Player.m_localPlayer && !player.IsDead() && !player.InCutscene() && !player.IsTeleporting()
                && station && station.m_canRepair && player.GetCurrentCraftingStation() == station;
        }

        internal static void StopWorker()
        {
            IEnumerator currentWorker = worker;
            if (currentWorker == null)
                return;

            worker = null;
            workerId++;
            if (MyLittleUI.instance)
                MyLittleUI.instance.StopCoroutine(currentWorker);
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
        public static class CraftingStation_Interact_RepairOnHold
        {
            public static void Postfix(CraftingStation __instance, Humanoid user, bool repeat, bool __result)
            {
                if (__result || repeat || !IsEnabled || worker != null || !__instance || !__instance.m_canRepair
                    || !MyLittleUI.instance || !MyLittleUI.instance.isActiveAndEnabled
                    || !Player.m_localPlayer || user != Player.m_localPlayer
                    || Player.m_localPlayer.GetCurrentCraftingStation() != __instance)
                    return;

                InventoryGui gui = InventoryGui.instance;
                string inputButton = GetUseButton();
                if (!gui || !gui.m_animator || !gui.m_animator.GetBool("visible") || inputButton == null)
                    return;

                int sessionId = ++workerId;
                worker = RepairOnHold(gui, Player.m_localPlayer, __instance, inputButton, sessionId);
                try
                {
                    MyLittleUI.instance.StartCoroutine(worker);
                }
                catch
                {
                    if (sessionId == workerId)
                        worker = null;
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        private static class InventoryGui_Hide_StopWorker
        {
            private static void Prefix() => StopWorker();
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        private static class InventoryGui_OnDestroy_StopWorker
        {
            private static void Prefix() => StopWorker();
        }
    }
}
