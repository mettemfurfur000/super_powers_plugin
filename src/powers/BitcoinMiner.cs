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
        Rarity = "Uncommon";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % (cfg_periodSeconds * 64) != 0)
            return;
        foreach (var user in Users)
        {
            if (Random.Shared.NextSingle() < (cfg_probabilityPercentage / 100)) // guh
                TemUtils.GiveMoney(user, cfg_moneyBonusAmount, "from the BitcoinMiner");
        }
    }

    public int cfg_moneyBonusAmount = 5555;
    public int cfg_probabilityPercentage = 5;
    public int cfg_periodSeconds = 5;


    public override string GetDescriptionColored() => "Every" + StringHelpers.Green(cfg_periodSeconds) + ", gain $" + StringHelpers.Green(cfg_moneyBonusAmount) + " with a chance of " + StringHelpers.Blue(cfg_probabilityPercentage.ToString() + "%");
}



