$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$exe = Join-Path $root 'Builds\ThreeFronts\Verdant.exe'
$result = Join-Path $root 'Builds\ThreeFronts\smoke-result.json'
if (!(Test-Path -LiteralPath $exe)) { throw 'Build the project first using Build.ps1.' }
if (Test-Path -LiteralPath $result) { Remove-Item -LiteralPath $result }
$arguments = '--smoke-test -screen-width 1600 -screen-height 1000 -screen-fullscreen 0 -logFile "{0}"' -f (Join-Path $root 'Logs\smoke.log')
$process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (!$process.WaitForExit(240000)) { Stop-Process -Id $process.Id; throw 'Smoke test timed out after 240 seconds.' }
if (!(Test-Path -LiteralPath $result)) { throw 'Smoke test produced no result; inspect Logs\smoke.log.' }
$data = Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
if (!$data.success) { throw "Smoke test failed: $($data.message)" }
Write-Output "PASS: $($data.assertions) assertions, $($data.waves) waves, $($data.kills) enemies defeated."

