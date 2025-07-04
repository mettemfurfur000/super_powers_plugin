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
        // Rarity = PowerRarity.Rare;
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
            if (chatFeedback)
                user.PrintToChat($"Main weapon type: {WeaponHelpers.GetRarityString(main_type.Value.Item1, WeaponHelpers.types.Sum(x => x.Item1), NiceText.FirstUpper(main_type.Value.Item2))}");

            if (chatFeedback)
            {
                if (main_type.Value.Item2 == "rifle") user.PrintToChat($"Rifle: {WeaponHelpers.GetRarityString(main_weapon.Value.Item1, WeaponHelpers.rifles.Sum(x => x.Item1), NiceText.FirstUpper(main_weapon.Value.Item2))}");
                if (main_type.Value.Item2 == "smg") user.PrintToChat($"SMG: {WeaponHelpers.GetRarityString(main_weapon.Value.Item1, WeaponHelpers.smgs.Sum(x => x.Item1), NiceText.FirstUpper(main_weapon.Value.Item2))}");
                if (main_type.Value.Item2 == "heavy") user.PrintToChat($"Heavy: {WeaponHelpers.GetRarityString(main_weapon.Value.Item1, WeaponHelpers.heavies.Sum(x => x.Item1), NiceText.FirstUpper(main_weapon.Value.Item2))}");
                if (main_type.Value.Item2 == "sniper") user.PrintToChat($"Sniper: {WeaponHelpers.GetRarityString(main_weapon.Value.Item1, WeaponHelpers.snipers.Sum(x => x.Item1), NiceText.FirstUpper(main_weapon.Value.Item2))}");
            }
            // select a pistol
            var pistol_selected = weightedSelection ? WeaponHelpers.GetWeighted(WeaponHelpers.pistols) : WeaponHelpers.pistols[Random.Shared.Next(WeaponHelpers.pistols.Count)]; ;

            user.GiveNamedItem(pistol_selected!.Value.Item2);
            if (chatFeedback)
                user.PrintToChat($"Pistol: {WeaponHelpers.GetRarityString(pistol_selected!.Value.Item1, WeaponHelpers.pistols.Sum(x => x.Item1), NiceText.FirstUpper(pistol_selected.Value.Item2))}");

            // roll utils and stuff
            WeaponHelpers.SingleItemRoll(user, "weapon_decoy", "Decoy", chance_decoy);
            WeaponHelpers.SingleItemRoll(user, "weapon_hegrenade", "HE", chance_hegrenade);
            WeaponHelpers.SingleItemRoll(user, "weapon_incgrenade", "Molly but for betas", chance_incgrenade);
            WeaponHelpers.SingleItemRoll(user, "weapon_molotov", "Molly", chance_molly);
            WeaponHelpers.SingleItemRoll(user, "weapon_flashbang", "Flash", chance_flash);
            WeaponHelpers.SingleItemRoll(user, "weapon_flashbang", "Flash", chance_flash);
            WeaponHelpers.SingleItemRoll(user, "weapon_smokegrenade", "Smoke", chance_smoke);

            WeaponHelpers.SingleItemRoll(user, "weapon_healthshot", "Health shot", chance_health);
            WeaponHelpers.SingleItemRoll(user, "weapon_taser", "Taser", chance_taser);

            // dont forget the armor
            WeaponHelpers.RollAction(user, () =>
            {
                if (chatFeedback)
                    user.PrintToChat($"+{WeaponHelpers.GetRarityString((int)(100 * kevlarChance), 100, "Armor")}");

                user.PlayerPawn.Value!.ArmorValue = kevlarIsAlsoRandom ? 1 + Random.Shared.Next(100) : 100;
                Utilities.SetStateChanged(user.PlayerPawn.Value!, "CCSPlayerPawn", "m_ArmorValue");

                WeaponHelpers.RollAction(user, () =>
                 {
                     if (chatFeedback)
                         user.PrintToChat($"+{WeaponHelpers.GetRarityString((int)(100 * helmetChance), 100, "Helmet")}");
                     new CCSPlayer_ItemServices(user.PlayerPawn.Value!.ItemServices!.Handle).HasHelmet = true;
                     Utilities.SetStateChanged(user.PlayerPawn.Value, "CCSPlayer_ItemServices", "m_bHasHelmet");
                 }, helmetChance);

            }, kevlarChance);

            if (user.TeamNum == (byte)CsTeam.Terrorist)
                user.GiveNamedItem("weapon_c4");

            if (user.TeamNum == (byte)CsTeam.CounterTerrorist)
            {
                WeaponHelpers.RollAction(user, () =>
                {
                    if (chatFeedback)
                        user.PrintToChat($"+{WeaponHelpers.GetRarityString((int)(100 * helmetChance), 100, "Helmet")}");
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
