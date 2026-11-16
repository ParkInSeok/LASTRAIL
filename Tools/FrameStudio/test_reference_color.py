import unittest
from PIL import Image
class BackgroundOffsetTests(unittest.TestCase):
 def test_calibrates_uniform_decoder_bias_without_blur(self):
  from reference_color import calibrate_background
  a=Image.new('RGB',(32,32),(40,60,80));b=Image.new('RGB',(32,32),(33,51,75));b.putpixel((10,10),(100,120,140));m=Image.new('L',(32,32));m.paste(255,(8,8,16,16))
  out,report=calibrate_background(a,b,m,(20,20,30,30));self.assertEqual(report['offset_rgb'],[7,9,5]);self.assertEqual(out.getpixel((20,20)),(40,60,80));self.assertEqual(out.getpixel((10,10)),(107,129,145));self.assertEqual(out.getpixel((9,10)),(40,60,80))
 def test_rejects_mask_overlap_and_textured_anchor(self):
  from reference_color import calibrate_background
  a=Image.new('RGB',(32,32),(40,60,80));m=Image.new('L',(32,32));m.paste(255,(8,8,16,16))
  with self.assertRaises(ValueError):calibrate_background(a,a,m,(10,10,20,20))
  a.putpixel((25,25),(255,0,0))
  with self.assertRaises(ValueError):calibrate_background(a,a,m,(20,20,30,30))
if __name__=='__main__':unittest.main()
