# LASTRAIL PLAN 작업 큐

2026-09-22 사용자 요청으로 만든 담당별 실행 큐. 신규 설계 원본이나 완료 이력 보관소가 아니다. [공통 운영 규칙](INTEGRATION_WAITLIST.md#role-work-queues-20260922), 프로젝트 AGENTS.md를 먼저 따른다. 최신 승인 기획만 신규 작업에 적용하며 구 정책은 신규 적용 대상에서 폐기한다. 진행 중 run의 START 스냅샷과 역사/검증 자료는 보존한다.

착수 가능한 항목만 이 큐에 둔다. 선행 미완료 후속은 기존 WAIT의 후속 등록 정의에 보관하며 실행 큐에 미리 추가하지 않는다. 착수 시 실제 파일·버전과 진행 상태를 기록한다. 완료 담당은 기존 인계/QA에 큐 ID·계약 버전·검증 근거를 보존하고, WAIT의 자기 ID 관련 후속 조건을 확인한다. 모든 선행이 완료된 후속을 대상 큐에 중복 없이 추가·확인한 뒤 자기 완료 블록을 삭제한다. 후속 등록 실패 시 `구현 완료·후속 등록 필요`로 남겨 재시도한다. 상세 절차는 [완료 시 후속 큐 등록](INTEGRATION_WAITLIST.md#completion-driven-queues-20260922)을 따른다. 타 큐에는 승인된 후속 항목 추가만 허용하며 기존 항목 수정/삭제는 담당 소유다. 빈 큐는 종료하며 자동 메시지·폴링하지 않는다.

정책 원본: [44 전투](../superpowers/plans_categories/44_COMBAT_DETAIL_CONTRACTS.md), [09 지휘실](../superpowers/plans_categories/09_COMMANDER_PROGRESSION.md), [13 장비](../superpowers/plans_categories/13_EQUIPMENT_ENCHANTMENTS.md), [14 경로](../superpowers/plans_categories/14_MAP_EVENTS_DROPS.md), [31 증강](../superpowers/plans_categories/31_AUGMENT_EFFECT_REFERENCE.md), [32 서사](../superpowers/plans_categories/32_NARRATIVE_REFERENCE.md), [45 정비](../superpowers/plans_categories/45_MAP_STATION_DETAIL_CONTRACTS.md). 각 문서의 최신 승인 절을 사용한다. [통합 대조 I01~09](../superpowers/plans_categories/34_COMBAT_POLICY_COMPANION.md#integration-conflict-audit-20260922-v01)는 발견 근거이며 런타임 전수검증 결과가 아니다.

2026-09-29 사용자 지시에 따라 아래 홈페이지 연동 계약 항목을 추가했다. P-01 완료 근거는 기존34/33 및 WAIT 완료 기록에 보존한다.

<a id="homepage-queue-20260929"></a>
## 홈페이지 연결 Plan 큐 — 2026-09-29 사용자 일괄 등록
[WAIT 정의](INTEGRATION_WAITLIST.md#homepage-queue-registration-20260929). 기존 P-02/03 선행과 H-01 정의 유지.

### P-13@community-public-contract-v1 / 상세 승인 반영 · 보존설정 경계 확인 잔여
11 §5의 승인된 4게시판/글·댓글·신고·차단/운영검수·남용/비공개 문의 분리를 실제 공개범위·권한/DTO/오류·감사/보존·버전으로 확정한다. 미승인 업로드/DM/게임재화 지급 추가 금지. Server S-16 및 홈페이지/Admin 소비자가 구현 가능한 계약과 완료 사례를 기존 기획/인계에 기록.

### P-14@homepage-integration-v1 / 선행 대기: H-02,H-03,H-04,A-02,A-03
WAIT-006·007·012의 실제 같은 DEV 버전/웹세션·공개/비공개·문의·커뮤니티·오류/복구를 통합 검증. QA와 각 담당 완료 근거를 연결하며 운영배포·실게시·실보상은 포함하지 않음.

## 2026-09-29 이번 작업 결과·재개 지점
- P-13: 11 community-public-contract-v1 신규 상세 사용자 승인 반영. 실 보존설정·schema·웹세션은 원담당 선행, 임의 기간 확정 없음.
- P-15 완료: 44 world-boss-binding-v2에 승인된 약탈왕/베일/오르트와 신규 월드 계약ID·18회차 매핑. S-WB-01/D-WB-01 등록·저장 확인 후 실행 블록 삭제. 실제 런타임 연결은 후속 담당, 완료 근거는34/33/WAIT.
- P-14: [34 통합검증표](../superpowers/plans_categories/34_COMBAT_POLICY_COMPANION.md#plan-queue-review-20260929). H-02/03/04·A-02/03 현재선행대기,7개검증사례/증거조건 준비. 실제통합은BLOCKED이며 미실행PASS0.
- 승인 보관함은 [13 v18](../superpowers/plans_categories/13_EQUIPMENT_ENCHANTMENTS.md#inventory-capacity-approved-v18)·09 현행표에 반영.30/36/42/48/54,장착제외·공간부족시정리후수령. 기존 S-02 관련인수는 WAIT 최신절 참조.
## 최종 승인 기획 큐 대조 — 2026-09-29
[WAIT 등록표](INTEGRATION_WAITLIST.md#approved-plan-queue-sync-20260929)에실행/후속정의와완료근거구분. 서버기존S-01/02인수보완,Direction D-EQ-01 신규,기존S-WB-01/D-WB-01 유지. P-13은사용자상세승인과실보존설정경계확인을구별해잔여유지;P-14는실웹소비선행유지. 추후P-EQ-01은U-EQ-01완료후, P-WB-01은기존월드후속AND충족후만등록. 이번등록작업을실구현완료로계산하지않는다.

2026-09-30 Plan 결과: P-EQ-DISCARD-01 원본화 완료 근거는 13#owned-equipment-discard-v19-20260930 및 WAIT-004 보충에 보존하고 자기 실행 블록을 삭제했다. Server/Unity 구현 완료는 아니다. P-EQ-MERCHANT-02 후보는 45#merchant-recovery-proposal-v17-20260930이며 승인 전 발행하지 않는다.

2026-09-30 승인 후: P-EQ-MERCHANT-02는 45#merchant-recovery-approved-v17-20260930 및 WAIT-004에 완료 근거를 남기고 실행 블록 삭제. 앞의 승인대기 기록은 당시 이력이다. P-13은 11#community-retention-intake-v2-20260930 설정 인수 상세 보완 완료/실설정 대기, P-14는 기존 소비자 선행 대기다.


2026-09-30 P-13 진행: 최초 대상은 한국만으로 확인. 11#community-retention-korea-proposal-v3-20260930에 신규 보존 운영값 제안 완료·사용자 승인 대기. 기존 실설정/처리 근거/저장소 파기 인수는 유지하며 P-13 완료 처리하지 않는다.


2026-09-30 P-13 승인/작성 설정 결과: 15#security-retention-approved-sync-20260930 및 log_retention_proposal.json v2 동기화 완료. 앞선 신규값/분류 승인대기는 해소했다. 실제 환경·저장소·권한·파기 검증 근거 인수가 잔여이며 문서 완료만으로 전체 P-13을 삭제하지 않는다.

<a id="sns-deferred-until-internal-test-20261001"></a>
## SNS 후속 보류 — 2026-10-01 사용자 결정

SNS 기획은 게임 내부 테스트 이후 재검토한다. 기존 운영안·공개 범위 승인 이력 및 미승인 첫 주 원고를 보존하되, 추가 원고·제작·운영 작업을 Plan 또는 다른 담당 실행 큐에 발행하지 않는다. 테스트 이후 실제 결과와 공개 자료를 확인하여 재검토하며 자동 재개하지 않는다. P-13/P-14 등 기존 선행 대기 상태는 이 결정으로 완료 처리하지 않는다. 원본: 50#sns-deferred-until-internal-test-20261001.

### P-MAINT-WEB-FULL-01 — 홈페이지 점검 전체 기능 차단 정책 정합화 / 사용자 2026-10-01 변경

- 착수 근거: 사용자가 홈페이지 점검 중 다른 기능을 못 쓰도록 메뉴/다른 기능을 제거하고 배경 원화만 남기도록 명시. 신규 확정 범위는 웹 전체 기능 게이트이며 기존 로그인 전용/공개 조회 허용과 충돌. 추가 수치나 강제 로그아웃/계정 데이터 삭제 지시 아님.
- 원본: 12_SERVICE_MAINTENANCE.md #game-web-login-maintenance-20261001, maintenance-login-v1, WAIT #maintenance-login-followups-20261001. Direction 후보 QA/DirectionImpact-20260921/web-maintenance-full-v08-illustration/manifest.json 및 REVIEW.md. 미술은 후보, 정책 변경 범위는 사용자 확정.
- 완료 조건: 기존 WEB 로그인 차단과 사이트 전체 UI 기능 차단의 감지 범위/버전·직접 URL/뒤로가기/이미 로그인 사용자·예정/실점검/unknown/해제·새 조회 실패 처리 정합화. 사이트 내 공지/계정/커뮤니티 등 다른 화면 우회 없이 점검 게이트만 표시. 상태 조회와 로컬 안내/기존 승인 배경은 복구를 위해 남김. 상태 확인 버튼 하나; 외부 페이지로 가는 점검 안내 버튼 없음. 예정 종료만으로 해제/credential 자동재전송 금지, 기존 세션/진행 작전 snapshot 보존.
- API 전체 접근 거절과 UI 경로 차단을 혼동하지 말고 필요한 Server/홈페이지 경계 및 보안 권한 규칙을 문서에서 확정. 게임 점검 범위까지 임의 확대하지 않음. 기획 계약 변경 후 승인된 실행 후속 조건을 WAIT에 정의하고 필요한 선행 충족 때 담당 큐에 중복 없이 등록. 기존 타 담당 항목 수정/삭제0. Direction의 신규 미술 승인/독립 조합 및 모든 최종시안 후 연결 조건은 유지.



## P-B-MIG-01 — B안 전환 / 착수 가능 / 2026-10-02
- 원본: [B 현행 결정](../superpowers/plans_categories/44_COMBAT_DETAIL_CONTRACTS.md#combat-b-mainline-approved-20261002), [통합/후속 조건](INTEGRATION_WAITLIST.md#combat-b-migration-20261002).
- 포탑 관련 지휘 노드/증강/장비/성장/경제의 실ID 영향과 대체·보존·환불 미결을 대조하고 승인안을 작성. 지원범위·요격·내부전·협동/월드 순서로 보완. 각 담당 근거를 합쳐 선행을 범위별 해제. 기획 MD 원본은 Plan 소유.
- 완료: 실제 파일/참조·유지/변경/폐기/미결 표와 담당 구현·검증 근거를 기존 인계/QA에 기록. 감사완료와 B 전체완료 구분. 자기 A 전용 큐는 새 결정에 맞게 정리하되 원자료 보존. 완료/후속등록 후 자기 항목 삭제; 미결 의존은 WAIT와 연결. 타담당 원본 수정0.



<a id="b-original-accepted-policy-input-20261006"></a>
## 기존 총괄 B accepted broker 후속의 필수 결정 입력 — 2026-10-06
기존 Server #b-accepted-native-broker-next-20261006에 대한필수결정 입력, 새프로젝트기능/중복작업ID생성아님. 선행원Unity2wave/복원QA c96c52...완료와 Server exactbroker/원source gap78PASS검증완료 확인후추가. 원근거 Server/WAIT#b-original-accepted-broker-result-20261006, API combat-b-original-accepted-broker-v1.md/QA d306b25de1121511db9ae88970e5c7c9b456cfd2cc8d513d47ea98e82e9fcdab.
결정필수: SERVER resultverification 실제소유자·원Unity runtimeassignment/completecheckpoint/journal 검증·불변witnessref발급 방식; acceptedARRIVE 실제원producer/의미(현LOCAL TERMINALstop/VICTORY는도착영수증아님); reportoriginmaprev와원거래revision 매핑·nativecommandRevision/sequence구분; checkpoint전송/bodybudget/보관/복구·거절/replay 계약. 원SAFE_EVENT/hold는각소유자의미지원별도. 현재combat-authority.ts의policy만으로승인된clientreport proof없고host/finalreportstore도NOT_PERFORMED이므로아무하나를원검증owner로조용히승격금지.
추천(미승인): 서버통제검증기가기존승인nativeowner/strictrestore/replay를재사용해원checkpoint+commandjournal을검증하고 scope/revision/event/seed/source에결속된불변witnessref를발급; 새시뮬레이터·서명/checksum만의전투증명·클라이언트flags권위0. 실제검증기원source없으면기본409유지. 기존source/코드독립단위재구현대기이유로삼지않음.
완료조건: 결정근거/원계약/소유자·소비입력·issuedref회복/거절·원checkpoint/source/event진위 및같은batchfenceproducer를구체화한후Server/Unity기존동일큐에착수범위인계. 운영STARΤ/실경제/지급/배포승인으로확장0. 타담당기존항목보존, 미충족합동후속선등록0.


<a id="b-result-consistency-policy-correction-20261006"></a>
### 기존 B accepted 결정 입력 정정·실제 잔여 — 2026-10-06
Server/WAIT#b-result-consistency-owner-result-20261006의 concrete owner·GAME report/ref API·원receipt chain 구현으로 앞 #b-original-accepted-policy-input-20261006 중 ‘검증 owner·opaque witness/전송 없음’ 대기는 v1지원범위에서해소. fullnative restore/replay는총괄44 ordinarysolo 승인과맞지않는추가선행이므로필수대기에서제외. 구추천/내부v1 QA는 역사보존; 최신 internalv2 RESULT_CONSISTENCY_VERIFIED/combatTruth NOT_PROVEN으로검증수준구분.
확정 combat.b-result-report.v1.20261006 POST/GET /v1/runs/:runId/b-result-reports[/opaqueRef], GAME 본인권한/no-store/재인증·1.8MB기술budget/동일key원결과복구/원mapchain·원편성/source검증. 서버 관련96PASS/TCP10PASS/type/lint0; 원QA변경없이 실제수신되나 원출발편성누락으로progressfalse. LOCAL compatible admission+조정report+별도LOCALarrival만2wave/node→SafeStore거래PASS, actualUnity수용으로승격0. 전체 suite2679PASS/75FAIL·새18전부PASS, 실패세부QA에기록.
Unity 최신v2 원producer41PASS f68c247d96db39224f10f4eed37aba4d26932fada9de069d6e00dde908d89089 수신. Server v2 intake/원eventRef·command/point sequence대조·budget/broker연결은 서버독립착수가능이며 새Plan결정대기로돌리지않는다. 현재endpoint는v1전용/v2schema400. 구sample을새v2증거로 relabel하지않음.
Plan 실제필요결정은 Unity인계#unity-b-original-event-producer-result-20261006/큐#unity-b-original-arrival-contract-pending-20261006의 선택 pendingEdge/node 도착기준(선두·꼬리·구간경계) 및 원구간거리 또는 원전이조건을 어느 승인source/owner가공급할지. 추천: 기존14_MAP_EVENTS_DROPS404–442의 실제구간이동→도착검증→사건진입 흐름에 원source/owner조건을명시해양측동일입력 사용, 새속도·거리·임의시간추가0. 추천미승인. 실제원departure selection/runtimeinput 일치·ARRIVE수락·hold/SAFE_EVENT권한source는각기남음. body기술한도와새보관기간정책구분; fullreplay/새보존정책을전체진행조건으로묶지않음. 운영 START/실경제·보상·배포승인 아님. 기존 P-B/통합부모부분완료·미충족AND선등록0/타담당원블록삭제0.

QA 원본 D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-result-report-validation-20261006.json SHA256 7205a49c1d7af0c97b1e2cbab3a827d66b8b9163a78fbe15eb0ed800f98e796d. final3/TCP10/safeLOCAL/full-final/related-recheck/departure-single 재검사 및source hash 포함.


<a id="b-result-v2-original-source-remaining-20261006"></a>
### 기존 총괄 B v2 후속 완료·실제 원source 잔여
기존 #b-result-consistency-server-next-20261006 v2intake/저장budget·원command/prefix/eventRef/point-vs-raw/hash/receipt/broker actualSQL연결 및GAMEHTTP 구현·검증완료. 관련107PASS+최종delta11/type/lint0, v2TCP12+v1TCP10. QA 02cf629ff908a557fb37bd5831dcc48a79d943661a7a6652b841947794816556/Server·WAIT#b-result-v2-journal-server-result-20261006. 전체75FAIL baseline미증명은그대로보존/무관전체수정0.
Server actualapproveddeparture-v4 producer→읽기input 연결에서 원5편성·별도2~4차량·owned성장원문보존공급을완료. 기존B시험native는차량별operator/cargo occupant layout, 원5formation.slot과carriage.slot을자동동일화할근거없음. reader는 effectiveCombatStatsIncluded/runtimeOccupantBindingIncluded/selectionSlotIsCarriageSlot/canStart=false. 기존v1position검사를실5요원 runtime매핑 완료로주장하지않는다. 기존승인 B runtime effectiveprofile/occupant binding의소유원계약·필드매핑을원source에서추가대조해야하며새배치정책/효과수치발명0. freshwholeUnityv2 export와원입력일치가actual통합검증잔여; 새fullreplay정책선행아님.
Unity 독립원v2전송/receipt복구는 원producer AND concreteServer공급완료로 기존Unity큐 등록키 U-B-RESULT-TRANSPORT@combat.b-result-report.v2.20261006 저장. 소비자정상통신은전체Server완료조건으로순환대기시키지않음. ARRIVE는사용자선두/꼬리질문답전새조건/구간거리/속도확정0, VICTORY변환0. hold/safeevent/operating motion/timing독립잔여. 전체부모PARTIAL/미충족AND후속선등록0/운영배포·실START·경제·보상0.


<a id="p-b-chassis-profile-decision-input-20261006"></a>
### 기존 B 부모의 실제 named profile: 차체 source 최소결정 입력
등록키 P-B-CHASSIS-PROFILE-DECISION@combat-b-chassis-binding-proposal-20261002. Server의원assembler조사 완료근거 Server/WAIT#b-named-profile-assembler-audit-20261006, 기획총괄직접지시 ‘새기획수치면최소필드·승인/미승인절·막히는호출먼저보고’에따른결정범위만등록, 구현미완료source를소비실행큐에선등록0. 해당Plan큐 exact동일등록키/차체proposal명미등록확인, 신규port/START작업아님.
결정대상 원44#combat-b-chassis-binding-proposal-20261002: B차체baseHP/baseDEF/성장식·actualinstancecarriage-level owner/version/revision/hash. 기존추천HP1100/DEF111/HP성장8%사용여부를검토·확정, 요원level/refitrank로대체0. 새initialunlock/소유발행/배포승인과분리. 직접막히는consumer BDirectAttackInput.cs112–115 TRAIN/CARRIAGE 필수 및BNamedOperatorNativeAdapter.CreateRuntime56 geometry실source필수. 이미있는assignedKyle/Sable영구stats/basic/interception원source는재사용·재구현0.
완료조건: 최신승인/미승인 상태를44원문에정확기록하고source최소필드/actualgrowthowner/지원단계를확정하거나정확보류. 미확정값0/옛A자동채택금지. geometryauthoring/요격 v08·geometry v09는이미승인된구현source의존성으로분리, 새재승인요청0. sourceproducer실구현/geometry·runtime지원미공급이있으면완료정책만Server/WAIT기록하고전체profile완료로승격0; 필요한다음서버정책소비단위만완료기반등록. 실제Unityconsumed/운영START/경제/배포0. 기존부모·다른Plan항목삭제수정0.


<a id="p-b-carriage-growth-owner-input-20261006"></a>
기존 P-B-CHASSIS-PROFILE-DECISION@combat-b-chassis-binding-proposal-20261002 입력addendum/중복새ID0. Server/WAIT#b-carriage-growth-owner-audit-20261006: 원level source는account_carriages(account_id,carriage_id).level/revision(종류별), Adeparture ownedState.carriages 원문+samebatch entitiesSql CAS. B물리instance table는FK만있고instance-level없음; 현재Bassignment성장캡처에carriagelevel/revision없음. 기존TRAIN upgrade는account_train.level이므로차체로혼용금지. 새결정필드: Bcommondefinition의동일account_carriages 종류별레벨공유를승계할지/instance별성장이필요한지, 실제carriage-level 변경owner+출발CAS매핑. 추천은기존종류owner 재사용이면공유semantic을명시하고definition↔instance↔level/revision 원source 결속; 승인전구A값/기본level1을Bpolicy로채택0. 현재HP/DEF/8%·초기5slot 사용자질문미응답은승인아님. 이원천조사만완료/정책없는adapter구현안함/후속원profile전체선등록0.


### P-MULTISHOT-BIND-01@multishot-enchant-v1.20261015 — 부분 완료 / 양측 인수·미공급 경계 기록, 실원천·평가시점 잔여
- 선행 완료: S-MULTISHOT-SOURCE-01 AND U-MULTISHOT-SOURCE-01 조사. 계약 multishot-enchant-v1.20261015, 13#multishot-enchant-detail-v1-20261015 §1–9, WAIT#multishot-followups-20261015. 마지막 조사 완료 Unity가 등록.
- 근거: LAST_RAIL_SERVER_2-D-20260913.md#multishot-server-source-result-20261015 / QA/CommanderS1-20260922/multishot-source-audit-20261015.json(기존70PASS/0FAIL, 신규효과 실행 아님); LAST_RAIL_Unity_2-D-20260913.md#unity-multishot-source-result-20261015 / QA/MultishotSource-20261015/verification.json(12파일핀·읽기 조사, 런타임 변경/시험 실행0).
- 범위: 실제 정식 ID/능력별 적격과 원기회·전체D·원치명·확정 본체 전체집합·L/geometry/LoS/총구·아군 투사체 수명/복원 실필드·대상 DEF/감소/조건 평가 시점을 소유 원천에 결속. 현재 actor:BASIC 진단 label은 formal abilityId가 아니고 적포수 및 미라 ACTIVE는 아군 BASIC 원천 대체 불가. EN17은 미예약/미발행 후보, 신규 아군 투사체 BASIC 완전지원 미확인.
- 완료 조건: 결속 가능한 실제 범위/미지원/미확인과 미공급 원천별 담당·필드/버전·구현/검증 수용 기준을 원문에 기록. 원천 부재를 공급완료로 승격하거나 임의 ID/API/피해를 발명하지 않는다. 정확 공급 완료 조건에 따라 Server 정의·저장 후 Unity 소비 후속을 등록하고 미충족 구현큐는 선등록하지 않는다. 기존 정상 BASIC·역사 자료·진행 START snapshot 보존, 배포/실입장/보상 제외.
2026-10-15 P-MULTISHOT-BIND-01 진행:13#multishot-source-binding-v2-20261015에 양측 실제 조사 인수·재사용/미공급/담당/수용조건 기록. 새 아군 투사체 대상 DEF 도달시 평가안은 미승인. 정식 능력/공간·피해예산/수명 미공급 유지, 구현큐 선등록0. 자기 항목 유지.

2026-10-15 판정시점 승인 및 독립기반 발행: 발사공격고정/도달방어확정으로 앞의 평가시점미승인기록 해소.13#projectile-foundation-contract-v1-20261015/WAIT#projectile-foundation-registration-20261015에 Server 내부정의/Unity 순수계산 착수2건 등록·확인. P-MULTISHOT-BIND-01은 실능력·공간·수명공급 결속 잔여로 유지한다.





### NONVICTORY legacy finish 복구 한정 통합 검토
등록키 SCENE-INFO-INTEGRATION:PLAN:20261019:nonvictory-result:legacy-finish-recovery. WAIT#nonvictory-result-followup-20261019 적용. Server#nonvictory-recovery-server-result-20261019 기존23PASS+최종직접7PASS/형검사lint0와Unity#unity6-legacy-recovery-error-result-20261019 Final45 100PASS/Windows Player46 실행·새프로세스재시작 Passed가선행근거. 기존unversioned finish의HTTP200 accepted=false/DUPLICATE_REPLAY 수락,409본문충돌/503영수증불가별도안내·원키/본문/보관·기지급·세대보존범위.
원QA D:/Project/LASTRAIL/QA/OperatorInformation-20261019/verification.json#legacyRecoveryAndErrorReporting, RecoveryPlayer46/player.json 및restart.json. 실제기존View버튼→Controller→Durable→D파일/UnityWebRequest를localhost합성protocol로검증;Server는별도Hono/SQL.js검증. 양측시험층위분리/외부migration0124미적용/Player실Hono연결없음/가짜crypto·seeded기지급receipt를명시. 첫finishPOST4/재시작0·claim0;필수lifecycle조회유지/GET절감주장0. 완료조건은이한정원wire·클라입력/복구/안내·원보관증거대조이며전체NONVICTORY/새12node/만료issuer/서비스반환/원완결응답/장비종결/전체작전O01완주승격0. PRD오류전체부모는별도미완료.

## P-REINA-SYNC-20261102 — 레이나 음원·포격 연출 사건 계약 / 착수 가능
사용자 제작요청2026-11-02. WAIT REINA-AUDIO-FX-HANDOFF-20261102 참조. 승인 v11음원/C레벨/BGM50, v12 발사→첫착탄1.4초 연출을 입력으로20/44 최신 계약의 실제 피해소유자·commit·탄착·스킵/요약·중단·시간배율 경계를 확인/상세화. 연출탄수와피해횟수분리, 기존snapshot/승인값보존. 미확정수치·게임지연을 오디오 도식으로 자동확정하지 말고 결정필요로 표시. 소유44 및 Plan QA만 수정;20 타임라인은Cutscene 소유. 완료조건: Unity가 소비할 명시 사건/앵커/수명/오류 계약과 승인필요 목록. 완료근거 후 WAIT 후속AND에 따라 Unity 등록.
