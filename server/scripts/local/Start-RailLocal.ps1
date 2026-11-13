[CmdletBinding()]
param([switch]$SelfCheck, [switch]$LobbyBootstrap, [string]$PresentationAudit, [string]$UguiPackageRoot,
  [switch]$RailPresentationV1, [string]$RailPresentationExtension, [string]$RailSelectionManifest,
  [switch]$RailPresentationZone02V1, [string]$RailZone02Manifest)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath 'D:/Project/LASTRAIL'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp'
$env:TMP=$env:TEMP; $env:TMPDIR=$env:TEMP
$env:npm_config_cache='D:/Project/LASTRAIL/LocalCache/npm'
$env:XDG_CACHE_HOME='D:/Project/LASTRAIL/LocalCache'
$env:NODE_COMPILE_CACHE='D:/Project/LASTRAIL/LocalCache/node'
New-Item -ItemType Directory -Force -Path $env:TEMP,$env:NODE_COMPILE_CACHE | Out-Null
if ([bool]$PresentationAudit -ne [bool]$UguiPackageRoot) { throw 'PresentationAudit and UguiPackageRoot must be supplied together.' }
if($RailPresentationV1 -and $RailPresentationZone02V1){throw 'Rail presentation profiles are mutually exclusive.'}
if([bool]$RailPresentationZone02V1 -ne [bool]$RailZone02Manifest){throw 'Zone02 profile and selection manifest must be supplied together.'}
if(($RailPresentationV1 -or $RailPresentationZone02V1) -and (!$PresentationAudit -or !$RailPresentationExtension -or !$RailSelectionManifest)){throw 'Rail presentation requires audit, extension and selected manifest paths.'}
if(!$RailPresentationV1 -and !$RailPresentationZone02V1 -and ($RailPresentationExtension -or $RailSelectionManifest)){throw 'Rail presentation inputs require explicit profile.'}
$launchArgs=@('node_modules/tsx/dist/cli.mjs','--tsconfig','tsconfig.base.json','scripts/validation/serve-ce03-local-host.ts','--rail-journey')
if($SelfCheck){$launchArgs+='--self-check'}
if($LobbyBootstrap){$launchArgs+='--lobby-bootstrap'}
if($PresentationAudit){$launchArgs+=@('--presentation-audit',$PresentationAudit,'--unity-project','D:/Project/LASTRAIL/unity-project','--ugui-package-root',$UguiPackageRoot)}
if($RailPresentationV1){$launchArgs+='--rail-presentation-v1'}
if($RailPresentationZone02V1){$launchArgs+=@('--rail-presentation-zone02-v1','--rail-zone02-manifest',$RailZone02Manifest)}
if($RailPresentationV1 -or $RailPresentationZone02V1){$launchArgs+=@('--rail-presentation-extension',$RailPresentationExtension,'--rail-selection-manifest',$RailSelectionManifest)}
Push-Location -LiteralPath 'D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server'
try { & node @launchArgs; exit $LASTEXITCODE } finally { Pop-Location }
