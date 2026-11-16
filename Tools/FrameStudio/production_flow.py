"""Staged authoring workspace. Studio owns persistence; this use case owns its contract."""
import copy, hashlib, io, json, re, uuid
from pathlib import Path
from PIL import Image, ImageChops, ImageColor, ImageDraw
from clip_workflow import decode_png
from wind_plan import request_wind
import approval_gate
import shading_review
import line_protection
from candidate_cleanup import discard_failed_versions

STAGES=('rough','line','regions','flat','shadow','gloss','texture','fx','composite')
LABELS=dict(zip(STAGES,('동작 러프','동작 선화','부위 지도','밑색','명암','광택','질감','독립 효과','최종 합성')))
REFS=('reference_line','reference_color')
PREVIOUS={'rough':(), 'line':(), 'regions':('line',), 'flat':('line',),
          'shadow':('line','flat'), 'gloss':('line','flat','shadow'), 'texture':('line','flat','shadow'),
          'fx':('line',), 'composite':('line','flat','shadow')}

def digest(value):
    return hashlib.sha256(json.dumps(value,sort_keys=True,ensure_ascii=False).encode()).hexdigest()

def text(value,label,limit=4000,optional=False):
    if not isinstance(value,str) or len(value)>limit or (not optional and not value.strip()):
        raise ValueError(label+'을 확인하세요.')
    return value.strip()

def config_checked(raw):
    if not isinstance(raw,dict):raise ValueError('제작 기준이 필요합니다.')
    result={k:text(raw.get(k),k) for k in ('lighting','motion')}
    if 'line_overlay' in raw:
        if type(raw['line_overlay'])!=bool:raise ValueError('선화 합성 여부는 켜기/끄기로 지정하세요.')
        result['line_overlay']=raw['line_overlay']
    if 'separate_line_protection' in raw:
        if type(raw['separate_line_protection'])!=bool:raise ValueError('선 보호 분리 설정 오류')
        result['separate_line_protection']=raw['separate_line_protection']
        if raw['separate_line_protection'] and raw.get('line_overlay',True):raise ValueError('분리 보호에서는 선화 추가 합성을 끄세요. 선은 보호 지도에서 합성합니다.')
    rows=raw.get('regions')
    if not isinstance(rows,list) or not 1<=len(rows)<=100:raise ValueError('부위는 1~100개입니다.')
    regions=[]; ids=set()
    for row in rows:
        if not isinstance(row,dict):raise ValueError('부위 형식 오류')
        rid=text(row.get('id'),'부위 ID',60)
        if not re.fullmatch('[a-zA-Z][a-zA-Z0-9_-]*',rid) or rid in ids:raise ValueError('부위 ID 중복/형식 오류')
        ids.add(rid)
        region={'id':rid,'name':text(row.get('name'),'부위 이름',100),
                'parent':text(row.get('parent',''),'상위 부위',60,True),
                'material':text(row.get('material'),'재질',100)}
        for role in ('base','light','shadow'):
            color=row.get(role)
            if not isinstance(color,str) or not re.fullmatch('#[0-9a-fA-F]{6}',color):raise ValueError('팔레트는 #RRGGBB 형식입니다.')
            region[role]=color.upper()
        ramps=row.get('ramps',{})
        if not isinstance(ramps,dict) or any(k not in ('base','light','shadow') for k in ramps):raise ValueError('색 단계 역할 오류')
        if ramps:
            region['ramps']={}
            for role,colors in ramps.items():
                if not isinstance(colors,list) or not 1<=len(colors)<=16 or any(not isinstance(c,str) or not re.fullmatch('#[0-9a-fA-F]{6}',c) for c in colors):
                    raise ValueError('각 명암 면의 고정 색 단계는 #RRGGBB 1~16개입니다.')
                region['ramps'][role]=[c.upper() for c in colors]
        regions.append(region)
    parents={r['id']:r['parent'] for r in regions}
    for rid in parents:
        seen=set(); current=rid
        while current:
            if current not in ids or current in seen:raise ValueError('부위 연결의 누락/순환을 확인하세요.')
            seen.add(current);current=parents[current]
    result['regions']=regions
    return result

def latest(flow,slot,index=-1):
    versions=flow.get('references',{}).get(slot,[]) if slot in REFS else flow.get('frames',{}).get(str(index),{}).get(slot,[])
    return versions[-1] if versions else None

def dependencies(flow,slot,index):
    required=list(PREVIOUS[slot])
    if slot=='texture' and flow.get('approval_gate',{}).get('sequence_version')==3:required.append('gloss')
    optional={'line':('rough',),'flat':('regions',),'composite':('gloss','texture','fx')}.get(slot,())
    return required+[s for s in optional if latest(flow,s,index)]

def fingerprint(p,slot,index):
    flow=p['production_flow']; refs={k:(latest(flow,k) or {}).get('asset') for k in REFS}
    config={k:v for k,v in flow['config'].items() if k!='line_overlay' or slot in ('shadow','composite')}
    signature={'config':config,'refs':refs,'schedule':[(f['duration_ms'],f['kind'],f['phase']) for f in p['frames']],
               'source_refs':p['refs'],'loop':p.get('loop',True),
               'contract':{k:p.get(k,'') for k in ('identity','motion','hidden')}}
    if slot not in REFS:
        if p.get('wind_settings') is not None:signature['wind_settings']=p['wind_settings']
        actual=[v['asset'] for v in p['frames'][index]['versions'][-1:]]
        baseline=flow.get('source_frames',{}).get(str(index),actual)
        signature['source']=baseline if actual==[flow.get('adopted',{}).get(str(index))] else actual
        signature['plan']=flow.get('plans',{}).get(str(index),{})
        signature['inputs']={s:(latest(flow,s,index) or {}).get('asset') for s in dependencies(flow,slot,index)}
        if flow['config'].get('separate_line_protection') and slot in ('flat','shadow','gloss','texture','composite'):
            signature['line_protection']=line_protection.record(flow,index)
        if slot in ('flat','shadow'):
            signature['masks']={r:{role:items[-1]['asset'] for role,items in roles.items()
                if role!='gloss' and (role in ('base','base_tone'))==(slot=='flat')} for r,roles in flow.get('masks',{}).get(str(index),{}).items()
                if any(role!='gloss' and (role in ('base','base_tone'))==(slot=='flat') for role in roles)}
        if slot=='gloss':
            signature['gloss_masks']={r:{role:items[-1]['asset'] for role,items in roles.items() if role in ('base','gloss')} for r,roles in flow.get('masks',{}).get(str(index),{}).items()}
        if slot=='composite':
            signature['optional']={s:(latest(flow,s,index) or {}).get('asset') for s in ('texture','fx')}
            if latest(flow,'gloss',index):signature['optional']['gloss']=latest(flow,'gloss',index)['asset']
    return digest(signature)

def current(p,slot,index,seen=None):
    item=latest(p['production_flow'],slot,index)
    if not item:return False
    if slot in REFS:return True
    return item['basis']==fingerprint(p,slot,index) and all(current(p,s,index) for s in dependencies(p['production_flow'],slot,index))

def review_basis(p,slot,index,mode):
    value={'own':fingerprint(p,slot,index)}
    if mode!='static' and slot not in REFS:
        n=len(p['frames']);indices=range(n) if mode=='playback' else ((index-1)%n,index,(index+1)%n)
        value['neighbors']=[((latest(p['production_flow'],slot,j) or {}).get('asset'),fingerprint(p,slot,j),current(p,slot,j)) for j in indices]
    return digest(value)

def mask_basis(p,index):
    flow=p['production_flow']
    return digest({'line':(latest(flow,'line',index) or {}).get('asset'),
                   'reference':(latest(flow,'reference_line') or {}).get('asset')})

def status(p):
    flow=p.get('production_flow')
    if not flow:return None
    rows={}
    for i in range(len(p['frames'])):
        rows[str(i)]={}
        for slot in STAGES:
            item=latest(flow,slot,i)
            valid=current(p,slot,i)
            rows[str(i)][slot]={'current':valid,'asset':item['asset'] if item else None,
                'review':item.get('review') if valid and item.get('review',{}).get('basis')==review_basis(p,slot,i,item.get('review',{}).get('mode','static')) else None}
    exports={slot:{**item,'current':item['basis']==review_basis(p,slot,0,'playback')}
             for slot,item in flow.get('exports',{}).items()}
    return {'frames':rows,'stages':LABELS,'revision':flow['revision'],'exports':exports,
            'approval_gate':approval_gate.state(p,current) if 'approval_gate' in flow else None}


class ProductionFlow:
    def __init__(self,repo,add_frame):self.repo=repo;self.add_frame=add_frame

    def target(self,p,data):
        slot=data.get('slot'); index=data.get('index',-1)
        if slot not in (*STAGES,*REFS):raise ValueError('제작 단계 오류')
        if slot not in REFS and (type(index)!=int or not 0<=index<len(p['frames'])):raise ValueError('프레임 번호 오류')
        return slot,(-1 if slot in REFS else index)

    def prerequisites(self,p,slot,index):
        flow=p['production_flow']
        if slot in REFS:return
        if any(not latest(flow,s) for s in REFS):raise ValueError('기준 선화와 채색본을 먼저 등록하세요.')
        for s in REFS:
            if latest(flow,s).get('review',{}).get('passed') is False:
                raise ValueError('기준 그림이 재작업 판정입니다. 수정·재검수 후 다음 단계를 진행하세요.')
        for s in dependencies(flow,slot,index):
            if not current(p,s,index):raise ValueError(LABELS[s]+'가 없거나 오래되었습니다.')

    def plan_checked(self,p,raw):
        if not isinstance(raw,dict):raise ValueError('프레임 계획 형식 오류')
        ids={r['id'] for r in p['production_flow']['config']['regions']}
        result={'motion':text(raw.get('motion',''),'동작 계획',4000,True),
                'occlusion':text(raw.get('occlusion',''),'가림/신규 노출',4000,True),'shadows':[]}
        shadows=raw.get('shadows',[])
        if not isinstance(shadows,list) or len(shadows)>100:raise ValueError('명암 관계 개수 오류')
        for row in shadows:
            if not isinstance(row,dict) or row.get('owner') not in ids:raise ValueError('명암을 받는 부위 오류')
            kind=row.get('kind');caster=row.get('caster','')
            if kind not in ('form','fold','contact','cast','fx'):raise ValueError('명암 종류 오류')
            if kind=='cast' and caster not in ids:raise ValueError('그림자를 만드는 부위를 지정하세요.')
            if caster and caster not in ids:raise ValueError('그림자 부위 오류')
            result['shadows'].append({'owner':row['owner'],'kind':kind,'caster':caster,
                                     'note':text(row.get('note'),'명암 변화 근거',2000)})
        return result

    def execute(self,p,data):
        old=p.get('production_flow',{}); expected=data.get('expected_revision')
        if type(data.get('expected_project_revision'))!=int or data['expected_project_revision']!=p['revision']:raise ValueError('원화/시간표가 변경됐습니다. 새로고침 후 다시 시도하세요.')
        if type(expected)!=int or expected!=old.get('revision',0):raise ValueError('작업본이 변경됐습니다. 새로고침 후 다시 시도하세요.')
        op=data.get('op')
        if op=='configure':
            config=config_checked(data.get('config'))
            flow=p.setdefault('production_flow',{'revision':0,'references':{},'frames':{},'plans':{},'requests':[],'history':[]})
            if not old:
                if len(p['frames'])!=12:raise ValueError('단계 제작은 12프레임 시간표가 필요합니다.')
                keys=data.get('key_indices',[4,8])
                if not isinstance(keys,list) or not keys or len(set(keys))!=len(keys) or any(type(i)!=int or not 0<=i<12 for i in keys):raise ValueError('정점 프레임 번호 오류')
                sequence=data.get('sequence_version',2)
                if type(sequence)!=int or sequence not in (1,2,3):raise ValueError('제작 순서 버전 오류')
                if sequence in (2,3) and 0 in keys:raise ValueError('첫 자세 0번은 정점 추가 번호와 분리하세요.')
                flow['approval_gate']={'key_indices':keys,'approvals':{},'sequence_version':sequence}
                if sequence in (2,3):flow['approval_gate']['detail_review_from']=0
                surface_required=data.get('surface_review_required',False)
                if type(surface_required)!=bool:raise ValueError('명암 면 검수 설정 오류')
                if surface_required and sequence not in (2,3):raise ValueError('명암 면 검수는 12·15단계 제작에 적용합니다.')
                if surface_required:flow['approval_gate']['surface_review_required']=True
            elif 'approval_gate' in flow and config!=flow.get('config'):
                # Color/motion contract changes require new key-pose reviews, not a silent advance.
                keep=0 if flow['approval_gate'].get('sequence_version') in (2,3) else 2
                flow['approval_gate']['approvals']={k:v for k,v in flow['approval_gate']['approvals'].items() if int(k)<keep}
            if flow.get('config'):flow['history'].append({'revision':flow['revision'],'config':copy.deepcopy(flow['config'])})
            flow['config']=config
            flow.setdefault('source_frames',{str(i):[v['asset'] for v in f['versions'][-1:]] for i,f in enumerate(p['frames'])})
        else:
            if not old:raise ValueError('제작 기준을 먼저 저장하세요.')
            flow=old
            if op=='approve_stage':approval_gate.approve(p,data,current)
            elif op=='reopen_first_shading':approval_gate.reopen_shading(p,data,current)
            elif op=='check_stage':approval_gate.check_stage(p,data,current)
            elif op=='surface_review':shading_review.record(p,data,current)
            elif op=='discard_failed_versions':discard_failed_versions(self.repo,p,data,current)
            elif op=='frame_plan':
                index=data.get('index')
                if type(index)!=int or not 0<=index<len(p['frames']):raise ValueError('프레임 번호 오류')
                approval_gate.guard(p,'upload','line',index,current)
                plan=self.plan_checked(p,data.get('plan'))
                flow['history'].append({'index':index,'plan':copy.deepcopy(flow['plans'].get(str(index))), 'revision':flow['revision']})
                flow['plans'][str(index)]=plan
            elif op=='mask':self.mask(p,data)
            elif op=='mask_edit':self.mask_edit(p,data)
            elif op=='line_protection':
                _,index=self.target(p,{**data,'slot':'line'})
                approval_gate.guard(p,'upload','line',index,current)
                line_protection.register(self.repo,p,data)
            elif op=='export':
                slot=data.get('slot')
                if slot not in STAGES or not all(current(p,slot,i) for i in range(len(p['frames']))):
                    raise ValueError('모든 프레임에 현재 단계 결과가 필요합니다.')
                from imaging import export_clip
                out=export_clip([self.repo.asset_path(latest(flow,slot,i)['asset']) for i in range(len(p['frames']))],
                    [f['duration_ms'] for f in p['frames']],self.repo.root/'exports'/uuid.uuid4().hex)
                flow.setdefault('exports',{})[slot]={'files':{k:self.repo.put_asset(Path(v).read_bytes(),Path(v).suffix[1:]) for k,v in out.items()},
                    'basis':review_basis(p,slot,0,'playback'),'status':'review-only'}
            elif op in ('upload','request','review','adopt','paint'):
                slot,index=self.target(p,data);self.prerequisites(p,slot,index)
                approval_gate.guard(p,op,slot,index,current)
                if op=='upload':self.upload(p,data,slot,index)
                elif op=='paint':self.paint(p,slot,index)
                elif op=='request':self.request(p,slot,index)
                elif op=='review':
                    item=latest(flow,slot,index)
                    if not item or not current(p,slot,index):raise ValueError('현재 단계 결과를 먼저 등록하세요.')
                    mode=data.get('mode');passed=data.get('passed')
                    if mode not in ('static','transitions','playback') or type(passed)!=bool:raise ValueError('관찰 범위를 선택하세요.')
                    note=text(data.get('note'),'검수 근거')
                    item.setdefault('review_history',[]).append(copy.deepcopy(item.get('review')))
                    item['review']={'mode':mode,'passed':passed,'note':note,'basis':review_basis(p,slot,index,mode),'asset':item['asset']}
                else:
                    if slot!='composite' or not current(p,slot,index):raise ValueError('현재 최종 합성만 후보로 채택할 수 있습니다.')
                    item=latest(flow,slot,index)
                    review=item.get('review',{})
                    if not review.get('passed') or review.get('basis')!=review_basis(p,slot,index,review.get('mode','static')):raise ValueError('현재 결과의 검수 기록을 먼저 남기세요.')
                    from imaging import inspect_png
                    raw=self.repo.asset_path(item['asset']).read_bytes();checks=inspect_png(raw)
                    if checks['blocking']:raise ValueError('최종 PNG 검사: '+', '.join(checks['blocking']))
                    self.add_frame(p,index,item['asset'],checks,'staged_authoring_candidate')
                    flow.setdefault('adopted',{})[str(index)]=item['asset']
                    flow['history'].append({'adopted_asset':item['asset'],'index':index,'review':copy.deepcopy(item['review'])})
            else:raise ValueError('지원하지 않는 제작 작업')
        flow['revision']+=1

    def upload(self,p,data,slot,index):
        flow=p['production_flow']; anchor=latest(flow,'reference_color') or latest(flow,'reference_line')
        request=None
        if data.get('request_id'):
            request=next((r for r in flow['requests'] if r['id']==data['request_id']),None)
            if not request or request['slot']!=slot or request['index']!=index or request['basis']!=fingerprint(p,slot,index):
                raise ValueError('작화 요청의 기준이 변경됐습니다. 결과를 다시 대조하세요.')
        if not anchor and p['refs'].get('first'):anchor={'asset':p['refs']['first']}
        size=None
        if anchor:
            with Image.open(self.repo.asset_path(anchor['asset'])) as im:size=im.size
        raw,_,_=decode_png(data.get('png'),size)
        if slot in REFS or slot in ('line','flat','composite'):
            with Image.open(io.BytesIO(raw)) as im:
                if not im.convert('RGBA').getchannel('A').getbbox():raise ValueError('빈 그림은 이 단계에 등록할 수 없습니다.')
        if slot=='gloss':
            with Image.open(io.BytesIO(raw)) as im:self.gloss_mask(p,index,im.size)
        if flow['config'].get('separate_line_protection') and slot in ('flat','shadow','gloss','texture','composite'):
            with Image.open(io.BytesIO(raw)) as im:layer=line_protection.apply(self.repo,p,index,slot,im.convert('RGBA'))
            out=io.BytesIO();layer.save(out,format='PNG');raw=out.getvalue()
        asset=self.repo.put_asset(raw)
        versions=flow['references'].setdefault(slot,[]) if slot in REFS else flow['frames'].setdefault(str(index),{}).setdefault(slot,[])
        versions.append({'asset':asset,'basis':fingerprint(p,slot,index),'revision':flow['revision']+1})
        if request:request.update(status='imported_candidate',result_asset=asset)

    def request(self,p,slot,index):
        flow=p['production_flow']; refs=[latest(flow,s)['asset'] for s in REFS if latest(flow,s)]
        first_pose_assets={}
        if flow.get('approval_gate',{}).get('sequence_version') in (2,3) and index>0 and slot in ('flat','shadow','gloss','texture','composite'):
            first_pose_assets={stage:latest(flow,stage,0)['asset'] for stage in ('flat','shadow','gloss','texture','composite') if current(p,stage,0)}
            refs+=list(first_pose_assets.values())
        if slot not in REFS:
            refs += [latest(flow,s,index)['asset'] for s in dependencies(flow,slot,index)]
            for j in (index-1,index+1):
                if 0<=j<len(p['frames']) and current(p,slot,j):refs.append(latest(flow,slot,j)['asset'])
        if p['refs'].get('first'):refs.append(p['refs']['first'])
        description=LABELS.get(slot,slot)
        prompt=f'''Create one transparent PNG: {description}. Same canvas and registration as references.
This is a staged external-authoring candidate, not an approved final frame.
Preserve motion, body volume, feet, hand ownership, weapon scale and exposure timing.
For line art: genuine clean lines only, no grayscale conversion or baked shading.
For flat color: use the named material palette. For shadow: draw pose-dependent boundaries,
respect the fixed light, caster/receiver and occlusion; do not shift or blur the first shadow mask.
No arbitrary wrinkles, no geometric warping or frozen motion to hide flicker.
Character: {p['name']}; stage: {slot}; frame: {index+1}.
Character contract: {p.get('identity','')}; hidden surfaces: {p.get('hidden','')}.
Stage palette and lighting: {json.dumps(flow['config'],ensure_ascii=False)}
Frame motion and shadow plan: {json.dumps(flow['plans'].get(str(index),{}),ensure_ascii=False)}
Exposure: {p['frames'][index]['duration_ms'] if index>=0 else 'reference'} ms.
Return only the requested stage; effects stay separate. All correspondence and temporal quality require review.'''
        guide,note=request_wind(p,index) if slot in ('rough','line') else (None,None)
        if note:prompt+=note
        gate=approval_gate.state(p,current) if 'approval_gate' in flow else None
        if gate and gate['active'] is not None:
            prompt+='\nStage authoring and review criteria (do not add approval stages): '+json.dumps(gate['stages'][gate['active']]['criteria'],ensure_ascii=False)
        if slot in ('regions','flat','shadow','gloss','texture','composite'):
            prompt+='\nPreserve approved line geometry and wind pose; apply surface correspondence and color/shadow only. Do not redesign the motion in this stage.'
        if flow.get('approval_gate',{}).get('sequence_version')==3:
            prompt+='\nSeparate stages: shadow = diffuse form/occlusion ONLY, no specular highlights; gloss = independent colored reflection layer clipped to explicit per-region gloss masks; texture = fine material strokes only after gloss approval.'
        if flow['config'].get('separate_line_protection'):
            prompt+='\nLine geometry is immutable. Internal ink RGB/density may change ONLY inside the registered editable ink support; do not move, thicken, erase or add lines. Contours, occlusion junctions, hands and feet remain protected. Density is expressed through ink RGB, never silhouette alpha.'
        if first_pose_assets:
            prompt+='\nUse these approved first-pose layers as the authoritative color-stage reference, ahead of historical reference_color: '+json.dumps(first_pose_assets)
        flow['requests'].append({'id':uuid.uuid4().hex,'slot':slot,'index':index,'prompt':prompt,
            'reference_assets':list(dict.fromkeys(refs)),'basis':fingerprint(p,slot,index),'status':'external_authoring_pending',**({'first_pose_assets':first_pose_assets} if first_pose_assets else {}),**({'wind_guide':guide} if guide is not None else {})})

    def mask(self,p,data):
        _,index=self.target(p,{**data,'slot':'flat'}); self.prerequisites(p,'regions',index)
        approval_gate.guard(p,'mask','gloss' if data.get('role')=='gloss' else 'shadow' if data.get('role') in ('shadow','light','shadow_tone','light_tone','ink_appearance') else 'flat',index,current)
        flow=p['production_flow'];rid=data.get('region');role=data.get('role')
        if rid not in {r['id'] for r in flow['config']['regions']} or role not in ('base','light','shadow','base_tone','light_tone','shadow_tone','gloss','ink_appearance'):
            raise ValueError('부위와 명암 역할을 확인하세요.')
        with Image.open(self.repo.asset_path(latest(flow,'reference_line')['asset'])) as im:size=im.size
        raw,_,_=decode_png(data.get('png'),size)
        with Image.open(io.BytesIO(raw)) as im:
            mask=ImageChops.multiply(im.convert('L'),im.convert('RGBA').getchannel('A'))
            if not (im.convert('RGBA').getchannel('A') if role.endswith('_tone') else mask).getbbox():raise ValueError('마스크에 적용 영역이 없습니다.')
            if role=='ink_appearance':line_protection.appearance_mask(self.repo,p,index,mask.point(lambda x:255 if x else 0))
        versions=flow.setdefault('masks',{}).setdefault(str(index),{}).setdefault(rid,{}).setdefault(role,[])
        versions.append({'asset':self.repo.put_asset(raw),'revision':flow['revision']+1,'basis':mask_basis(p,index)})

    def mask_edit(self,p,data):
        _,index=self.target(p,{**data,'slot':'flat'}); self.prerequisites(p,'regions',index)
        approval_gate.guard(p,'mask_edit','gloss' if data.get('role')=='gloss' else 'shadow' if data.get('role') in ('shadow','light','ink_appearance') else 'flat',index,current)
        flow=p['production_flow'];rid=data.get('region');role=data.get('role');mode=data.get('mode')
        if rid not in {r['id'] for r in flow['config']['regions']} or role not in ('base','light','shadow','gloss','ink_appearance'):
            raise ValueError('부위와 명암 역할을 확인하세요.')
        if mode not in ('add','erase','replace'):raise ValueError('마스크 편집 방식 오류')
        with Image.open(self.repo.asset_path(latest(flow,'reference_line')['asset'])) as im:size=im.size
        points=data.get('points')
        if not isinstance(points,list) or not 3<=len(points)<=2048:raise ValueError('경계점을 3~2048개 지정하세요.')
        for point in points:
            if not isinstance(point,list) or len(point)!=2 or any(type(v)!=int for v in point) or not (0<=point[0]<size[0] and 0<=point[1]<size[1]):
                raise ValueError('경계점은 그림 안의 정수 좌표여야 합니다.')
        polygon=Image.new('L',size);ImageDraw.Draw(polygon).polygon([tuple(v) for v in points],fill=255)
        versions=flow.get('masks',{}).get(str(index),{}).get(rid,{}).get(role,[])
        mask=Image.new('L',size)
        if versions and mode!='replace':
            if versions[-1]['basis']!=mask_basis(p,index):raise ValueError('선화가 변경됐습니다. 경계를 다시 지정하세요.')
            with Image.open(self.repo.asset_path(versions[-1]['asset'])) as im:
                mask=ImageChops.multiply(im.convert('L'),im.convert('RGBA').getchannel('A'))
        mask=polygon if mode=='replace' else ImageChops.lighter(mask,polygon) if mode=='add' else ImageChops.subtract(mask,polygon)
        if role=='ink_appearance':line_protection.appearance_mask(self.repo,p,index,mask)
        out=Image.new('RGBA',size,(255,255,255,0));out.putalpha(mask)
        raw=io.BytesIO();out.save(raw,format='PNG')
        versions=flow.setdefault('masks',{}).setdefault(str(index),{}).setdefault(rid,{}).setdefault(role,[])
        versions.append({'asset':self.repo.put_asset(raw.getvalue()),'revision':flow['revision']+1,'basis':mask_basis(p,index),
                         'edit':{'mode':mode,'points':points}})

    def gloss_mask(self,p,index,size):
        combined=Image.new('L',size)
        for roles in p['production_flow'].get('masks',{}).get(str(index),{}).values():
            if not roles.get('gloss'):continue
            if not roles.get('base'):raise ValueError('광택의 기본 부위 마스크가 필요합니다.')
            masks=[]
            for role in ('base','gloss'):
                item=roles[role][-1]
                if item['basis']!=mask_basis(p,index):raise ValueError('광택 마스크의 선화 기준이 오래되었습니다.')
                with Image.open(self.repo.asset_path(item['asset'])) as im:
                    masks.append(ImageChops.multiply(im.convert('L'),im.convert('RGBA').getchannel('A')))
            combined=ImageChops.lighter(combined,ImageChops.multiply(*masks))
        if not combined.getbbox():raise ValueError('광택 위치 마스크를 먼저 지정하세요.')
        return combined

    def paint(self,p,slot,index):
        if slot not in ('flat','shadow','composite'):raise ValueError('밑색·명암·최종 합성만 조합할 수 있습니다.')
        flow=p['production_flow']
        with Image.open(self.repo.asset_path(latest(flow,'reference_line')['asset'])) as im:canvas=Image.new('RGBA',im.size)
        if slot=='composite':
            flat_alpha=None
            for stage in ('flat','shadow','gloss','texture','line','fx'):
                if stage=='fx' and not flow['config'].get('line_overlay',True) and flat_alpha is not None:canvas.putalpha(flat_alpha)
                if stage=='line' and not flow['config'].get('line_overlay',True):continue
                item=latest(flow,stage,index)
                if item:
                    if not current(p,stage,index):raise ValueError(LABELS[stage]+'가 오래되었습니다.')
                    with Image.open(self.repo.asset_path(item['asset'])) as im:
                        layer=im.convert('RGBA')
                        if stage=='gloss':layer.putalpha(ImageChops.multiply(layer.getchannel('A'),self.gloss_mask(p,index,layer.size)))
                        if stage=='flat':flat_alpha=layer.getchannel('A')
                        canvas=Image.alpha_composite(canvas,layer)
        else:
            masks=flow.get('masks',{}).get(str(index),{});applied=0
            def read_mask(item):
                if item.get('basis')!=mask_basis(p,index):raise ValueError('선화가 변경됐습니다. 부위 마스크를 다시 등록하세요.')
                with Image.open(self.repo.asset_path(item['asset'])) as im:
                    return ImageChops.multiply(im.convert('L'),im.convert('RGBA').getchannel('A'))
            occupied=Image.new('L',canvas.size)
            for region in flow['config']['regions']:
                roles=masks.get(region['id'],{})
                if not roles:continue
                if not roles.get('base'):raise ValueError(region['name']+'의 기본 영역 마스크가 필요합니다.')
                base=read_mask(roles['base'][-1])
                if ImageChops.multiply(occupied,base).getbbox():raise ValueError('기본 부위 마스크가 겹칩니다. 가림 경계를 정리하세요.')
                occupied=ImageChops.lighter(occupied,base)
                for role in (('base',) if slot=='flat' else ('shadow','light')):
                    if not roles.get(role):continue
                    mask=read_mask(roles[role][-1])
                    if role!='base':
                        clip=base if flow['config'].get('line_overlay',True) else base.point(lambda v:255 if v else 0)
                        mask=ImageChops.multiply(mask,clip)
                    colors=region.get('ramps',{}).get(role)
                    if colors:
                        tone=roles.get(role+'_tone')
                        if not tone or tone[-1]['basis']!=mask_basis(p,index):raise ValueError(region['name']+'의 현재 색 단계 지도가 필요합니다.')
                        with Image.open(self.repo.asset_path(tone[-1]['asset'])) as im:
                            if ImageChops.multiply(mask,ImageChops.invert(im.convert('RGBA').getchannel('A'))).getbbox():raise ValueError('색 단계 지도가 적용 면을 모두 덮어야 합니다.')
                            indices=im.convert('L').point(lambda v:round(v*(len(colors)-1)/255))
                        layer=indices.convert('P');palette=[v for c in colors for v in ImageColor.getrgb(c)]
                        layer.putpalette(palette+[0]*(768-len(palette)));layer=layer.convert('RGBA')
                    else:layer=Image.new('RGBA',canvas.size,ImageColor.getrgb(region[role])+(255,))
                    layer.putalpha(mask)
                    canvas=Image.alpha_composite(canvas,layer);applied+=1
            if not applied:raise ValueError('이 단계의 부위 마스크를 먼저 등록하세요.')
        canvas=line_protection.apply(self.repo,p,index,slot,canvas)
        out=io.BytesIO();canvas.save(out,format='PNG')
        versions=flow['frames'].setdefault(str(index),{}).setdefault(slot,[])
        versions.append({'asset':self.repo.put_asset(out.getvalue()),'basis':fingerprint(p,slot,index),
                         'revision':flow['revision']+1,'method':'palette_masks' if slot!='composite' else 'layer_composite'})
