$ErrorActionPreference='Stop'
# This script is embedded in ConanPet.exe, not loaded from the writable package directory.
$adminSid=New-Object Security.Principal.SecurityIdentifier 'S-1-5-32-544'
$systemSid=New-Object Security.Principal.SecurityIdentifier 'S-1-5-18'
$userSid=New-Object Security.Principal.SecurityIdentifier 'S-1-5-32-545'
$sourceHandles=New-Object 'System.Collections.Generic.List[System.IDisposable]'
$sourceByName=@{}
$targetHandles=New-Object 'System.Collections.Generic.List[System.IDisposable]'
function Assert-NoLink([string]$Path){
 $item=Get-Item -LiteralPath $Path -Force
 while($null -ne $item){if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Reparse points are not allowed in protected destinations'};$item=if($item -is [IO.FileInfo]){$item.Directory}else{$item.Parent}}
}
function Assert-Protected([string]$Path){
 Assert-NoLink $Path
 $acl=Get-Acl -LiteralPath $Path
 $owner=$acl.GetOwner([Security.Principal.SecurityIdentifier]).Value
 if($owner -ne $adminSid.Value -and $owner -ne $systemSid.Value){throw 'Untrusted owner on protected component path'}
 $write=[Security.AccessControl.FileSystemRights]::Write -bor [Security.AccessControl.FileSystemRights]::Delete -bor [Security.AccessControl.FileSystemRights]::ChangePermissions -bor [Security.AccessControl.FileSystemRights]::TakeOwnership -bor [Security.AccessControl.FileSystemRights]::DeleteSubdirectoriesAndFiles
 foreach($rule in $acl.GetAccessRules($true,$true,[Security.Principal.SecurityIdentifier])){
  if($rule.AccessControlType -eq 'Allow' -and ($rule.FileSystemRights -band $write) -ne 0 -and $rule.IdentityReference.Value -ne $adminSid.Value -and $rule.IdentityReference.Value -ne $systemSid.Value){throw 'Writable protected component path'}
 }
}
function New-ComponentAcl([bool]$Directory){
 $acl=if($Directory){New-Object Security.AccessControl.DirectorySecurity}else{New-Object Security.AccessControl.FileSecurity}
 $acl.SetOwner($adminSid);$acl.SetAccessRuleProtection($true,$false)
 $inherit=if($Directory){[Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit}else{[Security.AccessControl.InheritanceFlags]::None}
 foreach($sid in @($adminSid,$systemSid)){$acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($sid,'FullControl',$inherit,'None','Allow')))}
 $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($userSid,'ReadAndExecute',$inherit,'None','Allow')))
 return $acl
}
function Ensure-ProtectedDirectory([string]$Path){
 if(Test-Path -LiteralPath $Path){Assert-Protected $Path;return}
 Assert-Protected (Split-Path $Path -Parent)
 [void][IO.Directory]::CreateDirectory($Path,(New-ComponentAcl $true))
 Assert-Protected $Path
}
function Hash-Stream([IO.Stream]$Stream){$sha=[Security.Cryptography.SHA256]::Create();try{return [BitConverter]::ToString($sha.ComputeHash($Stream)).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}}
try{
 # Verify and keep the exact source handles: pathname replacement cannot change copied bytes.
 foreach($name in $expected.Keys){
  $path=Join-Path $source $name
  if(([IO.File]::GetAttributes($path) -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Linked source component'}
  $stream=New-Object IO.FileStream($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  $sourceHandles.Add($stream);$sourceByName[$name]=$stream
  if((Hash-Stream $stream) -ne $expected[$name]){throw 'Source component hash mismatch'}
 }
 $programFiles=[Environment]::GetFolderPath('ProgramFiles')
 Assert-NoLink $programFiles
 # Program Files may include TrustedInstaller/CREATOR OWNER ACEs. Its existing OS ACL is not rewritten.
 $product=Join-Path $programFiles 'ConanDesktopAssistant.Security'
 if(-not (Test-Path -LiteralPath $product)){[void][IO.Directory]::CreateDirectory($product,(New-ComponentAcl $true))}
 Assert-Protected $product
 $target=Join-Path $product $expected['ConanBandwidth.exe']
 Ensure-ProtectedDirectory $target
 foreach($name in $expected.Keys){
  $path=Join-Path $target $name
  if(-not (Test-Path -LiteralPath $path)){
   $output=New-Object IO.FileStream($path,[IO.FileMode]::CreateNew,[Security.AccessControl.FileSystemRights]::Write,[IO.FileShare]::None,4096,[IO.FileOptions]::None,(New-ComponentAcl $false))
   try{$inputStream=$sourceByName[$name];$inputStream.Position=0;$inputStream.CopyTo($output);$output.Flush($true)}finally{$output.Dispose()}
  }
  Assert-Protected $path
  $verified=New-Object IO.FileStream($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  $targetHandles.Add($verified)
  if((Hash-Stream $verified) -ne $expected[$name]){throw 'Protected component hash mismatch'}
 }
 # Holding read-only handles through process exit prevents DLL replacement after validation.
 $helper=Start-Process -FilePath (Join-Path $target 'ConanBandwidth.exe') -ArgumentList $helperArgs -WorkingDirectory $target -Wait -PassThru -WindowStyle Hidden
 exit $helper.ExitCode
}catch{exit 41}finally{foreach($stream in $targetHandles){$stream.Dispose()};foreach($stream in $sourceHandles){$stream.Dispose()}}
