"""Persistent independent drone parts and event timing; no actor mutations."""
import copy
from clip_workflow import decode_png

def configure_companion(repo,p,data):
    if p.get('clip_identity',{}).get('direction')!=2:
        raise ValueError('현재 드론 리그는 2번 방향 전용입니다.')
    times=data.get('timing',{})
    keys=('command','charge','fire','end')
    values=[times.get(k) for k in keys]
    total=sum(f['duration_ms'] for f in p['frames'])
    if any(type(v)!=int for v in values) or not 0<=values[0]<values[1]<values[2]<values[3]<=total:
        raise ValueError('명령·충전·발사·종료 시간을 순서대로 지정하세요.')
    parts=data.get('parts',{})
    if set(parts)!=set(('body','head','left','right')):raise ValueError('드론 독립 파츠 4개가 필요합니다.')
    decoded={k:decode_png(v)[0] for k,v in parts.items()}
    previous=p.get('companion')
    if previous:p.setdefault('companion_history',[]).append(copy.deepcopy(previous))
    p['companion']={'kind':'eve-drone-d2-v1','timing':dict(zip(keys,values)),
                    'parts':{k:repo.put_asset(v) for k,v in decoded.items()},'source':str(data.get('source',''))[:1000]}
    p['revision']+=1
