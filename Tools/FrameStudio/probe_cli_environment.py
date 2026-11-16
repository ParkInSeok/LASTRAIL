"""Compare normal CLI identity against inherited desktop identity, text only."""
from pathlib import Path
from datetime import datetime,timezone
import subprocess,json,tomllib,sys
from codex_provider import CodexProvider,subscription_env
from urllib.parse import urlsplit

root=Path(__file__).resolve().parent/'data'/('environment-probe-'+datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ'))
root.mkdir(parents=True)
env=subscription_env()
for file in (Path(env['CODEX_HOME'])/'config.toml',Path('D:/Project/LASTRAIL/.codex/config.toml')):
 if file.exists():
  cfg=tomllib.loads(file.read_text(encoding='utf-8-sig'))
  safe={k:cfg.get(k,'default') for k in ('model_provider','forced_login_method','cli_auth_credentials_store')}
  for key in ('chatgpt_base_url','openai_base_url'):
   v=cfg.get(key);safe[key]=urlsplit(v).hostname if v else 'default'
  print(json.dumps({'file':str(file),'routing':safe}),flush=True)
# A standalone CLI should use its own default identity, not inherit the desktop override.
# Keep authentication, sandbox and permission environment unchanged.
env.pop('CODEX_INTERNAL_ORIGINATOR_OVERRIDE',None)
provider=CodexProvider()
model=sys.argv[1] if len(sys.argv)>1 else 'gpt-6-sol'
print('model='+model,flush=True)
cmd=[provider.executable,'exec','--model',model,'-c','forced_login_method="chatgpt"','-c','model_provider="openai"','--sandbox','read-only','--skip-git-repo-check','--ephemeral','--json','-C',str(root),'-o',str(root/'last.txt'),'-']
with (root/'events.jsonl').open('w',encoding='utf-8') as log:
 p=subprocess.Popen(cmd,stdin=subprocess.PIPE,stdout=log,stderr=subprocess.STDOUT,text=True,encoding='utf-8',env=env,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
 try:p.communicate('Reply exactly MODEL_OK. Do not call tools or inspect any files.',timeout=180)
 except subprocess.TimeoutExpired:p.terminate();p.communicate(timeout=15)
print('exit_code='+str(p.returncode),flush=True)
for line in (root/'events.jsonl').read_text(encoding='utf-8',errors='replace').splitlines():
 try:
  e=json.loads(line)
  if e.get('type') in ('error','turn.failed','turn.completed'):print(json.dumps(e),flush=True)
 except json.JSONDecodeError:pass
if (root/'last.txt').exists():print((root/'last.txt').read_text(encoding='utf-8'),flush=True)
print('RESULTS '+str(root),flush=True)
