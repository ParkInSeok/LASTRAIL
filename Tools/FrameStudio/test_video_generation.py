import unittest
from video_generation import validate_request, workflow

class VideoRequestTests(unittest.TestCase):
 def setUp(self):
  self.media={'image1':{'kind':'image','file':'a.png'},'audio1':{'kind':'audio','file':'a.wav'}}
  self.request={'asset':'image1','prompt':'A woman reaches forward.','seed':42}
 def test_registered_image_and_limits(self):
  r=validate_request(self.request,self.media)
  self.assertEqual((r['width'],r['height'],r['frames']),(288,512,49))
  for update in [{'asset':'../secret'},{'asset':'audio1'},{'prompt':''},{'prompt':'x'*4001},{'seed':-1},{'seed':True}]:
   with self.subTest(update=update),self.assertRaises(ValueError):validate_request(self.request|update,self.media)
 def test_workflow_uses_actual_prompt_image_and_seed(self):
  r=validate_request(self.request,self.media);w=workflow(r,'uploaded.png','job123')
  self.assertEqual(w['3']['inputs']['text'],self.request['prompt'])
  self.assertEqual(w['5']['inputs']['image'],'uploaded.png')
  self.assertEqual(w['10']['inputs']['noise_seed'],42)
  self.assertEqual(w['6']['inputs']['length'],49)
  self.assertIn('SaveAnimatedWEBP',[v['class_type'] for v in w.values()])
 def test_idle_profile_reuses_reference_at_end_and_removes_guide_before_decode(self):
  r=validate_request(self.request|{'profile':'idle_reference'},self.media);w=workflow(r,'uploaded.png','job')
  self.assertEqual(w['13']['inputs']['frame_idx'],48)
  self.assertEqual(w['13']['inputs']['image'],['5',0])
  self.assertEqual(w['10']['inputs']['latent_image'],['13',2])
  self.assertEqual(w['14']['class_type'],'LTXVCropGuides')
  self.assertEqual(w['11']['inputs']['samples'],['14',2])
  with self.assertRaises(ValueError):validate_request(self.request|{'profile':'unknown'},self.media)
 def test_wind_profile_preserves_start_but_relaxes_end_only(self):
  r=validate_request(self.request|{'profile':'wind_reference'},self.media);w=workflow(r,'uploaded.png','job')
  self.assertEqual(w['6']['inputs']['strength'],1.0)
  self.assertEqual(w['13']['inputs']['strength'],0.65)
  self.assertEqual(w['11']['inputs']['samples'],['14',2])

if __name__=='__main__':unittest.main()
