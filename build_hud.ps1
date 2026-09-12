<#
.SYNOPSIS
    Compiles and prepares Panorama files for the CS2 Workshop addon.

.DESCRIPTION
    Compiles XML/CSS source files using resourcecompiler.exe and copies both
    raw and compiled files to the Workshop content directory.

        .\build_hud.ps1                    # compile and prepare workshop addon

.NOTES
    Workshop content: E:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\content\csgo_addons\super_powers_hud\
#>
[CmdletBinding()]
param(
    [string] $Cs2Root = 'E:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive',
    [string] $AddonName = 'super_powers_hud'
)

$ErrorActionPreference = 'Stop'

$pluginDir = $PSScriptRoot
$sourceDir = Join-Path $pluginDir 'panorama'
$workshopDir = Join-Path $Cs2Root "content\csgo_addons\$AddonName"
$compiler = Join-Path $Cs2Root 'game\bin\win64\resourcecompiler.exe'

# Verify source directory exists
if (-not (Test-Path $sourceDir)) {
    throw "Panorama source directory not found: $sourceDir"
}

# Verify compiler exists
if (-not (Test-Path $compiler)) {
    throw "resourcecompiler.exe not found: $compiler"
}

# Clean workshop content directory
if (Test-Path $workshopDir) {
    Remove-Item $workshopDir -Recurse -Force
    Write-Host "Cleaned: $workshopDir" -ForegroundColor DarkGray
}

# Create directory structure
New-Item -ItemType Directory -Path (Join-Path $workshopDir 'panorama\layout\custom_game') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $workshopDir 'panorama\styles\custom_game') -Force | Out-Null

# Auto-discover and copy XML files
$xmlFiles = Get-ChildItem -Path (Join-Path $sourceDir 'layout\custom_game') -Filter *.xml -File -ErrorAction SilentlyContinue
if ($xmlFiles) {
    foreach ($file in $xmlFiles) {
        $target = Join-Path $workshopDir "panorama\layout\custom_game\$($file.Name)"
        Copy-Item $file.FullName $target -Force
        Write-Host "  layout\$($file.Name)" -ForegroundColor Gray
    }
} else {
    Write-Warning "No XML files found in $($sourceDir)\layout\custom_game"
}

# Auto-discover and copy CSS files
$cssFiles = Get-ChildItem -Path (Join-Path $sourceDir 'styles\custom_game') -Filter *.css -File -ErrorAction SilentlyContinue
if ($cssFiles) {
    foreach ($file in $cssFiles) {
        $target = Join-Path $workshopDir "panorama\styles\custom_game\$($file.Name)"
        Copy-Item $file.FullName $target -Force
        Write-Host "  styles\$($file.Name)" -ForegroundColor Gray
    }
} else {
    Write-Warning "No CSS files found in $($sourceDir)\styles\custom_game"
}

# Compile all source files
Write-Host "`nCompiling..." -ForegroundColor Cyan
$sources = Get-ChildItem -Path (Join-Path $workshopDir 'panorama') -Recurse -Include *.xml, *.css -File
$compiled = 0
$failed = 0

foreach ($src in $sources) {
    & $compiler -i $src.FullName -f 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $compiled++
    } else {
        $failed++
        Write-Host "  FAIL: $($src.Name)" -ForegroundColor Red
    }
}

Write-Host "  Compiled: $compiled, Failed: $failed" -ForegroundColor $(if ($failed -eq 0) { 'Green' } else { 'Yellow' })

# Copy compiled files from game directory to workshop content
$gamePanorama = Join-Path $Cs2Root "game\csgo_addons\$AddonName\panorama"
$compiledFiles = Get-ChildItem -Path $gamePanorama -Recurse -Include *.vxml_c, *.vcss_c -File -ErrorAction SilentlyContinue

if ($compiledFiles) {
    foreach ($file in $compiledFiles) {
        $relative = $file.FullName.Substring($gamePanorama.Length).TrimStart('\')
        $target = Join-Path (Join-Path $workshopDir 'panorama') $relative
        New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
        Copy-Item $file.FullName $target -Force
    }
    Write-Host "  Copied $($compiledFiles.Count) compiled files" -ForegroundColor Gray
}

# Create addoninfo.txt
$addonInfo = @"
"AddonInfo"
{
    "title"		"Super Powers HUD"
    "type"		"2"
    "tags"		" gameplay "
    "description"		"Terminal-style ASCII overlay for the Super Powers plugin. Provides 160x40 character grid for server-driven UI. Subscribe to enable ASCII overlay commands (sp_ascii, sp_ascii_close)."
    "author_name"		"Super Powers Team"
    "content_warning"	""
    "major_version"		"1"
    "minor_version"		"0"
}
"@
Set-Content -Path (Join-Path $workshopDir 'addoninfo.txt') -Value $addonInfo -Encoding UTF8

# Create build info file
$commitHash = try { git -C $pluginDir rev-parse --short HEAD 2>$null } catch { 'unknown' }
if (-not $commitHash) { $commitHash = 'unknown' }

$buildInfo = @"
Build Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Plugin Version: 0.5.0
Build Machine: $env:COMPUTERNAME
Source Commit: $commitHash
Files:
  Layouts: $($xmlFiles.Count) XML files
  Styles: $($cssFiles.Count) CSS files
  Compiled: $compiled files
"@
Set-Content -Path (Join-Path $workshopDir 'build_info.txt') -Value $buildInfo -Encoding UTF8

# Summary
$totalFiles = (Get-ChildItem -Path $workshopDir -Recurse -File | Measure-Object).Count
$size = (Get-ChildItem -Path $workshopDir -Recurse -File | Measure-Object -Property Length -Sum).Sum

Write-Host "`nWorkshop addon ready:" -ForegroundColor Green
Write-Host "  $workshopDir" -ForegroundColor Green
Write-Host "  $totalFiles files, $([math]::Round($size / 1KB, 1)) KB" -ForegroundColor Green
Write-Host "`nUse CS2 Workshop Tools to publish." -ForegroundColor Yellow
