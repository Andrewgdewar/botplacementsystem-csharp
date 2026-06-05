using _botplacementsystem.Globals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace _botplacementsystem.Controllers;

[Injectable]
public class BossSpawns(
    ISptLogger<BossSpawns> logger,
    ICloner cloner,
    WeightedRandomHelper weightedRandomHelper,
    RandomUtil randomUtil,
    ConfigServer configServer,
    PresetManager presetManager)
{
    private readonly BotConfig _botConfig = configServer.GetConfig<BotConfig>();

    // Mirrors MOAR's bossPerformanceHash: caps heavy boss entourages (and a couple of
    // chances) to reduce bot load. EscortAmount is a weighted comma list SPT rolls per
    // spawn; Chance (when set) overrides the configured spawn chance for that boss.
    private static readonly Dictionary<string, (string? EscortAmount, int? Chance)> BossPerformanceCaps = new()
    {
        ["bossZryachiy"] = ("0", 50),     // Lighthouse: halve chance, drop escort
        ["exUsec"]       = ("1", 40),     // Roaming rogues: cap pack to 1 follower
        ["bossBully"]    = ("2,3", null), // Reshala escort capped
        ["bossBoar"]     = ("1,2,2,2", null), // Boar (streets) escort capped
        ["bossKojaniy"]  = ("1,2,2", null),   // Shturman escort capped
    };

    private static void ApplyPerformanceCaps(BossLocationSpawn? spawn)
    {
        if (spawn?.BossName is null) return;
        if (!BossPerformanceCaps.TryGetValue(spawn.BossName, out var cap)) return;

        if (cap.EscortAmount is not null)
        {
            spawn.BossEscortAmount = cap.EscortAmount;
            spawn.Supports = null!; // drop heavy support groups (e.g. Boar close guards)
        }
        if (cap.Chance is not null) spawn.BossChance = cap.Chance.Value;
    }

    /// <summary>
    /// boss-rotation preset: for each present main boss, swap it for a random DIFFERENT
    /// boss from the rotation pool. The injected boss is forced to 100% spawn and keeps
    /// the original boss's zone. Runs AFTER performance caps so the pool's escort/support
    /// values (not the capped originals) are what spawns.
    /// </summary>
    private void ApplyBossRotation(List<BossLocationSpawn> bossesForMap)
    {
        if (!presetManager.RotateMainBosses) return;
        var pool = ModConfig.MainBossRotationPool;
        if (pool is null || pool.Count < 2) return;

        var poolNames = pool.Select(b => b.BossName).Where(n => n is not null).ToHashSet();
        var difficultyWeights = ModConfig.Config.BossDifficulty;

        for (var i = 0; i < bossesForMap.Count; i++)
        {
            var original = bossesForMap[i];
            if (original.BossName is null || !poolNames.Contains(original.BossName)) continue;

            var candidates = pool.Where(b => b.BossName != original.BossName).ToList();
            if (candidates.Count == 0) continue;

            var pick = candidates[randomUtil.GetInt(0, candidates.Count - 1)];
            var injected = cloner.Clone(pick);
            if (injected is null) continue;

            injected.BossChance = 100;
            injected.BossZone = original.BossZone;
            injected.BossDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            injected.BossEscortDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossesForMap[i] = injected;

            logger.Info($"[ABPS] boss-rotation: {original.BossName} -> {injected.BossName} @ zone='{injected.BossZone}'");
        }
    }

    /// <summary>
    /// roaming-goon-squad preset: guarantee the Goons squad on every map. If the map
    /// already has a knight, replace it with our example version keeping the map's zone.
    /// If no knight is present, inject our example as-is (BossZone "" = roams anywhere).
    /// </summary>
    private void ApplyRoamingGoonSquad(List<BossLocationSpawn> bossesForMap)
    {
        if (!presetManager.RoamingGoonSquad) return;
        if (ModConfig.InjectionExamples is null ||
            !ModConfig.InjectionExamples.TryGetValue("knight", out var knightTemplate) ||
            knightTemplate is null) return;

        var difficultyWeights = ModConfig.Config.BossDifficulty;
        var foundKnight = false;

        for (var i = 0; i < bossesForMap.Count; i++)
        {
            if (bossesForMap[i].BossName != "bossKnight") continue;

            var replacement = cloner.Clone(knightTemplate);
            if (replacement is null) continue;
            replacement.BossZone = bossesForMap[i].BossZone; // adopt the map's knight zone
            replacement.BossDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            replacement.BossEscortDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossesForMap[i] = replacement;
            foundKnight = true;
            logger.Info($"[ABPS] roaming-goon-squad: replaced map knight @ zone='{replacement.BossZone}'");
        }

        if (!foundKnight)
        {
            var injected = cloner.Clone(knightTemplate);
            if (injected is null) return;
            injected.BossDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            injected.BossEscortDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossesForMap.Add(injected); // BossZone stays "" -> roams
            logger.Info($"[ABPS] roaming-goon-squad: injected roaming knight (no existing knight)");
        }
    }

    public List<BossLocationSpawn> GetCustomMapData(string location, double escapeTimeLimit)
    {
        return GetConfigValueForLocation(location, escapeTimeLimit);
    }

    private List<BossLocationSpawn> GetConfigValueForLocation(string location, double escapeTimeLimit)
    {
        var bossesForMap = new List<BossLocationSpawn>();

        foreach (var (boss, bossData) in ModConfig.Config.BossConfig)
        {
            var bossDefaultData = cloner.Clone(GetDefaultValuesForBoss(boss, location));
            var difficultyWeights = ModConfig.Config.BossDifficulty;

            if (!bossData.Enable) continue;
            if (bossDefaultData is null) continue;
            if (bossData.DisableFollowers)
            {
                bossDefaultData[0].BossEscortAmount = "0";
                bossDefaultData[0].BossEscortType = bossDefaultData[0].BossName;
                bossDefaultData[0].Supports = null!;
            }
            
            if (boss == "exUsec" && !(bossData.DisableVanillaSpawns ?? false) && location == "lighthouse" ||
                boss == "pmcBot" && !(bossData.DisableVanillaSpawns ?? false) && (location == "laboratory" || location == "rezervbase") ||
                boss == "tagillaHelperAgro" && !(bossData.DisableVanillaSpawns ?? false) && location == "labyrinth")
            {
                foreach (var bossSpawn in bossDefaultData)
                {
                    bossDefaultData[0].BossDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
                    ApplyPerformanceCaps(bossSpawn);
                    bossesForMap.Add(bossSpawn);
                }
                if (!(bossData.AddExtraSpawns ?? false)) continue;
            }

            if (bossData.SpawnChance[location] == 0) continue;

            if (location.Contains("factory")) bossData.BossZone[location] = "BotZone";
            if (location.Contains("labyrinth")) bossData.BossZone[location] = "";
            if ((boss == "pmcBot") && (bossData.AddExtraSpawns ?? false))
            {
                bossesForMap.AddRange(GenerateBossWaves(location, escapeTimeLimit));
                continue;
            }

            if (!Enum.TryParse<WildSpawnType>(boss, ignoreCase: true, out var bossType))
            {
                logger.Warning($"Boss: {boss} is not a valid WildSpawnType. Report this.");
                bossDefaultData[0].BossChance = bossData.SpawnChance[location];
            }
            else
            {
                if (ModConfig.Config.WeeklyBoss.Enable)
                {
                    var isWeeklyBoss = IsWeeklyBoss(bossType);
                    if (isWeeklyBoss)
                    {
                        logger.Warning($"Weekly Boss: {boss} | 100% Chance on {location}");
                        bossDefaultData[0].ShowOnTarkovMap = true;
                        bossDefaultData[0].ShowOnTarkovMapPvE = true;
                        bossDefaultData[0].BossChance = 100;
                    }
                    else bossDefaultData[0].BossChance = bossData.SpawnChance[location];
                }
                else bossDefaultData[0].BossChance = bossData.SpawnChance[location];
            }

            bossDefaultData[0].BossZone = (string?)bossData.BossZone[location];
            bossDefaultData[0].BossDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossDefaultData[0].BossEscortDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossDefaultData[0].Time = bossData.Time;
            ApplyPerformanceCaps(bossDefaultData[0]);
            bossesForMap.Add(bossDefaultData[0]);
        }

        ApplyBossRotation(bossesForMap);
        ApplyRoamingGoonSquad(bossesForMap);

        return bossesForMap;
    }

    private bool IsWeeklyBoss(WildSpawnType bossType)
    {
        var bossList = _botConfig.WeeklyBoss.BossPool;
        var startOfWeek = DateTime.Today.GetMostRecentPreviousDay(DayOfWeek.Monday);

        var seed = startOfWeek.Year * 1000 + startOfWeek.DayOfYear;
        var random =  new Random(seed);

        var boss = bossList[random.Next(0, bossList.Count)];

        return boss == bossType;
    }

    private List<BossLocationSpawn> GenerateBossWaves(string location, double escapeTimeLimit)
    {
        var pmcWaveSpawnInfo = new List<BossLocationSpawn>();

        var difficultyWeights = ModConfig.Config.BossDifficulty;
        var waveMaxBotCount = location != "laboratory" ? 4 : 10;
        var waveGroupLimit = 3;
        var waveGroupSize = 2;
        var waveGroupChance = 100;
        var waveTimer = 450;
        var endWavesAtRemainingTime = 600;
        var waveCount = Math.Floor((((escapeTimeLimit * 60) - endWavesAtRemainingTime)) / waveTimer);
        var currentWaveTime = waveTimer;
        var bossConfigData = ModConfig.Config.BossConfig["pmcBot"];

        for (var i = 1; i <= waveCount; i++)
        {
            if (i == 1) currentWaveTime = -1;

            var currentBotCount = 0;
            var groupCount = 0;
            while (currentBotCount < waveMaxBotCount)
            {
                if (groupCount >= waveGroupLimit) break;
                var groupSize = 0;
                var remainingSpots = waveMaxBotCount - currentBotCount;
                var isAGroup = remainingSpots > 1 && randomUtil.GetChance100(waveGroupChance);
                if (isAGroup)
                {
                    groupSize = Math.Min(remainingSpots - 1, randomUtil.GetInt(1, waveGroupSize));
                }

                var bossDefaultData = cloner.Clone(GetDefaultValuesForBoss("pmcBot", ""));

                if (bossDefaultData is null) continue;
                
                bossDefaultData[0].BossChance = bossConfigData.SpawnChance[location];
                bossDefaultData[0].BossZone = bossConfigData.BossZone[location];
                bossDefaultData[0].BossEscortAmount = groupSize.ToString();
                bossDefaultData[0].BossDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
                bossDefaultData[0].BossEscortDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
                bossDefaultData[0].IgnoreMaxBots = false;
                bossDefaultData[0].Time = currentWaveTime;
                currentBotCount += groupSize + 1;
                groupCount++;
                
                if (bossConfigData.DisableFollowers)
                {
                    bossDefaultData[0].BossEscortAmount = "0";
                    bossDefaultData[0].BossEscortType = bossDefaultData[0].BossName;
                    bossDefaultData[0].Supports = null!;
                }
                
                pmcWaveSpawnInfo.Add(bossDefaultData[0]);
            }
            
            currentWaveTime += waveTimer;
        }

        return pmcWaveSpawnInfo;
    }

    private List<BossLocationSpawn> GetDefaultValuesForBoss(string boss, string location)
    {
        switch (boss)
        {
            case "bossKnight":
                return ModConfig.BossWaveDefaults["bossKnightData"];
            case "bossBully":
                return ModConfig.BossWaveDefaults["bossBullyData"];
            case "bossTagilla":
                return ModConfig.BossWaveDefaults["bossTagillaData"];
            case "bossKilla":
                return ModConfig.BossWaveDefaults["bossKillaData"];
            case "bossZryachiy":
                return ModConfig.BossWaveDefaults["bossZryachiyData"];
            case "bossGluhar":
                return ModConfig.BossWaveDefaults["bossGluharData"];
            case "bossSanitar":
                return ModConfig.BossWaveDefaults["bossSanitarData"];
            case "bossKolontay":
                return ModConfig.BossWaveDefaults["bossKolontayData"];
            case "bossBoar":
                return ModConfig.BossWaveDefaults["bossBoarData"];
            case "bossKojaniy":
                return ModConfig.BossWaveDefaults["bossKojaniyData"];
            case "bossTagillaAgro":
                return ModConfig.BossWaveDefaults["bossTagillaAgroData"];
            case "bossKillaAgro":
                return location == "labyrinth" ? ModConfig.BossWaveDefaults["bossKillaAgroData"] : ModConfig.BossWaveDefaults["bossKillaAgroNonLabyData"];
            case "tagillaHelperAgro":
                return location == "labyrinth" ? ModConfig.BossWaveDefaults["tagillaHelperAgroData"] : ModConfig.BossWaveDefaults["tagillaHelperAgroNonLabyData"];
            case "bossPartisan":
                return ModConfig.BossWaveDefaults["bossPartisanData"];
            case "sectantPriest":
                return ModConfig.BossWaveDefaults["sectantPriestData"];
            case "arenaFighterEvent":
                return ModConfig.BossWaveDefaults["arenaFighterEventData"];
            case "pmcBot": // Requires Triggers + Has Multiple Zones
                return location switch
                {
                    "rezervbase" => ModConfig.BossWaveDefaults["pmcBotReserveData"],
                    "laboratory" => ModConfig.BossWaveDefaults["pmcBotLaboratoryData"],
                    _ => ModConfig.BossWaveDefaults["pmcBotData"]
                };
            case "exUsec": // Has Multiple Zones
                return ModConfig.BossWaveDefaults["exUsecData"];
            case "gifter":
                return ModConfig.BossWaveDefaults["gifterData"];
            default:
                logger.Error($"[ABPS] Boss not found in config {boss}");
                return null;
        }
    }
}