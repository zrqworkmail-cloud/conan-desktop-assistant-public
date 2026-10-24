$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot -Parent)
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /r:System.Web.Extensions.dll /out:SkillsTests.exe src\Features\Reminders\SkillLogic.cs src\Features\Reminders\ReminderPoints.cs tests\SkillsTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& .\SkillsTests.exe
exit $LASTEXITCODE
