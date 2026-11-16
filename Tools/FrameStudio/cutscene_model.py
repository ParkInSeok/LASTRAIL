"""Validated, frame-based edit decisions. No UI, file paths or render processes."""
import math

def default_timeline():
 return dict(version=1,width=1080,height=1920,fps=30,frames=150,bgm=0.65,sfx=0.65,
             label="콘티 검토본",clips=[])

def number(value,lo,hi,name,integer=False):
 if isinstance(value,bool) or not isinstance(value,(int,float)) or not math.isfinite(value):
  raise ValueError(name+" 숫자를 확인하세요.")
 if not lo<=value<=hi or (integer and int(value)!=value):raise ValueError(name+" 허용 범위를 확인하세요.")
 return int(value) if integer else float(value)

def validate_timeline(data,media):
 if not isinstance(data,dict):raise ValueError("편집 시간표가 필요합니다.")
 t=default_timeline()
 size=(data.get('width',1080),data.get('height',1920))
 if data.get("fps",30)!=30 or size not in ((1080,1920),(1920,1080)):
  raise ValueError("출력 규격은1080×1920 또는1920×1080/30fps입니다.")
 t['width'],t['height']=size
 t["frames"]=number(data.get("frames",150),1,1800,"전체 길이",True)
 for bus in ("bgm","sfx"):t[bus]=number(data.get(bus,.65),0,1,bus)
 if data.get("label",t["label"]) not in ("콘티 검토본","동작 영상 후보","사용자 승인본"):
  raise ValueError("영상 구분을 확인하세요.")
 # Export never grants approval; this label is user-authored editorial metadata.
 t["label"]=data.get("label",t["label"])
 rows=data.get("clips",[])
 if not isinstance(rows,list) or len(rows)>48:raise ValueError("클립은 최대48개입니다.")
 for row in rows:
  if not isinstance(row,dict):raise ValueError("클립 형식 오류")
  asset=row.get("asset")
  if not isinstance(asset,str) or asset not in media:raise ValueError("등록한 미디어를 선택하세요.")
  m=media[asset];track=row.get("track","visual")
  if track not in ("visual","BGM","SFX") or (track=="visual")!=(m["kind"] in ("image","video")):
   raise ValueError("영상/이미지는 화면 트랙, 음원은 BGM/SFX 트랙에 배치하세요.")
  c=dict(asset=asset,track=track)
  c["start"]=number(row.get("start",0),0,t["frames"]-1,"배치 시각",True)
  c["length"]=number(row.get("length",30),1,t["frames"]-c["start"],"클립 길이",True)
  c["source"]=number(row.get("source",0),0,18000,"원본 시작",True)
  if m["kind"]!="image" and (c["source"]+c["length"])/30>m["duration"]+.035:
   raise ValueError("원본 미디어 길이를 넘었습니다.")
  for k,lo,hi,default in [("x",-1,2,.5),("y",-1,2,.5),("end_x",-1,2,.5),("end_y",-1,2,.5),
                         ("scale",.1,3,1),("end_scale",.1,3,1),("opacity",0,1,1),("gain",0,2,1)]:
   c[k]=number(row.get(k,default),lo,hi,k)
  for k in ("fade_in","fade_out"):c[k]=number(row.get(k,0),0,c["length"]//2,k,True)
  t["clips"].append(c)
 return t
