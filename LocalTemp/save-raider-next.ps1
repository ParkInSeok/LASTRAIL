$env:TEMP='D:/Project/LASTRAIL/LocalTemp'
$env:TMP=$env:TEMP
$env:TMPDIR=$env:TEMP
$enc=[Text.UTF8Encoding]::new($false)
$p=Get-Content -LiteralPath 'D:/CodexData/Home/worktrees/5ca3/LASTRAIL/LocalTemp/raider-next-art-payload.json' -Raw | ConvertFrom-Json
foreach($f in $p.files){
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($f.path)) | Out-Null
 if(Test-Path -LiteralPath $f.path){throw "Candidate target already exists: $($f.path)"}
 [IO.File]::WriteAllText($f.path,$f.text,$enc)
}
foreach($c in $p.copies){
 if(!(Test-Path -LiteralPath $c.from)){throw "Missing source: $($c.from)"}
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($c.to)) | Out-Null
 Copy-Item -LiteralPath $c.from -Destination $c.to -ErrorAction Stop
}
Add-Type -AssemblyName System.Drawing
foreach($root in $p.roots){
 $m=Get-Content -LiteralPath "$root/manifest.json" -Raw | ConvertFrom-Json
 $rows=@()
 foreach($s in $m.screens){
  $im=[Drawing.Image]::FromFile("$root/$($s.file)")
  $rows+= [pscustomobject]@{file=$s.file;width=$im.Width;height=$im.Height;sha256=(Get-FileHash -LiteralPath "$root/$($s.file)" -Algorithm SHA256).Hash}
  $im.Dispose()
 }
 $q=[ordered]@{pngAndJson='PASS';screens=$rows;visualInspection='Generated outputs visually inspected: selection/lock/confirm, neutral current, cruise/support3; P2 connected slack chain and same pin before/after corrected';artApproval='PENDING USER REVIEW';deviceInputAlphaGeometryRuntime='NOT RUN';wholeTaskCompletion=$false;productCodeChanged=$false}
 [IO.File]::WriteAllText("$root/qa.json",($q | ConvertTo-Json -Depth 12),$enc)
}
$root='D:/Project/LASTRAIL/QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v04-illustration'
$m=Get-Content -LiteralPath "$root/manifest.json" -Raw | ConvertFrom-Json
$q=Get-Content -LiteralPath "$root/qa.json" -Raw | ConvertFrom-Json
foreach($s in $q.screens){if((Get-FileHash -LiteralPath "$root/$($s.file)" -Algorithm SHA256).Hash -ne $s.sha256){throw "Approved image changed"}}
$m.status='approved-final-illustration';$m.approval='user-approved-20261002 R01-R02-R03-R04 revised composition'
foreach($s in $m.screens){$s.approval='user-approved-20261002'}
$q.composedRevision='USER APPROVED 20261002; device/independent/physics/runtime NOT RUN'
[IO.File]::WriteAllText("$root/manifest.json",($m|ConvertTo-Json -Depth 24),$enc)
[IO.File]::WriteAllText("$root/qa.json",($q|ConvertTo-Json -Depth 24),$enc)
function AppendExclusive([string]$path,[string]$anchor,[string]$body){
 $fs=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
 try{
  $sr=[IO.StreamReader]::new($fs,[Text.Encoding]::UTF8,$true,4096,$true);$old=$sr.ReadToEnd();$sr.Dispose()
  if(!$old.Contains($anchor)){
   $fs.Seek(0,[IO.SeekOrigin]::End)|Out-Null
   $bytes=$enc.GetBytes("`r`n"+$body+"`r`n");$fs.Write($bytes,0,$bytes.Length);$fs.Flush($true)
  }
 }finally{$fs.Dispose()}
 if(!([IO.File]::ReadAllText($path).Contains($anchor))){throw 'Append readback failed'}
}
$ordinaryApproval=@'
## 2026-10-02 수정 일반 작전 4종 최종 미술 승인
<a id="ordinary-v04-revision-approved-20261002"></a>
사용자 "수정된 시안 승인 다음 최종시안들 제작"으로 R01~R04 수정 조합 모두 최종 미술 승인. PNG 4종 SHA256 기존 QA와 동일 확인. manifest/qa 승인 등록. 실제 기기/터치/독립alpha·동적조합/물리·후미/runtime 검증 완료를 뜻하지 않으며 후속 잔여 유지.
'@
foreach($name in @('REVIEW.md','SCREEN_SPEC.md')){AppendExclusive "$root/$name" 'ordinary-v04-revision-approved-20261002' $ordinaryApproval}
$html=[IO.File]::ReadAllText("$root/index.html")
if(!$html.Contains('ordinary-v04-revision-approved-20261002')){
 $html=$html.Replace('<body>','<body><p id="ordinary-v04-revision-approved-20261002" style="padding:16px;color:#9fddac">2026-10-02 수정 4종 최종 미술 승인 · 실제 기기/독립조합/런타임 검증 잔여</p>')
 [IO.File]::WriteAllText("$root/index.html",$html,$enc)
}
$body=@'
### 일반 v04 수정 승인 · 약탈왕 반격/견인사슬 후보 5종 — 2026-10-02
<a id="raider-p1-w04-p2-review-produced-20261002"></a>
D-RAIL-ART-01: 사용자 수정 시안 승인으로 [일반 v04 R01~R04](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v04-illustration/index.html) 최종미술 승인 등록. 4종 PNG SHA256 기존QA동일, manifest/qa/REVIEW/명세/갤러리 승인 확인. 독립조합·실기/물리/runtime 잔여 유지.
D-WB-RAIL-01@world-boss-rail-action-v3-20261002: [P1 v05 W04 반격 후보](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v05-illustration/index.html) 및 [P2 v01 견인사슬 4상태 후보](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p2-v01-illustration/index.html). 각각 manifest/SCREEN_SPEC/REVIEW/PROMPTS/qa/INDEPENDENT_ELEMENTS 연결. P1 승인 v04 W01~W03 외부참조 보존, 새5종 사용자미술승인대기.
큰 가로원통그립·중앙현재·명시확정/수락후잠금/순항·지원3 유지. W04 후미 박스형객차·반격광. P2 예고/미리보기→수락잠금→사슬느슨(연결유지)/내부안전핀→동일핀인출/견인해제 전체성공예시. 잘못된 초기그립/끊어진사슬/다른핀 생성3종 history보존 후 builtin imagegen수정. P2좌측안전선/위험선 배치는 미연결fixture이며 다른패턴 일괄RIGHT안전정책 추가0. 비교 버프 운영값 굽기0.
PNG5/JSON/로컬갤러리참조검사·생성본육안검토. 기존v10승인기관차/객차/보스열차/인물 존재 대조. 사슬/핀/공통그립독립자산 이번산출물미공급, 기존객차원본/후보카메라조합검토필요. 실제360/430·긴화면/44px·alpha동적조합/실ID·geometry/last-rear-clear/합류·원거래/HTTP/runtime NOT RUN.
전체9패턴/회차·양방향·실패/협동/복구·전체최종미술/독립자산 잔여로 자기큐삭제·미충족View/bootstrap후속선등록0. 순수물리준비는미술승인선행으로막지않음. 제품코드·타담당원본·운영배포/실입장/실보상변경0. START snapshot/역사원화 보존.
'@
foreach($doc in @('DIRECTION_2-D-20260913.md','WORK_QUEUE_DIRECTION.md','INTEGRATION_WAITLIST.md')){
 AppendExclusive "D:/Project/LASTRAIL/docs/handoffs/$doc" 'raider-p1-w04-p2-review-produced-20261002' $body
}
$p.roots | ForEach-Object{Get-Content -LiteralPath "$_/qa.json" -Raw}

