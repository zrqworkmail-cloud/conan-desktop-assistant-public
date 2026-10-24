$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
$runtime='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$sdk='C:\Program Files (x86)\Windows Kits\10'
$version=Get-ChildItem "$sdk\UnionMetadata" -Directory | Where-Object {$_.Name -match '^10\.'} | Sort-Object Name -Descending | Select-Object -First 1 -ExpandProperty Name
$out=Join-Path (Split-Path $PSScriptRoot -Parent) 'dist\notification-bridge'
$package=Join-Path $out 'package'
New-Item $package -ItemType Directory -Force|Out-Null
$refs=@('/r:System.Xaml.dll','/r:System.Web.Extensions.dll',"/r:$runtime\System.Runtime.dll","/r:$runtime\System.Runtime.InteropServices.WindowsRuntime.dll","/r:$runtime\System.Runtime.WindowsRuntime.dll","/r:$runtime\WPF\WindowsBase.dll","/r:$runtime\WPF\PresentationCore.dll","/r:$runtime\WPF\PresentationFramework.dll","/r:$sdk\UnionMetadata\$version\Windows.winmd")
& "$runtime\csc.exe" /nologo /codepage:65001 /target:winexe /platform:x64 "/out:$package\ConanNotificationBridge.exe" $refs (Join-Path $PSScriptRoot 'NotificationBridge.cs')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
Copy-Item (Join-Path $PSScriptRoot 'AppxManifest.xml') $package -Force
Add-Type -AssemblyName System.Drawing
$icon=New-Object System.Drawing.Icon (Join-Path (Split-Path $PSScriptRoot -Parent) 'assets\conan.ico')
$bitmap=$icon.ToBitmap();$bitmap.Save((Join-Path $package 'logo.png'),[System.Drawing.Imaging.ImageFormat]::Png);$bitmap.Dispose();$icon.Dispose()
& "$sdk\bin\$version\x64\makeappx.exe" pack /d $package /p (Join-Path $out 'ConanNotificationBridge-unsigned.msix') /o
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
Copy-Item (Join-Path $PSScriptRoot '安装说明.md') $out -Force
Write-Output "Built unsigned notification package: $out"
