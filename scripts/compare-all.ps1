<#
.SYNOPSIS
    Runs every example side by side (upstream C left, C# port right), each for a fixed time,
    auto-advancing to the next. A quick visual sweep of the whole suite.

.EXAMPLE
    ./compare-all.ps1
    ./compare-all.ps1 -DurationSeconds 5
    ./compare-all.ps1 -Only Multiview,Stencil,ToTexture
#>
param(
    # Seconds to show each example before moving on.
    [int]$DurationSeconds = 6,

    # Restrict the run to these example names (default: all).
    [string[]]$Only,

    # Skip the one-time build of the C# examples.
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
# Scripts live in <repo>/scripts, so the repo root is one level up.
$root = Split-Path -Parent $PSScriptRoot
$compare = Join-Path $PSScriptRoot 'compare-single.ps1'

# Same order as Examples/Program.cs.
$all = @(
    'Basic', 'Probe', 'Lights', 'Pbr', 'Transparency', 'Skybox', 'Sponza', 'Sprite',
    'Animation', 'Bloom', 'Resize', 'Shader', 'Kinematics', 'Particles', 'Instanced',
    'Billboards', 'Sun', 'Dof', 'Decal', 'CustomMesh', 'AnimTree',
    'Multiview', 'Stencil', 'ToTexture'
)

$names = if ($Only) { $all | Where-Object { $Only -contains $_ } } else { $all }

# Build the C# examples once up front; per-example runs then use -NoBuild.
if (-not $NoBuild) {
    Write-Host "Building C# examples..." -ForegroundColor Cyan
    dotnet build (Join-Path $root 'Examples\Examples.csproj') -c Release | Out-Null
}

$upExeToken = @{ 'ToTexture' = 'to_texture' }
$csExe = Join-Path $root 'Examples\bin\Release\net10.0\Examples.exe'
$csCwd = Join-Path $root 'Examples\bin\Release\net10.0'

# Refresh the native DLLs once, before anything is running (per-example runs then skip it).
$srcRt = Join-Path $root 'R3D-cs\runtimes\win-x64\native'
$csRt  = Join-Path $csCwd 'runtimes\win-x64\native'
if ((Test-Path $srcRt) -and (Test-Path $csRt)) {
    Copy-Item (Join-Path $srcRt '*.dll') $csRt -Force
}

$i = 0
foreach ($name in $names) {
    $i++
    $token = if ($upExeToken.ContainsKey($name)) { $upExeToken[$name] } else { $name.ToLower() }
    $upExe = Join-Path $root ("External\r3d\build\bin\Release\r3d_{0}.exe" -f $token)

    Write-Host ("[{0}/{1}] {2}" -f $i, $names.Count, $name) -ForegroundColor Green

    if (Test-Path $upExe) {
        & $compare -Name $name -DurationSeconds $DurationSeconds -NoBuild -SkipDllRefresh
    }
    else {
        # No upstream equivalent (e.g. CustomMesh) — show the C# port alone.
        Write-Host "  (no upstream build; showing C# port only)" -ForegroundColor DarkGray
        $p = Start-Process -FilePath $csExe -ArgumentList $name -WorkingDirectory $csCwd -PassThru
        Start-Sleep -Seconds $DurationSeconds
        if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    }

    # Let window handles and DLL locks release before the next pair launches.
    Start-Sleep -Milliseconds 600
}

Write-Host "Done." -ForegroundColor Cyan
