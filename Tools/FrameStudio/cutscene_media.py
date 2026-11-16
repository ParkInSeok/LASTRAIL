"""Bounded media ingestion and local FFmpeg decoding; never accepts remote URLs."""
import hashlib, json, re, subprocess, uuid
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parent
MAX_BYTES=512*1024*1024
def input_args(path):
 formats={".mp4":"mov",".mov":"mov",".webm":"matroska",".wav":"wav",".mp3":"mp3",".ogg":"ogg",".m4a":"mov"}
 return ["-protocol_whitelist","file,pipe"]+(["-f",formats[path.suffix.lower()]] if path.suffix.lower() in formats else [])
def ffmpeg():
 binaries=list((ROOT/"vendor/imageio_ffmpeg/binaries").glob("ffmpeg*.exe"))
 if len(binaries)!=1:raise ValueError("로컬 FFmpeg가 준비되지 않았습니다.")
 return str(binaries[0])

def probe(path):
 if path.suffix.lower() in (".png",".jpg",".jpeg"):
  with Image.open(path) as im:
   if im.format not in ("PNG","JPEG") or min(im.size)<1 or max(im.size)>8192 or im.width*im.height>32_000_000:
    raise ValueError("이미지 크기/형식을 확인하세요.")
   im.load()
   return dict(kind="image",width=im.width,height=im.height,duration=0)
 result=subprocess.run([ffmpeg(),"-hide_banner","-nostdin",*input_args(path),"-i",str(path),"-t","0.05","-f","null","-"],
                       capture_output=True,timeout=25,creationflags=getattr(subprocess,"CREATE_NO_WINDOW",0))
 text=result.stderr.decode("utf-8","replace")
 if result.returncode:raise ValueError("미디어를 해독하지 못했습니다.")
 duration=re.search(r"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)",text)
 if not duration:raise ValueError("미디어 길이를 확인하지 못했습니다.")
 seconds=int(duration[1])*3600+int(duration[2])*60+float(duration[3])
 if not 0<seconds<=600:raise ValueError("원본 미디어는10분 이하입니다.")
 videos=re.findall(r"Video:[^\r\n]*? (\d{2,5})x(\d{2,5})([^\r\n]*)",text)
 if videos:
  # The output stream describes decoded/autooriented dimensions, not container storage.
  width,height,details=videos[-1];w,h=int(width),int(height)
  sar=re.search(r"SAR (\d+):(\d+)",details)
  if sar and int(sar[2]):w=round(w*int(sar[1])/int(sar[2]))
  if max(w,h)>4096:raise ValueError("영상은4096px 이하입니다.")
  return dict(kind="video",width=w,height=h,duration=seconds)
 if "Audio:" not in text:raise ValueError("오디오/영상 스트림이 없습니다.")
 return dict(kind="audio",duration=seconds)

def import_file(folder,stream,length,name):
 suffix=Path(name).suffix.lower()
 if suffix not in (".png",".jpg",".jpeg",".mp4",".mov",".webm",".wav",".mp3",".ogg",".m4a"):
  raise ValueError("지원하지 않는 미디어 형식입니다.")
 if not 0<length<=MAX_BYTES:raise ValueError("미디어는512MB 이하입니다.")
 ident=uuid.uuid4().hex;path=folder/(ident+suffix)
 digest=hashlib.sha256()
 try:
  with path.open("xb") as f:
   remaining=length
   while remaining:
    chunk=stream.read(min(1024*1024,remaining))
    if not chunk:raise ValueError("업로드가 중단됐습니다.")
    f.write(chunk);digest.update(chunk);remaining-=len(chunk)
  metadata=probe(path)
  if suffix in (".mp4",".mov",".webm") and metadata["kind"]!="video":
   raise ValueError("영상 파일에 영상 스트림이 없습니다.")
  if suffix in (".wav",".mp3",".ogg",".m4a") and metadata["kind"]!="audio":
   raise ValueError("음원 파일에 영상이 포함되어 있습니다.")
  return dict(id=ident,file=path.name,name=Path(name.replace("\\","/")).name[:160],
              sha256=digest.hexdigest(),bytes=length,**metadata)
 except (OSError,subprocess.TimeoutExpired) as error:
  path.unlink(missing_ok=True)
  raise ValueError("미디어 형식이 잘못됐거나 해독 시간이 초과됐습니다.") from error
 except Exception:
  path.unlink(missing_ok=True)
  raise
