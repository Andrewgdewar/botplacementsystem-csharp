using BepInEx.Configuration;
using System;

namespace acidphantasm_botplacementsystem
{
    internal static class AbpsConfig
    {
        private static int _loadOrder = 200;
        
        private const string DespawnConfig = "1. Despawn Settings";
        private static ConfigEntry<bool> _despawnFurthest;
        private static ConfigEntry<bool> _despawnPmcs;
        private static ConfigEntry<float> _despawnDistance;
        private static ConfigEntry<float> _despawnTimer;
        
        private const string GeneralConfig = "2. General Settings";
        private static ConfigEntry<int> _customsMapLimit;
        private static ConfigEntry<int> _factoryMapLimit;
        private static ConfigEntry<int> _interchangeMapLimit;
        private static ConfigEntry<int> _labsMapLimit;
        private static ConfigEntry<int> _lighthouseMapLimit;
        private static ConfigEntry<int> _reserveMapLimit;
        private static ConfigEntry<int> _groundZeroMapLimit;
        private static ConfigEntry<int> _shorelineMapLimit;
        private static ConfigEntry<int> _streetsMapLimit;
        private static ConfigEntry<int> _woodsMapLimit;
        private static ConfigEntry<int> _labyrinthMapLimit;

        private static ConfigEntry<int> _customsMaxPmcs;
        private static ConfigEntry<int> _factoryMaxPmcs;
        private static ConfigEntry<int> _interchangeMaxPmcs;
        private static ConfigEntry<int> _labsMaxPmcs;
        private static ConfigEntry<int> _lighthouseMaxPmcs;
        private static ConfigEntry<int> _reserveMaxPmcs;
        private static ConfigEntry<int> _groundZeroMaxPmcs;
        private static ConfigEntry<int> _groundZeroHighMaxPmcs;
        private static ConfigEntry<int> _shorelineMaxPmcs;
        private static ConfigEntry<int> _streetsMaxPmcs;
        private static ConfigEntry<int> _woodsMaxPmcs;
        private static ConfigEntry<int> _labyrinthMaxPmcs;

        private static ConfigEntry<int> _customsMaxScavs;
        private static ConfigEntry<int> _factoryMaxScavs;
        private static ConfigEntry<int> _interchangeMaxScavs;
        private static ConfigEntry<int> _labsMaxScavs;
        private static ConfigEntry<int> _lighthouseMaxScavs;
        private static ConfigEntry<int> _reserveMaxScavs;
        private static ConfigEntry<int> _groundZeroMaxScavs;
        private static ConfigEntry<int> _groundZeroHighMaxScavs;
        private static ConfigEntry<int> _shorelineMaxScavs;
        private static ConfigEntry<int> _streetsMaxScavs;
        private static ConfigEntry<int> _woodsMaxScavs;
        private static ConfigEntry<int> _labyrinthMaxScavs;
        
        private const string BossConfig = "3. Boss Settings";
        private static ConfigEntry<bool> _regressiveChances;
        private static ConfigEntry<bool> _progressiveChances;
        private static ConfigEntry<int> _chanceStep;
        private static ConfigEntry<int> _minimumChance;
        private static ConfigEntry<int> _maximumChance;

        private const string PmcConfig = "4. PMC Settings";
        private static ConfigEntry<bool> _pmcSpawnAnywhere;
        private static ConfigEntry<float> _customsPmcSpawnDistanceCheck;
        private static ConfigEntry<float> _factoryPmcSpawnDistanceCheck;
        private static ConfigEntry<float> _interchangePmcSpawnDistanceCheck;
        private static ConfigEntry<float> _labsPmcSpawnDistanceCheck;
        private static ConfigEntry<float> _lighthousePmcSpawnDistanceCheck;
        private static ConfigEntry<float> _reservePmcSpawnDistanceCheck;
        private static ConfigEntry<float> _groundZeroPmcSpawnDistanceCheck;
        private static ConfigEntry<float> _shorelinePmcSpawnDistanceCheck;
        private static ConfigEntry<float> _streetsPmcSpawnDistanceCheck;
        private static ConfigEntry<float> _woodsPmcSpawnDistanceCheck;
        private static ConfigEntry<float> _labyrinthPmcSpawnDistanceCheck;

        private const string ScavConfig = "5. Scav Settings";
        private static ConfigEntry<int> _softCap;
        private static ConfigEntry<int> _pScavChance;
        private static ConfigEntry<bool> _enableHotzones;
        private static ConfigEntry<int> _zoneScavCap;
        private static ConfigEntry<int> _hotzoneScavCap;
        private static ConfigEntry<int> _hotzoneScavChance;
        private static ConfigEntry<float> _customsScavSpawnDistanceCheck;
        private static ConfigEntry<float> _factoryScavSpawnDistanceCheck;
        private static ConfigEntry<float> _interchangeScavSpawnDistanceCheck;
        private static ConfigEntry<float> _labsScavSpawnDistanceCheck;
        private static ConfigEntry<float> _lighthouseScavSpawnDistanceCheck;
        private static ConfigEntry<float> _reserveScavSpawnDistanceCheck;
        private static ConfigEntry<float> _groundZeroScavSpawnDistanceCheck;
        private static ConfigEntry<float> _shorelineScavSpawnDistanceCheck;
        private static ConfigEntry<float> _streetsScavSpawnDistanceCheck;
        private static ConfigEntry<float> _woodsScavSpawnDistanceCheck;
        private static ConfigEntry<float> _labyrinthScavSpawnDistanceCheck;
        
        private const string DebugConfig = "6. Debug Settings";
        private static ConfigEntry<bool> _enableDebug;
        private static ConfigEntry<float> _directionalBias;
        private static ConfigEntry<float> _shufflePercent;
        private static ConfigEntry<int> _shuffleStep;
        private static ConfigEntry<float> _scavSpawnNoise;
        private static ConfigEntry<float> _pmcSpawnNoise;
        private static ConfigEntry<float> _pmcSkipClosestPercent;
        private static ConfigEntry<float> _scavSkipClosestPercent;
        private static ConfigEntry<float> _perPlayerScavMultiplier;
        private static ConfigEntry<float> _hotzoneScoreMultiplier;
        private static ConfigEntry<float> _scavScheduleStartPercent;
        private static ConfigEntry<float> _scavScheduleMidTimePercent;
        private static ConfigEntry<float> _scavScheduleMidBudgetPercent;
        private static ConfigEntry<float> _scavScheduleFullPercent;
        private static ConfigEntry<float> _pmcScheduleStartPercent;
        private static ConfigEntry<float> _pmcScheduleMidTimePercent;
        private static ConfigEntry<float> _pmcScheduleMidBudgetPercent;
        private static ConfigEntry<float> _pmcScheduleFullPercent;
        private static ConfigEntry<float> _pmcSpawnAttemptInterval;
        private static ConfigEntry<int> _pmcGroupChance;
        private static ConfigEntry<int> _pmcMaxGroupSize;
        private static ConfigEntry<int> _usecChancePercent;
        private static ConfigEntry<float> _pmcStartDelaySeconds;

        private static ConfigEntry<int> BindMaxPmc(ConfigFile config, string label, int defaultValue, string description)
        {
            return config.Bind(
                GeneralConfig,
                label,
                defaultValue,
                new ConfigDescription(description,
                    new AcceptableValueRange<int>(0, 50),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
        }

        private static ConfigEntry<int> BindMaxScav(ConfigFile config, string label, int defaultValue, string description)
        {
            return config.Bind(
                GeneralConfig,
                label,
                defaultValue,
                new ConfigDescription(description,
                    new AcceptableValueRange<int>(0, 100),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
        }

        public static void InitAbpsConfig(ConfigFile config)
        {
            // Despawn Settings
            _despawnFurthest = config.Bind(
                DespawnConfig,
                "Enable Despawning",
                false,
                new ConfigDescription("Enabling this will only despawn scavs, if you want to also despawn PMCs you must also check the below option.",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true, Order = _loadOrder-- }));
            Plugin.DespawnFurthest = _despawnFurthest.Value;
            _despawnFurthest.SettingChanged += ABPS_SettingChanged;
            
            _despawnPmcs = config.Bind(
                DespawnConfig,
                "Enable Despawning PMCs",
                false,
                new ConfigDescription("Allow ABPS to despawn PMCs. \nRequires `Enable Despawning`\n\n If you enable this and don't turn on PMC waves, then expect to have almost no PMCs in your raids. \nThat's on you.",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true, Order = _loadOrder-- }));
            Plugin.DespawnPmcs = _despawnPmcs.Value;
            _despawnPmcs.SettingChanged += ABPS_SettingChanged;
            
            _despawnDistance = config.Bind(
                DespawnConfig,
                "Despawn Distance",
                250f,
                new ConfigDescription("Distance that bots must be from player to trigger despawning.",
                    new AcceptableValueRange<float>(100f, 500f),
                    new ConfigurationManagerAttributes { IsAdvanced = true, Order = _loadOrder-- }));
            Plugin.DespawnDistance = _despawnDistance.Value;
            _despawnDistance.SettingChanged += ABPS_SettingChanged;
            
            _despawnTimer = config.Bind(
                DespawnConfig,
                "Despawn Timer",
                300f,
                new ConfigDescription("Timer for despawning, this is the MINIMUM time between despawning attempts. In Seconds.",
                    new AcceptableValueRange<float>(180f, 600f),
                    new ConfigurationManagerAttributes { IsAdvanced = true, Order = _loadOrder-- }));
            Plugin.DespawnTimer = _despawnTimer.Value;
            _despawnTimer.SettingChanged += ABPS_SettingChanged;
            
            // General Settings
            _customsMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Customs",
                23,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.CustomsMapLimit = _customsMapLimit.Value;
            _customsMapLimit.SettingChanged += ABPS_SettingChanged;

            _factoryMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Factory",
                13,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.FactoryMapLimit = _factoryMapLimit.Value;
            _factoryMapLimit.SettingChanged += ABPS_SettingChanged;

            _interchangeMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Interchange",
                22,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.InterchangeMapLimit = _interchangeMapLimit.Value;
            _interchangeMapLimit.SettingChanged += ABPS_SettingChanged;

            _labsMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Labs",
                19,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.LabsMapLimit = _labsMapLimit.Value;
            _labsMapLimit.SettingChanged += ABPS_SettingChanged;

            _lighthouseMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Lighthouse",
                22,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.LighthouseMapLimit = _lighthouseMapLimit.Value;
            _lighthouseMapLimit.SettingChanged += ABPS_SettingChanged;

            _reserveMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Reserve",
                22,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ReserveMapLimit = _reserveMapLimit.Value;
            _reserveMapLimit.SettingChanged += ABPS_SettingChanged;

            _groundZeroMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Ground Zero",
                16,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.GroundZeroMapLimit = _groundZeroMapLimit.Value;
            _groundZeroMapLimit.SettingChanged += ABPS_SettingChanged;

            _shorelineMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Shoreline",
                22,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ShorelineMapLimit = _shorelineMapLimit.Value;
            _shorelineMapLimit.SettingChanged += ABPS_SettingChanged;

            _streetsMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Streets",
                23,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.StreetsMapLimit = _streetsMapLimit.Value;
            _streetsMapLimit.SettingChanged += ABPS_SettingChanged;

            _woodsMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Woods",
                22,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.WoodsMapLimit = _woodsMapLimit.Value;
            _woodsMapLimit.SettingChanged += ABPS_SettingChanged;

            _labyrinthMapLimit = config.Bind(
                GeneralConfig,
                "Max Bots - Labyrinth",
                13,
                new ConfigDescription("Max bots allowed on map, value is ignored by certain bots.\nStarting PMCs ignore the cap by default, if you want to change this you must do so in the server config.\n\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.LabyrinthMapLimit = _labyrinthMapLimit.Value;
            _labyrinthMapLimit.SettingChanged += ABPS_SettingChanged;

            // Per-map runtime hard caps for PMC count (server schedules waves up to this number;
            // client enforces it at runtime as a safety net so we never exceed it).
            const string MaxPmcsDesc = "Hard cap on total PMC count (starting + waves) per raid.\nMust match the server config maxTotalPmcs value for this map.\nChanges do not take effect until next raid.";

            _customsMaxPmcs = BindMaxPmc(config, "Max PMCs - Customs", 12, MaxPmcsDesc);
            Plugin.CustomsMaxPmcs = _customsMaxPmcs.Value;
            _customsMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _factoryMaxPmcs = BindMaxPmc(config, "Max PMCs - Factory", 5, MaxPmcsDesc);
            Plugin.FactoryMaxPmcs = _factoryMaxPmcs.Value;
            _factoryMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _interchangeMaxPmcs = BindMaxPmc(config, "Max PMCs - Interchange", 12, MaxPmcsDesc);
            Plugin.InterchangeMaxPmcs = _interchangeMaxPmcs.Value;
            _interchangeMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _labsMaxPmcs = BindMaxPmc(config, "Max PMCs - Labs", 10, MaxPmcsDesc);
            Plugin.LabsMaxPmcs = _labsMaxPmcs.Value;
            _labsMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _lighthouseMaxPmcs = BindMaxPmc(config, "Max PMCs - Lighthouse", 12, MaxPmcsDesc);
            Plugin.LighthouseMaxPmcs = _lighthouseMaxPmcs.Value;
            _lighthouseMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _reserveMaxPmcs = BindMaxPmc(config, "Max PMCs - Reserve", 12, MaxPmcsDesc);
            Plugin.ReserveMaxPmcs = _reserveMaxPmcs.Value;
            _reserveMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _groundZeroMaxPmcs = BindMaxPmc(config, "Max PMCs - Ground Zero", 7, MaxPmcsDesc);
            Plugin.GroundZeroMaxPmcs = _groundZeroMaxPmcs.Value;
            _groundZeroMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _groundZeroHighMaxPmcs = BindMaxPmc(config, "Max PMCs - Ground Zero High", 9, MaxPmcsDesc);
            Plugin.GroundZeroHighMaxPmcs = _groundZeroHighMaxPmcs.Value;
            _groundZeroHighMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _shorelineMaxPmcs = BindMaxPmc(config, "Max PMCs - Shoreline", 12, MaxPmcsDesc);
            Plugin.ShorelineMaxPmcs = _shorelineMaxPmcs.Value;
            _shorelineMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _streetsMaxPmcs = BindMaxPmc(config, "Max PMCs - Streets", 12, MaxPmcsDesc);
            Plugin.StreetsMaxPmcs = _streetsMaxPmcs.Value;
            _streetsMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _woodsMaxPmcs = BindMaxPmc(config, "Max PMCs - Woods", 10, MaxPmcsDesc);
            Plugin.WoodsMaxPmcs = _woodsMaxPmcs.Value;
            _woodsMaxPmcs.SettingChanged += ABPS_SettingChanged;

            _labyrinthMaxPmcs = BindMaxPmc(config, "Max PMCs - Labyrinth", 8, MaxPmcsDesc);
            Plugin.LabyrinthMaxPmcs = _labyrinthMaxPmcs.Value;
            _labyrinthMaxPmcs.SettingChanged += ABPS_SettingChanged;

            // Per-map total scav spawn budget per raid (1 player baseline; scales by Per-Player Scav Multiplier).
            const string MaxScavsDesc = "Max total scav spawns per raid (1 player baseline). Scaled by Per-Player Scav Multiplier as players are added.\nChanges do not take effect until next raid.";

            _customsMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Customs", 20, MaxScavsDesc);
            Plugin.CustomsMaxScavs = _customsMaxScavs.Value;
            _customsMaxScavs.SettingChanged += ABPS_SettingChanged;

            _factoryMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Factory", 10, MaxScavsDesc);
            Plugin.FactoryMaxScavs = _factoryMaxScavs.Value;
            _factoryMaxScavs.SettingChanged += ABPS_SettingChanged;

            _interchangeMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Interchange", 22, MaxScavsDesc);
            Plugin.InterchangeMaxScavs = _interchangeMaxScavs.Value;
            _interchangeMaxScavs.SettingChanged += ABPS_SettingChanged;

            _labsMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Labs", 18, MaxScavsDesc);
            Plugin.LabsMaxScavs = _labsMaxScavs.Value;
            _labsMaxScavs.SettingChanged += ABPS_SettingChanged;

            _lighthouseMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Lighthouse", 20, MaxScavsDesc);
            Plugin.LighthouseMaxScavs = _lighthouseMaxScavs.Value;
            _lighthouseMaxScavs.SettingChanged += ABPS_SettingChanged;

            _reserveMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Reserve", 22, MaxScavsDesc);
            Plugin.ReserveMaxScavs = _reserveMaxScavs.Value;
            _reserveMaxScavs.SettingChanged += ABPS_SettingChanged;

            _groundZeroMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Ground Zero", 14, MaxScavsDesc);
            Plugin.GroundZeroMaxScavs = _groundZeroMaxScavs.Value;
            _groundZeroMaxScavs.SettingChanged += ABPS_SettingChanged;

            _groundZeroHighMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Ground Zero High", 14, MaxScavsDesc);
            Plugin.GroundZeroHighMaxScavs = _groundZeroHighMaxScavs.Value;
            _groundZeroHighMaxScavs.SettingChanged += ABPS_SettingChanged;

            _shorelineMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Shoreline", 24, MaxScavsDesc);
            Plugin.ShorelineMaxScavs = _shorelineMaxScavs.Value;
            _shorelineMaxScavs.SettingChanged += ABPS_SettingChanged;

            _streetsMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Streets", 18, MaxScavsDesc);
            Plugin.StreetsMaxScavs = _streetsMaxScavs.Value;
            _streetsMaxScavs.SettingChanged += ABPS_SettingChanged;

            _woodsMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Woods", 24, MaxScavsDesc);
            Plugin.WoodsMaxScavs = _woodsMaxScavs.Value;
            _woodsMaxScavs.SettingChanged += ABPS_SettingChanged;

            _labyrinthMaxScavs = BindMaxScav(config, "Max Scavs Per Raid - Labyrinth", 12, MaxScavsDesc);
            Plugin.LabyrinthMaxScavs = _labyrinthMaxScavs.Value;
            _labyrinthMaxScavs.SettingChanged += ABPS_SettingChanged;

            
            // Boss stuff
            _regressiveChances = config.Bind(
                BossConfig,
                "Regressive Boss Chances",
                false,
                new ConfigDescription("If a boss spawned in the previous raid, lower their chance by the Step Count.\nChanges do not take effect until next raid.",
                    null,
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.RegressiveChances = _regressiveChances.Value;
            _regressiveChances.SettingChanged += ABPS_SettingChanged;

            _progressiveChances = config.Bind(
                BossConfig,
                "Progressive Boss Chances",
                false,
                new ConfigDescription("If a boss didn't spawn in the previous raid, raise their chance by the Step Count.\nChanges do not take effect until next raid.",
                null,
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ProgressiveChances = _progressiveChances.Value;
            _progressiveChances.SettingChanged += ABPS_SettingChanged;

            _chanceStep = config.Bind(
                BossConfig,
                "Step Count",
                5,
                new ConfigDescription("Progressive: If a boss fails to spawn, how much to increase their spawn chance by.\n\nRegressive: The spawn chance is decreased by this amount if they spawned last raid.\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 15),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ChanceStep = _chanceStep.Value;
            _chanceStep.SettingChanged += ABPS_SettingChanged;

            _minimumChance = config.Bind(
                BossConfig,
                "Minimum Chance",
                5,
                new ConfigDescription("If only Progressive Chances are enabled, a boss that spawns will reset to this value.\n\nIf Regressive Chances are enabled, spawn chance instead decays toward this value if they spawned in the previous raid.\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(1, 25),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.MinimumChance = _minimumChance.Value;
            _minimumChance.SettingChanged += ABPS_SettingChanged;

            _maximumChance = config.Bind(
                BossConfig,
                "Maximum Chance",
                100,
                new ConfigDescription("The highest value a boss's spawn chance can reach when progressive chances are enabled.\nChanges do not take effect until next raid.",
                new AcceptableValueRange<int>(25, 100),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.MaximumChance = _maximumChance.Value;
            _maximumChance.SettingChanged += ABPS_SettingChanged;

            // PMC Settings
            _pmcSpawnAnywhere = config.Bind(
                PmcConfig,
                "Allow PMC Spawn Anywhere",
                false,
                new ConfigDescription("Enable this if you want PMCs to spawn at any spawn point instead of Player Spawn points.\nNote that with this disabled, PMCs will still spawn anywhere if there are no player spawn points available.",
                    null,
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcSpawnAnywhere = _pmcSpawnAnywhere.Value;
            _pmcSpawnAnywhere.SettingChanged += ABPS_SettingChanged;
            
            _customsPmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Customs", 
                100f, 
                new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.", 
                new AcceptableValueRange<float>(10f, 175f), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.CustomsPmcSpawnDistanceCheck = _customsPmcSpawnDistanceCheck.Value;
            _customsPmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _factoryPmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Factory", 
                30f, new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.", 
                new AcceptableValueRange<float>(10f, 175f), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.FactoryPmcSpawnDistanceCheck = _factoryPmcSpawnDistanceCheck.Value;
            _factoryPmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _interchangePmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Interchange",
                125f, new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.", 
                new AcceptableValueRange<float>(10f, 175f), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.InterchangePmcSpawnDistanceCheck = _interchangePmcSpawnDistanceCheck.Value;
            _interchangePmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _labsPmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Labs", 
                40f, new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.", 
                new AcceptableValueRange<float>(10f, 175f), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.LabsPmcSpawnDistanceCheck = _labsPmcSpawnDistanceCheck.Value;
            _labsPmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _lighthousePmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Lighthouse",
                125f, new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.", 
                new AcceptableValueRange<float>(10f, 175f), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.LighthousePmcSpawnDistanceCheck = _lighthousePmcSpawnDistanceCheck.Value;
            _lighthousePmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _reservePmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Reserve",
                90f, new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.", 
                new AcceptableValueRange<float>(10f, 175f), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ReservePmcSpawnDistanceCheck = _reservePmcSpawnDistanceCheck.Value;
            _reservePmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;
            
            _groundZeroPmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - GroundZero",
                85f, new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.", 
                new AcceptableValueRange<float>(10f, 175f), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.GroundZeroPmcSpawnDistanceCheck = _groundZeroPmcSpawnDistanceCheck.Value;
            _groundZeroPmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _shorelinePmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Shoreline", 
                130f, new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.", 
                new AcceptableValueRange<float>(10f, 175f), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ShorelinePmcSpawnDistanceCheck = _shorelinePmcSpawnDistanceCheck.Value;
            _shorelinePmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _streetsPmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Streets", 
                120f, new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.", 
                new AcceptableValueRange<float>(10f, 175f), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.StreetsPmcSpawnDistanceCheck = _streetsPmcSpawnDistanceCheck.Value;
            _streetsPmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _woodsPmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Woods",
                150f,
                new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.",
                new AcceptableValueRange<float>(10f, 175f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.WoodsPmcSpawnDistanceCheck = _woodsPmcSpawnDistanceCheck.Value;
            _woodsPmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _labyrinthPmcSpawnDistanceCheck = config.Bind(
                PmcConfig,
                "Distance Limit - Labyrinth",
                20f,
                new ConfigDescription("How far all PMCs must be from a spawn point for it to be enabled for other PMC spawns.\n Setting this too high will cause PMCs to fail to spawn.",
                new AcceptableValueRange<float>(10f, 175f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.LabyrinthPmcSpawnDistanceCheck = _labyrinthPmcSpawnDistanceCheck.Value;
            _labyrinthPmcSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;


            // Scav Settings

            _softCap = config.Bind(
                ScavConfig, 
                "Scav Soft Cap", 
                3, 
                new ConfigDescription("How many open slots before hard cap to stop spawning additional scavs.\nEx..If 3, and map cap is 23 - will stop spawning scavs at 20 total.\nThis allows PMC waves if enabled to fill the remaining spots.", 
                new AcceptableValueRange<int>(0, 10), 
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.SoftCap = _softCap.Value;
            _softCap.SettingChanged += ABPS_SettingChanged;

            _pScavChance = config.Bind(
                ScavConfig, 
                "PScav Chance", 
                20, 
                new ConfigDescription("How likely a scav spawning later in the raid is a Player Scav.",
                new AcceptableValueRange<int>(0, 100),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PScavChance = _pScavChance.Value;
            _pScavChance.SettingChanged += ABPS_SettingChanged;

            _zoneScavCap = config.Bind(
                ScavConfig,
                "Zone Cap",
                2,
                new ConfigDescription("How many scavs can spawn in any one zone (excluding Factory/Ground Zero).",
                new AcceptableValueRange<int>(0, 15),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ZoneScavCap = _zoneScavCap.Value;
            _zoneScavCap.SettingChanged += ABPS_SettingChanged;

            _enableHotzones = config.Bind(
                ScavConfig,
                "Hotzones",
                false,
                new ConfigDescription("Enables hotzones around maps, more common or quest areas are considered hotzones.",
                null,
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.EnableHotzones = _enableHotzones.Value;
            _enableHotzones.SettingChanged += ABPS_SettingChanged;

            _hotzoneScavCap = config.Bind(
                ScavConfig,
                "Hotzone Cap",
                4,
                new ConfigDescription("How many scavs can spawn in a hotzone, if you enable hotzones (excluding Factory/Ground Zero).",
                new AcceptableValueRange<int>(0, 15),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.HotzoneScavCap = _hotzoneScavCap.Value;
            _hotzoneScavCap.SettingChanged += ABPS_SettingChanged;

            _hotzoneScavChance = config.Bind(
                ScavConfig,
                "Hotzone Chance",
                20,
                new ConfigDescription("How likely a scav is to spawn in a hotzone (excluding Factory/Ground Zero).",
                new AcceptableValueRange<int>(0, 100),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.HotzoneScavChance = _hotzoneScavChance.Value;
            _hotzoneScavChance.SettingChanged += ABPS_SettingChanged;

            _customsScavSpawnDistanceCheck = config.Bind(
                ScavConfig, 
                "Distance Limit - Customs", 
                45f, 
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(5f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.CustomsScavSpawnDistanceCheck = _customsScavSpawnDistanceCheck.Value;
            _customsScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _factoryScavSpawnDistanceCheck = config.Bind(
                ScavConfig, 
                "Distance Limit - Factory", 
                30f, 
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(5f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.FactoryScavSpawnDistanceCheck = _factoryScavSpawnDistanceCheck.Value;
            _factoryScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _interchangeScavSpawnDistanceCheck = config.Bind(
                ScavConfig,
                "Distance Limit - Interchange",
                45f, 
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(1f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.InterchangeScavSpawnDistanceCheck = _interchangeScavSpawnDistanceCheck.Value;
            _interchangeScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _labsScavSpawnDistanceCheck = config.Bind(
                ScavConfig,
                "Distance Limit - Labs", 
                40f, 
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(1f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.LabsScavSpawnDistanceCheck = _labsScavSpawnDistanceCheck.Value;
            _labsScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _lighthouseScavSpawnDistanceCheck = config.Bind
                (ScavConfig,
                "Distance Limit - Lighthouse",
                45f, 
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(1f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.LighthouseScavSpawnDistanceCheck = _lighthouseScavSpawnDistanceCheck.Value;
            _lighthouseScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _reserveScavSpawnDistanceCheck = config.Bind(
                ScavConfig,
                "Distance Limit - Reserve",
                45f, 
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(1f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ReserveScavSpawnDistanceCheck = _reserveScavSpawnDistanceCheck.Value;
            _reserveScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _groundZeroScavSpawnDistanceCheck = config.Bind(
                ScavConfig,
                "Distance Limit - GroundZero",
                45f, 
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(1f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.GroundZeroScavSpawnDistanceCheck = _groundZeroScavSpawnDistanceCheck.Value;
            _groundZeroScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _shorelineScavSpawnDistanceCheck = config.Bind(
                ScavConfig,
                "Distance Limit - Shoreline",
                45f, 
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(1f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ShorelineScavSpawnDistanceCheck = _shorelineScavSpawnDistanceCheck.Value;
            _shorelineScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _streetsScavSpawnDistanceCheck = config.Bind(
                ScavConfig,
                "Distance Limit - Streets",
                45f, 
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(1f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.StreetsScavSpawnDistanceCheck = _streetsScavSpawnDistanceCheck.Value;
            _streetsScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _woodsScavSpawnDistanceCheck = config.Bind(
                ScavConfig,
                "Distance Limit - Woods",
                45f,
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(1f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.WoodsScavSpawnDistanceCheck = _woodsScavSpawnDistanceCheck.Value;
            _woodsScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;

            _labyrinthScavSpawnDistanceCheck = config.Bind(
                ScavConfig,
                "Distance Limit - Woods",
                45f,
                new ConfigDescription("How far PMCs must be from a spawn point for it to be enabled for Scav spawns.\n Setting this too high will cause Scavs to fail to spawn.",
                new AcceptableValueRange<float>(1f, 50f),
                new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.LabyrinthScavSpawnDistanceCheck = _labyrinthScavSpawnDistanceCheck.Value;
            _labyrinthScavSpawnDistanceCheck.SettingChanged += ABPS_SettingChanged;
            
            
            // Debug
            _enableDebug = config.Bind(
                DebugConfig,
                "Enable Debug Logging",
                false,
                new ConfigDescription("Enabling this will turn on Bepinex logging for debug.",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true, Order = _loadOrder-- }));
            Plugin.DebugLogging = _enableDebug.Value;
            _enableDebug.SettingChanged += ABPS_SettingChanged;
            
            _directionalBias = config.Bind(
                GeneralConfig,
                "Directional Bias",
                0.8f,
                new ConfigDescription("How much spawns favor the direction you're traveling. 0 = distance only, 0.5 = moderate bias, 1.0 = strong bias. Spawns ahead of you are favored, behind deprioritized.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.DirectionalBias = _directionalBias.Value;
            _directionalBias.SettingChanged += ABPS_SettingChanged;
            
            _shufflePercent = config.Bind(
                GeneralConfig,
                "Shuffle Percent",
                0.6f,
                new ConfigDescription("What portion of the ahead-sorted spawn list gets loosely shuffled. 0.5 = top 50%, 1.0 = entire list.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ShufflePercent = _shufflePercent.Value;
            _shufflePercent.SettingChanged += ABPS_SettingChanged;
            
            _shuffleStep = config.Bind(
                GeneralConfig,
                "Shuffle Step",
                2,
                new ConfigDescription("Every Nth element in the shuffle zone gets swapped with a random element from outside it. Lower = more shuffling.",
                    new AcceptableValueRange<int>(2, 10),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ShuffleStep = _shuffleStep.Value;
            _shuffleStep.SettingChanged += ABPS_SettingChanged;

            _scavSpawnNoise = config.Bind(
                GeneralConfig,
                "Scav Spawn Noise",
                100f,
                new ConfigDescription("Random distance noise (meters) added to scav spawn scoring. Higher = scavs spread more, less predictable.",
                    new AcceptableValueRange<float>(0f, 500f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ScavSpawnNoise = _scavSpawnNoise.Value;
            _scavSpawnNoise.SettingChanged += ABPS_SettingChanged;

            _pmcSpawnNoise = config.Bind(
                GeneralConfig,
                "PMC Spawn Noise",
                200f,
                new ConfigDescription("Random distance noise (meters) added to PMC spawn scoring. Higher = PMCs spread across the map, less likely to spawn near you.",
                    new AcceptableValueRange<float>(0f, 1000f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcSpawnNoise = _pmcSpawnNoise.Value;
            _pmcSpawnNoise.SettingChanged += ABPS_SettingChanged;

            _pmcSkipClosestPercent = config.Bind(
                GeneralConfig,
                "PMC Skip Closest Percent",
                0.15f,
                new ConfigDescription("Skip this fraction of the closest valid spawn points when picking a PMC spawn. 0.15 = never use closest 15%.",
                    new AcceptableValueRange<float>(0f, 0.5f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcSkipClosestPercent = _pmcSkipClosestPercent.Value;
            _pmcSkipClosestPercent.SettingChanged += ABPS_SettingChanged;

            _perPlayerScavMultiplier = config.Bind(
                GeneralConfig,
                "Per-Player Scav Multiplier",
                0.3f,
                new ConfigDescription("How much each extra player past the first scales the scav spawn budget.\n0.0 = same budget for any player count.\n0.2 = 5 players get 1.8x the solo budget (recommended).\n1.0 = vanilla acid behaviour: 5 players get 5x the solo budget.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PerPlayerScavMultiplier = _perPlayerScavMultiplier.Value;
            _perPlayerScavMultiplier.SettingChanged += ABPS_SettingChanged;

            _scavSkipClosestPercent = config.Bind(
                GeneralConfig,
                "Scav Skip Closest Percent",
                0.1f,
                new ConfigDescription("Skip this fraction of the closest valid spawn points when picking a scav spawn. 0.1 = never use closest 10%.",
                    new AcceptableValueRange<float>(0f, 0.5f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ScavSkipClosestPercent = _scavSkipClosestPercent.Value;
            _scavSkipClosestPercent.SettingChanged += ABPS_SettingChanged;

            _hotzoneScoreMultiplier = config.Bind(
                GeneralConfig,
                "Hotzone Score Multiplier",
                0.7f,
                new ConfigDescription("Score multiplier applied to spawn points inside a map hotzone. < 1.0 favors hotzones (lower score wins). 1.0 disables the bias.\nRequires 'Enable Hotzones' to be on.",
                    new AcceptableValueRange<float>(0.1f, 1.5f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.HotzoneScoreMultiplier = _hotzoneScoreMultiplier.Value;
            _hotzoneScoreMultiplier.SettingChanged += ABPS_SettingChanged;

            _scavScheduleStartPercent = config.Bind(
                GeneralConfig,
                "Scav Schedule Start Budget Pct",
                0.2f,
                new ConfigDescription("Fraction of the per-player scav budget unlocked at raid start. Ensures the map fills with some scavs immediately.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ScavScheduleStartPercent = _scavScheduleStartPercent.Value;
            _scavScheduleStartPercent.SettingChanged += ABPS_SettingChanged;

            _scavScheduleMidTimePercent = config.Bind(
                GeneralConfig,
                "Scav Schedule Mid Time Pct",
                0.30f,
                new ConfigDescription("Raid time (as fraction of BotStart..BotStop) at which the mid budget point is hit.",
                    new AcceptableValueRange<float>(0.05f, 0.95f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ScavScheduleMidTimePercent = _scavScheduleMidTimePercent.Value;
            _scavScheduleMidTimePercent.SettingChanged += ABPS_SettingChanged;

            _scavScheduleMidBudgetPercent = config.Bind(
                GeneralConfig,
                "Scav Schedule Mid Budget Pct",
                0.50f,
                new ConfigDescription("Fraction of the per-player scav budget unlocked at the mid time point. Should be > Start, < 1.0.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ScavScheduleMidBudgetPercent = _scavScheduleMidBudgetPercent.Value;
            _scavScheduleMidBudgetPercent.SettingChanged += ABPS_SettingChanged;

            _scavScheduleFullPercent = config.Bind(
                GeneralConfig,
                "Scav Schedule Full Time Pct",
                0.70f,
                new ConfigDescription("Raid time (as fraction of BotStart..BotStop) at which 100% of the budget is unlocked. Beyond this point, normal cap applies.",
                    new AcceptableValueRange<float>(0.1f, 1f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.ScavScheduleFullPercent = _scavScheduleFullPercent.Value;
            _scavScheduleFullPercent.SettingChanged += ABPS_SettingChanged;

            // PMC tick (client-driven wave PMCs) — same curve shape as scavs but with separate values.
            _pmcScheduleStartPercent = config.Bind(
                GeneralConfig,
                "PMC Schedule Start Budget Pct",
                0.20f,
                new ConfigDescription("Fraction of the per-map PMC budget unlocked at raid start. Higher = more PMCs available early.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcScheduleStartPercent = _pmcScheduleStartPercent.Value;
            _pmcScheduleStartPercent.SettingChanged += ABPS_SettingChanged;

            _pmcScheduleMidTimePercent = config.Bind(
                GeneralConfig,
                "PMC Schedule Mid Time Pct",
                0.40f,
                new ConfigDescription("Raid time (as fraction of BotStart..BotStop) at which the mid PMC budget point is hit.",
                    new AcceptableValueRange<float>(0.05f, 0.95f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcScheduleMidTimePercent = _pmcScheduleMidTimePercent.Value;
            _pmcScheduleMidTimePercent.SettingChanged += ABPS_SettingChanged;

            _pmcScheduleMidBudgetPercent = config.Bind(
                GeneralConfig,
                "PMC Schedule Mid Budget Pct",
                0.60f,
                new ConfigDescription("Fraction of the per-map PMC budget unlocked at the mid time point. Should be greater than Start.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcScheduleMidBudgetPercent = _pmcScheduleMidBudgetPercent.Value;
            _pmcScheduleMidBudgetPercent.SettingChanged += ABPS_SettingChanged;

            _pmcScheduleFullPercent = config.Bind(
                GeneralConfig,
                "PMC Schedule Full Time Pct",
                0.70f,
                new ConfigDescription("Raid time (as fraction of BotStart..BotStop) at which 100% of the PMC budget is unlocked.",
                    new AcceptableValueRange<float>(0.1f, 1f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcScheduleFullPercent = _pmcScheduleFullPercent.Value;
            _pmcScheduleFullPercent.SettingChanged += ABPS_SettingChanged;

            _pmcSpawnAttemptInterval = config.Bind(
                GeneralConfig,
                "PMC Spawn Attempt Interval",
                45f,
                new ConfigDescription("Minimum seconds between PMC spawn attempts. Lower = more frequent attempts (still gated by curve + cap).",
                    new AcceptableValueRange<float>(10f, 300f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcSpawnAttemptInterval = _pmcSpawnAttemptInterval.Value;
            _pmcSpawnAttemptInterval.SettingChanged += ABPS_SettingChanged;

            _pmcGroupChance = config.Bind(
                GeneralConfig,
                "PMC Group Chance",
                20,
                new ConfigDescription("Percent chance a PMC spawn rolls as a group (vs solo).",
                    new AcceptableValueRange<int>(0, 100),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcGroupChance = _pmcGroupChance.Value;
            _pmcGroupChance.SettingChanged += ABPS_SettingChanged;

            _pmcMaxGroupSize = config.Bind(
                GeneralConfig,
                "PMC Max Group Size",
                2,
                new ConfigDescription("Max additional PMCs in a group (total group = 1 + this).",
                    new AcceptableValueRange<int>(1, 5),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcMaxGroupSize = _pmcMaxGroupSize.Value;
            _pmcMaxGroupSize.SettingChanged += ABPS_SettingChanged;

            _usecChancePercent = config.Bind(
                GeneralConfig,
                "USEC Chance Percent",
                50,
                new ConfigDescription("Percent chance a PMC spawn is USEC (vs BEAR).",
                    new AcceptableValueRange<int>(0, 100),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.UsecChancePercent = _usecChancePercent.Value;
            _usecChancePercent.SettingChanged += ABPS_SettingChanged;

            _pmcStartDelaySeconds = config.Bind(
                GeneralConfig,
                "PMC Start Delay Seconds",
                60f,
                new ConfigDescription("Grace period at raid start before any wave PMCs are considered, so starting PMCs play out first.",
                    new AcceptableValueRange<float>(0f, 600f),
                    new ConfigurationManagerAttributes { Order = _loadOrder-- }));
            Plugin.PmcStartDelaySeconds = _pmcStartDelaySeconds.Value;
            _pmcStartDelaySeconds.SettingChanged += ABPS_SettingChanged;
        }
        private static void ABPS_SettingChanged(object sender, EventArgs e)
        {
            Plugin.DespawnFurthest = _despawnFurthest.Value;
            Plugin.DespawnDistance = _despawnDistance.Value;
            
            Plugin.CustomsMapLimit = _customsMapLimit.Value;
            Plugin.FactoryMapLimit = _factoryMapLimit.Value;
            Plugin.InterchangeMapLimit = _interchangeMapLimit.Value;
            Plugin.LabsMapLimit = _labsMapLimit.Value;
            Plugin.LighthouseMapLimit = _lighthouseMapLimit.Value;
            Plugin.ReserveMapLimit = _reserveMapLimit.Value;
            Plugin.GroundZeroMapLimit = _groundZeroMapLimit.Value;
            Plugin.ShorelineMapLimit = _shorelineMapLimit.Value;
            Plugin.StreetsMapLimit = _streetsMapLimit.Value;
            Plugin.WoodsMapLimit = _woodsMapLimit.Value;
            Plugin.LabyrinthMapLimit = _labyrinthMapLimit.Value;

            Plugin.CustomsMaxPmcs = _customsMaxPmcs.Value;
            Plugin.FactoryMaxPmcs = _factoryMaxPmcs.Value;
            Plugin.InterchangeMaxPmcs = _interchangeMaxPmcs.Value;
            Plugin.LabsMaxPmcs = _labsMaxPmcs.Value;
            Plugin.LighthouseMaxPmcs = _lighthouseMaxPmcs.Value;
            Plugin.ReserveMaxPmcs = _reserveMaxPmcs.Value;
            Plugin.GroundZeroMaxPmcs = _groundZeroMaxPmcs.Value;
            Plugin.GroundZeroHighMaxPmcs = _groundZeroHighMaxPmcs.Value;
            Plugin.ShorelineMaxPmcs = _shorelineMaxPmcs.Value;
            Plugin.StreetsMaxPmcs = _streetsMaxPmcs.Value;
            Plugin.WoodsMaxPmcs = _woodsMaxPmcs.Value;
            Plugin.LabyrinthMaxPmcs = _labyrinthMaxPmcs.Value;

            Plugin.CustomsMaxScavs = _customsMaxScavs.Value;
            Plugin.FactoryMaxScavs = _factoryMaxScavs.Value;
            Plugin.InterchangeMaxScavs = _interchangeMaxScavs.Value;
            Plugin.LabsMaxScavs = _labsMaxScavs.Value;
            Plugin.LighthouseMaxScavs = _lighthouseMaxScavs.Value;
            Plugin.ReserveMaxScavs = _reserveMaxScavs.Value;
            Plugin.GroundZeroMaxScavs = _groundZeroMaxScavs.Value;
            Plugin.GroundZeroHighMaxScavs = _groundZeroHighMaxScavs.Value;
            Plugin.ShorelineMaxScavs = _shorelineMaxScavs.Value;
            Plugin.StreetsMaxScavs = _streetsMaxScavs.Value;
            Plugin.WoodsMaxScavs = _woodsMaxScavs.Value;
            Plugin.LabyrinthMaxScavs = _labyrinthMaxScavs.Value;

            Plugin.RegressiveChances = _regressiveChances.Value;
            Plugin.ProgressiveChances = _progressiveChances.Value;
            Plugin.ChanceStep = _chanceStep.Value;
            Plugin.MinimumChance = _minimumChance.Value;
            Plugin.MaximumChance = _maximumChance.Value;

            Plugin.PmcSpawnAnywhere = _pmcSpawnAnywhere.Value;
            Plugin.CustomsPmcSpawnDistanceCheck = _customsPmcSpawnDistanceCheck.Value;
            Plugin.FactoryPmcSpawnDistanceCheck = _factoryPmcSpawnDistanceCheck.Value;
            Plugin.InterchangePmcSpawnDistanceCheck = _interchangePmcSpawnDistanceCheck.Value;
            Plugin.LabsPmcSpawnDistanceCheck = _labsPmcSpawnDistanceCheck.Value;
            Plugin.LighthousePmcSpawnDistanceCheck = _lighthousePmcSpawnDistanceCheck.Value;
            Plugin.ReservePmcSpawnDistanceCheck = _reservePmcSpawnDistanceCheck.Value;
            Plugin.GroundZeroPmcSpawnDistanceCheck = _groundZeroPmcSpawnDistanceCheck.Value;
            Plugin.ShorelinePmcSpawnDistanceCheck = _shorelinePmcSpawnDistanceCheck.Value;
            Plugin.StreetsPmcSpawnDistanceCheck = _streetsPmcSpawnDistanceCheck.Value;
            Plugin.WoodsPmcSpawnDistanceCheck = _woodsPmcSpawnDistanceCheck.Value;
            Plugin.LabyrinthPmcSpawnDistanceCheck = _labyrinthPmcSpawnDistanceCheck.Value;

            Plugin.SoftCap = _softCap.Value;
            Plugin.PScavChance = _pScavChance.Value;
            Plugin.EnableHotzones = _enableHotzones.Value;
            Plugin.ZoneScavCap = _zoneScavCap.Value;
            Plugin.HotzoneScavCap = _hotzoneScavCap.Value;
            Plugin.CustomsScavSpawnDistanceCheck = _customsScavSpawnDistanceCheck.Value;
            Plugin.FactoryScavSpawnDistanceCheck = _factoryScavSpawnDistanceCheck.Value;
            Plugin.InterchangeScavSpawnDistanceCheck = _interchangeScavSpawnDistanceCheck.Value;
            Plugin.LabsScavSpawnDistanceCheck = _labsScavSpawnDistanceCheck.Value;
            Plugin.LighthouseScavSpawnDistanceCheck = _lighthouseScavSpawnDistanceCheck.Value;
            Plugin.ReserveScavSpawnDistanceCheck = _reserveScavSpawnDistanceCheck.Value;
            Plugin.GroundZeroScavSpawnDistanceCheck = _groundZeroScavSpawnDistanceCheck.Value;
            Plugin.ShorelineScavSpawnDistanceCheck = _shorelineScavSpawnDistanceCheck.Value;
            Plugin.StreetsScavSpawnDistanceCheck = _streetsScavSpawnDistanceCheck.Value;
            Plugin.WoodsScavSpawnDistanceCheck = _woodsScavSpawnDistanceCheck.Value;
            Plugin.LabyrinthScavSpawnDistanceCheck = _labyrinthScavSpawnDistanceCheck.Value;
            
            Plugin.DebugLogging = _enableDebug.Value;
            Plugin.DirectionalBias = _directionalBias.Value;
            Plugin.ShufflePercent = _shufflePercent.Value;
            Plugin.ShuffleStep = _shuffleStep.Value;
            Plugin.ScavSpawnNoise = _scavSpawnNoise.Value;
            Plugin.PmcSpawnNoise = _pmcSpawnNoise.Value;
            Plugin.PmcSkipClosestPercent = _pmcSkipClosestPercent.Value;
            Plugin.ScavSkipClosestPercent = _scavSkipClosestPercent.Value;
            Plugin.PerPlayerScavMultiplier = _perPlayerScavMultiplier.Value;
            Plugin.HotzoneScoreMultiplier = _hotzoneScoreMultiplier.Value;
            Plugin.ScavScheduleStartPercent = _scavScheduleStartPercent.Value;
            Plugin.ScavScheduleMidTimePercent = _scavScheduleMidTimePercent.Value;
            Plugin.ScavScheduleMidBudgetPercent = _scavScheduleMidBudgetPercent.Value;
            Plugin.ScavScheduleFullPercent = _scavScheduleFullPercent.Value;

            Plugin.PmcScheduleStartPercent = _pmcScheduleStartPercent.Value;
            Plugin.PmcScheduleMidTimePercent = _pmcScheduleMidTimePercent.Value;
            Plugin.PmcScheduleMidBudgetPercent = _pmcScheduleMidBudgetPercent.Value;
            Plugin.PmcScheduleFullPercent = _pmcScheduleFullPercent.Value;
            Plugin.PmcSpawnAttemptInterval = _pmcSpawnAttemptInterval.Value;
            Plugin.PmcGroupChance = _pmcGroupChance.Value;
            Plugin.PmcMaxGroupSize = _pmcMaxGroupSize.Value;
            Plugin.UsecChancePercent = _usecChancePercent.Value;
            Plugin.PmcStartDelaySeconds = _pmcStartDelaySeconds.Value;
        }
    }
}
