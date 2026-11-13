# 47. 성장·반복 보상·출석·패스 상세 계약

2026-09-21 번호별 통합. 이 문서는 흩어진 상세 계약을 주제별로 모은 참조본이다. 원문별 제목·출처·기존 검토 상태를 유지하고, 정책 확정·후보·과거 구현 보고를 섞어 승인하지 않는다.

**적용 순서:** 최신 사용자 결정 → 00~29 담당 정책 및 34~37 충돌 정리 → 이 문서의 충돌하지 않는 상세 계약. 원문의 작성일·완료 문구만으로 현재 구현 또는 승인 완료를 뜻하지 않는다. 원문에 있던 C/E 경로는 과거 기록이며 현재 작업 경로는 프로젝트 AGENTS.md의 D 정책을 따른다.

**공통 대체 경계:** 증강 고정 5회·시작 지급은 폐기, 적격 풀 동일 가중치 3후보·카드별 FREE3+EXTRA1·선택 중 완전 정지가 현행이다. 증강 후보 효과 수치는 출시 승인값이 아니다. 순위 보상은 성장 재화 차등 없이 외형·명예로 구분한다. 인물 역할은 20~29 최신 바이블 우선이며 과거 abilityId 매핑은 자동 변경하지 않는다. 작전물자 30/20·노드3선택/물리2방향·협동 시간·T18 HP0 등 미결정은 34와33의 미결 상태를 유지한다.

원문 내 코드 블록·JSON·표·필드·수치는 생략하지 않았다. 옛 상대 경로를 포함하는 코드 예시는 원출처 기준이며 실행 지시가 아니다. 링크와 제목 앵커만 새 위치에 맞게 변환했다.

## 상세 목차

- [Equipment, Research And Progression Stat Proposal](#source-6445961dcc522513)
- [Quest And Pass Screen Detail V1](#source-a19eb0b11edacaf8)
- [출석 정예 확정권 — 최소 결정안](#source-ffeb1ba92462b970)
- [Attendance Recovery Proposal](#source-d30d577c026f653c)
- [Crystal Source Policy](#source-adabf2069a984f92)
- [Economy Balance Launch Draft](#source-54f2d0d28df9527c)
- [Growth Income Check - 2026-09-08](#source-b69d31ca14e8eafb)
- [Growth Milestones Proposal - 2026-09-08](#source-50012649704ef56a)
- [통합 출석·퀘스트·패스·상점 경제 설계 검토 — 2026-09-08](#source-c3ed6594fb1f2819)
- [Launch Economy Mutation Acceptance - 2026-09-09](#source-9d625ca45a7d9b6a)
- [Launch Economy V1 - 2026-09-09](#source-60149959551b12e2)
- [Low Activity Scenario Design](#source-02711d8c0e43e1fa)
- [Offline Baseline Reconciliation - 2026-09-08](#source-951ce31dfb822c6d)
- [Offline Progression Sensitivity - 2026-09-08](#source-ed20bcc99c9b2ac5)
- [Offline Reward Operations Policy](#source-00e5041688926c56)
- [Optional Car Cashflow Comparison](#source-3f90d984d2252ca6)
- [Optional Car Credit-source Stress](#source-79d99fcdfa4ba5b6)
- [Ordinary Quest Expiry And Mail Recovery — 2026-09-10](#source-54df3b38286fadd3)
- [Fifty-Level Pass Rewards Draft - 2026-09-08](#source-621b5eafbe983156)
- [Pass Benchmark - 2026-09-08](#source-d6799fef84ab2951)
- [Pass Progression Policy](#source-901d352e4d663bd3)
- [Pass Season End Proposal](#source-cee45fb251120260)
- [Quest Outcome Eligibility Proposal — 2026-09-10](#source-04d9d21432054734)
- [Repair and Compensation Policy](#source-9021ee087bd479a3)
- [랭킹 보상 정책 변경](#source-e146be10f59741ea)

<a id="source-6445961dcc522513"></a>
## Equipment, Research And Progression Stat Proposal

원출처: `WorkspaceRepo/docs/design/equipment-research-progression-v1.md`  
원문 SHA-256: `6327d64d5896022762880840431162819c79fcdd5eab867ae66ff2e426da8735`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Unapproved permanent4module Lv1–5/3research proposal; cannot override current13 sixstar3slots/free2merge or09 commander support_fire source. Preserve proposal status, no catalog promotion.

<!-- source-body:source-6445961dcc522513:start -->
<a id="source-6445961dcc522513-equipment-research-and-progression-stat-proposal"></a>
### Equipment, Research And Progression Stat Proposal

2026-09-09, Plan. PROPOSED, not runtime-enabled, no deployment or reward change.
Continues combat-stats-v1.md. Costs use launch-economy-v1.json, not older growth
milestone drafts. All identifiers prefixed proposal_ below are design keys ONLY.
Do not submit them to APIs or treat them as owned items.

<a id="source-6445961dcc522513-approach-and-scope"></a>
#### Approach And Scope

Options considered: one flat universal power multiplier (simple but little choice),
random-stat operator gear (many new drops and economy obligations), or four fixed
train modules with three small permanent research tracks. Recommend the third:
it fits the four equipment slots/three nodes used in the prior economic model.
This is a new catalog proposal, not discovery of a pre-existing approved catalog.
Individual operator weapons/random affixes/reroll costs are outside this slice.

Inspected sources: launch-economy-v1.json, growth-consumption-source-audit,
growth-sink-audit, launch-economy-validation-20260909.mjs and combat-stats-v1.
No Equipment/Research-named files found in the inspected Unity Scripts listing;
that limited search does not establish absence across every owner worktree.

<a id="source-6445961dcc522513-four-equipment-slots"></a>
#### Four Equipment Slots

Fixed slot, one equipped item per slot. Levels1..5; each percentage is the TOTAL
bonus at that level, not an increment stacked on all preceding levels.
Propose basic level1 modules supplied once through onboarding without random drops.
That entitlement requires later approval/implementation; no free grant performed.
Do not make launch story require missing/unowned module items.

| Design key | Display proposal | Affected stat | L1 | L2 | L3 | L4 | L5 |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| proposal_main_gun | Main gun assembly | Locomotive and Gun device ATK | 0% | 3% | 6% | 9% | 12% |
| proposal_hull | Reinforced hull | TrainTotalHP and carriage maxHP | 0% | 3% | 6% | 9% | 12% |
| proposal_power | Power regulator | Locomotive and Gun basic speed | 0% | 1% | 2% | 3% | 4% |
| proposal_medical | Medical service unit | Medical device healing | 0% | 3% | 6% | 9% | 12% |

L1 deliberately provides zero extra multiplier: current baseline already assumes
functional equipment. Do not award the same baseline ATK twice. Main-gun modifier
also affects support_fire, which derives locomotiveATK; not Combat interior device.
Power does not reduce manual/skill cooldown or increase heal pulse frequency.
Medical affects only Medical device heal; not operators, train repair or shields.
No module raises rarity, gacha odds, reward rate, offline cap or stamina capacity.

Train owner supplies these modules in coop. Operator player's train modules cannot
also affect the shared train. No role swapping mid-run. Freeze loadout at start.

<a id="source-6445961dcc522513-three-research-tracks"></a>
#### Three Research Tracks

Ranks0..5, rank0 unresearched. No pre-spend state implies rank1.
Proposed available after the first ordinary clear, with no node prerequisite chain.
Availability is server state, not client elapsed time. No timers or paid skips.

| Design key | Effect per rank | Rank5 total | Ownership in coop |
| --- | --- | ---: | --- |
| proposal_squad_training | Operator base ATK +1% | +5% | Squad owner |
| proposal_field_endurance | Operator maxHP +1% | +5% | Squad owner |
| proposal_rail_engineering | Train/carriage maxHP +1% | +5% | Train owner |

Both players' identical research must NOT add together. Personal research follows
the entity owner. No universal account aura, passive partner benefit, crit or
permanent cooldown reduction is added. All tracks use existing research cost steps.
Rank5 is a long-term sink, not first-month or story-entry requirement.

<a id="source-6445961dcc522513-modifier-order-and-caps"></a>
#### Modifier Order And Caps

This proposal extends, but does not silently enable, combat-stats-v1 equipment/
research flags. Existing definition remains reference with both flags false.

1. Resolve level/promotion multiplier from base; retain precision until final stat.
2. Add persistent percentages in the same stat bucket, then multiply once.
3. Floor resolved HP/ATK once; freeze into the attempt snapshot.
4. Apply combat skill/buff/armor formulas separately at their specified event time.

```
OperatorATK = floor(baseATK * levelFactor * promotionFactor * (1 + researchAtk))
OperatorHP  = floor(baseHP  * levelFactor * promotionFactor * (1 + researchHp))
TrainHP = floor(baseHP * trainHpFactor * (1 + hullHp + railResearchHp))
TrainATK = floor(baseATK * trainOutputFactor * (1 + mainGunAtk))
MedicalHeal = floor(baseHeal * trainOutputFactor * (1 + medicalBonus))
```

Same bucket module12% plus research5% =17%, NOT1.12*1.05. Rank bonuses are
persistent stats, not the temporary outgoing30% cap. Proposed persistent HP/ATK
bucket guard20% for this catalog; cap violation is catalog validation failure,
not a silently discarded purchase. Speed gear4% consumes the existing25% combat
speed cap along with temporary buffs. UI must show capped effective speed.
Armor remains unchanged. HP increases before battle start; no midfight refill.

<a id="source-6445961dcc522513-exact-existing-costs"></a>
#### Exact Existing Costs

| Target step | Credits | Parts | Data |
| --- | ---: | ---: | ---: |
| Equipment1->2 | 200 | 60 | 0 |
| Equipment2->3 | 400 | 120 | 0 |
| Equipment3->4 | 800 | 240 | 0 |
| Equipment4->5 | 1600 | 480 | 0 |
| Research0->1 | 200 | 0 | 20 |
| Research1->2 | 400 | 0 | 40 |
| Research2->3 | 800 | 0 | 80 |
| Research3->4 | 1600 | 0 | 160 |
| Research4->5 | 3200 | 0 | 320 |

One equipment to5:3000credits/900parts; all4:12000/3600.
One research to5:6200credits/620data; all3:18600/1860.
All equipment3 + research2:4200credits/720parts/180data combined.
Skill costs apply once per operator skill-rank target, upgrading the kit rank,
not charging the existing cost independently for passive+active+ultimate.
Changing to individual skill ranks would need a new economy review.
Actual inventory resource IDs still require server catalog mapping; credits/parts/
data here are economic dimensions, not invented API currency codes.

<a id="source-6445961dcc522513-story-growth-reference"></a>
#### Story Growth Reference

Verified catalog currently provides six ordinary missions and one story raid in
the economy table. Do NOT relabel these as all three chapters or invent chapter2/3
mission IDs. References below are suggested balancing targets, NOT entry gates,
automatic enemy scaling, promised days-to-clear or chapter labels.
No research or promotion/core spending assumed for these baseline targets.

| Encounter ordinal | Five-slot operator level | Train | Kit rank | Equipment | Research |
| --- | ---: | ---: | ---: | ---: | ---: |
| Ordinary1 | 1 | 1 | 1 | 1 | 0 |
| Ordinary2 | 5 | 2 | 1 | 1 | 0 |
| Ordinary3 | 10 | 3 | 1 | 1 | 0 |
| Ordinary4 | 15 | 4 | 1 | 1 | 0 |
| Ordinary5 | 20 | 5 | 2 | 2 | 0 |
| Ordinary6 | 25 | 6 | 2 | 2 | 0 |
| Story raid | 30 | 6 | 2 | 2 | 0 |

Need final operation-ID mapping and encounter simulation before authoring enemy
HP multipliers. The six first-clear rewards are insufficient by themselves to
reach these targets; repeat play, attendance/pass and offline income matter.
Do not create circular unlocks requiring that encounter's own first-clear core.
Five-slot cumulative EXP at levels5/10/15/20/25/30 is1150/5000/10000/15000/
22500/30000 under current economy. Level50 is70000, not old95000 trial.
Chapter2/3 targets remain unavailable until their catalogs are verified. Do not
present an extrapolated curve as completed three-chapter launch balance.

<a id="source-6445961dcc522513-affordability-check-and-tradeoffs"></a>
#### Affordability Check And Tradeoffs

Prior conditional fast/free/one-offline-claim28day model gross:
63347credits,4413parts,2312data,71214operatorEXP. It externally assumes progress;
therefore it cannot establish the combat feasibility of that progress schedule.

Illustrative moderate budget: train6 + all4 equipment3 + all5 kit rank2 + all3
research2, no promotions. Costs7500credits/1960parts/580data, plus30000EXP for
five level30 slots. Conditional remainder55847/2453/1732/41214EXP.
Thus modest research can fit the monthly aggregate but day-of-unlock affordability
is unverified. Large credit remainder is not permission for an arbitrary new tax.

Full train10 + all4 equipment5 + all5 kit rank5 + all3 research5 + all5 promotion3
costs76800credits/8690parts/19860data/16special cores and70000EXP to five50.
This exceeds that free gross by13453credits/4277parts/17548data. Core gross is
not included in that comparison;16cores is consumption, not claimed availability.
All-max in one month is NOT the target. Never promise it or fix it with forced
cooperative play/paid purchase. Level50 does not imply maxed all growth systems.

Medical stack/repair power and Sera utility risks from combat-stats-v1 remain.
These small permanent tracks do not solve or validate those combat imbalances.

<a id="source-6445961dcc522513-adoption-and-test-contract"></a>
#### Adoption And Test Contract

Next approval is catalog/target review, not deployment. Server must assign real
IDs and availability, then validate ownership/slot/rank/cost atomically with
idempotency and reject unknown targets without spending. Equipping costs zero,
only allowed outside battle; no salvage/refund rule introduced. UI queries
catalog/current state/next cost/result and does not calculate authoritative costs.
Before adoption test ranks0/1/5, max rejection, missing ownership, invalid slot,
duplicate request, insufficient resources, frozen old balance version, coop owner
isolation, persistent-vs-temporary caps and no midfight stat refill.

Verification for this work is source reads and exact cost arithmetic only.
No server/Unity tests, DB grants, build, video or deployment. All new IDs and
numerical recommendations remain proposals. World three-boss catalog is a separate
remaining task; this document does not mark all stat design complete.

<!-- source-body:source-6445961dcc522513:end -->


<a id="source-a19eb0b11edacaf8"></a>
## Quest And Pass Screen Detail V1

원출처: `WorkspaceRepo/docs/design/quest-pass-screen-detail-v1.md`  
원문 SHA-256: `9f00a38ced39fbacf5f6957b0638f3fa17c3ca4c26afec74f4a8e72b48f13d44`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Quest/pass UI carries ordinary14daymail follow-up; preserve separate claim/claimall, period status, free/premium rows and server-owned rewards. Do not restore earlier unresolved expiry text.

<!-- source-body:source-a19eb0b11edacaf8:start -->
<a id="source-a19eb0b11edacaf8-quest-and-pass-screen-detail-v1"></a>
### Quest And Pass Screen Detail V1

2026-09-10 / Plan_1. 사용자 '다음 세부 기획 ㄱㄱ'에 따른 후속 화면 상세 제안.
직전 결과 화면 구성을 연결 기준으로 사용한다. 아래 신규 화면 동작은 구현·배포 완료나
자동수령·일괄수령 정책의 추가 승인을 의미하지 않는다.

Sources: [전투 결과](44_COMBAT_DETAIL_CONTRACTS.md#source-3d41221b4ed5c4ed),
[결과 복구](44_COMBAT_DETAIL_CONTRACTS.md#source-4d37feef519db046),
[패스 진행](47_ECONOMY_DETAIL_CONTRACTS.md#source-901d352e4d663bd3),
[시즌 종료 승인안](47_ECONOMY_DETAIL_CONTRACTS.md#source-cee45fb251120260),
[레벨 보상 승인안](47_ECONOMY_DETAIL_CONTRACTS.md#source-621b5eafbe983156),
[임무 분배](47_ECONOMY_DETAIL_CONTRACTS.md#source-02711d8c0e43e1fa),
[종료 인정조건](47_ECONOMY_DETAIL_CONTRACTS.md#source-04d9d21432054734).

<a id="source-a19eb0b11edacaf8-1-진입과-화면-구획"></a>
#### 1. 진입과 화면 구획

기존 로비의 임무/패스 진입점을 사용한다. 새 로비 구조를 요구하지 않는다.
전투 결과의 '임무 확인'은 해당 기간·목표로, '패스 확인'은 해당 시즌의 목표 또는
보상레벨로 이동한다. 이전 기간 사건이면 현재 카운터와 섞지 않고 이전 반영내역을
보여준다. 복귀할 전투 결과와 미해결 claim은 기존 복구 계약을 따른다.

| 화면 | 상단 | 본문 | 주 행동 |
| --- | --- | --- | --- |
| 임무 | 일일/주간 선택, 해당 기간 종료시각 | 목표·진행·직접보상·개별 상태 | 서버가 허용한 보상 받기, 작전 선택 |
| 패스 목표 | 시즌명·레벨·XP, 일일/패스 주간 기간 | XP 목표와 진행 | 작전 선택, 보상 보기 |
| 패스 보상 | 시즌 종료일, 레벨·XP, 프리미엄 소유상태 | 같은 레벨의 무료/프리미엄 추가보상 두 열 | 개별 수령, 구매 상세 |

일반 주간은 월요일04시, 패스 주간은 시즌 기준7일 구간임을 각 탭에 표시한다.
패스29~31일에는 '이번 시즌 주간 목표 종료 · 일일 목표 진행 가능'을 표시하고
존재하지 않는5주차 보상이나 새600XP를 만들지 않는다.

<a id="source-a19eb0b11edacaf8-2-임무-행과-우선순위"></a>
#### 2. 임무 행과 우선순위

행 구성: 목표명 → 조건 설명/진행값 → 보상 종류·수량 → 상태/버튼.
조건 설명에서 정상패배 최소1wave, boss 성공만 횟수 인정, 포기·무효 제외를 확인할 수
있게 한다. 조건부 story패배 규칙은 실제 계약 확정 후 연결하며 임의 확정하지 않는다.
D1 로그인은 직접보상 없음이며 출석보상/패스 로그인XP와 중복 보상처럼 표시하지 않는다.

수령 가능 → 진행 중 → 완료 순서를 기본으로 하되, 화면을 읽거나 누르는 도중
행을 재정렬하지 않는다. 갱신 직후 변경 위치를 짧게 강조하고 다음 진입/명시 갱신에
정렬한다. 완료한 목표도 접힌 '완료한 임무'에서 확인 가능하다.

'작전 선택'은 기존 선택화면을 열 뿐 자동으로 전투에 입장하거나 비용을 사용하지
않는다. 특정 story 해금·협력 매칭을 모든 사용자에게 가능하다고 가정하지 않는다.
솔로 또는 협력으로 동일 목표를 진행하며 두 모드를 모두 요구하지 않는다.

<a id="source-a19eb0b11edacaf8-3-개별-수령-상태와-복구"></a>
#### 3. 개별 수령 상태와 복구

| 확인된 상태 | 문구/버튼 | 처리 |
| --- | --- | --- |
| 진행 중 | 현재값/목표값 | 수령 버튼 없음 |
| 달성, 지급자격 조회 중 | 보상 확인 중 | 달성만으로 지급완료 표시 안 함 |
| 수령 가능 | 보상 받기 | 해당 보상에만 수령 요청 |
| 수령 요청 결과 불명 | 지급 확인 중 | 동일 논리요청/내역 확인; 새 중복 요청 금지 |
| 지급 확정 | 수령 완료 | 서버 지급 내역 반영 |
| 일시 조회 실패 | 확인을 다시 시도해 주세요 | 지원되는 상태 재조회 |
| 기간 종료 | 기간 종료 | 지급/미수령/정산 상태는 별도 표시 |

자동 지급으로 확정된 보상에는 불필요한 수령 버튼을 만들지 않는다. 반대로 명시적
수령 계약을 자동지급으로 바꾸지 않는다. 패스 목표XP의 실제 지급 방식은 producer
계약에 맞추며, 클라이언트가 목표달성만 보고 XP를 올리지 않는다.

일괄수령은 이번 범위에 추가하지 않는다. 개별 요청 실패가 다른 확정 보상 상태를
되돌리지 않게 한다. 팝업은 확정 지급 품목만 요약하고 닫기/다음 행동을 막지 않는다.
요청 중 앱 종료·재접속 시 서버 내역을 재조회한다. 통신실패를 미지급으로 단정해
새 지급을 실행하지 않는다. 계정 전환은 표시/대기요청을 분리한다.

일반 일일/주간 임무의 종료 후 미수령 처리 정책은 아직 원문 계약 확인이 필요하다.
패스 자동정산을 복사하거나 소멸·우편·유예기간을 새로 만들지 않는다. 이 부분은
구현 전 해결할 정책 항목이며, 플레이어에게 모호한 임시정책을 노출하는 출시안이 아니다.

<a id="source-a19eb0b11edacaf8-4-패스-보상과-프리미엄"></a>
#### 4. 패스 보상과 프리미엄

레벨당 무료와 프리미엄 추가 보상을 같은 행에서 비교한다. 프리미엄 자원은 같은
무료행의 추가5배이며 구매자 합계6배를 추가5배와 혼동하지 않는다. 수량은 서버
시즌 보상표를 읽고 승인 문서 수치를 클라이언트에 별도 하드코딩하지 않는다.

| 레벨/소유 상태 | 무료 열 | 프리미엄 열 |
| --- | --- | --- |
| 레벨 미도달 | 레벨 달성 필요 | 레벨 달성 필요, 미소유 시 프리미엄 필요도 표시 |
| 레벨 도달·미소유 | 실제 수령상태 | 프리미엄 필요; 지급 완료로 보이지 않음 |
| 레벨 도달·소유 | 실제 수령상태 | 해당 추가보상의 실제 수령상태 |

최초 진입은 가장 낮은 수령 가능 레벨, 없으면 현재 진행 레벨 부근으로 이동한다.
이미 보고 있는 화면을 XP 갱신 때 강제로 스크롤하지 않는다.30/40/50 외형 보상은
도달·소유 조건을 별도로 보여준다. MAX50 이후 반복보상/보너스XP 수령을 암시하지 않는다.

구매 상세에는 현재 획득 레벨의 추가보상, 아직 미획득 보상, 시즌 종료시각과
구매 마감시각을 분리한다. 구매 자체는 XP/레벨을 주지 않는다. 도달가능 레벨 예측은
서버가 잔여 목표를 정확히 제공할 때만 조건부로 표시한다. 남은날×200만으로 계산하지 않는다.
서버 근거로 최종 외형 도달 불가가 확인되면 결제 확인 전에 알리되, 마감 전 하위레벨
보상을 원하는 구매를 임의 차단하지 않는다.

구매마감은 시즌종료24시간 전이다. 마감 후 새 결제진입은 비활성화하고 '이번 시즌
구매 마감'을 표시한다. 이미 생성한 유효 결제의 늦은 검증은 '구매 확인 중'으로
원시즌에 연결한다. 새시즌 프리미엄을 주거나 결제를 다시 하도록 유도하지 않는다.
실제 Store intent·검증 계약 확인 없이 이 동작이 구현되었다고 간주하지 않는다.

<a id="source-a19eb0b11edacaf8-5-시즌-종료와-알림"></a>
#### 5. 시즌 종료와 알림

진행 중 → 종료/정산 중 → 정산 완료를 구분한다. 종료 시 이미 획득한 무료보상과
소유 프리미엄의 미수령분만 자동정산한다. 미도달 레벨은 지급하지 않는다.
정산은 다시 수령해야 하는 만료 우편이 아니며 정산완료 알림은 내역 확인용이다.
실패·지연이면 '정산 확인 중'으로 유지하고 재조회/지원되는 문의 경로를 제공한다.
수동수령·구매 소급지급·종료정산은 같은 권리의 중복 지급을 만들지 않는다.

이전 시즌 정산 중에도 새 시즌 화면은 별도 상태로 열 수 있어야 한다. 이전 정산이
새 XP/소유권을 덮지 않는다. 이전시즌 지급내역 진입은 실제 조회 기능이 전제다.

로비 점표시는 실제 수령 가능 보상이 확인되었을 때만 켠다. 잠긴 프리미엄,
미달성 목표, 조회불명 상태를 수령가능 알림으로 세지 않는다. 지급 확인 중에는
중복 수령을 유도하는 점 대신 확인 중 상태를 사용한다. 전투 중 강제 팝업은 없다.
색상만으로 잠금/완료를 구분하지 않고 문구·아이콘을 병행한다. 시간은 KST 표기와
정확한 일시를 제공하고 카운트다운은 보조로만 쓴다. 로컬시각이 지급자격을 결정하지 않는다.

<a id="source-a19eb0b11edacaf8-6-구현-전-확인과-수용-시나리오"></a>
#### 6. 구현 전 확인과 수용 시나리오

필요 계약: 계정/시즌/목표/기간별 상태 조회, 지급 권리/내역 식별, 재시도 복구,
일반 임무 미수령 종료정책, story패배 비용계약, 구매검증과 종료정산.
이는 요구 의미이며 신규 API 경로가 존재한다는 주장이 아니다.

| 검토 상황 | 수용 기준 |
| --- | --- |
| 첫 종료 후 목표2개 동시 달성 | 각 목표 상태 분리, 해당 권리별1회 지급 |
| 수령 응답 유실 후 재진입 | 기존 지급상태 복구, 새 보상 생성 없음 |
| 일반주간만 초기화 | 패스 주간 진행 보존 |
| 시즌29일 진입 | 일일 제공,5주차 생성 없음 |
| 도달레벨20에서 프리미엄 검증 | 획득한1~20 권리만 추가, XP/레벨 불변 |
| 종료정산과 수동수령 경합 | 같은 보상1회, 확정 내역 일치 |
| 이전시즌 정산 지연/새시즌 진입 | 이전 보상과 새 XP·소유권 분리 |
| 소유하지 않은 프리미엄만 남음 | 수령가능 점표시 없음 |

문서·정책 대조와 저장 후 재읽기/링크/공백 검증만 수행. 위 표는 향후 수용검토용이며
실행한 테스트가 아니다. 코드·API·Store·DEV/PRD·기기 검증은 미수행.
다음 세부기획은 일반 임무의 기간 종료 후 미수령 처리와 복구 정책을 좁혀 정한다.

<a id="source-a19eb0b11edacaf8-ordinary-quest-expiry-follow-up--2026-09-10"></a>
#### Ordinary Quest Expiry Follow-up — 2026-09-10

사용자 확정: 일반 임무의 달성 미수령 직접보상은 기간 종료 시 우편으로 발송한다.
우편 발송 확정부터14일 내 직접 수령해야 하며 미수령 첨부보상은 기한 종료 시 소멸한다.
기간 중 임무 개별수령은 유지한다. 임무의 발송대기/우편발송과 실제 지급완료를 구분하며
우편으로 이관한 보상은 임무 화면에서 다시 지급하지 않는다. 발송 확정 후 '우편함'으로
연결하고 우편에 만료일시를 표시한다. 발송/열람만으로 지갑을 올리지 않는다.
앞선 일반 임무 자동정산 제안과 본문의 종료처리 미확인 항목은 이 설계로 대체한다.
패스 시즌종료 자동정산은 별도 정책으로 유지한다. 우편 계약·구현은 아직 확인 전이다.
자세한 경합·만료·지연처리는 [일반 임무 우편14일 정책](47_ECONOMY_DETAIL_CONTRACTS.md#source-54df3b38286fadd3)을 따른다.

<!-- source-body:source-a19eb0b11edacaf8:end -->


<a id="source-ffeb1ba92462b970"></a>
## 출석 정예 확정권 — 최소 결정안

원출처: `WorkspaceRepo/docs/ops/attendance-elite-ticket-decision-20260910.md`  
원문 SHA-256: `a91f0428d476e965d52e83dda8b87a696b6b1816ea8b03aedf1050c2fd1fb35b`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-ffeb1ba92462b970:start -->
<a id="source-ffeb1ba92462b970-출석-정예-확정권--최소-결정안"></a>
### 출석 정예 확정권 — 최소 결정안

2026-09-10 Plan. 기존 승인: 월출석28번째칸 정예랜덤확정권1장.
신규 제안: 아래풀/확률/중복/천장규칙. 아직 사용자채택·구현완료 아님.

근거: [출석계약대조](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-d8e2ae4cf7244b0715d8)는 특수권의
pool/duplicate/pity를별도승인계약으로요구하고, [통합경제](47_ECONOMY_DETAIL_CONTRACTS.md#source-c3ed6594fb1f2819)는
픽업대상/중복환원/천장진행을추정하지않았다고명시한다. Server도실사용ID/소비경로
미발견보고. Plan관련문서검색에서도기존완성계약을찾지못함;전체코드부재증명아님.

<a id="source-ffeb1ba92462b970-권장결정"></a>
#### 권장결정

1. 출시정예15명(초기5명포함)을고정목록으로한장당1명균등추첨. 별도픽업가중치없음.
   후속추가대원은자동포함하지않고버전정책으로추가한다.
2. 기존일반/픽업모집천장카운터를증가·초기화하지않는별도확정권소비.
3. 중복은이미승인된정예중복전환권리/수량과같게처리하되실제서버정의확인후고정.
   미확인전환수량을새로만들지않는다. 신규미보유보장/재추첨은없다.

이는신규확정권판매/크리스탈환산/일반권대체/추가출석지급승인이아니다.
획득은서버28칸권리1회,소비는별도명시동작이며동일키재시도로추첨/차감/지급중복없음.
item/policy ID는기존완성ID를복구한것처럼표시하지말고Server가충돌없는새식별자로
제안한다. 획득시고정pool version을소비까지보존하는권리모델을권장한다.
보유권의만료/재고정책도해당서버계약으로명시해야하며일반임무14일우편을복사하지않는다.

<a id="source-ffeb1ba92462b970-당장진행가능한작업"></a>
#### 당장진행가능한작업

S08월cycle/순차칸/자연출석/복구원자성은계속구현한다. 정예권binding없는정책은
실계정활성화하지않고28칸을일반권/현금으로채우지않는다. 27칸완료를28칸전체완료로
보고하지않는다. 테스트권리는fixture에한정,누락binding을명시검증한다.

사용자결정후Server는고정풀/중복/천장/권리ID·획득보존/소비계약을같은commit으로
제공하고Unity가실제수령/사용을연결한다. 현재는문서제안/readback만,제품검증없음.

<!-- source-body:source-ffeb1ba92462b970:end -->


<a id="source-d30d577c026f653c"></a>
## Attendance Recovery Proposal

원출처: `WorkspaceRepo/docs/ops/attendance-recovery-proposal-20260909.md`  
원문 SHA-256: `d77b30fc85f6272e88087b58b3746c7dba9db2e8d25ca9292be51b7397c99b3f`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-d30d577c026f653c:start -->
<a id="source-d30d577c026f653c-attendance-recovery-proposal"></a>
### Attendance Recovery Proposal

Status: USER APPROVED DESIGN on2026-09-09; not implemented/deployed by this document.
Existing 28 sequential cells, automatic attendance and KST04 reset retained.

<a id="source-d30d577c026f653c-recommended-terms"></a>
#### Recommended Terms

- 100 crystals per recovered cell, flat price; maximum7 cells per attendance month.
- FREE crystals first; explicit confirmation of PAID remainder before spending.
- Only missed, fully elapsed service days since account attendance eligibility
  began in this cycle. No dates before registration, no current/future day recovery.
- Process today's automatic attendance first. Recover the next sequential unclaimed
  cell, never select the elite cell directly. One cell per confirmed request.
- Remaining recoverable count is server-calculated: minimum of unused missed days,
  remaining monthly recovery quota, and remaining cells up to28.
- No carryover. Cycle switches at04:00KST on calendar-month day1. Previous-cycle
  recovery unavailable after that boundary; stale quote rejects without spending.
- Day28 elite guaranteed ticket remains obtainable through legitimate recovery,
  once per cycle. Recovery does not grant login quest/pass XP or another day's
  daily shop quota, offline time, or login-bonus source entitlement.
- Maximum28 rewards total whether earned normally or recovered. Later natural
  attendance advances from current progress; no second grant to recovered cells.

<a id="source-d30d577c026f653c-api-and-atomicity"></a>
#### API And Atomicity

Read response provides cycle/policy version, server time/endAt, cells and reward
IDs/amounts, natural/recovered status, recoverable count, price and currency split.
Unity renders server response and asset/string IDs, never calculates eligibility.
Read requests do not pay out. Automatic attendance and recovery are authenticated,
idempotent server mutations with atomic wallet/receipt/cell updates. Server validates
policy and account revision, date boundary, ownership, funds and quota in commit.
Replay of a completed request returns its original receipt even across rollover.
New stale requests fail without spending; concurrent natural/recovery claims must
not duplicate a cell. Reward crystals cannot fund the same request before debit.

<a id="source-d30d577c026f653c-value-check-and-tradeoffs"></a>
#### Value Check And Tradeoffs

Reference launch-economy-v1: ordinary ticket100crystals, daily discounted ticket50.
Recovery1/3/7cells costs100/300/700, equivalent to1/3/7ordinary tickets, or2/6/14
discount purchases across eligible days. This is opportunity cost, not a new shop.
Full attendance crystal reward remains650 and elite ticket1. Individual100/150/200
crystal milestone cells can refund or exceed100recovery cost; this is intentional
bounded catch-up, not zero-net-cost guaranteed. No extra reward above original28.
Approved seven recovery cap supports about a week missed, but cannot
guarantee completion for late registration or more missed days. No fictitious
missed-day credit or duplicate paid bypass to guarantee elite ticket completion.
28/29/30/31-day months allow different natural catch-up opportunities; server
uses actual service-day boundaries, not assumed28calendar days.

<a id="source-d30d577c026f653c-before-implementation"></a>
#### Before Implementation

User approved100price/7cap and monthly boundary. Reconcile existing
attendance cycle registration and receipt fields. Test full/late registration,
all month lengths, quota/funds failure, crystal milestone, elite once, midnight
vs04reset, stale quote, replay, concurrent natural/recovery, and rollback.
These are acceptance cases, not executed DB tests. No balance JSON or runtime edits.

<!-- source-body:source-d30d577c026f653c:end -->


<a id="source-adabf2069a984f92"></a>
## Crystal Source Policy

원출처: `WorkspaceRepo/docs/ops/crystal-source-policy.md`  
원문 SHA-256: `6b8ab4058ea6d0d91f451fde202ef5840051e0178b05aa10cbad09cd927791ac`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-adabf2069a984f92:start -->
<a id="source-adabf2069a984f92-crystal-source-policy"></a>
### Crystal Source Policy

<a id="source-adabf2069a984f92-accepted-2026-09-07"></a>
#### Accepted 2026-09-07

- One displayed crystal currency with separate free/earned and paid/purchased
  balances and order-linked grant/spend provenance.
- Attendance and both free/premium pass crystal rewards are FREE. Premium pass
  rewards retain purchase linkage for refund reconciliation.
- Offline repeat rewards grant no crystals. No tutorial crystal grant approved.
- Free-first consumption; paid usage disclosure. Track downstream ticket exchange
  without double recovery. Verified refunded/revoked orders reconcile once.
- Do not automatically delete recruited operators or silently recover unrelated
  rewards. Spent-order deficits require auditable reconciliation/support and an
  explicitly specified restriction policy before automated enforcement.

<a id="source-adabf2069a984f92-not-yet-fixed"></a>
#### Not Yet Fixed

Currency display name/ID, ticket conversion rate, attendance recovery price,
campaign/pass tables, real-money SKU pricing and detailed refund eligibility.
Earlier100-per-ticket/20-per-recovery/28-day proposals are not activated values.
This policy does not authorize live wallet migration or store integration.

Local planning record; implementation/deployed state belongs to Server handoff.

<!-- source-body:source-adabf2069a984f92:end -->


<a id="source-54f2d0d28df9527c"></a>
## Economy Balance Launch Draft

원출처: `WorkspaceRepo/docs/ops/economy-balance-launch-draft.md`  
원문 SHA-256: `ae10a403af0f3e140f54518fe51ecb9de159f29b7a55aa137ed3100c4532458a`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-54f2d0d28df9527c:start -->
<a id="source-54f2d0d28df9527c-economy-balance-launch-draft"></a>
### Economy Balance Launch Draft

<a id="source-54f2d0d28df9527c-purpose"></a>
#### Purpose

This document proposes first-pass launch economy numbers for LAST RAIL run rewards and offline rewards.

These are draft balance values for testing and tuning. They are not final production economy values until explicitly promoted into server balance seed/config.

<a id="source-54f2d0d28df9527c-goals"></a>
#### Goals

- Make active runs the primary source of meaningful progression.
- Let offline rewards provide recovery and baseline continuity without replacing play.
- Keep rare/special currencies controlled.
- Make Act 1 progression feel materially better without runaway compounding.
- Preserve server Source of Truth for all reward quantities.

<a id="source-54f2d0d28df9527c-currency-roles"></a>
#### Currency Roles

| Reward Code | Role | Launch Notes |
| --- | --- | --- |
| `cur_credits` | General upgrade/spend currency | Main repeat reward. |
| `cur_machine_parts` | Train and equipment maintenance | Main Act 1 progression material. |
| `cur_tactical_data` | Tactical upgrade/research material | Secondary active-run reward. |
| `cur_operator_exp` | Operator growth | Allowed in runs and offline rewards at conservative amounts. |
| `cur_special_core` | Rare upgrade limiter | First clear/high-risk/event only candidate. |
| `cur_command_ticket_fragment` | Controlled recruitment progress | Not normal offline repeat. Limited first clear/event candidate. |
| `cur_command_ticket` | Full recruitment ticket | Limited non-repeat launch sources only. Not normal offline repeat at launch. |

<a id="source-54f2d0d28df9527c-active-run-reward-formula"></a>
#### Active Run Reward Formula

Existing formula candidate:

```text
runReward = baseReward(operationId)
  x operationProgressionMultiplier
  x riskMultiplier
  x outcomeMultiplier
  x waveCompletionMultiplier
```

First clear bonus remains a separate reward line and is not multiplied into repeat reward.

<a id="source-54f2d0d28df9527c-active-run-multipliers"></a>
#### Active Run Multipliers

Risk multipliers:

| Risk | Multiplier |
| --- | --- |
| I | 0.80x |
| II | 1.00x |
| III | 1.20x |
| IV | 1.45x |
| V | 1.75x |

Operation progression multipliers:

| Operation | Multiplier |
| --- | --- |
| `opn_01_last_departure` | 1.00x |
| `opn_02_first_signal` | 1.10x |
| `opn_03_water_stop` | 1.20x |
| `opn_04_scrapstar_show` | 1.30x |
| `opn_05_night_boarders` | 1.40x |
| `opn_06_broken_line` | 1.55x |
| `raid_raider_king_engine` | 2.20x |

Outcome policy:

- Victory with 15 waves: 1.00x
- Failure with 1-14 cleared waves: `0.25 + 0.55 x clearedWaves / 15`, capped at 0.80x
- Abandoned: 0.00x for launch
- 0 cleared waves: 0 reward or minimal consolation only

<a id="source-54f2d0d28df9527c-active-run-base-rewards"></a>
#### Active Run Base Rewards

Base repeat rewards before multipliers:

| Operation | Credits | Machine Parts | Tactical Data | Operator EXP |
| --- | ---: | ---: | ---: | ---: |
| `opn_01_last_departure` | 120 | 8 | 4 | 700 |
| `opn_02_first_signal` | 135 | 9 | 5 | 780 |
| `opn_03_water_stop` | 150 | 11 | 6 | 860 |
| `opn_04_scrapstar_show` | 170 | 13 | 7 | 950 |
| `opn_05_night_boarders` | 195 | 15 | 8 | 1050 |
| `opn_06_broken_line` | 225 | 18 | 10 | 1180 |
| `raid_raider_king_engine` | 360 | 28 | 16 | 1600 |

Rationale:

- Credits scale smoothly with operation complexity.
- Machine parts grow faster because Act 1 train recovery is central.
- Tactical data remains lower to avoid early research saturation.
- Operator EXP stays conservative but meaningful.
- Raid repeat reward is higher but not a substitute for long-term rare currencies.

<a id="source-54f2d0d28df9527c-active-run-first-clear-rewards"></a>
#### Active Run First Clear Rewards

First clear reward is one-time per account, operation, and risk.

Recommended base first clear by operation at Risk II:

| Operation | Credits | Machine Parts | Tactical Data | Special/Controlled |
| --- | ---: | ---: | ---: | --- |
| `opn_01_last_departure` | 250 | 15 | 8 | none |
| `opn_02_first_signal` | 300 | 18 | 12 | none |
| `opn_03_water_stop` | 360 | 24 | 14 | none |
| `opn_04_scrapstar_show` | 430 | 30 | 18 | `cur_command_ticket_fragment` candidate x1 |
| `opn_05_night_boarders` | 520 | 38 | 22 | none |
| `opn_06_broken_line` | 650 | 48 | 28 | `cur_special_core` candidate x1 |
| `raid_raider_king_engine` | 1000 | 75 | 45 | `cur_special_core` candidate x2 |

Controlled reward notes:

- `cur_command_ticket_fragment` should remain candidate-only until gacha economy is validated.
- `cur_special_core` should be first-clear or event-limited, not normal repeat.
- Full `cur_command_ticket` should be granted only from selected Act 1 first clears and launch login bonus at launch.

Recommended launch command ticket distribution:

| Source | Tickets |
| --- | ---: |
| Launch login bonus total | 5 |
| `opn_03_water_stop` first clear | 1 |
| `opn_05_night_boarders` first clear | 1 |
| `opn_06_broken_line` first clear | 2 |
| `raid_raider_king_engine` story raid first clear | 3 |
| **Launch non-repeat total** | **12** |

Rationale:

- 12 tickets lets a launch player experience one 10-pull plus a small number of single pulls.
- Tickets stay tied to account progression and launch attendance, not repeat offline income.
- Offline repeat rewards remain stable and do not become a recruitment faucet.

<a id="source-54f2d0d28df9527c-offline-reward-formula"></a>
#### Offline Reward Formula

Existing formula candidate:

```text
offlineReward = baseHourlyReward
  x storyProgressMultiplier
  x cappedOfflineHours
```

Claimable hours:

```text
claimableHours = min(actualOfflineHours, offlineStorageCapHours, maxClaimableHours)
```

Risk tier does not directly multiply offline rewards.

<a id="source-54f2d0d28df9527c-offline-story-progress-policy"></a>
#### Offline Story Progress Policy

| Story Progress | Multiplier | Storage Cap |
| --- | ---: | ---: |
| `none` | 1.00x | 4h |
| `opn_01_last_departure` | 1.15x | 5h |
| `opn_02_first_signal` | 1.32x | 6h |
| `opn_03_water_stop` | 1.52x | 7h |
| `opn_04_scrapstar_show` | 1.75x | 8h |
| `opn_05_night_boarders` | 2.05x | 9h |
| `opn_06_broken_line` | 2.40x | 10h |
| `raid_raider_king_engine` | 3.00x | 12h |

<a id="source-54f2d0d28df9527c-offline-base-hourly-rewards"></a>
#### Offline Base Hourly Rewards

Base hourly rewards before story multiplier:

| Reward Code | Base Per Hour | Launch Notes |
| --- | ---: | --- |
| `cur_credits` | 24 | Main offline recovery reward. |
| `cur_machine_parts` | 2 | Slow train maintenance income. |
| `cur_tactical_data` | 1 | Conservative background research income. |
| `cur_operator_exp` | 80 | Modest growth continuity. |

Not included in normal offline rewards:

- `cur_special_core`
- `cur_command_ticket_fragment`
- `cur_command_ticket`

<a id="source-54f2d0d28df9527c-example-offline-full-cap-values"></a>
#### Example Offline Full-Cap Values

These examples use full storage cap and are for tuning intuition only.

| Story Progress | Cap | Credits | Machine Parts | Tactical Data | Operator EXP |
| --- | ---: | ---: | ---: | ---: | ---: |
| `none` | 4h | 96 | 8 | 4 | 320 |
| `opn_01_last_departure` | 5h | 138 | 12 | 6 | 460 |
| `opn_02_first_signal` | 6h | 190 | 16 | 8 | 634 |
| `opn_03_water_stop` | 7h | 255 | 21 | 11 | 851 |
| `opn_04_scrapstar_show` | 8h | 336 | 28 | 14 | 1120 |
| `opn_05_night_boarders` | 9h | 443 | 37 | 18 | 1476 |
| `opn_06_broken_line` | 10h | 576 | 48 | 24 | 1920 |
| `raid_raider_king_engine` | 12h | 864 | 72 | 36 | 2880 |

Rounding policy:

- Calculate per reward line independently.
- Use floor for fractional quantities.
- Minimum positive reward line can be 1 only if the base calculation is greater than 0 and claim is not below minimum time.

<a id="source-54f2d0d28df9527c-active-run-versus-offline-target"></a>
#### Active Run Versus Offline Target

Launch target:

- One full-cap offline claim should feel useful, but not exceed several active full clears.
- At early Act 1, a 5-6 hour offline claim should roughly equal 1 standard low-risk active run in credits, but lower in tactical agency.
- At late Act 1, a 10-12 hour full-cap claim can equal 2-3 standard active runs in credits, but should not grant rare progression currency.
- Risk IV/V active runs should remain the best path for accelerated rewards.

<a id="source-54f2d0d28df9527c-daily-earning-guardrails"></a>
#### Daily Earning Guardrails

Draft daily assumptions:

- Casual player: 2-3 active runs + 1 offline claim.
- Engaged player: 6-8 active runs + 1-2 offline claims.
- Heavy player: 12+ active runs + capped offline claims.

Guardrails:

- Offline rewards should not dominate engaged player income.
- Active run risk rewards should be visible but not force only Risk V play.
- First clear should feel meaningful but not destabilize repeat economy.
- Rare currencies should remain controlled by first clear, event, pass, shop, or explicit high-risk policies.

<a id="source-54f2d0d28df9527c-economy-risk-checks"></a>
#### Economy Risk Checks

Before promotion to production config, test:

- Day 1 total credits for casual, engaged, and heavy profiles.
- Day 7 machine part accumulation versus train upgrade costs.
- Tactical data income versus research unlock pacing.
- Operator EXP leveling speed through Act 1.
- Risk I farming value versus Risk III/V expected value.
- Offline full-cap value after each story milestone.
- First clear burst income per risk tier.
- Absence of `cur_special_core`, command ticket fragment, and command ticket from normal offline repeat.

<a id="source-54f2d0d28df9527c-admincs-interpretation"></a>
#### Admin/CS Interpretation

Admin should show these values as balance policy, not player-owned truth, until read endpoints return live data.

Support should explain:

- Active run reward depends on operation, risk, outcome, and wave completion.
- Offline reward depends on story progress, elapsed server time, and cap.
- Risk does not directly increase offline reward.
- Long absence may hit storage cap.
- Minimum time below threshold is not claimable.

Support should not promise:

- special core compensation
- command ticket compensation
- manual currency adjustment
- uncapped offline recovery
- gacha reroll

<a id="source-54f2d0d28df9527c-promotion-rules"></a>
#### Promotion Rules

These numbers can be promoted only after:

- Server balance config parser accepts the final shape.
- Unit tests cover representative active/offline examples.
- Admin labels and support docs match final values.
- Unity displays server-provided reward lines without local calculation.
- DEV compatibility smoke confirms preview/claim response shapes.

<a id="source-54f2d0d28df9527c-open-decisions"></a>
#### Open Decisions

- Whether `cur_operator_exp` offline income should be reduced after early launch testing.
- Whether `cur_command_ticket_fragment` appears in Act 1 first clear at all.
- Whether `cur_special_core` is first-clear only or can appear in Risk V event variants.
- Whether raid repeat rewards need weekly lockout or diminishing returns.
- Exact train upgrade costs and research costs needed to validate these income values.
- Whether economy balancing should use dynamic remote config before launch.

<!-- source-body:source-54f2d0d28df9527c:end -->


<a id="source-b69d31ca14e8eafb"></a>
## Growth Income Check - 2026-09-08

원출처: `WorkspaceRepo/docs/ops/growth-income-check-2026-09-08.md`  
원문 SHA-256: `34532bf817f19b48e137804a51e7283d8f406a90604ecd28acc585f3e6f7ddc4`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-b69d31ca14e8eafb:start -->
<a id="source-b69d31ca14e8eafb-growth-income-check---2026-09-08"></a>
### Growth Income Check - 2026-09-08

Plan direct source inspection and arithmetic, not runtime validation or approved
cost/rate changes. Existing accepted pass rewards preserved.

Sources inspected:
- E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/Train/TrainCoreUpgradeCostCatalog.cs
- C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/packages/balance/src/index.ts
- Same server apps/game-api/src scoped upgrade/research/EXP/data search.

Unity local catalog1-to10 totals10200credits,5090parts,1special core. No matching
server-authoritative upgrade/research/EXP spending table found in inspected scope;
absence is scoped, not proof none exists anywhere. Unity catalog not active truth.

Source offline base100credits/50parts/30data/20EXP per hour, no progress1x with4hour
storage; later progression increases caps and multipliers. Scenario:28days,one
4hour claim daily at1x, no additional claims. Adds11200credits/5600parts (also
3360data/2240EXP). Storage4hours is not a daily cap; additional claims possible.

| Source scenario | Free credits | Free parts | Buyer credits | Buyer parts |
| --- | --- | --- | --- | --- |
| Attendance+daily+4weekly+fullpass | 26300 | 1440 | 63800 | 5190 |
| Above plus4offline hours/day | 37500 | 7040 | 75000 | 10790 |

Run rewards,one-time grants,store spend,additional equipment and operator costs
excluded. Even this conservative offline scenario supplies more parts than the
local train1-to10 requirement. It does NOT prove actual level10 reachable: core,
unlock,server cost/access gates and competing sinks not modeled. Free parts from
non-offline subtotal fund levels1-to6 (1240parts), not next600; paid subtotal funds
all ordinary parts but not the special core. No invented ability to buy cores.

Interpretation: price/rate units must be reconciled before economy PASS. Do not
silently nerf agreed pass or multiply every cost by5. Need intended time-to-upgrade,
authoritative train/equipment/operator/research sinks and offline/run baseline.
Recommend testing an explicit free active-user approximately one-month Act1 train
growth target versus faster paid diversification, not locking basic access behind
paid pass. This timing is a proposal, not measured or approved.

JS sums10200/5090 and scenario totals checked. No test suite/deploy/DB writes.
Local uncommitted analytical record; growth balance remains unresolved.

<!-- source-body:source-b69d31ca14e8eafb:end -->


<a id="source-50012649704ef56a"></a>
## Growth Milestones Proposal - 2026-09-08

원출처: `WorkspaceRepo/docs/ops/growth-milestones-proposal-20260908.md`  
원문 SHA-256: `5be618e8d4cd71727968943f3f4aaec6a693379f3e88062316325a028d8acca7`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-50012649704ef56a:start -->
<a id="source-50012649704ef56a-growth-milestones-proposal---2026-09-08"></a>
### Growth Milestones Proposal - 2026-09-08

Plan-owned numerical proposal; not approved final costs, runtime patch or launch
balance certification. User continued the trial offline baseline analysis.
Hourly24credits/2parts/1data/20EXP is used for this trial, not applied to service.

<a id="source-50012649704ef56a-intended-milestones-and-conditional-results"></a>
#### Intended Milestones And Conditional Results

Keep pass level separate from permanent operator level. Equal active play and
equal level caps for free/premium; no exclusive power cap or purchase entry gate.
Proposed target bands: free five-core levels15-20 at day7,25-30 at day14,50 near
day28. Train roughly3-4,5-6,8 respectively, retaining material for other growth.

| Day | Free train | Premium train | Free five-core level | Premium five-core level | Pass level both |
| --- | --- | --- | --- | --- | --- |
| 7 | 3 | 6 | 17 | 22 | 20 |
| 14 | 5 | 8 | 29 | 35 | 40 |
| 21 | 7 | 9 | 40 | 46 | 50 |
| 28 | 8 | 9 | 50 | 50 | 50 |

Results from model assumptions, not promised player progression. Free reaches
five-core50 day27; premium day24. Paid train can afford level10 material byday28
under allocation but actual level10 excluded because special core availability
is unverified. Premium train advantage is especially large at day7: must test
combat tuning and non-core equipment rather than declaring equal strength.

<a id="source-50012649704ef56a-proposed-operator-exp-curve"></a>
#### Proposed Operator EXP Curve

Replace reference milestones ONLY after design acceptance; old architecture
reference55000EXP for five50 is not silently changed or treated as implemented.
New curve is transparent, nondecreasing per-step, exactly specified:
- Levels1to10 step costs40,50,60,80,100,120,150,180,220.
- Levels10to20 cost250 per level.
- Levels20to30 cost400 per level.
- Levels30to40 cost500 per level.
- Levels40to50 cost650 per level.

| Level | Cumulative EXP per core slot | All five |
| --- | --- | --- |
| 10 | 1000 | 5000 |
| 20 | 3500 | 17500 |
| 30 | 7500 | 37500 |
| 40 | 12500 | 62500 |
| 50 | 19000 | 95000 |

Existing5slots/minimum-sync/actual-invested-EXP100percent refund policy stays.
Model sends all operator EXP to five equal slots, no EXP cost for skill/research
assumed. Level result is attainable minimum-five level, not five arbitrary
individual optimizations. No extra leveling cost for every owned operator.

<a id="source-50012649704ef56a-train-and-other-resource-budget"></a>
#### Train And Other Resource Budget

Keep existing local train core reference costs for comparison, not newly server
certified: level1to10 total10200credits/5090parts/specialcore1.
Use at most65percent cumulative earned parts on train, retain35percent for
equipment/other growth. This percentage is an analytical allocation, NOT a
player spending restriction, actual sink or approved maintenance tax.
Check credit affordability too; no competing credit costs assumed yet.
Free28day parts4368, train8 spends2640 =>1728 remains; credits63088-5300=57788
before equipment,skills/research/store spending. Those are not modeled free money.
Missing authoritative costs still prevent a complete net-economy conclusion.
Do not invent recurrent repair fees merely to absorb excess income.

<a id="source-50012649704ef56a-model-conditions"></a>
#### Model Conditions

Reproduce: node docs/ops/growth-milestones-model-20260908.mjs
One synthetic28day season begins Tuesday, no prior weekly credit. Daily1hour
active,23hours offline split equally into two11.5h intervals. Standard externally
specified story stages start days1/3/5/8/11/15/20/25. Two ordinary RiskII15wave
victories/day using draft repeat reward table and latest completed ordinary
operation (op1 for initial state, op6 after raid). Progress schedule is not
derived from encounters, stat gates or fuel. First clears/boss rewards excluded.

Attendance material pattern follows accepted28cell design; daily and ordinary
weekly material rewards remain model drafts. Ordinary weekly sets granted
days3/9/16/23; pass600XP on days3/10/17/24, daily200XP. Accepted pass rewards
awarded when each level is earned; premium extra exactly5x at same earned level.
Pass50day17, not five-core50day17. All income gross before growth spending.
No late purchase, recovery, missed days, extra runs or different risk tiers.

Free28day credits63088/parts4368/data2258/EXP100492; buyer100588/8118/4008/117992.
Dedicated full-player economy, actual combat/device testing still outstanding.

<a id="source-50012649704ef56a-source-owner-confirmation-and-next-step"></a>
#### Source Owner Confirmation And Next Step

SERVER_1 handoff source-audit entry confirms DB/Admin growth states but no
growth APIs/levelExpCurve in inspected source. Offline100/50/30/20 also appears
in last-deployed fixed-source c08bfcd; no fresh remote check. Do not represent
trial baseline as harmless docs-only correction to already matching live values.

Next: user accepts/modifies target bands and this curve, then server reviews
Training Core transactional contract and implementation slice. Recover/finalize
equipment,skill/promotion,research costs separately without inventing old policy.
Only after approved config/version migration plan may actual rewards/costs change.

Verification: model run exit0; assertions pass for full50curve,total income,
premium extra amounts,pass50day17,paid/free ordering and special-core guard.
No runtime tests, deployments, DB mutations or source-owner files changed.
Plan report/script/handoff local uncommitted; inherited work preserved.

<!-- source-body:source-50012649704ef56a:end -->


<a id="source-c3ed6594fb1f2819"></a>
## 통합 출석·퀘스트·패스·상점 경제 설계 검토 — 2026-09-08

원출처: `WorkspaceRepo/docs/ops/integrated-economy-design-2026-09-08.md`  
원문 SHA-256: `a026cfe1c52bc5e2efb7af34ecc9b47dff193cd28c650bf7e1746b9047bd56f2`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-c3ed6594fb1f2819:start -->
<a id="source-c3ed6594fb1f2819-통합-출석퀘스트패스상점-경제-설계-검토--2026-09-08"></a>
### 통합 출석·퀘스트·패스·상점 경제 설계 검토 — 2026-09-08

**추천안 및 로컬 계산 결과이며, 출시 수량·현금 SKU·활성 정책의 확정본이 아니다.**
사용자가 확정한 규칙은 유지했고, 수량/XP/시즌/가격은 아래 제안으로 분리했다.
제품 코드, DEV/PRD 설정, 재화 지급, 실제 구매, Store 등록·배포는 변경하지 않았다.

<a id="source-c3ed6594fb1f2819-1-결론과-추천-선택"></a>
#### 1. 결론과 추천 선택

- **추천: 달력 월 패스, 30레벨, 누적3,000XP, 무료 보상 유지 + 유료 단독 동종재화5배.**
  일일100XP, 주간 일반450XP + 월드보스 참여50XP를 제안한다. 두 모드 OR 진행,
  소비/결제/모집 미션 없이 주5일은 여유 있고, 기존 주3일 유저는 주간 목표까지
  충족하면 완주한다. 신규·복귀·저진행 유저에게 같은 완주를 보장하지 않는다.
- 28일 매일/주간일반4회 기준 반복 크리스탈은 무료2,310, 유료 포함4,810이다.
  할인28회 후 남은 잔액을 정가 교환하면 일반권37/62장이다. 출석 정예권1장은 별도다.
  무료를 깎아 유료5배를 맞추지 않았으며, 일일 할인은 선택 소비다.
- **균형 최종 확정은 보류:** 성장·연구의 서버 권위 비용표, 기본재료 상점 가격,
  보장권 상세 처리와 현금 SKU가 없다. 유료 패스 재료와 기존 오프라인 수입이
  성장 제한을 크게 약화할 위험이 확인됐으므로 단순 '5배라 적절함'으로 승인하면 안 된다.

비교한 대안: (A) 과거28일 고정 시즌은 단순하지만 월출석과 주간/월경계가 계속
어긋난다. (B) 추천 달력 월은28/30/31일 차이를 원장으로 설명하며 사용자가 이해하기
쉽다. (C) XP 구매/유료XP가속은 완료 격차와5배 보상 해석을 혼동하므로 이번안에서 제외.
A의30레벨×100XP·일100/주500은 여전히 과거 미승인 초안이며, 숫자 재사용 자체가
승인이 아니다. B의 주500 중50은 선택적 보스이고 일반450만으로 완주 여부를 계산했다.

<a id="source-c3ed6594fb1f2819-2-확정-규칙--제안--현재-구현"></a>
#### 2. 확정 규칙 / 제안 / 현재 구현

| 구분 | 내용 |
| --- | --- |
| 사용자 확정 | 월간28칸 순차출석, 놓친 날 때문에 칸을 건너뛰지 않음, KST04 경계, 서버 지급 완료 후 확인 팝업 |
| 사용자 확정 | 크레딧=기본성장·보급품, 크리스탈=모집·특별구매; 크레딧→크리스탈/지휘권 직교환 제외 |
| 사용자 확정 | 일반 지휘권1장 계정당 일1회50% 할인, 이월/유료초기화 없음; 정예권/10장팩 할인 아님 |
| 사용자 확정 | FREE-first, PAID 사용량 공개; 출석·무료/유료패스 크리스탈은 모두 FREE, 유료패스 보상은 주문 연결 유지 |
| 사용자 확정 | 유료 트랙 단독 같은 레벨 보상≥무료5배로 계산; 무료 포함 합계≥6배. 총합5배라는 다른 해석과 구별 |
| 조건부 가격 기준 | 과거100크리스탈/일반권1장, 할인50을 계산에 사용. 승인기록과 crystal-source-policy 문구 불일치; 출시 seed/현재가격 확인 아님 |
| 신규 제안 | 아래 시즌·XP·레벨보상·퀘스트 수량·현금가격·종료처리. 정예권/일반재료 상품 판매는 새로 확정하지 않음 |
| 현재 소스 | c7163e4: 지갑 교환·FREE-first·idempotency·활동 기반. 현 배포 증거는 c08bfcd; 마지막 DEV 지갑/활동 정책0건(9/7), 오늘 원격조회 없음 |
| 아직 구현/활성 아님 | 이28칸 표·미션XP 연결·일일할인 한도·범용상품목록·실결제·아래 자동패스정산 |

<a id="source-c3ed6594fb1f2819-3-시즌진행구매-제안"></a>
#### 3. 시즌·진행·구매 제안

- 시즌: 매월1일04:00 KST 이상, 다음달1일04:00 미만. 일별/주별 기간은 서버 논리일,
  주간은 월요일04:00. 30레벨의 xp 필드는 레벨별 필요량100을 반복 기입하는 것이
  아니라 **누적100,200,…,3,000**이다. 초과XP는 추가보상·다음시즌 이월 없이 종료한다.
- XP는 아래 일/주 퀘스트에서만 적립한다. 현재 소스의 별도 runXp를 함께 켜지 않는다.
  제안에서는 runXp={}이며 직접 반복런XP 무한 파밍을 허용하지 않는다. 퀘스트 완료
  이벤트→시즌XP 연결은 현재 엔드포인트에 없는 기능이다.
- 중도 구매: 같은 시즌, 이미 실제 획득한 모든 레벨의 미수령 유료보상을 소급한다.
  레벨/XP 가속, 미획득 레벨 보장, 시즌 연장 없음. 구매일 기준으로 무료와 유료
  최종 레벨이 달라지지 않는다. 환불/철회 시 기존 주문추적 정책으로 대사한다.
- 종료 처리 제안: 종료 시 XP 고정, 획득 보상 자동정산; 결과 확인·누락 재시도7일
  (claimUntil=end+7일). 정산실패는 소각하지 않고 보류 원장/운영 확인 대상으로 유지한다.
  유료 entitlement의 수령 유효기간도 claimUntil까지 정합해야 한다. 기존 명시적 claim
  API가 자동정산까지 제공한다고 주장하지 않는다. 종료 후 새 패스 판매·미션 적립 없음.
- 패스 가격 제안: **시즌당7,900원**을 검토 출발점으로만 둔다.다른 후보가격 또는 시장가 대비
  효율 수치는 주장하지 않는다. 세금·지역가격·Store SKU·수수료·
  환불/영수증검증·유료 가치를 검증한 가격이 아니며 실제 Store 등록/판매 불가 상태다.
  현금크리스탈팩과 패스SKU를 합쳐 '몇 배 혜택' 광고하지 않는다.
- 잔여기간·현재 레벨·즉시 소급되는 수량을 구매 전에 표시한다. 월15/22일 신규 시작은
  아래처럼 미완주하므로 '구매하면 만렙' 표시는 금지한다. 늦은 시작 보정/구매형 레벨업은
  이번 예산에 없으며 추가 설계 없이 지급하지 않는다.

<a id="source-c3ed6594fb1f2819-4-일일주간-퀘스트-제안"></a>
#### 4. 일일·주간 퀘스트 제안

| ID | 목표/횟수 | 보상 제안 | 패스XP | 구분 |
| --- | --- | --- | ---: | --- |
| D1 | 유효 로그인1회 | 직접재화 없음(출석과 중복 로그인 지급 분리) | 20 | 일1회 |
| D2 | 일반작전1인 OR 2인, 서버 인정 종료1회 | FREE10, 크레딧100, 부품5 | 40 | 일1회 |
| D3 | 위 일반작전 누적2회 | FREE10, EXP50, 데이터5 | 40 | 일1회 |
| W1 | 유효 접속3개 서로 다른 서버일 | FREE50, 크레딧500 | 150 | 주1회 |
| W2 | 위 일반작전 누적6회 | FREE50, 부품50 | 150 | 주1회 |
| W3 | 인정 종료 결과의 누적 진행60웨이브 | FREE50, EXP300, 데이터20 | 150 | 주1회 |
| W4 | 월드보스1회 유효 참여 | 기존 World Raid Participation 보상과 동일 건; 별도 직접재화 추가 없음 | 50 | 선택 주1회 |

매일 최대 FREE20/크레딧100/부품5/EXP50/데이터5/100XP.
주간 일반 최대 FREE150/크레딧500/부품50/EXP300/데이터20/450XP.
보스는 참여만 판정하며 승리·개인딜·기믹성공·협동 의무 없음. 보스 없이 만렙 가능하도록
계산했다. 추가XP50은 제안이고 기존 참여 보상을 다시 지급하지 않는다.

D2/D3는 완료/실패를 서버 결과로 판정하고 즉시 포기·무효·격리 런은 제외한다.
개인 킬/딜, 친구 초대, 두 역할 모두, 스토리 전용, 결제/재화소비/매일모집을 요구하지 않는다.
한 런이 일일·주간 목표에 함께 기여하는 것은 의도된 중첩이며 각 목표 보상은1회만 지급한다.
1인·2인 합산 진행/수령 상태를 공유한다. 두 사람의 상보 행동을2번으로 세지 않는다.
웨이브 정규화가 없는 특정 모드는 동일 완료단위 매핑을 별도 승인해야 하며 임의 변환하지 않는다.
미션 잠금·콘텐츠 접근성·입장 연료 수급은 별도 검증 한계다.

<a id="source-c3ed6594fb1f2819-5-패스30레벨-표-및5배-검증"></a>
#### 5. 패스30레벨 표 및5배 검증

각 행 유료 보상은 동일 행 무료 보상의 동일 재화5배다. 이 구조로 **모든 중간 레벨
누적분**도 동일재화별5배를 충족한다. 구매자는 무료+유료6배이며 XP는 동일하다.
확정권/희귀품을 임의 크리스탈·현금가치로 환산하지 않았다. 패스에는 새 정예권/특별품을
추가하지 않아5배 비교에 예외가 없다. 특별한 외형/희귀 보상은 가치비교 밖의 별도 제안이다.

| 레벨 | 누적XP | 무료 | 유료 단독 |
| ---: | ---: | --- | --- |
| 1 | 100 | FREE 크리스탈 100 | FREE 크리스탈 500 |
| 2 | 200 | 크레딧 500 | 크레딧 2,500 |
| 3 | 300 | 부품 100 | 부품 500 |
| 4 | 400 | 대원 EXP 500 | 대원 EXP 2,500 |
| 5 | 500 | 전술 데이터 50 | 전술 데이터 250 |
| 6 | 600 | 크레딧 500 | 크레딧 2,500 |
| 7 | 700 | FREE 크리스탈 100 | FREE 크리스탈 500 |
| 8 | 800 | 크레딧 500 | 크레딧 2,500 |
| 9 | 900 | 부품 100 | 부품 500 |
| 10 | 1000 | 대원 EXP 500 | 대원 EXP 2,500 |
| 11 | 1100 | 전술 데이터 50 | 전술 데이터 250 |
| 12 | 1200 | 크레딧 500 | 크레딧 2,500 |
| 13 | 1300 | FREE 크리스탈 100 | FREE 크리스탈 500 |
| 14 | 1400 | 크레딧 500 | 크레딧 2,500 |
| 15 | 1500 | 부품 100 | 부품 500 |
| 16 | 1600 | 대원 EXP 500 | 대원 EXP 2,500 |
| 17 | 1700 | 전술 데이터 50 | 전술 데이터 250 |
| 18 | 1800 | 크레딧 500 | 크레딧 2,500 |
| 19 | 1900 | FREE 크리스탈 100 | FREE 크리스탈 500 |
| 20 | 2000 | 크레딧 500 | 크레딧 2,500 |
| 21 | 2100 | 부품 100 | 부품 500 |
| 22 | 2200 | 대원 EXP 500 | 대원 EXP 2,500 |
| 23 | 2300 | 전술 데이터 50 | 전술 데이터 250 |
| 24 | 2400 | 크레딧 500 | 크레딧 2,500 |
| 25 | 2500 | FREE 크리스탈 100 | FREE 크리스탈 500 |
| 26 | 2600 | 크레딧 500 | 크레딧 2,500 |
| 27 | 2700 | 부품 100 | 부품 500 |
| 28 | 2800 | 대원 EXP 500 | 대원 EXP 2,500 |
| 29 | 2900 | 전술 데이터 50 | 전술 데이터 250 |
| 30 | 3000 | 크레딧 500 | 크레딧 2,500 |

| 재화 | 무료 완주 | 유료 단독 | 구매자 합계 | 비교 |
| --- | ---: | ---: | ---: | --- |
| FREE 크리스탈 | 500 | 2,500 | 3,000 | 유료/무료5배, 합계6배 |
| 크레딧 | 5,000 | 25,000 | 30,000 | 유료/무료5배, 합계6배 |
| 부품 | 500 | 2,500 | 3,000 | 유료/무료5배, 합계6배 |
| 대원 EXP | 2,500 | 12,500 | 15,000 | 유료/무료5배, 합계6배 |
| 전술 데이터 | 250 | 1,250 | 1,500 | 유료/무료5배, 합계6배 |

실제 환산 가능한 항목도 출시가격 확인 전에는 조건부다. historical100 기준 패스
무료500은 정가권5장, 유료2,500은25장에 해당하는 크리스탈 수량이다. 일일 할인으로
각각10/50장의 구매력이 있지만 후자는 시즌의 일일 한도보다 많고 다른 수입과 한도를
공유하므로 **'패스에서 할인50장 지급'으로 표시하면 틀린다.** 재료/EXP/데이터/정예권의
환산 합계와 현금 대비 효율은 산정하지 않았다. 유료패스 크리스탈2,500도 PAID가 아니다.

<a id="source-c3ed6594fb1f2819-6-출석28칸과-통합-반복수입"></a>
#### 6. 출석28칸과 통합 반복수입

기존 제안 수량을 변경하지 않았다. 주별7칸은 FREE50 → 크레딧 → 부품 → EXP → 데이터
→ 크레딧 → 마일스톤 순서다. 각 주 크레딧(두 칸)은1000/1500/2000/2500,
부품50/75/100/125,EXP100/150/200/250,데이터25/40/55/70;
7/14/21칸은FREE100/150/200,28칸은정예 랜덤 확정권1.
합계650/14000/350/700/190/정예권1. 29–31번째 접속에 추가 출석칸·정예권 없음.

아래는28일 매일·주간일반4세트·보스 직접보상 제외·패스만렙의 **출석/미션/패스만** 합계다.
작전 본보상·오프라인·일회성·이벤트·보상우편·상점 재료구매는 섞지 않았다.

| 출처 | FREE | 크레딧 | 부품 | EXP | 데이터 | 정예권 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 출석 | 650 | 14000 | 350 | 700 | 190 | 1 |
| 일일28회 | 560 | 2800 | 140 | 1400 | 140 | 0 |
| 주간일반4회 | 600 | 2000 | 200 | 1200 | 80 | 0 |
| 무료 패스 | 500 | 5000 | 500 | 2500 | 250 | 0 |
| 무과금 합계 | 2310 | 23800 | 1190 | 5800 | 660 | 1 |
| 유료패스 추가 | 2500 | 25000 | 2500 | 12500 | 1250 | 0 |
| 패스 구매 합계 | 4810 | 48800 | 3690 | 18300 | 1910 | 1 |

<a id="source-c3ed6594fb1f2819-7-지급-시점-시뮬레이션"></a>
#### 7. 지급 시점 시뮬레이션

재현 명령: `python docs/ops/economy-model-20260908/model.py` (표준 Python만 사용).
[모델](../../../WorkspaceRepo/docs/ops/economy-model-20260908/model.py), [전체표](50_RELEASE_OPERATIONS_CONTRACTS.md#source-39523bef0fe14920),
[일자별 JSON 원장](../../../WorkspaceRepo/docs/ops/economy-model-20260908/results.json).

가정: 매 활성일 일반작전2회, 회당 서버인정10웨이브, 목표 달성 즉시 같은 날 지급,
획득레벨 즉시수령, 유료는 지정 구매일에 검증됐다고 가정, 처음 지갑0(명시적 보유PAID
사례 제외). 보스는 별도 사례만 XP50. 실제 플레이 시간/연료/기기·유료구매 검증은 아니다.
일일 할인부터1장 구매하고, `month_end_sweep` 사례만 그 달 마지막 접속일에 남은 금액을
정가100으로 교환한다. 최적화된 소비정책이 아니며 원장을 기준으로 실제 구매시점을 계산한다.
`discount_only`는 잔액을 다음달로 보존한다. 재화는 지급 전 선소비할 수 없다.

신규/복귀의 **주간진행0 시작**과 전월부터 이어진 기존 유저를 분리했다. 주간 미션은
월초에 초기화하지 않으므로 두 달 연결 계산에서는 과거 완료된 주간 보상을 다시 주지 않는다.
아래 첫 표는 28일 2027-02의0잔액·보스 없음·월말정가교환 사례다.

| 시나리오 | 접속 | XP/레벨 | FREE 수입 | 할인권+정가권 | 잔액 | 출석정예권 |
| --- | ---: | --- | ---: | --- | ---: | ---: |
| month_start_daily_free | 28 | 4600/30 | 2310 | 28+9 | 10 | 1 |
| month_start_daily_premium | 28 | 4600/30 | 4810 | 28+34 | 10 | 1 |
| return_day8_free | 21 | 3450/30 | 1970 | 21+9 | 20 | 0 |
| three_days_MWF_free | 12 | 3000/30 | 1540 | 12+9 | 40 | 0 |
| three_days_MWF_premium | 12 | 3000/30 | 4040 | 12+34 | 40 | 0 |
| five_days_weekdays_free | 20 | 3800/30 | 1900 | 20+9 | 0 | 0 |
| late_day15_free | 14 | 2300/23 | 1330 | 14+6 | 30 | 0 |
| late_day15_premium | 14 | 2300/23 | 3330 | 14+26 | 30 | 0 |
| late_day22_free | 7 | 1150/11 | 640 | 7+2 | 90 | 0 |
| late_day22_premium | 7 | 1150/11 | 1640 | 7+12 | 90 | 0 |

- 월초매일의 만렙은 이28일 사례에서17일. 구매해도 XP/완료일은 같다. 20일 중도구매도
  최종 획득재화가 첫날 구매와 동일하지만, 이전날의 구매력은 소급하지 않는다.
- 주3일은 월수금 고정·회당10웨이브 기준이다. 주3일이지만 회당5웨이브이면24레벨;
  로그인만 하면11레벨이다. '출석만으로 풀패스'나 모든3일 패턴 완주를 보장하지 않는다.
- 복귀8일차에는 남은21일로 패스는 완주해도 출석28칸은 못 채워 정예권0이다.
  신규15일/22일 시작은23/11레벨이며 유료도 미획득 레벨을 받지 못한다.
- 신규 일회성 기존 지휘권12장은 별도 열이다. 모델은 로그인5장+일회성 첫클리어
  1/1/2/3을 접속1/2/4/6/8회차에 받는 **예시 일정**만 사용한다. 실제 해금/첫클리어
  시점이 아니고, 반복수입·정예권·크리스탈 잔액에는 합산하지 않는다.

<a id="source-c3ed6594fb1f2819-달력283031일-및-월경계"></a>
##### 달력28/30/31일 및 월경계

모든 시작요일7개씩 총63 cold-start +63 continuing-month 조합을 계산했다.
기존 유저는 전월 한 달의 주간 진행/수령 기록을 워밍업한 뒤 해당월을 투영한다.

| 월길이 | 접속패턴 | 계속유저 XP 범위 | 레벨 | FREE 수입 범위 |
| ---: | --- | --- | --- | --- |
| 28 | 매일 | 4600–4600 | 30–30 | 2310–2310 |
| 28 | 월수금 | 3000–3000 | 30–30 | 1540–1540 |
| 28 | 평일5일 | 3800–3800 | 30–30 | 1900–1900 |
| 30 | 매일 | 4800–5250 | 30–30 | 2350–2500 |
| 30 | 월수금 | 3000–3550 | 30–30 | 1540–1710 |
| 30 | 평일5일 | 3800–4450 | 30–30 | 1900–2340 |
| 31 | 매일 | 4900–5350 | 30–30 | 2370–2520 |
| 31 | 월수금 | 3100–3650 | 30–30 | 1560–1880 |
| 31 | 평일5일 | 3900–4550 | 30–30 | 2120–2360 |

신규 월수금0진행 시작의 최저치는28일25레벨,30/31일26레벨이다. 경계 주간의 목표를
못 채운 영향이며, 계속유저 범위와 섞지 않았다. 주간 퀘스트 지급횟수는 '월4회'로
하드코딩하면 안 된다. XP는 목표가 서버에서 완료된 시점의 시즌에1회 귀속시켜, 뒤늦은
클라이언트 수령으로 이전시즌/다음시즌을 선택할 수 없게 한다.

실제2026-09→10 연속 매일·할인만 소비하면61개 할인권, FREE잔액1,820;
매 시즌 패스 구매 시 동일61개 할인권, FREE잔액6,820이다. 정예권은2장으로 별도다.
반면 월말 잔액을 정가로 거의 전부 쓰는 전략은 다음달 초 유동성을 줄인다. 모델의
연속2026-10 무료 사례는31일 접속해도 할인30회만 샀다. 총량이 충분해도 실제 지급시점과
지난 소비 때문에 하루를 놓칠 수 있으며 다음날에2장 구매로 보상하지 않는다.
31일 월에31회 할인을 쓰는 것과 월간출석28칸은 서로 다른 제한이다.

<a id="source-c3ed6594fb1f2819-중복이월-방지-계약"></a>
##### 중복·이월 방지 계약

- 이벤트 유일성은 account+serverEventId, 미션수령은 account+stableQuest+day/week,
  시즌XP는 account+season+sourceReceipt, 패스보상은 account+season+level+track.
  모드/역할/기기/세션/새idempotency key로 별도수령하지 못한다.
- 한 런으로 D2/D3/W2/W3가 달성되는 의도된 중첩과 같은 목표 재전송은 구분한다.
  이 표 외 런XP를 추가하지 않는다. 보스 Participation 본보상을 W4가 재지급하지 않는다.
- 월출석은 account+campaign/month+serverday와 순차칸 원자를 함께 보장한다.
  월1일04 전 방문과 이후 방문은 서로 다른 캠페인; 한쪽 재전송이 반대쪽 보상을 만들면 안 된다.
- 일일 특가는 account+stableOfferFamily+DB KST04day 한도1, 같은 배치에 차감/지급/
  원장/영수증. 전일 성공키 재전송은 전일 영수증을 돌려주고 오늘한도는 소모하지 않는다.
- 시즌XP/주간진행을 UI 수령일에 옮기지 않는다. 보상 정산 실패는 durable receipt 재시도,
  수령 버튼 여러 번/월 경계/중도구매로 FREE·PREMIUM을 중복 적립하지 않는다.
- 기존의 런XP-only 및 명시 claim API가 이 모든 미션/자동정산 기능을 구현했다고
  주장하지 않는다. 계산 모델의 중복검증은 데이터베이스 동시성 테스트가 아니다.

<a id="source-c3ed6594fb1f2819-8-수입-대-성장연구상점-소비-검토"></a>
#### 8. 수입 대 성장·연구·상점 소비 검토

서버 활성 강화/연구 비용표를 찾지 못했으므로 재료를 현금/크리스탈로 바꾸어 평가하지 않았다.
Unity 로컬 TrainCoreUpgradeCostCatalog는 비교 참고값이다. 총 L1→10=10,200크레딧,
5,090부품,특수코어1. 이 파일이 배포된 서버 가격권위라는 증거는 없다.

출석/퀘스트/패스만 놓고0재료·L1에서 모든 부품을 코어에 우선 쓴다고 가정하면:

| 구분 | 크레딧/부품 공급 | 로컬 코어표 상한 | 해석 |
| --- | --- | --- | --- |
| 무료 | 23,800 / 1,190 | L5(부품790누적), L6까지50부품 부족 | 크레딧보다 부품이 제한 |
| 유료 포함 | 48,800 / 3,690 | L9(부품3690누적), L10은부품1400+코어1부족 | 유료5배 보상이 실제 진행 격차를 키울 수 있음 |

이 비교는 기존 재료잔액·다른 강화/연구 소비·작전/오프라인 수입·해금 조건을 제외했다.
EXP와 데이터는 서버 권위 소모곡선이 없어 몇 레벨/연구회차인지 판정 불가하다.

추가로 실제 소스의 오프라인 상수는 시간당 크레딧100/부품50/데이터30/EXP20,
기획 draft는24/2/1/80으로 서로 다르다. 어느 쪽도 임의 교체하지 않았다.
28일×하루4시간 수령 가정에서 부품만 **5,600 대224**로25배 차이가 난다.
현재 반복수입표에 이를 얹으면 무료부품6,790까지 늘어 로컬 코어 부품 제한을 넘는다.
수령 간격/진행도별 cap/효율/활성 설정·실제 수령 가능 여부를 확인해야 한다.

참고용 단위 예산(실제 플레이 결과가 아님): 일2회×28일=56런을 모두 기본 작전
승리1배로 단순화하고, 오프라인4시간/일을 더하면 아래처럼 기준 선택이 크게 바뀐다.
작전 보상은 서버의 활성 balanceConfig를 읽으므로 이 상수표를 현재 live 값으로 단정하지 않는다.

| 무료 반복 합계 시나리오 | 크레딧 | 부품 | EXP | 데이터 |
| --- | ---: | ---: | ---: | ---: |
| 제안 출석/퀘스트/패스만 | 23800 | 1190 | 5800 | 660 |
| 위+소스 기본런(300credit/700EXP)56회+소스 offline112h | 51800 | 6790 | 47240 | 4020 |
| 위+draft기본런(120/8/700/4)56회+draft offline112h | 33208 | 1862 | 53960 | 996 |

승패/난도/성공웨이브/해금/보유연료/보스별 배수를 생략한 민감도이며, 미션 모델의
10웨이브 런과 '15웨이브 승리1배' 참고 예산을 같은 실측 시나리오로 섞지 않는다.
오프라인에는 어느 기준에도 크리스탈을 추가하지 않았다.

크레딧 기본재료 상점은 역할만 승인됐다. 가격/묶음/일한도는 아직 없다. 예를 들어
부품10개/500크레딧/일1회라는 **가정**은28일 최대부품280·크레딧14,000을 이동시킨다.
이는 새 출시상품 추천 확정이 아니며 코어 비용과 동시에 소비해야 하므로 크레딧 전액을
부품과 강화 양쪽에 중복 사용한 것처럼 계산하면 안 된다. 가격500/1000/2000 민감도는
동일280부품에14,000/28,000/56,000크레딧으로 바뀌며 실제 상품표 없이 과잉/부족 결론을
내릴 수 없다. 크레딧→크리스탈/일반권 전환은 이 검토에서도 제외했다.

<a id="source-c3ed6594fb1f2819-9-과다-수급희소성모집-위험"></a>
#### 9. 과다 수급·희소성·모집 위험

- 무료 패스500을 유지한 상태에서 유료2,500 추가는28일 주 모델에서 일반권25장을
  더 늘린다(일일할인 한도가 이미 같은28개이므로 추가분은정가). 전체일반권37→62이며
  전체 수급은5배가 아니다. 재료는 무료전체1190→3690부품 등으로 커져 성장 격차 점검이 필요하다.
- 같은 배너/동일 pity 상태에서 실제 코드는 정예미획득30,픽업미획득60,희귀이상미획득10
  카운터를 사용한다. 우선순위픽업→정예→희귀, 해당등급획득 시 관련 카운터 초기화,
  픽업은픽업정예일 때만 초기화된다. 표본당 일반 정예4%만 곱해 기대 정예수를 제시하면
  보장/카운터/기존상태/배너 이동을 무시한다. 이번 계산은 **모집권 수만** 계산했다.
- 출석 정예권은 별도 아이템/보장권 계약으로 남겼다. 일반권과 동일가치, pity진행,
  픽업대상/중복환원/평균 기대값을 추정하지 않았다.
- 기존 로그인5장+첫클리어1/1/2/3=12장은 계정 이력 기반 일회성이다. firstClear는
  실제 operation/risk 유일키를 따라야 하며 모든 난도 합계12장이라고 확대하지 않는다.
  향후500+100/100/200/300크리스탈로 바꾼다면 **동일 지급 건을 대체**해야 한다.
  12장과1,200을 동시에 합산하거나 복귀자에게 로그인5장을 재지급하지 않는다.
- 현금가격7,900원이 효율5배를 보장한다는 주장은 하지 않는다.5배는 같은재화 보상량
  비교이며 실결제 상품가치/제작시간/재료시장가격은 검증하지 않았다.

<a id="source-c3ed6594fb1f2819-10-출석-회복-민감도미승인"></a>
#### 10. 출석 회복 민감도(미승인)

회복0/20/50/100크리스탈, 캠페인한도0/2/4를 각각 계산했다. 달력1–4일을 놓치고
5–28일 자연접속24회,28일의 현재 방문 후 과거 놓친 날짜만 복구한다. 시작잔액0,
출석크리스탈만 사용, 매일 가능하면 할인1개, 퀘스트/패스는 제외한 분리 실험이다.
회복 처리 순서는 마지막날 할인보다 먼저이며, 재화가 없으면 일부만 복구한다.

| 한도/비용 | 실제복구 | 당월할인권 | 잔액 | 정예권 |
| --- | ---: | ---: | ---: | ---: |
| 0 / 0 | 0 | 12 | 50 | 0 |
| 4 / 0 | 4 | 12 | 50 | 1 |
| 4 / 20 | 4 | 11 | 20 | 1 |
| 4 / 50 | 2 | 11 | 0 | 0 |
| 4 / 100 | 1 | 11 | 0 | 0 |

회복은 과거 할인 슬롯을 주지 않으며 XP/퀘스트 완료를 소급하지 않는 안이다.
비용20·한도4에서80크리스탈로 정예권 접근성이 달라지는 점은 검토 위험이지 권장가격
확정이 아니다. 회복을 먼저 사용할지 일일할인을 먼저 사용할지도 실제 수급을 바꾼다.
늦은 가입 전 날짜를 '놓친 날'로 인정할지는 별도 정책이며 이 실험은1일에 캠페인에
등록됐지만1–4일 미접속한 계정만 가정한다. 회복 미설정은 off를 유지한다.

<a id="source-c3ed6594fb1f2819-11-검증-결과와-남은-결정"></a>
#### 11. 검증 결과와 남은 결정

실행 결과:100시나리오,63신규/복귀 시작요일 조합,63계속유저 조합,18기존계정 월투영.
각 일자 크리스탈 수입=잔액+지출 보존, 잔액음수 금지,일할인1회,XP/보상 receipt 유일,
FREE-first/PAID보유금 소진,30개모든누적유료5배,20일소급구매 최종량·XP일치,
2개월 일회성12장만 지급,04시논리일 전환 등을 assert 및 독립 원장 교차검증으로 확인했다.
독립 리뷰가100개원장과81개월투영의 산술을 교차확인했다. 초기 회복 모델의 놓친날
없는 전진 지급과 cold-start/계속유저 혼동을 수정한 뒤 검토에서 중대한 문제는 없었다.
이는 분석 모델 검증이지 새로운 API 구현/게임 플레이/DB경합/가격 활성 검증이 아니다.

추천 승인 단위: (1) 월시즌/XP/표 및 유료단독5배 정의, (2) 미션XP 연결·정산 규칙,
(3) 서버성장/연구·오프라인 기준과 재료 상점가격, (4) 회복정책/정예권 상세,
(5) 현금가격·Store·환불 조건은 별도. 이번 요청에서 어느 정책도seed/배포하지 않았다.
무료표를 낮춰 비율을 맞추지 않으며, 성장격차가 과도하면 승인된 전체 경제/성장곡선과
패스의 동종5배 구조를 함께 재검토해야 한다. 개인별 과금권위/임의재료가치를 만들지 않는다.

<a id="source-c3ed6594fb1f2819-소스와-재현-범위"></a>
#### 소스와 재현 범위

서버: C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/attendance-pass-20260907/server
- apps/game-api/src/activity-policy.ts: cumulative XP/04시/window/현재runXp지원
- activity-service.ts: 실제 runXp/level·track·entitlement·receipt 검증
- crystal-wallet.ts / crystal-service.ts / crystal-routes.ts: price/FREE-first/원장/중복방지
- index.ts: launchLoginBonus5, gacha보장/카운터, runRewards 활성설정 선택, offline 상수 경로
- packages/balance/src/index.ts / gacha-policy.ts: 기본/초안 보상과4% 풀
- packages/db/migrations/0022_attendance_pass.sql /0023_crystal_wallet.sql: 빈 정책테이블

Unity 로컬 참고: E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/Train/TrainCoreUpgradeCostCatalog.cs.
Canonical 기준: crystal-source-policy.md, attendance-shop-contract-audit-2026-09-08.md,
economy-balance-launch-draft.md, ../design/online-coop-mode-policy.md, ../handoffs/PLAN.md,
../handoffs/LAST_RAIL_SERVER_1.md. 과거 문서의 local-only 배포 문구는 계승 기록이 우선한다.
model.py/results.json/tables.md는 네트워크·계정·비밀정보 없는 로컬 설계 자료다.

<!-- source-body:source-c3ed6594fb1f2819:end -->


<a id="source-9d625ca45a7d9b6a"></a>
## Launch Economy Mutation Acceptance - 2026-09-09

원출처: `WorkspaceRepo/docs/ops/launch-economy-mutation-acceptance-20260909.md`  
원문 SHA-256: `3071e0b90cb6753b5fc8bef601f34875708329b61cb499f97f94b96c63ff1eca`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-9d625ca45a7d9b6a:start -->
<a id="source-9d625ca45a7d9b6a-launch-economy-mutation-acceptance---2026-09-09"></a>
### Launch Economy Mutation Acceptance - 2026-09-09

User explicitly approved actual growth-target connection, first-clear duplicate
checks and existing recruitment reward source conversion. Server task SERVER_1
owns implementation; Plan owns numeric and source-budget policy. Baseline config
8ca751b and owner-reported411tests do NOT cover the following mutations yet.

<a id="source-9d625ca45a7d9b6a-scope-and-order"></a>
#### Scope And Order

1. Immutable server-authoritative economy version/hash and selected cost.
2. TrainingCore actual owned operator/slots:level-up,sync-up,replace with atomic
   EXP debit/investedEXP/progression/receipt; actual investment refund100percent.
3. Train/skill/promotion actual owned targets and additive local persistence.
4. First-clear restricted-source entitlements and future-unclaimed-source FREE
   crystal replacement. Preserve run-start old-version calculation and replay.

Equipment/research without an authoritative target catalog remains unavailable;
numeric cost profile does not authorize arbitrary client IDs, effects or levels.
Do not fabricate content IDs to turn a blocked target test green.

<a id="source-9d625ca45a7d9b6a-source-semantics"></a>
#### Source Semantics

- Ordinary first-clear materials retain lawful account/operation/risk identity.
- Limited launch recruitment/core source tracks account/stable source family,
  not new risk/version/run/request identity. Fixed launch recruitment total12
  tickets or replacement1200FREEcrystals, not both. Fixed core source budget3.
- Audit existing receipts across all risks before deriving consumed state.
  Previously rewarded sources remain consumed; ambiguous/missing evidence must
  not cause a guessed extra award. Provide reconciliation blocker if needed.
- Conversion is not inventory exchange: preserve existing owned tickets and
  prior credited currency. Only future unclaimed equivalent sources change.
- Partial launch login claims preserve already received days/source slots.
  Old pinned run entitlement and new claim paths share duplicate protection.
- Missing wallet policy fails closed before any partial source/grant mutation.
  Do not rewrite old receipts or old run response to claim new crystal payout.

<a id="source-9d625ca45a7d9b6a-required-local-db-evidence"></a>
#### Required Local DB Evidence

| Test | Required invariant | Current status |
| --- | --- | --- |
| Successful owned-target upgrade | Exact server-selected multi-resource debit and level/ledgers/receipt commit together | Requested |
| Unknown/non-owned target | No balance, level or ledger change | Requested |
| Max level/open run | Server rejection, no spend | Requested |
| Insufficient one resource | Entire transaction unchanged | Requested |
| Same key same request replay | One debit/grant, original receipt returned | Requested |
| Same key altered payload | Conflict, no additional mutation | Requested |
| Successful replay after version changes | Original receipt before stale-version rejection | Requested |
| Concurrent different requests | No double spend beyond shared balance | Requested |
| TrainingCore replace | Refund stored investedEXP, not new curve; no reinvest | Requested |
| First-clear retry/newrun/same risk | Limited source granted at most once | Requested |
| First-clear other risk | Ordinary material semantics retained; limited source not duplicated | Requested |
| Ticket source already consumed | No crystal top-up or ticket removal | Requested |
| Partially claimed login source | Only unclaimed source components converted | Requested |
| Old pinned run versus new version claim | One economic benefit per source family | Requested |
| Missing wallet policy/injected failure | No partial grant, charge or consumed marker | Requested |

<a id="source-9d625ca45a7d9b6a-boundaries-and-reporting"></a>
#### Boundaries And Reporting

Local implementation/tests/local migrations approved. No remote deployment,
remote seed, historical compensation, balance rewrite or Store activation.
Preserve dirty/untracked work and inherited approval denials. Canonical handoff
is normal persistence; do not retry a rejected direct-message delivery.
Report separate status for config schema, real debit, reward route, catalogs,
DB tests and remote release. Existing411 tests are configuration baseline only;
do not copy that count into a new mutation completion claim.

Dispatched to01a079e8-858f-7b02-800d-2c5545324967 by send_message_to_thread.
Plan has not run these DB tests; all rows remain Requested until owner evidence.
Local Plan acceptance document uncommitted. Automatic monitoring remains deleted.

<!-- source-body:source-9d625ca45a7d9b6a:end -->


<a id="source-60149959551b12e2"></a>
## Launch Economy V1 - 2026-09-09

원출처: `WorkspaceRepo/docs/ops/launch-economy-v1.md`  
원문 SHA-256: `1be1c9cbbde529e8a284b034dc9db9219a558250b5b4de073ba3bf356ce11ea6`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-60149959551b12e2:start -->
<a id="source-60149959551b12e2-launch-economy-v1---2026-09-09"></a>
### Launch Economy V1 - 2026-09-09

<a id="source-60149959551b12e2-authority-and-scope"></a>
#### Authority And Scope

User authorized finishing costs, comparing income, fixing numbers, and server
implementation/testing. Plan selects implementation baseline
act1.economy.2026-09-09.001 in launch-economy-v1.json. This supersedes the
trial24/2/1/20 and five-core95000 proposal for this implementation scope.
It is NOT proof of deployment, complete combat balance or every future chapter's
content budget. JSON stores resource vectors, not new currencies or target IDs.
Retain server currency mapping and existing domain identifiers.

<a id="source-60149959551b12e2-numeric-decisions"></a>
#### Numeric Decisions

- Offline hourly credits24/parts2/data1/operatorEXP20. Existing story multipliers
  and storage caps unchanged. No crystal, special core or recruitment currency.
- Ordinary materials use six-operation draft table and existing ordinary/risk
  BPS. EXP follows recovered architecture policy: riskI-V700/900/1100/1300/1500,
  no operation multiplier and no extra risk multiplier. Failed ordinary run EXP
  floor(fullEXP*8000*clearedWaves/10000/15), abandon/zero waves0, full success100%.
  Keep material failure outcome contract distinct from EXP formula.
- Story raid boss success uses explicitly final repeat648credits/50parts/28data/
  1600EXP; these are final amounts, do not multiply again. Failure0reward and
  existing stamina refund. World Boss has no difficulty/EXP reward and is not
  an ordinary operation or story raid reward path.
- Preserve approved attendance28cells,650FREEcrystals,14000credits,350parts,
  700EXP,190data,one elite ticket. Preserve50level pass and exact5x premium extra,
  daily200/passweekly600 XP, 17day fastest full-pass arithmetic.
- Daily direct totals20FREEcrystals/100credits/5parts/5data/50EXP; ordinary
  weekly150FREEcrystals/500credits/50parts/20data/300EXP fixed for this table.
  Modes satisfy same objectives, no doubled solo/co-op grants. Weekly World
  Boss participation is its score track, not another identical quest reward.
- Ticket normal100crystals, daily50 once per account server day, no credit
  recruitment exchange. FREE-first spend; these counts are not raw paid crystals.

<a id="source-60149959551b12e2-full-growth-cost-table"></a>
#### Full Growth Cost Table

All step costs, not cumulative. No failure chance or mandatory paid currency.
Existing investments/refunds retain actual historical paid EXP, never revalue
old investments at a new curve. Do not downgrade existing accounts or increase
their stored levels automatically at config activation.

| System | Step | Credits | Parts | Data | Special cores |
| --- | --- | --- | --- | --- | --- |
| Equipment | 1to2 | 200 | 60 | 0 | 0 |
| Equipment | 2to3 | 400 | 120 | 0 | 0 |
| Equipment | 3to4 | 800 | 240 | 0 | 0 |
| Equipment | 4to5 | 1600 | 480 | 0 | 0 |
| Skill | 1to2 | 160 | 0 | 80 | 0 |
| Skill | 2to3 | 320 | 0 | 160 | 0 |
| Skill | 3to4 | 640 | 0 | 320 | 0 |
| Skill | 4to5 | 1920 | 0 | 960 | 0 |
| Promotion | 0toI | 640 | 0 | 320 | 1 |
| Promotion | ItoII | 1280 | 0 | 640 | 1 |
| Promotion | IItoIII | 2240 | 0 | 1120 | 1 |
| Research | 0to1 | 200 | 0 | 20 | 0 |
| Research | 1to2 | 400 | 0 | 40 | 0 |
| Research | 2to3 | 800 | 0 | 80 | 0 |
| Research | 3to4 | 1600 | 0 | 160 | 0 |
| Research | 4to5 | 3200 | 0 | 320 | 0 |

Train core preserves local nine-step table, now versioned server baseline:
credits[200,300,450,650,900,1200,1600,2100,2800],
parts[100,150,220,320,450,600,800,1050,1400],specialcore1only9to10.

Permanent operator EXP only: levels1to10 steps40/50/60/80/100/120/150/180/220;
10to20:200each;20to30:300each;30to40:350each;40to50:450each.
One-slot cumulative10/20/30/40/50:1000/3000/6000/9500/14000;
five-slot cumulative5000/15000/30000/47500/70000. Replaces95000 proposal because
that model wrongly used operation-scaled EXP against existing architecture.

Equipment is a cost profile for existing approved upgradeable targets, not an
instruction to invent four new items or replace a higher existing cap. Four
owned items in budget are a capacity assumption. Bind only after authoritative
catalog validation. Research is cost-only until real nodes/effects/prerequisites
are identified. No anonymous research purchase, fabricated effect or generic
client-selected target may spend currency. Missing catalog is an integration
blocker, not grounds to label research delivered. Skill/promotion step split
preserves old reference totals3040/1520 and4160/2080/core3.

<a id="source-60149959551b12e2-income-and-spending-evidence"></a>
#### Income And Spending Evidence

Run node docs/ops/launch-economy-validation-20260909.mjs; output JSON is generated
at launch-economy-validation-20260909.json.18 cases:3progress paces*3claim counts*
2pass tracks. Every purchase checks one shared wallet; gross=spent+remaining,
no negative balances. Lowest-first5EXP slots, train then equipment then skills.

Standard28days,2claims/day, daily2ordinary full clears, riskI days1-7 thenII,
23hours offline split evenly. Full attendance/quests/pass and exogenous story
dates as previous models. Actual fuel/stat/entry progression not simulated.
Goals restrict model purchase planner, NOT time gates in actual game.

| 28day result | Free | Premium buyer |
| --- | --- | --- |
| Gross credits | 62716 | 100216 |
| Gross parts | 4340 | 8090 |
| Gross data | 2244 | 3994 |
| Gross EXP | 73650 | 91150 |
| Five-slot Lv50 day | 27 | 22 |
| Ordinary tickets after specified exchange schedule | 39 | 77 |

Free baseline purchases:train8,4equipment3,5skills3,5core50; spent10100credits/
3360parts/1200data/70000EXP. Remainder52616credits/980parts/1044data/3650EXP.
Premium planner uses same target stop, not proof premium cannot upgrade further.
Slow progression free ends45-49dependingclaims; standard1claim ends47. Never
claim50within28days for every player. Early claims/fuel/real combat remain QA.

<a id="source-60149959551b12e2-other-sources-and-exceptions"></a>
#### Other Sources And Exceptions

- Existing seven-operation first-clear material budget at one risk:3510credits/
  248parts/147data; specialcores3 in separate original-source entitlements.
  Preserve server first-clear identity; do not invent a new per-risk core
  duplication. Owner must reconcile actual risk/operation claim identity before
  activation and report maximum attainable first-clear budget across risks.
- Historical12 launch tickets must be replaced by1200FREEcrystals at agreed
  exchange value, NOT granted alongside them. Five-login/7story source identities
  remain idempotent. Existing claimed sources do not get a retroactive second
  reward. Actual source timing/migration needs server evidence. Recurring39/77
  counts exclude this finite onboarding budget and guaranteed elite ticket.
- World Boss existing weekly score/rank tables retained as numeric envelopes:
  4events participation adds1200/80/40, maximum score+best rank adds20800/1360/680.
  No double solo/co-op or repeated-partner payout, no EXP/crystals/cores.
  Threshold calibration is combat QA, not certified by budget arithmetic.
- Promotion2steps one operator costs1920credits/960data/core2. Three existing
  research nodes at rank2 would cost1800credits/180data, conditional on catalog.
  Free residual data1044 plus first-clear147 can cover1140total; core entitlement
  and actual availability still gated. These are separate scenarios, not assumed
  automatic debits/grants. Core scarcity is explicit; no invented repeat source.
- Credit surplus is visible, not artificially removed with repair taxes or
  unapproved shops. Full collection/multiple-loadout spending is not modeled.
- Extra chapters/advertisements/live events/unconfigured shop products/purchase
  packs are not silently included at zero and declared balanced. Require explicit
  configured budget before enabling. No new Store price or live economy opening.

<a id="source-60149959551b12e2-server-acceptance-gates"></a>
#### Server Acceptance Gates

1. Strict versioned schema, whole numbers, max/min and target allowlists.
2. Separate quoted policy version/account revision from client-submitted costs.
3. Atomic balances+growth+ledgers+idempotency, insufficient funds rolls all back;
   concurrent duplicate/different requests, open-run lock, mismatched revisions.
4. Preserve refund investedEXP and source-entitlement identity. Reject unknown
   targets and unavailable catalogs without charging. Test all max-level paths.
5. Income config uses one authoritative version; retirement of former defaults
   is deliberate. Ordinary EXP operation invariance tested. No double scaling.
6. Local suite/type/lint/migrations and documented compatibility. Remote rollout,
   snapshot/backup/release version and actual device validation separately scoped.

Status: numeric baseline selected; arithmetic checksPASS18cases. Server config
implementation in progress. Full gameplay economic signoff NOT complete while
catalogs/extra source identities and runtime tests remain unverified.

<!-- source-body:source-60149959551b12e2:end -->


<a id="source-02711d8c0e43e1fa"></a>
## Low Activity Scenario Design

원출처: `WorkspaceRepo/docs/ops/low-activity-scenario-design-20260909.md`  
원문 SHA-256: `97799322c2047307bee1fc39eef20650e34b517dd8f98c52a7fb4c8ad50d2208`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-02711d8c0e43e1fa:start -->
<a id="source-02711d8c0e43e1fa-low-activity-scenario-design"></a>
### Low Activity Scenario Design

Plan,2026-09-09. Scenario specification plus actual pass arithmetic, not full wallet
or combat simulation. Sources: pass-progression-policy.md, attendance-recovery-
proposal-20260909.md, offline-reward-operations-policy.md and launch-economy-v1.json.

Observe first28 service days from aligned season/cycle start, account eligible day1.
All active sessions occur20:00KST in their service day; reset04:00. No recovery buys,
ads, crystal refills or extra XP. Runs below are assumed acknowledged full15wave
operations, not guaranteed affordable/clearable events. No simultaneous mode grants.

| Case | Active service days | Assumed runs per active day |
| --- | --- | ---: |
| Four days/week | Block offsets1,3,5,7 |1|
| Two days/week | Block offsets3,7 |2|
| One week absent | Days1..7 and15..28 |2|

Increment sequential attendance cell only on an active eligible day; never use
calendar day modulo7 to select rewards. Cap at28, no phantom catchup. Recovered
cells, if separately modeled later, do not count as login/weekly attendance or XP.
Do not assume21natural+7recoveries is affordable or within every month's eligibility.

Daily pass XP uses actual login40/first end80/second end80. Per season7day block,
grant each weekly200 once on reaching3distinct attendance days,6ends or60waves.
Counts reset at block boundary, missed daily XP not carried. Free/premium same XP.
Ordinary weekly quests are a different Monday04 calendar; do not derive their rewards
from this season-block arithmetic without a real calendar and objective definitions.

<a id="source-02711d8c0e43e1fa-actual-arithmetic-results"></a>
#### Actual Arithmetic Results

Node inline calculation iterated all28days, resets counters per block, counts full
waves and records first day reaching5000XP. Output:

| Case | Natural attendance cells | PassXP | Level at28 | First50day |
| --- | ---: | ---: | ---: | --- |
| Four days/week |16|3520|35|Not reached|
| Two days/week |8|2400|24|Not reached|
| One week absent |21|6000|50|24|

Four-day case earns daily16*120=1920 plus4*(attendance200+waves200)=1600; no six-end
weekly reward. Two-day case earns8*200=1600 plus4*200waves=800; no three-day/six-end
reward. Week-absence case earns21*200+3*600=6000. Excess XP is arithmetic, not a new
post50 reward. Do not call this complete player simulation or promise day24completion.

<a id="source-02711d8c0e43e1fa-next-cashflow-requirements"></a>
#### Next Cashflow Requirements

No reward on inactive days except offline time accumulating to existing cap. At
actual return claim once using real elapsed time since successful claim; never sum
one daily cap for each missed day. First-account baseline time must be explicit.
Story progression must come from accepted modeled clears or a separately disclosed
active-day schedule; do not inherit daily-player milestone dates without adjustment.
New progress affects rewards only under actual server snapshot policy, not by
retroactively rewriting missed days. Missing fuel/entry data blocks feasibility claims.

Compute actual attained pass-level reward cells, not full50track; attendance amounts
come from earned sequential cells. Couple costs and claims in chronological order.
Then compare no car/first Medical/all options and report deferred purchases, growth
delay and unavailable unlocks. Do not invent weaker-player bonuses, automatic catchup
XP or free premium compensation to force every activity level to50/28attendance.

Policy recommendation: keep reduced participation's earned progress without resetting
it within the cycle; do not promise maximum rewards for any login frequency. No
new gameplay policy or price activated here. Full low-activity cashflow not run.


<a id="source-02711d8c0e43e1fa-session-cashflow-detail-proposal--2026-09-10"></a>
#### Session Cashflow Detail Proposal — 2026-09-10

Plan_1. 상세설계 제안이며 기존 수치/보상/런타임 정책의 추가 승인이 아니다.
기존 3개 활동 패턴과 패스 계산을 보존하고, 실제 접속 때 무엇을 받을 수 있고
어느 시점에 성장/선택 객차를 살 수 있는지 비교하는 다음 분석 규격이다.

<a id="source-02711d8c0e43e1fa-계산-방식-선택"></a>
##### 계산 방식 선택

매일 정액 수입을 먼저 합산하는 기존 방식은 기준 결과 재현에는 적합하지만
결석·수령 누락·당일 해금 전 구매를 구분하지 못한다. 모든 전투를 실제로
시뮬레이션하는 방식은 전투/연료 계약이 필요하다. 우선은 접속·수령·종료·구매를
시간순 사건으로 기록하는 조건부 회계모델을 권장한다. 실제 클리어 가능성은
별도 검증이며 이 모델의 통과 조건에 끼워 넣지 않는다.

<a id="source-02711d8c0e43e1fa-입력과-비교군"></a>
##### 입력과 비교군

- 실제 기준 월/연도와 Asia/Seoul 04:00 경계, 계정 생성 시각, 관측 종료 시각을
  명시한다. 28일 관측과 달력 월말은 다르다. 월요일 일반 주간과 시즌 상대
  7일 패스 주간을 따로 계산한다. 달력 미지정이면 일반 주간 보상을 확정하지 않는다.
- 계정 생성은 서비스 day1 20:00, 첫 성공 수령 전 기준은 생성 시각으로 두는
  분석 후보. 첫 접속 day3인 패턴은 생성 이후 실제 경과시간만 사용한다.
  미접속 중 자동 수령은 없다. 첫날 즉시 수령은 최소시간 미달 가능성을 유지한다.
- 비교군은 기존 활동3패턴 × 무료/프리미엄2 × 객차구매3(없음/Medical만/전체)
  =18조건. 프리미엄은 관측 시작부터 해당 시즌 권리를 보유한 가정이며 현금
  구매/환불 모델이 아니다. 희망 프리미엄 전 구간 보상을 미리 지급하지 않는다.
- 활동일마다 첫 접속20:00에 오프라인 보상 수령을 시도한다. 전투 시작/종료는
  입력 manifest의 실제 또는 명시한 가정 시각을 사용하며 같은20:00에 모든
  사건을 겹쳐 놓지 않는다. 종료시각 미입력은 전투시간/연료 검증 불가로 표시한다.
- 원본 일차별 fast/standard/slow 해금표를 그대로 재사용하지 않는다. 분석용
  진행표는 활동 회차별 성공 run과 operation ID를 명시하고, 해당 보상/해금은
  그 run이 끝난 뒤에만 반영한다. 가정 클리어는 실제 도달 증거가 아니다.
- 난이도는 우선 동일한 접근 가능 Risk I 비교를 후보로 둔다. 실제 접근성이
  확인되지 않은 경우 synthetic 표시를 유지한다. 결석해도 day8부터 Risk II로
  자동 변경하는 기존 회계 가정을 신규 모델에 넣지 않는다.

<a id="source-02711d8c0e43e1fa-권장-사건-순서"></a>
##### 권장 사건 순서

| 순서 | 잔액/상태 반영 | 지켜야 할 경계 |
| --- | --- | --- |
| 1. 접속 | 서버일 판정과 자연출석 1칸, 해당 로그인 목표 | 출석복구 미포함. 같은 날 재접속은 중복 칸/XP 없음 |
| 2. 수령 | 현재 서버 진행도/수령 기준으로 오프라인 보상, 이미 획득한 패스/퀘스트 보상 | 최소시간 미달/실패는 잔액·성공수령 기준을 바꾸지 않음 |
| 3. 전투 전 성장 | 수령 완료 잔액으로 가능한 성장 | 이후 전투 보상을 먼저 빌려 쓰지 않음; 지출/성장 상한은 명시한 분석 전략 |
| 4. 전투 | 현재 보유/해금으로 입장, 종료 결과·진행·보상·목표를 차례로 반영 | 현재 미션이 줄 객차/슬롯을 그 미션 입장에 쓰지 않음 |
| 5. 종료 후 정산 | 방금 획득한 패스/퀘스트 보상을 수령한 뒤 성장 | 일/주 경계는 사건별 판정; 새 레벨 보상만, 같은 receipt 재지급 금지 |
| 6. 선택 객차 | 성장 후 남은 크레딧에서 해금된 후보를 명시 순서대로 구매 | 부족하면 다음 실제 구매 기회로 보류. 구매가 장착·슬롯 증가를 뜻하지 않음 |

한 세션에 2run이 있으면 4~6을 run별 반복한다. 비교의 기본 전략은 성장 우선,
객차 순서는 Medical → 두 번째 Gun → 두 번째 Defense → 두 번째 Medical이다.
앞 항목이 비싸서 미구매여도 독립적으로 해금·소유조건을 만족한 뒤 항목은 검토한다.
두 번째 Medical은 첫 Medical 소유를 요구한다. 기본 전략은 플레이어 강제가 아니다.
장비4개 가정은 실제 catalog가 없으므로 별도 synthetic 비용 비교로 표시하며,
그 조건을 실제 장비 성장 완료로 보고하지 않는다. 비교군 사이 성장 전략은 동일하다.

<a id="source-02711d8c0e43e1fa-오프라인-수령-시-진행도-변화"></a>
##### 오프라인 수령 시 진행도 변화

기존 offline-reward-operations-policy.md는 현재 서버 진행도와 마지막 성공
수령 시각을 사용하며, preview 후 스토리 진전으로 claim 입력이 달라질 수 있음을
허용한다. 따라서 이전 문단의 '과거를 재작성하지 않음'은 이미 확정한 수령 receipt를
다시 계산하지 않는다는 뜻으로 한정한다. 미수령 누적시간을 옛 진행도별 구간으로
나누는 새 정책을 도입하지 않는다. 실제 버전의 claim 계산 입력은 구현 전 재대조한다.

예: cap6시간, 마지막 성공수령 후72시간이라면 다음 수령의 시간 입력은 최대6시간이다.
결석3일 ×6시간으로18시간을 지급하지 않는다. 단위 예시일 뿐 이번에 새 계산을
실행한 결과는 아니다. 재접속 후 스토리를 먼저 깨고 수령하는 순서는 별도 민감도
비교로 추가할 수 있으나 기본18조건의 순서를 결과에 유리하게 바꾸지 않는다.

<a id="source-02711d8c0e43e1fa-결과-표와-미확인-입력-처리"></a>
##### 결과 표와 미확인 입력 처리

각 계정에 자연출석 칸수, 획득 XP/레벨, 자원별 수입·지출·잔액, 성공수령 시각과
적용 cap/진행도, 객차별 해금·첫 구매가능·실제구매 시각을 기록한다. 구매 보류는
미해금/선행객차 미보유/크레딧 부족으로 구분한다. 미구매는 day0이 아니라 미도달이다.
성장 지연은 같은 활동/프리미엄 조건의 객차 미구매군과 동일 사건 직후 비교한다.
계정 생성 후 일수와 실제 활동 회차를 함께 보여 결석일과 구매 경쟁을 혼동하지 않는다.

일반 퀘스트는 승인된 총액만으로 모두 완료 처리하지 않는다. 실제 objective 조건과
수락 event가 없으면 그 수입은 미확인으로 두고, 확정한 출처만의 subtotal을 별도 낸다.
미확인 수입을0으로 놓은 전체경제 PASS나 과거 정액 보상을 임의 삽입한 결과는 금지한다.
입장비/재생·보유/전투 가능성 입력이 없으면 회계 조건부 결과와 해당 미검증을 분리한다.
replay/실패 수령/04시·주간 경계/활동일 말미 해금/원장 보존 검증이 후속 실행 항목이다.

검증 범위: 기존 회계 스크립트와 오프라인 운영 정책, E 프로젝트 Game API claim의
입력 구간을 읽고 대조했다. 스크립트 수정·18조건 실행·런타임 테스트·배포 없음.
다음은 일반 퀘스트 실제 조건과 활동일별 진행/수령 입력표를 좁게 확인한 뒤,
이 규격의 조건부 회계모델 실행 여부를 결정하는 것이다. 가격/보상은 변경하지 않는다.


<a id="source-02711d8c0e43e1fa-quest-source-and-calendar-input-check--2026-09-10"></a>
#### Quest Source And Calendar Input Check — 2026-09-10

분석용 입력 확정/제안 구분. 사용자 'ㄱㄱ'는 직전 원문확인과 입력표 상세화의
진행 승인이다. 아래 과거 조건을 곧바로 새 런타임 정책으로 채택한 뜻은 아니다.

<a id="source-02711d8c0e43e1fa-복구한-조건과-승인-상태"></a>
##### 복구한 조건과 승인 상태

integrated-economy-design-2026-09-08.md §4에서 다음 목표별 분배를 찾았다.
그 절은 '제안'이며 현재 launch-economy-v1.json은 일/주 합계만 고정한다.
분배 합계 일20crystals/100credits/5parts/5data/50EXP 및 주150/500/50/20/300은
현재 수치와 일치하지만 합계 일치가 개별 목표·실패 자격 승인 증거는 아니다.

| 분석용 별칭 | 과거 조건 후보 | 직접보상 후보 |
| --- | --- | --- |
| D1 | 유효 로그인1 | 없음; 자연출석 별도 |
| D2 | 일반작전 서버 인정 종료1 | FREE10 + credits100 + parts5 |
| D3 | 일반작전 인정 종료 누적2 | FREE10 + EXP50 + data5 |
| W1 | 서로 다른 서버일 유효접속3 | FREE50 + credits500 |
| W2 | 일반작전 인정 종료 누적6 | FREE50 + parts50 |
| W3 | 인정 종료의 누적60waves | FREE50 + EXP300 + data20 |

D/W 별칭은 분석 키이며 실제 서버 catalog ID가 아니다. 옛 표의 패스XP20/40/40,
150/150/150와 World 추가50은 사용하지 않는다. 최신 패스의40/80/80,200/200/200과
시즌 상대4주를 독립 적용하며, 일반 퀘스트를 받을 때 패스XP를 다시 추가하지 않는다.
World 참여 보상은 기존 트랙과 동일 건이며 이번 비교에 넣지 않는다.

실제 owner Game API src 범위에서 독립 quest/objectiveType/objectiveId 키를
검색했으나 해당 계약을 확인하지 못했다. 이것은 배포/전체 저장소 부재 판정이 아니다.
초기 검색은 request_hash를 오탐했고, lookbehind는 기본 rg 엔진 오류가 났다.
PCRE2로 범위를 좁혀 재검색했다. generic activity나 run objective 문자열만으로
일반 퀘스트 이벤트 구현을 주장하지 않는다. 이번 첫 입력은 성공15wave만 가정해
아직 제안인 일반 패배 자격을 결정하지 않는다.

<a id="source-02711d8c0e43e1fa-비교용-달력과-세션-입력"></a>
##### 비교용 달력과 세션 입력

기준월은 예시 2026-09, 계정생성9/1 20:00KST, 관측은 서비스day1~28의 모든 세션.
종료는9/29 04:00KST 미만이다. 실제 이용 로그가 아닌 명시적 비교 fixture다.
월말 자동정산은 아직 발생하지 않으며,9/28 시작 일반 주간은 관측 밖으로 이어진다.
시작 전 일반 주간 진척은0으로 초기화하는 신규 계정 가정이다.

| 패턴 | 접속 날짜(9월) | 세션당 일반작전 성공 수 |
| --- | --- | ---: |
| 주4일 |1,3,5,7,8,10,12,14,15,17,19,21,22,24,26,28|1|
| 주2일 |3,7,10,14,17,21,24,28|2|
| 한 주 결석 |1~7,15~28|2|

각 세션 로그인/자연출석20:00, offline claim20:01, 이미 획득한 보상수령20:02,
성장20:03. 일반run1은20:05~20:09, 정산·성장·객차검토20:10;
2run 패턴은run2 20:15~20:19, 정산·성장·객차검토20:20으로 제안한다.
4분은 분석 시각을 정하기 위한 가정이며 실제 클리어시간/접속시간 예측이 아니다.
첫날 offline은 생성 후60초라600초 최소 미달; 수령 기준은 생성 시각에 남는다.
크리스탈 할인교환은 세션 마지막20:21에 잔액이 있으면1회로 놓는 비교 전략 후보다.
미접속일 구매/관측 마지막날 전량교환은 없으며 원본 모델의 sweep을 자동 상속하지 않는다.

<a id="source-02711d8c0e43e1fa-실제로-확인한-조건부-달력-집계"></a>
##### 실제로 확인한 조건부 달력 집계

위 날짜와 과거 목표 후보로 JS를 실행해 일반 퀘스트 완료 횟수를 집계했다.
PowerShell DateTime으로9/1 화요일,9/7 월요일을 교차 확인했다.
일반 주간은8/31~9/6,9/7~13,9/14~20,9/21~27,9/28~10/4의04경계이다.

| 패턴 | D2 | D3 | W1 | W2 | W3 |
| --- | ---: | ---: | ---: | ---: | ---: |
| 주4일 |16|0|4|0|3|
| 주2일 |8|8|0|0|3|
| 한 주 결석 |21|21|3|3|3|

주4일 패턴도 첫 일반 주간은3run=45waves라 W3 미달이다. 같은 패턴의 패스 주간은
시즌day1~7 네 번이라60waves를 채운다. 두 달력을 합치면 주간 보상이 과대 계산된다.
이 표는 조건/날짜 계산 검증이며 실제 이벤트 수락·보상 지급/연료 가능성 검증이 아니다.

<a id="source-02711d8c0e43e1fa-진행도-입력은-일반작전-횟수와-분리"></a>
##### 진행도 입력은 일반작전 횟수와 분리

일반 퀘스트의 run을 기존 스토리6미션으로 대체하지 않는다. 15wave 형식만으로
일반작전 여부나 스토리 해금을 판정하지 않는다. 4개 기본 작전 ID 미복구는 그대로다.
첫 계산에는 실제 ID 대신 '일반작전15wave 가정 event'라는 분석 전용 표시만 허용한다.
이 가정으로 경제 JSON의 특정 스토리 행 보상까지 선택하지 않는다.

소유·오프라인·해금 비교는 우선 다음 고정 진행도별 민감도로 분리할 것을 제안한다:
초기/ water-stop 완료 / night-boarders 완료. 각 군의 시작 진행도·소유·기존수령시각과
잔액은 별도 manifest에 적고, 이번에 새 진행·첫클리어 보상을 가상 지급하지 않는다.
후반군은 기존 계정 비교이므로 위 신규 계정 출석/패스 집계를 그대로 붙이지 않는다.
신규 계정이 성장하는 전체 모델은 별도 story run 시각·입장비·보상·진행 수락 입력이
있어야 한다. 예전 standard 날짜를 활동회차로 단순 치환해 실도달로 발표하지 않는다.

남은 좁은 결정: 위 일반 퀘스트 조건/보상 분배를 최신 설계로 채택할지, 그리고
일반작전 보상 catalog와 별도 story 진행 manifest 확보. 이번18조건 전체 wallet은
미실행이며 기존 일반작전 ID/연료/클리어 미검증도 유지한다. 코드/배포/영상 없음.


<a id="source-02711d8c0e43e1fa-accepted-quest-distribution-and-separated-reward-inputs--2026-09-10"></a>
#### Accepted Quest Distribution And Separated Reward Inputs — 2026-09-10

사용자가 직전 목표별 보상표/권장안에 'ㄱㄱ'로 진행 승인했다. D2/D3/W1/W2/W3의
목표·횟수·직접보상 분배를 USER APPROVED DESIGN으로 기록한다. 이전 절의 이 분배
'채택 대기'는 이 항목으로 대체한다. 런타임 구현/배포/수령 검증은 별도다.
실패/0wave/격리/스토리 포함 범위, 실제 catalog ID까지 확정한 것은 아니다.
일200·패스주600 XP는 독립이며 퀘스트 직접재화 수령에서 다시 XP를 추가하지 않는다.

<a id="source-02711d8c0e43e1fa-이번에-계산한-일반-퀘스트-직접수입"></a>
##### 이번에 계산한 일반 퀘스트 직접수입

이전 절 2026-09 신규계정/28서비스일/성공15wave 입력과 실제 완료횟수를 사용했다.
JS로 목표 횟수×승인 분배를 합산했다. 모든 달성 보상을 해당 세션에 수령한다는
조건이다. 전투 입장/종료의 실제 가능성은 검증하지 않았다.

| 패턴 | FREE crystals | credits | parts | data | operator EXP |
| --- | ---: | ---: | ---: | ---: | ---: |
| 주4일·각1run |510|3600|80|60|900|
| 주2일·각2run |310|800|40|100|1300|
| 한 주 결석·나머지2run |870|3600|255|165|1950|

이 표는 일반 퀘스트만의 수입 subtotal이다. 출석/패스/offline/전투 반복·첫클리어
수입이나 구매·연료 지출이 포함된 최종 잔액이 아니다. 무료/프리미엄 모두 같은
퀘스트 직접보상이며 프리미엄5x를 이 표에 곱하지 않는다. 기간이 바뀌면 재계산한다.

<a id="source-02711d8c0e43e1fa-반복보상-source-대조-결과"></a>
##### 반복보상 source 대조 결과

E:/Projects/LASTRAIL/server/packages/balance/src/index.ts의
launchEconomyBalanceDraft.activeRunBaseRepeatRewards는 opn_01_last_departure부터
opn_06_broken_line까지 실제6개 ID와 연결된다. narrative/act1-story-outline.md는
그6개+raid가 스토리1장이라고 명시한다. launch-economy-v1.json의6행 baseMaterials는
이 금액과 대응하지만 JSON 배열 자체에는 operation ID가 없다. 매핑 근거를 함께 보존한다.

현재 E 소스의 launchRunRewardsConfig는 별도 옛 값이며 draft와 동일하지 않다.
분석에는 승인된 launch-economy-v1 수치/EXP risk-only 규칙을 사용해야 한다.
옛 draft의 operation별 EXP780/860 등이나 raid2.20배를 재사용하지 않는다.
이 소스 대조는 원격 활성값 또는 새 income adapter 연결 완료의 증거가 아니다.

4개 기본 작전의 실제 ID/보상 catalog는 이번에도 확보하지 못했다. 위 스토리6행을
네 작전 보상으로 자동 복제/재명명하지 않는다. 앞 절의 '스토리와 분리'는 계산 출처와
진행 권한을 분리한다는 뜻이며, 모든 스토리15wave가 퀘스트에서 금지된다는 새 정책을
만든 것은 아니다. 실제 포함 여부는 인정 event/모드 계약으로 정해야 한다.

<a id="source-02711d8c0e43e1fa-분석-입력을-나누는-규격-제안"></a>
##### 분석 입력을 나누는 규격 제안

| 입력 묶음 | 반드시 기록할 내용 | 반영 시점/중복 방지 |
| --- | --- | --- |
| 일반작전 반복 | actual operation ID, mode/format, risk, reward policy/version, start/finish, outcome, 인정 waves | 승인 finish의 반복보상1건. catalog 미확인 행은 계산 보류 |
| 스토리 진행 | actual story operation ID, 입장 전 진행, 성공 finish, 서버수락 진행 및 car unlock | 성공 수락 뒤 진행/슬롯 반영. 같은 run은 아래 보상에 참조만 연결 |
| 스토리 반복보상 | 동일 story runId와 버전별 reward lines | 첫클리어여도 반복분1회. 진행 입력에 보상 복제하지 않음 |
| 첫클리어 재료 | account/operation/risk claim identity와 이미 사용한 권리 | 적격1회 추가분만. repeat 배율을 다시 곱하지 않음 |
| 제한된 출시권리 | 로그인/스토리 원래 source와 티켓→FREE 대체 여부, 과거 수령 | 일반 재료 첫클리어와 별도. core/티켓 대체를 난도마다 중복 지급하지 않음 |
| 퀘스트/패스 | 위 runId에 연결된 각 시스템별 적격 여부·일/주/시즌·목표 receipt | 한 run이 서로 다른 합법 목표에 기여 가능; 동일 목표/기간은1회 |

표의 필드들은 분석규격이지 새 HTTP API/DB schema 구현 계약이 아니다.
한 성공 run을 일반반복용1회와 스토리용1회로 복제해 입장비·보상·XP·waves를 두 배로
세지 않는다. 여러 결과가 한 run에 연결될 뿐이며, 각 권리의 적격성은 독립 확인한다.
명시적으로 별도 story run을 넣는다면 시각·연료·활동시간을 추가 입력해야 한다.

<a id="source-02711d8c0e43e1fa-다음-실행에-사용할-범위"></a>
##### 다음 실행에 사용할 범위

일반 퀘스트 subtotal은 위 조건에서 계산 완료다. 신규계정 전체 wallet에는 아직
일반작전 reward catalog, story run별 진척·연료·수령 입력이 필요하다. 알려진
출처만 더한 값에는 반드시 부분수입 표시를 붙이며 누락을0으로 간주하지 않는다.
성장/객차 구매일이나 완주 가능성은 이 subtotal만으로 확정하지 않는다.

다음 상세기획은 일반작전의 인정 종료 자격을 정리한다: 성공, 최소1wave 패배,
0wave, 포기·격리와 story/boss 포함 범위를 구분하고 패스의 기존 제안과 대조한다.
미복구4개 작전 ID는 계속 별도 gate이며 이름을 새로 만들지 않는다.
문서·계산만 수행했으며 런타임/배포/영상/새 담당 메시지 없음. 로컬 미커밋.

<!-- source-body:source-02711d8c0e43e1fa:end -->


<a id="source-951ce31dfb822c6d"></a>
## Offline Baseline Reconciliation - 2026-09-08

원출처: `WorkspaceRepo/docs/ops/offline-baseline-reconciliation-20260908.md`  
원문 SHA-256: `aa062efda525e3feefb24a5dfee2a9542d745cc284bf5251207d6fd34dc8c389`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-951ce31dfb822c6d:start -->
<a id="source-951ce31dfb822c6d-offline-baseline-reconciliation---2026-09-08"></a>
### Offline Baseline Reconciliation - 2026-09-08

Analysis and recommendation only. User approved continuing reconciliation, not
a particular new numeric release table. No code/config/DB/deployment changed.

<a id="source-951ce31dfb822c6d-provenance"></a>
#### Provenance

Source root: C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/attendance-pass-20260907/server
packages/balance/src/index.ts contains BOTH launchOfflineRewardsConfig100/50/30/20
and launchEconomyBalanceDraft.offlineBaseHourlyRewards24/2/1/80. Draft status
TESTING_TUNING_DRAFT, productionFinal=false, seedPromotionApproved=false.
Thus discrepancy is not only an old external document; both versions are in
the same code file. game-api/src/index.ts:4555,4677 use launchOfflineRewardsConfig.
This proves local reference wiring, NOT current remote deployed/config state.

Run tables differ too: launchRunRewardsConfig ordinary repeats include credits
and EXP; launchEconomyBalanceDraft adds parts/data and different credits/EXP.
Raid progression multiplier1.8 in config versus2.2 in planning draft. No raid
repeats modeled here, so this difference does not affect displayed arithmetic.

<a id="source-951ce31dfb822c6d-comparable-28-day-results"></a>
#### Comparable 28-Day Results

Same standard progress schedule and23offline hours/day as prior model. Each
claim uses floor per line. Columns credits/parts/data/EXP respectively.

| Offline baseline | Claims/day | Credits | Parts | Data | EXP |
| --- | --- | --- | --- | --- | --- |
| Code100/50/30/20 | 1 | 48743 | 24368 | 14617 | 9745 |
| Code100/50/30/20 | 2 | 96286 | 48136 | 28874 | 19250 |
| Code100/50/30/20 | 3 | 117411 | 58689 | 35205 | 23466 |
| Draft24/2/1/80 | 1 | 11693 | 966 | 479 | 38992 |
| Draft24/2/1/80 | 2 | 23098 | 1908 | 942 | 77024 |
| Draft24/2/1/80 | 3 | 28167 | 2307 | 1134 | 93927 |

Draft2claim EXP alone exceeds reference five-core Lv50 cumulative55000. This
reference is not a verified live cap/curve, but blocks claiming the old draft
is already balanced. Code2claim parts48136 versus local core5090 is the other
large mismatch. Neither justifies inflating every cost or cutting pass rewards.

<a id="source-951ce31dfb822c6d-ordinary-run-sensitivity"></a>
#### Ordinary Run Sensitivity

Assume two full15wave RiskII ordinary victories per day; latest completed
ordinary operation, op1 used for initial stage; op6 after raid. No boss repeats,
first-clear rewards, failures, purchases, expenses or other modes. Story stage
dates are externally imposed; this is NOT an executable progression schedule.
Fuel/entry/combat success and quest/pass feasibility remain unverified.

Code ordinary base credits [300,330,360,390,430,480], EXP[700,760,820,900,980,1100].
Draft base credits[120,135,150,170,195,225],parts[8,9,11,13,15,18],
data[4,5,6,7,8,10],EXP[700,780,860,950,1050,1180].
Both use ordinary multipliers[1,1.1,1.2,1.3,1.4,1.55].
Sum two individually floored rewards each model day:
- Code run income30638credits/0parts/0data/70290EXP.
- Draft run income13690credits/1020parts/556data/74442EXP.

Free attendance+daily+4weekly+fullpass plus offline2claim plus run subtotal:
- Code tables:153224credits/49576parts/29634data/96340EXP.
- Draft tables:63088credits/4368parts/2258data/158266EXP.
These are internally labeled bundles, not a claim either is final deployed truth.
Crystals unchanged; no duplication of first-clear launch tickets/crystal replacements.

<a id="source-951ce31dfb822c6d-recommended-next-candidate-not-approved-release-values"></a>
#### Recommended Next Candidate, Not Approved Release Values

Use a distinct trial hourly baseline24credits/2parts/1data/20EXP: credit/part/data
from draft; EXP retains current code20 instead of raising to80. Preserve story
multipliers/storage caps, no offline crystals, and approved attendance/pass5x.
This is an explicit hybrid NEW candidate, not a recovered prior decision.
Its standard2claim offline totals23098/1908/942/19250. With draft run rewards
and existing free activity/pass subtotal:63088/4368/2258/100492.

Even this candidate's EXP exceeds reference55000, so it is NOT balance-approved.
Before finalizing, establish intended five-core growth timing and restore exact
level curve; compare actual operation completion/access and growth gates. Train
parts4368 before one-time grants/other spending do not alone fund5090corecost.
Special cores and competing equipment sinks remain separate, not assumed free.

Next design work: specify target day7/day14/day28 core/operator progression and
allocate existing income among train/equipment/skills/research; recover existing
cost policies before inventing new level caps or tables. Server only validates
contracts/factual implementation until Plan's proposed numerical design accepted.

<a id="source-951ce31dfb822c6d-reproduction-and-verification"></a>
#### Reproduction And Verification

JS calculations executed directly in Plan tool session. Repeat previous
offline-progression-model-20260908.mjs calculation substituting base rates with
draft24/2/1/80 (same BPS,caps,stage starts,per-claim floor). Run model uses stage
index s, ordinary index max(0,min(5,s-1)), per-day2*floor(base*multiplier).
Results are conditional arithmetic, not server regression/remote smoke.
SERVER_1 fact-check still in progress at latest bounded snapshot. Plan files
local/uncommitted; no other owner's implementation modified.

<!-- source-body:source-951ce31dfb822c6d:end -->


<a id="source-ed20bcc99c9b2ac5"></a>
## Offline Progression Sensitivity - 2026-09-08

원출처: `WorkspaceRepo/docs/ops/offline-progression-analysis-20260908.md`  
원문 SHA-256: `c6acae71000029780f3712e0faded09ceef66e791d0cac176a43ba0c5ab01d4e`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-ed20bcc99c9b2ac5:start -->
<a id="source-ed20bcc99c9b2ac5-offline-progression-sensitivity---2026-09-08"></a>
### Offline Progression Sensitivity - 2026-09-08

Plan analysis only. No approved rates changed and no runtime/deployment tested.
Reproduce: node docs/ops/offline-progression-model-20260908.mjs

<a id="source-ed20bcc99c9b2ac5-inputs-and-limits"></a>
#### Inputs And Limits

Source: C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/packages/balance/src/index.ts
Read launchOfflineRewardsConfig and calculateOfflineRewardPreview (line811 onward).
Each claim floors each reward after multiplying hourly rate, BPS and capped time;
minimum10minutes, global cap12hours. Rates100credits/50parts/30data/20EXP per hour.
Progress none/op1/op2/op3/op4/op5/op6/raid multipliers:
1/1.15/1.32/1.52/1.75/2.05/2.4/3, storage4/5/6/7/8/9/10/12hours.

28 model days; daily1hour active and23hours offline split equally among1/2/3
claim intervals. This is a regular-interval sensitivity model, not observed
player behavior: sleep makes three evenly spaced claims less practical.
No opening backlog; each modeled interval earns before its claim. One progression
snapshot applies per model day; the source function consumes caller-supplied
progress, not a historical progress timeline. Progress changes during an interval,
online accrual, server clock/reset persistence and actual entry gates untested.

Stage activation days (not approved difficulty or measured clear times):

| Pace | None | Op1 | Op2 | Op3 | Op4 | Op5 | Op6 | Raid |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fast | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| Standard | 1 | 3 | 5 | 8 | 11 | 15 | 20 | 25 |
| Slow | 1 | 5 | 10 | 16 | 23 | 29 | 36 | 43 |

<a id="source-ed20bcc99c9b2ac5-offline-only-totals"></a>
#### Offline Only Totals

Lost hours are storage overflow out of644 modeled offline hours, not a daily cap.

| Pace | Claims/day | Credits | Parts | Data | EXP | Lost hours |
| --- | --- | --- | --- | --- | --- | --- |
| Fast | 1 | 84076 | 42037 | 25221 | 16814 | 343 |
| Fast | 2 | 161852 | 80924 | 48552 | 32368 | 63 |
| Fast | 3 | 167649 | 83820 | 50289 | 33525 | 26 |
| Standard | 1 | 48743 | 24368 | 14617 | 9745 | 412 |
| Standard | 2 | 96286 | 48136 | 28874 | 19250 | 184 |
| Standard | 3 | 117411 | 58689 | 35205 | 23466 | 59 |
| Slow | 1 | 25075 | 12535 | 7515 | 5007 | 470 |
| Slow | 2 | 50150 | 25070 | 15030 | 10014 | 296 |
| Slow | 3 | 74163 | 37065 | 22221 | 14805 | 128 |

<a id="source-ed20bcc99c9b2ac5-conditional-non-run-aggregate"></a>
#### Conditional Non-Run Aggregate

Keep the earlier full attendance,28daily,4ordinaryweekly,completed50level pass
subtotal fixed to isolate offline sensitivity. Slow story does NOT necessarily
mean missed quests; full quest/pass completion is a separate assumption and has
not been proven feasible for every schedule. Run/first-clear rewards and all
spending excluded. This is NOT complete net economy balance.

Standard pace,2claims/day:
- Free:122586credits,49576parts,29634data,26050EXP.
- Premium buyer:160086credits,53326parts,31384data,43550EXP.
- Crystals unchanged:2560free /6310buyer because offline has no crystals.

<a id="source-ed20bcc99c9b2ac5-interpretation-and-next-work"></a>
#### Interpretation And Next Work

- Story progression and collection frequency materially change income. Earlier
  fixed1x/4h/day figures remain only a low-progression example.
- At raid cap12h, two11.5h intervals capture all modeled23h. At4h cap, even
  three intervals lose11h/day; early progression creates collection pressure.
- Standard2claim offline parts48136 exceed the old local train1-to10 parts5090
  by about9.46x. That cost catalog is NOT confirmed server truth; special core,
  unlock gates and other equipment/operator/research sinks can remain limiting.
- Premium pass adds3750parts, only about7.6percent of this free aggregate parts,
  despite pass-only premium resources remaining exactly5x free pass resources.
  Do not confuse the approved pass ratio with whole-economy paid advantage.
- No automatic rate nerf, storage enlargement or cost multiplier is approved.
  Next inspect authoritative server costs and all sinks, then incorporate actual
  operation rewards and growth-gated progression rather than exogenous dates.

Verification: model executed exit0, checksPASS (minimum,cap,rounding,monotonic
scenario income,hour conservation). This is model verification, not server test
suite execution or DEV/device validation. Local files uncommitted.

<!-- source-body:source-ed20bcc99c9b2ac5:end -->


<a id="source-00e5041688926c56"></a>
## Offline Reward Operations Policy

원출처: `WorkspaceRepo/docs/ops/offline-reward-operations-policy.md`  
원문 SHA-256: `35f0cccd69ccc40ef108d0fee771723db5e957f23d118996623ec55e13e12003`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-00e5041688926c56:start -->
<a id="source-00e5041688926c56-offline-reward-operations-policy"></a>
### Offline Reward Operations Policy

<a id="source-00e5041688926c56-purpose"></a>
#### Purpose

This document defines launch operations policy for LAST RAIL offline rewards.

The Game API already owns preview and claim calculation. This policy defines how support, Admin, and server operations should interpret edge cases without opening repair tools too early.

<a id="source-00e5041688926c56-source-of-truth"></a>
#### Source of Truth

Server-owned records are authoritative:

- `account_story_progress.highest_story_progress_id`
- `account_offline_reward_state.last_offline_reward_claim_at`
- `account_offline_reward_state.revision`
- offline reward claim response records
- `reward_transactions`
- `economy_transactions`
- `currency_ledger`
- `idempotency_records`
- future `ops_error_logs`

Unity local state, screenshots, client timers, client-supplied story progress, risk tier, offline seconds, reward amounts, currency grants, item grants, and economy grants are not source of truth.

<a id="source-00e5041688926c56-launch-rules"></a>
#### Launch Rules

- Offline rewards use server-stored story progress.
- Risk I-V does not directly multiply offline rewards.
- Claimable time is capped by offline storage cap.
- Rewards below minimum claim time are not claimable.
- Empty claims must not advance `last_offline_reward_claim_at`.
- Duplicate replay must not grant rewards again.
- Duplicate replay must not increment offline state revision.
- Claim success must create auditable reward/economy/ledger references.
- Admin launch surfaces are read-only.
- Manual repair and compensation remain disabled.

<a id="source-00e5041688926c56-long-absence-and-cap"></a>
#### Long Absence And Cap

When a player is away longer than the storage cap:

- The server should calculate rewards only up to the applicable cap.
- Time beyond the cap is not claimable by default.
- Support should describe this as storage capacity, not lost server time.

Player-facing wording:

> 오프라인 보상은 서버에 저장된 마지막 수령 시각과 현재 스토리 진행도의 보관 한도를 기준으로 계산됩니다. 보관 한도를 초과한 시간은 기본 수령량에 포함되지 않을 수 있습니다.

Escalate only when:

- The stored cap does not match story progress policy.
- Claim result exceeds cap.
- Claim result is lower than cap without an explainable minimum, state, or policy reason.

<a id="source-00e5041688926c56-minimum-claim-time"></a>
#### Minimum Claim Time

Launch policy:

- Offline reward below the minimum claim seconds is not claimable.
- The recommended player-facing state is `offline_reward_not_claimable`.
- The server should return rejection without consuming the claim baseline.

Player-facing wording:

> 현재 서버 기록 기준으로 수령 가능한 오프라인 보상이 없습니다. 오프라인 보상은 최소 누적 시간과 보관 한도 기준을 함께 적용합니다.

Support should not escalate when:

- The only reason is that elapsed offline time is below the minimum.
- No ledger or state mutation occurred.

Escalate when:

- The claim baseline advanced after a not-claimable result.
- The player received a success response without ledger references.

<a id="source-00e5041688926c56-preview-and-claim-mismatch"></a>
#### Preview And Claim Mismatch

Preview is informational. Claim is the committed server transaction.

Expected differences:

- Time can advance between preview and claim.
- Story progress may advance before claim and change the calculation input.
- Offline reward state may change if another valid claim was committed first.

Unexpected mismatch candidates:

- Claim uses a story progress id not present in server state.
- Claim uses a baseline older than `last_offline_reward_claim_at`.
- Claim succeeds but reward/economy ledger is missing.
- Claim reward lines conflict with stored transaction payload.
- Duplicate replay returns a different claim id or references.

Operational response:

- Treat expected time drift as `info`.
- Treat missing ledger or duplicate mismatch as `high_risk`.
- Treat story policy missing as `needs_review` or `high_risk` depending on impact.
- Do not manually correct from launch Admin.

<a id="source-00e5041688926c56-clock-tamper-policy"></a>
#### Clock Tamper Policy

The server should not trust client clocks for offline reward duration.

Clock tamper suspicion candidates:

- Client diagnostic time is far ahead or behind server time.
- Repeated requests show impossible local elapsed time.
- Claim attempt cadence conflicts with server `last_offline_reward_claim_at`.
- Multiple devices produce contradictory local offline durations.

Launch handling:

- Server-calculated time remains authoritative.
- Support should not accuse the player.
- Use neutral wording and escalate only with server-side evidence.

Player-facing wording:

> 보상 수령 시간은 서버 기록을 기준으로 확인됩니다. 기기 시간과 서버 기록이 다를 수 있어 저장된 수령 기준으로 다시 확인하겠습니다.

Do not expose:

- raw client timestamps if sensitive
- fraud scoring internals
- device fingerprint internals
- anti-abuse thresholds

<a id="source-00e5041688926c56-state-revision-conflict"></a>
#### State Revision Conflict

`account_offline_reward_state.revision` exists to help detect concurrent or inconsistent updates.

Conflict candidates:

- Revision increments without a matching offline reward claim.
- Two claims appear to commit from the same baseline.
- Duplicate replay increments revision.
- Empty claim increments revision.
- Latest reward transaction does not match state update.

Operational response:

- Mark as `offline_state_conflict`.
- Severity should default to `high_risk`.
- Support should attach read-only evidence.
- Manual timestamp reset is disabled at launch.

<a id="source-00e5041688926c56-no-row-fallback"></a>
#### No-Row Fallback

Existing accounts may not have an offline reward state row.

Launch policy:

- Preview may use `accounts.created_at` lazy fallback.
- Claim may lazily prepare/upsert state during a successful claim transaction.
- Admin should show the fallback source clearly.
- Fallback itself is not an error.

Escalate when:

- A new guest account lacks state immediately after creation.
- Fallback is used after a successful claim should have created state.
- Fallback produces a claim inconsistent with account creation time.

<a id="source-00e5041688926c56-duplicate-replay"></a>
#### Duplicate Replay

Duplicate replay is expected when the same idempotency key is submitted again.

Expected behavior:

- Return stored claim/reference ids.
- Do not recalculate reward.
- Do not write additional ledger entries.
- Do not increment state revision.
- Reward lines may be empty in duplicate replay response.

Player-facing wording:

> 같은 수령 요청이 중복으로 들어온 경우에는 서버가 기존 처리 결과를 다시 확인합니다. 이 경우 보상이 한 번 더 지급되거나 수령 기준 시간이 다시 변경되지 않습니다.

Escalate when:

- Duplicate replay returns a different claim id.
- Duplicate replay creates additional ledger entries.
- Duplicate replay changes offline reward state.

<a id="source-00e5041688926c56-reward-line-policy"></a>
#### Reward Line Policy

Launch allowed recurring offline reward lines:

- `cur_credits`
- `cur_machine_parts`
- `cur_tactical_data`
- `cur_operator_exp`

Launch blocked default offline reward lines:

- `cur_special_core`
- `cur_command_ticket`
- `cur_command_ticket_fragment`

Support interpretation:

- Blocked default rewards should not appear in normal offline reward claims.
- If blocked rewards appear, treat as `offline_reward_plan_mismatch` or `economy_mutation_conflict`.
- Do not promise rare currency or ticket compensation from support.

<a id="source-00e5041688926c56-admin-read-only-fields"></a>
#### Admin Read-Only Fields

Admin should prioritize these fields:

- story progress id
- preview status
- claim status
- offline reward claim id
- last claim baseline
- state source
- state revision
- claimable seconds/hours
- storage cap hours
- minimum claim seconds
- max claimable hours
- reward transaction id
- economy transaction id
- account mutation id
- ledger presence status
- idempotency replay status
- error code
- support severity/category recommendation

Admin should not expose:

- repair buttons
- manual grant controls
- timestamp reset controls
- raw request body
- token/session secrets
- raw RNG
- raw multiplier bps
- internal exploit-sensitive formulas

<a id="source-00e5041688926c56-ops-error-code-mapping"></a>
#### Ops Error Code Mapping

Expected/no-escalation:

- `offline_reward_not_claimable`: use `info` when caused only by minimum time.
- `duplicate_replay`: use `info` when references match stored claim.

Needs review:

- `offline_story_progress_missing`
- `offline_reward_policy_missing`
- `offline_reward_policy_invalid`

High risk:

- `offline_reward_state_conflict`
- `offline_reward_clock_tamper`
- `offline_reward_claim_conflict`
- `offline_reward_plan_mismatch`
- `economy_mutation_conflict`

<a id="source-00e5041688926c56-support-decision-matrix"></a>
#### Support Decision Matrix

| Situation | Severity | Category | Launch Action |
| --- | --- | --- | --- |
| Below minimum claim time | `info` | `expected_behavior` | Respond with template |
| Capped long absence | `info` | `expected_behavior` | Explain cap |
| Duplicate replay matches stored claim | `info` | `expected_behavior` | Explain duplicate replay |
| State row missing for legacy account before first claim | `info` | `expected_behavior` | Explain fallback if needed |
| Successful claim but ledger missing | `high_risk` | `reward_claim_missing_ledger` | Escalate to owner review |
| Empty claim advanced baseline | `high_risk` | `offline_state_conflict` | Attach evidence, no repair |
| Duplicate replay wrote extra ledger | `high_risk` | `offline_claim_duplicate_mismatch` | Attach evidence, no retry advice |
| Unknown stored story progress | `needs_review` | `offline_state_conflict` | Escalate to operator review |
| Clock tamper suspected | `high_risk` | `offline_state_conflict` | Escalate with neutral wording |

<a id="source-00e5041688926c56-rollout-order"></a>
#### Rollout Order

1. Keep preview/claim support read-only.
2. Show fallback source and state revision in Admin.
3. Add ops error logs for real state conflicts.
4. Add support summary integration for recent offline errors.
5. Define repair preview separately.
6. Only then consider audited timestamp correction.

<a id="source-00e5041688926c56-open-decisions"></a>
#### Open Decisions

- Whether long absence cap overflow should ever be partially restored during special incidents.
- Whether clock tamper thresholds should be documented internally or kept in security-only docs.
- Whether no-row fallback should expire after a migration backfill.
- Whether offline reward state conflicts require OWNER review or a dedicated integrity queue.
- Whether `cur_operator_exp` should remain in offline rewards after economy balancing.

<!-- source-body:source-00e5041688926c56:end -->


<a id="source-3f90d984d2252ca6"></a>
## Optional Car Cashflow Comparison

원출처: `WorkspaceRepo/docs/ops/optional-car-cashflow-20260909.md`  
원문 SHA-256: `617cb73218578eb1ec6e5b8b0ef379f64344a7b15a897f519d9e2dd9b46c2bf8`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-3f90d984d2252ca6:start -->
<a id="source-3f90d984d2252ca6-optional-car-cashflow-comparison"></a>
### Optional Car Cashflow Comparison

Plan, 2026-09-09. Conditional accounting; prices remain proposals. No runtime change.

Run: node docs/ops/optional-car-cashflow-20260909.mjs. Report: same-name JSON.
Reuses exported baseline simulate with optional post-growth purchases/detail; direct
baseline command remains available. Baseline18 stored results deep-equal fresh
no-option results.54 comparisons=18conditions x none/Medical/all. Negative wallet,
duplicate purchase and unaffordable purchase guards passed. No growth-state day
differed from corresponding no-purchase case. This is not combat viability evidence.

| Standard/free/2claims scenario | Purchase dates | Day28 credits | Five-slot50 |
| --- | --- | ---: | ---: |
| None | None |52616|27|
| Medical600 | Day8 |52016|27|
| All4000 | Medical day8; remaining day15 |48616|27|

Fast schedule buys on4/6; standard8/15; slow first Medical16, other unlock stage is
day29 so remaining options are NOT bought within28days. These are inherited exogenous
story dates, not measured player clear times or affordability before same-day income.
Stage3/5 signify assumed mission3/5 completion. Day totals collected before growth
and purchases; intraday ordering, fuel and combat not simulated. First-clear/World
extra income not added; original attendance/pass/quests/full-clear assumptions kept.

Interpretation: candidate4000 total is affordable without delaying the original
growth planner in these54 conditional cases; no evidence it creates meaningful
growth-versus-purchase pressure here. Retain as accessible specialization option
candidate rather than inflate price solely to consume surplus credits. Missing
collection growth, other future spending, sparse participation and actual player
choices remain outside this model; not a blanket guarantee of affordability.

Authority/economy JSON and stored baseline report unchanged. New report/script are
local/uncommitted; baseline analysis script gained reusable function/import guard.
No server/Unity tests, live purchase, simulation combat, video or deployment.

<!-- source-body:source-3f90d984d2252ca6:end -->


<a id="source-79d99fcdfa4ba5b6"></a>
## Optional Car Credit-source Stress

원출처: `WorkspaceRepo/docs/ops/optional-car-credit-stress-20260909.md`  
원문 SHA-256: `c6328de9e5324a6055149174a89069f4add7be6afef5831e7bebea78bfa9118f`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-79d99fcdfa4ba5b6:start -->
<a id="source-79d99fcdfa4ba5b6-optional-car-credit-source-stress"></a>
### Optional Car Credit-source Stress

2026-09-09 Plan, conditional sensitivity only. Run node docs/ops/optional-car-credit-
stress-20260909.mjs (filename is continuous). JSON stores daily results.

162cases=18baseline conditions x3credit omissions x3purchase scenarios. Remove
attendance credits, pass credits or both; keep other rewards and daily activity.
This is NOT a missed-login or reduced-participation simulation. Story dates remain
exogenous. Purchase prices/acquisition are proposals, not live policy.

18default results matched saved baseline. Removed credit totals asserted14000 for
attendance,7500/free or45000/premium for pass; non-credit gross unchanged. No negative
wallet. Previous54case comparison rerun, still zero growth differences. Scoped diff
check passed. No combat/fuel/device/server tests.

Standard/free/2claims/all purchases ends with34616credits without attendance,
41116without pass,27116without both. Medical day8, others15; no growth delay versus
matched no-purchase control. These dates are assumptions, not predicted user dates.

One of162 cases differs in growth: fast/1claim/premium, both credit sources removed,
all purchases. First Medical4, second Gun/Defense6, second Medical7 (deferred from6).
On day8, one equipment1to2 and all five skills1to2 are delayed; daily growth matches
control again day9. Train and operator sync unaffected. Premium non-credit rewards
are retained, permitting earlier material spending and exposing a credit bottleneck;
this is not a claim real premium accounts are worse off.

Recommendation: keep accessible candidate prices; no automatic increase to drain
surplus. Do not promise zero opportunity cost. Purchases optional and explicit;
growth-first model is not a client-enforced wallet reservation. True low-activity
validation still needs missed attendance cells, reduced pass XP/quests/runs/offline
claims and consistent story delay, none supplied by this isolated credit test.

<!-- source-body:source-79d99fcdfa4ba5b6:end -->


<a id="source-54df3b38286fadd3"></a>
## Ordinary Quest Expiry And Mail Recovery — 2026-09-10

원출처: `WorkspaceRepo/docs/ops/ordinary-quest-expiry-recovery-proposal-20260910.md`  
원문 SHA-256: `1a6581b00748aaf4379df95d670493301b021ace44bcf4967cf6d8aa2e0a8b86`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-54df3b38286fadd3:start -->
<a id="source-54df3b38286fadd3-ordinary-quest-expiry-and-mail-recovery--2026-09-10"></a>
### Ordinary Quest Expiry And Mail Recovery — 2026-09-10

<a id="source-54df3b38286fadd3-current--사용자-확정-우편-14일-후-미수령-소멸"></a>
#### Current — 사용자 확정: 우편 14일 후 미수령 소멸

사용자 지시: '달성 후 미수령은 우편으로 보내서 14일 기간제한 두고 그래도 수령안하면 없어지는걸로'.
일반 임무의 자동지갑 지급/무기한 복구 제안을 이 정책으로 대체한다.
이는 승인된 설계이며 구현·배포 완료를 의미하지 않는다. 아래 역사 구간은 현행 규칙이 아니다.

<a id="source-54df3b38286fadd3-적용과-수령기한"></a>
##### 적용과 수령기한

- 기간 중 개별 임무 보상 수령은 유지한다. 일반 일일/주간 임무가 종료되면
  달성했지만 받지 않은 직접보상을 우편으로 발송한다.
- 우편에서 사용자가 보상을 수령해야 지갑/인벤토리에 지급된다. 우편 도착이나
  열람만으로 지급하지 않는다. 발송 후14일 동안 미수령이면 첨부보상은 소멸한다.
- 미달성 임무는 우편을 만들지 않고 부분진행도 다음 기간으로 이월하지 않는다.
  이미 직접 수령한 보상은 우편으로 재발송하지 않는다.
- 대상은 일반 D2/D3/W1/W2/W3 직접보상이다. 보상 없는 D1, 출석, 오프라인,
  전투 claim, World, 패스XP/레벨보상은 각 정책을 유지한다. 패스 시즌종료
  자동정산은 이번 일반 임무 수정의 대상이 아니다.

세부 시간 기준은 서버가 우편을 실제 생성하여 수령 가능 상태로 확정한 시각을
발송시각으로 삼고, 그 시각부터14×24시간(336시간)을 기한으로 정의한다.
첫 접속/첫 열람부터 세지 않는다. 발송이 지연되면 실제 발송부터14일을 제공한다.
동일 우편 재처리·재조회는 원래 발송/만료시각을 유지하고 기한을 연장하지 않는다.
예: 9/10 04:00 KST 발송 → 9/24 04:00 KST 만료. 수령 가능 구간은
[발송시각, 만료시각)이며 정확히 만료시각부터는 미수령 보상을 받을 수 없다.
이 시간 기준은 사용자14일 지시를 구체화한 설계 정의다.

<a id="source-54df3b38286fadd3-기간-종료발송수령만료의-정합성"></a>
##### 기간 종료·발송·수령·만료의 정합성

일일04시/일반주간 월요일04시와 적격 사건의 최초 서버 확정시각 귀속을 유지한다.
종료 전 확정된 사건이 늦게 반영되면 원기간의 달성 권리를 확인하여 우편을 생성한다.
종료 후 처음 확정된 사건을 옛 기간에 소급하지 않는다. 미접속 중에도 발송과
만료 시간은 흐르며 미접속을 이유로 새 임무 달성이나 기한 연장을 만들지 않는다.

계정+임무시스템+기간+목표별 하나의 권리를 직접수령/우편발송/우편수령/만료가
공유한다. 당시 보상표를 고정한다. 목표별 권리를 우편에 연결하며 여러 권리를
묶는 경우도 원권리별 중복방지 근거가 필요하다. 묶음 단위는 실제 우편 계약 확인 후 정한다.

| 상태 | 처리 |
| --- | --- |
| 기간 중 달성·미수령 | 기존 개별수령 가능 |
| 종료·달성·직접 지급 없음 | 우편 발송 대기; 발송 자체는 재화 지급 아님 |
| 우편 발송 확정 | 기존 임무에서는 우편 이동 안내; 우편에서만 수령 |
| 우편 수령 확정 | 실제 보상 지급, 해당 권리 수령 완료 |
| 기한 도달·미수령 | 보상 소멸, 재발송/재수령으로 복구하지 않음 |
| 발송/수령 결과 불명 | 서버 권리·우편·지급내역 확인; 중복 생성/지급 금지 |

직접수령과 발송이 경합하면 서버에서 하나의 경로만 확정한다. 직접지급 완료면
우편을 만들지 않고, 우편 이관 완료면 옛 임무 버튼으로 직접지급하지 않는다.
만료 후 옛 임무 요청이나 발송 작업 재시도도 소멸한 권리를 다시 살리지 않는다.

우편 수령과 만료는 서버에서 원자적으로 판정한다. 만료 전 서버가 수령권리를
유효하게 확정했다면 응답이 만료 후 도착해도 지급내역을 복구한다. 요청을 누른
시각만 만료 전이고 서버 확정은 만료 후라면 수령 불가다. 이미 확정된 수령의
지급 재시도는 미수령 만료와 구분하여 끝까지 복구한다. 품목 일부만 지급하고
전체 수령완료로 표시하지 않는다. 실제 원자적 지급/재시도 계약은 구현 전 확인한다.

발송 실패는 안전하게 재시도하며 존재하지 않는 우편의14일을 먼저 소진하지 않는다.
기한이 지난 미수령 첨부보상은 사용자 지시대로 소멸하며 자동지급·대체우편을 만들지
않는다. 중복방지·감사를 위한 최소 권리/만료 기록은 보상 소멸과 구분한다.
우편 본문/운영 기록의 삭제·보존기간은 별도 계약이며 모든 기록을 즉시 물리삭제하지 않는다.

<a id="source-54df3b38286fadd3-화면-문구와-경제-입력"></a>
##### 화면 문구와 경제 입력

- 임무 안내: '달성 후 받지 않은 보상은 임무 종료 시 우편으로 발송됩니다.
  우편 발송 후14일 이내에 수령하지 않으면 보상이 소멸합니다.'
- 발송 대기: '미수령 보상 우편 발송 중'. 발송 확정: '우편으로 발송됨 · 우편함'.
- 우편에는 원임무/기간, 실제 첨부품목, 정확한 KST 만료일시와 남은시간을 표시한다.
  '수령 완료', '기한 만료 · 보상 소멸', 조회실패는 별도 상태다.
- 알림 읽음은 보상수령이 아니다. 만료/수령 완료 우편은 수령가능 알림에 포함하지 않는다.
  우편 연계 후 임무와 우편함에서 같은 보상을 두 번 수령할 수 있게 표시하지 않는다.
- 현금흐름은 권리획득→우편발송→우편수령 또는 만료를 분리한다. 실제 수령 확정 때만
  지갑에 더한다. 우편발송을 수입으로 계산하지 않으며 만료는 미수령 권리 소멸이지
  이미 지급된 지갑 재화를 차감하는 사건이 아니다. 전량 직접수령 기존 계산은 그대로고,
  미수령/저활동 시나리오의 우편수령 일정과 만료 손실은 별도 계산이 필요하다.

<a id="source-54df3b38286fadd3-수용-기준과-검증-범위"></a>
##### 수용 기준과 검증 범위

| 검토 상황 | 기대 결과 |
| --- | --- |
| 달성 미수령 후 임무 종료 | 보상 우편 생성, 지갑은 수령 전 불변 |
| 발송 후14일 미접속 | 미수령 첨부 소멸; 접속 시14일 재시작 없음 |
| 만료 직전 수령확정·응답 유실 | 기존 지급 복구, 보상 소멸/중복 지급 없음 |
| 정확히 만료시각에 최초 수령확정 시도 | 만료로 수령 불가 |
| 직접수령/우편발송 동시 실행 | 지급 또는 우편 이관 중 하나만 확정 |
| 발송 실패 후 지연 성공 | 실제 발송 확정부터14일 |
| 발송 재시도 또는 만료 후 원사건 재처리 | 우편 재생성·만료 연장·재지급 없음 |
| 미달성 또는 이미 직접수령 | 신규 보상 우편 없음 |

위 표는 미실행 수용 시나리오다. 실제 검증은 문서 재읽기·변경내용/상대링크/공백
검사이며 코드·서버·우편·기기·배포 테스트와 경제 재계산은 미수행이다.
다음 구현 전 확인 항목: 우편 발송/만료/수령 원자성, 임무 권리 이관, 지연사건 복구.

Sources: [임무 화면](47_ECONOMY_DETAIL_CONTRACTS.md#source-a19eb0b11edacaf8),
[목표와 분배](47_ECONOMY_DETAIL_CONTRACTS.md#source-02711d8c0e43e1fa),
[기간과 사건](47_ECONOMY_DETAIL_CONTRACTS.md#source-901d352e4d663bd3),
[별도 패스 정책](47_ECONOMY_DETAIL_CONTRACTS.md#source-cee45fb251120260).

---

<a id="source-54df3b38286fadd3-superseded-history--자동정산-제안-사용자-우편14일-정책으로-폐기"></a>
#### Superseded history — 자동정산 제안, 사용자 우편14일 정책으로 폐기

아래는 수정 전 제안 원문 보존이다. 자동지급·무기한 보존·우편 불필요 문구 및
다음 단계는 모두 위 사용자 확정 정책으로 대체되며 현재 요구사항으로 사용하지 않는다.

<a id="source-54df3b38286fadd3-ordinary-quest-expiry-and-recovery-proposal--2026-09-10"></a>
### Ordinary Quest Expiry And Recovery Proposal — 2026-09-10

Plan_1 신규 정책 제안. 사용자 'ㄱㄱ'는 직전 예고한 일반 임무 기간종료·미수령 처리의
상세설계 진행 요청이다. 아래 자동정산을 기존 승인 정책이나 구현 상태로 간주하지 않는다.
기존 승인 목표/분배와 종료 인정조건의 조건부 경계를 유지한다.

Sources: [임무 화면](47_ECONOMY_DETAIL_CONTRACTS.md#source-a19eb0b11edacaf8),
[목표별 분배와 관측 입력](47_ECONOMY_DETAIL_CONTRACTS.md#source-02711d8c0e43e1fa),
[인정조건](47_ECONOMY_DETAIL_CONTRACTS.md#source-04d9d21432054734),
[기간 귀속](47_ECONOMY_DETAIL_CONTRACTS.md#source-901d352e4d663bd3),
[별도 승인된 패스 종료정책](47_ECONOMY_DETAIL_CONTRACTS.md#source-cee45fb251120260).

<a id="source-54df3b38286fadd3-1-선택지와-권장안"></a>
#### 1. 선택지와 권장안

| 안 | 사용자 결과 | 운영상 고려 |
| --- | --- | --- |
| 종료 즉시 미수령 소멸 | 달성했어도 수령을 놓치면 손실 | 초기화 직전 수령·장애 분쟁 발생 |
| 종료 후 일정기간 직접 수령 | 추가 수령 기회 제공 | 두 번째 기한·여러 기간 목록·재만료 처리 필요 |
| 획득 미수령분 자동정산 — 권장 | 달성한 보상은 보존, 미달성은 초기화 | 중복방지와 지연 사건 정합성 필요 |

권장안: 기간 중 개별 '보상 받기' 유지. 기간 종료 시 이미 달성한 일반 임무의
미수령 직접보상을 자동정산한다. 정산받기 위한 재접속·우편 수령·추가 확인은 없다.
미달성 목표의 부분진행은 다음 기간에 이월하지 않고 부분보상도 지급하지 않는다.

범위는 승인된 D2/D3/W1/W2/W3 직접보상이다. D1 로그인은 직접보상이 없으므로
정산할 보상이 없다. 출석·오프라인 보상·전투 claim·World·패스XP/레벨보상은
각자 정책을 유지한다. 같은 사건이 패스도 진행시킬 수 있지만 이 정산으로 패스XP를
추가 지급하지 않는다. 기존 패스 자동정산 승인을 일반 임무 승인으로 확대하지 않는다.

<a id="source-54df3b38286fadd3-2-기간과-획득-권리"></a>
#### 2. 기간과 획득 권리

일일은 KST04시부터 다음날04시 직전까지, 일반 주간은 월요일04시부터 다음
월요일04시 직전까지다. 정확히 경계시각에 최초확정된 적격 사건은 새 기간에 속한다.
수령 요청시각이나 응답도착시각으로 목표 달성 기간을 바꾸지 않는다.

기간 종료는 새 사건의 적립을 닫는 것이며 이미 확정된 적격 사건을 삭제하는 것이
아니다. 종료 전 확정됐으나 큐에서 늦게 반영된 사건은 원기간 목표에 반영한다.
목표 producer가 기간 내 확정 사건을 모두 반영했음을 확인하거나, 지연 사건을
안전하게 추가 정산하는 계약이 필요하다. 클라이언트나 작업 실행시각으로 재귀속하지 않는다.

목표별 권리는 계정+일반 임무 시스템+기간+목표 식별로1개다. 당시 적용한 규칙과
보상표 버전을 고정하여 이후 설정 변경이 과거 보상량을 바꾸지 않게 한다. 버전은
근거이지 같은 목표를 여러 번 받을 수 있는 별도 권리로 쓰지 않는다.
동일기간 내 목표 교체/마이그레이션은 권리 매핑을 별도로 정의하기 전 적용하지 않는다.

미접속 기간에는 로그인·전투·목표완료를 새로 만들지 않는다. 접속하지 않아도 이미
획득한 보상 정산은 진행한다. 예전 사건을 재처리해도 원래 권리만 조회/정산한다.
수치·시작 보유재화·입장비·작전 클리어·첫클리어 권리는 이 제안으로 바뀌지 않는다.

<a id="source-54df3b38286fadd3-3-수령과-자동정산의-경합"></a>
#### 3. 수령과 자동정산의 경합

| 상태 | 허용 처리 | 금지 처리 |
| --- | --- | --- |
| 기간 중 미달성 | 적격 사건 진행 | 미리 권리 지급 |
| 기간 중 달성·미수령 | 개별 수령 | 단순 목표 조회로 중복 지급 |
| 종료·미달성 확정 | 진행 마감 | 다음 기간 진행 이월 |
| 종료·달성·미지급 | 자동정산 대기/재시도 | 수령 기한이 지났다는 이유로 소멸 |
| 지급 확정 | 같은 지급내역 반환 | 수동/자동 경로별 재지급 |
| 원기간 사건 확인 중 | 확인 중 유지/후속 정합성 처리 | 최종 미달성으로 조기 확정 |

수동수령과 정산은 같은 권리와 지급내역을 사용한다. 하나가 먼저 지급을 확정하면
다른 경로는 같은 결과를 반환한다. 통신 응답만 끊긴 경우 지급 실패로 단정하지 않는다.
정산 대상의 여러 재화는 기존 원자적 지급 계약을 쓰거나 품목별 복구 근거가 있어야
한다. 일부 품목만 지급한 상태를 전체 지급완료로 표시해서는 안 된다.

초기화 후 도착한 옛 화면의 수령 요청도 원기간 권리를 확인한다. 획득한 보상이면
같은 지급/정산 경로로 해결하고 새날 임무를 수령하지 않는다. 기존 API가 종료 후
수동수령을 허용하지 않더라도 획득 권리는 정산으로 회복하고 상태조회로 안내한다.
새 API 경로·응답코드가 이미 존재한다는 주장은 아니다.

정산 장애는 안전하게 재시도한다. 획득 권리는 일시 오류나 재시도 횟수 초과만으로
소멸하지 않는다. 지급이 명확히 불가한 정책오류는 자동으로 금액을 바꾸거나 대체재를
주지 않고 운영 확인 대상으로 남긴다. 계정 제재·정정·회수는 기존 별도 정책을 따른다.
기존 적법 receipt를 후속 지원환급만으로 자동 회수하지 않는다.

<a id="source-54df3b38286fadd3-4-화면과-안내-문구"></a>
#### 4. 화면과 안내 문구

기간 중 설명: '기간 안에 달성한 미수령 보상은 종료 후 자동 지급됩니다.'
이는 본 제안 채택 및 구현 확인 후 노출할 문구이며 현행 기능 안내가 아니다.

초기화 후 현재 임무를 기본 표시하고 이전 기간은 '이전 임무 정산'에서 확인한다.
조회 지원이 전제이며 임의 보관기간·무기한 원시 로그 저장을 요구하지 않는다.
정산 권리/지급내역의 복구 보존과 사용자에게 보여주는 상세 기록 보존은 별도 계약이다.

| 확인 상태 | 문구 제안 |
| --- | --- |
| 달성 권리 확인, 지급 대기 | 이전 임무 보상 지급 확인 중 |
| 원기간 사건 반영 확인 중 | 이전 임무 달성 여부 확인 중 |
| 지급내역 확정 | 이전 임무 보상 지급 완료 + 실제 품목 |
| 미달성 확정 | 기간 종료 · 목표 미달성 |
| 조회 실패 | 정산 내역을 불러오지 못했습니다 · 다시 확인 |

기간별 지급내역을 묶어 확인할 수 있지만 추가 수령을 요구하지 않는다. 알림 확인은
알림 읽음 처리일 뿐 보상 지급이 아니다. 지연 정산이 확정되면 실제 신규 지급품목만
알리고 과거 내역을 다시 열었다고 새 보상 팝업을 반복하지 않는다.

기존 수령가능 점과 정산 완료 알림은 구분한다. 정산 대기/조회불명/미달성을
'받을 수 있는 보상'으로 세지 않는다. 새 기간 임무 이용은 이전 정산 지연만으로
막지 않는다. 지갑 합계는 지급 확정 후 서버 잔액으로 반영한다.

<a id="source-54df3b38286fadd3-5-경제-입력과-수용-시나리오"></a>
#### 5. 경제 입력과 수용 시나리오

본 제안은 보상 단가/일·주 목표 상한을 늘리지 않는다. 다만 미수령을 소멸시키는
가정과 비교하면 실수령액과 지급시점이 달라진다. 이전 저활동 계산의 전량수령
조건에서는 목표별 총액이 유지되지만 성장에 사용할 수 있는 시점은 달라질 수 있다.
자동정산 실행 지연을 무시해04시에 확정 현금을 얻었다고 가정하지 않는다.

후속 현금흐름 입력은 권리획득, 직접수령, 자동정산 지급확정을 별개 사건으로 둔다.
관측기간 끝에 달성권리만 남으면 미수령 잔여로 보고하고 실제 지갑에 더하지 않는다.
예를 들어9/28에 시작한 일반주간의 종료는9/29 관측마감 밖이므로 이 주의 미수령
보상을 관측 안에 가상의 종료정산으로 넣지 않는다. 기존 합계 계산 재실행은 이번에 하지 않았다.

| 검토 상황 | 기대 결과 |
| --- | --- |
| D2달성·미수령 후04시, 이후 미접속 | D2직접보상1회 정산, 로그인/다른 임무 보상 없음 |
| D3진행1/2로 종료 | D3보상 없음, 새날0/2; 별도 D2권리 보존 |
| 종료직전 직접수령과 정산 동시 실행 | 하나의 지급내역, 재화별 중복 없음 |
| 03:59확정 사건이04:02에 목표 반영 | 원날 달성권리 판정 후 정산; 새날 진행 불변 |
| 요청03:59·최초확정04:01 | 새날 진행; 옛날 미달성 소급완료 없음 |
| 지급성공·응답유실·정산 재시도 | 기존 지급내역 복구 |
| 월요일04시 일/주 동시 종료 | 각 권리별 지급, 동일 사건을 목표별로만 인정 |
| 일부 재화 처리 실패 | 전체 완료 표시 금지, 미지급분만 안전 복구 |
| 옛 설정의 권리를 설정변경 후 정산 | 권리 생성 당시 보상 유지, 새 권리 생성 없음 |

위 표는 설계 수용 기준이며 실행 테스트가 아니다. 문서 대조 범위에서 일반 임무
미수령 종료정책을 찾지 못했으며 전체 서버에 기능이 없다는 주장은 하지 않는다.
실제 검증은 문서 저장·재읽기·상대링크·공백 검사만 수행한다. 런타임·배포·기기
검증 및 새 경제 시뮬레이션은 미수행. 다음은 채택한 정책을 기준으로 저활동 입력의
직접수령/정산 시각을 고정하고, producer·권리조회·정산 계약 확인 항목으로 연결한다.

<!-- source-body:source-54df3b38286fadd3:end -->


<a id="source-621b5eafbe983156"></a>
## Fifty-Level Pass Rewards Draft - 2026-09-08

원출처: `WorkspaceRepo/docs/ops/pass-50-rewards-draft.md`  
원문 SHA-256: `98091be2b7494ed2d843a5127efa84ea2d9eddc7536f8f83955fafb499930d94`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-621b5eafbe983156:start -->
<a id="source-621b5eafbe983156-fifty-level-pass-rewards-draft---2026-09-08"></a>
### Fifty-Level Pass Rewards Draft - 2026-09-08

User accepted the quantities and premium cosmetic allocation on2026-09-08.
This is approved DESIGN, not active config or full economy certification.
Accepted progression is pass-progression-policy.md. No runtime/Store changes.

<a id="source-621b5eafbe983156-per-level-pattern"></a>
#### Per-Level Pattern

Repeat this10level pattern five times. Premium resource amount is exactly5x
the free amount on the same row, so cumulative partial-progress ratios are5x.

| Level modulo10 | Free reward | Premium additional |
| --- | --- | --- |
| 1 | FREE crystal50 | FREE crystal250 |
| 2 | credits750 | credits3750 |
| 3 | parts75 | parts375 |
| 4 | operator EXP350 | operator EXP1750 |
| 5 | tactical data35 | tactical data175 |
| 6 | credits750 | credits3750 |
| 7 | parts75 | parts375 |
| 8 | operator EXP350 | operator EXP1750 |
| 9 | tactical data35 | tactical data175 |
| 0 | FREE crystal100 | FREE crystal500 |

Premium additional non-power cosmetics: frame at30, operator skin at40, train
skin at50. Premium allocation accepted; subjects/design/entitlement not made.
No cosmetic cash valuation or replacement of resource5x requirement.

<a id="source-621b5eafbe983156-totals"></a>
#### Totals

| Resource | Old free30 | Proposed free50 | Premium extra | Buyer combined |
| --- | --- | --- | --- | --- |
| FREE crystals | 500 | 750 | 3750 | 4500 |
| Credits | 5000 | 7500 | 37500 | 45000 |
| Parts | 500 | 750 | 3750 | 4500 |
| Operator EXP | 2500 | 3500 | 17500 | 21000 |
| Tactical data | 250 | 350 | 1750 | 2100 |

Crystal/credits/parts rise50percent; EXP/data40percent. Smaller increase for
EXP/data is a conservative recommendation, not measured optimal sink matching.

<a id="source-621b5eafbe983156-twenty-eight-day-aggregate"></a>
#### Twenty-Eight-Day Aggregate

Assume full attendance650,28daily20 each=560,4ordinaryweekly150 each=600,
complete free pass750:2560 FREE crystals. Premium adds3750 =>6310.
At daily discounted ticket50 used28times and normal100 sweep afterwards:
free28discount+11normal=39 tickets with60 remaining; buyer28+49=77 with10
remaining. Attendance elite guaranteed ticket1 separate; no pass elite ticket.
Crystals are all FREE even in premium pass; cash-paid crystal packs excluded.

Plan ran JS table summation and illustrative payment chronology from zero wallet.
Chronology assumes weekly direct crystals credited on third day of each7day block;
this is NOT proof of ordinary Monday-reset calendar behavior. The totals above
are conditional budget math, not an all-calendar exact maximum or live economy.
Full earliest pass XP follows daily200,weekly400 day1+200 day3 =>day17level50.

<a id="source-621b5eafbe983156-risks-and-next-decisions"></a>
#### Risks And Next Decisions

- Premium adds37 or38 normal purchases in this aggregate depending rounding,
  not claim of50 discounted pass purchases; daily quota is shared with all income.
- Distinct regular Monday reward windows and pass season weeks require separate
  calendar ledgers before activation. Late-start,missed days,recovery not budgeted.
- Materials totals across attendance/quests/pass (excluding runs/offline):
  free credits26300/parts1440/EXP6800/data760; buyer63800/5190/24300/2510.
- Server-authoritative growth/repair/research costs remain unverified; do not
  call growth balance complete. Do not invent paid skin exchange value.
- Cash price7900KRW remains prior candidate, not validated or published SKU.
- Existing5/15/etc milestones are material rows; only10multiples have large
  crystals. Further presentation grouping may emphasize them without new grants.

<a id="source-621b5eafbe983156-calendar-check---2026-09-08"></a>
#### Calendar Check - 2026-09-08

Plan directly computed42 cases:7 start weekdays x3 month lengths x2 tracks.
Assumptions: continuing every-day player, zero opening crystals,2 acknowledged
10wave operations per day. Regular weekly progress rolls on Monday, all3 weekly
objectives finish Wednesday; no reset/regrant at month boundary. Daily quests20
crystals and each completed regular weekly set150. Pass XP windows remain separate
season7day blocks, weekly600 on day3; no new pass weekly budget after day28 assumed.
One discounted purchase each day if affordable; normal exchange at month end.

| Days | Free crystals | Buyer crystals | Free ordinary tickets | Buyer tickets |
| --- | --- | --- | --- | --- |
| 28 | 2560 | 6310 | 39 | 77 |
| 30 | 2600-2750 | 6350-6500 | 41-42 | 78-80 |
| 31 | 2620-2770 | 6370-6520 | 41-43 | 79-80 |

Variation is4 versus5 ordinary weekly rewards in that calendar month, NOT a
fifth pass-week grant. Elite attendance ticket1 remains separate. These are
computed conditional scenarios, not all-player maxima; cold-start or missed-day
players and different objective timing need their own ledgers. Growth sinks still
unverified; no certification of overall economy or active server behavior.

Local uncommitted approved design with conditional calculations. Next sink check
and scoped implementation planning; no live release action implied.

<!-- source-body:source-621b5eafbe983156:end -->


<a id="source-d6799fef84ab2951"></a>
## Pass Benchmark - 2026-09-08

원출처: `WorkspaceRepo/docs/ops/pass-benchmark-2026-09-08.md`  
원문 SHA-256: `0e58ecb5a6aad6437b8009fdf6814a48da446a97553044da6a355d21f0e20991`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-d6799fef84ab2951:start -->
<a id="source-d6799fef84ab2951-pass-benchmark---2026-09-08"></a>
### Pass Benchmark - 2026-09-08

<a id="source-d6799fef84ab2951-scope"></a>
#### Scope

Research and design comparison only. No reward/config/code changes. Primary
sources below support Star Rail and Brawl Stars comparisons; failed retrieval
and unverified current Genshin/NIKKE reward tables excluded from numeric claims.
Regional prices, currencies, rarity/pity and season length differ: no universal
cash-value multiplier or player acquisition forecast established.

<a id="source-d6799fef84ab2951-sources-and-findings"></a>
#### Sources And Findings

- Star Rail official v4.5 announcement (2026-08-17):
  https://www.hoyolab.com/article/46328730
  Paid Nameless Glory gives680 Stellar Jade immediately,4 Special Passes through
  progress, selectable4-star Light Cone and crafting/growth rewards. Free track
  includes ordinary passes and growth resources. This particular period has70
  levels over5 weeks and adjusted XP. Do not generalize period to all seasons.
- Brawl Stars official January2026 overhaul announcement:
  https://supercell.com/en/games/brawlstars/blog/news/new-power-brawl-pass-changes-and-a-new-starr-drop-2/
  Base/Plus announced USD8.99/12.99 with regional variation. Free growth remains;
  paid tiers combine resources, cosmetic/title rewards and choice-vault keys.
  Resource key has explicit alternatives2000coins/2000powerpoints/5000bling.
  Choice reduces unwanted resource allocation; it does not create cross-game
  exchange equivalence. This is dated overhaul policy, not audited current store.
- Subsequent official Brawl September2026 season notice:
  https://supercell.com/en/games/brawlstars/blog/release-notes/release-notes-august-2026/
  Confirms current seasonal pass skin/chroma/title themes; full latest free/paid
  ratio not derivable from text alone. No claim that industry norm is5x.

<a id="source-d6799fef84ab2951-last-rail-comparison"></a>
#### LAST RAIL Comparison

Current candidate: monthly30levels/3000XP, free500 and premium-extra2500 FREE
crystals; same-kind per-level premium exactly5x, buyer combined6x. At historical
100 crystals/ticket, premium extra is25 ordinary tickets at full price. The
daily50 offer has shared account/day quota, so claiming50 additional pass tickets
within a month is incorrect. 37vs62 total modeled tickets is not pass-only income.

Strengths: explicit5x ratio, free rewards preserved, shared progression, no paid
XP acceleration, retrospective earned rewards. Weaknesses: repeated six-level
pattern and final credit reward lack a distinct completion milestone; growth
material5x is not proof of acceptable growth speed without authoritative sinks.

Recommendations (NOT approved changes):
1. Retain existing5x minimum instead of replacing materials with invented skin value.
2. Redistribute existing amounts into5/10/15/20/25/30 milestones with paired free/
   premium rewards so same-progress5x remains true. No total reduction inferred.
3. Consider one cosmetic train livery/profile frame as extra finale, subject to art
   production scope and explicit approval. Not an exclusive-power unit/item.
4. Consider bounded growth choice only after server-authoritative costs/quotas,
   with equal-value options verified; not an immediate generic shop expansion.
5. Keep attendance elite guarantee distinct from paid-pass acquisition incentive.
6. Price7900KRW remains proposal. Compare only after local Store pricing and cash
   crystal packs exist; no claim of competitor cash-price superiority.

<a id="source-d6799fef84ab2951-verification-limits"></a>
#### Verification Limits

Official web sources retrieved through web search; some HoYoLAB direct open pages
return loading-only, so announcement text is from indexed official-author result.
No in-game purchases, current KR price screenshots, source model rerun or live
economy modifications performed. Local uncommitted research document.

<!-- source-body:source-d6799fef84ab2951:end -->


<a id="source-901d352e4d663bd3"></a>
## Pass Progression Policy

원출처: `WorkspaceRepo/docs/ops/pass-progression-policy.md`  
원문 SHA-256: `4fcd40c8758f72b154f3085f418faceb9def361f0143ece3a78829f3f2b11dd7`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-901d352e4d663bd3:start -->
<a id="source-901d352e4d663bd3-pass-progression-policy"></a>
### Pass Progression Policy

<a id="source-901d352e4d663bd3-accepted---2026-09-08"></a>
#### Accepted - 2026-09-08

- Maximum level50; cumulative threshold level *100XP, level50=5000XP.
- Free and premium tracks share earned XP/level. No paid XP acceleration.
- Daily reset KST04:00. Ordinary weekly quests keep Monday04:00 reset.
- Pass-specific weekly windows are season-relative seven-day blocks, not Monday
  weeks: days1-7,8-14,15-21,22-28. Interpret against season start, not account join.
- Daily pass quests: login40XP; one valid operation end80XP; two valid operation
  ends80XP. Total200XP/day.
- Weekly pass quests: three distinct attendance days200XP; six valid operation
  ends200XP; sixty acknowledged waves200XP. Total600XP/full window.
- Solo OR cooperative operation progress; each objective grants once. One event
  can contribute to separate daily/weekly objectives, not duplicate same reward.
- No mandatory purchase, spending, recruitment or World Boss objective for this
  pass pacing. Extra pass XP sources are not included in the accepted budget.

<a id="source-901d352e4d663bd3-period-boundary-detail-proposal---2026-09-09"></a>
#### Period Boundary Detail Proposal - 2026-09-09

Planning only. Season commit-time attribution is already approved in
pass-season-end-proposal-20260909.md; daily/weekly details below clarify the proposed
finish-snapshot model, not verified live behavior. Outcome qualification remains
the separate proposal below. No new XP, grace period or compensatory grant.

Compare start-time reservation, client result arrival and authoritative commit:
retain authoritative qualifying-event commit. Start-time reservation could keep
expired objectives open; response arrival makes identical retries change periods.
Use half-open periods [startsAt, endsAt); an event exactly at04:00 belongs to the
new period. Store immutable event time and the resolved period identities once.

| Case | Proposed attribution |
| --- | --- |
| Start03:58, accepted finish04:02 | New daily period; no credit to yesterday from start time |
| Finish committed03:59, response received04:02 | Original daily period, including any earned objective receipt |
| Request received03:59, authoritative finish commits04:01 | New period; request/client timestamps do not backdate completion |
| Retry original accepted finish after reset | Original receipt and periods; no second count or reward |
| Monday04 crossed, pass block unchanged | New ordinary quest week, same pass week; independent calendars |
| Pass seven-day block crossed, ordinary week unchanged | New pass block, same ordinary quest week |
| Finish on season days29-31 | Daily only; no fifth pass weekly bundle |
| Battle starts before season end, finish commits after | New season objectives only if available and eligible; no expired-season XP |
| Earlier committed event processed asynchronously after close | Original season; settle once after reconciling committed eligible events |

Daily and weekly counters may consume one event independently, but each
account+objective+period reward grants once. Both eligible cooperative participants
use the same authoritative run-finish time even if their clients reconnect at
different times; their already completed personal objectives can differ.
The proposed finish-snapshot model places all accepted waves with that finish.
Do not silently apply this to an existing incremental-wave implementation.

Persist event identity and period with the accepted finish, or atomically enqueue
a durable event with those attributes. Queue delivery time must not choose season.
Settlement must account for accepted-but-unapplied events before finalizing the
season snapshot, or reconcile them idempotently; do not mark settled while dropping
their entitlement. This is an implementation requirement, not an existing API claim.

An operation finishing after04:00 does not itself count as a new-day login or
attendance. Those use their own accepted event predicates; no forced relogin design
is introduced here. Recovered attendance still gives no loginXP. Entry cost stays
with its original admission receipt; changing quest periods never recharges entry.

Frontend reads current periods and awarded deltas from the server. An older receipt
may be displayed as previous-period progress without adding it to current counters.
During battle reset, do not interrupt combat with a quest popup; refresh after the
result or return to lobby. Countdown is informational, not an award authority.

Future verification: before/exactly/after04:00; Monday versus season-block boundary;
day28/29 and month transition; delayed queue; concurrent retry and settlement;
cooperative reconnects; unavailable new season; already completed thresholds.
No runtime tests or deployment performed for this design.

<a id="source-901d352e4d663bd3-outcome-qualification-proposal"></a>
#### Outcome Qualification Proposal

<a id="source-901d352e4d663bd3-operation-outcome-credit-proposal---2026-09-09"></a>
##### Operation Outcome Credit Proposal - 2026-09-09

New clarification proposed for previously underspecified valid operation end. Not
verified current runtime behavior or an approved change to existing quest products.
Keep daily40/80/80 and weekly200/200/200 unchanged; only event qualification below
is proposed. No XP grant from client end-screen arrival or retry response count.

| Accepted outcome | Proposed pass operation-end count | Proposed acknowledged waves |
| --- | --- | --- |
| Ordinary success |1|Actual completed15|
| Ordinary normal defeat with at least1 completed wave |1|Actual completed waves only|
| Ordinary defeat before completing wave1 |0|0|
| Voluntary abandon, invalid or cancelled run |0|0 in this finish-based proposal|
| Pending/lost response |Pending, no provisional grant|Pending|
| Story raid success |1|0; boss-only is not15waves|
| Story raid defeat, including proposed mutual destruction |0|0; failure-refund loop must not farm end-count XP|

The at-least1wave failure threshold and story-failure exclusion are recommendations
requiring adoption/owner contract review. Do not change failureEXP or first-clear
rewards: these are separate systems. A costly ordinary loss may count without
inventing a personal damage floor or making every failure suspect. Existing per-day/
week objective limits prevent repeated same-objective payout; more attempts do not
grant uncapped passXP. No automatic cheat penalty or increased entry cost proposed.

World participation remains its separately authored track; no new pass source or
mandatory World quest inferred here. Solo/coop use the same objective counters, not
two independently claimable tracks. In coop, count only eligible original participants
under the mode's accepted participation outcome; support/low damage is not exclusion.

Recommend committing acknowledged waves and end count from one authoritative accepted
finish snapshot for this first implementation, scoped by account+run+objective period.
Do not also increment per-wave events without reconciling duplicates. If live code
already commits wave progress incrementally, owner must reconcile abandonment/time
boundaries first; do not silently retract already earned progress with this proposal.
Qualifying finish commit selects daily/weekly period under the existing season event
policy; retry keeps original identity/period, not the receipt's new delivery time.

Use player-facing objective wording equivalent to 'complete valid operations' only
with a concise details view explaining defeat eligibility, rather than label it
'victories'. Separate 'clear' objectives keep success-only semantics. Main result
may show actual added progress after server acknowledgment; no assumed80XP per run
once both daily thresholds already completed. Attendance recovery grants no loginXP.

Future cases:0/1/14/15waves, abandon after partial progress, boss-only success/failure,
duplicate/concurrent finish,04boundary, week boundary, late response and eligible
coop support. No such runtime tests executed. Existing fastest successful-run and
late-entry arithmetic is unchanged; defeat pacing requires a new explicit model.

Scoped source check: Plan read policy and searched current owner game-api index for
operation-end terms; no complete quest event predicate established. This is not a
whole-server absence claim. No runtime modification or grant was made.

<a id="source-901d352e4d663bd3-verified-calculation"></a>
#### Verified Calculation

With season-day1 start, daily objectives completed, and grindable weekly goals
completed immediately, weekly400XP is available on day1 of each block and the
remaining200XP on day3. Earliest cumulative results:

| Season day | XP | Level |
| --- | --- | --- |
| 7 | 2000 | 20 |
| 14 | 4000 | 40 |
| 15 | 4600 | 46 |
| 16 | 4800 | 48 |
| 17 | 5200 | 50 |

This is arithmetic verification, not implemented event-source, fuel/access or
device testing. Third-week completion assumes all legal objectives are achievable.

<a id="source-901d352e4d663bd3-related-decisions-and-remaining-design"></a>
#### Related Decisions And Remaining Design

- Milestones30 profile frame,40 operator skin,50 train skin; premium placement
  recommended, final art subjects not yet selected. Non-power cosmetics.
- Same-kind premium reward minimum5x free, with cosmetics additional and no
  invented cash-equivalent value. Fifty-level increased reward amounts pending.
- Calendar month season, days29-31 daily-only and earned reward settlement are
  approved in pass-season-end-proposal-20260909.md, superseding the older pending
  note here. No fifth weekly budget or XP carryover. Runtime remains unverified.
- Supersedes older30level/3000XP and Monday-week PASS pacing drafts. Does not
  change ordinary weekly reset, attendance rules or active server configuration.
- No code/seed/deployment/Store publication is authorized by this policy file.

Local planning record, uncommitted. Plan owns further numerical design.

<!-- source-body:source-901d352e4d663bd3:end -->


<a id="source-cee45fb251120260"></a>
## Pass Season End Proposal

원출처: `WorkspaceRepo/docs/ops/pass-season-end-proposal-20260909.md`  
원문 SHA-256: `3561572f1e444936ccd7e9b6f61dceab5e8e2e512a3806d2377f2aef7373fd4a`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-cee45fb251120260:start -->
<a id="source-cee45fb251120260-pass-season-end-proposal"></a>
### Pass Season End Proposal

Status: USER APPROVED DESIGN on2026-09-09, not implemented/deployed. Preserve50levels,100XP/level,
daily200XP, four season-relative weekly600XP blocks, premium extra5x and cosmetics
at30/40/50. No paidXP boost, no required solo-plus-coop completion.

<a id="source-cee45fb251120260-calendar-and-xp"></a>
#### Calendar And XP

Monthly season: calendar-month day1 at04:00KST through next-month day1 at04:00KST,
exclusive end. Four full weekly blocks days1-7/8-14/15-21/22-28. Days29-31 retain
daily quests only; no fifth600XP weekly bundle. Last login before end does not
reserve the season. Server-authoritative qualifying event commit time selects
season; events after end do not add to expired season. Already committed events
and retries use original receipt. Final settlement follows committed eligible
events and uses transaction/fencing against concurrent claims.

Under existing fastest schedule (weekly400 on day1,200 on day3), day16 is
3200daily+1200prior weeks+400current week=4800XP. Day17 is3400+1800=5200XP,
therefore first50 remains day17. Cap earned level at50; excess XP expires,
no repeat rewards. Actual event timing remains server-authoritative.

<a id="source-cee45fb251120260-claims-and-paid-track"></a>
#### Claims And Paid Track

During season players may claim earned level rewards normally. On season close,
automatically credit all earned, unclaimed free rewards and owned premium rewards
once via durable idempotent settlement. Show a settlement notification/history;
not an expiring mailbox requiring another claim. If execution fails retry safely,
never silently forfeit. Same season/account/track/level receipt shared by manual
claim, premium backfill and final settlement. Cosmetics included by unique grant.

Premium unlock includes earned lower levels; unpaid players never receive paid
track rewards. Premium purchase creates no XP/levels. Approved purchase cutoff
24hours before season end, to reduce late-payment disputes. UI clearly shows
season end and purchase cutoff from server, no assumption all rewards obtainable.
Server creates short-lived checkout intent before cutoff. A store-confirmed valid
transaction tied to that intent and that season must be reconciled even if
verification arrives later: grant earned premium rewards by retained final XP
snapshot, not next-season entitlement. Never silently drop valid paid purchases.
Exact Store transaction timestamp/validation/refund integration needs separate
technical review; this policy does not authorize live SKU or purchases.

Reset season XP/quest counters/premium entitlement only. Keep claimed currency,
cosmetics, inventory, receipts and immutable season snapshots. New season is
independent. Refund/chargeback follows existing paid provenance design, not an
invented destructive balance correction here.

<a id="source-cee45fb251120260-frontend-and-verification"></a>
#### Frontend And Verification

<a id="source-cee45fb251120260-late-entry-and-purchase-disclosure-detail---2026-09-09"></a>
##### Late Entry And Purchase Disclosure Detail - 2026-09-09

Preserve approved fixed calendar season and24h purchase cutoff. Late account entry
does not start a personal season or replay expired weekly/daily objectives. Premium
does not add XP, levels or days. No new discount, catchup source or live product here.

Actual Node arithmetic iterated28/30/31day seasons and starts1/8/15/22, assuming login
and two acknowledged15wave completions each active day. Award daily200 and each
current block's attendance3/ends6/waves60 weekly200 once; no weekly reward days29+.

| Start day | End level28day season |30day|31day|
| --- | ---: | ---: | ---: |
|1|50|50|50|
|8|50|50|50|
|15|40|44|46|
|22|20|24|26|

First50 is day17 for day1start and24 for day8start in these cases. This is conditional
arithmetic, not measured clear/fuel feasibility or a promised player outcome. No
extra XP sources assumed; actual account quest availability must be checked. Do not
present this canned table as a live eligibility calculation.

Recommend purchase detail separates currently earned premium levels (available after
validated purchase), future unearned levels and season/cutoff timestamps. Any remaining
XP/attainable-level indicator must use server-approved remaining objectives and account
progress, labeled conditional on completing those objectives. Do not guess from only
remainingDays*200 or replay already claimed weekly rewards. If no such contract exists,
show current earned rewards and actual dates, not an invented forecast.

When remaining authorized XP cannot reach the final milestone, explicitly disclose
that milestone is unreachable this season before confirmation. Do not lock a useful
lower-level premium purchase solely because50 is unreachable while within cutoff;
retain informed choice and normal store confirmation. After cutoff do not create a
new checkout; late existing valid transactions retain the original reconciliation rule.

Keep30/40/50cosmetic cells visibly locked until earned. Purchase confirmation must
not animate all50rewards as granted; only earned entitlements can settle. Season-end
auto-settlement credits earned unclaimed rewards, never the missing levels. No warning
that already earned rewards will disappear if users do not buy immediately.

Future cases: start within partly elapsed weekly block, all month lengths, near04
boundary, already claimed objectives, unavailable operation, cutoff during checkout,
late validated payment and missing forecast fields. Only12 arithmetic cases ran;
no store, server, UI or device tests. UI additions remain proposals, existing cutoff
and auto-settlement policy unchanged.

Unity queries seasonID/version, server time, endsAt/purchaseEndsAt, level/XP,
track ownership, reward rows and claim states. Server decides final eligibility.
Display ended/settlementPending/settled and retry errors distinctly. No client
grant, reward table hardcode or local-clock rollover authority.
Acceptance: month lengths28-31, timezone04boundary, concurrent manual/automatic
settlement, login absent at end, failed-job replay, earned-level-only grants,
late valid purchase, repeated store callbacks, no paid rights next season.
No runtime tests executed for this proposal.

<!-- source-body:source-cee45fb251120260:end -->


<a id="source-04d9d21432054734"></a>
## Quest Outcome Eligibility Proposal — 2026-09-10

원출처: `WorkspaceRepo/docs/ops/quest-outcome-eligibility-proposal-20260910.md`  
원문 SHA-256: `69b643bd5c5aadb21dc1e17135c458ecad5dfc291562cc8e817fc24d8485fbe7`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-04d9d21432054734:start -->
<a id="source-04d9d21432054734-quest-outcome-eligibility-proposal--2026-09-10"></a>
### Quest Outcome Eligibility Proposal — 2026-09-10

Plan_1 상세기획 제안. 사용자 'ㄱㄱ'는 직전 예고한 인정조건 설계 진행 승인이다.
아래 새 포함 범위/패배 자격의 최종 채택이나 런타임 적용 승인은 아직 아니다.
승인된 일/주 직접보상 분배·패스 XP·초기5/솔로·협력 정책은 유지한다.

<a id="source-04d9d21432054734-design-direction-accepted--2026-09-10"></a>
#### Design Direction Accepted — 2026-09-10

후속 사용자 '다음 세부 기획 ㄱㄱ'에 따라 아래 권장 인정조건을 다음 UI 설계의
설계 방향으로 채택한다. 앞의 미채택 표시는 최초 작성 시점의 기록이다.
Story15wave 패배는 실제 비용/환급 계약 확인 조건부이며, 기존 incremental-wave
receipt 보존·동시파괴 판정 미확정·World 별도트랙 경계는 그대로다.
실제 producer/API parity 확인이나 런타임 적용·배포 승인이 아니다.
후속 [결과 화면 표시 상세](44_COMBAT_DETAIL_CONTRACTS.md#source-3d41221b4ed5c4ed)는 신규 제안이다.

Sources: [pass progression](47_ECONOMY_DETAIL_CONTRACTS.md#source-901d352e4d663bd3),
[accepted distribution/input](47_ECONOMY_DETAIL_CONTRACTS.md#source-02711d8c0e43e1fa),
[coop recurring policy](46_COOP_DETAIL_CONTRACTS.md#source-29ac58e206039d36),
[cost recovery](44_COMBAT_DETAIL_CONTRACTS.md#source-4e1a948cfc112887).

<a id="source-04d9d21432054734-권장-방향"></a>
#### 권장 방향

승리만 인정하면 전투에 참여한 패배자의 목표 진행이 전부 사라진다.
종료만으로 무조건 인정하면 즉시 포기/환급 반복으로 횟수 목표를 채울 수 있다.
권장안은 서버 확정 결과와 최소 완료wave를 사용하고, 유효한 boss 성공은 종료1회로
인정하되 없는 wave를 만들지 않는 방식이다. 모든 패턴을 승리로 맞추지는 않는다.

일반 퀘스트 D2/D3/W2와 패스 종료목표는 같은 적격 사건을 참고하도록 제안한다.
일반 W3와 패스 wave목표도 동일 completed-wave 값을 사용하되 기간과 receipt는
각 시스템별로 분리한다. 이름은 '유효 작전 종료'로 제안하고 세부 인정조건을 설명한다.
이는 기존 '일반작전' 문구를 story/boss까지 확장하는 신규 제안이며 조용히 적용하지 않는다.

<a id="source-04d9d21432054734-결과별-제안표"></a>
#### 결과별 제안표

| 서버 확정 사건 | 종료 횟수 | 인정wave | 비고 |
| --- | ---: | ---: | --- |
| 일반15wave 작전 성공 |1|15|솔로/협력 동일 |
| 일반15wave 정상 패배, 완료1~14wave |1|실제 완료분|일반 패배 입장비 유지 정책; 개인 피해량 조건 없음 |
| 첫wave 완료 전 패배 |0|0|진입만으로 진행하지 않음 |
| 스토리15wave 성공 |1|15|스토리 선택도 허용하는 신규 제안; 스토리만 요구하는 목표는 없음 |
| 스토리15wave 정상 패배, 완료1wave 이상 |1|실제 완료분|해당 모드가 정상 비용소모 패배로 인정될 때; 별도 무효화/환급 계약 확인 필요 |
| 스토리 boss-only 성공 |1|0|15wave 환산 금지; 기존 패스 제안과 맞춤 |
| 스토리 boss-only 패배 |0|0|기존 실패환급 경계 유지; 동시파괴는 확정 outcome 규칙을 따름 |
| 자발적 포기, 시작 전 취소, 무효 run |0|0|finish-snapshot 신규 모델 기준 |
| 결과 미확정·응답 유실·검토 격리 중 |대기|대기|아직0 확정이 아님; 서버 최종처리 후 판정 |
| World Raid 유효 참여 |이 표 제외|이 표 제외|기존 주간 World 참여트랙만; 추가 일일/패스XP 없음 |

일반15wave 패배에서15wave 완료가 함께 들어오거나 boss 결과/형식이 모순이면
자동 성공/0으로 보정하지 않고 모드 계약 확인 대상으로 둔다. 서버가 허용하는
최종 이벤트 정의를 먼저 확인한다. 클라이언트의 wave/성공/self-report를 신뢰하지 않는다.
스토리15wave 실패에 별도 환급정책이 있으면 해당 계약을 대조할 때까지 그 가지를
분석에서 BLOCKED로 표시한다. 이번 source는 story raid 환급만 확인하며 전체 story
환급 존재/부재를 단정하지 않는다. 실제 catalog metadata 없이는 포맷만으로 권리를 주지 않는다.

<a id="source-04d9d21432054734-비용환급과-목표의-관계"></a>
#### 비용·환급과 목표의 관계

환급 여부는 결과유효성과 구분한다. 입장 미성립/서버 취소/무효 run에는 목표가 없다.
정상 story boss 패배는 기존 실패환급과 함께 위 표대로0이며, 일반 정상 패배는
기존 패스 제안과 같이 완료1wave부터 인정한다. 별도 운영 보상/고객지원 환급을
나중에 받았다는 이유만으로 이미 확정된 목표를 자동 회수하는 새 정책은 만들지 않는다.

종료목표 인정은 '클리어'가 아니다. 패배는 새 스토리 진행·첫클리어·슬롯을 열지 않는다.
EXP/재료 패배보상도 기존 정책대로 별도 계산한다. 같은tick 동시파괴의 실패 우선 및
story 환급 overflow는 기존 미채택 제안으로 남기며 이 문서로 확정하지 않는다.

<a id="source-04d9d21432054734-협력접속재처리"></a>
#### 협력·접속·재처리

두 적격 원참가자가 같은 run을 완료하면 각 계정이 자기 목표에1회씩 반영한다.
한 계정이 train/squad 두 역할 몫을2회 받지 않는다. 개인 딜/막타/치유량 하한 없음.
이전부터 정한 참가·접속 적격성은 유지하고, 낮은 기여도만으로 참가를 박탈하지 않는다.
도중 접속 끊김만으로 포기를 확정하지 않는다. 재접속 또는 종료복구로 원래 결과를 확인한다.

목표 귀속은 서버가 확정한 qualifying event 시각/ID를 저장해 재처리에도 유지한다.
03:59 확정 후04:02 응답은 원래 기간,03:59 요청 후04:01 최초 확정은 새 기간이다.
격리 해제도 이미 적격 사건이 확정돼 있었는지와 이제 처음 확정되는지를 구분한다.
검토용 접수시각을 과거 reward period로 소급하는 새 권한을 만들지 않는다.

기존 구현이 incremental-wave로 이미 적법한 진행을 확정했다면, 포기0/finish귀속을
소급 적용해 취소하지 않는다. 실제 producer/receipt를 확인하고 버전 경계를 정해야 한다.
이 설계는 새 finish-snapshot 흐름의 후보이며 기존 원장 마이그레이션 지시가 아니다.

<a id="source-04d9d21432054734-플레이어-표시와-예시"></a>
#### 플레이어 표시와 예시

목표: '유효 작전 1회 종료', '유효 작전 2회 종료', '유효 작전 6회 종료',
'작전에서 60웨이브 완료'. 설명: '15웨이브 작전은 1웨이브 이상 완료 후 정상 종료하면
패배도 횟수에 포함됩니다. 보스전은 승리만 횟수에 포함되며 웨이브는 늘지 않습니다.
포기·취소·무효 결과는 포함되지 않습니다. 월드 레이드는 별도 참여 목표입니다.'
최종 catalog 범위를 확정한 뒤 표시문구도 같이 확정한다.

예시(계산 규칙 설명, 실행 테스트 아님): 정상 일반 패배7waves 다음 정상 일반
패배4waves는 종료2/누적11waves. boss 성공1회는 종료1/waves0.
1wave 뒤 포기는 이 신규 모델에서0/0. 각각 일일/주간 보상은 목표당1회이며
횟수1건이 언제나80XP 지급을 의미하지 않는다. 이미 달성한 목표에는 추가 지급 없음.

보스만 플레이하면 횟수목표는 충족할 수 있어도60wave 목표는 미달이다.
이 표를 모든 선택콘텐츠가 모든 목표를 충족한다는 약속으로 표현하지 않는다.
솔로/협력 각 모드로 전체 recurring 트랙을 달성할 수 있다는 기존 정책은 유지한다.

<a id="source-04d9d21432054734-검증과-다음-단계"></a>
#### 검증과 다음 단계

실제 수행: 연결 원문 읽기·제안 간 일관성 검토·문서 재읽기. 런타임/자동화 테스트,
실제 보상·환급/새 계산모델·배포·영상 실행 없음. 문서는 로컬 미커밋.
채택 후 필요한 계약 사례:0/1/14/15waves, 부분포기, boss 성공·실패·동시파괴,
협력 지원자/재접속, 최초 확정 전후04경계, 주간/시즌 경계, 중복 finish,
격리 해제, 이미 수령한 목표, 기존 incremental-wave 이력.
다음은 이 표의 신규 포함범위/패배조건을 채택하고 그 범위를 기준으로 입력을 고정하는 것.
실제4개 작전 catalog와 원격 구현의 미확인은 남는다.

<!-- source-body:source-04d9d21432054734:end -->


<a id="source-9021ee087bd479a3"></a>
## Repair and Compensation Policy

원출처: `WorkspaceRepo/docs/ops/repair-compensation-policy.md`  
원문 SHA-256: `6f36d75e56de081f8a0cb31d9790b4ba13d18292157c128170bf75933980b872`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-9021ee087bd479a3:start -->
<a id="source-9021ee087bd479a3-repair-and-compensation-policy"></a>
### Repair and Compensation Policy

<a id="source-9021ee087bd479a3-purpose"></a>
#### Purpose

This document defines the conditions required before LAST RAIL opens manual repair or compensation tools.

Launch support remains read-only. Repair and compensation are future controlled workflows that require explicit permission, audit records, evidence, and limits.

<a id="source-9021ee087bd479a3-current-launch-position"></a>
#### Current Launch Position

Manual repair is not available at launch.

Support staff may:

- Review server-owned records.
- Use approved response templates.
- Attach read-only evidence.
- Escalate cases for review.

Support staff may not:

- Grant currency manually.
- Grant items manually.
- Reset offline reward timestamps.
- Mutate story progress.
- Mutate unlocks.
- Reroll gacha results.
- Edit reward transactions.
- Edit economy ledger entries.
- Publish localization or narrative changes.

<a id="source-9021ee087bd479a3-repair-principles"></a>
#### Repair Principles

Any future repair workflow must follow these principles:

- Server records are the source of truth.
- Repair must reference the original affected record.
- Repair must create a new audited correction record, not rewrite history.
- Repair must be idempotent where replay is possible.
- Repair must have an explicit reason code.
- Repair must have an operator id.
- High-impact repairs require owner approval.
- Player-facing messaging must not expose exploit-sensitive internals.

<a id="source-9021ee087bd479a3-permission-model"></a>
#### Permission Model

Launch permission:

- `SUPPORT_READ`: read-only support investigation only.

Future repair permissions:

| Permission | Purpose | Suggested Roles |
| --- | --- | --- |
| `SUPPORT_ESCALATION_READ` | Read escalation status and evidence package. | VIEWER, OPERATOR, OWNER |
| `SUPPORT_ESCALATION_WRITE` | Mark case review status or attach notes. | OPERATOR, OWNER |
| `SUPPORT_REPAIR_PREVIEW` | Preview a proposed repair without committing it. | OPERATOR, OWNER |
| `SUPPORT_REPAIR_APPROVE` | Approve high-impact repair. | OWNER |
| `SUPPORT_REPAIR_EXECUTE` | Execute an approved repair transaction. | OWNER or restricted OPERATOR |

No future permission should imply unrestricted economy mutation.

<a id="source-9021ee087bd479a3-repair-categories"></a>
#### Repair Categories

| Category | Allowed Future Repair Type | Launch Status |
| --- | --- | --- |
| `reward_claim_missing_ledger` | Replay stored reward plan into ledger through a correction transaction. | Disabled |
| `reward_preview_without_claim` | Reopen or retry claim against stored reward preview. | Disabled |
| `offline_state_conflict` | Correct offline state timestamp/revision with evidence. | Disabled |
| `offline_claim_duplicate_mismatch` | Reconcile idempotency and ledger records. | Disabled |
| `story_unlock_missing` | Apply missing unlock/progress from verified full victory. | Disabled |
| `gacha_guarantee_conflict` | Escalate for manual review; direct reroll discouraged. | Disabled |
| `integrity_restricted_reward` | Owner review after integrity disposition. | Disabled |
| `account_identity_conflict` | Account recovery or merge workflow. | Disabled |
| `narrative_content_missing` | Content/localization fix, not player-state repair. | Disabled |

<a id="source-9021ee087bd479a3-required-evidence"></a>
#### Required Evidence

Every future repair request must include:

- support ticket id
- account id
- case type
- escalation category
- severity
- primary reference id
- affected transaction ids
- observed failure
- proposed repair action
- player-facing summary
- operator id
- approval status

Category-specific evidence:

- Run reward repair: run id, reward preview id, reward claim id if any, reward transaction id if any.
- Offline reward repair: offline reward claim id, state revision, last claim timestamp, story progress id.
- Story unlock repair: run id, operation id, outcome, cleared waves, progression ledger state.
- Gacha repair: gacha batch id, banner id, guarantee type, pity state, ledger state.
- Account recovery: candidate account ids, non-secret auth identity references, ownership proof status.

Do not include:

- tokens
- raw auth headers
- raw request bodies
- raw RNG material
- seed preimages
- per-candidate probabilities
- full weight lists
- raw multiplier bps
- internal reward calculation details
- localized dialogue full text

<a id="source-9021ee087bd479a3-audit-requirements"></a>
#### Audit Requirements

Future repair execution must create immutable audit records.

Required audit fields:

- audit id
- support ticket id
- account id
- operator id
- approver id if required
- permission used
- repair category
- reason code
- affected reference ids
- before snapshot hash or sanitized before summary
- after snapshot hash or sanitized after summary
- idempotency key if applicable
- created at

Audit records should not store secrets, raw request bodies, raw RNG, probability tables, or exploit-sensitive calculations.

<a id="source-9021ee087bd479a3-transaction-rules"></a>
#### Transaction Rules

Future repair transactions must:

- Be atomic.
- Avoid direct edits to historical ledger rows.
- Create correction ledger rows instead of rewriting prior ledger.
- Attach reason code and support ticket id.
- Be idempotency-protected when retried.
- Fail closed if evidence does not match current server state.
- Return a sanitized result with reference ids only.

<a id="source-9021ee087bd479a3-compensation-limits"></a>
#### Compensation Limits

Future compensation limits should be conservative.

Launch recommendation:

- No manual compensation.

Future limit candidates:

- Low-impact compensation can be previewed by OPERATOR and approved by OWNER.
- High-impact currency or rare item compensation always requires OWNER approval.
- `cur_special_core`, `cur_command_ticket`, and `cur_command_ticket_fragment` require explicit allowlist.
- Gacha reroll should remain prohibited unless a separate incident policy approves it.
- Compensation should not exceed the verified missing value from server records unless incident policy allows it.

<a id="source-9021ee087bd479a3-player-facing-messaging"></a>
#### Player-Facing Messaging

Use:

- "서버 기록 검토 후 필요한 경우 별도 기준에 따라 보정됩니다."
- "현재는 임의 지급 없이 저장된 기록을 기준으로 확인합니다."
- "보정이 필요한 경우 승인 절차와 기록 확인을 거쳐 처리됩니다."

Avoid:

- "바로 지급해 드리겠습니다" before approval.
- "확률 오류였습니다" without verified incident policy.
- "기록을 수정했습니다" unless an audited repair actually committed.
- "다시 뽑아 드리겠습니다" for gacha reports.

<a id="source-9021ee087bd479a3-non-goals"></a>
#### Non-Goals

This policy does not implement:

- Repair APIs
- Admin repair buttons
- Ticket system integration
- Economy correction jobs
- Account merge tooling
- Gacha reroll tooling
- Localization publishing

<a id="source-9021ee087bd479a3-rollout-order"></a>
#### Rollout Order

1. Keep launch support read-only.
2. Add support account summary read-only endpoint and Admin page.
3. Add escalation status read-only placeholders.
4. Define audit table and permission model.
5. Add repair preview endpoint.
6. Add owner approval workflow.
7. Add narrow repair execution endpoint for one category.
8. Expand only after production incidents prove the need.

<a id="source-9021ee087bd479a3-open-decisions"></a>
#### Open Decisions

- Whether repair approval should be in Admin or an external ticketing system.
- Whether support ticket id should be required for all repair previews.
- Whether OPERATOR can execute low-impact repairs after OWNER approval.
- Which currency/item ids are eligible for compensation.
- Maximum daily compensation per account and per operator.
- Whether repair audit data needs a separate retention policy.
- Whether account recovery belongs in support repair or a separate identity workflow.

<!-- source-body:source-9021ee087bd479a3:end -->


<a id="source-e146be10f59741ea"></a>
## 랭킹 보상 정책 변경

원출처: `docs/design/RANKING-REWARD-POLICY-20260914.md`  
원문 SHA-256: `7e9d25daa2130f8633c21a0d1052ce58c076290aeea18e3fa5d369377a0f1caa`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Same eligible players same growth currency; rank-only cosmetics/prestige nonconvertible. Score-tier rewards independent; old rankresource curves superseded.

<!-- source-body:source-e146be10f59741ea:start -->
<a id="source-e146be10f59741ea-랭킹-보상-정책-변경"></a>
### 랭킹 보상 정책 변경

2026-09-14 사용자 확정. LASTRAIL의 랭킹 보상에 적용한다. 기존 순위별 재화/성장 재료 차등 기획보다 우선한다.

<a id="source-e146be10f59741ea-확정-규칙"></a>
#### 확정 규칙

- 동일 이벤트의 동일 참가·수령 자격을 충족한 계정은 순위와 무관하게 같은 인게임 재화와 성장 보상을 받는다. 재화 종류·수량·수령 횟수에 순위 배율이나 상위권 추가 지급을 두지 않는다.
- 순위권 차등 보상은 스킨 등 게임 성능·성장에 영향을 주지 않는 외형/명예 보상으로 제한한다. 능력치, 성장 재료, 경험치, 뽑기/입장권, 재화 전환·판매·교환 가치, 수집 능력치 보너스를 통해 우회적인 성장 이점을 주지 않는다.
- 예시로 스킨·칭호·프로필 장식 등을 검토할 수 있으나 구체적인 보상 종류, 자산, 순위 구간, 영구/기간제 여부는 이번 지시만으로 확정하지 않는다.
- 기존 참가 자격, 유효 기록 판정, 부정행위 제외, 중복 수령 방지와 서버 권위 정산은 유지한다. 모든 계정에 무조건 지급하는 정책이 아니다.
- 순위 산정/협동 승계/솔로·협동 중복 방지 규칙은 유지하되, 모드 간 ‘더 좋은 보상’을 재화량 차이로 비교하지 않는다. 공통 재화는 이벤트당 한 번, 외형 보상은 별도로 확정한 등급 규칙으로 처리한다.
- 별도 점수 달성/참가 보상은 순위 보상과 구분한다. 이 지시만으로 기존 점수 기준이나 참가 조건을 폐지하거나 변경하지 않는다.

<a id="source-e146be10f59741ea-구현검수-기준"></a>
#### 구현·검수 기준

- 기존 순위별 성장 재화 테이블과 UI 차등 표시를 찾아 현행 지급 설정에서 제외한다. 동일 자격의 1위/중간/최하위 재화 번들이 동일한지 검증한다.
- 확정되지 않은 통일 지급량을 임의로 최고/최저/평균 금액으로 선택하지 않는다. 지급량 확정 전에는 새 운영 설정 배포와 실제 지급을 하지 않는다.
- 기존 정산 이력·영수증은 보존한다. 과거 지급의 회수/소급 재정산은 이번 지시에 포함되지 않는다.
- 현재 변경은 기획 확정이며 모든 코드 수정·실제 지급이 완료됐다는 뜻은 아니다. 소스/설정 참조와 미결정 사항을 담당별로 기록한다.

기존 확인 대상: WorkspaceRepo/docs/design/world-raid-rules.md의 Fixed Ranking Reward Test Configuration. 이 문서의 순위별 재화 차등 예시는 폐기한다.

<!-- source-body:source-e146be10f59741ea:end -->

2026-09-22 월드WB4: [44 입장·정산·보상·화면](44_COMBAT_DETAIL_CONTRACTS.md#world-boss-event-reward-flow-v04) 작성. WB3첫시험승인. 신규안:주간공통500/30/15·명예5구간/교차모드1개·서버자동지급/원장·마감/정정·화면흐름,승인대기. 기존점수단계유지·실측전. 다음WB5 최종기획대조/완료범위판정,수장정사·자산/실제구현미확정보존.

## 주간 코어 공급 신규안 연결 — 2026-09-28
[35 경제v04](35_CURRENT_ECONOMY_ANNEX.md#permanent-repeat-core-economy-v04) 반복5종산식및주간일반/스토리개인승리3회→코어2개제안。승인대기,기존W1~3/월드/패스보상변경없음。월요일04시·원기간미수령권리복구를명세했으며실카탈로그/계정지급미실행。

<a id="weekly-core-mail-scope-review-v01"></a>
## 주간 코어와 일반 임무 우편 만료의 적용 경계 — 2026-09-28
[35 주간 코어 상세안](35_CURRENT_ECONOMY_ANNEX.md#weekly-core-unclaimed-recovery-v01) 참조. 최신 반복 경제 v04의 3승·2코어/원기간 권리 보존은 승인 기준이다. 코어만 우편 14일 만료 후 기존 임무의 지난 보상에서 수령하는 예외는 이번 신규 제안으로 승인 대기다. 기존 D2/D3/W1/W2/W3의 14일 미수령 소멸, 당시 보상 고정, 발송/지급 원자성은 유지한다. 역사 구간의 전 임무 자동지급/무기한 보존을 되살리지 않는다. 승인 전 실제 만료/지급 계약 변경 없음.

<a id="recruitment-cinematic-skip-proposal-20261003"></a>
## 모집 등장 연출 스킵 상세안 — 2026-10-03 / 세부 UI 승인 대기

### 요청과 기존 결정의 구분
Direction_4에서 사용자 명시 요청 “등장 애니메이션 스킵기능도 필요하겟다 기획세션에 넘겨” 전달. **모집 등장 연출 스킵 필요성은 사용자 요청**, 아래 위치·노출시점·10회 스킵 범위·재감상은 이번 추천안이며 아직 최종 승인 아님. 이번은 문서 기획이며 시안 제작/Unity·Server 구현 실행큐를 새로 발행하지 않는다.
기존 49의 S05 recruitment reads and sparse-result recovery: 결과는 서버의 원모집 사건/receipt에 귀속된다. GET /v1/gacha/history 및 /v1/gacha/batches/:batchId는 재추첨·소모·지급을 하지 않는다. 기존 가격·확률·천장·중복 전환·획득 권한을 변경하지 않는다. 04 전투 컷신의 hold/DAILY 정책은 모집에 자동 이식하지 않는다.

### 추천 표시·입력 흐름
| 구간 | 1회 모집 | 10회 모집 |
|---|---|---|
| 서버 확정 결과를 기다리는 중 | 확정 대기 상태. 스킵으로 성공/실패를 결정하지 않음 | 동일. 10회 결과를 개별 재추첨 요청으로 쪼개지 않음 |
| 결과 확인 후 공통 도입/카드 등장 | 첫 표시 프레임부터 우상단 `스킵` | 첫 표시 프레임부터 같은 위치 `전체 스킵` |
| 개별 요원 등장 | `스킵` → 1회 결과 | 우상단 `전체 스킵` → 10회 결과, 우하단 `다음` → 현재 등장만 종료하고 다음 결과 표시 |
| 마지막 개별 등장 종료 | 1회 결과 | `다음`/자연 종료 모두 10회 전체 결과 |
| 결과 화면 | 원확정 결과 및 해당 상세 열기 | 원확정 순서의 10개 전체 결과·원중복전환 내역, 건너뛴 결과도 누락 없이 표시 |

- 버튼은 시스템 안전영역 안에 두고 44×44 논리단위 이상의 터치 영역을 확보하는 안. 인물·이름/희귀도·핵심 결과와 겹치지 않도록 Direction에서 실제 크기로 확인한다. 화면 어디든 탭해서 스킵하는 방식이나 강제 최소 감상시간은 추가하지 않는다. 모집 실행 버튼의 이전 pointer-up이 스킵으로 전달되지 않도록 새 입력만 수용한다.
- 스킵 확인 팝업은 두지 않는 안. 새 인물/최고희귀도도 강제 감상을 요구하지 않는다. 전체 스킵은 현재 및 남은 도입·카드·개별 등장을 생략하며 이미 결정된 10개 결과를 보존한다. `다음`은 다음 개별 결과의 순서를 바꾸거나 카드를 다시 추첨하지 않는다.
- 결과 상세에서 이름/등급/획득·중복 전환 내역을 재확인한다. 원배치 결과가 서버 기록에 보존된 범위는 기존 모집 기록을 통해 다시 열 수 있다. `등장 다시 보기`는 자산이 있는 요원에 한해 감상 전용으로 제공하는 추천안: 원batch/index 결과를 참조하고 소모·지급·천장·NEW 판정을 다시 실행하지 않는다. 10회 전체 재감상은 이번 필수 범위에서 제외한다. 실제 API에 없는 NEW/전환정보는 추측으로 합성하지 않는다.

### 상태·소유권·오류 계약
- 모집 Transaction/Result Model이 원batch·결과목록을 소유, Presentation Controller는 연출 상태와 현재index만 소유, 하위 View는 SkipAll/Next/Ended 등의 의도만 콜백으로 전달한다. 서버 확정/소모/지급은 기존 유스케이스가 담당하고 AnimationEvent·Skip·결과창 진입에서 별도 경제 요청을 만들지 않는다.
- 제안 상태: 결과확인대기 → 연출중(공통/개별index) → 결과표시 → 닫힘. 미확정 응답/통신 유실은 기존 원요청키 재생·서버 기록 복구로 처리하고 새 모집 요청을 자동 발행하지 않는다. 실패 모집에는 가짜 결과/희귀도 연출을 보여주지 않는다.
- 각 연출 재생 세대와 index를 묶어 종료를 한 번 수락한다. 연타·자연종료·타이머·스킵 동시 입력은 Controller의 한 종료 경로로 합친다. 이미 닫힌 세대의 늦은 콜백은 무시한다. 전체 스킵이 수락되면 후속 개별 등장 예약은 취소하고 결과 전환을 한 번만 한다. View 바인딩 해제 후 게시0, 재바인딩 중복0.
- 스킵 때 자신이 획득한 연출·오디오·리소스·입력 제어만 정리한다. 다른 팝업/작업의 입력잠금까지 해제하지 않는다. 연출 자산 로딩 실패도 원결과가 확인됐다면 같은 종료 경로로 정적 결과를 표시한다. 결과 자체가 불완전하면 원기록 복구 상태를 표시하며 누락 결과를 발명하지 않는다.
- 앱 종료/재접속에서는 기존 미확인 모집 결과 복구 정책을 재사용하는 안. 복구된 확정 batch는 연출 강제 재생 없이 결과로 진입하고 원지급을 반복하지 않는다. 정확 미확인 batch 표시/보관 기간은 기존 계약을 확인하고 이번에 새로운 기간이나 영속 필드를 확정하지 않는다.

### 검증·후속 등록 조건
검증 대상: 1회 도입/중간/마지막 스킵, 10회 전체/현재만/마지막 다음, 신규·중복·희귀 결과, 연타·자연종료/Skip 경합, 취소/파괴·재바인딩, 로딩 실패, ACK유실/재접속, 전부 감상한 경우와 소모·지급·천장·결과 순서 동일. 시각 확인은 버튼 고정위치/안전영역/실제 작은 화면 가독성, 코드 검증은 원경제요청 단일화·원receipt 보존·한 번 전환을 분리한다. 현재 실행하지 않았음.
세부안 사용자 승인 후 Direction에 1회/10회 각 스킵·결과·재확인 후보 명세를 등록한다. 실제 Unity 연결은 전체 최종시안/독립자산 및 실제 모집 결과 계약의 기존 선행조건을 따르며, 비화면 로직은 승인된 명세와 실제 계약이 갖춰진 범위만 별도 등록한다. Server에 스킵전용 지급 API를 추가하지 않으며 실제 계약의 누락 필드가 확인된 경우에만 해당 원조회 보완을 배정한다.
참고 시안: QA/DirectionImpact-20260921/reina-recruitment-v03-lastrail/index.html 및 REVIEW.md. 현재8초 무음 reference-only/임시 브랜드엠블럼/실제10회·스킵·Live2D·게임지급 연결 미구현. 이 시안 길이를 게임 필수 시청시간으로 채택하지 않는다.
