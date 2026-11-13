# B 첫 작전 버전·진행 집계 계약 — S24

승인 원문: 44_COMBAT_DETAIL_CONTRACTS.md의 combat-b-geometry-operation-approved-v09-20261004. 이 문서는 신규 버전/집계 공급 단위이며 운영 입장 지원 승인과 다르다.

## 버전과 원본

- operationId: opn_01_last_departure, 기존 스토리 정체성 보존.
- contentVersion: act1.opn01.b12.content.v1.20261004.
- profileVersion: opn_01_last_departure.b12.profile.v1.20261004.
- binding: operation.b-story12-binding.v1.20261004 / mode STORY_B_12.
- sourceVersion: combat.b-scout-gunner-source.v3.20261004.
- progress: operation.b-progress.v1.20261004 / wave receipt: operation.b-wave-receipt.v1.20261004.

신규 binding은 기존 OperationMapPublisher의 불변 published ORDINARY_12_NODE map contentHash, 모든 node wave 계약, 기존 rewardReference hash 및 catalogRevision을 결속한다. depth12/final node는 그래프 전체 노드 수12를 뜻하지 않는다. 일반전은2, 비전투 이벤트는0; ELITE/BOSS는 해당 불변 encounter 정의의 completedWaveContract {version,completedWaves}가 있을 때만 사용한다. 없다면 null/명시 wave_contract_unsupported이며 추정0으로 완료시키지 않는다.

readBOperationBinding은 원 snapshot 및 full combat-authority 계약·publication·reward hash를 검증한다. 오늘 catalog를 조회하여 진행중 원본을 바꾸지 않는다. 신규 B prefix의 미지원 버전/모드/훼손된 binding은 오류, 기존 원15wave run/quote는 원 분기 유지. sourceVersion 결속은 필수 공급 버전 선언이며 실제 accepted source/geometry/ability/support CAS 연결은 후속이다.

## API / 인증 / 오류

기존 POST /v1/operations/preview body의 operationId/risk/expectedFormationRevision/expectedLoadoutRevision/expectedUltimateRevision에 명시 contentVersion을 선택적으로 추가한다. 신규 버전은 opn_01에서만 사용한다. 기존 무필드 body와 wire/멱등 hash는 보존된다. 기존 Bearer/account ownership/Idempotency-Key·선택 revision·balance·fuel 정책을 재사용한다. 신규 응답에는 원 rewardReference/quote와 operationMapContent/bOperationBinding 및 runtimeSupported:false/canStart:false/missing이 추가된다. 활성 immutable Server catalog가 없으면409 combat_b_operation_catalog_unavailable. 카탈로그는 공개 쓰기 API나 migration seed 없이 내부 owner가 관리하고 quote batch에서 exact revision/publication CAS를 검사한다. 기존 성공 quote replay는 현재 catalog 철회 뒤에도 원응답을 반환한다. metadata quote는 실제 출격·연료 차감이 아니다.

인증 GET /v1/operations/b-story12-contract 및 GET /v1/runs/:runId/b-operation-progress: X-B-Operation-Version=operation.b-story12-binding.v1.20261004 필수, query/body 금지, Cache-Control:no-store, ETag/packetHash는 canonical packet SHA256. If-Match는 기존 bare64hash 규약. 조회 후 재인증하고 동일 계정인지 확인한다. 계약 GET은 공급 버전/definitionAvailable:true와 미지원 목록을 반환한다. progress GET은 본인 run만, bindingHash/mapRevision/completedWaves/completedNodes/fullCompletion과 미지원 정보를 반환한다. 해당 공개 완료 POST는 없다.

401 인증 실패/철회;400 contract_violation;409 combat_b_operation_version_conflict/stale/run_not_found/binding_invalid/binding_conflict/mode_unsupported/version_unsupported/progress_conflict. 원 MapStore 실패 코드 operation_map_*는 유지한다. response accepted:false/errorCode 규약 재사용. 운영 DEV 배포/활성 catalog 공급은 이번 범위에 없으며 이 계약의 신규 실행 환경 근거는 격리 SQL+signed loopback TCP QA다. 임시 loopback 주소를 고정 서비스 URL로 인계하지 않는다.

## 진행 소유권·멱등·원자성

MapStore만 노드 상태를 변경한다. BOperationProgressStore.completeWave는 내부 원 solo 완료 증거 verifier가 준비한 nonempty SQL source assertions를 같은 transaction에 넣어 원 account/run ACTIVE, 원 snapshot bytes, map revision/state/plan 및 이전 wave count를 CAS한다. verifier는 실제 wave 목표·잔류위해를 검증하고 검증한 모든 원본을 SQL guard로 fence해야 한다. 현재 production adapter 미등록이며 LOCAL 시험의 unconditional map proof/합성 wave evidence를 운영으로 사용하지 않는다.

wave event key=(runId,eventId), wave slot key=(runId,nodeId,waveOrdinal). 같은 command 재전송은 원 receipt, 다른 command로 같은 event는 event_conflict, 다른 event로 같은 slot/역순은 거절. 현재 ACTIVE node만 wave를 받을 수 있다. 노드 완료는 원 MapStore COMPLETE와 같은 transaction에서 wave 수/계약을 확인하고 node key=(runId,nodeId), node event key=(runId,eventId)를 삽입한다. node receipt/원 map receipt/state 중 하나 실패 시 전부 rollback. 순수 arrival/travel/boarding/중간 empty 상태는 node 완료0. MERCHANT 등 비전투 완료는 node1/wave0. 원 event replay는 완료뒤에도 다시 집계하지 않는다.

완료 수는 두 불변 journal에서 각각 도출한다. 전체 선택 경로12개가 모두 완료되고 마지막 원 목표 완료·hold0·FINAL_NODE_COMPLETE여야 fullCompletion=true. 한 wave/node는 전체 승리가 아니다. 다른 branch/run/binding의 receipt는 집계 오류. 원 map/source는 복원 시 바꾸지 않는다.

## 목표와 기존 정산 보존

objectiveFinishEvent는 caller operationId만으로 신규 모드를 결정하지 않고 본인 run의 검증된 원 snapshot으로 분기한다. 신규 input version/wave/fullCompletion은 원 집계와 일치해야 한다. VICTORY는 전체 목표 필요, ends1/실제 waves. FAILURE는 UNSUPPORTED_B_STORY_DEFEAT/목표 ends0/waves0. ABANDONED는 NOT_ELIGIBLE. 미확정 패배 목표권한을 추가하지 않는다. 실제 run_final_results.outcome·snapshot bytes·map state/revision·양쪽 journal count를 원 finish batch 안에서 fence하고 변화 시 SQL 실패로 rollback한다.

Migration0100은 objective_events 기존 waves 0..15 제약·payload·immutable trigger를 유지하고 b_completed_waves nullable0..1536(최대12node×128 원wave)를 추가한다. 신규 정확 B mode/version만 별도 값을 저장하며 기존 waves는0. 기존 이벤트는NULL이고 기존 집계 그대로. recurring read/sync는 COALESCE(b_completed_waves,waves)를 같은 기존 owner에서 읽는다. 기존 run_final_results.cleared_waves 0..15와 보상 정산/원 rewardPlan·balance 금액/배율은 바꾸지 않는다. LOCAL full victory 시험의 cleared_waves=15는 합성 구 정산 reference units이며 신규 24 실제 waves→15 지급 환산 정책을 승인하거나 구현한 것이 아니다. 운영 fresh START와 기존 finish admission guard를 해제하지 않는다. 실제 신규 mode final-result/report→기존 보상 progress 연결은 원 정산 semantics 확인 후 named supported profile 통합에 포함한다.

## 남은 후속

Unity U27 registry-candidate-v02는 기존 TEST asset/private-car-5/private-type·2000us/시험48000mm 원본 증거로만 보존한다. 실제 운영 차량 geometry, Kyle/Sable 실제 accepted native profile 및 all-reachable node execution/reward validation, S19 원 source/start CAS 결속이 필요하다. named 실제 지원 ID/치수/원 field를 승인하고 마지막 선행 완료 후 단일 support-registry 후속을 등록한다. 전체 버전/집계 구현을 이 선행 대기로 되돌리지 않는다. 상세 조건과 고정 등록키는 기존 WAIT의 S24 결과 절을 따른다.
## S26 최신 승인 정책 — 정상 패배의 완료 실적 / 2026-10-04

위 S24의 FAILURE 미확정/UNSUPPORTED_B_STORY_DEFEAT 설명은 신규 종결에 대해 이 절로 대체한다. 원승인 combat-b-defeat-completed-progress-approved-20261004, 정책 operation.b-completed-progress.v1.20261004. 원 binding/content/profile/source 버전 및 wave/node 원본은 유지한다. 계약/progress GET의 지원 정보는 defeatObjectiveSupported:true 및 objectiveProgressPolicyVersion을 포함하지만 runtimeSupported/canStart는 여전히false이다.

정상 FAILURE는 원 BOperationProgressStore로 검증된 completedWaves/Nodes만 COMPLETED_PROGRESS_ELIGIBLE로 저장한다. 미완료 현재wave/node는0, 기존완료값은 보존한다. 별도 b_completed_waves/b_completed_nodes와 b_progress_policy_version, source_json.objectiveProgress에 적용정책과목표별수치를 기록한다. 기존 ends=0/waves=0, 실제 승리/클리어/최종보스 목표 추가0. VICTORY 원전체목표조건은 유지하고 ends1/원완료수를 사용한다. ABANDONED/INVALID/CANCELLED는 NOT_ELIGIBLE와실적0 유지.

공통 recurringMetricValue는 완료실적-only event의 waves만 소비한다. 기존 ELIGIBLE event의 login/ends/waves 의미는 보존한다. 원 W3(완료웨이브60) read/sync가 정상 패배 waves를 인정하며 D2/D3/W2(작전종료 횟수)는 패배로 올리지 않는다. 승리횟수 목표를 신규 만들지 않는다. 현재 node 기반 목표는 없으며 검증완료node 저장값을미래실적목표로자동승격하지않는다.

Migration0101은 nodes/policy 필드만 가산하고 버전 guard를 갱신한다. 역사 UNSUPPORTED_B_STORY_DEFEAT의0/옛policy null 및 기존 events/payload는 재집계·수정하지 않는다. 동일run finish/retry/restore는 기존 unique source로 1회, 원 finish snapshot/상태/receipt count/final outcome CAS와 원 실패롤백을 유지한다. 원 rewardReference·run_final_results.cleared_waves0..15·패배정산/금액/배율과24→15환산미결은 변경0. 운영 실적 재집계·실지급·배포·START 해제0.