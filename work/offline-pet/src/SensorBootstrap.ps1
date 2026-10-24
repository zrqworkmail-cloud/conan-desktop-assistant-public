$ErrorActionPreference='Stop'
$admin=New-Object Security.Principal.SecurityIdentifier 'S-1-5-32-544'
$system=New-Object Security.Principal.SecurityIdentifier 'S-1-5-18'
$reader=New-Object Security.Principal.SecurityIdentifier $readerSid
$leases=New-Object 'Collections.Generic.List[IDisposable]'
function Assert-NoLink([string]$path){if(((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Linked sensor path rejected'}}
function Protected-Acl([bool]$directory){
 $acl=if($directory){New-Object Security.AccessControl.DirectorySecurity}else{New-Object Security.AccessControl.FileSecurity}
 $acl.SetOwner($admin);$acl.SetAccessRuleProtection($true,$false)
 $inherit=if($directory){[Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit}else{[Security.AccessControl.InheritanceFlags]::None}
 foreach($sid in @($admin,$system)){$acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($sid,'FullControl',$inherit,'None','Allow')))}
 $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($reader,'ReadAndExecute',$inherit,'None','Allow')))
 return $acl
}
function Digest([IO.Stream]$stream){$sha=[Security.Cryptography.SHA256]::Create();try{return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}}
try{
 # Directory handles prevent substitution of any writable ancestor during elevation and runtime.
 Add-Type -TypeDefinition @'
using System;using System.Runtime.InteropServices;using Microsoft.Win32.SafeHandles;
public class SensorPathLock{[DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]public static extern SafeFileHandle CreateFile(string path,uint access,uint share,IntPtr security,uint mode,uint flags,IntPtr template);}
'@
 $item=Get-Item -LiteralPath $folder -Force
 while($null -ne $item){Assert-NoLink $item.FullName;$handle=[SensorPathLock]::CreateFile($item.FullName,0,3,[IntPtr]::Zero,3,0x02000000,[IntPtr]::Zero);if($handle.IsInvalid){throw 'Cannot lock sensor directory path'};$leases.Add($handle);$item=$item.Parent}
 $allowedDirs=@{}
 foreach($name in $expected.Keys){$parent=Split-Path $name -Parent;if($parent){$allowedDirs[$parent]=$true}}
 foreach($dir in Get-ChildItem -LiteralPath $folder -Directory -Force){Assert-NoLink $dir.FullName;if(-not $allowedDirs.ContainsKey($dir.Name)){throw 'Unexpected sensor directory'}}
 foreach($name in $expected.Keys){$path=Join-Path $folder $name;Assert-NoLink $path;$stream=New-Object IO.FileStream($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$leases.Add($stream);if((Digest $stream) -ne $expected[$name]){throw 'Sensor component hash mismatch'}}
 # This dedicated per-user copy accepts only official files and its own configuration/logs.
 Set-Acl -LiteralPath $folder -AclObject (Protected-Acl $true)
 foreach($dir in Get-ChildItem -LiteralPath $folder -Directory -Force){Assert-NoLink $dir.FullName;if(-not $allowedDirs.ContainsKey($dir.Name)){throw 'Unexpected protected sensor directory'};Set-Acl -LiteralPath $dir.FullName -AclObject (Protected-Acl $true)}
 $files=@(Get-ChildItem -LiteralPath $folder -File -Force)+@(foreach($dir in Get-ChildItem -LiteralPath $folder -Directory){Get-ChildItem -LiteralPath $dir.FullName -File -Force})
 foreach($file in $files){Assert-NoLink $file.FullName;$relative=$file.FullName.Substring($folder.Length+1).Replace('\','/');if(-not $expected.ContainsKey($relative) -and $file.Name -ne 'LibreHardwareMonitor.config' -and $file.Name -notmatch '^LibreHardwareMonitorLog-\d{4}-\d{2}-\d{2}(-\d+)?\.csv$'){throw 'Unexpected sensor file'};Set-Acl -LiteralPath $file.FullName -AclObject (Protected-Acl $false)}
 # The provider copy is dedicated to read-only CPU telemetry. No network server or fan controls.
 $config=Join-Path $folder 'LibreHardwareMonitor.config'
 [xml]$doc='<configuration><appSettings/></configuration>'
 $values=@{logSensorsMenuItem='true';loggingInterval='2';runWebServerMenuItem='false';minCloseMenuItem='true';minTrayMenuItem='true';cpuMenuItem='true';gpuMenuItem='false';mainboardMenuItem='false';hddMenuItem='false';fanControllerMenuItem='false';ramMenuItem='false';powerMonitorMenuItem='false';psuMenuItem='false';batteryMenuItem='false';nicMenuItem='false'}
 foreach($key in $values.Keys){$entry=$doc.CreateElement('add');$entry.SetAttribute('key',$key);$entry.SetAttribute('value',$values[$key]);[void]$doc.SelectSingleNode('/configuration/appSettings').AppendChild($entry)}
 $doc.Save($config);Set-Acl -LiteralPath $config -AclObject (Protected-Acl $false)
 # Recheck the exact retained executable/DLL streams after permission changes.
 for($i=0;$i -lt $leases.Count;$i++){if($leases[$i] -is [IO.FileStream]){$leases[$i].Position=0}}
 foreach($name in $expected.Keys){$path=Join-Path $folder $name;if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected[$name]){throw 'Sensor protected hash mismatch'}}
 $provider=Start-Process -FilePath (Join-Path $folder 'LibreHardwareMonitor.exe') -WorkingDirectory $folder -PassThru -Wait
 exit $provider.ExitCode
}catch{exit 41}finally{foreach($lease in $leases){$lease.Dispose()}}
