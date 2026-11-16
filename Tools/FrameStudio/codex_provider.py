"""Subscription-only official Codex CLI bridge. Never reads/copies OAuth secrets."""
import os, shutil, subprocess, json, threading, time
from pathlib import Path

def subscription_env():
 env=dict(os.environ)
 for key in list(env):
  if key.upper() in ('OPENAI_API_KEY','CODEX_API_KEY','OPENAI_BASE_URL','OPENAI_ORG_ID','OPENAI_PROJECT_ID'):env.pop(key,None)
 tmp=Path(__file__).resolve().parents[2]/'LocalTemp';tmp.mkdir(exist_ok=True)
 env.update(TEMP=str(tmp),TMP=str(tmp),TMPDIR=str(tmp))
 return env

class CodexProvider:
 def __init__(self):
  self.executable=shutil.which('codex');self.process=None;self.lock=threading.Lock();self.cancelled=False
  self.model=json.loads(Path(__file__).with_name('provider.json').read_text(encoding='utf-8'))['model']
 def status(self):
  if not self.executable:return {'ready':False,'message':'Codex 실행 파일을 찾지 못했습니다.'}
  r=subprocess.run([self.executable,'login','status'],capture_output=True,text=True,encoding='utf-8',errors='replace',env=subscription_env(),timeout=20,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
  ok=r.returncode==0 and 'Logged in using ChatGPT' in r.stdout+r.stderr
  return {'ready':ok,'message':'ChatGPT 로그인 확인 · 이미지 생성 연결은 별도 검증 필요' if ok else 'ChatGPT 구독 로그인이 필요합니다. API 키 로그인은 사용하지 않습니다.','model':self.model,'billing':'ChatGPT subscription allowance / eligible credits; never API fallback'}
 def cancel(self):
  with self.lock:
   self.cancelled=True
   if self.process and self.process.poll() is None:self.process.terminate()
 def generate(self,request,refs,work):
  if not self.status()['ready']:raise ValueError('구독 로그인이 아니므로 생성을 중단했습니다.')
  work.mkdir(parents=True,exist_ok=True);output=work/'result.png';last=work/'last.txt'
  (work/'request.json').write_text(json.dumps(request,ensure_ascii=False,indent=2),encoding='utf-8')
  prompt=('Use the built-in imagegen tool exactly once for the frame request below. Do not use an image API, API key, external provider, Python drawing substitute, or rig/warp. '
          'If unavailable, stop and explain. Save/copy the actual generated PNG unchanged to '+str(output)+'. Preserve the generator original. '
          'Do not make additional generations, install packages, change settings, or modify unrelated files. '
          'This is an execution of an already approved frame-render job, not a request to design software. Reference files in attachment order: '+json.dumps([str(p) for p in refs])+'. Role mapping: '+json.dumps(request.get('reference_roles',{}))+'\n'+request['prompt'])
  cmd=[self.executable,'exec','--model',self.model,'-c','forced_login_method="chatgpt"','-c','model_provider="openai"','--enable','image_generation','--sandbox','workspace-write','--skip-git-repo-check','--ephemeral','--json','-C',str(work),'-o',str(last)]
  for ref in refs:cmd+=['-i',str(ref)]
  cmd+=['-']
  with self.lock:
   self.cancelled=False
   with (work/'events.jsonl').open('w',encoding='utf-8') as log:
    self.process=subprocess.Popen(cmd,stdin=subprocess.PIPE,stdout=log,stderr=subprocess.STDOUT,text=True,encoding='utf-8',env=subscription_env(),creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    self.process.stdin.write(prompt);self.process.stdin.close()
  try:
   rc=self.process.wait(timeout=900)
   if self.cancelled:raise ValueError('사용자가 생성을 중단했습니다. 사용된 구독량은 되돌려지지 않습니다.')
   if rc!=0:
    log_text=(work/'events.jsonl').read_text(encoding='utf-8',errors='replace')
    if 'not supported when using Codex with a ChatGPT account' in log_text:raise ValueError(self.model+' 모델이 구독 CLI 실행에서 거절됐습니다. 연결 미완료이며 API 재시도는 하지 않았습니다.')
    raise ValueError('Codex 실행이 실패했습니다. 요청 폴더의 events.jsonl을 확인하세요. API 재시도는 하지 않았습니다.')
   if not output.exists():raise ValueError('Codex 결과 PNG가 없습니다. 요청 폴더의 last.txt에서 제한 또는 오류를 확인하세요.')
   return output
  except subprocess.TimeoutExpired:
   self.cancel();self.process.wait(timeout=15);raise ValueError('15분 제한으로 중단했습니다. 자동 재시도하지 않습니다.')
  finally:
   with self.lock:self.process=None
