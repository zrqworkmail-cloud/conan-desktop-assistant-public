param([string]$Folder)
$ErrorActionPreference='Stop'
if(-not $Folder){$base=Split-Path $PSScriptRoot -Parent;$source=Join-Path $base 'assets\voices';$Folder=if(Test-Path $source){$source}else{Join-Path $base 'voices'}}
$manifest=Get-Content (Join-Path $Folder 'sources.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach($clip in $manifest.clips){
 $target=Join-Path $Folder $clip.file
 if((Split-Path $clip.file -Leaf) -ne $clip.file -or $clip.file -notmatch '^[a-z0-9]+\.mp3$'){throw 'Invalid voice file name'}
 $uri=[Uri]$clip.downloadUrl
 if($uri.Scheme -ne 'https' -or $uri.Host -ne 'hoovers.101soundboards.com'){throw 'Unexpected audio source'}
 if((Test-Path $target) -and (Get-FileHash $target -Algorithm SHA256).Hash.ToLowerInvariant() -eq $clip.sha256){continue}
 $partial=$target+'.download'
 Invoke-WebRequest -Uri $uri -OutFile $partial -UseBasicParsing -TimeoutSec 30
 if((Get-FileHash $partial -Algorithm SHA256).Hash.ToLowerInvariant() -ne $clip.sha256){throw ('Voice hash mismatch: '+$clip.id)}
 Move-Item $partial $target -Force
 Write-Output ('Downloaded '+$clip.id)
}
Write-Output 'All voice files verified'
