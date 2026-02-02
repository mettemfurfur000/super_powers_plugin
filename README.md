# powers and shi

this plugin is cool but still sucks i think

## problum

both charge_jump and super_jump wont work without `sv_legacy_jump 1` cuz jump events are not gettink produced without it

## curent power list

copied from `sp_list` output (and edited for readability)
```
dormant_power 				Internal use only
bot_disguise 				Disguise as a bot (to a certain point)
bot_guesser 				Allows to kick bots each round
banana 						Spawns a banana each round, not edible
bonus_health 				+150 HP on the start of each round
bonus_armor 				Obtain 250 armor each round, head armor not included
instant_defuse 				Defuse bombs instantly (even withot defuse kit)
instant_plant 				Plant a bomb with no delay
infinite_ammo 				Zeus included, nades not included
super_speed 				Increased walking speed (2.8)
headshot_immunity 			Calcels all headshots, landed on your head
infinite_money 				Near infinite supply of money
nuke_nades 					HE grenades, but 10 times more explosive
evil_aura 					Slowly harm enemies close to you. Can't kill
damage_bonus 				All your damage is multiplied by 2
vampirism 					Gain 20% of dealt damage, annoying sounds included
super_jump 					Look up and jump to get 2 times higher
invisibility 				I cant really see you
explosion_upon_death 		Explode on death, dealing 125 damage in a 500 units radius
regeneration 				Regenerate 10 HP if less than 75 every 2 seconds
warp_peek 					Warp back in time when hit. Only position is saved
snowballing 				Each kill will give you 25 more HP and 10% more damage. Limited to 300 HP and 100% bonus damage
charge_jump 				Jump while crouching to make a leap forward
rage_mode 					When a player gets 3 Kills, he enters 'rage mode,' gaining speed, damage boost, and temporary invincibility
healing_zeus 				zap your teammates to set their health to 75
flash_of_disability 		enemies have their powers disabled if you flash them
poisoned_smoke 				your smoke poisons anyone in it, 2 damage per second
damage_loss 				50% chance to ignore incoming damage event
instant_nades 				Reduce grenade and flash fuse by 4 times
pacifism 					On round start, gain invincibility until you start dealing damage
rebirth 					Respawn at your last death location. If survived, spawn with yout team as before
the_sacrifice 				+50 HP to all teammates on your death
talisman 					if k/d is below 1, gain 2500$
biocoded_weapons 			Only you can use weapons you bought
eternal_nade 				Once your grenade detonates, you get it back
```
some powers ar blacklisted for certain teams or disabled completely by default

## commands

copied from `sp_help` output
```
Availiable commands:
  sp_help                                                - should help in most cases
  sp_add <player> <power> (now)                          - adds power to player
  sp_add_team [t,ct] <power> (now)                       - adds power to all players of team
  sp_remove <player> <power> (now)                       - removes power from player
  sp_remove_team [t,ct]  <power> (now)                   - removes power from all players of team
  sp_list <player>                                       - lists availiable powers
  sp_mode [normal, random]                               - sets a special gamemode
flag 'now' triggers the power immediaty
Advanced commands:
  sp_status                                              - prints status of all powers and its users
  sp_inspect <power>                                     - prints info about power and its parameters
  sp_reconfigure <power> <power> [key1] [value1] ...     - reconfigures power
Special:
  sp_signal / signal / s <any input>                     - pass a signal of arbitrary data to the plugin system
```

you can also use flag `force` instead of `now` to set the power regardless of team requirements or if power is disabled, but i never said anything like that ok?

# config

configuration is generated reflectivly from powers private fields, pretti cool i think

# buildink

don mind my makefile here i use it with my msys2 developend enviorent

make sure to have dotnet installed and compile it inside of a plugin folder in cs2 on either linux or windows platforms so dotnet finds de dll needed for linking and stuff
