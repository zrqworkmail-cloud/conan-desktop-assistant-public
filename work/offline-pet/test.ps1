$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Set-Location $PSScriptRoot
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:LogicTests.exe src\PetLogic.cs tests\LogicTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& .\LogicTests.exe
exit $LASTEXITCODE
