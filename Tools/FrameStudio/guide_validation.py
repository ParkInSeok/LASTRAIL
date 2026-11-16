"""Opt-in result inspection and adoption gate; never modifies generated pixels."""
import hashlib, json
from PIL import Image, ImageChops


def digest(value):
 return hashlib.sha256(json.dumps(value,sort_keys=True,ensure_ascii=False).encode()).hexdigest()


def guide_for(p,index):
 return p.get('result_guides',{}).get(str(index))


def require_report(p,index):
 g=guide_for(p,index)
 if g is None:return
 f=p['frames'][index];r=f.get('result_guide_report')
 if not r:raise ValueError('가이드 결과 검사가 없습니다.')
 if not f['versions'] or r.get('candidate_asset')!=f['versions'][-1]['asset'] or r.get('revision')!=p['revision'] or r.get('guide_signature')!=digest(g):
  raise ValueError('가이드 검사가 오래되었습니다. 다시 검사하세요.')
 if r.get('status')!='passed' or r.get('feet_status')!='passed' or r.get('garment_review',{}).get('token')!=r.get('token'):
  raise ValueError('가이드 미달 또는 옷 윤곽 시각 미검증: 채택할 수 없습니다.')


def require_all_reports(p):
 for key in p.get('result_guides',{}):
  i=int(key)
  if not 0<=i<len(p['frames']):raise ValueError('가이드 대상 프레임이 없습니다.')
  require_report(p,i)


def foot_metrics(im,box):
 x0,y0,x1,y1=box
 a=im.getchannel('A').crop(box).point(lambda a:255 if a>=128 else 0)
 b=a.getbbox()
 if b is None:return None
 l,t,r,bot=b;row=a.crop((0,bot-1,a.width,bot)).getbbox()
 return {'left':x0+l,'right':x0+r-1,'sole_y':y0+bot-1,
         'contact_left':x0+row[0],'contact_right':x0+row[2]-1}


class GuideValidation:
 def __init__(self,repo):self.repo=repo
 def sha(self,asset):return hashlib.sha256(self.repo.asset_path(asset).read_bytes()).hexdigest()
 def configure(self,p,index,criteria):
  if type(index)!=int or not 0<=index<len(p['frames']):raise ValueError('가이드 프레임 번호 오류')
  if not isinstance(criteria,dict) or set(criteria)!={'version','canvas','feet','garment_source'}:raise ValueError('가이드 기준 필드를 확인하세요. 임의 허용오차는 지원하지 않습니다.')
  version=criteria['version'];size=criteria['canvas'];feet=criteria['feet'];source=criteria['garment_source']
  if not isinstance(version,str) or not 1<=len(version)<=120 or not isinstance(source,str) or not 8<=len(source)<=2000:raise ValueError('가이드 버전과 옷 참고 원천을 기록하세요.')
  if not isinstance(size,list) or len(size)!=2 or any(type(v)!=int or not 16<=v<=4096 for v in size):raise ValueError('가이드 해상도 오류')
  if not isinstance(feet,list) or len(feet)!=2:raise ValueError('양발 검사 영역 두 개가 필요합니다.')
  ref=p['refs'].get('first')
  if not ref:raise ValueError('승인 기준 첫 원화가 필요합니다.')
  regions=[]
  with Image.open(self.repo.asset_path(ref)) as raw:
   im=raw.convert('RGBA')
   if list(im.size)!=size:raise ValueError('기준 원화와 가이드 해상도가 다릅니다.')
   for foot in feet:
    if not isinstance(foot,dict) or set(foot)!={'name','region'}:raise ValueError('발 영역 형식 오류')
    name=foot['name'];box=foot['region']
    if name not in ('front','rear') or any(v['name']==name for v in regions):raise ValueError('앞발·뒷발 이름을 구분하세요.')
    if not isinstance(box,list) or len(box)!=4 or any(type(v)!=int for v in box):raise ValueError('발 영역 좌표 오류')
    x0,y0,x1,y1=box
    if not 0<=x0<x1<=size[0] or not 0<=y0<y1<=size[1]:raise ValueError('발 영역이 캔버스를 벗어났습니다.')
    expected=foot_metrics(im,box)
    if expected is None:raise ValueError('기준 영역에 발 실루엣이 없습니다.')
    regions.append(dict(name=name,region=box,expected=expected))
  g=dict(schema=1,version=version,canvas=size,reference_asset=ref,reference_sha256=self.sha(ref),alpha_threshold=128,tolerance_px=0,feet=regions,garment_source=source,garment_mode='manual-unverified')
  p.setdefault('result_guides',{})[str(index)]=g;p['revision']+=1
  p['frames'][index]['approved']=False;p['frames'][index].pop('result_guide_report',None)
  for key in ('clip_approval','export','review_export'):p.pop(key,None)
 def context(self,p,index):
  if type(index)!=int or not 0<=index<len(p['frames']):raise ValueError('가이드 프레임 번호 오류')
  g=guide_for(p,index)
  if not g:raise ValueError('가이드 기준을 먼저 등록하세요.')
  f=p['frames'][index]
  if not f['versions']:raise ValueError('검사할 후보가 없습니다.')
  ref=g['reference_asset']
  if p['refs'].get('first')!=ref or self.sha(ref)!=g['reference_sha256']:raise ValueError('가이드 기준 원화가 바뀌었습니다. 기준을 다시 등록하세요.')
  asset=f['versions'][-1]['asset']
  return dict(validation_version=1,candidate_asset=asset,candidate_sha256=self.sha(asset),reference_sha256=g['reference_sha256'],guide_signature=digest(g),revision=p['revision'])
 def current(self,p,index):
  r=p['frames'][index].get('result_guide_report')
  if not r:raise ValueError('가이드 결과 검사가 없습니다.')
  c=self.context(p,index)
  if any(r.get(k)!=v for k,v in c.items()) or r.get('token')!=digest(c):raise ValueError('가이드 검사가 오래되었습니다. 다시 검사하세요.')
  return r
 def inspect(self,p,index):
  c=self.context(p,index);g=guide_for(p,index);r=dict(c,token=digest(c),status='unverified',feet_status='passed',garment_status='unverified',feet=[],reasons=['옷 윤곽은 검토선입니다. 정확한 자동 판정이 없어 명시적 시각 확인이 필요합니다.'])
  with Image.open(self.repo.asset_path(c['candidate_asset'])) as raw:
   im=raw.convert('RGBA');r['actual_canvas']=list(im.size)
   if list(im.size)!=g['canvas']:
    r.update(status='failed',feet_status='failed');r['reasons'].append('후보 해상도와 기준 캔버스가 다릅니다. 자동 정렬하지 않습니다.')
   else:
    with Image.open(self.repo.asset_path(g['reference_asset'])) as reference:
     for foot in g['feet']:
      box=foot['region'];measured=foot_metrics(im,box)
      expected_mask=reference.convert('RGBA').getchannel('A').crop(box).point(lambda a:255 if a>=128 else 0)
      actual_mask=im.getchannel('A').crop(box).point(lambda a:255 if a>=128 else 0)
      changed=ImageChops.difference(expected_mask,actual_mask).histogram()[255]
      ok=measured==foot['expected'] and changed==0
      r['feet'].append(dict(name=foot['name'],expected=foot['expected'],measured=measured,silhouette_difference_pixels=changed,passed=ok))
      if not ok:r.update(status='failed',feet_status='failed');r['reasons'].append(('앞발' if foot['name']=='front' else '뒷발')+' 좌표·실루엣 미달 (허용오차 0px).')
  p['frames'][index]['result_guide_report']=r;p['frames'][index]['approved']=False;p.pop('clip_approval',None);p.pop('export',None)
  return r
 def review(self,p,index,token,note,confirmed):
  r=self.current(p,index)
  if token!=r['token'] or confirmed is not True:raise ValueError('현재 후보의 옷 윤곽 시각 확인이 필요합니다.')
  if r['feet_status']!='passed':raise ValueError('발 미달 결과는 수동 확인으로 통과시킬 수 없습니다.')
  if not isinstance(note,str) or not 12<=len(note.strip())<=2000:raise ValueError('옷 윤곽·몸 회전·바람을 확인한 근거를 기록하세요.')
  r['garment_review']=dict(token=token,note=note.strip(),scope='explicit visual confirmation, not automatic garment measurement')
  r.update(garment_status='manual-confirmed',status='passed',reasons=[])
 def require(self,p,index):
  if guide_for(p,index) is None:return
  self.current(p,index);require_report(p,index)
 def require_all(self,p):
  require_all_reports(p)
  for key in p.get('result_guides',{}):self.require(p,int(key))
 def summary(self,p,index):
  if guide_for(p,index) is None:return None
  try:
   r=self.current(p,index)
   can_adopt=False
   if r['status']=='passed':require_report(p,index);can_adopt=True
   return dict(status=r['status'],current=True,can_adopt=can_adopt,reasons=r['reasons'])
  except (ValueError,OSError) as e:return dict(status='unverified',current=False,can_adopt=False,reasons=[str(e)])
