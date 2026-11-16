import unittest
from PIL import Image,ImageDraw
from rigid_finish import RigidProcessor

class RigidTests(unittest.TestCase):
 def setup_processor(self,continuous=False):
  im=Image.new('RGBA',(96,96));ImageDraw.Draw(im).rectangle((15,35,70,55),fill=(210,130,60,255))
  mask=Image.new('L',im.size);ImageDraw.Draw(mask).rectangle((10,25,50,65),fill=255)
  config={'pivot':[50,45],'joint_radius':8,'angle_keys':[[0,0],[80,-.5],[160,0]],'thumb_box':[16,35,10,10],'thumb_keys':[[0,0],[80,1],[160,0]],'thumb_reference':1}
  config['continuous_joint']=continuous
  return RigidProcessor([im,im],mask,0,config,160)
 def test_continuous_joint_stays_connected_and_protects_body(self):
  import cv2,numpy as np
  p=self.setup_processor(True)
  for t in range(0,160,10):
   im=p.at(t);alpha=np.array(im)[:,:,3]
   count,_=cv2.connectedComponents((alpha>128).astype('uint8'))
   self.assertEqual(count,2)
   self.assertEqual(im.getpixel((65,45)),(210,130,60,255))
  self.assertEqual(p.at(0).tobytes(),p.at(160).tobytes())
 def test_loop_protection_and_no_scale(self):
  p=self.setup_processor()
  self.assertEqual(p.at(0).tobytes(),p.at(160).tobytes())
  self.assertEqual(p.at(40).getpixel((65,45)),(210,130,60,255))
  self.assertEqual(p.at(40).size,(96,96))
 def test_invalid_timeline(self):
  with self.assertRaises(ValueError):RigidProcessor([Image.new('RGBA',(96,96))],Image.new('L',(96,96),255),0,{'pivot':[0,0]},160)
 def test_local_thumb_transition_has_intermediate_coverage(self):
  im=Image.new('RGBA',(96,96));ImageDraw.Draw(im).rectangle((15,35,30,50),fill=(200,100,50,255))
  target=Image.new('RGBA',im.size);mask=Image.new('L',im.size,255)
  cfg={'pivot':[50,45],'joint_radius':0,'angle_keys':[[0,0],[160,0]],'thumb_box':[10,30,30,30],'thumb_keys':[[0,0],[80,1],[160,0]],'thumb_reference':1}
  p=RigidProcessor([im,target],mask,0,cfg,160)
  self.assertEqual(p.at(0).getpixel((22,42))[3],255)
  self.assertTrue(110<p.at(40).getpixel((22,42))[3]<145)
  self.assertEqual(p.at(80).getpixel((22,42))[3],0)
 def test_fixed_side_does_not_duplicate_into_moving_space(self):
  import numpy as np
  im=Image.new('RGBA',(96,96));ImageDraw.Draw(im).rectangle((56,4,80,30),fill=(255,0,0,255))
  mask=Image.new('L',im.size);ImageDraw.Draw(mask).rectangle((10,0,50,60),fill=255)
  cfg={'pivot':[50,45],'joint_radius':0,'continuous_joint':True,'angle_keys':[[0,0],[80,-.9],[160,0]],'thumb_box':[10,35,10,10],'thumb_keys':[[0,0],[160,0]],'thumb_reference':0}
  p=RigidProcessor([im],mask,0,cfg,160)
  for t in range(0,160,10):self.assertFalse(np.any(np.array(p.at(t))[:,:55,3]>0))
