import unittest
import numpy as np
from PIL import Image,ImageDraw
from keypose_guides import KeyposeGuideBuilder,interpolate_coverage

class GuideTests(unittest.TestCase):
 def test_intermediate_outline_has_no_faded_old_or_new_silhouette(self):
  a=np.zeros((32,32),dtype='float32');b=a.copy();a[:,4:20]=1;b[:,12:28]=1
  out=interpolate_coverage(a,b,.5)
  self.assertEqual(float(out[16,5]),0);self.assertEqual(float(out[16,26]),0)
  self.assertGreater(float(out[16,16]),.99)
 def test_peak_preserves_invisible_target_rgb_too(self):
  a=Image.new('RGBA',(32,32),(1,2,3,255));b=Image.new('RGBA',(32,32),(50,60,70,0))
  self.assertEqual(KeyposeGuideBuilder().build(a,b,Image.new('L',a.size,255),1).tobytes(),b.tobytes())
 def test_endpoints_and_outside_mask_are_exact(self):
  a=Image.new('RGBA',(64,64),(30,40,50,100));b=Image.new('RGBA',(64,64),(90,80,70,255));m=Image.new('L',a.size);ImageDraw.Draw(m).rectangle((20,20,44,44),fill=255)
  g=KeyposeGuideBuilder()
  self.assertEqual(g.build(a,b,m,0).tobytes(),a.tobytes())
  out=np.asarray(g.build(a,b,m,1));src=np.asarray(a);sel=np.asarray(m)>0
  self.assertTrue(np.array_equal(out[~sel],src[~sel]));self.assertTrue(np.array_equal(out[sel],np.asarray(b)[sel]))
 def test_translation_guide_moves_shape_between_keys(self):
  a=Image.new('RGBA',(96,96));b=a.copy();ImageDraw.Draw(a).ellipse((20,30,40,50),fill=(200,130,60,255));ImageDraw.Draw(b).ellipse((36,30,56,50),fill=(200,130,60,255))
  out=np.asarray(KeyposeGuideBuilder().build(a,b,Image.new('L',a.size,255),.5));y,x=np.where(out[:,:,3]>128)
  self.assertTrue(33<float(x.mean())<43,float(x.mean()))
 def test_invalid_amount_and_size_rejected(self):
  a=Image.new('RGBA',(32,32));g=KeyposeGuideBuilder()
  with self.assertRaises(ValueError):g.build(a,a,Image.new('L',a.size),1.1)
  with self.assertRaises(ValueError):g.build(a,Image.new('RGBA',(16,16)),Image.new('L',a.size),.5)
