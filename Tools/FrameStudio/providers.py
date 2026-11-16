"""One owner for engine selection and the lifetime of a render reservation."""
import json,threading

class ProviderRouter:
 def __init__(self,path,providers):
  self.path=path;self.providers=providers;self.lock=threading.RLock();self.active=None
  self.selected=json.loads(path.read_text(encoding='utf-8'))['selected'] if path.exists() else 'local'
  if self.selected not in providers:raise ValueError('저장된 생성 엔진을 찾을 수 없습니다.')
 def select(self,name):
  with self.lock:
   if self.active is not None:raise ValueError('생성 종료 후 엔진을 변경하세요.')
   if name not in self.providers:raise ValueError('지원하지 않는 생성 엔진')
   self.path.parent.mkdir(parents=True,exist_ok=True)
   tmp=self.path.with_suffix('.tmp');tmp.write_text(json.dumps({'selected':name}),encoding='utf-8');tmp.replace(self.path)
   self.selected=name
  return self.info()
 def info(self):return {'selected':self.selected,'busy':self.active is not None,'choices':['local','codex']}
 def status(self):
  result=self.providers[self.selected].status();return dict(result,**self.info())
 def reserve(self):
  with self.lock:
   if self.active is not None:raise ValueError('이미 생성 중입니다.')
   provider=self.providers[self.selected]
   if hasattr(provider,'prepare'):provider.prepare()
   self.active=self.selected
   return self.active
 def release(self):
  with self.lock:self.active=None
 def generate(self,*args):
  with self.lock:
   if self.active is None:raise ValueError('생성 예약이 없습니다.')
   provider=self.providers[self.active]
  return provider.generate(*args)
 def cancel(self):
  with self.lock:
   if self.active is not None:self.providers[self.active].cancel()
