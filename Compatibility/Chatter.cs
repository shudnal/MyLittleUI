using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Reflection;
using TMPro;

namespace MyLittleUI
{
    [HarmonyPatch]
    internal static class ChatterCompatibility
    {
        internal const string GUID = "redseiko.valheim.chatter";

        private static MethodBase target;

        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod()
        {
            if (target != null)
                return target;

            if (!Chainloader.PluginInfos.TryGetValue(GUID, out PluginInfo plugin)
                || plugin.Instance == null)
                return null;

            Type contentRowType = plugin.Instance.GetType().Assembly.GetType("Chatter.ContentRow");
            target = contentRowType == null ? null : AccessTools.Method(contentRowType, "AddBodyLabel");
            return target;
        }

        private static void Postfix(TextMeshProUGUI __result)
            => ChatItemLinks.TryRenderLinkLabel(__result);
    }
}
