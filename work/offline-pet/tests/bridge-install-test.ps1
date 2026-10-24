$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
$root=Split-Path $PSScriptRoot -Parent
$codexTestHome=Join-Path $root ('codex-test-'+[Guid]::NewGuid().ToString('N'))
New-Item $codexTestHome -ItemType Directory|Out-Null
@{hooks=@{SessionStart=@(@{hooks=@(@{type='command';command='echo original'})})}}|ConvertTo-Json -Depth 10|Set-Content (Join-Path $codexTestHome 'hooks.json') -Encoding UTF8
$installer=Join-Path $root 'dist\bridge\install-codex.ps1'
& $installer -CodexHome $codexTestHome
$bytes=[IO.File]::ReadAllBytes((Join-Path $codexTestHome 'hooks.json'))
if($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191){throw 'Codex cannot parse hooks.json with a UTF-8 BOM'}
& $installer -CodexHome $codexTestHome
$config=Get-Content (Join-Path $codexTestHome 'hooks.json') -Raw -Encoding UTF8|ConvertFrom-Json
if(@($config.hooks.SessionStart).Count -ne 2){throw 'Installer duplicated entries or removed original'}
if($config.hooks.SessionStart[0].hooks[0].command -ne 'echo original'){throw 'Original hook was modified'}
if(@($config.hooks.Stop).Count -ne 1){throw 'Stop handler duplicated'}
$bridgeConfig=Get-Content (Join-Path $codexTestHome 'conan-assistant\bridge-config.json') -Raw -Encoding UTF8|ConvertFrom-Json
if(!$bridgeConfig.eventsLinux.StartsWith('/mnt/')){throw 'WSL mapping not configured'}
& $installer -CodexHome $codexTestHome -Uninstall
$config=Get-Content (Join-Path $codexTestHome 'hooks.json') -Raw -Encoding UTF8|ConvertFrom-Json
if(@($config.hooks.SessionStart).Count -ne 1 -or $config.hooks.SessionStart[0].hooks[0].command -ne 'echo original'){throw 'Uninstall changed original hooks'}
if(@($config.hooks.Stop).Count -ne 0){throw 'Uninstall left our hook enabled'}
@{merge_preserves_original=$true;idempotent_install=$true;uninstall_preserves_original=$true;windows_wsl_paths=$true}|ConvertTo-Json
