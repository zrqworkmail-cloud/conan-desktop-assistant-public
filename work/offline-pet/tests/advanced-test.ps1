$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot -Parent)
$files=@('src\Features\Reminders\SkillLogic.cs','src\Features\Reminders\ReminderPoints.cs')
if(Test-Path src\AdvancedLogic.cs){$files+='src\AdvancedLogic.cs'}
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /r:System.Web.Extensions.dll /out:AdvancedTests.exe $files tests\AdvancedTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& .\AdvancedTests.exe
exit $LASTEXITCODE
