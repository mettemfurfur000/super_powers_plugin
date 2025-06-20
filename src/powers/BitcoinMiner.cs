using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class BitcoinMiner : BasePower
{
    public BitcoinMiner() => Triggers = [];
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

    private int moneyBonusAmount = 5555;
    private int probabilityPercentage = 5;
    private int periodSeconds = 5;

    public override string GetDescription() => $"Every ${periodSeconds} second(s), gain ${moneyBonusAmount} with a chance of {probabilityPercentage}%";
}

