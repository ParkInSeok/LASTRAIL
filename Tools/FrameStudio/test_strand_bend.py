import unittest
import numpy as np
from strand_bend import StrandBend

class StrandBendTests(unittest.TestCase):
 def test_length_root_and_loop_preserved(self):
  bend=StrandBend([(50,0),(55,20),(48,40),(50,60)])
  for phase in np.linspace(0,1,61):
   pose=bend.pose(phase,.3)
   np.testing.assert_allclose(np.linalg.norm(np.diff(pose,axis=0),axis=1),bend.lengths,atol=1e-10)
   np.testing.assert_array_equal(pose[0],bend.source[0])
  np.testing.assert_array_equal(bend.pose(0),bend.pose(1))
 def test_rest_maps_preserve_width_and_position(self):
  bend=StrandBend([(30,0),(30,30),(30,60)])
  x,y=bend.inverse_maps(bend.source,(60,60))
  yy,xx=np.mgrid[:60,:60]
  np.testing.assert_allclose(x,xx,atol=1e-5);np.testing.assert_allclose(y,yy,atol=1e-5)
 def test_curve_actually_changes(self):
  bend=StrandBend([(10,0),(10,30),(10,60),(10,90)])
  self.assertGreater(np.max(abs(bend.pose(.5)-bend.source)),1)
 def test_broader_bend_preserves_lengths_and_fixed_attachment(self):
  bend=StrandBend([(0,y) for y in range(0,501,20)])
  for phase in np.linspace(0,1,12):
   pose=bend.pose(phase,.6,.5)
   np.testing.assert_allclose(np.linalg.norm(np.diff(pose,axis=0),axis=1),bend.lengths,atol=1e-10)
   np.testing.assert_array_equal(pose[0],bend.source[0])
   self.assertTrue(np.all(np.diff(pose[:,1])>0))
  self.assertGreater(abs(bend.pose(.5,.6,.5)[8,0]),abs(bend.pose(.5,.2)[8,0])*2)
