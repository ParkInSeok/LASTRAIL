import unittest,tempfile,threading,json
from pathlib import Path
from PIL import Image,ImageChops
from refinement import LocalRefinement,validate_mask,refinement_request
from domain import new_project

class RefinementTests(unittest.TestCase):
 def test_loop_endpoint_copies_source_without_gpu(self):
  class Provider:
   def generate(self,*a,**kw):raise AssertionError('Endpoint must not run GPU')
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   root=Path(d);src=root/'source.png';Image.new('RGBA',(32,32),(10,20,30,80)).save(src)
   req={'preserve_endpoint':True,'reference_roles':{'pose':src.name}}
   result=LocalRefinement(Provider()).generate(req,[src],root/'job')
   self.assertEqual(result.read_bytes(),src.read_bytes())
 def test_endpoint_setting_is_explicit_in_request(self):
  p=new_project('loop');p['refs']['first']='first.png';p.update(refinement_mask='mask.png',refinement_prompt='hair',refinement_lock_endpoints=True)
  for f in p['frames']:f['versions']=[{'asset':'pose.png'}]
  self.assertTrue(refinement_request(p,0,42)['preserve_endpoint'])
  self.assertTrue(refinement_request(p,11,42)['preserve_endpoint'])
  self.assertFalse(refinement_request(p,5,42)['preserve_endpoint'])
  self.assertFalse(refinement_request(p,5,42)['silhouette_draft'])
  p['refinement_silhouette']=True
  self.assertTrue(refinement_request(p,5,42)['silhouette_draft'])
 def test_mask_rejects_empty_colored_oversize_and_wrong_canvas(self):
  for mask,size in [(Image.new('L',(20,20)),(20,20)),(Image.new('RGB',(20,20),'red'),(20,20)),(Image.new('L',(600,600),255),(600,600)),(Image.new('L',(20,20),255),(30,30))]:
   with self.assertRaises(ValueError):validate_mask(mask,size)
 def test_actual_patch_preserves_outside_and_alpha(self):
  class Provider:
   cancelled=threading.Event()
   def config(self):return {'output_node':'9','workflow':{'1':{'class_type':'CheckpointLoaderSimple'},'7':{'inputs':{}}}}
   def generate(self,req,refs,work,workflow_override=None):
    work.mkdir();p=work/'result.png';Image.new('RGB',(1024,1024),'red').save(p);return p
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   root=Path(d);src=root/'source.png';mp=root/'mask.png';im=Image.new('RGBA',(64,64),(20,80,150,120));im.putpixel((30,30),(20,80,150,255));im.save(src)
   im.putpixel((31,30),(20,80,150,254));im.putpixel((32,30),(20,80,150,249));im.save(src)
   mask=Image.new('L',(64,64));mask.paste(255,(20,20,40,40));mask.save(mp)
   req={'reference_roles':{'pose':src.name,'mask':mp.name},'prompt':'x'}
   output=LocalRefinement(Provider()).generate(req,[src,mp],root/'job');result=Image.open(output)
   self.assertEqual(result.getpixel((30,30)),(255,0,0,255));self.assertEqual(result.getpixel((0,0)),im.getpixel((0,0)))
   self.assertEqual(result.getpixel((25,25)),im.getpixel((25,25)))
   self.assertEqual(result.getpixel((31,30)),(255,0,0,254))
   self.assertEqual(result.getpixel((32,30)),im.getpixel((32,30)))
   self.assertEqual(result.getchannel('A').tobytes(),im.getchannel('A').tobytes())
   self.assertTrue(json.loads((root/'job/request.json').read_text())['outside_mask_exact'])
 def test_request_uses_current_frame_not_first(self):
  p=new_project('test');p['refs']['first']='first.png';p['refinement_mask']='mask.png';p['refinement_prompt']='hair';p['frames'][0]['versions']=[{'asset':'current.png'}]
  req=refinement_request(p,0,42);self.assertEqual(req['reference_roles'],{'pose':'current.png','mask':'mask.png'});self.assertEqual(req['seed'],42)
  p['frames'][0]['versions']=[]
  with self.assertRaises(ValueError):refinement_request(p,0,42)
 def test_wind_loop_endpoints_and_delayed_peak(self):
  p=new_project('wind');p['refs']['first']='first.png';p['refinement_mask']='mask.png';p['refinement_prompt']='hair';p['refinement_wind']=True
  for f in p['frames']:f['versions']=[{'asset':'pose.png'}]
  requests=[refinement_request(p,i,42) for i in range(12)]
  self.assertEqual(requests[0]['prompt'],requests[-1]['prompt']);self.assertEqual(requests[0]['refinement_denoise'],.16)
  peak=max(range(12),key=lambda i:requests[i]['motion_amount']);self.assertTrue(5<=peak<=6)
  p['refinement_lag']=2;delayed=[refinement_request(p,i,42) for i in range(12)];late=max(range(12),key=lambda i:delayed[i]['motion_amount'])
  self.assertGreater(late,peak);self.assertTrue(all(.16<=q['refinement_denoise']<=.55 for q in delayed));self.assertEqual(delayed[0]['prompt'],delayed[-1]['prompt'])
 def test_batch_refine_resume_keeps_done_versions(self):
  import io,time
  from repository import Repository
  from providers import ProviderRouter
  from service import Studio
  class Fake:
   def __init__(self):self.calls=[];self.fail=True
   def prepare(self):pass
   def cancel(self):pass
   def generate(self,req,refs,work):
    self.calls.append(req['frame_index'])
    if req['frame_index']==1 and self.fail:self.fail=False;raise ValueError('planned failure')
    return refs[0]
  def wait(s):
   end=time.monotonic()+5
   while s.active and time.monotonic()<end:time.sleep(.01)
   self.assertIsNone(s.active)
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   repo=Repository(Path(d));g=Fake();router=ProviderRouter(Path(d)/'engine.json',{'local':g});s=Studio(repo,router)
   p=new_project('resume');p['frames']=p['frames'][:2]
   b=io.BytesIO();Image.new('RGBA',(64,64),(50,100,150,255)).save(b,format='PNG');a=repo.put_asset(b.getvalue());p['refs']['first']=a
   for f in p['frames']:f['versions']=[{'asset':a,'checks':{'blocking':[]},'method':'imported'}]
   p.update(refinement_mask=a,refinement_prompt='tips');repo.save(p)
   s.action(p['id'],{'action':'refine_all'});wait(s);self.assertEqual(g.calls,[0,1]);self.assertEqual(repo.get(p['id'])['batch']['state'],'failed')
   s.action(p['id'],{'action':'refine_all'});wait(s);result=repo.get(p['id'])
   self.assertEqual(g.calls,[0,1,1]);self.assertEqual([len(f['versions']) for f in result['frames']],[2,2]);self.assertEqual(result['batch']['state'],'needs_review')
   self.assertTrue(all(f['versions'][-1]['method']=='local_masked_detail_draft' for f in result['frames']))

if __name__=='__main__':unittest.main()
