using System.Drawing;
using System.Reflection;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

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

    public static string ToReadableCase(string input)
    {
        return string.Concat(input.Select((c, i) =>
            i > 0 && char.IsUpper(c) && char.IsLower(input[i - 1])
                ? " " + c
                : c.ToString()));
    }

    public static string GetPowerNameReadable(ISuperPower power)
    {
        return ToReadableCase(power.GetType().ToString().Split(".").Last());
    }

    public static string GetPowerName(ISuperPower power)
    {
        return ToSnakeCase(power.GetType().ToString().Split(".").Last());
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

    public static IEnumerable<CCSPlayerController> SelectTeam(CsTeam team)
    {
        return Utilities.GetPlayers()
            .Where(player => player.Team == team);
    }

    public static CsTeam ParseTeam(string s)
    {
        CsTeam csteam = CsTeam.None;

        if (s.ToLower().Equals("ct"))
            csteam = CsTeam.CounterTerrorist;
        if (s.ToLower().Equals("t"))
            csteam = CsTeam.Terrorist;

        return csteam;
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


    public static void Damage(CCSPlayerPawn player, uint value)
    {
        Server.NextFrame(() =>
        {
            if (value >= player.Health)
            {
                var controller = player.OriginalController.Value!;
                controller.ExecuteClientCommandFromServer($"hurtme {value}");
            }
            else
            {
                player.Health -= (int)value;
            }

            Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");
        });
    }

    public static void MakeModelGlow(CBaseEntity entity)
    {
        CBaseModelEntity? modelGlow = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        CBaseModelEntity? modelRelay = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");

        if (modelGlow == null || modelRelay == null)
            return;

        if (entity.CBodyComponent?.SceneNode == null)
        {
            Server.PrintToChatAll("Failed to make pawn glow: CBodyComponent or SceneNode is null.");
            return;
        }

        string modelName = entity.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName;

        modelRelay.SetModel(modelName);
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;
        modelRelay.DispatchSpawn();

        modelGlow.SetModel(modelName);
        modelGlow.Spawnflags = 256u;
        modelGlow.DispatchSpawn();

        modelGlow.Glow.GlowColorOverride = Color.Red;

        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = 0;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 100;

        modelRelay.AcceptInput("FollowEntity", entity, modelRelay, "!activator");
        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");
    }
}