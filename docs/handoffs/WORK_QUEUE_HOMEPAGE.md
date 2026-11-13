# LASTRAIL HOMEPAGE 작업 큐

2026-09-22 사용자 요청으로 만든 담당별 실행 큐. 신규 설계 원본이나 완료 이력 보관소가 아니다. [공통 운영 규칙](INTEGRATION_WAITLIST.md#role-work-queues-20260922), 프로젝트 AGENTS.md를 먼저 따른다. 최신 승인 기획만 신규 작업에 적용하며 구 정책은 신규 적용 대상에서 폐기한다. 진행 중 run의 START 스냅샷과 역사/검증 자료는 보존한다.

착수 가능한 항목만 이 큐에 둔다. 선행 미완료 후속은 기존 WAIT의 후속 등록 정의에 보관하며 실행 큐에 미리 추가하지 않는다. 착수 시 실제 파일·버전과 진행 상태를 기록한다. 완료 담당은 기존 인계/QA에 큐 ID·계약 버전·검증 근거를 보존하고, WAIT의 자기 ID 관련 후속 조건을 확인한다. 모든 선행이 완료된 후속을 대상 큐에 중복 없이 추가·확인한 뒤 자기 완료 블록을 삭제한다. 후속 등록 실패 시 `구현 완료·후속 등록 필요`로 남겨 재시도한다. 상세 절차는 [완료 시 후속 큐 등록](INTEGRATION_WAITLIST.md#completion-driven-queues-20260922)을 따른다. 타 큐에는 승인된 후속 항목 추가만 허용하며 기존 항목 수정/삭제는 담당 소유다. 빈 큐는 종료하며 자동 메시지·폴링하지 않는다.

정책 원본: [44 전투](../superpowers/plans_categories/44_COMBAT_DETAIL_CONTRACTS.md), [09 지휘실](../superpowers/plans_categories/09_COMMANDER_PROGRESSION.md), [13 장비](../superpowers/plans_categories/13_EQUIPMENT_ENCHANTMENTS.md), [14 경로](../superpowers/plans_categories/14_MAP_EVENTS_DROPS.md), [31 증강](../superpowers/plans_categories/31_AUGMENT_EFFECT_REFERENCE.md), [32 서사](../superpowers/plans_categories/32_NARRATIVE_REFERENCE.md), [45 정비](../superpowers/plans_categories/45_MAP_STATION_DETAIL_CONTRACTS.md). 각 문서의 최신 승인 절을 사용한다. [통합 대조 I01~09](../superpowers/plans_categories/34_COMBAT_POLICY_COMPANION.md#integration-conflict-audit-20260922-v01)는 발견 근거이며 런타임 전수검증 결과가 아니다.

2026-09-29 사용자의 홈페이지 연동 작업 큐 일괄 등록 지시에 따라 아래 항목을 추가했다. 선행 대기 항목은 착수 금지로 표시하며 기존 완료 기반 등록·삭제 규칙은 그 외 항목에 유지한다.

<a id="homepage-queue-20260929"></a>
## 홈페이지 연결 후속 큐 — 2026-09-29 사용자 일괄 등록
기존 H-01(S-01 AND S-04)과 승인 홈페이지 미술/읽기 UI는 유지. 아래 항목은 명시 선행 완료 전 **착수 금지**이며 배포·실게시·가짜 성공자료 사용 금지. [WAIT 등록 근거](INTEGRATION_WAITLIST.md#homepage-queue-registration-20260929).

### H-02@public-read-v1 / 선행 대기: S-17
공지·확률 기존 list/detail과 PublicReleaseIndex/CatalogSnapshot을 기존 reading port에 연결. DEV 웹인증, 버전/구ID·게시이력과 현재 적용 구분, 400/401/403/404/503·no-store/재시도/새탐색/취소, 실제 브라우저·모바일 QA. 승인자료 없는 성공 화면 금지.

### H-03@private-support-v1 / 선행 대기: S-13,S-15
웹 본인문의 접수/목록/상세·공개답변 표시, 세션/CSRF/만료·중복전송·타계정/비공개 필터·응답유실 원receipt 복구와 재진입 QA. 운영 기능은 Admin.

### H-04@community-v1 / 선행 대기: S-13,S-16
11 §5의 공개게시판·글/댓글/신고/차단·본인권한 UI를 승인 계약으로 연결. 입력/접근성·sanitize 표시·오류/권한회수/재진입 QA. 공개 공식공지·위키를 유저 글로 편집하지 않음.

## 최종 승인 기획 인수 경로 — 2026-09-29 / 중복 실행 항목 없음
[WAIT 후속표](INTEGRATION_WAITLIST.md#approved-plan-queue-sync-20260929) 기준. 기존H-01/H-02는실제승인공개snapshot에최신5성·성급/가격·보관함30~54·공속/가속/DEF와월드보스3종이포함됐는지대조하고공개된행만표시. MD수를직접복사해실서비스데이터로표시금지,조건부시뮬레이션확률을실드랍확률로공시금지. H-04는11승인커뮤니티계약소비, S-13/S-16완료전착수금지. 기존홈페이지/미술유지. 추가중복H항목0.


<a id="server-contract-intake-20261001"></a>
## 기존 H-02/H-03/H-04 계약 인수 보완 — 2026-10-01 Server / 사용자 요청

[API/DTO·환경·87개QA](LAST_RAIL_SERVER_2-D-20260913.md#homepage-dependency-reconciliation-20261001), [WAIT 범위 정정](INTEGRATION_WAITLIST.md#homepage-dependency-reconciliation-20261001). 기존키/타담당블록 보존·새ID/중복0. 과거전체대기로 완료HTTP 소비까지 차단하지 않는다.
H-03@private-support-v1은 web-session-v1·본인문의접수/receipt·private-support-list-v1/private-support-ticket-v1의 기존화면 DTO/상태·오류·만료/CSRF/권한회수 연결 착수 가능. 실제HTTPS Origin/audience/격리환경·쿠키 저장 왕복은 제공후 Homepage검증, 서버전체완료를 기다리는 순환조건 제외.
H-02@public-read-v1은 기존공지/공시 페이지·구ID/원문/오류·index/catalog DTO 소비 착수 가능. 실제WEB+PUBLIC 승인자료/manifest source·현재index 성공근거는 미충족, MD/fixture를 실게시/위키원본으로 표시금지·PUBLISHED_HISTORY와 현재적용판 구분.
H-04@community-v1 명령은 S16 API/저장 미구현으로 대기, 로그인공통은 H03 재사용.
완료조건 준비된계약 소비/오류·재진입/원키QA와 실환경/원자료 BLOCKED 분리, 전체H완료 아님. 설정 https://dev-api.lastrail.app은 현재도달/배포증거 아님·HTTP4176 preview는 Secure cookie 왕복근거 아님. no-store/서버cursor/본인404 유지, Game Bearer/Admin credential 공개JS노출0. 기존승인미술 유지·배포/실게시/실보상 없음.

## 기존 H-03 최신 승인 UX 인수 — 2026-10-01 Plan
server-contract-intake-20261001 부분착수 해제 유지. 11#homepage-login-approved-20261001 및 03#support-processing-approved-20261001의 복귀/만료·동일계정 메모리 초안·다른계정 폐기·본인 문의/최신 답변·새문의 흐름을 기존 H-03에 인수한다. H-04 공통 인증은 재사용하되 커뮤니티 실제 명령은 S-16 인수 후. 실제 Origin/OAuth/공개자료·브라우저 QA 대기를 소비 코드 전체 대기로 확대하지 않는다. 기존 홈페이지 유지, 새 H항목 없음.

## 점검 로그인 화면 연결 예약 — 2026-10-01 / 실행 항목 아님
사용자 요청 12#game-web-login-maintenance-20261001. [WAIT](INTEGRATION_WAITLIST.md#maintenance-login-followups-20261001)의 H-MAINT-LOGIN-01은 서버/시안 인수 후 등록. 기존 H-03 로그인 재사용·본점검 신규로그인 차단/공지·공개열람 유지/실해제후재시도. 현재 승인 홈페이지 보존, 전체웹 차단은 별도 실제정책일 때만. 예약안내로 선행완료나 구현완료 주장0.

<a id="h03-processing-policy-intake-20261001"></a>
## 기존 H-03@private-support-v1 문의 처리 정책 근거 인수 — 2026-10-01 Server

[완료 단위·계약·42검사·환경](LAST_RAIL_SERVER_2-D-20260913.md#s15-processing-policy-implemented-20261001). S15 OWNER 관리 및 승인 종료 최종답변/되돌림 사유 원자 정책 구현 완료. WEB 본인 접수/list/detail/receipt DTO·인증/Origin/CSRF는 기존 확정 계약 유지, 내부메모/운영자 이력/관리 API를 공개 웹에 추가하지 않는다. 기존 H03 부분 착수 해제 유지·새 중복 H항목0.

소비 완료 조건: 고객에게 최신 공개답변·처리 상태를 그대로 표시하고 종료를 해결/보상 지급 완료로 표현하지 않음. 과거 reply없는 종료를 신규 규칙 위반으로 삭제하지 않음, 새 문의는 별도 ticketId이며 본문에 기존 문의번호를 기재하는 승인 흐름 사용(가짜 관계 필드/자동 합침0). 내부 사유 비노출·정정 최신 답변·응답유실 원ticket/원키/계정 경계 검증 및 실제 버전 기록. Server 정책 단위 완료를 홈페이지 브라우저 QA에 다시 종속시키지 않는다.

실제 HTTPS Origin/audience/격리 계정·원격0097 적용과 WEB+PUBLIC 자료는 기존 환경 선행으로 분리, 이번 로컬42검사를 브라우저 왕복으로 승격하지 않는다. 화면 연결은 시안 완료 후 별도 정책 유지. 실게시·운영 접수·실답변·배포 없음.

<a id="homepage-contract-consumer-implemented-20261001"></a>
## 기존 H-02/H-03 부분 진행 — 2026-10-01 Homepage
[완료 코드 단위·실제 경로/계약·67/67 검사·남은 범위](../../QA/Homepage/HANDOFF.md#homepage-contract-consumer-implemented-20261001), [소스 pin/정확한 검증 구분](../../QA/Homepage/queue-contract-20261001.json). H-02 공개 GET/원ID/현재index·catalog/공시이력 DTO 소비와 H-03 웹 API·세션/메모리 초안/원키 접수복구 Model 구현·로컬 검증 완료. 기존 승인 홈페이지/미술 유지. 실제 Reading port/성공 표시 및 로그인·문의 Controller/View는 아직 연결하지 않았음; 부분 소비 코드 완료를 화면/실브라우저 완료로 확대하지 않는다.
H-02: 승인 WEB+PUBLIC source/index·환경/버전 설정·공개 실제 왕복 및 PC/모바일 남음. H-03: 승인 화면 인수 후 별도 연결 조건과 HTTPS Origin/audience/격리계정 cookie 왕복·목록/상세/재진입 QA 남음. H-04 실제 community 명령은 기존 S-16 인수 대기, 인증 Model은 재사용. 부모 유지·P-14 AND 미충족·후속등록/완료삭제0, 새ID/중복0, 배포/실게시/실접수/실보상0. 기존 부분 착수 해제를 전체 Server 대기로 되돌리지 않는다.

<a id="homepage-support-read-model-20261001"></a>
## H-03 조회 Model 단위 추가 진행 — 2026-10-01 Homepage
[기존 인계·78/78 검증·변경 경로](../../QA/Homepage/HANDOFF.md#homepage-support-read-model-20261001), [정확한 근거](../../QA/Homepage/support-read-model-20261001.json). 본인 목록/상세/명시 최신 조회·cursor·401/403/404·계정전환/취소·늦은 응답 처리 구현 완료. 기존 API/세션/작성 규칙 재사용, bootstrap/실화면/원격환경 연결0. H-03 화면 인수 후 Controller/View 및 HTTPS·PC/모바일 검증 조건 유지; H-02 승인 공개 source/index, H-04 S-16 미충족. 부모 유지/P-14 AND 미충족/새중복·후속등록·완료삭제0.
## 연계 총괄 인수 — 2026-10-01 / 기존 H02/H03/H04
WAIT#coordination-unblock-20261001 C2/C5/C6 적용. 완료된Model을 재구현하지 않고 H03 Controller/View 연결에 필요한 승인화면을 정확 경로/버전으로 확인한다. 기존승인화면 범위는 전체새시안 대기로 되돌리지 않고, 신규미승인영역만 Direction 필요목록에 기록한다. 실제cookie/browser 검증은 Server가 제공하는 환경범위로 분리한다. source없는 H02 실제성공/미구현H04명령만 해당대기,전체Server완료를 공통코드 소비선행으로 요구하지 않는다. 미술재작성/임의실자료0.

## 총괄 최신 범위 — 화면 연결 전면 보류 / 2026-10-01
사용자 최신지시 WAIT#coordination-no-screen-20261001 우선. 모든 실제화면/View/bootstrap 연결은 최종시안 전체 완료 후. 바로앞 총괄인수의 초기화UI/기존승인페이지 선연결 지시는 보류한다. Model/Service·화면없는명령조정/API adapter·서버producer/권한/저장·실제독립HTTP검증은 계속한다. 원격초기화는 기존동일OWNER인증을 유지한 안전한지원경로가 확보된때만,없으면 그실행만대기하고 로컬연계는진행. 시안은사용자가직접진행,완료자료/기존진척삭제0.

<a id="h03-maint-login-contract-intake-20261001"></a>
## 기존 H-03 로그인 오류·공개 상태 계약 준비 인수 — 2026-10-01 Server

[maintenance-login-v1·경로/DTO/오류·62/62·실환경 잔여](LAST_RAIL_SERVER_2-D-20260913.md#maintenance-login-commit-20261001). 기존 WEB password/Google 요청 DTO·Origin/Cookie 계약 유지; 신규발급만503 {error:"maintenance_login_blocked",contractVersion:"maintenance-login-v1",confirmed:true}, 정책누락/손상503 maintenance_policy_unavailable confirmed:false. 기존 WEB 세션/공개열람을 전체 삭제/차단하지 않는다. GET /v1/public/maintenance/login은 auth-free/query/body0·no-store·CORS* credentials0, gameLoginAllowed/webLoginAllowed·policyRevision/serverTime·공개안내/시간/retry를 제공; 예정종료 경과로 자동해제0. 일반503/DEV PAUSED를 확정점검으로 표시하지 않고 명시 상태조회/사용자 재시도, credential 자동재전송0.

기존 H-03 Model/Service·화면없는 adapter 계약 소비만 준비 가능. 서버 전체완료/브라우저QA를 준비된 단위의 선행으로 회수하지 않는다. H-MAINT-LOGIN-01 화면후속은 아직 AND 미충족·미등록, 모든 View/bootstrap 연결은 최종시안 후. 검증은 Hono+SQL.js이며 현재 원격DEV 반영 아님; 실제 socket/HTTPS 환경 제공은 서버 잔여. 기존 타담당 블록 수정/삭제·새중복 항목/실게시/실접수0.

<a id="completion-reply-dispatch-20261001"></a>
## 사용자 승인: 작업 완료 답장 — 2026-10-01

이번 연계 작업 완료 시 기존 인계/QA에 작업 ID·변경/계약 버전·검증 범위와 근거·후속 해제 항목·남은 의존성을 기록하고, 요청을 보낸 기획 총괄 채팅에 완료 메시지를 답장한다. 총괄이 확인 후 다음 작업을 지시한다. 사용자 명시 승인으로 이번 완료 답장은 과거 자동 보고 금지의 예외다. 수신 확인만의 답장·반복 폴링은 불필요하다. 전체 화면 연결은 최종 시안 전체 완료 이후, 시안 진행은 사용자 담당이다. 완료 실행 항목만 근거 보존 후 정리하며 부모 미완료 작업은 유지한다.

원본: INTEGRATION_WAITLIST.md#completion-reply-dispatch-20261001. 2026-10-01 재시작 후 앱 도구 복구 및 현행 담당 4세션 메시지 전송 성공 확인. 완료 답장 대상: LAST RAIL_Plan_6 / 01a0c3bf-96d7-7f73-9deb-756181adad80 / local. 전송 성공과 구현 완료는 별개이며 아래 최신 총괄 배정을 따른다.

<a id="homepage-maintenance-login-consumer-20261001"></a>
## 기존 H-03 점검 로그인 소비 단위 완료 — 2026-10-01 Homepage
[인계·변경 경로·계약·92/92 검증](../../QA/Homepage/HANDOFF.md#homepage-maintenance-login-consumer-20261001), [소스 pin/정확 검증 범위](../../QA/Homepage/maintenance-login-20261001.json). maintenance-login-v1 public status GET/DTO·로그인503 분류·기존 세션 보존·화면 없는 상태 Model 구현 완료. 확정 점검/정책조회 실패/일반503 구분, anonymous/no-store·취소/계정전환/늦은응답·명시 재조회 확인. 예정종료 자동해제/credential 자동재전송0, 문의 Model 재구현0.
실제 HTTPS/브라우저 cookie 검증 잔여이며 전체 최종시안 전 View/bootstrap 연결 보류. H-03 부모 미완료 유지, H-02 승인 공개자료·H-04 S-16 유지. H-MAINT/P-14 후속 AND 미충족·후속등록0, 별도 실행항목 등록이 없으므로 삭제0. 타담당 기존 항목 변경0·실접수/배포0.

<a id="homepage-support-socket-completed-20261001"></a>
H-03 추가 로컬 WEB HTTP 실행단위 완료: [기존 인계·33/33 실제HTTP·계약/변경·수명·잔여](../../QA/Homepage/HANDOFF.md#homepage-support-socket-20261001). 완료 블록만 정리, H03 부모·최종시안전View/bootstrap보류·HTTPS/브라우저·H02원source/H04 S16 잔여 유지. P14/H-MAINT AND 미충족·후속등록0. 기존92단위 반복재검사0.



<a id="h04-author-delete-intake-20261008"></a>
## 기존 H-04@community-v1 부분 계약 인수 — 2026-10-08 / 비화면 본인 삭제 명령만 착수 가능
고정 기존키 H-04@community-v1에 인수 범위만 추가, 새 큐 ID 아님. 선행 완료: 기존 WEB 세션/CSRF 코어 + Server S-16 본인 삭제 API/DB 단위43PASS/type/lint0. Server·WAIT#s16-author-delete-result-20261008와 Server docs/api/community-author-delete-v1.md / community.content-store.v1.20261008를 소비한다. POST /v1/web/community/contents/:contentId/delete, commandId/expectedRevision, WEB cookie·Origin·CSRF·DEV exact tester·X-Community-Content-Version, no-store; 원영수증 재생/변경 payload409/권한 회수·삭제 상태 손상 거절.
완료 조건: 기존 웹 client/controller의 비화면 typed 삭제 명령·receipt/error 매핑, 동일 in-flight 명령 재사용·새 키로 임의 재실행0·세션종료/계정변경 취소 및 늦은응답 배제·권한/CSRF/개정 경합 처리. 현재 공급은 본인 글·댓글 삭제만, 기존 H04 전체 화면/작성·읽기·신고·차단·운영검수 해제 아님. UI 화면 연결은 승인 시안 완료 후 별도 등록 정책 유지. 실제 오리진/브라우저/remote D1/상시DEV 환경은 미검증이며 그 미검증을 로컬 typed 계약 소비 전체 선행으로 삼지 않는다. 기존 부모/역사 블록 수정·삭제0, 실제 게시·배포0.

<a id="homepage-community-delete-20261008"></a>
기존 H-04 부분인수 중 본인 삭제 소비 구현·로컬 검증 완료: [인계/정확계약·66/66·범위·잔여](../../QA/Homepage/HANDOFF.md#homepage-community-delete-20261008). 이 소비단위 반복착수하지 않음. H04부모·Server기존인수/역사블록 유지(새별도실행ID없음);전체커뮤니티/View·실브라우저 미완료. 다음확정API/환경만인수,미충족후속등록0.

<a id="h04-author-edit-intake-20261008"></a>
## 기존 H-04@community-v1 부분 계약 인수 — 본인 수정 / 2026-10-08
기존 고정키 H-04@community-v1에 비화면 typed edit 소비 범위 추가, 새 큐ID/중복 항목 아님. 완료 선행: 기존 WEB 세션 core + Server 수정/제한Markdown/원저장 공급검증. Server#s16-author-edit-result-20261008 + #s16-author-edit-expiry-result-20261008, WAIT#s16-author-edit-result-20261008, canonical Server docs/api/community-author-edit-v1.md. 버전 community.content-edit.v1.20261008 / renderer community.markdown.v1.20261008. 최종7파일75PASS/type/lint0, QA community-edit-expiry-final-20261008.json SHA23a2e5593f0391799441b33d33a0acda3c8a57edf773206e89ba12ab88092f18.
POST /v1/web/community/contents/:contentId/edit, commandId/expectedRevision/title/markdown(댓글 title:null). WEB cookie/Origin/CSRF/DEV exact tester·X-Community-Content-Version edit버전, no-store. receipt는본문없는원명령 metadata이며 재전송 후 현재 공개상태를 덮는 근거가 아님. 본문POST10000/COMMENT1000·title100 Unicode; 유한128000byte edit전송. client sanitizedBody/actor/visibility 주입0. 실제 승인링크원천 미공급은 링크입력만거절, 일반텍스트/code수정가능; 새 링크기준/원본문조회 기능 발명0.
완료 조건: 기존 비화면 client/controller에 엄격한 request/receipt/error(400/403/404/409/503)·별도edit버전·in-flight 원키 재사용·계정변경/logout 취소·늦은응답배제·original receipt를최신state로승격하지않는 계약을 연결/검증. 삭제 API는 v1유지하되 타인 비공개/삭제 target404로정정(공개타인403), 별도1초TTL발명0/밀리초시각 권한fence회귀. 공유필수auth/명의원천재사용. 화면연결은승인시안후별도등록 유지, 실제URL/browser/remoteD1미검증을 로컬소비 전체대기로묶지않음. H-04전체/create/read/report/block/운영검수 공개해제아님. 기존타블록수정삭제0/실게시·배포0.

<a id="homepage-community-edit-20261008"></a>
H-04 기존부분인수의 본인수정 소비 구현·로컬검증 완료: [인계·정확계약/원receipt·122/122·잔여](../../QA/Homepage/HANDOFF.md#homepage-community-edit-20261008). 완료삭제/수정단위 반복착수0. 수정·삭제shared수명회귀포함,부모/타담당역사인수블록보존(별도실행ID없음). create/read/report/block/검수·실환경·최종시안/View잔여만대기,전체커뮤니티완료0·미충족후속선등록0.

<a id="h04-own-read-intake-20261008"></a>
## 기존 H-04@community-v1 부분 인수 — 본인 상세 조회 / 2026-10-08
새ID/중복블록생성0. WEB 세션core + Server#s16-own-read-result-20261008와 canonical11ec Server docs/api/community-own-content-read-v1.md 공급으로 비화면 typed GET/버전/404·409·503/rendererVersion·null본문 소비 착수가능. community.own-content-read.v1.20261008, GET /v1/web/community/own/contents/:contentId, WEB cookie/exact Origin/DEVtester/CSRF불필요/no-store. 완료조건: 기존계정전환·logout·언마운트취소/늦은응답배제 재사용, 성공본문은정확rendererVersion만 표시, private/deleted title/body null, 503legacy본문 우회표시0, 원 mutation receipt를최신상태로사용0, 로컬소비검증·기존인계기록. 실제DEVbrowser/URL미검증은비화면소비전체대기아님. create/list/publicread/report/block/검수 전체해제0, 화면연결시안후별도등록. 기존타블록수정삭제0/운영게시·배포0.


<a id="h04-comment-create-list-intake-20261008"></a>
## 기존 H-04@community-v1 부분 인수 — 댓글 생성·본인 목록 / 2026-10-08
기존WEBcore+Server#s16-comment-create-list-result-20261008 공급으로 비화면 계약소비 착수가능. 새실행ID/중복등록0. canonical11ec Server docs/api/community-comment-create-list-v1.md, community.comment-create.v1.20261008 POST /v1/web/community/contents/:postId/comments(JSONcommandId/markdown), community.own-comments-list.v1.20261008 GET /v1/web/community/own/comments(limit20/max50/cursor). WEB cookie/exactOrigin/DEVtester·writeCSRF·no-store. 완료조건: 기존 command in-flight/key+accountlogout취소 재사용, samekey동일원receipt·changedpayload409, 429retryAfter/Retry-After 표시·본문임의수정재전송0, 원receipt는currentstate아님, cursor불투명보관/계정전환폐기·private본문null·rendererVersion대조·로컬소비QA/기존인계. 119PASS0FAIL·type/lint0 근거재사용, 실URL/browser미검증은typed소비전체대기아님. POST작성/publicread/list/report/block/Admin검수전체해제0·시안후화면연결별도. 타블록수정삭제0/배포·실게시0.

<a id="homepage-community-comment-read-20261008"></a>
H04 本인댓글생성/목록及본인상세소비단위 완료: [인계·정확계약·139/139·잔여](../../QA/Homepage/HANDOFF.md#homepage-community-comment-read-20261008). h04-own-read-intake 및 h04-comment-create-list-intake의비화면소비범위완료,반복착수0. 별도실행ID없으므로부모/역사인수블록보존,전체커뮤니티/공개작성/읽기/검수·실환경·최종시안/View미완료. 다음확정공급없는임의작업/미충족후속선등록0.

<a id="h04-post-create-intake-20261008"></a>
## 기존 H-04@community-v1 부분인수 — POST작성 / 2026-10-08
새ID/중복등록0. WEBcore+Server#s16-post-create-result-20261008·API community-post-create-v1.md 공급으로 비화면typedPOST 착수가능. community.post-create.v1.20261008 POST /v1/web/community/posts, boardId free/qna/guide_build/feedback·title/markdown·선택spoiler/wikiRefs/sourceBalanceVersion(defaultfalse/[]/null). rawactor/visibility/renderedHTML주입0. WEBcookie/exactOrigin/CSRF/DEVtester/no-store, 원semanticcommand재생/429 Retry-After·wiki unavailablegeneric400/503·plaintext전체차단0. 완료조건: 기존명령in-flight/receipt/계정전환/logout취소 재사용, 원receipt최신본문표시0·ownGET로실제상태재조회, 고정wikirefID/버전보존·임의ID/이름생성0, 로컬소비QA·기존인계. 관련140PASS0FAIL/type/lint0 근거공급, realURL/browser/TCP미검증별도. ownPOSTlist/public읽기·report/block/Admin검수전체해제0·시안후화면연결별도등록. 타블록수정삭제0/실게시·배포0.

<a id="h04-own-post-list-intake-20261008"></a>
## 기존 H-04@community-v1 부분인수 — 본인 POST 목록 / 2026-10-08
WEBcore+Server#s16-own-post-list-result-20261008·community-own-post-list-v1.md 공급, 새ID/중복등록0. GET /v1/web/community/own/posts, X-Community-Own-Posts-Version community.own-posts-list.v1.20261008, limit20/max50·선택boardId·불투명cursor. WEB cookie/exactOrigin/DEVtester/no-store·CSRF불필요. 완료조건: 기존 account/logout/언마운트 취소·늦은결과배제 재사용, 계정/board 변경 cursor폐기, 실제creation DESC 순서·masked null본문/ref/source/spoiler·rendererVersion 대조, 400/404/409/503 오류소비·로컬QA/기존인계. 151PASS/type/lint0 서버 격리근거 공급, 실browser 별도. 생성API 약관쌍/provider 동의gate는 후속 정확버전 공급 전 미완료이므로 운영작성 완료 오인0. 공개read/list·차단/신고/검수 전체해제0·시안후화면별도등록·타블록수정삭제0·배포/게시0.

<a id="homepage-community-post-20261008"></a>
H04 POST작성 비화면소비단위완료: [정확계약/150검사/8핀·잔여](../../QA/Homepage/HANDOFF.md#homepage-community-post-20261008). 작성완료단위반복착수0·기존부모/역사intake유지. ownPOST목록은문서실재만확인,실행범위등록후인수;실약관/동의·public읽기/report/block/검수·실환경/전체최종시안/View잔여. 미충족후속선등록0·실게시/배포0.
<a id="homepage-community-own-posts-20261008"></a>
H04 本인POST목록typed소비완료: [정확계약/159검사/6핀·잔여](../../QA/Homepage/HANDOFF.md#homepage-community-own-posts-20261008). h04-own-post-list-intake 직접확인후소비·앞선POST작성블록의등록대기는현재완료로대체. 완료단위반복착수0/원기록보존. 기존H04부모/역사인수block유지、실동의/publicread/report/block/검수·실환경/최종시안/View미완료·미충족후속선등록0.
<a id="h04-personal-block-intake-20261008"></a>
## 기존 H-04@community-v1 부분인수 — 개인 차단 저장 / 2026-10-08
WEBcore+Server/WAIT#s16-personal-block-result-20261008·community-personal-block-v1.md 공급, 새ID0. POST/GET /v1/web/community/blocks / community.personal-block.v1.20261008 exactheader. typed command expected actor전체relationrevision/opaque publicAuthorId/blocked, 原 receipt현재상태아님·changedpayload409·self403/unknown404. GET20/max50/revision-boundcursor/no-store. 완료조건: 기존명령in-flight/logout/account세대·취소 재사용, 관계변경409 명시재조회/계정변경cursor폐기·原 receipt로로컬상태복원0·본인권한목록소비/로컬QA·기존인계. 관련166PASS/type/lint0. public필터未連結이므로콘텐츠가이미숨겨진것처럼최종UI완료주장0, 약관gate차단에확대0. 실제browser·화면시안후별도등록·타블록수정삭제0·운영/배포0.

<a id="homepage-community-block-20261008"></a>
H04 개인차단/해제typed명령及본인관계목록소비단위완료: [인계·정확계약/126관련검사/9핀·잔여](../../QA/Homepage/HANDOFF.md#homepage-community-block-20261008). 完了단위반복착수0·기존부모/Server역사intake유지(새실행ID0). publicreader필터/콘텐츠효과·report/검수·실환경/최종시안/View미완료、약관gate차단확대0·미충족후속선등록0.
<a id="h04-public-read-intake-20261008"></a>
## 기존 H-04@community-v1 부분인수 — 공개상세/목록·개인차단 / 2026-10-08
WEBcore+Server/WAIT#s16-public-read-result-20261008·community-public-read-v1.md 공급, 새ID0. community.public-read.v1.20261008 / X-Community-Public-Version; GET /v1/web/community/posts(limit20/max50/board/cursor), /contents/:id, /contents/:postId/comments(limit/cursor). PRD匿名·DEV exacttester·로그인viewer個人block·no-store/VaryCookieOrigin·CSRF不必要. 완료조건: 기존account/logout/request取消·늦은결과배제 재사용, publicrendererVersion exact검사·opaqueauthor/固定wikiID保全·原mutationreceipt본문復元0, 필터/계정/parent변경cursor폐기·oldcursor404/409현재재조회, 만료cookie를匿名으로 자동retry0·개인cache공유0·차단/해제후현재public 재조회, hidden/PENDING/deleted·blocked부모댓글generic404처리, typed로컬QA/기존인계.188PASS+세션11PASS/type/lint0 서버격리근거. 실제browser/URL/terms원본·동의gate는별도선행; read전체대기0. 시안후화면별도등록/A03未供給/타블록수정삭제0/배포게시0.

<a id="h04-consent-intake-20261008"></a>
## 기존 H-04@community-v1 부분인수 — exact 약관·수락·생성envelope / 2026-10-08
Server/WAIT#s16-consent-result-20261008·community-consent-v1.md 공급, 새ID0. community.consent.v1.20261008 X-Community-Consent-Version·GET /consent(authenticatedsource/current동의)·POST /consent/accept(strictcommandId+exactpolicy,CSRF). 추가필수 creation eligibilityheader community.creation-eligibility.v1.20261008; 原 body/hash/receipt保全. 완료조건 비화면 typedDTO/문서쌍ID·version·digest/currentrevision exact보관, no rawHTML문서실행·account/acceptedbool주입0·별도명시수락(작성자동실행0), 원receipt현재동의로restore0·current변경409재조회/503실source준비중/403명시동의 필요표현, 기존WEB logout/계정세대取消와원command再生 재사용·로컬소비QA·既存인계. read/edit/delete/block/report에동의gate확대0. 실제약관原文/version未提供/보존·탈퇴未確定 운영활성화대기 유지·가짜checkbox/원본/실화면생성0.204PASS/type/lint0 隔離근거·実browser別도·최종시안후별도등록·타블록수정삭제0·배포0.

<a id="homepage-community-public-read-20261008"></a>
H04 公開상세/목록·개인차단現在조회typed소비완료: [정확계약/138관련검사/8핀·잔여](../../QA/Homepage/HANDOFF.md#homepage-community-public-read-20261008). 完了단위反復착수0·既存부모/Server역사intake保全。實同意/report/운영검수·실환경/전체최종시안/View미완료、publicreader소비를실browser전체완료로승격0·미충족후속先등록0.

<a id="h04-report-intake-20261008"></a>
### 기존 H-04@community-v1 신고 비화면 부분 인수
Server/WAIT#s16-report-result-20261008 및 canonical11ec Server docs/api/community-report-intake-v1.md 기준 POST reports / GET own/reports/:caseId 계약 공급. 버전·정확WEB/CSRF·원receipt/동일미처리target·rolling60초3/24h20·429Retry-After·private증거비노출 typed client 및 오류/재시도 격리소비 착수가능. 집중24/전체268PASS/type/lint0, 위QA hash와실환경미검증범위원문참조. 완료조건 typed exactDTO/version/auth/privatefields0/400·401·403·404·409·429·503 및 원결과 재생·권한분리 소비 검증을기존인계기록. 화면연결시안후별도, Admin검수/실source/운영파기는별도未供給이며 이비화면접수소비전체차단사유아님. 同key중복생성/他기존블록수정삭제0·운영쓰기/배포0.

<a id="homepage-community-consent-20261008"></a>
H04 약관·수락·생성envelope typed 비화면소비완료: [계약/8핀/전체194·신규13검사/잔여](../../QA/Homepage/HANDOFF.md#homepage-community-consent-20261008). h04-consent-intake-20261008의 이 단위 반복착수0; 기존Server intake/부모/타담당 역사 보존. 실제약관/운영보존/탈퇴·실환경·신고검수·최종시안/View 미완료, 부모삭제/미충족후속선등록0.

<a id="homepage-community-report-20261008"></a>
H04 h04-report-intake-20261008 非화면typed소비완료: [8핀/전체209·신규15/잔여](../../QA/Homepage/HANDOFF.md#homepage-community-report-20261008). 본단위반복착수0·기존Serverintake/부모/타담당역사保全. Admin검수/실환경·통지파기/backup탈퇴·최종시안/View미완료, 부모삭제/미충족후속등록0.


<a id="h04-update-time-intake-20261008"></a>
### 기존 H-04@community-v1 updatedAt 비화면 부분인수
Server/WAIT#s16-update-time-result-20261008 및既存API #community-updated-at-v1-20261008 기준. existingreadversion/header유지·additive updatedAt number|null strictDTO수용. VISIBLE actualrevision시각/생성=createdAt·원receipt후최신시각불변, private null은노출범위제한/source부재단정0, visible source미비503·stale409·createdAt정렬/커서불변. 非화면typed parser·오류/정렬/원재생소비검증후기존인계 기록이완료조건. 9집중/277전체PASS/type/lint0 및 정확QA/hash상기근거, actualbrowser未検証. 시안후화면별도·새descriptor수정/실운영/파기供給아님·기존key중복생성/타블록수정삭제0.

<a id="homepage-community-updated-at-20261008"></a>
H04 h04-update-time-intake-20261008 additive非화면소비완료: [6핀/전체216·신규7/미완료](../../QA/Homepage/HANDOFF.md#homepage-community-updated-at-20261008). 本단위반복착수0·기존Serverintake/부모/타담당역사保全. actualbrowser/시안/View·descriptorwriter·운영검수파기未完, 부모삭제/미충족후속先등록0.


<a id="h04-local-http-intake-20261008"></a>
### 기존 H-04@community-v1 — 비화면 실제 TCP 소비 착수 가능
Server/WAIT#s16-local-http-result-20261008 및 canonical11ec Server docs/api/community-loopback-http-v1.md exact재시작/비밀값0/lease/정상로그인/원contract/source핀인수. 별도mock API생성0, 실제Homepage WebApi를 명시Node시험transport→loopbackHTTP real게임app으로소비. 기존script import/cookieRAM/정확Origin/CSRF/versionCode123 사용, no automaticconsent;syntheticsource명시수락. 실행완료조건 소비측typed Model/실fetch 정상·원receipt·권한/오류·abort/logout/latepayload/실connection중단 검증과 기존인계/QA에 실제TCP·Nodeadapter/browser未検証 구분. 起動者listener수명소유·consumerjarclear/종료, 전체화면/HTTPS/CORS 미승격·시안후화면別도. 33Server actualTCP/4lifecycle/lint0 근거위링크, 마지막QAport已閉·영구URL아님·운영쓰기/배포0. 기존key部分인수·중복ID/타既存블록변경0.

<a id="homepage-community-loopback-20261008"></a>
H04 h04-local-http-intake-20261008 非화면actualTCP소비완료: [55check/70요청·cleanup전부true/잔여](../../QA/Homepage/HANDOFF.md#homepage-community-loopback-20261008). 本단위반복착수0·기존Serverintake/부모/타담당역사保全. 실제browserTLS/CORS/remote·시안/View·운영검수/파기/descriptor別도 未完, 부모삭제/미충족후속先등록0.

<a id="h04-moderation-effects-intake-20261008"></a>
## 기존 H-04@community-v1 운영검수 효과 비화면 부분 인수 — Server / 2026-10-08
Server/WAIT#s16-moderation-api-result-20261008·canonical Server docs/api/community-moderation-v1.md·347PASS/type/lint0. 新Admin community.moderation.v1.20261008はHomepage公開権限아님/WEB로呼出0. 기존community.public-read.v1.20261008 및own-report-read/updatedAt DTO·원접수receipt/bodyhash/createdAt·cursor순서변경0. 실제Admin terminal status변경후own report는 PROCESSED만 노출(내부ACTIONED/DISMISSED·사유/증거0), 原submitreceipt는原RECEIVED로재생. HIDDEN공개detail404·list제외·children부모차단; 적격RESTORE후현재VISIBLE/currentrevision 및검증된moderation proof시각updatedAt供給, 개인block仍適用. 비공개own body/time mask 유지.
착수가능: 기존 typed비화면read consumer에 신고원receipt/현재결과구분·404/list재조회·restore currentrevision/updatedAt표시데이터·개인block유지/허위낙관복원0 QA를 추가. 새DTO/version/UI재작성0, Admin전용권한정보/사적본문을Homepage에추가0. 0118는updatedreader전schema필수·운영未適用/상시URL없음·actualbrowser/Access/remote검증別도. 기존격리handler자료再用가능, 소비자browser를서버전체완료선행으로묶지않음. 화면시안후별도·운영게시/실신고·배포0; 기존원블록보존/중복ID0.
<a id="homepage-community-moderation-effects-20261009"></a>
H04 h04-moderation-effects-intake-20261008 非화면typed 검수효과소비완료: [40handler검사/정확원receipt·숨김복원/개인block/잔여](../../QA/Homepage/HANDOFF.md#homepage-community-moderation-effects-20261009). 本단위반복착수0·既存Serverintake/부모/타담당역사保全. actualbrowserAccess/remote·최종시안View·운영통지실파기/descriptor別도未完, 부모삭제/미충족후속등록0.

<a id="h04-comment-lock-intake-20261009"></a>
## H-04@community-v1 — 실제 댓글잠금 효과 비화면 부분 인수
추가producer community.comment-lock.v1.20261009. 선행 AND: 승인10#community-comment-lock-approved-20261009 + 실제S16댓글writer/원문proof/API/QA완료 + 기존H04 public/own/create/검수효과소비완료. 근거 Server/WAIT#s16-comment-lock-writer-result-20261009 / QA368PASS·type/lint0 / canonical Server docs/api/community-comment-lock-v1.md. 기존H04등록키 계약addendum·완료검수부분반복/신규전체View ID0.
범위: 기존public/own DTO/version/cursor/createdAt순서 그대로 currentcommentLocked/revision/updatedAt 공급·새댓글locked409가quota/receipt를소비하지않음. 기존본문/댓글조회·본인삭제·신고 유지, hidden부모잠금해제후에도공개404, AUTHOR_DELETED복구불가. 원댓글/잠금receipt는현재truth가아니므로현재조회, 완료생성원receipt재생유지. unlock후실WEB cookie/CSRF/exacttester/현재약관/계정/부모공개/개인차단/rate 원조건보존. 공개웹에Admin댓글잠금명령/자격증명추가0.
완료조건: 비화면typed현재flag/새댓글409·원receipt회복·oldread/delete/report·unlock원자격·기존숨김효과와독립성을검증하고QA/Homepage/HANDOFF에기록. 서버전체S16/최종시안은이비화면소비선행이아니며시안후화면연결정책유지. actualAccess/browser/TLS/remoteD1/상시DEV URL未검증, operating0119미적용(업데이트reader전필수), 실게시/운영DB/배포/View0. 다른기존블록수정삭제0.

<a id="homepage-community-comment-lock-effects-20261010"></a>
H04 h04-comment-lock-intake-20261009 非화면실제handler효과소비완료: [44검사/신규쿼터0·currenttruth/해제자격/잔여](../../QA/Homepage/HANDOFF.md#homepage-community-comment-lock-effects-20261010). 본단위반복착수0·기존Serverintake/부모/타담당블록保全. actualbrowserAccess/remote·시안View/운영통지파기·descriptor제재未完, 부모삭제/미충족후속등록0.

### ALL-SCREENS-LANDSCAPE:HOMEPAGE:20261023
사용자전체화면가로전환및담당충돌병합지시. 33#all-screens-landscape-approved-20261023 최신정책을읽고자기담당실제원본/화면/계약/제작프롬프트와대조하여수정·병합·검수. 이전전투한정예외대체. 선행완료된정책기반 독립조사/공통수정부터실행,미공급자산·계약전체연결은선행충족후. 공통파일은소유담당1곳,구원본보존/통째덮어쓰기0. 기존QA/인계에파일별근거·충돌·부분완료기록. 가로전투실제품연결·기존데이터재사용/권위/복원/API·메모리검수계속. 외부웹은반응형유지하며게임가로미리보기/연계범위점검. 무관한배포/경제행위권한추가0.
