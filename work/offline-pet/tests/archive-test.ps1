$ErrorActionPreference='Stop'
$env:CONAN_PET_TEST_MODE='1'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot -Parent)
$destination=Join-Path $PWD ('taskbar-style-test-'+[Guid]::NewGuid().ToString('N'))
New-Item $destination -ItemType Directory|Out-Null
Copy-Item assets\* $destination -Recurse
$env:CONAN_NOTIFICATION_DIR=Join-Path $destination 'notification-test'
$env:CONAN_PET_EVENT_DIR=Join-Path $destination 'events'
@('x=400','y=300','scale=1.15','roam=false','gaze=false','lines=true')|Set-Content (Join-Path $destination 'settings.ini')
$runtime='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$references=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Xaml.dll','/r:System.Web.Extensions.dll',"/r:$runtime\WPF\WindowsBase.dll","/r:$runtime\WPF\PresentationCore.dll","/r:$runtime\WPF\PresentationFramework.dll")
& "$runtime\csc.exe" /nologo /codepage:65001 /target:exe /platform:x64 /main:ArchiveTests "/out:$destination\ArchiveTests.exe" /win32manifest:app.manifest /resource:src\LimiterBootstrap.ps1,Conan.LimiterBootstrap $references @(Get-ChildItem -LiteralPath (Join-Path $PWD 'src') -Recurse -Filter '*.cs' | Sort-Object FullName | ForEach-Object {$_.FullName}) tests\ArchiveTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& "$destination\ArchiveTests.exe" (Join-Path $PWD '..\..\outputs\v5.23.0-archive')
exit $LASTEXITCODE
