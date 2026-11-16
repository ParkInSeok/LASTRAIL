"""Authored cloth surface bend. Static source art remains unchanged.

Same 96 horizontal affine bands as the browser. Root is fixed above bend_start;
only the surface bends, not child joint transforms. Padding preserves the hem.
"""
import math
from PIL import Image

BANDS=96

def offset(y,start,amount):
    return amount*(max(0,(y-start)/(1-start))**2)

def bend_texture(source,part,amount):
    if abs(amount)<1e-8:return source,0
    # Budget the padded texture, not only the unbent source. Tiny design widths
    # otherwise amplify padding by thousands of source pixels per design unit.
    limit=2040
    working_width=max(1,min(source.width,math.floor(limit*part["width"]/
                          (part["width"]+2*abs(amount)))))
    working_height=max(BANDS,min(2048,source.height))
    if source.size!=(working_width,working_height):
        source=source.resize((working_width,working_height),Image.Resampling.BICUBIC)
    scale=source.width/part["width"]
    pad=math.ceil(abs(amount)*scale)+3
    width=source.width+2*pad
    height=source.height
    rows=[]
    for band in range(BANDS):
        y0=round(band*height/BANDS);y1=round((band+1)*height/BANDS)
        if y1<=y0:continue
        d0=offset(y0/height,part.get("bend_start",.5),amount)*scale
        d1=offset(y1/height,part.get("bend_start",.5),amount)*scale
        # Destination rectangle -> inverse source quadrilateral (UL,LL,LR,UR).
        rows.append(((0,y0,width,y1),(-pad-d0,y0,-pad-d1,y1,
                     width-pad-d1,y1,width-pad-d0,y0)))
    return source.transform((width,height),Image.Transform.MESH,rows,
                            Image.Resampling.BICUBIC),pad/scale
