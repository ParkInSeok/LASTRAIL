import copy,io,json,tempfile,time,unittest
from pathlib import Path
from PIL import Image
from repository import Repository
from domain import new_project
from service import Studio
from cutscene_service import CutsceneService
from local_motion_service import LocalMotionService

class FakeVideo:
    def __init__(self,cut):self.cut=cut;self.jobs={};self.fail=False;self.complete=False;self.postfail=False
    def start(self,pid,data):
        if self.fail:raise ValueError('GPU busy')
        job=dict(id='a'*32,status='running',request=dict(frames=49,fps=16,width=32,height=32),media=None)
        self.jobs[pid]=job
        if self.complete:
            path=self.cut.folder(pid)/'ai-video'/job['id'];path.mkdir(parents=True)
            frames=[Image.new('RGB',(32,32),(i*4,30,50)) for i in range(49)]
            frames[0].save(path/'generated.webp',save_all=True,append_images=frames[1:],duration=63,lossless=True)
            job.update(status='complete',media={'id':'video'})
            if self.postfail:job.update(status='failed',message='MP4 conversion denied')
        return copy.deepcopy(job)
    def view(self,pid):return copy.deepcopy(self.jobs.get(pid))
    def cancel(self,pid):self.jobs[pid]['status']='cancelled';return self.view(pid)

class ServiceTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.repo=Repository(Path(self.tmp.name)/'data')
        self.studio=Studio(self.repo,None);self.cut=CutsceneService(self.repo);self.video=FakeVideo(self.cut)
        self.s=LocalMotionService(self.studio,self.cut,self.video)
        p=new_project('approved source');b=io.BytesIO();Image.new('RGBA',(64,64),(1,2,3,200)).save(b,format='PNG')
        p['refs']['first']=self.repo.put_asset(b.getvalue());self.repo.save(p);self.p=p
        self.req=dict(prompt='gentle breathing',approved_reference=True,seed=42)
    def tearDown(self):self.s.close();self.tmp.cleanup()
    def wait(self,status):
        until=time.monotonic()+5
        while time.monotonic()<until:
            view=self.s.view(self.p['id'])
            if view['job'] and view['job']['status']==status:return view['job']
            time.sleep(.03)
        self.fail('missing state '+status)
    def test_generation_creates_candidate_never_changes_approved_source(self):
        self.video.complete=True
        self.s.start(self.p['id'],self.req);job=self.wait('complete')
        self.assertEqual(self.repo.get(self.p['id']),self.p)
        candidate=self.repo.get(job['candidate_id'])
        self.assertEqual(len(candidate['frames']),12)
        self.assertTrue(all(len(f['versions'])==1 and not f['approved'] for f in candidate['frames']))
        self.assertFalse(candidate['character_approved'])
        self.assertTrue(all('투명 배경 없음' in f['versions'][0]['checks']['blocking'] for f in candidate['frames']))
        self.assertTrue(self.repo.asset_path(job['preview']).exists())
    def test_duplicate_and_cancel_preserve_source(self):
        self.s.start(self.p['id'],self.req)
        with self.assertRaises(ValueError):self.s.start(self.p['id'],self.req)
        self.s.cancel(self.p['id']);self.wait('cancelled')
        self.assertEqual(self.repo.get(self.p['id']),self.p)
    def test_video_postprocess_failure_keeps_valid_frames_and_discloses_warning(self):
        self.video.complete=True;self.video.postfail=True
        self.s.start(self.p['id'],self.req);job=self.wait('complete')
        self.assertEqual(job['video_warning'],'MP4 conversion denied')
        self.assertEqual(len(self.repo.get(job['candidate_id'])['frames']),12)
        self.assertEqual(self.repo.get(self.p['id']),self.p)
    def test_failure_is_not_completion_and_stale_source_rejected(self):
        self.video.fail=True
        with self.assertRaises(ValueError):self.s.start(self.p['id'],{**self.req,'revision':-1})
        self.assertEqual(len(self.repo.list()),1)
        with self.assertRaises(ValueError):self.s.start(self.p['id'],self.req)
        self.assertEqual(self.s.view(self.p['id'])['job']['status'],'failed')
    def test_restart_marks_unfinished_as_interrupted_without_resubmission(self):
        self.s.start(self.p['id'],self.req)
        other=LocalMotionService(self.studio,self.cut,self.video)
        self.assertEqual(other.view(self.p['id'])['job']['status'],'interrupted')
        other.close()

if __name__=='__main__':unittest.main()
