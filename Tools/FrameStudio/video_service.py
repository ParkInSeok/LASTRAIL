"""Owns local GPU request lifetime; registers output without changing an edit timeline."""
import copy,json,threading,time,uuid,subprocess
from pathlib import Path
from urllib.parse import urlencode
from PIL import Image,ImageSequence
from local_provider import LocalProvider
from cutscene_media import ffmpeg
from video_generation import validate_request,workflow,MODEL,ENCODER,DEFAULT_CANVAS
import production_method

class VideoService:
 def __init__(self,cutscene,root,provider=None):
  self.cutscene=cutscene;self.root=Path(root);self.provider=provider or LocalProvider(root)
  self.lock=threading.RLock();self.jobs={};self.active=None
 def _path(self,pid):return self.cutscene.folder(pid)/'latest-ai-video.json'
 def _write(self,pid,job):
  path=self._path(pid);tmp=path.with_suffix('.tmp');tmp.write_text(json.dumps(job,ensure_ascii=False,indent=2),encoding='utf8');tmp.replace(path)
 def view(self,pid):
  with self.lock:
   if pid in self.jobs:return copy.deepcopy(self.jobs[pid][0])
   path=self._path(pid)
   if not path.exists():return None
   job=json.loads(path.read_text('utf8'))
   if job['status'] in ('queued','running','cancelling'):job.update(status='interrupted',message='서버 재시작으로 결과 확인이 중단됐습니다. 원본은 보존됩니다.')
   return job
 def start(self,pid,data):
  with self.lock:
   if self.active:raise ValueError('진행 중인 로컬 영상 작업이 있습니다.')
   with self.cutscene.repo.lock:production_method.require(self.cutscene.repo.get(pid),'ltx')
   saved=self.cutscene.view(pid);request=validate_request(data,saved['media'])
   source=self.cutscene.media_path(pid,saved['media'][request['asset']]['file']);guide_source=None
   if request.get('middle_guide'):
    guide_source=self.cutscene.media_path(pid,saved['media'][request['middle_guide']['asset']]['file'])
   canvas=(request['width'],request['height'])
   if guide_source is not None or canvas!=DEFAULT_CANVAS:
    for reference in ([source,guide_source] if guide_source is not None else [source]):
     with Image.open(reference) as im:
      if im.n_frames!=1 or im.size!=canvas:raise ValueError(f'시작/중간 이미지는 요청한 {canvas[0]}×{canvas[1]} 정지 캔버스로 준비하세요.')
   self.provider.json('/system_stats')
   models=self.root/'runtime/ComfyUI/models'
   if not (models/'checkpoints'/MODEL).exists() or not (models/'text_encoders'/ENCODER).exists():raise ValueError('Setup-Video.py로 영상 모델을 먼저 설치하세요.')
   q=self.provider.json('/queue')
   if q.get('queue_running') or q.get('queue_pending'):raise ValueError('ComfyUI의 기존 생성이 끝난 뒤 시작하세요.')
   ident=uuid.uuid4().hex;work=self.cutscene.folder(pid)/'ai-video'/ident;work.mkdir(parents=True)
   job=dict(id=ident,status='queued',message='로컬 영상 생성 준비',request=request,media=None,created=time.time())
   event=threading.Event();self.jobs[pid]=(job,event);self.active=ident;self._write(pid,job)
   threading.Thread(target=self._run,args=(pid,job,event,work,source,guide_source),daemon=True).start()
   return copy.deepcopy(job)
 def _update(self,pid,job,**values):
  with self.lock:job.update(values);self._write(pid,job)
 def cancel(self,pid):
  with self.lock:
   pair=self.jobs.get(pid)
   if pair and pair[0]['status'] in ('queued','running'):
    pair[1].set();self._update(pid,pair[0],status='cancelling',message='결과 수신을 중단합니다. 실행 중인 GPU 계산은 끝날 수 있습니다.')
   return self.view(pid)
 def _run(self,pid,job,event,work,source,guide_source=None):
  prompt_id=None
  try:
   if event.is_set():raise InterruptedError()
   self._update(pid,job,status='running',message='참고 이미지와 프롬프트로 로컬 생성 중')
   image=self.provider.upload(source)
   if event.is_set():raise InterruptedError()
   middle_image=self.provider.upload(guide_source) if guide_source is not None else None
   graph=workflow(job['request'],image,job['id'],middle_image=middle_image)
   (work/'workflow.json').write_text(json.dumps(graph,ensure_ascii=False,indent=2),encoding='utf8')
   if event.is_set():raise InterruptedError()
   result=self.provider.json('/prompt',dict(prompt=graph,client_id=job['id']))
   prompt_id=result.get('prompt_id')
   if not prompt_id or result.get('node_errors'):raise ValueError('생성 워크플로 거절: '+json.dumps(result))
   self._update(pid,job,prompt_id=prompt_id)
   deadline=time.monotonic()+1800
   while time.monotonic()<deadline:
    if event.is_set():
     self.provider.json('/queue',{'delete':[prompt_id]});raise InterruptedError()
    history=self.provider.json('/history/'+prompt_id).get(prompt_id)
    if history:
     if history.get('status',{}).get('status_str')=='error':raise ValueError('로컬 생성 실패: '+json.dumps(history.get('status'),ensure_ascii=False)[:2500])
     entries=history.get('outputs',{}).get('12',{}).get('images',[])
     if not entries:raise ValueError('완료 결과에 영상 프레임이 없습니다.')
     raw=self.provider.call('/view?'+urlencode({k:entries[0][k] for k in ('filename','subfolder','type')}));break
    event.wait(2)
   else:raise TimeoutError('로컬 영상 생성 30분 제한 초과. ComfyUI 작업은 계속될 수 있습니다.')
   if event.is_set():raise InterruptedError()
   (work/'generated.webp').write_bytes(raw)
   with Image.open(work/'generated.webp') as im:
    if im.n_frames!=job['request']['frames'] or im.size!=(job['request']['width'],job['request']['height']):raise ValueError('생성된 영상 규격이 요청과 다릅니다.')
    for i,frame in enumerate(ImageSequence.Iterator(im)):frame.convert('RGB').save(work/f'{i:03}.png')
   self._update(pid,job,message='생성 완료 · 편집용 MP4 변환 중')
   output=work/'generated.mp4'
   cmd=[ffmpeg(),'-v','error','-nostdin','-y','-framerate','16','-i',str(work/'%03d.png'),'-c:v','libx264','-pix_fmt','yuv420p','-crf','18','-movflags','+faststart',str(output)]
   subprocess.run(cmd,check=True,capture_output=True,timeout=120,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
   with self.lock:
    if event.is_set():raise InterruptedError()
    with output.open('rb') as f:media=self.cutscene.import_media(pid,f,output.stat().st_size,'local-ai-'+job['id']+'.mp4')
    self._update(pid,job,status='complete',message='생성 완료 · 컷씬 미디어에 추가됨 (시간표는 유지)',media=media,finished=time.time())
  except InterruptedError:self._update(pid,job,status='cancelled',message='결과 수신 중단됨 · 다른 작업은 중단하지 않았습니다.')
  except Exception as e:self._update(pid,job,status='failed',message=str(e)[:3000],finished=time.time())
  finally:
   with self.lock:
    if self.active==job['id']:self.active=None
 def close(self):
  with self.lock:
   for job,event in self.jobs.values():event.set()
