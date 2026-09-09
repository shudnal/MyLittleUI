using HarmonyLib;
using UnityEngine;

namespace MyLittleUI
{
    public static class CookingStationRemoveItem
    {
        private static string ZDOSlotAuthor(int slot) => "slotauthor" + slot;
        private static long GetCurrentAuthor(CookingStation station, int slot) => station.m_nview.GetZDO().GetLong(ZDOSlotAuthor(slot), 0L);

        public static int GetSlotToRemove(CookingStation station, out string itemName, out long author)
        {
            itemName = "";
            author = 0L;
            if (!station || !station.m_nview || !station.m_nview.IsValid())
                return -1;
            for (int i = station.m_slots.Length - 1; i >= 0; i--)
            {
                station.GetSlot(i, out itemName, out _, out CookingStation.Status status, out _);
                if (string.IsNullOrEmpty(itemName) || status != CookingStation.Status.NotDone
                    || (station.m_overCookedItem && itemName == station.m_overCookedItem.name))
                    continue;
                CookingStation.ItemConversion conversion = station.GetItemConversion(itemName);
                if (conversion == null || !conversion.m_from || itemName != conversion.m_from.name)
                    continue;
                author = GetCurrentAuthor(station, i);
                return i;
            }
            itemName = "";
            return -1;
        }

        public static void RPC_MLUI_RemoveLastUncookedItem(long sender, ZDOID targetZDO, Vector3 userPoint)
        {
            if (targetZDO.IsNone() || !ZNetScene.instance || ZDOMan.instance == null)
                return;
            ZDO zdo = ZDOMan.instance.GetZDO(targetZDO);
            if (zdo == null)
                return;
            ZNetView view = ZNetScene.instance.FindInstance(zdo);
            if (view && view.IsValid() && view.IsOwner() && view.GetComponentInParent<CookingStation>() is CookingStation station)
                RemoveLastItem(sender, station, userPoint);
        }

        internal static void RemoveLastItem(long sender, CookingStation station, Vector3 userPoint)
        {
            int slot = GetSlotToRemove(station, out string itemName, out long author);
            if (slot < 0 || !station.m_nview.IsOwner())
                return;
            station.GetSlot(slot, out _, out _, out _, out bool cheated);
            if (!SpawnRawItem(station, itemName, slot, userPoint, cheated))
                return;

            station.SetSlot(slot, "", 0f, CookingStation.Status.NotDone, cheated: false);
            station.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", slot, "");
            if (author != 0L)
            {
                if (station.m_skill == Skills.SkillType.Cooking)
                    ZRoutedRpc.instance.InvokeRoutedRPC(author, "RPC_MLUI_UndoCookingSkillRaise");
                else if (station.m_skill != Skills.SkillType.None)
                    ZRoutedRpc.instance.InvokeRoutedRPC(author, "RPC_MLUI_UndoStationSkillRaise", (int)station.m_skill);
            }
        }

        private static bool SpawnRawItem(CookingStation station, string itemName, int slot, Vector3 userPoint, bool cheated)
        {
            GameObject prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(itemName) : null;
            if (!prefab || !prefab.GetComponent<ItemDrop>() || !station.m_slots[slot])
                return false;

            Vector3 direction;
            Vector3 position;
            if (station.m_spawnPoint)
            {
                direction = station.m_spawnPoint.forward;
                position = station.m_spawnPoint.position;
            }
            else
            {
                position = station.m_slots[slot].position;
                direction = userPoint - position;
                direction.y = 0f;
                direction.Normalize();
                position += direction * 0.5f;
            }
            GameObject item = Object.Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0, 360), 0f));
            ItemDrop.OnCreateNew(item.GetComponent<ItemDrop>(), cheated);
            Rigidbody body = item.GetComponent<Rigidbody>();
            if (body)
                body.linearVelocity = direction * station.m_spawnForce;
            station.m_pickEffector.Create(position, Quaternion.identity);
            return true;
        }

        internal static void RPC_MLUI_UndoCookingSkillRaise(long sender) => RPC_MLUI_UndoStationSkillRaise(sender, (int)Skills.SkillType.Cooking);

        internal static void RPC_MLUI_UndoStationSkillRaise(long sender, int skill)
        {
            if (Player.m_localPlayer && skill != (int)Skills.SkillType.None)
                Player.m_localPlayer.RaiseSkill((Skills.SkillType)skill, -0.4f);
        }

        private static void RemoveLastItemFromStation(CookingStation station, Humanoid user)
        {
            if (user && station.m_nview && station.m_nview.IsValid() && ZRoutedRpc.instance != null)
                ZRoutedRpc.instance.InvokeRoutedRPC(station.m_nview.GetZDO().GetOwner(), "RPC_MLUI_RemoveLastUncookedItem",
                    station.m_nview.GetZDO().m_uid, user.transform.position);
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.RPC_AddItem))]
        public static class CookingStation_RPC_AddItem_SetItemAuthor
        {
            public static long author;
            public static void Prefix(long sender, out long __state)
            {
                __state = author;
                author = sender;
            }
            public static void Finalizer(long __state) => author = __state;
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SetSlot))]
        public static class CookingStation_SetSlot_RemoveLastItem
        {
            public static void Postfix(CookingStation __instance, int slot, string __1)
            {
                if (!__instance.m_nview || !__instance.m_nview.IsValid())
                    return;
                if (string.IsNullOrEmpty(__1))
                    __instance.m_nview.GetZDO().Set(ZDOSlotAuthor(slot), 0L);
                else if (CookingStation_RPC_AddItem_SetItemAuthor.author != 0L)
                    __instance.m_nview.GetZDO().Set(ZDOSlotAuthor(slot), CookingStation_RPC_AddItem_SetItemAuthor.author);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Interact))]
        public static class CookingStation_Interact_RemoveLastItem
        {
            public static bool Prefix(CookingStation __instance, Humanoid user, bool hold, bool alt, bool __runOriginal)
            {
                if (!__runOriginal)
                    return false;
                if (!MyLittleUI.modEnabled.Value || !MyLittleUI.hoverCookingRemoveLastItem.Value
                    || hold || __instance.m_addFoodSwitch != null || !alt)
                    return true;
                RemoveLastItemFromStation(__instance, user);
                return false;
            }
        }

        [HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
        public static class Switch_Interact_RemoveLastItem
        {
            public static bool Prefix(Switch __instance, Humanoid character, bool hold, bool alt, bool __runOriginal)
            {
                if (!__runOriginal)
                    return false;
                if (!MyLittleUI.modEnabled.Value || hold || !alt || !MyLittleUI.hoverCookingRemoveLastItem.Value)
                    return true;
                if (__instance.m_onUse != null && __instance.GetComponentInParent<CookingStation>() is CookingStation station
                    && station.m_addFoodSwitch == __instance)
                {
                    RemoveLastItemFromStation(station, character);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
        public static class ZoneSystem_Start_RegisterRemoval
        {
            private static void Postfix()
            {
                ZRoutedRpc.instance.Register<ZDOID, Vector3>("RPC_MLUI_RemoveLastUncookedItem", RPC_MLUI_RemoveLastUncookedItem);
                ZRoutedRpc.instance.Register("RPC_MLUI_UndoCookingSkillRaise", RPC_MLUI_UndoCookingSkillRaise);
                ZRoutedRpc.instance.Register<int>("RPC_MLUI_UndoStationSkillRaise", RPC_MLUI_UndoStationSkillRaise);
            }
        }
    }
}
