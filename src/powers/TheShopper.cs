using System;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Threading;
using System.Linq;
using CounterStrikeSharp.API.Modules.Entities;
using System.Data.Common;

using super_powers_plugin.src;
using System.Net.Sockets;
using CounterStrikeSharp.API.Modules.Utils;

public class ShopOption
{
    public BasePower? Power;
    public int Price = 0;
    public bool bought = false;
}

public class TheShopper : BasePower
{
    public TheShopper()
    {
        Triggers = [typeof(EventRoundStart), typeof(EventGameStart)];
        NoShop = true;
    }

    public int shopStartedTick = 0; // used to track when the shop was started, so we can close it after some time

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() == typeof(EventRoundStart))
        {
            shopStartedTick = Server.TickCount;

            foreach (var user in Users)
                if (!activeShops.ContainsKey(user))
                {
                    activeShops[user] = ShopGenerate(user);
                    PrintShopToChat(user, activeShops[user]);
                }
        }

        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % 32 != 0)
            return;

        if (Server.TickCount - shopStartedTick > shopAvailableSeconds * 64)
            foreach (var user in Users)
            {
                if (user.IsValid)
                    if (activeShops.TryGetValue(user, out List<ShopOption>? shop))
                    {
                        user.PrintToChat(NiceText.Paint("Shop closed", ChatColors.Gold));
                        activeShops.Remove(user);
                    }
            }
    }

    public override Tuple<SIGNAL_STATUS, string> OnSignal(CCSPlayerController? player, List<string> args)
    {
        if (player == null)
            return SuperPowerController.ignored_signal;

        if (!Users.Contains(player))
            return SuperPowerController.ignored_signal;

        string command = args[0];

        if (command == "b")
        {
            string? choice = null;
            if (args.Count > 1)
                choice = args[1];

            // Server.PrintToChatAll("Shopper signal received: " + command + (choice != null ? $", {choice}" : ""));

            if (activeShops.TryGetValue(player, out List<ShopOption>? shop))
            {
                if (choice != null && int.TryParse(choice, out int index) && index > 0 && index <= shop.Count)
                {
                    var option = shop[index - 1];
                    if (!option.bought)
                    {
                        TemUtils.AttemptPaidAction(player, paidChoice ? option.Price : 0, NiceText.GetPowerColoredName(option.Power!), () =>
                        {
                            option.bought = true;
                            option.Power!.OnAdd(player);

                            if (!paidChoice)
                                activeShops.Remove(player);
                            // some powers will only activate on the next round start, dont know how to deal with it yet
                            // option.Power!.Execute(null); // if multiple players get the same power they get the effect twice, instead of +250 health it will be +500
                        });
                    }
                    else
                        player.PrintToChat($"You already bought {NiceText.GetPowerColoredName(option.Power!)}");
                }
                else
                    player.PrintToChat("Invalid choice!");
            }
            else
            {
                if (!paidChoice)
                    player.PrintToChat("Only 1 power can be chosen!");
                else
                    player.PrintToChat("Invalid shop!");
            }
        }

        return signalAccepted;
    }

    static Tuple<SIGNAL_STATUS, string> signalAccepted = new Tuple<SIGNAL_STATUS, string>(SIGNAL_STATUS.ACCEPTED, $"");

    // generates a list of unique powers for the shop
    private List<ShopOption> ShopGenerate(CCSPlayerController player)
    {
        List<ShopOption> retList = [];

        List<(int, string)> rollWeights =
        [
            (CommonWeight, "Common"),
            (UncommonWeight, "Uncommon"),
            (RareWeight, "Rare"),
            (LegendaryWeight, "Legendary"),
        ];

        for (int i = 0; i < powersAvailable; i++)
        {
            var chosen = WeaponHelpers.GetWeighted(rollWeights)!;

            int roll_limit = 15;

            do
            {
                roll_limit--;
                if (roll_limit < 0)
                    break;
                BasePower? power = SuperPowerController.GetRandomPower(chosen.Value.Item2);

                if (power == null)
                    continue;

                if (priceRoundFactor == 0)
                    priceRoundFactor = 1; // avoid division by zero

                var price = Math.Round((power.Price * priceMultiplier) / priceRoundFactor) * priceRoundFactor;

                if (!retList.Any(p => p.Power!.Name == power.Name)          // only unique and playable powers pass
                    && SuperPowerController.IsPowerPlayable(player, power)
                    && !power.Users.Contains(player)
                    && power.NoShop == false
                    && power.IsDisabled() == false
                    && price > 0) // price must be positive
                {
                    var option = new ShopOption
                    {
                        Price = (int)price,
                        Power = power
                    };

                    retList.Add(option);
                    break;
                }
            } while (true);
        }

        return retList;
    }

    public void PrintShopToChat(CCSPlayerController user, List<ShopOption> options)
    {
        user.PrintToChat($" {ChatColors.Gold}Super Power Shop");
        user.PrintToChat($" {ChatColors.Grey}Use command {ChatColors.Gold}/b <number> {ChatColors.Grey} pick a power" + (!paidChoice ? "(Only 1 pick)" : ""));

        // options.Sort((a, b) => a.Power!.priority.CompareTo(b.Power!.priority));
        // options.Reverse();
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            user.PrintToChat($" {i + 1} - {ChatColors.Green} {(paidChoice ? "${option.Price}" : "")} {NiceText.GetPowerColoredName(option.Power!)}");
            user.PrintToChat($" {option.Power!.GetDescriptionColored()}");
        }
    }

    public override string GetDescription() => $"Allows to buy powers on the start of each round";

    private int powersAvailable = 4;
    private float priceMultiplier = 1;
    private int shopAvailableSeconds = 35;
    private int priceRoundFactor = 500;

    private bool paidChoice = false;

    private int CommonWeight = 10;
    private int UncommonWeight = 6;
    private int RareWeight = 3;
    private int LegendaryWeight = 1;

    public Dictionary<CCSPlayerController, List<ShopOption>> activeShops = [];
}

