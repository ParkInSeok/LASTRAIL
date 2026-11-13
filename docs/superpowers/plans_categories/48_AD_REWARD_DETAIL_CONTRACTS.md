# 48. 광고 스킵·룰렛·지급 복구 상세 계약

2026-09-21 번호별 통합. 이 문서는 흩어진 상세 계약을 주제별로 모은 참조본이다. 원문별 제목·출처·기존 검토 상태를 유지하고, 정책 확정·후보·과거 구현 보고를 섞어 승인하지 않는다.

**적용 순서:** 최신 사용자 결정 → 00~29 담당 정책 및 34~37 충돌 정리 → 이 문서의 충돌하지 않는 상세 계약. 원문의 작성일·완료 문구만으로 현재 구현 또는 승인 완료를 뜻하지 않는다. 원문에 있던 C/E 경로는 과거 기록이며 현재 작업 경로는 프로젝트 AGENTS.md의 D 정책을 따른다.

**공통 대체 경계:** 증강 고정 5회·시작 지급은 폐기, 적격 풀 동일 가중치 3후보·카드별 FREE3+EXTRA1·선택 중 완전 정지가 현행이다. 증강 후보 효과 수치는 출시 승인값이 아니다. 순위 보상은 성장 재화 차등 없이 외형·명예로 구분한다. 인물 역할은 20~29 최신 바이블 우선이며 과거 abilityId 매핑은 자동 변경하지 않는다. 작전물자 30/20·노드3선택/물리2방향·협동 시간·T18 HP0 등 미결정은 34와33의 미결 상태를 유지한다.

원문 내 코드 블록·JSON·표·필드·수치는 생략하지 않았다. 옛 상대 경로를 포함하는 코드 예시는 원출처 기준이며 실행 지시가 아니다. 링크와 제목 앵커만 새 위치에 맞게 변환했다.

## 상세 목차

- [운행보급 선택 광고·총배율 룰렛 상세기획 r1](#source-f35162e6aee6d319)
- [영구 광고 스킵 이용권 상세 기획 v1 — 2026-09-17](#source-d06447f1a5c3fef4)
- [광고 스킵 2차 연결 작업 범위 — 2026-09-17](#source-455b6f504f90f85b)
- [영구 광고 스킵 서버 구현계약 — 2026-09-17](#source-5cbc96b63e17ce2a)
- [영구 광고 스킵 이용권 화면·상태 명세 — 2026-09-17](#source-fcd64dcd8469bdc3)
- [영구 광고 스킵 이용권 — Unity 구현 준비 계약](#source-2b83cf73b602eca9)
- [광고 스킵 2차 Unity 실제 연결 경계](#source-3eb8098bdcb26fce)
- [전투 클리어 광고 API 구현 인계](#source-8ed6f4d6a63ee84a)
- [전투 클리어 광고 룰렛 및 확률 확정](#source-9b925fbe1a114e96)
- [전투 결과 광고 룰렛 — 서버·화면 연결 상세안](#source-f100d05ec701191e)
- [전투 룰렛 Unity 연결 — 2026-09-17](#source-85e341febad1a3f0)
- [Server 광고 보급 설계 및 실행 계획](#source-61bc2eaacb1bc07f)
- [공통 룰렛 팝업 문구·상태 검토](#source-5e53317a60633b39)
- [Augment Reroll Server Phase 2 Implementation Plan](#source-a54cbe3073573484)

<a id="source-f35162e6aee6d319"></a>
## 운행보급 선택 광고·총배율 룰렛 상세기획 r1

원출처: `WorkspaceRepo/docs/design/offline-supply-ad-roulette-20260911.md`  
원문 SHA-256: `2a8dc1925d17fe6f9727e315b625c313a153a476a911d4c4968873d1e8b86faa`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Offline supply historical P/T−P latebonus design superseded by singlefullsettlement. Server source eligibility, stablebase/approved multiplier and separated ad authorization remain.

<!-- source-body:source-f35162e6aee6d319:start -->
<a id="source-f35162e6aee6d319-운행보급-선택-광고총배율-룰렛-상세기획-r1"></a>
### 운행보급 선택 광고·총배율 룰렛 상세기획 r1

2026-09-11 Plan_2. Unity를 통해 전달된 사용자 확정 방향을 반영한 검토안. 이번 요청은 기능 방향 및 시안 제작 진행이며 런타임·광고사 연결·경제 출시 승인 또는 확률 승인이 아니다.

<a id="source-f35162e6aee6d319-수락-범위"></a>
#### 수락 범위

기본 운행보급 수령은 유지한다. 선택적으로 광고를 완료하면 해당 보급에 총2/3/4배를 적용하는 룰렛을 제공한다. 총배율은 기본분 포함이다.3배는 기본+추가3배가 아니다. 광고 취소/실패에도 기본 수령 경로를 유지한다. 배율별 확률은 미정이고 동일 크기 룰렛이나 후보3개라는 이유로1/3 확률로 결정하지 않는다.

<a id="source-f35162e6aee6d319-제안-흐름과-보상-불변-조건"></a>
#### 제안 흐름과 보상 불변 조건

보급 조회→기본 보상과 선택 광고 경로 표시→기본 받기 또는 광고 선택. 기본 버튼은 광고 완료를 요구하지 않는다. 광고 전 서버가 소유자·보급 snapshot·시도ID·provider transaction과 연결되는 도전 정보를 준비한다. 광고 보상 액수는 광고가 끝난 뒤 새로 계산한 더 긴 누적분으로 바꾸지 않는다. 누적 cutoff/새 적립 구간의 경계는 기존 보급 규칙과 함께 확정한다.

광고 SDK의 로컬 완료 이벤트만으로 지급하지 않는다. 지원 광고사의 검증된 server callback 또는 동등한 검증 경로를 사용하며 서명/검증키·provider/app/placement·고유 transaction·owner challenge·유효기간·환경 일치를 확인한다. 최종 방식은 실제 provider 문서를 확인한 뒤 명세화한다. 검증 지연은 실패와 구분해 '확인 중'으로 표시한다. 클라이언트가 배율·확률·지급량·완료 boolean을 정하지 않는다.

검증 성공 후 서버가 버전 고정된 확률표로 결과를 한 번 결정하고 동일 보급 snapshot에 저장한다. 룰렛은 저장된 결과를 보여주는 연출이다. 재접속/재시도/화면 닫기/애니메이션 스킵은 재추첨 기회가 아니다. 결과·보상 journal 없이 미리 당첨을 표시하지 않는다.

보급 snapshot B와 총배율 m에 대해 지급 목표 T=m×B이다. 실제 지급 완료액 P를 기준으로 남은 지급액만 T−P를 지급한다. 이 식은 배율 대상 재화가 확정된 후 재화별로 적용한다. 기본 수령 뒤 광고 추가 도전을 허용하는 정책은 아직 미정이다. 기본이 이미 지급된 상태에서 검증된 광고가 늦게 도착할 때 '차액만 지급'을 허용할지 '기본 경로 확정으로 닫기'를 택할지 먼저 명세해야 한다. 어느 정책이든 두 독립 전체 보상 지급 경로를 두지 않는다.

<a id="source-f35162e6aee6d319-상태동시성-제안"></a>
#### 상태·동시성 제안

snapshot/owner를 기준으로 AVAILABLE, AD_PENDING, AD_VERIFIED, RESULT_FIXED, CLAIM_PENDING, CLAIMED 등의 논리 상태와 기존 보급 지급 상태를 연결한다. 정확한 wire enum/DB 필드는 Server 설계에서 결정한다. 광고 재생 상태와 보상 지급 상태는 별개다. 광고 취소는 기본 보상을 소멸시키지 않으며 기본 수령을 장시간 잠그지 않는다.

기본 수령·광고 callback·광고 결과 지급이 경합할 때는 같은 보급 ID의 원자적 상태 전이와 누적 지급 목표 검증으로 정합성을 보장한다. provider transaction 중복·다른 owner에 재사용·다른 보급에 재사용·stale challenge·이미 완료된 보급 재청구를 거부/기존 receipt 반환으로 처리한다. 같은 요청은 원본 결과/receipt를 반환한다. payout ledger, 상태, 누적 지급액과 결과 저장은 원자적 경계로 묶어야 한다. 응답 유실 뒤 재확인으로 중복 지급하지 않는다.

snapshot에 owner, 기존 보급 식별자/cutoff와 기본 보상, reward rules version, probability version, 광고 attempt/provider transaction 연결, 결과 m, 지급 이력/receipt를 보존한다. 개인정보·광고 토큰은 UI/일반 로그에 노출하지 않는다. 보상 종류별 상한·소지 한도·우편 대체 등 기존 규칙이 있다면 총배율에도 동일한 계약을 명시한다. 과거 snapshot의 확률/결과를 새 확률표로 다시 계산하지 않는다.

<a id="source-f35162e6aee6d319-unity-화면과-복구"></a>
#### Unity 화면과 복구

보급 화면에 '기본 받기'와 '광고 보고 총2~4배'를 구분한다. 총배율과 기본 포함을 명시하고, 광고 취소해도 기본 수령 가능함을 안내한다. 보유 재화/수량/시간은 실제 응답을 사용한다.

광고 완료 확인 중에는 룰렛 확정 결과를 지어내지 않는다. RESULT_FIXED 뒤 룰렛 표시→결과 배율/총 보상/이미 받은 기본분이 있으면 추가 지급분 표시→지급 확인 완료 상태로 이동한다. 당첨 표시와 지급 완료 배지를 분리한다. 룰렛 닫기나 네트워크 단절은 원본 상태를 재조회하고 같은 결과로 복구한다. 애니메이션이 끝났다는 이유로 지급 API를 새 키로 반복 호출하지 않는다.

2/3/4배 칸의 면적/반복 수/포인터 움직임이 균등확률을 암시하지 않도록 확률 확정 전에는 검토용이라고 표시한다. 최종 제품의 확률 안내·광고 가용성·광고 로딩/실패·기본만 수령·확인 중·지급 완료/재확인 상태가 필요하다. 실제 확률 공개 방식과 플랫폼 정책은 provider/출시 환경 선정 시 확인한다.

<a id="source-f35162e6aee6d319-경제-검토-및-미정-결정"></a>
#### 경제 검토 및 미정 결정

p2+p3+p4=1일 때 평균 총배율 E=2p2+3p3+4p4. 광고 검증 성공분에 대한 추가 발행량은 B×(E−1)이다. 특정 확률은 이 문서에서 배정하지 않는다. 참여율·완료율·일일 횟수·snapshot 크기·재화별 적용 범위·기존 재화 소모처를 함께 비교해야 한다. 광고 수익을 임의 가정해 게임 재화 발행을 승인하지 않는다.

확정 필요: 세 확률과 버전, 모든 재화/일부 재화 적용, 일일·보급별 횟수, 최소 누적량·최대 배율 대상 보상, 기본 수령 후 광고 허용, 취소/실패 후 재도전과 광고 완료 지연 경합 정책, challenge 만료/late callback, 계정/기기 범위, 선택 provider와 검증 방식, 광고 미지원 계정·지역·플랫폼 처리. 이 결정이 없으면 프로덕션 지급 규칙은 미완성이다. 기본 수령 유지와 결과 재사용·중복 지급 금지는 그대로 고정한다.

<a id="source-f35162e6aee6d319-담당-영향과-검증"></a>
#### 담당 영향과 검증

Server: 기존 보급 snapshot/receipt 및 원자 지급에 선택 광고 결과를 연결. 검증 adapter·재사용 방지·확률 선택·결과 pin·기본 지급 경합 처리·동일 결과 복구. 단순 클라이언트 random/완료 boolean 금지.
Unity: 광고 lifecycle와 기본 수령을 분리, server-confirmed 룰렛·지급 상태·재접속 복구, 원 키/receipt 보존, 공급자 미가용 시 기본 경로.
Direction: 보급 CTA/룰렛 검토 시안과 기본포함 총배율·확인중·실패/완료 표현. 생성 시안 수치와 칸 크기는 확률 승인 아님.
Plan: 확률·재화 발행 영향·늦은 광고 검증과 기본수령 정책을 추적하고 실제 구현 전 명세 확정. 기존 전투 출격/영상 목표와 별도 기능 변경으로 관리.

검증: 정상기본, 취소/광고실패, SDK완료만 수신, 지연/중복/위조/다른owner callback, 기본claim과callback 동시 도착, 다중기기 claim, 결과저장/지급/응답 경계 실패, 재접속 같은결과, 확률버전 변경 뒤 replay, cutoff 뒤 새적립, reward cap. 원자성 불변 조건은 지급합계가 확정 목표를 초과하지 않으며 같은 snapshot이 이중 소모되지 않는 것이다.

<a id="source-f35162e6aee6d319-이번-상태"></a>
#### 이번 상태

Unity는 보급 광고 CTA와 룰렛2장 시안 제작 중이라고 보고했다. Plan은 문서만 작성·재읽기 확인했다. 신규 코드/광고 요청/보상 지급/경제 변경/런타임 테스트 없음. 관련 작업 전달은 이전 자동 검토의 목적지 승인 거부로 아직 미전달이며 우회 전파하지 않았다.

<!-- source-body:source-f35162e6aee6d319:end -->


<a id="source-d06447f1a5c3fef4"></a>
## 영구 광고 스킵 이용권 상세 기획 v1 — 2026-09-17

원출처: `docs/design/AD-SKIP-PERMANENT-DETAILED-20260917.md`  
원문 SHA-256: `e5c7c34a592dd0b53545b415d9575ec9ae0c1991e69cc5272df603ab1fdcd557`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Permanent14900 design adopted; 30day excluded; product entitlement server ACTIVE only, account binding/refund/restore preserved. Old fixed five augment events cannot cap latest per-card budgets.

<!-- source-body:source-d06447f1a5c3fef4:start -->
<a id="source-d06447f1a5c3fef4-영구-광고-스킵-이용권-상세-기획-v1--2026-09-17"></a>
### 영구 광고 스킵 이용권 상세 기획 v1 — 2026-09-17

<a id="source-d06447f1a5c3fef4-결정-수준"></a>
#### 결정 수준
사용자 확정: 초기 상품은 영구권만 제작,30일권은 출시 범위에서 제외. 직전 추천안14,900원을 가격 기획 기준으로 이어간다(스토어 실제가격/상품등록/출시 승인과 별개). 아래 상세 운영 규칙은 이번 작성안이며 과거 확정 기록으로 소급하지 않는다. 원안 AD-SKIP-PRODUCT-DRAFT-20260917.md 보존. 이번 작업은 기획만, 런타임·DB·스토어 변경 없음.

<a id="source-d06447f1a5c3fef4-1-상품"></a>
#### 1 상품
- 표시명: 광고 스킵 이용권. 보조표시: 영구 / 1회 구매.
- 한국 가격 기획 기준14,900원. 가격 버튼은 출시 시 스토어의 현지화된 실제가격 사용; 숫자 하드코딩 금지.
- 게임 계정당 하나의 유효 권한. 기간만료·자동갱신·일일 스킵권 수령 없음. 별도 재화/성장스탯/전용확률 묶음 없음.
- 영구는 유효한 구매권한에 별도 만료일이 없다는 의미. 환불·구매취소로 회수된 권한까지 보장한다는 뜻이 아님.
- 30일권·소모성 스킵권은 초기 카탈로그에 넣지 않는다. 과거 문서의 두상품 비교는 역사적 제안.

<a id="source-d06447f1a5c3fef4-2-적용과-한도"></a>
#### 2 적용과 한도
|기능|보유자 동작|유지할 규칙|
|운행 보급|광고 없이 동일 보급 룰렛 진입|해당 적립분 한 번 수령,기본/광고/스킵 중복불가|
|작전 완료|광고 없이 해당 클리어 룰렛 진입|같은 결과 한 번 수령,실패/포기 자격 자동확대없음|
|증강 리롤|각 슬롯의 기존 광고 추가1회를 시청 없이 사용|무료3+추가1,다음 새 이벤트 초기화,같은 이벤트 재접속 초기화금지|
한 판 증강5회×3슬롯이므로 기존 추가리롤 최대15회분 시청을 생략할 수 있다. 스킵상품 자체 일일횟수 제한은 두지 않는다. 각 기능에 정당하게 설정된 이용자 공통 자격/한도는 그대로이고, 아직 미정인 광고 일일한도를 이 문서에서 숫자로 새로 확정하지 않는다. 광고SDK 미지원/미송출이어도 유효권한·게임보상정책이 있으면 이용권 경로는 작동하게 설계. 보상정책 미설정/서버점검까지 무시하는 권한은 아니다.
현재 포함범위를3종으로 명확히 표시. 미래 신규광고기능의 포함 여부는 출시 전에 상품설명/정책에 명시하며 기존3혜택을 슬쩍 유료 분리하지 않는다.
룰렛2/3/4배60/30/10 유지. 리롤은 후보풀·동일가중치·중복조건을 기존정책대로 유지. 스킵 사용 후 광고로 동일 추가회 다시 지급하지 않는다.

<a id="source-d06447f1a5c3fef4-3-상점과-구매-진입"></a>
#### 3 상점과 구매 진입
상점 편의상품 섹션에1카드: 아이콘/이름/영구표시/세혜택/실가격 구매버튼. 보유자는 구매버튼을 보유 중으로 바꾸고 중복구매 차단. 구매복원은 상점 또는 계정설정의 명시 동작으로 제공.
상품 설명 제안: 광고 시청 없이 운행 보급·작전 완료 룰렛과 증강 추가 리롤을 이용합니다. 보상 확률과 이용 횟수는 기존과 동일합니다.
비보유 광고버튼 주변에 작은 이용권안내 진입만 허용; 광고누를때마다 강제판매팝업 없음. 전투 증강선택 화면에서는 신규 상점/결제모달을 강제로 열지 않고 기존 무료/광고기능 유지. 로비에서 미리 구매하는 흐름 우선.
로그인된 게임계정을 확인한 뒤 구매 시작. 결제UI성공 신호만으로 활성화하지 않고 서버영수증검증 후 권한 활성화. 취소는 원화면으로,검증지연은 구매 확인 중+다시 확인/닫기; 재결제 유도금지. 앱종료후 동일거래 복구.
계정전환 중 완료된 구매는 구매 시작 때 서버에 결속한 계정으로 정산; 다른계정에서 재사용 금지. 스토어 구매복원과 게임 로그인복원은 구분한다. 다른OS스토어로 권한공유/구매이전은 플랫폼 계약확인 전 지원으로 광고하지 않는다.

<a id="source-d06447f1a5c3fef4-4-보유자의-룰렛-흐름"></a>
#### 4 보유자의 룰렛 흐름
보급/결과 화면: 기존 광고 버튼을 룰렛 돌리기로 변경하고 광고 스킵 적용을 작은 보조문구로 표시. 기본 받기 선택 유지.
입력1회→해당 보상기회 조회/구매권한 서버확인→스킵승인 저장→기존spin 명령→확정영수증에 맞춘 공통연출. 클릭한 뒤 팝업에서 다시 돌리기를 요구하지 않는다. 서버내부 요청이 여러개여도 사용자는 하나의동작.
처리 중에는 버튼중복잠금,광고SDK 호출없음/광고 확인 중 문구없음. 통신실패시 결과 확인 중/다시 확인으로 동일기회를 복구. 이미확정결과면 재추첨/재지급/재회전없이 기존결과표시.
기존6.5초회전,배수글자정방향,회전중기본카드유지,정지후0.6초수량증가,확인활성 유지. 광고스킵과룰렛연출스킵은다름.

<a id="source-d06447f1a5c3fef4-5-증강-리롤-흐름"></a>
#### 5 증강 리롤 흐름
각슬롯 기본 리롤3/3부터 사용.무료소진후 보유자버튼 추가 리롤1/1(광고스킵적용),미보유자는광고리롤1/1. 추가소진후 이번 선택 사용 완료.
승인에묶는키: account/run/증강event/slot/추가회차 및현재후보revision.무료/광고/스킵 입력은기존동일리롤유스케이스에연결. 서버승인/카드갱신이원자적으로확정되며권한조회실패만으로횟수소모없음. 응답유실후재요청은이미확정된동일후보와잔여횟수복원,클라이언트가임의다시뽑지않음. 이미다른후보를최종선택해이벤트닫혔으면늦은응답으로화면/슬롯변경금지.
협동은현재선택권을가진계정의권한과그계정에귀속된추가회만사용.팀공유리롤소유권이확정되지않은경로는임의로동료이용권을빌려쓰지않고계약완성전활성화차단.동료보상자동면제없음.

<a id="source-d06447f1a5c3fef4-6-서버-소유권과-중복-방지"></a>
#### 6 서버 소유권과 중복 방지
상품권한: UNKNOWN/PENDING/ACTIVE/REVOKED를 계정권한모델이소유,UI캐시는표시용.구매상태와개별보상시도상태를하나의bool로합치지않음.
사용승인경로: AD_SSV 또는 PURCHASE_ENTITLEMENT를명시저장.유료사용을가짜SSV/provider거래로작성하지않는다.구매원거래는여러정당한스킵사용을허용하는권한근거;각사용승인은별도의고유보상기회에한번만귀속.
기존보급/전투intent·보상미리보기·추첨고정·단일지급과리롤잔여횟수소유자를재사용.한기회에서광고와상품승인이경합해도하나의권한/결과로합류.한스토어거래를다른계정에중복결속금지.가격/구매성공/배수/지급수량/리롤잔량을클라이언트입력으로신뢰하지않음.
View는표시와의도콜백만,Controller가기존유스케이스호출,상점영수증/권한서비스는외부검증담당.광고공급자와구매권한은승인경계에서분리하고화면별동작복제금지. Bind/Unbind/계정교체후응답폐기필수.

<a id="source-d06447f1a5c3fef4-7-환불오류복원-상세안"></a>
#### 7 환불·오류·복원 상세안
- 서버에서권한회수확정된뒤새스킵승인금지.이후미보유흐름으로전환.
- 회수와사용승인은직렬화된권한revision/서버순서로판정: 회수가먼저면거절,유효권한으로내구성있는사용승인이먼저완료됐다면그기회는기존기한안에서끝까지복구허용.버튼을눌렀다는사실만으로승인확보아님.
- 기존지급확정영수증은유지.일반취소처리가과거보상을자동재추첨/임의차감하지않음.부정거래사후제재는별도운영정책.
- 구매보유중네트워크확인불가: 이용권 확인 필요+재확인.자동광고재생없음,기본보상수령선택가능.기본수령시해당결과의룰렛기회닫힘을기존규칙대로안내.
- 재설치/새기기같은게임계정: 서버권한조회복원.영수증미반영시스토어복원→서버검증.스토어계정동일하지만다른게임계정이면기존귀속안내/문의,자동이전없음.
- 앱백그라운드/팝업닫기/계정변경:뷰연출/구독중단,서버승인·확정결과보존,다시열면복구.결제/보상타임아웃을실패확정으로오인해새결제유도금지.

<a id="source-d06447f1a5c3fef4-8-로컬-검증과-출시-잔여"></a>
#### 8 로컬 검증과 출시 잔여
필수검증: 비보유SSV기존회귀,보유SDK호출0,3기능동일규칙,기본/광고/상품경합단일지급,영수증중복/위조/타계정거부,구매응답유실복원,권한회수경합,동일event재접속횟수보존,협동귀속,기본정보없는연출중립표시,긴수량,계정변경후콜백차단.
추적지표: 상품조회/구매시작/검증성공/복원성공/사용기능별횟수/회수후차단/복구오류.원영수증비밀값로그금지.광고시청감소·보상경로선택률·실플레이시간·성장재화발행량을보며가격타당성평가.광고면제로이용빈도가늘수있으므로성장영향없다고홍보하지않음.
출시전필수미결: 실제플랫폼상품ID/영수증검증·환불통지·계정귀속설정/현지화가격/구매복원실기기/스토어간권한범위/협동리롤소유권/운영광고한도. 실제등록·배포·결제·지급은이번기획권한에포함하지않는다.

<a id="source-d06447f1a5c3fef4-9-서버-계약-검토-반영--v11-상세-보완안"></a>
#### 9 서버 계약 검토 반영 — v1.1 상세 보완안
아래는 서버 읽기검토에 따른 설계보완이며 기존 구현/출시 설정 완료가 아니다. 위 일반 표현과 충돌하면 이 절이 우선한다.

<a id="source-d06447f1a5c3fef4-보상-기회기한확률-보존"></a>
##### 보상 기회·기한·확률 보존
보급은 이미 AD_VERIFIED인 시도에 별도 spin 기한을 적용하지 않는 현 계약을 유지한다. 전투는 승인 후에도 원래 rewardPreview 만료를 적용한다. 상품권한 확인 기한, 광고SSV 콜백 기한, 게임보상 기회 만료는 서로 구분한다. 상품 경로에 SSV 콜백 기한을 새 지급 기한으로 붙이지 않는다.
아직 승인되지 않은 AD_PENDING이 광고 콜백 기한만 만료된 경우, 기본수령 미소비/보상 기회 유효/유효 구매권한 조건을 모두 충족하면 같은 intent를 상품경로로 승인 가능하게 하는 상세안이다. 단순 만료표시만으로 새 보상기회를 만들지 않는다. 전투 preview 자체가 만료됐거나 이미 기본수령됐다면 전환 불가. 사용승인 후 권한회수는 §7의 서버 선후 판정을 따른다.
60/30/10은 신규 기회 출시정책 기준. 과거 보급 intent의 고정 weightsBps, 기본보상핀, 정책버전,0038 고정배율 및 지급영수증은 그대로 보존. 구매/승인원인 전환을 이유로 기존 시도를 현재확률로 다시 만들거나 재추첨하지 않는다. 신규 운영정책 설정은 별도 작업.

<a id="source-d06447f1a5c3fef4-광고-전용-조건과-공통-한도"></a>
##### 광고 전용 조건과 공통 한도
게임자격/보상정책/기회 소유권과 adUnit/appId/provider/challenge 조건을 분리한다. 유료경로는 광고공급자 등록/가용성을 필요로 하지 않되 유효한 게임보상정책은 필요하다. 현재 코드는 이 분리가 구현되지 않았으므로 기존 create를 그대로 호출하면 된다고 주장하지 않는다.
maxAdsPerDay의 기존 의미는 현재 광고 intent 생성 한도이며 실제시청수와 동일하지 않다. 영구권 경로에는 이 광고 전용 한도를 새로 부과하지 않는 상세안. 경제상 공통 기회 한도가 필요하면 별도 명시된 gameRewardOpportunityLimit으로 양 경로에 동일 적용하도록 정책을 승인받고 설정한다. 현재 미정 수치를 임의 도입하지 않는다. 광고→상품전환은 같은 보상기회1건이며 기존 광고생성 카운트를 되돌려 광고한도를 재충전하지 않는다. 광고요청 없이 시작한 상품사용은 광고생성카운트를 증가시키지 않는다.

<a id="source-d06447f1a5c3fef4-구매-거래복원회수"></a>
##### 구매 거래·복원·회수
현재 verified_store_orders.product_kind는 PASS/CRYSTAL만 지원하므로 영구 비소모상품 종류·권한 저장·영수증검증·복원/회수 이벤트 확장이 필요하다. 실제 product ID와 허용 환경 매핑은 출시 전 명시 등록. LOCAL/DEV 영수증으로 PRD 권한 생성 금지.
공급자 사건 순서/서버 거래상태를 검증하고 회수된 동일 거래가 늦은 구매응답/복원으로 ACTIVE로 되돌아가지 않게 한다. 복원은 새 구매가 아니다. 환불 후 정당한 별도 신규거래가 플랫폼에서 허용되고 서버에서 검증되면 새 권한취득 가능하되 취소된 원거래 재사용 금지. 구매완료시 실제로그인계정이 달라져도 원구매계정 결속 유지.

<a id="source-d06447f1a5c3fef4-승인-경합늦은-광고-콜백"></a>
##### 승인 경합·늦은 광고 콜백
유일 승인키는 보상기회 자체이며 AD_SSV/PURCHASE_ENTITLEMENT를 키에 포함해 두개의 승인행을 만들지 않는다. 첫 승인원인/근거거래/권한revision을 보존하고 후발 승인은 같은 결과로 합류. 기존SSV승인을상품원인으로 덮어쓰지 않는다.
상품승인후 늦은SSV는 서명·원challenge·계정/앱/환경/거래유일성이 유효한 경우에만 기존provider영수증 경계에 한 번 기록하여 다른기회 재사용을 막고, 이미승인된동일기회로 멱등 응답한다. 추가추첨/지급/무료회충전없음. 위조·다른기회 재사용은 거부한다. 이는 현재 AD_PENDING 외 거절 경로의 명시적 구현 변경 대상이며 이미 지원되는 동작이 아니다. 역사CLAIMED 응답과 고정결과를 우선한다.

<a id="source-d06447f1a5c3fef4-리롤-실패종료-경합"></a>
##### 리롤 실패·종료 경합
대체 가능한 후보가 없으면 기존카드 유지,무료/추가횟수 모두 미소모. 선택확정/이벤트종료와 리롤의 후보revision·잔여횟수 변경을 서버 원자경합으로 판정하여 늦은 응답을 UI에서 가리는 것만으로 처리하지 않는다. 현재 활성서버 리롤 명령 구현은 확인되지 않았으므로 §5의 기존유스케이스 재사용은 설계상 공통 명령을 뜻한다. 실제 부재시 무료/광고/상품이 공유하는 명령부터 구현해야 한다.

<!-- source-body:source-d06447f1a5c3fef4:end -->


<a id="source-455b6f504f90f85b"></a>
## 광고 스킵 2차 연결 작업 범위 — 2026-09-17

원출처: `docs/design/AD-SKIP-PHASE2-SCOPE-20260917.md`  
원문 SHA-256: `4b91dbf73252465087c13da9704aec4b4480b32db31157c9402ec1f250bd8197`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Phase2 reroll eligibility and preview/authorize routes retained as implementation scope; latest per-card FREE3+EXTRA1 supersedes shared-extra interpretation.

<!-- source-body:source-455b6f504f90f85b:start -->
<a id="source-455b6f504f90f85b-광고-스킵-2차-연결-작업-범위--2026-09-17"></a>
### 광고 스킵 2차 연결 작업 범위 — 2026-09-17

사용자1차 결과 후 ㄱㄱ. 현재2차 시작이며 완료아님. 첫 출시스토어는 사용자질문중. 답변전플랫폼별SDK설치/운영SKU/실스토어호출하지않는다.

<a id="source-455b6f504f90f85b-증강-추가-리롤"></a>
#### 증강 추가 리롤
우선 현재runtime/server에 실제grant발급·카탈로그·선택명령·잔여횟수 소유자가 있는지 확인한다. 소스탐색상Unity Scripts에는새AdSkipScope표시외 reroll 실행접점이확인되지않음. 문서정책을이미구현된서비스로가정하지않는다.
Server2는실제권위경계·계획·DTO와범위 내 로컬구현,Unity2는같은계약의서비스/controller/View연결을담당. 실제맵진행grant발급이없다면서버단위/fixture검증과실제전투연결미완료를분리하고광고상품REROLL AVAILABLE을허위로켜지않는다. 후보목록·기회생성·잔량을클라이언트주장에맡기지않음.
정책: 후보3장,슬롯별무료3+광고또는영구권추가1,새grant초기화·같은grant복원유지,직전및다른2장제외후동일가중치,후보소진미소모.무료/광고/구매경로동일명령.리롤입력은카드선택으로전파금지.최종선택/종료/회수/광고콜백과원자경합검수.
최신풀정책 AUGMENT-POOL-40-POLICY 및실제구현가능풀을대조;16종역사문구를상한으로사용하지않음.효과미구현카드를문서에있다는이유로실플레이풀에포함하지않는다.
협동기획은 COOP-UNIFIED-AUGMENT-CHOICE-20260916.md에공용grant3+1·현재담당리롤 규칙이존재.계획존재와실제권위연결을구분하며45초/A등미확정시험값을운영에임의확정하지않음.

<a id="source-455b6f504f90f85b-구매복원"></a>
#### 구매·복원
현재Unity manifest에com.unity.purchasing 미등록. 기존서비스/provider/journal경계를먼저조사. 실제플랫폼은사용자응답후선정하고공식문서·지원버전확인후로컬adapter로이어간다. 가격14,900은기획기준이며실상품미조회상태에서성공결제UI를만들지않음. 테스트격리와실제외부거래를구분.

<a id="source-455b6f504f90f85b-보존과-보고"></a>
#### 보존과 보고
1차905서버/104Unity회귀는과거검증근거.새코드검증으로재사용표현금지.원화승인/룰렛6.5+.6/정방향·긴수량보정/기본수령·기존광고유지.모든산출물D.실DBmigration/배포/SDK광고시청/스토어실구매/START/운영지급없음.새구현별계획·source핀·검증·남은연결을각역할handoff에기록.

<!-- source-body:source-455b6f504f90f85b:end -->


<a id="source-5cbc96b63e17ce2a"></a>
## 영구 광고 스킵 서버 구현계약 — 2026-09-17

원출처: `docs/design/AD-SKIP-SERVER-CONTRACT-20260917.md`  
원문 SHA-256: `48b13dc3140763a6dffed9c5af36b5f32b80ab7908c20a1941bd2bfd6d39d576`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Preserve exact request bodies, v2 header, strict idempotency grammar, provenance, error vocabulary, deadlines, history/revocation tombstones and cause/origin semantics. Per-card budget overrides any shared scope.

<!-- source-body:source-5cbc96b63e17ce2a:start -->
<a id="source-5cbc96b63e17ce2a-영구-광고-스킵-서버-구현계약--2026-09-17"></a>
### 영구 광고 스킵 서버 구현계약 — 2026-09-17

상태: **설계 제안 / 미구현 / 미적용**. 근거는 [상세안 v1.1](48_AD_REWARD_DETAIL_CONTRACTS.md#source-d06447f1a5c3fef4)의 §9 우선 규칙이다. 이 문서는 구현·마이그레이션 실행·스토어 등록·배포·실결제·운영 지급을 승인하지 않는다. 원본 상세안은 수정하지 않는다.

영구 비소모 단독상품, 한국 가격 기획 기준 14,900원. 기간형·소모성 권종은 제외한다. 실제 가격은 Store 현지화 표시값이며 서버가 가격을 신뢰해 권한을 지급하지 않는다. 실제 SKU/플랫폼 매핑은 등록 전 미확정이며 가상 운영값을 만들지 않는다.

<a id="source-5cbc96b63e17ce2a-1-조사-기준과-현재-구현"></a>
#### 1 조사 기준과 현재 구현

활성 checkout: `D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server`. 아래 소스 경로는 이 디렉터리 기준이다. Git top-level은 상위 attendance-pass-20260907, common-dir은 `D:/Project/LASTRAIL/.git`, 조사 시 HEAD는 `d4df4246762b6fc9677fc1e3126f9483033872eb`. 미커밋 변경이 있으므로 HEAD만으로 소스 동일성을 주장하지 않는다. 2026-09-17 현재 파일을 직접 읽었으며 최대 migration은 0038이다.

|현재 파일 / 함수·스키마|실제 역할|재사용 및 변경 경계|
|---|---|---|
|apps/game-api/src/crystal-orders.ts: verifiedStoreProof, storeOrderInsert, StoreIdentity/StoreProof|계정·환경·provider·app·product 일치 및 검증 시각 검사. kind는 PASS/CRYSTAL|검증 경계 재사용, 영구권 kind 및 취소 사건 모델 확장 필요. 현재 함수가 실제 Store adapter는 아님|
|apps/game-api/src/crystal-service.ts: claimCrystals|CRYSTAL 구매와 재화 지급|영구권에서 이 지급 명령 호출 금지. 영수증 검증/트랜잭션 패턴만 참고|
|apps/game-api/src/pass50-purchase.ts: purchasePass50|INTENT/RECONCILE, 계정 결속, 영수증 hash와 request receipt|계정 결속 패턴 재사용. 시즌 종료·10분 checkout·코스메틱 조건을 영구권에 복사하지 않음|
|apps/game-api/src/activity-service.ts: claimActivity|구형 PASS 구매·출석 등|광고 스킵 구매 라우트로 전용하지 않음|
|apps/game-api/src/crystal-refunds.ts: reconcileCrystalOrder|서버 전용 검증된 회수 경계, 주문/패스 회수와 crystal lot 회수|새 권한 회수는 별도 명령. 기존 경제 회수 부작용을 호출하지 않음. 현재 공개 webhook 아님|
|apps/game-api/src/shop-routes.ts: registerShopRoutes|GET /v1/shop, crystalProducts의 price=null/STORE_REQUIRED|편의상품 1개와 권한/가용성 표시 확장 제안. 실제 금액 하드코딩 금지|
|packages/db/migrations/0023_crystal_wallet.sql|verified_store_orders: kind CHECK PASS/CRYSTAL, UNIQUE(provider,app_id,transaction_id), revoked_at|전역 거래 유일성 유지. 신규 kind는 CHECK 변경 필요, 단순 INSERT 불가|
|packages/db/migrations/0033_recurring_objectives.sql|pass_checkout_intents, pass50_entitlements|영구권 저장소로 사용하지 않음|
|apps/game-api/src/offline-ad-service.ts: createOfflineAdIntent/readOfflineAdCreationReplay/readOfflineAdIntent/recordOfflineAdVerification/spinOfflineAd|보급 baseline별 intent, SSV 승인, 최초 결과 고정 및 원자 지급|동일 intent·base/policy·baseline·영수증 재사용. 승인 경계만 상품까지 확장|
|apps/game-api/src/offline-ad-policy.ts|고정 weightsBps, 4재화 배수 계산, 광고 조건을 포함한 정책|보상 정책과 광고 delivery 조건 분리. 과거 policy_json은 그대로 해석|
|packages/db/migrations/0036_offline_ad_intents.sql|baseline 유일성, AD_PENDING/AD_VERIFIED/CLAIMED CHECK|현재 승인/지급 상태는 provider_transaction_id/verified_at 필수. 상품 승인은 현 스키마에 저장 불가|
|packages/db/migrations/0038_offline_ad_fixed_results.sql|intent당 multiplier/policy_json/base_json 최초 저장|보존. 재추첨·backfill 금지. payout 실패 후 고정 결과 복구|
|apps/game-api/src/combat-ad-service.ts: createCombatAdIntent/readCombatAdIntent/recordCombatAdVerification/spinCombatAd|preview당 intent, RESULT_FIXED 후 지급|동일 preview/intent/결과를 재사용, 상품승인 분기 추가 필요|
|apps/game-api/src/run-reward-settlement.ts: settleRunReward|기본/전투 룰렛 정산, 원자 preview 소비|현 광고 지급 guard는 ad_provider_receipts ADMOB JOIN 필수(약162행). 검증된 단일 승인 근거의 AD/상품 분기로 바꿔야 하며 전체 guard 삭제 금지|
|packages/db/migrations/0037_combat_ad_intents.sql|전투 상태 CHECK, preview UNIQUE, ad_provider_receipts|승인 이후 provider 필수 CHECK 변경 필요. 공유 receipt는 provider 거래 유일성과 consumer/intent 유일성을 유지|
|apps/game-api/src/offline-ad-routes.ts: registerOfflineAdRoutes|공유 GET /v1/admob/ssv → verifyAdmobCallback → 각 record 함수|서명 검증/원challenge 바인딩을 유지. 상품 사용은 이 라우트를 호출하지 않음|
|apps/game-api/src/ad-provider-receipts.ts: providerReceiptStatement|LEGACY/OFFLINE/COMBAT 거래 재사용 방지|가짜 거래 저장 금지. 늦은 유효 SSV 관찰에만 사용; REROLL 지원은 별도 확장 필요|

현재 game-api/src의 reroll/리롤 검색에서 서버 리롤 명령을 찾지 못했다. `docs/design/AUGMENT-UNIFIED-POOL-POLICY-20260915.md`, `AUGMENT-POOL-40-POLICY-20260915.md`는 정책 근거이며 구현 증거가 아니다. 현재 구매복원/영구권/상품 사용승인 API도 확인되지 않았다.

<a id="source-5cbc96b63e17ce2a-2-책임과-내부-계약-제안"></a>
#### 2 책임과 내부 계약 제안

새 책임 단위(모두 apps/game-api/src 아래 제안 파일):

|파일|소유 책임과 공개 명령 제안|
|---|---|
|ad-skip-purchase.ts|beginAdSkipCheckout, verifyAdSkipPurchase, restoreAdSkipPurchase: 원계정 결속·검증·주문/권한 원자 저장|
|ad-skip-entitlement.ts|readAdSkipEntitlement, revokeAdSkipOrder: 계정/환경/상품의 권한 revision과 회수. 기존 보상 차감 없음|
|reward-opportunity-approval.ts|authorizeRewardOpportunity: 게임 기회당 최초 승인 원인·근거 저장, AD/상품 합류|
|ad-skip-routes.ts|인증, strict 입력, idempotency, 오류 번역. 도메인 판정/추첨을 직접 구현하지 않음|
|augment-reroll-service.ts|미구현 gap을 채울 공통 명령. 무료/광고/상품 모두 동일 후보·잔량·종료 규칙 사용|

Store adapter는 `verifyPurchase(expectedIdentity, checkoutBinding, opaqueReceipt)` 및 `verifyRevocation(orderIdentity, opaqueEvent)` 경계로 주입한다. 외부 호출 완료 후 DB commit에서 주문/권한 revision을 다시 검사한다. 외부 통신을 DB 원자성으로 착각하지 않는다. StoreProof의 현 60초 신선도 조건만으로 늦은 환불/구매 사건 순서를 보장할 수 없으므로 검증 결과에 신뢰 가능한 원거래 identity, 취소 상태, 사건 identity/순서 근거를 추가해야 한다.

권한 저장의 진실은 계정권한 모델 하나이다. ACTIVE/REVOKED와 revision, 활성 order를 관리한다. UNKNOWN은 조회 실패를 포함한 관측 상태, PENDING은 아직 검증되지 않은 구매 상태이며 둘을 ACTIVE 저장값으로 취급하지 않는다. 비보유는 `owned=false, entitlement=null`로 명시하고 네트워크 실패 UNKNOWN과 구분한다. 구매중 여부는 별도 checkout 상태이며 기존 ACTIVE 권한을 PENDING으로 덮지 않는다.

사용 승인 입력은 서버가 확정한 actor(account/environment), OpportunityRef, idempotencyKey이다. OpportunityRef는 OFFLINE(intent 또는 baseline), COMBAT(run/preview/intent), REROLL(run/event/slot/extraOrdinal) 판별 합집합이다. 정책·배수·잔량·계정은 클라이언트가 정하지 않는다. candidateRevision은 리롤 CAS 전제이지 새 추가회 키가 아니다.

<a id="source-5cbc96b63e17ce2a-3-저장마이그레이션-계획--실행-sql-아님"></a>
#### 3 저장·마이그레이션 계획 — 실행 SQL 아님

0038 이후 번호는 구현 직전 최신 목록을 확인해 배정한다. 아래 묶음은 제안이며 파일을 생성하거나 적용하지 않았다. seed/운영 SKU/권한 자동부여 없음.

1. **영구 상품 거래와 권한**: verified_store_orders kind에 AD_SKIP_PERMANENT 허용. 현 CHECK를 실제 SQLite/D1에서 바꾸려면 테이블 재구성 및 외래키/트리거/인덱스 보존 검증이 필요하다. 원거래 UNIQUE는 약화하지 않는다. 별도 중복 주문 저장소로 거래 유일성을 우회하지 않는다.
   - ad_skip_checkout_intents: id, account, environment, catalog version, provider/app/product, server binding, 생성/구매시작 허용기한, 검증 상태. 구독 만료일 아님.
   - account_ad_skip_entitlements: PK(account,environment,entitlement_key), revision, ACTIVE/REVOKED, active_order_id, updated_at. 만료일 null. 주문 원소유자와 환경 일치를 commit에서 확인.
   - ad_skip_order_events: 검증된 provider 사건 identity, order, 상태/순서 근거, processed_at; 중복 사건 유일성. 원문 토큰 저장/로그 금지.
   - ad_skip_request_receipts: UNIQUE(account,environment,operation,key), canonical body digest, 고정 응답/참조. checkout/verify/restore/authorize 키 영역 구분.
2. **게임 기회 승인 원인**: reward_opportunity_approvals에 UNIQUE(environment,kind,opportunity_key), account, intent 참조, cause AD_SSV/PURCHASE_ENTITLEMENT, approved_at, order_id/entitlement_revision 또는 provider receipt 근거. cause를 UNIQUE 키에 포함하지 않는다. AD에는 order가 없고 상품에는 provider 거래가 없어야 한다. 다형 참조의 실제 부모·계정 일치는 각 소비자의 원자 SQL guard로 검증한다.
3. **기존 intent 제약 변경**: offline/combat 테이블 재구성 계획. 기존 ID·자연키·정책/base JSON·시각·spin key·영수증을 보존한다. 승인 상태의 공급자 필수 조건을 원인별 승인 근거 조건으로 바꾼다. 새 상품전용 intent는 광고 challenge/user_binding/SSV 기한이 null이고 광고 등록 정보도 불필요하다. null은 무조건 승인 의미가 아니다. AD_PENDING/AD_VERIFIED 등 기존 상태명은 호환용 내부 이름으로 남길 수 있으나 승인 진실은 공통 approval이다. 상태 전이와 approval 쓰기는 항상 같은 batch; 외부 응답에는 approvalCause 추가, 화면은 canSpin을 사용한다.
4. **역사 데이터**: 기존 승인/고정/CLAIMED 중 실제 provider 근거가 있는 행만 AD_SSV 승인 provenance로 이관할 수 있다. 불일치·거래충돌은 실패로 보고하고 임의 수선하지 않는다. 이는 승인 근거 이관이지 0038 추첨결과 backfill이 아니다. 과거 CLAIMED response_json/result_json은 바이트 보존하고 우선 재생한다. 역사 결과를 새 형식으로 다시 지급하지 않는다.
5. **광고 생성 카운트**: ad_intent_origins 같은 별도 provenance에 (kind,intent), origin=AD_REQUEST/PURCHASE_ONLY, original_created_at을 고정한다. 기존 행은 AD_REQUEST. 현재 count(*) 전체 intent 조회를 AD_REQUEST만 세도록 교체한다. 상품전환으로 origin이나 기존 광고 카운트 변경 금지. 새 상품전용 intent는 count에서 제외한다.
6. **리롤**: event 상태/선택권자/revision, slot별 후보·무료잔량·추가소비, request receipt와 승인 FK가 필요하다. 런/event 권위·협동 소유권 확정 전 스키마 세부 적용/라우트 활성화를 보류한다. 광고 REROLL을 연결할 때 공유 provider receipt consumer 및 기존 교차기회 재사용 검사를 함께 확장한다.

재구성은 외래키 보존·연결된 pass/crystal lots·0038 FK·원영수증 수/내용·unique/trigger·실패 rollback을 격리 DB에서 검증해야 한다. D1의 실제 migration 제약 검증 전 운영 적용 가능한 SQL이라고 주장하지 않는다. 구서비스는 상품승인을 이해하지 못하고 0038 고정기록도 버전별로 무시할 수 있다. 신구 혼합 writer 금지, 호환되지 않는 구버전 rollback 금지. 배포·중단·백업 절차는 별도 승인 작업이다.

<a id="source-5cbc96b63e17ce2a-4-http-계약-제안과-충돌-확인"></a>
#### 4 HTTP 계약 제안과 충돌 확인

인증된 계정과 실행 환경은 서버에서 결정한다. 모든 조회 no-store, 모든 변경은 Idempotency-Key(기존 ASCII 1..128 규칙)와 strict body를 사용한다. 키 재사용+다른 body는409. 거래 원문은 bounded opaque 입력이며 원문 로그 금지. 수량/배수/가격 입력을 허용하지 않는다.

|제안 경로|입력|응답과 의미|
|---|---|---|
|GET /v1/ad-skip|빈 body/query|owned, entitlement{status,revision,permanent:true} 또는 null, purchaseState, catalog{productId/provider/appId 또는 null,price:null,priceSource:STORE_REQUIRED}, purchaseAvailable, featureAvailability, serverTime. 단순 조회는 복원/지급 안 함|
|POST /v1/ad-skip/checkout-intents|{catalogVersion,provider}|계정/환경에 결속한 checkoutIntentId, 실제 설정 productId/appId, opaque storeAccountBinding, purchaseStartBefore. ACTIVE면 중복 결제 시작 차단|
|POST /v1/ad-skip/purchases/verify|{checkoutIntentId,receipt}|검증된 orderRef, 처리 결과, entitlementRevision 참조. 취소/미검증은 권한 생성 없음. 늦은 완료는 결속된 원계정으로만 반영|
|POST /v1/ad-skip/purchases/restore|{provider,receipt}|동일게임계정 귀속의 검증된 기존 거래 재확인. 신규 checkout 필수 아님. 다른 계정 자동이전·회수 거래 재활성화 없음|
|POST /v1/ad-skip/uses/authorize|{opportunity:{kind:OFFLINE,intentId}} 또는 {kind:OFFLINE} / {kind:COMBAT,runId,rewardPreviewId,intentId?}|기존기회 생성/재사용 및 상품승인. intentId, approvalCause, canSpin, 기존 조회/spin 경로 반환. 추첨·지급은 기존 spin이 담당|

OFFLINE에서 intentId 생략 시 현재 baseline을 서버가 고정하고 요청 영수증으로 같은 intent에 귀속한다. 재요청 때 새 baseline으로 넘어가면 안 된다. COMBAT의 run/preview가 기회 identity이다. intentId가 있으면 모든 식별자가 일치해야 한다. 이미 AD_SSV 승인이라면 권한이 없어도 그 기회의 기존 승인 결과를 재생할 수 있으나 새 상품승인은 만들지 않는다.

리롤 제안은 `POST /v1/runs/:runId/augment-events/:eventId/slots/:slot/reroll`, body `{expectedRevision,mode:FREE|EXTRA,approvalSource?:AD_SSV|PURCHASE_ENTITLEMENT}`. mode/source는 요청 의도일 뿐 자격 증명이 아니다. 권한 승인·후보 선택·잔량 소비·영수증을 한 batch에 저장하고 결과를 반환한다. 무료/추가 명령을 별도 서비스로 복제하지 않는다. 서버 권위 gap 해결 전 `reroll_contract_unavailable`로 닫으며 상품 사용승인 API에 미완성 REROLL을 열지 않는다.

현재 확인된 경로:
- /v1/wallet/crystals/purchases/verify → registerCrystalRoutes/claimCrystals.
- /v1/passes/:activityId/purchases/verify → registerActivityRoutes/claimActivity.
- /v1/passes/:policyId/checkout-intents 및 /purchases/reconcile → registerRecurringRoutes/purchasePass50.
- /v1/offline-rewards/ad-intents, /:intentId, /:intentId/spin → registerOfflineAdRoutes.
- /v1/runs/:runId/reward-preview/:rewardPreviewId/ad-intents, /:intentId, /:intentId/spin → registerCombatAdRoutes.
- /v1/admob/ssv → 공유 서명검증.

활성 apps/game-api/src에서 `/v1/ad-skip`, `/v1/entitlements`, `/v1/store`를 검색했으며 기존 등록을 찾지 못했다. 새 prefix는 위 경로와 충돌하지 않는다. 리롤은 현재 소스 검색상 명령 부재이며 등록 시 Hono 라우트/와일드카드 우선순위 통합검사가 필요하다. 기존 body-empty spin API에 구매 영수증이나 multiplier를 추가하지 않는다. 상품 사용 후 기존 read/spin이 상품전용 null 광고 필드를 처리하도록 확장해야 하며, 광고 challenge가 없는 응답을 기존 클라이언트가 광고SDK 입력으로 쓰지 않게 capability를 구분한다.

구매 verify/restore의 원요청 receipt는 불변이다. 과거 성공 receipt 재생이 현재 ACTIVE를 의미하지 않도록 응답에 처리 결과와 현재 권한 조회를 분리한다. 회수 후 같은 key replay를 받아도 신규 승인에는 최신 권한을 사용한다. 계정 전환 후 원계정 인증 없이 다른 계정으로 receipt를 제출해 정산시키지 않는다. 서버 Store 통지 또는 원계정 재로그인 복구 경로를 사용한다.

<a id="source-5cbc96b63e17ce2a-unity-dto-호환-제안"></a>
##### Unity DTO 호환 제안

2026-09-17 Unity_2의 읽기조사 보고: 현 OfflineAdIntent/CombatAdIntent parser는 userId/customData/adUnit 필수, RequireSame에서도 고정하며 combat callbackDeadline도 필수다. 신규 begin은 ProviderAvailable로 차단하고 승인 후 자동 spin은 없다. 이 사실은 Unity 측 보고이며 이 문서 작성자가 C# 전체를 직접 검증한 결과는 아니다.

따라서 새 승인/read 응답의 논리 DTO는 `contractVersion: reward-opportunity-v2`, `approval: null | {cause,approvedAt}`, `adContext: null | {userId,customData,adUnit,expiresAt,callbackDeadline}`, `canSpin`, 기존 고정 base/probabilities/result로 제안한다. 상품전용은 adContext=null; 기존 AD_PENDING을 상품승인한 경우 원 adContext는 보존하고 approval.cause만 최초 승인값으로 확정한다. 회수 뒤 기존 승인은 유지되므로 UI가 현재 owned=false라는 이유로 canSpin을 다시 계산하지 않는다. order 원거래/token은 공개하지 않는다.

기존 AD API v1 응답을 무조건 nullable로 바꾸지 않는다. v2 지원 협상(header 등 구체 방식은 구현 시 클라이언트와 합의)이 확인된 요청만 상품전용 응답을 받으며, 미지원 클라이언트에는 명시적인 계약 미지원 오류로 종료하고 가짜 adUnit을 채우지 않는다. 기존 저장 AD intent의 v1→v2 변환은 원 광고 필드와 identity를 그대로 보존한다. RequireSame은 불변 identity/base/policy와 가변 approval 상태를 분리하여 정상 승인전환을 identity 변경으로 오판하지 않도록 해야 한다. 새로운 공통 Controller 흐름은 authorize→canSpin 확인→기존 spin 자동 호출 1회로 연결하고, ProviderAvailable은 광고 선택 분기에만 적용한다. 이는 서버/Unity 동시 호환 변경 대상이며 현재 동작이 아니다.

<a id="source-5cbc96b63e17ce2a-5-기회기한확률한도"></a>
#### 5 기회·기한·확률·한도

|조건|보급|전투|
|---|---|---|
|고유 기회|account+environment+baseline_ms+baseline_revision; 현 DB unique는 account/baseline/revision 유지|소유 account/run/rewardPreview; preview UNIQUE 유지|
|승인 전|baseline 미소비, 고정 게임정책, ACTIVE 구매권한|VICTORY/COMPLETED/ACTIVE preview, 미소비·미만료, ACTIVE 구매권한|
|AD_PENDING의 callback만 만료|같은 intent 상품승인 허용 제안|preview 유효할 때만 같은 intent 상품승인 허용 제안|
|승인 후 spin|SSV callback 기한을 지급 기한으로 사용하지 않음. 기본수령이 이기면 거절|preview 만료는 계속 적용. 시각 경계는 현재 expires_at > DB now 유지|
|기본/다른 지급 완료|원영수증은 원형 재생. 새 추가지급 없음|같음|
|확률|기존 intent weightsBps 그대로,0038 fixed 우선|기존 고정2/3/4와60/30/10 및 fixed 우선|

신규 기회 출시 확률은60/30/10 기준이지만 승인된 게임정책이 실제 존재해야 한다. 설정 없으면503이며 기본값으로 활성화하지 않는다. provider/app/adUnit/challenge/lifetime/grace는 광고 delivery 정책으로 분리한다. 광고SDK 미지원이어도 게임정책과 권한이 있으면 상품 경로 사용 가능하나, 게임 보상정책 미설정·점검·자격 미충족을 우회하지 않는다.

maxAdsPerDay는 광고 intent 생성 한도이며 시청 완료 수가 아니다. 상품전용 기회는 이를 소모하지 않는다. AD_PENDING→상품은 이미 소모한 카운트를 반환하지 않는다. 별도의 공통 gameRewardOpportunityLimit은 현재 값/도입 여부 미확정으로 임의 적용하지 않는다. 도입 승인 시 양 경로가 한 기회에 한 번만 같은 원자 counter를 소비해야 한다.

<a id="source-5cbc96b63e17ce2a-6-승인지급회수의-원자성"></a>
#### 6 승인·지급·회수의 원자성

1. 기존 요청 receipt와 기회의 확정 결과부터 찾는다. 원영수증이면 재생하고 추첨하지 않는다. 기존 approval이면 최초 원인을 바꾸지 않고 후속 spin/복구로 연결한다.
2. 신규 상품승인은 ACTIVE entitlement revision R/유효 order, 기회 소유·상태·정책/base를 읽는다. 광고 원인 신규승인은 검증된 callback 근거를 읽는다.
3. 하나의 DB batch에서 동일 기회 unique, actor/environment, 기회 미소비, 권한 ACTIVE/R/order 미회수(상품), 정책핀 및 필요한 게임기한을 검사한다. approval INSERT와 intent 승인 상태/요청 receipt를 함께 저장한다. 조건부 변경 0행을 성공으로 취급하지 않으며 guard 실패는 전체 rollback한다.
4. 회수 명령도 같은 권한 행 revision CAS와 account mutation sequence를 사용해 직렬화한다. 회수가 먼저 commit하면 승인 재검사에서 거절. 승인이 먼저 commit하면 그 approval은 이후 회수에도 유지한다. 서로 다른 기회의 정상 사용은 가능하며 권한 revision을 매 사용마다 반드시 증가시킬 필요는 없지만 회수와 충돌 검사는 같은 원자 경계에 있어야 한다.
5. spin은 내구 approval 근거를 검증하고 기존 fixed 결과/단일 정산 명령을 사용한다. 이후의 ACTIVE 여부로 이미 획득한 승인을 취소하지 않는다. 최초 추첨 저장과 지급 실패 경계는 현재 구조 유지. 기본수령이 먼저 기회를 소비하면 지급 거절, 승인만 있다고 기본수령을 무조건 막지 않는다.
6. 전투 settleRunReward의 현재 ADMOB receipt JOIN은 원인별 검증으로 변경: AD_SSV는 실제 공유 receipt 일치, PURCHASE_ENTITLEMENT는 내구 approval의 order/revision 승인 당시 근거 일치. 현재권한 ACTIVE 재검사와 혼동하지 않는다. outcome/owner/run/preview/base/balance/fixed multiplier/미소비/만료 SQL 조건은 유지한다.

구매 승인과 회수는 외부 receipt 검증 순서가 아니라 서버 commit 순서로 판정한다. 동일 취소 주문은 복원·늦은 verify로 되살리지 않는다. 먼저 도착한 취소 사건은 원거래 identity에 tombstone으로 보존해 미등록 주문의 나중 활성화를 차단한다. 취소된 주문 A의 늦은 사건이 새 정상 주문 B로 획득한 권한을 회수하지 않도록 active_order_id 비교를 반드시 포함한다. 별도 신규 거래 재구매는 플랫폼 허용/서버 검증/동일계정 결속이 성립할 때만 허용한다. Store간 공유 범위 미정은 자동 허용으로 해석하지 않는다.

<a id="source-5cbc96b63e17ce2a-7-늦은-ssv"></a>
#### 7 늦은 SSV

상품승인 이전에 실제 발행된 원challenge가 있는 경우에만 적용한다. 상품전용 null challenge에는 SSV를 연결할 수 없다. raw query 서명, 원user binding, 원policy adUnit/reward, 앱·환경·owner, 공급자 거래 유일성을 유지한다.

- 같은 기회의 같은 provider 거래 재전송: 기존 처리 응답, 추가 결과 없음.
- 다른 기회/계정에서 이미 사용한 거래: 거절.
- 상품승인 뒤 도착한 유효 SSV: receipt에 한 번 기록하고 최초 cause=PURCHASE_ENTITLEMENT 유지. 승인시각·배율·지급·잔량·카운트 변경 없음. 이미 CLAIMED여도 원영수증을 다시 만들지 않는다.
- 같은 기회에 서로 다른 두 거래: 현 UNIQUE(consumer,intent)를 유지하고 두 번째는 충돌로 거절한다. 첫 거래를 교체하지 않는다.
- callback의 광고 발생 timestamp는 원challenge 유효구간 안이어야 한다. **상품승인 후 도착 시 callback_deadline을 넘어선 관찰 허용 범위는 상세안에서 미확정**이다. 구현 전 판정이 필요하며 그 전에는 기존 수신 기한을 넓히지 않는다. 단순 receipt 관찰 허용을 새 보상 승인으로 취급하지 않는다. AD_SSV 신규승인에는 현 deadline을 그대로 적용한다.

후발 SSV 기록과 동일기회 멱등 응답은 같은 batch로 처리한다. 이 경로는 현재 AD_PENDING 외 거절을 바꾸는 신규 구현이며 검증 완료로 표기하지 않는다.

<a id="source-5cbc96b63e17ce2a-8-리롤-gap과-검수-전제"></a>
#### 8 리롤 gap과 검수 전제

무료3+슬롯 추가1, 5이벤트×3슬롯의 최대15 추가회는 정책이다. 서버 권위 이벤트 발급·선택권자·종료 상태·후보 revision 저장 및 SSV 추가회 승인부터 구현해야 한다. 추가회 identity에 candidateRevision을 포함해 revision마다 새 추가회를 만드는 것은 금지한다.

같은 슬롯 중복 reroll 및 다른 슬롯 선택 확정은 event revision으로 직렬화한다. 승인·후보교체·잔량·영수증 batch가 실패하면 모두 미소모. 대체 후보 없으면 기존카드/횟수 유지 응답을 고정하며 새 유료승인도 소비하지 않는다. 과거 성공 요청은 현재 이벤트가 닫혔어도 receipt 재생은 가능하지만 상태를 다시 열지 않는다. 협동 선택권·팀공유 소유권 계약 미완성 경로는 비활성이다. 상품권이 동료에게 자동 전파되지 않는다.

<a id="source-5cbc96b63e17ce2a-9-오류-계약-제안"></a>
#### 9 오류 계약 제안

공통 `{accepted:false,errorCode}` 형식. 기존 경로의 기존 오류는 유지하고 아래는 신규 경로 제안이다. 숨김 계정/기회의 존재를 오류문구로 노출하지 않는다.

|HTTP / code|조건|복구|
|---|---|---|
|400 contract_violation / idempotency_key_required|잘못된 strict body/key|입력 수정, 경제 변경 없음|
|401 기존 인증 오류|세션 불가|원계정 로그인|
|403 purchase_not_verified / purchase_binding_mismatch|위조·다른계정/상품/환경|권한 없음, 재결제 유도 없음|
|403 ad_skip_entitlement_required / ad_skip_entitlement_revoked|새 승인에 유효권한 없음/회수|기본 또는 명시 광고 선택|
|403 ad_intent_not_found|타계정/환경/잘못된 기회|임의 다른 기회 생성 금지|
|409 idempotency_conflict / ad_skip_already_owned|다른 body 재사용/중복 checkout|원요청 조회/보유 표시|
|409 ad_skip_state_changed|CAS 재시도 한도 초과|같은 key로 재조회·재시도|
|409 ad_supply_already_consumed / reward_preview_expired|기본수령 또는 게임기회 만료|기존 영수증 표시, 추가 지급 없음|
|409 reroll_revision_conflict / reroll_event_closed|카드/종료 경합|현재event 재조회, 잔량 미소모|
|503 ad_skip_purchase_unconfigured / purchase_verification_unavailable|설정/adapter 부재 또는 통신 실패|확인 중·동일 거래 재확인|
|503 ad_reward_policy_unconfigured / reroll_contract_unavailable|공통 보상정책/리롤 권위 부재|미지원 명시, 기본 경로 유지|

기존 공급자 검증 실패/거래 충돌은 기존 SSV 오류를 유지한다. 예외를 accepted=true로 감추지 않는다. UNKNOWN을 REVOKED로 저장하거나 권한 조회 실패를 자동 광고 실행으로 바꾸지 않는다.

<a id="source-5cbc96b63e17ce2a-10-구현-후-필수-검수표--모두-미실행"></a>
#### 10 구현 후 필수 검수표 — 모두 미실행

|검수|관찰 가능한 합격 조건|
|---|---|
|거래 위조/계정/앱/product/environment mismatch|주문·권한·지급 0, LOCAL/DEV→PRD 차단|
|구매 성공 후 응답 유실·restore·다른 key|동일 order, 계정권한 1개, 보상 지급 없음|
|취소 먼저/verify 늦음/동일거래 restore|REVOKED 유지, 새 승인 0|
|새 주문 B 취득 뒤 옛 주문 A 취소 재전송|B 권한 유지, A 재사용 불가|
|회수와 승인 동시성 양 순서|회수 먼저 거절/승인 먼저 기존 기회 복구; client 클릭시각 무관|
|기본/AD/상품 동시 승인·지급|기회당 approval 1개, 지급 1개, 최초원인 고정|
|보급/전투 지급실패 후 재개|같은 fixed multiplier·정책·base, 지급 중복 없음|
|과거 CLAIMED/0038/비표준 weightsBps|원영수증 동일, backfill·재추첨·60/30/10 덮어쓰기 없음|
|expired AD_PENDING 전환|callback만 만료하면 유효기회에 허용, 소비/preview만료면 거절|
|승인 후 deadline 경계|보급 신규 spin기한 없음, 전투 preview exact boundary 거절|
|SDK/adUnit 미지원, 게임정책 유효|상품 경로 SDK 호출0; 게임정책 부재는503|
|광고 한도 소진 후 상품 / AD→상품|상품전용 카운트 증가0, 기존 AD 카운트 환급0|
|늦은 SSV/중복/교차기회/두 transaction|검증된 같은기회만 관찰, 최초원인·결과 불변, 타기회 재사용 차단|
|리롤 재접속/중복 key/후보 소진|같은 후보·잔량 복원; 후보 소진 시 미소모|
|리롤과 최종선택/회수/협동|서버 종료 CAS 보존, 승인 선후 판정, 타선택권자 사용 차단|
|migration 재구성/실패/구 writer|FK/unique/trigger/원응답 보존, 불일치에서 중단, 구 writer 차단|
|클라이언트 계정 변경/영수증 replay|원계정 결속, 화면 callback 폐기, 과거구매성공≠현재 ACTIVE|

<a id="source-5cbc96b63e17ce2a-11-구현-순서와-미결-차단"></a>
#### 11 구현 순서와 미결 차단

1. 거래·권한·회수 및 schema 재구성 격리 검증 → 새 구매/복원 API 검증.
2. 공통 보상정책/광고조건 및 origin count 분리 → 기존 광고 회귀 검증.
3. 보급/전투 단일 approval과 원자 지급 guard 연결 → §10 경합·복구 검증.
4. 리롤 서버 권위 모델과 무료/AD/상품 공통 명령 구현 → 협동계약 포함 검증 후 활성화.
5. Unity·Admin 계약 반영 및 실제 Store/기기 검수는 별도 승인 범위.

미결: 실제 product/app/provider 등록, checkout 구매시작 허용기한, Store binding/복원·환불 통지 adapter, Store간 권한 범위, 늦은 SSV 수신 deadline 관찰, 공통 게임기회한도 도입 여부/수치, 협동 리롤 권위. 미결을 숫자·가짜 provider·자동 허용으로 채우지 않는다. 초기 상품이 세 혜택을 약속하므로 리롤 미구현 상태에서 세 혜택 전체 제공 완료로 출시 표시하지 않는다.

이번 작성 검수: 상세안 §9, 현재 구매/광고/정산 함수 및0036/0037 CHECK 대조, 신규 API prefix 검색, 문서 readback. 소스·DB·스토어 변경 및 런타임 테스트 없음. 이전112files884tests 결과는 광고 최초배율 고정 체크포인트의 증거이며 이 신규 설계의 구현·검증 결과가 아니다.

<!-- source-body:source-5cbc96b63e17ce2a:end -->


<a id="source-fcd64dcd8469bdc3"></a>
## 영구 광고 스킵 이용권 화면·상태 명세 — 2026-09-17

원출처: `docs/design/AD-SKIP-UI-FLOW-20260917.md`  
원문 SHA-256: `e16d1b63922c055fb81d2f0c857745be5eee5d34fd1271e23dbd41c64641296d`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Purchase/restore/cancel/refund flow and ACTIVE display retained; firstpublic OFF controls new checkout visibility; real Store price absent cannot become a fake configured price.

<!-- source-body:source-fcd64dcd8469bdc3:start -->
<a id="source-fcd64dcd8469bdc3-영구-광고-스킵-이용권-화면상태-명세--2026-09-17"></a>
### 영구 광고 스킵 이용권 화면·상태 명세 — 2026-09-17

단계: 상세 기획. 실행 UI/최종 시안 승인/결제 구현 완료 아님. 상위 문서는 AD-SKIP-PERMANENT-DETAILED-20260917.md v1.1. 서버 및 Unity 계약 문서는 각 담당이 작성 중이다.

<a id="source-fcd64dcd8469bdc3-상점-상품-카드"></a>
#### 상점 상품 카드
기존 상점 승인 미술의 독립 프레임/내부판/아이콘/문구/가격버튼을 재사용한다. 별도 상점 전체 재설계 없이 편의 상품 영역에 영구권1개 배치한다. 상품 아이콘은 광고 시청 생략을 나타내는 독립 심볼로 제작 계획하며 미승인 후보를 최종 원화로 간주하지 않는다.
- 제목: 광고 스킵 이용권
- 구분: 영구 · 1회 구매
- 본문: 운행 보급·작전 완료 룰렛과 증강 추가 리롤을 광고 없이 이용합니다.
- 보조: 보상 확률과 이용 횟수는 기존과 동일합니다.
- 가격: 국내 기획14,900원, 실제 버튼은 스토어 현지화 가격으로 표시. 가격 조회 실패 시 가짜 가격/구매 가능 표시 금지.
- 상세 펼침: 적용3종, 기존 횟수/룰렛 연출 유지, 게임 계정 귀속, 구매 복원 안내.
- 구매 복원: 상점 공통 보조 버튼. 실제 복원 계약이 없는 상태를 작동하는 구매 기능처럼 출시하지 않는다.

<a id="source-fcd64dcd8469bdc3-구매-화면-상태"></a>
#### 구매 화면 상태
|서버권한/구매상태|표시|입력|
|권한 조회 중|보유 여부 확인 중|구매 잠금, 화면 닫기 가능|
|미보유+가격 조회 완료|스토어 실제가격|구매 가능|
|미보유+가격 조회 실패|가격 확인 필요|다시 확인|
|구매창 열림/결제 대기|구매 진행 중|중복 구매 잠금|
|스토어 응답 후 서버 검증 중|구매 확인 중|재결제 금지, 다시 확인/닫기|
|보유|보유 중|구매 잠금, 상세/복원 가능|
|복원 중|구매 복원 중|복원 중복 입력 잠금|
|확인 실패|구매 내역을 확인하지 못했습니다.|다시 확인/닫기|
|취소 확정|구매가 취소되었습니다.|권한 재조회 후 미보유 화면|
|다른 계정 귀속|다른 게임 계정에 연결된 구매입니다.|기존 계정 확인/문의|
취소 확정은 스토어의 명확한 취소 응답일 때만 표시한다. 네트워크 타임아웃을 취소/미보유 확정으로 바꾸지 않는다. 별도 재로그인 강제·상점 강제복귀 없음.

<a id="source-fcd64dcd8469bdc3-광고-접점-상태"></a>
#### 광고 접점 상태
|상태|보급/작전 버튼|증강 슬롯 추가회|
|미보유|기존 광고 룰렛|기존 광고 리롤1/1|
|보유|룰렛 돌리기 + 광고 스킵 적용|추가 리롤1/1 + 광고 스킵 적용|
|권한 확인 불가|이용권 확인 필요 → 다시 확인|이용권 확인 필요 → 다시 확인|
|요청 처리 중|처리 중|처리 중|
|기회 소진|기존 수령/확인 상태|이번 선택 사용 완료|
공통 무료3회는 권한조회 실패로 잠그지 않는다. 룰렛 기본수령도 기존 자격 범위에서 유지한다. 권한 확인이 실패해도 자동으로 광고 SDK 호출 금지. 서버가 미보유를 확인하거나 사용자가 명시적으로 광고 경로를 선택한 때만 기존 광고 흐름을 사용한다.

<a id="source-fcd64dcd8469bdc3-입력과-복원"></a>
#### 입력과 복원
1. 보유자가 룰렛 돌리기1회 입력하면 Controller가 권한 승인과 해당기회 spin을 이어 처리한다. 공통 팝업에서 두 번째 시작 입력을 요구하지 않는다.
2. 요청 유실 시 같은 intent/멱등키를 조회·복원한다. 원인 미확정 재시도에서 새 광고/새구매/새추첨을 만들지 않는다.
3. 결과가 도착하면 기존 미리보기와 ID+kind로 대응해 기본카드를 유지하며6.5초회전/0.6초증가 후 확인 활성화. 같은 결과 재열기는 즉시 확정 상태.
4. 계정 전환 시 View Bind/Unbind, 작업 취소 또는 account-generation 확인으로 이전 응답이 다른 계정 화면에 반영되지 않게 한다. 서버에 이미 저장된 결제/지급은 UI 취소로 취소됐다고 표시하지 않는다.
5. 증강 추가회는 서버가 실제 후보 교체를 확정해야 소진 표시. 대체후보 없음은 기존카드/횟수 유지. 카드를 최종 선택한 뒤 늦은 리롤 결과는 복구 결과에 따라 처리하고 닫힌 화면을 다시 열지 않는다.

<a id="source-fcd64dcd8469bdc3-구현-순서-및-완료-기준"></a>
#### 구현 순서 및 완료 기준
1. 서버·Unity 실제 계약 대조: 신규필드/기존상태/구매공급자 경계와 미구현 리롤 명령을 명시한다. 불명확한 실제 상품ID를 invent하지 않는다.
2. 영구권 권한 기반: 승인된 테스트 영수증으로 계정 귀속·중복·회수/복원 순서 검증. 테스트환경은 PRD권한을 만들 수 없어야 한다.
3. 보급·작전 사용승인: 광고 경로와 하나의 보상기회/지급결과 공유. 구매한 사용자 광고SDK호출0, 응답유실 단일지급 확인.
4. 상점·표시: 기존 프레임 독립구성, 버튼 및 상태별 문구, 가격미조회/보유/검증지연/계정변경 실제 캡처. 결제가능성을 단순목업으로 위장하지 않는다.
5. 리롤 공통명령: 무료/광고/영구권 경로를 같은 후보/잔여횟수 소유자로 연결. 이벤트종료와 원자경합 테스트 및 협동 소유권 해결 뒤 활성화.
6. 화면연결: 보급/클리어 두호스트1280/1600, 정상/긴수량, 기본수령경합, 기존 비보유 광고회귀, 공급자불가+유효권한 경로 검수.
7. 실제 스토어·기기 검증은 별도 단계: 상품등록/복원/환불알림/실가격/앱재시작 검증. 로컬테스트 통과를 출시준비 완료로 부르지 않는다.

단위별 상세 구현 계획은 각 계약 문서 확정 뒤 실제 파일/명령/실패재현/완료조건으로 나눈다. 서버와Unity 경로를 정확히 확인하기 전 API이름·migration번호를 확정하지 않는다.

<!-- source-body:source-fcd64dcd8469bdc3:end -->


<a id="source-2b83cf73b602eca9"></a>
## 영구 광고 스킵 이용권 — Unity 구현 준비 계약

원출처: `docs/design/AD-SKIP-UNITY-CONTRACT-20260917.md`  
원문 SHA-256: `9b93d5c90dbcfabecbd276166ec2c0bcfa23a550b0710bd9fd4b798c4bc2da0d`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Preserve client DTO nullability/type distinctions, separate Store/opportunity journals, CAS/save-before-network, stale account lifecycle guards, Retry-After/timeout/unknown-result recovery and historicalreceipt versus ACTIVE distinction.

<!-- source-body:source-2b83cf73b602eca9:start -->
<a id="source-2b83cf73b602eca9-영구-광고-스킵-이용권--unity-구현-준비-계약"></a>
### 영구 광고 스킵 이용권 — Unity 구현 준비 계약

2026-09-17 / Unity_2 / 문서 단계. 기준: [상세 기획 v1.1](48_AD_REWARD_DETAIL_CONTRACTS.md#source-d06447f1a5c3fef4) §9 우선, [화면·상태 명세](48_AD_REWARD_DETAIL_CONTRACTS.md#source-fcd64dcd8469bdc3). 초기 영구 단독, 국내 기획 기준14,900원. 실제 상품ID·스토어 환경·현지화 가격·플랫폼 조건은 미정이다. 이 문서는 API 출시 계약이나 구매 구현 완료를 뜻하지 않는다.

조사 대상은 최신 `D:/Project/LASTRAIL/unity-project`의 실제 Assets C# 및 Packages/manifest.json. cwd D:/Project/LASTRAIL, Git top-level D:/Project/LASTRAIL/common-dir ../.git 확인. 과거 worktree/스냅샷은 런타임으로 취급하지 않았다. 이번 작업은 읽기와 이 문서·Unity 인계 기록 작성뿐이며 원화/코드/Unity 실행/결제/SDK/START 변경 없음.

<a id="source-2b83cf73b602eca9-1-실제-접점과-gap"></a>
#### 1. 실제 접점과 gap

아래 경로의 공통 루트는 `unity-project/Assets/_LASTRAIL/Scripts/Presentation/MissionShell/`이다. 이름은 실제 파일/메서드이며 신규 설계 이름과 구분한다.

|실제 소스|현재 확인한 동작|영구권에 필요한 변경 경계|
|---|---|---|
|`LobbyShop.cs:LiveShop`, `ShopOfferRuntime.cs:RefreshShopAsync/BuyShopOfferAsync`|GET /v1/shop과 크리스탈→모집권 교환. 크리스탈 상품은 가격 확인/구매 연결 준비 문구. 보급품은 등록 상품 없음|현재 교환 명령을 현금 구매로 재사용하지 않는다. 편의상품 카탈로그·가격·보유 상태를 별도 표시 계약으로 확장해야 한다|
|`ShopOfferContract.cs:ShopSnapshot`|shop-v1, ticketOffers/crystalProducts, STORE_REQUIRED 가격 검증. `Products`는 크리스탈 배열|영구권을 crystalProducts에 끼워 넣거나 quantity로 권한을 표현하지 않는다. 서버 상품 종류/카탈로그 확정 후 DTO 검증 확장|
|`SeasonPassView.cs:Build`|PassPurchase는 구매 준비 중·interactable=false|기존 패스 구매가 운영 IAP 연결의 증거가 아니다. 가짜 구매 성공 버튼 재사용 금지|
|`CrystalWalletRead.cs`|PurchaseConfigured/PurchaseLinked는 크리스탈 정책·읽기 정보|이 값 또는 패스 premium을 광고 스킵 권한으로 해석 금지|
|`OfflineAdContract.cs:IRewardAdPresentation`, `OfflineAdRuntime.cs:ConfigureRewardAds`|광고 Available/ShowAsync 주입 경계|구매 공급자와 구분. 실제 Assets 검색에서 구현체는 테스트 대역만 확인; 운영 광고 브리지 연결은 확인되지 않음|
|`OfflineAdRuntime.cs:RunAdAsync`|신규 begin은 AdProviderAvailable 필수. ad-create→intent→presented→spin journal. 광고 완료 후 GET 확인; 별도 spin 입력|providerless 상품 승인 경로 및 승인 후 기존 spin을 이어주는 orchestration 필요. 기존 begin을 그대로 호출하면 차단/광고 실행 가능|
|`CombatAdController.cs:Execute`, `CombatAdComposition.cs:RunCombatAdAsync`|신규 begin에 ProviderAvailable 요구. 계정/session generation/lifetime 검증. intent/결과 복원과 기존 spin|동일 전투 기회에 상품 사용 승인 추가. `Busy` 동안 공개 SpinAsync를 재진입시키면 차단되므로 내부 공통 단계 또는 명시적 조정자 구조 필요|
|`OfflineAdContract.cs:Parse/RequireSame`|userId/customData/adUnit 필수. 생성/만료/확률/정책·광고 context 고정. CanSpin은 AD_VERIFIED|광고 context 없는 상품 DTO를 현재 parser가 수용하지 못함. 승인원인별 검증과 옛 저장본 호환 필요. 임의 dummy 광고값 금지|
|`CombatAdIntent.cs:Parse/RequireSame`|광고 context와 callbackDeadline 필수. AD_VERIFIED/RESULT_FIXED만 CanSpin. base/excluded/정책/기한 고정; 확률6000/3000/1000 검증|사용승인 원인·게임기한·SSV기한 분리. 보급 과거 고정확률을 전투 parser 규칙으로 강제 통일하지 않음|
|`AccountActionJournal.cs:PendingAccountAction/AccountActionJournal/ReplaceAd`|허용 kind와 payload 검증, 계정당 pending1건, CAS 방식 광고 전이|신규 구매/사용승인 저장은 현재 kind에 자동 호환되지 않음. 별도 거래복구 저장 경계 및 schema/전이 호환 설계 필요. 기존 pending 덮어쓰기 금지|
|`AccountScreenRuntime.cs:SynchronizeSession/Current/RetryPendingAsync`|계정 교체 시 조회 stamp 증가, 보급/상점/광고 상태 초기화, 계정별 pending 복구|권한 캐시/구매 표시·사용승인 작업도 같은 계정 수명 계약에 포함. 서버 거래는 화면 종료로 삭제하지 않음|
|`OfflineSupplyView.cs`, `LobbyCommandDeck.cs:OpenOfflineSupply`, `LobbyOfflineAd.cs`|보급 진입 콜백→기존 상위 처리. 현재 광고 문구·팝업 별도 시작. preview는 Offline.RewardLines에서 전달|보유자 한 번 입력으로 승인→spin까지. 현재 승인 전 preview를 명시적으로 고정하여 결제/승인 후 새 적립분 preview와 혼합 금지|
|`RunVictoryDisplayState.cs`, `RunVictoryView.cs`, `LobbyResultPresentation.cs`, `LobbyCombatAd.cs`, `CombatAdView.cs`|VictoryAd 의도 콜백, 작전 자격 상태, 기존 Controller 실행. intent.BaseLines 전달|표시 데이터에 보유/확인필요/사용중 상태 추가; View에서 권한 판정/네트워크/지급 실행 금지|
|`SharedRoulettePopup.cs`, `SharedRouletteVisual.cs`, `RouletteRewardCountup.cs`|6.5초+0.6초, 기본카드/ID+kind/중립값/확인게이트, Unbind clear, restored receipt 즉시 최종. checkpoint61 최대숫자폭|승인 원인과 무관하게 재사용. popup 생성 직후 preview를 먼저 전달하여 신규 성공이 복원 receipt로 오인되어 연출 생략되지 않게 한다|

**구매 공급자 gap:** Assets/**/*.cs의 UnityEngine.Purchasing/IStoreListener/PurchaseProduct/RestoreTransactions 검색 및 Packages/manifest.json에서 운영 IAP adapter와 구매 패키지 연결을 확인하지 못했다. 이는 조사 범위의 미확인이지 외부 플러그인/바이너리까지 부재를 증명한 것은 아니다. 실제 공급자 선택·초기화·현지화 가격·구매/복원·서버 검증 전달 경계부터 필요하다.

**리롤 gap:** 최신 Assets C#의 `reroll/리롤`, Scripts·Packages의 `reroll/리롤/증강`, augment 파일명 검색에서 실행 리롤 명령/슬롯 상태 모델 접점을 확인하지 못했다. v1.1 §9대로 “기존 공통 리롤 유스케이스 재사용”은 현재 구현 완료 주장이 아니다. 무료/광고/상품이 함께 쓰는 서버 권위 후보·잔여횟수 명령과 Unity 표시 모델을 먼저 연결해야 한다. 역사 기획의 무료3+추가1을 로컬 bool/counter로 급조하지 않는다.

<a id="source-2b83cf73b602eca9-2-책임소유권-계약-구현-제안"></a>
#### 2. 책임·소유권 계약 (구현 제안)

|경계|소유 책임|금지/수명|
|---|---|---|
|스토어 공급자 adapter|카탈로그 실제가격, 구매창, 거래 증거/취소/대기, 복원 사건 전달|권한 ACTIVE 결정 금지. 광고 provider ShowAsync와 분리. 구독 해제와 진행 거래 처리 책임 명시|
|구매 조정자 + 거래 복구 저장소|구매 시작 당시 서버 결속 계정, 동일 거래 서버 검증/재확인, 중복 클릭 잠금|결제 UI 성공만으로 보유 플래그 저장 금지. 계정당 보상 pending1 슬롯에 영구 거래를 덮어쓰지 않음|
|계정 권한 read model/service|서버가 확인한 권한 상태/revision과 계정 소유의 표시 캐시|단일 authoritative owner. 캐시 ACTIVE만으로 사용 승인 불가. 계정 변경 시 이전 표시 폐기|
|보상 사용 조정자|한 입력의 기회 조회→상품 사용 승인→기존 spin/복원, 동일 멱등키 및 예상 상태 관리|보급/전투 View에 처리 복제 금지. 서버의 추첨/단일지급 규칙 소유 금지|
|리롤 공통 명령/상태 모델|서버 확정 event/slot/candidate revision·잔여횟수 반영|View의 임의 추첨/횟수 감소 금지. 협동 소유권 미확정 경로 활성화 금지|
|상점/보급/승리/슬롯 View|명시적 표시 데이터와 Buy/Restore/Retry/Use/Reroll 의도 콜백|Bind 교체 시 자신의 구독만 해제, 중복 바인딩·늦은 UI 갱신 차단. 하위가 상위를 검색하여 실행하지 않음|

AccountScreenRuntime에 기능을 전부 partial로 늘리는 방식만으로 책임 분리라고 주장하지 않는다. 기존 AccountScreenRuntime는 조합 접점으로 두고 구매/권한/기회 조정자는 필요한 경계만 작게 분리한다. 파일/클래스 최종명은 구현 계획 단계에서 확정한다.

권한 UNKNOWN/PENDING/ACTIVE/REVOKED, 구매 진행 상태, 보상 기회 진행 상태는 서로 다른 소유 상태다. 특히 **서버가 확인한 미보유**와 **조회 실패/아직 모름**을 구별해야 한다. 미보유 응답 표현(명시적 absent 여부 또는 상태 enum 확장)은 서버 계약 합의 사항이며 여기서 임의 wire enum을 확정하지 않는다.

<a id="source-2b83cf73b602eca9-3-서버와-합의할-dto-의미"></a>
#### 3. 서버와 합의할 DTO 의미

이 표는 필요한 의미 목록이다. 신규 endpoint/필드명/상품ID를 만들어 확정한 계약이 아니다.

|응답/요청 경계|반드시 보존할 의미|
|---|---|
|상품 정보|플랫폼/환경별 등록 상품 결속, 비소모 영구 종류, 구매 가능 여부. 실제가격은 해당 스토어 현지화 문자열, 서버 숫자를 결제금액으로 신뢰하지 않음|
|권한 조회/구매 검증/복원|게임계정 결속, 확정 미보유와 UNKNOWN 구분, 권한revision, 승인/회수 순서, 동일 거래 식별. 늦은 구매응답으로 회수 거래 재활성화 금지|
|보상 사용 승인|기회 자체 유일 키, AD_SSV 또는 PURCHASE_ENTITLEMENT, 첫 승인근거 및 revision, 승인 확정 여부, 기존 intent/result 참조. 원인별 별도 기회 생성 금지|
|광고 context|AD_SSV 경로에서만 필요한 userId/challenge/adUnit/provider/app/environment. 상품 경로를 가짜SSV/customData로 채우지 않음. 옛 ad 저장본은 버전별 정상 검증 유지|
|기한/가능 동작|상품 확인/SSV callback/게임 preview 기한 구별. 유효 승인된 보급의 기존 spin 무별도기한, 전투 원preview만료 유지. 만료 상태만 보고 클라이언트 새기회 생성 금지|
|기회 preview|기회에 고정된 기본/제외 항목 ID+kind+source+quantity, 정책/확률. 오래된 보급 고정weightsBps·0038고정배율·receipt 우선. 승인 후 최신 정책으로 갈아끼우지 않음|
|리롤|account/run/event/slot/추가회차/currentRevision, 서버 확정 후보/잔량/이벤트종료, 대체후보 없음의 명시 결과. 응답 유실 시 동일 결과 조회|

현재 parser는 광고 context와 일부 상태를 고정하므로 서버 변경과 Unity DTO 변경은 같은 버전 전이 계획이 필요하다. 기존 RequireSame의 회귀 차단을 통째로 완화하지 않고, 어떤 필드가 최초 승인 때 채워질 수 있는지/어떤 고정 필드는 절대 변하지 않는지 명시한다. 기존 CLAIMED는 원영수증 우선이다.

<a id="source-2b83cf73b602eca9-4-한-입력으로-보급전투-실행"></a>
#### 4. 한 입력으로 보급·전투 실행

1. 서버 조회로 ACTIVE가 확인된 사용자가 호스트의 `룰렛 돌리기`를 한 번 누른다. 해당 화면 Controller가 현재 account/generation/기회 ID를 캡처하고 중복 입력을 잠근다. UI에 `광고 스킵 적용`, 처리 중에는 `결과 확인 중`을 표시한다.
2. 기존 preview를 해당 기회와 함께 보존하고 공용 popup에 waiting 표시를 전달한다. 서버 기회 preview가 아직 없으면 명확히 중립으로 시작하며 최종÷배수로 기본값을 만들지 않는다. receipt 도착 후 재조회된 다음 적립분으로 기본값을 바꾸지 않는다.
3. 같은 기회를 조회한다. 이미 CLAIMED면 원receipt를 즉시 복원하고 재승인/추첨/연출을 반복하지 않는다. 기본수령 소진/전투preview만료면 종료 상태로 전환한다.
4. 미승인 기회만 서버 상품 사용 승인. AD_PENDING이 광고 callback만 만료됐어도 v1.1 조건을 서버가 판단한다. 광고 provider Available/ShowAsync를 호출하지 않는다. 승인 저장/키 보존을 확인한 뒤 기존 spin 실행 단계를 같은 사용자 동작 안에서 이어간다. Busy를 일시 해제해 공개 버튼 메서드를 우회 호출하는 방식은 피한다.
5. spin 응답 유실은 기존 intent와 멱등키로 조회/재시도한다. 자동으로 광고창/구매창을 열거나 새 intent/키로 다시 뽑지 않는다. 이미 내구성 있게 승인된 사용은 이후 권한회수와 별개로 서버 선후 판정에 따라 복구한다.
6. 새 확정 receipt만 기존 공용 6.5초 회전→0.6초 수량 증가→확인 활성. 정상 복원 receipt는 즉시 최종. confirmed 기본값은 기존 ID+kind 및 source 일치 검증, 불명확 값은 —→최종, checkpoint61 고정 최대폭을 유지한다.

현재 호스트 진입은 팝업을 열고 별도 시작 버튼을 누르는 구조이므로, 보유자 host action에서 새 조정자까지 연결해야 한다. popup 입력 이벤트를 프로그램으로 연타하거나 View가 승인을 직접 수행하는 방식으로 한 입력을 흉내 내지 않는다. 무료/기본수령은 기존 Controller 명령을 그대로 바인딩한다.

<a id="source-2b83cf73b602eca9-5-ui-상태오류-문구-계약"></a>
#### 5. UI 상태·오류 문구 계약

|상태|표시/입력|
|---|---|
|권한 조회 중/불명확|`보유 여부 확인 중` 또는 `이용권 확인 필요` + 다시 확인. 구매/상품사용 잠금. 적법한 기본수령·무료리롤은 유지|
|확정 미보유, 가격 준비|상점의 스토어 실제가격 구매 버튼. 광고 접점은 기존 광고 동작, 작은 안내 진입만. 매 광고마다 강제 판매창 금지|
|가격/카탈로그 확인 실패|`가격 확인 필요` + 다시 확인. 기획14,900을 결제 가능한 실제가격으로 대신 표시하지 않음|
|구매 UI 진행/스토어 대기|`구매 진행 중`, 중복 구매 잠금. timeout을 취소로 확정하지 않음|
|거래 수신/검증 유실|`구매 확인 중` 또는 `구매 내역을 확인하지 못했습니다.` + 재확인/닫기. 새 결제 유도 금지|
|명시적 취소|`구매가 취소되었습니다.`; 권한 재조회 후 표시. UNKNOWN을 미보유로 확정하지 않음|
|ACTIVE|상점 `보유 중`; 보급/전투 `룰렛 돌리기` + `광고 스킵 적용`. 추가 슬롯 `추가 리롤 1/1`. 상점 중복구매 잠금|
|상품사용/응답 유실|`결과 확인 중`/`다시 확인`. 광고 확인 중 문구나 자동 광고 재생 없음|
|확정 회수/다른 계정 결속|`이용권 상태가 변경되었습니다.` 또는 `다른 게임 계정에 연결된 구매입니다.`. 재조회/기존 계정 안내·문의. 자동 계정이전 없음|
|복원 진행|`구매 복원 중`; 복원 중복잠금. 로그인 복구와 스토어 거래복원 구분|
|리롤 대체 없음/추가 소진|`교체 가능한 후보가 없습니다.`는 카드·잔량 유지. 소진은 `이번 선택 사용 완료`|

위 문구는 semantic 결과 매핑 제안이다. 실제 서버 errorCode 목록은 서버 계약과 대조 후 확정하며, HTTP timeout/5xx만으로 실패 확정 또는 pending 삭제를 하지 않는다. 권한 실패 시 광고 자동전환 금지; 서버 확인 미보유 또는 명시적 사용자 광고 선택만 기존 광고 경로를 실행한다. 전투 증강 UI에서 구매 모달을 강제로 열지 않는다.

<a id="source-2b83cf73b602eca9-6-계정-전환닫기구독복구"></a>
#### 6. 계정 전환·닫기·구독·복구

- 구매 시작 전에 서버가 결속한 원계정을 거래기록에 유지한다. 계정B 화면에 계정A 구매 완료를 적용하지 않는다. UI 작업 취소는 스토어 결제/서버 지급 취소를 뜻하지 않으며, A로 복귀했을 때 동일 거래를 복구한다.
- 모든 await 뒤 current account/generation + 소유 Controller lifetime을 확인한다. 화면별 request serial로 재조회 응답 순서도 검증한다. 진행 API가 실제 취소를 지원하면 소유자가 cancel/dispose, 지원하지 않으면 응답 갱신 차단을 보장한다.
- Bind는 이전 자체 콜백을 해제하고 한 번 연결, Unbind/disable/destroy는 소유한 구독·표시 연출만 해제한다. 다른 소비자의 리스너 RemoveAll 금지. 기존 SharedRoulettePopup.CancelAndClear 계약 보존.
- 계정 권한 서비스는 계정 수명, 상점 화면 구독은 화면 수명, 미확정 거래복구는 서버 결속 거래 수명으로 분리한다. 화면 닫힘 때문에 미확정 영수증을 지우거나 완료 응답을 현재 계정으로 재결속하지 않는다.
- 기존 계정당 보상 pending1건 제한과 구매 거래 복구는 서로 충돌하지 않도록 저장 계약을 설계한다. 저장 실패 시 스토어창/승인 요청을 성급히 진행하지 않고 복구 가능한 문맥을 제공한다. 원영수증·token·customData 비밀값은 로그에 남기지 않는다.

<a id="source-2b83cf73b602eca9-7-구현-후-검수-계획-이번에는-미실행"></a>
#### 7. 구현 후 검수 계획 (이번에는 미실행)

|검증 단위|관찰 가능한 완료 기준|
|---|---|
|권한/카탈로그 경계|미보유≠UNKNOWN, 실제가격 없음 구매0, ACTIVE 서버검증 이전 활성화0, 플랫폼/환경/계정 불일치 거부, 원거래 중복·늦은 회수/복원 순서 검증|
|구매 수명|입력1회 공급자1회, 검증유실 후 동일거래 조회, 앱 재시작·재설치/동일계정 복구, B전환 후A완료가B에 반영0. 실기기 스토어 검증은 별도 단계|
|상품 보급/전투|권한ACTIVE+광고공급자없음 성공 경로, ShowAsync0, host1입력→승인→spin. 새기회/기존AD_PENDING/기존SSV승인/CLAIMED/기본소진/전투만료 조합|
|기회·회수 경합|기본/광고/상품 경합 한 기회·한 결과, 승인 먼저/회수 먼저 구분, 유실spin 동일키, 후발SSV가 재지급/재추첨하지 않음. 서버 테스트와 함께 대조|
|과거 계약|기존 ad journal 저장본과 역사 CLAIMED/고정weights/0038 결과 보존. 신규확률로 재생성0. 현재 OfflineAdRuntimeTests/CombatAdControllerTests 회귀 확장|
|리롤|슬롯별 무료3+추가1, 동일event 재접속 잔량보존, 새event 초기화, 후보 없음 미소모, select/reroll revision 경합, 권한/광고 추가회 중복0, 협동 소유권 미정이면 비활성|
|공용 UI|보급/전투×1280/1600×정상/긴 수량 actual host. preview→receipt 사이 새preview 도착·missing kind·excluded·중복ID를 검증. 6.5+.6/복원 즉시최종/매프레임TMP line1 overflow0/폰트38·카드폭고정|
|MVC·입력 수명|재바인딩 한 입력 한 처리, Unbind 후0, 계정 변경·닫기·늦은응답·무료/기본입력 경합. 재확인으로 새결제·새광고 자동시작0|
|독립 원화/표시|상품 프레임·아이콘·문구·현지가격·보유상태 독립 교체/숨김, 승인 원화와 실제 크기 대조. placeholder를 완성 구매화면으로 판정하지 않음|

선행 구현 순서: 서버 DTO/승인 전이·저장 호환 합의 → 공급자/권한/거래복구 경계 → 보급·전투 조정자 → 기존 View 표시 바인딩 → 서버·Unity 공통 리롤 명령 및 협동 소유권 → 실제 스토어/기기 단계. 이번 문서 승인만으로 상품등록/결제/SDK 설치·실행/배포 권한이 생기지 않는다.

<a id="source-2b83cf73b602eca9-8-합의-대기와-보고"></a>
#### 8. 합의 대기와 보고

실제 상품ID·스토어 구매/복원/회수 조건·현지화 가격·플랫폼 간 권한 범위·협동 리롤 소유권·운영 공통 기회 한도는 미정. 숫자/endpoint/migration을 임의 지정하지 않는다. 서버 전용 계약은 별도 설계 문서로 수신했으며 아래 절에서 대조했다. 본 문서는 해당 파일을 편집하지 않는다.

서버 담당에게 parser 광고 context 필수/RequireSame 고정, provider gate, 자동spin 부재, journal 단일슬롯/허용kind, 리롤 미확인 사실을 전달했다. 서버 계약 수신 시 DTO 이름과 호환 전이를 대조하되 이 문서의 제안을 구현 완료로 승격하지 않는다. 기존 영구권 기획과 checkpoint61 표시 안정성은 각각 기획 기준과 회귀 기준으로만 사용한다.

<a id="source-2b83cf73b602eca9-서버-계약-수신-대조--같은-날"></a>
##### 서버 계약 수신 대조 — 같은 날

[서버 계약](48_AD_REWARD_DETAIL_CONTRACTS.md#source-5cbc96b63e17ce2a) §2~4/7~11을 대조했다. 수신본 SHA256 `48B13DC3140763A6DFFED9C5AF36B5F32B80AB7908C20A1941BD2BFD6D39D576`. 아래는 양측 설계 정렬이며 API 구현 완료가 아니다.

- 확정 미보유는 서버안 `owned=false, entitlement=null`, UNKNOWN은 조회 실패, PENDING은 별도 구매 상태로 표현한다. §2에서 열어 둔 의미를 이 제안으로 정렬한다. 과거 verify/restore 성공 receipt와 현재 권한 조회를 분리하여 회수 후 재생이 ACTIVE로 오인되지 않게 한다.
- 서버안 `/v1/ad-skip` 조회 및 checkout/verify/restore/uses/authorize는 **신규 제안 경로**다. 승인 응답의 기존 intent/read/spin 참조를 검증한 뒤 기존 빈 body spin을 호출한다. 반환된 임의 URL로 bearer token을 전송하지 않고, 허용된 게임 API origin 및 해당 account/기회에 묶인 경로인지 확인한다.
- 논리 DTO 제안 `contractVersion=reward-opportunity-v2`, `approval:null|{cause,approvedAt}`, `adContext:null|{userId,customData,adUnit,expiresAt,callbackDeadline}`, `canSpin` 및 고정 base/probabilities/result에 맞춰 adapter를 계획한다. v2 요청 협상 수단은 미정이다. v1 필수 필드를 일괄 nullable로 완화하지 않는다. 기존 광고→상품은 원 adContext 보존, 상품전용은 null; 승인 후 회수됐어도 내구 승인과 서버 canSpin을 현재 owned로 덮어쓰지 않는다.
- OFFLINE intent 생략 승인 시 서버가 현재 baseline을 고정하므로, 승인 전 화면 preview와 서버 승인 기회의 기본값 일치 여부를 확인해야 한다. 클라이언트의 화면 진입 시 snapshot만으로 새baseline의 기본값을 단정하지 않는다. 서버 고정 preview 제공/검증 가능 여부를 구현 전 확정하고, 불명확할 때는 중립값을 유지한다.
- 리롤 candidateRevision은 CAS 전제이며 추가 사용의 유일 키가 아니다. 슬롯/event 추가회와 원자적으로 후보·잔량을 확정하는 서버 공통 명령을 기다린다. 세 혜택을 약속한 상품을 리롤 미구현 상태에서 전체 제공 완료로 표시하지 않는다.
- 상품 승인 후 늦은SSV 수신 deadline 관찰 범위는 서버 문서도 미결로 남긴다. Unity가 시계를 보고 허용하거나 광고 재실행으로 보충하지 않는다. 원계정 전환 뒤 거래 verify는 다른 계정 인증으로 대신 제출하지 않고 서버 통지 또는 원계정 로그인 복구 계약을 따른다.
- 기존 AccountActionJournal의 단일 pending 및 구매 거래복구 저장 확장, v1 저장본→v2 정규화/RequireSame 분리, 실제 Store adapter/협상 방식은 구현 전 세부 설계 항목으로 유지한다. 서버 파일은 수정하지 않았다.



<!-- source-body:source-2b83cf73b602eca9:end -->


<a id="source-3eb8098bdcb26fce"></a>
## 광고 스킵 2차 Unity 실제 연결 경계

원출처: `docs/design/AD-SKIP-UNITY-PHASE2-GAPS-20260917.md`  
원문 SHA-256: `d43998b9bc3a36eb3dd3213a72a49eb32bcb14a3b13455aa6633a7795a8d58b6`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Gap ledger remains historical evidence, not release completion; no demo product benefit; reroll wiring must be re-evaluated under per-card extra1.

<!-- source-body:source-3eb8098bdcb26fce:start -->
<a id="source-3eb8098bdcb26fce-광고-스킵-2차-unity-실제-연결-경계"></a>
### 광고 스킵 2차 Unity 실제 연결 경계

2026-09-17 / Unity_2. 아래 소유자 표는 이번 독립 단위 구현 전 조사이다. 조사 대상은 D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts, Tests, Resources, UI 및 Packages/manifest.json이다. 다른 worktree의 후보 구현을 최신 runtime으로 취급하지 않았다.

후속 승인 범위: 서버 내부 솔로 도메인 계약에 맞춘 독립 snapshot/command/controller/View/Presenter와 주입 port/journal 인터페이스를 추가했다. 실제 전투·API·영속 저장·효과·Store adapter는 연결하지 않았다. 내부 서버 최종 샘플은 QA/AdSkipRerollServer-20260917/internal-domain-fixtures.json이며 복사본 SHA237969294d59b7adbf2a93f4412efc75da49660deacd518b36b7473cca7b449f를 고정한다. 이 추가가 아래 운영 연결 gap을 해소했다는 뜻은 아니다. 검증 결과는 QA/AdSkipUnityPhase2-20260917/REPORT.md에 기록한다.

<a id="source-3eb8098bdcb26fce-확인한-현재-소유자"></a>
#### 확인한 현재 소유자

|책임|실제 현재 위치|승인 2차와의 차이|
|---|---|---|
|전투 local state·변경 저장|Scripts/Battle/CombatSceneDriver.cs의 Change, CombatCheckpointStore|변경 후보를 복원하고 저장한 뒤 적용하는 경계는 존재. 서버 augment grant/후보/슬롯 잔량의 소유자가 아님|
|구형 분기와 증강 수치|CombatRailJourney.cs / CombatRailJourneySimulation.cs|웨이브 경계 Selecting, 레버 preview/confirm, routeId와 ADD_BPS 증강 동시확정. 맵 이벤트 완료·카드 터치 전용 최신 계약과 다름|
|구형 카탈로그 허용|Ce03RailPreparation.cs|rail-augment-catalog-v1, squad/train/shield +500 세 효과 검증. 40종 플레이스타일 후보 풀로 확대해석 불가|
|구형 전투 View|CombatSecondFinalView.cs / ApprovedCombatView|BranchLever.Commit → ConfirmRailRoute. 슬롯별 카드 리롤 View가 아님|
|입장 패키지 참조|RunStartResponseDto/Adapter, CombatContentLoader, run snapshot/export|startAugmentPackageRef는 시작 패키지. 동일 event 후보/잔여 리롤 복구 계약이 아님|
|현재 영구권 조회/보급·전투 사용|Presentation/MissionShell/AdSkipAccountService, AdSkipUseController|1차 OFFLINE/COMBAT만 소비. REROLL은 server feature의 원문 표시와 1차 미지원 문구만 존재|
|승인된 새 증강 event/선택/리롤 owner|생산 Scripts에서 미발견|AugmentAcquisition/grantId/candidateRevision/freeRemaining/reroll 명령 검색 및 관련 소스 조사. 구형 증강 코드 자체가 없다는 뜻은 아님|

현재 런타임을 레거시 선택기의 후보 수만3으로 바꿔 증강2차라고 연결하면 선로 변경과 카드선택이 결합되고 횟수/서버 원자성도 보장되지 않는다. 이 경로를 임의 수정하는 방안은 채택하지 않는다. 서버와 합의한 새 snapshot/controller/명령 경계를 작게 구현하고, 실제 이벤트 발급·효과 적용이 없는 구간은 local fixture 검증으로 분리하는 방안을 채택한다.

<a id="source-3eb8098bdcb26fce-승인-ui와-리소스"></a>
#### 승인 UI와 리소스

`AUGMENT-SELECTION-FINAL-20260915.md`의 승인 원본 `QA/Design/AugmentSelection-20260915/augment-selection-three-reroll-v02.png`를 직접 표시했다. 제목, 혼합3카드, 유형/원화/이름/효과, 등급 프레임 표현, 카드별 무료·추가 잔량, 하단 터치/초기화 안내를 구분한다. 배경 전투의 추가 문구/불빛까지 승인한 자료는 아니다.

최신 Resources/UI 파일명에서 augment/reroll 독립 자산을 찾지 못했고, 승인3카드를 만드는 생산 View/연결 코드도 찾지 못했다. 이름 검색만으로 모든 기존 자산이 부재한다고 단정하지 않는다. 실제 매핑·등급명·독립 원화가 확인되기 전에는 전용자산 미확인으로 관리한다. reference-only 합성본을 실행UI로 붙이거나 잘라 상태문구까지 재사용하지 않는다. 기존 공용 금속 프레임과 TMP를 이용한 계약 검증은 가능하나 이를 승인 시안의 미술 완성으로 보고하지 않는다.

<a id="source-3eb8098bdcb26fce-서버와-확정할-항목"></a>
#### 서버와 확정할 항목

- 실제 run/map/node 완료에서 grant를 발급하는 소유자, 같은 단계 대체노드의 동일 grant,5회 정책 및 효과적격성/카탈로그 버전.
- snapshot의 run/grant/account/선택담당/event종료/revision, 서로 다른3후보와 슬롯별 무료/추가 잔량. 후보label/type/grade/art 식별자 원천.
- 단일 리롤 명령의 무료/광고/상품 승인 분기와 idempotency key, stale revision, 대체후보 없음, 유실응답 복구, 선택/리롤/종료 경합.
- 광고 provider 연결 없을 때의 명시 상태와 늦은 콜백, 상품권한 회수 전후 승인 순서. client purchase flag/SDK완료로 후보를 변경하지 않는다.
- 협동 담당권/공유잔량·시간예산은 아직 상세안 후보이다. 미합의 경로에 단독계정 권한을 임의 확장하지 않는다.
- 생산 후보와 실제 효과 반영이 없다면 서버 fixture/계약 단위의 성공과 runtime production readiness를 분리하고 REROLL UNSUPPORTED를 강제로 바꾸지 않는다.

<a id="source-3eb8098bdcb26fce-스토어-읽기-점검"></a>
#### 스토어 읽기 점검

- Packages/manifest.json에는 com.unity.purchasing이 없다. Assets/_LASTRAIL 전체 C#/JSON에서 IStore/구매 adapter, checkout-intents, purchases/verify/restore 및 RestorePurchases 소비자 명령은 발견되지 않았다.
- AdSkipShopView 구매·복원 Button은 disabled이고 실행 callback이 없다. 보유조회는 AdSkipAccountService의 GET이며 Store 복원이 아니다.
- AccountActionJournal의 pending은 계정당 게임행동 1건을 복구한다. 원게임계정/checkoutIntent/provider/app/product/receipt 검증 진행을 독립적으로 관리하는 Store 구매 journal은 확인되지 않았다. reward-approval에 구매영수증을 억지로 넣지 않는다.
- 추후 Store 경계에는 서버 checkout이 정한 원계정 결속, native transaction/receipt의 안전한 내구 보관, 서버 verify/restore 재시도, 계정전환 중 원계정 정산, 검증후 확정 처리 순서가 필요하다. 현재 구현되었다고 주장하지 않는다.
- 플랫폼은 Plan이 사용자에게 확인 중이다. 임의 SDK 버전·SKU·현지가격·Store 계정 범위를 택하지 않았고 package 설치/실제Store 호출은 하지 않았다.

구현 체크리스트는 `docs/superpowers/plans/2026-09-17-ad-skip-unity-phase2.md`를 따른다. 이 문서는 조사·경계 설계이며 새 리롤 기능 완료 보고가 아니다.

<!-- source-body:source-3eb8098bdcb26fce:end -->


<a id="source-8ed6f4d6a63ee84a"></a>
## 전투 클리어 광고 API 구현 인계

원출처: `docs/design/COMBAT-AD-API-IMPLEMENTED-20260916.md`  
원문 SHA-256: `39362caa55220fe86268d2ef7a32e39675bfa05db8eba9f8547da6ebec113def`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Implemented historical combat-ad endpoints and exact empty body/query/key rules; receipt/result authority, shared provider transaction uniqueness,503 basic survives; route06/07.

<!-- source-body:source-8ed6f4d6a63ee84a:start -->
<a id="source-8ed6f4d6a63ee84a-전투-클리어-광고-api-구현-인계"></a>
### 전투 클리어 광고 API 구현 인계

2026-09-16. 단독 계정의 기존 run/preview 대상으로 로컬 구현 및 격리 검증 완료. 운영 적용이나 Unity 연결 완료를 뜻하지 않는다.

<a id="source-8ed6f4d6a63ee84a-실제-경로"></a>
#### 실제 경로

기준 경로: `/v1/runs/:runId/reward-preview/:rewardPreviewId/ad-intents`

| 요청 | 동작 |
|---|---|
| POST 기준 경로 | 미리보기에 고정된 광고 시도 생성/복구 |
| GET 기준 경로/:intentId | 소유 계정·환경의 상태/원결과 복구 |
| POST 기준 경로/:intentId/spin | 검증된 광고의 서버 추첨 및 원자 지급 |

인증 필수. POST는 1~128자 출력 가능한 ASCII Idempotency-Key 필수. body와 query는 비운다. 클라이언트 금액·배율·광고 완료 여부를 받지 않는다. 기존 `/v1/admob/ssv`가 서명 검증 후 서버 challenge에 따라 보급/전투를 구분한다.

<a id="source-8ed6f4d6a63ee84a-상태와-표시"></a>
#### 상태와 표시

AD_PENDING → AD_VERIFIED → RESULT_FIXED → CLAIMED. RESULT_FIXED는 지급 실패 재시도에서도 배율을 보존한다. 기본 수령이 먼저 완료되면 REWARD_CONSUMED, 만료는 EXPIRED, 원본 미리보기 변경은 PREVIEW_CHANGED, 다른 비활성/비승리 상태는 UNAVAILABLE이다. canSpin은 검증/고정 상태에서만 참이다. canClaimBasic은 현재 미리보기 ACTIVE·미만료 여부다.

생성/조회에는 intent/run/preview ID, 서버 시각과 만료 정보, opaque userId/customData, adUnit, policyVersion, 6000/3000/1000 확률, eligibleBaseRewardLines/excludedBaseRewardLines, canClaimBasic 및 지급 후 result를 포함한다. UI는 공급자 검증을 대신 판정하지 않는다. 스핀 실패나 응답 유실 시 같은 intent를 조회/재시도한다. 새로운 시도 ID로 재추첨하지 않는다.

기본 줄은 생성 시점 원본 스냅샷이다. 최초 클리어 소진/변환 처리가 있는 실제 최종 지급은 result.rewardLines가 권위 있는 값이다. spin 결과는 multiplier/policyVersion/rewardClaimId/serverClaimedAt/rewardLines를 포함하며 재요청은 원래 저장된 결과를 반환한다. 계약 초안의 상세 UI 가공 필드나 자동 재시도 권장 간격은 아직 별도 구현하지 않았다.

<a id="source-8ed6f4d6a63ee84a-저장운영-경계"></a>
#### 저장·운영 경계

0037 migration은 검증 거래 공유 영수증, 전투 정책·시도·생성/스핀 요청 영수증을 추가한다. 기존 검증 거래를 backfill하고 역사적 충돌은 실패 처리한다. 기본/광고 수령이 동일 미리보기를 원자적으로 소비한다. 광고 지급은 combat_ad_reward_claim 멱등 namespace를 사용한다.

정책의 만료·유예·일일한도·UTC 일 경계·광고 단위·앱·환경·보상 binding은 명시 설정이 필요하다. 운영 seed는 없으며 미설정이면 광고 시작 503, 기본 보상 수령은 가능하다. 미리보기당 시도 하나로 복원하므로 만료 후 새 광고 시도 정책은 미구현이다. 일일 한도는 생성된 시도를 센다. 운영 재시도/한도 정책 확정 전에 활성화하지 않는다.

현재 배율 대상은 RUN_CLEAR의 크레딧/강화 재료/전술 데이터/요원 경험치다. 신규 목적지 source는 아직 생성되지 않으므로 임의 추가하지 않았다. 최초 클리어·희귀·아이템은 자동 배율 대상이 아니다.

<a id="source-8ed6f4d6a63ee84a-검증과-다음-작업"></a>
#### 검증과 다음 작업

QA/CombatAdApi-20260916: 최종 112 files / 877 tests PASS. 서명 변조, 미검증 스핀, 다른 계정·환경, 만료, 기본 수령 경쟁, 동시 스핀, 지급 실패 후 배율 보존, 보급/전투 공급자 거래 재사용, migration 역사 충돌, 정책 은퇴 후 복구, 내부 요청 키 충돌을 격리 SQL.js 및 합성 서명으로 검증했다. 실제 AdMob/D1 운영 검증이 아니다.

남은 일: 비전투 최종 화면 검수 우선순위를 유지한 Unity 결과/룰렛 MVC 연결, 앱 종료 후 조회 복구 및 취소/실패 화면, 운영 정책과 실제 공급자 설정, 협동 참가자별 정산, Admin 조회, 목적지 보상 생성. 기존 보급 검증의 batch 내부 만료 재검사는 별도 잔여사항이다.

<!-- source-body:source-8ed6f4d6a63ee84a:end -->


<a id="source-9b925fbe1a114e96"></a>
## 전투 클리어 광고 룰렛 및 확률 확정

원출처: `docs/design/COMBAT-CLEAR-AD-ROULETTE-20260916.md`  
원문 SHA-256: `5d19199f62cc9002f0b55d865d7afc606f153a05706bbb8162b2b86f866ed4ed`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Combat clear pending-basic+roulette settlement and server eligibility retained; totals2/3/4 60/30/10 supersede older pending probability draft; no basic-first late difference.

<!-- source-body:source-9b925fbe1a114e96:start -->
<a id="source-9b925fbe1a114e96-전투-클리어-광고-룰렛-및-확률-확정"></a>
### 전투 클리어 광고 룰렛 및 확률 확정

2026-09-16 사용자 명시 승인: ‘전투 클리어 보상에도 붙이자 확률은 추천대로 확정시켜’. 기존 운행보급 광고 룰렛에 더해 전투 클리어 결과에도 선택 광고 룰렛을 도입한다. 확률은 기존 보급과 신규 클리어 룰렛 모두 아래 표로 확정한다. 운영 배포·실제 광고 연결·지급 완료를 뜻하지 않는다.

<a id="source-9b925fbe1a114e96-1-확정-확률과-적용-보상"></a>
#### 1. 확정 확률과 적용 보상

|총배율|확률|서버 정수 가중치 예시|
|---|---:|---:|
|2배|60%|6000|
|3배|30%|3000|
|4배|10%|1000|

총배율은 기본분을 포함한다. 3배는 기본1배+추가3배가 아니다. 한 번 뽑은 배율을 대상 보상 전체에 동일 적용한다. 재화마다 따로 추첨하지 않는다. 증강 후보 동일 확률 정책과 별개의 확률이다.

클리어 결과의 공통 보상과 목적지 성공 보상을 합산한 크레딧, 기계 부품, 전술 데이터, 요원 경험치에 적용한다. 지급 근거는 서버가 검증해 저장한 원래 보상 미리보기다. 최초 클리어 별도 보상, 티켓·특수 코어·스킨·랭킹 보상, 작전 물자에는 이번 승인만으로 배율을 확대하지 않는다. 대상 밖 보상은 원래 수량을 유지한다.

클리어 판정이 확인된 결과에 붙인다. 열차 파괴·포기에는 자동 확대하지 않는다. 새 기획의 ‘목표 미달 생환’은 클리어와 구분되므로 이번 클리어 룰렛 자격에 포함하지 않는 상세안이다. 별도 자격 확대가 결정되면 명시적으로 정책을 개정한다.

<a id="source-9b925fbe1a114e96-2-사용자-흐름과-기본-수령"></a>
#### 2. 사용자 흐름과 기본 수령

클리어 → 서버 보상 산정 → 결과 미리보기 → ‘기본 받기’ 또는 ‘광고 보고 총 2~4배’ 선택 → 광고 시청 → 서버 검증 확인 → 룰렛 돌리기 → 결과 확정·지급 → 결과 연출 순서다.

광고 없는 기본 수령은 유지한다. 광고 취소·로딩 실패·미지원으로 기본 보상이 사라지지 않는다. 확인 지연은 확인 중/재확인으로 표시하며 광고 재시청을 강요하지 않는다. 광고 정책 미설정/미가용 때 기본 경로는 이용 가능해야 한다.

클리어 결과 한 건당 최종 보상 수령은 한 번이다. 기본 수령을 확정하면 그 결과의 광고 증액은 닫는다. 광고 경로는 기본 보상을 선수령하고 차액을 나중에 더하는 방식이 아니라, 서버가 확정한 전체 배율 보상을 원자적으로 지급한다. 기본 수령과 광고 spin이 경합하면 한 경로만 성공하고 다른 경로는 기존 확정 상태를 복원한다. 늦은 광고 콜백으로 두 번째 지급이나 재추첨을 하지 않는다.

<a id="source-9b925fbe1a114e96-3-서버unity-연결-상세안"></a>
#### 3. 서버·Unity 연결 상세안

기존 전투 finish → rewardPreview → claim의 소유권과 검증을 유지한다. 광고를 선택했을 때 runId, rewardPreviewId, accountId, 기존 보상/경제 버전, 배율 확률 버전, 광고 시도·검증 거래를 연결한다. 보상 금액은 광고 완료 시점의 새 정책이나 새 진행도로 재계산하지 않는다.

광고 검증부는 기존 보급 SSV 계약을 재사용할 수 있으나 보급 적립 cutoff와 전투 클리어 미리보기는 별도 소유 대상이다. 전투 결과를 offline-rewards 엔드포인트에 억지로 넣지 않는다. 기존 run 광고 테이블/계약의 재사용 가능성은 구현 전에 검토한다. 한 provider 거래를 보급·클리어·증강 리롤에 중복 사용하지 않는다.

SDK 완료 신호만으로 지급하지 않는다. 서버 검증 뒤 사용자 spin 입력에서 서버가 확률표에 따라 결과를 한 번 고정하고 지갑·원장·수령 상태를 함께 처리한다. 기본 claim과 같은 수령 소유권을 사용한다. 응답 유실·재접속·애니메이션 생략·재요청은 원래 결과/영수증 복원이며 새 추첨이 아니다.

View는 기본 수량·확률·당첨 배율·총수량·확인/오류 상태를 표시하고 Controller가 기존 수령/광고 서비스를 호출한다. View가 난수·금액·검증 여부를 결정하지 않는다. 룰렛 프레임·섹터·포인터·배율 글자·확률 안내·재화·수량은 독립 조합한다. 3개 섹터의 면적으로 동일 확률을 암시하지 않도록 확률을 명시한다.

협동은 계정별 보상 미리보기를 기준으로 각자 광고 참여를 선택하는 연결안이다. 한 명의 광고를 양쪽 계정에 사용하거나 동료의 결과 수령을 광고 종료까지 잠그지 않는다. 배율 대상은 각자 원래 받을 자격이 있는 보상이며 협동 보상 자격 자체를 변경하지 않는다.

<a id="source-9b925fbe1a114e96-4-경제-영향"></a>
#### 4. 경제 영향

광고 완료 후 정상 룰렛 수령의 평균 총배율은 2×0.6+3×0.3+4×0.1=2.5배다. 추가 발행 기대량은 기본 대상 보상의 1.5배다. 대상 클리어 중 광고 룰렛으로 수령하는 비율을 q라고 하면 평균 발행 배율은 1+1.5q이다. q=0/0.5/1일 때1/1.75/2.5배다. 미수령·검증 보류는 이 단순 모델과 별도로 추적한다.

기존 목적지 검증표를 그대로 사용하면 아래 기대값이 된다. 개별 지급은 정수2/3/4배이고 소수는 장기 기대값이다.

|목적지|기본 대상 보상: 크레딧/부품/데이터/경험치|광고 정상 수령 시 기대값|
|---|---|---|
|보급 집결지|240/4/2/450|600/10/5/1125|
|폐차 공업단지|60/16/8/450|150/40/20/1125|
|통신 기록소|60/4/2/1800|150/10/5/4500|

세 목적지 각1회 성공의 광고 전 합계360/24/12/2700은 전부 광고를 적용하면 기대900/60/30/6750이다. 이전 ‘3판 총량 동일’ 비교는 광고 전 기준에서만 유효하다. 실제 광고 참여와 오프라인 광고 수급을 함께 넣어 성장 속도를 재검토한다. 평균2.5배를 이유로 기본 보상을 자동으로 40%로 삭감하지 않는다. 광고를 안 보면 진행이 막히는 경제로 만들지 않는다. 기본 목적지 수량은 아직 검증 초안이다.

<a id="source-9b925fbe1a114e96-5-확정미결정검증-경계"></a>
#### 5. 확정·미결정·검증 경계

후속 [서버·Unity 연결 상세안](48_AD_REWARD_DETAIL_CONTRACTS.md#source-f100d05ec701191e)에 기존 claim,0009/0012/0036 제약,보급 전용SSV 조회,화면 상태와 신규 API 제안을 기록했다. 협동 run당 미리보기 유일성·교차보상 광고 거래 중복 방지는 추가 구현 검토 대상이다.

확정: 전투 클리어에도 선택 광고 룰렛, 보급/클리어 공통2배60%·3배30%·4배10%, 대상 일반 재화 동일 총배율, 광고 검증 후 spin, 기본 수령 유지.

미결정: 일일 광고 횟수 제한, 자격 세부 범위, 시도 만료/지연 콜백 처리 시간, 실패 후 재시도 횟수, 광고 공급 환경 등록, 실제 장치 검증 및 최종 경제 수량. 최종 결과 한 번이라는 중복 지급 금지와 일일 광고 한도를 혼동하지 않는다. 확률 승인만으로 나머지 운영값을 임의 설정하거나 활성화하지 않는다.

구현 검증 항목: 확률 경계/버전 고정, 같은 배율 전체 대상 적용, 대상 외 보상 보존, 기본 claim/spin 동시성, 검증 없는 spin 거부, 광고 취소·지연·중복 provider 거래, 수령 후 재접속, 보급/리롤과 거래 재사용 차단, 협동 독립 수령. 현재는 기획 문서 반영 및 기대값 산술 확인 단계이며 새 전투 룰렛 코드·실제 광고·지급은 미구현/미검증이다.

<!-- source-body:source-9b925fbe1a114e96:end -->


<a id="source-f100d05ec701191e"></a>
## 전투 결과 광고 룰렛 — 서버·화면 연결 상세안

원출처: `docs/design/COMBAT-ROULETTE-SERVER-UI-CONTRACT-20260916.md`  
원문 SHA-256: `e231e6dd3c5f4000558cd9e956d3f41ebd9eba83fe0271bc1a58b64e1b7b8b79`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Pin outcome/weights/server plan and full roulette replay receipt; preserve claim/skip arbitration, 409/410 paths, telemetry and offline/combat separation.

<!-- source-body:source-f100d05ec701191e:start -->
<a id="source-f100d05ec701191e-전투-결과-광고-룰렛--서버화면-연결-상세안"></a>
### 전투 결과 광고 룰렛 — 서버·화면 연결 상세안

2026-09-16 후속 상세기획. 전투 클리어 광고 룰렛과 확률2배60%/3배30%/4배10%는 사용자 확정이다. 본 문서의 API 이름·상태 이름·저장 구조는 구현 계약 제안이다. 현재 엔드포인트나 완료 기능으로 표시하지 않는다.

<a id="source-f100d05ec701191e-1-기존-코드에서-확인한-경계"></a>
#### 1. 기존 코드에서 확인한 경계

서버 경로는 Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server이다.

- apps/game-api/src/index.ts의 기존 claim은 runId/rewardPreviewId만 엄격하게 받는다. 클라이언트 배율 필드를 추가해서 보내면 현재 계약 위반이다. 이미 수령된 응답은 DUPLICATE_REPLAY이며 rewardLines가 비어 있는 경로가 있다. 룰렛 결과 복원에는 배율·실제 지급 명세·영수증을 다시 읽는 계약이 필요하다.
- 0012_run_reward_previews.sql은 UNIQUE(run_id)이다. 현재 테이블 하나에 같은 run의 두 계정 미리보기를 넣을 수 있다고 가정하지 않는다. 협동이 계정별 run을 쓰는지 공용 run을 쓰는지 확인한 뒤 권위 있는 보상 소유 키를 정해야 한다.
- 0009_ads.sql은 provider_transaction_id가 NOT NULL이고 UNIQUE(run_id,provider)다. 광고 검증 전 시도나 같은 공용 run의 계정별 시도·재시도를 그 테이블에 억지로 넣지 않는다.
- offline-ad-routes.ts의 /v1/admob/ssv는 현재 challenge를 offline_ad_intents에서 찾는다. 서명 검증 함수를 재사용할 수 있으나 이 경로가 이미 전투 callback을 처리한다고 주장하지 않는다.
- 0036_offline_ad_intents.sql의 provider 거래 중복 제약은 그 테이블 안의 제약이다. 다른 보상 종류의 테이블까지 중복을 막는 공통 거래 소유권은 별도 연결해야 한다.
- Unity OfflineSupplyView는 Bind/Unbind 콜백을 제공한다. RewardStoryScreenPresenter는 보상 표시 책임이 있다. 기존 파일 이름에 맞추느라 도메인·광고·수령 실행 책임을 Presenter에 추가하지 않는다. ApprovedCombatResultView는 전투 명중 효과 관련 코드이며 작전 종료 보상 화면으로 오인하지 않는다.

<a id="source-f100d05ec701191e-2-화면-흐름"></a>
#### 2. 화면 흐름

|화면 상태|표시|가능한 입력|
|---|---|---|
|결과 산정 중|보상 정보 확인 중|결과 재조회, 안전한 화면 이동|
|수령 가능|기본 보상 목록, 배율 대상/제외 구분|기본 받기 / 광고 보고 총2~4배|
|광고 준비·재생|광고 로딩/공급자 화면|공급자 종료·취소, 중복 시작 방지|
|광고 검증 대기|광고 확인 중|재확인 / 기본 보상으로 받기 / 나중에 확인|
|검증 완료|룰렛과 2배60%·3배30%·4배10% 안내|룰렛 돌리기 / 나중에 확인|
|spin 요청 확인 중|결과 확인 중|같은 요청 재확인, 신규 추첨 금지|
|지급 완료|배율, 대상 기본분과 총수량, 제외 보상 원수량|연출 생략 / 확인|
|광고 사용 불가|기본 보상, 필요한 짧은 사유|기본 받기|

광고는 결과 화면의 선택 행동이다. 전투 종료 직후 자동 재생하지 않는다. 광고 준비가 실패해도 기본 수령은 유지한다. 검증 대기에서 기본 수령을 선택하면 ‘기본 수령 시 이번 보상의 룰렛은 종료됩니다’라고 안내한 뒤 기존 기본 수령 동작으로 확정한다. 늦게 검증돼도 추가 지급하지 않는다.

확인 지연 동안 화면을 떠날 수 있으며 서버 상태가 남는다. 다음 진입에서 동일 보상·시도를 복원한다. 화면 닫기를 포기·광고 실패·작전 종료 명령으로 변환하지 않는다. 수령 완료가 불명확할 때 기본/광고를 새 키로 다시 보내지 않는다.

승인된 결과 화면과 룰렛 미술을 재사용하되 전투 결과에서 보급 누적시간 등 무관한 문구를 가져오지 않는다. 기존 승인본은 보존하고 추가 CTA·확률·상태를 독립 요소로 제작해 실제 크기에서 검수한다. 신규 전체 시안 승인까지 얻은 것으로 확대하지 않는다.

<a id="source-f100d05ec701191e-3-권위-상태와-단일-지급"></a>
#### 3. 권위 상태와 단일 지급

보상 정산 상태와 광고 시도 상태는 별개로 둔다. 보상은 수령 가능 → 지급 완료 또는 기존 만료/무효 경로를 따른다. 시도는 검증 대기 → 검증 완료 → 룰렛 수령 완료이며 기본 수령·만료·거절 때문에 닫힐 수 있다. 도메인 enum은 구현 시 기존 계약과 맞춘다.

기본 claim과 광고 spin은 같은 보상 미리보기의 수령 가능 상태를 조건으로 하는 하나의 원자적 정산 경계를 사용한다. 먼저 확정된 한 경로만 지갑·원장·최종 영수증을 만든다. 다른 요청은 그 결과를 복원한다. UI 비활성화나 계정 내 요청 큐만으로 중복 방지를 대신하지 않는다.

spin은 검증된 시도와 수령 가능한 미리보기일 때만 동작한다. 서버가 결과를 고정한 뒤 재시도에서 바뀌지 않게 저장하고, 지급 실패·응답 유실에서도 같은 추첨 결과를 복구한다. 저장된 결과 없이 클라이언트에 당첨을 먼저 보여 주지 않는다. 난수 결과 고정과 지급의 트랜잭션 설계는 실패 경계 테스트로 검증해야 한다.

광고가 만료됐다는 이유로 원래 기본 보상까지 무효화하지 않는다. 원래 보상의 만료는 기존 정산 계약에 따른다. 광고를 시작해 놓고 미리보기가 먼저 만료되지 않도록 유효기간 관계를 서버가 확인해야 한다. 안전한 검증·수령 여유가 없는 미리보기는 광고만 막고 기본 수령을 제공한다. 구체 만료/유예 시간은 아직 미정이며 임의 상수로 운영 활성화하지 않는다.

<a id="source-f100d05ec701191e-4-api-제안"></a>
#### 4. API 제안

아래 신규 경로는 아직 미구현이다. 기존 finish/preview/basic claim 계약은 유지하고 기본 claim 내부의 단일 지급 처리를 광고 spin과 공유한다.

|역할|제안 경로/계약|
|---|---|
|보상 조회|기존 GET /v1/runs/:runId/reward-preview/:rewardPreviewId 응답에 선택 가능한 수령 방식과 서버 광고 가용성 추가|
|광고 시도 생성|POST /v1/runs/:runId/reward-preview/:rewardPreviewId/ad-intents|
|시도 복원|GET /v1/runs/:runId/reward-preview/:rewardPreviewId/ad-intents/:intentId|
|룰렛 확정|POST /v1/runs/:runId/reward-preview/:rewardPreviewId/ad-intents/:intentId/spin|
|기본 수령|기존 claim, 클라이언트 배율·보상금액 필드 추가 금지|
|지급 복원|보상/시도 조회가 확정 배율·실제 지급 줄·영수증 ID를 반환하도록 확장|

신규 생성/spin은 인증·환경·계정·run/preview/intent 소유 관계와 Idempotency-Key를 검증한다. 클라이언트는 금액·확률·광고 완료 boolean을 전달하지 않는다. 재요청은 동일 키를 유지하고 다른 미리보기로 키를 재사용하면 명시적 충돌이다. 네트워크 오류는 새 시도 생성 근거가 아니다.

광고 생성 응답에는 intent ID, 서버 시각, 만료 정보, 검증용 opaque binding, 등록 광고 단위, 고정 확률 버전과 표시 확률, 대상 기본 보상, 기본 수령 가능 여부를 포함한다. 확인 응답에는 논리 상태, canSpin, 재시도 가능 여부, 지급됐다면 원래 결과를 포함한다. spin 응답은 실제 committed 상태, 총배율, 대상 기본/최종 줄, 제외 줄, 영수증 ID와 서버 확정 시각을 포함한다. 실패를 빈 보상 성공으로 표현하지 않는다.

<a id="source-f100d05ec701191e-5-광고-거래저장-설계"></a>
#### 5. 광고 거래·저장 설계

전투 광고 시도 저장소는 보상 미리보기 참조, 소유자, 환경, 생성 키, 확률/광고 정책 스냅샷, 대상/제외 기본 줄, 보상 버전, challenge, 검증 거래, 결과·영수증 참조를 갖는다. 시도와 최종 보상 권리를 분리해 새 시도가 새 지급 권리가 되지 않게 한다. 같은 미리보기에 활성 시도 하나만 허용하는 제안을 사용한다. 취소 후 재도전 한도는 별도 정책이다.

provider 거래는 보급·전투·증강 리롤 등 모든 지급 소비자에서 단일 소유가 되도록 공유 영수증 경계를 둔다. 서명 검증 후 challenge로 서버가 기록한 보상 종류/소유자로 전달한다. 클라이언트가 보낸 종류 문자열로 우회시키지 않는다. 기존 보급 거래도 포함하는 전환 검증 없이 각 테이블 UNIQUE만 추가하고 완료로 보지 않는다.

과거0009/0036의 행과 영수증은 보존한다. 새 migration 또는 호환 계층의 구체 선택은 서버 구현 설계에서 결정하며 기록 삭제로 제약 문제를 해결하지 않는다. 광고 정책 철회 후에도 이미 지급된 결과는 복원되어야 한다. 미지급 시도의 처리와 만료는 고정 정책으로 명시한다.

협동은 광고 선택·검증·배율·수령을 계정별로 관리하고 동료를 대기시키지 않는다. 단, 공용 run에 UNIQUE(run_id) 미리보기 하나를 쓰는 현재 제약과 충돌할 수 있으므로 실제 협동 run 구조 확인 전 구현 완료를 선언하지 않는다.

<a id="source-f100d05ec701191e-6-mvc수명-관리"></a>
#### 6. MVC·수명 관리

결과 View는 기본 수령, 광고 시작, 재확인, spin, 닫기 의도만 콜백으로 내보낸다. Controller가 보상 서비스와 광고 SDK 어댑터를 조정하고, 서버 응답으로 소유 모델을 갱신한다. 금액/배율 확정과 지급은 서버 책임이다. 보급의 표시/검증 어댑터 중 의미가 같은 부분만 재사용하고 보급 적립과 전투 정산 도메인은 섞지 않는다.

Bind 재호출은 자기 구독의 중복을 막고 Unbind/화면 종료는 자기 구독·재조회 작업만 해제한다. 이전 화면에 비동기 응답이 도착하면 세션 세대/취소 토큰으로 표시 갱신을 차단한다. 앱 재개는 원래 계정의 저장된 요청 키/ID로 복원하며 로그아웃 후 다른 계정에 결과를 적용하지 않는다. 서버에서 끝난 지급을 화면 종료로 취소하지 않는다.

<a id="source-f100d05ec701191e-7-구현-전달-단위와-검증"></a>
#### 7. 구현 전달 단위와 검증

후속 [협동 소유권 조사·구현 범위](46_COOP_DETAIL_CONTRACTS.md#source-f02428cfe7a062db)에서 runs의 단일 계정 소유,미리보기뿐 아니라 reward_transactions의 run별 유일성,0009 Admin 소비자를 확인했다. 공용 전투 시도와 계정별 경제 run 연결안을 채택한 구현 기준으로 정리했으며 협동 결과 정산은 아직 미구현이다.

1. 협동 run/preview 소유 구조와 기존0009 소비자를 확인하고 단일 정산 소유 키 확정.
2. 서버의 기본 claim 원자 경계를 재사용 가능한 보상 서비스로 분리하고 기존 수령 결과 보존 검증.
3. 광고 시도/공유 거래 소유권/SSV 전달/고정 확률/복구 계약 구현. 격리된 로컬 표본만 생성.
4. Unity 결과 CTA와 공용 룰렛 표현을 위 계약에 연결. 기존 비전투 승인 시안 작업과 충돌하지 않게 담당 순서 조정.
5. 기본-vs-spin/중복spin/검증 지연 후 기본수령/응답 유실/다른계정/교차보상 거래 재사용/만료/부분실패/협동 독립 수령을 검증.
6. 6000/3000/1000 경계값·합계 및 모든 대상 줄의 동일 배율을 확인. 유한 표본의 실제 당첨 횟수가 정확히60/30/10이어야 한다고 테스트하지 않는다.
7. 실제 모바일에서 광고 취소·복귀·일시중단·결과 연출과 독립 리소스 표시 검수. 실제 광고 공급자 연결은 운영 설정 확정 후 별도 검증.

아직 남은 운영 결정: 일일 횟수, 재시도 한도, 유효기간/지연 유예, 지원 환경과 광고 단위. 이번 산출물은 코드 확인을 바탕으로 한 상세 기획이며 API/DB/Unity 수정이나 실제 광고/지급은 수행하지 않았다.

<!-- source-body:source-f100d05ec701191e:end -->


<a id="source-85e341febad1a3f0"></a>
## 전투 룰렛 Unity 연결 — 2026-09-17

원출처: `docs/design/COMBAT-ROULETTE-UNITY-CONNECTION-20260917.md`  
원문 SHA-256: `8e7ccb7a6deb6c65e0e7f7752ebda389ce4b7540303dee673abf699ceb2f3e6e`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Unity connection ownership and pending action/journal recovery are historical implementation contracts;6.5+.6s finalpolish/cardkeep overrides earlier hiding.

<!-- source-body:source-85e341febad1a3f0:start -->
<a id="source-85e341febad1a3f0-전투-룰렛-unity-연결--2026-09-17"></a>
### 전투 룰렛 Unity 연결 — 2026-09-17

사용자가 승인한 서버 계약을 활성 D Unity 프로젝트에 연결했다. 실제 광고 공급자·운영 설정은 미연결이며 테스트 응답을 이용한 Editor 검증이다. 새 최종 미술 시안 승인이나 전체 리소스 감사 완료를 의미하지 않는다.

<a id="source-85e341febad1a3f0-구현"></a>
#### 구현

- CombatAdIntent: run/preview/intent/정책/시간/binding/배율 확률/보상 줄 검증. 날짜를 문자열로 보존하는 기존 GameApiJson 사용. 서버 결과만 당첨·지급 근거로 사용한다.
- CombatAdController: 요청 생성·조회·스핀과 세션/수명 검증, 고정 요청 키, 광고 표시 전 저장, 재접속 조회. SDK 종료는 서버 재조회의 계기이며 지급 권한이 아니다. 스핀 응답 유실 후 같은 키를 재사용한다. 계정 전환 뒤 응답은 폐기한다.
- 기존 AccountActionJournal: combat-ad 단계와 원자적 교체 계약 확장. 기존 보급 광고와 교차 교체를 금지한다. 저장 실패 시 요청을 전송하지 않는다. 테스트의 저장소 포트는 메모리 대역이며 실기기 암호화 검증이 아니다.
- AccountScreenRuntime: 별도 Controller를 조합하고 기존 기본 claim을 그대로 재사용한다. 공통 RetryPendingAsync도 저장된 run/preview의 전투 광고 조회로 연결한다. 만료 시 서버 canClaimBasic을 반영하되 보관된 기본 claim의 결과 복구 권한은 유지한다.
- RunVictoryView/Lobby: 승리 화면에 룰렛 진입 버튼, 분리된 CombatAdView, Bind/Unbind 콜백 연결. 기본 수령 전 룰렛 종료 확인을 거친다. 지급 결과와 만료 권한은 확인 버튼 이후에도 메모리에 유지하여 오래된 미리보기로 되돌아가지 않는다.

<a id="source-85e341febad1a3f0-화면독립-리소스"></a>
#### 화면·독립 리소스

기존 승인 보급 룰렛의 EntryPopup/roulette-room, roulette-ring, roulette-pointer를 각각 RawImage로 재사용한다. 외곽 프레임/판은 IndependentMetalSurface, 배율·확률·보상·안내는 TMP, 입력은 독립 Button이다. 보상 목록은 ScrollRect/RectMask2D로 잘라 긴 내역을 스크롤한다. 서버 거래 ID는 기존 보급 rewardTransactionId와 전투 rewardClaimId를 모두 지원하도록 표시 애니메이션만 확장했다.

전투 그래픽별 텍스처 연결과 raycast 비활성, 실제 화면 버튼 입력, 재바인딩 한 번 처리 및 Unbind 후 호출 차단을 검사했다. 현재 캡처는 720×1280; 다른 실기기 해상도·터치·회전과 장문 보상 스크롤 육안 검수는 후속이다. 합성 화면을 런타임 UI로 사용하지 않는다. 기존 원화·역사 시안은 수정하지 않았다.

<a id="source-85e341febad1a3f0-검증-근거"></a>
#### 검증 근거

QA/CombatAdUnity-20260917/final2.xml 및 final2.log: 59/59 PASS. 전투 Controller/실제 journal 계약, 기존 보급 광고, 계정 화면, 결과 표시·승리 화면·편성 기록 검사를 포함한다. 첫 미구현 컴파일 실패와 날짜 판독 실패, 중간 회귀 로그도 보존했다. 독립 코드 리뷰에서 찾은 공통 재시도 누락, 만료 권한 되돌림, 저장소 예외 전파를 수정하고 재검토했다.

실제 Unity 캡처: 같은 QA 폴더의 victory-entry.png, roulette-verified.png, roulette-expired.png, roulette-claimed.png. 테스트 금액과 계정이며 운영 보상이 아니다. 기존 기록 샘플에 없는 전투 시간·편성은 임의로 생성하지 않아 확인 필요로 표시된다.

<a id="source-85e341febad1a3f0-남은-범위"></a>
#### 남은 범위

1. 실제 AdMob SDK의 IRewardAdPresentation 구현과 주입, 광고 단위·운영 만료/한도 정책 확정. SDK가 없으면 이용 불가로 표시하고 기본 수령을 유지한다.
2. 전투 서버 배포 및 실기기 네트워크 단절·앱 종료·광고 취소·SSV 지연 통합 검증. 이번 작업은 서버 운영 배포·실제 광고·실제 경제 쓰기를 하지 않았다.
3. 협동 참가자별 보상, Admin 조회, 신규 목적지 보상 생성은 별도 작업이다. 기존 비전투 최종 화면/미술 검수 우선순위와 미확인 범위를 유지한다.

<a id="source-85e341febad1a3f0-2026-09-17-룰렛-지급-완료-화면-후속-수정"></a>
#### 2026-09-17 룰렛 지급 완료 화면 후속 수정
사용자 요청: 확인 버튼 하나 및 배경 추가. CLAIMED/REWARD_CONSUMED 화면은 확인 하나만 표시, 다른 진행 상태의 필수 조작은 유지. 기존 roulette-room 원본을 화면 전체 독립 RawImage로 배치하고 우측 조명·열차 모니터 구도 적용, 별도 어두운 오버레이/기존 프레임/룰렛/텍스트 유지. 원본 PNG 불변. CombatAdControllerTests/RunVictoryNavigationTests 15/15 PASS(single-confirm.xml), 활성 버튼1개·전체 배경 독립 연결 검사 및 실제720x1280 캡처 육안 확인. QA/CombatAdUnity-20260917/roulette-single-confirm.png. 기존59검사 증거와 이전 캡처는 보존; 이번 후속 검증은15건. 실제광고/운영쓰기 없음.

<a id="source-85e341febad1a3f0-2026-09-17-공통-룰렛-결과-팝업-unity-구현"></a>
#### 2026-09-17 공통 룰렛 결과 팝업 Unity 구현
승인 Concept05 기준 SharedRoulettePopup/SharedRouletteVisual로 보급·작전 화면 표시를 공통화했다. 기존 Controller/저널/공급자/지급 경계는 유지한다. 인셋 팝업, 배경/프레임/분할 룰렛/포인터/배수/재화 카드/버튼 독립 연결. 결과 영수증의 수량을 재계산하지 않고 표시하며 확인 하나만 남긴다. 4종 이상 카드는 가로 스크롤로 모두 접근 가능. 결과 없는 기본수령 소진 상태는 미리보기 금액을 결과처럼 표시하지 않고 안내와 확인만 유지한다. 콜백 재바인딩 및 해제 후 내부 확률 버튼 입력까지 차단. 회전은 확정 영수증 ID 기준 시각 연출이며 재조회마다 재생하지 않는다.
새 독립 RGBA 재화 아이콘 atlas는 실제 alpha 확인. 원본/승인 시안 불변. QA/SharedRoulette-20260917/RESOURCE-AUDIT.md에 자산 역할 기록. 실제 화면은 unity-result-4x.png, unity-supply-result.png, unity-combat-result.png. 합성 시안을 실행 UI로 쓰지 않았다.
검증: 최초 신규 레이아웃 조건 red 실패 확인, 컴파일 namespace 충돌 수정 후 30PASS, 시각 보정 및 리뷰 경계 수정 후 최종 final2.xml 40/40PASS. 보급/작전/계약/승리화면 회귀, 확정수량/4번째카드/배수subset/소진상태/해제입력 확인. 독립 읽기리뷰 후 콜백 보완. verification.json에 소스SHA. 실제 SDK/운영 서버/경제 쓰기 없음. Unity 내부 TestResults 기본 사본이 C LocalLow에도 기록됨을 로그에서 확인; 지정 QA 출력은 D이다.
잔여: 승인 이미지 대비 프레임 형태·광원 번짐·배경 질감은 완전 일치로 승인하지 않는다. 새 재화 원화는 승인본 방향의 구현 자산이며 별도 최종 미술 승인 아님. 보급 intent 계약은 허용 배수만 제공하고 숫자 확률을 제공하지 않아 상세창에 확률 미제공 안내를 표시한다; 공통 UI에 작전 확률을 임의 대입하지 않는다. 보급 확률 상세 데이터 연결, 실제 광고 SDK/실기기 검증은 후속이다. 작전 최초/특수 배율 제외 보상은 기존 작전 결과 화면에 남긴다.

<!-- source-body:source-85e341febad1a3f0:end -->


<a id="source-61bc2eaacb1bc07f"></a>
## Server 광고 보급 설계 및 실행 계획

원출처: `docs/design/SERVER-AD-ROULETTE-20260914.md`  
원문 SHA-256: `7f8d920f8ae2e78c8f091743bdb5365eab77668f5c5ec24f044a7e56fc0a2b95`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Server AD intent/SSV/plan/ledger design and integrity retained; later2/3/4 60/30/10 and permanent entitlement origin apply; old proposed probability not current.

<!-- source-body:source-61bc2eaacb1bc07f:start -->
<a id="source-61bc2eaacb1bc07f-server-광고-보급-설계-및-실행-계획"></a>
### Server 광고 보급 설계 및 실행 계획

> 2026-09-16 후속 승인: 보급/전투 클리어 룰렛 확률은 2배60%·3배30%·4배10%로 확정. 아래 확률 미정/추천 상태 기록은 이 결정으로 대체한다. [클리어 룰렛 확장](48_AD_REWARD_DETAIL_CONTRACTS.md#source-9b925fbe1a114e96)은 신규 연결 대상이며 기존 offline wire 구현 완료를 전투 룰렛 구현 완료로 해석하지 않는다. 다른 운영 설정 미결정은 유지한다.

2026-09-14. 현재 구현을 읽고 작성한 제안/작업 경계이며 운영 활성화 승인이 아니다. 기반: WorkspaceRepo/docs/design/offline-supply-ad-roulette-20260911.md. 기존 사용자 구현 승인 및 coordinator 지시에 따라 확정된 로컬 작업은 계속한다.

<a id="source-61bc2eaacb1bc07f-재사용과-선택"></a>
#### 재사용과 선택

기존 offline preview는 조회마다 계산하고 claim은 현재 시각까지 재계산한다. 광고가 시작된 시점의 보급을 고정하지 못하므로 그대로 배율만 더하는 방식은 제외한다. 기존0009 ad_reward_transactions는 run 귀속 제약이 있어 offline snapshot용으로 억지 재사용하지 않는다. 새 보급 snapshot/intent를 두고 기존 offline 계산, account_mutations sequence fence, economy_transactions/currency_ledger/reward_transactions와 원자 batch 및 idempotency를 재사용하는 방식을 채택한다. 기존 기본 claim이 광고 대기 중 같은 baseline을 소비하는 경우도 새 흐름과 조정해야 한다.

서명 검증은 입금과 분리한다. 신뢰된 고정 Google key source에서 받은 공개키로 callback 원문을 검증한 뒤 transactionId, opaque user binding, server challenge, ad unit을 intent와 대조한다. callback 자체에는 검증 가능한 app/environment 필드가 없으므로 서버가 등록한 ad unit→app/placement/environment 연결로 판정한다. 사용자 전달 key URL/app 표시는 신뢰하지 않는다. SDK 완료는 상태 재조회 계기만 된다.

<a id="source-61bc2eaacb1bc07f-현재-로컬-wire-등록격리-검증-완료-unity-표본-전달"></a>
#### 현재 로컬 wire (등록·격리 검증 완료, Unity 표본 전달)

- POST /v1/offline-rewards/ad-intents: authenticated, Idempotency-Key, no client reward/probability input; pinned snapshot and opaque challenge/userId, configured ad unit, expiration returned.
- GET /v1/offline-rewards/ad-intents/:id: owner/environment-scoped restoration; status/canSpin/creation-expiry/ad options/policyVersion, 지급된 경우 original result. 검증만 완료된 상태의 result는 null이며 spin에서 결과를 확정한다.
- Create/GET의 displayMultipliers는 intent에 고정된 정책에서 양수 확률을 가진 배율만 오름차순으로 반환한다(2/3/4의 비어 있지 않은 부분집합). 확률값/동일확률을 의미하지 않는다. 확정 spin 결과 형식은 유지한다. Unity 요청 후30관련테스트 및 DisplayMultipliers01 새표본 export1/readback PASS.
- GET /v1/admob/ssv: provider callback, no player Bearer; only cryptographic verification then atomic intent transition. Duplicate provider transaction across accounts/intents prohibited; exact replay is acknowledged without payout/reroll.
- POST /v1/offline-rewards/ad-intents/:intentId/spin: authenticated, Idempotency-Key, no body/query. 검증된 intent의 배율 확정과 전체 배율 보상을 원자 처리한다. 별도 client claim/선수령 차액 경로는 없다. 응답 유실은 같은 키 또는 GET으로 복구한다.
- Missing approved policy returns 503/ad_reward_policy_unconfigured; original basic offline claim stays usable. No default equal probabilities.

최신 사용자 흐름: 광고 시청완료→서버 검증 확인중/재확인→룰렛 버튼 활성화→서버 결과 확정/보상. States pending→verified→result_fixed→claimed. SSV가 결과를 미리 추첨하지 않고, 명시적인 spin 동작이 검증된 intent의 결과를 원자적으로 고정한다. 재시도/복구는 기존 결과를 반환한다. 광고 경로 중 기본 보급 선수령/나중 차액지급을 새로 도입하지 않는다. 별도 기존 기본 claim이 같은 snapshot을 소비했다면 spin/claim은 fence로 거부하며 두 경로가 같은 baseline을 지급하지 못한다. Future accrued supply is separate.

<a id="source-61bc2eaacb1bc07f-decisions-still-required"></a>
#### Decisions still required

확정: 배율 적용은 크레딧/강화재료/경험치 모두. 기존 offline 실제ID는 cur_credits/cur_machine_parts/cur_tactical_data/cur_operator_exp 네 가지다. Probabilities for 2/3/4, version, daily/per-supply limits, minimum/maximum eligibility, expiry/late callback, retries after cancel/failure, unsupported platform/region/account policy remain unset. 60/30/10은 coordinator 추천안일 뿐 승인된 운영값이 아니다.

앞선 기본 선수령+지연 차액지급 권장안은 사용자 정정에 따라 폐기한다. SSV 지연은 확인중 상태 재조회와 멱등 처리로 대응하며 재시청을 강요하지 않는다. 유효기간/재시도 제한이 미설정이면 광고 활성화를 거부한다.

Actual AdMob integration requires environment-bound app/ad-unit/placement registration, provider reward item/amount agreement, configured reachable callback, approved policy, and real device end-to-end verification. No credentials in chat/logs. Local signing fixtures prove cryptographic boundary behavior only, not Google account configuration or production delivery.

<a id="source-61bc2eaacb1bc07f-execution-checklist"></a>
#### Execution checklist

- [x] Correct gallery11 to existing PRD INTERNAL client-config/authorized; existing admission5 tests PASS (2026-09-14, isolated SQLite).
- [x] admob-ssv.ts raw-query/DER/P-256/SHA256/trustedkey/binding23 synthetic tests.
- [x] admob-keys.ts fixed-origin bounded fetch/cache24h/immediate unknown-key refresh9 mock tests. Reviewer cooldown/retry mismatch reproduced then fixed.
- [x] offline-ad-policy.ts complete explicit settings/no defaults/four-ID safe integer multiplier17 tests. Daily reset UTC minute must also be explicitly configured. No operating value selected.
- [x] migration0036 pinned intent/create receipts/provider tx uniqueness/empty approved-policy table; account sequence and baseline/revision fence; existing planActivityGrant shared wallet/ledger plan. Original snapshot timestamp advances baseline, preserving later accrual.
- [x] registered create/read/spin/provider routes. Local signed provider/real authenticated routes, concurrent spins/basic claim, ledger rollback, account binding/transaction replay, policy-retirement lost-response restoration, delayed spin, daily-limit/concurrentcreate:12 integration tests PASS.
- [x] Workers4-component typecheck, scoped lint/diff checks; reviewer no further material payout bug. Whole local110files835tests PASS before final replay-restoration delta, then affected6files69tests PASS and expanded integration12tests PASS. Exact evidence boundaries are retained rather than claiming the full suite was rerun after every addition.
- [ ] Actual environment-bound AdMob app/unit/placement setup, approved probabilities/expiry/limit/reset/retry/eligibility/platform policy, real device+Google SSV end-to-end. No activation/deploy/live grant authorized or performed.

Wire samples generated/readback under unity-project/QA/Unity1Artifacts/Reports/Server2-AdSamples-20260914 (export1testPASS, synthetic100%2x only). Unity was sent this frozen local contract. GALLERY-ROUTE-MAPPING.md's historical27 absent statement is superseded by this local implementation, not by a claim of production readiness.

Reference for wire signature format, key rotation, and deferred economic reward: [Google AdMob Unity SSV](https://developers.google.com/admob/unity/ssv), read2026-09-14. Original query bytes must stay unchanged; ECDSA SHA256 DER signature, key_id selects trusted key, cached keys expire within24h. Our verifier and policy-binding behavior are project implementation decisions.

<!-- source-body:source-61bc2eaacb1bc07f:end -->


<a id="source-5e53317a60633b39"></a>
## 공통 룰렛 팝업 문구·상태 검토

원출처: `docs/design/SHARED-ROULETTE-COPY-REVIEW-20260917.md`  
원문 SHA-256: `04cb261023940a7dd8345a9f3eec5f9c636f2f5a98dc00c579225b8d179f869a`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Complete copy/error/lifecycle review incl later approved6.5s+.6s, cards retained, idleframe, retry/claim rendering; no test/drafttext promoted live.

<!-- source-body:source-5e53317a60633b39:start -->
<a id="source-5e53317a60633b39-공통-룰렛-팝업-문구상태-검토"></a>
### 공통 룰렛 팝업 문구·상태 검토
2026-09-17 사용자 지시 반영. 코드 적용 완료 아님.

<a id="source-5e53317a60633b39-기준과-최근-수정"></a>
#### 기준과 최근 수정
승인 원본: QA/ResourceComposition-20260914/Direction/GalleryRevision01/27-supply-roulette-v02.png (2026-09-14 Gallery27 v02 승인). 보급과 작전 보상 모두 같은 파란 배수 분할 룰렛 팝업을 사용한다. 뒤의 원래 화면을 어둡게 표시하고 전체 페이지로 전환하지 않는다. 최근 Concept02/03 상자 이미지는 결과 표시 후보이며 기존 회전 룰렛을 대체하는 승인본이 아니다.

<a id="source-5e53317a60633b39-문구-판정"></a>
#### 문구 판정
- 보급 룰렛 / 전투 보상 룰렛 / 작전 보상 룰렛: 제목을 바꾸어 추가하지 말고 삭제한다(사용자 제목 삭제 지시).
- 룰렛을 돌려 보급 배율을 확인하세요: 기본 안내 삭제. 돌리기 버튼과 배수 분할로 전달한다.
- 기본 보급 / 기본 보상 / 수령 완료 · 확정 보상 / 지급 완료: 정상 화면의 제목·중복 상태 문구 삭제.
- 보상 표시: 2026-09-17 사용자 최신 지시로 Gallery27 v02 승인 최종시안의 보상 카드 구성·프레임·재화 아이콘·이름·수량 배치를 따른다. 아이콘 내부 우측하단 수량으로 바꾸는 Concept03 및 직전 검토안은 적용하지 않는다. 재화 이름과 수량은 승인본 배치대로 독립 텍스트로 유지한다. 회전 전은 배율 적용 전 대상 수량, 지급 후는 서버 확정 실제 수량이며 이미 총배수 적용된 값을 다시 곱하지 않는다. 정상 상태의 중복 제목 삭제 등 다른 문구 정책은 유지한다.
- 당첨 배율은 기본 보급 전체에 적용됩니다: 삭제. 작전의 최초 보상·특수 보상에는 예외가 있어 전체 적용이라고 안내하지 않는다. 적용 대상·제외 항목 설명은 확률 정보 상세로 옮긴다.
- 룰렛 돌리기: 검증 완료 전까지 비활성, 검증 완료 시 사용. 지급 확정 후 확인 하나로 바꾼다. 확인은 팝업 닫기/응답 확인이며 추가 지급 동작이 아니다.
- 확률 정보: 별도 상세 진입으로 유지. 본문에 확률 수치/가능배율 안내를 반복하지 않는다. 지급 완료에서는 확인 버튼 하나만 노출한다.

<a id="source-5e53317a60633b39-상태별-필요한-최소-안내"></a>
#### 상태별 필요한 최소 안내
정상 준비·지급 완료는 추가 문구 없음. 광고 검증 지연은 광고 확인 중…, 스핀 응답 미확정은 결과 확인 중…, 통신 실패는 연결을 확인한 뒤 다시 시도해 주세요. 상태에 맞는 재확인 동작은 동일 서버 요청/결과 복구이고 새 광고나 새 추첨이 아니다. 장기 지연에도 닫기 및 기존 결과 화면으로 복귀 가능하며 원래 요청은 보존한다. 미확정 상태에 당첨/수령 완료를 표시하지 않는다.

<a id="source-5e53317a60633b39-진입-및-보상-경계"></a>
#### 진입 및 보상 경계
작전 결과 화면의 선택 광고 → 광고 종료 → 같은 공통 팝업에서 검증 대기/룰렛 돌리기 → 원결과 표시 → 확인 → 작전 결과 화면 복귀. 보급은 보급 화면으로 복귀한다. 기본 수령 경로는 원래 결과/보급 화면에서 유지한다. 기본 수령이 먼저 확정된 경우 추가 지급하지 않는다. 광고 미설정/미지원 시 기본 수령을 유지한다.
작전 보상 중 배율 제외 항목은 결과 화면에 별도로 남긴다. 공통 View에 합성한 전체 숫자를 지급 근거로 삼지 않는다. 공통 View는 표시 데이터와 콜백만 받고 보급/작전 서버 연결·저널·지급은 각 Controller 책임을 유지한다.

<a id="source-5e53317a60633b39-현재-소스와-차이"></a>
#### 현재 소스와 차이
CombatAdView에는 아직 전투 보상 룰렛/확률/완료 문구가 존재하며 LobbyOfflineAd는 보급 전용 문구와 별도 화면 생성 경로를 유지한다. 최근 시안만으로 코드 삭제/공통 팝업 통합이 적용됐다고 주장하지 않는다. 현재 코드는 완료 시 확인 단일화와 배경 변경까지만 반영됐다. 다음 구현에서 두 진입 경로를 승인 팝업 View로 합치고 이 문구 정책과 상태별 검증을 함께 적용해야 한다.

<a id="source-5e53317a60633b39-2026-09-17-공통-룰렛-결과-팝업-최종-승인"></a>
#### 2026-09-17 공통 룰렛 결과 팝업 최종 승인
사용자: “좋아 이거로 최종시안 결정”. 승인 파일: QA/CombatAdUnity-20260917/Concept05/shared-roulette-result-reference-only.png. 보급·작전 보상 공통 룰렛의 지급 확정 후 결과 상태 최종 시각 기준으로 지정한다. 승인 범위는 결과 팝업이며 회전 전 Concept04의 별도 최종 승인을 추정하지 않는다. 기존 Gallery27 v02와 모든 원본은 보존한다.
파란 분할 룰렛 유지, 당첨 칸 강조, 중앙 확정 배수, 기존 보상 카드의 이름/아이콘/하단 중앙 수량 배치, 확인 버튼 하나를 사용한다. 제목·중복 완료 문구·닫기 X·확률 링크는 결과 상태에서 표시하지 않는다. 4배와 3,120/20/640은 예시이며 실제 배수·수량은 서버 확정 결과로 표시한다. 보급/작전 모두 같은 View를 쓰고 각 Controller가 데이터와 콜백을 연결한다. 프레임·배경·룰렛·포인터·배수·보상 아이콘/이름/수량·버튼·효과를 독립 제어한다. 승인 이미지는 reference-only이며 한 장짜리 실행 UI로 사용하지 않는다. 최종시안 승인 완료, Unity 반영 및 실제 화면 검수는 미완료.

<a id="source-5e53317a60633b39-구현-상태-갱신--2026-09-17"></a>
#### 구현 상태 갱신 — 2026-09-17
위 초기 검토의 별도 화면/옛 문구 미반영 상태는 이후 SharedRoulettePopup/SharedRouletteVisual 공통 연결로 대체되었다. 결과 단일 확인, 독립 카드, 서버 수량 표시, 제목/중복 문구 제거 및 소진 상태 구분 구현. QA/SharedRoulette-20260917/final2.xml 40/40 PASS. 실제 캡처 및 자산 감사는 같은 QA 폴더. 완전한 승인본 미술 일치와 보급 확률 상세 계약 연결은 잔여이며 테스트 통과로 완료 주장하지 않는다.


<a id="source-5e53317a60633b39-2026-09-17-룰렛-승인본-후속-보정보급-확률-계약-연결"></a>
#### 2026-09-17 룰렛 승인본 후속 보정·보급 확률 계약 연결
승인 Concept05 방향으로 전용 roulette-frame-v01 alpha 프레임을 생성·분리하고 RouletteFrameGraphic의 center 없는 9-slice로 외곽/카드/확인버튼에 적용. 기존 공통 팝업 원본 자산은 변경하지 않았다. 포인터 CanvasRenderer 누락 보완으로 cyan 면 표시, 섹터 발광 폭 및 배수 outline 조정. 실제 Unity 결과 QA/SharedRouletteFollowup-20260917/unity-result-4x.png 확인. 완전한 원화 미술 일치 승인이나 프로젝트 전체 자산 감사 완료를 의미하지 않는다.
보급 서버 intent는 저장된 policy_json의 probabilities[{multiplier,weightBps}] 2/3/4 전체 및 zero를 반환한다. displayMultipliers/추첨/지급/영수증 의미는 유지. Unity는 합10000·정수범위·중복/누락·허용배수 일치·알려진 확률 변경/제거를 검사하고 상세창에 실제 확률을 표시한다. 확률 없는 과거 기록은 호환하며 GET 후 새 정보 추가를 허용한다. ad-spin 복구에서 예전 pinned 대신 restored.WithResult를 사용해 갱신된 확률이 영수증/저널에 유지되도록 수정.
검증: 계약 최초red18건, sparse누락red2건, 확률복구유실red1건 확인 후 수정. Unity 최종63/63PASS(final.xml), 서버2files32PASS 및 Game API typecheck/변경 ESLint PASS. 실제 보급 UI 확률버튼 표시·확정결과·재바인딩·복구·4종스크롤·단일확인 검사. 독립리뷰 2건 수정·재검토 완료. 캡처 odds 2배100%는 합성 테스트 정책이며 운영 정책 변경 아님. 2배60/3배30/4배10 승인 운영기획 유지; 이번 작업은 확률을 바꾸지 않고 요청별 고정 정책을 전달한다.
QA 보고서/원화프롬프트/alpha 검수/verification.json은 QA/SharedRouletteFollowup-20260917. 서버 운영 배포·실제 광고/실기기 검증 없음. 신규 서버 계약 배포 전 과거 응답은 확률 미제공 안내를 유지한다. 실제 광고 SDK/운영설정·실기기 통합검증은 후속이다.

<a id="source-5e53317a60633b39-2026-09-17-룰렛-시각-보완-14-적용"></a>
#### 2026-09-17 룰렛 시각 보완 1~4 적용
사용자 승인 후속: 룰렛 내부 광택/중앙·포인터 네온/보상 원화/배경·버튼 질감. roulette-glass-v01을 독립 섹터·버튼판에 적용하고 반사 띠/발광 레이어를 보정. 중앙 배수는 별도 소유 TMP 재질로 속이 빈 네온 표현, 포인터는 어두운 내부와 청록 테두리로 조정. 경험치는 기존 원통 대신 alpha 분리한 얇은 금속 칩 사용, EXP/이름/수량은 각각 독립 텍스트. 배경은 좌측 램프·우측 열차 모니터 장비실 v02. 버튼의 거친 입자를 줄이고 독립 발광 테두리를 적용. 기존 프레임·정책·수량·지급·회전/복구 로직은 유지.
QA/RouletteArtPolish-20260917/first.xml 63/63PASS, 실제 Unity720x1280 결과·보급·작전 캡처 확인. 독립 읽기리뷰에서 수명/재질/경제권한 회귀 발견 없음. 원본 승인 이미지 및 구 자산 보존, 신규 원화/프롬프트/alpha·독립 구성은 RESOURCE-AUDIT.md, 소스/자산SHA verification.json. 전체 자산 감사나 새로운 원화 최종 승인·픽셀동일 주장 없음.
잔여 5번: 회전→감속→당첨강조→수량표시 영상 및 작은 화면·긴 수량 검수. 실제 광고 SDK/운영 배포/실기기 검증은 별도이며 이번 작업에서 수행하지 않았다.

<a id="source-5e53317a60633b39-2026-09-17-공통-룰렛-회전-순서소형-화면-보완"></a>
#### 2026-09-17 공통 룰렛 회전 순서·소형 화면 보완
회전 종료 전에 당첨 배수/강조/최종 수량이 노출되던 문제를 PlayMode 실패 테스트(red.xml)로 재현 후 수정. SharedRouletteVisual이 시각 연출 상태를 소유하며 회전·감속 중 결과/재화 영역을 숨기고 종료 후 서버 영수증 그대로 공개한다. SharedRoulettePopup은 연출 중 확인을 비활성화한다. 지급/추첨/광고/확률/서버 계약 변경 없음. 반복 Render는 회전을 재시작하지 않으며 동일 영수증 재열기는 즉시 결과를 표시한다.
검증: QA/RouletteMotion-20260917/green.xml 64/64 PASS. 실제 Unity PlayMode 프레임과 측정 시각(frames.json), 해당 시각 간격을 보존한 roulette-motion.mp4 생성. 360x640 및 480x853에서 긴 수량/4번째 카드 스크롤을 캡처하고 TMP overflow 없음 확인. 16자리 최대 경계 수량은 글씨가 작아지는 가독성 한계가 남는다. 영상은 로컬 합성 영수증으로 실제 View를 재생한 QA이며 실제 광고 SDK·서버 지급·실기기 성능 검증은 아니다. 원래 승인 원화/독립 리소스 구조 유지.

<a id="source-5e53317a60633b39-2026-09-17-룰렛-긴-수량-가독성-보완"></a>
#### 2026-09-17 룰렛 긴 수량 가독성 보완
이전 최대 16자리 수량의 축소 문제를 수정. 수량은 서버 영수증의 쉼표 포함 정수를 그대로 유지하고, TMP 실제 글자 폭에 맞춰 해당 보상 카드만 넓힌다. 넘치는 총 폭은 기존 수평 ScrollRect로 탐색하며 3종 이하라도 긴 수량이면 스크롤 안내를 표시한다. 일반 수량의 기존 카드 배치·아이콘·프레임 유지. 수량 글자 크기38 고정, 실제 글꼴 줄 높이45.33이 종전44 영역을 넘는 원인을 확인해 측정 높이+여백으로 배치했다. 숫자 축약/반올림/재곱셈/지급 계약 변경 없음.
QA/RouletteQuantity-20260917: 최초 red.xml 글씨20.75로 가독성 실패, green.xml 63/65로 세로overflow 확인, diagnostic.xml로 실제 높이 측정, final.xml 최종65/65PASS. 360x640/480x853 PlayMode 실제 캡처 및 마지막 카드 접근, 정확한 숫자·단일줄·overflow 없음 확인. long-quantity-360.png/last-card-360.png/long-quantity-480.png 보관. 이전 회전 QA 캡처는 보존하고 새 실행은 고유 run 폴더에 저장하도록 수정. 실제 기기·광고 SDK 검수는 별도.

<a id="source-5e53317a60633b39-2026-09-17-사용자-요청-룰렛-6초-이상-회전"></a>
#### 2026-09-17 사용자 요청: 룰렛 6초 이상 회전
SharedRouletteVisual의 회전 시간을 1.05초에서 6.5초로 변경하고 8회전 cubic 감속을 적용. Time.unscaledTime 기준, 회전 중 결과/수량 비공개 및 확인 차단 유지. 반복 조회 및 동일 영수증 복원은 재회전하지 않는다. 최소6초 결과 비노출 테스트 red 재현 후 관련65/65PASS(QA/RouletteSixSeconds-20260917/final.xml). 실제 Unity 재생 영상 roulette-6.5s.mp4는 프레임 시각 간격을 사용한다. 지급/확률/서버 변경 없음.

<a id="source-5e53317a60633b39-2026-09-17-룰렛-최종시안-재비교--미술-보완-미완료"></a>
#### 2026-09-17 룰렛 최종시안 재비교 — 미술 보완 미완료
사용자 요청으로 Concept05 최종 승인본과 최신6.5초 종료 frame-0080(정상3종수량)을 동일 표시폭으로 비교했다. QA/RouletteVisualReview-20260917/REVIEW.md와 comparison.html에 근거/우선순위 저장. P0: 당첨 섹터 대비·유리/금속 칸막이 깊이·중앙 허브와 배수 네온·누락된 하단 공통 장비판. P1: 포인터 크기/입체감·용도별 프레임·재화 원화 점유율·확인버튼 재질/높이. P2: 배경 국소조명/crop. 코드상 중앙은 원형그래픽 조합, 하단 공통판 없음, 프레임3용도와 유리2용도의 동일자산 재사용을 확인. 기능65PASS는 미술 승인 근거 아님. 이번은 분석만 수행했고 런타임/원본/보상 정책 불변. 외부 검정은 독립 QA 배경이므로 실제 로비 블러 누락으로 단정하지 않음. 다음 구현은 룰렛 본체/당첨광원→하단 장비판→원화/배경 순서.

<a id="source-5e53317a60633b39-2026-09-17-룰렛-미술-보완-첫-묶음-적용"></a>
#### 2026-09-17 룰렛 미술 보완 첫 묶음 적용
최종 승인본 비교에서 우선순위 높은 당첨 섹터/중앙 허브/포인터/하단 장비판 보완. roulette-hub-v01 실제 alpha 독립 원화 생성 및 연결, 배수는 별도 TMP와 소유 재질로 유지. 포인터 확대·속빈 네온 배치 조정. 비당첨 유리 명암을 낮추고 당첨 중심광·번짐·금속 칸막이 강화. 첫 캡처에서 발광 띠가 금속 링·허브에 가려짐을 확인해 실제 보이는 .218~.366 반경 영역으로 이동 후 재캡처. 배경 위에 별도 RewardEquipmentDeck을 추가하고 카드/버튼을 받치도록 구성, 확인버튼 높이120. 휠만 아래로20 이동하며 보상/스크롤 안내와 상태 문구 간격을 분리.
QA/RouletteDepth-20260917/final.xml 65/65PASS; first.xml도65PASS이나 시각 피드백에 따라 추가 수정한 뒤 재검증. unity-result.png 실제 PlayMode 종료 캡처, comparison.html 승인본과 동일 표시폭 비교. RESOURCE-AUDIT.md와 hub-generation-prompt.txt에 신규 자산·alpha·원본·런타임 독립 연결 기록. 6.5초8회전/서버 수량/광고·지급 계약 유지. 프로젝트 전체 미술 승인 완료 아님.
남은 차이: 카드/외곽 용도별 프레임, 보상 원화 점유율/실루엣, 버튼 전용 재질과 균열, 배경 국소 조명. 이번은 우선 첫 묶음 적용이며 기존 승인본을 새 캡처로 자동 대체하지 않는다.

<a id="source-5e53317a60633b39-2026-09-17-룰렛-카드버튼-보완"></a>
#### 2026-09-17 룰렛 카드·버튼 보완
카드/버튼의 동일 외곽 원화 축소 재사용을 RouletteInsetFrameGraphic 독립 빈 프레임으로 교체. 카드10/버튼15 두께, 절개 모서리·금속 면 명암·나사 표현 분리. 판/프레임/아이콘/동적 이름/수량/입력은 독립 유지. 보상 아이콘 최대 높이135→154(약14%)로 확대하며 이름/수량 겹침을 실제 캡처로 확인. 버튼은 신규 roulette-button-glass-v01 전용 재질을 사용하고 기존 강한 가로 광택 띠를 제거, 강조 강도2.7→1.65. 6.5초8회전·정확한 수량·스크롤·복원/광고/지급 로직 유지.
검증 QA/RouletteCards-20260917/first.xml 65/65PASS 및 git diff --check. unity-result.png 실제 PlayMode 결과, long-quantity-360.png에서 글자/아이콘 분리 확인. comparison.html은 승인본과 동폭 비교. 새 버튼 재질은 글자·프레임·아이콘 없는 불투명 내부 판 용도이며 alpha 프레임으로 오인하지 않는다. 생성 원본 exec-9aea8430-3097-4bc2-9b37-7353765d43d3.png 보존; 프롬프트 button-generation-prompt.txt 저장. 생성 원본 실제2172x724이며 런타임 내부판에 UV로 매핑. 프로젝트 전체 감사 완료나 미술 최종승인 아님.
잔여: 카드/버튼 프레임의 세밀한 마모·금속 재질 깊이, 배경 국소조명, 승인본과의 전체 비율 최종 비교. 현재 캡처로 승인 기준 이미지를 대체하지 않는다.

<a id="source-5e53317a60633b39-2026-09-17-룰렛-프레임-마모배경-조명-보완"></a>
#### 2026-09-17 룰렛 프레임 마모·배경 조명 보완
RouletteInsetFrameGraphic의 금속 띠에 고정 마모/긁힘을 추가. rebuild 때 난수나 상태 변경 없음, 중앙 판·텍스트·아이콘 영역을 덮지 않는다. RouletteAmbientLightGraphic 독립 UI 그래픽으로 배경 위·장비 아래에 왼쪽 따뜻한 램프광, 오른쪽 보조광, 모니터 푸른 반사광을 배치. 모든 광원은 raycastTarget=false이며 배경 원화는 보존. 전체밝기 변경 대신 국소 색/알파 falloff를 사용한다. 기존 독립 리소스·6.5초회전·수량·입력·지급 계약 유지.
QA/RouletteWearLighting-20260917/first.xml 65/65PASS, git diff --check 통과. unity-result.png 실제 PlayMode 결과 및 long-quantity-360.png 확인, comparison.html 승인본 동폭 비교. 금속 마모는 물체 고정 재질에 속하며 광원3개는 각각 숨김/색/배치 교체 가능한 독립 요소. 신규 생성 bitmap 없음. 전체 미술 승인/프로젝트 전체 자산 감사 완료 아님. 승인본과 중앙 네온 번짐·원화 재질/비율의 차이는 남고, 실제 로비 위 전체 합성 검수는 후속.
이번 Unity 로그에 C LocalLow Unity GiCache 생성도 기록되었다. 지정 프로젝트 QA/소스 출력은 D이며 앱 내부 캐시까지 D라고 주장하지 않는다.

<a id="source-5e53317a60633b39-2026-09-17-중앙-네온-번짐세부-유리-재질-보완"></a>
#### 2026-09-17 중앙 네온 번짐·세부 유리 재질 보완
CommittedMultiplier의 선명한 윤곽을 유지하고 별도 HubTextBloom TMP와 HubTextSpill 국소 반사광을 추가했다. 글자와 bloom은 기존 revealed 상태/배수에 동기화해 회전 중 결과 노출 없음. 각각 소유 재질은 OnDestroy에서 해제한다. 신규 roulette-glass-v02 독립 균열 유리 material texture 적용, 기존 v01 및 승인 원본 보존. 최초 캡처가 어두워 색상 곱셈을 완화하고 선택 섹터 내부 반사광을 추가해 재검증했다. 당첨 상태/글자/테두리는 텍스처에 굽지 않았다.
QA/RouletteNeonMaterial-20260917/final.xml 65/65PASS(최초 first도65PASS, 시각보정 후 재검증), git diff --check 통과. unity-result.png 실제 PlayMode 종료 화면, comparison.html 승인본과 동폭 비교. RESOURCE-AUDIT.md 및 glass-generation-prompt.txt에 원본/독립 구성 기록. 6.5초회전/정확한수량/광고·지급 계약 불변. 승인 원화와 완전 일치나 프로젝트 전체 자산 감사 완료를 주장하지 않으며 사용자 최종 미술 승인 상태는 별도.

<a id="source-5e53317a60633b39-2026-09-17-승인-변경-회전-중-보상-카드-유지"></a>
#### 2026-09-17 승인 변경: 회전 중 보상 카드 유지
사용자 승인(제안 후 ㄱㄱ): 회전 전/중 아이콘·이름·기본 보상 수량을 유지하며 카드 테두리에 은은한 청색 빛. 정지 전 중앙 확정 배수 및 최종 수량 노출 금지. 6.5초 회전 종료 후 당첨 배수를 강조하고 0.5~0.7초 정도 수량 증가 표현으로 서버 확정 수량에 도달한 뒤 확인 활성화. 기존 회전 중 보상 영역 숨김 정책을 이 범위에서 대체한다.
표현용 기본값은 기존 미리보기에서 보존하고 확정 receipt의 ID+kind와 대응시킨다. 최종 수량/배수로 역산하지 않는다. 기본값이 없거나 매칭되지 않는 항목은 수량 중립 표시 후 확정치를 보여준다. 최종 수량은 서버 값 그대로이며 중간 숫자는 화면 연출일 뿐 지급/저장 값이 아니다. 증가가 필요 없는 항목은 불필요하게 변동시키지 않는다. 회전/수량 연출 동안 확인 잠금; 재열기 완료 영수증은 최종 상태 즉시 표시. 해제/계정 교체 후 업데이트 차단, 미리보기 혼입·중복 연출·큰정수 오버플로·긴수량 잘림 검수.
Unity_2 단독 구현/테스트 및 보급·작전 실제 호스트 캡처/후저장 영상으로 검증. 기존 원화/정방향 글자/6.5초/지급 계약 유지. 이번 기록은 승인 명세와 작업 전달이며 구현 완료 보고가 아니다.

<a id="source-5e53317a60633b39-2026-09-17-회전-중-보상-유지-변경-사용자-승인"></a>
#### 2026-09-17 회전 중 보상 유지 변경 사용자 승인
사용자: 룰렛변경은 만족스럽네 좋아 다음으로 넘거가면되겟다. Checkpoint60의 회전 중 기본 카드 유지/정지 후0.6초 확정 수량 증가/확인 활성 흐름을 사용자 수용으로 기록한다. 이전 원본과 증거 보존. 이 수용을 전체 미술 잔여나 실기기 성능 검수 완료로 확대하지 않는다. 다음 요청은 광고 스킵 유료상품 기획이며 아직 가격·기간·범위 확정 없음.

<!-- source-body:source-5e53317a60633b39:end -->


<a id="source-a54cbe3073573484"></a>
## Augment Reroll Server Phase 2 Implementation Plan

원출처: `docs/superpowers/plans/2026-09-17-augment-reroll-server-phase2.md`  
원문 SHA-256: `8d8d82615fc691fd591c46fe406367e8b9c2d73bf6ae95cc955f1d520a208062`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Trusted solo-domain issuer/read/command/receipt/checkpoint contracts, single-flight revision recheck and finalization race; grant/event identity supersedes fixed five stages.

<!-- source-body:source-a54cbe3073573484:start -->
<a id="source-a54cbe3073573484-augment-reroll-server-phase-2-implementation-plan"></a>
### Augment Reroll Server Phase 2 Implementation Plan

> For agentic workers: use superpowers:executing-plans for inline work. 독립 도메인 범위 완료. 운영 연결은 아래 선행조건이 있는 후속 범위다.

**Goal:** 신뢰된 서버 내부 솔로 grant에서 무료3+공유 추가1 리롤 및 최종 선택을 처리한다.

**Architecture:** 작전 issuer가 run/event/stage와 검증된 적격 카탈로그를 제공한다. 단일 aggregate가 후보·잔량·revision·선택·원요청 결과를 소유하고, 외부 승인은 명시적 port로 주입한다. 도메인은 UI/Store/HTTP를 소유하지 않는다.

**Tech Stack:** TypeScript/Zod/Vitest. 이번 단계는 메모리 도메인과 내부 checkpoint이며 D1 연결 없음.

**Spec:** docs/design/RUN-MAP-AUGMENT-EVENTS-20260916.md, AUGMENT-UNIFIED-POOL-POLICY-20260915.md, AUGMENT-POOL-40-POLICY-20260915.md, AD-SKIP-PERMANENT-DETAILED-20260917.md §9. COOP-UNIFIED-AUGMENT-CHOICE-20260916.md는 기획 후보로만 대조.

<a id="source-a54cbe3073573484-사전-조사와-승인-범위"></a>
#### 사전 조사와 승인 범위

- [x] 실제 cwd D:/Project/LASTRAIL, Git top D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907, common D:/Project/LASTRAIL/.git, HEAD d4df4246762b6fc9677fc1e3126f9483033872eb 및 D probe 읽기/쓰기 확인.
- [x] 활성 server/apps/game-api/src 검색: 최신 map node/stage issuer 없음. ce03-rail-journey.ts는3종 ADD_BPS/2후보/wave3,6,9,12이고 ce03-local-host.ts가 preparation에서 호출. 0005_run.sql run_augment_history는 wave_index+augment_json뿐이며 src writer 없음.
- [x] 최신 기획의 경로별5stage/최초3후보와 불일치를 Plan/Unity에 보고. Unity도 기존 rail 로컬 흐름만 확인.
- [x] Plan 추가 지시로 독립 솔로 도메인 로컬구현 승인. trusted 내부 발급만, 공개 생성/API/생산 seed 금지. 실제 owner 연결 전 REROLL UNSUPPORTED 유지.

<a id="source-a54cbe3073573484-task-1-공통-도메인"></a>
#### Task 1: 공통 도메인

파일 기준: 활성 checkout의 server/.

- [x] tests/unit/solo-augment-selection.test.ts에서 관찰 가능한 요구사항 테스트 작성, 모듈 부재 red 확인.
- [x] apps/game-api/src/solo-augment-selection.ts 구현. issue/read/reroll/select/checkpoint/restore와 ExtraApprovalPort. 슬롯3+1, 현재3장 제외, 균등 index 추첨, 같은 후보 역사 재등장 허용, 후보없음 미소모.
- [x] FREE와 EXTRA를 같은 명령에 연결. 무료 소진 뒤만 EXTRA 가능. 검증된 승인 target 일치 확인, await 뒤 revision/state 재검사.
- [x] 동일key 진행 요청의 승인 중복2회 red 확인 후 single-flight 수정. SELECT와 다른 본문의 진행key 탈취도 거절.
- [x] 15테스트: 새grant 초기값, 후보교체/균등, 소진, owner/event/revision, 승인실패/잘못된 binding, 선택경쟁, 다른key경쟁, 같은key합류, 내부checkpoint 재개·승인/CONFIRMED 원결과 복원, malformed 입력, 외부 반환값 수정 격리.

<a id="source-a54cbe3073573484-task-2-검증과-내부-dto-인계"></a>
#### Task 2: 검증과 내부 DTO 인계

- [x] 독립 읽기 리뷰 지적 수정 확인. 잔여 actionable finding 없음.
- [x] scripts/validation/export-solo-augment-samples.ts로 실제 내부 반환값 TEST_ONLY fixture 생성. 공개 HTTP 응답으로 표기하지 않음.
- [x] 전체116files920tests PASS exit0, game-api tsc0, 변경3파일 lint0, diff-check0. 기존 index CRLF 경고/untracked 미포함 한계 기록.
- [x] 1차69개 source pin 일치 확인. 1차115files905 로그 보존.
- [x] QA/AdSkipRerollServer-20260917/README.md에 정확한 DTO·key/revision·오류·approval 및 운영 미연결 경계 기록.
- [x] 최종 source pins/Server handoff readback 및 Plan/Unity/Admin 인계 자료 준비.

<a id="source-a54cbe3073573484-후속-운영-연결-선행조건--이번-구현-완료에-포함하지-않음"></a>
#### 후속 운영 연결 선행조건 — 이번 구현 완료에 포함하지 않음

1. 실제 map 진행 owner: 신뢰된 node 도달/완료/안전 대기에서만 grant 발급. run/stage 유일성, 시작 포함 경로별5stage 보장. 클라이언트가 보내는 현재 wave/node 선언만으로 발급 금지.
2. 실제 검증 카탈로그와 보유 적격성: 32/40 기획 카드를 구현된 것으로 seed하지 않는다. 보유 중첩/역할 적격성/카탈로그 버전 갱신 규칙을 owner에서 결정.
3. D1 상태 소유와 효과 적용: 후보/잔량/revision/receipt를 한 transaction에 저장하고 read/replay를 구분. CONFIRMED는 지금 선택 결정뿐이며 실제 효과 적용영수증과 원자 연결이 필요하다. 내부 checkpoint는 untrusted client 복원 입력이 아니다.
4. 승인 adapter: 실제 영구권 회수와 승인 순서 검증, SSV challenge/계정/환경/기한, OFFLINE/COMBAT/REROLL 공유 provider 거래 유일성과 DB CHECK/구writer 경계. 현재 port 계약만 있으며 synthetic approval 테스트는 이를 운영 검증하지 않는다.
5. 위 경계 연결 후 Unity와 공개 DTO/HTTP 오류를 별도 고정하고 AVAILABLE를 판단. 지금 UI는 transport-independent Controller/View 단위로 준비한다. 과거 receipt snapshot으로 최신 revision을 낮추지 않는다.
6. 협동 문서의 교대 담당/공용3+1/직렬화는 기획 후보다. 솔로에서 임의 확정하지 않으며45초/A를 운영상수로 추가하지 않는다.

실제DB/Store/START/지급/배포 및 기존 legacy 교체 없음. 새 산출물은 D:/Project/LASTRAIL 내부에만 저장한다.


<!-- source-body:source-a54cbe3073573484:end -->
