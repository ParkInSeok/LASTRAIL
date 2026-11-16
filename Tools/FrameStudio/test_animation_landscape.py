import unittest
from pathlib import Path
from animation_model import validate_scene
from animation_render import draw_frame

class LandscapeTest(unittest.TestCase):
    def test_landscape_coordinates_and_legacy_default(self):
        row=dict(id='edge',kind='rect',color='#ff0000',width=100,height=100,pivot_x=0,pivot_y=0,keys=[dict(frame=0,x=1700,y=500)])
        s=validate_scene(dict(width=1920,height=1080,parts=[row]),{})
        self.assertEqual((s['width'],s['height']),(1920,1080))
        im=draw_frame(s,{},Path('.'),0,960,540,{})
        self.assertEqual(im.getpixel((870,270)),(255,0,0))
        legacy=validate_scene(dict(parts=[]),{})
        self.assertEqual((legacy['width'],legacy['height']),(1080,1920))
    def test_only_supported_canvases(self):
        with self.assertRaises(ValueError):validate_scene(dict(width=1200,height=1200,parts=[]),{})

if __name__=='__main__':unittest.main()
