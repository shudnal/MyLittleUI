using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;

namespace MyLittleUI
{
    internal static class SeasonsHoverCompatibility
    {
        internal const string GUID = "shudnal.Seasons";

        private static PluginInfo seasonsPlugin;
        private static Type seasonsType;
        private static FieldInfo seasonStateField;
        private static object boundSeasonState;
        private static object failedSeasonState;

        private static Func<Beehive, int, float, double> getSecondsToMakeHoney;
        private static Func<Plant, double> getSecondsToGrowPlant;
        private static Func<Pickable, double> getSecondsToRespawnPickable;

        private static ConfigEntryBase seasonsHoverBeeHive;
        private static ConfigEntryBase seasonsHoverPlant;
        private static ConfigEntryBase seasonsHoverPickable;
        private static ConfigFile seasonsConfig;

        private static bool initialized;
        private static bool configHandlerBound;
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

        private static void EnforceVanilla(ConfigEntryBase entry)
        {
            if (entry == null || entry.BoxedValue == null || entry.BoxedValue.ToString() == "Vanilla")
                return;

            MyLittleUI.LogWarning(
                $"Seasons hover '{entry.Definition.Key}' was reset to Vanilla because My Little UI controls this hover.");

            try
            {
                suppressSettingHandler = true;
                entry.BoxedValue = Enum.Parse(entry.SettingType, "Vanilla");
            }
            finally
            {
                suppressSettingHandler = false;
            }
        }

        private static void OnSeasonsConfigSettingChanged(object sender, SettingChangedEventArgs args)
        {
            if (suppressSettingHandler || args?.ChangedSetting == null)
                return;

            ConfigEntryBase entry = args.ChangedSetting;
            if (ReferenceEquals(entry, seasonsHoverBeeHive)
                || ReferenceEquals(entry, seasonsHoverPlant)
                || ReferenceEquals(entry, seasonsHoverPickable))
            {
                EnforceVanilla(entry);
            }
        }

        private static void EnsureHoverOwnership()
        {
            seasonsHoverBeeHive ??= GetSeasonsConfigEntry("hoverBeeHive");
            seasonsHoverPlant ??= GetSeasonsConfigEntry("hoverPlant");
            seasonsHoverPickable ??= GetSeasonsConfigEntry("hoverPickable");

            EnforceVanilla(seasonsHoverBeeHive);
            EnforceVanilla(seasonsHoverPlant);
            EnforceVanilla(seasonsHoverPickable);

            if (configHandlerBound
                || seasonsConfig == null
                || seasonsHoverBeeHive == null
                || seasonsHoverPlant == null
                || seasonsHoverPickable == null)
            {
                return;
            }

            seasonsConfig.SettingChanged += OnSeasonsConfigSettingChanged;
            configHandlerBound = true;
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
                && getSecondsToRespawnPickable != null)
            {
                return true;
            }

            boundSeasonState = state;
            failedSeasonState = null;
            getSecondsToMakeHoney = null;
            getSecondsToGrowPlant = null;
            getSecondsToRespawnPickable = null;

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

                if (honeyMethod == null || plantMethod == null || pickableMethod == null)
                    throw new MissingMethodException("Required Seasons hover timing methods were not found.");

                getSecondsToMakeHoney = (Func<Beehive, int, float, double>)Delegate.CreateDelegate(
                    typeof(Func<Beehive, int, float, double>), state, honeyMethod);
                getSecondsToGrowPlant = (Func<Plant, double>)Delegate.CreateDelegate(
                    typeof(Func<Plant, double>), state, plantMethod);
                getSecondsToRespawnPickable = (Func<Pickable, double>)Delegate.CreateDelegate(
                    typeof(Func<Pickable, double>), state, pickableMethod);

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
                return getSecondsToRespawnPickable(pickable);
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
