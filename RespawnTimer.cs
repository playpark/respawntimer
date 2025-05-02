using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Logging;
using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text.Json;
using System.IO;

namespace RespawnTimer;
public class RespawnTimer : BasePlugin
{
    public override string ModuleName => "RespawnTimer";
    public override string ModuleDescription => "Respawn Timer for Minigames with per-map settings";

    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "dollan";

    public bool DoRespawn = false;
    public new List<CSTimer> Timers = [];
    public const int DefaultRespawnTime = 45;
    public int MapRespawnTime = DefaultRespawnTime;

    public class MapSettings
    {
        public int RespawnTime { get; set; } = 45;
    }

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("[RespawnTimer] Plugin loaded");

        // Register command listeners
        AddCommandListener("css_respawn", OnRespawnCommand, HookMode.Pre);
        AddCommandListener("css_r", OnRespawnCommand, HookMode.Pre);
    }

    [GameEventHandler]
    public HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Logger.LogInformation("[RespawnTimer] RoundStart event triggered");
        
        // Get map name and load respawn time
        string rawMapName = Server.MapName ?? "";
        string cleanMapName = rawMapName.ToLower().Replace(".bsp", "");
        MapRespawnTime = LoadRespawnTimeForMap(cleanMapName);

        Logger.LogInformation($"[RespawnTimer] Loaded respawn time for map '{cleanMapName}': {MapRespawnTime}");

        // Reset respawn timers
        foreach (var timer in Timers)
        {
            timer.Kill();
        }
        Timers.Clear();

        DoRespawn = true;
        Logger.LogInformation($"[RespawnTimer] Respawn enabled for {MapRespawnTime} seconds");

        var new_timer = AddTimer(MapRespawnTime, DisableRespawn);
        Timers.Add(new_timer);

        return HookResult.Continue;
    }

    // Handle respawn command - this is critical for SharpTimer compatibility
    [GameEventHandler]
    private HookResult OnRespawnCommand(CCSPlayerController? player, CommandInfo command)
    {
        // If player is null or not valid, let other plugins handle it
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // If player is alive, let SharpTimer handle the restart (teleport to restart point)
        if (player.PawnIsAlive)
            return HookResult.Continue;

        // If player is dead and respawn is disabled, block the command
        if (!DoRespawn)
        {
            player.PrintToChat(StringExtensions.ReplaceColorTags("{Lime}[Minigames] {Red}Respawning is currently disabled!"));
            return HookResult.Stop;
        }

        // If player is dead and respawn is enabled, let SharpTimer handle it
        // It will teleport them to the correct restart point
        return HookResult.Continue;
    }

    private int LoadRespawnTimeForMap(string mapName)
    {
        string pluginDirectory = ModuleDirectory;
        string configPath = Path.Combine(pluginDirectory, "MapSettings", $"{mapName}.json");

        Logger.LogInformation($"[RespawnTimer] Looking for config file: {configPath}");

        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var settings = JsonSerializer.Deserialize<MapSettings>(json);
                if (settings != null)
                {
                    Logger.LogInformation($"[RespawnTimer] Loaded respawn time for map '{mapName}': {settings.RespawnTime} seconds");
                    return settings.RespawnTime;
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"[RespawnTimer] Error loading config for '{mapName}': {e.Message}");
                return DefaultRespawnTime;
            }
        }
        else
        {
            Logger.LogWarning($"[RespawnTimer] No config file found for map '{mapName}', using default {DefaultRespawnTime} seconds");
            return DefaultRespawnTime;
        }

        return DefaultRespawnTime;
    }

    public void DisableRespawn()
    {
        DoRespawn = false;
        Logger.LogInformation("[RespawnTimer] Respawn disabled");
        Server.PrintToChatAll(StringExtensions.ReplaceColorTags("{Lime}[Minigames] {Red}Respawn disabled"));
    }

    [GameEventHandler]
    public HookResult PlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (DoRespawn)
        {
            CCSPlayerController? user = @event.Userid;
            if (user != null)
            {
                var new_timer = AddTimer(1f, () =>
                {
                    user.Respawn();
                    user.PrintToChat(StringExtensions.ReplaceColorTags("{Lime}Respawned"));
                });
                Timers.Add(new_timer);
            }
        }
        return HookResult.Continue;
    }
}