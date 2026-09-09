from pathlib import Path
import re
import subprocess

texts = {}
formats = {}

def load(path):
    if path not in texts:
        data = Path(path).read_bytes()
        formats[path] = (data.startswith(b'\xef\xbb\xbf'), b'\r\n' in data)
        texts[path] = data.decode('utf-8-sig').replace('\r\n', '\n')
    return texts[path]

def replace(path, old, new, count=1):
    text = load(path)
    actual = text.count(old)
    if actual != count:
        raise RuntimeError(f'{path}: expected {count} matches, found {actual}: {old[:160]}')
    texts[path] = text.replace(old, new)

def section(path, start, end, new):
    text = load(path)
    if text.count(start) != 1:
        raise RuntimeError(f'{path}: section start is not unique: {start}')
    first = text.index(start)
    last = text.index(end, first + len(start))
    texts[path] = text[:first] + new.rstrip() + '\n\n' + text[last:]

# Keep cancellation and hiding as separate Harmony targets.
replace('Crafting/MultiCraft.cs', '        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]\n', '')
replace('Crafting/MultiCraft.cs', '        private static bool showPanel;', '        private static bool showPanel;\n        private static bool progressTextChanged;')
replace('Crafting/MultiCraft.cs', '        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetRecipe))]', '''        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        private static class InventoryGui_Hide_StopQueue
        {
            private static void Prefix() => StopQueue();
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetRecipe))]''')
replace('Crafting/MultiCraft.cs', '''                UpdateMulticraftPanel();
                if (!IsMulticraftEnabled)
                    return;

                if (textCrafting && queueGui == __instance && amount > 1)
                    textCrafting.SetText(Localization.instance.Localize($"$inventory_craftingprog ({amount})"));
                if (!showPanel)
                    return;''', '''                UpdateMulticraftPanel();
                if (textCrafting)
                {
                    if (IsMulticraftEnabled && queueGui == __instance && amount > 1)
                    {
                        textCrafting.SetText(Localization.instance.Localize($"$inventory_craftingprog ({amount})"));
                        progressTextChanged = true;
                    }
                    else if (progressTextChanged)
                    {
                        textCrafting.SetText(Localization.instance.Localize("$inventory_craftingprog"));
                        progressTextChanged = false;
                    }
                }
                if (!IsMulticraftEnabled || !showPanel)
                {
                    if (queueNextCraft && !isCrafting)
                        StopQueue();
                    return;
                }''')
replace('Crafting/MultiCraft.cs', '                showPanel = false;', '                showPanel = false;\n                progressTextChanged = false;')
replace('AmountScrollHandler.cs', '        public void OnPointerEnter(PointerEventData eventData)', '''        private void OnDisable()
        {
            hovered = false;
        }

        public void OnPointerEnter(PointerEventData eventData)''')
replace('Crafting/CraftFilter.cs', '''                if (gamepad.m_hint)
                    gamepad.m_hint.SetActive(false);''', '''                gamepad.enabled = false;
                if (gamepad.m_hint && gamepad.m_hint.transform.IsChildOf(clearButton.transform))
                    gamepad.m_hint.SetActive(false);
                gamepad.m_hint = null;''')

# Forecasts use the complete sector and never leak seeded Unity random state.
replace('WeatherForecast.cs', '        public static void UpdateWeather()', '''        public static void Reset()
        {
            currentBiome = default;
            inAshlandsOrDeepnorth = false;
            environmentPeriod = windPeriod = -1L;
            nextWeatherChange = nextWindChange = 0L;
            nextWeatherState = WeatherState.Clear;
            windsTransitionTimer = -1f;
            windList.Clear();
            winds.Clear();
            windsTransition.Clear();
        }

        public static void UpdateWeather()''')
replace('WeatherForecast.cs', '            InfoBlocks.forecastObject.SetActive(forecastEnabled.Value && nextWeatherChange > 0);', '''            if (!InfoBlocks.forecastObject)
                return;

            InfoBlocks.forecastObject.SetActive(modEnabled.Value && forecastEnabled.Value && nextWeatherChange > 0);''')
replace('WeatherForecast.cs', '            if (!windsEnabled.Value)\n                return;', '            if (!modEnabled.Value || !windsEnabled.Value || !EnvMan.instance || !InfoBlocks.windsObject)\n                return;')
replace('WeatherForecast.cs', 'Math.Min(windList.Count, windsTransition.Count)', 'Math.Min(windList.Count, Math.Min(winds.Count, windsTransition.Count))')
replace('WeatherForecast.cs', '''                Quaternion quaternion = Quaternion.LookRotation((Vector3)wind);

                Image arrow = windList[i].GetComponent<Image>();''', '''                if (!windList[i] || ((Vector3)wind).sqrMagnitude < 0.0001f)
                    continue;
                Quaternion quaternion = Quaternion.LookRotation((Vector3)wind);

                Image arrow = windList[i].GetComponent<Image>();
                if (!arrow)
                    continue;''')
replace('WeatherForecast.cs', 'return EnvMan.instance.m_windPeriodDuration / 8L;', 'return Math.Max(1L, EnvMan.instance ? EnvMan.instance.m_windPeriodDuration / 8L : 1L);')
replace('WeatherForecast.cs', 'return EnvMan.instance.m_environmentDuration;', 'return Math.Max(1L, EnvMan.instance ? EnvMan.instance.m_environmentDuration : 1L);')
replace('WeatherForecast.cs', 'return (int)sec / GetWindPeriodDuration();', 'return (long)sec / GetWindPeriodDuration();')
section('WeatherForecast.cs', '            private static long preCalculatedPeriod = -1L;', '        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.UpdateWind))]', '''            public static void Prefix(EnvMan __instance, out long __state)
            {
                __state = __instance.m_environmentPeriod;
            }

            public static void Postfix(EnvMan __instance, long __state)
            {
                if (!modEnabled.Value || !forecastEnabled.Value || !InfoBlocks.forecastObject)
                    return;

                if (__instance.m_environmentPeriod == environmentPeriod && __instance.m_environmentPeriod == __state
                    && __instance.m_currentBiome == currentBiome && __instance.m_inAshlandsOrDeepnorth == inAshlandsOrDeepnorth)
                {
                    UpdateWeatherTimer();
                    return;
                }

                environmentPeriod = __instance.m_environmentPeriod;
                currentBiome = __instance.m_currentBiome;
                inAshlandsOrDeepnorth = __instance.m_inAshlandsOrDeepnorth;
                UpdateNextWeather();
            }
        }''')
replace('WeatherForecast.cs', '                if (!modEnabled.Value || !windsEnabled.Value)', '                if (!modEnabled.Value || !windsEnabled.Value || !InfoBlocks.windsObject)')
replace('WeatherForecast.cs', '''            if (environmentPeriod > 0 && (string.IsNullOrEmpty(EnvMan.instance.m_forceEnv) || EnvMan.instance.GetEnv(EnvMan.instance.m_forceEnv) == null))
            {
                Vector3 position = Utils.GetMainCamera().transform.position;''', '''            Camera camera = Utils.GetMainCamera();
            if (modEnabled.Value && forecastEnabled.Value && EnvMan.instance && camera && environmentPeriod >= 0
                && (string.IsNullOrEmpty(EnvMan.instance.m_forceEnv) || EnvMan.instance.GetEnv(EnvMan.instance.m_forceEnv) == null))
            {
                Vector3 position = camera.transform.position;''')
replace('WeatherForecast.cs', '            if (!windsEnabled.Value || !EnvMan.instance)', '            if (!modEnabled.Value || !windsEnabled.Value || !EnvMan.instance || !InfoBlocks.windsObject || !InfoBlocks.windTemplate)')
replace('WeatherForecast.cs', '            if (windPeriod > 0)', '            if (windPeriod >= 0)')
replace('WeatherForecast.cs', 'ps => ps.name != null && environmentSystems.Contains(ps.name)', 'ps => ps && environmentSystems.Contains(ps.name)')
section('WeatherForecast.cs', '        private static Vector4 GetWind(int period)', '        private static WeatherState GetWeatherState', '''        private static Vector4 GetWind(int period)
        {
            if (!EnvMan.instance)
                return Vector4.zero;

            UnityEngine.Random.State state = UnityEngine.Random.state;
            try
            {
                float angle = 0f;
                float intensity = 0.5f;
                long timeSec = (windPeriod + period) * GetWindPeriodDuration();
                EnvMan.instance.AddWindOctave(timeSec, 1, ref angle, ref intensity);
                EnvMan.instance.AddWindOctave(timeSec, 2, ref angle, ref intensity);
                EnvMan.instance.AddWindOctave(timeSec, 4, ref angle, ref intensity);
                EnvMan.instance.AddWindOctave(timeSec, 8, ref angle, ref intensity);
                return new Vector4(Mathf.Sin(angle), 0f, Mathf.Cos(angle), Mathf.Clamp(intensity, 0.05f, 1f));
            }
            finally
            {
                UnityEngine.Random.state = state;
            }
        }''')
section('WeatherForecast.cs', '        private static EnvSetup GetEnvironment(long period, BiomeSector biome, bool isAshlands, bool isDeepNorth)', '        private static EnvSetup GetAvailableEnvironment', '''        private static EnvSetup GetEnvironment(long period, BiomeSector biome, bool isAshlands, bool isDeepNorth)
        {
            UnityEngine.Random.State state = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState((int)period);
                return GetAvailableEnvironment(biome, isAshlands, isDeepNorth);
            }
            finally
            {
                UnityEngine.Random.state = state;
            }
        }''')

# Dispose session-specific UI state even when the mod was disabled before leaving.
replace('InfoBlocks.cs', '(1 / (fuzzyTime.Length * 2))', '(1f / (fuzzyTime.Length * 2))')
replace('InfoBlocks.cs', '''                if (!modEnabled.Value)
                    return;

                WeatherForecast.windList.Clear();
                WeatherForecast.winds.Clear();
                WeatherForecast.windsTransition.Clear();''', '''                WeatherForecast.Reset();
                minimapHiddenByToggle = false;
                minimapBackground = null;
                clockBackground = null;
                forecastBackground = null;
                windsBackground = null;
                weatherIcon = null;''')
replace('InfoBlocks.cs', '''            if (!clockObject)
                return;

            clockObject.SetActive''', '''            if (!clockObject || !clockTimeObject || !clockDayObject || !timeText || !dayText || !Minimap.instance)
                return;

            clockObject.SetActive''')

# Carry capacity can change on natural status-effect expiration without an inventory event.
replace('InventoryPanel.cs', '''                if (!modEnabled.Value)
                    return;

                weight = null;''', '''                totalWeight = maxWeight = emptySlots = maxSlots = 0;
                weight = null;''')
replace('InventoryPanel.cs', '''                int currentWeight = showWeightLeft.Value ? maxWeight - totalWeight : totalWeight;''', '''                maxWeight = Mathf.FloorToInt(Player.m_localPlayer.GetMaxCarryWeight());
                int currentWeight = showWeightLeft.Value ? maxWeight - totalWeight : totalWeight;''')
replace('InventoryPanel.cs', 'item => item.m_gridPos.x < width && item.m_gridPos.y < height', 'item => item.m_gridPos.x >= 0 && item.m_gridPos.y >= 0 && item.m_gridPos.x < width && item.m_gridPos.y < height')
replace('InventoryPanel.cs', '''            public static void Postfix(StatusEffect __result)
            {
                if (!modEnabled.Value)
                    return;

                if (__result is SE_Stats se && se.m_addMaxCarryWeight > 0)
                    UpdateVisuals();''', '''            public static void Postfix(SEMan __instance, StatusEffect __result)
            {
                if (!modEnabled.Value || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetSEMan())
                    return;

                if (__result is SE_Stats se && se.m_addMaxCarryWeight != 0f)
                    UpdateStats();''')

# Reset only the aggregated fields; new SE_Stats defaults must not be erased by reflection.
section('InventoryCharacterStats.cs', '        private static void ClearStats(this SE_Stats statsEffect)', '        public static void UpdateTooltipState()', '''        private static void ClearStats(this SE_Stats statsEffect)
        {
            statsEffect.m_mods.Clear();
            statsEffect.m_jumpStaminaUseModifier = 0f;
            statsEffect.m_runStaminaDrainModifier = 0f;
            statsEffect.m_healthOverTime = 0f;
            statsEffect.m_staminaOverTime = 0f;
            statsEffect.m_eitrOverTime = 0f;
            statsEffect.m_healthRegenMultiplier = 1f;
            statsEffect.m_staminaRegenMultiplier = 1f;
            statsEffect.m_eitrRegenMultiplier = 1f;
            statsEffect.m_addMaxCarryWeight = 0f;
            statsEffect.m_noiseModifier = 0f;
            statsEffect.m_stealthModifier = 0f;
            statsEffect.m_speedModifier = 0f;
            statsEffect.m_maxMaxFallSpeed = 0f;
            statsEffect.m_fallDamageModifier = 0f;
            statsEffect.m_adrenalineModifier = 0f;
            statsEffect.m_staggerModifier = 0f;
            statsEffect.m_blockStaminaUseFlatValue = 0f;
            statsEffect.m_timedBlockBonus = 0f;
            statsEffect.m_swimSpeedModifier = 0f;
            statsEffect.m_addArmor = 0f;
            statsEffect.m_armorMultiplier = 0f;
        }''')
replace('InventoryCharacterStats.cs', 'totalSecondTooltipWasUpdated = 0', 'totalSecondTooltipWasUpdated = double.NegativeInfinity', 5)
replace('InventoryCharacterStats.cs', 'characterStatsTooltip.enabled = statsCharacterArmor.Value;', 'characterStatsTooltip.enabled = modEnabled.Value && statsCharacterArmor.Value;')
replace('InventoryCharacterStats.cs', 'characterEffectsTooltip.enabled = statsCharacterEffects.Value;', 'characterEffectsTooltip.enabled = modEnabled.Value && statsCharacterEffects.Value;')
replace('InventoryCharacterStats.cs', '''            UITooltip prefabTooltip = __instance.m_containerGrid.m_elementPrefab.GetComponent<UITooltip>();''', '''            if (!player || !__instance.m_containerGrid || !__instance.m_containerGrid.m_elementPrefab || !__instance.m_armor || !__instance.m_weight)
                return;
            UITooltip prefabTooltip = __instance.m_containerGrid.m_elementPrefab.GetComponent<UITooltip>();
            if (!prefabTooltip || !prefabTooltip.m_tooltipPrefab)
                return;''')
replace('InventoryCharacterStats.cs', '''            foreach (FieldInfo field in fields)
                field.SetValue(tooltip, field.GetValue(uiTooltip));''', '''            foreach (FieldInfo field in fields)
                if (!field.IsStatic && !field.IsInitOnly)
                    field.SetValue(tooltip, field.GetValue(uiTooltip));''')
replace('InventoryCharacterStats.cs', 'i < player.m_equipmentModifierValues.Length', 'i < System.Math.Min(player.m_equipmentModifierValues.Length, Player.s_equipmentModifierTooltips.Length)')
replace('InventoryCharacterStats.cs', '''            if (stats == null)
                stats = ScriptableObject.CreateInstance("SE_Stats") as SE_Stats;
            else
                stats.ClearStats();''', '''            if (!stats)
                stats = ScriptableObject.CreateInstance<SE_Stats>();
            stats.ClearStats();''')
replace('InventoryCharacterStats.cs', '''                if (!modEnabled.Value)
                    return;

                if (!statsCharacterArmor.Value && !statsCharacterEffects.Value)''', '''                if (!modEnabled.Value)
                    return;

                if (!statsCharacterArmor.Value && !statsCharacterEffects.Value)''', 4)
replace('InventoryCharacterStats.cs', '''                if (ZNet.instance.GetTimeSeconds() - totalSecondTooltipWasUpdated > 3)''', '''                if (!player || !ZNet.instance)
                    return;

                if (ZNet.instance.GetTimeSeconds() < totalSecondTooltipWasUpdated
                    || ZNet.instance.GetTimeSeconds() - totalSecondTooltipWasUpdated > 3)''')
replace('InventoryCharacterStats.cs', '''                    if (statsCharacterArmor.Value)
                        characterStatsTooltip.m_text''', '''                    if (statsCharacterArmor.Value && characterStatsTooltip)
                        characterStatsTooltip.m_text''')
replace('InventoryCharacterStats.cs', '''                    if (statsCharacterEffects.Value)
                        characterEffectsTooltip.m_text''', '''                    if (statsCharacterEffects.Value && characterEffectsTooltip)
                        characterEffectsTooltip.m_text''')
replace('InventoryCharacterStats.cs', '\n    }\n}', '''
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        private static class InventoryGui_OnDestroy_ClearTooltips
        {
            private static void Postfix()
            {
                characterStatsTooltip = null;
                characterEffectsTooltip = null;
                if (effectsTooltip)
                    Object.Destroy(effectsTooltip);
                effectsTooltip = null;
                if (stats)
                    Object.Destroy(stats);
                stats = null;
                skills.Clear();
                mods.Clear();
                totalSecondTooltipWasUpdated = double.NegativeInfinity;
            }
        }
    }
}''')

# Preserve native composite/appending layouts; format ordinary tooltips without mutating live items.
replace('ItemTooltip.cs', 'private static readonly Dictionary<int, string> tooltipCache = new Dictionary<int, string>();', '''private static readonly Dictionary<Tuple<ItemDrop.ItemData, int, bool, float, bool, string>, string> tooltipCache
            = new Dictionary<Tuple<ItemDrop.ItemData, int, bool, float, bool, string>, string>();
        private static bool buildingTooltip;''')
replace('ItemTooltip.cs', '''                    int currentQuality = item.m_quality;
                    item.m_quality = m_quality - 1;
                    string weight = item.GetWeight().ToString("0.0");
                    item.m_quality = currentQuality;''', '''                    ItemDrop.ItemData comparison = item.Clone();
                    comparison.m_quality = m_quality - 1;
                    string weight = comparison.GetWeight().ToString("0.0");''')
replace('ItemTooltip.cs', 'Math.Clamp(itemTooltipRecipeFontSize.Value, 1, InventoryGui.instance.m_recipeDecription.fontSizeMax)', 'Mathf.Clamp(itemTooltipRecipeFontSize.Value, 1, InventoryGui.instance.m_recipeDecription.fontSizeMax)')
section('ItemTooltip.cs', '        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip)', '\n    }\n}', r'''        private static string RecolorTooltip(string text)
        {
            return text.Replace("<color=orange>", itemTooltipColored.Value ? "<color=#ffa500ff>" : "<color=#add8e6ff>")
                .Replace("<color=yellow>", itemTooltipColored.Value ? "<color=#ffff00ff>" : "<color=#c0c0c0ff>")
                .Replace("<color=silver>", "<color=#c0c0c0ff>")
                .Replace("<color=lightblue>", "<color=#add8e6ff>")
                .Replace("\n\n\n", "\n\n");
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int),
            typeof(bool), typeof(float), typeof(int), typeof(bool))]
        private class ItemDropItemData_GetTooltip_ItemTooltip
        {
            [HarmonyPriority(Priority.First)]
            private static void Postfix(ItemDrop.ItemData item, int qualityLevel, bool crafting, float worldLevel, bool appending, ref string __result)
            {
                if (!modEnabled.Value || !itemTooltip.Value || buildingTooltip || item?.m_shared == null
                    || !Player.m_localPlayer || string.IsNullOrEmpty(__result)
                    || UnityInput.Current.GetKey(KeyCode.LeftAlt) || UnityInput.Current.GetKey(KeyCode.RightAlt))
                    return;

                ItemDrop appended = item.m_shared.m_appendToolTip;
                if (appending || (appended && appended.m_itemData.m_shared.m_food <= 0f
                    && appended.m_itemData.m_shared.m_foodStamina <= 0f && appended.m_itemData.m_shared.m_foodEitr <= 0f))
                {
                    // Recursive, non-food tooltips have no reliable boundary between their item sections.
                    // Preserve the native layout instead of merging statistics from different items.
                    __result = RecolorTooltip(__result);
                    return;
                }

                var key = Tuple.Create(item, qualityLevel, crafting, worldLevel, itemTooltipColored.Value, __result);
                if (!crafting && tooltipCache.TryGetValue(key, out string cached))
                {
                    __result = cached;
                    return;
                }
                int descriptionEnd = __result.IndexOf("\n\n", StringComparison.Ordinal);
                if (descriptionEnd < 0)
                    return;

                buildingTooltip = true;
                try
                {
                    string description = __result.Substring(0, descriptionEnd + 2);
                    string body = __result.Substring(description.Length);
                    int footerIndex = body.Length;
                    foreach (string tail in tails)
                        FindFooter(tail);

                    float skill = Player.m_localPlayer.GetSkillLevel(item.m_shared.m_skillType);
                    string statusEffect = item.GetStatusEffectTooltip(qualityLevel, skill);
                    if (!string.IsNullOrEmpty(statusEffect))
                        FindFooter("\n\n" + statusEffect);
                    string chain = item.GetChainTooltip(qualityLevel, skill);
                    if (!string.IsNullOrEmpty(chain))
                        FindFooter("\n\n" + chain);

                    string footer = body.Substring(footerIndex);
                    body = body.Substring(0, footerIndex);
                    sb.Clear();
                    sb.Append(description);
                    tokenPositions.Clear();
                    arrResult.Clear();
                    arrResult.AddRange(body.Split(new[] { '\n' }, StringSplitOptions.None));
                    string lastToken = null;
                    for (int i = 0; i < arrResult.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(arrResult[i]))
                            continue;
                        string token = localizedTooltipTokens.Keys.FirstOrDefault(value => arrResult[i].IndexOf(value, StringComparison.Ordinal) >= 0);
                        if (token != null)
                        {
                            if (!tokenPositions.TryGetValue(token, out List<int> positions))
                            {
                                positions = new List<int>();
                                tokenPositions.Add(token, positions);
                            }
                            positions.Add(i);
                            lastToken = token;
                        }
                        else if (lastToken != null)
                            tokenPositions[lastToken].Add(i);
                        else
                            sb.Append(arrResult[i]).Append('\n');
                    }

                    ReorderTooltip(item, qualityLevel, worldLevel,
                        upgradingTooltip: crafting && qualityLevel > 1 && qualityLevel <= item.m_shared.m_maxQuality);
                    sb.Append(footer);
                    __result = RecolorTooltip(sb.ToString());
                    if (!crafting)
                    {
                        if (tooltipCache.Count >= tooltipCachedEntriesMax)
                            tooltipCache.Clear();
                        tooltipCache[key] = __result;
                    }

                    void FindFooter(string marker)
                    {
                        if (string.IsNullOrEmpty(marker))
                            return;
                        int index = body.IndexOf(marker, StringComparison.Ordinal);
                        if (index >= 0)
                            footerIndex = Math.Min(footerIndex, index);
                    }
                }
                finally
                {
                    buildingTooltip = false;
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        private static class InventoryGui_OnDestroy_ClearTooltipCache
        {
            private static void Postfix() => tooltipCache.Clear();
        }''')

# A compact callback still rejects an empty symbol before indexing it.
replace('MyLittleUI.cs', '''itemQualitySymbol.SettingChanged += (sender, args) => { if (!string.IsNullOrEmpty(itemQualitySymbol.Value) && itemQualitySymbol.Value.Length > 1) itemQualitySymbol.Value = itemQualitySymbol.Value[0].ToString(); };''', '''itemQualitySymbol.SettingChanged += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(itemQualitySymbol.Value) && itemQualitySymbol.Value.Length > 1)
                    itemQualitySymbol.Value = itemQualitySymbol.Value[0].ToString();
            };''')

for path, text in texts.items():
    bom, crlf = formats[path]
    if crlf:
        text = text.replace('\n', '\r\n')
    Path(path).write_bytes((b'\xef\xbb\xbf' if bom else b'') + text.encode('utf-8'))

subprocess.run(['git', 'diff', '--check'], check=True)
for path in texts:
    if re.search(r'[\u0400-\u052f]', Path(path).read_text(encoding='utf-8-sig')):
        raise RuntimeError(f'Unexpected Cyrillic text in {path}')
print('Updated source files:', ', '.join(sorted(texts)))
print('No compilation, game execution, or mod tests were performed.')
