import base64, io, json, tempfile, unittest
from pathlib import Path
from PIL import Image
from domain import new_project
from repository import Repository
from service import Studio

def png(color=(120,80,70,255), size=(64,64), box=(20,10,40,54)):
    image=Image.new('RGBA',size)
    if box:image.paste(color,box)
    data=io.BytesIO();image.save(data,format='PNG')
    return base64.b64encode(data.getvalue()).decode()

class ClipWorkflowTests(unittest.TestCase):
    def setUp(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup)
        self.repo=Repository(Path(temp.name));self.studio=Studio(self.repo,object())
        self.p=new_project('clip');self.repo.save(self.p);self.pid=self.p['id']
    def action(self, action, **data):return self.studio.action(self.pid,{'action':action,**data})
    def rows(self):return [{'png':png((100+i*30,80,70,255)),'fx_png':png((255,220,40,255),box=(29,5,32,8)),'duration_ms':80,'phase':'사격' if i else '준비','kind':'key'} for i in range(3)]
    def imported(self):return self.action('import_clip',frames=self.rows())
    def battle(self):
        self.action('battle_settings',png=png(size=(160,180),box=(0,0,160,180)),anchor=[.5,.7],height=50)
        return self.action('battle_export')
    def projectile(self, **extra):
        return self.action('projectile_settings',enabled=True,png=png(size=(32,32),box=(8,14,24,17)),fire_frame=1,muzzle=[.47,.14],angle_deg=-90,speed=300,duration_ms=100,length=12,width=3,**extra)
    def test_bulk_import_keeps_timing_separate_fx_and_prior_history(self):
        p=self.imported();self.assertEqual(len(p['frames']),3)
        self.assertEqual([f['duration_ms'] for f in p['frames']],[80]*3)
        self.assertTrue(all(f['fx_asset'] for f in p['frames']))
        self.assertFalse(p['clip_approved'])
        old=[f['versions'][-1]['asset'] for f in p['frames']]
        p=self.imported();self.assertEqual([f['versions'][-1]['asset'] for f in p['clip_history'][-1]],old)
    def test_bad_late_frame_does_not_partially_replace_clip(self):
        self.imported();before=self.repo.get(self.pid);files=list(self.repo.root.joinpath('assets').iterdir())
        rows=self.rows();rows[-1]['fx_png']=png(size=(32,32))
        with self.assertRaises(ValueError):self.action('import_clip',frames=rows)
        self.assertEqual(self.repo.get(self.pid),before)
        self.assertEqual(list(self.repo.root.joinpath('assets').iterdir()),files)
    def test_malformed_batch_is_rejected_without_modifying_project(self):
        self.imported();before=self.repo.get(self.pid)
        for value in (None,42,{'not':'png'}):
            rows=self.rows();rows[-1]['png']=value
            with self.assertRaises(ValueError):self.action('import_clip',frames=rows)
            self.assertEqual(self.repo.get(self.pid),before)
    def test_legacy_signatures_survive_when_new_fields_are_absent(self):
        from delivery_review import signature
        p=self.repo.get(self.pid)
        import hashlib
        payload={key:p.get(key) for key in ('revision','refs','identity','motion','hidden','battle_settings')}
        payload['frames']=[(None,f['duration_ms'],f.get('notes')) for f in p['frames']]
        self.assertEqual(signature(p),hashlib.sha256(json.dumps(payload,sort_keys=True).encode()).hexdigest())
    def test_direction_status_uses_explicit_identity_and_current_approval(self):
        self.imported();self.action('clip_identity',character='레이나',motion='공격',direction=4)
        self.battle();self.action('approve_clip')
        rows=self.studio.list_projects();item=next(p for p in rows if p['id']==self.pid)
        self.assertEqual(item['clip_identity'],{'character':'레이나','motion':'공격','direction':4})
        self.assertTrue(item['clip_approved'])
        self.action('retime',durations=[100,80,80]);self.assertFalse(self.studio.list_projects()[0]['clip_approved'])
        with self.assertRaises(ValueError):self.action('clip_identity',character='레이나',motion='공격',direction=0)
    def test_fx_edit_invalidates_saved_battle_and_approval(self):
        self.imported();self.battle();self.action('approve_clip')
        p=self.action('frame_fx',index=1,png=png(box=(28,3,34,9)))
        self.assertFalse(p['battle_current']);self.assertFalse(p['clip_approved'])
        p=self.action('frame_fx',index=1,png=None);self.assertIsNone(p['frames'][1]['fx_asset'])
        self.assertEqual(len(p['frames'][1]['fx_versions']),3)
    def test_frame_fx_is_in_export_and_actor_is_immutable(self):
        p=self.imported();actor=self.repo.asset_path(p['frames'][0]['versions'][-1]['asset']).read_bytes()
        p=self.battle()
        with Image.open(self.repo.asset_path(p['battle_export']['apng'])) as im:
            r,g,b,a=im.convert('RGBA').getpixel((80,73))
            self.assertGreater(r,240);self.assertGreater(g,180);self.assertLess(b,80);self.assertEqual(a,255)
        self.assertEqual(self.repo.asset_path(p['frames'][0]['versions'][-1]['asset']).read_bytes(),actor)
    def test_projectile_is_one_shot_and_retiming_follows_fire_frame(self):
        self.imported();self.projectile();p=self.battle()
        report=json.loads(self.repo.asset_path(p['battle_export']['fx_manifest']).read_text())
        self.assertEqual(report['shot']['fire_ms'],80)
        flight=[row['bullet'] for row in report['timeline'] if row['bullet']]
        self.assertGreater(len(flight),1);self.assertEqual({r['shot_id'] for r in flight},{1})
        self.assertLess(flight[-1]['center'][1],flight[0]['center'][1])
        self.assertEqual(sum(row['duration_ms'] for row in report['timeline']),240)
        self.action('retime',durations=[120,80,80]);p=self.battle()
        report=json.loads(self.repo.asset_path(p['battle_export']['fx_manifest']).read_text())
        self.assertEqual(report['shot']['fire_ms'],120)
        self.assertEqual(sum(row['duration_ms'] for row in report['timeline']),280)
    def test_projectile_change_stales_approval_and_survives_reload(self):
        self.imported();self.projectile();self.battle();self.action('approve_clip')
        p=self.action('projectile_settings',**{**self.repo.get(self.pid)['battle_projectile'],'speed':400})
        self.assertFalse(p['clip_approved']);self.assertFalse(p['battle_current'])
        fresh=Studio(self.repo,object()).view(self.repo.get(self.pid))
        self.assertEqual(fresh['battle_projectile']['speed'],400)
    def test_bad_projectile_values_and_late_frame_fx_fail_without_mutation(self):
        self.imported();p=self.projectile();before=self.repo.get(self.pid)
        for key,value in [('muzzle',[float('nan'),.2]),('fire_frame',True),('fire_frame',3),('speed',-1),('angle_deg',float('inf')),('duration_ms',15)]:
            cfg=dict(p['battle_projectile']);cfg[key]=value
            with self.assertRaises(ValueError):self.action('projectile_settings',**cfg)
            self.assertEqual(self.repo.get(self.pid),before)

if __name__=='__main__':unittest.main()
