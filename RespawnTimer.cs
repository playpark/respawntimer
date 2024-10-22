using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Logging;

namespace RespawnTimer;
public class RespawnTimer : BasePlugin
{
    public override string ModuleName => "RespawnTimer";
    public override string ModuleDescription => "Respawn Timer for Minigames";

    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "dollan";

    public bool DoRespawn = false;

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("RespawnTimer loaded");
    }

    [GameEventHandler]
    public HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
    {
        DoRespawn = true;
        AddTimer(60, () => DoRespawn = false);
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
                user.Respawn();
                user.PrintToChat(StringExtensions.ReplaceColorTags("{Lime}[Minigames] {Red}Respawned"));
            }
            return HookResult.Continue;
        }
        return HookResult.Continue;
    }
}
