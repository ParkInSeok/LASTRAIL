"""Review output orchestration; originals and source approval state remain unchanged."""
import base64,io,uuid,copy
from pathlib import Path
from PIL import Image
from temporal_finish import TemporalProcessor
from rigid_finish import RigidProcessor
from imaging import export_clip
from delivery_review import signature

class TemporalReview:
 def __init__(self,repo):self.repo=repo
 def create(self,p,data):
  step=data.get('step_ms',20);reference=data.get('reference',0);transition=data.get('transition_ms',80)
  if type(step)!=int or step not in (20,40):raise ValueError('검토 간격은 20 또는 40ms입니다.')
  if type(transition)!=int or not 20<=transition<=160:raise ValueError('전환은 20~160ms입니다.')
  frames=p['frames'];total=sum(f['duration_ms'] for f in frames)
  if not frames or total%step or total//step>96:raise ValueError('전체 시간을 간격으로 나누고 96장 이하로 제한하세요.')
  images=[]
  for f in frames:
   if not f['versions']:raise ValueError('모든 원화가 필요합니다.')
   with Image.open(self.repo.asset_path(f['versions'][-1]['asset'])) as im:images.append(im.convert('RGBA'))
  encoded=data.get('mask_png')
  if encoded:
   try:
    raw=base64.b64decode(encoded,validate=True)
    if len(raw)>24*1024*1024:raise ValueError('마스크가 너무 큽니다.')
    with Image.open(io.BytesIO(raw)) as im:mask=im.convert('L')
   except Exception as e:raise ValueError('마스크 PNG를 확인하세요.') from e
  elif p.get('temporal_mask'):
   with Image.open(self.repo.asset_path(p['temporal_mask'])) as im:mask=im.convert('L')
   raw=self.repo.asset_path(p['temporal_mask']).read_bytes()
  else:raise ValueError('흰색=움직임, 검정=고정 마스크를 등록하세요.')
  rig=data.get('rig')
  processor=RigidProcessor(images,mask,reference,rig,total) if rig is not None else TemporalProcessor(images,mask,reference,data.get('landmarks'))
  # Preserve explicit blocked/draft state in separate review metadata.
  blockers=sorted({v for f in frames for v in f['versions'][-1]['checks'].get('blocking',[])})
  out=self.repo.root/'exports'/uuid.uuid4().hex;out.mkdir(parents=True)
  starts=[];at=0
  for f in frames:starts.append(at);at+=f['duration_ms']
  paths=[]
  for t in range(0,total,step):
   i=max(j for j,s in enumerate(starts) if s<=t);prev=(i-1)%len(frames)
   # Transitions finish at the scheduled boundary, keeping event timing fixed.
   nxt=(i+1)%len(frames);end=starts[i]+frames[i]['duration_ms']
   span=min(transition,frames[i]['duration_ms'])
   u=max(0,(t-(end-span))/span)
   result=processor.at(t) if rig is not None else processor.frame(i,nxt,u)
   path=out/f'frame-{len(paths):03d}.png';result.save(path);paths.append(path)
  exported=export_clip(paths,[step]*len(paths),out/'review')
  record={'signature':signature(p),'status':'review-only-not-approved','method':'masked optical-flow interpolation + fixed reference outside mask; no new AI drawing','step_ms':step,'transition_ms':transition,'reference':reference,'duration_ms':total,'blocking':blockers,'frames':[self.repo.put_asset(v.read_bytes()) for v in paths],'outputs':{k:self.repo.put_asset(Path(v).read_bytes(),Path(v).suffix[1:]) for k,v in exported.items()}}
  record['rig']=rig
  record['landmarks']=data.get('landmarks')
  if record['landmarks']:record['method']='landmark-guided triangle interpolation + fixed reference outside mask; no new AI drawing'
  if rig is not None:record['method']='rigid region rotation + fixed joint overlap + local thumb transition; existing artwork'
  if p.get('temporal_output'):p.setdefault('temporal_history',[]).append(copy.deepcopy(p['temporal_output']))
  p['temporal_mask']=self.repo.put_asset(raw);p['temporal_output']=record
