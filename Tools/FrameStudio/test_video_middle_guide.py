import io,unittest
from PIL import Image
from video_generation import validate_request,workflow
import test_video_service

class MiddleGuideContractTests(unittest.TestCase):
 def setUp(self):
  self.media={'first':{'kind':'image'},'middle':{'kind':'image'},'audio':{'kind':'audio'}}
  self.req={'asset':'first','prompt':'One command gesture','seed':20261022,'profile':'idle_reference'}
  self.guide={'asset':'middle','frame_index':24,'strength':1.0}
 def test_registered_middle_guide_reaches_sampler_and_crop(self):
  r=validate_request(self.req|{'middle_guide':self.guide},self.media)
  self.assertEqual(r.get('middle_guide'),self.guide)
  w=workflow(r,'start.png','job',middle_image='middle.png')
  self.assertEqual(w['16']['inputs']['image'],'middle.png')
  self.assertEqual(w['15']['inputs']['image'],['16',0])
  self.assertEqual(w['15']['inputs']['frame_idx'],24)
  self.assertEqual(w['15']['inputs']['positive'],['13',0])
  self.assertEqual(w['10']['inputs']['latent_image'],['15',2])
  self.assertEqual(w['14']['inputs']['positive'],['15',0])
  self.assertEqual(w['11']['inputs']['samples'],['14',2])
  self.assertEqual(w['6']['inputs']['length'],49)
 def test_invalid_middle_guide_rejected_not_ignored(self):
  for update in [{'asset':'audio'},{'asset':'../outside'},{'frame_index':0},{'frame_index':48},{'frame_index':True},{'strength':float('nan')},{'strength':True},{'strength':0},{'strength':1.1}]:
   with self.subTest(update=update),self.assertRaises(ValueError):validate_request(self.req|{'middle_guide':self.guide|update},self.media)
 def test_absence_keeps_existing_profiles_and_missing_upload_fails(self):
  for profile in ['standard','idle_reference','wind_reference']:
   r=validate_request(self.req|{'profile':profile},self.media)
   self.assertNotIn('middle_guide',r);self.assertNotIn('15',workflow(r,'start.png','job'))
  r=validate_request(self.req|{'middle_guide':self.guide},self.media)
  with self.assertRaises(ValueError):workflow(r,'start.png','job')

class MiddleGuideServiceTests(test_video_service.JobTests):
 def test_uploads_middle_and_rejects_size_before_submitting(self):
  def add(size,color):
   b=io.BytesIO();Image.new('RGB',size,color).save(b,format='PNG');return self.cut.import_media(self.pid,io.BytesIO(b.getvalue()),len(b.getvalue()),'guide.png')['id']
  first=add((288,512),'red');middle=add((288,512),'blue');uploaded=[]
  def upload(path):uploaded.append(path);return path.name
  self.provider.upload=upload
  req=self.req|{'asset':first,'profile':'idle_reference','middle_guide':{'asset':middle,'frame_index':24,'strength':1.0}}
  self.service.start(self.pid,req);self.assertTrue(self.provider.sent.wait(2))
  self.assertEqual(len(uploaded),2)
  graph=next(d['prompt'] for p,d in self.provider.calls if p=='/prompt')
  self.assertEqual(graph['16']['inputs']['image'],uploaded[1].name)
  self.service.cancel(self.pid);self.wait('cancelled');self.provider.sent.clear()
  req['middle_guide']['asset']=add((64,64),'blue')
  with self.assertRaises(ValueError):self.service.start(self.pid,req)
  self.assertFalse(self.provider.sent.is_set())

if __name__=='__main__':unittest.main()
