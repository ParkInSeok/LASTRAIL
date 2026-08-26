# LAST RAIL Server / Backend / Cloudflare 전체 구축 Master 작업지시

이 저장소에는 LAST RAIL 서버 작업이 아직 거의 또는 전혀 구현되어 있지 않다고 가정한다.

너는 이 지시문을 **LAST RAIL Server Source of Truth**로 사용하여 서버 프로젝트를 처음부터 구축한다.

기존 파일이 일부 존재한다면 먼저 전체를 읽고 현재 상태를 파악한 뒤, 아래 Source of Truth와 충돌하는 오래된 구조는 제거 또는 마이그레이션한다.

임의로 게임 기획을 변경하지 말고, 요구사항이 이미 명시된 영역은 다시 질문하지 말고 그대로 구현한다.

작업은 단계별로 진행하고 각 Phase 완료 시:

* 변경 파일
* 구현 내용
* 실행한 명령
* 테스트 결과
* 남은 문제
* 다음 Phase

를 짧게 보고한 뒤 다음 Phase를 진행한다.

단, **PRD에 실제 배포하거나 Production 데이터를 변경하는 행위는 내가 명시적으로 승인하기 전까지 금지한다.**

Cloudflare 로그인이나 외부 계정 승인처럼 사용자 개입이 반드시 필요한 경우에만 중단하고 정확히 무엇을 해야 하는지 알려준다.

---

# 0. 프로젝트 기본 정보

프로젝트:

**LAST RAIL**

플랫폼:

**Unity Android**

서버 Runtime:

**Cloudflare Workers**

언어:

**TypeScript**

Router:

**Hono**

DB:

**Cloudflare D1**

Package Manager:

**pnpm**

Config:

**wrangler.jsonc**

Async:

**Cloudflare Queues**

장기 Object Storage:

**R2**

Analytics:

**Workers Analytics Engine**

ID:

**ULID TEXT**

Timestamp:

**UTC Unix Milliseconds INTEGER**

영구 Economy:

**INTEGER only**

모든 서버 Domain Integer는 JavaScript에서도:

```text
Number.isSafeInteger
```

검증한다.

---

# 1. 핵심 서버 책임

전투 Frame Simulation은 서버에서 실행하지 않는다.

```text
Unity Client
= Combat Local Simulation

Server
= Permanent Data / Run 생성 / WavePlan / Reward / Validation
```

Server가 매 Frame 전투를 재시뮬레이션하지 않는다.

대신:

* Run Snapshot
* Balance Config Version
* Server WavePlan
* Augment Plan
* Augment History
* Tactical Ledger
* Action Ceiling
* DPS Envelope
* Deterministic Rule Validation

을 통해 결과 가능성을 검증한다.

---

# 2. Repository 구조

Monorepo:

```text
lastrail-server/

apps/
├─ game-api
├─ admin-api
└─ async-worker

packages/
├─ domain
├─ db
├─ economy
├─ progression
├─ operation
├─ run
├─ integrity
├─ balance
├─ analytics
├─ contracts
└─ common

tests/
├─ fixtures
└─ integration

scripts/
├─ migration
├─ seed
├─ validation
└─ maintenance

docs/
├─ architecture
├─ api
├─ cloudflare
└─ run-protocol
```

각 Worker는 별도:

```text
wrangler.jsonc
```

를 가진다.

`packages/*`가 `apps/*`를 import하면 안 된다.

의존성은:

```text
apps
↓
domain services / adapters
↓
domain
```

방향으로 유지한다.

Route에서 SQL 직접 작성 금지.

Route에서 Reward/Cost 직접 계산 금지.

---

# 3. 환경 분리

반드시:

```text
LOCAL
DEV
PRD
```

을 완전히 분리한다.

Workers:

```text
lastrail-game-api-dev
lastrail-game-api-prd

lastrail-admin-api-dev
lastrail-admin-api-prd

lastrail-async-worker-dev
lastrail-async-worker-prd
```

D1:

```text
lastrail-core-dev
lastrail-core-prd
```

DEV/PRD가 D1/Queue/R2/Secret을 공유하면 안 된다.

---

# 4. 공식 Domain

공식 Domain:

```text
lastrail.app
```

환경별:

```text
DEV
dev-api.lastrail.app
dev-admin.lastrail.app

PRD
api.lastrail.app
admin.lastrail.app
```

`lastrail.app` 루트는 향후:

* 공식 홈페이지
* Privacy Policy
* Terms
* Support

용도로 사용한다.

async-worker는 외부 공개 Domain을 만들지 않는다.

Balance 전용 별도 서브도메인은 만들지 않는다.

Game API:

```text
GET /v1/balance-config/:version
```

으로 제공한다.

---

# 5. Admin 보안

`admin.lastrail.app` 계열은 공개 Admin 페이지로 두지 않는다.

반드시:

```text
Cloudflare Access
↓
MFA
↓
Access JWT
↓
admin-api JWT 검증
↓
admin_principals
↓
OWNER / OPERATOR / VIEWER
```

구조를 사용한다.

Cloudflare Access 성공만으로 LAST RAIL Admin 권한을 부여하지 않는다.

JWT `sub`를:

```text
admin_principals.access_subject
```

와 매핑한다.

이메일만으로 권한을 부여하지 않는다.

직접 ID/Password 로그인 시스템을 새로 구현하지 않는다.

Cloudflare Access가 Authentication을 담당하고:

```text
admin_principals
```

가 Authorization을 담당한다.

Production Admin:

```text
MFA Required
```

정책.

최초 OWNER는 bootstrap script를 통해 명시적으로 등록한다.

마지막 ACTIVE OWNER를 제거하거나 Role을 내리는 요청은 막는다.

---

# 6. Admin Role

```text
OWNER
OPERATOR
VIEWER
```

Permission Matrix는 코드에서 관리한다.

최소 Permission:

```text
ACCOUNT_READ

ECONOMY_READ
ECONOMY_CORRECT

RUN_READ

INTEGRITY_READ
INTEGRITY_REVIEW
INTEGRITY_RESOLVE

ENFORCEMENT_READ
ENFORCEMENT_CREATE
ENFORCEMENT_LIFT
PERMANENT_BAN

COMPENSATION_READ
COMPENSATION_CREATE
COMPENSATION_PREPARE
COMPENSATION_EXECUTE

BALANCE_READ
BALANCE_DRAFT_CREATE
BALANCE_DRAFT_EDIT
BALANCE_DRAFT_DISCARD
BALANCE_VALIDATE
BALANCE_PUBLISH
BALANCE_ROLLBACK

VALIDATOR_READ
VALIDATOR_CONTROL

CLIENT_BUILD_READ
CLIENT_BUILD_CONTROL

ADMIN_PRINCIPAL_READ
ADMIN_PRINCIPAL_MANAGE

ADMIN_JOB_READ
ADMIN_JOB_EXECUTE

AUDIT_READ
```

Production 기준:

```text
BALANCE_PUBLISH
BALANCE_ROLLBACK
PERMANENT_BAN
COMPENSATION_EXECUTE
```

는 OWNER 전용으로 구현한다.

---

# 7. 위험 Admin 작업

고위험 작업은:

```text
Preview
↓
Confirmation Token
↓
Execute
```

절차를 사용한다.

대상:

* Balance Publish
* Balance Rollback
* Permanent Ban
* Economy Correction
* Compensation Execute
* Validator Runtime Control
* Client Build Block

Confirmation Token 원문은 DB에 저장하지 않는다.

Hash만 저장한다.

Confirmation:

* Principal 귀속
* Permission 귀속
* Payload Hash 귀속
* 짧은 TTL
* 1회용

Execute 순간 최신 Permission을 다시 확인한다.

Confirmation Consume과 실제 Domain Write는 반드시 같은 Transaction에 들어간다.

---

# 8. 모든 Admin Write Audit

Append-only:

```text
admin_audit_logs
```

사용.

기록:

* Principal
* Role
* Permission
* Action
* Resource
* Request ID
* Confirmation ID
* Payload Hash
* 변경 요약
* Account Mutation
* 결과
* Error Code
* Timestamp

금지:

* Access JWT 원문
* Confirmation Token
* Refresh Token
* Signing Secret
* Cloudflare Token
* Google Secret
* Ad Provider Secret

Audit 수정/삭제 금지.

---

# 9. Versioned Balance Config

Unity ScriptableObject는 수치형 Balance Source of Truth가 아니다.

```text
Unity ScriptableObject
= ID / Prefab / Behavior / Animation / FX / Audio / Sprite / Fallback

Server Balance Config
= Numeric Gameplay Balance Source of Truth
```

Server 구조:

```text
balance_config_drafts
balance_config_publish_claims
balance_config_versions
balance_config_activations
balance_config_active
```

Published Version은 immutable.

Admin 수정은 Draft에서만 한다.

Published Version 직접 Edit/Delete 기능 금지.

---

# 10. Balance Draft → Publish

Draft:

```text
baseVersion
schemaVersion
requiredContentVersion
config
checksum
revision
```

Draft 수정마다 revision 증가.

Publish는:

```text
draftId
expectedRevision
expectedChecksum
```

를 검증한다.

또한 Beta에서는:

```text
draft.baseVersion
==
현재 activeVersion
```

이어야 Publish 가능하게 한다.

아니면:

```text
BALANCE_DRAFT_STALE_BASE
```

실패.

Publish 성공 Transaction:

```text
새 balance_config_version INSERT

publish_claim INSERT

Draft → PUBLISHED

activation history INSERT
PUBLISH

balance_config_active 변경

Admin Confirmation consume

Admin Audit
```

전부 원자적으로 처리한다.

Publish 성공:

```text
Publish = Activate
```

이다.

---

# 11. Balance Rollback

Rollback은 Version을 수정하지 않는다.

예:

```text
105
106
107 ACTIVE
```

에서:

```text
107 → 106
```

으로 Active Pointer만 변경한다.

107은 그대로 영구 보존한다.

Rollback도:

```text
Preview
→ Confirmation
→ Execute
```

고위험 작업이다.

Activation History:

```text
PUBLISH
ROLLBACK
```

전부 append-only 기록.

---

# 12. Balance Config 필수 Metadata

Published Version:

```text
version
schemaVersion
requiredContentVersion
checksum
configJson
publishedAt
publishedBy
```

Checksum:

```text
Canonical JSON
→ SHA-256
```

대상 Envelope:

```text
schemaVersion
requiredContentVersion
config
```

과거 Published Balance Version은 영구 보존한다.

---

# 13. Client Build / Content Version

```text
client_builds
```

관리.

필드:

```text
platform
versionName
buildNumber
contentVersion
protocolVersion

ALLOWED
UPDATE_REQUIRED
BLOCKED
```

Balance:

```text
requiredContentVersion
```

과 Client:

```text
contentVersion
```

비교.

조건:

```text
client.contentVersion
>=
balance.requiredContentVersion
```

신규 Preview/Run에서 강제.

---

# 14. Client Balance Fetch

Boot/Lobby:

```text
GET /v1/bootstrap
```

에서:

```text
activeBalanceVersion
requiredContentVersion
schemaVersion
checksum
```

전달.

Local Cache Version이 다르면:

```text
GET /v1/balance-config/:version
```

Fetch.

Client는 checksum 검증 후 Cache 활성화.

Push를 사용해도:

```text
BALANCE_VERSION_CHANGED
```

알림만 전달한다.

실제 Balance Payload는 Push로 전달하지 않는다.

Push 유실 여부와 관계없이 Bootstrap/Run Start 전 Version을 확인한다.

---

# 15. Account Write Serialization

D1의 pre-read Race Condition을 막기 위해:

```text
account_mutations
```

사용.

계정마다:

```text
sequence
```

단조 증가.

Critical Write는 최신 sequence=N을 읽고:

```text
N+1
```

INSERT를 Transaction 첫 statement로 실행한다.

두 요청이 동시에 동일한 N+1을 사용하면 한쪽은 UNIQUE 충돌로 전체 rollback.

실패한 요청은:

```text
State Reload
→ Validation
→ Cost 재계산
→ 새로운 Mutation으로 Retry
```

한다.

Mutation 번호만 바꿔서 Blind Retry 금지.

적용:

* Upgrade
* Training Core
* Preset
* Unlock
* Preview 생성
* Run Start
* Checkpoint
* Finish
* Reward
* Integrity 상태
* Enforcement
* Compensation Grant
* Ad Reward Grant
* Admin Player Correction

---

# 16. Authentication

Beta:

**Guest Account 지원**

향후 Google Identity 연결 가능.

구조:

```text
accounts
auth_identities
auth_sessions
```

Refresh Token 원문 저장 금지.

저장:

```text
refresh_token_hash
family_id
generation
```

Refresh Token Rotation + Reuse Detection 지원.

Access Token:

* Signed
* Short TTL

Guest → Google 연결 시 기존 `account_id` 유지.

---

# 17. Bootstrap

```text
GET /v1/bootstrap
```

한 번으로:

* Account
* Profile
* Currency/Resources
* Fuel
* Training Core
* Operators
* Train
* Carriages
* Turrets
* Commander Preset
* Unlock
* Tutorial
* Active Run
* Active Preview
* Compensation summary
* Active Balance Version
* Build compatibility

동기화.

Client Permanent Local Data는 Cache일 뿐 서버가 정답이다.

---

# 18. Permanent Economy Resource

Canonical:

```text
CREDITS
MACHINE_PARTS
TACTICAL_DATA
SPECIAL_CORE
OPERATOR_EXP
```

`account_currencies`와 `currency_ledger`에 포함한다.

모든 Balance:

```text
INTEGER
>= 0
```

현재 Balance:

```text
account_currencies
```

역사:

```text
economy_transactions
currency_ledger
```

원칙:

```text
Balance
=
SUM(Ledger Delta)
```

초기 지급도 Balance를 직접 올리지 않고 Economy Transaction으로 처리한다.

Committed Ledger 수정/삭제 금지.

복구:

```text
Correction / Reversal Transaction
```

---

# 19. Idempotency

중요 Write:

* Run Start
* Run Finish
* Upgrade
* Training Core Change
* Reward
* Ad Claim
* Compensation
* 향후 Purchase/Gacha

Idempotency 필수.

`idempotency_records`는 성공한 committed operation만 저장.

Error 결과를 영구 Idempotency 결과로 저장하지 않는다.

Idempotency 하나만 믿지 않고 Domain UNIQUE도 반드시 둔다.

---

# 20. Fuel

Fuel은 일반 Currency와 분리.

```text
fuel_state
fuel_ledger
```

Lazy Regeneration.

Cron으로 전 유저 Fuel Write 금지.

```text
last_regen_at
+
Server UTC
```

로 조회/소비 시 계산한다.

GET `/fuel` 때문에 Regen Write 금지.

Run Start 같은 실제 Write 순간에 Materialize.

Overflow 허용.

정상 Max 이상이면 자연 Regen만 정지.

---

# 21. Operator Permanent Progress — Training Core

기존:

```text
모든 Operator가 개별 Permanent Level
Credits + Tactical Data Level Cost
```

구조는 폐기한다.

최종:

**Training Core 5 Slot + Sync Level**

---

# 22. Training Core

계정:

```text
operator_training_core_slots
```

정확히 Slot Index:

```text
0
1
2
3
4
```

최대 5개.

Slot:

```text
slotIndex
operatorId
trainedLevel
investedOperatorExp
revision
lastMutationSequence
```

동일 Operator 중복 금지.

계정이 실제 보유한 Operator만 등록 가능.

Training Core 교체는 **Open Run이 없을 때만** 가능.

Open Run:

```text
ACTIVE
FINISH_PENDING
QUARANTINED
```

---

# 23. Operator Level 저장

`account_operators`에서는 개별 Permanent `level`을 제거한다.

보존:

```text
operatorId
skillTraining
promotion
ownership metadata
revision
lastMutation
```

Training Core 밖 Operator에 Sync Level을 복제 저장하지 않는다.

---

# 24. Sync Level

5 Slot이 전부 채워진 경우:

```text
SyncLevel =
MIN(Training Core 5 trainedLevel)
```

Core Operator:

```text
EffectiveLevel = trainedLevel
```

Non-Core Operator:

```text
EffectiveLevel = SyncLevel
```

Sync 대상:

* Level 기반 HP
* Level 기반 ATK
* Heal Power
* Level 기반 Base Stat

Sync 제외:

* Skill Training
* Promotion
* Unlock
* Run Tactical Level
* Augment
* Skin

---

# 25. Training Core 초기 등록

Tutorial 완료 후:

```text
Kyle
Reina
Eve
Sera
Mira
```

를 5 Slot에 자동 등록.

각:

```text
trainedLevel = 1
investedOperatorExp = 0
```

Sync 기능은 5 Slot 전부 채워진 경우 활성화.

---

# 26. Operator EXP

Permanent Level:

```text
Operator EXP
```

만 사용.

Credits/Tactical Data Level Cost 폐기.

최종 성장 재화 역할:

```text
Permanent Operator Level
→ OPERATOR_EXP

Skill Training
→ Credits + Tactical Data

Promotion
→ Credits + Tactical Data + Special Core

Run Lv1~5
→ Tactical Supply

Commander Level
→ Commander XP
```

---

# 27. Operator EXP Reward

Versioned Balance Config:

```text
operatorExpByRisk

Risk I   700
Risk II  900
Risk III 1100
Risk IV  1300
Risk V   1500

tutorialOperatorExp = 500
```

Operation 종류는 Operator EXP에 영향 없음.

같은 Risk + 같은 결과는 같은 EXP.

Client가 Operator EXP Reward 값을 결정하지 않는다.

---

# 28. 실패 Run Operator EXP

Reference:

```text
FailureExp =
FullClearExp
× 0.8
× ClearedWaves / 15
```

정수 계산:

```text
floor(
  FullClearExp
  × 8000
  × ClearedWaves
  / 10000
  / 15
)
```

Final Clear:

100%.

현재 공식은 그대로 구현한다.

---

# 29. Training Core EXP Curve

Balance Config:

```text
levelExpCurve
```

관리.

Reference 누적:

```text
Lv10  600
Lv20  1,800
Lv30  4,000
Lv40  7,000
Lv50 11,000
```

5명 전체:

```text
3,000
9,000
20,000
35,000
55,000
```

정확한 Level별 Curve는 Balance Config가 Source of Truth.

---

# 30. EXP Refund 정책

**실제 소비한 Operator EXP를 추적하는 방식으로 구현한다.**

```text
investedOperatorExp
```

를 Slot에 저장.

Training Core에서 Operator 제거:

```text
OPERATOR_EXP
+= investedOperatorExp
```

100% 반환.

Curve Patch로 Level Cost가 변경돼도 과거 투자량을 현재 Curve로 역산하지 않는다.

실제로 사용한 EXP를 반환한다.

Skill/Promotion Reset 금지.

새 Operator에게 Refund EXP 자동투자 금지.

새 Core Operator:

```text
trainedLevel = 1
investedOperatorExp = 0
```

---

# 31. Training Core Level Up

Server가:

* 현재 Level
* Balance Version
* Level EXP Curve
* OPERATOR_EXP Balance

로 계산한다.

Client가 Cost를 보내더라도 신뢰하지 않는다.

Transaction:

```text
account_mutation

OPERATOR_EXP 차감
currency_ledger

training_core_slot level 증가
investedOperatorExp 증가

progression_ledger

idempotency
```

원자 처리.

---

# 32. SYNC UP API

예:

```text
POST /v1/operators/training-core/sync-up
```

동작:

```text
가장 낮은 trainedLevel Slot 선택

동률이면 낮은 slotIndex 우선

다음 Level 비용 조회

EXP 충분하면 Level Up

다시 반복

EXP 부족 또는 Max Level이면 종료
```

부분 EXP를 Slot에 저장하지 않는다.

남은 EXP는 Account OPERATOR_EXP Balance에 그대로 둔다.

---

# 33. 신규 Operator

신규 Operator 획득:

```text
account_operators INSERT

Skill Training = 1
Promotion = 0
```

Permanent Level Initialization Cost 없음.

Core 밖이면:

```text
EffectiveLevel = SyncLevel
```

즉 Sync 37 상태에서 Vex 획득:

```text
Vex Lv37 SYNC
Skill 1
Promotion 0
```

---

# 34. Skill / Promotion 비용

기존 Reference 유지.

Skill 1→5 총:

```text
Credits 3,040
Tactical Data 1,520
```

Promotion 0→III 총:

```text
Credits 4,160
Tactical Data 2,080
Special Core 3
```

Operator Permanent Level에서 Credits/Tactical Data 사용 금지.

---

# 35. Risk Permanent Spec

출전 Operator 평균 Permanent Level 기준 폐기.

공식 Operator Permanent Level Spec:

```text
Sync Level
```

초기 Reference:

```text
Risk I     Sync 8~10
Risk II    Sync 16~20
Risk III   Sync 26~30
Risk IV    Sync 36~40
Risk V     Sync 46~50
```

Skill/Promotion은 별도 Spec Parameter.

---

# 36. Training Core와 Deployment

Training Core 5명과 실제 Squad 5명은 동일할 필요 없다.

Training Core 여부를 Deployment 조건으로 사용하지 않는다.

Deployment는 항상 최대 **5인 Squad**.

---

# 37. Operation Preview

흐름:

```text
Objective / Risk 선택
↓
POST /v1/operations/preview
↓
Server
Region
Faction
Environment
Contract 후보
↓
Deployment
↓
POST /v1/runs/start
```

Preview:

* Fuel 미소비
* Balance Version 고정
* Generator Version 고정
* Seed 저장
* Generated Result가 Source of Truth
* TTL
* Account당 ACTIVE 최대 1
* 유효 ACTIVE Preview가 있으면 새 Preview 생성 금지
* Objective/Risk를 바꿔도 무료 리롤 금지
* Player Build 기반 Counter Preview 금지

만료는 Lazy Materialization.

---

# 38. Run Start

Run Start Transaction:

```text
Idempotency
Account Mutation
Account/Open Run
Preview
Contract
Deployment
Client Build
Content Compatibility
Fuel
검증

Fuel Materialize/Consume

Run 생성

RunSnapshot 생성

WavePlan 생성

AugmentPlan 생성

Preview → CONSUMED

runtime.active_preview = NULL
runtime.active_run = Run

Idempotency
```

하나라도 실패하면 전체 rollback.

---

# 39. Run Balance Version

Run은:

```text
Preview.balance_config_version
```

을 그대로 계승한다.

금지:

```text
Run Start 때 최신 activeVersion으로 바꾸기
```

Run:

```text
balance_config_version
```

끝까지 고정.

진행 중 Balance Publish가 발생해도 현재 Run에는 영향 없음.

Reward와 Integrity 역시 **Run Balance Version 기준**.

---

# 40. Run Snapshot

출전 5명의 실제 Effective Level을 Snapshot으로 고정.

```text
operators:
[
  {
    operatorId,
    effectiveLevel,
    levelSource: TRAINED | SYNC,
    skillTrainingLevel,
    promotionTier
  }
]

operatorSyncLevel
```

Run 이후:

* Training Core 교체
* Core Level 증가
* Sync Level 증가
* Permanent Upgrade

현재 Run에 반영 금지.

---

# 41. WavePlan

전체 **15 Wave**를 Run Start에서 Server 생성.

Generated WavePlan이 Source of Truth.

Seed는 Audit/Reproduction 용.

Player Build를 보고 실시간 Counter Enemy/Wave 생성 금지.

Run 시작 후 Plan 수정 금지.

---

# 42. AugmentPlan / Offline Continue

Offline Continue를 위해 Run Start에서 Augment 후보 흐름도 사전 승인한다.

```text
augment_plan_json
augment_generator_version
augment_seed
```

저장.

Offline 가능:

* Battle
* Tactical Upgrade
* Redeploy
* Command
* Ultimate
* 사전 승인 Augment

Offline 불가:

* 광고 Reroll
* Permanent Upgrade
* Fuel Claim
* 새 Run
* 최종 Reward 확정

---

# 43. Checkpoint

Major Server Checkpoint:

```text
Wave 3
Wave 6
Wave 9
Wave 12
Wave 15
```

순서 강제.

Offline에서 앞서가도 재접속 시 순서대로 제출.

Canonical Accepted Checkpoint만:

```text
run_checkpoints
```

저장.

Rejected 제출은 Integrity Evidence.

Checkpoint 승인도 Account Mutation.

---

# 44. Checkpoint 제출 Build

Run 시작 Build와 실제 제출 Build를 모두 보존.

```text
runs.client_build_id
run_checkpoints.submission_client_build_id
run_final_results.submission_client_build_id
```

앱 업데이트 후 Run Resume한 경우도 재현 가능해야 한다.

---

# 45. Run Status

```text
ACTIVE
FINISH_PENDING
FINISHED
FAILED
ABANDONED
EXPIRED
INVALID
QUARANTINED
```

`FAILED`:

정상 게임 패배.

Server Error 의미 아님.

Open Run:

```text
ACTIVE
FINISH_PENDING
QUARANTINED
```

계정당 최대 1개.

---

# 46. Run Status History

현재 상태:

```text
runs.status
```

역사:

```text
run_status_history
```

append-only.

예:

```text
NULL → ACTIVE
ACTIVE → QUARANTINED
QUARANTINED → ACTIVE
ACTIVE → FINISHED
```

과거 Quarantine 이력을 잃지 않는다.

---

# 47. Run Finish

Client 제출:

* Outcome
* Wave Summary
* Tactical Summary
* Damage/Action Summary
* Final State

Client가 보내지 않는 것:

* CreditsEarned
* PartsEarned
* TacticalDataEarned
* SpecialCoreEarned
* OperatorExpEarned
* CommanderXP

Reward는 서버가 계산한다.

---

# 48. Reward

Run Reward:

```text
Wave
Final
Operation
Contract
Boss
First Clear
Achievement
Operator EXP
```

Server에서 계산.

Reward는:

```text
runs.balance_config_version
```

기준.

현재 Active Balance 사용 금지.

---

# 49. Result Ad +50%

광고 증가 대상:

```text
CREDITS
MACHINE_PARTS
TACTICAL_DATA
```

초기 정책상 광고 증가 제외:

```text
SPECIAL_CORE
OPERATOR_EXP
```

즉:

```text
adEligible SPECIAL_CORE = 0
adEligible OPERATOR_EXP = 0
```

DB/Application 양쪽에서 검증.

---

# 50. Ad Reward SSV

Client 광고 완료 callback만으로 지급 금지.

흐름:

```text
Server Ad Intent
↓
Signed custom_data
↓
Rewarded Ad
↓
Provider SSV
↓
Provider Signature Verify
↓
LAST RAIL Binding Verify
↓
Business Validation
↓
VERIFIED
↓
Account Mutation
↓
Economy Grant
↓
GRANTED
```

Provider `reward_amount`를 LAST RAIL Currency 값으로 사용 금지.

Bonus:

```text
floor(base × 5000 / 10000)
```

Provider Transaction ID Replay 방지.

Base Reward당 Result Bonus 최대 1회.

Invalid Provider Signature나 invalid LAST RAIL Binding 요청으로 실제 Intent를 REJECT시키지 않는다.

---

# 51. False Positive 후 광고

Validator 오탐/Server Defect로 Run Reward가 늦게 복원된 경우:

**정상 Run Reward 생성 시각부터 새로운 Ad Eligibility Window를 부여한다.**

서버 문제 때문에 광고 기회를 잃지 않게 한다.

Compensation 자체에는 Result Ad를 붙이지 않는다.

---

# 52. Integrity / Anti-Cheat

Frame 전체 재시뮬레이션 금지.

Validator 카테고리:

```text
PROTOCOL
PROGRESS
SUPPLY
UPGRADE
AUGMENT
ACTION
ULTIMATE
COMMAND
OPERATOR_DAMAGE
TURRET_DAMAGE
ENEMY_DAMAGE
BOSS
REWARD
```

판정:

```text
PASS
SOFT_ANOMALY
HARD_VIOLATION
```

PASS는 원칙적으로 Integrity Event 생성하지 않음.

---

# 53. Integrity Evidence

```text
integrity_events
```

는 immutable.

기록:

* Run
* Balance Version
* Run Start Build
* Submission Build
* Original Validator
* Runtime Revision
* Evaluated Validator
* Evidence Code
* Evidence
* Fingerprint
* Classification

최초 HARD/SOFT 판정을 나중에 UPDATE해서 없애지 않는다.

---

# 54. Integrity Resolution

별도:

```text
integrity_event_resolutions
integrity_event_review_states
```

사용.

Resolution:

```text
CONFIRMED_VIOLATION
FALSE_POSITIVE
SERVER_DEFECT
CLIENT_DEFECT
BUG_EXPLOITATION
RESOLVED_NO_ACTION
```

Revalidation은 새로운 Validator Version/Runtime Revision으로 별도 기록.

과거 Original Detection 보존.

---

# 55. Enforcement

Observation:

```text
NORMAL
WATCHED
```

실제 제재:

```text
REWARD_RESTRICTED
TEMP_SUSPENDED
PERMANENT_BAN
```

분리.

HARD_VIOLATION != Permanent Ban.

Gameplay DPS 하나만으로 자동 Permanent Ban 금지.

DB에서도:

```text
AUTOMATED + PERMANENT_BAN
```

생성 차단.

Permanent Ban:

OWNER + Admin Confirmation.

---

# 56. False Positive 복구

순서:

```text
Integrity Resolution
↓
Run/Watch/Enforcement 복구
↓
원래 Run Reward 복원
↓
First Clear 복원
↓
필요하면 추가 Compensation
```

원래 Run Reward와 Compensation을 혼동하지 않는다.

---

# 57. Compensation

```text
compensations
compensation_recipients
```

Campaign과 Recipient 분리.

DRAFT:

편집.

PREPARED:

* Reward Spec Freeze
* Recipient Population Freeze
* Recipient Snapshot Hash

EXECUTING:

Async Grant.

COMPLETED:

Pending 0.

CANCELLED:

새 Grant 시작 중단.

---

# 58. Compensation Grant

실제 지급 순간만 Account Mutation.

같은 Mutation으로:

```text
Currency
Fuel
Recipient GRANTED
```

을 묶는다.

Retry-safe.

PROCESSING 상태를 DB에 영속화하지 않는다.

기술 실패:

```text
PENDING 유지
attempt_count 증가
```

---

# 59. Compensation과 Operator EXP

Compensation Resource Shape에서도:

```text
OPERATOR_EXP
```

지원.

단 Compensation은 Balance Version에 따라 계산되는 게 아니라 운영 결정 자체가 Source of Truth.

---

# 60. Admin Jobs / Queue

무거운 작업:

* Compensation Prepare/Execute
* Integrity Revalidation
* Economy Reconciliation
* Bulk Analysis
* Aggregate
* Alert

는:

```text
admin-api
↓
admin_jobs
↓
Queue
↓
async-worker
```

사용.

RUNNING Job은:

```text
lease_owner
lease_expires_at
```

필수.

Worker 죽으면 lease 만료 후 다른 Worker가 회수 가능.

Job Progress JSON은 Materialized Summary일 뿐 Domain Source of Truth가 아니다.

---

# 61. Compensation Cancel

Cancel:

```text
새 Recipient Grant 시작 금지
```

의미.

이미 실제 D1 Grant Transaction에 진입한 in-flight 작업은 Commit될 수 있다.

기존 GRANTED 지급 자동 Reversal 금지.

---

# 62. Analytics

Core D1과 Raw Analytics 분리.

Server-generated:

* Run Result
* Reward
* Upgrade
* Fuel
* Integrity
* Ad Result

Unity Telemetry:

* FPS
* UI
* Tutorial UX
* Network
* SDK Error
* Ad Load/Show Error

Enemy Attack 1회마다 Telemetry 전송 금지.

Wave/Run Summary 중심.

---

# 63. Body Limit

초기 정책:

```text
일반 API
32 KB

Telemetry
64 KB

Checkpoint / Finish
128 KB
```

플랫폼 최대치를 그대로 허용하지 않는다.

추가로:

* 배열 길이
* 문자열 길이
* JSON depth

제한한다.

---

# 64. Rate Limit / DDoS

구조:

```text
Internet
↓
Cloudflare DDoS
↓
WAF
↓
Rate Limit
↓
Worker
↓
Account Rate Limit
↓
Body/Auth/Schema
↓
D1
```

가능하면 Worker CPU를 사용하기 전에 차단.

---

# 65. Protection Mode

Server Runtime Config:

```text
NORMAL
ELEVATED
HIGH
LOCKDOWN
```

공격 시 우선 제한:

```text
Telemetry
↓
Guest Creation
↓
Preview
↓
New Run
```

기존 Run Finish/Reward는 최대한 보호한다.

---

# 66. Migration 원칙

Migration:

```text
0001_initial.sql
0002_economy.sql
0003_progression.sql
0004_operation.sql
0005_run.sql
0006_integrity.sql
0007_compensation.sql
0008_admin.sql
0009_ads.sql
0010_indexes.sql
```

Git 관리.

Production 직접 SQL 수정 금지.

원칙:

```text
Expand
→ Migrate
→ Contract
```

현재 아직 정식 서비스 이전이라면 기존 개발 Schema보다 이 Source of Truth를 우선한다.

---

# 67. 주요 D1 영역

최종 최소 테이블 책임:

```text
accounts
auth_identities
auth_sessions

admin_principals

player_profiles
account_runtime_state
account_mutations
account_tutorial_state

balance_config_drafts
balance_config_publish_claims
balance_config_versions
balance_config_activations
balance_config_active

client_builds

validator_versions
validator_runtime_configs
validator_runtime_active

account_currencies
economy_transactions
currency_ledger
fuel_state
fuel_ledger
reward_transactions
idempotency_records

account_operators
operator_training_core_slots
account_train
account_carriages
account_turrets
commander_presets
account_unlocks
progression_ledger
account_first_clears

operation_previews

runs
run_status_history
run_snapshots
run_wave_plans
run_checkpoints
run_wave_results
run_augment_history
run_tactical_ledger
run_final_results

integrity_events
integrity_event_review_states
integrity_event_resolutions
integrity_incidents
integrity_incident_events
account_integrity_state
account_enforcements

compensations
compensation_recipients

admin_confirmations
admin_audit_logs
admin_jobs

ad_reward_transactions
```

`account_daily_state` 같은 기능은 구체적 Daily Rule이 확정되지 않았으므로 임의 구현하지 않는다.

---

# 68. API 기본 구조

비버전:

```text
GET /status
```

Game:

```text
/v1/*
```

최소:

```text
POST /v1/auth/guest
POST /v1/auth/refresh

GET /v1/bootstrap

GET /v1/balance-config/:version

GET /v1/profile
GET /v1/economy
GET /v1/fuel

GET /v1/operators
POST /v1/operators/training-core/:slot/level-up
POST /v1/operators/training-core/sync-up
POST /v1/operators/training-core/:slot/replace

POST /v1/operations/preview

POST /v1/runs/start
POST /v1/runs/:id/checkpoints
POST /v1/runs/:id/finish

POST /v1/ads/result-bonus/prepare

POST /v1/telemetry/batch
```

Provider:

```text
/v1/provider-callbacks/admob/rewarded
```

Admin API는 Game API와 별도 Worker.

---

# 69. Server Runtime Config

Balance가 아닌 운영 설정:

* Preview TTL
* Run Expiration
* Confirmation TTL
* Ad Eligibility TTL
* SSV Callback Grace
* Rate Limit
* Protection Mode
* Maintenance
* Body Limits

는 Balance Config에 넣지 않는다.

별도 Server Runtime Config 책임으로 관리한다.

---

# 70. Balance Simulation Tool

Admin/Server 기능으로 설계한다.

Unity Editor가 Source of Truth가 아니다.

Draft 또는 Published Balance 대상으로 대량 Simulation 지원.

축:

```text
Risk

Permanent Spec Band
UNDER_SPEC
CHALLENGE_SPEC
RECOMMENDED_SPEC
COMFORT_SPEC

Augment Quality
AUGMENT_POOR
STANDARD
STRONG
HIGHROLL

Tactical Build Quality
TACTICAL_POOR
STANDARD
OPTIMAL

Representative Build
Balanced
Turret Firepower
Operator Strike
Anti-Boarding
Fortress
Greedy

다수 RNG Seed
```

주요 Acceptance Reference:

```text
RECOMMENDED + STANDARD
75~85%

COMFORT + POOR
100%
```

COMFORT 100% 기준은 Non-Sabotage Simulation Policy.

Balance가 목표에서 벗어나면 Permanent Spec Curve를 쉽게 바꾸기보다:

```text
Enemy
Wave
Threat
Hazard
Boss
```

측을 우선 조정하는 운영 원칙.

Simulation 결과:

* Clear Rate
* Failure Wave
* KPI
* Before/After
* Validation
* Stored Result

를 Admin에서 조회 가능하도록 서버 구조를 분리해 둔다.

이 Phase는 Core Game API가 안정된 뒤 구현한다.

---

# 71. Testing 필수

모든 Domain Service는 Unit Test.

D1 Integration Test:

```text
Migration 전체 적용

PRAGMA foreign_key_check

PRAGMA quick_check
```

필수.

대표 Query:

```text
Bootstrap Open Run
Active Preview
Idempotency
Reward Duplicate
Checkpoint Retry
Integrity Lookup
Compensation Pending Batch
Admin Job Lease
Ad Provider Transaction
```

은:

```text
EXPLAIN QUERY PLAN
```

으로 Index 사용 확인.

불필요한 Index를 무작정 추가하지 않는다.

---

# 72. Race Condition Test

최소 다음 경쟁을 자동 테스트한다.

```text
Operator Level Up vs Level Up

Run Start vs Operator Level Up

Run Start vs Training Core Replace

Preview Create vs Preview Create

Checkpoint vs Checkpoint Retry

Checkpoint 15 vs Finish

Reward Retry

Compensation Retry

Ad SSV Retry

Admin Confirmation Retry

Balance Publish vs Publish

Balance Publish vs Rollback
```

한 요청만 Commit되고 다른 요청은 Reload/Recompute하도록 검증.

---

# 73. Cloudflare Local 검증

Dependency 설치 후:

```text
pnpm install
```

Wrangler Version 확인.

Local D1:

```text
0001~0010 migration apply
```

후:

```text
PRAGMA foreign_key_check
PRAGMA quick_check
```

Type:

```text
wrangler types
```

TypeScript:

```text
typecheck
```

Tests:

```text
unit
integration
```

Worker:

```text
deploy --dry-run
```

전부 수행한다.

---

# 74. DEV Cloudflare 연결

Cloudflare 인증이 이미 되어 있거나 내가 로그인 승인을 완료한 경우에만 실제 DEV를 연결한다.

먼저:

```text
wrangler whoami
```

확인.

D1:

```text
lastrail-core-dev
```

생성.

반환된 실제 database_id를:

```text
game-api DEV
admin-api DEV
```

둘 다 같은 DEV Core DB에 연결한다.

단 DEV/PRD와 공유 금지.

DEV Remote Migration 적용.

Remote:

```text
foreign_key_check
quick_check
```

확인.

그다음:

```text
dev-api.lastrail.app
dev-admin.lastrail.app
```

연결을 준비한다.

---

# 75. Cloudflare Access DEV Admin

`dev-admin.lastrail.app`도 가능하면 Cloudflare Access 보호.

허용 대상은 초기에는 내 계정만.

Production Access/MFA 구조와 동일한 방식.

Access 설정을 자동 수행할 권한/API가 없다면 임의 우회하지 말고:

```text
Cloudflare Dashboard에서 필요한 정확한 설정 절차
```

를 문서화한다.

---

# 76. PRD 금지선

내가 별도로 승인하기 전:

```text
lastrail-core-prd 생성

prd 배포

api.lastrail.app PRD 연결

admin.lastrail.app PRD 연결

PRD Balance Publish

PRD D1 Mutation
```

실행 금지.

Config와 문서만 준비한다.

---

# 77. Backup / Migration 운영

Production 도입 시:

* Cloudflare D1 복구 기능 활용
* Migration 전 Restore Point/Backup 확보
* 별도 R2 Backup 준비
* Daily / Weekly / Monthly 보존 정책 문서화

Production DB 전체 Restore는 최후수단.

경제 사고는:

```text
Ledger
Correction
Reversal
Compensation
```

을 우선한다.

---

# 78. Secret

Git에 Secret 절대 저장 금지.

로컬:

```text
.dev.vars
```

Production:

```text
Workers Secrets / Secrets Store
```

사용.

예:

```text
AUTH_SIGNING_KEY_CURRENT
AUTH_SIGNING_KEY_PREVIOUS

AD_BINDING_KEY_CURRENT
AD_BINDING_KEY_PREVIOUS
```

Signing Key Rotation을 고려한다.

---

# 79. 구현 Phase

아래 순서로 진행한다.

## Phase 1 — Repository/Foundation

* Monorepo
* pnpm
* TypeScript
* Hono
* Wrangler
* ESLint
* Test setup
* Environment structure
* Common Result/Error/Logger/ULID/CanonicalJSON

## Phase 2 — D1 Schema

* 0001~0010
* Training Core 반영
* OPERATOR_EXP 반영
* 전체 FK
* Index
* Local D1 검증

## Phase 3 — Auth

* Guest
* Refresh
* Access Token
* Refresh Rotation
* Bootstrap Admin Owner Script

## Phase 4 — Balance

* Draft
* Validation
* Publish
* Rollback
* Fetch API
* Content Version Gate

## Phase 5 — Bootstrap / Permanent State

* Profile
* Economy
* Fuel
* Operators
* Training Core
* Train
* Turret
* Unlock
* Tutorial

## Phase 6 — Progression

* Core Level Up
* Core Replace
* Refund
* SYNC UP
* Skill
* Promotion
* Economy linkage

## Phase 7 — Operation Preview

* Preview Generator
* TTL
* Anti-reroll
* Balance freeze

## Phase 8 — Run Start

* Fuel
* Snapshot
* Effective Levels
* WavePlan
* AugmentPlan
* Account Mutation
* Version freeze

## Phase 9 — Checkpoint / Offline Continue

* Checkpoint 3/6/9/12/15
* Wave Results
* Tactical Ledger
* Augment History
* Retry

## Phase 10 — Finish / Reward

* Victory/Failure
* Operator EXP
* First Clear
* Reward
* FINISH_PENDING
* Run status history

## Phase 11 — Integrity

* Fast Validator
* DPS Envelope
* Deterministic Validators
* Events
* Quarantine
* Resolution
* Revalidation
* Enforcement

## Phase 12 — Ads

* Ad Intent
* AdMob SSV
* Signed custom_data
* +50%
* Replay protection
* Retry

## Phase 13 — Compensation

* Campaign
* Recipients
* Async Grant
* False Positive restoration path

## Phase 14 — Admin API

* Principal
* RBAC
* Confirmation
* Audit
* Jobs
* Balance operations
* Integrity
* Compensation

## Phase 15 — Analytics / Queue

* Analytics Engine
* Telemetry batch
* Queue
* Async Worker

## Phase 16 — Cloudflare Security

* Access
* MFA
* WAF
* Rate Limits
* Body Limits
* Protection Mode

## Phase 17 — Balance Simulation Tool

* Simulation profile
* Seeds
* Acceptance Matrix
* Stored results
* Draft/Published comparison

---

# 80. 구현 방식

각 Phase는:

```text
Failing Test
↓
Implementation
↓
Unit Test
↓
Integration Test
↓
Typecheck
↓
Lint
↓
Review
↓
Commit
```

순으로 진행한다.

큰 Phase는 작은 Commit으로 나눈다.

기존 테스트가 깨지면 그냥 무시하지 말고 원인을 분석한다.

---

# 81. 금지사항

절대 하지 말 것:

```text
Unity ScriptableObject를 Balance 최종 Source of Truth로 사용

Published Balance 직접 수정

Published Balance 삭제

Run 진행 중 Balance Version 교체

Client Cost 신뢰

Client Reward 신뢰

Client Operator EXP Reward 신뢰

Route에서 SQL 직접 실행

Route에서 Balance 직접 수정

Route에서 Reward 계산

Account Currency 직접 UPDATE만 하고 Ledger 생략

Player Build 기반 실시간 Counter Enemy 생성

Raw Combat Frame 서버 재시뮬레이션

Admin 직접 ID/Password Auth 새로 구현

Admin Console에서 D1 직접 접근

Secret Git 저장

DEV/PRD DB 공유

Production 직접 SQL 수정

내 승인 없는 Production 배포
```

---

# 82. 문서

반드시 작성:

```text
README.md

docs/architecture/server.md
docs/architecture/data-ownership.md

docs/api/game-api.md
docs/api/admin-api.md

docs/run-protocol/run-protocol.md

docs/cloudflare/setup.md
docs/cloudflare/environments.md
docs/cloudflare/admin-access.md

docs/balance/versioned-balance.md
docs/balance/training-core.md

docs/integrity/integrity.md

docs/operations/backup-recovery.md
```

---

# 83. 완료 조건

서버 전체 작업 완료라고 보고하려면 최소:

```text
pnpm install 성공

전체 TypeScript typecheck 통과

unit tests 통과

integration tests 통과

0001~0010 Local D1 Migration 통과

foreign_key_check clean

quick_check ok

P0 Query Plan index 확인

Game API /status 정상

Admin API /status 정상

Guest Auth 정상

Bootstrap 정상

Balance Draft/Publish/Rollback 테스트

Training Core / Sync Level 테스트

Operator EXP Level Up/Refund/SYNC UP 테스트

Operation Preview 테스트

Run Start 테스트

Offline Checkpoint 순서 테스트

Run Finish Reward 테스트

False Positive 복구 테스트

Ad Duplicate/SSV 테스트

Compensation Retry 테스트

Admin RBAC/Confirmation/Audit 테스트

wrangler deploy --dry-run 통과
```

가 있어야 한다.

---

# 84. 최초 작업 시작

지금 저장소가 비어 있다면 다음부터 바로 시작한다.

```text
1. Repository 상태 검사

2. 위 Source of Truth를
   docs/architecture/server.md에 정리

3. pnpm Monorepo 생성

4. game-api/admin-api/async-worker 생성

5. packages 구조 생성

6. 테스트 환경 구성

7. 0001~0010 Migration 작성

8. Local D1 전체 검증

9. 그다음 Auth/Bootstrap부터 구현
```

먼저 전체 저장소를 확인하고 **Phase 1 → Phase 2부터 실제 구현을 시작해라.**

내가 이미 확정한 위 요구사항을 다시 설계 질문으로 되돌리지 말 것.

다만 Cloudflare 로그인, Domain 인증, 결제/외부 계정 권한처럼 내 직접 승인이 필요한 경우만 작업을 중단하고 필요한 행동을 정확히 알려줘.

각 Phase 완료 때 실제 테스트 증거를 포함하여 보고하고 다음 Phase로 계속 진행해라.
