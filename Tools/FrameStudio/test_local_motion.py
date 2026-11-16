import io, tempfile, unittest
from pathlib import Path
from PIL import Image
from local_motion import validate_request, select_indices, prepare_reference, extract_twelve

class MotionContractTests(unittest.TestCase):
    def test_prompt_and_reference_confirmation_required(self):
        with self.assertRaises(ValueError): validate_request({'prompt':' ', 'approved_reference':True})
        with self.assertRaises(ValueError): validate_request({'prompt':'breathe', 'approved_reference':False})
        with self.assertRaises(ValueError): validate_request({'prompt':'breathe', 'approved_reference':True, 'seed':True})
    def test_twelve_ordered_distinct_samples_include_endpoints(self):
        indices=select_indices(33)
        self.assertEqual(len(indices),12)
        self.assertEqual(len(set(indices)),12)
        self.assertEqual((indices[0],indices[-1]),(0,32))
        with self.assertRaises(ValueError): select_indices(8)
    def test_reference_fits_without_stretch_and_is_not_cut(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'ref.png';Image.new('RGBA',(200,400),(200,100,50,255)).save(p)
            out=Path(d)/'input.png'; info=prepare_reference(p,out)
            with Image.open(out) as im:
                self.assertEqual(im.size,(288,512))
                self.assertEqual(info['resized'],[200,400])
                self.assertEqual(im.getpixel((0,0)),(52,64,73))
    def test_extract_retains_source_indices_and_twelve_pngs(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);frames=[Image.new('RGB',(32,32),(i*4,10,20)) for i in range(49)]
            source=root/'generated.webp'
            frames[0].save(source,save_all=True,append_images=frames[1:],duration=63,lossless=True)
            result=extract_twelve(source,root/'out')
            self.assertEqual(len(result['paths']),12)
            with Image.open(result['paths'][-1]) as im:
                self.assertEqual(im.getpixel((0,0)),(192,10,20,255))
            self.assertEqual(result['source_frames'],49)
            self.assertFalse(result['alpha_verified'])
    def test_actor_input_removes_faint_empty_margin_not_actor(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'ref.png'; im=Image.new('RGBA',(1000,1000),(0,0,0,0))
            im.paste((200,100,50,255),(400,50,600,950));im.putpixel((0,0),(20,20,20,1));im.save(p)
            before=p.read_bytes();info=prepare_reference(p,Path(d)/'input.png',actor_fit=True)
            self.assertEqual(p.read_bytes(),before)
            self.assertGreater(info['resized'][1],450)
            self.assertLessEqual(info['crop'][0],400);self.assertGreaterEqual(info['crop'][2],600)

if __name__=='__main__': unittest.main()
