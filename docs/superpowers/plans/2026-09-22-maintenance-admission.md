# Maintenance Admission Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 신규 출격·광고·구매 승인과 점검 전환을 같은 DB 승인 경계에서 판정하고 기존 원결과/유효 거래 복구를 보존한다.
**Architecture:** 실효 정책은 MaintenanceRuntimePolicyStore 하나가 소유한다. 초안과 가상 미리보기는 분리하고, 신규 승인 batch 내부의 DB guard가 저장 순간 정책·시각을 검사한다. 운영 활성화 HTTP는 계속503이다.
**Tech Stack:** 기존 TypeScript/Zod/Hono/D1/SQL.js/Vitest. 새 의존성 없음.
**Spec:** D:/Project/LASTRAIL/docs/superpowers/specs/2026-09-22-maintenance-admission-design.md (사용자 2026-09-22 요청27 승인).

## Global Constraints

- 실제 개발 원본 D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server. 아래 파일은 이 디렉터리 상대경로다.
- QA는 D:/Project/LASTRAIL/QA/MaintenancePlanServer-20260921/admission-20260922, TEMP/TMP/TMPDIR는 D:/Project/LASTRAIL/LocalTemp.
- 실행 전 using-git-worktrees 지침에 따라 현재 변경 보존·격리 경로를 확인한다. stale 앱 worktree 사용/전체 stage/reset/clean 금지. 기존 변경과 새 두 preview 파일을 잃지 않는다.
- 현재 DEV ec32703/DB0047/RUNNING/광고OFF 유지. 원격 쓰기·이관·배포·실제 점검 활성화·광고ON 없음.
- migration에는 운영 seed 없음. 누락 정책은 신규 승인 불가; 정상 정책도 명시한다.
- GAME 신규 승인만 범위. 전체 게임 쓰기/WEB/전투 checkpoint/기한 보정/공개 outbox/소비자 UI는 별도다.
- 기존 인증·광고OFF·전투 readiness·공시 pin·스토어 검증을 완화하지 않는다.
- 구현 전 migration0048이 미사용인지 확인하고 충돌 시 다음 미사용 번호를 선택, 이후 참조/테스트를 함께 갱신한다.

## Review Focus

1. 패스50 신규 결제도 차단 대상이며 RECONCILE은 기존 검증 후 복구 가능해야 한다(Task4).
2. 사전 조회 이후 정책은 같아도 DB 시간이 drain 경계를 넘을 수 있다(Task2).
3. 광고 기회가 먼저 생성됐어도 새 광고시작승인은 점검 이후 발급되면 안 된다(Task4).
4. 저장 실패 후 동일 요청이 이미 성공한 경우 현재 점검보다 원영수증 복구가 우선한다(Task3/4).
5. runs/pass_checkout_intents에 환경열이 없어 클라이언트/행 추정 환경으로 다른 정책을 고르면 안 된다(Task2/3/4).

## 저장·인터페이스 계약

신규 migration `packages/db/migrations/0048_maintenance_runtime_policy.sql`:
- maintenance_runtime_revisions: environment DEV/PRD, application_id, revision 양의 안전정수, state NORMAL/SCHEDULED/ACTIVE/COMPLETED/CANCELLED, maintenance_id/draft_revision, drain_starts_at/starts_at/estimated_end_at, ended_at, actor_id/reason/created_at. 복합PK(environment,application_id,revision). 초안 연결과 시각의 일관성 CHECK. NORMAL 초기개정만 원초안 null 허용. UPDATE/DELETE 거절.
- maintenance_runtime_heads: (environment,application_id) PK, revision과 revisions 복합FK. 변경은 정확 +1. 삭제 거절.
- maintenance_runtime_receipts: principal_id/environment/request_key PK, payload_hash/response_json 불변.
- maintenance_admission_guard: code TEXT CHECK/trigger. 'OK' insert는 RAISE(IGNORE)로 행을 남기지 않고, 'BLOCKED'는 maintenance_new_request_blocked, 나머지는 maintenance_policy_unavailable로 ABORT. cleanup 전역DELETE 불필요.
- 단일 head이므로 GAME 대상 active 계획 중복을 거절한다. 다른 active 계획을 덮어쓰지 않는다. 초안 저장은 실효 head를 변경하지 않는다.

신규 `packages/common/src/maintenance-runtime-policy.ts`:
```ts
export type MaintenanceContext = Readonly<{environment:'DEV'|'PRD';applicationId:string}>;
export type MaintenanceState = 'NORMAL'|'SCHEDULED'|'ACTIVE'|'COMPLETED'|'CANCELLED';
export type MaintenanceTransition =
 | {command:'INITIALIZE';expectedRevision:null}
 | {command:'SCHEDULE'|'ACTIVATE';expectedRevision:number;maintenanceId:string;draftRevision:number}
 | {command:'COMPLETE'|'CANCEL';expectedRevision:number};
export type MaintenanceReceipt = {accepted:true;environment:'DEV'|'PRD';applicationId:string;policyRevision:number};
```
Zod strict schemas는 임의 object/unknown field/NaN/불안전시각/빈대상/지원하지 않는 환경을 거절한다. INITIALIZE→NORMAL, NORMAL/COMPLETED/CANCELLED→SCHEDULED 또는 ACTIVE, SCHEDULED→CANCELLED, SCHEDULED/ACTIVE→COMPLETED만 허용. EXTEND는 이 단위에서 지원하지 않는다. 예약 효력은 상태 업데이트 worker 없이 DB시각으로 결정한다.

신규 `apps/admin-api/src/maintenance-runtime-policy-store.ts`:
```ts
class MaintenanceRuntimePolicyStore {
 constructor(db:D1Database, context:MaintenanceContext);
 transition(actor:MaintenancePrincipal, command:MaintenanceTransition,
   reason:string, key:string):Promise<MaintenanceReceipt>;
}
```
OWNER/ACTIVE·expectedRevision·정확 원초안/GAME 또는 GAME_AND_WEB 범위를 같은 batch에서 재검사하고 개정/head/SUCCESS 감사/영수증을 기록한다. SCHEDULE/ACTIVATE의 schedule은 저장 원초안에서만 복사한다. 원요청의 hash에 context/command/reason을 포함한다. 기존 Admin HTTP 명령에 이 메서드를 연결하지 않는다. 같은 원키는 원영수증, 다른 입력409; 실패 후에도 원영수증 우선 복구. 기존 감사 저장 형식은 maintenance-plan-store.ts의 실제 컬럼 계약 재사용.

신규 `apps/game-api/src/maintenance-admission.ts`:
```ts
function maintenanceContext(environment:unknown):MaintenanceContext;
function maintenanceAdmissionStatement(db:D1Database,context:MaintenanceContext):D1PreparedStatement;
function maintenanceAdmissionError(error:unknown):ActivityError|null;
```
applicationId는 서버 전용 명명 상수 LASTRAIL. context는 trusted worker environment에서 만들어 기존 service options로 전달한다. LOCAL은 운영 정책으로 승격하지 않는다: 관련 synthetic fixture를 명시 DEV로 변경하고 환경변환으로 검증이 약해지지 않는지 확인한다. production에서 LOCAL을 DEV로 자동 매핑하지 않는다.

## Task1: 정책 저장·개정 원자성

**Files:** 위 migration/common/store 신규, `tests/integration/maintenance-runtime-policy.test.ts` 신규. 기존 admin-audit 저장 helper를 읽고 동일계약 사용.
**Consumes:** MaintenancePlanStore의 저장 초안/기존 admin principal·audit.
**Produces:** 위 transition 계약, 스키마와 원영수증.

- [ ] 격리 SQL.js fixture에 새 migration을 실행하고 OWNER/VIEWER 및 GAME 초안을 만든다. 기존 tests/helpers/departure-fixture.ts setup과 SQL 접근을 재사용한다. 먼저 테스트를 작성한다.
```ts
const store=new MaintenanceRuntimePolicyStore(f.env.DB,{environment:'DEV',applicationId:'LASTRAIL'});
const receipt=await store.transition({id:'OWNER',role:'OWNER'},
 {command:'INITIALIZE',expectedRevision:null},'TEST_ONLY','init');
expect(receipt.policyRevision).toBe(1);
expect(await store.transition({id:'OWNER',role:'OWNER'},
 {command:'INITIALIZE',expectedRevision:null},'TEST_ONLY','init')).toEqual(receipt);
```
- [ ] run `node node_modules/vitest/vitest.mjs run tests/integration/maintenance-runtime-policy.test.ts --maxWorkers=1`; 신규 계약 미구현으로 실패 확인.
- [ ] strict schema와 위 migration/store를 구현. 기존 원영수증 조회→DB batch의 OWNER/개정/초안재검사→개정/head/audit/receipt 순서. guard가 실패하면 전체 rollback.
- [ ] VIEWER 거절, 권한이 사전 조회 뒤 철회된 경우, head CAS 실패, 이미 활성인 다른 계획, WEB-only, 초기화 중복, 감사쓰기 강제실패, 다른본문 같은키, 개정 UPDATE/DELETE 거절 검사를 추가한다. 감사 실패 후 head/개정/영수증 수가 모두 이전값인 것을 assert한다.
- [ ] 대상 테스트 green, common/Admin 타입 검사. 이 작업 파일만 검토 후 격리 브랜치에 선택 커밋한다.

## Task2: DB 저장 순간 신규 승인 guard

**Files:** `apps/game-api/src/maintenance-admission.ts`, `tests/integration/maintenance-admission.test.ts` 신규, Task1 migration의 guard trigger.
**Consumes:** Task1 heads/revisions, context.
**Produces:** maintenanceAdmissionStatement 및 알려진 오류 매핑.

- [ ] 격리 DB에서 guard와 관찰 가능한 dummy 승인 INSERT를 같은 batch로 실행하는 실패 테스트 작성.
```ts
await expect(f.env.DB.batch([
 maintenanceAdmissionStatement(f.env.DB,{environment:'DEV',applicationId:'LASTRAIL'}),
 f.env.DB.prepare("INSERT INTO maintenance_probe VALUES ('new')")
])).rejects.toThrow('maintenance_policy_unavailable');
expect(f.scalar('SELECT count(*) FROM maintenance_probe')).toBe(0);
```
maintenance_probe는 테스트에서만 `CREATE TABLE maintenance_probe(id TEXT PRIMARY KEY)`로 생성한다.
- [ ] 대상 vitest red 확인 후 SQL 구현. JOIN으로 head가 가리키는 정확개정을 읽는다. 유효 NORMAL/COMPLETED/CANCELLED만 통과, SCHEDULED/ACTIVE는 DB현재시각이 COALESCE(drain_starts_at,starts_at) 미만이어야 통과. 종료예상시각은 predicate에 넣지 않는다.
```sql
INSERT INTO maintenance_admission_guard(code)
SELECT CASE
 WHEN NOT EXISTS(SELECT 1 FROM maintenance_runtime_heads h
   JOIN maintenance_runtime_revisions r USING(environment,application_id,revision)
   WHERE h.environment=? AND h.application_id=?) THEN 'UNAVAILABLE'
 WHEN EXISTS(SELECT 1 FROM maintenance_runtime_heads h
   JOIN maintenance_runtime_revisions r USING(environment,application_id,revision)
   WHERE h.environment=? AND h.application_id=?
   AND (r.state IN ('NORMAL','COMPLETED','CANCELLED') OR
    (r.state IN ('SCHEDULED','ACTIVE') AND
     CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)<COALESCE(r.drain_starts_at,r.starts_at))))
 THEN 'OK' ELSE 'BLOCKED' END
```
DB CHECK/FK와 strict 검증으로 상태·시각의 일관성을 보장한다. 손상된 정책을 정상으로 처리하지 않는다.
- [ ] SQL과 같은 함수로 시각을 읽고 동일 statement 안에서 경계를 검증한다. Node Date.now만 바꿔 DB 시간 검사를 대체하지 않는다. 사전 조회 후 NORMAL→ACTIVE 전환, 오래된 head 캐시 미사용, 환경/앱 불일치, 정상일 때 guard 잔행0, 일반 DB 장애의 오분류 방지를 검증한다.
- [ ] 오류 변환은 정확한 guard 식별자 두 개만 503 maintenance_new_request_blocked / maintenance_policy_unavailable로 변환한다. DB 원문 오류는 응답에 노출하지 않는다.시각 데이터 손상은 Task1 제약과 읽기 검증 양쪽에서 거절한다.
- [ ] 테스트 통과, game/common 타입과 변경 lint 확인 후 선택 커밋.

## Task3: START 신규 저장 연결

**Files:** apps/game-api/src/index.ts run_start batch/catch, 신규 tests/integration/maintenance-run-start.test.ts, tests/helpers/departure-fixture.ts.
**Consumes:** Task2의 context/guard/error helper.
**Produces:** 기존 응답 형식의 점검 거절과 원결과 복구.

- [ ] 기존 legacy START 성공 fixture에 TEST_ONLY 정상 정책을 명시 초기화한다. 신규 admission은 combat_not_ready에서 먼저 중단되므로 그것만으로 검사하지 않는다. 점검 상태에서 신규 START를 요청하고 preview ACTIVE 유지, 새 runs/snapshot/멱등 기록/비용 변화0을 assert하는 실패 테스트를 먼저 실행한다.
- [ ] 기존 원결과 조회 위치를 유지하고 신규 저장 batch 앞에 guard를 추가한다.
```ts
await c.env.DB.batch([
 maintenanceAdmissionStatement(c.env.DB,maintenanceContext(c.env.ENVIRONMENT)),
 // 기존 departure guard, preview 소비, run, snapshot, 영수증 statements 유지
]);
```
- [ ] catch에서는 기존 completed replay를 먼저 처리한다. 그 다음 maintenanceAdmissionError(error)가 반환한 오류만 createRunStartRejectedResponse의 기존 형식으로503을 반환한다. 일반 저장 실패는 기존 ops error 처리 유지.
- [ ] 저장 직전 점검 전환, 성공 후 점검 중 같은 키 재송신, 다른 preview/같은 키, 다른 동일 요청 성공 뒤 현재 batch 실패를 각각 검사한다. 비용/preview/snapshot 전체 rollback을 확인한다. 테스트용 DB.batch 래퍼는 대상 INSERT를 식별해 한 번만 개입하고 원 batch에 위임하며 테스트 종료 시 해제한다.
- [ ] 새 테스트와 기존 START/finish/reward/readiness 회귀 통과, 변경 lint·game 타입 확인 후 선택 커밋. 정책 누락 테스트는 정상 초기화가 없는 격리 DB로 검사한다.

## Task4: 광고·구매 신규 승인 연결

**Files:** apps/game-api/src/offline-ad-service.ts, combat-ad-service.ts, ad-start-approval.ts, ad-skip-purchase.ts, pass50-purchase.ts; 신규 tests/integration/maintenance-reward-admission.test.ts.
**Consumes:** Task2의 context/guard/error helper와 각 서비스의 기존 원결과 검증.
**Produces:** 신규 기회·광고 시작·구매 시작의 점검 거절, 기존 거래 복구 유지.

- [ ] 기존 성공 fixture를 재사용해 정상 상태에서 각 경로가 성공함을 먼저 확인한다. 신규7종을 고정한다: OFFLINE/COMBAT 각각 AD_REQUEST/PURCHASE_ONLY, 광고 시작 승인, 광고 제거권 checkout, 패스50 INTENT. 각 경로에 점검 실패 테스트를 작성한다.
- [ ] 신규 저장 batch 맨 앞에 guard를 추가한다. 기존 winner/read-receipt 복구 분기와 SSV·VERIFY·RESTORE·RECONCILE batch에는 추가하지 않는다.
```ts
writes.unshift(maintenanceAdmissionStatement(db,maintenanceContext(o.environment)));
```
위 구문은 pass50에서 action.kind==='INTENT' 분기 내부에만 둔다. 광고 제거권도 checkout 생성 분기만 대상이다. 오류 처리 시 기존 원결과/winner 재조회가 guard 오류 변환보다 먼저다.
- [ ] 점검 거절 시 새 intent/checkout/광고 승인/원결과 영수증/통화 변경0을 검사한다. 점검 전 생성된 기회여도 새 광고 시작 승인은 거절하고 기존 시작 승인 재조회는 mayShow=false를 유지한다.
- [ ] 점검 중 검증된 SSV/스토어 복구는 원래 한 번의 결과를 유지하고 중복 지급하지 않는지 검사한다. 다른 계정·변조 증명·기한 만료·실효된 주문은 기존대로 거절한다. 기한을 자동 연장하지 않는다. checkout 없는 일반 스토어 주문 검증을 새 checkout과 혼동하지 않는다.
- [ ] 각 경로의 저장 직전 정책 전환과 동일키 경쟁 성공 뒤 실패를 재현한다. 실제 SDK/구매/지급은 실행하지 않는다. 대상 green, game 타입·lint 확인 후 선택 커밋.

## Task5: 회귀와 도입 제한 고정

**Files:** 기존 QA README/Server handoff, 새 정책 초기화가 필요한 기존 테스트 fixture. 소유권 밖 UI 수정 없음.
- [ ] 전체 대상 테스트와 common/game/admin 타입·변경 lint 검사 후 전체 suite를 한 번 실행한다. 명시적인 정상 정책이 없는 기존 fixture는 TEST_ONLY 초기화를 추가하며 제품의 fail-closed 처리를 완화하지 않는다.
```powershell
node node_modules/vitest/vitest.mjs run --maxWorkers=1 --testTimeout=15000
node node_modules/typescript/bin/tsc -p packages/common/tsconfig.json --noEmit
node node_modules/typescript/bin/tsc -p apps/game-api/tsconfig.json --noEmit
node node_modules/typescript/bin/tsc -p apps/admin-api/tsconfig.json --noEmit
```
- [ ] 독립 리뷰에는 승인 경계 목록과 실제 차분만 전달한다. 중요 지적 수정 후 영향 검사를 다시 실행한다. Admin ACTIVATE503/UNCONNECTED 유지 확인.
- [ ] QA에 결과·source SHA·migration SHA를 기록한다. 관련 파일만 선택 커밋한다. DB0047인 DEV에 새 코드를 배포하지 않는다.
- [ ] 운영 도입 순서는 별도 작업으로 남긴다: 전체 writer 호환 확인→승인된 중단→백업→새 schema와 명시 정상 정책 초기화→호환 코드→검증→재개. 이번 로컬 구현을 실제 이관 성공으로 표시하지 않는다.

## 근거와 자체 검토

D1 batch는 내부 SQL 중 하나가 실패하면 전체 sequence를 rollback한다. 공식 근거: https://developers.cloudflare.com/d1/worker-api/d1-database/#batch (2026-09-22 확인). 이 플랫폼 계약을 활용하되 SQL.js 결과를 실제 D1 동시 실행 검증으로 확대하지 않는다.
설계 요구 대응: owner/CAS/audit=Task1, DB시각·정책 누락·환경=Task2, START 원결과=Task3, 광고/PURCHASE_ONLY/실제 시작/구매2종/복구=Task4, 회귀·이관 제한=Task5. Review Focus 5개 항목의 검사를 각 소유 작업에 배정했다. 새로 발견한 패스50 경로는 기존 신규 구매 범위에 포함한다. HTTP 활성화 API/WEB 제어/전체 전투 보존/outbox는 이번 범위 밖이다.
추가 시각 검토: ACTIVATE는 저장 초안 startsAt이 DB현재시각 이하일 때만 허용한다. 미래 초안은 SCHEDULE만 허용한다. Task1 batch 안에서 정확 초안과 시각을 재검사하고 미래 ACTIVATE 거절 테스트를 추가한다. INITIALIZE는 head가 없을 때만 가능하다. 원자성을 잃는 개별 .run 호출로 batch를 대체하지 않는다.

## 실행 방법

추천은 Native: 이 세션에서 순서대로 구현한 뒤 독립 리뷰. 작업들이 같은 저장 계약에 의존하므로 병렬 수정하지 않는다. Subagent-driven을 선택해도 의존 작업 순서와 단일 파일 소유권을 유지한다. 사용자 계획 검토 및 실행 방법 확인 후 시작한다.
## 실행 결과 — 요청28 / 2026-09-22
Native 실행 승인 후 Task1~5의 로컬 구현·검증을 완료했다. 위 체크리스트는 원래 계획이며 아래 완료 기록과 실행 ruling이 우선한다.
- [x] Task1 정책 schema/store 및 OWNER/CAS/audit/원영수증 경합 검사.
- [x] Task2 현재 head·DB시각·손상 정책 검사와 batch rollback guard.
- [x] Task3 HTTP START와 retained departure core 연결, 원결과 복구 유지.
- [x] Task4 광고 기회4종/시작승인/checkout2종 연결 및 기존 거래 복구 검증.
- [x] Task5 전체130files/1125tests PASS, common/game/admin 타입0, 변경 lint0, 독립 리뷰 지적 수정·재검토, QA와 인계 갱신.
선택 커밋 단계는 실행 ruling으로 대체: 기존 더러운 파일의 타 변경을 포함하지 않도록 커밋하지 않고 작업 전 사본 대비22파일 패치와 SHA를 QA/admission-20260922에 보존했다. 원격0048 적용/배포/운영 활성화는 수행하지 않았다. 상세 근거와 남은 운영 범위는 QA/MaintenancePlanServer-20260921/README.md의 요청28 절 및 admission-20260922/progress.md를 따른다.