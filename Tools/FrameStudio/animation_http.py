"""Uses the existing localhost/token boundary and immutable media registry."""
import json
from urllib.parse import urlparse,parse_qs

def get(handler,service):
    u=urlparse(handler.path)
    if u.path!="/api/animation":return False
    handler.respond(service.view(parse_qs(u.query).get("id",[""])[0]));return True

def post(handler,service):
    u=urlparse(handler.path)
    if u.path!="/api/animation":return False
    size=int(handler.headers.get("Content-Length","0"))
    if not 0<size<=1024*1024:raise ValueError("동작 요청은1MB 이하입니다.")
    data=json.loads(handler.rfile.read(size))
    if not isinstance(data,dict):raise ValueError("동작 요청 형식 오류")
    pid=parse_qs(u.query).get("id",[""])[0];action=data.get("action")
    if action=="save":result=service.save(pid,data.get("scene"),data.get("revision"))
    elif action=="render":result=service.render(pid,data.get("revision"),data.get("preview",False))
    elif action=="cancel":result=service.cancel(pid)
    elif action=="apply":result=service.apply(pid,data.get("revision"),data.get("cutsceneRevision"))
    else:raise ValueError("없는 동작 명령")
    handler.respond(result);return True
