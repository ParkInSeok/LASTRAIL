import json, re, shutil
from pathlib import Path
from PIL import Image
ROOT=Path('D:/Project/LASTRAIL/QA/DirectionImpact-20260921/operator-cinematics-v01-review')
data=json.loads((ROOT/'manifest.json').read_text(encoding='utf-8'))
assert len(data['operators'])==14
files=list(ROOT.glob('*/assets/pose-*.png')); gifs=list(ROOT.glob('*/*.gif'))
assert len(files)==42 and len(gifs)==28
for f in gifs:
    im=Image.open(f);duration=0
    for i in range(im.n_frames):im.seek(i);duration+=im.info.get('duration',0);im.convert('RGB').load()
    assert duration==2500 and im.size==(432,768) and im.n_frames>3
page=(ROOT/'index.html').read_text(encoding='utf-8')
if 'SCREEN_SPEC.md' not in page:
    page=page.replace('<main>', '<main><nav><a href="manifest.json">manifest</a> · <a href="REVIEW.md">검토 기록</a> · <a href="SCREEN_SPEC.md">연출 명세</a> · <a href="operator-contact-0.jpg">포즈 비교 1</a> · <a href="operator-contact-1.jpg">포즈 비교 2</a></nav>')
    (ROOT/'index.html').write_text(page,encoding='utf-8')
for link in re.findall(r'src="([^"]+)"',page):assert (ROOT/link).exists(),link
stage=Path(__file__).parent
for name in ['export-operator-motion.py','operator-motion-final-input.json','operator-contact-0.jpg','operator-contact-1.jpg','operator-exclusions.json']:
    shutil.copy2(stage/name,ROOT/name)
(ROOT/'SCREEN_SPEC.md').write_text('''# 요원 등장·궁극기 연출 후보 v01

범위: 레이나 제외 기존14명, 모집 등장14 + 궁극기14. 신규 Rin/Cipher와 비전투 근무요원 제외. 17 등록부, 20~25/27/29 개인 바이블, 44 승인 스킬 방향 적용. 미확인7명 기존 초상 얼굴 + 공통 외형 기준으로 후보 제작, 미술 승인 아님.

모집: 이미 확정된 지급 결과의 characterId → 해당 등장 표현 → 종료/건너뛰기 → 원 모집 결과 화면. 연출은 지급을 소유하지 않으며 재생/반복/스킵으로 재지급하지 않음. 신규 획득 정책·희귀도별 시간 확정 없음.

궁극기: 원 승인 스킬 수락 사건의 시전자 ID → ON 연출 표현 → 종료/스킵 → 기존 전장. OFF는 기존 중앙 요약 표현 계약 재사용. 후보 2.5초는 비교 길이이며 실제 시전시간/게임 hold 정책 변경 없음. 원 피해·회복·보호·대상은 같은 스킬 사건의 소유자만 처리. 연출 FX에서 판정 발생0.

독립 구성: 인물 pose PNG, 배경, 이름, 광·장식, 카메라/opacity 타임라인 입력을 분리. GIF/연락판은 reference-only. actual alpha 존재 확인했으나 일부 배경광/장식과 인물의 반투명 연결 잔여로 최종 독립 인물 납품 완료 아님. 얼굴·눈/입·머리·손/무기·코트 파츠와 가림 복원, ArtMesh/Deformer/물리, model3/moc3 모델 없음. 연속 Live2D 관절 움직임 아님.

View는 원화/타임라인 표시와 종료 의도만 전달, 상위 Controller가 원 흐름의 종료 콜백 바인딩. 재바인딩 중복0, 종료/스킵/해제 단일 전달, 화면 종료 이후 timer/async 갱신 취소 계약. 게임 상태·비용·피해·지급은 도메인 소유. 참고 GIF 자체에 클릭/게임 입력 계약 구현하지 않음.

검수: 42원화 3포즈 연락판 육안 비교, 28GIF 전체 decode/432x768/2500ms 및 참조 파일 존재. 브라우저/모바일 실기·게임 재생 미검증. 긴 비교 페이지는 세로 목록, 432px 이하 한 열로 줄바꿈. 일부 포즈 변화 작음/전환 윤곽 중첩/도구 동일성·프레임 사이 의상 장식 정합은 후속 보정 대상. 아이리스 권총·오픈 블라우스 후보 제외 후 보호프레임/닫힌 재킷으로3종 보정. 세라 알약 쏟는 중간후보 제외 후 밀봉 촉매로 보정.
''',encoding='utf-8')
(ROOT/'REVIEW.md').write_text((ROOT/'REVIEW.md').read_text(encoding='utf-8')+'\n42 PNG / 28 GIF 존재 및 전체 decode·길이2500ms·432x768 PASS. 14명×3포즈 연락판 육안 비교. 아이리스 권총3후보·세라 알약중간1후보는 생성 이력만 보존하고 활성42원화에서 제외. SCREEN_SPEC 및 연락판2개 연결. 최신 승인미술7인/파츠리깅·실기·런타임 검증 잔여.\n',encoding='utf-8')
anchor='operator-cinematics-v01-produced-20261003'
entry=f'''\n\n<a id="{anchor}"></a>
### D-B-FINAL-01 · 전체요원 모집/궁극기 원화 모션 후보 — 2026-10-03
사용자 레이나 완료 후 ‘전체로’ 승인: 기존14명 전체, 신규Rin/Cipher 및 비전투근무요원 제외. QA/DirectionImpact-20260921/operator-cinematics-v01-review/index.html / manifest / SCREEN_SPEC / REVIEW / qa / PROMPTS(요원별) / 연락판2 / 독립posePNG42 / GIF28(각432×768·2500ms). 버전 operator-cinematics-v01-20261003. 기본4명 승인 초상 기반, Vex/Nyx/Iris 최신25/27/29 기준으로 구외형 대체후보, 개인바이블 미확인7명 기존얼굴+17공통기준의 미승인후보. 44 원스킬 방향, 수치·대상·원run 변경0.
builtin imagegen 원화42+아이리스3/세라1보정, 잘못된권총·알약 후보 활성제외/원생성이력 보존. 포즈 연락판42 육안 및 GIF28 전체decode·총길이/크기·파일참조 검증 PASS. 실제Live2D model3/moc3/파츠리깅 없음; 3포즈 평면전환/카메라·장식광 참고. 반투명 배경광/파츠 분리·작은 포즈 변화/의상·도구 정합·연속관절/기기·SDK·실게임 잔여. 일부원화alpha만으로독립리소스 최종납품완료 주장0. 미술최종승인/런타임 구현승격0.
D-B-FINAL-01 전체 전투군/독립자산 및 소비계약 AND 미충족 유지, U-B-VIEW-02 후속등록0. 자기큐 진행기록만추가, 타담당 기존항목 수정/삭제0. Unity/Server·기획원본·배포·실입장·지급 변경0.
'''
targets=[Path('D:/Project/LASTRAIL/docs/handoffs/DIRECTION_2-D-20260913.md'),Path('D:/Project/LASTRAIL/docs/handoffs/INTEGRATION_WAITLIST.md'),Path('D:/Project/LASTRAIL/docs/handoffs/WORK_QUEUE_DIRECTION.md'),ROOT.parent/'REVIEW.md']
(ROOT/'handoff-entry.txt').write_text(entry,encoding='utf-8')
print(json.dumps({'operators':14,'posePNG':len(files),'GIF':len(gifs),'checks':'decode/duration/size/references PASS','recordEntryPrepared':True},ensure_ascii=False))
