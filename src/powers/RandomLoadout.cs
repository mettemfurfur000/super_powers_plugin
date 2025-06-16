using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace super_powers_plugin.src;

public class RandomLoadout : ISuperPower
{
    public RandomLoadout() => Triggers = [typeof(EventRoundStart)];

    public List<(int, string)> types =
    [
        (4, "rifle"),
        (3, "smg"),
        (2, "heavy"),
        (1, "sniper"),
    ];

    public List<(int, string)> rifles =
    [
        (6, "weapon_galilar"),
        (5, "weapon_famas"),
        (3, "weapon_m4a1"),
        (3, "weapon_aug"),
        (2, "weapon_sg556"),
        (2, "weapon_m4a1_silencer"),
        (1, "weapon_ak47"),
    ];

    public List<(int, string)> smgs =
    [
        (3, "weapon_bizon"),
        (3, "weapon_p90"),
        (3, "weapon_ump45"),
        (3, "weapon_mp5sd"),
        (2, "weapon_mp7"),
        (2, "weapon_mp9"),
        (1, "weapon_mac10"),
    ];

    public List<(int, string)> heavies =
    [
        (4, "weapon_nova"),
        (4, "weapon_negev"),
        (3, "weapon_m249"),
        (2, "weapon_mag7"),
        (2, "weapon_sawedoff"),
        (1, "weapon_xm1014"),
    ];

    public List<(int, string)> snipers =
    [
        (3, "weapon_ssg08"),
        (2, "weapon_g3sg1"),
        (2, "weapon_scar20"),
        (1, "weapon_awp"),
    ];


    public List<(int, string)> pistols =
    [
        (4, "weapon_hkp2000"),
        (4, "weapon_usp_silencer"),
        (4, "weapon_glock"),
        (3, "weapon_fiveseven"),
        (3, "weapon_tec9"),
        (3, "weapon_p250"),
        (2, "weapon_cz75a"),
        (2, "weapon_elite"),
        (1, "weapon_revolver"),
        (1, "weapon_deagle"),
    ];

    private (int, string)? GetWeighted(List<(int, string)> list)
    {
        if (weightedSelection == false)
            return list[Random.Shared.Next(list.Count)];

        int totalWeight = list.Sum(x => x.Item1);
        int random = Random.Shared.Next(totalWeight);

        for (int i = 0; i < list.Count; i++)
            if (random <= list[i].Item1)
                return list[i];
            else
                random -= list[i].Item1;

        return null;
    }

    private static void SingleItemRoll(CCSPlayerController user, string weapon, string readName, float probability)
    {
        if (Random.Shared.NextSingle() < probability)
        {
            user.GiveNamedItem(weapon);
            user.PrintToChat($"{GetRarityString(1, (int)(1 / probability), "+" + readName)}");
        }
    }

    private static void RollAction(CCSPlayerController user, Action action, float probability)
    {
        if (Random.Shared.NextSingle() < probability)
            action.Invoke();
    }

    private static char GetRarityColor(float probability)
    {
        if (probability < 0.10)
            return ChatColors.Red;
        if (probability < 0.20)
            return ChatColors.Purple;
        if (probability < 0.40)
            return ChatColors.Blue;
        if (probability < 0.60)
            return ChatColors.Green;
        return ChatColors.BlueGrey;
    }

    private static string GetRarityString(int weight, int total, string item)
    {
        float prob = weight / (float)total;
        return $"{GetRarityColor(prob)}{item} {ChatColors.Default}({(int)(prob * 100)}%)";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        Users.ForEach(user =>
        {
            // first decide what type of main weapon to give
            var main_type = GetWeighted(types)!;

            (int, string)? main_weapon = null;

            main_weapon = main_type.Value.Item2 switch // weapon type
            {
                "rifle" => GetWeighted(rifles),
                "smg" => GetWeighted(smgs),
                "heavy" => GetWeighted(heavies),
                "sniper" => GetWeighted(snipers),
                _ => null,
            };

            user.GiveNamedItem("weapon_knife");
            user.RemoveWeapons();

            // give it
            user.GiveNamedItem(main_weapon!.Value.Item2);
            user.PrintToChat($"Main weapon type: {GetRarityString(main_type.Value.Item1, types.Sum(x => x.Item1), TemUtils.FirstUpper(main_type.Value.Item2))}");

            if (main_type.Value.Item2 == "rifle") user.PrintToChat($"Rifle: {GetRarityString(main_weapon.Value.Item1, rifles.Sum(x => x.Item1), TemUtils.FirstUpper(main_weapon.Value.Item2))}");
            if (main_type.Value.Item2 == "smg") user.PrintToChat($"SMG: {GetRarityString(main_weapon.Value.Item1, smgs.Sum(x => x.Item1), TemUtils.FirstUpper(main_weapon.Value.Item2))}");
            if (main_type.Value.Item2 == "heavy") user.PrintToChat($"Heavy: {GetRarityString(main_weapon.Value.Item1, heavies.Sum(x => x.Item1), TemUtils.FirstUpper(main_weapon.Value.Item2))}");
            if (main_type.Value.Item2 == "sniper") user.PrintToChat($"Sniper: {GetRarityString(main_weapon.Value.Item1, snipers.Sum(x => x.Item1), TemUtils.FirstUpper(main_weapon.Value.Item2))}");

            // select a pistol
            var pistol_selected = GetWeighted(pistols);
            user.GiveNamedItem(pistol_selected!.Value.Item2);
            user.PrintToChat($"Pistol: {GetRarityString(pistol_selected!.Value.Item1, pistols.Sum(x => x.Item1), TemUtils.FirstUpper(pistol_selected.Value.Item2))}");

            // roll utils and stuff
            SingleItemRoll(user, "weapon_decoy", "Decoy", 0.125f);
            SingleItemRoll(user, "weapon_hegrenade", "HE", 0.25f);
            SingleItemRoll(user, "weapon_incgrenade", "Molly but for betas", 0.25f);
            SingleItemRoll(user, "weapon_molotov", "Molly", 0.25f);
            SingleItemRoll(user, "weapon_flashbang", "Flash", 0.5f);
            SingleItemRoll(user, "weapon_flashbang", "Flash", 0.5f);
            SingleItemRoll(user, "weapon_smokegrenade", "Smoke", 0.5f);

            SingleItemRoll(user, "weapon_healthshot", "Health shot", 0.125f);
            SingleItemRoll(user, "weapon_taser", "Taser", 0.25f);

            // dont forget the armor
            RollAction(user, () =>
            {
                user.PrintToChat($"+{GetRarityString((int)(100 * kevlarChance), 100, "Armor")}");

                user.PlayerPawn.Value!.ArmorValue = kevlarIsAlsoRandom ? 1 + Random.Shared.Next(100) : 100;
                Utilities.SetStateChanged(user.PlayerPawn.Value!, "CCSPlayerPawn", "m_ArmorValue");

                RollAction(user, () =>
                {
                    user.PrintToChat($"+{GetRarityString((int)(100 * helmetChance), 100, "Helmet")}");
                    user.PawnHasHelmet = true;
                    Utilities.SetStateChanged(user, "CCSPlayerController", "m_bPawnHasHelmet");
                }, helmetChance);

            }, kevlarChance);

            if (user.TeamNum == (byte)CsTeam.Terrorist)
                user.GiveNamedItem("weapon_c4");

            if (user.TeamNum == (byte)CsTeam.CounterTerrorist)
            {
                user.PawnHasDefuser = true;
                Utilities.SetStateChanged(user, "CCSPlayerController", "m_bPawnHasDefuser");
            }

            var pawn = user.PlayerPawn.Value!;

            // block their buying capabilities
            pawn.InBuyZone = false;
            buyspamactive.Add(user);

            TemUtils.__plugin?.AddTimer(denyBuyTime, () =>
            {
                pawn.InBuyZone = false;
                pawn.WasInBuyZone = false;
                buyspamactive.Remove(user);
            });

        });

        return HookResult.Continue;
    }

    public override void Update()
    {
        buyspamactive.ForEach(user =>
        {
            user.PlayerPawn.Value!.InBuyZone = false;
        });
    }

    public List<CCSPlayerController> buyspamactive = [];

    private bool weightedSelection = true;
    private float denyBuyTime = 30f;
    private float kevlarChance = 0.75f;
    private float helmetChance = 0.75f;
    private bool kevlarIsAlsoRandom = false;

    public override string GetDescription() => $"todo";
}
