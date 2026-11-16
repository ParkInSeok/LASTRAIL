"""Snapshot renderer. FFmpeg owns the one video/audio clock for preview and export."""
import json, subprocess, threading, uuid, time
from cutscene_media import ffmpeg,probe,input_args

def command(folder,state,target,preview):
 t=state["timeline"];w,h=output_size(t,preview)
 duration=t["frames"]/30
 args=[ffmpeg(),"-hide_banner","-nostdin","-y","-filter_complex_threads","1",
       "-f","lavfi","-i",f"color=c=0x101820:s={w}x{h}:r=30:d={duration}",
       "-f","lavfi","-i",f"anullsrc=r=48000:cl=stereo:d={duration}"]
 filters=[];visual="0:v";audios=["1:a"]
 for n,c in enumerate(t["clips"],2):
  m=state["media"][c["asset"]];path=folder/m["file"]
  if m["kind"]=="image":args+=["-loop","1","-framerate","30"]
  args+=input_args(path)+["-i",str(path)]
  start,length,source=c["start"]/30,c["length"]/30,c["source"]/30
  fi,fo=c["fade_in"]/30,c["fade_out"]/30
  if c["track"]=="visual":
   # Progress reaches the end transform on the last visible frame.
   span=max(1/30,length-1/30)
   progress=f"min(max(t/{span},0),1)"
   scale=f"({c['scale']}+({c['end_scale']-c['scale']})*{progress})"
   fit=min(w/m["width"],h/m["height"])
   fw=f"max(2,trunc({m['width']*fit}*{scale}/2)*2)"
   fh=f"max(2,trunc({m['height']*fit}*{scale}/2)*2)"
   chain=f"[{n}:v]trim=start={source}:duration={length},setpts=PTS-STARTPTS,fps=30,setsar=1,format=rgba,scale=w='{fw}':h='{fh}':eval=frame,colorchannelmixer=aa={c['opacity']}"
   if fi:chain+=f",fade=t=in:st=0:d={fi}:alpha=1"
   if fo:chain+=f",fade=t=out:st={length-fo}:d={fo}:alpha=1"
   chain+=f",setpts=PTS+{start}/TB[v{n}]";filters.append(chain)
   p=f"min(max((t-{start})/{span},0),1)"
   x=f"W*({c['x']}+({c['end_x']-c['x']})*{p})-w/2"
   y=f"H*({c['y']}+({c['end_y']-c['y']})*{p})-h/2"
   filters.append(f"[{visual}][v{n}]overlay=x='{x}':y='{y}':enable='gte(t,{start})*lt(t,{start+length})':eof_action=pass:repeatlast=0[o{n}]")
   visual=f"o{n}"
  else:
   gain=c["gain"]*t[c["track"].lower()]
   chain=f"[{n}:a]atrim=start={source}:duration={length},asetpts=PTS-STARTPTS,aresample=48000,volume={gain}"
   if fi:chain+=f",afade=t=in:st=0:d={fi}"
   if fo:chain+=f",afade=t=out:st={length-fo}:d={fo}"
   chain+=f",adelay={round(start*48000)}S:all=1[a{n}]";filters.append(chain);audios.append(f"a{n}")
 filters.append("".join(f"[{a}]" for a in audios)+f"amix=inputs={len(audios)}:duration=first:normalize=0,alimiter=limit=0.98:latency=1[aout]")
 filters.append(f"[{visual}]format=yuv420p[vout]")
 graph=folder/(target.stem+".filter.txt");graph.write_text(";\n".join(filters),encoding="utf-8")
 args+=["-filter_complex_script",str(graph),"-map","[vout]","-map","[aout]","-t",str(duration),
        "-r","30","-c:v","libx264","-preset","veryfast","-crf","23" if preview else "18",
        "-pix_fmt","yuv420p","-c:a","aac","-b:a","192k","-movflags","+faststart",
        "-progress","pipe:1","-nostats",str(target)]
 return args

def output_size(timeline,preview):
 size=(timeline.get('width',1080),timeline.get('height',1920))
 return tuple(v//3 for v in size) if preview else size

class RenderJob:
 def __init__(self,folder,snapshot,preview):
  self.folder=folder;self.snapshot=snapshot;self.preview=bool(preview)
  self.id=uuid.uuid4().hex;self.filename=self.id+".mp4";self.status="queued"
  self.progress=0;self.error="";self.process=None;self.cancelled=threading.Event();self.lock=threading.RLock()
 def view(self,revision):
  with self.lock:return dict(id=self.id,status=self.status,progress=self.progress,error=self.error,
    revision=self.snapshot["revision"],stale=revision!=self.snapshot["revision"],preview=self.preview,
    label=self.snapshot["timeline"]["label"],
    file=self.filename if self.status=="complete" else None)
 def start(self):
  self.thread=threading.Thread(target=self.run,daemon=True);self.thread.start()
 def cancel(self):
  with self.lock:
   if self.status not in ("queued","running"):return
   self.cancelled.set()
   if self.process and self.process.poll() is None:self.process.terminate()
 def persist(self):
  value=self.view(self.snapshot["revision"])
  value["timeline"]=self.snapshot["timeline"]
  path=self.folder/(self.id+".render.json")
  path.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding="utf-8")
  tmp=self.folder/"latest-render.tmp";tmp.write_text(json.dumps(value,ensure_ascii=False),encoding="utf-8")
  tmp.replace(self.folder/"latest-render.json")
 def run(self):
  target=self.folder/self.filename;log=self.folder/(self.id+".log")
  try:
   args=command(self.folder,self.snapshot,target,self.preview)
   with self.lock:
    if self.cancelled.is_set():self.status="cancelled";return
    self.status="running"
   with log.open("wb") as errors:
    with self.lock:
     if self.cancelled.is_set():self.status="cancelled";return
     self.process=subprocess.Popen(args,stdout=subprocess.PIPE,stderr=errors,
        creationflags=getattr(subprocess,"CREATE_NO_WINDOW",0))
    for line in self.process.stdout:
     text=line.decode("ascii","ignore").strip()
     if text.startswith("out_time_us="):
      try:
       with self.lock:self.progress=min(.99,int(text.split("=")[1])/1e6/(self.snapshot["timeline"]["frames"]/30))
      except ValueError:pass
    code=self.process.wait()
   if self.cancelled.is_set():
    self.status="cancelled";target.unlink(missing_ok=True)
   elif code:
    raise ValueError("영상 출력 실패: "+log.read_text("utf-8",errors="replace")[-1800:])
   else:
    meta=probe(target);expected=output_size(self.snapshot['timeline'],self.preview)
    if (meta["width"],meta["height"])!=expected or abs(meta["duration"]-self.snapshot["timeline"]["frames"]/30)>.08:
     raise ValueError("출력 해상도 또는 길이가 편집 시간표와 다릅니다.")
    with self.lock:self.progress=1;self.status="complete"
  except Exception as e:
   with self.lock:self.error=str(e);self.status="cancelled" if self.cancelled.is_set() else "failed"
   target.unlink(missing_ok=True)
  finally:
   if self.process and self.process.stdout:self.process.stdout.close()
   self.persist()
