"""Single owner for edit revisions, media registry and project-local render jobs."""
import copy, json, re, threading
from pathlib import Path
from cutscene_model import default_timeline,validate_timeline
from cutscene_media import import_file

class CutsceneService:
 def __init__(self,repo):
  self.repo=repo;self.lock=threading.RLock();self.jobs={}
 def folder(self,pid):
  self.repo.get(pid)  # Existing project identity is mandatory.
  path=self.repo.root/"cutscenes"/pid;path.mkdir(parents=True,exist_ok=True)
  return path
 def _read(self,pid):
  path=self.folder(pid)/"edit.json"
  return json.loads(path.read_text("utf-8")) if path.exists() else dict(revision=0,timeline=default_timeline(),media={})
 def _write(self,pid,state):
  target=self.folder(pid)/"edit.json";tmp=target.with_suffix(".tmp")
  tmp.write_text(json.dumps(state,ensure_ascii=False,indent=2),encoding="utf-8");tmp.replace(target)
 def view(self,pid):
  with self.lock:
   s=self._read(pid)
   s["job"]=self.jobs[pid].view(s["revision"]) if pid in self.jobs else self.saved_job(pid,s["revision"])
   return s
 def saved_job(self,pid,revision):
  path=self.folder(pid)/"latest-render.json"
  if not path.exists():return None
  job=json.loads(path.read_text("utf-8"))
  job.setdefault("label",job.get("timeline",{}).get("label","이전 출력"))
  job.pop("timeline",None)
  job["stale"]=job["revision"]!=revision
  if job["status"] in ("queued","running"):job.update(status="interrupted",file=None)
  return job
 def import_media(self,pid,stream,length,name):
  metadata=import_file(self.folder(pid),stream,length,name)
  with self.lock:
   s=self._read(pid);s["media"][metadata["id"]]=metadata;self._write(pid,s)
  return metadata
 def save(self,pid,data,expected):
  with self.lock:
   s=self._read(pid)
   if type(expected)!=int or expected!=s["revision"]:raise ValueError("다른 편집이 저장됐습니다. 다시 열어 확인하세요.")
   t=validate_timeline(data,s["media"])
   # Preserve small edit history; media bytes remain immutable.
   history=s.setdefault("history",[]);history.append(dict(revision=s["revision"],timeline=s["timeline"]))
   s["history"]=history[-20:];s["timeline"]=t;s["revision"]+=1;self._write(pid,s)
   return self.view(pid)
 def media_path(self,pid,name):
  if not isinstance(name,str) or not re.fullmatch(r"[0-9a-f]{32}\.(png|jpg|jpeg|mp4|mov|webm|wav|mp3|ogg|m4a)",name):
   raise ValueError("잘못된 미디어 경로")
  folder=self.folder(pid)
  with self.lock:
   s=self._read(pid)
   registered=any(m["file"]==name for m in s["media"].values())
   job=self.jobs.get(pid)
   record=folder/(Path(name).stem+".render.json")
   rendered=record.exists() and json.loads(record.read_text("utf-8")).get("status")=="complete"
   if not registered and not rendered:raise ValueError("등록되지 않았거나 완료되지 않은 미디어")
  return folder/name
 def render(self,pid,expected,preview=False):
  from cutscene_render import RenderJob
  with self.lock:
   s=self._read(pid)
   if expected!=s["revision"]:raise ValueError("저장 버전이 변경됐습니다.")
   if pid in self.jobs and self.jobs[pid].view(s["revision"])["status"] in ("queued","running"):
    raise ValueError("진행 중인 출력을 먼저 끝내거나 취소하세요.")
   if not s["timeline"]["clips"]:raise ValueError("클립을 먼저 배치하세요.")
   job=RenderJob(self.folder(pid),copy.deepcopy(s),preview)
   self.jobs[pid]=job;job.start()
   return job.view(s["revision"])
 def cancel(self,pid):
  with self.lock:
   if pid in self.jobs:self.jobs[pid].cancel()
   return self.view(pid)
 def close(self):
  with self.lock:
   for job in self.jobs.values():job.cancel()
