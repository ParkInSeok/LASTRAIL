import unittest
from approval_gate import guard, approve, state


class GateTests(unittest.TestCase):
    def setUp(self):
        self.p={'production_flow':{'approval_gate':{'key_indices':[4,8],'approvals':{}},'references':{},'frames':{}},'frames':[{} for _ in range(12)]}
        self.valid=lambda p,s,i: True

    def put(self,slot,asset):
        self.p['production_flow']['references'][slot]=[{'asset':asset}]

    def approval(self):
        s=state(self.p,self.valid)
        approve(self.p,{'stage':s['active'],'basis':s['basis'],'confirmed':True,'note':'사용자 승인'},self.valid)

    def test_missing_previous_approval_blocks_request_and_upload(self):
        self.put('reference_line','line')
        for op in ('request','upload','paint'):
            with self.assertRaises(ValueError):guard(self.p,op,'reference_color',-1,self.valid)
        self.approval()
        guard(self.p,'upload','reference_color',-1,self.valid)

    def test_pair_opens_only_key_lines_and_freezes_approved_sources(self):
        self.put('reference_line','line');self.approval()
        self.put('reference_color','color');self.approval()
        guard(self.p,'upload','line',4,self.valid)
        for slot,index in [('line',1),('flat',4),('reference_line',-1)]:
            with self.assertRaises(ValueError):guard(self.p,'upload',slot,index,self.valid)

    def test_changed_asset_invalidates_approval_and_stale_receipt_rejected(self):
        self.put('reference_line','line');s=state(self.p,self.valid);self.put('reference_line','new')
        with self.assertRaises(ValueError):approve(self.p,{'stage':s['active'],'basis':s['basis'],'confirmed':True,'note':'승인'},self.valid)
        self.approval();self.put('reference_line','changed')
        self.assertEqual(state(self.p,self.valid)['active'],0)

    def test_empty_stage_cannot_be_approved(self):
        with self.assertRaises(ValueError):self.approval()

    def test_all_six_phases_require_complete_sets(self):
        self.put('reference_line','line');self.approval()
        self.put('reference_color','color');self.approval()
        frames=self.p['production_flow']['frames']
        frames['4']={'line':[{'asset':'b'}]}
        with self.assertRaises(ValueError):self.approval()
        frames['8']={'line':[{'asset':'c'}]};self.approval()
        self.assertEqual(state(self.p,self.valid)['active'],3)
        with self.assertRaises(ValueError):guard(self.p,'upload','line',1,self.valid)
        for i in (4,8):frames[str(i)]['composite']=[{'asset':f'color{i}'}]
        self.approval()
        for i in range(12):frames.setdefault(str(i),{}).setdefault('line',[{'asset':f'line{i}'}])
        self.approval()
        self.assertEqual(state(self.p,self.valid)['active'],5)
        with self.assertRaises(ValueError):guard(self.p,'adopt','composite',0,self.valid)
        for i in range(12):frames[str(i)].setdefault('composite',[{'asset':f'color{i}'}])
        self.approval();guard(self.p,'adopt','composite',0,self.valid)

if __name__=='__main__':unittest.main()

class StudioGateTests(unittest.TestCase):
    def setUp(self):
        from test_production_flow import ProductionFlowTests
        ProductionFlowTests.setUp(self)

    def act(self,op,**kw):
        p=self.repo.get(self.pid)
        return self.studio.action(self.pid,{'action':'production_flow','op':op,'expected_project_revision':p['revision'],'expected_revision':p.get('production_flow',{}).get('revision',0),**kw})

    def test_http_equivalent_commands_cannot_bypass_gate(self):
        from test_production_flow import png
        self.act('configure',config=self.config,sequence_version=1)
        self.act('upload',slot='reference_line',png=png())
        before=self.repo.get(self.pid)
        for action in ('import_clip','generate','apply_surface_shading','restore','upload','approve_frame'):
            with self.assertRaises(ValueError):self.studio.action(self.pid,{'action':action})
        with self.assertRaises(ValueError):self.act('upload',slot='reference_color',png=png())
        self.assertEqual(before,self.repo.get(self.pid))
        for slot in ('reference_line','reference_color'):
            if slot=='reference_color':self.act('upload',slot=slot,png=png())
            gate=self.studio.view(self.repo.get(self.pid))['production_flow_status']['approval_gate']
            self.act('approve_stage',stage=gate['active'],basis=gate['basis'],confirmed=True,note='실제 사용자 승인 인계')
        self.act('request',slot='line',index=4)
        with self.assertRaises(ValueError):self.act('request',slot='flat',index=4)
        with self.assertRaises(ValueError):self.act('request',slot='line',index=1)

    def test_generic_review_does_not_unlock_phase(self):
        from test_production_flow import png
        self.act('configure',config=self.config,sequence_version=1)
        self.act('upload',slot='reference_line',png=png())
        self.act('review',slot='reference_line',mode='static',passed=True,note='검사 결과일 뿐 사용자 승인 아님')
        with self.assertRaises(ValueError):self.act('request',slot='reference_color')

    def test_new_flow_separates_flat_shadow_masks_and_texture_approval(self):
        from test_production_flow import png
        self.act('configure',config=self.config)
        for slot in ('reference_line','reference_color'):
            self.act('upload',slot=slot,png=png())
        self.act('upload',slot='line',index=0,png=png())
        def approve_active():
            gate=self.studio.view(self.repo.get(self.pid))['production_flow_status']['approval_gate']
            self.act('check_stage',basis=gate['basis'],evidence=dict.fromkeys(gate['stages'][gate['active']]['criteria'],'fixture observation'))
            self.act('approve_stage',stage=gate['active'],basis=gate['basis'],confirmed=True,note='user approved this stage')
        approve_active()
        self.act('upload',slot='flat',index=0,png=png())
        region=self.config['regions'][0]['id']
        with self.assertRaises(ValueError):self.act('mask',index=0,region=region,role='shadow',png=png())
        with self.assertRaises(ValueError):self.act('request',slot='shadow',index=0)
        approve_active()
        with self.assertRaises(ValueError):self.act('mask',index=0,region=region,role='base',png=png())
        self.act('mask',index=0,region=region,role='shadow',png=png())
        self.act('upload',slot='shadow',index=0,png=png())
        self.act('paint',slot='composite',index=0)
        with self.assertRaises(ValueError):self.act('request',slot='texture',index=0)
        approve_active();self.act('request',slot='texture',index=0)
        self.act('upload',slot='texture',index=0,png=png())
        self.act('paint',slot='composite',index=0);approve_active()
        for i in (4,8):self.act('upload',slot='line',index=i,png=png())
        approve_active();p=self.act('request',slot='flat',index=4)
        request=p['production_flow']['requests'][-1]
        self.assertEqual(request['first_pose_assets']['flat'],p['production_flow']['frames']['0']['flat'][-1]['asset'])
        self.assertIn(request['first_pose_assets']['flat'],request['reference_assets'])


class SplitColorGateTests(unittest.TestCase):
    put=GateTests.put
    approval=GateTests.approval
    def setUp(self):
        GateTests.setUp(self)
        self.p['production_flow']['approval_gate']['sequence_version']=2

    def test_reopen_preserves_upstream_and_archives_texture_with_required_evidence(self):
        from approval_gate import reopen_shading, check_stage, criteria
        flow=self.p['production_flow'];flow['history']=[]
        self.put('reference_line','line');self.put('reference_color','style')
        frame=flow['frames']['0']={}
        for slot in ('line','flat','shadow'):
            frame[slot]=[{'asset':slot}];self.approval()
        frame['texture']=[{'asset':'old-texture'}];frame['composite']=[{'asset':'old-composite'}]
        reopen_shading(self.p,{'confirmed':True,'note':'사용자 명암 재작업 지시'},self.valid)
        self.assertEqual(state(self.p,self.valid)['active'],2)
        self.assertEqual(frame['flat'][0]['asset'],'flat')
        self.assertEqual(flow['history'][-1]['archived_candidates']['texture'][0]['asset'],'old-texture')
        self.assertNotIn('texture',frame)
        with self.assertRaises(ValueError):self.approval()
        s=state(self.p,self.valid)
        check_stage(self.p,{'basis':s['basis'],'evidence':dict.fromkeys(criteria(2),'실제 비교 근거')},self.valid)
        frame['shadow'].append({'asset':'new-shadow'})
        with self.assertRaises(ValueError):self.approval()
        s=state(self.p,self.valid)
        check_stage(self.p,{'basis':s['basis'],'evidence':dict.fromkeys(criteria(2),'새 결과 비교 근거')},self.valid)
        self.approval()
        self.assertEqual(state(self.p,self.valid)['active'],3)

    def test_twelve_phases_lock_each_color_layer_and_complete_pose_sets(self):
        flow=self.p['production_flow'];frames=flow['frames']
        self.put('reference_line','reference');self.put('reference_color','style-only')
        frames['0']={'line':[{'asset':'a-line'}]};self.approval()
        self.assertEqual(state(self.p,self.valid)['label'],'첫 자세 밑색')
        for slot in ('shadow','texture','composite'):
            with self.assertRaises(ValueError):guard(self.p,'upload',slot,0,self.valid)
        for stage,slot in [(1,'flat'),(2,'shadow'),(3,'texture')]:
            guard(self.p,'upload',slot,0,self.valid)
            frames['0'][slot]=[{'asset':'a-'+slot}]
            if slot=='texture':frames['0']['composite']=[{'asset':'a-complete'}]
            self.approval()
            with self.assertRaises(ValueError):guard(self.p,'upload',slot,0,self.valid)
        for slot in ('line','flat','shadow','texture'):
            for i in (4,8):
                guard(self.p,'upload',slot,i,self.valid)
                frames.setdefault(str(i),{})[slot]=[{'asset':f'{slot}{i}'}]
                if slot=='texture':frames[str(i)]['composite']=[{'asset':f'complete{i}'}]
                if i==4:
                    with self.assertRaises(ValueError):self.approval()
            self.approval()
        for slot in ('line','flat','shadow','texture'):
            for i in range(12):
                if i in (0,4,8):continue
                guard(self.p,'upload',slot,i,self.valid)
                frames[str(i)]={**frames.get(str(i),{}),slot:[{'asset':f'{slot}{i}'}]}
                if slot=='texture':frames[str(i)]['composite']=[{'asset':f'complete{i}'}]
            self.approval()
        self.assertIsNone(state(self.p,self.valid)['active'])
        self.assertEqual(len(state(self.p,self.valid)['stages']),12)

