# Super Powers Plugin

A Counter-Strike 2 plugin for [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) that gives players superpowers. Powers can be assigned manually, handed out randomly every round, or bought in a round-start shop.

## Requirements

- Counter-Strike 2 dedicated server
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) **v343+** (the version shipping the native `Trace` API, net10.0 runtime)
- Optional: MySQL server for cross-server player data (SQLite is used by default)

## Installation

1. Download a release (or build it yourself, see below).
2. Extract the archive into `game/csgo/addons/`, so that:
   - plugin files land in `addons/counterstrikesharp/plugins/super_powers_plugin/`
   - the shared API lands in `addons/counterstrikesharp/shared/super_powers_plugin_api/`
3. Restart the server or run `css_plugins reload super_powers_plugin`.
4. A default config is generated at `addons/counterstrikesharp/configs/plugins/super_powers_plugin/super_powers_plugin.json` on first launch.

## Game modes

Set with `sp_mode <mode>` (console, requires `@css/root`):

| Mode | Behavior |
|---|---|
| `normal` | No automatic power assignment. Use `sp_add` to hand out powers. |
| `random` | Every round start every player gets a random power. |
| `shop` | Every round start every player gets the `the_shopper` power and can buy powers with the `b` command. |

The mode persists until changed or the server restarts.

## Commands

All `sp_*` commands require the `@css/root` permission unless noted otherwise.

### Managing powers

| Command | Description |
|---|---|
| `sp_add <player> <power> [now\|force]` | Add power(s) to player(s). `now` triggers the power immediately, `force` bypasses team/disabled checks. |
| `sp_add_offline <steamid64> <power>` | Add a power to an offline player (applies when they rejoin). |
| `sp_remove <player> <power>` | Remove power(s). |
| `sp_list` | List all available powers and their status. |
| `sp_status` | Show which players currently have which powers (active + saved). |
| `sp_mode <normal\|random\|shop>` | Set the game mode. |

### Configuring

| Command | Description |
|---|---|
| `sp_inspect <power>` | Dump all config values of a power. |
| `sp_reconfigure <power> <key> <value> [...]` | Live-override config values without restarting. Pairs of key/value: `sp_reconfiguration radiation period_ticks 32`. |

### Player data / leveling

| Command | Description |
|---|---|
| `sp_pstats <player> [power]` | Show XP/level progression for a player's powers. |
| `sp_setlevel <player> <power> <level>` | Admin override of a power's level (used e.g. to unlock supply_closet's helmet upgrade). |

### Weapon modifiers

| Command | Description |
|---|---|
| `sp_mod_add <player> <modifier>` | Apply a weapon modifier to the player's active weapon. |
| `sp_mod_remove <player> <modifier>` | Remove a weapon modifier from the player's active weapon. |
| `sp_mod_list` | List all available weapon modifiers. |
| `sp_mod_status` | Show all currently applied weapon modifiers across all weapons. |
| `sp_mod_querry <player>` | Show modifiers applied to a specific player's active weapon. |
| `sp_mod_inspect <modifier>` | Dump all config values of a weapon modifier. |
| `sp_mod_reconfigure <modifier> <key> <value> [...]` | Live-override modifier config values. |

### Debug / misc

| Command | Description |
|---|---|
| `sp_trace [flags] [-exclude flags] [self]` | Ray trace test from your eyes along your view. No args = sweep of common masks. Example: `sp_trace solid,window -player`. |
| `sp_signal <args>` / `b` | Pass arbitrary input to powers (also used by the shopper UI). |
| `sp_force_signal <player> <args>` | Same, but targeted at a specific player. |

### Player selectors

`<player>` accepts:

- exact name or wildcards: `*`, `tem*`, `*bot`
- teams: `#t`, `#ct`
- SteamID64: `@76561198012345678`

`<power>` accepts wildcards too (`radia*`) and multiple comma-separated names.

## Configuration

The config file (`configs/plugins/super_powers_plugin/super_powers_plugin.json`) is generated automatically on first launch by reflecting over every power class. Any public field starting with `cfg_` becomes a tunable entry under its power's name:

```json
{
  "args": {
    "radiation": {
      "damage": "1",
      "health_cap": "50",
      "range": "3072",
      "period_ticks": "64",
      "trace_margin": "16"
    }
  }
}
```

Notes:

- Values are strings and parsed into whatever type the field declares.
- Stale entries are cleaned up and new ones filled with defaults on startup — you never need to hand-edit the structure.
- Global options at the root of the JSON:
  - `DataBaseConnectionString` — MySQL connection string if you want shared storage.
  - `StandaloneDatabase` — `true` uses a local SQLite file instead (default).

Changes apply after restart, or instantly via `sp_reconfigure`.

## Powers

Rarity affects nothing mechanically yet beyond shop coloring; prices are used by shop mode.

### Common

| Power | Price | Description |
|---|---|---|
| bonus_health | 2000 | +150 HP at round start |
| instant_defuse (CT only) | 2500 | Defuse bombs instantly, no kit needed |
| instant_plant (T only) | 2500 | Plant bombs with no delay |
| charge_jump | 2500 | Crouch-jump to leap forward |
| explosion_upon_death | 2500 | Explode on death: 125 dmg in 500u radius |
| healing_zeus | 2500 | Zeus zaps set teammates' health to 75 |
| super_jump | 2500 | Look up + jump for double height |
| door_dash | 2500 | $100 per 2s standing still after respawning |
| biocoded_weapons | 2500 | Only you can use weapons you bought |
| speedy_fella | 3000 | Increased walking speed |
| buildup | 3000 | +16 armor per thrown utility (cap 100) |
| social_security | 3500 | Round-end payout if your K/D is below 0.9 |
| super_speed | 3500 | Increased movement speed |

### Uncommon

| Power | Price | Description |
|---|---|---|
| bounty_hunter | 4500 | +$300 per kill, -$300 if you end the round with none |
| drop_reload | 4500 | Dropping your empty weapon fully reloads the second one |
| fake_passport | 4500 | Appear as the enemy team on radar/scoreboard-ish checks |
| golden_bullet | 4000 | Kill reward when using your last bullet |
| instant_nades | 3500 | Grenade/flash fuse reduced 4x |
| regeneration | 5000 | Regenerate 10 HP/s while below 75 |
| bitcoin_miner | 5000 | Random money ticks |
| damage_loss | 5000 | 50% chance to ignore incoming damage |
| poisoned_smoke | 5000 | Your smoke deals 2 dmg/s inside |
| infinite_ammo | 6500 | Endless magazine ammo (zeus included) |
| bullet_drain | 6500 | 50% chance to drain a bullet from enemies you hit |
| eternal_nade | 6000 | Grenades return to you after detonating |
| flash_of_disability | 6000 | Flashing enemies disables their powers briefly |
| pacifism | 6000 | Invincible until you deal damage |
| supply_closet | 6000 | Teammates in line of sight gain 2 armor/s (upgraded: helmets at full armor) |

### Rare

| Power | Price | Description |
|---|---|---|
| blood_fury | 7000 | Kills grant stacking damage/speed bonuses |
| evil_aura | 9500 | Slowly harm nearby enemies through walls, can't kill |
| headshot_immunity | 9000 | Headshots against you are cancelled |
| homing_nade | 8000 | Your grenades gravitate toward visible enemies |
| nuke_nades | 7000 | HE grenades, 10x more explosive |
| radiation | 7000 | 1 dmg/s to enemies in sight, stops at 50 HP |
| snowballing | 7500 | Kills grant HP and damage, stacks up to caps |
| warp_peek | 7000 | Warp back to where you were shortly before being hit |

### Legendary

| Power | Price | Description |
|---|---|---|
| invisibility | 8000 | You are nearly invisible |
| rebirth | 8000 | Respawn at your last death location |
| vampirism | 8000 | Heal 20% of damage dealt |
| wallhacks | 8500 | See all players glowing through walls |

### Disabled / internal

Not obtainable in random/shop modes: `banana`, `bonus_armor`, `bot_disguise`, `bot_guesser`, `damage_bonus`, `dormant_power` (internal utility), plus unfinished experiments in `src/powers/disabled/`.

## Weapon modifiers

Weapon modifiers are persistent effects attached to individual weapons rather than players. They are tracked per-weapon entity and survive across rounds as long as the weapon exists. Configure them via `weapon_modifiers.json` (auto-generated on first launch) or live with `sp_mod_reconfigure`.

| Modifier | Description |
|---|---|
| armor_piercing | 2x damage, 10% chance to jam on fire |
| biocoded_weapon | Weapon only works for the original buyer |
| infinite_ammo_weapon | Weapon never runs out of ammo |

### Contributing a weapon modifier

1. Create `src/weapon_modifiers/YourModifier.cs` extending `BaseWeaponModifier`.
2. Set `Triggers` to the game events you care about, override `Execute(GameEvent, WeaponModifierContext)`.
3. Override `Update(CCSWeaponBase, CCSPlayerController)` for per-tick logic (runs every tick — keep light).
4. Override `OnTakeDamage(DamageContext)` to modify incoming damage when this weapon is used.
5. Add `cfg_`-prefixed public fields for tunable values.
6. Register it in the constructor list in `src/models/WeaponModifierManager.cs`.
7. Override `GetDescriptionColored()` for player-facing text.

## Leveling

Powers with `SupportsLeveling` (currently `supply_closet`) gain XP as they trigger. Levels scale their effect (see each power) and unlock upgrades. Progression is stored per player in the configured database. Inspect with `sp_pstats`, override with `sp_setlevel`.

## Known caveats

- **`charge_jump` and `super_jump`** require `sv_legacy_jump 1` — CS2 does not produce jump events otherwise.
- The config is regenerated/merged on first launch after adding or renaming powers.
- The plugin must be built inside a real CS2 install so the CSSharp API DLL can be referenced (see below).

## Building from source

Requirements: .NET SDK (net10.0), make + bash (msys2 on Windows works fine).

Clone/place the repo inside `game/csgo/addons/counterstrikesharp/plugins/super_powers_plugin/` so that `../../api/CounterStrikeSharp.API.dll` resolves, then:

```bash
make                 # build + copy binaries next to the sources
make release_full    # package a release zip
```

`watch.sh` rebuilds whenever `src/` changes.

## Contributing a power

1. Create `src/powers/YourPower.cs` extending `BasePower`.
2. Set `Triggers` to the game events you care about, override `Execute(GameEvent)` and/or `Update()` (per tick — keep it light).
3. Add `cfg_`-prefixed public fields for anything servers should be able to tune.
4. Register it in the constructor list in `src/models/controller.cs`.
5. Override `GetDescriptionColored()` — this is what players see in chat/shop.
6. Override `OnTakeDamage(DamageContext)` to react to or modify damage dealt by or to the power's users.

See any file in `src/powers/` for examples; `Radiation.cs` shows ray tracing, `BountyHunter.cs` shows multi-event state tracking.
