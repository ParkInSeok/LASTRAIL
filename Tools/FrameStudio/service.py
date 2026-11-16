"""Use cases coordinate domain, storage, image QA and provider lifetime."""
import base64, io, json, uuid, threading
from PIL import Image
from domain import *
from guide_validation import GuideValidation
import production_method
import approval_gate
from imaging import inspect_png, compare_images, export_clip
from batch import BatchController,draft_request
from refinement import refinement_request,validate_mask
from edge_cleanup import clean_red_fringe,smooth_alpha_edge
from detail_finish import finish_detail
from reference_patch import prepare_reference_patch
from surface_shading import prepare_surface_shading
from temporal_workflow import TemporalReview
from delivery_review import BattleReview,signature,battle_current,clip_approved,approve_clip
from clip_workflow import ClipImport,set_clip_identity
from battle_scene import Projectile
from effect_design import EffectDesigner
from companion import configure_companion
from fx_timeline import FxTimeline
from wind_plan import WindPlan,configure_wind,DEFAULTS
from production_flow import ProductionFlow, status as production_flow_status

class Studio:
 def __init__(self,repo,provider):
  self.repo=repo;self.guides=GuideValidation(repo);self.provider=provider;self.active=None;self.batch=BatchController(self);self.battle=BattleReview(repo)
  self.clips=ClipImport(repo,self.add_frame);self.projectile=Projectile(repo)
  self.effects=EffectDesigner(repo,self.projectile)
  self.temporal=TemporalReview(repo)
  self.production_flow=ProductionFlow(repo,self.add_frame)
  for entry in repo.list():
   p=repo.get(entry['id'])
   if p.get('batch',{}).get('state')=='running':
    p['batch'].update(state='interrupted',error='서버가 재시작됐습니다. 빈 프레임부터 이어서 생성하세요.')
    for req in p['requests']:
     if req.get('status')=='running':req.update(status='failed',error='서버 재시작으로 결과 수신 중단')
    repo.save(p)
 def view(self,p):
  v=copy.deepcopy(p)
  v['production_flow_status']=production_flow_status(p)
  for i,f in enumerate(v['frames']):
   gate=self.guides.summary(p,i)
   if gate is not None:
    f['result_guide_gate']=gate
    if not gate['can_adopt']:f['approved']=False
  v['missing']=readiness(p);v['active']=self.active
  v['production_method']=production_method.selected(p);v['production_methods']=copy.deepcopy(production_method.METHODS)
  v['clip_approved']=clip_approved(p);v['battle_current']=battle_current(p)
  if v['clip_approved'] and p.get('result_guides'):
   try:self.guides.require_all(p)
   except (ValueError,OSError):v['clip_approved']=False
  if v.get('temporal_output'):v['temporal_output']['stale']=v['temporal_output']['signature']!=signature(p)
  v['wind_defaults']=copy.deepcopy(DEFAULTS);v['wind_defaults']['enabled']=bool(p.get('refinement_wind',False))
  if p.get('wind_settings'):v['wind_preview']=WindPlan(p['wind_settings']).preview(p)
  if v.get('export',{}).get('signature') and v['export']['signature']!=signature(p):v['export']['status']='stale'
  return v
 def wind_preview(self,pid,data):
  with self.repo.lock:return WindPlan(data.get('settings')).preview(self.repo.get(pid))
 def list_projects(self):
  return [{**entry,'clip_identity':p.get('clip_identity'),'clip_approved':clip_approved(p),'battle_current':battle_current(p),'filled':sum(bool(f['versions']) for f in p['frames']),'frame_count':len(p['frames'])} for entry in self.repo.list() for p in [self.repo.get(entry['id'])]]
 def upload(self,p,data):
  try:content=base64.b64decode(data['png'],validate=True)
  except Exception:raise ValueError('잘못된 PNG 데이터')
  expected=None
  if data.get('target') in ('frame','guide') and p['refs'].get('first'):
   with Image.open(self.repo.asset_path(p['refs']['first'])) as im:expected=im.size
  if data.get('target') not in ('reference','frame','guide'):raise ValueError('업로드 대상 오류')
  if data['target'] in ('frame','guide'):frame_at(p,int(data['index']))
  if data['target']=='reference' and data.get('role') not in (*ROLES,'hidden_ref'):raise ValueError('참고 역할 오류')
  checks=inspect_png(content,expected)
  if data['target']=='reference' and checks['blocking']:raise ValueError(', '.join(checks['blocking']))
  if data['target']=='guide' and checks['blocking']:raise ValueError(', '.join(checks['blocking']))
  asset=self.repo.put_asset(content)
  if data.get('target')=='reference':
   if checks['blocking']:raise ValueError(', '.join(checks['blocking']))
   replace_reference(p,data['role'],asset)
  elif data['target']=='guide':
   f=frame_at(p,int(data['index']));f['guide']=asset;f['approved']=False;f['needs_regeneration']=True;p['revision']+=1
   p.pop('export',None);p.pop('review_export',None)
  else:self.add_frame(p,int(data['index']),asset,checks,'imported')
  return asset
 def add_frame(self,p,index,asset,checks,method):
  if 'approval_gate' in p.get('production_flow',{}):
   if method!='staged_authoring_candidate' or approval_gate.state(p,__import__('production_flow').current)['active'] is not None:
    raise ValueError('단계 승인 전 일반 프레임 기록은 차단됩니다.')
  if not 0<=index<len(p['frames']):raise ValueError('프레임 번호 오류')
  f=p['frames'][index];f['versions'].append({'asset':asset,'checks':checks,'method':method});f['approved']=False
  f.pop('needs_regeneration',None);f.pop('result_guide_report',None)
  p.pop('review_export',None);p.pop('export',None);p.pop('temporal_review',None)
  # Changed keys invalidate dependent intermediate approvals and outstanding requests.
  p['revision']+=1
  if f['kind']=='key':
   for other in p['frames']:
    if other['kind']=='between':other['approved']=False
 def action(self,pid,data):
  with self.repo.lock:
   p=self.repo.get(pid);action=data['action']
   approval_gate.guard_legacy(p,action)
   if self.active and self.active['project']==pid and action not in ('cancel',):raise ValueError('생성 중에는 이 프로젝트를 수정할 수 없습니다.')
   if action in ('generate','draft','batch','batch_draft','refine','refine_all'):production_method.require(p,'frame_authoring')
   if action=='production_method':production_method.select(p,data.get('method'))
   elif action=='production_flow':self.production_flow.execute(p,data)
   elif action=='rename':
    name=data.get('name')
    if not isinstance(name,str) or not name.strip() or len(name)>100:raise ValueError('이름은 1~100자입니다.')
    p['name']=name.strip()
   elif action=='save':
    for key in ('identity','motion','hidden'):
     val=data.get(key,'')
     if not isinstance(val,str) or len(val)>30000:raise ValueError('제작 기준은 30,000자 이하입니다.')
     if p.get(key)!=val:p[key]=val;invalidate(p)
   elif action=='upload':self.upload(p,data)
   elif action=='result_guide_config':self.guides.configure(p,data.get('index'),data.get('criteria'))
   elif action=='result_guide_inspect':self.guides.inspect(p,data.get('index'))
   elif action=='result_guide_review':self.guides.review(p,data.get('index'),data.get('token'),data.get('note'),data.get('confirmed'))
   elif action in ('apply_reference_patch','apply_surface_shading'):
    prepare=prepare_surface_shading if action=='apply_surface_shading' else prepare_reference_patch
    content,checks,donor,mask=prepare(self.repo,p,data)
    checks['reference_patch'].update(donor_asset=self.repo.put_asset(donor),mask_asset=self.repo.put_asset(mask))
    self.add_frame(p,data['index'],self.repo.put_asset(content),checks,'surface_shading' if action=='apply_surface_shading' else 'reference_masked_patch')
   elif action=='import_clip':self.clips.replace(p,data)
   elif action=='frame_fx':self.clips.set_fx(p,data)
   elif action=='fx_timeline':FxTimeline(self.repo).configure(p,data)
   elif action=='companion_settings':configure_companion(self.repo,p,data)
   elif action=='clip_identity':set_clip_identity(p,data)
   elif action=='projectile_settings':self.projectile.configure(p,data)
   elif action=='effect_design':self.effects.configure(p,data)
   elif action=='temporal_review':
    if self.active or p.get('batch',{}).get('state')=='running':raise ValueError('생성 종료 후 보정하세요.')
    self.temporal.create(p,data)
   elif action=='wind_settings':configure_wind(p,data)
   elif action=='approve_character':approve_character(p)
   elif action=='battle_settings':self.battle.configure(p,data)
   elif action=='battle_export':self.battle.export(p)
   elif action=='approve_clip':
    self.guides.require_all(p);approve_clip(p)
   elif action=='schedule':
    if any(f['versions'] for f in p['frames']):p.setdefault('clip_history',[]).append(copy.deepcopy(p['frames']))
    set_schedule(p,data['rows'])
   elif action=='retime':retime_frames(p,data.get('durations'))
   elif action=='notes':
    i=int(data['index']);f=frame_at(p,i);f['notes']=str(data['notes'])[:4000];f['approved']=False;p['revision']+=1
    p.pop('export',None)
   elif action=='approve_frame':
    self.guides.require(p,int(data['index']));approve_frame(p,int(data['index']))
   elif action=='restore':
    i=int(data['index']);f=frame_at(p,i);v=int(data['version'])
    if not 0<=v<len(f['versions']):raise ValueError('버전 번호 오류')
    version=copy.deepcopy(f['versions'][v]);self.add_frame(p,i,version['asset'],version['checks'],'restored')
   elif action=='request':
    req=build_request(p,int(data['index']));p['requests'].append(req)
   elif action in ('clean_edges','clean_edges_all','finish_detail','finish_detail_all','smooth_alpha_edge'):
    if self.active:raise ValueError('생성 종료 후 정리하세요.')
    if not p.get('refinement_mask'):raise ValueError('먼저 수정 영역 마스크를 등록하세요.')
    all_frames=action.endswith('_all');detail=action.startswith('finish_detail')
    processor=finish_detail if detail else clean_red_fringe
    report_key='detail_finish' if detail else 'edge_cleanup'
    method='masked_detail_finish' if detail else 'masked_edge_color_cleanup'
    if action=='smooth_alpha_edge':processor=smooth_alpha_edge;report_key='alpha_edge_cleanup';method='masked_alpha_edge_cleanup'
    indices=list(range(len(p['frames']))) if all_frames else [int(data['index'])]
    prepared=[]
    # Validate/prepare all frames before mutating the project; one durable save below.
    with Image.open(self.repo.asset_path(p['refinement_mask'])) as mask:
     for i in indices:
      f=frame_at(p,i)
      if not f['versions']:raise ValueError('모든 대상 프레임에 결과가 필요합니다.')
      source=f['versions'][-1]
      with Image.open(self.repo.asset_path(source['asset'])) as im:result,report=processor(im,mask)
      if not report['changed_pixels']:continue
      content=io.BytesIO();result.save(content,format='PNG')
      # Existing blockers/approval restrictions cannot be bypassed by pixel cleanup.
      checks=copy.deepcopy(source['checks']);checks[report_key]=report
      prepared.append((i,content.getvalue(),checks))
    for i,content,checks in prepared:self.add_frame(p,i,self.repo.put_asset(content),checks,method)
    p[report_key]={'frames_changed':[i for i,_,_ in prepared],'scope':'bounded local contrast' if detail else 'red/magenta fringe near black within registered mask; no new AI generation'}
    if action=='smooth_alpha_edge':p[report_key]['scope']='single-pass coverage antialiasing in selected alpha edges; no new AI generation'
    if all_frames:self.review_export(p)
   elif action=='refinement_settings':
    prompt=data.get('prompt','')
    if not isinstance(prompt,str) or not 0<len(prompt.strip())<=4000:raise ValueError('수정 지시는 1~4000자로 입력하세요.')
    art=data.get('reference_asset')
    if art is not None and (not isinstance(art,str) or art not in p['refs'].values()):raise ValueError('등록된 원화 참고가 필요합니다.')
    wind=data.get('wind',False);lag=data.get('lag',0);lock_endpoints=data.get('lock_endpoints',False)
    silhouette=data.get('silhouette',False)
    if type(silhouette)!=bool:raise ValueError('Invalid silhouette setting')
    if type(lock_endpoints)!=bool:raise ValueError('시작·끝 보존 설정 오류')
    if type(wind)!=bool or type(lag)!=int or not 0<=lag<=3:raise ValueError('흩날림 설정 오류')
    if p.get('wind_settings'):wind=p['wind_settings']['enabled']
    if data.get('png'):
     try:content=base64.b64decode(data['png'],validate=True)
     except Exception:raise ValueError('잘못된 마스크 PNG')
     if not content.startswith(b'\x89PNG\r\n\x1a\n'):raise ValueError('PNG 마스크가 필요합니다.')
     frames=[f for f in p['frames'] if f['versions']]
     if not frames:raise ValueError('먼저 결과 프레임을 등록하세요.')
     with Image.open(io.BytesIO(content)) as mask:
      for f in frames:
       with Image.open(self.repo.asset_path(f['versions'][-1]['asset'])) as im:validate_mask(mask,im.size)
     p['refinement_mask']=self.repo.put_asset(content)
    if not p.get('refinement_mask'):raise ValueError('마스크를 등록하세요.')
    if art:p['refinement_reference']=art
    else:p.pop('refinement_reference',None)
    p['refinement_prompt']=prompt.strip();p['refinement_wind']=wind;p['refinement_lag']=lag;p['refinement_lock_endpoints']=lock_endpoints;p['refinement_silhouette']=silhouette;p['revision']+=1
   elif action in ('batch','batch_draft','refine_all'):
    self.batch.start(p,action!='batch',action=='refine_all');return self.view(p)
   elif action in ('generate','draft','refine'):
    if self.active:raise ValueError('다른 생성이 진행 중입니다. 한 번에 한 프레임만 실행합니다.')
    if action=='refine':
     if self.provider.info()['selected']!='local':raise ValueError('국소 수정은 로컬 전용입니다.')
     i=int(data['index']);frame_at(p,i);req=refinement_request(p,i,p.get('batch',{}).get('seed',923041))
    elif action=='draft':
     if self.provider.info()['selected']!='local':raise ValueError('원화 시험은 로컬 엔진 전용입니다.')
     first=p['refs'].get('first')
     if not first:raise ValueError('게임시점 첫 프레임을 등록하세요.')
     frame_at(p,int(data['index']))
     req=draft_request(p,int(data['index']),p.get('batch',{}).get('seed',int(uuid.uuid4().hex[:8],16)))
    else:req=build_request(p,int(data['index']))
    req['engine']=self.provider.reserve()
    try:
     req['status']='running';p['requests'].append(req)
     self.active={'project':pid,'request':req['id'],'frame':req['frame_index'],'engine':req['engine']};self.repo.save(p)
     threading.Thread(target=self.run,args=(pid,copy.deepcopy(req)),daemon=True).start();return self.view(p)
    except Exception:
     self.active=None;self.provider.release();raise
   elif action=='cancel':self.batch.cancel();self.provider.cancel()
   elif action=='review_export':self.review_export(p)
   elif action=='inspect':
    for i,f in enumerate(p['frames']):
     if i and f['versions'] and p['frames'][i-1]['versions']:f['comparison']=compare_images(self.repo.asset_path(p['frames'][i-1]['versions'][-1]['asset']),self.repo.asset_path(f['versions'][-1]['asset']))
   elif action=='export':
    if data.get('approved_only'):self.guides.require_all(p)
    if not p['frames'] or any(not f['versions'] for f in p['frames']):raise ValueError('모든 프레임의 결과가 필요합니다.')
    approved=clip_approved(p) or all(f['approved'] for f in p['frames'])
    if approved:
     try:self.guides.require_all(p)
     except ValueError:approved=False
    if data.get('approved_only') and not approved:raise ValueError('현재 애니메이션의 최종 미술 승인이 필요합니다.')
    if any(f['versions'][-1]['checks']['blocking'] for f in p['frames']):raise ValueError('이미지 차단 검사를 해결하세요.')
    out=export_clip([self.repo.asset_path(f['versions'][-1]['asset']) for f in p['frames']],[f['duration_ms'] for f in p['frames']],self.repo.root/'exports'/uuid.uuid4().hex)
    p['export']={k:self.repo.put_asset(Path(v).read_bytes(),Path(v).suffix[1:]) for k,v in out.items()};p['export']['status']='approved' if approved else 'review-only';p['export']['signature']=signature(p)
   else:raise ValueError('지원하지 않는 작업')
   self.repo.save(p);return self.view(p)
 def review_export(self,p):
  if any(not f['versions'] for f in p['frames']):raise ValueError('모든 프레임의 결과가 필요합니다.')
  paths=[self.repo.asset_path(f['versions'][-1]['asset']) for f in p['frames']]
  p['temporal_review']={'pairs':[dict(frame=i+1,**compare_images(paths[i-1],paths[i])) for i in range(1,len(paths))], 'loop':compare_images(paths[-1],paths[0]),'scope':'변화량 참고. 접지·파지·관절·플리커 미술 판정은 사람 검토 필요.'}
  diffs=[v.get('mean_rgb_difference',0) for v in p['temporal_review']['pairs']]
  p['temporal_review']['warnings']=['프레임 평균 변화량이 매우 작습니다. 동작이 실제 반영됐는지 확인하세요.'] if diffs and max(diffs)<0.05 else []
  out=export_clip(paths,[f['duration_ms'] for f in p['frames']],self.repo.root/'exports'/uuid.uuid4().hex)
  p['review_export']={k:self.repo.put_asset(Path(v).read_bytes(),Path(v).suffix[1:]) for k,v in out.items()};p['review_export']['status']='review-only-not-approved'
 def execute_request(self,pid,req):
  try:
   path=self.provider.generate(req,[self.repo.asset_path(n) for n in req['reference_assets']],self.repo.root/'jobs'/req['id'])
   content=path.read_bytes()
   with self.repo.lock:
    p=self.repo.get(pid)
    if p['revision']!=req['revision']:raise ValueError('제작 기준이 변경되어 결과를 자동 적용하지 않았습니다.')
    with Image.open(self.repo.asset_path(p['refs']['first'])) as im:expected=im.size
    checks=inspect_png(content,expected)
    if req.get('draft'):checks['blocking'].append('로컬 연결 시험본: 정식 제작·승인 대상 아님')
    method=req['engine']+('_masked_detail_draft' if req.get('refinement') else '_pose_repaint_draft' if req.get('draft') and req.get('reference_roles',{}).get('pose') else '_draft' if req.get('draft') else '')
    if req.get('preserve_endpoint'):method='preserved_loop_endpoint'
    elif req.get('silhouette_draft'):method='local_silhouette_draft'
    asset=self.repo.put_asset(content);self.add_frame(p,req['frame_index'],asset,checks,method)
    r=next(r for r in p['requests'] if r['id']==req['id']);r.update(status='needs_review',result_asset=asset);self.repo.save(p)
  except Exception as e:
   with self.repo.lock:
    p=self.repo.get(pid);r=next(r for r in p['requests'] if r['id']==req['id']);r.update(status='failed',error=str(e));self.repo.save(p)
   raise
 def run(self,pid,req):
  try:self.execute_request(pid,req)
  except Exception:pass # Failure is persisted by execute_request for the UI.
  finally:
   with self.repo.lock:self.active=None;self.provider.release()

from pathlib import Path
