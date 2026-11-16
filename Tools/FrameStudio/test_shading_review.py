import copy
import unittest
import approval_gate
import shading_review


class ShadingReviewTests(unittest.TestCase):
    def setUp(self):
        self.p={'frames':[{} for _ in range(12)],'production_flow':{
            'config':{'regions':[{'id':'hair','name':'머리'}]},
            'references':{'reference_line':[{'asset':'ref-line'}],'reference_color':[{'asset':'target'}]},
            'frames':{'0':{'line':[{'asset':'line'}],'flat':[{'asset':'flat'}]}},
            'approval_gate':{'sequence_version':2,'key_indices':[4,8],'approvals':{},'surface_review_required':True}}}
        self.current=lambda p,s,i:True
        for stage in (0,1):
            s=approval_gate.state(self.p,self.current)
            approval_gate.approve(self.p,{'stage':stage,'basis':s['basis'],'confirmed':True,'note':'existing approval'},self.current)
        self.frame=self.p['production_flow']['frames']['0']
        self.frame.update(shadow=[{'asset':'shadow'}],composite=[{'asset':'composite'}])

    def payload(self,passed=True):
        return {'index':0,'region':'hair','basis':shading_review.basis(self.p,0),
                'candidate_box':[.1,.1,.2,.3],'target_box':[.2,.2,.3,.4],
                'observations':dict.fromkeys(shading_review.FIELDS,'actual comparison observation'), 'passed':passed}

    def approve(self):
        s=approval_gate.state(self.p,self.current)
        approval_gate.approve(self.p,{'stage':2,'basis':s['basis'],'confirmed':True,'note':'user approved'},self.current)

    def test_missing_failed_and_changed_composite_block_approval(self):
        with self.assertRaises(ValueError):self.approve()
        shading_review.record(self.p,self.payload(False),self.current)
        with self.assertRaises(ValueError):self.approve()
        shading_review.record(self.p,self.payload(),self.current)
        before=copy.deepcopy(self.p)
        self.frame['composite'].append({'asset':'changed'})
        with self.assertRaises(ValueError):self.approve()
        self.p=before
        self.approve()
        self.assertEqual(approval_gate.state(self.p,self.current)['active'],3)

    def test_invalid_boxes_stale_basis_and_incomplete_notes_rejected(self):
        for change in ({'candidate_box':[0,0,0,1]}, {'target_box':[0,0,2,1]},
                       {'candidate_box':[0,0,float('nan'),1]}, {'basis':'old'},
                       {'observations':{}}, {'passed':'true'}, {'index':4}):
            data=self.payload();data.update(change)
            with self.subTest(change=change),self.assertRaises(ValueError):
                shading_review.record(self.p,data,self.current)

    def test_old_projects_unchanged_and_stale_source_blocks_review(self):
        old=copy.deepcopy(self.p)
        del old['production_flow']['approval_gate']['surface_review_required']
        s=approval_gate.state(old,self.current)
        approval_gate.approve(old,{'stage':2,'basis':s['basis'],'confirmed':True,'note':'old flow'},self.current)
        with self.assertRaises(ValueError):
            shading_review.record(self.p,self.payload(),lambda p,s,i:s!='composite')

    def test_texture_composite_does_not_revoke_approved_shading_review(self):
        shading_review.record(self.p,self.payload(),self.current)
        self.approve()
        self.frame['texture']=[{'asset':'texture'}]
        self.frame['composite'].append({'asset':'texture-composite'})
        self.assertEqual(approval_gate.state(self.p,self.current)['active'],3)
        self.frame['shadow'].append({'asset':'changed-shadow'})
        self.assertEqual(approval_gate.state(self.p,self.current)['active'],2)

if __name__=='__main__':unittest.main()
