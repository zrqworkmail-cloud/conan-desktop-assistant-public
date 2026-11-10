. (Join-Path $PSScriptRoot 'plugin-test-reference.ps1')
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
$env:CONAN_PET_TEST_MODE='1'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$r='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$out=Join-Path $root ('outputs\text-plugin-check-'+[Guid]::NewGuid().ToString('N'))
New-Item $out -ItemType Directory|Out-Null
Copy-Item (Join-Path $root 'dist\ConanPet.exe') (Join-Path $out 'ConanPet.exe')
Copy-Item $pluginApi $out -Force
& (Join-Path $r 'csc.exe') $pluginApiReference /nologo /codepage:65001 /target:exe /platform:x64 "/out:$out\TextToolsPluginTests.exe" "/r:$out\ConanPet.exe" /r:System.Xaml.dll "/r:$r\WPF\WindowsBase.dll" "/r:$r\WPF\PresentationCore.dll" "/r:$r\WPF\PresentationFramework.dll" (Join-Path $PSScriptRoot 'TextToolsPluginTests.cs')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& (Join-Path $out 'TextToolsPluginTests.exe') (Join-Path $out 'data') (Join-Path $root 'dist\plugin-packages\text-tools-1.0.0.conanplugin')
exit $LASTEXITCODE
