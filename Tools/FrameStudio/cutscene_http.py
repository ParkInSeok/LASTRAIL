"""Local authenticated HTTP adapter with bounded streaming imports and video ranges."""
import json,mimetypes,re
from urllib.parse import urlparse,parse_qs

def get(handler,service):
 u=urlparse(handler.path);q=parse_qs(u.query)
 if u.path=="/api/cutscene":
  handler.respond(service.view(q.get("id",[""])[0]));return True
 if u.path!="/cutscene-media":return False
 path=service.media_path(q.get("id",[""])[0],q.get("file",[""])[0])
 size=path.stat().st_size;start=0;end=size-1;partial=False
 requested=handler.headers.get("Range")
 if requested:
  match=re.fullmatch(r"bytes=(\d+)-(\d*)",requested)
  if not match:return handler.respond({"error":"지원하지 않는 범위"},416) or True
  start=int(match[1]);end=min(int(match[2]) if match[2] else end,end);partial=True
  if start>end or start>=size:
   handler.send_response(416);handler.send_header("Content-Range",f"bytes */{size}");handler.end_headers();return True
 handler.send_response(206 if partial else 200)
 handler.send_header("Content-Type",mimetypes.guess_type(path)[0] or "application/octet-stream")
 handler.send_header("Content-Length",str(end-start+1));handler.send_header("Accept-Ranges","bytes")
 handler.send_header("X-Content-Type-Options","nosniff")
 if partial:handler.send_header("Content-Range",f"bytes {start}-{end}/{size}")
 handler.end_headers()
 try:
  with path.open("rb") as f:
   f.seek(start);remaining=end-start+1
   while remaining:
    chunk=f.read(min(1024*1024,remaining))
    if not chunk:break
    handler.wfile.write(chunk);remaining-=len(chunk)
 except (BrokenPipeError,ConnectionResetError,ConnectionAbortedError):pass
 return True

def post(handler,service):
 u=urlparse(handler.path);q=parse_qs(u.query)
 if u.path not in ("/api/cutscene","/api/cutscene-import"):return False
 length=int(handler.headers.get("Content-Length","0"))
 pid=q.get("id",[""])[0]
 if u.path=="/api/cutscene-import":
  handler.respond(service.import_media(pid,handler.rfile,length,q.get("name",[""])[0]));return True
 if not 0<length<256*1024:raise ValueError("편집 요청 크기 초과")
 data=json.loads(handler.rfile.read(length));action=data.get("action")
 if action=="save":result=service.save(pid,data.get("timeline"),data.get("revision"))
 elif action=="render":result=service.render(pid,data.get("revision"),data.get("preview",False))
 elif action=="cancel":result=service.cancel(pid)
 else:raise ValueError("없는 편집 명령")
 handler.respond(result);return True
