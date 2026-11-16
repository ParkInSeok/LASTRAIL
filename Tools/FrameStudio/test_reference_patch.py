import base64, io, tempfile, unittest, copy
from pathlib import Path
from unittest.mock import patch
from PIL import Image
from repository import Repository
from service import Studio
from domain import new_project

def png(im):
 b=io.BytesIO();im.save(b,format='PNG');return b.getvalue()

class ReferencePatchTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory(dir=Path(__file__).parent)
  self.addCleanup(self.tmp.cleanup);self.repo=Repository(Path(self.tmp.name));self.studio=Studio(self.repo,None)
  self.p=new_project('patch');self.original=Image.new('RGBA',(32,32),(80,90,100,137))
  self.asset=self.repo.put_asset(png(self.original));self.p['refs']['first']=self.asset
  for i in (0,1):self.studio.add_frame(self.p,i,self.asset,{'blocking':['original review blocker']},'test')
  self.repo.save(self.p)
  mask=Image.new('L',(32,32));mask.paste(255,(10,10,20,20))
  self.data=dict(action='apply_reference_patch',index=0,source_index=0,source_asset=self.asset,source_version=0,source_revision=self.p['revision'],reference_asset=self.asset,generation_note='Test donor generated with the declared reference.',donor_png=base64.b64encode(png(Image.new('RGBA',(32,32),(200,150,40,0)))).decode(),mask_png=base64.b64encode(png(mask)).decode())
 def test_preserves_pixels_alpha_timing_blockers_and_restores(self):
  result=self.studio.action(self.p['id'],self.data);f=result['frames'][0];v=f['versions'][-1]
  with Image.open(self.repo.asset_path(v['asset'])) as im:
   self.assertEqual(im.getpixel((12,12)),(200,150,40,137));self.assertEqual(im.getpixel((0,0)),(80,90,100,137));self.assertEqual(im.getchannel('A').tobytes(),self.original.getchannel('A').tobytes())
  self.assertEqual(v['checks']['blocking'],['original review blocker']);self.assertFalse(f['approved'])
  self.assertEqual([x['duration_ms'] for x in result['frames']],[x['duration_ms'] for x in self.p['frames']])
  self.assertEqual(result['frames'][1],self.p['frames'][1]);self.assertIn('reference_patch',v['checks'])
  restored=self.studio.action(self.p['id'],dict(action='restore',index=0,version=0));self.assertEqual(restored['frames'][0]['versions'][-1]['asset'],self.asset)
 def test_rejects_bad_contract_without_state_change(self):
  bad=[dict(source_revision=-1),dict(source_version=3),dict(source_asset='wrong.png'),dict(index=1),dict(index=True),dict(reference_asset='unknown.png'),dict(generation_note=''),dict(donor_png='bad'),dict(mask_png='bad')]
  bad += [dict(donor_png=base64.b64encode(png(Image.new('RGB',(31,32)))).decode()),dict(mask_png=base64.b64encode(png(Image.new('L',(32,32)))).decode()),dict(mask_png=base64.b64encode(png(Image.new('RGB',(32,32),'red'))).decode())]
  for changes in bad:
   with self.subTest(changes=list(changes)):
    with self.assertRaises(ValueError):self.studio.action(self.p['id'],dict(self.data,**changes))
    self.assertEqual(self.repo.get(self.p['id']),self.p)
 def test_duplicate_request_rejected(self):
  self.studio.action(self.p['id'],self.data)
  with self.assertRaises(ValueError):self.studio.action(self.p['id'],self.data)
 def test_rgba_patch_preserves_outside_and_protected_foot_with_alpha_changes(self):
  donor=Image.new('RGBA',(32,32),(200,150,40,0));donor.putpixel((13,13),(210,160,50,255))
  data=dict(self.data,alpha_mode='masked_rgba',protected_regions=[[0,24,32,32]],donor_png=base64.b64encode(png(donor)).decode())
  result=self.studio.action(self.p['id'],data);v=result['frames'][0]['versions'][-1]
  with Image.open(self.repo.asset_path(v['asset'])) as im:
   self.assertEqual(im.getpixel((12,12)),(200,150,40,0));self.assertEqual(im.getpixel((13,13)),(210,160,50,255))
   for y in range(32):
    for x in range(32):
     if not (10<=x<20 and 10<=y<20):self.assertEqual(im.getpixel((x,y)),self.original.getpixel((x,y)))
  self.assertEqual(v['checks']['reference_patch']['alpha_mode'],'masked_rgba')
  self.assertTrue(v['checks']['reference_patch']['protected_regions_exact'])
  self.assertEqual(v['checks']['blocking'],['original review blocker']);self.assertFalse(result['frames'][0]['approved'])
  self.assertEqual(result['frames'][1],self.p['frames'][1])
 def test_rgba_patch_rejects_missing_alpha_or_unprotected_or_overlapping_mask(self):
  base=dict(self.data,alpha_mode='masked_rgba',protected_regions=[[0,24,32,32]])
  bad=[dict(alpha_mode='unknown'),dict(protected_regions=[]),dict(protected_regions=[[9,9,21,21]]),dict(protected_regions=[[0,24,33,32]]),dict(protected_regions=[[False,24,32,32]]),dict(donor_png=base64.b64encode(png(Image.new('RGB',(32,32)))).decode())]
  for change in bad:
   with self.subTest(change=list(change)):
    with self.assertRaises(ValueError):self.studio.action(self.p['id'],dict(base,**change))
    self.assertEqual(self.repo.get(self.p['id']),self.p)
 def test_rgba_patch_rejects_soft_mask_and_save_failure_is_retryable(self):
  base=dict(self.data,alpha_mode='masked_rgba',protected_regions=[[0,24,32,32]])
  mask=Image.new('L',(32,32));mask.paste(128,(10,10,20,20))
  with self.assertRaises(ValueError):self.studio.action(self.p['id'],dict(base,mask_png=base64.b64encode(png(mask)).decode()))
  self.assertEqual(self.repo.get(self.p['id']),self.p)
  with patch.object(self.repo,'save',side_effect=OSError('disk failure')):
   with self.assertRaises(OSError):self.studio.action(self.p['id'],base)
  self.assertEqual(self.repo.get(self.p['id']),self.p)
  self.assertEqual(len(self.studio.action(self.p['id'],base)['frames'][0]['versions']),2)
  with self.assertRaises(ValueError):self.studio.action(self.p['id'],base)
 def test_save_failure_leaves_original_and_can_retry(self):
  with patch.object(self.repo,'save',side_effect=OSError('disk failure')):
   with self.assertRaises(OSError):self.studio.action(self.p['id'],self.data)
  self.assertEqual(self.repo.get(self.p['id']),self.p)
  self.assertEqual(len(self.studio.action(self.p['id'],self.data)['frames'][0]['versions']),2)

if __name__=='__main__':unittest.main()
