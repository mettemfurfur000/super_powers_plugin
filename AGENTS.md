# AGENTS.md — super_powers_plugin

Counter-Strike 2 plugin for the CounterStrikeSharp framework. Adds superpowers to players.

## Build & dev

```bash
make                 # clean + dotnet build + copy DLLs to project root
make release_full    # package into addons/ dir, then zip
```

- `watch.sh` — polls `src/` for changes, runs `make` on each.
- Must be built **inside the CS2 plugin folder** (`game/csgo/addons/counterstrikesharp/plugins/super_powers_plugin/`) so DotNet resolves `../../api/CounterStrikeSharp.API.dll`.
- Target: `net8.0`, `AllowUnsafeBlocks=true`.

## Two projects

| Project | Location | Build separately? |
|---|---|---|
| Main plugin | `super_powers_plugin.csproj` (this dir) | `make` |
| Shared API | `../../shared/super_powers_plugin_api/` | Has its own `makefile` |

The plugin references the shared API as a compiled `.dll`. If you change the API interface, rebuild the API project first.

## Project structure

- `src/main.cs` — plugin entrypoint (`BasePlugin`), registers all game events, console commands.
- `src/models/controller.cs` — `SuperPowerController` static class: power registry, dispatch, config generation.
- `src/models/BasePower.cs` — base class all powers inherit from.
- `src/models/ShopPower.cs` — shop power mixin (rarity, price, noShop).
- `src/powers/` — one file per power. Each extends `BasePower`.
- `src/powers/disabled/` — registered in controller but disabled internally, not playable.
- `src/config/config.cs` — config POCO with reflectively-generated defaults.
- `src/utils/` — helpers: DB storage, string formatting, player extensions, raytrace.

## How a power works

- Set `Triggers` in constructor to event type(s).
- Override `Execute(GameEvent)` for per-event logic. Check `gameEvent.GetType()` matches expected trigger.
- Override `Update()` for per-tick logic (runs every tick, keep light).
- Config-exposed fields start with `cfg_` prefix (e.g. `cfg_multiplier`, `cfg_bonus`). They appear automatically in `super_powers_plugin.json`.
- Priority is set via `priority` field (lower = runs first, sorted ascending).

## Commands

All `sp_*` commands require `@css/root`. Key ones:

- `sp_add <player> <power> [now|force]` — add power. `force` bypasses team/disabled checks.
- `sp_remove <player> <power>` — remove power.
- `sp_list [player]` — list available powers.
- `sp_mode [normal|random]` — set game mode.
- `sp_reconfigure <power> <key> <value> [...]` — live config override.
- `sp_signal <args>` — pass arbitrary signal to powers.

Player selectors: wildcards (`*`), team (`#t`, `#ct`), steamid64 (`@7656119...`).

## Caveats

- `charge_jump` and `super_jump` require `sv_legacy_jump 1` (CS2 doesn't fire jump events otherwise).
- Config is **generated reflectively** on first launch from `cfg_*` fields. The config JSON lives at `../../configs/plugins/super_powers_plugin/super_powers_plugin.json`.
- Database (MySQL via MySqlConnector) is optional. Connection string in config.
- `.gitignore` blocks `.dll`, `.pdb`, `.sln`, `.deps.json`, `addons/`, `bin/`, `obj/`.
- No test infrastructure. No linter/formatter config. No CI.
