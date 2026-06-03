using System;
using acidphantasm_botplacementsystem.Spawning;
using acidphantasm_botplacementsystem.Utils;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace acidphantasm_botplacementsystem.Patches
{
    internal class PmcSpawnHookPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BossSpawnerClass), nameof(BossSpawnerClass.method_2));
        }

        [PatchPrefix]
        private static bool PatchPrefix(BossSpawnerClass __instance, BossLocationSpawn wave, BotSpawnParams spawnParams, BotDifficulty difficulty, int followersCount, BotCreationDataClass creationData, ref bool __result)
        {
            try
            {
                if (wave.BossType != WildSpawnType.pmcBEAR && wave.BossType != WildSpawnType.pmcUSEC)
                {
                    return true;
                }

                
                if (Plugin.DebugLogging)
                    Logger.LogInfo($"Spawn Point Attempt: {creationData.Profiles[0].Nickname} | WildSpawnType: {wave.BossType} | Count: {1 + wave.EscortCount}");

                var soloPointCount = 1;
                var escortPointCount = 1 + wave.EscortCount;
                var location = Utility.CurrentLocation ?? "default";
                location = location.ToLower();

                // Runtime hard cap: skip this wave if it would push us past the per-map total.
                // A group that would partially fit is rejected entirely so we don't overshoot.
                var maxPmcs = GetMaxPmcsForMap(location);
                if (maxPmcs > 0 && Utility.PmcsSpawnedThisRaid + escortPointCount > maxPmcs)
                {
                    Logger.LogInfo($"[ABPS] PMC cap reached for {location}: {Utility.PmcsSpawnedThisRaid}/{maxPmcs}, rejecting wave of {escortPointCount}");
                    __result = true;
                    return false;
                }

                var distance = GetDistanceForMap(location);
                var isSmallMap = location.Contains("factory4") || location.Contains("laboratory") ||
                                 location.Contains("labyrinth");
                var scavDistance = isSmallMap ? 20f : 50f;

                List<ISpawnPoint> validSpawnLocations;
                lock (Utility.SpawnPointLock)
                {
                    var pmcList = Utility.CachedPmcs.ToList();
                    var scavList = Utility.CachedAssaultBots.Concat(Utility.CachedBosses).ToList();

                    validSpawnLocations = GetValidSpawnPoints(pmcList, scavList, distance, scavDistance, escortPointCount);

                    if (validSpawnLocations.Count < escortPointCount && validSpawnLocations.Count > 0)
                    {
                        var neededSpawnPointCount = escortPointCount - validSpawnLocations.Count;
                        var spawnPoint = validSpawnLocations[0];
                        for (var i = 0; i < neededSpawnPointCount; i++)
                        {
                            validSpawnLocations.Add(spawnPoint);
                        }
                    }

                    if (validSpawnLocations.Count >= escortPointCount)
                    {
                        foreach (var point in validSpawnLocations)
                        {
                            Utility.ReservedSpawnPositions.Add(point.Position);
                        }
                    }
                }

                if (validSpawnLocations.Count >= soloPointCount)
                {
                    
                    if (Plugin.DebugLogging)
                        Logger.LogInfo($"ValidLocations: {validSpawnLocations.Count} needed: {escortPointCount} for {creationData.Profiles[0].Nickname}");

                    if (validSpawnLocations.Count >= escortPointCount)
                    {
                        var botZone =
                            __instance.BotSpawner_0.GetClosestZone(validSpawnLocations[0].Position, out float _);
                        __instance.Float_1 = Time.time;
                        __instance.WildSpawnType_0 = wave.BossType;
                        __instance.BotZone_1 = botZone;

                        if (creationData.SpawnStopped)
                        {
                            if (Plugin.DebugLogging)
                                Logger.LogInfo($"SpawnStopped before StartSpawnPMCGroup: {creationData.Profiles[0].Nickname}");
                            __result = false;
                            return false;
                        }

                        PmcGroupSpawner.StartSpawnPmcGroup(creationData, wave, spawnParams, followersCount, botZone,
                            validSpawnLocations).HandleExceptions();

                        Utility.PmcsSpawnedThisRaid += escortPointCount;
                        if (Plugin.DebugLogging)
                            Logger.LogInfo($"[ABPS] PMC spawned: {Utility.PmcsSpawnedThisRaid}/{maxPmcs} on {location}");

                        __result = true;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Plugin.DebugLogging)
                    Logger.LogError($"PatchPrefix EXCEPTION for {creationData?.Profiles?[0]?.Nickname ?? "unknown"}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                __result = true;
                return false;
            }

            if (Plugin.DebugLogging)
                Logger.LogInfo($"No valid spawnpoints found - skipping spawn: {creationData.Profiles[0].Nickname} | WildSpawnType: {wave.BossType} | Count: {1 + wave.EscortCount}");
            __result = true;
            return false;
        }

        private static List<ISpawnPoint> GetValidSpawnPoints(IReadOnlyCollection<Player> pmcPlayers, IReadOnlyCollection<Player> scavPlayers, float distance, float scavDistance, int neededPoints)
        {
            // maybe need to check Utility.CurrentLocation == "tarkovstreets" on this
            if (!Plugin.PmcSpawnAnywhere)
            {
                var validPlayerSpawnPoints = GetPlayerSpawnPoints(pmcPlayers, scavPlayers, distance, scavDistance, neededPoints);
                if (validPlayerSpawnPoints.Count >= neededPoints)
                {
                    return validPlayerSpawnPoints;
                }

                if (Plugin.DebugLogging)
                    Plugin.LogSource.LogInfo($"Falling back to any spawn points, couldn't get enough points");
                
                var fallbackSpawnPoints = GetAnySpawnPoints(pmcPlayers, scavPlayers, distance * 0.75f, scavDistance * 0.75f, neededPoints, true);
                return fallbackSpawnPoints;
            }

            var anywhereSpawnPoints = GetAnySpawnPoints(pmcPlayers, scavPlayers, distance, scavDistance, neededPoints);
            return anywhereSpawnPoints;
        }

        private static List<ISpawnPoint> GetPlayerSpawnPoints(IReadOnlyCollection<Player> pmcPlayers, IReadOnlyCollection<Player> scavPlayers, float distance, float scavDistance, int neededPoints)
        {
            // PMC lists are sorted ONCE at raid start (PMCSpawning.cs) with fuzzy PMC noise.
            // Skip the closest N%, then deterministically index into the remaining list based
            // on how many PMCs have spawned so far / wave budget, so each successive PMC picks
            // a point further into the sorted list (spread across the whole sorted range).
            var list = SortAndSkipClosestForPmc(Utility.PlayerSpawnPoints);
            return PickFromDeterministicIndex(list, pmcPlayers, scavPlayers, distance, scavDistance, neededPoints);
        }

        private static List<ISpawnPoint> SortAndSkipClosestForPmc(List<ISpawnPoint> source)
        {
            if (source == null || source.Count == 0) return new List<ISpawnPoint>();

            // PMC lists are sorted ONCE at raid start (PMCSpawning.cs) with fuzzy PMC noise.
            // Don't re-sort here — just skip the closest N% of the locked order so PMCs
            // never spawn in the area immediately around the player's spawn point.
            var skipCount = (int)System.Math.Floor(source.Count * Plugin.PmcSkipClosestPercent);
            if (skipCount <= 0 || skipCount >= source.Count) return new List<ISpawnPoint>(source);

            return source.GetRange(skipCount, source.Count - skipCount);
        }

        private static List<ISpawnPoint> GetAnySpawnPoints(IReadOnlyCollection<Player> pmcPlayers, IReadOnlyCollection<Player> scavPlayers, float distance, float scavDistance, int neededPoints, bool backupToPlayer = false)
        {
            // Same deterministic index approach as GetPlayerSpawnPoints, but on the fallback list.
            var sourceList = backupToPlayer ? Utility.BackupPlayerSpawnPoints : Utility.CombinedSpawnPoints;
            var alternativeList = SortAndSkipClosestForPmc(sourceList);
            return PickFromDeterministicIndex(alternativeList, pmcPlayers, scavPlayers, distance, scavDistance, neededPoints);
        }

        /// <summary>
        /// Picks a target slot in the (already-sorted, already-skipped) list based on
        /// how many PMCs have spawned so far. Starts the search at that slot to spread
        /// successive PMCs across the entire sorted distance range. Once a valid initial
        /// point is found, cluster group members within 20m of it.
        /// </summary>
        private static List<ISpawnPoint> PickFromDeterministicIndex(
            List<ISpawnPoint> list,
            IReadOnlyCollection<Player> pmcPlayers,
            IReadOnlyCollection<Player> scavPlayers,
            float distance,
            float scavDistance,
            int neededPoints)
        {
            var validSpawnPoints = new List<ISpawnPoint>();
            if (list.Count == 0) return validSpawnPoints;

            // Index spaces successive PMC spawns across the sorted list:
            // alreadySpawned / maxPmcs -> fractional position, multiplied by list size.
            // If maxPmcs is unknown (0), fall back to starting at index 0 (acid's behaviour).
            var mapName = (Utility.CurrentLocation ?? "default").ToLower();
            var maxPmcs = GetMaxPmcsForMap(mapName);
            var startIndex = 0;
            if (maxPmcs > 0)
            {
                var targetFraction = System.Math.Min(0.999f, (float)Utility.PmcsSpawnedThisRaid / maxPmcs);
                startIndex = System.Math.Min(list.Count - 1, (int)System.Math.Floor(list.Count * targetFraction));

                if (Plugin.DebugLogging)
                    Plugin.LogSource.LogInfo($"[ABPS] PMC index pick: spawned={Utility.PmcsSpawnedThisRaid}/{maxPmcs} fraction={targetFraction:0.00} startIdx={startIndex}/{list.Count}");
            }

            // Forward search from startIndex for the first valid point.
            ISpawnPoint firstPoint = null;
            for (var i = startIndex; i < list.Count; i++)
            {
                var checkPoint = list[i];
                if (!IsValid(checkPoint, pmcPlayers, distance) || !IsValid(checkPoint, scavPlayers, scavDistance))
                    continue;
                firstPoint = checkPoint;
                validSpawnPoints.Add(checkPoint);
                break;
            }

            // If nothing valid from startIndex onward, wrap back to beginning of usable list.
            if (firstPoint == null)
            {
                for (var i = 0; i < startIndex; i++)
                {
                    var checkPoint = list[i];
                    if (!IsValid(checkPoint, pmcPlayers, distance) || !IsValid(checkPoint, scavPlayers, scavDistance))
                        continue;
                    firstPoint = checkPoint;
                    validSpawnPoints.Add(checkPoint);
                    break;
                }
            }

            if (firstPoint == null) return validSpawnPoints;

            // Cluster group members within 20m of the picked point.
            foreach (var checkPoint in list)
            {
                if (validSpawnPoints.Count >= neededPoints) break;
                if (checkPoint == firstPoint) continue;
                if (Vector3.Distance(checkPoint.Position, firstPoint.Position) <= 20f)
                    validSpawnPoints.Add(checkPoint);
            }

            return validSpawnPoints;
        }

        private static bool IsValid(ISpawnPoint spawnPoint, IReadOnlyCollection<Player> players, float distance)
        {
            if (spawnPoint == null)
            {
                return false;
            }

            if (spawnPoint.Collider == null)
            {
                return false;
            }
            
            foreach (var reservedPos in Utility.ReservedSpawnPositions)
            {
                if (Vector3.Distance(spawnPoint.Position, reservedPos) < distance)
                {
                    return false;
                }
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
                    Plugin.LogSource.LogInfo($"Player Position is Null when checking Pmc.IsValid()");
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
                "bigmap" => Plugin.CustomsPmcSpawnDistanceCheck,
                "factory4_day" or "factory4_night" => Plugin.FactoryPmcSpawnDistanceCheck,
                "interchange" => Plugin.InterchangePmcSpawnDistanceCheck,
                "laboratory" => Plugin.LabsPmcSpawnDistanceCheck,
                "lighthouse" => Plugin.LighthousePmcSpawnDistanceCheck,
                "rezervbase" => Plugin.ReservePmcSpawnDistanceCheck,
                "sandbox" or "sandbox_high" => Plugin.GroundZeroPmcSpawnDistanceCheck,
                "shoreline" => Plugin.ShorelinePmcSpawnDistanceCheck,
                "tarkovstreets" => Plugin.StreetsPmcSpawnDistanceCheck,
                "woods" => Plugin.WoodsPmcSpawnDistanceCheck,
                "labyrinth" => Plugin.LabyrinthPmcSpawnDistanceCheck,
                _ => 50f,
            };
        }

        private static int GetMaxPmcsForMap(string mapName)
        {
            return mapName switch
            {
                "bigmap" => Plugin.CustomsMaxPmcs,
                "factory4_day" or "factory4_night" => Plugin.FactoryMaxPmcs,
                "interchange" => Plugin.InterchangeMaxPmcs,
                "laboratory" => Plugin.LabsMaxPmcs,
                "lighthouse" => Plugin.LighthouseMaxPmcs,
                "rezervbase" => Plugin.ReserveMaxPmcs,
                "sandbox" => Plugin.GroundZeroMaxPmcs,
                "sandbox_high" => Plugin.GroundZeroHighMaxPmcs,
                "shoreline" => Plugin.ShorelineMaxPmcs,
                "tarkovstreets" => Plugin.StreetsMaxPmcs,
                "woods" => Plugin.WoodsMaxPmcs,
                "labyrinth" => Plugin.LabyrinthMaxPmcs,
                _ => 0,
            };
        }
    }
}
