"""Authoritative state transitions and frame-request contract; no UI/IO."""
import uuid, copy
from guide_validation import require_report
from wind_plan import request_wind

ROLES=('front','back','left','right','first')
LABELS={'front':'정면','back':'후면','left':'좌측면','right':'우측면','first':'게임시점 첫 프레임'}

def new_project(name):
 if not isinstance(name,str) or not name.strip() or len(name)>100:raise ValueError('이름은 1~100자입니다.')
 p={'id':uuid.uuid4().hex,'name':name.strip(),'refs':{},'identity':'','motion':'','hidden':'','character_approved':False,'revision':0,'frames':[],'requests':[],'loop':True}
 set_schedule(p,[{'duration_ms':120,'phase':('준비' if i==0 else '복귀' if i==11 else '호흡'),'kind':'key' if i in (0,5,11) else 'between'} for i in range(12)])
 return p

def readiness(p):
 missing=[LABELS[r] for r in ROLES if not p['refs'].get(r)]
 missing += [label for key,label in [('identity','캐릭터 제작 기준'),('motion','동작 규칙'),('hidden','숨은면 검토 및 보완 근거')] if not p.get(key,'').strip()]
 return missing

def invalidate(p):
 p['character_approved']=False;p['revision']+=1
 p.pop('review_export',None);p.pop('export',None);p.pop('temporal_review',None)
 for f in p['frames']:f['approved']=False

def replace_reference(p,role,asset):
 if role not in ROLES and role!='hidden_ref':raise ValueError('지원하지 않는 참고 역할')
 p['refs'][role]=asset;invalidate(p)

def approve_character(p):
 missing=readiness(p)
 if missing:raise ValueError('등록 미완료: '+', '.join(missing))
 p['character_approved']=True

def set_schedule(p,rows):
 if not isinstance(rows,list) or not 2<=len(rows)<=48:raise ValueError('프레임은 2~48장입니다.')
 frames=[]
 for row in rows:
  ms=row.get('duration_ms');phase=row.get('phase');kind=row.get('kind')
  if type(ms)!=int or not 20<=ms<=3000 or ms%10:raise ValueError('시간은 20~3000ms, 10ms 단위입니다.')
  if not isinstance(phase,str) or not phase.strip() or len(phase)>200:raise ValueError('동작 단계를 입력하세요.')
  if kind not in ('key','between'):raise ValueError('주요/중간 프레임을 선택하세요.')
  frames.append({'duration_ms':ms,'phase':phase,'kind':kind,'notes':str(row.get('notes',''))[:4000],'versions':[],'approved':False})
 if frames[0]['kind']!='key' or frames[-1]['kind']!='key':raise ValueError('시작과 끝은 주요 프레임이어야 합니다.')
 if 'fx_timeline' in p:p.setdefault('fx_timeline_history',[]).append(p.pop('fx_timeline'))
 p['frames']=frames;p['revision']+=1
 p.pop('review_export',None);p.pop('export',None);p.pop('temporal_review',None)

def frame_at(p,index):
 if type(index)!=int or not 0<=index<len(p['frames']):raise ValueError('프레임 번호 오류')
 return p['frames'][index]

def retime_frames(p,durations):
 if not isinstance(durations,list) or len(durations)!=len(p['frames']):raise ValueError('시간만 변경할 때는 기존 프레임 수를 유지하세요.')
 if any(type(ms)!=int or not 20<=ms<=3000 or ms%10 for ms in durations):raise ValueError('시간은 20~3000ms, 10ms 단위입니다.')
 previous=[f['duration_ms'] for f in p['frames']]
 if previous==durations:return
 if p.get('fx_timeline'):raise ValueError('독립 FX 시간표를 먼저 해제한 뒤 배우 시간을 변경하세요.')
 p.setdefault('timing_history',[]).append({'revision':p['revision'],'durations_ms':previous})
 for f,ms in zip(p['frames'],durations):f['duration_ms']=ms;f['approved']=False
 p['revision']+=1
 for k in ('review_export','export','temporal_review'):p.pop(k,None)

def approve_frame(p,index):
 f=frame_at(p,index)
 require_report(p,index)
 if not p['character_approved']:raise ValueError('캐릭터 준비를 먼저 승인하세요.')
 if not f['versions']:raise ValueError('프레임 결과가 없습니다.')
 if f['versions'][-1]['checks']['blocking']:raise ValueError('차단 검사 항목을 해결하세요.')
 f['approved']=True

def build_request(p,index):
 if not p['character_approved'] or readiness(p):raise ValueError('캐릭터 등록·승인이 필요합니다.')
 f=frame_at(p,index);refs=list(p['refs'].values());neighbors=[]
 if f['kind']=='between':
  for candidates in (range(index-1,-1,-1),range(index+1,len(p['frames']))):
   key=next((j for j in candidates if p['frames'][j]['kind']=='key'),None)
   if key is None or not p['frames'][key]['approved']:raise ValueError('앞뒤 주요 프레임을 먼저 작화·승인하세요.')
   neighbors.append(key);refs.append(p['frames'][key]['versions'][-1]['asset'])
 prompt=f'''Frame-drawn animation, one full character PNG frame, NOT a rig deformation or stretched source.
Character: {p['name']}
Identity and invariants: {p['identity']}
Hidden-surface instructions: {p['hidden']}
Motion contract: {p['motion']}
Frame {index+1}/{len(p['frames'])}; exposure {f['duration_ms']}ms; phase {f['phase']}.
Targeted correction: {f['notes']}
Use approved game-view first frame as visual authority. Front/back/left/right are construction references, not collage panels.
Adjacent approved key frame indices: {[n+1 for n in neighbors]}. Connect these poses while preserving identity, camera, hand ownership, tool grip, grounded feet, palette, material and lighting.
Render a genuinely transparent PNG on the SAME canvas and registration as the game-view first frame. No added background, baked projectile FX, duplicate edges, extra limbs, whole-body stretching, automatic mirroring or arbitrary scale changes.
Reconstruct only needed pose/occlusion changes; keep untouched regions faithful. Do not chain only from the previous generated frame. This is a candidate requiring temporal art review.'''
 guide,note=request_wind(p,index)
 result={'id':uuid.uuid4().hex,'project_id':p['id'],'revision':p['revision'],'frame_index':index,'reference_assets':list(dict.fromkeys(refs)),'reference_roles':copy.deepcopy(p['refs']),'neighbor_indices':neighbors,'prompt':prompt+(note or ''),'status':'waiting_for_generator','method':'frame_redraw','result_asset':None}
 if guide is not None:result['wind_guide']=guide
 return result
