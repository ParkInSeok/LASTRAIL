"""Validated reference-derived donor composition; no generation or geometry changes."""
import base64, copy, hashlib, io
from PIL import Image, ImageChops
from refinement import validate_mask

def decode_png(encoded):
 try:
  if not isinstance(encoded,str) or len(encoded)>24*1024*1024:raise ValueError('PNG 입력 크기 오류')
  raw=base64.b64decode(encoded,validate=True)
  with Image.open(io.BytesIO(raw)) as im:
   if im.format!='PNG' or getattr(im,'n_frames',1)!=1 or max(im.size)>4096:raise ValueError('4096px 이하 정지 PNG만 사용하세요.')
   im.load();return raw,im.copy()
 except (OSError,TypeError) as e:raise ValueError('PNG를 확인하세요.') from e

def prepare_reference_patch(repo,p,data):
 index=data.get('index')
 if type(index)!=int or not 0<=index<len(p['frames']) or type(data.get('source_index'))!=int or data['source_index']!=index:raise ValueError('대상 프레임이 달라졌습니다.')
 f=p['frames'][index]
 if not f['versions']:raise ValueError('원본 프레임이 없습니다.')
 if type(data.get('source_version'))!=int or data['source_version']!=len(f['versions'])-1 or type(data.get('source_revision'))!=int or data['source_revision']!=p['revision'] or data.get('source_asset')!=f['versions'][-1]['asset']:raise ValueError('원본 버전이 달라졌습니다. 다시 선택하세요.')
 reference=data.get('reference_asset')
 if not reference or reference not in p['refs'].values():raise ValueError('프로젝트에 등록된 원화 참고를 선택하세요.')
 note=data.get('generation_note')
 if not isinstance(note,str) or not note.strip() or len(note)>4000:raise ValueError('실제 생성 참조와 교정 방식을 기록하세요.')
 donor_raw,donor=decode_png(data.get('donor_png'));mask_raw,mask=decode_png(data.get('mask_png'))
 with Image.open(repo.asset_path(f['versions'][-1]['asset'])) as im:original=im.convert('RGBA')
 if donor.size!=original.size:raise ValueError('교정본 크기는 현재 프레임과 같아야 합니다. 자동 변형하지 않습니다.')
 m=validate_mask(mask,original.size)
 mode=data.get('alpha_mode','preserve');protected=[]
 if mode not in ('preserve','masked_rgba'):raise ValueError('지원하지 않는 투명도 교정 방식입니다.')
 if mode=='masked_rgba':
  if donor.mode!='RGBA':raise ValueError('투명도를 포함한 RGBA 교정 PNG가 필요합니다.')
  if any(value not in (0,255) for count,value in m.getcolors(256)):raise ValueError('윤곽 교정 마스크는 검정/흰색만 사용하세요.')
  protected=data.get('protected_regions')
  if not isinstance(protected,list) or not 1<=len(protected)<=16:raise ValueError('발 등 보존할 영역을 지정하세요.')
  for box in protected:
   if not isinstance(box,list) or len(box)!=4 or any(type(v)!=int for v in box):raise ValueError('보호 영역 좌표를 확인하세요.')
   x0,y0,x1,y1=box
   if not (0<=x0<x1<=original.width and 0<=y0<y1<=original.height):raise ValueError('보호 영역이 캔버스를 벗어났습니다.')
   if m.crop(box).getbbox():raise ValueError('수정 마스크가 보호 영역과 겹칩니다.')
  result=Image.composite(donor,original,m)
 else:
  # Legacy mode retains original alpha, including when donor is opaque.
  result=Image.composite(donor.convert('RGB'),original.convert('RGB'),m).convert('RGBA');result.putalpha(original.getchannel('A'))
 delta=ImageChops.difference(original,result);outside=m.point(lambda x:255 if x==0 else 0)
 if any(ImageChops.multiply(c,outside).getbbox() for c in delta.split()):raise ValueError('마스크 밖 픽셀 보존 검증 실패')
 if any(any(c.crop(box).getbbox() for c in delta.split()) for box in protected):raise ValueError('보호 영역 픽셀 보존 검증 실패')
 if not any(c.getbbox() for c in delta.split()):raise ValueError('수정 영역에 변경된 픽셀이 없습니다.')
 content=io.BytesIO();result.save(content,format='PNG')
 checks=copy.deepcopy(f['versions'][-1]['checks'])
 report={'source_index':index,'source_version':data['source_version'],'source_asset':data['source_asset'],'source_revision':data['source_revision'],'reference_asset':reference,'reference_sha256':hashlib.sha256(repo.asset_path(reference).read_bytes()).hexdigest(),'donor_sha256':hashlib.sha256(donor_raw).hexdigest(),'mask_sha256':hashlib.sha256(mask_raw).hexdigest(),'generation_note':note.strip(),'provenance_scope':'editor-supplied generation record; not automatic proof of reference use','outside_mask_exact':True,'alpha_exact':not delta.getchannel('A').getbbox(),'alpha_mode':mode,'protected_regions':protected,'protected_regions_exact':True,'mask_bbox':m.getbbox()}
 checks['reference_patch']=report
 return content.getvalue(),checks,donor_raw,mask_raw
