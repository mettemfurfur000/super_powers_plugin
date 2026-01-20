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

public class BonusHealth : BasePower
{
    public BonusHealth()
    {
        Triggers = [typeof(EventRoundStart)];
        Price = 2000;
        Rarity = "Common";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            pawn.Health = cfg_bonus;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
        return HookResult.Continue;
    }


    public override string GetDescriptionColored() => "+" + StringHelpers.Green((cfg_bonus - 100).ToString() + " Health ") + "on the start of the round";
    public int cfg_bonus = 250;
}

