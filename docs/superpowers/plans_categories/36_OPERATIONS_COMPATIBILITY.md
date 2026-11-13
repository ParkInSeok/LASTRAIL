# 운영 상세 계약 보존 보충

이 보충은 현행 번호 문서와 함께 사용하는 운영 상세 계약 안내다. 아래 링크는 그 폴더를 기준으로 한다. 최신 사용자 결정과 번호별 본문이 정책을 소유하며, 이 보충은 생략하면 안 되는 기존 계약과 이행 경계를 연결한다. 원문 보존은 과거 구현/배포/테스트 결과를 현재 완료로 인정하는 뜻이 아니다.

## 릴리스·DB·API 순서

앱 설치 versionCode, 서버 release version/실제 Worker deployment id, DB migration/data, 콘텐츠 release는 서로 다른 축이다. 호환 스키마 추가 → 구·신 데이터 모두 읽는 서버 → 새 앱 배포 → 실제 서버 minimumVersionCode gate 활성 및 구클라이언트 차단 검증 → 비호환 동작 순서를 유지한다. 파괴적 정리는 별도 승인 단위다. D1은 기본적으로 forward migration/감사된 correction으로 복구한다. Worker rollback은 현재 스키마·enum·원장·idempotency·포인터를 구 Worker가 읽을 수 있을 때만 허용하고 minimumVersionCode를 자동으로 내리지 않는다. Admin 버전 저장은 배포나 DB rollback이 아니다.

서버 버전은 환경/서비스에 고유해야 하며 검증 source의 release branch와 서비스별 불변 tag를 보존한다. 배포+smoke 성공 뒤 불변 성공 이력(`If-None-Match:*`)과 현재 포인터(CAS)를 기록한다. 실패 시도는 별도 기록이며 current가 아니다. 실제 배포가 성공했는데 이력/포인터 기록이 실패하면 degraded evidence로 조정하며 자동 재배포하지 않는다. `ROLLBACK_COMPATIBLE`은 `rollbackVerified=true`, `MIN_VERSION_GATE_REQUIRED`는 하한 코드가 필요하다.

상세: [release DB/API compatibility](50_RELEASE_OPERATIONS_CONTRACTS.md#source-7e2cb4ac27ccff9b), [server version log](49_SERVER_API_CONTRACTS.md#source-253ca40b108e58e8).

## 정확한 설치 빌드 admission과 콘텐츠 핀

레지스트리의 정확한 설치 build-code 조회는08의 밸런스 기준점 이하 선택 알고리즘과 별도다. 표시 버전·폴더명·업데이트 안내만으로 서버 admission을 대신하지 않는다. 레지스트리 revision은 전체 quoted ETag이며 누락 object의 null과 구분한다. stale409를 자동으로 새 revision 저장으로 바꾸지 않는다. `artifactIdentity`는 등록 metadata hash이고 실제 APK/AAB byte 검증 증거와 동일하지 않다. legacy/exact 전환은 capability와 실제 배포 증거를 확인한다.

인증된 release 승인 후 실제 catalog/bundle hash와 의존성을 확인하고 다운로드 후 다시 승인한다. API Bearer를 공개 asset host에 보내지 않는다. 게임 요청의 `X-LastRail-Content-Release-Id`와 세션 release 결속을 유지한다. 구 `STABLE/INTERNAL/PRD` enum은08의 새 환경 정책으로 이행할 대상이며 wire/store literal을 이름만 바꿔 호환됐다고 보지 않는다. 보관 중인 release/pointer/tombstone/audit chain도 임의 소거하지 않는다.

상세: [client release registry](49_SERVER_API_CONTRACTS.md#source-b941b10631564c0a), [admission content release](49_SERVER_API_CONTRACTS.md#source-9362db4fe2c26c1d), [minimum version](49_SERVER_API_CONTRACTS.md#source-7dd0ce78b70ebd9f), [signed builder publication](50_RELEASE_OPERATIONS_CONTRACTS.md#source-d251ceee4d8ef83c).

## 진단·이력 metadata

START 시점과 최초 accepted FINISH 시점 버전 snapshot은 별도로 저장한다. start-A/finish-B/retry-C에서 retry는 저장된 B를 복원하며 현재 C로 역사 값을 덮어쓰지 않는다. legacy `runVersionSnapshot` alias는 start snapshot이며, 모르는 과거 버전은 null로 둔다. 공개 확률표와 내부 RNG/seed/weight 비공개는 서로 다른 projection이다.

클라이언트 오류는 실제 인증된 현재 계정 세션에 귀속 가능한 사건만 수집한다. prelogin/이전 계정/출처 불명 crash buffer를 업로드 때 현재 계정으로 붙이지 않는다. strict event allowlist와25events/60occurrences 상한, 새 batch12회/10분, replay-before-rate,429/Retry-After600, body/query 없는 receipt lookup을 보존한다. 로깅 실패는 원래 게임 응답을 실패로 바꾸지 않는다. Admin action 감사와 runtime 오류 log는 분리한다. 원문의 환경별/저장소별 TTL 및 severity별 critical180/error90/warning30/info14 후보는15의 확정 종류별 보존을 대체하지 않으며 실제 삭제/lifecycle 실행 권한이 아니다.

상세: [run result submit](49_SERVER_API_CONTRACTS.md#source-11d4e6e3b3c22eef), [ops error logs](49_SERVER_API_CONTRACTS.md#source-59ff0027b0008512), [tracing](49_SERVER_API_CONTRACTS.md#source-4a9273893fe00b6c), [severity retention historical proposal](50_RELEASE_OPERATIONS_CONTRACTS.md#source-23ad5b6c04b82589).

## Admin 조회의 의미·권한

유효 refresh session 수를 동접자/DAU로 표시하지 않는다. `lastLoginAt=max(auth_sessions.created_at)`는 회전으로 생성된 세션도 포함하며 실제 마지막 gameplay가 아니다. activity 원천이 없으면 lastSeen/concurrency/returning 지표는 null/UNAVAILABLE이며0이 아니다. KST calendar 집계와04시 service-day는 목적에 따라 구분한다. 광고 grant는 view나 매출이 아니다.

S01 account-state는 단일 SQLite SELECT snapshot이며 mutationSequence는 모든 API에 적용되는 ETag가 아니다. Admin player activities는 `INDEPENDENT_LIVE_READS`이므로 전체 동시 snapshot으로 표현하지 않는다. 조회로 sync/claim/grant를 수행하지 않고 실제 available0과 UNCONFIGURED/UNAVAILABLE을 구분한다. `SUPPORT_READ` 권한으로 상세 `OPS_ERROR_LOG_READ`를 추론하지 않는다. OWNER/OPERATOR의 오류 list/detail과 VIEWER 거부, 안전 요약 projection의 별도 계약을 보존한다. 현재 문의 답변/상태/내부메모 정책은03을 따르며 경제 repair 권한을 추가하지 않는다.

상세: [session metrics](49_SERVER_API_CONTRACTS.md#source-191ef8c5ab1f7fa5), [Admin API](49_SERVER_API_CONTRACTS.md#source-572d5a712dea75c9), [player activities](49_SERVER_API_CONTRACTS.md#source-33dcea8f8cbca85a), [account state](49_SERVER_API_CONTRACTS.md#source-e190f6d9758e8e32).

## identity·직렬화 이행

`accountId`, credential `loginId`, 표시 `displayName`, recovery email은 다른 identity다. email 일치로 계정 merge/switch/recovery를 승인하지 않는다. credential 검증 뒤 안전한 conflict preview를 표시하고 명시 confirm 후 새 session을 durable 저장한다. timeout/cancel/저장 실패를 새 guest 생성 사유로 삼지 않는다. 기존 guest/refresh replay key와 원래 payload를 유지한다. 비밀번호 reset start는 계정 존재를 숨기고 confirm은 자동 로그인하지 않는다.

`act_01`, `act_01_wasteland_railway`, mission/region/operation/encounter, artist별 character 이름, local ability 키와 executable ability ID, `ENV_PRD`와 새 `ENV_PROD`는 의미·원천을 대조한 mapping 없이 일괄 치환하지 않는다. 특히 구15wave/단일 leader/시작 package는 현재 전투 정책으로 재해석하지 말고 schema/policy/checkpoint별 구·신 분기를 유지한다. 새 per-operator .002/.003은 shared leaderCharge 복제가 아니며 다른 checkpoint를 거부한다. 상세 원문 내 closed union·nullable 필드·UTF8/canonical JSON/hash·signed64·duplicate key 거부를 단순 요약으로 대체하지 않는다.

상세: [auth linking](49_SERVER_API_CONTRACTS.md#source-6f64a90df51fe75e), [story progress](49_SERVER_API_CONTRACTS.md#source-378a824352e36d1b), [local producer](49_SERVER_API_CONTRACTS.md#source-e9b5514971aaad2e), [per-operator ultimates](49_SERVER_API_CONTRACTS.md#source-a9952f103a593941).
