using System.Reflection;
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

    public static string GetPowerName(ISuperPower power)
    {
        return ToSnakeCase(power.GetType().ToString()).Split(".").Last();
    }

    public static string GetSnakeName(Type type)
    {
        return ToSnakeCase(type.ToString()).Split(".").Last();
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

    public static void ParseConfigReflective(ISuperPower power, Type type, Dictionary<string, string> cfg)
    {
        foreach (var field in cfg)
        {
            var fieldInfo = type.GetField(field.Key, BindingFlags.Instance | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                Server.PrintToConsole($"Error occured while processing {type} : Field '{field.Key}' not found");
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
                var found_fields = "";
                foreach (var f in fields)
                {
                    found_fields += "\n\"" + f.Name + "\"";
                }
                Server.PrintToConsole($"All Availiable fields: {found_fields}");
                continue;
            }

            try
            {
                try { fieldInfo.SetValue(power, Convert.ChangeType(field.Value, fieldInfo.FieldType)); }
                catch (InvalidCastException ex) { Server.PrintToConsole($"Error occured while processing {type} : Failed to convert value for {fieldInfo.Name}: {ex.Message}"); }
                catch (FormatException ex) { Server.PrintToConsole($"Error occured while processing {type} : Invalid format for {fieldInfo.Name}: {ex.Message}"); }
            }
            catch
            {
                // aah whatever
            }
        }
    }
}