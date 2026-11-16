"""Screen-projected artistic wind guide, not a cloth physics simulation.

The adopted still represents steady airflow. Gust displacement adds rearward
pressure followed by elastic recovery; transverse flutter stays secondary.
"""
import math

class TrainWindProfile:
 def __init__(self,train_forward=(0,-1)):
  x,y=train_forward;length=math.hypot(x,y)
  if not math.isfinite(length) or length==0:raise ValueError('Finite nonzero train direction required')
  self.rear=(-x/length,-y/length)
  self.side=(-self.rear[1],self.rear[0])

 def displacement(self,phase,amplitude,delay=0,flutter=.12):
  if not 0<=phase<=1 or amplitude<0 or not 0<=delay<.5 or not 0<=flutter<=.25:
   raise ValueError('Invalid wind timing or amplitude')
  # Delayed pressure, smoothly at rest at both loop ends.
  u=(phase-delay)/(.94-delay)
  if u<=0 or u>=1:return (0.,0.)
  pressure=math.sin(math.pi*u)**2
  rear=amplitude*pressure
  side=amplitude*flutter*pressure*math.sin(2*math.pi*u)
  return tuple(rear*a+side*b for a,b in zip(self.rear,self.side))
