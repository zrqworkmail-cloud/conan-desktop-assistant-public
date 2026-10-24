$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot -Parent)
$destination=Join-Path $env:TEMP ('conan-voice-logic-'+[Guid]::NewGuid().ToString('N'))
New-Item $destination -ItemType Directory|Out-Null
$runtime='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
& "$runtime\csc.exe" /nologo /codepage:65001 /target:exe /r:System.Web.Extensions.dll "/out:$destination\tests.exe" src\VoiceLogic.cs tests\VoiceLogicTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& "$destination\tests.exe"
exit $LASTEXITCODE
