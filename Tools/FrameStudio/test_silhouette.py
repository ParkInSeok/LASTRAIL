import unittest
from PIL import Image
from silhouette import composite_silhouette, context_box

class SilhouetteTests(unittest.TestCase):
 def test_feather_does_not_mix_in_invisible_source_color(self):
  source=Image.new('RGBA',(4,4),(0,255,0,0));layer=Image.new('RGB',(4,4),(80,60,40))
  out=composite_silhouette(source,layer,Image.new('L',(4,4),128))
  self.assertEqual(out.getpixel((1,1)),(80,60,40,128))
 def test_unchanged_black_composite_keeps_soft_source_edges(self):
  source=Image.new('RGBA',(4,4),(100,60,30,80));bg=Image.new('RGBA',source.size,'black');bg.alpha_composite(source)
  out=composite_silhouette(source,bg.convert('RGB'),Image.new('L',source.size,255))
  self.assertEqual(out.tobytes(),source.tobytes())
 def test_context_keeps_mask_inside_near_canvas_edge(self):
  box=context_box((1254,1254),(1170,1100,1254,1254))
  self.assertEqual(box,(998,998,1254,1254))
  self.assertEqual(context_box((1254,1254),(708,150,795,316)),(623,105,879,361))
 def test_new_outline_and_erasure_are_bounded(self):
  source=Image.new('RGBA',(4,1),(80,60,40,255));source.putpixel((1,0),(0,0,0,0))
  patch=Image.new('RGB',(4,1),'black');patch.putpixel((1,0),(80,60,40))
  mask=Image.new('L',(4,1));mask.putpixel((1,0),255);mask.putpixel((2,0),255)
  out=composite_silhouette(source,patch,mask)
  self.assertEqual(out.getpixel((0,0)),source.getpixel((0,0)))
  self.assertEqual(out.getpixel((3,0)),source.getpixel((3,0)))
  self.assertEqual(out.getpixel((1,0)),(80,60,40,255))
  self.assertEqual(out.getpixel((2,0)),(0,0,0,0))
 def test_wrong_canvas_rejected(self):
  with self.assertRaises(ValueError):composite_silhouette(Image.new('RGBA',(4,4)),Image.new('RGB',(2,2)),Image.new('L',(4,4)))
