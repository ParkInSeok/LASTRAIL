"""Compile manually registered strand/shading tracks into the shared RGB transport mesh."""
import numpy as np


def compile_strands(spec):
 strands=spec.get('strands') if isinstance(spec,dict) else None
 if strands is None:return spec,[]
 if 'surfaces' in spec or not isinstance(strands,list) or not 1<=len(strands)<=16:
  raise ValueError('가닥 격자만 1~16개 지정하세요. 삼각형 방식과 혼합할 수 없습니다.')
 surfaces=[];tracks=[];ids=set()
 for strand in strands:
  if not isinstance(strand,dict):raise ValueError('가닥 형식 오류')
  name=strand.get('id');columns=strand.get('columns')
  if not isinstance(name,str) or not name or len(name)>100 or name in ids:raise ValueError('고유 가닥 ID가 필요합니다.')
  ids.add(name)
  if not isinstance(columns,list) or not 3<=len(columns)<=16 or any(not isinstance(c,str) or not c or len(c)>100 for c in columns) or len(set(columns))!=len(columns):
   raise ValueError('고유한 가닥 경계·명암 경계 열 이름을 지정하세요.')
  try:src=np.asarray(strand['source_rows'],dtype=float);dst=np.asarray(strand['target_rows'],dtype=float)
  except (KeyError,ValueError,TypeError):raise ValueError('가닥 대응점 형식 오류') from None
  if src.shape!=dst.shape or src.ndim!=3 or src.shape[1:]!=(len(columns),2) or not 2<=len(src)<=64 or src.size>512 or not np.isfinite(src).all() or not np.isfinite(dst).all():
   raise ValueError('같은 크기의 유한한 가닥 격자 두 개가 필요합니다. 최대256점입니다.')
  n=len(columns);triangles=[];orientation=None
  for row in range(len(src)-1):
   for col in range(n-1):
    a=row*n+col;b=a+1;c=a+n;d=c+1
    for tri in ([a,b,d],[a,d,c]):
     signs=[]
     for grid in (src,dst):
      pts=grid.reshape(-1,2)[tri];det=float(np.linalg.det(np.column_stack((pts[1]-pts[0],pts[2]-pts[0]))))
      if abs(det)<1e-6:raise ValueError('폭이 없거나 접힌 가닥 격자입니다.')
      signs.append(1 if det>0 else -1)
     if orientation is None:orientation=signs[0]
     if signs!=[orientation,orientation]:raise ValueError('가닥 진행 방향 또는 경계 순서가 뒤집혔습니다.')
     triangles.append(tri)
  surfaces.append({'id':name,'source':src.reshape(-1,2).tolist(),'target':dst.reshape(-1,2).tolist(),'triangles':triangles})
  tracks.append({'id':name,'columns':columns,'source_rows':src.tolist(),'target_rows':dst.tolist(),
                 'brush_direction':'row-progression','scope':'manual visible surface; full RGB carried along strand, not independent relighting'})
 return {'surfaces':surfaces},tracks
