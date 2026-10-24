$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot -Parent)
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /r:System.Web.Extensions.dll /out:WorkEndTests.exe src\Features\Reminders\SkillLogic.cs src\Features\Reminders\ReminderPoints.cs src\SkillRuntime.cs tests\WorkEndTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& .\WorkEndTests.exe (Join-Path $PWD 'assets\calendars\2026.json')
exit $LASTEXITCODE
