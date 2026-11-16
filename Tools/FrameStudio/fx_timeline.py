"""Optional clip-owned FX exposures, independent of actor exposures."""
from PIL import Image
from clip_workflow import decode_png

def stage_at(project, time_ms):
    timeline=project.get('fx_timeline')
    if not timeline:return None
    return next((row for row in reversed(timeline['stages']) if row['start_ms']<=time_ms),None)

class FxTimeline:
    def __init__(self,repo):self.repo=repo
    def configure(self,p,data):
        if 'stages' not in data:raise ValueError('FX 시간표가 필요합니다.')
        rows=data['stages']
        if rows is None:
            if 'fx_timeline' in p:
                p.setdefault('fx_timeline_history',[]).append(p.pop('fx_timeline'));p['revision']+=1
            return
        if not isinstance(rows,list) or not 1<=len(rows)<=48:raise ValueError('FX 단계는 1~48개입니다.')
        if not p['frames'] or any(not f['versions'] for f in p['frames']):raise ValueError('원화를 먼저 등록하세요.')
        threshold=data.get('occlude_actor_alpha')
        if threshold is not None and (type(threshold)!=int or not 1<=threshold<=255):raise ValueError('가림 alpha는 1~255입니다.')
        with Image.open(self.repo.asset_path(p['frames'][0]['versions'][-1]['asset'])) as im:size=im.size
        total=sum(f['duration_ms'] for f in p['frames']);previous=-1;decoded=[];bytes_total=0
        owned={r['asset'] for r in p.get('fx_timeline',{}).get('stages',[])}
        for row in rows:
            if not isinstance(row,dict):raise ValueError('FX 단계를 확인하세요.')
            start=row.get('start_ms');label=row.get('label','')
            if type(start)!=int or start%10 or not previous<start<total or (previous==-1 and start!=0):raise ValueError('FX 시작은 0부터 오름차순 10ms 단위이며 클립 안에 있어야 합니다.')
            if not isinstance(label,str) or len(label)>200:raise ValueError('FX 단계 이름은 200자 이하입니다.')
            asset=row.get('asset');content=None
            if row.get('png') is not None:
                content=decode_png(row['png'],size)[0];bytes_total+=len(content)
                if bytes_total>24*1024*1024:raise ValueError('FX 전체는 24MB 이하입니다.')
            elif asset not in owned:raise ValueError('이 시간표의 FX 자산 또는 PNG를 지정하세요.')
            decoded.append((start,label,asset,content));previous=start
        stages=[dict(start_ms=start,label=label,asset=self.repo.put_asset(content) if content is not None else asset) for start,label,asset,content in decoded]
        if p.get('fx_timeline'):p.setdefault('fx_timeline_history',[]).append(p['fx_timeline'])
        p['fx_timeline']=dict(stages=stages,occlude_actor_alpha=threshold);p['revision']+=1
