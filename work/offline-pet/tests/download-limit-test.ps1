param([switch]$DriverTest)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot -Parent)
$runtime='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$out=Join-Path $PWD '..\..\outputs\v5.19-monitor'
$references=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Xaml.dll','/r:System.Web.Extensions.dll',"/r:$runtime\WPF\WindowsBase.dll","/r:$runtime\WPF\PresentationCore.dll","/r:$runtime\WPF\PresentationFramework.dll")
& "$runtime\csc.exe" /nologo /codepage:65001 /target:exe /platform:x64 /main:DownloadLimiterTests "/out:$out\DownloadLimiterTests.exe" /resource:src\LimiterBootstrap.ps1,Conan.LimiterBootstrap $references @(Get-ChildItem -LiteralPath (Join-Path $PWD 'src') -Recurse -Filter '*.cs' | Sort-Object FullName | ForEach-Object {$_.FullName}) tests\DownloadLimiterTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
if($DriverTest){& "$out\DownloadLimiterTests.exe" $out (Join-Path $PWD 'dist\network-limit') '--driver'}else{& "$out\DownloadLimiterTests.exe" $out}
exit $LASTEXITCODE
