"""Rigid region rotation with a fixed joint overlap and local pose transition."""
import math
import numpy as np
from PIL import Image,ImageFilter

def curve(keys,t):
 for (a,x),(b,y) in zip(keys,keys[1:]):
  if a<=t<=b:
   u=(t-a)/(b-a);u=u*u*u*(u*(u*6-15)+10)
   return x+(y-x)*u
 return keys[-1][1]

class RigidProcessor:
 def __init__(self,images,mask,reference,config,duration):
  if not isinstance(config,dict):raise ValueError('관절 설정이 필요합니다.')
  if type(reference)!=int or not 0<=reference<len(images):raise ValueError('기준 프레임 오류')
  self.size=images[reference].size;w,h=self.size
  if any(im.size!=self.size for im in images) or mask.size!=self.size:raise ValueError('이미지와 마스크 크기를 맞추세요.')
  self.mask=np.asarray(mask.convert('L'),dtype=np.float32)/255
  if not np.any(self.mask) or not np.all((self.mask==0)|(self.mask==1)):raise ValueError('흰색 움직임 영역이 있는 이진 마스크가 필요합니다.')
  try:
   self.pivot=tuple(map(float,config['pivot']));self.radius=float(config['joint_radius'])
   self.angles=config['angle_keys'];self.thumbs=config['thumb_keys'];self.box=tuple(config['thumb_box']);target=config['thumb_reference']
   assert len(self.pivot)==2 and all(math.isfinite(v) for v in self.pivot)
   assert 0<=self.pivot[0]<w and 0<=self.pivot[1]<h and 0<=self.radius<=128
   assert type(target)==int and 0<=target<len(images)
   assert len(self.box)==4 and all(type(v)==int for v in self.box)
   x,y,bw,bh=self.box;assert 0<=x<x+bw<=w and 0<=y<y+bh<=h
   for keys,limit in [(self.angles,math.pi),(self.thumbs,1)]:
    assert 2<=len(keys)<=32 and keys[0][0]==0 and keys[-1][0]==duration and keys[0][1]==keys[-1][1]
    assert all(len(k)==2 and all(type(v) in (int,float) and math.isfinite(v) for v in k) and abs(k[1])<=limit for k in keys)
    assert all(a[0]<b[0] for a,b in zip(keys,keys[1:]))
   assert all(0<=k[1]<=1 for k in self.thumbs)
  except (KeyError,TypeError,ValueError,IndexError,OverflowError,AssertionError):raise ValueError('관절 좌표·반경·엄지 영역·시간표를 확인하세요.') from None
  self.duration=duration
  self.continuous_joint=config.get('continuous_joint',False)
  def prem(im):
   a=np.asarray(im.convert('RGBA'),dtype=np.float32)/255;a[:,:,:3]*=a[:,:,3:4];return a
  self.original=np.array(images[reference].convert('RGBA'))
  self.source=prem(images[reference]);self.target=prem(images[target])
  # Feather only the local replacement border; no global image blending.
  local=Image.new('L',self.size);local.paste(255,(x,y,x+bw,y+bh))
  self.local=np.asarray(local.filter(ImageFilter.GaussianBlur(2)),dtype=np.float32)[:,:,None]/255
  yy,xx=np.mgrid[:h,:w];distance=np.hypot(xx-self.pivot[0],yy-self.pivot[1])
  cap=np.clip((self.radius-distance)/4,0,1) if self.radius else np.zeros((h,w))
  self.body=self.source*(1-self.mask*(1-cap))[:,:,None]
  # One continuous inverse mapping prevents holes between rotated and fixed pieces.
  ys,xs=np.where(self.mask>0)
  def smooth(v):
   v=np.clip(v,0,1);return v*v*(3-2*v)
  self.joint_weight=smooth((distance-8)/85)*smooth((self.pivot[0]+5-xx)/30)*smooth((yy-ys.min())/18)*smooth((ys.max()+240-yy)/40)
  self.grid=(xx.astype('float32'),yy.astype('float32'))
 def at(self,time):
  import cv2
  t=time%self.duration;angle=curve(self.angles,t);blend=curve(self.thumbs,t)
  if self.continuous_joint:
   xx,yy=self.grid;dx=xx-self.pivot[0];dy=yy-self.pivot[1]
   a=angle*self.joint_weight;c=np.cos(a);s=np.sin(a)
   mx=(self.pivot[0]+c*dx+s*dy).astype('float32');my=(self.pivot[1]-s*dx+c*dy).astype('float32')
   source=self.source*(1-self.local*blend)+self.target*self.local*blend
   out=cv2.remap(source,mx,my,cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
   # Do not pull fixed-side head/torso pixels into the moving-side empty space.
   fixed_side=mx>=self.pivot[0]+5
   out[fixed_side]=0
   stationary=fixed_side&(xx>=self.pivot[0]+5)
   out[stationary]=source[stationary]
   out[:,:,:3]=np.divide(out[:,:,:3],out[:,:,3:4],out=np.zeros_like(out[:,:,:3]),where=out[:,:,3:4]>1e-6)
   result=np.clip(np.rint(out*255),0,255).astype('uint8')
   protected=(self.joint_weight==0)&(self.local[:,:,0]==0)
   result[protected]=self.original[protected]
   return Image.fromarray(result)
  arm=(self.source*(1-self.local*blend)+self.target*self.local*blend)*self.mask[:,:,None]
  matrix=cv2.getRotationMatrix2D(self.pivot,-math.degrees(angle),1)
  arm=cv2.warpAffine(arm,matrix,self.size,flags=cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
  out=self.body+arm*(1-self.body[:,:,3:4])
  out[:,:,:3]=np.divide(out[:,:,:3],out[:,:,3:4],out=np.zeros_like(out[:,:,:3]),where=out[:,:,3:4]>1e-6)
  result=np.clip(np.rint(out*255),0,255).astype('uint8')
  protected=(self.mask==0)&(arm[:,:,3]==0)
  result[protected]=self.original[protected]
  return Image.fromarray(result)
