﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Logging;
using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API.Modules.Commands;

namespace RespawnTimer;
public class RespawnTimer : BasePlugin
{
    public override string ModuleName => "RespawnTimer";
    public override string ModuleDescription => "Respawn Timer for Minigames";

    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "dollan";

    public bool DoRespawn = false;
    public new List<CSTimer> Timers = [];

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("RespawnTimer loaded");

        AddCommandListener("css_respawn", OnRespawnCommand, HookMode.Pre);
        AddCommandListener("css_r", OnRespawnCommand, HookMode.Pre);
    }

    [GameEventHandler]
    private HookResult OnRespawnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!DoRespawn)
        {
            player?.PrintToChat(StringExtensions.ReplaceColorTags("{Lime}[Minigames] {Red}Respawning is currently disabled!"));
            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Kill all timers
        foreach (var timer in Timers)
        {
            timer.Kill();
        }
        DoRespawn = true;
        var new_timer = AddTimer(45, DisableRespawn);
        Timers.Add(new_timer);
        return HookResult.Continue;
    }

    public void DisableRespawn()
    {
        DoRespawn = false;
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
            return HookResult.Continue;
        }
        return HookResult.Continue;
    }
}
