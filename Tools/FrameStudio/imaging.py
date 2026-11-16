"""Observable image checks only. No claim of anatomical/semantic AI QA."""
from PIL import Image, ImageChops
from pathlib import Path
import io, math, json, zipfile

def inspect_png(content,expected=None):
 if len(content)>24*1024*1024:raise ValueError('이미지는 24MB 이하만 등록할 수 있습니다.')
 with Image.open(io.BytesIO(content)) as source:
  if source.format!='PNG':raise ValueError('투명 PNG를 사용하세요.')
  if source.width>4096 or source.height>4096 or min(source.size)<32:raise ValueError('크기는 32~4096px입니다.')
  source.load();im=source.convert('RGBA')
 blocking=[];warnings=[];alpha=im.getchannel('A');bbox=alpha.point(lambda v:255 if v>16 else 0).getbbox()
 if alpha.getextrema()[0]==255:blocking.append('투명 배경 없음')
 if not bbox:blocking.append('보이는 인물 없음')
 if expected and list(im.size)!=list(expected):blocking.append('첫 프레임과 캔버스 크기 불일치')
 if bbox and (min(bbox[:2])<2 or bbox[2]>im.width-2 or bbox[3]>im.height-2):warnings.append('캔버스 가장자리 잘림 가능성')
 return {'size':list(im.size),'bbox':bbox,'blocking':blocking,'warnings':warnings,'scope':'alpha/canvas only; human art review required'}

def compare_images(a,b):
 with Image.open(a) as aa,Image.open(b) as bb:
  if aa.size!=bb.size:return {'warning':'캔버스 불일치'}
  aa=aa.convert('RGBA');bb=bb.convert('RGBA');small=[]
  for im in (aa,bb):
   bg=Image.new('RGBA',im.size,(235,235,235,255));bg.alpha_composite(im);small.append(bg.convert('RGB').resize((128,128)))
  hist=ImageChops.difference(*small).histogram();mean=sum((i%256)*v for i,v in enumerate(hist))/(128*128*3)
  return {'mean_rgb_difference':round(mean,3),'identical':aa.tobytes()==bb.tobytes(),'scope':'변화량 참고값이며 플리커/관절 판정 아님'}

def export_clip(paths,durations,out):
 out.mkdir(parents=True,exist_ok=True);frames=[]
 for path in paths:
  with Image.open(path) as im:frames.append(im.convert('RGBA'))
 if not frames or len(frames)!=len(durations) or len({f.size for f in frames})!=1:raise ValueError('프레임/캔버스/시간표 불일치')
 w,h=frames[0].size;cols=min(4,len(frames));rows=math.ceil(len(frames)/cols)
 if w*h*cols*rows>100_000_000:raise ValueError('시트가 너무 큽니다. 프레임 수 또는 해상도를 줄이세요.')
 sheet=Image.new('RGBA',(cols*w,rows*h));seq=[]
 for i,f in enumerate(frames):
  sheet.paste(f,(i%cols*w,i//cols*h));c=f.copy();c.thumbnail((720,720),Image.Resampling.LANCZOS);bg=Image.new('RGBA',c.size,(235,235,235,255));bg.alpha_composite(c);seq.append(bg.convert('RGB'))
 sheet.save(out/'sheet.png');palette_grid=Image.new('RGB',(seq[0].width*cols,seq[0].height*rows))
 for i,f in enumerate(seq):palette_grid.paste(f,(i%cols*f.width,i//cols*f.height))
 pal=palette_grid.quantize(colors=256);q=[f.quantize(palette=pal,dither=Image.Dither.NONE) for f in seq]
 q[0].save(out/'preview.gif',save_all=True,append_images=q[1:],duration=durations,loop=0,disposal=2,optimize=False)
 frames[0].save(out/'preview-full.png',save_all=True,append_images=frames[1:],duration=durations,loop=0,disposal=0,blend=0)
 timeline={'durations_ms':durations,'source_frames':len(frames),'gif_note':'GIF may merge identical consecutive exposures while preserving time. PNG frames are authoritative.'};(out/'timeline.json').write_text(json.dumps(timeline,indent=2),encoding='utf-8')
 with zipfile.ZipFile(out/'delivery.zip','w',zipfile.ZIP_DEFLATED) as z:
  for i,path in enumerate(paths):z.write(path,f'frames/{i+1:03d}.png')
  for name in ('sheet.png','preview.gif','preview-full.png','timeline.json'):z.write(out/name,name)
 return {'sheet':str(out/'sheet.png'),'gif':str(out/'preview.gif'),'apng':str(out/'preview-full.png'),'zip':str(out/'delivery.zip')}
