$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
$root=Split-Path $PSScriptRoot -Parent
Set-Location $root
$runtime='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$references=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Xaml.dll','/r:System.Web.Extensions.dll',"/r:$runtime\WPF\WindowsBase.dll","/r:$runtime\WPF\PresentationCore.dll","/r:$runtime\WPF\PresentationFramework.dll")
& "$runtime\csc.exe" /nologo /codepage:65001 /main:RuntimeTests /target:exe /platform:x64 /out:RuntimeTests.exe $references @(Get-ChildItem -LiteralPath (Join-Path $PWD 'src') -Recurse -Filter '*.cs' | Sort-Object FullName | ForEach-Object {$_.FullName}) tests\RuntimeTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:CONAN_NOTIFICATION_DIR=Join-Path $env:TEMP ('conan-notification-runtime-'+[Guid]::NewGuid().ToString('N'))
& .\RuntimeTests.exe (Join-Path $root ('runtime-test-'+[Guid]::NewGuid().ToString('N')))
exit $LASTEXITCODE
