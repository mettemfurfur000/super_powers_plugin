using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

public class RandomLoadout : BasePower
{
    public RandomLoadout()
    {
        Triggers = [typeof(EventRoundStart)];

        // Price = 10000;
        // Rarity = "Rare";
        NoShop = true;
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        Users.ForEach(user =>
        {
            var pawn = user.PlayerPawn.Value!;

            // first decide what type of main weapon to give
            var main_type = WeaponHelpers.GetWeighted(WeaponHelpers.types)!;

            (int, string)? main_weapon = null;

            main_weapon = main_type.Value.Item2 switch // weapon type
            {
                "rifle" => WeaponHelpers.GetWeighted(WeaponHelpers.rifles),
                "smg" => WeaponHelpers.GetWeighted(WeaponHelpers.smgs),
                "heavy" => WeaponHelpers.GetWeighted(WeaponHelpers.heavies),
                "sniper" => WeaponHelpers.GetWeighted(WeaponHelpers.snipers),
                _ => null,
            };

            user.GiveNamedItem("weapon_knife");
            user.RemoveWeapons();

            // give it
            user.GiveNamedItem(main_weapon!.Value.Item2);
            if (cfg_chatFeedback)
                user.PrintIfShould($"Main weapon type: {WeaponHelpers.GetRarityString(main_type.Value.Item1, WeaponHelpers.types.Sum(x => x.Item1), StringHelpers.FirstUpper(main_type.Value.Item2))}");

            if (cfg_chatFeedback)
            {
                if (main_type.Value.Item2 == "rifle") user.PrintIfShould($"Rifle: {WeaponHelpers.GetRarityString(main_weapon.Value.Item1, WeaponHelpers.rifles.Sum(x => x.Item1), StringHelpers.FirstUpper(main_weapon.Value.Item2))}");
                if (main_type.Value.Item2 == "smg") user.PrintIfShould($"SMG: {WeaponHelpers.GetRarityString(main_weapon.Value.Item1, WeaponHelpers.smgs.Sum(x => x.Item1), StringHelpers.FirstUpper(main_weapon.Value.Item2))}");
                if (main_type.Value.Item2 == "heavy") user.PrintIfShould($"Heavy: {WeaponHelpers.GetRarityString(main_weapon.Value.Item1, WeaponHelpers.heavies.Sum(x => x.Item1), StringHelpers.FirstUpper(main_weapon.Value.Item2))}");
                if (main_type.Value.Item2 == "sniper") user.PrintIfShould($"Sniper: {WeaponHelpers.GetRarityString(main_weapon.Value.Item1, WeaponHelpers.snipers.Sum(x => x.Item1), StringHelpers.FirstUpper(main_weapon.Value.Item2))}");
            }
            // select a pistol
            var pistol_selected = cfg_weightedSelection ? WeaponHelpers.GetWeighted(WeaponHelpers.pistols) : WeaponHelpers.pistols[Random.Shared.Next(WeaponHelpers.pistols.Count)]; ;

            user.GiveNamedItem(pistol_selected!.Value.Item2);
            if (cfg_chatFeedback)
                user.PrintIfShould($"Pistol: {WeaponHelpers.GetRarityString(pistol_selected!.Value.Item1, WeaponHelpers.pistols.Sum(x => x.Item1), StringHelpers.FirstUpper(pistol_selected.Value.Item2))}");

            // roll utils and stuff
            WeaponHelpers.SingleItemRoll(user, "weapon_decoy", "Decoy", cfg_chance_decoy);
            WeaponHelpers.SingleItemRoll(user, "weapon_hegrenade", "HE", cfg_chance_hegrenade);
            WeaponHelpers.SingleItemRoll(user, "weapon_incgrenade", "Molly but for betas", cfg_chance_incgrenade);
            WeaponHelpers.SingleItemRoll(user, "weapon_molotov", "Molly", cfg_chance_molly);
            WeaponHelpers.SingleItemRoll(user, "weapon_flashbang", "Flash", cfg_chance_flash);
            WeaponHelpers.SingleItemRoll(user, "weapon_flashbang", "Flash", cfg_chance_flash);
            WeaponHelpers.SingleItemRoll(user, "weapon_smokegrenade", "Smoke", cfg_chance_smoke);

            WeaponHelpers.SingleItemRoll(user, "weapon_healthshot", "Health shot", cfg_chance_health);
            WeaponHelpers.SingleItemRoll(user, "weapon_taser", "Taser", cfg_chance_taser);

            // dont forget the armor
            WeaponHelpers.RollAction(user, () =>
            {
                if (cfg_chatFeedback)
                    user.PrintIfShould($"+{WeaponHelpers.GetRarityString((int)(100 * cfg_kevlarChance), 100, "Armor")}");

                user.PlayerPawn.Value!.ArmorValue = cfg_kevlarIsAlsoRandom ? 1 + Random.Shared.Next(100) : 100;
                Utilities.SetStateChanged(user.PlayerPawn.Value!, "CCSPlayerPawn", "m_ArmorValue");

                WeaponHelpers.RollAction(user, () =>
                 {
                     if (cfg_chatFeedback)
                         user.PrintIfShould($"+{WeaponHelpers.GetRarityString((int)(100 * cfg_helmetChance), 100, "Helmet")}");
                     new CCSPlayer_ItemServices(user.PlayerPawn.Value!.ItemServices!.Handle).HasHelmet = true;
                     Utilities.SetStateChanged(user.PlayerPawn.Value, "CCSPlayer_ItemServices", "m_bHasHelmet");
                 }, cfg_helmetChance);

            }, cfg_kevlarChance);

            if (user.TeamNum == (byte)CsTeam.Terrorist)
                user.GiveNamedItem("weapon_c4");

            if (user.TeamNum == (byte)CsTeam.CounterTerrorist)
            {
                WeaponHelpers.RollAction(user, () =>
                {
                    if (cfg_chatFeedback)
                        user.PrintIfShould($"+{WeaponHelpers.GetRarityString((int)(100 * cfg_helmetChance), 100, "Helmet")}");
                    new CCSPlayer_ItemServices(user.PlayerPawn.Value!.ItemServices!.Handle).HasDefuser = true;
                    Utilities.SetStateChanged(user.PlayerPawn.Value, "CCSPlayer_ItemServices", "m_bHasDefuser");
                }, cfg_defuserChance);
            }

            // block their buying capabilities
            pawn.InBuyZone = false;
            buyspamactive.Add(user);

            TemUtils.__plugin?.AddTimer(cfg_denyBuyTime, () =>
            {
                if (!pawn.IsValid)
                    return;
                pawn.InBuyZone = false;
                pawn.WasInBuyZone = false;
                buyspamactive.Remove(user);
            });

        });

        return HookResult.Continue;
    }

    public override void Update()
    {
        buyspamactive.RemoveAll(s => s == null || !s.IsValid || s.Connected != PlayerConnectedState.PlayerConnected);
        buyspamactive.ForEach(user => user.PlayerPawn.Value!.InBuyZone = false);
    }

    public List<CCSPlayerController> buyspamactive = [];

    public bool cfg_weightedSelection = true;
    public float cfg_denyBuyTime = 21f;

    public float cfg_kevlarChance = 0.75f;
    public float cfg_helmetChance = 0.75f;
    public float cfg_defuserChance = 0.75f;

    public bool cfg_kevlarIsAlsoRandom = false;
    public bool cfg_chatFeedback = false;

    public float cfg_chance_decoy = 0.25f;
    public float cfg_chance_hegrenade = 0.5f;
    public float cfg_chance_incgrenade = 0.25f;
    public float cfg_chance_molly = 0.25f;
    public float cfg_chance_flash = 0.5f;
    public float cfg_chance_health = 0.25f;
    public float cfg_chance_taser = 0.5f;
    public float cfg_chance_smoke = 0.5f;

    public override string GetDescription() => $"todo";
}
