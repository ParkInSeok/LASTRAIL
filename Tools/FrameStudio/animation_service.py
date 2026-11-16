"""Authoritative scene persistence and explicit handoff into existing cutscene edits."""
import copy,json,threading
from animation_model import validate_scene,demo_scene
from animation_render import AnimationJob

class AnimationService:
    def __init__(self,cutscene):
        self.cutscene=cutscene;self.lock=threading.RLock();self.jobs={}
    def folder(self,pid):
        folder=self.cutscene.folder(pid)/"animation";folder.mkdir(exist_ok=True);return folder
    def _read(self,pid):
        path=self.folder(pid)/"scene.json"
        if path.exists():return json.loads(path.read_text("utf-8"))
        return dict(revision=0,scene=validate_scene(demo_scene(),{}),history=[])
    def job_view(self,pid,revision):
        if pid in self.jobs:return self.jobs[pid].view(revision)
        path=self.folder(pid)/"latest.json"
        if not path.exists():return None
        job=json.loads(path.read_text("utf-8"));job.pop("scene",None)
        job["stale"]=revision!=job["revision"]
        if job["status"] in ("running","queued"):job.update(status="interrupted",media=None)
        return job
    def view(self,pid):
        with self.lock:
            state=self._read(pid);cut=self.cutscene.view(pid)
            state.update(media=cut["media"],cutsceneRevision=cut["revision"],
                         job=self.job_view(pid,state["revision"]))
            return state
    def save(self,pid,scene,expected):
        with self.lock:
            state=self._read(pid)
            if type(expected)!=int or state["revision"]!=expected:raise ValueError("다른 동작 편집이 저장됐습니다. 다시 여세요.")
            clean=validate_scene(scene,self.cutscene.view(pid)["media"])
            state["history"]=(state.get("history",[])+[dict(revision=state["revision"],scene=state["scene"])])[-10:]
            state["revision"]+=1;state["scene"]=clean
            path=self.folder(pid)/"scene.json";temp=path.with_suffix(".tmp")
            temp.write_text(json.dumps(state,ensure_ascii=False),encoding="utf-8");temp.replace(path)
            return self.view(pid)
    def render(self,pid,expected,preview):
        with self.lock:
            state=self._read(pid)
            if type(expected)!=int or expected!=state["revision"]:raise ValueError("동작 저장 버전이 다릅니다.")
            current=self.job_view(pid,expected)
            if current and current["status"] in ("running","queued"):raise ValueError("출력이 진행 중입니다.")
            if pid in self.jobs and self.jobs[pid].thread.is_alive():raise ValueError("출력 기록을 마무리 중입니다. 잠시 후 다시 시도하세요.")
            if not state["scene"]["parts"]:raise ValueError("파츠를 먼저 추가하세요.")
            media=self.cutscene.view(pid)["media"]
            def register(path):
                with path.open("rb") as stream:
                    return self.cutscene.import_media(pid,stream,path.stat().st_size,"local-animation.mp4")
            job=AnimationJob(self.folder(pid),self.cutscene.folder(pid),copy.deepcopy(state),
                copy.deepcopy(media),preview,register)
            self.jobs[pid]=job;job.start();return job.view(expected)
    def cancel(self,pid):
        with self.lock:
            if pid in self.jobs:self.jobs[pid].cancel()
            return self.view(pid)
    def apply(self,pid,expected,cut_revision):
        with self.lock,self.cutscene.lock:
            state=self._read(pid);job=self.job_view(pid,state["revision"])
            if expected!=state["revision"] or not job or job["status"]!="complete" or job["stale"] or job["preview"]:
                raise ValueError("현재 저장 버전의1080 출력이 먼저 필요합니다.")
            cut=self.cutscene.view(pid)
            if type(cut_revision)!=int or cut_revision!=cut["revision"]:raise ValueError("컷씬 편집이 바뀌었습니다. 다시 확인하세요.")
            frames=state["scene"]["frames"];timeline=copy.deepcopy(cut["timeline"])
            audio=[]
            for clip in timeline["clips"]:
                if clip["track"]=="visual" or clip["start"]>=frames:continue
                clip["length"]=min(clip["length"],frames-clip["start"])
                for key in ("fade_in","fade_out"):clip[key]=min(clip[key],clip["length"]//2)
                audio.append(clip)
            timeline.update(frames=frames,label="동작 영상 후보",
                clips=[dict(asset=job["media"]["id"],track="visual",start=0,length=frames)]+audio)
            return self.cutscene.save(pid,timeline,cut_revision)
    def close(self):
        with self.lock:
            for job in self.jobs.values():job.cancel()
