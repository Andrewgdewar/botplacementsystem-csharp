using acidphantasm_botplacementsystem.Spawning;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace acidphantasm_botplacementsystem.Patches
{
    /// <summary>
    /// On raid start, fetches the active ABPS preset from the server and shows
    /// it as an in-game notification. Toggleable via Plugin.AnnouncePresetOnRaidStart.
    /// </summary>
    internal class PresetAnnouncePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            if (!Plugin.AnnouncePresetOnRaidStart) return;
            PresetAnnouncer.Announce();
        }
    }
}
