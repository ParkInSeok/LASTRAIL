# LASTRAIL 복구 안내

2026-11-26 중간 검증 기록. 이 목록은 전체 로컬 프로젝트 삭제를 승인하는 증거로 사용할 수 없다.

## 소스와 이력

- 원격 복구 브랜치는 `codex/project-backup-20261126`이다. 이 브랜치의 `docs/handoffs/RECOVERY-*.json`에는 작업 사본, 모델·도구, 내부 정션과 체크포인트의 복구 정보가 있다.
- 원래 Git 참조는 `recovery-20261126/root/` 접두사에, 외부 작업 사본 7개의 보존 커밋은 `recovery-20261126/worktree/<id>`에 있다. 원본에서 미추적이었던 파일을 구분하는 방법은 [작업 사본 목록](RECOVERY-WORKTREES-20261126.json)에 있다.
- [현행 작업 소스](https://github.com/ParkInSeok/LASTRAIL/releases/tag/active-project-source-20261126)는 ZIP 안의 원래 경로와 SHA-256 목록을 사용한다. [소스 보충본](https://github.com/ParkInSeok/LASTRAIL/releases/tag/working-source-supplement-20261126)의 `working-source-index.json`은 경로별로 기존 소스 ZIP이나 보충 묶음의 객체를 가리킨다.
- [구성 저장소 7개의 Git 이력](https://github.com/ParkInSeok/LASTRAIL/releases/tag/component-history-20261126)은 첨부된 복구 스크립트와 색인을 사용한다. 분리한 보안 데이터가 필요한 객체는 E 백업을 함께 사용한다. 새 저장소에서 원래 참조가 가리키는 객체를 검증했다.
- [Legacy 작업 사본](https://github.com/ParkInSeok/LASTRAIL/releases/tag/legacy-working-sources-20261126)에는 구 폴더 정리 시 보존한 767개 파일이 있다. Legacy 전체 Git 객체의 별도 복구 검증은 진행 중이다.

## 제작 자료

- [편집 원본](https://github.com/ParkInSeok/LASTRAIL/releases/tag/editable-art-20261126)은 PSD·PSB·CMO3 원본 경로를 색인에서 확인한다. [레이나 Cubism 별도 원본](https://github.com/ParkInSeok/LASTRAIL/releases/tag/reina-cubism-sources-20261126)도 있다.
- [제작·검수 자료](https://github.com/ParkInSeok/LASTRAIL/releases/tag/project-art-and-evidence-20261126)는 58,055개 경로를 `art-recovery-index.json`에 기록했다. 경로와 각 객체의 SHA-256은 [복구 도구](restore-project-art-20261126.py)로 확인할 수 있다. Python과 GitHub CLI가 설치된 환경에서 `python restore-project-art-20261126.py qa <빈_폴더>`로 복구한다.
- [프로젝트 제작 오디오](https://github.com/ParkInSeok/LASTRAIL/releases/tag/authored-audio-20261126)는 1,002개 경로를 같은 형식의 색인에 기록했다. [외부 분석용 영상](RECOVERY-EXTERNAL-AUDIO-REFERENCES-20261126.json)은 사용자 결정에 따라 출처와 해시, 자체 분석만 보존한다.
- [옛 프로젝트 생성 원화](https://github.com/ParkInSeok/LASTRAIL/releases/tag/historical-generated-art-20261126)는 234개의 파일 경로를 복구한다. 공개 목록은 개인 대화 식별자를 해시로 표기한다. 원래 폴더명은 E의 대화 백업 색인과 함께 복구할 수 있다.

## 공개 저장소 외 복구

보안 자료와 개인 로그는 사용자가 지정한 E 로컬 백업의 경로별 목록을 사용한다. GitHub 단독 복제만으로는 이 자료가 복구되지 않는다. 신규 프로젝트 위치에 원래 디렉터리 연결을 만들 때는 [내부 정션 30개 목록](RECOVERY-INTERNAL-LINKS-20261126.json)을 사용한다.

## 추가 검증

- [Unity 작업 원본](https://github.com/ParkInSeok/LASTRAIL/releases/tag/unity-source-supplement-20261126)의 색인은 Assets·Packages·ProjectSettings의 실제 파일 13,821개를 원본 해시와 대응한다. [공식 2D Animation 13.0.0 복구 기록](RECOVERY-UNITY-ANIMATION-PACKAGE-20261126.json)에 공식 원본과 일치하는 파일 1,979개와 프로젝트 변경 4개가 구분돼 있다. URL 인증정보 형식의 테스트 문자열은 E 조각과 함께 복원한다.
- [Unity 화면 검수 자료](https://github.com/ParkInSeok/LASTRAIL/releases/tag/unity-visual-evidence-20261126)는 48,754개 경로와 이미지 묶음 158개를 확인했다. 복구 도구의 `unity-visuals` 옵션으로 복원한다. 같은 도구의 `audio` 옵션으로 제작 오디오를 복원한다. 둘 다 공개 최종 색인으로 샘플 파일을 복원해 로컬 원본 바이트와 대조했다.
- [전체 저장소의 커밋·태그·트리 구조](https://github.com/ParkInSeok/LASTRAIL/releases/tag/historical-git-structures-20261126)와 파일 본문은 별도 자료다. 원본 Git 저장소의 전체 무결성 검사는 통과했다. [구 Legacy의 객체](https://github.com/ParkInSeok/LASTRAIL/releases/tag/legacy-source-history-20261126), [보충 소스 기록](https://github.com/ParkInSeok/LASTRAIL/releases/tag/legacy-history-supplement-20261126), [공식 배포본에서 재현된 객체 131개](RECOVERY-LEGACY-VENDOR-OBJECTS-20261126.json)를 함께 사용한다. 이 시점에 공개 보존을 보류한 구 DLL·PDB 객체가 6개 있다.
- [제작 음성 및 분석 보충본](https://github.com/ParkInSeok/LASTRAIL/releases/tag/project-audio-reference-records-20261126)에는 자체 제작 자료 90개 경로 및 외부 참고자료의 출처·해시가 있다.
- 프로젝트 원본 폴더의 Git 저장소에 연결돼 있던 외부 작업 사본 7개는 독립 Git 디렉터리로 전환했고, 미추적 파일 108개의 원격 Git 객체를 대조했다. 개인 대화 로그 609개와 인증 데이터는 E 백업에 보존했다.

D 삭제 조건은 충족하지 않았다. 전체 파일 대조와 FrameStudio 최종 색인, 고립 객체의 소유·보안 판별이 남아 있다. Unity 빌드 58개 디렉터리의 패키징 데이터 418개는 고유 파일 167개, 17,017,958,040바이트다. 해당 빌드 바이너리는 공개 전송을 진행하지 않았다.


