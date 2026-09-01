# PanoramaManager Agent Guide

## What this is

PanoramaManager is a CounterStrikeSharp library that drives the `custom_hud_layout` engine
entity from C#. You write Panorama XML/CSS layouts; this library pushes text and class
toggles into them per-player and routes button clicks back to your plugin.

- **NuGet**: `dotnet add package PanoramaManager`
- **Source**: https://github.com/Next-il/PanoramaManager
- **Layout tooling**: https://github.com/Next-il/PanoramaHUD-Skills (agent skills for writing Panorama layouts)

## Setup

### 1. Install the package

```xml
<PackageReference Include="PanoramaManager" Version="*" />
```

Or CLI:
```
dotnet add package PanoramaManager
```

### 2. Copy gamedata

Copy `gamedata/panoramamanager.json` from the NuGet package (under `contentFiles/any/any/gamedata/`)
or from the [releases](https://github.com/Next-il/PanoramaManager/releases) to:
```
addons/counterstrikesharp/gamedata/panoramamanager.json
```

### 3. Mount the workshop addon

Panorama layouts must be compiled into a VPK workshop addon. Directory structure inside the VPK:
```
panorama/layout/custom_game/   <- .xml layout files go here
panorama/styles/custom_game/   <- .css stylesheets go here
```

Build the addon, mount it on the server. The path `custom_game` is the search path CS2
registers for custom HUD layouts.

### 4. Verify

Run `css_panorama_diag` in server console. A healthy install logs nothing extra.
Errors mean gamedata is missing or a signature broke after a CS2 update.

## Core API

### Plugin lifecycle

```csharp
using PanoramaManager;

public class MyPlugin : BasePlugin
{
    private PanelHandle? _hud;

    public override void Load(bool hotReload)
    {
        Panorama.Init(this);  // required once, in Load
        _hud = Panorama.Spawn("panorama/layout/custom_game/my_hud.vxml_c");
    }

    public override void Unload(bool hotReload)
    {
        _hud?.Dispose();
        Panorama.Shutdown();  // call in Unload
    }
}
```

**Critical**: `Panorama.Init(this)` must be called before any `Spawn`. `Panorama.Shutdown()`
must be called in `Unload`. Two plugins can both use PanoramaManager independently - it is
a plain library, not a global singleton.

### Spawning a panel

```csharp
PanelHandle Panorama.Spawn(string layoutPath, LayoutContract? contract = null);
```

`layoutPath` is the compiled resource path (note the `_c` suffix):
```
panorama/layout/custom_game/my_hud.vxml_c
```

`LayoutContract` controls how the library maps to your layout's DOM:

```csharp
var hud = Panorama.Spawn(
    "panorama/layout/custom_game/scoreboard.vxml_c",
    new LayoutContract
    {
        RootPanelId = "ScoreboardRoot",   // root panel id in your XML
        RowCount    = 10,                 // how many rows your layout declares
        CaptureInput = false,             // true if the player interacts with it
        HideHud     = HideHudFlags.Crosshair,  // hide while open
    });
```

Default `LayoutContract` works with the bundled workshop layouts.

### Writing text (dialog variables)

Per-player (preferred):
```csharp
hud.SetVariableFor(player, "player_name", "Alice");
hud.SetVariableFor(player, "health", "100");
```

Global (every viewer sees the same text):
```csharp
hud.SetVariable("server_name", "My Server");
```

In your layout XML, reference these as `{s:player_name}`, `{s:health}`, etc. inside `<Label>` elements.

### Toggling CSS classes

Per-player class toggle:
```csharp
hud.SetClassFor(player, "row0", "selected", true);   // add .selected
hud.SetClassFor(player, "row0", "selected", false);   // remove .selected
```

This is how you show/hide rows, change colours, animate, etc. The server cannot send
colours or coordinates directly - everything is done through class swaps.

### Variants (class palettes)

For things like colour selection where only one class from a group should be active:

```csharp
hud.SetVariant("row0_color", "red");    // sets .red, removes .green/.blue from that group
hud.SetVariant("row0_color", null);     // clears all classes in the group
```

Define the group in your layout contract or let the default handle it.

### Click handling

```csharp
hud.OnEvent += OnHudEvent;

private void OnHudEvent(PanelEvent e)
{
    if (e.Action == PanelAction.Click)
    {
        // e.Player - who clicked
        // e.Item   - the MenuItem (if using SetItems), or null
        // e.ButtonId - raw button id string from the layout
    }
}
```

### HUD flags

Hide/show parts of the base HUD while your panel is open:

```csharp
Panorama.SetHideHud(player, HideHudFlags.Crosshair | HideHudFlags.Radar, hide: true);
// Restored automatically when the panel closes
```

### Diagnostics

```
css_panorama_diag
```

Reports: gamedata source, native resolution status, click channel status, live menu count.

## Menu layer (on top of raw panels)

If you need paginated lists with per-row callbacks, use the menu features on `PanelHandle`:

```csharp
var menu = Panorama.Spawn("panorama/layout/custom_game/admin_hud.vxml_c");
menu.Title = "Player List";
menu.PageSize = 10;  // matches RowCount in LayoutContract

menu.SetItems(players.Select(p => new MenuItem(
    Id:       $"player:{p.Slot}",
    Title:    p.PlayerName,
    Subtitle: $"{p.Ping}ms",
    OnSelect: e => KickPlayer(e.Player, p),
    Tag:      p  // arbitrary object, returned in e.Item.Tag
)));

menu.Open(player);
```

Features: per-viewer page state, tabs, central authorization gate via `OnEvent`, `TextPrompt`
for chat-based text input.

## Layout writing rules

Panorama CSS/HTML is NOT web standard. Key differences:

- No `rgba()` - use class-based colour palettes instead
- No `display: flex` - use `<StackPanel>` with `orientation`
- No JS/TypeScript on the client - server drives everything
- Silently drops unknown CSS properties (no errors, no warnings)
- Supported panels: Panel, Label, Image, Button
- Use `id` attributes for the server to target panels
- Use `class` attributes for the server to toggle states

Use [PanoramaHUD-Skills](https://github.com/Next-il/PanoramaHUD-Skills) for agent-assisted
layout authoring - it carries the full property vocabulary extracted from `libpanorama.so`.

## Server limitations (no workaround exists)

1. Cannot create panels dynamically - layout must be pre-compiled VXML
2. Cannot send colours directly - must use class swaps
3. Cannot send coordinates directly
4. Cannot take keystrokes - use chat input via `TextPrompt`

## Example: minimal HUD plugin

```csharp
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using PanoramaManager;

public class KillFeedHud : BasePlugin
{
    private PanelHandle? _hud;

    public override string ModuleName => "KillFeedHud";
    public override string ModuleVersion => "1.0.0";

    public override void Load(bool hotReload)
    {
        Panorama.Init(this);

        _hud = Panorama.Spawn("panorama/layout/custom_game/killfeed.vxml_c");
        _hud.SetClassFor  // not needed - just text
    }

    public override void Unload(bool hotReload)
    {
        _hud?.Dispose();
        Panorama.Shutdown();
    }

    // Example: push a kill message to all players
    private void ShowKill(CCSPlayerController? attacker, CCSPlayerController? victim)
    {
        if (_hud is null || attacker is null || victim is null) return;

        foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid))
        {
            _hud.SetVariableFor(p, "killer", attacker.PlayerName);
            _hud.SetVariableFor(p, "victim", victim.PlayerName);
            _hud.SetClassFor(p, "killfeed", "visible", true);
        }

        // Auto-hide after 3 seconds
        AddTimer(3.0f, () =>
        {
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid))
                _hud.SetClassFor(p, "killfeed", "visible", false);
        });
    }
}
```

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Menu renders but clicks do nothing | Click transport not installed | Check `css_panorama_diag`, ensure gamedata is in place |
| Nothing renders at all | Layout path wrong or VPK not mounted | Verify path ends in `_c`, workshop addon is mounted |
| Text shows for all players the same | Per-player text unavailable | Set `Panorama.UseGlobalDialogVariables = true` as fallback, report issue |
| Broken after CS2 update | Signature changed | Check PanoramaManager releases for gamedata update |