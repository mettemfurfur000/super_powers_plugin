using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.VisualBasic;

namespace super_powers_plugin.src;

public class TemUtils
{
    public static string Formatter(string message, char color)
    {
        //return $"[{ChatColors.Gold} Super Powers {ChatColors.Default}] {color} {message}";
        return $"[Super Powers] {message}";
        // return message;
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

    public static void InformValueChanged(CCSPlayerController player, int amount, string reason)
    {
        string theme = " " + (amount < 0 ? ChatColors.LightRed : ChatColors.Lime);
        player.PrintToChat(theme + (amount < 0 ? "-$" : "+$") + Math.Abs(amount) + $" {ChatColors.White} " + reason);
    }

    public static bool AttemptPaidAction(CCSPlayerController player, int amount, string object_name, Action a)
    {
        if (player.InGameMoneyServices!.Account < amount && amount > 0)
        {
            player.PrintToChat($" {ChatColors.LightRed}Not enough money to buy {object_name}");
            player.ExecuteClientCommand("play sounds/ui/weapon_cant_buy.vsnd");
            return false;
        }

        a.Invoke(); // in case opf an exception at least user will have their moners intact

        player.InGameMoneyServices!.Account -= amount;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

        if (amount > 0)
            TemUtils.InformValueChanged(player, -amount, $"for buying {object_name}");
        else
            player.PrintToChat($"{object_name} Acquired");

        player.ExecuteClientCommand("play sounds/ui/panorama/claim_gift_01.vsnd");

        return true;
    }

    public static void GiveMoney(CCSPlayerController player, int amount, string reason)
    {
        player.InGameMoneyServices!.Account += amount;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

        TemUtils.InformValueChanged(player, amount, reason);
    }

    public static String WildCardToRegular(String value)
    {
        return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
    }

    unsafe static float* g_DefaultViewVectors = null;
    /*
        static CViewVectors g_DefaultViewVectors(
	        Vector( 0, 0, 64 ),			//VEC_VIEW (m_vView)
	        Vector( 0, 0, 28 ),			//VEC_DUCK_VIEW		(m_vDuckView)
        
	        Vector(-16, -16, 0 ),		//VEC_HULL_MIN (m_vHullMin)
	        Vector( 16,  16,  72 ),		//VEC_HULL_MAX (m_vHullMax)
        
	        Vector(-16, -16, 0 ),		//VEC_DUCK_HULL_MIN (m_vDuckHullMin)
	        Vector( 16,  16,  36 ),		//VEC_DUCK_HULL_MAX	(m_vDuckHullMax)
        
	        Vector(-10, -10, -10 ),		//VEC_OBS_HULL_MIN	(m_vObsHullMin)
	        Vector( 10,  10,  10 ),		//VEC_OBS_HULL_MAX	(m_vObsHullMax)
        
	        Vector( 0, 0, 14 )			//VEC_DEAD_VIEWHEIGHT (m_vDeadViewHeight)
        );				
    */

    const float d_hull_width = 32;
    const float d_hull_height = 72;

    unsafe static System.Numerics.Vector3* m_vView = null;
    unsafe static System.Numerics.Vector3* m_vDuckView = null;

    unsafe static System.Numerics.Vector3* m_vHullMin = null; // normal player hull
    unsafe static System.Numerics.Vector3* m_vHullMax = null;

    unsafe static System.Numerics.Vector3* m_vDuckHullMin = null; // ducking hull
    unsafe static System.Numerics.Vector3* m_vDuckHullMax = null;

    unsafe static System.Numerics.Vector3* m_vObsHullMin = null; // observer hull
    unsafe static System.Numerics.Vector3* m_vObsHullMax = null;

    unsafe static System.Numerics.Vector3* m_vDeadViewHeight = null;

    public static byte[] default_values = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x42, 0x00, 0x00, 0x80, 0xC1, 0x00, 0x00, 0x80, 0xC1, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x41, 0x00, 0x00, 0x80, 0x41, 0x00, 0x00, 0x90, 0x42, 0x00, 0x00, 0x80, 0xC1, 0x00, 0x00, 0x80, 0xC1, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x41, 0x00, 0x00, 0x80, 0x41, 0x00, 0x00, 0x58, 0x42, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x38, 0x42, 0x00, 0x00, 0x20, 0xC1, 0x00, 0x00, 0x20, 0xC1, 0x00, 0x00, 0x20, 0xC1, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x60, 0x41];

    public static void SetGlobalPlayerHull(float scale)
    {
        unsafe
        {
            if (g_DefaultViewVectors == null)
            {
                float* ptr = (float*)NativeAPI.FindSignature(Addresses.ServerPath,
                    // "00 00 00 00 00 00 00 00 00 00 80 42 00 00 80 C1 00 00 80 C1 00 00 00 00 00 00 80 41 00 00 80 41 00 00 90 42 00 00 80 C1 00 00 80 C1 00 00 00 00 00 00 80 41 00 00 80 41 00 00 58 42 00 00 00 00 00 00 00 00 00 00 38 42 00 00 20 C1 00 00 20 C1 00 00 20 C1 00 00 20 41 00 00 20 41 00 00 20 41 00 00 00 00 00 00 00 00 00 00 60 41"
                    "10 08 00 00 40 08 00 00 50 6D 50 C9 5F 02 00 00"
                    );

                if (ptr == null)
                {
                    Server.PrintToChatAll("not found");
                    return;
                }

                ptr += 4; // skip 4 floats we wer aiming for to get our juicy oily barbarians to start digging in em

                g_DefaultViewVectors = ptr;

                m_vView = (System.Numerics.Vector3*)(ptr + 0);
                m_vHullMin = (System.Numerics.Vector3*)(ptr + 3);
                m_vHullMax = (System.Numerics.Vector3*)(ptr + 6);
                m_vDuckHullMin = (System.Numerics.Vector3*)(ptr + 9);
                m_vDuckHullMax = (System.Numerics.Vector3*)(ptr + 12);
                m_vDuckView = (System.Numerics.Vector3*)(ptr + 15);
                m_vObsHullMin = (System.Numerics.Vector3*)(ptr + 18);
                m_vObsHullMax = (System.Numerics.Vector3*)(ptr + 21);
                m_vDeadViewHeight = (System.Numerics.Vector3*)(ptr + 24);
            }

            if (scale == 1.0f)
            {
                byte* bptr = (byte*)g_DefaultViewVectors;
                for (int i = 0; i < default_values.Length; i++)
                    bptr[i] = default_values[i];
            }
            else
            {
                m_vHullMin->X = -d_hull_width / 2 * scale;
                m_vHullMin->Y = -d_hull_width / 2 * scale;
                // m_vHullMin->Z = 0;

                m_vHullMax->X = d_hull_width / 2 * scale;
                m_vHullMax->Y = d_hull_width / 2 * scale;
                m_vHullMax->Z = d_hull_height * scale;

                m_vDuckHullMin->X = -d_hull_width / 2 * scale;
                m_vDuckHullMin->Y = -d_hull_width / 2 * scale;
                // m_vDuckHullMin->Z = 0;

                m_vDuckHullMax->X = d_hull_width / 2 * scale;
                m_vDuckHullMax->Y = d_hull_width / 2 * scale;
                m_vDuckHullMax->Z = d_hull_height / 2 * scale;

            }

            Server.PrintToChatAll("Set hull max to " + m_vHullMax->ToString());
        }
    }

    public static IEnumerable<CCSPlayerController> SelectPlayers(string name_pattern)
    {
        if (name_pattern.StartsWith("@"))
        {
            ulong steamid64 = ulong.Parse(name_pattern.Replace("@", ""));

            var gamer = Utilities.GetPlayerFromSteamId(steamid64);
            IEnumerable<CCSPlayerController> ret = [];

            if (gamer == null)
                return ret;

            ret = ret.Append(gamer);
            return ret;
        }

        string r_pattern = WildCardToRegular(name_pattern);

        return Utilities.GetPlayers()
            .Where(player => Regex.IsMatch(player.PlayerName, r_pattern, RegexOptions.IgnoreCase));
    }

    public static ulong Combine(uint a, uint b)
    {
        uint ua = (uint)a;
        ulong ub = (uint)b;
        return ub << 32 | ua;
    }

    // public static ulong GetActiveWeaponUserSteamId64(CCSPlayerController user)
    // {
    //     return Combine(user.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.OriginalOwnerXuidLow, user.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.OriginalOwnerXuidHigh);
    // }

    // public static IEnumerable<CCSPlayerController> SelectPlayersBotsIncluded(string name_pattern)
    // {
    //     string r_pattern = WildCardToRegular(name_pattern);

    //     return Utilities.GetPlayers().Where(player => Regex.IsMatch(player.PlayerName, r_pattern, RegexOptions.IgnoreCase));
    // }

    // public static List<CCSPlayerController> GetPlayers()
    // {
    //     List<CCSPlayerController> players = new();

    //     for (int i = 0; i < Server.MaxPlayers; i++)
    //     {
    //         var controller = Utilities.GetPlayerFromSlot(i);

    //         if (controller == null || !controller.IsValid || controller.Connected != PlayerConnectedState.PlayerConnected)
    //             continue;

    //         players.Add(controller);
    //     }

    //     return players;
    // }

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

    public static float CalcDistance(Vector v1, Vector v2)
    {
        return (float)Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2) + Math.Pow(v1.Z - v2.Z, 2));
    }

    public static string? InspectPowerReflective(BasePower power, Type type)
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

    public static void ParseConfigReflective(BasePower power, Type type, Dictionary<string, string> cfg)
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

    public static void ParseConfigReflectiveRecursive(BasePower power, Type iter_type, Dictionary<string, string> cfg_unresolved)
    {
        if (cfg_unresolved.Count == 0)
            return;
        Dictionary<string, string> next_unresolved = [];

        // Log($"reading {iter_type}");

        foreach (var field in cfg_unresolved)
        {
            var fieldInfo = iter_type.GetField(field.Key, BindingFlags.Instance | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                next_unresolved.Add(field.Key, field.Value);
                continue;
            }

            try
            {
                try
                {
                    fieldInfo.SetValue(power, Convert.ChangeType(field.Value, fieldInfo.FieldType));
                }
                catch (InvalidCastException ex) { TemUtils.AlertError($"Error occured while processing {iter_type} : Failed to convert value for {fieldInfo.Name}: {ex.Message}"); }
                catch (FormatException ex) { TemUtils.AlertError($"Error occured while processing {iter_type} : Invalid format for {fieldInfo.Name}: {ex.Message}"); }
                // Log($"resloved {field.Key}");
            }
            catch
            {
                // aah whatever
            }
        }

        if (iter_type.BaseType != null)
            ParseConfigReflectiveRecursive(power, iter_type.BaseType, next_unresolved);
        else if (next_unresolved.SequenceEqual(cfg_unresolved))
        {
            TemUtils.AlertError("Failed to resolve some fields for '" + iter_type + "', list of them:");
            foreach (var field in next_unresolved)
                TemUtils.AlertError(field.Key + " : " + field.Value);
            AlertError("All fields for current type:");
            foreach (var field in iter_type.GetRuntimeFields())
                AlertError(field.ToString()!);

            return;
        }
        // Log($"null base class for {iter_type}");
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

    public static void SetPlayerInvisibilityLevel(CCSPlayerController player, float invisibilityLevel)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            TemUtils.Log("Player pawn is not valid.");
            return;
        }

        int alpha = (int)((1.0f - invisibilityLevel) * 255);
        alpha = alpha > 255 ? 255 : alpha < 0 ? 0 : alpha; // >:3
        var fadeColor = Color.FromArgb(alpha, 255, 255, 255);

        // Server.PrintToChatAll("alpha for " + player.PlayerName + " is " + alpha);

        pawn.Render = fadeColor;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        // Utilities.SetStateChanged(pawn, "CCSPlayer_ViewModelServices", "m_hViewModel");

        // pawn.RenderMode = RenderMode_t.kRenderTransAlpha;
        // Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_nRenderMode");

        // var viewModel = pawn.ViewModelServices!.Pawn.Value;
        // viewModel.Render = fadeColor;
        // Utilities.SetStateChanged(viewModel, "CBaseModelEntity", "m_clrRender");

        // viewModel.RenderMode = RenderMode_t.kRenderTransAlpha;
        // Utilities.SetStateChanged(viewModel, "CBaseModelEntity", "m_nRenderMode");

        var weaponServices = pawn.WeaponServices;
        if (weaponServices != null)
        {
            var activeWeapon = weaponServices.ActiveWeapon.Value;
            if (activeWeapon != null && activeWeapon.IsValid)
            {
                activeWeapon.Render = fadeColor;
                // activeWeapon.ShadowStrength = invisibilityLevel;
                Utilities.SetStateChanged(activeWeapon, "CBaseModelEntity", "m_clrRender");
                // Utilities.SetStateChanged(activeWeapon, "CBaseModelEntity", "m_flShadowStrength");
            }
        }

        var myWeapons = pawn.WeaponServices?.MyWeapons;
        if (myWeapons != null)
            foreach (var gun in myWeapons)
            {
                var weapon = gun.Value;
                if (weapon != null)
                {
                    weapon.Render = fadeColor;
                    // weapon.ShadowStrength = invisibilityLevel;
                    Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
                    // Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_flShadowStrength");
                }
            }
    }

    public static void CleanWeaponOwner(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            TemUtils.Log("Player pawn is not valid.");
            return;
        }

        var weaponServices = pawn.WeaponServices;
        if (weaponServices != null)
        {
            var activeWeapon = weaponServices.ActiveWeapon.Value;
            if (activeWeapon != null && activeWeapon.IsValid)
            {
                var realWeapon = activeWeapon as CCSWeaponBase;

                if (realWeapon == null)
                {
                    // TemUtils.Log("No active weapon found");
                    return;
                }

                realWeapon.AttributeManager.Item.CustomName = "";
                realWeapon.AttributeManager.Item.EntityQuality = 0;

                realWeapon.OriginalOwnerXuidLow = 0;
                realWeapon.OriginalOwnerXuidHigh = 0;

                Utilities.SetStateChanged(realWeapon, "CEconEntity", "m_OriginalOwnerXuidLow");
                Utilities.SetStateChanged(realWeapon, "CEconEntity", "m_OriginalOwnerXuidHigh");
                Utilities.SetStateChanged(realWeapon, "CEconEntity", "m_AttributeManager");
            }
        }

        // return;

        var myWeapons = pawn.WeaponServices?.MyWeapons;
        if (myWeapons != null)
            foreach (var gun in myWeapons)
            {
                var weapon = gun.Value;

                var realWeapon = weapon as CCSWeaponBase;

                if (realWeapon == null)
                {
                    // TemUtils.Log("some weapons wer unavabialb to be cleared of its original owners");
                    return;
                }

                realWeapon.AttributeManager.Item.CustomName = "";
                realWeapon.AttributeManager.Item.EntityQuality = 0;

                realWeapon.OriginalOwnerXuidLow = 0;
                realWeapon.OriginalOwnerXuidHigh = 0;

                Utilities.SetStateChanged(realWeapon, "CEconEntity", "m_OriginalOwnerXuidLow");
                Utilities.SetStateChanged(realWeapon, "CEconEntity", "m_OriginalOwnerXuidHigh");
                Utilities.SetStateChanged(realWeapon, "CEconEntity", "m_AttributeManager");
            }
    }

    // stolen

    public static void UpdatePlayerName(CCSPlayerController player, string name, string? tag = null)
    {
        if (player == null // || player.IsBot
        )
        {
            return;
        }

        if (player.PlayerName != name)
        {
            player.PlayerName = name;
            CounterStrikeSharp.API.Utilities.SetStateChanged(
                player,
                "CBasePlayerController",
                "m_iszPlayerName"
            );
        }

        if (tag == null) tag = "";

        if (player.Clan != tag)
        {
            player.Clan = tag;
            player.ClanName = tag;

            CounterStrikeSharp.API.Utilities.SetStateChanged(
                player,
                "CCSPlayerController",
                "m_szClan"
            );
            CounterStrikeSharp.API.Utilities.SetStateChanged(
                player,
                "CCSPlayerController",
                "m_szClanName"
            );

            var gameRules = GetGameRulesProxy();

            gameRules.GameRules!.NextUpdateTeamClanNamesTime = Server.CurrentTime - 0.01f;
            CounterStrikeSharp.API.Utilities.SetStateChanged(
                gameRules,
                "CCSGameRules",
                "m_fNextUpdateTeamClanNamesTime"
            );
        }

        // force the client to update the player name
        new EventNextlevelChanged(false).FireEventToClient(player);
    }

    public static CCSGameRules GetGameRules()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }

    public static CCSGameRulesProxy GetGameRulesProxy()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()!;
    }


    // private static MemoryFunctionVoid<CBaseEntity, string, int, float, float>? CBaseEntity_EmitSoundParamsFunc = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
    // new("\\x48\\x8B\\xC4\\x48\\x89\\x58\\x2A\\x48\\x89\\x70\\x2A\\x55\\x57\\x41\\x56\\x48\\x8D\\xA8\\x2A\\x2A\\x2A\\x2A\\x48\\x81\\xEC\\x2A\\x2A\\x2A\\x2A\\x45\\x33\\xF6") :
    // new("\\x48\\xB8\\x2A\\x2A\\x2A\\x2A\\x2A\\x2A\\x2A\\x2A\\x55\\x48\\x89\\xE5\\x41\\x55\\x41\\x54\\x49\\x89\\xFC\\x53\\x48\\x89\\xF3");

    // public static void EmitSound(CBaseEntity entity, string soundEventName, int pitch = 1, float volume = 1f, float delay = 1f)
    // {
    //     if (entity is null
    //     || entity.IsValid is not true
    //     || string.IsNullOrEmpty(soundEventName) is true
    //     || CBaseEntity_EmitSoundParamsFunc is null) return;

    //     CBaseEntity_EmitSoundParamsFunc.Invoke(entity, soundEventName, pitch, volume, delay);
    // }

    public static super_powers_plugin? __plugin = null;

    // stolen
    public static void CreateParticle(Vector position, string particleFile, float lifetime, string sound = "", CCSPlayerController? player = null)
    {
        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system")!;

        particle.EffectName = particleFile;
        particle.StartActive = true;
        particle.Teleport(position);
        particle.DispatchSpawn();

        if (player != null)
            particle.AcceptInput("FollowEntity", player.PlayerPawn.Value, particle, "!activator");

        if (!string.IsNullOrEmpty(sound))
            particle.EmitSound(sound);

        __plugin?.AddTimer(lifetime, () =>
            {
                if (particle != null && particle.IsValid)
                    particle.Remove();
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

    public static Tuple<CBaseModelEntity, CBaseModelEntity>? MakePawnGlow(CCSPlayerPawn pawn, byte teamNum, string ColorT = "#ffc800ff", string ColorCT = "#4c00ffff")
    {
        if (pawn.Controller?.Value == null)
            return null;

        CBaseModelEntity? modelGlow = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        CBaseModelEntity? modelRelay = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");

        if (modelGlow == null || modelRelay == null)
            return null;

        if (pawn.CBodyComponent?.SceneNode == null)
            return null;

        string modelName = pawn.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName;

        modelRelay.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(modelRelay.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
        modelRelay.SetModel(modelName);
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;
        modelRelay.DispatchSpawn();

        modelGlow.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(modelGlow.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
        modelGlow.SetModel(modelName);
        modelGlow.Spawnflags = 256u;
        modelGlow.DispatchSpawn();

        // 1 = spectator?
        // 2 = t
        // 3 = ct

        modelGlow.Glow.GlowColorOverride = ColorTranslator.FromHtml(teamNum == 2 ? ColorCT : ColorT);
        modelGlow.Glow.GlowRange = 4096;
        modelGlow.Glow.GlowTeam = teamNum;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 64;

        modelRelay.AcceptInput("FollowEntity", pawn, modelRelay, "!activator");
        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");

        // pairModels[pawn.OriginalController.Value!.Slot] = new Tuple<CBaseModelEntity, CBaseModelEntity>(modelGlow, modelRelay);
        return new Tuple<CBaseModelEntity, CBaseModelEntity>(modelGlow, modelRelay);
    }

    public const int default_velocity_max = 250;

    public static void PowerApplySpeed(List<CCSPlayerController> Users, float value)
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                return;

            pawn.MovementServices!.Maxspeed = value;
            pawn.VelocityModifier = (float)value / default_velocity_max;
        }
    }

    public static void PowerRemoveSpeedModifier(List<CCSPlayerController> Users, CCSPlayerController? player)
    {
        if (player != null)
        {
            if (Users.Contains(player))
            {
                var pawn = player.PlayerPawn.Value;

                if (pawn == null)
                    return;

                pawn.MovementServices!.Maxspeed = default_velocity_max;
                pawn.VelocityModifier = 1;
            }
        }
        else
            Users.ForEach(p =>
            {
                var pawn = p.PlayerPawn.Value;

                if (pawn == null)
                    return;

                pawn.MovementServices!.Maxspeed = default_velocity_max;
                pawn.VelocityModifier = 1;
            });

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
    /// Updates the configuration file
    /// </summary>
    /// <typeparam name="T">Type of the plugin configuration.</typeparam>
    /// <param name="config">Current configuration instance</param>
    public static void ResetConfig<T>(this T config)
    {
        var configPath = config.GetConfigPath();

        try
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete '{configPath}'.", ex);
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