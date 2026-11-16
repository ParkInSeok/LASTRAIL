import unittest
from domain import new_project,approve_character,ROLES
from batch import plan_frames,draft_request

class BatchTests(unittest.TestCase):
 def test_attack_uses_timeline_phase_without_invented_breathing(self):
  p=new_project('Eve');p['refs']['first']='first.png';p['motion']='one charged shot'
  p['frames'][5].update(phase='single discharge',notes='arms braced, tool aimed forward')
  prompt=draft_request(p,5,42)['prompt']
  self.assertIn('single discharge',prompt)
  self.assertIn('arms braced, tool aimed forward',prompt)
  self.assertNotIn('breathing',prompt)
  self.assertNotIn('chest breath intensity',prompt)
 def test_pose_guide_is_distinct_from_identity_reference(self):
  p=new_project('Eve');p['refs']['first']='first.png';p['frames'][3]['guide']='pose3.png'
  r=draft_request(p,3,42)
  self.assertEqual(r['reference_roles'],{'first':'first.png','pose':'pose3.png'})
  self.assertEqual(r['reference_assets'],['first.png','pose3.png'])
  p['frames'][3]['versions']=[{}];p['frames'][3]['needs_regeneration']=True
  self.assertIn(3,plan_frames(p,True))
 def test_keys_first_and_approval_gate(self):
  p=new_project('test')
  with self.assertRaises(ValueError):plan_frames(p,False)
  p['refs']={r:r+'.png' for r in ROLES};p.update(identity='x',motion='x',hidden='x');approve_character(p)
  self.assertEqual(plan_frames(p,False),[0,5,11])
  for i in (0,5,11):p['frames'][i]['versions']=[{'checks':{'blocking':[]}}]
  self.assertEqual(plan_frames(p,False),[])
  for i in (0,5,11):p['frames'][i]['approved']=True
  self.assertEqual(plan_frames(p,False),[1,2,3,4,6,7,8,9,10])
 def test_draft_skips_completed_and_changes_phase_not_source(self):
  p=new_project('Eve');p['refs']['first']='source.png';p['identity']='goggles';p['motion']='idle'
  p['frames'][0]['versions']=[{}]
  self.assertEqual(len(plan_frames(p,True)),11)
  a=draft_request(p,0,42);b=draft_request(p,5,42)
  self.assertEqual(a['seed'],b['seed']);self.assertEqual(a['reference_assets'],b['reference_assets'])
  self.assertNotEqual(a['prompt'],b['prompt']);self.assertTrue(a['draft'])

 def test_failure_resume_exports_and_restart_recovery(self):
  import tempfile,io,time
  from pathlib import Path
  from PIL import Image
  from repository import Repository
  from providers import ProviderRouter
  from service import Studio
  class Generator:
   def __init__(self):self.calls=[];self.fail=True
   def prepare(self):pass
   def cancel(self):pass
   def generate(self,req,refs,work):
    i=req['frame_index'];self.calls.append(i)
    if i==1 and self.fail:self.fail=False;raise ValueError('test failure')
    work.mkdir(parents=True,exist_ok=True);im=Image.new('RGBA',(64,64));im.paste((20+i*20,80,120,255),(20,20,40,40));im.save(work/'result.png');return work/'result.png'
  def wait(s):
   end=time.monotonic()+5
   while s.active and time.monotonic()<end:time.sleep(.01)
   self.assertIsNone(s.active)
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   repo=Repository(Path(d));g=Generator();router=ProviderRouter(Path(d)/'engine.json',{'local':g,'codex':g});studio=Studio(repo,router)
   p=new_project('test');p['frames']=p['frames'][:3];im=Image.new('RGBA',(64,64));b=io.BytesIO();im.save(b,format='PNG');p['refs']['first']=repo.put_asset(b.getvalue());repo.save(p)
   studio.action(p['id'],{'action':'batch_draft'});wait(studio)
   self.assertEqual(repo.get(p['id'])['batch']['state'],'failed');self.assertEqual(g.calls,[0,1])
   studio.action(p['id'],{'action':'batch_draft'});wait(studio);result=repo.get(p['id'])
   self.assertEqual(g.calls,[0,1,1,2]);self.assertEqual(result['batch']['state'],'needs_review')
   with Image.open(repo.asset_path(result['review_export']['gif'])) as gif:
    self.assertEqual(gif.info['loop'],0);self.assertEqual(gif.n_frames,3)
   self.assertTrue(all(not f['approved'] for f in result['frames']))
   result['batch']['state']='running';repo.save(result);Studio(repo,router)
   self.assertEqual(repo.get(p['id'])['batch']['state'],'interrupted')

if __name__=='__main__':unittest.main()
