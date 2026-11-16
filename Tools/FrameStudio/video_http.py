import json
from urllib.parse import urlparse,parse_qs

def get(handler,service):
 u=urlparse(handler.path)
 if u.path!='/api/video-generation':return False
 handler.respond({'job':service.view(parse_qs(u.query).get('id',[''])[0])});return True

def post(handler,service):
 u=urlparse(handler.path)
 if u.path!='/api/video-generation':return False
 n=int(handler.headers.get('Content-Length','0'))
 if not 0<n<=20000:raise ValueError('생성 요청 크기 초과')
 data=json.loads(handler.rfile.read(n));pid=parse_qs(u.query).get('id',[''])[0]
 if not isinstance(data,dict):raise ValueError('잘못된 생성 요청')
 if data.get('action')=='generate':job=service.start(pid,data)
 elif data.get('action')=='cancel':job=service.cancel(pid)
 else:raise ValueError('없는 생성 명령')
 handler.respond({'job':job});return True
