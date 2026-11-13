# LAST RAIL | 공통 밸런스 배포·버전 선택 계약

상태: **기획 계약 확정 / 현행 환경·채널 재정리 2026-09-21** — 이 세션의 최신 사용자 결정 우선. 구현·DB 마이그레이션·운영 활성화·실기기 검증은 수행하지 않았다. 아래 이름은 확정할 논리 계약의 명칭이며, 실제 기존 API 경로/DTO/DB와의 매핑은 구현 시 소스를 확인한다. 같은 기능의 서버 owner를 새로 중복 생성하지 않는다.

담당 범위: 채널별 배포 연결, 버전 선택, 테스트 활성화, 운영 승격, 롤백, 캐시/작전 고정, 영구 내실 호환성. 콘텐츠 파일의 다운로드·Addressables·로그인 전 승인 자체는 [02](02_CONTENT_UPDATE.md), 원요청 저널은 [04](04_COMBAT_RUN.md), 지휘관 내실은 [09](09_COMMANDER_PROGRESSION.md)가 소유한다.

<a id="current-stage"></a>
<a id="current-environment"></a>
## 0. 현재 환경 정책 — DEV 단일 비운영 환경, STG 폐기

**U9 사용자 확정(2026-09-21): STG/STAGE 환경 자체를 없애고, 그 환경에서 하려던 통합·실기기·내부테스트·출시 전 검증을 DEV에서 전부 수행한다.** U6의 DEV → STG 진행 범위는 이 결정으로 대체한다. STG를 DEV라는 이름으로만 바꾼 별도 서버·DB·도메인·빌드 프로필로 남기지 않는다.

현재 사용하는 비운영 환경은 DEV 하나다. 향후 공개 데모/정식 서비스의 PROD는 분리해 보존하며 현재의 실제 운영 활성화·인프라 폐쇄·데이터 삭제를 승인한 것으로 해석하지 않는다. 공개 출시의 실행은 별도 승인한다. 기존 링크를 위해 `current-stage` 앵커만 남겼으며 실행 환경 STG를 의미하지 않는다.

### 0.1 빌드·서버·데이터 연결

| 빌드/범위 | 서버 환경 | 데이터 채널 | 담당 범위 |
|---|---|---|---|
| DEV | DEV | 서버 승인 INTERNAL | 기능 개발, 통합, 실기기, 내부테스트, 회귀, 출시 후보 검증 전부 |
| 향후 운영 | PROD | PRODUCTION | 광고 없는 공개 데모·정식 운영, 별도 출시 승인 |

Unity 환경 심볼은 `LASTRAIL_ENV_DEV`와 향후 `LASTRAIL_ENV_PROD`를 사용하도록 정리한다. `LASTRAIL_ENV_STAGE`/`LASTRAIL_ENV_STG`와 STG용 부트·Addressables·CI/CD·배포 목록은 현행 구성에서 제거할 대상이다. 실제 코드/설정 존재 여부를 확인하고 변경하며, 이번 문서 편집은 코드나 원격 자원의 삭제가 아니다. 여러 환경 심볼 또는 환경 부재는 검증 실패로 드러내고 DEV/PROD로 조용히 추정하지 않는다.

일반 사용자용 환경 선택 메뉴는 만들지 않는다. 빌드 환경은 접속 설정이지 권한 증명이 아니다. 서버가 실제 앱·설치 버전·계정/테스트 접근을 검사하고 DEV/INTERNAL 문맥을 승인한다. 승인 순서는 기존 로그인 전 기본 admission → 인증 → 필요한 접근 권한 → 최종 데이터 선택을 유지한다.

LOCAL_TEST fixture, 빌드 산출물의 staging 임시 폴더, 전투 reinforcement stage는 배포 환경 STG가 아니며 제거 대상이 아니다. 과거 원문·QA·DB 이력의 STAGE 표기를 DEV로 재기록하지 않는다. 과거 STG 자원/저장 요청이 실제 존재하면 의존성·미확정 거래·보존 의무를 확인하는 별도 정리 작업이 필요하며, STG 토큰·계정·receipt·run을 DEV 것으로 재귀속하거나 자동 이전하지 않는다.

### 0.2 DEV 하나에서 개발과 검증을 함께 하는 방법 — 구체 연결안

**구형 계약과의 차이:** 기존 client-release-registry 및 content-release-channel-policy의 DEV STABLE/no-tester, PRD INTERNAL→STABLE 정책은 §0.1로 대체한다. distribution/status enum과 신규 데이터 channel은 같은 필드가 아니다. 서버/Admin/Unity의 admission·capabilities·tester·빌드 등록을 함께 매핑하기 전 구현 완료로 보지 않는다. 과거 DEV INTERNAL 거부 테스트는 현행 인수 기준이 아니다. 기존 PRD 리소스 식별자와 `LASTRAIL_ENV_PRD`는 실제 코드/인프라 매핑 없이 PROD로 일괄 개명하지 않는다. [기존 채널 원문](50_RELEASE_OPERATIONS_CONTRACTS.md#source-0c064bbc63da0cf7)을 호환 근거로 보존한다.

서버 환경을 늘리는 대신 데이터 작업 단계를 분리한다. **Draft는 편집 중 데이터, 발행 원본은 불변 데이터, active는 현재 검증에 사용하는 승인 연결**이다. Draft 저장이 active를 자동 변경하지 않으며 검증 중인 버전/소스/빌드/콘텐츠/해시를 명시적으로 고정한다.

권장 순서: `DEV에서 Draft 작성 → 검증 → 불변 버전 발행 → DEV 배포 미리보기 → expectedReleaseRevision으로 활성화 → 그 정확한 앱·데이터 조합으로 통합/실기기/회귀 검증 → 근거와 승인 상태 기록`.

하나의 기준점에 두 데이터를 동시에 일반 활성화하지 않는다. 서로 다른 versionCode의 호환 기준점은 기존 선택 규칙을 쓰되, 같은 코드에 여러 후보를 비교하는 작업은 고정 fixture 또는 명시 승인된 검증 입력으로 처리한다. 새 STG/QA 환경이나 숨은 채널을 자동 추가하지 않는다. 검증 중 코드/DB 변경으로 증거가 무효화될 수 있으므로 후보 검증 구간의 변경을 조율하고 소스·스키마·콘텐츠 fingerprint를 기록한다. 필요한 준비가 안 되면 검증을 재실행하며 환경 통합을 이유로 검증을 생략하지 않는다.

테스트 재화·작전·로그는 DEV에만 남긴다. 유저의 진행 상태를 지우는 테스트는 별도 격리 fixture·명시 테스트 계정에서 수행하고, 같은 DEV의 다른 검증자를 방해하는 전체 DB 초기화를 기본 절차로 삼지 않는다.

### 0.3 선택·승격·롤백

DEV/INTERNAL 활성 목록에서 호환성을 만족하는 `applyFromVersionCode <= installedVersionCode` 중 최대 기준점 하나를 선택한다. 표마다 별도 기준점을 섞지 않는다. 계정/캐시/저널은 실제 환경에 결속되며 DEV에서 PROD로 fallback하지 않는다.

**DEV → STG 승격 단계는 삭제한다.** 현재는 DEV 내부에서 후보 발행·활성화·검증만 수행한다. 향후에는 DEV에서 검증한 정확한 불변 원본·체크섬·공개 snapshot을 명시해 PROD에 직접 승격한다. 동일 데이터 승격과 앱 바이너리 승격은 다르므로 DEV 주소가 고정된 앱을 운영 배포하지 않는다. PROD 앱·부트 설정·콘텐츠의 최종 호환 검증은 별도다.

롤백도 현재는 DEV의 연결만 검증된 이전 호환 원본으로 돌리고 releaseRevision을 증가시킨다. 향후 PROD의 롤백은 PROD 문맥에서 독립 실행한다. 작전 시작에 고정한 데이터와 확정 원장/보상은 롤백으로 변경하지 않는다.

§2.1은 환경당 고정 채널과 서버 빌드 승인만 정의한다. 이전 동일 PROD 앱의 복수 채널 선택 제안은 현행 본문에서 삭제했다. 점검·공개 상태는 [12 점검 정책](12_SERVICE_MAINTENANCE.md)이 소유하며 데이터 버전 선택이나 배포 단계로 대체하지 않는다.

## 1. 불변 원칙

수치형 밸런스의 원본은 Server Versioned Balance Config다. 클라이언트는 승인된 묶음을 다운로드·검증·캐시하고, 작전 중 효과 계산은 로컬 메모리 데이터와 이미 배포된 효과 처리 코드로 수행한다. 공격·피해·스킬 발동마다 API를 호출하지 않는다.

발행된 BalanceVersion의 바이트는 수정하지 않는다. 데이터 작성·발행과 채널 활성화는 다른 동작이다. 표별로 서로 다른 버전을 하향 선택하지 않고 하나의 묶음을 선택한다. 내부테스트 파일·Draft·임의 캐시는 운영 승인 근거가 아니다.

## 2. 식별자와 책임

| 항목 | 정의 |
|---|---|
| environment | 현재 DEV, 향후 PROD. U9로 STG/STAGE를 현행 환경에서 폐기. 과거 기록 자동 변환 금지 |
| channel | 현재 DEV=INTERNAL, 향후 PROD=PRODUCTION으로 고정. 일반 사용자 모드 전환 없음 |
| economyScope | 계정·재화·작전·내실을 분리하는 서버 결정 범위. DEV와 PROD 경제를 공유하지 않음 |
| applicationId / platform | 앱 식별자와 플랫폼. versionCode 숫자만으로 앱을 구분하지 않음 |
| installedVersionCode | 현재 설치 앱의 양의 정수 코드. versionName과 다름 |
| applyFromVersionCode | 해당 채널에서 배포 연결을 적용하기 시작하는 기준점 |
| min/maxClientVersionCode | 데이터 원본의 기술적 실행 가능 범위. max=null은 상한 없음이지만 다른 검사를 생략하지 않음 |
| balanceVersion | 불변 데이터 원본 식별자. 숫자 대소로 최신성을 판단하지 않음 |
| releaseRevision | 특정 배포 문맥의 활성 연결 목록 개정 번호. 롤백도 증가 |
| schemaVersion / requiredCapabilities | 앱이 해석할 데이터 형식과 효과 처리 능력 |
| contentReleaseId | 필요한 정확한 콘텐츠 식별자. 준비 전 다운로드 가능 여부와 실행 호환을 구분 |
| commanderProgressionRulesId | 동일 운영 경제의 영구 XP·포인트·해금 규칙 식별자 |

배포 문맥 키는 `(environment, applicationId, platform, channel)`이다. economyScope는 서버의 채널 접근 권한과 묶어 결정한다. 캐시 접근 승인·유저 상태에는 accountId와 economyScope도 포함한다.

Android versionCode와 versionName의 구분은 공식 문서에 따른다. 기존 프로젝트도 앱 빌드·최소 지원 기준·콘텐츠 릴리스를 분리한다. 근거: R04/R05 및 외부 기술 참고 §12.

<a id="channel-resolution"></a>
### 2.1 현행 빌드·환경·채널 판정

현행 매핑은 **DEV 빌드 → DEV / INTERNAL**, 향후 **PROD 빌드 → PROD / PRODUCTION**이다. 같은 PROD 앱의 일반/테스트 모드 전환·복수 채널 선택 UI·STG 승격은 현행 구현 범위에서 제거한다. 새 환경이나 숨은 QA 채널을 만들지 않는다.

클라이언트는 실제 설치 applicationId/platform/versionCode와 빌드에 고정한 부트 문맥을 제공한다. 서버는 등록된 빌드의 허용 환경과 접근권한을 확인하고 채널을 응답한다. 높은 versionCode, installer, debug 또는 클라이언트 isTest 선언만으로 권한을 부여하지 않는다. Google Play 설치 트랙 자동 감지가 아니라 **프로젝트 환경 설정 + 서버 빌드 승인**으로 구분한다.

로그인 전 기존 admission은 앱·환경·공통 부트 적격성을 검사한다. 계정별 DEV 접근 검사는 인증 후 추가하며, 인증 전에 계정 권한을 요구하는 순환 의존을 만들지 않는다. 환경당 채널을 고정하므로 계정 로그인 때 동일 PROD 앱의 채널을 전환하는 추가 분기를 만들지 않는다. 정확한 빌드 등록/DEV 테스터 등록 wire는 기존 소스를 확인해 연결한다.

미등록 빌드를 데이터 하향 선택으로 승인하지 않는다. 먼저 해당 앱을 승인한 뒤, 같은 환경·채널에서 호환 기준점을 선택한다. 계정·캐시·저널·작전·결과는 원래 환경을 유지하고 DEV 기록을 PROD로 자동 이관하지 않는다. 부트 URL·프로필·서명·캐시 키·데이터 활성화 목록에서 이 경계를 함께 검사한다.

DEV에서 검증한 **데이터의 동일 원본**은 향후 PROD로 승격할 수 있다. DEV 부트가 고정된 **앱 바이너리**가 자동으로 PROD 앱이 되는 것은 아니다. 공개 후보는 PROD 부트 설정과 해당 데이터/콘텐츠의 결합을 별도로 검증한다.

## 3. 논리 데이터 모델

기존 [versioned-balance](49_SERVER_API_CONTRACTS.md#source-de8c86b812b01d1c)의 publish+activate 단일 트랜잭션은 종전 구현이다. 현행 PublishBalance는 불변 원본만 발행하고 활성 연결을 바꾸지 않는다. 분리 구현에서도 OWNER 권한, expectedRevision/expectedChecksum, payload에 결속된 일회 확인 토큰, 실패 감사와 원결과 멱등성은 보존한다. 전역 active와 문맥별 ActiveRelease를 동시에 독립 원본으로 두지 않는다.

| 논리 객체 | 필수 내용 | 갱신 방식 |
|---|---|---|
| ClientBuildRegistration | 앱·플랫폼·versionCode·지원 상태·허용 환경/채널·서버가 검증한 기능 집합 | 권한 있는 서버 운영 정책 |
| BalanceVersion | 원본 ID·schema·호환 min/max·필수 처리기·콘텐츠·테이블/체크섬·영구진행 규칙 ID | 발행 후 불변 |
| ReleaseManifest | 문맥 키·고유 ID·기준점→BalanceVersion 목록·검증 근거 | 작성 중 Draft, 발행 후 불변 |
| ActiveRelease | 문맥 키·releaseRevision·manifestId | 조건부 원자 교체 |
| ActivationAudit | actor·이유·전/후 revision·원본 digest·대상 기준점·멱등 키·결과 | 추가 기록, 원기록 유지 |
| BalanceSelection | 계정/경제/앱/채널·승인 revision·선택 기준점·원본 버전·만료/취소 상태 | 서버가 생성·검증 |

한 manifest에서 동일 기준점을 중복 정의할 수 없다. 기존 `drafts / versions / publish_claims / activations / active` 역할이 있으면 그 구조를 확장한다. 서버 전체 하나의 activeVersion을 새 문맥별 선택과 동시에 경쟁하는 기준으로 남기지 않는다.

`applyFromVersionCode >= minClientVersionCode`, 상한이 있으면 `applyFromVersionCode <= maxClientVersionCode`를 검사한다. 현재 지원 빌드뿐 아니라 알려진 다음 빌드와 관련 계정 저장 상태까지 배포 전 해석한다.

## 4. 데이터 선택 알고리즘

1. 현재 설치 앱의 진입 승인과 요청자 인증을 검증한다. 오래된 요청 작성 빌드를 현재 설치 코드로 가장하지 않는다.
2. 서버가 environment/channel/economyScope를 확정한다. 클라이언트의 isTest=true, 높은 버전 번호, 테스터 계정이라는 사실 하나만으로 환경 경계를 넘겨 승인하지 않는다.
3. 해당 문맥의 ActiveRelease를 한 일관된 스냅샷으로 읽는다.
4. 그 manifest에서 `applyFromVersionCode <= installedVersionCode`이고 데이터 min/max·schema·서버 등록 capabilities·콘텐츠 실행 호환·영구 계정 상태를 만족하는 후보를 찾는다.
5. 후보 중 applyFromVersionCode가 가장 큰 항목 하나를 선택한다. 정확 일치가 있으면 자연스럽게 우선한다.
6. 선택한 묶음과 승인 identity를 응답한다. 클라이언트는 동일 조건·참조·체크섬을 다시 검증하고 전체 준비 후 사용한다.

지원되는 콘텐츠가 아직 캐시에 없다는 이유만으로 하위 데이터를 선택하지 않는다. 다운로드 실패·파일 손상·서버 5xx·해석 오류도 임의 하향 선택 사유가 아니다. 미지원 앱 차단을 우회하려고 구형 데이터를 고르지 않는다. 유효 후보가 없으면 명시 실패한다.

INTERNAL에서 일치 항목이 없다고 PRODUCTION 목록을 자동 탐색하지 않는다. 운영 데이터를 테스트 기준으로 쓸 때는 같은 불변 원본을 INTERNAL 목록에 명시적으로 연결한다.

### 예시 — 실제 운영 설정이 아닌 설명용

| 환경 / 채널 | 기준점 | 원본 |
|---|---:|---|
| PROD / PRODUCTION | 100 | B100 |
| PROD / PRODUCTION | 120 | B200 |
| DEV / INTERNAL | 120 | B200 |
| DEV / INTERNAL | 125 | B210 |

환경/앱/계정이 각 채널에 승인되고 기타 호환을 만족할 때: 운영119→B100, 운영123→B200, 테스트123→B200, 테스트125/127→B210, 운영130→B200이다. 운영130이 테스트125 데이터를 가져오는 경로는 없다.

## 5. 논리 명령과 응답 계약

실제 route/header 명칭은 기존 owner에 매핑한다. 아래 논리 연산을 이유 없이 각각 새로운 HTTP API로 만들지는 않는다.

| 연산 | 주요 입력 | 출력/검증 |
|---|---|---|
| ResolveBalance | 실제 앱 identity, 설치 코드; 서버 인증 context | selectionId, context, revision, 기준점, balanceVersion, schema/capabilities/content/checksum, 만료/재검증 정보 |
| ReadBalance | 승인된 selection 또는 기존 run의 고정 버전 | 그 원본의 정확한 manifest/테이블. 다른 채널 원본의 임의 조회는 승인하지 않음 |
| PublishBalance | 검증된 Draft revision·체크섬 | 불변 BalanceVersion. 자동 운영 활성화 없음 |
| PreviewRelease | 목표 문맥·기준점 연결 변경·expectedReleaseRevision | 대상 빌드/계정 영향, 호환 결과, previewId/digest |
| ActivateRelease | previewId/digest·expectedReleaseRevision·idempotencyKey·reason | 새 revision과 활성 manifest; 승인/검증/원자성 필수 |
| PromoteRelease | 명시 sourceBalanceVersion/hash·검증 근거·대상 환경/채널/기준점·대상 expected revision | 환경간 승격은 향후 DEV → PROD만. DEV 내부 교체는 ActivateRelease가 담당. 검증한 동일 원본만 연결 |
| RollbackRelease | 명시 이전 호환 원본/manifest·reason·대상 expected revision | 새 revision으로 이전 내용을 참조. 원본/이력/유저 상태 삭제 없음 |

selection에는 최소한 accountId/economyScope, 앱/환경/채널, installedVersionCode, releaseRevision, applyFromVersionCode, balanceVersion이 결속된다. ID를 알아도 다른 계정·채널에 사용할 수 없다. 다운로드 원본의 hash는 무결성 확인이며 승인 자체를 대체하지 않는다. 인증정보를 임의 반환 URL로 전달하지 않는다.

### 동시 수정·재시도

활성화/승격/롤백은 expected revision을 비교한 동일 트랜잭션에서 active 포인터·개정 번호·감사·원결과를 확정한다. 다른 운영자가 먼저 수정하면 덮어쓰지 않고 충돌을 반환한다. HTTP로 노출할 때는 강한 ETag와 If-Match 또는 이에 동등한 서버 CAS를 사용한다.

같은 키·같은 본문은 최초 결과를 반환하고 같은 키·다른 본문은 거절한다. 원결과의 revision이 현재값보다 낮아도 이를 현재 active에 다시 쓰지 않는다. 재시도 결과와 현재 상태 조회는 분리한다.

## 6. 활성화·승격·롤백

**현재 경로:** `DEV Draft 편집 → 검증 → 불변 데이터 발행 → DEV 활성화 → 통합·실기기·회귀·출시 후보 검증`. 환경 이동 없이 정확한 후보와 근거를 고정한다.

**후속 출시 경로(현재 미실행):** `DEV에서 후보 검증 완료 → 동일 원본 PROD 승격 → 운영용 앱·콘텐츠 최종 확인 → 공개 배포`. DEV 전용 앱 자체가 자동으로 PROD 앱으로 바뀌는 뜻은 아니다.

각 단계는 별개 권한/작업이다. 운영 승격은 source version/hash를 정확히 지정한다. '최신 테스트 파일'을 그 시점에 다시 찾지 않는다. 데이터가 달라졌으면 새 버전·새 테스트를 거친다. 환경별 저장소로 복사해도 동일 바이트/해시를 확인하고 보상/구매/진행 데이터는 복사하지 않는다.

상태 변경 전에 실제 효과 처리기, 콘텐츠 가용성, 모든 활성 기준점의 대상 빌드, 영구진행 규칙, 유저 저장 상태, 현재 run 복원/정산 경로를 검증한다. 검증 가능한 이전 버전을 롤백 대상으로 명시한다.

롤백은 `기준점125: B211 → B210`처럼 호환 데이터 연결을 바꾼다. 기준점을 삭제해서 하위 데이터가 임의 적용되게 하는 기본 롤백은 사용하지 않는다. 이전 목록 전체로 돌아가더라도 새로운 releaseRevision을 발행하고 빠진 기준점의 영향을 미리 검증한다.

일반 롤백은 신규 작전에만 적용한다. 유효한 진행 작전·XP·아이템·보상 원장을 되돌리거나 DB 전체를 과거 상태로 복원하지 않는다. 기존 작전을 중단해야 하는 사고는 별도 명시적인 작전 중단/보상 정책의 영역이다.

## 7. 작전 시작·재개와 배포 경합

서버가 작전을 최초 승인할 때 selection, BalanceVersion, content, 설치 build, commander progression revision/효과와 economyScope를 스냅샷으로 저장한다. 현재 설치 build와 원요청 작성 metadata는 R13/R14대로 구분한다.

작전 시작 전 배포가 바뀌어 selection이 만료/구형이면 **원자적인 확정 거절**을 저장하고 입장 비용·run 생성·시작 보급을 발생시키지 않는다. 기존 key에 대한 지연/중복 요청도 그 거절 결과를 재현해야 한다. 그 거절을 확인한 뒤에만 새 selection으로 새 논리 요청을 구성한다. 응답이 단순 유실된 경우에는 새 key로 바꾸지 않고 원요청 확인/복구를 한다.

기존 [서버 통합 설계](49_SERVER_API_CONTRACTS.md#source-6f5e52c4e100a74b)의 실패 응답을 영구 멱등 결과로 저장하지 않는 일반 규칙과 구분한다. 이 절의 `SELECTION_STALE`은 **확정된 0차감 terminal rejection을 보존·재현하는 특정 예외**다. 모든 오류/5xx를 영구 캐시하는 규칙으로 확대하지 않으며 실제 원장·원요청 조회 계약에 이 예외를 명시적으로 연결해야 한다.

이미 승인된 작전의 재개·종료·보상 조회는 최신 하향 선택을 다시 실행하지 않고 고정 원본을 사용한다. 업데이트된 앱이 고정 데이터/저장 형식을 실행할 수 있는지 별도 검사한다. 설치 코드만 달라졌다고 원본문·고정 버전을 바꾸지 않는다. 실행이 불가능해도 확정 보상/원결과 기록을 삭제하지 않는다.

다운로드 실패·일반 비호환을 [04 §6](04_COMBAT_RUN.md#discard)의 구형 저장 형식 폐기 예외로 자동 확대하지 않는다. 게임 신규 진입 차단과 원결과 조회/복구의 허용 범위도 별도 관리한다.

## 8. 영구 내실과 앱 버전 간 일관성

동일 운영 economyScope에서 동시 지원되는 배포 기준점들은 **동일한 commanderProgressionRulesId와 동일한 영구 지급 규칙 digest**를 참조한다. 최소 대상은 XP 레벨 경계, 포인트 지급 일정, 확정 해금, 영구 구매 비용/원장 의미다. 스킬 전투 계수는 각 작전 BalanceVersion에 고정할 수 있지만, 앱을 바꿔 레벨업 보상·포인트를 중복 획득할 수 없어야 한다.

이를 바꾸려면 일반 수치 롤백이 아니라 별도 영구진행 마이그레이션 계획과 지원 앱 검증이 필요하다. 이미 획득한 레벨/포인트/해금은 단순 표 교체로 감소시키거나 재지급하지 않는다. 구버전 앱이 신형 특성·프리셋 상태를 모르면 임의 삭제/0으로 해석하지 않고 지원 가능한 승인 조합 또는 명시적 업데이트/호환 실패로 처리한다.

INTERNAL의 별도 XP·재화·내실은 운영 economyScope에 합치지 않는다. 테스트 계정 승격이나 원장 복제는 데이터 승격 계약에 포함되지 않는다.

## 9. 오류 분류

아래는 신규 논리 결과 분류다. 실제 HTTP 숫자/기존 error code를 확인해 매핑한다.

| 결과 | 처리 |
|---|---|
| APP_NOT_ADMITTED / APP_UPDATE_REQUIRED | 기존 앱 지원 정책과 승인된 업데이트 UI |
| CHANNEL_FORBIDDEN | 내부테스트 요청 거부. 운영으로 자동 변경하지 않음 |
| NO_COMPATIBLE_BALANCE | 새 작전 잠금·설정 확인. 내장표/다른채널 fallback 없음 |
| ACCOUNT_STATE_INCOMPATIBLE | 구버전으로 계정 상태를 축소하지 않음 |
| SELECTION_STALE | 최초 시작 전 확정 거절·0차감. 거절 확인 후 새 선택 |
| RELEASE_REVISION_CONFLICT | 운영 명령 덮어쓰기 금지·최신 상태 조회 |
| CHECKSUM_OR_REFERENCE_INVALID | 승인 묶음 적용 금지·정상 캐시 보존·재시도/운영 확인 |
| NETWORK_UNAVAILABLE | 기존 run·receipt 보존. 승인되지 않은 오프라인 신규 작전 허용을 뜻하지 않음 |
| RUN_DATA_INCOMPATIBLE | 고정 run 복구/정산 가능 범위 구분. 자동 밸런스 교체 금지 |

## 10. 필수 인수 검사

| 검사 | 기대값 |
|---|---|
| 정확한 기준점 / 기준점 사이 빌드 | 동일점 우선 / 호환 하위 최대점 |
| 첫 기준점보다 낮음 / 미등록 빌드 | 명시 실패 |
| 테스트에만 높은 기준점 존재 | 운영 선택 불변 |
| 테스트 목록 비어 있음 | 운영 자동 fallback 금지 |
| schema/capability/min/max 불일치 | 승인 후보에서 제외; 진입 정책 우회 금지 |
| 선택 원본 다운로드 손상 | 하위표 임의 전환 금지 |
| 한 manifest에 같은 기준점 두 개 | 발행/활성화 거절 |
| 같은 기준점 수치 패치 | 새 balanceVersion, 새 releaseRevision, 앱 재빌드 불필요한 범위만 |
| 동시 활성화와 같은키 재시도 | 하나만 active 교체 / 원결과 한 번 |
| INTERNAL 검증 뒤 원본 변경 | 기존 테스트 증거 재사용 불가 |
| 롤백 | revision 증가, 원본·원장·기존 run 유지 |
| selection과 start 사이 배포 | 확정 stale 거절 또는 그 이전에 원자 승인된 run 하나; 부분차감 없음 |
| 같은 계정 앱/채널 변경 | XP/포인트/보상 중복·유실 없음 |
| 진행 작전 재개 | 최신 release 대신 고정 버전 |
| 미래 효과 ID·잘못된 표 참조 | 무시하고 통과 금지 |

이 인수 검사는 구현 요구사항이다. 이번 파일 생성의 수치/문서 검사를 서버 DB·기기 인수 시험으로 표현하지 않는다.

## 11. Admin 필수 표시

Build 등록, 불변 BalanceVersion, 채널별 기준점 연결, 적용 결과 미리보기, 운영 승격, 롤백 이력을 별도 책임으로 표시한다. 미리보기는 선택 기준점·제외 이유·전후 데이터·영구진행 영향·진행 작전 영향·대상 앱을 보여준다. 이유/actor/대상 revision 없는 운영 변경을 허용하지 않는다.

## 12. 근거와 신규 범위

사용자 2026-09-20 지시: 서버 테이블·캐시·versionCode 비교·채널별 하위 최대 기준점·운영 승격·롤백 계약 확정. R04/R05: 실제 설치 코드와 서버 진입 승인; R13/R14: 원요청 및 최초 접수 build 보존; R18: 승인 세션 중 콘텐츠 hot-swap 금지. 이 문서는 그 기존 구현 사실과 신규 밸런스 계약을 구분한다.

외부 기술 참고(2026-09-20 확인): Android Developers, `Version your app` (https://developer.android.com/studio/publish/versioning); IETF RFC 9110 §13.1.1 `If-Match` (https://www.rfc-editor.org/rfc/rfc9110.html#section-13.1.1). 링크는 기술 의미 확인용이고 LAST RAIL에 구현됐다는 증거는 아니다.

이번 채널 질문의 외부 기술 참고(2026-09-20): Android InstallSourceInfo (https://developer.android.com/reference/android/content/pm/InstallSourceInfo), Play Integrity verdicts (https://developer.android.com/google/play/integrity/verdicts), Google Play Developer API edits.tracks (https://developers.google.com/android-publisher/api-ref/rest/v3/edits.tracks), generated APKs/publishing workflow (https://developers.google.com/android-publisher/download-apks). 이는 이전 설명의 출처 기록이다. 현행 §2.1은 환경당 고정 채널이며, 설치 트랙 기반 자동 전환을 구현했다고 주장하지 않는다. 이번 감사에서 외부 내용을 재검증한 것은 아니다.

U6(과거 결정): 사용자 “DEV 에서 STG 까지 진행할거야”. **U9(2026-09-21)에 의해 폐기·대체**되었다. 현재 규칙은 §0의 DEV 단일 비운영 환경이며, STG 이력은 현행 구성의 근거로 사용하지 않는다. 같은 PROD 빌드의 복수 채널 선택 제안은 현행 범위에서 제외했다. 빌드·코드·DB·원격 환경은 이번에 변경하지 않았다.

## 13. 광고·공지·공개 홈페이지의 배포 경계 — U7

사용자는 첫 공개판을 광고 없는 데모 느낌으로 제공하고 서버 스위치·공지·홈페이지 위키/확률/커뮤니티를 추가하도록 요청했다. 이는 신규 기획 범위이며 §0의 현재 DEV 단일 개발·검증 환경과 향후 PROD 별도 활성화 승인을 따른다. 공개 데모를 DEV에 일반 사용자 접근을 열어 제공하는 것으로 해석하지 않는다.

[10](10_LIVEOPS_NOTICES.md)의 RuntimePolicyRevision/adsEnabled와 NoticeRevision은 BalanceVersion/ReleaseRevision과 별도다. 광고 중단을 작전 끝까지 기다리거나 데이터 하위 기준점의 오래된 ON으로 우회하지 않는다. 데이터 승격·롤백은 광고 ON이나 공지 게시를 자동 실행하지 않는다.

[11](11_WEBSITE_COMMUNITY.md#public-data)은 공개 운영 데이터만 허용 필드로 투영한다. 권한 있는 게임 데이터 ReadBalance와 공개 ReadCatalog는 다른 연산이며 공개 버전 조회가 INTERNAL/Draft 접근권한을 주지 않는다. 새 공개 운영 배포 전에 정확한 원본·확률policy에 대응하는 위키/공시 snapshot을 준비·검증하고 그 식별자를 배포 승인에 결속하는 안을 추가한다. 상세 표기·캐시·법정 검토·사이트 공개 절차는 11에서 관리한다.

현재는 DEV에서 권한 있는 미리보기·통합·실기기 검증을 모두 진행한다. 공개 승인 전 test snapshot의 검색·상세·이미지 URL을 인터넷에 열지 않고 robots/noindex를 보안 장벽으로 취급하지 않는다.


## 14. 점검과 배포의 관계 — U9 연결

점검은 [12](12_SERVICE_MAINTENANCE.md)의 별도 서비스 가용성 정책이며 BalanceVersion에 고정하지 않는다. 새 작전 승인 시 최신 점검 gate와 활성 배포를 함께 확인하고 승인/거절의 순서를 서버에서 확정한다. 점검 시작·해제만으로 balanceVersion을 바꾸거나 기존 run을 최신 표에 재해석하지 않는다.

종료 예정시각이나 공개 상태 snapshot의 NORMAL만으로 게임 진입을 승인하지 않는다. 정상 복귀 시 기존 admission·auth·content·selection 검사 후, 기존 작전은 원래 버전으로 복구한다. 점검은 [04 §6](04_COMBAT_RUN.md#discard)의 구형 작전 폐기 사유가 아니며 불확실한 시작/정산은 같은 키로 확인한다.


## 15. 테이블 소유와 공개 전 결정

장비·기본효과·고유효과·세트·독립 인챈트는 [13](13_EQUIPMENT_ENCHANTMENTS.md), 맵·이벤트·노드 드랍은 [14](14_MAP_EVENTS_DROPS.md), 지휘관 내실은 [09](09_COMMANDER_PROGRESSION.md)가 수치/기믹 정의를 소유한다. 각 표가 개별 버전을 선택하는 것이 아니라 이 문서의 **하나의 승인 묶음**으로 배포된다. 공용 정의와 작전 인스턴스/인챈트 시술값은 구분한다.

미확정 밸런스·추첨/합성/시술 규칙은 작성용 파일에서만 유지하고 `publishable=false`로 둔다. [00의 판단 대기표](00_INDEX.md#open-decisions)와 원문/코드 확인 항목을 해결하기 전 운영 seed나 기본값으로 채우지 않는다.


## 누적 원문 대조 보충

[운영 상세 계약 보존 보충](36_OPERATIONS_COMPATIBILITY.md)에서 기존 상세 정책, 대체된 규칙과 미해결 경계를 함께 확인한다. 요약 밖 API·이력 원문은 보존한다.
