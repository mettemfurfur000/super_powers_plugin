using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class TheSacrifice : BasePower
{
    public TheSacrifice()
    {
        Triggers = [typeof(EventPlayerDeath)];
        Price = 5500;
        Rarity = "Uncommon";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;

        var player = realEvent.Userid!;
        if (Users.Contains(player))
        {
            var players = Utilities.GetPlayers();

            players.ForEach(p =>
            {
                if (p.TeamNum == player.TeamNum)
                {
                    var pawn = p.PlayerPawn.Value;
                    if (pawn == null)
                        return;

                    p.PrintToggleable($"{player.PlayerName} Sacrificed {cfg_value} health for you");

                    pawn.Health += cfg_value;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            });
        }

        return HookResult.Continue;
    }


    public override string GetDescriptionColored() => StringHelpers.Green("+" + cfg_value) + " HP to all teammates on your death";
    public int cfg_value = 50;
}

