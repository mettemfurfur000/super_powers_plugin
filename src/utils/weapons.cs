using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

public static class WeaponHelpers
{
    public static List<(int, string)> types =
    [
        (2, "rifle"),
        (1, "smg"),
        (1, "heavy"),
        (1, "sniper"),
    ];

    public static List<(int, string)> rifles =
    [
        (3, "weapon_galilar"),
        (3, "weapon_famas"),
        (2, "weapon_m4a1"),
        (2, "weapon_aug"),
        (1, "weapon_sg556"),
        (1, "weapon_m4a1_silencer"),
        (1, "weapon_ak47"),
    ];

    public static List<(int, string)> smgs =
    [
        (3, "weapon_bizon"),
        (3, "weapon_p90"),
        (2, "weapon_ump45"),
        (2, "weapon_mp5sd"),
        (1, "weapon_mp7"),
        (1, "weapon_mp9"),
        (1, "weapon_mac10"),
    ];

    public static List<(int, string)> heavies =
    [
        (4, "weapon_nova"),
        (3, "weapon_mag7"),
        (3, "weapon_sawedoff"),
        (2, "weapon_negev"),
        (2, "weapon_xm1014"),
        (1, "weapon_m249"),
    ];

    public static List<(int, string)> snipers =
    [
        (3, "weapon_ssg08"),
        (2, "weapon_awp"),
        (1, "weapon_g3sg1"),
        (1, "weapon_scar20"),
    ];


    public static List<(int, string)> pistols =
    [
        (4, "weapon_hkp2000"),
        (4, "weapon_usp_silencer"),
        (4, "weapon_glock"),
        (3, "weapon_fiveseven"),
        (3, "weapon_tec9"),
        (2, "weapon_p250"),
        (1, "weapon_cz75a"),
        (1, "weapon_elite"),
        (1, "weapon_revolver"),
        (1, "weapon_deagle"),
    ];

    public static (int, T)? GetWeighted<T>(List<(int, T)> list)
    {
        int totalWeight = list.Sum(x => x.Item1);
        int random = Random.Shared.Next(totalWeight);

        for (int i = 0; i < list.Count; i++)
        {
            if (random < list[i].Item1)
                return list[i];
            else
                random -= list[i].Item1;
        }

        return null;
    }

    public static void SingleItemRoll(CCSPlayerController user, string weapon, string readName, float probabilityPercentage, bool chatFeedback = false)
    {
        if (Random.Shared.NextSingle() < probabilityPercentage)
        {
            user.GiveNamedItem(weapon);
            if (chatFeedback)
                user.PrintToChat($"{GetRarityString(1, (int)(1 / probabilityPercentage), "+" + readName)}");
        }
    }

    public static void RollAction(CCSPlayerController user, Action action, float probabilityPercentage)
    {
        if (Random.Shared.NextSingle() < probabilityPercentage)
            action.Invoke();
    }

    public static char GetRarityColor(float probabilityPercentage)
    {
        if (probabilityPercentage < 0.10)
            return ChatColors.Red;
        if (probabilityPercentage < 0.20)
            return ChatColors.Purple;
        if (probabilityPercentage < 0.40)
            return ChatColors.Blue;
        if (probabilityPercentage < 0.60)
            return ChatColors.Green;
        return ChatColors.BlueGrey;
    }

    public static string GetRarityString(int weight, int total, string item)
    {
        float prob = weight / (float)total;
        return $"{GetRarityColor(prob)}{item} {ChatColors.Default}({(int)(prob * 100)}%)";
    }
}