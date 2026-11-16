import base64, io, tempfile, unittest
from pathlib import Path
from PIL import Image
from domain import new_project
from repository import Repository
from service import Studio

class DeliveryReviewTests(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
  self.repo=Repository(Path(self.temp.name));self.studio=Studio(self.repo,object())
  p=new_project('review');p['frames']=p['frames'][:2]
  for i,f in enumerate(p['frames']):
   im=Image.new('RGBA',(64,64));im.paste((100+i*60,30,70,255),(20,10,40,54));b=io.BytesIO();im.save(b,format='PNG')
   f['versions']=[{'asset':self.repo.put_asset(b.getvalue()),'checks':{'blocking':[]}}]
  self.repo.save(p);self.pid=p['id']
  b=io.BytesIO();Image.new('RGB',(160,180),'gray').save(b,format='PNG');self.bg=base64.b64encode(b.getvalue()).decode()
 def action(self,action,**kw):return self.studio.action(self.pid,{'action':action,**kw})
 def prepare(self):
  self.action('battle_settings',png=self.bg,anchor=[.5,.7],height=50)
  return self.action('battle_export')
 def test_visual_approval_independent_and_stales_on_change(self):
  self.prepare();p=self.action('approve_clip')
  self.assertTrue(p['clip_approved']);self.assertFalse(p['character_approved'])
  p=self.action('export',approved_only=True);self.assertEqual(p['export']['status'],'approved')
  p=self.action('retime',durations=[130,130]);self.assertFalse(p['clip_approved']);self.assertFalse(p['battle_current'])
  with self.assertRaises(ValueError):self.action('export',approved_only=True)
 def test_approval_requires_current_battle(self):
  with self.assertRaises(ValueError):self.action('approve_clip')
  self.prepare();self.action('battle_settings',anchor=[.6,.7],height=50)
  with self.assertRaises(ValueError):self.action('approve_clip')
 def test_bad_settings_and_blocked_frames(self):
  for anchor,height in [([float('nan'),.5],50),([1.2,.5],50),([.5,.5],0)]:
   with self.assertRaises(ValueError):self.action('battle_settings',png=self.bg,anchor=anchor,height=height)
  self.prepare();p=self.repo.get(self.pid);p['frames'][0]['versions'][-1]['checks']['blocking']=['opaque'];self.repo.save(p)
  with self.assertRaises(ValueError):self.action('approve_clip')
 def test_saved_composite_and_reload(self):
  p=self.prepare();im=Image.open(self.repo.asset_path(p['battle_export']['apng']))
  self.addCleanup(im.close)
  self.assertEqual(im.size,(160,180));self.assertEqual(im.n_frames,2);self.assertEqual(im.info['loop'],0)
  self.assertEqual(im.convert('RGB').getpixel((80,105)),(100,30,70))
  p=self.action('approve_clip');fresh=Studio(self.repo,object()).view(self.repo.get(self.pid))
  self.assertTrue(fresh['clip_approved']);self.assertTrue(fresh['battle_current'])

if __name__=='__main__':unittest.main()
