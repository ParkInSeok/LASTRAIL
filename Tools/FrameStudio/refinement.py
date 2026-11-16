"""Local patch preparation and pixel-preservation contract, independent of UI."""
import copy,json,uuid,math
from PIL import Image,ImageChops
from silhouette import composite_silhouette,context_box
from wind_plan import request_wind

def validate_mask(mask,size):
 if mask.size!=size:raise ValueError('마스크는 결과 프레임과 같은 크기여야 합니다.')
 if mask.mode not in ('L','RGB','RGBA'):raise ValueError('흑백 PNG 마스크를 등록하세요.')
 rgb=mask.convert('RGB');r,g,b=rgb.split()
 if ImageChops.difference(r,g).getbbox() or ImageChops.difference(r,b).getbbox():raise ValueError('마스크는 흑백이어야 합니다.')
 if mask.mode=='RGBA' and mask.getchannel('A').getextrema()!=(255,255):raise ValueError('마스크에는 투명도 대신 검정과 흰색을 사용하세요.')
 m=rgb.convert('L');box=m.getbbox()
 if not box:raise ValueError('흰색 수정 영역이 비어 있습니다.')
 if box[2]-box[0]>512 or box[3]-box[1]>512:raise ValueError('수정 영역을 가로·세로 512픽셀 이내로 나눠 주세요.')
 return m

def refinement_request(p,index,seed):
 if not p['refs'].get('first'):raise ValueError('게임시점 첫 프레임을 등록하세요.')
 f=p['frames'][index]
 if not f['versions']:raise ValueError('먼저 수정할 프레임 결과를 등록하세요.')
 mask=p.get('refinement_mask')
 if not mask:raise ValueError('수정 영역 마스크를 등록하세요.')
 prompt=p.get('refinement_prompt','').strip()
 if not prompt:raise ValueError('국소 수정 지시를 저장하세요.')
 source=f['versions'][-1]['asset']
 wind=p.get('refinement_wind',False);lag=p.get('refinement_lag',0)
 amount=math.sin(math.pi*max(0,(index-lag)/max(1,len(p['frames'])-1-lag)))**2 if wind else 0
 if index in (0,len(p['frames'])-1):amount=0
 guide,note=request_wind(p,index)
 if guide is not None:
  wind=p['wind_settings']['enabled']
  region=guide['region'];names=list(guide['parts']) if region=='mixed' else [region]
  amount=max(guide['parts'][name]['intensity'] for name in names)
 phase=('calm resting silhouette' if amount<.05 else 'gentle breeze, outer tips beginning to lift' if amount<.4 else 'clear outward wind sweep, lifted curved tips' if amount<.8 else 'strongest outward sweep, distinctly extended flowing tips')
 if p.get('refinement_silhouette',False):phase=('calm resting silhouette' if amount<.05 else 'tips begin to bend upward without lengthening' if amount<.4 else 'free tips bend upward, preserve length and attachment points' if amount<.8 else 'peak bend of free tips, same length, no elongated spikes')
 prefix='Preserve identity, roots and attachment points. Change only free outer tips in the mask. '+phase+'. ' if wind else 'Preserve reference identity and shape. '
 if guide is not None:
  phase='approved steady airflow, no added gust' if amount<.05 else 'light connected bending along the saved airflow' if amount<.4 else 'clear connected bend under rearward wind pressure' if amount<.8 else 'peak connected bend with retained length and natural recovery'
  prefix='Preserve identity, roots and attachment points. Redraw only the selected part inside the mask; '+phase+'. '
 result={'id':uuid.uuid4().hex,'revision':p['revision'],'frame_index':index,'draft':True,'seed':seed,'refinement':True,'silhouette_draft':bool(p.get('refinement_silhouette',False)),'preserve_endpoint':bool(p.get('refinement_lock_endpoints',False) and index in (0,len(p['frames'])-1)),'refinement_denoise':round(.16+(.34 if p.get('refinement_silhouette',False) else .39)*amount,4),'motion_amount':round(amount,4),'reference_assets':[source,mask],'reference_roles':{'pose':source,'mask':mask},'prompt':prefix+'Clean precise line art, sharp continuous edges. '+prompt+(note or ''),'status':'running'}
 if p.get('refinement_reference'):
  art=p['refinement_reference']
  if art not in p['refs'].values():raise ValueError('등록된 원화 참고가 필요합니다.')
  result['reference_conditioned']=True;result['reference_roles']['art']=art;result['reference_assets'].append(art)
  result['refinement_denoise']=.35
 if guide is not None:result['wind_guide']=guide
 return result

class LocalRefinement:
 def __init__(self,provider):self.provider=provider
 def generate(self,request,refs,work):
  work.mkdir(parents=True,exist_ok=True);by_name={p.name:p for p in refs};roles=request['reference_roles']
  if request.get('preserve_endpoint'):
   path=work/'result.png';path.write_bytes(by_name[roles['pose']].read_bytes())
   (work/'request.json').write_text(json.dumps(dict(request,profile='preserved-loop-endpoint',gpu_generated=False),ensure_ascii=False,indent=2),encoding='utf-8')
   return path
  with Image.open(by_name[roles['pose']]) as im:original=im.convert('RGBA')
  with Image.open(by_name[roles['mask']]) as im:mask=validate_mask(im,original.size)
  # Black-composited generation cannot safely recolor straight-alpha edge pixels.
  # Interpolated opaque interiors can be alpha 250..254; preserve soft edges <250.
  silhouette=request.get('silhouette_draft',False)
  if not silhouette:mask=ImageChops.multiply(mask,original.getchannel('A').point(lambda a:255 if a>=250 else 0))
  if not mask.getbbox():raise ValueError('수정 영역에 불투명한 내부 픽셀이 없습니다. 외곽 투명도는 보존됩니다.')
  bbox=mask.getbbox();cw=min(512,original.width);ch=min(512,original.height)
  x=max(0,min(original.width-cw,(bbox[0]+bbox[2]-cw)//2));y=max(0,min(original.height-ch,(bbox[1]+bbox[3]-ch)//2));box=(x,y,x+cw,y+ch)
  if silhouette:
   box=context_box(original.size,bbox);x,y=box[:2];cw=box[2]-x;ch=box[3]-y
  conditioned=bool(request.get('reference_conditioned'))
  if conditioned and silhouette:raise ValueError('원화 조건 편집은 원본 alpha 보존 모드만 지원합니다.')
  if conditioned and set(mask.tobytes())- {0,255}:raise ValueError('원화 조건 편집은 혼합 없는 이진 마스크가 필요합니다.')
  resolution=512 if silhouette or conditioned else 1024
  bg=Image.new('RGBA',original.size,'black');bg.alpha_composite(original)
  source=work/'patch-source.png';bg.convert('RGB').crop(box).save(source)
  mp=work/'patch-mask.png';mask.crop(box).resize((resolution,resolution),Image.Resampling.BILINEAR).convert('RGB').save(mp)
  cfg=copy.deepcopy(self.provider.config());w=cfg['workflow']
  # This profile is for the installed SD1.5 workflow only; fail on incompatible engines.
  if w.get('1',{}).get('class_type')!='CheckpointLoaderSimple':raise ValueError('현재 워크플로는 국소 수정 프로필을 지원하지 않습니다.')
  w['2']={'class_type':'LoadImage','inputs':{'image':'{{ref:pose}}'}}
  w['3']={'class_type':'ImageScale','inputs':{'image':['2',0],'width':resolution,'height':resolution,'upscale_method':'lanczos','crop':'disabled'}}
  for n,kind,key,src in [('4','VAEEncodeTiled','pixels','3'),('8','VAEDecodeTiled','samples','7')]:
   w[n]={'class_type':kind,'inputs':{key:[src,0],'vae':['1',2],'tile_size':512,'overlap':64,'temporal_size':64,'temporal_overlap':8}}
  w['10']={'class_type':'LoadImage','inputs':{'image':'{{ref:mask}}'}}
  w['11']={'class_type':'ImageToMask','inputs':{'image':['10',0],'channel':'red'}}
  w['12']={'class_type':'SetLatentNoiseMask','inputs':{'samples':['4',0],'mask':['11',0]}}
  strength=request.get('refinement_denoise',.16)
  if type(strength) not in (int,float) or not .16<=strength<=.55:raise ValueError('국소 재작화 강도 오류')
  w['7']['inputs'].update(latent_image=['12',0],denoise=strength,cfg=4,steps=24)
  sub=dict(request,reference_assets=[source.name,mp.name],reference_roles={'pose':source.name,'mask':mp.name})
  inputs=[source,mp]
  if conditioned:
   from reference_conditioning import attach_reference
   art=by_name[roles['art']];attach_reference(w)
   sub['reference_roles']['art']=art.name;sub['reference_assets'].append(art.name);inputs.append(art)
  raw=self.provider.generate(sub,inputs,work/'raw',workflow_override=cfg)
  if self.provider.cancelled.is_set():raise ValueError('국소 수정 취소됨')
  with Image.open(raw) as im:
   if conditioned and im.size!=(cw,ch):raise ValueError('원화 조건 편집 결과 크기 불일치; 자동 축소하지 않습니다.')
   patch=im.convert('RGB') if conditioned else im.convert('RGB').resize((cw,ch),Image.Resampling.LANCZOS)
  layer=original.convert('RGB');layer.paste(patch,(x,y))
  result=Image.composite(layer,original.convert('RGB'),mask).convert('RGBA');result.putalpha(original.getchannel('A'))
  if silhouette:result=composite_silhouette(original,layer,mask)
  # Check each channel; RGBA getbbox alone would hide RGB differences behind zero alpha.
  delta=ImageChops.difference(result,original)
  outside=mask.point(lambda v:0 if v else 255)
  if any(Image.composite(c,Image.new('L',c.size),outside).getbbox() for c in delta.split()):raise ValueError('수정 영역 밖 픽셀이 변경됐습니다.')
  path=work/'result.png';result.save(path)
  (work/'request.json').write_text(json.dumps(dict(request,crop=box,profile='local-silhouette-draft' if silhouette else 'local-detail-protected-edges',outside_mask_exact=True,alpha_preserved=not silhouette,protected_edge_alpha_below=None if silhouette else 250,gpu_generated=True,limitations='Approximate black matte in selected region; outline alpha needs review, not final delivery.' if silhouette else 'Existing alpha and edge RGBA below alpha250 preserved; new silhouette cannot extend into transparent pixels.'),ensure_ascii=False,indent=2),encoding='utf-8')
  return path

