param(
 [ValidateSet('Build','Run','Replay','Manual','QiUpgrade','Capacity','Performance','Tribulation','TrialV13','TrialReplay','TrialProfile','TrialBalance','TrialManual','Playable015','Playable015Replay','Playable016','Playable016Replay')][string]$Mode='Build',
 [Parameter(Mandatory=$true)][string]$OutputRoot,
 [string]$ProjectRoot=(Split-Path $PSScriptRoot -Parent),
 [string]$UnityEditor='C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe',
 [string]$ReplayProfile='')
$ErrorActionPreference='Stop'
$marker='idlegame-owned-windows-v10'
function Assert-NoReparse([string]$path) {
 $at=[System.IO.DirectoryInfo]::new([System.IO.Path]::GetFullPath($path))
 while($null -ne $at){if($at.Exists -and ($at.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Refusing reparse path'};$at=$at.Parent}
}
if(![IO.Path]::IsPathFullyQualified($OutputRoot)){throw 'Absolute OutputRoot required'}
$out=[IO.Path]::GetFullPath($OutputRoot);$project=[IO.Path]::GetFullPath($ProjectRoot)
if($out.StartsWith($project+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $out -eq $project){throw 'Build and test output must be outside source repository'}
Assert-NoReparse $out
New-Item -ItemType Directory -Path $out -Force | Out-Null
$rootMarker=Join-Path $out '.owner'
if(Test-Path -LiteralPath $rootMarker){if([IO.File]::ReadAllText($rootMarker) -ne $marker){throw 'Wrong output owner'}}
elseif(@(Get-ChildItem -LiteralPath $out -Force).Count){throw 'Refusing unowned nonempty output directory'}
else{[IO.File]::WriteAllText($rootMarker,$marker)}
$build=Join-Path $out 'build';$exe=Join-Path $build 'IdleGameV10.exe'
if($Mode -eq 'Build'){
 if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Existing Unity editor process; do not interrupt it'}
 New-Item -ItemType Directory -Path $build -Force | Out-Null
 Assert-NoReparse $build
 $bm=Join-Path $build '.owner';if((Test-Path -LiteralPath $bm) -and [IO.File]::ReadAllText($bm) -ne 'idlegame-v10-build'){throw 'Wrong build owner'}
 if(!(Test-Path -LiteralPath $bm)){[IO.File]::WriteAllText($bm,'idlegame-v10-build')}
 $p=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-nographics','-buildTarget','win64','-projectPath',('"'+$project+'"'),'-executeMethod','WindowsDevelopmentBuildV10.Build','-ownedV10Build',('"'+$exe+'"'),'-logFile',('"'+(Join-Path $out 'build.log')+'"')) -WindowStyle Hidden -PassThru
 $p.WaitForExit();if($p.ExitCode -ne 0){throw "Build failed: $($p.ExitCode); see build.log"};Get-Content -LiteralPath (Join-Path $build 'build-result.json');exit 0
}
if(!(Test-Path -LiteralPath $exe)){throw 'Build first'}
$ownedProfilesRoot=Join-Path $out 'profiles';Assert-NoReparse $ownedProfilesRoot;New-Item -ItemType Directory -Path $ownedProfilesRoot -Force | Out-Null
$pm=Join-Path $ownedProfilesRoot '.owned-player-root';if(Test-Path -LiteralPath $pm){if([IO.File]::ReadAllText($pm) -ne $marker){throw 'Wrong profile parent owner'}}else{[IO.File]::WriteAllText($pm,$marker)}
$ownedProfilePath=Join-Path $ownedProfilesRoot ('idlegame-v10-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $ownedProfilePath | Out-Null
[IO.File]::WriteAllText((Join-Path $ownedProfilePath '.owner'),$marker)
if($Mode -in @('Replay','TrialReplay','Playable015Replay','Playable016Replay')){
 if(![IO.Path]::IsPathFullyQualified($ReplayProfile)){throw 'Absolute owned replay profile required'}
 Assert-NoReparse $ReplayProfile
 if((Split-Path $ReplayProfile -Leaf) -notmatch '^idlegame-v10-[0-9a-f]{32}$' -or [IO.File]::ReadAllText((Join-Path $ReplayProfile '.owner')) -ne $marker -or [IO.File]::ReadAllText((Join-Path (Split-Path $ReplayProfile -Parent) '.owned-player-root')) -ne $marker){throw 'Unowned replay source'}
 $dest=Join-Path $ownedProfilePath 'saves';New-Item -ItemType Directory -Path $dest | Out-Null
 Assert-NoReparse (Join-Path $ReplayProfile 'saves')
 if($Mode -eq 'Playable016Replay'){$files=@('ManualSave1.json')}
 else{
  $files=@('ManualSave2.json','ManualSave2.cultivation-migration.json')
  foreach($pattern in @('ManualSave2.cultivation-*.legacy.bak','ManualSave2.cultivation-*.snapshot.bak','ManualSave2.cultivation-history-*.receipt.json')){$files+=@(Get-ChildItem -LiteralPath (Join-Path $ReplayProfile 'saves') -Filter $pattern -File | ForEach-Object {$_.Name})}
 }
 if($Mode -eq 'TrialReplay'){$files=@('ManualSave1.json','ManualSave2.json','V13InterruptedFixture.json')}
 if($Mode -eq 'Playable015Replay'){
  $sourceSaves=Join-Path $ReplayProfile 'saves';Assert-NoReparse $sourceSaves
  $files=@('AutoSave.json','ManualSave1.json','ManualSave2.json')
  $files+=@(Get-ChildItem -LiteralPath $sourceSaves -Filter 'AutoSave.tribulation-*.snapshot.bak' -File | ForEach-Object {$_.Name})
 }
 foreach($file in $files){$source=Join-Path $ReplayProfile ('saves/'+$file);if((Get-Item -LiteralPath $source).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked replay file refused'};Copy-Item -LiteralPath $source -Destination (Join-Path $dest $file)}
}
$head=git -C $project rev-parse HEAD;$runMode=if($Mode -eq 'Replay'){'replay'}elseif($Mode -eq 'Manual'){'observe'}elseif($Mode -eq 'QiUpgrade'){'v11qi'}elseif($Mode -eq 'Capacity'){'v11capacity'}elseif($Mode -eq 'Performance'){'v12performance'}elseif($Mode -eq 'Tribulation'){'v12tribulation'}elseif($Mode -eq 'TrialV13'){'v13trial'}elseif($Mode -eq 'TrialReplay'){'v13trialreplay'}elseif($Mode -eq 'TrialProfile'){'v15trialprofile'}elseif($Mode -eq 'TrialBalance'){'v15trialbalance'}elseif($Mode -eq 'TrialManual'){'v15trialmanual'}elseif($Mode -eq 'Playable015'){'v15playable'}elseif($Mode -eq 'Playable015Replay'){'v15playablereplay'}elseif($Mode -eq 'Playable016'){'v16playable'}elseif($Mode -eq 'Playable016Replay'){'v16playablereplay'}else{'matrix'}
$style=if($Mode -eq 'TrialManual'){'Normal'}else{'Hidden'}
$player=Start-Process -FilePath $exe -ArgumentList @('-ownedV10Root',('"'+$ownedProfilePath+'"'),'-ownedV10Mode',$runMode,'-ownedV10Head',$head,'-logFile',('"'+(Join-Path $ownedProfilePath 'player.log')+'"'),'-screen-fullscreen','0') -WindowStyle $style -PassThru
Write-Output "OWNED_PROFILE $ownedProfilePath";Write-Output "OWNED_PLAYER_PID $($player.Id)"
if($Mode -eq 'Manual'){exit 0}
$timeoutMs=if($Mode -eq 'TrialBalance'){420000}else{240000}
if(!$player.WaitForExit($timeoutMs)){
 if([IO.Path]::GetFullPath($player.MainModule.FileName) -ne [IO.Path]::GetFullPath($exe)){throw 'Refusing to stop unexpected process'}
 $player.Kill();$player.WaitForExit();throw 'Owned acceptance timed out; stopped only the process launched above'
}
if($player.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $ownedProfilePath 'player.log') -Tail 25;throw "Player failed: $($player.ExitCode)"}
Get-Content -LiteralPath (Join-Path $ownedProfilePath 'evidence/player-result.json')
