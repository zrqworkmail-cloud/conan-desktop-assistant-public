$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$out=Join-Path $root 'dist'
New-Item $out -ItemType Directory -Force|Out-Null
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /codepage:65001 /target:library "/out:$out\Conan.PluginApi.dll" (Join-Path $PSScriptRoot 'sdk\PluginContract.cs')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
