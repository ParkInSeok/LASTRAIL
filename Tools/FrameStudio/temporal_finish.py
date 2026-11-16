"""Masked optical-flow review interpolation, never new AI artwork or art approval."""
import numpy as np
from PIL import Image

class TemporalProcessor:
 def __init__(self,images,mask,reference,landmarks=None):
  if not images or type(reference)!=int or not 0<=reference<len(images):raise ValueError('기준 프레임 오류')
  if len({im.size for im in images})!=1 or mask.size!=images[0].size:raise ValueError('마스크와 모든 프레임 크기를 맞추세요.')
  self.mask=np.array(mask.convert('L'))
  if not np.any(self.mask):raise ValueError('흰색 움직임 영역이 필요합니다.')
  if not np.all((self.mask==0)|(self.mask==255)):raise ValueError('흰색/검정 이진 마스크를 사용하세요.')
  self.base=np.array(images[reference].convert('RGBA'));self.images=[np.array(im.convert('RGBA')) for im in images]
  ys,xs=np.where(self.mask>0);self.box=(max(0,xs.min()-12),max(0,ys.min()-12),min(mask.width,xs.max()+13),min(mask.height,ys.max()+13))
  x,y,r,b=self.box
  if (r-x)*(b-y)>1024*1024:raise ValueError('움직임 영역은 약 100만 픽셀 이하로 나누세요.')
  self.cache={}
  self.landmarks=landmarks
  if landmarks is not None:
   a=np.asarray(landmarks,dtype=float)
   if a.ndim!=3 or a.shape[0]!=len(images) or a.shape[2]!=2 or not 3<=a.shape[1]<=24 or not np.isfinite(a).all():raise ValueError('프레임마다 같은 수의 대응점 3~24개가 필요합니다.')
   if (a<0).any() or (a[:,:,0]>=images[0].width).any() or (a[:,:,1]>=images[0].height).any():raise ValueError('대응점이 캔버스를 벗어났습니다.')
 def guided(self,left,right,u):
  import cv2
  x,y,r,b=self.box;w,h=r-x,b-y
  edge=np.array([[0,0],[w-1,0],[w-1,h-1],[0,h-1]],dtype='float32')
  pa=np.vstack([edge,np.array(self.landmarks[left],dtype='float32')-[x,y]]).astype('float32')
  pb=np.vstack([edge,np.array(self.landmarks[right],dtype='float32')-[x,y]]).astype('float32')
  if np.any(pa<0) or np.any(pb<0) or np.any(pa[:,0]>=w) or np.any(pb[:,0]>=w) or np.any(pa[:,1]>=h) or np.any(pb[:,1]>=h):raise ValueError('대응점은 움직임 마스크의 경계 안에 두세요.')
  target=pa*(1-u)+pb*u;sub=cv2.Subdiv2D((0,0,w,h))
  for pt in target:sub.insert(tuple(map(float,pt)))
  def prem(im):
   v=im[y:b,x:r].astype('float32')/255;v[:,:,:3]*=v[:,:,3:4];return v
  a,z=prem(self.images[left]),prem(self.images[right]);out=np.zeros_like(a)
  for triangle in sub.getTriangleList():
   tri=triangle.reshape(3,2);indices=[np.argmin(np.sum((target-v)**2,axis=1)) for v in tri]
   if any(np.linalg.norm(target[i]-v)>1 for i,v in zip(indices,tri)):continue
   dst=target[indices];aa=cv2.warpAffine(a,cv2.getAffineTransform(pa[indices],dst),(w,h));bb=cv2.warpAffine(z,cv2.getAffineTransform(pb[indices],dst),(w,h))
   cover=np.zeros((h,w),dtype='uint8');cv2.fillConvexPoly(cover,np.rint(dst).astype('int32'),1)
   out[cover>0]=(aa if u<.5 else bb)[cover>0]
  out[:,:,:3]=np.divide(out[:,:,:3],out[:,:,3:4],out=np.zeros_like(out[:,:,:3]),where=out[:,:,3:4]>1e-6)
  patch=self.base.copy();patch[y:b,x:r]=np.clip(np.rint(out*255),0,255).astype('uint8');return patch
 def frame(self,left,right,u):
  if not 0<=u<=1:raise ValueError('보간 범위 오류')
  if u==0:patch=self.images[left]
  elif u==1:patch=self.images[right]
  elif self.landmarks is not None:patch=self.guided(left,right,u)
  else:
   import cv2
   x,y,r,b=self.box
   a=self.images[left][y:b,x:r];z=self.images[right][y:b,x:r]
   if np.array_equal(a,z):patch=self.images[left]
   else:
    key=(left,right)
    if key not in self.cache:
     def gray(im):
      rgb=im[:,:,:3].astype(np.float32)*im[:,:,3:4]/255
      return np.ascontiguousarray(np.clip(cv2.cvtColor(rgb,cv2.COLOR_RGB2GRAY)*.5+im[:,:,3]*.5,0,255).astype('uint8'))
     ga,gz=gray(a),gray(z)
     dis=cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
     self.cache[key]=(dis.calc(ga,gz,None),dis.calc(gz,ga,None))
    f,g=self.cache[key];yy,xx=np.mgrid[:a.shape[0],:a.shape[1]].astype('float32')
    def warp(im,flow,t):
     prem=im.astype('float32')/255;prem[:,:,:3]*=prem[:,:,3:4]
     return cv2.remap(prem,xx-flow[:,:,0]*t,yy-flow[:,:,1]*t,cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
    mixed=warp(a,f,u)*(1-u)+warp(z,g,1-u)*u
    mixed[:,:,:3]=np.divide(mixed[:,:,:3],mixed[:,:,3:4],out=np.zeros_like(mixed[:,:,:3]),where=mixed[:,:,3:4]>1e-6)
    patch=self.base.copy();patch[y:b,x:r]=np.clip(np.rint(mixed*255),0,255).astype('uint8')
  out=self.base.copy();active=self.mask>0;out[active]=patch[active]
  return Image.fromarray(out)
