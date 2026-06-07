using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace acidphantasm_botplacementsystem.Spawning
{
    /// <summary>
    /// Fetches the active preset (and its cap multipliers) from the server and
    /// shows an in-game notification. Triggered on raid start (postfix on
    /// GameWorld.OnGameStarted) and via the user-configurable hotkey.
    ///
    /// Updates Plugin.PresetScavCapMult / PresetPmcCapMult so the rest of the
    /// client picks up the active preset's cap multipliers (used by
    /// Utility.GetMaxPmcsForMap / GetMaxScavsForMap).
    /// </summary>
    public static class PresetAnnouncer
    {
        private static readonly Random _rng = new();

        // MOAR-style flavour suffixes appended to the announcement line.
        private static readonly List<string> _suffixes =
        [
            ", good luck out there.",
            ", may the bots be with you.",
            ", probably scuffed.",
            ", may your raids be bug-free.",
            ", enjoy the dumpster fire.",
            ", hope you brought enough mags.",
            ", you'll need it.",
            ", enjoy the carnage.",
            ", try not to rage-quit.",
            ", don't say I didn't warn you.",
            ", everything will be fine.",
            ", spare a thought for the scavs.",
            ", let's see how this goes.",
            ", roll the dice.",
            ", tonight's flavour."
        ];

        public static void Announce()
        {
            try
            {
                var raw = RequestHandler.GetJson("/botplacementsystem/preset") ?? string.Empty;
                var label = "live-like";

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        var resp = JsonConvert.DeserializeObject<PresetResponse>(raw);
                        if (resp != null && !string.IsNullOrEmpty(resp.Label))
                            label = resp.Label;
                        if (resp != null)
                        {
                            Plugin.PresetScavCapMult = SanitizeMult(resp.ScavCapMult);
                            Plugin.PresetPmcCapMult = SanitizeMult(resp.PmcCapMult);
                        }
                    }
                    catch
                    {
                        // Older server might still return a bare label string; fall back gracefully.
                        label = raw.Trim().Trim('"');
                        if (string.IsNullOrEmpty(label)) label = "live-like";
                    }
                }

                var suffix = _suffixes[_rng.Next(_suffixes.Count)];
                NotificationManagerClass.DisplayMessageNotification($"[ABPS] Preset: {label}{suffix}");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("PresetAnnouncer failed: " + ex);
            }
        }

        private static float SanitizeMult(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v) || v < 0f) return 1f;
            return v;
        }

        private sealed class PresetResponse
        {
            [JsonProperty("label")] public string Label { get; set; } = string.Empty;
            [JsonProperty("scavCapMult")] public float ScavCapMult { get; set; } = 1f;
            [JsonProperty("pmcCapMult")] public float PmcCapMult { get; set; } = 1f;
        }
    }
}
