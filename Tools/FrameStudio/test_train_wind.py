import unittest
from train_wind import TrainWindProfile

class TrainWindTests(unittest.TestCase):
 def test_forward_up_pushes_down_with_small_side_flutter(self):
  w=TrainWindProfile((0,-1))
  for i in range(101):
   x,y=w.displacement(i/100,20,.08)
   self.assertGreaterEqual(y,0)
   self.assertLessEqual(abs(x),.12*y+1e-9)
 def test_rotating_train_rotates_wind(self):
  x,y=TrainWindProfile((1,0)).displacement(.47,20)
  self.assertLess(x,0);self.assertAlmostEqual(y,0)
 def test_delayed_tip_and_closed_loop(self):
  w=TrainWindProfile()
  self.assertGreater(w.displacement(.1,20)[1],0)
  self.assertEqual(w.displacement(.1,20,.15),(0,0))
  for delay in (0,.08,.15):
   self.assertEqual(w.displacement(0,20,delay),(0,0))
   self.assertEqual(w.displacement(1,20,delay),(0,0))
 def test_invalid_direction_rejected(self):
  for v in ((0,0),(float('nan'),1)):
   with self.assertRaises(ValueError):TrainWindProfile(v)
