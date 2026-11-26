$ErrorActionPreference='Stop'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp'; $env:TMP=$env:TEMP; $env:TMPDIR=$env:TEMP
$root='D:/Project/LASTRAIL/QA/DirectionImpact-20260921/combat-b03-hp-v01-review'
$data=Get-Content -LiteralPath 'D:/CodexData/Home/worktrees/5ca3/LASTRAIL/b-migration-finalize-data.json' -Raw | ConvertFrom-Json
$audit=Get-Content -LiteralPath 'D:/CodexData/Home/worktrees/5ca3/LASTRAIL/b-migration-audit-stage.json' -Raw | ConvertFrom-Json
foreach($r in $audit.rows){if(-not(Test-Path -LiteralPath $r.path -PathType Leaf)){throw ('Audit source missing '+$r.path)}}
if($audit.rows.Count -ne 827){throw 'Audit count mismatch'}
Copy-Item -LiteralPath 'D:/CodexData/Home/worktrees/5ca3/LASTRAIL/b-migration-audit-stage.json' -Destination (Join-Path $root 'B_MIGRATION_AUDIT.json')
function Append-Exclusive([string]$path,[string]$text,[string]$anchor){
 $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
 try{$reader=[IO.StreamReader]::new($stream,[Text.UTF8Encoding]::new($false),$true,4096,$true);$current=$reader.ReadToEnd();$reader.Dispose();if($current.Contains($anchor)){throw "Duplicate $path"};$stream.Seek(0,[IO.SeekOrigin]::End)|Out-Null;$bytes=[Text.UTF8Encoding]::new($false).GetBytes($text);$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
 if(-not([IO.File]::ReadAllText($path).Contains($anchor))){throw 'Readback failure'}
}
Append-Exclusive (Join-Path $root 'SCREEN_SPEC.md') $data.spec '## D-B-MIG-01 — B안 현행 채택'
Append-Exclusive (Join-Path $root 'REVIEW.md') $data.record 'id="direction-b-migration-audit-20261002"'
foreach($p in @('D:/Project/LASTRAIL/docs/handoffs/DIRECTION_2-D-20260913.md','D:/Project/LASTRAIL/QA/DirectionImpact-20260921/REVIEW.md','D:/Project/LASTRAIL/docs/handoffs/INTEGRATION_WAITLIST.md','D:/Project/LASTRAIL/docs/handoffs/WORK_QUEUE_DIRECTION.md')){Append-Exclusive $p $data.record 'id="direction-b-migration-audit-20261002"'}
$queue='D:/Project/LASTRAIL/docs/handoffs/WORK_QUEUE_DIRECTION.md'
$stream=[IO.File]::Open($queue,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try{$reader=[IO.StreamReader]::new($stream,[Text.UTF8Encoding]::new($false),$true,4096,$true);$current=$reader.ReadToEnd();$reader.Dispose();$next=$current
foreach($change in $data.changes){if(-not $next.Contains($change[0])){throw ('Queue heading changed '+$change[0])};$next=$next.Replace($change[0],$change[1])}
$pattern='(?ms)^## D-B-MIG-01 — B안 전환 / 착수 가능.*?(?=^#{2,3} |\z)'
$matches=[regex]::Matches($next,$pattern);if($matches.Count -ne 1 -or -not $matches[0].Value.Contains('동일 후보 재생성 금지')){throw 'Migration item changed'}
$next=[regex]::Replace($next,$pattern,'')
$stale='현재 착수 가능한 등록 항목 없음. D-01 Direction 완료 근거:'
$next=$next.Replace($stale,'현재 등록 항목은 아래 최신 역할 큐 및 B 이행 표시를 따른다. 과거 D-01 Direction 완료 근거:')
if(-not $next.Contains('### D-EQ-01@equipment-supply-v18') -or -not $next.Contains('## D-MAINT-LOGIN-01')){throw 'Unrelated queue loss'}
$stream.Seek(0,[IO.SeekOrigin]::Begin)|Out-Null;$stream.SetLength(0);$bytes=[Text.UTF8Encoding]::new($false).GetBytes($next);$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)
}finally{$stream.Dispose()}
$m=Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
$m | Add-Member -NotePropertyName migrationAudit -NotePropertyValue 'B_MIGRATION_AUDIT.json'
$m | Add-Member -NotePropertyName currentPolicy -NotePropertyValue 'combat-b-mainline-approved-20261002'
[IO.File]::WriteAllText((Join-Path $root 'manifest.json'),($m|ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))
$qa=Get-Content -LiteralPath (Join-Path $root 'qa.json') -Raw | ConvertFrom-Json
$summary=[ordered]@{workId='D-B-MIG-01';date='2026-10-02';auditFiles=$audit.rows.Count;auditDirectories=$audit.directories;keywordHitFiles=$audit.hitFiles;sourcePathsExist=$true;currentPolicy='combat-b-mainline-approved-20261002';unchangedHpChecks=($qa.passed.ToString()+'/'+$qa.total);newArtGenerated=$false;artApproval='pending-user';runtimeModified=$false;fullMigrationComplete=$false;successorViewQueueRegistered=$false}
[IO.File]::WriteAllText((Join-Path $root 'B_MIGRATION_QA.json'),($summary|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
$hash=(Get-FileHash -LiteralPath 'D:/Project/LASTRAIL/QA/DirectionImpact-20260921/combat-b-camera03-portrait-final-20261002/combat-b-camera03-portrait-final.png' -Algorithm SHA256).Hash
if($hash -ne '8667AECEC19B7906107F9FB06A66CC042785EF2D6CCEF3E605F9AC90824AF5DA'){throw 'Approved reference changed'}
if([IO.File]::ReadAllText($queue).Contains('## D-B-MIG-01 — B안 전환 / 착수 가능')){throw 'Own task cleanup failed'}
$summary | ConvertTo-Json

