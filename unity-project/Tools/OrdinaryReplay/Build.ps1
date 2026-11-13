param([Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../Set-UnityDEnvironment.ps1')
$destination=[IO.Path]::GetFullPath($Output)
if(!$destination.StartsWith('D:\Project\LASTRAIL\QA\',[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $destination)){throw 'Fresh D project QA build directory required.'}
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$scripts=Join-Path $project 'Assets/_LASTRAIL/Scripts'
$editor='D:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Data'
$references=Join-Path $editor 'MonoBleedingEdge/lib/mono/4.8-api'
$json=Join-Path $project 'Library/PackageCache/com.unity.nuget.newtonsoft-json@eebc3d041427/Runtime/Newtonsoft.Json.dll'
$sources=@(Get-Content (Join-Path $PSScriptRoot 'core-sources.txt')|ForEach-Object{Join-Path $scripts $_})
$sources+=Join-Path $scripts 'Diagnostics/OrdinarySharedCoreProbe.cs'
foreach($path in $sources){if([IO.File]::ReadAllText($path) -match '\bUnityEngine\b'){throw "Unexpected engine dependency: $path"}}
[void][IO.Directory]::CreateDirectory($destination)
$common=@('-nologo','-langversion:latest','-nostdlib+','-define:LASTRAIL_SHARED_CORE_QA',"-r:$references/mscorlib.dll","-r:$references/System.dll","-r:$references/System.Core.dll","-r:$references/System.Numerics.dll","-r:$references/Facades/netstandard.dll","-r:$json")
& "$editor/netcorerun/netcorerun.exe" "$editor/DotNetSdkRoslyn/csc.dll" @common '-target:library' "-out:$destination/LastRail.Combat.Core.dll" @sources *> "$destination/core-build.log"
if($LASTEXITCODE -ne 0){throw "Core build failed: $destination/core-build.log"}
& "$editor/netcorerun/netcorerun.exe" "$editor/DotNetSdkRoslyn/csc.dll" @common '-target:exe' "-r:$destination/LastRail.Combat.Core.dll" "-out:$destination/SharedCoreProbe.exe" (Join-Path $PSScriptRoot 'Program.cs') *> "$destination/probe-build.log"
if($LASTEXITCODE -ne 0){throw "Probe build failed: $destination/probe-build.log"}
Copy-Item -LiteralPath $json -Destination "$destination/Newtonsoft.Json.dll"
$inventory=@($sources|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
@{scope='EXPLICIT_QA_CORE_NOT_DEPLOYED_VERIFIER';sources=$inventory;engineReferences=0;coreSha256=(Get-FileHash "$destination/LastRail.Combat.Core.dll").Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 5|Set-Content "$destination/build.json" -Encoding utf8
