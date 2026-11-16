import copy
import unittest
from test_surface_shading import SurfaceShadingTests,raw
import hashlib
from PIL import Image


class StrandSurfaceTests(SurfaceShadingTests):
 def strand_data(self):
  d=copy.deepcopy(self.data)
  d['surface_map']={'strands':[{'id':'lock1','columns':['edge','shade_boundary','edge2'],
    'source_rows':[[[8,8],[16,8],[24,8]],[[8,24],[16,24],[24,24]]],
    'target_rows':[[[8,8],[18,8],[24,8]],[[8,24],[18,24],[24,24]]]}]}
  return d
 def test_boundary_and_brush_follow_grid_not_screen(self):
  p=self.studio.action(self.p['id'],self.strand_data())
  with Image.open(self.repo.asset_path(p['frames'][0]['versions'][-1]['asset'])) as im:
   self.assertEqual(im.getpixel((17,16)),(40,50,60,137))
   self.assertEqual(im.getpixel((18,16)),(160,150,140,137))
   self.assertEqual(im.getpixel((0,0)),self.original.getpixel((0,0)))
  report=p['frames'][0]['versions'][-1]['checks']['surface_shading']
  self.assertEqual(report['strand_tracks'][0]['columns'][1],'shade_boundary')
  self.assertEqual(report['strand_tracks'][0]['brush_direction'],'row-progression')
 def test_invalid_grid_is_atomic(self):
  for case in ('duplicate','rows','fold','mixed'):
   d=self.strand_data();s=d['surface_map']['strands'][0]
   if case=='duplicate':s['columns'][1]='edge'
   if case=='rows':s['target_rows'].pop()
   if case=='fold':s['target_rows'][1].reverse()
   if case=='mixed':d['surface_map']['surfaces']=self.data['surface_map']['surfaces']
   with self.subTest(case=case):
    with self.assertRaises(ValueError):self.studio.action(self.p['id'],d)
    self.assertEqual(self.repo.get(self.p['id']),self.p)
 def test_brush_mark_rotates_with_longitudinal_direction(self):
  ref=Image.new('RGBA',(32,32),(40,50,60,255))
  for y in range(8,25):ref.putpixel((16,y),(200,100,20,255))
  asset=self.repo.put_asset(raw(ref));self.p['refs']['first']=asset;self.repo.save(self.p)
  d=self.strand_data();d.update(reference_asset=asset,reference_sha256=hashlib.sha256(self.repo.asset_path(asset).read_bytes()).hexdigest())
  d['surface_map']['strands'][0]['target_rows']=[[[24,8],[24,16],[24,24]],[[8,8],[8,16],[8,24]]]
  p=self.studio.action(self.p['id'],d)
  with Image.open(self.repo.asset_path(p['frames'][0]['versions'][-1]['asset'])) as im:
   self.assertEqual(im.getpixel((12,16)),(200,100,20,137))
   self.assertEqual(im.getpixel((16,12)),(40,50,60,137))


if __name__=='__main__':unittest.main()
