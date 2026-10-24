# Read an already-running LibreHardwareMonitor WMI provider; never install or start drivers.
$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
try {
 $sensors=Get-CimInstance -Namespace 'root/LibreHardwareMonitor' -ClassName Sensor -OperationTimeoutSec 1
 $cpu=$sensors|Where-Object {$_.SensorType -eq 'Temperature' -and $_.Identifier -match '^/(intelcpu|amdcpu|cpu)/'}|Measure-Object Value -Maximum
 $gpu=$sensors|Where-Object {$_.SensorType -eq 'Temperature' -and $_.Identifier -match '^/gpu'}|Measure-Object Value -Maximum
 $disk=$sensors|Where-Object {$_.SensorType -eq 'Temperature' -and $_.Identifier -match '^/(hdd|nvme|ssd)/'}|Measure-Object Value -Maximum
 @{Cpu=$cpu.Maximum;Gpu=$gpu.Maximum;Disk=$disk.Maximum}|ConvertTo-Json -Compress
} catch { Write-Output '{}' }
