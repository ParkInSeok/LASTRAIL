# 월드보스 선로 액션 v3 — 서버 로컬 소비 계약

작업 S-WB-RAIL-01 / 정책 44_COMBAT_DETAIL_CONTRACTS.md#world-boss-rail-action-v3-20261002 / wire·snapshot version `world-boss.rail-action.v3.20261002`. 상태: **LOCAL_CANDIDATE 비화면 판정 구현**, 운영 카탈로그/HTTP/원격 DB/실게임 미연결. 정책 승인과 실기·미술·운영 수치 승인을 구분한다. 소비자는 지금 독립 Model/Controller/API adapter 계약 및 로컬 대조에 착수할 수 있다. View/bootstrap은 전체 최종시안 이후다.

## 실제 구조와 소유권

Server combat-authority.ts는 WORLD_BOSS_SOLO/COOP의 SERVER/SERVER_COMBAT 권위를 선언한다. 실제 월드 event/attempt/score/선로 실행 원장은 아직 없다. story RAID_BOSS 및 boss_raider_king_engine은 별도이며 재사용하지 않는다. C3 일반 전투·기존 START·고정 DEV후보를 수정하지 않았다.

`world-boss-rail-contract.ts`: 엄격한 입력/후보 정의·시간표·기하 조건과 고정9패턴 매핑. `world-boss-rail-state.ts`: 캡슐화된 회차 상태·명령·권위 결과·원receipt·snapshot 내보내기/복원. 외부 서버 adapter가 인증 계정/권한, 서버 시각과 동일 시각 순서, 생존·단절·대체 역할, 실제 이동 보장, 원자 DB 저장과 실제 피해를 소유해야 한다. 클라이언트가 definition/availability/serverNow/restore snapshot을 전달해 권위를 얻으면 안 된다. 현재 receipt는 로컬 상태에 저장되어 복구되며 **D1 영속/다중 인스턴스 원자 커밋은 미구현**이다.

## 보스와 신규 패턴 ID

| bossId | phase1 | phase2 | phase3 |
|---|---|---|---|
| world_boss_raider_king | rail-bombardment | traction-chain | main-gun-charge |
| world_boss_veil | boarding-leap | carriage-breach | retreat-reentry |
| world_boss_ort | rail-resonance | polarity-switch | resonance-backflow |

새 ID는 v3 definition/contentVersion 안에서만 사용한다. 표시명/A·B·C/스토리ID를 실행키로 쓰지 않는다. 각 페이즈 round1/2 동일 규칙으로 총6회차. phase1은 선로만, phase2/3은 선로 후 내부1회. 약탈왕/베일/오르트phase1은 예고 안전 좌우1개. 오르트phase2/3은 좌우 모두 안전하고 각 경로 내부 조합이 달라 숨은 단일정답이 없다. 실제 actionId·번호·문양·공격범위는 회차 snapshot에 고정하고 색상만으로 예고하지 않는다.

## 시간·이동 후보

시작시각은 서버 startedAt, 절대종료 startedAt+180000ms, phase [0,60000)/[60000,120000)/[120000,180000). previewAt < lockAt < internalOpenAt < internalCloseAt ≤ resolveAt ≤ tailClearAt ≤ phaseEnd. 모든 숫자는 startedAt 기준 경과ms다. route confirm은 [previewAt,lockAt), 내부 입력은 [internalOpenAt,internalCloseAt). 동일 기한 입력은 거절, 180초 종료가 먼저다. 내부는 잠금 이후여서 솔로 동시 조작을 요구하지 않는다. 지연 입력/스로틀/일반 hold/단절은 시간을 늘리거나 회차를 재발행하지 않는다.

로컬 테스트 후보: 예고 시작8/34,68/94,128/154초; 잠금+4초, 내부+5~7초, 결과+8초, 마지막 객차+15초. 과거 공통8초창을 재사용한 운영 승인이 아니라 테스트용 **새 분리 시간표**다. 선로 길이10m+편성전체 길이10m, 전구간 보장 하한2m/s·상한4m/s·지연500ms이면 보수 통과10.5초, 잠금→tail11초에 들어간다. 모든 값은 LOCAL_TEST_ONLY이며 실제 최대편성/최대길이·가감속/지원속도 근거가 아니다. 검증기: `ceil((branchLengthMm+trainLengthMm)*1000/minTraversalMmPerSecond)+latencyMs ≤ tailClearAt-lockAt`, 이전tailClearAt ≤ 다음previewAt. 최소속도는 가감속을 포함한 전 구간 보장이 필요하다. 실제0속도/정지 지원이면 이 유한 상한을 증명할 수 없으므로 임의통과/객차순간이동으로 채우지 않는다.

로컬 definition.status는 LOCAL_CANDIDATE만 허용한다. geometry/시간표·역할계정·safeSides/entrySafe/경로별 internalActions/공격·표적참조/버프를 명시 입력한다. 운영 PUBLISHED를 같은 파서로 발행하지 않는다. 운영 참조와 인증 START validator는 별도 선행이다.

## 명령·권한·원결과

현재 **로컬 TypeScript 유스케이스**, 실HTTP URL 없음: `WorldBossRailAttempt.command(body, authenticatedAccountId, authoritativeNow, serverAvailability)`.

```json
{"version":"world-boss.rail-action.v3.20261002","attemptId":"attempt","contentVersion":"LOCAL_TEST_ONLY","roundId":"p2r1","commandId":"original-key","expectedRevision":5,"action":{"kind":"CONFIRM_ROUTE","side":"LEFT"}}
```

내부는 `action:{kind:"INTERNAL",actionId:"snapshot에 예고된 입력 ID"}`. client actor/role/AUTO/serverTime/판정 필드를 body에 넣으면 엄격 파서가 거절한다. preview는 클라이언트 표시 상태만, 확정은 이 명령 수락 후 표시. 레버와 기존 경로 터치는 동일 명령에 연결한다. ENTRY는 현재 진입선이며 선택 가능한 세 번째 값이 아니다. 미확정은 ENTRY로 유지하고 snapshot.entrySafe/ENTRY 조합에 따라 실제 판정, 자동안전선택 없음.

SOLO는 두 역할 같은 계정, COOP는 서로 다른 계정. 선로는 trainAccountId, 내부는 operatorAccountId에 한정한다. 외부 인증/현재 계정 적격성은 호출 전 adapter 필수다. 한 회차 확정1회/내부1회; 다른 key의 변경·재입력은 거절. 같은 계정+commandId+원본문(원expectedRevision 포함) 재전송은 기한/현재revision보다 먼저 원receipt를 반환한다. key 같고 본문 다르면 충돌. 원키/본문을 복구 후 그대로 재전송하고 새로운 revision으로 원본문을 바꾸지 않는다.

receipt: commandId/roundId/acceptedRevision/acceptedAt/route/internalAction. 조회 snapshot은 깊은 복사이며 외부 수정이 원본을 바꾸지 않는다. 로컬 restore는 v3 schema+시간/라운드 참조/중복ID·완료결과 대응·원명령/영수증·버프 출처 일관성을 검사한다. failure eventId와 계정/키는 JSON tuple로 식별하여 구분자 포함 ID 충돌을 막는다. 신뢰할 수 있는 영속 상태만 복원하며 arbitrary client snapshot은 수용하지 않는다.

오류를 향후 HTTP adapter에서 매핑할 기준: 형식/AUTO/ENTRY 선택 400; rail_role_forbidden 403; definition/key/revision/이미확정/내부중복/기한·회차닫힘/종료/불가 409; 권위 정의·지원환경 부재503. **이 표는 구현된 HTTP가 아니다.** 오류는 성공receipt·물자·피해·점수로 바꾸지 않는다. actor/권한/revision/명령창 검사에서 거절된 로컬 명령은 상태를 바꾸지 않는다.

## 결과·피해·공용 버프

resolveAt 최종결과는 FULL_SUCCESS/PARTIAL_SUCCESS/FAILURE, 전투다운 CANCELLED 또는 예고 전 역할부재 SKIPPED. 역할의 대체 생존자가 있으면 operatorAvailable=true를 서버가 전달한다. 이미 발행된 회차의 단절만으로 취소하지 않는다. resolveAt과 다운동시각은 기한 판정 우선이며 원 권위 event sequence와 대조해야 한다. 예고에 늦은 scheduler가 lockAt 이후 처음 도착하면 SKIPPED로 기록하고 몰아성공/소급피해를 만들지 않는다. **실제 scheduler 장애는 별도 서버 기술무효 판정·기록이 필요하며 client가 임의 advance를 호출할 수 없다.**

안전선로+내부실패는 railSuccess=true/internalSuccess=false, 선로 회피 유지, 완전성공 버프0. 선로실패도 내부성공 여부를 별도로 보존한다. round 결과와 failure eventId를 한 번 고정, directScore 항상0. 실제 보스 유효피해1:1의 정밀점수 원장은 이 객체가 발급하지 않는다.

failureAttacks는 **피해 적용 전 권위 intent**: eventId, reason, attackRef, targetRef, defensePipeline=SUPPORTED_DEF_REDUCTION_SHIELD, carriageTransfer=false, penalty=false. 이전 최대HP35%·DEF/보호막우회 계산은 없다. 실제 피해량/HP·실드 소모는 여기서 계산하거나 실행하지 않는다. 현재 로컬 후보는 같은 회차 실패를 단일 target/budget로 합쳐 rail 실패가 있으면 railAttackRef, rail 회피 유지시 내부실패이면 internalAttackRef를 출력한다. **서로 다른 예고 공격 2개를 독립 피해로 인정할지와 표적은 미결**이며 운영값이 아니다. 승인된 일반 DEF·감소·보호막 체인 및 원사건 중복 방지와 연결해야 한다. 현재 numeric-defense resolver 주석은 ordinary/story 한정이므로 월드 profile의 자동지원으로 승격하지 않는다.

완전성공은 resolveAt에서 보스피해 슬롯1개, 후보 multiplier12500/13500/15000bps·12000/12000/15000ms. 원buff가 유효하면 같거나강한 새 성공만 원지속으로 교체, 약한buff 연장0. 원round 재처리로재발급0. 시작≤hit<만료, attempt 종료 이후0. 합산 점수에 버프를 다시 곱하지 않는다. 실제 수치 첫 비교값과 최종발행 수치를 구분한다.

## 호환·독립 검증 해제·결정 필요

구 WB2/v2/진행 attempt는 원definition/START/receipt와 원resolver 그대로 보존. readRailSnapshot의 HISTORICAL 분류는 원데이터 보존 라우팅 표시이지 자동 승격/검증 성공이 아니다. unknown/지원 없는 원버전은 실행 금지, v3 restore로 구snapshot을 변환하지 않는다. v3에 새 contentVersion/명시 정의를 pin한 신규 로컬 attempt만 생성한다. 실START·입장권 차감·실보상 경로 추가0.

Unity U-WB-RAIL-02는 이 계약과 로컬 판정에 지금 착수 가능: Model 상태/순차 Controller, 권한·원키·원receipt·재접속 대조, 실제 주행/최대편성/속도 계측. URL·원격 서버가 있는 척하거나 View 연결은 금지. 운영표적/수치 및 실원장은 분리 잔여다.

총괄 결정 항목과 추천:
1. 회차길이/입력창은 최대편성 실제 길이와 Unity 지원 속도·가감속·지연 자료 후 고정. 추천: 선로 구간의 최소 통과속도를 명시 보장하고 그 범위로 모든 객차가 phase 안에서 합류하도록 geometry 조정. 0속도를 지원해야 한다면 정지 가능 시간과 공격/합류 처리부터 결정. 시험10m/2m/s를 운영값으로 채택하지 않음.
2. 선로실패+내부실패가 같은 예고사건이면 단일 budget/원target 1회 추천. 독립2공격이면 각각 예고/타격시각·표적·각 eventId를 승인해야 하며 같은열차 direct/transfer/penalty 중복은 계속 금지. 현재 후보는 단일budget.
3. 실패 공격계수·표적: 원성장·DEF·실드·8/12/16초 복구 자료로 회복 반응창을 비교하여 승인. 최대HP35% 우회 재도입0; 후보 공격ref는 실등록 공격값이 아님. 실제 월드 profile/arena/narrative 참조 검증은 기존 S-WB-01 잔여.
4. 결과시각과 종료·다운동시각은 기존 서버 event 순서 유지. 실제 지연 scheduler의 원기술무효/환급 판정은 기존 월드 실행 원장 구현 때 대조하며 로컬 SKIPPED만으로 실경제 처리하지 않음.

검증: QA/CommanderS1-20260922/world-boss-rail-final6-20261002.json. 9패턴×솔로/협동, 오르트2/3 양쪽·잘못된조합, 안전/위험/미확정·ENTRY실안전, 역할/AUTO/중복/원키충돌/기한/재접속/180초·단절/다운/부분성공/중복피해intent/공용buff/geometry/구snapshot 분리. 제품 입력→실주행·실피해·D1경합·작은화면·원격은 NOT_RUN.
## 중앙 진입선·실제 속도 보충 인수

45#rail-central-entry-review-20261002 반영: 중앙은 공통진입 도식/제3선택 아님. 로컬 geometry.currentPathStatus=LOCAL_LOGICAL_ONLY_UNVERIFIED_Y를 필수로 고정하여 ENTRY가 실제 제3선로인 척 발행하지 않는다. 현재 숫자 길이/속도 상한 검증은 후미 시간의 로컬 후보 검산이며 ENTRY가 Y에서 어떤 월드 좌표 경로로 연결되는지 입증하지 않는다. 이 물리 geometry는 미완료이고 실주행/운영 발행을 차단한다.

필요 결정: 추천 후보는 예고 전에 원geometry에 물리적으로 연속인 기존 진행경로를 명시 pin하고 현재진행 위험을 표시하는 방식. 좌우 후보 중 하나를 기한에 자동으로 고르는 방식은 금지. 실제 선형으로 구현 가능한지 Unity가 조사한 뒤 기존 진행선의 분기 연결점·좌표·공격범위를 Server가 대조해야 한다. 순수 Y라 미확정 입력에 연속경로가 없으면 현 정책을 물리적으로 만족 못한다고 보고하며, 현재 코드에 임의세갈래/좌우선택을 추가하지 않는다. 제3출구는 별도사용자 결정 없이는 제외한다.

actual/target/forcedSlowdown은 실제 이동 소유자의 독립 필드이고 자동 actual 손잡이 갱신은 목표명령을 재발행하지 않는다. Server 도메인은 View나 속도값을 새로소유하지 않는다. 현재 Unity20/20 표본2~4객차/7~13m/s/4.1lag는 운영상한이 아님. 일반D1 미결hold는 월드에 도입하지 않으며 월드 시계는 지속한다.
