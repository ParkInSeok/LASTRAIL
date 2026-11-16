"""Procedural FX artwork and validated design state, independent of actor frames."""
from pathlib import Path
import base64,copy,io,math,re,json,zipfile
from PIL import Image,ImageDraw,ImageFilter

DEFAULT_STYLE=dict(impact_pattern='reina',shape='teardrop',color='#ffb430',core='#ffffec',glow=1.2,impact_enabled=True,impact_scale=2.2,impact_duration_ms=160,impact_color='#ffbf3d',impact_core='#ffffef')
SHAPES=('teardrop','orb','ember','ring','arcane','kyle')

def validated_style(data):
    if not isinstance(data,dict):raise ValueError('효과 제작 설정을 확인하세요.')
    s={**DEFAULT_STYLE,**{k:v for k,v in data.items() if k in DEFAULT_STYLE}}
    if s['shape'] not in SHAPES:raise ValueError('탄환 모양을 선택하세요.')
    if s['impact_pattern'] not in ('reina','kyle'):raise ValueError('명중 모양을 선택하세요.')
    for k in ('color','core','impact_color','impact_core'):
        if not isinstance(s[k],str) or not re.fullmatch(r'#[0-9a-fA-F]{6}',s[k]):raise ValueError('색상을 확인하세요.')
    for k,lo,hi in [('glow',0,4),('impact_scale',.5,3),('impact_duration_ms',40,1000)]:
        v=s[k]
        if type(v) not in (int,float) or not math.isfinite(v) or not lo<=v<=hi:raise ValueError('효과 설정 범위를 확인하세요: '+k)
    if type(s['impact_duration_ms'])!=int or s['impact_duration_ms']%20:raise ValueError('명중 지속시간은 20ms 단위입니다.')
    if type(s['impact_enabled'])!=bool:raise ValueError('명중 효과 사용 여부를 확인하세요.')
    return s

def rgb(value):return tuple(bytes.fromhex(value[1:]))

def projectile_image(s):
    if s['shape']=='kyle':
        with Image.open(Path(__file__).parent/'presets/kyle-projectile-source.png') as source:
            return source.convert('RGBA').crop((428,268,524,307)).transpose(Image.Transpose.FLIP_LEFT_RIGHT).resize((22,12),Image.Resampling.LANCZOS)
    # Supersampled independent sprite, pointing right. Same 15x9 core as Reina v03.
    ss=4;im=Image.new('RGBA',(64*ss,64*ss));d=ImageDraw.Draw(im);color=rgb(s['color'])+(245,);core=rgb(s['core'])+(255,)
    def pt(x,y):return ((32+x)*ss,(32+y)*ss)
    def poly(points,fill):d.polygon([pt(x,y) for x,y in points],fill=fill)
    def ellipse(box,fill=None,outline=None,width=1):d.ellipse(tuple((v+32)*ss for v in box),fill=fill,outline=outline,width=round(width*ss))
    shape=s['shape']
    if shape=='teardrop':
        poly([(-9,0),(-4,-2.8),(0,-4.5),(3,-4.1),(5,-2.7),(6,0),(5,2.7),(3,4.1),(0,4.5),(-4,2.8)],color)
        poly([(-5,0),(-1,-2.5),(2,-2.7),(4,-1.7),(4.8,0),(4,1.7),(2,2.7),(-1,2.5)],core)
    elif shape=='orb':ellipse((-6,-6,6,6),color);ellipse((-3,-3,3,3),core)
    elif shape=='ring':ellipse((-9,-7,9,7),outline=color,width=1.5);ellipse((-3,-3,3,3),core)
    elif shape=='ember':
        poly([(-9,0),(-6,-4),(-2,-3),(0,-6),(5,-3),(7,0),(4,4),(-1,5),(-4,2)],color);ellipse((-3,-2,4,2),core)
        for x,y in [(-12,-4),(-10,5),(-6,-7)]:ellipse((x-1,y-1,x+1,y+1),color)
    else:
        ellipse((-7,-6,7,6),outline=color,width=2);ellipse((-4,-3,4,3),rgb(s['color'])+(100,));poly([(1,-4),(4,-1),(1,3),(-1,0)],core)
    im=im.resize((64,64),Image.Resampling.LANCZOS);glow=im.filter(ImageFilter.GaussianBlur(s['glow']));glow.alpha_composite(im)
    return glow.crop(glow.getbbox())

class ImpactArtwork:
    def __init__(self,style):self.style=style
    def render(self,age):
        if self.style['impact_pattern']=='kyle':
            from kyle_effect import render_impact
            return render_impact(self.style,age)
        s=self.style;duration=s['impact_duration_ms'];out=Image.new('RGBA',(192,192))
        if not 0<=age<duration:return out
        # Normalize animation phase so duration changes do not change maximum extent.
        o=64 if s['impact_scale']<=2.2 else 96
        out=Image.new('RGBA',(o*2,o*2))
        phase=age*160/duration;scale=s['impact_scale'];fade=1-phase/160;rad=(5+phase*.075)*scale;d=ImageDraw.Draw(out)
        color=rgb(s['impact_color']);core=rgb(s['impact_core'])
        if phase<80:
            points=[(o+math.cos(i*math.pi/8)*rad*(1 if i%2==0 else .35),o+math.sin(i*math.pi/8)*rad*(1 if i%2==0 else .35)) for i in range(16)]
            d.polygon(points,fill=color+(round(255*fade),));r=max(1,4*(1-phase/100))*scale;d.ellipse((o-r,o-r,o+r,o+r),fill=core+(round(255*fade),))
        for angle in [.2,1.55,2.65,3.8,5.1]:
            start=rad+phase*.045*scale;end=start+3*fade*scale
            spark=(color[0],color[1],min(255,color[2]+9))
            d.line((o+math.cos(angle)*start,o+math.sin(angle)*start,o+math.cos(angle)*end,o+math.sin(angle)*end),fill=spark+(round(235*fade),),width=max(1,round(2*scale)))
        glow=out.filter(ImageFilter.GaussianBlur(1.5*scale));glow.alpha_composite(out)
        result=Image.new('RGBA',(192,192));result.alpha_composite(glow,(96-o,96-o));return result

class EffectDesigner:
    def __init__(self,repo,projectile):self.repo=repo;self.projectile=projectile
    def configure(self,p,data):
        style=validated_style(data.get('style'));sprite=projectile_image(style);stream=io.BytesIO();sprite.save(stream,format='PNG')
        cfg=copy.deepcopy(data.get('projectile',p.get('battle_projectile',{})))
        if not cfg.get('enabled'):raise ValueError('탄환 사용과 발사 위치·시점을 먼저 지정하세요.')
        cfg.update(png=base64.b64encode(stream.getvalue()).decode(),length=sprite.width,width=sprite.height)
        if style['shape']=='kyle':cfg['launch_offset']=11
        candidate=copy.deepcopy(p);self.projectile.configure(candidate,cfg)
        artwork=ImpactArtwork(style);frames=[artwork.render(t) for t in range(0,style['impact_duration_ms'],20)]
        # Preview has a dark matte; exported PNGs retain real alpha.
        preview=[]
        for frame in frames+[Image.new('RGBA',(192,192))]*10:
            matte=Image.new('RGBA',(192,192),'#182630');matte.alpha_composite(frame);preview.append(matte.convert('RGB'))
        gif=io.BytesIO();preview[0].save(gif,format='GIF',save_all=True,append_images=preview[1:],duration=20,loop=0,disposal=2)
        package=io.BytesIO()
        with zipfile.ZipFile(package,'w',zipfile.ZIP_DEFLATED) as archive:
            archive.writestr('projectile.png',stream.getvalue());archive.writestr('design.json',json.dumps(style,ensure_ascii=False,indent=2))
            for i,frame in enumerate(frames):
                png=io.BytesIO();frame.save(png,format='PNG');archive.writestr(f'impact/{i+1:03}.png',png.getvalue())
        candidate['effect_assets']={'projectile':candidate['battle_projectile']['tracer'],'impact_preview':self.repo.put_asset(gif.getvalue(),'gif'),'zip':self.repo.put_asset(package.getvalue(),'zip')}
        candidate.setdefault('effect_history',[]).append({'style':p.get('effect_style'),'projectile':p.get('battle_projectile'),'assets':p.get('effect_assets')})
        candidate['effect_style']=style;candidate['revision']+=1
        p.clear();p.update(candidate)
