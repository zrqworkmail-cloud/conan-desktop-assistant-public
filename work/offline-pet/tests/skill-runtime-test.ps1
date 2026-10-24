$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot -Parent)
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:SkillRuntimeTests.exe src\Features\Reminders\SkillLogic.cs src\Features\Reminders\ReminderPoints.cs src\SkillRuntime.cs src\DesktopActions.cs tests\SkillRuntimeTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$destination=Join-Path $PWD ('skill-runtime-test-'+[Guid]::NewGuid().ToString('N'))
& .\SkillRuntimeTests.exe $destination
exit $LASTEXITCODE
