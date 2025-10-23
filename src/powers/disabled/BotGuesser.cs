using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class BotGuesser : BasePower
{
    public BotGuesser()
    {
        Triggers = [typeof(EventRoundStart)];
        SetDisabled();
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        var gameRules = TemUtils.GetGameRules();

        if (gameRules.TotalRoundsPlayed == 0 ||
         gameRules.TotalRoundsPlayed % cfg_guess_each_x_rounds != 0)
            return HookResult.Continue;

        foreach (var user in Users)
        {
            user.PrintToChat("Vote availiable - use !signal kick <name>");
            user.PrintToChat("Vote availiable - use !signal kick <name>");
            user.PrintToChat("Vote availiable - use !signal kick <name>");
            user.PrintToChat("Vote availiable - use !signal kick <name>");
            user.PrintToChat("Wildcards usable to match any set of characters, example - '*hnepixel*' , matches 0hnepixel ");

            allow_vote = true;
        }

        return HookResult.Continue;
    }

    public override Tuple<SIGNAL_STATUS, string> OnSignal(CCSPlayerController? player, List<string> args)
    {
        if (args.Count != 3)
            goto shortcut_ignore;

        string details = "";

        string subcmd = args[1];
        if (subcmd == "kick")
        {
            if (allow_vote == false)
            {
                details = "Not availiable";
                goto shortcut_error;
            }

            string target = args[2];
            // Server.PrintToChatAll($"desired to kick {target}");

            var sel = TemUtils.SelectPlayers(target);

            if (sel == null || !sel.Any())
            {
                details = "Player not found";
                goto shortcut_error;
            }

            var sel_player = sel.First();

            // Server.PrintToChatAll($"selected {sel_player.PlayerName}");

            var power = SuperPowerController.GetPowersByName("bot_disguise");

            if (power.Users.Contains(sel_player))
            {
                allow_vote = false;

                if (sel_player.IsBot == cfg_do_kick_bots)
                {
                    Server.PrintToChatAll($"Guessed right");
                    sel_player.Disconnect(CounterStrikeSharp.API.ValveConstants.Protobuf.NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED_VOTEDOFF);
                }
                else
                    Server.PrintToChatAll($"Incorrect");
            }
            else
            {
                Server.PrintToChatAll($"{sel_player.PlayerName} cant be chosen for this, vote is still availiable");
            }

            return Tuple.Create(SIGNAL_STATUS.ACCEPTED, "");
        }

    shortcut_ignore:
        return Tuple.Create(SIGNAL_STATUS.IGNORED, "");
    shortcut_error:
        return Tuple.Create(SIGNAL_STATUS.ERROR, details);
    }

    public bool cfg_do_kick_bots = false;
    public int cfg_guess_each_x_rounds = 5;

    public bool allow_vote = false;
    public int rounds_without_a_vote = 0;

    public override string GetDescription() => $"Allows to kick bots each round";
}

