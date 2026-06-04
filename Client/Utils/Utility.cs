using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using SPT.Reflection.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace acidphantasm_botplacementsystem.Utils
{
    internal class Utility
    {
        private static string _mapName = string.Empty;
        
        public static bool Initialized;
        
        // Spawn Points
        private static List<ISpawnPoint> _allSpawnPoints = new();
        public static List<ISpawnPoint> PlayerSpawnPoints = new();
        public static List<ISpawnPoint> BackupPlayerSpawnPoints = new();
        public static List<ISpawnPoint> CombinedSpawnPoints = new();
        public static Dictionary<string, BotZone> SpawnPointToZone = new(); // point ID → zone
        public static List<ISpawnPoint> AllBotSpawnPoints = new(); // flat list of all bot spawn points
        
        // Zones
        public static List<BotZone> CurrentMapZones = new();
        public static List<BotZone> CachedNonSnipeZones = new();
        
        // Bot Trackers
        public static readonly HashSet<Vector3> ReservedSpawnPositions = new();
        public static readonly HashSet<string> UsedSpawnPointIds = new(); // Track used spawn points
        public static readonly object SpawnPointLock = new object();
        public static List<Player> CachedPmcs = new();
        public static List<Player> CachedAssaultBots = new();
        public static List<Player> CachedBosses = new();
        public static List<Player> CachedConnectedPlayers = new();
        public static double BotsSpawnedPerPlayer = 0.0d;
        public static int PmcsSpawnedThisRaid = 0;
        public static float LastPmcSpawnAttemptTime = 0f;
        // BotStart/BotStop from current map's LocationSettings, captured by
        // NonWavesSpawnSystemPatch the first time it runs. Used by other patches
        // (PmcSpawnHookPatch curve gate) that don't have direct access to that data.
        public static float RaidBotStart = 0f;
        public static float RaidBotStop = 0f;
        
        // Player spawn position for distance-sorted spawning
        public static Vector3? InitialPlayerSpawnPosition = null;
        public static Vector3? CurrentPlayerPosition = null;
        public static Vector3? TravelDirection = null;
        // Monotonic counter bumped every time CurrentPlayerPosition or TravelDirection
        // is updated. Patches that cache sorted spawn lists key on this so they
        // re-sort only when the position snapshot changes.
        public static int PositionSnapshotVersion = 0;
        private static readonly List<Vector3> _positionHistory = new();
        private static float _lastPositionUpdateTime = 0f;
        private const float PositionUpdateInterval = 15f; // Sample position every 15 seconds
        private const int MaxPositionHistory = 6;
        private const float MinDirectionDistance = 10f; // Min movement to establish direction

        public static readonly Dictionary<string, string[]> MapHotSpots = new()
        {
            {"rezervbase", ["ZoneSubStorage", "ZoneBarrack"]},
            {"shoreline", ["ZoneSanatorium1", "ZoneSanatorium2"]},
            {"lighthouse", ["Zone_LongRoad", "Zone_Chalet", "Zone_Village"]},
            {"interchange", ["ZoneCenter", "ZoneCenterBot"]},
            {"bigmap", ["ZoneDormitory", "ZoneScavBase", "ZoneOldAZS", "ZoneGasStation"]}
        };

        public static Profile GetPlayerProfile()
        {
            return ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
        }

        public static string CurrentLocation
        {
            get
            {
                if (_mapName != string.Empty) return _mapName;

                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld != null)
                {
                    _mapName = gameWorld.LocationId;
                    return _mapName;
                }
                return "default";
            }
        }
        
        public static void InitializeSpawnPoints(BotZone[] allBotZones)
        {
            _mapName = string.Empty;
            
            _allSpawnPoints.Clear();
            PlayerSpawnPoints.Clear();
            BackupPlayerSpawnPoints.Clear();
            CombinedSpawnPoints.Clear();
            
            CachedNonSnipeZones.Clear();
            CurrentMapZones.Clear();
            
            ReservedSpawnPositions.Clear();
            UsedSpawnPointIds.Clear();
            CachedPmcs.Clear();
            CachedAssaultBots.Clear();
            CachedBosses.Clear();
            CachedConnectedPlayers.Clear();
            InitialPlayerSpawnPosition = null;
            CurrentPlayerPosition = null;
            TravelDirection = null;
            PositionSnapshotVersion = 0;
            _positionHistory.Clear();
            _lastPositionUpdateTime = 0f;
            
            SpawnPointToZone.Clear();
            AllBotSpawnPoints.Clear();
            
            BotsSpawnedPerPlayer = 0.0;
            PmcsSpawnedThisRaid = 0;
            LastPmcSpawnAttemptTime = 0f;
            RaidBotStart = 0f;
            RaidBotStop = 0f;
            
            // Recache spawn points now
            _allSpawnPoints = SpawnPointManagerClass.CreateFromScene().ToList();
    
            PlayerSpawnPoints = _allSpawnPoints
                .Where(x => x.Categories.ContainPlayerCategory() && x.Infiltration != null)
                .ToList();
        
            BackupPlayerSpawnPoints = _allSpawnPoints
                .Where(x => x.Categories.ContainBotCategory() 
                            && !x.Categories.ContainBossCategory() 
                            && !x.IsSnipeZone)
                .ToList();
        
            CombinedSpawnPoints = PlayerSpawnPoints
                .Concat(BackupPlayerSpawnPoints)
                .ToList();
            
            foreach (var botZone in allBotZones)
            {
                var zoneName = botZone.NameZone;
                foreach (var spawnPoint in botZone.SpawnPoints)
                {
                    if (spawnPoint.Categories != ESpawnCategoryMask.All && !spawnPoint.Categories.ContainBotCategory())
                    {
                        continue;
                    }
                    // Skip sniper zone points: they're far from typical play areas (rooftops,
                    // ridgelines, etc) and scavs spawning there feels "all over the map".
                    if (spawnPoint.IsSnipeZone)
                    {
                        continue;
                    }
                    AllBotSpawnPoints.Add(spawnPoint);
                    SpawnPointToZone[spawnPoint.Id] = botZone;
                }
            }
            
            Initialized = true;
        }
        
        public static BotZone GetNewValidBotZone()
        {
            var randomIndex = UnityEngine.Random.Range(0, CachedNonSnipeZones.Count);
            return CachedNonSnipeZones[randomIndex];
        }
        
        /// <summary>
        /// Scores a spawn point based on distance, direction of travel, and noise.
        /// Directional bias is applied only in a ~90° cone in front of the player
        /// (anything within 45° of forward). Points outside the front cone but not
        /// behind are neutral (no bonus, no penalty). Points behind the player are
        /// penalised by the full bias amount.
        /// Falls back to distance + noise when no travel direction is established.
        /// Points inside a hotzone receive a score multiplier (HotzoneScoreMultiplier)
        /// to favor them, restoring the hotzone preference that was lost when the
        /// flat global search replaced the zone-gated search.
        /// </summary>
        public static float GetDirectionalScore(Vector3 spawnPoint, Vector3 playerPos, float noiseAmount)
        {
            const float FrontConeCosThreshold = 0.707f; // cos(45°)
            var distance = Vector3.Distance(spawnPoint, playerPos);
            var noise = noiseAmount > 0f ? UnityEngine.Random.Range(0f, noiseAmount) : 0f;
            float score;

            if (!TravelDirection.HasValue || Plugin.DirectionalBias <= 0f)
            {
                score = distance + noise;
            }
            else
            {
                var offset = (spawnPoint - playerPos);
                if (offset.sqrMagnitude < 1f)
                {
                    score = noise; // On top of player
                }
                else
                {
                    var dot = Vector3.Dot(offset.normalized, TravelDirection.Value);
                    // Front cone (within 45° of forward): full bonus scaled by dot.
                    // Behind (dot < 0): full penalty scaled by -dot.
                    // Side (0 <= dot < 0.707): neutral, no adjustment.
                    float directionalMultiplier;
                    if (dot >= FrontConeCosThreshold)
                        directionalMultiplier = 1.0f - (dot * Plugin.DirectionalBias);
                    else if (dot < 0f)
                        directionalMultiplier = 1.0f - (dot * Plugin.DirectionalBias); // penalty: 1 + |dot|*bias
                    else
                        directionalMultiplier = 1.0f; // side neutral
                    score = distance * directionalMultiplier + noise;
                }
            }

            if (Plugin.EnableHotzones && IsInHotzone(spawnPoint))
                score *= Plugin.HotzoneScoreMultiplier;

            return score;
        }

        private static bool IsInHotzone(Vector3 spawnPoint)
        {
            var mapName = CurrentLocation?.ToLower();
            if (mapName == null || !MapHotSpots.TryGetValue(mapName, out var hotzoneNames))
                return false;

            foreach (var zone in CurrentMapZones)
            {
                if (zone == null) continue;
                if (!hotzoneNames.Contains(zone.NameZone)) continue;
                // Cheap check: spawn point is within the zone's bounding sphere center radius.
                var zoneCenter = zone.CenterOfSpawnPoints;
                var radius = zone.MaxPersons > 0 ? 80f : 50f;
                if (Vector3.Distance(spawnPoint, zoneCenter) <= radius)
                    return true;
            }
            return false;
        }

        public static bool IsPlayerHeadless(Player player)
        {
            return player.Profile.Info.MemberCategory == EMemberCategory.UnitTest;
        }

        public static bool IsPlayerHeadless(IPlayer player)
        {
            return player.Profile.Info.MemberCategory == EMemberCategory.UnitTest;
        }

        /// <summary>
        /// Per-map total PMC count for the raid. Single source of truth used by both
        /// PmcSpawnHookPatch (starting PMCs runtime cap), NonWavesSpawnSystemPatch
        /// (wave PMC tick), and TryToSpawnInZonePatch (deterministic index spacing).
        /// The returned value already includes the active preset's PMC cap multiplier.
        /// </summary>
        public static int GetMaxPmcsForMap(string mapName)
        {
            var baseline = mapName switch
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
            return ApplyPresetMult(baseline, Plugin.PresetPmcCapMult);
        }

        /// <summary>
        /// Per-map total scav count for the raid (1-player baseline; scaled at spawn
        /// time by PerPlayerScavMultiplier). The returned value already includes the
        /// active preset's scav cap multiplier.
        /// </summary>
        public static int GetMaxScavsForMap(string mapName)
        {
            var baseline = mapName switch
            {
                "bigmap" => Plugin.CustomsMaxScavs,
                "factory4_day" or "factory4_night" => Plugin.FactoryMaxScavs,
                "interchange" => Plugin.InterchangeMaxScavs,
                "laboratory" => Plugin.LabsMaxScavs,
                "lighthouse" => Plugin.LighthouseMaxScavs,
                "rezervbase" => Plugin.ReserveMaxScavs,
                "sandbox" => Plugin.GroundZeroMaxScavs,
                "sandbox_high" => Plugin.GroundZeroHighMaxScavs,
                "shoreline" => Plugin.ShorelineMaxScavs,
                "tarkovstreets" => Plugin.StreetsMaxScavs,
                "woods" => Plugin.WoodsMaxScavs,
                "labyrinth" => Plugin.LabyrinthMaxScavs,
                _ => 0,
            };
            return ApplyPresetMult(baseline, Plugin.PresetScavCapMult);
        }

        private static int ApplyPresetMult(int baseline, float mult)
        {
            if (baseline <= 0) return 0;
            if (mult <= 0f) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(baseline * mult));
        }
        
        /// <summary>
        /// Loosely shuffles the close half of a sorted list by swapping each Nth
        /// element in the FIRST half of the shuffle zone with a random element in
        /// the SECOND half of the same zone. Variety stays inside the close-distance
        /// band; the far half of the full list never bubbles to the front.
        /// </summary>
        public static void LooselyShuffle<T>(List<T> list, float shufflePercent, int shuffleStep)
        {
            if (list.Count < 4 || shufflePercent <= 0f || shuffleStep < 2) return;

            var shuffleZone = Mathf.Max(2, Mathf.FloorToInt(list.Count * shufflePercent));
            var halfZone = shuffleZone / 2;
            if (halfZone < 1) return;
            var secondHalfStart = halfZone;
            var secondHalfLen = shuffleZone - halfZone;

            for (var i = shuffleStep - 1; i < halfZone; i += shuffleStep)
            {
                var swapIndex = secondHalfStart + UnityEngine.Random.Range(0, secondHalfLen);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
        
        /// <summary>
        /// Periodically updates the reference position for spawn sorting based on
        /// the AVERAGED position of all alive connected players. Using the centroid
        /// instead of a single random player prevents TravelDirection from oscillating
        /// when players move in opposite directions.
        /// Call this from a frequently-running patch (e.g. NonWavesSpawnSystem.Update).
        /// </summary>
        public static void TryUpdatePlayerPosition()
        {
            if (Time.time - _lastPositionUpdateTime < PositionUpdateInterval) return;
            if (CachedConnectedPlayers.Count == 0) return;

            var center = Vector3.zero;
            var aliveCount = 0;
            foreach (var p in CachedConnectedPlayers)
            {
                if (p == null || p.HealthController?.IsAlive != true) continue;
                try { center += p.Position; aliveCount++; }
                catch { /* skip players whose position throws */ }
            }
            if (aliveCount == 0) return;

            var currentPos = center / aliveCount;

            // Update positions
            CurrentPlayerPosition = currentPos;
            _lastPositionUpdateTime = Time.time;
            PositionSnapshotVersion++;

            _positionHistory.Add(currentPos);
            if (_positionHistory.Count > MaxPositionHistory)
                _positionHistory.RemoveAt(0);

            // Travel direction is anchored at the initial spawn point: it's the vector
            // FROM where the squad spawned TO their current position. This stays stable
            // for the whole raid (no oscillation, no staleness once you stop) and lines
            // up with the natural play pattern of moving outward from spawn.
            if (InitialPlayerSpawnPosition.HasValue)
            {
                var direction = currentPos - InitialPlayerSpawnPosition.Value;
                if (direction.magnitude > MinDirectionDistance)
                {
                    TravelDirection = direction.normalized;
                    if (Plugin.DebugLogging)
                        Plugin.LogSource.LogInfo($"[ABPS] Travel direction (from spawn anchor): {TravelDirection.Value}, dist {direction.magnitude:0}m");
                }
            }
            
            // PMC spawn lists (PlayerSpawnPoints / BackupPlayerSpawnPoints / CombinedSpawnPoints)
            // are locked at raid start in PMCSpawning.cs and NEVER re-sorted here — PMCs use a
            // fixed plausible distribution from the player's spawn point.
            // CurrentPlayerPosition / TravelDirection are still updated above for the scav system
            // (TryToSpawnInZonePatch uses them for live directional scoring against AllBotSpawnPoints).

            if (Plugin.DebugLogging)
                Plugin.LogSource.LogInfo($"[ABPS] Updated spawn reference position to {currentPos}");
        }
    }
}
