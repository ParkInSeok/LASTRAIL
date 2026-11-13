<!-- RESOURCE-REFERENCE-AUDIT-20261029 -->
> **리소스 참조 재검사 완료(2026-10-29):** 남은 169개 문서 표기를 실제 위치·묶음/논리 경로·역사 자료로 대조했다. 정확한 경로는 본문을 정정했고, 없는 옛 증거는 현행 입력에서 제외 표시했다. 상세: D:/Project/LASTRAIL/QA/ResourceCleanup-20261028/169-decisions.json. 역사 자료는 최신 승인본을 대체하지 않는다. 실제 Player에는 옛 요원 미술과 임시 객차가 남아 있어 최신 아트 연결 완료가 아니다.

# LASTRAIL DIRECTION 작업 큐

전투 카드 프레임·열차 운행 손잡이·콘솔 프레임 제작 시 [스킨 교체 단위와 실제 크기 검수 기준](../superpowers/plans_categories/51_UI_ART_DETAIL_CONTRACTS.md#combat-hud-skin-slots)을 적용한다. 기본 외형부터 같은 기준을 사용한다.

## 최신 제작 우선순위 — 2026-10-19 사용자 정정

사용자 지시: 닉스의 1~4번 대기(idle)·기본 공격 모션을 먼저 완료하고, 다섯 요원이 갖춰진 뒤 나머지 애니메이션을 진행한다. 아래 과거의 네 요원 스킬/잔여 동작 우선 또는 닉스 첫 후보 후순위 문구보다 이 순서를 우선한다.
- 현재 우선 단위는 D-NYX-FIRSTPOSE-01의 제작 정책·최신 원본 확인 → 전투 카메라 2번 첫 자세 검토/승인 → 제작도구에서 방향별 12프레임 대기·바람/기본 공격과 독립 효과 연결 → 1~4번 전투 조합 검수·승인본 통합이다. 첫 자세와 단계별 승인 절차는 유지하며 미승인 후보를 승인본으로 취급하지 않는다.
- 닉스 기본 모션 완료 후 다섯 요원의 나머지 스킬·다운/복귀 등 잔여 애니메이션과 궁극기 연출을 기존 상세 계약/큐 범위에 맞춰 진행한다. 기존 네 요원 승인 대기/기본 공격은 재제작하지 않는다.
- 이번 기록은 우선순위와 닉스 기본 모션 목표의 사용자 승인이다. 선행 미완료 세부 후속 큐를 미리 발행하지 않으며 단계별 자산/원사건 계약과 검수 근거를 갖춰 확장한다.


2026-09-22 사용자 요청으로 만든 담당별 실행 큐. 신규 설계 원본이나 완료 이력 보관소가 아니다. [공통 운영 규칙](INTEGRATION_WAITLIST.md#role-work-queues-20260922), 프로젝트 AGENTS.md를 먼저 따른다. 최신 승인 기획만 신규 작업에 적용하며 구 정책은 신규 적용 대상에서 폐기한다. 진행 중 run의 START 스냅샷과 역사/검증 자료는 보존한다.

착수 가능한 항목만 이 큐에 둔다. 선행 미완료 후속은 기존 WAIT의 후속 등록 정의에 보관하며 실행 큐에 미리 추가하지 않는다. 착수 시 실제 파일·버전과 진행 상태를 기록한다. 완료 담당은 기존 인계/QA에 큐 ID·계약 버전·검증 근거를 보존하고, WAIT의 자기 ID 관련 후속 조건을 확인한다. 모든 선행이 완료된 후속을 대상 큐에 중복 없이 추가·확인한 뒤 자기 완료 블록을 삭제한다. 후속 등록 실패 시 `구현 완료·후속 등록 필요`로 남겨 재시도한다. 상세 절차는 [완료 시 후속 큐 등록](INTEGRATION_WAITLIST.md#completion-driven-queues-20260922)을 따른다. 타 큐에는 승인된 후속 항목 추가만 허용하며 기존 항목 수정/삭제는 담당 소유다. 빈 큐는 종료하며 자동 메시지·폴링하지 않는다.

정책 원본: [44 전투](../superpowers/plans_categories/44_COMBAT_DETAIL_CONTRACTS.md), [09 지휘실](../superpowers/plans_categories/09_COMMANDER_PROGRESSION.md), [13 장비](../superpowers/plans_categories/13_EQUIPMENT_ENCHANTMENTS.md), [14 경로](../superpowers/plans_categories/14_MAP_EVENTS_DROPS.md), [31 증강](../superpowers/plans_categories/31_AUGMENT_EFFECT_REFERENCE.md), [32 서사](../superpowers/plans_categories/32_NARRATIVE_REFERENCE.md), [45 정비](../superpowers/plans_categories/45_MAP_STATION_DETAIL_CONTRACTS.md). 각 문서의 최신 승인 절을 사용한다. [통합 대조 I01~09](../superpowers/plans_categories/34_COMBAT_POLICY_COMPANION.md#integration-conflict-audit-20260922-v01)는 발견 근거이며 런타임 전수검증 결과가 아니다.

현재 등록 항목은 아래 최신 역할 큐 및 B 이행 표시를 따른다. 과거 D-01 Direction 완료 근거: [기존 인계](DIRECTION_2-D-20260913.md#d01-direction-complete-20260924). 후속은 INTEGRATION_WAITLIST의 선행 충족 후 등록.


### D-WB-01@world-boss-binding-v2 / B 이행 대상 — 구 A 신규 제작 중단·수장별 계약 재검토
선행 P-15 승인계약 [44 v2](../superpowers/plans_categories/44_COMBAT_DETAIL_CONTRACTS.md#world-boss-binding-v2) 완료. 약탈왕/베일/오르트 실제 수장명과 신규계약ID를 시안 manifest에 연결. A/C 각3페이즈·2회차의 장치/순서·경고·실패표적·역할별입력/읽기를 전수대조하고 필요한부분 새버전 수정. B는 베일본체 내부침입·외부같은보스피격창구·별도기믹장치·종료후퇴각으로 재설계. 기존A승인원화/공통HUD 재사용, B/C 전용외형 미술승인은 별도;A원화복제 금지.
서버전체완료를원화착수선행으로두지않는다. 실carId/피해/타이밍바인딩은 Server완료뒤 Unity 인수. phase/round·담당/읽기/실패표적·독립 alpha자산/입력영역·430px 검수근거를기존QA에보존. 완료는시안/자산담당범위이며실전투통합완료아님. 후속Unity는Server계약 AND 필요한자산완료조건확인후등록.
진행 2026-09-29: [수장별 검토 후보](../../QA/DirectionImpact-20260921/world-boss-binding-v01/index.html) 제작·정적검수 기록. B/C 신규 인물 원화와 430px 실제 조합·입력 검수 미완료이므로 큐 유지. 근거 [REVIEW](../../QA/DirectionImpact-20260921/world-boss-binding-v01/REVIEW.md).
미술 후속 2026-09-29: [v02 고퀄 검토 후보](../../QA/DirectionImpact-20260921/world-boss-binding-v02-art/index.html)에서 B/C 전용 투명 차량 원화·조합을 추가. [검수/잔여](../../QA/DirectionImpact-20260921/world-boss-binding-v02-art/REVIEW.md). 사용자 미술 승인과 430px 실제 검수 전이므로 항목 유지.
사용자 승인/추가 2026-09-29: v02 월드보스 화면 승인. [v03 약탈왕 인물·열차 분리 후보](../../QA/DirectionImpact-20260921/world-boss-binding-v03-art/index.html)에서 합성 원본 보존·단독 alpha 2종을 별도 레이어로 연결. [REVIEW](../../QA/DirectionImpact-20260921/world-boss-binding-v03-art/REVIEW.md). 새 약탈왕 원화 승인과 430px 실제 검수 전이므로 항목 유지.

2026-09-29 원화 개선: [v04 약탈왕 퀄리티 개선 후보](../../QA/DirectionImpact-20260921/world-boss-binding-v04-art/index.html) / [이전·개선 비교](../../QA/DirectionImpact-20260921/world-boss-binding-v04-art/compare.html) / [REVIEW](../../QA/DirectionImpact-20260921/world-boss-binding-v04-art/REVIEW.md). 독립 alpha·화면 참조·JS/JSON 정적 검수 완료. 사용자 신규 원화 승인 및 430px 실기 조합 검수 잔여. 기존 v02 화면 승인과 v03 자산 보존.

2026-09-29 시점 통일: [v05 세 보스 차량 상단 사선 후보](../../QA/DirectionImpact-20260921/world-boss-binding-v05-art/index.html) / [이전·개선 차량 비교](../../QA/DirectionImpact-20260921/world-boss-binding-v05-art/vehicle-compare.html) / [REVIEW](../../QA/DirectionImpact-20260921/world-boss-binding-v05-art/REVIEW.md). 우리 열차 카메라를 기준으로 약탈왕·베일·오르트 차량 원화 3종을 교체했다. 새 투명 자산·참조·JS/JSON 정적 검수 완료. 신규 미술 승인, 430px 실기 조합 검수 및 실제 계약 연결 잔여로 D-WB-01 유지.

2026-09-29 선로 시점: [v06 차량·선로 상단 사선 후보](../../QA/DirectionImpact-20260921/world-boss-binding-v06-art/index.html) / [전투 조합](../../QA/DirectionImpact-20260921/world-boss-binding-v06-art/screen.html?boss=world_boss_raider_king&phase=1&round=1&role=solo) / [REVIEW](../../QA/DirectionImpact-20260921/world-boss-binding-v06-art/REVIEW.md). 두 선로의 레일·침목·노반을 차량 카메라에 맞춰 재구성. CSS·JS/JSON 정적 검수 완료. 실제 화면에서 바퀴/선로 정렬과 430px 조합·터치 검수 및 사용자 신규 미술 승인 잔여. 기존 승인 화면/원화 보존, 항목 유지.

2026-09-29 사용자 시각 피드백: v06의 선로 회전이 차량과 어긋남. [v07 평행 선로 재정렬 후보](../../QA/DirectionImpact-20260921/world-boss-binding-v07-art/index.html) / [전투 조합](../../QA/DirectionImpact-20260921/world-boss-binding-v07-art/screen.html?boss=world_boss_ort&phase=1&round=1&role=solo) / [REVIEW](../../QA/DirectionImpact-20260921/world-boss-binding-v07-art/REVIEW.md). skew 제거, 선로 위치·폭과 베일/오르트 차량 높이 보정. CSS/JS/JSON 정적 검수 후 실제 화면 바퀴·선로 정렬은 사용자 시각 확인 필요. v06은 미승인 이력으로 보존, 큐 유지.

2026-09-29 차량 축 보정: [v08 보스 차량 기울기 후보](../../QA/DirectionImpact-20260921/world-boss-binding-v08-art/index.html) / [오르트 전투](../../QA/DirectionImpact-20260921/world-boss-binding-v08-art/screen.html?boss=world_boss_ort&phase=1&round=1&role=solo) / [REVIEW](../../QA/DirectionImpact-20260921/world-boss-binding-v08-art/REVIEW.md). 평행한 선로·우리 열차는 유지하고 보스 차량의 바퀴 축 표시 각도를 각 자산에 맞춰 보정. 정적 CSS/JS/JSON 검수 후 사용자 육안 정렬 검토와 430px 화면 검수 잔여. v07 보존, 큐 유지.

2026-09-29 구도 재작화: [v09 우리 열차 카메라 기준 보스 차량 3종](../../QA/DirectionImpact-20260921/world-boss-binding-v09-art/index.html) / [동일 구도 비교](../../QA/DirectionImpact-20260921/world-boss-binding-v09-art/camera-compare.html) / [REVIEW](../../QA/DirectionImpact-20260921/world-boss-binding-v09-art/REVIEW.md). v08의 차량별 회전 보정으로 해결되지 않아 우리 기관차·객차 원화를 구도 원본으로 사용해 약탈왕·베일·오르트 차량을 재작화하고 회전을 제거. 독립 alpha·참조·JS/JSON/CSS 정적 검수 완료. 신규 미술 승인, 430px 전투 조합·선로 정렬 실기 검수, 계약 연결 잔여로 D-WB-01 유지.

2026-09-29 선두 기관차 보완: [v10 베일·오르트 기관차+전용 객차 조합](../../QA/DirectionImpact-20260921/world-boss-binding-v10-art/index.html) / [편성 직접 비교](../../QA/DirectionImpact-20260921/world-boss-binding-v10-art/consist-compare.html) / [REVIEW](../../QA/DirectionImpact-20260921/world-boss-binding-v10-art/REVIEW.md). 각 전용 객차 앞에 독립 alpha 선두 기관차를 연결해 객차 한 칸만 보이던 시안을 보완. 신규 기관차는 별도 HP·점수·입력 대상이 아닌 시각 요소. 정적 자산·JS/JSON/CSS 검수 완료; 사용자 미술 승인과 실제 조합·선로 정렬·430px 검수는 잔여. v09 보존, D-WB-01 유지.

사용자 승인 2026-09-29: v10 선두 기관차+객차 조합 미술 채택. [manifest](../../QA/DirectionImpact-20260921/world-boss-binding-v10-art/manifest.json)·[QA](../../QA/DirectionImpact-20260921/world-boss-binding-v10-art/qa.json)에 반영. 360/430px 실제 조합·바퀴/선로 정렬 및 Server/Unity 계약 연결은 미완료이므로 D-WB-01은 유지.

<a id="approved-plan-queue-sync-20260929"></a>
### D-EQ-01@equipment-supply-v18 / 착수 가능 — 최종 장비·보관함·상인 화면 변경
원본 [13 v16~v18](../superpowers/plans_categories/13_EQUIPMENT_ENCHANTMENTS.md#inventory-capacity-approved-v18), [45 상인v16](../superpowers/plans_categories/45_MAP_STATION_DETAIL_CONTRACTS.md#equipment-merchant-contract-v16),09 DO05 현행표. 사용자최종승인/원화선행분리정책에따라서버전체완료없이시안착수.
범위: 보관함30~54/점유수·내부스크롤·장착품제외,가득참→원보상대기→보관함정리→부분/잔여수령복귀;유효인챈트1/2/3카드의빈자리/적격없음상태;종류3종선택;상인가격8/16/32/할인후실가격·잔액/공간부족·원재고.30칸을한화면30개고정노출로해석금지,불필요한매노드정리알림추가0.
기존승인장비상세·드래그/합성미리보기·공통팝업/지휘실미술재사용,실수치/슬롯/문구독립. 구24/48·6성·항상3카드표시는새버전에서제거하고역사시안보존. 신규미술은자동승인아님.
완료근거: 실제크기의빈상태/30가득참/54상한/대기수령·복귀·부분수령/잠금·합성불가·구매공간부족 상태표와manifest/독립요소/입력·스크롤검수. 기존QA에보존. 후속U-EQ-01은S-02의해당원계약/QA AND D-EQ-01필요자산완료후등록.
안내: 위역사‘현재착수가능항목없음’은현행아님. 현재실행항목은기존D-WB-01과본D-EQ-01이다.
진행 2026-09-29: [25상태 검토 후보](../../QA/DirectionImpact-20260921/equipment-supply-v01/index.html) 제작·정적검수 기록. 실제 360/430/720 조합·터치·내부스크롤 검수와 신규 화면 미술 승인 미완료이므로 큐 유지. 근거 [REVIEW](../../QA/DirectionImpact-20260921/equipment-supply-v01/REVIEW.md).
미술 후속 2026-09-29: [v02 고퀄 검토 후보](../../QA/DirectionImpact-20260921/equipment-supply-v02-art/index.html)에서 승인 프레임·장비 아틀라스 재조합과 보관함/카드/상인 시각 위계 보강. [검수/잔여](../../QA/DirectionImpact-20260921/equipment-supply-v02-art/REVIEW.md). 사용자 미술 승인과 360/430/720 실제 검수 전이므로 항목 유지.
사용자 변경 2026-09-29: 위 범위의 `유효1/2/3카드·종류3종선택·항상3카드제거`는 역사 지시로 대체. [v03 수정 후보](../../QA/DirectionImpact-20260921/equipment-supply-v03-art/index.html)는 빈칸 아이콘/SET01~06 표시, **항상3카드·장비카드당 미리 제시된 장비1개 선택**, 구매·확인 버튼 프레임 수정. [REVIEW](../../QA/DirectionImpact-20260921/equipment-supply-v03-art/REVIEW.md). 13 v16 원본 개정/Server·Unity 계약 조정은 Plan 선행, Direction 시안·실크기검수는 계속 진행. 현행 장비 상인은 장비3재고만 승인; 회복 포션 판매 정책 없음.
2026-09-30 세트 문양 개선: [v04 검토 후보](../../QA/DirectionImpact-20260921/equipment-supply-v04-art/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-supply-v04-art/REVIEW.md). 배지 좌상단 위치는 유지, 원 내부 여섯 문양을 중앙 정렬하고 조형·대비를 개선. v03 보존. 실기 화면 크기/터치 검수와 사용자 미술 승인 전까지 D-EQ-01 유지.
2026-09-30 후속 아이콘 개선: [v05 검토 후보](../../QA/DirectionImpact-20260921/equipment-supply-v05-art/index.html) / [빈칸](../../QA/DirectionImpact-20260921/equipment-supply-v05-art/screen.html?state=empty) / [잠금](../../QA/DirectionImpact-20260921/equipment-supply-v05-art/screen.html?state=locked) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-supply-v05-art/REVIEW.md). 빈칸 소켓·자물쇠 독립 SVG와 SET03 전술 제어 단일 모래시계 문양. v03/v04 보존. 실기 검수·사용자 미술 승인 전 D-EQ-01 유지.
로컬 검토 링크 보정 2026-09-30: 대화 링크가 `?`를 `%3F`로 파일명에 인코딩해 열리지 않아 쿼리 없는 [장착 해제 불가](../../QA/DirectionImpact-20260921/equipment-supply-v05-art/unequip.html) / [빈칸](../../QA/DirectionImpact-20260921/equipment-supply-v05-art/empty.html) / [잠금](../../QA/DirectionImpact-20260921/equipment-supply-v05-art/locked.html) 직접 진입 파일을 추가. 같은 본체 View·자산을 쓰며 초기 상태만 지정한다.
사용자 미술 승인 2026-09-30: v05 `locked.html` 검토 후 승인. [manifest](../../QA/DirectionImpact-20260921/equipment-supply-v05-art/manifest.json) / [QA](../../QA/DirectionImpact-20260921/equipment-supply-v05-art/qa.json)에 반영. 25/25 상태 VM 렌더, 빈칸30·잠금 자물쇠·인챈트5상태×3카드 정적 재검수 PASS. 360/430/720 실기 터치·스크롤 및 원계약 개정/Server·Unity 연결은 남아 D-EQ-01 유지.
사용자 입력 변경 2026-09-30: 빈칸이 있을 때 보상 장비 카드 직접 클릭/터치로 **해당 장비 한 개**를 수령한다. [v06 직접 수령 후보](../../QA/DirectionImpact-20260921/equipment-supply-v06-art/near.html) / [가득 찬 대기](../../QA/DirectionImpact-20260921/equipment-supply-v06-art/pending.html) / [남은 장비](../../QA/DirectionImpact-20260921/equipment-supply-v06-art/cleared.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-supply-v06-art/REVIEW.md). v05 승인 미술은 보존, 버튼식 부분수령 입력만 교체. 27/27 상태·40/40 VM 흐름검사 PASS. 사용자 v06 화면 검토와 실기 터치, Plan D3 입력 설명 개정 및 Server/Unity 실제 원거래 연결 전 D-EQ-01 유지.
2026-09-30 후속: 사용자가 기존 **미장착 보유 장비 한 개 수동 버리기** 정책을 승인. [v07 검토 시안](../../QA/DirectionImpact-20260921/equipment-supply-v07-art/index.html) / [가득 찬 대기→상세→확인→직접 수령](../../QA/DirectionImpact-20260921/equipment-supply-v07-art/pending.html) / [잠금 차단](../../QA/DirectionImpact-20260921/equipment-supply-v07-art/locked.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-supply-v07-art/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/equipment-supply-v07-art/manifest.json). 승인 v05 미술과 v06 후보 보존. 28/28 상태 렌더, 취소·잠금·단건 폐기 후 빈칸·보상 자동수령 없음·직접 수령 14검사 PASS. v07 미술/실기 검토, Plan 원본 및 Server/Unity 실거래 미완료로 D-EQ-01 유지.

2026-09-30 D-EQ-01 고퀄 v08: [전체 후보](../../QA/DirectionImpact-20260921/equipment-supply-v08-art/index.html) / [수령 대기](../../QA/DirectionImpact-20260921/equipment-supply-v08-art/pending.html) / [인챈트](../../QA/DirectionImpact-20260921/equipment-supply-v08-art/enchant.html) / [상인](../../QA/DirectionImpact-20260921/equipment-supply-v08-art/merchant.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-supply-v08-art/REVIEW.md). 계약 direction.equipment-supply.art-v08.20260930. 승인 v05 프레임·장비·공통팝업 재사용, 신규 독립 정비고 배경(image_gen)과 polish.css 금속/조명/카드 조합. 28/28 상태·목록 링크, HTML11·CSS/자산/JSON 검수 PASS; 상호작용은 검수된 v07 동일소스. v05/v07 보존. v08 미술 최종 승인·360/430/720 실기 검수 및 WAIT-004 실제 연결은 잔여, D-EQ-01 유지.

2026-09-30 D-EQ-01 사용자 정정: 고퀄 요청은 통짜 최종시안 원화. [v09 PNG3종 비교](../../QA/DirectionImpact-20260921/equipment-supply-v09-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-supply-v09-illustration/REVIEW.md). 보관함·수령 대기, 인챈트3카드, 상인3상품을 built-in image_gen으로 한장 완성 원화 제작·직접 육안 확인. 제목/장비명·대기품 잠금 오류 수정. 미술 승인 대기·reference-only, 28상태 전부 완료/런타임 자산 완료로 승격하지 않음. v05/v07/v08 보존.

2026-09-30 사용자 최종 미술 승인: v09 통짜 작전장비/인챈트/상인3종 채택. 확정 버튼 문구 수령 포기/건너뛰기, 수정 PNG inventory-pending-final.png/enchant-final.png로 등록. [승인본 비교](../../QA/DirectionImpact-20260921/equipment-supply-v09-illustration/index.html). 다음 [v10 장비상세·버리기확인2종](../../QA/DirectionImpact-20260921/equipment-followups-v10-illustration/index.html) 제작, 미술 검토 후보. 모달 뒤는 동일 장비창host의 암전 문맥이며 별도 페이지/중복 연결 대상 아님. v09 원화 승인과 독립자산·실기·Server/Unity 완료를 구분하고 D-EQ-01 유지.
2026-09-30 D-EQ-01 사용자 정정 — 버리기 추가 확인 제거: 보유 미장착 장비 상세 → 버리기 버튼으로 단건 요청, 별도 ‘장비 버릴까요’ 모달 없음. v07/v08 view.js·manifest·REVIEW·qa.json 및 equipment-followups-v10-illustration/index.html·manifest·REVIEW 반영. discard-confirm.png는 역사 자료로 보존하되 현행 시안/연결에서 제외. 직접 버리기·중복/잠금 차단·자동수령 없음·후속 개별수령·스크롤 유지 Node VM/구문 PASS. Plan P-EQ-DISCARD-01의 이전 확인 흐름은 이 사용자 지시로 대체되며 Plan 소유 원문 개정 필요. 실제 Server/Unity 거래·독립자산·실기 검증 미완료로 큐 유지, 후속 Unity 작업 조기 등록 없음.
2026-09-30 D-EQ-01: 사용자 현행 v10 보유 장비 상세 최종 미술 승인. 추가 버리기 확인은 제외. 다음 승인된 상인 상태 merchantPoor(보급5·20/30)와 merchantFull(보급40·30/30)의 통짜 원화를 equipment-merchant-states-v11-illustration으로 제작. 기존 상인 구도·가격8/16/32·재고1 유지, 미술 후보/reference-only, 실행코드 수정 없음.

2026-09-30 D-EQ-01: v10 장비 상세 한 종 최종 미술 승인 등록(manifest/index/REVIEW); 버리기 추가 확인 제외 유지. 다음 equipment-merchant-states-v11-illustration/index.html 통짜 상인 상태2종 제작. 적용 source: v07 merchantPoor/merchantFull 및 승인 v09 merchant.png, 계약 direction.equipment-merchant-states.illustration-v11.20260930. built-in image_gen·상품3/가격8·16·32/재고1/보급5·20/30 또는 보급40·30/30/비활성구매3개·상태문구 직접 육안 확인. manifest/REVIEW/PROMPTS 연결. 신규2종은 미술 승인 대기/reference-only. 독립자산·실기·원구매 연결 미완료로 D-EQ-01 유지, Unity 후속 AND 미충족·신규등록 없음.
D-EQ-01 2026-09-30 착수: v11 보급 부족 최종승인, v12 상인5종/보관함정리모달 통짜후보 제작. 신규수리·포션 수치미확정, P-EQ-MERCHANT-02 기획큐등록. 승인미술유지·런타임수정없음.

2026-09-30 D-EQ-01 v12 저장 완료: [상인5종·보관함모달 비교](../../QA/DirectionImpact-20260921/equipment-merchant-storage-v12-illustration/index.html), [보관함 클릭 진입](../../QA/DirectionImpact-20260921/equipment-merchant-storage-v12-illustration/merchant.html), manifest/REVIEW/PROMPTS. 원화2종 직접 육안 확인: 장비3종/열차수리/회복포션·보관함진입·가격미정, 동일상인암전·장비9칸·잠금·상세·닫기. HTML 이미지간 보관함/닫기 경로 제공(실제 거래/장비입력은 없음). 신규미술승인/신상품가격·효과·재고/독립자산·실기/Server·Unity 잔여, 기존큐유지. Plan P-EQ-MERCHANT-02 단일등록 확인. v11 보급부족 최종미술승인.
2026-09-30 D-EQ-01 최종 미술 승인: equipment-merchant-storage-v12-illustration 상인5종·상인 보관함정리 모달2종 채택. manifest/index/REVIEW 승인 등록, 원화 보존. 사용자 추가 상세기획 요구를 기존 P-EQ-MERCHANT-02에 보완: 열차수리 가격/수리량; 회복포션은 요원 전체 일정%회복 및 가격. 비율/가격 수치는 미확정, 부활 정책 임의 확정 없음. 신규 미술 승인 대기만 해소, 독립자산·실기/거래·신상품정책 및 Server/Unity 소비 잔여로 D-EQ-01 유지, 후속 선행 완료 승격 없음.
2026-09-30 D-EQ-01 후속 착수: enchant-followups-v13-illustration 인챈트 장비카드 선택 전확정/보관함공간부족 확정후 통짜2종. 승인 v09 3카드구도·건너뛰기 미술 재사용, 신규수치없음. 13 v18/D3 원결과 보존상 선택권 commit 후 원장비대기이며 다른카드 재선택/원선택 건너뛰기 아님. 현행 v07/v08 offerOverflow의 항상건너뛰기 노출은 시안 연결 충돌로 WAIT-004 기록; 런타임 임의변경 없음. 미술 후보/reference-only.
2026-09-30 D-EQ-01 인챈트 후속2종 저장: [v13 선택/공간부족 통짜원화](../../QA/DirectionImpact-20260921/enchant-followups-v13-illustration/index.html), manifest/REVIEW/PROMPTS/qa.json. 적용 direction.enchant-followups.illustration-v13.20260930. built-in image_gen, 직접 육안 확인: 원3카드·선택장비1성·확정전 획득/건너뛰기; 확정후 30/30·원장비1개·수령대기보기·대안카드 비활성·건너뛰기0. PNG/갤러리참조6검사·JSON상태계약 PASS. 신규미술승인·독립자산·실기·원거래연결 잔여, 기존큐유지·타담당후속 조기등록 없음. v07/v08 overflow 건너뛰기 연결 차이는 기존 WAIT-004 기록 유지.
2026-09-30 D-EQ-01: 사용자 v13 인챈트 선택/공간부족2종 승인, 공간부족에서 직접 보관함 진입 요청. v14는 버튼 보관함열기→동일 인챈트host 암전 위 작전장비 모달(원선택장비1개 대기)로 수정. 정리 후 원장비 카드 직접수령·자동수령0·닫기시보상포기0·미해결시같은원결과유지. v13 승인원본 보존, 새모달문맥원화는검토후보. 독립자산/실기/실거래별도, 기존큐유지.
2026-09-30 D-EQ-01 v14 보관함직접진입 저장: [수정/팝업2종](../../QA/DirectionImpact-20260921/enchant-storage-v14-illustration/index.html), [열기/닫기원화연결](../../QA/DirectionImpact-20260921/enchant-storage-v14-illustration/overflow.html), manifest/REVIEW/PROMPTS/qa.json. built-in image_gen으로 기존 공간부족버튼 보관함열기 수정 및 동일인챈트host장비모달(대기품1개) 조합. 원화 육안/PNG/링크/JSON상태확인 PASS, 신규모달문맥미술/독립자산/기기/실거래 잔여. v13 사용자승인등록, 큐유지·후속조기등록없음.
2026-09-30 D-EQ-01: v14 보관함직접진입/정리모달2종 사용자 최종미술승인 등록. 다음 미제작통짜상태 v15 offerGear3(각장비1개씩 3카드)/offerChanged(원승급카드유지·현재대상부적격) 제작착수. 원3카드·원결과 유지·재추첨0·신규수치없음, 미술후보/reference-only. 독립자산/실기/원거래 잔여로기존큐유지.
2026-09-30 D-EQ-01 v15通짜2종 저장: [장비3카드/생성후부적격](../../QA/DirectionImpact-20260921/enchant-offer-states-v15-illustration/index.html), manifest/REVIEW/PROMPTS/qa.json. 계약 direction.enchant-offer-states.illustration-v15.20260930. built-in image_gen, 카드당1장비/별1·2·3/배지중앙 및 원승급카드유지·비활성·현재대상부적격·건너뛰기 직접육안확인, PNG/갤러리6참조·상태JSON PASS. 신규미술승인대기/reference-only, 독립자산·실기·Server/Unity원결과연결 잔여, D-EQ-01유지·후속조기등록0. v14보관함열기/팝업2종은최종승인등록완료.
2026-09-30 WAIT-004 사용자정정: 승급원카드의현재대상부적격표시는잘못된흐름으로 v15 target-ineligible.png를검토/연결에서제외·역사보존. 승급원카드클릭→대상선택모달에서보관함 AND 요원/포탑장착중품목을표시→부적격품목각각에사유·선택불가표시→적격품목선택→원승급명령. 현재장비1개의부적격이원카드진입차단사유가아님. v16신규시안제작, 13 원카드/재검증문구와 v07/v08 offerChanged 노드카드비활성구현은소비자수정필요. 원선택권소비는최종성공시, 취소/닫기0소모·원카드재추첨0. 신규수치정책추가없음, 실제계약인수/실기별도.
2026-09-30 D-EQ-01 v16 수정시안저장: [승급→대상장비선택](../../QA/DirectionImpact-20260921/enchant-promote-target-v16-illustration/index.html), [카드진입 미리보기](../../QA/DirectionImpact-20260921/enchant-promote-target-v16-illustration/node.html), manifest/REVIEW/PROMPTS/qa.json. 일반승급1~2성대상예시를14원구간과대조하여적격1/2성과최대5성장비별부적격표시. 보관함/요원·포탑장착중품목함께표시·취소0소모·원장착슬롯유지. built-in image_gen 신규모달후보, v09노드승인원화재사용. 원v15노드카드부적격그림제외확인·원본보존. 육안·PNG/링크13검사·상태JSON PASS. 신규미술승인/독립자산/실기/Server·Unity원거래연결잔여로기존큐유지.
2026-09-30 D-EQ-01 사용자정정: 승급대상선택은목록하나+카테고리 전체/보관함/장착중으로필터. 장착품에는요원장착/포탑장착배지, 품목별부적격사유유지. 보관함/장착중독립2목록은 v16이력으로보존하고 v17통합목록후보제작. 장착상태가승급차단사유아님, 원itemID/owner/slot유지·중복instance0·필터선택소비0. 최신UI연결을WAIT-004소비자가인수, 런타임임의변경없음.
2026-09-30 D-EQ-01 v17수정후보저장: [카테고리+단일승급목록](../../QA/DirectionImpact-20260921/enchant-promote-unified-v17-illustration/index.html), manifest/REVIEW/PROMPTS/qa.json. direction.enchant-promote-unified.illustration-v17.20260930. built-in image_gen 편집, 전체/보관함/장착중카테고리·단일리스트/스크롤·품목별요원/포탑장착배지·부적격사유·원세트문양육안확인. PNG/갤러리/열기닫기참조12검사·JSON단일목록계약PASS. v16분리목록역사보존. 신규미술승인·실제필터/실기·독립자산·Server/Unity원장비ID/slot거래연결잔여로큐유지.
2026-09-30 D-EQ-01: 통합승급목록v17 사용자최종미술승인등록. 다음v18 인챈트대상선택모달은승인카테고리/단일리스트/장착배지구조재사용. 실제잠금·효과풀적격성으로품목판정, 최대성급승급부적격을시술에복제하지않음. 기존인챈트보유는교체후보표시(무조건부적격아님), 새효과는D4승인실행후추첨공개·교체손실공통확인재사용. 미술후보/reference-only·신규수치없음·실제실행없음, 기존큐유지.
2026-09-30 D-EQ-01: v17 단일 승급 목록 최종 미술 승인 등록. 다음 [인챈트 대상 선택 v18](../../QA/DirectionImpact-20260921/enchant-target-v18-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/enchant-target-v18-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/enchant-target-v18-illustration/manifest.json) / [QA](../../QA/DirectionImpact-20260921/enchant-target-v18-illustration/qa.json). 계약 direction.enchant-target.illustration-v18.20260930. built-in image_gen으로 동일 단일목록/필터·장착배지/잠금/기존인챈트보유·최대성급활성·성공후효과공개 원화 제작, 육안 및 PNG/HTML참조12검사 PASS. v18 신규미술승인 대기/reference-only. 기존효과교체는 승인 공통확인 재사용·실제효과 바인딩 필요. 독립자산·실기·원결과/Server·Unity거래 잔여로 D-EQ-01 유지; 후속등록 선행 완료로 승격하지 않음.

2026-09-30 D-EQ-01 v18 최종 미술 승인 등록: [인챈트 대상 장비 선택](../../QA/DirectionImpact-20260921/enchant-target-v18-illustration/index.html). 계약 direction.enchant-target.illustration-v18.20260930. manifest/index/REVIEW/qa 승인 동기화, 기존 PNG·12참조검수 근거 보존. 신규 미술 승인 대기 해소. 독립 자산·실기·Server/Unity 원거래 및 기존 효과 교체 연결 잔여로 D-EQ-01 유지, 후속 선행 완료 승격 없음.

2026-09-30 D-EQ-01 v19 인챈트 성공 결과 후보 저장: [통짜 원화](../../QA/DirectionImpact-20260921/enchant-result-v19-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/enchant-result-v19-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/enchant-result-v19-illustration/manifest.json). 계약 direction.enchant-result.illustration-v19.20260930. built-in image_gen, 2성 강화장갑·EN01 선제방벽 5%/6초 승인표 fixture, 원시술 성공후공개·확인1개·원노드해결상태복귀. 신규미술승인대기/reference-only. 실제 추첨 보장 없음. 원화 육안/PNG·페이지 참조 검수 PASS, 독립자산·기기·원영수증/소비host·실거래연결 미완료로 D-EQ-01 유지. 타담당 후속 조기등록 없음.

2026-09-30 D-EQ-01: v19 인챈트 완료 최종 미술 승인 등록(manifest/index/REVIEW/qa), 미술승인대기 해소. 다음 [승급 완료 v20 통짜 후보](../../QA/DirectionImpact-20260921/enchant-promote-result-v20-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/enchant-promote-result-v20-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/enchant-promote-result-v20-illustration/manifest.json). 계약 direction.enchant-promote-result.illustration-v20.20260930. built-in image_gen 승인 v19 재질 참조 편집, 일반 승급 동일 강화장갑2→3성·확인1개·원노드 해결상태 복귀. 육안 및 PNG/HTML4검사 PASS. 신규 v20 미술승인대기/reference-only, 독립자산·실기·원영수증/장착슬롯·Server/Unity 거래 연결 잔여로 D-EQ-01 유지. 후속 선행 미충족·조기등록 없음.

2026-09-30 D-EQ-01: v20 승급 완료 사용자 최종 미술 승인 등록. 다음 [인챈트 교체 공통확인 v21](../../QA/DirectionImpact-20260921/enchant-replace-confirm-v21-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/enchant-replace-confirm-v21-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/enchant-replace-confirm-v21-illustration/manifest.json). 계약 direction.enchant-replace-confirm.illustration-v21.20260930. 승인 공통2버튼팝업 미술을 built-in image_gen으로 원대상목록host 위 조합. 기존EN01 2성5%/6초 예시·새효과시술후공개·취소/인챈트·기존효과보유장비선택 육안확인, PNG/HTML4참조 PASS. v21 신규미술승인대기/reference-only. 전용prefab/중복통신구현 요구 없음, 매개변수/기존Controller콜백 재사용. 독립자산·실기·원장비revision/영수증/Server·Unity 거래 잔여로 D-EQ-01 유지, 후속 조기등록 없음.

2026-09-30 D-EQ-01: v21 인챈트 교체확인 최종미술승인 등록. 다음 [상인 구매완료·품절 v22](../../QA/DirectionImpact-20260921/equipment-merchant-soldout-v22-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-merchant-soldout-v22-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/equipment-merchant-soldout-v22-illustration/manifest.json). 계약 direction.equipment-merchant-soldout.illustration-v22.20260930. built-in image_gen 승인 v12 편집; 강화장갑만품절/잔액32/보관함21/30/다른장비재고1/5상품·버튼 구매/이용 육안확인 및 PNG/HTML4참조 PASS. 최신 Plan45#merchant-recovery-approved-v17-20260930 수리20·20%/포션24·생존각25% 반영(가격미정표현 현행후보에서 제외). 기존 v12승인 미술 보존, 최신 수치 소비 업데이트이며 실거래완료 아님. v22 신규미술승인대기/reference-only. 서비스 구매 전 실회복대상/량 바인딩은 잔여; 독립자산·실기·원재고/HP영수증 및 Server·Unity 거래 검증 미완료로 D-EQ-01 유지, 후속조기등록0.

2026-09-30 D-EQ-01: v22 상인품절 최신5상품 원화 최종승인 등록. 다음 [열차 수리 선택 v23](../../QA/DirectionImpact-20260921/equipment-merchant-repair-v23-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-merchant-repair-v23-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/equipment-merchant-repair-v23-illustration/manifest.json). 계약 direction.equipment-merchant-repair.illustration-v23.20260930, built-in image_gen. 승인45v17 예시650/1000→850/1000·+200·20보급·취소/수리·같은상인host 공통2버튼 구성 육안확인 및 PNG/HTML4검사·산술 PASS. 바 채움은 시안참고이며 실제65/20/15% 정확한fill연결 잔여. 신규v23미술승인대기/reference-only, 독립자산/실기/quote·HP기준쌍·Server/Unity원거래 잔여로 D-EQ-01 유지·후속조기등록 없음.

2026-09-30 D-EQ-01: v23열차수리선택 최종미술승인 등록. 다음 [회복 포션 선택 v24](../../QA/DirectionImpact-20260921/equipment-merchant-potion-v24-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-merchant-potion-v24-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/equipment-merchant-potion-v24-illustration/manifest.json). 계약 direction.equipment-merchant-potion.illustration-v24.20260930. built-in image_gen 공통2버튼팝업·동일상인host·5요원개별HP·전투불능제외·24보급·취소/구매 원화 육안 PASS, PNG/HTML4참조 및5명회복량[250,50,0,0,225]·합525·잔액32→8산술 PASS. 초상화는operator-cutouts source-reference/요원1~5 fixture이며실출격ID대응완료아님. 신규v24미술승인대기/reference-only, 독립자산·ID/HP·실기·원wallet/stock/HP영수증·협동소비권한 검증 잔여로D-EQ-01유지·후속조기등록없음.

2026-09-30 D-EQ-01: v24회복포션선택 최종미술승인 등록. 다음 [포션사용후상인 v25](../../QA/DirectionImpact-20260921/equipment-merchant-service-used-v25-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-merchant-service-used-v25-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/equipment-merchant-service-used-v25-illustration/manifest.json). 계약 direction.equipment-merchant-service-used.illustration-v25.20260930, built-in image_gen. 원성공후잔액32→8·칸21/30유지·포션사용완료·열차서비스미소비·품절/구매/구매/이용/사용완료 비활성버튼 원화육안 PASS, PNG/HTML4참조·잔액/원재고·칸무변화 PASS. 추가완료팝업없음·원host/스크롤유지. 신규v25미술승인대기/reference-only, 독립자산·실기·원HP/wallet/stock영수증·Server/Unity거래 잔여로D-EQ-01유지·후속조기등록없음.

2026-09-30 D-EQ-01: v25포션사용후상인 최종미술승인 등록. 다음 [상인 할인 v26](../../QA/DirectionImpact-20260921/equipment-merchant-discount-v26-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-merchant-discount-v26-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/equipment-merchant-discount-v26-illustration/manifest.json). 계약 direction.equipment-merchant-discount.illustration-v26.20260930, built-in image_gen. 원5상품/취소선원가/13%현재견적·7/14/28/18/21·구매/구매/구매/이용/구매 활성·잔액40·칸20/30 원화육안 및 PNG/HTML4참조·ceil산술 PASS. DO03/X08서비스할인·장비서비스성공한도공유 승인45v17적용,5견적동시노출은5회할인구매보장아님. 거래후원남은횟수/견적갱신 명세. 신규v26미술승인대기/reference-only,독립자산·실기·원quote/counter/wallet/stock/HP영수증 잔여로D-EQ-01유지·후속조기등록없음.

2026-10-01 D-EQ-01: v26상인할인 최종미술승인 등록. 다음 [보관함가득찬상인 v27](../../QA/DirectionImpact-20260921/equipment-merchant-full-v27-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-merchant-full-v27-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/equipment-merchant-full-v27-illustration/manifest.json). 계약 direction.equipment-merchant-full.illustration-v27.20261001, built-in image_gen. 잔액40·칸30/30활성정리진입·재고1장비3구매비활성·즉시수리20/포션24활성·5행이름/아이콘·가격8/16/32/20/24 육안 및 PNG/HTML4참조·상태계약 PASS. 승인45v17칸미점유/13용량정책소비,원v12역사보존. 신규v27미술승인대기/reference-only,독립자산·실기·원capacity/quote/HP영수증·Server/Unity입력검증 잔여로D-EQ-01유지·후속조기등록없음.

2026-10-01 D-EQ-01: v27보관함가득찬상인 최종미술승인 등록. 다음 [상인회복불필요 v28](../../QA/DirectionImpact-20260921/equipment-merchant-no-healing-v28-illustration/index.html) / [REVIEW](../../QA/DirectionImpact-20260921/equipment-merchant-no-healing-v28-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/equipment-merchant-no-healing-v28-illustration/manifest.json). 계약 direction.equipment-merchant-no-healing.illustration-v28.20261001, built-in image_gen. 장비3구매활성/수리·포션회복0만비활성/최대HP·모두최대HP사유/가격8·16·32·20·24/잔액40·칸20/30 육안 및 PNG/HTML4참조·적격분리 PASS. 승인45v17회복0무차감/무재고소모 재사용,미사용재고는유지·품절과구별. 신규v28미술승인대기/reference-only,독립자산·실기·실HP적격검사/원quote·Server/Unity거래 잔여로D-EQ-01유지·후속조기등록없음.


2026-10-01 D-EQ-01: v28회복불필요상인 최종미술승인 등록. 다음 equipment-inventory-empty-v29-illustration/index.html · manifest/REVIEW/PROMPTS/qa.json. 계약 direction.equipment-inventory-empty.illustration-v29.20261001. built-in image_gen, 빈보관함0/30·9칸viewport·내부스크롤·보상버튼없음 육안PASS. 실제30슬롯/30~54용량·장착품제외 원데이터 연결, 독립자산·실기·통합검증 잔여. 신규v29는미술검토후보/reference-only, D-EQ-01유지·후속조기등록없음.


2026-10-01 D-EQ-01: v29빈보관함 최종미술승인 등록. 다음 [확장54/54시안 v30](../../QA/DirectionImpact-20260921/equipment-inventory-expanded-v30-illustration/index.html), manifest/REVIEW/PROMPTS/qa.json. 계약 direction.equipment-inventory-expanded.illustration-v30.20261001; 승인13v18의30+6×DO05랭크/최대54·장착품제외. built-in image_gen,9카드+다음행clipping/내부스크롤/54/54/잠금/보상버튼없음 육안PASS. 신규v30검토후보/reference-only,독립자산·실기·실용량/목록/입력 연결 잔여로D-EQ-01유지·후속조기등록없음.


2026-10-01 D-EQ-01: v30확장54/54보관함 최종미술승인 등록. 다음 [54/54·수령대기1개 v31](../../QA/DirectionImpact-20260921/equipment-inventory-pending-v31-illustration/index.html), manifest/REVIEW/PROMPTS/qa.json. 계약 direction.equipment-inventory-pending.illustration-v31.20261001. built-in image_gen,54/54·미소유원보상1개/잠금없음·보관품잠금·6카드+다음행clipping·내부스크롤·수령포기 육안PASS. 공간확보54→53후직접수령53→54/자동수령0/원보상·scroll보존은연결명세이며실검증아님. 신규v31미술후보/reference-only,독립자산·실기·실거래잔여로D-EQ-01유지·후속조기등록없음.


2026-10-01 D-EQ-01: v31확장54/54·수령대기1개 최종미술승인 등록. 다음 [공간확보53/54 v32](../../QA/DirectionImpact-20260921/equipment-inventory-cleared-v32-illustration/index.html), manifest/REVIEW/PROMPTS/qa.json. 계약 direction.equipment-inventory-cleared.illustration-v32.20261001. built-in image_gen,53/54·빈칸1·원보상유지/수령가능외곽선·기존잠금품보존·동일scroll육안PASS. 위치는구성fixture/정렬정책변경없음. 자동수령0·직접claim53→54·원영수증/scroll보존은연결명세로실검증별도. 신규v32검토후보/reference-only,독립자산·실기·실거래잔여로D-EQ-01유지·후속조기등록없음.


2026-10-01 D-EQ-01 사용자 정정: 보관함 정리 후 중간빈칸 금지, 보유품 기존순서를앞으로당기고 빈칸은목록끝. [v33수정후보](../../QA/DirectionImpact-20260921/equipment-inventory-packed-v33-illustration/index.html), manifest/REVIEW/PROMPTS. v32미승인역사보존/현행제외.53/54의빈칸은54번째·현재viewport밖,scrollTop보존/자동아래이동0/원보상·직접수령유지. 기존v08 view.js38행보유품후빈칸·74행scroll복구구조 확인,실기완료아님. built-in image_gen 원화육안PASS·새미술승인대기. Plan/Unity명세소비필요를WAIT-004에기록,타담당코드/원문변경없음. 독립자산·실기·거래잔여로D-EQ-01유지.


2026-10-01 D-EQ-01: v33빈칸하단배치 최종미술승인 등록. 다음 [마지막보상수령완료 v34](../../QA/DirectionImpact-20260921/equipment-inventory-claimed-v34-review/index.html) / manifest·REVIEW·qa·verify.cjs. 별도새원화없이승인v30일반54/54보관함을동일복사재사용, 신규미술승인요청아님/상태연결검토. Direction v07/v08 view.js의pending>0조건으로빈수령완료plate제거. 19검사PASS:대기상태영역/포기유지·마지막claim후두영역제거·scrollTop보존·승인PNGbyte동일성. 초기검증의문자열검사가full상태대기확인버튼까지잡아패널검사로정정후PASS. 실기/Server·Unity거래/원아이템ID연결은NOT RUN, 독립자산잔여로D-EQ-01유지·후속AND미충족/조기등록없음. UI구성충돌은WAIT-004에기록.


2026-10-01 D-EQ-01: 사용자다음최종시안지시에따라 [보관함합성고퀄 v35](../../QA/DirectionImpact-20260921/equipment-synthesis-v35-illustration/index.html) / manifest·REVIEW·PROMPTS·qa.json 제작. 계약 direction.equipment-synthesis.illustration-v35.20261001; 승인v02구성/삭제문구유지·13 uniform-base-stats-v06 DEF25→60/최고5성적용. built-in image_gen 원화육안PASS:2+2→3/主철벽·보조화력/인챈트없음/신규고유I1개미추첨공개/취소합성X/수수료·삭제문구없음. 기존v02의6.3→13.23은신규제작제외·역사보존,장착품합성후속수치갱신필요. 모달뒤배경은동일host/별도페이지아님. 신규v35미술승인대기/reference-only,독립자산·실기·원합성receipt연결잔여로D-EQ-01유지/후속조기등록없음.


2026-10-01 D-EQ-01: v35보관함합성 최종미술승인 등록. 다음 [장착주재료+보관함합성 v36](../../QA/DirectionImpact-20260921/equipment-synthesis-equipped-v36-illustration/index.html), manifest/REVIEW/PROMPTS/qa. 계약 direction.equipment-synthesis-equipped.illustration-v36.20261001. built-in image_gen, 승인v02구성/삭제문구·승인v35미술재사용, 최신13 DEF25→60/최고5/반복EN02최종피해기준반영. 육안PASS:2+2→3/타워1슬롯1유지/주철벽·EN06시술9%6초승계/보조화력·EN02최종피해25%소멸/효과추첨전종류미공개/취소합성X. 구6.3→13.23·첫공격·ATK25분모는역사보존/신규제외. 모달뒤host별도페이지연결금지. 신규v36미술승인대기/reference-only;독립자산·실기·원슬롯/효과지원·실합성receipt잔여로D-EQ-01유지·후속AND미충족.


2026-10-01 D-EQ-01: v36장착합성 최종미술승인 등록. 다음 [합성성공결과 v37](../../QA/DirectionImpact-20260921/equipment-synthesis-result-v37-illustration/index.html), manifest/REVIEW/PROMPTS/qa. 계약 direction.equipment-synthesis-result.illustration-v37.20261001. 승인공통1버튼결과프레임·v36결과원화재사용/built-in image_gen. 육안PASS:확정3성·철벽·DEF60·EQ14제어저항I2%예시·EN06시술9%6초·원타워1슬롯1·확인하나. 실결과effectID/등급/값은원영수증입력이며EQ14항상보장아님. 확인은닫기만/재합성·재수령·재추첨0,미확인결과성공표시금지. 같은모달로보관함/요원/타워위치및인챈트유무입력,뒤host별도페이지금지. 신규v37미술승인대기/reference-only,독립자산·실기·원receipt/handler지원잔여로D-EQ-01유지·후속조기등록없음.


2026-10-01 D-EQ-01: v37합성성공결과 최종미술승인 등록. 다음 [드래그적격/부적격표시 v38](../../QA/DirectionImpact-20260921/equipment-drag-targets-v38-illustration/index.html), manifest/REVIEW/PROMPTS/qa. 계약 direction.equipment-drag-targets.illustration-v38.20261001. built-in image_gen, 승인v03고스트식별·v30미술·v33빈칸하단/13최고5반영. 육안PASS:원본이동중/고스트강화장갑2성/적격타겟2성→3미리보기/잠금/성급1불일치/최고5/다른종류3개표시·8/30·빈칸끝·화살표없음. 보관함부분상태검토로선택요원/타워3슬롯/출격5초상기존계약유지. 부적격드롭무소모/고스트복구/자동교체전환0/예고만승급0,실거래는v35미리보기확정후. 신규v38미술승인대기/reference-only,독립자산·실기스크롤/손가락/입력/원receipt잔여로D-EQ-01유지·후속조기등록없음.

### 2026-10-01 — D-EQ-01 v38 승인 / v39 장착 장비 상세 후보
- 사용자 승인: QA/DirectionImpact-20260921/equipment-drag-targets-v38-illustration/ 최종 승인 미술 등록. manifest/qa/REVIEW 저장 확인.
- 신규 후보: QA/DirectionImpact-20260921/equipment-equipped-detail-v39-illustration/index.html (equipped-detail.png, manifest/REVIEW/PROMPTS/qa).
- 계약: 13 최신 DEF3=60%, EQ14=적격 제어 지속시간2%, EN06 기록계수9%/6초; approved equipment-click-v01 / equipment-replace-v02 및 v37 미술 재사용.
- 장착품 클릭 상세 → 교체 후보 → 기존 비교. 해제는 기존 보관함 용량 검사(+1칸) 필요. 원 호스트/선택/스크롤 복원, 프레임·배경은 별도 화면 연결 대상 아님.
- 검증: PNG signature/JSON/문서 참조/3성60% 및 효과 fixture/이전 승인 저장 PASS; 생성 원화 육안 검수 PASS. 실제 모바일·거래 QA NOT RUN.
- 잔여: v39 미술 승인·독립 자산 조합·실기·실제 계약 연결. D-EQ-01 전체 완료 및 AND 선행 충족 아님, 큐 삭제/Unity 후속 선등록 없음.

### 2026-10-01 — D-EQ-01 v39 최종 미술 승인 / v40 교체 비교 후보
- 승인: QA/DirectionImpact-20260921/equipment-equipped-detail-v39-illustration/ (manifest/REVIEW/qa/index 최종 승인 저장 확인).
- 후보: QA/DirectionImpact-20260921/equipment-replace-v40-illustration/index.html; replacement.png, manifest/REVIEW/PROMPTS/qa 연결.
- 적용 계약: equipment-replace-v02 동일 태그 최종 승인 구성 + 13 최신2성 DEF/ATK25%; 원 장착자 실제3슬롯 세트 유지. 비교 증감은 옵션기여−25/+25%p, 최종 능력치 아님. 도움말/기존 장비 이동 안내 삭제 유지.
- 검증: 원화 육안 PASS, PNG/JSON/문서 연결/fixture/이전 승인 저장 PASS. 실제 모바일/거래 QA NOT RUN.
- 잔여: v40 미술 승인, 독립 자산 조합, 실제 권한/revision/영수증 연결, 모바일 본문/터치. D-EQ-01 전체 완료 아님, AND 후속 선등록·큐 삭제 없음. Unity/Server 수정 없음.

### 2026-10-01 — D-EQ-01 v40 승인 / v41 세트 변경 비교 후보
- v40 동일 태그 교체 최종 미술 승인 등록: QA/DirectionImpact-20260921/equipment-replace-v40-illustration/.
- 다음 후보: QA/DirectionImpact-20260921/equipment-set-change-v41-illustration/index.html (set-change.png/manifest/REVIEW/PROMPTS/qa).
- 계약: 기존 equipment-replace-v02/02 승인 구성 + 13 §5 SET01/04 및 최신2성 DEF/ATK25%. 실제 장착자3슬롯 철벽2화력1→철벽1화력2. 기본 기여−25/+25%p와 세트 해제10%·유지6%·활성6%·유지4% 분리.
- 원화 육안 PASS, 저장 PNG/JSON/참조/fixture/이전 승인 PASS. 실제 모바일/거래 NOT RUN. 후보 미술 승인·독립자산·실제 계산/권한/revision·모바일 QA 잔여.
- D-EQ-01 전체 완료 및 AND 후속 충족 아님. 큐 삭제/후속 선등록 없음, Unity/Server 수정 없음.

### 2026-10-01 — D-EQ-01 v41 최종 승인 / v42 빈 슬롯 선택 후보
- v41 최종 승인 등록: QA/DirectionImpact-20260921/equipment-set-change-v41-illustration/.
- 다음 후보: QA/DirectionImpact-20260921/equipment-empty-slot-v42-illustration/index.html; empty-slot.png/manifest/REVIEW/PROMPTS/qa 연결.
- 계약: equipment-click-v01/01 승인 흐름; 실제3슬롯·빈슬롯2선택·장비미선택시 장착비활성.6보관품 표시수는 용량정책 아님(30~54 유지). 실제5출격요원 초상/ID 계약 변경없음.
- 검증: 원화 육안/3슬롯/6종2별/중앙 세트배지/냉각 모래시계1개/불필요 SET삭제 PASS. PNG/JSON/참조/fixture/이전 승인 저장 PASS. 실제 모바일·거래 NOT RUN.
- 잔여: v42 미술 승인·독립 자산·실제 선택/장착 연결·실기. D-EQ-01 전체 완료 아님, 큐 삭제 및 AND 후속 선등록 없음. Unity/Server 수정 없음.

## D-MAINT-LOGIN-01 / 착수 가능 — Unity·홈페이지 점검 로그인 안내 시안
12#game-web-login-maintenance-20261001 사용자 제작 지시. 기존 승인 화면/미술 먼저 식별하고 Unity 로컬 진입차단 화면, 기존 홈페이지 로그인 영역 차단(PC/모바일), 종료미정/연장·조회실패·해제 재시도 상태 시안 제작. 제목/사유/일정/마지막확인/버튼·장식 독립, Unity 초기 로컬 의존성으로 구성 가능해야 함. 홈페이지 전체 리디자인/전체웹 차단으로 확대하지 않음. 원격이미지 실패에도 기본문구 표시. 기존 점검 시안 존재시 차이만 제작하고 원본 보존. 후보 미술·사용자 승인·독립자산·실연결 상태를 구분. Server 완료 전 시안 준비 가능, 최종 실제표시는 계약 대조. 완료 후 WAIT에서 Unity/Homepage 선행 AND 확인, 전체 게임 화면 연결 방침 유지.

### 2026-10-01 — D-EQ-01 v42 장착 장비 세트 배지 수정
- 사용자 지적 반영: equipment-empty-slot-v42-illustration/empty-slot-equipped-badge.png, index/manifest의 현행 참조 갱신. 기존 empty-slot.png는 배지 누락 이력으로 보존.
- 보관함뿐 아니라 모든 실제 장착 장비에도 좌상단 원형 세트 아이콘 표시, 원 안 중앙 정렬. 빈 슬롯에는 표시하지 않음. 예시 상단 보조 동력 철벽 배지 추가.
- 원화 육안 PASS: 상단 배지/기존6배지/빈슬롯/2별/비활성 상태 유지. PNG/참조/JSON PASS. 미술 승인 pending, 독립자산·모바일·실제 연결 잔여 유지.

### 2026-10-01 — D-EQ-01 v42 배지수정본 승인 / v43 후보선택 상태
- 승인: equipment-empty-slot-v42-illustration/empty-slot-equipped-badge.png 최종 미술 등록. 배지 누락 원본은 승인 대상 제외/이력 보존.
- 신규: QA/DirectionImpact-20260921/equipment-empty-slot-selected-v43-illustration/index.html; selected.png/manifest/REVIEW/PROMPTS/qa 연결.
- 기존 equipment-click-v01 선택 상태 통합:2성 철벽 강화 장갑 선택·DEF25·철벽1→2/DEF10활성/HP6유지. 슬롯2는 확정 전 빈칸 유지, 장착 버튼 활성. 선택만으로 실행/별도 준비페이지 없음.
- 육안/PNG/JSON/fixture/참조/이전 승인 PASS; 실제 모바일·거래 NOT RUN. 후보 승인·독립자산·실제 입력/revision/영수증·실기 잔여. D-EQ-01 전체 완료 및 AND 후속 충족 아님, 큐 삭제/선등록 없음.

### 2026-10-01 — D-EQ-01 v43 최종 승인 / v44 보관품 상세 후보
- v43 최종 미술 승인 등록: equipment-empty-slot-selected-v43-illustration/.
- 신규: QA/DirectionImpact-20260921/equipment-inventory-detail-v44-illustration/index.html; inventory-detail.png/manifest/REVIEW/PROMPTS/qa 연결.
- 기존 equipment-click-v01/02:조준 장치2성 화력 ATK25% 상세→실제5출격요원/타워선택→실3슬롯→장착 또는 비교. 대상미선택 비활성.
- 요원5실루엣은 원승인 구성의 초상자리, 실제 출발snapshot ID·승인 초상 연결 잔여(NOT CONNECTED). 타워3개는 예시, 고정 정책 아님. 신규 캐릭터/ID 생성 없음.
- 원화 육안·PNG/JSON/fixture/문서참조/이전 승인 PASS. 실제 모바일·거래 NOT RUN. 후보 승인·독립자산·실제 초상/ID·게임 연결/실기 잔여. D-EQ-01 전체 완료 및 AND 후속 충족 아님, 큐 삭제/선등록 없음.

### 2026-10-01 — D-EQ-01 v44 승인·타워 가로스크롤 / v45 대상슬롯 후보
- 승인: equipment-inventory-detail-v44-illustration/ 최종 미술 등록; 해금 객차/run 실제 타워목록2/4등 증가·카테고리만 가로스크롤·instanceID/가로위치유지 명세 추가.
- 후보: QA/DirectionImpact-20260921/equipment-recipient-slots-v45-illustration/index.html; recipient-slots.png/manifest/REVIEW/PROMPTS/qa 연결.
- 보관품조준장치2성ATK25 고정→타워1/빈슬롯2선택·장착활성,확정 전 빈슬롯유지. 실제3슬롯 및 장착품세트배지.3전체+4번째일부노출은 가로목록 예시,타워4개 고정정책 아님.5요원 초상/ID는 실제출발snapshot 연결자리.
- 육안·PNG/JSON/fixture/참조·이전승인 PASS; 실제 모바일·거래 NOT RUN. 스와이프/탭/드래그구분 및 가로/세로스크롤실기·미술승인·독립자산·실제연결 잔여. D-EQ-01 전체 완료/AND후속충족 아님,큐삭제·선등록 없음.

### 2026-10-01 — D-EQ-01 v45 승인 / v46 요원선택 슬롯 후보
- v45 최종미술 승인등록: equipment-recipient-slots-v45-illustration/.
- 후보: QA/DirectionImpact-20260921/equipment-operator-slots-v46-illustration/index.html; operator-slots.png/manifest/REVIEW/PROMPTS/qa 연결.
- 기존 보관품상세5요원선택→실제owner3슬롯. 첫요원만선택,타워해제,요원1빈슬롯2·장착활성. 타워슬롯을요원에게복제하는계약아님,대상변경시 실제슬롯갱신/원ID 및늦은응답차단 필요. 초상은실제출발snapshot 연결자리.
- 원화육안·PNG/JSON/fixture/참조/이전승인 PASS; 실제모바일·거래 NOT RUN·초상 NOT CONNECTED. 후보승인·독립조합·실제대상/초상/명령·실기잔여. D-EQ-01전체완료/AND후속충족아님,큐삭제·선등록없음.

### 2026-10-01 — D-EQ-01 v46 승인 / v47 해제 공간부족 후보
- v46 최종미술등록: equipment-operator-slots-v46-illustration/.
- 후보: QA/DirectionImpact-20260921/equipment-unequip-full-v47-illustration/index.html; unequip-full.png/manifest/REVIEW/PROMPTS/qa 연결.
- 기존 v05 unequip 상태와 v39 상세미술:미장착30/30이면해제+1불가·장착상태유지/해제비활성·보관함정리진입. 교체점유변화0원계약유지. 정리후자동해제없음,원owner/slot/scroll보존·실제용량30~54.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS; 실제모바일·거래 NOT RUN. 후보미술승인/독립조합/실제공간·권한·revision·영수증·복귀실기잔여. D-EQ-01전체완료·AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v47 승인 / v48 잠금보관품상세 후보
- v47 최종미술등록: equipment-unequip-full-v47-illustration/.
- 후보: QA/DirectionImpact-20260921/equipment-locked-detail-v48-illustration/index.html; locked-detail.png/manifest/REVIEW/PROMPTS/qa 연결.
- v10owned-detail 승인미술+13명시잠금계약. 잠금미장착1성화력예시:자물쇠/잠금중/버리기비활성/별도잠금해제. 해제는잠금만변경,자동버리기없음·버리기별도1입력추가확인없음. 잠금품장착/해제/이동허용원조건유지.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS; 실제모바일·거래 NOT RUN. 후보승인·독립조합·실제권한/revision/명령영수증·실기잔여. D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

## 연계 총괄 인수 — 2026-10-01
WAIT#coordination-unblock-20261001 C4/C5 적용. D-MAINT-LOGIN-01은 Server전체완료 없이 기존12 상태규칙으로 시안 준비를 계속한다. 홈페이지 로그인/문의에서 소비담당이 특정한 미승인영역만 기존승인페이지 기준으로 제작하고, 존재하는승인화면은 정확경로/버전으로 인계해 중복제작/대기 방지. 미확정서버표시값은 실제지원대조 잔여로 남기되 아트전체 작업을 막지 않는다. 기존진행미술/원본 보존,런타임편집0.

## 총괄 범위 정정 — 2026-10-01
시안 제작 진행은 사용자가 직접 지휘한다. 직전 연계총괄의 추가조정은 현재 지시로 대체하며 기존사용자 시안요청은 유지한다. 모든화면연결은 최종시안 전체 완료 후,Plan은다른코드/API 연계만총괄한다.

### 2026-10-01 — D-EQ-01 v48 승인 / v49 부분수령2개대기 후보
- v48 최종미술등록: equipment-locked-detail-v48-illustration/.
- 후보: QA/DirectionImpact-20260921/equipment-partial-pending-v49-illustration/index.html; partial-pending.png/manifest/REVIEW/PROMPTS/qa 연결.
- v31승인미술+v05partial원흐름:원3개중1수령/2대기·보관품30/30. 대기잠금없음/원결과유지/자동수령없음/직접탭1수령. 목록앞부터압축·빈칸끝·수령/버리기scrollTop유지. 냉각모래시계는SET03전술제어 예시,신규태그없음.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS; 실제모바일·거래 NOT RUN. 미술승인·독립조합·원결과/영수증·실기잔여. D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v49 승인 / v50 수령포기 공통확인 후보
- v49 최종미술등록: equipment-partial-pending-v49-illustration/.
- 후보: QA/DirectionImpact-20260921/equipment-forfeit-dialog-v50-illustration/index.html; forfeit-dialog.png/manifest/REVIEW/PROMPTS/qa 연결.
- common-dialog-v01 2버튼승인디자인+13D3 DeclineRemaining. title/body/content/buttons/callback매개변수재사용,확인한원미수령2item집합+bundle revision만포기. 집합변경확인무효·재확인/자동확대없음. 기존보관품/이미수령/영구보상불변,보관품버리기추가확인아님.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS; 실제모바일·거래 NOT RUN. 후보승인·독립공통조합·원집합/revision/영수증·실기잔여. D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v50 승인 / v51 장착품교체 후보목록
- v50 최종미술등록: equipment-forfeit-dialog-v50-illustration/.
- 후보: QA/DirectionImpact-20260921/equipment-replace-picker-v51-illustration/index.html; replace-picker.png/manifest/REVIEW/PROMPTS/qa 연결.
- equipment-click-v01 장착품클릭→후보→기존비교:슬롯2현재강화장갑유지/실3슬롯/6미선택후보,후보탭은비교만,실행은비교교체장착1회. 실제owner3슬롯계산(v41fixture결과자동복제없음). 모래시계SET03전술제어.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS; 실제모바일·거래 NOT RUN. 후보승인·독립조합·실제비교입력/revision/영수증·실기잔여. D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v51 승인 / v52 작전장비 기본 전체 후보
- v51 최종미술등록: equipment-replace-picker-v51-illustration/.
- 후보: QA/DirectionImpact-20260921/equipment-main-v52-illustration/index.html; equipment-main.png/manifest/REVIEW/PROMPTS/qa 연결.
- equipment-drag-v03 승인 기본창 구도 통합.5요원 초상자리/동적타워 가로목록/owner3슬롯/장착품 포함 배지/보관품6개6/30. idle 화살표·ghost·도움말 없음. 장착·교체·합성은 기존 입력계약. 시안fixture이며 실제30~54/출발snapshot/원ID 적용 필요.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS; 실제 모바일·거래 NOT RUN,초상 NOT CONNECTED. 후보승인·독립자산·실제 연결/실기잔여. D-EQ-01 전체완료/AND후속충족 아님,큐삭제/선등록 없음.

### 2026-10-01 — D-EQ-01 v52 승인 / v53 드래그장착 전체 후보
- v52 최종미술등록: equipment-main-v52-illustration/. 사용자 확인:5요원 자리 실제출발snapshot 초상화로 대체.
- 후보: QA/DirectionImpact-20260921/equipment-drag-equip-v53-illustration/index.html; drag-equip.png/manifest/REVIEW/PROMPTS/qa 연결.
- equipment-drag-v03 원계약을 v52 전체창에 조합:조준장치2성화력 원자리dim/ghost이름성급배지/타워1빈슬롯3blue호버,확정 전6/30·빈슬롯유지. 화살표없음,드롭기존장착명령1회·취소/무효drop원복·owner/scroll보존.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS; 실제 모바일·거래 NOT RUN,초상 NOT CONNECTED. 미술승인·독립조합·실입력/revision/영수증·가림/스크롤실기잔여. D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v53 승인 / v54 장착품 합성 드래그 후보
- v53 최종미술등록:equipment-drag-equip-v53-illustration/.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-drag-synthesis-v54-illustration/drag-synthesis.png ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-drag-synthesis-v54-illustration/index.html/manifest/REVIEW/PROMPTS/qa 연결.
- v52기본창+원드래그/v36합성흐름.보관강화장갑2성→타워1슬롯2장착강화장갑2성 호버.소스dim/ghost2성배지/합성가능/대상blue/빈3neutral/6/30유지.드롭은기존미리보기,확정영수증전에소모/성급변화없음.실owner/effect적용,v36fixture자동복제없음.
- 육안·PNG/JSON/링크/fixture/이전승인 PASS.모바일·실합성 NOT RUN,초상 NOT CONNECTED.후보승인·독립조합·실입력/revision/영수증·실기잔여.D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v54 승인 / v55 합성 후 기본창 갱신 후보
- v54 최종미술등록:equipment-drag-synthesis-v54-illustration/.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-synthesis-refresh-v55-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-synthesis-refresh-v55-illustration/synthesis-refresh.png/manifest/REVIEW/PROMPTS/qa 연결.
- 동일작전장비창갱신·별도성공페이지아님.타워1슬롯2강화장갑2→3/주태그유지,보관보조1소모6→5/5/30.남은순서앞당겨끝빈칸,owner/scroll유지,ghost해제.원영수증성공후만반영·실제효과/결과ID추정없음.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS.실제모바일·거래 NOT RUN/초상 NOT CONNECTED.후보승인·독립조합·실갱신/영수증/입력·실기잔여.D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v55 승인 / v56 보관품합성 완료갱신 후보
- v55 최종미술등록:equipment-synthesis-refresh-v55-illustration/.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-inventory-synthesis-refresh-v56-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-inventory-synthesis-refresh-v56-illustration/inventory-synthesis-refresh.png/manifest/REVIEW/PROMPTS/qa 연결.
- 기존v35보관품합성 완료후같은창갱신.보관강화장갑3성철벽,장착품2성그대로/자동장착없음.독립fixture보관7→6/30(v55와다른예시).원영수증후1점유감소·scroll/owner/배지유지·별도성공페이지없음.
- 육안·PNG/JSON/fixture/링크/이전승인 PASS.모바일·거래 NOT RUN/초상 NOT CONNECTED.후보승인·독립조합·실영수증/갱신·실기잔여.D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v56 승인 / v57 교체장착 갱신 후보
- v56 최종미술등록:equipment-inventory-synthesis-refresh-v56-illustration/.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-replacement-refresh-v57-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-replacement-refresh-v57-illustration/replacement-refresh.png/manifest/REVIEW/PROMPTS/qa 연결.
- 기존비교확정후같은창갱신.타워1슬롯2조준장치2성화력/기존강화장갑2성철벽보관함반환·6/30점유불변·서로다른장갑instance2개.태그실3슬롯철벽2화력0→철벽1화력1(v41fixture자동복제없음).표시순서는예시,실원정렬/owner/scroll보존.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS.모바일·거래 NOT RUN/초상 NOT CONNECTED.미술승인·독립조합·실swap영수증/갱신·실기잔여.D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v57 승인 / v58 빈슬롯장착 완료갱신 후보
- v57 최종미술등록:equipment-replacement-refresh-v57-illustration/.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-equip-refresh-v58-illustration/equip-refresh.png ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-equip-refresh-v58-illustration/index.html/manifest/REVIEW/PROMPTS/qa 연결.
- 기존v53드롭/클릭장착 원영수증후기존창갱신.조준장치2성화력→타워1슬롯3/장착3개·보관6→5/30·소모아닌이동.남은순서앞당겨끝빈칸·owner/scroll유지.실3슬롯철벽2화력1,실계산원계약.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS.모바일·거래 NOT RUN/초상 NOT CONNECTED.후보승인·독립조합·실장착/영수증갱신·실기잔여.D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v58 승인 / v59 장비해제 갱신 후보
- v58 최종미술등록:equipment-equip-refresh-v58-illustration/.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-unequip-refresh-v59-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-unequip-refresh-v59-illustration/unequip-refresh.png/manifest/REVIEW/PROMPTS/qa 연결.
- 기존해제원영수증후같은창갱신.타워1슬롯2빈칸/1·3원위치유지,강화장갑2성철벽반환·보관5→6/30(+1실공간필요).다른보관품/실instance/태그·성급유지,실owner세트수변화/scroll갱신.반환표시위치는fixture이며실정렬원계약.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS.모바일·거래 NOT RUN/초상 NOT CONNECTED.후보승인·독립조합·실해제/영수증/세트갱신·실기잔여.D-EQ-01전체완료/AND후속충족아님,큐삭제/선등록없음.

### 2026-10-01 — D-EQ-01 v59 사용자 최종미술승인
- equipment-unequip-refresh-v59-illustration/index.html/manifest/qa/REVIEW에 사용자최종승인 저장·재읽기확인.장비해제후기존창갱신채택.독립조합·실연결·모바일/거래잔여,D-EQ-01전체완료아님.

### 2026-10-01 — D-MAINT-LOGIN-01 게임진입점검 고퀄후보v01
- D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-maintenance-v01-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-maintenance-v01-illustration/login-maintenance.png/manifest/REVIEW/PROMPTS/qa 연결.승인로그인3번v02미술재사용,12§5/game-web-login-maintenance-20261001 기준.
- 확인된점검 인증전bootstrap전체화면:공개사유/일정KST/마지막확인/상태재확인/공지/타이틀.모든시각fixture,종료시간도달자동로그인없음·조회실패를점검확정으로추정없음·실저장성공주장없음.재확인single-flight/서버retryAfter,독립로컬자산연결잔여.
- 출력육안·PNG/JSON/문구fixture/참조 PASS.실bootstrap/API/360·430터치/원격실패 NOT RUN.신규미술승인·독립조합·종료미정/연장/조회실패/해제·웹PC모바일후보잔여.D-MAINT-LOGIN-01유지,AND조건전소비큐선등록없음.런타임·실점검변경0.

### 2026-10-01 — D-MAINT-LOGIN-01 v01 승인 / 종료미정v02 후보
- login-maintenance-v01-illustration/ 최종미술승인저장확인.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-maintenance-unknown-v02-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-maintenance-unknown-v02-illustration/maintenance-unknown.png/manifest/REVIEW/PROMPTS/qa 연결。
- 12§5.2/게임웹로그인점검계약:확인된점검·expectedEnd미제공 '종료 시간 미정'·추정시각/카운트다운없음.시작/마지막확인공개값(원화fixture),단일View데이터상태/재확인retryAfter/공지/타이틀계약유지。
- 육안·PNG/JSON/fixture/참조/이전승인 PASS.로컬bootstrap/API/360·430터치 NOT RUN.후보승인·독립조합·연장/조회실패/해제/웹후보·실계약/실기잔여.큐유지·AND조건전후속등록없음,런타임·운영점검변경0。

### 2026-10-01 — D-MAINT-LOGIN-01 종료미정v02 승인 / 연장v03 후보
- login-maintenance-unknown-v02-illustration/ 최종미술승인저장확인.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-maintenance-extension-v03-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-maintenance-extension-v03-illustration/maintenance-extension.png/manifest/REVIEW/PROMPTS/qa 연결.
- 확인된공개연장·새종료예정06:00/기존예정04:00보조(KST/03:50마지막확인모두fixture).과거행은같은점검이전성공공개snapshot근거있을때만,없으면숨김·새DTO/조회API요구아님.단일Viewdata/원bootstrap·재확인retryAfter·공지계약유지,예정시각자동로그인0.
- 육안·PNG/JSON/fixture/참조/이전승인 PASS.실API/로컬bootstrap/360·430터치 NOT RUN.후보미술승인·독립조합·실연장/과거근거·조회실패/해제/웹후보·실기잔여.D-MAINT-LOGIN-01유지·AND조건전후속선등록없음,런타임/운영변경0.

### 2026-10-01 — D-MAINT-LOGIN-01 연장v03 사용자문구정정
- '기존 예정' 문구 및 이전시각 표시/캐시조회요구 삭제.현행원화:QA/DirectionImpact-20260921/login-maintenance-extension-v03-illustration/maintenance-extension-current-only.png;index/manifest/REVIEW/qa참조갱신.
- 현재종료예정만표시.원이미지/검수는변경전미승인이력보존.직전과거시각보조행기획은본사용자수정으로대체.
- 생성원화육안·PNG/JSON/참조/previousEnd제거 PASS.신규미술승인pending/독립조합·실API/모바일잔여.런타임·운영변경0.

### 2026-10-01 — D-MAINT-LOGIN-01 연장v03수정본 승인 / 최초조회실패v04 후보
- login-maintenance-extension-v03-illustration/maintenance-extension-current-only.png만 최종미술승인;기존예정포함원본은승인제외이력.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-status-failure-v04-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-status-failure-v04-illustration/status-failure.png/manifest/REVIEW/PROMPTS/qa 연결.
- 최초조회실패/이전성공상태없음fixture.점검여부미확인·일정/마지막확인추정없음,단순503/통신실패를점검/정상으로단정0.기존status재확인singleflight/retryAfter/공지설정URL/안전타이틀·bootstrap 계약유지.새클라gate정책아님.
- 육안·PNG/JSON/unknownfixture/참조/이전승인 PASS.실통신/로컬bootstrap/360·430터치 NOT RUN.후보승인·독립조합·실오류/API/공지·기존확인값있는실패/해제/웹후보·실기잔여.큐유지·AND후속선등록없음,운영·런타임변경0.

### 2026-10-01 — D-MAINT-LOGIN-01 최초조회실패v04 승인 / 마지막정보유지v05 후보
- login-status-failure-v04-illustration/ 최종미술승인저장확인.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-status-stale-v05-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-status-stale-v05-illustration/status-stale.png/manifest/REVIEW/PROMPTS/qa 연결。
- 이전성공공개점검snapshot후재조회실패.마지막일정/성공확인03:50유지·갱신실패/갱신필요로최신아님표시.모든시각fixture,조회실패시각으로덮어쓰기/현재점검·해제확정0.기존publiccache/세대/retryAfter/공지계약,새영속cache/차단정책아님。
- 육안·PNG/JSON/stalefixture/링크/이전승인 PASS.실API/cache세대/로컬bootstrap/360·430터치 NOT RUN.후보미술승인·독립조합·실상태·해제/웹후보·실기잔여.큐유지·AND후속선등록없음,운영·런타임변경0。

### 2026-10-01 — D-MAINT-LOGIN-01 v05 승인 / 해제확인v06 후보
- login-status-stale-v05-illustration/ 최종미술승인저장확인.
- 후보:D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-maintenance-released-v06-illustration/index.html ; D:/Project/LASTRAIL/QA/DirectionImpact-20260921/login-maintenance-released-v06-illustration/maintenance-released.png/manifest/REVIEW/PROMPTS/qa 연결.
- 최신GAME점검해제/로그인허용확인fixture.로그인화면으로→기존선택화면,자동인증/이전명령재전송0.06:05KST시안값.예정시각자동허용없음·인증경합서버판정/세대유지·전서비스정상보장아님.
- 육안·PNG/JSON/fixture/링크/이전승인 PASS.실API/auth/bootstrap/360·430터치 NOT RUN.미술승인·독립조합·실인증복귀·웹후보/실기잔여.게임6상태원화후보만제작·전체완료아님,큐유지/AND후속선등록없음,운영·런타임변경0.

### 2026-10-01 — D-MAINT-LOGIN-01 해제확인v06 승인 / 홈페이지PC·모바일v07 후보

- 사용자 승인: QA/DirectionImpact-20260921/login-maintenance-released-v06-illustration 최종 미술 승인 등록. 기존 로그인 선택 복귀·자동 인증 없음 유지.
- 후보: QA/DirectionImpact-20260921/web-login-maintenance-v07-illustration/index.html, desktop.png/mobile.png 및 manifest/REVIEW/PROMPTS/qa. 12#game-web-login-maintenance-20261001·maintenance-login-v1 기준. QA/Homepage/HANDOFF.md 승인 키비주얼, 기존 /account 콘텐츠 상태; 공개 메뉴·공지 유지. 로그인 View 현재 미연결, 실제 구현 완료 아님.
- 출력 육안·PNG/JSON/fixture/참조/이전 승인 PASS. 실제 브라우저/실기/WEB API/쿠키 인증 NOT RUN. 신규 미술승인·독립 조합·웹 상태 변형·320~430px 및 실제 로그인 검수 잔여. 원본 홈페이지/코드 보존, 전체 큐 및 후속 AND 미완료. 타 담당 큐 수정·선등록0.

### 2026-10-01 — 홈페이지 전체 점검 사용자 변경 / v08 후보

- 사용자 확정 범위: 홈페이지 점검 중 다른 기능 사용 불가, 메뉴/헤더/햄버거/푸터/계정/공지 링크 모두 제거. 배경 원화+점검 안내+상태 다시 확인 하나만 유지. 로그인 전용 차단/공개 메뉴 유지의 v07은 이 웹 정책 신규 적용에서 제외하고 역사 보존. 미술 v08은 검토 후보.
- QA/DirectionImpact-20260921/web-maintenance-full-v08-illustration/index.html 및 desktop.png/mobile.png/manifest/REVIEW/PROMPTS/qa. 전체 페이지 게이트이며 팝업 아님; 깊은 URL/뒤로가기/로그인 사용자도 같은 웹 UI 게이트, 메뉴 숨김만으로 기능 차단 완료 아님.
- 12#game-web-login-maintenance-20261001·maintenance-login-v1은 기존 로그인 차단 계약이므로 신규 전체 웹 차단으로 임의 재해석하지 않는다. P-MAINT-WEB-FULL-01 Plan 정책 정합화 등록. 실제 API 범위/감지·해제/unknown 처리·소비 후속을 담당 원본에서 확정해야 함. 상태 재조회 경계·로컬 배경/안내는 게이트에서도 사용 가능; 세션 강제삭제/예정종료 자동해제 없음.
- PC·모바일 생성 출력 육안 및 PNG/JSON/fixture/참조 PASS. 실 브라우저/모바일/라우트·API 차단 NOT RUN. 미술승인·독립조합·정책정합/상태변형·실 연결 잔여. D-MAINT-LOGIN-01 유지, 타 담당 코드/기존 큐 수정·배포0.

### 2026-10-01 — 홈페이지 전체점검v08 승인 / 종료미정v09 후보

- 사용자 승인으로 web-maintenance-full-v08-illustration PC·모바일 최종 미술 승인 등록. 메뉴·푸터·다른 기능 링크 제거/상태 확인 하나 유지. 전체 웹 정책 변경 결정과 미술 승인 구분, 실제 연결 완료 아님.
- 후보 QA/DirectionImpact-20260921/web-maintenance-unknown-v09-illustration/index.html, desktop.png/mobile.png 및 manifest/REVIEW/PROMPTS/qa. 기존 승인 v08 배치로 종료 시간 미정 상태를 표시. confirmed 웹점검은 유지하며 종료일정 값만 null; 점검 여부 unknown/조회 실패 상태와 구분. 시작02:00/마지막확인02:15 KST는 예시.
- 종료 추정·카운트다운·예정시각 자동해제·새 공지 이동 없음. 기존 전체 페이지 게이트/로컬 배경/재조회 수명·서버 retryAfter 계약 재사용. P-MAINT-WEB-FULL-01 담당 원본의 전체 웹 정책 정합화 여부는 실제 연결 전에 확인.
- PC·모바일 육안/PNG·JSON·fixture·참조·이전승인 PASS. 실제 브라우저/기기/WEB API/세션·라우트 차단 NOT RUN. 신규 미술승인·독립조합·나머지 상태·실 소비 검수 잔여. D-MAINT-LOGIN-01 전체 완료/후속 조건 충족으로 처리하지 않음. 원본/역사/타 담당 코드·기존 큐 보존.

### 2026-10-01 — 웹 종료미정v09 승인 / 점검연장v10 후보

- 사용자 승인으로 web-maintenance-unknown-v09-illustration PC·모바일 최종 미술 승인 등록. 실제 전체 웹 게이트/독립 조합 완료 아님.
- 새 후보 QA/DirectionImpact-20260921/web-maintenance-extension-v10-illustration/index.html 및 desktop.png/mobile.png/manifest/REVIEW/PROMPTS/qa. 승인 전체 점검 배치에 연장 제목/안내 및 현재 종료 예정06:00·마지막 확인03:50 KST 예시. 기존 예정 비교 문구/과거 시각 행 없음, 메뉴/푸터/다른 링크 없음, 상태 확인 하나 유지.
- 실제 연장 확인 근거가 제공된 경우만 연장 표현 사용. 시각이 지났거나 종료 시각이 나중이라는 이유만으로 연장 추정 금지; 미연결 계약을 신규 필드로 임의 확정하지 않음. whole-web 정책은 기존 P-MAINT-WEB-FULL-01/WAIT의 담당 원본 정합화 확인 후 소비.
- 출력 육안·PNG/JSON/fixture/참조/이전승인 PASS. 실제 브라우저/기기/WEB API/상태·라우트 차단 NOT RUN. 신규 미술승인·독립조합·조회실패/갱신실패/해제 상태·실 연결 검수 잔여. D-MAINT-LOGIN-01 전체 미완료, 소비후속 선등록/타 담당 코드 수정/운영배포0.

### 2026-10-01 — 웹 연장v10 승인 / 최초조회실패v11 PC 승인·모바일 후보

- 직전 사용자 승인 v10 PC·모바일 최종 등록을 중단 후 마무리. 방금 사용자 승인한 생성 출력은 v11 최초 상태 조회 실패 PC뿐이므로 PC 최종 승인, 이번 제작 모바일은 후보로 구분.
- QA/DirectionImpact-20260921/web-status-failure-v11-illustration/index.html 및 desktop.png/mobile.png/manifest/REVIEW/PROMPTS/qa. 최초 조회 실패·마지막 성공 정보 없음: 점검 여부 미확인, 일정/KST/마지막 확인 없음, 상태 다시 확인 하나.
- 조회 실패를 점검 확정/정상 상태로 바꾸지 않음. 이 화면은 상태 조회 실패 표현이며 unknown 때 실제 웹 접근 제한은 기존 P-MAINT-WEB-FULL-01 담당 계약 정합화 후 적용, 실패만으로 새 전체 서버 차단 정책 발명0. 원본 승인 배경/독립 표현 재사용.
- 출력 육안·PNG/JSON/fixture/참조/승인 저장 PASS. 실제 브라우저/실기/WEB/API/라우트/세션 QA NOT RUN. 모바일 미술승인·독립조합·정책정합·갱신실패/해제·실 연결 잔여. 전체 큐 완료/소비후속 선등록/타 담당 코드 수정/운영배포0.

### 2026-10-01 — 웹 최초조회실패v11 전체 승인 / 갱신실패v12 후보

- 사용자 승인으로 v11 모바일 추가 승인, PC·모바일 모두 최종 미술 승인 등록. QA/DirectionImpact-20260921/web-status-failure-v11-illustration.
- 후보 QA/DirectionImpact-20260921/web-status-stale-v12-illustration/index.html 및 desktop.png/mobile.png/manifest/REVIEW/PROMPTS/qa. 이전 공개 점검 snapshot 조회 성공 후 재조회 실패. 기존 일정/성공확인03:50 예시 유지, 상태 갱신 실패/마지막 정보/갱신 필요로 표시. 현재 점검·정상·해제 미확인, 실패 시각으로 덮어쓰기 없음. 메뉴·다른 링크 없이 상태 확인 하나.
- 새 영속cache/무제한 재사용·unknown 실 게이트 규칙 발명0; 기존 Controller/Model cache·세대·retryAfter 재사용과 담당 정책 정합화 확인 후 소비. 독립 조합 때 PC/모바일 경고 아이콘 동일 원본 통일 검수 필요.
- 육안·PNG/JSON/fixture/참조/v11 승인 PASS. 실제 브라우저/기기/WEB·cache·라우트·인증 NOT RUN. 미술승인·독립조합·정책정합·해제 후보·실 QA 잔여. D-MAINT-LOGIN-01 전체 미완료 및 후속 선등록/타 담당 코드·큐 변경/운영배포0.

### 2026-10-01 — 웹 갱신실패v12 승인 / 해제확인v13 후보

- 사용자 승인으로 web-status-stale-v12-illustration PC·모바일 최종 미술 승인 등록. 기존 마지막성공/갱신필요 표시 유지, 실 소비 완료 아님.
- 후보 QA/DirectionImpact-20260921/web-maintenance-released-v13-illustration/index.html 및 desktop.png/mobile.png/manifest/REVIEW/PROMPTS/qa. 최신 성공 조회에서 whole-WEB 해제/이용가능 확인 예시; 점검이 종료되었습니다/홈페이지 이용 가능/홈페이지로 한 버튼. 06:05KST 시안값, 이전 일정·갱신필요 없음.
- 기존 홈페이지 root / 복귀, 자동 인증/원명령 재전송 없음. GAME나 로그인전용 해제/시각만으로 전체웹 허용 추론 금지. 실제 whole-web 계약·세션·경합은 담당 정책 정합화 확인 후 연결; fixture는 신규 API 필드 아님.
- 육안·PNG/JSON/fixture/참조/v12 승인 PASS. 실 브라우저/기기/WEB 해제·세션·경합·라우트 NOT RUN. 미술승인·독립리소스/조합·정책정합·실 QA 잔여. 게임/웹 통짜6상태 자료만으로 전체 큐/실 구현 완료 주장0, 소비후속 선등록/타 담당 코드·큐 변경/운영배포0.

### 2026-10-01 — 웹 해제v13 승인 / D-WB-01 약탈왕 통짜v11 후보

- 사용자 승인 QA/DirectionImpact-20260921/web-maintenance-released-v13-illustration PC·모바일 최종 미술 등록. 게임·웹 점검 통짜 승인은 독립 자산/실 게이트/기기 QA 완료 아님. D-MAINT-LOGIN-01 잔여 유지.
- 다음 기존 큐 D-WB-01: QA/DirectionImpact-20260921/world-boss-raider-v11-illustration/index.html 및 raider-p1.png/manifest/REVIEW/PROMPTS/qa. 승인 world-boss-binding-v10-art 인물·차량/scene/catalog 참고, world-boss.binding.v2.20260929; 약탈왕 pressure-lock P1/R1/solo E→I 단일 상태 고퀄 통짜 후보.
- 평행 두 열차 외부전+내부5요원/5초상자리+스킬3. 실제 출격 초상 및 가변 객차 조합 필요, 고정 객차 수/신규 캐릭터 디자인으로 승격하지 않음. 톱니 장식은 기존 계약 확인 전 새 입력 버튼으로 연결하지 않음.
- 육안 후보검수·PNG/JSON/fixture/참조/v13 승인 PASS. 실제 선로·바퀴/360·430px/arenaLayout·carId/명령·권한·runtime QA NOT RUN. 후보 미술승인·독립조합·나머지보스/상태·실검증 잔여, D-WB-01 전체 미완료/후속 선등록0. 기존 원화/타 담당 코드/실 배포 보존.

### D-RAIL-ART-01@ordinary-rail-gimmicks-v01 — B 이행 대상·구 A 신규 제작 중단 / 2026-10-02
사용자 “그거먼저 하자 ㄱㄱ” 직접 지시로 착수. [검토 후보](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v01-illustration/index.html) / [화면구성 명세](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v01-illustration/SCREEN_SPEC.md) / [REVIEW](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v01-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v01-illustration/manifest.json) / [QA](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v01-illustration/qa.json).
적용: 04 navigation-resume-policy-v02 D1 승인·14 목적지/물리선로 분리·44 damage-shield-hazard-detail-v03·45 충돌하지 않는 기존 HUD·전투3차 승인 화풍. 통짜 후보 R01 분기 미리보기/확정 전, R02 낙석예고/감속/잠김, R03 지상잔해 집중사격/저속, R04 충돌피드백/순항목표보존·실제강제감속 4종. 미선택 보정 전3장 history 보존.
PNG4종948×1659/1660·JSON·fixture·검토페이지 참조 PASS, 생성본 육안검토 수행. 미술 승인 대기; 독립alpha/동적UI/실제출격편성·초상/360·430px·긴화면·입력44px/실물리·합류·원거래·복구 검수 NOT RUN. 회피/파괴성공·낙하피격·확정요청중/오류·접근성 후속 잔여. 후보 제작을 실제 구현·전수승인으로 처리하지 않음. 타담당 파일/실입장·보상·운영배포 변경0. 미충족 소비 후속 등록0, 완료 항목 삭제0. 기존 월드보스 후속 보존.

### D-WB-RAIL-01@world-boss-rail-action-v3-20261002 — B 적합성 계약 대기·구 A 신규 제작 중단
이번 사용자 명시 승인으로 배정. 44#world-boss-rail-action-v3-20261002 및 WAIT 동명절의9종으로 월드보스 시안을 개정. 기존 WB2/v2 장치순서 후보를 신규 확정기준으로 사용 중단, 원화/역사 보존. 최신 승인 인물·차량·공통 HUD 미술 유지; 기존 좌우 레버·명시확정과 실제 분기/객차추종/합류·외부공격예고·내부조작을 구성. 약탈왕1 솔로의 예고→선택→확정→열차전체통과→반격 상태부터 검수가능한 단위로 제작하고 나머지9종/협동/실패/미확정까지 진행. 오르트2/3 양쪽 선택 가능 조합 표시. 미확정 timing/피해 수치를 시안에서 승인값처럼 굽지 않음. 프레임/레버/선로/위험범위/요원/문구·동적게이지 독립 제어, 실제 작은 화면에서 가독성/입력 분리 검수. 새 후보 미술 승인은 사용자에게 제시하고 기획 승인을 미술 승인으로 대체하지 않음. 기존 Direction 인계/QA에 근거, 실제 Unity연결0. 전체 최종시안 준비 조건은 WAIT에 기록.

### 일반 작전 선로 기믹 v02 전체 미술 승인 — 2026-10-02
<a id="ordinary-rail-gimmicks-v02-approved-20261002"></a>
D-RAIL-ART-01@ordinary-rail-gimmicks-v01: R01~R03 최종 승인 후 R04 저속 손잡이 보정도 사용자 최종 승인.
[최종 승인4종](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v02-illustration/index.html) / [명세](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v02-illustration/SCREEN_SPEC.md) / [REVIEW](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v02-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v02-illustration/manifest.json) / [QA](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v02-illustration/qa.json).
승인 표시 계약: 손잡이는 실제 운행 속도를 따라 감속 중 저속 위치로 이동, 목표속도는 별도 상태/보조 문구 유지. 프로그램 표시 갱신은 입력 명령 발행0, 사용자 드래그는 기존 목표속도 명령, 감속 해제 후 실제 가속에 따라 복귀. 속도 상태의 잠금 아이콘 제거, 선로 저장 후 잠금은 유지.
적용기획04 D1·14 목적지/물리선로 분리·44 피해/위험v03·45 충돌하지 않는 HUD 및 최신 사용자 표시 결정. 타담당45 목표손잡이 표시 문구 정합화/Unity실연결은 WAIT-003 잔여, 소유 원본 직접 변경0.
검수: R01~R03 v01과 SHA256동일 PASS, 네PNG/JSON/로컬참조 및 승인 저장 확인, R04 생성본 손잡이저속/목표별도/속도잠금제거 육안 확인. 실제360·430px/긴화면·44px/동적속도·입력·독립alpha·차량/선로/합류·최초접촉/복구/런타임 NOT RUN. v01/보정 전 원화·START snapshot 보존.
미술4종은 승인 완료. 회피/파괴 성공·낙하피격·요청중/오류·접근성 등 후속과 독립조합/실검증 잔여로 전체 큐 완료 삭제/소비후속 선등록0. 배포/실입장/실보상 변경0.
### D-RAIL-ART-01 중앙 진입선 도식 복원·재검토 — 2026-10-02
<a id="ordinary-rail-central-entry-rework-20261002"></a>
사용자가 v02 좌우만 있는 선로 도식을 지적 후 재작업 지시. 기존 승인 전투3차의 좌·중앙·우 도식을 복원하되 중앙은 현재 진입선 표시, 좌우는 선택 입력으로 구분. 현재 지시만으로 제3직진선 선택을 신규 승인하지 않음. 기존 4종 미술/저속손잡이 승인은 역사로 보존하고 중앙 도식은 수정 필요로 표시, 전체 승인 취소/원화 삭제0.
원본 45#source-2dc80daf4baa73f3-1-플레이-목표와-조작-역할·14 목적지/물리선로 분리·04 D1 유지. 손잡이 실제속도 추종/목표별도 승인 유지. 일반4종의 미리보기/확정/낙석/잔해/충돌 중앙 표시와 실제 분기·합류·선택 가능 영역을 재검토. 최신 원화는 새 버전으로 제작·사용자 미술 검토, 독립 요소/버튼 입력 명세 대조 필요.
사용자 지시로 Plan P-RAIL-HUD-REVIEW-01@central-entry-line-20261002 등록. 월드44#world-boss-rail-action-v3-20261002 및 D-WB-RAIL-01도 같은 중앙 누락/미선택현재진입선/서버확정/합류/9종 차이를 총괄 검토 대상으로 연결. 총괄에서 새로운 선택 정책이 필요하다고 판단하면 사용자 승인 전 시안에서 임의 적용하지 않음. 원 START 및 승인 자산·역사 보존, 런타임/타담당 원본 수정0.

### 일반 선로 v03 중앙 표시 복원 후보 제작 — 2026-10-02
<a id="ordinary-rail-central-v03-produced-20261002"></a>
D-RAIL-ART-01@ordinary-rail-gimmicks-v01: 사용자 중앙 현재 진입선 표시 복원 지시 이행.
[4종 수정 후보](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v03-illustration/index.html) / [명세](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v03-illustration/SCREEN_SPEC.md) / [REVIEW](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v03-illustration/REVIEW.md) / [QA](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v03-illustration/qa.json).
승인 v02 네 장을 builtin image_gen으로 편집해 좌/중앙현재/우 도식 복원. 중앙은 중립 표시 전용, 신규 선택/확정/콜백영역 없음. R01우측미리보기/확정전, R02낙석예고/잠김/감속, R03지상잔해/우리포탑사격/저속, R04충돌/원목표별도·실제손잡이저속 유지.
총괄45#rail-central-entry-review-20261002 대조 완료 근거 인수, 중앙복원 정책 선행 해소. 일반D1 미결hold와 월드v3미확정 계속진행 구분, 이번은 일반4종만 제작. Plan 새작업/완료 상태 임의 변경0, 메시지0.
생성본 중앙도식/현재문구·좌우효과·각위험/속도상태 육안 확인, PNG4/JSON/fixture/로컬참조 검수. v03미술승인 대기, 독립alpha/실제360·430px/긴화면/44px·입력·물리·거래·복구/런타임 NOT RUN. v02 승인 이력/원화·START 보존. 전체큐삭제/소비후속 선등록0, 제품코드·실입장·보상·배포 변경0.

### 일반 선로 중앙 복원 v03 최종 미술 승인 — 2026-10-02
<a id="ordinary-rail-central-v03-approved-20261002"></a>
D-RAIL-ART-01@ordinary-rail-gimmicks-v01: 사용자 “선로 복구쪽들 다 최종시안 승인”으로 v03 4종 최종 미술 승인 등록.
[최신 승인4종](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v03-illustration/index.html) / [manifest](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v03-illustration/manifest.json) / [명세](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v03-illustration/SCREEN_SPEC.md) / [REVIEW/잔여](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v03-illustration/REVIEW.md).
중앙현재진입 표시·좌우선택/확정·실제속도 손잡이추종 승인 유지. 승인상태/PNG/JSON/참조 확인. 독립alpha·실제360/430px/긴화면·입력44px/물리·합류·거래·복구·런타임 NOT RUN. 기존후속성공/오류상태 잔여로 전체큐완료삭제/소비선등록0.
다음 기존 D-WB-RAIL-01@world-boss-rail-action-v3-20261002: 약탈왕1 선로포격 솔로 예고→좌우미리보기/명시확정→전체열차추종→통과/합류·반격 상태부터. 승인인물/열차/공통HUD 재사용, 중앙표시복원 유지. 월드미확정 현재진행/권위180초와 일반미결hold 분리. 구WB2/v2 장치순서 시안은 역사로 보존, 새미술/런타임승인으로 자동승격0.

### 약탈왕 1페이즈 선로 포격 4종 후보 제작 — 2026-10-02
<a id="world-boss-rail-raider-p1-v01-produced-20261002"></a>
D-WB-RAIL-01@world-boss-rail-action-v3-20261002: [검토 후보4종](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v01-illustration/index.html) / [명세](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v01-illustration/SCREEN_SPEC.md) / [REVIEW](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v01-illustration/REVIEW.md) / [manifest](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v01-illustration/manifest.json) / [QA](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v01-illustration/qa.json).
44 선로액션v3·45 중앙/모드/속도 대조와 승인 차량/일반HUD 참고. 약탈왕1 솔로 포격예고·우측미리보기→수락확정/잠금→객차통과→회피후반격 4종 builtin image_gen 제작. 중앙현재 표시전용, 좌우만 입력, 월드시간 계속진행. 통과/반격도 손잡이우측·확정됨·잠금·확정버튼disabled 유지. 구장치순서 시안/오류보정전원화 history 보존.
PNG4종948×1659/1660·JSON·로컬참조 PASS, 생성본 흐름/조작부 육안 확인. 미술승인 대기. W04 후미 차량 전면이 기관차처럼 읽힐 여지 및 차량별 형상/곡선접촉 연속성 검수 잔여; 실제 승인alpha조합에서 정확한 편성 확인 필요. 실제360/430px/긴화면·44px 입력/독립자산·후미/합류·실geometry/권위판정·HTTP/복구/게임연결 NOT RUN. 시안 fixture는 운영수치/새인물승인 아님.
현재 WAIT Server 로컬소비계약 final6 78/78 및 U-WB-RAIL-02 등록 확인, 실제물리/Direction미술/통합 완료로 승격0. 나머지8패턴/각회차·양방향·실패/협동·복구 및 전체최종미술/독립자산 잔여로 자기큐완료삭제/소비후속선등록0. 제품코드·실입장·보상·배포 변경0. 총괄회신은 사용자상시권한 direction-coordinator-replies-authorized-20261002 범위에서 실결과만 전달.

### 약탈왕1 W03 선로 방향·기존선 포격 보정 — 2026-10-02
<a id="world-boss-rail-raider-w03-geometry-v02-20261002"></a>
D-WB-RAIL-01: 사용자 3번 후진처럼 읽히는 분기선/기존선포격 누락 지적. [v02 W03 단독 후보](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v02-illustration/index.html) / [명세·REVIEW](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v02-illustration/SCREEN_SPEC.md) / [QA](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v02-illustration/qa.json).
전방왼쪽 진행·분기점후방오른쪽·운전자기준우측=화면위쪽 회피선으로 배치 보정. 기존전방진행선에포격범위/해칭/문구, 안전선밖위험 표시. 중앙현재 표시와 우측잠금/비활성확정/순항 유지. 두 선택경로 유지, 제3선택 추가0.
PNG948×1660/JSON/로컬참조PASS·방향/위험영역육안확인. 사용자미술승인대기. Server LOCAL_LOGICAL_ONLY_UNVERIFIED_Y의 실미확정 currentENTRY 연결/공격범위·후미/합류/차량형상·독립alpha·기기/입력·runtime NOT RUN. 시각후보를실geometry결정으로승격0. v01보존, W01/W02/W04 동일배치추가보정 잔여, 전체큐완료삭제/후속선등록0. 제품코드·타담당원본·운영/실입장/보상0.
### 약탈왕1 W03 전방 선로·포격 범위 최종 미술 승인 — 2026-10-02
<a id="world-boss-rail-raider-w03-v02-approved-20261002"></a>
D-WB-RAIL-01: 사용자 승인 다음 최종 시안 ㄱㄱ으로 [v02 W03](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v02-illustration/index.html) 최종 미술 승인. manifest/qa/REVIEW/index 승인 등록, PNG 변경0. 기존선 전방포격·우측 회피선 전방연속 표시와 잠금 HUD 승인. 실제 currentENTRY geometry/후미/합류·독립alpha·기기/입력·runtime NOT RUN 유지. 다음 W01/W02 동일선로 예고·미리보기/수락확정 후보 제작; W04 및 나머지8패턴 잔여. 자기큐완료삭제/미충족후속등록0.

### 약탈왕1 예고·확정 보정 v03 후보 제작 — 2026-10-02
<a id="world-boss-rail-raider-p1-v03-produced-20261002"></a>
D-WB-RAIL-01: [W01/W02 새 최종 검토 후보 + 승인 W03 비교](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v03-illustration/index.html) / [명세·잔여](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v03-illustration/SCREEN_SPEC.md) / [manifest](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v03-illustration/manifest.json) / [QA](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v03-illustration/qa.json).
사용자 W03 승인 등록 후 동일 구도로 예고/미리보기와 서버수락 확정 두 상태 builtin image_gen 제작. 기관차앞분기·LEFT기존선위험예고/RIGHT우회 preview, W01확정버튼활성·잠금없음, W02손잡이우측잠금/확정됨·확정버튼disabled·지원3 유지. 잘못된 지원칸잠금 생성본 history보존후보정. W03은 승인원본외부참조로SHA동일 확인.
44#world-boss-mainline-detour-approved-20261002 최신사용자 승인 원문 인수: 기존진행선+우회+공통합류 topology 승인대기 해소, ENTRY기존선별칭/제3선택0·미입력기존위험선계속. 이전 LOCAL_LOGICAL_ONLY_UNVERIFIED_Y는 과거계약검증경계이며 새기획승인으로 물리검증까지 완료된것은 아님. 최종미술은 순수물리준비선행아님, View/bootstrap 전체최종미술등AND 유지.
W01/W02미술승인대기; W03최종미술승인 유지. PNG2종948×1659/1660+승인3참조/JSON/로컬참조PASS·예고/확정/지원상태육안검수. 실제기기44px·독립alpha/차량형상·곡률/후미/합류·원거래·HTTP/runtime NOT RUN. W04보정과나머지8패턴/회차·방향·실패·협동·복구 잔여. 전체큐완료삭제/후속선등록/제품코드·실입장·보상·배포0.

### 약탈왕1 선로 조작부 전투3차 형태 적용 v04 — 2026-10-02
<a id="world-boss-rail-raider-p1-v04-third-controls-20261002"></a>
D-WB-RAIL-01 사용자 명시요청으로 [3종 수정후보](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v04-illustration/index.html) / [명세·검수](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v04-illustration/SCREEN_SPEC.md) / [QA](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v04-illustration/qa.json).
QA/Design/Combat-Route-Speed-20260914/combat-third-final.png 직접확인·builtin imagegen참조편집, W01/W02/W03 큰가로원통그립·황동콜라·좌우화살표·중앙현재·우측선택광으로대체. 기존 명시확정/수락후잠금 유지, 속도순항·지원3 유지. 원본pixel복사 독립자산 아님/형태재현 후보, glyph비율/문구미세차이 검토대상. 세전체수정본은미술승인대기, v02W03승인이력보존. PNG3/JSON/로컬참조PASS·형태/상태육안확인. 독립그립/track/화살표/현재/선택광/확정/lock명세추가, 실제독립alpha·기기44px/Bind수명·물리후미/runtime NOT RUN. W04·나머지8패턴/독립검수잔여로전체큐삭제/후속선등록/제품·운영/실입장/보상0.

### 전투 최종시안 선로 조작부 공통 반영 — 2026-10-02
<a id="combat-final-route-controls-applied-20261002"></a>
사용자 “좋아 승인 전투 관련 최종시안들에 해당부분 수정해줘”. [약탈왕 v04 W01~W03](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v04-illustration/index.html) 최종미술승인 등록(PNG변경0). [일반 v04 R01~R04 수정본](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v04-illustration/index.html) / [명세·검수](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v04-illustration/SCREEN_SPEC.md) / [범위·제외근거](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v04-illustration/APPLY_SCOPE.md) / [QA](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v04-illustration/qa.json).
현재최신7종 공통형태정합: 큰요철가로원통그립/황동콜라·좌우삼각·중앙현재표시·우측선택광·명시확정. 일반4종생성후R03/R04그립우측끝보정, speed저속/강제감속실제와목표순항·지원3 유지. PNG4/JSON/로컬참조PASS·조작상태육안확인. 일반4종은승인디자인적용수정본/합성실검수잔여; 새미술사용자추가검토와기기/독립검수를완료한것으로표시0. WB3사용자최종승인과기획mainline-detour승인 유지.
3차원본이미기준·과거일반/WB시안은역사보존. 폐기장치순서WB/미제작후속은일괄수정완료로집계0, 기존D-WB-RAIL-01후속 W04/나머지8종·협동/실패/복구제작에서동일승인디자인사용명세연결. 선로장치없는입장/성장/장비UI 추가0. 독립grip/track/arrows/current/glow/lock/confirm·MVC같은명령/Bind해제 명세. 실제기기44px/독립alpha·동적UI/물리/원거래/runtime NOT RUN. 기존D-RAIL/D-WB전체완료삭제·미충족소비후속등록/제품·운영·실입장·보상0.


### 일반 v04 수정 승인 · 약탈왕 반격/견인사슬 후보 5종 — 2026-10-02
<a id="raider-p1-w04-p2-review-produced-20261002"></a>
D-RAIL-ART-01: 사용자 수정 시안 승인으로 [일반 v04 R01~R04](../../QA/DirectionImpact-20260921/ordinary-rail-gimmicks-v04-illustration/index.html) 최종미술 승인 등록. 4종 PNG SHA256 기존QA동일, manifest/qa/REVIEW/명세/갤러리 승인 확인. 독립조합·실기/물리/runtime 잔여 유지.
D-WB-RAIL-01@world-boss-rail-action-v3-20261002: [P1 v05 W04 반격 후보](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p1-v05-illustration/index.html) 및 [P2 v01 견인사슬 4상태 후보](../../QA/DirectionImpact-20260921/world-boss-rail-raider-p2-v01-illustration/index.html). 각각 manifest/SCREEN_SPEC/REVIEW/PROMPTS/qa/INDEPENDENT_ELEMENTS 연결. P1 승인 v04 W01~W03 외부참조 보존, 새5종 사용자미술승인대기.
큰 가로원통그립·중앙현재·명시확정/수락후잠금/순항·지원3 유지. W04 후미 박스형객차·반격광. P2 예고/미리보기→수락잠금→사슬느슨(연결유지)/내부안전핀→동일핀인출/견인해제 전체성공예시. 잘못된 초기그립/끊어진사슬/다른핀 생성3종 history보존 후 builtin imagegen수정. P2좌측안전선/위험선 배치는 미연결fixture이며 다른패턴 일괄RIGHT안전정책 추가0. 비교 버프 운영값 굽기0.
PNG5/JSON/로컬갤러리참조검사·생성본육안검토. 기존v10승인기관차/객차/보스열차/인물 존재 대조. 사슬/핀/공통그립독립자산 이번산출물미공급, 기존객차원본/후보카메라조합검토필요. 실제360/430·긴화면/44px·alpha동적조합/실ID·geometry/last-rear-clear/합류·원거래/HTTP/runtime NOT RUN.
전체9패턴/회차·양방향·실패/협동/복구·전체최종미술/독립자산 잔여로 자기큐삭제·미충족View/bootstrap후속선등록0. 순수물리준비는미술승인선행으로막지않음. 제품코드·타담당원본·운영배포/실입장/실보상변경0. START snapshot/역사원화 보존.

### 약탈왕 신규5종 승인·전투 추가제작 보류 / 비전투 점검 — 2026-10-02
<a id="raider-five-approved-combat-hold-20261002"></a>
사용자 “승인이긴한데 일단 기획 b 안으로 바뀔지도모르니까 이쪽은 여기서 마무리하고 전투쪽 말고 다른쪽 체크해보자”. P1 v05 W04 1종/P2 v01 4종 최종미술승인 등록, PNG5 SHA256 기존QA동일·manifest/qa/REVIEW/갤러리 readback 확인. 현재 A 승인본/역사자료 보존, B안 정책·미술 승인으로 승격0. D-WB-01/D-WB-RAIL-01/D-RAIL-ART-01 추가제작은 사용자 재개 지시까지 보류; 전체 완료 삭제/소비후속 등록 아님. 제품코드/실입장/보상/배포 변경0.
비전투 실제큐: D-EQ-01(장비·보관함·상인) 및 D-MAINT-LOGIN-01(게임/웹점검) 담당 잔여 존재. EQ 최신v59 장비해제갱신까지 통짜 미술 승인 이력, 점검 게임v01~v06 및 웹v08~v13 승인 이력 확인. 우선 잔여는 독립자산/동적조합·실크기/터치/스크롤 검수 및 실제소비 경계이며 전부 신규통짜 미술 미제작으로 집계하지 않음.
메뉴 v03 10종·journal-followups-v01·작전일지 states-v02·문의server-list/detail-v04/list-states·지휘실v17/43노드 아이콘은 기존 승인 근거 확인. 구 candidate/retired 이력을 현행미승인으로 잘못 집계하지 않음. 동일화면 중복제작 불필요.
비전투 신규 고퀄 통짜 검토 가능 대상: 포탑성장(detail/조건부족/최대/작전중차단/결과) 기존 coop-world-turret-v02 비전투 부분은 결합 수정 후보이며 전용 최종 통짜/신규 미술승인 근거를 이번 확인범위에서 찾지 못함. 35 현행 성장정책과 WAIT 예정 D-03 조건 재대조 후 제작, 필요수치 임의확정0. 기존 협동 시안 사용자 대기는 유지. 이번은 비전투 범위 점검이며 새큐 임의등록/타담당항목 수정/자동메시지 없음.

### D-EQ-01 장비·보관함·상인 독립 조합 후보 v60 — 2026-10-02
<a id="equipment-composition-v60-produced-20261002"></a>
사용자 포탑도B안에서삭제가능하므로 포탑성장 신규제작보류·장비/보관함/상인 우선지시. 기존 전투보류 유지. [검토갤러리](../../QA/DirectionImpact-20260921/equipment-composition-v60-review/index.html), SCREEN_SPEC/REVIEW/manifest/qa/verify 및4화면 연결. contract direction.equipment-independent-composition-v60.20261002 / D-EQ-01@equipment-supply-v18.
장비v52/보관함v30-v34/상인v12·수리v23·포션v24 승인 구성 재조합. 45 merchant-recovery-approved-v17 수리20/20%·포션24/25% 반영, 할인0·재고/HP/미연결값시안fixture. 승인6아이콘atlas·프레임/판/라벨/별/배지/빈칸/잠금/입력 독립, 장착품배지/냉각모래시계·5초상미연결자리·가변타워가로스크롤/원대상슬롯미추정. 30/54내부스크롤·단건즉시버리기·잠금차단·직접단건수령·압축/빈칸끝·상인모달복귀/스크롤유지 검토예시. 실제게임명령/통신/영속저장0.
새포션alpha는 builtin image_gen 후보(4px샘플clear60271/partial38289), 미술승인대기; 원본생성/기존승인미술보존. 상인배경/수리아이콘은통짜원화와정합잔여를명시, 전체자산완비/최종조합승인으로처리0.
headless Chrome360/430/720×4화면138/138 PASS·PNG12장·equipment430/merchant360/inventory360육안검토. 초기아이콘압축발견→원화미수정/atlas정사각region비율보정후재검증138PASS·보정후2캡처육안확인. manifest자산존재/SHA/JSON/QA저장확인. 실제기기터치·드래그합성/실ID·권한영수증/거래/Unity import/runtime NOT RUN.
미술조합검토후보이며 전체D-EQ-01 잔여유지, 큐삭제·미충족소비후속등록없음. 타담당코드·타큐항목·실입장/보상/운영배포변경0. 포탑B안승인/삭제를추정하지않음. 다음잔여: 신규포션/조합미술판단·상인승인배경/수리원화독립정합·실초상/owner연결·드래그/합성/실기·실거래검증.

### v60 승인 / 상인 배경·열차수리 독립 미술 후보 v61 — 2026-10-02
<a id="equipment-merchant-independent-v61-produced-20261002"></a>
D-EQ-01@equipment-supply-v18: 사용자 승인 다음 ㄱㄱ으로 v60 조합4화면/포션독립PNG최종미술 승인 등록(manifest/qa/REVIEW/index·자산SHA유지확인). 승인으로실거래/터치/runtime완료처리0.
다음 [v61 상인원화2종/조합검토](../../QA/DirectionImpact-20260921/equipment-merchant-independent-v61-review/index.html) / merchant.html / SCREEN_SPEC·REVIEW·PROMPTS·manifest·qa·verify 및3폭capture. contract direction.equipment-merchant-independent-v61.20261002. 승인v12상인통짜참조 builtin imagegen: UI없는정차철도역배경1672×941, 기관차수리서비스투명원화1448×1086. 배경/프레임/판/서비스·장비아이콘/배지/별/문구/가격/버튼독립. 기존파랑렌치/빈중앙배경은v60역사승인보존, 새2종미술검토대기. 포션v60승인PNGbyte동일재사용.
headlessChrome360/430/720 상인45/45 PASS: 신규자산연결/독립숨김, 가로넘침/스크립트·로드오류없음, 단건정리→동일상인재고/보급/스크롤복귀·장비구매가능재평가·만석서비스표시·배지중앙. alpha4px샘플clear44628/partial53781·PNG/JSON/자산SHA/갤러리로컬참조PASS. 원화2및360/430capture육안확인. 다른승인v60화면검증은기존138근거재사용, 이번전수새검증으로표시0.
45 merchant-recovery-approved-v17 수리20보급/최대HP20%,포션24보급/생존요원최대HP25% 유지. 실HP/할인counter/revision/영수증·구매실행미연결. 정차기관차환경/수리삽화는runtime객차모듈로사용금지,새포탑/기믹정책추가0. 실제START초상/ownerID·실기터치/드래그합성·Unityimport/실거래/runtime 잔여로전체큐완료삭제/미충족소비후속선등록0. 타담당코드/실입장·보상·배포변경0. 전투·포탑성장 B안 검토보류 유지.

### v61 승인 / 장비드래그 독립조합 v62 — 2026-10-02
<a id="equipment-drag-independent-v62-produced-20261002"></a>
D-EQ-01: 사용자 승인 다음 ㄱㄱ으로 v61상인배경/수리독립미술+조합최종승인등록, manifest/qa/REVIEW/index·자산SHA유지확인. 새 [드래그독립3상태](../../QA/DirectionImpact-20260921/equipment-drag-independent-v62-review/index.html), contract direction.equipment-drag-independent-v62.20261002 / SCREEN_SPEC·REVIEW·manifest·fixture·qa·verify 연결.
승인v38적격표/8장비 및v60프레임/atlas/배지재사용, 신규원화0. 원본흐림·장비명/성급고스트·적격2→3/잠금/성급/최고5/종류불일치 독립, 태그무관·드롭대상주재료/원본보조. 적격드롭은합성미리보기부분조합만/부적격자동장착전환0. 실제소모/추첨/실명령0. 실제v35/v36합성전체상세대체금지. 요원/타워슬롯/ID추정복제0, 전투/포탑성장보류유지.
headlessChrome360/430/720실마우스·fixture93/93 PASS:고스트/원본/사유/빈칸끝·적격드롭1회미리보기1개·잠금원본/대상차단·취소/Escape/pointercancel·Unbind후호출없음·소모0/페이지overflow오류없음. 9상태+3미리보기PNG. valid360/invalid430육안확인→5성별폭보정 후93재검사PASS·valid360/preview360재검토. JSON/자산SHA/갤러리연결확인. 물리터치/실STARTowner·장착타겟·거래/Unity/runtime NOT RUN. touch는이번시안에서시작안하고기본스크롤유지,8px마우스감지fixture는runtime튜닝승인아님.
새조합미술승인대기·전체D-EQ-01미완료/큐삭제·미충족후속선등록0. 기존승인/START snapshot보존, 타담당제품코드/실입장·보상·배포0.

### D-B03-HP-01 수정 후보 제작·검수 — 2026-10-02
<a id="combat-b03-hp-v01-produced-20261002"></a>
- 사용자 직접 제작 지시(Plan 전달 근거 확인), QA/DirectionImpact-20260921/combat-b03-hp-v01-review/index.html / combat-b03-hp-v01.png / states.html / SCREEN_SPEC.md / REVIEW.md / manifest.json / qa.json / PROMPTS.md.
- direction.combat-b03-hp-v01.20261002. 승인 B03 원본 보존, ③ 세로 정후방·좌측 병주·전방 바위·원통 선로손잡이·중앙표시·속도·지원3 유지. 요원/객차/충전 분리, 짐칸 객차만. v03 정정 우선/v04 미승인 반영0. 원본 네 요원+짐칸 예시를 고정 runtime 편성/ID로 승격0.
- 기본 통짜 후보1+상태 도해7종(정상/파손/다운/동시/짐칸피격·조준/요원표적/객차표적). 생존 요원 있어도 객차 조준; 실제표적한곳; 파손시 전체카드 잠금0.
- Chrome headless 360/430/720px 48/48 PASS. 기본3/카드21캡처, 생성본과360파손/객차표적/기본 육안. 기본PNG 360px 작은라벨 가독성 잔여. 독립상태도해의12px/44px 표시검수와 통짜축소 가독성 구분.
- 신규 미술승인 대기. reference-only; 실기/독립alpha/START·carId/원표적·피해·복구/공통팝업미술조합/최종HUD 검수 미실행. 전체최종시안 후 View/bootstrap 조건 유지. Unity/Server/운영·실입장·보상·영상 변경0. 이 항목은 후보 제작 완료이며 미술 승인/전체B전환 완료 아님.


### D-B-MIG-01 Direction 이행 감사 완료 — 2026-10-02
<a id="direction-b-migration-audit-20261002"></a>
- 기준 44#combat-b-mainline-approved-20261002 / WAIT#combat-b-migration-20261002. D-B03-HP-01 후보의 미술승인 추정0. 기존 QA/DirectionImpact-20260921/combat-b03-hp-v01-review/SCREEN_SPEC.md에 유지/변경/신규폐기/미결 실제파일·ID·독립자산·상태/화면 목록 보완; B_MIGRATION_AUDIT.json에827텍스트/112화면군 검색근거(234적중파일, 폐기수 아님) 저장.
- 실제충돌: d01 composition.js:8 타워owner/장비; v60 view.js:6,19 owner타워1/6타워목록; train-modular/departure-composition.js; coop-world-turret-v02 포탑성장; commander-tree-v17-art/current-tree-data.js:2 TR04/X05/X13/X20 포탑전용, X15/X24 혼합효과. X13 부모의 TC01/OC02와 OR 연결도 영향. TR03는 열차HP노드이며 전체삭제대상0. OC01 현재세트효과 유지.
- 승인 B③ 원본 SHA8667AECEC19B7906107F9FB06A66CC042785EF2D6CCEF3E605F9AC90824AF5DA 보존. HP후보 신규승인대기, 기본4요원+짐칸은 예시(최대5정책의 전체변형미제작). 외부/특정내부전 별도구성, Eve센트리/적포대 일괄삭제0. 비전투미술 보존.
- 감사 완료/전체B전환 미완료. 문서/JSON/파일근거 검증, 기존48/48 HP표시 QA 재사용(변경없음). 실기·독립alpha·B runtime·전체화면상태 검증 미실행. 노드대체/기투자·장착품 이행/내부전·협동·월드패턴/v06·경제 미결은 WAIT연결.
- 자기 A전용 신규미술 큐를 B이행대상으로 재분류. D-EQ-01/D-MAINT-LOGIN-01 공통독립잔여 유지. U-B-VIEW-02 선행AND 미충족→후속등록0. 기획MD/타역할큐·코드/실거래·배포·영상·모델/새미술생성 변경0. 자기 D-B-MIG-01 감사항목만 완료정리.

## D-B-FINAL-01 — B 전투 최종시안 순차 제작 / 진행 중 / 2026-10-02
사용자 “좋아 순서대로 ㄱㄱ” 직접 승인. 앞서 제시한 1~9 화면군을 순서대로 최종 검토 후보 제작. 1번 일반 외부전 기본: 승인 B③ 세로 정후방 구도/공통 조작 유지, 5요원·짐칸 포함 편성 및 HP·충전 모바일 가독성 보완. 출력 QA/DirectionImpact-20260921/combat-b-exterior-base-v02-illustration. 신규 미술 승인 대기, 원본 보존/reference-only.
2공격·스킬/3요격/4등반/5파손·다운/6선로는 승인 v03/v04범위방향/v05역할과 현행 B절 기준. v06 미승인 세부는 별도 대기. 7월드/8협동/9내부전은 해당 상세계약 선행만 대기. 사용자 순서 승인으로 미결 정책/미술 자동 승인0. 전체 최종시안+독립자산+실 소비계약 후 View 연결 조건 유지. 생성 원화·명세·검토/검증 근거 기존 QA/인계에 저장; 완료한 화면군은 이 항목 진행기록, 전체 미완료 중 삭제0. 타담당 원본·코드/운영·실입장·보상·영상 변경0.
### D-B-FINAL-01 / 1번 외부전 기본 v02 후보 — 2026-10-02
<a id="combat-b-exterior-base-v02-produced-20261002"></a>
- 사용자 순서대로 제작 승인. QA/DirectionImpact-20260921/combat-b-exterior-base-v02-illustration/index.html / five.html / cargo.html / PNG2 / SCREEN_SPEC/REVIEW/manifest/fixture/PROMPTS/qa.
- direction.combat-b-exterior-base.illustration-v02.20261002. 승인 B③ 구도+HPv01참고, 44현행B/v03표적/v04범위방향승인/v05역할방향승인 적용경계. v06지원세부·경합/밸런스 임의확정0.
- 고퀄통짜:5요원·짐칸0 / 4요원·짐칸1. 원통선로/중앙/속도/지원3/병주/전방바위/포탑없음 유지. 반복작은HP문구를 공유아이콘범례로 모음. 카드요원HP/객차HP/충전분리, 짐칸객차만. 실출격ID·초상매핑미연결, 요원5/중립스킬은placeholder.
- 초기생성6명/배치오류 및 게이지누락3종은 생성이력보존·선정제외. 원본HPv01에서 다시 생성한5명 및그파생짐칸2종선정, 최종육안명수5/4+짐칸/카드/범례·게이지 확인.
- Chrome360/430/720 페이지로드/이미지/가로넘침9/9 PASS,6캡처; five360/cargo430육안. 360px작은범례/이름·백분율 가독성 잔여(자동PASS로주장0). 실기·입력44px/독립alpha·HUD조합·전투runtime NOT RUN.
- 신규2종미술승인대기/reference-only. 1번후보제작완료,2~9제작/계약확인및전체자산잔여로D-B-FINAL-01유지. U-B-VIEW-02 AND미충족→후속등록0. 기존승인원본/기획·타담당코드/배포·실입장·보상·영상변경0.

### B 객차 버프 타입별 미술 선행 — 2026-10-02
<a id="b-carriage-buff-art-prerequisite-20261002"></a>
사용자 “객차 버프 타입별로 디자인도 따로 만들자” 및 “기획 세션에서 타입을 먼저 확정” 선택. P-B-CARRIAGE-BUFF-01을 WORK_QUEUE_PLAN.md에 새로 등록, 기존 P-B-MIG-01 수정0. 기존 train-modular 자산/Unity 구타입은 B 확정 버프목록 근거로 사용0. 타입·ID/효과/적용대상·파손/복구·선두/짐칸 예외를 Plan이 대조·승인받은 뒤 Direction 디자인 후속. 지붕설비/실루엣/문양/아이콘으로 구분, 신규 부착포탑 복원0. 기본v02미술 승인대기/기존PNG 보존. 초상 위 HP/충전 문구는 공통범례(실제게이지아님), 사용자 삭제결정으로 추정0. 타입별 원화만 선행 대기, 기존 D-B-FINAL-01 전체 삭제/다른 승인 규칙 제작 차단0. 타담당코드/기획원본·기존큐 수정/메시지·운영·실보상·영상0.


<a id="combat-b-attack-skill-v01-produced-20261002"></a>
### D-B-FINAL-01 / 2번 공격·스킬 후보 2종 — 2026-10-02
combat-b-attack-skill-v01-illustration/index.html / focus·skill PNG/HTML / manifest/PROMPTS/SCREEN_SPEC/qa/captures. direction.combat-b-attack-skill-v01.20261002. 승인 B③·v04범위/v05역할 방향 적용. 집중단일 적요원 표식·적격 요원 집중/부적격 기본지속, 공격형 궁극기충전0%·선택강조/일시문구. 피해수치/3적 시각예시를 새대상수 정책으로 확정0. builtin imagegen 2생성+2보정, 기존원본 보존. 레이나초상 잘못변경→실game 우선 CombatSecondFinal/reina-v02 참조보정, Kyle 장거리집중 VFX 제거요청 반영. OperatorIdentity 구초상 이름맵은 후보/미확정으로 최종정체성 채택0.
360/430/720 파일/이미지/가로넘침9 PASS·6캡처, 기본이미지·skill360 육안. 360 작은이름/HP범례 가독성·탄선방향/발사owner/5슬롯정렬·진짜초상/스킬매핑/정확VFX 후속조합 잔여; 자동검사를전투적격·최종가독성으로주장0. 신규2종미술승인대기/reference-only. 승인객차7원화 정밀합성 아직0. 독립요원/표적링/궤적/효과/HP·충전/입력과실ID·geometry/기기44px·runtime 잔여.
D-B-FINAL-01 유지, 다음3요격. v06/경합/내부/월드 상세미결은 별도대기. U-B-VIEW-02 AND미충족 후속등록0. 정책원본/제품코드·운영/실입장/보상·영상변경0.
<a id="ultimate-live2d-onoff-v01-produced-20261002"></a>
### D-B-FINAL-01 / 궁극기 Live2D ON·OFF 검토 후보 — 2026-10-02
사용자 ON/OFF·서브컬처·Live2D 명시. ultimate-live2d-v01-review/index.html / on·off·character PNG/HTML / manifest/PROMPTS/LIVE2D_SPEC/qa/captures. direction.ultimate-live2d-v01.20261002. builtin imagegen3종. ON 전체화면 얼굴/권총·분리clock/소유hold·스킵, OFF 상단부분요약·위험/조작부 보존·hold없음. 04§14 및20 full/summary-cinematic 적용, OFF완전삭제로 변경0. 원피해/비용/범위는동일 원사건, callback으로 이중피해0. 약3초/0.9초는 비교안/시전시간 확정아님. DAILY세부 추가승인 유지.
검은포니테일·붉은눈 여성/권총은 최신20바이블+사용자홈페이지 원화참고; 구 CombatSecondFinal 남성레이나 초상은 최신외형과충돌하므로 역사보존/이번신규컷인채택0. 앞선 공격후보의 구초상 이행검토 필요, 정책/제품파일 덮어쓰기0. 다른4초상 전체canonical매핑 완료0.
360/430/720 로딩/가로넘침9 PASS·6캡처·OFF360육안. character1024×1536 실제alpha(clear sampled129258); 평면인물원화임. 신규미술승인대기/reference-only. 실제 Live2D model3/moc3/PSD파츠/가림복원/ArtMesh/Deformer/물리·motion은 미공급/NOT_RIGGED. 눈/입/헤어/팔/권총/코트/장식 분리·Bind해제/스킵/hold수명 명세. flatPNG/CSS움직임을Live2D완료로주장0. 실모델재생/물리기기/FPS·캐릭터가림/입력44px·독립UI/게임runtime 잔여. 정지키프레임으로모션실검증 주장0.
D-B-FINAL-01 유지, U-B-VIEW-02 전체AND 미충족 후속등록0. 제품코드/기획원본/운영·실입장·보상·영상/Runway0.

<a id="operator-cinematics-v01-produced-20261003"></a>
### D-B-FINAL-01 · 전체요원 모집/궁극기 원화 모션 후보 — 2026-10-03
사용자 레이나 완료 후 ‘전체로’ 승인: 기존14명 전체, 신규Rin/Cipher 및 비전투근무요원 제외. QA/DirectionImpact-20260921/operator-cinematics-v01-review/index.html / manifest / SCREEN_SPEC / REVIEW / qa / PROMPTS(요원별) / 연락판2 / 독립posePNG42 / GIF28(각432×768·2500ms). 버전 operator-cinematics-v01-20261003. 기본4명 승인 초상 기반, Vex/Nyx/Iris 최신25/27/29 기준으로 구외형 대체후보, 개인바이블 미확인7명 기존얼굴+17공통기준의 미승인후보. 44 원스킬 방향, 수치·대상·원run 변경0.
builtin imagegen 원화42+아이리스3/세라1보정, 잘못된권총·알약 후보 활성제외/원생성이력 보존. 포즈 연락판42 육안 및 GIF28 전체decode·총길이/크기·파일참조 검증 PASS. 실제Live2D model3/moc3/파츠리깅 없음; 3포즈 평면전환/카메라·장식광 참고. 반투명 배경광/파츠 분리·작은 포즈 변화/의상·도구 정합·연속관절/기기·SDK·실게임 잔여. 일부원화alpha만으로독립리소스 최종납품완료 주장0. 미술최종승인/런타임 구현승격0.
D-B-FINAL-01 전체 전투군/독립자산 및 소비계약 AND 미충족 유지, U-B-VIEW-02 후속등록0. 자기큐 진행기록만추가, 타담당 기존항목 수정/삭제0. Unity/Server·기획원본·배포·실입장·지급 변경0.

### D-B-FINAL-01 · 카일 모집 카드 등장 v02 — 2026-10-03
사용자 카드 반짝임 누락 및 카일부터 순차수정 승인 반영. QA/DirectionImpact-20260921/kyle-recruitment-card-v02/index.html / GIF / 단계연락판 / 독립card-back·background / SCREEN_SPEC·REVIEW·qa·재현recipe. version kyle-recruitment-card-v02-20261003. 기존pose01 유지, 카드광택/별빛→양쪽열림→카일등장. 432×768/2500ms/39프레임 전체decode PASS·4단계육안. 기존14명 검토index의 카일 모집 참조만 v02로 변경, 원모집GIF/궁극기/타13명·레이나 보존. 미술승인대기/reference-only, 브라우저/실기/Live2D/runtime 미실행. 희귀도·지급정책 변경0. 다음요원은 카일피드백 우선; D-B-FINAL-01 및 전체AND 잔여 유지, 후속등록0.


### D-B-FINAL-01 · 레이나 모집 등장 v01 후보 — 2026-10-03
사용자 타게임참고 제안흐름 승인 후 레이나 모집만 제작. QA/DirectionImpact-20260921/reina-recruitment-v01-review/index.html, 신규모집2원화·독립카드/배경·절차적FX/이름·GIF·단계연락판·명세/프롬프트/manifest/qa. version reina-recruitment-v01-20261003. 카드도착→빛집중/정지→뒤집힘→눈클로즈업→정면시선/이름. builtin imagegen2회, 432×768/4400ms/102프레임 전체decode PASS·6단계육안/로컬참조검증. 기존요원검토nav에 레이나모집링크만 추가. 신규미술승인대기/reference-only. 4.4초비교길이/무음·2원화전환, 실제Live2D/음향/브라우저/기기/게임NOT RUN. 인물반투명배경광·독립파츠잔여,alpha검사로납품완료주장0. 기존v08궁극기3포즈/e076·폐기후보/카일·타13명·기획정책/지급/Unity/Server/배포변경0. 레이나피드백우선, D-B-FINAL-01전체AND미충족유지·후속등록0.


### D-B-FINAL-01 · 레이나 모집 v03 LASTRAIL 문양 — 2026-10-03
사용자 기존브랜드엠블럼 임시사용 승인. QA/DirectionImpact-20260921/reina-recruitment-v03-lastrail/index.html / REVIEW.md. 기존title_emblem 원본동일SHA/독립SVG표시, 카드/광택/점등/인물/이름 독립. 8초 reference-only HTML 후보; 실제Live2D/모듈손동작/runtime 미완료. 기존시안보존/전체큐미완료 유지.

### D-B-FINAL-01 · 레이나 Live2D 준비 — 2026-10-03
사용자 실제제작진행 및 Cubism/PSD없음 확인. reina-live2d-preparation-v01/REVIEW.md/qa.json: 후보2 배경광잔여로제외, 파츠/정합PSD/Cubism리깅·export·재생잔여. 기존모집/궁극기보존. 실제Live2D미완료, 전체큐유지.

<a id="d-b-roof-types-01-ready-20261005"></a>
## D-B-ROOF-TYPES-01 — 객차 천장 7종 독립 부속품 제작 / 착수 가능
사용자 승인 원본: 44_COMBAT_DETAIL_CONTRACTS.md#combat-b-carriage-roof-art-approved-20261005. 현재 리소스 작업 저장·검증 안전경계 다음 우선 제작. 기본 천장과 BCB01~07 타입 부속품을 승인 방향대로 제작한다. 현재발견 whole-carriage base/BCB7종과 fixed-roof-placement-v01 및 최신승인 B③ 세로구도를 실제 대조하고 재사용 가능한 원본은 보존·고정한다. 천장단독 원본 없으면 그 사실을 명시해 별도 천장 자산을 제작하며 wholecarriage/잘라낸 조각을 독립생성 원화라고 설명하지 않는다.
제작 순서: 공통 천장과 배치/마스크 명세 → 방호형으로 방벽높이·중앙공간·등반/차량연결통로 검수 → 나머지6종 → 동일카메라7종 연락판/실크기 요원조합 비교. 중간 첫타입이 사용자피드백 필수가 아닌 제작검수 통과라면 나머지 승인범위도 이어 진행. 신규결과는 검토후보이며 방향승인을 모든그림 최종승인으로 확대하지 않는다.
산출물은 D:/Project/LASTRAIL/QA/DirectionImpact-20260921 아래 새식별 버전으로, 기본천장/타입장치/전후방벽/발광/요원 독립. 실제alpha·공통좌표/앵커·겹침/마스크·원본/hash/프롬프트/제작방식·실사용/후보/제외 inventory 기록. 동일기본천장 유지와 단독숨김/교체/7타입 식별·작은세로화면 요원/HP 가독성·발접지·등반/연결통로 실제조합 검수. 대표요원은 독립참고배치로만 쓰고 새통합천장그림에 굽지 않음. 실패후보·역사 보존, 게임 판정/버프/포탑/자동치유·요격 기능추가0.
기존 D-B-FINAL-01 유지. 완료한 이 큐만 근거를 기존 Direction 인계/QA/WAIT에 보존후 정리. Unity 최종화면연결은 전체최종시안승인+독립자산+정확 소비계약 선행 모두 충족후 후속단일등록; 현시점선등록0. 서버/Unity코드·운영배포·실경제·영상결제 없음.
2026-10-05 D-B-ROOF-TYPES-01 최신 사용자 승인: '거치대와 고정 구조를 더한 7종 천장 승인'. combat-b-roof-types-v01/assets/BCB01~BCB07-installed-roof-v02.png를 7종 시각 승인본으로 고정(정적 장비/거치·고정 구조/문양 포함). REVIEW 최신 승인 절 및 installed-roof-assets-v02.json 원본. 앞선 신규결과 후보/최종미술승인 대기 상태는 이 7파일의 시각 범위에서 해소. 통합 정적 천장 방식은 사용자 수정 우선, 요원·동적 상태 별도. 일반 비율 캐릭터 채택 아님/기존 SD 유지. 전수alpha·좌표/앵커/등반·연결/요원HP 실크기·동적 발광 필요·소비계약 검수 잔여로 큐 유지/Unity 후속 선등록0.

<a id="d-b-normal-proportion-motion-01-ready-20261005"></a>
## D-B-NORMAL-PROPORTION-MOTION-01 — 일반 비율 요원·큰 동작 제작 / 사용자 승인
원정책 19_SD_COMBAT_MODEL_TRANSLATION.md#normal-proportion-large-motion-approved-20261005. 과거 SD 기본채택/일반비율미확정 문구는 이번 방향승인으로 대체, 기존자료보존. 현재작업 저장·필수검증 안전경계 후 우선 레이나 일반비율의 원화정체성/실루엣/발접지/세로B카메라·승인천장과의비율 검수→파츠 재사용·수정·누락 명세→실제 지원 기본공격의 큰 준비/주동작/회수 및 피격 등 모션 순차제작. 기존판정시간·공격주기/범위 불변. 없는 스킬/요격추가0. 이후요원은 같은기준으로 기존부모제작범위 안에서 순차대조하며 전원재생성부터하지 않는다.
신규산출물은D프로젝트의일반비율임을식별하는새버전; 기존 combat-sd 자료를 조용히 덮어쓰지 않음. 이미지편집/생성·재사용·조합·관절가동·실제리그/게임연결을 정확분리보고. 실제alpha/파트독립/무기파지/접지·관절·코트가림/루프/방향/작은세로화면 실동작 검수, 정지그림pan/scale은모션완료아님. 이미승인된 installed-roof-v02 7종 고정통합천장보존, 캐릭터는별도. 미지원도구/파츠는구체기록하고가능한독립제작계속. 신규그림최종승인/화면연결자동승격0. 완료단위근거는Direction인계/QA/WAIT 저장후큐최신화; 기존천장큐의미완료검수는보존. Server/Unity코드·배포/실경제/영상결제0.
2026-10-05 D-B-ROOF-TYPES-01 크기정정후속: 사용자 BCB02-battle-footprint-v03 모서리정합기준채택. 나머지6종새1672x941 재작화/각소스4점 동일screen목표정합, 실제7alpha전수/hash 및14단독·요원HP배치검수 combat-b-roof-types-v01/REVIEW 최신결과절·battle-footprint-contact-v03 근거. 기존installed-v02 디자인승인/원본보존, 신규6종그림최종승인/PSD·동적발광·등반충돌/실게임수용잔여로큐유지. D-B-NORMAL-PROPORTION-MOTION-01 일반비율정책유지/몸통후속잔여,후속Unity선등록0.

2026-10-05 최신사용자7종승인/레이나재개: D-B-ROOF-TYPES-01 battle-footprint-v03 신규6종도시각승인(기준전술형포함7), 기존assetsJSON/REVIEW에hash7일치·승인갱신. 기술/실게임잔여큐유지. D-B-NORMAL-PROPORTION-MOTION-01 bodyfit-v02 배치/임시목어깨허리좌표저장·원화불변, REVIEW순차02 근거; 접속가림/실제rig·모션미완료,다음동일body접속대조. 신규화면연결선등록0.
2026-10-07 Reina 참고 GIF 시안 추가: 사용자가 올린 전신 12f 평탄화 체크무늬 시트를 정지 B③ 전투화면에 reference-only로 배치. QA/Direction-.../gensprite-style-trial-12f-v06-user-sheet-battle-placement-v02 의 배치JSON/REVIEW 참조. 원본/12 PNG/GIF/APNG 보존; runtime 연결·외형/루프 승인·기존 98% 목표 변경0. 셀경계 잘림 가능성·알파추정·겹침/실접지 미검수 기록, D-B-NORMAL-PROPORTION-MOTION-01 작업과 Live2D 중단 상태 유지.
2026-10-07 사용자 끊김 검수: 원본12f GIF 무한루프의 마지막→첫 포즈 불일치 seam 확인. 별도 one-shot preview(no loop; 80ms×11 +300ms end hold) 추가, source 순서/위치 고정. 24f crossfade는 흐림/이중윤곽으로 기각·보존. REVIEW/기존 handoff 기록 참조; 내부 포즈 중간동작·실제 cadence 미완료, D-B queue 유지.
2026-10-07 사용자 첨부 GIF의 시점 유지 요청: 단일 키포즈만 내장 imagegen 편집으로 상공3/4 시점 비교후보 출력. `QA/DirectionImpact-20260921/reina-combat-sd-v01/normal-proportion-v01/gensprite-style-trial-12f-v07-single-keypose-camera-test/` 참조. 외형/2D 화풍 인지는 남으나 원근·팔 겹침도 재해석되어 pure camera-only 아님; 미채택·reference-only. 12f 전파/애니메이션·밝고어두운 알파/실게임 연결 미검수. 기존 98% 파츠 작업 및 Live2D 중단은 유지.
2026-10-07 사용자 방식 전환/파츠 작업 삭제: 사용자가 “전체 파츠 작업 삭제”를 선택. QA/DirectionImpact-20260921/reina-combat-sd-v01의 두 parts-raw 폴더(279파일)와 normal-proportion-v01 내 파츠 작업 PSD 481개 제거, QA 트리 PSD 0/폴더 부재 확인. QA·원화·최근 GIF/시점 후보 보존. old 98% PSD 제작흐름은 자산 삭제되어 재개/달성 주장이 불가; 10분 자동화 `automation`은 PAUSED 상태 보존. 신규 전신시트/시점방식은 사용자가 선택했으나 최신 단일키포즈 후보는 비교전용·미승인이고 12f/게임 연결 미완료. 다음 진전은 이 한 장의 방향/외형 검수부터.

2026-10-07 현행 방식 보정: 사용자 요청으로 19_SD_COMBAT_MODEL_TRANSLATION.md#full-body-sprite-sheet-workflow-20261007 및 Reina PROMPT.txt에 전신 시트 기본방식/재사용 프롬프트를 기록. 시점 변경은 단일 전신 키포즈 검수 후 시트 확장, 실제 알파·프레임 연속성·reference-only 전투배치를 별도 검수. 현 키포즈 후보 미승인; full-sheet/loop/game integration 미완료. 기존 파츠 삭제와 자동화 PAUSED 유지.
2026-10-07 사용자 첨부 구도 승인 후 전신12f 후보 생성. 결과 v01 경계접촉 미채택 보존, v02 genuine RGBA 1448×1086 4×3/셀362, alpha≥32 셀경계접촉0이나 하단여백 최소4px, 1↔12 실루엣IoU93.33%, 포즈 반복·반동 급변 잔여. 상세 QA/PROMPT 경로 `reina-combat-sd-v01/REVIEW.md`, `PROMPT.txt` 참조. 후보는 루프/화풍·전체 애니메이션 미승인. 다음: 가장자리 여백·12 pose cadence 육안검수/필요한 단일 보정. GIF/전투배치/game 연결 미완료.
2026-10-07 요청에 따라 v02 시트로 one-shot GIF 생성: `normal-proportion-v01/gensprite-style-trial-12f-v08-approved-angle-attack-sheet/reina-attack-12f-one-shot-v01.gif`. 12f/362px cell; 80ms×11 + 300ms hold, total1180ms, no loop extension; reopen structural validation passed. GIF alpha binary cutoff `<128`; edge quality visual review still needed. Timing comparison-only, not gameplay cadence; runtime/battle scene 연결0. 세부 source/output hash는 same-folder JSON manifest 및 REVIEW 참조.
2026-10-07 개선 후보: v03은 9–12 하체 재해석/연속성 급락으로 보존·미채택. v04 상체미세편집으로 one-shot GIF v03 생성. 4×3 RGBA, alpha32 cell-boundary hit0, minimum bottom gap~3px (16px goal missed), adjacent mask IoU .8997–.9865, first/last .9490 vs prior .9333. 끝점 유사도는 다소 개선했으나 동작 변화가 여전히 작아 motion approval 보류; one-shot timing 80×11+300ms, no loop, GIF binary alpha. Paths, prompt, limitations in existing PROMPT/REVIEW. Next one-frame sequential pose test only, then assess adjacency; no game integration.
2026-10-07 광원 균일화 비교: user frame-flip observation confirmed as plausible; imagegen independently re-renders highlights/shadows, GIF quantization is not the sole cause. v05 RGBA + one-shot GIF v04-light-test saved; genuine alpha/edge hits0, hair ROI luminance spread slightly narrower but no exact lighting lock claim. Image edit itself may perturb drawing; candidate unapproved, visual flip/background check pending. Review/PROMPT contain details.
2026-10-07 v06 색감 통일 비교는 미채택: 고정 머리 ROI alpha≥200 평균휘도 편차 v05 1.47→v06 1.65, 평균은 더 어두워짐. 따라서 공통 색감 개선으로 기록하지 않으며 v06 GIF 미생성. 사용자 색감 이질감 관찰 타당; 공통 LUT/동일 보정은 미적용. 기존 REVIEW/PROMPT의 결과와 제한 참조, v04/v05/v06 후보 전부 보존.
2026-10-07 사용자 상시 지시: 모든 신규/수정 전신시트 후보에 후보별 review GIF 동시 출력, 미채택/실패 상태는 manifest/REVIEW에서 구분, RGBA PNG 보존 및 GIF palette/alpha 제한 명시. v06 색보정 미채택시트의 one-shot GIF 생성·재열기 검사(12f, 80ms×11+300ms, no loop, shared palette); 결과/제한은 Reina REVIEW/PROMPT 참조.
2026-10-07 v06 color-match GIF follow-up correction: alpha≥32 adjacent silhouette IoU ranges on fixed cells were v04 0.900–0.986, v05 0.939–0.988, v06 0.953–0.991; v06 frames are distinct but pose deltas became smaller. Its one-shot GIF stops after 1180ms. Do not present v06 as a motion improvement; v04 one-shot remains the clearer motion comparison candidate, still unapproved for loop/game. Full correction in Reina REVIEW/PROMPT.
2026-10-07 사용자 정정: 매 전달 때 동일 후보 버전의 PNG 12f sheet와 derived GIF를 한 쌍으로 함께 제공; GIF 단독 전달 금지. v04 pair는 REVIEW에서 모션 비교후보, 루프 승인 보류로 구분.
2026-10-07 temporal shading v07 comparison: two candidates v07a/v07b and matching GIFs/manifests preserved. v07a retained moderate motion but hair ROI luminance spread worsened to1.98; adjacent alpha32 silhouette IoU .741–.938. v07b spread improved to1.39 but pose continuity broke (.608–.992). Neither accepted. User intent recorded in 19 plan and Reina REVIEW/PROMPT; need a pose-preserving material-aware grade for the next attempt. PNG/GIF pairs are in the existing 12f v08-approved-angle folder.
2026-10-07 v08 combined pose/color attempt also failed: fixed hair ROI luminance spread2.19 (worse than v04 1.59); alpha32 edge contacts on frames4/7/8/12, pose IoU .616–.882. Matching PNG/GIF/JSON preserved but rejected. v07/v08 imagegen edits did not preserve both shading coherence and motion; next requires geometry-locked material-aware editing. Full evidence/prompt in existing Reina REVIEW/PROMPT; no runtime connection.
2026-10-07 user temporal-color correction applied to 19_SD_COMBAT_MODEL_TRANSLATION.md full-body sheet workflow and the current reusable base prompt in Reina PROMPT.txt. Material base colors/style and light logic remain coherent; pose-driven shadow/highlight changes allowed; no pixel-identical lighting, and pose/registration QA after edits required. v07/v08 remain rejected evidence.
2026-10-07 user update: all sprite-sheet review GIFs must loop infinitely (loop extension 0), superseding one-shot default for future outputs; preserve historical one-shot files. v09 sheet/GIF pair created from v08 color ref + v04 motion ref; 12×80ms/960ms, infinite loop, alpha32 edges0, adjacent silhouette IoU .894–.972, endpoint .976. Motion candidate, seam/color quality still separately reviewed; full evidence in Reina REVIEW/PROMPT.


<a id="d-kyle-shoulder-local-review-20261008"></a>
## D-KYLE-SHOULDER-LOCAL-20261008 — 사용자 첨부 어깨 경계 개선 / 착수 가능
사용자가 Plan의 프레임 분석 후 '개선작업시켜' 승인. 입력 C:/Users/dlstj/AppData/Local/Temp/codex-clipboard-24d0d853-385a-49c5-835e-48408661535d.gif (12f/640px). 검수 확대 D:/CodexData/Home/worktrees/205a/LASTRAIL/QA/KyleShoulderReview-20261008/shoulder-detail.png 및 shoulder-contact.png. 원첨부와 현행 프로젝트GIF 해시/버전을 먼저 대조; 이미 자기 v19/v20 국소보정이 진행/완료되어 있으면 같은 수정 중복생성 대신 최신본에서 해당결함 잔존을 비교하고 부족한 범위만 수정. （2026-10-29 정리: 이 옛 검수 경로는 현 위치에 없음. 현행 제작·런타임 입력에서 제외하며 최신 승인본으로 임의 치환하지 않음.）
첨부관측: 배턴측 오른어깨(화면왼쪽), 7프레임 소매상단 돌출/패임 및10→11 외곽변화. 고정견갑/몸통과 움직이는상완의 접속이 끊긴 인상. GIF만 본 원인추정(레이어/마스크 확정아님). 실제 원RGBA·국소원화·합성경계를 확인하고 견갑 아래로 이어지는 상완/소매의 숨은면과 겨드랑이 접속 보완. 어깨중심 등록/견갑앞가림/동작별 주름·그림자를 정합화; 경계blur나 전신stretch로 덮지 않음. 기존 얼굴·배턴/방호판·발·동작시간표 보존, 필요한 국소영역만 첫원본기반 편집·조립.
수용:1~12 전체 어깨접속/6→7→8·10→11→12·loop seam, 밝/어두운alpha와 실제 전투크기, 새국소재질의 플리커·파지 확인. 동일버전PNG시트+무한GIF 및 원본대비를 사용자에게 제시; 구조검사만으로 재생품질 합격 주장금지. 원본/실패보존, 자기QA/인계 기록후 이단위큐만정리. Reina/타요원·runtime·Live2D/automation 변경0.
2026-10-08 위 D-KYLE-SHOULDER-LOCAL-20261008 처리상태: 원첨부v20 SHA256동일 확인. v21 국소상단 소매윤곽 후보/12f PNG+무한GIF/전후비교/전투참고 저장, 정지12·밝어두운/alpha/구조loop 검수근거 REVIEW 최신절. 실제시간재생·moving재질 플리커·사용자미술승인 잔여로 이큐 유지. 동일 생성 반복하지 말고 v21 해당잔여부터 검수.

<a id="d-dual-range-animation-20261008"></a>
## D-DUAL-RANGE-ANIMATION-20261008 — 양 거리 대응 애니메이션 제작 / 사용자 직접 지시
사용자: ‘해당부분 반영된 애니메이션 제작하라고하고 레이나는 최종시안 idle 이랑 공격모션 애니메이션 검수체크한번하라고해’. 원본44#dual-range-basic-attacks-detail-v01-20261008의 요원별 수단을 반영한 시각 제작 착수 승인. 신규 계수0.8/사거리/취소판정·Server/Unity 구현 승인으로 확대하지 않는다. 제작정책19 최신 첫프레임 기반 방식 및17/개인바이블·자기PRODUCTION/REVIEW부터 읽고 기존 승인 원본/진행본 확인. 기존15인+후보2인 구분; 후보 신규ID/출시 확정0.
순서: 레이나 최종 Idle/공격 재검수 → 카일 현행 어깨후보의 잔여 검수와 근접/원거리 시각 대응 → 기본5인 나머지 Eve/Sera/Mira → 기존 나머지 승인 제작 범위에서 한 명씩. 진행 중 파일쓰기/생성을 안전경계까지 마친 뒤 순서 조정, 전원 일괄 재생성 금지. Kyle 단일 봉 내려치기/단일 에너지 탄, 원거리 FX는 캐릭터와 독립. 무기/탄/장치 제안은 승인된 제작후보로 표현하되 결과물 최종미술승인으로 승격하지 않는다. 장비 외형이 미정인 요원은 첫프레임 제시/검수 후 후속프레임, 기존 개인 승인 외형을 덮어쓰기0. 실제 피해사건/공격주기 source 미공급은 명시하고 프리뷰 시간표를 런타임 값으로 사용하지 않는다.
모드별 근접/원거리 전달방식·파지·준비/공격/회수와 공통Idle 접속을 표시. 권총/기관총 근거리 사격은 별도 접촉공격으로 위장하지 않고 같은모션 재사용 가능성을 먼저 검수. Kyle 어깨 v21을 정책변경만으로 폐기/반복생성0. 원본/실패후보 보존, 모든 신규 버전 PNG시트+무한GIF 쌍 및 검수근거/제작방식/독립FX/원본참조를 기존 QA·인계에 저장. 실제 재생/실크기/alpha/접속/루프/바람·재질플리커 검수, 구조검사만으로 시각완료 주장금지. 게임코드·최종화면 연결·Live2D·자동화·외부결제 추가 없음.


2026-10-08 D-DUAL-RANGE-ANIMATION-20261008 최신: Reina독립FXv01시각승인기록. Kylev23양거리PNG/무한GIF+청록독립탄+전투참고저장·nativev21RGBA0/12f1040msloop0/정지검수는REVIEW와dual-mode-v23.json 근거. Kyle미술/시간재생·팔플리커/Idle와나머지요원잔여로전체큐유지; runtime/자동화0.

2026-10-08 사용자앞쪽봉휘두르기수정: Kylev26공격PNG12f/무한GIF·독립탄동기화·전투참고저장. 기존REVIEW/v26.json에국소생성2회·v24급변/v25옆칸조각실패보존·v26연결요소정리/어깨정지·발/얼굴RGBA0/12f1040msloop0근거. v23동작이최종승인된것아님; v26미술/시간재생·팔플리커잔여/Idle미제작으로전체큐유지, 타요원한명씩.

2026-10-08 사용자 방패앞쪽 방향정정: Kylev26 왼쪽궤적불합격보존. v27국소팔1회생성→실제12연결요소/균일0.62/어깨185,148 및앞층가림으로 v28 가슴횡단→화면오른쪽 방패앞 후보 PNG12f+무한GIF 저장. REVIEW/v28.json에 전체정지·밝어두운/12f1040msloop0/하체·첫끝RGBA0/alpha누락·경계0 근거. 준비봉머리뒤가림·국소팔명암/접속·실제연속재생/미술승인잔여; FX미갱신(v26탄배치는새동작대응아님), runtime/다른요원/automation0. 다음v28궤적·접속검수후 독립탄등록; 전체큐유지.

2026-10-08 Kyle전신방식승인: 국소v28대신v30-fullbody PNG12f/무한GIF/JSON·밝어두운12저장. v05정체성참고→전신v29→중복봉/회수표적editv30(2call), 원실패보존. 균일1.4/관측오른boot568,584등록·순서수정·첫끝복사; 원본픽셀/양발고정주장0. 12f1040msloop0/alpha누락·출력경계0, 상체연동/방패앞횡단정지확인. 3→4/8→9전환·왼발/장비세부/시간플리커·사용자승인잔여. REVIEW/v30-fullbody.json참조, 다음후보검수후필요전신프레임보완; FX/Idle/runtime/타요원/자동화0, 전체큐유지.


2026-10-08 사용자 ‘에너지탄까지 하는거 넣어봐’: Kyle v30전신공격불변 + 기존독립청록탄v02를오른쪽진행으로반전한v03 재사용, 신규AI/캐릭터색보정0. Direction 한단위 v31조합reference-only. kyle-ranged-fullbody-v31-sheet.png4096×1920 / loop.gif1024×640 / v31.json, 독립right-v03.png96×39와flight12슬롯PNG, 발사정지/밝어두운4검수저장. 6번슬롯관측봉끝628,307에탄꼬리618,287등록→698/778/858,287직선진행; 1발1040ms프리뷰로실제발수/탄속/사건/피해권위아님. 전체시트와발사/밝어두운정지에서접속·오른방향/동일탄색확인. GIF재열기12f/loop0/기존시간표·alpha128누락0/alpha32경계0/첫끝RGBA0, charhashf3532156b2f96cb50d846cec83c7592d5292307443c0ec137664f3ee14f960c8 유지. 캐릭터v30회수급변/세부변화/접지·시간플리커잔여유지, 완전개선판정0. RGBA광원보존/GIFbinaryalpha한계. FX별도제어, 표적/명중/상태효과/전투재배치/runtime/Idle/타요원/자동화0. 다음v31사용자시각검수 후 모션잔여 또는KyleIdle 한단위, 전체큐유지.


2026-10-08 사용자8프레임봉중간위치정정: v31의8내림을7아래오른쪽→8가슴앞오른위→9올림 회수중간으로 변경. 기존8전신edit대상+7/9포즈참고 imagegen1회, kyle-recovery-frame08-raw-v32.png1254정사각·실프롬프트보존. 원캔버스균일640리사이즈/자동센터0, 전신v32시트8슬롯만교체, 다른11셀RGBA차이모두0. kyle-recovery-frame08-v32.json에관측봉끝7/8/9≈619,427→523,224→358,60/방식·잔여기록. 전후7/old8/new8/9및밝어두운789정지에서중간회수방향·견갑/팔연동확인,8기본재질색/조명지시적용·시간플리커최종판정0. 새8은전신재작화로머리/갑옷표면·발픽셀미세차존재, 원본픽셀불변주장0.
전신v32 PNG시트와에너지탄포함 kyle-ranged-fullbody-v32-sheet.png/loop.gif1024×640·v32.json저장. 독립청록탄v03/발사6슬롯/618→698→778→858,y287 및1040ms기존시간표유지. GIF재열기12f/loop0/alpha128누락0/alpha32경계0/첫끝RGBA0. 기존v30/v31/실패보존. 전체실시간재생/색감·접지·새8원근/사용자시각승인잔여, 게임타이밍/runtime/타요원/Idle/Live2D/automation변경0. 다음v32회수검수후해당잔여만보완, 전체큐유지.


2026-10-08 사용자붉은표시첨부 cf90cbf8 기준 대상정정: 실제대상둘째줄셋째칸=7슬롯, 앞선번호8설명보다시각표시우선. v32수정칸오해를정정, 최신7전신edit대상+6/8참고 내장imagegen1회로내린봉을수평6→완만오른위7→더높은8 중간회수위치로변경. raw-frame07-v33/프롬프트/수정JSON보존, 원1254정사각캔버스균일640리사이즈·자동센터0. 전신v33시트7칸만교체(기존8유지), 다른11칸RGBA0. 관측봉끝6/7/8≈628,307→577,229→523,224, 실제생성원근/팔/상체함께편집, 국소팔붙임0. 6-old7-new7-8밝어두운정지에서중간각/1봉/연결및재질큰색계열확인; 새7전신세부/발·시간명암플리커완전해소판정0.
에너지탄v03별도/6슬롯발사·기존궤적/1040ms시간표불변, kyle-ranged-fullbody-v33-sheet.png/loop.gif·v33.json저장. GIF재열기12f/loop0/alpha128누락0/alpha32경계0/첫끝RGBA0. v32·원본/실패보존, 실제시간재생·정체성세부/양발접지/잔여플리커·사용자미술승인미결; 게임runtime/다른요원/Idle/Live2D/자동화0. 다음v33회수/표시한칸사용자검수, 향후프레임번호보다첨부표시우선; 전체큐유지.

2026-10-08 사용자공격v33 최종시각승인·Idleㄱㄱ: KyleIdlev34 PNG12f/무한GIF1440ms/JSON·원프롬프트·밝어두운12/허리방패검수저장. 전신idleatlas1회/관측등록·첫Idle하체재사용으로y430+RGBA0, 방패끝잘림마스크수정후재검수; raw/승인공격불변. REVIEW/v34.json 근거12floop0/alpha누락·경계0/첫끝0. Idle사용자승인/시간플리커·연속재생/실전투크기·attack전환잔여; FX/runtime/타요원/automation0. 다음v34검수, 전체큐유지.

2026-10-08 사용자Idle허리끊김/극소소매지시: v34상하체390/430합성접속문제보존, v35첫전신고정+소매846px만시간주름RGB변화. imagegencontext1회/실외부변화폐기·재질톤±8/차±12제한. PNG12시트+무한GIF1440msloop0/JSON·확대QA저장, mask밖/허리y330+/alphaRGBA불변 및불투명누락0. 실제상체호흡/옷실루엣움직임없음, 시간소매플리커/사용자승인잔여. REVIEW/v35-local.json근거, 승인공격v33유지/다른요원·runtime·automation0. 다음v35극소Idle검수.

2026-10-08 Eve 첫준비자세v01: 내장imagegen 1회, 승인초상eve.png 정체성참고→eve-combat-v01/eve-first-pose-v01.png1254² RGBA/JSON/실prompt/밝어두운QA저장. 얼굴·브라운단발/고글·청회색짧은재킷·공구양손지지/허리케이스·양발연결 정지검수; alpha0~255/alpha32외곽경계0. 원 alpha>0 bbox21,0,1224,1244로상하여백좁음, 약한외곽픽셀 존재/미술승인미결. 장비는큐44양거리제작후보, 게임능력확정0. 다음사용자첫자세검수후여백/장비세부수정 또는첫프레임기준국소모션; 12f/GIF/전투배치/시간플리커검수아직없음. KyleIdlev35후보/공격v33·Reina승인본보존.

### Kyle Idle 국소추가 v36 — 2026-10-08
사용자 카일Idle국소부위추가요청으로Eve후속보류. v35두소매시간주름유지 + 방패아래짧은olive패널끝1372px허용영역 국소움직임. 실제first640 crop430,350~550,460참고확대→내장imagegen국소atlas1회1448×1086, 셀crop8,27~358,342/균일120÷350/원점430,350배치. 원context방패·다리재작화는미사용, 패널뿌리x480~494가중/명시방패·허벅지앞가림고정. 초기재질색filter가선화를구멍처럼잘라외곽노이즈생겨명시가림mask로수정후재검수; 최신확대에서해당점상혼입감소. 원그림과재질RGB중앙차[-5,-9,-5] 및시간correction±6적용, 전신늘림/허리가로분할0.
approved-angle kyle-idle-12f-sheet-v36-local.png2560×1920/loop-v36-local.gif640²/JSON·실prompt/국소raw·밝어두운12/패널확대저장. 전체12정지/밝어두운/확대에서1봉·방패·접속·발위치유지확인. v35대비패널허용밖RGBA0/하체y450+원첫RGBA0/12f120ms×12=1440msloop0/첫끝동일/alpha128GIF누락0. 첫/끝원v35복사라1→2/11→12국소재작화선화차이잔여, 실제시간연속재생/미세플리커·패널크기·사용자미술승인미결; 전체개선완료주장0. alpha바뀐범위패널만, 원본/승인공격v33·Reina/Live2D/runtime/automation변경0. 다음사용자Idle검수에따라추가국소천영역 또는패널접속/loop잔여보완.

### Kyle Idle 소매천외곽 v37 — 2026-10-08
사용자다음ㄱㄱ→v36이어배턴측소매아래천외곽1149px 국소편집. crop190,180~310,300 확대문맥참고/내장imagegen1회1254²atlas4×3, 셀상단62/셀폭square/균일120÷셀폭/고정원점190,180. 국소polymask·피부제외·뿌리top/right가중으로팔/견갑/몸불변, v35두소매내부변화+v36패널유지. 소매기본materialRGBshift[-19.5,-16,-11]/시간tone±5 제한, source후반보정상한도달하므로완전색플리커해소판정0. 새소매와패널첫끝강도[0,.35,.7,1,1,1,1,1,1,.7,.35,0]로급변완화, 중간포즈보간생성이라설명0. endpoint혼합약한외곽잔상/원선화차이실제시간잔여검수, 모든명암픽셀동일고정아님.
approved-angle kyle-idle-12f-sheet-v37-local.png/loop-v37-local.gif/JSON·실prompt/원atlas/밝어두운12·소매확대저장. 전체정지/밝어두운·소매확대에서천끝윤곽움직임/피부·견갑접속·1봉/방패/접지유지확인. taper후v36대비새소매mask밖RGBA0, 하체y450+원첫RGBA0/전체bbox157,83~602,599동일/alpha32경계0/alpha128GIF누락0/12f120ms×12=1440msloop0/첫끝동일검증. 사용자미술/실시간loop·국소플리커/실전투크기잔여, 후보상태. 원본/v35/v36·승인공격v33·Reina보존. Eve후속보류/runtime·Live2D·automation변경0. 다음사용자Idle확인후남은국소천 또는경계/플리커보완.

### Kyle Idle 팔윤곽복원 v38 — 2026-10-08 사용자정정
사용자 v37팔살늘어남지적. v37소매외곽부풀림이팔둘레/피부접속까지변형된인상을만듦; 밝은피부색filter·mask밖불변만으로인체형태유지검수불충분. v37미채택보존, 이전피부·팔윤곽유지설명은시각품질보장으로사용하지않음. 재생성없이원첫v35의배턴측팔전체문맥155,175~286,277 RGBA복원(native조합), 해당소매외곽및내부주름변화삭제. 반대소매미세주름/v36패널끝·endpointtaper는그대로, 다른영역새편집0.
approved-angle kyle-idle-12f-sheet-v38-arm-fixed.png/loop-v38-arm-fixed.gif/JSON·밝어두운12/팔확대저장. 수정전팔영역최대1318px변화→12모두원첫RGBA차0, 복원영역밖v37대비차0, GIF12f120ms×12/1440msloop0/alpha128누락0/첫끝동일확인. 팔확대12정지에서동일피부/팔·소매윤곽복원확인. AI이미지생성0/전신늘림0, 팔바람움직임납품주장0. 반대소매·패널시간플리커/연속loop·미술승인잔여. 원본/v37·공격v33·Reina보존/Eve보류/runtime/Live2D/automation변경0. 다음팔고정v38사용자검수, 팔형태바꾸지않는천국소영역만추가가능.

### Kyle Idle 미세호흡 v39 — 2026-10-08 사용자요청
사용자호흡살짝움직임승인으로 중앙흉부만추가. v38에원첫이미지흉부platepolygon 추출→sin²시각호흡 rigid상하최대1.4px native이동(스케일/몸warp0), vacated좁은252~303px경계에실제국소hidden직물edit1회1254²/원context250,230~450,430 동일200resize사용. plate는통합원그림에서추출한기존픽셀이며개별생성독립원화라설명0; 새로운hidden층만AI편집. 프레임별채색재생성0·원plate재질/중립광유지·bodyalpha원v38고정, 샘플링선화미세shimmer최종판정0.
approved-angle kyle-idle-12f-sheet-v39-breath.png/loop-v39-breath.gif/JSON·actualprompt/hidden원본·밝어두운12/흉부확대저장. 전체정지/밝어두운/흉부확대에서작은plate움직임과벨트/갑옷연결확인. 원팔155,175~286,277RGBA차12모두0, 허리340~390/285~435 v38차0, 허용흉부밖v38차0, bodyalpha불변/12f200ms×12=2400msloop0/alpha128GIF누락0/첫끝같음검증. 복부/몸전체호흡/전신리깅완료아님, 시간loop/plate경계·기존천플리커/사용자미술승인미결. v38/원본·공격v33/Reina보존·Eve보류/runtime/Live2D/automation변경0. 다음미세호흡v39사용자검수, 팔살/허리변형금지유지.

### KyleIdle v39 최종시각승인·Reina2인전투배치 v01 — 2026-10-08
사용자승인및레이나와배치요청. Kylev39.json승인기록갱신, 공격v33/ReinaIdlev29보존. 기존combat-final-reina-slot-clean-v01 941×1672 reference에firstslot정적인물제거 국소imagegenroofpatch1회/실crop450,470~650,670, 원HUDHP영역재사용. formerbodypolygon및595~610아래잔여다리mask만patch, 승인캐릭터원화재작화/색grade0. Kyle앞slotscale.18/foot400,584→545,580/origin473,475; Reina뒤slot기존.215/foot335,588→553,666/origin481,540. 공통좌표uniformscale·프레임자동center0.
approved-angle kyle-reina-idle-battle-v01.png/GIF 및 zoom-v01 PNG/GIF·contact/JSON/배경patch저장. 두승인sourceSHA JSON기록/Kyle200ms슬롯·Reina120ms슬롯유지,40ms공통sampling7.2초LCM무한loop0; GIF재열기동일프레임병합80f/7200msloop0확인. 전체전투정지/두인확대/12시간샘플에서배치/두원외형/발/바닥·가림확인, 천/미세호흡이실크기작게보이는표현비교이며실시간플리커완전판정0. 팔/배경외원캐릭터픽셀불변. reference-only조합·다른인물/HUD정적/256팔레트품질제한·실게임연결아님. 전체큐의타요원/실제사건검증잔여유지. 다음사용자전투배치검수또는Eve첫자세진행, 기존승인Idle모션은재생성하지않음.


<a id="all-fifteen-direction-scope-20261008"></a>
## 기존 D-DUAL-RANGE-ANIMATION-20261008 범위 보완 — 기존 15인 전원
사용자 “15인 다 해야해 ㄱㄱ” 직접 지시에 따라 [17 전원 제작 상세](../superpowers/plans_categories/17_OPERATOR_COMMON_STYLE.md#all-fifteen-production-design-animation-v01-20261008) 적용. 중복 작업 ID를 만들지 않고 기존 큐의 제작 범위를 보완한다.
- 대상: Kyle, Reina, Eve, Sera, Mira, Vex, Nyx, Iris, Sable, Marrow, Rail, Anchor, Lumen, Cinder, Warden. 린/사이퍼 후보 제외.
- 전원 첫 자세/Idle/근거리·원거리/일반/궁극기/피격/다운/복귀 명세와 Kyle·Sable 요격, Eve 센트리 명세 포함. 공용 가능한 양 거리 시트는 중복 제작 불필요. 신규 상세 7인의 얼굴·장비는 첫 시안 검수 대상.
- 현재 Kyle 최신 v49 승인 자세 기반 공격의 안전 경계까지 먼저 진행. 최신 왼손 봉/오른손 방패를 구 바이블 방향으로 되돌리지 않는다. Reina 승인 원본 보존 및 기존 검수 요청 유지. 이후 Eve/Sera/Mira/Vex/Nyx/Iris/Sable/Marrow/Rail/Anchor/Lumen/Cinder/Warden 순차 제작. 이미 공급된 후보를 먼저 대조한다.
- 첫 프레임 기준 후속 편집/조립, 동일버전 PNG/GIF/manifest 및 실제 크기 검수. 미승인 수치·사거리·게임 규칙 변경 금지. 새 첫 자세 승인 대기는 해당 요원에만 적용하며 다른 승인된 독립 작업까지 멈추지 않는다.
- 완료한 하위 작업만 자기 큐에서 정리하고 원본/검수/승인 근거는 기존 REVIEW·인계에 보존. 일부 요원 완료로 전체15인 큐 삭제 금지. 완료 답장/자동 중계는 요구하지 않는다.

### Eve 얼굴 보완 v03/v04 — 2026-10-08 후속 Direction
승인초상 operator-bible-portraits-v01/assets/eve.png 및 REVIEW 2026-09-22 승인절 실제 대조. 필독 확인 범위: 본 인계 마지막 오류후속절, 자기큐 all-fifteen,17 전원 공통/산출물/Eve절,22 얼굴·머리·장비·동작·검수 및 최신연결절,16 관련 품질절. 내장imagegen2회 얼굴보완 요청, v03/v04 원본·실prompt·JSON·reference-v02-v04 비교/밝어두운/120·160px QA 저장. v04에서 v02 대비 눈열림/올리브홍채·입가긴장·얼굴의카메라향이 승인초상에 가까워진 정지결과 확인; 코/하관·앞머리·고글 원근 차이는 남음. 얼굴98% 객관적척도/달성근거 없음. 머리만편집 지시에도 전신 재렌더로 장비/몸세부도 변화, 고정몸픽셀불변·독립머리원화 주장0. v04 1254² RGBA alpha0~255/alpha32외곽0 확인. 하단 여백 약41px로10% 목표미충족; 정지 시각후보이며 첫자세승인/Idle·공격12f·GIF·시간플리커·실제B배치 미완료. v01~03/기존Kyle49·56/Reina30·28 보존. 런타임/타요원/Live2D/자동화 변경0. 다음은 v04 정체성잔여·하단여백 보완 후 첫자세최종검수, 승인후첫프레임기준후속. 제작정책부터 읽고 시작; 초상승인을 새첫자세승인으로 승격금지.


### Eve 첫 자세 v03 사용자 최종시각승인 — 2026-10-08
사용자 첨부와 “이거로 승인” 기준. 첨부 SHA256 및 decoded RGBA가 eve-first-pose-v03.png와 완전 동일함을 확인; v04는 첨부와 다르며 미채택 후보로 보존. 승인원본 SHA256 24e81dbd4dbeed8d30e19c45af1843d9e02297c1931faa33e23fd54770900837 / v03.json 승인기록 저장. 이후 Eve Idle/양거리 공격은 승인 v03 얼굴·비율·장비·좌향 자세를 기준으로 필요한 부위 편집/조립. 추가 얼굴수정/98% 달성검증을 승인조건으로 유지하지 않음. 첫자세 미술승인과 alpha여백/후속모션·시간플리커·실제B배치·runtime 검증은 구분; Idle/공격 미제작 및 전체15인 큐 유지. PNG 원본 변경0, 기존후보/다른요원 보존.

### Eve Idle v01 후보 — 2026-10-08
사용자 ㄱㄱ에따라 승인첫자세v03 원본기준12f 제작. 얼굴/머리/고글·목·손/공구·허리/케이스·하체/발 고정, 밝은이너 흉부ellipse538,422~636,518 최대native1.5px 국소상하 및 뒤재킷끝791,471~843,517 최대3.5px제어(실최대JSON)만smooth taper 역샘플링. 상체전체/스켈레탈호흡아님, 전신프레임재생성0. 숨은재킷imagegen1회 생성은crop프레이밍이달라 미사용으로보존/실prompt저장; 새독립원화라설명0. 최종움직임은승인첫그림의기존픽셀조합.
Eve폴더 eve-idle-12f-sheet-v01.png/12개nativePNG1574²/loop-v01.gif640² 및dark-preview/JSON·밝어두운12/흉부재킷확대/120·160px 저장. clearpadding160/원점160,160, 원v03불변hash확인. 12f200ms/2400msloop0 재열기, 첫끝RGBA동일/허용영역밖nativeRGBA0·alpha32외곽0각12/GIFalpha128불투명누락0각12 검증. 정지확대·밝어두운·축소시형태/손허리접속유지확인. 실시간연속재생·미세선화shimmer/바람가독성·사용자미술승인 및실제B배치미검수, GIFbinaryalpha/팔레트한계유지. PNG/GIF일치쌍후보로제시, 후보승인아님. 다음 Idle검수및원v03기준양거리공격국소제작; 공격/센트리/스킬/실게임연결미완료, 전체15인큐유지/타요원·Live2D·자동화변경0.

### Eve Idle 머리끝 추가 v02 — 2026-10-08
사용자 ‘머리도 해야겟다’에 따라 v01 흉부/재킷동작을그대로 유지하고 뒤단발끝만추가. 승인첫v03기존픽셀 native국소역샘플링, 머리영역667,211~749,322/root x<=667 고정·sin바람연동 최대제어5.5/2.5px(실변위는mask가중), 얼굴/귀/고글/정수리뿌리·손공구발은고정. 새이미지생성0, 새독립머리원화라설명0. v01/원v03보존, 필요한영역조합방식유지.
Eve폴더 eve-idle-12f-sheet-v02.png/12nativePNG1574²/loop-v02.gif640²·dark-preview/머리확대PNG+무한GIF/JSON·밝어두운/축소 저장.12f200ms×12=2400msloop0/첫끝RGBA동일, 독립재검증 v01대비머리ROI밖RGBA0각12·얼굴고글0각12·alpha32외곽0·GIFalpha128불투명누락0 확인. 머리확대12정지에서뿌리접속/얼굴인상유지확인; 실제시간재생/미세머리선화shimmer·바람강도·미술승인/B배치잔여. 공격/스킬/센트리/실게임연결미완료·전체15인큐유지/타요원·Live2D·자동화변경0. 다음v02사용자검수 또는승인원v03기준양거리공격한단위.


### Eve Idle v02 머리늘림 사용자 거부 — 2026-10-09
사용자 ‘이건 억지로 늘렷다가한거같아’. 실제v02는기존머리픽셀국소역샘플링으로볼륨늘림인상발생, 자연스러운가닥동작검수완료로설명하지않음. v02.json 미채택기록/원파일보존, Idlev01복귀기준(별도미술승인아님). 원v03머리crop620,190~760,335 기준 내장imagegen1회 가닥굽힘/겹침 국소원화trialv03 및실prompt저장. 생성은뿌리/귀/팩세부·머리덩어리상대크기도변화하므로 즉시합성/시트채택하지않음, 실제animation미수정. 다음원v03고정뿌리와접속을맞추고끝가닥변화축소한 국소재작화/조합 후 동일버전PNG+무한GIF·선화/재질플리커/알파/얼굴고글고정검수. 후보한장으로수정완료주장0, 타요원/runtime/Live2D/자동화변경0.


### Eve 머리늘림 개선 Idle v04 — 2026-10-09
사용자 개선ㄱㄱ. 거부v02 broad elastic역샘플링폐기유지. 원승인v03에서위얇은가닥/오른외곽끝가닥만추출해 scale1 뿌리중심서로다른위상rigid회전(실각JSON), 기존선화·길이/두께유지방식. 전체머리warp0/새독립가닥원화주장0. 내장imagegen숨은머리면1회(underlayer-raw-v04/실prompt),140×145크기등록후기존가닥이비운좁은footprint에만사용; 생성귀/고글/팩세부미사용. root x<658 및y302+ 원crop복원, 얼굴/고글/귀·v01흉부재킷/손공구발변화0.
Eve폴더 v04 12nativePNG1574²/시트RGBA+무한GIF640²/머리확대PNG·무한GIF/밝어두운12/JSON 저장. 원hash불변·12f200ms×12/2400msloop0재열기·첫끝동일·머리ROI밖v01차0·얼굴고글차0·alpha32외곽0·GIFalpha128누락0각12검증. 머리확대/밝어두운정지에서가닥각차/고정얼굴확인. 이절은길이보존방법변경근거이며자연스러움최종승인/실시간플리커완전해소주장0, 뿌리/hidden접속·서브픽셀선화/연속재생·실B크기최종미술검수잔여. v02/실패후보/승인원본보존, 공격/센트리/runtime미완료/타요원·Live2D·자동화변경0. 다음v04사용자시각검수 또는필요가닥접속보완, 전체15인큐유지.

### Eve Idle 추가2가닥 v05 — 2026-10-09
사용자좋아이렇게몇개더ㄱㄱ: v04시각수용기록/원본보존. 기존2가닥·흉부재킷Idlev04유지+뒤정수리작은arc/하단짧은tip2개추가하여총4가닥. 원승인v03픽셀추출·scale1뿌리pivot서로다른위상rigid회전, 전머리elasticwarp0/독립재작화원화주장0. 숨은underlayer imagegen1회/실prompt/rawv05 보존,140×185등록후추가가닥이비운영역만사용. 추가허용native660,177~682,207 및686,275~707,307밖v04RGBA0각12, 얼굴/귀/고글·몸/기존가닥보존(접한root영역은기록mask범위만변경).
Eve폴더v05 12nativePNG1574²/시트+무한GIF640²2400ms/머리확대PNG+무한GIF/밝어두운·축소·JSON 저장.12f200ms/loop0·첫끝RGBA동일·원v03hash불변·얼굴고글차0·alpha32외곽0/GIFalpha128불투명누락0각12 재검증. 머리확대12정지검수, 새가닥root/hidden면접속·실시간선화플리커/동작강도·시각최종승인/B배치잔여유지. v05후보승인미확정·공격/센트리/실게임연결미완료, 전체15인큐유지/타요원·Live2D·자동화변경0. 다음v05사용자검수또는승인원v03기준양거리공격한단위.

### Eve Idle 머리폭확대 v06 — 2026-10-09
사용자 움직임너무작다/머리는크게허용. v05 4가닥 각각rigidscale1회전각제어 확대:위wisp22/외곽curve12/정수리arc10.5/아래tip13도, 실제최대·위상은v06JSON. 위wisp실최대약13.76도(구3.13), 외곽curve11.48도(구3.16). 머리크기/가닥두께늘림0, 얼굴/고글·몸/손/공구/발·v01흉부재킷보존. 기존AI숨은면v04/v05재사용·새imagegen0, 앞선후보보존.
Eve폴더 v06 12nativePNG1574²/시트PNG+무한GIF640²/확대PNG+무한GIF/밝어두운·축소·JSON 및2가닥중간base(참고) 저장. 상위haircontext y190→170확장/이동여백확대. 원v03hash불변·12f200ms×12/2400msloop0·첫끝RGBA동일·원v01대비hairROI658,170~760,312밖RGBA0각12·얼굴고글0각12·alpha32외곽0/GIFalpha128누락0확인. 확대12정지에서wisp큰각이동/얼굴유지확인; 큰각도에서뿌리/숨은면·인접가닥실루엣접속/선화시간shimmer는최종연속재생검수잔여, 자연스러움완전해결주장0. 후보승인대기/공격센트리실게임미완료/전체15인큐유지/타요원·Live2D·자동화변경0.

### Eve Idle 追加5가닥 v07 — 2026-10-09
사용자좋아5개더: v06큰머리동작수용기록/보존, 기존4가닥·몸그대로+정수리얇은arc2/왼단발끝3추가하여총9. 원승인v03가닥픽셀추출rigidscale1·서로다른phase회전, 늘림/신체재생성0/독립생성가닥원화주장0. imagegen숨은면1회 head-underlayer-raw-v07/실prompt,340×255등록후추가가닥이비운면만사용·생성얼굴/고글/귀/옷미사용. 5가닥각도/pivot/polygon/region·위상은v07JSON, 원v03hash보존.
Eve폴더v07 12nativePNG1574²/시트RGBA+무한GIF640²2400ms/머리확대PNG+무한GIF/밝어두운·축소·JSON保存. v06대비5추가ROI밖RGBA0·얼굴/고글차0각12·alpha32외곽0/GIFalpha128누락0·12f200msloop0/첫끝同RGBA 검증. 머리12확대정지검수; 추가가닥root/숨은면·연속재생선화shimmer/시간강도·시각승인/B배치잔여유지. v07후보승인미확정, 공격/스킬/센트리/실게임未完·全15큐유지/타요원·Live2D·automation0. 다음사용자v07검수후필요접속보완또는원v03양거리공격단위.

### Eve Idle 몸연동 v08 — 2026-10-09
사용자머리v07좋아/몸쪽움직임ㄱㄱ. 머리v07시각수용기록·9가닥/기존천동작 유지, 상체/머리/양팔공구공통 native상하최대4px·좌1.5px 호흡추가. 복부y565~690 smoothstep감쇠로허리연속, y690+하체발v07RGBA0각12. 원승인v03/전체비율보존·전신재생성0·새AI생성0, 실제방법은국소역샘플링이며스켈레탈리깅아님. 얼굴그림은상체와같이좌표이동, 이전절대좌표고정과구분.
Eve폴더v08 12nativePNG1574²/시트+무한GIF640²/상체확대PNG+무한GIF/밝어두운·축소/JSON保存.12f200ms2400msloop0/첫끝RGBA동일·원v03hash불변·alpha32외곽0/GIFalpha128불투명누락0각12검증. 상체12정지에서손공구/머리일체이동·허리연결/발유지확인, 복부taper·케이스고정부·선화샘플링shimmer/실시간연속재생·최종시각승인/B배치잔여유지. 공격센트리/runtime미완료/전체15큐유지/타요원Live2Dautomation0. 다음v08검수또는몸영역추가국소천/원v03양거리공격한단위.

### Eve Idle 소매/재킷끝 v09 — 2026-10-09
사용자v08좋아ㄱㄱ: 상체호흡v08시각수용기록,머리9가닥/기존몸동작그대로+소매아래clothfringe·후방jacketcorner2부위원v03픽셀추출rigidscale1회전(5/6도제어·위상차). 옷국소움직임후기존상체translation동일적용. 피부/팔두께/공구파지변형추가0, 전신재작화0. 숨은천imagegen1회/raw-v09·실prompt保存,230×200등록후비운천footprint만사용; 생성피부/장비미사용. v09-pre는중간참고조합이며납품본아님.
Eve폴더v09 12nativePNG1574²/시트PNG+무한GIF640²2400ms/상체확대PNG+무한GIF/밝어두운·축소·JSON保存.12f200msloop0/첫끝같음·하체y690+고정·alpha32외곽0/GIFalpha128누락0각12확인, v08대비추가천영역밖RGBA0·노출전완ROI차0각12. 전체상체12정지에서팔/손/허리/장비실루엣확인. 시간선화shimmer/숨은천접속·풍압강도·최종시각승인/B배치잔여유지. 원v03/모든이전후보보존/공격센트리runtime미완료/전체15큐유지·타요원Live2Dautomation0. 다음v09사용자검수 또는원v03근/원거리공격국소준비포즈.

### Eve Idle v09 최종승인 / 공격 주동작 key v01 — 2026-10-09
사용자좋아승인다음ㄱㄱ: Idlev09.json 최종시각승인 저장, 원PNG/9머리가닥/호흡·소매재킷원본고정. 공격착수: 원v03국소상체crop참고 imagegen12atlas1회→칸프레이밍/어깨스케일과몸통비례불일치라원전신에합성미사용 reference-only/원atlas·실prompt보존. 이어원v03전신을registration context로 양팔공구주동작 edit1회. 생성전체body결과raw보존, 실제사용은arm/tool/필요소매·앞이너polygon만이며바깥원승인픽셀복원; 새전신동작프레임전체교체아님. 공격peak2손파지/좌향공구제시, 독립펄스아직미제작.
Eve폴더 eve-attack-commit-local-v01.png/ready-commit-compare-v01.png/keypose-flip-v01.gif/commit-v01.json·실prompt保存. heady<360/하체y625+/mask밖RGBA차0 확인, 비교정지에서얼굴발유지·공구제시확인. 3keyflip은준비→주동작→준비대조reference-only,12f실공격애니메이션이라설명0. 주동작팔/손·어깨/feather2px앞이너접속·재질시각검수와중간준비/반동/회수국소프레임·독립FX·최종시트/GIF/시간플리커/실크기미완료유지. 다음원v03권위원본+commit접속기준으로중간프레임국소제작, raw연쇄재생성금지. 전체15큐유지/타요원runtimeLive2Dautomation0. 첫표현및Idle승인과공격/게임전체완료구분.


## 2026-10-09 Eve attack/pulse v02 local candidate
User approved commit and requested pulse: 좋아 ㄱㄱ 펄스도만들어줘. Idle v09 remains user-approved. QA root: QA/DirectionImpact-20260921/eve-combat-v01. Added independent eve-pulse-projectile-v01.png (builtin new raster, actual soft alpha, cropped only); independent fullcanvas flight PNGs/sheet and combined eve-attack-pulse-preview-v02.gif. Character sequence has FOUR distinct keyposes in NINE timed exposures, not nine independent drawings; preparation/recoil generated locally then mask-composited onto original v03. Face/head rectangle and y625+ lowerbody equality checked; original SHA unchanged. Recoil larger/higher than requested micro recoil: candidate pending visual review. Light/dark static review, GIF infinite-loop and 1320ms summed timing checked. No runtime/projectile speed/damage/event integration, temporal material/Idle transition unverified. Exact manifest eve-attack-pulse-v02.json; build-pulse-attack-v02.py owns assembly. Failed old atlas retained reference-only. Whole fifteen roster scope and pending tasks unchanged.

2026-10-09 Eve charge v04 user final visual approval / battle placement v01: user 좋아 이거로 승인 전투화면에 배치해봐. Approval registered in eve-charge-attack-v04.json; original media unchanged. Existing duo battle reference used; local builtin roof cleanup masked only former third-slot gunner/flare, original HP bar restored. Eve approved 4-key/15-exposure charge sequence+independent FX composed uniformly .1 at widecanvas origin451,636, no per-frame recenter/repaint. Kylev40/Reinav29 first frames remain static; other historical actors/HUD static. eve-charge-battle-placement-v01.png/GIF, zoom-v01.png/GIF and manifest/build saved in eve-combat-v01. Fullcolor zoom inspected: Eve on third roofslot, no overlap with Reina; source outer rendering and fixed foot registration preserved. GIF loop0/1680ms verified. Reference-only battle preview, not Unity gameplay integration; temporal palette shimmer/full gameplay integration unverified. Placement pending user review. Whole fifteen roster queue remains.

2026-10-09 trio battle v03: user corrected Eve undersizing and requested Kyle/Reina animation too. Eve uniform scale .1->.135 (+35%), wide origin413,591, center-foot approx560,765 retained. Enlarged body overlaps lower Reina silhouette edge in 2D preview; candidate user size/spacing review remains. Kyle approved v39 Idle200ms, Reina v29 Idle120ms now animated simultaneously; Eve approved chargev04 1680ms then Idlev09, final240ms neutral hold. 7200ms reference cycle sampled40ms; GIFs coalesced106frames, loop0/7200ms both verified. Fullcolor trio-eve-charge-battle-v03.png and zoom PNG plus full/zoom GIF/manifest/build saved in eve-combat-v01. No source art repaint, original candidate preserved, runtime/HUD/otheractor integration unchanged. Static enlarged trio inspected; temporal transition/art approval pending. Existing charge approval maintained; battle placement itself pending.

2026-10-09 correction trio v04: prior trio v03 incorrectly reverted Kyle49->39 and Reina30->29 despite latest handoff explicit prohibition. User identified Kyle mismatch. Corrected to approved Kyle49-panel-wind (native cell1254 normalized640, origin477,473), Reina30-upper-body; retained enlarged Eve/FX/time/foot placement. Full/zoom trio-eve-charge-battle-v04 PNG/GIF/JSON saved; loop0/7200ms verified, static three-actor PNG inspected. v03 preserved as superseded wrong-version preview; no claim it used final Kyle/Reina. Reference-only/game integration remains pending.


2026-10-09 user fixed exactly3 aim directions: directly adjacent car / forward target / rear target. Plan_6 explicitly authorized follow-up message sent, no polling/reply requested. Rear Reina local leftgaze/leftgun v01 visually approved by user for rear-target scope. Created two new firstpose reference-based redraws (not mirrored/cropped original claimed as new art): reina-adjacent-target-raw-v01.png and reina-forward-target-raw-v01.png. Wholebody angle-specific firstframes are candidates; face/module/coat detail continuity and common foot registration pending. Original rear raw preserved. Display-only alpha bbox crop/uniform scaling padded640, side-by-side comparison and per-direction PNG/manifest; new two angles do not yet have animation. reina-three-directions-review-v01.html visible browser opened, all3 labels present. Current source/outputs eve-combat-v01. Need angle firstpose review then source-based local animation. Exact class thresholds/runtime contracts left to Plan, no range/gameplay change invented.

2026-10-09 user approved elevated-camera adjacent/forward v03 and instructed animation. Approval recorded reina-camera-v03.json. Added both directions Idle12f1440ms and attack12f960ms, native1574 RGBA frames/sheets + independentFX sheets + dark-backedloopGIF and lightQA; build-reina-angle-animation-v01.py/manifest owns settings/hashes. Method original feathered distal hair/coat extracted pixels rigid rotated scale1, forearm+pistol rigid recoil up to3deg; exposed background transparent, no independently redrawn part claim. Original face/feet fixed (native1080+ equality), firstlastRGBAexact, alpha10borderzero, GIFloop0durationverified. First static lightQA inspected; existing muzzleFX found actualnonemptycell3 and corrected extraction before final export. Rootmask/hinge material seam and temporal quality remain user review; raw firstposeapproval does not approve resulting motion. Earlier/rear sources preserved, gameplay integration0. Animation-review-v01.html and battleloops created, browser blocked so deliver local media links. Whole15queue remains.

2026-10-09 user rejected new-angle Idle cut-fragment wagging; requests whole hair then sleeves stepwise. Old v01 Idle clips superseded for current production, preserve history. This unit adjacent whole ponytail root-to-tip extraction (not independent redraw) from approved camera v03, scale1 rotation ±1.7deg tied root672235; builtin tail-removal hidden backhead/shoulder support generated1 and consumed only vacated polygon. Original front scalp/bangs/face fixed, source body690+ fixed, old distal hair/coat motions removed. 12native1574 PNGs + RGBA sheet/whole/closeGIF1440loop0 and light12QA saved, firstlast/faceROI/body equality checked. Static lightQA reviewed; redtie/root/alpha/temporal seam pending userreview. Step1 only; forward whole hair and sleeve full structures remain pending, attack correction and runtime uncompleted. Accurate source/method in reina-adjacent-whole-hair-v02.json/build. Do not call whole hairstyle newly generated independent art or all Idle fixed.

### D-EVE-DRONE-01@eve-combat-drone-v1 / 승인 상세안 첫 외형·동반 자세 제작
선행 P-EVE-DRONE-DESIGN-01 완료: [22 승인 정책](../superpowers/plans_categories/22_EVE_VISUAL_BIBLE.md#eve-combat-drone-approved-v01-20261013), [22 상세안 §1–8](../superpowers/plans_categories/22_EVE_VISUAL_BIBLE.md#eve-combat-drone-detail-v01-20261013), [Plan 완료 근거](PLAN-D-MIGRATION.md). 사용자 2026-10-13 승인. 제작 정책부터 읽고 시작. 최신승인절이작성시점미채택문구보다우선.
Direction 소유: 기존 Eve 제작 QA/FrameStudio data에 별도 버전의 접힘/전개/강화 드론 외형·이브 동반 첫자세 후보, 실제 전투 크기 검토. 고정 객차 저고도 부양1기/단일헤드/양측밀폐포드/기존공구지휘/제한노란표식. 자유추격·새능력·기본인물전면재설계 없음. 실제앵커·전개동선 미확인은 표시, 외형후보를 런타임 연결로 설명하지 않음. 기존 이브1 승인 공격/이브3 후보·원본 보존. 새 시안 승인 전 Idle12장/전체 모션 확장하지 않음.
완료 조건: 단독 기체 정면/측면/후면 및 접힘 연결·전개/강화 구분, 인물파지/얼굴/실전투크기가림·독립구성 검수, 실제제작방식·큐ID/버전/경로·시각승인/미승인·미해결 앵커/동선 근거를 기존QA에 저장. 후속 등록 정의는 WAIT의 eve-combat-drone-queues-20261013. 자동채팅/폴링/배포 없음.
### D-KYLE-REINA-SIGNATURE-01@signature-design.v1.20261017 — 승인 제작 큐
등록키 KYLE-REINA-SIGNATURE:DIRECTION:20261017. 사용자 상세안 승인 후 ‘리소스 제작세션 큐에 등록’ 명시. 선행 Plan 상세작성 완료:32#kyle-reina-signature-design-detail-20261017 및 #signature-production-approved-20261017. 제작정책부터 읽고 시작:21 Kyle 최신장비/제작정책,20 Reina 최신승인원화/방향,17/19 적용절, 각기존QA의최신승인원본. 오래된원화번호를최신으로추정하지않음.
범위 Direction: 카일 방어접촉1장·레이나 발사순간1장의 FX OFF/ON 비교 후보. 카일 기존오른손배턴/왼팔방호판+낮은고정부/앰버전달광/작은접지반응; 레이나 기존권총/왼팔모듈+분절문양/응축광/탄흔적. 상세문서의 색·치수·장식시간은 비교검수값. 신규능력/피해반사·충격저장/탄추가·3D/전체인물재생성 없음. 고정물체는인물프레임에포함가능하나광/문양/먼지/탄흔적은독립제어alpha/절차효과로보존. 최신승인인물/원본이력보존,다른미승인후보자동승격0.
소유: Direction이기존개인QA와필요한20/21소비절만최소갱신.32/Unity원본편집0. 완료조건: 확대와실제5슬롯크기에서얼굴·양손·무기·발/경고가림,방향축/alpha/발광OFF 원본보존 확인; 사용원본/제작방식/독립요소/미검수기록 및 사용자후보제시. 문서채택을완성미술승인으로취급하지않음. 첫후보사용자검수전전방향/전체애니메이션/Unity후속선등록금지. 큐ID·근거를기존QA/인계에남기고해당원화제작단위완료후에만자기블록정리. 자동재개/폴링권한추가없음.
### D-MIRA-RECOIL-01@mira-signature-design.v1.20261017 — 첫 시안 제작
등록키 MIRA-RECOIL:DIRECTION:20261017. 사용자 ㄱㄱ 승인, Plan 선행 상세완료. 원본32#mira-signature-design-approved-20261017 및 #mira-signature-design-proposal-20261017. 제작정책부터읽고시작:24/17/19 적용절 및 기존미라QA 최신원본 확인. 소유 Direction 기존미라자산/QA, 필요24소비절만최소갱신.32/Unity/타역할파일편집0.
범위: 같은대기자세의기존총/낮은보정추하우징추가총비교+실발사순간1장 후보. 급탄함·왼손지지·얼굴과기존몸/머리보존, 실제최신승인원화가없으면후보임을명시하고승인원본이라칭하지않음. 좁은보호틈의내부왕복/작은반동과숙련된사수자세,기계적표현중심. 효과는독립alpha/절차제어; 고정부품통짜인물포함가능,인체파츠리깅강제0. 큰외골격/추가총열/네온룬/신규스탯·과열/발수추가없음.
수용: 동일자세확대및실5인크기에서총몸/급탄함/보정추구분·손접속·얼굴/무기가림·alpha·원본보존검수, 사용원본/제작방식/큐ID/경로·미검수기존QA기록,후보사용자제시. 첫시안검수전전방향/애니메이션/Unity선등록0. 기존카일레이나큐순서를임의삭제/대체하지않고진행중작업의안전경계후실행. 완료근거후자기단위정리,결과총괄답장.
## 현재 작업 우선순위 정정 — 2026-10-17 사용자 명시
사용자 ‘미라? 지금 이브 카메라시점때문에 애니메이션 다시 잡고있는데?’로 현재 리소스 작업을 정정. 이브 카메라 시점에 맞춘 애니메이션 수정은 해당 리소스 채팅 최신 승인/지시에 따라 계속한다. Plan의 직전 미라 계속 지시는 현행작업을 확인하지 않은 오류로 철회. MIRA-RECOIL:DIRECTION:20261017 및 KYLE-REINA-SIGNATURE:DIRECTION:20261017은 승인된 후순위 대기 작업으로 보존하며 현재 이브 작업을 선점하지 않는다. 기존 산출물 삭제/승인철회/세부 카메라 기준 재정의 없음.
KYLE-REINA-SIGNATURE:DIRECTION:20261017 주손정정: 위오른손배턴/왼팔방호판 고정은Plan의구21참조오류.17#all-operators-active-ultimate-motion-detail-20261017 및기존Kyle상세의최신승인원화우선적용. 최신보고왼손봉/오른손방패는실원본승인대조후확정,임의반전0. 방호팔/무기손역할로FX연결. 현재이브카메라작업우선유지; 전요원액티브궁극기보완기획은새일괄제작지시가아님.

### D-FOUR-MOTION-IMPROVE-01@four-motion-improvement.v1.20261017 — 승인 개선 제작
등록키 FOUR-MOTION-IMPROVE:DIRECTION:20261017. 사용자 ‘개선안 승인 리소스 작업 큐에 넣고’ 명시. 선행 Plan 검토·상세기획 완료:17#four-operator-existing-motion-review-20261017, 승인 기록 #four-motion-improvement-approved-20261017. 담당 Direction, 기존 네 요원 QA/FrameStudio 프로젝트와 필요한 소비 기록만 수정. Plan 원본/Unity 코드는 수정하지 않음.
제작 정책부터 읽고 시작:Tools/FrameStudio/PRODUCTION.md,17 해당 개선절·19 카메라/12프레임 기준,20/21/22/23 최신 적용절과 각 QA 최신 사용자 승인. 이브→세라 현재 작업 순서 유지. 진행 중 이브 수정에 해당 검수 기준을 합쳐 중복 후보 생성하지 않는다. 카일/레이나 기존 KYLE-REINA-SIGNATURE:DIRECTION:20261017과 동일 FX 비교는 한번만 수행하고 두 큐ID에 같은 근거를 연결한다. 기존 타 큐 블록 편집/삭제 없음.
착수 단위: 이브 최신 지휘의 손/드론 분리·충전/발사/회수 확인 및 필요한 최소 수정, 세라 v02 첫자세 카메라/체격/손 여백 비교. 세라 첫자세 승인 전 12장 확장 금지. 이어 레이나 조준/반동/회수 및 분절 문양, 카일 공격/방호 실루엣·잠금/하중 효과의 승인본 대 후보 비교. 새 액티브/궁극기8종 전체 제작은 이 큐에 포함하지 않는다.
완료 조건: 승인 원본 보존·수정 영역/방법·방향/프레임 기록, 확대/실전투크기 FX OFF/ON 및 정상/느린 연속재생 검수, 얼굴/손/발/총구 연결과 alpha/가림/첫끝 전이 확인. 신규12프레임 기준과 기존 승인24프레임 보존 구분. 실제 고유작화/반복노출·원사건과 프리뷰시간 구분. 후보 시각승인/미승인과 Unity 미연결 명시, 기존 QA/인계에 큐ID·버전·검증·남은 의존 기록 후 완료 단위 정리 및 총괄기획 결과 답장. 후속 Unity 실행 큐는 실제 자산승인/계약완료 전 선등록하지 않는다.
### D-FOUR-SKILL-MOTION-FX-01@four-skill-motion-fx.v1.20261017 — 승인 8종 제작 준비와 첫 포즈
등록키 FOUR-SKILL-MOTION-FX:DIRECTION:20261017. 사용자 네명 액티브/궁극기 상세안 승인 및 리소스큐 등록 지시. 선행 Plan 상세작성·승인 완료:17#four-operator-skill-motion-fx-detail-20261017 / #four-skill-motion-fx-approved-20261017. 이브→세라 진행 순서와 기존 개선큐 유지; 현재 작업 중단/중복 원화 생성 금지.
제작정책부터 읽고 시작:Tools/FrameStudio/PRODUCTION.md,17 승인8종·기존개선절,19 카메라,20/21/22/23 적용절 및 실제 최신개인QA. 특히 이브는 맨손지휘 최신원화, 카일은 실제 승인 손배치. 원본/원시간/12포즈/독립FX를 도구에 선등록하고 미결을 구분한다.
현재 실행 단위는 8종 자산·앵커·시간 의존 대조 및 승인 원화가 확보된 대상의 준비/정점/회수 첫 포즈 후보다. 세라 첫자세 승인·요원별 개선 자산승인·원시간 인수 전 해당 전체12프레임/4방향 확장은 대기 정의로 둔다. 레이나/카일 시간 미확인에 임의 ms 발행 금지. 인물/분절문양/방벽/도발/장갑/기체/치유/다운지원은 원상태에 맞춰 독립 제어.
담당 Direction 기존 각요원QA·FrameStudio 산출물만 소유. 완료조건: 사용원본·8종 준비상태·미결 계약·첫후보 검수 근거, 확대/실크기 FX OFF/ON, 손/총구/발 접속 및 수혜자/기체유무 분기 확인. 큐ID·버전·승인상태·미검수와 후속 등록 근거를 기존QA/인계에 남긴다. 전체8종 제작완료/Unity연결로 보고하지 않는다. 피격/다운/복귀 신규제안은 이 승인에 포함하지 않는다.
### 기존 승인 큐 소비 기준 정정 — SERA-LATEST-SEAL-SYNC:20261017
FOUR-MOTION-IMPROVE/FOUR-SKILL-MOTION-FX의 세라 소비는23 최신 직접사용자 결정 및17#sera-latest-seal-crystals-sync-20261017 우선. 부유수정3개/수인3안/마법진v02 승인,손촉매 허리보조수납. 옛촉매직접사격·손에든촉매 조작/보호 포즈로 신규제작하지 않는다. 세라 스킬2종/다운복귀의 최신수인 세부포즈 정합화는 Plan 잔여이며 해당부분만 대기,현재 직접지시된 수인공격 후보작업을 중단하지 않는다. 최종동작 승인/실전투검수와 디자인채택 구분. 타요원큐 범위 변경없음.
### 현행 제작 기준 반영 — SERA-EVE-CURRENT-MOTION:20261017
사용자 세라스킬 수인정합화/이브공구기획정리지시 완료.17#sera-eve-current-motion-reconciliation-20261017과수정된8종상세,22#eve-current-glove-authority-20261017,23#sera-current-skills-seal-sync-20261017을 기존FOUR-SKILL-MOTION-FX 큐에서소비. 앞 SERA-LATEST-SEAL-SYNC의 Plan 세라스킬포즈정합대기는해소; 실제원화/시간/첫포즈승인 선행은유지. 세라는수정3개+수인→치료/지원,이브는전술장갑→드론지휘. 옛촉매직접파지/공구접촉·공구펄스/케이스조작을새로제작하지않음. 기존큐D-EVE-DRONE의공구지휘문구보다이정정우선. 현재직접지시된제작흐름유지,새중복큐없음. 이브기본공격과독립장치발사판정결속은미확인;원피해/충전/장치수를표시로재정의하지않음.
### 방향 범위 정정 — DOWN-RECOVER-D2:20261017
사용자 다운/복귀2번만제작지시.17#operator-animation-inventory-20261017 우선:다운진입·유지·복귀는2번만,대기/기본공격/액티브/궁극기4방향유지. 기존전동작4방향문구로다운복귀를4세트생성하지않는다. 다운유지는마지막자세재사용,피격은색변화만. 기존원본보존. 이정정은현재승인스킬큐범위와진행순서를바꾸지않고다운복귀포즈후보전체미술승인을추가하지않는다.
### D-FOUR-REMAINING-MOTION-01@four-remaining-motion.v1.20261018 — 승인 잔여 동작 준비/핵심포즈
등록키 FOUR-REMAINING-MOTION:DIRECTION:20261018. 사용자 명시 승인 및 리소스 큐 등록. 선행 Plan 상세/승인 완료:17#four-operator-remaining-motion-detail-20261017 / #four-remaining-motion-approved-20261018. 제작 정책부터 읽고 시작:Tools/FrameStudio/PRODUCTION.md,17 해당절/다운복귀8종 최신정정/목록,19 및20~23 최신 승인/개인QA.
Direction 소유: 기존 네 요원 QA/FrameStudio 자산과 필요 소비 기록. 현재 이브→세라 흐름 보존. 기존 개선/스킬8종 큐와 중복 제작하지 말고 동일 검수 근거를 연결한다. 기존 승인 Idle/공격24프레임을 신규12로 재생성하지 않는다.
현재 실행 단위: 동작별 존재/승인/누락 대조와 원사건 매핑, 부족한 다운/복귀2번8종의 핵심포즈 후보 (2026-11-09 사용자 정정: 요격은 열차 전용, 카일 요격 제작 제외). 최신 승인 전신을 사용하며 미확인 자산은 먼저 식별한다. 이브 전술장갑/드론 지휘·세라 수인/수정3개 유지. 피격은 색만. 전체 프레임 확장은 개별 첫포즈/원사건 계약이 충족된 뒤 WAIT 후속 정의로 등록한다.
완료 기준: 원본/변경범위/실제제작방식/방향/큐ID를 기존QA에 기록; 확대/실크기 파지·발·alpha·가림과 다른방향→다운2/복귀2→실방향 연결 검토, 승인/후보/미검수 구분. Eve 기본공격-장치 원천 미확인은 해당 연결만 보류. 기존 담당 인계에 근거 남기고 후속 등록/저장 확인 후 자기 완료 블록 정리, 총괄기획 결과 답장. 닉스는 다섯 번째 선정만 확정, 이번 신규 상세의 전체 제작은 포함하지 않음.

### D-NYX-FIRSTPOSE-01@nyx-motion.v1.20261018 — 다섯 번째 요원 준비·첫 후보
등록키 NYX-FIRSTPOSE:DIRECTION:20261018. 사용자 닉스 상세안 승인/리소스큐 등록. 선행 Plan 상세완료·승인:27#nyx-fifth-motion-detail-20261018 / #nyx-motion-approved-20261018,17#nyx-fifth-production-approved-20261018.
제작 정책부터 읽고 시작:Tools/FrameStudio/PRODUCTION.md,27 본문 및 최신절,16/17/19 해당 성인비율·카메라·방향·발 기준,44 Nyx 기존 영역 계약, 기존 닉스 자산/QA의 최신 승인. 현재 이브→세라와 네 명 승인 작업 흐름을 선점하지 않는다. 미라 등 기존 큐 삭제 없음.
Direction 소유: 기존 닉스 제작 QA/FrameStudio 자산·필요 소비 기록. 실행 단위는 기존 승인 자산 유무/사용원본 확인, 2번 대기 전신 첫 후보와 기본/액티브/궁극기 핵심 실행 포즈 비교. 성인 여성·애시모브 중장발·청록 비대칭 코트·왼손 열린 타원 촉매·오른손 제어. 기존 최신 원본이 없으면 신규 후보로 표시한다. 인물/촉매 발광/영역 경계/실접촉광 독립, FX OFF/ON 비교. 첫 전신 방향 승인 전 전방향/전체프레임 확장하지 않음.
다운/복귀2번만, 피격색만. 영역 실제 경계가 미공급이면 reference-only 개념 비교로 명시하고 실제 판정 범위/반경을 발행하지 않는다. 기본공격 원천 미확인은 시각 후보 제작과 구별한다.32 신규 서사는 미승인, 새 조직 문양·과거 장비·은신/순간이동을 미술에 추가하지 않는다.
완료: 원본/방법/버전/큐ID·확대/실크기 손파지·촉매 빈공간·얼굴/발/경고 가림·alpha 검수와 후보/승인 상태를 기존 QA/인계에 기록. WAIT#nyx-motion-followup-20261018의 후속 등록 선행 확인, 충족 시 중복검사·저장확인 후 자기 완료블록 정리 및 총괄기획 결과 답장. 전체 자산/Unity 연결 완료 주장 금지.

### 닉스 기존 큐 서사 소비 정정 — NYX-NARRATIVE-CONSUMER:20261018
사용자 닉스 서사 승인. NYX-FIRSTPOSE:DIRECTION:20261018의 ‘32 신규 서사 미승인’은 해소하며32#nyx-narrative-approved-20261018 /27#nyx-narrative-consumer-approved-20261018을 소비한다. 현재 첫후보 제작 범위·순서는 유지. 침착한 영역술사 태도만 기존 포즈에 반영, 새 조직/장비/능력·CG·음성 작업 추가 없음. 기존 큐 중복 발행하지 않는다.

### D-FIVE-ULT-CINEMATIC-01@cinematic-five.v1.20261018 — 승인 컷신 준비·콘티 보드
등록키 FIVE-ULT-CINEMATIC:DIRECTION:20261018. 사용자 다섯명 연출안 승인·리소스큐 등록. 선행 Plan 상세/승인 완료:18#five-ultimate-cinematic-detail-20261018 / #five-ultimate-cinematic-approved-20261018,20/21/22/23/27 각 #ultimate-cinematic-detail-20261018.
제작 정책부터 읽고 시작:Tools/FrameStudio/PRODUCTION.md,16/18 및 각개인 최신원화/제작정책,04 승인 컷신 규칙. 기존 네명 전투모션·이브→세라·닉스첫포즈 진행순서 보존. 이번 큐로 진행 중 자산을 중단하거나 같은 전신을 중복 생성하지 않는다.
Direction 소유: 기존 요원 QA/FrameStudio 자산과 필요 소비 기록. 현재 단위는 최신 승인원본/부족원본 대조, 각5장면 콘티 보드·요약 안전크롭/레이어 계획. 승인 전신이 없는 인물은 구조 스케치/기존 reference-only로 콘티를 검토하고 최종미술로 표시하지 않는다. 약3초/0.9초는 승인된 비교 시간표, 실제스킬시간이 아님.
레이나 권총수렴/카일 방호하중/이브 정모·전술장갑과한기드론/세라 수인·수정3지원/닉스 열린촉매·곡선정렬. 구공구·손촉매·돌진공격 복원0. 인물/장비 고정재질과 독립FX·배경·전환·UI 분리. 피격모션 추가0. 음향은 타이밍 표기까지, 새녹음/음성합성 작업 없음.
완료: 사용원본·실제방법·고유작화/재사용·후보/승인상태·큐ID·시간표·독립요소 목록, 세로 safe area의 얼굴/양손/장비/스킵과 요약HUD 가림 검토를 기존 QA/인계에 보존. 개별 콘티 보드/원화 승인 뒤 애니매틱 후속을 WAIT 정의대로 등록. 실Player 미검수는 명시. 자기 완료블록 정리 전 후속등록/저장확인, 총괄기획 결과 답장.

### 궁극기 콘티 제작 방향 정정 — ORIGIN-STYLE:20261018
사용자 메이플6차 오리진스킬 컷신을 연출 기준으로 지정. FIVE-ULT-CINEMATIC:DIRECTION:20261018은18#ultimate-origin-style-20261018을 우선 소비한다. 표준전체화면은 캐릭터고유 전용배경·대표동작·카메라전환·독립FX를 결합한 시네마틱으로 기획/콘티화한다. 기존5장면 반신 중심구성을 정지그림 슬라이드 최종영상으로 확정하지 않는다. 기존요원별의미/최신외형/원스킬판정·기존작업순서는유지,공통3초강제없음·개별애니매틱검토. 전체영상새일괄제작전 개별보드/원화검수단계유지. OFF/DAILY반복요약은별도 유지. 원작고유자산복제0.

### 기존 궁극기 큐 참고 갱신 — ORIGIN-STORYBOARD-V2-REFERENCE:20261018
사용자 오리진형5인상세콘티재기획.18#origin-storyboard-common-v2-20261018 및20/21/22/23/27 각 #origin-storyboard-v2-20261018 작성. 기존FIVE-ULT-CINEMATIC의최신검토자료로연결,중복큐발행0. 사용자승인된오리진형방향은유지하되이번7패널/4.2~5.0초/요약1.0~1.1초는신규비교안이다. 구3초슬라이드형으로최종제작확정하지말고개별보드/정점키아트검수단계를따른다. 새전체영상/3D/음성제작자동확대0,기존진행순서보존.

### 전 요원 궁극기 공통 제작 기준 — ALL-ORIGIN-SCOPE:20261019
사용자모든요원궁극기오리진형기준확정.18#all-operator-origin-cinematic-approved-20261019/17연결절우선. FIVE-ULT-CINEMATIC 등 현재큐와후속요원궁극기모두전용배경/고유동작·카메라/독립FX/정점키아트/전장복귀를기준으로검토한다. 현재5인콘티/제작순서유지, 나머지요원은개별상세선행후실행단위등록. 공통4.2~5초/7패널강제없음. 전원최종영상일괄제작큐추가나기존자산승인자동승격없음.

### D-REMAINING-TEN-ULT-01@origin-storyboards.remaining-ten.v1.20261019 — 승인 콘티 보드·정점 후보
등록키 REMAINING-TEN-ULT:DIRECTION:20261019. 사용자 나머지10인콘티승인·리소스큐등록. 선행Plan상세/승인완료:18#remaining-ten-cinematic-approved-20261019 및연결원본24/25/29 #origin-storyboard-v2-20261019,17#remaining-seven-origin-storyboards-20261019.
제작정책부터읽고시작:Tools/FrameStudio/PRODUCTION.md,16/17/18 오리진형공통,각개인 최신정체성·실제승인원화/QA,44 실제궁극기. 대상 Mira/Vex/Iris/Sable/Marrow/Rail/Anchor/Lumen/Cinder/Warden, 후보Rin/Cipher제외.
Direction소유 기존개인QA/FrameStudio자산과소비기록. 현재단위는원본/미승인외형대조,10인각7패널보드·정점키아트후보1장·요약안전크롭/레이어계획. 개인최종외형없는7인은구조스케치부터검토하고신규성별/소속/무기최종확정0. 외형첫후보/콘티보드승인후애니매틱확장. 기존5인/이브→세라·닉스·전투모션큐선점/중복원화제작0.
피해감소태세/방벽/지속치료/정화예방/공격지연/표식/경직/연소를각원능력대로구분. Vex방어관통을다중적관통으로표현0,Anchor구ATK복원0. 인물/배경/FX/전환/UI독립,3D·Live2D재개/음성제작추가0.
완료조건: 사용원본/제작방식/큐ID/버전·실제고유작화와재사용·승인/후보/미검수기록,실제표시크기/세로안전영역/얼굴손장비·스킵/요약HUD가림·alpha/FX OFFON 검토. 기존QA/인계후WAIT#remaining-ten-cinematic-followup-20261019 선행확인/후속등록·저장확인후자기완료블록정리및총괄답장. 최종영상/Player완료로보고하지않음.

### D-FIELD-FX-READABILITY-01@field-fx-readability.v1.20261019 — 승인 독립FX 비교
등록키 FIELD-FX-READABILITY:DIRECTION:20261019. 사용자승인·큐등록,선행18#ultimate-field-fx-readability-approved-20261019 및앞상세절. 제작정책부터읽고시작:Tools/FrameStudio/PRODUCTION.md,16/18,대상개인최신승인정책/원화·QA,04 원사건수명. 현재진행중작업안전경계와우선순위유지.
Direction소유 기존개인FX/FrameStudio·QA. 승인된기존요소부터핵심/장식분리목록,방벽/태세/치유/다운지원/영역/표식/지연/예방/연소형태대조,실크기단독·중첩FX OFF/ON 비교후보. 전원새FX재생성0,기존사용가능자산재사용. 미공급실영역/원천은reference-only표시검토로구분.
필수위험·HP/다운·실탄/명중/영역경계보존,주변입자/후광부터감축. 팔레트/알파최종값은실검수후보로기록. 텍스트/수치/게이지굽기0·새숫자정책/히트스톱/3D/음성제작0. Unity/Plan원본동시편집0.
완료:원본·방법·큐ID/revision·독립레이어/미연결·실크기겹침검수·승인/후보를기존QA/인계에보존. 승인자산manifest/원수명계약확인뒤WAIT#field-fx-readability-followup-20261019에따라소비등록. 최종Player검수와구별,후속등록/저장확인후자기완료블록정리·총괄결과답장.

### D-COMBAT-RESULT-LABELS-01@combat-result-labels.v1.20261019 — 승인 결과 표시 후보
등록키 COMBAT-RESULT-LABELS:DIRECTION:20261019. 선행44#combat-result-labels-approved-20261019 및연결된기존피해/결과보완상세사용자승인. 제작정책부터읽고시작:16/18·현승인HUD/폰트/아이콘·Tools/FrameStudio/PRODUCTION.md 및대상QA. 기존제작순서유지.
Direction소유기존표시자산/QA. 기존폰트/아이콘재사용대조후HP피해/방패흡수/+회복/치명강조/제어차단의실크기후보·단독/연사/복합중첩비교. 숫자/문구는동적렌더링,이미지굽기0. 기호와색함께구분,큰전체문구/매타격섬광추가0. 이전FIELD-FX독립요소와중복생성0.
완료:원본/방법·독립요소·큐ID/revision·실크기/화면비/대상대응·읽힘검수·후보승인상태를기존QA/인계에기록. 표시시간/색/밀도는비교근거남기고임의합산/숫자누락정책발급0. WAIT#combat-result-labels-followup-20261019 자산후속등록확인후자기완료블록정리·총괄결과답장. 새3D/음성/Player완료주장0.

### D-HEALTH-DOWN-FEEDBACK-01@health-down-feedback.v1.20261019 — 승인 상태 비교
등록키 HEALTH-DOWN-FEEDBACK:DIRECTION:20261019. 선행44#health-down-state-feedback-approved-20261019 및앞상세절사용자승인. 제작정책부터읽고시작:현재승인HUD/개인QA,16/18·Tools/FrameStudio/PRODUCTION.md. 기존작업순서유지.
Direction소유기존카드/게이지·표시자산/QA. 기존프레임/아이콘/폰트재사용대조후 정상HP/부분방벽/실소진/자연만료/다운/지원후다운/실복귀/요원생존+객차파손/동시발생 비교후보. 원HP와후행장식·개인방벽과공유풀·요원과객차를区別. 숫자/残時間/동적게이지이미지굽기0. 다운복귀2번/피격색만유지,기존모션중복제작0.
완료:실크기/화면비·상태형태구분·입력영역/HUD가림·독립제어·원본/방법/큐ID/revision·승인/후보/미검수기존QA기록. 정확최종색/잔상시간은비교검수,새수리버튼/원복귀시간발급0. WAIT#health-down-feedback-followup-20261019 후속등록확인후완료블록정리·총괄답장.

### D-ULT-READINESS-01@ultimate-readiness.v1.20261019 — 승인 카드 상태·안내 후보
등록키 ULT-READINESS:DIRECTION:20261019. 선행44#ultimate-readiness-feedback-approved-20261019 및앞상세절사용자승인. 제작정책부터읽고시작:16/18·현승인HUD/카드/폰트/개인QA·Tools/FrameStudio/PRODUCTION.md.
기존순서보존,Direction소유기존UI자산/QA. 충전중/충전충분+적격/다운/행동중/대상없음/효과없음/미지원/상태확인 비교후보. 기존충전게이지·AUTO명칭·아이콘/프레임보존,조건불가를충전0처럼그리지않음. 기존안내영역과짧은문구사용,새상시긴문구/모달/버튼0. 동적문구/충전숫자굽기0.
완료:원본/방법/큐ID/revision·실크기/화면비·게이지와불가상태구분/문구가림·입력영역·승인/후보/미검수기록. 기존HP/결과표시큐와원본재사용. WAIT#ultimate-readiness-followup-20261019 자산소비후속등록확인후완료블록정리·총괄답장. 실제원적격/Player검수완료주장0.

### D-FOCUS-FEEDBACK-01@focus-feedback.v1.20261019 — 집중·가림·화면 이탈 승인 표시
등록키 FOCUS-FEEDBACK:DIRECTION:20261019. 선행 Plan 상세/승인 완료:44#focus-visibility-boundary-approved-20261019 및 연결 두 상세절. 제작 정책부터 읽고 시작:Tools/FrameStudio/PRODUCTION.md,16/18 독립 UI/가독성,기존 대상 표시 제작 정책·승인 시안/QA. 기존 이브/세라 등 진행 순서와 안전 경계 유지.
Direction 소유 기존 선택/HUD 자산·QA. 기존 집중틀과 실제 상태 표식, 참여 가능 카드 강조와 궁극기 준비 표시를 혼동하지 않는 독립 요소 비교. 현재 실행 단위는 자산 대조·기존 요소 재사용·실크기 후보/가림/화면 밖 안내 배치 검토. 새 초상화/자동 추적/가장자리 화살표/벽 뒤 투시 없음. 텍스트·숫자 굽기 없이 독립 제어, 선택틀이 입력 영역을 넓히지 않음.
완료: 사용 원본/방식/큐ID/버전·승인과 후보 구분, 집중+상태 표식+위험 경계/카드 준비 동시, 화면비/안전영역/alpha/표시 ONOFF 검수 근거를 기존 QA/인계에 기록. 최종 승인 자산 후 WAIT#focus-feedback-followup-20261019 후속 등록·저장 확인, 자기 완료 블록 정리 및 총괄 결과 답장. 실제 Player/전체 통합 미검수 구분. 다음 적 공격 예고 신규 제안은 범위 제외.

### D-ENEMY-TELEGRAPH-01@enemy-telegraph.v1.20261019 — 적 공격 예고 독립 표시
등록키 ENEMY-TELEGRAPH:DIRECTION:20261019. 선행44#enemy-telegraph-lifecycle-approved-20261019 및 앞 상세절 승인 완료. 제작 정책부터 읽고 시작:Tools/FrameStudio/PRODUCTION.md,16/18 가독성·독립 요소, 해당 적/기존 위험 표시의 최신 승인 시안·제작 정책/QA. 진행 중 자산 작업 순서와 안전 경계 보존.
Direction 소유 기존 경고/아이콘 자산·QA. 현재 실행 단위는 사용 가능 자산 대조, 요원/객차/기관차 대상 구분, 예고/발동/취소 상태·집중/실표식과의 독립 표시 비교 후보. 실제 geometry 없으면 대상 표식까지, 추정 반경/예고 시간·벽 뒤 투시/새 화살표 없음. 수치/문구 이미지 굽기 없음, 실제 경계와 장식 후광 분리.
실크기 겹친 두 경고 중 하나 종료·집중/표식/HP 동시·장식 가림·화면비/alpha/색 이외 형태 구분 검수. 기존QA/인계에 원본/방식/큐ID/버전·후보/승인/미검수 기록. WAIT#enemy-telegraph-followup-20261019 후속 선행 충족·중복/저장 확인 후 자기 완료 블록 정리 및 총괄 결과 답장. 최종 Player와 전체 통합 완료 별도. 다음 전투 종료 표시 신규 제안은 제외.

### D-COMBAT-TERMINAL-01@combat-terminal.v1.20261019 — 종료·결과 전환 표시 비교
등록키 COMBAT-TERMINAL:DIRECTION:20261019. 선행44#combat-terminal-presentation-approved-20261019 및 앞 상세절 승인 완료. 제작 정책부터 읽고 시작:16/18 독립UI·가독성,04 기존 결과/정산 표시, 해당 종료/결과 화면 최신 승인 시안·제작정책/QA. 현재 진행 자산과 순서 유지.
Direction 소유 기존 HUD/종료/결과 자산·QA. 현재 실행 단위는 기존 자산 대조·마지막 타격/원종료/결과 확인/지급 대기 상태 비교. 프레임·문구·수치·인물·FX 독립, 새 승리 모션/전체 화면 재제작 없음. 종료 후 집중·준비·위험 표시 해제, 최종HP/다운과 실제 결과 유지. 사라지는 탄에 가짜 명중/폭발 표시 없음.
실크기/화면비/결과 입력 영역과 마지막 숫자·잔류FX 가림 검수, 지급 미확인을 지급 성공처럼 표현하지 않음. 기존QA/인계에 큐ID/버전/원본·방법/후보·승인·미검수 기록. WAIT#combat-terminal-followup-20261019 후속 선행/중복/저장 확인 후 자기 완료 블록 정리·총괄 결과 답장. 실Player 통합은 별도. 다음 상태 효과 지속/중첩안은 신규 제안으로 제외.

### D-STATUS-FEEDBACK-01@status-feedback.v1.20261019 — 상태 효과 독립 표시
등록키 STATUS-FEEDBACK:DIRECTION:20261019. 선행44#status-effect-duration-feedback-approved-20261019와 앞 상세절 승인. 제작정책부터 읽고 시작:16/18 독립UI/가독성,기존 해당상태 아이콘 제작정책·최신 승인 시안/QA. 기존 진행순서 유지.
Direction 기존 상태아이콘/HUD·QA 소유. 현재 단위는 기존자산 대조·독립아이콘/시간/중첩숫자·자연만료/정화/예방권 비교후보. 실효과 없는 신규능력 표현0,숫자굽기0. 위험/HP/다운/집중과 실크기 중첩·안정배치·색외형태·입력 가림/alpha 검수. 최대노출수·새팝업 임의확정0.
기존QA/인계에 큐ID/버전/원본/방법·후보/승인/미검수 기록. WAIT#status-feedback-followup-20261019 후속선행 충족·중복/저장확인 후 자기완료블록 정리·총괄결과답장. 최종Player통합 별도,다음 배속/정지표시안 제외.

### D-PAUSE-SPEED-FEEDBACK-01@pause-speed-feedback.v1.20261019 — 배속·정지 상태 비교
등록키 PAUSE-SPEED-FEEDBACK:DIRECTION:20261019. 선행44#combat-pause-speed-feedback-approved-20261019/상세절 승인. 제작정책부터 읽고 시작:04 §14.2~14.4,16/18 독립UI,기존 배속/정지/메뉴 최신 승인 시안·제작정책/QA. 현재 작업순서 유지.
Direction 기존 HUD/메뉴·QA 소유. 현재 단위는 기존 자산 재사용 대조와 선택배속/실정지·컷신+메뉴·재개/실만료 안내 비교. 컷신에 중복배너 추가0,분기travel대기와 전투전체정지 구별. 문구/수치/아이콘 독립,최종미술 임의승격0.
실크기/화면비/입력영역·기존HUD가림/상태구분 검수,큐ID/버전/원본·방법/후보·승인·미검수 기존QA/인계기록. WAIT#pause-speed-feedback-followup-20261019 후속선행·중복/저장확인 후 자기완료블록 정리·총괄답장. 실Player 통합 별도. 다음 기본공격대기 신규안 제외.

### D-SELECTED-RANGE-01@selected-range.v1.20261019 — 요원 선택 범위 후보
등록키 SELECTED-RANGE:DIRECTION:20261019. 선행44#operator-selection-range-approved-20261019 및 정정절. 제작정책부터 읽고 시작:16/18 독립표시,기존 전장 선택/범위 최신승인시안·제작정책/QA. 기존 진행순서 유지.
현재 단위: 기존사용가능자산/절차경계 대조,선택1명 기본공격범위 실크기 후보. 얇은경계/낮은농도면과 근접내부경계는 비교후보,최종승인으로표시0. 원객차지원영역/고저차/앵커와 제공된 geometry 기준,미공급은 reference-only. 전원24m복사/가짜전역원0. 기존Idle 유지,상시대기이유 문구/아이콘 제작0.
입력영역과 분리·위험/HP/탄 가림·화면비/alpha·선택교체/숨김 검수. 원본/방식/큐ID/버전/후보·승인·미검수 기존QA/인계 기록. WAIT#selected-range-followup-20261019 선행충족 후 후속중복/저장확인·자기완료블록정리·총괄답장. 실제Player통합별도,다음 입력제스처안 제외.

### 기존 선택 범위 큐 정정 — SELECTED-RANGE-INFO:DIRECTION:20261019
사용자 직접 요원정보보기+장착장비아이콘 지시. 기존 SELECTED-RANGE:DIRECTION:20261019는44#operator-info-equipment-correction-20261019 / info-equipment.v2.20261019를 우선 소비한다. 별도부모기능 중복생성 없음. 요원클릭시 정보보기 상태로 사거리+실장착3슬롯 아이콘 동시표시/요원교체시함께교체/해제시함께숨김. 현재run 실제장착원본·revision 소비,미확인과빈칸 구별·미리보기/가방품 대체0. 정상Idle·원전투유지,장비교체/해제명령 추가0.
Direction은 기존 정책/승인시안 확인후 독립 슬롯틀·장비아이콘 재사용과 실크기배치 후보/가림검수. Unity는 기존선택Controller→정보/범위View 결속·장비owner 읽기전용 소비·옛선택응답격리/구독수명과0/1/3장비·빠른선택전환·실장착갱신검수. 각자 기존소유파일만 편집,미공급만보류·기존진행순서유지. 기존QA/인계에 v2 적용/실근거/잔여를 기록한다. 이전WAIT후속키를 재사용하며 최종통합은 장비/범위 동시읽힘까지 확인. 새선택제스처/아이콘상세팝업 자동구현0.

### 요원정보 장비 상세 추가 — SELECTED-RANGE-DETAIL:DIRECTION:20261019
사용자 직접 장비아이콘 상세보기 승인. 기존 SELECTED-RANGE:DIRECTION:20261019에44#operator-info-equipment-detail-approved-20261019 / info-equipment.v3.20261019 적용. v2의 아이콘상세 자동구현금지는 이번 명시승인으로 해소. 기존입력제스처의 미승인부분은 유지.
장착아이콘→읽기전용 상세1개→닫기시 같은요원정보/범위 유지. 이름/성급/기본옵션·고유/인챈트확정값/조건·원세트상태 공급분 표시,13 최신정책/기존상세 재사용. 빈칸상세0·미확인/실패구별·장착/판매/합성등 거래버튼0·보기로전투정지0. Direction 독립기존아이콘/프레임/동적문구 배치후보와가림검수,Unity 기존Controller callback/실instance·revision/선택세대·구독수명 소비. 요원변경/아이템교체/종료시 상세닫기·늦은응답차단,닫기뒤입력전달0.
각역할 소유파일/진행순서 유지,동일부모 중복구현0. 기존QA/인계에 v3근거·미공급·검수기록 및 총괄답장. WAIT 기존후속키 재사용,최종통합수용에 상세조회/닫기/응답역전/읽기전용불변·실Player추가. 신규부모큐 없음.

### 정보창 닫기 입력 승인 반영 — SELECTED-RANGE-NAVIGATION:DIRECTION:20261019
기존 SELECTED-RANGE:DIRECTION:20261019에44#operator-info-navigation-approved-20261019 / info-navigation.v4.20261019 반영. 사용자닫기입력안 명시승인으로 앞신규제스처미승인제약은해당상세범위에서해소. 장비상세닫기/뒤로가기 한단계복귀,바깥전장첫클릭 상세만닫기·동일입력뒤전장명령0. 요원정보닫기/같은요원재클릭/빈전장해제시 정보+범위숨김/적집중기존정책유지,다른요원교체·적클릭집중과정보유지.
기존UI/지원/안전메뉴·드래그계약보존,다운동일요원정보유지/퇴장·종료닫기,컷신차단/유효상태복귀·옛입력/비동기결과차단. Direction 기존닫기/상세배치독립요소·실크기입력/가림비교,Unity 기존Controller상태·입력소비/Bind수명·실Player검수. 새중복부모/전역입력체계0,진행파일안전경계유지. 기존QA/인계에v4근거/잔여 및총괄답장,WAIT 기존후속키재사용. 다음요원요약이름/HP배치안은미승인으로제외.

### 요원 정보 기본 구성 승인 — SELECTED-RANGE-SUMMARY:DIRECTION:20261019
기존 SELECTED-RANGE:DIRECTION:20261019에44#operator-info-summary-approved-20261019 / info-summary.v5.20261019 적용. 이름/기존승인초상·현재최대HP·독립개인보호막·장비3슬롯,전장범위 유지. 다운/복귀는기존표시계약재사용,공유보호막을개인값으로복제0·미공급값0추정0·새전체스탯표/중복효과목록0.
Direction 기존자산/독립트랙·수치/아이콘·시안비교/실크기가림검수,Unity 원Vitals/장비owner 읽기전용입력·같은선택세대로이름HP장비일치/원revision보존·구독수명·실Player검수. 실제미공급부분만대기,기존정상표시경로재사용. 기존QA/인계에v5근거/잔여와총괄답장. WAIT기존후속키재사용·같은부모중복구현0·진행작업안전경계유지. 새장비상세스크롤/레이아웃안은미승인범위로제외.

### 장비 상세 배치·스크롤 승인 — SELECTED-RANGE-LAYOUT:DIRECTION:20261019
기존 SELECTED-RANGE:DIRECTION:20261019에44#equipment-detail-layout-approved-20261019 / equipment-detail-layout.v6.20261019 적용. 상단아이콘/이름/성급/닫기고정,기본옵션→고유→인챈트→세트본문스크롤·조건/긴설명생략0. 다른instance전환처음부터/같은장비실값갱신읽던위치유지·유효범위보정. 패널스크롤 뒤전장/카메라전달0·스크롤끝놓음닫기오인0.
Direction 기존독립자산/안전영역·실크기긴설명/닫기·필수전장가림 비교,Unity 기존상세View/Controller/스크롤수명과교체/종료/컷신/조회갱신검수. 정확px/폰트는미술검수로확정,새전체화면관리창/거래명령0. 부모중복구현0·진행안전경계유지. 기존QA/인계v6근거/잔여·총괄답장,WAIT기존후속키재사용. 다음세트충족표현세부는신규제안으로제외.

### 세트 충족 표시 승인 — SELECTED-RANGE-SET-STATUS:DIRECTION:20261019
기존 SELECTED-RANGE:DIRECTION:20261019에44#equipment-set-status-detail-approved-20261019 / equipment-set-status.v7.20261019 반영. 해당장착자 실제세트개수·1/2/3누적단계 충족/미충족 체크/문구·미충족설명읽힘. 조건충족과실발동구분,타요원/가방/미리보기비합산,View세트재계산0. 원인스턴스유지시다른슬롯확정변경갱신/원아이템교체시상세닫기·스크롤수명유지.
Direction 기존세트기호/독립문구/대비·실크기검수,Unity 기존원집계/상세View수명·A3/A2+B1/A1+B1+C1/미공급/응답역전/Player검수. 각담당소유파일/진행안전경계유지,같은부모중복구현0. 기존QA/인계v7근거/잔여·총괄답장,WAIT기존후속키재사용. 다음인챈트본문배치는신규제안으로제외하되기존13의승인EN17표시정책은유지.

### 인챈트 상세 배치 승인 — SELECTED-RANGE-ENCHANT:DIRECTION:20261019
기존 SELECTED-RANGE:DIRECTION:20261019에44#equipment-enchant-inspection-layout-approved-20261019 / enchant-inspection.v8.20261019 반영.13 기존EN17승인표시정책 재사용. 장비성급vs시술성급/이아이템저장수치·실장착자적용/중복상태/원조건설명 배치. 낮은중복의수치를높은값으로덮기0·장비전체무효표시0·보조대상없음에적용배지점멸0. 미지원원천/다른인챈트규칙추정0.
Direction 기존독립문구/상태기호/숫자배치·긴설명검수,Unity 기존원선정instance/출발원본·상세View/스크롤수명소비.1성시술→2성장비/15vs25/동률·미공급/출발후인벤토리변경·선택교체 검증. 기존EN17유효근거재사용·변경View검수추가,진행파일안전경계/부모중복구현0. 기존QA/인계v8근거/잔여·총괄답장,WAIT기존후속키재사용. 다음정보준비/재시도표시세부는신규제안으로제외.

### 기존 정보표시 범위 축소 정정 — SELECTED-RANGE-MEMORY:DIRECTION:20261019
사용자 로딩/조회실패/재시도UI 제외 직접지시. 기존 SELECTED-RANGE의44#operator-info-memory-direct-approved-20261019 / info-memory.v9.20261019 우선. 요원/장비메모리정보즉시표시,해당조회로딩·오류·재시도패널/버튼 제작없음. 기존v3등 조회실패표현문구보다이정정우선. 실제빈슬롯/미부여는정상표시유지. 새로운제작큐없이기존승인정보/상세/독립자산작업유지.

2026-10-20 담당 이관 — D-FIVE-ULT-CINEMATIC-01 및 연결 컷신 큐의 닉스·이브 범위는 LAST RAIL_Cutscene_1(01a1586f-b110-7b31-81c2-20e54d73b0e8) 전용 담당으로 이관. 신규 산출물 QA/CutsceneProduction-20261020/nyx 및 eve는 해당 담당 소유. 본 세션은 편집 중인 컷신 파일 없음, 전투 원화/애니메이션 계속. 카일·레이나 컷신은 원화/애니메이션 정리 전 보류, 세라는 이번 우선 제작 제외. 다른 요원·전투 큐는 유지.

### 초상화 화풍 변경 후보 — PORTRAIT-STYLE:DIRECTION:20261023
사용자 이번 UI 시안 초상화 화풍 선호 및 리소스 담당 제작 지시. 참조 QA/SideViewReuse-20261023/ui-layout-v01/A.png,B.png,C.png는 선·채색·명암·얼굴 가독성의 화풍 참고만. 생성 시안의 잘못된 머리색·성별·복식·장비/ID 매핑을 원본 설정으로 채택하지 않는다. 기존 17 character-registry와 최신 승인 외형, Tools/FrameStudio/PRODUCTION.md 및 대상 제작정책부터 확인. 기존 요원 정체성을 보존한 UI 정면 초상화 스타일 후보부터 제작; 우선 현재5인 중 대표1인 비교로 공통 화풍을 확인 후 확대. 프레임/배경/글자와 분리한 alpha 인물, 실제 카드크기 및 확대 검수. 완료조건: 정확한 승인 원본/버전과 시안 비교·기존 QA 기록·사용자 후보 제시. 이번 요청은 전투 애니메이션/전신/컷신 전체 재제작 승인이 아님. 외형 변경 필요 발견 시 구체적인 불일치만 기록하고 임의 변경하지 않는다. Direction 원본 소유, Plan은 런타임/미술 파일 편집하지 않음. 기존 작업 안전경계에서 진행.


### LANDSCAPE-D2:DIRECTION:20261023 — 사용자 가로 D안·2방향 전환 승인
선행 정책 완료:44#landscape-two-facing-approved-20261023 및17#landscape-d2-animation-approved-20261023. 이번실행: 제작정책/기존큐의4방향 신규확장을 안전경계에서 중지·보존하고2번/반대 총2방향 생산표로 전환, 5인 대표포즈와 풍향/손장비/소켓/발 접지 검수. 현재개인QA에 작업파일소유·원본기록, Tools/FrameStudio/PRODUCTION.md 최신우선 안내는 Direction만 편집. 레이아웃 원본 QA/SideViewReuse-20261023/ui-layout-v01/D-controls.png. 완성합성이미지실행UI금지; 노즐/틀/핸들/라벨/HP/카드/초상/기능아이콘 독립화 필요요소대조. 초상화화풍큐 동시유지, 얼굴/색/의상 ID보존. 컷신/다른자산의무전면재제작0. 이큐의완료는제작목록/대표검수·공급가능계약 근거까지이며 모든클립/Unity제품완료아님. 과거D1/3/4큐를별개로계속실행하지 않도록 최신예외 우선. 후속전량제작/실소비는 원선행충족 후등록.

### LANDSCAPE-D2 추가승인·충돌 점검 반영 — 2026-10-23
기존 LANDSCAPE-D2:DIRECTION:20261023의 최신소비는44#landscape-curve-roof-artillery-20261023 및 뒤승인갱신,44#landscape-enemy-train-boarding-proposal-20261023 및 뒤승인갱신을 함께 적용. 17#landscape-d2-animation-approved-20261023을 Plan이 이동승인/5인70자리 기준(2026-11-09 사용자 정정: Kyle요격 제외, 열차 전용)으로 정정·재열람확인. D2 기존QA reina-direction2-underlap-v14/qa.json의 landscape_d2_conflict_recheck_20261023은 전체PNG미러의풍향/비대칭장비 충돌,발bbox와실앵커구분,소켓/독립그림자/HUD미확인 기록. 대표참고반전만으로공급완료처리0. 공용세로4방향뷰어는가로제품검수증거아님. 각담당기존QA와실자산검수계속,역사원본보존.
### ALL-SCREENS-LANDSCAPE:DIRECTION:20261023
사용자전체화면가로전환및담당충돌병합지시. 33#all-screens-landscape-approved-20261023 최신정책을읽고자기담당실제원본/화면/계약/제작프롬프트와대조하여수정·병합·검수. 이전전투한정예외대체. 선행완료된정책기반 독립조사/공통수정부터실행,미공급자산·계약전체연결은선행충족후. 공통파일은소유담당1곳,구원본보존/통째덮어쓰기0. 기존QA/인계에파일별근거·충돌·부분완료기록. 가로전투실제품연결·기존데이터재사용/권위/복원/API·메모리검수계속. 외부웹은반응형유지하며게임가로미리보기/연계범위점검. 무관한배포/경제행위권한추가0.


### KYLE-PURE-MARTIAL:DIRECTION:20261026 — 승인 원화 후보 제작
선행정책완료21#kyle-pure-martial-design-approved-20261026 및 상세design-detail-proposal §1~8 / KYLE-PURE-MARTIAL-DESIGN-v1.20261026. 사용자 원화제작 명시지시. 최신 대상제작정책MD·Tools/FrameStudio/PRODUCTION.md와 적용16/17/19부터 실제확인,승인얼굴·자산원본 식별. A맨손호위무인/크롭컷·넓은체격/회백수련상의·먹빛하의띠·황토손목천·낮은장화,봉/방패/기계장비제외. 전신·초상·손가드/접합·2번/반대정지 후보 제작/동일인·실크기검수 후사용자제시. 다른요원원화/무기무단변경0.
기존편집중원본은안전경계에서보존, 새버전/역할로출력. 인물실alpha·프레임/배경/문구독립, 합성참고를독립원화로표현0. 최종외형승인전16모션자리전량제작0,구원본삭제0. 원화실파일/버전·소켓/발/풍향·미검수/승인상태는기존Direction QA/인계에 기록. 컷신/음향에 공급가능한 실제참조와 최신manifest원본을명시. WAIT#kyle-pure-martial-production-followups-20261026 후속조건 적용. 제작/후보제시와사용자최종원화승인 구분.

### COMMON-STYLE-V10:DIRECTION:20261026 — 사용자 전 요원 화풍 확정·재제작
선행 화풍 확정:17#eve-v10-common-style-approved-20261026 / EVE-V10-COMMON-STYLE.20261026. 기준 eve-landscape-cutin-v01/eve-ui-style-v10.png; 외형 전파 아닌 선/채색/명암 기준. 현재 컷신 담당은 레이나 최종3번/V21을 새 화풍으로 보정(선택A 손/권총과3초 동작 보존), 이브v10채택. Direction은 전 registry의 기존 원화/초상/전투 파생 공급목록을 대조하여 새 화풍 재제작, 기존 개별 정체성/장비/카메라/독립 구조/승인본 보존. 카일 최신 외형 승인 대기 유지; 원본 미확정 부분은 생성으로 추정하지 않는다. 대상별 기존 QA에 원본/수정본/검수/실소비 기록; 문서 등록을 전체 제작/제품 연결 완료로 세지 않는다. 컷신 담당 자산 동시편집 금지.

## CS-REINA-SYNC-20261102 — Cutscene_2 소유 / 상세명세·프리뷰 공급 / 음영상 검수 대기
사용자 제작요청2026-11-02. WAIT REINA-AUDIO-FX-HANDOFF-20261102의 원본·해시·제약부터 확인. Cutscene 전용 큐가 없어 기존 Direction 큐 내 담당 명시; Direction이 이 블록 실행/20 동시수정 금지.20 최신신호탄정책과 제작정책부터 읽고, v11 고정음원 + v12 발사→첫착탄1.4초에 맞춰 총구/신호상승/대기/낙하/큰착탄/보조착탄 시간·위치·크기·앵커·수명 상세명세와 검토가능한 동영상/프리뷰 제작. 큰 타격 사이 작은 포탄/보조 폭발 추가로 밀도보강, 모든 잔향을새폭발로세지않음. 기존5초콘티와6.45초폭격음은 컷씬/전투후속 구간 구분하여 해결; 음원변형/유료생성0. 도식은참고, 실제피해횟수정책아님. 완료조건: 타임라인/독립FX명세/음영상동기 검수근거, 미검수명시.20/컷씬 QA는본담당,44는Plan. WAIT 후속조건으로 D 제작등록.

2026-11-02 공급: QA/CutsceneProduction-20261020/reina/audio-sync-v01/SPEC.md·timeline.json·preview.html·QA.json. 원본해시/6.45·10초/1.4초/수명/JS경계검사PASS. 청취·브라우저연속재생검수대기. D준비완료미확인으로BUILD미등록,이CS블록유지.

## D-REINA-FX-BUILD-20261102 — Direction / D2_1 소유 / 독립 FX 제작
등록키 D-REINA-FX-BUILD-20261102. 선행 AND: CS-REINA-SYNC.v3.20261102 상세명세·사용자 시각 승인 + D-PREP 완료. 준비 근거 Tools/FrameStudio/data/reina-direction2-underlap-v14/qa.json#reina_fx_prep_20261102 및 reina-flare-fx-prep-20261102/asset-checks.json. 승인 원본 QA/CutsceneProduction-20261020/reina/audio-sync-v01/SPEC.md, timeline.json, QA.json user_approval. 기존 비트맵 그대로 최종 채택0, 독립 alpha 구조·타임라인 재사용. 총구점화/신호탄 코어·꼬리/상단신호/낙하/큰·작은 폭발코어/충격파/먼지 9종을 독립 숨김·교체·수명 제어 가능하게 제작하고 실제 크기·조합 검수. 제작 전 FrameStudio 작업 등록. 기존 기본/액티브 모듈 중복 제작0, 원화·음원 보존·유료 생성0. 31 시각 이벤트를 피해4회로 연결하지 않음. 런타임 5/10초 대 8.95초 경계는 Plan 조정 대기; 독립 제작 가능, Unity 연결0. 최종 미술은 사용자 검수. D2는 개인 자산/QA 소유, 공유 큐·WAIT는 상위만 반영.

### CS-REINA-AUDIO-LAYOUT:20261102 — Cutscene_2 소유 / 승인시계 조정
44#reina-cinematic-battle-audio-layout-approved-20261102 적용.20/SPEC 타임라인의5초복귀후보를8.95전체화면으로조정,31개승인집중배치보존/실피해4회분리. Audio와10초믹스꼬리/원stem중복없이결속,새원음절단금지. 기존CS/자산선행근거갱신 후 U-BIND AND 확인,미충족선등록0.

### KYLE-UNIFORM-FRAMES-PORTRAIT:DIRECTION:20261103 — 카일 먹색 도복 양방향 프레임·로비/출발 초상 제작
사용자 제작 요청. [승인 기준](../superpowers/plans_categories/21_KYLE_VISUAL_BIBLE.md#kyle-uniform-animation-review-approved-20261103)과 [Plan 실참조 조사](PLAN-D-MIGRATION.md#카일-양방향-외형unity-참조-조사--plan_8--2026-11-03)를 적용한다. 기존 카일 제작자가 보유한 프레임과 원화를 먼저 대조하고 같은 작업은 한 번만 제작한다.
- 실사용·제작 중인 기존 동작의 2번 방향과 반대 방향을 먹색 도복·검은 깃과 여밈·작은 흰 산 심볼·등 흰 巖으로 제작한다. 실제 의상 위치·가림과 기존 동작의 발·접촉점·공격사건 시점을 보존한다.
- 로비·출발 공용 카일 초상을 교체 대상에 포함한다. 소비자 ID는 op_kyle, 현재 경로는 unity-project/Assets/_LASTRAIL/Resources/OperatorIdentity/op_kyle.png. 일반 전투 카드의 대기 프레임 크롭과 구분해 초상 원화·실제 화면 크롭을 검수한다.
- 기존 제작 manifest에 동작/방향 또는 초상 용도별 원본·납품 경로, 버전/해시, 공급·승인 상태, 실크기·연속재생 근거를 기록한다. 미공급·해당 없음은 근거를 구분한다. Direction은 자산과 파일 목록을 공급하고 Unity 연결은 기존 WAIT KYLE-BLACK-UNIFORM-20261102 조건에 따른다.


### GUNNER-FIRING-POSE:DIRECTION:20261103 — 첫 전투 사수·무기 좌우 발사 자세 시안 제작 승인
[기존 원본 조사](../../Tools/FrameStudio/PRODUCTION.md#gunner-source-audit-20261103) 후 사용자 승인. 차체에서 사수 인물과 무기를 분리하고 양쪽 방향의 발사 자세를 제작한다. 각 방향에서 인물·무기를 개별 표시할 수 있어야 한다. 발 접지점, 손과 무기가 맞물리는 지점, 총구 끝이 식별되는 기준점 표시본과 깨끗한 시안을 함께 제공한다.
기존 Direction QA에 원본·검수용 조합·방향별 파일 목록과 버전을 기록하고 Plan_8에 전달한다. 검수는 두 방향에서 실제 크기의 발 접지·손/무기 정렬·총구 가시성 및 차체 가림을 대조한다. 시안 채택 후 Unity가 실제 장면에서 축척·깊이·자세 시점을 실측한다.
GUNNER-NEW-ART-20261104 / 기존 GUNNER-FIRING-POSE 제작 방식 확정. 이용 범위를 확인할 수 있는 미술 담당자가 사수 독립 무기 신규 원화를 제작한다. 담당·게임 내 사용·좌우 편집 및 Unity 수록 가능 범위의 확인 근거를 기존 납품 목록에 연결한다. contact-review-v02의 방향별 인물 배치와 양발 접지선 y=583을 유지하고 독립 무기 및 좌우 결합본을 공급한다. Plan은 양손 연결점과 실제 그려진 총구 끝부터 확인한 후 발 접지를 대조한다.

### BATTLE-D-FIELD:DIRECTION:20261104 — 승인 D 정유시설·고가교 배경 납품
Direction_12 제작 담당. 승인 D와 후속 구도에서 차체·인물·HUD와 분리된 전장 레이어를 제작하고 가림 순서·크롭·출처·이용 범위·납품 경로를 기존 QA에 기록한다. 납품 후 Unity_17에 경로를 전달해 BattleDPlayer848/comparison-v3와 같은 닉스 포함 편성·상태에서 16:9·21:9·4:3을 재촬영한다. Plan은 승인 D와 비교한다.
BATTLE-D-FIELD 담당 갱신. Direction_11이 직접 제작을 수락하고 승인 D·Player 848 기준 확인에 착수했다. 위 Direction_12 담당 표기를 대체한다. 기존 독립 작업본 발견 시 중복 편집 없이 인수하며 실제 배경 납품 후 Unity_17 연결·재촬영 조건은 동일하다.

### D-KYLE-CAMERA-R05-20261108 — 카일 전투 시점 교정 후보 / 사용자 직접 등록 승인
- 요청: 2026-11-08 Plan 현재 사용자 승인. 리소스 제작 담당이 카일만 승인 R-05 지붕 부감에 맞춰 교정 후보 제작. 다른 4인은 Plan에서 배율/발 위치 검토, 임의 재작화 금지.
- 제작 정책부터 확인: Tools/FrameStudio/PRODUCTION.md#external-authoring-default-20261106 및 #production-continuity-gate-20261106, 실제 도구 프로젝트/선택 버전/수정권한 재확인. 알려진 project 5b1c2c8eace24a6fb611af7167b926e3 rev539/full-review-v24는 과거 확인값으로 현재 상태 재확인 필요.
- 현재 보호 원화: QA/SideViewReuse-20261023/reina-battle-hud-v01/kyle-guard/studio-restart-20261106/tone-v23-frame-01.png. 먹색 도복·젊은 외형·체형·장비·기술 정체성 유지. 구 군모 초상 금지. 머리만 숙이거나 전체를 세로압축/회전해 시점 교정으로 처리하지 않음.
- 구도 기준: QA/DirectionImpact-20260921/combat-carriage-durability-v01/approved-assets-v11.png 및 train-r05-trim-v07.png. 지붕과 정수리/어깨/가슴/골반/발의 투영을 함께 대조. 레이나는 상대 비교 자료이며 자세 차이를 카메라각으로 오인하지 않음.
- 수용 조건: 원본/후보 전후 비교+동일 열차 배치+양발 접점 근거, 실제 표시크기 검수 후 사용자 후보 승인. 이후 프레임 확장은 승인·기존 제작 계약에 따라 진행. 기존 원본/도구 버전 보존.
- 소비/통합: Plan 시안, 후속 Unity는 승인·출력 완료 후. 카일 교정 완료 전 현재 원화를 그대로 사용한다는 사용자 명시. 이번 등록은 착수/완료 보고가 아니며 동일 파일 동시 수정 금지.
### D-RIGHT-POSE-INTAKE-20261108 — 레이나·세라·닉스 우측 원화 공급 확인/부재 제작
- 2026-11-08 사용자 명시: 승인 우측 원화부터 Plan 적용, 없는 요원은 리소스 제작 큐 등록. 현재 확인된 적용가능: Kyle right-idle-first-v05(시점교정 전 승인),Eve equipment-v02/Studio asset c14e156e199b41edab6108bd1aca8b3f. 이 둘 재제작 제외.
- 레이나: 좌우승인 이력은 있으나 approved-combat-library-v01/reina-idle-projects.json 4개 project 첫원화 실물은 좌/좌사선/정면/후면으로 이번 소비에 맞는 우측조준 원본을 확정못함. 승인 원본/버전/경로를 먼저 찾아 공급; 존재하면 새로 만들지 않는다. 미확인은 없음으로 단정하지 않음.
- 세라/닉스: 현재 공급 원화는 좌향, 해당 우측 승인 근거/파일 미확인. 제작정책·실제Studio원본 확인 후 기존승인 원본있으면공급, 부재확정 시 우측 첫자세 후보부터 제작/검토. 원화 좌우반전으로 완료처리 금지, 얼굴/장비의 해부학적 위치/부감 보호.
- 필독 Tools/FrameStudio/PRODUCTION.md#external-authoring-default-20261106 및 #production-continuity-gate-20261106. 도구 프로젝트/선택버전/원본/승인근거 먼저 확인·기록, 기준모호한 상태의 임의 새작화 금지.
- 구도 기준 QA/DirectionImpact-20260921/combat-carriage-durability-v01/approved-assets-v11.png의 열차 및 현재 승인 원화, 원본은 기존verification.json. 시점/발 기준/실제 표시크기와 좌우 비대칭 검수 후 공급. 12프레임·Unity연결은 첫자세승인과별도.
- 기존 같은범위 작업이 있으면 본등록키를 연결해 중복제작하지 않음. 완료는 기존Direction인계/QA에 원본·승인·공급 경로 기록. Plan은 없는 슬롯을 미공급으로 표시, 좌향원화를 우향처럼 표시하지 않음.
## DEFAULT-TRAIN-V04:DIRECTION:20261112 — 전투 최종시안 제작 시 반영 / 사용자 지정 후속
- 선행 완료: train-landscape-v04/02-train.png 사용자 승인, 기본 열차 외형 기준으로 지정. 원본 QA/ResourceIntake-20261110/connection/train-landscape-v04/manifest.json (SHA256 6DFCC81F2D9C540D0EF2BC27C6D22E7BE13C0846F08A7B06C6E433D81CBF5E02).
- 다음 전투 화면 최종시안 제작 시 이 기관차·객차 디자인과 양끝 평평한 전투 지붕을 반영. 기관차1+객차5, 후미/기관차 뒤 한 객차에5인 집결 규칙 유지. 옛 v03 각진기관차 재작화 후보로 대체하지 않음.
- 완료조건: 전투 최종시안과 열차 v04 외형 대조, 5인 공간/통로/가림 확인 및 사용자 검토. 지금 전투시안 자동 제작하지 않음. Unity/독립자산/스킨 연결은 후속 구현으로 남김.

