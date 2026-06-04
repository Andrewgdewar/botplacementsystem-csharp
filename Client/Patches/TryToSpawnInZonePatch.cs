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
        // snapshot version changes (every ~15s via Utility.TryUpdatePlayerPosition).
        // Avoids an O(n log n) sort over all bot spawn points on every scav tick.
        private static int _cachedScavSortVersion = -1;
        private static List<ISpawnPoint> _cachedScavSorted = new();
        // Cumulative weight array aligned with _cachedScavSorted[0..PickPoolSize].
        // Rebuilt only when the sort cache or PickBiasPower changes.
        private static double[] _cachedCumulativeWeights = Array.Empty<double>();
        private static int _cachedPoolSize = 0;
        private static float _cachedBiasPower = -1f;
        private const int PickPoolMax = 80;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), nameof(BotSpawner.TryToSpawnInZoneAndDelay));
        }

        [PatchPrefix]
        private static void PatchPrefix(BotSpawner __instance, ref BotZone botZone, BotCreationDataClass data, bool withCheckMinMax, bool newWave, ref List<ISpawnPoint> pointsToSpawn, bool forcedSpawn = false)
        {
            try
            {
                // Scavs only: PMC spawn point selection lives in PmcSpawnHookPatch
                // (BossSpawnerClass.method_2), which is the correct game-side entry
                // point for PMC waves shipped as BossLocationSpawn entries.
                if (!data.IsValidSpawnType(WildSpawnType.assault) || pointsToSpawn != null) return;

                var mapName = Utility.CurrentLocation.ToLower();
                var isSmallMap = mapName.Contains("factory") || mapName.Contains("sandbox") ||
                                 mapName.Contains("labyrinth") || mapName.Contains("laboratory");
                // Buffer to keep scavs from spawning right on top of a human player.
                // NOT the PMC-vs-PMC spacing from cfg (which is 100-150m and would
                // forbid every spawn point near the squad on a large map).
                var humanDistance = isSmallMap ? 25f : 50f;
                var scavDistance = isSmallMap ? 20f : 40f;

                List<Player> pmcList;
                List<Player> scavList;
                lock (Utility.SpawnPointLock)
                {
                    pmcList = Utility.CachedPmcs.ToList();
                    scavList = Utility.CachedAssaultBots.ToList();
                }

                var bestPoint = FindBestGlobalSpawnPoint(pmcList, humanDistance, scavList, scavDistance);

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
                        Plugin.LogSource.LogInfo($"{data.Id} - No valid scav spawn points found globally");
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
        /// Uses weighted random pick (power-of-index): index 0 most likely, fall-off
        /// controlled by Plugin.PickBiasPower. Returns the picked valid point, or
        /// null if no valid candidates remain in the pool.
        /// </summary>
        private static ISpawnPoint FindBestGlobalSpawnPoint(IReadOnlyCollection<Player> pmcList, float pmcDistance, IReadOnlyCollection<Player> scavList, float scavDistance)
        {
            var allPoints = Utility.AllBotSpawnPoints;
            if (allPoints.Count == 0) return null;

            // Wait for the first human player to load before letting scavs spawn.
            // Without a reference position we'd fall back to random-across-the-map,
            // which produces the "scavs splatter everywhere" feel at raid start.
            // Scav waves keep ticking, they just no-op until the player is cached.
            if (!Utility.CurrentPlayerPosition.HasValue)
            {
                if (Plugin.DebugLogging)
                    Plugin.LogSource.LogInfo("[ABPS] Scav spawn deferred: no player position yet");
                return null;
            }

            // Re-sort only when the player position snapshot changes.
            var currentVersion = Utility.PositionSnapshotVersion;
            if (currentVersion != _cachedScavSortVersion || _cachedScavSorted.Count != allPoints.Count)
            {
                var playerPos = Utility.CurrentPlayerPosition.Value;
                _cachedScavSorted = allPoints
                    .OrderBy(sp => Utility.GetDirectionalScore(sp.Position, playerPos, Plugin.ScavSpawnNoise))
                    .ToList();
                _cachedScavSortVersion = currentVersion;
                _cachedBiasPower = -1f; // force weight rebuild below
            }

            var poolSize = Math.Min(PickPoolMax, _cachedScavSorted.Count);
            if (poolSize <= 0) return null;

            // Rebuild cumulative weight table if sort changed or bias power changed.
            var biasPower = Plugin.PickBiasPower;
            if (poolSize != _cachedPoolSize || Math.Abs(biasPower - _cachedBiasPower) > 0.0001f)
            {
                if (_cachedCumulativeWeights.Length < poolSize)
                    _cachedCumulativeWeights = new double[poolSize];
                double acc = 0;
                for (var i = 0; i < poolSize; i++)
                {
                    acc += 1.0 / Math.Pow(i + 1, biasPower);
                    _cachedCumulativeWeights[i] = acc;
                }
                _cachedPoolSize = poolSize;
                _cachedBiasPower = biasPower;
            }

            // Try up to N weighted picks. Each pick rejects on UsedSpawnPointIds /
            // IsValid and re-rolls so we don't get stuck if the top candidate is
            // already taken or invalid.
            const int MaxPickAttempts = 12;
            var totalWeight = _cachedCumulativeWeights[poolSize - 1];
            for (var attempt = 0; attempt < MaxPickAttempts; attempt++)
            {
                var roll = UnityEngine.Random.value * totalWeight;
                var idx = BinarySearchCumulative(_cachedCumulativeWeights, poolSize, roll);
                var point = _cachedScavSorted[idx];
                if (Utility.UsedSpawnPointIds.Contains(point.Id)) continue;
                if (!IsValid(point, pmcList, pmcDistance) || !IsValid(point, scavList, scavDistance)) continue;
                Utility.UsedSpawnPointIds.Add(point.Id);
                return point;
            }

            // Final fallback: forward scan the pool for any valid point so a scav
            // never silently no-ops when there is a valid candidate available.
            for (var i = 0; i < poolSize; i++)
            {
                var point = _cachedScavSorted[i];
                if (Utility.UsedSpawnPointIds.Contains(point.Id)) continue;
                if (!IsValid(point, pmcList, pmcDistance) || !IsValid(point, scavList, scavDistance)) continue;
                Utility.UsedSpawnPointIds.Add(point.Id);
                return point;
            }

            return null;
        }

        private static int BinarySearchCumulative(double[] arr, int length, double target)
        {
            var lo = 0;
            var hi = length - 1;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (arr[mid] < target) lo = mid + 1;
                else hi = mid;
            }
            return lo;
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

