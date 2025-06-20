using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;

using super_powers_plugin.src;

public class Invisibility : BasePower
{
    public Invisibility() => Triggers = [typeof(EventPlayerSound), typeof(EventWeaponFire)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is EventPlayerSound realEventSound)
            HandleEvent(realEventSound.Userid, 0.4);
        if (gameEvent is EventWeaponFire realEventFire)
            HandleEvent(realEventFire.Userid, 0.2);
        return HookResult.Continue;
    }

    private void HandleEvent(CCSPlayerController? player, double duration = 1.0)
    {
        if (player == null)
            return;

        if (!Users.Contains(player))
            return;

        var idx = Users.IndexOf(player);
        if (idx == -1)
            return;

        Levels[idx] = -duration;
    }

    public override void Update()
    {
        for (int i = 0; i < Users.Count; i++)
        {
            if (Levels[i] < 0.5)
                continue;

            var player = Users[i];

            if (player.PlayerPawn != null && player.PlayerPawn.Value != null)
            {
                var pawn = player.PlayerPawn.Value;

                pawn.EntitySpottedState.SpottedByMask[0] = 0;
                pawn.EntitySpottedState.SpottedByMask[1] = 0;
                pawn.EntitySpottedState.Spotted = false;

                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState", Schema.GetSchemaOffset("EntitySpottedState_t", "m_bSpotted"));
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState", Schema.GetSchemaOffset("EntitySpottedState_t", "m_bSpottedByMask"));
            }

        }

        if (Server.TickCount % 8 != 0)
            return;

        for (int i = 0; i < Users.Count; i++)
        {
            var newValue = Levels[i] < 1.0f ? Levels[i] + 0.1 : 1.0f;

            if (newValue != Levels[i])
            {
                var player = Users[i];
                TemUtils.SetPlayerVisibilityLevel(player, (float)newValue);

                Levels[i] = newValue;
            }
        }
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player == null)
        {
            foreach (var p in Users)
            {
                Levels[Users.IndexOf(p)] = -1.0f;
                TemUtils.SetPlayerVisibilityLevel(p, 0.0f);
            }
            return;
        }

        Levels[Users.IndexOf(player)] = -1.0f;
        TemUtils.SetPlayerVisibilityLevel(player, 0.0f);
    }

    public override string GetDescription() => $"Gain invisibility, when not moving (Custom items will still be seen)";

    public double[] Levels = new double[65];
}

