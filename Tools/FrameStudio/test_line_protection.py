import base64, io
from PIL import Image
from test_gloss_stage import GlossTests


def mask(box):
    im=Image.new('RGBA',(64,64));im.paste((255,255,255,255),box)
    out=io.BytesIO();im.save(out,format='PNG')
    return base64.b64encode(out.getvalue()).decode()


class LineProtectionTests(GlossTests):
    def test_internal_alpha_pinhole_does_not_restore_black_ink(self):
        from test_production_flow import png
        self.act('configure',config={**self.config,'line_overlay':False,'separate_line_protection':True},sequence_version=3)
        for slot in ('reference_line','reference_color'):
            self.act('upload',slot=slot,png=png())
        source=Image.new('RGBA',(64,64));source.paste((90,80,70,253),(10,10,50,50))
        source.putpixel((27,27),(90,80,70,100))
        stream=io.BytesIO();source.save(stream,format='PNG')
        self.act('upload',slot='line',index=0,png=base64.b64encode(stream.getvalue()).decode())
        self.act('line_protection',index=0,ink=mask((20,20,30,29)),editable=mask((20,20,22,22)),protected=mask((35,35,40,40)))
        self.approve_current()
        self.act('mask',index=0,region=self.config['regions'][0]['id'],role='base',png=mask((10,10,50,50)))
        self.act('upload',slot='flat',index=0,png=png());self.approve_current()
        self.act('mask',index=0,region=self.config['regions'][0]['id'],role='ink_appearance',png=mask((28,26,29,29)))
        self.act('upload',slot='shadow',index=0,png=png((170,160,150,255)))
        result=self.act('paint',slot='composite',index=0)
        with Image.open(self.repo.asset_path(result['production_flow']['frames']['0']['composite'][-1]['asset'])) as out:
            self.assertEqual(out.getpixel((28,27)),(170,160,150,253))
            self.assertEqual(out.getpixel((27,27))[3],100)
            self.assertEqual(out.getpixel((9,27))[3],0)

    def test_semtransparent_interior_ink_can_have_continuous_appearance(self):
        from test_production_flow import png
        self.act('configure',config={**self.config,'line_overlay':False,'separate_line_protection':True},sequence_version=3)
        for slot in ('reference_line','reference_color','line'):
            self.act('upload',slot=slot,index=0 if slot=='line' else -1,png=png((90,80,70,185)))
        self.act('line_protection',index=0,ink=mask((10,10,50,50)),editable=mask((20,20,25,25)),protected=mask((30,30,40,40)))

    def test_separate_appearance_from_geometry_and_approval(self):
        from test_production_flow import png
        self.act('configure',config={**self.config,'line_overlay':False,'separate_line_protection':True},sequence_version=3)
        for slot in ('reference_line','reference_color'):
            self.act('upload',slot=slot,png=png())
        self.act('upload',slot='line',index=0,png=png((90,80,70,253)))
        with self.assertRaises(ValueError):self.approve_current()
        # Editable pixels must be existing ink, away from the contour and protected joints.
        args=dict(index=0,ink=mask((10,10,50,50)),protected=mask((30,30,40,40)))
        with self.assertRaises(ValueError):self.act('line_protection',**args,editable=mask((0,0,0,0)))
        with self.assertRaises(ValueError):self.act('line_protection',**args,editable=mask((10,10,12,12)))
        with self.assertRaises(ValueError):self.act('line_protection',**args,editable=mask((32,32,34,34)))
        self.act('line_protection',**args,editable=mask((20,20,25,25)))
        self.approve_current()
        with self.assertRaises(ValueError):self.act('line_protection',**args,editable=mask((21,21,23,23)))
        self.act('mask',index=0,region=self.config['regions'][0]['id'],role='base',png=mask((10,10,50,50)))
        self.act('upload',slot='flat',index=0,png=png((100,100,100,255)));self.approve_current()
        from PIL import ImageColor
        fixed_rgb=ImageColor.getrgb(self.config['regions'][0]['base'])
        self.act('upload',slot='shadow',index=0,png=png((170,160,150,255)))
        region=self.config['regions'][0]['id']
        with self.assertRaises(ValueError):self.act('mask',index=0,region=region,role='ink_appearance',png=mask((32,32,34,34)))
        self.act('mask',index=0,region=region,role='ink_appearance',png=mask((26,26,28,28)))
        self.act('upload',slot='shadow',index=0,png=png((170,160,150,255)))
        p=self.act('paint',slot='composite',index=0)
        with Image.open(self.repo.asset_path(p['production_flow']['frames']['0']['composite'][-1]['asset'])) as im:
            self.assertEqual(im.getpixel((22,22)),(170,160,150,253))
            self.assertEqual(im.getpixel((26,26)),(170,160,150,253))
            self.assertEqual(im.getpixel((32,32)),fixed_rgb+(253,))
            self.assertEqual(im.getpixel((10,10)),fixed_rgb+(253,))
            self.assertEqual(im.getpixel((5,5))[3],0)
        # Direct composite upload uses the same protection path.
        p=self.act('upload',slot='composite',index=0,png=png((200,200,200,255)))
        with Image.open(self.repo.asset_path(p['production_flow']['frames']['0']['composite'][-1]['asset'])) as im:
            self.assertEqual(im.getpixel((32,32)),fixed_rgb+(253,))

