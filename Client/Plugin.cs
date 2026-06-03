using acidphantasm_botplacementsystem.Patches;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using EFT;

namespace acidphantasm_botplacementsystem
{
    [BepInPlugin("com.acidphantasm.botplacementsystem", "acidphantasm-botplacementsystem", "2.0.18")]
    [BepInDependency("com.fika.headless", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        public static bool DespawnFurthest;
        public static bool DespawnPmcs;
        public static float DespawnDistance;
        public static float DespawnTimer;
        
        public static int CustomsMapLimit;
        public static int FactoryMapLimit;
        public static int InterchangeMapLimit;
        public static int LabsMapLimit;
        public static int LighthouseMapLimit;
        public static int ReserveMapLimit;
        public static int GroundZeroMapLimit;
        public static int ShorelineMapLimit;
        public static int StreetsMapLimit;
        public static int WoodsMapLimit;
        public static int LabyrinthMapLimit;

        public static int CustomsMaxPmcs;
        public static int FactoryMaxPmcs;
        public static int InterchangeMaxPmcs;
        public static int LabsMaxPmcs;
        public static int LighthouseMaxPmcs;
        public static int ReserveMaxPmcs;
        public static int GroundZeroMaxPmcs;
        public static int GroundZeroHighMaxPmcs;
        public static int ShorelineMaxPmcs;
        public static int StreetsMaxPmcs;
        public static int WoodsMaxPmcs;
        public static int LabyrinthMaxPmcs;
        
        public static bool RegressiveChances;
        public static bool ProgressiveChances;
        public static int ChanceStep;
        public static int MinimumChance;
        public static int MaximumChance;

        public static bool PmcSpawnAnywhere;
        public static float CustomsPmcSpawnDistanceCheck;
        public static float FactoryPmcSpawnDistanceCheck;
        public static float InterchangePmcSpawnDistanceCheck;
        public static float LabsPmcSpawnDistanceCheck;
        public static float LighthousePmcSpawnDistanceCheck;
        public static float ReservePmcSpawnDistanceCheck;
        public static float GroundZeroPmcSpawnDistanceCheck;
        public static float ShorelinePmcSpawnDistanceCheck;
        public static float StreetsPmcSpawnDistanceCheck;
        public static float WoodsPmcSpawnDistanceCheck;
        public static float LabyrinthPmcSpawnDistanceCheck;


        public static int SoftCap;
        public static int PScavChance;
        public static int ZoneScavCap;
        public static bool EnableHotzones;
        public static int HotzoneScavCap;
        public static int HotzoneScavChance;
        public static float CustomsScavSpawnDistanceCheck;
        public static float FactoryScavSpawnDistanceCheck;
        public static float InterchangeScavSpawnDistanceCheck;
        public static float LabsScavSpawnDistanceCheck;
        public static float LighthouseScavSpawnDistanceCheck;
        public static float ReserveScavSpawnDistanceCheck;
        public static float GroundZeroScavSpawnDistanceCheck;
        public static float ShorelineScavSpawnDistanceCheck;
        public static float StreetsScavSpawnDistanceCheck;
        public static float WoodsScavSpawnDistanceCheck;
        public static float LabyrinthScavSpawnDistanceCheck;

        public static bool DebugLogging;

        public static float DirectionalBias;
        public static float ShufflePercent;
        public static int ShuffleStep;
        public static float ScavSpawnNoise;
        public static float PmcSpawnNoise;
        public static float PmcSkipClosestPercent;
        public static float PerPlayerScavMultiplier;

        public static BotSpawner BotSpawnerInstance;


        internal void Awake()
        {
            LogSource = Logger;

            /*
             * This patch is only for development purposes in specific scenarios (or it would be in IFDEBUG)
            */
            //new GameworldOnStartedPatch().Enable();

            new BossSpawnScenarioStopPatch().Enable();
            new BotOwnerCreationPatch().Enable();
            new PlayerOnDeadPatch().Enable();
            new MenuLoadPatch().Enable();
            new SetMaxBotCountPatch().Enable();
            new BossSpawnScenarioSpawnProgressPatch().Enable();
            new BossProgressiveRegressivePatch().Enable();
            new PmcSpawnHookPatch().Enable();
            new AssaultGroupPatch().Enable();
            new NonWavesSpawnSystemPatch().Enable();
            new TryToSpawnInZonePatch().Enable();
            new IsPlayerEnemyPatch().Enable();
            new BotsControllerInitPatch().Enable();
            
            AbpsConfig.InitAbpsConfig(Config);
        }
    }
}
