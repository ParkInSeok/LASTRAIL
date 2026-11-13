
> **2026-10-02 현행 전투 변경:** A안 신규 적용 폐기, B안 채택. [44 현행 전환 결정](44_COMBAT_DETAIL_CONTRACTS.md#combat-b-mainline-approved-20261002)이 과거 A 유지/B 임시 및 객차 장착 포탑 전제보다 우선한다. 특정 노드 내부전은 별도 유지. 미승인 세부·기존 snapshot·역사자료는 구분하며 일괄 삭제/수치 치환하지 않는다.
# 46. 협동·매칭·입장·역할 상세 계약

> **최신 일반 협동 보상 정정:** [v03](#ordinary-coop-survivor-clear-v03) — 결과 ACK 필수 폐기, 원최종기록 대리 제출 허용, 전투 중 이탈 후 잔류자 완주 시 잔류자만 클리어 보상. 호스트 이탈 시 서버 승인 이관 필요.

> **2026-09-22 최신 사용자 확정:** 일반 협동은 단일 클라이언트 호스트 전투, 월드보스 솔로·협동은 서버 판정 유지. [모드별 책임·후속 경계](#authority-split-host-coop-v01)가 구 일반 협동 서버 전투 정책보다 우선한다.

2026-09-21 번호별 통합. 이 문서는 흩어진 상세 계약을 주제별로 모은 참조본이다. 원문별 제목·출처·기존 검토 상태를 유지하고, 정책 확정·후보·과거 구현 보고를 섞어 승인하지 않는다.

**적용 순서:** 최신 사용자 결정 → 00~29 담당 정책 및 34~37 충돌 정리 → 이 문서의 충돌하지 않는 상세 계약. 원문의 작성일·완료 문구만으로 현재 구현 또는 승인 완료를 뜻하지 않는다. 원문에 있던 C/E 경로는 과거 기록이며 현재 작업 경로는 프로젝트 AGENTS.md의 D 정책을 따른다.

**공통 대체 경계:** 증강 고정 5회·시작 지급은 폐기, 적격 풀 동일 가중치 3후보·카드별 FREE3+EXTRA1·선택 중 완전 정지가 현행이다. 증강 후보 효과 수치는 출시 승인값이 아니다. 순위 보상은 성장 재화 차등 없이 외형·명예로 구분한다. 인물 역할은 20~29 최신 바이블 우선이며 과거 abilityId 매핑은 자동 변경하지 않는다. 작전물자 30/20·노드3선택/물리2방향·협동 시간·T18 HP0 등 미결정은 34와33의 미결 상태를 유지한다.

원문 내 코드 블록·JSON·표·필드·수치는 생략하지 않았다. 옛 상대 경로를 포함하는 코드 예시는 원출처 기준이며 실행 지시가 아니다. 링크와 제목 앵커만 새 위치에 맞게 변환했다.

## 상세 목차

- [Coop Status And Signals V1](#source-18f61d36e170ad16)
- [Invite Code Lobby Flow V1](#source-60368238f12ef8db)
- [Solo And Online Cooperative Mode Policy](#source-29ac58e206039d36)
- [Player Skill Input And Cooperation Detail](#source-cb160e8c21eae28f)
- [Postbattle Retry And Partner Continuation V1](#source-a508f1b20654cb63)
- [Random Match Queue And Acceptance V1](#source-049dad14b54e01a7)
- [Online Cooperative First Match Implementation Plan](#source-7a7421f71c42e376)
- [2인 전투 — 열차 / 요원 역할별 화면 기획](#source-2a8c21536363ec8b)
- [역할별 증강 선택 상세 제안](#source-cda19b06fbf79722)
- [협동 보상 소유권 조사와 광고 룰렛 구현 범위](#source-f02428cfe7a062db)
- [2인 작전 물자·정비역·사건 의사결정 상세안](#source-e5a41af1ba3b3528)
- [2인 통합 증강 선택·카드별 리롤 상세안](#source-991422e2420e54e4)

<a id="source-18f61d36e170ad16"></a>
## Coop Status And Signals V1

원출처: `WorkspaceRepo/docs/design/coop-status-signals-v1.md`  
원문 SHA-256: `fcc12c565b19582f40088a00643d7a60e4ac66c07453283c89e5a9392a136501`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Rolecontrolreadonlypartner, pingsnotcommand/credit/focus;3s/2s candidates; originalissuedexpiry,no replay/queue; stale unknownnotready.

<!-- source-body:source-18f61d36e170ad16:start -->
<a id="source-18f61d36e170ad16-coop-status-and-signals-v1"></a>
### Coop Status And Signals V1

2026-09-09 / Plan / proposed presentation detail, no implementation.
Source: player-input-cooperation-detail-v1.md optional role signals and
battle-connection-recovery-v1.md. Retain three signal types, sender3s throttle,
2s lifetime as existing candidates, not newly validated production values.

<a id="source-18f61d36e170ad16-layout-and-information"></a>
#### Layout And Information

Both players retain upper exterior/lower interior orientation. Ally formation stays
inside the lower-left area, left3/right2, with enemies to the right. Role does not
mirror or swap the battle layout. My role exposes its own interactive controls;
partner status is read-only, not duplicate actionable buttons.

A compact shared status strip at the boundary of the two views contains partner
display name, role icon, connection state and role availability. Preserve safe area,
fixed dimensions and readable TMP; status changes cannot move gameplay controls.

- Squad player sees train current/max durability, locomotive main action readiness
  and relevant disabled-device warning from authoritative data. Do not invent a
  new repair action or show all internal device timers as a dashboard.
- Train player sees five small operator portraits with HP/down/recovery states and
  selected ultimate readiness. Whole squad availability is not inferred from one
  downed operator. Do not expose five extra partner skill buttons.
- Ready, cooldown, recovering and connection-unknown are distinct. On stale data,
  show unknown/connection warning, not a misleading green ready indicator.
- Actual field availability is a server presentation contract prerequisite. No
  client-derived authoritative HP/cooldown or automatic private inventory exposure.

<a id="source-18f61d36e170ad16-signals"></a>
#### Signals

| Signal | Scope | Meaning |
| --- | --- | --- |
| Danger | Valid visible mechanic/entity or my role's area | Draw attention, not an automatic dodge or counter |
| Need defense | Explicit train or squad area | Request help appropriate to that area; not a guaranteed shield/heal |
| Focus target | My existing valid visible target | Suggest a target; recipient selection remains unchanged |

Recommended interaction: compact signal icon opens three fixed icon choices.
Uses already selected target or labeled role-area context; no new targeting mode,
radial gesture dependency or hidden long press. Focus is disabled with no valid target.
Signal send consumes the tap; it cannot also cast or change focus underneath.
Opening the small chooser does not pause combat and closes on selection/cancel.

Use shapes/icons plus short localized labels, not color alone. Size touch targets
against existing mobile UI policy; no unverified pixel-size exception. A partner
signal is an informational overlay, not a clickable command prompt.

<a id="source-18f61d36e170ad16-delivery-priority-and-noise"></a>
#### Delivery, Priority And Noise

- Server checks membership/run/visible valid target and enforces combined3s sender
  cooldown across all signal types. Duplicate identity displays once. No score,
  buff, skill activation, quest activity or participation credit from signaling.
- Signal lifetime2s from authoritative issue time; late/expired messages are dropped,
  not shown for another full2s after reconnect. Dead target clears its marker; do
  not silently attach it to another enemy. Reconnect never replays old signals.
- Threat telegraph outranks partner signal; signal cannot obscure marked actors,
  HP, skill controls or countdown. When overlap occurs use a small status-strip
  notice rather than covering the threat. Do not queue old signals for later.
- Provide optional partner-signal sound/mute independently of critical mechanic
  warnings. Muting optional signals never disables server-required threat cues.
- On connection loss disable sending; no deferred sends when connection returns.
  Report rejected send unobtrusively without interrupting combat with a modal.

<a id="source-18f61d36e170ad16-mode-boundaries-and-examples"></a>
#### Mode Boundaries And Examples

Solo hides partner strip and signal controls. No artificial partner messages or
manual ping requirement. Story remains solo. No new launch availability is implied.

Example: squad sees interior sabotage and sends Danger. Train player sees the
marker, but train barrier still only protects train. Interrupt/focus are performed
by the appropriate player's actual controls, not by the signal itself.
Example: train requests defense; squad can choose an available compatible action
such as Mira repair. If unavailable, no penalty or scripted automatic action occurs.

<a id="source-18f61d36e170ad16-future-verification"></a>
#### Future Verification

Check narrow portrait screens, both roles, long localized names, all5 down,
one operator down, disconnected stale readiness, mechanic/signal overlap, duplicate
and late signals, invalid target, muted signals, background return and tap-through.
Neither signals nor low damage may disqualify legitimate support participation.
These cases are NOT run. Policy comparison and document readback only; local
uncommitted, no code/tests/deploy/video/owner order. Unity reference-search assignment
continues separately; no assumption that it has completed.

<!-- source-body:source-18f61d36e170ad16:end -->


<a id="source-60368238f12ef8db"></a>
## Invite Code Lobby Flow V1

원출처: `WorkspaceRepo/docs/design/invite-code-lobby-flow-v1.md`  
원문 SHA-256: `53be898c9c8c71c12425623ab791aa95664637d01f6364c5549ee24dad27b616`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Fourdigit leading0 code locator notmembership; immutable room+generation,expiry separatemembership, approval/withdraw/start races atomic; no hostroletransfer; carrybelowtop2atleastoneeligible nothostonly; capsTTLpending.

<!-- source-body:source-60368238f12ef8db:start -->
<a id="source-60368238f12ef8db-invite-code-lobby-flow-v1"></a>
### Invite Code Lobby Flow V1

2026-09-09 / Plan / UI refinement, not implemented or deployed.
Source: online-coop-mode-policy.md. Existing4-digit server-issued code and host
approval retained. Codes locate rooms, do not authenticate or establish membership.

<a id="source-60368238f12ef8db-host-and-guest-flows"></a>
#### Host And Guest Flows

Host selects eligible coop content, difficulty where applicable and own role,
then creates invitation lobby. Show code with copy/share actions and remaining
validity from server. No cost for code generation/room creation. No added OS
contacts permission, friend graph or external messaging service requirement.

Guest logs in -> Join By Code -> explicit input/paste -> request join -> host
approval -> validate membership/role/content -> preparation. Do not auto-read
clipboard or send lookups on every keystroke. Preserve leading zeroes as text.
DEV/PRD and server routing must not cross-resolve the same digits; no prefix design
change here. Unmatched routing returns unavailable, not a different server's room.

Before approval, disclose only necessary safe display names and proposal context,
not full inventory/raw account IDs/session metadata. Host accepts/declines the
identified request. Guest can withdraw while pending. Approval is not automatic
ready or debit consent. Once admitted, both see operation/role/cost and ready normally.

<a id="source-60368238f12ef8db-membership-races"></a>
#### Membership Races

- Bind request to immutable room identity, code generation and current preparation
  revision. Two simultaneous approvals can fill only the one available guest slot.
- On code regeneration revoke pending requests for old generation; approved member
  remains unless explicitly removed. Code expiry prevents new lookup, does not
  automatically expel approved member. Room expiry remains a separate policy.
- Changing operation/difficulty while guest is pending requires renewed acceptance
  of the changed proposal; no surprise join to higher-cost/different content.
- Guest withdraw versus host approval resolves once. If already admitted, use normal
  prebattle leave; do not leave an invisible membership and create another room.
- Once full, stop new requests. On start/closure retire code. Old code cannot admit
  a replacement to an active battle or substitute for original-account reconnect.
- Host removal before start clears ready states. Removal racing with start resolves
  on server; no unilateral kick path after accepted battle start.

<a id="source-60368238f12ef8db-eligibility-display"></a>
#### Eligibility Display

Invite ordinary below top2 difficulty tiers: at least one participant has selected
difficulty unlocked and both satisfy common baseline requirements. This does NOT
require the host specifically to be the unlocked participant. Roles still require
each player's own legal assets. Top2 tiers require both independently eligible.
Power is not an admission threshold. Show 'invite condition satisfied' neutrally,
not a stigmatizing newcomer/carry badge. Invite clear does not unlock unrelated story.

World boss: shared common entry rules and current event, no difficulty, story
completion not newly required. Story/story raid has no invite mode.

<a id="source-60368238f12ef8db-errors-and-spam-boundaries"></a>
#### Errors And Spam Boundaries

Use one safe unavailable response for invalid/expired/closed/nonjoinable code rather
than expose a directory of accounts/rooms. Keep current input editable, no automatic
brute-force retries. Distinguish transport failure from an accepted join request;
retry with original identity instead of submitting duplicates.
Rate limits and request/code TTL require owner configuration/measurement, not new
unverified numeric constants. Respect server retry-after. Do not reuse random
match15s offer window as invite host deadline; host may be sharing code elsewhere.

Pending requests must be bounded server-side and one guest must not fan out into
unlimited rooms. Exact caps not set here. No client-only protection assumption.
Host disconnect before start disables starting until reconciled; do not silently
transfer host ownership. Prebattle room lease/closure timeout needs owner contract.
After start, original host disconnect does not stop the server-owned battle.

<a id="source-60368238f12ef8db-evidence-and-next"></a>
#### Evidence And Next

Future cases: leading0, two joiners, stale generation, expiry with approved guest,
role/content edit, withdrawal/approval race, kick/start race, wrong environment,
transport replay and rate limit. Not executed. Policy read and document readback
only. Local/uncommitted, no code/tests/deploy/video or owner assignment.
Next implementation mapping must confirm bounded requests, generation identity,
membership persistence and actual invitation expiry before shipping this UI.

<!-- source-body:source-60368238f12ef8db:end -->


<a id="source-29ac58e206039d36"></a>
## Solo And Online Cooperative Mode Policy

원출처: `WorkspaceRepo/docs/design/online-coop-mode-policy.md`  
원문 SHA-256: `45bbfe87059132f32d31fab2b61d31d9d99946344b8dcd178a0723578b24e906`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Consolidated storysolo/basic4typesIDunknown/world1or2no difficulty; roleownassets/fixed; carrybelowtop2invite versusrandomindependent; WSserverauth replaces250mspoll, socket≠membership; recurringtrackshared no damagequota.

<!-- source-body:source-29ac58e206039d36:start -->
<a id="source-29ac58e206039d36-solo-and-online-cooperative-mode-policy"></a>
### Solo And Online Cooperative Mode Policy

<a id="source-29ac58e206039d36-status"></a>
#### Status

Consolidated user decisions; this is not implemented networking or release approval. World Raid mechanics, tickets, and settlement are in [world-raid-rules.md](44_COMBAT_DETAIL_CONTRACTS.md#source-f676f54cacce84e4).

<a id="source-29ac58e206039d36-content-matrix"></a>
#### Content Matrix

| Content | Solo | Online two-player | Difficulty |
| --- | --- | --- | --- |
| Story and story raid | Yes | No | Existing story rules |
| Four basic operation types | Yes | Yes | Existing operation difficulty tiers |
| World Raid | Yes | Yes | None; event boss and mode only |

The exact four operation type IDs must be recovered from authoritative design/source before routing or migrations are written. The six Act 1 mission IDs are missions, not evidence of four operation categories. Do not invent names or use Rail Choice Events as a category.

<a id="source-29ac58e206039d36-roles-and-assets"></a>
#### Roles And Assets

- Each participant uses their own phone/account: one train role and one squad role.
- Train player uses their own train, parts, and weapons. Squad player uses their own five operators with their upgrades/equipment.
- Preserve the external/internal split; role-specific emphasis and controls may differ. Inside the train, allies remain in the left region with the accepted 3/2 arrangement and enemies on the right.
- Solo focuses on squad control plus train support commands. Solo mechanics must be sequentially solvable; cooperative versions may require complementary actions within a shared window.
- Changing role/operation/difficulty/loadout before departure clears both ready states and triggers eligibility/ownership revalidation. No role or asset exchange during combat.
- No asset ownership transfer. Historical ranked builds show only the score-producing public loadout, never current inventory presented as history.

<a id="source-29ac58e206039d36-invitation-codes"></a>
#### Invitation Codes

- Server issues four numeric digits, including leading zeroes, unique among active code-bearing lobbies within the lookup routing scope.
- A code discovers a lobby and submits a join request; it does not grant membership. Require login and host approval before admission.
- Show only safe display names before approval. Provide copying/sharing, request rejection, pre-combat removal, code regeneration, expiry, and server rate limits.
- Retire the code on start, closure, or expiry. Code exhaustion must return controlled unavailability, never overwrite another active lobby.
- Bind approvals to immutable room ID and code generation so a stale request cannot enter a different room that later reuses the digits.
- Reconnection uses original membership, not the code. No new participant joins an active attempt. Host disconnection does not terminate a server-owned fight.
- Exact code TTL/rate-limit configuration requires implementation measurement. Local test constants must not silently become production policy.

<a id="source-29ac58e206039d36-random-matching-and-carry"></a>
#### Random Matching And Carry

- Random matches require the same operation ID, selected difficulty, mode/rules version, complementary roles, and independent eligibility for both accounts. Combat power is not a filter.
- Waiting longer cannot relax content/difficulty eligibility. A player choosing either role can be assigned one role before readiness, not two roles at once.
- World Raid matches the same event/boss/mode/rules version and complementary roles with common content eligibility. It has no difficulty filter.
- Invited ordinary-operation parties may carry a basic-content-eligible player below the top two difficulty tiers if at least one participant has the selected difficulty unlocked.
- At the top two ordinary-operation tiers, both participants must qualify independently. Random matching has no carry-admission exception at any tier.
- Low power/damage does not reduce a valid player's reward. Do not require both roles, individual last hits, or minimum damage to complete quests.
- Carry completion counts for eligible rewards/quests, not automatic story or chained-difficulty unlocks. Existing unlock authority remains intact.
- Recheck all entry constraints at start. An invite exception cannot be carried into a random match by changing queue or role.

<a id="source-29ac58e206039d36-recurring-quests"></a>
#### Recurring Quests

- Solo-only, cooperative-train-only, cooperative-squad-only, and mixed play can each obtain the full recurring reward track.
- Share progress and claim state across modes. Do not create separately required mode objectives or doubled payouts.
- No recurring requirement to invite a friend, play both roles, clear solo-only story, meet personal damage/kill quotas, or complete both modes.
- Use equivalent completion units: two complementary actions of one cooperative mechanic count as one completion.
- World Boss has one weekly participation objective, no daily duplicate, no win requirement, and no mechanic-success quota. Its direct reward is the World Raid Participation tier, not another duplicate bundle.
- Server validates events, reset boundaries, progress, and grants idempotently. The World Raid ticket budget does not replace ordinary-operation entry costs.

<a id="source-29ac58e206039d36-implementation-and-release-boundaries"></a>
#### Implementation And Release Boundaries

<a id="source-29ac58e206039d36-real-time-session-decision"></a>
##### Real-Time Session Decision

- Use a persistent authenticated WebSocket connection for cooperative room/match state and gameplay commands. This is an in-app bidirectional game connection, not mobile OS notifications.
- Both clients connect to the same server-owned room/attempt authority. The server validates role commands and pushes accepted state/events to both; clients do not exchange authoritative state peer-to-peer.
- Keep HTTP for authentication, initial room lookup/admission where suitable, record queries, and reward claims. The previous 250ms polling plan is superseded as the first actual cooperative verification path, not a parallel score authority.
- A room-scoped Cloudflare Durable Object is the selected architectural direction to evaluate and validate; bindings, persistence, transport implementation, and measured suitability are not yet established. Do not equate this decision with live infrastructure approval.
- Socket lifetime is separate from account session, room membership, and attempt lifetime. A disconnect must not delete/reset the attempt or grant another account access on reconnect.
- Review authentication at upgrade and during reconnect/session expiry, original-account membership, stale-connection fencing, acknowledged command IDs, snapshot revisions, full-state resynchronization, backpressure, and payload limits before implementation.
- Gameplay state includes authoritative time/phase, health/recovery, mechanic outcomes, buff windows, and combined score. Animation frames and camera motion stay local. State update frequency is a measured implementation parameter, not a promised frame rate.
- A live simulation tick and mobile radio activity are not free during an idle/hibernation-capable connection. Validate room restoration, timer catch-up, actual idle behavior, latency, and cost before choosing runtime scheduling.
- Reference: https://developers.cloudflare.com/durable-objects/best-practices/websockets/

- First engineering target: two distinct authenticated local clients, real command/snapshot transport, authoritative server state, and existing Unity HUD assets in an isolated test encounter.
- Deterministic fixture combat is evidence of protocol/state flow, not a finalized skill engine, real player inventory, public matchmaking, or production account progression.
- No new art/video is required for this proof. Screenshot fixtures alone do not prove actual controller/transport wiring.
- Remote migrations/seeds, live infrastructure, secrets/OAuth registration, production deployment, store/signing, and video remain subject to their existing separate approvals.

<!-- source-body:source-29ac58e206039d36:end -->


<a id="source-cb160e8c21eae28f"></a>
## Player Skill Input And Cooperation Detail

원출처: `WorkspaceRepo/docs/design/player-input-cooperation-detail-v1.md`  
원문 SHA-256: `226b9cfbc2e8ed030a5b1d96a73cb157500b5d2d885a8cb96163195ac6cdb477`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Input tap350ms/48units and queue1s candidates; preserve ownrole/targetvalidity/cancelrace/commandidentity/queueACKnotcastCD, no committedretarget/tapreplay. oldno fullscreenpause superseded04§14.

<!-- source-body:source-cb160e8c21eae28f:start -->
<a id="source-cb160e8c21eae28f-player-skill-input-and-cooperation-detail"></a>
### Player Skill Input And Cooperation Detail

2026-09-09, Plan. PROPOSED interaction specification, design only.
Sources: combat-control-detail-proposal-20260909.md, combat-skill-control-proposal-
20260909.md, combat-stats-v1.md/.json, enemy-ai-wave-execution-v1.md.
Retains approved real-time automatic basics/manual skills/fixed formation.
No implementation, art generation, video, runtime test or deployment order.

<a id="source-cb160e8c21eae28f-control-surface"></a>
#### Control Surface

Always show both exterior and interior. Interior allies stay within LEFT region,
rear column3/front column2, enemies RIGHT. No drag-to-move or formation edit in combat.
Five portrait actives + one selected ultimate + three train commands, no extra MP
bar, no per-use persistent currency. Ultimate is the chosen operator's, not five
simultaneous ultimate controls. Damage and recovery counters remain mode-specific.

Solo: controls both groups. Coop squad role: five actives/selected ultimate and
interior focus. Coop train role: three train commands and exterior focus. Other
role controls are compact read-only readiness indicators, never actionable buttons.
No role swap, borrowed loadout, dual-owner stat stacking or peer-host authority.
Role ownership is enforced server-side, not just by disabling a UI button.

Input hit area proposal: minimum48 logical UI units at a reference360-wide mobile
canvas, five portraits in one row with8-unit gaps and16-unit side margins; these
fit as5*48+4*8+2*16=304 units. Art can be smaller than its hit area. Safe-area
insets are additional layout constraints; do not assume all devices have344 units.
Use layout fallback before reducing hit targets; train controls occupy separate
row from the ultimate. Exact placement awaits real portrait device testing.

<a id="source-cb160e8c21eae28f-gesture-model"></a>
#### Gesture Model

- Portrait tap: self/automatic fixed-target action submits once, selectable-target
  skill enters preview. Long press350ms opens description without activation;
  release closes description. Long press consumes the gesture, no trailing tap.
- Target preview: tap valid marked target to submit. No double-tap requirement.
  Another skill replaces the unsubmitted preview; cancel/back closes it at no cost.
  Tapping empty battle area cancels; dragging never moves units or commits a skill.
- Enemy tap with NO skill preview submits focus, not an attack or skill. With
  preview active, taps belong to that skill and cannot accidentally change focus.
- Release outside a button or OS interruption cancels unsubmitted gesture.
  One pointer owns the current preview; extra simultaneous pointers cannot confirm
  another actor's skill. Independent accepted actors still act concurrently.
- Opening description/aim/cancel never pauses battle or hides essential warnings.
  No auto-activate on cooldown completion, held touch, or returning from background.

<a id="source-cb160e8c21eae28f-target-modes-match-catalog"></a>
#### Target Modes Match Catalog

Do not turn every skill into arbitrary ally/enemy selection; that changes its kit.

| Catalog target | Proposed interaction | Validation |
| --- | --- | --- |
| self / train / all_allies | One tap submit; show affected group | Owner/caster/target state valid |
| ally_lowest_hp | One tap; highlight proposed lowest ally | Resolve ratio at cast start, stable tie |
| enemy_focus_any | Preview current focus, tap desired legal enemy | Skill target need not alter squad focus |
| enemies_front_2 / enemies_any_3 | Preview deterministic group, explicit confirm | No free choice of arbitrary extra targets |
| external_focus | Preview exterior target, tap confirm | Train role and exterior target only |

Group membership previews can become stale while battle runs. On cast start choose
current valid group by catalog; display authoritative group immediately. Never
silently redirect to another lane or change a single manually chosen target.
If manually selected target invalid before cast start: reject/cancel queue, no cost.
Auto-target chooses at actual cast start, not the earlier local preview timestamp.
After windup begins, snapshot target IDs. Before commit, a now-invalid target loses
that effect; multi-target skills retain valid members without unmarked replacement.
After commit projectile never retargets. Beneficial effects cannot heal down allies.
This player-target proposal narrows combat-stats-v1's generic pre-commit fallback;
enemy BASIC fallback remains the separately telegraphed rule in enemy-ai spec.

If lowest-HP heal has no missing living HP, reject with no cooldown. For combined
shield/cleanse/taunt kits reject only if NO effect can apply; a shield at full HP
is useful. Support fire with no enemy rejects. Train repair at full HP rejects
unless another attached effect can apply (Mira ultimate damage buff, for example).
Do not reject useful status refresh solely because another effect would overheal.

<a id="source-cb160e8c21eae28f-intent-queue-and-cooldown-states"></a>
#### Intent, Queue And Cooldown States

UI states: ready -> aiming (optional) -> submitting -> queued OR casting -> cooldown.
Rejected returns to authoritative readiness; down/controlled/ended are explicit
disabled states. Submitting means outcome unknown, not a successful cast animation.

Command identity: attemptId, unique commandId, actorId, skillId, optional targetId,
observed revision. No client damage/cost/HP claims. Uncertain submission reuses the
same commandId; reconciliation cannot create another activation. Closed attempts
reject all actions even if a stale screen still shows ready.

One queued command per ACTOR, not a shared team turn. A ready skill can queue behind
that actor's committed basic and recovery for at most1000ms. Further taps return
the existing pending status; never append repeated casts. Different actors can
cast concurrently. No queue behind another skill, stun, down or cooldown.
Queued request reserves only input position, NOT cooldown or currency. Revalidate
when it starts; expire with no cooldown if1s passes or target/actor becomes invalid.
Skill cooldown begins ONLY at cast start, not queue acknowledgment. This clarifies
earlier ambiguous "accepted cast" wording: accepted-for-queue is not cast-start.

Queued cancellation is a server command: if cancel wins before cast-start, remove
queue; if cast-start already won, reply too-late and retain cooldown. Local cancel
does not assert cancellation. Once casting, no voluntary cancel for a refund.
Interruption/down after cast start consumes the normal cooldown as stat proposal
specifies, not the older unadopted3s retry. Already committed effects follow their
continuation rules. No global cooldown between different operators.

No response after1s: show connection/pending indicator, keep same commandId and
request status. Do not label failure/refund solely on elapsed client time. Confirmed
transport loss disables new remote commands until reconciliation. Authoritative
simulation/time does not pause; existing reconnect/takeover policy still governs.

<a id="source-cb160e8c21eae28f-ordinary-train--squad-interactions"></a>
#### Ordinary Train / Squad Interactions

<a id="source-cb160e8c21eae28f-focus-selection-lifecycle-proposal---2026-09-09"></a>
##### Focus Selection Lifecycle Proposal - 2026-09-09

Clarifies reachable-target focus in combat-stats-v1 without replacing kit targets.
Recommend one interior focus and one exterior focus per attempt, independently.
Solo may edit both; coop squad edits interior and train role edits exterior. Partner
signals remain suggestions, not focus commands. No account-wide or next-run enemy ID.

- With no skill preview open, tap a targetable compatible enemy to request focus.
  Display pending/accepted distinctly; the authoritative accepted selection owns the
  persistent marker. Repeated tap on the same target is a no-op, not toggle or attack.
  A small clear-focus control returns that lane to its existing automatic priority.
- A focus influences the next eligible automatic attack's target selection, subject
  to reach and effect scope. Front-limited basics cannot shoot an unreachable rear
  enemy merely because selected. Those actors continue their normal legal fallback;
  do not stop the whole squad waiting for an unreachable focus.
- Do not reset attack interval, cancel committed windup for a free attack or redirect
  emitted projectiles. Existing invalid-target/re-windup policy remains separate from
  voluntary focus change. No new damage/mark bonus from focus itself.
- Selecting a skill target does not change persistent focus; self/lowestHP/group
  skills retain their catalog selection. A group attack is not forcibly narrowed to
  one focused target or extended to another lane. Device focus routing must follow
  its explicit existing contract, not infer owner permissions from this UI proposal.
- When focus dies or permanently leaves the encounter, clear it and fall back to
  normal legal target selection. Do not silently choose a new manual focus, carry
  the old entity ID into another wave, or automatically pick the next saboteur.
  Temporary untargetability may preserve the marker with unavailable treatment while
  normal attacks use valid alternatives; it does not confer invulnerability bypass.
- Reconnect restores accepted focus if still valid, not a stale local tap. Wave/new
  attempt/terminal transition clears obsolete IDs. A returned stale command cannot
  restore a dead target or overwrite a newer accepted focus revision.

Boss exterior/interior proxies represent the same boss entity but retain their lane
display/target eligibility. Two focus markers do not create two health pools, doubled
AOE hits or permission for an interior attack to hit arbitrary exterior enemies.

Use a visible target marker without concealing HP/warning. Reach limitations should
not produce repeated blocking popups; selected focus means priority where legal,
not all five guaranteed to hit. Keep hit areas independent from illustration alpha
and reserve skill-preview input ownership. No automatic recommended-target purchase
or hidden future-wave information. Numeric input throttling remains an owner concern,
not an added per-use cost or gameplay cooldown in this proposal.

Future cases: front-limited actor with rear focus, target death during windup, emitted
projectile, target temporarily unavailable, repeated taps, stale accepted revision,
skill-preview tap, coop unauthorized lane, wave transition and shared boss proxy.
Source-policy comparison/readback only; no input/runtime tests or new API assumed.

Interactions initially use existing effects, not a new combination gauge or hidden
combo damage multiplier. Sequencing is a tactical opportunity, not a scripted QTE.

| Threat | Squad action | Train action | Result |
| --- | --- | --- | --- |
| Interior sabotage | Focus source or Sera interrupt | Barrier before pulses, repair actual loss | Stop future pulses and sustain train separately |
| Heavy front hit | Kyle shield or available control | Continue exterior fire | Interior shield absorbs normal hit; train fire does not magically stun brute |
| Jammer | Focus/interrupt, later cleanse if available | Keep exterior damage running | Remove source/status, no invented squad-wide immunity |
| Exterior pressure | Mira repair/buff when available | Focus exterior target/support fire | Shared train sustains, effects/caps apply once |

Initial five include Sera interrupt, Kyle shield and Mira repair. They do NOT
include every cleanse/control kit; base encounters must remain viable without
cleansing every slow or owning a gacha operator. All defensive cooldowns unavailable
does not guarantee no damage: ordinary damage is survivable pressure to tune,
not a mandatory perfect-counter gate. No fake action when the initial kit lacks it.

In solo, these actions never require same-frame taps across panes. A4s sabotage
warning permits sequential decisions but does not prove latency/accessibility
fairness. Measure actual reaction and cooldown availability before adopting.
Train barrier affects train only; cannot save marked operator HP. Medical device
heals according to its periodic rules, not by tapping its visual.

Coop optional role signals: danger target, need defense, target focus. Proposed
signal throttle one per sender per3s, visual duration2s; informational only, no
cooldown/buff/participation credit. Do not make signal spam a daily objective.
No new voice/chat dependency. Partner signal does not override my selected target.
Both roles see partner readiness/down state; neither can activate the other's skill.
These signals are new UI proposals, not implemented transport contract fields.

<a id="source-cb160e8c21eae28f-pause-recovery-and-transitions"></a>
#### Pause, Recovery And Transitions

System menu does not pause an online attempt. Solo pause capability remains an
explicit separate decision; aim/help are never pause even in solo. Do not market
a pause button before solo authoritative-time behavior is approved.
Wave gap keeps beneficial commands usable, offensive no-target commands reject.
Entering enemies cannot be hit early by confirming a preview on their sprite.
Down cancels preview/queue, retains started cooldown, and shows remaining recovery.
Return to foreground/reconnect clears local gestures and renders current snapshot;
never replay a tap that occurred before suspension. No duplicate summon/result.

Ultimate visual emphasis cannot lock other controls or freeze world time. A full
screen cutscene or assumed invincibility is not part of this kit. At attempt end
discard pending inputs, clear preview and await existing result settlement, not
client-calculated first-clear/reward transitions.

<a id="source-cb160e8c21eae28f-verification-cases-and-remaining-work"></a>
#### Verification Cases And Remaining Work

Design review only. No UI prototype, runtime test or deployment executed.
Required implementation tests:
- Tap vs long press, release outside, second pointer, back while aiming.
- Focus vs skill target tap; unsupported role/lane; stale or down target.
- Auto heal with fullHP, useful shield at fullHP, partial mixed effect applicability.
- Queue start/expiry/cancel race, duplicate command, delayed response, reconnect.
- Two actors casting concurrently; same actor cannot double cast.
- No cooldown on queue/invalid request; full cooldown on interrupted started cast.
- Down/return/gap/attempt end; no tap replay or aimed-target ghost projectile.
- Initial-five counterplay without rare/elite purchases; train/squad shield separation.

Next: initial-five event simulation design and balance acceptance matrix. Measure
attack/skill overlap, threat frequency, operator downs, train repair/shield stacking
and both-lane clear time. Existing119.7 basicDPS/ideal timing arithmetic is not that
simulation. Equipment/world catalogs and actual chapter2/3 mapping remain pending;
this unit does not mark all combat design or launch readiness complete.

<!-- source-body:source-cb160e8c21eae28f:end -->


<a id="source-a508f1b20654cb63"></a>
## Postbattle Retry And Partner Continuation V1

원출처: `WorkspaceRepo/docs/design/postbattle-rematch-flow-v1.md`  
원문 SHA-256: `1c5736f39a5134d5920bb14497aa95e10bf022e97627b6077fdf322902f12764`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Two affirmativeattemptbound continuationconsents/newprepnotautoready/debit; randomorigin eligibilitycannotbecomeinvitecarry; expiredcoderetired; newrun onlylegaladmission.

<!-- source-body:source-a508f1b20654cb63:start -->
<a id="source-a508f1b20654cb63-postbattle-retry-and-partner-continuation-v1"></a>
### Postbattle Retry And Partner Continuation V1

2026-09-09 / Plan / design proposal, not implementation.
Sources: combat-result-retry-flow-v1.md and online-coop-mode-policy.md.

<a id="source-a508f1b20654cb63-solo"></a>
#### Solo

After authoritative result and applicable claim are resolved: Retry, Edit Formation,
Operation Select. Retry reuses legal draft selections only; creates a new validated
preview and shows current cost before new admission. No automatic charging loop.
Preserve operator/train choices as convenience, not ownership or eligibility proof.
If a selection is unavailable, identify it rather than silently substitute another.
Mode-specific no-claim results resolve without waiting for a nonexistent claim.

New battle resets its initial state only through accepted new admission. Old run
and rewards remain immutable. A network retry uses the original command identity;
a player choosing another battle is a new intent. Do not confuse the two.

<a id="source-a508f1b20654cb63-coop"></a>
#### Coop

Offer Continue With Partner and Leave. Continuation requires two affirmative,
attempt-bound consents; host alone cannot move or charge the other account.
One consent shows Waiting For Partner, withdrawable before joint continuation
commits. No endless resend/multiple invitations, forced countdown or autoaccept.
Nonresponse may keep the waiting UI cancellable; reservation TTL is an owner
configuration requirement, no unverified new timer specified here.

When both agree, create a new preparation revision with the same pair, operation,
mode/difficulty and roles as defaults. Neither is ready automatically. Each resolves
their previous applicable reward flow; preparing a draft must not grant pending loot.
Both press ready, host starts via normal admission. Consent itself costs nothing.

Role swap allowed only here before battle, by mutual agreement; clear both ready
states and rebuild from each account's OWN train or squad assets. No asset sharing.
Departure while waiting leaves the other free to return to selection. Missing
participant is not replaced inside the completed or active attempt.

<a id="source-a508f1b20654cb63-eligibility-and-invitation-boundaries"></a>
#### Eligibility And Invitation Boundaries

- Random-origin continuation retains independent eligibility and same operation/
  difficulty/mode rules. Remaining together does not silently become relaxed invite
  carry. To use explicit invite policy, leave this continuation and create an
  explicit invite room that revalidates its own mode/difficulty conditions.
- Invite carry exceptions remain existing policy only, not new entitlement unlocks.
- Started room's old4-digit code remains retired. Direct mutual continuation uses
  authenticated membership/consent; inviting a new person requires fresh lobby/code.
- Story/story raid remains solo; no Continue With Partner shown there.
- World Raid has no difficulty. Recheck current event/boss and entry window; if
  rotation changed, return to updated preparation for explicit confirmation.
  No old-event start, timer extension, pooled score or duplicate participation grant.

<a id="source-a508f1b20654cb63-races-and-recovery"></a>
#### Races And Recovery

Consent/withdraw/leave processed against the same revision. A late old consent must
not join an unrelated new room. Duplicate continuation returns one preparation room.
If one account is already admitted elsewhere, reject stale continuation without
charge; do not interrupt its active run. No new broad account-session policy implied.
Uncertain start reconciles before another retry. Client cannot cancel a committed
battle by pressing Leave on a stale result screen.

On catalog/client-version changes, reuse existing update/content-consent flow and
revalidate afterward. Cached assets need not redownload when compatible, but must
not bypass required checks. Preserve old run versions, not mutable current inventory.

<a id="source-a508f1b20654cb63-verification-and-next"></a>
#### Verification And Next

Future tests: one-sided consent, withdrawal race, duplicate confirmation, role swap,
random-to-invite eligibility boundary, stale code, event rollover, pending claim,
lost start response and account switch. None executed. Source-policy comparison and
document readback only. Local/uncommitted, no code/deploy/video/owner assignment.
Next map room continuation/consent capabilities before implementation; UI policy
does not prove endpoint availability or authorize additional multiplayer scope.

<!-- source-body:source-a508f1b20654cb63:end -->


<a id="source-049dad14b54e01a7"></a>
## Random Match Queue And Acceptance V1

원출처: `WorkspaceRepo/docs/design/random-match-queue-flow-v1.md`  
원문 SHA-256: `40433311441b8f9f6f0b563f89001061f3f47cd2ab9ebcfaa844a770040e1a2b`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: 전투/작전 교차분야 상세 계약. 구형15wave/start package와 새 노드정책은 프로토콜 이행 대조 필요

<!-- source-body:source-049dad14b54e01a7:start -->
<a id="source-049dad14b54e01a7-random-match-queue-and-acceptance-v1"></a>
### Random Match Queue And Acceptance V1

2026-09-09 / Plan / UX refinement proposal, no implementation.
Source: online-coop-mode-policy.md, battle-entry-readiness-v1.md and
postbattle-rematch-flow-v1.md. No new modes or release-scope expansion.

<a id="source-049dad14b54e01a7-queue-flow"></a>
#### Queue Flow

Select operation/mode/difficulty where applicable -> choose train/squad/either
-> validate eligibility -> searching -> offer -> both accept -> preparation.
Offer acceptance is not ready-to-fight or charge consent. Preparation still requires
validated loadout, both ready and normal host start/admission.

Queue freezes search criteria with a revision. To change operation/difficulty/role,
cancel old queue authoritatively then start a new queue. Draft edits relevant to
role eligibility trigger revalidation, not hidden substitution. One active random
queue per account is recommended to avoid multiple simultaneous reservations;
duplicate request resumes its existing queue, no priority boost.

Retain exact same operation/difficulty/mode/rules and complementary-role filter,
independent eligibility for each player, no power filter. World matches same
event/boss/mode/rules and has no difficulty. Story remains solo.
'Either' means either currently valid owned loadout, not automatic entitlement to
an unavailable role. Assigned role is visible in offer and remains fixed unless
later consensually changed with full revalidation before battle.

<a id="source-049dad14b54e01a7-offer"></a>
#### Offer

Show operation/difficulty or world boss, assigned role, safe partner name and
Accept/Decline. No low-power warning, public damage history or automatic rejection
of an eligible newcomer. Candidate response window15s from server offer creation;
not benchmarked. Both receive the same deadline, not separate local timers.

Both accept -> server commits exactly one preparation room. One declines/expires
-> reservation releases, no charge. Recommended accepted participant returns to
search with original priority; declining/expired participant returns to selection
and explicitly restarts if desired. No rating loss or automatic ban proposed.
Do not leave the accepted player waiting on an expired offer.

Offer expiry15s differs from battle-entry readiness60s candidate and does not
consume that budget. Neither timer is a final device-validated production value.

<a id="source-049dad14b54e01a7-navigation-and-cancellation"></a>
#### Navigation And Cancellation

Searching may be minimized to a persistent status strip while browsing safe lobby
views. Other admission/match/invite actions require resolving the active queue first.
Search cancellation has Pending/Confirmed states. Client disappearance alone does
not prove server cancellation; reconnect queries current queue/offer/room identity.

Cancel racing with offer/both-accept resolves once server-side: either cancelled
or assigned preparation room, never both. If room assignment already committed,
show preparation with Leave rather than create a duplicate queue. No battle has
started solely because the offer was accepted. Never debit on a stale search UI.

Background/disconnect does not autoaccept. Offer expires normally; reconnection
must show current server state rather than replay an expired accept popup. Server
queue lease/liveness configuration needs measurement; no indefinite zombie queue.
Do not add OS notification permission or guaranteed background matching by this UX.

<a id="source-049dad14b54e01a7-long-wait-and-availability"></a>
#### Long Wait And Availability

Show measured elapsed wait only; no fabricated queue position/player count or ETA.
Keep cancel and change-role actions accessible. No automatic bot replacement,
lowered unlock gates, broadened difficulty or invite-carry exception after waiting.
Changing to either is a new explicit validated search, not hidden role reassignment.
Event rotation invalidates old-world offer/search and requires new selection.

<a id="source-049dad14b54e01a7-evidence-and-limits"></a>
#### Evidence And Limits

Future tests: same-account duplicates, cancel/accept race, asymmetric timeout,
background return, event rotation, either with one eligible role, changed loadout,
zero candidates, old revision and offer accepted twice. None run.
Policy reads and document/handoff readback only. Local/uncommitted, no runtime
code/tests/deploy/video/owner assignment. Queue/offer/room contract and lease/15s
timing measurement remain implementation prerequisites, not claimed capabilities.

<!-- source-body:source-049dad14b54e01a7:end -->


<a id="source-7a7421f71c42e376"></a>
## Online Cooperative First Match Implementation Plan

원출처: `WorkspaceRepo/docs/superpowers/plans/2026-09-05-online-coop-first-match.md`  
원문 SHA-256: `f1a3721fdbfb7cc2ec4568649a27b030091b9dc9060c3eba6fc20862ddd9af0b`  
검토·적용 경계: Current workspace guidance, routing or distinct technical handoff; not covered by this retirement batch

<!-- source-body:source-7a7421f71c42e376:start -->
<a id="source-7a7421f71c42e376-online-cooperative-first-match-implementation-plan"></a>
### Online Cooperative First Match Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development or superpowers:executing-plans when implementing this plan. Checkboxes represent work not yet executed.

**Goal:** Verify one isolated local World Raid attempt with two independently authenticated Unity clients, authoritative server commands/snapshots, and identical finalized team score without granting real rewards.

**Architecture:** Separate pure policy, WebSocket command/state protocol, local session authority, and Unity presentation. Reuse Hono/TypeScript and HTTP/auth components for authentication and non-realtime queries; do not route World Raid through story run result submission. A serialized room authority owns the timer/state and pushes validated updates to both authenticated clients.

**Tech Stack:** Existing TypeScript, Hono, Zod, Vitest, Wrangler; Unity 6000.3.22f1, C#, UnityWebRequest for HTTP, TMP, Input System. WebSockets with a room-scoped Durable Object are the selected direction; validate local/runtime support and an appropriate Unity Android-capable WebSocket transport before implementation. No installed binding or client package is assumed.

**Spec:** [Mode policy](46_COOP_DETAIL_CONTRACTS.md#source-29ac58e206039d36), [World Raid rules](44_COMBAT_DETAIL_CONTRACTS.md#source-f676f54cacce84e4).

<a id="source-7a7421f71c42e376-global-constraints"></a>
#### Global Constraints

- Runtime repository: `E:/Projects/LASTRAIL`; server root `server`; Unity root `unity-project`. Planning repository is `C:/Users/dlstj/OneDrive/문서/ChatGPT/LASTRAIL`.
- Planning does not authorize remote infrastructure, paid services, production deployment, remote seed/migration, real currency grants, APK signing, or video capture.
- Preserve unrelated dirty changes. Use an isolated execution worktree only when needed and verify Unity asset/reference paths before copying anything.
- Story is solo-only. World Raid has unlimited boss HP, no difficulty selector, one train role/one squad role, server-owned 180-second first-test deadline and phases at 120/60/0 seconds.
- No new art is necessary. Preserve allied left-region 3/2 and enemy right-region positions.
- Four operation category IDs are not verified. Do not identify them with the first four `opn_` story missions. Ordinary-operation routing cannot be marked complete before obtaining authoritative IDs.
- Existing skill/ultimate definitions and live damage producers are not established. A deterministic local fixture must be explicitly labeled test combat, not production skill implementation.
- Pure policy evidence, two WebSocket protocol clients, a Unity fixture screenshot, and two actual Unity processes are distinct milestones. Report each separately.

<a id="source-7a7421f71c42e376-existing-files-verified"></a>
#### Existing Files Verified

| Path relative to runtime repository | What it actually supplies |
| --- | --- |
| `server/apps/game-api/src/index.ts` | Hono HTTP auth, profile, story/run and reward endpoints; no observed room authority |
| `server/apps/game-api/wrangler.jsonc` | D1/local/dev/prd config; no current Durable Object room binding |
| `server/package.json` | Vitest/typecheck/lint scripts; no confirmed cooperative transport |
| `unity-project/Assets/_LASTRAIL/Scripts/Run/UnityWebRequestRunApiHttpAdapter.cs` | Base URL and HTTP endpoint transport |
| `unity-project/Assets/_LASTRAIL/Scripts/Auth/GameApiAuthenticatedHttpTransport.cs` | Bearer application; not full session orchestration |
| `unity-project/Assets/_LASTRAIL/Scripts/Run/RunApiFlowFacade.cs` | Act 1 local wave/run flow; not a cooperative authority |
| `unity-project/Assets/_LASTRAIL/Scripts/Battle/BattleSceneRoot.cs` | Local SimulationClock tick; not authoritative online World Raid time |
| `unity-project/Assets/_LASTRAIL/Scripts/Presentation/Battle/FullyAnimatedCombatHudViewState.cs` | Presentation-only state; hard-coded story boss defeat/refund labels |
| `unity-project/Assets/_LASTRAIL/Scripts/Run/RunEncounterType.cs` | Ordinary15Wave/RaidBoss only; do not reinterpret RaidBoss as World Raid |
| `unity-project/Assets/_LASTRAIL/Scripts/Presentation/Startup/StartupScreenRootBehaviour.cs` | RenderAuth and callbacks; not a confirmed runtime flow owner |
| `unity-project/Tools/run-editmode-tests.ps1` | No `-quit`, but currently includes `-nographics`; do not use unchanged for this project's graphics/license-sensitive checks |

<a id="source-7a7421f71c42e376-checkpoint-1-executable-server-policies"></a>
#### Checkpoint 1: Executable Server Policies

**Create:** `server/packages/common/src/coop/world-raid-policy.ts`, `ranking-settlement.ts`, `ticket-policy.ts` and `server/tests/unit/coop-policy.test.ts`.
**Boundary:** No routes, DB writes, exports into existing gameplay, or time obtained from client input. Tests pass explicit trusted clock values.

Proposed pure interfaces:

```ts
export type RaidPhase = 1 | 2 | 3 | "ended";
export function phaseAt(startMs: number, durationMs: number, nowMs: number): RaidPhase;
export function recoveryMs(previousDowns: number): number;
export type RankedTeam = {
  attemptId: string;
  members: readonly [string, string];
  eligible: readonly string[];
  score: bigint;
  completedAtMs: number;
};
export type RewardAssignment = { accountId: string; position: number; attemptId: string };
export function assignTeamRewards(rows: readonly RankedTeam[]): RewardAssignment[];
export function accrueTickets(stock: number, elapsedDailyBoundaries: number): number;
```

Contracts: reject negative/nonfinite clocks, invalid counts, self-pairs, duplicate members, eligible accounts outside the pair, and conflicting repeated attempt IDs. `assignTeamRewards` is scoped to one frozen board; sort by score descending, completion ascending, attempt ID ordinal. Deduplicate to the best row per unordered pair before assigning. Ineligible members neither receive nor occupy reward positions. `accrueTickets` treats stock above cap as a retained owed refund and adds no regeneration.

- [ ] Add failing Vitest assertions with actual boundary values:

```ts
expect(phaseAt(0, 180000, 59999)).toBe(1);
expect(phaseAt(0, 180000, 60000)).toBe(2);
expect(phaseAt(0, 180000, 120000)).toBe(3);
expect(phaseAt(0, 180000, 180000)).toBe("ended");
expect([0, 1, 2, 10].map(recoveryMs)).toEqual([8000, 12000, 16000, 16000]);
const row = (attemptId: string, a: string, b: string, score: bigint): RankedTeam => ({
  attemptId, members: [a, b], eligible: [a, b], score, completedAtMs: 180000
});
const assigned = assignTeamRewards([
  row("r1", "A", "B", 400n), row("r2", "A", "C", 300n),
  row("r3", "B", "C", 200n), row("r4", "D", "E", 100n)
]);
expect(assigned.map(x => [x.accountId, x.position])).toEqual([
  ["A", 1], ["B", 1], ["C", 2], ["D", 3], ["E", 3]
]);
expect(accrueTickets(9, 1)).toBe(10);
expect(accrueTickets(12, 3)).toBe(12);
```

- [ ] Run from `server`: `pnpm exec vitest run tests/unit/coop-policy.test.ts`; confirm relevant failures, then implement pure helpers only. Use integer/fixed-precision damage and `bigint` internally; never serialize bigint directly as JSON.
- [ ] Add permutation, duplicate-result, role-swap, ineligible-member, all-ineligible, daily reset/cap, and over-cap refund assertions. Two calls over the same frozen rows must produce the same assignments regardless of reward claim order.
- [ ] Verify score tiers use cumulative differences (Bronze to Silver grants 700 credits/50 parts/25 data), never all lower rows again. Better cross-mode ranking bundle pays once; selection cannot rerun cooperative promotion.
- [ ] Run focused tests, then `pnpm typecheck` and `pnpm lint`. Review/commit only these policy files and tests. Mark this as policy verification, not online gameplay.

<a id="source-7a7421f71c42e376-checkpoint-2-shared-protocol-and-fixture-contract"></a>
#### Checkpoint 2: Shared Protocol And Fixture Contract

**Create:** `server/packages/common/src/coop/protocol.ts`, `server/tests/unit/coop-protocol.test.ts`, `server/docs/api/coop-local.md` and `server/tests/fixtures/coop-local/` canonical request/response fixtures.

**Produces:** Versioned JSON parsed by both TypeScript and Unity; proposed names below are design interfaces, not existing endpoints.

```ts
type CoopRole = "train" | "squad";
type RoomState = "waiting" | "ready" | "running" | "ended";
type Command = {
  protocolVersion: 1; commandId: string; roomId: string;
  attemptId: string; expectedRevision: number;
  kind: "intercept" | "release_anchor" | "request_support";
};
type Snapshot = {
  protocolVersion: 1; roomId: string; attemptId: string;
  rulesVersion: string; sequence: number; serverNowMs: number;
  startedAtMs: number; deadlineMs: number; phase: 1 | 2 | 3 | "ended";
  assignedRole: CoopRole; members: { displayName: string; role: CoopRole }[];
  trainHp: number; operatorHp: { operatorId: string; hp: number }[];
  mechanic: null | { id: string; deadlineMs: number; state: "active" | "success" | "failure" };
  buff: null | { eventId: string; multiplierBps: number; expiresAtMs: number };
  scoreFixed: string; scoreScale: number; scoreFinal: string | null;
  connection: "connected" | "grace" | "partner_absent";
  finalReason: null | "time_limit" | "early_end" | "server_invalid";
};
```

Command identity and role come from authenticated membership, not a supplied accountId. Commands contain intentions, never damage/score/buff declarations. `scoreFixed` is a bounded decimal integer string and `scoreScale` is fixed by version; validate bounds in both clients without floating-point rounding. Member/loadout snapshot fields must include the established public assets required for rendering but no tokens/internal diagnostics.

- [ ] Add strict schema tests: preserve `"0037"`, reject numeric code `37`, unknown command kind, forged role fields, negative time, self-pair, oversized payload, unsupported version, and client score fields.
- [ ] Define route shapes for create room, code request, host approve/reject, role/loadout choice, ready, start, snapshot, command, result, and reconnect in `coop-local.md`. Use opaque room/request IDs and code generation to reject stale approvals. Start/approval/commands require idempotency keys.
- [ ] Publish paired fixtures for 120/60/0-second boundaries, active/success/failure mechanic, buff expiration, disconnected/ineligible member, and final combined score. Set `rulesVersion=coop-local-test-v1`, test-only account inventory, no real grants.
- [ ] Define exact server ordering: attempt deadline ends before incoming gameplay commands at that instant; phase boundary retires the old unfinished mechanic before new commands; expired buffs do not apply; valid pre-boundary failures resolve once. Do not leave ordering to client request arrival races.
- [ ] Both owners review names/required fields and fixture parsing before server endpoints or Unity DTO implementations begin. Commit protocol/spec/fixtures together; this is not an API deployment.

<a id="source-7a7421f71c42e376-checkpoint-3-local-serialized-room-authority"></a>
#### Checkpoint 3: Local Serialized Room Authority

**Proposed files:** `server/apps/coop-local/src/index.ts`, `room-authority.ts`, `invite-registry.ts`, `wrangler.jsonc`, and `server/tests/integration/coop-local-smoke.test.ts`.
**Modify only if needed:** local test workspace/package wiring, not live `apps/game-api` routes or deployed bindings.

This checkpoint first validates the local transport/authority approach. Prefer a local serialized room object and separate serialized code registry consistent with the existing Workers stack. Read current official/runtime documentation during implementation. If the existing runtime cannot host it, record the exact blocker before changing architecture; do not pretend module-global memory is restart-safe authority.

- [ ] Prove local authentication with two separately provisioned test accounts. Reuse existing server verifier helpers where accessible without exposing secrets. No production tokens, query-string bearer tokens, or unrestricted debug impersonation endpoint.
- [ ] Implement authenticated WebSocket command/state transport for the first actual cooperative pass. Preserve HTTP authentication/admission/query/claim roles. Retire the earlier 250ms polling baseline; periodic HTTP reads are not the required proof of live push. Review upgrade authentication, original membership, session expiry, and old-connection fencing before accepting commands.
- [ ] Specify acknowledgments by command ID, per-attempt snapshot sequence, full snapshot on initial/reconnect, bounded outbound queues, payload/rate limits, and slow-client recovery. Do not silently drop durable outcomes while coalescing replaceable state. Validate heartbeat/disconnect detection and authoritative timer catch-up independently of socket activity.
- [ ] Implement four-digit discovery with randomized allocation/retry, explicit exhaustion, host acceptance, immutable request/room/generation IDs, expiry, and membership max two. Local TTL/rate-limit constants are test settings documented in the contract, not production decisions.
- [ ] Serialize ready/asset/role/start transitions. Simulate per-account ticket debit in a local test ledger with a single start transaction; repeat starts return the original attempt, never an extra debit. Real multi-account inventory transaction design is a later production gate.
- [ ] Run one test mechanic: train `intercept`, squad `release_anchor`; accept each only in its role/window. Server-owned configured fixture actions produce deterministic damage; successful combined actions produce the configured shared buff once. Never accept a client-reported success or damage amount.
- [ ] Advance time with trusted clock; reads/ticks catch up missed deadlines once. Store revision/event IDs so two clients cannot duplicate penalties or rewards. Test controlled local authority restart and reject obsolete attempt/rules versions.
- [ ] Implement original-member reconnect, latest snapshot, 30/60-second grace, no timer/buff reset, and early-end vs voluntary-abandon distinction. Eligibility is recorded from explicit test evidence; it is not a released anti-AFK classifier.
- [ ] At deadline write one deterministic local result and dry-run reward assignments only. Assert real wallet/claim writes are zero and no production DB binding exists in this test worker.
- [ ] Run two independent WebSocket clients with HTTP auth/admission against the local endpoint: request/approve/ready/start, unsolicited server state push to both, wrong-role rejection, duplicate command, skewed client clock, disconnect/reconnect, exact final score. Capture redacted command IDs/revisions/results. Two sockets must receive the same authoritative outcome without a polling request. This passes protocol integration only.

<a id="source-7a7421f71c42e376-checkpoint-4-actual-unity-controllers"></a>
#### Checkpoint 4: Actual Unity Controllers

**Create:** `unity-project/Assets/_LASTRAIL/Scripts/Coop/{CoopProtocol,ICoopTransport,CoopWebSocketTransport,CoopSessionController,CoopLocalBootstrap}.cs`.
**Create:** `Scripts/Presentation/Coop/{CoopLobbyView,WorldRaidHudViewState,WorldRaidHudPresenter}.cs` under the same scripts root and `Tests/EditMode/CoopSessionTests.cs`.
**Modify narrowly:** existing `FullyAnimatedCombatHudRootBehaviour.cs`/prefab builder only to expose reusable view/input surfaces; preserve existing story adapter and labels. Use a separate local entry prefab/scene, not global startup build-scene replacement.

Proposed client interface:

```csharp
public interface ICoopTransport
{
    event System.Action<CoopSnapshot> SnapshotReceived;
    event System.Action<string> Disconnected;
    System.Threading.Tasks.Task ConnectAsync(
        string roomId, System.Threading.CancellationToken cancel);
    System.Threading.Tasks.Task<CoopCommandAck> SendAsync(
        CoopCommand command, System.Threading.CancellationToken cancel);
    System.Threading.Tasks.Task RequestSnapshotAsync(
        System.Threading.CancellationToken cancel);
    System.Threading.Tasks.Task DisconnectAsync();
}
// CoopSnapshot/CoopCommand mirror the reviewed protocol, not story RunStartRequest.
// CoopCommandAck contains commandId, accepted, rejectionCode (nullable), and snapshotSequence.
// The controller applies pushed snapshots on Unity's main thread, never in a socket callback.
```

- [ ] Parse the canonical fixtures from Checkpoint 2; add NUnit failures for unknown protocol, decimal score overflow, stale snapshots, previous attempt snapshot, and lost leading zeroes. Copy no private server account identifiers into public UI.
- [ ] Add controller state transitions for idle/requested/admitted/ready/running/reconnecting/ended. Use two isolated test token stores. Reuse HTTP Bearer authentication without rewriting login/OAuth/token storage; do not assume UnityWebRequest itself supplies a WebSocket client. Bind socket authority to admitted membership and marshal incoming state to Unity's main thread.
- [ ] Wire create/code request/host approve/role/readiness to server acknowledgments. Do not mark ready/start locally before acceptance. Reject role changes during running on both UI and server.
- [ ] Replace fixture-only icon actions with local test command callbacks owned by the controller. Authority responses update HUD; local click/VFX cannot resolve a mechanic, alter HP, or add score.
- [ ] Add World Raid view state with remaining time, phase, score, HP, mechanic/buff deadlines, and role. Do not cast it to story `RaidBoss` or show boss-defeat/refund labels. Do not run `BattleSceneRoot` local simulation as online authority.
- [ ] Test snapshot revision ordering, cancellation on disable, lost response/retry with same commandId, phase transitions without repeated cues, buff expiry during reconnect, and identical terminal result parsing.
- [ ] Render 1080x1920 and 360x640 with actual authority snapshots; label screenshots by their real input source. Preserve formation and battle background. No video without a separate user request.
- [ ] Run focused tests with graphics batchmode, no `-nographics` and no `-quit` when using `-runTests`. Require fresh XML and failed count zero, not only process exit 0. Existing wrapper currently needs a graphics option or a direct command.

Example direct verification command (execution phase only):

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe' -batchmode -projectPath 'E:\Projects\LASTRAIL\unity-project' -runTests -testPlatform EditMode -testFilter 'LastRail.Tests.EditMode.CoopSessionTests' -testResults 'E:\Projects\LASTRAIL\unity-project\TestResults-CoopSession.xml' -logFile 'E:\Projects\LASTRAIL\unity-project\Unity-CoopSession.log'
```

<a id="source-7a7421f71c42e376-checkpoint-5-one-real-two-client-attempt"></a>
#### Checkpoint 5: One Real Two-Client Attempt

**Evidence:** `unity-project/QA/Act1Launch/Reports/coop-local-first-match.json` and optional user-requested screenshots; no video. A detailed log must omit tokens and raw auth headers.

- [ ] Run two independent Unity clients with live WebSocket connections against the same local authority and distinct test accounts. Do not open two editors on one Unity project checkout; use an isolated test copy or an existing safe client build path. If neither is available, report this checkpoint unexecuted instead of relabeling protocol clients.
- [ ] Client A creates a room, B requests by code, A approves, roles select their own fixture assets, both ready, server starts exactly one attempt.
- [ ] Verify both observe the same attempt/config/deadline/phase/score. Send a wrong-role command and confirm authoritative rejection without side effects.
- [ ] Complete one cooperative mechanic, then fail one. Verify one shared buff, one declared target penalty, down/recovery behavior, phase transition, and no post-deadline damage.
- [ ] Disconnect/reconnect one original account, restore current snapshot, and verify no second cost, phase rewind, role switch, or replayed buff. Separately exercise voluntary abandonment and permitted remaining-player early end.
- [ ] Compare finalized score/result IDs on both clients. Record local ledger debits, dry-run assignments, real reward writes=0, elapsed time, and whether the input was deterministic test combat.
- [ ] Review evidence against the spec before broadening. This proves one local networked loop, not phone-device readiness, production combat balance, public random matching, or full Act 1 integration.

<a id="source-7a7421f71c42e376-follow-on-work-coverage"></a>
#### Follow-On Work Coverage

| Requirement | Gate before broad implementation |
| --- | --- |
| Four operation types | Obtain authoritative category IDs/definitions; add invitation/random admission matrix tests without substituting story missions |
| Solo mechanics | Add solo adapter for the same boss schedule; prove sequential inputs and no unavailable mandatory support |
| Public random queue | Exact operation/difficulty or world event/boss key, complementary roles, independent eligibility, no power matching; queue concurrency/cancel tests |
| Real inventory/tickets | Local schema/transaction plan, atomic two-account charge/refund and trusted ownership; no remote migration without approval |
| Real skill/damage/boost engine | Server-owned catalog and battle producer, including cross-role buff composition; fixture logic cannot substitute |
| Actual ranking/rewards | Persist frozen score/eligibility/loadout, execute promotion/idempotent grants, review post-settlement invalidation and privacy |
| Quest integration | One shared solo/cooperative progress track, weekly participation only, no duplicate direct reward; test all role-only paths |
| Phone latency/device QA | Two separate phones or approved emulators, measured network loss/clock skew/readability; do not infer from Editor |
| Production release | Explicit infrastructure/secret/deploy approval and release branch/tag procedure |

<a id="source-7a7421f71c42e376-review-outcome-and-current-status"></a>
#### Review Outcome And Current Status

All checkboxes are initially unchecked. This plan is a staged implementation order with concrete first-checkpoint interfaces/tests; WebSocket transport integration, serialized authority validation, and full gameplay definitions are engineering gates, not existing capabilities. Do not execute later checkpoints in parallel before their contract dependencies exist.

The planning pass reads source and creates documents only. It does not run a server, start Unity, record video, change assets, add packages, create live services, or grant rewards.

<!-- source-body:source-7a7421f71c42e376:end -->


<a id="source-2a8c21536363ec8b"></a>
## 2인 전투 — 열차 / 요원 역할별 화면 기획

원출처: `docs/design/COMBAT-COOP-ROLE-UI-20260914.md`  
원문 SHA-256: `34b3baa0acf531a7e19e7bed18168564c637ce48deab05d1c7c348f14b5eb007`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Coop role UI proposal; cards=routes/type authorization superseded; per-role actual controls/serverauth/reconnect preserved;20s etc unapproved.

<!-- source-body:source-2a8c21536363ec8b:start -->
<a id="source-2a8c21536363ec8b-2인-전투--열차--요원-역할별-화면-기획"></a>
### 2인 전투 — 열차 / 요원 역할별 화면 기획

2026-09-14 / Plan_5 / 검토용 초안. 사용자 확정 사항은 ‘2차 최종시안은 1인용’과 ‘2인은 열차 / 요원 담당 분리’다. 아래 신규 배치·동작은 제안이며 구현·시안 승인 기록이 아니다.

<a id="source-2a8c21536363ec8b-기존-결정과-최신-기준"></a>
#### 기존 결정과 최신 기준

- [온라인 협동 정책](46_COOP_DETAIL_CONTRACTS.md#source-29ac58e206039d36): 각자 휴대폰·계정으로 접속한다. 열차 담당의 열차·부품·무장과 요원 담당의 요원 5명·장비를 사용한다. 전투 중 역할/자산 교환이나 대체 참가자는 없다.
- 양쪽 모두 상단 외부 / 하단 객실 방향을 유지한다. 내부 아군 좌측 3/2 배치, 적 우측. 외부 열차 우향·후방 추격과 편성별 객차 연결을 유지한다. 역할 때문에 화면을 반전하지 않는다.
- [2차 최종시안](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-ea1df21c2a944423c49a)의 어두운 물리 콘솔, 관측 영상, 한글 이름, B형 직사각 궁극기, 청록 지원 아이콘을 공통 미술 기준으로 사용한다. ‘외부 관측/객실 관측’ 표제는 되살리지 않는다.
- 기존 협동 가능 콘텐츠만 대상이며 스토리 협동 확대나 모든 콘텐츠에 분기 도입을 의미하지 않는다.
- [이전 입력 초안](46_COOP_DETAIL_CONTRACTS.md#source-cb160e8c21eae28f)의 ‘선택 궁극기 하나’는 최신 카드별 궁극기 표현과 충돌한다. 이번 제안은 최신 카드별 궁극기 5개 배열을 기준으로 한다. 과거 초안의 별도 액티브 버튼을 추가하지 않으며 스킬 대상/효과는 기존 카탈로그를 따른다.

<a id="source-2a8c21536363ec8b-배치-대안"></a>
#### 배치 대안

| 대안 | 장점 | 약점 | 판단 |
| --- | --- | --- | --- |
| 1인 화면 그대로, 상대 버튼만 잠금 | 학습과 재사용이 쉬움 | 조작하지 못하는 큰 버튼이 남음 | 비교안 |
| 같은 상하 구조, 담당 영상·조작부 강조 | 상황 공유와 역할 몰입을 함께 유지 | 역할별 화면 검증 필요 | 추천 |
| 담당 영상만 전체 화면, 상대 장면 탭 전환 | 담당 장면이 가장 큼 | 상대 위험을 놓치고 화면 전환이 잦음 | 기본안에서 제외 |

추천은 같은 지휘 시스템의 서로 다른 담당 콘솔이다. 상대 영상도 작은 관측창으로 계속 보인다. 전투 중 자동 확대나 패널 출몰로 버튼 위치가 바뀌지 않는다.

<a id="source-2a8c21536363ec8b-역할별-화면--위에서-아래"></a>
#### 역할별 화면 — 위에서 아래

영상 비율은 영상 영역 안의 시안 출발값이다. 전체 화면의 확정 치수가 아니며 안전 영역·읽기 크기·터치 영역을 먼저 확보한다.

| 영역 | 열차 담당 | 요원 담당 |
| --- | --- | --- |
| 상단 | 타이틀, 웨이브/진행, 열차 내구도, 내 역할, 연결 상태 | 같은 핵심 진행 정보와 내 역할 |
| 외부 영상 | 크게: 영상 영역 약 60%. 추격·포격·선로와 외부 대상 선택 | 작게: 약 35%. 열차 위험·이동 확인 |
| 영상 경계 | 동료 이름·요원 담당 상태·짧은 신호 | 동료 이름·열차 담당 상태·짧은 신호 |
| 객실 영상 | 작게: 약 40%. 요원 전황 확인 | 크게: 약 65%. 내부 대상 선택과 전투 판독 |
| 요원 상태 | 작은 초상화 5개와 HP/다운/궁극기 준비 표시, 읽기 전용 | 한글 이름·HP·충전을 갖춘 카드 5개와 B형 궁극기 버튼 |
| 최하단 | 좌 가로 분기 레버 / 중앙 세로 스로틀 / 우 지원 3개 | 낮은 열차 상태 패널: 속력·경로·지원 준비 상태, 읽기 전용 |

열차 담당에게 요원 궁극기를 큰 회색 잠금 버튼으로 남기지 않고 작은 상태등으로 바꾼다. 요원 담당에게 조작 불가능한 큰 레버를 남기지 않고 확보한 높이를 객실과 카드에 쓴다. 양쪽 모두 검은 금속 바탕을 유지하며 준비/위험/입력 부위에만 빛을 집중한다.

<a id="source-2a8c21536363ec8b-조작-소유권"></a>
#### 조작 소유권

| 조작 | 열차 담당 | 요원 담당 |
| --- | --- | --- |
| 분기·증강 경로 | 레버 또는 증강 카드 탭으로 선택·확정 | 설명 열람·추천 신호 |
| 물리 운행 속도 | 세로 스로틀 조절 | 현재 속력 확인 |
| 방어막 / 지원 사격 / 긴급 수리 | 직접 실행 | 상태 확인·도움 요청 |
| 요원 궁극기 | 준비 상태 확인·도움 요청 | 카드별 실행 |
| 집중 대상 | 외부의 유효 대상 | 내부의 유효 대상 |
| 협력 신호 | 위험 / 방어 요청 / 집중 대상 | 동일 |

카탈로그가 허용한 요원 스킬의 외부 대상·열차 지원 효과는 유지한다. 상대의 신호는 자동 시전·대상 변경·레버 확정이 아니다. 역할 권한은 서버에서 검증해야 하며 UI 잠금만으로 완료 처리하지 않는다.

AUTO는 요원 자동 스킬 운용이라면 요원 담당만 조절하고 열차 담당에는 상태만 표시하는 안을 추천한다. 작은 오른쪽 고정 원을 유지한다. 기존 AUTO가 열차 지원까지 포함하는지는 구현 전 계약 점검 후 역할별 범위를 분리해야 한다. AUTO가 분기·스로틀을 자동 확정하지 않는다.

<a id="source-2a8c21536363ec8b-분기-협력-흐름"></a>
#### 분기 협력 흐름

1. 분기 기믹이 있는 전투에서 서버가 선택 단계 진입을 양쪽에 알린다. 양쪽 전투에 같은 극저속이 적용되고 UI는 정상 반응한다.
2. 열차 담당은 기존 좌측 콘솔 레버 또는 화면의 증강 카드로 선택한다. 요원 담당은 하단 고정 상태 영역에서 같은 경로·증강 설명을 읽고 추천 표시를 보낼 수 있다.
3. 열차 담당이 추천을 참고해 레버 놓기 또는 카드 탭 완료로 최종 확정한다. 매번 양쪽 동의 버튼을 요구하지 않는 안을 추천한다. 확정 권한은 호스트 여부가 아닌 열차 역할을 따른다.
4. 서버가 한 번 확정하고 공유 전투에 증강을 한 번 적용한다. 요원 효과는 요원 담당 편성에, 열차 효과는 열차 담당 편성에 적용한다. 자산 복제·중복 부여는 없다.
5. 양쪽 선로·열차·객차·지도는 같은 경로로 전환하고 이전 운행 상태로 함께 복귀한다. 극저속 때문에 스로틀 손잡이가 움직이지 않는다. 늦은 추천/이전 분기 입력은 다음 분기로 이월하지 않는다.

선택 중 일반 전투 입력은 기존 전투 규칙을 따르되, 미전송 탭을 복귀 순간에 몰아서 시전하지 않는다.

<a id="source-2a8c21536363ec8b-분기-카드-직접-터치--1인2인-공통"></a>
#### 분기 카드 직접 터치 — 1인·2인 공통

2026-09-14 사용자 요청으로 카드 직접 터치 선택을 추가한다. 카드 터치 지원은 확정 요구사항이며 아래 세부 제스처·표시는 구현용 설계다. 이번 변경은 기획 반영이고 런타임 구현 완료가 아니다.

- 왼쪽 증강 카드 = 왼쪽 선로, 오른쪽 카드 = 오른쪽 선로. 카드에 방향 표시와 증강 이름·효과를 함께 보여준다. 카드 전체를 터치 영역으로 사용한다.
- 1인 플레이와 2인 열차 담당은 카드 한 번 탭으로 해당 선로를 확정한다. 누르는 동안 카드·경로를 미리 강조하고, 같은 카드 안에서 손을 떼면 확정한다. 별도의 확인 버튼이나 두 번째 탭을 요구하지 않는다.
- 카드 밖으로 끌어 손을 떼거나 포커스를 잃으면 미확정 입력을 취소한다. 카드 아래 전투 장면으로 터치가 통과해 집중 사격이나 스킬이 실행되지 않는다.
- 레버 미리보기와 카드 강조는 같은 선택 상태를 사용한다. 카드로 확정하면 레버 손잡이도 해당 방향으로 이동하고, 레버로 확정하면 같은 카드가 선택 표시된다. 화면상 레버 이동이 다시 선택 명령을 발생시키지 않는다.
- 하나의 제스처가 입력을 점유한다. 레버 드래그 중 두 번째 손가락 카드 탭은 중복 확정하지 않는다. 확정 요청 중에는 카드와 레버를 함께 잠그고, 승인된 선택 한 건만 경로 전환·증강 적용·시간 복귀를 실행한다. 온라인에서는 서버 승인 전에는 확정 완료로 표시하지 않는다.
- 2인 요원 담당의 카드 터치는 선로 확정이 아닌 추천 표시만 보낸다는 제안을 유지한다. 화면에도 ‘터치하여 추천’, 열차 담당에는 ‘터치하여 선로 선택’으로 의미를 구분한다. 추천은 열차 담당 레버를 움직이거나 선택을 바꾸지 않는다.
- 재접속·초기 표시·저장 복원·연출용 레버 이동은 입력이 아니다. 분기 ID와 현재 선택 단계가 맞지 않는 늦은 탭은 무시한다.
- 추가 검증 항목: 좌우 카드와 레버 결과 동일, 카드 밖 해제 취소, 터치 관통 없음, 카드 연타/멀티터치/레버와 경합 시 한 번만 적용, 온라인 거부·지연·재접속, 요원 추천으로 확정 불가. 아직 실행하지 않은 검증 항목이다.

<a id="source-2a8c21536363ec8b-온라인-시간연결"></a>
#### 온라인 시간·연결

- 기존 [연결 복구 기획](44_COMBAT_DETAIL_CONTRACTS.md#source-9e22320e57141e91)에 따라 개인 메뉴·백그라운드·연결 끊김으로 온라인 전투를 정지하지 않는다. 일시정지 자리는 메뉴로 표시한다. 분기 극저속은 서버가 발행하는 공유 기믹이다.
- 온라인 시뮬레이션 배속은 기본 1배 고정을 제안한다. 물리 스로틀은 계속 조절한다. 개인 1.5/2배 조작은 상대 시간에 영향을 주므로 그대로 이식하지 않는다.
- 연결 끊김 시 준비 상태를 확정 표시하지 않고 미완료 제스처를 취소한다. 서버 승인 효과와 기본 공격은 기존 규칙대로 진행한다. 상대에게 권한을 넘기거나 수동 궁극기·지원을 자동 시전하지 않는다.
- **추가 결정 필요: 분기 대기 중 열차 담당 이탈.** 1인용 무기한 선택 대기를 그대로 적용하면 상대가 갇힌다. 온라인 전용 유한 선택 기한과 사전에 표시한 기본 경로를 두고 서버가 만료 시 확정하는 안을 추천한다. 정확한 기한·기본 경로 규칙은 미정이며 승인된 규칙이 아니다. 요원 담당 이탈만으로 정상 확정을 막지는 않는 안이다.
- 공식 제한 시간이 있는 콘텐츠에 분기를 넣을 경우 점수용 시간과 극저속 관계를 별도 확정해야 한다. 이 초안으로 기존 시간 제한을 연장하지 않는다.
- 재접속은 현재 역할·분기 ID/단계·확정 경로·속도·스킬 상태를 서버 스냅샷으로 복구한다. 과거 탭과 신호를 재생하지 않는다.

<a id="source-2a8c21536363ec8b-협력-예시와-후속-검증"></a>
#### 협력 예시와 후속 검증

언덕 포격 진지가 나오면 열차 담당은 외부 표적 지정과 속도 조절로 파괴/노출 시간 단축을 판단한다. 요원 담당은 객실 적을 처리하며 카탈로그상 가능한 지원 궁극기를 사용한다. 방어 요청은 동료에게 전달하는 정보다. 파괴하지 못한 진지는 공유 이동 거리에 따라 양쪽에서 동일하게 지나간다.

다음 시안은 같은 순간의 열차 담당/요원 담당 화면 한 쌍으로 제작한다. 일반 전투, 분기, 외부 포격과 내부 위험 동시 발생, 요원 다운, 동료 연결 끊김을 비교한다. 1인용 원본은 보존한다.

구현 후에는 두 실제 클라이언트의 권한 위조 거부, 역할별 포커스, 공유 시간 복귀, 추천/확정 경합, 중복 증강 방지, 만료/재접속 경합, 오래된 준비 표시, 좁은 세로 화면, 긴 한글 이름, 메뉴 중 전투 지속을 검증해야 한다. 이번에 실행한 테스트가 아니다.

이번 수행은 기존 정책 대조와 문서 작성·재읽기다. 소스/아트/네트워크 변경, 빌드, 배포, 실기기 검증 없음. 이 초안을 2인용 최종시안으로 명명하지 않는다.

<!-- source-body:source-2a8c21536363ec8b:end -->


<a id="source-cda19b06fbf79722"></a>
## 역할별 증강 선택 상세 제안

원출처: `docs/design/COOP-AUGMENT-SELECTION-DETAIL-20260914.md`  
원문 SHA-256: `b44b91d471f8d3ef71b23d6fb94d59d359eb254e667dee8cb2dc0753e86ade0e`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Type-specific/double grants/lever policy superseded; coop deadlines20 proposal not default; same authority result/expiry concurrency preserved.

<!-- source-body:source-cda19b06fbf79722:start -->
<a id="source-cda19b06fbf79722-역할별-증강-선택-상세-제안"></a>
### 역할별 증강 선택 상세 제안

2026-09-14 / Plan_5. 사용자 요청에 따른 세부 기획. 사용자 후속 승인: 증강 지급 시 열차/요원 종류를 랜덤으로 결정한다. 온라인 기한·기본 후보·화면 배치 등 나머지 제안은 별도 승인 전이며 구현 완료가 아니다.

<a id="source-cda19b06fbf79722-최신-결정-종류-랜덤-등장"></a>
#### 최신 결정: 종류 랜덤 등장

- 기존 증강 지급 조건을 충족할 때마다 열차 증강 또는 요원 증강 중 한 종류가 등장한다. 같은 지급 한 번에 양쪽 선택권을 동시에 추가하지 않는다.
- 종류를 먼저 정하고, 해당 종류의 유효한 후보를 제시한다. 플레이어는 후보 중 효과를 선택하며 종류를 바꾸기 위해 취소·재접속·재요청할 수 없다.
- 1인에서는 등장한 종류의 단계만 처리한다. 2인에서는 열차 종류는 열차 담당, 요원 종류는 요원 담당이 확정한다. 상대는 설명 열람·추천만 한다.
- 열차 종류는 기존 선로 카드/레버와 연결한다. 요원 종류는 요원 카드 선택으로 처리하며, 요원 증강을 이유로 선로를 임의 변경하거나 별도 분기를 만들지 않는다. 기존 실제 선로 진행은 유지한다.
- 랜덤 종류와 후보 결과는 지급 구간에 고정·보존한다. 서버가 결과를 결정하고 재접속·재전송에는 같은 결과를 돌려준다. 두 클라이언트가 따로 추첨하지 않는다.
- 미선택 역할의 동의는 기다리지 않는다. 현재 선택 담당자의 확정 후 효과·필요한 경로 연출·공유 시간 복귀를 한 번 처리한다. 미선택 역할의 일반 입력 허용 범위는 기존 선택 단계 규칙을 따른다.
- 기존 지급 횟수·총량은 유지하며, 낙석 우회 같은 일반 분기에서는 추가 추첨하지 않는다. 무효 후보를 피하기 위한 유효성 검증과 실패 처리는 기존 계약을 유지한다.
- 2026-09-15 사용자 승인: 기본 열차/요원 50:50, 동일 종류 최대 2연속 뒤 반대 종류 보정. 1인·2인 공통이며 전투별 이력과 재접속 결과를 보존한다. [분포 정책](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-eccce2e1f7a560dd41f3)을 따른다.
- 아래 방식 비교와 동시/순차 두 선택 흐름은 이전 제안의 이력이다. 최신 실행 기획은 이 절의 단일 랜덤 종류 선택이 우선한다.

<a id="source-cda19b06fbf79722-기존-승인과-적용-범위"></a>
#### 기존 승인과 적용 범위

- 각자 계정/기기로 열차 담당과 요원 담당을 분담한다. 전투 중 역할·자산 교환과 대체 참가자는 없다. [기존 협동 정책](46_COOP_DETAIL_CONTRACTS.md#source-29ac58e206039d36)을 따른다.
- 최신 1인 화면은 [3차 최종시안](51_UI_ART_DETAIL_CONTRACTS.md#source-61f16f3947c254bb). 2인 화면은 [역할 UI 초안](46_COOP_DETAIL_CONTRACTS.md#source-2a8c21536363ec8b)이며 최종 승인본이 아니다.
- 가로 레버와 화면 카드 터치는 같은 선로 선택으로 연결한다. 일반 우회 분기는 전투를 계속하며 추가 증강을 주지 않는다. 증강 분기는 기존 극저속·거리 유지 계약을 따른다.
- 이번 문서는 지급 횟수/희귀도/기존 증강 예산을 늘리지 않는다. 모드별 현재 지급표를 먼저 확인해야 하며, 기존 선택 한 번을 열차와 요원 각각 한 번으로 자동 두 배 지급하지 않는다.

<a id="source-cda19b06fbf79722-이전-방식-비교--랜덤-등장-결정-이전-이력"></a>
#### 이전 방식 비교 — 랜덤 등장 결정 이전 이력

| 방식 | 장점 | 비용 |
|---|---|---|
| 열차 담당이 공통 증강 하나 선택 | 가장 단순 | 요원 담당의 성장 결정권이 약함 |
| 열차·요원 지급 시점을 항상 분리 | 각 역할 성장 연출이 명확 | 감속/대기 구간 증가, 단독 플레이 흐름 반복 |
| 종류·권한 분리, 같은 지급 구간은 함께 선택 | 각자 결정권과 전투 흐름 유지 | 두 선택의 완료·연결 복구 상태 관리 필요 |

이전에는 세 번째 방식을 추천했으나, 후속 사용자 결정으로 단일 랜덤 종류 선택으로 대체했다. 두 종류 동시 지급을 기본 기능으로 구현하지 않는다.

<a id="source-cda19b06fbf79722-선택권과-귀속"></a>
#### 선택권과 귀속

- 열차 선택권: 선로별 열차 증강 카드. 열차 담당이 레버/카드로 확정한다. 다른 역할은 추천만 가능하다.
- 요원 선택권: 객실/요원 영역의 증강 카드. 요원 담당이 확정하며 선로를 바꾸지 않는다. 요원 증강을 위해 두 번째 선로 선택을 요구하지 않는다.
- 카드 후보 수는 현행 카탈로그/지급표에서 가져온다. 열차 좌우 경로는 각각 하나의 선택 가능한 묶음에 연결한다. 후보 수가 맞지 않는 기존 콘텐츠는 별도 변환 승인 전 적용하지 않는다.
- 요원 증강은 카탈로그에 기록된 대상 범위(편성 전체/특정 요원 등)를 명시한다. 대상 선택형이면 카드 선택 후 유효한 요원을 지정해야 최종 확정된다. 확정 전 단계는 뒤로 가기를 허용한다.
- 선택권은 해당 전투·역할·지급 구간에 귀속한다. 계정 영구 재화/장비 지급으로 바꾸지 않는다. 같은 선택을 두 플레이어에게 중복 지급하지 않는다.
- 호스트는 확정 권한의 기준이 아니다. 상대 추천은 자신의 선택·레버·대상을 자동 변경하지 않는다.

<a id="source-cda19b06fbf79722-이전-1인-두-종류-순차-흐름--채택하지-않음"></a>
#### 이전 1인 두 종류 순차 흐름 — 채택하지 않음

1. 기존 지급 조건 충족 후 안전한 증강 구간에 진입한다. 일반 우회 경고/잠금 구간과 겹치지 않는다.
2. 선택권이 둘 다 있으면 열차 증강 → 요원 증강 순서로 처리한다. 현재 단계와 남은 단계를 작게 표시한다.
3. 열차 확정 후 선로 선택은 잠그되 전투/거리 복귀는 모든 선택이 끝난 뒤 한 번만 한다. 요원 선택 중 레버를 다시 움직여 재선택하지 않는다.
4. 기존 1인 무제한 대기를 유지한다. 극저속에서도 UI 입력·설명 스크롤은 정상 속도로 반응한다.
5. 선택권이 한 종류뿐이면 그 단계만 연다. 빈 단계를 표시하거나 두 번째 선택권을 생성하지 않는다.

<a id="source-cda19b06fbf79722-이전-2인-동시-흐름과-화면--동시-선택은-채택하지-않음"></a>
#### 이전 2인 동시 흐름과 화면 — 동시 선택은 채택하지 않음

1. 서버가 동일 구간 ID와 각 역할의 유효한 후보/선택권, 공유 시간 상태를 보낸다.
2. 두 선택권이 있으면 동시에 선택한다. 열차 담당은 외부 영상 근처의 선로 카드와 하단 레버, 요원 담당은 요원 카드 위 증강 판을 사용한다. 상대 화면을 덮는 공통 전체 팝업을 강제하지 않는다.
3. 먼저 확정한 플레이어는 선택 결과와 '동료 선택 중'을 읽는다. 상대 효과를 설명으로 볼 수 있지만 대신 확정할 수 없다.
4. 최종 확정 전에는 미리보기, 서버 접수 중에는 입력 잠금, 승인 후에는 결과로 구분한다. 확정된 선택은 취소/교체하지 않는다.
5. 모든 필요한 선택이 확정되면 서버가 효과를 적용하고, 선로 전환·객차 이동·맵 연결 연출을 시작하며 공유 운행 상태를 한 번 복귀시킨다. 요원 단독 선택 구간에는 가짜 분기 연출을 만들지 않는다.
6. 화면 상단/하단 조작 위치는 선택 전후에 갑자기 바뀌지 않는다. 관측창 방향도 반전하지 않는다. 상대 역할은 작은 준비 상태로 확인한다.

<a id="source-cda19b06fbf79722-시간과-대기-제안"></a>
#### 시간과 대기 제안

- 온라인은 무기한 대기를 피하기 위해 공통 선택 제한시간을 둔다. 출발 제안값은 실제 시간 20초이며 운영 확정값이 아니다. 극저속 배율로 제한시간을 늘리지 않는다.
- 서버가 선택 구간을 열 때 기한을 정한다. 화면 로딩/재접속/추천/먼저 확정으로 기한을 다시 시작하지 않는다.
- 만료 시 아직 확정하지 않은 역할만 사전에 화면에 표시한 기본 후보로 확정한다. 랜덤으로 뒤집거나 상대에게 권한을 넘기지 않는다.
- 기본 후보는 현재 편성에 유효한 후보 중 지급 데이터에 지정된 우선순위를 따른다. 특정 요원 대상형은 기본 대상까지 사전 고정·표시해야 한다. 기본 후보/대상이 없는 잘못된 지급 구성은 출시 전 데이터 검증에서 차단한다.
- 서버 오류 때문에 후보가 없는 경우 임의 보상을 발명하지 않는다. 실패 복구 절차와 기존 전투 종료/재접속 계약을 적용하며 미해결 상태를 성공으로 처리하지 않는다.
- 선택 중 스로틀 입력은 잠그고 기존 운행 설정 위치를 유지한다. 극저속은 별도 시간 상태로 표시한다. 감속을 저속 운행 보너스나 지원 충전 농사에 이용하지 못하게, 증강 선택 구간은 속도별 보너스 누적을 중지하는 안을 제안한다.
- 제한시간 콘텐츠는 점수 시간 포함/제외와 최대 선택 구간 수를 별도 승인하기 전 이 방식을 도입하지 않는다. 이 초안은 기존 제한시간을 연장하지 않는다.

<a id="source-cda19b06fbf79722-입력연결충돌"></a>
#### 입력·연결·충돌

- 레버/카드 입력은 동일한 선택 명령으로 합류한다. 한 제스처만 점유하고 카드 밖 해제는 취소, UI 아래 전투로 터치가 통과하지 않는다.
- 확정 요청은 구간·역할·후보·대상·요청 식별자를 포함한다. 서버가 역할 권한, 후보 유효성, 현재 단계/기한을 검증한다.
- 재전송은 같은 결과를 반환한다. 만료 처리와 사용자 확정이 경합하면 서버가 먼저 확정한 유효 결과 한 건만 유지한다. 클라이언트 전송 시각으로 기한을 소급하지 않는다.
- 연결이 끊겨도 선택 기한은 진행한다. 재접속 시 남은 기한/확정 결과를 복원하고 이전 미확정 탭·드래그는 재생하지 않는다. 역할 대체와 자동 수동 궁극기 시전은 없다.
- 효과 지급과 선택권 소비는 한 번만 확정한다. 확정 결과를 두 화면에 여러 번 전달해도 효과·선로 전환·속도 복귀는 중복 실행되지 않는다.
- 전투 패배/종료가 먼저 확정되면 선택 화면을 닫는다. 늦은 선택 응답으로 전투를 재개하거나 보상을 추가하지 않는다.

<a id="source-cda19b06fbf79722-구현-경계와-검수"></a>
#### 구현 경계와 검수

- Model: 선택권/확정 상태/효과 귀속. Controller: 권한별 입력·기한/연결 상태 조정. View: 표시와 콜백 전달. 서버는 권위 있는 확정·중복 방지·지급을 소유한다.
- 카드 판·프레임·효과 아이콘·설명·대상 표시·남은 시간·추천 표시는 독립 요소다. 숫자/문구/게이지를 이미지에 굽지 않는다. Bind/Unbind와 화면 종료 후 늦은 응답 차단을 적용한다.
- 확인 사례: 1인 한 종류/두 종류, 2인 동시에/한쪽 먼저, 요원 대상 지정 취소, 카드-레버 경합, 잘못된 역할, 한쪽 이탈, 만료와 확정 경합, 응답 유실 재전송, 재접속 후 이미 지급됨, 선택 중 패배, 일반 우회와 증강 미중첩.
- 미확정: 온라인 기한 20초, 기본 후보 우선순위, 속도 보너스 정지 세부 범위, 시간제 콘텐츠 도입 여부, 2인 최종 화면 비율. 종류 확률·연속 보정은 2026-09-15 승인됐다. 검수는 단일 랜덤 종류·해당 역할만 확정·재접속 추첨 결과 보존을 우선하며, 위 이전 두 종류 동시/순차 흐름은 현재 검수 대상에서 제외한다. 실제 테스트/운영 반영은 수행하지 않았다.

최신정책주의: 본문과거종류선추첨/레버선택은통합동일가중치/카드터치정책으로대체됨. [2인 통합후보 후속설계](46_COOP_DETAIL_CONTRACTS.md#source-991422e2420e54e4)의교대선택·리롤권한은검증후보로구분한다.

<!-- source-body:source-cda19b06fbf79722:end -->


<a id="source-f02428cfe7a062db"></a>
## 협동 보상 소유권 조사와 광고 룰렛 구현 범위

원출처: `docs/design/COOP-REWARD-OWNERSHIP-IMPLEMENTATION-SCOPE-20260916.md`  
원문 SHA-256: `a118567f313ce415e877d3c50a4c017f2a00b3e18b6eb7a7f7c7c1e1fc1a973f`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Account economic run at co-op admission; no fake second START/fuel; durable perparticipant settlement,partial fanout retry, independent ad choices; Admin0009 retained.

<!-- source-body:source-f02428cfe7a062db:start -->
<a id="source-f02428cfe7a062db-협동-보상-소유권-조사와-광고-룰렛-구현-범위"></a>
### 협동 보상 소유권 조사와 광고 룰렛 구현 범위

2026-09-16 사용자 후속 진행. 현재 서버 체크아웃의 apps/packages 소스와 DB migrations, 관련 계약 및 tests 참조를 정적으로 조사했다. 실제 협동 세션·DB·광고 지급은 실행하지 않았다. 아래 구조는 조사에 따른 구현 기준안이며 기존 구현 사실과 구분한다.

<a id="source-f02428cfe7a062db-1-확인-결과"></a>
#### 1. 확인 결과

기준: Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server.

|근거|확인한 내용|영향|
|0005_run.sql|runs는 account_id 하나를 소유|공용 전투 ID를 그대로 두 계정의 지급 소유자로 사용할 수 없음|
|0012_run_reward_previews.sql|보상 미리보기 UNIQUE(run_id)|같은 run에 두 계정의 미리보기를 추가하는 것만으로 해결되지 않음|
|0002_economy.sql|reward_transactions도 UNIQUE(run_id)|미리보기 제약만 바꿔도 두 번째 지급에서 충돌|
|realtime-coop.ts|room에 run_id 하나, participants에 계정별 회원 정보|공용 연결/참여와 계정별 경제 정산은 다른 경계|
|docs/api/realtime-coop.md 및 해당 소스|서버 골격과 입력 접수 경로, 최종 협동 결과의 일반 보상 연결은 미완성|참가자 보상 기록이 이미 만들어진다고 가정할 수 없음|
|0009_ads.sql|provider 거래 필수, UNIQUE(run_id,provider)|검증 전 광고 시도·공용 run 계정별 시도·여러 재시도 저장에 부적합|
|apps/admin-api/src/index.ts|0009 테이블에서 VERIFIED/GRANTED 집계와 일별 통계를 조회|이 테이블을 미사용으로 삭제하면 안 됨|

apps/packages의 이름 참조 검색에서는 0009 광고 거래에 대한 game-api 쓰기 경로를 찾지 못했고 Admin 조회·migration/index 참조를 확인했다. 외부 운영 도구나 모든 역사 자료까지 쓰기 부재를 증명한 것은 아니다. tests/unit/admin-api-launch-read-models.test.ts에도 해당 통계 출처를 기대하는 검증이 있다.

보급 광고의 현행0036 테이블은 별도다. Admin의 위0009 집계만으로 보급/신규 전투 광고 전체 통계를 포괄한다고 보지 않는다. 사용자에게 보여 준 지급 성공과 운영 지표 사이의 누락도 구현 범위로 관리한다.

<a id="source-f02428cfe7a062db-2-소유권-설계-기준안"></a>
#### 2. 소유권 설계 기준안

공용 전투 시도를 sharedAttempt로 식별하고 참가자의 경제 이력을 accountRun으로 분리한다. 이름은 논리 용어이며 신규 실제 ID/테이블 이름은 아직 정하지 않았다.

공용 시도는 맵 seed·경로·적/목표·진행·최종 전투 결과의 유일한 권위다. 서버가 승인한 참가자 관계에는 계정, 역할, 입장 자격/비용 영수증, 계정 소유 run을 연결한다. 같은 시도에서 같은 계정의 경제 run은 하나만 인정하며 역할 교체나 재접속으로 새 보상 권리를 만들지 않는다.

계정 소유 run은 기존 runs.account_id와 보상 미리보기/영수증 UNIQUE(run_id) 계약을 유지하는 방향이다. 이것은 두 개의 전투 시뮬레이션을 돌린다는 뜻이 아니다. 경제 run마다 맵을 다시 생성하거나 클라이언트가 별도로 승리 결과를 제출해 공용 판정을 덮어쓰지 못한다.

참가자 run 연결은 협동 입장/시작 처리에서 고정한다. 결과 화면에서 보상을 주기 위해 사후 가짜 START를 실행하거나 연료를 다시 차감하지 않는다. 실제 두 계정 입장 원자성·실패 복구는 기존 협동 admission 계약과 함께 설계해야 하며 이번 광고 기능이 이를 구현 완료로 대신하지 않는다. 기존 단일 run 기반 room을 새로운 연결 모델로 전환하는 어댑터와 이전 기록 복원도 필요하다.

이 방안은 공용 run 하나에 두 계정의 지급을 넣기 위해 기존 경제 테이블의 유일성을 일괄 완화하는 방안보다 소유권을 명확히 유지한다. 다만 참가자 입장/공용 결과 연결이 추가되는 비용이 있으며 본 문서만으로 현재 DB에 연결이 존재하지 않는다.

<a id="source-f02428cfe7a062db-3-공용-결과에서-계정별-보상으로"></a>
#### 3. 공용 결과에서 계정별 보상으로

공용 전투 판정이 완료되면 서버가 고정된 결과 ID와 참가자 자격 스냅샷을 보존한다. 참가자별 보상 생성 작업은 (공용 결과 ID, 계정 ID)를 유일한 처리 키로 사용한다. 계정별 미리보기에는 같은 공용 결과 근거와 자기 계정의 보상 설정/자격을 연결한다.

방 저장소와 경제 DB를 하나의 원자 트랜잭션으로 가정하지 않는다. 공용 결과 확정 및 재전달 가능한 전달 기록, 계정별 멱등 반영, 완료 확인을 통해 복구한다. 실제 스토리지별 구현은 서버 담당이 결정한다. 첫 참가자의 미리보기 생성 후 두 번째가 실패하면 이미 만든 것을 삭제하거나 첫 참가자에게 다시 지급하지 않고 누락된 참가자 반영만 재시도한다.

이후 각 계정은 자기 미리보기에서 기본 수령 또는 광고 룰렛을 고른다. A가2배, B가4배를 받거나 B가 광고를 보지 않는 것이 가능하다. 광고 참여 차이 외에 역할·딜량·마지막 타격·접속 복귀 순서로 정당한 기본 보상을 깎지 않는다. 공통 자격은 기존 협동 정책을 따른다.

동료의 광고·결과 화면 종료는 내 기본 수령을 잠그지 않는다. 한 계정의 네트워크 단절은 지급 취소가 아니며, 나중에 자기 계정 run/preview로 이어받는다. 다른 계정은 동료의 시도나 영수증을 조회·사용할 수 없다.

<a id="source-f02428cfe7a062db-4-광고-저장소와-admin-범위"></a>
#### 4. 광고 저장소와 Admin 범위

전투 광고 시도는 계정 소유 미리보기와 연결하는 신규 구조로 만든다. 0009의 provider 거래 NOT NULL을 가짜 거래 번호로 채우거나 UNIQUE를 피하려고 run_id를 NULL로 보내지 않는다. 기존0009 데이터는 역사 증거와 Admin 호환 대상으로 보존한다.

보급/전투/향후 리롤이 공유할 검증 provider 거래 소유권에는 provider와 provider transaction의 전역 유일성이 필요하다. 기존0009/0036의 거래를 조사·대조해 소유자가 충돌하면 운영 전환을 중단하고 기록을 확인한다. 초기 이관만 하고 기존 보급 쓰기 경로를 그대로 방치하면 다시 중복될 수 있으므로 신규 검증과 기존 검증이 모두 같은 권위 경계를 사용하도록 함께 연결한다. 재사용 방지는 서명 검증과 별개의 규칙이다.

Admin은 역사0009, 보급0036, 신규 전투 기록을 출처별로 구분해 검증 완료/지급 완료를 표시해야 한다. 같은 provider 거래를 여러 저장소에 기록했다고 합산 중복하지 않는다. ‘시도 생성’, ‘검증 성공’, ‘실제 지급’, ‘기본 수령 때문에 광고 미지급’을 구분한다. 클라이언트 로그를 지급 통계의 원본으로 쓰지 않는다.

<a id="source-f02428cfe7a062db-5-구현-작업-단위와-완료-조건"></a>
#### 5. 구현 작업 단위와 완료 조건

|순서|범위|완료 증거|
|---|---|---|
|1|기존 단독 보상의 기본 claim을 공통 정산 서비스로 분리|기존 수령·영수증·재접속·오류 행동 보존, 한 지급만 발생|
|2|단독 전투 광고 시도/고정확률/spin과 기본 claim 경합 연결|검증 없는 spin 거부,기본-vs-spin 하나만 지급,응답 유실 원결과 복원|
|3|보급/전투 공통 provider 거래 소유권과 기존 데이터 호환|교차보상/다른계정 거래 재사용 차단,기존 보급 정상 지급 보존|
|4|결과 UI의 기본/광고 CTA와 룰렛 복구 연결|광고 취소/검증 지연/계정 전환/화면 재생성에서 중복 실행 없음|
|5|협동 입장 관계 및 공용 결과→계정 run 정산 연결|2계정 자격·소유 검증,부분실패 재전달로 각 미리보기 하나|
|6|협동 계정별 광고 수령|A기본+B광고/A2배+B4배/동료 이탈 모두 독립 처리|
|7|Admin 집계·전체 회귀|기존/보급/전투 검증·지급 지표 일치,이중집계 없음|

1~4는 협동 정산 완성 전에도 단독 기준으로 격리 구현·검증 가능하다. 5~6을 생략하고 협동 룰렛 완료로 표시하지 않는다. 3의 호환 작업은 실제 활성화 전에 필수이며 순서2의 로컬 시험을 운영 지급 승인으로 해석하지 않는다.

세부 코드 책임: ResultSettlement는 지급 상태/금액 계획/원자 반영, CombatAdIntent는 시도/고정 정책/검증 상태, ProviderReceiptRegistry는 거래 소유권, CoopResultFanout은 공용 결과의 참가자별 멱등 전달을 맡는 논리 분리안이다. 파일/클래스 이름을 의무화하지 않으며 기존 서비스를 재사용하고 순환 의존을 만들지 않는다. Unity View는 Bind/Unbind 입력 콜백, Controller는 호출 조정, 표시 모델은 서버 상태를 표현한다.

<a id="source-f02428cfe7a062db-6-검수-경계와-남은-결정"></a>
#### 6. 검수 경계와 남은 결정

실제 기능 테스트는 아직 실행하지 않았다. 다음 검증은 격리 로컬 DB/서명 표본과 두 계정으로 수행하고 실제 START/연료·광고/계정 지급은 이번 조사에 포함하지 않는다. 운영 일일 횟수·시도 만료·재시도·광고 등록은 여전히 미결정이다. 확정 확률은6000/3000/1000이며 임의 균등값으로 대체하지 않는다.

이번에 확정한 것은 구현 작업 범위와 의존 순서다. 기존 비전투 시안 정합 작업의 우선순위를 유지한다. 다른 작업에 구현 착수를 전달하거나 새 작업을 생성하지 않았다.

<!-- source-body:source-f02428cfe7a062db:end -->


<a id="source-e5a41af1ba3b3528"></a>
## 2인 작전 물자·정비역·사건 의사결정 상세안

원출처: `docs/design/COOP-SUPPLIES-STATION-EVENTS-20260916.md`  
원문 SHA-256: `bf57818b37f372e886aac401e52f9d85eca648a3a8496e23abd9a7c80a187c0b`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Coop common wallet/sharedplan atomic consent90s/30s proposals; source says initial20 vs supplies initial30 conflicting proposals, do not select numeric.

<!-- source-body:source-e5a41af1ba3b3528:start -->
<a id="source-e5a41af1ba3b3528-2인-작전-물자정비역사건-의사결정-상세안"></a>
### 2인 작전 물자·정비역·사건 의사결정 상세안

2026-09-16 사용자 상세기획 진행 요청. 공용 지갑/공동 구매안/기한은 후속 설계 후보이며 운영 승인·실제 온라인 구현 완료 아님. 기존 온라인 정책의 각자 계정·열차/요원 역할·전투 중 역할/자산 교환 없음·서버 소유 run을 유지한다.

<a id="source-e5a41af1ba3b3528-1-적용-범위와-문서-우선순위"></a>
#### 1. 적용 범위와 문서 우선순위

협동 허용 콘텐츠 중 정비역/사건을 지원하는 작전에만 적용한다. 스토리를2인으로 확장하거나 월드레이드에 정비역을 자동 추가하지 않는다.

COOP-AUGMENT-SELECTION-DETAIL-20260914.md와COMBAT-COOP-ROLE-UI-20260914.md의 과거 종류 선추첨/레버 증강 선택은 최신 통합 동일 가중치 풀/카드 터치 전용 정책으로 대체됐다. 이를 구매 권한의 근거로 재사용하지 않는다. 한 판5증강을2인당5개로 해석하지 않는다. 혼합 후보에서2인 증강 최종확정권·카드별리롤 소유는 별도 미확정이며 이번 상점 합의 규칙을 자동 적용하지 않는다.

<a id="source-e5a41af1ba3b3528-2-공용-지갑-하나"></a>
#### 2. 공용 지갑 하나

- run의 작전 물자 잔액 하나를 두 사람에게 같은 개정번호와 함께 표시한다. 적 처치자·개인 기여·호스트에 따라 분배하지 않는다.
- 기본20을 두 계정에 각각20씩 주지 않는다. run획득 사건당 한 번,잔액과내역만 양쪽에 복제한다.
- 돈을 절반씩 나누는 개인 지갑/개인 이월/계정 재화 이동은 첫안에 없다. 구매한 개조/장비도 run 상태이며 열차 담당 계정으로 영구 이전하지 않는다.
- 잔액 소유자는 서버다. 클라이언트에서 임시로 차감한 값을 진실로 취급하지 않는다. 응답이 늦어도 같은 구매를 두 번 누를 수 없게 요청 상태를 표시한다.

<a id="source-e5a41af1ba3b3528-3-전투-조작과-정비-제안-권한"></a>
#### 3. 전투 조작과 정비 제안 권한

|행동|열차 담당|요원 담당|
|---|---|---|
|주행 속도·실제 선로 확정|기존 조작 유지|위험/목적지 요청 신호|
|지원3종·비상 장비 실행|실행|지원/대상 요청. 대신 실행하지 않음|
|내부 집중·요원 궁극기|상태 확인/요청|기존 조작 유지|
|정비역 상품 보기·비교|가능|가능|
|공동 구매안 추가·수정 제안|가능|가능|
|정비 결제/출발 동의|본인 동의|본인 동의|

Combat객차 정비·위협 추적 개조처럼 내부전투에 영향을 주는 상품은 요원 담당이 직접 구매안에 넣을 수 있다. 열차/Gun 상품도 제안 자체를 금지하지 않으며 대상 소유권 변경은 아니다. 실제 장착/수리는 양쪽의 동일 구매안 승인 뒤 서버가 실행한다.

이 문서는 상품을 새로 늘려 요원 담당 전용 영구강화를 만들지 않는다. 처음부터 각역에서두사람모두같은양을소비할필요도없다. 운영상한쪽제안이계속묵살되는문제는실제2인검증에서평가해야한다.

<a id="source-e5a41af1ba3b3528-4-공동-구매안과-한-번의-결제"></a>
#### 4. 공동 구매안과 한 번의 결제

상점에서 즉시 개별결제하는1인 흐름과 달리,2인은 ‘함께 정비’ 목록에 구매를 담는다. 목록에는 대상,가격,기존→교체 결과,폐기물,총액,결제후잔액을 두 화면에 동일하게 표시한다.

1. 어느 쪽이든 상품/대상을 제안한다. 목록 수정 명령은 현재 planRevision을 포함한다. 동시 수정이면 서버가 수락한 새 버전을 보내고 오래된 편집을 재확인하게 한다.
2. 같은 객차에 개조2개처럼 충돌하는 항목은 목록 단계에서 막는다. 같은 수리를 여러 번 담으면 최종실효회복량·재고를 순서대로미리계산한다. 실제 효과가0인 상품에 돈을 쓰는안은 거부한다.
3. 본인의 ‘정비 확정’을 누르면 그 목록버전/총액/대상 결과에 동의한다. 상대 동의 없이 결제하지 않는다.
4. 목록을 수정하면 양쪽 동의를 모두 해제한다. 승인된 값과 다른 항목을 몰래 끼워 넣지 않는다. 마감시각은 편집으로 연장되지 않는다.
5. 같은 최신 버전에 양쪽 동의가 모이면 잔액·재고·대상 상태를 재검증하고 전체 목록을 한 번에 확정한다. 하나라도 실패하면 전체 미결제,이유와 최신 상태를 공유한다. 부분결제 후 나머지 실패로 설명과 실제소비가 어긋나지 않는다.
6. 성공하면 같은 purchaseId 영수증과 변경 스냅샷을 양쪽에 전송한다. 응답 유실 재시도는 같은 결과를 반환한다.

첫안은 역마다 유료 결제1회다. 무료권·기본장치복귀·임시객차분리도 같은 계획에 포함하며 효과 순서를미리표시한다. 결제 후 추가 쇼핑을 반복해 동의를 계속 요구하지 않는다. 1인 화면에도 같은 계획표현을 재사용할 수 있지만 본인 확인1회면된다.

무료복귀/객차분리처럼돈이안드는행동도상대의편성운영을바꾸므로합의대상이다. 계정의원래출발객차삭제/자산교환은허용하지않는다.

<a id="source-e5a41af1ba3b3528-5-출발-합의와-상점-기한"></a>
#### 5. 출발 합의와 상점 기한

정비역 상태는 둘 모두 같은 서버 안전 단계에 진입한 뒤 열린다. 클라이언트 메뉴를 먼저 열었다고 전투를 멈추지 않는다. 안전 정비 단계에서는 전투 시계가 멈추고,상점 기한은 별도 서버 현실시계로 진행한다.

- 첫 검증 기한90초. 신규 확정 운영값이 아니다. 남은 시간은 두 화면에 표시한다.
- 유료 목록의 양쪽 ‘정비 확정’은 결제와 출발준비를 함께 의미한다. 성공 영수증을 보여주는 짧은 공통 전환 뒤 출발한다. 확인 뒤 아무 말 없이 추가 구매 단계에 남겨두지 않는다.
- 빈 목록이면 둘 다 ‘정비 없이 출발’을 누르면 조기 출발한다. 한쪽만 동의하면 마감까지 대기하며 누구도 다른 사람의 동의 버튼을 대신누르지못한다.
- 마감까지 합의 못하면 미확정 목록을 취소하고 구매없이 운행 복귀한다. 잔액/재고/장비는 보존한다. 자동추천 구매·자동수리·벌금은 없다.
- 결제성공과마감이경합하면서버처리순서로하나의결과를확정한다. 성공한구매를마감때되돌리거나두번결제하지않는다.

소통에충분한시간인지,모바일읽기부담이큰지실제2인으로검증한다. 지연/반복편집/부재로상대를무기한붙잡지않는것이목적이다. 이90초를증강기한이나실제전투제한시간으로복사하지않는다.

<a id="source-e5a41af1ba3b3528-6-현장-사건-결정"></a>
#### 6. 현장 사건 결정

실제 분기 진입/통과는 열차 담당이 조작한다. 요원 담당에게 동일 예고와 요청 신호를 제공한다. 이후 안전 사건 선택은 양쪽의 같은 선택 동의로 결정하는 첫안이다.

- 비용뿐 아니라 임시객차추가/분리/장비교체/구조작업수락/수리대상도 공동선택이다. F01부품회수와연결처럼서로배타적인선택을동시실행하지않는다.
- 한쪽이선택안을지정하면다른쪽은동의또는다른안제안. 변경은기존동의를해제한다.
- 기한30초검증안. 미합의/마감시 기본값은 ‘통과’. 비용·보상없음. 명시선택없이부품20을자동지급하지않는다.
- 이미수락한호위·회수작업중에는전투가진행한다. 온라인메뉴/설명창/연결끊김이작업을정지시키지않는다. 실패/포기는실제작업규칙으로처리한다.
- 무료수리권은공용run권리이며다음역공동구매안에적격대상1개로넣는다. 한쪽계정에개별무료권을복제하지않는다.

<a id="source-e5a41af1ba3b3528-7-연결-끊김과-복귀"></a>
#### 7. 연결 끊김과 복귀

기존 온라인 규칙처럼 연결이 끊겨도 서버 소유 전투/시도는 삭제하거나 다른 참가자로교체하지않는다. 역할양도·대체계정입장·상대수동궁극기대신시전은없다.

|상황|처리 후보|
|---|---|
|정비/안전사건에서 한쪽 끊김|해당 참가자의 미확정동의해제,마감시각유지. 복귀하면 같은잔액·안·재고·남은시간수신|
|결제접수 직전 끊김|서버가이미양쪽동의를수락했는지로결정. 이후클라이언트상태로기록을소급삭제하지않음|
|이미결제완료 후 끊김|구매보존,같은영수증재전달. 이중결제없음|
|둘다끊김|기존attempt수명유지,안전단계기한은진행. 미합의종료시통과/구매없음. 재접속으로기한초기화안함|
|전투중끊김|전투시간계속. 기존서버기본공격/복구는계속되지만새수동명령을만들지않음|

전투 중 열차 담당 부재의 실제기본경로/유지속도,요원 담당 부재의기본공격·AUTO는기존명령정책을따른다. 이번문서가새로운자동플레이어/역할인수/자동비상장비를추가하지않는다. 영구이탈에따른패배·정산정책은현재정산계약검토가필요하며부재만으로0보상이라고새로정하지않는다.

새연결은기존계정·멤버십·역할을인증하고오래된연결명령을차단한다. 입력중손가락드래그나미확정UI선택은재생하지않는다. 단절을이용해다른재고/사건/역할을얻지못한다.

<a id="source-e5a41af1ba3b3528-8-화면과-소유권"></a>
#### 8. 화면과 소유권

공통 잔액/수입소비내역/계획목록/상대동의/남은시간은두화면동일한권위개정번호에대응한다. 각자스크롤·상품상세열람은개인표현상태이며상대화면을강제로바꾸지않는다. ‘상대가검토중’과‘연결끊김’을구분하고상대탭위치를과도하게중계하지않는다.

View는제안·동의·취소의도를callback으로전달한다. Controller는공통명령을사용하고서버run경제/정비단계가검증·합의·차감·효과·영수증을소유한다. 계획편집이역할자산을직접수정하지않는다. 대기중화면해제/늦은응답/중복구독/기한타이머수명검증필수.

<a id="source-e5a41af1ba3b3528-9-검증과-다음기획"></a>
#### 9. 검증과 다음기획

미실행:동일수입단일지급,두편집경합,같은개정동의,수정후동의해제,잔액/재고부족전부원복,복구+개조순서,0효과수리거부,결제vs마감,결제응답유실,양쪽끊김·재접속,무료권만료,부재시무단시전없음,개인계정자산불변,한쪽지속거부시90초종료,실제두역에서대기피로.

다음은 **2인 통합증강3후보의확정권·리롤권한·5회귀속**을최신동일가중치정책에맞춰정리한다. 현재옛종류별담당규칙을그대로구현하면혼합후보에서권한이모순되므로별도설계가필요하다. 공간계약(T19/T20/O18)도남아있다. 신규네트워크/서버경제/실START는변경하지않았다.

<!-- source-body:source-e5a41af1ba3b3528:end -->


<a id="source-991422e2420e54e4"></a>
## 2인 통합 증강 선택·카드별 리롤 상세안

원출처: `docs/design/COOP-UNIFIED-AUGMENT-CHOICE-20260916.md`  
원문 SHA-256: `af8748b6098ae456801506968b7ba2c403c4421535476f5f3f9c249794a64492`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Coop alternating A/B grant owner,45+A deadlines/autofirst candidate only; common grant PER SLOT3+1 not shared acrossslots; fixed5 obsolete.

<!-- source-body:source-991422e2420e54e4:start -->
<a id="source-991422e2420e54e4-2인-통합-증강-선택카드별-리롤-상세안"></a>
### 2인 통합 증강 선택·카드별 리롤 상세안

2026-09-16 사용자 다음 상세기획 요청. 혼합 풀/동일 가중치/3후보/한판5선택/카드별무료3+광고1은 기존 확정 정책이다. 아래 교대 선택권·기한·광고 대기 계약은 이를2인에 맞춘 설계 후보이며 신규 승인 운영 규칙/구현 완료가 아니다.

<a id="source-991422e2420e54e4-1-권한-방식과-선택-이유"></a>
#### 1. 권한 방식과 선택 이유

비교한 방식:
- 카드 종류별 담당:혼합 후보에서누가최종선택하는지충돌하고유형별개인선택은한회2효과를만들수있어제외.
- 매번공동동의:협의는가능하지만리롤/타임아웃마다교착하고5회선택의대기비용이커첫안에서제외.
- **회차별선택담당교대:** 한명은최종선택/리롤,다른한명은의견표시. 통합추첨을바꾸지않고결정권을나누므로첫검증안으로채택.

선택담당은전투역할과별도인해당grant의권한이다. 요원담당이열차카드를고를수있지만스로틀/열차지원을조작할권한을얻지는않는다. 열차담당이요원카드를골라도요원궁극기시전권을얻지않는다.

<a id="source-991422e2420e54e4-2-5회-분담"></a>
#### 2. 5회 분담

run생성시시작선택담당을두멤버중동일확률로한번정하고고정한다. 초대방장/접속순/계정전투력에우선권을주지않는다. 이추첨은카드유형/후보추첨과분리한다.

|회차|담당|
|---|---|
|1|시작담당A|
|2|상대B|
|3|A|
|4|B|
|5|A|

한판의결정권은3회/2회이며양쪽각각5회가아니다.5라는홀수때문에생기는차이를공개한다. 다음판에무조건반대역할이라는계정간숨은보정은넣지않는다. 실제체감불균형이크면교대시작정책을재검토한다.

카드선택효과는공용run에단한번적용한다. 효과의열차/요원/개체범위는카탈로그가정하며누가선택했는지로그만남긴다. 계정영구획득/양쪽중복지급없음.

<a id="source-991422e2420e54e4-3-화면과-상대의견"></a>
#### 3. 화면과 상대의견

두사람에게같은grantID/후보3장/슬롯개정번호/유형/효과/기본·광고잔여/선택담당/남은시간을표시한다. 시스템추천/추천확률/빌드우대는없다.

선택담당은기존카드터치로최종확정한다. 상대는상세열람과‘이카드희망’또는‘이슬롯교체요청’을보낼수있다. 이는동료이름이붙은의견이며자동추천표시와구분한다. 요청만으로리롤/선택/시간연장은일어나지않는다. 상대동의버튼/거부권은추가하지않는다.

동료의희망표시는후보ID+슬롯개정에귀속한다. 카드가바뀌면그표시를지우며새카드에이전희망을옮기지않는다. 신호반복은표시빈도를제한하고게임명령큐를압도하지않게한다.

일반경로레버는증강선택명령에연결하지않는다. 선택구간전안전조건과기존hold/운행복귀계약을유지한다. 증강화면을열었다고포탄/위험을삭제하지않는다.

<a id="source-991422e2420e54e4-4-리롤-소유와-경쟁-처리"></a>
#### 4. 리롤 소유와 경쟁 처리

- 슬롯마다무료3회+광고1회는**공용grant기준**이다. 두계정에각각3+1을주지않는다. 담당교대는다음grant이며그때새3+1을받는다.
- 현재선택담당만리롤명령을실행한다. 상대는교체요청만가능하다. 같은슬롯의동시요청은한개만접수한다.
- 서버가현재슬롯후보와다른두슬롯후보를제외한적격풀에서동일가중치로새후보를뽑는다. 대체후보없음/실패는카드·잔여횟수유지. 역할/희망표시로추첨가중치를바꾸지않는다.
- 첫구현안은grant내리롤처리한개씩직렬화한다. 다른슬롯리롤/선택은처리완료스냅샷후입력한다. 이로인해응답유실에무기한잠기지않도록같은requestID조회/재시도와서버종결이필요하다.
- 선택/리롤은현재grantRevision과candidateID를검증한다. 오래된카드터치로새카드를선택하지않는다. 한명령의권위확정순서로결과를정하고선택확정뒤리롤은거부한다.

<a id="source-991422e2420e54e4-5-광고-리롤과-시간"></a>
#### 5. 광고 리롤과 시간

기본선택기한은45초검증안이다. 서버현실시계로진행하고후보확인/동료의견/일반리롤/재접속으로초기화하지않는다. 정비90초·사건30초와별도다.

광고를보는동안상대가카드를선택하거나대상슬롯을바꾸면결과가모순되므로,광고리롤은서버에먼저승인된세션을만들고grant를AD_PENDING으로잠근다. 현재선택담당만시청하며상대에게해당슬롯과광고처리상태를표시한다. 광고시작/시청만으로횟수를소비하지않는다.

- AD_PENDING동안기본45초기한진행을보류하되별도광고세션기한A는현실시간으로진행한다. 재시청/SDK콜백으로A를연장하지않는다.
- 광고검증성공+대체후보존재+세션유효일때카드교체와그슬롯광고1회소비를한번확정한다. 실패/중단/만료면카드와사용가능횟수를보존한다.
- 성공/실패후기본선택의남은시간을재개한다. 처리전입력한동료요청/카드터치는자동재실행하지않는다.
- 1개grant에서각슬롯광고1회를모두쓴경우최대대기상한은45+3A초다. 반복실패도무한시간보류가되지않도록**슬롯별광고시도에허용한누적대기예산A**를쓴다. 실패는리롤횟수를소모하지않지만시간예산은되돌리지않는다.
- A소진후그회차광고입력은기한종료사유로비활성,무료리롤/기존카드선택은유지한다. 다음grant에는새예산. 이를'광고1회사용완료'로표시하지않는다.
- A는실제SDK시청·검증시간을측정한뒤정해야한다. 일반광고조차끝내지못하는짧은값을임의출시하지않는다. 지연콜백은만료세션을되살리거나종료된grant를변경하지않는다.

광고3회를실제로보면상대대기가길다는제품상위험은남아있다. 광고시청을조합완성의필수조건으로설계하지않으며,실기기2인대기검증없이는완료처리하지않는다. 기존광고3슬롯각1회정책을무단삭제하거나양쪽동시광고로우회하지않는다. 시간제/랭킹모드의선택·광고시간점수반영은별도규칙확정전적용하지않는다.

<a id="source-991422e2420e54e4-6-기한-종료연결-끊김"></a>
#### 6. 기한 종료·연결 끊김

선택담당이기한까지선택하지못하면서버가현재유효한첫번째슬롯을확정하는기본안이다. 이는강함/유형/희망에따른추천이아닌사전공개순서다. 자동선택예정표시를모두에게보여준다. 대안으로재추첨하지않는다.

첫번째가효과적격성을잃으면현재표시된다음유효슬롯으로넘긴다. 세장모두무효면소유grant를소비하지않고명시복구상태로둔다. 무효카드를성공지급하거나새확률로몰래재추첨하지않으며,이상태의재생성/중단계약은적격성서비스구현전완료조건이다.

담당연결끊김도기본기한유지. 상대가역할을탈취하거나새수동궁극기를대신쓰지않는다. 원계정복귀시같은담당권·후보·잔여·서버승인광고상태를복원한다. 광고중끊김은A안에서검증결과를기다리고종결하며타계정으로광고권을이동하지않는다.

둘다끊겨도server-ownedrun/기한정책은유지한다. 실제전투종료가먼저확정되면grant를닫고늦은광고·선택은효과를추가하지않는다. 선택확정과만료가경합하면서버에서먼저확정한유효결과하나만존재한다.

<a id="source-991422e2420e54e4-7-상태와-책임"></a>
#### 7. 상태와 책임

OPEN→REROLL_PENDING/AD_PENDING→OPEN→COMMITTING→COMMITTED 또는 TERMINATED. 클라이언트로딩표시는서버단계와구분한다. 실제DB필드/상태수를그대로이식하라는명세는아니다.

서버grant소유자가담당·후보·리롤잔여·시도예산·기한·선택효과영수증을관리한다. View의본인담당여부표시는권한검증을대체하지않는다. Controller는카드선택/리롤/동료의견을각명령에연결하고Bind/Unbind·늦은응답·재접속개정변경을처리한다.

실제배포풀의효과검증·중복보유·적격후보소진정책은기존미확정항목이다. 40후보존재를런타임40구현으로오인하지않고,이권한설계가그미해결을해결했다고주장하지않는다.

<a id="source-991422e2420e54e4-8-비교-검증과-다음-단위"></a>
#### 8. 비교 검증과 다음 단위

미실행:5grant에서3/2권한배분,선택역할과효과유형분리,계정지급0/공용효과1,공유슬롯3+1,빠른동시리롤/선택·stale tap,희망표시만료,광고실패무소비/예산비복원/늦은콜백,기한자동선택,양쪽끊김·복귀,전체무효,모바일2인대기피로,시너지편향없는추첨.

다음은 **외부 공간과 유효 거리 계약**:T19계류방벽/T20거점전개/O18외부궁극기/O14잔류/회수·호위사건이함께사용할맵고정영역과표적범위를정한다. 증강2인정책은위후보비교후확정해야하며지금런타임/실광고/네트워크변경은없다.


<!-- source-body:source-991422e2420e54e4:end -->

2026-09-22 월드WB3: [44 효과/협동참여](44_COMBAT_DETAIL_CONTRACTS.md#world-boss-effects-coop-v03) 작성,신규승인대기. WB2전투안승인·정사배정별도. 기본증강0/40종적격분류·장비/지휘원천소유·Sera/T18·유효참여계측/대행 범위정의. 다음WB4 주간/입장권/랭킹·보상/화면연결,전체월드미완료.

<a id="authority-split-host-coop-v01"></a>
## 전투 판정 담당 분리·일반 협동 호스트 방식 v01 — 2026-09-22

**사용자 확정:** 월드보스는 솔로·협동 모두 서버 전투 판정을 유지한다. 일반 협동은 한 플레이어 클라이언트가 호스트로 전투를 계산하고 다른 클라이언트는 입력 전달/상태 수신을 맡는다. 목적은 서버 실시간 전투 계산 부담 절감이며 통신량·비용 절감률을 보장하지 않는다. 기존 모든 협동을 서버 실시간 전투로 묶은 정책은 신규 일반 협동에 한해 대체한다. 아래 미결 세부항목까지 승인으로 확장하지 않는다.

### 1. 모드별 소유권 — 승인 기준

| 모드 | 실시간 HP/피해/공격·스킬/쿨다운/전투시간 | 서버 소유 |
|---|---|---|
| 일반·스토리 솔로 | 기존 Unity 로컬 전투 | 인증/보유/편성/스탯·정의/START, 선택·거래·진행 및 제출 결과/보상 검증 |
| 일반 협동 | 세션에 지정된 단일 Unity 호스트가 양쪽 전투 상태 계산; 상대는 입력/표시 | 방/참가자/역할·호스트 지정, 출발 데이터·선택/구매·판매/재화·진행·결과/보상 검증 |
| 월드보스 솔로·협동 | 서버가 HP/시간/기믹/피해/점수 확정; 클라이언트는 입력/표시 | 기존 입장권·참여·랭킹·경제·전투 판정 모두 유지 |

전투 상태의 기준은 모드마다 하나다. 두 클라이언트가 각자 HP를 확정한 뒤 마지막 도착 값으로 덮어쓰지 않는다. 상대 화면의 보간/예측은 표시용이며 보상·진행의 근거를 새로 만들지 않는다. 서버가 저장한 호스트 체크포인트는 복구용 보고 데이터이며 서버가 실제 전투를 재계산해 검증한 상태와 구분한다.

### 2. 실행 흐름·경제 경계

1. 서버는 기존 소유/편성·규칙 버전/적격성을 검증하고 참가자·역할·호스트가 식별되는 원 START 자료를 발급한다. 호스트는 이 자료의 스탯·정의·seed를 소비하며 새 재화/장비/효과를 임의 발급하지 않는다. 호스트를 열차 역할과 자동 동일시하지 않는다.
2. 양쪽 플레이어의 입력은 단일 호스트 전투 명령 경계로 모인다. 호스트는 실시간 타깃/공격·HP/다운/복귀/전투시간과 원사건 순서를 계산한다. 서로 같은 적/열차/요원의 피해를 중복 처리하지 않는다.
3. 증강 선택·리롤·경로 확정·구매/판매·재화 변경·해금·스토리 진행 등 서버 소유 명령은 기존 서버 거래를 거친다. 호스트 보고만으로 승인하지 않는다. 성공한 원거래 결과를 원명령에 연결해 전투에 한 번 반영하고, 응답 유실 때 새 거래 대신 원결과를 조회한다. 서버 복구용 저장과 매 타격 동기 HTTP 승인 의존은 구분한다.
4. 일반 협동 전투 시계는 호스트의 논리시간을 기준으로 상태와 함께 동기화한다. 승인된 일반 hold/선택 정지는 해당 정책대로 공유하며 각 기기가 독립 pause하지 않는다. 입장/인증·경제 기간은 서버 시간, 월드보스180초는 서버 전투시간으로 유지한다.
5. 종료 보고를 서버가 수신·검증하고 계정별 결과/보상·스토리 권리를 멱등 확정한다. 클라이언트의 승리/HP 보고나 두 사람의 일치만으로 정상 전투를 증명했다고 하지 않는다. 검증 불충분/불일치는 확정 성공과 구분하며 보상 검증 수준·보류/재검토 처리는 아래 세부 계약에서 확정한다.

### 3. 통신·복구 설계 요구사항 — 수치/제품 선택 미확정

- 매 프레임 전체 상태 전송을 기본으로 하지 않는다. 변경분 묶음, 공격/스킬/다운/복귀 등 사건, 기준 논리시각과 주기적 보정, 재접속/불일치 시 전체 스냅샷을 조합한다. 전송 주기·배치 크기·오차 허용치는 기기/지연/대역폭 계측 후 제안한다.
- 복구/중복 방지 계약에는 run·참가자·호스트 세대·명령/상태 순번·기준 snapshot revision·논리시간·정의 버전의 의미가 필요하다. 최종 DTO 이름은 담당 계약에서 확정한다. 전투효과별 전체 상태/원천·잔여시간·카운터·다음 tick을 포함해 재접속으로 초기화/중복 공격/무한 연장하지 않는다.
- 일시 단절과 호스트 교체를 구분한다. 재접속한 구 호스트와 새 호스트가 동시에 권위를 갖지 않도록 서버가 승인한 호스트 세대를 검증해야 한다. 클라이언트 자기 승격이나 마지막 패킷 우선은 금지한다. 일반 협동에 월드보스 단절대행30/60초·참여90초를 복사하지 않는다.
- **추가 상세기획 잔여:** 최초 호스트 선정 기준, 단절 유예/정지·종료 규칙, 자동 호스트 이관 채택 여부, 체크포인트 주기·신뢰/복구 범위, 일반 협동 결과 검증의 보장 수준, 손실/순서역전 처리 및 전송 주기. 미정 상태에서 자동 몰수·무료 환급·자동 승리·호스트 이관을 구현하지 않는다. 이번 승인으로 새 벌칙/재화 정책을 만들지 않는다.
- 전송 수단은 실제 환경에서 비교한다. 직접연결/중계 중 어느 제품도 이번에 채택하지 않는다. 서버 중계면 트래픽은 남고, 직접연결도 연결 성립/기기 환경 대응 비용이 있다. 월드보스 연결을 일반 호스트 데이터 통로로 이름만 바꾸지 않는다.
- 서버 검증의 한계: 보유·스탯·선택·거래·진행 검사만으로 호스트의 모든 HP/피해 조작을 검출할 수 없다. 체크포인트/hash·양측 일치도 진실 증명이 아니다. 결과 재현 검증을 채택하려면 결정론·완전한 입력/상태·원버전 재생을 별도 입증해야 하며 기존 가변시간 로컬 전투가 이를 이미 지원한다고 가정하지 않는다.

### 4. 기존 작업 정정·담당

- S-01/S-02: 공통 카탈로그/스탯·START/버전·거래/진행·결과/복구 계약 유지. 신규 일반 솔로/협동을 매 타격 서버 계산·HP 저장 완료 응답 없이는 전진하지 않는 구조로 바꾸지 않는다. 이미 구현된 정확 피해·궁극기·HP ledger는 삭제하지 않고 월드보스/검증/복구 재사용 범위를 근거로 분리한다. 과거 테스트 PASS는 당시 범위로 보존한다.
- S-03/S-04/U-03: 월드보스 서버 전투/경제 유지. U-01 및 S-08~11/U-07~10: 같은 능력 규칙을 일반 로컬/호스트 실행과 월드 서버 실행에 각각 연결한다. Server의 스킬 정의/모드 지원 확정은 일반 전투 루프를 Server에 이전한다는 뜻이 아니다. 클라이언트 효과 구현은 Unity 소유다.
- Plan은 이 정책과 미결 복구/검증 경계를 소유. Server는 방/호스트 권한·경제/진행·결과 검증·허용 전송 계약, Unity는 단일 호스트 전투·입력/상태 동기화·복구·표시 수명을 소유한다. Direction은 기존 미술을 유지하고 실제 연결/대기/복구 상태 표시를 명세한다.
- 진행run은 START의 원모드/실행 정책을 유지한다. 기존 run을 호스트 방식으로 중간 전환하거나 서버 월드run을 클라이언트로 이관하지 않는다. 실제 전환은 새 지원버전의 신규run에만 적용한다.

### 5. 인수와 측정

두 실제 클라이언트에서 양쪽 입력이 한 번만 실행되는지, HP/다운/시간 일치, 중복/누락/역순 패킷·재연결 복구, 구 호스트 패킷 거절, 거래 성공/응답 유실의 효과1회, 버전 불일치·조작된 선택/재화 거절, 결과 중복 지급0을 검증한다. 정상 입력만 반복한 단일 fixture로 전체 완료를 주장하지 않는다. 미정 복구/검증 정책은 확정 후 해당 실패 사례를 추가한다.

동일 전투·기기·인원·시간에서 기존 서버 전투와 호스트 방식을 비교: 서버 CPU/실행시간, 송수신 byte와 메시지 수, 중계 포함 트래픽, 클라이언트 프레임/발열, 상태 지연·보정 횟수·재접속 비용. 측정 전 비용 절감률·운영 가능성은 미확인이다. 코드/배포/실제 계정 변경은 이 문서 확정 작업의 완료 범위가 아니다.
<a id="ordinary-coop-host-recovery-v02"></a>
## 일반 협동 호스트 선정·복구·결과 검증 v02 — 2026-09-22 / 상세 제안

> **후속 사용자 정정:** 아래 자동이관 제외·1인완주 금지·양측 최종ACK 필수는 [v03 잔류자 완주 정책](#ordinary-coop-survivor-clear-v03)으로 폐기·대체한다. 기타 시험 수치/기한의 승인 대기 상태는 유지한다.

**상태: 신규 세부정책·시험 수치 승인 대기.** v01의 일반 협동 호스트/월드 서버 판정은 확정이다. 이번 ㄱㄱ는 그 후속 상세안 작성 지시이며 아래 재접속 기한·자동이관 제외·중단/결과 보류 정책까지 승인으로 기록하지 않는다. 범위는 일반 협동뿐이고 월드보스 솔로/협동 및 기존 솔로 저장 정책을 바꾸지 않는다.

### 1. 추천 구성과 대안

추천 A: 원래 호스트 한 명으로 작전 끝까지 진행하며 전투 중 자동 호스트 교체는 초기 지원에서 제외한다. 통신 단절 시 공동 전투를 멈추고 원 상태를 복구한다. 서버는 권한·경제·진행·검증을, 호스트는 실시간 전투를 소유한다. 승인된 일반 역할 분담/양측 참여를 보존하며 파트너가 사라졌다고 AI대행·1인 완주 권한을 신설하지 않는다.

대안 B 자동이관: 원호스트가 돌아오지 않아도 상대가 이어갈 수 있으나 완전한 상태 이전·권한 세대 교체·옛호스트 차단·검증/경제 인계까지 필요하다. 단순 패킷 전달 기능으로 간주하지 않는다. 대안 C 매 피해 서버 검증은 일반 협동 계산 절감 목적과 충돌하므로 이번안에서 채택하지 않는다. 향후 B는 검증 후 별도 확대 가능하며 이번 A는 원호스트 이탈에 취약하다는 제품상 대가가 있다.

### 2. 출발 전 호스트 선정

- 대기방 생성자와 전투 호스트는 다른 개념이다. 열차/요원 역할·보유 스펙·과금·계정레벨로 호스트 우선권을 주지 않는다.
- 서버가 두 참가자의 인증·현재 연결·동일 지원 전투 버전·필수 상태 직렬화/복원 지원을 확인한다. 누락된 버전은 출발 거절, 호스트가 부족한 효과를 자체 생략하지 않는다.
- 양쪽 모두 준비 완료 후 후보별5초 연결 표본을 수집하는 첫시험안. 중계라면 상대까지 실제 전달 왕복값을 사용하며 API ping만으로 직결 품질을 추정하지 않는다. 높은 p95 왕복시간이 더 작은 후보를 우선, 차이가20ms 이하면 서버 방 입장 순번이 앞선 후보를 선택한다. 편도 경로가 대칭이라 두 후보 차이가 없으면 순번으로 결정한다. 측정 불가/필수 연결 실패는 호스트 추측 선택 없이 출발 대기다.
- 기기 성능은 과거 점수로 등급화하지 않고 동일 테스트 전투를 감당하는지 별도 실기 검증한다. 성능 부족을 발견하면 출발 전에만 다른 적격 후보로 재선정. 상대 수동 동의 없이 역할·편성은 바꾸지 않는다.
- 서버는 선택된 계정/연결 세대와 run을 결합해 START에 고정한다. 출발 전 연결/편성 revision이 바뀌면 준비를 취소하고 재검증한다. 수락된 START의 재전송은 원호스트/원비용을 재사용하며 새 작전을 만들지 않는다.

### 3. 전투 상태와 명령 경계

표기 이름은 설계용이며 실제 wire DTO/endpoint는 S-12가 확정한다.

| 기록 | 필수 의미 |
|---|---|
| 입장/실행 정책 | run, 원참가자2명·역할, hostAccount, hostEpoch, 모드/지원버전, 원START 참조 |
| 입력 | 계정·역할·명령ID·입력순번·run/hostEpoch·명령종류/대상; 피해량/자기HP를 입력으로 신뢰하지 않음 |
| 전투 사건 | 원행동ID·사건순번·전투 논리시각·원천/대상·효과·원결과 |
| 상태 차분 | 기준 snapshot revision·마지막 사건순번·변경값; 기준이 다르면 적용 대신 전체 복구 |
| 복구 체크포인트 | 모든 전투 개체·HP/방벽/상태층/대상·행동큐/투사체·tick/CD/다운/복귀·PRNG 상태·맵 진행·효과 카운터·거래 적용 cursor·논리시간 |
| 종료 보고 | 원START/규칙버전·최종 checkpoint/사건순번·성공/실패 근거·웨이브/스토리 사건 보고·서버 거래 cursor·각 계정 수신 확인 |

호스트 입력도 상대와 같은 도메인 명령 경로를 통과한다. 같은 ID의 재전송은 원결과, 다른 payload로 ID 재사용은 충돌 거절. 상대 입력은 도착 순서를 호스트가 확정하되 자기 역할 밖 입력은 거절한다. 오래된 세대/다른 run 패킷은 버리고 순번 누락을 보간한 피해로 채우지 않는다. 재접속 때 미확정 공격/버튼을 새 ID로 자동 실행하지 않는다.

### 4. 첫 측정용 전송 수치

- 대체 가능한 전투 상태 차분: 최대10회/초, 100ms 묶음. 변경 없으면 상태 heartbeat를 위해 전체 상태를 보내지 않는다. 화면 렌더 프레임과 네트워크 갱신은 독립이다.
- 중요한 입력/확정 사건: 해당 전송의 즉시 flush와 ACK/동일ID 재전송. 초당 공격마다 HTTP 저장 호출하지 않는다. 이동/조준처럼 덮어쓸 수 있는 입력과 구매·스킬처럼 버리면 안 되는 명령을 구분한다.
- 연결 확인: 유휴 시1초, 정상 인증 패킷/ACK가 있으면 별도 heartbeat를 중복 전송하지 않는다. 3초 연속 유효 응답 없음 또는 명시 연결 종료를 단절 후보로 삼는다. 서버 연결도 끊기면 경제/진행 명령뿐 아니라 공동 전투의 연속 진행을 멈추는 초기안이다.
- 서버 내구성 복구 checkpoint: 전투 진행 중5초마다 한 묶음. 호스트 본문1개와 상대의 같은 revision/digest 수신 확인을 기록하며 서버 검증된 피해라는 표시는 붙이지 않는다. 멈춘 전투에서 변하지 않는 본문을5초마다 다시 저장하지 않는다.
- wave/node 경계, 서버 거래 직전, 종료 때에는 강제 checkpoint 장벽을 둔다. 상대/서버 확인 전 다음 경계로 진행하지 않는다. 일반 타격은 이 장벽 대상이 아니다.
- 체크포인트 확인 없이 진행한 전투 논리시간이5초에 도달하면 저장 확인 대기로 정지하는 초기안. 따라서 정상 복구의 롤백은 마지막 공동 내구성 상태 이후 최대5초이며 실제 네트워크 지연으로 정지 빈도가 높아지면 계측 후 주기/상한을 함께 조정한다. 상대가 마지막 화면에서 본 최근 피해가 되돌아갈 수 있음을 복구 안내에 표시한다.
- 입력/사건 재전송/스냅샷 byte 한도·큐 상한은 실제 payload를 측정해 S-12가 계약값으로 제시한다. 한도를 넘으면 조용히 사건을 버리지 않고 복구 상태로 전환한다. 이 수치들은 출시 확정값이 아니라 비교 측정 시작값이다.

### 5. 단절 상태 전이와 보관

| 상태 | 허용 전이/처리 |
|---|---|
| PREPARING | 서버 START 성공 후 RUNNING; 후보 탈락/연결 실패면 재준비, 미출발 비용 중복 없음 |
| RUNNING | 단절 감지→RECONNECTING; 마지막 전투 종료 보고→RESULT_PENDING |
| RECONNECTING | 양쪽 원계정 재연결·원호스트 복구·같은 checkpoint 확인→RUNNING; 최초 감지부터60초 안에 실패→SUSPENDED |
| SUSPENDED | 원2계정/원호스트 복구와 양측 재개 동의→RUNNING; 보관 기한 만료→INTERRUPTED |
| RESULT_PENDING | 서버 검증 적격→FINALIZED; 증거 충돌/미확인→REVIEW_PENDING |
| REVIEW_PENDING | 원자료 재조회/누락 복구로 검증되면FINALIZED; 부족/충돌 지속이면 미확정 중단 결과로 종결, 자동 승리 없음 |
| FINALIZED/INTERRUPTED | 원결과 조회만, 늦은 입력/교체/보상 재발급 불가 |

- 호스트 또는 상대 어느 한쪽이 끊겨도 같은 정지/복구를 적용한다. 자동공격/적 공격·회복·CD·투사체·논리시간을 함께 정지하고 잃어버린 시간을 몰아 계산하지 않는다. 감지 지연 동안 생긴 미확인 꼬리는 마지막 공동 checkpoint로 복구한다.
- 서버가 단절 상태와 최초 감지시각을 소유한다. 60초를 재시도/새 연결로 초기화하지 않는다. 호스트 단절 때 상대가 새 호스트로 자체 승격하지 않는다. 양쪽 모두 끊겨도 서버의60초/보관 타이머는 진행한다.
- **보관은 run의 최초 SUSPENDED 시각부터24시간**을 제안한다. 재개/재단절로 이 절대기한을 뒤로 미루지 않는다. 기한이 지난 뒤 새 단절로 보관해야 하면 즉시 중단; 정상 RUNNING 전투를 해당 기한만으로 강제 종료하지 않는다. 초기 활성run 충돌/성장 차단은 기존 정책을 유지한다.
- 단절 때문에 일반 적에게 패배한 것으로 기록하지 않는다. INTERRUPTED는 미완주 중단이며 스토리 승리/첫클리어·작전 승리 보상0. 이미 확정된 계정 거래/기존 실적은 취소하지 않는다. 입장비 자동 환급이나 새 패널티는 주지 않는 제안이며, 서버 장애 보상은 기존 운영 절차와 별도다. 기존 증분 실적을 이 중단 정책으로 소급 회수하지 않는다.
- 자발적 포기는 별도 의사표시다. 종료 요청 경합은 서버의 원terminal 결과로 처리하며 강제 단절을 포기로 위장하지 않는다. 복구중 양쪽 화면에는 ‘연결 복구 중’과 남은60초, 보관 후 ‘작전 이어하기’와 원만료를 표시한다. 상대 복귀를 무한 로딩으로 기다리게 하지 않는다.
- 기존 증강/광고/정비 선택 기한은 서버가 원 grant 정책대로 소유한다. 전투가 멈췄다고 서버 현실시간 기한을 새로60초/24시간으로 늘리지 않는다. 단절 중 확정된 선택·광고 영수증은 복구 때 순서대로 반영하며 자동선택 여부는 각 grant의 최신 승인 정책을 따른다.

### 6. 재연결과 서버 거래를 함께 복원

같은 원호스트라도 새 실행 연결을 받을 때 서버가 hostEpoch를 증가시켜 옛 연결을 차단한다. 이는 호스트 계정 교체가 아니다. 새 epoch에 선택된 단 하나의 공동 내구성 checkpoint만 연결하고 원5초 단위 seed 재시작 대신 전체 PRNG/효과/예약 상태를 복원한다. 기기 교체는 같은 계정이라도 필수 원버전/복구 지원을 통과해야 하며 원계정 여부만으로 생략하지 않는다.

거래 장벽: 호스트는 전투를 안전한 원정지 지점에 두고 거래 직전 checkpoint를 공동 저장한다. 서버가 원revision/재화/재고·명령ID로 구매/선택을 확정한 뒤 호스트가 원receipt를 소비해 효과를 적용한다. 거래 이후 checkpoint 저장 전 끊기면 거래 전 checkpoint와 서버의 그 이후 확정receipt를 순서대로 재생한다. 재화는 다시 차감하지 않으며 적용 cursor로 효과도 한 번만 적용한다. 복원 불가능한 효과나 누락receipt가 있으면 진행 차단/복구 대상으로 남기고 구매를 취소한 척하지 않는다.

현재HP가 필요한 수리 구매는 서버가 호스트의 HP를 ‘실전 정당성 검증 완료’로 신뢰하지 않는다. 공동 checkpoint를 보고 기준으로 사용하고 형식·원버전 maxHP/범위·재고/비용/중복·보고상 실효과 가능성을 검사해 거래한다. 정상적인 게임 진행으로 해당HP가 만들어졌다는 보장은 없으며 그 한계를 결과 검증에 명시한다. 무효 효과 판매 금지의 경제 규칙과 완전한 부정행위 방지를 혼동하지 않는다.

### 7. 결과 검증의 범위와 보류

일반 협동의 추천 검증 수준은 **서버 기록 기반 정합성 검사**다. 전체 전투를 서버가 재시뮬레이션하는 구조를 기본으로 추가하지 않는다. 서버는 원START/참가자/호스트 epoch·지원버전, 서버 승인 경로/선택·거래/재화, 단조 증가하는 checkpoint/논리시간·합법 파형/웨이브 전이, 보고값 범위·종료 이유, 최초권리/보상계산·중복을 검사한다. 웨이브/HP의 값이 범위 안이라는 사실을 실제 처치/피격의 독립 증명이라고 표시하지 않는다.

- 시간 이상 검사는 서버가 관측한 실행 구간 및 승인 배속/hold와 비교한다. 네트워크 도착 간격을 모든 타격의 실제 시간으로 간주하지 않는다. 허용 오차·속도 상한은 지원 모드 계약에서 수치화하고 없으면 검증 완료로 발행하지 않는다.
- 호스트는 최종 상태/사건 cursor를 제출하고 상대는 같은 최종 상태를 받았다는 ACK를 보낸다. 정상 때10초까지 ACK/검증을 기다리고 그 뒤 결과 보관 화면으로 이동, 서버 처리는 같은 원요청으로 유지한다. UI 대기10초가 승리 판정/결과 만료시각은 아니다.
- 원최종보고를 서버가 받은 시각부터24시간을 결과 증거 복구 기한으로 제안한다. 양측 일치·경제/진행 검사 통과면 확정, ACK 누락/충돌은 REVIEW_PENDING. 상대 ACK만으로 보상을 산출하지 않으며 상대가 거부하거나 돌아오지 않으면 정상 플레이도 확정이 지연될 수 있다.
- 기한까지 원증거가 복구되지 않으면 미확정 중단 결과로 종결하고 신규 승리/클리어/첫보상은 발급하지 않는 제안이다. 제출된 보고를 이미 지급된 것으로 표시하지 않는다. 이 정책의 대가는 상대 이탈/거부로 정당한 플레이어도 보상을 받지 못할 수 있다는 점이며, 승인 때 함께 선택할 사항이다. 향후 일방 보고 수용 정책은 별도 부정행위/보상 허용 위험 결정이 필요하다.
- 서버가 이미 FINALIZED했다면 상대의 늦은 단절/상이 보고로 원결과를 자동 취소하지 않는다. 보상 수령/재조회는 기존 권리와 멱등 정책, 별도 신규 수령버튼 없음. 서버 점검/장애의 운영 정정은 근거를 보존한 기존 절차로 처리한다.
- 양쪽 공모/변조 클라이언트의 그럴듯한 피해·HP 보고는 이 방식으로 완전 검출하지 못한다. hash는 전송/상태 일치 용도이며 부정행위 방지 증명이 아니다. 월드보스 점수/랭킹에는 이 검증 수준을 적용하지 않는다.

### 8. 필수 인수 사례

1. 열차 역할이 상대여도 적격 요원 역할 계정이 호스트 가능, 기기/경로 미지원 상태에서는 비용 차감 START0.
2. 동일 입력100회 재전송→행동1회; 같은ID/다른payload 거절; 호스트 자기 입력도 같은 경로.
3. 차분의 기준 revision 누락→전체복구, 누락 피해 추측 적용0.
4. 5초 checkpoint 지연→원전투 정지; 장기 단절 복구에서 마지막 공동 상태 이후 최대5초 롤백, 원경제 거래는 재차감0.
5. 호스트·상대 각각 단절→양쪽 전투 정지,60초 재시도 reset0; 양쪽 동시 단절도 동일.
6. 같은 계정 재접속 후 이전 epoch 입력/결과 거절. 예전 호스트 프로세스가 돌아와도 두 실행 소유자0.
7. 최초 보관09:00,12:00재개/13:00재단절→만료 다음날09:00 유지. 24시간으로 전투시간/쿨다운 몰아처리0.
8. 구매 확정 후 적용 전 단절→원receipt 한번 재생; 적용 후 저장 전 단절도 거래 전 checkpoint부터 복구해 효과1회.
9. 단절 중 서버 grant 확정→원선택 소비, 다른 후보/재추첨/기한 초기화0.
10. 상대 최종ACK 누락→10초 뒤 보관 표시/검증 지속,24시간 내 같은 원결과 복구 시1회 확정; 미복구 시승리권리0.
11. 서버 FINALIZED 뒤 늦은 보고/계정전환/종료 재요청→원결과·지급1회, 타계정 결과 노출0.
12. 범위 밖 HP·다른 정의/호스트·허용 안 된 경로/재화·시간 진행 거절. 그럴듯한 공모보고는 완전 검출됐다고 주장하지 않음.
13. 월드보스 솔로/협동은 기존 서버시간/HP/180초 규칙 유지; 일반 host/pause/보관/ACK정산 정책 유입0.
14. 동일 전투에서 초당 메시지/byte·checkpoint 저장 횟수/크기·서버 실행시간·호스트 FPS/발열·복구 지연을 비교.10Hz/5초가 서버/클라이언트 양쪽에 적합한지 실측 후 조정, 단순 CPU 감소를 비용 절감 확정으로 발표하지 않음.

### 9. 승인 범위·작업 연결

이번 결정 대상은 A안(자동이관 제외),5초 선정표본/20ms 동률,10Hz 차분/유휴1초/단절3초/공동 checkpoint5초와 진행 상한,60초 재접속→최초 보관24시간,중단 보상/환급 방침,거래 장벽/복원,양측 최종수신 확인·10초 UI대기/24시간 결과복구 및 검증 한계다. 승인 전에는 S-12의 조사/계약 초안 참고이며 신규 정책 구현·운영 발행의 확정 기준이 아니다. 승인 후 S-12가 실제 DTO/전송 수단/한도·정밀도·시간 오차/지원버전을 확정하고 D-09→U-11→P-12로 인수한다. 기존 스킬·스토리·경제 미결을 묵시 승인하지 않는다.
<a id="ordinary-coop-survivor-clear-v03"></a>
## 일반 협동 잔류자 완주·결과 대리 제출 v03 — 2026-09-22 / 사용자 확정

**승인 범위:** 사용자 ‘추천대로 해주고 … 전투 도중 한 명이 끊긴 뒤, 남은 사람이 혼자 계속 싸워 클리어하는 것 … 남은사람만 클리어 보상’에 따른다. (1) 상대의 최종 수신 ACK는 클리어 확정 필수조건이 아니다. (2) 원호스트가 사라져도 다른 원참가자가 전달받은 완전한 최종 기록을 제출하고 서버 검증을 통과하면 확정한다. (3) 전투 도중 이탈 뒤 잔류자 혼자 이어서 승리하면 잔류자만 클리어 보상을 받는다. 월드보스 솔로·협동에는 적용하지 않는다.

이 절이 v02의 ‘전투 중 자동이관 제외/원2명 복귀 필수/1인 완주 금지/최종 양측ACK 필수’를 대체한다. v02의60초·24시간·전송/선정 수치·환급/미확정 중단 정책까지 일괄 승인된 것은 아니다. 상세 구현 지원 전 신규 일반 협동을 지원 완료로 표시하지 않는다.

### 1. 보상 결정표

| 실제 진행·검증된 결과 | 클리어 보상 |
|---|---|
| A/B가 함께 전투를 끝냈고 결과 전달/화면 표시 중 B 단절 | 원래 적격한 A/B 각각 보상 권리 저장. B ACK 부재로 A 또는 B를 탈락시키지 않음 |
| 호스트 A가 함께 달성한 클리어 기록을 B에게 전달한 뒤 단절, B가 원최종기록 대리 제출 | 서버 검증 통과 시 같은 원결과로 A/B 각각 적격 권리 저장. 제출자 B만 승리한 것으로 바꾸지 않음 |
| 전투 도중 B 이탈, A 혼자 계속 전투해 승리 | A의 원래 개인 클리어 보상만. B 클리어 보상0, B 몫을 A에게 합산 지급하지 않음 |
| 전투 도중 호스트 A 이탈, B가 서버 승인 호스트 이관 후 혼자 승리 | B의 원래 개인 클리어 보상만. A 클리어 보상0 |
| 단절 후 완전한 최종기록도 복원 가능한 진행 상태도 없음 | 복구/검토 대기. 단순 승리 주장·상대 ACK 거부/누락만으로 지급하거나 패배 확정하지 않음 |
| 서버에서 이미 결과/보상 권리가 확정된 뒤 재접속·단절·중복 제출 | 확정된 원결과 유지. 재지급/상대 몫 이전/자동 회수0 |

클리어 보상은 수령 버튼을 누른 사람/최종보고 제출자가 아니라 서버가 확인한 해당 run의 완주 자격을 기준으로 한다. 기존 개인 보상식·첫권리·소유 자산은 유지한다. 실제 지급 시 중복 방지 키는 원run+계정+기존 보상권리 단위이며 같은 결과를 두 명이 제출해도 두 번 지급하지 않는다. 이탈자의 기존 확정 거래·이전에 얻은 별도 권리는 삭제하지 않는다. 신규 중도참여자·보상 대리수령·2인분 보상은 만들지 않는다.

### 2. 순간 단절과 잔류 완주 구분

전투 중 순간 연결 끊김과 확정된 이탈 뒤1인 진행을 구분하는 서버 참여 상태가 필요하다. 클라이언트가 ‘상대가 나갔다’고 보고했다는 이유만으로 상대 자격을 제거하지 않는다. 원연결·참가자·호스트 세대와 실제 서버 관측/복구 상태를 확인한다. 단절 감지/복귀 유예의 초수는 v02 시험 후보이며 이번 승인으로 확정하지 않는다.

**후속 세부 제안:** 단절 감지 뒤 복구 상태에서 잔류자에게 기존 화면의 ‘계속 진행’을 제공하고, 서버가 이탈/잔류 상태와 원checkpoint를 확정하면1인 진행으로 전환한다. 전환 전 원상 복귀는2인 유지. 전환 후 그 run은 잔류자 완주 경로로 고정하고 이탈자의 재참여/막판 복귀 보상 복원은 허용하지 않는 안을 추천한다. 강퇴 버튼으로 정상 상대를 탈락시키는 경로는 두지 않는다. 이 버튼/재참여 금지 세부안은 사용자 핵심 보상 결정을 구현하기 위한 후보이며 별도 확정이 필요하다.

스토리 클리어·첫권리·퀘스트/패스 실적도 단순히 참가자2명에게 복제하면 보상 정책과 어긋날 수 있다. **추천:** 잔류 완주자는 기존 적격성을 통과한 승리/사건 실적만 인정, 이탈자에게 후속 승리로 새 스토리 클리어/승리 실적을 주지 않고 이탈 전에 이미 확정된 실적은 유지. 구35의 일반 종료/웨이브·증분 보호 규칙을 손실 없이 대조한 뒤 S-12 계약에 반영한다. 이 승리 외 실적 범위는 이번 ‘클리어 보상’ 승인을 자동 확장하지 않는다.

### 3. 호스트 이탈 뒤에도 이어갈 수 있는 실행 계약

- 상대만 이탈하면 원호스트가 그대로 전투를 소유한다. 원호스트가 이탈하면 서버가 잔류 원참가자를 새 호스트로 지정해야 한다. 잔류자의 임의 자기 승격은 금지한다. 따라서 v02의 자동이관 제외는 새 정책의 구현 제한으로 사용할 수 없다.
- 이관은 전투를 잠시 정지한 상태에서 서버가 단일 원run의 hostEpoch·원호스트/새호스트·참가상태·복구checkpoint·거래 cursor를 원자 확정하는 절차다. 같은 이관 요청의 재전송은 원결과이며 두 호스트를 병렬 승인하지 않는다. 기존 월드 서버run을 클라이언트run으로 바꾸는 작업이 아니다.
- 새호스트는 서버가 지정한 완전한 checkpoint와 그 이후 확정된 서버 거래receipt를 복원한다. HP/상태층·예약/투사체·효과tick·쿨다운/충전·PRNG/논리시간을 이어받으며 무료 완치·충전 초기화·새seed/장비 재계산은 없다. 미확인 꼬리 상태의 무손실 복원을 약속하지 않고 실제 지원 rollback 범위를 계약에 기록한다.
- 새 호스트 복구 성공을 확인한 후에만1인 전투를 재개한다. 원호스트가 복귀해도 구epoch 입력/상태 갱신으로 새분기를 덮지 않는다. 전환 후 양쪽이 각각 로컬 승리를 만들어 두 결과를 제출해도 서버의 한 원run 계보로 검증·종결한다.
- 1인 진행에서는 checkpoint/경계 통과를 이탈자의 ACK에 계속 묶지 않는다. 현재 적격 호스트의 상태와 서버 저장 확인으로 복구 경계를 유지하고 ‘2인 공동 확인’ 상태로 거짓 표시하지 않는다. 최종 결과도 동일하다. 잔류자까지 끊기거나 서버와 통신할 수 없으면 서버 이관 승인 없이 계속 진행하지 않고 복구 상태로 돌아간다.
- 새 호스트가 필요 상태를 직렬화/복원할 수 없는 프로필은 미지원으로 남긴다. 연결 전투/경제/결과가 검증될 때까지 호스트 이탈 대응 전체 완료로 처리하지 않는다.

### 4. 이탈한 역할·공용 선택의 처리 — 구현 전에 확정할 후보

잔류자의 계정에 상대 요원/장비를 지급하거나 자기 스탯으로 덮어쓰지 않는다. 원START의 열차·요원·효과 구성은 유지하며 기존 자동 기본공격/자동 액티브는 원규칙대로 계속 돌리는 안을 추천한다. 기존 승인 없는 신규 AI가 상대 수동궁극기를 쓰거나 재화를 소비하게 하지 않는다.

1인 진행을 가능하게 하려면 원래 이탈자가 담당하던 필수 경로/공용 증강 선택을 해결해야 한다. 추천은 서버가 해당 run의 공용 진행 선택권만 잔류자에게 이관하고, 카드/리롤 잔여·원grant/기한·정비 재고를 그대로 이어받는 것이다. 상대 계정의 영구 재화·광고권/유료승인·인벤토리 거래 권한은 이관하지 않는다. 이탈자의 진행 중 광고 거래는 원계정/원session으로만 종결하고 잔류자에게 새 무료횟수를 만들지 않는다.

상대 역할의 수동 전투 조작을 잔류자에게 함께 제공할지, 기존 자동행동만 둘지는 입력/난이도 정책으로 별도 확정한다. 필수 양역할 조작 없이는 통과 불가능한 일반 콘텐츠는 이 처리가 정해지기 전1인 계속 진행 지원으로 발행하지 않는다. 이 미결을 이유로 이미 확정된 ‘잔류자만 보상’을 양측 보상/양측 지급 차단으로 되돌리지 않는다.

### 5. 최종기록 대리 제출·종결 경합

ACK는 전달 확인/진단 증거이며 상대가 결과를 승인하거나 거부하는 투표가 아니다. 원호스트 최종기록은 수신한 다른 원참가자가 서버로 전달할 수 있다. 서버는 제출자 인증/원참가자 여부와 기록의 run·원호스트 epoch·사건순번·최종상태·정의/원START·거래 cursor 및 진행/결과 정합성을 검증한다. 단순 화면 캡처·승리 bool·타계정 주장으로 대체하지 않는다. 기록 서명/인증전송 등 출처 검증 방식은 S-12가 실제 계약으로 정의하며 출처 확인이 실제 전투의 정당성을 완전 증명한다고 주장하지 않는다.

전투 종료와 이탈/이관 요청이 경합하면 서버가 원run을 한 번만 종결한다. 이미 수신된 완전한 종료기록은 이관 확정 전에 검증하며, 이관 전에 함께 클리어했는지 또는1인 계속 진행으로 새로 클리어했는지 원사건/참여계보를 구분한다. 네트워크 도착 순서나 클라이언트 wall clock 하나만으로 보상 자격을 바꾸지 않는다. 이관 이후 지연 도착한 구epoch 최종기록은 새 호스트의 상태로 적용하지 않고 결과 증거로만 조회·검토하며, 이미 확정된 결과를 덮거나 두 번째 지급하지 않는다. 계보를 확정할 수 없으면 해당 결과의 검증 대기로 두며 ACK를 대신 요구해 차단하지 않는다.

서버 검증을 통과하면 상대 재접속을 기다리지 않고 적격 계정별 권리를 확정한다. 완전한 기록 자체가 없거나 실제 충돌이 있는 경우에만 복구/검토 상태를 사용한다. 따라서 v02의 ‘상대ACK 없음→24시간 뒤 무보상’ 사례는 폐기한다. 자료 부족의 보관/종결 기한·자동환급은 별도 미결이며 이번 승인에 포함하지 않는다. 일반 협동의 제한된 정합성 검사·공모보고 검출 한계는 유지하며 월드 점수/랭킹에는 적용하지 않는다.

### 6. 수용 사례·담당 영향

1. 함께 승리→B 결과화면 전에 단절→A 보고 검증 통과: A/B 각각 원래 적격 보상 권리, B ACK 없어도 지급 차단0.
2. A호스트가 전달한 최종기록을 B 대리 제출: 보고주체만 바뀌고 보상수혜자/금액은 원자격 유지.
3. B 전투 중 이탈→A 잔류 완주: A 개인 클리어 보상1회/B0, A에게2인분 지급0.
4. A호스트 이탈→서버 이관·B 복구/완주: B1회/A0, 구호스트 재접속 시 제2승리/보상0.
5. 새호스트의 checkpoint/경계/최종결과에 옛상대 ACK 요구0. 단계 전환·거래는 서버 원권한 유지.
6. 이탈 선언 위조/다른 계정/잘못된 epoch→이관/보상 자격변경 거절. 손상/누락 checkpoint→복구 대기, 무료 새전투 생성0.
7. 구매 확정 직후 이관→원receipt/재화/적용효과1회, 리롤 잔여/광고권 재발급0.
8. 완주와 이관 동시/지연종료기록→원계보 단일 종결, 검증 불가면 검토; 전달 지연을 단독 근거로 보상 탈락시키지 않음.
9. 이미 확정된2인 승리 후 이탈→권리 회수0; 이미 확정된1인 승리 후 재접속→원결과 조회만.
10. 월드보스 솔로·협동은 기존 서버 판정·참여/보상 정책 유지; 일반 잔류 보상 규칙 유입0.

S-12는 현재 진행 중인 동일 작업에서 이관/참여자별 보상자격·최종기록 대리 제출 계약을 보완한다. D-09는 이관중/계속진행·개인별 결과 표시, U-11은 복구/단일호스트/공용선택·실제2클라이언트, P-12는 위10사례와 정상2인/월드 비회귀를 인수한다. 별도 중복 큐를 만들지 않는다. 신규 세부 후보(유예/계속진행 입력/재참여·실적·역할 대행)는 확정 상태를 구분해 남긴다.
<a id="ordinary-coop-rejoin-controls-v04"></a>
## 일반 협동 재접속·잔류 조작·공용 선택 v04 — 2026-09-22 / 상세 제안

**2026-09-22 사용자 승인 다음 ㄱㄱ로 v04 세부안 승인.** 60초복구/명시1인전환·자동전환없음,재참여제한·양역할조작·공용선택/개인권한 및 §5 실적범위를 포함한다. 아래 작성 당시 후보/별도승인/승인전 문구는 이 범위에서 대체한다. 기술시험수치·보관/자료부족종결은 별도다. 사용자 확정인 v03(전투 중 이탈 후 잔류 완주자는 자기 보상만,최종ACK 필수 없음,호스트 이탈에도 이어가기)을 구현 가능한 흐름으로 상세화한다. 이번 다음 ㄱㄱ는 아래 새 유예/재참여·조작권/실적 정책의 작성 지시다. 월드보스 솔로·협동은 서버 판정과 기존 참여/보상 유지.

### 1. 추천 흐름 — 짧은 복구와1인 전환을 분리

- 통신 단절을 서버가 확인하면 전투 논리시간/행동을 잠시 정지한다. 화면에는 ‘상대 연결을 복구하고 있습니다’와 남은 복구시간, ‘혼자 계속하기’를 표시한다. 추천 유예는 최초 확인부터60초이며 반복 재시도/소켓 교체로 초기화하지 않는다. 클라이언트의 상대 미응답 주장만으로 버튼을 활성화하지 않는다.
- 원참가자가60초 안에 복귀하고 원상태 동기화까지 끝났으며1인 전환이 아직 확정되지 않았으면2인 진행을 재개한다. 단순 로그인/소켓 연결만으로 복구완료로 세지 않는다. 짧은 연결 오류만으로 클리어 자격을 잃지 않는다.
- 잔류자는 유예 중에도 ‘혼자 계속하기’를 선택할 수 있다. 버튼 근처에 ‘전환 후 상대는 이 작전에 다시 참여할 수 없습니다’를 한 번 표시하고 별도 재확인 팝업은 추가하지 않는다. 사용자 명령과 서버의 상대 단절 확인·원checkpoint 확보가 모두 있어야 전환한다.
-60초가 지나도 자동으로1인 전환하거나 상대를 영구 탈락시키지 않는다. 잔류자 선택을 기다리는 정지 상태/보관 화면으로 이동한다. ‘혼자 계속하기’ 또는 ‘나중에 이어하기’를 제공한다. 양쪽 모두 사라지면 자동 전투/AI 완주/자동 승리 없이 복구 보관이다. 보관 기한·최종 중단/환급은 v02 후보24시간을 아직 확정하지 않고 별도 잔여로 유지한다.
-1인 전환 실패/응답 유실은 원명령 상태를 조회한다. 서버 성공 전 로컬에서 상대 역할을 열거나 상대 보상자격을 지우지 않는다. 이미 전환 성공했으면 원결과로 복귀하며 두 번째 요청으로 재화/효과를 중복 적용하지 않는다.

### 2. 재접속과 전환의 경합

서버의 한 참여 revision에서 RECONNECTING→COOP_ACTIVE 또는 RECONNECTING→SOLO_CONTINUING 중 하나만 확정한다. 원참가자의 복구완료가 먼저 확정됐으면 오래된 혼자계속 명령은 거절/최신2인 상태 응답.1인 전환이 먼저 확정됐으면 늦은 복구는 참가 복원 대신 원run 이탈 상태 조회만 허용한다. 클릭 시각/클라이언트 시간으로 우선순위를 재판정하지 않는다.

**추천:** SOLO_CONTINUING 전환은 그 run에서 되돌리지 않는다. 이탈한 원참가자도 전투 재진입·관전 제어·막판 보상자격 복원은 불가,새 사람 중도합류도 없다. 잔류자 자신이 끊겼다 돌아오는 것은 같은1인 진행의 복구로 허용하며 이탈했던 상대가 대신 호스트가 되어 보상을 가져갈 수 없다. 새 협동은 이 run의 기존 활성작전 규칙을 정리한 뒤 별도 출발한다.

단절만으로 이탈 계정에 추가 벌점/재화 차감/장비 손실을 만들지 않는다.1인 전환 전 이미 함께 달성한 클리어 기록이 확인되면 v03의 공동클리어 판정이 우선하며 단절 화면을 봤다는 이유로 권리를 회수하지 않는다. 이관/종료의 증거가 충돌하면 검토 상태로 분리한다.

### 3. 이탈한 역할의 전투 운용 — 원편성 유지·잔류자가 기존 솔로 조작 사용

추천은 상대 전투 개체를 삭제하거나 별도 신형 AI를 만드는 대신, **원START의 열차/요원 편성을 유지하고 잔류자에게 해당 run의 양역할 조작만 허용**하는 방식이다. 원기본공격·자동 액티브·승인된 궁극기 AUTO 설정은 그대로,수동궁극기/열차 지원/집중 대상/레버 등은 기존 솔로 컨트롤을 재사용한다.1인 전환 자체가 무료궁극기나 강제AUTO 변경을 발생시키지 않는다.

- 열차 담당이 남아도 기존 편성 요원 조작을,요원 담당이 남아도 기존 열차 조작을 사용할 수 있다. 실제 화면은 기존 솔로 HUD/역할 패널의 조합을 재사용한다. 소유자 이름·자산 귀속은 원계정이며 새 능력/유료구매 버튼은 만들지 않는다.
- 서버는 입력권한만 ‘원역할’에서 ‘해당run의 잔류 조작권’으로 바꾼다. 원효과 actor/sourceOwner·스탯/장비/패시브·공유풀/필터는 원START 그대로다. 원출처를 잔류계정으로 바꾸어 세트옵션이나 Lumen표식·지휘효과를 중복 적용하지 않는다. 실제 사용 개체 범위는 원run의 효과명세 그대로다.
- 영구 보유/편성/레벨·장비·프리셋을 상대 계정 대신 변경할 수 없다. 상대자산을 복사하거나 자기 계정에 지급하지 않는다. 원궁극기 충전/CD·HP/상태/배치·진행 난이도는 유지,적HP/웨이브/보상을1인이라는 이유로 자동 낮추거나 늘리지 않는다.
- 전환 중 수락된 미발사/발사후 행동은 원행동큐 상태와 복구 사건대로 처리한다. 손가락을 누르고 있던 입력/미확정 명령은 새ID로 재실행하지 않는다. 패널 재바인딩은 구리스너를 해제하고 한 입력이 한 명령으로만 연결되게 한다.
- 두 플레이어의 동시 물리 조작이 필수인 일반 콘텐츠는 기존 솔로 입력으로 완주 가능한지 프로필별 검증한다. 불가능한 프로필을 지원 가능으로 발행하지 않으며 전환 버튼을 뒤늦게 막는 대신 일반 협동 지원 발행 단계에서1인 복구 적격성까지 검사한다. 월드보스 양역할 기믹에 이 조작권 정책을 확대하지 않는다.

### 4. 공용 진행 선택과 구매/광고

| 기능 |1인 전환 후 담당/소비 |
|---|---|
| 경로 확정·스토리 노드 진행 | 잔류자에게 원run 선택권,같은 그래프/node/선택revision 사용. 새경로 추첨/되돌리기 없음 |
| 공용 증강 후보 선택·무료리롤 | 잔류자가 현재/이후 grant 선택. 기존 후보/슬롯별 FREE3+EXTRA1 잔여 유지,교대 담당 규칙은1인 동안 같은 잔류자로 고정 |
| 공용 작전물자 정비 | 잔류자가 기존 run물자/재고/대상 적격성으로 거래. 이탈자 개인재화 차감 권한 없음 |
| 장비/판매·개인 인벤토리 | 기존 원계정 소유/판매 가능 조건 유지. 전투 조작권 이관을 상대 장비 판매·영구 이전 권한으로 사용하지 않음 |
| 광고/유료승인 | 각 실제 시청/결제 계정의 권리와 원session 유지. 이탈자 승인/토큰 재사용0,새 무료횟수 지급0 |

공용 grant의 담당 이관은 서버 원자 거래다. 기존 AD_PENDING/구매/리롤 처리가 있으면 먼저 원거래 상태를 조회한다. 이탈자 광고 검증이 원유효기간 안에 성공하면 원grant에 한번 반영하고 잔류자가 같은 슬롯을 또 무료로 얻지 않는다. 이미 종결/만료된 원session의 늦은 성공은 기존 광고정책대로 처리하며 상태를 되살리지 않는다.

추천은 원pending이 종결된 뒤 잔류자가 자기 계정으로 같은 공용 슬롯의 남은 EXTRA1을 승인받을 수 있게 하는 것이다. 이는 남은 공용 횟수의 소비이며 계정 교체로 횟수·누적 대기예산을 새로 발급하지 않는다. 이 사용자의 광고/결제 적격성은 기존 서비스가 다시 검사한다. 광고를 보거나 유료승인을 받는 동작은 명시 입력이 필요하며 혼자 계속하기가 대신 승인하지 않는다.

공용 선택의 서버 기한/자동선택·광고보류는 각 최신 승인 정책을 유지한다. 담당 이관만으로45초/광고예산 등을 새로 시작하지 않는다. 원정책에서 자동선택이 허용되지 않은 grant를 이번 문서만으로 자동확정하지 않는다. 원거래 결과 적용 후 checkpoint를 다시 저장하고 전투 재개한다.

### 5. 클리어 외 진행 기록 — 신규 추천 범위

사용자가 확정한 클리어 보상은 v03을 따른다. 그와 연결되는 스토리/퀘스트/패스는 다음을 추천하되 별도 승인 대상으로 구분한다.

-1인 완주자의 승리·완료웨이브 실적은 원run에1회만 계산,협동+솔로 두 작전으로 분리하지 않는다. 이관 횟수/역할 추가로XP/패스실적을 복제하지 않는다.
- 이탈자에게 이후 잔류자의 승리를 근거로 새 승리·작전종료/웨이브 실적을 발행하지 않는다. 이미 확정된 구증분실적/이탈 전 원계정 거래는 보존한다. 이탈을 정상패배로 재분류해 별도 종료보상을 만들지 않는다.
- 스토리는 해당 계정의 사건 적격/잠정성공과 같은 작전 최종승리가 모두 있어야 한다. 상대가 가진 진행도·잠정성공을 잔류자의 계정 것으로 복사하지 않는다. 이탈자가 앞서 사건을 성공했더라도 그 뒤 이탈한 작전의 잔류승리로 자기 첫클리어/첫보상을 확정하지 않는 안이다.
- 함께 클리어 후 결과 전달 중 단절은 위의 중도이탈 배제가 아니다. 각 원계정의 적격 진행/보상·조회 권리는 유지하고 최종ACK 누락만으로 실적을 막지 않는다.

### 6. 실제 사용자 흐름·인수 기준

1.2인 전투→상대 단절 확인→전투 정지/남은 복구시간 표시.60초 안에 복구완료→원상태2인 재개,보상자격 유지.
2. 잔류자가 ‘혼자 계속하기’→서버 참가상태/호스트세대 확정→원checkpoint·거래 복원→기존 솔로 조작 표시→1인 진행. 이탈자 계정/역할에 따라 보상이 뒤바뀌지 않음.
3. 전환 명령 응답 유실→원결과 조회;전투 패널을2번 만들거나 같은 명령2회 실행하지 않음.
4. 복구완료와1인전환 동시→서버 단일revision 승자만 적용. 복귀한 정상 상대를 오래된 버튼으로 탈락시키지 않음.
5.1인전환 후 이탈자 복귀→참여불가 상태/원결과 조회,잔류자 호스트 탈취·막판보상복원0. 잔류자 본인 복귀는 같은1인 상태 복원.
6. 궁극기 충전80/100과남은CD4초인 상태로 전환→80/100·4초 그대로. 전투정지 시간으로충전/치유 증가0.
7. 같은 슬롯 무료리롤2회사용·EXTRA1미사용 후 이관→무료1회/추가1회 남음. 이탈자 광고pending성공→추가0회,잔류자 새무료권0.
8. 자기 조작권으로 상대 자산 판매/개인재화 차감 시도→서버거절. 원run의 합법적 공용물자 수리는 기존 거래한번.
9.1인전환 전스토리성공 원계정A·잔류계정B가자격없음→B보통클리어보상만,스토리 첫권리 복사0. 기존 승인 획득자격이 있는B의사건만 별도판정.
10.60초 만료→자동1인/자동포기/자동승리0. 양쪽 단절/서버미연결→보관/복구,미승인환급0.
11. 일반 프로필 전수지원 범위에서 기존 솔로 조작으로 경로·선택·정비·전투종료까지 통과 가능. 필수2인동시조작 프로필을 조용히 우회하지 않음.
12. 월드보스2인/솔로 서버판정·참여보상 및 기존 일반솔로에 v04 참가이관/1인보상 필터가 잘못 적용되지 않음.

### 7. 승인 대상·후속

추천 확정 대상:60초 재접속 유예와 유예중 명시1인전환/시간만료 자동전환 없음,1인전환 뒤 원이탈자 재참여 금지,원편성·난이도 유지/잔류자의 기존 솔로 양역할 조작,공용선택/물자만 위임·원계정자산/광고권 보호,위 승리 외 실적 범위다. 기술10Hz/5초 checkpoint·보관기한/자료부족 종결·전송수단은 앞선 후보와 실제계약 잔여로 분리한다.

S-12는 서버 참여revision/hostEpoch·run한정 입력권한·grant담당/광고session·개인별실적 필드를 실제 계약으로 제시한다. D-09는 기존 승인미술에서 복구/혼자계속/솔로패널 연결, U-11은 기존 솔로 Controller 재사용·Bind수명/이관복원, P-12는 위12사례를 인수한다. 승인 전 새 세부후보를 코드에 확정 반영하지 않고 독립 계약조사는 이어갈 수 있다.
<a id="ordinary-coop-retention-validation-v05"></a>

**2026-09-28 사용자 ‘확정’ 승인:** v05 보관24시간/자료부족 결과복구·검토/점유해제·거래보호와 통신·저장 첫시험값을 채택한다. 아래 및9월28일 결정요약의 승인대기는 이 범위에서 대체. 실제전송수단/호스트선정·한도/오차의 담당계약과 실측은 별도이며 v03/v04 원권리 유지.
## 일반 협동 보관·미확인 결과·통신 시험 기준 v05 — 2026-09-22 / 상세 제안

**신규 정책·시험 수치 승인 대기.** 직전 v04 승인으로60초 복구/명시1인전환/재참여 제한/기존솔로 양역할 조작·공용선택/승리외 실적이 확정됐다. 이 절은 아직 미결인 보관·자료부족 종결과 통신 시험값만 제안한다. v03의 최종ACK 불필수·검증된 원결과 대리제출,월드 서버판정은 유지한다.

### 1. 작전 이어하기는 최초 보관부터24시간

추천: 서버가 처음 SUSPENDED(나중에 이어하기/양쪽 부재 등 실제 보관)를 확정한 시각+24시간을 해당 run의 재개 기한으로 고정한다. 최초3초 단절 의심이나60초복구 시작 자체를 보관 시작으로 세지 않는다. 재접속/상태조회/반복보관으로 기한을 갱신하지 않는다. 기한은 서버시각의 반개구간으로 판정하며 정확 만료시각에는 새 재개를 허용하지 않는다.

기한 전에 재개가 확정되어 실제 진행 중인 전투는 보관 만료시각만으로 끊지 않는다. 이후 다시 보관해야 하는데 최초 기한이 이미 지났으면 추가 재개는 종료한다. 보관 중 HP/회복·쿨다운/기믹·전투시간을 현실 경과만큼 진행하지 않으며 서버 경제/광고/선택 기한은 해당 원정책대로 유지한다.

2인 상태로 보관했으면 원참가자 복귀 또는 v04의 서버검증·명시1인전환을 거친다. 이미1인전환한 run은 잔류계정만 복구하며 옛상대를 새 잔류자로 바꾸지 않는다. 모든 재개는 원정의/원START·완전checkpoint/서버거래를 복원하고 입장비를 다시 차감하지 않는다.

만료 시에는 먼저 서버가 보유한 최종보고·미확정 거래를 확인한다. 검증 가능한 완주보고가 있으면 정산으로 전환하고,전투 미완료만 확인되는 run은 INTERRUPTED_EXPIRED로 종결한다. 새 승리/스토리/클리어 보상은 없고 기존 확정 권리·거래는 보존한다. 만료=적에게 패배로 기록하거나 이탈자에게 별도 패널티를 만들지 않는다. 새 자동 입장비 환급은 제안하지 않으며 서버 장애 보상은 기존 운영 정정 절차와 분리한다.

### 2. 전투 재개 기한과 결과 자료 복구 기한은 별개

| 상황 | 처리 |
|---|---|
| 완전한 최종기록과 검증 근거가 있음 | 즉시 원결과 확정,상대ACK/24시간 만료를 기다리지 않음 |
| 상대 ACK만 없음 | 정상 검증 경로 유지,자료부족 사유로 분류하지 않음 |
| 실제 최종기록 누락·충돌 또는 원거래 미확인 | RESULT_REVIEW로 원자료 복구/대조,가짜 승리/패배 없음 |
| 전투 미완료이고 보관24시간 만료 | 재개 종료,새 클리어 보상0;늦은 진짜 최종기록 검증 권한까지 없애지 않음 |

추천: 최초 적법한 최종보고 접수(또는 서버가 최종기록 충돌을 발견한 시각)부터24시간 동안 자동 누락자료 조회/원결과 재조회로 복구를 시도한다. 재전송으로 기한을 늘리지 않는다. 이는 상대의 결과동의를 기다리는 시간이 아니다. 원최종기록을 가진 원참가자의 대리 제출은 동일 run으로 허용한다.

24시간 뒤에도 검증 불가능하면 **‘결과 확인 필요’** 상태로 전환하고 일반 자동복구 루프를 끝낸다. 승리로 지급하지도,기록을 삭제하거나 영구적으로 무보상 확정하지도 않는다. 근거 있는 늦은 원기록 또는 운영 재검증으로 같은 run의 미확정 권리를 나중에 확정할 수 있다. 이미 지급된 결과를 이 흐름으로 다시 지급하거나 자동 취소하지 않는다. 악성/유효하지 않은 요청은 원오류로 거절하고 무기한 새검토건을 만들지 않는다.

증거/원장 실제 보존기간은 기존 데이터 정책을 사용한다.24시간은 자동복구 작업 기한이지 데이터 삭제/개인정보 보관기간 정책이 아니다. 새 고객센터·자동 알림·상시폴링 서비스를 이 기획만으로 신설하지 않는다. 사용자가 결과 화면을 다시 열면 원상태/필요 조치만 조회한다.

### 3. 결과 불확실 때문에 계정을 무기한 묶지 않기

추천은 전투가 더 진행될 수 없는 결과검토 단계로 들어갈 때 원run의 신규 전투입력을 차단하고 활성 전투 점유를 해제하는 것이다. 결과를 다시 열어도 전투를 이어서 할 수 없으며 늦은 제출은 원종료까지의 기록만 받는다. 참가자 권한/hostEpoch를 원자 종료해 결과검토 중 새 공격으로 더 좋은 승리를 만들지 못하게 한다.

완료 여부가 불명확한 구매/리롤/광고·입장 거래는 먼저 원요청 원장을 조회해 해당 재화 예약을 확정/해제해야 한다. 거래 미확인을 무시하고 잔액을 새로 계산하거나 계정 전체를 영구 잠그지 않는다. 해제 가능한 범위와 원거래 자원 잠금 범위를 분리하고 확정 전 같은 예약을 재사용하지 않는다. 단순 결과 보상 미확정은 신규 독립 출격/성장 전체 차단 사유로 만들지 않는 안이다.

출발snapshot은 이미 고정됐으므로 이후 성장/다른 작전이 원검토run의 스탯/첫권리 판단을 소급 바꾸지 않는다. 늦게 검증된 보상은 기존 원계정·원기간·첫권리 원장 기준으로 처리한다. 이전run과새run의 같은 첫권리 경합은 서버 원자 자격검사로1회만 지급하며 ‘늦게 왔으니 추가 한 번’으로 처리하지 않는다. 기존 확정 첫클리어를 취소해 새 권리를 부여하는 이행은 금지한다.

### 4. 통신/저장 비교시험 시작값

첫 시험 후보는 v02와 연결하되 표시는 모드별로 분리한다.

| 항목 | 첫 시험값/계약 |
|---|---|
| 대체 가능한 상태 변경분 | 최대10Hz(100ms 묶음),변경 없으면 전체상태 전송0 |
| 명령/중요 사건 | 즉시 전달·ACK/원ID 재전송;동일ID 다른내용 거절,반복확정0 |
| 유휴 연결 확인 |1초,정상 인증 왕복이 있으면 별도heartbeat 중복0 |
| 단절 의심 |3초 유효응답 없음 또는명시종료;서버가 원멤버십/연결을 확인한 뒤 v04복구 절차 |
| 내구성 checkpoint | 전투 논리시간5초 간격 및 wave/node·서버거래·종료 경계 |
| 저장확인 없이 진행 가능한 상한 | 마지막 서버저장 확인 후 논리시간5초.상한에서정지하고 복구/저장 상태표시 |
| UI 결과대기 |10초 뒤 보관화면으로 이동 가능.처리취소/승리 판정/자료 만료 아님 |

5초 checkpoint는 서버가 전투의 모든 피해를 계산한다는 뜻이 아니다. 호스트가 완전 상태를 보고하고 서버가 버전/참가자/계보·형식/저장revision을 검사·저장한다.2인 상태의 상대 수신 ACK는 진단/공동복구 증거로 보관하되 **서버 저장 완료 자체나 마지막 결과 검증의 무조건 필수조건으로 삼지 않는다.** 상대 상태 차분이 뒤처지면 전체복구를 요구하고,단절이 확인되면 v04를 따른다.1인전환 뒤 이탈자 ACK를 요구하지 않는다. 이 정의가 v02의 일괄 ‘상대/서버 확인 장벽’보다 우선하는 제안이다.

같은 사건의 저장 중복을 막고 변경없는정지상태는 반복 full쓰기하지 않는다. 구매 직전 checkpoint와 원receipt 적용cursor 장벽은 유지한다. 최초5초 이후 저장응답 지연 때문에 매번 멈춘다면 해당 조합은 실기 부적합으로 기록한다. 조용히 상한을 무한대로 바꾸거나 HP만저장해 해결했다고 하지 않는다.

직접연결/서버중계/전문중계 제품은 아직 미선정. 같은2클라이언트·동일전투/시나리오에서 비교하고 실제 메시지크기/프로토콜상한·가변시간 복원·시간오차 허용치를 S-12 계약으로 확정한다. CPU절감·byte절감·DB쓰기 감소·클라프레임 유지가 각각 다른 지표임을 구분한다. 지금운영서비스/비용을 확정하지 않는다.

### 5. 검증 시나리오

1.09:00첫보관→12:00재개/13:00보관→다음날09:00만료.재접속해도 기한연장0.
2. 만료 직전 재개 확정→정상 진행 유지,정확만료시 새재개 거절. 이후새보관은 원기한검사.
3. 보관만료 처리와 유효완주보고 경합→한원terminal계보로 처리,무조건보상0/이중종결금지.
4. 상대ACK없음+완전한 원최종기록→즉시 검증/적격 지급,24시간검토 강제0.
5. 실제기록누락→24시간자동복구후 결과확인필요,기록삭제/영구무보상확정0.나중에 원증거검증→원계정권리1회.
6. 검토진입→전투입력영구차단/활성전투점유해제,조회가전투재개를일으키지않음.미확정거래의원예약은정합화전 재사용0.
7. 이전run결과검토중 새run이같은첫권리확정→늦은결과로두번째첫보상0.원계정/기간을새run으로바꾸지않음.
8. 서버저장확인없는 논리시간5초→정지,저장복구후밀린공격몰아실행0.비저장실시간체크포인트를확정상태로오인하지않음.
9.1인상태서버저장→옛상대ACK없이진행,2인상대지연→전체동기화/단절정책 적용.월드서버타이머에일반hold유입0.
10. 양쪽단절·앱강제종료·패킷지연/중복·거래확정직후호스트이관 각각 원자료/재화/기록1회,기존 v03/v04참가·보상 유지.
11. 동일 20분 시험에서10Hz 지속변경이면 상태묶음 최대12000회/송신 스트림,5초저장이면 정기저장 약240회+필수경계저장.송수신총합·실제byte·재전송·프레임/열/서버CPU·DB비용은 별도 실측. 이산술을 비용 절감률로 발표하지 않음.

### 6. 승인·마감 범위

승인 대상은 최초보관24시간/진행중강제종료없음,불충분결과24시간자동복구후비파괴검토·늦은원증거검증,결과검토시전투점유해제와원거래예약보호,위통신/저장첫시험값과상대ACK비필수저장경계다. 출발전 호스트선정5초표본/20ms동률은 v02 기술후보로 남고 실제전송수단/패킷한도/시간오차와 함께 S-12에서 계측·계약한다. 새로운 게임경제·서버과금상품·배포는 포함하지 않는다.

승인 후 S-12/D-09/U-11/P-12 동일작업에 반영한다. v03/v04확정과이v05승인대기를분리하며신규중복큐를만들지않는다. 일반협동 정책마감과 실제연결/복구·부정행위검증 범위·실기밸런스 완료는 다르다.

## v05 재검토 결정 요약 — 2026-09-28
사용자순차목록2번에따라 ordinary-coop-retention-validation-v05를현재승인v03/v04와재대조했다. 신규승인대기유지. 추천결정은①최초실보관부터24시간재개허용·반복조회로연장0·이미재개한전투강제종료0 ②불충분결과24시간자동복구후‘결과확인필요’보존·후일원증거정산허용 ③검토진입시새전투점유해제하되미확정거래예약보호 ④10Hz상태/유휴1초/5초checkpoint·서버미확정논리진행5초상한을첫시험값으로측정이다. 상대최종ACK부재만으로지급보류하지않고잔류자개인권리/월드서버판정유지. 실제전송수단/호스트선정·패킷한도/오차는담당계약이며비용절감률미확인. 승인전S-12신규범위를확정하거나v05완료로기록하지않는다.

<a id="four-operation-id-gap-20260929"></a>
## 기본 작전 4종 ID 재확인 — 2026-09-29
원 통합기획 사용자 L25057은 “기본 4가지 작전 타입들도 1인 2인 모드로 나누기”를 명시한다. **지원 방향은 승인됐으나 이 발언에는 네 타입명/정확한 타입 ID가 없다.** 따라서 해당 ID는 미확정·근거 부족으로 유지한다. 기존6개 ordinary 사건 ID, 스토리/일반/월드 모드명, 전투/엘리트/보스 이벤트를 임의 네 타입으로 치환하지 않는다.
L25060 assistant 답변의 당시 스토리 1인 전용 등 역사 정책을 이번 복구로 되살리지 않는다. 모드별 현재 허용·권한·보상은 최신 승인 절을 유지한다. 원 타입 정의 또는 후속 명시 결정이 필요하며 실제 데이터/라우팅/큐 변경 없음. [판정표](33_CURRENT_WORK_STATUS.md#chat-gap-reconciliation-20260929).

<a id="four-operation-reward-types-resolved-20260929"></a>
## 기본 작전4종 공백 정정 — 사용자 확인
사용자 직접 확인: **“기존작전 4종은 보상타입 4종으로 편입되서 최신꺼가 맞아.”**
따라서 과거 별도 작전4종의 이름/타입 ID를 복구해야 한다는 확인 항목은 종료한다. 분류는 실제 누락이나 미확정 신규 작전이 아니라 **후속 결정으로 최신 보상 타입4종에 편입·변경**이다.
최신 보상 타입 선택·정산과 모드별1인/2인 정책을 사용한다. 새 입장 타입4개·구 ID·예전 네 작전을 생성하지 않으며 보상 타입과 지급 재화 개수를 동일 개념으로 세지 않는다. four-operation-id-gap 절 및 과거 원출처 복구 요청은 역사 상태이고 이 정정이 우선한다. 실제 wire 식별자·지급표 변경은 없다.
<a id="combat-b-coop-direction-approved-20261002"></a>
## B안 협동 방향 승인·상세 작업 보류 — 2026-10-02
사용자 “추천대로 승인하고 일단 여기까지하고 … 1인 전투 다시 상세하게 잡자”. 직전 추천의 열차 1대·열차 담당/요원 담당 분담을 승인한다. 양쪽 기기는 ③ 기반 세로 화면을 유지하며 역할별 하단 조작부를 달리한다. 열차 담당은 자기 열차/객차 구성과 선로·속도·지원 행동, 요원 담당은 자기 최대 5명 편성과 스킬·AUTO·집중 대상을 맡는 방향이다. 짐칸 전환에 따른 전력/보상은 출발 전 양쪽이 확인한다. 보상 수치 신설 승인이 아니다.
일반 협동 호스트 전투/월드보스 서버 판정 및 이탈 후 잔류자 완주 시 잔류자만 클리어 보상이라는 기존 결정 유지. 조작권 인수·역할별 자산 적용·특수 내부전투 등 협동 상세 작업은 보류하고 B안 1인 전투 선행 설계부터 진행한다. 기존 지원 버튼 효과/ID를 그림만으로 덮어쓰지 않는다. 각자 열차 1대/요원 분할 방식은 채택하지 않는다. 구현 착수·큐 발행·실배포 승인으로 확대하지 않는다.
