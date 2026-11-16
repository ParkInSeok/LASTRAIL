"""Explicit flat-background calibration of a reviewed donor; never blurs geometry.
The caller must choose an unedited background witness. Not automatic segmentation.
Use the existing validated reference-patch action to compose/save the donor.
"""
from statistics import median
from PIL import Image

def calibrate_background(source,donor,mask,box):
 if source.size!=donor.size or mask.size!=source.size:raise ValueError('캔버스 불일치')
 if len(box)!=4 or any(type(v)!=int for v in box):raise ValueError('기준 영역 오류')
 x0,y0,x1,y1=box
 if not 0<=x0<x1<=source.width or not 0<=y0<y1<=source.height or (x1-x0)*(y1-y0)<16:raise ValueError('기준 영역 범위 오류')
 if mask.convert('L').crop(box).getbbox():raise ValueError('배경 기준은 수정 영역 밖이어야 합니다.')
 a=source.convert('RGB');b=donor.convert('RGB');offset=[]
 for ac,bc in zip(a.crop(box).split(),b.crop(box).split()):
  av=list(ac.tobytes());bv=list(bc.tobytes())
  if max(av)-min(av)>12 or max(bv)-min(bv)>12:raise ValueError('평탄한 배경 기준이 필요합니다.')
  d=round(median([u-v for u,v in zip(av,bv)]))
  if abs(d)>32:raise ValueError('배경 오차가 보정 허용 범위를 넘었습니다.')
  offset.append(d)
 corrected=Image.merge('RGB',tuple(c.point([min(255,max(0,v+d)) for v in range(256)]) for c,d in zip(b.split(),offset)))
 return corrected,dict(anchor=list(box),offset_rgb=offset,method='constant RGB offset from explicit flat-background witness; no spatial blend, blur or warp',limitations='Does not repair geometry or prove color fidelity of hair; review clipping and boundary residuals.')
