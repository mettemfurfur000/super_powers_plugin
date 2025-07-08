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

                    p.PrintToChat($"{player.PlayerName} Sacrificed {value} health for you");

                    pawn.Health += value;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            });
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"+{value} HP to all teammates on your death";
    public override string GetDescriptionColored() => NiceText.Green("+" + value) + " HP to all teammates on your death";
    private int value = 50;
}

