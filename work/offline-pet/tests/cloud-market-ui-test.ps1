. (Join-Path $PSScriptRoot 'plugin-test-reference.ps1')
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
$env:CONAN_PET_TEST_MODE='1'
$env:CONAN_PET_CATALOG_TEST_NETWORK='1'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$r='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$out=Join-Path $root ('outputs\cloud-ui-check-'+[Guid]::NewGuid().ToString('N'))
New-Item $out -ItemType Directory|Out-Null
New-Item (Join-Path $out 'profile') -ItemType Directory -Force|Out-Null
Copy-Item (Join-Path $root 'dist\plugin-packages') (Join-Path $out 'profile\plugin-packages') -Recurse -Force
Copy-Item (Join-Path $root 'dist\ConanPet.exe') (Join-Path $out 'ConanPet.exe')
$refs=@("/r:$out\ConanPet.exe",'/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$r\WPF\WindowsBase.dll","/r:$r\WPF\PresentationCore.dll","/r:$r\WPF\PresentationFramework.dll")
Copy-Item $pluginApi (Split-Path "$out\CloudMarketUiTests.exe" -Parent) -Force
& (Join-Path $r 'csc.exe') $pluginApiReference /nologo /codepage:65001 /target:exe /platform:x64 "/out:$out\CloudMarketUiTests.exe" $refs (Join-Path $PSScriptRoot 'CloudMarketUiTests.cs')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& (Join-Path $out 'CloudMarketUiTests.exe') $out (Join-Path $root 'outputs\plugins\system-monitor-1.0.1.conanplugin')
exit $LASTEXITCODE
