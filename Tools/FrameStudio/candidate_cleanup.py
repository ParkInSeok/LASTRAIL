"""Detach explicitly selected failed first-shading versions after replacement review."""
import copy
import hashlib
import json
import approval_gate


def discard_failed_versions(repo,p,data,current):
    gate=approval_gate.state(p,current);note=data.get('note')
    if gate['active']!=2 or data.get('index')!=0 or type(data.get('index'))!=int or data.get('confirmed') is not True or not isinstance(note,str) or not note.strip() or len(note)>4000:
        raise ValueError('첫 명암 후보의 명시적 폐기 지시와 사유가 필요합니다.')
    review=gate['stages'][2].get('surface_review')
    if not review or not review['passed']:
        raise ValueError('개선된 현재 명암 면·경계를 먼저 검수하세요.')
    assets=data.get('assets')
    if not isinstance(assets,list) or not assets or any(not isinstance(a,str) for a in assets) or len(set(assets))!=len(assets):
        raise ValueError('폐기할 이전 후보 자산 목록을 지정하세요.')
    frame=p['production_flow']['frames']['0']
    previous={v['asset'] for slot in ('shadow','composite') for v in frame.get(slot,[])[:-1]}
    latest={v[-1]['asset'] for v in frame.values() if isinstance(v,list) and v}
    if not set(assets)<=previous or set(assets)&latest:
        raise ValueError('현재 결과·선화·밑색은 폐기할 수 없습니다.')
    proposed=copy.deepcopy(p)
    for slot in ('shadow','composite'):
        proposed['production_flow']['frames']['0'][slot]=[v for v in frame[slot] if v['asset'] not in assets]
    for request in proposed['production_flow'].get('requests',[]):
        if request.get('result_asset') in assets:
            asset=request.pop('result_asset')
            request['discarded_result_sha256']=hashlib.sha256(repo.asset_path(asset).read_bytes()).hexdigest()
            request['status']='result_discarded'
    def referenced(value):
        if isinstance(value,str):return value in assets
        if isinstance(value,list):return any(referenced(v) for v in value)
        if isinstance(value,dict):return any(referenced(v) for v in value.values())
        return False
    if referenced(proposed):raise ValueError('다른 현재/이력 연결에서 사용하는 후보입니다.')
    for path in (repo.root/'projects').glob('*.json'):
        if path!=repo.project_path(p['id']) and referenced(json.loads(path.read_text(encoding='utf-8'))):
            raise ValueError('다른 프로젝트에서 사용하는 자산은 폐기할 수 없습니다.')
    hashes=[hashlib.sha256(repo.asset_path(a).read_bytes()).hexdigest() for a in assets]
    proposed['production_flow'].setdefault('history',[]).append({'discarded_candidate_sha256':hashes,'note':note.strip(),'count':len(assets)})
    # Save the detached project via Studio first; the explicit caller may then delete unreferenced files.
    p['production_flow']=proposed['production_flow']
