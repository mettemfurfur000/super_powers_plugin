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
        (2, "rifle"),
        (1, "smg"),
        (1, "heavy"),
        (1, "sniper"),
    ];

    public List<(int, string)> rifles =
    [
        (3, "weapon_galilar"),
        (3, "weapon_famas"),
        (2, "weapon_m4a1"),
        (2, "weapon_aug"),
        (1, "weapon_sg556"),
        (1, "weapon_m4a1_silencer"),
        (1, "weapon_ak47"),
    ];

    public List<(int, string)> smgs =
    [
        (3, "weapon_bizon"),
        (3, "weapon_p90"),
        (2, "weapon_ump45"),
        (2, "weapon_mp5sd"),
        (1, "weapon_mp7"),
        (1, "weapon_mp9"),
        (1, "weapon_mac10"),
    ];

    public List<(int, string)> heavies =
    [
        (4, "weapon_nova"),
        (3, "weapon_mag7"),
        (3, "weapon_sawedoff"),
        (2, "weapon_negev"),
        (2, "weapon_xm1014"),
        (1, "weapon_m249"),
    ];

    public List<(int, string)> snipers =
    [
        (3, "weapon_ssg08"),
        (2, "weapon_awp"),
        (1, "weapon_g3sg1"),
        (1, "weapon_scar20"),
    ];


    public List<(int, string)> pistols =
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

    private (int, string)? GetWeighted(List<(int, string)> list)
    {
        if (weightedSelection == false)
            return list[Random.Shared.Next(list.Count)];

        int totalWeight = list.Sum(x => x.Item1);
        int random = Random.Shared.Next(totalWeight);

        for (int i = 0; i < list.Count; i++)
            if (random < list[i].Item1)
                return list[i];
            else
                random -= list[i].Item1;

        return null;
    }

    private void SingleItemRoll(CCSPlayerController user, string weapon, string readName, float probability)
    {
        if (Random.Shared.NextSingle() < probability)
        {
            user.GiveNamedItem(weapon);
            if (chatFeedback)
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
            var pawn = user.PlayerPawn.Value!;

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
            if (chatFeedback)
                user.PrintToChat($"Main weapon type: {GetRarityString(main_type.Value.Item1, types.Sum(x => x.Item1), TemUtils.FirstUpper(main_type.Value.Item2))}");

            if (chatFeedback)
            {
                if (main_type.Value.Item2 == "rifle") user.PrintToChat($"Rifle: {GetRarityString(main_weapon.Value.Item1, rifles.Sum(x => x.Item1), TemUtils.FirstUpper(main_weapon.Value.Item2))}");
                if (main_type.Value.Item2 == "smg") user.PrintToChat($"SMG: {GetRarityString(main_weapon.Value.Item1, smgs.Sum(x => x.Item1), TemUtils.FirstUpper(main_weapon.Value.Item2))}");
                if (main_type.Value.Item2 == "heavy") user.PrintToChat($"Heavy: {GetRarityString(main_weapon.Value.Item1, heavies.Sum(x => x.Item1), TemUtils.FirstUpper(main_weapon.Value.Item2))}");
                if (main_type.Value.Item2 == "sniper") user.PrintToChat($"Sniper: {GetRarityString(main_weapon.Value.Item1, snipers.Sum(x => x.Item1), TemUtils.FirstUpper(main_weapon.Value.Item2))}");
            }
            // select a pistol
            var pistol_selected = GetWeighted(pistols);
            user.GiveNamedItem(pistol_selected!.Value.Item2);
            if (chatFeedback)
                user.PrintToChat($"Pistol: {GetRarityString(pistol_selected!.Value.Item1, pistols.Sum(x => x.Item1), TemUtils.FirstUpper(pistol_selected.Value.Item2))}");

            // roll utils and stuff
            SingleItemRoll(user, "weapon_decoy", "Decoy", chance_decoy);
            SingleItemRoll(user, "weapon_hegrenade", "HE", chance_hegrenade);
            SingleItemRoll(user, "weapon_incgrenade", "Molly but for betas", chance_incgrenade);
            SingleItemRoll(user, "weapon_molotov", "Molly", chance_molly);
            SingleItemRoll(user, "weapon_flashbang", "Flash", chance_flash);
            SingleItemRoll(user, "weapon_flashbang", "Flash", chance_flash);
            SingleItemRoll(user, "weapon_smokegrenade", "Smoke", chance_smoke);

            SingleItemRoll(user, "weapon_healthshot", "Health shot", chance_health);
            SingleItemRoll(user, "weapon_taser", "Taser", chance_taser);

            // dont forget the armor
            RollAction(user, () =>
            {
                if (chatFeedback)
                    user.PrintToChat($"+{GetRarityString((int)(100 * kevlarChance), 100, "Armor")}");

                user.PlayerPawn.Value!.ArmorValue = kevlarIsAlsoRandom ? 1 + Random.Shared.Next(100) : 100;
                Utilities.SetStateChanged(user.PlayerPawn.Value!, "CCSPlayerPawn", "m_ArmorValue");

                RollAction(user, () =>
                {
                    if (chatFeedback)
                        user.PrintToChat($"+{GetRarityString((int)(100 * helmetChance), 100, "Helmet")}");
                    new CCSPlayer_ItemServices(user.PlayerPawn.Value!.ItemServices!.Handle).HasHelmet = true;
                    Utilities.SetStateChanged(user.PlayerPawn.Value, "CCSPlayer_ItemServices", "m_bHasHelmet");
                }, helmetChance);

            }, kevlarChance);

            if (user.TeamNum == (byte)CsTeam.Terrorist)
                user.GiveNamedItem("weapon_c4");

            if (user.TeamNum == (byte)CsTeam.CounterTerrorist)
            {
                RollAction(user, () =>
                {
                    if (chatFeedback)
                        user.PrintToChat($"+{GetRarityString((int)(100 * helmetChance), 100, "Helmet")}");
                    new CCSPlayer_ItemServices(user.PlayerPawn.Value!.ItemServices!.Handle).HasDefuser = true;
                    Utilities.SetStateChanged(user.PlayerPawn.Value, "CCSPlayer_ItemServices", "m_bHasDefuser");
                }, defuserChance);
            }

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
    private float denyBuyTime = 21f;

    private float kevlarChance = 0.75f;
    private float helmetChance = 0.75f;
    private float defuserChance = 0.75f;

    private bool kevlarIsAlsoRandom = false;
    private bool chatFeedback = false;

    private float chance_decoy = 0.25f;
    private float chance_hegrenade = 0.5f;
    private float chance_incgrenade = 0.25f;
    private float chance_molly = 0.25f;
    private float chance_flash = 0.5f;
    private float chance_health = 0.25f;
    private float chance_taser = 0.5f;
    private float chance_smoke = 0.5f;

    public override string GetDescription() => $"todo";
}
