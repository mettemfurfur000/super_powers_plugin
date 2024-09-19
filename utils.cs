using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace super_powers_plugin;

public class TemUtils
{
    public static string RandomString(int length)
    {
        const string chars = "0123456789";
        var random = new Random();
        var result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = chars[random.Next(chars.Length)];

        return new string(result);
    }

    public static string ToSnakeCase(string input)
    {
        return string.Concat(input.Select((c, i) =>
            i > 0 && char.IsUpper(c) && char.IsLower(input[i - 1])
                ? "_" + char.ToLower(c)
                : char.ToLower(c).ToString()));
    }

    public static string GetPowerName(ISuperPower type)
    {
        return ToSnakeCase(type.GetType().ToString()).Split(".").Last();
    }

    public static String WildCardToRegular(String value)
    {
        return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
    }

    public static IEnumerable<CCSPlayerController> SelectPlayers(string name_pattern)
    {
        string r_pattern = WildCardToRegular(name_pattern);

        return Utilities.GetPlayers()
            .Where(player => Regex.IsMatch(player.PlayerName, r_pattern, RegexOptions.IgnoreCase));
    }
}