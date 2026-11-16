"""Six user-approved production phases; receipts bind the exact upstream assets."""
import hashlib
import json
import copy
import shading_review
import line_protection

CRITERIA=(
 ('형태·연결·선 두께', '손 구분·그립·발 접지', '승인 원화 비율·카메라'),
 ('부위별 면 대응·색상표', '선화·투명도 보존', '피부·소매·장식 색 누락'),
 ('큰 형태의 명암 면·경계', '겹침·접촉 그림자', '재질별 광택·색 단계', '밑색·선화·손·발 보존'),
 ('가닥 방향·붓결 연결', '옷 재질·얼룩·잔점', '승인 명암 경계·밝기 보존'))

def layers(gate):
    return ('line','flat','shadow','gloss','texture') if gate.get('sequence_version')==3 else ('line','flat','shadow','texture')


def criteria(stage,sequence=2,separate_lines=False):
    rows=CRITERIA
    if sequence==3:
        rows=(*CRITERIA[:2],('큰 형태의 명암 면·경계','겹침·접촉 그림자','광택 없는 형태 명암','밑색·선화·손·발 보존'),
              ('광택 위치·폭·방향·강도·끝 모양','광택 마스크 밖 보존·승인 명암 보존','목표 반사광 배치 비교'),CRITERIA[3])
    return list(rows[stage%len(rows)])+(['형태·선 위치·두께·외곽 투명도 고정','허용 내부선 색·농도 범위 / 손·발·겹침 보호'] if separate_lines else [])+['이전 승인 그림체와 동일 크기 비교']+(['인접 전환·연속 재생·끝 연결'] if stage>=len(rows) else [])

def reopen_shading(p,data,current):
    """Explicit first-pose shading revision; archive downstream candidates, never delete assets."""
    flow=p['production_flow'];gate=flow['approval_gate'];s=state(p,current)
    note=data.get('note')
    if gate.get('sequence_version') not in (2,3) or s['active'] not in range(3,len(layers(gate))+1) or data.get('confirmed') is not True or not isinstance(note,str) or not note.strip() or len(note)>4000:
        raise ValueError('첫 자세 명암 재작업은 해당 단계 사용자 지시와 승인된 명암이 필요합니다.')
    frame=flow['frames']['0']
    flow['history'].append({'reopened_stage':2,'note':data['note'],'approvals':copy.deepcopy(gate['approvals']),
        'archived_candidates':{slot:frame.pop(slot) for slot in ('gloss','texture','composite','fx') if slot in frame}})
    gate['approvals']={k:v for k,v in gate['approvals'].items() if int(k)<2}
    gate['detail_review_from']=2

def check_stage(p,data,current):
    s=state(p,current);stage=s['active'];evidence=data.get('evidence')
    if stage is None or data.get('basis')!=s['basis'] or not s['stages'][stage]['ready']:
        raise ValueError('현재 결과를 먼저 완성하고 다시 확인하세요.')
    if not isinstance(evidence,dict) or any(not isinstance(evidence.get(k),str) or not evidence[k].strip() or len(evidence[k])>4000 for k in criteria(stage,p['production_flow']['approval_gate'].get('sequence_version',2),p['production_flow'].get('config',{}).get('separate_line_protection',False))):
        raise ValueError('세부 검수 항목별 실제 관찰 근거가 필요합니다.')
    p['production_flow']['approval_gate'].setdefault('detail_reviews',{})[str(stage)]={'basis':s['basis'],'evidence':evidence}

LABELS=('첫 자세 미채색','첫 자세 채색','정점 자세 미채색','정점 자세 채색','전체 12장 미채색','전체 12장 채색')
SPLIT_LABELS=tuple(f'{group} {layer}' for group in ('첫 자세','정점 주요 자세','나머지 자세') for layer in ('미채색','밑색','명암','질감'))

def state(p,current):
    flow=p['production_flow'];gate=flow['approval_gate'];keys=gate['key_indices']
    targets=[[('reference_line',-1)],[('reference_color',-1)],
             [('line',i) for i in keys],[('composite',i) for i in keys],
             [('line',i) for i in range(len(p['frames']))],
             [('composite',i) for i in range(len(p['frames']))]]
    labels=LABELS
    if gate.get('sequence_version') in (2,3):
        labels=tuple(f'{group} {layer}' for group in ('첫 자세','정점 주요 자세','나머지 자세') for layer in (('미채색','밑색','명암','광택','질감') if gate.get('sequence_version')==3 else ('미채색','밑색','명암','질감')));targets=[]
        for indices in ([0],keys,[i for i in range(len(p['frames'])) if i not in [0,*keys]]):
            for slot in layers(gate):
                items=[(slot,i) for i in indices]
                if slot=='texture':items += [('composite',i) for i in indices]
                targets.append(items)
        targets[0]=[('reference_line',-1),('reference_color',-1),('line',0)]
    chain=[];rows=[];active=None
    for stage,items in enumerate(targets):
        assets=[];ready=True
        for slot,i in items:
            versions=flow.get('references',{}).get(slot,[]) if i==-1 else flow.get('frames',{}).get(str(i),{}).get(slot,[])
            item=versions[-1] if versions else None
            ready=ready and bool(item) and current(p,slot,i)
            assets.append((slot,i,item['asset'] if item else None))
            if slot=='line' and flow.get('config',{}).get('separate_line_protection'):
                protection=line_protection.record(flow,i)
                ready=ready and bool(protection)
                assets.append(('line_protection',i,protection))
        chain.append(assets)
        basis=hashlib.sha256(json.dumps(chain,sort_keys=True).encode()).hexdigest()
        receipt=gate['approvals'].get(str(stage),{})
        surface=shading_review.status(p,sorted({i for _,i in items}),current) if gate.get('surface_review_required') and gate.get('sequence_version') in (2,3) and stage%len(layers(gate))==2 else None
        if surface is not None and receipt.get('basis')==basis and receipt.get('surface_review',{}).get('passed'):
            surface=copy.deepcopy(receipt['surface_review'])
        passed=active is None and ready and receipt.get('basis')==basis and (surface is None or surface['passed'])
        if not passed and active is None:active=stage
        details=gate.get('detail_reviews',{}).get(str(stage),{})
        rows.append({'label':labels[stage],'ready':bool(ready),'approved':bool(passed),'basis':basis,
            'surface_review':surface,
            'criteria':criteria(stage,gate.get('sequence_version',2),flow.get('config',{}).get('separate_line_protection',False)) if gate.get('sequence_version') in (2,3) else [],
            'detail_required':stage>=gate.get('detail_review_from',99),'detail_passed':details.get('basis')==basis})
    return {'active':active,'label':labels[active] if active is not None else '전체 승인 완료',
            'basis':rows[active]['basis'] if active is not None else None,'stages':rows,'key_indices':keys}

def guard(p,op,slot,index,current):
    if 'approval_gate' not in p.get('production_flow',{}):return
    if op=='adopt':
        if state(p,current)['active'] is not None:raise ValueError('전체 단계 승인 후에만 최종 후보로 채택할 수 있습니다.')
        return
    if op not in ('upload','request','paint','mask','mask_edit'):return
    s=state(p,current);active=s['active'];keys=s['key_indices']
    if p['production_flow']['approval_gate'].get('sequence_version') in (2,3):
        slots=layers(p['production_flow']['approval_gate']);width=len(slots)
        if slot.startswith('reference_'):required=0
        else:
            group=0 if index==0 else width if index in keys else width*2
            layer=slots.index(slot) if slot in slots else {'rough':0,'regions':1,'fx':width-1,'composite':width-1}.get(slot)
            required=group+layer if layer is not None else -1
            if slot=='composite' and active in (group+2,group+width-2):required=active
        if required!=active:
            raise ValueError('단계 잠금: 현재 '+s['label']+' 단계입니다. 승인된 선행 층은 변경할 수 없습니다.')
        return
    if active in (4,5) and index in keys:raise ValueError('승인된 정점 자세는 잠겨 있습니다. 나머지 프레임을 제작하세요.')
    if slot.startswith('reference_'):required=0 if slot=='reference_line' else 1
    elif slot in ('rough','line'):required=2 if index in keys and active is not None and active<=3 else 4
    else:required=3 if index in keys and active is not None and active<=3 else 5
    if required!=active:
        raise ValueError('단계 잠금: 현재 '+s['label']+' 단계입니다. 이전 단계 사용자 승인 없이 진행하거나 승인본을 덮어쓸 수 없습니다.')

def approve(p,data,current):
    s=state(p,current);stage=s['active']
    if stage is None or type(data.get('stage'))!=int or data['stage']!=stage or data.get('basis')!=s['basis']:
        raise ValueError('승인 대상이 변경됐습니다. 현재 결과를 다시 확인하세요.')
    note=data.get('note','')
    if data.get('confirmed') is not True or not isinstance(note,str) or not note.strip() or len(note)>4000:
        raise ValueError('명시적인 사용자 승인과 근거가 필요합니다.')
    if not s['stages'][stage]['ready']:raise ValueError('현재 단계의 모든 결과가 필요합니다.')
    row=s['stages'][stage]
    if row.get('surface_review') and not row['surface_review']['passed']:raise ValueError('모든 부위의 현재 명암 면·경계 비교를 통과해야 합니다.')
    if row['detail_required'] and not row['detail_passed']:raise ValueError('현재 결과의 세부 검수 근거를 먼저 기록하세요.')
    receipt={'basis':s['basis'],'note':note.strip()}
    if row.get('surface_review'):receipt['surface_review']=copy.deepcopy(row['surface_review'])
    p['production_flow']['approval_gate']['approvals'][str(stage)]=receipt

def guard_legacy(p,action):
    if 'approval_gate' not in p.get('production_flow',{}):
        if not p.get('production_sequence_required'):return
        if action in ('production_flow','rename','save','schedule','wind_settings','cancel'):return
        raise ValueError('제작 순서 화면에서 기준과 단계 승인을 먼저 설정하세요.')
    if action not in ('production_flow','rename','cancel','export','battle_export'):
        raise ValueError('단계 잠금 프로젝트입니다. 제작 순서 화면을 사용하세요. 일반 생성·가져오기·보정·기준 변경은 차단됩니다.')
