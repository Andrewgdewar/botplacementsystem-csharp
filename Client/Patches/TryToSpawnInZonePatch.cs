using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using acidphantasm_botplacementsystem.Utils;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace acidphantasm_botplacementsystem.Patches
{
    internal class TryToSpawnInZonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), nameof(BotSpawner.TryToSpawnInZoneAndDelay));
        }

        [PatchPrefix]
        private static void PatchPrefix(BotSpawner __instance, ref BotZone botZone, BotCreationDataClass data, bool withCheckMinMax, bool newWave, ref List<ISpawnPoint> pointsToSpawn, bool forcedSpawn = false)
        {
            try
            {
                if (!data.IsValidSpawnType(WildSpawnType.assault) || pointsToSpawn != null) return;

                var mapName = Utility.CurrentLocation.ToLower();
                var pmcDistance = GetDistanceForMap(mapName);
                var isSmallMap = mapName.Contains("factory") || mapName.Contains("sandbox") ||
                                 mapName.Contains("labyrinth") || mapName.Contains("laboratory");
                var scavDistance = isSmallMap ? 20f : 40f;

                List<Player> pmcList;
                List<Player> scavList;
                lock (Utility.SpawnPointLock)
                {
                    pmcList = Utility.CachedPmcs.ToList();
                    scavList = Utility.CachedAssaultBots.ToList();
                }

                // Flat global search: score ALL spawn points, pick best valid one, assign zone
                var bestPoint = FindBestGlobalSpawnPoint(pmcList, pmcDistance, scavList, scavDistance);
                
                if (bestPoint != null)
                {
                    // Look up which zone this point belongs to and override
                    if (Utility.SpawnPointToZone.TryGetValue(bestPoint.Id, out var pointZone))
                    {
                        botZone = pointZone;
                    }
                    
                    pointsToSpawn = new List<ISpawnPoint> { bestPoint };
                }
                else
                {
                    if (Plugin.DebugLogging)
                        Plugin.LogSource.LogInfo($"{data.Id} - No valid spawn points found globally");
                    pointsToSpawn = null;
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"TryToSpawnInZonePatch EXCEPTION: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Searches ALL spawn points across ALL zones, scored by directional bias.
        /// Returns the best valid point, or null if none found.
        /// </summary>
        private static ISpawnPoint FindBestGlobalSpawnPoint(IReadOnlyCollection<Player> pmcList, float pmcDistance, IReadOnlyCollection<Player> scavList, float scavDistance)
        {
            var allPoints = Utility.AllBotSpawnPoints;
            if (allPoints.Count == 0) return null;

            // Score and sort all points directionally
            List<ISpawnPoint> sorted;
            if (Utility.CurrentPlayerPosition.HasValue)
            {
                var playerPos = Utility.CurrentPlayerPosition.Value;
                sorted = allPoints
                    .OrderBy(sp => Utility.GetDirectionalScore(sp.Position, playerPos, Plugin.ScavSpawnNoise))
                    .ToList();
            }
            else
            {
                sorted = allPoints
                    .OrderBy(_ => GClass856.Random(0f, 1f))
                    .ToList();
            }

            // Loosely shuffle the ahead portion for variety
            Utility.LooselyShuffle(sorted, Plugin.ShufflePercent, Plugin.ShuffleStep);

            foreach (var point in sorted)
            {
                if (!IsValid(point, pmcList, pmcDistance) || !IsValid(point, scavList, scavDistance))
                    continue;

                return point;
            }

            return null;
        }
        private static bool IsValid(ISpawnPoint spawnPoint, IReadOnlyCollection<Player> players, float distance)
        {
            if (spawnPoint?.Collider == null)
            {
                return false;
            }

            if (players == null || players.Count == 0)
            {
                return true;
            }
            
            foreach (var player in players)
            {
                if (player == null || Utility.IsPlayerHeadless(player))
                {
                    continue;
                }
                
                Vector3 playerPosition;
                try
                {
                    playerPosition = player.Position;
                }
                catch
                {
                    Plugin.LogSource.LogInfo($"Player Position is Null when checking Scav.IsValid()");
                    continue;
                }
                
                if (spawnPoint.Collider.Contains(playerPosition))
                {
                    return false;
                }
                if (Vector3.Distance(spawnPoint.Position, playerPosition) < distance)
                {
                    return false;
                }
            }
            return true;
        }
        private static float GetDistanceForMap(string mapName)
        {
            return mapName switch
            {
                "bigmap"                        => Plugin.CustomsScavSpawnDistanceCheck,
                "factory4_day" or "factory4_night" => Plugin.FactoryScavSpawnDistanceCheck,
                "interchange"                   => Plugin.InterchangeScavSpawnDistanceCheck,
                "laboratory"                    => Plugin.LabsScavSpawnDistanceCheck,
                "lighthouse"                    => Plugin.LighthouseScavSpawnDistanceCheck,
                "rezervbase"                    => Plugin.ReserveScavSpawnDistanceCheck,
                "sandbox" or "sandbox_high"     => Plugin.GroundZeroScavSpawnDistanceCheck,
                "shoreline"                     => Plugin.ShorelineScavSpawnDistanceCheck,
                "tarkovstreets"                 => Plugin.StreetsScavSpawnDistanceCheck,
                "woods"                         => Plugin.WoodsScavSpawnDistanceCheck,
                "labyrinth"                     => Plugin.LabyrinthScavSpawnDistanceCheck,
                _                               => 10f,
            };
        }
        
        private static bool DoesMapHaveHotzones(string mapName)
        {
            return Plugin.EnableHotzones && Utility.MapHotSpots.ContainsKey(mapName);
        }
        private static bool IsZoneHotzone(string mapName, string botZone)
        {
            return Utility.MapHotSpots[mapName].Contains(botZone);
        }
    }
}

