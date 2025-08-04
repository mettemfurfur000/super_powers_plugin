using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

// supposed to warp players back a few seconds after they get hurt
public class WarpPeek : BasePower
{
    public WarpPeek()
    {
        Triggers = [typeof(EventPlayerHurt)];
        Price = 7000;
        Rarity = "Rare";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        EventPlayerHurt realEvent = (EventPlayerHurt)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        if (timeouts.ContainsKey(player))
            if (timeouts[player] > 0)
            {
                timeouts[player] = timeout;
                return HookResult.Continue;
            }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        int next_index = (current_index + 1) % max_index;

        pawn.Teleport(positions[player][next_index].Item1, positions[player][next_index].Item2, new Vector(0, 0, 0));

        timeouts[player] = timeout;

        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % period != 0)
            return;

        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null) continue;

            var absOrigin = pawn.AbsOrigin;
            if (absOrigin == null) continue;

            if (!positions.ContainsKey(user))
                positions[user] = [];

            positions[user][current_index] = new Tuple<Vector, QAngle>(
                new Vector(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
                new QAngle(pawn.V_angle.X, pawn.V_angle.Y, pawn.V_angle.Z)
                );

            if (!timeouts.ContainsKey(user))
                timeouts.Add(user, 0);

            if (timeouts[user] > 0)
                timeouts[user] -= 1;
        }

        current_index++;
        if (current_index >= max_index)
            current_index = 0;
    }

    public override string GetDescription() => $"Warp back in time when hit";
    public override string GetDescriptionColored() => "Warp " + NiceText.Blue("back in time") + " when hit";

    // each user must have their own position history
    // has a static array for memory economy
    public Dictionary<CCSPlayerController, Dictionary<int, Tuple<Vector, QAngle>>> positions = [];
    public Dictionary<CCSPlayerController, int> timeouts = [];
    public int current_index = 0;
    private int max_index = 10;
    private int period = 16;
    private int timeout = 30;
}

