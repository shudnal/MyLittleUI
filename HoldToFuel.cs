using HarmonyLib;
using System.Collections;
using UnityEngine;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    internal static class HoldToFuel
    {
        private static readonly WaitForSeconds wait = new WaitForSeconds(0.1f);
        private const float delay = 0.4f;
        private static IEnumerator worker;
        private static Switch activeSwitch;

        public static IEnumerator AddOnHold(Switch addOreFuelSwitch, Smelter smelter, Humanoid human)
        {
            try
            {
                yield return new WaitForSeconds(delay);
                if (!CanContinue())
                    yield break;

                if (!smelter.m_nview.IsOwner())
                {
                    smelter.m_nview.ClaimOwnership();
                    for (int i = 0; i < 5 && CanContinue() && !smelter.m_nview.IsOwner(); i++)
                        yield return wait;
                }

                while (CanContinue() && smelter.m_nview.IsOwner() && HaveRoomForOreFuel())
                {
                    if (addOreFuelSwitch.m_onUse == null || !addOreFuelSwitch.m_onUse(addOreFuelSwitch, human, null))
                        yield break;
                    yield return wait;
                }
            }
            finally
            {
                worker = null;
                activeSwitch = null;
            }

            bool CanContinue() => modEnabled.Value && hoverSmelterHoldToAddSeveral.Value && addOreFuelSwitch && smelter
                && smelter.m_nview && smelter.m_nview.IsValid() && human && human == Player.m_localPlayer
                && !Player.m_localPlayer.IsDead() && ZInput.GetButton("Use") && ZInput.GetButtonPressedTimer("Use") >= delay
                && Player.m_localPlayer.GetHoverObject() == addOreFuelSwitch.gameObject;

            bool HaveRoomForOreFuel() => addOreFuelSwitch == smelter.m_addOreSwitch && smelter.GetQueueSize() < smelter.m_maxOre
                || addOreFuelSwitch == smelter.m_addWoodSwitch && smelter.GetFuel() < smelter.m_maxFuel;
        }

        [HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
        public static class Switch_Interact_HoldToRepeat
        {
            public static bool Prefix(Switch __instance, bool hold) => !modEnabled.Value || !hoverSmelterHoldToAddSeveral.Value
                || !hold || worker == null || activeSwitch != __instance;

            public static void Postfix(Switch __instance, Humanoid character, bool hold, bool __runOriginal)
            {
                if (!modEnabled.Value || !hoverSmelterHoldToAddSeveral.Value || !hold || !__runOriginal
                    || worker != null || !character || character != Player.m_localPlayer)
                    return;
                if (__instance.m_onUse != null && __instance.GetComponentInParent<Smelter>() is Smelter smelter
                    && (__instance == smelter.m_addOreSwitch || __instance == smelter.m_addWoodSwitch))
                {
                    activeSwitch = __instance;
                    worker = AddOnHold(__instance, smelter, character);
                    instance.StartCoroutine(worker);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        private static class InventoryGui_OnDestroy_StopWorker
        {
            private static void Prefix()
            {
                if (worker != null && instance)
                    instance.StopCoroutine(worker);
                worker = null;
                activeSwitch = null;
            }
        }
    }
}
