"""Adapts the shared VideoService to separate, unapproved twelve-frame actor candidates."""
import copy,hashlib,io,json,re,threading,time
from PIL import Image
from domain import new_project,set_schedule
import production_method
from imaging import inspect_png,export_clip
from local_motion import validate_request,prepare_reference,extract_twelve

class LocalMotionService:
    def __init__(self,studio,cutscene,video):
        self.studio=studio;self.repo=studio.repo;self.cutscene=cutscene;self.video=video
        self.lock=threading.RLock();self.jobs={};self.threads={};self.events={}
    def folder(self,pid):
        self.repo.project_path(pid)
        path=self.repo.root/'local-motion'/pid;path.mkdir(parents=True,exist_ok=True);return path
    def _write(self,pid,job):
        path=self.folder(pid)/'latest.json';tmp=path.with_suffix('.tmp')
        tmp.write_text(json.dumps(job,ensure_ascii=False,indent=2),encoding='utf-8');tmp.replace(path)
    def _update(self,pid,job,**values):
        with self.lock:job.update(values);self._write(pid,job)
    def view(self,pid):
        with self.lock,self.repo.lock:
            p=self.repo.get(pid);refs=[]
            if p['refs'].get('first'):refs.append(dict(key='first',label='등록 첫 원화',asset=p['refs']['first']))
            for i,f in enumerate(p['frames']):
                if f['versions']:refs.append(dict(key=str(i),label=f'{i+1}번 프레임'+(' · 승인' if f['approved'] else ' · 승인 확인 필요'),asset=f['versions'][-1]['asset']))
            if pid in self.jobs:job=copy.deepcopy(self.jobs[pid])
            else:
                path=self.folder(pid)/'latest.json';job=json.loads(path.read_text('utf-8')) if path.exists() else None
                if job and job['status'] in ('running','queued','cancelling','extracting'):
                    job.update(status='interrupted',message='서버가 재시작됐습니다. 원본과 생성 기록은 보존됩니다.')
            return dict(id=pid,name=p['name'],revision=p['revision'],references=refs,job=job,production_method=p.get('production_method'))
    def start(self,pid,data):
        request=validate_request(data)
        with self.lock:
            if pid in self.threads and self.threads[pid].is_alive():raise ValueError('이 원화의 생성 작업이 진행 중입니다.')
            with self.repo.lock:
                source=self.repo.get(pid)
                production_method.require(source,'ltx')
                if data.get('revision',source['revision'])!=source['revision']:raise ValueError('원화가 변경됐습니다. 다시 선택하세요.')
                ref=request['reference']
                if ref=='first':asset=source['refs'].get('first')
                elif ref.isdecimal() and int(ref)<len(source['frames']):
                    versions=source['frames'][int(ref)]['versions'];asset=versions[-1]['asset'] if versions else None
                else:asset=None
                if not asset:raise ValueError('선택한 원화가 없습니다.')
                raw=self.repo.asset_path(asset).read_bytes()
                candidate=new_project((source['name'][:65]+' · LTX 12프레임 후보'))
                production_method.select(candidate,'ltx')
                candidate['refs']={'first':asset};candidate['motion']=request['prompt'];candidate['loop']=False
                candidate['local_motion_source']=dict(project=pid,revision=source['revision'],asset=asset,
                    sha256=hashlib.sha256(raw).hexdigest(),approval_confirmation=True,reference=ref)
                self.repo.save(candidate)
            cid=candidate['id'];work=self.folder(pid)/cid;work.mkdir()
            job=dict(status='queued',candidate_id=cid,source=candidate['local_motion_source'],request=request,
                     message='원화와 프롬프트 준비',created=time.time(),preview=None)
            self.jobs[pid]=job;event=threading.Event();self.events[pid]=event;self._write(pid,job)
            try:
                prep=prepare_reference(self.repo.asset_path(asset),work/'input.png',actor_fit=request['profile'] in ('idle_reference','wind_reference'));job['preparation']=prep
                with (work/'input.png').open('rb') as stream:
                    media=self.cutscene.import_media(cid,stream,(work/'input.png').stat().st_size,'motion-reference.png')
                # The existing VideoService owns inference, GPU busy checks, cancellation and model selection.
                video=self.video.start(cid,dict(asset=media['id'],prompt=request['prompt'],seed=request['seed'],profile=request['profile']))
                self._update(pid,job,status='running',video_id=video['id'],message='LTX 동작 생성 중')
                thread=threading.Thread(target=self._watch,args=(pid,job,event,work),daemon=True)
                self.threads[pid]=thread;thread.start()
            except Exception as error:
                self._update(pid,job,status='failed',message=str(error));raise
            return copy.deepcopy(job)
    def _watch(self,pid,job,event,work):
        try:
            while not event.is_set():
                video=self.video.view(job['candidate_id'])
                if not video or video['id']!=job['video_id']:raise ValueError('생성 작업 연결이 바뀌었습니다.')
                raw_video=self.cutscene.folder(job['candidate_id'])/'ai-video'/job['video_id']/'generated.webp'
                # Frame import does not depend on the optional MP4 conversion.
                recover_frames=video['status']=='failed' and raw_video.is_file()
                if video['status']=='complete' or recover_frames:
                    with self.lock:
                        if event.is_set():return
                        if recover_frames:
                            with Image.open(raw_video) as generated:
                                if generated.n_frames!=video['request']['frames'] or generated.size!=(video['request']['width'],video['request']['height']):
                                    raise ValueError('복구할 생성 영상의 규격이 요청과 다릅니다.')
                            job['video_warning']=video.get('message','공유 영상 후처리 실패')
                        self._update(pid,job,status='extracting',message='12프레임 검수 후보 저장 중')
                        self._finish(pid,job,video,work)
                    return
                if video['status'] in ('failed','cancelled','interrupted'):
                    self._update(pid,job,status=video['status'],message=video.get('message','생성 중단'));return
                event.wait(.5)
        except Exception as error:self._update(pid,job,status='failed',message=str(error))
    def _finish(self,pid,job,video,work):
        if not re.fullmatch('[0-9a-f]{32}',video['id']):raise ValueError('잘못된 생성 ID')
        source=self.cutscene.folder(job['candidate_id'])/'ai-video'/video['id']/'generated.webp'
        result=extract_twelve(source,work/'frames');indices=result['source_indices']
        fps=video['request']['fps'];starts=[round(i/fps*100)*10 for i in indices]
        end=round(result['source_frames']/fps*100)*10
        durations=[starts[i+1]-starts[i] for i in range(11)]+[max(20,end-starts[-1])]
        exports=export_clip(result['paths'],durations,work/'review')
        with self.repo.lock:
            p=self.repo.get(job['candidate_id'])
            set_schedule(p,[dict(duration_ms=ms,phase=f'생성 영상 {idx+1}번 → 후보 {i+1}번',kind='key' if i in (0,5,11) else 'between') for i,(idx,ms) in enumerate(zip(indices,durations))])
            for i,path in enumerate(result['paths']):
                raw=path.read_bytes();checks=inspect_png(raw)
                checks['warnings'].append('LTX 저해상도 영상 샘플 · 원화 일관성/전환/alpha 미승인')
                self.studio.add_frame(p,i,self.repo.put_asset(raw),checks,'local_ltx_video_sample')
            p['local_motion_result']=dict(source_video_id=video['id'],source_frames=result['source_frames'],
                source_indices=indices,source_fps=fps,durations_ms=durations,alpha_verified=False,
                source_video=str(source.relative_to(self.repo.root)),model=video['request'].get('model'),
                method=result['method'],profile=video['request'].get('profile','standard'),review_only=True)
            self.repo.save(p)
            preview=self.repo.put_asset((work/'review/preview.gif').read_bytes(),'gif')
            sheet=self.repo.put_asset((work/'review/sheet.png').read_bytes())
            archive=self.repo.put_asset((work/'review/delivery.zip').read_bytes(),'zip')
        self._update(pid,job,status='complete',message='12프레임 후보 저장 완료 · 미술/투명도/연결 검수 필요',
            preview=preview,sheet=sheet,archive=archive,result=p['local_motion_result'],finished=time.time())
        if job.get('video_warning'):
            self._update(pid,job,message='12프레임 후보 저장 완료 · 공유 MP4 변환 실패(원영상 보존) · 미술/투명도 미승인')
    def cancel(self,pid):
        with self.lock:
            job=self.jobs.get(pid)
            if job and job['status'] in ('running','queued','cancelling'):
                self.events[pid].set();self.video.cancel(job['candidate_id'])
                self._update(pid,job,status='cancelled',message='후보 등록 중단 · 실행 중 GPU 계산은 끝날 수 있습니다.')
            return self.view(pid)['job']
    def close(self):
        with self.lock:
            for event in self.events.values():event.set()
        for thread in list(self.threads.values()):thread.join(timeout=3)
