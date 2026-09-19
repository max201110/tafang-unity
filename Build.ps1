param([string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe")
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if (!(Test-Path -LiteralPath $Unity)) { throw "Unity editor not found: $Unity" }
New-Item -ItemType Directory -Force -Path (Join-Path $root 'Logs') | Out-Null
$arguments = '-batchmode -nographics -quit -projectPath "{0}" -executeMethod Verdant.Editor.ProjectSetup.BuildWindows -logFile "{1}"' -f $root, (Join-Path $root 'Logs\build.log')
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Unity build failed. See Logs\build.log" }
Write-Output "Build complete: $root\Builds\ThreeFronts\Verdant.exe"

