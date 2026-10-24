$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$destination=Join-Path $env:TEMP ('conan-action-timing-'+[Guid]::NewGuid().ToString('N'))
New-Item $destination -ItemType Directory|Out-Null
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe "/out:$destination\tests.exe" src\ActionTiming.cs tests\ActionTimingTests.cs
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& "$destination\tests.exe"
exit $LASTEXITCODE
