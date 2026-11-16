import io,unittest
from unittest.mock import patch
from PIL import Image
from video_generation import validate_request,workflow
import test_video_service

class CanvasContractTests(unittest.TestCase):
 def setUp(self):
  self.media={'first':{'kind':'image'}};self.req={'asset':'first','prompt':'A single command','seed':20261022}
 def test_explicit_supported_canvas_reaches_workflow_without_changing_defaults(self):
  legacy=validate_request(self.req,self.media)
  self.assertEqual((legacy['width'],legacy['height']),(288,512))
  wide=validate_request(self.req|{'width':384,'height':512},self.media)
  self.assertEqual((wide['width'],wide['height']),(384,512))
  graph=workflow(wide,'input.png','job')
  self.assertEqual((graph['6']['inputs']['width'],graph['6']['inputs']['height']),(384,512))
  self.assertEqual(wide['frames'],49)
 def test_unsupported_or_noninteger_canvas_rejected_not_silently_resized(self):
  for dimensions in [{'width':512,'height':512},{'width':384,'height':384},{'width':True},{'width':384.0},{'height':'512'},{'width':None},{'height':0}]:
   with self.subTest(dimensions=dimensions),self.assertRaises(ValueError):validate_request(self.req|dimensions,self.media)

class CanvasServiceTests(test_video_service.JobTests):
 def add_image(self,size):
  b=io.BytesIO();Image.new('RGB',size).save(b,format='PNG');return self.cut.import_media(self.pid,io.BytesIO(b.getvalue()),len(b.getvalue()),'input.png')['id']
 def test_wide_middle_inputs_upload_at_requested_size(self):
  first=self.add_image((384,512));middle=self.add_image((384,512))
  req=self.req|{'asset':first,'width':384,'height':512,'middle_guide':{'asset':middle,'frame_index':24,'strength':1.0}}
  self.service.start(self.pid,req);self.assertTrue(self.provider.sent.wait(2))
  graph=next(d['prompt'] for p,d in self.provider.calls if p=='/prompt')
  self.assertEqual(graph['6']['inputs']['width'],384)
  self.service.cancel(self.pid);self.wait('cancelled')
 def test_wrong_wide_input_is_rejected_before_provider_submission(self):
  req=self.req|{'asset':self.add_image((288,512)),'width':384,'height':512}
  with self.assertRaises(ValueError):self.service.start(self.pid,req)
  self.assertFalse(self.provider.sent.is_set())
 def run_output_case(self,size):
  images=[Image.new('RGB',size,(i*5,10,20)) for i in range(49)];out=io.BytesIO();images[0].save(out,format='WEBP',save_all=True,append_images=images[1:],duration=63,lossless=True)
  original=self.provider.json
  def response(path,data=None):
   if path.startswith('/history/'):
    return {'owned-job':{'outputs':{'12':{'images':[{'filename':'out.webp','subfolder':'','type':'output'}]}}}}
   return original(path,data)
  self.provider.json=response;self.provider.call=lambda path:out.getvalue()
  req=self.req|{'asset':self.add_image((384,512)),'width':384,'height':512}
  with patch('video_service.ffmpeg',side_effect=RuntimeError('postprocess-marker')):
   self.service.start(self.pid,req);return self.wait('failed')
 def test_matching_wide_output_saved_before_postprocess_failure(self):
  job=self.run_output_case((384,512));self.assertEqual(job['message'],'postprocess-marker')
  folder=self.cut.folder(self.pid)/'ai-video'/job['id'];self.assertEqual(len(list(folder.glob('[0-9][0-9][0-9].png'))),49)
  self.assertIsNone(job['media'])
 def test_wrong_output_size_rejected_and_raw_preserved(self):
  job=self.run_output_case((288,512));self.assertIn('규격',job['message'])
  folder=self.cut.folder(self.pid)/'ai-video'/job['id'];self.assertTrue((folder/'generated.webp').exists());self.assertFalse((folder/'000.png').exists())

if __name__=='__main__':unittest.main()
