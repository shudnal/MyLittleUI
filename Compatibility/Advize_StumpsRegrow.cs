
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using static MyLittleUI.MyLittleUI;

[HarmonyPatch]
public static class Advize_StumpsRegrow_Compat
{
    internal const string GUID = "advize.StumpsRegrow";
    static ConfigEntry<float> stumpGrowthTime;
    static bool initialized;
    static bool hasGrowthTime;

    static double GetTimeSincePlanted(ZNetView nview) => (ZNet.instance.GetTime() - new DateTime(nview.GetZDO().GetLong(ZDOVars.s_plantTime, 0L))).TotalSeconds;

    static float GetGrowTime() => stumpGrowthTime != null ? Mathf.Max(stumpGrowthTime.Value, 1f) : 1f;

    static void Postfix(object __instance, ref string __result)
    {
        if (!modEnabled.Value)
            return;

        if (__instance == null)
            return;

        if (!hoverStumpGrowerEnabled.Value || hoverStumpGrower.Value == StationHover.Vanilla)
            return;

        if (string.IsNullOrWhiteSpace(__result))
            return;

        if (!initialized)
        {
            initialized = true;
            hasGrowthTime = Chainloader.PluginInfos.TryGetValue(GUID, out PluginInfo plugin) && plugin.Instance.Config.TryGetEntry("General", "StumpGrowthTime", out stumpGrowthTime);
        }

        if (!hasGrowthTime)
            return;

        ZNetView nview = (__instance as MonoBehaviour)?.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid())
            return;

        if (hoverStumpGrower.Value == StationHover.Percentage)
            __result += $"\n{GetTimeSincePlanted(nview) / GetGrowTime():P0}";
        else if (hoverStumpGrower.Value == StationHover.Bar)
            __result += $"\n{FromPercent(GetTimeSincePlanted(nview) / GetGrowTime())}";
        else if (hoverStumpGrower.Value == StationHover.MinutesSeconds)
            __result += $"\n{FromSeconds(GetGrowTime() - GetTimeSincePlanted(nview))}";
    }

    public static MethodBase target;

    public static bool Prepare(MethodBase original)
    {
        if (!Chainloader.PluginInfos.TryGetValue(GUID, out PluginInfo plugin))
            return false;

        target ??= AccessTools.Method(plugin.Instance.GetType().Assembly.GetType("Advize_StumpsRegrow.StumpGrower"), "GetHoverText");
        if (target == null)
            return false;

        if (original == null)
            LogInfo("Advize_StumpsRegrow.StumpGrower:GetHoverText method is patched to show configurable hover");

        return true;
    }

    public static MethodBase TargetMethod() => target;
}
