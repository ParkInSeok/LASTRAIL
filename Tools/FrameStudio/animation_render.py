"""Rasterize the authored rig, not the reference movie; own the encoder lifetime."""
import json, math, subprocess, threading, uuid
from pathlib import Path
from PIL import Image, ImageDraw
from animation_model import world_matrices
from cutscene_media import ffmpeg,probe
from animation_surface import bend_texture

def texture(part,asset,media,folder,cache):
    key=(part["kind"],asset,part["width"],part["height"],part["color"])
    if key in cache:return cache[key]
    w,h=max(1,round(part["width"])),max(1,round(part["height"]))
    if part["kind"]=="sprite":
        with Image.open(folder/media[asset]["file"]) as source:
            im=source.convert("RGBA")
            # Texture storage capped independently of the rendered canvas.
            im.thumbnail((2048,2048),Image.Resampling.LANCZOS)
    else:
        im=Image.new("RGBA",(w,h));d=ImageDraw.Draw(im);color=part["color"]
        if part["kind"]=="rect":d.rectangle((0,0,w-1,h-1),fill=color)
        elif part["kind"]=="ellipse":d.ellipse((0,0,w-1,h-1),fill=color)
        elif part["kind"]=="ring":
            thick=max(1,round(min(w,h)*.035))
            for start in (5,95,185,275):d.arc((0,0,w-1,h-1),start,start+65,fill=color,width=thick)
    cache[key]=im
    return im

def draw_frame(scene,media,folder,frame,width,height,cache):
    output=Image.new("RGBA",(width,height),scene["background"])
    states=world_matrices(scene,frame);factor=width/scene.get('width',1080)
    for part in scene["parts"]:
        st=states[part["id"]]
        if part["kind"]=="group" or st["opacity"]<=0:continue
        src=texture(part,st["asset"],media,folder,cache)
        src,padding=bend_texture(src,part,st["bend"])
        # Source pixels -> design size -> pivot -> parent world -> output canvas.
        a,b,c,d,e,f=[v*factor for v in st["matrix"]]
        e-=a*part["pivot_x"]*part["width"]+c*part["pivot_y"]*part["height"]
        f-=b*part["pivot_x"]*part["width"]+d*part["pivot_y"]*part["height"]
        e-=a*padding;f-=b*padding
        a*=(part["width"]+2*padding)/src.width;b*=(part["width"]+2*padding)/src.width
        c*=part["height"]/src.height;d*=part["height"]/src.height
        corners=[(e,f),(e+a*src.width,f+b*src.width),(e+c*src.height,f+d*src.height),
                 (e+a*src.width+c*src.height,f+b*src.width+d*src.height)]
        x0=max(0,math.floor(min(p[0] for p in corners))-2);y0=max(0,math.floor(min(p[1] for p in corners))-2)
        x1=min(width,math.ceil(max(p[0] for p in corners))+2);y1=min(height,math.ceil(max(p[1] for p in corners))+2)
        if x1<=x0 or y1<=y0:continue
        det=a*d-b*c
        inverse=(d/det,-c/det,(d*(x0-e)-c*(y0-f))/det,
                 -b/det,a/det,(-b*(x0-e)+a*(y0-f))/det)
        layer=src.transform((x1-x0,y1-y0),Image.Transform.AFFINE,inverse,Image.Resampling.BICUBIC)
        if st["opacity"]<1:layer.putalpha(layer.getchannel("A").point([round(i*st["opacity"]) for i in range(256)]))
        output.alpha_composite(layer,(x0,y0))
    return output.convert("RGB")

class AnimationJob:
    def __init__(self,folder,media_folder,snapshot,media,preview,on_import):
        self.folder=folder;self.media_folder=media_folder;self.snapshot=snapshot;self.media=media
        self.preview=bool(preview);self.on_import=on_import;self.id=uuid.uuid4().hex
        self.status="queued";self.progress=0;self.error="";self.output=None;self.process=None
        self.cancelled=threading.Event();self.lock=threading.RLock()
    def view(self,revision):
        with self.lock:
            return dict(id=self.id,status=self.status,progress=self.progress,error=self.error,
                revision=self.snapshot["revision"],stale=revision!=self.snapshot["revision"],
                preview=self.preview,media=self.output,frames=self.snapshot["scene"]["frames"])
    def start(self):
        self.thread=threading.Thread(target=self.run,daemon=True);self.thread.start()
    def cancel(self):
        with self.lock:
            if self.status not in ("queued","running"):return
            self.cancelled.set()
            if self.process and self.process.poll() is None:self.process.terminate()
    def run(self):
        path=self.folder/(self.id+".mp4");log=self.folder/(self.id+".log")
        try:
            size=(self.snapshot['scene'].get('width',1080),self.snapshot['scene'].get('height',1920))
            w,h=tuple(v//3 for v in size) if self.preview else size
            scene=self.snapshot["scene"];cache={}
            with log.open("wb") as err:
                with self.lock:
                    if self.cancelled.is_set():self.status="cancelled";return
                    self.process=subprocess.Popen([ffmpeg(),"-hide_banner","-nostdin","-y",
                        "-f","rawvideo","-pix_fmt","rgb24","-s",f"{w}x{h}","-r","30","-i","pipe:0",
                        "-an","-c:v","libx264","-preset","veryfast","-crf","20","-pix_fmt","yuv420p",
                        "-movflags","+faststart",str(path)],stdin=subprocess.PIPE,stderr=err,stdout=subprocess.DEVNULL,
                        creationflags=getattr(subprocess,"CREATE_NO_WINDOW",0))
                    self.status="running"
                for frame in range(scene["frames"]):
                    if self.cancelled.is_set():break
                    self.process.stdin.write(draw_frame(scene,self.media,self.media_folder,frame,w,h,cache).tobytes())
                    with self.lock:self.progress=(frame+1)/scene["frames"]*.95
                self.process.stdin.close()
                code=self.process.wait(timeout=45)
            if self.cancelled.is_set():self.status="cancelled"
            elif code:raise ValueError("동작 출력 실패: "+log.read_text("utf-8",errors="replace")[-1000:])
            else:
                info=probe(path)
                if (info["width"],info["height"])!=(w,h) or abs(info["duration"]-scene["frames"]/30)>.06:
                    raise ValueError("동작 출력 규격이 일치하지 않습니다.")
                if not self.cancelled.is_set():
                    media=self.on_import(path)
                    with self.lock:
                        if self.cancelled.is_set():self.status="cancelled"
                        else:self.output=media;self.progress=1;self.status="complete"
                else:self.status="cancelled"
        except Exception as error:
            with self.lock:
                self.status="cancelled" if self.cancelled.is_set() else "failed"
                self.error="" if self.cancelled.is_set() else str(error)
        finally:
            if self.process:
                if self.process.poll() is None:
                    self.process.kill();self.process.wait()
                if self.process.stdin and not self.process.stdin.closed:self.process.stdin.close()
            if self.status!="complete":path.unlink(missing_ok=True)
            value=self.view(self.snapshot["revision"]);value["scene"]=self.snapshot["scene"]
            record=self.folder/(self.id+".json")
            record.write_text(json.dumps(value,ensure_ascii=False),encoding="utf-8")
            temp=self.folder/"latest.tmp";temp.write_text(json.dumps(value,ensure_ascii=False),encoding="utf-8")
            temp.replace(self.folder/"latest.json")
