"""Register approved source as reference only; never pretend missing views exist."""
from pathlib import Path
from domain import new_project,replace_reference
from repository import Repository
root=Path(__file__).resolve().parent
repo=Repository(root/'data')
name='이브 · 2번 기준 자료 준비'
if not any(p['name']==name for p in repo.list()):
 p=new_project(name)
 source=root.parents[1]/'QA/DirectionImpact-20260921/eve-combat-v01/eve-idle-breath-v13-01.png'
 replace_reference(p,'first',repo.put_asset(source.read_bytes()))
 p['identity']='이브: 갈색 짧은 보브, 청동 고글, 청회색 짧은 작업 재킷, 밝은 이너, 회색 작업 바지, 작업 부츠, 소형 등팩과 허리 케이스. 오른손 공구, 왼손 하우징 지지. 승인 2번 원화 기준.'
 p['motion']='2번 승인 동작 분석을 기준으로 준비→공구 올림→충전→단발 방출→짧은 반동→회수. 각 동작 시간표와 주요 포즈를 별도 확정한다. 발 접지, 관절 부피, 손과 장비 연결 유지. 전신 늘림으로 대체하지 않는다.'
 p['hidden']='미검토: 정면·후면·좌우측면을 준비하고 공구 반대면/팔 안쪽/등팩 가림을 확인해야 한다.'
 p['source_note']=str(source)+'; 기존 승인 원화 복사, 신규 생성 아님. 4방향 자료 미등록.'
 repo.save(p)
 print(p['id'])
