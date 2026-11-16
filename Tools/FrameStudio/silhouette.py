"""Opt-in black matte approximation for local outline drafts, never final alpha."""
import numpy as np
from PIL import Image,ImageFilter

def context_box(size,bbox):
 side=min(512,max(256,bbox[2]-bbox[0]+64,bbox[3]-bbox[1]+64))
 cw=min(side,size[0]);ch=min(side,size[1])
 x=max(0,min(size[0]-cw,(bbox[0]+bbox[2]-cw)//2));y=max(0,min(size[1]-ch,(bbox[1]+bbox[3]-ch)//2))
 return x,y,x+cw,y+ch

def composite_silhouette(source,layer,mask):
 if source.size!=layer.size or source.size!=mask.size:raise ValueError('윤곽 합성 캔버스 불일치')
 rgb=np.asarray(layer.convert('RGB'),dtype=np.float32)
 # A draft matte only. Dark outlines can be thinned; visual review is required.
 alpha=np.clip((rgb.max(axis=2)-8)/24,0,1)
 straight=np.clip(rgb/np.maximum(alpha[:,:,None],1/255),0,255)
 straight[alpha==0]=0
 rgba=np.dstack((straight,alpha*255)).round().astype('uint8')
 # Do not reinterpret unchanged soft source edges as new opaque geometry.
 bg=Image.new('RGBA',source.size,'black');bg.alpha_composite(source.convert('RGBA'))
 delta=np.abs(rgb-np.asarray(bg.convert('RGB'),dtype=np.float32)).max(axis=2)
 confidence=np.clip((delta-8)/24,0,1)
 confidence=np.asarray(Image.fromarray((confidence*255).round().astype('uint8')).filter(ImageFilter.GaussianBlur(.7)),dtype=np.float32)/255
 weight=np.asarray(mask.convert('L'),dtype=np.float32)/255*confidence
 # Blend coverage and premultiplied color, not invisible straight RGB.
 src=np.asarray(source.convert('RGBA'),dtype=np.float32);dst=rgba.astype(np.float32)
 sa=src[:,:,3]/255;da=dst[:,:,3]/255
 outa=sa*(1-weight)+da*weight
 premul=src[:,:,:3]*sa[:,:,None]*(1-weight[:,:,None])+dst[:,:,:3]*da[:,:,None]*weight[:,:,None]
 outrgb=premul/np.maximum(outa[:,:,None],1/255)
 result=np.dstack((outrgb,outa*255)).round().clip(0,255).astype('uint8')
 result[weight==0]=src.astype('uint8')[weight==0]
 return Image.fromarray(result)
