. (Join-Path $PSScriptRoot 'plugin-test-reference.ps1')
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot -Parent)
Copy-Item $pluginApi (Split-Path "BubbleTests.exe" -Parent) -Force
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe $pluginApiReference /nologo /codepage:65001 /out:BubbleTests.exe src\BubbleLogic.cs tests\BubbleTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& .\BubbleTests.exe
exit $LASTEXITCODE
