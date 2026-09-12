# Super Powers HUD - Workshop Addon

This directory contains the compiled Panorama resources for the Super Powers plugin's custom HUD.

## What's Included

- `panorama/layout/custom_game/super_powers_hud.vxml_c` - Main HUD layout
- `panorama/styles/custom_game/hud_shared.vcss_c` - Shared HUD primitives
- `panorama/styles/custom_game/super_powers_hud.vcss_c` - Terminal-style skin

## Publishing to Steam Workshop

### Prerequisites

1. CS2 Workshop Tools installed (Steam → Properties → DLC)
2. Compiled Panorama resources (run `.\build_hud.ps1 -Workshop`)

### Steps

1. **Launch Workshop Tools**
   - Open CS2 with Workshop Tools enabled
   - Select the `super_powers_hud` addon

2. **Open Workshop Manager**
   - Go to Tools → Workshop Manager
   - Click "New" to create a new workshop item

3. **Fill in Details**
   - Title: "Super Powers HUD"
   - Description: Custom terminal-style HUD for the Super Powers plugin
   - Preview image: Add a screenshot of the HUD

4. **Publish**
   - Click "Submit" to upload to Steam Workshop
   - Note the Workshop ID for server configuration

### Server Configuration

Servers need to mount this addon for players to see the HUD:

```
// In server.cfg or workshop collection
sv_addon 1
sv_addon_id <your_workshop_id>
```

Or add to your workshop collection and use:
```
sv_workshop_collection <collection_id> required
```

### Client Usage

Once subscribed to the workshop addon:

```
// Toggle the HUD
sp_hud

// Debug HUD entities
sp_hud_debug
```

## Development Workflow

For development without publishing:

```bash
# Compile and deploy to overrides (for testing)
.\build_hud.ps1

# Compile and prepare for workshop
.\build_hud.ps1 -Workshop

# Watch mode for live development
.\build_hud.ps1 -Watch
```

## File Structure

```
content/csgo_addons/super_powers_hud/
├── addoninfo.txt                    # Workshop metadata
├── panorama/
│   ├── layout/custom_game/
│   │   └── super_powers_hud.vxml_c  # Compiled layout
│   └── styles/custom_game/
│       ├── hud_shared.vcss_c        # Shared styles
│       └── super_powers_hud.vcss_c  # Skin styles
└── MANIFEST.txt                     # Package contents

game/csgo_addons/super_powers_hud/
├── panorama/
│   ├── layout/custom_game/
│   │   └── super_powers_hud.vxml_c  # Compiled layout
│   └── styles/custom_game/
│       ├── hud_shared.vcss_c        # Shared styles
│       └── super_powers_hud.vcss_c  # Skin styles
└── _iconcache/                      # Tool cache
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| HUD not appearing | Ensure addon is subscribed and mounted |
| VPK not loading | Check server console for mount errors |
| Styles not applying | Verify all 3 files are in the VPK |
| Build fails | Run `.\build_hud.ps1` first to compile resources |

## Technical Notes

- The `custom_game` path is a special search path in CS2
- Files are loaded from mounted VPK addons automatically
- PanoramaManager handles the server-to-client communication
- No client-side scripts needed - server drives everything

## Links

- [PanoramaManager Documentation](https://github.com/Next-il/PanoramaManager)
- [PanoramaHUD-Skills](https://github.com/Next-il/PanoramaHUD-Skills)
- [Super Powers Plugin](../README.md)
