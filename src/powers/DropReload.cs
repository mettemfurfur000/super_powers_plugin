using System;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Threading;
using System.Linq;
using CounterStrikeSharp.API.Modules.Entities;
using System.Data.Common;

using super_powers_plugin.src;

public class DropReload : BasePower
{
    public DropReload()
    {
        Triggers = [typeof(EventItemRemove)];
        Price = 4500;
        Rarity = "Uncommon";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        EventItemRemove itemRemoveEvent = (EventItemRemove)gameEvent;
        var user = Users.FirstOrDefault(u => u.PlayerPawn.Value == itemRemoveEvent.Userid);
        if (user == null || !user.IsValid || !user.PawnIsAlive)
            return HookResult.Continue;

        // drop reload - dropping your empty weapon fully reloads your second weapon

        if (itemRemoveEvent.Item == "weapon_knife" || itemRemoveEvent.Item == "weapon_c4")
            return HookResult.Continue;

        var pawn = user.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return HookResult.Continue;
        
        var activeWeapon = pawn.WeaponServices!.ActiveWeapon.Value;

        if (activeWeapon == null || !activeWeapon.IsValid)
            return HookResult.Continue;
        
        if (activeWeapon.Clip1 > 0)
            return HookResult.Continue;
        
        var weapons = pawn.WeaponServices!.MyWeapons.Where(w => w.Value != null && w.Value.IsValid && w.Value != activeWeapon).ToList();
        if (weapons.Count < 2)
            return HookResult.Continue;
        
        var secondWeapon = weapons[0].Value;
        if (secondWeapon == null || !secondWeapon.IsValid)
            return HookResult.Continue;
        
        secondWeapon.Clip1 = secondWeapon.VData!.MaxClip1;

        return HookResult.Continue;
    }


    // public override string GetDescriptionColored() => "+" + StringHelpers.Green((cfg_bonus - 100).ToString() + " Health ") + "on the start of the round";
    // public int cfg_bonus = 250;
}

