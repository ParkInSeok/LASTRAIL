import unittest, tempfile, io
from pathlib import Path
from PIL import Image
from domain import new_project, readiness, approve_character, build_request, set_schedule, replace_reference, approve_frame
from repository import Repository
from imaging import inspect_png, export_clip

class StudioTests(unittest.TestCase):
 def ready(self):
  p=new_project('테스트')
  for role in ('front','back','left','right','first'):replace_reference(p,role,role+'.png')
  p['identity']='오른손 공구';p['motion']='한 번 발사';p['hidden']='추가 숨은면 없음: 고정시점'
  approve_character(p);return p
 def test_incomplete_cannot_approve(self):
  p=new_project('Eve');self.assertGreater(len(readiness(p)),3)
  with self.assertRaises(ValueError):approve_character(p)
 def test_invalid_frame_indices_rejected(self):
  p=self.ready()
  for i in (-1,12):
   with self.assertRaises(ValueError):build_request(p,i)
   with self.assertRaises(ValueError):approve_frame(p,i)
 def test_api_credentials_removed_from_child(self):
  from unittest.mock import patch
  from codex_provider import subscription_env
  with patch.dict('os.environ',{'OPENAI_API_KEY':'dummy','CODEX_API_KEY':'dummy','OPENAI_BASE_URL':'http://invalid'}):
   env=subscription_env()
   self.assertNotIn('OPENAI_API_KEY',env);self.assertNotIn('CODEX_API_KEY',env);self.assertNotIn('OPENAI_BASE_URL',env)
 def test_reference_change_revokes_approval(self):
  p=self.ready();replace_reference(p,'first','changed.png');self.assertFalse(p['character_approved'])
 def test_inbetween_requires_two_approved_keys(self):
  p=self.ready();set_schedule(p,[{'duration_ms':120,'phase':'준비','kind':'key'},{'duration_ms':120,'phase':'사이','kind':'between'},{'duration_ms':120,'phase':'반동','kind':'key'}])
  with self.assertRaises(ValueError):build_request(p,1)
  for i in (0,2):p['frames'][i]['versions']=[{'asset':f'{i}.png','checks':{'blocking':[]}}];approve_frame(p,i)
  q=build_request(p,1);self.assertIn('0.png',q['reference_assets']);self.assertIn('2.png',q['reference_assets']);self.assertIn('first.png',q['reference_assets'])
 def test_bad_duration_and_phase(self):
  for v in (0,-1,15,'120'):
   with self.assertRaises(ValueError):set_schedule(new_project('x'),[{'duration_ms':v,'phase':'x','kind':'key'}])
 def test_bad_pixels_not_approved(self):
  p=self.ready();p['frames'][0]['versions']=[{'asset':'x','checks':{'blocking':['투명도 없음']}}]
  with self.assertRaises(ValueError):approve_frame(p,0)
 def test_paths_and_persistence(self):
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   r=Repository(Path(d));p=new_project('한글');r.save(p);self.assertEqual(r.get(p['id'])['name'],'한글')
   with self.assertRaises(ValueError):r.asset_path('../outside.png')
 def test_alpha_and_size_validation(self):
  im=Image.new('RGBA',(64,64),(0,0,0,0));im.paste((100,80,70,255),(20,20,40,40));b=io.BytesIO();im.save(b,format='PNG')
  self.assertFalse(inspect_png(b.getvalue(),[64,64])['blocking'])
  self.assertTrue(inspect_png(b.getvalue(),[32,32])['blocking'])
  opaque=io.BytesIO();Image.new('RGB',(64,64)).save(opaque,format='PNG');self.assertTrue(inspect_png(opaque.getvalue())['blocking'])
 def test_export_timing_and_source_preservation(self):
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   root=Path(d);imgs=[]
   for i in range(3):
    im=Image.new('RGBA',(64,64));im.paste((160,70+i*10,30,255),(20,20,40,40));f=root/f'{i}.png';im.save(f);imgs.append(f)
   out=export_clip(imgs,[100,150,200],root/'out')
   with Image.open(out['apng']) as lossless:
    self.assertEqual(lossless.n_frames,3);self.assertEqual(lossless.info['loop'],0)
    for i,path in enumerate(imgs):
     lossless.seek(i)
     with Image.open(path) as original:self.assertEqual(lossless.convert('RGBA').tobytes(),original.convert('RGBA').tobytes())
   g=Image.open(out['gif']);total=0
   for i in range(g.n_frames):g.seek(i);total+=g.info['duration']
   self.assertEqual(total,450);g.close();self.assertTrue(Path(out['sheet']).exists())

if __name__=='__main__':unittest.main()
