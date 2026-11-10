$pluginRoot=Split-Path $PSScriptRoot -Parent
$pluginApi=Join-Path $pluginRoot 'dist\Conan.PluginApi.dll'
if(-not (Test-Path $pluginApi)){& (Join-Path $pluginRoot 'plugins\build-sdk.ps1');if($LASTEXITCODE -ne 0){throw 'Plugin API build failed.'}}
$pluginApiReference="/r:$pluginApi"
$env:CONAN_TEST_MONITOR_PACKAGE=Join-Path $pluginRoot 'outputs\plugins\system-monitor-1.0.1.conanplugin'

$env:CONAN_TEST_SEARCH_PACKAGE=Join-Path $pluginRoot 'outputs\plugins\file-search-1.0.0.conanplugin'
