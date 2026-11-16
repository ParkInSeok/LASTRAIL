import unittest,tempfile
from pathlib import Path
from cutscene_model import validate_timeline,default_timeline
from cutscene_render import command

class LandscapeCutsceneTests(unittest.TestCase):
 def test_landscape_audio_render_size_and_legacy(self):
  t=default_timeline();t.update(width=1920,height=1080)
  clean=validate_timeline(t,{})
  self.assertEqual((clean['width'],clean['height']),(1920,1080))
  with tempfile.TemporaryDirectory() as d:
   folder=Path(d)
   args=command(folder,dict(timeline=clean,media={}),folder/'test.mp4',True)
   self.assertTrue(any('s=640x360' in arg for arg in args))
  legacy=validate_timeline(default_timeline(),{})
  self.assertEqual((legacy['width'],legacy['height']),(1080,1920))
 def test_square_rejected(self):
  with self.assertRaises(ValueError):validate_timeline(dict(width=1080,height=1080),{})
