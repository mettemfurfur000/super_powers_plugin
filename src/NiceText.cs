using System.Diagnostics.CodeAnalysis;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

public static class NiceText
{
    public static string ToCamelCase(string input)
    {
        return string.Concat(input.Select((c, i) =>
            i == 0 ? char.ToLower(c).ToString() : char.ToUpper(c).ToString()));
    }

    public static string Paint(string word, char color)
    {
        return $" {color}{word}{ChatColors.White}";
    }

    public static string Red<T>(T word)
    {
        return $" {ChatColors.LightRed}{word!.ToString()}{ChatColors.White}";
    }

    public static string Green<T>(T word)
    {
        return $" {ChatColors.Lime}{word!.ToString()}{ChatColors.White}";
    }

    public static string Blue<T>(T word)
    {
        return $" {ChatColors.LightBlue}{word!.ToString()}{ChatColors.White}";
    }

    public static string ToSnakeCase(string input)
    {
        return string.Concat(input.Select((c, i) =>
            i > 0 && char.IsUpper(c) && char.IsLower(input[i - 1])
                ? "_" + char.ToLower(c)
                : char.ToLower(c).ToString()));
    }

    public static string ToReadableCase(string input)
    {
        return string.Concat(input.Select((c, i) =>
            i > 0 && char.IsUpper(c) && char.IsLower(input[i - 1])
                ? " " + c
                : c.ToString()));
    }

    public static string FirstUpper(string input)
    {
        return string.Concat(input.Select((c, i) =>
            i == 0 && char.IsUpper(c)
                ? char.ToUpper(c).ToString()
                : c.ToString()));
    }

    public static string GetPowerNameReadable(BasePower power)
    {
        return ToReadableCase(power.GetType().ToString().Split(".").Last());
    }

    public static string GetPowerName(BasePower power)
    {
        return ToSnakeCase(power.GetType().ToString().Split(".").Last());
    }

    public static string GetSnakeName(Type type)
    {
        return ToSnakeCase(type.ToString()).Split(".").Last();
    }

    public static string GetPowerColoredName(BasePower power)
    {
        return $" {GetPowerRarityColor(power)}{NiceText.GetPowerNameReadable(power)}";
    }

    public static string GetPowerRarityColor(BasePower power)
    {
        return power.Rarity switch
        {
            PowerRarity.Common => ChatColors.White.ToString(),
            PowerRarity.Uncommon => ChatColors.Green.ToString(),
            PowerRarity.Rare => ChatColors.Purple.ToString(),
            PowerRarity.Legendary => ChatColors.Orange.ToString(),
            _ => ChatColors.White.ToString()
        };
    }

    public static string GetPowerDescription(BasePower power)
    {
        return $" {ChatColors.White}{power.GetDescription()}";
    }
}