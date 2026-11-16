import unittest,tempfile,io,time,json,threading
from pathlib import Path
from PIL import Image
from repository import Repository
from domain import new_project
from cutscene_service import CutsceneService
from video_service import VideoService
from video_generation import MODEL,ENCODER

class FakeProvider:
 def __init__(self):self.sent=threading.Event();self.calls=[];self.fail=False;self.busy=False
 def upload(self,path):return 'reference.png'
 def json(self,path,data=None):
  self.calls.append((path,data))
  if path=='/queue':return {'queue_running':[1] if self.busy else [],'queue_pending':[]}
  if path=='/prompt':self.sent.set();return {'node_errors':{'bad':{}}} if self.fail else {'prompt_id':'owned-job'}
  return {}

class JobTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name);repo=Repository(self.root/'data');project=new_project('video');repo.save(project);self.pid=project['id'];self.cut=CutsceneService(repo)
  b=io.BytesIO();Image.new('RGB',(32,64)).save(b,format='PNG');m=self.cut.import_media(self.pid,io.BytesIO(b.getvalue()),len(b.getvalue()),'ref.png');self.req=dict(asset=m['id'],prompt='Reach forward',seed=42)
  for kind,name in [('checkpoints',MODEL),('text_encoders',ENCODER)]:
   p=self.root/'runtime/ComfyUI/models'/kind/name;p.parent.mkdir(parents=True,exist_ok=True);p.touch()
  self.provider=FakeProvider();self.service=VideoService(self.cut,self.root,self.provider)
 def tearDown(self):self.service.close();self.tmp.cleanup()
 def wait(self,state):
  deadline=time.monotonic()+4
  while time.monotonic()<deadline:
   j=self.service.view(self.pid)
   if j and j['status']==state and self.service.active is None:return j
   time.sleep(.01)
  self.fail('job did not reach '+state)
 def test_duplicate_cancel_only_owned_queue_and_preserve_edit(self):
  before=self.cut.view(self.pid);self.service.start(self.pid,self.req);self.assertTrue(self.provider.sent.wait(2))
  with self.assertRaises(ValueError):self.service.start(self.pid,self.req)
  self.service.cancel(self.pid);self.wait('cancelled')
  self.assertIn(('/queue',{'delete':['owned-job']}),self.provider.calls)
  self.assertFalse(any(p=='/interrupt' for p,d in self.provider.calls));self.assertEqual(before,self.cut.view(self.pid))
 def test_foreign_queue_blocks_submission(self):
  self.provider.busy=True
  with self.assertRaises(ValueError):self.service.start(self.pid,self.req)
  self.assertFalse(self.provider.sent.is_set())
 def test_failed_submission_not_complete(self):
  self.provider.fail=True;self.service.start(self.pid,self.req);job=self.wait('failed');self.assertIsNone(job['media'])
 def test_restart_keeps_interrupted_receipt(self):
  path=self.cut.folder(self.pid)/'latest-ai-video.json';path.write_text(json.dumps(dict(status='running',prompt_id='old')),encoding='utf8')
  self.assertEqual(self.service.view(self.pid)['status'],'interrupted');self.assertEqual(json.loads(path.read_text())['prompt_id'],'old')

if __name__=='__main__':unittest.main()
