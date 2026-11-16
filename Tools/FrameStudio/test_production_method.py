import copy,tempfile,unittest
from pathlib import Path
from repository import Repository
from service import Studio
from domain import new_project
from cutscene_service import CutsceneService
from video_service import VideoService
from local_motion_service import LocalMotionService

class ProductionMethodTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.repo=Repository(Path(self.tmp.name));self.studio=Studio(self.repo,None)
  self.p=new_project('method');self.repo.save(self.p)
 def tearDown(self):self.tmp.cleanup()
 def select(self,value):return self.studio.action(self.p['id'],dict(action='production_method',method=value))
 def test_persist_reopen_preserves_all_existing_state(self):
  before=copy.deepcopy(self.p)
  for method in ('ltx','frame_authoring'):
   self.assertEqual(self.select(method)['production_method'],method)
   reopened=Studio(Repository(self.repo.root),None).view(self.repo.get(self.p['id']))
   self.assertEqual(reopened['production_method'],method)
   stored=self.repo.get(self.p['id']);stored.pop('production_method');self.assertEqual(stored,before)
 def test_invalid_selection_does_not_write(self):
  for value in ('local','codex','',None,[],True):
   with self.assertRaises(ValueError):self.select(value)
   self.assertEqual(self.repo.get(self.p['id']),self.p)
 def test_legacy_view_defaults_without_rewriting(self):
  self.assertEqual(self.studio.view(self.p)['production_method'],'frame_authoring')
  self.assertEqual(self.repo.get(self.p['id']),self.p)
 def test_ltx_selection_blocks_frame_generation_before_provider(self):
  self.select('ltx')
  for action in ('generate','draft','batch','batch_draft','refine','refine_all'):
   with self.assertRaisesRegex(ValueError,'프레임 작화'):self.studio.action(self.p['id'],dict(action=action,index=0))
 def test_frame_selection_blocks_both_ltx_paths_before_side_effect(self):
  self.select('frame_authoring');cut=CutsceneService(self.repo);video=VideoService(cut,Path(__file__).parent)
  motion=LocalMotionService(self.studio,cut,video)
  with self.assertRaisesRegex(ValueError,'로컬 AI 모션'):video.start(self.p['id'],{})
  with self.assertRaisesRegex(ValueError,'로컬 AI 모션'):
   motion.start(self.p['id'],dict(reference='first',approved_reference=True,prompt='raise arm',seed=42))
  self.assertEqual(len(self.repo.list()),1);self.assertFalse(video.jobs);self.assertFalse(motion.jobs)

if __name__=='__main__':unittest.main()
