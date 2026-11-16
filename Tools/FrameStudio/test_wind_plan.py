import copy, tempfile, unittest
from pathlib import Path
from domain import new_project,build_request
from refinement import refinement_request
from repository import Repository
from service import Studio
from delivery_review import signature

def settings(**extra):
    cfg={'enabled':True,'strength':50,'forward_angle':-90,'flutter':.12,'region':'hair','parts':{'hair':{'gain':1,'delay':0},'ribbon':{'gain':1.2,'delay':.08},'coat':{'gain':.8,'delay':.15}}}
    cfg.update(extra);return cfg

class WindPlanTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.repo=Repository(Path(self.temp.name));self.studio=Studio(self.repo,object())
        self.p=new_project('wind');self.repo.save(self.p)
    def save(self,cfg):return self.studio.action(self.p['id'],{'action':'wind_settings','settings':cfg})
    def preview(self,cfg):return self.studio.wind_preview(self.p['id'],{'settings':cfg})
    def test_preview_is_read_only_and_strength_scales_all_parts(self):
        before=self.repo.get(self.p['id']);weak=self.preview(settings(strength=25));strong=self.preview(settings(strength=75))
        for part in ('hair','ribbon','coat'):
            self.assertAlmostEqual(strong['frames'][5]['parts'][part]['offset_percent'][1],3*weak['frames'][5]['parts'][part]['offset_percent'][1])
        self.assertEqual(self.repo.get(self.p['id']),before)
    def test_strength_120_is_saved_and_not_clipped_to_100(self):
        self.assertEqual(self.save(settings(strength=120))['wind_settings']['strength'],120)
        a=self.preview(settings(strength=80));b=self.preview(settings(strength=120))
        for part in ('hair','ribbon','coat'):
            self.assertAlmostEqual(b['frames'][5]['parts'][part]['offset_percent'][1],1.5*a['frames'][5]['parts'][part]['offset_percent'][1])
            self.assertLessEqual(b['frames'][5]['parts'][part]['intensity'],1)
        with self.assertRaises(ValueError):self.save(settings(strength=121))
    def test_forward_direction_means_opposite_wind_and_closed_loop(self):
        for angle,axis,sign in [(-90,1,1),(0,0,-1),(90,1,-1)]:
            result=self.preview(settings(forward_angle=angle))
            self.assertGreater(sign*result['frames'][5]['parts']['hair']['offset_percent'][axis],0)
            for frame in (result['frames'][0],result['frames'][-1]):
                for part in frame['parts'].values():self.assertEqual(part['offset_percent'],[0.0,0.0])
    def test_zero_strength_and_disabled_add_no_gust(self):
        for cfg in [settings(strength=0),settings(enabled=False)]:
            for frame in self.preview(cfg)['frames']:
                self.assertTrue(all(part['intensity']==0 for part in frame['parts'].values()))
    def test_part_delays_are_independent(self):
        result=self.preview(settings())
        peaks={part:max(range(12),key=lambda i:result['frames'][i]['parts'][part]['intensity']) for part in ('hair','coat')}
        self.assertGreater(peaks['coat'],peaks['hair'])
    def test_envelope_uses_exposure_time_not_frame_number(self):
        self.p['frames'][0]['duration_ms']=900;self.repo.save(self.p)
        result=self.preview(settings())
        self.assertAlmostEqual(result['frames'][1]['phase'],900/(900+120*10))
    def test_invalid_values_rejected_without_state_changes(self):
        before=self.repo.get(self.p['id'])
        for cfg in [settings(strength=float('nan')),settings(strength=-1),settings(forward_angle=float('inf')),settings(flutter=.5),settings(region='body'),settings(enabled='yes'),settings(parts={'hair':{'gain':1,'delay':0}})]:
            with self.assertRaises(ValueError):self.save(cfg)
            self.assertEqual(self.repo.get(self.p['id']),before)
    def test_saved_settings_history_and_signature_invalidation(self):
        before=self.repo.get(self.p['id']);original=signature(before)
        p=self.save(settings());self.assertNotEqual(signature(p),original)
        same=self.save(settings());self.assertEqual(same['revision'],p['revision'])
        updated=self.save(settings(strength=80));self.assertEqual(updated['wind_history'][-1]['settings'],settings())
        self.assertEqual(updated['frames'],before['frames'])
    def test_refinement_and_full_redraw_share_saved_wind_plan(self):
        p=self.save(settings(strength=25));p['refs']['first']='first.png';p.update(refinement_mask='mask.png',refinement_prompt='hair',character_approved=True,identity='x',motion='x',hidden='x')
        for role in ('front','back','left','right'):p['refs'][role]=role+'.png'
        for f in p['frames']:f['versions']=[{'asset':'source.png'}];f['approved']=True
        weak=refinement_request(p,5,42);full=build_request(p,5)
        p['wind_settings']['strength']=75;strong=refinement_request(p,5,42)
        self.assertLess(weak['refinement_denoise'],strong['refinement_denoise'])
        self.assertEqual(weak['wind_guide'],full['wind_guide'])
        self.assertIn('Wind redraw guide',full['prompt']);self.assertIn('Do not translate',strong['prompt'])
        self.assertGreaterEqual(strong['refinement_denoise'],.16);self.assertLessEqual(strong['refinement_denoise'],.55)
    def test_legacy_refinement_without_profile_keeps_existing_behavior(self):
        p=self.p;p['refs']['first']='first.png';p.update(refinement_mask='mask.png',refinement_prompt='hair',refinement_wind=True)
        for f in p['frames']:f['versions']=[{'asset':'source.png'}]
        req=refinement_request(p,5,42)
        self.assertNotIn('wind_guide',req);self.assertGreater(req['motion_amount'],.9)
    def test_local_trial_uses_the_same_wind_guide(self):
        from batch import draft_request
        p=self.save(settings(strength=65));p['refs']['first']='first.png'
        req=draft_request(p,5,42)
        self.assertEqual(req['wind_guide'],self.preview(settings(strength=65))['frames'][5])
        self.assertIn('Wind redraw guide',req['prompt'])

if __name__=='__main__':unittest.main()
