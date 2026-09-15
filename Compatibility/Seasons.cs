using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace MyLittleUI
{
    internal static class SeasonsHoverCompatibility
    {
        internal const string GUID = "shudnal.Seasons";

        private sealed class HoverOwnership
        {
            internal ConfigEntryBase Entry { get; }
            internal Func<bool> ShouldOwn { get; }
            internal object ReleasedValue { get; set; }
            internal bool Owned { get; set; }

            internal HoverOwnership(ConfigEntryBase entry, Func<bool> shouldOwn)
            {
                Entry = entry;
                ShouldOwn = shouldOwn;
            }
        }

        private static PluginInfo seasonsPlugin;
        private static Type seasonsType;
        private static FieldInfo seasonStateField;
        private static object boundSeasonState;
        private static object failedSeasonState;

        private static Func<Beehive, int, float, double> getSecondsToMakeHoney;
        private static Func<Plant, double> getSecondsToGrowPlant;
        private static Func<Pickable, double> getSecondsToRespawnPickable;
        private static Func<Vector3, bool> isProtectedPosition;

        private static ConfigEntryBase seasonsHoverBeeHive;
        private static ConfigEntryBase seasonsHoverPlant;
        private static ConfigEntryBase seasonsHoverPickable;
        private static ConfigFile seasonsConfig;
        private static HoverOwnership[] hoverOwnerships;

        private static bool initialized;
        private static bool seasonsConfigHandlerBound;
        private static bool myLittleUIConfigHandlerBound;
        private static bool suppressSettingHandler;
        private static bool delegateErrorLogged;
        private static bool invocationErrorLogged;

        internal static void Initialize()
        {
            if (initialized || !DetectSeasons())
                return;

            initialized = true;
            EnsureHoverOwnership();
            EnsureDelegates();
        }

        private static bool DetectSeasons()
        {
            if (!Chainloader.PluginInfos.TryGetValue(GUID, out seasonsPlugin) || seasonsPlugin?.Instance == null)
                return false;

            seasonsType = AccessTools.TypeByName("Seasons.Seasons");
            seasonStateField = seasonsType == null ? null : AccessTools.Field(seasonsType, "seasonState");
            seasonsConfig = seasonsPlugin.Instance.Config;
            return seasonsType != null && seasonStateField != null;
        }

        private static ConfigEntryBase GetSeasonsConfigEntry(string fieldName)
        {
            return seasonsType == null
                ? null
                : AccessTools.Field(seasonsType, fieldName)?.GetValue(null) as ConfigEntryBase;
        }

        private static void SetSeasonsHoverValue(ConfigEntryBase entry, object value)
        {
            if (entry == null || value == null || Equals(entry.BoxedValue, value))
                return;

            try
            {
                suppressSettingHandler = true;
                entry.BoxedValue = value;
            }
            finally
            {
                suppressSettingHandler = false;
            }
        }

        private static void EnforceVanilla(ConfigEntryBase entry)
        {
            if (entry == null || entry.BoxedValue == null || entry.BoxedValue.ToString() == "Vanilla")
                return;

            MyLittleUI.LogWarning(
                $"Seasons hover '{entry.Definition.Key}' was reset to Vanilla because My Little UI controls this hover.");

            SetSeasonsHoverValue(entry, Enum.Parse(entry.SettingType, "Vanilla"));
        }

        private static void ReconcileHoverOwnership(HoverOwnership ownership)
        {
            if (ownership?.Entry == null)
                return;

            bool shouldOwn = MyLittleUI.modEnabled?.Value == true && ownership.ShouldOwn();
            if (shouldOwn)
            {
                if (!ownership.Owned)
                {
                    ownership.ReleasedValue = ownership.Entry.BoxedValue;
                    ownership.Owned = true;
                }

                EnforceVanilla(ownership.Entry);
                return;
            }

            if (!ownership.Owned)
                return;

            object releasedValue = ownership.ReleasedValue;
            ownership.ReleasedValue = null;
            ownership.Owned = false;
            SetSeasonsHoverValue(ownership.Entry, releasedValue);
        }

        private static void ReconcileHoverOwnerships()
        {
            if (hoverOwnerships == null)
                return;

            foreach (HoverOwnership ownership in hoverOwnerships)
                ReconcileHoverOwnership(ownership);
        }

        private static HoverOwnership FindHoverOwnership(ConfigEntryBase entry)
        {
            if (entry == null || hoverOwnerships == null)
                return null;

            foreach (HoverOwnership ownership in hoverOwnerships)
            {
                if (ReferenceEquals(entry, ownership.Entry))
                    return ownership;
            }

            return null;
        }

        private static bool IsMyLittleUIOwnershipSetting(ConfigEntryBase entry)
        {
            return ReferenceEquals(entry, MyLittleUI.modEnabled)
                || ReferenceEquals(entry, MyLittleUI.hoverBeeHiveEnabled)
                || ReferenceEquals(entry, MyLittleUI.hoverBeeHive)
                || ReferenceEquals(entry, MyLittleUI.hoverPlantEnabled)
                || ReferenceEquals(entry, MyLittleUI.hoverPlant)
                || ReferenceEquals(entry, MyLittleUI.hoverPickableEnabled)
                || ReferenceEquals(entry, MyLittleUI.hoverPickable);
        }

        private static void OnSeasonsConfigSettingChanged(object sender, SettingChangedEventArgs args)
        {
            if (suppressSettingHandler || args?.ChangedSetting == null)
                return;

            HoverOwnership ownership = FindHoverOwnership(args.ChangedSetting);
            if (ownership == null)
                return;

            if (ownership.Owned)
                ownership.ReleasedValue = ownership.Entry.BoxedValue;

            ReconcileHoverOwnership(ownership);
        }

        private static void OnMyLittleUIConfigSettingChanged(object sender, SettingChangedEventArgs args)
        {
            if (args?.ChangedSetting == null || !IsMyLittleUIOwnershipSetting(args.ChangedSetting))
                return;

            ReconcileHoverOwnerships();
        }

        private static void EnsureHoverOwnership()
        {
            seasonsHoverBeeHive ??= GetSeasonsConfigEntry("hoverBeeHive");
            seasonsHoverPlant ??= GetSeasonsConfigEntry("hoverPlant");
            seasonsHoverPickable ??= GetSeasonsConfigEntry("hoverPickable");

            hoverOwnerships ??= new[]
            {
                new HoverOwnership(
                    seasonsHoverBeeHive,
                    () => MyLittleUI.hoverBeeHiveEnabled?.Value == true
                        && MyLittleUI.hoverBeeHive?.Value != MyLittleUI.StationHover.Vanilla),
                new HoverOwnership(
                    seasonsHoverPlant,
                    () => MyLittleUI.hoverPlantEnabled?.Value == true
                        && MyLittleUI.hoverPlant?.Value != MyLittleUI.StationHover.Vanilla),
                new HoverOwnership(
                    seasonsHoverPickable,
                    () => MyLittleUI.hoverPickableEnabled?.Value == true
                        && MyLittleUI.hoverPickable?.Value != MyLittleUI.StationHover.Vanilla)
            };

            ReconcileHoverOwnerships();

            if (!seasonsConfigHandlerBound && seasonsConfig != null)
            {
                seasonsConfig.SettingChanged += OnSeasonsConfigSettingChanged;
                seasonsConfigHandlerBound = true;
            }

            if (!myLittleUIConfigHandlerBound && MyLittleUI.instance?.Config != null)
            {
                MyLittleUI.instance.Config.SettingChanged += OnMyLittleUIConfigSettingChanged;
                myLittleUIConfigHandlerBound = true;
            }
        }

        private static bool EnsureDelegates()
        {
            if (!initialized)
                Initialize();

            if (!initialized)
                return false;

            object state = seasonStateField.GetValue(null);
            if (state == null || ReferenceEquals(failedSeasonState, state))
                return false;

            if (ReferenceEquals(boundSeasonState, state)
                && getSecondsToMakeHoney != null
                && getSecondsToGrowPlant != null
                && getSecondsToRespawnPickable != null
                && isProtectedPosition != null)
            {
                return true;
            }

            boundSeasonState = state;
            failedSeasonState = null;
            getSecondsToMakeHoney = null;
            getSecondsToGrowPlant = null;
            getSecondsToRespawnPickable = null;
            isProtectedPosition = null;

            try
            {
                Type type = state.GetType();
                MethodInfo honeyMethod = AccessTools.Method(
                    type,
                    nameof(GetSecondsToMakeHoney),
                    new[] { typeof(Beehive), typeof(int), typeof(float) });
                MethodInfo plantMethod = AccessTools.Method(
                    type,
                    nameof(GetSecondsToGrowPlant),
                    new[] { typeof(Plant) });
                MethodInfo pickableMethod = AccessTools.Method(
                    type,
                    nameof(GetSecondsToRespawnPickable),
                    new[] { typeof(Pickable) });
                MethodInfo protectedPositionMethod = AccessTools.Method(
                    seasonsType,
                    "IsProtectedPosition",
                    new[] { typeof(Vector3) });

                if (honeyMethod == null
                    || plantMethod == null
                    || pickableMethod == null
                    || protectedPositionMethod == null)
                {
                    throw new MissingMethodException("Required Seasons hover timing methods were not found.");
                }

                getSecondsToMakeHoney = (Func<Beehive, int, float, double>)Delegate.CreateDelegate(
                    typeof(Func<Beehive, int, float, double>), state, honeyMethod);
                getSecondsToGrowPlant = (Func<Plant, double>)Delegate.CreateDelegate(
                    typeof(Func<Plant, double>), state, plantMethod);
                getSecondsToRespawnPickable = (Func<Pickable, double>)Delegate.CreateDelegate(
                    typeof(Func<Pickable, double>), state, pickableMethod);
                isProtectedPosition = (Func<Vector3, bool>)Delegate.CreateDelegate(
                    typeof(Func<Vector3, bool>), protectedPositionMethod);

                delegateErrorLogged = false;
                invocationErrorLogged = false;
                return true;
            }
            catch (Exception exception)
            {
                failedSeasonState = state;
                if (!delegateErrorLogged)
                {
                    delegateErrorLogged = true;
                    MyLittleUI.LogWarning(
                        $"Failed to initialize Seasons hover timing delegates.{Environment.NewLine}{exception}");
                }
                return false;
            }
        }

        private static double InvocationFailed(string methodName, double fallback, Exception exception)
        {
            if (!invocationErrorLogged)
            {
                invocationErrorLogged = true;
                MyLittleUI.LogWarning(
                    $"Failed to call Seasons.{methodName}; using the vanilla duration.{Environment.NewLine}{exception}");
            }
            return fallback;
        }

        internal static double GetSecondsToMakeHoney(
            Beehive beehive,
            int amount,
            float product,
            double fallback)
        {
            if (!EnsureDelegates())
                return fallback;

            try
            {
                return getSecondsToMakeHoney(beehive, amount, product);
            }
            catch (Exception exception)
            {
                return InvocationFailed(nameof(GetSecondsToMakeHoney), fallback, exception);
            }
        }

        internal static double GetSecondsToGrowPlant(Plant plant, double fallback)
        {
            if (!EnsureDelegates())
                return fallback;

            try
            {
                return getSecondsToGrowPlant(plant);
            }
            catch (Exception exception)
            {
                return InvocationFailed(nameof(GetSecondsToGrowPlant), fallback, exception);
            }
        }

        internal static double GetSecondsToRespawnPickable(Pickable pickable, double fallback)
        {
            if (!EnsureDelegates())
                return fallback;

            try
            {
                if (isProtectedPosition(pickable.transform.position))
                    return fallback;

                double secondsRemaining = getSecondsToRespawnPickable(pickable);
                if (double.IsNaN(secondsRemaining) || secondsRemaining < 0d)
                    return fallback;

                if (double.IsPositiveInfinity(secondsRemaining))
                    return secondsRemaining;

                long pickedTime = pickable.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L);
                if (pickedTime <= 1)
                    return fallback;

                double elapsedSeconds = (ZNet.instance.GetTime() - new DateTime(pickedTime)).TotalSeconds;
                return Math.Max(0d, elapsedSeconds) + secondsRemaining;
            }
            catch (Exception exception)
            {
                return InvocationFailed(nameof(GetSecondsToRespawnPickable), fallback, exception);
            }
        }

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
        private static class FejdStartup_Awake_Initialize
        {
            private static void Postfix() => Initialize();
        }
    }
}
