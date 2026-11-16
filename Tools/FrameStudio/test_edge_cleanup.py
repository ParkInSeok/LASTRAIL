import unittest
from PIL import Image
from edge_cleanup import clean_red_fringe,smooth_alpha_edge

class EdgeCleanupTests(unittest.TestCase):
 def test_alpha_cleanup_preserves_interior_and_ignores_invisible_rgb(self):
  im=Image.new('RGBA',(32,32),(0,255,0,0));im.paste((120,80,40,255),(8,8,24,24));mask=Image.new('L',im.size,255)
  result,report=smooth_alpha_edge(im,mask)
  self.assertEqual(result.getpixel((16,16)),im.getpixel((16,16)))
  edge=result.getpixel((7,16));self.assertTrue(0<edge[3]<255);self.assertEqual(edge[:3],(120,80,40))
  self.assertGreater(report['changed_pixels'],0)
 def test_alpha_cleanup_outside_mask_exact(self):
  im=Image.new('RGBA',(32,32));im.paste((120,80,40,255),(8,8,24,24));mask=Image.new('L',im.size);mask.paste(255,(0,0,16,32))
  result,_=smooth_alpha_edge(im,mask)
  self.assertEqual(result.crop((16,0,32,32)).tobytes(),im.crop((16,0,32,32)).tobytes())
 def test_only_red_fringe_in_mask_changes_and_alpha_stays(self):
  im=Image.new('RGBA',(32,32),(0,0,0,255));im.putpixel((8,8),(180,25,20,120));im.putpixel((24,8),(180,25,20,255));im.putpixel((8,20),(120,85,55,255))
  mask=Image.new('L',im.size);mask.paste(255,(0,0,16,32))
  result,report=clean_red_fringe(im,mask)
  self.assertLess(result.getpixel((8,8))[0],180)
  self.assertEqual(result.getpixel((24,8)),im.getpixel((24,8)))
  self.assertEqual(result.getpixel((8,20)),im.getpixel((8,20)))
  self.assertEqual(result.getchannel('A').tobytes(),im.getchannel('A').tobytes())
  self.assertEqual(report['changed_pixels'],1)
 def test_red_interior_away_from_black_is_preserved(self):
  im=Image.new('RGB',(32,32),(180,25,20));mask=Image.new('L',im.size,255)
  result,report=clean_red_fringe(im,mask);self.assertEqual(result.convert('RGB').tobytes(),im.tobytes());self.assertEqual(report['changed_pixels'],0)
 def test_service_keeps_blockers_and_original_version(self):
  import io,tempfile
  from pathlib import Path
  from repository import Repository
  from service import Studio
  from domain import new_project
  with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as d:
   repo=Repository(Path(d));studio=Studio(repo,None);p=new_project('edge');p['frames']=p['frames'][:1]
   def asset(im):
    b=io.BytesIO();im.save(b,format='PNG');return repo.put_asset(b.getvalue())
   im=Image.new('RGBA',(32,32),(0,0,0,255));im.putpixel((8,8),(180,25,20,255));a=asset(im)
   p['frames'][0]['versions']=[{'asset':a,'checks':{'blocking':['draft restriction']},'method':'local_draft'}];p['refinement_mask']=asset(Image.new('L',(32,32),255));repo.save(p)
   result=studio.action(p['id'],{'action':'clean_edges','index':0});versions=result['frames'][0]['versions']
   self.assertEqual(len(versions),2);self.assertEqual(versions[0]['asset'],a);self.assertEqual(versions[-1]['checks']['blocking'],['draft restriction']);self.assertFalse(result['frames'][0]['approved'])

if __name__=='__main__':unittest.main()
