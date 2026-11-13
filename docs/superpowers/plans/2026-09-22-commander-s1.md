# Commander S1 v02 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 기존5요원 편성을 보존하며 지휘특성3슬롯 저장·활성·원결과·정의지원·출발snapshot 및 조건부레거시 이행을 구현한다.
**Architecture:** 기존XP/BalanceVersion을 원본으로 사용하고 별도 PresetStore가 계정설정과 영수증을 소유한다. DepartureBinding이 실제 신규START batch에 동일 원본의 guard와 snapshot을 연결한다.
**Tech Stack:** 기존 TypeScript/Zod/Hono/D1/SQL.js/Vitest. 신규패키지 없음.
**Spec:** D:/Project/LASTRAIL/docs/superpowers/specs/2026-09-22-commander-s1-design.md — 사용자 요청34에서 문서승인.

## Global Constraints

- 실제Server D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server, branch codex/dev-stable-registry-20260907. 아래코드경로는 이Server 기준이다.
- QA D:/Project/LASTRAIL/QA/CommanderS1-20260922, TEMP/TMP/TMPDIR D:/Project/LASTRAIL/LocalTemp. 앱의stale worktree 사용금지.
- 기존dirty변경과점검후보 d01a2ba5 보존. 시작때변경전사본/SHA기록, 전체stage/reset/clean금지. 커밋은작업만분리되는경우에한하며그렇지않으면QA패치로고정한다.
- migration0049 후보는착수시미사용확인. 기존commander_presets/account_formation/영수증 의미변경0, 시너지필수필드/카탈로그/태그오류0.
- XP원본 player_profiles.commander_xp, 미사용P는슬롯별파생값. 실제XP지급/3P중복가산0, 구run snapshot수정0.
- 작성JSON은운영발행물이아니다. 실제지원handler없는효과는SUPPORTED/조용한0효과로처리하지않는다. 합성지원능력은fixture에만주입한다.
- 기존readiness/점검gate/광고OFF/권한유지. 이번계획은로컬구현·검증이며원격이관/게시/배포/실환불/소비자UI수정없음.

## Review Focus

1. 프로필명변경이XP revision을바꾸지않고, 같은값XP쓰기/overflow를다루는가(Task2).
2. 같은key의서버성공응답유실뒤정의지원철회/XP변경이있어도원영수증을재생하는가(Task2/3).
3. 구BalanceVersion에정의가없을때S1미사용계정만기존출발을유지하며, 저장이력계정은조용히특성을제거하지않는가(Task4).
4. 미선택/빈슬롯을quote한뒤선택행이생기는경합도부재CAS로막는가(Task4).
5. 레거시태그만있는자료·다른정의하위의존성·실제adapter부재를3P환불로오인하지않는가(Task5).

## Task1: 정의·랭크·진행 검증

**Create:** packages/common/src/commander-traits.ts, apps/game-api/src/commander-definition.ts, tests/unit/commander-rules.test.ts, tests/fixtures/commander-s1-test-only.json.
**Inputs:** 승인JSON의nodes/rankRows/levels/confirmed/proposed, 기존balance_config_versions/active.
**Outputs:** 아래 명시타입과검증인터페이스. type정의와schema는같은모듈에서공급한다.

```ts
export type Ranks=Readonly<Record<string,number>>;
export type CommanderProgression={xp:number;level:number;earnedPoints:number;revision:number};
export type CommanderNode={id:string;parents:readonly string[];maxRank:number;kind:string};
export type CommanderRank={nodeId:string;rank:number;cost:number;totalNodeCost:number;minimumEarnedPoints:number;minimumLevel:number};
export type CommanderLevel={level:number;totalPoints:number;cumulativeXp:number;capstoneLimit:number};
export type CommanderDefinition={rulesVersion:string;traitDefinitionVersion:string;sourceHash:string;nodes:readonly CommanderNode[];rankRows:readonly CommanderRank[];levels:readonly CommanderLevel[];handlers:Readonly<Record<string,string>>};
export type CommanderSupport=Readonly<Record<string,string>>;
export type CommanderPin={balanceVersion:number;configJson:string;definition:CommanderDefinition};
// parseCommanderDefinition(raw:unknown):CommanderDefinition
// commanderProgression(xp:number,revision:number,definition:CommanderDefinition):CommanderProgression
// validateCommanderRanks(input:unknown,p:CommanderProgression,d:CommanderDefinition,s:CommanderSupport):{ranks:Ranks;usedPoints:number;unusedPoints:number}
// readCommanderDefinition(db:D1Database):Promise<CommanderPin|null>
```

- [x] 정상43노드/106랭크/50레벨의 명시 fixture를 준비하고 미구현 module import 실패를 기록한다. 최신 승인 JSON을 검증용으로 투영하며 지원 handler는 TEST_ONLY로 명시한다. 원문 수치와 ID는 변경하지 않는다.
- [x] 아래대표검사와cycle/누락부모/중복rank/비용불일치/숫자상한/레벨경계/3P상위한도검사를추가한다.
```ts
expect(validateCommanderRanks({TR01:0},progression,definition,support).ranks).toEqual({});
expect(()=>validateCommanderRanks({unknown:1},progression,definition,support)).toThrow('contract_violation');
expect(()=>validateCommanderRanks({OC01:1},progression,definition,support)).toThrow('commander_parent_required');
expect(()=>validateCommanderRanks(validOc01Ranks,progression,definition,{})).toThrow('commander_definition_unsupported');
```
여기서 progression은Lv25/25P, validOc01Ranks는승인그래프의실제부모경로를함께투자한fixture다. expected 오류우선순위는형식→레벨/랭크→부모→상위수→예산→지원으로고정하고테스트의다른조건은합법으로만든다.
- [x] 계정XP로정의의누적XP임계값이하최대level을선택한다. 초과XP는원값유지, P는totalPoints. rank당누적비용은선택최종rank행에서한번계산한다. 직접부모중하나실제rank>0;capstone종류개수를level한도와비교한다.
- [x] 정의reader는balance JSON의commanderTraits 부재만null,잘못된JSON/정의/DB실패는503. sourceHash와고정version을검사하고비교에쓸configJson을반환한다. 제품support는실제검증된소비능력만명시하며모든노드를지원하는테스트목록을기본값으로쓰지않는다.
- [x] `node node_modules/vitest/vitest.mjs run tests/unit/commander-rules.test.ts --maxWorkers=1` 및common/game타입·변경lint. QA단위결과와선택차분기록.

## Task2: XP revision·3슬롯 저장/활성·영수증

**Create:** packages/db/migrations/0049_commander_traits.sql, apps/game-api/src/commander-preset-store.ts, tests/helpers/commander-db-fixture.ts, tests/integration/commander-presets.test.ts.
**Consume:** Task1타입/validator/reader, 기존SqlJsD1Database/account_mutations.
**Produce:** CommanderPresetStore(db,support), read(accountId), save(accountId,command,key), activate(accountId,command,key), readResult(accountId,key), readCommanderRunApplied(db,accountId). 원run 조회는 저장 snapshot만 읽고 NONE/LEGACY_UNRECORDED/UNAVAILABLE/APPLIED를 구분하며 현재 설정으로 합성하지 않는다. 명령타입은아래대로commander-traits.ts에추가한다.
```ts
export type CommanderSave={slotId:1|2|3;expectedPresetRevision:number;expectedProgressionRevision:number;rulesVersion:string;traitDefinitionVersion:string;ranks:Ranks};
export type CommanderActivate=Omit<CommanderSave,'ranks'> & {expectedSelectionRevision:number};
export type CommanderReceipt={accepted:true;contractVersion:'commander-s1-v2';command:'SAVE'|'ACTIVATE';slotId:1|2|3;presetRevision:number;selectionRevision:number;receiptId:string};
```
- [x] DBfixture에accounts2개와명시프로필XP/정의를준비한다. 새schema미존재실패→migration구현. 모든계정fixture자동high-level/지원정책seed금지.
```sql
ALTER TABLE player_profiles ADD COLUMN commander_progression_revision INTEGER NOT NULL DEFAULT 1
 CHECK(typeof(commander_progression_revision)='integer' AND commander_progression_revision BETWEEN 1 AND 9007199254740991);
CREATE TRIGGER commander_xp_revision AFTER UPDATE OF commander_xp ON player_profiles
WHEN NEW.commander_xp<>OLD.commander_xp
BEGIN UPDATE player_profiles SET commander_progression_revision=OLD.commander_progression_revision+1 WHERE account_id=NEW.account_id; END;
```
추가CHECK/trigger로XP의안전정수·음수·revisionoverflow와클라이언트revision변조를거절하며trigger재귀동작을SQL.js/D1호환범위로검사한다. 기존XPwriter가revision을제출하도록요구하지않는다. 나머지preset/selection/receipt/migration tables는spec의PK/FK/불변조건을정확히구현한다.
- [x] `save`의입력정규화/command종류/expected revisions/version으로canonicalhash를만들고원영수증을먼저읽는다. 현재상태조회부터batch까지한catch안에두어preflight경합도원영수증재조회로회복한다.
```ts
const prior=await replay(); if(prior)return prior;
try { const state=await readAuthority(); const writes=prepareAtomicSave(state); await db.batch(writes); return receipt; }
catch(error){const committed=await replay(); if(committed)return committed; throw mapStorageError(error);}
```
readAuthority/prepareAtomicSave/mapStorageError는store내책임별private메서드로구현한다. 동일accountXP/revision·현재BalanceVersion/configJSON·preset정확원본/부재를guard하고다른계정행변경0. 새키no-op도동일guard검사후영수증1개,해당revision증가0.
- [x] 슬롯1/2각25P저장이동시에합법,총50P차감0;A활성중B저장A유지;A저장후다음조회최신A;진행run존재해도S1저장가능;편성행/Snapshot불변검사. 현재조회는3슬롯의빈표시값을합성할수있지만GETDB쓰기0.
- [x] XP변경/name변경/같은XP/최대revision;같은key같은본문/다른본문/다른계정;같은revision동시저장;저장vs활성;감사/영수증실패전부rollback;정의철회후원결과재생을검사한다.
- [x] `node node_modules/vitest/vitest.mjs run tests/integration/commander-presets.test.ts --maxWorkers=1` 통과,DBguard잔행0/FK0/타입·lint확인후QA에단위완료기록.

## Task3: 실제 조회·저장·활성·원결과 API

**Create:** apps/game-api/src/commander-routes.ts, tests/integration/commander-routes.test.ts.
**Modify:** apps/game-api/src/index.ts (registerFormationRoutes 인근에등록만추가).
**Consume:** CommanderPresetStore와기존resolveAuthContext/readBoundedBody/ActivityError.
**Produce:** spec의4종API 및contractVersion commander-s1-v2. 현재run 표시는 Task2의 원snapshot 읽기를 사용한다.
```ts
registerCommanderRoutes(app,resolveAuthContext,commanderSupport);
// app.get('/v1/commander/traits',...)
// app.post('/v1/commander/traits/presets/:slot/save',...)
// app.post('/v1/commander/traits/presets/:slot/activate',...)
// app.get('/v1/commander/traits/commands/:key',...)
```
- [x] 실제 Hono 검사에서 미등록404를 확인하고 인증·strict 입력·소유권 검사를 먼저 추가한다. 원키 printable ASCII1..128, URL slot1..3, query/추가필드/accountId/preferredSynergyTagId 거절. 기존18KB body 상한과400 계약을 재사용한다.
- [x] 사용자identity는resolveAuthContext결과만사용한다. raw본문이나헤더의accountId를참조하지않는다. store결과와알려진ActivityError만외부노출,DB원문503일반오류로변환. no-store를인증실패포함모든응답에확인한다.
- [x] 실제해당계정원결과GET은현재정의지원과무관하게원영수증읽기만수행. 404를실패/진행중/정상빈값으로꾸미지않는다. 동일키다른본문충돌은쓰기재시도에서검사한다.
- [x] 지원정의없는GET은지원불가를명시하고슬롯/진행run조회실패와구분한다. currentRunApplied의NONE/LEGACY_UNRECORDED/UNAVAILABLE/APPLIED를spec대로표시하며현재계정rank로합성0.
```ts
expect((await save({ ...validCommand, preferredSynergyTagId:'old' })).status).toBe(400);
expect((await save(validCommand,'same')).status).toBe(200);
expect(await (await readResult('same')).json()).toEqual(originalReceipt);
expect((await readResultAsOtherAccount('same')).status).toBe(404);
```
validCommand/인증계정은Task2fixture에strict명령을구성하는테스트wrapper다.
- [x] API테스트와기존formation/departure통과,game타입/변경lint,wire요청/응답샘플은TEST_ONLY로그로보관한다.

## Task4: 출발 quote·원자 START·진행 snapshot

**Create:** apps/game-api/src/commander-departure.ts, tests/integration/commander-departure.test.ts.
**Modify:** departure.ts의state/admission/guards/버전, index.ts legacy신규START batch/snapshot, ce03-local-host.ts의실제서버입장조합,필요한해당test fixture.
**Consume:** 정의/XP/preset/selection소유store.
**Produce:** readCommanderDeparture(db,accountId,support), commanderDepartureGuards(db,accountId,state). 원run 읽기는 Task2 구현을 재사용한다. state에는snapshot과정확DB원본/부재를분리보관한다. snapshot타입은level/P/revisions/ranks/정의및balance/hash를spec대로명시한다.
- [x] 성공 quote 후 preset/selection/XP/BalanceVersion을 변경하는 실패 검사를 먼저 작성한다. 미선택 quote 후 selection 신규 생성도 포함한다.
```ts
await expect(startAfterSelectionRace()).rejects.toThrow('departure_quote_changed');
expect(await fuelAfter()).toBe(fuelBefore);expect(await snapshotCount()).toBe(0);
expect(await replayAfterPresetSave()).toEqual(originalRunResponse);
```
- [x] state 읽기에서 S1 정의 없음/설정 이력 없음인 경우만 legacy null을 반환한다. 이력 존재·정의 누락은 명시503. schema 미적용과 definition 부재를 구분하고 미설정 계정 fixture에 임의 preset/지원 정의를 seed하지 않는다.
- [x] admission version 갱신은 신규 quote에 한정하고 기존 확정 START의 원결과 검색이 먼저다. 구 미확정 quote는 재준비를 요구한다. legacy HTTP 경로에도 동일한 S1 검증을 적용하고 combat_not_ready는 해제하지 않는다.
```ts
await db.batch([
 ...commanderDepartureGuards(db,accountId,commanderState),
 // existing maintenance/fuel/ownership/preview/run/snapshot/receipt statements
]);
```
위 주석 위치의 기존 SQL은 삭제하거나 별도 transaction으로 옮기지 않는다. snapshot에 commanderTraits를 추가하고 복원에 필요한 version/hash/ranks를 고정한다.
- [x] 실제 타입과 caller를 따라 callback 생산자가 snapshot 추가 데이터를 보존하는지 확인한다. 추가 인수는 trusted support만 전달하며 미확인 지원을 전부 허용하지 않는다. 미지원 특성을 자동 삭제하고 출발하는 처리는 금지한다.
- [x] snapshot 저장 실패 전부 rollback, 원 START 재생, 구run JSON 불변, 다른 계정run 접근 불가, currentRunApplied 상태 구분을 검사한다. 신규 검사와 기존 departure/legacy START/readiness/maintenance 검사 및 game 타입/lint를 완료한다.

## Task5: 확인된 구정의의 OC01 재편성

**Create:** apps/game-api/src/commander-legacy-reconciliation.ts, tests/integration/commander-legacy-reconciliation.test.ts.
**Consume:** Task2의 원자적 store/migration receipt, 신뢰된 legacy adapter와 원정의.
**Produce:** 내부 reconcile(accountId,slotId,sourceVersion,targetVersion). 공개 환불 endpoint는 만들지 않는다.
- [x] TEST_ONLY 구정의 fixture는 rank1/태그만/투자0/자식OR/진행run을 구분한다. 실제 adapter는 미확인이므로 제품에서 임의 JSON을 구정의로 받아들이지 않는다.
```ts
expect(after.unusedPoints-before.unusedPoints).toBe(3);
expect(after.ranks.OC01??0).toBe(0);
expect(after.earnedPoints).toBe(before.earnedPoints);
expect(await reconcileAgain()).toEqual(firstReceipt);
expect(await originalRunSnapshot()).toBe(beforeRunSnapshot);
```
- [x] 순수 정규화에서 원정의의 실제랭크/비용/자식 그래프를 검사한다. 실제 OC01 rank1이면 투자3P를 해제하고 태그만이면0P다. 자식이 있는 구정의는 해당 OR/연쇄규칙을 사용하며 알 수 없는 규칙은 오류다.
- [x] 확인된 원행 revision/version/content와 대상행을 같은 batch로 guard하고 ranks/정의/revision/receipt/migration marker를 함께 저장한다. 경합 Save를 덮지 않고 원결과를 재조회한다. activeSelection/XP/run은 변경하지 않는다.
- [x] 실제 adapter 없음은 NOT_APPLICABLE, 알 수 없는 데이터는 UNSUPPORTED로 구분한다. 실제0건이나 환불 성공을 꾸미지 않는다. TEST_ONLY 구version을 운영 registry에 등록하지 않는다.
- [x] 대상 검사·타입/lint·이행 재실행의 중복환불0을 기록한다. 실제 운영 데이터 확인과 필요한 adapter 연결은 남은 의존성으로 명시한다.

## Task6: 최종 회귀·계약 인계

- [x] 작업 전 차분과 spec 전항목을 대조하고 정의 지원과 실제 효과 실행을 구분한다. OC01의 장비 A1B1C1/제외피해/장비 갱신은 소비자 연결 의존성이며 저장검사PASS를 실전PASS로 표시하지 않는다.
- [x] 전체 대상 검사 후 전체 suite를 한 번 실행한다.0049 추가로 고정 migration 개수 fixture가 실패하면 실제 실행경로에 필요한 fixture만 수정하고 원인을 기록한다.
```powershell
node node_modules/vitest/vitest.mjs run --maxWorkers=1 --testTimeout=15000
node node_modules/typescript/bin/tsc -p packages/common/tsconfig.json --noEmit
node node_modules/typescript/bin/tsc -p apps/game-api/tsconfig.json --noEmit
node node_modules/typescript/bin/tsc -p apps/admin-api/tsconfig.json --noEmit
node node_modules/typescript/bin/tsc -p apps/async-worker/tsconfig.json --noEmit
```
- [x] 독립 review에는 실제 변경만 전달한다. 중요 지적 수정 후 영향검사 재실행. 변경파일 전체 lint, migration/정의원본 SHA와 원결과 샘플을 QA에 저장한다.
- [x] 기존 Server 인계/WAIT-011에 실제 API/schema/support/미연결 effect/legacy 확인 범위와 검증 근거를 기록한다.5요원 계약과 Direction/Unity 파일은 변경하지 않는다. 운영 발행·실기 QA 완료로 확대하지 않는다.

## 실행 방법·자체 검토

추천 Native: 이 세션에서 Task1→6을 순서대로 구현한 뒤 독립 review를 수행한다. 단계별 타입과 저장 경계를 다음 작업이 사용하므로 병렬 쓰기를 피한다. Subagent-driven은 작업마다 구현자와 검토자를 분리하는 선택지이며 의존 순서는 동일하다. 모델 설정은 변경하지 않는다.

Task2의 store가 원run 읽기와 상태 구분도 제공하고 Task3 API와 Task4 출발 연결이 이를 재사용한다. 테스트 전용 capability를 제품에 넣지 않는다. 실제 운영 데이터/소비 handler의 미확인을 이미 승인된 게임 규칙이나 저장 계약의 미정으로 표시하지 않는다.

## 실행 결과 — 2026-09-22 요청35
사용자 Native 승인으로 Task1–6 연속 로컬 구현·검증 완료. 실제 변경22파일(신규14/수정8), 전체136files/1167tests PASS(S1 신규33), 타입4/lint/diff-check PASS. 근거 QA/CommanderS1-20260922/implementation/README.md, source-pins.json, commander-s1-task.patch, contract-samples.TEST_ONLY.json. task1 정의 fixture는 JSON으로 투영했고 책임별 공통 조회/guard 함수와 PresetStore를 사용했다. 테스트 예시의 구체 구현은 해당 검사파일과 QA 로그를 따른다.
독립 리뷰 P2 세 건 수정 및 실패→성공 검사 확보. 최종 소규모 추가 검토는 모델 용량 오류로 실행되지 않아 별도 승인으로 주장하지 않는다. 실제 지원handler/실운영legacy adapter/정의게시/0049 원격이관/배포/OC01 실기 효과 검증은 이번 로컬 구현 완료와 구분하며 WAIT-011에 남긴다. 기존5요원 편성·타담당 파일·점검릴리스 후보 d01a2ba5 보존.
