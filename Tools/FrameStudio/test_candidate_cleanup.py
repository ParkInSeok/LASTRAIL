import copy,json,tempfile,unittest
from pathlib import Path
from repository import Repository
import test_shading_review as fixtures
import shading_review
from candidate_cleanup import discard_failed_versions

class CleanupTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.repo=Repository(Path(self.tmp.name))
  fixture=fixtures.ShadingReviewTests();fixture.setUp();self.p=fixture.p;self.p['id']='1'*32;self.current=fixture.current
  self.p['production_flow']['frames']['0']['shadow'].insert(0,{'asset':'failed.png'})
  self.p['production_flow']['requests']=[{'id':'old-request','result_asset':'failed.png','status':'complete'}]
  self.repo.asset_path('failed.png').write_bytes(b'failed candidate')
  shading_review.record(self.p,fixture.payload(),self.current)
  self.data={'index':0,'assets':['failed.png'],'confirmed':True,'note':'user requested failed candidate removal after verified replacement'}
 def test_detaches_only_old_candidates_and_leaves_physical_delete_to_verified_caller(self):
  before=copy.deepcopy(self.p['production_flow']['frames']['0'])
  discard_failed_versions(self.repo,self.p,self.data,self.current)
  frame=self.p['production_flow']['frames']['0']
  self.assertEqual(frame['shadow'],before['shadow'][-1:]);self.assertEqual(frame['flat'],before['flat'])
  self.assertTrue(self.repo.asset_path('failed.png').exists())
  self.assertNotIn('result_asset',self.p['production_flow']['requests'][0])
  self.assertEqual(self.p['production_flow']['requests'][0]['status'],'result_discarded')
 def test_unreviewed_shared_current_and_path_escape_rejected_without_mutation(self):
  for case in ('failed-review','shared','current','path','unconfirmed'):
   with self.subTest(case=case):
    p=copy.deepcopy(self.p);data=copy.deepcopy(self.data)
    if case=='failed-review':p['production_flow']['surface_reviews']['0']['hair']['passed']=False
    if case=='shared':self.repo.save({'id':'2'*32,'reference':'failed.png'})
    if case=='current':data['assets']=['shadow']
    if case=='path':data['assets']=['../failed.png']
    if case=='unconfirmed':data['confirmed']=False
    before=copy.deepcopy(p)
    with self.assertRaises(ValueError):discard_failed_versions(self.repo,p,data,self.current)
    self.assertEqual(p,before);self.assertTrue(self.repo.asset_path('failed.png').exists())
    shared=self.repo.project_path('2'*32)
    if shared.exists():shared.unlink()

if __name__=='__main__':unittest.main()
