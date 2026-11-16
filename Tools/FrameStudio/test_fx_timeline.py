import copy, json, tempfile, unittest, zipfile
from pathlib import Path
from PIL import Image
from repository import Repository
from service import Studio
from domain import new_project
from battle_scene import BattleScene
from delivery_review import signature
from test_clip_workflow import png

class FxTimelineTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup)
  self.repo=Repository(Path(self.tmp.name));self.studio=Studio(self.repo,object())
  p=new_project('timing');self.repo.save(p);self.id=p['id']
  self.action('import_clip',frames=[dict(png=png(),fx_png=png(box=(1,1,4,4)),duration_ms=40,phase='hold',kind='key') for _ in range(24)])
 def action(self,name,**data):return self.studio.action(self.id,dict(action=name,**data))
 def rows(self):return [dict(start_ms=0,png=png(box=None),label='prepare'),dict(start_ms=450,png=png((255,230,180,255),box=(1,1,4,4)),label='release'),dict(start_ms=600,png=png(box=None),label='off')]
 def test_independent_boundary_survives_storage_without_actor_retime(self):
  before=copy.deepcopy(self.repo.get(self.id)['frames'])
  self.action('fx_timeline',stages=self.rows())
  reopened=Repository(self.repo.root).get(self.id)
  self.assertEqual(reopened['frames'],before)
  from fx_timeline import stage_at
  self.assertEqual([stage_at(reopened,t)['label'] for t in [449,450,451,599,600]],['prepare','release','release','release','off'])
  renderer=BattleScene(self.repo,reopened,Image.new('RGBA',(160,180)),dict(height=44,anchor=[.5,.8]))
  self.assertIn(450,renderer.times());self.assertIn(600,renderer.times())
  self.assertIsNone(renderer.render(449)[1].getbbox())
  self.assertIsNotNone(renderer.render(450)[1].getbbox())
  self.assertEqual(renderer.render(450)[2]['actor_frame'],12)
 def test_bad_late_stage_is_atomic_and_invalidates_signature(self):
  before=self.repo.get(self.id);files=list(self.repo.root.joinpath('assets').iterdir())
  for bad in [450,True,960,-10,451]:
   rows=self.rows();rows[-1]['start_ms']=bad
   with self.assertRaises(ValueError):self.action('fx_timeline',stages=rows)
   self.assertEqual(self.repo.get(self.id),before)
   self.assertEqual(list(self.repo.root.joinpath('assets').iterdir()),files)
  rows=self.rows();rows[-1]['png']=png(size=(32,32))
  with self.assertRaises(ValueError):self.action('fx_timeline',stages=rows)
  self.assertEqual(list(self.repo.root.joinpath('assets').iterdir()),files)
  after=self.action('fx_timeline',stages=self.rows())
  self.assertNotEqual(signature(before),signature(after))
 def test_export_contains_exact_boundary_and_legacy_fallback(self):
  p=self.repo.get(self.id);bg=Image.new('RGBA',(160,180));settings=dict(height=44,anchor=[.5,.8])
  self.assertIsNotNone(BattleScene(self.repo,p,bg,settings).render(449)[1].getbbox())
  self.action('fx_timeline',stages=self.rows())
  self.action('battle_settings',png=png(size=(160,180),box=(0,0,160,180)),**settings)
  p=self.action('battle_export');report=json.loads(self.repo.asset_path(p['battle_export']['fx_manifest']).read_text())
  self.assertIn(450,[x['time_ms'] for x in report['timeline']])
  self.assertEqual(sum(x['duration_ms'] for x in report['timeline']),960)
  with zipfile.ZipFile(self.repo.asset_path(p['battle_export']['zip'])) as archive:
   for i,row in enumerate(p['fx_timeline']['stages']):self.assertEqual(archive.read(f'source-fx/{i+1:03}.png'),self.repo.asset_path(row['asset']).read_bytes())
  self.assertEqual(report['fx_timeline'],p['fx_timeline'])
  p=self.action('fx_timeline',stages=None)
  self.assertNotIn('fx_timeline',p)
  self.assertIsNotNone(BattleScene(self.repo,p,bg,settings).render(449)[1].getbbox())
 def test_actor_retime_rejected_while_timeline_attached_and_reimport_archives(self):
  self.action('fx_timeline',stages=self.rows());before=self.repo.get(self.id)
  with self.assertRaises(ValueError):self.action('retime',durations=[50]*24)
  self.assertEqual(self.repo.get(self.id),before)
  self.action('import_clip',frames=[dict(png=png(),duration_ms=40,phase='new',kind='key') for _ in range(24)])
  self.assertNotIn('fx_timeline',self.repo.get(self.id))
  self.assertTrue(self.repo.get(self.id)['fx_timeline_history'])
 def test_missing_empty_or_foreign_asset_rejected_without_writes(self):
  before=self.repo.get(self.id)
  for data in [{},{'stages':[]},{'stages':[{'start_ms':0,'asset':'foreign.png'}]},{'stages':self.rows(),'occlude_actor_alpha':True}]:
   with self.assertRaises(ValueError):self.action('fx_timeline',**data)
   self.assertEqual(self.repo.get(self.id),before)
 def test_occlusion_and_edit_reuse_assets(self):
  p=self.action('fx_timeline',stages=self.rows(),occlude_actor_alpha=64)
  rows=copy.deepcopy(p['fx_timeline']['stages']);rows[1]['start_ms']=440
  assets=list(self.repo.root.joinpath('assets').iterdir())
  p=self.action('fx_timeline',stages=rows,occlude_actor_alpha=64)
  self.assertEqual(list(self.repo.root.joinpath('assets').iterdir()),assets)
  self.assertEqual(p['fx_timeline']['stages'][1]['start_ms'],440)

if __name__=='__main__':unittest.main()
