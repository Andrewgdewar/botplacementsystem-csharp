using _botplacementsystem.Globals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace _botplacementsystem.Controllers;

[Injectable]
public class PmcSpawns(
    ISptLogger<PmcSpawns> logger,
    ICloner cloner,
    WeightedRandomHelper weightedRandomHelper,
    RandomUtil randomUtil)
{
    public List<BossLocationSpawn> GetCustomMapData(string location, double escapeTimeLimit)
    {
        return GetConfigValueForLocation(location, escapeTimeLimit);
    }

    private List<BossLocationSpawn> GetConfigValueForLocation(string location, double escapeTimeLimit)
    {
        location = location.ToLowerInvariant();

        var pmcSpawnInfo = new List<BossLocationSpawn>();

        if (ModConfig.Config.PmcConfig.StartingPMCs.Enable)
        {
            pmcSpawnInfo.AddRange(GenerateStartingPmcWaves(location));
        }

        var canGenerateWaves = ModConfig.Config.PmcConfig.Waves.Enable && (location != "labyrinth" || ModConfig.Config.PmcConfig.Waves.AllowPmcsOnLabyrinth);
        if (canGenerateWaves)
        {
            pmcSpawnInfo.AddRange(GeneratePmcWaves(location, escapeTimeLimit));
        }

        return pmcSpawnInfo;
    }

    private List<BossLocationSpawn> GeneratePmcWaves(string location, double escapeTimeLimit)
    {
        var pmcWaveSpawnInfo = new List<BossLocationSpawn>();
        var ignoreMaxBotCaps = ModConfig.Config.PmcConfig.Waves.IgnoreMaxBotCaps;
        var difficultyWeights = ModConfig.Config.PmcDifficulty;
        var waveGroupSize = ModConfig.Config.PmcConfig.Waves.MaxGroupSize;
        var waveGroupChance = ModConfig.Config.PmcConfig.Waves.GroupChance;
        var firstWaveTimer = ModConfig.Config.PmcConfig.Waves.DelayBeforeFirstWave;

        // MOAR-style even-spread schedule with group bias.
        // waveBudget = total PMC slots for waves (max total - starting cap reserved).
        // averageTime = active window / budget. Each scheduled entry advances time by
        // (1 + groupSize) * averageTime so a 3-man group naturally pushes the next
        // spawn out by 3x. Hard stop at taperEndTime regardless.
        var raidSeconds = escapeTimeLimit * 60;
        ModConfig.Config.PmcConfig.Waves.MaxTotalPmcs.TryGetValue(location, out var maxTotalPmcs);
        if (!ModConfig.Config.PmcConfig.StartingPMCs.MapLimits.TryGetValue(location, out var startingRange))
        {
            logger.Info($"[ABPS] {location}: unknown map for StartingPMCs.MapLimits, skipping wave schedule");
            return pmcWaveSpawnInfo;
        }
        var startingPmcMax = startingRange.Max;
        var waveBudget = Math.Max(0, maxTotalPmcs - startingPmcMax);
        var taperEndTime = raidSeconds * ModConfig.Config.PmcConfig.Waves.TaperEndPercent;
        var activeWindow = taperEndTime - firstWaveTimer;

        if (waveBudget <= 0 || activeWindow <= 0)
        {
            logger.Info($"[ABPS] {location}: no PMC waves (budget {waveBudget}, window {activeWindow:0}s)");
            return pmcWaveSpawnInfo;
        }

        var averageTime = activeWindow / waveBudget;
        var slotsRemaining = waveBudget;
        var currentTime = (double)firstWaveTimer;
        var totalSpawned = 0;
        const int jitter = 15;

        while (slotsRemaining > 0 && currentTime < taperEndTime)
        {
            // Roll for a group, capped by remaining slots and configured max.
            var allowGroup = waveGroupSize > 1 && slotsRemaining > 1 && randomUtil.GetChance100(waveGroupChance);
            var groupSize = allowGroup ? Math.Min(slotsRemaining - 1, randomUtil.GetInt(1, waveGroupSize)) : 0;

            var pmcType = randomUtil.GetChance100(ModConfig.Config.PmcType.UsecChance) ? "pmcUSEC" : "pmcBEAR";
            var bossDefaultData = cloner.Clone(GetDefaultValuesForBoss(pmcType));
            if (bossDefaultData is null)
            {
                slotsRemaining -= 1 + groupSize;
                currentTime += (1 + groupSize) * averageTime;
                continue;
            }

            bossDefaultData[0].BossEscortAmount = groupSize.ToString();
            bossDefaultData[0].Time = Math.Max(0, currentTime + randomUtil.GetInt(-jitter, jitter));
            bossDefaultData[0].BossDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossDefaultData[0].BossEscortDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossDefaultData[0].BossZone = "";
            bossDefaultData[0].IgnoreMaxBots = ignoreMaxBotCaps;
            pmcWaveSpawnInfo.Add(bossDefaultData[0]);

            var spawnSize = 1 + groupSize;
            totalSpawned += spawnSize;
            slotsRemaining -= spawnSize;
            currentTime += spawnSize * averageTime;
        }

        logger.Info($"[ABPS] {location}: scheduled {totalSpawned} PMCs across {pmcWaveSpawnInfo.Count} waves (budget {waveBudget}, max {maxTotalPmcs}, window {firstWaveTimer:0}s-{taperEndTime:0}s, avg {averageTime:0.0}s)");
        return pmcWaveSpawnInfo;
    }

    private List<BossLocationSpawn> GenerateStartingPmcWaves(string location)
    {
        var startingPmcWaveInfo = new List<BossLocationSpawn>();
        var ignoreMaxBotCaps = ModConfig.Config.PmcConfig.StartingPMCs.IgnoreMaxBotCaps;
        var mapAsMinMax = ModConfig.Config.PmcConfig.StartingPMCs.MapLimits[location];
        var minPmcCount = mapAsMinMax.Min;
        var maxPmcCount = mapAsMinMax.Max;
        var generatedPmcCount = randomUtil.GetInt(minPmcCount, maxPmcCount);
        var groupChance = ModConfig.Config.PmcConfig.StartingPMCs.GroupChance;
        var groupLimit = ModConfig.Config.PmcConfig.StartingPMCs.MaxGroupCount;
        var groupMaxSize = ModConfig.Config.PmcConfig.StartingPMCs.MaxGroupSize;
        var difficultyWeights = ModConfig.Config.PmcDifficulty;

        var currentPmcCount = 0;
        var groupCount = 0;

        while (currentPmcCount < generatedPmcCount)
        {
            var canBeAGroup = groupCount < groupLimit;
            var groupSize = 0;
            var remainingSpots = generatedPmcCount - currentPmcCount;

            var isAGroup = remainingSpots > 1 && randomUtil.GetChance100(groupChance);
            if (isAGroup && canBeAGroup) 
            {
                groupSize = Math.Min(remainingSpots - 1, randomUtil.GetInt(1, groupMaxSize));
                groupCount++;
            }

            var isTrue = randomUtil.GetChance100(ModConfig.Config.PmcType.UsecChance);
            var pmcType = randomUtil.GetChance100(ModConfig.Config.PmcType.UsecChance) ? "pmcUSEC" : "pmcBEAR";
            var bossDefaultData = cloner.Clone(this.GetDefaultValuesForBoss(pmcType));

            if (bossDefaultData is null) continue;
            
            bossDefaultData[0].BossEscortAmount = groupSize.ToString();
            bossDefaultData[0].BossDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossDefaultData[0].BossEscortDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossDefaultData[0].BossZone = "";
            bossDefaultData[0].IgnoreMaxBots = ignoreMaxBotCaps;
            bossDefaultData[0].Time = 1;
            currentPmcCount += groupSize + 1;
            startingPmcWaveInfo.Add(bossDefaultData[0]);
        }
        return startingPmcWaveInfo;
    }

    private List<BossLocationSpawn>? GetDefaultValuesForBoss(string boss)
    {
        switch (boss)
        {
            case "pmcUSEC":
                return ModConfig.PmcDefaults.PmcUSEC;
            case "pmcBEAR":
                return ModConfig.PmcDefaults.PmcBEAR;
            default:
                logger.Error($"[ABPS] PMC not found in config {boss}");
                return null;
        }
    }
    
    public List<BossLocationSpawn> GenerateScavRaidRemainingPmcs(string location, double remainingRaidTime)
    {
        location = location.ToLowerInvariant();
        
        var startingPmcWaveInfo = new List<BossLocationSpawn>();
        var ignoreMaxBotCaps = ModConfig.Config.PmcConfig.StartingPMCs.IgnoreMaxBotCaps;
        var mapMinMax = ModConfig.Config.PmcConfig.StartingPMCs.MapLimits[location];
        var minPmcCount = mapMinMax.Min;
        var maxPmcCount = mapMinMax.Max;
        var generatedPmcCount = randomUtil.GetInt(minPmcCount, maxPmcCount);
        var groupChance = ModConfig.Config.PmcConfig.StartingPMCs.GroupChance;
        var groupLimit = ModConfig.Config.PmcConfig.StartingPMCs.MaxGroupCount;
        var groupMaxSize = ModConfig.Config.PmcConfig.StartingPMCs.MaxGroupSize;
        var difficultyWeights = ModConfig.Config.PmcDifficulty;

        var currentPmcCount = 0;
        var groupCount = 0;
        var spawnTime = 1d;

        
        generatedPmcCount = remainingRaidTime switch
        {
            >= 3000 => randomUtil.GetInt(7, 10),
            >= 2400 => randomUtil.GetInt(6, 9),
            >= 1800 => randomUtil.GetInt(5, 8),
            >= 1200 => randomUtil.GetInt(4, 6),
            >= 600  => randomUtil.GetInt(2, 4),
            _       => randomUtil.GetInt(1, 2)
        };
        
        if ((location.Contains("factory") || location.Contains("labyrinth") || location.Contains("laboratory")) && generatedPmcCount >= 8) generatedPmcCount -= 2;

        while (currentPmcCount < generatedPmcCount)
        {
            var canBeAGroup = groupCount < groupLimit;
            var groupSize = 0;
            var remainingSpots = generatedPmcCount - currentPmcCount;
            var isAGroup = remainingSpots > 1 && randomUtil.GetChance100(groupChance);
            if (isAGroup && canBeAGroup) 
            {
                groupSize = Math.Min(remainingSpots - 1, randomUtil.GetInt(1, groupMaxSize));
                groupCount++;
            }

            var pmcType = randomUtil.GetChance100(ModConfig.Config.PmcType.UsecChance) ? "pmcUSEC" : "pmcBEAR";
            var bossDefaultData = cloner.Clone(GetDefaultValuesForBoss(pmcType));

            if (bossDefaultData is null) continue;
            
            bossDefaultData[0].BossEscortAmount = groupSize.ToString();
            bossDefaultData[0].Time = spawnTime;
            bossDefaultData[0].BossDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossDefaultData[0].BossEscortDifficulty = weightedRandomHelper.GetWeightedValue(difficultyWeights);
            bossDefaultData[0].BossZone = "";
            bossDefaultData[0].IgnoreMaxBots = ignoreMaxBotCaps;
            currentPmcCount += groupSize + 1;
            startingPmcWaveInfo.Add(bossDefaultData[0]);
        }
        
        return startingPmcWaveInfo;
    }
}