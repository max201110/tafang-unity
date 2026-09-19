param([switch]$Standard, [switch]$Passive)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$exe = Join-Path $root 'Builds\ThreeFronts\Verdant.exe'
$name = if ($Passive) { 'passive' } elseif ($Standard) { 'standard' } else { 'veteran' }
$flags = '--balance-test'
if ($Standard) { $flags += ' --standard' }
if ($Passive) { $flags += ' --passive-test' }
$result = Join-Path $root "Builds\ThreeFronts\balance-$name.json"
if (Test-Path -LiteralPath $result) { Remove-Item -LiteralPath $result }
$arguments = '{0} -logFile "{1}"' -f $flags, (Join-Path $root "Logs\balance-$name.log")
$p = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (!$p.WaitForExit(240000)) { Stop-Process -Id $p.Id; throw 'Balance test timed out.' }
if (!(Test-Path -LiteralPath $result)) { throw 'No test result was written.' }
$data = Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
$data | Format-List
if (!$data.success -and !$Passive) { throw 'The tested strategy did not win. See the balance log.' }
