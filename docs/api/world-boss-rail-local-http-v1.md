# World boss rail isolated local HTTP v1

기존 S-WB-RAIL-01 / 실행 단위 S-WB-RAIL-HTTP-LOCAL-01. Transport `world-boss.rail-local-http.v1.20261002`; command/snapshot `world-boss.rail-action.v3.20261002`. 이 문서는 운영 API 등록 또는 배포 승인 문서가 아니다.

## 사용 가능한 환경과 경계

Canonical Server: D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server. `apps/game-api/src/world-boss-rail-local-http.ts`의 createWorldBossRailLocalHttp()가 반환하는 격리 Hono app에서만 사용한다. 실제 game index.ts에 등록되지 않았다. LOCAL ENVIRONMENT, LOCAL_WORLD_RAIL_PROFILE=LOCAL_TEST_PROFILE, HTTP loopback hostname(localhost/127.0.0.1/[::1])을 모두 요구한다. 최초 계획의 DEV 조건보다 좁힌 LOCAL만 허용하며 DEV/PRD는404로 닫는다. Origin이 있으면 역시 HTTP loopback이어야 한다. 브라우저 쿠키·LOCAL_HEADER·관리자 자격증명은 인증수단이 아니다.

실행 검증 환경은 actual Hono Fetch handler + 실제 HMAC 서명·DB ACTIVE session/account 검사 + SQL.js SQLite/D1 adapter transaction이다. 정상 game guest 로그인으로 발급된 실제 로컬 token의 인수도 테스트했다. 모킹된 authenticate callback을 받지 않는다. 별도 리스닝 서비스/소켓·실DEV URL·운영DB 연결은 제공/검증하지 않았다. 아래 URL은 factory의 loopback 요청 경로 계약이며 현재 상주 서버가 있다는 뜻이 아니다.

기존 resolveAuthContext export를 그대로 사용한다. Authorization: Bearer <정상 game access token>. token 서명/만료/DB session ACTIVE·만료/account ACTIVE를 매요청 검사하고 원key 재생에도 우회하지 않는다. 지원 session은 기존 auth_sessions 원본이며 복제한 독립 인증 소유자가 없다. trial 역할/참여자는 서버 저장 WorldBossRailAttempt.definition의 trainAccountId/operatorAccountId를 따르고 client account/role 입력은 없다.

## 요청과 응답

공통 필수 header: `X-World-Rail-Version: world-boss.rail-action.v3.20261002`, `X-World-Content-Version: <저장 정의의 contentVersion>`, Authorization. 버전/내용 불일치409. 모든 성공·오류 응답은 Cache-Control:no-store 및 X-World-Rail-Transport:world-boss.rail-local-http.v1.20261002. 자동재시도 없음.

GET `/_local/world-boss/attempts/:attemptId/rail`: body/query 없음. 참여자만 조회. 응답 `{transportVersion,version,status:'LOCAL_TEST_PROFILE',ownerRevision,authoritativeAtMs,frameValidUntilMs,snapshot}`. snapshot은 원v3 상태/정의/원receipt이며 GET이 time advance/경로선택/회차발행을 실행하지 않는다. frame이 오래돼도 마지막 원본 조회/receipt 복원은 가능하고 신선하다고 표시하지 않는다. 실제 geometry 상태가 결속됐다는 표시도 추가하지 않는다.

POST `/_local/world-boss/attempts/:attemptId/rail/commands`: Content-Type:application/json, `Idempotency-Key` 필수(printable ASCII1..128자), 이 값은 body.commandId와 같아야 한다. query 없음. body는 원 v3 strict DTO:

```json
{"version":"world-boss.rail-action.v3.20261002","attemptId":"trial","contentVersion":"LOCAL_GEO","roundId":"p1r1","commandId":"route","expectedRevision":1,"action":{"kind":"CONFIRM_ROUTE","side":"RIGHT"}}
```

다른 action은 `{"kind":"INTERNAL","actionId":"<해당 정의의 선택>"}`. CONFIRM_ROUTE는 train 역할, INTERNAL은 operator 역할 및 기존 회차/window 조건을 적용한다. 서버 Date.now()만 명령 시각이며 movement/availability/definition/snapshot/now/role 등 추가필드는400. 몸체18000 byte 초과/깨진JSON/다른Content-Type도400. attemptId는 path와, contentVersion은 header와 일치해야 한다.

성공 `{accepted:true,transportVersion,version,status:'LOCAL_TEST_PROFILE',receipt:{commandId,roundId,acceptedRevision,acceptedAt,route,internalAction}}`. 새로운 명령은 expectedRevision과 현재 snapshot.revision이 같아야 한다. 같은 account+commandId+원body(원expectedRevision 포함)는 이전 receipt를 먼저 재생하므로 이후 window/frame/attempt 종료·현재revision을 이유로 원receipt를 새로 만들거나 거절하지 않는다. 현재 token/session/참여권한 검사는 여전히 선행한다. 같은key 다른body409, header/bodykey불일치400. 응답 receipt만 원본이며 최신state는 GET으로별도조회한다.

## 원자 저장과 단일 상태 소유자

`apps/game-api/local/world-boss-rail.sql`은 isolated local DB에만 설치하는 schema이며 packages/db/migrations 또는 frozen DEV51 이관에 추가하지 않았다. world_boss_local_rail_attempts에는 profile_status=LOCAL_TEST_PROFILE, owner_revision, rail_json, frame_json이 있다. WorldBossRailAttempt가 도메인 상태/규칙의 단일 소유자이며 repository는 restore/advance/command의 유효 변경 경로만 호출하고 그 snapshot을 저장한다. 원 command digest/receipt/state가 같은 rail_json row에 들어간다. transaction guard는 owner_revision+원rail/frame bytes를 CAS하고 상태·receipt 함께 commit하거나 함께 rollback한다. 동시 원동일key는 저장 원receipt 재조회로 수렴, 다른key/동일revision은 하나만commit. commit 응답유실 시 저장원본 확인, DB실패를 가짜성공으로대체하지않는다. SQLite export/reopen 이후 원receipt 재생을 실행검증했다.

Server 내부 trusted local owner만 provision(attemptId,definition,frame)/publishFrame(attemptId,expectedOwnerRevision,frame)를 호출한다. 공개 provision/start/frame/movement/snapshot upload route는 없다. frame={observedAtMs,validUntilMs,availability}; strict검사/시각단조/기존180000ms 종료 상한을 지키며 owner가 기존 WorldBossRailAttempt.advance에 제공한다. 이는 명시된 LOCAL 시험 owner 입력이고 실제 game health/connection owner와 실시간 연결됐다는 주장이 아니다. HTTP는 저장된 trusted frame만 읽고 신선한 지원 자료가 없으면503. client heartbeat/availability를 권위자료로 승격하지 않는다. 실제 이동/연결/health 원소유자 adapter는 다음 실제 통합단위에서 인수해야 하며 이 local facade가 독립 게임 진실로 사용되면 안 된다.

## 오류와 소비 기준

- 400: contract_violation/idempotency_key_required/rail_command_key_mismatch/rail_content_version_required.
- 401: 기존 auth_required/access_token_invalid/auth_session_invalid. 재생 요청도 동일.
- 403: account_not_active/rail_attempt_forbidden/rail_role_forbidden/rail_local_origin_forbidden.
- 404: rail_local_transport_disabled/rail_local_attempt_not_found; public start 등 미등록route.
- 409: rail_version_conflict/rail_definition_conflict/rail_revision_conflict/rail_command_key_conflict/rail_local_state_conflict/rail_attempt_ended/rail_round_not_open/rail_round_not_found/rail_window_closed/rail_route_already_confirmed/rail_internal_already_accepted/rail_actor_unavailable.
- 503: rail_authority_frame_unavailable/rail_local_state_invalid/rail_local_temporarily_unavailable. 기존auth secret미설정은 기존500 auth_not_configured 계약을 유지한다. 실제SQL/token/개인자료는오류body에노출하지않는다.

새 명령 window는 원v3 snapshot 기준이며 자동선택/카운트다운 연장/클라이언트상태로취소없음. stale revision/state conflict이면 GET 후 별도 사용자명령을 만들고, 응답유실은 같은key/body만 재시도한다. 401/403은 인증/권한 복구가 필요하다. 503은 원owner/DB 경계가 복구될 때까지 실패로 유지하고 새token/대체권한으로우회하지않는다.

QA 원본: QA/CommanderS1-20260922/world-boss-rail-local-http-final-20261002.json, world-boss-rail-local-http-fixtures-20261002.json(실제9요청/원receipt1), world-boss-rail-local-http-validation-20261002.json. fixtures에는 bearer/token/secret가 없다. 전체69실패 중 대표42이관 auth fixture의 maintenance_policy_unavailable을 실제재현하고 latest schema+명시 TEST_ONLY policy/정상 guest login→local bearer read를검증했다. 이를 전체실패가무관하다는추정으로사용하지않는다.

다음 순서: 원receipt+권위 geometry sample/event/impact를 같은 revision/journal에 원자 고정하고 재생하는 실행단위 → 실제 승인된 supported defense/shield/HP owner adapter → 해당버전 소비 검증. geometry7핀/v3/78/73/C3/구snapshot·고정DEV후보는 보존한다. 실worldSTART/입장/보상/원격배포 및 운영수치 승격은없다. View/bootstrap은 전체최종시안 조건충족후 별도등록한다. 이 isolated 계약 제출만으로 모든 실서버 소비 선행을해제하지않는다.

구현 참고: transaction batch의 실패 시 rollback 경계는 [Cloudflare D1 공식 문서](https://developers.cloudflare.com/d1/worker-api/d1-database/)로 확인하고 실제SQLite rollback으로검증했다. 요청별 상태/비밀값 관리에는 [Workers 공식 구현 규칙](https://developers.cloudflare.com/workers/best-practices/workers-best-practices/)을 적용했다.
## 최종 검증 상태 — 2026-10-02

신규 HTTP29+기존 maintenance17+world/geometry/combat102=148/148, game tsc/target eslint PASS. 실제9요청 fixture 저장 확인. 전체검사는2184중2112PASS/72FAIL(exit1)이며 앞선69는기준점이다. CE03 실행기/리포트 실패7은 범위를좁힌39검사에서39/39로재현되지않았고 전체실행 조건의원인은미확정으로보존한다. 대표옛42이관DB의 로그인503만 실제 maintenance_policy_unavailable로분류했으며 나머지실패전체를같은원인/HTTP무관으로단정하지않는다. 전체 실패케이스와 baseline차이는 최종 manifest에보존한다. geometry7핀/기존v3/DEV6+51 hash불변, 실제remote/START/reward0.