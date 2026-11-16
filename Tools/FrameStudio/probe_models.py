"""Explicit user-requested text-only subscription model matrix, no image calls."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
import json, subprocess
from codex_provider import CodexProvider, subscription_env

def main():
 provider=CodexProvider()
 if not provider.status()['ready']:raise SystemExit('ChatGPT login required; no API fallback')
 root=Path(__file__).resolve().parent/'data'/('model-probe-'+datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ'))
 root.mkdir(parents=True)
 models=['gpt-6.1-sol','gpt-6-sol','gpt-6-astra','gpt-6-luna','gpt-5.6-sol','gpt-5.6-terra','gpt-5.6-luna']
 def check(model):
  work=root/model;work.mkdir()
  cmd=[provider.executable,'exec','--model',model,'-c','forced_login_method="chatgpt"','-c','model_provider="openai"','--sandbox','read-only','--skip-git-repo-check','--ephemeral','--json','-C',str(work),'-o',str(work/'last.txt'),'-']
  result={'model':model,'status':'failed','image_tested':False}
  with (work/'events.jsonl').open('w',encoding='utf-8') as log:
   p=subprocess.Popen(cmd,stdin=subprocess.PIPE,stdout=log,stderr=subprocess.STDOUT,encoding='utf-8',text=True,env=subscription_env(),creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
   try:
    p.communicate('Reply with exactly MODEL_OK. Do not call any tools, inspect files, generate images, or change anything.',timeout=180)
    result['exit_code']=p.returncode
   except subprocess.TimeoutExpired:
    p.terminate();p.communicate(timeout=20);result['status']='timeout'
  raw=(work/'events.jsonl').read_text(encoding='utf-8',errors='replace')
  errors=[]
  for line in raw.splitlines():
   try:
    event=json.loads(line)
    if event.get('type') in ('error','turn.failed'):errors.append(event)
   except json.JSONDecodeError:pass
  reply=(work/'last.txt').read_text(encoding='utf-8').strip() if (work/'last.txt').exists() else ''
  result.update(reply=reply,errors=errors)
  if result.get('exit_code')==0 and reply=='MODEL_OK':result['status']='passed'
  elif 'not supported when using Codex with a ChatGPT account' in raw:result['status']='unsupported_for_chatgpt'
  (work/'result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
  return result
 results=[]
 with ThreadPoolExecutor(max_workers=2) as pool:
  for future in as_completed([pool.submit(check,m) for m in models]):
   result=future.result();results.append(result)
   print(json.dumps(result,ensure_ascii=False),flush=True)
 (root/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
 print('RESULTS '+str(root/'results.json'),flush=True)

if __name__=='__main__':main()
