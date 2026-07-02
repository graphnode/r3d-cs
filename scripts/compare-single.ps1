<#
.SYNOPSIS
    Runs an r3d example side by side: the upstream C build (left) and the C# port (right).

.EXAMPLE
    ./compare-single.ps1 Multiview
    ./compare-single.ps1 Stencil -DurationSeconds 6
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    # If > 0, both windows auto-close after this many seconds. Otherwise they run until you close them.
    [int]$DurationSeconds = 0,

    # Skip the incremental build of the C# examples.
    [switch]$NoBuild,

    # Skip refreshing the native DLLs into the C# output (the caller already did it).
    [switch]$SkipDllRefresh
)

$ErrorActionPreference = 'Stop'
# Scripts live in <repo>/scripts, so the repo root is one level up.
$root = Split-Path -Parent $PSScriptRoot

# C# example name -> upstream exe token, for names that aren't a plain lowercase match.
$upstreamNameMap = @{
    'ToTexture' = 'to_texture'
}
$upToken = if ($upstreamNameMap.ContainsKey($Name)) { $upstreamNameMap[$Name] } else { $Name.ToLower() }

$csExe   = Join-Path $root 'Examples\bin\Release\net10.0\Examples.exe'
$csCwd   = Join-Path $root 'Examples\bin\Release\net10.0'
$csRt    = Join-Path $csCwd 'runtimes\win-x64\native'
$srcRt   = Join-Path $root 'R3D-cs\runtimes\win-x64\native'
$upExe   = Join-Path $root ("External\r3d\build\bin\Release\r3d_{0}.exe" -f $upToken)
$upCwd   = Join-Path $root 'External\r3d\examples'

# Build the C# examples so the port is current.
if (-not $NoBuild) {
    Write-Host "Building C# examples..." -ForegroundColor Cyan
    dotnet build (Join-Path $root 'Examples\Examples.csproj') -c Release | Out-Null
}

# Make sure the C# output loads the freshly built native DLLs (avoids the stale-runtime trap).
# Best-effort: a DLL held by a just-closed process may still be locked briefly.
if (-not $SkipDllRefresh -and (Test-Path $srcRt) -and (Test-Path $csRt)) {
    try { Copy-Item (Join-Path $srcRt '*.dll') $csRt -Force -ErrorAction Stop }
    catch { Write-Host "  (native DLLs already in use; using existing copy)" -ForegroundColor DarkGray }
}

if (-not (Test-Path $csExe)) { throw "C# example build not found: $csExe" }
if (-not (Test-Path $upExe)) {
    throw "Upstream exe not found: $upExe`n(There may be no upstream equivalent for '$Name', or the native build hasn't run.)"
}

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win {
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
}
"@

$screenW = [Win]::GetSystemMetrics(0)
$screenH = [Win]::GetSystemMetrics(1)

# Native window size the examples use, plus a small gap.
$winW = 800; $winH = 450; $gap = 20
$totalW = $winW * 2 + $gap
$x0 = [Math]::Max(0, [int](($screenW - $totalW) / 2))
$y0 = [Math]::Max(0, [int](($screenH - $winH) / 2))

Write-Host "Launching '$Name'  (left = upstream C, right = C# port)" -ForegroundColor Green

$up = Start-Process -FilePath $upExe -WorkingDirectory $upCwd -PassThru
$cs = Start-Process -FilePath $csExe -ArgumentList $Name -WorkingDirectory $csCwd -PassThru

function Wait-Handle($p) {
    for ($i = 0; $i -lt 100; $i++) {
        $p.Refresh()
        if ($p.MainWindowHandle -ne [IntPtr]::Zero) { return $p.MainWindowHandle }
        Start-Sleep -Milliseconds 100
    }
    return [IntPtr]::Zero
}

$hUp = Wait-Handle $up
$hCs = Wait-Handle $cs

# SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW  (move only, keep native size)
$flags = [uint32]0x0045
if ($hUp -ne [IntPtr]::Zero) { [Win]::SetWindowPos($hUp, [IntPtr]::Zero, $x0,               $y0, 0, 0, $flags) | Out-Null }
if ($hCs -ne [IntPtr]::Zero) { [Win]::SetWindowPos($hCs, [IntPtr]::Zero, $x0 + $winW + $gap, $y0, 0, 0, $flags) | Out-Null }

if ($DurationSeconds -gt 0) {
    Start-Sleep -Seconds $DurationSeconds
    foreach ($p in @($up, $cs)) { if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
} else {
    Write-Host "Close either window to finish." -ForegroundColor DarkGray
    # When one window closes, tear down the other so the pair stays in sync.
    while (-not $up.HasExited -and -not $cs.HasExited) { Start-Sleep -Milliseconds 200 }
    foreach ($p in @($up, $cs)) { if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
}
