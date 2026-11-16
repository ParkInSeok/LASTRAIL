import copy, io, tempfile, time, unittest, wave
from pathlib import Path
from PIL import Image
from animation_model import validate_scene, sample_pose, world_matrices, demo_scene
from animation_render import draw_frame
from animation_service import AnimationService
from cutscene_service import CutsceneService
from repository import Repository
from domain import new_project

def part(ident="a", **kw):
    p=dict(id=ident,name=ident,parent=None,kind="rect",color="#ff0000",width=100,height=100,
           pivot_x=0,pivot_y=0,visible=True,keys=[dict(frame=0,x=100,y=100,rotation=0,sx=1,sy=1,opacity=1)])
    p.update(kw);return p

def scene(parts=None):
    return dict(version=1,frames=30,background="#101820",reference=None,parts=parts or [part()])

class AnimationTests(unittest.TestCase):
    def test_bend_bounds_allocation_and_preserves_low_resolution_root(self):
        from animation_surface import bend_texture
        from PIL import Image
        im,pad=bend_texture(Image.new("RGBA",(2048,2048),"red"),
                           dict(width=1,height=2048,bend_start=.5),160)
        self.assertLessEqual(im.width,2048)
        im,pad=bend_texture(Image.new("RGBA",(100,1),"red"),
                           dict(width=100,height=500,bend_start=.5),80)
        self.assertEqual(im.getchannel("A").crop((0,0,im.width,1)).getbbox(),(round(pad),0,round(pad)+100,1))
        self.assertEqual(im.getpixel((round(pad)+10,10))[3],255)
    def test_authored_bend_keeps_root_and_moves_hem_without_clipping(self):
        s=validate_scene(scene([part(bend_start=.5,keys=[
            dict(frame=0,x=100,y=100,bend=0),
            dict(frame=10,x=100,y=100,bend=80)])]),{})
        self.assertEqual(sample_pose(s["parts"][0],5)["bend"],40)
        im=draw_frame(s,{},Path("."),10,1080,1920,{})
        self.assertEqual(im.getpixel((130,110)),(255,0,0))
        self.assertEqual(im.getpixel((250,195)),(255,0,0))
        self.assertEqual(im.getpixel((110,195)),(16,24,32))
    def test_bend_validation_and_old_scene_default(self):
        self.assertEqual(validate_scene(scene(),{})["parts"][0]["keys"][0]["bend"],0)
        for p in [part(bend_start=1),part(keys=[dict(frame=0,bend=float("nan"))]),
                  part(keys=[dict(frame=0,bend=201)])]:
            with self.assertRaises(ValueError):validate_scene(scene([p]),{})
    def test_validation_rejects_cycles_unknown_media_and_nan(self):
        validate_scene(scene(),{})
        bads=[scene([part(parent="a")]),scene([part(parent="missing")]),
              scene([part(),part()]),scene([part(kind="sprite",asset="unknown")]),
              scene([part(keys=[dict(frame=0,x=float("nan"))])]),
              scene([part(keys=[dict(frame=30)])])]
        for bad in bads:
            with self.assertRaises(ValueError):validate_scene(bad,{})
    def test_interpolation_parent_pivot_and_step_swap(self):
        p=part(keys=[dict(frame=0,x=100,rotation=0,asset="one"),
                     dict(frame=10,x=200,rotation=90,asset="two")])
        pose=sample_pose(p,5)
        self.assertEqual(pose["x"],150)
        self.assertEqual(pose["rotation"],45)
        self.assertEqual(pose["asset"],"one")
        self.assertEqual(sample_pose(p,10)["asset"],"two")
        p["keys"][0]["ease"]="hold"
        self.assertEqual(sample_pose(p,9)["x"],100)
        s=validate_scene(scene([part("root",width=0,height=0,kind="group",
            keys=[dict(frame=0,x=100,y=100,rotation=90)]),
            part("child",parent="root",keys=[dict(frame=0,x=100,y=0)])]),{})
        m=world_matrices(s,0)["child"]["matrix"]
        self.assertAlmostEqual(m[4],100)
        self.assertAlmostEqual(m[5],200)
    def test_raster_and_hidden_parent(self):
        draw_frame(validate_scene(scene([part(kind="ring",width=1,height=1)]),{}),{},Path("."),0,360,640,{})
        s=validate_scene(scene(),{})
        im=draw_frame(s,{},Path("."),0,1080,1920,{})
        self.assertEqual(im.getpixel((150,150))[:3],(255,0,0))
        s["parts"][0]["visible"]=False
        self.assertEqual(draw_frame(s,{},Path("."),0,1080,1920,{}).getpixel((150,150))[:3],(16,24,32))
    def test_demo_has_real_joint_changes(self):
        s=validate_scene(demo_scene(),{})
        a=world_matrices(s,0);b=world_matrices(s,96)
        self.assertNotEqual(a["forearm"]["matrix"],b["forearm"]["matrix"])
        self.assertGreater(len(s["parts"]),5)
    def test_save_export_and_apply_keeps_audio_and_rejects_stale(self):
        with tempfile.TemporaryDirectory() as d:
            repo=Repository(Path(d));p=new_project("animation");repo.save(p)
            cuts=CutsceneService(repo);svc=AnimationService(cuts);pid=p["id"]
            wav=io.BytesIO()
            with wave.open(wav,"wb") as w:
                w.setnchannels(1);w.setsampwidth(2);w.setframerate(48000);w.writeframes(b"\0\0"*96000)
            audio=cuts.import_media(pid,io.BytesIO(wav.getvalue()),len(wav.getvalue()),"music.wav")
            timeline=cuts.view(pid)["timeline"]
            timeline["clips"]=[dict(asset=audio["id"],track="BGM",start=0,length=60,gain=.4)]
            cuts.save(pid,timeline,0)
            saved=svc.save(pid,scene(),0)
            with self.assertRaises(ValueError):svc.save(pid,scene(),0)
            self.assertEqual(AnimationService(cuts).view(pid)["revision"],1)
            svc.render(pid,1,True)
            deadline=time.time()+30
            while time.time()<deadline:
                job=svc.view(pid)["job"]
                if job["status"] not in ("queued","running"):break
                time.sleep(.1)
            self.assertEqual(job["status"],"complete",job)
            self.assertEqual(job["media"]["width"],360)
            # Source is a registered video; preview cannot masquerade as final export.
            with self.assertRaises(ValueError):svc.apply(pid,1,0)
            svc.render(pid,1,False)
            deadline=time.time()+30
            while time.time()<deadline:
                job=svc.view(pid)["job"]
                if job["status"] not in ("queued","running"):break
                time.sleep(.1)
            self.assertEqual(job["status"],"complete",job)
            result=svc.apply(pid,1,1)
            self.assertEqual(result["timeline"]["frames"],30)
            self.assertEqual(result["timeline"]["clips"][0]["asset"],job["media"]["id"])
            music=result["timeline"]["clips"][1]
            self.assertEqual((music["asset"],music["track"],music["length"],music["gain"]),(audio["id"],"BGM",30,.4))
            with self.assertRaises(ValueError):svc.apply(pid,1,0)
            svc.save(pid,scene(),1)
            with self.assertRaises(ValueError):svc.apply(pid,2,1)
            svc.close()

if __name__=="__main__":unittest.main()
