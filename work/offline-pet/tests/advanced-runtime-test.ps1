. (Join-Path $PSScriptRoot 'plugin-test-reference.ps1')
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot -Parent)
Copy-Item $pluginApi (Split-Path "AdvancedRuntimeTests.exe" -Parent) -Force
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe $pluginApiReference /nologo /codepage:65001 /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:AdvancedRuntimeTests.exe src\Features\Reminders\SkillLogic.cs src\Features\Reminders\ReminderPoints.cs src\AdvancedLogic.cs src\AdvancedRuntime.cs tests\AdvancedRuntimeTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$destination=Join-Path $PWD ('advanced-runtime-test-'+[Guid]::NewGuid().ToString('N'))
& .\AdvancedRuntimeTests.exe $destination
exit $LASTEXITCODE
