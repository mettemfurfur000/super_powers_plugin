using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace super_powers_plugin.src;

public class DormantPower : ISuperPower
{
    public DormantPower()
    {
        Triggers = [typeof(EventRoundStart)];
        setDisabled();
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

    public override string GetDescription() => $"Internal use only";

    private string master_rule = "fill_me";
    private string round_rule_separator = "|";
    private string power_separator = ";";

    private void ParseMasterRule()
    {
        if (master_rule == "fill_me")
        {
            TemUtils.AlertError("Master rule is not set");
            return;
        }

        var round_rules = master_rule.Split(round_rule_separator).ToHashSet();
        if (round_rules.Count == 0)
            return;

        foreach (var rule in round_rules)
        {
            var power_commands = rule.Split(power_separator);

            int round_number = int.Parse(power_commands[0]);

            dormant_power_rules.Add(round_number, power_commands.ToHashSet());
        }
    }
}

