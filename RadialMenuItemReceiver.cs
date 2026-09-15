using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MyLittleUI
{
    internal sealed class RadialMenuItemReceiver : MonoBehaviour, IHasHoverMenu
    {
        private sealed class ReceiverDefinition
        {
            internal Type TargetType { get; }
            internal Func<bool> Enabled { get; }
            internal Func<Component, Player, IEnumerable<string>> GetItems { get; }
            internal Func<Component, Player, bool, bool> CanUseItems { get; }
            internal MethodBase[] HoverTextMethods { get; }

            internal ReceiverDefinition(
                Type targetType,
                Func<bool> enabled,
                Func<Component, Player, IEnumerable<string>> getItems,
                Func<Component, Player, bool, bool> canUseItems,
                MethodBase[] hoverTextMethods)
            {
                TargetType = targetType;
                Enabled = enabled;
                GetItems = getItems;
                CanUseItems = canUseItems;
                HoverTextMethods = hoverTextMethods;
            }
        }

        private static readonly ReceiverDefinition[] Definitions =
        {
            Create<Fermenter>(
                () => MyLittleUI.radialMenuFermenterItemSelection?.Value == true,
                GetFermenterItems,
                CanUseFermenterItems,
                AccessTools.Method(typeof(Fermenter), nameof(Fermenter.GetHoverText)))
        };

        private static readonly HashSet<Type> ErrorTypes = new HashSet<Type>();

        private Component target;
        private ReceiverDefinition definition;

        internal bool IsActive => target
            && definition != null
            && MyLittleUI.modEnabled?.Value == true
            && definition.Enabled();

        private static ReceiverDefinition Create<T>(
            Func<bool> enabled,
            Func<T, Player, IEnumerable<string>> getItems,
            Func<T, Player, bool, bool> canUseItems,
            params MethodBase[] hoverTextMethods)
            where T : Component
            => new ReceiverDefinition(
                typeof(T),
                enabled,
                (component, player) => getItems((T)component, player),
                (component, player, sendErrorMessage) => canUseItems((T)component, player, sendErrorMessage),
                hoverTextMethods?.Where(method => method != null).ToArray() ?? Array.Empty<MethodBase>());

        internal static IEnumerable<MethodBase> GetHoverTextMethods()
            => Definitions.SelectMany(receiver => receiver.HoverTextMethods).Distinct();

        private static IEnumerable<MethodBase> GetTargetAwakeMethods()
            => Definitions
                .Select(receiver => AccessTools.Method(receiver.TargetType, "Awake", Type.EmptyTypes))
                .Where(method => method != null)
                .Distinct();

        private static void TryAttach(Component targetComponent)
        {
            if (!targetComponent)
                return;

            ReceiverDefinition targetDefinition = Definitions.FirstOrDefault(receiver => receiver.TargetType.IsInstanceOfType(targetComponent));
            if (targetDefinition == null)
                return;

            MonoBehaviour[] components = targetComponent.GetComponents<MonoBehaviour>();
            if (components.Any(component => !(component is RadialMenuItemReceiver)
                && (component is IHasHoverMenu || component is IHasHoverMenuExtended)))
            {
                return;
            }

            RadialMenuItemReceiver receiverComponent = components.OfType<RadialMenuItemReceiver>().FirstOrDefault()
                ?? targetComponent.gameObject.AddComponent<RadialMenuItemReceiver>();

            receiverComponent.target = targetComponent;
            receiverComponent.definition = targetDefinition;
        }

        public bool TryGetItems(Player player, out List<string> items)
        {
            items = new List<string>();
            if (!IsActive)
                return false;

            try
            {
                if (!definition.CanUseItems(target, player, true))
                    return true;

                items = (definition.GetItems(target, player) ?? Enumerable.Empty<string>())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct()
                    .ToList();
                return true;
            }
            catch (Exception exception)
            {
                LogFailure(exception);
                return false;
            }
        }

        public bool CanUseItems(Player player, bool sendErrorMessage = true)
        {
            if (!IsActive)
                return false;

            try
            {
                return definition.CanUseItems(target, player, sendErrorMessage);
            }
            catch (Exception exception)
            {
                LogFailure(exception);
                return false;
            }
        }

        private void LogFailure(Exception exception)
        {
            Type targetType = definition?.TargetType;
            if (targetType == null || ErrorTypes.Add(targetType))
            {
                MyLittleUI.LogWarning(
                    $"Failed to evaluate radial menu item receiver for '{targetType?.FullName ?? "unknown type"}'.{Environment.NewLine}{exception}");
            }
        }

        private static IEnumerable<string> GetFermenterItems(Fermenter fermenter, Player _)
        {
            if (!fermenter || fermenter.m_conversion == null)
                yield break;

            foreach (Fermenter.ItemConversion conversion in fermenter.m_conversion)
            {
                if (conversion?.m_from != null)
                    yield return conversion.m_from.m_itemData.m_shared.m_name;
            }
        }

        private static bool CanUseFermenterItems(Fermenter fermenter, Player player, bool sendErrorMessage)
        {
            if (!fermenter
                || !player
                || fermenter.m_nview == null
                || fermenter.m_nview.GetZDO() == null)
            {
                return false;
            }

            if (sendErrorMessage)
                fermenter.UpdateCover(0f, forceUpdate: true);

            if (!PrivateArea.CheckAccess(fermenter.transform.position, 0f, flash: sendErrorMessage))
                return false;

            if (fermenter.GetStatus() != Fermenter.Status.Empty)
                return false;

            if (!fermenter.m_hasRoof)
            {
                if (sendErrorMessage)
                    player.Message(MessageHud.MessageType.Center, "$piece_fermenter_needroof");
                return false;
            }

            if (fermenter.m_exposed)
            {
                if (sendErrorMessage)
                    player.Message(MessageHud.MessageType.Center, "$piece_fermenter_exposed");
                return false;
            }

            if (fermenter.FindCookableItem(player.GetInventory()) != null)
                return true;

            if (sendErrorMessage)
                player.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems");

            return false;
        }

        [HarmonyPatch]
        private static class RegisteredReceiver_Awake_AttachAdapter
        {
            private static IEnumerable<MethodBase> TargetMethods() => GetTargetAwakeMethods();

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(object __instance)
            {
                if (__instance is Component targetComponent)
                    TryAttach(targetComponent);
            }
        }
    }
}
