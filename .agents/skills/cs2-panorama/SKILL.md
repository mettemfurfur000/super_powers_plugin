---
name: panorama-hud
description: >-
  How to craft CS2 (Source 2) Panorama UI shipped as a workshop asset and driven by a
  custom_hud_layout entity — the compile/delivery pipeline, the Panorama CSS/markup limitations,
  the full stylesheet-property reference, url() protocol rules, and matching the native CS2 UI.
  Framework-agnostic: the server that drives the layout can be ModSharp, CounterStrikeSharp,
  Swiftly, a map's cs_script, or anything else — this is about authoring the .vxml/.vcss correctly.
  Use WHENEVER a task involves a CS2 custom HUD / scoreboard / menu / overlay, a Panorama .vxml or
  .vcss file, custom_hud_layout, a workshop panorama addon, or making a custom UI look native — even
  if the user only says "the HUD", "the scoreboard", or "the menu". Read this BEFORE writing any
  Panorama layout or style, because the delivery model and CSS limitations are non-obvious and each
  mistake costs a full compile round-trip.
---

# CS2 Panorama UI (custom_hud_layout / workshop assets)

CS2's `custom_hud_layout` entity renders a **server-controlled Panorama layout** on the client. You
author the UI as compiled Panorama — a `.vxml` layout + `.vcss` styles shipped in a **workshop addon**
— and a server plugin drives its **state** at runtime (per-player text via dialog variables, CSS class
toggles, button clicks). This skill is about **authoring that Panorama correctly**: the delivery/compile
pipeline, Panorama's CSS/markup limitations, the property reference, and matching the native UI. The
server side that drives it is framework-specific (ModSharp, CounterStrikeSharp, Swiftly, a map's
cs_script, …) and secondary here — the Panorama itself is identical whatever drives it.

## THE #1 GOTCHA: how the layout reaches the client

**Only entity state crosses the wire — never the layout itself.** The compiled `.vxml_c`/`.vcss_c` must
ALREADY exist on every client, or the entity has nothing to render. So:

- Compile the Panorama with the **CS2 SDK Workshop Tools resourcecompiler** (the authoring tools — a
  dedicated game server ships no resourcecompiler, so you can't compile on the server; compile in the
  SDK/Workshop Tools). This is a manual, human step in most setups.
- Ship the compiled files to clients in a **workshop addon** they subscribe to (or bundle the
  `panorama/layout/custom_game` + `panorama/styles/custom_game` folders into the map addon the server
  runs). Only state travels at runtime.
- Addon-supplied custom layouts and stylesheets load on a retail client (`Panorama/AllowCustomGameUI` is
  enabled, and `panorama/layout/custom_game` + `panorama/styles/custom_game` are in the search paths) — no
  tools mode required.

Practical consequence: the write→see loop has a **manual compile in the middle**, and clients cache
compiled panorama hard — after recompiling you usually must re-subscribe/remount so the new
`.vxml_c`/`.vcss_c` are actually re-read. Batch your edits, **validate them against the CSS reference
before compiling**, and prove the pipeline with the simplest possible layout first.

## Authoring the layout (.vxml)

```xml
<root>
  <styles><include src="s2r://panorama/styles/custom_game/mystyles.vcss_c" /></styles>
  <Panel class="HudScreen">
    <Panel id="MyRoot" class="HudRoot ...anchor...">
      <Label text="{s:title}" />
      <!-- Panel / Label / Image / Button -->
    </Panel>
  </Panel>
</root>
```

- **The top-level panel must NOT carry an `id`.** A layout is `<root>` → `<styles>` → ONE top-level `<Panel>`.
  The custom_hud system owns that root panel and rejects an `id` on it — give it a `class` only (convention
  `HudScreen`) and put your addressable `id` on the panel INSIDE it (convention `HudRoot`, e.g. `id="MyRoot"`).
  Everything the server drives (dialog vars, class toggles) lives under that inner root, addressed by its id.
- **No `<scripts>`** — native layouts pull in `.vts` scripts, but custom_hud has no client scripting; don't
  include any. Only `Panel`/`Label`/`Image`/`Button`, attributes limited to `id class hittest` (+ `text` on
  Label, `src texturewidth textureheight` on Image), and no inline `style="..."`.
- Dynamic text is a **dialog variable**: `<Label text="{s:varName}" />`, set from the server. Dynamic
  images bind via a class, not a runtime `src` string.
- There is **no client scripting** in custom_hud — all animation is pure CSS `@keyframes`, triggered by
  the server toggling a class. For a slot-machine / case-unbox reveal: a fixed strip of tile `<Label>`s,
  fill their dialog variables (winner at a fixed tile index), toggle one `spinning` class — one
  decelerating keyframe runs the reel with zero per-tick server pushes.
- Keep the server's dialog-variable names and class names in sync with the vxml.

## Panorama CSS/markup limitations — read `references/panorama-css.md`

Panorama CSS is a **subset with a different value grammar**; invalid values are often warned-and-ignored,
so a "wrong" style silently doesn't apply. The most common traps:

- Layout is Panorama's own model — **no** `display`/flex/grid/float/`top-right-bottom-left`/`box-sizing`.
  Use `flow-children`, `width`/`height` = `fit-children` | `<px>` | `<%>` | `fill-parent-flow( w )` |
  `width-percentage( p )`, plus `position: x y z`, `align`, `ignore-parent-flow`.
- **A bare `border-left: none` fails** — the border shorthand wants `width style color` (styles: solid,
  dashed, none). Disable with `border-left-width: 0px` or `border-left-style: none`. `box-shadow: none`
  is likewise invalid — use a transparent `box-shadow: #00000000 0px 0px 0px 0px`.
- **No `:not()`, `::before`/`::after`, `:nth-of-type`, or attribute selectors.** Allowed: `.class #id
  PanelType descendant`, pseudo `:hover :focus :active :selected :disabled :root`, structural
  `:nth-child :first-child :last-child`. At-rules: only `@define @import @keyframes`.
- Colors are `#rrggbb`/`#rrggbbaa` hex or `gradient(...)` (2008 WebKit form). `rgba()` also works in
  practice — Valve's own CSS uses `rgba(0,0,0,0.88)` — but hex is the safest. No `calc()`, no
  `var()`/custom properties, no `@media`. (The binary's "no rgba/url by name" list is unreliable for
  value functions: `url()` is on it yet clearly works.)
- **`url()` accepts ONLY `file://`, `panel://`, or `s2r://`.** Anything else is a hard compile error
  (`Unexpected protocol specified in url()`). Put custom icons under `panorama/icons/game_custom/` and
  reference them with `s2r://` or `file://`.
- **No inline `style="..."`** allowed on panels via the custom_hud validator; class-driven only.
  `hittest="false"` disables a panel's click hit-testing.
- `overflow: squish|clip|scroll|noclip` (not `hidden`); `visibility: visible|collapse`.
- Useful bits people miss: **`sound: "name"`** plays a sound when a selector applies (and `sound-out`
  when it clears) — free button/hover feedback with zero server code; `wash-color` tints a panel+children;
  `blur`/`background-blur`; `ui-scale`; full transform/perspective. See the reference for all 140 props.

For the exhaustive property vocabulary, see **`references/panorama-stylesheet-reference.txt`** (the full
binary-derived list, credit Snake) — or run `dump_panorama_css_properties` in the CS2 console.

## Matching the native CS2 UI

To make a custom layout look like a native panel (e.g. a scoreboard), **read Valve's own Panorama and
copy the real values** instead of eyeballing. Extract it from the game's `pak01_dir.vpk` with a Source 2
decompiler (e.g. [Source2Viewer](https://github.com/ValveResourceFormat/ValveResourceFormat)); layouts
live under `panorama/layout/`, styles under `panorama/styles/` (e.g. `scoreboard.xml`/`scoreboard.css`,
with `csgostyles.css` holding shared `@define` colors and fonts — native uses the `Stratum2` family;
team colors `@define color-CT #B5D4EE` / `color-T #EAD18A`). Some native behavior (round-history
timelines, Steam voice/UGC buttons) is driven by client scripting custom_hud can't replicate — match
the static structure and skip those rather than faking them.

## Driving it from the server (framework-specific — brief)

The `custom_hud_layout` entity is exposed differently per framework; the Panorama above is identical
whatever drives it. In **ModSharp** (which added the entity API), you get the manager in
`OnAllModulesLoaded` via `GetPanoramaManager()` → `IPanoramaManager`, then `CreateLayout(...)`,
`SetDialogVariableStringForPlayer(player, panelId, var, value)`, class overrides via
`SetClassOverrideForPlayer(..., HudPanelClassStatus.ForceEnable/ForceDisable)`, per-layout click
callbacks (`InstallClickCallback`), and input capture (`SetInputCaptureEnabled`). Other frameworks
(CounterStrikeSharp, Swiftly) or a map's own `cs_script` expose their own equivalents — consult that
framework's docs for the exact calls. The authoring rules in this skill apply regardless.

## License

MIT. Contributions welcome — this captures hard-won Panorama/workshop-asset knowledge so nobody has to
re-derive it through failed compiles.
