# 전투·증강·경로 원문 보완 계약

> **2026-09-22 모드별 판정 담당 정정:** 일반·스토리 솔로는 기존 로컬 전투, 일반 협동은 단일 Unity 호스트 전투, 월드보스 솔로·협동은 서버 전투 판정이다. 서버의 스탯/정의·선택/거래·진행/결과 검증과 실시간 HP 계산 담당을 구분한다. 이 문서의 모드 미구분 ‘권위/서버 처리’는 [46 최신 사용자 확정](46_COOP_DETAIL_CONTRACTS.md#authority-split-host-coop-v01)을 따른다. 구run은 원 START 정책 유지.

작성: 2026-09-21. 현재 04·05·14의 승인 결정을 보완한다. 이 문서는 회수된 원문의 **정책·계약 경계**이며 구현 완료, 게임성 검증 완료 또는 출시 밸런스 승인을 뜻하지 않는다. 관련 원문 116개를 전체 내용으로 검토했지만 원문이 참조하는 모든 코드·자산·외부 의존성을 재검증한 것은 아니다. 아래 링크는 보존된 상세 원문으로 연결된다.

## 1. 적용 순서와 승인 범위

현재 사용자 정정과 04·05·14의 명시적 결정이 오래된 원문보다 우선한다. 원문의 필드·오류·멱등·복구·수명 계약은 요약에서 빠졌다고 폐기되지 않는다. 충돌하는 정책 문장만 아래처럼 대체하고, 이미 승인된 요청·응답과 저장된 실행의 버전은 소급 변경하지 않는다.

증강은 플레이어 판단, 반복 운영 방식, 손익과 조합을 바꾸어야 한다. 높은 피해 수치·문서·기술 테스트만으로 철학 준수를 판정하지 않는다. 실제 조작과 비교 플레이가 필요하며 후보 추천·가중 유도는 허용하지 않는다. [LASTRAIL-CORE-COMBAT-PHILOSOPHY.md](30_PROJECT_RULES.md#source-9b8fb18db0c66c5c)

고정 전투 대형과 독립적인 실시간 행동, 다음 공격부터 적용되는 집중 대상, 이미 확정된 공격 대상 유지, 일반 피격만으로 행동 취소하지 않는 방향은 보존한다. 수동 액티브를 전제로 작성된 옛 입력 문서는 현재 13의 자동 액티브·마나 계약과 호환 확인 없이 다시 적용하지 않는다. 시안에서 보이는 인물·역할이 기술의 효과 소유자를 변경하지 않는다. [combat-control-detail-proposal-20260909.md](44_COMBAT_DETAIL_CONTRACTS.md#source-716afa118bd7a8bb) [combat-skill-control-proposal-20260909.md](44_COMBAT_DETAIL_CONTRACTS.md#source-28efa930bf7b4071) [AUGMENT-ACTOR-WEAPON-MAPPING-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-14f939bc39c7134953d2)

## 2. 피해·파괴·회복

오퍼레이터 HP, 객차 HP, 열차 HP는 별도 상태다. 객차 피해를 방어·실드 처리한 뒤 실제 객차 HP 감소를 C=min(피격 전 객차 HP, 방어·실드 후 손실량)로 확정하고, 열차 전이를 floor(C×30/100)으로 계산한다. 전이에 방어·실드를 다시 적용하지 않고 직접 열차 피해와 이중 지급하지 않는다. 실드만 소모했다면 전이 0이며, HP 20인 객차의 큰 피격은 실제 감소 20에 대한 열차 피해 6이다. HP 잔량을 넘은 방어 후 피해까지 전이하던 초기 제안은 대체된다. [TRAIN-DAMAGE-CARRIAGE-REPAIR-20260914.md](44_COMBAT_DETAIL_CONTRACTS.md#source-d25448b3ff1afe52) [carriage-disable-repair-proposal-20260909.md](44_COMBAT_DETAIL_CONTRACTS.md#source-ea7ae0446f7c3451) [combat-damage-resolution-proposal-20260909.md](44_COMBAT_DETAIL_CONTRACTS.md#source-55fa7474d833e11e)

새 대상 선택에서는 파괴 객차를 제외한다. 이미 대상으로 확정한 공격을 임의 재지정하지 않으며 파괴 객차에 도착한 공격의 객차 HP 감소는 0이다. 파괴 구간의 물리 장애물 직접 열차 피해는 별도 계약이고, 그 계수를 객차 전이 30%와 같다고 추론하지 않는다. [TRAIN-DAMAGE-CARRIAGE-REPAIR-20260914.md](44_COMBAT_DETAIL_CONTRACTS.md#source-d25448b3ff1afe52) [PHYSICAL-HAZARD-DAMAGE-20260915.md](50_RELEASE_OPERATIONS_CONTRACTS.md#source-05c99f4bad69cf6e)

객차 파괴는 새 장치 발동을 중단하되 이미 발사한 투사체·정상 만료 중인 효과를 소급 취소하지 않는다. 보상·인벤토리·오퍼레이터 전투를 함께 삭제하지 않는다. 포탑은 자기 HP와 객차 생존 조건을 모두 만족해야 한다(04 J01). 남은 포탑 HP로 파괴 객차에서 계속 작동할 수 없다. 장치 외부의 기본 긴급 수리 경로가 필요하며, 객차 회복이 이미 전이된 열차 손실을 환급하지 않는다. [carriage-device-function-proposal-20260909.md](44_COMBAT_DETAIL_CONTRACTS.md#source-1422c7ea457550ec) [TRAIN-DAMAGE-CARRIAGE-REPAIR-20260914.md](44_COMBAT_DETAIL_CONTRACTS.md#source-d25448b3ff1afe52)

일반 전투에서 전원 Down 자체는 즉시 실패가 아니며 열차 HP 0은 실패다. 일반 치유를 Down 부활로 취급하거나 웨이브 경계에서 임의 완치하지 않는다. 회복 6/9/12초·50%·2초는 사용자 수용 **첫 테스트 기준**이고 출시 전체 기본값이 아니다. 객차 10/15/20초·40%·2초는 별도 제안이다. 월드 보스 규칙을 일반 전투로 확장하지 않는다. [combat-health-recovery-proposal-20260909.md](44_COMBAT_DETAIL_CONTRACTS.md#source-e7a419b8cc8f45de) [TRAIN-DAMAGE-CARRIAGE-REPAIR-20260914.md](44_COMBAT_DETAIL_CONTRACTS.md#source-d25448b3ff1afe52)

## 3. 증강 풀·카드·효과

최초 후보는 적격 통합 풀에서 동일 확률의 서로 다른 3개다. 유형을 먼저 50:50으로 뽑거나 유형 연속 최대 2회, 희귀도·조합 추천 가중치를 적용하는 옛 정책은 대체된다. 리롤은 해당 카드 슬롯만 바꾸고 다른 두 후보를 유지한다. 카드 터치만 선택이며 리롤 버튼 이벤트가 선택으로 전파되지 않는다. 카드별 FREE 3 + EXTRA 1, EXTRA 1에 대한 광고/구매 대체 승인, 신규 지급 시 초기화·재진입 시 유지, 실제 효과 적용까지 완전 정지는 05를 따른다. [AUGMENT-UNIFIED-POOL-POLICY-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-fe9b0192772c8ac867d2) [AUGMENT-TYPE-RANDOM-POLICY-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-eccce2e1f7a560dd41f3) [AUGMENT-SELECTION-FINAL-20260915.md](51_UI_ART_DETAIL_CONTRACTS.md#source-f2447fd0ba0513fa) [AUGMENT-POOL-40-POLICY-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-d3f826b0ed60dea628b0)

40개(열차 20·오퍼레이터 20)는 설계 후보 카탈로그이며 40개 효과 구현·자산 완료·모든 조합 검증을 뜻하지 않는다. 초기 수치안→플레이 방식 개정→조합 개정→마지막 8개 상세 순서의 수정 이력을 보존한다. 과거 16개 상한, 고정 5회·시작 지급·모든 경로 5회 보장은 현재 랜덤 맵 이벤트 정책으로 대체한다. 과거 74/496·85/496 조합 검토와 5픽 예시는 전체 조합 또는 지급 횟수 증명이 아니다. [AUGMENT-16-PLAYSTYLE-REVISION-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-4267c1b530490dbabbc0) [AUGMENT-COMPOSITION-REVISION-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-adb1d1d1b83e43eaeed4) [AUGMENT-40-CANDIDATE-CATALOG-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-7a0f5750072250b2703f) [AUGMENT-LAST8-FIVE-PICK-REVIEW-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-61aad0ca7b2004c3464e) [AUGMENT-32-COMBINATION-AUDIT-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-66700fb74ccb913cc621) [AUGMENT-32-REAUDIT-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-f834db248a1ef09da86d) [RUN-MAP-AUGMENT-EVENTS-20260916.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-c14755757c9a8445)

다음은 효과 설계가 채택될 경우 보존할 후보 계약이며, 일괄 승인된 런타임 규칙은 아니다.

- 한 정상 공격 기회와 하나의 최종 공격 형태를 소유한다. 보조 효과를 무료 추가 공격으로 만들지 않는다. T13/O16/T16의 후속 조합 개정과 Gun 기회 고갈 우려를 함께 보존한다. [AUGMENT-COMPOSITION-REVISION-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-adb1d1d1b83e43eaeed4)
- T06의 자원 1과 폐철·수리·파편 예산은 작전 보급품 지갑과 동일시하지 않는다. 인과 식별자로 자기 효과 재충전을 막는다. [AUGMENT-SCRAP-REPAIR-REVISION-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-47e0aeacaeb8e62356dd)
- O05의 원래 양수 피해는 실드 적용 전 기준이므로 최종 HP 손실 0과 양립한다. 반대로 실제 치유·수리·HP 피해 표시에는 해당 실제 결과가 필요하다. O13 위협 미리보기를 실제 위협 없이 조작하지 않는다. [AUGMENT-GUARD-COUNTER-REVISION-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-6782e58f20301115d0bf)
- O09는 Due > Tick 상태의 실제 취소를 기준으로 하고 단순 이동을 중단으로 세지 않는다. O14는 웨이브·슬롯 영역에 고정되며 이동에 묶이지 않는다. [AUGMENT-SUPPRESSION-RESIDUAL-DETAIL-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-3d24094fa98888460860)
- T17 순증가 용량 B−A, O19 원래 소유·만료를 보존하는 원자적 파편 이전을 유지한다. T17 관련 O07 표기는 후속 원문의 T07 수정이 우선한다. [AUGMENT-SHIELD-ALLOCATION-TRANSFER-20260916.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-0ab763c6e3026e293bd4)
- O17의 실제 취소와 O20의 정상 공격 기회 소유·우선순위·해제 조건을 보존한다. [AUGMENT-OPENING-BANKED-FIRE-DETAIL-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-a1b4e96c9360b17049c5)
- **T18 HP 0 객차의 Gun 임시 작동은 04 J01과 충돌하는 미승인 후보다.** 정식 예외 승인이 없는 현재 정책에 편입할 수 없으며 재설계 또는 별도 결정이 필요하다. [AUGMENT-BACKUP-WIRING-REVISION-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-298775cf95f5786a0d3f)

선택 CONFIRMED는 효과 적용 완료가 아니다. 실제 양수 HP 변화가 없더라도 APPLIED가 될 수 있다. 이벤트·행동·원인·원천·버전·pending·receipt 필드를 잘라내지 않는다. 솔로 도메인용 trusted issuer, single-flight, revision 재검사, 체크포인트 신뢰 경계도 공개 API나 서버 저장 완료로 승격하지 않는다. [AUGMENT-STYLE-EFFECT-CONTRACT-SERVER-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-6852fb25f6d9474e7851) [2026-09-17-augment-reroll-server-phase2.md](48_AD_REWARD_DETAIL_CONTRACTS.md#source-a54cbe3073573484)

## 4. 물리 경로·속도·장애물

레버는 물리 경로 입력이며 증강 유형·선택·리롤과 결합하지 않는다. 목표 속도·실제 속도·안정화 상태와 게임 배속을 구분하고, 하나의 권위 있는 이동 거리로 전투·표시를 동기화한다. 경고 거리는 화면 진입이 아니라 최대 물리 속도×최대 게임 배속×실제 반응 시간에 제동 거리와 여유를 더해 판단한다. 열차 앞부분뿐 아니라 꼬리·투사체·이벤트 참조가 빠져나갈 때까지 구간을 유지한다. [TRAIN-COMBAT-ROUTE-SPEED-DETAIL-20260915.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-2dc80daf4baa73f3) [TRAIN-MAP-COMPOSITION-20260914.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-0166eada68646771) [TRAIN-SPEED-OBSTACLES-20260914.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-eeb6f5c756ecc820) [TRAIN-SPEED-OBSTACLES-DIRECTION-SUPPLY-20260914.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-8a4b19669359c82f)

경로 예약→선두 진입 잠금→꼬리 합류는 원문의 물리 처리 계약이다. 다만 옛 **2갈래/연속 자동 확정**과 현재 14의 **노드 3선택**, 04의 **명시 확인·0.01**은 동일한 규칙이라고 단정할 수 없다. 2026-09-22 사용자 승인으로 목적지3선택과 같은 목적지로 연결되는 구간 내 좌우 위험 대응을 분리한다([14 승인 범위](14_MAP_EVENTS_DROPS.md#destination-track-choice-v01)). 입력 제스처·변경 허용·시간 연결은 [후속 후보](14_MAP_EVENTS_DROPS.md#route-confirm-time-v01)로 남으며 옛 자동 확정을 복원하지 않는다. 옛 증강 1% 시간 진행은 현재 완전 정지에 의해 대체된다. [TRAIN-COMBAT-ROUTE-SPEED-DETAIL-20260915.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-2dc80daf4baa73f3) [TRAIN-ROUTE-ROCKFALL-20260914.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-018bb2d2f8ee2b6c) [TRAIN-ROUTE-ROCKFALL-DIRECTION-SUPPLY-20260914.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-233575477408fe34161d)

장애물은 Dormant/Warned/Active/Destroyed 또는 Collided/Cleared, 미선택 경로의 Skipped를 구분한다. 지나침을 처치로 세지 않는다. 프레임 간 구간 교차로 고속 관통 누락을 막고, 하나의 접촉에서 이벤트 소비·피해·감속을 각각 한 번 처리한다. 충돌 방어를 중복 적용하거나 숨은 지속 HP 차감을 추가하지 않는다. 피해 1.3배, 추가 20% 등 수치는 제안이며 확정값으로 만들지 않는다. [PHYSICAL-HAZARD-DAMAGE-20260915.md](50_RELEASE_OPERATIONS_CONTRACTS.md#source-05c99f4bad69cf6e) [TRAIN-ROUTE-ROCKFALL-20260914.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-018bb2d2f8ee2b6c) [TRAIN-SPEED-OBSTACLES-20260914.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-eeb6f5c756ecc820)

낙석 [S,E)·양수 차체 겹침·D>0 유리수 시간 비교·D=0 별도 처리·epsilon 금지는 별도 정밀 판정 제안의 계약이다. 과거 synthetic trace 통과를 실제 게임·출시 판정 검증으로 확대하지 않는다. [TRAIN-ROCKFALL-TIME-OVERLAP-20260914.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-5bbf69a7f11a96d5) [TRAIN-MAP-MECHANICS-LAB-20260914.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-3067ebf8a989e607) [TRAIN-MAP-UNITY-QA-HANDOFF-20260914.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-0c2b2b22feffb3dc3931)

생성된 최종 맵 그래프·모듈·seed·버전은 고정한다. 근처 스트리밍, 재접속, 미리보기로 다시 추첨하지 않는다. 연결점 변환·접선·높이·폭·태그, 안정적인 ID 순서, 제한된 되돌리기와 검증된 fallback/명시적 오류를 보존한다. 원문의 고정 증강 횟수 제한만 대체하며 실제 이벤트 지급은 run/eventInstanceId로 한 번 처리한다. [TRAIN-MAP-GENERATION-RESOURCE-SPEC-20260914.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-433428c2a8805d9c) [RUN-MAP-AUGMENT-EVENTS-20260916.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-c14755757c9a8445)

서사 Rail Choice의 두 선택지는 별도 내러티브 사건이다. 물리 3경로·증강 후보로 합치지 않는다. eventId/optionId/clientPresentedAt는 제안 필드이며 현재 API 존재를 뜻하지 않는다. [rail-choice-events.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-e3a34c241f00b5ad8056)

## 5. 작전 보급품·정차·현장 선택

작전 보급품은 런 소유 자원이고 영구 재화 전환·다음 런 이월·증강 구매/리롤·유료 보충으로 확장하지 않는다. 운영 방향은 채택되었으나 자원 ID·시작 수량·클리어 수급·상점 가격·정차 횟수는 제안이다. 솔로 시작 30과 협동 시작 20은 상충하는 제안이므로 임의로 하나를 승인값으로 정하지 않는다. [OPERATION-SUPPLIES-STATION-DETAIL-20260916.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-46d48e7324fb0802) [COOP-SUPPLIES-STATION-EVENTS-20260916.md](46_COOP_DETAIL_CONTRACTS.md#source-e5a41af1ba3b3528)

정차는 적·유해 투사체·꼬리 통과까지 안전 여부를 판정한 뒤 처리하고, 정차 중 완전 정지·쿨다운·자연 회복의 상세 제안은 실제 상태 머신에 별도로 반영·검증해야 한다. run/station 기반 재현 가능한 재고, 재진입 시 초기화 금지, 재화 차감+효과+재고+receipt 원자성, 무효 효과 구매 금지를 보존한다. 추천 상품으로 증강의 공정 선택을 우회하지 않는다. [OPERATION-SUPPLIES-STATION-DETAIL-20260916.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-46d48e7324fb0802)

M01–04 객차 개조의 객차별 1슬롯은 13의 오퍼레이터/포탑별 장비 3슬롯과 별도 개념이다. 개조 가격 60·수치 15는 제안이다. T10/T11과 동일 효과를 단순 유료 재판매하지 않는다. 현장 F01–04 선택·임시 객차 획득은 영구 계정 소유권 변경이 아니며 과거 화면의 객차 수를 최대 편성 제한으로 삼지 않는다. 선택 결과와 자원·객차 변경은 한 트랜잭션으로 처리한다. [CARRIAGE-MODIFICATIONS-STATION-20260916.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-9c24ca132b0c9a3d) [FIELD-EVENTS-CARRIAGE-RESCUE-20260916.md](45_MAP_STATION_DETAIL_CONTRACTS.md#source-fe3063546d2cfa0e)

협동은 자기 소유 자산과 고정 역할·서버 권위를 유지한다. 같은 지급의 카드 슬롯별 예산을 두 사용자가 공유하는 것이며 모든 슬롯을 합쳐 3+1이라는 뜻이 아니다. 교대 A/B 선택자·45초+A, 보급 계획 동의 90초/30초는 제안이다. 온라인 서버 시간과 현재 전체 화면 정지 정책의 결합은 서버 권위·소유 hold 계약을 명시해야 하며 로컬 시간 정지만으로 해결됐다고 하지 않는다. [COOP-UNIFIED-AUGMENT-CHOICE-20260916.md](46_COOP_DETAIL_CONTRACTS.md#source-991422e2420e54e4) [COOP-SUPPLIES-STATION-EVENTS-20260916.md](46_COOP_DETAIL_CONTRACTS.md#source-e5a41af1ba3b3528) [online-coop-mode-policy.md](46_COOP_DETAIL_CONTRACTS.md#source-29ac58e206039d36)

## 6. 생략하면 안 되는 실행·복구 계약

아래 원문은 상세 계약의 보존 목적지다. 원문에 있는 과거 선택 정책은 위 규칙으로 대체하되 API 필드·null 구분·오류·receipt·수명 계약은 통째로 요약 삭제하지 않는다.

| 원문 | 유지할 경계 |
|---|---|
| [2026-09-06-durable-run-operations.md](44_COMBAT_DETAIL_CONTRACTS.md#source-e894b886198cfc95) | 불변 START/FINISH body·key·build·time, 최초 승인 dispatch snapshot, 현재 admission 헤더, 암호화 journal을 POST 전 저장하고 확정 결과를 publish 전 저장, 계정·용량·손상 경계 |
| [combat-entry-ce03-server-20260911.md](44_COMBAT_DETAIL_CONTRACTS.md#source-b6574859c764e5cc) | rewardReference 정규 projection/UTF-16 key 정렬과 raw hash 구분, CSPRNG little-endian·xorshift 0 처리, seed만이 아닌 전체 checkpoint, lifecycle 필드·오류, CLAIMED와 TTL, HTTP 응답과 transaction receipt 구분 |
| [run-combat-requirements.md](49_SERVER_API_CONTRACTS.md#source-7e409c4a47efc1a6) | replay 선처리 후 combat_not_ready 409 무쓰기, requirements v2 null/차단 사유/궁극기 선택, source invalid 503, start-state 200 SNAPSHOT_UNAVAILABLE와 404 run_not_found, 계정 범위·no-store |
| [run-start-augment-package.md](49_SERVER_API_CONTRACTS.md#source-ed0862e306d76eb4) | 기존 operationPreviewId 요청·원본 멱등 재생·서로 다른 preview의 contract_violation·run_start_state_conflict 원자 rollback, nullable version snapshot; 옛 시작 패키지는 새 랜덤 지급 정책으로 복구하지 않음 |
| [train-loadout.md](49_SERVER_API_CONTRACTS.md#source-e992dfe4e3dd5d24) | 실제 소유 행과 동일 instance 중복 금지·해금 검증, GET/POST DTO와 오류, expectedRevision·no-op receipt·검사 전 불변 replay·CAS rollback; 당시 Medical/포탑 미지원은 현재 기획상 금지 아님 |
| [realtime-coop.md](49_SERVER_API_CONTRACTS.md#source-8ca626676d8145d6) | POST room/WS 연결·Bearer 전달 경계, 전체 DTO와 9개 오류, READY/PLAYER_INPUT/HEARTBEAT/CLIENT_STATE_HINT ACK와 시뮬레이션 실행 구분; skeleton은 실제 매칭·재연결·종료 commit 완료가 아님 |
| [combat-entry-next-slice-20260910.md](44_COMBAT_DETAIL_CONTRACTS.md#source-d7dfeb371afbbf2c) | 불투명 key·원문 body, tick debt+remainder·sequence hole·CAS 저장 후 publish, typed hash graph·엄격한 소유권; 로컬 과거 승인과 현재 런타임 검증 구분 |
| [combat-presentation-production-breakdown-20260912.md](51_UI_ART_DETAIL_CONTRACTS.md#source-78eacf9c42a3e722) | 실제 효과와 APPLIED 구분, presentation cursor/ledger·복원 시 효과 재생 금지·오디오/풀/구독 수명; 과거 cut-in 타이밍은 현재 04 §14로 대체 |

FINISH accepted=false/DUPLICATE_REPLAY를 실패로 바꾸거나 HTTP 200만으로 도메인 성공을 판정하지 않는다. 불확정 종료를 패배·환불·새 admission 허가로 추정하지 않는다. 통계에서 overkill·buff를 이중 공적으로 세지 않고 누락을 0으로 꾸미지 않는다. [combat-result-retry-flow-v1.md](44_COMBAT_DETAIL_CONTRACTS.md#source-4d37feef519db046) [combat-result-summary-detail-v1.md](44_COMBAT_DETAIL_CONTRACTS.md#source-3d41221b4ed5c4ed)

Bind/Unbind, 소유한 구독·타이머·풀·비동기 작업 해제, 취소 뒤 갱신 금지, 저장 후 publish를 UI 단순화 과정에서 제거하지 않는다. 재연결 시 현재 generation의 전체 snapshot을 적용하고 옛 탭·효과·핑을 재생하지 않는다. [realtime-combat-implementation-design-20260909.md](44_COMBAT_DETAIL_CONTRACTS.md#source-71831ab73bb3248c) [battle-connection-recovery-v1.md](44_COMBAT_DETAIL_CONTRACTS.md#source-9e22320e57141e91) [coop-status-signals-v1.md](46_COOP_DETAIL_CONTRACTS.md#source-18f61d36e170ad16)

## 7. 남은 결정과 검증

- T18 예외/재설계, 물리 2갈래와 현재 3선택 대응, 명시 확인 시간 정책, 협동 서버 시간과 전체 화면 정지의 결합은 미해결이다.
- 옛 배우별 치유·공격·수리 역할과 최신 시안 명칭, 수동 액티브와 현재 자동 액티브, 옛 armor 감소와 13 DEF%의 대응을 확인해야 한다. 서로 다른 버전의 train HP 3000/5000이나 감쇠식을 합산하지 않는다. [AUGMENT-ACTOR-WEAPON-MAPPING-20260915.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-14f939bc39c7134953d2) [combat-stats-v1.md](44_COMBAT_DETAIL_CONTRACTS.md#source-5bcf7c7f9134d9bc) [train-combat-test-profile-v1.md](44_COMBAT_DETAIL_CONTRACTS.md#source-836281eb9f35fa72)
- J04의 적 수·웨이브 수는 미승인이다. 챕터 6임무·15웨이브 및 EHP 1.30은 기존 첫 테스트/제안의 범위이며 신규 승인으로 재사용하지 않는다. [chapter1-encounter-curve-v1.md](44_COMBAT_DETAIL_CONTRACTS.md#source-25e8ad2e0b6d2840) [ordinary-enemy-budget-proposal-20260909.md](44_COMBAT_DETAIL_CONTRACTS.md#source-af672e8610564410)
- 상세 명칭 10개, 지원 7개 로컬 수치, ce03.local.qa.20260911.001은 후속 원문에서 로컬 채택 기록이 있다. 앞부분의 미확정 메모만 인용해 모두 자료 없음으로 되돌리지 않는다. 동시에 출시·전체 콘텐츠 승인으로 올리지 않는다. [combat-detail §13 후속 결정](51_UI_ART_DETAIL_CONTRACTS.md#source-7dd86e35b7c512fd) [combat-entry-next-slice-20260910.md](44_COMBAT_DETAIL_CONTRACTS.md#source-d7dfeb371afbbf2c) [ce03-minimum-operation-report.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-6c0c06d3f878957dd4a3)
- 과거 작은 cut-in·비정지·런마다 첫 회/설치 공용 상태는 현재 04 §14의 ALWAYS/DAILY/OFF, 전체 화면 combat 0, owner hold, 효과 중복 금지에 의해 대체된다.
- 원문 전체 회수·내용 검토와 효과 구현·연결·실제 게임성·시각/오디오 품질 검증은 별도 완료 항목이다. 현재 문서 통합만으로 원본 삭제나 구현 완료를 승인하지 않는다.


<a id="combat-detail-completion-gate-20260922"></a>
## 8. 전투 상세기획 잔여 점검과 완료 기준 — 2026-09-22

Plan_6 문서 대조. **전투 전체 상세기획은 아직 완료되지 않았다.** 사용자 요청에 따라 일반·스토리 전투의 상세기획 완료 시 명시적으로 알리고, 그다음 월드보스 상세기획과 관련 구현 충돌 대조로 이어간다. 이번 목록은 확인한 미결과 후속 검토 범위이며 모든 원문·구현의 전수 검증 결과가 아니다. 완료 알림만을 위한 자동화·타 작업 폴링은 추가하지 않는다.

### 이미 결정된 항목과 과거 미결 문구

- 목적지 선택과 물리 선로 조작 분리, 미리보기→명시 확정→저장, 확인 시간 정책은 D1 승인 범위를 따른다. §7의 3선택/2방향·명시 확인 시간 미결 표기는 이 범위에서 대체한다. 구현 연결·실기 검증은 별도다.
- 최초 일반 솔로 시험 규모는 D6의 12방문 노드, 일반2/엘리트3/최종보스1웨이브·3페이즈와 승인 그래프/가중치 기준이다. J04의 옛 전체 길이·웨이브 미승인 표기를 이 범위에 다시 적용하지 않는다. 적 수·편성·스탯·패턴별 피해량까지 D6가 승인한 것은 아니다.
- 스토리 경로·우회·작전 승리 시 진행/첫 보상은 D7 기준이다. 과거 런당 사건 수·보상 시점 미결 문구를 다시 결정할 필요는 없다.
- 승인 원본: [04 D1](04_COMBAT_RUN.md#navigation-resume-policy-v02), [14 D6](14_MAP_EVENTS_DROPS.md#operation-scale-trial-v02), [32 D7](32_NARRATIVE_REFERENCE.md#story-route-policy-v02). 과거 원문과 조사 이력은 보존한다.

### 다음 상세기획 순서

| 순서 | 남은 작업 | 닫아야 할 항목 / 원본 |
|---|---|---|
| 1 | Eve·Sera·Mira 6스킬 | 핵심 방향은 승인. 센트리 대상·상속·내구도·수명, 치유·다운 단축, 연사 횟수·간격·행동 점유, 수락/취소/소모·중첩·복원, 수치와 단위의 승인 가능한 상세안. [44 역할 스킬](44_COMBAT_DETAIL_CONTRACTS.md#eve-sera-mira-skill-draft-v01), WAIT-001 |
| 2 | 피해·다운·객차 파괴/수리 | 열차 전이30% 등 확정 규칙 보존. 요원 복귀 시험값과 객차 수리 제안값을 구분하고 자연복귀/치유/보호/파괴 장치/수리 가용성의 경계 및 시험 프로필을 정리. §2 및44 피해·회복 원문 |
| 3 | 증강40종 효과와 조합 | T18 HP0 작동과 객차 생존 필수 충돌 해결, 효과별 발동·상한·중첩·파생 재발동·대상 제외·수명/복원 및 운용 변화 검토. 과거 일부 조합 검토를 전체 완료로 확대하지 않음. 05·§3·WAIT-009 |
| 4 | 일반 적·엘리트·스토리 보스 | 승인 D6 규모 위에 적 편성/예산·패턴 예고/대응/피해·페이즈/종료 규칙을 정리. 초기5인 A안에서 중단·무작위 증강·추가 인물 없이 합법적 대응 수단 확인. [44 대응 비교](44_COMBAT_DETAIL_CONTRACTS.md#initial-five-counter-gap-v01), WAIT-001/003 |
| 5 | 전투 연계 자원·연출 경계 | 작전물자30/20은 모드/원출처를 확인해 구분한 뒤 결정하고, 수리 비용/공급 가용성을 위 생존 설계와 연결. DAILY 집계·하루 기준·기본값은 [04 §14.6](04_COMBAT_RUN.md#cinematic-daily-policy)의 권장안 상태로 남아 있어 별도 확정 필요 |
| 6 | 최종 누락·충돌 정리 | 기본 공격/궁극기·지원·지휘 특성·장비·증강·종료/정산의 교차 효과에서 미정 규칙이 남는지 확인. 기존 제한 소스 대조와 이후 제공된 실제 구현을 구분해 차이를 기존 WAIT에 모음. 화면 연결안도 함께 대조 |

1번을 다음 상세기획 단위로 진행한다. 2~5번은 관련 최신 승인 절을 확인한 뒤 필요한 부분만 보완하며, 이미 확정된 규칙을 재제안하지 않는다. 협동 서버 시간·월드보스 회복/점수·모드 전용 패턴은 별도 후속 범위로 유지한다. 일반 전투 규칙을 해당 모드에 자동 적용하지 않는다.

### 완료 알림의 조건

일반·스토리 전투 범위의 필수 규칙에 대상/조건/효과/수치·단위/상한/종료·예외가 정의되고, 미승인 선택이 해결되며, 주요 교차 효과에 모순이 없음을 문서 대조한 뒤 **“일반·스토리 전투 상세기획 완료”**라고 알린다. 시험 수치와 출시 확정값을 구분하며 실제 밸런스 검증 전 출시 적정성을 보장하지 않는다.

그 알림에는 ① 완료 범위와 기준 문서 ② 구현과의 확인된 차이·미확인 범위 ③ 남은 구현/실기 검증 ④ 월드보스 후속 기획 시작점을 함께 제시한다. API/코드/시안/실기 완료는 별도 증거가 있어야 한다. 실제 구현을 아직 읽지 않은 부분은 충돌 없음으로 처리하지 않으며, 통합 보완은 기존 대기 목록의 사용자 지시 절차를 따른다.

### 2026-09-22 후속 상태
- [44 6스킬 v02](44_COMBAT_DETAIL_CONTRACTS.md#six-skills-detail-v02): 사용자 후속 ㄱㄱ로 세부 규칙/수치를 첫 시험 기준 승인. 위 잔여표1번의 상세안 작성·결정은 이 범위에서 진척했으며 실제 효과/조합·초기5인 생존 검증은 남음.
- [44 피해·복구 v02](44_COMBAT_DETAIL_CONTRACTS.md#damage-recovery-detail-v02): 객차/포탑 복구 수치·장치 가동 재개·보호·동시 종료 후보 작성, 신규 승인 대기. 물리위험 직접계수·방어/보호막 개별 계약 대조는 별도 잔여. 전투 전체 완료 아님.
2026-09-22 추가:44 피해·복구 v02는 사용자 후속 승인으로 첫 시험 기준 채택. T18은 [31 호환 v02](31_AUGMENT_EFFECT_REFERENCE.md#augment-combat-compatibility-v02)의 실제HP 조기복구 재설계안이 신규 승인 대기이며 기존 HP0가동 금지는 유지. 증강 최신 역할 변형/개별 수치/합성 예산은 여전히 잔여.

2026-09-22 후속: [31 v04](31_AUGMENT_EFFECT_REFERENCE.md#augment-numeric-budget-v04)40종 수치·합성 예산과 인용한 v02 경계 사용자 ㄱㄱ로 첫 시험 기준 승인. [44 적·보스 v02](44_COMBAT_DETAIL_CONTRACTS.md#ordinary-enemy-boss-detail-v02)에12노드 편성·8종 공격·보스3패턴·초기5인 대응·종료/검증 작성, 신규 수치/규칙 승인 대기. 구15웨이브·폐기된 요원 역할을 새 프로필에 사용하지 않음. 코드/스탯 발행/실기 변경 없음. 다음 피해·방어·보호막 합성/물리위험 상세. 전투 전체 상세기획 완료 아님.

2026-09-22 후속:44 적·보스 v02 사용자 ㄱㄱ로 첫 시험 기준 승인. [44 피해·보호막·물리위험 v03](44_COMBAT_DETAIL_CONTRACTS.md#damage-shield-hazard-detail-v03)에 DEF/약화/관통·장비피해그룹·보호막층·물리위험 수치/단일경로/회피 배치 제약 작성, 신규 정책 승인 대기. 다음 전체 잔여 대조 및 효과별 그룹 매핑. 문서·산술 확인만, 구현/실기 미완료·전투 전체 기획 완료 아님.

2026-09-22 계산그룹 대조: 피해·위험 v03 후속 사용자 승인으로 첫시험기준 채택. [44 효과그룹 v04](44_COMBAT_DETAIL_CONTRACTS.md#combat-effect-group-map-v04)에 장비6/EQ16/EN16/세트6·지휘43노드 적용표와 TC02/지휘식 우선순위, 공속/관통합산/초과치유 추가선택 작성. 신규 상한·배분 승인 대기. 다음 보급·수리 경제 및 DAILY 세부정책. 구현/실기/전체 기획 완료 아님.

2026-09-22 후속:44 계산그룹 v04 추가선택 첫시험기준 승인. [45 경제 v02](45_MAP_STATION_DETAIL_CONTRACTS.md#solo-supplies-repair-detail-v02)와 [04 DAILY v02](04_COMBAT_RUN.md#cinematic-daily-detail-v02) 작성, 신규 수급/가격/정비배치·연출정책 승인대기. 서버 로컬04:00 KST 근거 확인,배포/시청API 미확인. 다음 승인 반영 후 일반·스토리 필수 잔여 최종 대조/완료 여부 판정. 코드/실기 변경 없음.

<a id="ordinary-story-final-audit-v01"></a>
## 일반·스토리 전투 상세기획 마감 점검 — 2026-09-22

Plan_6. 판정: **최종 점검 수행 / 전투 전체 상세기획 미완료**. 사용자의 직전 ㄱㄱ로45 경제 v02와04 DAILY v02를 첫 시험 기준 승인 반영했다. 수치가 적힌 문서/과거 시험의 존재를 최신 실행 스펙/밸런스 검증으로 바꾸지 않는다. 아래는 현재 읽은 최신 절과 관련 원문에 대한 제한 문서 대조이며 모든 파일/운영 구현의 전수 감사가 아니다.

### 이번에 닫은 것

- 일반·스토리 START 물자30/일반20/엘리트25/정비4·8층 접근/수리서비스 예외 및 DAILY 스킬별 오전4시 일자·기록·동기화 정책: 직전 제안에 대한 사용자 승인 반영.
- TC01 적용표가 TR01까지 제외하는 것처럼 읽히던 충돌:09의 기존 상세 계약대로 **200% 보조포격에 TR01만1회**, X09/X17/X18 제외로44 표와 파생 원천 문장을 정정. 새 수치/행동을 추가한 것이 아니다.
- 현재 우선 원본은44 six-skills-detail-v02 / damage-recovery-detail-v02 / ordinary-enemy-boss-detail-v02 / damage-shield-hazard-detail-v03 / combat-effect-group-map-v04,31 augment-numeric-budget-v04,09 oc01-reconciliation-final-v01,45 solo-supplies-repair-detail-v02,04 cinematic-daily-detail-v02다. 과거 승인대기 문구는 해당 최신 후속 승인 범위에서만 역사 기록으로 읽는다.

### 기획 마감을 막는 남은3묶음

| 순서 | 남은 결정·근거 | 닫는 산출물 |
|---|---|---|
| F1 궁극기 충전 | 44 6스킬은 개인100충전/100소모를 정의하지만 시작량·정상충전 원천/획득량을 출발 정의에 위임. 44 후반 Unity 기록의 basic-hp-hit-v1/유효 기본HP적중4%는 로컬 QA 후보이며 공식 규칙이라고 명시되지 않음. 현재04/09/34와 관련44 절에서 최신 정식 충전표 연결을 확인하지 못함. | 시작량·일반적중/완전실드/다단/처치/피격/시간/다운/노드전환/복귀·저장과 O11의200용량 적용표. 기존 승인 근거가 있으면 연결하고 없으면 첫 시험 제안으로 결정. |
| F2 고정 시험 스탯·계수 기준 | 적 EHP는 내부100/외부60 비교값이며 플레이어 실제DPS 아님. 구 초기5인 기초표에는 Eve Medic/Sera Engineer/Mira Support가 남아 새6스킬과 함께 사용하는 단일 시험 패키지가 없음. TC01의 지휘 고유base와 support_fire의 TRAIN_ATK400%가 같은 수치를 뜻하는지 근거 불충분. | 원본 HP/ATK/장갑/주기·레벨/성장·열차/객차/포탑·지원3개를 특정 버전으로 묶고 최신역할을 명시. 적 편성별 실제HP 역산/반올림표,TC01 base의 단위/원천 고정. 새 runtime ID를 임의 발행하지 않음. |
| F3 일반 최종목표·증원 콘텐츠 | D7은 스토리 보스와 일반 우회 최종보스를 구분.44 v02의3패턴은 스토리 보스 시험안이며 일반 최종보스에 자동 복사한다고 정하지 않았음. DC02는15초 호출/계획ID 필수지만 현재 검토 범위에 구체 증원 조합·출현간격·살아있는 적 상한·의무출현 종료의 단일 표가 없음. | 일반 최종보스가 같은 전투 모델을 공유하는지 또는 별도 패턴인지 명시하고 스토리 성공근거와 분리. DC02 깊이별 증원 편성/HP예산/출현·사망·종료를 DC01/적상한과 대조. |

F1~F3는 현재 확인한 연결 부족이며 미발견 문서까지 부재라고 단정하지 않는다. 구현 미지원으로 숨기거나 기준값0을 넣어 기획 완료 처리하지 않는다. 다음 작업은 F1→F2→F3을 한 마감 작업 단위로 진행하고 필요한 신규 정책만 승인받는 것이다. 이 목록을 근거 없이 더 많은 독립 기획 주제로 늘리지 않는다.

### 기획 마감과 구별해 인수할 구현·검증

- Server/Unity: 신규12node/정의버전/START 지원접근성/스냅샷·영속원장·타이머·기본피해 호출 체인,원run 구프로필 보존.
- Direction/Unity: 이미 승인된 미술 보존,초기5인 역할·노드/웨이브/궁극기 표시·시안 화면 연결을 실제 데이터와 대조.
- 시험: 무증강 초기5인 생존/적정 전투시간,증강 합성·정비 경로별 경제,물리 회피 가능성,동시종료/재접속/DAILY hold 및 두 기기 기록. 이번에는 수행하지 않았다.
- 위 구현/실기 대기는 기획 결정을 대신하지 않지만,결정 완료 뒤에도 전투 전체 구현/밸런스 완료 선언을 막는 별도 조건이다. WAIT-001/003/004/009/011에 기존 담당 경계를 유지한다. 자동 작업 전송/폴링 없음.

F1~F3의 결정과 최신 원본 정합화가 끝나면 **일반·스토리 전투 상세기획 완료**를 명시적으로 알리고,그 기준으로 월드보스 상세기획 및 사용자가 요청한 타 작업 구현 충돌 대조로 넘어간다. 이번 턴에는 완료 선언을 하지 않는다.

2026-09-22 F1~F3: [44 마감 상세안](44_COMBAT_DETAIL_CONTRACTS.md#combat-closeout-f1-f3-v01)에개인충전0/유효기본HP적중4·고정Lv30/T6수치/최신3패시브/TC01 base·일반보스공유/DC02증원 정의작성. 신규안승인대기. 정의작성과운영ID·합법편성/실기 검증분리,전체전투기획완료선언전승인/정합화필요.

<a id="ordinary-story-design-baseline-complete-v01"></a>
## 일반·스토리 전투 상세기획 — 첫 시험 기준 완료 / 2026-09-22

Plan_6. 사용자 후속 ㄱㄱ로 마지막 F1~F3의 개인충전·고정 수치·최신3패시브·TC01 base·일반 최종보스 공유·DC02 증원안을 승인 반영했다. **초기5인·위험도I·12노드 일반/스토리 솔로의 첫 시험 기준 상세기획 완료**로 판정한다. 이 절은 앞선 마감점검의 F1~F3 미결 및 다음작업 표기를 대체한다. 옛 원문/검토 이력은 삭제하지 않는다.

### 완료 범위와 원본

| 기준 | 최신 승인 원본 | 이번 마감에서 확인한 연결 |
|---|---|---|
| 입력·목적지·12노드·이야기 판정 | 04 navigation-resume-policy-v02 / 14 operation-scale-trial-v02 / 32 story-route-policy-v02 | 물리 선로와 목적지 분리,일반우회 승리를 이야기 성공으로 만들지 않음 |
| 초기5인 스킬·다운/복구 | 44 six-skills-detail-v02 / damage-recovery-detail-v02 | 최신6스킬과 개인충전,객차·포탑 독립HP/복귀,열차실제손실30%전이 |
| 적·보스·의무증원 | 44 ordinary-enemy-boss-detail-v02 / combat-closeout-f1-f3-v01 | 일반/스토리 전투모델 공유와 사건권리 분리,DC01/DC02 출현·상한·종료 |
| 피해·장비·지휘·증강 합성 | 44 damage-shield-hazard-detail-v03 / combat-effect-group-map-v04,31 augment-numeric-budget-v04,09 oc01-reconciliation-final-v01 | TC01 TR01만1회,TC02 별도풀,실장착 세트/OC01,지휘에 장비원천 금지 |
| 스탯·충전·최신 패시브 | 44 combat-closeout-f1-f3-v01 | Lv30/T6 첫비교표,0시작/4충전/100소모,O11 200용량,구3패시브 대체 |
| 물리위험·정비·경제 | 44 damage-shield-hazard-detail-v03 / 45 solo-supplies-repair-detail-v02 | 단일피해경로,감속/회피 배치,정비20% 서비스와전투수리15% 구별 |
| DAILY 표시 | 04 cinematic-daily-detail-v02 | 오전4시 서버일자,스킬별 첫영상,첫프레임/스킵/hold·완화동기화 |

최신 승인 본문과 인용한 예외가 적용 기준이다. 과거 미승인이라는 문장을 검색해 최신 승인을 취소하거나,이번 승인을 다른모드/다른콘텐츠에 확장하지 않는다.

### 완료에 포함되지 않는 것

1. 출시 밸런스 확정,위험도II~V·전체영입요원별 재설계·협동·월드보스 전용 상세안.
2. 실제 신규 ID/카탈로그 발행·운영 START/admission·합법 편성·저장/API/Unity 효과 연결·배포.
3. 무증강 초기5인 생존,경로경제/20~30분 시간/780조합 등 실전측정,물리회피 생성검증·네트워크/재접속/hold 실기.
4. 리소스 작업의 화면 연결안과 실제 Unity/Server 구현 전수 충돌 검수. 기존 승인 미술은 유지하고 최신 데이터를 표시하는 연결만 대조한다.

### 구현 대조에 넘길 확인된 차이와 미확인

- 구 CE03 fixture는 옛 Eve 치유/Sera 공격·표식/Mira 수리·옛 패시브/15wave·선택궁극기 기준을 포함한다. 최신 마감안을 그 fixture가 이미 구현했다고 보지 않는다. 대체 정의/버전과 구run 스냅샷을 함께 관리해야 한다.
- TC01 TR01 적용 누락처럼 읽히던44 표는09 원계약대로 수정 완료. 노드JSON/실제 효과 처리기 전체의 일치까지 검증한 것은 아니다.
- Unity 전이 헬퍼의 float×0.30만으로 실제손실 제한/내림 호출체인 준수를 증명할 수 없다. 신규 충전/층별실드/공용풀/물리위험/증원등록과 함께 실제 경로 확인 필요.
- 서버 로컬 오전4시 출석 계산 근거는 확인했지만 DAILY 설정/시청기록 API와 현재 배포는 미확인. 두 기기 정확히1회 보장은 채택하지 않았다.
- WAIT-001/003/004/009/011은 관련 **기획 선행 준비**만 진척한 것이며 통합완료로 닫지 않는다. 소비자가 실제 계약·데이터·화면·실기 근거를 남겨야 한다.

### 후속 순서

사용자에게 이번 완료 범위와 미검증 경계를 알린다. 이후 월드보스는 기존46/44의 원문과 관련 점수·시간·복구 계약을 먼저 대조하여 별도 상세기획한다. 일반전투 충전/복구/장비수치가 월드보스에 자동 적용된다고 가정하지 않는다. 타 작업 구현·리소스 화면 연결 충돌 점검은 기존 WAIT 목록을 기준으로 사용자 지시에 따라 일괄 검증/보완하며 자동 전송·폴링을 하지 않는다.

이번 검증: 최신 기준 앵커 존재/중복,43노드 및 장비효과 적용표의 ID 참조,TC01 예외/승인 헤더/캐릭터 안내 갱신을 확인한다. 이전 산술 근거는 값 변경이 없어 재사용한다. 문서 검증이며 전투 실행 시험은 아니다.

2026-09-22 사용자 후속 순서 확정: 월드보스 상세기획을 먼저 완료하고 완료 시 사용자에게 명확히 알린다. 그 다음 일반·스토리·월드보스 최신 기획 상호 충돌,각 작업의 실제 구현/데이터/화면 연결안 차이를 기존 INTEGRATION_WAITLIST 기준으로 모아 검증·보완한다. 완료 알림에는 통합 충돌 점검으로 넘어갈 시점임을 명시한다. 기획 중 발견한 선행 충돌은 즉시 원문/기존 WAIT에 기록하고 기획을 막는 부분은 먼저 해결하되 전수 통합점검은 월드보스 기획 이후 진행한다. 별도 알림 자동화·타 작업 폴링/자동 메시지는 만들지 않는다.

2026-09-22 월드보스 착수: [44 WB1](44_COMBAT_DETAIL_CONTRACTS.md#world-boss-core-detail-v01) 공통전투/180초·6회기믹시간틀/점수정밀도·종결·실시간연출 신규안 작성. 기존05시입장권/주간과04시DAILY분리. 다음WB2 수장3종서사·패턴·기본전투/장치복구,이후WB3지원표/WB4보상연결/WB5마감. 전체월드기획완료전통합전수점검보류,필수선행충돌은발견시정리. 신규안승인대기·실기없음.

2026-09-22 월드WB2: [44 수장3종/9패턴](44_COMBAT_DETAIL_CONTRACTS.md#world-boss-three-encounters-v02) 신규안 작성. WB1 첫시험기준 사용자승인. A약탈왕/B베일후보/C오르트후보,서사정사는별도미확정·패턴승인으로승격금지. 전용상호작용·기본90/2500ms·장치복구 제안. 다음WB3 효과지원/협동참여. 전체월드완료아님.

2026-09-22 월드WB3: [44 효과/협동참여](44_COMBAT_DETAIL_CONTRACTS.md#world-boss-effects-coop-v03) 작성,신규승인대기. WB2전투안승인·정사배정별도. 기본증강0/40종적격분류·장비/지휘원천소유·Sera/T18·유효참여계측/대행 범위정의. 다음WB4 주간/입장권/랭킹·보상/화면연결,전체월드미완료.

2026-09-22 월드WB4: [44 입장·정산·보상·화면](44_COMBAT_DETAIL_CONTRACTS.md#world-boss-event-reward-flow-v04) 작성. WB3첫시험승인. 신규안:주간공통500/30/15·명예5구간/교차모드1개·서버자동지급/원장·마감/정정·화면흐름,승인대기. 기존점수단계유지·실측전. 다음WB5 최종기획대조/완료범위판정,수장정사·자산/실제구현미확정보존.

<a id="world-boss-final-audit-v05"></a>
## 월드보스 WB5 마감 점검 — 2026-09-22

Plan_6. WB1~4의 전투·모드지원·참여·입장·정산·결과 흐름은 사용자 후속 승인으로 **첫 시험 기준 상세 작성 및 결정 완료**. 수장 후보의 정사·배정·주간순서는 별도 사용자 선택을 요청한 상태이며 답변 전에는 확정하지 않는다. 따라서 현재 전체 월드보스 마감 알림은 그 선택의 범위를 확인한 뒤 한다. 코드·실기·운영 발행은 이번 마감과 별개다.

### 기준·승인 대조

| 범위 | 최신 원본 | 확인한 경계 |
|---|---|---|
| 180초/3페이즈/기믹6회/점수 | 44#world-boss-core-detail-v01 | 정확히deadline 효과0,보스점수만소수누적,기믹창/버프종료177초이내 |
| 수장전투3종/9패턴/장치복구 | 44#world-boss-three-encounters-v02 | 특정스킬필수없음,전용상호작용4초최대/행동창8초,개체별8/12/16초·60%·3초 |
| 효과·협동·참여 | 44#world-boss-effects-coop-v03 | 기본증강0,40종분류/개인원천·파트너중복0,90초적격·단절유예30/누적60 |
| 입장권·주간·보상·화면 | 44#world-boss-event-reward-flow-v04 | 오전5시/각1장,월요일04:57:00마지막START,팀중복승급·계정공통권리 |
| 수장서사 | 32 §1~2 / WB2 §1 | 약탈왕확인,베일·오르트및순환배정은답변대기. 정사선택과전투룰승인을혼동하지않음 |

완료 판정은 위 네 상세안을 조합한 첫 시험 범위다. 기존 점수문턱200000 등은 출력 측정 전 경제시험값이며 실제 달성 난도를 입증하지 않는다. 숫자형 HP/부활이 아니라 무한보스·시간제 경쟁이라는 전제가 모든 소비 계약에 필요하다.

### 통합 충돌 점검 인수 목록

| 우선 | 교차 지점 | 점검할 것 |
|---|---|---|
| 1 | 일반/월드 시간·종료 | 일반컷신hold·HP0패배/월드무정지·자동복귀가모드별로분리됐는지 |
| 2 | 피해·점수 | 일반정수내림을월드점수에먼저사용하지않는지,서버고정정밀도/범위/오버플로·분할예산·재생동등성 |
| 3 | 원천·역할 | 요원/열차asset와지휘프리셋소유,같은계정노드중복적용·타역할명령거절 |
| 4 | 충전·회복·효과지원 | 최신3패시브/Sera·T18/게이지·마나분리,무한HP·단일본체/미발동표시 |
| 5 | 기믹·기본전투 | 장치상호작용이native스킬트리거를만들지않음,기존행동점유/담당교체/단절기한/파트너표시 |
| 6 | 참가·대행 | 통신연속성·행동/유지제어증거,자동대행을개인참여초로합산하지않음,검토대기를환급으로치환하지않음 |
| 7 | 이벤트·경제 | 오전5시티켓/주간과오전4시DAILY별도,두계정차감·예약·응답유실·서버무효원환급 |
| 8 | 랭킹·지급 | 공개팀기록과보상순위분리,솔로/협동한권리·차액자동지급/정산동결·정정원장 |
| 9 | 화면·미술·서사 | 당시편성/권위상태표시,공통팝업확정미술보존,계정별대사/원공개범위,수장asset/ID바인딩 |

이 표는 확인된 실제 버그 목록이 아니라 다음 실물 대조 범위다. WB 문서와 기존 일반문서의 서로 다른 규칙을 모두 같은모드로 합치는 것은 충돌이며,모드별명시차이를 통일하려고 삭제해서도 안 된다. Server 고정소수단위·schema/ID·실제 legal loadout·발행자산은 소유 담당의 구현계약으로 확인하며 아직 제공되지 않은 값을 검증완료라고 하지 않는다.

### 구현·실기 수용 근거

실제 소스/JSON/화면연결안의 버전과 원기획을 대조하고 항목별 일치/수정필요/미연결/미확인을 기록한다. 담당이 이미 완료한 검증은 해당 버전과 범위가 유효하면 재사용한다. 필요 실행은180초정상/정확deadline·단절·재접속·자발포기/기술무효,솔로9패턴/협동양역할,역할별무증강기본편성,0점적격·두모드차액·팀중복승급·주간경계다. 현재 미실행이며 문서 확인으로 PASS를 만들지 않는다.

사용자 지시 순서는 **월드보스 기획 마감 알림 → 위 통합 충돌 대조·보완**. 지금 타작업에 자동메시지/실행/폴링하지 않는다. 필요한 새 보안/반부정행위 조치나 출시·지급 작업을 이 기획 승인에 포함하지 않는다.

<a id="world-boss-design-complete-20260922"></a>
## 월드보스 상세기획 완료 — 첫 시험 기준 / 2026-09-22

사용자 “순환 확정”으로 마지막 수장·소속·전투 유형 배정과 주간 순서가 확정됐다. [32 승인 원본](32_NARRATIVE_REFERENCE.md#world-boss-rotation-confirmed-20260922)의 약탈왕→베일→오르트→약탈왕 순환을 따른다. 결말·생사·전체 대본은 범위 밖이다.

**WB1~4와 수장 순환을 합친 월드보스 솔로·2인 첫 시험 기준 상세기획 완료.** WB5의 수장 선택 대기 및 전체 마감 알림 보류는 이 승인으로 해소한다. 전투180초/3종9패턴/개체복구·효과지원/기본증강0/참여·단절/입장권·주간·보상·결과 흐름이 해당 범위다.

운영 ID·자산 발행·출시일/실제 START·저장·지급 구현·계측 검증·밸런스/점수 문턱 보정·실기/화면 연결은 완료에 포함하지 않는다. 서사 전체 집필이나 운영 출시 완료도 아니다.

사용자 요청대로 **월드보스 상세기획 완료, 이제 일반·스토리·월드보스와 실제 구현·화면 연결의 통합 충돌 점검 단계**임을 알린다. 다음 작업 원본은 본문 WB5의 통합9영역과 기존 INTEGRATION_WAITLIST다. 확인된 차이는 담당/원본/영향/보완/검증 근거로 기록하고, 문서 승인만으로 기존 WAIT를 닫지 않는다. 현재 통합 실물 점검을 이미 수행했다는 뜻은 아니며 자동 타작업 메시지·폴링은 하지 않는다.

<a id="integration-conflict-audit-20260922-v01"></a>
## 통합 충돌 정적 점검 v01 — 2026-09-22

사용자 통합 점검 지시로 Plan_6이 최신 기획과 실제 D 원본 소스·JSON·Direction 연결안을 대조했다. **1차 정적 대조 완료, 통합 수정/실행 검증 미완료.** Unity/Server 구현은 변경하지 않았다. 과거 로컬 정책을 최신 운영 구현으로 간주하지 않고, 새 정책으로 재사용할 때의 차이를 구분한다. 아래 번호는 본 점검 행 번호이며 별도 WAIT를 증식시키지 않는다.

근거 경로 약칭: U=`D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts`; S=`D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server`; Q=`D:/Project/LASTRAIL/QA/DirectionImpact-20260921`. 행의 줄 번호는 이번 열람 시점이다. 기획은 44의 F1~3, damage-shield-hazard-v03/groupmap-v04, WB1~4 및 본문 WB5를 기준으로 한다. 생산 배포 버전·계정·실제 START는 조회/실행하지 않았다.

### 확인된 차이와 보완 순서

| 번호/우선 | 분류·근거 | 최신 정책과 영향 | 담당·수용 조건 / 기존 대기 |
|---|---|---|---|
| I01 / 높음 | 구현 차이: S/apps/game-api/src/ce03-ultimate-policy.ts:25~32 최대100 literal. U/Battle/CombatOperatorAutomation.cs:19~20 최대100 및 자동사용 `==100`; ApprovedCombatView.cs:278~280 `/100f`, `==100` | O11 최대200을 수용하지 못한다. 기존100 정책에서는 동작하더라도 새 게이지만200으로 바꾸면 104 이상 자동사용·준비표시가 어긋남 | Server 정책 버전/START snapshot→Unity 용량·비용·준비표시 계약. 96→100,100→104,196→200,초과상한·비용100차감·재접속을 검사. 기존 저장run 버전 유지. WAIT-001/009 |
| I02 / 높음 | 데이터 차이: S/tests/fixtures/ce03/combat-stats.json:2 버전2026-09-09.001,24~26 옛3패시브,42~44 Eve치유/Sera공격·표식/Mira수리 | 최신 Eve소환/Sera치유·복귀단축/Mira연사 및 새3패시브와 다름. 테스트 fixture 차이를 운영 오지급/오전투의 실증으로 확대하지 않음 | Server 새 카탈로그/정책버전, Unity 효과·표시 연결. 6스킬과5인 기준 출력·분할/취소·소환수 수명 대조. 기존 fixture 역사 보존. WAIT-001/002 |
| I03 / 높음 | 계산 경로 차이: U/Battle/CombatSkillCommands.cs:143~152가 bonus를 outgoingCap으로 함께 제한하고 최종 int 나눗셈으로 반환 | 최신 장비·비장비·지휘 효과군 분리와 월드 보스 소수누적 점수에 그대로 재사용 불가. 해당 함수에 모든 새 효과가 이미 들어간다는 증거는 없음 | Server/Unity 동일 효과군·반올림 위치 계약. 일반 정수 피해와 월드 점수 누적 분리,작은 여러타격/분할예산/방어막/재생 동등성 검사. WAIT-004/009/011 |
| I04 / 높음 | 모드 재사용 충돌: U/Battle/BasicCombatSimulation.cs:208 열차HP0 종료,211~212 웨이브종료 승리; CombatDamageRouter.cs:99 이후 열차0 terminal이면 피해 적용 안 함 | 일반 규칙은 유효. 월드180초 무정지·무한보스·열차0 개별복귀·요원전투 지속에 같은 종료 경로를 사용하면 충돌 | Server/Unity 모드별 시계·종료·복귀 소유권 확정. 월드 열차0/요원생존,동시처리,정확180초,연출 중 시간경과 검사. 일반 종료 규칙 삭제 금지. WAIT-003 |
| I05 / 높음 | 미연결: U/Battle/CombatDamageRouter.cs:108~112 실제 HP손실의30% 내림은 존재. U Scripts 전체 `new CombatDamageRouter`/`ApplyCarriageDamage` 검색에서 정의 외 실행 호출 발견 못함 | 도메인 함수 완료와 실제 피해 producer·원자 저장 완료가 다름. 잘못된 30% 산식 전체 미구현으로 재등록하지 않음 | Unity producer→candidate Vitals+ledger→Server snapshot/CAS 연결. 동일event 재전송·0손실receipt·응답유실·복원 검사. 기존33 복원 계약 재사용. WAIT-003 |
| I06 / 중간 | 구 로컬 경로: S/apps/game-api/src/ce03-local-producer.ts:127 ORDINARY_15_WAVE | 12방문노드·일반2/정예3/최종1웨이브 정책을 이 구 로컬 producer가 증명하지 못함. 15웨이브를12노드와 단순 숫자치환하면 안 됨 | Server 경로그래프/노드별전투/저장버전→Unity 맵host. 분기확정1회·최종3페이즈·스토리성공+최종승리 검사. WAIT-003/010 |
| I07 / 높음 | 미확인: S/tests/fixtures/ce03/combat-stats.json:100 월드참고 enabled=false/id=null. S/apps/game-api/src/realtime-coop.ts:1~31은2인방/서버권위 계약 | 2인방 존재가 WB 역할원천·9기믹·단절대행·90초참여 적격성 완료 증거는 아님 | Server/Unity 해당 실행 모듈/버전·QA 연결 필요. 양역할 소유권,같은효과 중복0,대행수동기능금지,기믹취소/실패,참여검토대기 검사. 기존 WAIT-003/009/011 교차 |
| I08 / 높음 | 미확인: 이번 S/apps/game-api/src의 worldBoss/WORLD_BOSS/world-boss/weeklyRank 검색에서 WB4 실행 연결 근거 발견 못함 | 05시티켓·양계정원자예약·04:57경계·팀중복승급·계정차액지급·동결정정의 구현 여부 미확인. 다른 이름/패키지 구현이 없다고 단정하지 않음 | Server 계약·migration·원장·검증 원본 제시 후 Unity/Admin 소비 대조. 04시DAILY와05시경제 분리,AB/AC/BC 승급·0점적격·중복요청·늦은무효 검사. 실제 지급은 이번 범위 아님 |
| I09 / 중간 | 미연결/미확인: commander_tree_expansion_draft.json:3 publishable=false, Q/commander-tree-v17-art/REVIEW.md는 reference-only/기능검증 별도 명시 | 43노드·OC01 데이터 및 승인미술 존재가 런타임 효과/저장/화면 완료를 뜻하지 않음. WB 수장 순환 승인도 운영ID 발행을 대신하지 않음 | Server 발행계약→Unity 실제ID/효과·프리셋snapshot→Direction 독립자산 연결. 기존 지휘실/공통팝업 확정미술 유지. WAIT-011 및 WB 수장 바인딩 |

### 일치 확인 — 다시 만들거나 폐기할 대상 아님

- 개인 기본공격 실제 HP손실당4충전·자동 일반스킬 정책은 S/apps/game-api/src/ce03-ultimate-policy.ts에 LOCAL_QA_OPT_IN으로 존재한다. 전체 부재로 기록하지 않는다. O11/월드 소수점 유효타격 및 운영연결은 별도다.
- OC01 JSON의 노드 효과(658행)와 effect binding은 실제3슬롯·서로다른3태그·일반 받는피해10%감소다. 가상인원/선호선택 복원 불필요. Q/departure-readiness-v01/REVIEW.md도 최신OC01·프리셋저장/활성분리·출발snapshot·원START복구를 명시한다.
- Q/operation-map-preview-v03/REVIEW.md의 후보클릭=미리보기/확정1회/추가확인팝업없음/뒤로상태보존은 D1과 일치한다. 12층 전체지도나 실제 적격 그래프를 증명하는 화면은 아니다.
- Q/equipment-click-v01/REVIEW.md의 최신 하단 승인 기록을 우선한다. 두진입은 같은 장착/교체 처리에 합류하고 실제3슬롯·실제태그·미리보기 후 명시실행을 유지한다. 상단 후보라는 역사 문구만으로 미승인으로 되돌리지 않는다. 목록6개는 용량상한이 아니다.
- Q/commander-tree-v17-art/REVIEW.md의 일부 장식연결선은 확대된 미술예시이며43노드 그래프가 아니다. 이를 새 그래프로 채택하지 않으면 기획 충돌이 아니다. 최종 미술 유지,실제 source JSON 연결만 후속 검증한다.

### 기록 정합화·남은 증거

WAIT 상단의 전투미완료/6스킬 세부미승인 요약은 후속 승인보다 오래된 문구여서 현재 상태로 정정한다. 과거 상세 승인 이력은 보존한다. 이번 정적 점검으로 WAIT 완료 상태를 만들지 않는다.

우선순위는 I01~03 정책/데이터/계산 계약 → I04~06 모드·피해·경로 host → I07~08 월드협동·경제 → I09와 기존 화면의 실제 연결/실기다. 신규 기획 재승인이 필요한 차이로 확인된 것은 없으며 이미 승인된 정책 반영과 구현 계약 확인이 우선이다. 점수 고정정밀도·운영 ID 등 담당 계약은 미확정 상태를 유지하고 임의 생성하지 않는다.

통합9영역 중 시간/종료·정수계산·충전·구카탈로그·맵/장비/출격준비/지휘실 연결안은 위 근거까지 정적 확인했다. 역할별 전체 효과,9기믹 실행,단절/참여,이벤트/경제/랭킹,실제 수장asset/ID와 실제 화면 수명은 미확인이다. 테스트/180초실행/실제API/실기 검수를 이번에 수행하지 않았으므로 통합 PASS 또는 전수 무충돌을 주장하지 않는다. 담당자에게 자동 메시지는 보내지 않았다.
<a id="p01-policy-queue-review-20260922"></a>
## P-01 최신 정책·후속 정의 정합화 — 2026-09-22

Plan 문서 작업. 최신 승인 절과 구정책 신규 적용 폐기 경계,후속 ID/선행 관계를 점검했다. 검증 기준 버전은 `plan-queue-review.2026-09-22.1`이며 런타임 정책버전이 아니다.

- 44 six-skills-detail-v02 / damage-shield-hazard-detail-v03 / combat-effect-group-map-v04 / combat-closeout-f1-f3-v01 / world-boss-core-detail-v01 / world-boss-three-encounters-v02 / world-boss-effects-coop-v03 / world-boss-event-reward-flow-v04를 최신 첫 시험 범위로 연결했다. 상단 보존 문구가 미결 상태를 현재처럼 보이게 하던 문제는 우선 안내를 추가해 보완했다.
- 31 augment-numeric-budget-v04,45 solo-supplies-repair-detail-v02에도 승인 범위의 우선 안내를 추가했다. 미승인 전체 후보/운영 발행까지 확대하지 않았다.
- 09 oc01-reconciliation-final-v01,13 equipment-set-integration-v01의 별도시너지폐기/실3슬롯·단일태그/OC01조건,14 operation-scale-trial-v02의 경로,32 world-boss-rotation-confirmed-20260922의 순환을 기존 최신 원본으로 유지한다. 별도 효과/경제 수치를 새로 승인하지 않았다.
- 후속 정의의 P-03 선행에 P-02를 추가했다. 기존 U-03/A-01/H-01만으로는 공통전투 구현·연결의 통합 검증 없이 월드 통합에 도달할 수 있었으므로 선행 누락을 보완했다. 나머지 작업 범위와 담당 ID는 유지한다.
- 폐기 대상은 구정책의 신규 적용이다. 진행run START snapshot/구버전 재현/역사미술은 유지한다. 신규정의와 진행run을 같은ID 최신값으로 소급 치환하지 않는다. 기존 homepage·지휘실·공통팝업 미술 유지.

완료 수용: 새 안내의 파일/앵커 연결,후속10개 정의와 의존 ID의 존재·유일성·비순환성·독립시작점에서 도달 가능성을 점검한다. 결과는 아래 검증 기록으로 확정한다. 실제 코드 전환·통합 플레이·경제 실행은 P-01 범위 밖이며 P-02/03에서 담당 근거와 함께 검증한다. P-01을 요구하는 후속 정의는 없으므로 P-01 완료만으로 타큐 등록/깨우기를 하지 않는다.
### P-01 검증 결과 / 완료
2026-09-22 실제 readback: 최신 안내10개 앵커 연결 PASS,후속10개 ID/정의 유일성 PASS,모든 선행 존재·독립시작점 도달·순환없음 PASS,P-03의 P-02 선행 PASS. 09/13/14 원본 앵커 존재 확인. 안내 삽입 누락은 초기 검증에서 발견 후 수정하고 재검증했다. P-01 문서 범위 완료. 타역할 코드/시안 수정·실행 검증 없음. 후속정의 중 P-01 의존 항목0이므로 추가 등록 대상 없음.

<a id="growth-reward-combat-handoff-audit-v01"></a>
## 성장·보상·전투 구현 인수 누락 대조 v01 — 2026-09-28

범위: 최신35/14/44 기획과 WORK_QUEUE_SERVER/UNITY/PLAN, INTEGRATION_WAITLIST의 기존 후속 정의를 문서 대조했다. 코드 전수감사/실기/다른 세션 폴링은 수행하지 않았다. 승인원본과 담당구현 완료를 구분하며 승인 승급안의 구계정 실제이행은 별도 검증 전제다.

### 1. 발견과 문서 보완

- **승급 상태 불일치:** 35 승급/투자비교와 Server 큐/WAIT의 이전 승인대기 기록이 남아 있었다.35의 원승급·비교 절에 최신 사용자승인을 반영하고 큐/WAIT에 같은 승인원본의 우선안내를 추가했다. 역사기록/담당진행블록은 보존한다.
- **Server 최신 인수 연결 누락:** Server 큐의 반복경제 안내는 있었지만 최종장비 미지급·훈련승인·15요원 효과매핑·승급의 최신 근거가 직접 연결되지 않았다. 기존 S-02 범위를 가리키는 Plan 정책 안내로 보완했다. 새 독립 실행항목이나 우선순위 변경은 아니다.
- **성장/전투 경계 증거 누락:** 성장 레벨 저장 또는 스키마허용을 전투소비 완료로 오인하지 않도록 원ID/버전/산출값/거래·재생·모드검증 증거를 후속 인수조건에 명시했다. 앞선 CE03 초기5인/훈련1·승급0 제한 대조는 기존 근거이며 이번에 해결됐다고 하지 않는다.

### 2. 승인 원본 → 기존 담당 → 인수 조건

| 항목 | 원본 | 담당/후속 | 닫기 위해 필요한 근거 |
|---|---|---|---|
|일반/엘리트/최종장비|[14 보상승인](14_MAP_EVENTS_DROPS.md#battle-reward-approval-v01)|S-02 → U-02 → P-02|원node완료1회·일반2+확률/엘리트3+확률·최종장비0·실패0·가득찬보관함과최종정산분리·구START|
|영구반복5종/종결|[35 경제v04](35_CURRENT_ECONOMY_ANNEX.md#permanent-repeat-core-economy-v04)|S-02 종결/권리, S-05 경제 소비 → U-02/U-04 → P-02/P-04|실완료N/개인V·최종보스1wave·승리보너스1회·실패/포기구분·중복claim0·구보상표와이중지급0|
|주간3승2코어|같은35 경제v04|S-05 → U-04 → P-04|개인3승권리·월04시원기간·늦은결과/미수령복구·주1회·월드제외·주간목표실카탈로그|
|공통스킬훈련|[35 훈련](35_CURRENT_ECONOMY_ANNEX.md#operator-skill-training-effects-v01),[44 15명](44_COMBAT_DETAIL_CONTRACTS.md#operator-skill-training-mapping-v01)|S-02 + S-08~11 관련요원 → U-02/U-07~10 → P-02/P-07~11|한요원공통1~5/1회비용·일반/궁극기효과별실ID/버전·제외필드·소환/지속중복0·Sable/Warden궁극기불변|
|요원승급·누적|[35 승급](35_CURRENT_ECONOMY_ANNEX.md#operator-promotion-effects-v01),[투자비교](35_CURRENT_ECONOMY_ANNEX.md#operator-growth-investment-comparison-v01)|S-02 + 관련요원 효과계약 → 기존Unity/Plan후속|0~III 비용/개별소유·HP/ATK각1회·최대 적격 스킬1.32/HP·기본공격1.10·훈련슬롯환불과분리·구계정재청구/강제하향없음|
|실전밸런스|[14 비교](14_MAP_EVENTS_DROPS.md#normal-elite-reward-validation-v01),35 투자비교,44 전체검증|관련Server/Unity지원완료 후 기존Plan 통합범위|동일지원profile/성장/편성/설정·표본·실원결과·실효용/위험/시간·역할별출력·미실행사례 명시|

화살표는 담당 연결이지 기존 등록조건의 대체가 아니다. U-02는 S-02 **및 D-01**, P-02는 U-01 **및 U-02**, S-05는S-02, U-04는S-05 **및 U-02**, P-04는S-05 **및 U-04**를 유지한다. 관련요원후속도 WAIT의 개별조건을 그대로 따른다. 현재문서의빈Unity/Plan큐는누락으로간주하지않는다.

### 3. 공통 완료 증거 묶음

1. 실제 지원모드/카탈로그·경제·전투정의 버전, operatorId/abilityId/effectId 또는 보상권리ID와 원필드 매핑. API필드이름은Server 실제계약을 원본으로 하고 Plan이 임의 DTO를 새로 만들지 않는다.
2. 동일 계정의 거래 전후 크레딧/데이터/코어·요원랭크/revision 및 원영수증; 정상·부족·최대·동시거래·응답유실/재요청. 성공변경과차감이같은원자단위인지 확인.
3. START의승급/훈련·원산출값과실전소비 결과. 신규계정변경이진행run을바꾸지않으며재접속/협동host이관이재성장/재지급하지않음. 일반전투HP를매타격서버판정으로전환하지않고월드서버권한을유지.
4. 보상노드/종결/개인권리·claim 연결, 주간원기간과현지갑을분리. 실제클라화면의표시·비활성조건·재진입복구가서버원결과와일치.
5. 구계정/구START 지원표와미지원경계. 진행snapshot/기존투자보존, 이행검증전 임의기본값/새프로필발행0. 실제모드미지원은미검증으로남기고테스트fixture통과와구분.

담당완료는자기QA에버전/원결과/실행환경/미완료범위를기록한뒤기존완료기반등록절차를따른다. 후속소비자가필요한계약이빠졌으면‘구현완료·후속등록필요’또는해당미완료상태를유지하며큐삭제로문제를숨기지않는다.

### 4. 영향 없는 범위와 현재 결론

기존승인지휘실/공통팝업/홈페이지미술을재제작하지않는다. Direction은실제숫자/상태표시에레이아웃보완필요가확인될때그범위만,Admin은실지원설정/발행화면영향이있을때,Homepage는공개계약변경이있을때기존후속에연결한다. 이번내부성장기획만으로셋의신규실행항목을만들지않는다.

이번문서대조에서발견한인수연결누락과승인표기는보완했다. **성장·보상 기획의이번승인묶음은정리됐으나전투전체구현/통합·실기밸런스완료는아니다.** 실제효과ID/모드별소비·성장거래와보상검증·구계정이행·UI복구·비교플레이가남아있다. 다음은해당담당근거가확보된범위의실제구현충돌대조이며근거가없는부분은기존WAIT에유지한다.

<a id="growth-reward-runtime-audit-v01"></a>
## 성장·보상 실제 소스/QA 대조 v01 — 2026-09-28

[직전 인수점검](#growth-reward-combat-handoff-audit-v01)의 후속 정적검토. S=D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server, U=D:/Project/LASTRAIL/unity-project, Q=D:/Project/LASTRAIL/QA/CommanderS1-20260922. 아래행번호는이번읽기시점이며담당작업으로변할수있다. 로컬소스와기존QA파일을읽었으며서버실행/테스트재실행/실계정확인/배포상태조사는하지않았다. 전체저장소의모든대체실행경로를검사한결과가아니다.

### G01 — 신규 반복보상과 구15웨이브 경로 불일치 / 신규 연결 전 해결

확인: S/apps/game-api/src/index.ts:1295는 completedWaveCount>15를거절,1549는15를완료로판정,6148~6179는RAID_BOSS완주를보상용15wave로치환해calculateRunRewardPreview에전달한다. S/packages/balance/src/index.ts:227은maxRunWaves=15,745~789는기존작전기본보상/진행·위험도·결과·완료배율을조합,898/908은승리15필수/15초과거절이다.

판정: [35 경제v04](35_CURRENT_ECONOMY_ANNEX.md#permanent-repeat-core-economy-v04)의신규12방문실완료N·5종산식·최종보스1wave와같지않다. 예컨대17wave승리의최신예시를이구입력경로에그대로보내면정상정산할수없다. 구run보존용코드가존재하는것자체를버그로단정하지않으며신규프로필에재사용하면충돌한다.

인수 S-02/S-05: 원START의프로필/보상버전으로구15wave와신규정산을명시분리,노드/phase/실wave완료근거에서N과개인V를확정. 신규5종/주간/첫권리를원영수증에고정하고기존preview/claim저장·멱등경로를가능한재사용. 15를전역17로바꾸거나구함수의제한만제거하지않는다. 검증:신규17wave승리=500XP/700요원EXP/300크레딧/50부품/30데이터,5wave정상패배=100/125/50/10/5,포기0,보스3phase중복0,구15wave원결과재생불변. 이는기대값이지이번실행결과가아니다.

### G02 — 성장 저장과 전투 효과 소비의 연결 근거 부족 / 지원 범위 분리

확인: S/apps/game-api/src/growth-service.ts:101~113은실요원skill_training/promotion레벨·비용표를읽어갱신한다. S/apps/game-api/src/ce03-local-producer.ts:44는skillTraining=1/promotion=0만허용,132~133은그시험범위의요원값을생성한다. ce03-local-schema.ts:15의대상은초기5인이다. U/Assets/_LASTRAIL/Scripts/Battle/ProducerCombatAdapter.cs:69는producer의promotion/skillRank와원보유값일치여부를검사한다.

판정: 성장레벨저장및클라정합성검사는존재하지만승인된승급0~III·훈련1~5·15요원효과를이CE03경로가실행한다는증거는아니다. 다른미검사경로전체에효과가없다고확대하지않는다. 과거시험기의허용값만넓히는수정으로최신능력소비를완료처리하면안된다.

인수 S-01/S-02/관련요원: 실제지원producer/능력정의·적격효과별훈련배율·승급원스탯·원START를연결하고출발전지원검사와성장거래의지원범위를맞춘다. 35 승인정책과44 operator-skill-training-mapping-v01을소비. R1/3/5×승급0/I/II/III의원출력·제외효과·거래/재요청·구계정/구run증거필요. Unity는resolved값에배율재적용하지않고원전후표시/오류/복구를검증한다. 비용청구가능여부와실효과지원이일치하는지는추가통합검증항목으로남긴다.

### G03 — 신규 공개 START 차단과 정책진단 QA / 미구현을 허용으로 바꾸지 않음

확인: S/apps/game-api/src/index.ts:5952~5961의신규공개START경로는현재입장버전검사뒤 describeCombatRequirements 결과와함께 combat_not_ready/409를반환한다. 위쪽기존성공재생과구run보존은별도다. combat-readiness.ts:25는status BLOCKED/canStart false다. Q/s01-binding-evidence.json은 POLICY_BINDING_DIAGNOSTICS_IMPLEMENTED_EXECUTION_PENDING, liveExecutionEnabled=false,당시52검사PASS를명시한다.

판정: 최신정책요구·진단이연결된부분과실전지원은다르며G01/G02를실계정신규작전오지급이확인됐다고표현하지않는다. 이번에출발차단을해제하지않는다. 인수S-01은실행바인딩/수치/원천/지원모드의원QA를제출한후원등록조건으로진행하며구QA의52PASS를신규성장/보상시험통과로재사용하지않는다.

### G04 — 협동 개인권리·최종검증은 부분 구현 / 새 보상 수혜 확정 미완료

확인: Q/coop-occupancy-evidence.json은2026-09-28 내부host초기화/공유점유·synthetic입장/예약guard범위132검사를기록하고실입장/개인START·실checkpoint/예약정합·최종검증/화면API를remaining으로명시한다. 현재Server큐도S-12진행중이다. S/apps/game-api/src/combat-finish-authority.ts는명시협동/월드계약의legacy client finish를 combat_finish_verification_required로거절하며원영수증재생/계정접근검사후검증하도록정의한다.

판정: 점유보호구현은인정하되개인승리·잔류자권리·주간3승코어집계가끝까지연결됐다는근거는없다. 구client finish를새협동/월드종결에우회사용하지않는다. S-12→기존U-11/P-12및경제소비범위에서함께완주후전달단절양측권리/전투중이탈후잔류완주잔류자권리·원기간집계·늦은결과/재전송을검증한다. S-12현재우선지시는유지한다.

### 5. 범위 밖·다음 완료 조건

최종장비0/일반·엘리트실드랍/주간카탈로그/15명실효과ID의모든구현을이번정적감사에서끝까지추적하지않았다. 따라서‘없음’또는‘완료’로결론내리지않고원담당QA대조잔여로기록한다. settleRunReward의기존저장preview·지갑/권리처리는읽었으나동시성·멱등성테스트를재실행하지않았으므로안전성전수검증을주장하지않는다.

기존Server큐에G01~04근거연결추가,WAIT에현재구현/후속증거상태를기록한다. 새로운부모작업/미충족Unity큐를만들지않고원담당코드를수정하지않는다. 이번완료는**제한된소스/QA대조와발견사항인수**다. 전투전체상세기획/구현·실기종료선언은44 combat-validation-closure-v01의미완료항목과별도다. 후속은위미추적항목의실제소스/QA대조또는담당이확보한새소비증거인수이며자동폴링하지않는다.

<a id="drops-weekly-roster-runtime-audit-v01"></a>
## 장비 드랍·주간 코어·15요원 ID 실제 대조 v01 — 2026-09-28

직전 growth-reward-runtime-audit-v01의 G01~04 후속. S=D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server, U=D:/Project/LASTRAIL/unity-project. 이번은명시범위정적읽기이며코드수정/런타임실행/DB조회/배포확인없음. 아래파일행은읽기시점이다.

### G05 — 장비 드랍 실행 경로 미확인

S/apps/game-api/src의equipment/loot/dropTable/nodeReward/node_reward검색과packages/db/migrations의equipment/loot/drop_table/gear_instance검색에서승인노드드랍→실장비instance생성→보관/원영수증으로이어지는경로를확인하지못했다. 검색불일치는전체프로젝트미구현의증명이아니다. U/Assets의장비/보관/드랍관련파일명검색에서도명확한소비자를확인하지못했으며파일명이일반적인다른실행경로는미추적이다.

직접확인한S/apps/game-api/src/ce03-local-producer.ts:163은variants.equipment=OFF,ce03-local-schema.ts:87도OFF고정이다. combat-damage-budget.ts의EQUIPMENT_DIRECT효과군은피해계산지원이며장비획득·실물보관지원근거가아니다. 이시험기의PASS를일반2+30%/엘리트3+50%/최종0드랍완료로사용하지않는다.

인수 S-02: 실제노드완료입력부터원START보상정의/추첨결과고정·장비실물ID·개인귀속·보관함처리·원결과복구의소유경로와QA를제시한다. 기존구현이있으면그실제경로를등록하고중복서비스를만들지않는다. 부재가확인되면기존작업범위에서구현. 수용:기본/특성source분리,중복완료/재접속재추첨0,일반/엘리트수량·성급,실패0,최종장비0와영구정산분리,보관함초과·실3슬롯/태그연결. Unity U-02는이계약완료후기존선행조건으로등록한다.

### G06 — 주간 개인3승·2코어의 기존 퀘스트 재사용 경계

확인 소스:
- S/apps/game-api/src/recurring-policy.ts:13~34는ORDINARY_15/STORY_15/STORY_BOSS/WORLD분류와15wave검증. 일반정상패배도조건충족시ends=1이며승리전용count가아니다. 14~20목표는D1~3/W1~3,주간은login3/ends6/waves60. 49~53보상목록에주간3승2코어는없다.
- objective-events.ts:3~15는기존6스토리ID를STORY_15로분류하고FINISH의occurred_at을INSERT시점DB시계로기록한다. 원확정종료시각을인수받는필드는해당입력에없다. 원계정+run중복삽입방지경로는존재한다.
- recurring-progress.ts:16~35는login/ends/waves를occurred_at기간에누적하고recurringObjectiveSpecs를QUESTS/PASS50양쪽에사용한다. 같은목표목록에새코어목표를무조건추가하면PASS50에도목표/XP경로가생길수있다.
- recurring-objective-state.ts:36의일반주간달력은MONDAY_04_KST로승인방향과일치한다. recurring-grants.ts:68~75/99~111은기간종료후mail경로·발송후14일만료를사용한다.

판정: **현재확인한목표/집계에는승인된신규개인3승2코어가연결돼있지않다.** 기존ends를wins로이름만바꾸거나STORY_15검증상한만늘리는수정은원퀘스트/구작전정책까지바꾸므로피한다. 주간경계가맞아도늦은결과의원기간귀속은충족하지않는다. 기존mail만료가있으므로‘원기간권리복구’를단순우편발송으로완료처리할수없다. 35의보존권리와연결된미수령저장/복구를구체화하고기존다른퀘스트우편정책을전역변경하지않는다. 코어권리에새14일소멸정책을자동승계시키지않으며불명확한복구만료는승인정책대조후별도결정한다.

인수 S-05(선행S-02): 검증된개인승리자격·원종료시각/버전·최초권리를집계,기존원목표를보존하고신규QUESTS전용목표/코어2권리를실카탈로그에연결. PASS50목표/XP·W1~3보상변경0. 같은run/account중복0·양측/잔류개인자격·월드제외·3승한번/4승추가0·월04시전후/늦은결과원주간·주차전환미수령/복구·일반패배ends는승리로미집계. 미완료2승은다음주이월0. S-12개인종결증거와연결필요한범위는기존WAIT에서인수하며미지원협동을가짜승리로대체하지않는다.

### G07 — 보유 ID 15개 확인 / 전투 능력 지원과 분리

S/packages/balance/src/gacha-policy.ts:58의launchEliteOperatorIds에서아래15ID를확인했다. 이전44효과표의추가10인operatorId미확인은**보유카탈로그식별범위에서해소**한다. 실제계정보유/해금/모드지원/능력ID가자동증명되는것은아니다.

| 이름 | 보유 카탈로그 ID |
|---|---|
|Kyle|op_kyle|
|Reina|op_reina|
|Eve|op_eve|
|Sera|op_sera|
|Mira|op_mira|
|Vex|op_vex|
|Nyx|op_nyx|
|Iris|op_iris|
|Sable|op_sable|
|Warden|op_warden|
|Anchor|op_anchor|
|Marrow|op_marrow|
|Lumen|op_lumen|
|Rail|op_rail|
|Cinder|op_cinder|

S/apps/game-api/src/combat-policy-catalog.ts:9의scope.roster는초기5명,18~38은Eve/Sera/Mira6능력요구,combat-policy-binding.ts:13~16은Kyle/Reina4능력요구다. 여기서는실제 .active/.ultimate 형식의초기5인10능력ID가존재한다. binding은범위외요원에combat_policy_operator_unsupported를기록하고마지막에EXECUTION_UNAVAILABLE/canExecute=false·효과handler미연결사유를반환한다. ID존재/정책요구바인딩은실행handler완료가아니다.

인수 S-01/S-02 및 S-08~11: 위실operatorId를원보유와대조하고추가10인의ability/effect실ID·버전·지원handler/모드·원스탯/훈련/승급소비를등록. 이름에 .active/.ultimate를붙여실제정의확보로위장하지않는다. 같은새효과를구ID에무버전덮어쓰기하지않고구START를보존한다. 각후속Unity는Server실지원매핑/QA를받은뒤지원/미지원표시와원결과소비를검증한다.

### 결론·남은 범위

이번세항목의정적대조는완료했다. 장비드랍은실경로확보필요,주간코어는승리전용집계/원기간/패스분리/미수령권리인수필요,요원보유ID15개는확보됐지만전투능력/효과handler지원은별도다. 원담당실행작업에근거연결만추가하고신규후속큐/자동메시지/우선순위변경없음. 담당계약/QA가없는범위를완료나확정미구현으로승격하지않는다.

<a id="equipment-drop-path-audit-v02"></a>
## 장비 드랍 경로 추가 추적 v02 — 2026-09-28

G05 후속. S=D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server(현담당큐/인계에서확인), U=D:/Project/LASTRAIL/unity-project. 루트server/apps·packages도보조검색했으나현담당원본으로교체하지않았다. 서버소스/스키마·Unity Assets의장비/실물/드랍/보상참조와작성JSON·담당인계를읽었다. 루트넓은파일검색은Migration-Codex-20260914/acl-probe접근거절로완전하지않아이후명시소스경로로좁혔다. 구백업/이전worktree전수를현행런타임으로간주하지않는다.

### 1. 재사용할 서버 기반 확인

S/apps/game-api/src/operation-map-plan.ts:5에는nodeId/eventKind/encounterProfileRef/rewardPlanRef가있다. operation-map-publisher.ts:5~38은서버소유OperationMapCatalog를한번읽고각node의보상참조/버전/종류를대조해operation.map-content.v1에실사용정의전문·bindings·contentHash를고정한다. catalog부재는catalog_unavailable이며운영adapter미설치가명시돼있다. definition의generic JSON허용은실드랍수량/성급/장비효과처리검증이아니다.

QA/CommanderS1-20260922/map-content-start-evidence.json은departure-v4/operation.map-content.v1 원문무결성·비공개START고정범위58검사PASS(당시기록),실catalog adapter/발행·도착/목표/거래verifier·스토리/협동/Unity소비를remaining으로남긴다. 담당인계의같은절은원content를run_snapshots/지도start_snapshot_json에결속하고구START를재작성하지않는다고명시한다. operation-map-state.ts의COMPLETE는노드단계전이이며장비실물지급증거가아니다.

따라서 G05의‘드랍실행미확인’은유지하되 **보상정의참조/원START고정기반은존재**한다고보완한다. 이기반을중복제작하지말고실행카탈로그·노드결과/장비보관계약을결합한다. 정의hash만있다고추첨/지급이실행됐다고판정하지않는다.

### 2. 장비처럼 보인 클라이언트 경로의 실제 용도

U/Assets/_LASTRAIL/Scripts/Presentation/MissionShell/SharedRoulettePopup.cs:62의RewardEquipmentDeck은팝업장식영역RectTransform이다. 같은파일100~106의Render는전달받은result/lines를SharedRouletteVisual.Present로보낸다. SharedRouletteVisual.cs:75~108은rewardLines/원수령식별자를받아보상카드와숫자연출을표시한다.

실호출자는CombatAdView.cs:9/22 및LobbyOfflineAd.cs:23/58이다. CombatAdController.cs:49의요청은 /v1/runs/{run}/reward-preview/{preview}/ad-intents, CombatAdView는광고의원BaseLines/Result를팝업에전달한다. 즉확인된경로는작전결과/오프라인광고룰렛공통표현이며노드장비추첨·실instance생성·3슬롯장착·보관함저장경로가아니다. QA의hide/restore-RewardEquipmentDeck.png는이영역의시각제어근거일뿐장비지급검증으로사용하지않는다.

기존승인팝업/독립미술과컨트롤러콜백은보존한다. 이름만보고장비지급로직을View에추가하거나최종보스장비0을이유로공통보상패널을삭제하지않는다. 추후노드장비화면은원지급결과를표시하는계약으로연결하며시안자체가확률추첨/저장을소유하지않는다.

### 3. 작성 JSON과 실행 카탈로그 구분

현재 docs/superpowers/plans_categories/map_drop_draft.json은authoringFormat=lastrail-map-drop-design-v2,publishable=false, mapLength/wavesPerCombatNode/finalOperationPlan에null이남아있다. equipment_enchant_draft.json은13원문상수치표작성본이며publishable=false다. 명시서버/Unity소스경로검색에서이두파일을실행카탈로그로직접로딩하는참조를확인하지못했다.

최신MD의12방문/노드별2·3wave/최종1wave·보상정책을이null로취소하지않으며,파일이존재한다고실보상정의지원으로간주하지않는다. Server가실catalog변환을할때최신승인MD/작성표의대상필드를대조해원정의버전을발행해야한다. 작성JSON을통째로운영에사용하거나이번읽기만으로publishable=true로바꾸지않는다. 이번감사는JSON미정필드의새정책값/배포승인을추가하지않는다.

### 4. 기존 S-02 인수의 구체적인 연결 순서

1. **원정의:** 기존map publisher/catalog 경계를재사용. 최신승인14의수량/성급/기본·특성source·최종0을검증할실보상정의와지원validator를연결한다. 표준형식/실ID는Server소유이며Plan이임의API를만들지않는다.
2. **원결과:** 기존node완료검증과원START rewardRef/version을연결. 원run/node/source의승인횟수에따라추첨결과·지급권리·실장비ID를고정하고재조회에서재추첨하지않는다. 보상불명시선택/이동/재시도로무료복제하지않으며진행hold는기존원계약을따른다.
3. **보관·소비:** 승인된실장비카테고리/성급/태그/효과와개인소유를저장. 보관함초과·장착3슬롯·합성/처분·노드종료/작전종료정리는13의현정책을소비한다. 공유노드라고개인장비권리를파트너명의로복사하지않는다.
4. **실제 소비 증거:** 원node정의→완료영수증→장비실물→보관조회→Unity표시/조작→전투효과의동일ID/버전을추적. 일반/엘리트/실패/최종0·응답유실/중복완료·보관함초과·협동개인권리·구START 사례의실행결과를남긴다. 그후기존U-02 선행AND조건에따라후속등록한다.

상태:정의고정기반/공통룰렛표시경로는확인,실노드드랍·보관실물·장착소비를잇는끝단경로는이번확인범위에서미확인. ‘전부미구현’주장과‘지급완료’주장모두하지않는다. 같은검색을반복하기보다원담당이제시할실계약/QA로다음검증을진행한다. 코드는수정하지않았고실기/테스트재실행/운영조회없음.

<a id="authoring-policy-status-reconciliation-v01"></a>
## 작성 JSON의 승인 상태 정합화 v01 — 2026-09-28

대상은같은plans_categories의map_drop_draft.json/equipment_enchant_draft.json이다. 장비실행계약대기중가능한독립문서작업으로전체JSON을읽고14의D6승인/32스토리별승인/14전체공개·최종보상/13장비세트정책과대조했다. 수치변경이나런타임형식설계가아니다.

발견:map JSON은d6PolicyApproval에서승인을이미기록했지만requiredDecisions에J04_NEW_MAP_WAVE_PROFILE_APPROVAL, waveAndMapProposal.status에미승인,증강상대가중치20에도미승인표기를유지했다. 장비JSON도requiredDecisions에J04_NODE_PROFILE_APPROVAL을유지했다. 이표기들을현재승인범위와정합화했다. 상대가중치20을운영20%확률이나증강보장으로바꾸지않았다.

두파일에currentPolicyReferences/authoringInterpretation을추가해최신승인원본과실행미지원경계를연결했다. map의mapLength/wavesPerCombatNode/finalOperationPlan null은과거미완성필드이며현재정책미결/0이아님을명시한다. 기존숫자정보는waveAndMapProposal에유지되고,이전필드에스칼라/객체형식을임의로만들어삽입하지않는다. 원d6PolicyApproval.storyD7Approved=false는당시D6승인에스토리가포함되지않았다는뜻으로보존하고후속별도승인스토리원문을연결했다. 일반finalMode를스토리의제동/해방조건없이범용승리로사용하지않는다.

변경없는범위:일반2+30%/엘리트3+50%,성급확률6행,장비수치36/128/96/18/6행,세트/합성/인챈트,handlerId null, requiresBindings 및publishable=false. requiredDecisions가비어도효과지원/밸런스/발행완료가아니다. 신규API/Unity/Server실행코드변경없음.

Server S-02의실catalog변환은이참조가리키는원문과프로필별로대조하고구필드/null을그대로실행스키마에복사하지않는다. 최신승인정책은다시승인을기다리지않되실행바인딩/검증은별도로완료한다. 이번변경은작성자료동기화이며관련담당진행/우선순위/후속선행조건은유지한다.

<a id="approved-map-flow-audit-20260928"></a>
## 최신 승인 지도 흐름 대조 — 2026-09-28 / 순차검수8

대상 QA/DirectionImpact-20260921/operation-map-polish-v02의manifest/index/view.js·qa.json,연결된operation-map-full-reveal-v01/model.js/controller.js를읽었다. manifest는direction-d11.elite-boss-icons.2026-09-28.1 final-art-approved,runtimeComplete=false다. 기존qa.json의360/430/720기록은당시검수이며이번브라우저/육안/실기재검사는없다.

14전체공개v02와대조: View는모든graph노드를출력하며엘리트/보스별독립아이콘을연결한다. Model은먼노드/접근불가도선택열람가능하되직접연결next·정보존재·미확정일때만확정한다. Controller의열람/확정행동분리,정보누락을비공개로위장하지않는설명,미추첨장비/결말비공개가원기획과일치한다. sessionStorage는시안재열기예시이며서버원START복원증거가아니다. 예시16node/엘리트2·보스1을실생성/12방문정책으로사용하지않는다.

**표시누락1건:** index.html 하단사건범례는‘일반 전투 · 장비 · 인챈트 · 최종 전투’만표시하지만 view.js/data의엘리트·보스도실시안에존재한다. 아이콘/이벤트자체누락은아니며범례문구만보완대상이다. 승인미술/배치/효과/graph/model/controller를재제작하거나보스정체/결말공개를추가하지않는다.

독립Direction D-12: 기존현재표시사건의범례에엘리트전투/보스전투를포함하고최종전투와의중복/다른종결예외를원타입기준으로구분한다. 실제미지원사건종류를새범례로추가하지않는다. 360/430/720에서줄바꿈/잘림·아이콘의미·접근성이름·열람/직접확정/누락오류를확인하고기존Direction QA에근거를남긴다. 미술신규승인/실API권한은추가하지않으며완료후자기큐블록을삭제한다. 이수정만으로다른미충족후속등록없음.

<a id="plan-queue-review-20260929"></a>
## Plan 큐 일괄 작업·인수 대조 — 2026-09-29
사용자 기획 큐 전부 진행 요청. 작업 원본 D:/Project/LASTRAIL, 앱205a 사본은 사용하지 않음. 실행 코드·미술·운영 변경 없음.
- 보관함: [13 v18](13_EQUIPMENT_ENCHANTMENTS.md#inventory-capacity-approved-v18) 기본30/DO05최대54·장착제외·부족시정리후원보상수령 승인. 주기정리 의무0. 구START 유지.
- P-13: [11 계약v1](11_WEBSITE_COMMUNITY.md#community-public-contract-v1) 공개/본인/Admin DTO·명령·상태·오류·감사·보존 경계 작성. 신규 세부정책 승인은 별도, S-13+P-13 완료 전 S-16 진행 불가.
- P-15: [44 레지스트리v1](44_COMBAT_DETAIL_CONTRACTS.md#world-boss-pattern-registry-v1) 9종/18매핑·B본체침입·단일점수·복구·A/C시안 대조 작성. 실제worldID 미확인, story ID 전용 금지. 월드DEF 전환 자동승계0. 승인/실profile 매핑 잔여.
- 위 문서의 자체검토는 기존 승인과 제안 구분·시간 산술·소유권·오류·스냅샷 대조다. 실제 구현 통합 PASS가 아니다.

### P-14 홈페이지 통합 검증 준비
확인한 현재 WORK_QUEUE_HOMEPAGE의 H-02/03/04, WORK_QUEUE_ADMIN의 A-02/03 모두 선행대기다. 최신 역할 큐에서 완료근거를 확인하지 못했으며 단순 문서 작성으로 구현완료를 추정하지 않는다. 다섯 선행 완료 및 동일 DEV build/API schema/public release 식별 확보 후 실행한다.

| 사례 | 필요한 실제 선행 | 기대 결과·수집 근거 | 현재 |
|---|---|---|---|
| 공개 목록→상세→구버전→롤백 | H-02/S-17 | 실제 공개ID/digest일치, 비공개404, 원버전 유지; HTTP+브라우저 | BLOCKED |
| 로그인/만료/로그아웃/다른계정 | H-02/03/04/S-13 | DEV tester/CSRF·Origin,401복구,이전계정응답/캐시 비노출 | BLOCKED |
| 본인문의 접수→Admin답변→본인조회 | H-03/A-02/S-15 | 원ticket/receipt,재전송중복0,운영메모/타계정0 | BLOCKED |
| 글·댓글→신고→Admin숨김→재조회 | H-04/A-03/S-16 | 원case/revision,목록/검색/상세/캐시 모두 본문비노출 | BLOCKED |
| 차단/삭제/잠금/제재·복구 | H-04/A-03 | 개인차단격리,자기권한/게임연쇄0,복원경합오류 | BLOCKED |
| 네트워크실패/중복클릭/늦은응답 | 다섯소비자 | 가짜성공0,동일키원결과,화면종료후갱신0 | BLOCKED |
| 접근성/작은화면/키보드 | 다섯소비자 | 실제브라우저 포커스/신고·차단/본문/오류 재진입 | BLOCKED |

모든 사례의 실행기록에 DEV 환경·build·server schema·계정역할(비밀 제외)·release/version·시각·실제 요청/응답/화면·원키를 남긴다. fixture 성공 화면으로 실제연동 성공을 대체하지 않는다. 실게시/PROD배포/실재화 지급은 범위밖. P-14는 삭제하지 않고 선행대기를 유지한다.

## P-15 보스 연결 검수 — 2026-09-29
[44 v2](44_COMBAT_DETAIL_CONTRACTS.md#world-boss-binding-v2)와32의2026-09-22확정수장순환 대조: 약탈왕/A,베일/B본체침입,오르트/C,각3페이즈×2회차 총18연결. story ID를월드실운영ID로오인하지않고신규계약ID임을명시. v1수장미확정기록정정. 승인상세/Plan설계완료,Server실등록/점수·재생/DEF검증은후속. S-WB-01/D-WB-01등록후P-15실행블록삭제;원완료근거는본절/44/WAIT에보존.

<a id="story17-integration-audit-20260930"></a>
## Act 1 확장 기획·구현·큐 충돌 점검 — 2026-09-30
### 범위와 판정
32의승인된17사건/수장시험/ID/대사/8사건편성,35의신규첫보상/오프라인투영,44의최초발견·첫개최를 읽은개발Server·Unity소스 및 현재담당큐와 대조했다. 기획승인은반영됐지만실연결은미완료다. PROD/DB·모든브랜치·전투실행은검사하지않았다. 소스문자열미발견을모든구현부재로확대하지않으며구코드존재자체를버그로단정하지않는다.
실제읽은경로: Server=Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server,Unity=unity-project/Assets/_LASTRAIL/Scripts. 새17사건ID문자열은Server packages·game-api/src 및Unity Scripts에서미발견,기존1장키는존재했다. 운영지원/발행완료주장없음.

| 번호 | 충돌·공백과 근거 | 조치/인수 소유 |
| --- | --- | --- |
| ST17-01 | Unity Presentation/MissionShell/DepartureRuntime.cs:45의Operations는기존7개,ActOneMissionNarrativeFixtures.cs/UnityDisplayLabels.cs도1장행존재.새10ID미발견 | S-01/S-02가명시정의/적격/원결과계약제공→기존Unity스토리소비에서버전별목록/키를연결.구목록을단순17개로대체해구START재생을깨지않음 |
| ST17-02 | balance/src/index.ts maxRunWaves=15 및승리시15조건(227/774/898행대조)이남음.반면game-api/src/run-reward-settlement.ts는영속preview/원경제권리처리모듈존재 | 기존S-02에서새N/V생산자→preview→원정산을끝까지대조.구15계산기삭제나정산기전면재작성아님.스토리event ID와run operationId를동일시하지않음 |
| ST17-03 | offline-economy.ts의progress는1장8키(none포함)다.같은파일은실economy binding을읽어시간단가를대체하므로구balance단가만으로현행수입을단정할수없음 | S-02경제/진행계약에서새스토리→승인된3배12시간등급투영·24/2/1/20시간단가및근무구간을대조.실binding조회실패를none으로저장하지않음 |
| ST17-04 | story-facility-state/store/interaction/waves/attack 모듈과기존S-02부분검증이존재.새8사건의비피격사후객체를구피격시설HP600모델로일괄적용하면의미충돌 | S-02의기존상태/멱등/행동점유기반재사용,비피격객체와피격시설능력구분.새8사건의확인상태/진영적개체/16wave지원매핑필요 |
| ST17-05 | 스토리유한HP/300초·계정첫보상과월드무한HP/180초·점수는같은인물이어도다른정의.실arena지원은미확인 | S-01/S-02스토리와기존S-WB-01월드를분리.전역최초Act완료/칭호/주간예약의새원장연결필요.별보스추가·월드ID로스토리완료금지 |
| ST17-06 | 새codex공개/잠정성공슬롯·자기장완료회고매핑은문서승인,실응답/화면/음성연결증거없음.기존WAIT-010은주로run기록목록계약 | WAIT-003의사건진행과WAIT-010의표현/복구경계를연결하되스토리기록과작전이력의권리/목록을같은것으로합치지않음.기존미술유지·화면재제작을자동요청하지않음 |
| ST17-07 | 베일3anchor·오르트내외부창구의모든공격사거리·센트리구역·포탑중단/복구실행근거미확인.오르트‘실객차1개’강공격의구체표적선정이새명세에빠져있음 | Plan이아래선정안을구체화한뒤Server/Unity의원공격계약과검증.무한사거리/미지원기본지원사격으로메우지않음 |
| ST17-08 | QA/StoryBossBalance-20260929는72개기본공격산술,객차4회강공격파괴고립조건.신규8사건96개검산도EHP할당뿐 | 실전무장비/저획득/중·고성장·AUTO·장치·회복/방벽·객차복구QA는잔여.산술PASS를밸런스/출시PASS로표기하지않음 |

### 남은 설계 판단 — 오르트 강공격 표적
추천 후보는예고시살아있는실객차중열차앞쪽부터1개고정,동률은원START슬롯순이다.예고중파괴/대상소실이면그타격취소·즉시다른객차로전환하지않고다음예정주기에다시선정한다.열차본체직접피해로자동치환하지않는다.이안은정식공간순서/원물리위험계약과대조해야하며아직승인/적용하지않는다.대안최저HP집중은기존손상을집중공격해복구난도를높이므로첫시험에는추천하지않는다.

### 의존성과 중복 작업 정리
현재Server의S-01(정의/계산/실producer),S-02(맵·피해저장·물자/상호작용·정산),S-WB-01(월드바인딩)은진행중부모다.같은기능을새S-ID로중복등록하지않고기존WAIT-003/010와월드후속정의에이번원문/수용차이를연결한다.기존S-12협동계약을새스토리협동지원승인으로해석하지않는다.
현재Direction D-WB-01은월드시안,D-EQ-01은장비/보관함이다.이들을스토리10사건제작완료로취급하지않고새스토리외형개체/목표/창구의필요자산만별도검토한다.기존미술을전면재제작하지않는다.원화작업과런타임계약선행은프로젝트기존규칙대로분리한다.
홈페이지H-02/03/04 및Admin A-02/03은현재공개읽기/문의/커뮤니티범위다.신규스토리콘텐츠를이큐에임의추가하지않는다.실제운영/공개API영향이확인될때해당소비작업을등록한다.과거기획의미확정/승인대기는최신승인절이대체하지만보존자료삭제/타담당진행블록삭제는하지않는다.
이번에는충돌점검과대기인수정의만기록했다.후속실행큐등록은기존조건대로원담당이선행완료근거/실버전/QA를남긴뒤수행한다.자기개발원본·타큐진행상태를이세션에서수정하지않았다.후속조건은[WAIT 통합인수](../../handoffs/INTEGRATION_WAITLIST.md#story17-integration-intake-20260930)에기록한다.

<a id="story17-target-resolution-20260930"></a>
## ST17-07 표적 정책 해소·구현 인수 경계 — 2026-09-30
[32 오르트표적승인](32_NARRATIVE_REFERENCE.md#ort-story-target-approved-20260930)에사용자승인및12개경합/복구사례반영. 앞선표적추천미승인문구는해소한다. 단,ST17-07전체는실공격원분류·베일3anchor/오르트내외부지원·센트리/포탑사거리의실증이남아열린상태다.발사후투사체/물리위험의파손타깃처리를같은것으로혼합하지않는다.
이번정책으로새즉사·재표적·자동직접열차피해를만들지않는다. ST17-01~06/08의계약/구현대조도원대기유지다. 다음실제인수에서필수증거는①17사건지원정의/진영개체표②보스공격원분류·대상/사거리행렬③N/V정산/첫권리/오프라인투영원결과④구START·재접속/대사공개회귀⑤실편성의처치·다운/객차복구기록이다. 어느하나의문서작성/내부계산통과를전체전투완료로보고하지않는다.

<a id="story17-source-intake-20260930"></a>
## 17사건 구현 연결 상세 대조 — 2026-09-30
사용자 ‘승인 다음’에 따라 승인된 표적·복구 조건을 유지하고 실제 소스를 추가로 읽었다. 아래는 개발 체크아웃의 정적 확인이며 운영 장애 또는 전체 저장소 구현 부재의 판정이 아니다. 실행 코드·타 담당 큐는 변경하지 않았다. 앞선 점검의 ‘새17사건ID 미발견’은 전체 17개 중 **추가된 10개 ID**의 검색 결과를 뜻한다.

### 확인된 연결 차이와 재사용 범위
| 항목 | 소스 근거 | 인수 시 필요한 변경·증거 |
| --- | --- | --- |
| ST17-04 전투 후 확인 | Server `apps/game-api/src/story-facility-state.ts`: definitionFields는 4종 피격 시설과 HP600 고정. postCombat 중 pending interaction을 금지하며, 상호작용 명령은 전투 종료 후 거절한다. END_COMBAT은 기존 상호작용을 종료한다. | 새 8개 비피격 확인 객체를 기존 시설 ID 추가만으로 처리할 수 없다. 피격 시설 계약은 보존하고 비피격·전투 후 상호작용을 명시적으로 지원하는 정의와 상태를 연결한다. 2웨이브와 필수 선택 완료→1.5초 확인→잠정 성공→작전 승리 시 계정 반영을 검증한다. 중복 확인·재접속·확인 전 이탈은 추가 지급을 만들지 않아야 한다. |
| ST17-02 첫 보상 권리 | Server `run-reward-settlement.ts:39–47`은 저장된 reward plan의 calculation.operationId를 `prepareLaunchRewards`에 전달한다. `launch-entitlements.ts:5–18`의 ticketAmounts/coreAmounts는 기존 사건만 지원하며, 제한 대상 보상에 매핑이 없으면 launch_source_unrecognized를 발생시킨다. | 추가 사건의 모집권·코어를 이 경로로 보내면 기존 매핑만으로 처리할 수 없다. 실제 생산자의 storyEventId와 run operationId 결합을 검증하고 계정별 사건 첫 권리로 연결한다. 기존 중복 방지 원장·거래를 재사용한다. 보상표 숫자 추가만으로 완료하지 않는다. 현재 변환 경로는 모집권을 수정으로 변환할 수 있으므로 최신 경제 계약과 최종 지급 ID·수량도 대조한다. 이 점검으로 별도 통화 전환 정책을 승인하거나 철회하지 않는다. |
| ST17-07 객차 피해 | Unity `Battle/CombatDamageRouter.cs:78–120`은 해결된 피해량을 받아 객차 보호막·HP 처리 후 실제 HP 손실의 30%를 열차에 한 번 전이하며 eventId 재처리를 방지한다. 클래스 주석은 비활성 경계와 원장 복원 후속 연결을 명시한다. Scripts 범위 검색에서 외부 호출을 확인하지 못했다. | 이 도메인 처리를 재사용하되 보스 원공격→DEF/피해감소 계산→router→복구/저장 연결을 검증한다. router는 resolvedMagnitude 입력이므로 원피해432를 그대로 최종 피해로 넣지 않는다. 인메모리 중복 방지와 재접속 후 중복 방지는 별도 검증이다. 표적 파괴 취소·발사 후 처리·사거리 구현은 아직 확인되지 않았다. |
| ST17-06 기록실 조회 | Unity `Narrative/StoryArchiveReadModel.cs:38–81`은 `/v1/story-progress`, 계정/요청 세대 검사, 실패 시 상태 제거, 응답 버전 일치·목록 중복 검사를 이미 제공한다. actId는 act_01_wasteland_railway다. | 기존 조회 모델을 사용해 승인된 추가 사건의 서버 완료 목록과 기록 내용을 연결한다. 새 act1.story17.v1은 정의 버전이며 actId 대체값이 아니다. 이 리더의 존재만으로 완료 기록 공개 조건·실제 화면 연결이 검증됐다고 보지 않는다. 작전 실패·잠정 성공·계정 전환에서 미해금 기록이 노출되지 않는지 확인한다. |

Server 기준 경로: `D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server`. Unity 기준 경로: `D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts`. 파일·함수와 확인 범위가 근거이며 줄 번호는 이후 변경될 수 있다.

### 연결 검수 순서
1. S-01/S-02에서 새 사건 정의와 비피격 사후 확인 상태, 사건별 최초 보상 원천을 먼저 연결한다. 정상·실패·중복·구 START 결과를 남긴다.
2. 보스 공격 종류·발사 시점·명중 시점·표적 소실 처리·실제 사거리 행렬을 확정된 공간 정의와 대조한다. 아직 지정되지 않은 공격을 투사체나 열차 직접 피해로 임의 분류하지 않는다.
3. 해당 서버 계약과 필요한 독립 자산이 준비되면 기존 Unity 후속 범위에 소비 작업을 등록한다. 피해 router와 기록 조회 모델을 중복 작성하지 않는다.
4. 실제 정산·재접속·기록 공개·전투 플레이를 검수한다. 현재 이 사례들은 NOT_RUN이며 전투 밸런스 완료 또는 출시 가능 판정은 아니다.

새 밸런스 수치나 보상 정책을 제안한 단계가 아니라 기존 승인안의 구현 연결 조건을 구체화한 단계다. 남은 공격 분류·공간 적격성과 월드 배치 별도 미결은 유지한다.

<a id="vitals-boss-source-audit-20260930"></a>
## 최대HP·보스 표적 승인 규칙의 실제 연결 점검 — 2026-09-30

사용자 승인에 따라 담당 인수 묶음 다음 단계로 소스를 대조했다. 정책 원본은 [13 최대HP 승인](13_EQUIPMENT_ENCHANTMENTS.md#max-hp-current-health-rebase-approved-20260930), [44 공격·창구 승인](44_COMBAT_DETAIL_CONTRACTS.md#boss-attack-surface-resolution-approved-20260930)이다. 이번 작업은 기획 문서 보완이며 실행 코드 변경이나 실전 테스트가 아니다. 아래는 읽은 파일의 관찰 결과로, 다른 브랜치·운영 환경의 전체 구현 부재를 뜻하지 않는다.

Unity 경로 기준: `unity-project/Assets/_LASTRAIL/Scripts/Battle/`. Server 경로 기준: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/`. 행 번호 대신 파일과 멤버를 인수 키로 사용한다.

| 인수 키 | 실제 관찰 | 필요한 연결 / 소유 |
| --- | --- | --- |
| VS-01 | CombatVitals의 MaxHp는 생성 시 고정이며 현재HP 보정 기준쌍이 없다 | Unity 전투 상태 소유자가 유효 최대HP와 마지막 실제HP 변경 기준쌍을 관리한다. 장비 거래의 최종 스탯이 확정된 뒤 보정은 한 번 적용한다. 원START/Config를 현재 상태처럼 덮어쓰지 않는다. S-01/S-02는 동일 의미의 버전·거래 경계를 제공한다 |
| VS-02 | Damage, ApplyTransferredHpLoss, RestoreHp, RecoverFromDown이 실제HP를 변경한다 | 네 경로 모두 실제 양수 손실/회복이 발생한 결과에서 기준쌍을 갱신한다. 보호막만 소모·0피해·0회복·순수 최대HP 보정·저장/복원에서는 갱신하지 않는다. 전달 피해도 빠뜨리지 않는다 |
| VS-03 | CombatSkillCommands의 HpComparer, armor_when_low_hp, RecoverOperators가 Config.MaxHp를 읽는다. 회복량 사전 검사도 같은 값 사용 | 현재 비율 기반 대상 선택·저체력 조건·예약 회복 실행은 해당 시점의 유효 최대HP를 사용한다. 원스냅샷 설정과 실행값을 혼동하지 않는다. 회복 대기 중 장비 변경은 회복 시각/보호 시간을 새로 시작하지 않는다 |
| VS-04 | 같은 파일의 CASTER_MAX_HP 및 열차 최대HP 효과 분기가 Config.MaxHp를 사용한다 | 효과별 승인된 계수 스탯과 스냅샷 시점에 맞춰 값을 공급한다. 모든 효과를 무조건 명중 시 재계산하거나 시전 시점으로 일괄 변경하지 않는다. 각 handler의 원계수·시점 매핑을 제출한다 |
| VS-05 | CombatHudReadModel의 요원·열차·스킬 표시 maxHp가 Config.MaxHp다 | View는 상태 소유자가 제공한 유효 최대HP를 표시한다. HUD만 바꾸거나 표시값으로 HP를 역산하지 않는다. 갱신이 회복/장비 명령을 다시 발생시키지 않는다 |
| VS-06 | CombatVitals.CaptureLocal은 hp/lastTick/layers만 기록하고 RestoreLocal은 이 필드 집합을 엄격 검사한다 | 최대HP·기준쌍을 포함한 새 저장 버전과 검증 경계를 먼저 정의한다. 구저장에 필드를 몰래 추가하지 않는다. 새 버전의 기준쌍 누락/범위 오류는 거절하고 손상된 값에서 임의 복구하지 않는다. 기존 START는 원정책·원리더로 유지한다. S-12 envelope와 Unity 복원 연결 시 함께 검증한다 |
| VS-07 | CombatVitals는 생성 시 shieldCapBps≤10000으로 HP 기반 shieldCap을 만들며 발급·RestoreLocal 모두 제한한다 | 최신 직접 보호막의 전역 HP 상한 제거 정책에 이 구경로를 그대로 재사용하면 잘리거나 복원이 거절될 수 있다. 실제 선택되는 정책/호출 경로부터 증명하고, 구버전 제한은 보존하면서 최신 프로필을 분리한다. 최대HP 보정으로 기존 방벽 잔량을 재배율하지 않는다 |
| VS-08 | Server combat-damage-store.ts의 combat.damage-state.v1은 initialTargets와 targets의 maxHp 동일 및 피해 원장으로 복원되는 hp를 요구한다 | 이 모듈은 내부 피해 영속 경계이며 전체 Vitals 원장이라고 간주하지 않는다. 최신 전체 상태로 재사용할 경우 회복/최대HP 변경 사건을 수용하는 명시적 계약·순서·멱등 검증이 필요하다. 단순 검증 제거 금지. 정상 협동을 서버 매타격 판정으로 바꾸거나 raw client damage API로 노출하지 않는다 |
| VS-09 | BasicCombatSimulation.ResolveBasic은 대상 HP0 시 살아있는 공격자가 새 대상을 고르고 전체 예고 시간을 재부여한다 | 새 스토리 베일/오르트 6공격의 고정 표적 소실 취소 정책에 구경로를 그대로 쓰지 않는다. 명시적 공격 프로필/handler로 연결하고 구 일반 공격·구START 동작은 보존한다. HP0뿐 아니라 제거·파괴·접근 무효도 해당 신공격 계약대로 확인한다 |

### 담당자가 제출할 관찰 가능한 검증
아래는 기존 HP/AT 인수 사례를 실제 변경 위치에 연결한 것으로 별도 게임정책이나 중복 큐가 아니다. 현재 실행 상태는 모두 NOT_RUN이다.

1. 333/1000 → 최대HP1500에서499 → 저장/복원 → 최대HP1000에서333. 왕복 동안 기준쌍이 변하지 않으며 방벽 잔량/만료도 유지된다.
2. 499/1500에서 실제HP 피해1이면498/1500을 새 기준으로 삼아 최대HP1000에서332. 보호막만 피해1을 흡수했다면 기존333/1000 기준을 유지한다. 실제 회복·수리·전달 피해·예정 복귀도 각각 독립 사례로 확인한다.
3. 최대HP 증가/감소 후 HUD 최대값, 최저HP비율 대상, 저체력 조건, 회복량이 같은 유효 상태를 참조한다. 계수 효과는 각 효과의 원스냅샷 시점으로 별도 확인한다.
4. HP0 중 최대HP가 바뀌어도 부활하지 않는다. 원래 복귀 시각에 원프로필 비율로 복귀하며 기준쌍을 갱신한다. 기존 보호 시간/객차와 포탑의 독립 복귀 정책을 변경하지 않는다.
5. 최신 직접 방벽이 수신자 최대HP를 넘는 합법적 경우 발급과 저장/복원이 모두 유지된다. 구정책의 제한 사례는 여전히 원결과를 재현한다.
6. 새 스토리 공격의 예고 중 표적 소실 → 이번 타격 취소, 다른 표적으로 즉시 이동 없음. 구 일반 공격의 재선정은 원버전대로 유지한다. 선택 창구·단일 본체 중복 방지·명중 시 감쇄는 기존 AT 사례와 함께 검증한다.
7. 거래 재전송/체크포인트 복원에서 최대HP 보정이 중복 적용되지 않고 실제HP 기준쌍도 재설정되지 않는다. 구저장/신저장/손상저장 결과를 구분한다.

### 인수와 다음 경계
현재 발견 사항은 이미 승인된 정책을 구현에 연결하는 작업이므로 신규 수치 승인을 다시 요청하지 않는다. 실제 클래스 분리·wire 필드·버전 이름은 원담당이 기존 구조에 맞춰 확정한다. 단순 Config.MaxHp 전역 치환이나 새 전체 상태 싱글턴 도입은 해결로 인정하지 않는다.

기존 S-01/S-02는 계산·거래·영속 계약, S-12는 전체 복원 계약을 담당한다. 해당 선행이 인수된 뒤 기존 Unity 후속 범위에 VS-01~09 중 소비 대상만 전달한다. [WAIT 단일 진입점](../../handoffs/INTEGRATION_WAITLIST.md#combat-object-role-intake-20260930)을 유지하며 타 담당 큐를 수정하거나 완료로 처리하지 않았다.

다음 점검은 실제 arena/profile의 공격 사거리·표적 selector·창구 ID와 승인 배치의 매핑이다. 실제 좌표/지원 정의가 없으면 임의 숫자를 채우지 않고 담당 계약 필요로 기록한다. 전체 전투 밸런스·협동/월드 실플레이 검증은 여전히 잔여다.

<a id="boss-arena-source-audit-20260930"></a>
## 보스 전장·사거리·타격 부위 연결 대조 — 2026-09-30

[44 승인 배치](44_COMBAT_DETAIL_CONTRACTS.md#boss-arena-reachability-approved-20260930)와 H-R3/AR-01~10을 실제 Unity Battle 소스와 대조했다. 새 배치나 거리 수치를 제안하는 작업이 아니다. 아래는 정적 소스 대조 결과이며 실행/미술 검증은 NOT_RUN이다.

대조 범위: `unity-project/Assets/_LASTRAIL/Scripts/Battle`의 BasicCombatSimulation, CombatSkillCommands, CombatExteriorLayout, CombatDamageRouter. Server `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src` 및 Unity Scripts에서 world_boss_, raid_veil, raid_ort, surfaceId, arenaProfile, bossBody 문자열을 검색했지만 일치하지 않았다. 이는 해당 검색 범위/키의 결과이며 다른 명칭·데이터·브랜치에 구현이 없다는 증거가 아니다.

| 항목 | 확인한 구현 | 최신 승인과 연결할 사항 |
| --- | --- | --- |
| AR-S01 기본 공격 | SelectTarget은 내부/외부·Train/Operators 구분, HP/AvailableAt, Slot/Id 정렬과 focus를 사용한다. FrontReach는 Slot<2 조건이다 | 이것을 실제 객차 거리나 베일3anchor 지원으로 간주하지 않는다. 실제 carId·논리 위치·원공격 적격을 연결한 프로필과 결과가 필요하다. 기존 슬롯 규칙은 구프로필에 보존한다 |
| AR-S02 스킬 표적 | SkillTargets는 내부/외부 후보와 focus, enemies_front_2의 Take(2)를 사용한다 | 타격 부위 두 개를 적 두 명으로 등록하면 동일 보스가 두 대상이 되는 문제가 생길 수 있다. 단일 본체를 먼저 식별하고 승인된 부위 선택/중복 제거를 적용해야 한다. 정상 다단과 서로 다른 실제 적은 보존한다 |
| AR-S03 화면 좌표 | CombatExteriorLayout은 스스로 presentation coordinates이며 gameplay distance/routing이 아니라고 명시한다 | 화면 Road0/Road1 좌표·크기·스케일을 서버 사거리로 복사하지 않는다. Direction은 표시 anchor 대응, 판정 담당은 논리 위치/범위의 권위 있는 정의를 제공한다 |
| AR-S04 객차 피해 | CombatDamageRouter는 train/carriage별 Vitals와 eventId 원장을 제공하지만 주석상 비활성 경계이며 producer 연결/원장 복원이 후속이다 | 오르트 강공격의 실제 앞쪽 살아있는 객차 선택→예고 고정→취소/피해→열차 전달 연결 증거가 필요하다. 라우터 존재만으로 보스 강공격 지원 완료가 아니다 |
| AR-S05 보스 등록 | 위 범위의 지정 키 검색에서는 새 보스/arena 연결을 확인하지 못했다 | S-01/S-02/S-WB-01이 실제 등록 파일·프로필 버전·handler와 지원 편성을 제출해야 매핑을 확정한다. 임의 ID·좌표를 실행 기본값으로 만들지 않는다 |

### 보스별 반드시 제출할 연결
- 스토리/월드 베일: 실제 전투 객차 하나에 묶인 세 anchor, 원기본공격/스킬의 단계별 적격, 외부 부위→같은 본체 연결. 스토리 HP 단계/2초 전환과 월드60/120초 전환을 구별한다.
- 스토리 오르트: 내부 제어 연결부·외부 통제부→같은 유한 본체, 승인된 단계별0.85 감쇄와 장치 상태, 실제 객차 강공격 경로. 월드 정의로 자동 승계하지 않는다.
- 월드 오르트/약탈왕: 내부 연결부·외부 공격부→단일 점수 대상, 서버 판정과 원기믹. 연결부에 독립HP·추가 처치/보상을 만들지 않는다.
- 공통: Eve 센트리의 원객차 방어구역, 포격 객차의 실제 두 슬롯별 공격 적격, 분산 요원 편성, 파손/복구 후 재개를 원규칙대로 대조한다. 현재 소스 검색만으로 센트리/모든 편성의 지원 여부를 판정하지 않았다.

### 담당 인수 산출물과 완료 기준
기존 AR 행렬에 각 행의 실제 파일/정의 참조, mode·boss·arena 버전·phase, source entity/ability·car/slot·target surface/body, 원범위/선택 규칙, 기대 허용/거절, 실행 결과를 연결한다. 이 항목들은 의미 계약이며 새 API 필드명 강제가 아니다. 거리 표현도 기존 엔진의 논리 구역 또는 geometry를 확인한 뒤 정하고 임의 미터값을 넣지 않는다.

최소 비교 결과는 (1) 세 anchor/각 단계에서 적격 공격 가능, (2) 부적격 구역 공격 거절, (3) 내부/외부 동시 접촉에도 한 논리 타격은 본체 한 번, (4) 정상 다단은 보존, (5) 화면 크기/카메라 변경 전후 판정 동일, (6) 장치 탭과 공격 명령 중복 없음, (7) 파손/다운·복구 후 원주기 재개, (8) 구START 원결과 보존이다. 구체 실행 사례는 기존 AR-01~10/AT 사례를 재사용하며 새 중복 테스트 묶음을 의무화하지 않는다.

기획 배치 방향은 승인 완료다. 실제 등록/좌표/사거리·행동 결과는 원담당 선행이며 이를 Plan이 추측해서 닫지 않는다. 동일 내용의 상세안을 반복 작성할 단계는 종료하고 실계약이 제출될 때 통합 검증을 재개한다. 다음 독립 기획은 현재 Plan 큐의 보관함 버리기·상인 수리/전체 요원 회복 상품이다. 큐의 최신 지시에서 버리기 추가 확인이 제거된 점을 반영하고, 이전 확인 팝업 요구를 그대로 복원하지 않는다. 신규 가격·회복량은 승인 후보로 분리한다.

<a id="internal-playtest-sequence-v1-20261001"></a>
## 내부 플레이테스트 진행 순서·판정 초안 v1 — 2026-10-01

상태: 사용자 ‘다음 기획’ 요청에 따른 기획 초안. 아래 시험 운영 순서·판정 방식은 승인 대기다. 기존 게임 정책은 변경하지 않는다. SNS는 내부 테스트 이후 재검토한다. 목적은 실제 한 판에서 진행이 막히는 문제, 판정/정산 오류, 재미·난이도 문제를 구분하여 발견하는 것이다. 문서 검토·계산 모형·개별 자동 검사만으로 실제 플레이 완료를 선언하지 않는다.

### 선택한 접근
- 모든 모드 동시 시험은 문제 원인과 선행 구현을 분리하기 어렵다.
- 수치 계산만 먼저 반복하면 입력·이동·대상 선택·지원 스킬의 실제 사용 기회를 놓친다.
- **추천: 일반 솔로 한 판 연결 → 선택/성장 비교 → 일반 협동 → 월드보스 순으로 확장.** 각 단계의 필요한 계약만 확인하고 무관한 홈페이지/SNS 완료를 요구하지 않는다. 해당 모드의 독립 기능 검증은 준비되는 대로 병행할 수 있으나 다른 모드 통과를 대신하지 않는다.

### 실행 단위·진입 조건
한 시험 묶음은 빌드/전투규칙/콘텐츠 정의 버전·모드·START 편성·실제 지원 장비/증강 범위가 같은 사례들로 구성한다. 승인 문서 버전과 실제 적용 버전을 함께 기록한다. 실입장 가능한 편성과 강제 주입 fixture를 구분하고, 미지원 효과를 0효과로 치환해 밸런스 자료에 섞지 않는다. 격리된 테스트 계정/환경을 사용하며 실제 운영 재화나 랭킹에 영향을 주는 시험은 이 초안의 범위가 아니다.

44의 객체 통합 검수 및 WB-G1~4, 이 문서의 VS/AR 검증을 원근거로 재사용한다. 각 사례에 실제 계약/결과가 없으면 BLOCKED 또는 NOT_RUN이며 PASS로 승격하지 않는다. 모의 서버로 가능한 화면 검수와 실제 권위 판정·영속 정산 검수를 분리한다.

### 단계별 플레이와 완료 근거
| 단계 | 실제 플레이 범위 | 확인할 결과 / 다음 단계 판단 |
|---|---|---|
| IT-01 일반 솔로 한 판 | 편성·출발 → 지도 전체 사건 아이콘/경로 선택 → 전투·이벤트 → 장비 획득/장착/승급/보관함 정리 → 최종 작전 종료·정산·로비 | 12개 방문 노드의 해당 run 흐름과 승인 정산 시점을 대조. 획득 창, 정비, 출발 사이 진행 막힘 없음. 원보상·거래는 정확히 한 번. 다른 사건 유형은 별도 경로 사례로 보충하고 한 판에서 전부 나왔다고 가정하지 않음 |
| IT-02 선택·성장 비교 | 지원 가능한 저/중/고성장 편성, 장비 분산/집중, 지원 적음/있음, AUTO 켬/끔 | 같은 조건에서 한 축씩 비교. 딜 상승뿐 아니라 처치 시간·피격·회복 반응창·행동 선택 변화와 투자 대가를 함께 확인. 고점 장비 강제 지급 사례로 자연 획득률을 계산하지 않음 |
| IT-03 일반 협동 | 정상 2인, 전투 중 한 명 단절, 잔류자 솔로 클리어, 호스트 변경/복원, 재접속·응답 유실 | 기존 권위·checkpoint·원거래 계약 적용. 단절 후 미복귀한 사람에게 잔류자의 클리어 보상을 복제하지 않음. 잔류자는 기존 자격에 따라 1회 수령. 복귀자는 최신 참가/보상 규칙으로 별도 판정. 필요한 S-12 실계약 인수 후 수행 |
| IT-04 월드보스 | 약탈왕/베일/오르트 각각 솔로·협동, 단계 전환·기믹 성공/실패·종료/복귀 | 서버 HP/피해/점수/정산, 단일 본체와 공격 창구, 열차/요원 피해 분리. 44#world-boss-balance-acceptance-v5의 WB-G1~4 및 승인 첫 적용 수치를 사용. 일반 협동 방식으로 권위를 바꾸지 않음 |

### 밸런스 비교 방법
동일 정의/맵 seed를 재현할 수 있으면 원본을 기록해 비교하고, 불가능하면 서로 다른 경로/적/보상 조건을 명시하여 장비 하나의 효과로 단정하지 않는다. 테스트용 자연 획득 run과 특정 빌드 fixture는 결과군을 분리한다. 전투 중 편성/규칙 변경이 생기면 해당 비교군을 분리하고 진행 중 작전의 START를 소급 변경하지 않는다.

필수 관측은 클리어/실패 위치·전투 시간, 열차/요원 각각 최저 HP와 다운/파괴, 순간·연속 피격, 실치유/초과치유, 보호막 실흡수/만료, 선택한 증강/장비/인챈트·슬롯 투자, 스킬 입력/발동 시점이다. ‘왜 해당 경로/장비를 골랐는지’, ‘위험을 알아차리고 대응할 수 있었는지’, ‘투자가 체감됐는지’도 짧게 남긴다. 끝 HP만 높다고 전투 전체가 쉬웠다고 판정하지 않는다.

공격 주기·가속에 새 상한을 만들거나 승인 계수를 임의 하향하지 않는다. 어려움은 우선 대상 선택·동시 피격·예고/반응창·실제 지원 효과 누락을 구분한다. 쉬움은 실흡수/실치유와 화력의 투자 대가를 함께 확인한다. 수치 변경이 필요하면 원버전·관측·원인 가설·변경 후보를 묶어 별도 승인받는다. 기존 35%/3초 등 진단선은 피해 하드캡이 아니다.

### 실패 분류와 재시험
- 진행/경제 차단: 진행 불가, 저장 유실, 중복 보상/차감, 다른 대상 피해·권위 오류. 영향 사례의 밸런스 결론을 보류하고 담당 수정 후 같은 사례를 재시험한다. 독립된 화면 검수까지 모두 중단시키지는 않는다.
- 전투 품질: 지나친 무피해 유지·반응 불가 연속 다운·투자 효과 미발동·선택 간 운용 차이 부재. 재현 조건과 타임라인을 남기고 원인 확인 후 후보를 제안한다.
- 표시/입력: 수치 불일치, 중복 입력, 예고 가독성, 독립 요소 연결 문제. 표시 문제와 실제 판정 문제를 구분하되 조작을 막으면 진행 차단으로 올린다.

결과 상태는 PASS/FAIL/BLOCKED/NOT_RUN으로 기록한다. 기능 PASS는 해당 사례의 기대 결과 충족이며 전체 밸런스 확정을 뜻하지 않는다. 밸런스 결론은 관측 있음/추가 표본 필요/조정 후보로 별도 남긴다. 임의 판수만 채우면 완료하거나 소표본으로 드랍률을 보장하는 기준은 두지 않는다.

### 담당 인수·기록 최소 형식
기존 QA/인계에 사례ID, 실제 버전/환경, 선행 계약, 재현 입력, 기대 결과, 관측 결과/원거래·타임라인 근거, 제외 범위, 실패 담당과 재시험 결과를 기록한다. Server는 권위/정산·복원, Unity는 실제 입력/플레이 연결, Direction은 승인 화면·독립 요소 가독성, Plan은 승인 규칙 대조와 밸런스 해석을 맡는다. 개인정보나 비밀값을 증거에 넣지 않는다.

현재는 시험 운영 초안만 작성했다. 실제 빌드 적격·통합 테스트·플레이 결과는 미확인이다. 기존 S-01/S-02/S-12/S-WB-01 및 소비자 후속을 중복 생성하지 않으며, 초안 승인 및 해당 선행 인수 후 필요한 실행 항목만 기존 완료 기반 큐 절차로 등록한다. SNS 재개·외부 공개는 이 시험의 자동 후속이 아니다.

<a id="implementation-queue-audit-20261001"></a>
## 순차 작업 점검: 소비자 선행·솔로 한 판 잔여 — 2026-10-01

사용자 ‘순서대로 ㄱㄱ’에 따라 기존 담당별 큐·Server/Unity 인계와 QA/ApiVerification-20261001/README.md, Unity API 검사 XML을 확인했다. 다른 담당 실행을 대신하거나 재시험한 결과가 아니다. 신규 게임 정책/실행 큐 중복 생성 없음.

### 1. 큐 선행 대조 결과
| 항목 | 지금 가능한 범위 | 실제 잔여 |
|---|---|---|
| A-02 | 기존 processing GET/POST·답변/메모·오류/원결과 소비 착수, Server 보충 인수 있음 | 최신 승인 OWNER grant관리·종료조건 구현 인수, 실제 인증 환경 왕복 |
| H-03 | 웹세션·본인 접수/receipt·목록/상세 계약 소비 착수, Server 보충 있음 | 실제 HTTPS Origin/OAuth·쿠키 브라우저 검증, 승인 로그인/문의 UX 반영 |
| H-02 | 공지/공시/공개 index·catalog DTO 및 오류 소비 착수 | 실제 승인 source·WEB+PUBLIC 자료와 현재 적용 성공 근거 |
| A-03/H-04 | 공통 인증 준비 재사용 | 커뮤니티 실제 API/DB 연결. Server의 content-state 단위 구현은 전체 API 완료 아님 |
| U-CORE-01 | 최대HP·숫자DEF 도메인 구현/검증 부분완료, 나머지 로직 활성 | 실제 효과 생산자·정의/START·작전 거래 연결 |
| U-API-01 | 공통 검사 도구 및 로컬 계정조회 검사 완료, 담당이 실행 블록 삭제 | 실제 문의/경제/공개 자료·Player/DEV/D1 전반은 미검증. WAIT의 후속 인수 조건 유지 |

A/H 큐의 과거 ‘전체 선행 대기’ 제목보다 server-contract-intake-20261001의 범위별 해제가 우선한다. 소비 인수 누락은 발견하지 않았으며 원담당 블록을 재작성하지 않는다. 최신 승인된 폼/로그인/권한 정책 참조를 추가 인계한다. Server가 해제한 범위를 다시 전체 대기로 묶지 않는다.

Unity API 결과는 GET /v1/account/state의 정상/인증거절/정상복구 등 실제3HTTP 사례, 실제HTTP 뒤 응답주입2사례 및 모의/설정검사를 포함한80/80이다. 실제 로그인 발급 흐름이나 전체 API 정상의 증거로 확대하지 않는다. U-API-01을 새로 등록하지 않고 실제 지원환경 제공 시 WAIT#unity-api-verification-client-evidence-20261001에 따라 미검증분 후속만 등록한다.

### 2. 솔로 한 판 구현 인수 순서
| 흐름 | 닫아야 할 연결 | 기존 소유 큐·실제 완료 증거 |
|---|---|---|
| 편성→출발 | 실제 지원 정의/효과에서 START 생성, 미지원 거절 | S-01/S-02→U-CORE: 실제 정의/version/지원효과·원START. 요구 카탈로그만으로 실행 가능 처리 금지 |
| 전투 | 실제 스킬/증강/장비/지휘실 입력→계산→보호막/HP/다운·종료 | U-CORE 및 S-01 인수: 현재 최대HP·DEF 단위근거 재사용, 실제 producer/전투 결과 별도 |
| 경로·사건 | 원그래프/배정→선택→도착→해당 사건 처리·종결 | S-02→U-CORE: 실제 도착/종결 검증과 원거래. 지도 표시 연결은 시안 완료 후 |
| 획득·정비 | 실제 보상 공급·장비 합성/시술·상인/정비 HP·물자 거래 | S-02→U-CORE: 기존 저장 코어를 실제 source/권위 거래와 연결, 중복0/유실복구/원장 결과 |
| 최종 종료→정산 | 실제 전투/사건 결과→종료 자격→1회 정산→본인 상태 재조회 | S-02 및 U-CORE/API 후속: 한 run 원ID의 끝까지 이어진 결과. 부분 모듈 검사를 완주 증거로 사용하지 않음 |

현재 전투·장비·거래 코어가 다수 구현됐지만 위 실제 생산/적용 연결은 담당 인계상 잔여다. 새로운 기획이 없어 못 만드는 것과 실제 API/정의 연결이 미완료인 것을 분리한다. 같은 기능은 이미 S-01/S-02/U-CORE에 등록돼 있으므로 신규 중복 항목은 만들지 않는다. 협동 S-12·월드 S-WB-01은 별도 선행이며 솔로의 독립 작업을 막지 않는다.

### 3. 다음 경계
각 담당은 기존 활성 큐 구현을 계속하고, Plan은 제출된 실제 정의·원결과·검증 근거를 위 흐름으로 인수한다. 화면 시안 완료 전 Unity 화면 연결 큐는 발행하지 않는다. 실제 내부 플레이테스트는 아직 NOT_RUN, SNS 보류 유지. 공개 자료 승인/게시 절차 보완은 실제 source 인수와 구분하며 fixture/MD를 실공개자료로 대체하지 않는다.

<a id="solo-start-runtime-gate-20261001"></a>
## 일반 솔로 실제 START 차단 소스 확인 — 2026-10-01

실제 Server apps/game-api/src/index.ts의 신규 출발 경로(확인 시5982~5988)는 현재 departureVersion 검사 뒤 describeCombatRequirements 결과와 함께 combat_not_ready409를 반환한다. 저장된 성공 재생은 앞에서 별도 처리한다. combat-policy-binding.ts의 bindCombatPolicyRequirements는 canExecute=false로 시작하고 정상 요구사항 매핑 뒤에도 EXECUTION_UNAVAILABLE와 combat_effect_handler_missing 사유를 반환한다. 정책 카탈로그는 요구사항이며 실행 지원을 선언하지 않는다.

Unity Assets/_LASTRAIL/Scripts/Battle/ProducerCombatAdapter.cs의 확인 시53~57행은 carriageDamageEvents=UNSUPPORTED 및 equipment/research/extraPressure=OFF만 허용한다. 이는 현재 adapter의 제한이며 전체 저장소에 해당 계산 코드가 없다는 뜻이 아니다. 별도 구현된 최대HP/DEF/공속 계산 단위가 새실행 producer에 연결됐음을 증명하지 못한다.

따라서 현재 public 신규 START에서 최신 승인 솔로 한 판을 검증할 수 있다고 주장할 수 없다. 원인 우선순위는 실제 지원정의/handler·프로필 생산→Unity 현재계약 소비·상태 적용→실지원 근거 기반 START gate 연결이다. 상수 canExecute=true/409삭제·구CE03시험프로필 대체로 차단을 없애지 않는다. 정확 지원 버전·원효과/조건·미지원 거절·구START 원재생을 유지하면서 기존 S-01/S-02/U-CORE에서 구현한다.

첫 인수 증거는 실제 신규 출발의 성공 또는 정확한 미지원 거절, 생성된 원START/실프로필·정의hash·효과목록, 해당 Unity 적용 결과다. 그 뒤 같은 run의 사건/거래/정산까지 확장한다. 실제 호출·경제 변경·코드 수정은 이번 점검에서 수행하지 않았다.


<a id="combat-build-diversity-audit-20261011"></a>
## B 전투 빌드 다양성·성장 체감 감사 — 2026-10-11
요청: 기획된 전투 전반이 세피리아처럼 다양하고 강한 빌드를 제공할 수 있는지 확인. 이번은 읽기전용 기획 대조와 결과 기록이며 수치/정책 변경·구현 배정·운영 발행 승인이 아니다.

### 판정과 확인 범위
성장 배율과 서로 다른 역할의 재료는 있다. 다만 B 적격 효과 풀, 운용이 달라지는 완성 조합, 자연 획득 가능성, 실제 전투 체감이 함께 검증되지 않아 목표 달성을 선언할 수 없다. 캐릭터15인·증강40종·고유16종·인챈트16종·세트6종의 개수를 합쳐 빌드 수로 제시하지 않는다.
확인:05 증강 발급/리롤,09 단일 트리/노드/OC01,13 장비·세트·고유·반복 인챈트·최신 공속/가속/보관함,14 최종 드랍v15와 경로 시험 한계,17 전원 명단,30 전투 철학,31 v02~v04 관련 효과/합성 예산,34 플레이 검수,44 계수·B 전환/객차/요격/적웨이브/양거리/월드보스 관련 최신절,45 선로 관련절,46 B협동 방향,35 성장 관련부분,작성 JSON 해당 행,Server/Unity 관련 인계.
보존 원문 전체를 한 줄도 빠짐없이 전독하거나 모든 코드/실행 handler/실기/15인 패시브 전체를 재검증한 감사는 아니다. 개별 확인 범위에서 최신 승인이 구형 추천보다 우선한다. 구현 상태는 인계 근거와 일부 카탈로그/작성 데이터 확인이며 실제 플레이 미실행이다.

### 공식 비교 기준
[세피리아 공식 상점](https://store.steampowered.com/app/2436940/Sephiria/?l=koreana)은 무기별 고유 강화와 아티팩트·석판 배치를 주요 구조로 설명한다.
[TEAM HORAY 공식 1.0.31 공지](https://steamcommunity.com/app/2436940/allnews/?l=koreana)는 특수공격으로 특정 아티팩트를 즉시 발동하는 눈부신 침묵, 빙설덩굴의 공전/재사용 방식이 바뀌는 서릿발 화원, 먹구름 벼락에서 잔류벼락을 모아 방출하는 창운도 아르게스를 보여준다.
이 비교에서 채택한 분석 기준은 효과의 수가 아니라 원효과→다른 효과의 발동/형태 변경→성장 투자→강한 결과가 이어지는지다. 경쟁 게임 전체를 직접 플레이하거나 모든 최신 빌드 성능을 검증한 것은 아니다.

### 핵심 발견
| 우선 | 발견 | 실제 근거와 해석 |
|---|---|---|
| 1 | B 이행 후 죽은 효과를 먼저 정리해야 함 |44의 combat-b-turret-retirement-impact-v01-20261002/trait-adjacency-conflicts: TR04/X05/X13/X20은 포탑 의존, X06/X14는 같은 객차 다른요원 조건으로 B 1인 배치에서 무효. X15/X24의 요원 경로는 별도 유지. 대체안은 미승인.31의 Gun 전용 증강도 B 완료 풀로 세지 않음. |
| 1 | 일부 증강 합성이 화력 증폭보다 예산 재분배에 머묾 |31 v04 O10은 B를70/15/15로 분배. O08/O10/O15/O20 합성은 O15의 최대0.3B 추가 후 전체1.3B 안에서 분배하고 원기회3개 저장·방출. 다른궁극기 종료/방벽소비/저장 중 기회포기까지 있어 여러 카드의 독자 이득이 반드시 필요. 전술 가치가 없다는 뜻이나 모든 상한 제거 제안은 아님. |
| 1 | 평타/연속시전은 실제 연결 근거가 강한 사슬 |공속→EQ07(5회)/EQ12(8회)/EQ13(6회), EN02 적격 평타 추가피해, 액티브 성공 EQ11 공속. 스킬가속/EN08·EQ12/EN13→시전 반복. 마나 적격 사용자만 자원 효과 허용. 공속/ATK가 여러 이득을 동시에 얻어 다른 투자를 압도하는지 비교 필요. |
| 1 | 세트만으로 분대 빌드 다양성이 보장되지 않음 |13 SET01~06 대부분 능력치/효율 보너스. 개별 장착자의3슬롯만 계산하므로 팀 배치 시너지를 자동 제공하지 않음.09 OC01 A1B1C1은 세트 집중을 포기하는 선택이나 핵심 효과는 받는피해 감소. 폐기된 요원시너지/가상인원 복구 제안 아님. |
| 1 | 방어·지원 조합의 직관적인 연결 일부가 끊김 |HP 방벽/ATK 방벽/DEF 공격의 다른 성장축은 존재. 그러나 O02 초과치유 보호는 O15 소비 제외, EN16 파생치유는 EN15/O02 재발동 제외. 개인 보호가 객차까지 보호하지 않음. 요격 반사·요격 성공 공격강화는 기본 확정 능력이 아님. |
| 1 | 성장 숫자는 이미 크며 일괄 배율 상향이 첫 해법은 아님 |13 최신 ATK/HP/DEF/MANA_REGEN=10/25/60/150/350%, ASPD/SKILL_HASTE=5/12/30/70/150%. 같은 장착자 동일 ATK3슬롯만 비교하면1성3개1.3배,5성3개11.5배로 약8.85배 차이. 기본2초 공속150%3개는 다른효과/점유 없는1ms예시364ms. 전체DPS/시전속도 보증 아님. |
| 1 | 정해진3슬롯과 행동점유가 실제 고점의 핵심 제약 |ATK3개·공속3개·가속3개를 동시에 장착한 계산 금지. 고정 연사/센트리/예열·회수는 공속으로 자동 단축되지 않음. 요격은 기본공격 대기 정지와 별도 가속. 수치가 커도 실제 발사/시전이 늘지 않는 구간을 측정해야 함. |
| 1 | 장비 확보 확률은 완성 빌드 확률이 아님 |14 v15의 임의5성3개23%, 목표공속3개1.058%는 특정 경로/예산/합성 조건 모형. 원하는 태그·고유효과·인챈트 조합까지 완성할 확률은 아님.05 증강 최소횟수 없음/균등 후보 유지. 핵심 증강 없으면 정상 플레이 자체가 불가능한 빌드 금지. |
| 1 | 적 공급과 난이도가 성장 체감을 제한할 가능성 |44 첫 혼합 적은 대요원 돌격병/사수 중심, 동시8명/후속최대2명 제약. 고점 화력이 적을 지워도 스폰 대기만 길어지는지 확인. 적은 플레이어 순간 스탯 추종 스케일 금지이며 위험도와 깊이를 구분. 차체위협/구조물/일반보스는 별도 상황으로 비교. |
| 2 | 월드보스와 일반 최종보스는 빌드 시험 목적이 다름 |현행 WB 상세는 기본증강0/런장비·증강 이월없음/잡몹·처치없음이며 저HP/처치/다중적 효과가 작동하지 않는 범위 존재. 일반 런 완성 빌드의 보상전투는 일반 최종보스에서 평가. 월드 자체 역할 유효성은 별도 검수. |
| 1 | 승인 문서와 실행 가능한 카탈로그는 다름 |작성 equipment_enchant_draft.json에6성/옛EN02 첫공격·노드1회/ATK참조가 남고 publishable=false/runtimeValidated=false. 이를 현행 실행값으로 사용하면 안 됨. 이 파일만으로 모든 실행 handler 부재를 단정하지도 않음. Server/Unity 원profile·geometry·출격/전투 통합 잔여와 별도 비교. |

### 기존 근거가 있는 빌드 후보와 아직 없는 연결
| 시험 사슬 | 있는 재료 | 반드시 구별할 한계 |
|---|---|---|
| 평타 연사·적중 순환 |공속·EN02·EQ07/EQ12·SET02 |단일 기회/다단 적격, 점유, 실제3슬롯/마나 적격; 전역 최강 확정 아님 |
| 액티브 반복 시전 |스킬가속·EN08·EQ12/EN13·EQ11 |궁극기/고정연사와 별개, 자원과 점유가 병목 |
| 처치 연쇄·잔류 피해 |O03 초과피해 도약·EN14·EN13·O14 |단일보스/추가표적없음에서 이득 감소, 무한 재귀 없음 |
| 방어력 공격 |Anchor DEF28/56% 스킬·BCB06 |기본공격ATK, 지연면역 대상에서는 지연 관련 패시브 제외 |
| HP 방벽·치유 지원 |Kyle/Sable HP계수, Iris/Warden ATK계수, Sera/Marrow |B 수혜 범위 미결과 개인/객차HP 분리; 힐→방벽→탄환 전체순환 확정 아님 |
| 요격·짐칸 보호 |Kyle 자기칸/Sable 인접칸·BCB03·짐칸 배치 |공격 기회 비용, 원거리 짐칸 자동보호0, 반사/반격은 별도 미확정 |
| 집중 사격·단일 강적 |Vex 표적유지·Lumen 표식·EQ02/EQ09 |집중 시간/목표 변경/범위와 직접 피해원천, 보호/광역 포기 |
| 장치·지속 영역 |Eve 센트리·Nyx 영역·Cinder 연소 |B 외부 배치/실타격 영역 계약과 실제 효과 공급; 공속 자동전파0 |

위 표는 완성·승인된 빌드 패키지 목록이 아니라 기존 재료의 시험군이다. 기본 보관함은 최신30칸/장착품제외, DO05 최대54칸이며 과거24칸 계산을 현행 UX로 단정하지 않는다. AUTO는 궁극기 자동 사용, 일반스킬/평타/요격의 자동 실행과 구분한다.

### 보완 권고·검수 순서 (새 정책 미승인)
1. 먼저 B 적격 ID별 유지/대체/특수내부전전용/미지원·실제소비자를 확정한다. 포탑 관련카드/지휘노드의 미결 대체부터 마감한다. 카드 총량을 유지하려고 무효 카드 추가 금지.
2. 기존 재료에서 실제3슬롯·태그·인챈트·요원·객차·지휘 투자로 재현 가능한 대표 조합을 작성한다. 핵심 하나만 얻은 상태, 중간 조합, 완성형 및 대체 재료를 구분한다. 정해진 세트만 고르도록 추천UI/가중추첨/보장 획득을 새로 넣는 권고가 아니다.
3. 증강 복합은 단순 합산 총량뿐 아니라 바뀐 선택·실제 적중 표적수·처치 속도·안전성·발동 빈도로 평가한다. 효과가 다른 효과를 유효하게 강화하는 연결과 같은 사건 중복실행을 구분한다. 기술적 멱등/무한재귀 방지는 유지하고, 예산 재분배만으로 복합 성장이 사라지는 부분만 재설계 후보로 올린다.
4. 기존 IT-01/02에 동일 난이도/seed/편성·버전 비교를 연결한다. 원형/단일/2~3효과와 저·보통·고점 획득을 분리. 강제 완성 fixture는 효과 시험용이며 자연 획득률/재미 증거로 대체하지 않는다.
5. 관측: 단일적·무리 처치시간, 실제초당행동/요격에 잃은공격, 효과별 기여, 등반 전 처치율, 열차·객차·요원 손실, 보호막/치유 유지, 공백 스폰대기, 빌드 방향 성립 노드·완성 도달률·대체 선택·정리 시간. 실패 run을 표본에서 빼지 않는다. 강한 조합이 강해진 구간과 약점이 동시에 보이는지 사람 플레이로 확인한다.
6. 수치 조정/새 세트 기능/반사·무한연쇄/보장드랍/보스면역 변경은 이 감사로 확정하지 않는다. 본체재귀 오류/표시 누락과 의도된 고점을 구별한 뒤 정확 수정안만 승인받는다.

이번 상태: 기획 비교 완료(위 범위), 실제 재미·전체 밸런스 판정 NOT_RUN. 실행 큐에 새로운 미승인 재설계 작업을 등록하지 않았다.
\r\n
<a id="combat-build-improvement-v1-20261011"></a>
## B 빌드 개선 상세안 v1 — 2026-10-11 / 작성 완료·새 효과와 수치 검토안

### 1. 요청·승인 경계·선택한 방식
사용자 “좋아 개선 작업 ㄱㄱ”로 앞 감사의 개선 기획에 착수했다. 목표는 같은 공격력 배율만 올리는 것이 아니라 획득한 효과가 연결돼 공격 형태·대상 선택·지원 배치와 전투 결과가 달라지는 빌드다. 아래 신규 효과·수치는 이번에 처음 작성한 시험 제안이며 기존 출시/승인 카탈로그를 대체하거나 실행 허가로 쓰지 않는다.
비교한 접근: (A) 모든 피해/드랍 상향은 원인과 무관하고 성장 격차를 키움, (B) 기존 ID의 무효 조건과 연결 단절을 수정하는 방식 권고, (C) 신규 빌드 전용 재화·추첨/스킬 체계를 추가하는 방식은 범위와 획득 부담이 큼. 이번은 B를 선택한다.
기존 확정 유지: B 세로/객차당1요원·최대5배치칸, 포탑 삭제·특수 내부전 별도, 전원 양거리 수단, 요격의 행동 비용, AUTO는 궁극기 자동 사용만, 3장비슬롯·최고5성·동일 장비 스탯 합산·다른 계열 계산, 실제 개인 세트태그, 균등 적격 추첨/슬롯별3+1 리롤, 증강 획득 보장 없음, 진행 run START snapshot. 장비·드랍·회복·적 체력 일괄 상향 없음.
이번 산출물 범위는 핵심 4증강 합성, B 무효 지휘노드6개 및 혼합대상2개, 8대표 시험군과 인수 조건이다. 포탑 의존 증강7종의 신규 대체능력 전체·세트6종 전면 개편·15인 미공급 실제 profile까지 완료했다고 하지 않는다.

### 2. 빌드 공통 설계 계약
- 초반 핵심 장비/요원 능력 하나로 운용이 성립하고 증강은 강화/변형한다. 특정 증강 없음을 기본 전투 불가능 사유로 만들지 않는다.
- 하나의 요원은 실제3슬롯만 사용한다. 표의 장비 스탯 배분과 고유효과/세트/인챈트는 별개 추첨축이다. 원하는 모든 축을 동시에 가진 장비가 자연 드랍된다고 가정하지 않는다.
- 아래 빌드명은 내부 시험 분류다. 플레이어에게 빌드 추천/점수·확률 가중·맞춤 보장을 새로 노출하지 않는다.
- 핵심 효과 미획득 때는 일반 ATK/주력계수 장비로 진행 가능, 무효 자원/범위 효과를 0으로 지급하지 않는다. 적격 판단은 현재 지원 source와 원후보 규칙을 따른다.
- 노드/웨이브 종료, 다운, 대상 소멸, pause, 재접속·복원으로 준비·카운터가 무료 재발급되지 않는다. 승인된 기존 효과별 종료 경계가 우선이다.

### 3. 핵심 증강 O08/O10/O15/O20 개정안
이 절은 31#augment-numeric-budget-v04의 해당4행/해당합성만 대체하는 후보다. 다른 카드의 계수와 원본 공격·고유/인챈트 적격성은 그대로다.
B0는 현재 공격 기회에서 이4종 적용 전, 장비·일반 공격계수·적격 직접피해 modifier까지 적용된 방어전 피해다. 이4종을 다시 포함해 B0를 계산하지 않는다. 대상별 DEF·피해감소·보호막/HP는 기존 피해경로에서 각각1회 처리한다. '최종피해 비례 추가피해'인 인챈트는 그 원계약을 따르며 이 방어전 예산으로 재해석하지 않는다.

| ID | 변경안 | 의미·종료 |
|---|---|---|
| O08 | 본인 원궁극기 첫효과 확정부터8초, 본인 기본 공격 전체 예산 ×1.25. 기존 승인된 인물별 형태(60/40 또는70/30)를 적용. 다른 요원 궁극기는 이 태세를 끝내지 않음 | 개인별 태세1개·본인 재발동은 시간갱신. 분산할 추가 표적이 없으면 해당 몫은 원주대상에 합침. 지연탄이 발사 전에 잃은 표적까지 복원/환불하지 않음. 실제 형태 source가 없는 인물을 임의 패턴으로 채우지 않음 |
| O10 | 본체 예산100% 유지 + 추가 도탄2명 각각 본체 예산30% | 원기회당 도탄 경로1개. 기존 거리0.25L·원사거리/동일 영역 유지. 본체가 타격하기로 확정한 모든 대상 제외, 도탄 재방문0. 추가 적 없으면 도탄분만0; 본체를70%로 깎지 않음 |
| O15 | 기존처럼 적격 보호막 잔량20%를 소비하되 최대 소유자HP5%. 추가 예산 min(실제소비량,0.5×B0) | 기존0.3B 상한을0.5B 후보로 변경. O02 자체스킬 초과치유 유래 보호도 소비 가능으로 변경. 원공용/다른요원 보호 무단소비0, 잔해/복귀무적 제외 유지. 소비는 피해흡수가 아니며 피격/방벽파괴/만료회수 발동0 |
| O20 | 원래 정상 기회 최대3개 저장. 원궁극기 후딜 뒤 각 저장 기회를 방출할 때 ×1.25 집중 보너스 | 공짜 공격 생성0. 간격 min(250ms, 현재 정상 주기P), 공격/요격 점유 중 새 방출 시작0. 다운/웨이브 종료 폐기 유지. 마지막 방출 시작부터P와 원후딜이 모두 끝나야 새 정상 기회. 첫 방출은 궁극기 후딜 종료와 행동 적격을 모두 만족한 때 |

합성 순서: O15 실제소비를 원자 확정하여 C 산출 → D=(B0+C)×O08의1.25(활성일 때)×O20의1.25(저장기회 방출일 때) → D를 본체 형태에 배분 → O10은 같은 원기회 D에서0.3D씩 최대2회 추가. 마지막 단계에서 각 대상의 방어를 적용한다. O08 분할탄/도탄/O20 연출 프레임을 별도 원공격 기회로 세지 않는다.
O20 저장 시에는 기회ID/수량만 저장하며 피해·장비/태세는 실제 방출 시 유효 source로 계산한다. O15도 방출마다 실제 잔량을 소비한다. 저장만으로 방벽을 차감하거나 노드 넘어 저장한 태세를 유지하지 않는다. 지연 중 적법한 요격이 먼저 시작되면 이후 방출은 완료까지 지연; 진행 중 공격/스킬을 무조건 취소하지 않는 기존 행동 우선순위를 보존한다.
소비할 보호막은 소유자의 적격 layer 중 먼저 만료될 것→생성사건ID 순으로 차감, 기존 owner가 layer 순서를 명시했다면 그 순서를 재사용한다. 실제 차감합만 C의 입력. O02/EN15 등 파생 보호의 원천 종류는 보존한다. 이번 소비 허용 확장은 O02에만 해당하며 다른 파생 layer는 자동 허용하지 않는다.

#### 수치 예시와 안전 경계
B0=100, O15가 실제50을 소비할 수 있을 때:
- 원형100; O10만은100+30+30=160(서로 다른 적3명).
- O08+O10은125+37.5+37.5=200. O08 본체가2명으로 나뉘면 본체총125+도탄2명75이며 서로 다른 적4명이 필요.
- O08+O15+O10은187.5+56.25+56.25=300.
- 4종과 충분한 보호가 모두 있으면 저장기회당234.375+70.3125+70.3125=375, 저장3회 총1125. 단일 적이면3회 총703.125.
이는 방어 전 이론 예산이며, 저장3기회를 미리 포기했고 3번 실제 보호소비·합법 표적·태세·행동시간이 필요하다. 추가표적/보호부족이면 자동 감소. 이것을 무비용3.75배 지속DPS로 보고하지 않는다.
O10 분산은 적중횟수형 EQ07/EQ12/EQ13의 카운터를 추가 생성하지 않는다. EN02 등 직접공격 반응의 기존 적격 root hit 규칙을 변경하지 않는다. 도탄·파생추가피해는 자신/O03/EN14의 새 연쇄 출발점이 아니며 실제 킬 기록과 새로운 proc 적격은 구분한다. 이미 규정된 O03 내부2도약은 유지한다.
원사건 한 번의 파생이 중복 실행되지 않게 rootOpportunityId, effectId, effectRevision, branch/targetId로 소유 상태를 보존한다. 새 idempotency 검사기를 여러 계층에 복제하지 않는다.

### 4. B 무효 지휘 노드 대체 상세안
Lv50·레벨당1P·상위Lv25·부모OR·개방레벨·랭크/비용은09 현행 그대로다. 아래6개는 효과버전만 바꾸는 후보이며 그래프/무료포인트 추가 없음. 새명칭은 검토용이며 기존 확정 미술을 임의 재제작하지 않는다.

| ID | B 개정 효과 | 조건·정리 |
|---|---|---|
| TR04 구조물 대응 | 요원 직접 기본/자체스킬의 구조물 피해 +2/4/6/8% | 4랭크 원비용 유지. 적 열차/일반 적은 구조물로 자동 분류하지 않음. 선두 구조물 효과와 같은 가산군에 등록, 두 번 곱하지 않음 |
| X05 표적 유지 | 요원이 같은 적에 원기본공격3기회를 명중시킨 후 4번째부터 후속 기본 피해+6/12% | 스킬/도탄/분할탄은 별도기회 아님. 목표변경·4초 유효기본명중 없음·다운/퇴장 초기화. 순간 사거리 이탈만으로 즉시초기화하지 않음 |
| X06 교대 엄호 | 자체 액티브 성공 시 인접±1칸 다른 생존 요원1명에게 다음 정상기본 피해+8/16%,4초 | 발동자8초·버프1. 기존버프 없는 대상 우선→슬롯순, 전부보유면 갱신/늦은예약 없이 미발급. 자신/짐칸/다운 제외. 피해/무료기회 생성0 |
| X13 요격 반격 | 적격 탄 제거 성공 뒤 자기 다음 정상기본 피해+10/20%,4초 | 요원별8초·버프1, 실패/동일탄 중복0. 요격으로 정지한 공격 대기는 정상 복귀, 즉시무료공격0. 다중요격 성공을 여러 버프로 중첩하지 않음 |
| X14 구조 대열 | 요원 Down 원사건에 인접±1칸 생존 요원 각각 자신의 최대HP3/6% 보호막4초 | 원다운객차12초, 최대2명, 동종 큰 값 교체·작은값으로 수명연장0, 부활0. 의도적 다운 반복이 이득인지 검수 |
| X20 방벽 사격 | 해당 요원 객차의 적 직접공격을 현재 흡수할 수 있는 공용방벽 잔량>0 동안 기본 피해+6/12% | 개인 보호막은 제외. 구역형 방벽 범위 밖이면0. 공격 확정시 조건판정, 이후 날아가는 탄 소급취소0; 다음기회부터 소진반영 |

X15/X24는 요원 적용만 보존하고 객차 장착 포탑 가지를 제거하는 대상이다. 원 수치/발동/보상은 유지. 실제 정의에서 포탑 수가 분모/대상선정에 쓰인 곳은 단순 문자열삭제로 마감하지 않는다.
X06/X13/X22처럼 다음기본을 강화하는 서로 다른 지휘노드 modifier는 같은 '지휘 다음기본 가산군'으로 더하고 그 공격에1회 적용하는 안이다. 한 버프가 다른버프의 발동 사건이 되지 않는다. 피해 원스킬/장비/인챈트 곱연산 계열 전체를 이 가산군에 합치지 않는다.
X06/X14의 ±1칸 규칙은 일반 B외부전에만 적용한다. 특수 내부전은 실제 내부 지원 영역 source가 제공될 때 원본 명단과 매핑하며 외부 슬롯거리로 내부 공간을 가정하지 않는다.
의미 변경 프리셋은 구버전 snapshot 보존, 신규 B출발 전 명시 재편집이 필요하다. 추천은 영향 프리셋 무료초기화 선택과 실제투자P 전액복구이며 운영 지급/실초기화는 별도 이행 검증 후다. 고아자손을 조용히 지우거나 상위무료선택을 복구하지 않는다.

### 5. 포탑 증강·세트 정리 범위
T05/T10/T11/T13/T16/T18/T20은 현행 Gun 실행자를 B요원/이브센트리로 문자열치환하지 않는다. 아래는 다음 설계 순서이며 이번4증강 상세와 같은 완성 상태가 아니다.
- T11: 기존 요격 능력 보유자 강화 방향. 무능력자에게 자동 요격 제공은 제외.
- T16: 요원 궁극기와 지휘지원의 역할 연계. X06/X13의 단순 복제 배제.
- T13/T18: 차체 수리/복구와 전투 복귀 운용. 요원 HP회복·부활과 분리.
- T05/T10/T20: 후미 추격 대응·경로 설치물·일시 외부 전개. 실제 source/행동 소유자가 없는 상태에서 지급하지 않는다.
새 B 추첨 pool은 이7개뿐 아니라 모든 카드의 실제 원공격자/거리/모드/능력 적격 매핑을 확인한 버전으로 발행한다. 미지원 카드를 빼서 변한 분모·단일카드 노출률·리롤과 완성률은 재계산한다. 없는 무효카드로 40개 수만 유지하거나 인기효과에 숨은 가중치를 넣지 않는다.
SET01~06의 기존1/2/3개 누적규칙과 원수치는 이번에 바꾸지 않는다. 세트6종 전부 신규 proc로 바꾸면 원인을 분리하기 어렵다. 우선 속사/축전 세트가 평타·액티브 연결을 얼마나 돕는지, 철벽/정비가 방어·지원 선택의 기여를 제공하는지 시험한다. O02→O15 연결과 지휘개편 후에도 약하면 특정3세트 효과만 별도 제안한다. 별도 요원시너지/가상태그 부활0.

### 6. 실제3슬롯 대표 빌드 시험 설계
아래 스탯은 실제3칸 배분이다. 고유효과와 인챈트는 장비별1개/원규칙, 같은인챈트 최고값1개 적용. '핵심→중간→고점'은 획득 보장이 아니라 실제 있는 재료에 따라 진행할 분기다. 시작50P 완성트리를 강제하지 않으며 시험레벨에서 실제 비용·부모를 만족한 노드만 사용한다.

| 빌드/기존 대표 | 3슬롯 스탯 예 | 핵심→중간→고점 | 결핍시 대안·약점·조작 |
|---|---|---|---|
| 연사 순환 / 기본공격 기여가 큰 요원 | ASPD/ASPD/ATK | 빠른기본→EQ12 또는 EN02→O08+O10 | 증강없어도 평타형. EQ12는 실제스킬 사용자만. 단일적에서는 도탄가치0, 요격/시전점유와 충돌. 집중대상으로 우선위험 적 제거 |
| 액티브 순환 / 실제마나 액티브 보유자 | SKILL_HASTE/ATK/MANA_REGEN | 주력스킬→EN08 또는 EQ13→EQ12+EQ11 | 마나미사용자는 회복칸을 ATK 또는 스킬의 HP/DEF로 대체. SET06은 실사용자만. 시전시간/마나 병목과 궁극기AUTO 별도 검수 |
| 처치 연쇄 / 직접공격 궁극기 사용자 | ATK/ATK/SKILL_HASTE | 직접공격→EN14 또는 EN13→O03+O14 | 단일 보스는 ATK 직접공격으로 대응, 보스용 추가잡몹 강제0. 약한적을 기점으로 궁극기 사용, 연쇄 파생이 새연쇄를 부르지 않음 |
| 방어력 전투 / Anchor | DEF/DEF/ATK | DEF계수 스킬→지속생존→가속장비로1칸 교체 비교 | 평타는 ATK이므로 DEF3칸 전행동강화 아님. 지연면역 보스에 미발동패시브 포함금지. 버티며 기술 발동하는 구성 |
| 방벽 탄환 / Kyle 또는 Sable+자체스킬 힐러 | HP/HP/ASPD | 자체방벽→O15→치유지원+O02+O08 | 보호가 없으면 기본공격유지. O02 생성자는 원치유, 수혜자는 공격요원으로 실제 지원범위 만족 필요. 공격을 위해 보호를 소모해 피격위험 증가. 소비/흡수·HP를 분리 측정 |
| 요격 반격·짐칸 호위 / Sable·Kyle | HP/DEF/ASPD | 자체요격→BCB03→X13+X06 | 요격불가탄/범위밖은 막지 못함. Sable 인접 보호로 짐칸 위치를 선택; 요격 인원 증가만큼 화력손실 측정. 반사는 추가하지 않음 |
| 단일 집중 폭딜 / Vex 또는 Lumen | ATK/ATK/ASPD | 직접집중→EQ02 또는 EQ09→X05+O20 | 목표변경/장시간 공백에 약함. O20 없으면 지속집중형. 표적 유지와 위험적 전환 사이 선택. 기존 요원 고유 집중효과와 중복기여 표시 |
| 장치·잔류 제압 / Eve·Nyx·Cinder 중 실제지원자 | 주력 스킬계수/주력 스킬계수/SKILL_HASTE | 원장치/영역→원지속효과 강화→O14 등 적격 추가영역 | 주력계수는 승인된 본인 profile의 ATK/HP/DEF를 사용. 공속을 센트리 발사간격으로 자동전파0. 이동적/범위이탈에 약하며 선로·속도로 체류 변화 관측 |

고유 ID·스탯·태그를 동시에 만족한 장비가 카탈로그에 실제 존재하는지 구현 인수 시 검증한다. 단일영역 보스용 비교군과 무리전 비교군을 분리하고, 시험군간 복수 요원 편성으로 섞을 수 있지만 같은 개인3슬롯을 초과해 설명하지 않는다.

### 7. 실행 상태·데이터·표현 계약
Model/도메인 소유: 개인별 태세·저장기회·소비 layer·후속기본 버프·노드 카운터. Controller는 기존 명령으로 궁극기/집중대상/선로/속도를 전달. View는 준비/발동/소멸과 실제피해를 표시, 규칙/저장/서버판정을 소유하지 않는다.
동일 원효과는 기존 피해/자원/상태 owner에 연결하고 O08전용 별도 피해 시뮬레이터를 만들지 않는다. 일반solo/협동host/WB의 기존 권위를 변경하지 않는다. Server는 실제지원 definition/version/source·적격풀·출발 snapshot/검증계약을 소유하고 Unity는 승인 모드의 원실행 owner에서 소비한다.
definition에 원ID+effectRevision+지원mode+원공격종류+대상/거리source+원기회/파생적격+수치단위+종료/저장상태를 구분한다. 기존 필드가 같은 의미면 재사용. 콘텐츠 새 버전 누락을 기본값0/승인으로 보정하지 않는다.
원프로필을 공급할 수 없는 카드는 미지원, 후보효과는 draft. 옛6성/첫공격1회 작성 JSON을 현행으로 발행하지 않는다. 실제 지원·저장복원·비화면 소비 검증 전 publishable/runtimeValidated 승격0.
시각 관측: O10은 본체/도탄을 구분, O20은 저장 수와 방출을 확인 가능, O15는 소비한 보호가 줄고 공격이 강화되는 시점 표현, X13은 성공한 요격 후 반격준비. 텍스트·게이지를 그림에 굽지 않음. 전체 최종시안/독립자산 후 화면연결 조건 유지. 신규 연출은 독립 리소스 검수 후이며 이 문서가 미술 최종승인은 아니다.

### 8. 비교 검증·완료 기준
1. 정적 참조: 위6노드/2혼합ID/4증강의 현행효과·구snapshot·부모·비용을 대조하고 유효실행 profile의 지원표를 만든다. 실행 원본이 없는 경우 증거와 차단범위를 명시.
2. 기술 사례: 원기회1→원피해/파생의 정확횟수, 다단/도탄의 카운터중복0, 대상0/1/다수, 보호부족/다수layer, O08 동료궁극기, O20 P<250ms/요격경합, 다운·웨이브끝·pause·복원·중복수신, 같은버전 재현.
3. 효과 비교: 동일 seed/난이도/요원레벨/장비3칸 예산에서 원형→단일효과→2효과→완성군. 고정완성 fixture는 효과 검증, 자연획득 run은 성립률 검증으로 분리.
4. 전투 상황: 단일강적, 작은무리/다수약적, 등반, 투사체압박, 구조물, 일반최종보스. 아직 미구현 상황은 NOT_RUN으로 남기고 임의위협 source 생성0.
5. 측정: 실제행동/피해원천별기여, TTK/동시처치, 공격포기/요격성공, 보호소비·실흡수·실HP손실, 대상없는대기, 적공급 공백, 성립노드/핵심결핍·대체선택·실패율·정리시간.
6. 자연획득은 기존 드랍 입력에 B적격분모/장비3축/경로/상점예산/합성/보관30~54를 실제 반영, 기본공급/추가공급 분리. 실패run 포함. 5성3개 확률을 이 표8빌드 완성률로 대체하지 않음.
7. 재미 수용: 핵심1개일 때 기존보다 무엇이 달라졌는지 영상/입력기록으로 식별되고, 2개 이상 조합이 단순 장비수치 증가와 다른 유효 기여를 보여야 한다. 강점상황과 약점상황을 각각 입증한다. 여러번 골랐는데 이득이 없거나 새로운 비용만 늘면 재설계. 특정빌드가 모든상황을 지배하면 전역장비너프보다 원조합 원인을 먼저 조사.
8. 위 수치가 방어전 산술과 맞는 것은 밸런스/실플레이 PASS가 아니다. 실제효과·자연성립·표현의 증거가 모여야 완료한다. 현재 실전 검증 NOT_RUN.

### 9. 후속 경계
Plan: 이 시험효과/계수 사용자 검토 → 09/31 현행 버전으로 승격 및 실제 JSON 원본/ID 대조, 7포탑카드 대체와15인 profile 지원 잔여를 별도 범위로 마감.
Server: 승인된 정확버전의 정의·적격풀·snapshot/거절 계약과 원소유자 검증을 준비. Unity 실제효과 전체완료를 정의공급의 순환선행으로 삼지 않는다.
Unity: 공급된 계약과 승인 effect spec에서 기존 실행 owner로 비화면 구현·중복/복원/행동 검증. 공급 안 된 effect를 fake지원으로 만들지 않는다.
Direction: 최종 effect/producer를 인수한 뒤 기존 확정 화풍/캐릭터/세로구도를 보존하며 준비·발동·종료 표현을 독립 제작. 기술검증과 전체미술승인을 구분한다.
실행 큐는 선행 준비가 확인된 단위만 등록하며 미확정 새수치를 먼저 구현시키지 않는다. 기존 게임 개발/다른 B작업을 이 개선안 승인대기로 일괄 중단하지 않는다.
\r\n
<a id="combat-build-improvement-approved-v1-20261011"></a>

## B 빌드 개선 v1 승인 반영 — 2026-10-11
사용자 “승인 다음 ㄱㄱ”는 직전 제시한 #combat-build-improvement-v1-20261011 상세안과 시험 수치의 승인이다. 해당 절 §3의 O08/O10/O15/O20 및 합성, §4의 TR04/X05/X06/X13/X14/X20·X15/X24, 시험군/상태/검수·프리셋 이행 설계를 첫 시험 기준으로 채택한다. 해당 범위의 '검토안/미승인' 표시는 작성 당시 이력이며 이 승인으로 대체한다. 후속7포탑카드 신규 효과, 미공급15인 profile, 세트 전면개편·새 운영환불/배포까지 승인한 것이 아니다.
정책버전 combat.b-build-improvement.v1.20261011. 원승인 09/31 연결 절에서 이 버전을 참조한다. JSON/실행 정의·handler·소비는 아직 미반영, 실제 출격/시각/자연완성률/게임성 NOT_RUN. 실제 운영 초기화·환급은 수행하지 않음.
§3 예산100/160/200/300/375 및 저장3기회1125·단일703.125 산술 검산은 유지; 실제 보호차감/대상수/시간비용 전제 생략 금지.


<a id="combat-build-loadouts-validation-v1-20261011"></a>

## 실제3슬롯 대표 빌드·성장 비교 명세 — 2026-10-11
앞선 개선 v1과7증강 승인 후 진행하는 시험 조합 상세화. 새 캐릭터/장비효과/보장드랍/수치정책은 추가하지 않는다. 승인된 재료로 무엇을 비교할지 정한 명세이며 실제 장비 생성 가능성·profile지원·전투 실행이 모두 검증됐다는 뜻은 아니다.

### 1. 시험에 실제 넣는 장비와 단계
L/M/H는 모두3개 장착, 성급을 각각1/3/5로 고정하는 비교 fixture다. 실플레이 초중후반에 그 성급3개 획득을 보장한다는 뜻이 아니다. 장비ATK/HP/DEF/MANA_REGEN의 해당값10/60/350%, 공속/가속5/30/150%를 사용한다.
시험A는 기본스탯만, 시험B는 적격고유/세트/인챈트를 각기 별도로, 시험C는 증강과지휘를 추가한다. 모든효과를한꺼번에넣고원인분석하지않는다. 고유효과는3성부터생성되는원규칙/합성 이력,3성장비 총I/4성II/5성IV를 지킨다. 예를들어5성3개라도고유효과합계XII이내에서실재합성이허용한분배만, 16효과가각XII인허구장비금지. 인챈트는장비당1개·동종최고1개·시술성급고정. 세트는실제각장비단일태그,타요원/보관품기여0.
테스트가요구한고유/태그/스탯 조합이원카탈로그에서가능한지검사후 fixture제작;불가능하면유사효과를꾸며넣지말고시험불가로표기하고합법적대체를쓴다. 작성JSON은runtime데이터로발행하지않는다.

### 2. 기본공격 장비배분 산술
기본공격이ATK비례이고공속외행동점유/자원/효과/방어변경이없다는비교조건. 무장비평타를1로둔 이상적인지속피해=(1+ATK합)×(1+ASPD합). 실제DPS에는논리tick/초탄·후딜·스킬/요격점유·이동/표적부재가포함된다.

| 3슬롯 배분 | L:1성3개 | M:3성3개 | H:5성3개 | H에서 기본2초의 주기 |
|---|---:|---:|---:|---:|
| ATK/ATK/ATK |1.30|2.80|11.50|2000ms|
| ATK/ATK/ASPD |1.26|2.86|20.00|800ms|
| ATK/ASPD/ASPD |1.21|2.56|18.00|500ms|
| ASPD/ASPD/ASPD |1.15|1.90|5.50|364ms(1ms단위올림)|

이 표는 세트/증강/고유·인챈트 이전값이며 실행모드의 tick이1ms라고 보장하지 않는다. 공속만3개를최강이라고하지않고 높은기본계수·고정주기스킬·적중횟수순환·공격기회비용을 함께본다.
H의 ATK2+ASPD 대비 ATK1+ASPD2는순수평타10%낮지만공격기회빈도60%높음. 실제EQ12/EQ13/고유스킬로마나·쿨타임회수가이차이를보상하는지측정한다. 공속3개는5.5배와20배차이를메울실효과가필요. ATK3개는평타11.5배이나공속의영향을받지않는ATK계수스킬/치유/방벽에는강점이남는다. 낮은성급에서ATK3개우세→높은성급에서혼합우세라는전환도확인대상.
장비가속만으로기본CD12초를계산하면H의가속1/2/3개는4.8/3/약2.1818초(1ms올림2182ms). 이는시전/마나/별도SET03·EN08전이며3가속장비와3ATK장비를동시가정하지않는다.

### 3. 8개 시험카드
각 행은실제3개스탯을지정하고, 고유/인챈트는별도시험추가목록이다. 단계별획득확률/편성제공보장이아니다. 특정이름요원이라도실제모드/형태source없으면지원확정0.

| ID/역할 | 3슬롯 기준 / 대조군 | 핵심→중간→완성 비교 | 실제 장비 효과 후보 / 약점 |
|---|---|---|---|
| BLD01 연사 순환 | ATK/ASPD/ASPD vs ATK/ATK/ASPD | 평타→EQ12→O08+O10 | EQ07·EQ11·EN02는각각추가분리. SET02대조. 적중카운터는원공격기회당,도탄3명으로3회적립0. 적은적수/행동점유에약함 |
| BLD02 액티브 순환 | ATK/SKILL_HASTE/MANA_REGEN vs ATK/ATK/SKILL_HASTE | 실제마나자동스킬→EN08→EQ12/EQ13과EQ11연결 | EQ05/SET03/SET06각각비교. 마나미사용자는회복장비효과0이므로회복칸을주력스킬HP/DEF/ATK로교체. 마나병목해소와CD병목해소구분 |
| BLD03 처치 연쇄 | ATK/ATK/SKILL_HASTE vs ATK/ATK/ASPD | 직접공격→EN14→O03+O14 | EQ03의저HP조건/직접처치검증. EN13은그원계약처치만. 피해파생킬을새직접처치로승격0. 추가적없는보스에서ATK기본기만으로의존유지 |
| BLD04 DEF 전투 | DEF/DEF/ATK vs DEF/ATK/SKILL_HASTE | Anchor DEF스킬→BCB06→EQ05·철벽세트비교 | 기본공격ATK는DEF로증폭안됨. 기본피해·DEF스킬·방어기여따로표시,보스제어면역패시브0. DEF고점이공격/생존동시최강인지검수 |
| BLD05 방벽 탄환 | HP/HP/ASPD vs HP/ATK/ASPD | Kyle/Sable원방벽→O15→O02지원+O08 | EQ16/EN15/SET04각각대조. 자체스킬과파생방벽적격을정확매핑. O15소비는원B0의0.5상한이라HP만높여피해무한증가0. 지속피격/힐부족/ATK부족에약함 |
| BLD06 요격 호위 | HP/DEF/ASPD vs HP/DEF/HP | 기본요격→X13→T11+공용방벽공급 | BCB03가속별도. Sable인접범위/짐칸위치비교,요격성공/절약피해/잃은공격기회측정. 공속이요격시간을자동단축하지않음. 빔/범위밖/단발에광역요격이득0 |
| BLD07 단일 집중 | ATK/ATK/ASPD vs ATK/ATK/ATK | Vex/Lumen실능력→EQ02또는EQ09→X05+O20 | 같은표적유지형과긴궁극기딜러분리. 3기회저장전피해공백까지포함. 작은적교체/짧은전투/웨이브종료시저장손실 |
| BLD08 지속 구역 | ATK/ATK/SKILL_HASTE(ATK계수사용자) vs ATK/SKILL_HASTE/SKILL_HASTE | Eve/Nyx/Cinder원장치→O14→T20/T10 지휘층 추가 | EQ05의실적격자체액티브만,ASPD센트리전파0. 지휘Bs와요원ATK는별도이므로요원장비로T20강화0. 고정구역유도/체류시간과즉시처치손해비교 |

완성에예시재료가다필요하다는뜻은아니다. 최소성립은기존능력+장비방향, 중간엔연계1개, 고점은실제획득한지원증강을추가한다. BLD05는보호부족시O15추가피해0이어도기본공격유지, BLD06은요격불가시정상공격, BLD08은T20없으면원지휘지원으로운용.
인챈트계수는13 최신승인표의시술성급을읽고표의옛6성계수를5성에복사하지않는다. 각효과의대상이자동액티브/궁극기/기본/지휘중무엇인지실source로분리. AUTO끄기는궁극기시전결정만변경,기본/요격이꺼진다고시험하지않는다.

### 4. 지휘·편성 비교의 공정성
지휘Lv/전체투자P/요원성장/객차개조랭크를군간동일하게고정하고실제부모OR·개방·원비용검증. 요격빌드에는공짜X13,집중빌드에는공짜X05를붙이지않는다. 지원노드에든포인트는다른군도동일총량으로합법적배분한대조군사용. 전체50P를예시로가정할때도원JSON이B동기화되기전확정프리셋파일을꾸며발행하지않음.
기본은같은요원/같은적에서장비·효과한축만바꾼쌍비교. 서로다른요원군은캐릭터고유계수차이때문에ATK숫자만맞춰밸런스동일하다고말하지않는다.
짐칸1칸을더넣은편성과5요원편성은별도편성시험: 생존·클리어·보상원계약을함께보되이번문서에서짐칸보상률추가/재설계0. 피해흡수와예방은객차/요원/열차대상을따로분류하고객차전파를다시예방피해로중복합산하지않는다.

### 5. 시행 순서·인수 산출물
- 정적 단계: 카드별 실제3개장비·합성이력·태그·시술성급/고유등급과원정의hash/요원source/비용그래프를검증. unsupported source는정확ID로실패/미실행기록. 임의HP/DEF·캐릭터기본치를발명하지않음.
- 첫 실행은BLD01/BLD05/BLD06 세대표만 실제지원되는범위에서. 각각공격복합,방벽소비,방어행동기회라는상이한위험을우선확인. 그다음나머지5군으로확장,독립미구현상황때문에이미가능한군을전부막지않음.
- 단일강적/약한적무리→투사체/등반→구조물/일반최종보스 순. 동일seed·난이도·원공급·단계시간. 원형→단일→복합, 장비단계는L/M/H. 지원없는상황은NOT_RUN.
- 표본은run/seed/definition·mapversion/편성·3장비/투자P/입력·효과event·실시간·승패·종료원인으로식별. 관측은피해/회복/보호소비/실흡수/요격·실제행동·대기·손실/처치시간. 파생별root를추적하고최종HP절삭전피해와실HP손실을둘다남김.
- 수용은수치목표를새로못박지않고장점과대가를원대조로입증:BLD01은10%평타손실대신실순환기여,BLD05는실방벽소모와유효피해상승,BLD06은화력손실대신실탄제거·피해예방. 모든군/모드최고이거나누적선택이계속손해면그원연결을수정안으로돌림.
- 자연획득검증은효과검증과별도. 현재B적격pool과T12/T20배타·3+1리롤·실경로/상인예산·보관30~54·2개합성/태그/고유/인챈트원정책을모델입력으로먼저인수. 빌드성립률을이전5성3개23%와동일시하지않음. 실패run포함,대체조합성립/포기/정리시간까지기록. 아직추첨source미공급이므로이번수치추정0.
- 결과각각 DESIGN_READY/DEFINITION_SUPPORTED/DOMAIN_VERIFIED/INTEGRATED/PLAY_REVIEWED로증거구분,하나의종합PASS로승격0. 운영배포/실보상과분리.

현재: 기본스탯4배분×3단계의12개이상적피해값과주기,12초기본CD의3가속예시산술확인. fixture생성·전투실행·자연추첨시뮬레이션미수행. 실제피해계수/적정치/재미최종판정은미완료. 다음미결은공급된B전체효과/장비카탈로그와8시험카드의실매핑, 자연성립률입력및실테스트다.


<a id="combat-build-source-readiness-audit-20261011"></a>

## 8빌드 시험명세 승인·실소스 대조 — 2026-10-11
사용자 “승인 다음 ㄱㄱ”로 #combat-build-loadouts-validation-v1-20261011 시험명세를채택하고관련실소스를읽기전용대조했다. 새게임수치변경0. 실제테스트/fixture제작/운영출격 미실행.
범위 S=D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server, U=D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts. S/apps/game-api/src 및 S/packages의관련ID/버전탐색과아래파일관련절, U/Battle관련owner/adapter 확인. 모든repo/전체handler전수감사아님. 최초S/src는없는경로여서조회실패했고apps/game-api/src로정정했다. 이름검색미발견만으로전체미구현단정0.

| 원소스·관측 | 의미 | 필요한후속 |
|---|---|---|
| S/apps/game-api/src/equipment-run-stat-owner.ts:12/16~23 category HP/ATK/DEF,1~5성 bps 및 SET01/SET04 정적계산 | 기본스탯 계산 재사용가능. ASPD/HASTE/MANA_REGEN의실장착소비는이경로미지원 | 기존S-01/S-02에서카테고리·원주기/마나소유연결 |
| 동일:62 uniqueEffects.length 또는 enchantment 있으면 effect_handler_unavailable,63 category_unsupported | 보유/합성영수증과실효과실행은다름. 고유·인챈트포함완성장비를그대로인수못함 | 원효과별지원handler/계수/카운터와재진입·복원검증;가드삭제로통과금지 |
| S/docs/api/equipment-run-stat-owner-v1.md 지원범위 및추가절 | 원수치산술1~5성,실source1~2성/고유인챈트제외. equipped merge는후속구현으로앞미완료문구갱신 | 합성기반전체를재구현하지말고실효과경계만확장. 문서수치지원과실장착효과지원분리 |
| S/apps/game-api/src/combat-b-named-effective-source.ts actors effectiveStats=null, modifierApplications=0, runtimeSupported=false/canStart=false | 카일·세이블의원소유/영구스탯자료가최종run효과적용완료는아님 | actual run equipped items·지휘효과·runtime effective owner와결속 |
| U/Battle/BNamedEffectiveSource.cs:28/46 false/null/0을명시검증, BNamedEffectiveSourceOwner.cs는자료수명/콜백소유 | Unity가해당미지원자료를정확소비하는것과플레이스탯적용은다름 | Server정확새버전후새지원경로인수;구버전검사를조용히완화하지않음 |
| U/Battle/BNamedOperatorNativeAdapter.cs 관련영구스탯경로 activeBound/ultimateBound/effectiveStatProfileBound/fullActorSupported=false, 장비효과배열빈값요구 | 이특정adapter경로는영구기본공격/요격부분범위 | 다른경로지원여부를소유자가대조후실효과통합,모든Unity코드미구현이라는단정금지 |
| U/Battle/CombatEquipmentLoadout.cs 3슬롯/실태그/OC01 owner검사 | 실3슬롯·실태그 원검사는존재 | 이를재사용,가상태그/신장비슬롯신설0 |
| S/apps/game-api/src/combat-policy-catalog.ts status REQUIRED_NOT_EXECUTABLE/구5인·구수치 | 기획요구목록이며최신실행카탈로그아님 | 새정의공급전옛표를현행build source로발행금지 |

탐색한Server apps/game-api/src/packages TS와Unity Scripts CS에서이번개선버전명과O20/EQ12의직접문자열지원은확인못했다. 데이터외부화/다른source경로가능성이있으므로Server/Unity소유자의실handler매핑이최종인수근거다. 코드참조만확인했으며이번에unit PASS/실HTTP/Unity플레이를수행했다고하지않는다.

### 대표군의 현재 판정
BLD01/02: 공속·가속/마나와고유·인챈트연결필요. BLD03: 직접스킬·처치source와파생handler필요. BLD04: DEF산술기반은있지만Anchor실스킬/최종profile 인수필요. BLD05: HP산술기반은있지만원방벽/힐/소비효과/최종profile필요. BLD06: 기존요격/능력source 일부재사용가능하나T11공용방벽/2탄·X13후속/실장비owner필요. BLD07: 집중/저장공격·요원profile필요. BLD08: 원장치/지속영역/지원사격source와새handler필요.
전체시험가능으로인수한완성빌드는이번점검에서0/8. 8종의모든기능이없는것이아니라종단연결완료근거가없다는판정이다. 전체미술을기다리지않고기본산술/원기회·상태단위검증은독립진행한다.

### 우선 인수 순서
1. 기존원장비/3슬롯/태그/합성·HP·ATK·DEF 기반재사용, ASPD/HASTE/MANA원owner와적격장비source지원.
2. BLD01에필요한EQ12/EN02·원스킬/기회와승인O08/O10을첫수직단위로연결. 마나사용증거없는요원은EQ13시험제외. BLD05방벽·BLD06요격은각원지원단위별병행가능하나서로를전체선행으로삼지않음.
3. 실제정의/version·원소스/거절·복원증거를공급한부분만Unity비화면소비등록. 클라이언트자료reader완료를실runtime계산완료로오인금지.
4. 이후8군으로확장,자연완성률은실적격pool/장비생성조건확정후. 새문서/카탈로그객체만늘리고handler공백을남기는완료처리금지.
기존B-BUILD-IMPROVE-V1 및S-01/S-02에입력보완하며중복부모큐생성0. 새권한/배포/실보상·장비지급0.


<a id="combat-experience-eight-review-20261011"></a>

## 빌드·플레이 경험 8항목 순차 검토 — 2026-10-11
사용자 요청:8~9단계에빌드거의완성,매번같은선택을피하는로그라이크다양성,나머지6항목순차검토. 이번은설계검토및수용기준정리이며실전플레이/새확률시뮬레이션미실행. 05/09/13/14/35/44 관련절과본34의개선안·실코드대조결과를이용했다. 원문전체재독/15인전수실행감사아님. 새로운출력계수·보장드랍·자동화버튼·적스케일은이번검토로추가하지않는다.

### 1. 빌드 완성 시점 — 방향 채택, 현재 충족은 미확인
사용자 8~9단계 제시를빌드가거의완성되는설계목표로채택한다. 기준시점은8/9번방문노드의보상·정비를마치고다음노드로출발할때, 최종12보스이전10/11과보스에서운용할수있어야한다. 8완성은9~12 최대4방문,9완성은10~12 최대3방문이남으나비전투사건도있으므로3~4전투를보장하지않는다.
'거의완성'은최고성급/동일카테고리5성3개/정해진증강전부가아니다. 실제조건:①주력요원또는분대의반복운용방향이있고②성장투자와그방향의효과가최소한하나유효하게연결되며③현재능력/사거리/마나/행동점유에맞아작동하고④마지막핵심카드를얻기전에도남은전투에대응가능. 여러요원모두완성은기준으로강제하지않는다.
추천단계목표:1~3현재재료로방향탐색,4~6첫연결체감,7~9핵심조합작동,10~12고점활용/미세보완. 이구간별역할은검수프레임이며노드사건/성급고정배치가아니다.
현05는증강0회가능/최소횟수없음,14는최고5성직접드랍0/후반성급상승. 따라서O08+O10 등특정2증강조합을8~9층모든run의완성정의로쓰면현승인과충돌한다. 장비·고유·인챈트·기존요원/객차능력으로성립하는대체경로를포함하고,태그/인챈트까지완벽한고점은희귀하게남긴다.
필요측정:8/9단계도달률,그시점핵심성립률,전체시작run중해당시점성립률,완성후남은전투수/유효전투초,완성되기전실패이유. 도달자만분모로써초반탈락을숨기지않음. 기존12노드끝의5성확보23%는8~9층완성률아님.
개선순서추천:현source로층별획득/실가능조합먼저측정→무효/기능중복/대체안없는병목효과정리→부족하면후반10~11에치우친품질을7~9로앞당기는분포조정안별도비교. 수량증가·증강확정노드/천장·숨은드랍보정은자동도입하지않는다. 완성후일반전이없어보스한번뿐인경로는별도검토,무조건전투노드삽입으로현그래프변경0.

### 2. 선택 다양성 — 사용자의 반복고정선택 반대 기준 채택
같은상황에서도한카드가항상우세하면실질선택이없다. 균등추첨만으로이를해결하지못한다. 다만모든카드전체픽률을같게만드는것도목표가아니다. 요원/현재장비/이미보유한카드/남은지도/생존압박에따라서로다른선택이유가생기는것이목표.
개선안에서주의할범용후보는O08개인태세×1.25,O10본체유지추가도탄. 강점은명확해졌지만둘이거의모든평타형에서자동정답이되는지검수필수. O10은단일적에서추가가치0, O08은실궁극기빈도/태세가동시간에따라이득이다. 이를실적구성에서구분하지않으면문서상약점만남는다. T11은실요격/공용방벽,T13은수리/속도운용 등조건부가치를실제로보상해야한다.
현T12/T20배타는동시무효방지용,선호빌드를막기위한추가배타/보유보정확률은만들지않는다. 특정선택을했으니그계열카드를더주는숨은가중치없음.
비교기록은'노출된적격3장중선택률'을분모로하고보유/요원/사건깊이/위험도/이전선택/플레이어숙련을나눈다. 리롤소모/포기/못사용한선택,완료율·소요/피격을함께본다. 전체획득빈도높음만으로너프금지. 같은seed에매번같은루트가이기는원인이사건가치인지카드범용성인지분리.
조정우선순위:실제로무효인카드제거·명확설명→조건부카드의독자보상/사용조건개선→범용카드특정과도한합성만수정검토. 신규픽률상한/교대강제/전체일괄너프0. 실제픽률자료가없으므로현재항상정답이라고확정하지않음.

### 3. 빌드 간 격차 — 연사 우세 위험과 방어/지원의 조건부 효율
34 실제3슬롯산술상H단순평타 ATK2+ASPD1=20배,ATK1+ASPD2=18배. 후자는공격기회빈도60%높아EQ12/EQ13/궁극기충전·적중효과를동시에얻는다. 고정피해를추가하는효과와원최종피해비례EN02를동일횟수이득으로중복계산하지않는다.
DEF형은DEF계수스킬과방어동시이득가능하나기본공격ATK,원공격/행동시간확인이필수. 방벽형은HP만늘려도O15의0.5B0상한에막힐수있어혼합투자비교. 요격은예방피해와공격기회손실,지원은치유결손/실흡수/다운감소·아군공격시간회복,지속영역은적체류/실명중을평가한다. 공격DPS하나로8군서열을만들지않음.
동일레벨/총지휘P/3장비성급재료예산/개조랭크에서①같은요원효과만변경②역할이다른실편성교체를분리. 일반무리/단일강적/투사체/등반/차체위협/구조물의다른환경에강점이있는지확인. 최상희귀완성군만강하고중간조합이계속손해인빌드는완성도부족.
권고:먼저BLD01/05/06의같은입력도메인수직단위를완성하고DEF/스킬/장치확장. 만능딜러를억지면역/적실시간스탯추종으로막지말고효과기여·기회비용의원인을고친다. 수치재조정은실증거후정확효과별제시.

### 4. 적 구성·성장 체감 — 물량과 위협 역할을 구분
기존B는많은약적/무작위편성/위험도별등장간격가속,동시4/6/8·한기회최대2·기본공격자리/표적별시작간격을갖는다. 플레이어현재스탯에맞춰적HP추종금지유지. 단순적총수증가만으로광역빌드재미를보장하지않음.
점검대상은현재초기실지원돌격/사수에치우친테스트가구조물/객차우선/등반/강적전체를대표하는지다. 다수정리형·단일집중형·요격형·짐칸호위형이각각적격전장에서기여해야한다. 새로운미지원적을리소스만으로투입하지않음.
관측:스폰대기로적없는시간,죽이기전의체류시간,동시적수분포,한번에처치수,주요위협처치까지시간,차체/요원별예방·손실,적공격자리포화. 고점빌드가적을빨리정리할때대기만늘면적체력일괄증가보다원등장계획/필수전환시간을먼저비교. 모든잔여적즉시스폰같은새규칙은미승인.
보스단일상황의도탄/처치형약점은허용하되잡몹을반드시추가해모든빌드최강으로만들지않는다. 남은기본화력으로진행가능한지/특정빌드면필패인지분리. 보스면역/등장배율변경이번0.

### 5. 조작 부담 — 새7종이 기존 입력에 추가한 조건을 검토
B입력은선로위험대응·속도·궁극기·지휘지원·집중표적. AUTO OFF도평타/일반스킬/요격은계속, 궁극기자동사용만변경한다. 별도요격버튼/장비전투중교체추가0.
신T05는고속전환,T13은저속/순항,T16은궁극기역할→지원,T18은수리→지원사격. 모두같은순간성공해야정상운용되는필수콤보로만들면부담크다. 빠른기동/안정정비는다른운용선택으로평가하고둘다유지하려는속도왕복이최적이면재설계대상. 이미승인한1초/8초/10초창은임의변경하지않음.
기록:선로예고부터성공입력까지시간,스킬입력때선로누락,동시에기한있는입력수,잘못된지원소비/준비만료,속도왕복횟수,시선이전투/하단UI에머문비중. 최초와숙련·AUTO ON/OFF를나눠작전클리어및보너스수혜를비교한다. 순수초당탭수만으로부담판정0.
우선권고는선로생존정보/임박위험이읽히고준비기회는선택적으로사용가능하게표현하는것. 전체화면항상경고/팝업증식0. 새보조자동화/자동지휘지원/슬로모션은별도정책이며이번검토에서추가하지않음. 최종시안·독립리소스후실휴대기기손가림/터치영역검수가필요.

### 6. 장비 정리 시간 — 용량보다 선택 손실과 대기 원인
현승인기본30/DO05최대54,장착품제외,공간부족시원보상고정→합성/장착등정리→수령,2~3노드강제정리없음. 이구조유지. 자동폐기/강제합성/무한미수령창고/새정리재화0.
성급만보면합성이득이어도세트태그/고유합산·주재료인챈트계승과시술계수고정때문에기능이약해질수있다. 원합성결과미리보기·실장착효과/손실이정확히보이는지점검. 새추천점수/일괄자동합성으로의사결정을대체하지않는다.
또13의HP장비교체시비율유지규칙에는'낮은최대HP에서고정량치유→높은HP장비복귀'의이득이기록돼있다. 교체만으로무한회복은아니지만반복정비가생존최적전략이되는지실원치유/안전구간조건으로검증. 근거없이새교체제한을넣지않는다.
기록:안전정비활성시간/전체활성플레이비율,보상창막힘횟수,공간이남아있는데반복정리한이유,버려야했던기능재료,합성전후실효과손실,장비비교횟수,사용자가의도한생각시간vs조회지연/반복탭. 로딩/네트워크대기는숙고시간과분리. 기존20~30분예산의비전투/이동과중복집계하지않음.

### 7. 패배 납득 — 피해 대상·예고·완료실적의 세 축
요원우선/객차우선적이공존하므로요원이살아있어도객차HP손실가능. 요원HP·객차HP·열차HP와실전파원인을독립표시해야함. 요원방벽이차체를보호한것처럼보이거나객차전파를적의추가타로이중표시하면잘못된판단을유발.
검수기록은예고시각/위협source·대상/적격요격범위·실패이유/방어·흡수·실HP손실/전파/파손·Down/결정입력·원clock으로하나의피해계보복원. 마지막피해한개만으로원인확정하지않고전투말미사건을재현. 숨은예고/화면밖순간피해·회복이취소된이유·방벽소비로생긴빈틈도포함.
플레이어평가는본인이무엇에졌고다음에어떤배치/선로/지원선택을바꿀지설명가능한가. 실패시완료한실적을인정하는기존사용자정책은유지하며그것을전체작전승리/미완료보스보상으로바꾸지않음.
새패배리포트화면·리플레이서버/보관기간은이번에추가하지않음. 먼저기존HP/예고/결과표현과QA원사건기록으로오인원인을찾고필요한최소표현만별도시안요청.

### 8. 영구성장 대 작전성장 — 곱해지는격차를 같은표에서 비교
35 성장정의는훈련/승급이기초스탯에반영되고그뒤장비/증강등을1회적용. 따라서같은빌드에서도영구기초가커지면최종출력차이가남는다. 예를들어ATK기초가2배인두계정에같은승인장비/효과를적용하면다른조건동일시ATK비례출력도2배;장비11.5배를새계정비교로만설명해격차를숨기지않음. 모든스킬이ATK형이거나승급이DEF도같이올린다고가정하지않는다.
비교축은영구성장낮음/현재난이도기준/높음 × 작전획득저점/평균/고점. 각셀에서같은작전위험도·seed를사용하고실제로해금/입장가능한조합만. 동일계정내작전성장효과와계정간기초차이를따로표시,성장투자총량/지휘P차이도기록.
검토실패유형:허용된초기난이도에서저성장이핵심대응을해도순식간에패배;고성장은아무선택없이전부압도해빌드선택이무의미;고성장의회복/방벽이적정예고를상시무효;특정영구강화가공격/지원/방어를모두증폭해다른투자를배제.
낮은난이도를성장후쉽게도는것자체는성장보상이며문제로단정하지않음. 높은성장의도전은기존위험도/작전난이도선택에서검토하고플레이어실시간스탯에적맞춤상향0. 레벨정규화/새성장상한/캐치업보상/강제난이도는이번새정책아님. 실입력없는계정레벨50값을임의만들어전투실험하지않음.

### 종합·후속의 순서
문서검토결과:①8~9핵심성립목표와②고정정답회피는사용자방향으로기록. ③연사순환범용성 ④적공급공백 ⑤신증강속도/지원조건중첩 ⑥합성손실·HP교체정비 ⑦3종HP/피해전파인지 ⑧기초성장×작전성장차이는실검증우선위험으로정리. 실제재미/완성률/균형PASS아님.
실행선행은34 source-readiness감사의미지원효과/source 연결. 기존Server B-BUILD부모·Unity후속경계를유지. 문서상검토8개를했으므로새로운8개구현큐/강제수치수정은만들지않음.
다음독립기획단위는1번의'현재12노드에서층별합법적획득→핵심성립→남은실전시간' 모델입력정의다. 같은모델로2번조건부선택·3번빌드분포를측정한뒤적·조작·정리·패배·성장순실플레이로검수한다. 정책모형과실제실행source불일치시따로보고하고추정확률을실측처럼표시하지않음.


<a id="build-pacing-model-input-audit-20261011"></a>

## 8~9단계 빌드 성립 모델 입력·기존 경로 재집계 — 2026-10-11

### 수행 범위와 원자료
사용자의 후속 진행 지시에 따라 단계별 획득 → 합법적인 핵심 조합 → 남은 전투 기회와 시간의 측정 입력을 정리했다. 이번 수치는 보존된 12,000개 경로를 읽기 전용으로 재집계한 결과다. 현행 B 전투의 자연 빌드 완성률이나 실제 플레이 시간은 아직 계산하지 않았다.

원자료: QA/EquipmentRarity-20260929/minimum-target-routes.json.
SHA256: 15ad2bc4604a5ddd8a75a15ac1f20a9b2d1e48882b567eaf081dab5b347e3332.
12,000행 모두 route 길이 11을 확인했다. 최종 12단계 보스는 문자열 밖에 있다.
원 생성기 minimum-target-validation.mts는 qa-seven-kinds 정의와 경로 선택 가중치 A100/I80/E65/S50/N50/M50/R0을 사용한다. A=증강, I=인챈트, E=엘리트, S=무료 장비, N=일반 전투, M=상인, R=정비다. 이 값은 경로 선택 모형이며 사건 생성 확률이나 실제 사용자 선택률이 아니다.

기존 high-star-extra-validation.mts는 카테고리별 1성 환산 재료와 최대 합성을 중심으로 계산한다. 상인 방문마다 3개 구매 가능한 예산, 전투 성공 등을 가정하며 실제 세트·고유 효과·인챈트·요원별 3슬롯·실패를 빌드 조건으로 추적하지 않는다. 용량 검사도 과거 24칸 기준이다. 따라서 그 결과를 현행 30~54칸과 기능 조합 완성률로 그대로 사용하지 않는다. 역사적 시뮬레이터와 결과 파일은 변경하지 않았다.

### 1. 단계별 증강 기회와 남은 전투

각 행은 해당 방문 노드의 처리가 끝난 시점이다. 증강 사건 횟수는 원하는 카드 획득 횟수가 아니다. 남은 전투는 다음 방문부터 11단계까지의 일반·엘리트 사건만 센다.

| 완료 단계 | 표본 수 | 증강 0회 | 증강 1회 | 증강 2회 이상 | 남은 일반·엘리트 평균 | 남은 일반·엘리트 0회 |
|---|---:|---:|---:|---:|---:|---:|
| 6 | 12,000 | 2,522 (21.0167%) | 4,482 (37.3500%) | 4,996 (41.6333%) | 2.3254 | 520 (4.3333%) |
| 8 | 12,000 | 1,494 (12.4500%) | 3,558 (29.6500%) | 6,948 (57.9000%) | 1.4047 | 1,831 (15.2583%) |
| 9 | 12,000 | 1,152 (9.6000%) | 3,070 (25.5833%) | 7,778 (64.8167%) | 0.9344 | 3,352 (27.9333%) |
| 11 | 12,000 | 698 (5.8167%) | 2,214 (18.4500%) | 9,088 (75.7333%) | 0 | 12,000 (100%) |

재현 절차: JSON 배열 전체를 파싱하고 각 route 길이를 검사한다. d가 6/8/9/11일 때 route[0:d]의 A 개수로 세 그룹을 집계하고, route[d:11]의 N/E 개수 평균과 0회 경로를 구한다. 모든 행에서 세 그룹 합계가 12,000인지 대조했다. 최초 단일 행 JSON 본문 출력은 잘렸으므로 전문을 읽었다고 간주하지 않았으며, 이후 구조 파싱으로 전체 배열을 집계했다.

해석:
- 별도 증강 공급이 없는 이 모형에서 특정 증강 2개가 반드시 필요한 조합은 8단계 성립률이 57.9%를 넘을 수 없다. 원하는 후보가 안 나오거나 다른 카드를 선택하면 더 낮아진다. 장비 기반 조합까지 포함한 전체 빌드 성립률의 상한은 아니다.
- 9단계에 처음 조합이 작동하면 약 27.9%의 경로는 남은 일반·엘리트 전투가 없고 최종 보스에서만 활용한다. 보스도 없다는 뜻이 아니며 현재 B 전체 경로의 실측 확률도 아니다.
- 권고: 7~8단계에 핵심 운용이 시작되고 9단계까지 보완할 수 있도록 설계한다. 사용자의 8~9단계 거의 완성 목표 안에서, 9단계를 일괄적인 첫 작동 시점으로 잡지 않는다. 전투 강제 삽입·증강 보장·드랍표 변경은 이번에 적용하지 않는다.

### 2. 정밀 모델의 필수 입력
아래 항목은 시험 입력 명세이며 새 API 필드나 별도 상태 소유자를 강제하지 않는다. 기존 원소유 객체와 계약을 재사용한다.

| 영역 | 필요한 입력과 제한 |
|---|---|
| 맵 | 정의 버전·해시·seed, 전체 노드와 연결, 도달 가능한 후보, 보스 예외. 보존된 선택 경로만으로 선택하지 않은 전체 지도를 복원하지 않는다. |
| 경로 선택 | 합법 연결 중 균등 선택 / 역사적 선호 가중 / 현재 재료·예산·정비 필요를 반영한 적응 선택을 각각 비교한다. 공개 지도는 이용하되 미래의 숨은 추첨 결과를 미리 아는 전략은 별도 참고군으로 둔다. |
| 장비 공급 | 현행 v15의 구간별 성급·수량·추가 획득, 실제 카테고리·태그·고유 효과의 분포와 상관관계, 발급 instance. 명시되지 않은 분포를 임의로 균등 처리하지 않는다. |
| 상인 | 도착 시 실제 전술보급 잔액, 가격·할인·재고·구매 순서·공간. 모든 방문에서 3개 구매 가능하다는 과거 가정을 기본군에 사용하지 않는다. |
| 장착·보관 | 요원별 실제 3슬롯, 기본 30칸과 DO05 랭크당 6칸, 장착품 제외, 수령 대기·잠금·부분 포기. 총 재료량만으로 장착 가능성을 판정하지 않는다. |
| 합성 | 동일 카테고리·성급의 두 재료, 주재료 선택, 세트·고유 효과·인챈트 계승과 시술 성급, 장착 상태 변화. 높은 성급이 되었다는 이유로 기능 보존을 가정하지 않는다. |
| 인챈트 | 실제 3종 후보와 대상 적격성, 시술 풀·계수·시술 성급·승급 조건. 강한 추가 공급 가정을 기본 공급과 분리한다. |
| 증강 | 현재 B 지원 카탈로그와 버전, 요원 능력·모드·보유 효과에 따른 적격 목록, T12/T20 배타. 정책상 존재하는 카드 전체를 실제 실행 지원으로 간주하지 않는다. |
| 리롤 | 슬롯별 무료 3회·추가 1회 정책, 추가 사용 권한과 소진 조건, 현재 표시 3장 제외 및 빈 후보 처리. solo-augment-selection.ts의 issuance.eligibleIds와 rerollContext를 기준으로 대조한다. 과거에 본 모든 카드가 영구 제외된다고 가정하지 않는다. |
| 전투 능력 | 실제 요원 effective profile·계수·마나·거리·행동 점유, 지휘 부모 조건·포인트·랭크, 객차 효과. 미지원 효과는 정책상 계산과 실제 실행을 구분한다. |
| 실패·시간 | 단계별 도달·승패·중단, 전투 시간·스폰 대기·비전투 정리 시간, 효과 발동 사건. 임의 생존 확률이나 TTK를 실측처럼 넣지 않는다. |

재사용 가능한 것은 역사적 경로·seed, 성급·수량 계산 일부와 기존 장비·합성·리롤 계약이다. 추가 확인이 필요한 것은 현행 태그/고유 효과 발급 분포, B 적격 풀과 실행 지원, 상인 경제 및 실패·시간 자료다. 없는 값을 0 또는 100%로 채워 완성률을 내지 않는다.
난수는 실제 게임의 seed·분리 영역·추첨 순서를 재현하고 정의 버전을 고정한다. 비교안이 난수 소비를 바꾸면 같은 seed라도 이후 획득이 같지 않을 수 있음을 기록한다.

### 3. 빌드 성립 판정
성급 고점과 기능 조합은 별도로 측정한다.

1. 재료 보유: 필요한 실제 instance 또는 효과를 보유했지만 장착·적격성·발동은 확인하지 않은 상태.
2. 조합 적격: 합법적인 3슬롯과 세트·고유·인챈트, 능력·거리·자원·지휘 조건이 해당 핵심 운용을 허용하는 상태.
3. 기능 확인: 원 전투 실행에서 해당 운용이 실제로 발생하고 기여가 관측된 상태.
4. 고점 강화: 이미 작동하는 조합의 성급·고유 등급·추가 연계로 출력이 높아진 상태.

BLD01~08은 #combat-build-loadouts-validation-v1-20261011의 핵심·중간·고점 조건을 개별 판정식으로 옮긴다. 예를 들어 BLD01의 공속/EQ12 순환, O08 태세, O10 추가 표적 경로를 하나의 카드 이름 검사로 합치지 않는다. 실제 기본 적중과 대기 중인 스킬, 태세 적용, 유효 추가 표적 등 각 원효과 조건을 확인한다.
최소 성립 판정에는 저성급 유효 연계, 고성급 무효 조합, 마나 부족, 적용 스킬 없음, 보호량 0, 범위 밖, 고유 효과 미장착, 중복 인챈트 등 참·거짓 사례가 필요하다. 모든 요원이나 최고성급 3개 완성을 기본 기준으로 강제하지 않는다.

최초 성립 단계와 8/9단계 현재 성립 여부를 따로 기록한다. 합성·교체로 효과가 사라지면 최초 이력은 남기되 현재 성립은 해제한다. 실행 미지원은 실패로 섞지 않고 미확인으로 기록한다. 한 작전이 여러 빌드에 해당할 수 있지만 전체 성립 작전 수는 중복을 제거한다.

### 4. 분모·출력·후속 순서
매 단계에 전체 시작 수 N, 도달 수 R, 조합 적격 수 E, 기능 확인 수 F, 미지원 수 U를 표시한다. 도달률 R/N, 도달자 중 적격률 E/R, 전체 시작 중 적격률 E/N을 구분한다. 전부 생존을 가정한 정책 모델은 그 가정을 명시하고 실제 클리어율로 부르지 않는다.

핵심 성립 후 남은 일반·엘리트·최종 보스 수와 유효 전투 초를 기록한다. 시간 자료가 없으면 전투 수만 표시하고 시간은 미확인으로 둔다. 정리·로딩·명시적 일시정지는 전투 시간에 합치지 않는다.
기본 공급 / 전투 추가 공급 / 강한 인챈트 추가 공급, 무료 리롤만 / 추가 권한 사용, 경로 전략별 결과를 분리한다. 주력 1인 성립과 분대 지원 성립, 전원 성립도 혼동하지 않는다.

후속 순서:
1. 원 발급·거래 계약의 분포와 적격 풀을 확정 입력으로 인수.
2. 단계별 합법 재료·조합 가능성을 계산.
3. 기존 원효과 실행과 연결해 기능 성립 검증.
4. 실패·시간 자료를 반영해 실제 플레이 성립률과 활용 시간을 측정.

추후 재현 실행기·입력 버전·결과는 기존 QA 폴더에서 역사적 결과와 구분해 보존한다. 이번에는 새 실행기나 전투 시뮬레이션 결과 파일을 만들지 않았다. 근거 없는 '8단계 완성률 80%' 같은 목표를 결과로 제시하지 않는다. 병목이 확인되면 공급량을 먼저 늘리기보다 특정 증강 두 장만 요구하는 구조와 대체 성립 경로 부재를 먼저 검토한다.


<a id="late-combat-time-survival-review-20261011"></a>

## 10·11단계 전투 추가: 시간·생존 부담 검토 — 2026-10-11
사용자 후속 진행 요청. 확정된 전투+사건 및 보상 방향을 유지하면서 기존 시간 가정과 생존 검수 경계를 대조했다. 실제 전투 실행/생존률/TTK/회복량 측정은 이번에 수행하지 않았다.

### 1. 기존 시간 예산에 대한 산술
14 operation-scale-trial-v02의 기존 가정은 일반90초/엘리트150초/보스180초, 비전투선택30초, 노드간전환15초다. 일반5+엘리트2+비전투4+보스1의 표준 예시는1215초(20분15초). 모두 실측이 아닌 기획 예산이다.
동일한90초를 신규 일반전투에 적용하면 추가0/1/2전투는 각각0/90/180초 증가다. 예시 경로의10/11원사건 구성에 따라 계산상20분15초/21분45초/23분15초다. 모든 경로가 원래20분15초였다고 가정하지 않는다.
원12,000경로의 추가0회2565(21.375%),1회6083(50.6917%),2회3352(27.9333%)에 적용한 추가전투시간 평균은95.9025초다. 원사건 선택시간은 그대로 남으며 이미 포함한 노드간 이동/선택을 다시11회 더하지 않는다. 전투 보상 정리의 추가 시간, 원올인/표적증원, 로딩, 실제 후반 빌드에 따른 처치시간 변화는 이95.9초 밖의 항목이다.
정확한 변화는 추가 전투 시간 + 추가 보상 정리 + 기존 전투의 장비강화에 따른 시간 변화 + 기타 전환 변화다. 강해진 장비로 보스가 짧아진다고 미리 시간을 차감하지 않는다. 기존 활성플레이가30분에 가까운 경로는 초과할 수 있다. 20~30분은 유지할 측정 목표이며 제한시간/자동실패나 모든경로 보장이 아니다.

### 2. 생존상 핵심 변화
45의 기본 정비지원 프로필은4/8단계 선택지이며 강제방문이 아니다. 8에서 정비를 선택해도9가전투이면9→10→11→12보스의4노드 연속전투가 가능하다. 앞 복합계약의정비선처리행을 근거로10/11에 새정비를 배치하지 않는다.
추가 전투는 적에게 공격기회를 늘리지만 공격·방벽·회복 장비도 공급한다. 반대로 빠른 처치가 늘면 순피해가 줄 수 있으므로 전투횟수만으로 HP손실이나클리어율을 계산하지 않는다. 전체피해와총치유가 같아도 순간집중피해/요격행동점유/치유범위밖/시전자다운으로 패배할 수 있다.
현재 일반후반13~16적/동시8·원공격참여제한·대요원원시피해24×위험도×구간의 해당승인범위를 그대로 사용한다. 그 원피해를 객차/열차·특수공격에 복사하지 않고 실프로필의 DEF/감소/흡수/최종내림을 사용한다. 등장간격 위험도가속을 공격/탄속/등반시간에 중복 적용하지 않는다.

### 3. 다음 실검증 설계
같은9단계종료상태에서 기존/복합안 두 실행을 비교한다. 실제 장비·세트·인챈트·원지원효과·HP/마나/CD/다운/객차/열차/보급 상태를 고정하고, 신규전투와그보상이 만드는 차이는 실제순서로 적용한다. 아직 얻지 않은10/11보상으로해당전투진입스탯을미리높이지않는다.
경로군은①9비전투+10/11비전투→추가2회②9전투+10/11비전투→연속위험③원10/11전투→추가0회 대조④원1전투+1비전투→추가1회. 정비8선택/우회,일반/엘리트9,기존위험도·작전난이도,4요원+짐칸/5요원은 각원적격 범위에서 구분한다.
편성은지원능력이 실제있는 피해형/요격형/방벽형/치유형을 비교하며 특정힐러필수라는새제약을두지않는다. AUTO는현재 궁극기자동사용만바꾸고기본/일반스킬/요격은원정책대로 유지한다. 옛 회복범위제안절의 '자동스킬전체중단'을최신규칙으로복원하지않는다.
빌드군은저성급유효세트/혼합성급핵심완성/5성고점으로나누며공속5성3개+속사같은희귀조합을기본생존필수로놓지않는다.

필수관측:
- 요원·객차·열차 각각의 전투진입/종료HP,최저HP,다운/파손/복귀시간,직접피해와객차전파 계보.
- 실치유/초과치유,발급방벽/실흡수/만료손실,실요격성공·실패이유와기본공격손실. 서로다른대상의방벽/치유를합쳐열차생존량으로환산하지않는다.
- 위협발생부터유효대응까지시간,집중공격순간,마나부족/CD/거리/행동점유로대응불가능했던구간.
- 실제정비/상인회복의구매가능성과지출,잔액,선택포기이유.20비상금유보를실제수리로취급하지않는다.
- 효과적인전투초/적없는스폰대기/노드전환/추가정리시간,전체활성시간과벽시계시간. 보스전에실패한run도분모에서제외하지않는다.

최대HP변경/부활/복구가있는구간은단순시작HP−피해+치유식만으로감사하지않고원상태전이원장을대조한다. 8/9에서성립한빌드의활용시간과9→보스도달률은별도보고한다.

### 4. 권고와 조정 순서
현재는추가전투보상과기존전투규칙을유지하고실검증한다. 자동완전회복/새10·11정비/강제힐러/보스HP상향을이번검토에서추가하지않는다.
문제가나오면①중복피해·잘못된수혜/미지원효과·숨은예고같은오류②무의미한스폰대기/중복전환③후반적구성의특정대상편중과공격기회④회복·방벽·재화와전투부담을순서대로대조한다. 실제수치조정은근거와전후안을작성하여기존승인값과구분한다.
20~30분초과라는이유만으로강화한빌드의체감전투를삭제하거나보상선택제한시간을강제하지않는다. 반대로30분안에들어온다는이유로순간사망/힐러강제를허용하지않는다.

현재결과는시간산술및문서검토완료. 원source-readiness의15인profile·효과실지원·전투owner연결과실로그가후속선행이며실제생존·재미PASS아님. 기존Server/Unity원작업에필요검수입력으로연결하고중복실행큐나자동폴링을만들지않는다.

