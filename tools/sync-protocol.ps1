# Copies the v2 game connection messages (RavenNest.Protocol) from the RavenNest repo into the game.
#
# The game compiles them from source next to its own MessagePack source, so the files are copied, not a DLL.
# Run it after changing the protocol in RavenNest; the server and the game must agree on it.
#
#   pwsh tools/sync-protocol.ps1                     (RavenNest next to this repo)
#   pwsh tools/sync-protocol.ps1 -RavenNest D:\RavenNest

param(
    [string]$RavenNest = (Join-Path $PSScriptRoot "..\..\RavenNest")
)

$ErrorActionPreference = "Stop"
$source = Join-Path $RavenNest "src\RavenNest.Protocol"
$target = Join-Path $PSScriptRoot "..\Assets\Scripts\Shinobytes\RavenNestApi\Protocol"

if (-not (Test-Path (Join-Path $source "RavenNest.Protocol.csproj"))) {
    throw "RavenNest.Protocol not found at $source (pass -RavenNest <path to the RavenNest repo>)"
}

New-Item -ItemType Directory -Force $target | Out-Null

$files = Get-ChildItem $source -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$names = @()
foreach ($file in $files) {
    $text = Get-Content $file.FullName -Raw
    $header = "// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.`r`n"
    Set-Content -Path (Join-Path $target $file.Name) -Value ($header + $text) -NoNewline -Encoding utf8
    $names += $file.Name
}

# files that were removed from the protocol
Get-ChildItem $target -Filter *.cs | Where-Object { $names -notcontains $_.Name } | ForEach-Object {
    Remove-Item $_.FullName
    Remove-Item ($_.FullName + ".meta") -ErrorAction SilentlyContinue
}

$commit = git -C $RavenNest rev-parse --short HEAD 2>$null
Write-Host "Copied $($names.Count) protocol files from RavenNest $commit"
