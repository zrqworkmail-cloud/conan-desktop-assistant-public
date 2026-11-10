. (Join-Path $PSScriptRoot 'plugin-test-reference.ps1')
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$r='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$out=Join-Path $root ('outputs\cloud-market-check-'+[Guid]::NewGuid().ToString('N'))
New-Item $out -ItemType Directory|Out-Null
Copy-Item (Join-Path $root 'dist\ConanPet.exe') (Join-Path $out 'ConanPet.exe')
Copy-Item $pluginApi $out -Force
& (Join-Path $r 'csc.exe') $pluginApiReference /nologo /codepage:65001 /target:exe /platform:x64 "/out:$out\CloudMarketTests.exe" "/r:$out\ConanPet.exe" (Join-Path $PSScriptRoot 'CloudMarketTests.cs')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& (Join-Path $out 'CloudMarketTests.exe') (Join-Path $out 'test-data') (Join-Path $root 'dist\plugin-packages\system-monitor-1.0.1.conanplugin')
exit $LASTEXITCODE
