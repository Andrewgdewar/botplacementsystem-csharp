using System.Reflection;
using System.Text.Json.Nodes;
using _botplacementsystem.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _botplacementsystem.Globals;

/// <summary>
/// MOAR-style preset system. A preset is a named, partial override of the ABPS
/// config (see Presets/Presets.json). When presets are enabled the active preset
/// is layered on top of the authored config.json each time the spawn caches are
/// rebuilt (server start / raid end / save), exactly like MOAR re-rolls on raid end.
///
/// "random" performs a weighted roll using Presets/PresetWeightings.json; any other
/// value forces that specific preset. "live-like" intentionally applies no overrides.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class PresetManager
{
    private readonly JsonUtil _jsonUtil;
    private readonly WeightedRandomHelper _weightedRandomHelper;
    private readonly ISptLogger<PresetManager> _logger;
    private readonly string _presetPath;

    // Raw override JSON per preset, kept as a string so we can re-parse a fresh
    // JsonNode tree on every merge (a JsonNode can only have one parent).
    private readonly Dictionary<string, string> _presets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> _weightings = new(StringComparer.OrdinalIgnoreCase);

    private bool _loaded;
    private readonly object _loadLock = new();

    public const string OffLabel = "off";
    public const string RandomLabel = "random";

    /// <summary>The kebab-case label of the preset applied to the most recent rebuild ("off" when disabled).</summary>
    public string CurrentPresetLabel { get; private set; } = OffLabel;

    /// <summary>Human readable name of the active preset, or empty when presets are disabled.</summary>
    public string CurrentPresetName =>
        CurrentPresetLabel == OffLabel ? string.Empty : KebabToTitle(CurrentPresetLabel);

    public PresetManager(
        JsonUtil jsonUtil,
        WeightedRandomHelper weightedRandomHelper,
        ModHelper modHelper,
        ISptLogger<PresetManager> logger)
    {
        _jsonUtil = jsonUtil;
        _weightedRandomHelper = weightedRandomHelper;
        _logger = logger;
        var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _presetPath = Path.Combine(modPath, "Presets");
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        lock (_loadLock)
        {
            if (_loaded) return;

            try
            {
                LoadPresets();
                LoadWeightings();
                Validate();
            }
            catch (Exception ex)
            {
                _logger.Error($"[ABPS] Failed to load presets: {ex.Message}");
            }

            _loaded = true;
        }
    }

    private void LoadPresets()
    {
        var file = Path.Combine(_presetPath, "Presets.json");
        if (!File.Exists(file))
        {
            _logger.Warning($"[ABPS] Presets.json not found at '{file}', preset system disabled.");
            return;
        }

        var node = JsonNode.Parse(File.ReadAllText(file))?.AsObject();
        if (node is null) return;

        foreach (var kvp in node)
        {
            if (kvp.Value is null) continue;
            _presets[kvp.Key] = kvp.Value.ToJsonString();
        }
    }

    private void LoadWeightings()
    {
        var file = Path.Combine(_presetPath, "PresetWeightings.json");
        if (!File.Exists(file))
        {
            _logger.Warning($"[ABPS] PresetWeightings.json not found at '{file}'.");
            return;
        }

        var node = JsonNode.Parse(File.ReadAllText(file))?.AsObject();
        if (node is null) return;

        foreach (var kvp in node)
        {
            if (kvp.Value is null) continue;
            _weightings[kvp.Key] = kvp.Value.GetValue<double>();
        }
    }

    // Mirror of MOAR's checkPresetLogic: every weighting must reference a real preset.
    private void Validate()
    {
        foreach (var key in _weightings.Keys)
        {
            if (!_presets.ContainsKey(key))
            {
                _logger.Error($"[ABPS] PresetWeightings.json references preset '{key}' which is missing from Presets.json");
            }
        }
    }

    /// <summary>
    /// Returns the config the spawn caches should be built from. When presets are
    /// disabled (or the rolled preset has no overrides) the original config is
    /// returned unchanged. Otherwise a deep-merged clone is produced.
    /// </summary>
    public AbpsConfig GetEffectiveConfig(AbpsConfig baseConfig)
    {
        EnsureLoaded();

        var presetSettings = baseConfig.PresetConfig;
        if (presetSettings is null || !presetSettings.Enable)
        {
            CurrentPresetLabel = OffLabel;
            return baseConfig;
        }

        var label = ResolveLabel(presetSettings.ForcedPreset);
        CurrentPresetLabel = label;

        if (!_presets.TryGetValue(label, out var overrideJson))
        {
            return baseConfig;
        }

        var overrideNode = JsonNode.Parse(overrideJson)?.AsObject();
        if (overrideNode is null || overrideNode.Count == 0)
        {
            // e.g. "live-like" == {} -> run on pure config.json values.
            return baseConfig;
        }

        try
        {
            var baseNode = JsonNode.Parse(_jsonUtil.Serialize(baseConfig))?.AsObject();
            if (baseNode is null) return baseConfig;

            Merge(baseNode, overrideNode);

            var merged = _jsonUtil.Deserialize<AbpsConfig>(baseNode.ToJsonString());
            if (merged is null) return baseConfig;

            _logger.Info($"[ABPS] Applied preset '{label}'");
            return merged;
        }
        catch (Exception ex)
        {
            _logger.Error($"[ABPS] Failed to apply preset '{label}': {ex.Message}");
            return baseConfig;
        }
    }

    private string ResolveLabel(string? forced)
    {
        var value = (forced ?? RandomLabel).Trim();

        if (value.Equals(RandomLabel, StringComparison.OrdinalIgnoreCase) || value.Length == 0)
        {
            return RollWeightedPreset();
        }

        if (_presets.ContainsKey(value))
        {
            return value;
        }

        _logger.Warning($"[ABPS] Unknown forced preset '{value}', falling back to a random preset.");
        return RollWeightedPreset();
    }

    private string RollWeightedPreset()
    {
        if (_weightings.Count == 0)
        {
            return _presets.Keys.FirstOrDefault() ?? "live-like";
        }

        return _weightedRandomHelper.GetWeightedValue<string>(_weightings);
    }

    /// <summary>Recursively merges <paramref name="overrideNode"/> into <paramref name="target"/>.</summary>
    private static void Merge(JsonObject target, JsonObject overrideNode)
    {
        foreach (var kvp in overrideNode)
        {
            if (kvp.Value is JsonObject overrideChild &&
                target[kvp.Key] is JsonObject targetChild)
            {
                Merge(targetChild, overrideChild);
            }
            else
            {
                target[kvp.Key] = kvp.Value?.DeepClone();
            }
        }
    }

    /// <summary>Returns the selectable presets for the config UI (label + display name).</summary>
    public IReadOnlyList<PresetOption> GetPresetOptions()
    {
        EnsureLoaded();
        return _presets.Keys
            .Select(label => new PresetOption(label, KebabToTitle(label)))
            .ToList();
    }

    // "more-scavs-and-pmcs" -> "More Scavs And Pmcs"
    public static string KebabToTitle(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return string.Join(" ", value
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }
}

public record PresetOption(string Label, string Name);
