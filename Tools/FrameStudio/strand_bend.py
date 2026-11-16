"""Length-constrained 2D centerline guides; not individual hair simulation."""
import numpy as np

class StrandBend:
 def __init__(self,points):
  self.source=np.asarray(points,dtype=np.float64)
  if self.source.ndim!=2 or self.source.shape[1]!=2 or len(self.source)<3:raise ValueError('A centerline needs at least three points')
  delta=np.diff(self.source,axis=0);self.lengths=np.linalg.norm(delta,axis=1)
  if not np.all(np.isfinite(self.source)) or np.any(self.lengths<1e-6):raise ValueError('Invalid centerline')
  self.angles=np.arctan2(delta[:,1],delta[:,0])

 def pose(self,phase,amplitude=.16,root_falloff=1.):
  if not 0<=phase<=1:raise ValueError('Phase outside loop')
  if not np.isfinite(root_falloff) or root_falloff<=0:raise ValueError('Positive root falloff required')
  if phase in (0,1):return self.source.copy()
  s=np.linspace(0,1,len(self.lengths))
  envelope=np.sin(np.pi*phase)**2
  # A smooth travelling bend. Distances never come from displacement scaling.
  rotation=amplitude*envelope*s**root_falloff*np.sin(2*np.pi*phase-3*s)
  angles=self.angles+rotation
  steps=self.lengths[:,None]*np.column_stack((np.cos(angles),np.sin(angles)))
  return np.vstack((self.source[0],self.source[0]+np.cumsum(steps,axis=0)))

 def inverse_maps(self,target,size):
  # Continuous cross-sections avoid nearest-segment ownership discontinuities.
  if np.any(np.diff(target[:,1])<=0):raise ValueError('Guide must remain monotone in screen y')
  width,height=size;yy,xx=np.mgrid[:height,:width].astype('float32')
  rows=np.arange(height);arc=np.r_[0,np.cumsum(self.lengths)]
  parameter=np.interp(rows,target[:,1],arc)
  tx=np.interp(parameter,arc,target[:,0]);sx=np.interp(parameter,arc,self.source[:,0]);sy=np.interp(parameter,arc,self.source[:,1])
  mx=xx+(sx-tx)[:,None];my=np.broadcast_to(sy[:,None],xx.shape).copy()
  outside=(rows<target[0,1])|(rows>target[-1,1])
  mx[outside]=xx[outside];my[outside]=yy[outside]
  return mx.astype('float32'),my.astype('float32')
