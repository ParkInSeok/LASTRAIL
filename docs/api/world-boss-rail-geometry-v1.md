# 월드보스 mainline+detour geometry v1

S-WB-GEO-01 / 승인44#world-boss-mainline-detour-approved-20261002. 별도버전 `world-boss.rail-geometry.v1.20261002`. LOCAL_TEST_PROFILE만, 운영발행/실HTTP/DB/health/View 연결 미완료. 기존 world-boss.rail-action.v3.20261002·원4핀/18fixture 및 구snapshot은 바꾸지 않는다.

## 고정 최소 데이터

profile: version/status=LOCAL_TEST_PROFILE/geometryRef/contentVersion/roundId, 좌표 frame=PATH_LOCAL_FORWARD_RIGHT_MM(진행+x, 운전자우측+y; 화면투영 없음), paths.LEFT/RIGHT. 각 path는 points[{sMm,xMm,yMm}], splitSmm/mergeSmm. 두 path는 접근구간·분기점·합류점·합류이후 좌표가 일치해야 하며 s는 각 경로의 누적 실제 길이. 각 segment의 s차는 유클리드 거리의 mm반올림과1mm내 오차, x단조증가/역진행0. LEFT는 기존mainline, RIGHT는 우측으로 떨어졌다 합류하는detour. ENTRY는 LEFT의별칭이며 세 번째 points/command 없음. 현 profile은 직선기존선+전방우회선 시험구조, 임의 모든8패턴 geometry로 확대하지 않는다.

consist: vehicles[{id,lengthMm,gapAfterMm,halfWidthMm}], 처음은 기관차이며 순서고정, 마지막gap0. 최대개수·길이·폭은 시험profile에 명시한 maxVehicles/maxConsistLengthMm로 검증한다. 전단은 headDistanceMm에서 앞차length+gap 누적을 뺀 값, 후단은 전단-length. 각 전후단/중간구간은 선택한 같은 arc경로를 따른다. 화면 오프셋/중심점/차량별 임의lag 사용0.

movement: 최초 sample 및 후속 sample {atMs,sequence,headDistanceMm,distanceRemainder2000,actualMmPerSecond,targetMmPerSecond,forcedSlowdownBps}. 시각/거리는 단조, 원동일sample 중복은 원snapshot, 같은sequence 다른body는충돌. 속도0 허용/시간계속/전진거리0. 실제속도·목표·강제감속은 이동owner에서 전달, Server는 검증하며 표시/목표명령을 생성하지 않는다. 샘플 사이 실제속도는 선형가감속 구간이고 가감속/목표변경 경계에서 owner가 샘플을 나눠야 한다. `deltaMm=floor(((previousActual+actual)*deltaMs+previousRemainder)/2000)`, remainder는 mod2000. maxSpeed/maxAcceleration/maxSampleMs는 시험profile의 명시값으로검증. 거리 보고와 이 누적치가 어긋나면 거절하며 snapshot갱신0.

선택은 기존 v3 server receipt에서 수락된 LEFT/RIGHT를 이동owner가 따라야 한다. geometry 모델의 selectRoute는 신뢰된 local adapter 경계이며 공개클라이언트 명령이 아니다. head가split을통과한뒤선택변경금지·확정후변경금지, ENTRY무입력은 기존선지속. 같은수락경로 중복호출은멱등. 다른경로로tail/객차를따로분리하지않음. 실제명령+geometry+원receipt의원자저장은별도단위로연결한다.

snapshot: version/profile/selectedRoute(ENTRY/LEFT/RIGHT)/routeConfirmed/sample, vehicles 전후단좌표, events. 사건은 HEAD_SPLIT/Tail split TAIL_SPLIT/TAIL_MERGE를 각각기록하고 단일eventId=[geometryRef,roundId,kind] JSON tuple, observedAtMs 및previousSampleAtMs 포함. 이는 해당샘플구간 안에서 통과해 마지막샘플에 관측했다는 의미이며 임의 정확교차시각 추정아님. 정확기한·피격 판정에는 해당시각 sample을 반드시 공급한다. 다음분기 허용은 **마지막후미 merge 이탈**(rearS≥mergeS) 이후만. 그림에서junction이후방인것과별도다. 동일distance로멈춰도시간진행/중복event0.

## 포격·기존결과 결속

profile.attack: impactAtMs 및 areas[{minXmm,maxXmm,minYmm,maxYmm}] 시험위험영역. 해당시각의 차량 전후단이 차지하는 경로segment를 차량halfWidth로확장한영역과 대조한다. point/기관차중심만으로 회피성공을 선언하지 않는다. 약탈왕1 정의는 기존선 전방영역과교차/RIGHT중간회피부는공격밖. 긴편성은 RIGHT를 선택해도 뒤객차가공격범위에있으면피격이다. geometry 계약은 올바른경로버튼만으로실회피를가정하지않는다.

`observeAttack`는 sample.atMs==impactAtMs에만 결과를 고정하며 원event/원result재생, 실제HP/실드실행0. LEFT와ENTRY는같은물리path/같은공격예산. `bindGeometryResult`는 기존v3 snapshot의roundId/contentVersion/수락route·resolved result와geometry 원관측을대조한다. 판정불일치는 geometry_result_conflict로차단하고 v3를몰래변경/재발급하지않는다. 일치하면 기존result/intent/원buff와 geometry evidence를함께돌려준다. 원v3 LOCAL_LOGICAL_ONLY 플래그는역사로보존, 새결속evidence만 LOCAL_PATH_VERIFIED가된다. 이값은실Unity/운영geometry검증완료가아님.

## 소유·인수·잔여

Server는 이형식/판정/이동snapshot검증·권위event 원본, Unity U-WB-GEO-01은같은계약으로 순수거리/가감속·차량후미/pose 모델을구현한다. client adapter는실제Server값을소비하며geometry/source/authority를새로정의하지않는다. 로컬 fixed path/vector는오직시험값, 이전2~4객차/7~13m/s/lag도운영승인값아님. 수치허용범위·포격계수·최대편성/속도는측정후운영결정필요.

검증목표: 연속경로/카메라분리·ENTRYalias·head/tail/merge분리·긴편성/정지/가감속·거리잔여/중복/충돌/복구·기관차만회피했을때뒤차피격·RIGHT실회피/LEFT·ENTRY포격·기한정확시각·기존결과불변결속. HTTP 인증/영속DB 경합/실health/실입장/보상/실기미술은별도. 기존시안/DEV OWNER 인증을이로컬구현 선행으로두지않는다.
<a id="world-boss-geometry-final-contract-20261002"></a>
## 최종 로컬 인수 조건 보완 — 2026-10-02

기존 최소 필드와 버전은 유지한다. 실제 snapshot은 initial/samples/selectedAtSequence/physicalRoute/attackResult/nextBranchAllowed를 포함한다. 앞선 routeConfirmed 표현은 selectedAtSequence!=null에서 파생한 설명이며 별도 wire 필드가 아니다. 동일 sequence/body 재전송은 상태를 변경하거나 이벤트를 재발급하지 않고 현재 snapshot을 반환한다. 과거 sample 시점의 snapshot을 별도 발급하지 않는다. observeAttack은 원 관측 result를 재생한다.

**작전 식별자:** 기존 v3 결과에 결속하는 profile.geometryRef는 신뢰된 생성자에서 JSON.stringify([attemptId,roundId])로 만든 작전/회차 전용 식별자다. 최대 길이는 v3 두 ID 각각200자의 JSON escape를 수용하는2407자. LOCAL_* 같은 독립 시험 ref는 계측만 가능하며 v3에 결속하지 않는다. 별도 경로 catalog ID를 geometryRef와 혼동하지 않는다. 사건 tuple에 이 ref가 들어가므로 다른 작전 사건과 충돌하지 않는다. bindGeometryResult는 contentVersion/round/route/result와 ref를 확인하고, 선택 sample.atMs가 원 route receipt.acceptedAt와 정확히 같아야 한다. ENTRY에는 수락 receipt/선택 sample이 없어야 한다. 포격 관측은 해당 회차 absolute lockAt 이후·resolvedAt 이전 범위에서만 결속한다. 공개 HTTP/DB 원자 저장은 별도다.

**안전창 검사:** validateTraversalWindow 입력은 previewAtMs/lockAtMs/impactAtMs/nextPreviewAtMs/headAtPreviewMm/maxAllowedConsistLengthMm/guaranteedMinSpeedMmPerSecond/maxAllowedSpeedMmPerSecond/latencyMs의 필수9필드 strict object다. 누락/추가/음수/NaN/비정수는 입력 거절. 특정 profile의 실제 vehicles 길이+gap와 maxAllowedConsistLengthMm가 정확히 같아야 한다. 다른 폭/차량 조합에 이 근거를 재사용하지 말고 각 고정 편성별로 검사한다. 선언 최대 편성 길이>=접근 split이면 PROFILE_APPROACH_CAPACITY로 거절한다.

차량별 halfWidth로 확장한 공격 사각형과 각 path segment의 교차 arc 구간을 구한다. 이를 해당 차량 front offset/length로 head 거리의 폐쇄 위험 구간으로 변환하고 전체 차량/영역의 합집합 forbiddenHeadWindows를 만든다. 경계 접촉은 피격이다. 허용 속도 하한/상한과 latency를 적용한 impact 도달 거리 [minimumHeadAtImpact,maximumHeadAtImpact]가 이 합집합과 조금이라도 교차하면 지원하지 않는다. 중간 우회 안전창을 마지막 전체 경로 위험점으로 지워 버리지 않는다. 정지/하한 미만 slowdown을 허용하는 운영에서는 이 보장이 성립하지 않는다. 실제 sample 가감속/slowdown 상태 검증은 이동 소유자 및 후속 권위 adapter에서 유지한다.

impactSupported는 포격 시각의 전체편성 안전 보장만 뜻한다. supported는 여기에 확정기한 이전 head split 미통과, 다음 예고 이전 마지막 tail merge, 최고속 nextPreview까지 경로 끝 미초과를 모두 요구한다. PATH_ENVELOPE_EXHAUSTED/NEXT_BRANCH_BEFORE_TAIL_MERGE가 있으면 impactSupported가 true여도 전체 supported는 false다. safeHeadWindows는 위험구간 사이의 유한 열린 head 거리창과 그에 대응하는 정수 시간창을 설명한다. 빈 창이나 모든 reason은 유효성 실패 근거이며 피할 수 없는 피격을 허용하는 승인 정책이 아니다.

**실측 후보:** Unity 100m 접근/6차량60m/폭500mm/실제 속도10~20m/s 후보의 지연0 공통 회피창22113..31224ms, 지연500ms를 포함하면22613..30724ms. 시험 impact25000ms는 안전. 단 기존705612mm RIGHT 끝은 nextPreview65000ms 최고속 head1170000mm를 수용하지 못해 전체 supported=false. 공통 suffix를 양쪽 모두600000mm 늘린 별도 LOCAL_WIDE_EXTENDED_TRIAL 후보는 동일 포격/분기/합류를 보존하며 전체 로컬 bounds 검사를 통과한다. 이 숫자들은 LOCAL_TEST_PROFILE 측정값이며 운영 승인값이 아니다. 원40m 접근/선언60m 후보는 최대편성 수용 불가, 긴편성 후미피격 trace는 테스트 오류 검출용으로 보존한다.

최종 인수 근거는 QA/CommanderS1-20260922/world-boss-geometry-validation-20261002.json과 world-boss-geometry-fixtures-20261002.json에서 source SHA/버전/실행 결과를 확인한다. 이전 v3 4핀/18fixture/78검증, Unity73 및 C3·고정 DEV배포 후보는 수정하지 않는다. LOCAL_PATH_VERIFIED는 결속된 로컬 geometry evidence만의 상태이며 실제 world HTTP/저장/HP/View 완료를 뜻하지 않는다.