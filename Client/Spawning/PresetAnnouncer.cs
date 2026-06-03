using EFT.Communications;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace acidphantasm_botplacementsystem.Spawning
{
    /// <summary>
    /// MOAR-style "announce on raid start". Asks the server which preset is active
    /// for the current raid and shows it as an in-game notification with a random
    /// (mostly tongue-in-cheek) suffix.
    /// </summary>
    public static class PresetAnnouncer
    {
        private static readonly List<string> Suffixes = new List<string>
        {
            ", good luck!",
            ", may the bots ever be in your favour.",
            ", you're probably screwed.",
            ", may your raids be bug-free.",
            ", enjoy the dumpster fire.",
            ", hope you brought snacks.",
            ", good luck, seriously.",
            ", prepare to be crushed.",
            ", you're about to get wrecked.",
            ", enjoy the show.",
            ", good luck, you'll need it.",
            ", enjoy the carnage.",
            ", try not to rage-quit.",
            ", don't say I didn't warn you.",
            ", best of luck surviving that.",
            ", it's going to be a long day for you.",
            ", be water my friend.",
            ", let the feelings of dread pass over you.",
            ", it's about to get ugly. Enjoy."
        };

        private static readonly Random Rng = new Random();

        public static async Task Announce()
        {
            try
            {
                var presetName = await GetActivePresetName();

                // Empty means the preset system is disabled server-side; stay quiet.
                if (string.IsNullOrEmpty(presetName))
                {
                    return;
                }

                var suffix = Suffixes[Rng.Next(Suffixes.Count)];
                NotificationManagerClass.DisplayMessageNotification(
                    "Current preset is " + presetName + suffix,
                    ENotificationDurationType.Long,
                    ENotificationIconType.EntryPoint);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to announce ABPS preset: " + ex);
            }
        }

        private static async Task<string> GetActivePresetName()
        {
            var payload = await RequestHandler.GetJsonAsync("/botplacementsystem/announcePreset");
            if (string.IsNullOrEmpty(payload))
            {
                return string.Empty;
            }

            // The server returns the display name serialized as a JSON string ("More Scavs").
            return JsonConvert.DeserializeObject<string>(payload);
        }
    }
}

