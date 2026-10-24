$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
Add-Type @'
using System;using System.Runtime.InteropServices;
public class ConanPerfNative {
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern IntPtr FindWindow(string c,string n);
 [DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
}
'@
$dir=Join-Path $env:TEMP ('conan-v4-perf-'+[Guid]::NewGuid().ToString('N'))
New-Item $dir -ItemType Directory|Out-Null
foreach($name in @('ConanPet.exe','conan.png','conan.ico','台词.txt','detective-console-background-v1.png','detective-button-plate-v1.png')){Copy-Item (Join-Path $root ('dist\'+$name)) $dir}
$env:CONAN_PET_TEST_MODE='1';$env:CONAN_PET_EVENT_DIR=Join-Path $dir 'events';$env:CONAN_NOTIFICATION_DIR=Join-Path $dir 'notifications'
@('x=400','y=300','scale=1.15','roam=false','gaze=false','lines=false')|Set-Content (Join-Path $dir 'settings.ini')
$p=Start-Process (Join-Path $dir 'ConanPet.exe') -PassThru
function Measure-Pet($name){$p.Refresh();$before=$p.TotalProcessorTime.TotalSeconds;$watch=[Diagnostics.Stopwatch]::StartNew();Start-Sleep -Seconds 9;$p.Refresh();return @{mode=$name;seconds=$watch.Elapsed.TotalSeconds;cpu_percent_all_logical_processors=100*($p.TotalProcessorTime.TotalSeconds-$before)/$watch.Elapsed.TotalSeconds/[Environment]::ProcessorCount;working_set_mib=$p.WorkingSet64/1MB;private_memory_mib=$p.PrivateMemorySize64/1MB}}
$handle=[IntPtr]::Zero
try{
 $pidCondition=New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ProcessIdProperty),$p.Id
 $titleCondition=New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty),'柯南桌宠 · 离线版 [Test]'
 $condition=New-Object System.Windows.Automation.AndCondition $pidCondition,$titleCondition
 $deadline=[DateTime]::UtcNow.AddSeconds(12);do{$window=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,$condition);if($window){$handle=[IntPtr]$window.Current.NativeWindowHandle;break};if($p.HasExited){throw ('Test process exited: '+$p.ExitCode)};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow -lt $deadline)
 if($handle -eq [IntPtr]::Zero){$all=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants,$pidCondition);foreach($item in $all){Write-Output ('TEST_WINDOW '+$item.Current.Name)}}
 if($handle -eq [IntPtr]::Zero){throw 'Test pet did not start'}
 $idle=Measure-Pet 'pet_only'
 [ConanPerfNative]::PostMessage($handle,0x0312,[IntPtr]0x4312,[IntPtr]::Zero)|Out-Null
 Start-Sleep -Milliseconds 800
 $consoleTitle=New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty),'柯南 · 侦探控制台 v4';$consoleCondition=New-Object System.Windows.Automation.AndCondition $pidCondition,$consoleTitle;$console=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,$consoleCondition);if(!$console){throw 'Native console failed to open'}
 $dashboard=Measure-Pet 'console_open'
 $result=@{utc=[DateTime]::UtcNow.ToString('o');samples=@($idle,$dashboard);note='Short local observations; main process only, excludes transient NVIDIA driver helper CPU and varies by machine.'}
 $result|ConvertTo-Json -Depth 5|Set-Content (Join-Path $root 'qa\v4-performance.json') -Encoding UTF8
 $result|ConvertTo-Json -Depth 5
}finally{if($handle -ne [IntPtr]::Zero){[ConanPerfNative]::PostMessage($handle,0x10,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null};if(!$p.HasExited){$p.WaitForExit(5000)|Out-Null};if(!$p.HasExited){Stop-Process -Id $p.Id}}
