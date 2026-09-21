using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MyLittleUI
{
    internal static class ChatItemLinks
    {
        private const string RpcName = "MyLittleUI_ItemLink";
        private const string LinkIdPrefix = "mlui-item:";
        private const string MessageMarkerPrefix = "\uE000MLUI_ITEM_LINK:";
        private const string MessageMarkerSuffix = "\uE001";
        private const int PayloadVersion = 1;
        private const int MaxPayloadBytes = 256 * 1024;
        private const int MaxCachedLinks = 300;

        private static readonly Dictionary<int, ItemDrop.ItemData> linkedItems = new Dictionary<int, ItemDrop.ItemData>();
        private static readonly Queue<int> linkedItemOrder = new Queue<int>();
        private static int nextLinkId;

        internal static bool TrySendItemLink(ItemDrop.ItemData item)
        {
            Player player = Player.m_localPlayer;
            if (!MyLittleUI.modEnabled.Value || !player || item == null)
                return false;

            Talker talker = player.GetComponent<Talker>();
            if (!talker || talker.m_nview == null || !talker.m_nview.IsValid())
                return false;

            string fallbackText = BuildPlainDisplay(item);
            ZPackage payload = SerializeItem(item);
            if (payload == null || payload.Size() > MaxPayloadBytes)
            {
                MyLittleUI.LogWarning(
                    $"Item link payload for '{item.m_shared?.m_name ?? "unknown item"}' is too large; sending plain chat text instead.");
                talker.Say(Talker.Type.Normal, fallbackText);
                return true;
            }

            byte[] payloadData = payload.GetArray();
            Chat.CheckPermissionsAndSendChatMessageRPCsAsync((long user, bool filterText) =>
            {
                Chat.GetChatMessageData(fallbackText, filterText, out UserInfo userInfoToSend, out string textToSend);

                if (user != 0L)
                {
                    SendItemLinkToTarget(talker, user, userInfoToSend, textToSend, payloadData);
                    return;
                }

                // Some platforms use target 0 for an unrestricted broadcast. Expand it to concrete routed peer UIDs
                // so CCS capability checks can still choose rich item links per recipient.
                HashSet<long> targets = new HashSet<long>();
                if (ZNet.instance != null)
                {
                    long localUid = ZNet.instance.LocalPlayerCharacterID.UserID;
                    if (localUid != 0L)
                        targets.Add(localUid);

                    foreach (ZNet.PlayerInfo playerInfo in ZNet.instance.GetPlayerList())
                    {
                        if (playerInfo.m_characterID != ZDOID.None && playerInfo.m_characterID.UserID != 0L)
                            targets.Add(playerInfo.m_characterID.UserID);
                    }
                }

                if (targets.Count == 0)
                {
                    talker.m_nview.InvokeRPC(
                        0L,
                        "Say",
                        (int)Talker.Type.Normal,
                        userInfoToSend,
                        textToSend);
                    return;
                }

                foreach (long target in targets)
                    SendItemLinkToTarget(talker, target, userInfoToSend, textToSend, payloadData);
            });

            return true;
        }

        private static void SendItemLinkToTarget(
            Talker talker,
            long target,
            UserInfo userInfo,
            string fallbackText,
            byte[] payloadData)
        {
            bool localTarget = target == ZNet.GetUID();
            bool supportsItemLinks = localTarget
                || global::ConditionalConfigSync.ConditionalConfigSync.HasCompatibleConsumer(MyLittleUI.pluginID, target);

            if (supportsItemLinks)
            {
                talker.m_nview.InvokeRPC(
                    target,
                    RpcName,
                    (int)Talker.Type.Normal,
                    userInfo,
                    fallbackText,
                    new ZPackage(payloadData));
                return;
            }

            talker.m_nview.InvokeRPC(
                target,
                "Say",
                (int)Talker.Type.Normal,
                userInfo,
                fallbackText);
        }

        private static ZPackage SerializeItem(ItemDrop.ItemData item)
        {
            try
            {
                ZPackage itemPackage = new ZPackage();
                item.Clone().Save(itemPackage);

                ZPackage payload = new ZPackage();
                payload.Write(PayloadVersion);
                payload.Write(itemPackage);
                return payload;
            }
            catch (Exception exception)
            {
                MyLittleUI.LogWarning($"Failed to serialize chat item link.{Environment.NewLine}{exception}");
                return null;
            }
        }

        private static bool TryDeserializeItem(ZPackage payload, out ItemDrop.ItemData item)
        {
            item = null;
            if (payload == null)
                return false;

            try
            {
                if (payload.ReadInt() != PayloadVersion)
                    return false;

                ZPackage itemPackage = payload.ReadPackage();
                ItemDrop.ItemData loadedItem = new ItemDrop.ItemData();
                int prefabHash = ItemDrop.ItemData.Load(itemPackage, loadedItem, global::Version.c_ItemDataVersion);

                GameObject prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(prefabHash) : null;
                if (!prefab && ZNetScene.instance)
                    prefab = ZNetScene.instance.GetPrefab(prefabHash);

                ItemDrop prefabItem = prefab ? prefab.GetComponent<ItemDrop>() : null;
                if (!prefabItem)
                    return false;

                loadedItem.m_dropPrefab = prefab;
                loadedItem.m_shared = prefabItem.m_itemData.m_shared;
                item = loadedItem;
                return true;
            }
            catch (Exception exception)
            {
                MyLittleUI.LogWarning($"Failed to deserialize chat item link.{Environment.NewLine}{exception}");
                return false;
            }
        }

        private static void ReceiveItemLink(
            Talker talker,
            long sender,
            int chatType,
            UserInfo userInfo,
            string fallbackText,
            ZPackage payload)
        {
            Player player = Player.m_localPlayer;
            if (!player || !talker || !Chat.instance)
                return;

            Talker.Type type = (Talker.Type)chatType;
            float range = type == Talker.Type.Whisper
                ? talker.m_visperDistance
                : type == Talker.Type.Normal
                    ? talker.m_normalDistance
                    : type == Talker.Type.Shout
                        ? talker.m_shoutDistance
                        : 0f;

            if (range <= 0f || Vector3.Distance(talker.transform.position, player.transform.position) >= range)
                return;

            string message = fallbackText;
            if (MyLittleUI.modEnabled.Value && TryDeserializeItem(payload, out ItemDrop.ItemData item))
            {
                int linkId = StoreLinkedItem(item);
                message = BuildMessageMarker(linkId);
            }

            Vector3 position = talker.m_character
                ? talker.m_character.GetHeadPoint()
                : talker.transform.position;

            Chat.instance.OnNewChatMessage(
                talker.gameObject,
                sender,
                position,
                type,
                userInfo,
                message);
        }

        private static int StoreLinkedItem(ItemDrop.ItemData item)
        {
            int linkId = ++nextLinkId;
            if (linkId <= 0)
            {
                linkedItems.Clear();
                linkedItemOrder.Clear();
                nextLinkId = 1;
                linkId = 1;
            }

            linkedItems[linkId] = item;
            linkedItemOrder.Enqueue(linkId);

            while (linkedItemOrder.Count > MaxCachedLinks)
                linkedItems.Remove(linkedItemOrder.Dequeue());

            return linkId;
        }

        private static bool TryGetLinkedItem(int linkId, out ItemDrop.ItemData item)
            => linkedItems.TryGetValue(linkId, out item);

        private static void ResetLinks()
        {
            linkedItems.Clear();
            linkedItemOrder.Clear();
            nextLinkId = 0;
        }

        private static string BuildMessageMarker(int linkId)
            => MessageMarkerPrefix + linkId + MessageMarkerSuffix;

        private static bool TryGetMessageLink(string text, out int linkId, out ItemDrop.ItemData item)
        {
            linkId = 0;
            item = null;

            if (string.IsNullOrEmpty(text)
                || !text.StartsWith(MessageMarkerPrefix, StringComparison.Ordinal)
                || !text.EndsWith(MessageMarkerSuffix, StringComparison.Ordinal))
            {
                return false;
            }

            int numberStart = MessageMarkerPrefix.Length;
            int numberLength = text.Length - MessageMarkerPrefix.Length - MessageMarkerSuffix.Length;
            if (numberLength <= 0
                || !int.TryParse(text.Substring(numberStart, numberLength), out linkId))
            {
                return false;
            }

            return TryGetLinkedItem(linkId, out item);
        }

        private static string RenderChatLink(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            int markerStart = text.IndexOf(MessageMarkerPrefix, StringComparison.Ordinal);
            if (markerStart < 0)
                return text;

            int numberStart = markerStart + MessageMarkerPrefix.Length;
            int markerEnd = text.IndexOf(MessageMarkerSuffix, numberStart, StringComparison.Ordinal);
            if (markerEnd < 0)
                return text;

            int numberLength = markerEnd - numberStart;
            if (numberLength <= 0
                || !int.TryParse(text.Substring(numberStart, numberLength), out int linkId)
                || !TryGetLinkedItem(linkId, out ItemDrop.ItemData item))
            {
                return text;
            }

            int markerLength = markerEnd + MessageMarkerSuffix.Length - markerStart;
            return text.Remove(markerStart, markerLength).Insert(markerStart, BuildChatLink(linkId, item));
        }

        private static string BuildChatLink(int linkId, ItemDrop.ItemData item)
        {
            string name = EscapeRichText(Localization.instance.Localize(item.m_shared.m_name));
            string amount = item.m_stack > 1 ? $" x{item.m_stack}" : string.Empty;
            return $"<link=\"{LinkIdPrefix}{linkId}\"><color=#ffbf00><u>[{name}]</u></color></link>{amount}";
        }

        private static string BuildPlainDisplay(ItemDrop.ItemData item)
        {
            string name = Localization.instance.Localize(item.m_shared.m_name);
            string amount = item.m_stack > 1 ? $" x{item.m_stack}" : string.Empty;
            return $"[{name}]{amount}";
        }

        private static string EscapeRichText(string text)
            => (text ?? string.Empty).Replace("<", "‹").Replace(">", "›");

        private static bool IsLinkModifierHeld()
        {
            if (MyLittleUI.chatItemLinkModifier == null)
                return false;

            KeyboardShortcut shortcut = MyLittleUI.chatItemLinkModifier.Value;
            return shortcut.MainKey != KeyCode.None && shortcut.IsPressed();
        }

        private static bool TryParseTmpLink(string linkId, out int itemLinkId)
        {
            itemLinkId = 0;
            return !string.IsNullOrEmpty(linkId)
                && linkId.StartsWith(LinkIdPrefix, StringComparison.Ordinal)
                && int.TryParse(linkId.Substring(LinkIdPrefix.Length), out itemLinkId);
        }

        private sealed class ChatItemLinkHover : MonoBehaviour
        {
            private TMP_Text output;
            private UITooltip tooltip;
            private int currentLinkId = -1;

            private void Awake()
            {
                output = GetComponent<TMP_Text>();
            }

            private void LateUpdate()
            {
                if (!MyLittleUI.modEnabled.Value || !output || !output.gameObject.activeInHierarchy)
                {
                    HideTooltip();
                    return;
                }

                int linkIndex = TMP_TextUtilities.FindIntersectingLink(output, ZInput.pointerPosition, null);
                if (linkIndex < 0 || linkIndex >= output.textInfo.linkCount)
                {
                    HideTooltip();
                    return;
                }

                TMP_LinkInfo linkInfo = output.textInfo.linkInfo[linkIndex];
                if (!TryParseTmpLink(linkInfo.GetLinkID(), out int linkId)
                    || !TryGetLinkedItem(linkId, out ItemDrop.ItemData item))
                {
                    HideTooltip();
                    return;
                }

                if (currentLinkId == linkId && tooltip && UITooltip.m_current == tooltip)
                    return;

                if (!EnsureTooltip())
                    return;

                HideTooltip();
                tooltip.Set(item.m_shared.m_name, item.GetTooltip());
                tooltip.OnHoverStart(output.gameObject);
                currentLinkId = linkId;
            }

            private bool EnsureTooltip()
            {
                if (tooltip)
                    return true;

                if (!InventoryGui.instance
                    || !InventoryGui.instance.m_playerGrid
                    || !InventoryGui.instance.m_playerGrid.m_elementPrefab)
                {
                    return false;
                }

                InventoryElement inventoryElement =
                    InventoryGui.instance.m_playerGrid.m_elementPrefab.GetComponent<InventoryElement>();
                if (!inventoryElement || !inventoryElement.m_tooltip || !inventoryElement.m_tooltip.m_tooltipPrefab)
                    return false;

                GameObject host = new GameObject("MyLittleUI Item Link Tooltip", typeof(RectTransform));
                host.transform.SetParent(output.transform, false);
                tooltip = host.AddComponent<UITooltip>();
                tooltip.m_tooltipPrefab = inventoryElement.m_tooltip.m_tooltipPrefab;
                return true;
            }

            private void HideTooltip()
            {
                if (tooltip && UITooltip.m_current == tooltip)
                    UITooltip.HideTooltip();

                currentLinkId = -1;
            }

            private void OnDisable() => HideTooltip();

            private void OnDestroy() => HideTooltip();
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.OnLeftDown))]
        private static class InventoryGrid_OnLeftDown_LinkItem
        {
            private static bool Prefix(InventoryGrid __instance, UIInputHandler clickHandler)
            {
                if (!MyLittleUI.modEnabled.Value
                    || !IsLinkModifierHeld()
                    || clickHandler == null
                    || __instance.m_inventory == null)
                {
                    return true;
                }

                Vector2i position = __instance.GetButtonPos(clickHandler.gameObject);
                if (position.x < 0 || position.y < 0)
                    return true;

                ItemDrop.ItemData item = __instance.m_inventory.GetItemAt(position.x, position.y);
                if (item == null)
                    return true;

                return !TrySendItemLink(item);
            }
        }

        [HarmonyPatch(typeof(Talker), nameof(Talker.Awake))]
        private static class Talker_Awake_RegisterItemLinkRpc
        {
            private static void Postfix(Talker __instance)
            {
                if (!__instance.m_nview)
                    return;

                __instance.m_nview.Register<int, UserInfo, string, ZPackage>(
                    RpcName,
                    (long sender, int chatType, UserInfo userInfo, string fallbackText, ZPackage payload)
                        => ReceiveItemLink(__instance, sender, chatType, userInfo, fallbackText, payload));
            }
        }

        [HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
        private static class Chat_Awake_InitializeItemLinks
        {
            private static void Postfix(Chat __instance)
            {
                ResetLinks();

                if (!__instance.m_output)
                    return;

                if (!__instance.m_output.GetComponent<ChatItemLinkHover>())
                    __instance.m_output.gameObject.AddComponent<ChatItemLinkHover>();
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), new Type[] { typeof(string) })]
        private static class Terminal_AddString_RenderItemLink
        {
            private static void Prefix(ref string text)
            {
                text = RenderChatLink(text);
            }
        }

        [HarmonyPatch(typeof(Chat), nameof(Chat.AddInworldText))]
        private static class Chat_AddInworldText_RenderItemLink
        {
            private static void Prefix(ref string text)
            {
                if (TryGetMessageLink(text, out _, out ItemDrop.ItemData item))
                    text = BuildPlainDisplay(item);
            }
        }
    }
}
