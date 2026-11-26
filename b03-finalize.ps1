$env:TEMP='D:/Project/LASTRAIL/LocalTemp'; $env:TMP=$env:TEMP; $env:TMPDIR=$env:TEMP
$root='D:/Project/LASTRAIL/QA/DirectionImpact-20260921/combat-b03-hp-v01-review'
[IO.File]::WriteAllText((Join-Path $root 'REVIEW.md'),@'
# D-B03-HP-01 제작·검수 — 2026-10-02
상태: 신규 미술 사용자 승인 대기. 시안 제작/브라우저 보조 검수 완료, 런타임 완료 아님.
## 산출물
- [기본 통짜 후보](combat-b03-hp-v01.png) / [갤러리](index.html) / [상태 비교](states.html) / [명세](SCREEN_SPEC.md) / [프롬프트](PROMPTS.md) / [QA](qa.json).
- 원본 승인 B03은 보존. 생성 원본 exec-1ea291c8-4b5d-489e-9252-999e82f09035.png 보존.
- 계약: 44 요격·등반 부분승인, 객차/다운/짐칸v02 및 v03 승인·우선순위 정정. v04 미승인 초안 제외.
## 검수 근거
Chrome headless / Playwright 360·430·720px × 상태7종 + 입력 분리/이미지 검증 48/48 PASS. captures/에 기본3장 및 상태 카드21장.
확인: 페이지 가로 넘침0, 카드5, 짐칸 요원HP/충전/스킬0, 선택 실제 표적1, 파손만으로 스킬 비활성화0, 다운/동시 때 스킬 비활성화, 객차 상세는 유지, 카드 행/스킬 버튼44px 이상, 도해 라벨12px 이상, 상세 입력이 스킬 입력을 중복 실행하지 않음, 오류0.
육안: 생성 기본 후보(원통손잡이/중앙/속도/지원3/병주/바위 유지), broken-360 / carriage-360 / base-360 확인. 상태 도해는 초상 대신 검토용 심볼 사용.
## 잔여와 한계
360px 통짜 축소 시 작은 HP 라벨은 읽기 어렵다. 기본 PNG 확대본과 native 상태 도해를 함께 검토; 실제 전투장면·HUD 결합에서 폰트/터치/카드 공간을 별도 조합 검수해야 한다. 위 PASS를 작은 통짜 이미지 가독성 완료로 간주하지 않음.
실제 요원 START ID/객차carId/공격원대상/실기터치/피해·복구/공통 팝업 승인 미술 조합/alpha 분리/색각 검수 NOT RUN. 상세 팝업은 QA 의미 연결 도해이며 기존 승인 공통팝업을 대체하는 새 미술 승인 아님. 가동 중단 목록·복구시간/진행률·범위·계수 미확정, 시안 예시HP100을 승인 게임값으로 적용 금지.
합성 reference-only. 미술승인/독립자산/전체 최종시안 후 View/bootstrap 소비 조건 유지. Unity/Server/배포/실입장·보상·영상 변경0.

'@,[Text.UTF8Encoding]::new($false))
$m=Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
$m | Add-Member -NotePropertyName imageSha256 -NotePropertyValue ((Get-FileHash -LiteralPath (Join-Path $root 'combat-b03-hp-v01.png') -Algorithm SHA256).Hash)
[IO.File]::WriteAllText((Join-Path $root 'manifest.json'),($m|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
$record=@'

### D-B03-HP-01 수정 후보 제작·검수 — 2026-10-02
<a id="combat-b03-hp-v01-produced-20261002"></a>
- 사용자 직접 제작 지시(Plan 전달 근거 확인), QA/DirectionImpact-20260921/combat-b03-hp-v01-review/index.html / combat-b03-hp-v01.png / states.html / SCREEN_SPEC.md / REVIEW.md / manifest.json / qa.json / PROMPTS.md.
- direction.combat-b03-hp-v01.20261002. 승인 B03 원본 보존, ③ 세로 정후방·좌측 병주·전방 바위·원통 선로손잡이·중앙표시·속도·지원3 유지. 요원/객차/충전 분리, 짐칸 객차만. v03 정정 우선/v04 미승인 반영0. 원본 네 요원+짐칸 예시를 고정 runtime 편성/ID로 승격0.
- 기본 통짜 후보1+상태 도해7종(정상/파손/다운/동시/짐칸피격·조준/요원표적/객차표적). 생존 요원 있어도 객차 조준; 실제표적한곳; 파손시 전체카드 잠금0.
- Chrome headless 360/430/720px 48/48 PASS. 기본3/카드21캡처, 생성본과360파손/객차표적/기본 육안. 기본PNG 360px 작은라벨 가독성 잔여. 독립상태도해의12px/44px 표시검수와 통짜축소 가독성 구분.
- 신규 미술승인 대기. reference-only; 실기/독립alpha/START·carId/원표적·피해·복구/공통팝업미술조합/최종HUD 검수 미실행. 전체최종시안 후 View/bootstrap 조건 유지. Unity/Server/운영·실입장·보상·영상 변경0. 이 항목은 후보 제작 완료이며 미술 승인/전체B전환 완료 아님.

'@
function Append-Exclusive([string]$path,[string]$text){
 $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
 try{$reader=[IO.StreamReader]::new($stream,[Text.UTF8Encoding]::new($false),$true,4096,$true); $current=$reader.ReadToEnd();$reader.Dispose();if($current.Contains('id="combat-b03-hp-v01-produced-20261002"')){throw "Duplicate anchor $path"}; $stream.Seek(0,[IO.SeekOrigin]::End)|Out-Null; $bytes=[Text.UTF8Encoding]::new($false).GetBytes($text);$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
 if(-not([IO.File]::ReadAllText($path).Contains('id="combat-b03-hp-v01-produced-20261002"'))){throw 'Readback failed'}
}
foreach($path in @('D:/Project/LASTRAIL/docs/handoffs/DIRECTION_2-D-20260913.md','D:/Project/LASTRAIL/QA/DirectionImpact-20260921/REVIEW.md','D:/Project/LASTRAIL/docs/handoffs/INTEGRATION_WAITLIST.md','D:/Project/LASTRAIL/docs/handoffs/WORK_QUEUE_DIRECTION.md')){Append-Exclusive $path $record}
$queue='D:/Project/LASTRAIL/docs/handoffs/WORK_QUEUE_DIRECTION.md'
$stream=[IO.File]::Open($queue,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try{
$reader=[IO.StreamReader]::new($stream,[Text.UTF8Encoding]::new($false),$true,4096,$true);$current=$reader.ReadToEnd();$reader.Dispose()
$pattern='(?ms)^### D-B03-HP-01 — B안 ③ 세로 최종 시안 상태 표시 수정 후보.*?(?=^### |\z)'
$matches=[regex]::Matches($current,$pattern);if($matches.Count -ne 1){throw 'Queue target mismatch'}
if(-not $matches[0].Value.Contains('완료 근거')){throw 'Queue contract changed'}
$next=[regex]::Replace($current,$pattern,'',1)
$stream.Seek(0,[IO.SeekOrigin]::Begin)|Out-Null;$stream.SetLength(0);$bytes=[Text.UTF8Encoding]::new($false).GetBytes($next);$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)
}finally{$stream.Dispose()}
$verified=[IO.File]::ReadAllText($queue)
if($verified.Contains('### D-B03-HP-01 —')){throw 'Queue removal failed'}
Get-Content -LiteralPath (Join-Path $root 'qa.json') -TotalCount 8
Write-Output 'Artifacts and four records saved; own completed candidate-production queue item removed.'

