import unittest,tempfile,json,threading
from pathlib import Path
from providers import ProviderRouter
from local_provider import render_workflow,LocalProvider

class Fake:
 def status(self):return {'ready':True,'message':'test'}
 def generate(self,*args):raise RuntimeError('original failure')
 def cancel(self):pass

class ProviderTests(unittest.TestCase):
 def test_selection_persists_without_touching_projects(self):
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   p=Path(d)/'settings.json';r=ProviderRouter(p,{'local':Fake(),'codex':Fake()})
   self.assertEqual(r.selected,'local');r.select('codex')
   self.assertEqual(ProviderRouter(p,{'local':Fake(),'codex':Fake()}).selected,'codex')
   with self.assertRaises(ValueError):r.select('paid-api')
 def test_failure_does_not_fallback_and_switch_blocked(self):
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   r=ProviderRouter(Path(d)/'settings.json',{'local':Fake(),'codex':Fake()})
   r.reserve()
   with self.assertRaises(ValueError):r.select('codex')
   with self.assertRaisesRegex(RuntimeError,'original failure'):r.generate({},[],Path(d))
   r.release();self.assertEqual(r.selected,'local')
 def test_template_exact_substitution(self):
  w={'1':{'class_type':'LoadImage','inputs':{'image':'{{ref:first}}'}},'2':{'class_type':'CLIPTextEncode','inputs':{'text':'{{prompt}}'}}}
  result=render_workflow(w,{'prompt':'quote " and newline\n','ref:first':'x.png'})
  self.assertEqual(result['2']['inputs']['text'],'quote " and newline\n')
  self.assertEqual(w['1']['inputs']['image'],'{{ref:first}}')
  with self.assertRaises(ValueError):render_workflow(w,{'prompt':'x'})
 def test_unsupported_references_rejected_before_upload(self):
  from unittest.mock import Mock
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   provider=LocalProvider(Path(__file__).parent);provider.upload=Mock()
   req={'id':'12345678','prompt':'x','reference_roles':{'first':'first.png','back':'back.png'},'reference_assets':['first.png','back.png']}
   with self.assertRaisesRegex(ValueError,'back'):provider.generate(req,[Path('first.png'),Path('back.png')],Path(d))
   provider.upload.assert_not_called()
 def test_prepare_failure_releases_reservation(self):
  from unittest.mock import Mock
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   bad=Fake();bad.prepare=Mock(side_effect=ValueError('offline'))
   r=ProviderRouter(Path(d)/'settings.json',{'local':bad,'codex':Fake()})
   with self.assertRaises(ValueError):r.reserve()
   r.select('codex');self.assertFalse(r.info()['busy'])
 def test_draft_cannot_be_approved_even_if_pixels_pass(self):
  import io
  from PIL import Image
  from repository import Repository
  from domain import new_project,approve_frame
  from service import Studio
  from unittest.mock import Mock
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   root=Path(d);repo=Repository(root);p=new_project('draft')
   im=Image.new('RGBA',(64,64));im.paste((50,100,150,255),(20,20,40,40));b=io.BytesIO();im.save(b,format='PNG')
   asset=repo.put_asset(b.getvalue());p['refs']['first']=asset;p['character_approved']=True
   req={'id':'test','revision':p['revision'],'reference_assets':[asset],'frame_index':0,'engine':'local','draft':True}
   p['requests'].append(req);repo.save(p)
   provider=Mock();provider.generate.return_value=repo.asset_path(asset)
   Studio(repo,provider).run(p['id'],req)
   result=repo.get(p['id']);self.assertEqual(result['requests'][0]['status'],'needs_review')
   with self.assertRaises(ValueError):approve_frame(result,0)
   provider.release.assert_called_once()

if __name__=='__main__':unittest.main()
