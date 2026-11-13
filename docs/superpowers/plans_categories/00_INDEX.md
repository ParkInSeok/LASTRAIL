<!-- CURRENT-REFERENCE-20261029 -->
> **2026-10-29 현행 참조 우선순위.** 아래 누적 기록은 작성 당시 이력이며, 옛 시안의 존재·파일명·도구 승인 필드만으로 현행 승인본을 판단하지 않는다. 최신 승인 정책과 실제 제작 REVIEW/QA를 함께 확인한다. 가로 화면, 애니메이션 2번/반대 방향, 카일 순수 맨손 무공 및 노드 시작 재입장이 과거 상충 지침보다 우선한다. 최신 제안은 사용자 승인 전까지 적용하지 않는다. 삭제 목록은 `D:/Project/LASTRAIL/QA/ResourceCleanup-20261028/deleted.jsonl`; 삭제된 원화를 임의 복원하거나 다른 버전으로 조용히 대체하지 않는다. 이번 정리는 링크·정책 진입점 대조이며 자산/런타임 전체 검수 완료가 아니다.
현행 원본: [전체 화면 가로 전환](33_CURRENT_WORK_STATUS.md#all-screens-landscape-approved-20261023) · [2방향 애니메이션](17_OPERATOR_COMMON_STYLE.md#landscape-d2-animation-approved-20261023) · [카일 맨손 디자인](17_OPERATOR_COMMON_STYLE.md#kyle-pure-martial-design-approved-route-20261026) · [공통 화풍](17_OPERATOR_COMMON_STYLE.md#eve-v10-common-style-approved-20261026) · [노드 시작 재입장](44_COMBAT_DETAIL_CONTRACTS.md#battle-node-restart-user-correction-20261028).

> **2026-10-02 현행 전투 변경:** A안 신규 적용 폐기, B안 채택. [44 현행 전환 결정](44_COMBAT_DETAIL_CONTRACTS.md#combat-b-mainline-approved-20261002)이 과거 A 유지/B 임시 및 객차 장착 포탑 전제보다 우선한다. 특정 노드 내부전은 별도 유지. 미승인 세부·기존 snapshot·역사자료는 구분하며 일괄 삭제/수치 치환하지 않는다.
# LAST RAIL | 현행 정책·카테고리 문서 안내

현재 편집본: **00~53번 MD 54개 + 작성용 JSON 4개**. 아래 과거 정리 횟수·개수는 해당 단계의 기록이다.
> **로컬 문서 정리 진입점 — 2026-09-21:** 전체 문서 위치는 [상세 원문 등록부](39_SOURCE_REGISTER.md), 전수 파일/동일 사본은 [MD 등록부](../../../QA/DocumentIntegration-20260921/md-cleanup/assets/archive.html#doc-76a704e29c7ce935f063), 추가 전달6개 반영 상태는 [추가 자료 체크 목록](../../../QA/DocumentIntegration-20260921/md-cleanup/assets/archive.html#doc-71a6b3d577d494ee1689)에서 관리한다. 아래 과거 감사/구현 보고는 그 시점의 증거이며 이번 작업의 새 검증으로 읽지 않는다. 이 폴더가 현행 기획 편집 위치이며 새 최종본 사본 폴더는 만들지 않는다.

## 번호별 보완 문서와 상세 계약

- [전체 전투 시나리오와 작전 시작~종료 흐름](04_COMBAT_RUN.md#whole-operation-scenario-flow-20261106): 일반전·엘리트·스토리/월드보스 사건별 대응, 12단계 지도·노드 선택·보상/성장·복구/종결 연결. 2026-11-06 기존 승인 통합 및 신규 연출 제안·미결 구분; 구현 완료 문서 아님.

- [누적 충돌 대조·정리 결과](38_DOCUMENT_RECONCILIATION.md): 검토 범위, 대체 관계, 남은 결정과 원문 보존 기준.
- [전투·증강·경로 보완](34_COMBAT_POLICY_COMPANION.md), [반복 경제·영구 성장](35_CURRENT_ECONOMY_ANNEX.md), [운영 상세 계약](36_OPERATIONS_COMPATIBILITY.md).

- [화면 승인·구형 계약 적용 경계](37_UI_APPROVAL_AND_LEGACY_BOUNDARIES.md): 현재3차 전투/비전투 승인, 최신 캐릭터와 과거 구현의 구분, 출격·서사 문구의 대체 관계.

- 프로젝트 공통 [객체지향·MVC·콜백/이벤트·유지보수](30_PROJECT_RULES.md#source-ebd69050958b1d99), [독립 리소스 제작·검수](30_PROJECT_RULES.md#source-c42b52f9d3a9059a).
- [전투 핵심 철학](30_PROJECT_RULES.md#source-9b8fb18db0c66c5c): 플레이 방식이 바뀌는 증강. 고정5회 등 후속 정정된 횟수는05가 우선.
- [랭킹 보상 정책](47_ECONOMY_DETAIL_CONTRACTS.md#source-e146be10f59741ea): 동일 자격의 재화/성장 보상 통일, 순위 차등은 성능과 무관한 외형/명예.
- [화면별 승인 결정](51_UI_ART_DETAIL_CONTRACTS.md#source-f05ef25c6c893f7d), [비전투 화면 우선 마무리](44_COMBAT_DETAIL_CONTRACTS.md#source-1dfaba3a727da855), [전투3차 기준](51_UI_ART_DETAIL_CONTRACTS.md#source-61f16f3947c254bb). 이후 승인된 화면별 수정은 해당 원문/인계를 함께 대조한다. 번호 아트 문서가 기존 UI 승인 화면을 자동 폐기하지 않는다.
- 07과 함께 기존 [영구권 상세](48_AD_REWARD_DETAIL_CONTRACTS.md#source-d06447f1a5c3fef4), [서버 계약](48_AD_REWARD_DETAIL_CONTRACTS.md#source-5cbc96b63e17ce2a), [Unity 계약](48_AD_REWARD_DETAIL_CONTRACTS.md#source-2b83cf73b602eca9)을 보존한다. 광고 OFF는10, 카드별3+1은05 최신 정정 우선.




**전달 당시 기록 — 현재 로컬 검사와 구분:** 최근 문서 정리 및 전체 재전달: **2026-09-21**. 사용자 요청 “최종 0번부터 다시 전부다 줘봐 다시 다운받게”에 따라 **00~29의 번호 MD30개, data JSON4개, 안내·지시·이번 문서 검사4개, 과거 검사 증거3개를 개별 파일로 제공**한다. 이번에는 변경분만이 아니라 전체 현행 문서의 재다운로드용 구성이다. 합본·ZIP이나 새 게임 정책을 만들지 않았다.

내용의 기준은 직전 최종 전달본의17개 번호 문서와 이번에 라이브러리의 특정 버전에서 확보한 변경 없는13개 번호 문서의 전체 추출 텍스트다. data JSON4종도 전체 행을 확보해 내용 변경 없이 포함했다. 원본 raw 바이트를 얻지 못한 자료는 추출 텍스트 기준이며 원본 바이트 동일성을 보증하지 않는다. 이전 통합 감사의 원문32개는 §7의 출처로 보존하고, 실제 저장소·자산·API를 다시 전부 검증했다고 주장하지 않는다. 파일명 날짜·재업로드 시각보다 승인 순서를 우선한다.

## 1. 가장 먼저 적용할 우선순위

**현재 사용자의 명시적 변경 → 그 변경 뒤 승인된 제안 → 충돌하지 않는 기존 계약 → 미승인 제안** 순서다. 오래된 확정이 이후 명시 정정되면 최신 정정을 사용한다. assistant가 '반영했다'고 썼다는 사실만으로 모든 제안 수치를 승인 처리하지 않는다.

정책 확정·작성 수치·코드 구현·실제 연결·실기기/운영 검증은 별도 상태다. 과거 로그의 통과·미연결은 해당 시점과 범위로만 읽는다. 이전 통합 감사는 문서와 작성용JSON을 갱신한 이력이다. 이번 아트 전달 작업은 MD만 정리했으며 JSON·게임/서버/Admin/홈페이지 소스·DB·배포를 변경하지 않았다.

이전 추출본/v2/v3는 **현행 아님**으로 표시한다. 원본32개는 수정하지 않고 증거로만 보존하며 그 안의 폐기된 정책을 되살리지 않는다. 과거 라이브러리 설계 원문이 이 실행환경에 없는 경우 출처참조로만 유지하고 이번에 다시 확인했다고 하지 않는다.

## 2. 담당 문서 — 한 규칙의 전문은 한 곳에

**캐릭터 전체 명단 진입점:** [17 전체 명단 네 축 연결표](17_OPERATOR_COMMON_STYLE.md#character-registry). 기존15개 ID와 별도 후보2명을 구분하고 서사·역할/실제 스킬·디자인·실제 ID를 함께 추적한다. 16→17 공통 기준은 명단 전체에 적용하며 개인20~29의10인 제작 범위를 전체 로스터로 오인하지 않는다. 변경 영향과 실제 소비 파일은 [33](33_CURRENT_WORK_STATUS.md#first-four-mapping)에 연결한다.

| 문서 | 담당 범위 |
|---|---|
| [01 계정](01_ACCOUNT_ENTRY.md) | 인증/복구·프로필·Main 진입 |
| [02 콘텐츠](02_CONTENT_UPDATE.md) | 승인·다운로드·캐시·필수업데이트 |
| [03 문의/진단](03_SUPPORT_DIAGNOSTICS.md) | 서버접수·Admin처리·본인답변·receipt |
| [04 전투/작전](04_COMBAT_RUN.md) | 지원3개·주행/포격·작전저널/수명/비용·연출설정과정지/복귀 |
| [05 증강](05_AUGMENTS.md) | 랜덤발급·3후보·리롤·완전정지·카탈로그연결 |
| [06 보상](06_REWARDS_ROULETTE.md) | 기본수령·SSV·원자정산·공용룰렛 |
| [07 광고스킵](07_AD_SKIP.md) | 구매권·회수·사용승인·복원 |
| [08 데이터/배포](08_BALANCE_RELEASE.md) | DEV/PROD·호환기준점·불변묶음·승격/롤백 |
| [09 지휘관](09_COMMANDER_PROGRESSION.md) | 선택형내실·보관함특성·규칙형상위6종 |
| [10 운영/공지](10_LIVEOPS_NOTICES.md) | 기존Admin일원화·광고ON/OFF·정상OFF화면·공지 |
| [11 홈페이지](11_WEBSITE_COMMUNITY.md) | 공식위키·확률공시·커뮤니티·공개데이터 |
| [12 점검](12_SERVICE_MAINTENANCE.md) | 앱/웹점검화면·Admin제어·진행작전보호 |
| [13 장비/인챈트](13_EQUIPMENT_ENCHANTMENTS.md) | 6성·합성·단일세트·고유16종·인챈트16종·미리보기 |
| [14 맵/드랍](14_MAP_EVENTS_DROPS.md) | 합류분기·사건풀·노드보상·드랍시험표 |
| [15 조사로그/보존](15_LOG_RETENTION.md) | 게임사공개자료·승인된유형별보관·Admin조사·보류/파기 |
| [16 원화/아트](16_ART_STYLE_GUIDE.md) | 두 번째 릴스 품질 목표·세계관·아트 상위 방향 |
| [17 오퍼레이터 공통](17_OPERATOR_COMMON_STYLE.md) | 10인 역할별 시각 차별화·재질/얼굴·개별 바이블 목차 |
| [18 배경/컷신](18_BACKGROUND_CUTSCENE_STYLE.md) | 전투/CG/웹 배경·공간/조명·스킬컷신3모드·표준전체화면/요약연출·납품 |
| [19 SD 변환](19_SD_COMBAT_MODEL_TRANSLATION.md) | 원화 정체성 압축·레이어/앵커·모션·표시/검수 |
| [20 Reina 바이블](20_REINA_VISUAL_BIBLE.md) | 보존 외형·개인 상세안·팔레트·권총/마도·SD·전체화면/요약 장면 |
| [21 Kyle 바이블](21_KYLE_VISUAL_BIBLE.md) | 전열 외형·무기/방호 후보·SD·궁극기 장면·기존 역할 대조 |
| [22 Eve 바이블](22_EVE_VISUAL_BIBLE.md) | 단발/고글·정비복·물리 도구·장치·Eve/Cipher 구분 |
| [23 Sera 바이블](23_SERA_VISUAL_BIBLE.md) | 의료복·낮은 매듭·촉매/의료낭·회복 표현 |
| [24 Mira 바이블](24_MIRA_VISUAL_BIBLE.md) | 기관총/급탄부·양손 지지·지속 화력의 인상 |
| [25 Vex 바이블](25_VEX_VISUAL_BIBLE.md) | 정밀 장총·좁은 실루엣·조준축·방진 패널 |
| [26 Rin 바이블](26_RIN_VISUAL_BIBLE.md) | 가칭·사선 복식·사지 보강·단창 우선 비교 |
| [27 Nyx 바이블](27_NYX_VISUAL_BIBLE.md) | 가칭·곡선 코트·촉매·공간 제어와 대상 상태 |
| [28 Cipher 바이블](28_CIPHER_VISUAL_BIBLE.md) | 가칭·세로 센서/단말·정보 관측·짧은 입력 |
| [29 Iris 바이블](29_IRIS_VISUAL_BIBLE.md) | 가칭·보호 프레임·방벽 면·보호 상태 표현 |

작성용 JSON: [지휘관](commander_growth_draft.json), [장비/인챈트](equipment_enchant_draft.json), [맵/드랍](map_drop_draft.json). 셋 모두 **publishable=false**다. 장비JSON은 수치표를 완성했지만 runtime 검증 전이며, 별도 [로그보관 정책JSON](log_retention_proposal.json)은 운영정책 승인 완료·구현/개인정보 범위 검토 전이며 publishable=false다. MD와 JSON은 같은 값을 유지하되, 현재 게임 수치 원본은 검증 후 발행된 서버 BalanceVersion으로만 삼는다.

<a id="u1"></a><a id="u2"></a><a id="u3"></a><a id="u4"></a><a id="u5"></a><a id="u6"></a><a id="u7"></a><a id="u8"></a><a id="u9"></a>
<a id="current-policies"></a>
## 3. 최신 결정과 폐기한 해석

위 U1~U9 앵커는 이전 링크 호환용이다. 상세 연혁을 현행 규칙으로 중복 복사하지 않는다. 아래는 담당 위치를 찾기 위한 요약이며 전문은 해당 문서에 둔다.

| 분야 | 최신 적용 기준 | 현행에서 제외한 규칙/해석 | 전문 |
|---|---|---|---|
| 환경 | DEV 한 곳에서 개발·통합·실기기·내부테스트 전부. 향후 공개 PROD는 분리 | STG/STAGE 환경·승격, 동일 PROD의 테스트모드 필수 구현 | [담당](08_BALANCE_RELEASE.md) |
| 증강 | 큰 전투규칙 변화, 랜덤 노드 획득, 최초3후보·선택 중 완전정지 | 구형 고정웨이브·2후보·시작/경로별5회 보장 | [담당](05_AUGMENTS.md) |
| 지원/포탑 | 오른쪽 지원3개. 포탑 자체HP·전투객실 종속,객실파괴 시 잔여HP와 무관하게 전투불능. 궁극기 없음 | 명령2개 초안·포탑 궁극기/전용게이지 | [담당](04_COMBAT_RUN.md) |
| 장비 | 요원/포탑 각각3슬롯,작전전용,최고6성,동일성급·카테고리 합성 | 최고4/5성 표를 최종표로 사용 | [담당](13_EQUIPMENT_ENCHANTMENTS.md) |
| 세트 | 아이템당 단일태그,각 장착자의 실제3슬롯1/2/3개로 활성 | 복수태그·합성재료 기여도 누적·성급별 가상개수 | [담당](13_EQUIPMENT_ENCHANTMENTS.md#sets) |
| 합성미리보기 | 2개·무료합성,주재료태그/인챈트승계. 확정값 사전표시·새 랜덤효과는 합성후 공개 | 전체 편성의 무조건 이득/세트보존 약속 | [담당](13_EQUIPMENT_ENCHANTMENTS.md#preview) |
| 효과풀 | 장비고유16종,별도 인챈트16종;시술계수는승급으로 불변 | 8/5종만으로최종완료,인챈트를현재성급으로 재계산 | [담당](13_EQUIPMENT_ENCHANTMENTS.md) |
| 지휘관 | 작전보너스 선택특성,보관함도 특성 하나,규칙형 상위6종 | 레벨별 보관함 자동확장·단순가산 상위특성 | [담당](09_COMMANDER_PROGRESSION.md) |
| 지휘스킬원천 | 내실·명시증강만 직접강화 | 장비·세트·인챈트에서 지휘스킬 직접강화 | [담당](09_COMMANDER_PROGRESSION.md) |
| 데이터 | 서버 불변묶음·캐시·작전고정,환경/채널내 호환 하위최대기준점 | 표마다다른버전 혼합,다운로드실패로구형표 우회 | [담당](08_BALANCE_RELEASE.md) |
| 광고 | 첫공개판OFF,비보유자기본보상/무료분·유효구매자광고없는룰렛/추가리롤 유지. 광고스킵신규판매숨김 | 무조건광고·OFF전용게임차단·전원가짜구매권 | [담당](10_LIVEOPS_NOTICES.md) |
| 운영/웹 | 모든운영기존Admin;문의서버저장;위키/확률/커뮤니티/공지 제공 | 별도웹백오피스·위키숫자수동중복원본 | [담당](11_WEBSITE_COMMUNITY.md) |
| 점검/복구 | 앱·웹점검화면,정상작전/거래보존;구형복구불가만별도폐기 | 점검·통신실패를일괄작전삭제로 처리 | [담당](12_SERVICE_MAINTENANCE.md) |
| 작전수명/비용 | 최초서버승인부터72시간·중단보정·복구후최소24시간;정상만료/포기 미환급,서비스복구불가 종결 시 실차감1회 반환 | J11 미확정 취급,클라이언트 우선삭제,보상원장 연쇄삭제 | [담당](04_COMBAT_RUN.md#run-expiry) |
| 조사보관 | 체크포인트180일·핵심행위1년·작전요약/Admin감사3년,유형별분리·사건별hold | 종결7일뒤조사증거삭제,작전만료와로그파기의혼동 | [담당](15_LOG_RETENTION.md) |
| 원화/아트 | 두 번째 릴스 품질·세계관 유지·전투SD. 17~19 공통,20~29 개인 본문·외형 방향/차별화의 중립 전신 기준 채택 | 픽셀 기준 복원,가칭 이름/실제 무기·스킬/비교값 자동확정,자동액티브/패시브까지컷신화 | [상위16](16_ART_STYLE_GUIDE.md)·[개인목록17](17_OPERATOR_COMMON_STYLE.md#bible-template)·[배경18](18_BACKGROUND_CUTSCENE_STYLE.md)·[SD19](19_SD_COMBAT_MODEL_TRANSLATION.md) |
| 스킬 컷신 설정 | 하루 한 번(DAILY)·항상(ALWAYS/ON)·요약연출(OFF). DAILY 반복은 요약, 실제 전체화면에서 전투배속0 | 2단계설정,OFF연출완전삭제,ON반복자동축약,포탑궁극기추가 | [시간/입력04](04_COMBAT_RUN.md#skill-cinematic-policy)·[제작18](18_BACKGROUND_CUTSCENE_STYLE.md#cinematic-scope) |

## 4. 이번에 실제 정리한 사항

**전체 재다운로드 전달:** 직전에는 최초 `00~16번 MD + data/ JSON4개` 대비 `00/04/11/16` 교체와 `17~29` 추가인 변경분을 제공했다. 이번 요청에는 변경 없는 `01~03·05~10·12~15`도 전체 본문으로 포함하여 `00~29`를 모두 제공하고, JSON4종도 다시 포함했다. 번호 문서 중 이번 재전달에서 수정한 것은00의 전달 범위·출처·사용 안내뿐이며,01~29의 정책·아트 본문과 데이터 값은 바꾸지 않았다. [변경 안내](40_ART_DELIVERY_CHANGES.md)·[다음 채팅 인계](43_NEXT_CHAT_HANDOFF.md)·[Codex 통합 지시](41_INTEGRATED_HANDOFF.md)는 안내이지 별도 정책 원본이 아니다.

**개인 바이블:**20~29의10인 상세 본문·교차 검토·개인 보완을 통합했다. 외형 기준안의 승인 요청 뒤 사용자 “ㄱㄱ”에 따라 얼굴·헤어·체형 인상·복식·색면·역할 장비 방향을 중립 전신 설계 기준으로 채택했다. Kyle은 성인 남성·크롭컷 우선·넓은 상체·중형 방호 방향이 채택되었지만 단축형 라이플/근접 대응·방패·주 손은 미확정이다. Rin의 단창도 우선 비교 후보이며 최종 무기가 아니다. 실제 기본 공격·스킬·수치·ID·원화/SD/컷신 검수는 별도다.

**이전 스킬 컷신 정정 — 현재 효력 유지:** 사용자 “Off는 이전 제안처럼 요약연출로”를 적용한다. 3모드는 유지하며 ON과 DAILY 첫 재생은 표준 전체화면, OFF와 DAILY 반복은 요약 연출이다. 요원은 짧은 부분 컷인, 일반 지휘관은 짧은 명령/전장 반응, 별도 허용된 초토화급은 짧은 전용 요약 컷신을 사용한다. 실제 전체화면일 때만 전투배속0을 적용한다. 직전 OFF 완전 삭제 해석은 폐기한다. 04는 선택/시간/입력/원사건/복귀를, 18은 제작을 소유한다. 스킬별 하루 첫1회·서버 게임일자·기본값DAILY·시청기록 방식은 별도 권장안이며 이번 정정으로 추가 승인된 것은 아니다. 16·17·19의 관련 문구를 맞췄고 JSON4종·전투 수치·개인 외형은 변경하지 않았다.

**공통 문서 후속:**16은 상위 방향,17은10인 비교와 개인 진입점,18은 장면·개인 구도 참조,19는 변환·검수 기준을 소유한다. 얼굴 비교쌍·헤어/색면/손과 장비·컷신 구도를 보완했다. 상세 전문은 개인 문서에 통합하고 새로운 중복30번 바이블을 만들지 않았다. 원화·SD 숫자 규격과 재생 길이는 비교안이며 게임·서버·경제·JSON을 변경하지 않았다.

### 이전 게임 기획 통합에서 반영한 내용 — 이력 보존

아래는 이전 통합 작업의 기록이며 이번 아트 전달에서 데이터를 다시 수정했다는 뜻이 아니다.

장비 고유효과 16종·인챈트 16종과 맵·드랍을 13·14번 담당 문서와 작성 데이터에 편입했다. 최고 6성·단일 세트를 사용하고, 이전 5성 수치표와 복수 태그안은 현행 계산표에서 제외했다. 08번의 동일 PROD 빌드 복수 채널 전환 제안은 제거하고 환경당 고정 채널로 단순화했다. 환경간 `PromoteRelease`와 DEV 내부 `ActivateRelease`의 역할도 구분했다.

06·07번은 광고 제공 문장에 현재 허용 조건을 명시했다. 첫 광고 OFF 공개판에서 구형 문장만 읽고 광고를 켜지 않게 했다. 지휘관 JSON의 구형 테스트 채널 참조도 삭제했다. J03 후속 결정으로04·11·13번은 확정 고유효과를 미리 표시하고 새 무작위 효과는 합성 후 공개한다. 미리보기 조회는 난수/재료를 소비하지 않는다.

현행 지침과 충돌하지 않는 인증·다운로드·원요청·정산 계약은 유지했다. 상품 권한과 광고 OFF, 공지와 실제 점검, 문의 서버 저장과 기기의 메타데이터 전용 저널, 요청 복구와 전투 전체 복원은 서로 다른 범위다.

<a id="open-decisions"></a>
## 5. J01~J11 후속 결정 현황

2026-09-21 사용자의 항목별 답변과 이후 명시적 승인을 적용했다. **승인한 구조·기존 수치 시험안 채택과 실제 구현/밸런스 검증은 별개**다. J11의 재개 수명·중단 보정·원인별 비용 처리는 앞선 게임 기획의 명시 지시로 확정되었다. J04의 노드·웨이브 숫자는 2026-09-22 D6 첫 일반 솔로 시험 범위에서 승인됐다. 실제 구현·발행과 스토리별 예외는 원문을 따른다.

| ID | 상태 | 현재 처리 | 남은 작업/전문 |
|---|---|---|---|
| <a id="j01"></a>J01 | 생존·스킬별 자원구조 확정 | 포탑 자체HP·객실종속. 현재쿨타임형,미래마나/에너지소모는스킬별정의 | 실제비용/쿨타임·복구/재건·효과적격성연결. [04](04_COMBAT_RUN.md#turret-skill-resources) |
| <a id="j02"></a>J02 | 승인 | 같은 성급·카테고리2개,수수료없음,다른세트허용,주재료단일태그/인챈트승계. 인챈트1슬롯·동일효과최고값1개. 안전 정비 구간에서 조작 | handler/소유/재료·결과 원자저장 구현. [13](13_EQUIPMENT_ENCHANTMENTS.md#merge) |
| <a id="j03"></a>J03 | 승인 | 카테고리·태그·기본/확정고유효과 미리표시. 새 무작위 고유효과는 무작위라고 표시하고 합성 후 공개 | 미리보기 난수소비0·원결과 복구 검증. [13](13_EQUIPMENT_ENCHANTMENTS.md#preview) |
| <a id="j04"></a>J04 | D6 첫 일반 솔로 시험 승인 | 일반2wave/엘리트3wave/보스1wave3phase·시작제외12방문·상대가중치 기준 채택 | 실제 encounter/종료프로필·생성/Seed·실기검증. [14 D6](14_MAP_EVENTS_DROPS.md#operation-scale-trial-v02), 스토리 예외는32 |
| <a id="j05"></a>J05 | 추천 기준 채택 | 지휘관 기존 일반 수치 시험안·상위6종 기믹을 기준으로 연결·강도 검증 | 앞서 미작성된 매개변수/실제스킬ID는 값 생성·검증 필요, null실행불가. [09](09_COMMANDER_PROGRESSION.md) |
| <a id="j06"></a>J06 | 수치표작성완료·실전검증전 | 기본36·고유128·인챈트96·세트18단계완성.6성최대·직접5/6성미드랍유지 | actual handler/호환/드랍·공급량·합성밸런스검증. [13](13_EQUIPMENT_ENCHANTMENTS.md)·[14](14_MAP_EVENTS_DROPS.md) |
| <a id="j07"></a>J07 | 승인 | 카드(슬롯)마다 무료3회·추가1회. 2026-09-21 사용자 최신 정정 반영. 비보유ON은광고검증후추가1/OFF는추가없음. 구매자는상품승인으로같은추가1 | 원slot DTO매핑 확인;광고1+상품1로2회지급금지. [05](05_AUGMENTS.md) |
| <a id="j08"></a>J08 | 승인 | 비보유BASIC_ONLY·유효구매자는OFF에도광고없는룰렛/리롤. 첫데모광고스킵신규판매숨김 | 실제권한/상품·무료분 보존·OFF UI 연결. [10](10_LIVEOPS_NOTICES.md#off-benefits) |
| <a id="j09"></a>J09 | 승인 | 새공지별첫안전로비1회·중요개정만재알림·오탈자재노출없음 | 서버계정읽음·팝업·ACK복구 연결. [10](10_LIVEOPS_NOTICES.md#popup) |
| <a id="j10"></a>J10 | 제시된 추천 범위 채택 | 기존인증·텍스트게시판·Admin운영·DEV/PROD격리. 이미 제시된 범위로 진행 | 공개데모→정식진행유지안은신규추천. 타IAP·보존기간같이 원안에값이 없던사항은 임의확정하지않음. [11](11_WEBSITE_COMMUNITY.md) |
| <a id="j11"></a>J11 | **재개 수명·원인별 비용 처리 승인** | 최초START승인부터72h,게임중단보정+복구후최소24h,서버종결후재개데이터정리. 정상만료/포기는미환급;서비스사유복구불가종결은실차감입장비1회반환. 조사보관은[15](15_LOG_RETENTION.md)로분리 | 원거래/저널/만료·완료경합/점검보정/클라표시 실구현·검증. [04](04_COMBAT_RUN.md#run-expiry)·[12](12_SERVICE_MAINTENANCE.md#recovery) |

### 아직 새로 결정/작성할 것

- **J04 새 추천:** 노드별 횟수·12노드 데모 경로·증강 가중치. 일반2/엘리트3/보스1wave3phase는 새 승인 전 숫자다.
- **J01 잔여:** 스킬별 자원정의는 확정. 현재 쿨타임형을 유지하고 새스킬만 명시한 마나/에너지 비용을 갖는다. 재건/부활은 별도다.
- **J05/J06 작성/검증:** 장비수치표는 앞선 게임 기획 요청에서 작성 완료. 지휘관상위특성 매개변수/실제스킬ID·runtime검증은 별도이며 실제운영값/발행완료로 가장하지 않는다.
- **J10 경계:** 추천받지 않았던 타IAP·법정/개인정보보존값은 포괄 승인으로 새 정책을 만들지 않는다. 공개 데모와 정식의 동일PROD진행 유지안을 별도 추천한다.

<a id="art-open-decisions"></a>
### 아트 제작에서 다음에 고정할 항목

공통17~19와 개인20~29가 작성되었다. 개인 큰 외형 방향·차별화 보완은 중립 전신 설계 기준으로 채택했으나 완성 시안은 미검수다. Reina의 보존 요소는 유지한다. Kyle은 크롭컷 우선안과 성인 남성·방호/색면 방향을 사용하고 뒤로 넘긴 머리는 비교 대안 이력으로 남긴다. 최종 무기·방패·주 손·정확한 비대칭/HEX·궁극기 동작/길이는 확인 대기다. Rin의 단창은 우선 비교 후보만 유지한다.

가칭 Rin/Nyx/Cipher/Iris의 성인 인상·큰 외형 방향은 개인 기준안을 따르지만 정확한 이름·이름 종속 문양·로고·새 ID는 확정하지 않는다. 실제 나이·키·새 서사·로스터 성비 정책을 추가하지 않는다. 모든 요원의 정확한 얼굴 그림·체형 비례·장비 두께/고정부/파지·색 출력·스킬 연결은 실제 자료/시안에서 확인한다. 제작 요청 없이 이미지·코드·배포를 시작하지 않는다.

<a id="art-baseline-approved"></a>
**이번 채택의 효력:** 얼굴·헤어·복식·색면·역할 장비 방향과 개인 차별화 문구를 중립 전신 설계의 기준으로 사용한다. 장비의 외형 채택은 새 전투 기능의 승인이 아니며, Kyle/Rin의 무기 확인과 지원 요원의 기본 공격 연결은 별도다. 상태의 전문은 [17 개인 목록](17_OPERATOR_COMMON_STYLE.md#bible-template)과20~29에 둔다.

| 구분 | 확인할 근거 | 확인 전 처리 |
|---|---|---|
| Kyle/Rin 무기 | 승인 원화·무기/공격·Block/Taunt/Interrupt/Breaker 실제 계약 | 후보를 유지하고 새 이동·넉백·타격을 만들지 않음 |
| 지원 요원 기본 공격 | Eve/Sera/Nyx/Cipher/Iris의 수단·발동 주체 | 역할 도구를 확정 무기나 공격 불가로 변환하지 않음 |
| 사격3인 | 실제 발사·재장전/과열 존재 여부·타깃/부위 | 컷신/프레임으로 추가 발사·피해·대기를 만들지 않음 |
| 장치·회복·제어·정보·보호 | 실제 생성/수명/적격성·공개 정보·대상/영역 | 장식과 실제 적용·실패·종료를 분리 |
| 자산/표시 | 실제 ID·경로·앵커·카메라·revision | 임의 식별자와 검증 완료 기록 생성 금지 |

Rin의 과거 Military/Mercenary 언급은 실제 태그/ID가 이번 자료에 확인되지 않은 참고 이력이다. 외형 승인으로 현행 시너지 데이터에 올리지 않는다. 자료 미확보와 정의 자체의 부재를 구분하고, 기존 자료에서 해결할 사실을 새 사용자 취향 선택으로 바꾸지 않는다.

SD 비례3.25등신·480×480/몸체240px·720root 표시128px, 원화/CG 원본 해상도, 파츠/프레임 제작 방식은 **새 비교 후보**다. 실제5인 객실·무기·UI 배치 검수 뒤 채택한다. 배경의 환경군 분류가 새 지역이나 J04 노드 수를 확정하지 않으며, 스킬 컷신3모드와 실제 전체화면의 전투0·OFF 요약 연출은 확정이다. 스킬별 하루 첫1회·초기값DAILY·서버일자와 시작/스킵 기록은 [04 §14.6 권장안](04_COMBAT_RUN.md#cinematic-daily-policy)이다. 정확한 일일 초기화 시각은 기존 계약을 확인하고 임의로 정하지 않는다. 개인별/스킬별 재생길이·실제자산ID·연출패키지와 구현은 추가 결정/검증하며 04·18을 따른다.

### 원문/구현 확인

40종 증강·실제 지원3개·요원 시너지·handler·인증/Keystore·광고/Store·원거래DTO는 기존 source와 대조한다. 단순 정책 승인으로 구현 완료로 바꾸지 않는다. 협동은 별도 후속이며 슬롯 예산·기한을 솔로에서 임의 파생하지 않는다.

<a id="qa"></a>
## 6. 원문에 보고된 검증 현황

아래는 **첨부 보고서의 결과**이며 이번 편집 작업에서 Unity/서버 테스트를 새로 실행한 수치가 아니다. 서로 다른 시점·중복 suite·부분 실행이므로 합산하지 않는다. skip은 통과가 아니며 별도 실행 증거는 그 범위로만 보존한다.

| 범위 | 원문 보고 결과·근거 | 증거 한계 |
|---|---|---|
| 계정·Main 연결 | Main 전체 **1377 / 1369 pass / 0 fail / 8 skip**, 실제 씬 확인 7/7. [R21](#r21) | HTTP/storage/catalog는 주입 fixture. 콘텐츠/APK를 새로 빌드하지 않았으며 새 Main의 배포 번들 증거가 아님. |
| 이메일 UI | 최종 **1083 / 1081 pass / 0 fail / 2 skip**, 취소 중 안전 저장 경합 수정. [R07](#r07) | UI fake-wire와 실제 이메일/기기 인증은 다름. strict content HTTP 별도 실행과 구분. |
| 필수 업데이트 | **48/48** focused 검증. [R08](#r08) | synthetic opener. 실제 스토어 이동·설치 확인이 아님. |
| 문의·진단 | 실제 Game/Admin Hono·sql.js HTTP, 응답 유실 뒤 원 ticket GET 및 Admin 조회. [R10](#r10) | 프로필 오염/응답 유실은 실제 응답 이후 주입한 시험 조건. |
| receipt 재시작 | 통합 **117/117**, 전체 **1204 / 1202 pass / 0 fail / 2 skip**. [R12](#r12) | 실제 임시 파일+fake crypto+새 인스턴스. Android 프로세스 강제 종료/Keystore 검증 아님. |
| 작전 요청 복구 | **1275 / 1271 pass / 0 fail / 4 skip**, 두 실제 HTTP 빌드 B/C 사례 실행. [R14](#r14) | 파일/인스턴스 재생성 검증이며 전투 전체 기기 복원 증거가 아님. |
| 기준선 빌드 | helper **28/28**, Android LOCAL_TEST full A/B와 A 기반 무변경 update, 원본 56파일 보존. [R16](#r16) | 콘텐츠 변경이나 Android 실행/OTA를 입증하지 않음. |
| 합성 delta | **29/29**, 변경된 Windows 번들 A/B 실제 HTTP 다운로드·cache 재로드. [R17](#r17) | 이 단계 자체는 제품 admission/로비 전환 검증이 아님. |
| 제품 콘텐츠 전환 | strict HTTP **29/29**, 전체 **1305 / 1298 pass / 0 fail / 7 skip**; 새 프로세스 pin 충돌·B 전환·지원 A cache·hash 실패 재시도. [R18](#r18) | 제품 backend/coordinator와 합성 lobby 사용. 서버 승인/auth는 제어 fixture, 정상 프로세스 재시작. |
| 전투 콘솔 | 최종 **132/132**, current-profile Unity HTTP·native UI controls·pause/CAS 복원; 별도 2칸 15wave 승리 기록. [R22](#r22) | 4칸은 합성 view, notch는 기하 검사, Down/HP 경고는 구성한 진단. 실제 터치/기기 검증 아님. |
| 증강 서버/Unity | 서버 **116files/920tests**, Unity 독립 선택/리롤 및 TEST_ONLY 8PNG 기록. [R31](#r31) [R32](#r32) | 메모리 도메인·독립 화면 검증. 실제 map/catalog/effect/DB/승인 연결은 [05](05_AUGMENTS.md)의 후속. |
| 보상·룰렛 | 정산/API/View 범위의 작업 체크와 회귀·캡처 완료 기록. [R23](#r23)–[R27](#r27) | 문서에 없는 통합 총계나 실제 provider/운영 완료를 만들지 않음. |
| 광고 스킵 | Unity 회귀 **104/104**, 후속 상태 29/29·문구 28/28·상점 13/13은 각각 별도 범위. 서버 격리 DB/API/migration 검증 기록. [R29](#r29) [R30](#r30) | 통과 수를 합산하지 않음. 성공 host 캡처와 최종 source가 같은 시점이라는 주장도 하지 않음. |

**공통 한계:** 로컬 HTTP/Hono/sql.js, 합성 fixture, fake crypto, Editor/Windows bundle 실행, Android 대상 빌드는 각각 다른 증거다. 실제 Android Keystore·기기 OAuth/터치/키보드·강제 종료·네트워크 전환, 실광고/Store 결제/환불, 원격 배포/D1/R2/출시 검증으로 확대하지 않는다. 원문별 범위는 위 참조를 따른다.


## 7. 출처 목록 — 원문 32개, 주 분류 중복 없음

`R01–R32`는 **이 압축 정리본의 출처 번호**다. 이전 v3의 번호와 혼용하지 않는다. 파일명의 `(1)`은 정책 버전으로 해석하지 않았다. 원문을 삭제·이동·수정하지 않았다. 아래 분류는 이번 재편집의 조직 방식이지 저장소 공식 분류를 확인한 결과는 아니다.

| 출처 | 원본 파일 | 주 문서 | 다루는 내용 |
|---|---|---|---|
| <a id="r01"></a>R01 | `2026-09-05-network-content-startup(1).md` | [02](02_CONTENT_UPDATE.md) | 네트워크별 다운로드 정책·구현 계획 |
| <a id="r02"></a>R02 | `2026-09-06-addressables-runtime(1).md` | [02](02_CONTENT_UPDATE.md) | Addressables 전환·로컬 예외·번들 구성 |
| <a id="r03"></a>R03 | `2026-09-06-startup-auth-integration.md` | [01](01_ACCOUNT_ENTRY.md) | 인증 소유자·복구 기반·후속 통합 기록 |
| <a id="r04"></a>R04 | `2026-09-06-player-entry-runtime-and-admission(1).md` | [01](01_ACCOUNT_ENTRY.md) | 진입 단계·인증 replay·로비 준비 계약 |
| <a id="r05"></a>R05 | `2026-09-06-login-loading-lobby-verification(1).md` | [01](01_ACCOUNT_ENTRY.md) | 로그인·콘텐츠 승인·로비 연결 검증 |
| <a id="r06"></a>R06 | `2026-09-06-password-email-ui(1).md` | [01](01_ACCOUNT_ENTRY.md) | 가입·연결·비밀번호 초기화 구현 계획 |
| <a id="r07"></a>R07 | `2026-09-06-password-email-ui-verification(1).md` | [01](01_ACCOUNT_ENTRY.md) | 이메일 UI·취소 경합·콘텐츠 HTTP 검증 |
| <a id="r08"></a>R08 | `2026-09-06-required-update-verification(1).md` | [02](02_CONTENT_UPDATE.md) | 필수 업데이트 버튼·목적지 검증 |
| <a id="r09"></a>R09 | `2026-09-06-support-inquiry.md` | [03](03_SUPPORT_DIAGNOSTICS.md) | 문의 모델·패널·초기 구현 범위 |
| <a id="r10"></a>R10 | `2026-09-06-support-diagnostics-http-verification.md` | [03](03_SUPPORT_DIAGNOSTICS.md) | 문의·현재 세션 오류 수집 HTTP 검증 |
| <a id="r11"></a>R11 | `2026-09-06-receipt-restart-recovery(1).md` | [03](03_SUPPORT_DIAGNOSTICS.md) | 재시작 영수증 메타데이터 복구 계획 |
| <a id="r12"></a>R12 | `2026-09-06-receipt-recovery-verification(1).md` | [03](03_SUPPORT_DIAGNOSTICS.md) | 복구 UI·용량·계정 격리 검증 |
| <a id="r13"></a>R13 | `2026-09-06-durable-run-operations(1).md` | [04](04_COMBAT_RUN.md) | 작전 시작·종료 요청 영속화 계약 |
| <a id="r14"></a>R14 | `2026-09-06-durable-run-verification(1).md` | [04](04_COMBAT_RUN.md) | 원요청 복구·빌드 스냅샷·직렬화 검증 |
| <a id="r15"></a>R15 | `2026-09-06-content-state-baselines(1).md` | [02](02_CONTENT_UPDATE.md) | 전체 빌드 기준선 보존 설계 |
| <a id="r16"></a>R16 | `2026-09-07-content-state-verification.md` | [02](02_CONTENT_UPDATE.md) | 기준선·manifest·무변경 업데이트 검증 |
| <a id="r17"></a>R17 | `2026-09-07-synthetic-content-delta-verification.md` | [02](02_CONTENT_UPDATE.md) | 합성 자산 변경·실제 번들 캐시 검증 |
| <a id="r18"></a>R18 | `2026-09-07-backend-content-release-transition-verification.md` | [02](02_CONTENT_UPDATE.md) | 제품 콘텐츠 경로·릴리스 전환 검증 |
| <a id="r19"></a>R19 | `2026-09-07-pre-lobby-process-audit.md` | [01](01_ACCOUNT_ENTRY.md) | 통합 전 결함 조사·미연결 항목 |
| <a id="r20"></a>R20 | `2026-09-07-main-integration-implementation.md` | [01](01_ACCOUNT_ENTRY.md) | 인증·네트워크 수정·Main 연결 계획 |
| <a id="r21"></a>R21 | `2026-09-07-main-integration-verification.md` | [01](01_ACCOUNT_ENTRY.md) | 통합 후 결과·씬 소유권·잔여 조건 |
| <a id="r22"></a>R22 | `2026-09-13-combat-second-final.md` | [04](04_COMBAT_RUN.md) | 전투 콘솔·주행·포격·표현 프로필 |
| <a id="r23"></a>R23 | `2026-09-16-combat-reward-settlement.md` | [06](06_REWARDS_ROULETTE.md) | 클리어 보상 원자 정산·룰렛 계산 |
| <a id="r24"></a>R24 | `2026-09-16-combat-ad-api.md` | [06](06_REWARDS_ROULETTE.md) | 광고 create/read/spin·SSV·정산 |
| <a id="r25"></a>R25 | `2026-09-17-unity-combat-roulette(1).md` | [06](06_REWARDS_ROULETTE.md) | 승리 화면·광고 룰렛·요청 복구 |
| <a id="r26"></a>R26 | `2026-09-17-shared-roulette-popup(1).md` | [06](06_REWARDS_ROULETTE.md) | 보급·전투 공용 결과 화면 |
| <a id="r27"></a>R27 | `2026-09-17-roulette-approved-polish(1).md` | [06](06_REWARDS_ROULETTE.md) | 승인 화면 보완·보급 확률 상세 |
| <a id="r28"></a>R28 | `2026-09-17-ad-skip-local-integration(1).md` | [07](07_AD_SKIP.md) | 상품 공통 계획·담당 경계·인수 조건 |
| <a id="r29"></a>R29 | `2026-09-17-ad-skip-server(1).md` | [07](07_AD_SKIP.md) | 구매·복원·회수·상품 사용승인 |
| <a id="r30"></a>R30 | `2026-09-17-ad-skip-unity-phase1(1).md` | [07](07_AD_SKIP.md) | 권한 조회·호스트·승인 복구·상점 |
| <a id="r31"></a>R31 | `2026-09-17-augment-reroll-server-phase2(1).md` | [05](05_AUGMENTS.md) | 솔로 증강 도메인·카탈로그 연결 경계 |
| <a id="r32"></a>R32 | `2026-09-17-ad-skip-unity-phase2(1).md` | [05](05_AUGMENTS.md) | 증강 선택 UI·리롤 독립 단위 |



## 8. 문서 유지·편집 검증

새 사용자 지시는 담당 문서 본문에 반영하고 index의 판단 상태를 갱신한다. 같은 규칙의 날짜별 복사본이나 새 통합 버전을 계속 늘리지 않는다. 과거 원문과 QA는 증거로 보존하고, 진행된 구현을 취소한 것처럼 재작성하지 않는다. 이번 실제 파일·링크·보존 검사와 미확인 범위는 [문서 검사](42_DOCUMENT_VALIDATION.md)에 기록한다. 이는 저장소·게임 검증이 아니다.

이전 통합 감사 기록 `validation/session_policy_audit_20260921.json`(과거 검사 참조; 로컬 확보 상태는 점검 보고서)은 해당 시점의 링크/앵커·JSON·원본 보존 검사다. 당시 J결정 반영은 항목별 반영 검사 `validation/j_decisions_followup_20260921.json`(과거 검사 참조; 로컬 확보 상태는 점검 보고서)에서 별도로 확인한다. 두 기록 모두 **문서검사**다. 실제 게임·서버 API·DB 경합·배포·실기기·법률·밸런스 검증을 뜻하지 않는다. `validation`의 이전 기록은 당시 문서 해시를 보존한 이력이며, 이번 수정 파일에 대한 최신 PASS로 사용하지 않는다.

## 9. 승인 이력과 현재 효력

| 순서 | 반영 내용 | 현재 상태 |
|---|---|---|
| J 항목별 결정 | 포탑 HP·객실 종속, 합성·인챈트·미리보기, 리롤·광고 상품·공지·홈페이지 범위 | 담당 문서의 최신 규칙 적용. J04의 제안 수치는 별도 |
| 6성 수치·스킬 자원 | 기본효과36칸·고유효과128칸·인챈트96칸·세트18단계 작성, 포탑의 스킬별 자원 정의 | 수치 작성 완료. 실제 효과 처리기·밸런스·기기 검증과 구분 |
| 조사 보관 | 체크포인트180일·핵심행위1년·요약/Admin감사3년, 아카이브·사건별hold | 승인. 과거7일 정리안으로 되돌리지 않음 |
| 저장 작전 | 72시간 재개, 서버 확인 중단 보정, 복구 후 최소24시간, 원인별 입장비 처리 | J11 승인. [04](04_COMBAT_RUN.md#run-expiry)가 단일 원문 |
| 원화 품질 | 업로드 영상 확인 후 두 번째 릴스 캐릭터 품질을 원화 기준으로 채택, 세계관 유지·SD 축약 | [16](16_ART_STYLE_GUIDE.md) 반영. 새 게임 자산 제작 완료는 아님 |
| 아트 공통 문서 | 사용자 요청대로10인 공통·배경/컷신·SD 변환을 개별 바이블보다 먼저 작성 | [17](17_OPERATOR_COMMON_STYLE.md)·[18](18_BACKGROUND_CUTSCENE_STYLE.md)·[19](19_SD_COMBAT_MODEL_TRANSLATION.md). 신규 공통 규격은 비교안. 이후 개인20·21 작성은 아래 이력으로 구분 |
| 스킬 컷신 설정 | DAILY/ALWAYS/OFF 3모드. 전체화면 동안전투0,OFF·DAILY반복은기존요약연출 | 최신사용자정정승인. 하루집계·기본값은권장안. [04 §14](04_COMBAT_RUN.md#skill-cinematic-policy)·[18 §8](18_BACKGROUND_CUTSCENE_STYLE.md#cinematic-scope). 실자산/Unity미구현 |
| 이전 개인 전달 | 당시 Reina·Kyle를20·21번으로 수록 | 당시 Reina 상세 진행 승인·Kyle 세부 제안 상태였음. 현재 효력은 후속 외형 채택 행을 적용 |
| 후속10인 외형 기준안 | 22~29 상세안·교차 비교·개인 보완 작성 후 명시한 승인 범위에 사용자 “ㄱㄱ” | **중립 전신의 큰 외형 방향 채택**. 무기/스킬 확인·수치 비교·실물 자산 검수 제외. [현재 범위](#art-baseline-approved) |
| 직전 누적 파일화 | 변경된 기존 본문과 신규 개인8개·안내 자료 | 최초 기준본 대비00/04/11/16 교체·17~29 추가. 당시 검사 기록은 직전 전달 검사 `validation/document_validation_partial_delivery_20260921.md`(과거 검사 참조; 로컬 확보 상태는 점검 보고서)로 보존 |
| 전체 재다운로드 | 사용자 요청에 따라00~29·JSON4종·안내·검사 자료를 개별 재전달 | 정책·수치·아트 승인 범위 변경 없음. 이번 검사는 [문서 검사](42_DOCUMENT_VALIDATION.md)를 따름 |

<a id="run-expiry-approved"></a>
재개 수명·비용 승인은 사용자 “미확정사항 추천대로 확정시켜”가 인용한 해당 안에 적용했다. J04의 일반2/엘리트3웨이브·12노드·증강 가중치는 여전히 별도 검토 항목이다. 로그 보관 승인도 법률·개인정보 범위 검토와 실제 저장·삭제 구현의 완료 선언이 아니다.

이전에는 Instagram 링크 접근에 실패했지만 이후 사용자가 녹화 두 개를 첨부했고, 그 뒤 품질 가이드 작성을 승인했다. 따라서 ‘현재 영상 미열람이라 아트 방향 미채택’이라는 예전 설명을 현행 제한으로 사용하지 않는다. 참조 파일과 실제 제작기법 미확인 범위는 16번에 명시한다.

## 10. 이번 전달본의 사용 방법

**이번 파일은 변경분 모음이 아니라00~29 전체 번호 문서와 data/ JSON4종을 포함한 재다운로드용 전체본**이다. 번호 MD와 안내 문서는 같은 폴더에, 작성용 JSON4개는 그 아래 `data/`에 둔다. 과거 검사3개는 `validation/`에 둔다. 폴더의 실제 이름은 사용자가 선택할 수 있으며 이 다운로드 위치를 실제 프로젝트 루트라고 가정하지 않는다.

직전 아트 전달본의01~29 정책·설계 본문과 승인 범위는 변경하지 않았다. 처음 받은00~16만 있던 경우에는17~29가 추가되는 관계이고, 이전에 이번 채팅의 변경분을 적용했다면 이번 재다운로드 때문에 게임 정책이나 데이터가 새로 바뀌는 것은 아니다. 사용자 로컬에 후속 수정이 있으면 자동 덮어쓰지 않고 원문·승인 근거를 대조한다.

13·14·15와 data JSON4종은 직전 전달에서 본문 미확보였으나 이번에는 파일 목록에서 확인한 버전을 지정하여 전체 추출 텍스트를 확보했다. 긴 JSON2종은 연속 행 구간을 이어 전체 본문으로 복원하고 구문·중복 키·publishable=false를 검사했다. 값·ID·미확정 null을 임의로 채우지 않았다. 추출본과 원본 raw 바이트의 동일성, 사용자 저장소의 현재 상태, 전체 런타임 계약 적합성은 별도다.

기존 검사 기록은 통합 감사 `validation/session_policy_audit_20260921.json`(과거 검사 참조; 로컬 확보 상태는 점검 보고서), J결정 후속 `validation/j_decisions_followup_20260921.json`(과거 검사 참조; 로컬 확보 상태는 점검 보고서), 직전 부분 전달 검사 `validation/document_validation_partial_delivery_20260921.md`(과거 검사 참조; 로컬 확보 상태는 점검 보고서)로 보존한다. 기록의 당시 PASS·미확인 상태·해시를 이번 파일에 대한 검증으로 다시 쓰지 않는다. 원문32개·영상·실제 프로젝트 소스·자산은 별도 증거이며 이번 다운로드에 복제하지 않았다.

`LASTRAIL_CURRENT_ALL.md`는 과거 열람용 합본으로 유지하고 수정 원본으로 사용하지 않는다. 한 규칙의 전문은 담당 문서 한 곳에 둔다. 새30번 통합 바이블이나 ZIP은 만들지 않는다. JSON4종은 작성용이며 publishable=false를 해제하지 않는다.

실제 저장소의 폴더 조사·상세 계약/소스 대조·경로 재배치·구현 차이 보고는 [Codex 통합 지시](41_INTEGRATED_HANDOFF.md)를 따른다. 과거D:/E: 경로와 sandbox 경로를 현재 작업경로로 고정하지 않고 기존 담당 구조를 먼저 확인한다. 문서 이동이 .meta/GUID에 영향을 주면 별도 작업으로 분리한다.

[이번 문서 검사](42_DOCUMENT_VALIDATION.md)는 재전달 파일의 존재·본문 보존·상대 링크/명시 앵커·JSON 구문 등의 확인 결과다. 실제 게임·API·DB·원화·SD·실기기·밸런스 검증이 아니다. 새 채팅은 [다음 채팅 인계](43_NEXT_CHAT_HANDOFF.md)와 실제로 제공된 파일을 기준으로 시작한다.

## 로컬 적용 상태 — 후속 정정이 전달 안내보다 우선

사용자가 추가 제공한00·07·13·20·23·27 전문을 확보했다. 00은 전체 재다운로드 안내가 추가된 판본이며, 로컬 경로·원문 등록부·최신 리롤 정정을 병합했다. 전달 안내의 data/ 폴더 설명은 원래 다운로드 구성이고 실제 JSON4개는 이 폴더에 있다. 과거 검사 문구를 현재 저장소 검사로 재사용하지 않는다. [점검 보고서](../../../QA/DocumentIntegration-20260921/md-cleanup/assets/archive.html#doc-ba36689cc32a3c0b60d0)와 [자료 반영 체크](../../../QA/DocumentIntegration-20260921/md-cleanup/assets/archive.html#doc-71a6b3d577d494ee1689)를 따른다.

## 로컬 상세 원문 경로 — 접수 확인

아래는 파일명 대응과 존재 확인이다. 전달 원본과 바이트 동일성 또는 전문 계약 통합 완료를 뜻하지 않는다. 과거 폐기 정책은 현행으로 복원하지 않는다.

| 출처 | 실제 상세 원문 | 담당 번호 |
|---|---|---|
| R01 | [2026-09-05-network-content-startup.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-8cdfe2238dd38298e0ad) | 02 |
| R02 | [2026-09-06-addressables-runtime.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-94989c49f5fa630540d7) | 02 |
| R03 | [2026-09-06-startup-auth-integration.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-bc1205c0f343c248637e) | 01 |
| R04 | [2026-09-06-player-entry-runtime-and-admission.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-cd9bd69201847971a07f) | 01 |
| R05 | [2026-09-06-login-loading-lobby-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-1b9dc0b62a5e5962edbb) | 01 |
| R06 | [2026-09-06-password-email-ui.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-c23bff649afee32c3dab) | 01 |
| R07 | [2026-09-06-password-email-ui-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-624a0e756bca908bdead) | 01 |
| R08 | [2026-09-06-required-update-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-356bad83f06db4c037a4) | 02 |
| R09 | [2026-09-06-support-inquiry.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-915653596aacc92fcd39) | 03 |
| R10 | [2026-09-06-support-diagnostics-http-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-ed5aecef2ed012bd1916) | 03 |
| R11 | [2026-09-06-receipt-restart-recovery.md](44_COMBAT_DETAIL_CONTRACTS.md#source-491cfbace9035662) | 03 |
| R12 | [2026-09-06-receipt-recovery-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-0b3709541b555ef072a3) | 03 |
| R13 | [2026-09-06-durable-run-operations.md](44_COMBAT_DETAIL_CONTRACTS.md#source-e894b886198cfc95) | 04 |
| R14 | [2026-09-06-durable-run-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-6b081df063799c37f55d) | 04 |
| R15 | [2026-09-06-content-state-baselines.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-dd4f6182095cbd325b76) | 02 |
| R16 | [2026-09-07-content-state-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-ea1cf27bccefa63eb77e) | 02 |
| R17 | [2026-09-07-synthetic-content-delta-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-65d96c9caa9cad00cc55) | 02 |
| R18 | [2026-09-07-backend-content-release-transition-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-dc1feb6eb3709aa97473) | 02 |
| R19 | [2026-09-07-pre-lobby-process-audit.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-b3169ffe392218460949) | 01 |
| R20 | [2026-09-07-main-integration-implementation.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-1a8cc13024cec6d81804) | 01 |
| R21 | [2026-09-07-main-integration-verification.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-4b3c40332749c415de44) | 01 |
| R22 | [2026-09-13-combat-second-final.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-3d6004241c9dfe4ca38e) | 04 |
| R23 | [2026-09-16-combat-reward-settlement.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-87a2a5811d77e667e32c) | 06 |
| R24 | [2026-09-16-combat-ad-api.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-9e294fa25a49c33f9e34) | 06 |
| R25 | [2026-09-17-unity-combat-roulette.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-ceb24e8c514c0457bd4e) | 06 |
| R26 | [2026-09-17-shared-roulette-popup.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-6550acc0b88939f906e0) | 06 |
| R27 | [2026-09-17-roulette-approved-polish.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-61324e1262dd74967300) | 06 |
| R28 | [2026-09-17-ad-skip-local-integration.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-514303eac46ac52f6ba3) | 07 |
| R29 | [2026-09-17-ad-skip-server.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-1abea96db825288c5dbb) | 07 |
| R30 | [2026-09-17-ad-skip-unity-phase1.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-b5053d073811199f9bf0) | 07 |
| R31 | [2026-09-17-augment-reroll-server-phase2.md](48_AD_REWARD_DETAIL_CONTRACTS.md#source-a54cbe3073573484) | 05 |
| R32 | [2026-09-17-ad-skip-unity-phase2.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-8c30e7d966b4b128d65c) | 05 |


## 2026-09-21 의미 기준 문서 정리

남은 2,167개에서 종료·대체·통합된 Markdown 1,534개를 추가 제거하고 통합 참조 3개를 설치했다. 원문과 역사 스냅샷의 동반 meta 232개는 해시 검증한 ZIP에 보존했다. 실제 Unity Assets의 meta와 코드·이미지·DB는 이 정리에서 삭제하지 않았다. 나머지 문서는 모두 최종 기획이라는 뜻이 아니며 기술 계약·리소스 출처·외부 도구·인계 자료를 포함한다. 서로 다른 상세 계약 90개는 아직 별도 파일이 필요해 유지했다. 정리 완료를 구현·승인·전체 API 통합 완료로 해석하지 않는다.

- [증강 40종 상세 참조](31_AUGMENT_EFFECT_REFERENCE.md)
- [서사·대본 참조](32_NARRATIVE_REFERENCE.md)
- [현재 미완료 작업](33_CURRENT_WORK_STATUS.md)
- [검색 가능한 원문 보관함](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html)
- [파일별 유지·제거 근거](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/all-file-decisions.json)

## 30~52 추가 번호 문서

기존 00~29는 유지한다. 아래 문서는 같은 최종 폴더의 정식 편집 위치다. 통합 상세 안의 후보·역사적 계약을 새로 승인한 것은 아니다.

| 번호 문서 | 담당 범위 |
|---|---|
| [30 공통 코드·리소스 규칙과 전투 핵심 철학](30_PROJECT_RULES.md) | 통합 상세 계약·원문별 상태/출처 |
| [31 증강 40종 효과 참조 — 현행 정책과 미승인 효과 후보](31_AUGMENT_EFFECT_REFERENCE.md) | 기존 통합 보완 문서의 번호 지정 |
| [32 내러티브 현행 경계와 보존 대본 색인](32_NARRATIVE_REFERENCE.md) | 기존 통합 보완 문서의 번호 지정 |
| [33 현재 구현·검수 잔여 — 문서 보관 전 인계](33_CURRENT_WORK_STATUS.md) | 기존 통합 보완 문서의 번호 지정 |
| [34 전투·증강·경로 원문 보완 계약](34_COMBAT_POLICY_COMPANION.md) | 기존 통합 보완 문서의 번호 지정 |
| [35 현행 반복 경제·영구 성장 보존 부록](35_CURRENT_ECONOMY_ANNEX.md) | 기존 통합 보완 문서의 번호 지정 |
| [36 운영 상세 계약 보존 보충](36_OPERATIONS_COMPATIBILITY.md) | 기존 통합 보완 문서의 번호 지정 |
| [37 화면 승인·구현 기록의 적용 경계](37_UI_APPROVAL_AND_LEGACY_BOUNDARIES.md) | 기존 통합 보완 문서의 번호 지정 |
| [38 누적 기획 충돌 대조·보존 기준](38_DOCUMENT_RECONCILIATION.md) | 기존 통합 보완 문서의 번호 지정 |
| [39 상세 원문 등록부](39_SOURCE_REGISTER.md) | 기존 통합 보완 문서의 번호 지정 |
| [40 LAST RAIL | 누적 아트 문서 전달·변경 안내](40_ART_DELIVERY_CHANGES.md) | 기존 통합 보완 문서의 번호 지정 |
| [41 LAST RAIL | Codex 전체 기획·문서 통합 정리 지시문](41_INTEGRATED_HANDOFF.md) | 기존 통합 보완 문서의 번호 지정 |
| [42 LAST RAIL | 최종 전달본 문서 검사](42_DOCUMENT_VALIDATION.md) | 기존 통합 보완 문서의 번호 지정 |
| [43 LAST RAIL | 다음 채팅 인계](43_NEXT_CHAT_HANDOFF.md) | 기존 통합 보완 문서의 번호 지정 |
| [44 전투·피해·조작·복구 상세 계약](44_COMBAT_DETAIL_CONTRACTS.md) | 통합 상세 계약·원문별 상태/출처 |
| [45 선로·맵·장애물·정비역 상세 계약](45_MAP_STATION_DETAIL_CONTRACTS.md) | 통합 상세 계약·원문별 상태/출처 |
| [46 협동·매칭·입장·역할 상세 계약](46_COOP_DETAIL_CONTRACTS.md) | 통합 상세 계약·원문별 상태/출처 |
| [47 성장·반복 보상·출석·패스 상세 계약](47_ECONOMY_DETAIL_CONTRACTS.md) | 통합 상세 계약·원문별 상태/출처 |
| [48 광고 스킵·룰렛·지급 복구 상세 계약](48_AD_REWARD_DETAIL_CONTRACTS.md) | 통합 상세 계약·원문별 상태/출처 |
| [49 서버 API·상태 소유권 상세 계약](49_SERVER_API_CONTRACTS.md) | 통합 상세 계약·원문별 상태/출처 |
| [50 콘텐츠 배포·서명·운영·문의 상세 계약](50_RELEASE_OPERATIONS_CONTRACTS.md) | 통합 상세 계약·원문별 상태/출처 |
| [51 화면 승인·UI·아트 제작 상세 계약](51_UI_ART_DETAIL_CONTRACTS.md) | 통합 상세 계약·원문별 상태/출처 |
| [52 과거 담당 인계·개발 환경 참조](52_PROJECT_HANDOFF_REFERENCE.md) | 통합 상세 계약·원문별 상태/출처 |

이번 번호 정리로 기존 원문 238개를 대체·삭제했다. [원문→번호 위치 대조표](../../../QA/DocumentIntegration-20260921/md-cleanup/numbered/source-map.json)에서 과거 경로를 찾을 수 있다. 현재 작업은 [33 잔여 작업](33_CURRENT_WORK_STATUS.md), 상세 계약은 44~51번을 참조한다.

## 53 Unity 기술·음원 제작 참조

[53번 문서](53_UNITY_TECHNICAL_AND_ASSET_REFERENCE.md)는 Assets에 흩어진 기술·제작 기록 15개의 통합 위치다. 최신 정책/구현 상태와 원문 작성 당시 상태를 구분한다. 구버전 자산·종료 감사 기록은 [검색 보관함](../../../QA/DocumentIntegration-20260921/md-cleanup/assets/archive.html)으로 정리했다.

현재 작업은 [33번 C01~C20 변경 영향 대조](33_CURRENT_WORK_STATUS.md#change-impact-checklist)를 선행하고 [I01~I26 기능 목록](33_CURRENT_WORK_STATUS.md#implementation-backlog)으로 연결한다. 캐릭터·스토리·ID 및 신규 화면/광고 상태 검토 없이 구시안을 먼저 완성하지 않는다. 변경 영향 없는 비전투 작업은 계속 진행할 수 있다.

<a id="plan6-decision-agenda"></a>
## 10. 후속 기획 결정 순서 — 2026-09-21 / 1번 방향 승인·2/3번 미결

Plan_6이 작성한 후보 중 다른 담당의 연결을 막는 결정을 모은 **검토 순서**다. 승인 원본은 각 담당 문서에 유지한다. 이 표 작성, 다음 작업 진행 요청, 선행 준비 기록을 설계 승인으로 취급하지 않는다. J01~J11의 이미 승인된 항목을 다시 열거나 구현 완료로 바꾸지 않는다.

| 순서·연결 | 결정할 내용 | 현재 추천 | 결정 이후에도 남는 선행 |
|---|---|---|---|
| 1 / WAIT-001 기본3인 스킬 — 방향 승인 | Eve 센트리 설치/강화, Sera 생존 회복/다운 복귀 단축, Mira 집중 연사/다중 연사라는6개 핵심 효과 방향 | [44 역할 후보](44_COMBAT_DETAIL_CONTRACTS.md#eve-sera-mira-skill-draft-v01)를 방향으로 채택하고 [기본5인 대응 비교안 A](44_COMBAT_DETAIL_CONTRACTS.md#initial-five-counter-gap-v01)를 우선 검증 대상으로 삼기. 중단을 필수 보유 조건으로 만들지 않는 방향 | 지원3개 실제 능력/기본 접근성, 센트리·다운·행동 큐 계약, 수치·버전·생존 검증. 보스 피해/주기 변경이나 출시 승인까지 포함하지 않음 |
| 2 / WAIT-003 이야기 경로 | 다음 미완료 사건의 선택 가능한 경로 제시, 일반 경로 우회, 작전당 신규 사건 수 | [32 A안](32_NARRATIVE_REFERENCE.md#story-random-map-v01): 이야기/일반 경로를 선택하며 첫 시험은 신규 사건 최대1개. 단7사건 진행에는 최소7회의 사건 성공 작전이 필요하다는 비용을 함께 판단 | 경로 제시 보장/첫 이야기 예외의 정확한 조건, J04 노드·웨이브, 사건별 성공 근거·START 이행, 구체 수령/복구 계약. 클리어·첫 보상은 최신 정정에 따라 스토리 작전 승리 시 확정 |
| 3 / WAIT-004 장비 조작 | 합성 결과의 장착 위치, 잠금 장비의 파괴적 변경 보호 | [13 상세 후보](13_EQUIPMENT_ENCHANTMENTS.md#equipment-screen-flow-v01): 주재료 장착 슬롯에 결과 유지/주재료가 보관함이면 결과도 보관함, 보조 슬롯 비움. 잠긴 재료의 소모·인챈트 교체는 명시적 해제 후 진행 | 잠금의 나머지 보호 범위, 반환 장비 용량 산정, 시술/획득 보류 계약, 실제 instance 명령·원거래 복구·화면 승인. 수수료/재료 수/승계는 기존 J02/J03 유지 |

사용자 승인(2026-09-21, 추천대로 진행): 기본3인 일반/궁극기6개 핵심 효과 방향을 채택한다. Eve 센트리 설치/강화, Sera 생존 회복/다운 복귀 단축, Mira 집중 연사/다중 연사. 초기5인 대응 비교안 A를 우선 검증 대상으로 채택한다. 세부 대상·수명·행동 큐 계약과 작업명·수치·보스 조정값·버전·구현·출시는 별도 확인/검증 대상이다. 이야기 경로와 장비 조작의 다른 결정 묶음은 이번 승인에 포함하지 않는다.

### 결정과 구현 착수를 구분하는 방법

- 사용자가 특정 행의 추천을 선택하면 그 행에서 명시한 결정 범위만 원본 문서와 해당 WAIT 항목에 반영한다. 다른 행·남은 수치·아트·운영 배포까지 묶어 승인하지 않는다. 답변의 지시 대상이 모호하면 실제 승인 범위를 확인한다.
- 기본3인 방향이 정해져도 구 스킬 입력을 즉시 덮어쓰지 않는다. 계약/버전과 비교 검증을 준비한 뒤 각 소유자가 승인된 구현 단위를 맡는다. 현재 이 정리만으로 타 작업에 자동 실행 메시지를 보내지 않는다.
- [스토리 클리어·첫 보상 확정](32_NARRATIVE_REFERENCE.md#story-first-reward-v01): 스토리 작전 승리 시 클리어/진행과 첫 보상을 정산한다. 사건 직후 진행 저장은 대체됐고 A안은 비채택. 패배·포기·미승리 만료에는 이번 클리어/첫 보상 없음. 구체 지급·복구·기존 계정 이행은 별도.
- 기존7인 설정·아트 후보는 [17](17_OPERATOR_COMMON_STYLE.md#existing-seven-design-v01)·[32](32_NARRATIVE_REFERENCE.md#existing-seven-personality-v01)에서 개별 검토한다. 얼굴/헤어/무기/최종 원화를 이3개 결정에 묶지 않는다. 홈페이지는 이미 만든 결과를 유지하며 공개 계약 연결은 기존 WAIT-006/007 담당 범위다.

이번 단위의 완료 범위는 결정 목록 정리와 원본 연결 확인이다. 새 스킬/보상/장비 명령을 구현하거나 실행 테스트를 수행한 기록이 아니다. 계획·대기 원본: [33 상태](33_CURRENT_WORK_STATUS.md), [공용 대기 목록](../../handoffs/INTEGRATION_WAITLIST.md).
- 2026-09-22 사용자 승인: [목적지3선택·선로2방향 분리 A](14_MAP_EVENTS_DROPS.md#destination-track-choice-v01). 내부 좌우 선로는 선택한 동일 목적지로 연결한다. [확정 입력·시간 연결](14_MAP_EVENTS_DROPS.md#route-confirm-time-v01)은 후속 미승인 후보이며 J04 수치·실제 구현 승인은 포함하지 않는다.
- 2026-09-22 사용자 승인: [목적지 미리보기 A](14_MAP_EVENTS_DROPS.md#destination-preview-info-v01). 다음 목적지 사건/확인된 위험/보상 기회 공개, 먼 사건·결말 비공개, 확정/확률 보상과 미공개/조회 실패 구분, 스토리 승리 지급 조건 및 미확정 수치 미표시. 실제 데이터/표시 연결과 앞선 확정·시간 후보는 별도 대기.
- 2026-09-22 사용자 승인: [도착 후 사건 화면 자동 연결](14_MAP_EVENTS_DROPS.md#destination-arrival-detail-v01). 도착 검증·진행/준비 조건 충족 후 별도 입장 버튼 없이 연결하며 비용/선택의 기존 확인 절차는 유지한다. 연출 완료가 진입 판정을 소유하지 않는다. 저장·복구·로딩/시간 계약과 선로 확정 세부안은 별도 대기.
<a id="detail-decision-agenda-20260922"></a>
## 상세기획 후속 결정 묶음 — 2026-09-22 / 검토 목록

할 일6번 결과. 아래는 최근 상세기획과 직접 연결된 남은 결정이며 프로젝트 전체 미결 전수 목록이 아니다. 사용자 “ㄱㄱ”는 이 목록 정리 지시로 처리했다. 표 자체·상세 문서 작성·시각 승인을 게임 정책의 일괄 승인으로 바꾸지 않는다. 상세 원본은 링크된 문서가 소유한다.

### 이미 결정되어 다시 묻지 않는 범위

목적지3선택/같은 목적지 내부 좌우 선로 분리, 다음 목적지 정보 공개 A, 도착 확인/준비 후 사건 화면 자동 연결(비용/선택 확인 유지), 스토리 작전 승리 시 계정 클리어/첫 보상, J02/J03의 합성·승계·새 고유효과 공개 규칙은 유지한다.04 §2 기존 분기0.01/복귀0.6초·명시confirm·timeout 없음과 §4 저널 명시 재시도도 기존 기준이다. 이 숫자/규칙 자체를 새 미결로 돌리지 않고 새 화면·선로와의 결합만 결정한다.

### A. 사용자 결정 묶음과 추천 순서

| 순서 | 남은 결정·현재 추천 | 선택에 따른 차이·주의 | 원본/대기 |
|---|---|---|---|
| D1 조작·복귀 UX — 승인 | 사용자 표시 수정: 선로 확정 버튼 옆 변경 불가 경고 문구는 제외. 목적지는 카드 미리보기+확정1회·추가 팝업 없음. 선로는 좌우 미리보기+확정, 첫 연결에서는 확정 후 재선택 불가. 원 기록/재진입이 보장되면 결과 확인 지연 중 로비 복귀 허용. 재접속은 읽기 확인 후 계속하기 | 반복 팝업을 줄이되 선로 재선택 편의는 제한됨. 뒤로/닫기는 거래 취소·보상 포기 아님. 기존0.01/0.6은 유지하고 중첩 정지 결합은 기술 검증 필요 | [14 입력](14_MAP_EVENTS_DROPS.md#route-confirm-time-v01), [화면](14_MAP_EVENTS_DROPS.md#destination-screen-detail-v01), [04 종료](04_COMBAT_RUN.md#operation-end-lobby-detail-v01), [복구](04_COMBAT_RUN.md#resume-flow-detail-v01) / WAIT-003 |
| D2 장비 배치·잠금 — 승인 | [승인 정책](13_EQUIPMENT_ENCHANTMENTS.md#merge-placement-lock-v01): 결과는 주재료 위치, 보조 장착 슬롯 비움. 잠금은 합성·인챈트 신규/교체·이벤트 승급 차단, 장착 이동 허용·잠금 유지. 명시 해제 후 실행 | 2026-09-22 사용자 승인: 위 배치·잠금 정책 채택. 원 슬롯 적격성·최종 용량·실제 거래 검증은 별도 | [13 화면](13_EQUIPMENT_ENCHANTMENTS.md#equipment-screen-flow-v01), [비교](13_EQUIPMENT_ENCHANTMENTS.md#equipment-change-detail-v01) / WAIT-004 |
| D3 보상 공간 부족 — 승인 | 일부 수령·이번 새 보상 수령 포기를 허용하고 중간 노드는 잔여 항목을 해결한 뒤 진행. 최종 노드는 쓸 수 없는 런 장비 정리를 강제하지 않음 | 합성 불가 상태의 막힘 해소. 포기한 새 보상은 되돌릴 수 없으며 기존 보유 장비 삭제/분해·영구 보상 포기와 다름. 원 항목별 거래 및 종결 처리 필요 | [13 보류](13_EQUIPMENT_ENCHANTMENTS.md#equipment-overflow-flow-v01), [14 결과](14_MAP_EVENTS_DROPS.md#node-result-detail-v01) / WAIT-004·003 |
| D4 인챈트 사건 — 승인 | 독립 인챈트 A: 최종 실행 때 추첨/공개, 기존 효과 교체 손실 확인. 최종 실행 때 선택권 소비. 진행 불가/원치 않을 때 명시 확인 후 보상 없이 지나가기 후보 | B 사전 효과 공개보다 결과 예측성이 낮음. J03 고유효과와 별도 결정. 영구 무효 선택지의 보정 방식은 아직 추천 확정 전 | [13 사건](13_EQUIPMENT_ENCHANTMENTS.md#enchant-event-flow-v01) / WAIT-004 |
| D5 화면 정리 — 승인 | 사건 결과+보상 처리 상태를 한 화면에 표시하고 정비는 필요할 때만 진입. 장비 변경은 같은 상세에서 비교/손실 확인 후 실행1회, 원 맥락 복귀 | 반복 팝업 감소. 최신 승인 시안의 호스트/모달 구성과 충돌하면 재조정. 시안 전체 재제작 승인 아님 | [14 결과](14_MAP_EVENTS_DROPS.md#node-result-detail-v01), [13 변경](13_EQUIPMENT_ENCHANTMENTS.md#equipment-change-detail-v01) / WAIT-003·004 |
| D6 J04 시험 규모 — 승인 | 방문12노드(시작 제외·최종보스 포함), 일반2wave/엘리트3wave/보스1wave3phase와 기존 가중치 제안을 별도 시험안으로 검토 | 작전 길이·장비 공급·맵 분포에 큰 영향. 승인돼도 출시 밸런스 확정 아님. 증강 최소 보장 없음은 유지 | [14 시험안](14_MAP_EVENTS_DROPS.md#node-wave-proposal), §3/9 / WAIT-003 |
| D7 이야기 경로 — 승인 | 다음 미완료 사건 경로와 일반 우회 경로 선택, 첫 시험은 신규 사건 최대1개/작전 |7사건 진행에는 최소7회의 해당 사건 승리 작전 필요. 경로 제시 보장·시작 예외는 별도 미결. 첫 보상 시점은 이미 승리 정산으로 확정 | [32 경로](32_NARRATIVE_REFERENCE.md#story-random-map-v01) / WAIT-003 |

추천 검토 순서는 D1 → D2 → D3 → D4 → D5 → D6/D7이다. 특히 D3의 수령 포기와 D4의 무작위 교체·보상 없는 통과는 되돌릴 수 없는 사용자 선택이므로 조작 편의 승인에 묶어 자동 확정하지 않는다. 항목별 선택 범위만 원본에 반영하고 조건이 남으면 해당 조건은 결정 필요로 보존한다.

### B. 사용자가 다시 승인할 항목과 구분할 연결·검증 대기

- Server/Unity: 원 명령 식별·멱등/조회 일관성·revision/종결 경합, DTO/오류/버전 매핑, 원 결과 복구·계정 격리. 이미 정해진 정책을 실제 계약으로 구현하는 작업이며 후보 정책 확정 권한은 아님.
- 전투/진행: 체크포인트가 보장하는 범위, 도착 판정/진입 저장 경계·꼬리/잔여 객체 수명, 자산 준비 실패 때 안전 대기와 시간 조정. 기존 근거를 먼저 확인하고 게임 동작의 새 선택이 필요한 차이만 Plan으로 올린다.
- 장비: 장착/반환 장비의 용량 산정·실제 적격 풀·정비/잠금 계약. 원 정책이 없으면 구현 편의로 확정하지 않고 D2~D4에 결정 필요로 연결한다.
- Direction/Unity: 최신 승인 시안과 기획의 전체 화면/내부 상태/모달 호스트·입력·복귀 대조, 독립 자산·실제 크기·스크롤·수명 검수. 작전 일지는04 v0.2의 제한된 문서 대조만 끝났고 실제 연결은 미검증이다.
- Server: 스토리 승리/진행/첫 지급 원자 경계와 구 계정/위험도 이행, 지급 상태/영수증 연결. 최신 승리 지급 원칙을 다시 선택하는 일이 아님.

### C. 다음 작업·완료 판정

이6개 상세기획 단위의 문서 작성/보완은 끝났으나 각 후보의 승인·구현·실기 검수는 별개다. 다음에는 D1의 조작·복귀 조건부터 결정 범위를 확정하는 것이 우선이다. 사용자가 별도 기획을 지정하면 승인된 독립 기획을 진행하며 미결을 임의 구현하지 않는다. 일괄 통합은 [대기 목록](../../handoffs/INTEGRATION_WAITLIST.md)의 기획/시안/구현 대조·보완·재검증 절차를 적용한다. 이 표를 이유로 다른 작업에 자동 메시지·폴링·실행 지시를 보내지 않는다.
- 2026-09-22 지휘실 확정 정정: [단일 트리 확장 상세](09_COMMANDER_PROGRESSION.md#commander-unified-tree-v01).50레벨·레벨당1P·상위6종25레벨부터·부모OR·유료 상위·진행 snapshot 유지. 기존12P/트리8P/무료 상위는 폐기. 일반37+상위6=43노드,1레벨 최초1P·상위3P·40레벨두번째/최대2·효과/XP/환불 이행안은 새 시험 후보. J05의 기존 일반13/상위6기믹 방향은 보존하되 구 접근/지급 틀을 복원하지 않는다. 최종 시안은 확장안 확정 후 제작.
- 2026-09-22 지휘실 제작 인수: [09 v1.1 확정 검토본](09_COMMANDER_PROGRESSION.md#commander-production-spec-v11). 43노드·106랭크·50레벨 지급표, 저장/활성 구분·연쇄환불·이행·12화면 상태. 신규 수치는 추천안이며 운영 승인 아님. 통합 WAIT-011.

- 2026-09-22 지휘실 v1.1 추가 수치·정책 사용자 최종 승인: 09 상단 승인 기록 우선. 앞선 후보/미승인 기록은 승인 전 이력이며 미술·실기·운영 발행은 별도 대기.
- 다음 기획 D3 상세 검토안: [선택 수령·남은 장비 수령 포기](13_EQUIPMENT_ENCHANTMENTS.md#equipment-reward-resolution-v02). 선택 집합 전부 성공/미적용, 남은 전체 포기 확인, 복구·최종 종결·12수용 사례 명세. D3는 사용자 승인 대기, 지휘실 승인과 별개.

- 2026-09-22 D3 사용자 승인: [13 선택 수령·포기 v02](13_EQUIPMENT_ENCHANTMENTS.md#equipment-reward-resolution-v02) 추천 묶음 채택. 일부 수령·남은 원 장비 명시 포기·원자 처리·중간 잔여 해결·최종 무강제 정리. API/시안/실기 별도.
- 다음 D4 [독립 인챈트 정책 v02](13_EQUIPMENT_ENCHANTMENTS.md#enchant-event-policy-v02): 실행 후 추첨 A·성공 시 선택권 소비·교체 확인·최초 부족 후보 보충/이후 재생성 없음·명시 무보상 통과와14수용 사례. 신규 D4 묶음 승인 대기.

- 2026-09-22 D4 사용자 승인: [13 인챈트 v02](13_EQUIPMENT_ENCHANTMENTS.md#enchant-event-policy-v02) 추천 묶음 채택. 실행 후 추첨 A·성공 소비·교체 확인/수용·최초 부족 후보 보충/이후 재생성 없음·명시 무보상 통과. 확률표·실제 연결/검증 별도.
- D5 [14 통합 결과·정비 연결 v02](14_MAP_EVENTS_DROPS.md#result-equipment-navigation-v02): 상태별 행동·확인 횟수·원 사건 복귀·기존 시안 차이6건·수용12사례. 신규 UX 승인 대기, 문서 대조만 수행.

- 2026-09-22 D5 사용자 승인: [14 통합 결과·정비 연결](14_MAP_EVENTS_DROPS.md#result-equipment-navigation-v02)의 통합 결과/보상·필요시 정비·동일 상세 비교/결과·원 사건 복귀·확인 횟수 채택. 최신 시안/실기 검증 별도.
- D6 [작전 규모 시험 v02](14_MAP_EVENTS_DROPS.md#operation-scale-trial-v02):12노드·웨이브/보스·생성 제약/가중치·지휘실 증원 집계·20~30분 측정 목표·1000seed/4전략·실기12회 계획. 신규 시험 패키지 승인 대기, 실제 시험 미실행. D7 스토리 배치는 별도.

- 2026-09-22 D6 사용자 승인: [14 시험 v02](14_MAP_EVENTS_DROPS.md#operation-scale-trial-v02) 첫 일반 솔로 시험 패키지 채택. map_drop_draft.json에 승인 범위 메타데이터 추가, publishable=false 유지. 실제 시험/운영 발행 별도.
- D7 [32 스토리 경로 v02](32_NARRATIVE_REFERENCE.md#story-route-policy-v02): A 선택경로·신규1사건·적격시 접근/우회 보장·첫층/4~8층/최종 대체 예외·승리 정산/복구14사례. 신규 경로 정책 승인 대기.

- 2026-09-22 D7 사용자 승인: [32 v02](32_NARRATIVE_REFERENCE.md#story-route-policy-v02) 추천 묶음 채택. D2~D7 정책/시험안 승인 반영,실제 구현·시안·검증은 별도.
- 남은 D1 [04 확정/복귀 v02](04_COMBAT_RUN.md#navigation-resume-policy-v02): 미리보기+확정1회·재선택 없음·조건부 로비복귀·명시 계속하기12수용 사례 작성,신규 UX 승인 대기. 선로 옆 경고 삭제는 기존 승인 유지.


<a id="cross-session-contract-review-20260922"></a>
## 승인 이후 세션 간 계약 대조 — 2026-09-22 / 제한된 문서 검토

Plan_6. D1~D7과 지휘실 v1.1 추천 묶음은 사용자 승인 반영 완료다. 기존 상세 본문의 승인 대기 표현은 각 원본 상단/후속 승인 기록이 해당 범위에서 우선한다. 문서 승인을 미술 최종 승인·개발 완료·운영 발행으로 바꾸지 않는다.

검토 범위: 원본04/09/13/14/32, INTEGRATION_WAITLIST, Direction REVIEW/장비 후보 코드/지휘실 v1.1 REVIEW·manifest·source-snapshot, 기존 Unity/Server 인계의 관련 기록. 실제 코드 전수 감사·API/DB 호출·실행 화면 검수는 이번 범위 밖이다. Server의 최근 DEV 배포 성공 기록은 이번 신규 장비/스토리/지휘실 계약 구현의 증거가 아니다.

| 항목 | 확인 결과/분류 | 담당·다음 인수 근거 |
|---|---|---|
| D1~D7 승인 상태 | 과거 후보/미승인 표기와 최신 승인 공존. 정책 선택 대기는 해제 | Plan 최신 승인 링크를 소비 기준으로 고정,이력 보존 |
| 지휘실 v1.1 데이터 | 원본 JSON과 시안 source-snapshot의 nodes/rankRows/levels/proposed/examples/effectContract 6구역 직렬화 대조 일치 | Direction 재사용 가능;미술 승인/실제 동작 별도 |
| OC01 정합화 완료 | 별도시너지폐기·실제3종장비 요원피해10%감소 첫시험기준·구투자이행명세 | 09 최종기준/S1 v02 소비→실제효과/이행/화면검증;정책충돌종료·구현대기 |
| 장비 시안 | 기존 full은 정비 이동만, enchant는 연결 대기, 결과는 예시 보관함 복귀 | Direction D2~D5 상태 보완,Server 항목/원거래/용량,Unity 원 사건 복귀 |
| 스토리 맵 | D6 일반 최종1연결과 D7 마지막 스토리2선택은 프로필별 명시 예외로 양립 | Server 프로필 구분·도달/우회·7사건 성공/실패 매핑;일반 그래프 전역 변경 금지 |
| 작전 결과/기록 | D1 결과 지연 중 로비 복귀와 기록 상세 수령 후 상세 유지는 진입 맥락이 달라 양립 | Unity 진입 문맥/원 run 유지,Server 원 결과 복구,Direction 기존 승인 상세 보존 |
| 실제 구현 상태 | Unity 관련 인계는 새 장비/내실 instance/API/START 경계 선행을 남김. 현재 소스 전수 확인 안 함 | 담당 최신 구현 커밋·계약·테스트 근거 필요. 미구현 단정/완료 주장 모두 금지 |

지휘실 대조는 정책 데이터의 원본 사본 일치이며 실제 View가 모든 필드를 정확히 표현/실행한다는 증명은 아니다. Direction 보고의 규칙16/입력7·23장면 QA는 보고된 범위로만 참조했고 다시 실행하지 않았다. manifest의 artApproval=pending-user-review를 확인했다. 장비와 결과 화면 미술의 승인 범위도 각각 확인해야 한다.

### 담당별 보완 순서와 닫기 조건

1. Server: 기존 명령/조회/영수증/revision·실패/미확인 구분을 각 승인 정책에 매핑하고 미지원 기능을 표시한다. 새 API가 존재한다고 가정하지 않는다. 장비 선택 집합 원자 처리·이벤트 소비·작전 종결 경합,스토리 승리 진행/첫권리,지휘실 태그/이행/START snapshot 계약이 핵심이다.
2. Plan: 실제 계약이 필요한 규칙 공백(우선 OC01 태그 선택)을 기존09에 보완한다. 확정 정책의 단순 연결은 다시 승인 대기로 돌리지 않고 새 정책 선택이 필요한 차이만 구분한다.
3. Direction: 승인된 원본을 유지하고 D1~D5 입력/상태/복귀·OC01 선택·D7 표식을 최신 미술에 반영한다. 오래된 후보를 최종 승인본으로 대체하지 않는다.
4. Unity: 기존 Controller/Model·팝업/로딩을 재사용해 입력/저장/복귀를 연결하고 중복 실행·해제 후 콜백·화면 종료 후 복구를 검증한다.
5. 통합: 같은 기획 절/계약 버전/시안 revision/구현 근거를 지정하고 원 요청 유실·기기 재시작·계정 전환·종결 경합·실제 크기 입력 검수를 수행한 뒤 WAIT 항목을 닫는다.

목록의 순서는 자동 타 작업 실행 지시가 아니다. 의존 없는 독립 작업은 병행 가능하며 같은 파일을 동시에 수정하지 않는다. 이번에는 기존 WAIT-003/004/010/011에 결과를 기록했으며 새 보고서/타 세션 메시지/폴링/런타임 변경은 없다.

- 2026-09-22 OC01 공백 보완: [09 태그 선택/저장/환불 v01](09_COMMANDER_PROGRESSION.md#oc01-tag-selection-v01). 기존 상세 내 선택·미선택 저장 차단·선호 저장/현 편성 START 적격 분리·환불/초기화 연동14수용 사례. 신규 UX/저장 경계 승인 대기,실제 카탈로그/DTO는 Server 선행.

- 2026-09-22 OC01 선택·저장 보완 사용자 승인: [09 태그 선택 v01](09_COMMANDER_PROGRESSION.md#oc01-tag-selection-v01) 추천 묶음 채택. 해당 신규 기획 결정 대기 해제. 실제 카탈로그/원자 저장·START 계약,Direction 추가 상태/미술 승인,Unity 연결/14수용 사례는 WAIT-011 대기.

- 2026-09-22 지휘실 실제 계약 제한 조사: [09 소스 대조 v01](09_COMMANDER_PROGRESSION.md#commander-contract-source-audit-v01). commander_presets/formation API는 요원5명 편성 저장임을 확인. 신규 특성/태그 계약과 분리,출격/영수증 재사용 경계 및 Server 의미 계약 명세. 실제 코드/DB/HTTP 변경 없음.

- 2026-09-22 [13 작전 장비 소스 대조](13_EQUIPMENT_ENCHANTMENTS.md#equipment-contract-source-audit-v01): equipment level-up 경로는 실제 거절,CE03 서버/Unity는 equipment OFF만 지원,기존 run reward claim은 노드 item 집합 거래와 별개. 수령/포기/합성/시술/종결·전투효과 계약 의미 및 담당 인수 명세 작성. 코드/DB/HTTP/테스트 변경 없음.

- 2026-09-22 [14 경로/스토리 실제 계약 대조](14_MAP_EVENTS_DROPS.md#route-story-contract-audit-v01): 기존15wave·좌우증강 offer와 신규12노드 그래프 분리,FINISH 원 사건 성공근거/최종목표 결합,위험도별 기존 첫클리어와 사건 최초권리 매핑 명세. 실제 실행/이행은 대기.

- 2026-09-22 [04 복구 실제 계약 대조](04_COMBAT_RUN.md#recovery-contract-source-audit-v01): 실제 DurableRunClient/암호화 저널·계정격리/명시재시도 기반 확인. 현재 요청3종과 신규거래의 경계,로비 복귀 영속성 조건·계속하기 전 Step 차단·실기 저장검증 잔여 기록.

- 2026-09-22 [담당별 착수 묶음](../../handoffs/INTEGRATION_WAITLIST.md#approved-contract-work-packages-20260922): S1~S3 서버 계약,U1 복구 조사,R1 상태 시안은 독립 선행;U2~U4/E1 소비·효과 연결 및 I1 통합은 계약 확보 후. 첫 인수 S1 지휘실 추천. 자동 지시/구현 완료 아님.

- 2026-09-22 [S1 지휘실 계약 초안](09_COMMANDER_PROGRESSION.md#commander-s1-contract-draft-v01): 논리 DTO·저장/활성 revision·원요청 재생·START 버전결속·오류/경합 fixture 작성. Plan 초안 준비이며 실제 Server API/카탈로그/DB 구현은 미완료.

- 2026-09-22 [S2 장비 계약 초안](13_EQUIPMENT_ENCHANTMENTS.md#equipment-s2-contract-draft-v01): instance/보상item/이벤트 상태·논리명령10종·멱등/revision·경합9행·S2-a~d 인수 명세. 승인 정책 구체화이며 실제 API/DB 구현 아님.

- 2026-09-22 [S3 경로·스토리 계약 초안](14_MAP_EVENTS_DROPS.md#route-story-s3-contract-draft-v01): 원모델9종·논리명령8종·승리/스토리 결정표·경합/구버전복구·S3-a~d 인수. S1~S3 Plan 초안 준비,실제 담당계약/구현/QA와 구분.

2026-09-22 OC01 최신 재검토: [09 근거/대체안/영향](09_COMMANDER_PROGRESSION.md#oc01-equipment-integration-review-v01). 기존 태그 카탈로그→선택UI 제작 작업은 정책 확정 전 보류. 장비 실장착 정책/확정 지휘실 미술 유지, 대체효과 신규 승인 대기.

2026-09-22 최신 확정: 별도 요원 시너지 폐기·장비 세트옵션 통합. [13 상세](13_EQUIPMENT_ENCHANTMENTS.md#equipment-set-integration-v01). OC01 가상인원/선호UI 선행은 폐기, 대체효과 승인 대기. 기존 장비 실장착 규칙/확정 미술 유지.

### 최신 정정 — 작전 지도 전체 공개 / 2026-09-22
[14 전체공개v02](14_MAP_EVENTS_DROPS.md#operation-map-full-reveal-v02): START전체경로/노드이벤트고정·모든아이콘공개. 이전다음목적지만공개A/먼사건숨김은대체. Direction D-11등록,원데이터/실제구현·미술검증은별도.

## 현재 잔여 작업 진입점 — 2026-09-28
[33 순차 잔여 목록](33_CURRENT_WORK_STATUS.md#next-work-ordered-review-20260928)을 현재 승인/기획공백/구현대기 구분에 사용한다. 아래·위 과거 승인대기 연혁으로 최신 승인 범위를 되돌리지 않는다. 전투/성장/보상 승인 묶음과 실제 실행·통합 완료를 구분한다.

## 2026-09-29 과거 통합기획 복구 입구
[32 대단절·세력·직계제자](32_NARRATIVE_REFERENCE.md#recovered-world-origin-20260929), [33 항목별 판정·남은 충돌](33_CURRENT_WORK_STATUS.md#chat-gap-reconciliation-20260929). 과거 원문 발견으로 승인된 누락만 보완했으며 후보·충돌은 별도 유지한다. 구현/배포/미술 착수 지시가 아니다.

## 업데이트 운영 문서 — 2026-10-01
[게임 업데이트 운영 절차](../../operations/GAME_UPDATE_RUNBOOK.md): 기존02/08/11/12/36/50 정책을 준비·적용·검증·재개·롤백 순서로 연결한 운영 초안. 번호 카테고리 정책 원본을 대체하지 않으며 실제 배포 승인이 아니다.
