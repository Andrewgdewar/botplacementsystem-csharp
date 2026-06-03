using acidphantasm_botplacementsystem.Utils;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using Systems.Effects;
using UnityEngine;
using Object = System.Object;

namespace acidphantasm_botplacementsystem.Patches
{
    internal class NonWavesSpawnSystemPatch : ModulePatch
    {
        private static float _nextDespawnCheckTime = 0f;
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NonWavesSpawnScenario), nameof(NonWavesSpawnScenario.Update));
        }

        [PatchPrefix]
        private static bool PatchPrefix(
            NonWavesSpawnScenario __instance,
            ref BotsController ___botsController_0,
            ref AbstractGame ___abstractGame_0,
            ref LocationSettingsClass.Location ___location_0,
            ref GClass1881<BotDifficulty> ___gclass1881_0,
            ref GClass1881<WildSpawnType> ___gclass1881_1,
            ref GClass1876 ___gclass1876_0,
            ref bool ___bool_0,
            ref bool ___bool_1,
            ref bool ___bool_2,
            ref float ___nullable_0,
            ref float ___float_2,
            ref float ___float_0)
        {
            ref var isActive = ref ___bool_1;
            ref var isAtBotCap = ref ___bool_0;
            ref var isInSpawnWindow = ref ___bool_2;
            ref var nextWindowToggleTime = ref ___nullable_0;
            ref var spawnAttemptInterval = ref ___float_2;
            ref var lastSpawnAttemptTime = ref ___float_0;
            ref GClass1881<BotDifficulty> difficultyWeights = ref ___gclass1881_0;

            if (__instance == null || !isActive) return true;

            // Periodically update spawn reference position as player moves
            Utility.TryUpdatePlayerPosition();

            if (___abstractGame_0.PastTime < (float)___location_0.BotStart || ___abstractGame_0.PastTime > (float)___location_0.BotStop)
                return false;

            // PMC tick runs independently of the scav active/inactive cycle so PMC pacing
            // isn't affected by scav quiet windows. Has its own attempt interval + gates.
            TryToSpawnPmc(___botsController_0, ___abstractGame_0, ___location_0);

            if (nextWindowToggleTime.Equals(null) || nextWindowToggleTime <= ___abstractGame_0.PastTime)
            {
                isInSpawnWindow = !isInSpawnWindow;
                nextWindowToggleTime = ___abstractGame_0.PastTime + (isInSpawnWindow
                    ? GClass856.Random((float)___location_0.BotSpawnTimeOnMin, (float)___location_0.BotSpawnTimeOnMax)
                    : GClass856.Random((float)___location_0.BotSpawnTimeOffMin, (float)___location_0.BotSpawnTimeOffMax));
            }

            if (!isInSpawnWindow)
            {
                return false;
            }

            if (___abstractGame_0.PastTime - lastSpawnAttemptTime < spawnAttemptInterval)
            {
                return false;
            }
            lastSpawnAttemptTime = ___abstractGame_0.PastTime;

            var freeSlots = (___botsController_0.MaxCount - Plugin.SoftCap) - ___botsController_0.AliveLoadingDelayedBotsCount;

            if (isAtBotCap)
            {
                if (freeSlots < ___location_0.BotSpawnCountStep)
                {
                    return false;
                }
                spawnAttemptInterval = 15f;
                isAtBotCap = false;
            }
            else if (freeSlots <= 0)
            {
                spawnAttemptInterval = Math.Max((float)___location_0.BotSpawnPeriodCheck, 15f);
                isAtBotCap = ___botsController_0.MaxCount - ___botsController_0.AliveAndLoadingBotsCount <= 0;
                return false;
            }
            
            // Per-map per-player total scav budget (configurable, defaults sized per map).
            // Scaled at spawn time by PerPlayerScavMultiplier.
            var mapNameForBudget = (Utility.CurrentLocation ?? "default").ToLower();
            var perPlayerBudget = Utility.GetMaxScavsForMap(mapNameForBudget);
            // Fall back to vanilla per-player cap if no per-map config (eg unrecognized map).
            if (perPlayerBudget <= 0)
                perPlayerBudget = ___botsController_0.BotLocationModifier.NonWaveSpawnBotsLimitPerPlayerPvE;

            if (Utility.BotsSpawnedPerPlayer >= perPlayerBudget)
            {
                return false;
            }

            // Schedule gate: cap the per-player budget based on raid progress so scavs
            // spawn steadily throughout the raid instead of all in the first window.
            // Piecewise linear: (0%, Start), (MidTime, MidBudget), (FullTime, 100%).
            var budgetCap = (double)perPlayerBudget;
            var botStart = (float)___location_0.BotStart;
            var botStop = (float)___location_0.BotStop;
            if (botStop > botStart && budgetCap > 0)
            {
                var elapsedFrac = Math.Min(1.0, Math.Max(0.0, (___abstractGame_0.PastTime - botStart) / (botStop - botStart)));
                var startBudget = Plugin.ScavScheduleStartPercent;
                var midTime = Plugin.ScavScheduleMidTimePercent;
                var midBudget = Plugin.ScavScheduleMidBudgetPercent;
                var fullTime = Plugin.ScavScheduleFullPercent;

                double allowedFrac;
                if (elapsedFrac >= fullTime)
                {
                    allowedFrac = 1.0;
                }
                else if (elapsedFrac <= midTime)
                {
                    // Lerp from startBudget at 0 to midBudget at midTime.
                    var segment = midTime > 0 ? elapsedFrac / midTime : 1.0;
                    allowedFrac = startBudget + (midBudget - startBudget) * segment;
                }
                else
                {
                    // Lerp from midBudget at midTime to 1.0 at fullTime.
                    var segment = (elapsedFrac - midTime) / Math.Max(0.0001, fullTime - midTime);
                    allowedFrac = midBudget + (1.0 - midBudget) * segment;
                }

                if (Utility.BotsSpawnedPerPlayer >= budgetCap * allowedFrac)
                {
                    return false;
                }
            }
            
            var mapName = Utility.CurrentLocation.ToLower();
            if (Utility.CurrentMapZones.Count == 0)
            {
                Utility.CurrentMapZones = ___botsController_0.BotSpawner.AllBotZones.ToList();
            }

            var botZone = GetValidBotZone(WildSpawnType.assault, 1, ___botsController_0.BotSpawner.AllBotZones, mapName, ___botsController_0);
            ___botsController_0.ActivateBotsByWave(new BotWaveDataClass
            {
                BotsCount = 1,
                Time = Time.time,
                Difficulty = ___gclass1881_0.Random(),
                IsPlayers = GClass856.IsTrue100(Plugin.PScavChance),
                Side = EPlayerSide.Savage,
                WildSpawnType = WildSpawnType.assault,
                SpawnAreaName = botZone,
                WithCheckMinMax = false,
                ChanceGroup = 0,
            });
            Utility.BotsSpawnedPerPlayer += 1d / Math.Max(1d, 1d + Plugin.PerPlayerScavMultiplier * Math.Max(0, Utility.CachedConnectedPlayers.Count - 1));

            if (!(Time.time >= _nextDespawnCheckTime) || !Plugin.DespawnFurthest)
            {
                return false;
            }
            
            _nextDespawnCheckTime = Time.time + Plugin.DespawnTimer;
            var center = GetPlayerCountAndCenter();
            DespawnFurthestBots(___botsController_0, center);

            return false;
        }
        
        private static void DespawnFurthestBots(BotsController botsController, Vector3 centerOfPlayers)
        {
            var despawnDistance = Plugin.DespawnDistance;

            if (Plugin.DespawnPmcs)
            {
                foreach (var pmc in Utility.CachedPmcs)
                {
                    if (pmc == null || !pmc.HealthController.IsAlive) continue;
                    if (Vector3.Distance(pmc.Position, centerOfPlayers) >= despawnDistance)
                        AttemptToDespawnBot(botsController, pmc.AIData.BotOwner);
                }
            }

            foreach (var scav in Utility.CachedAssaultBots)
            {
                if (scav == null || !scav.HealthController.IsAlive) continue;
                if (Vector3.Distance(scav.Position, centerOfPlayers) >= despawnDistance)
                    AttemptToDespawnBot(botsController, scav.AIData.BotOwner);
            }
        }
        
        private static Vector3 GetPlayerCountAndCenter()
        {
            var centerPoint = Vector3.zero;
            var count = 0;

            foreach (var player in Utility.CachedConnectedPlayers)
            {
                if (player == null || !player.HealthController.IsAlive)
                {
                    continue;
                }
                
                centerPoint += player.Position;
                count++;
            }

            return count == 0 ? centerPoint : centerPoint / count;
        }

        private static void AttemptToDespawnBot(BotsController botsController, BotOwner botToDespawn)
        {
            var effectsCommutator = Singleton<Effects>.Instance.EffectsCommutator;
            var gameWorld = Singleton<GameWorld>.Instance;

            if (effectsCommutator is null || gameWorld is null) return;

            var botPlayer = botToDespawn.GetPlayer;
            
            effectsCommutator.StopBleedingForPlayer(botPlayer);
            gameWorld.UnregisterPlayer(botToDespawn);
            gameWorld.UnregisterPlayer(botPlayer);
            botToDespawn.Deactivate();
            botToDespawn.Dispose();
            botsController.BotDied(botToDespawn);
            botsController.DestroyInfo(botPlayer);
            UnityEngine.Object.DestroyImmediate(botToDespawn.gameObject);
            UnityEngine.Object.Destroy(botToDespawn);
        }

        private static void TryToSpawnPmc(BotsController botsController, AbstractGame abstractGame, LocationSettingsClass.Location location)
        {
            // Per-map cap (config). If unknown map, bail.
            var mapName = (Utility.CurrentLocation ?? "default").ToLower();
            var maxPmcs = Utility.GetMaxPmcsForMap(mapName);
            if (maxPmcs <= 0) return;

            // Hard cap: never exceed per-map PMC total for this raid.
            if (Utility.PmcsSpawnedThisRaid >= maxPmcs) return;

            // Grace period: give starting PMCs (Time=1, server-scheduled) a head start.
            var raidElapsed = abstractGame.PastTime - location.BotStart;
            if (raidElapsed < Plugin.PmcStartDelaySeconds) return;

            // Per-PMC attempt interval (independent from scav interval).
            if (Time.time - Utility.LastPmcSpawnAttemptTime < Plugin.PmcSpawnAttemptInterval) return;
            Utility.LastPmcSpawnAttemptTime = Time.time;

            // Schedule curve: budget unlocks progressively over raid time.
            var botStart = (float)location.BotStart;
            var botStop = (float)location.BotStop;
            if (botStop > botStart)
            {
                var elapsedFrac = Math.Min(1.0, Math.Max(0.0, (abstractGame.PastTime - botStart) / (botStop - botStart)));
                var startBudget = Plugin.PmcScheduleStartPercent;
                var midTime = Plugin.PmcScheduleMidTimePercent;
                var midBudget = Math.Max(Plugin.PmcScheduleStartPercent, Plugin.PmcScheduleMidBudgetPercent);
                var fullTime = Math.Max(midTime + 0.001, Plugin.PmcScheduleFullPercent);

                double allowedFrac;
                if (elapsedFrac >= fullTime)
                    allowedFrac = 1.0;
                else if (elapsedFrac <= midTime)
                {
                    var segment = midTime > 0 ? elapsedFrac / midTime : 1.0;
                    allowedFrac = startBudget + (midBudget - startBudget) * segment;
                }
                else
                {
                    var segment = (elapsedFrac - midTime) / Math.Max(0.0001, fullTime - midTime);
                    allowedFrac = midBudget + (1.0 - midBudget) * segment;
                }

                if (Utility.PmcsSpawnedThisRaid >= maxPmcs * allowedFrac) return;
            }

            // Group roll. Group size is additional members beyond the leader.
            var groupSize = 0;
            if (Plugin.PmcMaxGroupSize > 1 && Utility.PmcsSpawnedThisRaid + 1 < maxPmcs)
            {
                if (UnityEngine.Random.Range(0, 100) < Plugin.PmcGroupChance)
                {
                    groupSize = UnityEngine.Random.Range(1, Plugin.PmcMaxGroupSize + 1);
                    var remaining = maxPmcs - Utility.PmcsSpawnedThisRaid - 1;
                    if (groupSize > remaining) groupSize = Math.Max(0, remaining);
                }
            }
            var totalBots = 1 + groupSize;

            // USEC vs BEAR roll.
            var isUsec = UnityEngine.Random.Range(0, 100) < Plugin.UsecChancePercent;
            var spawnType = isUsec ? WildSpawnType.pmcUSEC : WildSpawnType.pmcBEAR;
            var side = isUsec ? EPlayerSide.Usec : EPlayerSide.Bear;

            // Pick a zone (any non-snipe). The actual spawn point is overridden by
            // TryToSpawnInZonePatch when the wave fires.
            if (Utility.CurrentMapZones.Count == 0)
                Utility.CurrentMapZones = botsController.BotSpawner.AllBotZones.ToList();
            var botZone = GetValidBotZone(spawnType, totalBots, botsController.BotSpawner.AllBotZones, mapName, botsController);

            try
            {
                botsController.ActivateBotsByWave(new BotWaveDataClass
                {
                    BotsCount = totalBots,
                    Time = Time.time,
                    Difficulty = RollPmcDifficulty(),
                    IsPlayers = false,
                    Side = side,
                    WildSpawnType = spawnType,
                    SpawnAreaName = botZone,
                    WithCheckMinMax = false,
                    ChanceGroup = 0,
                });

                Utility.PmcsSpawnedThisRaid += totalBots;

                if (Plugin.DebugLogging)
                    Plugin.LogSource.LogInfo($"[ABPS] PMC wave queued: {(isUsec ? "USEC" : "BEAR")} x{totalBots} on {mapName} ({Utility.PmcsSpawnedThisRaid}/{maxPmcs})");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"[ABPS] PMC ActivateBotsByWave failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // Weighted PMC difficulty roll matching the original server config
        // pmcDifficulty: easy=10, normal=50, hard=30, impossible=10. Used for wave
        // PMCs because the scav BotDifficulty randomizer doesn't reflect PMC tuning.
        private static BotDifficulty RollPmcDifficulty()
        {
            var roll = UnityEngine.Random.Range(0, 100);
            if (roll < 10) return BotDifficulty.easy;
            if (roll < 60) return BotDifficulty.normal;
            if (roll < 90) return BotDifficulty.hard;
            return BotDifficulty.impossible;
        }
        
        private static string GetValidBotZone(WildSpawnType botType, int count, BotZone[] allZones, string location, BotsController _botsController)
        {
            if (Utility.CachedNonSnipeZones == null || Utility.CachedNonSnipeZones.Count == 0)
            {
                Utility.CachedNonSnipeZones = allZones.Where(x => !x.SnipeZone).ToList();
            }
            var botZones = Utility.CachedNonSnipeZones.OrderBy(_ => GClass856.Random(0f, 1f)).ToList();

            if (Plugin.EnableHotzones && GClass856.IsTrue100(Plugin.HotzoneScavChance) && Utility.MapHotSpots.ContainsKey(location))
            {
                var hotSpotZone = Utility.MapHotSpots[location].RandomElement();
                return hotSpotZone;
            }
            foreach (var currentZone in botZones)
            {
                if (_botsController.Bots.GetListByZone(currentZone).Count(x => x.IsRole(WildSpawnType.assault)) < Plugin.ZoneScavCap)
                {
                    return currentZone.NameZone;
                }
            }

            return "";
        }
    }
}
