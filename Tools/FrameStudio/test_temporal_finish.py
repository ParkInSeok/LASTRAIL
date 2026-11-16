import unittest
import numpy as np
from PIL import Image,ImageDraw
from temporal_finish import TemporalProcessor

class TemporalTests(unittest.TestCase):
 def pair(self):
  a=Image.new('RGBA',(96,96));ImageDraw.Draw(a).rectangle((20,35,40,55),fill=(220,120,40,255))
  b=Image.new('RGBA',(96,96));ImageDraw.Draw(b).rectangle((36,35,56,55),fill=(220,120,40,255))
  mask=Image.new('L',(96,96));ImageDraw.Draw(mask).rectangle((10,20,75,75),fill=255)
  return a,b,mask
 def test_endpoints_and_protection(self):
  a,b,m=self.pair();p=TemporalProcessor([a,b],m,0)
  self.assertEqual(p.frame(0,1,0).tobytes(),a.tobytes())
  result=np.array(p.frame(0,1,.5));outside=np.array(m)==0
  self.assertTrue(np.array_equal(result[outside],np.array(a)[outside]))
  x=np.where(result[:,:,3]>100)[1].mean()
  self.assertGreater(x,32);self.assertLess(x,44)
 def test_invalid_mask_and_reference(self):
  a,b,m=self.pair()
  with self.assertRaises(ValueError):TemporalProcessor([a,b],Image.new('L',(3,3)),0)
  with self.assertRaises(ValueError):TemporalProcessor([a,b],m,9)
 def test_guided_translation_and_protected_region(self):
  a,b,m=self.pair()
  pts=[[[20,35],[40,35],[40,55],[20,55]],[[36,35],[56,35],[56,55],[36,55]]]
  p=TemporalProcessor([a,b],m,0,pts);out=np.array(p.frame(0,1,.5))
  self.assertTrue(np.array_equal(out[np.array(m)==0],np.array(a)[np.array(m)==0]))
  x=np.where(out[:,:,3]>128)[1].mean();self.assertAlmostEqual(x,38,delta=2)
  with self.assertRaises(ValueError):TemporalProcessor([a,b],m,0,[[[1,2]]])
 def test_workflow_preserves_source_and_invalidates_preview(self):
  import tempfile,io,base64,copy
  from pathlib import Path
  from repository import Repository
  from service import Studio
  from domain import new_project,set_schedule
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   repo=Repository(Path(d));studio=Studio(repo,None);p=new_project('review')
   set_schedule(p,[{'phase':'a','duration_ms':80,'kind':'key'},{'phase':'b','duration_ms':80,'kind':'key'}])
   a,b,m=self.pair()
   for i,im in enumerate([a,b]):
    buf=io.BytesIO();im.save(buf,format='PNG');studio.add_frame(p,i,repo.put_asset(buf.getvalue()),{'blocking':['draft']},'source')
   repo.save(p);original=copy.deepcopy(p['frames']);buf=io.BytesIO();m.save(buf,format='PNG')
   result=studio.action(p['id'],{'action':'temporal_review','reference':0,'step_ms':20,'transition_ms':40,'mask_png':base64.b64encode(buf.getvalue()).decode()})
   self.assertEqual(result['frames'],original);self.assertEqual(result['temporal_output']['blocking'],['draft'])
   self.assertEqual(len(result['temporal_output']['frames']),8)
   self.assertFalse(result['temporal_output']['stale'])
   result=studio.action(p['id'],{'action':'notes','index':0,'notes':'changed'})
   self.assertTrue(result['temporal_output']['stale'])
