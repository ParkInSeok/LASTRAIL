import base64, copy, io, tempfile, unittest
from pathlib import Path
from PIL import Image
from domain import new_project
from repository import Repository
from service import Studio


def png(color=(90,80,70,255), size=(64,64)):
    im=Image.new('RGBA',size); im.paste(color,(10,10,50,50))
    out=io.BytesIO(); im.save(out,format='PNG')
    return base64.b64encode(out.getvalue()).decode()


class ProductionFlowTests(unittest.TestCase):
    def test_staged_wind_request_and_changed_basis(self):
        from test_wind_plan import settings
        from wind_plan import request_wind
        from production_flow import current
        self.setup_flow(); self.put('reference_line'); self.put('reference_color')
        self.studio.action(self.pid,{'action':'wind_settings','settings':settings(strength=120)})
        p=self.act('request',slot='line',index=4,expected_revision=3)
        req=p['production_flow']['requests'][-1]
        self.assertEqual(req['wind_guide'],request_wind(p,4)[0])
        self.assertIn('strand-by-strand',req['prompt'])
        self.assertIn('key-pose comparison',req['prompt'])
        self.put('line',4)
        self.studio.action(self.pid,{'action':'wind_settings','settings':settings(strength=80)})
        before=self.repo.get(self.pid)
        self.assertFalse(current(before,'line',4))
        self.assertTrue(current(before,'reference_line',-1))
        with self.assertRaises(ValueError):
            self.act('upload',slot='line',index=4,png=png(),request_id=req['id'],expected_revision=5)
        self.assertEqual(before,self.repo.get(self.pid))

    def test_wind_does_not_redraw_color_stages_or_reference_pair(self):
        from test_wind_plan import settings
        self.setup_flow()
        self.studio.action(self.pid,{'action':'wind_settings','settings':settings(strength=120)})
        p=self.act('request',slot='reference_line',index=-1,expected_revision=1)
        self.assertNotIn('Wind redraw guide',p['production_flow']['requests'][-1]['prompt'])
        self.put('reference_line'); self.put('reference_color'); self.put('line',0)
        p=self.act('request',slot='flat',index=0,expected_revision=5)
        req=p['production_flow']['requests'][-1]
        self.assertNotIn('Wind redraw guide',req['prompt'])
        self.assertIn('approved line geometry',req['prompt'])

    def test_embedded_ink_applies_character_alpha_once(self):
        self.config['line_overlay']=False;self.setup_flow();self.put('reference_line');self.put('reference_color');self.put('line',0)
        self.act('mask',index=0,region='sleeve_r',role='base',png=png((255,255,255,128)),expected_revision=4)
        self.act('mask',index=0,region='sleeve_r',role='shadow',png=png((255,255,255,255)),expected_revision=5)
        self.act('paint',slot='flat',index=0,expected_revision=6)
        self.act('paint',slot='shadow',index=0,expected_revision=7)
        p=self.act('paint',slot='composite',index=0,expected_revision=8)
        with Image.open(self.repo.asset_path(p['production_flow']['frames']['0']['composite'][-1]['asset'])) as im:
            self.assertEqual(im.getpixel((20,20)),(17,34,51,128))
        fx=Image.new('RGBA',(64,64));fx.putpixel((55,55),(255,220,80,255));buf=io.BytesIO();fx.save(buf,format='PNG')
        self.act('upload',slot='fx',index=0,png=base64.b64encode(buf.getvalue()).decode(),expected_revision=9)
        p=self.act('paint',slot='composite',index=0,expected_revision=10)
        with Image.open(self.repo.asset_path(p['production_flow']['frames']['0']['composite'][-1]['asset'])) as im:
            self.assertEqual(im.getpixel((55,55)),(255,220,80,255))

    def test_embedded_ink_does_not_receive_duplicate_line_overlay(self):
        self.config['line_overlay']=False;self.setup_flow()
        self.put('reference_line');self.put('reference_color')
        self.act('upload',slot='line',index=0,png=png((200,0,0,255)),expected_revision=3)
        self.put('flat',0);self.put('shadow',0)
        p=self.act('paint',slot='composite',index=0,expected_revision=6)
        with Image.open(self.repo.asset_path(p['production_flow']['frames']['0']['composite'][-1]['asset'])) as im:
            self.assertEqual(im.getpixel((20,20)),(90,80,70,255))
        self.assertTrue(p['production_flow_status']['frames']['0']['line']['current'])
        with self.assertRaises(ValueError):self.act('configure',config={**self.config,'line_overlay':'false'},expected_revision=7)

    def test_fixed_color_steps_use_only_declared_rgb_and_tone_changes_invalidate(self):
        self.config['regions'][0]['ramps']={'base':['#102030','#506070','#90A0B0']}
        self.setup_flow();self.put('reference_line');self.put('reference_color');self.put('line',0)
        def act(op,**kw):return self.act(op,expected_revision=self.repo.get(self.pid)['production_flow']['revision'],**kw)
        act('mask',index=0,region='sleeve_r',role='base',png=png((255,255,255,255)))
        before=self.repo.get(self.pid)
        with self.assertRaises(ValueError):act('paint',slot='flat',index=0)
        self.assertEqual(before,self.repo.get(self.pid))
        for value,expected in [(0,(16,32,48,255)),(128,(80,96,112,255)),(255,(144,160,176,255))]:
            p=act('mask',index=0,region='sleeve_r',role='base_tone',png=png((value,value,value,255)))
            if value:self.assertFalse(p['production_flow_status']['frames']['0']['flat']['current'])
            p=act('paint',slot='flat',index=0)
            with Image.open(self.repo.asset_path(p['production_flow']['frames']['0']['flat'][-1]['asset'])) as im:
                self.assertEqual(im.getpixel((20,20)),expected)
                self.assertEqual(im.getpixel((0,0))[3],0)
        self.assertEqual(p['frames'],self.p['frames'])

    def test_color_step_contract_rejects_bad_colors_and_missing_coverage(self):
        self.config['regions'][0]['ramps']={'base':['#123456']*17}
        with self.assertRaises(ValueError):self.setup_flow()
        self.config['regions'][0]['ramps']={'base':['#123456','#ABCDEF']}
        self.setup_flow();self.put('reference_line');self.put('reference_color');self.put('line',0)
        self.act('mask',index=0,region='sleeve_r',role='base',png=png((255,255,255,255)),expected_revision=4)
        im=Image.new('RGBA',(64,64));im.putpixel((20,20),(0,0,0,255));buf=io.BytesIO();im.save(buf,format='PNG')
        self.act('mask',index=0,region='sleeve_r',role='base_tone',png=base64.b64encode(buf.getvalue()).decode(),expected_revision=5)
        with self.assertRaises(ValueError):self.act('paint',slot='flat',index=0,expected_revision=6)

    def test_polygon_mask_edit_preserves_sources_and_erases(self):
        self.setup_flow(); self.put('reference_line'); self.put('reference_color'); self.put('line',0)
        before=copy.deepcopy(self.repo.get(self.pid))
        def edit(mode,points):
            p=self.repo.get(self.pid)
            return self.act('mask_edit',index=0,region='sleeve_r',role='base',mode=mode,points=points,expected_revision=p['production_flow']['revision'])
        p=edit('replace',[[12,12],[30,12],[30,30],[12,30]])
        p=edit('erase',[[20,20],[25,20],[25,25],[20,25]])
        asset=p['production_flow']['masks']['0']['sleeve_r']['base'][-1]['asset']
        with Image.open(self.repo.asset_path(asset)) as im:
            self.assertEqual(im.getpixel((15,15)),(255,255,255,255))
            self.assertEqual(im.getpixel((22,22))[3],0)
            self.assertEqual(im.getpixel((31,31))[3],0)
        self.assertEqual(p['production_flow']['references'],before['production_flow']['references'])
        self.assertEqual(p['frames'],before['frames'])
        saved=self.repo.get(self.pid)
        with self.assertRaises(ValueError):edit('add',[[0,0],[64,5],[3,5]])
        self.assertEqual(saved,self.repo.get(self.pid))
        self.put('line',0)
        with self.assertRaises(ValueError):edit('add',[[1,1],[4,1],[4,4]])

    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.repo=Repository(Path(self.tmp.name)); self.studio=Studio(self.repo,object())
        self.p=new_project('test'); self.repo.save(self.p); self.pid=self.p['id']
        self.config={'lighting':'화면 왼쪽 위 고정', 'motion':'손을 들고 복귀, 발 고정',
                     'regions':[{'id':'sleeve_r','name':'오른팔 소매','parent':'',
                                 'material':'천','base':'#334455','light':'#556677','shadow':'#112233'}]}
    def act(self, op, **kw):
        return self.studio.action(self.pid,{'action':'production_flow','op':op,'expected_project_revision':self.repo.get(self.pid)['revision'],**kw})
    def setup_flow(self):
        p=self.repo.get(self.pid)
        p['production_flow']={'revision':0,'references':{},'frames':{},'plans':{},'requests':[],'history':[]}
        self.repo.save(p)
        return self.act('configure',config=self.config,expected_revision=0)
    def put(self,slot,index=-1):
        p=self.repo.get(self.pid)
        return self.act('upload',slot=slot,index=index,png=png(),expected_revision=p['production_flow']['revision'])
    def test_assets_stay_separate_and_requests_use_stage_references(self):
        self.setup_flow(); self.put('reference_line'); self.put('reference_color'); self.put('line',0)
        p=self.act('request',slot='flat',index=0,expected_revision=3+1)
        self.assertEqual(p['frames'],self.p['frames'])
        req=p['production_flow']['requests'][-1]
        self.assertIn('sleeve_r',req['prompt']); self.assertEqual(len(req['reference_assets']),3)
        self.assertEqual(req['slot'],'flat')
    def test_bad_config_and_stale_write_are_atomic(self):
        self.setup_flow(); before=self.repo.get(self.pid)
        with self.assertRaises(ValueError):self.act('configure',config=self.config,expected_revision=0)
        bad=copy.deepcopy(self.config);bad['regions'][0]['parent']='missing'
        with self.assertRaises(ValueError):self.act('configure',config=bad,expected_revision=1)
        self.assertEqual(before,self.repo.get(self.pid))
    def test_review_is_invalidated_by_upstream_only_and_never_approves_actor(self):
        self.setup_flow();self.put('reference_line');self.put('reference_color')
        self.put('line',0); self.put('flat',0)
        p=self.act('review',slot='flat',index=0,note='영역과 색 확인',mode='static',passed=True,expected_revision=5)
        self.assertEqual(p['production_flow']['frames']['0']['flat'][-1]['review']['mode'],'static')
        self.put('line',1)
        p=self.studio.view(self.repo.get(self.pid))
        self.assertTrue(p['production_flow_status']['frames']['0']['flat']['current'])
        self.put('line',0)
        p=self.studio.view(self.repo.get(self.pid))
        self.assertFalse(p['production_flow_status']['frames']['0']['flat']['current'])

    def test_changed_line_requires_mask_redraw_and_adjacent_review_is_stale(self):
        self.setup_flow();self.put('reference_line');self.put('reference_color');self.put('line',0);self.put('line',1)
        self.act('review',slot='line',index=0,note='1→2 연결 검사',mode='transitions',passed=True,expected_revision=5)
        self.put('line',1)
        p=self.studio.view(self.repo.get(self.pid))
        self.assertIsNone(p['production_flow_status']['frames']['0']['line']['review'])
        self.act('mask',index=0,region='sleeve_r',role='base',png=png((255,255,255,255)),expected_revision=7)
        self.put('line',0)
        with self.assertRaises(ValueError):self.act('paint',slot='flat',index=0,expected_revision=9)

    def test_old_external_request_cannot_be_imported_as_current(self):
        self.setup_flow();self.put('reference_line');self.put('reference_color')
        p=self.act('request',slot='line',index=0,expected_revision=3)
        request=p['production_flow']['requests'][-1]['id']
        self.act('configure',config={**self.config,'motion':'다른 동작'},expected_revision=4)
        with self.assertRaises(ValueError):self.act('upload',slot='line',index=0,png=png(),request_id=request,expected_revision=5)
        self.assertFalse(any(f['approved'] for f in p['frames']))
    def test_size_mismatch_and_missing_prerequisite_rejected(self):
        self.setup_flow();self.put('reference_line')
        before=self.repo.get(self.pid)
        with self.assertRaises(ValueError):self.act('upload',slot='reference_color',index=-1,png=png(size=(32,32)),expected_revision=2)
        with self.assertRaises(ValueError):self.put('shadow',0)
        self.assertEqual(before,self.repo.get(self.pid))
    def test_shadows_require_valid_owner_and_caster(self):
        self.setup_flow(); before=self.repo.get(self.pid)
        with self.assertRaises(ValueError):self.act('frame_plan',index=0,plan={'shadows':[{'owner':'sleeve_r','kind':'cast','caster':'missing','note':'팔 그림자'}]},expected_revision=1)
        self.assertEqual(before,self.repo.get(self.pid))

    def test_region_mask_paints_exact_palette_and_stales_on_mask_change(self):
        self.setup_flow();self.put('reference_line');self.put('reference_color');self.put('line',0)
        p=self.act('mask',index=0,region='sleeve_r',role='base',png=png((255,255,255,255)),expected_revision=4)
        p=self.act('paint',slot='flat',index=0,expected_revision=5)
        item=p['production_flow']['frames']['0']['flat'][-1]
        with Image.open(self.repo.asset_path(item['asset'])) as im:self.assertEqual(im.getpixel((20,20)),(51,68,85,255))
        p=self.act('mask',index=0,region='sleeve_r',role='base',png=png((128,128,128,255)),expected_revision=6)
        self.assertFalse(p['production_flow_status']['frames']['0']['flat']['current'])

    def test_adoption_keeps_stage_chain_current_and_character_edit_stales_it(self):
        self.setup_flow();self.put('reference_line');self.put('reference_color')
        for slot in ('line','flat','shadow','composite'):self.put(slot,0)
        p=self.act('review',slot='composite',index=0,note='정지 검토만',mode='static',passed=True,expected_revision=7)
        p=self.act('adopt',slot='composite',index=0,expected_revision=8)
        self.assertTrue(p['production_flow_status']['frames']['0']['composite']['current'])
        self.assertFalse(p['frames'][0]['approved'])
        p=self.studio.action(self.pid,{'action':'save','identity':'주손 변경','motion':'','hidden':''})
        self.assertFalse(p['production_flow_status']['frames']['0']['composite']['current'])

    def test_partial_export_rejected_and_complete_export_keeps_actor_untouched(self):
        self.setup_flow();self.put('reference_line');self.put('reference_color');self.put('line',0)
        with self.assertRaises(ValueError):self.act('export',slot='line',index=0,expected_revision=4)
        for i in range(1,12):self.put('line',i)
        before=self.repo.get(self.pid)['frames']
        p=self.act('export',slot='line',index=0,expected_revision=15)
        self.assertEqual(before,p['frames'])
        self.assertTrue(self.repo.asset_path(p['production_flow']['exports']['line']['files']['apng']).exists())

    def test_failed_reference_blocks_downstream_and_empty_reference_is_rejected(self):
        self.setup_flow();self.put('reference_line');self.put('reference_color')
        self.act('review',slot='reference_line',note='선화와 채색 구조 불일치',mode='static',passed=False,expected_revision=3)
        with self.assertRaises(ValueError):self.put('line',0)
        with self.assertRaises(ValueError):self.act('upload',slot='reference_line',png=png((0,0,0,0)),expected_revision=4)


if __name__=='__main__':unittest.main()
