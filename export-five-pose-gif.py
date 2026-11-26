import os,json,math,shutil,hashlib
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import sys
root=Path('D:/Project/LASTRAIL/QA/DirectionImpact-20260921')
out=root/'ultimate-five-pose-v07-gif';(out/'assets').mkdir(parents=True,exist_ok=True)
new=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
old=root/'ultimate-front-shot-v05-preview'/'assets'
sources=[old/'raised.png',Path(new[0]['source']),old/'mid.png',Path(new[1]['source']),old/'aim.png']
poses=[];alpha=[]
for i,src in enumerate(sources,1):
    target=out/'assets'/f'pose-{i:02d}.png';shutil.copy2(src,target)
    img=Image.open(target).convert('RGBA');h=img.getchannel('A').histogram();alpha.append({'pose':i,'size':list(img.size),'zeroAlphaPixels':h[0]})
    assert h[0]>0, f'pose{i} has no transparent pixels'
    poses.append(img.resize((432,648),Image.Resampling.LANCZOS))
W,H=432,768
bg=Image.new('RGB',(W,H));pix=bg.load()
for y in range(H):
    for x in range(W):
        red=math.exp(-((x-W*.72)**2/(W*.6)**2+(y-H*.3)**2/(H*.55)**2))
        pix[x,y]=(int(9+31*red),int(11+10*red),int(18+10*red))
draw=ImageDraw.Draw(bg)
for x in range(-800,1000,125):draw.line((x,0,x-540,H),fill=(54,38,30),width=1)
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',27)
def ease(v):v=max(0,min(1,v));return v*v*(3-2*v)
def blend_at(t):
    for a,b,i in [(280,520,0),(650,830,1),(920,1130,2),(1230,1370,3)]:
        if t<a:return poses[i]
        if t<=b:return Image.blend(poses[i],poses[i+1],ease((t-a)/(b-a)))
    return poses[4]
def render(t):
    frame=bg.convert('RGBA');actor=Image.new('RGBA',(W,H));actor.alpha_composite(blend_at(t),(0,120))
    recoil=max(0,1-abs(t-1500)/110) if 1450<=t<=1610 else 0
    if recoil:
        scale=1+recoil*.016;s=actor.resize((int(W*scale),int(H*scale)),Image.Resampling.BICUBIC)
        actor=Image.new('RGBA',(W,H));actor.alpha_composite(s,(-int((s.width-W)/2),int(3*recoil)))
    frame.alpha_composite(actor)
    if 1440<=t<=1570:
        power=max(0,1-abs(t-1480)/65);fx=Image.new('RGBA',(W,H));d=ImageDraw.Draw(fx);cx,cy=W*.44,H*.431
        for r,c in [(33,(244,188,101,int(70*power))),(16,(255,225,164,int(160*power))),(6,(255,249,218,int(255*power)))]:d.ellipse((cx-r,cy-r,cx+r,cy+r),fill=c)
        d.line((cx-28,cy-28,cx+28,cy+28),fill=(255,224,153,int(190*power)),width=2);d.line((cx-28,cy+28,cx+28,cy-28),fill=(255,224,153,int(190*power)),width=2)
        frame.alpha_composite(fx.filter(ImageFilter.GaussianBlur(.8)))
    label=ImageDraw.Draw(frame);label.text((30,680),'레이나',font=font,fill=(236,220,193,255));label.line((30,718,143,718),fill=(177,139,84,255),width=1)
    fade=ease(t/160)* (1-ease((t-2100)/400));frame=Image.blend(Image.new('RGBA',(W,H),(8,12,17,255)),frame,fade)
    return frame.convert('RGB')
times=list(range(0,2500,40));frames=[render(t) for t in times]
palette_sheet=Image.new('RGB',(W*5,H))
for i,t in enumerate([220,570,870,1180,1480]):palette_sheet.paste(render(t),(i*W,0))
palette=palette_sheet.quantize(colors=256,method=Image.Quantize.MEDIANCUT)
indexed=[f.quantize(palette=palette,dither=Image.Dither.FLOYDSTEINBERG) for f in frames]
durations=[40]*len(indexed);durations[-1]=2500-40*(len(indexed)-1)
gif=out/'reina-ultimate-5poses-2.5s.gif'
indexed[0].save(gif,save_all=True,append_images=indexed[1:],duration=durations,loop=0,disposal=2,optimize=False)
sheet=Image.new('RGB',(W*5,H),(8,12,17));
for i,t in enumerate([220,570,870,1180,1400]):sheet.paste(render(t),(i*W,0))
sheet.resize((1080,384),Image.Resampling.LANCZOS).save(out/'five-poses.png')
render(1480).save(out/'shot.png')
check=Image.open(gif);duration=0
for i in range(check.n_frames):check.seek(i);duration+=check.info.get('duration',0);check.convert('RGB').load()
qa={'pass':check.n_frames>5 and duration==2500 and all(x['zeroAlphaPixels']>0 for x in alpha),'size':[W,H],'encodedFrames':check.n_frames,'durationMs':duration,'loop':check.info.get('loop'),'poseCount':5,'alpha':alpha,'sha256':hashlib.sha256(gif.read_bytes()).hexdigest(),'browserVerification':'not used; GIF decoded with Pillow'}
(out/'qa.json').write_text(json.dumps(qa,ensure_ascii=False,indent=2),encoding='utf-8');assert qa['pass']
manifest={'task':'D-B-FINAL-01','version':'direction.ultimate-five-pose-v07.20261002','status':'motion-review-candidate','referenceOnly':True,'rigged':False,'live2dModelSupplied':False,'poseCount':5,'durationMs':2500,'gif':gif.name,'sourcePoses':[str(s) for s in sources],'newArt':new,'excluded':'previous rejected upper-tilted pose not used','remaining':['continuous articulated Live2D motion','eye/hair animation','gameplay synchronization','user art review']}
(out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'PROMPTS.md').write_text('# 新規 중간 원화2장\n\nbuilt-in image_gen / truealpha\n\n'+'\n\n'.join(x['key']+'\n'+x['prompt'] for x in new),encoding='utf-8')
(out/'REVIEW.md').write_text('# 5자세 · 2.5초 GIF\n\n사용자 GIF/3→5동작원화 요청. 기존raised·첨부로선택한mid·aim 보존, 두구간 중간원화2장 추가. GIF432×768, 2500ms, 반복, globalpalette256. PNGalpha5장/총길이/파일decode 검사PASS; 실제Live2D리깅 아님, 평면keypose크로스페이드로윤곽중첩·정합잔여. GIF는참고용합성본, 독립PNG5개+manifest 제공. GIF내텍스트/장식광은참고전용이며 실행UI대체금지. 실제시전/발사횟수/피해정책확정0. 미술후보/기기·게임연결미완료.\n',encoding='utf-8')
(out/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>레이나 · 5자세 GIF</title><style>body{background:#080c11;color:#dfd3c1;font:15px system-ui;margin:20px}main{max-width:680px;margin:auto}h1{font-size:22px}img{display:block;max-width:100%;height:auto;margin:16px auto}a{color:#d8b47b}p{line-height:1.7;font-size:13px;color:#b4ab9e}</style><main><h1>레이나 · 5자세 궁극기 미리보기</h1><p>2.5초 반복 GIF · 든 자세 → 조준 진입 → 선택한 중간 자세 → 정면 조준 진입 → 정면 조준·격발<br>평면 원화 모션 후보이며 실제 Live2D 리깅은 별도입니다.</p><img src="reina-ultimate-5poses-2.5s.gif" alt="레이나 5자세 궁극기 GIF"><a href="reina-ultimate-5poses-2.5s.gif" download>GIF 저장</a><p>동작 원화 5장 비교</p><img src="five-poses.png" alt="5자세 비교"><p>얼굴·의상·팔 윤곽의 프레임 간 정합과 연속 관절 움직임은 추가 제작 대상입니다.</p></main></html>''',encoding='utf-8')
print(json.dumps({'output':str(gif),'qa':qa},ensure_ascii=False))
