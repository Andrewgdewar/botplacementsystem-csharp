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
        // Cached sorted scav spawn list. Re-sorts only when the player position
        // snapshot version changes (every ~120s via Utility.TryUpdatePlayerPosition).
        // Avoids an O(n log n) sort over all bot spawn points on every scav tick.
        private static int _cachedScavSortVersion = -1;
        private static List<ISpawnPoint> _cachedScavSorted = new();

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), nameof(BotSpawner.TryToSpawnInZoneAndDelay));
        }

        [PatchPrefix]
        private static void PatchPrefix(BotSpawner __instance, ref BotZone botZone, BotCreationDataClass data, bool withCheckMinMax, bool newWave, ref List<ISpawnPoint> pointsToSpawn, bool forcedSpawn = false)
        {
            try
            {
                if (pointsToSpawn != null) return;

                var isScav = data.IsValidSpawnType(WildSpawnType.assault);
                var isPmc = data.IsValidSpawnType(WildSpawnType.pmcUSEC) || data.IsValidSpawnType(WildSpawnType.pmcBEAR);
                if (!isScav && !isPmc) return;

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

                ISpawnPoint bestPoint = isPmc
                    ? FindBestPmcSpawnPoint(pmcList, pmcDistance, scavList, scavDistance)
                    : FindBestGlobalSpawnPoint(pmcList, pmcDistance, scavList, scavDistance);

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
                        Plugin.LogSource.LogInfo($"{data.Id} - No valid spawn points found ({(isPmc ? "PMC" : "scav")})");
                    pointsToSpawn = null;
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"TryToSpawnInZonePatch EXCEPTION: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// PMC spawn point selection. Uses Utility.PlayerSpawnPoints (locked at raid start
        /// with PmcSpawnNoise) and picks a slot deterministically based on
        /// PmcsSpawnedThisRaid / maxPmcs so PMCs spread across the sorted list.
        /// </summary>
        private static ISpawnPoint FindBestPmcSpawnPoint(IReadOnlyCollection<Player> pmcList, float pmcDistance, IReadOnlyCollection<Player> scavList, float scavDistance)
        {
            var source = Utility.PlayerSpawnPoints;
            if (source == null || source.Count == 0)
            {
                if (Plugin.DebugLogging)
                    Plugin.LogSource.LogInfo($"[ABPS PMC pick] FAIL: PlayerSpawnPoints empty (source null? {source == null})");
                return null;
            }

            // Skip closest N% (PMC), don't re-sort (list is locked from raid start).
            var skipCount = (int)Math.Floor(source.Count * Plugin.PmcSkipClosestPercent);
            var list = (skipCount > 0 && skipCount < source.Count)
                ? source.GetRange(skipCount, source.Count - skipCount)
                : new List<ISpawnPoint>(source);

            var mapName = (Utility.CurrentLocation ?? "default").ToLower();
            var maxPmcs = Utility.GetMaxPmcsForMap(mapName);
            var startIndex = 0;
            if (maxPmcs > 0)
            {
                var targetFraction = Math.Min(0.999f, (float)Utility.PmcsSpawnedThisRaid / maxPmcs);
                startIndex = Math.Min(list.Count - 1, (int)Math.Floor(list.Count * targetFraction));
            }

            var invalidCount = 0;
            var usedCount = 0;

            // Forward search from startIndex.
            for (var i = startIndex; i < list.Count; i++)
            {
                var p = list[i];
                if (Utility.UsedSpawnPointIds.Contains(p.Id)) { usedCount++; continue; }
                if (!IsValid(p, pmcList, pmcDistance) || !IsValid(p, scavList, scavDistance)) { invalidCount++; continue; }
                Utility.UsedSpawnPointIds.Add(p.Id);
                if (Plugin.DebugLogging)
                    Plugin.LogSource.LogInfo($"[ABPS PMC pick] OK: idx {i}/{list.Count} (start {startIndex}), skipped {usedCount} used + {invalidCount} invalid");
                return p;
            }
            // Wrap to beginning of usable list.
            for (var i = 0; i < startIndex; i++)
            {
                var p = list[i];
                if (Utility.UsedSpawnPointIds.Contains(p.Id)) { usedCount++; continue; }
                if (!IsValid(p, pmcList, pmcDistance) || !IsValid(p, scavList, scavDistance)) { invalidCount++; continue; }
                Utility.UsedSpawnPointIds.Add(p.Id);
                if (Plugin.DebugLogging)
                    Plugin.LogSource.LogInfo($"[ABPS PMC pick] OK (wrap): idx {i}/{list.Count} (start was {startIndex}), skipped {usedCount} used + {invalidCount} invalid");
                return p;
            }
            if (Plugin.DebugLogging)
                Plugin.LogSource.LogInfo($"[ABPS PMC pick] FAIL: no valid point | list={list.Count} (source {source.Count} -skip {skipCount}), pmcDistance={pmcDistance}, scavDistance={scavDistance}, used={usedCount}, invalid={invalidCount}, pmcs={pmcList?.Count ?? 0}, scavs={scavList?.Count ?? 0}");
            return null;
        }

        /// <summary>
        /// Searches ALL spawn points across ALL zones, scored by directional bias.
        /// Returns the best valid point, or null if none found.
        /// </summary>
        private static ISpawnPoint FindBestGlobalSpawnPoint(IReadOnlyCollection<Player> pmcList, float pmcDistance, IReadOnlyCollection<Player> scavList, float scavDistance)
        {
            var allPoints = Utility.AllBotSpawnPoints;
            if (allPoints.Count == 0) return null;

            // Re-sort only when the player position snapshot changes. Between updates
            // (typically 120s apart) the scoring is stable, so we reuse the cached order.
            var currentVersion = Utility.PositionSnapshotVersion;
            if (currentVersion != _cachedScavSortVersion || _cachedScavSorted.Count != allPoints.Count)
            {
                if (Utility.CurrentPlayerPosition.HasValue)
                {
                    var playerPos = Utility.CurrentPlayerPosition.Value;
                    _cachedScavSorted = allPoints
                        .OrderBy(sp => Utility.GetDirectionalScore(sp.Position, playerPos, Plugin.ScavSpawnNoise))
                        .ToList();
                }
                else
                {
                    _cachedScavSorted = allPoints
                        .OrderBy(_ => GClass856.Random(0f, 1f))
                        .ToList();
                }
                _cachedScavSortVersion = currentVersion;
            }

            // Work on a copy so the loose shuffle does not mutate the cached order.
            var sorted = new List<ISpawnPoint>(_cachedScavSorted);

            // Loosely shuffle the ahead portion for variety
            Utility.LooselyShuffle(sorted, Plugin.ShufflePercent, Plugin.ShuffleStep);

            // Skip the closest N% of points so scavs don't spawn right on top of the player.
            var skipCount = (int)System.Math.Floor(sorted.Count * Plugin.ScavSkipClosestPercent);
            var startIndex = skipCount > 0 && skipCount < sorted.Count ? skipCount : 0;

            for (var i = startIndex; i < sorted.Count; i++)
            {
                var point = sorted[i];
                if (Utility.UsedSpawnPointIds.Contains(point.Id))
                    continue;
                if (!IsValid(point, pmcList, pmcDistance) || !IsValid(point, scavList, scavDistance))
                    continue;

                Utility.UsedSpawnPointIds.Add(point.Id);
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

