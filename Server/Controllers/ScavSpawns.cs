using _botplacementsystem.Globals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Collections;

namespace _botplacementsystem.Controllers;

[Injectable]
public class ScavSpawns(
    ISptLogger<ScavSpawns> logger,
    ICloner cloner,
    RandomUtil randomUtil,
    DatabaseService databaseService)
{
    public List<Wave> GetCustomMapData(string location)
    {
        return GetConfigValueForLocation(location);
    }

    private List<Wave> GetConfigValueForLocation(string location)
    {
        var scavSpawnInfo = new List<Wave>();
        if (ModConfig.Config.ScavConfig.StartingScavs.StartingMarksman)
        {
            var marksmanSpawn = GenerateStartingScavs(location, "marksman");
            if (marksmanSpawn.Any())
                scavSpawnInfo.AddRange(marksmanSpawn);
        }

        if (ModConfig.Config.ScavConfig.StartingScavs.Enable)
        {
            var assaultSpawn = GenerateStartingScavs(location, "assault", scavSpawnInfo.Count);
            if (assaultSpawn.Any())
                scavSpawnInfo.AddRange(assaultSpawn);
        }

        return scavSpawnInfo;
    }
    
    public List<Wave> GetLateStartMapData(string location, double remainingRaidTime)
    {
        var scavSpawnInfo = new List<Wave>();
        
        if (!ModConfig.Config.ScavConfig.StartingScavs.MaxBotSpawns.TryGetValue(location, out var maxStartingSpawns))
        {
            logger.Error($"unable to find location MaxBotSpawns: {location}");
            return scavSpawnInfo;
        }
        var lateStartCap = remainingRaidTime switch
        {
            >= 1800 => randomUtil.GetInt(maxStartingSpawns, maxStartingSpawns),
            >= 1200 => randomUtil.GetInt(5, 8),
            >= 600  => randomUtil.GetInt(3, 5),
            _       => randomUtil.GetInt(2, 3)
        };
        
        if (ModConfig.Config.ScavConfig.StartingScavs.StartingMarksman)
        {
            var marksmanSpawn = GenerateStartingScavs(location, "marksman");
            if (marksmanSpawn.Any())
                scavSpawnInfo.AddRange(marksmanSpawn);
        }

        if (ModConfig.Config.ScavConfig.StartingScavs.Enable)
        {
            // Pass lateStartCap as the max, scavSpawnInfo.Count as current so marksman count against the cap
            var assaultSpawn = GenerateStartingScavs(location, "assault", scavSpawnInfo.Count, lateStartCap);
            if (assaultSpawn.Any())
                scavSpawnInfo.AddRange(assaultSpawn);
        }

        return scavSpawnInfo;
    }

    private List<Wave> GenerateStartingScavs(string location, string? botRole = "assault", int currentCount = 0, int? capOverride = null)
    {
        var scavWaveSpawnInfo = new List<Wave>();


        if (!databaseService.GetLocations().GetDictionary().TryGetValue(databaseService.GetLocations().GetMappedKey(location), out var locationData))
        {
            logger.Error($"unable to find location: {location}");
            return scavWaveSpawnInfo;
        }

        if (!ModConfig.Config.ScavConfig.StartingScavs.MaxBotSpawns.TryGetValue(location, out var maxStartingSpawns))
        {
            logger.Error($"unable to find location MaxBotSpawns: {location}");
            return scavWaveSpawnInfo;
        }
        
        var hardCap = capOverride ?? maxStartingSpawns;
        var scavCap = capOverride ?? maxStartingSpawns;

        var availableSpawnZones = botRole == "assault"
            ? new ExhaustableArray<string>(GetNonMarksmanSpawnZones(location), randomUtil, cloner)
            : new ExhaustableArray<string>(GetMarksmanSpawnZones(location), randomUtil, cloner);

        var marksmanCount = 0;
        // Per-map cap on the TOTAL number of snipers. Falls back to 3 if unset.
        var marksmanMax = ModConfig.Config.ScavConfig.StartingScavs.MaxMarksmanSpawns != null
            && ModConfig.Config.ScavConfig.StartingScavs.MaxMarksmanSpawns.TryGetValue(location, out var perMapMarksmanMax)
                ? perMapMarksmanMax
                : 3;
        // Snipe zones shuffled once. Snipers are placed one-per-zone in order and wrap
        // around if marksmanMax exceeds the zone count (so a nest can hold 2+ snipers).
        var shuffledSnipeZones = botRole != "assault" && GetMarksmanSpawnZones(location) is { Count: > 0 } snipeZones
            ? snipeZones.OrderBy(_ => randomUtil.GetInt(0, 100000)).ToList()
            : new List<string>();

        var isMarksman = botRole != "assault";

        // Marksman ignores the scav cap (scavCap/hardCap) - it is bounded only by
        // marksmanMax and available snipe zones (handled by the inner break below).
        while (isMarksman || currentCount < scavCap)
        {
            if (!isMarksman && currentCount >= hardCap) break;
            var scavDefaultData = cloner.Clone(ModConfig.ScavDefaults);
            var selectedSpawnZone =
                location.Contains("factory") || (botRole == "assault" && location.Contains("sandbox")) || location.Contains("labyrinth") || !availableSpawnZones.HasValues()
                    ? ""
                    : availableSpawnZones.GetRandomValue();

            if (isMarksman)
            {
                // Exact total sniper count: stop at marksmanMax, reuse zones via wraparound.
                if (marksmanCount >= marksmanMax || shuffledSnipeZones.Count == 0) break;
                selectedSpawnZone = shuffledSnipeZones[marksmanCount % shuffledSnipeZones.Count];
                marksmanCount++;
            }

            if (scavDefaultData is null) continue;
            
            scavDefaultData.SlotsMin = botRole == "assault" ? 0 : 1;
            // Assault scavs: 0-1 per wave. Marksman: exactly 1 sniper per wave, so the
            // total sniper count equals maxMarksmanSpawns for the map.
            scavDefaultData.SlotsMax = 1;
            scavDefaultData.TimeMin = botRole == "assault" ? 3 : ModConfig.Config.ScavConfig.StartingScavs.MarksmanTimeMin ?? 60;
            scavDefaultData.TimeMax = botRole == "assault" ? 5 : ModConfig.Config.ScavConfig.StartingScavs.MarksmanTimeMax ?? 300;
            scavDefaultData.Number = currentCount;
            scavDefaultData.WildSpawnType = botRole == "assault" ? WildSpawnType.assault : WildSpawnType.marksman;
            scavDefaultData.IsPlayers = botRole == "assault" && randomUtil.GetChance100(10); // <- This doesn't actually matter because the client handles it in this version
            scavDefaultData.SpawnPoints = selectedSpawnZone;

            currentCount++;
            scavWaveSpawnInfo.Add(scavDefaultData);
        }

        return scavWaveSpawnInfo;
    }

    private List<string>? GetMarksmanSpawnZones(string location)
    {
        return location switch
        {
            "bigmap" => ModConfig.MapZoneDefaults.CustomsSnipeSpawnZones,
            "lighthouse" => ModConfig.MapZoneDefaults.LighthouseSnipeSpawnZones,
            "sandbox" or "sandbox_high" => ModConfig.MapZoneDefaults.GroundZeroSnipeSpawnZones,
            "shoreline" => ModConfig.MapZoneDefaults.ShorelineSnipeSpawnZones,
            "tarkovstreets" => ModConfig.MapZoneDefaults.StreetsSnipeSpawnZones,
            "woods" => ModConfig.MapZoneDefaults.WoodsSnipeSpawnZones,
            _ => null
        };
    }

    private List<string>? GetNonMarksmanSpawnZones(string location)
    {
        return location switch
        {
            "bigmap" => ModConfig.MapZoneDefaults.CustomsSpawnZones,
            "factory4_day" or "factory4_night" => ModConfig.MapZoneDefaults.FactorySpawnZones,
            "interchange" => ModConfig.MapZoneDefaults.InterchangeSpawnZones,
            "laboratory" => ModConfig.MapZoneDefaults.LabsNonGateSpawnZones,
            "lighthouse" => ModConfig.MapZoneDefaults.LighthouseNonWaterTreatmentSpawnZones,
            "rezervbase" => ModConfig.MapZoneDefaults.ReserveSpawnZones,
            "sandbox" or "sandbox_high" => ModConfig.MapZoneDefaults.GroundZeroSpawnZones,
            "shoreline" => ModConfig.MapZoneDefaults.ShorelineSpawnZones,
            "tarkovstreets" => ModConfig.MapZoneDefaults.StreetsSpawnZones,
            "woods" => ModConfig.MapZoneDefaults.WoodsSpawnZones,
            "labyrinth" => ModConfig.MapZoneDefaults.LabyrinthSpawnZones,
            _ => null
        };
    }
}