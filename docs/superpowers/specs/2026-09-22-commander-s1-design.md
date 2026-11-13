# S1 v02 서버 저장·출발 계약 설계

상태: 요청33의 구현 지시에 따라 구체화한 구조 설계 검토본. 게임 정책은 이미 승인됐으며 재승인 대상이 아니다. 제품 코드/원격 배포 완료 문서가 아니다.

## 목표와 범위

기존5요원 편성과 별도로 지휘 특성3프리셋의 규칙검증·저장·활성·원결과 조회, 실제 정의지원, 계정XP의 레벨/P 파생, 출발 시점 snapshot 결속을 구현한다. 폐기된 요원 시너지/선호 태그는 신규 DTO·필수검사·출격오류에 존재하지 않는다. 실제 구투자가 식별되는 경우만 원자적·멱등 이행을 제공한다. 진행run은 원snapshot을 보존한다.

원본은09의 oc01-reconciliation-final-v01/commander-s1-contract-draft-v02와13 equipment-set-integration-v01, 최신 commander_tree_expansion_draft.json이다. JSON의 proposed 필드는 policyApproval에 따라 승인된 v1.1 설계수치로 해석한다. authoringFormat/publishable:false를 실행지원 버전으로 오인하지 않는다. 포인트·효과 수치의 운영 발행은 별도다.

선택: 별도 지휘특성 저장소와 기존 BalanceVersion/START 승인 경계 조합. 기존 commander_presets 의미 변경은5요원 편성FK/영수증을 손상시킨다. account JSON 하나에 모든 원본을 추가하는 방법은3슬롯/활성선택/원영수증 경합을 분리하기 어려우므로 채택하지 않는다.

실제 개발 D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server, branch codex/dev-stable-registry-20260907. 점검 배포 후보 d01a2ba5와 원격DEV는 변경하지 않는다. 후보 migration0049는 구현 시작 시 번호 충돌을 재확인한다. 다른 담당 소유 파일은 편집하지 않는다.

## 책임과 원본

- CommanderRules:43노드/106랭크/레벨표/부모OR/상위한도/비용을 검증하는 순수 도메인. 정의 그래프와 랭크표 일치·cycle/누락부모/중복ID/안전정수도 검증한다. 상태와 DB를 소유하지 않는다.
- CommanderDefinitionReader: 현재 고정 BalanceVersion의 commanderTraits 정의를 strict 검증하고 실제 지원 능력과 대조한다. 외부 HTTP 요청이 지원 목록을 제출할 수 없다.
- CommanderPresetStore: 계정별3슬롯·활성선택·원영수증·revision의 권위. 도메인에 검사 위임, 같은batch로CAS/쓰기/계정mutation/영수증을 확정한다.
- CommanderDepartureBinding: 현재 selection/preset/progression/정의와 정확 원본을 읽고 quote/snapshot 및 같은batch guard를 공급한다. 임의 SQL 입력을 클라이언트에서 받지 않는다.
- CommanderLegacyReconciliation: 확인된 구정의/저장 포맷만 정규화하는 내부 경계. 일반 클라이언트에 환불량·레거시 랭크 입력을 받지 않는다.
- routes는 기존 게임 인증과 bounded body/error 계약을 조합한다. UI/기타담당은 입력 의도와 결과 표시만 소유한다.

## 정의와 실제 지원

BalanceVersion의 새 선택 구역 commanderTraits에 rulesVersion, traitDefinitionVersion, sourceHash, nodes/rankRows/levels와 효과별 handlerVersion 요구를 담는다. 불변 BalanceVersion의 기존 게시·해시 계약을 재사용한다. 서버 실행코드가 제공하는 지원능력과 정확 handler version을 대조하며 문구가 존재한다는 이유로SUPPORTED를 반환하지 않는다. 작성JSON을 읽어 자동 게시하거나 임의 실제 버전ID를 발급하지 않는다.

로컬 검증용 명시TEST_ONLY 정의와 능력은 fixture에서만 사용한다. 제품에서 알려지지 않은 handler/정의는 commander_definition_unsupported다. 43노드 전체 랭크/예산 검증은 구현하되, 실제 소비자가 연결되지 않은 효과를0효과로 실행하지 않는다. sourceHash는 원본 추적이며 실행 버전의 대체가 아니다.

구 BalanceVersion에 commanderTraits 구역이 없고 계정에 S1 저장/활성 이력이 없으면 기존 지휘특성 미적용 출발 의미를 유지한다. 이것은 임의0효과 새OC01 발행이 아니다. 계정에 S1 설정이 있는데 구역이 없어지거나 지원정의가 소실되면 명시 미지원 오류로 신규저장/활성/START를 거절한다. 기존 원영수증과run 읽기는 현재 정의 지원 여부와 분리한다. 신규카탈로그 조회는 정의가 없음을 명시하며 정상 빈카탈로그로 위장하지 않는다.

OC01 피해handler 자체와 나머지 전투/장비 효과의 실기 소비는 해당 소유자 의존성이다. 서버는 runtimeBindings/지원표를 구현하지만 실제 능력이 없으면 지원되지 않는 것으로 노출한다. S1 데이터 구현 완료와43효과 실행 완료는 별도다.

## XP·레벨·포인트

player_profiles.commander_xp를 유일한 기존XP 원본으로 유지한다. 새 XP 복제값/고객제출 레벨/지급량을 만들지 않는다. 승인 levels 표로 Lv1 최초1P, 최대Lv50/50P를 파생하며 초과XP를 잘라 저장하지 않는다. 이번 S1 작업에서 XP 지급 기능을 임의 추가하지 않는다.

player_profiles에 commander_progression_revision(초기1)을 추가하고 commander_xp 실제 변경 시 증가시키는 DB trigger로 모든 기존XP writer를 포괄한다. 이름변경 같은 비관련 쓰기는 증가시키지 않는다. overflow/음수/정밀도 위반을 거절한다. Save/Activate/START의 guard는 XP와 revision 양쪽을 검사한다. 레벨/P는 선택된 rulesVersion과 함께 파생한다. 정의 변경도 같은XP의 의미를 바꿀 수 있으므로 정의 pin 검사를 생략하지 않는다.

포인트는 각슬롯에서 동일 earnedPoints를 독립 예산으로 사용한다.3슬롯 사용P 합산 차감 없음. 미사용P=earnedPoints-해당슬롯투자비용이며 별도 가변 잔액을 만들지 않는다. OC01 제거에 따른3P환불은 같은슬롯의 파생 미사용P가3늘어나는 것으로 표현하고 계정 earnedPoints/XP에는 가산하지 않는다.

## 저장 스키마

- commander_trait_presets: (account_id,slot_id1..3)PK, revision>=1, rules_version,trait_definition_version,balance_version,canonical ranks_json,updated_at. 선언되지 않은 선호태그 컬럼은 없다. 참조정의는 실제로 복원 가능한 불변 원본으로 남긴다.
- commander_trait_selections: account_id PK, slot_id,revision>=1,updated_at, 계정/slot 복합FK. 포인터는 슬롯을 가리키며 저장revision을 영구고정하지 않는다.
- commander_trait_receipts: (account_id,idempotency_key)PK,command_hash,mutation_id FK,response_json,created_at. update/delete 금지. command 종류도 hash에 포함한다.
- commander_trait_migrations: 실제 이행을 실행했을 때만 (account_id,slot_id,source_definition_version,target_definition_version) 식별자와 before/after revision·환불P·원영수증 참조 기록. 실제 레거시 adapter가 없으면 행을 만들지 않는다.
- 같은batch guard는 실패 시 전체rollback, 성공 시 잔행0이다. commander_presets/account_formation/formation_receipts 의미와 행은 보존한다.

읽기만으로3행이나 활성슬롯을 쓰지 않는다. 미저장슬롯은 revision0/ranks{}인 표시 데이터, 미선택은 slot:null/revision0으로 표현한다. 최초 저장은 해당슬롯만 생성하고 자동활성화하지 않는다. 정의가 제공된S1 환경에서 미선택은 빈랭크이며 미선택 때문에 기존출발을 강제로막지 않는다. 실제 미선택/미저장 상태도 quote에고정한다.

## HTTP 계약

기존 game-api 인증 후 계정ID를 정한다. body의 accountId 허용0. 응답 Cache-Control:no-store. 다음 URL은 이번 설계에서 정한 신규 계약이며 아직 구현된 API가 아니다.

| 명령 | URL/본문 | 결과 |
| --- | --- | --- |
| 조회 | GET /v1/commander/traits | contractVersion:commander-s1-v2,progression,rules,slots3개,activeSelection,currentRunApplied |
| 저장·무료초기화 | POST /v1/commander/traits/presets/:slot/save; expectedPresetRevision,expectedProgressionRevision,rulesVersion,traitDefinitionVersion,ranks | 저장 영수증; ranks{}는무료초기화 |
| 활성 | POST /v1/commander/traits/presets/:slot/activate; expectedPresetRevision,expectedProgressionRevision,expectedSelectionRevision,rulesVersion,traitDefinitionVersion | 다음작전활성 영수증 |
| 원결과 조회 | GET /v1/commander/traits/commands/:key | 해당계정의확정원결과 또는404 |

모든쓰기 Idempotency-Key printable ASCII1..128. 각rank는0..노드최대의 안전정수,알수없는node/중복/불필요필드/선호태그거절.0랭크는canonical ranks에서제거하고키순서정렬. 다른표현의동일정규ranks는동일명령으로취급한다. expectedRevision과정의버전도hash에포함한다.

순서: 인증/문법→계정원키hash/확정영수증→현재정의지원/revision/도메인검증→원자저장. 같은키다른명령은409. 저장성공후정의/XP/현재슬롯이바뀌어도같은요청은원결과를반환하며재적용하지않는다. catch에서도다른동일요청의commit을먼저재조회한다. GET 결과는현재상태표시가아니며정확원영수증이다.

Save는새키의정규값이완전히같으면preset revision 유지,명령영수증은남긴다. 내용/정의변경이면+1. Activate는같은slot이면selection revision 유지,다른slot이면+1. 둘다expected revisions검사후no-op판정한다. 진행run존재를S1저장/활성차단으로복사하지않는다. A활성/B저장은A유지,A저장은다음START에최신A,진행run불변.

노드부모는직접부모중하나의실제랭크>0인OR. 삭제후전체최종ranks를검증하므로불법자식보존을거절하고클라이언트가제시한합법최종집합만저장한다. 무료환불거래가구정의자식연쇄규칙을요구하면별도reconciliation에서그원정의를사용한다. capstone한도는Lv25~39 1개/Lv40~50 2개,1랭크3P,총43노드/106랭크정의와일치시킨다.

## 출발과 진행run

기존 admission버전을S1을포함하는새버전으로올리고기존미확정v2quote는명시재준비를요구한다. 이미확정한START는version검사앞의원run재생을유지한다. legacy HTTP START, retained departure core/local composition의실제신규저장경계를모두목록화하여누락없이연결한다. 기존전투readiness/광고OFF/점검gate는해제하지않는다.

quote와새run snapshot의 commanderTraits는적용정의/BalanceVersion/hash,level/earnedPoints/progressionRevision,slot/selectionRevision/presetRevision,canonical ranks 및복원에필요한효과버전을담는다. 새선호태그는없다. 정의구역미존재의기존호환상태는명시null로기록한다.

최종STARTbatch에서정확profileXP/revision,선택행 또는부재,해당preset행 또는부재,정의원본과현재지원능력을재검사한다. 비용차감/preview소비/run/snapshot/원영수증과같은transaction이다. 사전조회만으로경합을해결하지않는다. quote후슬롯/랭크/XP/정의/지원이바뀌면전부미적용하고재준비한다.

currentRunApplied는현재run의원snapshot을읽으며,구run에S1정보가없으면LEGACY_UNRECORDED를명시한다. 조회장애는UNAVAILABLE로구분해NONE과혼동하지않는다. 새계정설정으로복원하지않는다. 원snapshot에필요한역사정의가없으면미지원복구오류이며새OC01을소급적용하지않는다.

## OC01 레거시 이행

현재확인된실제저장은5요원편성뿐이므로자동스캔환불은없다. 내부reconciliation은확인된legacy adapter가권위있는저장소/실제정의/투자내역을반환할때만실행한다. 웹요청이나임의JSON에OC01이라는문자열이있다는이유로환불하지않는다. 알수없는포맷/정의는명시오류로격리하며추측하지않는다.

실제OC01rank1이면rank0,선호필드제거,파생unusedP+3,같은presetrevision+1/이행영수증을batch로확정한다. 투자0/태그만존재는0P. 나머지랭크·활성선택·accountXP/earnedP는보존한다. 구적용정의의하위가0인지확인하고존재시해당정의의OR/연쇄규칙을사용한다. 새효과자동재투자0. 진행run/snapshot update0. 반복실행은이행원영수증반환,새Save와경합하면CAS충돌로전부미적용한다.

실제구데이터/adapter부재는이행미필요상태로노출하며실운영환불검증완료로보고하지않는다. 합성이행fixture는TEST_ONLY로구분한다.

## 오류·검증·수용 경계

400 contract_violation/idempotency_key_required;409 idempotency_conflict/commander_preset_revision_conflict/commander_selection_revision_conflict/commander_progression_revision_conflict/commander_points_exceeded/commander_parent_required/commander_rank_locked/commander_capstone_limit;503 commander_definition_unavailable/commander_definition_unsupported/commander_state_unavailable. 기존계정인증401/403은유지. 내부DB/개인정보노출0. 태그미선택·편성태그없음오류는없다.

필수검증:43노드/106랭크정합,XP0/레벨경계/초과보존,3슬롯독립예산/OR/상위한도/무료초기화,미지원handler거절,태그없는합법저장,계정분리,같은키재생/다른본문충돌,동시같은revision,저장vs활성vsSTART,commit직전XP/정의변경,응답유실,일반편성불변,구START재생,진행snapshot불변,실제지원정의누락을정상빈값으로위장하지않음.

OC01수용시나리오는A1B1C1/A2B1/A3/빈슬롯·타장착자0합산·방어후100→90→보호막30→HP60·제외피해·장비확정후다음피해/기존타격보존이다. 이는S1store검사와별도의실제효과소유자수용목록이며purefixture만으로실기완료처리하지않는다.

검증로그/실제DTO/정의지원표/마이그레이션SHA/변경전패치는기존QA/CommanderS1-20260922에보존하고Server인계/WAIT-011만갱신한다. 다른소유자UI/미술/장비원본은변경하지않는다. 로컬구현이끝나도실제정의게시·원격DB이관·운영배포·게임실연동은별도단계다.

## 자체 검토

포인트이중소유금지(기존XP+파생예산),5요원테이블보존,구영수증재생우선,현재정의와과거snapshot분리,새시너지요구0,미지원효과0으로위장금지,현재빈계정의기존출발호환,미확인구투자자동환불0을대조했다. 구현계획은이설계검토후작업단위별테스트/파일/검증순서를확정한다.

## 승인 이력
2026-09-22 요청34의 사용자 ㄱㄱ로 위 작성 설계 승인. 구현 계획 docs/superpowers/plans/2026-09-22-commander-s1.md 작성 후 검토·실행방식 확인 단계로 진행한다.
