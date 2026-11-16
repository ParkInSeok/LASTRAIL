"""Shared fixed placement and timed independent FX for saved battle reviews."""
import math
from PIL import Image
from clip_workflow import decode_png
from effect_design import ImpactArtwork,validated_style
from fx_timeline import stage_at

class Projectile:
    def __init__(self,repo):self.repo=repo
    def configure(self,p,data):
        if type(data.get('enabled'))!=bool:raise ValueError('탄환 사용 여부를 선택하세요.')
        if not data['enabled']:
            p['battle_projectile']={**p.get('battle_projectile',{}),'enabled':False};return
        fire=data.get('fire_frame');muzzle=data.get('muzzle')
        if type(fire)!=int or not 0<=fire<len(p['frames']):raise ValueError('발사 프레임을 선택하세요.')
        if not isinstance(muzzle,list) or len(muzzle)!=2 or any(type(v) not in (int,float) or not math.isfinite(v) or not 0<=v<=1 for v in muzzle):raise ValueError('총구 위치는 원화 안에서 지정하세요.')
        offset=data.get('launch_offset',p.get('battle_projectile',{}).get('launch_offset',6))
        if type(offset) not in (int,float) or not math.isfinite(offset) or not 0<=offset<=120:raise ValueError('발사 간격을 확인하세요.')
        cfg={'launch_offset':offset,'enabled':True,'fire_frame':fire,'muzzle':muzzle}
        for key,low,high in [('angle_deg',-180,180),('speed',1,3000),('duration_ms',20,3000),('length',1,120),('width',1,40)]:
            value=data.get(key)
            if type(value) not in (int,float) or not math.isfinite(value) or not low<=value<=high:raise ValueError('탄환 설정 범위를 확인하세요: '+key)
            cfg[key]=value
        if type(cfg['duration_ms'])!=int or cfg['duration_ms']%10:raise ValueError('탄환 비행 시간은 10ms 단위입니다.')
        content=decode_png(data['png'])[0] if data.get('png') else None
        asset=p.get('battle_projectile',{}).get('tracer')
        if not asset and content is None:raise ValueError('독립 탄환 PNG를 등록하세요.')
        cfg['tracer']=self.repo.put_asset(content) if content is not None else asset
        p['battle_projectile']=cfg

class BattleScene:
    def __init__(self, repo, p, bg, settings):
        self.repo=repo;self.p=p;self.bg=bg;self.settings=settings
        self.actors=[];self.flashes=[];self.ends=[];total=0
        for f in p['frames']:
            with Image.open(repo.asset_path(f['versions'][-1]['asset'])) as image:self.actors.append(image.convert('RGBA'))
            if f.get('fx_asset'):
                with Image.open(repo.asset_path(f['fx_asset'])) as image:self.flashes.append(image.convert('RGBA'))
            else:self.flashes.append(None)
            total+=f['duration_ms'];self.ends.append(total)
        base=self.actors[0];self.size=base.size
        self.timeline_images={}
        for row in p.get('fx_timeline',{}).get('stages',[]):
            with Image.open(repo.asset_path(row['asset'])) as image:
                if image.size!=self.size:raise ValueError('원화와 FX 캔버스가 서로 다릅니다.')
                self.timeline_images[row['asset']]=image.convert('RGBA')
        if any(im.size!=self.size for im in self.actors) or any(im is not None and im.size!=self.size for im in self.flashes):raise ValueError('프레임·FX 캔버스가 서로 다릅니다.')
        box=base.getchannel('A').point(lambda v:255 if v>128 else 0).getbbox()
        if not box:raise ValueError('첫 프레임에 표시할 인물이 없습니다.')
        self.scale=settings['height']/(box[3]-box[1]);self.target=tuple(round(v*self.scale) for v in self.size)
        if min(self.target)<1 or max(self.target)>8192:raise ValueError('인물 배율이 너무 큽니다.')
        self.pos=(round(settings['anchor'][0]*bg.width-(box[0]+box[2])*.5*self.scale),round(settings['anchor'][1]*bg.height-box[3]*self.scale))
        self.cfg=p.get('battle_projectile',{});self.shot=None;self.impact=None
        if self.cfg.get('enabled'):
            fire=self.cfg['fire_frame']
            if not 0<=fire<len(p['frames']):raise ValueError('시간표가 변경됐습니다. 발사 프레임을 다시 지정하세요.')
            self.fire_ms=0 if fire==0 else self.ends[fire-1]
            self.muzzle=tuple(self.pos[i]+self.cfg['muzzle'][i]*self.size[i]*self.scale for i in (0,1))
            radians=math.radians(self.cfg['angle_deg']);self.direction=(math.cos(radians),math.sin(radians))
            with Image.open(repo.asset_path(self.cfg['tracer'])) as image:tracer=image.convert('RGBA')
            box=tracer.getbbox()
            if not box:raise ValueError('탄환 PNG에 보이는 그림이 없습니다.')
            tracer=tracer.crop(box).resize((round(self.cfg['length']),round(self.cfg['width'])),Image.Resampling.LANCZOS)
            self.sprite=tracer.rotate(-self.cfg['angle_deg'],resample=Image.Resampling.BICUBIC,expand=True)
            self.shot={'count_per_loop':1,'fire_ms':self.fire_ms,'muzzle_scene':self.muzzle,'direction':self.direction}
            if p.get('effect_style',{}).get('impact_enabled'):
                style=validated_style(p['effect_style']);self.impact=ImpactArtwork(style)
                self.impact_ms=self.fire_ms+self.cfg['duration_ms'];self.impact_end=self.impact_ms+style['impact_duration_ms']
                if self.impact_end>self.ends[-1]:raise ValueError('명중 효과가 시간표 끝을 넘습니다. 비행·명중 시간을 줄이거나 시간표를 늘려 주세요.')
                distance=self.cfg.get('launch_offset',6)+self.cfg['duration_ms']*self.cfg['speed']/1000
                self.impact_point=tuple(self.muzzle[i]+self.direction[i]*distance for i in (0,1))
    def times(self):
        times={0,*self.ends}
        times.update(row['start_ms'] for row in self.p.get('fx_timeline',{}).get('stages',[]))
        if self.shot:
            end=min(self.ends[-1],self.fire_ms+self.cfg['duration_ms'])
            times.update(range(self.fire_ms,end,20));times.add(end)
        if self.impact:times.update(range(self.impact_ms,self.impact_end,20));times.add(self.impact_end)
        return sorted(times)
    def impact_layer(self,time):
        layer=Image.new('RGBA',self.bg.size)
        if self.impact:
            sprite=self.impact.render(time-self.impact_ms)
            layer.alpha_composite(sprite,(round(self.impact_point[0]-96),round(self.impact_point[1]-96)))
        return layer
    def render(self,time):
        index=next(i for i,end in enumerate(self.ends) if time<end)
        scene=self.bg.copy();scene.alpha_composite(self.actors[index].resize(self.target,Image.Resampling.LANCZOS),self.pos)
        fx=Image.new('RGBA',self.bg.size)
        stage=stage_at(self.p,time)
        flash=self.timeline_images[stage['asset']] if stage else self.flashes[index]
        threshold=self.p.get('fx_timeline',{}).get('occlude_actor_alpha')
        if flash is not None:
            if stage and threshold is not None:
                flash=flash.copy();flash.paste((0,0,0,0),(0,0),self.actors[index].getchannel('A').point(lambda a:255 if a>=threshold else 0))
            fx.alpha_composite(flash.resize(self.target,Image.Resampling.LANCZOS),self.pos)
        bullet=None
        if self.shot and 0<=time-self.fire_ms<self.cfg['duration_ms']:
            age=time-self.fire_ms;distance=self.cfg.get('launch_offset',6)+age*self.cfg['speed']/1000
            center=tuple(self.muzzle[i]+self.direction[i]*distance for i in (0,1))
            fx.alpha_composite(self.sprite,(round(center[0]-self.sprite.width/2),round(center[1]-self.sprite.height/2)))
            bullet={'shot_id':1,'age_ms':age,'center':center}
        fx.alpha_composite(self.impact_layer(time));scene.alpha_composite(fx)
        impact={'age_ms':time-self.impact_ms,'center':self.impact_point} if self.impact and self.impact_ms<=time<self.impact_end else None
        return scene,fx,{'time_ms':time,'actor_frame':index+1,'bullet':bullet,'impact':impact}
