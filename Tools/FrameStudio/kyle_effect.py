"""Kyle review-v01 impact geometry, sampled to independent transparent frames."""
import math
from PIL import Image,ImageDraw

def render_impact(style,age):
    out=Image.new('RGBA',(192,192))
    if not 0<=age<style['impact_duration_ms']:return out
    p=age/style['impact_duration_ms'];fade=1-p;scale=style['impact_scale']
    radius=17*(.4+.6*p)*scale
    ss=4;layer=Image.new('RGBA',(192*ss,192*ss));d=ImageDraw.Draw(layer)
    core=tuple(bytes.fromhex(style['impact_core'][1:]));color=tuple(bytes.fromhex(style['impact_color'][1:]))
    stops=[(0,(239,255,255,255)),(.28,(139,255,255,255)),(.58,(32,205,230,170)),(1,(32,205,230,0))]
    for n in range(math.ceil(radius*1.3*ss),0,-1):
        u=min(1,n/(radius*1.3*ss));a,b=next((a,b) for a,b in zip(stops,stops[1:]) if a[0]<=u<=b[0])
        t=(u-a[0])/(b[0]-a[0]);rgba=[round(x+(y-x)*t) for x,y in zip(a[1],b[1])];rgba[3]=round(rgba[3]*fade*.85)
        o=96*ss;d.ellipse((o-n,o-n,o+n,o+n),fill=tuple(rgba))
    def point(angle,r):return ((96+math.cos(angle)*r)*ss,(96+math.sin(angle)*r)*ss)
    if p<.5:d.polygon([point(i*math.pi/6,radius*.7 if i%2==0 else 3*scale) for i in range(12)],fill=core+(round(255*fade),))
    for angle in [.2,1.5,2.7,3.8,5.1]:d.line([point(angle,radius),point(angle,radius+5*fade*scale)],fill=color+(round(255*fade),),width=max(1,round(2*scale*ss)))
    return layer.resize(out.size,Image.Resampling.LANCZOS)
