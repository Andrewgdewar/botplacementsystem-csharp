using System.Reflection;
using System.Text.Json;
using _botplacementsystem.Controllers;
using _botplacementsystem.Models;
using _botplacementsystem.Models.Enums;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Loaders;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _botplacementsystem.Globals;

[Injectable (InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader)]
public class ModConfig : IOnLoad
{
    private static JsonUtil _jsonUtil;
    private static FileUtil _fileUtil;
    private static MapSpawns _mapSpawns;
    
    public static MapZoneDefaults? MapZoneDefaults { get; private set; }
    public static BossWaveDefaults? BossWaveDefaults { get; private set; }
    public static PmcDefaults? PmcDefaults { get; private set; }
    public static ScavDefaults? ScavDefaults { get; private set; }
    public static HostilityDefaults? HostilityDefaults { get; private set; }
    /// <summary>Pool of main bosses used by the boss-rotation preset (Injections/MainBosses.json).</summary>
    public static List<BossLocationSpawn>? MainBossRotationPool { get; private set; }
    public static Dictionary<string, BossLocationSpawn>? InjectionExamples { get; private set; }
    public static AbpsConfig Config {get; private set;} = null!;
    public static AbpsConfig OriginalConfig {get; private set;} = null!;
    
    private static int _isActivelyProcessingFlag = 0;
    
    public static string? _modPath;

    public ModConfig(
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        ModHelper modHelper,
        MapSpawns mapSpawns
    )
    {
        _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _jsonUtil = jsonUtil;
        _fileUtil = fileUtil;
        _mapSpawns = mapSpawns;
    }
    
    public async Task OnLoad()
    {
        Config = await _jsonUtil.DeserializeFromFileAsync<AbpsConfig>(_modPath + "/config.json") ?? throw new ArgumentNullException();
        OriginalConfig = await _jsonUtil.DeserializeFromFileAsync<AbpsConfig>(_modPath + "/config.json") ?? throw new ArgumentNullException();
        MapZoneDefaults = await _jsonUtil.DeserializeFromFileAsync<MapZoneDefaults>(_modPath + "/Defaults/MapZones.json") ?? throw new ArgumentNullException();
        BossWaveDefaults = await _jsonUtil.DeserializeFromFileAsync<BossWaveDefaults>(_modPath + "/Defaults/Bosses.json") ?? throw new ArgumentNullException();
        PmcDefaults = await _jsonUtil.DeserializeFromFileAsync<PmcDefaults>(_modPath + "/Defaults/PMCs.json") ?? throw new ArgumentNullException();
        ScavDefaults = await _jsonUtil.DeserializeFromFileAsync<ScavDefaults>(_modPath + "/Defaults/Scavs.json") ?? throw new ArgumentNullException();
        HostilityDefaults = await _jsonUtil.DeserializeFromFileAsync<HostilityDefaults>(_modPath + "/Defaults/Hostility.json") ?? throw new ArgumentNullException();
        MainBossRotationPool = await _jsonUtil.DeserializeFromFileAsync<List<BossLocationSpawn>>(_modPath + "/Injections/MainBosses.json") ?? new List<BossLocationSpawn>();
        InjectionExamples = await _jsonUtil.DeserializeFromFileAsync<Dictionary<string, BossLocationSpawn>>(_modPath + "/Injections/Examples.json") ?? new Dictionary<string, BossLocationSpawn>();
    }
    
    public static async Task<ConfigOperationResult> ReloadConfig()
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
            return ConfigOperationResult.ActiveProcess;

        try
        {
            var configPath = Path.Combine(_modPath, "config.json");
            var configTask = _jsonUtil.DeserializeFromFileAsync<AbpsConfig>(configPath);

            await Task.WhenAll(configTask);

            Config = configTask.Result ?? throw new ArgumentNullException(nameof(Config));
            OriginalConfig = DeepClone(Config);

            await Task.Run(() => _mapSpawns.ConfigureInitialData());

            return ConfigOperationResult.Success;
        }
        catch (Exception ex)
        {
            return ConfigOperationResult.Failure;
        }
        finally
        {
            Interlocked.Exchange(ref _isActivelyProcessingFlag, 0);
        }
    }
    
    public static async Task<ConfigOperationResult> SaveConfig()
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
            return ConfigOperationResult.ActiveProcess;

        try
        {
            var configPath = Path.Combine(_modPath, "config.json");

            var serializedConfigTask = Task.Run(() => _jsonUtil.Serialize(Config, true));
            await Task.WhenAll(serializedConfigTask);

            var writeConfigTask = _fileUtil.WriteFileAsync(configPath, serializedConfigTask.Result!);
            await Task.WhenAll(writeConfigTask);

            await Task.Run(() => _mapSpawns.ConfigureInitialData());
            
            // Update 'Original' config stuff since we've saved so the 'Undo' function works
            OriginalConfig = DeepClone(Config);

            return ConfigOperationResult.Success;
        }
        catch (Exception ex)
        {
            return ConfigOperationResult.Failure;
        }
        finally
        {
            Interlocked.Exchange(ref _isActivelyProcessingFlag, 0);
        }
    }
    
    private static T DeepClone<T>(T source)
    {
        var json = _jsonUtil.Serialize(source);
        return _jsonUtil.Deserialize<T>(json)!;
    }

    /// <summary>
    /// Reverts the active <see cref="Config"/> to a clone of <see cref="OriginalConfig"/>,
    /// undoing any prior preset overlay so the user's saved config is in effect.
    /// </summary>
    public static void RestoreFromOriginal()
    {
        if (OriginalConfig != null)
            Config = DeepClone(OriginalConfig);
    }
}