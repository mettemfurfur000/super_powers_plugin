using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace super_powers_plugin.src;

public class TemUtils
{
    public static string Formatter(string message, char color)
    {
        return $"[{ChatColors.Gold} Super Powers {ChatColors.Default}] {color} {message}";
    }

    public static void Print(string message, char color)
    {
        Server.PrintToConsole(Formatter(message, color));
    }

    public static void AlertError(string message)
    {
        Print(message, ChatColors.Red);
    }

    public static void Warning(string message)
    {
        Print(message, ChatColors.Yellow);
    }

    public static void Log(string message)
    {
        Print(message, ChatColors.Default);
    }

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

    public static string? InspectPowerReflective(ISuperPower power, Type type)
    {
        string output = "";

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        int total = 0;
        foreach (var field in fields)
        {
            var fieldInfo = type.GetField(field.Name, BindingFlags.Instance | BindingFlags.NonPublic);

            if (fieldInfo != null)
            {
                if (fieldInfo.IsPublic) continue;
                if (fieldInfo.Name.Contains("Triggers")) continue;
                if (fieldInfo.Name.Contains("Users")) continue;
                output += $"{field.Name}: {fieldInfo.GetValue(power)}\n";
                total++;
            }
            else
                output += $"{field.Name}: null\n";
        }

        if (total == 0)
            return null;

        return output;
    }

    public static void ParseConfigReflective(ISuperPower power, Type type, Dictionary<string, string> cfg)
    {
        foreach (var field in cfg)
        {
            var fieldInfo = type.GetField(field.Key, BindingFlags.Instance | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                TemUtils.AlertError($"Error occured while processing {type} : Field '{field.Key}' not found");
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
                var found_fields = "";
                foreach (var f in fields)
                {
                    found_fields += "\n\"" + f.Name + "\"";
                }
                TemUtils.Log($"All Availiable fields: {found_fields}");
                continue;
            }

            try
            {
                try { fieldInfo.SetValue(power, Convert.ChangeType(field.Value, fieldInfo.FieldType)); }
                catch (InvalidCastException ex) { TemUtils.AlertError($"Error occured while processing {type} : Failed to convert value for {fieldInfo.Name}: {ex.Message}"); }
                catch (FormatException ex) { TemUtils.AlertError($"Error occured while processing {type} : Invalid format for {fieldInfo.Name}: {ex.Message}"); }
            }
            catch
            {
                // aah whatever
            }
        }
    }

    public static string ReflectPrintClass(object thing)
    {
        if (thing == null)
            return string.Empty;

        var classTypeDef = thing.GetType();

        var output = new StringBuilder();
        var properties = classTypeDef.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var fields = classTypeDef.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var property in properties)
        {
            output.AppendLine($"Property: {property.Name}");
            output.AppendLine($"  Type: {property.PropertyType}");
            output.AppendLine($"  Can Read: {property.CanRead}");
            output.AppendLine($"  Can Write: {property.CanWrite}");

            if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
            {
                output.AppendLine("  Nested Properties:");
                output.AppendLine(ReflectPrintClass(property.PropertyType).Replace(Environment.NewLine, Environment.NewLine + "    "));
            }
        }

        foreach (var field in fields)
        {
            output.AppendLine($"Field: {field.Name}");
            output.AppendLine($"  Type: {field.FieldType}");
            output.AppendLine($"  Is Public: {field.IsPublic}");

            if (field.FieldType.IsClass && field.FieldType != typeof(string))
            {
                output.AppendLine("  Nested Fields:");
                output.AppendLine(ReflectPrintClass(field.FieldType).Replace(Environment.NewLine, Environment.NewLine + "    "));
            }
        }

        return output.ToString();
    }

    public static void SetPlayerVisibilityLevel(CCSPlayerController player, float invisibilityLevel)
    {
        var playerPawnValue = player.PlayerPawn.Value;
        if (playerPawnValue == null || !playerPawnValue.IsValid)
        {
            TemUtils.Log("Player pawn is not valid.");
            return;
        }

        int alpha = (int)((1.0f - invisibilityLevel) * 255);
        alpha = alpha > 255 ? 255 : alpha < 0 ? 0 : alpha; // >:3
        var fadeColor = Color.FromArgb(alpha, 255, 255, 255);

        //Server.PrintToConsole("alpha for " + player.PlayerName + " is " + alpha);

        playerPawnValue.Render = fadeColor;
        Utilities.SetStateChanged(playerPawnValue, "CBaseModelEntity", "m_clrRender");
        Utilities.SetStateChanged(playerPawnValue, "CCSPlayer_ViewModelServices", "m_hViewModel");

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

    private static MemoryFunctionVoid<CBaseEntity, string, int, float, float>? CBaseEntity_EmitSoundParamsFunc = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
    new("\\x48\\x8B\\xC4\\x48\\x89\\x58\\x2A\\x48\\x89\\x70\\x2A\\x55\\x57\\x41\\x56\\x48\\x8D\\xA8\\x2A\\x2A\\x2A\\x2A\\x48\\x81\\xEC\\x2A\\x2A\\x2A\\x2A\\x45\\x33\\xF6") :
    new("\\x48\\xB8\\x2A\\x2A\\x2A\\x2A\\x2A\\x2A\\x2A\\x2A\\x55\\x48\\x89\\xE5\\x41\\x55\\x41\\x54\\x49\\x89\\xFC\\x53\\x48\\x89\\xF3");

    public static void EmitSound(CBaseEntity entity, string soundEventName, int pitch = 1, float volume = 1f, float delay = 1f)
    {
        if (entity is null
        || entity.IsValid is not true
        || string.IsNullOrEmpty(soundEventName) is true
        || CBaseEntity_EmitSoundParamsFunc is null) return;

        CBaseEntity_EmitSoundParamsFunc.Invoke(entity, soundEventName, pitch, volume, delay);
    }

    public static void SpawnParticle(string filename, Vector pos)
    {
        CParticleSystem? particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");

        if (particle == null)
            return;

        particle.EffectName = filename;

        particle.DispatchSpawn();
        particle.AcceptInput("Start");
        particle.Teleport(pos);

        // causes crashes
        // Server.RunOnTick(Server.TickCount + 64, () =>
        // {
        //     if (particle == null || !particle.IsValid)
        //         return;

        //     particle.Remove();
        //     Server.PrintToChatAll("removed");
        // });
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

public static class TemConfigExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public static JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions;

    /// <summary>
    /// Gets the configuration file path
    /// </summary>
    /// <typeparam name="T">Type of the plugin configuration.</typeparam>
    /// <param name="_">Current configuration instance</param>
    public static string GetConfigPath<T>(this T _)
    {
        string assemblyName = typeof(T).Assembly.GetName().Name ?? string.Empty;
        return Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "configs", "plugins", assemblyName, $"{assemblyName}.json");
    }

    /// <summary>
    /// Updates the configuration file
    /// </summary>
    /// <typeparam name="T">Type of the plugin configuration.</typeparam>
    /// <param name="config">Current configuration instance</param>
    public static void Update<T>(this T config)
    {
        var configPath = config.GetConfigPath();

        try
        {
            using var stream = new FileStream(configPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(JsonSerializer.Serialize(config, JsonSerializerOptions));
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update configuration file at '{configPath}'.", ex);
        }
    }

    /// <summary>
    /// Reloads the configuration file and updates current configuration instance.
    /// </summary>
    /// <typeparam name="T">Type of the plugin configuration.</typeparam>
    /// <param name="config">Current configuration instance</param>
    public static void Reload<T>(this T config)
    {
        var configPath = config.GetConfigPath();

        try
        {
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Configuration file '{configPath} not found.");
            }

            var configContent = File.ReadAllText(configPath);

            var newConfig = JsonSerializer.Deserialize<T>(configContent)
                ?? throw new JsonException($"Deserialization failed for configuration file '{configPath}'.");

            foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanWrite)
                {
                    property.SetValue(config, property.GetValue(newConfig));
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to reload configuration file at '{configPath}'.", ex);
        }
    }
}