using System.Reflection;
using System.Text.Json.Nodes;
using _botplacementsystem.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _botplacementsystem.Globals;

/// <summary>
/// Weighted-random preset roller. Each preset supplies a set of multipliers
/// (and optional behavior flags) that are applied to <see cref="ModConfig.Config"/>
/// just before the spawn cache rebuilds in <see cref="Controllers.MapSpawns.ConfigureInitialData"/>.
///
/// Preset shape (see Presets/Presets.json):
///   {
///     "name": {
///       "scavCapMult":    1.0,   // client-side: multiplies per-map MaxScavs cap
///       "pmcCapMult":     1.0,   // client-side: multiplies per-map MaxPmcs cap
///       "scavStartMult":  1.0,   // server-side: multiplies startingScavs.maxBotSpawns per map
///       "pmcStartMult":   1.0,   // server-side: multiplies startingPMCs.mapLimits per map
///       "guaranteeMainBosses": false, // server-side: each map's main boss -> 100% spawnChance
///       "roamingBosses":       false  // server-side: also wipe main boss bossZone so it picks anywhere
///     }
///   }
///
/// Cap multipliers are fetched by the client at raid start via the
/// /botplacementsystem/preset route. Start multipliers and boss flags are
/// applied server-side and baked into the cache directly.
/// </summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostSptModLoader)]
public class PresetManager
{
    private const string DefaultPresetName = "live-like";

    private readonly ISptLogger<PresetManager> _logger;
    private readonly RandomUtil _randomUtil;
    private readonly string _modPath;

    private readonly Dictionary<string, PresetDefinition> _presets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _weights = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    /// <summary>The label of the most recently rolled preset (e.g. "more-scavs").</summary>
    public string ActivePresetLabel { get; private set; } = DefaultPresetName;
    /// <summary>Cap multiplier to apply to <c>Plugin.*MaxScavs</c> on the client.</summary>
    public float ScavCapMult { get; private set; } = 1f;
    /// <summary>Cap multiplier to apply to <c>Plugin.*MaxPmcs</c> on the client.</summary>
    public float PmcCapMult { get; private set; } = 1f;
    /// <summary>When true, swap each present main boss for a random different one from the rotation pool.</summary>
    public bool RotateMainBosses { get; private set; }
    /// <summary>When true, ensure every map has the Goons squad (replace existing knight using its zone, else inject roaming).</summary>
    public bool RoamingGoonSquad { get; private set; }

    // (boss type, [maps where it's the main boss]). Pulled from SPT_Data and confirmed
    // against the user's tuning. Used by guaranteeMainBosses / roamingBosses flags.
    // Bosses already at 100% (Zryachiy on Lighthouse, Tagilla/KillaAgro on Labyrinth)
    // are intentionally omitted - no-op anyway.
    private static readonly (string Boss, string[] Maps)[] _mainBossesPerMap =
    [
        ("bossBully",    ["bigmap"]),
        ("bossTagilla",  ["factory4_day", "factory4_night"]),
        ("bossKilla",    ["interchange"]),
        ("bossGluhar",   ["rezervbase"]),
        ("bossSanitar",  ["shoreline"]),
        ("bossBoar",     ["tarkovstreets"]),
        ("bossKolontay", ["tarkovstreets"]),
        ("bossKojaniy",  ["woods"]),
    ];

    private static readonly string[] _maps =
    [
        "bigmap", "factory4_day", "factory4_night", "interchange",
        "lighthouse", "rezervbase", "sandbox", "sandbox_high",
        "shoreline", "tarkovstreets", "woods"
    ];
    // laboratory and labyrinth are excluded from start-count multipliers: starting
    // PMCs/scavs on those maps are 0 in the base config and applying multipliers
    // would do nothing.

    public PresetManager(
        ISptLogger<PresetManager> logger,
        RandomUtil randomUtil,
        ModHelper modHelper)
    {
        _logger = logger;
        _randomUtil = randomUtil;
        _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Re-rolls and applies the chosen preset to <see cref="ModConfig.Config"/>.
    /// When presets are disabled, restores Config to OriginalConfig so a previous
    /// overlay does not linger.
    /// </summary>
    public void RollAndApply()
    {
        // Always start from a clean Config so the previous preset's mutations
        // do not stack on top of whatever we apply this raid.
        ModConfig.RestoreFromOriginal();

        var cfg = ModConfig.Config?.PresetConfig;
        if (cfg == null || !cfg.Enable)
        {
            ActivePresetLabel = DefaultPresetName;
            ScavCapMult = 1f;
            PmcCapMult = 1f;
            RotateMainBosses = false;
            RoamingGoonSquad = false;
            return;
        }

        EnsureLoaded();

        var chosen = ChooseRandomPreset();
        ActivePresetLabel = chosen;

        if (!_presets.TryGetValue(chosen, out var preset))
        {
            ScavCapMult = 1f;
            PmcCapMult = 1f;
            RotateMainBosses = false;
            RoamingGoonSquad = false;
            return;
        }

        ScavCapMult = preset.ScavCapMult;
        PmcCapMult = preset.PmcCapMult;
        RotateMainBosses = preset.RotateMainBosses;
        RoamingGoonSquad = preset.RoamingGoonSquad;

        try
        {
            ApplyStartMultipliers(preset.ScavStartMult, preset.PmcStartMult);
            if (!string.IsNullOrWhiteSpace(preset.EscortAmount))
                ModConfig.Config.PmcConfig.Waves.EscortAmount = preset.EscortAmount;
            if (preset.GuaranteeMainBosses)
                ApplyMainBossOverrides(preset.RoamingBosses);

            _logger.Info($"[ABPS] Preset applied: {chosen} (scavCap x{preset.ScavCapMult:0.00}, pmcCap x{preset.PmcCapMult:0.00}, scavStart x{preset.ScavStartMult:0.00}, pmcStart x{preset.PmcStartMult:0.00}, escort='{preset.EscortAmount ?? "(base)"}', guaranteeBosses={preset.GuaranteeMainBosses}, roaming={preset.RoamingBosses}, rotateBosses={preset.RotateMainBosses}, goonSquad={preset.RoamingGoonSquad})");
        }
        catch (Exception ex)
        {
            _logger.Error($"[ABPS] Preset overlay failed for '{chosen}': {ex.Message}");
        }
    }

    private void ApplyStartMultipliers(float scavMult, float pmcMult)
    {
        var startingScavs = ModConfig.Config.ScavConfig.StartingScavs.MaxBotSpawns;
        var startingPmcs = ModConfig.Config.PmcConfig.StartingPMCs.MapLimits;

        foreach (var map in _maps)
        {
            if (Math.Abs(scavMult - 1f) > 0.0001f)
            {
                var baseline = startingScavs[map];
                startingScavs[map] = Math.Max(0, (int)Math.Round(baseline * scavMult));
            }
            if (Math.Abs(pmcMult - 1f) > 0.0001f)
            {
                var limits = startingPmcs[map];
                limits.Min = Math.Max(0, (int)Math.Round(limits.Min * pmcMult));
                limits.Max = Math.Max(0, (int)Math.Round(limits.Max * pmcMult));
                // record class -> mutation visible without reassignment
            }
        }
    }

    private void ApplyMainBossOverrides(bool roaming)
    {
        foreach (var (boss, maps) in _mainBossesPerMap)
        {
            BossLocationInfo info;
            try { info = ModConfig.Config.BossConfig[boss]; }
            catch { continue; } // unknown boss key, skip

            foreach (var map in maps)
            {
                info.SpawnChance[map] = 100;
                if (roaming)
                    info.BossZone[map] = "";
            }
        }
    }

    private string ChooseRandomPreset()
    {
        if (_weights.Count == 0) return DefaultPresetName;

        var total = _weights.Values.Where(w => w > 0).Sum();
        if (total <= 0) return DefaultPresetName;

        var roll = _randomUtil.GetInt(0, total - 1);
        var acc = 0;
        foreach (var kv in _weights)
        {
            if (kv.Value <= 0) continue;
            acc += kv.Value;
            if (roll < acc) return kv.Key;
        }
        return DefaultPresetName;
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        try
        {
            var presetsPath = Path.Combine(_modPath, "Presets", "Presets.json");
            var weightsPath = Path.Combine(_modPath, "Presets", "PresetWeightings.json");

            if (File.Exists(presetsPath))
            {
                var root = JsonNode.Parse(File.ReadAllText(presetsPath)) as JsonObject;
                if (root != null)
                {
                    foreach (var kv in root)
                    {
                        if (kv.Value is not JsonObject obj) continue;
                        _presets[kv.Key] = new PresetDefinition
                        {
                            ScavCapMult         = ReadFloat(obj, "scavCapMult",   1f),
                            PmcCapMult          = ReadFloat(obj, "pmcCapMult",    1f),
                            ScavStartMult       = ReadFloat(obj, "scavStartMult", 1f),
                            PmcStartMult        = ReadFloat(obj, "pmcStartMult",  1f),
                            EscortAmount        = ReadString(obj, "escortAmount", null),
                            GuaranteeMainBosses = ReadBool(obj,  "guaranteeMainBosses", false),
                            RoamingBosses       = ReadBool(obj,  "roamingBosses",       false),
                            RotateMainBosses    = ReadBool(obj,  "rotateMainBosses",    false),
                            RoamingGoonSquad    = ReadBool(obj,  "roamingGoonSquad",    false),
                        };
                    }
                }
            }
            else
            {
                _logger.Warning($"[ABPS] Presets.json not found at {presetsPath}");
            }

            if (File.Exists(weightsPath))
            {
                var root = JsonNode.Parse(File.ReadAllText(weightsPath)) as JsonObject;
                if (root != null)
                {
                    foreach (var kv in root)
                    {
                        if (kv.Value is JsonValue v && v.TryGetValue<int>(out var w))
                            _weights[kv.Key] = w;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[ABPS] Failed to load presets: {ex.Message}");
        }
        finally
        {
            _loaded = true;
        }
    }

    private static float ReadFloat(JsonObject obj, string key, float fallback)
    {
        if (obj.TryGetPropertyValue(key, out var n) && n is JsonValue v && v.TryGetValue<double>(out var d))
            return (float)d;
        return fallback;
    }

    private static bool ReadBool(JsonObject obj, string key, bool fallback)
    {
        if (obj.TryGetPropertyValue(key, out var n) && n is JsonValue v && v.TryGetValue<bool>(out var b))
            return b;
        return fallback;
    }

    private static string? ReadString(JsonObject obj, string key, string? fallback)
    {
        if (obj.TryGetPropertyValue(key, out var n) && n is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
            return s;
        return fallback;
    }

    private sealed class PresetDefinition
    {
        public float ScavCapMult;
        public float PmcCapMult;
        public float ScavStartMult;
        public float PmcStartMult;
        public string? EscortAmount;
        public bool GuaranteeMainBosses;
        public bool RoamingBosses;
        public bool RotateMainBosses;
        public bool RoamingGoonSquad;
    }
}
