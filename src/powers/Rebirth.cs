using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class Rebirth : BasePower
{
    public Rebirth()
    {
        Triggers = [typeof(EventPlayerDeath), typeof(EventRoundStart)];
        Price = 8000;
        Rarity = "Legendary";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is null)
            return HookResult.Continue;

        if (gameEvent.GetType() == typeof(EventPlayerDeath))
        {
            EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;

            
            var player = realEvent.Userid;
            if (player == null)
                return HookResult.Continue;

            if (Users.Contains(player))
            {
                                var pawn = player.PlayerPawn.Value!;
                positions[player] = new Tuple<Vector, QAngle>(
                    new Vector(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
                    new QAngle(pawn.V_angle.X, pawn.V_angle.Y, pawn.V_angle.Z)
                    );
                            }
        }

        if (gameEvent.GetType() == typeof(EventRoundStart))
        {
            EventRoundStart realEvent = (EventRoundStart)gameEvent;

            Users.ForEach(player =>
            {
                var pawn = player.PlayerPawn.Value!;

                
                if (Users.Contains(player))
                {
                                        if (positions.TryGetValue(player, out Tuple<Vector, QAngle>? value))
                    {
                        
                        Server.NextFrame(() => pawn.Teleport(value.Item1, value.Item2, new Vector(0, 0, 0)));

                        if (cfg_allowBuy)
                        {
                            pawn.InBuyZone = true;
                            buyspamactive.Add(player);
                            TemUtils.__plugin?.AddTimer(cfg_allowBuyTime, () =>
                            {
                                pawn.InBuyZone = false;
                                pawn.WasInBuyZone = true;
                                buyspamactive.Remove(player);
                            });
                        }

                        positions.Remove(player);
                    }
                }
            });
        }

        return HookResult.Continue;
    }

    public override void Update()
    {
        buyspamactive.ForEach(user =>
        {
            user.PlayerPawn.Value!.InBuyZone = true;
        });
    }


    public override string GetDescriptionColored() => $"Respawn at your " + StringHelpers.Blue("last death location") + ". If survived, spawn with yout team as before";

    public Dictionary<CCSPlayerController, Tuple<Vector, QAngle>> positions = [];
    public List<CCSPlayerController> buyspamactive = [];

    public bool cfg_allowBuy = true;
    public int cfg_allowBuyTime = 20;
}

