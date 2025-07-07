using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

public class BitcoinMiner : BasePower
{
    public BitcoinMiner()
    {
        Triggers = [];
        Price = 5000;
        Rarity = PowerRarity.Uncommon;
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % (periodSeconds * 64) != 0)
            return;
        foreach (var user in Users)
        {
            if (Random.Shared.NextSingle() < 100 / probabilityPercentage)
            TemUtils.GiveMoney(user, moneyBonusAmount, "from the BitcoinMiner");
        }
    }

    private int moneyBonusAmount = 5555;
    private int probabilityPercentage = 5;
    private int periodSeconds = 5;

    public override string GetDescription() => $"Every ${periodSeconds} second(s), gain ${moneyBonusAmount} with a chance of {probabilityPercentage}%";
    public override string GetDescriptionColored() => "Every" + NiceText.Green(periodSeconds) + ", gain $" + NiceText.Green(moneyBonusAmount) + " with a chance of " + NiceText.Blue(probabilityPercentage.ToString() + "%");
}



