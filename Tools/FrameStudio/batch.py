"""Sequential, resumable generation; approvals remain owned by the domain."""
import copy,threading,uuid
from domain import build_request,readiness
from refinement import refinement_request
from wind_plan import request_wind

def plan_frames(p,draft):
 if draft:
  if not p['refs'].get('first'):raise ValueError('첫 원화를 등록하세요.')
  return [i for i,f in enumerate(p['frames']) if not f['versions'] or f.get('needs_regeneration')]
 if not p['character_approved'] or readiness(p):raise ValueError('캐릭터 자료 등록·승인이 필요합니다.')
 keys=[i for i,f in enumerate(p['frames']) if f['kind']=='key']
 missing=[i for i in keys if not p['frames'][i]['versions']]
 if missing:return missing
 if any(not p['frames'][i]['approved'] for i in keys):return []
 return [i for i,f in enumerate(p['frames']) if not f['versions']]

def draft_request(p,index,seed):
 f=p['frames'][index]
 prompt=f'''Detailed anime game character, preserve reference identity, costume, hands and equipment. {p['identity']}
Motion: {p['motion']}. Timeline phase: {f['phase']}. Frame direction: {f['notes']}
Fixed camera, consistent anatomy, lighting and crisp linework. No stretching, no extra limbs.'''
 roles={'first':p['refs']['first']}
 if f.get('guide'):roles['pose']=f['guide']
 guide,note=request_wind(p,index)
 result={'id':uuid.uuid4().hex,'revision':p['revision'],'frame_index':index,'reference_assets':list(dict.fromkeys(roles.values())),'reference_roles':roles,'draft':True,'seed':seed,'prompt':prompt+(note or ''),'status':'running'}
 if guide is not None:result['wind_guide']=guide
 return result

class BatchController:
 def __init__(self,studio):self.studio=studio;self.cancelled=threading.Event()
 def cancel(self):self.cancelled.set()
 def start(self,p,draft,refine=False):
  s=self.studio
  if s.active:raise ValueError('생성 종료 후 다시 시작하세요.')
  if draft and s.provider.info()['selected']!='local':raise ValueError('시험 일괄 생성은 로컬 전용입니다.')
  old=p.get('batch',{})
  if refine:
   if s.provider.info()['selected']!='local':raise ValueError('국소 수정은 로컬 전용입니다.')
   indices=list(range(len(p['frames'])))
   for i in indices:refinement_request(p,i,0)
   if old.get('refinement') and old.get('state') in ('failed','cancelled','interrupted') and old.get('resume_revision')==p['revision']:indices=list(old['pending'])
  else:indices=plan_frames(p,draft)
  if not indices:raise ValueError('생성할 빈 프레임이 없습니다. 주요 포즈 승인 또는 문제 프레임 재생성을 진행하세요.')
  engine=s.provider.reserve();self.cancelled.clear()
  seed=old.get('seed',int(uuid.uuid4().hex[:8],16))
  if old:p.setdefault('batch_history',[]).append(copy.deepcopy(old))
  p['batch']={'id':uuid.uuid4().hex,'state':'running','draft':draft,'refinement':refine,'resume_revision':p['revision'],'engine':engine,'seed':seed,'pending':indices,'done':[],'error':None}
  s.active={'project':p['id'],'frame':indices[0],'engine':engine,'batch':True}
  try:
   s.repo.save(p);threading.Thread(target=self.run,args=(p['id'],),daemon=True).start()
  except Exception:s.active=None;s.provider.release();raise
 def run(self,pid):
  s=self.studio
  try:
   while True:
    with s.repo.lock:
     p=s.repo.get(pid);b=p['batch']
     if self.cancelled.is_set():b['state']='cancelled';s.repo.save(p);return
     if not b['pending']:break
     i=b['pending'][0];req=refinement_request(p,i,b['seed']) if b.get('refinement') else draft_request(p,i,b['seed']) if b['draft'] else build_request(p,i)
     req.update(engine=b['engine'],status='running');p['requests'].append(req);s.active['frame']=i;s.repo.save(p)
    s.execute_request(pid,req)
    with s.repo.lock:
     p=s.repo.get(pid);b=p['batch'];b['pending'].remove(i);b['done'].append(i);b['resume_revision']=p['revision'];s.repo.save(p)
   with s.repo.lock:
    p=s.repo.get(pid);b=p['batch']
    if all(f['versions'] for f in p['frames']):
     s.review_export(p);b['state']='needs_review'
    else:b['state']='awaiting_key_approval'
    s.repo.save(p)
  except Exception as e:
   with s.repo.lock:
    p=s.repo.get(pid);p['batch'].update(state='cancelled' if self.cancelled.is_set() else 'failed',error=str(e));s.repo.save(p)
  finally:
   with s.repo.lock:s.active=None;s.provider.release()
