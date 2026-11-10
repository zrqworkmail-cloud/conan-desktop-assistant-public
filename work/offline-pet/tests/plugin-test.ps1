. (Join-Path $PSScriptRoot 'plugin-test-reference.ps1')
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
$env:CONAN_PET_TEST_MODE='1'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$r='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$out=Join-Path $root ('outputs\plugin-check-'+[Guid]::NewGuid().ToString('N'))
New-Item $out -ItemType Directory|Out-Null
Copy-Item (Join-Path $root 'dist\ConanPet.exe') (Join-Path $out 'ConanPet.exe')
Copy-Item $pluginApi (Split-Path "$out\PluginTests.exe" -Parent) -Force
& (Join-Path $r 'csc.exe') $pluginApiReference /nologo /codepage:65001 /target:library "/out:$out\BlockingMonitorPlugin.dll" (Join-Path $PSScriptRoot 'BlockingMonitorPlugin.cs')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& (Join-Path $r 'csc.exe') $pluginApiReference /nologo /codepage:65001 /target:exe /platform:x64 "/out:$out\PluginTests.exe" "/r:$out\ConanPet.exe" /r:System.Web.Extensions.dll "/r:$r\WPF\WindowsBase.dll" (Join-Path $PSScriptRoot 'PluginTests.cs')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& (Join-Path $out 'PluginTests.exe') (Join-Path $out 'profile') (Join-Path $root 'outputs\plugins\system-monitor-1.0.1.conanplugin') (Join-Path $out 'BlockingMonitorPlugin.dll') (Join-Path $root 'outputs\plugins\file-search-1.0.0.conanplugin') (Join-Path $root 'dist\plugin-packages')
exit $LASTEXITCODE
