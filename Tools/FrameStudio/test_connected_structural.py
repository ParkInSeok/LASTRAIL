import tempfile,threading,unittest
from pathlib import Path
from PIL import Image,ImageDraw
from connected_structural import ConnectedStructuralEdit
from refinement import validate_mask

class FakeProvider:
 def __init__(self):self.calls=[];self.cancelled=threading.Event();self.output_size=(1254,1254)
 def generate(self,request,refs,work,workflow_override=None):
  self.calls.append((request,refs,workflow_override));work.mkdir(parents=True)
  Image.new('RGB',self.output_size,(200,100,50)).save(work/'result.png');return work/'result.png'

class ConnectedTests(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
  self.original=Image.new('RGBA',(1254,1254),(13,27,39,255));self.mask=Image.new('L',(1254,1254));ImageDraw.Draw(self.mask).rectangle((220,280,920,765),fill=255)
  self.protected=Image.new('L',(1254,1254));ImageDraw.Draw(self.protected).rectangle((0,0,1253,265),fill=255)
  self.lineart=self.root/'lineart.png';Image.new('RGB',(1280,1280),'black').save(self.lineart);self.provider=FakeProvider()
 def run_edit(self,**settings):return ConnectedStructuralEdit(self.provider).generate(dict(id='abc12345',prompt='test',seed=20261028,**settings),self.original,self.mask,self.protected,self.lineart,self.root/'job')
 def test_requested_denoise_reaches_sampler(self):
  self.run_edit(denoise=0.10)
  self.assertEqual(self.provider.calls[0][2]['workflow']['7']['inputs']['denoise'],0.10)
 def test_invalid_denoise_rejected_before_generation(self):
  for value in [0,1.1,float('nan'),True,'0.1']:
   with self.assertRaises(ValueError):self.run_edit(denoise=value)
  self.assertEqual(self.provider.calls,[])
 def test_large_region_preserves_outside_and_consumes_noise_mask(self):
  result=self.run_edit();im=Image.open(result)
  self.assertEqual(im.getpixel((500,150)),(13,27,39,255));self.assertEqual(im.getpixel((300,400)),(200,100,50,255));self.assertEqual(im.getpixel((900,1230)),(13,27,39,255))
  request,refs,cfg=self.provider.calls[0];g=cfg['workflow'];latent=g['7']['inputs']['latent_image'];self.assertEqual(g[latent[0]]['class_type'],'SetLatentNoiseMask')
  masknode=g[latent[0]]['inputs']['mask'][0];self.assertEqual(g[masknode]['class_type'],'ImageToMask');self.assertIn('mask',request['reference_roles'])
  with self.assertRaises(ValueError):validate_mask(self.mask,self.original.size)
 def test_protected_overlap_rejected_before_generation(self):
  self.mask.putpixel((500,150),255)
  with self.assertRaises(ValueError):self.run_edit()
  self.assertEqual(self.provider.calls,[])
 def test_gray_mask_rejected(self):
  self.mask.putpixel((500,500),128)
  with self.assertRaises(ValueError):self.run_edit()
 def test_wrong_output_size_rejected_without_resizing(self):
  self.provider.output_size=(512,512)
  with self.assertRaises(ValueError):self.run_edit()

if __name__=='__main__':unittest.main()
