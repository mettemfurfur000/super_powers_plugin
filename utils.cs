using System.Drawing;
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

    public static void SetPlayerVisibilityLevel(CCSPlayerController player, float invisibilityLevel)
    {
        var playerPawnValue = player.PlayerPawn.Value;
        if (playerPawnValue == null || !playerPawnValue.IsValid)
        {
            Console.WriteLine("Player pawn is not valid.");
            return;
        }

        int alpha = (int)((1.0f - invisibilityLevel) * 255);
        alpha = alpha > 255 ? 255 : alpha < 0 ? 0 : alpha; // >:3
        var fadeColor = Color.FromArgb(alpha, 255, 255, 255);

        playerPawnValue.Render = fadeColor;
        Utilities.SetStateChanged(playerPawnValue, "CBaseModelEntity", "m_clrRender");

        var viewModel = playerPawnValue.ViewModelServices!.Pawn.Value;
        viewModel.Render = fadeColor;
        Utilities.SetStateChanged(viewModel, "CBaseModelEntity", "m_clrRender");

        var weaponServices = playerPawnValue.WeaponServices;
        if (weaponServices != null)
        {
            var activeWeapon = weaponServices.ActiveWeapon.Value;
            if (activeWeapon != null && activeWeapon.IsValid)
            {
                activeWeapon.Render = fadeColor;
                activeWeapon.ShadowStrength = invisibilityLevel;
                Utilities.SetStateChanged(activeWeapon, "CBaseModelEntity", "m_clrRender");
            }
        }

        var myWeapons = playerPawnValue.WeaponServices?.MyWeapons;
        if (myWeapons != null)
            foreach (var gun in myWeapons)
            {
                var weapon = gun.Value;
                if (weapon != null)
                {
                    weapon.Render = fadeColor;
                    weapon.ShadowStrength = invisibilityLevel;
                    Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");

                    // if (weapon.DesignerName == "weapon_c4")
                    // {
                    //     // Server.PrintToChatAll($"C4 Glow values: {weapon.RenderMode}, {weapon.RenderFX}, {weapon.Glow.GlowColor}, {weapon.Glow.GlowColorOverride}, {weapon.Glow.GlowType}");

                    //     weapon.RenderMode = visibilityLevel <= 1.0f ? RenderMode_t.kRenderNone : RenderMode_t.kRenderNormal;
                    //     weapon.RenderFX = visibilityLevel <= 1.0f ? RenderFx_t.kRenderFxNone : RenderFx_t.kRenderFxPulseFastWide;

                    //     Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_nRenderMode");
                    //     Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_nRenderFX");
                    // }
                }
            }
    }


    public static void Damage(CBasePlayerPawn player, int value)
    {
        Server.NextFrame(() =>
        {
            player.Health -= value;
            if (player.Health < 1)
                player.Health = -1;
            Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");
        });
    }
}