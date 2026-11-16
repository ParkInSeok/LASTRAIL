"""Saved battle review and visual delivery approval, separate from generation readiness."""
import base64, hashlib, io, json, math, uuid
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image
from imaging import export_clip
from battle_scene import BattleScene
import zipfile
from guide_validation import require_all_reports

def signature(p):
 payload={k:p.get(k) for k in ('revision','refs','identity','motion','hidden','battle_settings')}
 payload['frames']=[(f['versions'][-1]['asset'] if f['versions'] else None,f['duration_ms'],f.get('notes')) for f in p['frames']]
 if any(f.get('fx_asset') for f in p['frames']):payload['frame_fx']=[f.get('fx_asset') for f in p['frames']]
 for key in ('battle_projectile','clip_identity','wind_settings','effect_style','fx_timeline','result_guides'):
  if key in p:payload[key]=p[key]
 return hashlib.sha256(json.dumps(payload,sort_keys=True).encode()).hexdigest()

def battle_current(p):
 return bool(p.get('battle_export') and p['battle_export'].get('signature')==signature(p))

def clip_approved(p):
 try:require_all_reports(p)
 except ValueError:return False
 return bool(p.get('clip_approval') and p['clip_approval'].get('signature')==signature(p) and battle_current(p))

def validate_frames(p):
 if not p['frames'] or any(not f['versions'] for f in p['frames']):raise ValueError('모든 프레임을 먼저 등록하세요.')
 if any(f['versions'][-1]['checks']['blocking'] for f in p['frames']):raise ValueError('이미지 차단 검사 항목을 해결하세요.')

def approve_clip(p):
 require_all_reports(p)
 validate_frames(p)
 if not battle_current(p):raise ValueError('현재 프레임과 배치로 전투 미리보기를 만든 뒤 검토하세요.')
 p['clip_approval']={'signature':signature(p),'at':datetime.now(timezone.utc).isoformat(),'scope':'현재 애니메이션과 전투 배치의 사용자 미술 승인'}

class BattleReview:
 def __init__(self,repo):self.repo=repo
 def configure(self,p,data):
  anchor=data.get('anchor');height=data.get('height')
  if not isinstance(anchor,list) or len(anchor)!=2 or any(type(v) not in (int,float) or not math.isfinite(v) or not 0<=v<=1 for v in anchor):raise ValueError('발 위치는 배경 안에서 지정하세요.')
  if type(height) not in (int,float) or not math.isfinite(height) or not 24<=height<=600:raise ValueError('인물 높이는 24~600px입니다.')
  asset=p.get('battle_settings',{}).get('background')
  if data.get('png'):
   try:
    content=base64.b64decode(data['png'],validate=True)
    if len(content)>24*1024*1024:raise ValueError('배경 용량 초과')
    with Image.open(io.BytesIO(content)) as im:
     if im.format!='PNG' or im.n_frames!=1 or min(im.size)<32 or max(im.size)>4096:raise ValueError('배경은 32~4096px 정지 PNG를 사용하세요.')
     im.load()
   except Exception as e:raise ValueError('전투 배경 PNG를 확인하세요.') from e
   asset=self.repo.put_asset(content)
  if not asset:raise ValueError('전투 배경을 먼저 등록하세요.')
  p['battle_settings']={'background':asset,'anchor':anchor,'height':height}
 def export(self,p):
  validate_frames(p);settings=p.get('battle_settings')
  if not settings:raise ValueError('전투 배경과 배치를 먼저 저장하세요.')
  with Image.open(self.repo.asset_path(settings['background'])) as im:bg=im.convert('RGBA')
  work=self.repo.root/'exports'/uuid.uuid4().hex;work.mkdir(parents=True)
  renderer=BattleScene(self.repo,p,bg,settings);times=renderer.times();paths=[];timeline=[];durations=[]
  for i,(time,end) in enumerate(zip(times,times[1:])):
   scene,fx,row=renderer.render(time);row['duration_ms']=end-time;timeline.append(row);durations.append(end-time)
   path=work/f'frame-{i+1:02}.png';scene.save(path);paths.append(path);fx.save(work/f'fx-{i+1:02}.png')
   renderer.impact_layer(time).save(work/f'impact-{i+1:02}.png')
  files=export_clip(paths,durations,work)
  report={'shot':renderer.shot,'timeline':timeline,'scope':'independent FX; source actor frames unchanged; preview only'}
  if p.get('fx_timeline'):report['fx_timeline']=p['fx_timeline']
  manifest=work/'fx-manifest.json';manifest.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8');files['fx_manifest']=str(manifest)
  with zipfile.ZipFile(files['zip'],'a',zipfile.ZIP_DEFLATED) as archive:
   archive.write(manifest,'fx-manifest.json')
   for i,row in enumerate(p.get('fx_timeline',{}).get('stages',[])):archive.write(self.repo.asset_path(row['asset']),f'source-fx/{i+1:03}.png')
   for i in range(len(paths)):archive.write(work/f'fx-{i+1:02}.png',f'fx/{i+1:03}.png')
   for i in range(len(paths)):archive.write(work/f'impact-{i+1:02}.png',f'impact/{i+1:03}.png')
  p['battle_export']={k:self.repo.put_asset(Path(v).read_bytes(),Path(v).suffix[1:]) for k,v in files.items()}
  p['battle_export'].update(signature=signature(p),scope='검수용 전투 합성 · 배경 속 다른 요원은 정지 · 게임 적용 아님')
