using HarmonyLib;
using UnityEngine;

namespace MyLittleUI
{
    public static class CookingStationRemoveItem
    {
        private static string ZDOSlotAuthor(int slot) => "slotauthor" + slot;

        private static long GetCurrentAuthor(CookingStation __instance, int slot) => __instance.m_nview.GetZDO().GetLong(ZDOSlotAuthor(slot), 0L);

        public static int GetSlotToRemove(CookingStation station, out string itemName, out long author)
        {
            for (int i = station.m_slots.Length - 1; i >= 0; i--)
            {
                station.GetSlot(i, out itemName, out _, out _);
                if (itemName == "" || station.IsItemDone(itemName))
                    continue;

                author = GetCurrentAuthor(station, i);
                return i;
            }

            author = 0L;
            itemName = "";
            return -1;
        }

        public static void RPC_MLUI_RemoveLastUncookedItem(long sender, ZDOID targetZDO, Vector3 userPoint)
        {
            if (targetZDO.IsNone())
                return;

            ZDO zDO = ZDOMan.instance.GetZDO(targetZDO);
            if (zDO != null)
            {
                ZNetView zNetView = ZNetScene.instance.FindInstance(zDO);
                if (zNetView != null && zNetView.IsValid() && zNetView.IsOwner() && zNetView.GetComponentInParent<CookingStation>() is CookingStation station)
                    RemoveLastItem(sender, station, userPoint);
            }
        }

        internal static void RemoveLastItem(long sender, CookingStation station, Vector3 userPoint)
        {
            int slot = GetSlotToRemove(station, out string itemName, out long author);
            if (slot == -1)
                return;

            station.SpawnItem(itemName, slot, userPoint);

            station.SetSlot(slot, "", 0f, CookingStation.Status.NotDone);
            station.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", slot, "");
            
            if (author != 0L)
                ZRoutedRpc.instance.InvokeRoutedRPC(author, "RPC_MLUI_UndoCookingSkillRaise");
        }

        internal static void RPC_MLUI_UndoCookingSkillRaise(long sender) => Player.m_localPlayer.RaiseSkill(Skills.SkillType.Cooking, -0.4f);

        private static void RemoveLastItemFromStation(CookingStation station, Humanoid user)
        {
            if (station.m_nview != null && station.m_nview.IsValid())
                ZRoutedRpc.instance.InvokeRoutedRPC(station.m_nview.GetZDO().GetOwner(), "RPC_MLUI_RemoveLastUncookedItem", station.m_nview.GetZDO().m_uid, user.transform.position);
        }
        
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.RPC_AddItem))]
        public static class CookingStation_RPC_AddItem_SetItemAuthor
        {
            public static long author;

            public static void Prefix(long sender) => author = sender;

            public static void Postfix() => author = 0L;
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SetSlot))]
        public static class CookingStation_SetSlot_RemoveLastItem
        {
            public static void Postfix(CookingStation __instance, int slot)
            {
                if (!__instance.m_nview.IsValid())
                    return;

                if (CookingStation_RPC_AddItem_SetItemAuthor.author != 0L)
                    __instance.m_nview.GetZDO().Set(ZDOSlotAuthor(slot), CookingStation_RPC_AddItem_SetItemAuthor.author);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Interact))]
        public static class CookingStation_Interact_RemoveLastItem
        {
            public static bool Prefix(CookingStation __instance, Humanoid user, bool hold, bool alt)
            {
                if (hold || __instance.m_addFoodSwitch != null || !alt)
                    return true;

                RemoveLastItemFromStation(__instance, user);
                return false;
            }
        }

        [HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
        public static class Switch_Interact_RemoveLastItem
        {
            public static bool Prefix(Switch __instance, Humanoid character, bool hold, bool alt)
            {
                if (hold || !alt || !MyLittleUI.hoverCookingRemoveLastItem.Value)
                    return true;

                if (__instance.m_onUse != null && __instance.GetComponentInParent<CookingStation>() is CookingStation station && station.m_addFoodSwitch == __instance)
                {
                    RemoveLastItemFromStation(station, character);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
        public static class ZoneSystem_Start_Taxi
        {
            private static void Postfix()
            {
                ZRoutedRpc.instance.Register<ZDOID, Vector3>("RPC_MLUI_RemoveLastUncookedItem", RPC_MLUI_RemoveLastUncookedItem);
                ZRoutedRpc.instance.Register("RPC_MLUI_UndoCookingSkillRaise", RPC_MLUI_UndoCookingSkillRaise);
            }
        }
    }
}
