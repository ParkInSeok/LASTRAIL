"""Optical-flow pose guides. These are interpolation inputs, not AI-drawn finals.

Optional dependency: requirements-guides.txt in the local engine venv.
"""
import numpy as np
from PIL import Image

def interpolate_coverage(a,b,amount):
 import cv2
 def distance(alpha):
  inside=(alpha>.5).astype('uint8')
  return cv2.distanceTransform(inside,cv2.DIST_L2,3)-cv2.distanceTransform(1-inside,cv2.DIST_L2,3)
 signed=distance(a)*(1-amount)+distance(b)*amount
 return np.clip(.5+signed*.5,0,1)

class KeyposeGuideBuilder:
 def build(self,source,target,mask,amount):
  if source.size!=target.size or source.size!=mask.size:raise ValueError('Keypose canvas mismatch')
  if not isinstance(amount,(int,float)) or not 0<=amount<=1:raise ValueError('Guide amount must be 0..1')
  a=np.asarray(source.convert('RGBA'),dtype=np.float32);b=np.asarray(target.convert('RGBA'),dtype=np.float32)
  weight=np.asarray(mask.convert('L'),dtype=np.float32)/255
  if amount==0 or np.array_equal(a,b):return source.convert('RGBA').copy()
  if amount==1:middle=b
  else:
   import cv2
   def rgba_to_gray(rgba):
    alpha=rgba[:,:,3:4]/255;rgb=rgba[:,:,:3]*alpha+235*(1-alpha)
    return cv2.cvtColor(rgb.astype('uint8'),cv2.COLOR_RGB2GRAY)
   ga,gb=rgba_to_gray(a),rgba_to_gray(b)
   flow=cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
   forward=flow.calc(ga,gb,None);backward=flow.calc(gb,ga,None)
   yy,xx=np.mgrid[:a.shape[0],:a.shape[1]].astype('float32')
   def warp(rgba,field,t):
    premul=rgba.copy();premul[:,:,:3]*=premul[:,:,3:4]/255
    return cv2.remap(premul,xx-t*field[:,:,0],yy-t*field[:,:,1],cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
   wa=warp(a,forward,amount);wb=warp(b,backward,1-amount)
   middle=wa*(1-amount)+wb*amount
   middle[:,:,:3]/=np.maximum(middle[:,:,3:4]/255,1/255)
   middle[:,:,3]=interpolate_coverage(wa[:,:,3]/255,wb[:,:,3]/255,amount)*255
  # Exact source outside the selected hair region; coverage-correct soft boundary.
  sa=a[:,:,3:4]/255;da=middle[:,:,3:4]/255;w=weight[:,:,None]
  alpha=sa*(1-w)+da*w
  rgb=(a[:,:,:3]*sa*(1-w)+middle[:,:,:3]*da*w)/np.maximum(alpha,1/255)
  out=np.concatenate([rgb,alpha*255],axis=2).round().clip(0,255).astype('uint8')
  out[weight==0]=a.astype('uint8')[weight==0]
  if amount==1:out[weight==1]=b.astype('uint8')[weight==1]
  return Image.fromarray(out)
