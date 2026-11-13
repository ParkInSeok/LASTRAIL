<!-- CURRENT-REFERENCE-20261029 -->
> **2026-10-29 현행 참조 우선순위.** 아래 누적 기록은 작성 당시 이력이며, 옛 시안의 존재·파일명·도구 승인 필드만으로 현행 승인본을 판단하지 않는다. 최신 승인 정책과 실제 제작 REVIEW/QA를 함께 확인한다. 가로 화면, 애니메이션 2번/반대 방향, 카일 순수 맨손 무공 및 노드 시작 재입장이 과거 상충 지침보다 우선한다. 최신 제안은 사용자 승인 전까지 적용하지 않는다. 삭제 목록은 `D:/Project/LASTRAIL/QA/ResourceCleanup-20261028/deleted.jsonl`; 삭제된 원화를 임의 복원하거나 다른 버전으로 조용히 대체하지 않는다. 이번 정리는 링크·정책 진입점 대조이며 자산/런타임 전체 검수 완료가 아니다.
현행 정책의 바로가기는 [카테고리 색인](superpowers/plans_categories/00_INDEX.md) 상단, 문서 검사 범위와 남은 미확인은 [대조 기록](superpowers/plans_categories/38_DOCUMENT_RECONCILIATION.md#resource-reference-reconciliation-20261029)을 따른다.

# LASTRAIL 문서는 여기서 시작

현행 기획의 편집 위치는 **[카테고리 색인](superpowers/plans_categories/00_INDEX.md)** 이다. 새 날짜별 “최종본”을 만들지 않고 담당 원문을 갱신한다.

| 찾는 것 | 위치 | 사용 방법 |
|---|---|---|
| 현행 기획·판단 상태 | [00_INDEX](superpowers/plans_categories/00_INDEX.md) | 최신 사용자 결정 우선. 기획과 구현 상태 구분 |
| 누적 내용 충돌·최종 보충·보존 기준 | [대조 결과](superpowers/plans_categories/38_DOCUMENT_RECONCILIATION.md) | 상세 원문386개 정책 대조. 미결정 충돌 명시, 나머지 MD 일괄 삭제 금지 |
| 번호 밖의 상세 설계/API/기존 구현 계획 | [상세 원문 등록부](superpowers/plans_categories/39_SOURCE_REGISTER.md) | 생략된 계약 보존. 구형 정책은 최신 결정으로 대조 |
| 추가 전달6개 반영 | [추가 자료 체크 목록](../QA/DocumentIntegration-20260921/md-cleanup/assets/archive.html#doc-71a6b3d577d494ee1689) | 전문 확보·병합 완료. 원본 해시와 후속 검증 구분 |
| 전체 MD·동일 사본 위치 | [전수 등록부](../QA/DocumentIntegration-20260921/md-cleanup/assets/archive.html#doc-76a704e29c7ce935f063) | 백업/QA/외부 의존성과 현행 정책을 구분 |
| 실제 점검 결과·남은 한계 | [정리 보고서](../QA/DocumentIntegration-20260921/md-cleanup/assets/archive.html#doc-ba36689cc32a3c0b60d0) | 목록 검사와 전문/구현 검증 범위 구분 |
| 현재 담당 작업·실제 실행 경로 | [D 인계](handoffs/README-D.md) | 이전 C/E 경로보다 현재 루트 AGENTS 지침 우선 |

정책의 전문은 담당 문서 한 곳에서 관리한다. 과거 원문·검증 기록은 기존 위치에서 보존하며, 날짜·파일명·해시 동일성만으로 승인/구현 완료를 추정하지 않는다. 번호00~29 전문은 모두 확보했다. 후속 전달6개를 반영하고 카드별 리롤 정정을 보존했다. 코드·DB·Unity 자산·운영 상태는 이번 정리로 변경하지 않는다.


## 2026-09-21 의미 기준 문서 정리

남은 2,167개에서 종료·대체·통합된 Markdown 1,534개를 추가 제거하고 통합 참조 3개를 설치했다. 원문과 역사 스냅샷의 동반 meta 232개는 해시 검증한 ZIP에 보존했다. 실제 Unity Assets의 meta와 코드·이미지·DB는 이 정리에서 삭제하지 않았다. 나머지 문서는 모두 최종 기획이라는 뜻이 아니며 기술 계약·리소스 출처·외부 도구·인계 자료를 포함한다. 서로 다른 상세 계약 90개는 아직 별도 파일이 필요해 유지했다. 정리 완료를 구현·승인·전체 API 통합 완료로 해석하지 않는다.

현재 기준: [최종 기획 색인](superpowers/plans_categories/00_INDEX.md).


## 2026-09-21 번호 통합 완료

최종 기획 편집 위치는 `docs/superpowers/plans_categories`이며 00~52번 MD 53개와 작성용 JSON 4개로 구성한다. 기존 보완 문서 13개에 번호를 부여하고 분산 상세 계약 225개를 주제별 10개 문서로 통합해, 원래 위치의 MD 238개를 삭제했다. 상세 본문·필드·표·후보/과거 상태는 보존하며 문서 통합이 신규 승인이나 런타임 구현 완료를 뜻하지 않는다. 원문은 numbered/originals.zip과 source-map.json으로 복원할 수 있다. 이전 636개 집계는 이번 정리 전 기록이다.
