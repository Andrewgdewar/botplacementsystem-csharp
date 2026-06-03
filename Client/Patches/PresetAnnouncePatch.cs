using acidphantasm_botplacementsystem.Spawning;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace acidphantasm_botplacementsystem.Patches
{
    /// <summary>
    /// Announces the active ABPS preset when a raid starts, mirroring MOAR's
    /// NotificationPatch. Gated by the "Announce Preset On Raid Start" F12 setting.
    /// </summary>
    internal class PresetAnnouncePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        private static async void PatchPostfix()
        {
            if (!Plugin.ShowPresetOnRaidStart)
            {
                return;
            }

            await PresetAnnouncer.Announce();
        }
    }
}
