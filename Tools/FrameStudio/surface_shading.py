"""Deterministic reference shading transport; target geometry and alpha stay owned by the frame."""
import base64,hashlib,io,json
import numpy as np
from PIL import Image
from refinement import validate_mask
from reference_patch import decode_png,prepare_reference_patch
from strand_surface import compile_strands

def prepare_surface_shading(repo,p,data):
 index=data.get('index')
 if type(index)!=int or not 0<=index<len(p['frames']):raise ValueError('프레임 번호 오류')
 f=p['frames'][index]
 if not f['versions'] or data.get('source_asset')!=f['versions'][-1]['asset'] or type(data.get('source_revision'))!=int or data['source_revision']!=p['revision']:raise ValueError('원본 버전이 달라졌습니다.')
 reference=data.get('reference_asset')
 if not reference or reference not in p['refs'].values():raise ValueError('등록된 기준 원화를 선택하세요.')
 reference_raw=repo.asset_path(reference).read_bytes();sha=hashlib.sha256(reference_raw).hexdigest()
 if data.get('reference_sha256')!=sha:raise ValueError('기준 원화 해시가 달라졌습니다.')
 with Image.open(io.BytesIO(reference_raw)) as im:ref=np.array(im.convert('RGBA'))
 with Image.open(repo.asset_path(f['versions'][-1]['asset'])) as im:original=im.convert('RGBA')
 if original.size!=(ref.shape[1],ref.shape[0]):raise ValueError('원본과 기준 캔버스 크기를 맞추세요.')
 _,mask=decode_png(data.get('mask_png'));m=np.array(validate_mask(mask,original.size))
 input_spec=data.get('surface_map');spec,strand_tracks=compile_strands(input_spec);surfaces=spec.get('surfaces') if isinstance(spec,dict) else None
 if not isinstance(surfaces,list) or not 1<=len(surfaces)<=16:raise ValueError('대응 면은 1~16개입니다.')
 h,w=m.shape;out=np.array(original);covered=np.zeros((h,w),bool);owner=np.full((h,w),-1,np.int16);seen=set()
 for si,s in enumerate(surfaces):
  if not isinstance(s,dict) or not isinstance(s.get('id'),str) or not s['id'] or len(s['id'])>100 or s['id'] in seen:raise ValueError('고유한 면 ID가 필요합니다.')
  seen.add(s['id'])
  try:src=np.asarray(s['source'],dtype=float);dst=np.asarray(s['target'],dtype=float)
  except (KeyError,ValueError,TypeError):raise ValueError('면 대응 좌표 오류')
  if src.shape!=dst.shape or src.ndim!=2 or src.shape[1]!=2 or not 3<=len(src)<=256 or not np.isfinite(src).all() or not np.isfinite(dst).all():raise ValueError('유한한 동일 개수의 대응점이 필요합니다.')
  for pts in (src,dst):
   if (pts<0).any() or (pts[:,0]>w-1).any() or (pts[:,1]>h-1).any():raise ValueError('대응점이 캔버스를 벗어났습니다.')
  triangles=s.get('triangles')
  if not isinstance(triangles,list) or not 1<=len(triangles)<=512:raise ValueError('삼각형 연결 오류')
  for tri in triangles:
   if not isinstance(tri,list) or len(tri)!=3 or any(type(v)!=int or not 0<=v<len(src) for v in tri) or len(set(tri))!=3:raise ValueError('삼각형 인덱스 오류')
   a=src[tri];b=dst[tri];sm=np.column_stack((a[1]-a[0],a[2]-a[0]));dm=np.column_stack((b[1]-b[0],b[2]-b[0]));sd=np.linalg.det(sm);dd=np.linalg.det(dm)
   if abs(sd)<1e-6 or abs(dd)<1e-6 or sd*dd<=0:raise ValueError('접히거나 뒤집힌 대응 면입니다.')
   x0,y0=np.floor(b.min(0)).astype(int);x1,y1=np.ceil(b.max(0)).astype(int);yy,xx=np.mgrid[y0:y1+1,x0:x1+1]
   points=np.stack((xx-b[0,0],yy-b[0,1]),-1);uv=points@np.linalg.inv(dm).T
   inside=(uv[:,:,0]>=-1e-8)&(uv[:,:,1]>=-1e-8)&(uv.sum(2)<=1+1e-8)&(m[y0:y1+1,x0:x1+1]>0)
   y=yy[inside];x=xx[inside]
   if not len(x):continue
   mapped=np.rint(a[0]+uv[inside]@sm.T).astype(int);colors=ref[mapped[:,1],mapped[:,0]]
   if (colors[:,3]==0).any():raise ValueError('대응 위치가 기준의 투명한 틈입니다.')
   duplicate=covered[y,x]
   if ((owner[y,x][duplicate]!=si).any() or (out[y,x,:3][duplicate]!=colors[:,:3][duplicate]).any()):raise ValueError('면 대응이 중첩되어 충돌합니다.')
   out[y,x,:3]=colors[:,:3];covered[y,x]=True;owner[y,x]=si
 if ((m>0)&~covered).any():raise ValueError('수정 마스크에 대응되지 않은 면이 있습니다.')
 # Nearest sampling retains source RGB exactly; upgrade only with an explicit filtered-color contract.
 buf=io.BytesIO();Image.fromarray(out).save(buf,format='PNG')
 args={**data,'alpha_mode':'preserve','donor_png':base64.b64encode(buf.getvalue()).decode(),'generation_note':'Deterministic same-surface reference RGB sampling using saved triangle correspondence; no AI redraw or actor geometry warp.'}
 content,checks,donor,mask_raw=prepare_reference_patch(repo,p,args)
 checks['surface_shading']={'schema':1,'reference_asset':reference,'reference_sha256':sha,'map':spec,'map_sha256':hashlib.sha256(json.dumps(spec,sort_keys=True,allow_nan=False).encode()).hexdigest(),'sampling':'nearest-source-RGB; mask255 exact, transition weights blend target RGB','covered_pixels':int(covered.sum()),'exact_core_pixels':int((m==255).sum()),'transition_pixels':int(((m>0)&(m<255)).sum()),'scope':'color transport only; manually supplied correspondence needs visual review; no automatic semantic tracking'}
 if strand_tracks:
  checks['surface_shading']['strand_tracks']=strand_tracks
  checks['surface_shading']['input_map']=input_spec
 return content,checks,donor,mask_raw
