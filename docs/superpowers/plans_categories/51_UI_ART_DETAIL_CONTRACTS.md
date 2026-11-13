<!-- RESOURCE-REFERENCE-AUDIT-20261029 -->
> **리소스 참조 재검사 완료(2026-10-29):** 남은 169개 문서 표기를 실제 위치·묶음/논리 경로·역사 자료로 대조했다. 정확한 경로는 본문을 정정했고, 없는 옛 증거는 현행 입력에서 제외 표시했다. 상세: D:/Project/LASTRAIL/QA/ResourceCleanup-20261028/169-decisions.json. 역사 자료는 최신 승인본을 대체하지 않는다. 실제 Player에는 옛 요원 미술과 임시 객차가 남아 있어 최신 아트 연결 완료가 아니다.

<!-- CURRENT-REFERENCE-20261029 -->

<a id="combat-hud-skin-slots"></a>
### 전투 HUD 외형 스킨 제작 기준 · 2026-11-04

Direction은 기본 외형도 향후 BM 상품 또는 다이아 구매 스킨으로 교체할 수 있게 제작한다. Unity는 같은 조작부에서 외형만 교체한다. 판매 방식·가격·획득 조건은 후속 경제 기획에서 결정한다.

| 독립 교체 단위 | 외형 자산 | 화면이 별도로 소유하는 요소 |
|---|---|---|
| 요원 카드 프레임 | 투명 테두리, 모서리 장식, 금속 재질 | 초상화·마스크, 이름, HP, 역할 문양, 궁극기 게이지·완충광, 선택 표식 |
| 열차 운행 손잡이 | 속도 레버 손잡이와 선로 노즐 손잡이의 개별 외형 | 손잡이의 기준점과 이동축, 속도 3단·선로 좌/중앙/우 정지점, 드래그·확정 입력 |
| 운행 조작부·콘솔 프레임 | 고정 하우징, 내부 판, 외곽 장식 | 움직이는 손잡이, 눈금, 선택등, 버튼 상태, 입력 영역 |

납품 시 교체 단위마다 기본 외형과 같은 기준점·표시 영역을 명시한다. 스킨 교체 또는 기본 외형 복귀 후에도 같은 카드 상태에서 초상화와 네 역할색을 읽을 수 있어야 한다. 실제 전투 표시 크기에서 손잡이의 세 위치와 드래그·취소·확정을 같은 입력 영역으로 검수한다. 판매용 외형 선택은 전투 수치·입력 판정에 영향을 주지 않는다.

> **2026-10-29 현행 참조 우선순위.** 아래 누적 기록은 작성 당시 이력이며, 옛 시안의 존재·파일명·도구 승인 필드만으로 현행 승인본을 판단하지 않는다. 최신 승인 정책과 실제 제작 REVIEW/QA를 함께 확인한다. 가로 화면, 애니메이션 2번/반대 방향, 카일 순수 맨손 무공 및 노드 시작 재입장이 과거 상충 지침보다 우선한다. 최신 제안은 사용자 승인 전까지 적용하지 않는다. 삭제 목록은 `D:/Project/LASTRAIL/QA/ResourceCleanup-20261028/deleted.jsonl`; 삭제된 원화를 임의 복원하거나 다른 버전으로 조용히 대체하지 않는다. 이번 정리는 링크·정책 진입점 대조이며 자산/런타임 전체 검수 완료가 아니다.
현행 원본: [전체 화면 가로 전환](33_CURRENT_WORK_STATUS.md#all-screens-landscape-approved-20261023) · [2방향 애니메이션](17_OPERATOR_COMMON_STYLE.md#landscape-d2-animation-approved-20261023) · [카일 맨손 디자인](17_OPERATOR_COMMON_STYLE.md#kyle-pure-martial-design-approved-route-20261026) · [공통 화풍](17_OPERATOR_COMMON_STYLE.md#eve-v10-common-style-approved-20261026) · [노드 시작 재입장](44_COMBAT_DETAIL_CONTRACTS.md#battle-node-restart-user-correction-20261028).

> **2026-10-02 현행 전투 변경:** A안 신규 적용 폐기, B안 채택. [44 현행 전환 결정](44_COMBAT_DETAIL_CONTRACTS.md#combat-b-mainline-approved-20261002)이 과거 A 유지/B 임시 및 객차 장착 포탑 전제보다 우선한다. 특정 노드 내부전은 별도 유지. 미승인 세부·기존 snapshot·역사자료는 구분하며 일괄 삭제/수치 치환하지 않는다.
# 51. 화면 승인·UI·아트 제작 상세 계약

2026-09-21 번호별 통합. 이 문서는 흩어진 상세 계약을 주제별로 모은 참조본이다. 원문별 제목·출처·기존 검토 상태를 유지하고, 정책 확정·후보·과거 구현 보고를 섞어 승인하지 않는다.

**적용 순서:** 최신 사용자 결정 → 00~29 담당 정책 및 34~37 충돌 정리 → 이 문서의 충돌하지 않는 상세 계약. 원문의 작성일·완료 문구만으로 현재 구현 또는 승인 완료를 뜻하지 않는다. 원문에 있던 C/E 경로는 과거 기록이며 현재 작업 경로는 프로젝트 AGENTS.md의 D 정책을 따른다.

**공통 대체 경계:** 증강 고정 5회·시작 지급은 폐기, 적격 풀 동일 가중치 3후보·카드별 FREE3+EXTRA1·선택 중 완전 정지가 현행이다. 증강 후보 효과 수치는 출시 승인값이 아니다. 순위 보상은 성장 재화 차등 없이 외형·명예로 구분한다. 인물 역할은 20~29 최신 바이블 우선이며 과거 abilityId 매핑은 자동 변경하지 않는다. 작전물자 30/20·노드3선택/물리2방향·협동 시간·T18 HP0 등 미결정은 34와33의 미결 상태를 유지한다.

원문 내 코드 블록·JSON·표·필드·수치는 생략하지 않았다. 옛 상대 경로를 포함하는 코드 예시는 원출처 기준이며 실행 지시가 아니다. 링크와 제목 앵커만 새 위치에 맞게 변환했다.

## 상세 목차

- [Opt-in real DEV login → download → lobby: server prerequisites](#source-e6e0a844347e9bf9)
- [지휘관 3종 편성 및 분대장 충전 연출 상세기획 r1](#source-b150572e3b350007)
- [전투 세부 규칙·연출 상세기획 v1](#source-7dd86e35b7c512fd)
- [Combat Mechanic Warning UI V1](#source-b41a901d11532729)
- [전투 연출 보강 v03 — 2026-09-12](#source-e020940776a37ed6)
- [Departure Preparation Screen V1](#source-2c187562f447eff3)
- [Admin screen read integration - 2026-09-10](#source-f027f3659659d270)
- [App Icon Production - 2026-09-08](#source-cad753599df41f17)
- [전투 HUD 콘텐츠·미술 인계 r1](#source-0162a4001283270c)
- [전투 연출 제작명세 — 2026-09-12](#source-78eacf9c42a3e722)
- [Lobby Navigation Proposal](#source-2a73c19463d80b2d)
- [Lobby Screen Detail Proposal](#source-ff6ba45bd7d6f8ea)
- [New Player Guidance Proposal](#source-f59ad886e2765803)
- [일반 5종·궁극 5종 시안과 구현 차이 — 2026-09-13](#source-168523dd5098955c)
- [HP0 incapacitation pose candidates](#source-6804b653993214cf)
- [Skill gesture samples added — v02](#source-a189bbb30640efff)
- [전원 궁극기 카드 UI — 최신 사용자 기준](#source-d8e95303990ee060)
- [Startup Motion Art Backlog](#source-8bafad2f0b3c60ac)
- [Startup/login partial delivery v03 - 2026-09-08](#source-1115ac84904315f4)
- [Startup Motion Sample Gate - 2026-09-08](#source-72b59c808eee9904)
- [증강 선택 최종시안](#source-f2447fd0ba0513fa)
- [전투 화면 3차 최종시안](#source-61f16f3947c254bb)
- [갤러리 1–28 사용자 결정](#source-f05ef25c6c893f7d)

<a id="source-e6e0a844347e9bf9"></a>
## Opt-in real DEV login → download → lobby: server prerequisites

원출처: `WorkspaceRepo/docs/api/dev-login-download-lobby-test.md`  
원문 SHA-256: `34f7acd85c4f28863d0473e38e311dd1935016a2d7063d2750adcbfe7b570ad0`  
검토·적용 경계: Detailed client registry/publication contract survives canonical policy overrides; prior audit identified missing wire/ETag/exact-version/rollback details in summaries.

<!-- source-body:source-e6e0a844347e9bf9:start -->
<!-- Local correction source: c7163e4b00e0b946d4be775c40992c7c5695116b; deployed DEV remains c08bfcd. -->

<a id="source-e6e0a844347e9bf9-opt-in-real-dev-login--download--lobby-server-prerequisites"></a>
### Opt-in real DEV login → download → lobby: server prerequisites

Updated 2026-09-08 KST. Status: **BLOCKED**, not a live test success.
Canonical contract: `docs/api/client-release-registry.md` in the planning checkout.
Server implementation: `C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/attendance-pass-20260907/server`.
Unity owns the opt-in suite and real HTTP/session adapter. This document supplies
its server prerequisite and assertion contract; it is not an executable test result.

<a id="source-e6e0a844347e9bf9-dev-policy-correction--local-only-2026-09-08"></a>
#### DEV policy correction — local only, 2026-09-08

DEV uses STABLE/DISABLED only; INTERNAL/STABLE separation and tester entitlement
are PRD-only. The local correction on codex/dev-stable-registry-20260907 is not yet
deployed. DEV2026.09.07.4 c08bfcd and D1 migrations0022–0024 were already deployed;
do not repeat that rollout. A separate approved deployment of the correction is
needed before validating the new DEV semantics remotely.
Prior deployment: [DEV2026.09.07.4 report](50_RELEASE_OPERATIONS_CONTRACTS.md#source-3a289b92502a9144). No registry activation,
tester grant, publication, policy seed or additional account is part of this patch.
DEV has no tester prerequisite; retain content publication/activation, exact code,
minimum, normal authentication, release pin and economy gates. Timestamped older
publication/session observations below are not a fresh remote inventory.

<a id="source-e6e0a844347e9bf9-latest-narrow-authorization-update--2026-09-07"></a>
#### Latest narrow authorization update — 2026-09-07

Plan relayed explicit user approval for Unity code and actual DEV testing that sends
only the dedicated DEV test account session to exactly:
`https://dev-api.lastrail.app/v1/client-config/authorized`.
No other account, PRD endpoint, token logging/documentation/cross-task transmission,
new secure-storage mechanism, registry deployment or remote migration is authorized
by this update. Keep the single existing guest-creation/replay-key budget. No token
value or account credential was read or transmitted in this server checkpoint.

The missing trusted-user consent for that exact DEV adapter destination is now
provided. Unity must apply it through normal approval review. Approval arrival is
not evidence that its formerly rejected patch/test runner is applied, that tool
review now accepts it, or that a real request succeeded. AUTH_ADAPTER stays an
implementation/readiness prerequisite until Unity supplies actual verification.
Prior text asking for adapter destination consent is superseded only within this
narrow DEV scope; it must not be read as an outstanding request for the same consent.

Newer independent publication evidence was read from Unity's
`E:/Projects/LASTRAIL/unity-project/QA/Reports/dev-publication-check.json`:
2026-09-07T11:28:09.7259591Z, catalog_0.1.0.bin HTTP404, bytesMatch=false,
sha256Match=false, status=BLOCKED. This is public HTTPS evidence only; no activation
or login evidence. Server did not repeat that request. A05 publication/activation,
deployment of the DEV correction remains separate; a tester entitlement is not a DEV gate.

<a id="source-e6e0a844347e9bf9-current-evidence-and-stop-conditions"></a>
#### Current evidence and stop conditions

| Prerequisite | Current observed state | Test decision |
| --- | --- | --- |
| Actual A05 public objects uploaded and HTTPS hashes verified | Upload previously rejected; Unity HTTPS report at11:28:09Z shows catalog404, no hash match | BLOCKED_ARTIFACT_PUBLICATION |
| versions/current.json activated by successful fresh-ETag CAS/readback | No activation performed by this server task; last observed legacy pointer had no contentRelease | BLOCKED_CONTENT_ACTIVATION |
| DEV policy correction deployed | c08bfcd registry foundation deployed; DEV STABLE correction local only | BLOCKED_DEV_POLICY_NOT_DEPLOYED for new DEV semantics |
| Tester entitlement | PRD INTERNAL only; never required in DEV | Not a DEV prerequisite |
| Unity authenticated adapter and session pin | Narrow DEV session consent supplied; patch/runner acceptance and wiring not yet verified | BLOCKED_AUTH_ADAPTER |
| Approved dedicated account | Private state checked: no account, no access token, no seed, no pull checks | Account budget unused; do not create until other prerequisites pass |

These publication/pointer observations are timestamped task evidence, not a fresh
remote inventory. No repeated remote probes, new upload, pointer write, account
creation or token transmission occurred in this checkpoint. A changed state requires
fresh authoritative readback before updating these gates.

Actual A05 release identity and public-file whitelist remain in the approved artifact
packet and SERVER_1 handoff; do not replace them with synthetic registry fixtures.
The test must take expected release identity/hash from that approved packet/readback.
Presence of a local build, DNS, TLS, R2 bucket, or a passing fixture is insufficient.

Do not silently skip and count a gated real case as passed. Emit an explicit BLOCKED
result with the prerequisite code(s); keep fixture and real-DEV totals separate.
For a legacy deployed server after actual A05 activation, a legacy smoke may be run
within its approved scope, but it cannot prove registry INTERNAL/STABLE behavior.
Do not deploy the new registry or apply remote migrations merely to make a test pass.

<a id="source-e6e0a844347e9bf9-normal-api-sequence-after-prerequisites-are-satisfied"></a>
#### Normal API sequence after prerequisites are satisfied

1. Use configured official DEV base `https://dev-api.lastrail.app` and actual installed
   App-Version/Version-Code/Platform/Channel headers. Do not spoof a higher code,
   use account-id authority, or disable admission. No PRD calls in this DEV suite.
2. GET `/v1/client-config` without credentials. Legacy/STABLE requires accepted=true,
   clientAdmission.status=ACCEPTED and contentRelease.status=APPROVED_COMPATIBLE.
   DEV must not enter an INTERNAL AUTH_REQUIRED/tester flow. A stored DEV INTERNAL
   row is invalid policy and stops the test with version_policy_unconfigured.
   Unknown, disabled, update-required, malformed policy, offline or503 stops the flow.
3. Reuse the approved dedicated session if already created. Otherwise the sole
   approved account creator uses POST `/v1/auth/guest` with JSON `{}` and the existing
   high-entropy persisted Idempotency-Key (16–128 characters). Coordinate account
   budget ownership; Unity must not create a second guest beside the server's approved
   gacha-verification guest. No new key on timeout/response loss; no automatic guest
   recreation on401/403/5xx. Test identity/state is not a fixture account.
4. Verify HTTP200 tokenType=Bearer, account.status=ACTIVE, nonempty account.id,
   accessToken/accessTokenExpiresAt and refreshToken/refreshTokenExpiresAt. Keep
   credential material in the existing approved secure-session mechanism. Never put
   raw tokens, replay keys or credential response bodies into logs, XML or handoffs.
   POST `/v1/auth/refresh` uses `{refreshToken}` with one persisted rotation replay key
   only when restoring the existing session; it does not allocate a new account.
5. GET `/v1/profile` with that session, no body or account query. Require accepted=true
   and account.id equal to the authenticated account. Read profile.displayName. If
   already set, preserve it. If missing, use the agreed test nickname (2–16 Unicode
   characters; letters/numbers/space/_/-), GET `/v1/profile/display-name/check?displayName=...`,
   require available=true, then POST `/v1/profile/display-name` with `{displayName}`.
   GET profile again and verify the persisted normalized name/account. Do not loop
   across names or ignore cooldown/taken errors. GET `/v1/story-progress` hydrates the
   same authenticated player without granting rewards or starting gameplay.
6. On a deployed registry server, GET `/v1/client-config/authorized` with the existing
   session. Require accepted=true, ACCEPTED, APPROVED_COMPATIBLE, exact installed code
   and expected releaseId/catalogHash. DEV requires no tester entitlement. Keep the quoted
   registryRevision for revalidation and bind releaseId to this session.
7. Download only the authorized catalog/bundles. Do not forward API Bearer credentials
   to the public asset host. Verify approved catalog identity and required bundle
   completion. Preserve network consent and cancellation behavior; no Editor-local
   fallback can count as an actual remote download.
8. Revalidate the current authorized release after download; stop on changed policy,
   disabled/update-required code, changed identity or session logout. Retain
   `X-LastRail-Content-Release-Id` for registry-protected gameplay requests. Enter Main
   and lobby only with the accepted session/profile/name/story and verified content.
   Record actual scene/lobby observations; a downloaded byte count alone is not lobby success.

The smoke does not claim login bonuses, attendance, pass rewards, crystals, purchases,
ticket exchanges or draws. The separately approved 11-ticket/1+10-draw budget remains
unchanged and is not seeded or consumed by a lobby test.

<a id="source-e6e0a844347e9bf9-approval-holds-already-recorded"></a>
#### Approval holds already recorded

The publication tool rejected the A05 upload because trusted-user authorization did
not identify the exact proprietary payload and R2 destination sufficiently. Required
scope is the existing A05 packet's 11 public files (85,341,920 bytes), only beneath its
approved release namespace in `lastrail-content-dev`; activation is a separate guarded
write to `lastrail-versions-dev/versions/current.json`. No alternative CLI/channel retry
has been used to bypass that rejection.

Unity's latest handoff separately records rejection of code sending session Bearer
credentials to client-config/authorized. Its exact adapter/token-destination approval
must be resolved through the normal review path by Unity. This server document neither
clears that rejection nor expands DEV testing to PRD, secret storage, deployments or schemas.

<a id="source-e6e0a844347e9bf9-verified-local-evidence-separate-from-remote"></a>
#### Verified local evidence, separate from remote

Server registry suite checkpoint:67 files /394 tests passed,4 TypeScript projects and
lint passed; actual local D1 migration0024, FK clean, quick_check OK, tester grants0.
Those results validate local contracts and guards only. This documentation checkpoint
adds no code changes and did not rerun that suite or claim remote advancement.

<!-- source-body:source-e6e0a844347e9bf9:end -->


<a id="source-b150572e3b350007"></a>
## 지휘관 3종 편성 및 분대장 충전 연출 상세기획 r1

원출처: `WorkspaceRepo/docs/design/combat-command-loadout-and-leader-presentation-20260911.md`  
원문 SHA-256: `a90d40f401c4fa8d767cce2d8962f4571c003f2cba25ddbe3c38438ff6d6677e`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Accepted3chosen~10support direction snapshotordered IDs/catalog/revision, commoncastlock; originalSTARTresponse retained. Localcharge0/4%/100 optinnotprod. User N%only supersedes충전N%; canonicalnamesgap. Nonpausing smallcutin/perbattlefirstuse superseded04§14 full-screenALWAYS/DAILY; skipno effectduplicate/lifecycle preserved.

<!-- source-body:source-b150572e3b350007:start -->
<a id="source-b150572e3b350007-지휘관-3종-편성-및-분대장-충전-연출-상세기획-r1"></a>
### 지휘관 3종 편성 및 분대장 충전 연출 상세기획 r1

<a id="source-b150572e3b350007-explicit-completion-order-prepared-verified-destination-retry-rejected--2026-09-11"></a>
#### Explicit Completion Order Prepared; Verified-Destination Retry Rejected — 2026-09-11

Unity relays user explicit instruction to assign all remaining work to existing owners, implement/verify/QA and report back without repeating completed work. Created docs/ops/combat-hud-completion-assignment-20260911.md with concrete Unity/Direction/Server scopes and changed-battle-only QA acceptance, exact readback verified. Latest commander gauge text is N% ONLY, superseding 충전 N% and 사용 가능 copy. Selected ultimate name/title must share canonical mapping; active names fixed through cooldown, icon radial mask/seconds separate; unlit/100%-lit/consumed-dark frame must be separate layer.100% does not override gameplay input gating.

Common external/window map world-distance projection replaces independent phases; pause/speed/map/restore and depth parallax must stay coherent. Existing hit-tint5PASS is excluded from rework; do not commission character hit animation. Remaining proper attack/down/recovery/rigging, separated turret/wheels, actual selected support3/ownership/catalog and accepted local START are assigned on paper with evidence boundaries. Canonical names remain an identified gap; no invented names authorized. Direction scope is source lookup and explicitly provisional candidates if none found; unrelated implementation need not stop.

Plan used app list_threads and read_thread to check destinations: LAST RAIL_Unity_1/Direction_1/SERVER_1 appear in same local project16d0803d-ef68-4df2-a5c9-0bca3ba8b596. Unity turn items remained unavailable. A single normal send retry to Unity with this new evidence was REJECTED: automatic review stated untrusted same-project listing does not establish ownership/destination-specific disclosure approval. No dispatch succeeded; no Server/Direction relay attempted. All new assignments remain NOT SENT/NOT ACKNOWLEDGED, not active work claims. User-specific approval is requested for these named existing destinations. Prior rejection history preserved; no bypass.

Plan changed documents only; no new runtime tests/code/video, and did not claim task complete. Readback verified; local/uncommitted. Actual next step after destination approval is dispatch exact reviewable assignment and follow owners' real results; source-naming gap and implementation evidence remain open.

<a id="source-b150572e3b350007-hud-skill-labels-canonical-name-mapping-not-located--2026-09-11"></a>
#### HUD Skill Labels: Canonical Name Mapping Not Located — 2026-09-11

Unity relays user HUD corrections: commander title must show selected operator's real ultimate name; charge text only 충전 N%; frame unlit below100 and cyan at100; each active name remains fixed; support icon/text centering and spacing corrected. User correction supersedes earlier generic commander title/ready-badge mockup text where inconsistent. Proposed radial cooldown mask/seconds over icon and brief completion glow are Unity proposals, not reported implemented. Baked-in commander illumination requires separate unlit-frame/light layer.

Plan searched original docs, actual Unity Assets/_LASTRAIL cs/json/csv/asset files and relevant Unity handoff/design text. Directly parsed docs/design/combat-stats-v1.json: kits define effects/timing but no Korean display names. Server ce03-local-producer.ts normalizeAbility generates IDs operatorId.active / operatorId.ultimate (initial op_kyle/op_reina/op_eve/op_sera/op_mira) and no name mapping. docs/narrative/act1-localization-string-id-map.md is narrative/cue planning, not skill-name catalog. 강철의 의지 was found in QA/Design/CombatCandidates-20260911/FINAL-v02.md prompt only and is not proof of an approved canonical name.

No canonical skill-ID→Korean active/ultimate display-name source was located in these searched scopes. This is a concrete content-data gap, not proof that no historical conversation ever named skills. Do not invent names or label prompt text as canonical. Required next content artifact is versioned display-name mapping for exact existing ability IDs with provenance/approval status; separate display data from gameplay snapshot/effect semantics. Once supplied, HUD binds by ID and selected ultimate, retains active names during cooldown, puts countdown/mask on icon.100% lighting does not override down/pause/invalid-target/save-recovery input gating.

No code/runtime tests or name authoring performed. Result has not been transmitted to Unity because earlier automatic-review destination authorization remains unresolved; no retry/indirect relay. This lookup and user corrections are recorded locally only.

<a id="source-b150572e3b350007-user-decision-character-hits-use-tint-not-hit-animation--2026-09-11"></a>
#### User Decision: Character Hits Use Tint, Not Hit Animation — 2026-09-11

Unity relays explicit user removal of character hit-motion requirement. Plan directly checked current Unity/art handoff matching this change and parsed CombatMotion-20260911/hit-tint-tests.xml:5PASS/0FAIL/0SKIP,end14:23:14Z. Owner reports actual0.16 simulation-second red→original tint, no character hit-pose switch or hit overlay, attacks continue. Down/recovery and vehicle hit effects remain. Mirror equality is owner-reported; Plan did not rerun tests/source audit.

Supersedes earlier mandatory character hit-motion/frame/rigging entries in integrated production lists: required character motion is idle/move/attack/skill/down/recovery as applicable; hit feedback remains actual-event tint only. This removes visual reaction animation, not damage resolution or explicitly authored control/interrupt rules. Visual and combat semantics must remain distinct. Duration0.16 is current implementation detail, not separately claimed user numeric approval.

Existing combat-motion/polish/connected videos were recorded before this change and are not tint-only visual evidence. New capture is needed to show current feedback. Historical art/test records remain preserved. No new art/code/test by Plan; task propagation still NOT SENT under earlier automatic-review destination rejection.

2026-09-11 / Plan_2 / 상태: 수락된 방향 반영 + 구현 전 검토용 상세 제안.
Unity 최신 canonical과 전달된 사용자 결정을 확인했다. 이번 산출물은 문서이며 코드·효과·경제·실제 출격 구현 완료가 아니다. 기존 원본과 테스트 이력은 보존한다.

<a id="source-b150572e3b350007-1-확정-범위와-용어"></a>
#### 1. 확정 범위와 용어

- 일반 대원 스킬: 기존 개별 쿨타임 유지.
- 분대장 스킬: 선택한 대원의 별도 스킬. 분대 전투 활동으로 충전하여100%에서 수동 사용. 산업형 A의 푸른 카드·게이지를 사용한다. 기존15초 최초 대기/45초 쿨타임 구현은 새 방향에 부합하지 않는다.
- 지휘관 지원 스킬: 하단 지원 슬롯3개. 약10종 풀에서 중복 없이3개를 출격 전에 고르고 선택 순서대로 표시한다. 전투 중 교체하지 않는다. 각각 개별 쿨타임이며 한 스킬 시전 중 다른 지원 스킬은 공통 시전 잠금.
- 처음10종을 모두 선택 가능하게 조합 검증하는 범위는 초기 검증 구성이다. 영구 해금·구매·성장·재화 정책을 뜻하지 않는다.
- 상단 외부 열차 오른쪽 진행, 하단 아군 LEFT 내3/2열·적 RIGHT, 고정편성 및 실시간 동시 행동은 유지한다.
- 연출 수치와 아래 세부 처리안은 Plan 제안이다. 사용자에게 구체 연출 승인이나 미정 전투 수치 승인을 받았다고 기록하지 않는다.

<a id="source-b150572e3b350007-2-지원-스킬-풀과-대상-처리"></a>
#### 2. 지원 스킬 풀과 대상 처리

아래 명칭은 사용자 수락 후보명이다. 실제 catalog ID를 새로 확정하지 않는다. 기존3종 ID와 신규 후보 ID의 등록·매핑은 버전 명세에 따로 명시해야 한다.

|후보|수락된 역할|제안 입력/표시|구현 전 확정할 항목|
|---|---|---|---|
|지원 사격|외부 단일 집중 포격|유효 외부 적 선택·조준 표시 후 확정|사거리·계수·타격 횟수·대상 소멸 처리|
|방어막|열차 보호막|열차 강조 후 즉시 명령|흡수량·지속·공유 보호막 상한·갱신 규칙|
|긴급 수리|열차 내구도 회복|열차와 회복 예상 범위 강조|회복량·회복 상한·최대HP 상태 허용 여부|
|제압 포격|내부 범위 공격+짧은 행동방해|내부 대상/영역 미리보기 후 확정|범위·피해·방해 종류/시간·면역|
|의료 지원|대원 전체 회복|유효 아군 전체 강조|회복량·대상 생존/복귀중 조건·과회복|
|연막 전개|대원 피해감소|대원 영역에 얇은 연막 범위|감소 유형·지속·중첩/상한·적용 대상|
|약점 스캔|지정 적 받는피해 증가|유효 적 지정·약점 표식|내외부 허용·증가율·지속·기존 mark 중첩|
|화력 증폭|열차/포탑 화력 증가|영향받는 열차 무장만 강조|Combat/Gun 포함 여부·지원 사격 포함·지속·cap|
|전자기 교란|적 시전 방해|방해 가능한 대상과 시전 상태 표시|단일/범위·lane·interrupt 면역·무시전 사용|
|긴급 방호|최저체력 대원 강한 보호막|자동 대상 강조 후 명령|절대HP/HP비율 기준·동률·고정 시점·보호막 정책|

새 역할이 기존 effect와 비슷해도 이름만 매핑해 동작 완료로 보지 않는다. 예: 연막은 회피 확률을 새로 추가한다는 뜻이 아니다. 전자기 교란은 피해/침묵 효과를 자동 추가하지 않는다. 의료 지원은 부활로 확대하지 않는다.

<a id="source-b150572e3b350007-3-편성저장출격복구"></a>
#### 3. 편성·저장·출격·복구

출격 준비에3개 슬롯과 후보 목록을 둔다. 후보 선택→빈 슬롯 배치, 이미 선택된 항목은 선택됨 표시, 순서 변경과 제거를 지원한다.3개 미만이면 출격 불가 사유를 표시한다. 효과·쿨타임·대상 설명은 확정 데이터만 표시하고 미정 값을0으로 표시하지 않는다.

저장은 기존 편성 변경 방식과 일관된 명시 저장을 권장한다. 서버 소유 선택에 별도 지원 편성 revision 및 catalog/rulesVersion을 둔다는 논리 계약을 제안한다. 정확한 필드명·DB 구조는 Server 명세에서 결정한다. 요청에는 스킬ID/slot/expected revision만 받고 효과량·쿨타임·해금 플래그는 클라이언트 권한으로 받지 않는다. 중복·누락·unknown·미지원·slot 불연속과 stale revision을 거부한다.

기존 고정3종 플레이어의 호환 정책은 명시적으로 결정해야 한다. 권장: 최초 새 준비 화면에서 기존 지원 사격/방어막/긴급 수리를 초안으로 보여주고 저장시 검증한다. 과거 출격 snapshot이나 미확인 재시도 body를 변경하지 않는다. 기존 사용자 데이터를 몰래 새로운 선택으로 확정하지 않는다.

출격 준비와 실제 승인 시점에 owner/선택ID·순서/revision/사용가능 상태/catalog 버전을 다시 검증한다. 원자적 출격 snapshot에는 검증된3종의 순서·효과/대상/쿨타임·공통 채널 규칙·버전이 동결되어야 한다. 원래 START 재시도는 원본 응답·snapshot·seed를 반환하며 현재 저장된 지원 편성을 덮어씌우지 않는다.

전투의 선택은 snapshot에서만 읽는다. 계정 준비 편성 변경이 진행 중인 전투에 반영되지 않는다. checkpoint에는3종별 ready 시점, 공통 시전 채널, queued/accepted/applied receipt, 대상 및 확정된 효과 순서가 필요하다. 저장/복구·중복 입력으로 cooldown 초기화나 이중 효과가 생기면 안 된다. UI slot index를 스킬 식별자로 사용하지 않는다.

<a id="source-b150572e3b350007-4-분대장-게이지의-논리-상태-제안"></a>
#### 4. 분대장 게이지의 논리 상태 제안

충전 이벤트·증가량·소모량·이월·다운 정책은 아직 미정이다. r1은 수치를 발명하지 않고 아래 인터페이스와 시각 상태를 정의한다.

게이지 값과 사용 가능 여부를 분리한다. 사용 가능 = 완충 AND 선택 분대장 유효 AND 생존/행동 가능 AND 허용된 시전 상태 AND 유효 대상 조건 AND 소유/세션 유효 AND 저장/복구 오류 없음 AND 비일시정지. 정확한 시전 채널 조건은 확정 계약을 따른다.100%는 자원 표시이지 실행 허가가 아니다.

권장 소비 경계: 대상 미리보기와 취소에는 소비 없음. 명령을 검증·수락하는 시뮬레이션 전이에서 게이지 소비와 receipt를 함께 처리하고, 저장 성공 후 HUD에 확정 표시한다. 저장 실패는 공개 상태를 유지하고 복구 잠금으로 진입한다. 동일 명령 재전송은 기존 receipt 반환, 이중소비 없음. 시전 후 방해/사망 시 환급 여부는 별도 정책이므로 연출이 임의 환급하지 않는다. 비용100% 전량 여부도 미확정이며100% 사용 가능 문구만으로 단정하지 않는다.

확정해야 할 정책: 어떤 아군의 어떤 실제 행동이 충전하는지, 피해0/과회복/지속효과/지원 스킬/자기 궁극기 기여, 이벤트당 양과 상한·초과분, 시작값, 소비량·시점, wave/전투 간 이월, 분대장 down 중 적립/보존, interrupt 환급. 권장 방향은 실제 적용 이벤트만 반영하고 같은 이벤트의 재생·시각 반복으로 재충전하지 않는 것이다. 구체 수치는 별도 로컬 규칙 버전으로 확정한다.

<a id="source-b150572e3b350007-5-완충-버튼-활성화-연출-제안"></a>
#### 5. 완충 버튼 활성화 연출 제안

모든 시간은 연출 초안의 실제 화면 시간이다.1.5/2배 시뮬레이션 속도에 의해 입력 허가나 논리 효과가 달라지지 않는다.

|상태|카드·게이지|음향/진동|입력과 예외|
|---|---|---|---|
|충전중|시뮬레이션 값을100–160ms 보간하는 청록 막대·수치, 테두리는 낮은 밝기|지속 루프음 없음|선택/설명은 가능, 사용 불가. 시각 보간 중100%를 앞서 표시하지 않음|
|완충 순간|180ms 테두리 순차 점등, 뒤이어250ms 작은 내부 입자·충전100% 표시|짧은 전자 확인음1회, 선택적 약한 진동1회|충전 임계 통과 이벤트당1회만. 화면 전체 번쩍임 없음|
|완충+사용가능|사용 가능 배지,2.4초 주기 약한 테두리 밝기 변화|반복음/반복진동 없음|선택 분대장 카드만 강조; 다수 지원 버튼과 경쟁 금지|
|완충+사용불가|100% 수치는 유지, 점멸 중지·잠금과 짧은 사유|없음|전투불능/일시정지/시전중/대상없음/복구필요를 구분|
|누름/대상 선택|80ms 안쪽 눌림, 필요한 대상/영역 프리뷰·취소|가벼운 클릭음|선택 취소는 카드 원상복구, 준비완료 폭발 재생 없음|
|수락 대기|작은 확인중 표시·중복 사용 잠금|확정음은 아직 없음|이 단계에서 게이지가 소비된 것처럼 먼저 비우지 않음|
|저장된 사용확정|120–180ms 실제 잔량으로 게이지 전환·배지 제거|발동 연출의 확정음과 통합|0으로 만드는 대신 authoritative 잔량 표시|

pause에서는 장식 반복과 입력을 정지한다. 분대장 down 시 자원 수치는 실제 정책값 그대로 보여주며 사용 불가로 전환한다. 복귀해 다시 사용 가능해져도 동일 완충 이벤트 축하를 반복하지 않고 짧은 테두리 강조만 권장한다. 재접속/화면 재구성은 저장된 상태를 바로 표시하며 완충음·진동·확정 발동을 재연하지 않는다. 낮은 자극 설정에서는 입자·순간 발광·진동을 제거하고 배지와 테두리 대비만 유지한다.

<a id="source-b150572e3b350007-6-분대장-스킬-사용-연출-제안"></a>
#### 6. 분대장 스킬 사용 연출 제안

권장안은 전체 화면을 가리지 않는 카드 연계 소형 인물 컷인이다. 대안인 전체 화면 컷인은 두 전장의 경고를 가리며, 카드 발광만 사용하면 궁극기 구분이 약하다. 기본은 소형 컷인, 낮은 자극 모드는 카드 강조만 사용한다. 인물 표현은 기존 승인된 리소스를 사용한다.

|단계|제안 길이|연출|논리 경계|
|---|---|---|---|
|입력|0–80ms|카드 눌림·청록 내부광|원시 입력일 뿐, 소비/효과 없음|
|대상 선택|사용자 선택까지|대상 테두리/범위, 부적합 대상에 사유|필요한 스킬만 프리뷰. 선택으로 전투 정지하지 않음|
|사용확정|저장 성공 후0–120ms|카드 테두리 선이 선택 대원 위치와 대상 방향으로 연결|저장된 ACCEPTED receipt를 근거로1회 재생. 피해 발생 뜻 아님|
|발동 준비|약120–400ms|분대장 얼굴·상체 소형 컷인, 화면폭 최대28%·높이18%, 카드 바로 위 아군측에0.28초 노출·0.12초 퇴장|실제 cast 시작/윈드업 이벤트에 동기화; HP/적 시전/대상 표식/지원 버튼을 가리면 자동 생략|
|타격/효과|실제 effect event 시점|대상 중심 VFX·해당 체력/보호막 변화, 짧은 효과음|피해/회복/보호막 적용은 시뮬레이션 이벤트만. 애니메이션 프레임으로 판정하지 않음|
|종료|마지막 주요효과 후150–250ms|연결선·순간광 제거, 카드 정상 상태|시각 종료가 cooldown/채널 해제를 결정하지 않음|

컷인과 전투는 동시에 진행한다. 전체 time-scale 조절·히트스톱·카메라 줌으로 게임 흐름을 멈추지 않는다. 기본 카메라는 고정. 강한 공격의 주 타격에만 내부 전장 범위 최대2px/80ms 미세 흔들림을 선택적으로 허용하고 HUD/외부 전장에는 적용하지 않는다. 방어/지원은 흔들림 없음. 타격 전에 밝은 광원으로 전장 전체를 덮지 않는다.

공격형: 기존 스킬이 실제로 타격하는 대상에 얇은 청록 궤적→짧은 주황/백색 충격을 겹친다. 다중 타격이면 실제 타격 수만큼 약한 피드백, 큰 SFX/진동은 첫 주요 타격1회. 새 광역 피해를 암시하지 않는다.
방어형: 기존 효과의 수혜 대상 윤곽에 청록 보호막 육각 패턴을 짧게 전개하고 유지효과는 얇게 남긴다. 보호막이 없는 아군까지 방어된 것처럼 표시하지 않는다.
지원형: 실제 회복/강화 수혜자에게 아래→위 펄스와 상태 아이콘을 표시한다. 빛기둥/숫자는 실제 변화량과 구분하며, 공격형 폭발·적 경직을 추가하지 않는다.
선택 분대장의 성격은 이름으로 고정하지 않고 동결된 효과 정의와 수혜자·적용 이벤트를 기준으로 연출을 매핑한다.

SFX는 카드 확인음/발동음/주효과음의3층 이내. 음성은 확보된 승인 음성이 있을 때만 선택적으로1회 사용하고 미보유 음성을 전제로 만들지 않는다. 전장 경고음 우선, 겹친 발동음은 수를 제한한다. 진동은 확정 시 약20ms, 강한 주타격30ms의 선택적 초안이며 설정 OFF를 존중한다. 저장 실패·미확정 입력에는 발동 진동 없음.

취소/방해/사망: 수락 전 취소는 프리뷰만 정리. 수락 후 중단은 실제 중단 이벤트를 받아 연결선/컷인을 짧게 닫고 중단 표시, 이미 확정된 효과는 이벤트에 따라 유지한다. 발동 요청 실패를 성공처럼 재생하지 않는다. 복원은 현재 지속효과·시전 진척만 재구성하며 이미 지나간 컷인/일회성 SFX/진동은 재생하지 않는다.

<a id="source-b150572e3b350007-7-담당별-영향과-검증-기준"></a>
#### 7. 담당별 영향과 검증 기준

Unity: 일반 cooldown과 leader gauge 모델 분리, charge event ID/명령 receipt/저장 성공 경계,3개 지원 선택 readmodel 및 전체10종 효과·대상 coverage, 공통 시전 잠금, checkpoint charge/지원 상태, 재접속 무중복 연출, 실제 prefab/PlayMode 가독성 검증. 기존 고정3종·leader cooldown 테스트는 과거 버전 증거로 유지한다.
Server: 지원 편성 저장/revision/catalog·허용 검증, prepare 및 atomic START의 재검증·동결, leader charge policy version, snapshot/replay 및 checkpoint/settlement 계약 일관성, 실제 전체 consumer attestation coverage 갱신. 새 구현 증거 없이 과거 profile hash에 변경을 끼워넣지 않는다.
Direction: 선택 산업형 A 배치 유지,10종 식별 가능한 아이콘/대상·잠금/완충 상태 시각명세, 공격·방어·지원별 기존 리소스 매핑, 화면 경고/HP/대상 표시 가림 확인. 시안에 보이는 이름·수치로 실제 roster/content를 바꾸지 않는다.
Plan: 역할 수락 범위와 미정 수치 분리, 지원 편성·게이지·연출을 하나의 새 버전 변경으로 추적. release 해금·경제는 별도 미승인.

필수 시나리오:3/10중복·순서·stale저장; 출격 중 편성경합; 원본 START 재시도; 개별쿨타임과 공통 잠금;100%이지만 down/paused/no target; 프리뷰 취소; 저장 실패·명령 중복; 충전/소비 직후 재시작; wave 경계·다운/복귀; 여러 효과 같은tick; 느린프레임/1.5x/2x; 낮은 자극·무음; 상하단 적 경고가 컷인에 가리지 않음. 최종 로비→실제전투→결과→명시보상수령→로비 영상에서 새 버전 동작을 검증한다.

<a id="source-b150572e3b350007-8-상태와-전달"></a>
#### 8. 상태와 전달

문서·원본 인수인계 저장/재읽기 검증만 수행. 신규 코드·런타임 테스트·VFX 제작·경제 변경 없음. 구체 연출안은 사용자 검토용이며 승인 완료 아님. 이전 자동 검토가 Server/Unity 목적지별 정보 전달 승인을 거부했으므로 이번 변경의 외부 작업 전파도 아직 미전달이다. 다른 역할을 중계로 사용하거나 거부된 메시지를 우회하지 않았다.
<a id="source-b150572e3b350007-9-사용자-수락-추가-설정전투-연출-모드건너뛰기"></a>
#### 9. 사용자 수락 추가: 설정·전투 연출 모드·건너뛰기

2026-09-11 Unity 전달: 사용자가 설정/연출 스킵 제안에 계속 진행을 승인했다. 아래 옵션·진입 방향은 수락 내용이며, 기본값·저장범위·적용 시점·첫 사용 기록 복구는 아직 미정이다. Unity는 설정 게임 탭/전투 일시정지 이미지2장과 흐름 문서를 제작 중이라고 보고했다. 이 기록은 이미지 완성·런타임 구현 증거가 아니다.

<a id="source-b150572e3b350007-연출-모드"></a>
##### 연출 모드

|모드|표현|항상 유지|
|---|---|---|
|전체 연출|분대장 컷인·카드 연결·카메라 강조와 발동/타격 표현|위험·대상·핵심 전투상태|
|간소화|컷인·카메라 강조 생략, 발동과 실제 타격/효과 피드백 유지|동일한 위험·대상·실제 효과 시점|
|첫 사용만 전체|한 전투에서 분대장 첫 사용은 전체, 이후는 간소화|동일한 입력·판정·상태 표시|

모드는 시각 표현만 변경한다. 스킬 수락/시전/타격시간, 피해·회복·보호막, 게이지 적립·소비, cooldown, 명령 receipt, 실제 전투 진행은 동일해야 한다. 연출 길이가 줄어도 다음 행동이 일찍 가능해지지 않는다. 연출에 의해 정지하지 않는 기존 실시간 전투 원칙을 유지한다.

연출 중 표시하는 '연출 건너뛰기'는 현재 장식 컷인/카메라 강조만 종료하고 라이브 전장으로 돌아온다. 스킬 취소·전투 시간 건너뛰기·타격 즉시 실행·게이지 환급 버튼이 아니다. 이미 진행 중인 실제 cast와 pending impact는 원래 순서로 계속된다. 위험 경고·대상 표식·발동/타격 피드백은 삭제하지 않는다. 여러 번 눌러도 같은 연출 종료만 수행한다.

권장 구현 경계: presentation controller가 현재 연출 토큰을 닫고 카메라/광원 임시 상태를 원복한다. command/시뮬레이션 시간을 조작하지 않는다. 옵션 변경·skip은 발동 명령과 별도 입력이며 전투 입력 중복방지 receipt를 새로 만들거나 재발행하지 않는다. 가려지는 경고가 있다면 skip 버튼보다 우선해서 보이게 한다.

<a id="source-b150572e3b350007-설정-정보-구조-및-흐름"></a>
##### 설정 정보 구조 및 흐름

- 게임: 전투 연출 모드, 화면 흔들림, 섬광.
- 사운드: 전체/BGM/SFX/음성. 실제 음성 리소스 미보유를 설정 존재로 보완했다고 보지 않는다.
- 화면: 화질/FPS. 지원하지 않는 조합을 실행 가능한 것으로 표시하지 않는다. 정확한 단계와 기기별 지원 값은 별도 확정한다.
- 계정: 기존 계정 연결/로그아웃 동작 재사용. 전투중 안전하게 처리할 수 없는 동작은 기존 계정/복구 계약에 따라 제한하고 사유를 표시한다.

로비 메뉴→설정→닫기는 원래 로비 화면으로 돌아간다. 전투의 일시정지 메뉴→설정→닫기는 원래 메뉴로 돌아가며 자동 전투 재개로 해석하지 않는다. '계속하기'는 기존 권위 정책에 따라 별도로 처리한다. 설정창을 열었다는 이유로 온라인 전투가 멈췄다고 표시하지 않는다. 온라인에서 일시정지가 허용되지 않으면 메뉴/설정 중에도 전투가 진행된다는 상태를 명확히 표시하며, 서버 또는 공동전투 clock을 로컬 설정으로 멈추지 않는다.

'로비로 나가기'는 기존 복귀/정산/미확인 요청 복구 흐름에 연결한다. 설정/메뉴 UI가 승리·패배·포기·보상 청구를 새로 결정하지 않는다. 화면을 나가도 run 상태/저널을 삭제하지 않으며, ACTIVE/종료미확인/REWARD_REVIEW 등 실제 상태별 기존 허용 경로와 안내를 사용한다. 명시 보상 수령 원칙은 그대로 유지한다.

<a id="source-b150572e3b350007-저장첫-사용복구의-미정-항목과-권장안"></a>
##### 저장·첫 사용·복구의 미정 항목과 권장안

아래는 아직 수락되지 않은 세부 제안이다. 임의 기본값을 승인값으로 기록하지 않는다.

|항목|결정 필요|권장 검토안|
|기본값|전체/간소화/첫 사용 중 최초값|첫 사용만 전체를 후보로 비교하되 자동 채택하지 않음|
|적용 시점|현재 연출/다음 연출|다음 연출부터 적용; 현재 연출은 명시 skip으로 축소, 이미 간소화된 연출을 소급 재생하지 않음|
|저장 범위|기기/계정/클라우드 동기화|표현·음량·화질은 기기 로컬을 우선 검토, 계정간 공유 여부 명시. 계정 권한/게임 밸런스 데이터와 분리|
|첫 사용 소비 시점|입력/수락/재생시작/효과|저장된 사용확정 뒤 전체 연출 시작 시1회 기록하는 안. 입력 취소·실패는 첫 사용을 소모하지 않음|
|기록 식별자|전투/분대장/버전|안정적인 run 식별자와 선택 분대장/연출버전으로 구분; 새 전투 시작과 동일run복구를 구분|
|재접속|첫 연출 반복 여부|같은run에서 이미 시작한 전체 연출은 반복하지 않고 지속 상태만 복원. skip도 사용한 첫 연출로 처리하는 안|
|저장 실패|옵션/첫사용기록 실패|전투 판정은 영향 없음. 표현 저장 실패와 전투 checkpoint 실패를 구분하고 무한 팝업/연출 반복을 방지|

표현용 first-use marker는 전투 효과/게이지/정산 권한을 갖지 않는다. 다만 같은run 재연 억제와 계정 전환에서의 혼동을 막도록 저장·복원 계약을 정해야 한다. 설정 변경 때문에 기존 전투 snapshot의 규칙 hash나 accepted START 원문을 바꾸지 않는다.

<a id="source-b150572e3b350007-추가-검증-및-담당-영향"></a>
##### 추가 검증 및 담당 영향

Unity: 동일 command stream에서3모드/skip 유무에 관계없이 전투 이벤트·최종상태·게이지·쿨타임이 동일한지 검사. skip 반복·타격 직전skip·다운/방해 중skip·저장복구·재접속·모드전환을 검증. 카메라/일시광원 원복, 위험/대상 표시 유지, 메뉴 닫기와 전투 재개 분리, 온라인 진행 표시 확인.
Direction: 산업형 A에 맞는 게임 설정/전투 메뉴 시안,3모드의 비교 설명, skip 버튼과 위험 경고가 겹치지 않는 배치. 정적 이미지가 실제 일시정지/저장/재생 구현 증거는 아니다.
Server: 온라인 권위/기존 계정·복귀·정산 경계를 유지. 표현 옵션을 gameplay effect나 보상 허가 필드로 취급하지 않는다. 계정 동기화 선택 시에만 별도 저장 계약을 명시한다.
Plan: 기본값·범위·적용 시점·first-use 복구 결정을 추적. 사용자 수락 옵션 구조와 미정 세부 제안을 분리. 기존 전체전투 QA 목표를 설정 시안 완료로 대체하지 않는다.

전파 상태: 원본 통합 문서 반영만 완료. 앞선 목적지별 메시지 자동 검토 거부가 해소되지 않아 관련 작업으로 새 메시지를 보내지 않았으며 우회 전달하지 않았다.
<a id="source-b150572e3b350007-10-세-연결-후-qa-및-명시적-로컬-충전-시험-규칙"></a>
#### 10. 세 연결 후 QA 및 명시적 로컬 충전 시험 규칙

2026-09-11 Unity 전달 사용자 지시: 승인 산업형A UI, 판정→공격/피격/다운 VFX, 분대장 게이지/발동을 모두 연결한 다음 QA한다. 기존 diagnostic 영상은 시작 상태 기록이며 이 목표 완료 증거가 아니다.

Unity가 구현용으로 명시한 opt-in 로컬 QA 규칙을 시험 입력으로 추적한다. 사용자에게 각 수치를 개별 승인받았거나 Server 정식 규칙으로 채택됐다고 주장하지 않는다. 이 절은 production profile을 변경하는 계약이 아니다.

- 초기0%, 살아 있는 대원의 유효 기본공격으로 실제 HP 피해가 발생한1회당4% 충전.
-100% 상한. 수락 시100% 소비. 시간 경과만으로 충전하지 않음.
- wave 간 보존, 새 전투는 초기화.
- 분대장 다운 중 기존 게이지 보존, 다른 살아 있는 대원의 적격 공격으로 충전 가능.100%여도 다운이면 사용 불가.
- 스킬/궁극기 효과와 HP 피해0은 충전에 기여하지 않음. 보호막만 깎아 HP 피해0이면 제외한다는 조건이 이 문구에 해당한다.
- 별도 opt-in config/version과 checkpoint config fingerprint에 포함할 예정이라고 Unity가 보고했다. 실제 포함 여부·코드·테스트 결과는 아직 미검증.

정확한 구현 경계 확인 필요: '1회'는 authoritative damage event를 기준으로 중복 기여하지 않아야 하며, 살아 있음 판정이 source-death committed impact의 어느 시점인지 고정해야 한다. 기본공격 multi-hit별 처리·분대장 자기 기본공격·동일tick charge/consume 순서·시전 중단 후 환급·저장 실패 재시도는 실행 규칙과 테스트로 명확히 해야 한다. 이 문서는 미정 환급 정책을 임의로 확정하지 않는다.

필수 QA: 최초0/적격25회→100,24회→96; 상한 초과 방지; shield-only/0피해/스킬/궁극기/시간만 경과 제외; 다운100% 사용 불가와 타대원 적립; wave 이월·새run 초기화; 동일 이벤트 재생/동일명령 retry 무중복; 소비 직전·직후 restore; config 다른 checkpoint 거부. 수락·소비·저장 성공·연출 게시 순서와3가지 연출 모드/skip의 전투 동일성도 검증한다. 25회는 이 QA 수치의 산술 기대치이며 실행 테스트 통과가 아니다.

기존 유효 기본공격·피해·사망·복구 의미를 변경하는 경우에는 독립 버전으로 기록한다. 서버 승인 START와 다른 test seed/profile을 섞지 않고 현재 로컬 시험임을 영상에 명시한다. 지원10중3/설정/실제lifecycle 등 별도 미연결 범위는 영상 결과와 함께 남긴다.

Unity는 clean background 신규 생성 및 기존 sprite/effect 재사용 연결 중이라고 보고했다. Plan은 신규 자산을 직접 검사하거나 코드를 실행하지 않았다. 관련 작업으로 새로운 메시지는 전송하지 않았으며 기존 자동 검토 거부를 우회하지 않았다.

<!-- source-body:source-b150572e3b350007:end -->


<a id="source-7dd86e35b7c512fd"></a>
## 전투 세부 규칙·연출 상세기획 v1

원출처: `WorkspaceRepo/docs/design/combat-detail-and-presentation-v1-20260912.md`  
원문 SHA-256: `531d27957bf154895a9a8d28ff3cf61063b7c0224fbdb5b28c7f74daaae0fa90`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Later §13 updates earlier pending states: names10 supplied combat-display-ko.plan3.20260912.001, new7 support numbers adopted LOCAL separateversion only notprod. Preserve target/basis/effect capability gaps, per-effect positive typedresults and eventcursor. Smallcutin/perrunfirstuse/sharedinstall defaults superseded04§14; local4%charge notglobalapproval.

<!-- source-body:source-7dd86e35b7c512fd:start -->
<a id="source-7dd86e35b7c512fd-전투-세부-규칙연출-상세기획-v1"></a>
### 전투 세부 규칙·연출 상세기획 v1

2026-09-12 / LAST RAIL_Plan_3 / 사용자 요청: 작업 진행 지시 및 전투 상세·연출 기획 진행.

이 문서는 기존 실시간 전투와 승인 산업형 A 화면을 구체화한다. **기존 승인 유지**, **Plan이 이번에 정한 로컬 구현 기준**, **수치 검토안**을 구분한다. 작성 자체는 구현·테스트·미술 승인·출시 완료가 아니다. 현재 CE03 r6 연결 작업은 계속하며 이 문서를 이유로 기존 계약을 다시 작성하지 않는다.

<a id="source-7dd86e35b7c512fd-1-적용-범위와-문서-우선순위"></a>
#### 1. 적용 범위와 문서 우선순위

- 고정 편성: 내부 아군 LEFT의 뒤3/앞2, 적 RIGHT. 상단 외부 열차 오른쪽 진행. 내부/외부 전장을 함께 읽을 수 있어야 한다. 전투 중 대원 이동·편성 교체 없음.
- 독립된 자동 기본공격, 대원별 액티브, 선택 분대장 궁극기 하나, 저장한 지원3종 수동 입력. 타깃 선택·스킬 컷인으로 다른 전투원의 시간이 멈추지 않는다.
- 현행 로컬 runtime 기준은 고정 Server `682e44bbbb3ef06889c2761ded59e687a325353a`의 r5/r6/v3 계약과 실제 Unity 구현이다. 문서의 예시보다 accepted run의 profile·command·content·checkpoint가 우선한다.
- 초기5명/첫 작전의 이미 채택된 로컬 subset과 `basic-hp-hit-v1`은 다시 승인 대기로 되돌리지 않는다. 기존 문서의 최초15초/45초 궁극기 쿨타임은 charge opt-in 경로를 설명하지 않는다. 역사적 다른 경로는 보존한다.
- 새 지원7효과는 §6의 검토안이다. 현재 r6에 추가하거나 executionAvailable=true로 바꾸는 지시가 아니다. 별도 버전 계약과 실제 효과 검증 후 채택한다.
- 실제 r6 parser/loader/runner 개발은 진행 중이다. 작성 도중 Ce03PresentationManifest.cs/Loader.cs 파일 존재를 관측했으며 이를 아직 미작성으로 고정하지 않는다. 작성 완료·실제 연결·검증 여부는 담당 결과로 갱신한다.

기준 문서: [기존 지휘/분대장 기획](51_UI_ART_DETAIL_CONTRACTS.md#source-b150572e3b350007) §9–10, [승인 입력 방향](44_COMBAT_DETAIL_CONTRACTS.md#source-716afa118bd7a8bb), [입력 상세안](46_COOP_DETAIL_CONTRACTS.md#source-cb160e8c21eae28f), [적·웨이브 제안](44_COMBAT_DETAIL_CONTRACTS.md#source-354d63ae7f0c9117), [기존 수치 출처](../../../WorkspaceRepo/docs/design/combat-stats-v1.json), [전반 감사](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-e0340f30439ecb459439).

기존 문서 상단의 미전송/승인 대기, HUD 미구현, 분대장 전부 미정 문구는 최신 인계와 충돌하면 역사로 해석한다. 이번 문서는 그 역사나 고정 accepted profile을 덮어쓰지 않는다.

<a id="source-7dd86e35b7c512fd-2-플레이어가-읽고-판단하는-순서"></a>
#### 2. 플레이어가 읽고 판단하는 순서

한 전투에서 반복하는 판단은 ‘위협 대상 확인 → 기본공격 집중 여부 → 방어/회복/방해 중 개입 → 실제 결과 확인’이다. 작은 일반 타격마다 수동 반응을 요구하지 않는다. 주된 선택은 서로 다른 대원의 개별 액티브와 공통 지원 채널, 분대장 게이지를 언제 사용하는가다.

| 순간 | 반드시 보이는 정보 | 가능한 판단 | 장식의 제한 |
| --- | --- | --- | --- |
| 적 입장 | 내부/외부 위치, 대상 가능 여부, wave | 유효해진 적 집중, 아군 상태 점검 | 등장 이동이 판정 시작을 지연시키지 않음 |
| 평상 교전 | 열차/대원 HP·보호막, 액티브 쿨타임, N% | 집중 대상을 바꾸거나 스킬을 보존 | 작은 피해 숫자가 상태 표시를 덮지 않음 |
| 위험 예고 | 공격원·고정 대상·남은 시간·방해 가능 여부 | 방해/보호/처치 중 대응 | 컷인·대사·숫자보다 경고 우선 |
| 효과 적용 | 실제 HP/보호막/상태 변경과 적용 대상 | 대응 성공·실패를 확인 | 공격 아닌 효과에 폭발/피해를 암시하지 않음 |
| wave 사이 | 다음 wave, 남은 HP·자원·다운 복귀 | 기존 계약에서 허용된 회복/방어 | 자동 완치·쿨타임 리셋 없음 |
| 전투 종료 확인 | 결과 확인중/실제 결과·보상 상태 | 기존 재조회 또는 명시 수령 | 미확인 결과를 승리/패배로 단정하지 않음 |

내부 적을 탭한 집중 요청은 다음 유효 기본공격의 대상 선택에만 영향을 준다. 이미 확정된 공격·발사체의 방향을 바꾸지 않는다. 외부 집중은 별도다. 한 전장의 무효 대상 때문에 다른 전장을 멈추지 않는다.

<a id="source-7dd86e35b7c512fd-3-입력과-버튼-상태"></a>
#### 3. 입력과 버튼 상태

아래는 구현 대조 기준이다. 기존 엔진의 실제 receipt/queue 명칭에 대응시키며 새로운 서버 필드명으로 가정하지 않는다.

| 상태 | 표시와 입력 | 자원·효과 처리 |
| --- | --- | --- |
| 준비 | 고정 스킬명, 사용 가능한 아이콘 | 탭은 자가/자동대상 명령 또는 대상 프리뷰 |
| 대상 선택 | 해당 스킬의 유효 대상·범위, 취소 | 전투 계속. 아직 비용·게이지·쿨타임 변화 없음 |
| 확인중 | 중복 탭 잠금과 확인중 표시 | 성공 연출 금지. 미확인 요청은 원본 명령 ID 유지 |
| 대기 접수 | 현행 엔진이 queue를 지원할 때만 대기 표시 | 접수를 시전·효과 적용으로 오인하지 않음 |
| 시전 확정 | 실제 시전 상태, 원본 receipt | 비용/쿨타임은 동결된 규칙의 경계에서만 변경 |
| 쿨타임 | 이름 고정, 아이콘 부채꼴·남은 초 | 0초 표시만으로 사용 허가하지 않음 |
| 사용 불가 | down/대상없음/지원시전중/일시정지/복구필요 구분 | 유효성을 만족하기 전 효과 없음 |

입력 우선순위는 메뉴·명시 버튼 → 현재 스킬 대상 선택 → 일반 집중 요청이다. 스킬 대상으로 적을 탭하는 동시에 집중 명령을 보내지 않는다. 설명 길게 누르기 종료가 뒤늦은 시전 탭으로 이어지지 않는다. 프리뷰 취소·화면 밖 release·OS 입력 중단에는 명령을 만들지 않는다.

대원별 시전은 독립이다. 지원3종은 현행 `SHARED_TRAIN_MANUAL_CAST` 채널만 공유하며, 지원 하나가 시전 중이라고 대원 기본공격·액티브·적 행동까지 잠그지 않는다. 쿨타임 동안 누른 요청을 자동 예약하여 준비 즉시 발동시키지 않는다. 미지원 queue/취소 API를 UI만으로 새 기능처럼 만들지 않는다.

<a id="source-7dd86e35b7c512fd-대상-결정과-무효화"></a>
##### 대상 결정과 무효화

- 수동 단일 대상은 선택 ID를 검증한다. 시전 전에 무효가 되면 다른 적에게 몰래 발동하지 않는다.
- 자동 최저 HP는 비율 비교, 동률은 안정적인 편성 slot 순서, 마지막 동률은 entity ID 순서를 신규안 기준으로 한다. 실제 기존 kit가 다른 tie-break를 갖는다면 기존 계약을 보존하고 차이를 명시한다.
- 집단 효과는 실제 시전에서 확정된 대상 목록만 강조한다. 시전 이후 새로 생긴 대상을 시각적으로 추가하지 않는다.
- 생존자 회복은 down을 부활시키지 않는다. 단독 회복/수리는 유효 회복 대상이 없으면 낭비 시전을 막는 안을 기준으로 검토한다. 복합 스킬은 보호막·버프 등 한 효과라도 유효하면 ‘회복량0’만으로 전체를 무효화하지 않는다.
- 적용 대상 소멸·방해·면역의 결과는 실제 효과 receipt로 보여준다. 연출이 임의 재조준/환급/추가 피해를 하지 않는다.

<a id="source-7dd86e35b7c512fd-4-판정연출저장-경계"></a>
#### 4. 판정·연출·저장 경계

전투 시간은 기존 simulation clock, UI 장식 시간은 presentation clock을 사용한다. 배속이 바뀌면 windup/impact/down 복귀와 연동된 포즈는 simulation 진척을 따른다. UI 컷인의 입퇴장 길이는 표시 설정이며 전투 시간을 변경하지 않는다. 합법적인 solo pause에서는 전투와 진행 중 전투 장식도 멈춘다. 네트워크 전투 메뉴는 권위 clock을 멈추지 않는다.

연출이 받아야 하는 의미 단위는 다음과 같다. 실제 필드 이름은 Unity 기존 이벤트에 매핑한다.

| 의미 | 필요한 식별/상태 | 연출 | 금지 |
| --- | --- | --- | --- |
| 시전 확정 | run/명령/시전자/대상/시작·예정효과 시각 | 포즈 준비·카드 연결 | 원시 탭을 성공으로 처리 |
| 타격·회복·흡수 | 유일 이벤트/원인명령/대상/실제 변화 | 해당 VFX·수치·음향 | animation callback으로 효과 실행 |
| 경고 | 행동/원천/대상/시작·확정시각/방해 가능 | 표식·진척 | 로컬 컷인 길이만큼 경고 지연 |
| 방해·취소 | 행동/실제 사유/남은 시전 | 예고·준비 종료 | 이미 적용한 피해 취소 |
| down·복귀 | 대상/상태/실제 복귀시각 | down 포즈·남은 시간·복귀 | 모두 down을 열차 패배로 추정 |
| 종료 | 확정 simulation 종료/서버 lifecycle | 확인중→결과 | 로컬 영상 끝을 승리로 처리 |

표시용 event ID/재생 cursor는 이미 지나간 컷인·타격음·진동을 복원 때 반복하지 않도록 한다. 기존 전투 journal 외에 또 다른 게임 명령 송신자가 생기지 않게 한다. 표현 marker 저장 실패는 전투 checkpoint 실패와 별도다. 전투 저장 실패는 기존 복구 잠금을 유지하고, 표현 실패는 장식을 줄여도 실제 전투 상태를 바꾸지 않는다.

동일 tick의 피해/회복/방해/종료 순서는 현재 고정 엔진을 따른다. 문서·시각 우선순위로 치유를 먼저 처리하거나 보스 사망을 우선 처리하지 않는다. 신규 규칙이 필요하면 별도 simulation 버전과 기대결과를 검토한다.

<a id="source-7dd86e35b7c512fd-5-분대장-게이지-및-모드--이번-plan-로컬-구현-기준"></a>
#### 5. 분대장 게이지 및 모드 — 이번 Plan 로컬 구현 기준

현행 opt-in 기준은 초기0, 생존 대원의 유효 BASIC 실제 HP 피해 이벤트당4, cap100, 수락 시100 소비, wave 이월, 새 run 초기화다. down 중 기존 게이지 보존·다른 생존 대원 적립, down 사용 불가를 유지한다. shield-only/스킬/궁극기/시간경과를 충전원으로 늘리지 않는다. 같은 이벤트 재생은 적립하지 않는다. 소스 사망과 다중타격 같은 경계는 현재 코드·테스트를 확인하며 여기서 몰래 바꾸지 않는다.

텍스트는 **N%만** 유지한다. ‘충전 N%’, ‘사용 가능’ 배지를 다시 추가하지 않는다. 명칭은 실제 canonical mapping만 쓰고 미확정이면 기존 ‘명칭 미정’을 유지한다. 테두리는100%에서 독립 점등, 소비하면 꺼진다. 완충이라도 권한·상태가 불가하면 잠금 사유를 보여준다.

기존 §9의 미정 옵션 중 아래는 Plan이 이번 로컬 구현 기준으로 선택한다. 사용자가 과거 이 세부값을 승인했다고 기록하지 않는다.

| 항목 | 이번 기준 | 이유 |
| --- | --- | --- |
| 초기 연출 모드 | 첫 사용만 전체 | 첫 인물 소개를 보존하고 반복 교전을 가리지 않음 |
| 전체 | 카드 연결+소형 인물 컷인+실제 효과 | 두 전장을 유지. 전체 화면 영상/강제 정지 없음 |
| 간소화 | 카드 짧은 강조+실제 효과 | 경고·대상·HP·실제 적용 피드백은 동일 |
| 설정 변경 적용 | 다음 발동부터 | 진행 중 컷인 확대·재시작 없음. 현재 컷인은 skip |
| 저장 | 기기 로컬 표현 설정 | 계정 동기화용 새 서버API를 선행조건으로 만들지 않음 |
| 공유 범위 | 해당 설치의 계정 간 표현/음량 설정 공유 | 소유권·진행·전투 데이터와 분리. 계정 전환 UI와 구별 |
| 첫 사용 기록 | owner/run/선택leader/표현버전으로 분리 | 새 run과 같은 run 복원 구분 |
| 첫 사용 소모 | 저장된 시전 확정 후 표현 토큰을 시작할 때 | 입력 취소·실패는 소모하지 않음. 실제 첫 시전이 핵심경고로 간소화되어도 그 기회를 소모해 뒤늦은 컷인 폭주 방지 |
| skip | 현재 장식만 즉시 축소, 첫 사용 소비 유지 | 효과 조기실행·쿨타임 단축·환급 없음 |
| marker 저장 실패 | 해당 run 메모리 억제, 복원 불확실 시 간소화 | 전투 실패로 취급하지 않고 반복 컷인 억제 |
| 소유 전환/종료 | 이전 run marker 정리, 이전 계정 전투 노출 금지 | 게임 journal 삭제와는 별도 |

첫 사용 marker는 gameplay snapshot/hash에 넣지 않는다. 현재 연출 중 모드를 변경해도 미래 발동에만 적용한다. 낮은 자극은 컷인 장식·섬광·흔들림·진동을 줄이지만 방해 가능/대상/위험의 도형·텍스트는 보존한다. 신규 로컬 기본값은 흔들림 OFF, 섬광 OFF, 진동 OFF로 둔다. 기존 사용자의 저장값은 존중한다. 이 설정은 미술 품질 검증 완료를 뜻하지 않는다.

<a id="source-7dd86e35b7c512fd-6-신규-지원7효과--명시적-수치-검토안-v1"></a>
#### 6. 신규 지원7효과 — 명시적 수치 검토안 v1

현행 실행3종 `support_fire/barrier/emergency_repair`는 기존 profile을 재사용한다. 아래7개 `ce03.role.*`는 기존 저장용 기술 키이며 정식 표시명을 새로 승인하지 않는다. **아래 값은 Plan이 이번에 제안한 opt-in 비교시험 입력**이다. 출시 수치·현재 r6 규칙이 아니다. Server capability 검토 → 동결된 신규 전체 profile → Unity 실제 결과값 검증을 거쳐 채택한다. 기존 계약에 몰래 값을 넣지 않는다.

공통 검토 기준: 무작위 명중/회피/치명타 추가 없음, 지원은 분대장 충전원 아님, 독립 cooldown+공통 지원 시전 채널, 중복 command는 무효과. 시간은 simulation ms, 비율은 bps(10000=100%). 신규7종 비교시험의 firstReadyMs=0/recoveryMs=250을 제안하며 cooldown은 시전 시작 기준, 수락 전 거부는 무소비, 시작 후 불발/중단은 임의 환급 없음으로 시험한다. shared 채널 해제는 실제 시전+recovery 종료에 맞춘다. 현재 engine과 달라지는 부분은 신규 버전 항목으로 검토하며 기존 r6에는 소급하지 않는다. 수치 변경은 이 표의 revision으로 추적한다.

| 기술 키 접미사 | 목적/대상 | 1차 비교시험 값 | 대상·중첩·예외 | 실제 검증 기준 |
| --- | --- | --- | --- | --- |
| suppressive_barrage | 내부 적 집단 약피해+짧은 방해 | train ATK120%씩 최대3, windup500, CD28000, stun500 | 내부 유효 적을 현재 focus→spawn순서로 최대3 확정. 외부 타격 없음. 면역 대상의 stun만 거부, 피해는 자체 유효성대로. cast후 대상 교체 없음 | 각 피해/방해/면역·목록 고정, 다른 lane 피해0 |
| medical_support | 생존 대원 전체 회복 | 각 대상 maxHP15%, windup350, CD30000 | caster 대원 지정 없이 지원 원천. missingHP cap, down/복귀중 제외, 전원 full이면 무효과 시전 거부안 | 각 실제 회복량, 과회복0, 부활0 |
| smoke | 생존 대원 받는 일반피해 감소 | 20%, 6000, windup300, CD30000 | 확률회피/열차감소 없음. 같은 카테고리는 최강값만, 재사용은 해당 source 만료 갱신. mode 고유 기믹피해를 변경하지 않음 | 일반피해 변화·expiry 경계·중복적용 없음 |
| weakness_scan | 지정 적 받는 일반피해 증가 | 15%, 6000, windup300, CD24000 | 내부/외부 중 한 유효 적 지정. 기존 mark와 같은 카테고리는 최강값, 더 약한 효과가 강한 값을 낮추지 않음. 원천별 만료 유지 | mark 교체·만료·피해단계1회·추가피해 자체0 |
| firepower_boost | 열차 기본무장·Combat/Gun 장치 강화 | 15%, 6000, windup300, CD30000 | 대원/지원사격/회복수리 제외안. 기존 train_damage_buff의 출처별 합산·outgoing cap 유지, 같은 상태키는 약화 교체 금지. 적용 출처 확장이 필요 | 포함/제외 출처별 공격결과·expiry·복원 |
| electronic_disruption | 적 시전·채널 중단 | 직접피해0, windup200, CD24000 | 내부/외부 한 대상. interruptible 현재 행동만. 무시전·면역이면 새 시전 거부안, windup 중 행동 소멸은 이미 시작한 시전의 불발로 기록. stun/침묵 추가 없음 | commit 직전/직후·채널 다음 pulse·면역·피해0 |
| emergency_protection | 최저HP비율 생존 대원 보호막 | 대상 maxHP25%, 5000, windup250, CD28000 | 시전 시작 최저비율→slot→entityId 확정, 이후 무표시 교체 없음. 기존 source/ability/effectIndex layer와 약화 교체 무시·동강도이상 만료갱신, 공유 cap40% 적용안 | 동률/전원down/대상소멸/흡수순서·실HP불변 |

검토용 강도 산술: 의료15%는 missing30%인 아군에게15% 회복, missing5%면5%만 회복. 연막20%의 감쇠 전 해당 단계 피해100은80. 스캔15%의 해당 단계 피해100은115. 보호막 maxHP1000인 대상은250 후보. 제압포격 train ATK60이면 대상별72 후보(최대216, armor 전). 이는 계산 예시이며 실행 PASS가 아니다.

<a id="source-7dd86e35b7c512fd-server-실제-capability-감사-반영"></a>
##### Server 실제 capability 감사 반영

[지원7효과 원천 감사](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-591a01f4711af979c34c)를 직접 읽어 아래 차이를 명시한다. 감사는 당시 소스의 정적 관측이며 최신 runner PASS를 대신하지 않는다.

| 실제 현재 동작 | 이번 후보가 요구하는 차이 | 후속 처리 |
| --- | --- | --- |
| 지원 caster=train; heal CASTER_ATK=trainATK, shield CASTER_MAX_HP=trainHP | 의료15%·방호25%는 대상 대원의 최대HP 비율 | TARGET_MAX_HP와 명확한 target당 계산을 새 basis로 정의. trainHP 비율로 잘못 대체 금지 |
| target은 제한된 enum, RequestSkill은 독립 targetId 없음 | 제압 내부 최대3/focus우선, 스캔·교란 양lane 독립 지정 | 새로운 selector/명령 target 계약·journal/checkpoint 필요. 기존 focus 변경을 숨은 대체로 쓰지 않음 |
| 기본 대상 전멸 시 commit 재선택/재windup 가능, override는 commit에 선택 | 후보의 시전 시작 target pin/소멸후 무교체 | 기존 kit 유지, 신규 지원의 target 정책을 별도 버전에서만 추가 |
| 9효과 집합에 stun/피해감소 없음 | 제압stun500·연막20% | 새 effect/state/checkpoint/실제 vectors 필요. 기존 interrupt에 duration 추가하거나 회피로 대체 금지 |
| interrupt는 미래 basic attacks만 취소; 이미Due 보존 | 교란은 interruptible skill/channel도 중단 | 취소가능 action의 상태/commit/pulse·면역 의미 명세. 이미 committed 효과 보존, 같은tick 순서 유지 |
| train_damage_buff는 train 원천에만, 활성상태 합산+cap | Combat/Gun 포함·지원사격 제외 | damage source category 분리. 기존 package Gun 보너스와 혼동 금지 |
| mark strongest→cap2000bps, shield 공유cap4000bps, outgoing cap3000bps 관측 | 스캔15%, 방호25%, 화력15%와 상호작용 | 이번 비교시험은 기존 cap 유지. 다른 source mark는 최강 유지, shield cap 여유만 적용, outgoing 합산cap 유지 |
| 기존 template manual은 정확히3개, server/client 실행 allowlist는3종 | 10중3의 신규효과 실행 | catalog에서 selected3를 먼저 resolve→template 투영. manual10행을 그대로 주입하지 않음 |

연막의 신규 감소는 일반 피해에서 기존 outgoing/mark/armor 처리 후·shield 흡수 전에 한 번 적용하는 안이다. 같은 연막 카테고리는 strongest, 비교시험 cap30%, source별 만료를 보존한다. mode 고유 고정 기믹피해는 제외하며 ‘armor 증가’로 바꾸지 않는다. 기존 engine이 이 단계를 지원하지 않으므로 별도 pipeline/version 및 숫자 회귀가 필요하다.

의료/방호는 down·복귀중 비생존자를 제외하되 실제 복귀 완료 후 보호중인 생존자는 포함하는 후보 기준이다. 상태 marker만으로 회복 보호시간을 늘리지 않는다. 교란은 면역규칙을 우회하지 않고 실제 interruptible action이 없으면 준비 중 사용불가, 시전 시작 이후 대상 행동이 먼저 끝난 경우 기존 소비 유지·불발로 처리하는 후보 기준이다.

미채택 기준: 위 차이를 단순 필드명 변환으로 출시하지 않는다. 구체 basis/selector/effect/action취소 계약, 실제 효과 회귀와 source/catalog/profile/checkpoint/prepare 버전 재고정 후 다음 채택 단위를 연다. 이 문서는 제압을 임시 front2+기본공격 interrupt로 낮춰 완료 처리하지 않는다. 이미 적격인3종 r6 왕복은 이를 기다리지 않는다.

<a id="source-7dd86e35b7c512fd-7-기본-공격액티브효과별-연출"></a>
#### 7. 기본 공격·액티브·효과별 연출

모든 캐릭터는 승인된 외형·정렬 anchor로 돌아온다. 정적인 포즈 이동을 완성된 연속 애니메이션으로 보고하지 않는다. 캐릭터 피격은 기존 tint만 유지하며 별도 피격 모션을 제작하지 않는다.

| 단계/효과 | 화면 구성 | 동기화·종료 | 필요한 실제 자산 |
| --- | --- | --- | --- |
| 기본공격 준비 | 발을 고정하고 상체·팔·무기만 준비 | 실제 windup 진척, 임의 반복 준비 없음 | mesh/weights/bones 또는 검증 연속프레임, 손/총구 socket |
| 기본공격 타격 | 총구/공격점→실제 대상 짧은 궤적, 해당 충격 | 실제 effect 시점에 종점 일치, 장거리 그림 때문에 피해 지연 금지 | muzzle·projectile·impact 개별 layer |
| 액티브 | 해당 대원 카드 강조, 대상 표시, 역할별 VFX | 시전 확정→실제 효과→복귀 | 기존 portrait 재사용, 효과별 유효 영역 |
| 흡수/방어 | 실제 수혜자 윤곽·얇은 보호막, 흡수 반응 | shield 잔량/만료가 authoritative | HP와 별도 shield layer, 소실 표현 |
| 회복/수리 | 대상 아래→위 펄스, 실제 회복 수치 | fullHP에서 가짜 회복 숫자 금지 | 회복/열차수리 서로 구별 |
| mark/강화 | 대상 위 작은 고정 상태표식 | source별 상태 종료에 연동 | 도형+짧은 설명, 색상만으로 구분 금지 |
| interrupt | 해당 예고선 종료·짧은 중단 표식 | 실제 interruption만 성공 반응 | stun과 다른 중단 glyph |
| down | 자세 붕괴→고정 down, 카드 상태·복귀시간 | 실제 down 이벤트, 같은 사건 반복 없음 | 공격/다운/복귀 분리 clip·연결 pose |
| 복귀 | 기존 위치에서 기립→대기 | 실제 targetability/행동 가능 시각을 pose가 지연시키지 않음 | 발접지·관절·최종 anchor 검증 |

외부는 열차 hull·실내·포탑·바퀴·지도 layer를 유지한다. 기본 포탑 body/barrel/muzzle·바퀴 구현을 재제작하지 않는다. 타격 recoil은 실제 발사 이벤트와 연결하며 끝없이 루프 발사하지 않는다. 지도와 창 밖 풍경은 기존 공통 이동좌표를 유지하고 낮은 자극 설정도 이동거리·배속을 바꾸지 않는다.

<a id="source-7dd86e35b7c512fd-8-분대장-발동의-컷-구성과-동시-발동"></a>
#### 8. 분대장 발동의 컷 구성과 동시 발동

전체 연출의 기준은 ‘입력한 카드 → 실제 분대장 → 실제 대상’의 짧은 시선 연결이다. 카드만 발광하는 대안은 구분이 약하고, 전체 화면 컷신은 두 전장의 경고를 가려 제외한다. 소형 컷인을 기본으로 사용하고 위험·가림 시 자동 간소화한다.

| 컷 | 표시 길이 초안(표현시간) | 구성 | 판정과 관계 |
| --- | --- | --- | --- |
| 입력 반응 | 최대80ms | 누른 카드 안쪽 반응 | 성공 발동음 없음 |
| 확정 시작 | 0–120ms | 카드와 분대장 위치 짧은 연결선 | 저장된 시전 확정으로1회 생성 |
| 인물 강조 | hold280ms + exit120ms 이내 | 카드 위 아군측 소형 상체 컷인, 최대 화면폭28%·높이18% | 실제 cast가 짧으면 타격 뒤에도 인물컷을 끌지 않고 축소 |
| 실제 효과 | 실제 effect 시각 | 대상 중심 역할 VFX·주요 SFX | 위 hold시간 완료를 기다리지 않음 |
| 정리 | 150–250ms 이내 | 임시광·연결선·컷인 원복 | cooldown/지원채널 해제와 무관 |

공간 예약 검사가 HP·warning·선택 타깃·입력 버튼과 충돌하면 컷인을 생략한다. 동시에 다른 대원이 발동하면 일반 카드 강조와 실제효과만 함께 보여준다. 큰 인물컷은 최대1개, 밀린 컷인 queue를 만들어 몇 초 뒤 재생하지 않는다. danger의 판정/이벤트가 먼저이며 시각 연출의 선착순이 전투 순서를 결정하지 않는다.

skip 버튼은 장식 컷인이 있을 때만 보이고 필수 경고와 충돌하지 않는 HUD 공간에 둔다. 여러 번 눌러도 같은 표현 토큰 종료만 한다. 타격 직전/직후 skip 모두 실제 효과·자원·다음행동 시각은 동일해야 한다. skip 후에도 대상 표식/타격/지속효과 표시가 남는다.

<a id="source-7dd86e35b7c512fd-9-위협-경고wave결과-전환"></a>
#### 9. 위협 경고·wave·결과 전환

경고는 공격원→대상 연결 또는 대상칸 표식과 함께 남은 시간을 표시한다. 고정편성 게임이므로 회피 이동을 요구하는 원형 위험지대를 쓰지 않는다. 방해 가능이면 별도 중단 아이콘, 불가이면 방어/회복 대응이 가능한 정보만 보여준다. 카메라 밖 위협은 해당 전장 가장자리 표식으로 안내한다. 실제 engine에 없는 ‘무조건 방해 성공’ 추천 버튼은 만들지 않는다.

경고 도중 복원하면 현재 남은 시간부터 표시한다. 앞선 적 AI 문서의 warning 독점·1.5초 간격·4초 최대지연은 제안이므로 이번 연출 작업에서 engine에 새로 적용하지 않는다. 시각 충돌은 장식 감축으로 해결한다.

wave 전환은 실제 both-lane clear/pending-spawn/effect 조건에 따르고, 적이 잠깐 화면에서 안 보인다고 clear하지 않는다. 기존 계약의 gap에서 다음wave 작은 라벨을 띄우며 HP/쿨타임/회복시간/게이지를 유지한다. 장식 잔해·사망 clip이 다음 wave 판정을 붙잡지 않는다.

전투 종료 후보 → 원본 finish 확인중 → 서버가 제공한 결과/preview → 사용자 명시 claim → receipt에 맞는 공유상태 갱신 → 로비 순서를 따른다. 조회/재시도 중 같은 run·원본 body/key를 유지한다. 보상 숫자 count-up은 실제 preview의 표시이고 지급이 아니다. 응답 유실은 확인중이며 패배·환불로 표시하지 않는다. 알 수 없는 outcome은 기존 안전한 상태로 안내한다.

<a id="source-7dd86e35b7c512fd-10-음향가독성접근성"></a>
#### 10. 음향·가독성·접근성

- 음향 우선순위: 치명 위협/열차위험 → 주요 스킬효과 → 일반타격 → 장식. 큐가 밀린 경고음을 뒤늦게 틀지 않는다. 현재 경고 상태는 항상 시각으로도 제공한다.
- 같은 프레임 다중타격의 시각 적용은 모두 보존하되 큰 효과음·진동은 해당 명령의 첫 주요효과1회로 묶는 안을 쓴다. 개별 피해 판정이나 통계를 합치지 않는다.
- 공격/방어/회복 효과의 모양·방향·아이콘을 달리하고 색상만으로 알리지 않는다. 실제 승인 음성이 없는 인물은 음성을 필수 제작물로 가정하지 않는다.
- 이름은 고정, 쿨타임은 별도 영역. 작은 화면에서 이름 자리를 줄여 임의 약칭을 만들지 않는다. 안전영역·경고·타깃·입력 터치영역을 먼저 확보하고 장식을 줄인다.
- 큰 전면 섬광/히트스톱/강제 줌 없음. 흔들림 옵션이 이후 구현되어도 HUD와 상대 전장의 읽기를 방해하지 않도록 국소 표현만 검토한다.

<a id="source-7dd86e35b7c512fd-11-검증-기준과-제공할-산출물"></a>
#### 11. 검증 기준과 제공할 산출물

| 검증 묶음 | 반드시 비교할 것 | 완료 증거 |
| --- | --- | --- |
| 실제 연결 | 동일 prepared owner/profile/content→runner→provider→accepted run | 실제 실행 entry/source/hash와 4산출물, HTTP 기록·원본 run 일치 |
| 모드 동일성 | 같은 seed·명령 stream으로 전체/간소화/첫사용/skip | gameplay event·최종HP·자원·cooldown·outcome 동일; marker/영상 차이는 허용 |
| skip 경계 | 타격 직전/후, 중복skip, down/interrupt 중, 옵션 변경 | 사건별 같은 효과횟수·시간·자원, 임시layer 원복 |
| 복원 | 확정 직전/후·완충·사용 직후·첫컷 시작/skip후 | 원본 명령 중복0, 이미 지난 컷인·SFX·진동 재생0 |
| 지원7신규안 | 실제 값·타깃·최강중첩·expiry·면역·원천제외 | APPLIED만 아닌 수치/상태 변화 단언과 신규 profile version |
| 동작 | 공격 준비/타격/복귀·down/기립, pause/배속 | rig/clip/weights/footprobe/socket 및 실제 캡처. packing PASS로 대체 금지 |
| 위협·UI | 양전장 위협·동시 스킬·작은 안전영역 | 경고/HP/입력이 컷인에 가려지지 않는 실제 frame |
| 전체 왕복 | 실제 전투→finish→preview→명시claim→로비 | 연결된 짧은 영상+검증 로그, synthetic 여부와 환경 명시 |

검토 화면시간(80/120/280ms 등)은 제작 초안이며 실제 가독성에 맞춰 Direction이 조정할 수 있다. effect 시각은 조정할 수 없다. 실제 QA는 1x 및 지원하는 배속, 합법 pause/복원, 모든3모드에서 수행한다. 기존 client/effect28·panel1 같은 무관·중복 suite를 합산해 새 기능 PASS로 세지 않는다.

<a id="source-7dd86e35b7c512fd-12-실행-순서와-남은-결정"></a>
#### 12. 실행 순서와 남은 결정

1. Unity는 현재 r6 parser/loader/runner를 완성하고 Server와 실제 왕복을 우선 검증한다. 이번 상세기획의 미정 콘텐츠는 이 경로를 막지 않는다.
2. Direction의 [제작 breakdown](51_UI_ART_DETAIL_CONTRACTS.md#source-78eacf9c42a3e722)을 수신·직접 읽었다. 컷/레이어/가림검사/납품 mesh·weights·socket/사건상대60fps 검수 프레임은 제작안이며 실제 제작·QA 완료는 아니다. 그 문서의 first-use 결정대기는 이 문서 §5의 로컬 기준으로 해소한다. Direction 소스사본 경로와 실제 `E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/Battle`은 구분한다.
3. Unity는 §5 표현 옵션·marker·skip 및 이벤트 매핑의 구현 차이와 기존 작업 순서를 제시하고, 실제 연결 주경로를 보존하면서 후속 구현한다. Server는 순수 표현 옵션용 API를 새로 만들 필요가 없다.
4. Server/Unity capability 검토 후 §6 수치안의 원천·stack·cap·효과 schema 차이를 좁힌다. 신규 profile 전체 버전·회귀가 준비되면 별도 채택 기록 후7효과 연결을 배정한다.
5. 정식 이름·전체 소유외형·16대원 확장·출시 밸런스·기기 검증은 별도 잔여다. 임의 데이터로 완료 처리하지 않는다.

이번 Plan 수행은 원본 자료·실제 코드 일부 읽기, 담당 지시, 문서 상세화와 정적 자체 검토다. 제품 코드/이미지/오디오 생성·게임 테스트·DEV/PRD/device 실행은 하지 않았다. 문서는 로컬 미커밋이며 원본 저장과 읽기 검증 후 인계에 경로·배정 결과를 기록한다.

<a id="source-7dd86e35b7c512fd-13-실행-상태-정정-및-전투-연출-사건표--2026-09-12-후속"></a>
#### 13. 실행 상태 정정 및 전투 연출 사건표 — 2026-09-12 후속

이 절은 앞 절의 작성 당시 진행 상태를 갱신하고 제작을 구체화한다. r6 실제 headless 15wave 전투·finish·명시 claim은 edfeab5로 완료되었다. W01/W02 정상 UI 왕복과 동일 Node DB에서 Unity 전체 재시작은 별도 진행 중이다. 초기5명10표시명은 combat-display-ko.plan3.20260912.001로 Plan 신규 채택·자료 공급 완료이며 실제 화면 검증은 별도다. §6 신규7지원 수치는 실행장부 §3.2에서 별도 버전의 로컬 구현·비교 기준으로 채택되었다. 따라서 숫자 승인 대기로 멈추지 않는다. 현재 r6 활성화나 출시 밸런스 확정은 아니다.

<a id="source-7dd86e35b7c512fd-131-화면-사건의-읽기와-충돌-처리"></a>
##### 13.1 화면 사건의 읽기와 충돌 처리

아래는 Plan이 새로 정한 로컬 표현 기준이다. simulation 순서·피해·시전시간은 기존 계약 그대로다.

| 사건 | 플레이어가 확인할 결과 | 표현 시작과 종료 | 동시 사건 처리 |
| --- | --- | --- | --- |
| 입력 접수 전 | 누른 대상과 선택 상태 | 카드 내부 눌림/프리뷰, release 취소로 해제 | 성공음·비용 소비 표현 없음 |
| 저장된 시전 확정 | 누가 무엇을 누구에게 사용하는가 | 카드명 유지, 시전자/확정 대상 연결. 실제 시전이 끝나면 준비 표시 제거 | 분대장 외 큰 인물 컷은 만들지 않음 |
| 피해 | 실제 피격자와 HP/보호막 결과 | 권위 effect 시점의 국소 impact와 tint, 기존 상태 값 갱신 | 별도 피격 동작·히트스톱 없음. 여러 타격을 하나의 판정으로 합치지 않음 |
| 보호막 흡수 | 보호막으로 막힌 피해와 남은 보호막 | 실제 흡수 사건 때만 보호막 반응. 새 적용과 흡수 구별 | HP 피해가 없으면 HP 감소 숫자 없음 |
| 회복/수리 | 생존 대원 회복 또는 열차 수리 | 각각 해당 수혜자에 다른 도형·실제 회복량 | 무효·과회복0을 양의 회복처럼 표시하지 않음 |
| 버프/표적 | 현재 효과 대상과 종료 상태 | 현재 권위 상태에서 지속표식 재구성 | 약한 효과 만료가 강한 다른 원천 표식을 제거하지 않음 |
| 방해 | 실제 중단된 행동 | 실제 interruption 때 예고 종료와 중단 도형 | 이미 적용된 효과는 보존. 면역·무시전은 성공 연출 없음 |
| down/복귀 | 행동불가와 실제 복귀 가능 시점 | 실제 상태 전이와 시각에 연결. clip 지연이면 현재 상태에 맞춰 축소 | 전원 down을 자동 패배로 해석하지 않음 |
| 결과 | 확정 outcome과 정산 단계 | 실제 terminal 이후 finish 확인중→preview→명시 claim→receipt | 승리 음악/화면이 지급을 뜻하지 않음 |

가림 충돌은 필수 경고·대상·상태·입력 영역을 보존하고, 인물 컷→연결선→장식 입자 순서로 줄인다. 두 전장 경고가 겹치면 각각 자기 전장 안에 원천/대상을 표시한다. 하나를 숨기거나 공격 시각을 미뤄 해결하지 않는다. 여러 주요 효과는 각각 대상 반응을 유지하되 인물 컷은 최대1개이며 나중 재생 queue는 없다.

피해·회복 숫자가 모이면 표시 밀도를 줄일 수 있으나 HP/보호막 값, 대상, 중단 여부는 항상 남긴다. 숫자 병합은 이번 기본안에서 사용하지 않는다. 향후 병합이 필요하면 같은 수혜자/효과종류/표시구간이라는 별도 표현 규칙으로 검증하고 게임 event stream은 보존한다.

<a id="source-7dd86e35b7c512fd-132-사건표-납품-규격"></a>
##### 13.2 사건표 납품 규격

Unity의 매핑표는 의미명, 실제 producer 파일/멤버, 생성 경계, 안정 식별키, 대상/수혜자, 적용값·시각, 복원 동작, 현재 부족 근거를 한 행에 기록한다. 이는 새 wire field 도입 지시가 아니다. HP 차분만 존재하면 피해 원인·흡수·중단을 추정하지 않고 해당 표현을 미연결로 둔다. 누락을 발견해도 이미 가능한 기본 상태 표시와 다른 작업은 진행한다.

Direction은 초기5명 active/ultimate 10행을 실제 kit에 대응시킨다. 전선 방호/최후의 방벽, 집중 사격/결정타, 응급 처치/생명선, 신호 차단/집중 표적, 현장 수리/기관 재가동의 명칭 자체에서 새 범위·피해·부활·무기를 만들지 않는다. 각 행은 준비→적용→회수, 실제 수혜자, full/simplified 차이, interrupt/down/restore 정리, 보유/부족 자산, 음향 source, 검사 프레임을 포함한다.

지원은 현행3종과 별도 버전 신규7종을 명확히 분리한다. 신규 스캔과 교란의 지정 대상 프리뷰는 확정 target ID에 연결하고 숨은 focus 교체를 하지 않는다. 의료는 대원 회복, 수리는 열차 회복, 방호는 보호막, 연막은 피해감소를 구별한다. 신규 효과 구현 전 그럴듯한 성공 VFX를 먼저 넣어 완료로 세지 않는다.

<a id="source-7dd86e35b7c512fd-133-제작검수-묶음"></a>
##### 13.3 제작·검수 묶음

| 순서 | 산출물 | 통과 조건 |
| --- | --- | --- |
| A | 실제 UI 전투/정산 및 전체 재시작 증거 | 동일 origin/Node DB·원본 run/명령 유지, 명시 claim·가시 로비, 환경과 영상 명시 |
| B | 사건 매핑표+초기5명10행 연출 시트 | 각 효과의 실제 kit·producer 근거, 누락·자산 상태 구분 |
| C | 모드/skip/복원 검증 | 같은 초기상태/명령의 gameplay 결과 동일, 지난 one-shot 재생0 |
| D | Kyle bounded rig 진단 | 정확 부족 layer·좌표 또는 실제 mesh/weights/clip/socket·접지 증거. 실패를 packing 성공으로 대체하지 않음 |
| E | 신규7지원 별도 version | 타깃·적용량·중첩·만료·중단·복원 실제 단언, 기존 r6 회귀 유지 |

A를 기다리는 동안 Server의 E 계약/구현과 Direction의 B는 진행한다. C/D는 Unity의 실행 소스 freeze와 조율한다. 표현시간·음량·입자량은 제작 변수로 남기고 실제 사건시각을 수정하지 않는다. 비교 영상·로그가 없는 행은 미검증으로 표시한다.

이번 후속은 기획 상세화와 기존4담당 실행 지시다. Plan은 게임 코드·이미지·오디오를 생성하거나 제품 테스트를 실행하지 않았다. 문서 정적 대조 및 원본 전체 문자열 읽기 검증만 수행했다.

<!-- source-body:source-7dd86e35b7c512fd:end -->


<a id="source-b41a901d11532729"></a>
## Combat Mechanic Warning UI V1

원출처: `WorkspaceRepo/docs/design/combat-mechanic-warning-ui-v1.md`  
원문 SHA-256: `f2e0830c49e16b55b0ec6ad1e316e9ad2a101e43e286bd54ce2a636d52a0eaa7`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Warnings target vsresponsiblerole separated, authoritativewindow; cancel≠success; statusstrongest/cap/sourceexpiry/no fabricatedzero. Capacity3icons proposal; oldcutinrules superseded04§14.

<!-- source-body:source-b41a901d11532729:start -->
<a id="source-b41a901d11532729-combat-mechanic-warning-ui-v1"></a>
### Combat Mechanic Warning UI V1

2026-09-09 / Plan / presentation design proposal only.
Sources: story-raid-boss-rotation-v1.md, world-raid-rules.md,
player-input-cooperation-detail-v1.md and coop-status-signals-v1.md.

<a id="source-b41a901d11532729-warning-information"></a>
#### Warning Information

Each actionable warning separates four facts: affected target, responsible role,
valid response and authoritative remaining window. Damage target and actor required
to counter it are NOT necessarily the same. A train threat may need a squad action.
Do not infer the required role solely from upper/lower placement or threat color.

Use a fixed short callout near the pane boundary plus exact target outline. Required
action uses existing controls or a separately authored mechanic interaction, never
a fake universal counter button. Countdown follows the mechanic deadline, not a
fresh duration started when the client received the event. No pause while reading.

Target highlights remain visible without hiding feet, silhouettes, HP, skill buttons
or the five-person formation. Icons/shapes and text supplement color; avoid full-screen
flashes and repeated shake. No new art production is authorized by this document.

<a id="source-b41a901d11532729-mode-specific-meaning"></a>
#### Mode-specific Meaning

- Ordinary/story: preserve authored warning durations and existing counter windows.
  Story's4s warning remains that design's candidate, not a global override. Show
  interruption or defensive mitigation only when the actual attack supports it.
- World boss: current candidate mechanic penalty bypasses ordinary armor/shields.
  A generic shield cannot be presented as mechanic success. Show the authored task,
  marked affected entities and failure consequence using server-approved display data.
- Solo: present ordered tasks only if solo authored mechanic has such steps. Never
  demand simultaneous taps in both panes or copy a two-person task unchanged.
- Coop: show my task prominently and partner task read-only. My completion becomes
  confirmed only on server acknowledgment; partner pending is not shared success.
  Role controls stay owned, and pings cannot complete a mechanic.

Exact action texts for world boss patterns are a content-contract prerequisite.
This UI spec does not invent a new interactable, boss task, damage rule or phase.

<a id="source-b41a901d11532729-outcome-states"></a>
#### Outcome States

Warning -> responding -> acknowledged subtask (when applicable) -> server resolution.

| Outcome | Presentation |
| --- | --- |
| My task done, partner pending | Compact completed mark beside my role; keep shared warning |
| Shared success | Clear warning; show actual earned effect and remaining duration |
| Failure | Clear warning; show affected target and actual damage, not a generic all-squad hit |
| Cancelled/interrupted | Distinct cancelled/interrupted cue; no automatic success buff |
| Phase retires task | Remove it neutrally; do not manufacture success or failure |
| Connection uncertain | No local green completion; reconcile current mechanic |

World success buff is one shared modifier. UI may show one icon with actual
multiplier/time, never apply another score multiplier. Recovery/reconnect does not
refresh it. Do not grant world-boss success bonuses to ordinary/story warnings.

<a id="source-b41a901d11532729-priority-and-conflicts"></a>
#### Priority And Conflicts

Critical target markers and mechanic deadline outrank partner pings, damage-number
decoration and flavor dialogue. A skill cut-in may be reduced behind warnings but
cannot hide actionable controls or delay gameplay. If multiple threats are legal,
retain each target marker and use a bounded list ordered by imminent deadline;
never suppress an actionable threat solely to keep one pretty banner. Actual HUD
capacity must match the authored simultaneous-threat cap before shipping.

Expired messages are dropped, not queued for replay. An approaching phase boundary
uses existing world scheduling/cancellation rules; UI cannot extend the window or
grant success because time ran out. Muting pings does not mute core warning visuals.
Audio/vibration respect user settings; essential meaning must survive without either.

<a id="source-b41a901d11532729-acceptance-cases-and-limits"></a>
#### Acceptance Cases And Limits

<a id="source-b41a901d11532729-status-and-shield-readability-proposal---2026-09-09"></a>
##### Status And Shield Readability Proposal - 2026-09-09

Display existing combat-stats-v1 effects, not new buffs or stacking rules. Distinguish
incoming warning, active effect and skill cooldown; one icon must not imply all three.

- Keep HP and shield visibly distinct. Shield is an adjacent outlined segment or bar,
  not extra green HP or a claim of healing. Its display follows actual remaining
  eligible shield; a capped/expired application does not show its original amount.
  This does not suggest ordinary shields prevent World failure-mechanic damage.
- Actor down/recovery replaces the actionable portrait state with remaining return
  time. Stun uses an explicit control-disabled symbol; attack slow does not disable
  legal skills. Cooldown remains independent and continues by the existing rules.
- Proposed compact portrait capacity: three ordinary status icons plus an overflow
  count, subject to portrait device QA. Control-disabled state and incoming targeted
  mechanic markers are outside this overflow and must remain visible. Never hide
  urgent input unavailability behind +N. Do not shrink hit targets to fit icons.
- Give ordinary negative statuses priority, then beneficial effects. Keep stable
  ordering by authored priority/type rather than re-sorting every frame by time.
  Use shapes/icons and short accessible labels, not color alone. Counts are effect
  kinds not an invented stack multiplier or number of failed mechanics.
- Group identical types for compact display, but show actual effective magnitude
  under strongest-source/cap rules, not summed source percentages. The expanded
  view can list source and expiry separately when those fields exist. Different
  sources may expire at different times; no one countdown may imply all expire then.
- Show countdown to the next relevant displayed state change for grouped effects,
  and identify it as such in detail. Never reset remaining time upon late packets,
  reconnect or visual reopening. If source detail is unavailable, omit it rather
  than invent a countdown or use0 to imply expiry. Unknown state differs from absent.

Expose a compact status-detail control separately from portrait skill taps/long
press descriptions. Detail is read-only, does not pause battle, auto-cleanse, cast,
or change target; essential warnings and controls stay visible. Do not force this
panel open when a status changes. Touch cancellation and backgrounding follow the
existing input rule; reviewing a status cannot accidentally activate a skill.

Train effects attach to train status, operator effects to their actor. Mark/armor
break attach to affected enemy, with selected-target detail showing actual values.
Shared World success buff may have one shared icon; it is not a second per-player
bonus. Cosmetic focus marker remains distinct from damage-amplifying mark status.

Feedback cues acknowledge effective changes, not every network retransmission.
When cleansing one eligible status, remove that confirmed effect only; remaining
debuff icons persist. No universal Cleansed badge when a non-cleansable mechanic
mark remains. Healing numbers represent effective healing; no fake positive number
for overheal, unused shields or a rejected cast. Avoid repeated flashes/shake.

Future UI checks: capped shields, different-source expiries, debuff overflow,
stun+cooldown, down+recovery, sound off, large text, duplicate events, late snapshots,
reconnect and tap/long-press separation. Display data is a contract dependency, not
proof these aggregates already exist. No art generation, prototype or device test
performed. Three-icon capacity is a proposal pending actual layout review.

Future review: initial-five kits without an available counter, wrong defensive tool,
solo sequential task, coop one-sided completion, downed required role, color-vision
variation, sound off, large text, overlapping legal threats, late packets, reconnect,
phase cancellation and skill effects over targets. No runtime tests performed.
Only policy reads and document readback. Local/uncommitted; no code/build/video/
deployment or owner assignment. Next authoring check should map each actual mechanic
to these four facts and supported responses, not add new mechanics by default.

<!-- source-body:source-b41a901d11532729:end -->


<a id="source-e020940776a37ed6"></a>
## 전투 연출 보강 v03 — 2026-09-12

원출처: `WorkspaceRepo/docs/design/combat-presentation-enhancement-v03-20260912.md`  
원문 SHA-256: `849d735192c60e3fa642a655ceca770fac84b74b34383a82d9d534e6a6ec7e56`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Typed actualpositive effect cues, refresh-onlyno newshield, actualinterruption; max1/noqueuecutinoldnow04§14 override; rigvisualFAIL historical; decodesnotlisteningquality.

<!-- source-body:source-e020940776a37ed6:start -->
<a id="source-e020940776a37ed6-전투-연출-보강-v03--2026-09-12"></a>
### 전투 연출 보강 v03 — 2026-09-12

사용자 요청: “연출 더 디벨롭해서 보강”. 기존 전투 상세와 production breakdown을 잇는 Plan 신규 제작 기준이다. 구현·최종 미술 승인 기록이 아니다. 기존 네온/단일 해, 실제 typed 효과 및18음원 연결 보고, 실제 전투·재시작·claim 증거를 보존한다.

<a id="source-e020940776a37ed6-1-우선순위와-범위"></a>
#### 1. 우선순위와 범위

첫 보강은 공격 출처의 차이, 효과 수혜자 식별, 준비/적용/회수의 리듬에 집중한다. 전체 화면 빛이나 흔들림 증가를 타격감의 대체로 사용하지 않는다. 고정 대형·독립 실시간 공격·히트스톱 없음·피격 tint만·정확한 N%/실제 값은 유지한다. 기존 화면을 다시 디자인하지 않는다.

Kyle 한면 가중변형은 시각 FAIL이고 독립 원천6개가 부족하다. 이를 완성된 rig처럼 사용하지 않는다. 가능한 국소 VFX/기존 초상 컷인/음향부터 진행하며 검증된 분리 부위가 생기면 연속동작을 결합한다. 몸통 전체를 흔들거나 늘려 미완성 관절을 숨기지 않는다.

<a id="source-e020940776a37ed6-2-효과별-보강-기준"></a>
#### 2. 효과별 보강 기준

| 구간 | 보강할 표현 | 근거·실패 처리 |
| --- | --- | --- |
| 칼 공격 | 준비 때 무기 방향을 읽게 하고 짧은 베기 궤적과 접촉점을 구별. 국소 사선 충격 뒤 빠르게 회수 | 실제 공격/타깃·검증 socket 필요. 타격 시점은 기존 판정. 피격자 동작 추가 없음 |
| 총·포탑 | 총구 광원/짧은 발사 흔적과 대상 충격 분리. 총과 포의 크기·질감 구별 | 실제 발사 근거/포구 좌표가 없으면 먼저 매핑. 궤적 도착이 피해를 실행하지 않음 |
| 보호막 발급 | 수혜자 외곽을 따라 펼쳐지는 얇은 선, 실제 layer 잔량 표시는 지속 | 실제 addedAmount>0. refresh-only를 새 보호량처럼 표시하지 않음 |
| 보호막 흡수·만료 | 흡수는 접촉 위치의 짧은 파문, 만료는 조용한 윤곽 해제 | 실제 absorbed>0·실제 expiry. 파괴/만료 근거가 없으면 폭발처럼 보이지 않게 함 |
| 의료 회복 | 수혜자 중심의 부드러운 상승 펄스와 의료 도형 | actualHpGain>0인 해당 대상만. 부활·지속회복 추가 없음 |
| 열차 수리 | 차체 수리점의 짧은 용접성 입자·기계 도형 | repair 실제 증가만. 대원 회복과 같은 초록 원으로 통일하지 않음 |
| 표적·도발 | 표적은 얇은 잠금 브래킷, 도발은 영향을 받은 적의 짧은 방향 신호 | 실제 상태 추가/유효 갱신, 낮은 효과 거절은 성공 연출 없음 |
| 중단 | 끊어진 예고선과 짧은 차단 기호 | 실제 취소된 행동만. 현재 기본공격 중단을 스턴·침묵으로 확대하지 않음 |
| 열차 강화 | 해당 장치 영역의 짧은 에너지 흐름, 이후 작은 상태 표식 | 실제 버프 범위·만료. 새 공격이나 속도 상승을 연출로 만들지 않음 |

색상만으로 구분하지 않고 모양·방향·수혜 위치를 함께 사용한다. 적 HP의 붉은 구분을 유지한다. 지속 표식이 많으면 장식을 줄이고 경고/대상/HP/버튼을 우선한다.

<a id="source-e020940776a37ed6-3-분대장과-동시-시전"></a>
#### 3. 분대장과 동시 시전

전체 연출은 카드 확인→선택 분대장 소형 초상→실제 수혜자/타격점 연결의 세 구간으로 읽히게 한다. 기존 상한과 max1/noqueue를 유지한다. 효과가 먼저 발생하면 초상 hold를 기다리지 않고 축소한다. 경고가 겹치면 인물 컷부터 생략한다.

일반 액티브/지원에는 새 인물 컷인을 추가하지 않는다. 다중 대상은 각 대상의 실제 반응을 보존하되 같은 command/effect의 큰 음향은1회다. Sera처럼 여러 효과를 갖는 스킬은 피해·중단·표적 결과를 각각 판단하고 실제 성립한 효과만 표시한다.

down은 현재 상태를 명확히 표시하고 복귀 가능한 실제 시점과 표현을 맞춘다. 완성된 clip가 없으면 상태 표시로 남기며, 회복 효과나 초상 등장으로 실제 복귀를 대신 연출하지 않는다.

<a id="source-e020940776a37ed6-4-음향과-시간"></a>
#### 4. 음향과 시간

기존 개별 cast/effect 파일을 출발점으로 쓴다. 준비음과 성공음을 서로 다른 사건에 연결하며, 같은 소리의 과도한 중첩을 줄인다. 효과 시점은 simulation 사건에 종속하고 소리 길이에 맞춰 전투를 늦추지 않는다.

각 스킬 비교 영상에서 다른 소리가 마스킹하지 않도록 단독 구간과 실제 동시 교전 구간을 둘 다 제공한다. 무음/배속/skip/restore에서 중복 재생을 확인한다. 실제 청음하지 않았다면 decode·peak/clip0만 기록하고 음질 합격이라 쓰지 않는다. 새 유료 구입은 범위에 넣지 않는다.

<a id="source-e020940776a37ed6-5-산출물과-완료-기준"></a>
#### 5. 산출물과 완료 기준

- Direction: 현재→보강→실제 자산/좌표/출처→미구현 구분표, 제작 가능한 효과 리소스. v03 별도 보존.
- Unity: actual typed 결과와 연결한 보강 구현. 0량/면역/소멸/낮은 효과 거절·중복·복원에서 잘못된 성공 연출0.
- 같은 초기 상태/seed/명령의 전후 gameplay 결과가 같아야 한다. 표현 marker·음향 상태는 비교에서 구분한다.
- 30초 안팎의 실제 음향 포함 전후 비교 영상부터 제출한다. 모든 효과를 억지로 한 영상에 넣지 않고 구현된 묶음부터 제공한다.
- 화면에 컷 이름·구현 범위를 구분하고 신규 실제 admission인지 로컬 재현인지 명시한다. 검정 프레임·실제 오디오 트랙·같은 스케일 전후 화면을 확인한다.
- 기존23e3 전투/7c127 claim/f496 offline 증거를 새 보강의 실행 증거로 재사용하지 않는다. 새 서버 실행이 필요하면 새로운 전체 자료를 검증한다.

이번 Plan 산출은 제작 기준과 담당 지시이며 제품 코드/미디어/게임 테스트를 실행한 것이 아니다.

<!-- source-body:source-e020940776a37ed6:end -->


<a id="source-2c187562f447eff3"></a>
## Departure Preparation Screen V1

원출처: `WorkspaceRepo/docs/design/departure-preparation-screen-v1.md`  
원문 SHA-256: `5e5480febad13ee147c485ed3dab359dbfd57aad305e88b5d3467138fd894f94`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Preserve substantive clauses under: Portrait Structure; Start Package Display; Readiness And Departure; Availability And Errors; Squad Editing Detail - 2026-09-09; Train Editing Detail - 2026-09-09; Original Screen Verification Scope

<!-- source-body:source-2c187562f447eff3:start -->
<a id="source-2c187562f447eff3-departure-preparation-screen-v1"></a>
### Departure Preparation Screen V1

2026-09-09 / Plan / proposed UI layout, not a produced mockup or implementation.
Sources: predeparture-validation-freeze-v1.md, start-package-selection-detail-v1.md,
online-coop-mode-policy.md, invitation and random-match flow documents.

<a id="source-2c187562f447eff3-portrait-structure"></a>
#### Portrait Structure

Top fixed summary: operation/boss, mode, difficulty only where applicable, two
participants' roles and readiness. Show safe display names, not raw identifiers.
Middle scrollable workspace: My Setup / Partner Setup tabs. My Setup initially
selected; partner's setup is read-only. Avoid two full equipment panels side by side.
Bottom fixed safe-area action strip: my authoritative quoted cost/balance and
ready/cancel-ready or host departure. Content scroll padding prevents occlusion.
No extra full-screen illustration generation or functional lobby redesign here.

Own role determines editor: squad5 legal slots and selected ultimate, or own train/
carriage/equipment slots. Other personal inventory is not substituted for partner's.
Squad preview retains ally-area left3/right2 orientation, not3top/2bottom. Actual
operator assignment stays user-selectable under rules, no new class-slot restriction.
Tapping a slot opens eligible owned replacements; no fake unlock or auto-purchase.

Partner tab shows the shared accepted draft loadout summary, not full inventory.
Role icon and read-only treatment distinguish it from my editor. Viewing details,
scrolling or changing tabs never changes selection or readiness.

<a id="source-2c187562f447eff3-start-package-display"></a>
#### Start Package Display

Where this encounter has an authored start package, display the shared package once,
not four effects per participant. Use previous proposed3augment+1tactic cardinals
only if that package contract is adopted. Do not silently apply it to World Raid.
Under previous role-allocation proposal: squad edits squad augment+tactic, train edits
train+support augment. Partner-owned choices remain read-only. This allocation is
still a proposal; layout cannot confer authority absent a server contract.
Show incompatible option reason and require a legal selection, not silent substitution.

<a id="source-2c187562f447eff3-readiness-and-departure"></a>
#### Readiness And Departure

- Each player presses Ready once their current revision validates. Validation can
  happen behind that action; do not add a confirmation popup for every API step.
- Ready locks my editing. Choose Cancel Ready to edit; any accepted relevant
  loadout/role/operation/package change clears both readiness states. No tab-change reset.
- Both ready -> host action becomes Depart, guest sees Waiting For Departure plus
  Cancel Ready while server permits it. One clear primary action per state.
- Host Depart commits only the mutually accepted revision/current quoted costs.
  No force start, host alteration of partner assets or timeout auto-selection.
- Late old response cannot restore ready after an edit. Display validating/unknown
  distinctly rather than reuse a green mark from the prior revision.
- If cost/content changes, update summary and require renewed readiness. A ready
  mark is not permission to debit any future price.
- Cancel-ready versus departure race follows server outcome; committed start
  enters existing recovery flow, not a client-side refund or new admission.

Solo removes partner elements and the separate ready stage: edit -> Depart performs
validation/start with stage feedback. No redundant Ready then Depart by one person.
Keep applicable resource download/consent before admission, per entry-readiness plan.

<a id="source-2c187562f447eff3-availability-and-errors"></a>
#### Availability And Errors

<a id="source-2c187562f447eff3-saved-setup-proposal---2026-09-09"></a>
##### Saved Setup Proposal - 2026-09-09

New optional convenience proposal. The earlier squad-editor slice explicitly did
not add presets; it remains historically accurate. This section proposes that later
extension, not implemented storage or a new prerequisite for combat simulation.
Recommend three free account-scoped setup slots, with no paid expansion in this slice.

Each setup stores a name and selection references: five operator IDs/positions,
selected ultimate, train/carriage instance selection and supported equipment/package
choices. Never store authoritative HP/level/ownership, quoted price, readiness,
partner assets, or an accepted run snapshot as reusable truth. Incompatible future
systems remain absent, not empty fields granting permission to use them.

Recommend server-synced preferences with account-scoped local draft cache; this
requires an owner storage/contract proposal before implementation. No endpoint or
sync behavior exists by implication. Local drafts are not entitlements and must
never cross accounts, guest links or account switches without account policy review.
Manual preparation remains available if preference sync is unavailable.

Save is explicit. Editing the current preparation does not silently overwrite the
saved setup. Overwrite/delete uses one confirmation; deletion does not unequip the
current draft or remove any inventory. Show save failure/unsynced distinctly. Remote
revision conflict must not silently replace a newer saved setup; offer reload or
explicit overwrite against the current revision, preserving the local draft.

Load first previews applicable changes and invalid selections; Apply updates only
the editable personal draft. Valid references resolve current stats. Missing/unowned
items remain visibly unresolved until replaced or explicitly cleared; no auto-purchase,
substitute operator or fabricated stats. Cancel preserves the original draft. Never
start a battle merely by selecting a saved setup.

In solo, applicable squad/train sections may load together. In coop, load only the
current user's assigned role and permitted package choices. Do not copy the other
role into the partner, change roles, operation or difficulty. A preset made for story
does not grant story coop or install its package into World. Unknown/incompatible
choices require correction under the selected encounter's current rules.

Ready users must cancel ready before applying. A semantically changed accepted draft
clears shared readiness by the existing revision rule; preview/rename/save-only or
identical selections do not. Server departure commitment wins over a late apply;
active run loadout remains frozen. Resources/preflight/cost are revalidated normally.

Recommended UI: compact setup selector in My Setup, Save action and overflow menu
for rename/delete. Show the squad/train contents and correction count before Apply,
not a green guaranteed-clear badge. Do not auto-load on operation change in the first
version; retain the draft and advise incompatible choices instead.

Future verification: load changed growth, removed selection, duplicate operator,
coop role mismatch, unsupported package, same-selection no-op, ready/start race,
account switch, failed save and conflicting revision. Not run. Three slots and sync
architecture remain proposals; no storage/code/deploy work authorized by this doc.

Attach slot errors to offending selection and focus it on attempted Ready. Incomplete
package, unavailable operator, invalid capacity and insufficient cost have distinct
server-derived reasons. Do not display power as a disqualifying threshold.
Partner leaving/connection unknown invalidates shared readiness and makes Depart
unavailable; preserve draft. Prebattle host loss does not imply host auto-transfer.
Invite code visible only in an appropriate unfilled invitation room; not a permanent
element of matched/full preparation or active battle.

<a id="source-2c187562f447eff3-squad-editing-detail---2026-09-09"></a>
#### Squad Editing Detail - 2026-09-09

Proposed interaction detail, no new stats/classes/entitlements. Source fixed
formation and selected ultimate: player-input-cooperation-detail-v1.md.

- Tap one of five slots to open owned-operator selector, then tap candidate to show
  portrait, role, actual level, basic targeting and skill summary; apply through one
  explicit Replace command. Browsing candidates is not an accepted draft change.
- If candidate already occupies another slot, show Swap Positions instead of Replace;
  swap both occupants atomically in the draft, never duplicate the same operator.
  Tap-based interaction is sufficient, dragging is not required. Cancel leaves draft.
- Editing uses left rear3/right front2 inside the ally area. Class icons suggest
  capabilities, not mandatory tank/healer positions. No flat front damage bonus or
  rear invulnerability invented. Enemy targeting exceptions retain actual rules.
- Owned selector can filter by role and sort by level/name with stable ties; preserve
  selected slot while scrolling. Unowned archive previews cannot be applied as owned.
  No combat-power autoformation or recruitment popup required to repair a draft.
- Empty slots may exist while editing but all5 unique eligible operators are required
  at preparation validation. Do not silently refill an empty slot or replace a removed
  operator when returning from another view. No saved-preset system added here.

<a id="source-2c187562f447eff3-ultimate-and-dependent-choices"></a>
##### Ultimate And Dependent Choices

One selected ultimate is attached to operator identity, not slot index. Swapping
positions preserves that selection. Replacing a non-selected operator also preserves
it. Removing the selected operator clears ultimate and shows a required-choice marker;
no automatic fallback or new cooldown benefit. Select exactly one of current five.
No claim that all five ultimates are available simultaneously during battle.

After accepted squad/ultimate change, revalidate authored package/tactic compatibility.
Keep still-valid choices; clear invalid ones with the reason, never grant a substitute
effect. UI prediction is not authority; Server validates IDs and eligibility before
run freeze. Role changes use each participant's own assets, not copied partner squad.

<a id="source-2c187562f447eff3-summary-and-ready-state"></a>
##### Summary And Ready State

Display ordinary skill target scope and the selected ultimate separately; for example,
Kyle lowest-HP ally1 shield must not appear as all-squad protection. Avoid comparing
an arbitrary sum of attack/heal/shield as one misleading power requirement. Detailed
resolved stats require actual server data, otherwise show unavailable rather than
invent computed values.

Accepted replace/swap/ultimate change invalidates shared ready and requires fresh
revision; browsing/filtering/cancel does not. Ready must be cancelled before editing.
Late validation response for old formation cannot overwrite new draft. All editing
is free and prebattle only; accepted start freezes the exact slot order and selections.

Future checks: occupied-slot swap, remove ultimate owner, invalid tactic, short list
or empty owned list, stale revision, readonly partner selector, long localization,
recovery after interrupted edit. No runtime tests or UI captures performed. This
follow-up is local design/readback only, not Unity or Server implementation.

<a id="source-2c187562f447eff3-train-editing-detail---2026-09-09"></a>
#### Train Editing Detail - 2026-09-09

Sources: train-loadout-speed-detail-v1.md, train-combat-test-profile-v1.md and
equipment-research-progression-v1.md. Proposed editing UX, not catalog activation.

Show locomotive separately and selectable carriage slots in side-view order, with
selected-car detail below. Tap slot -> available owned candidate -> before/after
comparison -> Apply. Cancel is no change. Narrow portrait allows scrolling the
preview strip without shrinking tap targets. Current slot remains visibly selected.

Actual server entitlement supplies slot count. Existing audited defaults2/3/4 are
source evidence, not a client unlock grant. All available slots filled and exactly1
Combat carriage under existing rules. Gun/Medical/Defense max2, Cargo max1 remain
type limits, NOT proof of second-instance ownership or Medical availability.
Candidate list must distinguish type definition from actual equip eligibility.

Show the required Combat slot's restriction before invalidating a draft; either
disable an invalid replacement with reason or allow only an atomic legal exchange.
Already-equipped unique instance moves/swaps, never duplicates itself. Multiple
same-type cars require verified quantity/instance semantics; do not invent item IDs.
Visual position changes grant no new interception or adjacency bonus.

Comparison includes selected-car function/HP where available, train resolved HP,
weapon contribution where available, total weight and base speed index. Call speed
an index, not km/h. Never sum legacy/new HP tables or fire-control bonuses. All
before/after values must use one versioned server-resolved profile; unavailable
comparison is explicitly unavailable, not a client guessed preview.

Existing audited weight rule slows to its configured floor, not automatic overweight
rejection. No new weight admission gate, wave reduction, reward bonus or World Raid
time extension. UI may show speed decrease alongside stronger defense, not claim
every green number means a strictly superior build. Pressure benefits require
their own approved/implemented rule and are not inferred by this screen.

<a id="source-2c187562f447eff3-equipment"></a>
##### Equipment

Separate equipment slots from carriage slots. Only actual catalog-compatible owned
equipment is selectable. Prior main-gun/hull/power/medical four-module proposal is
not implemented ownership and must not become a compulsory empty-slot blocker.
No free starter-module grant, random affix reroll, paid slot or sell/dismantle action
added. Equipping is a free draft change; upgrades remain a separate existing cost flow.
If upgrade changes relevant stats, revalidate the preparation revision before ready.

Show effect scope: Medical-device healing is not Eve healing or train repair;
power attack speed is not manual-skill cooldown; locomotive output is not engine
speed. Avoid generic 'all stats up' copy. Do not add an emergency acceleration button
from a speed calculator whose actual activation contract is unresolved.

Coop train owner edits and supplies train assets; squad player views only. Partner
modules do not stack on the shared train. Role swap revalidates newly selected
owner's loadout. Accepted edits clear shared readiness; browsing/cancel do not.
Accepted battle freezes configuration; no train swap during combat.

Future tests: legal2/3/4 slots, missing/duplicate Combat, missing second instance,
unavailable Medical, invalid module scope, stale upgrade revision, profile mismatch,
weight clamp and read-only partner UI. Not executed. Policy review/readback only;
local uncommitted, no implementation/art/video/deploy or owner assignment.

<a id="source-2c187562f447eff3-original-screen-verification-scope"></a>
#### Original Screen Verification Scope

Future checks: narrow portrait, longest localization, safe area,5slots, dynamic
cost label, disconnected partner, stale responses, ready/edit races, role swap,
package incompatibility and no hidden controls under footer. No screenshots/UI
implementation/tests executed. Policy comparison and document readback only.
Local/uncommitted; no Unity assignment, art, video, build or deployment. Next visual
wireframe/contract mapping requires separate implementation scope; source field
availability and numeric package decisions remain explicit prerequisites.

<!-- source-body:source-2c187562f447eff3:end -->


<a id="source-f027f3659659d270"></a>
## Admin screen read integration - 2026-09-10

원출처: `WorkspaceRepo/docs/ops/admin-screen-read-integration-20260910.md`  
원문 SHA-256: `9eab94dc922e301e19c3ec65da906930fa6cf11f5a7648c27a5792bcc266c19a`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-f027f3659659d270:start -->
<a id="source-f027f3659659d270-admin-screen-read-integration---2026-09-10"></a>
### Admin screen read integration - 2026-09-10

Scope: Admin Web only. No Server edits, remote query/seed, activation, payment,
manual grants or deployment. This audit is local source evidence, not live proof.

<a id="source-f027f3659659d270-existing-consumption-and-implementation"></a>
#### Existing consumption and implementation

- `/gacha`: existing banners, batch list/detail and result quantities are wired.
  GachaHistory previously used banner response only in its filter. Added a collapsed
  read-only policy table from the SAME query: id/type/status, startsAt/endsAt,
  pickupOperatorId, cost item/quantity, pullCountOptions, pity thresholds,
  balanceVersion/updatedAt. No inferred approval, active status or probability.
- Support account: existing loginBonus, run reward, offline reward references are
  already read-model based. Account-once login bonus is NOT monthly attendance.
- No new placeholder pages or simulated QA products/prices were introduced.

Source examined:
- Runtime Admin src/pages/GachaHistory.tsx, SupportAccountPage.tsx,
  src/shared/gacha/gachaContract.ts, src/shared/api/client.ts.
- Server owner worktree attendance-pass-20260907/server/apps/admin-api/src/index.ts:
  support summary around2982, gacha banners route3017. No shop/month attendance/
  pass/ordinary quest/mail Admin GET routes found in this inspected source.
- Server owner worktree handoff read; Game implementation does not imply an Admin
  route exists or is deployed. SERVER_1 must confirm any newer unpublished shape.

<a id="source-f027f3659659d270-minimum-server-dependencies-proposals-not-existing-endpoints"></a>
#### Minimum Server dependencies (proposals, not existing endpoints)

| Scope / consumer | Read model proposal | Minimum evidence |
| --- | --- | --- |
| S06 approved products / support | Admin shop policy read or support composition | productId, policyVersion, approval/status, currency/item cost, reward summary, purchase limits, period; account purchase/grant references and status |
| S08 monthly attendance / support | support.attendance | policyId/version, serverPeriod/day boundary, sequential day/count, server grant status, grantedAt, reward/ledger refs; calendar approved reward entries only |
| S09 pass / support | support.pass | seasonId/version/start/end/purchaseCutoff, entitlement, level/XP, claimed reward refs, settlement status/at/transaction refs |
| S07 ordinary quests/mail / support | support.ordinaryQuests and support.questMail | questId, system, period, entitlementId, completion/direct-claim/transfer state, mailId, sentAt, expiresAt, claim/expiry state, claimedAt, transaction refs |

Use existing support summary with per-section availability if appropriate rather
than a new operational product. Minimum error/source semantics: absent API/section
means not connected, queried empty list means none, null field means absent, zero
only when returned as zero, failure is classified. Unknown backend statuses remain
visible; do not derive eligibility, grant, settlement or expiry in Admin.

Ordinary quest period-end completed/unclaimed rewards transfer to mail, not wallet.
Unclaimed attachment expires 14 days from actual sentAt; use server expiresAt and
state rather than local arithmetic. Pass season-end earned/unclaimed auto-settlement
is separate, not that mailbox policy. No resend/claim/settle actions in Admin.

Next: Plan routes these dependencies to SERVER_1; consume exact supported safe DTO
after owner delivery. Missing catalogs require policy decisions, not invented QA
values or activation. No public Game bearer endpoint as Admin auth workaround.

<a id="source-f027f3659659d270-verification"></a>
#### Verification

RED: missing BannerPolicy module before implementation. Focused six files/23 tests
PASS after implementation; loading/error/empty/zero/unknown status and no actions.
Full suite/typecheck/build results recorded in canonical Admin handoff after run.
Interactive browser, authenticated live read, deployment and device not performed.

<!-- source-body:source-f027f3659659d270:end -->


<a id="source-cad753599df41f17"></a>
## App Icon Production - 2026-09-08

원출처: `WorkspaceRepo/docs/ops/app-icon-production-brief.md`  
원문 SHA-256: `7d375356c59739f6a6bd639c0bc87818d5cf4aec619ad2126816538d18352a10`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-cad753599df41f17:start -->
<a id="source-cad753599df41f17-app-icon-production---2026-09-08"></a>
### App Icon Production - 2026-09-08

<a id="source-cad753599df41f17-approved-task"></a>
#### Approved Task

User requests app icon production by successor LAST RAIL_Direction_1 in addition
to existing startup-motion work. Successor setup is still pending; this is its
queued work brief, not evidence of message delivery or generated icons.

Create a high-quality LAST RAIL bitmap icon using existing visual direction.
Prefer a distinctive train/rail silhouette readable at small size, not a crowded
scene or tiny full-title text. Propose a small candidate set, preserve source art,
and do not regenerate finished motion layers. Use imagegen skill for artwork.

<a id="source-cad753599df41f17-deliverables"></a>
#### Deliverables

- PRD main icon and clearly distinguishable DEV variant sharing the same identity.
- Google Play square store PNG export, 512x512, sRGB, at most 1024KB;
  no baked rounded corners or external drop shadow, full background coverage.
- Android adaptive foreground/background layers and monochrome mask candidate;
  validate real alpha and mask-safe composition, not a painted checkerboard.
- High-resolution source, 48/72/96px readability previews, round/squircle masked
  previews and metadata/source note. Consult current official specs before export.
- Output under E:/Projects/LASTRAIL/unity-project/QA/Design/AppIcon-20260908/
  using Sources/Exports/Preview; obtain normal write approvals where needed.

Store icon and installed launcher icon are separate deliverables. Startup layer
downsampling policy does not override store icon specifications. Actual Unity
icon binding belongs to Unity after delivery review; no store upload, branding
claim or production release is authorized here. No video needed for this task.

<a id="source-cad753599df41f17-references"></a>
#### References

- https://developer.android.com/distribute/google-play/resources/icon-design-specifications
- https://developer.android.com/develop/ui/views/launch/icon_design_adaptive
- https://support.google.com/googleplay/android-developer/answer/9866151

<!-- source-body:source-cad753599df41f17:end -->


<a id="source-0162a4001283270c"></a>
## 전투 HUD 콘텐츠·미술 인계 r1

원출처: `WorkspaceRepo/docs/ops/combat-hud-content-20260911/ART-SPEC.md`  
원문 SHA-256: `20e5a51ce7a68494c36f75c6a05e9a8da7a135d44d386f725af9abcd70f772b6`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-0162a4001283270c:start -->
<a id="source-0162a4001283270c-전투-hud-콘텐츠미술-인계-r1"></a>
### 전투 HUD 콘텐츠·미술 인계 r1

2026-09-11 Direction. 기존 사용자 지시/승인 A의 구현용 검수 사양. 새 이미지·애니메이션 생성 또는 정식 이름 채택이 아니다. Unity가 실제 적용 파일을 소유하며 Direction은 사양·검수를 제공한다.

<a id="source-0162a4001283270c-이름-조사-결과"></a>
#### 이름 조사 결과

`operator-skill-names-review.json`은 원본 combat-stats의16 kit×active/ultimate=32개 검토 행이다. 초기5의10개는 실제 producer의 `operatorId.active`/`.ultimate` 규칙을 확인했다. 나머지22개는 원문 kit에 대응하는 검토 키이며 런타임 등록 ID를 확인한 것으로 표시하지 않았다.

모든 `canonicalKo`는 null, `approvedForDisplay`는 false다. 새 `candidateKo`는 효과에 맞춘 검토 후보이며 자동 fallback·Unity 정식 이름 등록을 금지한다. 이 파일은 전투 효과/계수/소유 정보의 교체 파일이 아니다.

|현재 producer ID|검토 후보 active|검토 후보 ultimate|
|---|---|---|
|op_kyle|전선 방호|최후의 방벽|
|op_reina|집중 사격|결정타|
|op_eve|응급 처치|생명선|
|op_sera|신호 차단|집중 표적|
|op_mira|현장 수리|기관 재가동|

정확한 미정은 이10개 표시명의 채택이다. 이름 결정이 완료되면 같은 versioned 표시 mapping을 active 버튼·분대장 카드 제목·발동 타이틀이 공유해야 한다. 충전 상태나 cooldown으로 이름 자체를 교체하지 않는다. 나머지22개 후보를 이번 초기5 실행범위 확장으로 해석하지 않는다.

검색 근거: 원본 design/narrative 및 실제 Unity/server의 md/json/cs/ts, 선택 producer. combat-stats에는 효과·target·timing이 있고 표시명은 없다. voice-guide와 localization map의 skill.cue/ultimate.cue는 발동 대사이며 정식 기술명이 아니다. 승인A FINAL-v02는 portraits/skill names가 illustrative임을 명시한다. 따라서 ‘강철의 의지’도 해당 prompt만으로 canonical이 되지 않는다. 모든 과거 대화·외부 파일을 빠짐없이 검색했다는 주장은 아니다.

<a id="source-0162a4001283270c-기준-파일과-최신-지시-우선순위"></a>
#### 기준 파일과 최신 지시 우선순위

기준 이미지: `D:/Project/LASTRAIL/Worktrees/5f93/LASTRAIL/QA/Design/CombatCandidates-20260911/A-final-charge-gauge-v02.png`. 직접 시각 확인했다. 근거 문서 FINAL-v02.md 및 `docs/handoffs/LAST_RAIL_ART_DIRECTION.md`를 읽었다. 이전 SevenScreen showcase보다 이 승인A가 최신 기준이다.

최신 사용자 수정이 승인A 이미지의 예시 글자보다 우선한다: 분대장 actual ultimate명, **N%만**,100% 청색 점등/실제 소모 후 소등; active 실제 이름 고정과 원형 cooldown; 지원3개 정렬. 이미지에 있는 ‘충전100%’, ‘사용 가능’, 임의 대원명/‘강철의 의지’를 복사하지 않는다. 숫자·외형·무기·피해·게이지는 actual snapshot/state를 따른다.

실제 적용 폴더 `E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/UI/ApprovedCombat/`, binding `Assets/_LASTRAIL/Resources/CombatPresentation/approved.asset`. 현재 bare-hull/cannon-module/cabin/console/icons 및 operator polish atlas가 존재한다. 기존 CombatPolish/CombatMotion 보고서의 정식 연속프레임·리깅 미완료 상태를 유지한다. 자산 존재만으로 품질 완료를 판정하지 않는다.

<a id="source-0162a4001283270c-hud-레이어-인수-사양"></a>
#### HUD 레이어 인수 사양

|레이어|내용|검수|
|---|---|---|
|소등 콘솔/카드 frame|건메탈·닳은 철·절제된 황동, 청색 발광과 글자 없는 베이스|0%에서도 빛이 남지 않음; 외곽/장식이 아이콘·한글 영역을 침범하지 않음|
|emissive mask|frame과 같은 캔버스/좌표의 분할 가능한 청색 경로|100% 임계 전이는 순차 점등; 소비 후 off; mask를 base에 굽지 않음|
|초상/아이콘|actual operator/ability에 대응하는 별도 이미지|검은 프레임에 합성된 잘못된 인물·예시 아이콘 사용 금지; 교체해도 frame 좌표 유지|
|title/name|Unity text, 정식 표시 mapping|궁극기 카드/발동 타이틀 같은 이름; active 이름 cooldown 동안 고정|
|charge text/fill|실제 표시값의 N%, 독립 게이지 fill|문구 중복 없음; 보간으로 실제보다 먼저100% 노출 금지|
|cooldown mask/seconds|아이콘 영역의 어두운 원형 가림+남은 초|시간에 따라 가림 해제, 완료 초 제거; 이름을 가리지 않음|
|support3 layout|같은 frame/아이콘 영역/하단 이름 영역|3개 동일 크기·간격·장식 여백, 아이콘 시각적 중앙; 실제 선택 순서 유지|

픽셀 출력 규격은 Unity가 실제 사용할 Rect/atlas 크기로 고정한다. Direction이 다른 크기의 중복 자산을 생성하지 않는다. frame/mask는 동일 canvas·pivot·border를 공유하고 9-slice 시 장식 모서리와 점등 경로가 함께 정렬되어야 한다. alpha 실제값 검사 및 밝은/어두운 배경 합성에서 halo/검은 테두리/그려진 체크무늬를 확인한다. mask off 캡처와100% 캡처를 같은 노출로 비교한다.

가독성 검수는 실제720×1280·안전영역에서 실시한다. 이름이 잘리면 후보를 임의 약칭으로 바꾸지 말고 text 영역/타이포를 조정한다. 임의 초당 빛/재생성 난수를 넣지 않는다.100% 시각과 실제 행동 guard는 별도다. pause/down/target/recovery 제한을 발광 상태로 무효화하지 않는다. 저장 복구 때 이미 완충된 상태는 유지하되 신규 완충 축하를 중복 재생하지 않는다.

<a id="source-0162a4001283270c-연속-공격다운복귀-사양"></a>
#### 연속 공격·다운·복귀 사양

- 채택된 5명 및 실제 표시 enemy별 silhouette/의상/무기/광원을 pose 사이에 유지한다. 같은 actor의 발 앵커·크기·pivot을 유지하고 프레임마다 발이 미끄러지거나 얼굴/무기가 바뀌면 불합격이다.
- 공격은 준비→방출/접촉→반동→복귀의 연속성이 보이도록 한다. 독립 정지 포즈 교체나 전체 그림 이동만으로 정식 연속 애니메이션 완료를 주장하지 않는다. 프레임 수를 임의 공통값으로 강제하지 않고 실제 cadence에서 튐이 없는지 검수한다.
- 무기끝/muzzle·손·발 anchor를 데이터로 기록한다. 실제 공격 이벤트에 맞춰 시각 action을 재생하며 projectile/VFX가 피해 판정을 호출하지 않는다.50ms simulation·배속·pause·복구는 Unity의 actual event/tick을 따른다.
- 다운은 서 있는 그림을 단순 어둡게 만든 것과 구분하고 바닥 접촉/실루엣을 유지한다. 복귀는 일어남→ready 흐름으로 연결하되 시각 지연 때문에 authoritative 행동시점을 바꾸지 않는다. down/recovery의 상태별 프레임 누락을 기록한다.
- **캐릭터 피격 모션과 hit 오버레이를 새로 제작하지 않는다.** 실제 피격0.16 simulation초 색상 반응만 유지하고 공격 모션을 중단·교체하지 않는다. 기존 문서의 ‘피격 상태 프레임’ 항목은 최신 지시로 대체한다. 차량 피격 표현은 별도다.

<a id="source-0162a4001283270c-포탑차륜창문-사양"></a>
#### 포탑·차륜·창문 사양

- 포탑 base/barrel/muzzle을 분리하고 mount pivot·barrel axis·muzzle point를 제공한다. 조준 시 포신만 회전, 발사 반동/복귀 시 접합부 벌어짐·축 어긋남·차체 잘림을 확인한다. 차체별 장착 좌표와 스킨 교체 검수는 별도 목록으로 유지한다.
- 차륜은 wheel center/radius/axle 및 회전 가능한 분리 그림 또는 리깅을 제공한다. 외형상 실제 이동거리와 회전이 일치해야 하며 실제 모델 속도를 임의 시각 속도로 대체하지 않는다. sprite 기준 반지름/스케일을 단일 mapping에 기록한다.
- 외부와 창문 풍경은 같은 mapId/진행방향/누적거리/복구 상태를 투영한다. 랜드마크 world 위치를 공유하고 camera/window offset 차이만 허용한다. 독립 time×임의계수/무작위 phase 금지. 원경/근경은 깊이별 시차만 허용한다.
- cabin은 투명 window mask/중립 유리/내부 조명을 분리한다. 창문에 특정 맵을 구워 넣지 않는다. 다른 cabin skin의 창 좌표·clip mask는 mapping으로 관리한다.
- 동일 랜드마크를 exterior/window에서 비교 촬영하고 pause·배속·맵교체·restore 직후 점프/이중 속도/반대방향을 점검한다. 전체 motion 영상만으로 이 공통 투영 검증을 대체하지 않는다.

<a id="source-0162a4001283270c-역할과-완료-증거"></a>
#### 역할과 완료 증거

Unity: 실제 자산 재사용/필요한 분리 제작·연속프레임 또는 rig·적용 파일·actual load/state/영상 소유. Direction: 본 사양 및 표시명 후보, 승인A 대비 미술 검수. 제작 착수 전 기존 채택 파일/담당을 공유해 중복 image generation을 피한다. 이번 Direction 턴은 이미지 생성 없이 사양을 전달한다.

변경 구간 QA만: 미충전→완충→사용→소등, active 이름고정/원형가림, 지원3개, 반복공격/down/recovery, 포신/차륜, 외부/창문 랜드마크. 실제 서버run 여부와 기술 오버레이를 분리한다. 결과물마다 prompt 원문·참조·원본·채택/미채택·실제크기/alpha·hash·적용path를 기록한다. 현재 producer 이름10개의 채택만 콘텐츠 미정으로 보고하며 이 때문에 나머지 구현을 중단하지 않는다.

<!-- source-body:source-0162a4001283270c:end -->


<a id="source-78eacf9c42a3e722"></a>
## 전투 연출 제작명세 — 2026-09-12

원출처: `WorkspaceRepo/docs/ops/combat-presentation-production-breakdown-20260912.md`  
원문 SHA-256: `76009340c7a800886ec8cf5ce32f5e7e73eb8bff9e09933a1bcd09968c992ced`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-78eacf9c42a3e722:start -->
<a id="source-78eacf9c42a3e722-전투-연출-제작명세--2026-09-12"></a>
### 전투 연출 제작명세 — 2026-09-12

Direction 소유 산출물. Plan_3 상세 규칙 문서와 파일을 분리한다. 기준은 docs/design/combat-command-loadout-and-leader-presentation-20260911.md 8–10절 및 그 문서의 5–6절 연출안, 최신 Unity canonical/CombatHud REPORT다. 아래 컷 길이·자산 분해·검수 수치는 제작 제안이며 게임 수치 확정이나 구현·품질 승인 기록이 아니다. 승인된 산업형 A, 고정 전장, 현재 HUD를 보존한다. 호스트 연결과 독립해 진단 제작 가능하나 실제 서버 연결 완료를 주장하지 않는다.

<a id="source-78eacf9c42a3e722-1-기존-규칙과-제작-선택"></a>
#### 1. 기존 규칙과 제작 선택

사용자 수락: 전체/간소화/첫 사용만 전체, 현재 연출 1회 건너뛰기. 위험·대상·실제 효과 피드백은 모든 모드에 유지한다. 기본 모드·저장 범위·first-use key/소비/복구는 [Plan 상세 규칙 §5](51_UI_ART_DETAIL_CONTRACTS.md#source-7dd86e35b7c512fd)의 2026-09-12 로컬 구현 기준으로 확정되었다. 아래 최신 반영 절을 따른다. 이는 구현 완료·사용자의 과거 개별 수치 승인·출시 정책 확정 주장이 아니다. 8절의 과거 전달 차단은 현재 상시 승인/정상 전달 및 Plan_3 승계로 대체된 역사다.

제작 경로 비교: (A) 승인 원화의 고정 하체+짧은 상체 rig를 실제 이벤트에 연결하는 안을 우선한다. 작은 공격 범위부터 접지·가림면 검증이 가능하다. (B) 독립 수작업 연속 프레임은 큰 포즈 변화에 유리하지만 컷마다 정체성·발·칼 위치의 제작비가 높다. (C) 현재 단일 pose 전환은 진단/기존 완료분 보존용으로 유지할 수 있으나 최종 연속동작 합격으로 쓰지 않는다. 반복 imagegen sheet 편집은 중지한다. A도 가려진 몸통/관절 원화가 없으면 실패할 수 있으며 성공을 전제하지 않는다.

<a id="source-78eacf9c42a3e722-2-실제-입력과-표현-책임"></a>
#### 2. 실제 입력과 표현 책임

현행 Unity 소스 사본 docs/handoffs/CombatHudReadModel.cs에는 actors.attackDue/attackWindup/attackTarget/attacking/casting/hp/recoverAt/protected, hits, casts.id/actor/kind/status/due/targets, tick/outcome이 존재한다. ApprovedCombatView.cs 사본은 pose UV/발바닥 보정, HP 전이, hit tint, 궁극기 연출 카운터를 사용한다. 이번 E:/Projects/LASTRAIL/unity-project/Assets/Scripts 경로는 찾지 못했으므로 이를 live source 직접 검증이라고 부르지 않는다. 사본과 최신 실행 소스의 일치는 Unity가 구현 시 확인한다.

| 의미 | 연출 시작/정리 조건 | 부족한 계약 확인 |
|---|---|---|
| accepted command | 저장된 수락만 카드 확인 1회. 입력/대상 선택은 프리뷰 | receipt 게시와 cast 시작을 분리; 요청 실패는 확정음 없음 |
| cast/windup | 실제 시작/진척에 팔 준비·대상 표시 | 상태 조회마다 재시작 금지; command ID와 재생 세대별 재연 억제 |
| impact/effect applied | 실제 대상/적용된 효과에 타격·회복·보호막 표시 | HP 차분만으로 회복/보호막/피해 출처를 추정하지 않음. producer의 효과별 식별자·수혜자·실제 delta/layer/만료 정보를 매핑 |
| interrupt/cancel | 실제 중단 시 남은 장식·미발생 트레일 제거 | 실제 중단 이유/시각 식별 필요. 이미 committed impact를 source down만으로 취소하지 않음 |
| down/recovery | 권위 상태 전이 때 1회 동작, 현재 상태는 복원 가능 | HP0→양수만으로 부활 종류·보호시간을 발명하지 않음; 실제 recover/protected 값 우선 |
| threat/outcome | 실제 위협과 결과 상태 | 예정 공격과 중단/실패/결과의 근거 이벤트 필요. 화면 애니메이션 완료가 승리/보상 확정 아님 |

표의 의미 이름은 새로운 wire 필드가 아니다. Unity/Server의 확정 계약에 대응표를 작성한다. 불충분한 효과 이벤트는 명시적 미연결로 남기고 성공 VFX를 추정 생성하지 않는다. 복원은 현재 지속효과만 재구성하고 지난 one-shot/SFX/진동을 재생하지 않는다.

<a id="source-78eacf9c42a3e722-3-레이어와-가림-금지"></a>
#### 3. 레이어와 가림 금지

뒤→앞 순서: 공유 원경/창밖 → hull·고정 시설 → 접지 그림자 → 하체/몸통/후방 팔 → 머리/전방 팔/손·무기 → 대상 국소 VFX → 전장 상태·위협 → 현재 HUD → 비필수 컷인 → 최상위 필수 경고/입력 안전영역. 컷인은 겹침 검사를 통과한 공간에서만 표시한다. 현재 콘솔·카드·지원 버튼의 배치나 hitbox를 바꾸지 않는다.

720×1280 기준 컷인 상한은 원안의 화면폭28%·높이18%(201.6×230.4px)이며 자동 맞춤 크기다. 이는 고정 배치 좌표나 전체 화면 면적 허가가 아니다. 카드 위 아군측 후보 영역에서 실제 RectTransform 경계로 열차/대원 HP, charge N%, cooldown초·스킬명, 적 castbar, 대상 표식, 위험 경고, 지원3버튼, pause/skip 안전영역과 겹치면 컷인을 생략한다. 이름 미정은 현행 명칭 미정 표시를 유지한다.

위협 경고가 올라오면 컷인보다 우선하며 skip을 눌러야 볼 수 있게 만들지 않는다. 동일 위치 효과는 얇은 윤곽+아이콘으로 정리하며 대상 실루엣과 적 공격 선행동작을 덮지 않는다. 전체 섬광·전체 timeScale/줌/히트스톱 없음. 선택 미세 흔들림은 원안 범위 내부 전장2px/80ms 이하 제작안, HUD/외부전장 고정; 낮은 자극에서는 제거한다.

<a id="source-78eacf9c42a3e722-4-컷별-제작표"></a>
#### 4. 컷별 제작표

| 컷 | 실제 동기점과 구성 | 종료·예외 / 제작물 |
|---|---|---|
| 기본 근접 공격 | attackDue까지 짧은 팔 당김→상체 전진, 손-칼 socket 유지. 실제 impact에서 칼끝 대상 국소 접촉·짧은 스파크 | 하체 고정. 준비 길이는 실제 windup에 맞춤, 4프레임 GIF의 고정180ms를 실제 판정에 쓰지 않음. 미발생 impact에 타격음 금지. 가려진 몸통/팔 관절 원화 필요 |
| 기본 원거리/기본 포탑 | 실제 준비에서 조준, 확정된 발사 시 총구/포신 반동, 실제 impact에서 대상 스파크 | 포신만 반동, body 고정. 화면 트레이서 도달로 피해 판정 금지. 실제 발사 근거 없으면 impact 표현만 유지. 기본 rig 재사용, 총구 socket 검증 필요 |
| 피해/피격 | 실제 hit에 0.16 simulation-second 적색→원색 tint | 캐릭터 hit pose/흔들림 모션 추가 없음. 동시 hit는 구현 규칙을 따르며 총 HP 변화와 중복 수를 확인 |
| 보호막/방어 | 실제 수혜자 윤곽에 육각선 펼침, 지속 중 얇은 경계와 상태 표시 | 실제 layer 소멸/만료 때만 제거. 모든 아군을 보호한 것처럼 확장 금지. 얇은 경계·전개 마스크 필요 |
| 회복/수리 | 실제 회복은 수혜자 아래→위 짧은 펄스, 수리는 실제 열차/장치 지점의 공구·용접광 | 녹색/청록만으로 종류를 구분하지 않고 작은 효과 아이콘 병기. delta0이면 HP증가 숫자 없음. 미보유 장치를 새로 그려내지 않음 |
| 피해 지원/정밀/범위 | 실제 타격 대상에 선·작은 충격. 범위 경계는 실제 effect 범위만 | 장식 폭발로 추가 AoE를 암시하지 않음. 다중타격은 event별 작은 충격, 주효과음 중첩 제한 |
| taunt/mark/interrupt | 실제 상태 부여에 수혜자·표적 아이콘, interrupt 성공에 해당 적 castbar 종료 피드백 | cast 실패와 방해 성공을 구분. 상태 유지/만료는 실제 계약. 적 경직/추가 데미지 발명 금지 |
| 분대장 전체 | 수락 카드 확인→실제 cast에 소형 얼굴·상체 컷인과 연결선→실제 효과별 국소VFX→퇴장 | 소형 컷인 노출0.28s+퇴장0.12s 원안. 실제 windup 짧거나 위협 겹침이면 줄이거나 생략; impact 지연 금지. 다중효과는 실제 수혜자별 매핑 |
| 분대장 간소화 | 카드 확인·대상 표시·실제 효과VFX만 | 컷인·장식 연결·카메라 강조 없음. 판정·효과시각·게이지·cooldown 동일 |
| 첫 사용만 전체 | Plan이 확정한 같은run first-use 상태에 따라 위 두 경로 선택 | 입력취소/수락실패를 임의 첫사용 소모로 삼지 않음. 권장안은 저장된 수락 뒤 전체 연출 시작 기록, 복구 재연 억제. Plan §5 기준을 검증 fixture에 표시 |
| 1회 skip | 현재 presentation token의 컷인/장식/미세 camera offset만 즉시 종료 | 이후 impact·cast·경고·target 유지, 반복 눌림 idempotent. 옵션 영구 변경이나 다음 컷 자동skip 아님 |
| interrupt/down | 중단 사건에 아직 남은 준비 연출 정리. 실제 down 때 무릎→중심하강→정지 상태 | down 실루엣이 이웃 발/HP를 침범하지 않도록, 무기 소유 socket 유지 또는 명시 drop원화. 기발생 shield/effect를 임의 취소하지 않음 |
| recovery | 실제 recover 시작/복귀 가능 상태에 손짚기→몸통세우기→기본자세. protected는 실제 값만 | 앉은 상태를 한프레임 뒤집어 세우지 않음. 실제 행동 재개를 clip 끝까지 지연하지 않음. 미완성 clip은 진단 표시 |
| 위협 경고 | 실제 공격/위험 발생·예정 시각에 표적 방향·castbar·한번 경고음 | 중단/만료 시 원인에 맞게 해제. 낮은 자극에서도 텍스트/방향 대비 보존. 장식보다 상위 |
| 결과전환 | 실제 terminal outcome을 받은 뒤 전장 마무리→간결 결과표시→기존 정산/복구 UI | 적 전멸 장면만으로 승리/보상확정 금지. unresolved는 확인중/복구필요. skip해도 run/저널/미확인 청구 유지 |

<a id="source-78eacf9c42a3e722-5-자산-제작-인벤토리와-rig-납품"></a>
#### 5. 자산 제작 인벤토리와 rig 납품

재사용: CombatPolish의 승인5대원+scout pose/portrait, console/icons/hull/cabin/차량12종 polish 묶음; CombatHud의 console-off-v02와 기본 turret-rig, 기존 기본 바퀴 UV, 기존 shield/hit 표현. 기존 단일 pose는 source reference/임시표현이지 연속동작 완성품이 아니다. 현행 Latest 승인화면 PNG는 배경 설계 기준으로 보존한다.

실제 부족: (1) Kyle 분리 하체/클린몸통/머리·스카프/양팔상하박/손칼과 가려진 면, 관절 겹침; (2) 공격·down·recovery mesh/weights/bones/clip; (3) 나머지4대원+scout 각각의 같은 제작 규격; (4) 보호막전개/회복/수리/표적/방해/위험 표식의 실제 alpha·atlas·재질 확인; (5) 5분대장 소형 컷인 safe-crop(기존 portrait로 가능한 것부터, 새 full-screen 원화 불필요); (6) 아래 SFX 실파일·권리/출처·재생 연결. 실제 없는 리소스를 mockup으로 충족 처리하지 않는다.

납품 묶음은 원본/alpha export, bone hierarchy, mesh 및 weights, local pivot/footL/footR/hand/weaponTip/muzzle/target anchor의 좌표계·단위, clip와 key times, imported GUID/경로, 실제 source hash, 진단 scene/run/profile, event-대응표, 캡처와 측정CSV를 포함한다. 발 anchor는 프레임 눈대중이 아니라 root-local로 고정하고 mirrored actor에서도 손칼/총구 방향이 맞아야 한다. 무기tip 추정 좌표는 VERIFIED socket으로 바꾸지 않는다.

발·관절 QA 제작 목표: 720기준 고정 하체 공격에서 양발 root-local 오차 각각1px 이하, planted 구간 접지면 관통/공중뜸1px 이하. 회전/가림이 필요한 관절은 alpha gap·손칼 분리0, mask로 관절 누락을 숨기지 않는다. 이 수치는 새 합격 목표이며 현재 v05는 뒷발25px 차이로 실패다. down/recovery처럼 발이 이동하는 구간은 고정발 허용치를 억지 적용하지 않고 접촉 키프레임/이동 구간을 사전 라벨링한다. 2D rig 실패 시 부위 원화 복원/수작업 clip 경로로 전환 여부를 Plan에 근거와 함께 보고한다.

<a id="source-78eacf9c42a3e722-6-음향-큐-제작-목록"></a>
#### 6. 음향 큐 제작 목록

| 큐 | 트리거·재생 제한 | 필요한 소스/예외 |
|---|---|---|
| UI press/accepted/charge-full | press는 입력, accepted는 저장확정, full은 실제 임계 통과 각1회 | 짧은 금속/전자 클릭·확정음·충전음. 복원시 one-shot 재생 없음 |
| swing/shot/impact | 실제 공격 준비/발사/impact로 분리 | 칼 스윙·금속 접촉·총성/포탑·실제 타격. 이벤트 없는 발사음 추가 금지 |
| shield/heal/repair | 실제 적용 대상 효과 | 전개, 짧은 상승음, 용접/수리. 지속 루프 기본 불필요 |
| mark/interrupt/down/recovery | 실제 상태 전이 | 작은 확인/차단/착지/장비 일어섬. 음성/신음은 승인 보유 음성이 있을 때만 |
| leader cast/major impact | 카드확정/발동/주효과 최대3층 | 다중타격 큰 효과음은 첫 주요타격1회. full/simplified도 필수 효과 cue 유지 |
| threat/result | 실제 위협/결과 | 위험음 우선, 다른 SFX duck은 선택 제작안. 승/패 음악은 실제 outcome에만 |

SFX/BGM/voice volume와 OFF, 낮은 자극/진동OFF를 존중한다. mute를 효과 실패로 표시하지 않는다. 동시 재생 상한/duck dB·길이는 믹싱 QA에서 정할 제작 변수이며 게임규칙이 아니다. 임의 유료 구매·미보유 음성 합성은 이 명세 범위 밖이다.

<a id="source-78eacf9c42a3e722-7-정확한-qa-프레임검수-시퀀스"></a>
#### 7. 정확한 QA 프레임·검수 시퀀스

새 진단 캡처는 720×1280/60fps 고정 제안. 기존24fps HUD 영상과 별도 증거로 보관한다. frame0은 녹화 시작, 각각 event가 최초 게시된 프레임을 Fa(accepted), Fc(cast), Fi(impact), Fx(interrupt), Fd(down), Fr(recovery), Ft(threat), Fo(outcome), Fs(skip)로 실제 로그에서 얻는다. 각 프레임에는 simulation tick/시각, presentation 시각, speed/pause, actor/target/command/effect ID, HP/shield/charge/cooldown, mode/marker/skip token을 비밀값 없이 기록한다. 아래 오프셋은 검사 지점이며 사건 발생 프레임을 미리 조작하지 않는다.

| 시퀀스 | 저장할 프레임 | 확인 사항 |
|---|---|---|
| 기본공격/포탑 | Fc−1,Fc, windup25/50/75%가 최초 도달한 프레임, Fi−1,Fi,Fi+1,Fi+6,Fi+12 | 준비/칼·총구socket/실제 impact/복귀; Fi 전 HP피해VFX 없음. 실제 timeline에 부족한 샘플은 N/A 명시 |
| full 컷인 | Fa−1,Fa,Fc,Fc+8,Fc+17,Fc+24,Fi−1,Fi,Fi+1 | 상한/가림 검사와 실제 효과시각. 짧은windup에서 컷인 축소/생략 확인 |
| charge | 96% 마지막 프레임,100% 첫 프레임과+1/+11,consumption−1/0/+1 | N%·rim과 enabled 독립. 25회 적격4%는 opt-in 로컬규칙 fixture 한정 |
| skip | Fc+1 / Fi−1 / Fi 각각 별도 run으로 skip 주입, Fs−1,Fs,Fs+1,Fi,Fi+1 | 바로 장식 정리/원복, 효과누락·이중효과0. 동일token에10회 입력도 동일결과 |
| interrupt/down/recover | Fx−1/0/+1/+6, Fd−1/0/+1/+6/+12/+24, Fr−1/0/+1/+6/+12/+24 | 실제 취소/committed impact 구분, 접지·관절·무기·protected 정합. ally down/recover 사건이 실제 있어야 PASS |
| 위협 겹침 | Ft−1,Ft,Ft+1 및 컷인 겹침 최대 프레임 | 경고·적cast·대상·HUD rect 가림0, skip 없이 판독 가능 |
| 결과·복구 | Fo−1,Fo,Fo+1,Fo+18,Fo+36; restore 첫프레임과+1 | terminal과 정산/claim 구분, restore one-shot0, unresolved 확인중 유지 |

연속60fps 전체 MP4와 무손실 지정 PNG+CSV를 함께 저장하고 누락/프레임드롭을 기록한다. 컷인0.28/0.12초는60fps에서17/7프레임 근사이므로 실제시간도 기록한다. 고정 초과 지점을 검사하는 것이며 발동/피해 tick을 rounding하지 않는다.

같은 initial state·command stream을 full/simplified/first-use-full 및 skip 유무로 돌려 authoritative event stream·최종 gameplay state·charge/cooldown/결과가 동일한지 검증한다. 표현 marker/시간 같은 비게임 필드를 비교에서 명시 분리한다. speed1/1.5/2, pause/resume, disabled100%, 취소/거절/저장실패, cast중down, 다중타격, shield-only/0damage, 첫사용skip·동일run복원·새run, 설정 전환, 저자극/mute, 소형화면·안전영역을 포함한다. marker정책은 Plan §5로 fixture를 고정하며 실행 전 PASS 금지.

<a id="source-78eacf9c42a3e722-8-진행-순서와-증거-상태"></a>
#### 8. 진행 순서와 증거 상태

1. Unity는 최신 소스와 사본 대응, 실제 cast/impact/interrupt/down/recovery 입력 식별 가능성을 확인한다. 데이터가 부족한 것은 필요한 최소 event mapping으로 보고하고 독립 rig 진단은 계속한다.
2. Kyle 하나의 짧은 공격 rig를 만들어 접지/socket/가림면을 Direction에 제공한다. 통과 뒤 down/recovery와 나머지5캐릭터로 확장한다. 현재 HUD/host 구현을 교체하거나 기다리게 하지 않는다.
3. 기존 효과 자산 재사용 가능성을 alpha/가독성/타깃별로 검수하고 부족 큐만 제작한다. first-use/skip 설정은 확정된 Plan §5 로컬 구현 기준으로 연결한다.
4. 위 사건별 실제 영상/프레임/CSV와 동일 command-stream 검증을 제출한다. Direction은 외형·연속성·가림·socket·음향 필요 충족을 검수, Unity는 실행 연결, Plan은 규칙/완료 판정을 통합한다.

이번 실제 수행: 기준8–10절 및5–6절, 최신 HUD/잔여감사와 Unity 소스 사본을 읽고 제작명세를 작성·자체 대조했다. 코드·rig·새이미지·음향·영상·게임 테스트는 생성/실행하지 않았다. 모든 신규 QA 행은 미실행이다. 원본 로컬 미커밋 문서이며 production/DEV/PRD/device 성공 주장이 없다.

<a id="source-78eacf9c42a3e722-9-plan-로컬-구현-기준-인수--2026-09-12-후속"></a>
#### 9. Plan 로컬 구현 기준 인수 — 2026-09-12 후속

[combat-detail-and-presentation-v1-20260912.md §5–6](51_UI_ART_DETAIL_CONTRACTS.md#source-7dd86e35b7c512fd)를 직접 읽었다. 앞 절의 first-use 정책 결정 대기는 이 기준으로 해소한다. 제작시간·rig 허용치 제안 및 실제 구현/검증 대기는 별개다.

- 기본 first-use-full. 기기 로컬 표현·음량 설정은 해당 설치 계정 간 공유하되 소유·진행·전투 정보와 분리한다. 설정 변경은 next cast부터이며 현재 컷인 재시작/확대 없음.
- marker는 owner/run/선택leader/표현version으로 분리한다. 저장된 시전 확정 후 presentation token 시작 때 첫 기회를 소비한다. 핵심 경고 때문에 자동 간소화된 첫 시전도 소비한다. 입력 취소·실패는 소비하지 않는다.
- 현재1회skip은 현재 장식만 닫고 첫 사용 소비를 유지한다. 효과/판정/게이지/cooldown에 영향 없음. marker 저장실패는 해당run 메모리 억제, 복원 불확실하면 간소화한다. 소유전환/종료는 이전marker를 정리하되 gameplay journal을 삭제하지 않는다. marker는 gameplay snapshot/hash 밖이다.
- 신규 흔들림/섬광/진동 default OFF, 기존 저장값 존중. 원안의2px/80ms 흔들림은 ON일 때만 고려할 제작 상한이며 기본 재생 지시가 아니다.
- QA 추가 고정: 첫 실제 cast가 위협으로 간소화된 뒤 다음cast도 간소화; token시작 전 거절은 marker 미소비; marker 저장실패 뒤 같은run 반복·restore 불확실은 간소화; 계정전환에서 표현설정 공유와 전투marker 분리; next-cast 변경 및 신규OFF/기존ON 존중을 각각 실제 검증한다. 아직 모두 미실행이다.
- Plan §6의 신규 지원7효과는 schema/basis/target selector/상태/중단 계약 확장이 필요한 검토안이다. 본 문서 지원 컷의 의미 분류를 현재 실행 가능한7효과라고 해석하지 않는다. 현재3종과 실제 입증된 kit effect만 기존renderer에 연결하고 신규7종은 계약·회귀·profile 채택 전 성공 VFX를 추정 표시하지 않는다. 실제 r6 왕복은 이 확장을 기다리지 않는다.

실제 소스 경로 정정: E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/Battle/CombatHudReadModel.cs 및 ApprovedCombatView.cs의 존재·파일크기를 직접 확인했다. 앞서 Assets/Scripts 경로 실패는 잘못된 경로 관측이며 live source 부재가 아니다. 이번 후속은 경로 확인과 명세 정정만 수행; live 파일 내용/사본 동등성·실행/이벤트 결과를 새로 검증하지 않았다.

<a id="source-78eacf9c42a3e722-10-초기5명-activeultimate-10개-실제-kit별-제작안--2026-09-12"></a>
#### 10. 초기5명 active/ultimate 10개 실제 kit별 제작안 — 2026-09-12

<a id="source-78eacf9c42a3e722-101-근거와-현재-표현의-한계"></a>
##### 10.1 근거와 현재 표현의 한계

명칭은 원본 combat-hud-content-20260912/skill-names.json의 신규 채택10개를 그대로 사용한다. 수치는 docs/design/combat-stats-v1.json의 /kits/0..4 실제 원천이며 새 밸런스 제안이 아니다. 현재 producer normalizeEffect/normalizeAbility와 실제 Assets/_LASTRAIL/Scripts/Battle/CombatSkillCommands.cs의 SkillTargets/ExecuteSkill/ApplySkillEffect를 함께 대조했다. 관측 파일 SHA·시각은 [source-observation](../../../WorkspaceRepo/docs/ops/combat-cue-evidence-20260912/source-observation.json)에 고정했다.

중요한 실제 차이: (1) 현재 enemy_focus_any는 코드상 내부 생존 적 focus→fallback이며 양 lane 임의 지정이 아니다. external_focus만 producer가 EXTERIOR_ENEMY_FOCUS로 변환한다. (2) all_allies는 HP>0인 대원이며 열차/장치 제외다. (3) 기본 cmd.Targets가 모두 죽으면 현재 구현은 재선택+재windup할 수 있다. targetOverride는 commit에서 다시 resolve하고 효과 적용 전에 수혜자 목록을 고정한다. (4) 현재 interrupt는 대상의 Due>Tick 기본공격만 취소하며 이미Due된 공격/스킬/채널 취소가 아니다. (5) APPLIED는 명령 처리상태이며 각 효과의 실제 delta>0/방해성공을 뜻하지 않는다.

현재 ApprovedCombatView는 공통 Skill 효과·약0.55초 궁극기 컷인, hit/HP전이/pose UV를 사용한다. 아래 개별 연출·full/simplified·효과별 성공 이벤트가 이미 구현됐다는 뜻이 아니다. ReadHudState는 hits와 casts를 제공하지만 healing/shield/status별 독립 effect result 전체를 이 코드에서 확인하지 못했다. 음향도 Battle 경로에서 직접 cue 호출을 찾지 못했고 다른 Presentation/Battle의 HUD cue 메서드는 존재하므로 현재 approved renderer의 실제 연결을 별도로 확인한다.

producer에 남은 ultimate firstReady15000/cooldown45000은 역사적 일반 kit 값이며 현 opt-in charge 모드를15/45초로 되돌리는 근거가 아니다. 아래 windup만 현재 전달 kit에서 읽은 준비 길이로 다룬다. 실제 시작/적용/취소/종료 tick이 표현보다 우선한다.

<a id="source-78eacf9c42a3e722-102-10행-준비--실제-적용--회수수혜금지-오인"></a>
##### 10.2 10행: 준비 → 실제 적용 → 회수·수혜·금지 오인

A=active, U=선택 분대장 ultimate. 계수는 소스 기준량이며 armor/package/passive/cap/현재HP 등 적용 후 실제 결과와 다르다. 화면 수치는 실제 결과만 표시한다.

| ability ID / 채택명 | 실제 kit 근거·수혜자 | 준비 → 적용 → 회수 | 금지 오인 |
|---|---|---|---|
| op_kyle.active / 전선 방호 | /kits/0/active,350ms. ally_lowest_hp 1명에 casterHP18% shield6s; 별도 enemies_front_2에 taunt4s | 손·상체 짧은 방호신호→실제 shield발급 대상에 얇은 육각선, 실제 taunt수혜 내부적 최대2에 시선/도발표식→선만 회수하고 shield/taunt표식은 각각실제만료 | Kyle자기보호로고정 금지, 실물방패장비추가 금지, 모든아군/외부적도발 금지, shield증가0이면 획득숫자없음 |
| op_kyle.ultimate / 최후의 방벽 | /kits/0/ultimate,600ms. all_allies 생존대원 각 casterHP22% shield8s | 짧은 결의포즈/선택시소형컷인→실제 shield수혜자 각각윤곽→동시에장식회수, layer별실제잔량/만료표시 | 열차·장치보호/무적/피해감소/부활 추가없음. 표적목록에없는대원을 거대돔으로 포함하지 않음 |
| op_reina.active / 집중 사격 | /kits/1/active,450ms. 내부focus1명, casterATK400% damage | 현재표적조준·기존총기socket→실제피해사건에서대상국소충격/HP또는흡수→총구/궤적짧게제거·준비자세회수 | 명칭의사격을다중타격으로늘리지않음; 탄도도착이판정아님, 외부선택허용으로확대금지 |
| op_reina.ultimate / 결정타 | /kits/1/ultimate,800ms. 내부focus1명, casterATK1000% damage | 안정조준·선택시소형인물컷인→하나의실제hit에단일주충격→잔광정리/조준회수 | 자동치명타/처형/관통/추가AoE없음, 높은계수도실제HP변화보다큰숫자로표시하지않음 |
| op_eve.active / 응급 처치 | /kits/2/active,400ms. ally_lowest_hp1명, casterATK700% heal | 실제대상표시·손지원신호→실제HP증가에수혜자위로짧은펄스→장식회수 | 최저HP는절대HP가아닌비율·slot·id기준. 열차수리/부활/HoT/보호시간추가없음. 과회복수치를지급처럼표시금지 |
| op_eve.ultimate / 생명선 | /kits/2/ultimate,650ms. all_allies 생존대원, casterATK800% heal | 선택시소형컷인/수혜범위표식→각실제HP증가대원에동시짧은회복펄스→회수 | down대원포함/열차회복/지속치유/정화없음. 여러수혜자를1개큰수치로합쳐단일타깃회복처럼표시하지않음 |
| op_sera.active / 신호 차단 | /kits/3/active,300ms. 내부focus1명, damage180%ATK→interrupt→mark10%6s | 얇은표적선·신호준비→실제hit/실제미래기본공격취소/실제mark를분리표시→장식정리·mark는실제만료까지 | 스킬·채널중단/스턴/침묵으로그리지않음. 타격으로대상사망시후속효과가실제로미적용이면성공표식없음 |
| op_sera.ultimate / 집중 표적 | /kits/3/ultimate,500ms. 내부focus1명, damage350%ATK+mark20%10s | 선택시컷인·조준격자→실제hit와실제mark→조준선회수·지속mark만유지 | 이름에서아군집중사격명령/피해추가pulse/interrupt를발명하지않음. mark중첩/cap반영없는강화숫자금지 |
| op_mira.active / 현장 수리 | /kits/4/active,600ms. train1,trainHP5% repair | 실제열차수리점/공구준비→실제trainHP증가에국소수리광→광회수 | 대원치료/장치부활/독립정비칸소환없음. repaircap/잃은HP만큼만표시 |
| op_mira.ultimate / 기관 재가동 | /kits/4/ultimate,800ms. train1,trainHP10%repair+train_damage_buff15%10s | 선택시컷인·기관부준비→실제수리량+실제train강화상태아이콘→광회수·실제buff만유지 | 현재버전의train원천강화를대원/모든장치/포격칸까지임의확대금지. 즉시기본공격·엔진부활·속도상승없음 |

효과 배열 순서와 실제commit수혜목록을 로그에 남긴다. Sera처럼 같은effect묶음의damage로사망한표적은 현재코드의후속.Where(Hp>0) 때문에다음효과가빠질수있다. 효과별관측없이 APPLIED로완전성공아이콘을모두점등하지않는다.

<a id="source-78eacf9c42a3e722-103-10행-자산sfx모드중단복원검수"></a>
##### 10.3 10행: 자산·SFX·모드·중단/복원·검수

P(id)=현재 UI/ApprovedCombat/{id}-polish-v01.png+CombatPresentationAssets.ActorAtlas/PoseUv. V=현재 assets.Skill/Hit/Shield 및 IconAtlas. 둘 다 보유 경로이지 연속rig/개별effect 완성 증거는 아니다. 아래 wav ID는 §12의 보유 임시음이며 새 재생연결은 제안이다.

공통 X: 수락 전취소/거절에는프리뷰만제거, cast중source down은실제CANCELLED_DOWN에따라회수. target전멸재windup은현재due/target을갱신하고직전표적충격금지. 이미발급된shield/HP변화는임의취소하지않음. 실제interrupt가없는스킬에가짜중단을주입하지않음. restore는현재지속상태만복원하고지난cutin/SFX/진동은0회. 만료는실제expiry, source down시taunt제거도실제status를따른다.

A모드: 분대장연출옵션은일반active에새인물컷인을추가하지않는다. 양모드모두같은준비/대상/효과피드백. U모드: full은선택분대장소형컷인+장식연결, simplified는카드강조+실제효과만. first-use/skip/경고자동간소화는Plan§5기준. 어떤모드도cast/impact/cooldown을앞당기지않는다.

| ability ID | 재사용 / 실제 부족 | SFX cue 제안 | full/simplified 및 예외 | 사건별 검수 프레임 |
|---|---|---|---|---|
| op_kyle.active | P(Kyle),V.Shield / 고정발상체rig·clean몸통/손socket·수혜자별shield/taunt결과 | cast combat_skill;shield combat_shield;실제taunt combat_debuff 각command/effect군1회 | A모드+X. source down으로실제taunt사라지면표식회수, shield정책과분리 | Fc−1,0,+10;FeShield−1,0,+1;FeTaunt0;Fexpiry−1,0,+1;Fx/Fd/Fr±1 |
| op_kyle.ultimate | P(Kyle),V.Shield / 생존대원별발급로그·소형safe-crop·layer만료표현 | cast combat_ultimate;실제발급군 combat_shield1회 | U모드+X. 실제수혜0이면보호완료축하없음, down수혜제외 | Fc−1,0,+18;각Fe±1;첫fullFc+17/+24;shield만료±1;Fs±1 |
| op_reina.active | P(Reina),V.Hit/Skill / 총구socket·조준/반동clip·실제shot/impact분리 | cast combat_skill;실제hit combat_hit. 전용총성미보유로표시 | A모드+X. 피해0·흡수0에는큰impact없음,흡수만이면HP피해숫자없음 | Fc−1,0,+13;Fi−1,0,+1,+6,+12;target재선택전후±1 |
| op_reina.ultimate | P(Reina),V.Hit / 소형portraitcrop·강타국소VFX·전용주타격음 | cast combat_ultimate;주hit combat_hit1회 | U모드+X. 강한공격이어도OFF기본흔들림/섬광미재생 | Fc−1,0,+24;Fi±1,+6,+12;Fs=Fi−1/Fi 별도run;restore첫±1 |
| op_eve.active | P(Eve),V.Skill / 손지원clip·실제heal delta·회복전용펄스 | cast combat_skill;실제HP증가 combat_heal | A모드+X. fullHP/전원down/target변경의실제결과대로정리 | Fc−1,0,+12;FeHeal−1,0,+1,+6;fullHP/최저비율동률case;Fr±1 |
| op_eve.ultimate | P(Eve),V.Skill / 다수수혜자delta별펄스·portraitcrop | cast combat_ultimate;회복군 combat_heal1회 | U모드+X. 5명분음중복금지, 개별효과숫자는보존 | Fc−1,0,+19;모든Fe±1;0회복/부분회복비교;Fs±1;restore첫±1 |
| op_sera.active | P(Sera),V.Hit/Skill / mark·실제기본공격취소기록·표식수명 | cast combat_skill;hit combat_hit;실제취소/mark군 combat_debuff1회 | A모드+X. 이미Due공격보존, 무공격상태에서interrupt성공음없음;mark성공과분리 | Fc−1,0,+9;Fi±1;FxBasic−1,0,+1;Due=Tick/＞Tick경계;mark만료±1 |
| op_sera.ultimate | P(Sera),V.Hit / mark전용표식·강도/만료readmodel·portraitcrop | cast combat_ultimate;hit combat_hit;실제mark combat_debuff | U모드+X. 기존강한mark가유지되면새강화과장없음 | Fc−1,0,+15;Fi/FeMark±1;stronger-source만료±1;Fs±1 |
| op_mira.active | P(Mira),현재hull/V.Skill / 수리point/socket·실제train delta·공구clip/전용수리광 | cast combat_skill;repair임시 combat_heal(확실한수리시각병행),전용repair음없음 | A모드+X. train0은유효수리target아님, 조기부활표현금지 | Fc−1,0,+18;FeRepair±1,+6;missingHP/repaircap경계;trainterminal±1 |
| op_mira.ultimate | P(Mira),hull/V.Skill / repair+trainbuff분리결과·기관state아이콘 | cast combat_ultimate;실제repair combat_heal;실제buff combat_buff | U모드+X. 수리0이어도실제buff발급이면buff만표시. 현버전train범위유지 | Fc−1,0,+24;FeRepair/FeBuff±1;buffexpiry±1;Fs±1;restore첫±1 |

Fc+중간값은60fps/1×에서소스windup의중간관측목표(350→약10,600→18,450→13,800→24,400→12,650→19,300→9,500→15프레임)다. 사건을그프레임에발생시키라는명령이아니다. 1.5×/2×/pause에서는실제simulation tick으로해당비율최초도달프레임을채택한다. Fe는효과별실제적용, Fi는hit, FxBasic는실제취소된미래기본공격, Fexpiry는실제상태/layer만료다. 관측없으면N/A/미연결이지PASS가아니다. 모든경우발·관절·손무기socket·HUD가림 검수는§5/§7을병행한다.

<a id="source-78eacf9c42a3e722-11-현재3지원과-신규7지원-표현-큐-분리"></a>
#### 11. 현재3지원과 신규7지원 표현 큐 분리

<a id="source-78eacf9c42a3e722-111-현재3종-원본-trainmanual과-현재-producerconsumer-경로"></a>
##### 11.1 현재3종: 원본 train.manual과 현재 producer/consumer 경로

full/simplified는분대장장식옵션이므로지원명령에인물컷인을새로추가하지않는다. 저장된선택3순서/실제cooldown/공통채널을유지한다. 새가용성·소유·계수는추가하지않는다.

| 실제 ID | 근거와 준비→적용→회수 | 재사용·부족/SFX | 금지·예외·QA |
|---|---|---|---|
| support_fire | windup500ms,external_focus→EXTERIOR_ENEMY_FOCUS1명,trainATK400%damage. 실제열차포구준비→외부실제hit국소충격→반동/궤적회수 | 현재defaultTurret/TurretRig/Hit; 실제발사socket·전용포성부족. combat_skill→combat_hit 임시 | 내부적/전장전체폭격없음. target전멸재windup·0HP손실/흡수·외부focus검수. Fc±1,Fi±1/+6,재표적±1 |
| barrier | windup350ms,train1,trainHP8%train_shield6s. 열차윤곽준비→실제layer발급→윤곽지속/expiry회수 | 현재Shield와hull; 열차윤곽mask/layer결과필요. combat_skill→combat_shield | 사람보호/치유/무적없음. 실제cap여유/낮은강도재적용/흡수·만료·restore. Fc±1,FeShield±1,Fexpiry±1 |
| emergency_repair | windup600ms,train1,trainHP8%repair. 기관부준비→실제HP증가→수리광회수 | hull/Skill; 공구연출·repairdelta·전용수리음부족. combat_skill→combat_heal 임시 | 대원회복/부활/정비칸소환없음. repaircap/fullHP/train0·restore. Fc±1,FeRepair±1,+6,terminal±1 |

<a id="source-78eacf9c42a3e722-112-신규7종-별도-로컬개발-baseline-현재r6-실행완료-아님"></a>
##### 11.2 신규7종: 별도 로컬개발 baseline, 현재r6 실행완료 아님

근거는 Plan 상세§6 및 approved-scope-completion-orders§3.2다. 별도버전의기준은채택됐으므로숫자결정대기로되돌리지않는다. 그러나새TARGET_MAX_HP/selector/targetpin/stun/감쇠/actioninterrupt/sourcecategory계약·실제결과가준비되기전성공VFX로현재r6를대체하지않는다. 아래기술키는표시명새승인이아니며 prefix는기존ce03.role.*를따른다.

| 기술 키 접미사 / 준비 | 실제미래적용에연결할표현·회수 | 보유임시cue/부족자산 | 오인금지·중단/복원·검수 |
|---|---|---|---|
| suppressive_barrage /500ms | 확정내부최대3표적선→각실제damage 및stun성공대상만짧은차단표식→stun만료회수 | combat_skill/hit/debuff; 표적3트레이서·stun상태아이콘부족 | 외부폭격·기본interrupt대체금지. 면역은stun표시없고실제damage분리. Fi/FeStun±1,expiry±1,목록pin/대상소멸 |
| medical_support /350ms | 생존대원별실제TARGET_MAX_HP heal→짧은상승펄스→회수 | combat_skill/heal; 실제target별delta/지원의료아이콘부족 | trainATK heal로대체/부활없음. down/복귀중제외·과회복0·전원full거절. Fe대상별±1,Fr±1 |
| smoke /300ms | 실제피해감소state수혜자에얇은연막테두리/방패아닌감쇠아이콘→expiry회수 | combat_skill/buff; 실루엣안가리는연막mask·감쇠표식부족 | 회피/실명/열차감소/armor상승아님. 실제pipeline계산전후·strongest·expiry. Fe±1,피해Fi±1,Fexpiry±1 |
| weakness_scan /300ms | 확정한단일lane표적에스캔선→실제mark부여→얇은표적표식만유지 | combat_skill/debuff; 양lane독립표적지원·markresult필요 | 추가피해/다중표적없음. 더강한mark유지·원천만료/대상고정/restore. Fe±1,strongerexpiry±1 |
| firepower_boost /300ms | 계약상포함된train/Combat/Gun기본무장에작은강화표식→실제state만료회수 | combat_skill/buff; sourcecategory별장치표식필요 | 대원/지원사격/repair강화로확대금지; 현재train-only효과로완료대체불가. 포함/제외source 실제Fi전후·expiry±1 |
| electronic_disruption /200ms | 실제interruptible행동표식→실제취소성공에선끊김→회수 | combat_skill/debuff; action/commit/channelpulse별중단결과·전용중단큐부족 | stun/침묵/추가피해없음. 이미commit된효과보존·면역·무행동/소멸불발구분. Fx±1,commit前後/다음pulse±1 |
| emergency_protection /250ms | 확정최저HP비율대원에대상윤곽→실제TARGET_MAX_HP shieldlayer→expiry회수 | combat_skill/shield; 단일수혜자고정·layer결과·방호아이콘필요 | trainHP shield로대체/target소멸후무표시교체없음. 동률·cap·흡수순서·전원down·restore. Fe±1,Fexpiry±1 |

신규지원도분대장컷인은없으며full/simplified에서필수효과·타깃·경고동일, 낮은자극은장식연막/flash만축소한다. 상태복원은원천별현재상태를재구성하고지나간펄스/음향재연없음. 실제producer에없는성공결과는generic APPLIED나클라이언트추정으로보완하지않는다.

<a id="source-78eacf9c42a3e722-12-보유-sfx-실제-원천-조사와-연결-경계"></a>
#### 12. 보유 SFX 실제 원천 조사와 연결 경계

[22 WAV inventory](../../../WorkspaceRepo/docs/ops/combat-cue-evidence-20260912/audio-source-inventory.json): 실제Audio하위22wav를열어전체PCM byte수/채널/bit폭/rate/frames/duration/SHA를기록했다. 새음향생성·구매·음성합성없음. 청음·실기기믹싱을하지않았으므로인식성/음질합격은아니다.

| 보유 ID (Audio/Sfx/Combat/{ID}.wav) | 실제 길이 | 이 명세의 임시 용도 | 남은 제작 |
|---|---:|---|---|
| combat_skill |0.55s|실제active/support cast 시작|인물/행동별발동질감 구분 |
| combat_ultimate |1.10s|실제선택ultimate cast 시작|짧은windup과의mix/경고우선/skip장식정리 |
| combat_hit |0.18s|실제hit/흡수의국소피드백|칼·총·포·장갑·차체의서로다른접촉음 |
| combat_shield |0.42s|실제shield발급군|대원/열차보호구분·흡수/소멸음 |
| combat_heal |0.46s|실제heal,repair는임시재사용|의료/기계수리청각구분 필요 |
| combat_buff |0.36s|실제buff발급군|train/무장/감쇠별식별 |
| combat_debuff |0.42s|실제mark/taunt/interrupt성공군|표적/도발/중단/새stun구분 |

다른보유: UI ui_tap/ui_confirm/ui_error/ui_back/ui_toggle_or_tab, StoryRaid story_raid_warning/boss_defeat, Reward reward_claim/reward_line_tick. ui_confirm은저장확정에만, warning은실제위험에우선, boss_defeat는실제boss종료에만, reward_claim은실제명시수령확정에만매핑한다. 일반적down/recovery/총구발사/수리/charge-full 전용clip은이22파일목록에서찾지못했으므로없다고구분하고임의미보유음성으로채우지않는다.

원천README/QUALITY-SOURCE-NOTES는모두Unity LaunchAudioResourceBuilder의절차생성임시음이라고명시한다. BGM도최종작곡/스코어아님. LastRailAudioCuePlayer는동일프레임동일resolvedID를coalesce하고기존sourcevolume/mute를존중하지만event-ID dedup/다음프레임중복억제/전체mix peak보장은아니다. alias를별개성공음처럼중복재생하지않는다. 사건별presentation ledger와audio cue 배정을Unity에서정확히연결해야한다.

한시전에서확인/발동/주효과3층이내를원칙으로하고multi-target당큰효과음을반복하지않는다. Sera active의mark와interrupt는실제결과를각각표시하되동시debuff음은1회로묶는다. skip은이미시작한일회성필수cue를새로재생하지않고장식음tail처리는별도source가있을때만중지가능; 공유SFXsource.Stop으로다른위협음을자르지않는다. master/SFX/voice mute와신규진동OFF를준수한다.

<a id="source-78eacf9c42a3e722-13-w04-연계와-이번-검증-범위"></a>
#### 13. W04 연계와 이번 검증 범위

Unity는현재usablemesh/weights/clip/foot/socket산출물이없다고회신했고W01/W02후기존approvedsource boundedrig를수행하며부위/가림영역/pivot/좌표계/단위를구체회신하기로했다. Direction은그요구와실제capture를받아§5의접지·관절·칼/총구socket·가림검수를한다. 실패한RGB단일body원화/alpha교정의반복, 같은sheet재생성, 미보유음성합성은재개하지않는다. 다른4대원의개별연출안은제작명세이며Kyle gate통과전연속clip완성으로세지않는다.

이번수행은live소스정적대조,원본kit/flat10명칭일치,10행+3지원+7지원큐작성,22wav전체PCM읽기·hash다. 실제cast/impact/SFX/모드동등성/프레임캡처·게임테스트·청음은미실행. 소스관측과새제작안을구분하며신규7지원의미구현상태를현재3종완료분에소급하지않는다. 다음은Unity가각effect result/실제renderer/audio caller·rig요구를회수하고본사건별QA를수행하는것이다.

<a id="source-78eacf9c42a3e722-14-plan-13-사건-우선순위ae-납품-기준-인수"></a>
#### 14. Plan §13 사건 우선순위·A–E 납품 기준 인수

Plan 상세§13을직접읽어이번10행산출물에반영한다. r6headless15wave/finish/명시claim완료는Plan의edfeab5근거를보존하고본새연출작성으로미완료로되돌리지않는다. 정상UI왕복/동일NodeDB전체Unity재시작A와기본임시renderer의성공은서로다른검증범위다.

<a id="source-78eacf9c42a3e722-141-가림동시사건-우선순위"></a>
##### 14.1 가림·동시사건 우선순위

필수경고·대상·HP/보호막·상태·입력영역을보존하고 인물컷→연결선→장식입자 순서로축소한다. 인물컷최대1개,나중재생queue없음. 두전장의위협은각자전장안에원천/표적을표시하고하나를숨기거나공격시각을미루지않는다. 피해·회복숫자밀도는줄여도이번기준에서합산표시하지않으며개별event/대상/상태는유지한다.

보호막신규발급과피해흡수의광형태를분리한다(신규=외곽전개,흡수=접촉국소파문). 실제흡수0이면파문없음, HP피해0이면HP감소숫자없음. 약한상태만료가강한다른원천표식을제거하지않는다. 전원down은자동패배가아니며복귀clip은실제행동가능시점을지연하지않는다. 결과전환은terminal→finish확인중→preview→명시claim→receipt를분리한다.

<a id="source-78eacf9c42a3e722-142-실제-producer-의미-매핑표"></a>
##### 14.2 실제 producer 의미 매핑표

경로약칭B=E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/Battle/. 아래안정키의빈칸/한계를새wire필드발명으로채우지않는다. 이것은실제로읽은생성경계/노출값과남은관측계약의표다.

| 의미 | 실제 파일/멤버·생성 경계 | 식별키·수혜/값/시각 | 복원·현재 부족 |
|---|---|---|---|
| 저장된명령/상태게시 | B/CombatSceneDriver.Change: candidate apply후 save(candidate,revision),owner재확인뒤current=candidate; RequestSkill/Advance모두경유 | commandId는RequestSkill입력,actorId/abilityId 명시. 반환QUEUED와실제CASTING구분 | save실패는RecoveryRequired,성공표현금지. 실제caller의receipt→controllertoken연결은추가검증 |
| cast시작/진척 | B/CombatSkillCommands.ProcessSkillCommands: Targets결정,Seq/Due설정,CASTING; CombatHudReadModel casts | id/actor/kind/status/due/targets,현재tick. commandId는있지만효과별ID아님 | 복원지난cast cutin재생억제필요. target재선택/재windup 때 due/targets변경을반영 |
| 피해/흡수 | B/CombatSkillCommands.ApplySkillEffect damage→Vitals.Damage→events.Add; B/BasicCombatSimulation.BasicCombatHit | Tick/Source/Target/HpLoss/RemainingHp/Absorbed. 독립hitID/abilityId/effectIndex없음 | ApprovedCombatView.seenHits는현재배열index. Tick+Source+Target만으로multi-hit고유성보장불가;복원안정event키/생성인과매핑미확인 |
| 회복/수리 | B/CombatSkillCommands.ApplySkillEffect heal/repair→Vitals.RestoreHp | source/target/cmd/effectIndex는메서드내부,readmodel에는HP값만확인. heal/repair실제delta·effect시간별독립행없음 | HP차분으로동시damage/heal/repair출처추정금지. 실제effect-result투영필요,현재HP표시는유지 |
| 보호막발급/흡수/만료 | ApplySkillEffect→Vitals.Shield(CombatShieldSource(source,id,index),magnitude,expiry,Tick); ReadHudState shield합계 | layer원천키는생성호출에존재,HUD합계는recipient별합. 흡수량은BasicCombatHit.Absorbed | 합계변화만으로재발급/낮은강도거절/개별expiry를추정못함. layer별source/result/expiry투영미확인 |
| mark/taunt/trainbuff | ApplySkillEffect statuses: Source/Target/Ability/Index/Type/Value/Sequence/Expiry. ProcessSkillCommands에서만료/사망taunt정리 | 원천식별정보는내부상태에존재,HUD현재export에각status행없음 | 강한원천유지/약한만료/실제target별현재상태투영필요. restore현재state만,one-shot없음 |
| 현재interrupt | ApplySkillEffect interrupt: attacks.Source==target && Due>Tick 제거 | 내부target/취소공격객체/Due는있지만실제취소건수/개별actionID/result HUD노출없음 | APPLIED나castbar소멸만으로방해성공추정금지. 신규skill/channel취소와별개 |
| down/복귀 | B/CombatSkillCommands.OperatorDown 및현재actor상태; ReadHudState hp/recoverAt/protected | actor.id,현재tick,hp,recoverAt,protected. view는HP전이감지 | 현재상태표시는가능,과거전이one-shot 복원억제/actualrecoveryboundary검증필요;sourceevent종류를HP차분만으로발명금지 |
| 결과/정산 | B/CombatSceneDriver.Outcome 및ReadHudState.outcome | 실제terminal enum. finish/preview/claim/receipt는이Battle파일밖기존실제caller에귀속 | terminal만으로보상확정불가. 본단위에서정산caller의정확멤버를재조사하지않았으므로A담당증거에연결 |

추가관측: B/CombatSceneDriver.cs 및 B/BasicCombatSimulation.cs의파일hash를source-observation.followupFiles에저장했다. 명령ID가존재한다는이유로모든effect-ID문제가해소됐다고보고하지않는다. 누락표현만미연결로남기고가능한기본HP/보호막/대상/상태표시는계속한다.

<a id="source-78eacf9c42a3e722-143-ae-결과-상태"></a>
##### 14.3 A–E 결과 상태

| 묶음 | 이번 산출/근거 | 현재 상태와 다음 실제 작업 |
|---|---|---|
| A 정상UI·정산·전체재시작 | 기존Plan/Unity/Server진행보존 | Direction이번실행없음. 동일origin/NodeDB/run/원키·명시claim·가시로비영상은담당검증 |
| B 사건매핑+초기5명10행 | §10의10kit행+10제작/QA행,§11의3+7지원,§14.2실제producer표·파일hash·22WAV원천 | 문서/원천조사공급완료. 효과별미노출근거명시; 실제VFX/audio연결완료아님 |
| C mode/skip/restore | 동일commandstream·지난one-shot0·개별사건frame 검사정의 | 아직실행안함. Unitysourcefreeze조율후검증,새승인질문으로대체안함 |
| D Kyle boundedrig | 실제layer/가림/pivot/좌표·단위요구 대기,기존RGB/identity실패보존 | usablemesh/weights/clip/socket아직없음. packing/정적시안을rig성공으로대체안함 |
| E 신규7지원 | 별도baseline채택/정확효과범위·cue·미지원계약분리 | Server새버전/Unity실제target·delta·중첩·expiry·interrupt·restore단언 및r6회귀. 현재실행완료아님 |

A를기다리며B를실제로완성했고E의선행계약작업을막지않는다. C/D는실제실행소스와좌표자료를받아검수한다. 본명세의연출시간/음량/입자량은제작변수이며사건시각변경권한이없다.

<!-- source-body:source-78eacf9c42a3e722:end -->


<a id="source-2a73c19463d80b2d"></a>
## Lobby Navigation Proposal

원출처: `WorkspaceRepo/docs/ops/lobby-navigation-proposal-20260909.md`  
원문 SHA-256: `51ed1e8f0e708ae75bb089945e65d6ef54513f85b9d1d6113fc12716eeb15d53`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-2a73c19463d80b2d:start -->
<a id="source-2a73c19463d80b2d-lobby-navigation-proposal"></a>
### Lobby Navigation Proposal

Status: USER ACCEPTED DESIGN on2026-09-09 by continuation to next planning item;
no UI implementation or new resource production performed here.
Login design is directed by user in Unity task; do not duplicate that assignment.

<a id="source-2a73c19463d80b2d-portrait-layout"></a>
#### Portrait Layout

- Train command cabin background, lightly animated; fixed usable controls over it.
- Top: profile/nickname, fuel, credits, total crystals. Crystal detail separates
  FREE/PAID. Amounts and regeneration anchor come from server, no grant locally.
- Center: train/squad overview and compact currently selected operation preview.
  Main departure opens operation selection, never consumes fuel from lobby click.
- Right-side compact entries: attendance, pass, daily/weekly quests, offline reward.
- Bottom fixed destinations: lobby, operators, train, recruitment, shop. Main
  departure above bottom navigation as distinct primary command. Icons plus labels.
- Secondary menu: archive, settings, support, account linking. Preserve approved
  account conflict rules; this proposal does not create linking/merging authority.
- No forced shop modal; unavailable modes/catalog features hidden or honestly
  locked based on published server feature/eligibility state, not placeholder claims.

<a id="source-2a73c19463d80b2d-entry-and-popup-order"></a>
#### Entry And Popup Order

Admission requires accepted version/session/profile/nickname/content prerequisites.
On first eligible lobby visit in KST service day: server idempotent automatic
attendance mutation, then28cell calendar showing granted result; Confirm only.
Recovery remains separate paid confirmation action in calendar, not required to
dismiss daily result. Never say awarded if response failed; recover via receipt.
No repeat daily modal on normal reentry. If response arrives during navigation,
defer result presentation until safe lobby rather than covering combat/another modal.
After calendar close: normal interactive lobby. Offline/pass settlement is a badge
or small notification, not another chain of full-screen unsolicited popups.
Attendance monthly/service-day boundary and reconnect are server-authoritative.

<a id="source-2a73c19463d80b2d-api-state-and-navigation"></a>
#### API State And Navigation

Load server summary/feature states, then relevant detail on opening screen; API
shape must be reconciled with existing routes, not invented by UI. Show loading,
empty,error,retry and last-known read-only data distinctly. Stale data cannot
authorize cost/reward actions. Mutation receipt refreshes affected balances/status.
Remember list scroll/filter/selected item on back; Back closes dialog before page.
One screen/modal stack; rapid taps do not spawn duplicates. Disable submit in
flight and preserve idempotency after response uncertainty.
Red badges mean claimable reward or actionable issue, not every store item or
locked offer. Unavailable data is unknown, never a fabricated red badge/zero grant.
Offline minimum eligible claim produces badge only after server says claimable;
no auto offline claim or alteration of approved accumulation caps.

<a id="source-2a73c19463d80b2d-acceptance-before-implementation-signoff"></a>
#### Acceptance Before Implementation Signoff

Confirm chosen layout with user. Verify first/returning day, rollover while open,
late attendance response, unknown/failed rewards fetch, duplicate taps, empty
inventory, long Korean amounts/names, safe area and restored navigation state.
Actual request/response UI integration and device readability are unverified.

<!-- source-body:source-2a73c19463d80b2d:end -->


<a id="source-ff6ba45bd7d6f8ea"></a>
## Lobby Screen Detail Proposal

원출처: `WorkspaceRepo/docs/ops/lobby-screen-detail-proposal-20260909.md`  
원문 SHA-256: `b4fdfcc43d982723cbc3da94793f92e99126f82c922eec95868f6fc74480629b`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-ff6ba45bd7d6f8ea:start -->
<a id="source-ff6ba45bd7d6f8ea-lobby-screen-detail-proposal"></a>
### Lobby Screen Detail Proposal

Status: USER ACCEPTED DESIGN on2026-09-09 by advancing from lobby to next design.
Refines accepted lobby-navigation-proposal-20260909.md; implementation unverified.
No artwork generation, implementation, capture or combat design in this unit.

<a id="source-ff6ba45bd7d6f8ea-composition"></a>
#### Composition

Portrait9:16 with safe-area anchors;1080x1920 is reference, not fixed device size.
Top two rows avoid packing profile and all currencies onto one line: profile/menu
row, then fuel/credits/crystals row. Full balances on detail, readable abbreviation
in header when necessary. No automatic purchase from currency taps.

Full-bleed train command cabin is the main center scene. Existing cabin art must
be inspected against final anchors before asserting reuse works. Keep primary
map/window visible; no need to put five standing battle sprites in this lobby.
Right shortcut rail: attendance, pass, quests, offline reward with icons/labels.
Below center: compact five-portrait current squad strip; opens formation for
inspection/edit, no automatic changes. Train destination provides actual equipment.
Lower band: selected/next available operation name and large Departure command.
Bottom fixed five navigation destinations: lobby/operators/train/recruitment/shop.
Scene yields height on short screens before controls shrink; no forced full-page
scroll for primary lobby actions. Buttons have stable dimensions and touch spacing.

<a id="source-ff6ba45bd7d6f8ea-actions"></a>
#### Actions

Profile opens identity/display details, not an account switch shortcut. Secondary
menu exposes archive/settings/support/linking via approved flows. Fuel detail uses
server remaining/max and regeneration anchor; crystal detail shows FREE/PAID.
Departure opens operation selection, NOT an immediate run mutation or debit.
Operation suggestion uses server eligibility/progress; valid previous selection
may be restored but never invent an unlocked operation. Unknown state shows retry.
Selection -> formation/entry review -> explicit start is separate from lobby UI.
Do not force a new default team or auto spend when revisiting any destination.

<a id="source-ff6ba45bd7d6f8ea-reward-surfaces"></a>
#### Reward Surfaces

Attendance opens28cell calendar, today's automatic result Confirm only; voluntary
recovery is separate100crystals/cell up to7monthly with server eligibility.
Pass opens free/premium rows at current level, purchased status and server claims.
Quests opens daily/weekly tabs; actual progress and rewards from server.
Offline opens server duration/cap/preview and explicit claim. Not auto-claimed.
All return to previous lobby state on close. First eligible attendance result has
modal priority; offline/pass do not force a subsequent full-screen popup chain.

<a id="source-ff6ba45bd7d6f8ea-rendering-and-state"></a>
#### Rendering And State

Amounts/eligibility/rows use API data and string/art IDs. Static local assets are
not economic truth. Read must not grant; mutation receipt drives final UI update.
Claimable badges require server evidence. No red badges for merely affordable
upgrades or shop inventory; unread critical issue and claimable reward distinct.
In-flight actions suppress duplicate submission; uncertain result reconciles
receipt/status without charging again. Detail failure preserves back navigation.
Per-surface refresh avoids blanking whole lobby. Background network refresh cannot
open a modal over another page/combat. No new arbitrary polling interval specified.
Source labels/memory/animation targets and native safe areas need implementation
QA later. No claim that layout is already bound or final art already composed.

Next after acceptance: detailed combat design, then implementation/verification;
tutorial stays deferred. Paid-products/equipment/research backlog retained.

<!-- source-body:source-ff6ba45bd7d6f8ea:end -->


<a id="source-f59ad886e2765803"></a>
## New Player Guidance Proposal

원출처: `WorkspaceRepo/docs/ops/new-player-guidance-proposal-20260909.md`  
원문 SHA-256: `f649c5e7a19ce36d072b9694b9f853342bfed478fb6fe7e4dfdc58b548e4b634`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-f59ad886e2765803:start -->
<a id="source-f59ad886e2765803-new-player-guidance-proposal"></a>
### New Player Guidance Proposal

Status: DEFERRED; detailed combat guide is an unapproved draft, not an implementation
order. User sequence: lobby screen composition -> detailed combat design -> combat
implementation/verification -> tutorial. Only common persistence/skip policy approved.
Existing account/operation/unlock,
initial free5 ownership and crystal-source-policy remain authoritative. No new
tutorial crystal/ticket grant or guaranteed recruitment outcome.

<a id="source-f59ad886e2765803-sequence"></a>
#### Sequence

1. After admission and daily attendance calendar close, optional start/skip guide.
2. Show server-owned initial5 and squad slots. Recommend existing eligible5;
   player confirms through existing loadout action, never create ownership locally.
   Preserve existing3+2 squad geometry; no placement redesign.
3. Select first available story operation and show actual server fuel requirement.
   Story solo only, ordinary15waves unchanged. No fake run or forced free entry.
4. Contextual combat callouts: exterior train integrity and interior operator
   health are separate; point to existing skill/target controls without inventing
   new input/actions. One hint at a time, no essential HUD occlusion. Do not pause
   authoritative combat by freezing only the client or change reward timing.
5. On actual result show server-confirmed rewards, then on successful first-clear
   suggest TrainingCore growth when server quote says affordable. Preview first,
   explicit confirm for spending, no compulsory investment. Failure offers retry
   or normal lobby; no automatic success/reward to advance the guide.
6. Introduce offline/quests/pass as available and optional recruitment preview.
   Recruitment spending is optional with server result only; no forced pull,
   invented elite guarantee or requirement to use daily discounted ticket.

<a id="source-f59ad886e2765803-persistence-and-skip"></a>
#### Persistence And Skip

Guide may be skipped/closed and replayed from settings/help. Skipping does not
skip nickname/version/auth checks, unlock content, finish an operation or earn
rewards. Do not replay introductory modal on every login after skip.
User-approved policy: device-local, account-scoped step/completed/skipped/version
state, using non-sensitive PlayerPrefs if appropriate. No server guide-state API
required. Reinstall/device change may reset guidance; skip/replay remains available.
Auth tokens remain in Keystore, never PlayerPrefs. Tutorial grants/unlocks forbidden;
any future reward or unlock addition requires separate server authority design.
Resume at safe lobby checkpoint, never duplicate a cost/grant. Existing account
ownership/story/receipts determine which objectives are already satisfied.
Account switch changes local guide namespace and loads target account state. Linking
follows approved account-selection/conflict policy, no cross-account guide merge
that changes progress or rewards. Failed state saves must not prevent normal play.

<a id="source-f59ad886e2765803-scope-and-acceptance"></a>
#### Scope And Acceptance

Visual cues use string IDs and existing art. Narrative flavor yields to tutorial
hint, following narrative launch checklist. Existing UI/menu eligibility stays
server-owned; no extra forced tutorial locks on shop/pass/archive.
Test skip/replay, interrupted combat, late reward response, failure, no fuel,
insufficient EXP, new/returning/linked account, network retry, long localized
text, and no duplicate onboarding/attendance/first-clear rewards.
No runtime tests run, no new endpoint/SKU/reward amount approved by this proposal.

<!-- source-body:source-f59ad886e2765803:end -->


<a id="source-168523dd5098955c"></a>
## 일반 5종·궁극 5종 시안과 구현 차이 — 2026-09-13

원출처: `WorkspaceRepo/docs/ops/operator-concept-gap-20260913.md`  
원문 SHA-256: `cb372cd801cb2531315a11827214987825cb26fc6adeca60beff7497bd8d64bb`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-168523dd5098955c:start -->
<a id="source-168523dd5098955c-일반-5종궁극-5종-시안과-구현-차이--2026-09-13"></a>
### 일반 5종·궁극 5종 시안과 구현 차이 — 2026-09-13

Direction이 Plan 제공 원본 10장을 직접 확인한 비교표다. 현재 몸체 시전 동작과 공통 결과 표시는 구현되어 있지만, 아래 시안의 능력별 공간 연출 전체가 구현되었다는 근거는 없다. 시안 생성 이력은 개별 최종 승인이나 구현 완료를 뜻하지 않는다.

<a id="source-168523dd5098955c-근거와-한계"></a>
#### 근거와 한계

- 시안 루트: `C:/Users/dlstj/.codex/generated_images/01a0956d-aa68-72d3-9cba-97be7a3e2614/`. 아래 파일명은 모두 이 루트 기준이며 10장 직접 열람했다.
- 현재 코드 직접 열람: `E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/Battle/ApprovedCombatResultView.cs`, `CombatResolvedVisual.cs`, `CombatResultGlyph.cs`.
- 결과 표시는 실제 수신자 위치의 ShieldIssue/Heal/Repair/Interrupt/Mark/Taunt/TrainBuff 등의 공통 도형이 중심이다. ShieldIssue는 HP 옆이며 유닛 크기 40, Repair 100, 일반 결과 86. 지속은 Interrupt .32초, Repair .48초, Mark .45초, 기본 .58초 등이다. 일부 궁극은 발동자→대상 .22초 선을 더한다. 이는 능력별 전용 시안 재현과 다르다.
- 기존 `all-operator-skills-v03-20260913` 10종 APPLIED·시전 복귀 검증과 이번에 다시 본 s2-f30은 몸체 동작/기능의 과거 근거다. 새 시안과 동일 시점의 최신 VFX 비교 영상으로 취급하지 않는다.
- Unity가 현재 수정 중이므로 아래는 열람 시점의 비교 기준이다. 미확인 전용 연출은 최신 촬영으로 확인해야 하며 코드 전체 부재를 단정하지 않는다.

<a id="source-168523dd5098955c-능력별-차이"></a>
#### 능력별 차이

| 능력 / 시안 파일 | 시안에서 직접 본 형태·색·범위 | 현재 확인된 차이와 구현 확인점 | 실제 판정 보존 조건 |
|---|---|---|---|
| Kyle 일반 전선방호 / `exec-8ce3ce86-9afe-45b5-ac97-c7202b489f4f.png` | 몸을 감싸는 청록 육각 개인 보호막, 가장자리 백색 접점광 | HP 옆 작은 ShieldIssue와 가드 동작에 비해 몸 전체 보호막·육각 면·접점 반응이 부족. 몸과 HP를 가리지 않는 반투명 외곽 필요 | 실제 보호막 수신자만 표시. 주변 아군까지 보호한다는 인상 금지 |
| Reina 일반 집중사격 / `exec-6c504b74-923c-469f-af90-bdea1181450b.png` | 한 적으로 모이는 금빛 다중 탄선, 주황 분절 표적 고리·백색 타격광 | 기존 방패/해머 준비 동작과 공통 피격으로는 집중되는 탄선과 표적 고리가 드러나지 않음 | 실제 무기는 방패·해머. 시안 총기로 임의 교체하거나 아군 전체 사격·추가 피해를 만들지 않음 |
| Eve 일반 응급처치 / `exec-aa0c5f5d-b5ac-4103-a5a1-1364993f2405.png` | Eve에서 Kyle로 휘는 민트색 회복 줄기, 몸 주변 의료 십자 | 공통 Heal 표시와 손동작보다 발동자→수신자 관계·몸을 감는 궤적이 약함 | 실제 회복 대상에만 연결. 가까운 Reina까지 광역 회복으로 읽히는 시안 요소 조정 |
| Sera 일반 신호차단 / `exec-ece64e6d-728b-4b69-ac0e-daaad085528b.png` | 보라 연결광, 적 몸체 주위 끊긴 기술 고리와 사각 입자, 차단 표식 | 헤드셋 동작과 짧은 Interrupt 도형에 비해 색·대상 둘레의 교란 공간 표현이 부족 | 연속 피해 빔이나 반복 차단으로 바꾸지 않음. 실제 상태 지속과 종료에 연결 |
| Mira 일반 현장수리 / `exec-7b252f53-6e06-4d40-9e97-fd0ff3687602.png` | 공구함·청록 렌치 홀로그램, 열차 특정 부위 윤곽과 스파크 | 작업 손동작·Repair 표시보다 수리 부위와 장비의 연결이 부족. 공구함은 신규 소품 필요 여부 별도 판단 | 실제 train_a 수리 위치/양/시점 유지. 지휘관 수리 드론과 구별 |
| Kyle 궁극 최후의방벽 / `exec-fb989ca2-9172-4a7b-90b1-038592dca9a5.png` | 아군 앞 대형 청색 육각 방벽, 아군 바닥 윤곽, 복수 접점광 | 수신자별 공통 보호막 표시와 가드에 비해 전면 방벽·바닥·접점의 조합 미확인 | 실제 보호 대상만 표시. 새 투사체 충돌/무적 금지. 적 예고·HP 가림 방지, 지휘관 필드와 구별 |
| Reina 궁극 결정타 / `exec-f15b114d-b13a-42d5-911a-721fc0c9d97b.png` | 단일 적으로 뻗는 굵은 금빛 타격, 큰 타원 충격 고리·백색 중심·파편 | 짧은 연결선과 기존 피격에 비해 중심 폭발·충격 고리·잔광의 시간차 미확인 | 임의 넉백·지형 파괴·광역 피해·무기 교체 금지. 시각 반동은 실제 판정과 분리 |
| Eve 궁극 생명선 / `exec-5251842a-9960-40f6-86d2-183d6723ff7e.png` | 여러 아군으로 갈라지는 녹색 회복 곡선, 인체 실루엣·바닥 ECG | 수신자별 Heal과 짧은 연결선보다 분기 네트워크·심전도·몸 실루엣이 부족 | 실제 회복 수신자만 연결. 실제 부활 전 다운 인물이 되살아난 듯한 발광 금지 |
| Sera 궁극 집중표적 / `exec-82ef6ec6-7dca-4ecd-a8dc-335269d93fa0.png` | 단일 적 중심 큰 주황 조준선, 보라 외곽 곡선과 상부 삼각 표식 | 공통 Mark와 순간 선보다 대상 식별·상태 유지 중 조준 표시가 약함 | 실제 표적과 만료/해제/복원에 연결. 새 타깃 우선순위나 강제 아군 사격을 만들지 않음 |
| Mira 궁극 기관재가동 / `exec-5888c34d-9a22-42b2-813b-13b36a069ba6.png` | 열차 청록 윤곽·연결 노드, 기관부 황금빛·증기, 진단 콘솔과 케이블 | TrainBuff/Repair 계열 도형과 작업 동작보다 열차 전체의 에너지 흐름·기관 가동 단계가 부족 | 실제 결과 범주 유지. 이동 속도 증가·미발생 수리·쿨다운 초기화·열차 모델 교체를 시안에서 추론하지 않음 |

<a id="source-168523dd5098955c-촬영수용-기준"></a>
#### 촬영·수용 기준

아이콘 교체만으로 위 차이가 해결되지는 않는다. 실제 720 화면에서 각 능력의 발동자, 수신자, 고유 형태와 색이 구별되어야 한다. 발동 전→형성→실제 결과 순간→잔광→종료를 같은 장면으로 촬영하고 HP·적 예고·카드 글자 가림도 함께 확인한다. 정지 시안은 정확한 지속 시간의 근거가 아니다.

캐릭터/무기, 잘못된 시안 카드 이름·아이콘·100% 게이지, 큰 중앙 배너, 근접 카메라와 임의 적 배치는 복사 계약이 아니다. 최신 사용자 요청인 상단 타이틀 제거와 실제 HP/쿨다운/흰 보호막 표시를 유지한다.

지휘관 지원사격·전자기장·수리 드론은 별도 진행 중이며 이 표가 촬영을 막지 않는다. Unity 최신 보고상 지원사격 110px 및 불규칙 필드 비교 촬영 완료, 드론은 창밖 배경 가림 수정 후 재촬영 중이다. 이 최신 보고는 본 문서에서 직접 영상 검수 완료로 바꾸지 않는다.

<!-- source-body:source-168523dd5098955c:end -->


<a id="source-6804b653993214cf"></a>
## HP0 incapacitation pose candidates

원출처: `WorkspaceRepo/docs/ops/operator-parts-20260913/DOWN-V01.md`  
원문 SHA-256: `c70327823087cc9b819ff014a948d947b56ec544c333e7e63b65f4c9a066555e`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-6804b653993214cf:start -->
<a id="source-6804b653993214cf-hp0-incapacitation-pose-candidates"></a>
### HP0 incapacitation pose candidates

down-pose-v01.json overlays the existing5rigs. Unchanged source bitmap, masks, pivots and sourceScale. downAngles and5downPoseSamples are ABSOLUTE clockwise joint angles. Every rootOffset is[0,0]; lower body and Sera tripod roots remain fixed. Authoring duration .35 seconds with smoothstep; hold only while actual model HP<=0/down. Recover only at actual model recovery, approximately .30s blend toward appropriate current rest. Use saved/pause-aware presentation clock. Never change HP, recoverAt, availability or command rules to serve the animation.

Kyle/Reina/Eve have one combined lower-body part and therefore cannot visibly buckle knees or lie down using this source. Their candidate is a visibly bowed, forward-slumped incapacitation pose with fixed standing legs. Do not call it full-body collapse or a completed kneeling/down redraw. Sera/Mira retain kneeling legs, bow torso/head and lower hands/weapon. No whole-figure90degree rotation, root drop, or floating-floor shortcut. This makes HP0 different from idle within the available source limitation.

Direct browser midpoint and final-hold stills were checked for all5. The initial Reina shield protruded too far right; far_upper target changed to30degrees to keep the shield against the body. Current hands/weapons/coat have no obvious large separation in those stills. Waist/neck overlap and cast-to-death transitions still need actual game-size QA.

down-floor-check-v01.json checks101 evenly spaced rest-to-down parameter values using transformed convex hulls of source pixels retained by native polygon+mask. All5report zero additional depth below their own rest feet/tripod baseline, with lower fixed. This is a source pixel-center geometry approximation (about2sourcepixels boundary tolerance), not a claim of continuous framebuffer proof. It uses the rig's real retained lower shape, not an invented ground height. Actual gameplay floor placement must remain the existing sole anchor. Actual death starting from an attack/skill pose must blend from that captured current state and be checked separately; the101check only covers authored rest-to-down.

Recommended runtime QA: trigger actual model HP0 during idle and during attack/cast; confirm stopped attacks/muzzle events, the down hold, unchanged foot anchor, then actual recoverAt and pose recovery. Hit reaction remains color only per user. No skill or standing animation fallback should silently replace down hold.

Files include source-linked browser preview and reproducible read-only geometry check. Kyle source/connected-mask JSON copied unchanged only to reproduce the preview; the other4source files already exist in this delivery directory. This is metadata for native rig consumption, not bitmap editing.

<!-- source-body:source-6804b653993214cf:end -->


<a id="source-a189bbb30640efff"></a>
## Skill gesture samples added — v02

원출처: `WorkspaceRepo/docs/ops/operator-parts-20260913/MOTION-V02.md`  
원문 SHA-256: `7606edd0505289c9dac832766d16e04681c8c2460a028c5bfa3fb73732b35da2`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-a189bbb30640efff:start -->
<a id="source-a189bbb30640efff-skill-gesture-samples-added--v02"></a>
### Skill gesture samples added — v02

Use four *-rig-v02.json files with the same immutable source PNGs. Geometry,source,pivots,scales and sockets remain the v01 definitions; v02 adds skillAngles and skillPoseSamples at0,.25,.5,.75,1, and removes nonexistent generic angle keys. New rest/attack/skill values are ABSOLUTE clockwise degrees. Do not add the target angles to restAngleDeg a second time. Existing original v01 delivery remains preserved.

Reina: braces shield and raises hammer slightly while focusing, preserving the approved shield/hammer art and existing 집중 사격 model effect. Eve: raises the existing medical pistol, small head inclination; existing 응급 처치 unchanged. Sera: near hand tracks the headset while far hand remains on turret, with IK solved at all5samples. Mira: lowers the scoped rifle, inclines head and reaches forward with free supporting hand; current field-repair effect remains in the model, no new tool/effect invented. Lower-body and tripod root remain fixed.

Direct browser skill-midpoint and skill-target stills were inspected for all4. Sera headset gesture and Mira lowered-rifle/reaching-hand silhouettes are visible; no large joint separation was seen in those states. Initial Mira reaching hand was obscured by rifle, adjusted target from[290,298] to[318,290]. Actual-size, intermediate interpolation, real active/ultimate timing, skill return and hit/down still require runtime evidence. Small joint seams and Mira support elbow angularity remain QA priorities. This gesture candidate is not a fabricated approved ability animation.

Consumer should interpolate the supplied samples over actual casting/return timeline; retain actual model IDs/effect timing, then capture each real cast and return. Generic Kyle guard fallback is not equivalent. No new bitmap generation or editing for this addition. Reproduce with build_previews-v02.py, open preview-v02.html.

<!-- source-body:source-a189bbb30640efff:end -->


<a id="source-d8e95303990ee060"></a>
## 전원 궁극기 카드 UI — 최신 사용자 기준

원출처: `WorkspaceRepo/docs/ops/operator-ultimate-ui-20260913.md`  
원문 SHA-256: `d2af6b47ab8e6a5978d583ec3c7d8d9858f607eb507206fd699c44eaae2cfd26`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-d8e95303990ee060:start -->
<a id="source-d8e95303990ee060-전원-궁극기-카드-ui--최신-사용자-기준"></a>
### 전원 궁극기 카드 UI — 최신 사용자 기준

2026-09-13. 기준 시안: `C:/Users/dlstj/.codex/generated_images/01a0956d-aa68-72d3-9cba-97be7a3e2614/exec-deedcdff-678e-4048-881e-2961e0dd7b7b.png`. 사용자 최종 승인(좋아 이거로 ㄱㄱ) 원본을 Direction이 직접 열람했다. 이 원본이 63ce/c68 중간 시안보다 우선한다. 이전 라벤더/일반·궁극 2버튼/카드 위 AUTO 배치안은 대체되었다. 변경 이력은 Direction 인수인계에 보존한다.

<a id="source-d8e95303990ee060-확정된-화면-구조"></a>
#### 확정된 화면 구조

- 별도 분대장 궁극기 칸 삭제. 동일 폭 요원 카드 5장.
- 각 카드: 초상 → 이름 → 청록 HP·흰 보호막 → 금색 궁극기 게이지와 숫자% → 카드 전체 폭의 단일 궁극기 버튼.
- 게이지에는 ULT/궁극기 문구를 넣지 않는다. 궁극기 버튼에는 큰 능력별 아이콘만 둔다. 궁극기/준비 글자와 배지를 제거하고 실제 ready일 때 아이콘·테두리 발광만 유지한다. 일반 스킬 아이콘과 일반 버튼은 카드에서 제거한다.
- 상단 header에는 기존 ON/OFF 자리의 토글 내부 AUTO만 남긴다. 궁극기 문구/외부 중복 AUTO/ON OFF 문자 제거. thumb 위치와 활성색을 함께 변경하고 터치 영역 유지. 접근성 상태값은 실제 값을 제공하되 표시 문구를 재추가하지 않는다.
- AUTO 범위는 궁극기 5종이며 기본 OFF. 일반 5종의 상시 자동 기능과 혼동하지 않는다.
- 일반 스킬 CD는 전투 화면의 각 아군 캐릭터 HP 바로 아래 얇은 청색 진행바로 표시한다. 시안의 하늘색/청색 CD와 위 청록 HP를 독립된 어두운 홈·수직 간격으로 구분한다. 적 HP 아래에는 이 아군 일반 CD바를 추가하지 않는다.
- 타이틀 제거와 현재 지휘관 3종 버튼을 유지한다. 시안의 고정 수치·잘못된 아이콘·지원 슬롯 순서·적 배치는 실제 계약을 덮어쓰지 않는다.

<a id="source-d8e95303990ee060-확정된-일반-자동-발동"></a>
#### 확정된 일반 자동 발동

사용자 답변: **일반 스킬은 준비되면 자동 발동**. 일반 5종은 궁극 AUTO와 무관한 기본 자동 기능이다. 일반 수동 버튼과 별도 일반 AUTO 토글을 추가하지 않는다.

실제 쿨타임 준비·생존/시전 가능·유효 대상 조건을 만족하면 자동 요청한다. 대상 없음/다운/시전 중에는 준비된 CD 상태와 실제 발동 가능 여부를 구별하며, 준비 바가 찼다고 가짜 시전 연출을 재생하지 않는다. 같은 요원의 공유 큐·중복 예약 방지·실제 자원 소비 시점을 유지한다.

수동 궁극이 일반 자동 요청에 계속 밀리지 않도록 정해진 처리 단계에서 수동 궁극 우선과 일반/궁극 자동 경합을 결정론적으로 해결한다. 유효 대상이 없을 때 매 tick 거절 기록이 쌓이지 않도록 요청 전 후보 자격을 판정한다. Plan·Unity 전달 Server 정합 중인 우선순위는 manual → ultimateAUTO → generalAUTO → basic이며 command-v3/.003 신규 계약으로 제작 중이다. 정확한 순서·예약 계약과 신규 정책 version/hash의 확정·구현 증거는 Plan·Server·Unity가 제공한다. 기존 prepared/저장 해석을 소급 변경하지 않는다.
<a id="source-d8e95303990ee060-cd바-표현-및-실제-값-연결"></a>
#### CD바 표현 및 실제 값 연결

- HP 앵커를 따라 이동하고 HP 아래 3~4px 간격, 두께 4~6px를 720 원본의 초기 검토값으로 삼는다. 다운 포즈/시전·피격에서도 캐릭터와 이웃 바를 가리지 않는지 확인한다.
- 색/차오름 방향은 디자인 판단과 기능 판정을 분리한다. 제안은 실제 cooldownRemaining/total을 이용한 준비 진행률(1-remaining/total), 준비 시 full이다. 최종 방향은 Unity·Plan 정합을 거쳐 한 방향으로 유지한다. total=0/스킬 미보유를 나눠 처리하며 임의 cooldown을 만들지 않는다.
- CD 완료는 시전 가능과 다를 수 있다. 다운·대상 없음·시전 중 등에서는 full을 유지하더라도 준비 발광을 끄고 실제 enabled를 따른다. 기본 공격 주기/공격 windup을 일반 스킬 CD로 대신 표시하지 않는다.
- CD바는 HP·흰 보호막에 합쳐 계산하지 않는다. 캐릭터가 표시되지 않으면 함께 숨기고 전투 pause/복원에서 실제 시간과 상태를 따른다.

<a id="source-d8e95303990ee060-궁극기-상태"></a>
#### 궁극기 상태

| 실제 상태 | 표시 |
|---|---|
| 충전 중 | 실제 숫자%와 금색 fill, 버튼 강조 약함 |
| 100% + 실제 enabled | 큰 아이콘과 금색 테두리 발광만. 준비 글자·배지 없이 과도한 전원 점멸 금지 |
| 100% + 불가 | 100% 보존, 준비 발광 해제·아이콘 감광. 버튼에 불가 문구를 추가하지 않으며 실제 입력 조건·접근성 상태로 구분 |
| 시전 접수/진행 | 실제 접수 후 아이콘의 짧은 반응, 중복 입력 차단. 시전 문구 없이 게이지 소비 시점은 실제 결과를 따름 |
| 다운/기록 복구/종료 | 초상·아이콘 감광과 입력 제한, 전역 복구/종료 표시는 유지. 버튼 문구 없이 보유 게이지를 임의 0으로 만들지 않음 |
| AUTO 켜짐 | 상단 단일 토글 위치/활성색 변화. 켜짐 자체를 충전·시전 완료로 표현하지 않음 |

<a id="source-d8e95303990ee060-계약과-검수"></a>
#### 계약과 검수

현재 직접 읽은 CombatHudReadModel/CombatLiveHudBinding/ApprovedCombatView/ApprovedCombatResultView에는 selectedUltimate·단일 ultimate 객체, 단일 카드/Row/Refresh/cutin/lastCharge/연출 판별 의존이 있다. 전원 확장 시 actor ID+ability ID로 결합하고 slot은 배치에만 사용한다. 컷인과 연출은 실제 발동 명령의 actor/ability를 따른다. 저장 호환·AUTO 순서·수동 공존·selectedUltimate 이행은 Plan·Server·Unity 계약에 따른다.

720 원본에서 5종 서로 다른 궁극 충전율, 전원100%, 100% 불가, 다운+보유게이지, 일반CD+궁극ready, AUTO 두 상태, pause/복원을 검수한다. 카드 단일 버튼이 해당 요원 궁극기만 발동하고 일반 스킬로 잘못 연결되지 않아야 한다. 월드 CD바는 아군 ID별 정확한 일반 스킬의 실제 값과 비교한다. 일반 준비 후 대상 없음/다운·복귀/pause·restore/일반 자동과 궁극 수동·자동의 경합을 추가 검증한다. 일반 자동 정책의 실제 구현 완료는 이 문서 갱신과 구분한다.

지원사격·전자기장 v02·드론 v04의 필수 미술 보완 수용은 별도 보존한다. 이 문서는 최신 UI 기준이며 새 UI 구현·캡처 검수 완료를 의미하지 않는다.
최종 deedcdff 시안에는 궁극기/준비 버튼 글자가 없으며 큰 아이콘만 보인다. gold 게이지+%와 상단 AUTO는 유지한다. 일반 준비시 자동 발동은 확정되어 답변 대기 상태가 아니다. 새 이미지 생성은 필요 없다.

<a id="source-d8e95303990ee060-실제-촬영-인수-기준"></a>
#### 실제 촬영 인수 기준

1. 신규 command-v3/.003의 정확 정책/version/hash와 실행 source hash, 기존 저장 호환 검증 범위를 제시한다.
2. 실제5인 서로다른충전→준비→수동궁극발동→소비/복귀,일반자동 발동과world CD의초기화·진행을확인한다.
3. 궁극AUTO OFF/ON 및일반자동계속동작,수동궁극·궁극AUTO·일반AUTO경합과대상없음/다운복귀/pause/restore를분리검증한다.
4. 최종720 원본PNG와재생가능영상, 명령/결과기록을함께제공한다. 100%와enabled 불일치사례도필수다.
5. 최신UI 기능·미술검수 완료전에는시안승인을구현완료로표현하지않는다. 기존지휘관3종수용은보존하며요원10종고퀄VFX 차이는 operator-concept-gap-20260913.md의별도미완료범위로관리한다.

<!-- source-body:source-d8e95303990ee060:end -->


<a id="source-8bafad2f0b3c60ac"></a>
## Startup Motion Art Backlog

원출처: `WorkspaceRepo/docs/ops/startup-motion-art-backlog.md`  
원문 SHA-256: `6914acc0637f698556abac5af1dfe19eb8cc166be7118fb30480b1af7ca36090`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-8bafad2f0b3c60ac:start -->
<a id="source-8bafad2f0b3c60ac-startup-motion-art-backlog"></a>
### Startup Motion Art Backlog

<a id="source-8bafad2f0b3c60ac-2026-09-08-limited-production-approval"></a>
#### 2026-09-08 Limited Production Approval

Subsequent user approval includes applying this specific motion set in Unity
and recording its actual animated Play Mode flow. This supersedes the integration
and video hold below only for this slice. Direction creates assets; Unity owns
integration and capture. Do not add actual lobby transition or alter auth/content
gates. QA-injected login success must be distinguished from real DEV auth.
Use real rendered moving frames, not a still slideshow or composite fallback.
Validate the replacement MP4 before retaining only the latest QA video within
the explicitly scoped project Video directory, then open that folder.
Existing-resource bulk edits and future unrequested videos remain on hold.

User now requests Direction to create yesterday's motion resources. This
supersedes generation deferral ONLY for the seven opening and up-to-seven login
prepared assets below. Existing-resource bulk changes remain deferred. The later
approval above permits this slice's Unity integration and real recording, not
APK/AAB builds or remote publication. Direction dispatch accepted; no generated
files verified yet. Preserve source illustrations and produce separate layers,
loop-compatible exports, alpha/mask checks and source/size manifest under
E:/Projects/LASTRAIL/unity-project/QA/Design/StartupMotion-20260908/.
Use normal filesystem approvals where required. Memory figures remain targets,
not measured results. Actual Assets/prefab installation is owned by Unity under
the subsequent bounded approval above and is not yet complete.

<a id="source-8bafad2f0b3c60ac-2026-09-07-user-direction"></a>
#### 2026-09-07 User Direction

Record only; defer generation due to token budget. No images/video created or
Unity animation implemented by this planning entry.

<a id="source-8bafad2f0b3c60ac-opening-rail-motion"></a>
#### Opening Rail Motion

Seven planned assets: sky/distant mountains, distant ruins, nearby terrain,
loopable perspective rails/sleepers/ground, foreground train body, steam sheet,
dust/spark sheet. Requires loop seams, occlusion cleanup and perspective-aware
motion; seven files alone do not establish an animated scene. Existing logo and
buttons reused. Desired short-loop feel, not a recorded-video deliverable.

<a id="source-8bafad2f0b3c60ac-login-interior-rattle-estimate"></a>
#### Login Interior Rattle Estimate

Minimum: existing interior background only (zero new illustrations), small
background translation/rotation with overscan; fixed UI. This yields a simple
camera-rattle effect, not separately moving vehicle/environment.

Recommended: four total layers, leveraging existing interior reference:
1. Carriage interior/cabin frame with transparent window openings.
2. Distant outside landscape, seamless slow scrolling.
3. Near outside scenery, seamless faster scrolling.
4. Hanging handle/lamp/cable prop layer for restrained independent sway.

Existing interior PNG is the reference/base but not already four prepared
layers. Estimate three additional separated images plus one existing-background
edit, or four prepared layer exports in total. Window masks can reuse cabin
alpha; do not count masks as new paintings. Dust/light optional, not required.

Login text,inputs,buttons remain fixed; avoid strong periodic full-screen shake,
provide reduced-motion/static variant. Bootstrap-local inclusion and overdraw/
memory budgets must be checked during later implementation. Production deferred.

<a id="source-8bafad2f0b3c60ac-login-lighting-and-transition-addition"></a>
#### Login Lighting And Transition Addition

User requests planning only: occasional cabin light outage during idle; after
successful login lights brighten, map railway lines illuminate and a transition
is suggested. Do not implement/generate yet or add an actual lobby transition now.

Plan sparse, non-strobing lamp dim/off/on cycles, local to lighting/background;
UI remains readable. Login failure does not trigger success lighting. Success
lighting follows authenticated/profile-ready state and does not claim downloaded
content/Main readiness. Railway illumination may accompany preparation, but
actual scene entry must still wait for all runtime gates.

Additional prepared layers: cabin illumination mask (bright/dim variants driven
by render parameters), map base (reuse existing rail-map art only after checking
composition), and separate railway-route emissive mask. Estimate up to three
additional prepared assets over the four login layers, seven total; fewer new
paintings if map is reusable. No need to paint every flicker frame.

Exact timing, map placement and final transition destination remain design
follow-ups. Reduced-motion option uses a steady light/fade, not repeated flashes.

<a id="source-8bafad2f0b3c60ac-preliminary-size-budget"></a>
#### Preliminary Size Budget

Observed current PNG source sizes: cabin2905991 bytes, exterior3037465 bytes,
map3600173 bytes. Pretendard regular/bold/semibold source fonts about1.57MB each.
These are source files, not installed package or GPU allocation measurements.

Estimate only: seven opening plus up-to-seven login assets, typically1080x1920
backgrounds and smaller masks/props/sheets, Android ASTC6x6 or4x4 depending on
quality. A1080x1920 ASTC6x6 image has921600 bytes of block payload before mipmaps/
atlas/container overhead; ASTC4x4 has2073600. Full-screen14images at6x6 would be
12.9MB payload, not a promised download size. Larger seamless atlases may increase it.

Initial bootstrap asset budget: motion layers8-18MB, common UI2-5MB,font/TMP3-8MB,
short compressed audio1-3MB: approximately15-35MB, target25MB or below, excluding
Unity engine/native libraries/code. Does not include remote game assets or QA
mockups/source contact sheets. APK/AAB actual size requires build report; memory
must be measured separately, especially with uncompressed fallback/read-write.
Reference: https://github.com/ARM-software/astc-encoder/blob/main/Docs/FormatOverview.md

<a id="source-8bafad2f0b3c60ac-lower-resolution-memory-estimate"></a>
#### Lower-Resolution Memory Estimate

User accepted this as the planning budget and requested a read-only audit of
existing resources. Actual resource resizing/reimport/compression/group changes
remain ON HOLD. The proposed startup dimensions are not blanket limits for all
operator/combat art. Audit actual use and visual requirements before proposing
changes. Profile measurements must be distinguished from computed estimates.

User asks GPU/resident asset estimate for proposed reduced dimensions. No assets
created or texture import settings changed. Assumptions: native ASTC6x6 support,
no mipmaps on fixed UI,Read/Write off,no atlas padding/extra full-screen targets.
MiB means1048576bytes. Per-image ASTC6x6 payload versus RGBA32:
720x1280:0.392/3.516MiB;540x960:0.220/1.978;
360x640:0.098/0.879;512x512:0.113/1.000.
Example fourteen-layer inventory4large+4medium+2small+4sheets gives~3.1MiB
ASTC payload or~27.7MiB RGBA32. Real inventory is not fixed; budget animation
textures3-6MiB including quality/packing allowances. UI,font atlases/audio/runtime
objects add a provisional12-24MiB: total startup asset residency target15-30MiB,
temporary loading/transition planning budget25-45MiB. These are budgets, not a
profiled measurement or whole-app RAM prediction. Uncompressed fallback can
invalidate the compressed estimate. Render buffers,engine/code and gameplay are
excluded; one1080x1920 RGBA32 render surface alone is~7.9MiB.
Release unused title-layer references before loading later surfaces where safe;
resident bootstrap/shared references may otherwise prevent reclamation.
Full mip chain adds~one third to base texture payload; Read/Write retains CPU copy.
Reference: https://developer.android.com/games/engines/unity/unity-reduce-memory

<!-- source-body:source-8bafad2f0b3c60ac:end -->


<a id="source-1115ac84904315f4"></a>
## Startup/login partial delivery v03 - 2026-09-08

원출처: `WorkspaceRepo/tmp/startup-motion-20260908/DELIVERY-v03.md`  
원문 SHA-256: `f22cba4ba48115c3997d982929d3b701497ebfc70b540d0d7fd0ce71e11cafd0`  
검토·적용 경계: Current workspace guidance, routing or distinct technical handoff; not covered by this retirement batch

<!-- source-body:source-1115ac84904315f4:start -->
<a id="source-1115ac84904315f4-startuplogin-partial-delivery-v03---2026-09-08"></a>
### Startup/login partial delivery v03 - 2026-09-08

Supersedes SAMPLE_GATE_FAILED for the cabin only. Historical v01/v02 sources and
their failed-alpha records remain unchanged. User approved actual mask repair
after requesting the missing video. This is a TWO-LAYER integration candidate,
not all fourteen resources or a finished opening/login sequence.

<a id="source-1115ac84904315f4-accepted-for-bounded-unity-integration-review"></a>
#### Accepted for bounded Unity integration review

- Exports/login-cabin-frame-v03.png: 720x1280 RGBA. Three real transparent
  windows. Source-space mask is bounded connected-component segmentation of
  neutral bright checkerboard, one-pixel contamination removal and feathered
  boundary. No global color key. Furniture/opaque dark cabin preserved.
- Exports/login-outside-far-loop-v02.png: 1440x360 opaque panorama, generated source
  preserved. One 720x360 downsized image plus its horizontal reflection.
  Left/right edge RGB difference is zero. This is an explicitly mirrored loop,
  not a claim the original generation was seamlessly tiled. Repeated landmarks
  and velocity reversal appearance at mirror joins require moving-frame review.
  Earlier far-loop-v01 is superseded: resize introduced partial alpha at its
  outer border. v02 composites that border over the dark backing; all pixels
  are opaque. Do not select exports by directory count; follow manifest paths.

Top-left 720x1280 artboard coordinates: cabin (0,0), far strip (0,380), strip
height360 and native width1440, horizontal repeat span1440. Put far below cabin;
do not scale the strip to full screen height. Parent both beneath fixed UI.
Use a steady dark opaque backing behind the group. Candidate overscan is common
group scale1.035 about center, for restrained <=2px / <=0.12 degree movement.
This is framing guidance, not a required new resolution or runtime contract.

Visual review: scene composite and green contrast composite were actually
opened and inspected. Three windows reveal their backdrop; no painted checker
remaining in the viewed holes, opaque cabin preserved. This is static image QA,
not device/UI/motion certification. Alpha count is recorded on the master;
resized export metadata and transparent pixels must be rechecked at import.

<a id="source-1115ac84904315f4-motion-and-recording-boundary"></a>
#### Motion and recording boundary

Unity owns implementation and actual moving Play Mode frame recording. This
pair supports exterior parallax and restrained cabin rattle. Keep UI fixed;
reduced motion = static group/no rattle. Do not call a pan/zoom of a flattened
still an applied-layer recording. Preserve existing videos until replacement
is encoded and reviewed. Report resolution, duration, actual capture path and
any simulated QA login input separately from real authentication.

Near scenery, hanging prop, light mask, map/route mask and seven opening exports
are not delivered. Do not label a partial recording the full requested effect.
Rare non-strobing lighting and success route illumination remain planned until
those layers are ready. No lobby transition/gate or authentication mutation.
No bulk edit of existing resources. Unity must honor its current writer/tool
approval constraints; this delivery is not authorization to bypass a rejection.

Sources/login-cabin-alpha-master-v03.png preserves corrected full-size master;
Sources/login-outside-far-source-v01.png preserves untouched generated panorama.
Preview composites are static QA only, not video. Verification-v03 JSON records
algorithm/placements and measured data. No texture memory/build/device PASS.

<!-- source-body:source-1115ac84904315f4:end -->


<a id="source-72b59c808eee9904"></a>
## Startup Motion Sample Gate - 2026-09-08

원출처: `WorkspaceRepo/tmp/startup-motion-20260908/SOURCE-NOTE.md`  
원문 SHA-256: `29bfb45ece26b5f5534f7ba0875ad89fd18949b9472f0369812b4dee4254a8bd`  
검토·적용 경계: Current workspace guidance, routing or distinct technical handoff; not covered by this retirement batch

<!-- source-body:source-72b59c808eee9904:start -->
<a id="source-72b59c808eee9904-startup-motion-sample-gate---2026-09-08"></a>
### Startup Motion Sample Gate - 2026-09-08

Status: SAMPLE GATE FAILED. Not a Unity-ready layer delivery.

<a id="source-72b59c808eee9904-scope-and-ownership"></a>
#### Scope and ownership

Plan authorized seven opening and up to seven login motion asset candidates.
Only new startup/login assets are in scope. Existing resource bulk edits remain
on hold. Direction owns artwork preparation; Unity owns subsequent integration
and video capture under the later scoped user approval. No Assets, prefab,
import, build, authentication, economy, or lobby transition changes were made.

Read-first planning source: docs/ops/startup-motion-art-backlog.md in the planning
workspace. Owner handoff: docs/handoffs/LAST_RAIL_DIRECTION.md.

<a id="source-72b59c808eee9904-reference-and-generation"></a>
#### Reference and generation

Built-in image_gen was used, not CLI/API fallback. Existing cabin and wasteland
exterior were visually inspected. Reference root:
E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/UI/Backgrounds/.

- bg_main_lobby_train_interior.png: worn riveted metal, arched centered cabin,
  cool shadow/amber practical light. Used as the cabin extraction reference.
- bg_combat_wasteland_rail.png: illustrated armored train and perspective track,
  warm sky and weathered steel. Inspected only; no opening layer generated yet.
- bg_mission_select_rail_map.png: view_image failed to display; System.Drawing
  decoded 1080x1920 RGB. Composition suitability remains UNREVIEWED. Do not treat
  this as approved map reuse or as a corrupted-file diagnosis.

Request 1: preserve the cabin composition; remove hanging props and luminous
light pools; make the two large windows and small rear window genuinely alpha
transparent; replace rear map with blank panel; no text/logo/UI; portrait 9:16,
intended 720x1280 export. Full prompt is in manifest.json.

Request 2: targeted alpha-only correction of sample 1; retain cabin pixels and
composition, remove painted checkerboard and output actual RGBA window holes.
Full prompt is in manifest.json.

Both results are 941x1672 Format24bppRgb. Window alpha reads 255. White/gray
checks are painted pixels, not a transparency preview. Thus both are rejected
as layered assets even though the cabin art direction is plausible.

<a id="source-72b59c808eee9904-files-and-quality-gate"></a>
#### Files and quality gate

Sources contains two untouched generator outputs. Preview contains an unchanged
copy of the second rejected sample for visual inspection, NOT a composited or
animated preview. Exports is deliberately empty. manifest.json lists requested
coverage; verification.json records decoded dimensions, format, sampled window
pixels, file sizes and SHA-256. Zero approved exports and zero video files.

No alpha was fabricated, checkerboard keyed out, pixels resized/cropped, or
existing art overwritten. Source resolution is not the target export size.
Overscan, window occlusion, hanging-prop alignment, light-mask registration,
route-mask registration, rail perspective loop and seamless edges are UNVERIFIED.
Do not import these samples into Assets or record them as completed motion.

<a id="source-72b59c808eee9904-intended-motion-handoff-not-implementation"></a>
#### Intended motion handoff, not implementation

- Opening: short repeating rail travel; independent distance layers, train body,
  steam and dust/sparks. Perspective rails cannot be certified by a flat scroll.
- Login: restrained cabin rattle with sufficient overscan; fixed readable UI;
  separately scrolling exterior and gentle prop sway.
- Lighting: rare local dim/off/on, never rapid whole-screen strobe; reduced
  motion uses steady lighting or a gentle fade.
- Success: brighten cabin and sequentially illuminate separate map route mask.
  This visual cue does not authorize lobby entry or bypass runtime gates.
- Target scales: 720x1280 main, 540x960 middle, 360x640 distant, 256-512 effects.
  Extra loop width and overscan must be explicit, never silently cropped.
- 3-6 MiB textures / 30 MiB startup / 45 MiB transition remain planning targets,
  not measured allocation, package, build or device PASS.

<a id="source-72b59c808eee9904-exact-next-step"></a>
#### Exact next step

Resolve genuine alpha extraction before expanding the batch. A separately
authorized deterministic window mask cleanup or a verified alpha-capable
generation workflow is needed; neither alternative was used in this work.
Recheck transparent-pixel count, window borders over contrast backgrounds,
occlusion, target resolution and overscan; then make a second aligned layer and
review the composite before producing the full set. Unity application/video
waits for real accepted exports, not the two rejected RGB samples.

<!-- source-body:source-72b59c808eee9904:end -->


<a id="source-f2447fd0ba0513fa"></a>
## 증강 선택 최종시안

원출처: `docs/design/AUGMENT-SELECTION-FINAL-20260915.md`  
원문 SHA-256: `55797baec82e1f593b3354c56d46a7d630fae5dbd0c97ddd1fea483e33cb0a1a`  
검토·적용 경계: Current approved three-card overlay source and input boundaries; fixed five occurrences superseded by current05.

<!-- source-body:source-f2447fd0ba0513fa:start -->
<a id="source-f2447fd0ba0513fa-증강-선택-최종시안"></a>
### 증강 선택 최종시안

> 2026-09-16: [작전 맵·증강 이벤트 정책](45_MAP_STATION_DETAIL_CONTRACTS.md#source-c14755757c9a8445)에 따라 맵에 배치된 증강 획득 이벤트 완료 시 이 화면을 연다. 레버는 해당 이벤트로 향하는 선로 선택만 수행하고 증강 확정은 계속 카드 터치 전용이다. 한 판 5회, 혼합 후보 3장, 카드별 기본 3회·광고 1회 리롤 정책을 유지한다.

사용자 승인: “3번사진을 증강선택 최종시안으로 지정” — 직전 제작한 후보 3장·카드별 리롤 화면을 지정한다.

승인 이미지: [augment-selection-three-reroll-v02.png](../../../QA/Design/AugmentSelection-20260915/augment-selection-three-reroll-v02.png). 원본 파일명과 역사 자료는 보존한다.

- 승인 범위는 증강 선택 오버레이: 혼합 후보 3장, 카드별 기본 리롤 3/3·광고 리롤 1/1, 유형·효과·등급 표현과 초기화 안내이다.
- 증강 확정은 카드 클릭/터치로만 한다. 좌우 선로 레버는 증강 미리보기·선택·확정에 사용하지 않는다. 리롤 입력은 카드 확정으로 전파하지 않는다.
- 카드 선택은 선로 예약/변경을 실행하지 않는다. 기존 증강-선로 연동 계약은 이 후속 결정으로 대체한다. 증강 hold와 확정 후 운행 복귀는 유지하며 일반 선로 위험 도중 증강을 열지 않는다.
- 배경 전투 화면은 COMBAT-THIRD-FINAL-20260914.md가 기준이다. 이 이미지의 추가 배경 문구/점등 등 생성 변형을 전투 화면 승인으로 확대하지 않는다.
- 합성 시안은 reference-only이며 프레임·원화·문구·버튼·횟수·효과는 실행 UI에서 독립 조합한다. 시각 승인과 Unity 구현 완료는 별개이다.

<a id="source-f2447fd0ba0513fa-좌우-선로-레버-역할-분석"></a>
#### 좌우 선로 레버 역할 분석

사용자가 아래 고려사항의 기획 반영을 승인했다. 세부 기준과 검증 절차는 [열차 전투 상세 기획](45_MAP_STATION_DETAIL_CONTRACTS.md#source-2dc80daf4baa73f3)에 구체화했다.

증강 기능을 제거해도 기존 맵의 낙석 우회, 짧은 포대 노출로/긴 차폐로, 잔해 지름길/긴 우회 선택에 필요하다. 실제 위험·이동 거리·추격전 지속시간이 달라지며 세로 속도 레버와 지원 스킬 판단에 연결된다. 후보 3개를 좌우 2방향에 대응시키는 모호함도 해소된다.

단, 분기가 드물거나 항상 우회가 유리하면 장치가 장식처럼 느껴질 수 있다. 위험의 사전 가독성, 드래그 예약→잠금→열차 전체 경로 추종의 피드백, 선택 가능 구간의 점등과 불가능 구간의 비활성 표시를 실제 플레이에서 검수한다. 기획상 사용 기회는 있으나 구현 체감 검증은 완료되지 않았다. 사용 횟수를 채우기 위한 의미 없는 분기는 추가하지 않는다.

<!-- source-body:source-f2447fd0ba0513fa:end -->


<a id="source-61f16f3947c254bb"></a>
## 전투 화면 3차 최종시안

원출처: `docs/design/COMBAT-THIRD-FINAL-20260914.md`  
원문 SHA-256: `6d56f642b156e26707f3d3023a2da8e59c1e963b4a0d8df2f47c949174dff7ed`  
검토·적용 경계: Current approved composition source image/hash and exact visual acceptance boundaries.

<!-- source-body:source-61f16f3947c254bb:start -->
<a id="source-61f16f3947c254bb-전투-화면-3차-최종시안"></a>
### 전투 화면 3차 최종시안

사용자 지시: 선로 선택 장치를 2차 최종시안의 형태로 복원한 이번 시안을 3차 최종시안으로 삼는다.

기준 이미지: [combat-third-final.png](../../../QA/Design/Combat-Route-Speed-20260914/combat-third-final.png)

SHA256: `67AB5836A9BF512EA52F85DC4B970375AA8DDCE15E047AC4A44F7903BEBA0A0D`

- 1인용 전투 화면의 최신 시각 기준은 이 3차 최종시안이다. 1차/2차 원본은 역사 및 스타일 근거로 보존한다.
- 선로 선택: 2차의 가로 기계식 레버, 세 선로 도식, 프레임 복원. 좌측 점등과 손잡이 위치로 선택 상태를 표시한다. 중앙 직선 표시는 새로운 세 번째 선택 기능을 승인하는 의미가 아니다. 실제 가능한 경로와 입력은 선로 기획에 따른다.
- 장치 안의 우회/낙석/예약 문구와 낙석 그림은 제거한다. 위험 및 우회 정보는 외부 열차 장면에서 전달한다.
- 외부 낙석/우회/포대/추격 장면, 전체 내구도와 별도 객차별 상태, 순항 점등/지원 재충전, 기존 초상화/궁극기/지원 스킬 배치를 유지한다.
- 게이지 수치와 객차 수는 예시다. 실제 구성의 객차 수/경로 연결/시간 상태는 런타임 기획에 맞춰 표현한다.
- 이 파일은 시각 기준 등록이며 Unity 기능 구현 완료를 뜻하지 않는다. 통합 이미지는 참조용이며 프로덕션 UI는 프레임/배경/캐릭터/텍스트/게이지/아이콘/효과/입력을 독립 조합한다.

생성: 내장 image_gen, 참조 이미지 2개를 이용한 제한적 수정. [최종 프롬프트](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-7a953e58e64b5bd85c25)

<!-- source-body:source-61f16f3947c254bb:end -->


<a id="source-f05ef25c6c893f7d"></a>
## 갤러리 1–28 사용자 결정

원출처: `docs/design/GALLERY-DECISIONS-20260914.md`  
원문 SHA-256: `ae67950e7c07384ccf40ea03fd98f5032f72089ba3970a6dc19acc1c24f70c3e`  
검토·적용 경계: Authoritative individual screen approvals and later corrections; canonical boundary summary omits full numbered approval table.

<!-- source-body:source-f05ef25c6c893f7d:start -->
<a id="source-f05ef25c6c893f7d-갤러리-128-사용자-결정"></a>
### 갤러리 1–28 사용자 결정

2026-09-14 직접 사용자 지시. 번호는 REFERENCE-REVIEW-GALLERY.md의 이미지 순서이며 화면군 18개 번호가 아니다. 사용자 확인: 대상은 1–28번뿐이며 29번은 존재하지 않는다. 승인과 런타임 적용 완료는 다르다. 원본번호/경로를 보존한다.

| 번호 | 식별자 | 결정 |
|---|---|---|
| 1 | startup-01-title | 현재 구현된 Unity 화면을 최종 기준으로 지정. fresh capture/source pin 필요 |
| 2 | startup-02-update | 공통 팝업 프레임 재디자인 시안. 버전 표시 제거. 8/10/11에 같은 디자인 적용 |
| 3 | startup-03-login | 기존 구성 유지하고 일반 타이틀 텍스트 대신 타이틀 디자인 적용 시안 |
| 4 | startup-04-id-login | 현재 제시 이미지 최종 승인 |
| 5 | startup-05-register | 현재 제시 이미지 최종 승인 |
| 6 | startup-06-recovery | 로그인으로 돌아가기 텍스트 중앙 정렬 수정 |
| 7 | startup-07-nickname | 이 이름으로 시작 → 출격 |
| 8 | startup-08-download-consent | 2번 새 공통 팝업 디자인 적용 시안 |
| 9 | startup-09-download | 현재 구현된 Unity 다운로드 화면을 최종 기준으로 지정. fresh capture/source pin 필요 |
| 10 | startup-10-network-error | 2번 새 공통 팝업 디자인 적용 시안 |
| 11 | startup-11-internal-access | 2번 새 공통 팝업 디자인 적용 시안 |
| 12 | screen-departure-v02 | 현재 제시 이미지 최종 승인 |
| 13 | screen-encyclopedia-kyle-v02 | 현재 제시 이미지 최종 승인 |
| 14 | screen-settings-account-unavailable-v01 | 현재 제시 이미지 최종 승인 |
| 15 | screen-settings-display-loading-v01 | 현재 제시 이미지 최종 승인 |
| 16 | screen-settings-game-v02 | 현재 제시 이미지 최종 승인 |
| 17 | screen-settings-sound-v01 | 현재 제시 이미지 최종 승인 |
| 18 | screen-story-archive-v01 | 현재 제시 이미지 최종 승인 |
| 19 | screen-tutorial-formation-v01 | 분대장 개념 삭제에 따라 튜토리얼 화면 수정 |
| 20 | candidate-commander-selection-v01 | 현재 제시 이미지 최종 승인 |
| 21 | candidate-defeat-v01 | 현재 제시 이미지 최종 승인 및 보상 정보 확인중 이후 결과 전이 실제 검증 |
| 22 | candidate-victory-v01 | 현재 제시 이미지 최종 승인 |
| 23 | candidate-attendance-v01 | 현재 제시 이미지 최종 승인 |
| 24 | candidate-offline-supply-ad-v02 | 현재 제시 이미지 최종 승인. 운행 보급 기준은 이 안 |
| 25 | candidate-offline-supply-v01 | 25번 구안 폐기. 24번으로 대체하고 현재 선택/사용 목록에서 제거 |
| 26 | candidate-pass-v01 | 선택수령 → 전체 수령. 실제 전체 수령 동작/수령가능 항목 범위 확인 |
| 27 | candidate-supply-roulette-v01 | 룰렛 위 광고 시청 완료 문구 제거. 실제 광고 검증 로직은 유지 |
| 28 | candidate-battle-pause-v01 | 일시정지 팝업 범위 확인 및 해당 팝업 최종 승인 |

25번 삭제는 현재 후보/실사용 선택에서 제외하고 폐기 표시한다. 역사 원본은 우선 보존하며 제거 대상 원본 경로를 기록한다. 19번은 분대장 개념 삭제이며 20번 지휘관/지원 선택과 동일 개념인지 소스/문구 확인 없이 함께 삭제하지 않는다.

28번 직접 확인: Settings-20260911/FLOW.md는 배경 전투/대원 카드를 분위기용이라고 명시한다. 이번 승인은 중앙 일시정지 팝업에 한정하며 전투2차최종을 대체하지 않는다.

<a id="source-f05ef25c6c893f7d-수정-시안-직접-승인-및-추가-수정--2026-09-14"></a>
#### 수정 시안 직접 승인 및 추가 수정 — 2026-09-14

사용자가 GalleryRevision01의 2·3·7·8·10·11·19·26·27번 수정 시안을 승인했다. 해당 번호의 v02 이미지를 최종 시각 기준으로 적용한다. 이전 ‘새 10장 승인 대기’ 상태를 이 9개에 한해 대체한다. 승인은 리소스 분리/실제 기능 검증 완료를 의미하지 않는다.

- 1번 현재 Unity 오프닝 타이틀: 일반 타이틀 텍스트를 승인된 3번의 금속 로고/심볼 디자인으로 교체한다. 기존 오프닝 배경과 진행 동작은 유지한다. 로고는 별도 시각 요소로 공급한다.
- 6번 복구 화면: 사용자 첨부에 표시된 자물쇠 장식과 그 아래 잘린 잔여 장식을 제거한다. 앞서 반영한 복귀 버튼 중앙 정렬과 나머지 구성은 유지한다. 기능상 복구/인증 로직을 제거하는 요청이 아니다. 수정 이미지와 실제 화면을 확인한다.
- 26번은 승인된 ‘전체 수령’ 표시와 실제 무료/프리미엄 수령 가능 보상 전체 처리 및 부분 실패 보존을 함께 구현·검증한다.

근거 첨부: C:/Users/dlstj/AppData/Local/Temp/codex-clipboard-5ff62452-e2bc-4f18-8a26-83ca34e55e94.png (사용자 제공 참고 이미지, 프로젝트 신규 산출물 아님).

<a id="source-f05ef25c6c893f7d-2026-09-17-공통-룰렛-결과-팝업-최종-승인"></a>
#### 2026-09-17 공통 룰렛 결과 팝업 최종 승인
사용자: “좋아 이거로 최종시안 결정”. 승인 파일: QA/CombatAdUnity-20260917/Concept05/shared-roulette-result-reference-only.png. 보급·작전 보상 공통 룰렛의 지급 확정 후 결과 상태 최종 시각 기준으로 지정한다. 승인 범위는 결과 팝업이며 회전 전 Concept04의 별도 최종 승인을 추정하지 않는다. 기존 Gallery27 v02와 모든 원본은 보존한다.
파란 분할 룰렛 유지, 당첨 칸 강조, 중앙 확정 배수, 기존 보상 카드의 이름/아이콘/하단 중앙 수량 배치, 확인 버튼 하나를 사용한다. 제목·중복 완료 문구·닫기 X·확률 링크는 결과 상태에서 표시하지 않는다. 4배와 3,120/20/640은 예시이며 실제 배수·수량은 서버 확정 결과로 표시한다. 보급/작전 모두 같은 View를 쓰고 각 Controller가 데이터와 콜백을 연결한다. 프레임·배경·룰렛·포인터·배수·보상 아이콘/이름/수량·버튼·효과를 독립 제어한다. 승인 이미지는 reference-only이며 한 장짜리 실행 UI로 사용하지 않는다. 최종시안 승인 완료, Unity 반영 및 실제 화면 검수는 미완료.

<!-- source-body:source-f05ef25c6c893f7d:end -->
