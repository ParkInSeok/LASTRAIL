import unittest
import numpy as np
from PIL import Image,ImageFilter
from detail_finish import finish_detail

class DetailTests(unittest.TestCase):
 def test_preserves_mask_alpha_and_black_and_caps_change(self):
  im=Image.new('RGBA',(64,64),(0,0,0,255));im.paste((100,80,60,255),(12,12,52,52));im=im.filter(ImageFilter.GaussianBlur(1))
  mask=Image.new('L',im.size);mask.paste(255,(0,0,32,64));result,report=finish_detail(im,mask)
  self.assertEqual(result.crop((32,0,64,64)).tobytes(),im.crop((32,0,64,64)).tobytes());self.assertEqual(result.getchannel('A').tobytes(),im.getchannel('A').tobytes());self.assertEqual(result.getpixel((0,0)),im.getpixel((0,0)));self.assertGreater(report['changed_pixels'],0);self.assertLessEqual(report['max_channel_delta'],10)
 def test_uniform_field_does_not_gain_detail(self):
  im=Image.new('RGB',(32,32),(80,60,40));result,report=finish_detail(im,Image.new('L',im.size,255));self.assertEqual(result.convert('RGB').tobytes(),im.tobytes());self.assertEqual(report['changed_pixels'],0)

if __name__=='__main__':unittest.main()
