import json, math, shutil, hashlib
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter
ROOT=Path('D:/Project/LASTRAIL/QA/DirectionImpact-20260921/operator-cinematics-v01-review')
ROOT.mkdir(parents=True,exist_ok=True)
data=json.loads(Path(__file__).with_name('operator-motion-final-input.json').read_text(encoding='utf-8'))
W,H=432,768
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',26)
def ease(v):
    v=max(0,min(1,v)); return v*v*(3-2*v)
def background(color):
    im=Image.new('RGB',(W,H)); p=im.load()
    for y in range(H):
        for x in range(W):
            a=math.exp(-((x-W*.6)**2/(W*.65)**2+(y-H*.3)**2/(H*.6)**2))
            p[x,y]=tuple(int(8+c*.19*a) for c in color)
    d=ImageDraw.Draw(im)
    for x in range(-900,1000,110):d.line((x,0,x-520,H),fill=(40,43,46))
    return im.convert('RGBA')
qa=[];cards=[];manifest=[]
for spec in data:
    folder=ROOT/spec['id'];(folder/'assets').mkdir(parents=True,exist_ok=True)
    poses=[];alpha=[]
    for i,src in enumerate(spec['sources'],1):
        dst=folder/'assets'/f'pose-{i:02}.png';shutil.copy2(src,dst)
        im=Image.open(dst).convert('RGBA');hist=im.getchannel('A').histogram()
        alpha.append({'size':list(im.size),'zero':hist[0],'opaque':hist[255],'semi':sum(hist[1:255])})
        assert hist[0]>0, f'{spec["id"]} pose{i}: alpha missing'
        poses.append(im.resize((W,648),Image.Resampling.LANCZOS))
    bg=background(spec['color'])
    def render(t,mode):
        im=bg.copy()
        if mode=='recruitment':
            actor=poses[0]; z=1+.025*ease(t/2100); y=100+int(22*(1-ease(t/650)))+int(2*math.sin(t/550))
        else:
            if t<550:actor=poses[0]
            elif t<700:actor=Image.blend(poses[0],poses[1],ease((t-550)/150))
            elif t<1000:actor=poses[1]
            elif t<1150:actor=Image.blend(poses[1],poses[2],ease((t-1000)/150))
            else:actor=poses[2]
            z=1+.012*ease(t/1300);y=110
        actor=actor.resize((int(W*z),int(648*z)),Image.Resampling.BICUBIC)
        im.alpha_composite(actor,(-int((actor.width-W)/2),y))
        fx=Image.new('RGBA',(W,H));d=ImageDraw.Draw(fx)
        if mode=='recruitment':
            for n in range(14):
                x=(n*71+t*.027)%W;y0=(n*137-t*.07)%H
                d.ellipse((x,y0,x+2,y0+2),fill=(*spec['color'],95))
        elif 1300<t<1850:
            a=int(155*math.sin(math.pi*(t-1300)/550));c=(*spec['color'],a)
            if spec['action']=='fire':
                # Decorative flash only: not a gameplay hit or projectile count.
                if t<1480:
                    cx,cy=spec.get('flashCenter',[215,437]);r=23*(1-(t-1300)/180);d.ellipse((cx-r,cy-r,cx+r,cy+r),fill=(255,224,158,a))
            elif spec['action']=='brace':
                d.arc((62,330,370,658),200,340,fill=c,width=3)
            elif spec['action']=='mechanical':
                for k in range(4):d.line((130+k*24,510,143+k*24,480),fill=c,width=2)
            else:
                for k in range(7):
                    x=105+k*31;y0=505-int(20*math.sin(k+t/210));d.ellipse((x,y0,x+3,y0+3),fill=c)
        im.alpha_composite(fx.filter(ImageFilter.GaussianBlur(.7)))
        d=ImageDraw.Draw(im);d.text((28,688),spec['name'],font=font,fill=(238,225,205));d.line((28,726,140,726),fill=(*spec['color'],255))
        fade=ease(t/180)*(1-ease((t-2200)/300))
        return Image.blend(Image.new('RGBA',(W,H),(8,11,16,255)),im,fade).convert('RGB')
    for mode in ['recruitment','ultimate']:
        frames=[render(t,mode) for t in range(0,2500,40)]
        pal=Image.new('RGB',(W*4,H))
        for i,t in enumerate([300,800,1200,1400]):pal.paste(render(t,mode),(i*W,0))
        pal=pal.quantize(colors=256,method=Image.Quantize.MEDIANCUT)
        fs=[f.quantize(palette=pal,dither=Image.Dither.FLOYDSTEINBERG) for f in frames]
        durations=[40]*len(fs);durations[-1]=2500-40*(len(fs)-1)
        file=folder/f'{mode}-2.5s.gif';fs[0].save(file,save_all=True,append_images=fs[1:],duration=durations,loop=0,disposal=2,optimize=False)
        check=Image.open(file);length=0
        for j in range(check.n_frames):check.seek(j);length+=check.info.get('duration',0);check.convert('RGB').load()
        assert length==2500 and check.n_frames>3
        qa.append({'id':spec['id'],'mode':mode,'durationMs':length,'frames':check.n_frames,'sha256':hashlib.sha256(file.read_bytes()).hexdigest(),'alpha':alpha})
    sheet=Image.new('RGB',(W*3,H),(8,11,16))
    for i,t in enumerate([300,800,1200]):sheet.paste(render(t,'ultimate'),(i*W,0))
    sheet.resize((648,384),Image.Resampling.LANCZOS).save(folder/'poses.jpg',quality=94)
    render(1400,'ultimate').resize((216,384),Image.Resampling.LANCZOS).save(folder/'poster.jpg',quality=94)
    (folder/'PROMPTS.md').write_text('\n\n'.join(spec['prompts']),encoding='utf-8')
    label='외형·도구 추가 검토' if spec.get('provisional') else '최신 바이블 기준 후보'
    cards.append(f'<section><h2>{spec["name"]}</h2><p>{label}</p><div class="pair"><figure><figcaption>모집 등장 · 2.5초</figcaption><img loading="lazy" src="{spec["id"]}/recruitment-2.5s.gif"></figure><figure><figcaption>궁극기 · 3포즈 / 2.5초</figcaption><img loading="lazy" src="{spec["id"]}/ultimate-2.5s.gif"></figure></div><details><summary>포즈 비교</summary><img src="{spec["id"]}/poses.jpg"></details></section>')
    manifest.append({**spec,'referenceOnly':True,'rigged':False,'status':'art-motion-review-candidate'})
(ROOT/'qa.json').write_text(json.dumps(qa,ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'manifest.json').write_text(json.dumps({'task':'D-B-FINAL-01','version':'operator-cinematics-v01-20261003','scope':'existing14 excluding completed Reina; Rin/Cipher separate','operators':manifest,'runtimeImplemented':False},ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'index.html').write_text('<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>요원 등장·궁극기 연출</title><style>body{background:#090c11;color:#e7dac7;font:15px system-ui;margin:24px}main{max-width:1000px;margin:auto}.pair{display:flex;gap:16px;flex-wrap:wrap}figure{flex:1;min-width:240px;margin:0}img{width:100%;max-width:432px;height:auto}details img{max-width:648px}section{padding:22px 0;border-top:1px solid #554939}p{color:#b9ad9b;line-height:1.7}</style><main><h1>요원 등장·궁극기 연출 검토</h1><p>레이나 제외 기존 14명. 3포즈 평면 원화 전환 미리보기이며 실제 Live2D 모델·관절 리깅은 별도입니다.<br>재생 2.5초와 장식 효과는 참고용이며 실제 스킬 수치·대상·피해를 확정하지 않습니다.</p>'+''.join(cards)+'</main></html>',encoding='utf-8')
(ROOT/'REVIEW.md').write_text('# 요원 연출 후보 v01\n\n기존14인 전체 요청. 등장/궁극기 각2.5초 GIF. 원화3포즈·프롬프트·출처·해시·GIF decode 검증 제공. 최신20~29 개인문서와17/44 스킬 방향 기준. 미확인7인 외형/장비는 후보이며 기존게임 얼굴 기반, 최신문서 미술승인으로 승격하지 않음. Rin/Cipher 및 비전투 근무요원 제외. 실제Live2D/기기/런타임 미완료.\n\n잔여: 포즈 전환시 윤곽 중첩, 실제눈/입/머리카락 변형, 일부 원화의 반투명 배경광 제거와 파츠 분리. alpha 존재만으로 독립인물 납품완료 판정하지 않음. FX/문구는 참고GIF 합성용이고 실행UI로 사용하지 않음.\n',encoding='utf-8')
print(json.dumps({'operators':len(data),'gifs':len(qa),'output':str(ROOT)},ensure_ascii=False))
