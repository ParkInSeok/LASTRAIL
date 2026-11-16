"""ComfyUI localhost adapter; one submitted job, no remote/API fallback."""
import copy,json,re,time,uuid,threading
from pathlib import Path
from urllib.request import Request,build_opener,ProxyHandler,HTTPRedirectHandler
from urllib.parse import urlencode

def render_workflow(template,values):
 def walk(value):
  if isinstance(value,dict):return {k:walk(v) for k,v in value.items()}
  if isinstance(value,list):return [walk(v) for v in value]
  if isinstance(value,str) and value.startswith('{{') and value.endswith('}}'):
   key=value[2:-2]
   if key not in values:raise ValueError('워크플로 입력 누락: '+key)
   return values[key]
  return value
 return walk(template)

class NoRedirect(HTTPRedirectHandler):
 def redirect_request(self,*args,**kwargs):raise ValueError('로컬 서버의 외부 리디렉션은 허용하지 않습니다.')

class LocalProvider:
 def __init__(self,root,port=8188):
  self.root=Path(root);self.base='http://127.0.0.1:'+str(port);self.cancelled=threading.Event()
  self.http=build_opener(ProxyHandler({}),NoRedirect());self.job=None
 def prepare(self):self.cancelled.clear()
 def call(self,path,data=None,content_type='application/json'):
  raw=json.dumps(data).encode() if isinstance(data,dict) else data
  req=Request(self.base+path,data=raw,headers={'Content-Type':content_type})
  with self.http.open(req,timeout=15) as response:
   body=response.read(32*1024*1024+1)
   if len(body)>32*1024*1024:raise ValueError('로컬 응답 크기 초과')
   return body
 def json(self,path,data=None):return json.loads(self.call(path,data))
 def config(self):
  p=self.root/'local-workflow.json'
  if not p.exists():raise ValueError('로컬 제작 워크플로가 아직 설치되지 않았습니다.')
  cfg=json.loads(p.read_text(encoding='utf-8'))
  if not isinstance(cfg.get('workflow'),dict) or not cfg.get('output_node'):raise ValueError('워크플로 설정 오류')
  return cfg
 def status(self):
  try:
   cfg=self.config();self.json('/system_stats')
   return {'ready':True,'message':'로컬 ComfyUI 연결됨 · '+cfg.get('label','미술 검토 필요'),'billing':'local_gpu'}
  except Exception as e:return {'ready':False,'message':'로컬 준비 필요: '+str(e),'billing':'local_gpu'}
 def upload(self,path):
  boundary='FrameStudio'+uuid.uuid4().hex;name=uuid.uuid4().hex+'.png'
  body=(f'--{boundary}\r\nContent-Disposition: form-data; name="image"; filename="{name}"\r\nContent-Type: image/png\r\n\r\n').encode()+path.read_bytes()+f'\r\n--{boundary}--\r\n'.encode()
  result=json.loads(self.call('/upload/image',body,'multipart/form-data; boundary='+boundary))
  return '/'.join(filter(None,[result.get('subfolder',''),result['name']]))
 def cancel(self):self.cancelled.set()
 def generate(self,request,refs,work,workflow_override=None):
  if request.get('refinement') and workflow_override is None:
   from refinement import LocalRefinement
   return LocalRefinement(self).generate(request,refs,work)
  cfg=workflow_override or self.config();work.mkdir(parents=True,exist_ok=True)
  by_name={p.name:p for p in refs};roles=request.get('reference_roles',{})
  if request.get('draft') and roles.get('pose'):
   cfg=copy.deepcopy(cfg)
   # Pose-guided repaint explicitly consumes the pose, not a hidden first-frame substitution.
   def pose_input(v):
    if isinstance(v,dict):return {k:pose_input(x) for k,x in v.items()}
    if isinstance(v,list):return [pose_input(x) for x in v]
    return '{{ref:pose}}' if v=='{{ref:first}}' else v
   cfg['workflow']=pose_input(cfg['workflow'])
  values={'prompt':request['prompt'],'seed':request.get('seed',int(request['id'][:8],16))}
  template=json.dumps(cfg['workflow']);used=[]
  # Explicitly fail when a production workflow would silently drop references.
  required=set(roles) if not request.get('draft') else {'pose' if roles.get('pose') else 'first'}
  if request.get('neighbor_indices'):required.update(('before','after'))
  mapping=dict(roles)
  extras=[n for n in request['reference_assets'] if n not in roles.values()]
  if request.get('neighbor_indices') and len(extras)>=2:mapping.update(before=extras[-2],after=extras[-1])
  for role in required:
   if '{{ref:'+role+'}}' not in template:raise ValueError('현재 로컬 워크플로는 '+role+' 참고를 지원하지 않습니다. 임시 원화 시험 또는 다중참조 워크플로가 필요합니다.')
  for role,name in mapping.items():
   if '{{ref:'+role+'}}' in template:
    if self.cancelled.is_set():raise ValueError('생성 취소됨')
    values['ref:'+role]=self.upload(by_name[name]);used.append(role)
  graph=render_workflow(cfg['workflow'],values)
  (work/'request.json').write_text(json.dumps(dict(request,workflow=graph,used_reference_roles=used),ensure_ascii=False,indent=2),encoding='utf-8')
  if self.cancelled.is_set():raise ValueError('생성 취소됨')
  answer=self.json('/prompt',{'prompt':graph,'client_id':uuid.uuid4().hex})
  if answer.get('node_errors') or not answer.get('prompt_id'):raise ValueError('로컬 워크플로 거절: '+json.dumps(answer))
  self.job=answer['prompt_id'];deadline=time.monotonic()+900
  try:
   while time.monotonic()<deadline:
    if self.cancelled.is_set():
     self.json('/queue',{'delete':[self.job]})
     raise ValueError('결과 수신을 취소했습니다. 이미 GPU에서 실행 중이면 해당 로컬 작업은 완료될 수 있습니다.')
    history=self.json('/history/'+self.job).get(self.job)
    if history:
     if history.get('status',{}).get('status_str')=='error':raise ValueError('로컬 모델 실행 실패: '+json.dumps(history.get('status')))
     images=history.get('outputs',{}).get(str(cfg['output_node']),{}).get('images',[])
     if not images:raise ValueError('지정한 출력 노드에 이미지가 없습니다.')
     entry=images[0];png=self.call('/view?'+urlencode({k:entry[k] for k in ('filename','subfolder','type') if k in entry}))
     if not png.startswith(b'\x89PNG\r\n\x1a\n'):raise ValueError('로컬 출력이 PNG가 아닙니다.')
     result=work/'result.png';result.write_bytes(png);return result
    self.cancelled.wait(1)
   raise ValueError('로컬 생성 15분 제한 초과. ComfyUI에서 작업 상태를 확인하세요.')
  finally:self.job=None
