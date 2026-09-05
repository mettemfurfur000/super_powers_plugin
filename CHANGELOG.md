# Changelog

## 0.5.0

### New: Weapon Modifiers
A new subsystem for per-weapon persistent effects, separate from player powers. Modifiers are tracked by weapon entity and survive across rounds. Config lives in `weapon_modifiers.json` (auto-generated) or via live `sp_mod_reconfigure`.

- **BaseWeaponModifier** base class — set `Triggers` for event hooks, override `Execute`, `Update`, `OnTakeDamage`, `OnApply`, `OnRemove`.
- **WeaponModifierManager** — registry, per-weapon tracking via CustomName ID, entity map rebuild every 32 ticks, tick-loop update, damage dispatch.
- **DamageListener** — hooks `Listeners.OnEntityTakeDamagePre` (replaces obsolete `VirtualFunctions.CBaseEntity_TakeDamageOldFunc`), resolves attacker weapon via `WeaponServices.ActiveWeapon` with `.As<CCSWeaponBase>()` casting.
- **DamageContext** — wraps `CTakeDamageInfo` with resolved victim/attacker/weapon; exposes `Damage` property for modification, `Prevented` flag to cancel damage entirely.
- **ModifierConfigHelper** — shared config infrastructure extracted from `SuperPowerController`, used by both power and modifier managers (wildcard selection, config feed/merge, reflective dump, parse, reconfigure).

### New modifiers
- **armor_piercing** — 2x damage, 10% chance to jam on fire.
- **biocoded_weapon** — weapon refuses to fire for anyone but the original buyer.
- **infinite_ammo_weapon** — clip refills on every shot.

### New commands
- `sp_mod_add <player> <modifier>` — apply modifier to active weapon.
- `sp_mod_remove <player> <modifier>` — remove modifier from active weapon.
- `sp_mod_list` — list available weapon modifiers.
- `sp_mod_status` — print all applied weapon modifiers.
- `sp_mod_querry <player>` — print modifiers for a player's active weapon.
- `sp_mod_inspect <modifier>` — dump modifier config values.
- `sp_mod_reconfigure <modifier> <key> <value> [...]` — live-override modifier config.

### Changes
- `sp_help` now lists weapon modifier commands.
- `BasePower` gains `OnTakeDamage(DamageContext)` virtual hook — powers can now react to or modify damage dealt by or to their users.
- `SuperPowerController` refactored to delegate config logic to `ModifierConfigHelper`.
- `tem_utils.cs`: fixed early `return` → `continue` in weapon iteration loop (was aborting inventory scan on first invalid weapon); switched to `.As<CCSWeaponBase>()` cast.
- `BotDisguise`: migrated from `gameEvent as EventRoundStart` to `gameEvent.As<EventRoundStart>()`.
- CSS API bumped to 1.0.373.

## 0.4.0

### New powers
- **drop_reload** — dropping your empty weapon fully reloads your second weapon. Reworked from scratch: inventories are now polled instead of relying on events, and only genuinely empty drops trigger the reload (thrown grenades, knives and loadout strips are filtered out).
- **radiation** — deals 1 damage every second to all enemies in line of sight, never reducing them below 50 health.
- **bullet_drain** — 50% chance to remove a bullet from the enemy's magazine when you hit them.
- **buildup** — gain 16 armor for every thrown utility, capped at 100.
- **supply_closet** — teammates in your line of sight gain 2 armor per second, up to 100. Supports leveling: at level 1+, fully-stocked teammates also receive a helmet.
- **bounty_hunter** — gain $300 for every kill, lose $300 if the round ends without any kills.
- **door_dash** — for every 2 seconds standing still after respawning, gain $100 (freeze-time payouts disabled by default).

### Changes
- Migrated all ray tracing (radiation, supply_closet, homing_nade) from the metamod RayTrace plugin to the native `Trace` API shipped with newer CounterStrikeSharp versions. The old wrapper has been removed.
- Fixed homing_nade: trace hit entities were pattern-matched against the wrong CLR type, so homing never applied.
- Line-of-sight traces now stop just short of the target's hull so segments terminating inside a player no longer register as blocked sight (`cfg_traceMargin`).
- **evil_aura** price raised 9000 → 9500 (more powerful than radiation).
- drop_reload no longer depends on `EventItemRemove`, which does not fire reliably.

### Added
- `InventoryTracker` utility class — polls player inventories and reports weapon removals with their last known clip count.
- `sp_trace` console command — sweeps ray presets from your eyes along your view to debug tracing masks; supports custom `Contents` flags, exclusions and self-hit testing.

### Technical
- Registered previously missing `EventPlayerSpawn` handler.
- Target framework bumped to net10.0, matching current CounterStrikeSharp builds.

## 0.3.2
- Database storage setup (SQLite/MySQL) and leveling system groundwork.
