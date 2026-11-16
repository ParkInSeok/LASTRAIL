"""Paired surface evidence, bound to actual composited pixels; never automatic art approval."""
import copy
import hashlib
import json
import math

FIELDS=('밝은 면·중간 면·그림자 위치', '명암 경계 폭·부드러움', '겹침·접촉 그림자', '재질 광택·보호 선화')


def basis(p,index):
    flow=p['production_flow'];frame=flow.get('frames',{}).get(str(index),{})
    assets={slot:(frame.get(slot) or [{}])[-1].get('asset') for slot in ('line','flat','shadow','composite')}
    assets['target']=(flow['references'].get('reference_color') or [{}])[-1].get('asset')
    return hashlib.sha256(json.dumps(assets,sort_keys=True).encode()).hexdigest()


def status(p,indices,current):
    flow=p['production_flow'];rows=[]
    for index in indices:
        ready=all(current(p,slot,index) for slot in ('line','flat','shadow','composite'))
        frame=flow.get('frames',{}).get(str(index),{})
        ready=ready and all(frame.get(slot) for slot in ('line','flat','shadow','composite'))
        expected=basis(p,index)
        for region in flow['config']['regions']:
            evidence=flow.get('surface_reviews',{}).get(str(index),{}).get(region['id'],{})
            valid=bool(ready and evidence.get('basis')==expected)
            rows.append({'index':index,'region':region['id'],'name':region['name'],
                         'basis':expected,'ready':bool(ready),'current':valid,
                         'passed':valid and evidence.get('passed') is True,
                         'evidence':evidence})
    return {'rows':rows,'passed':bool(rows) and all(row['passed'] for row in rows),'fields':FIELDS}


def record(p,data,current):
    import approval_gate
    gate=approval_gate.state(p,current);stage=gate['active']
    if stage is None or not gate['stages'][stage].get('surface_review'):
        raise ValueError('명암 면 검수 단계에서만 기록할 수 있습니다.')
    index=data.get('index');region=data.get('region')
    row=next((r for r in gate['stages'][stage]['surface_review']['rows'] if r['index']==index and r['region']==region),None)
    if type(index)!=int or not row or not row['ready'] or data.get('basis')!=row['basis']:
        raise ValueError('현재 프레임의 명암 합성과 목표 원본을 다시 확인하세요.')
    for key in ('candidate_box','target_box'):
        box=data.get(key)
        if not isinstance(box,list) or len(box)!=4 or any(type(v) not in (int,float) or not math.isfinite(v) or not 0<=v<=1 for v in box) or not (box[0]<box[2] and box[1]<box[3]):
            raise ValueError('양쪽 이미지에서 비교할 동일 부위 영역을 선택하세요.')
    observations=data.get('observations')
    if type(data.get('passed'))!=bool or not isinstance(observations,dict) or any(not isinstance(observations.get(k),str) or not observations[k].strip() or len(observations[k])>4000 for k in FIELDS):
        raise ValueError('각 명암 면·경계의 실제 관찰과 판정이 필요합니다. 해당 없음도 이유를 적으세요.')
    evidence={k:copy.deepcopy(data[k]) for k in ('basis','candidate_box','target_box','observations','passed')}
    flow=p['production_flow'];records=flow.setdefault('surface_reviews',{}).setdefault(str(index),{})
    if region in records:flow.setdefault('history',[]).append({'surface_review':records[region],'index':index,'region':region})
    records[region]=evidence
