param(
  [Parameter(Mandatory = $true)]
  [string]$Version,
  [string]$Runtime = "win-x64",
  [string]$PublishDir = "",
  [string]$OutputDir = "",
  [string]$InnoSetupCompiler = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$installerScript = Join-Path $repoRoot "installer\transtools.iss"

if (-not $PublishDir)
{
  $PublishDir = Join-Path $repoRoot "artifacts\packages\transtools-$Runtime-onefile-latest"
}

if (-not $OutputDir)
{
  $OutputDir = Join-Path $repoRoot "artifacts\installers"
}

$exePath = Join-Path $PublishDir "transtools.exe"
if (-not (Test-Path $exePath))
{
  throw "Published executable was not found at $exePath."
}

if (-not (Test-Path $installerScript))
{
  throw "Inno Setup script was not found at $installerScript."
}

if (-not $InnoSetupCompiler)
{
  $command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
  if ($command)
  {
    $InnoSetupCompiler = $command.Source
  }
}

if (-not $InnoSetupCompiler)
{
  $defaultCompiler = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
  if (Test-Path $defaultCompiler)
  {
    $InnoSetupCompiler = $defaultCompiler
  }
}

if (-not $InnoSetupCompiler -or -not (Test-Path $InnoSetupCompiler))
{
  throw "Inno Setup compiler was not found. Install Inno Setup 6 or pass -InnoSetupCompiler."
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$compilerArgs = @(
  "/DMyAppVersion=$Version",
  "/DSourceDir=$PublishDir",
  "/DOutputDir=$OutputDir",
  $installerScript
)

& $InnoSetupCompiler @compilerArgs
if ($LASTEXITCODE -ne 0)
{
  throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
}

$installerPath = Join-Path $OutputDir "transtools-$Version-setup.exe"
if (-not (Test-Path $installerPath))
{
  throw "Installer build completed but setup exe was not found at $installerPath."
}

Write-Host "INSTALLER: $installerPath"
