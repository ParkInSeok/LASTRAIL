# 운영 점검과 신규 요청 승인 경합 설계

2026-09-22 / SERVER_3 / 설계 검토 대기. 구현·이관·배포·실제 활성화 승인으로 간주하지 않는다.

## 목표와 첫 구현 범위

점검 차단 시각 이후 신규 출격·광고 시작·구매 시작이 저장되지 않게 하고, 이미 승인된 원요청의 결과와 유효 거래 복구는 보존한다. 게임 빌드 없이 서버 격리 DB에서 검증한다. 첫 구현 단위는 GAME 신규 요청에 대한 저장 경계와 정책 저장 계약이다. 모든 게임 쓰기/전투 보존/실제 점검 활성화/공개 상태 발행까지 완료했다고 주장하지 않는다.

현재 DEV는 ec32703, DB0047, RUNNING, 광고OFF다. 이번 설계 때문에 원격 설정이나 데이터를 변경하지 않는다. 기존 Admin 초안과 요청별 hypothetical helper는 실제 정책으로 승격하지 않는다.

## 대조한 기존 원본

개발 원본: D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server.
- apps/game-api/src/index.ts의 run_start: 원영수증을 먼저 읽고, operation_previews 소비·runs·snapshot·멱등 기록을 DB.batch로 저장한다. 저장 실패 후 원결과를 재조회한다.
- offline-ad-service.ts 및 combat-ad-service.ts: 기존 기회·생성 영수증 복구가 신규 insert보다 앞에 있고 새 기회는 batch로 기록한다. AD_REQUEST와 PURCHASE_ONLY를 함께 다뤄야 한다.
- ad-start-approval.ts: 기존 승인 영수증 재조회와 신규 ad_start_approvals insert가 분리되어 있다. 기회 생성만 막으면 이미 생성된 기회로 새 광고를 시작할 수 있으므로 별도 경계다.
- ad-skip-purchase.ts: checkout 생성과 검증된 store 주문/권한 적용이 분리되어 있다. 후자는 기존 거래 복구이며 신규 checkout 차단과 구분해야 한다.
- 0041_ad_runtime_policy.sql: 기존 광고 정책은 DB trigger로 같은 승인 경계에서 검증한다. 이 광고 정책 소유자는 그대로 유지한다.
- maintenance-plan-store.ts: SAVE_DRAFT 이외 명령은 현재503. maintenance-plan-policy.ts의 application은 UNCONNECTED다.

## 선택한 구조와 대안

추천: 권위 있는 실효 정책 저장소 + 신규 승인 batch 내부의 DB guard. 사전 읽기는 안내에만 사용하며 최종 허용은 같은 트랜잭션 안에서 판단한다.
대안인 HTTP middleware 전면 차단은 원결과 재조회와 콜백까지 막을 수 있다. 애플리케이션 사전 조회만 추가하면 조회 후 정책 전환 경합이 남는다. 따라서 둘 다 최종 권한 검사로 사용하지 않는다.

## 소유권과 저장 계약

MaintenanceRuntimePolicyStore가 실효 정책의 유일한 쓰기 소유자다. 초안 저장소는 편집본만 소유한다. game adapter는 정책을 읽고 DB guard를 구성하며 정책을 변경하지 않는다. 공개 표시·캐시는 향후 동일 개정의 파생 데이터이며 별도 진실이 아니다.
첫 GAME 범위는 명시 environment/applicationId별 단일 head와 불변 개정으로 제안한다. 일정, 상태, 원초안 ID/개정, policyRevision, 실제 종료/취소시각을 보관한다. 개정 변경은 expectedRevision, 감사, 원영수증과 원자적으로 수행해야 한다. 같은 대상의 중복 활성 정책을 허용하지 않는다. WEB은 이 첫 저장 guard의 제어 대상에서 제외하며 향후 범위 교집합 계약 없이 임의 확장하지 않는다.
정책 누락/손상/환경 불일치는 정상으로 추정하지 않는다. 신규 요청은 unavailable로 거절한다. 정상 상태도 명시적인 검증된 개정으로 표현한다. 기존 결과 재조회와 거래 복구는 새 정책 누락만으로 막지 않되 기존 인증·기한·저장소 오류 계약을 유지한다.
신규 스키마에는 임의 운영 정상/점검 seed를 넣지 않는다. 로컬 fixture만 TEST_ONLY 초기 상태를 만든다. 원격 적용 전에는 명시적인 정상 정책 초기화·이관/코드 배포 순서와 복구를 별도 검증해야 한다. 따라서 이 단위 완료 코드를 현재 DEV에 바로 배포하면 안 된다.

## 승인 경계

1. 기존 인증과 입력 검증 후, 같은 계정·원키·원본문에 결합된 기존 영수증을 확인한다. 기존 영수증은 현재 점검 때문에 새 결과로 바꾸지 않는다.
2. 신규 분기에서는 상태와 시각을 현재 DB에서 확인하는 guard를 비용·preview 소비·새 run/intent/checkout/광고승인 insert와 같은 batch에 포함한다.
3. DB 평가 시각을 기준으로 drainStartsAt 이상이면 신규 요청을 차단한다. worker의 예약 실행이나 요청 진입 때의 Date.now 값에 의존하지 않는다. estimatedEndAt는 해제 조건이 아니다.
4. 점검 개정 저장과 신규 승인 저장의 DB 순서로 승패를 확정한다. 점검이 먼저면 신규 쓰기 전체가 rollback, 요청 승인이 먼저면 그 원결과는 유지한다.
5. batch 실패 후에는 기존 코드의 원영수증 복구를 먼저 수행한다. 점검 guard의 알려진 실패만 점검 오류로 변환하고 일반 저장 장애를 점검으로 위장하지 않는다. 구체 오류코드/HTTP envelope는 구현 계획에서 각 기존 응답 형식에 맞춘다.
6. DB와 실행 설정에서 확인된 environment/applicationId만 사용한다. 현재 runs에는 environment 열이 없으므로 raw runs trigger에서 환경을 추정하지 않는다. START는 명시된 배포 문맥을 검증한 guard statement로 처리한다. 환경 열이 있는 기회도 외부 입력만으로 다른 환경의 정책을 선택하게 하지 않는다.

## 경로별 연결 범위

| 경로 | 신규 차단 경계 | 보존할 분기 |
|---|---|---|
| START | preview 소비/비용/run/snapshot/영수증과 같은 batch | 기존 성공 영수증 및 저장 경합 뒤 원결과 재조회 |
| OFFLINE/COMBAT 기회 | AD_REQUEST/PURCHASE_ONLY 새 intent insert batch | 기존 동일 기회와 생성 영수증 복구 |
| 광고 시작 승인 | 새 ad_start_approvals insert | 기존 승인 재조회, 유효 SSV |
| 구매 시작 | 새 checkout 승인 batch | 검증된 기존 store 주문/원거래 복구 |

신규/복구의 구분을 클라이언트 operation 라벨이나 Idempotency-Key 존재만으로 결정하지 않는다. 원거래 소유자의 기존 조회와 binding 검증이 증거다. 기존 광고 OFF/공시 pin/전투 readiness/스토어 검증은 그대로 독립적으로 강제한다.

## 필수 격리 검증

- 사전 조회 후 정책 개정이 차단으로 바뀐 경우, 새 run/intent/checkout/비용/영수증 모두0.
- 사전 조회 후 drain 시각을 넘겨도 저장 경계에서 차단; 경계 동일 시각도 차단.
- 승인 저장 후 점검으로 바뀌고 응답이 유실된 경우, 같은 키는 원영수증을 반환하고 중복 지급/비용0.
- 점검 거절과 동시에 다른 동일 요청의 성공이 관찰되면 원결과 복구 우선.
- 다른 계정/본문/환경/기회로 키를 재사용하면 기존 계약대로 거절.
- 정책 누락·손상·감사실패 시 신규 승인/개정 부분 저장 없음.
- 기존 SSV/store 복구는 검증·만료·멱등 계약을 통과한 경우만 처리하며 무조건200 처리 금지.
- 종료 예상시각 이후 차단 유지, 명시적 정상 개정만 신규 차단 해제.
- 기존 광고OFF/전투readiness/공시pin 회귀 및 Admin ACTIVATE503 유지.

## 남는 경계와 검토 결과

첫 단위가 통과해도 본점검의 모든 게임 쓰기 차단, 전체 전투 checkpoint, 기존 작전 기한 보정, 공개 상태 outbox, WEB 정책, 실제 D1 동시성/배포 리허설은 미완료다. 실제 ACTIVATE는 계속 닫힌 상태다. 구버전 writer가 새 guard를 사용하지 않는 기간에는 경합 안전을 주장하지 않는다. 모든 신규 승인 경로의 목록을 구현 전에 고정하고 배포 호환 계획에 포함해야 한다.
자체 대조: 초안/실효 정책 구분, 원결과 복구 순서, 광고 기회와 실제 광고 시작의 이중 경계, 구매 시작/거래 복구 분리, runs 환경 열 부재, 명시적 초기화 필요를 반영했다. 실제 D1 직렬화 근거와 남은 신규 구매 경로 전수 목록은 구현 계획 전 확인사항이며 현재 원격 검증 완료가 아니다.
