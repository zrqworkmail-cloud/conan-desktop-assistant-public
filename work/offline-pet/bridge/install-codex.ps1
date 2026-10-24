param([string]$CodexHome,[switch]$Uninstall)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
if(!$CodexHome){$CodexHome=$env:CODEX_HOME}
if(!$CodexHome){$CodexHome=Join-Path $env:USERPROFILE '.codex'}
if(!(Test-Path $CodexHome -PathType Container)){throw '请选择存在的Codex配置目录。'}
$hookPath=Join-Path $CodexHome 'hooks.json'
if(Test-Path $hookPath){$config=Get-Content $hookPath -Raw -Encoding UTF8|ConvertFrom-Json}else{$config=[pscustomobject]@{hooks=[pscustomobject]@{}}}
if(!$config.hooks){$config|Add-Member -NotePropertyName hooks -NotePropertyValue ([pscustomobject]@{}) -Force}
$events=@('SessionStart','UserPromptSubmit','PreToolUse','PostToolUse','PermissionRequest','Stop','Interrupt','SessionEnd')
$installed=Join-Path $CodexHome 'conan-assistant'
$linuxRoot=if($installed -match '^([A-Za-z]):\\(.*)$'){'/mnt/'+$matches[1].ToLower()+'/'+$matches[2].Replace('\','/')}else{throw '需要Windows驱动器中的配置目录。'}
# A Windows path can legally contain $(), backticks and single quotes.
function Quote-Posix([string]$Value){
 $q=[string][char]39
 $replacement=$q+[char]34+$q+[char]34+$q
 return $q+$Value.Replace($q,$replacement)+$q
}
if($installed -match '[%!]'){throw 'Codex配置路径不支持百分号或感叹号，请选择其他目录以避免Windows命令展开。'}
$command='python3 '+(Quote-Posix ($linuxRoot+'/codex_hook.py'))
$windowsCommand='"'+(Join-Path $installed 'ConanBridge.exe')+'"'
foreach($event in $events){
 $entries=@()
 if($config.hooks.PSObject.Properties.Name -contains $event){
  foreach($entry in @($config.hooks.$event)){
   $remaining=@($entry.hooks|Where-Object {($_.command -notmatch 'conan-assistant/codex_hook\.py') -and ($_.commandWindows -notmatch 'conan-assistant\\ConanBridge\.exe')})
   if($remaining.Count){$entry.hooks=$remaining;$entries+=$entry}
  }
 }
 if(!$Uninstall){$entries+=[pscustomobject]@{hooks=@([pscustomobject]@{type='command';command=$command;commandWindows=$windowsCommand;async=$true;timeout=2})}}
 $config.hooks|Add-Member -NotePropertyName $event -NotePropertyValue @($entries) -Force
}
if(Test-Path $hookPath){Copy-Item $hookPath ($hookPath+'.conan-backup-'+(Get-Date -Format 'yyyyMMdd-HHmmssfff'))}
if(!$Uninstall){
 New-Item $installed -ItemType Directory -Force|Out-Null
 Copy-Item (Join-Path $PSScriptRoot 'ConanBridge.exe'),(Join-Path $PSScriptRoot 'codex_hook.py') $installed -Force
 $eventRoot=if($env:CONAN_PET_EVENT_DIR){$env:CONAN_PET_EVENT_DIR}else{Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Codex\ConanAssistant\events'}
 if($eventRoot -notmatch '^[A-Za-z]:\\'){throw '事件目录需要Windows本机绝对路径。'}
 New-Item $eventRoot -ItemType Directory -Force|Out-Null
 $linuxEvents='/mnt/'+$eventRoot.Substring(0,1).ToLower()+'/'+$eventRoot.Substring(3).Replace('\','/')
 @{eventsWindows=$eventRoot;eventsLinux=$linuxEvents}|ConvertTo-Json|Set-Content (Join-Path $installed 'bridge-config.json') -Encoding UTF8
}
$temp=$hookPath+'.conan-tmp'
[IO.File]::WriteAllText($temp,($config|ConvertTo-Json -Depth 32),(New-Object System.Text.UTF8Encoding($false)))
Move-Item $temp $hookPath -Force
Write-Output $(if($Uninstall){'已移除柯南钩子，其他钩子保留。'}else{'已安装柯南状态钩子。请在Codex中审核并信任，然后开始新的回合。'})
