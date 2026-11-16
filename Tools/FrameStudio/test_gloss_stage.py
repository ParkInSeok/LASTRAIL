import unittest
from test_approval_gate import StudioGateTests
from test_production_flow import png
from PIL import Image
import io, base64


class GlossTests(StudioGateTests):
    def approve_current(self):
        gate=self.studio.view(self.repo.get(self.pid))['production_flow_status']['approval_gate']
        self.act('check_stage',basis=gate['basis'],evidence=dict.fromkeys(gate['stages'][gate['active']]['criteria'],'test observation'))
        self.act('approve_stage',stage=gate['active'],basis=gate['basis'],confirmed=True,note='fixture approval')

    def test_fifteen_stages_and_masked_gloss_preserve_shadow(self):
        p=self.act('configure',config={**self.config,'line_overlay':False},sequence_version=3)
        self.assertEqual(len(p['production_flow_status']['approval_gate']['stages']),15)
        for slot in ('reference_line','reference_color'):
            self.act('upload',slot=slot,png=png())
        self.act('upload',slot='line',index=0,png=png());self.approve_current()
        region=self.config['regions'][0]['id']
        self.act('mask',index=0,region=region,role='base',png=png((255,255,255,255)))
        self.act('upload',slot='flat',index=0,png=png((50,50,50,255)));self.approve_current()
        self.act('upload',slot='shadow',index=0,png=png((20,20,20,255)))
        with self.assertRaises(ValueError):self.act('request',slot='gloss',index=0)
        self.approve_current()
        before=self.repo.get(self.pid)['production_flow']['frames']['0']['shadow']
        with self.assertRaises(ValueError):self.act('request',slot='texture',index=0)
        with self.assertRaises(ValueError):self.act('upload',slot='gloss',index=0,png=png())
        self.act('mask_edit',index=0,region=region,role='gloss',mode='replace',points=[[10,10],[20,10],[20,20],[10,20]])
        self.act('upload',slot='gloss',index=0,png=png((240,240,240,255)))
        p=self.act('paint',slot='composite',index=0)
        asset=p['production_flow']['frames']['0']['composite'][-1]['asset']
        with Image.open(self.repo.asset_path(asset)) as im:
            self.assertEqual(im.getpixel((15,15)),(240,240,240,255))
            self.assertEqual(im.getpixel((30,30)),(20,20,20,255))
        self.assertEqual(before,p['production_flow']['frames']['0']['shadow'])
        self.approve_current()
        with self.assertRaises(ValueError):self.act('mask_edit',index=0,region=region,role='gloss',mode='erase',points=[[0,0],[2,0],[2,2]])
        self.act('upload',slot='texture',index=0,png=png((0,0,0,0)))
        self.act('paint',slot='composite',index=0);self.approve_current()
        p=self.act('request',slot='line',index=4)
        self.assertEqual(p['production_flow_status']['approval_gate']['active'],5)

if __name__=='__main__':unittest.main()


class FifteenSequenceTests(unittest.TestCase):
    def test_all_fifteen_phases_and_explicit_reopen(self):
        import approval_gate as gate
        p={'frames':[{} for _ in range(12)],'production_flow':{'approval_gate':{'sequence_version':3,'key_indices':[4,8],'approvals':{}},'references':{'reference_line':[{'asset':'rl'}],'reference_color':[{'asset':'rc'}]},'frames':{},'history':[]}}
        valid=lambda p,s,i:True
        frames=p['production_flow']['frames']
        for stage in range(15):
            self.assertEqual(gate.state(p,valid)['active'],stage)
            indices=[0] if stage<5 else [4,8] if stage<10 else [i for i in range(12) if i not in (0,4,8)]
            slot=('line','flat','shadow','gloss','texture')[stage%5]
            for i in indices:
                gate.guard(p,'upload',slot,i,valid)
                frames.setdefault(str(i),{})[slot]=[{'asset':f'{slot}{i}'}]
                if slot=='texture':frames[str(i)]['composite']=[{'asset':f'composite{i}'}]
            state=gate.state(p,valid)
            gate.approve(p,{'stage':stage,'basis':state['basis'],'confirmed':True,'note':'fixture'},valid)
        self.assertIsNone(gate.state(p,valid)['active'])
        p['production_flow']['approval_gate']['approvals']={k:v for k,v in p['production_flow']['approval_gate']['approvals'].items() if int(k)<4}
        gate.reopen_shading(p,{'confirmed':True,'note':'explicit rework'},valid)
        self.assertEqual(gate.state(p,valid)['active'],2)
        self.assertNotIn('gloss',frames['0'])
