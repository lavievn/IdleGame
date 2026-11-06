param(
 [ValidateSet('Editor','Check','Mono','Native','Build','Player','Replay','Observe','MainGame')][string]$Mode='Editor',
 [string]$StorageRoot=$(if($env:IDLEGAME_HOME){$env:IDLEGAME_HOME}else{'D:\IdleGame'}),
 [string]$UnityEditor='C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe',
 [string]$Group='cultivation-related',
 [string]$Method='CultivationV11CapacityVerifier.RunNative',
 [ValidateSet('Run','Capacity')][string]$PlayerMode='Run',
 [string]$ReplayProfile=''
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$projectEntry=Get-Item -LiteralPath $project -Force
if($projectEntry.LinkType -eq 'Junction'){$project=[IO.Path]::GetFullPath($projectEntry.Target)}
$store=[IO.Path]::GetFullPath($StorageRoot)
if($store.StartsWith('C:\',[StringComparison]::OrdinalIgnoreCase)){throw 'IdleGame data root must be outside C; choose StorageRoot on D'}
foreach($name in @('Temp','Logs','Caches\upm','Tools\mono-bin','Builds')){New-Item -ItemType Directory -Path (Join-Path $store $name) -Force|Out-Null}
$env:IDLEGAME_HOME=$store;$env:TEMP=Join-Path $store 'Temp';$env:TMP=$env:TEMP
$bashTemp=(& 'C:\Program Files\Git\usr\bin\cygpath.exe' -u $env:TEMP).Trim()
$env:TMPDIR=$bashTemp;$env:UPM_CACHE_ROOT=Join-Path $store 'Caches\upm'
foreach($legacy in @('UPM_CACHE_PATH','UPM_NPM_CACHE_PATH')){[Environment]::SetEnvironmentVariable($legacy,$null,'Process')}
Set-Location -LiteralPath $project
if($Mode -eq 'Mono'){
 if($Group -notmatch '^[a-z0-9-]+$'){throw 'Invalid test group'}
 $mono=Join-Path (Split-Path $UnityEditor -Parent) 'Data\MonoBleedingEdge'
 $monoUnix=(& 'C:\Program Files\Git\usr\bin\cygpath.exe' -u $mono).Trim()
 $compiler=$mono.Replace('\','/')+'/lib/mono/4.5/mcs.exe'
 [IO.File]::WriteAllText((Join-Path $store 'Tools\mono-bin\mcs'),"#!/usr/bin/env bash`nexec '$monoUnix/bin/mono.exe' '$compiler' "+'"$@"'+"`n")
 [IO.File]::WriteAllText((Join-Path $store 'Tools\mono-bin\mono'),"#!/usr/bin/env bash`nexec '$monoUnix/bin/mono.exe' "+'"$@"'+"`n")
 $unixBin=(& 'C:\Program Files\Git\usr\bin\cygpath.exe' -u (Join-Path $store 'Tools\mono-bin')).Trim()
 $env:PATH=(Join-Path $store 'Tools\mono-bin')+';'+$env:PATH
 & 'C:\Program Files\Git\bin\bash.exe' 'Tests~/run.sh' ('--'+$Group)
 exit $LASTEXITCODE
}
if($Mode -in @('Build','Player','Replay','Observe')){
 $task=if($Mode -eq 'Build'){'Build'}elseif($Mode -eq 'Replay'){'Replay'}elseif($Mode -eq 'Observe'){'Manual'}else{$PlayerMode}
 & (Join-Path $project 'Tools\Run-WindowsAcceptanceV10.ps1') -Mode $task -OutputRoot (Join-Path $store 'Builds\WindowsAcceptance') -ProjectRoot $project -UnityEditor $UnityEditor -ReplayProfile $ReplayProfile
 exit $LASTEXITCODE
}
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'A Unity Editor is already running; close it before this launch'}
$log=Join-Path $store ('Logs\Unity-'+$Mode+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
$argsList=@('-projectPath',('"'+$project+'"'),'-logFile',('"'+$log+'"'))
if($Mode -eq 'Check'){$argsList+=@('-batchmode','-nographics','-quit')}
if($Mode -in @('Native','MainGame')){
 if($Mode -eq 'MainGame'){$Method='MainGameFoundationProbe.RunIsolated'}
 if($Method -notmatch '^[A-Za-z][A-Za-z0-9_]*\.[A-Za-z][A-Za-z0-9_]*$'){throw 'Explicit static verifier method required'}
 $argsList+=@('-batchmode','-executeMethod',$Method)
 if($Mode -eq 'Native'){$argsList+='-nographics'}
 if($Mode -eq 'MainGame'){$argsList+=@('-foundationValidationRun','-foundationCultivationV11C','-foundationSourceHead',(git rev-parse HEAD));if($ReplayProfile){$argsList+=@('-foundationResumeSavedProfile',('˜'+$ReplayProfile+'˜').Replace('˜','"'),'-foundationResumeSlot','ManualSave2')}}
}
$process=Start-Process -FilePath $UnityEditor -ArgumentList $argsList -WorkingDirectory $project -WindowStyle Hidden -PassThru
Write-Output "PROJECT $project";Write-Output "TEMP $env:TEMP";Write-Output "UPM_CACHE_ROOT $env:UPM_CACHE_ROOT";Write-Output "LOG $log";Write-Output "PID $($process.Id)"
if($Mode -ne 'Editor'){$process.WaitForExit();exit $process.ExitCode}
