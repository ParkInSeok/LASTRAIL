# LAST RAIL | 콘텐츠·다운로드·업데이트

범위: 자원 패키징, 서버 진입 승인, 다운로드 동의, 릴리스 핀, 빌드 기준선, 필수 업데이트. 계정/로비 소유권은 [01](01_ACCOUNT_ENTRY.md), 검증은 [문서 안내](00_INDEX.md#qa)를 따른다.

## 1. 패키징과 진입 승인

최초 로딩·로그인·오류·업데이트 의존성은 로컬에 남기고, 대부분의 게임 자원을 실제 Addressables 번들로 전환한다. 원격 screen prefab은 내부 asset 참조를 번들에 유지하며 안정된 path key·category label을 사용한다. 자료의 기준은 Unity `6000.3.22f1`, Addressables `2.8.1`이다. source/showcase/test 자원을 제품 입력으로 포함하지 않는다. [R02](00_INDEX.md#r02)

| 승인 항목 | 조건 |
|---|---|
| 서버 설정 | `accepted=true`, admission=`ACCEPTED` |
| 업데이트 | `updateRequired=false` |
| 콘텐츠 | `APPROVED_COMPATIBLE` |
| 버전 | 실제 설치 Android versionCode를 사용한다. 누락·미지원 코드를 임의 생성하지 않는다. |
| 구형 필드 | `minimumClientVersion`만으로 위 승인을 덮어쓸 수 없다. |

앱 빌드·지원 하한·콘텐츠 릴리스는 독립 정보다. 인증/프로필/문의 등 보호 HTTP에는 설치 코드 헤더를 붙이되 원래 Bearer·body·키를 보존하고 redirect를 끈다. Editor/no-code, 신뢰된 catalog root/승인 설정 부재는 성공으로 위장하지 않는다. [R04](00_INDEX.md#r04) [R05](00_INDEX.md#r05)

## 2. 다운로드 정책

| 상황 | 처리 |
|---|---|
| Wi-Fi 시작 | 허용된 병행 다운로드를 진행한다. 인증 완료가 콘텐츠 작업을 새로 시작시키지 않는다. |
| 처음부터 비-Wi-Fi | 명시적 다운로드 동의를 받는다. |
| Wi-Fi에서 다른 망으로 전환 | 자신이 소유한 요청만 중단하며, 전환 동의는 로그인 이후로 미룬다. 초기 크기 계산 전의 전환 이력도 유지한다. |
| pause 후 resume | 새 네트워크 snapshot이 Unity context에 적용된 뒤 suspension을 해제한다. 오래된 Wi-Fi 상태로 잠깐 다운로드를 허용하지 않는다. |
| 실패한 로비 진입·캐시 누락 | 같은 승인 릴리스를 유지하되 preparation과 남은 용량 계산부터 다시 수행한다. |

DownloadAccessPolicy/DownloadCoordinator는 config·auth·content gate를 분리한다. Android capability로 전송망을 보수적으로 판별하며 `internetReachability`만으로 Wi-Fi를 추정하지 않는다. 승인 동의는 메모리 전용이며 catalog bundle fingerprint에 결속된다. 네트워크/수명 변경 뒤 늦은 callback을 배제한다.

소유한 순차 `UnityWebRequestAssetBundle` prefetch가 버전 캐시를 채우고, cache-only provider가 이를 소비한다. package cache 편집·전역 큐 변경·OS 백그라운드 서비스는 추가하지 않는다. 중단된 번들 일부는 다시 내려받을 수 있으며 HTTP range resume를 구현했다고 보지 않는다. [R01](00_INDEX.md#r01) [R19](00_INDEX.md#r19) [R21](00_INDEX.md#r21)

## 3. 카탈로그·의존성·캐시 검증

승인된 catalog bytes를 SHA256으로 확인하고 설치 provider의 binary/JSON 형식을 유지한다. startup settings에는 원격/캐시 fallback 의존성이 없는 로컬 카탈로그 하나만 둔다. 전체 카탈로그 자동 업데이트 조회를 사용하지 않는다.

필수 의존성 graph는 지원 provider와 신뢰된 위치만 허용한다. 원격 bundle의 cache identity·동의 다운로드를 확인하되 설치 로컬 bundle에 원격 캐시 조건을 잘못 적용하지 않는다. 화면은 승인된 IResourceLocation으로 해석하고, 전역 key 조회로 승인 경계를 우회하지 않는다. 손상된 cache CRC는 거부하며 cache-only 단계가 임의 추가 네트워크를 발생시키지 않는다. [R05](00_INDEX.md#r05) [R07](00_INDEX.md#r07) [R18](00_INDEX.md#r18)

## 4. 미완료 릴리스 핀과 전환

미완료 준비 metadata를 원자적으로 저장하지만 그것 자체는 승인 권한이 아니다. 재실행마다 새 서버 승인과 source·설치 코드·trusted root·release identity를 대조한다. 디스크 핀과 새 승인이 다르면 명시 Retry로 pending metadata만 정리하고 admission을 무효화한 뒤 다시 승인받는다.

같은 인스턴스의 동일 완료 릴리스만 중복 진입을 허용하며, 새 프로세스는 이전 완료나 캐시를 승인 근거로 삼지 않는다. 완료는 그 인스턴스가 검증한 전체 identity에 한정된다. 승인된 세션 도중에는 서버 선택이 바뀌어도 기존 릴리스를 hot-swap하지 않는다. 이 규칙은 콘텐츠 전환이지 전투 Balance Config 계약이 아니다. [R05](00_INDEX.md#r05) [R18](00_INDEX.md#r18)

## 5. LOCAL_TEST 전체 빌드 기준선

전체 빌드는 `QA/Act1Launch/Addressables/Baselines` 아래 고유한 명시 ID로 보관한다. content_state/state.bin·runtime/remote output과 파일별 SHA256·길이 manifest를 함께 보존한다. identity에는 profile ID·target·Unity/Addressables 버전·settings GUID·평가된 catalog load URL을 포함한다.

업데이트는 명시한 full ID만 사용한다. 최신 파일 자동 선택·외부 state import·기존 ID 덮어쓰기를 금지한다. 역직렬화 전에 identity와 목록의 hash를 검증하고, 격리 working output에서 ContentUpdateScript API를 호출한다. update archive는 원 full ID/state hash를 기록하되 full baseline으로 재사용하거나 원본을 덮어쓰지 않는다.

staging 검증 후 디렉터리 rename, unsafe/UNC/device/reparse 경로 차단, catalog/group override 차단, RemoteBuildPath 복원, 빌드 후 원본 archive 재검증을 적용한다. SHA256은 훼손 확인 수단이지 외부 출처 인증이 아니며 신뢰된 로컬 빌드 산출물만 취급한다. 무변경 업데이트·합성 asset delta·제품 backend 전환 검증의 증거 범위는 서로 다르다. [R15](00_INDEX.md#r15) [R16](00_INDEX.md#r16) [R17](00_INDEX.md#r17) [R18](00_INDEX.md#r18)

## 6. 필수 업데이트 목적지

RequiredUpdateAction은 명시 주입된 목적지와 정확한 승인 URL을 고정한다. canonical HTTPS Play listing 검증에서 credentials·다른 host/port·fragment·encoded ID·추가 query를 거부한다. 사용자 클릭만 외부 opener를 호출하며, 실패/예외/누락은 안전한 안내와 재시도로 처리한다.

외부 열기 성공은 설치 완료가 아니므로 업데이트 gate를 해제하거나 로비로 보내지 않는다. 자료상 기본 목적지/allowlist는 비어 있고 승인된 listing URL·client-config 목적지 필드가 없었다. 제품 URL을 만들어 채우지 않는다. Android intent adapter 존재와 실제 market handoff 완료도 구분한다. [R08](00_INDEX.md#r08)

## 7. 밸런스 테이블 배포와의 연결

채널별 앱 버전 기준점·밸런스 묶음 선택·운영 승격·롤백은 [08 공통 계약](08_BALANCE_RELEASE.md)이 소유한다. 이 문서의 콘텐츠 승인/다운로드/핀을 별도 업데이트 owner로 교체하지 않는다. 현재 채널은 DEV=INTERNAL, 향후 PROD=PRODUCTION으로 환경에 고정한다. 로그인 전 앱/환경 admission을 유지하고, 계정별 DEV 접근 권한은 인증 후 추가 검사한다. 같은 PROD 앱의 테스트 모드 전환 분기를 만들지 않는다. 원격 콘텐츠 준비와 밸런스 선택은 상호 호환을 검사하되 서로 같은 버전 번호로 가정하지 않는다.

## 8. 실시간 운영 설정·공지와의 경계 — U7

[10](10_LIVEOPS_NOTICES.md)의 광고 마스터 스위치는 별도 RuntimePolicy와 현재 유효성으로 검사한다. 콘텐츠/밸런스의 기존 작전 고정값 때문에 새 광고 중단이 지연되게 하지 않는다. 정책 미확인/기능 미지원이면 광고만 비활성으로 두고, 원래 admission/다운로드 동의·캐시 검증을 우회하지 않는다.

일반 공지는 필수 콘텐츠 준비 gate와 분리하며 실패해도 로비를 가두지 않는다. 점검·필수 업데이트는 기존 권한 gate로 판단한다. 홈페이지용 공개 데이터도 런타임 콘텐츠 다운로드 권한이 아니고 [11](11_WEBSITE_COMMUNITY.md#public-data)의 별도 공개 투영이다. 신규 앱 기능/SDK가 없는 빌드를 서버 bool 하나로 광고 지원 빌드로 바꾸지 않는다.


## 9. U9 환경 통합·로컬 점검 화면

현재 환경 정의는 [08 §0](08_BALANCE_RELEASE.md#current-environment)의 DEV 단일 비운영 환경이다. 별도 STG/STAGE 부트·Addressables 프로필·배포 단계를 신규 구성하지 않는다. 기존 LOCAL_TEST와 임시 staging 디렉터리는 삭제할 환경이 아니다.

[12 §5](12_SERVICE_MAINTENANCE.md#client-screen)의 기본 점검 화면·문구·필수 폰트·버튼·일반 연결실패 화면은 앱 설치 리소스에 둔다. 원격 Main/공지 이미지/카탈로그를 받아야 점검을 알 수 있는 순환 의존을 만들지 않는다. 기존 초기 loading/login/error/update 로컬 예외와 같은 경계다.

점검 gate를 실제로 확인하면 소유한 불필요한 원격 준비 요청만 중지하고 정상 캐시·원본 핀은 보존한다. 공용 네트워크 전체를 끊어 상태 조회까지 막지 않는다. 점검 화면 진입을 이유로 핀 충돌을 자동 삭제/해결하지 않고, 해제 뒤 기존 승인·핀·동의 정책으로 재검증한다.

## 기존 상세 계약·검증 원문 위치

아래 전문은 생략된 필드·오류·직렬화·복구 경계를 확인하는 원문이다. 최신 사용자 정정과 충돌하는 구형 정책을 복원하지 않으며, 과거 테스트를 이번 검증으로 재사용하지 않는다.

- [R01 2026-09-05-network-content-startup.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-8cdfe2238dd38298e0ad)
- [R02 2026-09-06-addressables-runtime.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-94989c49f5fa630540d7)
- [R08 2026-09-06-required-update-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-356bad83f06db4c037a4)
- [R15 2026-09-06-content-state-baselines.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-dd4f6182095cbd325b76)
- [R16 2026-09-07-content-state-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-ea1cf27bccefa63eb77e)
- [R17 2026-09-07-synthetic-content-delta-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-65d96c9caa9cad00cc55)
- [R18 2026-09-07-backend-content-release-transition-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-dc1feb6eb3709aa97473)
