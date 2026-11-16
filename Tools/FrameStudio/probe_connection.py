"""One explicitly requested subscription-only image test; no retry/API fallback."""
from pathlib import Path
import json
from datetime import datetime, timezone
from codex_provider import CodexProvider
root=Path(__file__).resolve().parent
ref=root.parents[1]/'QA/DirectionImpact-20260921/eve-combat-v01/eve-idle-breath-v13-01.png'
job={'prompt':'Create ONE front-facing full-body character registration reference of the SAME adult female engineer Eve in the attached approved source. Neutral stable standing pose, moderate feet spacing, same smoky brown short bob, raised bronze goggles, slate blue short work jacket, pale inner top, grey trousers, brown work boots, low backpack and hip hard case. Anatomical right hand holds the same short thick gunmetal pulse tool at waist, left supports its housing. Preserve identity, proportions, material palette, tool design. Front view not rear, no action, no extra equipment, all body inside generous transparent padding. Genuine transparent PNG. This is an unapproved multi-view registration candidate, not a production animation frame. Use only built-in imagegen once.'}
provider=CodexProvider()
print(json.dumps(provider.status(),ensure_ascii=False),flush=True)
try:
 result=provider.generate(job,[ref],root/'data'/('connection-probe-'+datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')));print(str(result),flush=True)
except Exception as e:print(type(e).__name__+': '+str(e),flush=True);raise SystemExit(1)
