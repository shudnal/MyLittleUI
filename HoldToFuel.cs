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

        public static IEnumerator AddOnHold(Switch addOreFuelSwitch, Smelter smelter, Humanoid human)
        {
            yield return new WaitForSeconds(delay);

            if (!smelter)
            {
                worker = null;
                yield break;
            }

            if (!smelter.m_nview.IsOwner())
            {
                smelter.m_nview.ClaimOwnership();

                for (int i = 0; i < 5; i++)
                {
                    if (!smelter.m_nview.IsOwner())
                        yield return wait;
                }

                if (!smelter || !smelter.m_nview.IsOwner())
                {
                    LogInfo($"Still not the owner: {smelter}");
                    worker = null;
                    yield break;
                }
            }

            while (ZInput.GetButtonPressedTimer("Use") >= delay && smelter && HaveRoomForOreFuel() && Player.m_localPlayer.GetHoverObject() == addOreFuelSwitch.gameObject)
            {
                addOreFuelSwitch.m_onUse(addOreFuelSwitch, human, null);
                LogInfo($"used {addOreFuelSwitch}");
                yield return wait;
            }

            worker = null;

            bool HaveRoomForOreFuel() => addOreFuelSwitch == smelter.m_addOreSwitch && smelter.GetQueueSize() < smelter.m_maxOre || addOreFuelSwitch == smelter.m_addWoodSwitch && smelter.GetFuel() < smelter.m_maxFuel;
        }

        [HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
        public static class Switch_Interact_HoldToRepeat
        {
            public static bool Prefix() => worker == null;

            public static void Postfix(Switch __instance, Humanoid character, bool hold, bool __runOriginal)
            {
                if (!hoverSmelterHoldToAddSeveral.Value || !hold || !__runOriginal)
                    return;

                if (__instance.m_onUse != null && __instance.GetComponentInParent<Smelter>() is Smelter smelter)
                {
                    worker = AddOnHold(__instance, smelter, character);
                    instance.StartCoroutine(worker);
                }
            }
        }
    }
}
