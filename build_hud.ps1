<#
.SYNOPSIS
    Compiles Panorama HUD layouts and deploys them for development.

.DESCRIPTION
    Compiles the super_powers_plugin Panorama resources using resourcecompiler.exe
    and copies them to the CS2 overrides directory.

        .\build_hud.ps1                    # compile and deploy
        .\build_hud.ps1 -Watch             # recompile on save
        .\build_hud.ps1 -NoDeploy          # compile only
        .\build_hud.ps1 -Force             # force recompile all

    Requires:
    - Workshop Tools installed (for resourcecompiler.exe)
    - gameinfo.gi with "Game csgo/overrides" entry for the client to load overrides

.NOTES
    Adapted from PanoramaHUD-Skills build-hud.ps1
#>
[CmdletBinding()]
param(
    [string] $Cs2Root = 'E:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive',
    [switch] $Watch,
    [switch] $NoDeploy,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

$pluginDir  = $PSScriptRoot
$compiler   = Join-Path $Cs2Root 'game\bin\win64\resourcecompiler.exe'
$sourceDir  = Join-Path $pluginDir 'panorama'
$contentDir = Join-Path $Cs2Root 'content\csgo_addons\super_powers_hud\panorama'
$gameDir    = Join-Path $Cs2Root 'game\csgo_addons\super_powers_hud\panorama'
$overrides  = Join-Path $Cs2Root 'game\csgo\overrides\panorama'

function Assert-Path([string] $path, [string] $what) {
    if (-not (Test-Path $path)) {
        throw "$what not found: $path`nPass -Cs2Root if your install is elsewhere."
    }
}

Assert-Path $compiler   'resourcecompiler.exe'

function Build {
    # Ensure content directory exists
    New-Item -ItemType Directory -Path $contentDir -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $contentDir 'layout\custom_game') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $contentDir 'styles\custom_game') -Force | Out-Null

    # Copy source files to content directory
    $xmlFiles = Get-ChildItem -Path (Join-Path $sourceDir 'layout\custom_game') -Filter *.xml -File
    $cssFiles = Get-ChildItem -Path (Join-Path $sourceDir 'styles\custom_game') -Filter *.css -File

    if (-not $xmlFiles -and -not $cssFiles) {
        throw "No .xml or .css files found under $sourceDir"
    }

    Write-Host "`n[1/3] Copying $($xmlFiles.Count) XML and $($cssFiles.Count) CSS files to content directory" -ForegroundColor Cyan

    foreach ($file in $xmlFiles) {
        $target = Join-Path $contentDir "layout\custom_game\$($file.Name)"
        Copy-Item $file.FullName $target -Force
        Write-Host "      $($file.Name)" -ForegroundColor DarkGray
    }

    foreach ($file in $cssFiles) {
        $target = Join-Path $contentDir "styles\custom_game\$($file.Name)"
        Copy-Item $file.FullName $target -Force
        Write-Host "      $($file.Name)" -ForegroundColor DarkGray
    }

    # Compile
    $sources = Get-ChildItem -Path $contentDir -Recurse -Include *.xml, *.css -File

    Write-Host "`n[2/3] Compiling $($sources.Count) file(s)" -ForegroundColor Cyan

    foreach ($src in $sources) {
        $rcArgs = @('-i', $src.FullName)
        if ($Force) { $rcArgs += '-f' }

        & $compiler @rcArgs | Out-Null

        if ($LASTEXITCODE -ne 0) { throw "Compile failed: $($src.Name)" }

        Write-Host "      $($src.Name)" -ForegroundColor DarkGray
    }

    $compiled = Get-ChildItem -Path $gameDir -Recurse -Include *.vxml_c, *.vcss_c -File -ErrorAction SilentlyContinue

    if (-not $compiled) {
        throw "Nothing compiled into $gameDir. Check resourcecompiler output above."
    }

    if ($NoDeploy) {
        Write-Host "[3/3] Compiled to $gameDir (not deployed)" -ForegroundColor Green
        return
    }

    # Deploy to overrides
    Write-Host "[3/3] Copying $($compiled.Count) compiled file(s) to overrides" -ForegroundColor Cyan

    foreach ($file in $compiled) {
        $relative = $file.FullName.Substring($gameDir.Length).TrimStart('\')
        $target   = Join-Path $overrides $relative

        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item $file.FullName $target -Force

        Write-Host "      panorama\$relative" -ForegroundColor DarkGray
    }

    Write-Host "      -> $overrides" -ForegroundColor Green
    Write-Host "`nDone! Restart CS2 to pick up changes (or just the game if using overrides)." -ForegroundColor Green
}

Build

if ($Watch) {
    Write-Host "`nWatching $sourceDir - Ctrl+C to stop.`n" -ForegroundColor Yellow

    $watcher = New-Object System.IO.FileSystemWatcher $sourceDir, '*.*'
    $watcher.IncludeSubdirectories = $true
    $watcher.EnableRaisingEvents   = $true

    while ($true) {
        $change = $watcher.WaitForChanged([System.IO.WatcherChangeTypes]::All, 1000)

        if ($change.TimedOut) { continue }
        if ($change.Name -notmatch '\.(xml|css)$') { continue }

        Start-Sleep -Milliseconds 250
        while (-not $watcher.WaitForChanged([System.IO.WatcherChangeTypes]::All, 150).TimedOut) { }

        Write-Host "changed: $($change.Name)" -ForegroundColor Yellow

        try   { Build }
        catch { Write-Host "  $_" -ForegroundColor Red }
    }
}
