using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class InfiniteMoney : BasePower
{
    public InfiniteMoney() => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % 32 != 0)
            return;
        foreach (var user in Users)
        {
            user.InGameMoneyServices!.Account += 500;
            Utilities.SetStateChanged(user, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player != null)
            player.InGameMoneyServices!.Account = 800;
        else
        {
            Users.ForEach(p =>
            {
                p.InGameMoneyServices!.Account = 800;
            });
        }
    }

    public override string GetDescription() => $"Near infinite supply of money";
}

