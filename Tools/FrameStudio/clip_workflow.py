"""Clip ingestion and explicit direction identity; no UI or approval ownership."""
import base64, copy, io
from PIL import Image
from domain import set_schedule, frame_at
from imaging import inspect_png

def decode_png(encoded, expected=None, actor=False):
    try:
        content=base64.b64decode(encoded,validate=True)
        if len(content)>24*1024*1024:raise ValueError('PNG는 24MB 이하입니다.')
        with Image.open(io.BytesIO(content)) as image:
            if image.format!='PNG' or image.n_frames!=1 or min(image.size)<1 or max(image.size)>4096:raise ValueError('4096px 이하 정지 PNG를 사용하세요.')
            image.load();size=image.size
            if expected and size!=tuple(expected):raise ValueError('원화와 FX의 캔버스 크기를 맞추세요.')
            if image.convert('RGBA').getchannel('A').getextrema()[0]==255:raise ValueError('실제 투명 배경 PNG가 필요합니다.')
        checks=inspect_png(content,expected) if actor else None
        if checks and checks['blocking']:raise ValueError(', '.join(checks['blocking']))
        return content,size,checks
    except (TypeError,KeyError,OSError) as error:raise ValueError('PNG 데이터를 확인하세요.') from error

class ClipImport:
    def __init__(self, repo, add_frame):self.repo=repo;self.add_frame=add_frame
    def replace(self,p,data):
        rows=data.get('frames')
        if not isinstance(rows,list) or not 2<=len(rows)<=48:raise ValueError('프레임은 2~48장입니다.')
        if any(not isinstance(row,dict) for row in rows):raise ValueError('프레임 목록을 확인하세요.')
        if any(not isinstance(row.get('png'),str) or (row.get('fx_png') is not None and not isinstance(row['fx_png'],str)) for row in rows):raise ValueError('원화와 FX PNG 데이터를 확인하세요.')
        if sum(len(row.get('png',''))+len(row.get('fx_png') or '') for row in rows)>32*1024*1024:raise ValueError('일괄 가져오기는 전체 PNG 24MB 이하입니다.')
        candidate=copy.deepcopy(p)
        set_schedule(candidate,rows) # Validate the complete schedule before writing assets.
        expected=None
        if p['refs'].get('first'):
            with Image.open(self.repo.asset_path(p['refs']['first'])) as image:expected=image.size
        decoded=[]
        for row in rows:
            content,size,checks=decode_png(row.get('png'),expected,actor=True)
            expected=size
            fx=decode_png(row['fx_png'],size)[0] if row.get('fx_png') else None
            decoded.append((content,checks,fx))
        if any(f['versions'] for f in p['frames']):candidate.setdefault('clip_history',[]).append(copy.deepcopy(p['frames']))
        for i,(content,checks,fx) in enumerate(decoded):
            self.add_frame(candidate,i,self.repo.put_asset(content),checks,'imported_clip')
            asset=self.repo.put_asset(fx) if fx else None
            candidate['frames'][i].update(fx_asset=asset,fx_versions=[asset])
        p.clear();p.update(candidate)
    def set_fx(self,p,data):
        f=frame_at(p,data.get('index'))
        if not f['versions']:raise ValueError('원화를 먼저 등록하세요.')
        with Image.open(self.repo.asset_path(f['versions'][-1]['asset'])) as image:size=image.size
        content=decode_png(data['png'],size)[0] if data.get('png') else None
        asset=self.repo.put_asset(content) if content else None
        f.setdefault('fx_versions',[f.get('fx_asset')]).append(asset);f['fx_asset']=asset
        p['revision']+=1

def set_clip_identity(p,data):
    identity={}
    for key in ('character','motion'):
        value=data.get(key)
        if not isinstance(value,str) or not value.strip() or len(value)>100:raise ValueError('캐릭터와 동작을 1~100자로 입력하세요.')
        identity[key]=value.strip()
    direction=data.get('direction')
    if type(direction)!=int or direction not in (1,2,3,4):raise ValueError('게임 방향은 1~4번입니다.')
    identity['direction']=direction
    if p.get('clip_identity')!=identity:p['clip_identity']=identity;p['revision']+=1
