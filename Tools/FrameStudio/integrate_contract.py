from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
section=json.loads(Path(__file__).with_name('contract-text.json').read_text(encoding='utf-8'))
for name in ('QA/DirectionImpact-20260921/eve-combat-v01/eve-animation-production-prompt-v01.md','QA/DirectionImpact-20260921/eve-combat-v01/kyle-animation-production-prompt-v01.md','docs/superpowers/plans_categories/19_SD_COMBAT_MODEL_TRANSLATION.md'):
 p=root/name
 if '## 프레임 작화 도구 입력 계약 — 2026-10-12' not in p.read_text(encoding='utf-8'):
  with p.open('a',encoding='utf-8') as f:f.write(section)
