# Release.ps1 — Build, VirusTotal scan, GitHub release
# Usage:
#   .\Release.ps1 -Tag "v1.5.0" -Notes "## What's new`n- ..."
#   .\Release.ps1 -Tag "v1.5.0-beta" -Notes "..." -Prerelease

param(
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][string]$Notes,
    [switch]$Prerelease
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$solutionPath = "$PSScriptRoot\HideoutCraftModifier.sln"
$dllPath      = "$PSScriptRoot\bin\Release\HideoutCraftModifier\HideoutCraftModifier.dll"
$zipPath      = "$PSScriptRoot\bin\Release\HideoutCraftModifier.zip"
$dotnet       = "C:\Users\riicr\.dotnet\dotnet.exe"
$secretsFile  = "$PSScriptRoot\.release-secrets.ps1"

# ── Load secrets ─────────────────────────────────────────────────────────────
if (-not (Test-Path $secretsFile)) {
    Write-Error "Missing .release-secrets.ps1 — create it with: `$env:VT_API_KEY = 'your-key'"
}
. $secretsFile
if (-not $env:VT_API_KEY) { Write-Error "VT_API_KEY not set in .release-secrets.ps1" }

# ── Build ─────────────────────────────────────────────────────────────────────
Write-Host "`n[1/3] Building $Tag..." -ForegroundColor Cyan
& $dotnet build $solutionPath -c Release
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed." }

# ── VirusTotal scan ───────────────────────────────────────────────────────────
Write-Host "`n[2/3] Uploading DLL to VirusTotal..." -ForegroundColor Cyan

$uploadJson = & curl.exe -s -X POST "https://www.virustotal.com/api/v3/files" `
    -H "x-apikey: $($env:VT_API_KEY)" `
    -F "file=@$dllPath" | ConvertFrom-Json

$analysisId = $uploadJson.data.id
if (-not $analysisId) { Write-Error "VirusTotal upload failed: $($uploadJson | ConvertTo-Json)" }
Write-Host "  Analysis ID: $analysisId" -ForegroundColor DarkGray

Write-Host "  Waiting for analysis (~30s)..." -ForegroundColor DarkGray
$vtHeaders = @{ "x-apikey" = $env:VT_API_KEY }
$analysis  = $null
$attempts  = 0
do {
    Start-Sleep -Seconds 15
    $attempts++
    $analysis = Invoke-RestMethod "https://www.virustotal.com/api/v3/analyses/$analysisId" -Headers $vtHeaders
    Write-Host "  [$attempts] $($analysis.data.attributes.status)" -ForegroundColor DarkGray
} while ($analysis.data.attributes.status -ne "completed" -and $attempts -lt 20)

if ($analysis.data.attributes.status -ne "completed") {
    Write-Warning "VT analysis timed out — continuing without scan results."
    $vtSection = "`n---`n> ⚠️ VirusTotal scan timed out."
} else {
    $stats     = $analysis.data.attributes.stats
    $sha256    = $analysis.meta.file_info.sha256
    $vtLink    = "https://www.virustotal.com/gui/file/$sha256/detection"
    $malicious = $stats.malicious
    $suspic    = $stats.suspicious
    $total     = $malicious + $suspic + $stats.undetected + $stats.harmless + $stats.timeout
    $badge     = if ($malicious -eq 0 -and $suspic -eq 0) { "✅" } else { "⚠️" }

    Write-Host "  $badge $malicious/$total detections" -ForegroundColor $(if ($malicious -eq 0) { "Green" } else { "Yellow" })

    $vtSection = @"

---
### $badge VirusTotal scan
$malicious malicious · $suspic suspicious · out of $total engines · [View full report]($vtLink)
"@
}

# ── GitHub release ────────────────────────────────────────────────────────────
Write-Host "`n[3/3] Creating GitHub release $Tag..." -ForegroundColor Cyan

$fullNotes   = $Notes + $vtSection
$releaseArgs = @(
    "release", "create", $Tag,
    "$zipPath#HideoutCraftModifier.zip",
    "--repo", "elChivoR/HideoutCraftModifier",
    "--title", $Tag,
    "--notes", $fullNotes
)
if ($Prerelease) { $releaseArgs += "--prerelease" }
else             { $releaseArgs += "--latest" }

& gh @releaseArgs
if ($LASTEXITCODE -ne 0) { Write-Error "gh release create failed." }

Write-Host "`nDone! $Tag published." -ForegroundColor Green
