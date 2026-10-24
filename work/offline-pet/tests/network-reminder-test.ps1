$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Set-Location (Join-Path $root 'work\offline-pet')
$runtime='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$out=Join-Path $root ('outputs\network-test-'+[Guid]::NewGuid().ToString('N'))
New-Item $out -ItemType Directory|Out-Null
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Xaml.dll','/r:System.Web.Extensions.dll',"/r:$runtime\WPF\WindowsBase.dll","/r:$runtime\WPF\PresentationCore.dll","/r:$runtime\WPF\PresentationFramework.dll")
& "$runtime\csc.exe" /nologo /codepage:65001 /target:exe /platform:x64 /main:NetworkReminderTests "/out:$out\NetworkReminderTests.exe" $refs @(Get-ChildItem src -Recurse -Filter '*.cs'|Sort-Object FullName|ForEach-Object {$_.FullName}) tests\NetworkReminderTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& "$out\NetworkReminderTests.exe" $out
exit $LASTEXITCODE
