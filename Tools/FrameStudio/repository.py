"""Local persistence; immutable image versions and atomic project JSON."""
import json, uuid, re, threading
from pathlib import Path

class Repository:
 def __init__(self,root):
  self.root=root.resolve();self.root.mkdir(parents=True,exist_ok=True);self.lock=threading.RLock()
  (self.root/'assets').mkdir(exist_ok=True);(self.root/'projects').mkdir(exist_ok=True)
 def asset_path(self,name):
  if not isinstance(name,str) or not re.fullmatch(r'[a-zA-Z0-9_-]+\.(png|gif|json|zip)',name):raise ValueError('잘못된 자산 경로')
  return self.root/'assets'/name
 def put_asset(self,content,suffix='png'):
  name=uuid.uuid4().hex+'.'+suffix;self.asset_path(name).write_bytes(content);return name
 def project_path(self,pid):
  if not re.fullmatch('[0-9a-f]{32}',pid):raise ValueError('잘못된 프로젝트 ID')
  return self.root/'projects'/(pid+'.json')
 def get(self,pid):return json.loads(self.project_path(pid).read_text(encoding='utf-8'))
 def save(self,p):
  target=self.project_path(p['id']);tmp=target.with_suffix('.tmp');tmp.write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf-8');tmp.replace(target)
 def list(self):
  return [{'id':p['id'],'name':p['name']} for path in sorted((self.root/'projects').glob('*.json')) for p in [json.loads(path.read_text(encoding='utf-8'))]]
