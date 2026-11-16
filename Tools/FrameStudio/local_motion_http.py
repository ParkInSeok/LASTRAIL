"""Local-motion routes reuse the host and CSRF checks in the Studio HTTP server."""
import json
from urllib.parse import urlparse,parse_qs

def get(handler,service):
    url=urlparse(handler.path)
    if url.path!='/api/local-motion':return False
    handler.respond(service.view(parse_qs(url.query).get('id',[''])[0]));return True

def post(handler,service):
    url=urlparse(handler.path)
    if url.path!='/api/local-motion':return False
    length=int(handler.headers.get('Content-Length','0'))
    if not 0<length<=20000:raise ValueError('제작 요청 크기 초과')
    data=json.loads(handler.rfile.read(length))
    if not isinstance(data,dict):raise ValueError('잘못된 제작 요청')
    pid=parse_qs(url.query).get('id',[''])[0]
    if data.get('action')=='generate':job=service.start(pid,data)
    elif data.get('action')=='cancel':job=service.cancel(pid)
    else:raise ValueError('없는 제작 명령')
    handler.respond({'job':job});return True
