import copy, io, tempfile, unittest, wave, time, subprocess, threading, json
import urllib.request, urllib.error
from pathlib import Path
from PIL import Image
from cutscene_model import validate_timeline, default_timeline
from cutscene_service import CutsceneService
from repository import Repository
from domain import new_project

class TimelineTests(unittest.TestCase):
 def test_bounds_and_owned_media(self):
  media={"a":{"kind":"image","duration":0}}
  t=default_timeline();t["clips"]=[dict(asset="a",track="visual",start=0,length=30,source=0)]
  good=validate_timeline(t,media)
  self.assertEqual(good["clips"][0]["length"],30)
  for change in ({"asset":"../bad"},{"start":-1},{"length":0},{"track":"BGM"},{"opacity":float("nan")}):
   bad=copy.deepcopy(t);bad["clips"][0].update(change)
   with self.assertRaises(ValueError):validate_timeline(bad,media)
 def test_trim_and_duration(self):
  t=default_timeline();t["clips"]=[dict(asset="v",track="visual",start=0,length=60,source=30)]
  with self.assertRaises(ValueError):validate_timeline(t,{"v":{"kind":"video","duration":2}})
 def test_round_trip_conflict_and_import(self):
  with tempfile.TemporaryDirectory() as d:
   repo=Repository(Path(d));p=new_project("cutscene");repo.save(p)
   service=CutsceneService(repo)
   raw=io.BytesIO();Image.new("RGB",(48,80),"red").save(raw,format="PNG")
   media=service.import_media(p["id"],io.BytesIO(raw.getvalue()),len(raw.getvalue()),"frame.png")
   state=service.view(p["id"]);t=state["timeline"]
   t["clips"]=[dict(asset=media["id"],track="visual",start=0,length=30,source=0)]
   saved=service.save(p["id"],t,0)
   self.assertEqual(saved["revision"],1)
   self.assertEqual(service.view(p["id"])["timeline"]["clips"][0]["asset"],media["id"])
   with self.assertRaises(ValueError):service.save(p["id"],t,0)
   with self.assertRaises(ValueError):service.media_path(p["id"],"../x.png")
   with self.assertRaises(ValueError):service.import_media(p["id"],io.BytesIO(b"bad"),3,"bad.png")
 def test_audio_not_allowed_on_visual(self):
  t=default_timeline();t["clips"]=[dict(asset="a",track="visual",start=0,length=30)]
  with self.assertRaises(ValueError):validate_timeline(t,{"a":{"kind":"audio","duration":2}})
 def test_real_render_with_audio_and_stale_revision(self):
  with tempfile.TemporaryDirectory() as d:
   repo=Repository(Path(d));p=new_project("render");repo.save(p);s=CutsceneService(repo)
   png=io.BytesIO();Image.new("RGB",(64,96),"red").save(png,format="PNG")
   a=s.import_media(p["id"],io.BytesIO(png.getvalue()),len(png.getvalue()),"red.png")
   wav=io.BytesIO()
   with wave.open(wav,"wb") as w:
    w.setnchannels(1);w.setsampwidth(2);w.setframerate(48000);w.writeframes(b"\x00\x00"*48000)
   b=s.import_media(p["id"],io.BytesIO(wav.getvalue()),len(wav.getvalue()),"audio.wav")
   t=default_timeline();t["frames"]=30
   t["clips"]=[dict(asset=a["id"],track="visual",start=0,length=30,end_scale=1.1,fade_in=3),
               dict(asset=b["id"],track="SFX",start=0,length=30)]
   s.save(p["id"],t,0)
   s.render(p["id"],1,True)
   deadline=time.time()+40
   while time.time()<deadline:
    job=s.view(p["id"])["job"]
    if job["status"] not in ("queued","running"):break
    time.sleep(.1)
   self.assertEqual(job["status"],"complete",job)
   from cutscene_media import probe,ffmpeg
   path=s.media_path(p["id"],job["file"])
   meta=probe(path)
   self.assertEqual((meta["width"],meta["height"]),(360,640))
   self.assertAlmostEqual(meta["duration"],1,delta=.06)
   audio=subprocess.run([ffmpeg(),"-v","error","-i",str(path),"-map","0:a:0","-f","null","-"],capture_output=True,creationflags=getattr(subprocess,"CREATE_NO_WINDOW",0))
   self.assertEqual(audio.returncode,0,audio.stderr)
   s.save(p["id"],t,1)
   self.assertTrue(s.view(p["id"])["job"]["stale"])
 def test_phone_rotation_uses_decoded_portrait_dimensions(self):
  from cutscene_media import ffmpeg,probe
  with tempfile.TemporaryDirectory() as d:
   base=Path(d)/"base.mp4";rotated=Path(d)/"rotated.mp4"
   subprocess.run([ffmpeg(),"-v","error","-f","lavfi","-i","color=red:s=128x72:r=30:d=1",
                   "-c:v","libx264","-pix_fmt","yuv420p",str(base)],check=True,capture_output=True,creationflags=getattr(subprocess,"CREATE_NO_WINDOW",0))
   subprocess.run([ffmpeg(),"-v","error","-display_rotation:v:0","90","-i",str(base),"-c","copy",
                   str(rotated)],check=True,capture_output=True,creationflags=getattr(subprocess,"CREATE_NO_WINDOW",0))
   meta=probe(rotated)
   self.assertEqual((meta["width"],meta["height"]),(72,128))
 def test_http_auth_upload_range_and_invalid_media(self):
  from server import make_server
  with tempfile.TemporaryDirectory() as d:
   server=make_server(0,Path(d))
   thread=threading.Thread(target=server.serve_forever,daemon=True);thread.start()
   base=f"http://127.0.0.1:{server.server_port}"
   try:
    token=json.load(urllib.request.urlopen(base+"/api/state"))["token"]
    def post(path,body,authorized=True):
     headers={"X-Studio-Token":token} if authorized else {}
     return urllib.request.urlopen(urllib.request.Request(base+path,body,headers))
    with self.assertRaises(urllib.error.HTTPError) as error:post("/api/create",b'{"name":"test"}',False)
    self.assertEqual(error.exception.code,403)
    p=json.load(post("/api/create",b'{"name":"test"}'));pid=p["id"]
    png=io.BytesIO();Image.new("RGB",(16,16),"blue").save(png,format="PNG")
    m=json.load(post("/api/cutscene-import?id="+pid+"&name=x.png",png.getvalue()))
    url=base+"/cutscene-media?id="+pid+"&file="+m["file"]
    with urllib.request.urlopen(urllib.request.Request(url,headers={"Range":"bytes=0-7"})) as response:
     self.assertEqual(response.status,206);self.assertEqual(response.read(),png.getvalue()[:8])
    with self.assertRaises(urllib.error.HTTPError):post("/api/cutscene-import?id="+pid+"&name=x.mp4",b"not a video")
   finally:
    server.cutscene.close();server.shutdown();server.server_close();thread.join()

if __name__=="__main__":unittest.main()
