## 최우선 사용자 정정 — BATTLE-NODE-RESTART-OVERRIDE-20261028
44#battle-node-restart-user-correction-20261028 승인. 아래모든기존작업의중단직전HP/위치/탄/공격phase/입력prefix새process이어가기요구를폐기하고 **동일선택전투node의시작상태로재입장**으로대체. 세부진행복원개발/시험확대중단. HP는해당node첫개시5인+열차값·만피/작전최초HP아님. 원장비/증강/이전노드확정진행유지·적/위치/탄/공격초기화. 원완료/패배확정은결과복귀·결과불명확은원키복구먼저. Server node시작snapshot/원자시도세대·revision, Unity host재생성/옛callback격리·실Player검수. 재출격비용/중복보상0·영속경제거래롤백0. 같은메모리단순백그라운드는pause/resume. 공유재현은동일시도검증으로유지,새시도에옛입력이어붙이기0. 기존코드/QA통삭제0. 실제full5행동/선로/포격/결과/API3회메모리계속. 새수용조건충족후기존통합등록조건대체.

# LASTRAIL UNITY 작업 큐

전투 카드·열차 조작부 연결 시 [스킨 제작 기준](../superpowers/plans_categories/51_UI_ART_DETAIL_CONTRACTS.md#combat-hud-skin-slots)을 따른다. 프레임과 손잡이 외형을 교체해도 입력 영역·손잡이 정지점·역할색과 상태 표시를 유지한다.

## 일반 협동 클리어·호스트 이탈 정정 — 2026-09-22 사용자 확정
[46 v03](../superpowers/plans_categories/46_COOP_DETAIL_CONTRACTS.md#ordinary-coop-survivor-clear-v03) 우선: 상대 최종ACK 없이 검증 결과 확정·원참가자의 원최종기록 대리 제출 허용. 함께 클리어 후 결과전달 중 단절은 양쪽 적격 권리 유지;전투중 이탈 뒤 잔류자 완주는 잔류자만 개인 클리어 보상. 원호스트 이탈에도 잔류자가 이어갈 수 있도록 서버 승인 hostEpoch 이관/완전checkpoint·원거래 복원 필요. v02 자동이관 제외/양측ACK필수/1인완주금지는 사용하지 않는다. 현재 S-12와 이후 U-11 범위에 포함하고 기존 진행기록/QA는 보존. 미확정 유예/재참여/역할 대행·승리 외 실적은 v03 후보 상태 확인.

## 모드별 책임 정정 — 2026-09-22 사용자 확정 / 기존 작업 문구보다 우선
[46 모드별 기준](../superpowers/plans_categories/46_COOP_DETAIL_CONTRACTS.md#authority-split-host-coop-v01): 일반 솔로=기존 Unity 로컬, 일반 협동=단일 Unity 호스트 전투, 월드보스 솔로·협동=서버 전투. 공통 카탈로그/START·선택/거래·진행/결과 검증은 서버다. 아래 기존 S-01/S-02 및 후속의 ‘권위 피해/HP 저장’은 일반 전투까지 매 타격 서버 계산으로 확장하지 않는다. 기존 구현/QA는 보존하고 모드별 적용 범위를 분리한다. S-03/S-04의 월드 범위는 유지. 호스트 복구·결과 검증의 미결 정책은46을 확인하고 추측 구현하지 않는다. 이 정정은 기존 담당 진행기록/완료 여부를 바꾸지 않는다.

2026-09-22 사용자 요청으로 만든 담당별 실행 큐. 신규 설계 원본이나 완료 이력 보관소가 아니다. [공통 운영 규칙](INTEGRATION_WAITLIST.md#role-work-queues-20260922), 프로젝트 AGENTS.md를 먼저 따른다. 최신 승인 기획만 신규 작업에 적용하며 구 정책은 신규 적용 대상에서 폐기한다. 진행 중 run의 START 스냅샷과 역사/검증 자료는 보존한다.

착수 가능한 항목만 이 큐에 둔다. 선행 미완료 후속은 기존 WAIT의 후속 등록 정의에 보관하며 실행 큐에 미리 추가하지 않는다. 착수 시 실제 파일·버전과 진행 상태를 기록한다. 완료 담당은 기존 인계/QA에 큐 ID·계약 버전·검증 근거를 보존하고, WAIT의 자기 ID 관련 후속 조건을 확인한다. 모든 선행이 완료된 후속을 대상 큐에 중복 없이 추가·확인한 뒤 자기 완료 블록을 삭제한다. 후속 등록 실패 시 `구현 완료·후속 등록 필요`로 남겨 재시도한다. 상세 절차는 [완료 시 후속 큐 등록](INTEGRATION_WAITLIST.md#completion-driven-queues-20260922)을 따른다. 타 큐에는 승인된 후속 항목 추가만 허용하며 기존 항목 수정/삭제는 담당 소유다. 빈 큐는 종료하며 자동 메시지·폴링하지 않는다.

정책 원본: [44 전투](../superpowers/plans_categories/44_COMBAT_DETAIL_CONTRACTS.md), [09 지휘실](../superpowers/plans_categories/09_COMMANDER_PROGRESSION.md), [13 장비](../superpowers/plans_categories/13_EQUIPMENT_ENCHANTMENTS.md), [14 경로](../superpowers/plans_categories/14_MAP_EVENTS_DROPS.md), [31 증강](../superpowers/plans_categories/31_AUGMENT_EFFECT_REFERENCE.md), [32 서사](../superpowers/plans_categories/32_NARRATIVE_REFERENCE.md), [45 정비](../superpowers/plans_categories/45_MAP_STATION_DETAIL_CONTRACTS.md). 각 문서의 최신 승인 절을 사용한다. [통합 대조 I01~09](../superpowers/plans_categories/34_COMBAT_POLICY_COMPANION.md#integration-conflict-audit-20260922-v01)는 발견 근거이며 런타임 전수검증 결과가 아니다.

현재 실행 항목: U-CORE-01. 2026-10-01 사용자 지시에 따라 화면 연결과 분리한 핵심 로직 구현을 우선한다. 아래 최신 실행 블록을 따른다.

### Plan 정책 승인 갱신 — 일반 협동 v04 / 2026-09-22
[46 v04](../superpowers/plans_categories/46_COOP_DETAIL_CONTRACTS.md#ordinary-coop-rejoin-controls-v04)는 사용자승인으로 확정.60초복구/명시1인전환·자동전환없음,재참여제한,원편성유지/기존솔로양역할조작,공용선택·개인권한/광고·개인별실적을 기존 S-12/U-11 범위에서 소비한다. 담당 진행기록/완료판정은 유지한다. 새 [v05](../superpowers/plans_categories/46_COOP_DETAIL_CONTRACTS.md#ordinary-coop-retention-validation-v05)의 보관/검토/통신시험안은 승인대기이며 v04승인으로 확대하지 않는다.
### Plan 정책 변경 — 작전 지도 전체 공개 / 2026-09-22
[14 전체공개v02](../superpowers/plans_categories/14_MAP_EVENTS_DROPS.md#operation-map-full-reveal-v02) 사용자확정. 기존 S-02/U-02 관련범위:START원그래프와모든노드이벤트배정고정·전체경로/아이콘공개,다음목적지만사건공개/먼사건숨김폐기. 먼노드열람과현재직접edge이동확정을분리,협동이관/재접속재추첨0·구run원배정보존. 실제지원/API/클라이언트수정은담당근거로검증. Direction D-11시안수정은별도등록됐으며기존진행기록/완료판정변경없음.
### Plan 포탑 보유정책 승인 — 2026-09-23
[04 v02](../superpowers/plans_categories/04_COMBAT_RUN.md#turret-ownership-loadout-v02) 확정:종류1회해금으로실제포격객차슬롯내동종반복,출발전설치/교체/해제무료·작전중종류교체불가. 원계정자산/종류레벨과전투개체독립,자동두기장착없음. 기존관련S-02/S-06/U-02/U-05/D-10범위에서소비하며담당진행기록/완료여부는유지. 다음3종스킬/해금v03은신규승인대기이므로미술/카탈로그발행에임의사용하지않는다。
### Plan 포탑3종 첫시험 기준 승인 — 2026-09-23
[04 v03](../superpowers/plans_categories/04_COMBAT_RUN.md#turret-three-kits-v03) 직사/속사/곡사스킬·기본값/해금안사용자승인. 기존포탑소유/성장/장착관련작업에서실ID/지원·모드별원천/구run경계를맞춘다. [44 조합검증](../superpowers/plans_categories/44_COMBAT_DETAIL_CONTRACTS.md#turret-pair-balance-validation-v01)의동종/혼합투자차이·기본/액티브점유·파괴복원사례를소비,실제미구현부분을문서승인으로완료처리하지않는다. 기존담당진행기록보존。
## 최종 승인 기획 후속 경로 — 2026-09-29 / 실행 항목 선등록 아님
[WAIT 후속표](INTEGRATION_WAITLIST.md#approved-plan-queue-sync-20260929) 참조. 기존U-01/U-02의전투/장비/경로/지휘실원범위유지. 신규승인변경의 U-EQ-01@equipment-supply-v18은 S-02해당계약/QA AND D-EQ-01필요자산완료후, U-WB-01@world-boss-binding-v2는 S-WB-01 AND D-WB-01완료후 최종선행담당이등록한다. 기존부모와같은파일/기능이면한담당이묶어구현하고각수용조건만별도추적,두구현생성금지. 지금fixture/가짜계약으로착수하지않음.

<a id="unity-core-logic-first-20261001"></a>
## U-CORE-01@approved-core-logic-v1 / 착수 가능 — 핵심 로직 우선

**2026-10-02 B 이행 우선:** 44#combat-b-mainline-approved-20261002 및 WAIT#unity-b-migration-foundation-20261002 적용. 앞의 A 객차장착포탑 제작/성장/독립실행 잔여는 신규제품에서 폐기·역사/원run 호환검증으로만보존. Eve센트리/적포대삭제0, 투자/장비자동이전·환불0. 新B에는외부전 fixedcarriage↔operator·승인15인범위·별도내부전 profile을사용; 기존A producer를新B로가짜통과시키지않음. B 도메인LOCAL43/43은 actualSTART/전체전투완료가아님. 실제ServerB 계약필요부분만WAIT, 공통HP/계산·통신경계는이어감. U-WB-SOCKET-LOCAL-01 공통통신큐 유지, B pattern suitability 별도. 상세미결/화면AND는원WAIT에보존.
Unity_3 Sera 단위·63실패분류 완료 / 2026-10-01: WAIT#unity-c3-sera-consumer-complete-20261001·QA/CoreLogic-20261001/SeraConsumer/verification.json. Green03 58/58·Regression04 159/159, 실제원input→nativeHP·예약/취소/down/복원 receipt대조 완료. 전체START/atomicDB·협동cursor/hostEpoch·일반actor/다른효과·화면은미완료,부모유지·후속등록0·MD인계. 제공된 한효과의 entity/stat/event/queue 입력은더이상전면missing아님.
C3 정의소비완료 / 2026-10-01 Unity_3: WAIT#unity-c3-definition-intake-complete-20261001·QA/CoreLogic-20261001/C3DefinitionIntake/verification.json. 48/48·신규31 PASS, 전체nographics2489중2386PASS/63FAIL/40SKIP 원문보존·전체PASS아님. own C3소블록만완료정리,부모유지·Server owned-profile/effect/START 실제입력후다음소비·화면등록0·총괄reply NOT_SENT_APPROVAL_REQUIRED.

진행 기록 / 2026-10-01 Unity_3: 기존 HP58/58·NumericDefense58/58(123vectors)·ActionTiming60/60·StatResolver71/71·EquipmentOc0176/76 근거보존. 추가ProducerSupportBoundary/verification.json Run01 59/59 PASS(신규11+기존48)·8핀: 실제CE03미지원ON/새DEF/미발행version/hash/body거부·원입력불변·원model저장복원진행동일/현재정의로원checkpoint재해석거부. 제품지원검사완화0·신producer지원완료아님. 인계·WAIT#unity-core-producer-boundary-20261001 저장확인. 전체UCORE 미완료. 다음: 실제지원producer/원효과event·SET02/03수명 및 작전로컬원거래 해당계약 인수. 독립로직 가능부분은 원정의/원입력 준비후 진행; 실카탈로그/지원handler·S01/S02거래·S12/SWB 대기유지. 완료삭제/화면후속등록 없음.

2026-10-01 사용자 직접 지시: 핵심 로직 구현을 Unity 큐에 추가하고, 화면 연결은 화면 시안이 모두 나온 뒤 별도로 등록한다. **로컬 도메인 로직은 Direction 시안을 기다리지 않고 진행한다.** 이 지시는 과거 UI/자산 선행을 포함한 묶음 작업에서 로직 부분을 분리한다. 서버 미완료 계약을 임의 확정할 권한은 아니다.

담당/경로: Unity / D:/Project/LASTRAIL/unity-project. 착수 시 실제 브랜치·수정 파일·기존 구현 및 지원 범위를 원 Unity 인계에 기록한다. U-01/U-02/U-08 등 과거 범위의 완료 코드와 유효 검증을 먼저 대조하고 최신 승인 변경분·누락만 한 구현에 반영한다. 같은 기능의 새 전투 엔진/별도 상태 저장소를 만들지 않는다.

### 바로 구현할 범위와 순서
1. **상태·객체 책임:** B 열차/객차/요원/적/보스·명시 소환체의 정의와 전투 인스턴스, 고정 배치와 대상별 독립 HP·타이머·상태, 생성/파손/다운/복귀/종료 전이와 소유권. 기존 모델을 확장하고 UI/통신과 분리한다.
2. **스탯·전투 계산:** 최신 공격력/체력/방어력 계수, 장비 합연산과 승인된 서로 다른 효과군의 곱연산, 방어·피해감소·보호막·실 HP 적용 순서, 실치유/초과치유, 최대 HP 변경 기준쌍. 무료 완충·중복 피해·파생 재귀 방지, 승인된 가속합과 상한 제거 유지.
3. **행동·효과:** 기본공격/일반스킬/궁극기, AUTO의 기존 의미, 대상 적격/소멸·취소, 행동 점유·쿨타임/충전, 장비 세트·인챈트·증강·지휘실 효과를 실제 지원 정의에 매핑. 미지원 효과를 무효 성공으로 숨기지 말고 항목별 인수 상태를 남긴다.
4. **작전 로컬 상태:** 기존 확정 START/명령 결과를 입력으로 쓰는 편성·장비/승급/보관함·지도 진행·종료 상태 전이, 버리기·상인 회복의 로컬 적용, 로컬 저장/복구의 상태 완전성. 거래 승인·재화 차감·보상 지급을 Unity가 독자 판정하지 않는다. 확정 입력이 없는 새 서버 연동 부분은 아래 대기로 분리한다.

### 원본과 검증
- [44 객체·전투 상세](../superpowers/plans_categories/44_COMBAT_DETAIL_CONTRACTS.md#combat-object-architecture-v1-20260930)의 최신 승인 A/B/C/D/E/F 및 H 수용 기준. G의 화면 구현은 이번 범위에서 제외하고 로직이 제공할 상태/명령 경계만 재사용한다.
- [13 장비·최대 HP](../superpowers/plans_categories/13_EQUIPMENT_ENCHANTMENTS.md#max-hp-current-health-rebase-approved-20260930), 09 지휘실, 14 지도/드랍, 21 요원, 31 증강, 45 정비, 46 협동의 해당 최신 승인 절을 사용한다. 후보/역사 수치를 신규 적용하지 않는다.
- [34 소스 대조](../superpowers/plans_categories/34_COMBAT_POLICY_COMPANION.md#vitals-boss-source-audit-20260930)의 VS/AR와 실제 변경을 대응시킨다. UI 없이 실행 가능한 EditMode/도메인 테스트 등으로 정상·경계·중복·취소·복구를 검증한다. 검산 예시만 복제하지 말고 실제 상태 변화/원결과를 확인한다.
- 단절/재실행 전후 상태 일치, 최대 HP 교체 왕복/실피해 후 기준 갱신, 같은 원사건 1회 적용, B 대상별 독립 HP 및 보존 A 원상태 일치, 열차/요원 회복 대상 분리, 지원 효과 실발동을 변경 범위에 맞춰 확인한다. 테스트 fixture는 검증용으로만 사용하며 런타임 API/카탈로그 대체물로 출하하지 않는다.

### 제외·선행 대기
화면/버튼/HUD·팝업·레이아웃·리소스·씬 표시 연결과 시안 재제작은 이번 큐에 포함하지 않는다. 독립 명령/상태 통지는 준비할 수 있으나 기존 화면에 새로 바인딩하지 않는다. 실제 플레이 화면 연결 완료도 주장하지 않는다.

새 서버 카탈로그/START·선택·거래·보상 계약은 S-01/S-02 해당 인수 후, 협동 full checkpoint/hostEpoch·확정거래 복원은 S-12 후, 월드보스 서버 판정/점수 소비는 S-WB-01 해당 인수 후 등록한다. 일반 솔로 로컬·일반 협동 호스트·월드 서버 권위 구분 유지. 미완료 계약/geometry/ID를 발명하지 않고 필요한 구체 계약만 WAIT에 남기며 독립 로직은 계속 진행한다.

### 완료·후속 처리
구현 파일/버전, 지원 효과·미지원 목록, 실행 검증 결과 및 잔여 서버 선행을 기존 Unity 인계/QA에 남긴다. 로직 완료와 통합/플레이 완료를 분리한다. 필요한 선행 대기를 WAIT에 인계하고 저장 확인 후 이 실행 블록을 삭제한다. **화면 연결 큐는 자동 발행하지 않는다.** 사용자 기준인 화면 시안 완료가 확인된 뒤 필요한 독립 리소스·서버 계약 조건까지 갖춘 범위로 별도 등록한다. 자세한 후속 경계는 WAIT#unity-core-logic-first-20261001을 따른다.

<a id="server-api-contract-intake-20261001"></a>
## 기존 U-API-01 서버 계약 인수 — 2026-10-01 Server

활성 U-API-01@unity-server-verification-v1 확인, 중복후속/화면연결등록0. [실제API·버전·오류·환경·87개QA](LAST_RAIL_SERVER_2-D-20260913.md#homepage-dependency-reconciliation-20261001).
Game Bearer POST /v1/support/tickets·GET /receipt·본인목록/상세 private-support-list-v1/private-support-ticket-v1 및 공지/공시/release GET의 DEV정확tester/version 경계 인수. web/Admin API 게임이식금지.
재현 server/tests/integration/support-private-read-routes.test.ts, game-api-support-tickets-smoke.test.ts, public-notices.test.ts, probability-disclosures.test.ts, public-release-routes.test.ts; tests/helpers/departure-fixture.ts SQL.js모든migration/격리a,b/정확tester123. 14파일87/87·game/admin타입PASS는 Hono app.request이며 실제socket/Unity·실D1근거 아님.
실제listening testURL·격리계정/토큰발급·적용migration/정확tester 근거 미제공(BLOCKED). 설정DEV URL을 현재지원/도달로 승격하지 않음. 독립실행기/계약오류 매핑 착수, Server환경제공후 기존UAPI키로 실제왕복/Editor·Player·NOT_RUN 구분. 화면연결 시안완료후 별도등록 유지.

## U-CORE 실제 producer 연결 인수 — 2026-10-01 Plan
[34 실제 차단](../superpowers/plans_categories/34_COMBAT_POLICY_COMPANION.md#solo-start-runtime-gate-20261001). ProducerCombatAdapter.cs는 현재 equipment/research/extraPressure OFF·carriageDamageEvents UNSUPPORTED만 수용. 기존 최대HP/DEF/공속 단위 구현을 완료된 새실행 지원으로 오인하지 말고 실제 신규 producer/version·승인효과·상태 적용과 연결한다. S-01/S-02 해당 실제 계약 인수 전 기존 엄격 검사 완화/테스트프로필 출하 금지. 독립 계산·모델 작업은 계속 가능. 신규START 지원 계약과 Unity 소비의 정의/hash 일치·미지원 거절·원START 재생 검증을 기존 U-CORE에 포함한다. 새U항목/화면 연결 추가 없음.

<a id="uapi01-owner-grant-evidence-intake-20261001"></a>
## 기존 U-API-01@unity-server-verification-v1 권한 회수 근거 보완 — 2026-10-01 Server

[OWNER 권한 관리 구현·API·환경·G01~16 근거](LAST_RAIL_SERVER_2-D-20260913.md#s15-owner-grant-implemented-20261001). schema0096/support-grant-management-v1 관리 API는 Admin 전용이며 GAME/공개 웹으로 노출하지 않는다. 기존 GAME 본인 문의 POST/list/detail/receipt 계약·권한 경계는 유지, 회수 전에 확정된 공개답변 보존·회수 이후 운영 답변 저장 차단을 실제 Hono/DB 경합으로 검증했다. 과거 문의/작전 원snapshot 변경0.

기존 U-API-01 검증 근거에 tests/integration/support-grant-management.test.ts G09·admin-support-processing-routes.test.ts를 추가 참조. 관리 단위19/19·전체230파일1991/1991 PASS는 로컬 Hono+SQL.js이고 실제 socket/Unity/원격D1 왕복이 아니다. 원격 DEV0047·기존 OWNER 실제 인증/최초 점검 초기화 및0096 배포 후 지원환경 제공 잔여. 준비된 기존 GAME 계약의 독립 검증은 계속 가능. 새U항목/중복 ID/화면 연결 등록0, 시안 완료 후 별도 화면 등록 유지.
## 점검 로그인 화면 연결 예약 — 2026-10-01 / 실행 항목 아님
사용자 요청 12#game-web-login-maintenance-20261001. [WAIT](INTEGRATION_WAITLIST.md#maintenance-login-followups-20261001)의 U-MAINT-LOGIN-01 조건을 충족하면 실제 연결 작업을 등록한다. 지금은 Server 차단계약·Direction 시안 제작 단계이며 기존 전체 게임 시안 완료 후 연결 방침 유지. 로컬bootstrap 점검 안내/상태갱신을 기존 진입 owner에 연결하고 U-CORE에 화면작업을 섞지 않는다.

## 연계 총괄 인수 — 2026-10-01 / 기존 U-CORE와 API 후속
WAIT#coordination-unblock-20261001 C2/C3 적용. Server 실제원정의 기반 schema/버전·효과입력/상태를 받으면 전체서버완료를 기다리지 않고 기존adapter/handler 소비 단위로 인수한다. 미지원검사 완화/구fixture출하 금지,실제실행근거를 Server START연결에 제공. 원계약 미제공 상태에서는 필요한 정확필드/효과/버전만 원인계에 명시. U-API-01 재등록0; 실제socket환경 제공 때 U-API-02의 준비된 API묶음만 등록. 도구/순수계산을 반복구현하지 않으며 게임화면연결은 시안완료 조건 유지.

## 총괄 최신 범위 — 화면 연결 전면 보류 / 2026-10-01
사용자 최신지시 WAIT#coordination-no-screen-20261001 우선. 모든 실제화면/View/bootstrap 연결은 최종시안 전체 완료 후. 바로앞 총괄인수의 초기화UI/기존승인페이지 선연결 지시는 보류한다. Model/Service·화면없는명령조정/API adapter·서버producer/권한/저장·실제독립HTTP검증은 계속한다. 원격초기화는 기존동일OWNER인증을 유지한 안전한지원경로가 확보된때만,없으면 그실행만대기하고 로컬연계는진행. 시안은사용자가직접진행,완료자료/기존진척삭제0.

<a id="completion-reply-dispatch-20261001"></a>
## 사용자 승인: 작업 완료 답장 — 2026-10-01
Unity_3 C3 인수 입력 특정 / 2026-10-01: 기존 완료QA 유지·새코드/무변경재검사0. WAIT#unity-c3-data-first-intake-20261001 및기존Unity인계에 최소지원1effect의 실제profile/version/hash·원정의/구dispatch·owner/3slot/tag/revision·원스탯/계산주체·SET02공격발행vs적중/miss/4초clock·SET03native사용/refresh·원source/snapshot/저장version 입력을 특정. Server데이터계약우선→Unity검증→START판정; 전체서버/원격DEV/미술을도메인계약선행으로요구하지않음. 현재새확정handler입력없음·UCORE부모유지, UAPI01종료·UAPI02총괄배정대기. 총괄회신은사용자승인/실제ID확인후수행하며이문서기록만전달성공으로간주하지않음.


이번 연계 작업 완료 시 기존 인계/QA에 작업 ID·변경/계약 버전·검증 범위와 근거·후속 해제 항목·남은 의존성을 기록하고, 요청을 보낸 기획 총괄 채팅에 완료 메시지를 답장한다. 총괄이 확인 후 다음 작업을 지시한다. 사용자 명시 승인으로 이번 완료 답장은 과거 자동 보고 금지의 예외다. 수신 확인만의 답장·반복 폴링은 불필요하다. 전체 화면 연결은 최종 시안 전체 완료 이후, 시안 진행은 사용자 담당이다. 완료 실행 항목만 근거 보존 후 정리하며 부모 미완료 작업은 유지한다.

원본: INTEGRATION_WAITLIST.md#completion-reply-dispatch-20261001. 2026-10-01 재시작 후 앱 도구 복구 및 현행 담당 4세션 메시지 전송 성공 확인. 완료 답장 대상: LAST RAIL_Plan_6 / 01a0c3bf-96d7-7f73-9deb-756181adad80 / local. 전송 성공과 구현 완료는 별개이며 아래 최신 총괄 배정을 따른다.

Unity_3 이번 총괄회신 전달 결과 / 2026-10-01: 앱도구는응답했으나cross-thread답장자동승인검토2회거부·전송성공0. 실제타채팅userMessage확인도현재채팅의trusted승인이아니라는사유. 우회/추가재시도0·현재Unity채팅사용자명시승인필요. 원인과근거는기존Unity인계에저장. 문서입력특정완료/코드·QA·부모상태불변.



## C3 큐 총괄 인수·Unity 실제 소비로 전환 — 2026-10-01
Server c3-sera-queue-final3 57/57 및 변경4파일hash 확인. 단일Sera 예약 자동dispatch/복원 경계만 인수, 원자DB/전체actor/실Unity 미완료.
Unity 상태도구 및 인계 확인: UAPI02 82/82·정의48/48 완료 보고, 전체Full01 63fail/40skip 미해소. 다음 U-CORE 지시 직접전달: 보존63실패를 원인군/owner/C3영향/이전근거로 분류→자기변경 회귀/핵심차단 보완→Sera 원input/entity/event/receipt/nativeHP/queue를 기존Unity owner/adapter/fixedstep에 비화면 실제연결. 화면/리소스는근거만WAIT, 무변경전체재검사/기존baseline추정 금지. 실제Due/Sequence·clock/debt/remainder 재사용, 중복/취소/down/복원 및 서버관측 결과대조. 파싱만으로 실행완료 주장금지. 원본 Server 인계#c3-sera-queue-20261001 및 관측QA 사용. 실제부족필드만 분리.
Server에는 이 Sera 계약 버전을 유지하고 Unity 피드백까지 C3 추가확장대기를 직접전달. 새버전 반복/확인답장 불필요. 전체START/화면/remote 미해제. 기존부모 유지.
Unity 회신은 해당세션 자동승인검토가 직접사용자승인부족으로 거부했다는 기존기록 확인. 우회/반복재시도 요청하지 않으며 MD에 결과보존 후 총괄이 필요한 경계에서 직접 조회. 다른세션의 전송성공을 Unity 회신성공으로 주장하지 않음.


<a id='world-boss-geometry-contract-intake-20261002'></a>
기존 U-WB-GEO-01 최소계약 인수: [Server 원본 geometry v1](../api/world-boss-rail-geometry-v1.md), world-boss.rail-geometry.v1.20261002. 승인 mainline LEFT/RIGHTdetour·ENTRY=LEFT alias, 경로arc/차량length-gap-width/선형가감속sample·2000분모거리잔여/actual-target-forcedSlowdown·HEAD_SPLIT/TAIL_SPLIT/TAIL_MERGE 계약을고정. 원v3/18fixture 변경0. Server는같은계약의판정/결과결속 구현진행, Unity는해당순수모델 및측정진행(새중복큐0). 시험profile/수치운영승격0·View/실HTTP/DB/health 미연결경계 유지. 상호완료AND뒤소비연결후속등록.







<a id="u-b-refit-http-09-ready-20261003"></a>



<a id="u-b-local-flow-10-resume-20261003"></a>

<a id="u-b-rank-flow-12-ready-20261003"></a>



<a id="u-b-active-consume-14-evidence-number-correction-20261003"></a>
U-B-ACTIVE-CONSUME-14 본Server등록블록의 원v1 source 검증 숫자 문구 정정: **기존부분52PASS/TCP11**, 새탄도공급**60PASS(新8+原52)/TCP11**, 原63핀·U13原11핀不變이다. 잘못된 `53?` 표기는오타이며실근거아님. 정확QA manifest/final2 및인계의숫자만사용한다. 타담당원블록수정0.










<a id="sable-source-queue-correction-20261004"></a>
## S27/U29 Sable 승인 원문 정정 — 2026-10-04
서버 대조로 총괄 S27 큐의 ‘Sable 승인 DEF형/옛 HP형 복원 금지’가 승인 원문과 반대임을 확인하여 해당 지시를 철회·정정했다. 정책 변경이나 새 승인 아님. 원본은 44#def-scaling-reassignment-v10, #def-effect-mapping-status-v11, #def-shield-effect-mapping-v12 및 #combat-b-operator-role-approved-v05-20261002. Sable은 HP형 유지: 일반 시전자 최대HP18%, 자기 방벽 숙련+10% 1회,6초. 태세 일반25%/궁극20% 동일군 최강 하나. 본체 DEF282와 방벽 계수는 별개이며 DEF110% 비교 후보는 미채택. 구 HP30%/궁극 전체HP20% 설명이나 폐기된 공통 HP40% 상한을 복원하지 않는다. 최신 승인 HP형 정의 유지와 B 일반/궁극 실행 지원 완료는 구분한다. S27 기본 actor/owned/growth/요격 공급 및 U29 native 연결은 계속 진행하고 일반/궁극 미구현은 capability 미지원으로 유지한다. 신규 수치·실계정 지급·운영 START 승인 없음.
<a id="named-source-resume-20261004"></a>
## 실제 중단 확인 및 재개 — 2026-10-04
사용자 ‘작업멈췃는데 확인해봐’에 따른 총괄 확인. Server S27 완료 idle, Unity U29는 S27 문서 읽기 시작 뒤 contextCompaction 이후 ‘지침 전달만으로 개발 재개하지 않음’으로 종료. 도구 오류나 U29 완료가 아님. Unity는 기존 U29 그대로 새 exact named source strict 소비·Kyle/Sable native 기본/요격 결속을 재개한다. 중복 큐/새 장면 없음. S27 완료 AND U29 실제완료 후 동일seed no-boost 비교 후속을 등록·진행한다.
서버 병행 범위는 아래 S28: U29나 전체작전 완료를 원수치 producer 연결의 선행으로 삼지 않는다. null 필드를 추정값으로 채우지 않고 원 승인 효과·원 소유자에서 확인된 부분만 구현하며, 미승인 필드와 정확 의존성은 별도 유지한다.


<a id="u-b-safe-maintenance-runtime-source-20261005"></a>
## U-B-SAFE-MAINTENANCE-RUNTIME-SOURCE — 原runtime 정비 관측 / 기존 수정 안전경계 후
Server equipment.safe-maintenance.v1.20261005 내부 journal 구현 공급, QA SHA46f589705b04cabf356fb528cff578884e14afdfc505dddaebaf4d97b678f95c 총괄파일일치 확인. 원계약 Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/equipment-safe-maintenance-v1.md 전체의 실제미연결 경계 참조.
Unity 기존 BattleSession/SimulationClock/BDirectAttackBasicLoop/BEnemyAttackAdmissionOwner 및 실제 travel·node-event owner를 대조해 accepted 전이에서 원 combat/travel 정지·잔여 모든 hazard·안전event/hold·run/node/source revision을 불변 관측으로 생성하고 원복원/변경세대를 연결한다. clock/HP/맵 진실을 새로 만들지 않는다. 총알·예약공격 등 하나의카운터만0이라고 전체hazard종료로 처리하지 않는다. travel원천이 없으면 정지true로 채우지 않고 명시미지원. ENTER/EXIT가 사용자pause버튼이나View에서 직접발행되는구조 금지. 원승인 정비 시점 이외 허용0.
여기서 Unity관측은 서버 신뢰 증명이 아니다. 인증된클라이언트의 paused=true·hazards=0 주장을 SQL에 저장하는 것만으로 trusted source라 부르지 않는다. 기존 ordinarysolo UNITY_LOCAL/coop UNITY_HOST 권위 정책과 원검증기 경계를 보존하고, 정확 관측 구조/실지원/복원/로컬경합 검증을 기존 인계에 공급한다. Server 연결은 그 원관측 소비와 accepted transition 검증·samebatch fence가 모두 준비된 뒤 단일후속. 신규 network wire/공개route를 독단발명하지 않는다. 실제재개·노드이동·종료·hazard추가/종료와 원관측revision·중복·복원 검증, 종료수명과 늦은갱신거절. 기존 진행중인 JSON strict수정/검증부터 완료한후 착수. 새View/미술/운영START/실계정경제/배포0. 전체 S29 완료/실네트워크 증거로 과장0.
<a id="unity-b-safe-maintenance-observation-result-20261005"></a>
Unity 2026-10-05 담당 결과: 원 B accepted/full native LOCAL 관측과 saved rail/checkpoint 관측 producer18+기존driver3=21PASS, restore/duplicate/경합/종료수명 검증. [인계](LAST_RAIL_Unity_2-D-20260913.md#unity-b-safe-maintenance-observation-result-20261005)/QA SHAe61f3e7e9f778e5302db1940370de80864ef8040c8885602c015f26871603be5. 본큐는 원 run/node/map/safe-event accepted 운영 검증기+B 실제 travel+모든hazard/서버samebatch fence 미연결로 유지. LOCAL 자료를 server trusted evidence로 승격0. 원 관측 단위 완료와 전체 큐 완료 분리; 정확선행 원인계/WAIT 기록/미준비 타역할 실행큐 등록0.

<a id="u-b-travel-maintenance-owner-next-20261005"></a>
## B 실제 이동 원소유자 연결 — 정비 관측 다음 실행
사용자 중단확인재개. 기존 LOCAL관측21PASS를 반복하지 말고 원 B run에 actual travel owner가 없는 구현공백을 처리한다. 기존 승인 B 주행/선로/노드 전이 규칙과 기존 CombatRailJourney를 필요한절만 대조하여 실제 BFixedStep/session 소유경계로 이동거리/진행phase/정지·재개를 조합한다. 독립 clock/가짜 paused를 추가하지 않고 이미존재한 올바른 모델 재사용, A전투/포탑을 복원하지 않는다. 원run/node/commandseq/source revision·복원/수명·원자실패를 보존하고 B 실제주행→accepted 정지→거리불변→재개를 검증한다. 새속도/기믹/노드허용시점을 발명하지 말고 승인값 없는 항목만 구체보류한다. 전체hazard는 기존 모든원소유자에서 terminal/pending 의미를 확인해 원관측에 결속, 관측없는 종류는 unknown으로 거절. UI/새장면/미술 없이 수행한다.
서버는 s29-safe-transition-verifier-next-20261005에서 exact accepted전이 계약을 공급한다. 그것을 운영네트워크연결 선행으로 두되 B이동 모델의 독립구현까지 전체대기하지 않는다. 기존 LOCAL관측을 그대로 운영증명으로 전송하지 않는다. 양쪽준비뒤 원map/safe-event 검증·장비변경 같은batch 경합 통합후속 단일등록. 실제범위/원source/검증/미지원기록, 실START/계정경제/배포0.
<a id="u-b-travel-owner-partial-evidence-20261005"></a>
위 travel 다음 실행의 독립 내부모델/installed hazard 조합은 완료: [원인계](LAST_RAIL_Unity_2-D-20260913.md#unity-b-travel-owner-result-20261005) 15PASS/QA SHAd5a3a7d0771555b7673d6c8e16a1427f92a53ea50477d701fbd20fbeb03d1dc5. 잔여만 유지: 승인 운영 B 속도·가감속/선로-node 선택 source 및 Server exact accepted transition/map/safe-event verifier 공급 후 samebatch 경합 통합. 기존 완료내부 구현/15검증 반복0; 전체 정비 운영연결 완료아님/후속실행큐 등록0.

<a id="u-b-safe-transition-consumer-residual-20261005"></a>
diagnostic 계약 소비 완료근거는 원인계#unity-safe-transition-consumer-result-20261005 / SHA8508d4a953589f31f64b8377d5d186fa00bc875ae6bc62808c4098fcce24f60e. 실행완료 addendum삭제/부모미완료 유지. 서버GET/POST 미공급대기 해소; accepted 운영runtime source·safe-event/hold권위·allhazard capability/samebatch 경합·운영주행속도/선로노드입력만잔여. 완료consumer/11검증 반복0·선행AND미충족후속큐0.

<a id="b-route-maintenance-next-20261005"></a>
## 다음 실행: 실제 B 노드 전이와 정비 수명 결속
사용자 중단확인 재개. Unity 주행15PASS·진단API소비11PASS/실HTTP11 완료를 원인계에서 확인. 진단거절API를 반복 확장하는 것으로 실제 연결 완료를 대체하지 않는다. 리소스 천장7종은 active이며 별도재배정0.
Server 담당: 기존 OperationMapStore/BOperationProgressStore와 실제 선택/ARRIVE/COMPLETE 원 계약에서 B node identity/선택/이동시작/도착/노드완료 및 정비 hold의 실제 수락 흐름을 특정하고, 누락된 B adapter를 기존승인 전이에 맞춰 구현한다. 원 map/wave verifier 미설치 부분은 exact 지원범위·검증필요입력으로 분리, 정책을 바꿔 전투HP를 서버재연산하지 않는다. safe event의 기존 허용 이벤트 ID와 원종료/보상 hold를 대조하고 실제 권위 공급 가능 범위의 수명 producer를 구현. 새이벤트/허용시점/속도수치·기믹 추가0. 최종 source가 없는 경우 진단 true를 권한으로 변조하지 않음. 원코드 실행 경로·원API·입력/응답/미지원·구snapshot 보존을 Unity에 즉시 특정하고, 실제공급 계약으로 검증가능 부분을 진행한다.
Unity 담당: 원 서버의 기존 선택/이동/도착/완료 API·typed 계약을 먼저 확인하여 기존 BScoutGunnerNodeOwner/주행session과 비화면 Controller로 연결. 선로 조작과 지도목적지선택은 기존정책대로 구분, 선택원본/원revision/account/run/node/명령ID·노드교체 시 이전runtime수명/복원·실패원자성 보존. 기존 Battle/travel/hazard 모델 재사용. B 운영속도/가감속 미확정이면 초기부터 typed 외부설정필수·missing명시오류, 시험설정은 LOCAL로 명시; 수치가 없어도 검증된 node packet의 입력전달과 수명/실패/원소유자 연결은 구현한다. 서버 미설치 source를 로컬조작으로 통과시키지 않는다. 새 wire는 서버소유, 기존지원 API 소비와 미공급후속을 구분.
양측 현재모델·diagnostic 작업 재검사만 반복금지. 실제 원API가 없으면 계약 없는 Unity 네트워크구현은 대기하되 독립 Controller수명/원node-binding은 진행. 합동지원범위가 갖춰지면 같은run/전이원본으로 실제장비변경 가능여부까지 검증하고 미지원이면 정확 원인 남김. 실START/계정경제/배포/최종View/임의수치승인0. 최신수정단위/담당파일/검증결과·진짜 미결을 기존인계/WAIT 저장.
<a id="b-route-controller-independent-complete-20261005"></a>
b-route-maintenance-next Unity 독립가능 node/model/Controller·원publicdestination 소비는 완료: [원인계](LAST_RAIL_Unity_2-D-20260913.md#unity-b-route-controller-result-20261005) / QA SHAce3ca97594ee13c400cd857f153125b9a56e2bc604c9b4d52d3a243d8ddfc1e5 / original node/runtime16PASS+actualHTTP1PASS7건. 완료검증반복0. 이기존부모항목은 Server exact admitted generated-node·acceptedARRIVE/COMPLETE adapter/원safe-event-hold권위·allhazard samebatch 및 운영속도/가감속/physicaltrack입력 공급후연결만유지. 실제HTTPfixture와LOCALnode모델을같은원본운영통합으로과장0. 운영typed source missing명시거절/다른실행가능부품전체대기0. 신규단독큐/선행미완료후속0.


<a id="b-route-native-source-next-20261005"></a>
## 실제 HTTP 노드 → 전투 runtime 결속 다음 단위
사용자 중단확인·기존승인구현 재개. Unity #unity-b-route-consumer-result-20261005에서 실제HTTP run은 rawScout node-only/nativeRuntime미부착임을 확인. API성공을 전체전투연결로 주장하지 않는다. 리소스는별도active 유지.
Server: 기존 B 출발 pin/고정 manifest/원S22-v2 boarding·S23-v3 event/intercept 정의를 재사용해 해당 실제 선택node의 native factory가 요구하는 exact boarding/wave/source 묶음을 공급한다. 이름만신규 wrapper발명하지말고 Unity 원factory/strict intake에서 실제필수 schema/IDs/hash/origin을 읽고 기존generation계약으로 연결한다. 고정plan/seed/node/wave·원revision 보존, later-node를 current catalog에서 재추첨하지 않음. original pin에필요원본없는구run은미지원유지, 필요한신규version에서 출발당시원본을 고정·보존한다. 우선현재지원 ordinary Scout/Gunner1node 범위부터 exact typed sample/API 공급·실제SQL/원auth/replay/변조·node바꿔치기/CAS검증, 이후같은승인manifest상ordinary later-node 범위의결정성 보존. 엘리트/보스 미지원값발명0. 운영속도/accepted broker未연결은별도이며 이source생성 전체대기금지. 준비된 sample/contract를기존Unity큐동일절에인계. 새정지정책/공개START/실경제/배포0.
Unity 현재 잔여: v1 원 result POST/GET 및 opaque receipt 복구의 typed transport·Controller 연결은 완료(원인계 unity-b-result-report-transport-result-20261006,43독립+실제HTTP1PASS). 실제수신은 LOCAL 원출발편성 누락으로 progressAuthorizedfalse. 남은 입력은 일치하는 frozen departure formation/runtime input, Plan actual ARRIVE 기준·구간 원조건/owner, Server v2 intake 실제공급, reportOriginMapRevision→현currentMapRevision 원계보 및 원hold/safe-event permission·전체route통합이다. v2강등0·LOCALref를서버opaque로변조0; 완료부분 재착수 제외/부모AND 유지.
양측원소스단독소유/같은파일동시편집금지. 실제연결이구현됐으면다음원accepted event broker의최소지원경계를원runtime결과근거로특정한다. 별도신규공개게임기능/수치승인없이기존승인원범위만진행.

<a id="u-b-original-broker-input-20261006"></a>
## 기존 B accepted event 후속 입력 — 2026-10-06
기존총괄 #b-accepted-native-broker-next-20261006 후속입력이며 새중복ID 없음. Server #b-original-accepted-broker-result-20261006: internal bridge 구현·78PASS/type·lints0 완료, actualUnity 결과수용은원SERVER 검증기/발급정책미충족으로기본409유지.
원계약 D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/combat-b-original-accepted-broker-v1.md. 내부exactsample QA/CommanderS1-20260922/combat-b-original-broker-local-sample-20261006.json은 LOCALverifiedSQLsource only, 실제clientbody아님. 기존HTTP b-route-v1 DTO·GAMEauth·commandId/maprevision/evidenceRef는그대로이며 acceptedref는원검증owner발급전클라이언트가만들지않음. 새checkpoint/reportupload/witness발급 endpoint는현재없으므로 계약없는network구현0.
독립원전송준비: originalEventId resume-step-362, checkpointHash sha256:0b3a391f51e2509339ee7df6010b3285693bc91b78ec97b0c17fad1b80432dfd, commandRevision507/nativeSequence1063와reportorigin SQLmaprev2를 각각보존; 원source/context/account/run/node/원nativebinding/frozennodeSourceHash·전체원journal/installedhazard registry 및원acceptedprefix내실제event refs·유실/Unbind/노드교체수명분리. LOCALVICTORY/TERMINALstop을acceptedARRIVE로바꾸지않음. verificationflags·bool·QA파일서명/digest를검증된원전투사실로송신0.
실원owner제공후broker는원ARRIVE/wave/COMPLETE/hold/BETWEEN_NODES actualStore source/assertions로연결가능. SAFE_EVENT는독립원permission필수. operatingmotion/acceptedtimingnull은별도유지. 원2wave실행/복원QA는그대로재사용, 재실행0. 실제운영검증기·전송/발급계약결정은총괄필수입력으로Plan큐기록; 준비됐다고전체큐삭제/합동AND선등록0.
QA manifest SHA d306b25de1121511db9ae88970e5c7c9b456cfd2cc8d513d47ea98e82e9fcdab. 타담당원소스/기존큐블록편집삭제0.


<a id="unity-b-original-arrival-contract-pending-20261006"></a>
원 wave/terminal producer 실행단위 완료·인계/QA/WAIT 저장확인(41PASS, unity-b-original-event-producer-result-20261006). 재실행 제외. 남은 선행은 Plan의 도착기준 및 선택 pendingEdge/node 원구간거리/전이조건 source-owner 계약이다. 14_MAP_EVENTS_DROPS.md404–442 계약대기; 실제주행 owner ARRIVE 없음. 원조건 없이 새거리/속도/시간/VICTORY 도착변환0. 이 절은 도착 선행대기 기록이며 계약공급 전 실행큐 아님. 기존 부모/Server 입력블록 유지; SERVER result consistency/ref/transport·hold permission 전체 통합은 부모 AND 준비후 진행.


<a id="u-b-result-consistency-input-20261006"></a>
### 기존 B accepted 통합 후속의 서버 입력 공급 — 2026-10-06
서버 v1 report/receipt 및 실제 owner 공급 완료, 전체통합은부분완료. 원근거 Server 인계/WAIT#b-result-consistency-owner-result-20261006, API D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/combat-b-result-report-v1.md. 96PASS·원GAME auth 실제TCP10PASS·scopedtype/lint0, 실제 unchanged Unity v1 nodeSession 전달·복구. 새v2원producer41PASS/f68c247d... 인계수신·재실행0; 본서버현재v1만지원, v2schema400. Server가준비된원v2sample intake/broker 대응을다음독립단위로진행하며 원journal을v1로강등/소급생성하지않는다.
확정 소비계약: POST /v1/runs/:runId/b-result-reports 및 GET /v1/runs/:runId/b-result-reports/:evidenceRef, 기존GAME Bearer/X-B-Result-Version combat.b-result-report.v1.20261006/no-store. POST strict version/requestId/reportOriginMapRevision/checkpointHash/nodeSession, Idempotency-Key=requestId, bytebudget1,800,000. 같은requestid/body멱등, changedbody409, GET opaque ref복구. 응답verificationLevel STRUCTURE_SOURCE_RESULT_CONSISTENCY/combatTruth NOT_PROVEN/fullNativeReplayPerformedfalse/arrivalAcceptedfalse/rewardsAuthorizedfalse/progressAuthorized/missingSources. 원출발편성미공급이면수신가능하나진행불가 ORIGINAL_DEPARTURE_FORMATION. 원 GAME schema/body400/인증401/ref404/budget413/원충돌409.
기존 b-route DTO/version변경0. progressAuthorized ref와각명령원currentMapRevision을 사용; reportOriginMapRevision2/commandRevision507/nativeSequence1063을 expectedMapRevision으로대체0. 원wave 사건정보와 최종no-op원ID를혼용0. GET immutable 원receipt 조회후종결replay보존. v2 originalEvents의 rawEvent.sequence와pointsequence·binding.originalCommand/eventRef는 별도이며 LOCALref를Server B_RESULT ref로조작0.
v1전송/reference복구는원계약기반독립구현가능; 전체부모 AND는미충족, 신규통합실행ID/최종View선등록0. 실제운영수용에는일치하는원departure formation/runtimeinput·독립ARRIVE 필요. Unity 현재대기 #unity-b-original-arrival-contract-pending-20261006의 도착기준/선택pendingEdge·node 구간거리또는전이조건/owner 결정은Plan에인계. fullnative replay는Server필수선행이아님. VICTORY/TERMINAL→ARRIVE전환0, 미정motion/acceptedtiming/hold/safeevent source발명0. 타담당기존블록 수정삭제0/운영START·실지급·배포0.

QA 원본 D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-result-report-validation-20261006.json SHA256 7205a49c1d7af0c97b1e2cbab3a827d66b8b9163a78fbe15eb0ed800f98e796d. final3/TCP10/safeLOCAL/full-final/related-recheck/departure-single 재검사 및source hash 포함.




<a id="u-b-result-v2-transport-input-20261006"></a>
### 기존 B 부모의 독립 v2 전송·receipt복구 착수 가능
등록키 **U-B-RESULT-TRANSPORT@combat.b-result-report.v2.20261006**. 선행: Unity원v2 producer41PASS/f68c247d... AND Serverconcretev2공급107PASS+delta11/TCP12/type/lint0 완료. 기존부모에독립전송범위만추가, 전체통합/최종View 선등록0. 원자료 Server/WAIT#b-result-v2-journal-server-result-20261006, API D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/combat-b-result-report-v2.md, QA 02cf629ff908a557fb37bd5831dcc48a79d943661a7a6652b841947794816556.
원 native v2.originalEvents를포함한whole nodeSession runtime/hash,원 reportOriginMapRevision·영속requestId를POST /v1/runs/:runId/b-result-reports에전송(GAME Bearer/X-B-Result-Version v2/Content-Type/Idempotency-Key=requestId/no-store). v2UTF8 4MB(원core1.8MB), 서버분할보존·원GET opaque ref복구; v1지원유지/강등·retrofill0. 같은key/body재시도만; 오류401/400/404/413/409 및원maprevision/source충돌을실패로유지. receipt verificationLevel STRUCTURE_SOURCE_RESULT_CONSISTENCY/combatTruthNOT_PROVEN/arrivalAcceptedfalse/rewardsfalse/progressAuthorized/missingSources를구분. rawwave seq1048 vspoint1063/terminalcommand506 vsfinalno-op507 혼용0.
원departure input은 GET /v1/runs/:runId/b-departure-runtime-input + X-B-Departure-Input-Version combat.b-departure-runtime-input.v1.20261006, 기존GAME/no-store/ETag·If-Match. 실제원producer의5편성/별도2~4차량/owned성장원문공급, 현재inventory로구snapshot보충0. effectiveCombatStatsIncluded/runtimeOccupantBindingIncluded/selectionSlotIsCarriageSlot/canStart 모두false; 이source만으로5요원→Boccupant/효과profile구성을발명하지않음.
완료조건: 실제원v2 nativeexport→Server opaque ref발급→동일요청 retry/GET복구·계정/node/원revision/종결/실패수명 검증, 원입력/source일치와실미공급source를분리해원QA/인계기록. 원flowController/producer재사용, 하위에전송복제0. 지금 전체SQL도착/진행권한성공을요구하지않음: 실제originalprofile/occupant binding·도착정책은분리잔여. ServerTCP는 DERIVED_TRANSPORT_COMPOSITION(구v1capture+변경없는원신규journal), freshwholeUnityv2 실행증거아님. 이제Unity가실제wholev2 export와원runtimeinput으로검증하며LOCAL조정fixture를실운영증거로승격0. actualARRIVE는Plan질문중/VICTORY전환0, 실START·배포·지급0.



<a id="u-b-result-v2-full-budget-intake-20261006"></a>
### 기존 U-B-RESULT-TRANSPORT의 실제 whole v2 budget 대기 해제 입력
기존 등록키 U-B-RESULT-TRANSPORT@combat.b-result-report.v2.20261006 그대로; 별도중복작업 생성0/기존블록 삭제수정0. 서버선행 b-fresh-v2-full-budget-result-20261006 완료: actual unchanged Unity원4,400,234bytes POST200→whole GET→samebody receipt replay;35PASS+owner34PASS/type·lint0. API 원 Server docs/api/combat-b-result-report-v2.md, budget combat.b-result-budget.v2.20261006. 과거4MB/core1.8MB 대기 문구는 역사상태이며 현재v2 한도는 HTTP/whole canonical6,000,000UTF8bytes. 원runtime와 observation.native/journal 양쪽 보존, hash/revision/body원문/버전불변, 축소·강등0; private분할은서버책임/Unitychunkupload추가0. v1 1.8MB그대로.
선행근거 source manifest890c66cf8d8fa340acf385d0274e57649f2d3a4230f55bb869f773bd167ed10f; server QA/CommanderS1-20260922/combat-b-fresh-whole-budget-final-20261006.json SHA3dd400e1bab8306d63d85a36b7314565222f50536f8865c754027e2a46c943b2 및actualsample SHA5fde5ceda5da9dda784ed7bba440fdc75307e9763cda78467172d25ebdbc3777. disposable GAME SQL/TCP/sourcefixture일치검증, 상시DEV URL아님/listener종료.
소비완료조건: 기존typedtransport의v2finite한도를확정budget으로적용, 원freshwhole requestId/wholeJSON/hash 보존→기존GAME POST/GET/유실samebodyretry·receipt재생/401/409/413/no-store/해제후callback수명검증 후원QA/인계기록. 413을본문삭제로회피0; GET완전성실패409/storage_conflict를성공처리0. prior Unity재실행결과51uniquePASS는재사용가능하며 불필요producer재실행요구0. Progressfalse/ORIGINAL_DEPARTURE_FORMATION·same-runassignment/profile/occupant·PlanARRIVE/hold원source는별도잔여, 전송완료조건과순환대기0. 원화/최종View 선등록0/Unity채팅0/실START·운영배포·지급0. 실제Cloudflarememory/CPU는local테스트로증명하지않음.


<a id="u-b-assignment-result-boundary-input-20261006"></a>
기존 U-B-RESULT-TRANSPORT@combat.b-result-report.v2.20261006 및 U-B-ASSIGNMENT-SOURCE@combat.b-departure-assignment.v1.20261006 계약addendum만인계/중복새작업0. Server/WAIT#b-assignment-result-adaptation-result-20261006 구현검증: reportv1/v2버전그대로, Bimmutableadmission을oldA5형식과분리검증. B실선택actor불일치나원pin/assignment/고정slot/instance/refit·permanentstats모순409; 현재B형reportprogressfalse에 B_RUNTIME_ASSIGNMENT_PROVENANCE/B_RUNTIME_PHYSICAL_PLACEMENT/B_RUNTIME_EFFECTIVE_PROFILE 정확누락. 숫자slot/차량이름/actorID/profilehash일치로true변경0. actualsame-run executor소비/원physical-instance/approvedeffectiveprofile없으므로이addendum이runtime binding완료나최종통합선행AND해제아님. 현재freshLOCAL원reportA형snapshot의ORIGINAL_DEPARTURE_FORMATION은별도유지, 원자료를B형으로rebind요구0. 기존typing/error/false권위보존범위에서소비; 신규B실행producer계약·최종View자동착수0. actualsource QA aec0162c...; source16PASS 및v1/v2related48PASS/수정delta33PASS(중복), 자세한API combat-b-result-report-v2.md#b-assignment-result-boundary-20261006. Unity파일편집·채팅0.







<a id="u-core-late-combat-observation-preparation-20261011"></a>

### 기존 U-CORE 관측 준비 보완 — 후반 전투 생존·시간
새 서버 계약 소비큐 선등록이 아니라 기존 U-CORE의 원전투 관측 검수 보완이다. 원본34#late-combat-time-survival-review-20261011.
실제확인: Assets/_LASTRAIL/Scripts/Battle/BNamedOperatorNativeAdapter.cs:24~26/47~49는 장비/최종스탯 및 active/ultimate/fullActor 미지원. 원일반계산/다른경로완료여부와구분, 새계약없는현재reader완화금지.

지금 독립 가능:
- 기존 전투 clock/피해·치유·방벽·요격/다운·파손·스폰·완료의 실제 관측 사건을 기존 QA 수집경로에 매핑한다. 이미있는기록은재사용하고거대새전투모형/상태owner를만들지않는다.
- 요원/객차/열차별 피해와실흡수·실치유·전파, 예고/행동점유/기회손실, 전투/스폰대기/정리/중단시간을구분한다. 누락사건은0이아닌미지원/미확인.
- 단일입력/구독수명, restore 후동일사건중복없음, pause·배속을기존clock에서측정하는검수를원유효테스트와대조하고변경범위만수행한다. 전체완료테스트 반복만으로닫지않는다.
원소유source가미공급인부분은그범위만정확대기. 실제지원이이미있는경로면파일/버전/원근거를남겨Server와동일입력쌍비교로인수한다. 임의실계수나형식만맞춘프로필로밸런스PASS를내지않는다.

후속 실행(아직 선행 미충족): Server 실제지원/9종료상태·복합계약 인수후 같은상태에서기존/10·11추가안 비교. 미취득보상을미리적용하지않고실보상후다음전투만변화. 34검수범위에맞춰정비8/우회·빌드/위험도/짐칸편성을원지원범위에서확장. 최종View/휴대기기조작은시안·독립자산이후,비화면준비에미술전체완료선행없음. 운영START/실보상/배포/새화면작업0.


### U-KYLE-ART-01@kyle-delivery-v01 / 자산 인수·표현 연결 착수 가능 — 2026-10-14
사용자 승인 후속: 카일 합동 전투 승인 및 Unity 세션이 바로 적용할 수 있게 기준 정리 요청. 기존 큐 우선순위/타 항목 변경 없음.
선행: Direction 승인 1~4 대기·공격96PNG/탄환/명중8PNG/섬광사양 준비·검증 완료. [정확한 적용 기준](DIRECTION_2-D-20260913.md#kyle-unity-ready-v01), [manifest](../../QA/DirectionImpact-20260921/eve-combat-v01/kyle-delivery-v01/manifest.json), [검증](../../QA/DirectionImpact-20260921/eve-combat-v01/kyle-delivery-v01/verification.json), [ZIP](../../QA/DirectionImpact-20260921/eve-combat-v01/kyle-delivery-v01/kyle-approved-resources-v01.zip).
범위: 실제 D:/Project/LASTRAIL/unity-project에서 op_kyle 대기·공격4방향 importer/clip/표현 fixture 및 기존 전투 사건 소비 경로 확인. 기존 4장12fps builder를 그대로 사용하지 않고12노출 가변시간·방향별원화크기/고정발pivot·좌표반전·right-facing tool 탄환 기준 적용. hit/down/skill·다른요원·기존전투수치 변경 없음. Unity 담당이 실제 변경 파일/현행 소비경로를 착수 시 기록한다.
완료 조건: 원본/alpha/8모션 연결, 4방향 접지·레이나 대비 크기 실기 검수, 공격380ms 표현발사1회·pause/취소/restore/풀수명, 실제지원 HIT/MISS 사건에 독립명중1회, 기존 경로 회귀검증. 미지원 충돌/이벤트 경계는 명시 보류; 미술 비행300ms를 권위 피해판정으로 구현 금지. 기존 Unity QA/인계에 근거 보존 후 완료 처리. 자산준비 완료를 전체 게임 연결 완료로 취급하지 않는다.

<a id="kyle-import-partial-20261014"></a> U-KYLE-ART-01 진행: importer/105원PNG/8clip/접지fixture Import02 실제6/6, 기존인계#unity-kyle-import-result-20261014 참조. 원전체작업미완료로큐유지; controller/fireonce/flash/actualHIT·시각실기잔여만후속단독진행。

<a id="kyle-presentation-partial-20261015"></a> U-KYLE-ART-01 최신부분완료: 로컬timeline/View/독립FX·GPUfixture25/25, 본인계#unity-kyle-presentation-result-20261015 및QA/KyleUnity-20261014/verification.json. 미연결nativeKyle start/facing/anchor·HIT/sourceCue/scenecursor adapter와실game·Reina·mobile QA만잔여; 앞의후속세션생성예고는자동검토거부/ID없음으로정정, 동일세션진행. 전체항목삭제/완료승격0。




### U-MIRA-BASIC-BIND-01@mira-basic-ballistics-v1.20261015 — 부분 완료 / 첫 LOCAL 발사·HP·JSON 복원 검증·전모드 잔여
- 선행 AND 완료: S-MIRA-BASIC-SOURCE-01 정의·원천 공급/담당검증 및 U-FRIENDLY-PROJECTILE-LIFECYCLE-01 수명기반142PASS 인수. 원44#friendly-basic-ballistics-detail-v1-20261015, WAIT#mira-basic-live-followups-20261015. 마지막 선행 Server가 등록.
- 확정 Server 계약/실필드/내부함수: LAST_RAIL_SERVER_2-D-20260913.md#mira-basic-source-result-20261015. server/apps/game-api/src/combat-b-mira-basic-source.ts miraBasicDefinition/captureMiraBasicSource/readPinnedMiraBasicSource. 정식내부ability op_mira.basic,정의 mira-basic-ballistics-v1.20261015/source combat.mira-basic-source.v1.20261015. 기존원기본ATK계수1/1·1800ms원주기·인접2범위·OWN_BASIC1000bps 보존,승인48000mm/s/radius60mm/max24000mm/flight500ms/L12000mm·발사공격/도달방어. source는원owned/definition/commander/refit/hash/revision과원미공급null/false를반환. profile성장ATK를30상수로대체0/패시브이중적용0.
- 근거: QA/CommanderS1-20260922/mira-basic-source-20261015.json 신규14+인접23=37PASS0FAIL/type/scopedlint0,SHA256947af4bad7ad8f75fe706eee90955d4c0922eb0b7589cdbd8c424d39a9ac1adf. Unity 인계#unity-friendly-projectile-result-20261015 및 QA/FriendlyProjectile-20261015/verification.json·Green01/editmode.xml 수명기반142PASS/실HP미연결. Server가Unity검사를재실행한근거아님.
- 범위/담당 Unity: 먼저공급된정의·원source strictconsumer/원hash·계정·버전·복원검증을구현. 실scene muzzle socket/world-mm/targethitshape/LoS·geometry 관측을기존수명owner와결속하고무효과미라 원기회→발사→시간비례직선·swept접촉→도달DEF/감소→HP/사망/보상원사건·복원복합원자commit을검증. 미공급effective스탯/원동적D/기회/치명·실공간은해당부분만보류/정확원source담당기록,기존unsupported/START가드나false를임의해제0. 계약consumer 준비는미공급공간의전체완료를기다리지않음.
- 완료조건:6/12/24m·범위바깥/폭경계/고속얇은표적/이동MISS·차폐/요격/세대교체/다운후기발사탄·hold/중복/용량실패/restore/노드종료와원주기·피해1회·HP/사망/보상원자성. 원판정모형authoring/시험fixture/실scene 검증을구분하고48m시험모형을운영source로재명명0. 미라ACTIVE/적탄원천복사·새발수/주기/ATK·새원화권한0. 실제모드/최종표시미검증은명시.
- 후속: 본실연결/검증완료후 WAIT 원정의 P-MIRA-BASIC-INTEGRATION-01@mira-basic-ballistics-v1.20261015 Plan큐등록. 시술provider/정식인챈트ID·외부catalog/다중발사·관통 전체완료로승격0. 운영배포/실입장/실보상없음.
- 2026-10-15 소비부분완료: Unity 인계#unity-mira-basic-bind-partial-20261015·QA/MiraBasic-20261015/verification.json,실Server3패킷/신규15포함172PASS0FAIL. 원본pin/계정/정의/성장/refit/지휘보존 strictconsumer 완료,canStartfalse유지. effectiveStats/실geometry/발사원aggregate·nativeHP/보상/복원 결손표기록,실연결후속미등록. 같은consumer조사/구현을반복하지말고해당잔여만진행.

- 2026-10-15 잔여 진행 결과: Unity 인계#unity-mira-native-continuation-result-20261015 및 QA/MiraGeometry-20261015/verification.json nativeContinuation. 223PASS0FAIL/exit0, native 공간 저장 v2·staging 복원/원계층·소수 yaw·분할불변 수정, 저장 전용씬/prefab 재로딩 및1920x1080 GPU overlay 검수 완료(격리 판정). 실제 발사/HP bundle·모드 checkpoint는 미완료. 원치명 적용/결과 원천이 없으므로 첫 시험 N/A 명시 승인 또는 승인 원천/복원 경계가 최소 Plan 선행. 그 뒤 기존 Session/BasicLoop의 원기회/next·OWN_BASIC1회·FriendlyProjectileOwner→native HP/사망 journal 복합 게시·복원을 Unity가 구현한다. 가짜 비치명0·운영START 해제0·부모/공간 큐 유지.

### U-MIRA-GEOMETRY-01@mira-live-inputs-v1.20261015 — 부분 완료 / 첫 시험 원배치·세대/공간 JSON 복원 검증·최종 실기 잔여
사용자2번Unity실행지시. 기존U-MIRA-BASIC-BIND-01 공간잔여실행단위,부모유지/consumer재작성0. 원44#mira-live-inputs-and-fire-binding-20261015 필독. 실scene muzzle/hitshape/transform·차폐/세대·동일tick revision world-mm·swept접촉결속. 승인48m/s/반경60mm/24m/L12m·인접2유지.6/12/24m·폭/차폐·이동MISS/방향·hold/복원검증. 미공급shape/실리그는정확필드/실authoring필요근거,새적폭/객차치수임의확정0. 시험48m/pixel/적탄대체0. 원화편집/HP전체연결완료주장0. 실제근거기존Unity인계/QA,후속WAIT#mira-live-inputs-dispatch-20261015.

- 2026-10-15 실제원천대조: Unity 인계#unity-mira-geometry-authoring-gap-20261015·QA/MiraGeometry-20261015/verification.json,73씬/프리팹/1481C#검색. 물리3개는기존시험씬뿐,미라socket자료는미승인pixel진단. 실pose/socket/world-mm·첫대상shape/차폐판정모형 최소입력필요. 새치수발명/시험모형운영승격0,실swept/hold복원NOT_RUN·가드/런타임변경0. 전체실공급미완료/후속AND미충족,부모/자기큐유지.

- 2026-10-15 승인 판정 부분결과: Unity 인계#unity-mira-judgement-partial-result-20261015 및 QA/MiraGeometry-20261015/verification.json approvedAuthoringPartial. 별도 first-static consumer/판정 profile·native 관측·sweep 구현, 신규49PASS0FAIL/exit0·기존회귀157PASS. 저장 전용씬/실render 검수·원기회/치명/HP복합게시·모드/native복원·6/12/24m 별도 실검증·리뷰는 미완료. authoring 수치 입력대기는 해소됐고 이 잔여만 계속한다. 부모/자기큐 유지·후속 선등록0.

- 2026-10-15 geometry 잔여: 위 native-continuation 결과를 적용. 승인 authoring·전용 씬·native 공간 v2 복원·명시 거리/표면접촉·이동차폐는 검증했으나 실제 Board 배치/원세대 callback의 live 시험 context 결속은 미완료. 수학적 대수 동률 비교는 double 시간 한계로 별도 미보장, 최종 미술 socket/모바일/운영scene 미검증. 부모 발사/HP/모드 checkpoint와 구분하고 완료 삭제/후속 등록하지 않음.

### 첫 미라 치명NA 승인 — 기존 U-MIRA 잔여 재개
등록키 MIRA-FIRST-CRITICAL-NA:UNITY:20261015. 사용자 명시 승인 원본44#mira-first-test-critical-na-20261015 및 #mira-first-live-trial-order-20261015. 첫 무효과 시험만 치명 NOT_APPLICABLE, 확률0/추첨 실패로 대체하지 않으며 치명 RNG 소비 없음. 일반 전투/별도 장착시험 확대0. 원Server packet/hash 보존, 시험 context/checkpoint에 명시 정책 결속·복원 불일치 거절. 이 범위의 원치명 입력 대기는 해소했으며 U-MIRA-BASIC-BIND-01/U-MIRA-GEOMETRY-01의 실제 발사D·원기회/next·HP/사망·복합 복원 잔여를 계속한다. 공간223PASS 재작업이나 실발사 완료 승격 없음. 기존 큐 유지/후속은 실통합 완료 후만 등록. 운영START/보상/배포 없음.
### 기존 U-MIRA-BASIC-BIND-01 추가 승인 — 재탐색·대기
등록키 MIRA-RETARGET-WAIT:UNITY:20261015. 사용자 명시승인, 원44#basic-target-reacquisition-approved-20261015와 앞 proposal 절. 기존 우선순위·유효 준비대상 유지, 발사 직전 무효화시 한 번 재탐색, 없으면 취소/대기, 밀린 탄 누적 없음. 실제 Session/BasicLoop 원기회·next/발사 게시 경계에서 발사HP복원 작업과 함께 구현·검증. 인위적 준비시간/새표적AI/추가 공격기회 없음. 준비 취소 비용/발사후 고정은 기존계약 유지. ID·generation·정책/기회 상태 복원, 확정탄 재탐색0, 다요원 동일step·hold/중복·빈전장 검증을 기존 QA/인계에 보존. 부모 미완료 유지, 기존 P-MIRA 실제통합 후속 조건 유지. 새 표현 제안 projectile-feedback-proposal은 구현범위 아님. 완료/부분완료·검증·잔여를 사용자 지시대로 총괄에 결과 답장. 운영START/배포/실보상 없음.
### 기존 U-MIRA-BASIC-BIND-01 표현 승인 추가
등록키 MIRA-FEEDBACK:UNITY:20261015. 사용자 직접 승인·작업 지시. 원44#projectile-feedback-approved-20261015 및 앞 proposal. 주탄/보조/관통 구별·실접촉/보호막/HP·MISS/차폐 표시, 독립 View와 원사건 바인딩/중복·해제·복원 검증 진행. 현재 실공급된 사건 연결부터 착수, 미공급 다중/관통은 입력 계약·독립 표현 준비와 실연결을 구분. 별도 미래 소비 큐를 선등록하는 것이 아닌 기존 부모 표현 범위 추가. 원발사HP복원과 독립 View 준비는 진행 가능, 새 미술선행으로 원작업 차단0. 정확 지속시간/팔레트·합산창 및 damage-feedback-detail-proposal의 새 HP숫자 정책은 미승인이므로 임의 채택하지 않음. 기존 자산 적합성/임시표현 표시, 최종 원화/socket·모바일 미검수 별도. 실제QA/인계·완료 또는 부분결과 답장, 부모 전체완료/후속등록은 실수용 후. 운영START/배포/실보상 없음.
<a id="mira-first-native-local-qa-20261015"></a>
U-MIRA 최신 결과 / 2026-10-15: 승인 첫 무효과 LOCAL profile의 원발사→지연접촉→native HP/보호막/사망·원node/공간 JSON 복원과 재탐색·실 MAIN 표현 시험 검증 완료. 기존 인계#unity-mira-first-native-local-result-20261015 / QA/MiraFirstLive-20261015/verification.json·FinalGreen04 112/112PASS0FAIL0SKIP/exit0. JSON null/float wire 복원 실제실패→수정·strict hash/version보존, 기존정의/Server packet/원출발 유지. 다중/관통은 표시계약 fixture뿐이며 실제장착source/예약·다요원·일반협동 epoch/서버거래cursor·전모드·최종미술/socket/mobile/실Player·기존숫자consumer 잔여. 위 과거 ‘치명/원배치/HP 결손’은 당시 이력이고 첫 명시profile에서는 해소됐다. 부모두항목과전체후속AND는미완료이므로 삭제/Plan선등록0. 이미 검증한 LOCAL 첫시험을 중복 구현하지 말고 잔여만 진행한다. 총괄 결과답장 전송확인은 기존인계/QA가 보유한다.
<!-- UNITY-EQUIPPED-TRIAL-POLICY-ONLY-20261015 -->
정책 인수만 / 소비 실행항목 아님: 원44#mira-equipped-controlled-trial-approved-20261015·13#multishot-formal-catalog-approved-20261015의 EN17/새저장버전·별도LOCAL1–2성 연구/치명NA 승인 확인. 정확 source/catalog/envelope/profile 결속은 Server 실계약·검증 후 등록되는 소비큐에서 진행한다. 기존 빈장비profile/packet/hash·112PASS 보존, 가짜source/미지원효과0폴백/기존시험 재구현0. 상세는 기존 Unity 인계#unity-equipped-trial-policy-intake-20261015. 새피해숫자/6배치/관통/다요원/3–5성/전모드 확대0. 현재 새소비큐 선등록·자동폴링·수신확인답장0.



### 승인 5인 전투 시각 참고 소비 — 2026-10-17
등록키 FIVE-AGENT-VISUAL-V02:UNITY:20261017. Direction 전달 사용자 Unity연결요청에 따른 시각 소비 단위. 원본 QA/DirectionImpact-20260921/eve-combat-v01/five-agent-combat-fx-v02/QA.md ‘사용자 승인 및 Unity 소비 기준’ 및 unity-reference-contract.json(version direction.five-agent-combat-fx.v02.20261017)부터 읽고 시작. 원본은Direction 편집종료·보존, Unity는unity-project 소비어댑터/독립표현만소유.
승인5슬롯세로배치·크기0.9·1~3방향적열차/4방향전방장애물시각표현 유지. 뒤2슬롯카일/레이나복제는비교용placeholder이며실로스터발급0. 기존실ID를registry와대조,없는능력/4·5번실요원생성0. PNG교체재생/독립FX·드론/발앵커·이벤트bind/unbind. 배경/HUD reference-only,6400ms자동방향순환/비행시간/화면좌표를권위공격수치로이식0.
착수가능:309참조인벤토리대조·기존자산중복import방지·시각consumer/카메라상대배치·지원된실공격이벤트결속. 미정PPU/카메라와원runtimeID·실장애물hitbox/아군통과/파괴는원계약확인후해당경계만진행. 판정미공급은시각시험으로표시하고기존EN17/관통판정변경0. 기존전투코드같은파일동시편집금지.
완료조건:4방향/충전발사명중복귀·접지/선두시야/가림·화면비·hold/대상소멸/풀재사용·중복이벤트0,실행카메라/PPU·소비자산과미연결사건/실검수근거기존UnityQA인계기록. 원시안승인은최종Unity/실5요원밸런스승인이아님. 다른2요원자산대기와독립시각준비분리. 완료/부분결과총괄답장. 운영입장/경제/배포0.
FIVE-AGENT-VISUAL-V02-RESUME-20261017: 사용자 재개 후 표시기반 부분 검증. 기존 수명실패 fixture 수정·실playerloop4회재사용, idle/1024한도와 원본FX누락 보완. VisualFinal01 20PASS0FAIL0SKIP/exit0, Graphics02 새36PNG 표본육안·일부native크기검수. 기존실패이력은 인계/QA 보존. source delta review Critical0/Important0. 실제camera/PPU/roof·실5인/나머지2원화·nativephase/target/world projection·물리/파괴·픽셀동등성/최종미술 인수는 미완료. 기존Unity인계 #unity-five-agent-visual-v02-resume-result-20261017 및 QA/FiveAgentVisual-20261017/verification.json resume. 전체 완료조건 미충족으로 이 큐 유지·후속 선등록0.

FIVE-AGENT-COMMITTED-SOURCE-20261017: 기존20/36캡처 유지. 실제현재SceneDriver는Basic/CE03이며B native와별개, 읽기전용원snapshot/hold-clock소비·실Canvas좌표변환포트 신규8PASS0FAIL/exit0. 명시호출포트이며자동장면설치0. 현재720x1280 실내·외부HUD와승인941x1672 roof5 구도다름. 다음은실제battle owner/원phase·event/context/4방향·target generation/발총구impact공유좌표·roof/camera화면합성계약 확인후상위결속. 실제5인/나머지2원화·물리잔여 유지. 인계 #unity-five-agent-committed-source-result-20261017 및 기존QA committedSceneConnection. 전체미완료 큐유지/후속선등록0.

FIVE-AGENT-VISUAL-V02:UNITY:20261017 승인 갱신 / roof-battle-presentation.v1.20261017:44#roof-battle-runtime-presentation-approved-20261017 및 직전 상세절 전부 승인. B native 단일 LOCAL 실행상위→확정공격사건→View 실연결,2D941×1672고정fit/승인발점/공통투영authoring을 기존잔여로 계속한다. Basic/CE03와동시루프0. 미실측socket/방향축/투영점은실authoring으로진행하며허위물리값/가짜receipt금지. 먼저지원LOCAL하나의실발사·명중·hold·저장실패·복원·실Player검수,기존20/8PASS재작성0. source/자산미공급경계만남기고전체대기금지. 새가독성제안(roof-battle-readability)은미승인·범위추가0. 결과기존인계QA후총괄답장.
FIVE-AGENT-VISUAL-V02:UNITY:20261017 / 2026-10-17 사용자 취침 중단·저장: roof-battle-presentation.v1.20261017의 B native 단일 LOCAL owner/source/store·실행 관측 구현, RoofBoundaryGreen01 17PASS0FAIL0SKIP. UI/Scene/PlayerBuilder 4개 C#는 QA/FiveAgentVisual-20261017/RoofSceneStaging01 준비본이며 Unity 통합·실Player 빌드/실행은 미수행. 원 world/socket/contact 및 전체5인 인수 잔여. 기존 Unity 인계 #roof-b-native-local-user-stop-20261017 및 QA verification.json roofNativeLocal 참조. 기존 큐 유지·미충족 후속 선등록0. 사용자 재개 전 자동 작업/폴링 없음.
FIVE-AGENT-VISUAL-V02:UNITY:20261017 / B LOCAL 실제 Player 결과 / 2026-10-17: 사용자 재개 후 준비본4개 실제Unity연결, 배속/terminal-auto 중요2건 RED→GREEN. RoofIntegrationFinal01 18PASS0FAIL(씬3/D진단경계7/기존safequeue8); 기존17/20/8 재실행·합산0. 최종 RoofPlayerBuild05 성공/errors0/warnings4. FinalPortrait01 기능32PASS; 오류주입 개발콘솔 이미지 시각제외. 동일checkpoint 새프로세스 FinalNarrowRestore01(540x960)/FinalRestore01(실941x1080)/FinalWide01(1672x941) 각8PASS/exit0·깨끗한실제캡처3장 직접검수. 원 Eveinstant/HIT/HP700→675→650·pause/2x·save실패hold→복원/retry·중복0. 실제Button callback 자동검수, 물리pointer/touch미검증. 원천없는charge/drone/world총구/contact 생성0; neutral Eve1명/승인발점·고정fit LOCAL진단화면, 전체5인/실지붕접지·공통투영·공격위상/최종미술 잔여. Windows개발검수만 명시D진단경로·최종4회D파일/C크기mtime유지(기존C초기기록보존·앱전체이전주장0). 기존Unity인계 #roof-b-native-local-player-result-20261017 및 QA/FiveAgentVisual-20261017/verification.json roofNativeLocal.playerIntegration. 전체수용미충족 큐유지·후속선등록0·운영/경제/배포0.
FOUR-AGENT-V03 구현갱신 2026-10-19: 32논리클립/493참조294texture import·고정pivot·Sera/Eve독립FX/연기 구현, V03Integrated03 37PASS. 실제 D3D11 Player 3화면 각42체크/exit0·camera RenderTexture39캡처/17직접열람(OSframebuffer아님). 기존Eve원clock/HP/hold/restore→neutral body실결속. 네요원 accepted/cancel/down/target generation·world/facing/camera·실조작/통합은미완료. 기존인계 #unity4-v03-runtime-result-20261019 / QA verification.json fourAgentV03Runtime. 숨긴창검정캡처/초기실패보존·시각근거제외. 부모전체미완료 유지·후속선등록0.

### U-FIVE-ULT-PRESENTATION-01@ultimate-presentation-intake.v1.20261018 — 승인 연출 연결
등록키 FIVE-ULT-PRESENTATION:UNITY:20261018. 사용자 궁극기 전투 연결 상세안 승인·Unity큐 등록. 선행 Plan 계약완료:04#five-ultimate-presentation-intake-approved-20261018 및 앞 상세절, 기존 §14/DAILY v02,18#five-ultimate-cinematic-approved-20261018.
소유 Unity: D:/Project/LASTRAIL/unity-project의 기존 action/표시 연결과 QA/인계. 기존 B LOCAL/실Player 완료근거를 재사용하고 실제원행동 owner·스킬ID/세대·시계/hold·설정/시청·결과사건 공급여부부터 확인한다. Basic의발사/HP사건만 발견했다고5인궁극기원천이 공급됐다고 주장하지 않는다. 기존 owner/종료처리를 재사용하고 별도전투루프/피해owner를 추가하지 않는다.
착수: 표준→요약→최소FX 준비검사, 원수락된action 소비·중복/세대검증, 소유hold/입력·카메라복귀·오디오·비동기 수명, 스킵/실패 공통종료, 기존모드/DAILY 연결. View 콜백→Controller 기존처리, Model의원판정/저장 분리. 요약은전투진행,전체화면만안전tick hold. 영상종료/AnimationEvent의새피해·치유·소모·드론생성0.
최종자산 전에는 명시된표시fixture로 제어경계 검증가능; 실제궁극기통합/최종미술완료와구별. 공급된실원천은연결하고 미공급부분은필요계약/담당/실근거를기존인계에남긴다. 구공구/손촉매·가짜충전·임의skillId·전투결과상수0. Direction 원본/Server 원권한 수정0. 기존 작업 안전경계에서 착수,같은파일동시편집0.
완료: 입력1회/수락1회·스킵/종료경합1회·해제후갱신0·다른hold보존·원모드별피해/비용/결과동일·세대교체/복원/실패대체를관련범위에서검증. 실제Player실행/포인터입력/최종미술검수를구분하여큐ID·버전·원천/fixture·검증·남은의존을기존QA/인계에기록하고총괄결과답장. WAIT#five-ultimate-presentation-followup-20261018의전체AND 충족전완료삭제/통합큐선등록0. 운영START/경제/배포없음. 다음first-player-trial 제안의새시험범위를자동채택하지않는다.

- 2026-10-19 부분 구현·검증: 표시 session18검사·기타와 최종77PASS. 실제native 궁극기/DAILY·owned hold/replay 자산 결속 잔여, fixture를실궁극기로세지않음. 기존인계 #unity-approved-feedback-result-20261019 / QA/FiveAgentVisual-20261017/verification.json approvedFeedback. 전체AND전큐유지·후속선등록0.

### U-FIELD-FX-READABILITY-01@field-fx-readability.v1.20261019 — 승인 중첩 표시 연결
등록키 FIELD-FX-READABILITY:UNITY:20261019. 사용자승인·큐등록,선행18#ultimate-field-fx-readability-approved-20261019 및앞상세절. Unity소유unity-project기존표시consumer·QA. 기존roof/궁극기연결큐와같은경로는처리재사용,중복전투loop/거대View상태소유0. 진행중같은파일안전경계후착수.
즉시가능범위: 현재실공급원사건/geometry/표시자산에서핵심과장식분리·위험/HUD우선·장식밀도/후광감축,원탄/총구/impact/영역위치보존. 원target generation/effect instance/수명·최강효과판정결과소비,표시가효과합산/삭제/피해재계산0. View콜백은기존Controller바인딩,풀반환/Unbind/scene종료시자기FX/구독/비동기만해제.
미공급효과는표시fixture/미연결로명시,최종Direction자산전에도독립제어검증가능. 기본5인미공급을가짜능력으로채우지않음. 새숫자정책/합산/새설정·카메라흔들림/히트스톱추가0.
검증:동일원snapshot FX ON/OFF의피해/치유/상태/수명/경계동일,단독/중첩·표적소멸/풀재사용·hold/복원·화면비·필수입력보존. 실Player와fixture,자동callback과물리입력검수구분. 자산승인전부분기능완료와전체시각완료구분. 기존QA/인계에큐ID/버전/실근거/미검수보존·총괄결과답장. WAIT#field-fx-readability-followup-20261019 전체AND전통합완료/자기큐삭제0. 운영/경제/배포없음.

- 2026-10-19 부분 구현·검증: 원Mira core/원궤적·contact 보존·ShortTrail 독립감축/실Player기능 확인. 실제15효과/HUD위험 중첩·최종FX 및정상종료/입력 잔여. 기존인계 #unity-approved-feedback-result-20261019 / QA/FiveAgentVisual-20261017/verification.json approvedFeedback. 전체AND전큐유지·후속선등록0.

### U-COMBAT-RESULT-LABELS-01@combat-result-labels.v1.20261019 — 승인 원결과 숫자 소비
등록키 COMBAT-RESULT-LABELS:UNITY:20261019. 선행44#combat-result-labels-approved-20261019 및연결상세승인. 기존미라/roof/FX표시owner를대조하고같은경로재사용,새결과원장/전투loop추가0. Unity소유unity-project표시consumer·기존QA.
실공급원결과에서HP감소/흡수/실회복·초과/critical상태/효과거절이유·event/target generation을소비. 피해/회복을두HPsnapshot차이로추측하지않음. 원F/피해·치명·치유·충전재계산0. 현실공급부분부터진행,없는회복/면역원천은정확미공급으로기록하며fixture검증과실연결구분.
표시:실HP숫자·흡수방패·+실회복/초과제외·만피0생략·원치명강조·제어차단구별·원내림0접촉과MISS구별. 타격별표시/임의합산0,동일원event/대상세대중복0. 현재HP즉시반영/후행장식분리,풀반환·hold·복원에서옛숫자재발행0. 필수HUD입력가로채기0. 기존FIELD-FX수명/중첩제어와연결.
검증:부분/전량흡수치명·초과피해·부분치유/만피HoT·피해+제어면역·동시피해회복·다단/원내림0·세대교체/복원,표시ONOFF 원HP/충전/사망동일. 최종Direction자산과실Player가독성·물리입력검수는별도。기존QA/인계에큐ID/버전·원천/fixture·미검수기록후총괄답장. WAIT#combat-result-labels-followup-20261019 AND전전체완료/후속선등록0. 운영/경제/배포0. 다음HP/다운전이신규제안자동구현0.

- 2026-10-19 부분 구현·검증: 원Mira CONTACT hp/흡수·point/gen·NA 숫자 실소비/GPU 및Player읽힘, generic치유·치명·면역은fixture. 전체원천/최종미술/정상종료·입력 잔여. 기존인계 #unity-approved-feedback-result-20261019 / QA/FiveAgentVisual-20261017/verification.json approvedFeedback. 전체AND전큐유지·후속선등록0.

### U-HEALTH-DOWN-FEEDBACK-01@health-down-feedback.v1.20261019 — 승인 원상태 전이 소비
등록키 HEALTH-DOWN-FEEDBACK:UNITY:20261019. 선행44#health-down-state-feedback-approved-20261019 및앞상세절승인. Unity소유unity-project기존상태View/Controller·QA. 기존결과숫자/roof/FX큐의원owner·수명·표시경로재사용,같은파일안전경계후착수.
실공급현재/최대HP·개인방벽/공유풀·다운/복귀원시각·요원/객차generation 소비. HP즉시갱신·후행장식분리,실소진과만료구별,세라단축은다운상태유지. 원남은시간양수올림초/원복귀확정전0표시없음/시간미공급시다운만. 화면타이머가실복귀를실행하지않음.
실복귀는HP/적격즉시반영+2번모션,모션끝까지원행동지연0. 복귀중재다운/풀재사용/저장복원·hold/장면종료에서구callback·옛소진/숫자재발행0. 카드확인/스킬입력기존callback바인딩,중복구독/해제후갱신0. 객차파손을요원다운으로대체0.
현재공급부분부터진행,미공급원시각/상태는명시·fixture와실연결구분. 기존검증재사용,부분/전량흡수·같은tick피해회복·지원후다운·再다운·객차/요원조합·복원/실Player읽힘·입력검수. 기존QA/인계에큐ID/버전/실근거·미검수남긴후총괄답장. WAIT#health-down-feedback-followup-20261019 AND전통합완료/후속선등록0. 운영/경제/배포0. 다음궁극기적격안내제안자동구현0.

- 2026-10-19 부분 구현·검증: 명시원상태·양수올림/no0·소진vs만료/공유풀·다운/복귀ticket20검사. 실제native producer/generation/recoverAt 및원자산·Player결속 잔여. 기존인계 #unity-approved-feedback-result-20261019 / QA/FiveAgentVisual-20261017/verification.json approvedFeedback. 전체AND전큐유지·후속선등록0.

### U-ULT-READINESS-01@ultimate-readiness.v1.20261019 — 승인 적격 이유 소비
등록키 ULT-READINESS:UNITY:20261019. 선행44#ultimate-readiness-feedback-approved-20261019 및앞상세절승인. Unity소유unity-project 기존원능력/카드표시소비·QA. 진행중같은파일안전경계후기존입력/수명/궁극기표시처리재사용.
충전실값과원적격을별도소비,강한준비강조는실적격확인시에만. 대표거절이유기존owner우선,없으면승인된표시우선순위로표시계층에서정리하되원판정순서변경0. 같은결과/원revision을카드/버튼에연결. 미공급지원/적격은가짜대상없음/가능상수로대체하지않음.
수동거절은기존영역짧은안내/동일기회반복문구중복0,거절터치자동재실행0. AUTO매평가실패팝업0,원충전·일반스킬·기본공격유지. 수동AUTO경합원소비1회,컷신미준비는요약/최소FX대체. 전체화면차단입력의복귀후발사0. View콜백→기존Controller/원owner,Bind/Unbind·generation·scene/비동기수명준수.
검수:충전충분+다운/대상없음/행동중·모드미지원/미확정·효과없음·연타/동시입력·자산없음·hold복원/세대교체. 표현ONOFF원행동/소비동일。최종자산없이가능한기능은진행,fixture/실공급/Player검수구분. 기존QA/인계에큐ID/버전/원근거/잔여후총괄답장. WAIT#ultimate-readiness-followup-20261019 AND전전체완료/후속선등록0. 운영/경제/배포0.

- 2026-10-19 부분 구현·검증: 원충전/적격분리·대표이유·수동중복억제/AUTO무팝업·차단입력무예약14검사. 원native적격/같은revision 원천·실카드/버튼·Player결속 잔여. 기존인계 #unity-approved-feedback-result-20261019 / QA/FiveAgentVisual-20261017/verification.json approvedFeedback. 전체AND전큐유지·후속선등록0.

### U-FOCUS-FEEDBACK-01@focus-feedback.v1.20261019 — 집중 원상태·가림·투영 소비
등록키 FOCUS-FEEDBACK:UNITY:20261019. 선행 Plan 상세/승인 완료:44#focus-visibility-boundary-approved-20261019 및 연결 두 상세절. Unity 소유 unity-project 기존 선택/카드 View·Controller와 QA. 기존 승인 큐의 같은 파일 편집 안전 경계 후 진행, 새 선택 처리 중복 구현 없음.
현재 실행 단위: 기존 선택 명령·실적격/해제 사건·대상 세대·정보 공개/카메라 투영 공급 확인과 공급된 경로 연결. 참여 강조는 기본공격 원적격, 궁극기 준비와 구별. 부적격 요원 자기 전투·진행 행동·비행탄 유지. 화면 밖 유효 선택 유지, 실제 이탈/사망 해제는 원owner 결과만 소비. 실제 차폐와 장식 가림 구분, 미확인 좌표/적격 추정 없음.
원 hit-test/입력 차단 재사용, 숫자/선택 장식 입력 가로채기 없음. 현재 카메라 재투영·옛 revision/세대 콜백 차단·Bind/Unbind 수명 적용. 별도 물리 검사 loop/서버 폴링/새 저장 원장 없음. 미공급 계약은 해당 경로만 의존 대기로 기록, fixture와 실공급 구분.
검수: 전원 부적격/혼합 사거리·차폐, 집중 변경 중 비행탄, 화면 안밖/실이탈, 같은 ID 새세대, 표식 만료와 집중 유지, 입력 겹침·컷신/복원·화면비. 표시 ONOFF 원선택/발사/피해 동일, 입력 한 번 처리 한 번. 기존 QA/인계에 큐ID/버전/근거·실Player 미검수 남기고 총괄 결과 답장. WAIT#focus-feedback-followup-20261019의 최종 자산·실Player 통합은 별도. 다음 적 공격 예고 신규 제안 자동 구현 없음.

FOCUS-FEEDBACK Unity4 2026-10-19 부분: 표시소비/입력위임/바인딩·revision/현재투영 실패 경계 구현. 최종31PASS(집중fixture17+준비표시회귀14)/exit0. 실제B producer/host·최종자산·Player 미결속; 전체AND전큐유지. 기존인계 #unity4-focus-result-20261019 및 QA verification.json focusFeedback. 후속선등록0.

### U-ENEMY-TELEGRAPH-01@enemy-telegraph.v1.20261019 — 원공격 예고 사건 소비
등록키 ENEMY-TELEGRAPH:UNITY:20261019. 선행44#enemy-telegraph-lifecycle-approved-20261019 및 상세절 승인 완료. Unity 소유 unity-project 기존 원공격 표시/Controller·QA. 같은 파일 진행 작업의 안전 경계 후 기존 사건·시간·입력/수명 재사용.
현재 실행 단위는 공급된 공격의 원대상/geometry·시작/확정/해결/취소·전투 시계/마감·공격/대상 세대 소비와 연결. 미공급 필드/공격은 정확히 기록하고 fixture와 실공급 구분. 타깃 고정/재지정·취소/발사체 수명은 원규칙 유지, 애니메이션 끝/경고100%로 피해 실행 없음. 베일·오르트6공격 계약을 일반 적 전체에 확대하지 않음.
예고와 명중 분리, 독립 경고 중첩/취소·최신 투영·원정보 공개·hold/복원·옛 사건 차단과 구독 해제 적용. 새 서버 폴링/추정 범위/시간 없음. 최종 자산 이전 가능한 기존 표시 경로부터 검증한다.
검수: 표적 소실/파손→수리, 원재지정, 즉시 타격vs비행탄, 같은 대상 두 예고 중 하나 취소, 집중/표식 중첩·화면 밖·컷신/복원/풀 재사용. 표시ONOFF 원표적/시각/피해/탄수명 동일. 기존QA/인계에 큐ID/버전/실근거/Player 미검수 기록 후 총괄 결과 답장. WAIT#enemy-telegraph-followup-20261019 최종 자산/실Player 통합 별도, 다음 전투 종료 신규 제안 자동 구현 없음.

### U-COMBAT-TERMINAL-01@combat-terminal.v1.20261019 — 종료·결과 Controller 연결
등록키 COMBAT-TERMINAL:UNITY:20261019. 선행44#combat-terminal-presentation-approved-20261019 및 앞 상세절 승인 완료. Unity 소유 unity-project 기존 전투/결과 Controller·View·QA. 같은 파일 진행 작업 안전 경계 후 기존원종료/정산/저널 처리 재사용.
현재 실행 단위: 실제 종료 결과/사건순서 공급 확인과 공급된 경로의 표시·입력 수명 연결. 적수0로 승리 추정 없음, 잔류탄/모드별 원종료 유지. 마지막 유효 결과와 종료 후 무효 콜백 구분, 결과 진입 전 잔류 장식은 정산 선행 조건 아님. 집중/준비/예고 정리·원최종HP 유지. 컷신 끝/스킵/종료는 단일cleanup과 자기소유hold만 해제, 결과 화면 진입1회.
화면 전환 중 제스처 종료로 결과 버튼 오입력 차단. 원수락/소비/거절 재사용, 새환불/재실행 없음. 확정승패와 지급대기 구별,View 종료시 제출 정산/복구 기록 삭제 없음. 새전투/계정 세대에 옛콜백 전달0. 미공급 계약은 해당 범위만 기록/대기,fixture와 실연결 구분.
검수: 잔류탄·다음wave·동tick 파괴, 마지막유효결과 지연/무효콜백, 궁극기·스킵·종료경합, 누른채화면전환, 지급응답유실/복원/새전투. 표시ONOFF 원승패·HP·소비·보상동일. 기존QA/인계에 큐ID/버전/실근거·Player잔여 기록·총괄답장. WAIT#combat-terminal-followup-20261019 최종자산/실Player통합 별도. 새상태효과 표시제안 자동구현 없음.

### U-STATUS-FEEDBACK-01@status-feedback.v1.20261019 — 원효과 시간·중첩 소비
등록키 STATUS-FEEDBACK:UNITY:20261019. 선행44#status-effect-duration-feedback-approved-20261019와 상세절 승인. Unity소유 unity-project 기존상태View/Controller·QA. 사용자 지정 네요원v03 실제연결 진행을 선점하지 않고 같은파일 안전경계 후 진행.
공급된 실효과 owner의 인스턴스/출처/대상세대/revision·강도/중첩·원시간/갱신/만료/해제 사건 소비. 임의합산/재적용횟수 집계0,원개별갱신예외 유지. 양수올림초·시간미확인 숫자숨김,View시간으로 효과삭제0. 정화/예방권과 자연만료 구별,차단효과 가짜등록0. 미공급은 경로별기록·fixture와 실제연결 구분.
기존상태/결과표시 재사용·안정배치·입력통과·hold/복원/세대/구독 수명 검증. 중첩상한재적용/약한무갱신/시간갱신,두출처중하나종료,정화후다른효과유지,다운복귀/만료갱신경합/복원 확인. 표시ONOFF 원효과·시간·피해·소비동일. 기존QA/인계에 큐ID/버전/실근거/Player잔여 기록 후 총괄답장. WAIT#status-feedback-followup-20261019 최종자산·통합 별도. 배속/정지 신규안 제외.

### U-PAUSE-SPEED-FEEDBACK-01@pause-speed-feedback.v1.20261019 — 원시계 상태 표시 소비
등록키 PAUSE-SPEED-FEEDBACK:UNITY:20261019. 선행44#combat-pause-speed-feedback-approved-20261019/상세절 승인. Unity소유 unity-project 기존시계/HUD·Controller·QA. 네요원v03 실제연결 우선순위 유지,동일파일 안전경계 후 진행.
현재 실행단위: 공급된 선택배속/유효정지·원대표사유/복원 상태를 기존시계owner에서 소비. 표시용 새hold/Time.timeScale/원시계복제0. 컷신은 자기hold만 반환,메뉴잔여정지 보존,차단입력 예약0·표시갱신 재명령0. 분기대기/서버대기에서 전체정지 추정0. 미공급 계약은 해당경로 기록·fixture와실공급 구분.
기존궁극기cleanup/입력/시계처리 재사용.2배→컷신스킵,메뉴중첩/연속전체화면tick누수/부분요약/백그라운드/서버만료/종료재개경합·구독해제 검수. 표시ONOFF 원시간/피해/자원 동일. 기존QA/인계에 큐ID/버전/근거·Player잔여 기록 후 총괄답장. WAIT#pause-speed-feedback-followup-20261019 최종자산/통합별도. 다음 기본공격대기 신규안 자동구현0.

### U-SELECTED-RANGE-01@selected-range.v1.20261019 — Idle·선택 기본공격 범위
등록키 SELECTED-RANGE:UNITY:20261019. 선행44#operator-selection-range-approved-20261019/정정절. Unity unity-project 기존선택Controller/독립범위View·QA 소유. 네요원v03 우선순위와 동일파일 안전경계 유지.
현재 공급된 요원선택/해제 통지와 원기본공격 사거리·지원영역·세계앵커/geometry 읽기전용 소비. 선택1명 표시/교체/해제숨김,원카메라재투영·세대/구독수명. 원범위검사를 표시에서 복제0,전원미라24m복사0. 미공급 경계만기록·fixture/실연결구분. 범위표시는 입력차단/강제공격/전투정지0. 정상대기Idle 유지,수락행동/회수/다운 덮기0. 미승인 대기이유 표시확장 구현0.
원선택 입력/Controller 재사용,같은요원재클릭/빈전장해제 등 다음신규제스처 임의도입0. 선택교체/해제·범위/객차고저차·차폐 의미·카메라/컷신/복원 검증,표시ONOFF 원대상/발사/피해동일. 기존QA/인계에 큐ID/버전/공급/검증/Player잔여 기록·총괄답장. WAIT#selected-range-followup-20261019 최종자산/실Player통합 별도.

### 기존 선택 범위 큐 정정 — SELECTED-RANGE-INFO:UNITY:20261019
사용자 직접 요원정보보기+장착장비아이콘 지시. 기존 SELECTED-RANGE:UNITY:20261019는44#operator-info-equipment-correction-20261019 / info-equipment.v2.20261019를 우선 소비한다. 별도부모기능 중복생성 없음. 요원클릭시 정보보기 상태로 사거리+실장착3슬롯 아이콘 동시표시/요원교체시함께교체/해제시함께숨김. 현재run 실제장착원본·revision 소비,미확인과빈칸 구별·미리보기/가방품 대체0. 정상Idle·원전투유지,장비교체/해제명령 추가0.
Direction은 기존 정책/승인시안 확인후 독립 슬롯틀·장비아이콘 재사용과 실크기배치 후보/가림검수. Unity는 기존선택Controller→정보/범위View 결속·장비owner 읽기전용 소비·옛선택응답격리/구독수명과0/1/3장비·빠른선택전환·실장착갱신검수. 각자 기존소유파일만 편집,미공급만보류·기존진행순서유지. 기존QA/인계에 v2 적용/실근거/잔여를 기록한다. 이전WAIT후속키를 재사용하며 최종통합은 장비/범위 동시읽힘까지 확인. 새선택제스처/아이콘상세팝업 자동구현0.

### 요원정보 장비 상세 추가 — SELECTED-RANGE-DETAIL:UNITY:20261019
사용자 직접 장비아이콘 상세보기 승인. 기존 SELECTED-RANGE:UNITY:20261019에44#operator-info-equipment-detail-approved-20261019 / info-equipment.v3.20261019 적용. v2의 아이콘상세 자동구현금지는 이번 명시승인으로 해소. 기존입력제스처의 미승인부분은 유지.
장착아이콘→읽기전용 상세1개→닫기시 같은요원정보/범위 유지. 이름/성급/기본옵션·고유/인챈트확정값/조건·원세트상태 공급분 표시,13 최신정책/기존상세 재사용. 빈칸상세0·미확인/실패구별·장착/판매/합성등 거래버튼0·보기로전투정지0. Direction 독립기존아이콘/프레임/동적문구 배치후보와가림검수,Unity 기존Controller callback/실instance·revision/선택세대·구독수명 소비. 요원변경/아이템교체/종료시 상세닫기·늦은응답차단,닫기뒤입력전달0.
각역할 소유파일/진행순서 유지,동일부모 중복구현0. 기존QA/인계에 v3근거·미공급·검수기록 및 총괄답장. WAIT 기존후속키 재사용,최종통합수용에 상세조회/닫기/응답역전/읽기전용불변·실Player추가. 신규부모큐 없음.

### 정보창 닫기 입력 승인 반영 — SELECTED-RANGE-NAVIGATION:UNITY:20261019
기존 SELECTED-RANGE:UNITY:20261019에44#operator-info-navigation-approved-20261019 / info-navigation.v4.20261019 반영. 사용자닫기입력안 명시승인으로 앞신규제스처미승인제약은해당상세범위에서해소. 장비상세닫기/뒤로가기 한단계복귀,바깥전장첫클릭 상세만닫기·동일입력뒤전장명령0. 요원정보닫기/같은요원재클릭/빈전장해제시 정보+범위숨김/적집중기존정책유지,다른요원교체·적클릭집중과정보유지.
기존UI/지원/안전메뉴·드래그계약보존,다운동일요원정보유지/퇴장·종료닫기,컷신차단/유효상태복귀·옛입력/비동기결과차단. Direction 기존닫기/상세배치독립요소·실크기입력/가림비교,Unity 기존Controller상태·입력소비/Bind수명·실Player검수. 새중복부모/전역입력체계0,진행파일안전경계유지. 기존QA/인계에v4근거/잔여 및총괄답장,WAIT 기존후속키재사용. 다음요원요약이름/HP배치안은미승인으로제외.

### 요원 정보 기본 구성 승인 — SELECTED-RANGE-SUMMARY:UNITY:20261019
기존 SELECTED-RANGE:UNITY:20261019에44#operator-info-summary-approved-20261019 / info-summary.v5.20261019 적용. 이름/기존승인초상·현재최대HP·독립개인보호막·장비3슬롯,전장범위 유지. 다운/복귀는기존표시계약재사용,공유보호막을개인값으로복제0·미공급값0추정0·새전체스탯표/중복효과목록0.
Direction 기존자산/독립트랙·수치/아이콘·시안비교/실크기가림검수,Unity 원Vitals/장비owner 읽기전용입력·같은선택세대로이름HP장비일치/원revision보존·구독수명·실Player검수. 실제미공급부분만대기,기존정상표시경로재사용. 기존QA/인계에v5근거/잔여와총괄답장. WAIT기존후속키재사용·같은부모중복구현0·진행작업안전경계유지. 새장비상세스크롤/레이아웃안은미승인범위로제외.

### 장비 상세 배치·스크롤 승인 — SELECTED-RANGE-LAYOUT:UNITY:20261019
기존 SELECTED-RANGE:UNITY:20261019에44#equipment-detail-layout-approved-20261019 / equipment-detail-layout.v6.20261019 적용. 상단아이콘/이름/성급/닫기고정,기본옵션→고유→인챈트→세트본문스크롤·조건/긴설명생략0. 다른instance전환처음부터/같은장비실값갱신읽던위치유지·유효범위보정. 패널스크롤 뒤전장/카메라전달0·스크롤끝놓음닫기오인0.
Direction 기존독립자산/안전영역·실크기긴설명/닫기·필수전장가림 비교,Unity 기존상세View/Controller/스크롤수명과교체/종료/컷신/조회갱신검수. 정확px/폰트는미술검수로확정,새전체화면관리창/거래명령0. 부모중복구현0·진행안전경계유지. 기존QA/인계v6근거/잔여·총괄답장,WAIT기존후속키재사용. 다음세트충족표현세부는신규제안으로제외.

### 세트 충족 표시 승인 — SELECTED-RANGE-SET-STATUS:UNITY:20261019
기존 SELECTED-RANGE:UNITY:20261019에44#equipment-set-status-detail-approved-20261019 / equipment-set-status.v7.20261019 반영. 해당장착자 실제세트개수·1/2/3누적단계 충족/미충족 체크/문구·미충족설명읽힘. 조건충족과실발동구분,타요원/가방/미리보기비합산,View세트재계산0. 원인스턴스유지시다른슬롯확정변경갱신/원아이템교체시상세닫기·스크롤수명유지.
Direction 기존세트기호/독립문구/대비·실크기검수,Unity 기존원집계/상세View수명·A3/A2+B1/A1+B1+C1/미공급/응답역전/Player검수. 각담당소유파일/진행안전경계유지,같은부모중복구현0. 기존QA/인계v7근거/잔여·총괄답장,WAIT기존후속키재사용. 다음인챈트본문배치는신규제안으로제외하되기존13의승인EN17표시정책은유지.

### 인챈트 상세 배치 승인 — SELECTED-RANGE-ENCHANT:UNITY:20261019
기존 SELECTED-RANGE:UNITY:20261019에44#equipment-enchant-inspection-layout-approved-20261019 / enchant-inspection.v8.20261019 반영.13 기존EN17승인표시정책 재사용. 장비성급vs시술성급/이아이템저장수치·실장착자적용/중복상태/원조건설명 배치. 낮은중복의수치를높은값으로덮기0·장비전체무효표시0·보조대상없음에적용배지점멸0. 미지원원천/다른인챈트규칙추정0.
Direction 기존독립문구/상태기호/숫자배치·긴설명검수,Unity 기존원선정instance/출발원본·상세View/스크롤수명소비.1성시술→2성장비/15vs25/동률·미공급/출발후인벤토리변경·선택교체 검증. 기존EN17유효근거재사용·변경View검수추가,진행파일안전경계/부모중복구현0. 기존QA/인계v8근거/잔여·총괄답장,WAIT기존후속키재사용. 다음정보준비/재시도표시세부는신규제안으로제외.

<a id="unity5-info-equipment-partial-20261019"></a>
### 기존 SELECTED-RANGE 소비 결과 / Unity5 2026-10-19
v3장비상세/v4닫기 컨트롤러·Eve실LOCAL요원입력·Mira확정context별도표시 부분구현 검증. [원인계](LAST_RAIL_Unity_2-D-20260913.md#unity5-info-equipment-result-20261019), [QA](../../QA/OperatorInformation-20261019/verification.json): Green04 27/27·보완ReviewGreen07 15/15(중복포함), Player03 17체크/exit0·PNG3직접열람. 일반장착/아이콘·상세원본/world범위/최종미술/물리입력·통합미완료로 부모삭제/통합후속등록0. 이후 summary/layout 최신정정은 다음단위에서 승인절/실원천 확인후 소비; 기존다른큐 수정없음.

### 클라 메모리 즉시 표시 정정 — SELECTED-RANGE-MEMORY:UNITY:20261019
사용자 직접 정정. 기존 SELECTED-RANGE:UNITY:20261019에44#operator-info-memory-direct-approved-20261019 / info-memory.v9.20261019 우선적용. 이전조회실패/로딩/재시도UI 문구는이기능에서폐기. 클릭시원전투메모리의선택요원/Vitals/실장착3슬롯/장비정의·실적용원본을바로표시,장비상세도같은원본소비. 별도조회API·로딩타이머·재시도·새캐시원장0.
기존상태통지/Controller/읽기전용View·세대/Bind수명재사용. 필수데이터누락은연결오류를진단하고원공급/바인딩수정,가짜0/없음/옛요원값대체0·사용자용재시도UX추가0. 미지원필드는정확잔여로기록,지원되는실경로부터진행. 기존원거래오류처리삭제범위아님. 기존코드에이기능용불필요조회UI가추가됐다면실경로확인후해당범위만제거·검증.
선택→상세→스크롤→한단계닫기→다른요원→해제/다운/종료/상태갱신 검수,실Player입력·실메모리일치근거기록. 네요원v03 등진행파일안전경계유지. 기존QA/인계에v9반영·원공급/검증/미완료·총괄답장. WAIT기존후속키유지.44#operator-info-integrated-acceptance-proposal-20261019은통합시험구성참고이며기존필수검증을승인대기로멈추지않는다.

### 정보 원천 결속 구분 — SELECTED-RANGE-DATA-BOUNDARY:UNITY:20261019
기존SELECTED-RANGE/info-memory.v9에44#operator-info-data-boundaries-20261019 적용. 메모리즉시표시가현재전필드연결완료라는뜻아님. 필드별producer→원payload/저장·메모리owner→adapter→View/변경통지를기존QA에명시. 서버발급데이터도전투중에는원메모리소비,Task인터페이스를HTTP로오인0.
현재Eve Task.FromResult(owner.Snapshot())/slots null,미라trial start.source 일부슬롯/EN17와일반장착owner미연결을구분. 이름/아이콘·원옵션/세트·범위미공급을별도원인으로기록. 원payload에존재하면기존Unity결속수정,실제로없으면Server계약잔여로정확히기록·WAIT해당경계만대기. 임의새API/로딩·재시도UI/원판정복제0. 기존승인연결범위만진행,각모드실권위유지·선택클릭정보HTTP0/원값일치검증. 동일부모·기존QA/인계사용,전수확인전전체메모리공급주장0.

### 시작·선택 확정 데이터 재사용 — SELECTED-RANGE-START-CHOICE:UNITY:20261019
사용자직접승인. 기존SELECTED-RANGE에44#combat-info-start-choice-approved-20261019 / info-start-choice.v10.20261019 적용. 전투시작원데이터+선택지확정때수신/적용한원상태를현재메모리에보유·정보창/장비상세는그대로표시. 클릭별조회/로딩·실패·재시도UI0,별도원장/판정복제0.
후보/미리보기/실패/취소는현재적용정보변경0.05 CONFIRMED≠효과적용완료 유지,원적용영수증/owner 확정후실효과표시. 출발고정필드덮기0·중복응답재적용0. 기존start/선택/복원Controller가확정과통신소유,View는상태통지소비. 실제payload/owner/메모리/표시필드연결근거·미공급범위기존QA/인계기록. HTTP0/확정전후/중복·응답유실복구/선택전환·Player검수,진행안전경계/같은부모재사용·총괄결과답장. 타씬새기획은미승인으로제외.

### U-SCENE-INFO-SOURCE-01@scene-info.v1.20261019 — 승인 기존정보 재사용
등록키 SCENE-INFO-SOURCE:UNITY:20261019. 선행49#scene-information-lifecycle-approved-20261019/앞상세,44 start/choice 승인. 기존계정/run Model·화면Controller/QA소유. 요원정보/네요원진행파일안전경계유지. 현재공급된필드부터최초취득→메모리→확정통지→무효화/재진입 경로대조/최소연결. 장비는작전인벤토리·실장착/미장착,계정items와혼용0·전투중새거래허용0.
既유효상태있으면화면열기마다무조건Refresh하지않고원계정/버전/무효화에따라재사용. 서버확정명령/필수재조회보존,원응답미공급은해당범위대기. 실제memory欠損を새API로우회0. SELECTED-RANGE동일기능중복구현0. 계정전환/응답역전/실패복구/preview취소/確定후관련화면일치 검증.
49#scene-information-call-count-20261019의재화→열차→편성9→5/유효보유0은조건부이론값. 기존transport계측으로실제사용경로/외부HTTPvsLOCAL·읽기vs명령·재시도구분하여변경전후확인. 실제원응답/Model근거와호출수·UI/권한불변을기존QA/인계기록후총괄답장. 전체씬완료주장0·새상시폴링/공통TTL0. WAIT#scene-info-source-followup-20261019 준수.

<a id="unity5-memory-source-partial-20261019"></a>
### Unity_5 v9 정정 검증 / v10·타씬 원천 감사 부분 상태
SELECTED-RANGE:UNITY:20261019 및 SCENE-INFO-SOURCE:UNITY:20261019 유지. [기존 인계 결과](LAST_RAIL_Unity_2-D-20260913.md#unity5-memory-source-result-20261019), [기존 QA](../../QA/OperatorInformation-20261019/verification.json). 정보 LOADING/실패/재시도 UI 제거, Eve 기존 commit 통지 갱신; MemoryGreen09 21/21·MemoryPlayer04 19항목 PASS. 일반run 장비/실효과·Vitals/범위/스크롤/실입력 및 타씬 재사용은 미완료. 타씬 기존 조회·명령 경계 감사만 수행, 실제 요청수 개선 미측정. 원천 없는 부분만 의존대기이며 계정 재사용 구현은 승인된 다음 작업으로 유지. 부모 큐/후속 정의 변경 없음.

<a id="unity5-scene-reuse-partial-20261019"></a>
### SCENE-INFO-SOURCE Unity_5 후속 부분 결과
앞 unity5-memory-source-partial 이후 [기존 인계](LAST_RAIL_Unity_2-D-20260913.md#unity5-scene-reuse-result-20261019)로 갱신. 공유 계정3종 Ready/진행중요청 재사용을 재화/열차공유/편성계정/출격초기진입에 연결, 수동·거래·복구 force refresh 유지. 선택 성공 전체snapshot 재사용(효과적용과 구별). Selection29/Scene52/최종5 PASS; 실제Unity HTTP→응답fixture 부분 읽기 순서9→최초5/반복2 확인, 외부서버/전체작전측정 아님. 실제선택 HTTP/journal host 부재·일반run 장비/실효과/전체사이클·train/ultimate 무효화 잔여. 부모큐 계속 유지, 미완료를 삭제하지 않음.

### SCENE-INFO-SOURCE 승인 추가 — SCENE-INFO-MEMORY:UNITY:20261019
49#single-command-approved-audit-20261019 및 승인상세 #single-command-response-design-20261019 적용. 독립착수: 실제Player 기본상주/리소스메모리 기준선·endpoint별호출계측·기존Model수명개선. 기존Unity소스/QA소유,현재작업안전경계유지. 기본상주는bootstrap/로비/전투실로드구분,자산은texture/atlas/audio/mesh/animation/material/shader/RenderTexture등큰항목/중복·참조보유분류. used/reserved/프로세스/GPU중복합산0,디스크크기메모리오인0.
실제로공급된계약부터소비,신규Serverwire미완료부분만대기. Server검증/소비큐후출격1·선택확정1·claim1과정상후속GET0 실제연결검수. 동일기기/빌드/해상도/자산/시나리오·프로파일러조건기록,최초와warm5사이클·피크/retained/GC구분. 기존baseline없으면전수치조작0,현재만실측. 운영START/실경제0,승인된격리시험환경사용. 측정만으로누수단정/무관자산변경0. 원QA/인계근거와미검수범위기록후총괄답장,기존WAIT후속유지.

<a id="unity5-memory-baseline-result-20261019"></a>
### SCENE-INFO-MEMORY 현재 Player 부분 근거
기존 인계 LAST_RAIL_Unity_2-D-20260913.md#unity5-memory-baseline-result-20261019 및 QA/OperatorInformation-20261019/verification.json#playerMemoryBaseline. Build02 성공·Player01 exit0/14 checkpoints/cold1+warm5. 인증없는 격리 lobby/Eve, 복귀 used123.44→123.68MiB/reserved258.89MiB; assets395 고정·전체객체+20/cycle 원인미확정. 프로세스0반환 무효·그래픽추정별도·진단씬로드peak누락/계측비용/프레임조건·로그경고 한계 보존. 운영 전체사이클·OS/deep참조·선택claim peaks 및 Server 검증신규wire 소비 미완료, 기존부모 유지/통합후속등록0. 신규승인 소비는 미승인으로 보류하지 않고 실제wire 공급부분만 대기.

<a id="unity5-memory-cause-result-20261019"></a>
### SCENE-INFO-MEMORY 객체증가 원인 수정 / texture 대조
기존 인계 LAST_RAIL_Unity_2-D-20260913.md#unity5-memory-cause-result-20261019 및 QA/OperatorInformation-20261019/verification.json#memoryCauseAudit. InputActionReference 매회20누적 타입/ID 확인→중복등록제거·owned10참조수명해제. 전후Player cold1+warm5 복귀120→0/전체객체3356고정, 관련3/3PASS. texture294개970.210MiB 카탈로그귀속·493참조공유/중복합산아님, 자산삭제압축0. 외부OS process/누락씬로드peak 보완; 실제프레임제한불일치/ComputeBuffer종료경고·전체운영/Serverwire소비 잔여. 부모 큐/WAIT 유지, 전수완료/통합후속등록0.

### 기존 SCENE-INFO-SOURCE 확장 — NODE-RESOLUTION:UNITY:20261019
45#node-resolution-route-approved-20261019 승인. 독립작업은기존결과/보관함/사건/분기Controller·Model 상태/콜백/수명대조와기공급원천소비. 미완료Serverwire추정0,완료소비등록후실계약결속. 보상수령대기·사건/효과해결전경로차단,성공응답으로상태갱신/후속GET0,분기시간과증강완전정지분리. 기존메모리원인조사보존·동일파일동시수정0. API/메모리·유실/중복·가득참/복합/보스종결실검수기존QA인계저장후답장. 새전체화면/독립복제원장0.

### SCENE-INFO-CONSUME:UNITY:20261019:RUN_CLAIM_V2 — 기존 SCENE-INFO-SOURCE 부분 소비
등록키 SCENE-INFO-CONSUME:UNITY:20261019:RUN_CLAIM_V2. 원49 승인/WAIT#scene-info-source-followup-20261019. Server#single-command-claim-result-20261019, QA/CommanderS1-20260922/run-complete-claim-20261019.json 47PASS 신규15, 원응답 예제 run-complete-claim-example-20261019.json. 기존 POST /v1/runs/:runId/reward-preview/:rewardPreviewId/claim 에 X-Run-Claim-Version: run.reward-claim.v2.20261019, 기존 Bearer/body runId/rewardPreviewId/Idempotency-Key. 성공 불변 원영수증과 owner snapshots로 기본claim 후 정상 GET 불필요. migration0122 DEV 미적용·외부URL 미검증, in-process SQL.js 검증만 완료. Unity 독립 소비 준비·fixture 검증은 착수 가능하며 외부 전체사이클 완료로 취급하지 않는다.
완료조건: 기존 Model/Controller/journal 재사용, account CURRENCIES/ITEMS/FUEL와 wallet BALANCES 범위만 원자수락, progress는 원STORY_PROGRESS mutation/hash별도비교; 낮은owner버전rollback0/중복0·동일progress버전hash충돌진단. 새응답 성공후 일반조회제거, legacy/v2원영수증없음409·유실/충돌/오류는 기존복구유지. 광고권리/별도claim변경0. 실제transport연결 및 동일조건 정상/복구호출·잔존/peak메모리는 따로실측,1535byte fixture를 RAM절감값으로표시0. 완료QA/인계 후 기존WAIT AND충족부분만Plan통합등록. 부모중복/타블록수정0,새화면작업/운영경제/배포0.

### SCENE-INFO-SOURCE 승인 확장 — FINISH-RESPONSE:UNITY:20261019
06#finish-result-response-approved-20261019 및앞상세. 기존결과Controller/View/복구원장대조·이미공급된결과원천소비독립진행. Server미검증wire임의추정0,공급완료후finish응답→표시·claim→원상태갱신·로비후속GET0 연결. 정상과재접속/유실/옛응답복구구분,광고UNKNOWN기본수령보존·미지원광고활성화0. 실HTTP/최종구간2명령목표와Player결과/메모리검수,기존원인조사근거재사용. QA/인계저장후결과답장.

<a id="unity5-node-intake-result-20261019"></a>
### NODE-RESOLUTION Unity_5 독립준비 부분결과
기존 QA/OperatorInformation-20261019/verification.json#nodeResolutionPreparation에6개소유경계·기공급/미공급 기록. 기존 AugmentSelectionController에현재owner참조/run/event와일치하는검증snapshot 최초수락 추가: 정상초기port조회0,리롤/선택각1·성공후조회0. 기존journal미결은원키복구조회유지,공급최신revision/확정floor보존. CONFIRMED를효과적용/경로허가로승격0.
Red18 missing-intake3실패→Green19 32PASS; reviewer발견복구회귀 Red20 1실패재현→수정후Final21 33/33PASS. 기존테스트중복합산0, 실제HTTP/전체Player/노드통합주장0. 변경Controller와기존tests만; View/게임판정복제0.
B route 원응답은완결보상/사건/효과/다음encounter부족,canStart/acceptedMapTransitionSupported false및필수refresh유지. 기존local분기시간/clone-save-publish,최종claim과중간보상분리,보관함hold미해결은원계약대기. Server검증wire/producer·production port/journalhost·부모완료/가득참/복합/보스실통합 미완료,부모큐유지/통합후속등록0. 메모리잔여보존. 새 FINISH-RESPONSE 승인은다음연속작업에서소비하며 앞제안상태기록은역사상태임.
### SCENE-INFO-SOURCE 확장 — CHOICE-EFFECT:UNITY:20261019
05#choice-reroll-effect-link-approved-20261019/앞상세승인. 기존Controller部分검증재사용,独立hold/effect owner/checkpoint수명대조·지원원천소비진행,Server검증wire후production연결. 원효과등록+적용ID보관뒤자기hold만해제,트리거실발동대기교착0·구탄snapshot변경0·중복스택0. 실행중리롤/SELECT경쟁·유실/저장실패/타hold·재진입·Player/API·메모리검수. 미공급wire추정0,QA인계후총괄답장.

### SCENE-INFO-CONSUME:UNITY:20261019:node-resolution-destination-v2 — 기존 노드 연결의 목적지 독립 소비
등록키 SCENE-INFO-CONSUME:UNITY:20261019:node-resolution-destination-v2. 선행45 승인/WAIT#node-resolution-followup-20261019 및#scene-info-source-followup-20261019, Server#node-destination-server-result-20261019. QA/CommanderS1-20260922/node-destination-regression-20261019.json143PASS, 최종직접회귀23PASS/type·lint0; node-destination-example-20261019.json은 실제auth Hono→SQL.js fixture이며 외부환경 검증 아님. 기존 POST /v1/runs/:runId/operation-map/destination + X-Map-Destination-Version: operation.map-destination.v2.20261019, Bearer/Idempotency-Key/body{expectedRevision,edgeId}. 성공은 같은원영수증의 목적지/원지도버전·hash와 진행revision, nextAction AWAIT_VERIFIED_ARRIVAL. 원wire 전체/오류·권한/호환은 Server인계 참조. 새migration 없음, 외부배포URL 미검증.
완료조건: 기존 clone→save→publish/명시확정/journal/Controller 재사용, 정상확정 뒤 목적지GET제거 및 version/hash/revision별수락. 과거응답rollback/중복이동0, 다른목적/같은키다른계약409·유실원키재생·저장실패이동0·레거시복구 유지. 선택된encounterProfileRef/rewardPlanRef는 원참조이며 canStart/실도착/전투/보상/사건완료 의미가 아니다. 원도착/실원천필수refresh·검증거래 제거0. 내부port를HTTP로집계0, 실연결 이후 외부정상/복구호출과Player수명·메모리 별도측정. 기본claim 소비와독립, 전체node/보류/finish 전체소비는 미공급범위만 유지. 부모중복/타블록수정·운영경제/배포0; AND충족후기존Plan통합후속.

### 기존 원복구·SCENE-INFO-SOURCE 확장 — RECOVERY-LINK:UNITY:20261019
04#interruption-reconnect-approved-20261019 승인. 원journal/checkpoint/recovery Controller·hold·계정/run수명 독립대조·기공급소비,미완료Server계약만대기. 읽기확인후명시계속/원키재시도,서버성공·로컬저장실패분리/중복스택·지급0/기지급옛상태롤백0. 실제앱중단/재시작·저장실패/만료/다른hold·로비재진입검수. 기존API/메모리계측재사용·fixture/production구분,QA인계후답장.

### 리소스 수명 승인 구현 — RESOURCE-LIFETIME:UNITY:20261019
04#combat-resource-lifecycle-approved-20261019 및앞상세. 기존메모리원인감사/입력객체수명수정재사용. 실제로더·카탈로그/참조scope소유부터명시하고승인최소변경: 공용/로비/run/node/cutscene/result/diagnostic 분리,필수자산준비/구세대취소/소유handle반환·shared보존/전환peak·결과복귀retained검증. 실제지원경로단위로구현,미지원producer부분만대기. 전자산재제작/삭제/일괄압축·프레임조건미확인성능주장0. 기존QA/인계에원본/빌드/기기/cold1warm5·실화면/누락·객체증가·API별도근거저장후총괄답장. 전체970MiB회수목표로품질손상0.

<a id="unity5-claim-boundary-result-20261019"></a>
### Unity5 claim-v2 경계 부분검증
기존인계#unity5-claim-boundary-result-20261019 및 QA/OperatorInformation-20261019/verification.json#completeClaimBoundary. 원영수증v2엄격검증/journal보존 Final27 36PASS; owner원자반영/버전·용량/transport/후속GET0 아직미연결. legacy·광고·기존조회유지. 부모/RUN_CLAIM_V2미완료유지·통합후속0. 신규destination-v2/CHOICE-EFFECT/RECOVERY-LINK/RESOURCE-LIFETIME 승인잔여는Unity6문서인계로계속.

### SCENE-INFO-SOURCE 단일출격 확장 — DEPARTURE-CHANGE:UNITY:20261019
49#departure-change-notice-approved-20261019 및앞상세승인. 독립準備:既준비View/Controller 의도snapshot·변경항목/버튼문구·중복입력·계정/시도세대·자산준비연결. Server검증wire소비후미적용확정과응답유실분리/조건재확인새키vs원키복구/정상1회·변경2회·추가GET0 검수. '미차감'은원명령미적용확정근거있을때만,미지원원천추정0. 기존QA/인계저장후총괄답장,원리소스/복구작업안전경계유지.

### 기존 S-02/노드보상 소비 확장 — CAPACITY-REWARD:UNITY:20261019
13#capacity-reward-ui-link-approved-20261019 승인. 기존장비모달/보상Controller에묶음ID·선택/scroll복귀/정상응답재사용·明示수령연결. 보유품버리기추가확인0/남은보상전체포기확인유지,30~54·장착제외·自動수령/삭제0. 기공급소비/독립준비진행,미검증Serverwire추정0. 部分수령/정리왕복/경합/최종/상인·API/Player메모리 검수후QA인계답장.

### 기존 장비/실전투 연결 — MAINTENANCE-APPLY:UNITY:20261019
13#maintenance-combat-apply-approved-20261019 승인. 기존stat/effect/HP/checkpoint/hold소유경계·지원원천대조/독립준비,Server검증공급후최종구성한번반영·보관·UI통지/재개. 원HP기준쌍/보호막·구탄/충전/CD·수락행동유지/중복스택0·실효과별예외확인,생성시상속과동적참조구분. 기공급부분검증재사용,미지원프로필완료승격0. API/Player/복구·메모리 QA인계후답장.

### 기존 결과·복구 독립 준비 — NONVICTORY-RESULT:UNITY:20261019
04#nonvictory-result-flow-approved-20261019 및 앞 상세 승인. 기존 전투/결과 Controller·journal·hold·자산 수명에서 사유별 표시/명시 포기 확인/중복입력·구세대 격리·결과 미확인 보존을 준비한다. 미공급 wire를 추정하지 않고 검증 공급 후 실소비. 서버 성공 전 로컬 장비 삭제 금지, 원기지급/필수복구 보존, 결과 GET 중복 제거는 충분한 응답 경계에서만. 실제 Player 재진입/중단·원키복구·종결 정리·API/메모리 근거를 기존 QA/인계에 남기고 총괄 답장. 기존 장착/claim 등 완료 단위 재작성 없이 안전 경계에서 진행.


### 명시 오류 producer·수명 독립 구현 — PRD-ERROR-REPORT:UNITY:20261019
15#production-client-error-reporting-approved-20261019 및 앞 상세. 기존 CurrentSessionErrorRuntime/Client·준비 실패 경로를 재사용하여 실제 기능별 누락 producer 조사와 기공급 계약 범위 연결·폭주/계정/수명 검증. 로컬 전역 큐를 새 로그인 계정으로 자동 업로드하지 않는다. 미공급 필드/원본문 내구 계약은 추정하지 말고 Server 검증 공급 후 소비. 필수 journal 보존·진단 실패 재귀0/추가 게임 상태 변경0, 기존 QA/인계 근거 후 총괄 답장. 운영 전송/새 SDK 제외.

### PRD-ERROR-CONSUME:UNITY:20261019:client-errors-v2
선행완료: LAST_RAIL_SERVER_2-D-20260913.md#prd-error-v2-server-result-20261019, 승인15#production-client-error-reporting-approved-20261019 / WAIT#prd-error-report-followup-20261019. Server 명시 version client-errors.v2.20261019 / contract.ts 고정 code-phase·전체 strict wire·응답/오류/원키복구 계약과 검증23PASS 공급. 정확 필드는 위 원본을 읽고 적용: events1..25/count1..60/body18000bytes, v2 key16..128 printable ASCII; 모든 nullable 필드 명시null. GET receipt 헤더만, 정상성공 뒤 추가GET0. duplicate accepted=true/acceptedEventCount0, 원본문 변경409·미확정503·429 Retry-After600·401 원계정복구 구분. 환경은 Server 배포값/별도DB, rawstack/message/path/응답/임의필드 금지. 기존 CurrentSessionErrorRuntime/Client producer를 연결하고 지원되는 준비·자원·상태·계약·복구·효과·원장 코드별 실제 발생지점과 미지원 표기. 원계정/세대/원키·본문·거래해시 보존, 다른거래 임의합산0, 기존 진단 실패가 gameplay/진단재귀를 일으키지 않게 검증. 전역 Unity capture queue 자동업로드 활성화 금지(TransportAvailable=false 유지). PRD-config 합성 Player→격리 ingest/DB/receipt·상한/세대/수명/호출근거를 기존 QA에 저장. migration0125 운영미적용, 운영배포/실사용자자료전송 권한 없음. 소비 완료 후 WAIT의 AND 조건으로만 Plan 통합 등록, 실제 PRD 전체수신 완료로 표현하지 않음.

### 증강 4종 개선·신규2종 독립 전투 구현 — AUGMENT-UTILITY:UNITY-INDEPENDENT:20261019
승인31#augment-utility-approved-20261019/앞상세§1–9. Unity runtime source/기존QA·인계소유,기존전투effect/HP/치유/보호/원행동·journal소유재사용. Server선행전확정된독립도메인과기공급원천만구현검증,미검증wire/ID/실보유발명0. T05접근등반취소,T16역할지원,O06잔여재배치,O08 Eve/Mira 수정·흡혈/생명역류·O02분배/파생차단. Eve O06 기본동작과차이없으면보류,부족능력을가짜완료처리0. HP실감소/과잉치유·1초상한/부모예산·동시성/다운/hold/복원·차폐/재바인딩을검증. Server확정계약후WAIT후속소비로실선택→실효과→Player검수연결. 정보는출발/선택응답메모리즉시사용,실표시MVC·독립자산/피격색만/DEV진단유지. QA/인계후총괄결과답장,구완료단위재작성/운영배포0.

### 증강 선택 효과·조합 설명 — AUGMENT-CHOICE-DISPLAY:UNITY:20261019
05#augment-choice-display-approved-20261019 및앞상세전체 승인. 기존선택MVC에 카드요약/상세·별도선택,slot별리롤후옛상세/입력무효,모바일스크롤/고정버튼,보유조합전후와손실·상한표시연결. O02/역류/O15/O06/O12·흡혈파생제외는31최신승인정의버전재사용. 기공급정의/메모리로독립표시가능,미공급Server신규ID/wire는 기존AUGMENT-UTILITY 후속검증후소비. 추천/확률가중·추가GET/중복모달0. 원CONFIRMED≠APPLIED/hold/checkpoint보존,View가실효과실행0. 실제Player의3카드/모바일입력·긴문구/리롤경합/정의버전·왕복API/구독수명검수;자산미확인은분리기록. 기존QA/인계에구현범위·증거·잔여저장후총괄답장. 후속은WAIT#augment-choice-display-followup-20261019. 신규전투기여표시 제안은범위아님.

### 증강 실발동·기여 확인 — AUGMENT-FEEDBACK:UNITY:20261019
승인05#augment-feedback-approved-20261019 및앞상세. 기존확정전투사건/통계Model·정보상세/표시를재사용해소형발동·이번노드실제기여표시구현. HP회복/HP피해/적보호피해·보호생성/흡수/자발소비/중단·해제분리,기여합산점수0. 기존승인효과의확정사건기공급범위/독립표시부터진행,미공급source는추정0·범위만대기. 부모/sequence/checkpoint중복가산0,복원미공급시부분기록표시·연출콜백으로HP측정0. 추가GET0/전체로그보관0/수명MVC·DEV진단/피격색만유지. Player배속/pause·다단/모바일가림/표시우선·창왕복/복원/API·잔류검수후기존QA인계기록·총괄답장. 정책MD편집0,새노드경계상세제안은범위아님.

### 기존 증강 전투 수명 확장 — AUGMENT-LIFECYCLE:UNITY-INDEPENDENT:20261019
31#augment-lifecycle-approved-20261019/앞상세승인. 기존effect/부모/탄·HP/치유/보호/hold/checkpoint 소유재사용,새원행동부터새조합·진행다단부모정의유지·소급발급0·흡혈상한다운복귀보존·노드간예약폐기·확정결과보존. 기존동적참조/카드별종료정책유지,HP충전노드값임의확정0. 기공급계약/독립도메인부터구현,미공급Serverwire만후속대기. 발사전후획득/분할치유중획득/고속T05·충전O06획득/다운복귀/hold/노드종료동시명중/복원·풀재사용을실전투/Player검수. 기존통과증거재사용,계약·QA/인계후총괄답장. 정책MD소유Plan,신규면역설계제안은포함0.

### 증강·적 저항 전투 연결 — AUGMENT-RESISTANCE:UNITY-INDEPENDENT:20261019
31#augment-resistance-approved-20261019/앞상세승인. 기존행동/HP/보호/상태·effect owner재사용,판정축분리·기승인저항·동일행동증강취소/지연1회·정상지원소비/면역실패·치유거절량제외구현검증. 미공급적profile/단계계약만대기·기공급/독립도메인진행,모든보스일괄면역/무효추가피해환산0. 무적/보호만/HP/동시제어·단계전환/원스킬중단창·복원/Player기여표시회귀. API0/상태메모리재사용,누락원천을면역표시로숨김0. 기존QA인계후총괄답장,WAIT후속조건준수. 신규통합시험안은아직제안이며이번승인과구분.

### 개선 증강 통합시험 선행 준비 — AUGMENT-TRIAL-PREP:UNITY:20261019
31#augment-integrated-trial-approved-20261019/앞전체시험안. 기존효과·선택/기여/수명/저항작업과검증재사용,지원원천별7장면·동일3시드·무증강/단독/핵심조합·고정입력vs자율운용·실측항목을기존QA에준비. 실제지원완료된범위만원Player시험,미완료장면은실행큐에미리실행항목등록않고WAIT정의로보존. 실제선택/보유는Server확정원천후,fixture결과와구분. API/상주·peak/누적할당/5회왕복후잔류/가독성측정,기여점수/임의너프0. QA인계후총괄답장,운영경제/배포없음.

### 후보 적격 표시·생명탄 지원 정정 — AUGMENT-ELIGIBILITY:UNITY-INDEPENDENT:20261019
05#augment-eligibility-approved-20261019/앞상세,31#overflow-healer-eligibility-correction-20261019 승인. 생명역류치료요원편성만조건,기본공격총구미보유이유로카드차단0. 생명탄독립발사/앵커연결실제원천확인·기존파이프라인재사용,정확profile미공급부분만대기/임의탄도발명0. 후보1~2/0·원hold/선택/리롤잔량·무효slot복구표시독립구현,Server확정wire뒤실소비. 후보강약추천/클라재추첨·누락정의정상0처리0. Player/원복구/API0·수명검수와기존QA인계후총괄답장. 다음치유source/생명탄profile세부제안은승인전값자동적용0.

### 치료 요원 생명 역류 운용 — OVERFLOW-HEALING:UNITY-INDEPENDENT:20261019
31#overflow-healing-operation-approved-20261019/앞상세승인. 기존원치유/부모/표적source결속,정식패시브·원틱상한공유/파생제외·부상우선/만피수동/AUTO추가조건0,치유앵커→공통상체앵커독립발사준비. 실제부모/앵커/AUTO원천확인후지원범위구현,미공급wire/profile만보류/가짜총구0. 기공급·독립작업진행,Player실발사/단일·광역·지속·복원검수/QA인계후총괄답장. 다음탄도수치미승인,기본공격원천을후보조건으로복원0.

### 즉시 치료·독립 생명탄 연결 — OVERFLOW-BALLISTICS:UNITY-INDEPENDENT:20261019
31#overflow-projectile-approved-20261019/앞탄도상세승인. 기존원HP/치유확정후초과분만독립FriendlyProjectileOwner로연결,치료는적명중대기없음/MISS·차폐·요격시치료롤백0. 36m/s·80mm·24m·step시간/거리절단·예약적1/비유도·치유/상체앵커·미라profile분리·파생proc0. 기공급/독립도메인진행,Server정식profile검증뒤실소비. 실앵커/기본공격없는힐러/6·12·24m/좌우적·객차/배속·복원·Player검수. QA인계후총괄답장,기존완료fixture再作成0。新흡혈상세제안은미포함。

### 기존 흡혈 최신 정정 — LIFESTEAL-UNCAPPED:UNITY:20261019
31#lifesteal-uncapped-correction-20261019.흡혈12%무상한·원천제한삭제를기존도메인/표시/시험에반영. rolling1초8%예산과최대HP변경예산폐기,요원정식귀속피해전체·추가탄/드론/지속/생명탄포함. 동일사건중복/귀속공유복제0·흡혈초과O02/역류재전환0,기본치유/HP/다운규칙유지. 구checkpoint/진행run호환보존·Server검증version소비,임의전역구원장삭제0. 기공급독립구현진행/실귀속경로·Player검수,QA인계후총괄답장.

### 무상한 흡혈 원피해 귀속 — LIFESTEAL-ATTRIBUTION:UNITY-INDEPENDENT:20261019
31#lifesteal-attribution-approved-20261019/앞상세승인. 기존원행동/탄/드론/지속틱/HP사건·논리요원ID에귀속결속,소환물피해본인회복/생명탄치유자회복/원부모자식이중집계0. 설치후새독립공격vs진행원부모정의구분,다운/슬롯교체/동시HP/복원/기여UI검수. 12%무상한·초과재전환0유지,기공급독립구현후Server검증wire실소비。QA인계/Player근거후총괄답장。새피해→회복원자상세제안은승인전과구분。

### 실제 흡혈 회복·checkpoint·Player — LIFESTEAL-RESOLUTION:UNITY-INDEPENDENT:20261019
31#lifesteal-runtime-resolution-approved-20261019/앞전체승인. 기존확정피해projection/실보유·source/세대·치유modifier 연결→원Vitals실회복と원journal/checkpoint의원자·복원/중복0 구현. UIでHP加算/新원장0. 기공급/독립원천과자기담당승인producer부터계속구현,Server미공급wire만대기. 실제1요원원경로1000→120/10000→1200·다운/치유금지/결손·HP저장실패/ACK유실/복원순서/실Player기여검수후다른원천확장. QA인계후총괄답장,전체완료확대0/정책MD편집0.

### 회복 증강 문구 최신화 — HEALING-AUGMENT-COPY:UNITY:20261019
05#healing-augment-copy-approved-20261019/앞상세전체승인. 기존AUGMENT-CHOICE-DISPLAY/FEEDBACK표시소유재사용해흡혈/O02/O15/역류요약·조합·예시정정,정의버전동기/모바일긴문구/무상한vs부모상한·즉시치료vs적명중구분검수. 구직접본체·초당8%·생명탄흡혈불가현재문구폐기/구run보존,추가GET0/독립텍스트. 기존LIFESTEAL-RESOLUTION은실보유proof·host연결/회복·저장/복원/Player까지계속진행. 자기담당선행구현·외부미공급구분。QA인계/총괄답장。새결과상세提案미포함。

### 노드 결과 증강 효과 확인 — AUGMENT-NODE-RESULT:UNITY:20261019
05#augment-node-result-approved-20261019 및앞전체상세승인. 기존결과/보유상세/기여Model재사용,보조진입/미보유숨김/읽기전용·실단위분리/0vs기록없음·부분범위/최종sequence·노드수명. 집계미공급은표시독립부분진행·실소비만대기,가짜0/전작전누적/추천/보상변경0. 추가GET0·모바일입력/보상·다음경로독립/복원/잔류검수. 기존LIFESTEAL-RESOLUTION 실authority/host·HP/복원/Player도계속,독립표시완료로실연결대체0. QA인계후총괄답장,후속WAIT#augment-node-result-followup-20261019.

### 치유 보정·전환 계산 연결 — HEALING-MODIFIER:UNITY-INDEPENDENT:20261019
31#healing-modifier-approved-20261019/앞상세승인. 기존원치유/흡혈계산단일원천에modifier종류/수혜·시전자단계/최종정수화·거절E0·O06이전예산/제2수혜보정결속. 이미수정된조기버림회귀재사용,전역곱연산/새상한0. 기공급독립구현진행·검증계약뒤실소비,실LIFESTEAL-RESOLUTION authority/host/HP/save/복원/Player계속。QA인계후총괄답장。다음보호막계층상세는미승인。

### 보호막 owner·피격·소비 연결 — SHIELD-LAYER:UNITY-INDEPENDENT:20261019
31#shield-layer-approved-20261019/앞전체승인. 기존layer owner재사용·공유/개인·상한/재적용유지,원순서優先/미정개인만료순·O15실소비원자/흡수·소비·만료·이전기여분리·복원/Player. 기공급독립구현진행、Server검증후실소비. 실제LIFESTEAL-RESOLUTION는Server인계#lifesteal-resolution-server-result-20261019의 combat.o22-application-original.v1.20261019를기존독립작업에서수신검증/host설치→APPLIED·원행동/node세대·Vitals/save결속으로진전. runtimeSupported=false를근거없이true로우회하지않고실지원선행을구현. QA인계후총괄답장。


### 보호막 이전·수용 한도 — SHIELD-TRANSFER:UNITY-INDEPENDENT:20261019
31#shield-transfer-approved-20261019 및 앞 상세 승인. 기존 보호owner의 부분이전/원자 차감·추가/원출처·만료/적격 O15/HP변경·복원 및 표시 독립 구현·검수. 원정의 필요부분만 대기, Server 확정 버전 후 실소비. 실제 LIFESTEAL-RESOLUTION selection host receiver/store 설치→APPLIED/세대·원행동 proof→Vitals/save/restore/Player 계속. 공개SELECT는 Server#augment-public-select-shield-server-result-20261019에서 공급됨; 운영issuer 잔여와 구분. 검증 후 QA/인계·총괄답장, WAIT#shield-transfer-followup-20261019 수용 기준 준수. 동시판정 신규제안 구현0.



### 동시 피격·흡혈·다운 — COMBAT-RECOVERY-ORDER:UNITY-INDEPENDENT:20261019
31#combat-same-step-recovery-approved-20261019/앞 상세 승인. 원전투sequence·피해/흡혈 완결·다운/기존탄존속·보호원자 처리, 늦은 회복표현/세대·복원 검수. 기존원owner/기공급계약으로 독립 구현 가능, 실선행 소비는 검증버전 확인. 실제 LIFESTEAL-RESOLUTION 정식 selection host/journal·definition 설치/APPLIED·원행동치유proof→HP/save/restore/Player 계속. fixture/조합port≠실host. QA인계·WAIT#combat-recovery-order-followup-20261019 후속/Plan_7 결과답장. 신규 다중타격 제안 미구현.



### 다중 대상·다단 흡혈 — LIFESTEAL-MULTIHIT:UNITY-INDEPENDENT:20261019
31#lifesteal-multihit-approved-20261019 및 앞 상세 승인. 원피해사건별 회복/광역·틱·관통·보조탄·드론 귀속·부모중복0·과잉피해·정수화·표시합산/복원 독립구현검수, 실제계약 공급후 실소비. 실제 LIFESTEAL-RESOLUTION host설치/APPLIED·원action proof→Vitals/save/restore/Player 계속. 기존 검증 재사용, 실경로/작은다단 절삭 증거 기록. QA/인계 후 WAIT#lifesteal-multihit-followup-20261019·Plan_7 답장. 새 lifecycle 상세는 미승인.



### 요원 수명과 지연 피해 — LIFESTEAL-ACTOR-LIFECYCLE:UNITY-INDEPENDENT:20261019
31#lifesteal-actor-lifecycle-approved-20261019 및 앞 상세 승인. 기존 owner의 교체/다운복귀·지연탄/드론·논리세대·풀재사용/바인딩수명·저장복원 독립구현검수. 실제지원경로와 fixture 구분, 새교체기능0. 실제 LIFESTEAL-RESOLUTION host설치/APPLIED·원행동치유proof→HP/save/restore/Player 계속. QA인계 후 WAIT#lifesteal-actor-lifecycle-followup-20261019 후속/Plan_7 결과답장. 신규 종료상세 미적용.



### 종료 직전 흡혈·결과 확정 — COMBAT-TERMINAL-HEALING:UNITY-INDEPENDENT:20261019
31#combat-terminal-healing-approved-20261019 및 앞 상세 승인. 원종료owner에 마지막 피해/회복 완결·최종HP/기여snapshot·취소탄/지연표현·저장복구·Player 결과 연결 검수. 기존 계약/독립 구현 착수, 미공급만 대기. 실제 LIFESTEAL-RESOLUTION host/APPLIED·원action proof→HP/save/restore/Player 계속, 부분조합을 종단으로 승격0. QA/인계 후 WAIT#combat-terminal-healing-followup-20261019·Plan_7 답장. 신규 Player 완료보고 상세 미승인이나 기존 실검수 중단0.



### 실제 흡혈 첫 연결·Player 수용 — LIFESTEAL-PLAYER:UNITY-INDEPENDENT:20261019
31#lifesteal-player-acceptance-approved-20261019 전체 승인. 기존 LIFESTEAL-RESOLUTION 남은 실제 node화면host/원definition/APPLIED·피해치유proof→Vitals/save/restore/Player 호출 연결을 진행. 기공급계약·독립 설치 작업은 착수, 실제발급선행 미공급 부분만 대기. no-effect LOCAL/합성authority를 실제지원으로 승격0. 첫실지원1경로부터 동일원사건으로 선택/회복/복원/종료 증명후 원천확장. 구상한/생명탄제외 기대값 사용0. 유효QA 재사용, QA인계 후 WAIT#lifesteal-player-followup-20261019·Plan_7 답장.



### LIFESTEAL-PLAYER 기존 항목 실행 보강 — 2026-10-19 사용자 직접 지시
조사만 하지 말고 실제 연결 구현까지 계속. 확인된 B-native CommitResolvedDamage→CombatVitals/BCombatLocalModel의 기존 DAMAGE/SHIELD journal에 정식 APPLIED·원피해/회복 staging/replay/저장 경계를 책임분리하여 구현하고 실제 node CreatePersistent/definition/화면host에 연결. 기존 Ce03LocalDeparture START는 실제원본으로 보존하되 고정configuration hash에 O22 단순주입0, no-effect 프로필 우회0. 자기소유 논리세대/action/치유proof producer를 구현하며 외부공급만 정확필드로 남긴다. 각 단위 수정파일·호출설치·관찰검증/잔여 기록, 합성PASS 반복/재조사만으로 완료처리0. 체감비교 승인31#lifesteal-gameplay-comparison-approved-20261019, 실행은 WAIT 선행후.



### 생명 역류 시전 대상 변화 — OVERFLOW-CAST-TARGET:UNITY-INDEPENDENT:20261019
31#overflow-cast-target-transition-approved-20261019 및 앞 상세 승인. 원치유caller 적용시실결손/대상변화·원스킬취소/재탐색·별도적표적/발사불가저축0·치료유지/안내·Player 독립구현검수. 신규공통AUTO/환급0. 기존 LIFESTEAL-PLAYER 실제 B-native APPLIED/치유journal·원nodehost 설치 구현계속, 합성검사/재조사만으로종료0. QA인계 후 WAIT#overflow-cast-target-followup-20261019·Plan_7 답장. 다음틱예산차감 상세미승인.



### 광역·지속 치료 생명탄 예산 — OVERFLOW-PULSE-BUDGET:UNITY-INDEPENDENT:20261019
31#overflow-pulse-budget-approved-20261019 및 앞 상세 승인. 원치유/부모예산owner→틱합산생명탄·배정시차감/적없음실패환급0·상한후원치료/보호유지·복원/표시·Player 독립구현검수. 계약공급후실소비, 미공급만대기. LIFESTEAL-PLAYER B-native APPLIED/치유journal·실nodehost 호출설치계속, 단위시험반복/재조사만종료0. QA인계 후 WAIT#overflow-pulse-budget-followup-20261019·Plan_7 답장. 새복수힐러상세미적용.



### 복수 치료 요원 귀속 — OVERFLOW-MULTI-HEALER:UNITY-INDEPENDENT:20261020
31#overflow-multiple-healers-approved-20261020 및 앞 상세 승인. 기존치유/탄/예산owner의 시전자·부모격리/원순서현재결손·흡혈/기여·복원·표시검수 독립구현. 실힐러2명미지원이면fixture와Player미실행구분. 실제 LIFESTEAL-PLAYER B-native APPLIED/회복journal·실nodehost 설치우선계속,조사종료0. QA인계 후 WAIT#overflow-multi-healer-followup-20261020·Plan_7 답장. 신규O02병용상세미적용.



### 생명 역류·O02 병용 분배 — OVERFLOW-SHIELD-SPLIT:UNITY-INDEPENDENT:20261020
31#overflow-shield-split-approved-20261020 및 앞상세승인. 원치유/분배owner→O02/탄 각한도·미사용교차보충0·O15소비후재분배0·표시복원 독립구현검수. LIFESTEAL-PLAYER 이미연결한BDirectAttackSession/APPLIED/route.ApplyO22 재사용, 실제Server원본소비 scene/bootstrap→BRouteAugmentScreen host진입·Player/저장복원까지우선계속. 합성입력을실발급으로승격0. QA인계·WAIT#overflow-shield-split-followup-20261020·Plan_7답장. 신규선택설명상세미적용.



### 선택 화면 병용 효과 설명 — OVERFLOW-CHOICE-COPY:UNITY:20261020
05#overflow-combination-choice-copy-approved-20261020 및 앞 상세승인. 기존 카드/조합상세의보유단독→병용계수변화·효과추가·중립표시/접힌예시·한도/확정응답후갱신/추가GET0 구현·모바일Player검수. 원정의version재사용/미공급값발명0. LIFESTEAL-PLAYER 기존실연결경로→정식원본/scene host→Player/복원검수우선계속. QA인계 후 WAIT#overflow-choice-copy-followup-20261020·Plan_7답장. 새결과상세제안 미적용.



### 전투 결과 병용 효과 기여 — OVERFLOW-RESULT-COPY:UNITY:20261020
05#overflow-combination-result-approved-20261020 및 앞상세승인. 기존결과/기여Model·View 실HP피해/실흡수/실회복·소비/만료·중간취득전후/미기록구분/GET0 구현·모바일Player검수. 원기록누락을추정0으로채우지않고미공급범위만명시. 실제 LIFESTEAL-PLAYER 정식Server원본→scene/host/Player·복원/종료우선계속. QA인계 후 WAIT#overflow-result-copy-followup-20261020·Plan_7답장. 신규추가획득상세미적용.



### 추가 획득 적용 경계 — AUGMENT-ACQUISITION-TRANSITION:UNITY-INDEPENDENT:20261020
31#overflow-acquisition-transition-approved-20261020 및 앞상세승인. 실제APPLIED/원부모version·잔여한도/탄·보호/지속틱·구run복원/Player 독립구현검수. 기존LIFESTEAL-PLAYER v8정식소비/v7호환·scene/bootstrap→host→Player/저장복원 우선계속,flag우회0. QA인계 후 WAIT#augment-acquisition-transition-followup-20261020·Plan_7답장. 신규재개UI미적용.



### 선택 적용·재개 및 Player — AUGMENT-APPLY-RESUME:UNITY-INDEPENDENT:20261020
05#augment-apply-resume-approved-20261020 및 앞상세승인. 원효과/checkpoint완료후자기hold해제/다른hold·배속보존·중복선택/복구/간결안내 구현검수. 기완료v7/v8소비/ARRIVE→선택→native회복파일복원증거재사용,실운영startup명령→host·정식원본·Player/모바일우선계속. 새기능때문에실연결중단0/합성PASS를실Player승격0. QA인계후 WAIT#augment-apply-resume-followup-20261020·Plan_7답장.



### 진행 작전 저장·증강 호환 — AUGMENT-VERSION-RESUME:UNITY-INDEPENDENT:20261020
05#augment-version-resume-approved-20261020 및 앞 상세 승인. 원 v7 진행 저장·v8 지원 소비, 필요 시 동등 의미 원자 변환, 사실에 맞는 업데이트/복구 안내, 원키 중복 복구 방지 독립 구현검수. 기존 v7/v8 시험 근거 재사용하고 실제 startup→host→정식 원본→흡혈→저장복원 Player/모바일 검수를 우선 계속한다. 합성 ARRIVE/TestCrypto 시험을 실제 운영 Player 완료로 승격하지 않는다. 근거는 기존 QA/인계, WAIT#augment-version-resume-followup-20261020 후속 등록 후 완료 블록 정리·Plan_7 결과 답장. 다음 화면 분기안은 미승인.



### 재접속 화면 복귀 — AUGMENT-RECONNECT-ROUTING:UNITY-INDEPENDENT:20261020
05#augment-reconnect-screen-routing-approved-20261020 승인. 기존 Model/Controller의 검증 원상태로 선택/원키복구/효과저장/전투/결과 복귀 구현. 원후보/리롤·HP·보상 중복0·다른 hold/배속·View 세대 격리. 실제 startup/흡혈/저장복원 Player 검수 우선 계속, 합성 시험과 분리. 확정된 독립 구현만 착수, 서버 계약 의존 소비는 공급 후 등록. 기존 QA/인계·WAIT#augment-reconnect-routing-followup-20261020·Plan_7 결과 답장.

### 재접속 원상태 계약 소비 — AUGMENT-RECONNECT-ROUTING:UNITY-CONSUME:20261020:combat.augment-reconnect-state.v1.20261020
선행 Server 검증 완료: [정확 lifecycle additive DTO·상태/권위/원키/scope/version·24PASS·type/lint·잔여](LAST_RAIL_SERVER_2-D-20260913.md#augment-reconnect-routing-server-result-20261020), QA/CommanderS1-20260922/augment-reconnect-routing-verified-20261020.json. 기존 GET /v1/runs/:runId/lifecycle의 augmentRecovery만 소비; 유효 원선택/보유/복구 Model에 충분한 응답이 있으면 화면분기용 추가 GET0. SELECTING 후보·리롤 원본 재사용, CONFIRMED_RUNTIME_CHECKPOINT_REQUIRED 원영수증/applicationOriginal을 기존 설치·checkpoint 소유자에 전달, local runtimeApplied=null/NOT_OBSERVED를 미적용 확정으로 해석하지 않음. 원키 pending 복구가 SELECTING/NO_CURRENT_GRANT 관측보다 우선, NODE_EXITED의 map.phase/holdIds·기존 terminal finish/reward/nextAction로 기존 화면 복귀. 자기hold만 실제저장후해제, 다른hold/배속 보존·late callback/scope/version/화면세대 격리. UNKNOWN/MODE_NOT_APPLICABLE은 기존 해당모드 복구 사용, REVIEW_REQUIRED에서 임의 전투/선택 재개0. 완료조건 원계약 HTTP/Model 연결·강제종료 위치별 복구·중복효과/소비0·실제 Player/모바일 증거. Server 실제geometry/rail/독립accepted producer 및v7 역사원본 잔여는 별도부모 유지; 이 계약이 START/실Player 지원을 열지 않음. 공통 원문 D:/Project/LASTRAIL/AGENTS.md 데이터 재사용 절. WAIT#augment-reconnect-routing-followup-20261020 준수, 실제 양측Player 근거후 Plan통합 등록.

### 실제 전장 원본·도착→흡혈 Player 후속 — 2026-10-20
사용자 명시 연결 구현 지속. #unity6-apply-resume-routing-20261020의 hold/복원 시험 재사용. 기존 LIFESTEAL-PLAYER 부모에서 실제 geometry/native composition 원소유/직렬화·비전투 AUGMENT와 후속 전투 생명주기·운영 startup→host→실제Player 연결을 소유범위 구현검수. Server독립 accepted도착/원시간 계약 준비 후 실제소비, fixture승격/임의지원 우회0. 남은 구체계약은 기존인계에 남기고 확정된 호출부는 계속 구현. 새 이벤트 UI는 선행완료후 소비, 현재 흡혈우선 유지.


### 실제 운영 장면 측정 결속→도착/흡혈 Player — 2026-10-20 사용자 지시
#unity6-scene-measurement-result-20261020 측정owner/4PASS 재사용. 기존LIFESTEAL-PLAYER에서 실제운영scene/prefab frame/collider/anchor·승인단위/rail/roof/topology를 찾아 명시참조 결속, 원본없음/정책미결/자기미구현 구분. 기존계약에따라 실제자기소유provider/startup·AUGMENT→후속전투·host연결 구현. Server physical composition/도착권위 계약 소비후 실제흡혈Player검수. 원천자료미공급을시험치수로대체0, 측정코드완료를Player완료로승격0. 기존QA/인계·Plan_7답장.

### 신규 전장 저작 초안 소비 — PHYSICAL-AUTHORING:UNITY-CONSUME:20261020:b.physical-authoring.v1.20261020
사용자 직접 확인후없으면제작 요청으로 생산완료된 좁은 후속. [제작명세/파일/실제Unity·cross-wire검수/잔여](LAST_RAIL_SERVER_2-D-20260913.md#physical-authoring-created-server-result-20261020), Unity 인계 #server-created-physical-authoring-20261020. 필독 제작명세 #physical-authoring-production-spec-20261020부터 읽고 시작. 기존 LIFESTEAL-PLAYER/physical-wire-clock에 합쳐 신규 Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoringV01의 실제측정원본/명시단위를 native geometry/scene startup소비로 연결·검수. 원template와account배치instance를 분리, 원scene/reference·단위/rail/roof/topology/CAS·version/원clock 결속. 초안치수를과거승인/최종운영수용으로승격0; 속도/stepTicks/진행node거리·독립원clock 원본 미공급은 발명0. 실제소비/실행/Player근거 후자기완료, 부분은기존부모유지. 운영START/경제/배포0. 원문 D:/Project/LASTRAIL/AGENTS.md 기존정보재사용규칙.

### 컷신 준비·실패·연속 표시 — U-ULT-READY-SEQUENCE-01
2026-10-20 Unity_7 독립 표시/자산준비/세대·수명/View 제어 구현·최종194 67PASS(exit0). [근거/실공급 미결](LAST_RAIL_Unity_2-D-20260913.md#unity7-ready-sequence-work-20261020), verification.json.ultimateReadySequence. native 원행동·DAILY/hold·실자산/Player 미공급/미검수만 잔여; 전체 AND 미충족으로 본큐/U-FIVE 부모 유지·후속 선등록0.
등록키 ULT-READY-SEQUENCE:UNITY:20261020:ultimate.ready-sequence.v1.20261020. 사용자 승인 원본18#ultimate-ready-sequence-approved-20261020와 두 상세절,04#cinematic-input-recovery. Plan 정책 선행완료로 기존 U-FIVE-ULT-PRESENTATION-01에 합쳐 독립 구현: 기존 로더/연출owner·모드/DAILY 재사용, 실편성 요약/제한예열·원사건순직렬·중간tick0·현재편스킵 입력세대·다음요원/요약중첩·미준비대체·실패/스킵/종료 멱등·자기hold/참조수명·DEV진단. 원효과·비용 재명령0/정상게임API추가0. 실제흡혈Player 우선 유지, 같은 소유 파일 동시편집 금지. 닉스/이브 최종자산 미공급 범위만 WAIT#ultimate-ready-sequence-followup-20261020에 대기, 대체자산 시험을 최종Player 통합완료로 보고0. 기존 QA/인계 근거→후속등록→자기완료 정리, 나머지 부모큐 삭제0.

<a id="unity6-physical-local-player-progress-20261020"></a>
PHYSICAL-AUTHORING:UNITY-CONSUME:20261020:b.physical-authoring.v1.20261020 / LIFESTEAL-PLAYER 진행근거: [LOCAL Player179 첫실행/별도프로세스 복원exit0·17513PASS·정확운영잔여](LAST_RAIL_Unity_2-D-20260913.md#unity6-measured-acquisition-player-result-20261020). 실제 원계정/운영rail·도착권위·production startup/Android 잔여로 부모와 소비 큐 유지. 다음 승인 독립 단위는 위 U-ULT-READY-SEQUENCE-01을 기존 궁극기 owner에 통합하며 미승인 음향안 제외.

### 컷신 모바일 배치·AV 조합 교체 — U-ULT-MOBILE-AV-01
Unity_8 2026-10-20: 불변AV선택/기존대여·보호영역배치/포인터계약 독립구현 검증. [203 27PASS+201 무변경56, 중복제외83 / 정확미결](LAST_RAIL_Unity_2-D-20260913.md#unity8-audio-work-20261020). 최종자산/원caller/실Player AND 미충족으로 큐유지·후속선등록0.

등록키 ULT-MOBILE-AV:UNITY:20261020:ultimate.mobile-av.v1.20261020. 사용자 승인18#ultimate-mobile-av-approved-20261020 및 연결두상세. 기존 U-FIVE-ULT-PRESENTATION/U-ULT-READY-SEQUENCE owner에 통합, 신규병렬관리자/중복구현0. 선행Plan정책완료로 독립 구현: 기존safearea/입력Controller의 포인터세대·표시시간유지·스킵복귀누출차단, 기존manifest/로더의 영상/cue/음원조합 고정·지원검증·실패대체·대기/재생중교체금지·참조수명. 지원기기/실ID 추정0, 미승인믹싱안 제외. 최종 장면보호영역/조합자산은 WAIT#ultimate-mobile-av-followup-20261020 선행준비후소비, 시험fixture로최종완료0. 흡혈Player 현재안전단위 보존. 관련기존QA/검증·후속등록·자기완료만정리.
### 궁극기 연출 강도 — U-ULT-REDUCED-01
Unity_8 2026-10-20: 설정/검증프로필선택/중도줄이기안전종료 독립구현 검증. [203 27PASS+201 무변경56, 중복제외83 / 정확미결](LAST_RAIL_Unity_2-D-20260913.md#unity8-audio-work-20261020). 최종자산/원caller/실Player AND 미충족으로 큐유지·후속선등록0.

등록키 ULT-REDUCED:UNITY:20261020.18#ultimate-reduced-presentation-approved-20261020/연결상세 승인. 기존연출/설정owner에서 기본/줄이기 선택·DAILY독립·같은시간축 프로필선택·중도줄이기 지원전환 또는 안전종료·프로필미준비대체·원효과/입력/hold/설정수명 구현검수. 앞ready-sequence/mobile-av와 통합, 미승인전체믹싱 별도. Cutscene 승인프로필 미공급은 해당최종자산만대기, 독립소비부터 진행. 기존QA/후속/완료정리 준수, 흡혈Player 우선 유지.
### 궁극기 BGM 전환 — U-ULT-THEME-MUSIC-01
Unity_8 2026-10-20: 단일현재곡/표준임시테마·설정/소유audio hold 독립구현 검증. [203 27PASS+201 무변경56, 중복제외83 / 정확미결](LAST_RAIL_Unity_2-D-20260913.md#unity8-audio-work-20261020). 최종자산/원caller/실Player AND 미충족으로 큐유지·후속선등록0.

등록키 ULT-THEME-MUSIC:UNITY:20261020.18#ultimate-theme-music-approved-20261020/연결상세 정책선행완료. 기존 연출/음악/설정owner에 독립구현: 표준전용테마·요약전투곡유지, BGM/SFX분리·미준비전투곡유지·현재곡복귀·연속/스킵/오류/메뉴/백그라운드 수명. 사용자설정롤백0·원효과지연0·재생중늦은테마삽입0. 기존컷신부모통합/흡혈Player 안전단위 유지. 최종음악·시각동기화 선행미완료는 해당자산소비만 대기, 단위시험을실청취Player로승격0. QA/후속등록/완료정리 기존규칙.
### 전투 상황별 음악 — U-BATTLE-MUSIC-STATE-01
Unity_8 2026-10-20: B LOCAL 확정상태소비·현재곡/동일곡/결과누락 독립구현 검증. [203 27PASS+201 무변경56, 중복제외83 / 정확미결](LAST_RAIL_Unity_2-D-20260913.md#unity8-audio-work-20261020). 최종자산/원caller/실Player AND 미충족으로 큐유지·후속선등록0.

등록키 BATTLE-MUSIC-STATE:UNITY:20261020.18#battle-music-state-approved-20261020/연결상세 승인. 기존음악/전투lifecycle/설정owner 실제경로 확인→원상태곡선택·같은상태재시작0·위기원천미공급분기차단·궁극기종료현재곡·세대/설정/메뉴/결과수명 독립구현검수. 기존 U-ULT-THEME-MUSIC과통합/중복owner0. 미지원곡/보스상태/위기임계추정0·신규API0. 실제흡혈Player 안전단위 유지, 기존QA/후속/완료정리. 최종음악소비/실청취Player는선행완료후.
### 전투 효과음 중첩 — U-COMBAT-SFX-PRIORITY-01
Unity_8 2026-10-20: 지원결과/위치·공개/원세대 수명 독립구현 208 43PASS(신규13), 실제LOCAL 원receipt불변 확인. [구현·정확미결](LAST_RAIL_Unity_2-D-20260913.md#unity8-support-position-work-20261020). 실제원위치/공개owner·승인mapping/최종음원/실Player AND 미충족, 큐유지·후속선등록0.
Unity_8 2026-10-20: 원사건순서/정확그룹·공급voice우선순위/수명 독립구현 검증. [203 27PASS+201 무변경56, 중복제외83 / 정확미결](LAST_RAIL_Unity_2-D-20260913.md#unity8-audio-work-20261020). 최종자산/원caller/실Player AND 미충족으로 큐유지·후속선등록0.

등록키 COMBAT-SFX-PRIORITY:UNITY:20261020.18#combat-sfx-priority-approved-20261020/연결상세 승인. 기존원전투사건/오디오owner에서 우선순위·중복/원발사와명중분리·반복표현·루프/세대·SFX설정수명 독립구현검수. 원판정/수치/전투사건 생략0, 원천미공급을HP차추정0. 기존음향/전투부모통합·흡혈Player 안전단위 유지, 임의상한/새경고정의0. 실제음원/청취 미완료는분리, QA/후속/완료정리 기존규칙.
### 명중·흡수·면역 음향 — U-COMBAT-HIT-AUDIO-01
Unity_8 2026-10-20: 지원결과/위치·공개/원세대 수명 독립구현 208 43PASS(신규13), 실제LOCAL 원receipt불변 확인. [구현·정확미결](LAST_RAIL_Unity_2-D-20260913.md#unity8-support-position-work-20261020). 실제원위치/공개owner·승인mapping/최종음원/실Player AND 미충족, 큐유지·후속선등록0.
등록키 COMBAT-HIT-AUDIO:UNITY:20261020.18#combat-hit-outcome-audio-approved-20261020/연결상세 및44기존표시승인 소비. Unity_7은 기존확정결과/오디오owner에 HP손실/전량부분흡수/실면역/비명중/내림0/원치명 대응 독립구현검수. HP차역산/새효과/중복타격음0·기존SFX우선순위와통합·원결과불변. 관련QA/원천미공급·최종음원/청취Player분리, 부모유지/선행완료후후속정리.
### 지원 결과·위치 음향 — U-SUPPORT-POSITION-AUDIO-01
Unity_8 2026-10-20: 지원결과/위치·공개/원세대 수명 독립구현 208 43PASS(신규13), 실제LOCAL 원receipt불변 확인. [구현·정확미결](LAST_RAIL_Unity_2-D-20260913.md#unity8-support-position-work-20261020). 실제원위치/공개owner·승인mapping/최종음원/실Player AND 미충족, 큐유지·후속선등록0.
등록키 SUPPORT-POSITION-AUDIO:UNITY:20261020.18#support-position-audio-approved-20261020/연결두상세 정책선행완료. Unity_8 기존지원결과/사건위치/공개범위·카메라/오디오owner로 독립구현검수. 실제회복/초과·정화예방/다운지원구분, 발사와접촉원위치·단발고정/지속소유자·세대·모노/무음/중요신호, 원판정불변. 미공급위치/결과·거리수치추정0·기존음향부모중복구현0. 실제음원/Player 선행미완료만분리, QA/후속/완료정리 기존규칙.
### 첫 음향 실제 연결·통합 시험 — U-AUDIO-FIRST-TRIAL-01
Unity_8 2026-10-21: 215 실제 LOCAL C접촉/임시흡혈 재생·ON/OFF/매핑누락 원결과동일·복구무재생 확인. 정상 프로세스 종료는 timeout으로 미통과. [범위·근거·잔여](LAST_RAIL_Unity_2-D-20260913.md#unity8-first-audio-trial-20261021), QA/FiveAgentVisual-20261017/verification.json.firstAudioPlayerTrial. 3관련큐 유지·전체완료/후속선등록0.

등록키 AUDIO-FIRST-TRIAL:UNITY:20261021.18#combat-audio-production-trial-approved-20261021 및 두상세 승인. Unity_8 기존음향부모의 실제scene audioPlayer/매핑·원사건→재생소비를 확인해 자기미구현은 구현, 준비된실제경로 AU01부터 검수. 208 독립근거재사용, 새로운검증은실연결/변경/실패범위에집중. 미승인후보는정식발행0, 필요개발용임시음은임시시험임을명시. 지원/궁극기/Android 선행없는행은미실행과정확원천기록, 독립PASS로전체Player승격0. 각기기청취/메모리/API·원결과동일성 증거는실행한범위만기록. 관련QA/WAIT 후속정리, 기존부모삭제0.
### U-AUDIO-FIRST-TRIAL-01 보완: 승인 매핑·누락 정책
Unity_8 2026-10-21: 215 실제 LOCAL C접촉/임시흡혈 재생·ON/OFF/매핑누락 원결과동일·복구무재생 확인. 정상 프로세스 종료는 timeout으로 미통과. [범위·근거·잔여](LAST_RAIL_Unity_2-D-20260913.md#unity8-first-audio-trial-20261021), QA/FiveAgentVisual-20261017/verification.json.firstAudioPlayerTrial. 3관련큐 유지·전체완료/후속선등록0.

등록키 AUDIO-BINDING:UNITY:20261021. 기존 AUDIO-FIRST-TRIAL 작업에 결속하며 별도 재생기/매핑 원장 중복 구현 금지. 18#combat-audio-binding-approved-20261021 / combat.audio-binding.v1.20261021 적용. 실제 scene audioPlayer/카탈로그/원사건 연결은 자기 구현 범위에서 계속 진행한다. 첫 시험 실제 무기·발사 원천·확정 접촉/방호 재질의 원문 경로/지원 범위를 기존 Unity QA에 공급하여 Audio의 대표음 제작 의존성을 해소한다. 미확정 재질 발명 금지. Audio QA/battle-audio-supply-v01.json의 production_trial_supply 최신 승인 BGM/C 후보 상태 및 정확 hash 참조. 후보 검토용과 정식 매핑 분리. AU01 실제 경로와 사용 가능한 AU 사례의 누락/무음/중복/종료 검수까지 진행, 미공급 원천만 보류. 실제 소스 재생/녹음과 사람 청취·장치 검수를 분리 기록. 완료 근거는 기존 QA/인계에 보존, 부모 전체 완료/미충족 후속 등록 금지. 다음 배속·정지 제안은 미승인으로 구현 범위에서 제외한다.
### 첫 음향 시험 보완: 배속·정지 승인 / 승인 C 실제 소비
Unity_8 2026-10-21: 215 실제 LOCAL C접촉/임시흡혈 재생·ON/OFF/매핑누락 원결과동일·복구무재생 확인. 정상 프로세스 종료는 timeout으로 미통과. [범위·근거·잔여](LAST_RAIL_Unity_2-D-20260913.md#unity8-first-audio-trial-20261021), QA/FiveAgentVisual-20261017/verification.json.firstAudioPlayerTrial. 3관련큐 유지·전체완료/후속선등록0.

등록키 AUDIO-SPEED-PAUSE:UNITY:20261021. 18#combat-audio-speed-pause-approved-20261021 및 상세절 적용, 기존 U-AUDIO-FIRST-TRIAL-01과 단일 구현으로 결속. 이전 큐의 C후보/배속미승인 문구는 최신 승인으로 대체한다. Audio manifest의 승인 C 단독 정확 hash/버전을 실제 명중 원사건에 연결하고 실제 Player 재생/중복·종료·실지원 배속/정지/복귀까지 진행. C를 발사/흡혈/방호음으로 임의 전용하지 않는다. 미공급 발사/재질은 구분하고 준비된 명중 경로를 막지 않는다. 기존 소유자 시계/hold/설정·데이터 재사용, 새 중복 카탈로그/추가 게임조회 금지. 원사건→실source/녹음→사람청취/기기 결과를 구분해 기존 QA에 기록. 다음 음원로딩 신규제안은 미승인 제외. 부모 전체 및 미충족 후속 완료처리 금지.
### 증강 선택→결과 통합 잔여 연결 — U-AUGMENT-E2E-01
등록키 AUGMENT-E2E:UNITY:20261021. 05#augment-end-to-end-flow-approved-20261021 / augment.end-to-end.v1.20261021. 기존 Model/Controller/선택 port/전투효과/checkpoint/결과 View를 재사용해 자기 독립 잔여 연결 구현. 실제 지원 흡혈 AE01 우선, 서버 실제 계약 공급 전에는 독립 구현/복구검증만 진행하고 fixture를 양측 통합으로 승격하지 않음. 준비된 실제 서버 경로→선택 화면→적용 저장→첫 공격→결과 화면을 Player 검수, AE02–08 지원범위 확장. 215는 실제 선택부터 전체 결과 검증이 아니며 정상 프로세스 종료 timeout도 미해결로 구분·자기 범위 원인 확인. 컷씬/음향/애니메이션 작업은 사용자 별도 진행이므로 이번 새 지시에서 제외. 추가 GET/이중효과/원부모 소급변경 금지. 기존 QA에 실제 경로·원receipt/결과·미공급만 기록, 전체부모/후속 선등록 없음. 새 카드 입력 보완 제안은 미승인 제외.
### AUGMENT-E2E 보완: 카드 오선택 방지
등록키 AUGMENT-CHOICE-INPUT:UNITY:20261021. 05#augment-choice-input-revision-approved-20261021 / augment.choice-input.v1.20261021 승인. 기존 큐의 카드입력미승인 제외 문구는 이 승인으로 대체. 누름중 후보교체 취소·리롤터치 새카드 관통방지·표시/선택 payload 일치·교체 상세 스크롤/다른슬롯 보존 구현 및 실제 Player 검수. 원Model/Controller 사용, 해제/재바인딩·구세대 응답 격리. AE02/03와 증강 실제 선택→적용→전투→결과 검수 계속. 컷씬/음원/애니메이션 제작 제외. 신규 적용대상 표시안 미승인 제외. 실제 공급되지 않은 양측 구간은 미실행으로 보존, 부모전체완료/후속선등록 금지.
### AUGMENT-E2E 보완: 적용 대상·발동 조건
등록키 AUGMENT-TARGET:UNITY:20261021. 05#augment-target-readout-approved-20261021 승인, 앞 큐의 대상표시미승인 문구 대체. 기존 카드/상세 Model/Controller로 원범위·현재편성 대상·조건·조합 표시와 리롤 revision 일관 갱신 구현. 근거없는 대상 없음/예상DPS/일시다운 제외 금지. 새 조회/원화제작 없이 기존 메모리/초상 재사용. 실제 E2E 원서버 SELECT→실적용/저장→전투→결과 Player 검수 지속, 표시대상과 원판정 일치 검수. 미지원 실제 원천만 분리, 신규 보유목록 탐색안 제외. 컷씬/음원/애니메이션 제작 제외.
### AUGMENT-E2E 보완: 보유 목록·조합 탐색
등록키 AUGMENT-OWNED:UNITY:20261021. 05#augment-owned-navigation-approved-20261021 승인, 앞큐의 해당미승인 문구 대체. 기존 작전정보/보유 Model·상세 Controller 재사용, 확정보유만·기존순서·실조합이동/스크롤복원·유한이력·노드기여/구독수명 구현. 열기/왕복추가GET0. targetReadout 6종 원범위/조건 공급과 targets=null 미공급은 구분. 실제 SELECT→APPLIED/checkpoint→전투→결과 Player 검수 계속 우선, 새 미디어 제작 제외. 신규 정비표시안은 미승인 제외. 미지원 실제 경로는 별도 기록.
### AUGMENT-E2E 보완: 정비 후 대상·조합 갱신
등록키 AUGMENT-MAINTENANCE:UNITY:20261021. 05#augment-maintenance-readout-approved-20261021 승인, 이전 미승인 문구 대체. 기존 정비/효과/checkpoint/표시 owner로 최종구성 일치·미리보기분리·과거기여보존·응답유실복구 구현/검수. 실제 지원 정비 AE05/07 및 첫 선택→적용→전투→결과 Player 연결 계속 우선. 새교체권한/미디어/별도상세GET 금지. 신규 결과조건 안내안 미승인 제외, 공급없는 실제경로만 분리.

### 데이터 재사용·수명 정리 및 DEV 조회/메모리 계측
등록키 DATA-LIFECYCLE:UNITY:20261021. 49#data-lifecycle-approved-20261021 / data.lifecycle.v1.20261021 사용자 승인. 기존 API/메모리 작업에 결속하여 변경 전 baseline부터 확보. Story 단일owner·화면진입 재사용/동시요청합침·세션 캐시수명·완료저널 인계·UI rebuild/복사 비용/구독·참조정리 실제구현. 응답완결 소비는 Server 확정 공급부분만 연결, 나머지 독립 구현은 계속. DEV 서버에서만 조회/수신/재사용/무효화/참조해제 기록·유한버퍼·모바일 복사 가능한 기존 진단 확장. 참조해제와 실제 GC/texture회수 구분. 로그인부터 5회 화면왕복·명령·작전·보상·로그아웃 동일조건 실제 Player에서 API 논리/전송/재시도와 기본상주·managed/할당/peak/잔류·texture CPU/GPU 전후검수. 미지원 실제행은 미실행, 합성PASS로 대체0. 기존QA/인계·WAIT#data-lifecycle-followup-20261021 후속규칙. 컷씬/음원/애니메이션 제외; 기존 증강/흡혈 실제 연결 잔여 유지. Plan_7에 부분/전체 검증근거 답장.


### DATA-LIFECYCLE 보완: 갱신 화면·버전 충돌
등록키 DATA-REFRESH-CONFLICT:UNITY:20261021. 49#data-refresh-conflict-approved-20261021 / data.refresh-conflict.v1.20261021 승인. 기존 DATA-LIFECYCLE 작업에 결속하여 owner별 버전 수락/부분갱신·명시삭제·동등버전 충돌·원키복구와 기존화면/선택/스크롤 보존을 자기역할 실제코드에 반영. Server는 실제계약/충돌·복구원천 공급, Unity는 원Model/Controller의 원자게시·영향입력만잠금·메모리재사용/계정격리 구현. 이미 승인된 전투 정보창 조회UI 제외 유지. 위6수용사례와 API/메모리 전후 실제검수 계속, baseline 선확보. 조사만으로 종료0, 기존QA/인계 근거·잔여 후 Plan_7 결과답장. WAIT 기존 data-lifecycle 후속AND/고정키 사용, 중복 통합큐0. 신규 날짜경계/복귀안 미승인 제외, 미디어 제작 제외.


### DATA-LIFECYCLE 보완: 날짜·기간·앱 복귀
등록키 DATA-TIME-RESUME:UNITY:20261021. 49#data-time-resume-approved-20261021 / data.time-resume.v1.20261021 승인, 이전 관련 미승인 표기 대체. Server는 원서버기준일/기간/권리·버전/원키복구 계약 공급, Unity는 관련데이터만 무효화·현재화면 필요조회/요청합침·복귀순서/기존화면유지 구현. 전체조회/로그인보상 반복0, 참여종료와수령마감 분리, 진행run 원정책유지. 기존 실제API·메모리 전후검수에 경계/시계변경/유실/반복복귀 수용사례 결속. baseline 먼저, 미실행 정확표기, 조사만으로 종료0. 기존QA/인계 후 Plan_7 결과답장, WAIT data-lifecycle 후속키 재사용. 새 저메모리 캐시정리안은 미승인 제외. 미디어 제작 제외.


### DATA-LIFECYCLE 보완: 메모리 압박·재진입
등록키 DATA-CACHE-PRESSURE:UNITY:20261021. 49#data-cache-pressure-approved-20261021 / data.cache-pressure.v1.20261021 사용자 승인. 기존 owner/리소스수명에 단계캐시정리·공유자산 마지막사용자 반환·선택/스크롤복원·수명세대격리 구현. 전투필수/미확정원명령/복구원본 보존, 임의공통MB/디스크삭제0. 유효owner정보→로컬자산→필요범위API 복원, 화면해제만으로 GET0. DEV서버 전용 유한정리기록, 실제API·메모리 baseline/5회왕복/정리후지연/잔류 검수까지 계속. 합성압박과 실제기기신호 구분, 조사만으로 종료0. 기존QA/인계 근거후 Plan_7 답장. WAIT data-lifecycle 후속조건/키 재사용. 신규 계정전환안 미승인, 미디어제작 제외.


### DATA-LIFECYCLE 보완: 계정 종료·전환
등록키 DATA-ACCOUNT-EXIT:UNITY:20261021. 49#data-account-exit-approved-20261021 / data.account-exit.v1.20261021 승인. 원요청안전저장/새입력차단·계정캐시/구독해제·구응답격리·영속인증종료경계 실제구현 및검수. Server 원인증/계정별원영수증 계약, Unity 수명/화면/실소비 담당. 게스트삭제/병합0, 원복구보존, 토큰갱신과계정전환 구분. Unity는 기존 #data-lifecycle-time-server-supply-20261021 실제응답 연결 후 실제Player API/메모리 전후까지 계속. baseline/지원범위/미실행 구분, memory-only시험 재시작영속복구 주장0. 기존DATA-LIFECYCLE 부모/WAIT 후속키 결속. 기존QA/인계 근거 후 Plan_7 답장. 새 로컬저장실패 UX안 미승인, 미디어제작 제외.


### DATA-LIFECYCLE 보완: 저장 실패
등록키 DATA-SAVE-FAILURE:UNITY:20261021. 49#data-save-failure-approved-20261021 / data.save-failure.v1.20261021 사용자승인. Server는 원영수증/원키 재생·충돌/기한 계약과 필요한구현, Unity는 전송전영속실패 차단·서버성공후 로컬재저장/화면/관련명령제한·단일재시도 구현. 이전관련미승인표기 대체. 실제서버응답 소비→Player API/메모리 전후검수까지 계속, 합성/memory-only 재시작한계 유지. 기존QA/인계 근거·잔여후 Plan_7 답장. 기존DATA-LIFECYCLE 부모/WAIT후속키 결속, 새 네트워크재시도안 미승인·미디어제작 제외.


### DATA-LIFECYCLE 보완: 네트워크 복구 자동1회
등록키 DATA-NETWORK-RETRY:UNITY:20261021. 49#data-network-retry-approved-20261021 / data.network-retry.v1.20261021 승인. Server 기존오류/재생·기한/복구 계약, Unity 요청owner/자동1회예산·자동수동합침/기존화면유지·해제수명 구현. 같은논리요청 연결신호/재진입/상하위재시도중첩으로 예산초기화0, 원키원payload 유지. 실제서버응답소비→Player API/메모리 전후검수 지속, baseline/합성/미실행구분. 기존QA/인계 후 Plan_7 결과답장, 기존DATA-LIFECYCLE/WAIT후속키 결속. 새 다중화면명령 상세안 미승인·미디어제작 제외.


### DATA-LIFECYCLE 보완: 다중 화면 명령
등록키 DATA-MULTIVIEW-COMMAND:UNITY:20261021. 49#data-multiview-command-approved-20261021 / data.multiview-command.v1.20261021 승인, 이전미승인표기 대체. Server 기존대상identity/원권리·멱등/다른키경합 계약검증·필요구현, Unity 기존명령owner 공유/단일입력경로·진행상태/연출중복방지·구독수명 구현. 정상반복구매와중복입력 구분, 새명령자동실행0/미지원병렬0. 실제서버응답연결→Player API·메모리 전후검수 계속, baseline/미실행구분. 기존QA/인계후 Plan_7 답장, 기존DATA-LIFECYCLE/WAIT키 결속. 신규목록갱신안 미승인·미디어제작 제외.


### DATA-LIFECYCLE 보완: 목록 갱신 연속성
등록키 DATA-LIST-CONTINUITY:UNITY:20261021. 49#data-list-continuity-approved-20261021 / data.list-continuity.v1.20261021 승인. Server 기존ID/부분목록·cursor/version/삭제 계약공급·필요수정, Unity ID선택·항목기준스크롤/상세유지·pointer대상검사·구query응답격리/행재사용수명 구현. 미공급삭제추정0/새전체조회남발0. 실제서버응답소비→Player API/메모리 전후검수 지속, baseline/미실행 분리. 기존QA/인계후 Plan_7 답장, DATA-LIFECYCLE/WAIT 기존후속키. 이전목록안 미승인표기 대체. 신규상세창 실행조건변경안 미승인, 미디어제작 제외.


### DATA-LIFECYCLE 보완: 실행 조건 변경
등록키 DATA-COMMAND-CONDITIONS:UNITY:20261021. 49#data-command-conditions-approved-20261021 / data.command-conditions.v1.20261021 승인. Server 원조건version/권위검증·원자미실행/변경응답 공급, Unity 전후조건표시·사용자재확정/원키복구구분 구현. 미실행확정후 새조건새명령, 불명확은원키, 임의가격/재료전환0. 충분응답후GET0. 실제서버응답연결→Player API/메모리검수 지속, baseline/미실행분리. 기존QA/인계후 Plan_7 답장, DATA-LIFECYCLE/WAIT후속키 재사용. 앞실행조건미승인표기 대체, 신규일괄받기부분결과안 미승인·미디어제작 제외.


### DATA-LIFECYCLE 보완: 일괄 받기 부분결과
등록키 DATA-BULK-RESULT:UNITY:20261021. 49#data-bulk-result-approved-20261021 / data.bulk-result.v1.20261021 승인. 기존일괄원자성/권리유지, 원대상고정·항목별완료/이미지급/미지급/불명확/대상외구분, 누락추정0·합계중복0. Server 원응답/원키·개별일괄경합, Unity 표시/복구/원목록저장·구독수명 구현. 충분응답후GET0. 실제서버응답연결→Player API/메모리전후 검수계속, baseline/미실행분리. 기존QA/인계후 Plan_7 답장, 기존DATA-LIFECYCLE/WAIT키 결속. 앞일괄안 미승인표기 대체, 신규재화표시안 미승인·미디어제작 제외.


### DATA-LIFECYCLE 보완: 재화 표시
등록키 DATA-CURRENCY-DISPLAY:UNITY:20261021. 49#data-currency-display-approved-20261021 / data.currency-display.v1.20261021 승인. Server 확정잔액/버전·원영수증 공급, Unity 보유/미수령/불명확 분리·단일owner/연출callback가산0·구연출역행0 구현검수. 실제예약계약없이임의보류잔액0, 충분응답후GET0. 기존서버응답연결→Player API메모리전후 계속, baseline/미실행분리. 기존QA/인계후Plan_7답장, DATA-LIFECYCLE/WAIT기존키. 앞재화안미승인표기대체, 신규콘텐츠버전안미승인·미디어제작제외.


### DATA-LIFECYCLE 보완: 데이터·리소스 버전
등록키 DATA-CONTENT-MISMATCH:UNITY:20261021. 49#data-content-mismatch-approved-20261021 / data.content-mismatch.v1.20261021 승인. 02의기존admission/release pin/다운로드동의유지. Server 정의version/호환·입장계약공급, Unity 아이콘대체와규칙불일치차단구분/필요자산준비합침·수명구현. 아이콘없음 계정GET0, 다른ID대체/진행run효과덮어쓰기0. 실제서버응답소비→Player API메모리전후 지속, baseline/미실행분리. 기존QA/인계후Plan_7답장, DATA-LIFECYCLE/WAIT기존키. 앞관련미승인표기대체, 새화면전환세부안 미승인·미디어제작제외.


### DATA-LIFECYCLE 보완: 빠른 전환·메모리 증가 원인
등록키 DATA-FAST-NAVIGATION:UNITY:20261021. 49#data-fast-navigation-approved-20261021 / data.fast-navigation.v1.20261021 승인. 화면진입세대/owner유효조회공유·행ID/리소스version 바인딩검사·종료멱등/공유사용권반환 실제구현. 기존SOURCE실응답연결과 Player API메모리검수계속. unity10 저장후/해제후증가를 동일조건 재현, retained원참조/일시할당/GC·계측비용 분리후 최소수정/전후검수. 실측미지원분리·증가은폐0/임의상시GC0. 기존QA/인계후Plan_7답장, DATA-LIFECYCLE/WAIT기존키. 이전빠른전환미승인표기대체, 신규DEV비교화면안 미승인·미디어제작제외.


### DATA-LIFECYCLE 보완: DEV 비교 결과 화면
등록키 DEV-DATA-COMPARISON:UNITY:20261021. 49#dev-data-comparison-approved-20261021 / dev.data-comparison.v1.20261021 승인. 기존DEV서버전용진단에 전후조건/논리API·전송/복구·managed/texture/잔류/지연·증가근거/미측정·판정 표시, 모바일요약/선택복사·내보내기 구현. 유한기록/버퍼수명·민감값제외, PRD0. 현재실제응답소비·증가원인조사·Player전후 검수 우선계속, 새UI를그선행대기로삼지않음. 기존QA/인계후Plan_7답장, 기존DATA-LIFECYCLE/WAIT키. 앞DEV화면미승인표기대체, 신규출격준비상태안 미승인.


### DATA-LIFECYCLE 보완: 출격 준비·복구
등록키 DATA-DEPARTURE-READINESS:UNITY:20261021. 49#data-departure-readiness-approved-20261021 / data.departure-readiness.v1.20261021 승인. Server 원START/확정구성·원영수증/권한계약, Unity 기존메모리/승인자산준비·선택세대·필수준비후확정·같은run복구 구현. 새preview조회/중복START/자동환불0. 실제서버응답연결/메모리증가원인확인→Player API메모리전후검수 계속, baseline/실원천미지원 구분. 기존QA/인계후Plan_7답장/WAIT기존키. 앞출격안미승인표기대체, 신규종료복귀해제안미승인·미디어제작제외.


### DATA-LIFECYCLE 보완: 종료·로비 복귀
등록키 DATA-LOBBY-RETURN:UNITY:20261021. 49#data-lobby-return-approved-20261021 / data.lobby-return.v1.20261021 승인. Server 원finish/claim·원영수증/복구·응답완결 공급, Unity 결과필수상태안전분리/owner반영/종료전투수명정리·로비표시재시도 구현. 중간노드작전보존/미수령권리보존/중복명령·이중해제0, 충분응답후로비GET0. 실제서버응답연결/메모리증가원인확인→Player API메모리전후 계속, baseline/미실행분리. 기존QA/인계후Plan_7답장/WAIT기존키. 앞종료안미승인표기대체, 신규다음run분리안미승인·미디어제작제외.


### DATA-LIFECYCLE 보완: 다음 작전 상태 분리
등록키 DATA-RUN-ISOLATION:UNITY:20261021. 49#data-run-isolation-approved-20261021 / data.run-isolation.v1.20261021 승인. Server 원run귀속/START·계정version/영수증 검증·필요공급, Unity 원상태보존/새확정owner전환·구run callback격리/pool lease초기화 구현. 임시run·구효과자동승계0. 실제서버응답연결·증가원인·Player API메모리전후 지속, baseline/미실행분리. 기존QA/인계후Plan_7답장/WAIT기존키. 앞run분리미승인표기대체, 신규저장대조복원안미승인·미디어제작제외.


### DATA-LIFECYCLE 보완: 저장·서버 대조 복원
등록키 DATA-RESTORE-RECONCILE:UNITY:20261021. 49#data-restore-reconcile-approved-20261021 / data.restore-reconcile.v1.20261021 승인. Server 원account/run/START·checkpoint/영수증·현재상태계약 공급, Unity 원자료대조/미적용완료/검증후재개·원키복구/수명 구현. mtime채택/NOT_FOUND새출격/부분복원전투/임의초기화0. 실제응답연결·메모리증가원인·Player API메모리전후 지속, 새run저장수정 실검수를이전formation자료로대체0. 기존QA/인계후Plan_7답장/WAIT기존키. 앞복원미승인표기대체, 신규복원취소안미승인·미디어제작제외.


### DATA-LIFECYCLE 보완: 복원 취소·재중단
등록키 DATA-RESTORE-INTERRUPT:UNITY:20261021. 49#data-restore-interrupt-approved-20261021 / data.restore-interrupt.v1.20261021 승인. Server 원명령/귀속/영수증 복구 계약·부족 구현, Unity 화면 취소와 저장·거래 수명 분리/단일 복원 소유자·재진입 구현. 원기록 보존/중복 효과·소비·지급0, 공유 사용권 준수. 실제 응답 연결·증가 원인 조사·Player API/메모리 전후 우선 지속; 기존 시험을 새 변경 실검수로 대체하지 않는다. 기존 담당 QA/인계에 근거와 미검증 기록 후 Plan_7 결과 답장. 기존 WAIT AND/후속키 유지, 신규 첫 입력·재개 상세안 미승인. 앞 복원취소 미승인 표기 대체, 미디어 제작 제외.


### DATA-LIFECYCLE 보완: 복원 후 첫 입력·전투 재개
등록키 DATA-RESTORE-RESUME:UNITY:20261021. 49#data-restore-resume-approved-20261021 / data.restore-resume.v1.20261021 승인. Server lifecycle/확정단계·시간/복구 계약 확인·부족 구현, Unity 정지 전장·전투 계속/선택·결과 분기/새입력 수락·전투 시간 재개 담당. 준비 시간 전투 가산/터치 관통/중복 재개0. 실제 응답 연결·메모리 증가 원인·Player API/메모리 전후 검수 우선 계속. 충분 응답 재조회0, 실제 원천·합성SQL·영속재시작 미지원 구분. 기존 담당 QA/인계에 증거 후 Plan_7 결과 답장. 기존 WAIT AND/후속키 유지. 앞 첫 입력 미승인 표기 대체; 새 대기 중 만료·종료 전환안 미승인, 미디어 제외.


### DATA-LIFECYCLE 보완: 복원 대기 만료·종료
등록키 DATA-RESTORE-TERMINAL:UNITY:20261021. 49#data-restore-terminal-approved-20261021 / data.restore-terminal.v1.20261021 승인. Server 실제 작전 만료 원천/lifecycle/원키 계약 확인·부족 구현, Unity 재개 제한/확정 결과 전환·늦은 응답 격리·준비 수명 담당. reward.expiresAt을 작전 만료로 오용 금지, 기존 충분 상태 재사용/폴링0. 실제 서버 응답 연결·메모리 증가 원인·Player API/메모리 전후 우선 계속, 이전 부분 검수로 전체 완료 대체 금지. QA/담당 인계 기록 후 Plan_7 결과 답장. 기존 WAIT AND/완료 기반 후속키 유지. 앞 만료안 미승인 표기 대체, 신규 실패 안내 상세안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 복원 실패 안내·복구 선택지
등록키 DATA-RESTORE-RECOVERY-UI:UNITY:20261021. 49#data-restore-recovery-ui-approved-20261021 / data.restore-recovery-ui.v1.20261021 승인. Server 기존 오류·복구/문의 계약 확인·부족 구현, Unity 확인된 실패 단계별 표시·기존 재시도 연결/로비 이동·수명 담당. 저장 재시도 HTTP0/불명확 원키 유지·연타 병합/DEV 상세 진단·PRD 기존 오류전송 보존. 실제 서버 응답 연결·메모리 증가 원인·Player API/메모리 전후 우선 계속. 기존 담당 QA/인계 근거 후 Plan_7 결과 답장. 기존 WAIT AND/완료 기반 후속키 유지. 앞 실패 안내 미승인 표기 대체, 신규 로비 이어가기안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 로비 중단 작전 이어가기
등록키 DATA-LOBBY-RESUME:UNITY:20261021. 49#data-lobby-resume-approved-20261021 / data.lobby-resume.v1.20261021 승인. Server 작전 발견·계정 귀속/기존 입장·복구 계약 확인 및 부족 구현, Unity 기존 출격 영역 표시/동일 복원 Controller 연결/수명 담당. 유효 정보 재사용·로비 왕복 GET0/새START·연료 재소비0/원기록 보존. 실제 서버 응답 연결·메모리 증가 원인·Player API/메모리 전후 우선 계속. QA/담당 인계 증거 후 Plan_7 결과 답장, 기존 WAIT AND/후속키 유지. 앞 로비안 미승인 표기 대체. 신규 편성·장비 변경 표시안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 이어가기 편성·장비 기준
등록키 DATA-RESUME-CONFIGURATION:UNITY:20261021. 49#data-resume-configuration-approved-20261021 / data.resume-configuration.v1.20261021 승인. Server 원START·정비 확정/버전/복구 계약 공급·부족 구현, Unity 원run 구성/현재계정 표시 분리·정비 불명확 원키 복구·준비세대 수명 담당. 기존 항목별 적용시점 보존/충분 응답 추가GET0. 실제 서버 응답 연결·증가 원인·Player API/메모리 전후 우선 계속, 새 UI를 핵심 연결의 선행 대기로 삼지 않음. QA/인계 근거 후 Plan_7 결과 답장, 기존 WAIT AND/후속키 유지. 앞 편성안 미승인 표기 대체. 신규 복원 전투 정보 표시 일치안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 복원 전투 정보 표시 일치
등록키 DATA-RESTORED-INFO:UNITY:20261021. 49#data-restored-info-approved-20261021 / data.restored-info.v1.20261021 승인. Server 원run/정비·증강·부분응답 계약 공급·부족 구현, Unity 실제 전투 owner 정보 표시/상세·세트·조건 일관 갱신·수명 담당. 열닫기GET0/로비값 혼입0/누락0변환 금지. 실제 서버 응답 연결·메모리 증가 원인·Player API/메모리 전후 우선 지속. 기존 QA/인계 근거 후 Plan_7 결과 답장, WAIT AND/후속키 유지. 앞 표시 일치안 미승인 표기 대체. 신규 열린 정보창 갱신 세부안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 열린 정보창 갱신
등록키 DATA-OPEN-INFO-REFRESH:UNITY:20261021. 49#data-open-info-refresh-approved-20261021 / data.open-info-refresh.v1.20261021 승인. Server 원ID/부분응답·명시삭제·버전 계약 공급·부족 구현, Unity 상세 대상/scroll 유지·해제/삭제 분기·owner 통지/입력 경계/수명 담당. 표시갱신 명령0/불필요GET0. 실제 응답 연결·증가 원인·Player API/메모리 전후 우선 계속. ClaimComplete owner·앱 영속 인계 등 핵심 연결을 신규 UI 대기로 미루지 않음. QA/인계 근거 후 Plan_7 결과 답장, WAIT AND/후속키 유지. 앞 열린창안 미승인 표기 대체. 신규 보상 후 여러 화면 갱신안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 보상 수령 후 표시 갱신
등록키 DATA-CLAIM-DISPLAY:UNITY:20261021. 49#data-claim-display-approved-20261021 / data.claim-display.v1.20261021 승인. Server claim 완료/owner별 버전·부분응답 계약 공급·부족 구현, Unity ClaimComplete 실제 owner 소비/결과·로비·보관함/저장·수명 연결 담당. 지급과 연출분리·과거영수증 잔액rollback0·충분응답GET0. 실제 서버 연결·증가 원인·Player API/메모리 전후 우선 지속. 충분한 실제 owner 반영 검증 전 기존 fallback GET만 삭제하지 않음. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 보상표시안 미승인 표기 대체. 신규 후속 소비 순서안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 보상 직후 후속 명령
등록키 DATA-POST-CLAIM-COMMAND:UNITY:20261021. 49#data-post-claim-command-approved-20261021 / data.post-claim-command.v1.20261021 승인. Server 권한/원키·owner버전 계약 및 부족 구현, Unity 지급확정·상태반영·필수저장 후 관련소비 허용/자동예약입력0·독립작업보존/수명 담당. 실제 응답 연결·메모리 증가 원인·Player API/메모리 전후 우선 계속. 기존 ClaimComplete owner·앱 영속 연결 검증 우선, 충분 응답 추가GET0. QA/인계 근거 후 Plan_7 답장, 기존 WAIT AND/후속키 유지. 앞 후속소비 미승인 표기 대체. 신규 획득 아이템 찾기안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 획득 아이템 찾기
등록키 DATA-REWARD-ITEM-LOCATE:UNITY:20261021. 49#data-reward-item-locate-approved-20261021 / data.reward-item-locate.v1.20261021 승인. Server 지급원ID/귀속·조회계약 확인·부족 구현, Unity 정확 대상 연결/필터 명시해제·현재상태/수명 담당. 유효정보재사용/동명대체0/부분목록누락삭제추정0. 실제 응답·증가원인·Player API/메모리 전후 우선 지속, ClaimComplete owner/영속연결 우선. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 아이템찾기 미승인 표기 대체. 신규 왕복 위치안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 결과·보관함 왕복 위치
등록키 DATA-RESULT-INVENTORY-RETURN:UNITY:20261022. 49#data-result-inventory-return-approved-20261022 / data.result-inventory-return.v1.20261022 승인. Server 원영수증/최신owner 계약·부족 구현, Unity 결과ID/항목/scroll 최소복귀정보·계정격리/화면수명 담당. 왕복GET/재지급0·현재경제상태유지. 사용자 우선지시: ClaimComplete account/wallet/progress 실제반영·archive 소비와 원응답문자열 생존/메모리 증가 원인 조사→최소수정→Player API/메모리 동일조건 전후 계속. 필수원자료보존/약참조만누수단정0/QA계측원인구분. 기존 QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 왕복안 미승인 표기 대체, 신규 재실행 영수증 정리안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 재실행 영수증 반영·정리
등록키 DATA-RECEIPT-RESTART-APPLY:UNITY:20261022. 49#data-receipt-restart-apply-approved-20261022 / data.receipt-restart-apply.v1.20261022 승인. Server 원영수증/owner version·복구계약 공급·부족 구현, Unity archive 소비/ClaimComplete owner별 반영·영속저장/참조해제 담당. 부분반영재종료·동일영수증·더최신상태 안전, 충분원본 재조회0. 실제반영·원문문자열 retained원인 조사→최소수정→Player API/메모리 전후 우선 지속. 새프로세스/실기기/QAcrypto·합성SQL 구분. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 재실행안 미승인 표기 대체. 신규 손상영수증안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 손상·호환불가 영수증
등록키 DATA-RECEIPT-REPAIR:UNITY:20261022. 49#data-receipt-repair-approved-20261022 / data.receipt-repair.v1.20261022 승인. Server 원키·귀속 복구계약 공급·부족 구현, Unity 실패분류/검증된원키복구·원본보존·검증후교체/owner·수명 담당. 새수령키·자동재지급0/관련범위만제한. ClaimComplete 실제반영·원문문자열 메모리증가 원인→최소수정→Player API/메모리 전후 우선. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 손상안 미승인 표기 대체, 신규 복수영수증안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 복수 영수증 복구
등록키 DATA-RECEIPT-BATCH-RECOVERY:UNITY:20261022. 49#data-receipt-batch-recovery-approved-20261022 / data.receipt-batch-recovery.v1.20261022 승인. Server owner별version·부분응답/원키 계약 공급·부족 구현, Unity 동일상태 직렬반영/독립기록진행·선행대기/필요원문만읽기·참조해제 담당. 실제 ClaimComplete 반영·원문메모리원인·Player API/메모리 전후 우선 계속. 원자료보존/반복 QA문자열참조 구분, 기존 owner소비 검증전 fallbackGET만 제거 금지. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 복수안 미승인 표기 대체. 신규 복구중표시안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 복구 진행 표시·기능 이용
등록키 DATA-RECOVERY-AVAILABILITY:UNITY:20261022. 49#data-recovery-availability-approved-20261022 / data.recovery-availability.v1.20261022 승인. Server 오류/권한·원복구 계약 공급·부족 구현, Unity 안내/관련명령제한·완료해제/닫기수명 담당. 독립기능보존/가짜진행률0/복구중복·예약입력0. 실제 ClaimComplete owner반영·원문 retained원인·Player API/메모리 전후 우선 계속, 새UI 선행대기 금지. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 복구표시안 미승인 표기 대체. 신규 복구중 새보상안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 복구 중 새 보상 요청
등록키 DATA-RECOVERY-NEW-CLAIM:UNITY:20261022. 49#data-recovery-new-claim-approved-20261022 / data.recovery-new-claim.v1.20261022 승인. Server 동일지급/권한·원키·일괄계약 공급·부족 구현, Unity 동일요청합침/관련제한·자동예약0·owner순서/수명 담당. 독립기능보존/임의일괄분할0/충분응답GET0. ClaimComplete 실제반영·원문메모리원인·Player API/메모리 전후 우선 지속. 기존QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 새보상안 미승인 표기 대체. 신규 앱복귀 세부안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 보상중 앱복귀
등록키 DATA-CLAIM-APP-RESUME:UNITY:20261022. 49#data-claim-app-resume-approved-20261022 / data.claim-app-resume.v1.20261022 승인. Server 원결과/원키·권한 계약 공급·부족 구현, Unity lifecycle신호합침·실단계복귀/이전입력폐기·영속수명 담당. 복귀자체GET/재수령0·계정격리. 실제 ClaimComplete 반영·원문메모리원인·Player API/메모리 전후 우선 계속. 데스크톱/모바일·새프로세스 구분. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 앱복귀 미승인 표기 대체, 신규 인증만료안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 보상 인증만료 복구
등록키 DATA-CLAIM-AUTH-RECOVERY:UNITY:20261022. 49#data-claim-auth-recovery-approved-20261022 / data.claim-auth-recovery.v1.20261022 승인. Server 인증/원키·권한 계약 공급·부족 구현, Unity 인증owner합침·계정대조/재시도예산·실제상태/영속수명 담당. 인증실패지급실패단정0·새키/계정혼입0. 실제 ClaimComplete 반영·원문메모리원인·Player API/메모리 전후 우선 계속. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 인증안 미승인 표기 대체, 신규 점검복구안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 점검 중 보상복구
등록키 DATA-CLAIM-MAINTENANCE:UNITY:20261022. 49#data-claim-maintenance-approved-20261022 / data.claim-maintenance.v1.20261022 승인. Server 명시제한/원키·권한 계약 공급·부족 구현, Unity 응답분류/재시도중복방지·원기록보존·수명 담당. 사용자우선 정상v2발행→실제account/wallet/progress 변경·영속반영 및 원문heap root/QA영향 확인→최소수정→Player API/메모리 전후. matching-only/legacy경로는 미완료, 기존원요청version/호환보존. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 점검안 미승인 표기 대체. 신규 공통반영세부안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 보상 공통반영
등록키 DATA-CLAIM-COMMON-APPLY:UNITY:20261022. 49#data-claim-common-apply-approved-20261022 / data.claim-common-apply.v1.20261022 승인. Server 실제원응답·owner별version/구호환 공급·부족 구현, Unity 정상v2/유실복구/archive 동일반영·영속/수명 담당. 잔액일치만완료0·기전송version/key보존·실반영전GET삭제0. 정상v2 실제owner변경·원문root/QA원인·Player API/메모리 전후 우선. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 공통안 미승인 표기 대체. 신규 결과표시실패안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 결과 표시실패
등록키 DATA-CLAIM-DISPLAY-FAILURE:UNITY:20261022. 49#data-claim-display-failure-approved-20261022 / data.claim-display-failure.v1.20261022 승인. Server 확정원결과/owner계약 공급·부족 구현, Unity 완료확인후 화면만재시도·공유수명/구세대격리 담당. 재지급/재가산/충분상태GET0. 정상v2 실제owner변경·archive·원문root/QA원인·Player API메모리 전후 우선 지속. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 표시실패안 미승인 표기 대체. 신규 일부owner판정안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 일부 반영 완료 판정
등록키 DATA-CLAIM-PARTIAL-APPLY:UNITY:20261022. 49#data-claim-partial-apply-approved-20261022 / data.claim-partial-apply.v1.20261022 승인. Server owner/version/coverage·복구계약 공급·부족 구현, Unity 실제반영·필수검증/저장후완료·필요범위복구 담당. 미공급UNCHANGED오인0·독립sequence비교0·충분응답GET0. 정상v2 실제owner변경·archive·원문root/QA원인·Player API메모리 전후 우선. 49 wallet observation version 기존공급 확인, 정책무효화와잔액순서구분. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 일부안 미승인 표기 대체, 신규 정책변경안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 재화 정책 변경
등록키 DATA-CURRENCY-POLICY-CHANGE:UNITY:20261022. 49#data-currency-policy-change-approved-20261022 / data.currency-policy-change.v1.20261022 승인. Server 정책/권한·관측 계약 공급·부족 구현, Unity 잔액/정책분리·변경확인/원요청보존·수명 담당. 실제무효화범위만조회·자동재소비0. 사용자우선 진행정보 실제변경·독립sequence/표시연결 및 원문root/QA원인→Player API/메모리 전후. versioned wallet/정상v2부분완료 재검수남발0, VERIFIED_EMPTY progress변경대체0. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 정책안 미승인 표기 대체. 신규 해금진입안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 다음구간 해금 표시·진입
등록키 DATA-REWARD-UNLOCK-ENTRY:UNITY:20261022. 49#data-reward-unlock-entry-approved-20261022 / data.reward-unlock-entry.v1.20261022 승인. Server 실제progress/정의·권한 계약 공급·부족 구현, Unity 실제상태반영·동일owner표시/구간보기·수명 담당. 연출기반해금/자동START/충분응답GET0. 실제잠금변경·원문root/QA원인·Player API메모리 전후 우선 지속, VERIFIED_EMPTY대체0. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 해금안 미승인 표기 대체. 신규 리소스준비안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 해금구간 자산준비
등록키 DATA-UNLOCK-RESOURCE-READINESS:UNITY:20261022. 49#data-unlock-resource-readiness-approved-20261022 / data.unlock-resource-readiness.v1.20261022 승인. Server 기존콘텐츠·입장계약 공급·부족 구현, Unity 필수준비/기존동의·선택세대/공유수명 담당. 사용자우선 story-progress-observation-version-20261022 seq/hash·정의 연결→실제nonempty진행·해금/영속반영, 원문root/QA원인→Player API메모리 전후. claim후GET강제0·기존원key/호환보존. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 자산안 미승인 표기 대체, 신규 선택후조건변경안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 선택후 진입조건변경
등록키 DATA-ENTRY-CONDITION-CHANGE:UNITY:20261022. 49#data-entry-condition-change-approved-20261022 / data.entry-condition-change.v1.20261022 승인. Server 조건/입장·원키계약 공급·부족 구현, Unity 선택유지/관련통지·새입력·구세대격리 담당. 상시GET/자동재전송/오진입0. 진행seq/hash 실제해금·원문root/QA원인·Player API메모리 전후 우선 지속. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 선택안 미승인 표기 대체, 신규 출격대기안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 출격 대기 입력·이동
등록키 DATA-START-PENDING-NAVIGATION:UNITY:20261022. 49#data-start-pending-navigation-approved-20261022 / data.start-pending-navigation.v1.20261022 승인. Server START/원키·복구계약 공급·부족 구현, Unity 안전보존후로비·기존요청합침/원run성공표시·수명 담당. 화면닫기취소오인/강제진입/새START0. 진행seq/hash실해금·원문root/QA원인·Player API메모리전후 우선. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 출격대기안 미승인 표기 대체, 신규 진입실패로비안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 확정후 진입실패 로비표시
등록키 DATA-CONFIRMED-ENTRY-FAILURE:UNITY:20261022. 49#data-confirmed-entry-failure-approved-20261022 / data.confirmed-entry-failure.v1.20261022 승인. Server 원START/권한복구 공급·부족 구현, Unity 확정과불명확표시분리·실패단계복구/원run보존·수명 담당. 새START/연료재소비/충분원정보GET0. 진행seq/hash실해금·원문root/QA원인·Player API메모리전후 우선. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 실패로비안 미승인 표기 대체, 신규 문의정보안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 진입실패 문의정보
등록키 DATA-ENTRY-SUPPORT-CONTEXT:UNITY:20261022. 49#data-entry-support-context-approved-20261022 / data.entry-support-context.v1.20261022 승인. Server 기존문의/오류·권한계약 확인·부족 구현, Unity 사실기반최소요약·기존문의/복사·수명 담당. 자동제출/원문노출/원작전삭제0. 진행seq/hash실해금·원문root/QA원인·Player API메모리전후 우선. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 문의안 미승인 표기 대체, 신규 작성중상태안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 문의 작성중 상태전환
등록키 DATA-SUPPORT-DRAFT-TRANSITION:UNITY:20261022. 49#data-support-draft-transition-approved-20261022 / data.support-draft-transition.v1.20261022 승인. Server 기존문의/계정·key계약 공급·부족 구현, Unity_11 본문보존/진단시점분리·계정격리·제출시점/수명 담당. 최신우선 충분경로후속GET축소·부분복구/필수owner완료판정·새프로세스 Player API메모리검수. Unity_10 nonemptyprogress·Regex root 근거 재사용/전체완료승격0. QA/인계 근거 후 Plan_7 답장, WAIT AND/후속키 유지. 앞 작성중안 미승인 표기 대체, 신규 제한해제안 미승인·미디어 제외.


### DATA-LIFECYCLE 보완: 한 구간 한 편성 실제 전투 진입
등록키 DATA-BATTLE-ENTRY-ASSETS:UNITY:20261022. 49#data-battle-entry-assets-approved-20261022 / data.battle-entry-assets.v1.20261022 승인. Server_5는 기존 구간/편성/원START·버전 지원 공급과 부족 구현, Unity_13은 실제 자산 매핑·입장 구성·기본공격/명중·종료해제 연결 및 통합 검수. 현 BLOCKED/LobbyKey를 READY로 우회하지 않는다. 확정된 독립 부분부터 구현하고 미공급 의존은 WAIT에 기록. 우선 한 구간 한 편성 실제 Player API/메모리 전후 및 새 프로세스·부분 복구 검수까지 진행. 충분 응답 후 GET0·원키/버전·단일 owner·계정/run 격리 보존. QA/인계 근거 후 Plan_7 결과 답장; 전체 완료/메모리 절감 추정0. 미디어 작업 제외, 기존 WAIT AND/후속키 유지.


### DATA-LIFECYCLE 보완: 첫 화면·첫 공격
등록키 DATA-BATTLE-FIRST-FRAME:UNITY:20261022. 49#data-battle-first-frame-approved-20261022 / data.battle-first-frame.v1.20261022 승인. Server 기존 원START·전투시점/복원 공급 및 실제 부족 구현, Unity_13 첫 표시 일치·시작 1회·복원 대기/계정/run 세대/자원 수명 구현. 선행 미공급 범위만 WAIT 보류. 최우선 에디터 전용 자산 연결을 실제 Player resolver/빌드 자산으로 연결→한 구간 한 편성 실제 공격→종료해제→API/메모리 전후 검수. READY/플래그만 우회0, 기존 충분정보 재사용/추가조회0·원키/버전 유지. 기존 DATA-LIFECYCLE AND/후속키 유지, QA/인계 후 Plan_7 결과 답장. 실제 Player 전체완료 미확인.


### DATA-LIFECYCLE 보완: 전투 종료·자원 해제 검수
등록키 DATA-BATTLE-EXIT-RELEASE:UNITY:20261022. 49#data-battle-exit-release-approved-20261022 / data.battle-exit-release.v1.20261022 승인. Server_5 확정 START/지원 구간/편성/version과 실제 BattleAssets424 명세 결속·호환 공급 및 부족 구현; Unity_13 Player resolver/consumer→전장/첫공격→종료·해제 연결. 이미 승인된 독립 작업 진행, 미공급 의존만 WAIT. 한 구간 한 편성3회 실제 Player API/메모리 전후, 신규/재사용 구분·늦은응답/재진입 중복/공유자산 수명 검수. 기존 충분정보 재사용·원START/키/hash 불변, 플래그 READY 우회0. 기존 종료/보상규칙 유지. QA/인계 후 Plan7 결과 답장, 기존 WAIT AND/후속키 유지·전체완료 미확인.


### DATA-LIFECYCLE 보완: 확정 출격·Player 자산 불일치 복구
등록키 DATA-PLAYER-ASSET-MISMATCH:UNITY:20261022. 49#data-player-asset-mismatch-approved-20261022 / data.player-asset-mismatch.v1.20261022 승인. 누락/손상/버전미지원/역할불일치 분기·원작전 보존·재소비0·세대격리. Server_5/Unity_13 최우선 49#ce03-player-asset-binding-server-v1-20261022 실제 소비·runner-v6 검증자 연결→전투 종료3회→Player API/메모리 검수. 합성true/플래그우회0, 충분 기존정보 재사용·원key/hash/version 보존. 미공급만 WAIT 보류, 기존 AND/후속키 유지, QA/인계 후 Plan7 결과답장. 전체통합 미완료.


### DATA-LIFECYCLE 보완: Player3회 비교·완료 판정
등록키 DATA-PLAYER-THREE-CYCLE:UNITY:20261022. 49#data-player-three-cycle-approved-20261022 / data.player-three-cycle.v1.20261022 승인. 기존 DATA-BATTLE-EXIT-RELEASE 시험의 조건/판정 보완이며 별도 중복시험 프로젝트 아님. Server5/Unity13 새Player 계약 실제연결→전투종료3회/API메모리 최우선. 동일빌드/구간/편성/조건, 최초1회·동일프로세스2~3회, 정상/복구 별도, 실제공격·명중·종료·해제 및 상태/중복소비·지급 검수. 이전실행불가면 최초기준값·절감률0, 미지원 미검수유지. 기존 원키/버전/정보재사용·WAIT AND/후속키 유지. QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 요원별 행동·효과 귀속
등록키 DATA-BATTLE-EFFECT-ATTRIBUTION:UNITY:20261022. 49#data-battle-effect-attribution-approved-20261022 / data.battle-effect-attribution.v1.20261022 승인. 기존공격/지원 원천·실행주체/소유요원·대상/효과표시/결과집계 확인, 미지원능력 미완료유지·새복제원장0. 현재3회검수 차단조건 추가0. 최우선 실제Unity Player 진입점/전체빌드·검토source를 ce03-player-executed-provider-v1-20261022 adapter/attest/host에 연결→전투종료3회/API메모리 검수. Server5 계약·실행검증자/host, Unity13 실제runner/consumer·전투/계측 담당. 기존정보재사용·원키/hash·수명·WAIT AND/후속키 유지. QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 전투 정보창 상태 일치
등록키 DATA-BATTLE-INFO-CONSISTENCY:UNITY:20261022. 49#data-battle-info-consistency-approved-20261022 / data.battle-info-consistency.v1.20261022 승인. 선택요원·사거리·장비·HP/보호막 동일owner, 선택전환/닫기 구독수명·늦은응답격리, 추가GET0 검수. 최우선 Server5/Unity14 확정START→실제전투/3회 API메모리·새프로세스복구 계속, 정보창검수로 핵심연결중단0. 실제파일 저장포트 호출/종료/새Player복원과 QA/제품저장 경계 명시. 기존키/hash/version/정보재사용·WAIT AND/후속키 유지. QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 복원 선택·정보창
등록키 DATA-RESTORED-SELECTION:UNITY:20261022. 49#data-restored-selection-approved-20261022 / data.restored-selection.v1.20261022 승인. 같은실행 유효선택유지/새프로세스 창닫힘/요원제외·계정run전환 해제/다운정보유지. 기존run owner 즉시표시·추가GET0·구독수명 검수. Server5/Unity14 최우선 변경후source/build/hash 일치 공급본과 편집종료 경계를 기존인계에 고정→실host/확정START/Player전투3회/API메모리/실파일 새프로세스복구. 구검증우회0. 기존 AND/후속키 및 원정보/키/버전 보존, QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 복원 첫 명중 연속성
등록키 DATA-RESTORED-FIRST-HIT:UNITY:20261022. 49#data-restored-first-hit-approved-20261022 / data.restored-first-hit.v1.20261022 승인. 기존대기/투사체/피해·흡혈·사망/종료규칙의 연속실행vs새process복원 대조, 미지원미검수. Server5/Unity14 최우선 Unity출발UI 실통신/원journal→확정START→실전투 및 실제응답유실복구·원키중복소비0 검수. Player465의 Node/QA평문/offscreen 경계 보존. 관리메모리증가 원인 참조/할당/GC·계측 분리→최소수정→동일조건 비교, 전체절감 추정0. 변경source/build/hash재검증·기존정보재사용·WAIT AND/후속키 유지. QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 출격 응답 유실 안내·입력
등록키 DATA-START-LOSS-UI:UNITY:20261022. 49#data-start-loss-ui-approved-20261022 / data.start-loss-ui.v1.20261022 승인. 준비중/결과확인/원키다시확인/확정/거절표시, 불명확구간편성고정·충돌입력만제한·왕복원요청보존, 저장실패와출격실패분리. 최우선 Server5/Unity14 UnityUI실통신·영속원journal/응답유실·새process복구 및 managed증가 원인분리/최소수정검수. NodeTCP검수는 제한근거만재사용, 기존자동1회/원키·버전/충분정보조회0 유지. 기존 AND/후속키, QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 출격 요청 기록 저장 실패
등록키 DATA-START-JOURNAL-SAVE:UNITY:20261022. 49#data-start-journal-save-approved-20261022 / data.start-journal-save.v1.20261022 승인. 전송전저장실패 HTTP0/같은키저장재시도·전송전취소경합, 전송후불명확보존, 확정응답보유시저장만재시도. NOT_FOUND/409를미출격으로추정0. Server5/Unity14 최우선 Unity출발UI 실통신·영속journal복구검수·메모리원인분리/최소수정 계속. 기존근거범위재사용/원key/version/owner·수명 및 AND/후속키 유지. QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 완료 출격 기록 정리·보관
등록키 DATA-START-JOURNAL-RETENTION:UNITY:20261022. 49#data-start-journal-retention-approved-20261022 / data.start-journal-retention.v1.20261022 승인. 동일계정/run/요청 durable완료 후 대기제외·복구원본보존 분리, 불명확/저장미완료 보존, 임시응답참조해제/종료기존보관정책. 정리중종료·새process/반복대기누적 검수. Server5/Unity14 Unity출발UI 실통신·영속journal복구·managed원인분리/최소수정검수 우선. 기존원key/version/정보재사용/AND·후속키 유지. QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 저장·정리 중복 진입
등록키 DATA-START-REENTRY-DEDUP:UNITY:20261022. 49#data-start-reentry-dedup-approved-20261022 / data.start-reentry-dedup.v1.20261022 승인. 동일계정/run/요청 저장과대기 대조·전장진입1회·충돌보존·현재종료상태우선. 최우선 Server5/Unity15 저장사이durable공백/전체journal재파싱/receipts메모리누적 실제수정→종료경계·새process·동시이어가기·API메모리 Player검수. Unity14 인계 retention/final488 절 근거. 원key/hash/version·복구원본보존·불필요GET0·기존 AND/후속키 유지. QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 출격 저장·복구 통합 완료 판정
등록키 DATA-START-RECOVERY-ACCEPTANCE:UNITY:20261022. 49#data-start-recovery-acceptance-approved-20261022 / data.start-recovery-acceptance.v1.20261022 승인. 기존시험 통합/별도중복실행0. 저장전송/서버확정응답/영수증checkpoint/대기정리경합/반복수명 단계별검수, 기능·메모리별도판정·미확인보존. Server5/Unity15 최우선 durable공백/전체재파싱/receipt누적 실제수정→Player검수. 기존유효근거재사용·source/build/hash변경검증·원키/버전/AND후속키 유지. QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 여러 계정·작전 복구 격리
등록키 DATA-RECOVERY-RECORD-ISOLATION:UNITY:20261022. 49#data-recovery-record-isolation-approved-20261022 / data.recovery-record-isolation.v1.20261022 승인. 계정/run/원key/version 격리·종료run재진입0·중복1회·충돌보존/필요기록만읽기 검수. 최신우선 Server5/Unity15 기존제품 보안저장/실제로비 연결, ComputeBuffer 생성소유해제 경고원인/managed·native·GPU메모리 확인→최소수정→실Player검수. 507로컬부분완료근거 재사용·제품전체승격0·QA평문fallback0. 기존원복구·정보재사용/AND후속키 유지. QA/인계 후 Plan7 결과답장.
Unity16 부분완료(2026-10-22): #unity16-account-storage-buffer-result-20261022 / verification.json.unity16. 제품계정별암호화journal+legacy보존/anchor복구·구세션캐시해제,525 99PASS0FAIL.526/527 buffer owner수정·새PID3회경고0·Editor반복통과. 실Android/제품전체로비·public입장/run별선택/전체전투메모리잔여,원507범위보존·부모AND/본큐유지.


### DATA-LIFECYCLE 보완: 보안 저장소 실패 복구
등록키 DATA-SECURE-STORAGE-FAILURE:UNITY:20261022. 49#data-secure-storage-failure-approved-20261022 / data.secure-storage-failure.v1.20261022 승인. 원기록보존·읽기/키/쓰기실패구분·빈저장/평문fallback0·출격전전송차단/확정후저장재시도·계정세대격리. Server5/Unity16 제품실로비/권한출격·현재run owner/run별pending→실전투/Android secure저장·새process복구/전체전투메모리 우선. publicBLOCKED우회0·환경미공급 정확기록/독립구현진행. 525/527부분근거재사용·전체승격0. 기존 AND/후속키·원키/version 유지. QA/인계 후 Plan7 결과답장.


### DATA-LIFECYCLE 보완: 로비 작전·복구 기록 선택
등록키 DATA-LOBBY-RUN-SELECTION:UNITY:20261022. 49#data-lobby-run-selection-approved-20261022 / data.lobby-run-selection.v1.20261022 승인. 현재owner 단일판정/동일run이어가기1개/종료우선/충돌임의선택0/불명확보존·추가GET0. Server5/Unity16 우선 제품출격 currentrun원천/public-LOCAL 요청차이·지원명세/실행권한/복구호환 계약을 기존49에서 맞추고 실제제품로비·Android secure복구/전체전투메모리 구현검수. BLOCKED우회0·필요최소원천/기존정보재사용, 확정독립작업진행·미결만WAIT. 기존 AND/후속키, QA/인계 후 Plan7 결과답장.

Unity16 2026-10-22 부분검증 갱신(DATA-SECURE-STORAGE-FAILURE/LOBBY-RUN-SELECTION): Unity인계#unity16-secure-storage-failure-result-20261022, verification.json.unity16SecureStorage. start-result확정응답 저장재시도HTTP0/credential갱신보존/기존공통입력연결·파일읽기failclosed,535 99PASS/536 build성공. 현재run최신영수증선택제거·원START/저장실패분류·최신public consumer/Android/전체메모리는미완료. 새후속등록/완료삭제0.

### DATA-LIFECYCLE 보완: 제품 출격 지원·차단 표시
등록키 DATA-DEPARTURE-SUPPORT-UI:UNITY:20261022. 49#data-departure-support-ui-approved-20261022 / data.departure-support-ui.v1.20261022 승인. 해금/적격/자산/지원분리·기존정보표시GET0·실행권한유지. Server5/Unity16 현재owner/run선택 및 최신승인policy 실제제품runtime·결과검증 연결 우선, LOCAL수치전용/flag우회0. Android연결후실검수·그외독립진행, 전체전투메모리/버퍼재발확인. 기존 AND/후속키·원key/version 보존. QA/인계 후 Plan7 결과답장.

Unity16 2026-10-22 추가부분결과: 인계#unity16-lobby-run-selection-result-20261022 / verification.json.unity16RunSelection. 최신시간선택제거·단일복구표시판정/충돌·종료·원key구분/로비재진입GET0,546111PASS/547build성공. 최신public v3/v4 consumer·지원/차단표시·승인정책실행/실Android/전체전투메모리는잔여. Unity17 문서계승예정,기존큐/AND유지.
Unity17 2026-10-22 부분결과(DATA-DEPARTURE-SUPPORT-UI/LOBBY-RUN-SELECTION): Unity인계#unity17-departure-consumer-result-20261022 및 verification.json.unity17DepartureConsumer. 최신 public requirements-v3/admission-v3/v4 진단소비·원policy/편성/ultimate/authority대조·차단안내/추가GET·START0,55399PASS/554build성공/555source대조. 실제policy실행결속·전체분리표시/nativeUI·직접START/최초저장복구·Android·전체전투메모리잔여. 기존큐/AND/후속키유지,완료삭제0.


### DATA-LIFECYCLE 보완: 복수 차단 사유
등록키 DATA-DEPARTURE-MULTI-BLOCK:UNITY:20261023. 49#data-departure-multi-block-approved-20261023 / data.departure-multi-block.v1.20261023 승인. 대표1개+전체목록/승인우선순위/부분해소·상태통지/클릭재검사·추가GET0. Server5/Unity17 실제최신9/29.1 policy 및requirements/admission계약소비→제품runtime/결과검증 연결 우선. 9/22역사복구보존·LOCAL우회0. Android연결시/전체전투메모리검수 계속. 기존 AND/후속키유지, QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과: 인계#unity17-multi-block-result-20261023 / verification.json.unity17MultiBlock. 대표+전체사유/부분해소/기존해결콜백·현재검사/표시GET0,56472PASS/565build성공/566source대조. nativeUI 전체알림·역순응답/최신policy실행/Android/전체전투메모리잔여; 본큐·AND유지,완료삭제0.


### DATA-LIFECYCLE 보완: 차단 해소 후 입력
등록키 DATA-DEPARTURE-UNBLOCK-INPUT:UNITY:20261023. 49#data-departure-unblock-input-approved-20261023 / data.departure-unblock-input.v1.20261023 승인. 활성화만/예약자동출격0·새클릭현재조건·기존송신원요청복구·구화면입력격리·버튼위치유지. Server5/Unity17 최신실계약/9/29.1policy 제품연결 우선, Android연결시/전체전투메모리 검수계속. 기존key/version/정보재사용/AND후속키 유지. QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-DEPARTURE-UNBLOCK-INPUT): 인계#unity17-unblock-input-result-20261023 / verification.json.unity17UnblockInput. 차단중입력예약·구화면선택입력차단/기존원요청재사용,56975PASS/570build성공/571source대조. 물리입력·실제품전투원천결속·Android/전체전투메모리잔여,기존큐/AND유지·완료삭제0.


### DATA-LIFECYCLE 보완: 출격 요약·확정편성 일치
등록키 DATA-DEPARTURE-SUMMARY:UNITY:20261023. 49#data-departure-summary-approved-20261023 / data.departure-summary.v1.20261023 승인. 준비표시/선택버전/원요청/확정run 일치·조건변경안내/자동재출격0·로비변경혼입0·추가GET0. Server5/Unity17 최신계약 제품전투결속 우선, Android연결시/전체전투메모리검수 계속. 기존유효검수재사용·원key/version·AND후속키 유지. QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-DEPARTURE-SUMMARY): 인계#unity17-summary-result-20261023 / verification.json.unity17DepartureSummary. 선택revision불일치·낡은비용차단/원preview보존·서버변경안내계약소비,57587PASS/576build/577source. 확정START FUEL PATCH소비/불필요성공후GET제거·확정run실consumer/제품runtime/Android/전체메모리잔여. 본큐/AND유지·완료삭제0.


### DATA-LIFECYCLE 보완: 전장 전환 초기 정보
등록키 DATA-BATTLE-TRANSITION-DISPLAY:UNITY:20261023. 49#data-battle-transition-display-approved-20261023 / data.battle-transition-display.v1.20261023 승인. 표시준비전기존안내/확정·복원상태첫표시/이전run·기본값노출0/전환실패재소비0/시작신호결속. Server5/Unity17 departureChange최신계약 안내/제품전투실결속 우선, Android연결시·전체메모리검수 계속. 기존원key/version/정보재사용·AND후속키, QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-BATTLE-TRANSITION-DISPLAY): 인계#unity17-battle-transition-result-20261023 / verification.json.unity17BattleTransition. 첫render owner/revision변경·pause차단/원상태보존·실패1회정리,58150PASS/582build성공/583source대조. LOCAL/Editor근거이며제품전환전체·최신public runtime/Android/전체메모리잔여. 본큐/AND유지·완료삭제0.


### DATA-LIFECYCLE 보완: 조건변경 후 준비 복귀
등록키 DATA-DEPARTURE-CHANGE-RETURN:UNITY:20261023. 49#data-departure-change-return-approved-20261023 / data.departure-change-return.v1.20261023 승인. 실제변경항목/확보새값표시·미공급추정0·무효화범위갱신·원요청복구/자동출격0. 최신우선 Server5 일반5인 effectiveStats/실행원천 실제구현(원boundary참조), Unity17 정확버전제품소비·Player전투연결. 기존계산/승인정책재사용·B1인/LOCAL대체0·조사만반복0. Android연결시/전체메모리검수. 기존 AND/후속키, QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-DEPARTURE-CHANGE-RETURN): 인계#unity17-change-return-result-20261023 / verification.json.unity17ChangeReturn. 검증된항목별안내·owner한정갱신/이전inflight차단·원요청보존,58671PASS/587build/588source대조. 일반5인Server원천공급·Unity제품전투/실값강조·Android/전체메모리잔여. 본큐/AND유지·완료삭제0.


### DATA-LIFECYCLE 보완: 능력치 원천별 적용 확인
등록키 DATA-STAT-SOURCE-VERIFICATION:UNITY:20261023. 49#data-stat-source-verification-approved-20261023 / data.stat-source-verification.v1.20261023 승인. 기본성장/장비/연구증강/일시효과·복원 원천대조·중복0·DEV단계값복사/실사용자내부정보0. Server5 corestats를 START원본/현재HP/기타modifier·동일batch에 실제연결하고 남은효과실행원천 구현, Unity17 정확버전제품소비·Player검수. NO_ACTIVE 원천필수/미공급0처리0·전체완료승격0. Android연결시/전체메모리검수 계속. 기존 AND/후속키, QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-STAT-SOURCE-VERIFICATION): 인계#unity17-stat-source-result-20261023 / verification.json.unity17OrdinaryStatSource. 정확corepacket/source검증·불변값/HP보존·역행/중복/thread경계,59369PASS/594build/595source대조. Server START/CAS설치작업중·완료계약미확인,확정제품adapter/정보창/DEVcopy/Player/Android/전체메모리잔여. 본큐/AND유지·완료삭제0.


### DATA-LIFECYCLE 보완: 최대HP 상태·표시 일치
등록키 DATA-MAX-HP-CONSISTENCY:UNITY:20261023. 49#data-max-hp-consistency-approved-20261023 / data.max-hp-consistency.v1.20261023 승인. 기존정책current/maxHP동시반영·보호막분리·만료/저장복원/열린정보창일치·치료사건오인검수, 미확정HP보정방식임의채택0. Server5/Unity17 START원본/남은효과실행원천 실제제품연결 우선·Android연결시/전체메모리검수. 기존 AND/후속키·원key/version유지, QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-MAX-HP-CONSISTENCY): 인계#unity17-max-hp-result-20261023 / verification.json.unity17MaxHpInformation. 기존전투원Vitals→열린정보창 current/maxHP·별도shield/확정revision연결;60167PASS/602build/603source대조,600실크기후보캡처검토. Server동일batch부분완료확인,운영modifier/HP verifier·loop미공급 및일반5인정확제품adapter/실Player/Android/전체메모리잔여. 본큐/AND유지·전체완료승격/삭제0.


### DATA-LIFECYCLE 보완: 효과와 진행 중 공격 판정
등록키 DATA-INFLIGHT-EFFECT-CONSISTENCY:UNITY:20261023. 49#data-inflight-effect-consistency-approved-20261023 / data.inflight-effect-consistency.v1.20261023 승인. 발사값고정/명중방어·효과만료획득/동시순서/복원중복0 검수. Server5 실제modifier/HP event verifier/loop caller 연결, Unity17 정확원천제품소비·Player통합 우선. 합성60PASS전체승격0·임의원천/순서0. Android연결시/전체메모리검수 계속. 기존AND후속키, QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분검수(DATA-INFLIGHT-EFFECT-CONSISTENCY): 인계#unity17-inflight-result-20261023 / verification.json.unity17InflightConsistency. 발사전후공격증감/도착DEF/두발restore/중복contact 4조합추가·60449PASS,운영코드불변1260compiled source대조로602build/603핀재사용. 실제효과producer/동시사건순서·일반5인loop/Player/Android/전체메모리잔여. 큐/AND유지.


### DATA-LIFECYCLE 보완: 효과 사건 적용1회
등록키 DATA-EFFECT-ONCE:UNITY:20261023. 49#data-effect-once-approved-20261023 / data.effect-once.v1.20261023 승인. 동일사건재가산0/내용충돌구분/정상반복보존/만료·복원순서/기록수명 검수. Server5 실제phase/초기HP·stat·loop원자결속/진행modifier·HP검증자·호출부 구현, Unity17 정확제품소비/Player통합 우선. INITIAL원본으로현재phase추정0·사용자미결로전가0. Android연결시/전체메모리. 기존AND후속키, QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-EFFECT-ONCE): 인계#unity17-effect-once-result-20261023 / verification.json.unity17EffectOnce. 표시커서동일ID본문충돌명시거절·Seed전체원자검증,6054FAIL→60645PASS/607build/608source대조. 사건별hash비용/장기메모리미측정·실phase/진행효과/HP verifier/loop와일반5인제품/Player/Android잔여. 큐/AND유지.


### DATA-LIFECYCLE 보완: 효과 표시·판정 종료
등록키 DATA-EFFECT-DISPLAY-EXPIRY:UNITY:20261023. 49#data-effect-display-expiry-approved-20261023 / data.effect-display-expiry.v1.20261023 승인. 실owner만료/중첩/해제/복원시간 표시일치·구독수명·아이콘판정소유0·추가GET0. Server5 제품loop/실HP verifier/진행modifier연결, Unity17 실제제품소비/Player검수 우선. DB RUNNING전체실행승격0·Android연결시/전체메모리. 기존AND후속키, QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-EFFECT-DISPLAY-EXPIRY): 인계#unity17-display-expiry-result-20261023 / verification.json.unity17EffectDisplayExpiry. 기존보호막접근/흡수표시의50ms가정제거·원clock결속,20/100ms실simulation pause/restore/만료·mesh/오류입력검수,61363PASS/614build/615source대조. 실제진행효과원천/전체아이콘·시간/일반5인제품/Player/Android/전체메모리잔여. 본큐/AND유지.


### DATA-LIFECYCLE 보완: 효과 누락·오래된 상태
등록키 DATA-EFFECT-PARTIAL-STALE:UNITY:20261023. 49#data-effect-partial-stale-approved-20261023 / data.effect-partial-stale.v1.20261023 승인. 미공급≠해제/명시삭제·역순차단·불연속범위복구/타run격리·표시GET0 검수. Server5 liveHP event/modifier/제품loop실결속, Unity17 정확소비/Player통합 우선. 초기HP40PASS는신규한정재사용·복원초기화0. Android연결시/전체메모리검수. 기존AND후속키, QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-EFFECT-PARTIAL-STALE): 인계#unity17-partial-stale-result-20261023 / verification.json.unity17EffectPartialStale. 기존typed-result검증을표시기록전에재사용·누락과명시0구분·Seed원자보존. 6165FAIL→61750PASS/618build/619source대조. 신규INITIALIZE40PASS재사용(복원초기화0),진행효과원천/정확제품소비·불연속복구/Player/Android/전체메모리잔여. 본큐/AND유지.

### DATA-LIFECYCLE 보완: 동일사건 상태 표시
등록키 DATA-COMBAT-EVENT-DISPLAY:UNITY:20261023. 49#data-combat-event-display-approved-20261023 / data.combat-event-display.v1.20261023 승인. 같은확정단위 HP/보호막/효과동일표시·별개사건순서보존/표시중간판정0·version혼합0·복원후사건연결. Server5 진행HP/효과/제품loop실결속, Unity17 제품소비/Player통합 우선. Android연결시/전체메모리, 기존AND후속키·원key/version 유지. QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-COMBAT-EVENT-DISPLAY): 인계#unity17-event-display-result-20261023 / verification.json.unity17CombatEventDisplay. 실제기존엔진TEST_ONLY2000피해×보호막3경계의확정HP/흡수/전장meter/실정보문구일치·저장실패/복원재생0. 62137PASS→검토보완622해당1PASS(중복합산0). 운영수정0·compiled1260동일로618build/619핀재사용. 진행실원천/제품loop/정확소비/Player/Android/전체메모리잔여,큐/AND유지.

### DATA-LIFECYCLE 보완: 전투 표시 지연
등록키 DATA-COMBAT-DISPLAY-BACKLOG:UNITY:20261023. 49#data-combat-display-backlog-approved-20261023 / data.combat-display-backlog.v1.20261023 승인. 최신확정상태표시/실판정집계보존/기존가독성내연출정리·주요전환유지/대기참조수명검수. Server5 일반피해명중·실효과생성만료·제품loop구현, Unity17 실제제품소비·표시/Player통합 우선. Android연결시/전체메모리, 기존AND후속키 유지. QA/인계 후 Plan7 결과답장.

Unity17 2026-10-23 부분결과(DATA-COMBAT-DISPLAY-BACKLOG): 인계#unity17-backlog-result-20261023 / verification.json.unity17CombatDisplayBacklog. paused종료시결과장식3목록/객체·linkedCommands정리와terminal신규장식/음향억제,원사건/HP보존.6232FAIL→62443PASS·검토보완625b7PASS(중복합산0)/626build/627source. MAX_HP_REBASE41PASS만재사용·일반명중/live효과/제품loop와정확소비/실Player·앱복귀전체가독성/Android/전체메모리잔여,큐/AND유지.
### LANDSCAPE-D2:UNITY-BASE:20261023 — 가로 전투 전환 착수 승인
사용자 2026-10-23 명시 작업지시. 원본44#landscape-two-facing-approved-20261023, 포대 최신확정/새제안 분리는44#landscape-curve-roof-artillery-20261023.
선행완료된 정책 기반 실행: 실제 제품 출격→전투 진입 경로 식별 후 가로 D 레이아웃·카메라/안전영역·기존 노즐 명령/바인딩·2방향 표시소비 기반을 구현. 준비된 자산만 연결하고 미공급은 정확히 기록. 승인 포대는 선로 겨냥, 열차 보호막/가감속/방향전환/열차사격 대응, 요원 직접표적/드론/연쇄/광역대응 제외·열차버프만허용. 곡선/지붕이동/포격세부패턴은 신규제안이며 승인혼동0.
소유파일/계약을 기존 Unity 인계에 착수 전 기록, Direction 제작파일 동시편집0. LOCAL/옛뷰만 수정해서 제품완료 주장0. 기존 일반피해/실효과/제품loop 연결 지속. 새GET/신규START로 화면변경 처리0. 구snapshot/방향호환 유지. 독립기반 구현·빌드·가로Player 입력 근거부터 기록하고 미완료 자산/서버계약만 대기. 전체통합은 기존 LANDSCAPE-D2:UNITY-INTEGRATION:20261023 선행 충족 후, 실제Player/API/3회종료메모리/Android 연결시 검수. 이기반큐는 전체통합완료 아님.

### LANDSCAPE-CURVE-ROOF-ARTILLERY:UNITY:20261023
사용자44#landscape-curve-roof-artillery-20261023 상세안 승인,뒤승인갱신우선. 기존 LANDSCAPE-D2:UNITY-BASE:20261023 가로제품전환 계속. 승인곡선측면유지/위치변경UI·합법앵커/원명령·표시/포격예고·속도예측·요원포대미대응을 준비된원계약/자산 범위에서 실제구현. 서버미완료계약은원천합의/정확대기로남기고중복판정0. 화면방향과원공간분리,열차꼬리판정,원효과·발사탄·쿨다운·복원유지. 이동2방향자산미공급은표시완료주장0. 설명숫자확정0. 앞뒤적열차/승차신규절은미승인. 실제Player가로입력→전투→결과/복원,API/3회전체메모리 검수·Android연결시. 기존QA에부분/전체구분.

### LANDSCAPE-ENEMY-BOARDING:UNITY:20261023
사용자 앞뒤 적 열차/승차 상세 승인. 44#landscape-enemy-train-boarding-proposal-20261023 §1~6 및 뒤승인갱신 우선, 이전미승인표기대체.
가로D기반전환을계속하며 준비된원계약으로 앞뒤적표시·승차예고/착지·표적/위치변경·2방향·가림을 실제제품에 연결. 원계약미공급은독립구현범위와구분,임의클라판정0.
앞뒤최소간격/추돌미도입·공중신규타깃제외/기발사탄유지·착지즉시피해0·포대직접대응제외 유지. 자산/계약 미준비는해당연결만대기. 승인된곡선/이동/선로포격과 기존피해/효과제품연결 계속. 실제Player/API/3회종료메모리·Android연결시검수,부분/전체구분. 44#landscape-combined-threat-proposal-20261023은새제안이므로이번승인에포함하지않음.

Unity17 2026-10-23 가로기반 부분검증: Landscape631 관련44PASS/시험fixture1FAIL→632가로4PASS(중복합산0),635Player build오류0/경고5. 634 첫승리후 프레임겹침 발견으로중단·구MonitorBezel/console shell숨김,636수정Player3회진행. 44곡선/지붕/포격및앞뒤승차승인갱신확인;combined-threat제외. Server#landscape-rail-hazard-result-20261023의순수구간9PASS는실HP/원geometry/clock/운영caller완료아님. 일반UNITY_LOCAL/HOST 제품binding/원사건producer는Unity자체잔여로정정하며전체를Server대기로두지않음. 2방향검증자산·승차원계약및제품START연결미완료,본큐유지. 최종근거는기존Unity인계의unity17-landscape-base-result-20261023 및verification.json.unity17LandscapeBase에기록.

### LANDSCAPE-COMBINED-THREAT:UNITY:20261023 / 실행 담당 경계 정합
사용자복합위협/대응시간안승인. 44#landscape-combined-threat-proposal-20261023 및뒤승인갱신,49#landscape-authority-owner-alignment-20261023 적용. 이전미승인문구대체. 3초/2초는첫시험가설로검수,운영실측완료아님.
일반 SOLO=UNITY_LOCAL/COOP=UNITY_HOST 원판정handler·제품binding·원사건emit/HP·효과·가로표시/입력연결을실제구현. 일반loop전체Server대기0; WORLD_BOSS는서버권위유지.
복합위협은원실행권위에시간표/조합적격검사·예고전연기·마감표시연결. 미공급필드/검증경계만구체대기,기존원응답/메모리재사용·표시GET0. 가로전투/곡선/이동/포격/승차 계속. 원START부터실제Player/API/3회전체메모리/새프로세스복원까지검수,Android연결시. 새분기이탈/추격/종료절은미승인. 기존QA/인계에caller·원천·부분/전체근거기록.

### ALL-SCREENS-LANDSCAPE:UNITY:20261023
사용자전체화면가로전환및담당충돌병합지시. 33#all-screens-landscape-approved-20261023 최신정책을읽고자기담당실제원본/화면/계약/제작프롬프트와대조하여수정·병합·검수. 이전전투한정예외대체. 선행완료된정책기반 독립조사/공통수정부터실행,미공급자산·계약전체연결은선행충족후. 공통파일은소유담당1곳,구원본보존/통째덮어쓰기0. 기존QA/인계에파일별근거·충돌·부분완료기록. 가로전투실제품연결·기존데이터재사용/권위/복원/API·메모리검수계속. 외부웹은반응형유지하며게임가로미리보기/연계범위점검. 무관한배포/경제행위권한추가0.

Unity17 2026-10-23 부분반영: 공통가로정책과Auth/Startup/Lobby/독립overlay 재배치·확인결함수정. 최종64931PASS/650build0error10warning/651세비율69offscreen Player PNG(합성자료·무계정상태구분). 인계#unity17-all-screens-landscape-result-20261023 및 verification.json.unity17AllScreensLandscape 근거. 인증제품전체순회/모바일입력/최신자산·전투실행연결잔여로큐미삭제. ordinaryKit 원START 공급완료확인, Unity adapter/handler/product binding은자체잔여.


### LANDSCAPE-BRANCH-COMPLETION:UNITY:20261025
선행 정책 완료:44#landscape-branch-completion-approved-20261025 및 해당 분기 이탈·추격·전투 완료 §1~5. 최신 승인으로 이전 큐의 이 항목 미승인 문구를 대체. 기존 가로 제품 전투 연결과 함께 일반 전투의 분기잠금/경로분리/재교전/승차병 잔존/종료 상태를 단일 실행 owner에 연결한다.
기존 source가 있는 독립 구현부터 진행. 화면밖 표시 해제와 적 상태 폐기를 분리하고 재합류 HP/효과 보존, 사거리 이탈은 새 공격만 중단하며 진행 탄/효과는 기존 계약 유지. 포대에 요원 직접 공격 금지. 정보 UI는 원목표/메모리 재사용, 새 GET·피격모션/추격보정 추가 금지. 새 계약 미공급은 해당 결속만 WAIT.
기존 인계/QA에 실제 입력→판정→결과·복원·동일 적 재합류·승차병 남은 섬멸 종료 방지 근거 기록. 실제 Player/API 호출/전투 종료3회 메모리 검수 지속, Android는 연결 시 검수. 합성/단위시험과 제품 검수 구분. Server 계약 완료 후 소비 후속은 WAIT#landscape-branch-completion-followups-20261025에서 등록한다.


### LANDSCAPE-COMMON-UI:UNITY:20261025
선행 정책 완료:33#landscape-common-layout-approved-20261025 / LANDSCAPE-COMMON-UI-v1.20261025. 상세원본33#landscape-common-layout-detail-proposal-20261025 §1~7. ALL-SCREENS-LANDSCAPE 기존 구현 재사용, 역할별 본문/안전영역/고정 행동·최상위 입력 소유·한입력 한단계·팝업 닫기 관통 방지·좁은 상세 복귀를 연결한다. 화면 닫기와 이미 전송한 명령 취소를 분리하고 원요청 복구 보존. 기존 메모리/응답 Model 재사용, 화면 왕복 GET0/전체응답 복제0/늦은 callback 격리/MVC Bind-Unbind 유지.
현재 공급된 자산·계약으로 독립 구현 진행, 미공급 자산/원계약만 정확 대기. 승인된 제품 가로 전투 연결 계속. 완료 근거는 기존 Unity QA/인계에 실제 화면경로/비율/입력/키보드·명령중 이탈/복원/API·3회메모리·미검수 기록. 단위/합성/offscreen과 제품/모바일 검수 구분. WAIT#landscape-common-ui-followups-20261025 후속 조건 적용. 새로운 전투 터치 충돌 상세 제안은 미승인으로 분리.


### LANDSCAPE-TOUCH:UNITY:20261025
선행 정책 완료:44#landscape-touch-ownership-approved-20261025 / LANDSCAPE-TOUCH-v1.20261025. 상세44#landscape-touch-ownership-proposal-20261025 §1~6 및33 공통 UI 승인. 기존 입력 owner/Controller에 pointer 최초소유·독립 동시입력·같은제어 중복금지·모달/재배치/앱복귀 cancel과 새 down 재개를 연결. 실제속도 손잡이/목표 의도 분리, 확정명령 rollback 금지. 재구현/전역 상태원장/표시 GET 추가0.
현재 공급된 원천 기반 독립 구현부터 진행하고 실제 가로 전투 연결 계속. QA는 실제 입력→명령/자원·cooldown·취소/복원, 노즐→카드 관통0, 노즐+보호막, 동일노즐 두손가락, 이동앵커소멸, 잠금전후/강제감속/노치·창폭변경/앱복귀를 확인. PC 시험으로 모바일 통과 대체0. 실제 Player/API·3회 종료 메모리/새프로세스 복구 계속, Android 연결 시. 기존 QA/인계에 근거·미검수, WAIT#landscape-touch-followups-20261025 완료기반 후속. 새 예고 중첩 표시안은 미승인.


### LANDSCAPE-THREAT-DISPLAY:UNITY:20261025
선행 정책 완료44#landscape-threat-display-approved-20261025 / LANDSCAPE-THREAT-DISPLAY-v1.20261025, 상세 threat-display-detail-proposal §1~6. 기존 사건owner→HUD projection으로 원위치·핵심1건·보조목록/상세 연결. 실제 마지막 유효 대응시각/수단별 기한 차이·미공급 기한·원sequence 안정순서, 종료/취소/복원 표시 일치. 표시용 GET/물리판정 복제/자동카메라0, 승인 공통UI/터치입력 연결 재사용.
실제5인 가로 전투와 저장·복원 연결 계속. Server49#ordinary-initialization-recovery-contract-20261025의 기존 lifecycle 응답 ordinaryInitialization은 초기화 상태만이며 runtimeExecutionVerified:false를 실전투완료로 승격하지 않음. 실제 bootstrap/5인 adapter/handler/checkpoint 결속과 원권위 유지, 합성/admission우회 금지. 원예고 계약/자산 미공급은 해당부분만 WAIT. 실제 Player/API·3회전투메모리·새프로세스복원/모바일 읽기·터치 검수, 기존 QA/인계에 부분/전체 구분. WAIT#landscape-threat-display-followups-20261025 후속 조건 적용. 새5인 카드안 미승인.


### LANDSCAPE-FIVE-CARDS:UNITY:20261026
정책 선행 완료44#landscape-five-operator-cards-approved-20261026 / 상세five-operator-cards-proposal-20261025 §1~5. 사용자 우선순위33#five-operator-product-binding-priority-20261026 유지: 실제원START5인 factory/handler/checkpoint 저장복원 연결을 우선하고 카드표시만 반복하여 제품연결을 미루지 않는다. 현재공급된 기존카드/상태owner 독립결속부터 진행, 제품상태미공급 부분만대기.
원slot순서고정/선택·궁극기·참여·이동·다운 분리, 초상정보와궁극기hitbox 분리, 다운정보확인/동일요원선택유지, 실제Vitals/ability/행동Model 재사용. 원계정/run/instance/hash·세대/구독수명검사, 표시GET/원문복제0. 이동중궁극기보류1건 기존예외 보존, 다운/종료/복원후미소비입력자동재실행0. 작은가로/실제5인카드↔전장↔정보일치 및새프로세스복원·3회메모리/API 검수, LOCAL합성근거를제품통과로대체0. 기존QA/인계근거, WAIT#landscape-five-cards-followups-20261026 후속정의. 새목적지표시안 미승인.


### LANDSCAPE-TRAIN-STATE:UNITY:20261026
선행정책완료44#landscape-train-control-state-approved-20261026 / LANDSCAPE-TRAIN-STATE-v1.20261026, 상세train-control-state-proposal §1~5. 기존열차View/Controller/원상태owner에 기능별 접수·실행·효과유지·재사용/거절을독립결속. 보호막유지와쿨다운/목표속도와실제속도/방향예약과진입/발사와명중구분. 실제즉시수락을가짜네트워크대기로늘리지않음. 원명령·권위검사·영속복구/세대유지, 표시GET/자원독립원장/새기능0.
실제5인factory/handler/checkpoint·제품bootstrap 저장복원이최우선(33#five-operator-product-binding-priority-20261026). UI독립단위는현재공급범위에서적용하되제품연결대체0. 기존QA/인계에실제정상/동시입력·중단·복원·3회메모리/API/플랫폼검수증거, LOCAL과제품구분. 미공급만대기,WAIT#landscape-train-state-followups-20261026 후속. 신규목적지/궁극기보류표시상세안 미승인.


### ULTIMATE-HOLD-FEEDBACK:UNITY:20261026
정책완료44#ultimate-hold-feedback-approved-20261026 / ULTIMATE-HOLD-FEEDBACK-v1.20261026, 상세landscape-ultimate-hold-feedback-proposal §1~5. 실제수락후정지후사용배지/명시취소/원보류1건/실행직전불충족시해제·자동재보류0,취소실행경합원순서·소비1회·다운/종료/복원미소비자동실행0. 기존ability/이동owner·카드Controller 재사용,표시GET0/독립원장0. 실제5인factory/handler/영속checkpoint·제품연결최우선,UI로대체0. 기존QA/인계에정상/취소/경합/복원·모바일/API/메모리근거분리. 새five-basic-product-source-proposal-20261026/목적지표시안은미승인. WAIT#ultimate-hold-feedback-followups-20261026 완료기반후속.


### BASIC-MIRA-EVE-A:UNITY:20261026
선행정책완료44#mira-general-eve-a-approved-20261026 / BASIC-MIRA-EVE-A-v1.20261026. 기존미라원탄도/수명·BASIC원기회재사용,일반지원version 검증·5인handler연결. 이브A는BASIC발사매개와액티브드론독립owner/충전귀속을분리하고두피해복제0. 아트/소켓미공급부분만대기,실source기반독립action/저장결속계속. 승인정책만으로임시시각/탄도를제품완료로승격0.
실제5인전투·저장복원연결최우선,기존인계/QA에원호출·계약·정상/단독및장치병용/복원·제품Player/API/메모리증거분리. 미라시험연구치명제외전체확대0,실admission우회0. 새레이나/세라수치안미승인,WAIT#basic-mira-eve-a-followups-20261026 완료기반후속. 기존전체부모완료처리0.


### BASIC-REINA-SERA:UNITY:20261026
정책 선행완료44#reina-sera-basic-ballistics-approved-20261026 / BASIC-REINA-SERA-v1.20261026 및 원 proposal §1~4. 기존 큐의 레이나/세라 미승인 기록은 이번 범위에서 대체. Server는 실제 기본공격 definition·formal ID/version/hash·START pin·저장호환을, Unity는 현재 공급된 action/탄도/회수·공속 snapshot·발사원/상태저장 독립 연결을 구현. 상대 계약·실geometry 미공급 부분만 대기하고 가짜값/소비완료 주장 금지. 레이나24m/72m/s/r35mm/W180/R220/C1400,세라18m/36m/s/r80mm/W240/R260/C1200. 원피해/영역/치명·연구 보존, 원주기에 준비회수 추가0, 세라 치유/생명탄과 구별.
최우선 실제5인 START→factory/행동/HP→영속checkpoint→새프로세스복원→결과 연결 계속. 기존 응답/Model 재사용·표시GET0/계정run세대격리/원요청복구 유지. 기존 QA/인계에 정상·경계/공속·차폐/Commit중복·실Player/API·3회메모리 근거를 구분. WAIT#reina-sera-basic-followups-20261026 선행 충족 후 소비/통합 등록. 카일·이브 새수치 미승인.


### BASIC-KYLE-EVE:UNITY:20261026
정책완료44#kyle-eve-basic-ballistics-approved-20261026 / BASIC-KYLE-EVE-v1.20261026 및 proposal §1~4. Server 실제ordinaryBasicSource에원정의·formalID/version/hash·START/원키/역사호환을반영,기존v1을동일버전으로변경하지않음. Unity 현재공급범위에서공유action·근접접촉/기공탄/이브BASIC총구·저장단일owner 독립연결. 실geometry/socket/새버전소비 선행미충족부분만대기,가짜값제품완료0. 기존미승인 문구는이번범위대체.
최우선 실제5인출격→factory/행동·HP→영속checkpoint→새프로세스복원→결과 계속. 최신49#ordinary-basic-source-contract-20261026 기존공급과대조,계정/run/hash격리·기존응답재사용/원요청복구/표시GET0 유지. 정상/거리·LOS·카일mode전환/요격재개·이브액티브병용/다운/Commit중복·Player/API·3회메모리근거 기존QA/인계에구분. Android연결시검수. WAIT#kyle-eve-basic-followups-20261026 완료기반후속. 미라새W/R안미승인,임의연사추가0.


### BASIC-MIRA-TIMING:UNITY:20261026
정책선행완료44#mira-basic-timing-approved-20261026 / BASIC-MIRA-TIMING-v1.20261026 및 proposal §1~4. W280/R320/C1800ms,기존24m/48m/s/r60mm/500ms·원발수·피해·일반modifier유지. 원발수대조/실발사사건기반표현·공속비례·회수/취소·복원중복방지 구현. Server원정의/START 새version/hash·역사호환,Unity현재공급action/표현/저장독립연결. 상대계약·geometry미공급부분만대기,기존같은버전내용수정/가짜값제품완료0.
실제5인 START→factory/행동HP→영속checkpoint→새프로세스복원→결과 우선계속. 기존응답/Model재사용·계정run세대격리/원키복구,표시GET0. 원발수·공속/차폐·발사전후복원/EN17/일반연구치명·실Player/API정상복구·3회메모리근거는기존QA/인계에구분,Android연결시검수. WAIT#mira-basic-timing-followups-20261026 선행완료후소비통합등록. 새지붕이동-공격재개안은미승인.


### ROOF-MOVE-BASIC-RESUME:UNITY:20261026
선행정책완료44#roof-move-basic-resume-approved-20261026 / ROOF-MOVE-BASIC-RESUME-v1.20261026 및 proposal §1~5. Server 기존저장/원계약·version/hash/계정run복구검증의필요경계만구현,일반전투두번째서버실행엔진신설0. Unity공간예약·action중재/회수후이동·도착후새W/원nextAttack·이브독립장치/동일checkpoint 연결. 현재공급범위독립구현,미공급geometry/계약부분만대기.
실제5인START→factory/행동HP→영속저장→새프로세스복원→결과 최우선계속. latest49 ordinary-basic-v2-v3-contract 공급버전대조·역사원본보존. 기존응답/단일Model재사용,표시GET0/원요청복구/계정run세대격리. 각phase이동·취소·도착/궁극기경합/기발사탄·복원중복0와실Player/API·3회메모리/Android연결시검수. 기존QA/인계증거구분,WAIT#roof-move-basic-resume-followups-20261026 완료기반후속. 새이동지원범위제안은미승인.


### RELOCATION-SUPPORT-SCOPE:UNITY:20261026
정책선행완료44#relocation-support-scope-approved-20261026 / RELOCATION-SUPPORT-SCOPE-v1.20261026 및proposal §1~4. Server능력별공간형/편성형·효과유지조건대조→실source/START버전·저장호환,미분류추정0/기존버전내용덮기0. Unity현재공급범위의객차crossing/원효과평가·표시/단일checkpoint소비. 기발사탄/부착효과유지,오라원tick수혜자갱신,설치/추종구분,구slot·카드불변. 실제통로경계source미공급만대기,임의기준발명0.
실제5인START→factory/행동HP/효과→영속저장→새프로세스복원→결과최우선계속. 기존응답/단일owner재사용·표시GET0·원키복구/계정run버전격리. 실제경계Commit/오라출입·부착효과/설치추종/복원중복0·Player/API정상복구/3회메모리,Android연결시검수. 기존QA/인계근거구분,WAIT#relocation-support-scope-followups-20261026 완료기반후속. 새이동경로무효화안미승인.


### ROOF-DESTINATION-DISPLAY:UNITY:20261026
선행정책44#roof-destination-display-approved-20261026 / ROOF-DESTINATION-DISPLAY-v1.20261026 및 기반2제안절. 현재공급공간owner/Controller에서합법·점유·예약·도착표시와명령1회/취소분리·선택유지/작은가로hitbox목록대안연결. 가짜예약/도착·UI독립원장/표시GET0. 실제5인전투저장복원우선,geometry미공급부분만대기. 큐완료근거기존QA/인계:연타/2요원경합·역순통지·닫기/다운/복원/노즐관통0·구독해제·Player/API메모리. 경로무효화신규정책미승인. WAIT#roof-destination-display-followups-20261026 후속.


### FOUR-SKILL-SCOPE:UNITY:20261026
정책완료44#four-unclassified-skill-scope-approved-20261026 / FOUR-SKILL-SCOPE-v1.20261026 및proposal §1~4. 카일복합effect범위/수락·Commit분리,레이나주대상24m앞뒤1칸,미라매발사현재범위/16발순환을각원definition/version/hash/현재소비경로에결속. Server원scope·START/역사호환,Unity실target/geometry/action·효과/checkpoint. 레이나새광역피해집합은아직미승인,scope공급을전체실행지원승격0.
실제5인제품START→factory/행동HP→영속checkpoint→새프로세스복원→결과최우선계속. 원응답/단일owner재사용·표시GET0·원키복구/계정run세대격리. 정상/경계·차폐/대상소실/미라회전/복원중복0·실Player/API/3회메모리·Android연결시검수,기존QA/인계근거분리. WAIT#four-skill-scope-followups-20261026 후속,경로무효화새안미승인유지.


### REINA-SKILL-DAMAGE-SHAPE:UNITY:20261026
선행정책44#reina-skill-damage-shape-approved-20261026 / REINA-SKILL-DAMAGE-SHAPE-v1.20261026 및proposal §1~4. 액티브단일400%,궁극1000%+3m내최대3명각300%/총4명,추가미사용분주대상재분배0. 동일revision집합확정/24m앞뒤1칸/각LOS·중심차폐/정렬·원피해필터/훈련·면역/사망처리보존. Server원정의새version/hash·START/역사호환,Unity기존DIRECT_DAMAGE/target/공간·효과checkpoint소비. 최신4스킬scope와결속,과거LOCAL단일경로전체덮기0.
실제5인START→전투→영속checkpoint→새프로세스복원→결과최우선계속. 기존응답·단일owner재사용/표시GET0·원키복구/계정run격리. 정상1~5대상·경계/차폐·면역/대상소실·Commit복원·실Player/API/3회메모리,Android연결시검수. 기존QA/인계근거구분,WAIT#reina-skill-damage-shape-followups-20261026 후속. 새미라궁극탄도제안미승인.


### MIRA-ULTIMATE-FLIGHT:UNITY:20261026
정책완료44#mira-ultimate-flight-approved-20261026 / MIRA-ULTIMATE-FLIGHT-v1.20261026 및proposal §1~4. Server궁극정식shot/도착·원snapshot/새version·hash/START호환,Unity기존ACTIVE도착처리의원계약재사용가능범위·궁극회전/shot소비/checkpoint연결. 기본물리탄과혼합0. 실5인START→행동HP→영속저장→새프로세스복원→결과계속최우선. 표적이탈/소실·다운/종료·step/배속/회전/도착전후복원/API정상복구·3회메모리·Android연결시검수. 단일owner·유효응답재사용/표시GET0·원키복구·계정run세대격리유지. 기존QA/인계근거구분,WAIT#mira-ultimate-flight-followups-20261026 선행충족후소비/통합등록. 새이브장치탄도제안미승인.


### EVE-ACTIVE-DRONE-FLIGHT:UNITY:20261026
정책선행44#eve-active-drone-flight-approved-20261026 / EVE-ACTIVE-DRONE-FLIGHT-v1.20261026 및proposal §1~4. Server실장치탄도/version·hash/START호환,Unity기존장치·물리탄·실총구/효과/checkpoint소비. 18m48m/s/r50mm/375ms·구역교집합·적가로채기/차폐/아군비차단,기존일반강화계수·주기/nextShot/생성ATK/1기수명·다운/잔탄귀속유지. BASIC비행체별도,추가BASIC충전0.
실제5인START→전투→영속저장→새프로세스복원→결과최우선계속. 기존응답/단일owner재사용·표시GET0·원키복구/계정run세대격리. 기회/강화경계/파괴잔탄/충돌·차폐·복원중복0/실Player/API정상복구·3회메모리·Android연결시검수. 기존QA/인계근거구분,WAIT#eve-active-drone-flight-followups-20261026 선행충족후등록. 새혼합판정통합시험안은별도제안.


### FIVE-MIXED-HIT-INTEGRATION:UNITY:20261026
정책44#five-mixed-hit-integration-approved-20261026 / FIVE-MIXED-HIT-INTEGRATION-v1.20261026 및proposal §1~3. 지금실행범위는現공급계약/실caller연결·미연결수정·현재조건검증. Server최신relocationv6/basicv3·인증START/원키·영속복구/결과경계,Unity정확버전소비→5인factory/공간·행동HP/탄장치→checkpoint/newprocess/Player. 계약문서반복만으로끝내지않음. 단일owner/유효응답재사용·표시GET0·원키복구/계정run격리,제2전투서버엔진/입장우회0.
전체시험은실geometry/효과consumer/영속저장선행이충족된범위부터. 원제안A~F/정상대조/각phase재실행/결과·로비/API정상복구·3회메모리/Android연결시검수. 기존QA/인계에build/source/정의hash·실입력/실측/미완료구분. WAIT#five-mixed-hit-integration-followups-20261026 선행충족후Plan통합등록. 새혼합판정표현안은미승인.


### MIXED-HIT-FEEDBACK:UNITY:20261026
선행정책44#mixed-hit-feedback-approved-20261026 / MIXED-HIT-FEEDBACK-v1.20261026 및원proposal. 실원사건→기존View/FX/숫자·정보문구연결,물리탄차폐·고정도착·즉시확산구분·가짜명중/피격모션0,원면역/흡수표시유지. 구독·풀수명/잔탄·복원과거FX재생0/표시GET0. 실제5인전투저장새프로세스복원Player검수우선,미공급자산/사건부분만대기. 작은가로/배속·pause·프레임드롭·FX OFF판정동일/구독메모리근거기존QA/인계기록. WAIT#mixed-hit-feedback-followups-20261026 후속. 새집중표적전환표시안미승인.


### FOCUS-CHANGE-ACTION:UNITY:20261026
정책44#focus-change-action-boundary-approved-20261026 / FOCUS-CHANGE-ACTION-v1.20261026 및proposal. 기존focus/action owner·Controller/View에서능력별전환경계/수락마커·선택요원안내/참여가능표시연결,진행target/시계불변·빠른입력최종revision/표식패시브혼동0. 단일owner/표시GET0·구독해제·복원중복0. 실제초기화API→5인factory/action/HP/checkpoint/newprocess/Player최우선. 49#ordinary-initialization-http-contract-20261026의최초receipt/currentstate구분·원key/계정run/hash·실TCP검수. 기존QA/인계에표적전환/실Player/API·3회메모리근거구분,WAIT#focus-change-action-followups-20261026 후속. 새초기화첫저장안미승인.


### INITIALIZATION-FIRST-CHECKPOINT:UNITY:20261026
정책44#initialization-first-checkpoint-approved-20261026 / INITIALIZATION-FIRST-CHECKPOINT-v1.20261026 및proposal §1~3. Server49 ordinary-initialization-http 기존원receipt·인증원key/phase·현재state경계지원,Unity보안영속요청/저장owner·factory/clock조정연결. 원요청기록→POST→receipt검증→초기저장확정→5인구성→개시;live/checkpoint덮기0·초기저장전공격0·동시진입1회·미개시증명없는손상만피복구0.
실제제품API→5인전투→영속저장→새프로세스복원→결과/Player검수계속최우선. 기록쓰기실패/ACK유실/응답후commit전/commit후factory실패/첫전투사건후중단·계정run/원key·hash경계,실TCP·API정상복구/3회메모리/Android연결시검수. 단일owner/원응답재사용·표시GET0/원키복구유지. 공개STARTgate우회0·새원장/서버엔진0. 기존QA근거구분,WAIT#initialization-first-checkpoint-followups-20261026 후속. 새5인부분구성안미승인.


### FIVE-ACTOR-STAGING-FAILURE:UNITY:20261026
정책 선행 완료:44#five-actor-staging-failure-approved-20261026 / FIVE-ACTOR-STAGING-FAILURE-v1.20261026 및 원proposal §1~4. 기존 bootstrap/factory/저장 owner에 부분 실패 정리·동일 checkpoint 재구성·세대 격리·5인 단일 개시 연결. 필수/대체 가능 자산 구분, 첫 저장 보존, 부분 actor 공격/clock 금지, 정상 재구성 추가 API0. 실제 초기화 API→5인 전투 저장/새 프로세스 복원/Player 검수 우선. 실패1~5번째·HP/geometry 연결 실패·연타/늦은callback/계정전환·3회 실패와 정상 메모리 별도 비교. 기존 QA/인계 근거 후 WAIT#five-actor-staging-failure-followups-20261026 소비. AGENTS.md 기존 정보 재사용 준수: 원START/receipt/checkpoint 소유·유효범위 구분, 표시 GET0, 원키 복구, 진행 HP 덮기0.


### COMBAT-VISUAL-FAILURE:UNITY:20261026
정책 선행44#combat-visual-failure-approved-20261026 / COMBAT-VISUAL-FAILURE-v1.20261026 완료. 기존 View/리소스 owner로 장식 생략·기본 자세·대체 표식과 선택 유지·현재 상태로 표시 복구 구현. 판정/HP/탄/드론 재초기화0·과거연출재생0·표시GET0·무한재시도0. 실제 상태 연결 실패는 기존 정지/복구. 미공급 자산만 별도 대기, 실제품 초기화API→5인 전투→저장/새프로세스 복원→Player 최우선 유지. 정상/오류 판정 동일·선택·세대/자원수명·반복 메모리 근거 기존QA/인계에 기록 후 WAIT 후속 충족 시 등록. AGENTS.md 기존 정보 재사용·MVC 준수.


### COMBAT-STATE-FAULT-STOP:UNITY:20261026
선행 정책44#combat-state-fault-stop-approved-20261026 / COMBAT-STATE-FAULT-STOP-v1.20261026 및 원proposal 전체. 기존 실행/저장 owner와Controller로 전체 전투 정지·정상checkpoint보존·원복구·명시 재개1회 연결. 확정사건 유지/불명사건 추측0·정지 뒤 입력 몰아실행0·정지시간 catch-up0·초기receipt 현재HP대체0. 서버 만료/권위 계약 유지, 미결 계약만 별도 대기. 실제초기화API→5인전투→저장/새프로세스복원→Player/API/메모리 최우선. 명중/저장 경계실패·동시탄/장치/포격·연타/계정전환 검수 근거 기존 QA/인계에 기록. AGENTS.md 데이터 원천/단일owner/유효응답재사용/표시GET0/원키복구 준수. WAIT#combat-state-fault-stop-followups-20261026 후속.


### COMBAT-RECOVERY-TERMINAL-RACE:UNITY:20261026
선행 정책44#combat-recovery-terminal-race-approved-20261026 / COMBAT-RECOVERY-TERMINAL-RACE-v1.20261026 및 원proposal. 기존 복구/종료 owner에서 검증된 최종상태 이후 재개 차단·늦은actor/자산완료 정리·결과 확인 연결. 종료불명 원키복구, 표시GET0·새보상지급/승패순서발명0·계정run세대격리. 실제5인 제품 API/저장복원/Player 우선 유지. 종료확정/재개경합·ACK유실/역순·새프로세스·중복보상0·자원수명 검수 기존QA/인계 기록. WAIT#combat-recovery-terminal-race-followups-20261026 후속. AGENTS.md 단일owner/유효응답재사용 계약 적용.


### LANDSCAPE-FIRST-CONTROLS-GUIDE:UNITY:20261026
정책44#landscape-first-controls-guide-approved-20261026 / LANDSCAPE-FIRST-CONTROLS-GUIDE-v1.20261026. 기존 도움말/View/Controller와 계정별 안내owner 확인 후 재사용. 안전한 준비요약/건너뛰기·다시보기/전투중 한항목·위협예고우선·터치가림0·실사용조건/수락상태 기준. 새 API/강제입력/능력/원화 제작0. 미공급 저장계약만 대기. 실제초기화API→5인전투→저장복원·실종료원본 공급/Player 검수 최우선 유지. AGENTS.md 단일owner·유효정보재사용·화면GET0·구독수명 적용. 작은가로/노치/큰글자·계정전환/ONOFF 동일판정 근거 기존QA 기록. WAIT#landscape-first-controls-guide-followups-20261026 후속.


### RAIL-BOMBARDMENT-OUTCOME-FEEDBACK:UNITY:20261026
정책44#rail-bombardment-outcome-feedback-approved-20261026 / RAIL-BOMBARDMENT-OUTCOME-FEEDBACK-v1.20261026. 현재 공급된 실제포격/흡수/피해/종료 사건으로 기존표시 연결; 부족한 원인근거는 미확인/대기, 별도판정 엔진으로 추정0. 전체열차 기준·뒤칸피격/다단/전량부분흡수·경로회피/무력화 구분·원사건중복0·가로 위협표시 가림0·복원과거배너0. AGENTS.md 단일owner/정보재사용·표시GET0·세대/구독해제 유지. 실제초기화API/5인전투/저장복원/terminal producer·Player 우선. 기존QA 근거 후 WAIT#rail-bombardment-outcome-feedback-followups-20261026.


### FIRST-BATTLE-ENCOUNTER-SOURCE:UNITY:20261026
정책44#first-battle-encounter-source-approved-20261026. 지금 범위는 기존encounter/geometry/checkpoint 실제 경로·소유권 식별과 공급된 계약 소비 연결. 정식 Server 계약 미공급 필드 임의생성0·미정배치수치 발행0. 단계/원위협/시간/분기 복원·중복spawn0·정상GET0·정의공유/동적상태수명 적용. 공식소비 선행완료 후 WAIT 후속등록. 실제5인전투API/저장복원/terminal producer·Player 우선 유지. 기존QA에 구현/미연결 구분.

### 첫 학습 배치 원본 상태 계약 소비 — FIRST-BATTLE-ENCOUNTER-SOURCE:UNITY-CONSUME:20261026
Server 선행의 좁은 공급: [49 v1 계약](../superpowers/plans_categories/49_SERVER_API_CONTRACTS.md#first-battle-encounter-source-contract-20261026), [START/검증64PASS·한계](LAST_RAIL_SERVER_2-D-20260913.md#first-battle-encounter-source-result-20261026). firstBattleEncounterSource={packet,sourceHash}, combat.first-battle-encounter-source.v1.20261026. 실제account/run/originHash/formationRevision과canonical hash 확인, DEFINITION_NOT_ISSUED/NOT_EVALUATED/definition및참조null을 실행가능·빈위협·첫계정적격으로 해석하지 않는다. 역사 필드부재와명시null 구분; 기존START재사용·단계GET0. 현재 소비 완료조건은 이 비실행 상태 계약의 파싱/범위/변조/역사호환 검증까지이며 실행 definition schema/적격producer/실geometry 및 시나리오 준비는 별도잔여다. 기존5인/terminal 우선, 새로운stageowner/정의/수치/동적구간 발명0. AGENTS.md 기존정보재사용/단일owner·원키/버전 규칙. 실제배치·Player 선행 모두전 전체통합키 등록0.

### FIRST-BATTLE-COMPLETION-RETRY:UNITY:20261026
정책44#first-battle-completion-retry-approved-20261026. 기존도움말/결과/출격준비 명령 재사용, 안내다시보기와 실재도전 구분·읽음/조작수락/단계/승패 owner분리·강제조작/능숙도점수0. 단계/결과원본 미공급 완료표시 추측0. 공급계약 범위 연결, 표시GET0·계정전환/복원중복0. 실제5인API/저장복원/terminal/Player 최우선. 기존QA근거 후 WAIT후속.


### FIRST-BATTLE-ENTRY-REQUIREMENTS:UNITY:20261026
정책44#first-battle-entry-requirements-approved-20261026. 기존준비화면/출격명령에 공급된 원적격/필수선택기능 표시 연결. 미발행배치/미지원은 사용자편성문제로오표시0·표시GET0·무료기능해금0. 선택기능 미충전은 출격차단 대신 실제사용조건 안내. 미공급 계약은 추측금지. 실제5인API/저장복원/terminal producer/Player 우선. 기존QA/수명·계정run격리 근거, WAIT후속.


### FIRST-BATTLE-PLAYABLE-SLICE:UNITY:20261026
정책44#first-battle-playable-slice-approved-20261026. 실제제품 출격/초기화source→5인factory/action/HP→한쪽적→포격점유判定/노즐→checkpoint/newprocess 실행을 연결. 기존단일owner/실geometry·원API/계정run·수명재사용, 별도fake전투엔진0. 미공급계약만 대기하고 실제호출부 구현 가능한 부분 계속. 같은빌드/정의hash Player 입력·HP·포격·복원근거/API/메모리 기록, slice를전체클리어/정상terminal완료로대체0. 기존QA/WAIT후속.


### FIRST-BOMBARDMENT-TIMING:UNITY:20261026
정책44#first-bombardment-timing-approved-20261026. 기존실열차/포격/입력owner의 길이/위험범위/활성시간/속력곡선·수락시각 원천 추적, 공급된 실제값으로 최초배치 검산/Player 연결. 미공급밸런스값 추측0·직선식으로실충돌교체0. 뒤객차/최저속/다단/배속/복원 확인. 실제5인전투 API/저장복원/terminal 우선. 기존DEV/QA에 정의/geometry버전·실통과/명중근거, 새GET/무한로그0. WAIT#first-bombardment-timing-followups-20261026 후속.


### FIRST-PLAYABLE-NUMERIC-BASELINE:UNITY:20261027
44#first-playable-numeric-baseline-20261027 Plan시험값확정. 기존5인제품owner/실선로·전체열차길이/가감속/사거리·총구 연결계속. gunner2baseHP240각/포대baseHP1000, 단발raw50/6m/8초예고→7.5초발사→8초명중, 전교전잔여위협해소후최소2초. Server정식프로필 미공급field는임의schema복제0. 실제A위치/선로ID와8초해법 계산·Player검수, 불가능하면실근거보고하고gate우회0. 여러객차중첩은raw50한번·흡수/방어원owner, 역사반복포격경로사용0. 저장/새프로세스 사수만피/중복탄·포격0/API·메모리계속. 기존QA/WAIT후속.

### FIRST-PLAYABLE-NUMERIC-BASELINE:UNITY-CONSUME:20261027
선행완료: 44#first-playable-numeric-baseline-20261027,49#first-playable-numeric-profile-contract-20261027,Server#first-playable-numeric-server-result-20261027. 정식profile first_battle_forward_gunners_single_artillery /combat.first-playable-profile.v1.20261027/hash e890b2bdbbf388048d8a2c77d82eb4ecca37c60b4883cf6317a7aaf0a7c1a1ae. 신규START firstBattleEncounterSource v3/구v1v2보존. gunner원정의·HP240×2/DEF53, 포대HP1000/SINGLE raw50사건당1회/객차곱0·6m·7500launch/8000impact/500flight·위협해소후최소2000ms. scaling.variants는기존risk/progress정확결과, appliedScaling:null이므로실run증명없이I/EARLY가정0·이미적용결과에배율재적용0. 원START/receipt재사용/GET0/계정run/hash·원키복구, AGENTS.md 기존정보재사용절 적용. QA52PASS+focused1중복, 원init export v3갱신/compactSTART62164bytes. 소비완료조건: exactv3/profilehash/역사소비·실owner의현재HP/사건1회/원clock/checkpoint결속 및실선로/가감속/위험A 공급후8초해법·Player/API/새process/메모리검증. 운영원본없는부분만대기·가짜값/지원flag우회0, 전체terminal 별도잔여. WAIT#first-playable-numeric-baseline-followups-20261027 AND충족후Plan통합등록, 현재는수치소비선행만완료.
FIRST-PLAYABLE-NUMERIC-BASELINE:UNITY-CONSUME:20261027 추가 공급: 49#first-playable-initialization-binding-20261027 /Server#first-playable-initialization-binding-result-20261027. 실제초기화영수증firstBattleEncounterBinding={version,originHash,sourceHash} additive, 동일batch저장/원키복구. 원START의동일3값과typed대조하고기존영수증/checkpoint재사용, 과거필드부재를최신값으로채우지않음. 원profile복사/새GET0. export파일갱신/과거755검수hash그대로재사용불가. 40+신규1PASS/type·lint0. 이binding은배율·실행/terminal검증증거아님, 현재진행배율안미적용.

### FIRST-PLAYABLE-PROGRESS-SCALING:UNITY:20261027
정책44#first-playable-progress-scaling-approved-20261027. Server정식결속소비→실factory/action/포대에동일variant 전달·중복배율0·현재HP복원보존·null을1배대체0. 미공급wire추측금지. 실제선로/5인전투/저장새process/Player/API메모리계속, II/MIDDLE 소수25.3/63.25의원pipeline·4/5/8/9경계·사수포대동일band/기발사불변 확인. 기존QA/WAIT후속.

### FIRST-PLAYABLE-PROGRESS-SCALING:UNITY-CONSUME:20261027
선행완료44#first-playable-progress-scaling-approved-20261027 /49#first-playable-progress-scaling-contract-20261027 /Server#first-playable-progress-scaling-result-20261027. 원STARTv4 selectedRisk+policy 및기존검증ARRIVE receipt.firstPlayableScaling v1.20261027 공급. account/run/encounterId/nodeId/depth·진행revision/hash·START/initreceipt/source/profile/risk정의hash+exact applied최종HP/raw+각1회marker+bindingHash를원응답에서소비(후속GET0). 실제published학습profile node만/0·12·15wave임의EARLY0, v1~v3역주입0. 사수2명→포대같은binding 공유·재곱/현재HP만피복원0, 다른node실진입만새결속. QA 관련80+수정focused1=고유81통과범위/합성도착proof·내부START, Player아님. 실제factory/공격·포격/영속checkpoint typed소비→실선로/가감속/현재HP·소수·ACK/경합/새process/Player검증후 WAIT#first-playable-progress-scaling-followups-20261027 Plan통합등록. 기존원상태/응답/원키복구·AGENTS.md 기존정보재사용절준수, 전체terminal잔여와구분. 새rail-impact-health-commit 상세미승인안적용0(기존승인원자반영은계속).

### RAIL-IMPACT-HEALTH-COMMIT:UNITY:20261027
44#rail-impact-health-commit-approved-20261027 승인. RailHazard rawintent/receipt를 실제제품 damage host→기존방어/실드/HP→checkpoint에 연결. 동일receipt 재반환만으로HP중복방지완료0, 전객차중첩피해1회·배율재곱0·전량실드HIT완료·HP0terminal1회. 실패전중후/중복충돌/새process 시험과 실제Player 검수. Server 새wire 미공급 추측0, 기존소유권/응답/원키복구 재사용·표시GET0·매피격저장신설0. 단계배율v4/ARRIVE receipt 실제 소비와 실선로5인/저장복원/API메모리 우선. 기존QA 근거 후 WAIT#rail-impact-health-commit-followups-20261027 후속.

### RAIL-IMPACT-HEALTH-COMMIT:UNITY-CONSUME:20261027
선행계약변경검증완료:49#rail-exact-hazard-contract-20261027 /Server#rail-exact-hazard-server-result-20261027. UNITY-PROGRESS-RATIONAL-BOUNDARY 해결용 hazardv2 SINGLE: definition.damage/result.rawDamageIntent canonical positive exact {numerator,denominator}, MISS0/1; 나머지frame/identity/교차/시간/restore 원v1동일·정수v1보존. profilev2/hash d8db615dfb8beef8aaca82a76544d3e2427a52970da15df9166f74af784ea4f3→STARTv5→기존ARRIVE firstPlayableScaling exact선택결과. v1~v4 과거producer/reader유지, 새run만v5/현재배율재곱0. 원source/receipt/checkpoint재사용·GET0, AGENTS.md 기존정보재사용절 적용.
수용: typedv5/profilev2/원정의hash·scope를검증후 exactraw를기존방어/실드/HP pipeline에1회전달, hazard해결/HP·실드 같은checkpoint·중복/충돌/부분저장거절·실HP0 terminal1회. replay raw재반환을피해재적용으로사용금지. 253/4 방어전절삭0, 기존마지막반올림. QA53PASS는Server원본/도착/소수owner·복원·기존numeric계산이며실Unity/Player아님. 실제제품checkpoint/terminal 원본을기존인계에공급하여Server저장검증경계연결필요. 실제선로/full5/새process/Player/API/메모리 및 WAIT#rail-impact-health-commit-followups-20261027 AND충족후Plan통합등록. 미승인포대파괴/발사상세안 적용근거로사용0.

### ARTILLERY-LAUNCH-DESTRUCTION-RESTORE:UNITY:20261027
44#artillery-launch-destruction-restore-approved-20261027 승인. 실제launcher→원발사확정/포대파괴→잔탄/선로경고→명중단일HP반영/checkpoint 연결. 7500ms경계는기존권위순서, 미정순서추측0. 발사전후/해결후복원 생성/피해1회, 실제취소는정상원결과·강제재발사0. 최신소수hazardv2/profilev2/STARTv5/ARRIVE 배율을실제품소비, 조기floor/재곱0. 기존owner·원응답/복구재사용, false지원승격0. 선로5인전투·저장새process·Player/API/메모리 우선계속. 미지원열차사격은강제해금없이미확인분리. 기존QA후 WAIT 후속.

### ARTILLERY-LAUNCH-DESTRUCTION-RESTORE:UNITY-CONSUME:20261027
49#rail-original-restore-train-source-20261027 /Server#rail-original-restore-train-result-20261027 계약변경선행공급. START ordinaryTrainSource v1.20261027 packet/sourceHash: 원account/run/ordinaryOriginHash/ownedSourceHash/loadoutRevision·trainId/level/revision·원base정의hash·permanentStats(maxHp/attack/DEF/basicInterval). 기존owned snapshot/CAS에서생성·현재계정성장으로재계산0. 초기화receipt ordinaryTrainBinding(version/ordinaryOriginHash/sourceHash)와원START동등소비·기존원본/receipt/checkpoint재사용·후속GET0. 이는currentHP/실드/일시효과반영아님, 초기화5요원health외train target 없음; 임의sixth/만피복구0. 실제trainowner와서버초기상태연결잔여를기존인계에공유.
ExactHazard.restoreBound(snapshot, separately retained originalDefinition) 서버대조경계추가·v2wire불변. Unity기존원definition대조restore를재사용가능하며원입력자체를trusted original로동시사용금지. 6종자가일관위조거절/영수증복원12PASS·START/초기화포함68PASS+기존train/finish19PASS(13중복). 새export63184bytes/이전766 hash증거재사용0. 실제launcher발사전취소/후잔탄/같은checkpoint·원HP/실드/terminal 및새processPlayer/API/메모리 검증후 WAIT#artillery-launch-destruction-restore-followups-20261027 AND통합등록. AGENTS기존정보재사용/원키·계정run·역사호환, 임의새상태DTO·전투엔진·추가GET0. 미승인shield상세제안적용0.

### RAIL-SHIELD-IMPACT-BOUNDARY:UNITY:20261027
44#rail-shield-impact-boundary-approved-20261027 승인. 실제제품 명중→그시각열차방어/실드→HP·hazard/checkpoint 연결. 효과생성/만료 경계와 원권위순서, 연출기준판정0·MISS피격소비0·복원재충전0. 소수raw 조기floor/중복배율0. 최신ordinaryTrainSource/binding와exact/ARRIVE소비, 선로5인전투·저장새process·Player/API/전체전투메모리 계속. 미공급초기HP/실드원본은추측하지않고 정확의존만기록. 기존QA/WAIT후속·원응답재사용/GET0.


### TRAIN-INITIAL-HEALTH-RESUME:UNITY:20261027
44#train-initial-health-resume-approved-20261027 승인. 공급된권위초기열차상태→실train owner/첫checkpoint/화면, 진행복원은검증된현재HP·실드·원시계우선. 영구maxHP를임의currentHP복사0·역사누락만피/0대체0. 미공급wire추측없이 기존복원/세대격리·수명재사용. 소수hazard/profile/START·ARRIVE배율 실제소비와 선로5인·HP/잔탄저장새process·Player/API메모리 우선. 기존QA/WAIT후속, 추가GET/독립UI원장0.

### TRAIN-INITIAL-HEALTH-RESUME:UNITY-CONSUME:20261027
선행완료 Server 초기 거래/영수증:49#train-initial-health-contract-20261027 /Server#train-initial-health-result-20261027 /QA train-initial-health-reviewed-20261027.json 55PASS. 새ordinaryTrainSource v2는승인초기화정책/현재NO_ACTIVE_DEPARTURE_EFFECTS pin, v1원본보존. 기존인증초기화receipt.health는 요원5+실제trainId HP6target, trainInitialState v1={accountId,runId,trainId,ordinaryOriginHash,sourceHash,healthTargetId,healthRevision1,stage INITIAL_BEFORE_COMBAT,logicalTimeMs0,activeShields[]}; HP/rebase는동일health target 소유. source/binding/전체원response hash 대조, 원응답재사용·GET0. 원source없는역사값0/만피fallback금지. 현재승인출격효과없는범위만지원, 임의자동실드발동0.
수용: 실제trainowner를원초기화6target에서생성, 같은원시계/첫checkpoint와결속, 진행checkpoint 우선·다음node/재진입/원키재전송 초기HP/실드재부여0. 계정강화/교체로진행run재계산0; 실선로5인/포격방어실드HP/저장새process/Player/API·메모리검증. 기존 원키/계정run/정의버전·AGENTS.md 정보재사용계약 유지. consumer export ordinary-initialization-consumer-source-20261026.json 최신바이트는 합성계정/내부START·인증HTTP이며 Unity/Player 완료아님. 실제진행checkpoint/terminal 원본을 기존인계에 공급하여 Server 검증경계 연결. WAIT#train-initial-health-resume-followups-20261027 전체AND 전Plan통합등록0. 미승인 train-first-checkpoint-interruption 신규상세안 적용0.

### TRAIN-FIRST-CHECKPOINT-INTERRUPTION:UNITY:20261027
44#train-first-checkpoint-interruption-approved-20261027 승인. 실제5인+열차+필수전장 및 첫유효영속checkpoint 확인 후 전투시계/피해개방1회. 응답전후/쓰기중/commit직후/기록정리전/첫피격후 새process 복구검증. pending/지연callback이진행HP덮기0·초기실드재지급0·손상저장만피fallback0. 기존저장/복구/세대격리 재사용, 새wire추측0. trainInitialState/소수hazard/배율 실owner연결과실Player/API메모리 우선계속. 기존QA/WAIT후속.


### TRAIN-OPERATOR-CONCURRENT-DAMAGE:UNITY:20261027
44#train-operator-concurrent-damage-approved-20261027 승인. 실제6target owner/정식대상/피해·실드·다운/terminal/checkpoint 연결. 배열위치/선택창으로대상추론0·열차포격요원복제0·같은tick기존순서·미정승패추측0. 같은revision사건/상태저장·후발중복무효·새process대조. 열차초기HP/소수피해/단계배율 실제품→선로5인→Player/API/메모리 우선. 기존QA/WAIT근거, 새wire추측/표시GET/복제원장0.


### RAIL-IMPACT-POSITION-TIME:UNITY:20261027
44#rail-impact-position-time-approved-20261027 승인. 실제motion/geometry owner→원impactAt 전체객차점유→원단일피해→checkpoint 연결. 저프레임/배속/정지에서도원명중시각·목표속도즉시반영/렌더위치대체/단발sweep0. 후미피격·다객차피해1회·분기객차별span. 실제선로/가감속 공급없는부분만대기하고확정호출부구현계속. 열차초기HP/소수raw/배율→실5인·저장새process·Player/API메모리 우선. 기존QA/WAIT, 새GET/복제원장/지원flag우회0.


### ORDINARY-RAIL-FIRST-PROFILE:UNITY:20261027
44#ordinary-rail-first-profile-approved-20261027 첫시험수치승인. actual ordinary 선로/출격물리편성→motion/span/노즐/명중host 연결가능부분계속. 정식wire미공급추측0·기존기하충돌확인·5요원으로5객차발명0. 공급수치로가감속/최저최고속력·최소회피해법검산, 예시125~131앵커강제0. 실제trainInitialState/소수/ARRIVE배율·5인→checkpoint새process→Player/API메모리검수. 기존QA/WAIT후속, 신규공통엔진/원장/추가GET0.


### ORDINARY-PHYSICAL-CONSIST:UNITY:20261027
44#ordinary-physical-consist-approved-20261027 승인. 기존BPhysicalAuthoringV01 scene/prefab/측정/단위/rail endpoints에서실차량매핑/기관차기하/선로길이·초기위치를확인하고원선택에소비연결. 기존자산중복제작0·10m초안을승인12m로근거없이승격0·미승인스케일변경0. Server에필요한정적원본경계는기존QA로공급해상호대기만반복하지않음. 실motion/span→hazard/6targetcheckpoint/Player/API메모리우선. 실출격목록재사용·GET0/미공급wire추측0. 기존QA/WAIT후속.


### ORDINARY-CARRIAGE-12M-ALIGNMENT:UNITY:20261027
44#ordinary-carriage-12m-alignment-approved-20261027 사용자승인. PhysicalConsist793 실제측정/기존BPhysicalAuthoringV01 재사용해 원본보존한12m변형 제작·차체길이/지붕/충돌/접속/역할별anchor 정합. 폭높이/요원/이펙트전체확대0. 실제START차종 매핑과 기관차별도기하를 식별, 5template를5인편성으로간주0. 신규버전/hash/측정·실표시 근거 기존QA에 공급하여 Server결속 가능하게 함. 기존선로100m 부족은 신규미승인선로안으로임의확장하지않고 독립자산작업진행. 실선로5인전투/저장새process/Player/API메모리 부모계속. 완료근거후 WAIT#ordinary-carriage-12m-followups-20261027 등록규칙 적용.


### ORDINARY-FINITE-RAIL-CAPACITY:UNITY:20261027
44#ordinary-finite-rail-capacity-approved-20261027 승인. 기존기하계열 원본보존한 실제최소500m직선/경고/카메라/충돌참조 작성·실측, 실제편성L 기준후미20m·선두20+L 연결. 실제속력효과/예고전이동예산 포함잔여검산, 미확정기관차/ID매핑추측0·5template고정편성0. Carriage12m794 변형/원본근거 재사용. 선로자산은독립제작가능; 최초Player진입은필수물리매핑/버전결속충족후. 500m를무기한전투보장/무한연장으로해석0. 실제선로5인전투/저장새process/Player/API·메모리 우선계속. 기존QA/인계공급 후 WAIT#ordinary-finite-rail-followups-20261027.


### ORDINARY-ENGINE-CARRIAGE-MAPPING:UNITY:20261027
44#ordinary-engine-carriage-mapping-approved-20261027 승인. 기존12m변형재사용해12×3×3.2m 기관차별도역할자산제작, 실제선택차종 매핑/접속/방향/단위·버전hash 공급. 1기관차+원2객차 실제조합38m/초기20~58m 검증, 5template단순사용금지. 서버정식선택/instance계약소비, 기관차임의요원배치권한0·기존5인slot확인. FiniteRail795 500m 자산재사용하되실효과/예고전예산충족별도. 준비/원초기화→실motion/명중/6targetcheckpoint/새process Player/API전체메모리 계속. 기존QA/인계 근거 후 WAIT#ordinary-engine-mapping-followups-20261027.


### ORDINARY-FIVE-ACTOR-ROOF-SPAWN:UNITY:20261027
44#ordinary-five-actor-roof-spawn-approved-20261027 승인. 실제actor→차량/정식slot→anchor/발총구/HP/카드상태연결 및전장준비+첫영속checkpoint 후개시경계·재시도수명·진행복원 구현가능부분계속. EngineConsist796/FiniteRail795 근거재사용·Server정식물리instance/START발행소비. 현재각객차Foot1개를정원1로간주하지않고미정roof별도기록, 신규3+2안미승인적용0. 실제선로5인전투/저장새process/Player/API전체메모리 계속.


### ORDINARY-CARRIAGE-12M:UNITY-CONSUME:20261027
Server 원START railv3 기하결속 공급: 49#ordinary-physical-start-binding-v3-20261027 및 Server#ordinary-physical-start-binding-result-20261027. QA/CommanderS1-20260922/ordinary-physical-binding-compact-20261027.json 77PASS, consumer export ordinary-initialization-consumer-source-20261026.json. EngineConsist796/FiniteRail795 원자산hash와 정확 train_a/combat0/gun1 연결, Server물리32hex ID3개(Engine/Combat/Gun순서), 38m/tail20/head58·500m기하 원정의hash/segment ID 공급. v3정책본문은 v2 definitionVersion/hash 참조로해석; 알수없는참조/다른원run/hash는준비실패, 역사v1/v2소급변경0. 실제instance/기하를기존motion/span/checkpoint 소비에결속하고재진입/저장새process원ID·기하일치검증. status GEOMETRY_BOUND_EXECUTION_UNREADY/initialMotion null: 기하소비가5인roof/실속도·예고전예산·전투개시권한을뜻하지않음. 미지원다른편성은v2unbound, 임의38m/3+2배치금지. 실제Player/API전체메모리·예고전효과이동 검수는부모범위유지. 완료근거는기존Unity QA/인계와WAIT, Plan통합선등록0.


### ORDINARY-FINITE-RAIL:UNITY-CONSUME:20261027
Server 원START railv3 기하결속 공급: 49#ordinary-physical-start-binding-v3-20261027 및 Server#ordinary-physical-start-binding-result-20261027. QA/CommanderS1-20260922/ordinary-physical-binding-compact-20261027.json 77PASS, consumer export ordinary-initialization-consumer-source-20261026.json. EngineConsist796/FiniteRail795 원자산hash와 정확 train_a/combat0/gun1 연결, Server물리32hex ID3개(Engine/Combat/Gun순서), 38m/tail20/head58·500m기하 원정의hash/segment ID 공급. v3정책본문은 v2 definitionVersion/hash 참조로해석; 알수없는참조/다른원run/hash는준비실패, 역사v1/v2소급변경0. 실제instance/기하를기존motion/span/checkpoint 소비에결속하고재진입/저장새process원ID·기하일치검증. status GEOMETRY_BOUND_EXECUTION_UNREADY/initialMotion null: 기하소비가5인roof/실속도·예고전예산·전투개시권한을뜻하지않음. 미지원다른편성은v2unbound, 임의38m/3+2배치금지. 실제Player/API전체메모리·예고전효과이동 검수는부모범위유지. 완료근거는기존Unity QA/인계와WAIT, Plan통합선등록0.


### ORDINARY-FIVE-ROOF-THREE-TWO:UNITY:20261027
44#ordinary-five-roof-three-two-approved-20261027 승인. 기존EngineConsist796 원본보존한버전별실roof5anchor 생성/측정: combat+3/0/-3m,gun+3/-3m/Z0/실roof발Y. 명단1~3/4~5 ID별연결, 기존배치복원우선·기관차배치0. 기존v3소비큐와실5인초기화gate→motion/명중/6targetcheckpoint 연결. 실요원동작/포격장치간섭은존재자산으로검수하고누락을통과로처리0. Server정식결과소비·추가GET0. 실제선로/새process Player/API전체메모리 계속. 기존QA/인계→WAIT후속.


### ORDINARY-FIRST-PLAYER-TRAVEL-BUDGET:UNITY:20261028
44#ordinary-first-player-travel-budget-approved-20261028 승인. 기존FiniteRail795계열원본보존1200m유한변형제작/실측·실속력효과포함42초예산확인, 초과는事前길이상향/새정의. 초기motion20m/s/가감속없음(실출격효과우선), 진행checkpoint속도보존. roof실anchor/ServerID별결과→full5 actor View/공격/기존motion·hazard·6targetcheckpoint 실제연결. Sera slice/합성피해시험을전체5인실전증거로대체0. 30초초과QA관찰기록·제품패배/강제사살0. 실제선로5인전투 저장새process Player/API전체메모리까지계속. 기존QA/인계.


### ORDINARY-FIVE-ROOF:UNITY-CONSUME:20261027
2026-10-28 Unity17 마지막 선행 대조: Server roof source80PASS(49#ordinary-roof-layout-source-v1-20261027) + RailRoof797 실5anchor/reload 근거 및 PlayerBudget798/roof-crosscheck.json 차량순서/X/Z 일치. 원START roof source/hash, 원요원 순서→rail physicalID→실anchor 소비, 초기화 binding/저장 복원 동일성 검증. 실제배치·actor발offset/총구/포격장치간섭·full5 Player/API/메모리 전 완료처리 금지. 기존 원응답 재사용/후속GET0, explicit placement/progress 우선. 등록키 ORDINARY-FIVE-ROOF:UNITY-CONSUME:20261027.


### ORDINARY-FIRST-PLAYER-TERMINAL:UNITY:20261028
44#ordinary-first-player-terminal-review-approved-20261028 승인. full5실공격/선로포격→최종6target/잔탄/checkpoint→기존terminal·확정결과화면실연결. Sera slice를제품5인완료로대체0. 명중직전/최종피해후/terminal후새process복원, 중복피해/지급/초기HP0. 결과닫기소유객체해제·3회API/잔류메모리검수. 정상추가GET0,실1200m/roof798원천재사용·Server확정응답소비. roof구checkpoint호환위험은삭제/만피로회피하지않고잔여유지. 기존QA/인계후WAIT후속.


2026-10-28 기존 ORDINARY-FIRST-PLAYER-TRAVEL-BUDGET:UNITY:20261028의 공급근거 추가(기록키 SERVER-RAIL-V4-CONSUMER-INPUT-20261028): Server#ordinary-player-budget-result-20261028 /49#ordinary-player-budget-source-v4-20261028 /83PASS. 최신서버export는 railv4·policyv3/hash981a453c3f58f5985d3774ba393593dd227415cc9af9051160481f141546e691·798실1200m/hash eeeb...00bc/segment ordinary-first-straight-v2·initialMotion20000mm/s/accel0·MOTION_BOUND_CAPACITY_UNVERIFIED. v4만중복ownedSourceHash생략, 원origin에서재구성. 기존v1–v3불변/진행복원속도초기화0. roofv1새railhash/초기화binding도검증필요. 현재v3전용reader는명시v4소비로확장할것. 실효과42초예산/예고여유/회피·full5/Player완료아님; 이추가는이미승인된부모의구체입력이며 AND미충족소비/통합완료키선등록아님.


### ORDINARY-ROOF-CHECKPOINT-COMPATIBILITY:UNITY:20261028
44#ordinary-roof-checkpoint-compatibility-approved-20261028 승인. 보고된구roof checkpoint/newconfig hash거절경계를원버전별로검증·지원원버전구성복원/명시무손실변환. 변환중단/원본보존/진행HP·속력·배치·공격·잔탄동일성·계정run혼입/변조확인. 최신3+2/1200m/20m/s/만피채움0. actual제품구저장없으면없음명시, QA시험을제품호환으로승격0. full5실뷰/공격/motion/hazard/결과·새process Player/API3회메모리 연결계속,기존owner/GET0. 기존QA/인계·WAIT후속.


### ORDINARY-FIRST-ENCOUNTER-OBJECTIVE:UNITY:20261028
44#ordinary-first-encounter-objective-approved-20261028 승인. 실5인격파/원hazard해결/6targetHP/시계·revision을기존owner에서정식producer로연결하고Server계약소비→구간/노드/작전수준맞는결과표시. full5 actual View/공격/motion/hazard/저장새process Player/API메모리 계속. 포대자동파괴/발사후잔탄삭제/42초승리/가짜종결0. 승리뿐아닌HIT생존/HP0/발사전취소·중복검수. 구버전복원은확보한원본만증거, 없는제품구저장발명0. 기존QA/인계·WAIT후속.


2026-10-28 기존 ORDINARY-FIRST-ENCOUNTER-OBJECTIVE:UNITY:20261028 공급근거(기록키 SERVER-FIRST-OBJECTIVE-SOURCE-V6-20261028): 49#ordinary-first-objective-source-v6-20261028 /Server#ordinary-first-objective-source-result-20261028/85PASS. 최신export sourcev6/profilev3 completion은목표version/hash참조(a5b558a3d1003bf2dd74cbde8430083a1b220d1995fc702e26f475c9d907b012); 별도정적원본 combat-first-encounter-objective.ts. 원사수2/원단발정상해소/열차생존·잔탄0/추가목표보존. 기존v5문자열/구run불변. 현source reader v5전용이면명시v6소비가필요. 이번은원정의결속공급이며실ID/clock/revision/checkpoint증거와정식게시노드전체목표일치검증·Server완료adapter가남아 node/run승리권한발행0. 기존801component를실full5 host/원증거producer로연결하는독립작업계속; 새GET/별도완료원장0. 완료기반고정CONSUME키전체선행완료로주장하지않음.


### ORDINARY-ENCOUNTER-TRANSITION:UNITY:20261028
44#ordinary-encounter-transition-state-approved-20261028 승인. 실제구간전환과노드완료UI/선택분리·기존owner HP/배치/속도/시계/효과유지·終結owner수명해제. full5실host→FirstPlayableArtillery/사수/원checkpoint→Server계약 실제caller연결우선. 단위판정만추가해제품호출부가tests-only인상태를완료로처리0. firstsourcev6/最新export소비·원キー/응답재사용·finishguard우회0. 실제새processPlayer/API3회메모리/결과계속,미확인효과정책발명0.


### ORDINARY-FIRST-OBJECTIVE:UNITY-CONSUME:20261028
고정 등록키 ORDINARY-FIRST-OBJECTIVE:UNITY-CONSUME:20261028. Server계약/검증어댑터공급완료: 49#ordinary-first-objective-evidence-v1-20261028 /Server#ordinary-first-objective-verifier-result-20261028 /QA/CommanderS1-20260922/ordinary-objective-verifier-final-20261028.json85PASS. sourcev6/profilev3·현재railv4/roofv1 원본재사용. 원문코드 server/apps/game-api/src/combat-first-objective-verifier.ts의firstObjectiveEvidenceSchema/firstObjectiveEncounterIdentities. 정책추가가아닌현재승인목표에필요한정확필드/지원범위/기존COMPLETE입력계약.
원account/run/ARRIVE encounterId/sourceHash→안정사수2/단발ID 생성. 실제full5 host에서그ID를기존적·hazard owner에결속, 원사수격파/원포격종결/6target·잔탄·clock/checkpoint revision을같은commit원천으로생성. 현재component판정/QA fixture를제품producer로대체0. 전체START/정적정의재전송/GET추가/별도완료원장0. 진행HP/공격/배치/motion을원START로초기화하지않음.
목표object 참조·정식node전체목표/현재node·원ARRIVE와결속. 같은node추가목표면진행유지, COMPLETE는전체목표일치한경우만기존MapStore거래. 원결과state/revision/holdIds/verificationId재사용·보상대기보존. /finish guard/작전승리우회0.
Server operating FirstObjectiveEvidenceOwner/ingress는미설치이며이계약만으로rawclient증거를권위완료로수락하지않음. Unity는기존실caller/host·checkpoint를연결해원증거를공급하고Server는진위/최신성검증자·원row조건검사를운영원본에연결할책임. 실샘플없음을필드정의대기로다시돌리지않음. 수용: 원ID/중복/순서/HIT·MISS·취소/잔탄/HP0/추가목표/재진입·구간인계 및실저장새process Player/API3회메모리. 일부증거/수송미연결은정확범위로기록하고독립연결계속.


### ORDINARY-COMPLETION-EVIDENCE-FLOW:UNITY:20261028
44#ordinary-completion-evidence-flow-approved-20261028 승인. 이미Server발행evidence-v1/기존소비큐재사용해실full5 host/View/공격/motion/hazard/checkpoint producer/caller→확정응답→결과연결. Sera slice와tests-only component를전체실행으로대체0. 원계정/run/node/사건/시계/revision·최종6target/잔탄근거일치, 원키복구/모델반영후표시·추가GET0. 실제새processPlayer/API3회메모리계속. Server권위owner구현과Unity실host잔여를분리해독립작업진행.


### ORDINARY-COMPLETION-PENDING:UI-UNITY:20261028
44#ordinary-completion-pending-ui-approved-20261028 승인. 기존MVC/완료요청owner에대기표시/입력차단·정보열람·원요청복구·로비이동/늦은응답계정화면세대처리연결. 전체노드전이만적용·미해결전투강제완료0. full5실host/원사건producer→기존Server검증/확정응답→결과 우선,UI만완료전체주장0. 실선로/저장새process/Player/API메모리계속. 미승인적열차배치후속값적용0.


### ORDINARY-ENEMY-TRAIN-SPATIAL:UNITY:20261028
44#ordinary-enemy-train-spatial-approved-20261028 승인. 실제적단일차체/사수2실anchor·총구·hit영역 생성/원서버정의소비,아군actualmotion동행4mgap·적선두포함유한선로검산. 실full5 host/View/공격/LOS·기관차차폐·원범위대기/재탐색·hazard/저장새process연결. 공간원천미결해소범위로진행하되실총구/모델없음은별도기록·사거리확장/가짜처치0. 실Player/API3회메모리계속·기존QA/인계.


### ORDINARY-COMPLETION-INGRESS-TRUST:UNITY:20261028
44#ordinary-completion-ingress-trust-approved-20261028 승인. 기존49#ordinary-completion-ingress-v1-20261028 POST completion strict nodeId/evidenceRef/expectedRevision+원key를실full5 producer/caller에서소비. 접수/진위검증/노드완료분리·없는증거생성0·원영속기록/ACK유실복구. actualmotion/적열차805/원사수/공격/LOS/6target checkpoint·새process Player/API3회메모리계속. 적총구모델미확인은확인범위/필요원천으로기록,확정독립host구현을그대기로돌리지않음.


### ORDINARY-START-SIZE-BUDGET:UNITY:20261028
44#ordinary-start-size-budget-approved-20261028 승인. Server최소원본변경계약대조후reader/원hash/영속journal/checkpoint소비·구버전유지. 이미보유정적정의해결·누락준비차단,rawJSON화면별복제/불필요장기참조정리·필수복구원본보존. 실제full5 host/View/공격/motion/hazard/完了caller/Model sink/pending UI/새processPlayer/API3회메모리계속. mockHTTP/base6481PASS는실통신증거아님. 기존QA/인계·추가GET0/상한무단변경0.


### ORDINARY-DEFINITION-LIFETIME:UNITY:20261028
44#ordinary-definition-reference-lifetime-approved-20261028 승인. 보유 카탈로그/자산/캐시→확정 START 참조 대조·누락만 기존 필수준비 경로 확보, 원버전 검증·첫 저장 후 개시. 공유 자산 수명/원run 복원 보존·화면 복사본 해제, 신규 무제한캐시/GET0. 실제 full5 host/View/공격/motion/hazard/완료caller/모델반영·저장 새process Player/API3회메모리 계속. DEV만 출처/취득사유/해제 기록.


### ORDINARY-POST-START-RESOURCE-RECOVERY:UNITY:20261028
44#ordinary-post-start-resource-recovery-approved-20261028 승인. 원확정run/영수증 보존→실필수준비재시도/로비복귀/새process·개시1회 연결. Server49#ordinary-start-size-source-v7-20261028/QA ordinary-start-reference-definitions-20261028.json 및최신export 공급됨: firstsourcev7/profilev4/scalingv2 원정의참조해결·shape/reader/동등검증소비 우선, 구reader호환유지. 지원최대미검증을별도남기고준비된계약소비를막지않음. actualfull5 host/공격/motion/hazard/완료caller/sink·Player/API3회메모리계속.


### ORDINARY-STORAGE-ENVELOPE-BUDGET:UNITY:20261028
44#ordinary-storage-envelope-budget-approved-20261028 승인. actual START→lookup→journal/문자열escape→실보안저장 경계크기/단위확인·v7실reader소비·최대/동일/초과/손상/구읽기/새process 검수. mockbase64와실암호화구분·실행플랫폼만보고·원pending보존. DEV진단복붙가능/비밀payload0. 이검수와독립full5실host/View/공격/motion/hazard/완료caller/sink 실제연결계속,실Player/API3회메모리까지.

ORDINARY-STORAGE-ENVELOPE-BUDGET:UNITY:20261028 부분근거: LAST_RAIL_Unity_2-D-20260913.md#unity17-storage-envelope-810-20261028. Windows실파일/실serializer+base64대역36PASS,실Android기기0;native새process/최대편성/실DEV화면/full5통합 미완료로 큐유지. 512KiB envelope와32행한도별개,고정v7누적7번째한도초과/원pending보존 확인.


### ORDINARY-STORAGE-PRESSURE:UNITY:20261028
44#ordinary-storage-pressure-recovery-approved-20261028 승인. 실제오류원인분류/앱소유비필수정리대상검증→안전정리/기존저장재시도·원복구자료보존/공유수명/원run재개연결. 테스트는격리QA·프로젝트전체삭제0. v8최신export/definitions.firstBattleStatic 공급됨: 49#ordinary-storage-envelope-source-v8-20261028 원schema/참조/hash소비/구1~7유지·실저장검증. full5 actualhost/View/공격/motion/hazard/완료caller/sink·새processPlayer/API3회메모리계속,Android미연결은분리.

ORDINARY-STORAGE-PRESSURE:UNITY:20261028 부분근거: LAST_RAIL_Unity_2-D-20260913.md#unity17-static-reference-811-20261028 및 #unity17-storage-pressure-812-20261028. v8원정의/구reader/원문저장소비검증,파일실패분류→기존출격재시도Controller 연결. 실제단말diskfull/안전정리가능대상/Player/full5 미완료로큐유지. 타계정미결삭제/신규계정수제한0.


### ORDINARY-MULTI-ACCOUNT-JOURNAL:UNITY:20261028
44#ordinary-multi-account-journal-retention-approved-20261028 승인. 원계정/run별기록소유·반영/보관종료검증후에만기존정리경로소비, 미결/타계정원키보존·일관쓰기/실패복구. 정리대상입증불가면삭제추가0·계정수제한0. 독립full5 actualhost/View/행동/motion/hazard/완료caller/sink 조립우선계속; 저장세부시험만누적하며실전연결대체0. 실선로/저장새processPlayer/API3회메모리까지.


### ORDINARY-FULL-FIVE-ASSEMBLY:UNITY:20261028
최우선:44#ordinary-full-five-player-assembly-approved-20261028. 사용자조사만하지말고실화면/전투실행부구현지시. 기존제품출격View입력→상위Controller→ordinary준비/usecase 실제caller를추가하고, 원6target/정의/행동소유를사용하는5인실행조정·HUD표시계약을실제구현. Sera전용factory/ReadHudState거절경계를단순우회하지말고일반5인책임으로연결; 독립입력/표현/도메인/저장분리·기존owner재사용. 첫코드연결단위를완료하고관련검증/Player빌드실행, 이어원motion/hazard/완료producer-caller/sink·저장새process/API3회메모리. 확정독립구현을모델/Server전체완료대기로보류0. 실제누락자산/미지원범위정확표기·mock전체완료0. 기존QA/인계에수정파일/실경로/Player근거기록.


### ORDINARY-FIVE-HUD-CONSISTENCY:UNITY:20261028
44#ordinary-five-hud-state-consistency-approved-20261028 승인. 실제full5 실행owner/ReadHudState 표시계약→CombatPlayerScene/ApprovedCombatView와카드/전장선택콜백연결. 814 준비버튼만연결한단계에서계속실행조정/HUD거절원인수정·gate우회0. 원행동/HP/총구/ID/저장결속,실Player빌드실행·새process복원/API3회메모리까지. Server검증실행방식결정과독립된클라코드진행·조사/테스트추가만종료0. 기존QA/인계.


### ORDINARY-SHARED-VERIFIER:UNITY:20261028
44#ordinary-shared-combat-verifier-approved-20261028 승인. 기존실행도메인/행동owner를공유재현가능하게최소분리·표현/입력/저장경계유지,Server입출력계약에실START/ARRIVE/정의/시드·입력으로동일결과재현연결. 전체신규엔진/전원Sera화0. 815 Player연결은tick0 AWAITING_ENCOUNTER만검증이므로실ARRIVE/motion/기본공격/포격/완료까지계속; 한줄시각배치를승인3+2실차량anchor로수정. 실제행동/저장중단새process/동일코어재현/PlayerAPI메모리검수. 독립클라작업지속·코어편집권단일화.


### ORDINARY-REPLAY-INPUT-RESUME:UNITY:20261028
44#ordinary-replay-input-resume-approved-20261028 승인. 도메인실명령→원tick/seq기록/서버수락prefix후잔여저장·재전송/동일checkpoint연결, 새로운프레임별원장/GET0. SharedCore816 동일코어재현/3+2Player부분근거재사용, tick0gate에서실ARRIVE/기하/실행행동·motion/hazard/완료까지구현. 서버runner와공유규칙/버전단일원천맞춤,현재seed1고정은권위시드아님. 새process실전/Player/API3회메모리계속.

## U-NODE-START-CAPTURE-PREP-20261028 — 기존 재입장 작업 상세 승인 보충
원본44#battle-node-start-capture-approved-20261028 / BATTLE-NODE-START-CAPTURE-v1.20261028. 기존49 restart 계약으로 확정된 새 host 수명/이전 callback 격리/미결 요청 복구 분기를 진행하고 전투 중간 checkpoint를 이어가는 작업은 확대하지 않는다. 실제 초기 snapshot producer/ARRIVE 세대 wire는 Server 발행을 기다리는 해당 부분만 보류하고 임의 필드/현재 HP 대체 금지. 선행 완료 후 WAIT의 U-NODE-START-CAPTURE-INTEGRATION-20261028로 실제 원본 소비·Player 검수를 연결한다. 실제5인 화면/행동/선로/포격 독립 작업 계속, 사용자 콘솔 숨김·기존 정보 재사용 규칙 유지.

## U-NODE-REENTRY-READY-UI-20261028 — 승인된 준비 화면·전투 개시 연결
44#battle-node-reentry-ready-ui-approved-20261028 / BATTLE-NODE-REENTRY-READY-UI-v1.20261028 및 앞 proposal1–9. 기존 NodeEntry818 수명 처리/NodeWire819 계약 소비를 재사용해 안내·중복 입력·취소·오류·foreground 개시 조건을 연결한다. 확정된 독립 UI/수명 처리는 진행하고 실제 runtime producer 의존 새host/개시 검수는 기존 WAIT 후속 조건으로 연결. 첫 렌더 상태와 실제 시작HP 일치, 준비 중 시계/피해0, 늦은응답 자동열기0, 자산재시도 추가restart0, 배경중 자동개시0을 Player에서 검수. API/메모리3회와 실제5인 전투 작업 유지. mock/합성은 별도 표시; 원본 누락 임의대체 금지. 완료 근거는 기존 Unity 인계/QA에 보존한다.

## U-NODE-LEAVE-INTENT-20261029 — 승인된 독립 메뉴·수명 처리 착수
44#battle-node-leave-intent-approved-20261029 / BATTLE-NODE-LEAVE-INTENT-v1.20261029. 기존 pause/로비/포기 Controller 재사용으로 메뉴구분·확인취소·touch-through차단·전장해제·늦은응답격리를 구현. 새 endpoint/즉시재시작/무료재도전0. 원nodeStart/미결요청 유지, 이탈시restart0. 실제 재입장·종료 경합 통합은 기존 source 선행 충족 뒤 검수; 해당 부분만 대기. 최신 승인 자산·객차/실제ARRIVE→교전Player 작업 우선 유지. 821의 화면연결과 API0/tick0는 실제전투완료 아님. 기존QA/인계에 구현·검수 근거 보존.
Unity822 부분 구현: 기존 인계#unity17-leave-intent-822-20261029 / QA/CommanderS1-20260922/LeaveIntent822. 확인취소/뒤로가기/one-shot 이탈/입력차단/수명 해제 구현, scoped9PASS. 실제 포기 producer·서버 결과 경합·node초기HP 재입장 미완료로 항목 유지. UI callback 시험을 실제 포기 원키복구로 취급하지 않음.

### 기존 실제5인 연결 작업 수용조건 보충 — FIRST-LIVE-ENGAGEMENT:UNITY:20261029
44#first-live-engagement-acceptance-approved-20261029 사용자 승인. 기존 full-five/node-start/자산 연결 작업에 실제제품 진입→5인행동→피해/지원→포격→종료→노드시작재입장·동조건3회 검수 적용. 실제영상·동일실행서버근거·소스/빌드/자산버전 보존. 준비카메라/API0 합격과실교전 분리. 선행미완료는 기존WAIT, 새막힌통합큐 생성아님. 최신승인자산연결과runtime/도착caller 실제구현 우선계속. 실패수정우선순위 제안은 아직미승인.
Unity823 부분: 기존 인계#unity17-node-origin-823-20261029 / NodeOrigin823/green.xml 75PASS. 49 확정 원seed 계약을 accepted nodeStart 입력 경계에서 검증·보존; 실제 full-five 실행기는 아직 해당 원seed/초기effects/actions를 소비하지 않으므로 실제교전/재입장 수용 미완료. 기존 원천·host 연결 작업 유지.

### NODE-INITIAL-ACTION-INPUT:UNITY:20261030 — 기존 행동소비 작업 보충
44#node-initial-action-input-approved-20261030 사용자작업지시. 기존원본/행동owner에서준비중시계·예약0, 개시후첫기회/자동사용조건, 자동·수동소비1, 원초기상태재입장/같은메모리pause유지 구현·검수. 실actions원천 미공급은 해당통합만기존WAIT로분리, 독립명령/표시경계진행. 실제destination/도착검증/nodeStart→5인교전·포격·종료/최신자산연결이최우선. 새표적소실대조안은미승인.
Unity824 부분: 기존 인계#unity17-initial-actions-824-20261030 / InitialActions824 green17PASS·Player표시3회. 미공급 charge/cooldown을null/미확인으로표시, 실제actions소비·자동수동발동·교전은미완료. 내부준비placeholder/실행gate유지, 전체큐유지.

### SKILL-TARGET-LOSS-STAGE:UNITY:20261030 — 기존 행동 실행·표현 보충
44#skill-target-loss-stage-approved-20261030 승인. 원스킬별 단계와 표적 소실/세대변경 대조, 발사전 불발 표현/기발사탄 원계약·소비1회 연결. View에서 환급·재표적·피해 판정 신설0. 제품 출발/도착 호출부와 nodeStart→실5인 행동·포격·종료/재입장 Player 연결 최우선. 미공급 producer는 기존 WAIT로 분리하고 독립 구현 계속. QA/인계에 실제 실행/합성 시험을 구분. 이동시간 검증 새 제안은 미승인.

### TRAVEL-ARRIVAL-TIME:UNITY:20261030 — 기존 제품 이동·도착 연결 승인 보충
44#travel-arrival-time-verification-approved-20261030 승인. 실제 제품 경로 선택/이동 입력·시간 원천을 기존 작전세션/지속 명령에 연결, 서버 확정 ARRIVE/nodeStart 소비로 실5인 전투 개시. 로컬도착을 권위증거로 승격0, 준비 중 전투시간 진행0, 성공 후 확인GET0. 미결 추가 wire는 Server 49와 맞추고 기존 WAIT 사용. 실제 교전/노드시작 재입장·Player API/메모리 검수 우선. 도착 지연 화면 보충안은 아직 미승인.

### TRAVEL-COMMAND-ACCEPTANCE:UNITY:20261031 — 비전투 이동 입력 연결
44#travel-command-acceptance-approved-20261031 사용자 승인. 노즐 확정값 전송·미결속도1개·정지의도 보존·예정/확정값 구분을 기존 Controller/이동 소유자에 연결. 확정49 wire 공급 후 원키/응답/서버 적용시점 소비, 미확정 구간 자동도착0. 전투 중 회피 노즐에 일괄 적용하지 않는다. 추가 복구 기준점 화면 세부안 미승인. 실제 제품 로비/지속 원요청/ARRIVE/nodeStart→5인 실행과 Player 검수 우선. Server 선행 의존은 기존 WAIT, 확정 독립 작업 진행; 합성 검증을 실교전 완료로 보고하지 않는다.

### TRAVEL-RECOVERY-BASELINE:UNITY:20261031 — 기존 복구 화면·상태 연결 보충
44#travel-recovery-baseline-approved-20261031 승인. 권위 기준점과 미결 원요청 복구·확정 도착 우선, 미확정 위치/입력 폐기·편성/HP/경제상태 보존, 정지 준비화면의 계속 이동/로비 연결. 기존 Controller/상태 소유자 재사용, 실제49 공급 후 소비; 선행 의존은 기존 WAIT로 한정. 실제 로비→이동→5인 교전/노드시작 재입장 Player·API·메모리 우선. 새 재개대기 세부안 미승인, 기존 합성 검사와 제품 실행 증거 구분.

### TRAVEL-RESUME-PENDING:UNITY:20261031 — 기존 재개 입력/영속 수명 보충
44#travel-resume-pending-approved-20261031 승인. 원요청 송신전 영속화/실패 미송신·미결 재개1개·원결과 복구·늦은응답/화면세대격리·충분한응답후GET0 적용. exact49 공급 의존은 기존 WAIT에 두고 미연결 별도원장/추측wire 신설0. 실제 제품owner/도착/nodeStart/5인 실행 연결 우선. 기존 승인된 독립 구현 진행, 새 통합 시험 보충은 미승인. QA에 실제와 합성 구분.

### TRAVEL-TO-COMBAT-INTEGRATION:UNITY:20261031 — 기존 Player 검수 보충
44#travel-to-combat-integration-approved-20261031 승인. 실제 로비→속도/정지/재개→도착→5인행동과 재개응답유실/새프로세스 복구를 기존 통합검수에 포함. 기존 DurableRunClient 재사용과 exact49 소비로 제품 연결; 추측 endpoint/별도원장0. 실제 종료/노드시작 재입장·동조건3회/API와메모리 검수 유지, 정상/복구 분리. 선행은 기존 WAIT. 새 도착직전 경합 제안 미승인.

### ARRIVAL-CONTROL-ORDER:UNITY:20261031 — exact49 소비 및 입력 경합
44#arrival-control-order-approved-20261031 승인. 49#travel-command-acceptance-v1-20261031 공급됨: 기존 DurableRunClient/제품owner에 요청·응답·영속복구 연결. exact 미공급 대기는 공급범위에서 해소하고 실제 소비 구현 진행. 이동 ARRIVE evidenceRef 후 기존 map ARRIVE/nodeStart까지 연결, 미결속도 정리·정지의도 전투개시 보류·역순응답격리. 실제5인 실행·Player 검수 계속, 새 원천구성 보충 미승인.

### NODE-RUNTIME-SOURCE-ASSEMBLY:UNITY:20261031 — 실제 초기상태 실행 소비
44#node-runtime-source-assembly-approved-20261031 승인. 원nodeStart 행동/효과/설정/기하·적행동을 실제factory/공유실행에 소비, 검증후 한 번 개시 및 원본재입장. 기존827 Durable/제품owner를 사용해 목적지초기응답/첫입력·지도ARRIVE/nodeStart 연결 계속. travelInitial projection 결함은 Server수정선행으로만 분리. PAUSE arrival/pauseIntent 기존처리 유지. 실제 Player 교전/종료/API메모리 검수, 합성근거 분리. 새 목적지UI 보충 미승인.

### DESTINATION-FIRST-MOVEMENT-UI:UNITY:20261031 — 제품 첫 입력 연결
44#destination-first-movement-ui-approved-20261031 승인. 49#travel-initial-http-projection-fix-20261031 공급으로 초기projection 누락은 수정계약 소비/실검증 단계. 기존827/828 owner·Durable·지도ARRIVE를 재사용해 목적지선택/확정 버튼→초기상태→첫이동화면/노즐→실수락→지도ARRIVE/nodeStart를 연결. 신규정상 추가GET/자동PAUSE:false0, 원영수증/정지의도 유지. 실제 Player 입력/통신 증거를 남기고 아직없으면 미실행으로 기록. runtime 선행만 분리, 교전/종료/재입장/API메모리 기존큐 계속.

### ORDINARY-ULTIMATE-AUTO:UNITY:20261031 — 실제 AUTO 소비·표시
44#ordinary-ultimate-auto-ownership-approved-20261031 승인. 기존설정/행동owner로 궁극기AUTO단일상태·수동자동소비1·수락후OFF취소0·같은메모리현재값/재입장원값/다음노드최종값 연결. Server실원천과exact공급 후제품소비, UI로false원본발명0. 기존 적원천49#ordinary-node-enemies-v1-20261031 재사용, 실제 이동→5인 교전/Player 검수 계속. 새 이월연결상세는 미승인, 기존최초준비/충전0규칙의구현은유효.

### NODE-ACTION-CARRYOVER:UNITY:20261031 — 실제 행동 상태 소비
44#node-action-carryover-approved-20261031 사용자 승인. 원actions 잔여/충전/AUTO와실행시계/표시·실사용 연결, 정비/로딩무료초기화0·같은node시작상태재입장. 원종료응답/상태소유자 재사용. 실제목적지/이동/도착/nodeStart→5인교전/포격/종료/Player/API메모리 우선 계속. 새효과이월보충은 미승인, 원천선행만기존WAIT.

### NODE-EFFECT-CARRYOVER:UNITY:20261031 — 효과 상태 실행/표현 연결
44#node-effect-carryover-approved-20261031 승인. 원effects 잔량/수명/범위소비·원노드시작재입장·실드흡수/표시일치·옛객체/콜백격리. 실제원천선행만기존WAIT, 임의빈목록0. 실제제품이동/교전/종료/Player API메모리 검수 계속. 새정비출처변경안 미승인.

### MAINTENANCE-EFFECT-SOURCE-CHANGE:UNITY:20261031 — 정비 확정효과 소비
44#maintenance-effect-source-change-approved-20261031 승인. 기존정비응답/Model통지로장비/능력치/효과일관반영·대상ID격리·수명정리·nodeStart불변 유지. UI임의판정/효과재부여/추가GET0. 원계약선행만기존WAIT, 실제이동/5인교전/Player검수 계속. 새효과미리보기표시안 미승인.

### MAINTENANCE-EFFECT-PREVIEW:UNITY:20261031 — 기존 비교창 효과변화 표시
44#maintenance-effect-preview-approved-20261031 승인. 기존비교창에변화효과/대상·유지/해제/조건변경·잔량표시, 확정전미적용/정상재조회0/확정후같은상태통지갱신. 실제이동→5인교전/Player검수최우선. 49#ordinary-run-actions-v1-20261031의ordinaryAutomation/nodeStart.automation 공급됨을확인해실행소비연결; 전체source완료로확대0. 새확정차이보충미승인.

### MAINTENANCE-PREVIEW-CONFIRM-DIFFERENCE:UNITY:20261031 — 결과표시·시간정합
44#maintenance-preview-confirm-difference-approved-20261031 사용자승인/쿨다운단위수정지시. 기존정비미적용충돌/성공/유실복구 UI·상태연결. Server49 시간단위정합과맞춰initialization/nodeStart/COMPLETE→실행소비, 비0잔여/경계/재입장검수; 임의변환/0우회0. 실제이동→5인교전 Player검수계속. 새쿨다운숫자표시안미승인.

### COOLDOWN-READY-DISPLAY:UNITY:20261031 — 실제 잔여/사용가능 표시
44#cooldown-ready-display-approved-20261031 승인. 1초이상올림/미만문구/정확0표시해제·시간조건과최종적격구분·배속중복0·기존정보통지재사용. 수정시간단위소스 새Player빌드로실이동→교전 검수, 구829빌드혼동0. 원천선행만기존WAIT, 실제실행/종료/재입장/API메모리계속. 새총구기하보충미승인.

### COMBAT-ANCHOR-GEOMETRY-BINDING:UNITY:20261031 — 실제 Geometry/factory 실행
44#combat-anchor-geometry-binding-approved-20261031 승인. 원발/발사/피격기준·반전·기하adapter/factory를실행호출부에연결해 null/미연결범위해소. 임의공통박스/값0/미술좌표권위승격0. 원기하값부재만정확의존분리하고실교전검수진행. 최신시간단위새Player로이동/5인발사·명중/차폐/포격/종료·재입장/API메모리까지계속. 새물리탄접촉보충미승인.

### PHYSICAL-PROJECTILE-CONTACT:UNITY:20261031 — 적v2/실충돌연결
44#physical-projectile-contact-resolution-approved-20261031 승인. 49#ordinary-node-enemies-spatial-v2-20261031 소비를v1구저장호환유지로연결. 원hash/instance검증과실기하/탄이동구간·접촉/차폐·원사건1회 연결. v2수신만으로null총구/형상완료처리0. 기존승인기하제작/실행연결은계속하며정확누락만기존WAIT. 실제이동→5인교전/포격/종료/재입장 Player/API메모리계속. 새제작책임보충미승인.

### NATIVE-GEOMETRY-AUTHORING:UNITY:20261031 — 실제 총구/형상 제작 책임
44#native-geometry-authoring-ownership-approved-20261031 승인. 기존승인scene/prefab/authoring에서총구·능력발생점/피격형상직접구성·측정근거기록, 원수치재사용/없는값후보명시. 같은정의를Server등록계약으로전달하고실Geometry/factory/native발사·LOS/피해연결. 상대세션의시각좌표제작을기다리기만하지말고자기제작범위진행. 실제이동부터5인교전/Player검수계속, v2수신검수와형상검수분리. 새기하버전보충미승인.

### GEOMETRY-VERSION-RUN:UNITY:20261031 — 후보보완/실등록 연결
44#geometry-version-run-compatibility-approved-20261031 승인/835직접검토. 총구[966,1408,0]은합성PNG차체기준점을인물신장축척에사용한근거부족, H1800/R350캡슐은실인물겹침미확인으로제품승인보류. 실제renderer/pivot/월드축척/부모frame 및원화오버레이로발/총구/피격영역양방향대조보완. 원차체측정재사용/strict export준비, 후보전체pin자동발행0. 구run기하보존·실adapter/공격연결독립작업계속/Player까지검수. 새사수역할보충미승인.

### GUNNER-VISUAL-HIT-ROLE:UNITY:20261102 — 사수표현/판정 역할분리
44#gunner-visual-hit-role-approved-20261102 승인. 실제ordinary제품경로의사수표시원본·물리frame결속을확인/구현하고사수HP/총구/차폐역할대조.836CE03버기RawImage와835측정PNG불일치/Foot렌더0은확인범위명시,편의후보승격0. 원승인자산·실기하제작/strict export/adapter/factory연결독립작업계속. 실제이동부터5인교전Player검수우선. 새표시원본보충미승인.

### RENDER-SOURCE-BINDING:UNITY:20261102 — 실제 자산 오연결/총구·피격 결속
44#ordinary-render-source-binding-approved-20261102. 실제 ordinary buggy 및 사수 animation 기계 사냥개 오등록 참조 정리, 승인 자산 출처 검증·독립 역할/월드투영 결속. 원본 삭제/후보 미술 자동 승인 금지. Server 신규 차체 정적 ID 계약 소비, 구정의 보존. 실제 nodeStart factory/native 공격·LOS·피해·효과·완료 연결→실서버 이동/5인 교전/노드 시작 재입장 Player/API/메모리 검수 계속. 부족 자산은 정확 의존으로 남기고 독립 실행 연결은 진행. 사수 격파 표현 후속 제안은 미승인.

### GUNNER-DEFEAT-PRESENTATION:UNITY:20261102 — 사수 격파 표시/실교전
44#gunner-defeat-presentation-approved-20261102 적용. 실제 처치 사건으로 새 공격 중지·표현·목표 일치, 기존 잔탄/차체/별도 포격 유지. 49#first-enemy-body-static-id-v1-20261102의 enemy_train_first_battle을 strict export/실행에 소비. 승인 사수 자산 오연결 정리, native 공격·LOS·피해·효과·종료 호출부와 노드 시작 재입장 실Player 검수 계속. 자산 미확정만 WAIT에 유지, 합성 ARRIVE를 실교전으로 승격 금지. 새 동시 발사 경계 보충안 미승인.

### GUNNER-DEATH-FIRE:UNITY:20261102 — HP 단일 소유권/실행 연결
44#gunner-death-fire-boundary-approved-20261102 적용. B loop HP와 Actor.Vitals 이중원장을 실제 호출/수명 조사 후 권위 변경 경로 하나로 연결. 구체 소유 클래스 결정은 담당 구현 책임, 새 기획 승인 대기 아님. 발사전 처치 취소/발사후 잔탄/기존 동순위·종료 유지, 실제 공격·LOS·HP·처치/목표/완료를 연결해 Player 검수까지 계속. 사수 자산/기하 미확정만 별도 의존. 새 피해결과 소비 보충은 미승인.

### DAMAGE-RESULT-CONSUMER:UNITY:20261102 — 단일 피해 결과 소비
44#damage-result-consumer-consistency-approved-20261102 적용. HP 단일 소유권에서 확정된 결과를 표시/흡혈/목표가 재사용하고 재계산/이중차감 금지. 실제 발사·명중·처치/잔탄/완료 연결과 서버49 원사건 계약 결속, 실제5인 Player 검수 계속. 기존 B loop HP 병행 금지. 새 인스턴스 정리 보충은 미승인.

### BATTLE-INSTANCE-DISPOSAL:UNITY:20261102 — 종료정리/실제 교전
44#battle-instance-disposal-approved-20261102 적용. 탄·콜백·구독 소유수명/풀 초기화/이전attempt 격리, 원요청 복구 보존. 839 단일HP kernel을 공개 nodeStart/restart caller와 실제 발사·명중·처치/잔탄/포격에 연결하고 기존 서버 수락 계약 결속. 실5인 Player/API/3회 잔류 검수 계속. 새 준비→교전 보충 미승인.

### PREPARATION-ENGAGEMENT:UNITY:20261102 — 준비→실교전 공개 경로
44#ordinary-preparation-engagement-transition-approved-20261102 적용. 단일HP/현재attempt/필수자산·기하/공격 처리 준비 후 시작1회, 준비중 공격·시간 진행0, 기존 첫공격 유지. 공개caller→실발사/명중/처치와 종료/노드 시작 재입장 Player 검수까지 구현. 새 행동귀속 보충 미승인.

### FIRST-ENGAGEMENT-ATTRIBUTION:UNITY:20261102 — 실제 5인 혼합 행동
44#first-engagement-actor-attribution-approved-20261102 적용. 839단일HP/840공개준비driver 재사용→실발사·명중·처치/지원·잔탄·포격/목표 연결. 원actor 귀속과 기존 혼합판정 유지, 실교전 Player 검수 계속. 합성 준비3회수명과 실교전API/메모리는 구분. 새 프로세스 currentattempt 보충은 미승인.

### FRESH-PROCESS-ATTEMPT:UNITY:20261102 — 새프로세스 재입장
44#fresh-process-current-attempt-approved-20261102 적용. 서버 현재attempt 응답 소비→공개 node시작 재입장, 과거재시작영수증 추정 금지/완료불명확원키복구. 단일HP 실제 발사·명중·처치/지원·종료 Player 검수 계속. 새 준비취소 보충 미승인.

### REENTRY-CANCEL-RETRY:UNITY:20261102 — 단일준비/현재응답 소비
44#reentry-cancel-retry-ownership-approved-20261102 적용. currentAttempt 기존응답 소비, 요청/driver 재사용·취소후늦은게시 차단·교전전 재선택 세대남발 금지. 단일HP 실제발사/명중/처치와5인 Player 검수 우선 계속. 새 선택상태 보충 미승인.


### REINA-BARRAGE-DAMAGE:UNITY:20261102 — 승인4회 포격 논리실행
44#reina-barrage-damage-detail-approved-20261102 및 원상세전체 적용. 준비/commit/충전/4회일정/원frame·대상적격/단일HP·후속효과 연결. 코드독립범위부터진행, 구snapshot불변. U-REINA-SYNC-BIND AND는별개유지/음원배치는후속 REINA-AUDIO-LAYOUT 승인준용. 실제5인발사명중처치Player계속.

### REINA-AUDIO-LAYOUT:UNITY:20261102 — 승인시계·모드 연결
44#reina-cinematic-battle-audio-layout-approved-20261102 적용.8.95전체화면/논리hold/실4회사건음·스킵80ms·기존정지/배속소유계약 독립구현. CS/D/Audio미완성자산을완료로취급0,U-BIND AND유지. 새kitv2/relocationv7 소비·실5인Player계속. 구역표시는후속 REINA-AREA-FEEDBACK 승인준용.

### REINA-AREA-FEEDBACK:UNITY:20261102 — 포격구역/대상이탈표시
44#reina-barrage-area-feedback-approved-20261102 적용. 원frame구역/선정대상/이탈재진입/실4회진행/종료정리,기존표시계약재사용·피해판정분리. CS/Audio후속연결은기존AND유지. 부분결과는후속 REINA-PARTIAL-OUTCOME 승인준용, 실제단일HP5인교전Player계속.

### REINA-PARTIAL-OUTCOME:UNITY:20261102 — 무피해/중단 표시
44#reina-barrage-partial-outcome-approved-20261102 적용. 기존피해/흡수/면역/기여재사용,정상회차와실피해/취소구분,이탈·사망가짜숫자0/추가조회0. 원frame소멸간단안내1회,실5인Player연결계속. 시전자행동은후속 REINA-CASTER-ACTION 승인준용.

### REINA-CASTER-ACTION:UNITY:20261102 — 회수후행동재개
44#reina-barrage-caster-action-approved-20261102 적용. 회수300ms후기존기본/이동/액티브재개,포격독립수명·시전별귀속/다운복귀중복방지. 원논리hold유지. 실제단일HP5인교전Player와음원후속연결계속. 중첩 표현은 후속 REINA-OVERLAP-FEEDBACK 승인 준용.

### REINA-OVERLAP-FEEDBACK:UNITY:20261102 — 복수포격 표시·음향
44#reina-barrage-overlap-feedback-approved-20261102 적용. 원시전별상태/표시수명/기존중첩음향제한 및hold,실제기하·원천adapter연결→5인Player검수우선계속. Audio v15정확파일결속/후보단발자동승인0. 새화면밖보충미승인.

### REINA-OFFSCREEN:UNITY:20261102 — 화면밖포격
44#reina-barrage-offscreen-feedback-approved-20261102 적용. 원frame판정유지/화면끝진행표시/복귀과거연출폭주0/원월드음향·수명. 844측정adapter에실등록기하·pose/tick·효과원천연결→5인Player우선계속. 새가독성보충미승인.

### KYLE-BLACK-UNIFORM-AUDIT:UNITY:20261102 — 실제 소비 참조 조사
사용자 제작수정요청 Cutscene_2 전달.21#kyle-black-uniform-approved-20261102/17 registry갱신확인. 현재카일전투/초상/카드/도감/컷인실asset 참조·캐시/atlas와표현버전영향만조사,승인새자산공급전임의교체0. Approved821/Operators.asset·provenance.json은문자열참조발견출발점이지최종활성증거아님. 원HP/능력/기하변경0. 결과기존인계에기록후 WAIT KYLE-BLACK-UNIFORM-20261102 후속조건확인.

KYLE-BLACK-UNIFORM-AUDIT:UNITY:20261102 정정(2026-11-03): 최신 v2.20261103의 등 글자는 巖(바위 암). 守가 들어간 실제 소비 자산/manifest는 수정 대상으로 기록하고 Direction 승인 공급 후 연결. 문파명 守路門 문자열 일괄 치환 금지.21#kyle-back-character-correction-20261103 참조.
임무 우선순위 2026-11-03 / 기존 NATIVE-GEOMETRY-AUTHORING:UNITY:20261031. 현재 사수의 양방향 총구·피격 범위를 원 장면에서 실측·검증하고, 5인의 공격 위치·자기 피격 범위를 검증된 역할 순서로 내보낸다. [총괄 범위·착수 근거](PLAN-D-MIGRATION.md#five-combat-geometry-first-20261103)

### LOBBY-DEPARTURE-BGM:UNITY:20261103 — 〈차창 너머〉 연결
[채택된 마스터·정책](../superpowers/plans_categories/18_BACKGROUND_CUTSCENE_STYLE.md#lobby-departure-bgm-approved-20261103). 로비와 출발 준비 왕복에서 같은 재생 위치를 이어 가고, 실제 교전 진입 사건에서 전투곡으로 한 번 전환한다. 기존 음악 재생 담당과 BGM 설정을 재사용한다. Player에서 양방향 왕복·반복 경계·진입 대기·실교전 전환을 구분해 재생 위치와 중복 재생 여부를 확인한다. 실교전 선행이 미완료이면 확인한 범위만 기존 인계에 기록한다.
LOBBY-DEPARTURE-BGM:UNITY:20261103 최종본 갱신. 18의 최신 Runway 2분 마스터(runway-loop.wav, 44.1kHz, 5292001프레임)로 교체하고 기존 846의 48초판 결과와 별도로 새 원본 해시·Player 루프 경계·로비 왕복·전투곡 전환 결과를 확인한다.

### BASIC5-CONTACT-C-MIX:UNITY:20261103 — 기본 5인 평타 명중음 C 믹스 B
[사용자 청취 승인 및 원음 해시](../../QA/AudioProduction-20261020/QA.md#five-agent-contact-c-level-approved-20261103). 레이나·카일·이브·세라·닉스 평타 믹스의 전투 BGM은 50%, 발사·스윙은 기존 레벨을 사용한다. 기존 실제 명중 사건이 C 원음 hit-local-v03/C.wav를 소비하는 경로에서 최종 게인 0.460165를 한 번 적용한다. B 비교본은 보정된 청취 기준이다. 사용자 BGM/SFX 설정을 존중하고 Player에서 실제 재생 원본·게인·명중 1회당 재생 횟수를 확인한다. 실제 교전 원천이 미공급이면 검증한 재생 범위를 구분한다.

### BATTLE-D-PLAYER:UNITY:20261104 — 전투 전체 화면 승인 D 대조
Unity_17 담당. 최신 unity-project Windows Player 기준 화면부터 확보한다. QA/SideViewReuse-20261023/reina-battle-hud-v01/d-console 및 DIRECTION_2-D-20260913.md의 후속 승인 기준으로 상단 작전명·배속·일시정지, 좌측 철로·열차 상태·스로틀, 중앙 5인 초상·금속 프레임·충전 발광, 우측 지원 3종, 전장과 차체 배치를 대조해 확인된 차이를 수정한다. 삭제 승인된 상단 중앙 단계 배너는 제외한다. 실제 편성과 상태를 보존하고 독립 표시와 기존 입력 콜백을 사용한다. 같은 편성·상태의 수정 전후 16:9 Player 화면 및 4:3·21:9에서 잘림·겹침·버튼 입력 결과를 기존 Unity QA에 기록한다. 재현 불가 상태와 미공급 원화는 분리 기록. 최종 비교 검수는 Plan 담당.

### OBSTACLE-NUMERIC-PLAYER:UNITY:20261106 — 승인 장애물 수치 실제 Player
2026-11-06 확정49#ordinary-obstacle-sequence-20261106 소비 연결: marker/원enemiesHash·현재attempt·단계/중복/사망·원HP/시계·HUD 반영, 최종112/112. [새소스 검증/미공급](../../QA/ObstacleTrial-20261106/product-consumer-verification.json), 인계#obstacle-product-consumer-20261106. 실제 장애물producer/Basic·Gunner·SceneMotion 및 운영evidence/로컬세션 인수·후속절대tick 결속·제품Player는 남음. wire미공급은 해소, readiness우회/과거15건대체 없음.
2026-11-06 로컬 Player15사례 검수 완료. [화면비별 실제명중/충돌/입력·최종실행본·소스해시](../../QA/ObstacleTrial-20261106/player-review.html). 모델7/7·일반회귀38/38. 제품producer/보호막적격/노드·후속웨이브·미술/음향 인수 및 정확히동시각 Player경계사례·확대편성/복합경계 검수는 유지. 자세한 완료근거는 기존 Unity 인계의 같은 큐ID 참조.
사용자 직접 Unity 적용/검수 지시. 담당 Unity_17 단독 unity-project 편집, Plan은 문서/결과검수. 원본 45#obstacle-numeric-trial-approved-20261106 및 직전 obstacle-numeric-trial/obstacle-external-detail 절, 44#external-detail-approved-20261106. QA/ObstacleTrial-20261106/results.json은 조건부 해석검산이며 성공명중/Player 근거 아님.
착수:기존모델/피해/운행/입력 경로 재사용해 격리된 승인수치 QA Player 시험 구현·새빌드.38m 원결속편성/현재원배경 유지,600m시험선로·포탑선두뒤6m기하 authoring 측정. 필수사례:낙하중/착지후실탄명중·부분파괴·파괴뒤충돌0·수평/낙하피해/보호막·1회접촉·저속파괴·고속후미통과·후미피격·정지/배속/재입장. 실제런타임0.4초명중상한/원HP·실속도 증거와 Player영상/프레임·입력·build/source hash를 기존UnityQA에 기록. 세화면비/1~2배속에서판독/입력확인. 실제producer미공급이면격리fixture임을표시하고제품서버등록/실START를가짜로연결하지않는다. 웹렌더/정지준비화면을실교전검수로대체금지. 완료후기존인계/QA 링크와잔여의존성을현재Plan채팅에결과답장.

2026-11-07 Unity_17 / OBSTACLE-NUMERIC-PLAYER 최신 우선범위 완료: [최종 실제서버·제품Player·연속프레임](../../QA/ObstacleTrial-20261106/player-live-final-verification.html), [근거/한계](LAST_RAIL_Unity_2-D-20260913.md#obstacle-live-player-unity-20261107). 실제 로컬 장애물producer→원Basic/Gunner/SceneMotion/HP·절대tick→다음웨이브 및 세비율 표시 결속 확인. 기존6사례기능+최종4사례변경검수 재사용, 정상HTTP전송중 진행·고정pending비교·128후속입력상한·실제동일키200/이전attempt409 확인. 원화/음향·전체node완료/evidence·충돌실보호막적격·확대편성/정확히동시각복합경계는 부모잔여로유지. v08 종료오류1회원인미확인, v09네실행정상종료. 부모전체완료삭제/새후속확대등록 없음.
### COMBAT-GAP-01-CAMERA:UNITY:20261107 — 승인 구도 카메라
사용자 순차실행 승인, Plan #combat-gap-sequential-approved-20261107. Unity_17 단독 unity-project/기존QA 소유. v07~v10 승인 경로/구도→현재 실제제품 Player 카메라 연결, 세부 수용조건은 Plan 실행지시 및 기존45. 원배경/객차실측/판정 보존, 좌직진우 카메라 중심·방향 연속/미니어처 회전·계단차 금지. 최소 경로/열차 투영 의존성 포함하되02/03 전체완료와구분. 실제 Player 영상·세비율·정상종료/입력·명중좌표 대조를 기존QA/인계에 보존 후 Plan_8 결과회신. 후속02는01검증 이후 등록. 운영배포/실계정변경없음.
COMBAT-GAP-01-CAMERA:UNITY:20261107 사용자 정정: 제목/목표는 승인시점 2D리소스 배치. 직전 camera yaw 구현수단 지정 철회. 시점별 승인원화를 세팅하여 지붕/측면/원근을 맞추며 화면추종과 구분한다. 3D카메라회전/이미지왜곡으로 없는면을 대체하지 않는다. Plan인계 01 제작 시점 정정 절 우선.

2026-11-07 Unity_17 공급분 부분완료: 고정 승인2D원화 이중압축 제거, approved-view-01/verified.xml6/6·player-approved-view-v01·approved-view-verification.json 네실행exit0/MP4. 최초변동입력실패 제외·새session ID/seed 차이 명시. 상세 LAST_RAIL_Unity_2-D-20260913.md#combat-gap-01-2d-source-audit-20261107. 01전체는 방향별원화/등록/매핑 WAIT 유지, 큐삭제/02착수 없음.

2026-11-07 요청5인 표시선행 부분완료: op_reina/op_kyle/op_eve/op_sera/op_nyx 원화·카드순서/부츠등록·표시전용Player3비율exit0,19/19검사,영상확보. Unity인계#requested-roster-unity-20261107, QA/ObstacleTrial-20261106/requested-roster-01/verification.json. 실제전투5인·제품HUD는Nyx 실행원천미공급으로WAIT NYX-ACTUAL-FORMATION:20261107; 기존Mira위장0. 01전체/02미완료·부모큐유지.2026-11-07 우향 후속 부분완료: Reina 승인우향4장 Studio해시일치로표시연결, 최종 player-requested-roster-right-v03 세비율exit0/참고Reina-Kyle간격과지붕투명구멍회피. Unity인계#requested-roster-right-unity-20261107, requested-roster-01/spacing-final-verification.json. 나머지4인우향·제품HUD/버튼/실Nyx전투미완료, 부모큐유지. 녹화전용약10초Player이며자동종료해제옵션없음.
2026-11-07 Nyx 독립 실행핵심 부분완료: 영역수명/예산/연속HP패시브·공유피해감쇄율·새예약지연 구현,nyx-final.xml96/96. Unity인계#nyx-zone-core-unity-20261107. 새정의/실기하/현재시도내부상태복사·현재노드시작재입장/제품HUD/실Player 입력은미완료; 기존WAIT NYX-ACTUAL-FORMATION 유지,부모큐삭제없음.
2026-11-07 Nyx 현재시도 후속 부분완료: 사용자정정에 따라 공개RestoreOwned/host Nyx 저장DTO 철회, 노드 restart-only 유지; 내부 clone/retire·명시 current-effects v2/감쇄율 arrival·아군신규예약 지연 및 Nyx표적고정 연결. nyx-host-green146/146, nyx-restart-final44/44 별도 PASS. 최신 소비 v5/v9 해시검증(아직 START reader 수용 아님). Unity인계#nyx-checkpoint-host-unity-20261107 및 requested-roster-01/nyx-host-verification.json. 새reader/적예약/실영역dispatch·실START/기하/HUD/Player 잔여; 중간복원은 잔여에서 제외. 부모유지.

2026-11-07 Unity17 새reader/적Gunner 신규예약/영역dispatch·host/HUD 코드 부분완료: 인계#nyx-readers-dispatch-unity-20261107 및 requested-roster-01/nyx-readers-dispatch-verification.json. 최종247/247 + 후속HUD 별도 PASS. FOOT만 등록, 실기하/거리·LOS 정책/새START/실Player 검수 대기. 48m는 미승인 시험제안, 기본24m 유지. 전투중간 영속복원은 폐기(44 두 정정 anchor); 현재시도clone/현재노드시작 재입장 유지. 부모미완료.


2026-11-07 Unity17 48m 승인 후속: 44#nyx-zone-selection-distance-approved-20261107 확인. 위 미승인/거리정책대기는 이 기록으로 해소. NyxZoneDispatch 원관측 포트는 inRange bool 대신 명시 footWorldMm/centerWorldMm을 받아 실제3D 제곱거리≤48000²와권위LOS를수락시검사. 생성시는고정구역/소유세대·present만재확인,거리/LOS재선택없음. 새서버wire를발명하지않은in-process포트이며실권위원천/새START연결은잔여. nyx-48m-red.xml9/19→green46/46·exit0: 48000/48001·높이/횡·3D조합·차폐·수락후거리변화/구역고정·세대변경생성실패시구영역보존. 기본24m불변. 실제기하·정식발행은별도미완료.

2026-11-07 Unity17 relocation v10 실소비 코드 후속: 49#nyx-zone-selection-48m-server-20261107 및 nyx-48m-owned-source-contract-20261107.json 확인. 새 정의hash a0aab36339ec85fae31c96a01ff8845a63e9c6318854a6a1369926c9f5f750eb, 중첩 zoneSelectionSource 원packet/hash126e5387830a0e71c9dcc55dccf1f2fca9c5f33b02355e284204dad687b5999b 검증. WithNyxSources는v10+동일selectionHash+원attempt 구성binding을요구, 구v9 reader와기존run pin보존. 기초수치/시간 source v2는기존수치원본으로유지, 새선택은approvedv3 companion이며혼동하지않음. typed in-process 관측에는명시eligible/FOOT·centerWorldMm/고정local representativeCenterMm·경계/소유frame세대를검증. 원world 변환/LOS 사실확인 및 실제 공급자는아직미연결.
검사 nyx-v10-red 새1실패→nyx-v10-green65/65; 이후 적격/대표점 및 Kyle 표시 합동 관련검사 kyle-right-01/final.xml101/101·exit0. 실제 Nyx 새START/Player는여전히미실행. 선택거리/새reader를대기로재등록하지말고원world기하·실zone owner/발행·통합입력검수만남긴다.

2026-11-07 Kyle 우향 기존5인 제품Player 부분연결: 인계#kyle-right-live-unity-20261107, kyle-right-01/verification.json. 101검사PASS/실HTTP장애물→전투,직접pointer용PID44500 pause 유지. 첫적22m/원사거리18m라실기공탄발사미검수; Nyx새5인아님. 종료잔류본인PID47512만명시종료,정상exit미확인. 부모유지.


Unity17 2026-11-07 Kyle 적격 QA 정정: 18m만이아니라 physicalAdjacentCars=0 및현재카일slot2/적사수slot0로거절. 실제13.416m 병렬기하후보도발사불가(첫4/5실패),원적격보존5/5PASS는공격성공아님. 인계#kyle-right-live-unity-20261107, kyle-right-01/close-eligibility-verification.json. 원자기객차적/원승인지원영역의실host/frame/generation·적격계약이선행,slot위장/사거리변경0. 정상닫기후14588잔류명시종료,정상exit미확인. 부모유지.

