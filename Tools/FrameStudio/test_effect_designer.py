import copy,json
from PIL import Image
from test_clip_workflow import ClipWorkflowTests
from battle_scene import BattleScene

class EffectDesignerTests(ClipWorkflowTests):
    def test_kyle_approved_source_and_separate_impact(self):
        self.imported();self.projectile()
        p=self.design(shape='kyle',impact_pattern='kyle',impact_scale=1)
        self.assertEqual(p['battle_projectile']['length'],22)
        self.assertEqual(p['battle_projectile']['width'],12)
        self.assertEqual(p['effect_style']['impact_pattern'],'kyle')
        from effect_design import ImpactArtwork,validated_style
        hit=ImpactArtwork(validated_style(p['effect_style']))
        self.assertIsNotNone(hit.render(0).getbbox())
        self.assertIsNone(hit.render(40).getbbox())
    def design(self,**overrides):
        style=dict(shape='teardrop',color='#ffb430',core='#ffffec',glow=1.2,impact_enabled=True,impact_scale=2.2,impact_duration_ms=40)
        style.update(overrides)
        return self.action('effect_design',style=style)
    def test_design_persists_separate_sprite_and_invalidates_review(self):
        self.imported();self.projectile();self.battle();self.action('approve_clip')
        before=copy.deepcopy(self.repo.get(self.pid)['frames']);p=self.design()
        self.assertEqual(p['frames'],before);self.assertFalse(p['battle_current']);self.assertFalse(p['clip_approved'])
        self.assertEqual(self.repo.get(self.pid)['effect_style']['shape'],'teardrop')
        with Image.open(self.repo.asset_path(p['battle_projectile']['tracer'])) as im:
            self.assertEqual(im.mode,'RGBA');self.assertEqual(im.getchannel('A').getextrema()[0],0)
    def test_invalid_design_does_not_mutate_project(self):
        self.imported();self.projectile();before=self.repo.get(self.pid)
        for change in [dict(shape='unknown'),dict(color='bad'),dict(glow=float('nan')),dict(impact_scale=30),dict(impact_enabled='true'),dict(impact_duration_ms=15)]:
            with self.assertRaises(ValueError):self.design(**change)
            self.assertEqual(before,self.repo.get(self.pid))
    def test_impact_follows_flight_endpoint_and_is_exported(self):
        self.imported();self.projectile();self.design();p=self.battle()
        report=json.loads(self.repo.asset_path(p['battle_export']['fx_manifest']).read_text())
        hits=[r for r in report['timeline'] if r.get('impact')]
        self.assertEqual(hits[0]['time_ms'],180);self.assertTrue(all(r['bullet'] is None for r in hits))
        self.assertEqual(hits[-1]['time_ms'],200)
        import zipfile
        with zipfile.ZipFile(self.repo.asset_path(p['battle_export']['zip'])) as z:self.assertTrue(any(n.startswith('impact/') for n in z.namelist()))
        self.design(impact_enabled=False);p=self.battle()
        report=json.loads(self.repo.asset_path(p['battle_export']['fx_manifest']).read_text());self.assertFalse(any(r.get('impact') for r in report['timeline']))
    def test_all_shapes_generate_distinct_sprites(self):
        self.imported();self.projectile();assets=[]
        for shape in ['teardrop','orb','ember','ring','arcane']:
            p=self.design(shape=shape);assets.append(self.repo.asset_path(p['battle_projectile']['tracer']).read_bytes())
        self.assertEqual(len(set(assets)),5)
    def test_long_impact_cannot_silently_truncate_at_loop_end(self):
        self.imported();self.projectile();self.design(impact_duration_ms=160)
        with self.assertRaisesRegex(ValueError,'시간표 끝'):
            self.battle()
    def test_portable_package_preserves_alpha_and_exact_default_impact_duration(self):
        self.imported();self.projectile();p=self.design(impact_duration_ms=160)
        import zipfile,io
        with zipfile.ZipFile(self.repo.asset_path(p['effect_assets']['zip'])) as z:
            frames=[n for n in z.namelist() if n.startswith('impact/')]
            self.assertEqual(len(frames),8)
            for name in ['projectile.png',*frames]:
                with Image.open(io.BytesIO(z.read(name))) as im:self.assertEqual(im.getchannel('A').getextrema()[0],0)

if __name__=='__main__':
    import unittest
    unittest.main()
