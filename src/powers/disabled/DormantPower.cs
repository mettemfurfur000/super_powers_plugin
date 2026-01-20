using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

// needs sum rework, mayb make it activate a random power each time you get attacked once each round

public class DormantPower : BasePower
{
    public DormantPower()
    {
        Triggers = [typeof(EventRoundStart)];
        SetDisabled();
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.Handle == 0)
            return HookResult.Continue; // prevent recursive call

        var gameRules = TemUtils.GetGameRules();

        if (dormant_power_rules.Count == 0)
        {
            ParseMasterRule();
        }

        HashSet<string> power_commands = [];

        if (dormant_power_rules.Count == 0)
        {
            Server.PrintToConsole($"No rules for {gameRules.TotalRoundsPlayed} rounds");
            return HookResult.Continue;
        }

        try
        {
            power_commands = dormant_power_rules[gameRules.TotalRoundsPlayed];
        }
        catch (Exception)
        {
            return HookResult.Continue;
        }

        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            foreach (var command in power_commands)
            {
                Server.NextFrame(() =>
                {
                    var real_command = command.Replace("user", user.PlayerName);
                    Server.ExecuteCommand(real_command);
                    TemUtils.Log($"{ChatColors.Blue}Executed command {real_command} for {user.PlayerName}");
                });
            }
        }
        return HookResult.Continue;
    }

    public Dictionary<int, HashSet<string>> dormant_power_rules = [];



    public string cfg_master_rule = "fill_me";
    public string cfg_round_rule_separator = "|";
    public string cfg_power_separator = ";";

    public void ParseMasterRule()
    {
        if (cfg_master_rule == "fill_me")
        {
            TemUtils.AlertError("Master rule is not set");
            return;
        }

        var round_rules = cfg_master_rule.Split(cfg_round_rule_separator).ToHashSet();
        if (round_rules.Count == 0)
            return;

        foreach (var rule in round_rules)
        {
            var power_commands = rule.Split(cfg_power_separator);

            int round_number = int.Parse(power_commands[0]);

            dormant_power_rules.Add(round_number, power_commands.ToHashSet());
        }
    }
}

