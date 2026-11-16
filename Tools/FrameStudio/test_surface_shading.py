import base64,copy,hashlib,io,tempfile,unittest
from pathlib import Path
from PIL import Image
from repository import Repository
from service import Studio
from domain import new_project

def raw(im):
 b=io.BytesIO();im.save(b,format='PNG');return b.getvalue()

class SurfaceShadingTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup)
  self.repo=Repository(Path(self.tmp.name));self.studio=Studio(self.repo,None);self.p=new_project('surface')
  ref=Image.new('RGBA',(32,32),(40,50,60,255))
  for y in range(32):
   for x in range(16,32):ref.putpixel((x,y),(160,150,140,255))
  self.ref=self.repo.put_asset(raw(ref));self.p['refs']['first']=self.ref
  self.original=Image.new('RGBA',(32,32),(80,90,100,137));a=self.repo.put_asset(raw(self.original));self.studio.add_frame(self.p,0,a,{'blocking':[]},'test');self.repo.save(self.p)
  mask=Image.new('L',(32,32));mask.paste(255,(10,10,22,22))
  self.data=dict(action='apply_surface_shading',index=0,source_index=0,source_version=0,source_revision=self.p['revision'],source_asset=a,reference_asset=self.ref,reference_sha256=hashlib.sha256(self.repo.asset_path(self.ref).read_bytes()).hexdigest(),mask_png=base64.b64encode(raw(mask)).decode(),surface_map={'surfaces':[{'id':'cloth','source':[[8,8],[24,8],[24,24],[8,24]],'target':[[8,8],[24,8],[24,24],[8,24]],'triangles':[[0,1,2],[0,2,3]]}]})
 def test_fixed_boundary_exact_rgb_alpha_and_determinism(self):
  p=self.studio.action(self.p['id'],self.data);v=p['frames'][0]['versions'][-1]
  with Image.open(self.repo.asset_path(v['asset'])) as im:
   self.assertEqual(im.getpixel((15,16)),(40,50,60,137));self.assertEqual(im.getpixel((16,16)),(160,150,140,137));self.assertEqual(im.getpixel((0,0)),self.original.getpixel((0,0)))
   self.assertEqual(im.getchannel('A').tobytes(),self.original.getchannel('A').tobytes())
  self.assertIn('surface_shading',v['checks']);self.assertFalse(p['frames'][0]['approved']);self.assertEqual(p['frames'][0]['duration_ms'],120)
  self.repo.save(self.p);again=self.studio.action(self.p['id'],self.data);self.assertEqual(self.repo.asset_path(v['asset']).read_bytes(),self.repo.asset_path(again['frames'][0]['versions'][-1]['asset']).read_bytes())
 def test_correspondence_moves_shade_boundary_not_actor(self):
  d=copy.deepcopy(self.data);d['surface_map']['surfaces'][0]['target']=[[10,8],[26,8],[26,24],[10,24]]
  p=self.studio.action(self.p['id'],d)
  with Image.open(self.repo.asset_path(p['frames'][0]['versions'][-1]['asset'])) as im:
   self.assertEqual(im.getpixel((17,16)),(40,50,60,137));self.assertEqual(im.getpixel((18,16)),(160,150,140,137))
 def test_invalid_and_stale_contract_is_atomic(self):
  for change in ('stale','sha','nan','flipped','uncovered'):
   d=copy.deepcopy(self.data);s=d['surface_map']['surfaces'][0]
   if change=='stale':d['source_revision']=-1
   elif change=='sha':d['reference_sha256']='bad'
   elif change=='nan':s['target'][0][0]=float('nan')
   elif change=='flipped':s['target'][1],s['target'][3]=s['target'][3],s['target'][1]
   else:s['target']=[[0,0],[2,0],[2,2],[0,2]]
   with self.subTest(change=change):
    with self.assertRaises(ValueError):self.studio.action(self.p['id'],d)
    self.assertEqual(self.repo.get(self.p['id']),self.p)
 def test_reference_interior_alpha253_is_valid_but_transparent_sample_rejected(self):
  im=Image.new('RGBA',(32,32),(90,20,40,253));asset=self.repo.put_asset(raw(im));self.p['refs']['first']=asset;self.repo.save(self.p)
  d=copy.deepcopy(self.data);d.update(reference_asset=asset,reference_sha256=hashlib.sha256(self.repo.asset_path(asset).read_bytes()).hexdigest())
  p=self.studio.action(self.p['id'],d)
  with Image.open(self.repo.asset_path(p['frames'][0]['versions'][-1]['asset'])) as out:self.assertEqual(out.getpixel((15,15)),(90,20,40,137))
  im.putpixel((15,15),(90,20,40,0));asset=self.repo.put_asset(raw(im));self.p['refs']['first']=asset;self.repo.save(self.p);d.update(reference_asset=asset,reference_sha256=hashlib.sha256(self.repo.asset_path(asset).read_bytes()).hexdigest())
  with self.assertRaises(ValueError):self.studio.action(self.p['id'],d)
 def test_explicit_transition_mask_blends_rgb_only(self):
  mask=Image.new('L',(32,32));mask.putpixel((12,12),128);d=copy.deepcopy(self.data);d['mask_png']=base64.b64encode(raw(mask)).decode()
  p=self.studio.action(self.p['id'],d)
  with Image.open(self.repo.asset_path(p['frames'][0]['versions'][-1]['asset'])) as im:self.assertEqual(im.getpixel((12,12)),(60,70,80,137))

if __name__=='__main__':unittest.main()
