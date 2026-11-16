from http.server import ThreadingHTTPServer,BaseHTTPRequestHandler
from pathlib import Path
from urllib.parse import urlparse,parse_qs
import json,secrets,mimetypes,argparse
from repository import Repository
from codex_provider import CodexProvider
from providers import ProviderRouter
from local_provider import LocalProvider
from service import Studio
from domain import new_project
from cutscene_service import CutsceneService
import cutscene_http
from animation_service import AnimationService
import animation_http
from video_service import VideoService
import video_http
from local_motion_service import LocalMotionService
import local_motion_http
import combat_review

ROOT=Path(__file__).resolve().parent

def make_server(port=8769,data=None):
 repo=Repository(data or ROOT/'data')
 router=ProviderRouter(repo.root/'engine.json',{'local':LocalProvider(ROOT),'codex':CodexProvider()})
 studio=Studio(repo,router);cutscene=CutsceneService(repo);animation=AnimationService(cutscene);video=VideoService(cutscene,ROOT);motion=LocalMotionService(studio,cutscene,video);token=secrets.token_urlsafe(32)
 class Handler(BaseHTTPRequestHandler):
  def respond(self,value,code=200):
   raw=json.dumps(value,ensure_ascii=False).encode();self.send_response(code);self.send_header('Content-Type','application/json; charset=utf-8');self.send_header('Content-Length',str(len(raw)));self.end_headers();self.wfile.write(raw)
  def allowed(self):return self.headers.get('Host') in (f'127.0.0.1:{self.server.server_port}',f'localhost:{self.server.server_port}')
  def do_GET(self):
   if not self.allowed():return self.respond({'error':'잘못된 호스트'},403)
   try:
    if combat_review.get(self):return
    if local_motion_http.get(self,motion):return
    if video_http.get(self,video):return
    if animation_http.get(self,animation):return
    if cutscene_http.get(self,cutscene):return
    u=urlparse(self.path)
    if u.path=='/api/state':
     pid=parse_qs(u.query).get('id',[None])[0]
     with studio.repo.lock:self.respond({'token':token,'engine':router.info(),'projects':studio.list_projects(),'project':studio.view(studio.repo.get(pid)) if pid else None})
    elif u.path=='/api/connection':self.respond(studio.provider.status())
    else:
     if u.path.startswith('/assets/'):path=studio.repo.asset_path(u.path[8:])
     else:
      files={'/companion-view.js':'companion-view.js','/effect-designer.js':'effect-designer.js','/':'index.html','/app.js':'app.js','/pose-review.js':'pose-review.js','/clip-workflow.js':'clip-workflow.js','/wind-view.js':'wind-view.js','/style.css':'style.css'}
      files.update({'/cutscene':'cutscene.html','/cutscene-editor.js':'cutscene-editor.js','/cutscene.css':'cutscene.css'})
      files['/fx-timeline.js']='fx-timeline.js'
      files['/parts-progress']='parts-progress.html'
      files['/reference-patch.js']='reference-patch.js'
      files['/result-guide-view.js']='result-guide-view.js'
      files['/production-flow']='production-flow.html'
      files['/production-flow.js']='production-flow.js'
      files['/mask-editor.js']='mask-editor.js'
      files['/shading-review.js']='shading-review.js'
      files.update({'/animation':'animation.html','/animation-editor.js':'animation-editor.js','/animation-model.js':'animation-model.js','/animation.css':'animation.css'})
      files.update({'/video-generation':'video-generation.html','/video-generation.js':'video-generation.js'})
      files.update({'/local-motion':'local-motion.html','/local-motion.js':'local-motion.js'})
      if u.path not in files:raise FileNotFoundError()
      path=ROOT/'web'/files[u.path]
     content=path.read_bytes();self.send_response(200);self.send_header('Content-Type',mimetypes.guess_type(path)[0] or 'application/octet-stream');self.send_header('Content-Length',str(len(content)));self.send_header('X-Content-Type-Options','nosniff');self.end_headers();self.wfile.write(content)
   except (ValueError,FileNotFoundError,KeyError) as e:self.respond({'error':str(e) or '찾을 수 없음'},404)
  def do_POST(self):
   if not self.allowed() or self.headers.get('X-Studio-Token')!=token:return self.respond({'error':'새로고침 후 다시 시도하세요.'},403)
   try:
    if local_motion_http.post(self,motion):return
    if video_http.post(self,video):return
    if animation_http.post(self,animation):return
    if cutscene_http.post(self,cutscene):return
    n=int(self.headers.get('Content-Length','0'))
    if not 0<n<=34*1024*1024:raise ValueError('요청 크기 초과')
    data=json.loads(self.rfile.read(n))
    if self.path=='/api/create':
     p=new_project(data['name'])
     p['production_sequence_required']=True
     with studio.repo.lock:studio.repo.save(p)
     self.respond(studio.view(p))
    elif self.path=='/api/engine':self.respond(router.select(data['selected']))
    elif self.path=='/api/wind-preview':self.respond(studio.wind_preview(data['id'],data))
    elif self.path=='/api/action':self.respond(studio.action(data['id'],data))
    else:self.respond({'error':'없는 작업'},404)
   except (ValueError,KeyError,IndexError,FileNotFoundError) as e:self.respond({'error':str(e)},400)
   except Exception as e:self.respond({'error':type(e).__name__+': '+str(e)},500)
  def log_message(self,*args):pass
 server=ThreadingHTTPServer(('127.0.0.1',port),Handler);server.studio=studio;server.cutscene=cutscene;server.animation=animation;server.video=video;server.motion=motion;return server

if __name__=='__main__':
 a=argparse.ArgumentParser();a.add_argument('--port',type=int,default=8769);args=a.parse_args();server=make_server(args.port)
 print(f'FrameStudio http://127.0.0.1:{server.server_port}',flush=True)
 try:server.serve_forever()
 finally:server.motion.close();server.video.close();server.animation.close();server.cutscene.close();server.studio.provider.cancel();server.server_close()
