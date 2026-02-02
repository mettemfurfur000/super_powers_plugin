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
        Triggers = [typeof(EventRoundStart), typeof(EventGameStart), typeof(EventPlayerHurt)];
        NoShop = true;
    }

    public int shopStartedTick = 0; // used to track when the shop was started, so we can close it after some time
    public Dictionary<(CCSPlayerController, CCSPlayerController), bool> revealedThisRound = []; // track revealed power pairs to avoid spam

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() == typeof(EventRoundStart))
        {
            shopStartedTick = Server.TickCount;
            revealedThisRound.Clear(); // Clear power reveals on new round

            foreach (var user in Users)
                if (!activeShops.ContainsKey(user))
                {
                    activeShops[user] = ShopGenerate(user);
                    PrintShopToChat(user, activeShops[user]);
                }
        }
        else if (gameEvent.GetType() == typeof(EventPlayerHurt))
        {
            var hurtEvent = (EventPlayerHurt)gameEvent;
            var attacker = hurtEvent.Attacker;
            var victim = hurtEvent.Userid;

            if (attacker == null || victim == null)
                return HookResult.Continue;

            // Check if victim has shopper power
            if (!Users.Contains(victim))
                return HookResult.Continue;

            // Check if we haven't already revealed this attacker to this victim this round
            var pair = (attacker, victim);
            if (revealedThisRound.ContainsKey(pair))
                return HookResult.Continue;

            revealedThisRound[pair] = true;

            // Get all powers the attacker has
            var attackerPowerNames = GetPlayerPowerNames(attacker);
            
            attackerPowerNames.Sort();
            attackerPowerNames.Remove(StringHelpers.GetPowerColoredName(this)); // remove shopper power from the list

            if (attackerPowerNames.Count > 0)
            {
                var powerList = string.Join($"{ChatColors.White}, ", attackerPowerNames);
                victim.PrintToggleable($"{ChatColors.Gold}{attacker.PlayerName} powers: {ChatColors.White}{powerList}");
            }
        }

        return HookResult.Continue;
    }

    // Helper method to get all power names for a player
    private List<string> GetPlayerPowerNames(CCSPlayerController player)
    {
        var powerNames = new List<string>();
        foreach (var power in SuperPowerController.GetPowers())
        {
            if (power.Users.Contains(player))
                powerNames.Add(power.ColoredName);
        }
        return powerNames;
    }

    public override void Update()
    {
        if (Server.TickCount % 32 != 0)
            return;

        if (Server.TickCount - shopStartedTick > cfg_shop_open_seconds * 64)
            foreach (var user in Users)
            {
                if (user.IsValid)
                    if (activeShops.TryGetValue(user, out List<ShopOption>? shop))
                    {
                        user.PrintToggleable(StringHelpers.Paint("Shop closed", ChatColors.Gold));
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

            
            if (activeShops.TryGetValue(player, out List<ShopOption>? shop))
            {
                if (choice != null && int.TryParse(choice, out int index) && index > 0 && index <= shop.Count)
                {
                    var option = shop[index - 1];
                    if (!option.bought)
                    {
                        TemUtils.AttemptPaidAction(player, cfg_is_paid ? option.Price : 0, StringHelpers.GetPowerColoredName(option.Power!), () =>
                        {
                            option.bought = true;
                            option.Power!.OnAdd(player);

                            if (!cfg_is_paid)
                                activeShops.Remove(player);
                            // some powers will only activate on the next round start, dont know how to deal with it yet
                            // option.Power!.Execute(null); // if multiple players get the same power they get the effect twice, instead of +250 health it will be +500
                        });
                    }
                    else
                        player.PrintToggleable($"You already bought {StringHelpers.GetPowerColoredName(option.Power!)}");
                }
                else
                    player.PrintToggleable("Invalid choice!");
            }
            else
            {
                if (!cfg_is_paid)
                    player.PrintToggleable("Only 1 power can be chosen!");
                else
                    player.PrintToggleable("Invalid shop!");
            }
        }

        return accepted_signal;
    }

    static Tuple<SIGNAL_STATUS, string> accepted_signal = new Tuple<SIGNAL_STATUS, string>(SIGNAL_STATUS.ACCEPTED, $"");

    // generates a list of unique powers for the shop
    public List<ShopOption> ShopGenerate(CCSPlayerController player)
    {
        List<ShopOption> retList = [];

        List<(int, string)> rollWeights =
        [
            (cfg_common_weight, "Common"),
            (cfg_uncommon_weight, "Uncommon"),
            (cfg_rare_weight, "Rare"),
            (cfg_legendary_weight, "Legendary"),
        ];

        for (int i = 0; i < cfg_shop_amount; i++)
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

                if (cfg_price_round_to == 0)
                    cfg_price_round_to = 1; // avoid division by zero

                var price = Math.Round((power.Price * cfg_price_mult) / cfg_price_round_to) * cfg_price_round_to;

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
        user.PrintToggleable($" {ChatColors.Gold}Super Power Shop");
        user.PrintToggleable($" {ChatColors.Grey}Use command {ChatColors.Gold}/b <number> {ChatColors.Grey} pick a power" + (!cfg_is_paid ? "(Only 1 pick)" : ""));

        // options.Sort((a, b) => a.Power!.priority.CompareTo(b.Power!.priority));
        // options.Reverse();
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            user.PrintToggleable($" {i + 1} - {ChatColors.Green} {(cfg_is_paid ? "${option.Price}" : "")} {StringHelpers.GetPowerColoredName(option.Power!)}");
            user.PrintToggleable($" {option.Power!.GetDescriptionColored()}");
        }
    }



    public int cfg_shop_amount = 4;
    public float cfg_price_mult = 1;
    public int cfg_shop_open_seconds = 35;
    public int cfg_price_round_to = 500;

    public bool cfg_is_paid = false;

    public int cfg_common_weight = 10;
    public int cfg_uncommon_weight = 6;
    public int cfg_rare_weight = 3;
    public int cfg_legendary_weight = 1;

    public Dictionary<CCSPlayerController, List<ShopOption>> activeShops = [];
}

