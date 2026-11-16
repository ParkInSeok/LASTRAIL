import base64, copy, io, tempfile, unittest
from pathlib import Path
from PIL import Image
from domain import new_project, approve_frame
from repository import Repository
from service import Studio
from delivery_review import approve_clip, signature


class GuideValidationTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup)
  self.repo=Repository(Path(self.tmp.name));self.studio=Studio(self.repo,object())
  im=Image.new('RGBA',(64,64));im.paste((200,190,180,255),(8,40,24,56));im.paste((200,190,180,255),(40,44,56,60))
  self.raw=self.png(im);self.ref=self.repo.put_asset(self.raw)
  p=new_project('gate test');p['frames']=p['frames'][:2];p['character_approved']=True;p['refs']['first']=self.ref
  for f in p['frames']:f['versions']=[{'asset':self.ref,'checks':{'blocking':[]},'method':'test'}]
  self.repo.save(p);self.pid=p['id'];self.criteria={'version':'test-v1','canvas':[64,64],'feet':[{'name':'front','region':[0,32,32,64]},{'name':'rear','region':[32,32,64,64]}],'garment_source':'unapproved review outline; manual visual review required'}
 def png(self,im):
  b=io.BytesIO();im.save(b,format='PNG');return b.getvalue()
 def act(self,action,**kw):return self.studio.action(self.pid,dict(action=action,**kw))
 def configure(self):return self.act('result_guide_config',index=0,criteria=self.criteria)
 def inspect(self):return self.act('result_guide_inspect',index=0)['frames'][0]['result_guide_report']
 def manual(self,r):return self.act('result_guide_review',index=0,token=r['token'],confirmed=True,note='Test fixture visual confirmation; not production approval')
 def test_missing_and_unverified_block_direct_commands(self):
  self.configure()
  with self.assertRaisesRegex(ValueError,'가이드'):self.act('approve_frame',index=0)
  r=self.inspect();self.assertEqual(r['feet_status'],'passed');self.assertEqual(r['status'],'unverified')
  with self.assertRaises(ValueError):self.act('approve_frame',index=0)
  p=self.repo.get(self.pid)
  with self.assertRaises(ValueError):approve_frame(p,0)
  self.manual(r);self.act('approve_frame',index=0)
  self.assertTrue(self.repo.get(self.pid)['frames'][0]['approved'])
 def test_shifted_feet_and_wrong_size_block(self):
  self.configure()
  for size,delta in [((64,64),1),((65,64),0)]:
   im=Image.new('RGBA',size);im.paste((200,190,180,255),(8,40+delta,24,56+delta));im.paste((200,190,180,255),(40,44,56,60))
   self.act('upload',target='frame',index=0,png=base64.b64encode(self.png(im)).decode())
   r=self.inspect();self.assertEqual(r['status'],'failed')
   with self.assertRaises(ValueError):self.manual(r)
   with self.assertRaises(ValueError):self.act('approve_frame',index=0)
 def test_stale_candidate_guide_and_asset_bytes(self):
  self.configure();r=self.inspect();self.manual(r)
  self.act('upload',target='frame',index=0,png=base64.b64encode(self.raw).decode())
  with self.assertRaises(ValueError):self.act('approve_frame',index=0)
  with self.assertRaises(ValueError):self.manual(r)
  r=self.inspect();self.manual(r);self.criteria['version']='test-v2';self.configure()
  with self.assertRaises(ValueError):self.act('approve_frame',index=0)
  r=self.inspect();self.manual(r)
  p=self.repo.get(self.pid);asset=p['frames'][0]['versions'][-1]['asset'];im=Image.new('RGBA',(64,64));self.repo.asset_path(asset).write_bytes(self.png(im))
  with self.assertRaises(ValueError):self.act('approve_frame',index=0)
 def test_clip_and_approved_export_block_but_save_allowed(self):
  self.configure();p=self.repo.get(self.pid);p['battle_export']={'signature':signature(p)};self.repo.save(p)
  with self.assertRaises(ValueError):self.act('approve_clip')
  with self.assertRaises(ValueError):approve_clip(p)
  with self.assertRaises(ValueError):self.act('export',approved_only=True)
  self.assertEqual(len(self.repo.get(self.pid)['frames'][0]['versions']),1)
 def test_legacy_without_guide_and_non_target_compatible(self):
  self.act('approve_frame',index=0);self.assertTrue(self.repo.get(self.pid)['frames'][0]['approved'])
  self.configure();self.act('approve_frame',index=1);self.assertTrue(self.repo.get(self.pid)['frames'][1]['approved'])
 def test_bad_criteria_and_automatic_tolerance_rejected(self):
  for key,value in [('canvas',[0,64]),('feet',[{'name':'front','region':[-1,0,30,64]}]),('tolerance',1)]:
   c=copy.deepcopy(self.criteria);c[key]=value
   with self.assertRaises(ValueError):self.act('result_guide_config',index=0,criteria=c)
 def test_reference_change_and_reinspection_revoke_confirmation(self):
  self.configure();r=self.inspect();self.manual(r);self.act('approve_frame',index=0)
  self.inspect()
  with self.assertRaises(ValueError):self.act('approve_frame',index=0)
  r=self.inspect();self.manual(r)
  self.act('upload',target='reference',role='first',png=base64.b64encode(self.raw).decode())
  with self.assertRaises(ValueError):self.act('result_guide_inspect',index=0)
  with self.assertRaises(ValueError):self.act('approve_frame',index=0)
 def test_invalid_indices_and_no_manual_confirmation_rejected(self):
  for i in [-1,100,True,'0',None]:
   with self.assertRaises(ValueError):self.act('result_guide_config',index=i,criteria=self.criteria)
   with self.assertRaises(ValueError):self.act('result_guide_inspect',index=i)
  self.configure();r=self.inspect()
  with self.assertRaises(ValueError):self.act('result_guide_review',index=0,token=r['token'],note='Fixture only, no user confirmation',confirmed=False)
 def test_schedule_cannot_drop_gate_silently(self):
  self.configure();self.act('schedule',rows=[dict(phase='test',duration_ms=120,kind='key')]*2)
  self.assertIn('0',self.repo.get(self.pid)['result_guides'])
  with self.assertRaises(ValueError):self.act('approve_clip')
 def test_same_extrema_with_changed_foot_silhouette_blocked(self):
  self.configure()
  with Image.open(io.BytesIO(self.raw)) as src:im=src.convert('RGBA')
  im.putpixel((9,41),(0,0,0,0))
  self.act('upload',target='frame',index=0,png=base64.b64encode(self.png(im)).decode())
  r=self.inspect();front=r['feet'][0]
  self.assertEqual(front['expected'],front['measured']);self.assertEqual(front['silhouette_difference_pixels'],1)
  self.assertEqual(r['status'],'failed')
  with self.assertRaises(ValueError):self.act('approve_frame',index=0)

if __name__=='__main__':unittest.main()
