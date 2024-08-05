using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI
{
    internal class WeatherForecast
    {
        public enum WeatherState
        {
            Clear,
            Rain,
            Snow,
            Thunder,
            Mist,
            RainCinder
        }

        public static Heightmap.Biome currentBiome;
        public static bool inAshlandsOrDeepnorth;
        public static long environmentPeriod;
        public static long windPeriod;

        public static long nextWindChange;

        public static long nextWeatherChange;
        public static WeatherState nextWeatherState;
        
        // env dur 666 wind dur 1000

        public static Sprite iconClear;
        public static Sprite iconRain;
        public static Sprite iconSnow;
        public static Sprite iconThunder;
        public static Sprite iconMist;
        public static Sprite iconRainCinder;

        public static void UpdateWeather()
        {
            if (!EnvMan.instance)
                return;

            UpdateWeatherIcon();
            UpdateWeatherTimer();
            InfoBlocks.UpdateForecastBackground();
        }

        public static void UpdateWind()
        {
            if (!EnvMan.instance)
                return;

            UpdateWindTimer();
            InfoBlocks.UpdateForecastBackground();
        }

        public static long GetWindPeriodDuration()
        {
            return EnvMan.instance.m_windPeriodDuration / 8;
        }

        public static double GetCurrentWindPeriod(long sec)
        {
            return sec / GetWindPeriodDuration();
        }

        public static double TimeToNextWind(long sec)
        {
            return (1 - (GetCurrentWindPeriod(sec) - (int)GetCurrentWindPeriod(sec))) * GetWindPeriodDuration();
        }
        
        public static void UpdateWeatherTimer()
        {
            if (nextWeatherChange > 0)
                InfoBlocks.weatherText?.SetText(GetNextWeatherTimer());
        }

        public static void UpdateWeatherIcon()
        {
            if (nextWeatherChange > 0 && InfoBlocks.weatherIcon != null)
                InfoBlocks.weatherIcon.overrideSprite = GetWeatherSprite();
        }

        public static Sprite GetWeatherSprite()
        {
            return nextWeatherState switch
            {
                WeatherState.Rain => iconRain,
                WeatherState.Snow => iconSnow,
                WeatherState.Thunder => iconThunder,
                WeatherState.Mist => iconMist,
                WeatherState.RainCinder => iconRainCinder,
                _ => iconClear,
            };
        }

        public static void UpdateWindTimer()
        {

        }

        private static string GetNextWeatherTimer()
        {
            if (nextWeatherChange < EnvMan.instance.m_totalSeconds)
                return "";

            return GetWeatherTimerString(nextWeatherChange - EnvMan.instance.m_totalSeconds);
        }

        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.UpdateEnvironment))]
        public static class EnvMan_UpdateEnvironment_UpdateForecast
        {
            public static void Postfix(EnvMan __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (__instance.m_environmentPeriod == environmentPeriod && __instance.m_currentBiome == currentBiome && __instance.m_inAshlandsOrDeepnorth == inAshlandsOrDeepnorth)
                {
                    UpdateWeatherTimer();
                    return;
                }

                environmentPeriod = __instance.m_environmentPeriod;
                currentBiome = __instance.m_currentBiome;
                inAshlandsOrDeepnorth = __instance.m_inAshlandsOrDeepnorth;

                UpdateNextWeather();
            }
        }

        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.UpdateWind))]
        public static class EnvMan_UpdateWind_UpdateWindStatus
        {
            public static void Postfix(EnvMan __instance, long timeSec, float dt)
            {
                if (!modEnabled.Value)
                    return;

                if (windPeriod == GetCurrentWindPeriod(timeSec))
                {
                    UpdateWindTimer();
                    return;
                }

                windPeriod = (long)GetCurrentWindPeriod(timeSec);

            }
        }

        private static string GetWeatherTimerString(double seconds)
        {
            if (seconds < 60)
                return DateTime.FromBinary(599266080000000000).AddSeconds(seconds).ToString(@"ss\s");

            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.Hours > 0)
                return string.Format((int)seconds % 2 == 0 ? "{0:d2}:{1:d2}" : "{0:d2}<alpha=#00>:<alpha=#ff>{1:d2}", (int)span.TotalHours, span.Minutes);
            else
                return span.ToString(@"mm\:ss");
        }

        private static string TimerString(double seconds)
        {
            if (seconds < 60)
                return DateTime.FromBinary(599266080000000000).AddSeconds(seconds).ToString(@"ss\s");

            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours > 24)
                return string.Format("{0:d2}:{1:d2}:{2:d2}", (int)span.TotalHours, span.Minutes, span.Seconds);
            else
                return span.ToString(span.Hours > 0 ? @"hh\:mm\:ss" : @"mm\:ss");
        }

        public static void UpdateNextWeather()
        {
            nextWeatherState = WeatherState.Clear;
            nextWeatherChange = 0;

            if (environmentPeriod > 0 && (string.IsNullOrEmpty(EnvMan.instance.m_forceEnv) || EnvMan.instance.GetEnv(EnvMan.instance.m_forceEnv) == null))
            {
                Vector3 position = Utils.GetMainCamera().transform.position;
                bool inAshlands = WorldGenerator.IsAshlands(position.x, position.z);
                bool inDeepNorth = WorldGenerator.IsDeepnorth(position.x, position.y);

                WeatherState currentState = GetWeatherState(EnvMan.instance.m_nextEnv ?? EnvMan.instance.GetCurrentEnvironment());
                for (int i = 1; i <= 11; i++)
                {
                    WeatherState nextState = GetWeatherState(GetEnvironment(environmentPeriod + i, currentBiome, inAshlands, inDeepNorth));
                    if (currentState != nextState)
                    {
                        nextWeatherState = nextState;
                        nextWeatherChange = (environmentPeriod + i) * EnvMan.instance.m_environmentDuration;
                        break;
                    }
                }
            }

            UpdateWeather();
        }

        private static WeatherState GetWeatherState(EnvSetup env)
        {
            if (env == null)
                return WeatherState.Clear;

            if (IsInList(env, forecastListRainCinder.Value.Split(',')))
                return WeatherState.RainCinder;
            else if (IsInList(env, forecastListMist.Value.Split(',')))
                return WeatherState.Mist;
            else if (IsInList(env, forecastListThunder.Value.Split(',')))
                return WeatherState.Thunder;
            else if (IsInList(env, forecastListSnow.Value.Split(',')))
                return WeatherState.Snow;
            else if (IsInList(env, forecastListRain.Value.Split(',')))
                return WeatherState.Rain;

            return WeatherState.Clear;
        }

        private static bool IsInList(EnvSetup env, string[] environmentSystems)
        {
            return env.m_envObject != null && environmentSystems.Contains(env.m_envObject.name) ||
                   env.m_psystems != null && env.m_psystems.Any(ps => ps.name != null && environmentSystems.Contains(ps.name));
        }

        private static EnvSetup GetEnvironment(long period, Heightmap.Biome biome, bool isAshlands, bool isDeepNorth)
        {
            UnityEngine.Random.State state = UnityEngine.Random.state;
            UnityEngine.Random.InitState((int)period);

            EnvSetup env = GetAvailableEnvironment(biome, isAshlands, isDeepNorth);

            UnityEngine.Random.state = state;

            return env;
        }

        private static EnvSetup GetAvailableEnvironment(Heightmap.Biome biome, bool isAshlands, bool isDeepNorth)
        {
            List<EnvEntry> availableEnvironments = EnvMan.instance.GetAvailableEnvironments(biome);
            if (availableEnvironments != null && availableEnvironments.Count > 0)
            {
                EnvSetup envSetup = EnvMan.instance.SelectWeightedEnvironment(availableEnvironments);
                foreach (EnvEntry item in availableEnvironments)
                {
                    if (item.m_ashlandsOverride && isAshlands)
                    {
                        envSetup = item.m_env;
                    }

                    if (item.m_deepnorthOverride && isDeepNorth)
                    {
                        envSetup = item.m_env;
                    }
                }

                return envSetup;
            }

            return null;
        }

        [HarmonyPatch(typeof(Game), nameof(Game.UpdateNoMap))]
        public static class Game_UpdateNoMap_UpdateForecastPosition
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                InfoBlocks.UpdateForecastBlock();
            }
        }
    }
}
