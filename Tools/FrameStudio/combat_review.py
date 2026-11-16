"""Studio playback adapter; existing review pages retain animation/FX ownership."""
import json
import mimetypes
from pathlib import Path
from urllib.parse import urlparse, parse_qs, unquote

ROOT = Path(__file__).resolve().parents[2] / 'QA/SideViewReuse-20261023/reina-battle-hud-v01'
REVIEWS = [
 dict(character='Reina',label='레이나 · 기본/액티브/궁극기',path='d-console/index.html',projects=['06a7ff72e7b5487c80c3cf658ae7da88']),
 dict(character='Eve',label='이브 · 드론/개틀링/컷인',path='eve-gatling/index.html',projects=['6b1ec2fa786f40aaaed8016a924f7dfd','8ac24e3901ec4181892b6fa478f714ce']),
 dict(character='Sera',label='세라 · 액티브 치유',path='sera-heal/index.html',projects=['1352b233cbcc4c5899751c0ca795d601']),
 dict(character='Sera',label='세라 · 궁극기 치유/컷인',path='sera-ultimate/index.html',projects=['a57f2090076a4f7fb298f440367befd4']),
 dict(character='Nyx',label='닉스 · 억제영역',path='nyx-zone/index.html',projects=['c6735af62882470e9e910c023814ea85']),
 dict(character='Nyx',label='닉스 · 봉쇄영역/파쇄/컷인',path='nyx-zone/index.html?skill=ultimate',projects=['02dc26dbc1bb447d9e69dea6f2dd3958']),
 dict(character='Kyle',label='카일 · 우측 궁극기/보호/컷인',path='kyle-guard/studio-right-ultimate-20261107/battle.html',projects=['4948e5afdc964a3697b37c25e0ccccb4','d7a8d6c11c1c4594ac934f180a5f01a4','ed96bff303a3417885ebfce251063a7a']),
 dict(character='Kyle',label='카일 · 좌측 궁극기/보호/컷인',path='kyle-guard/studio-left-ultimate-20261107/battle.html',projects=['63da550f13b44fdfa49cc2c7e0e3249c']),
]

def resolve_media(relative):
 relative = unquote(relative)
 if Path(relative).is_absolute() or '..' in Path(relative).parts:
  raise ValueError('허용되지 않은 전투 자료 경로')
 path = (ROOT / relative).resolve()
 if not path.is_relative_to(ROOT.resolve()) or path.suffix.lower() not in {'.html','.js','.css','.json','.png','.webp','.svg','.gif','.jpg','.woff2'}:
  raise ValueError('허용되지 않은 전투 자료 형식')
 return path

def page(project):
 selected = next((i for i,r in enumerate(REVIEWS) if project in r['projects']),0)
 data = json.dumps(REVIEWS,ensure_ascii=False).replace('<','\\u003c')
 return '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>제작도구 · 전투 연출 재생</title><style>body{margin:0;background:#1c2028;color:#eee;font:15px system-ui}header{display:flex;gap:12px;align-items:center;padding:12px;flex-wrap:wrap}a{color:#aee1ff}select,button{padding:8px;background:#303948;color:white;border:1px solid #667}p{margin:0 12px 12px;font-size:12px;color:#bdc7d1}iframe{width:100%;height:calc(100vh - 115px);border:0;background:#22252d}</style>
<header><a id="back">원화 작업으로</a><strong>전투 연출 재생</strong><select id="reviews" aria-label="요원과 연출 선택"></select><button id="restart">처음부터 재생</button><button id="stop">재생 닫기</button></header>
<p>승인 시연의 모션·컷인·독립 FX 코드 함께 재생 · 원화 편집과 별도 · Unity 판정 미연결</p><div id="player"></div>
<script>const rows='''+data+''',select=document.querySelector('#reviews'),host=document.querySelector('#player');
select.replaceChildren(...rows.map((r,i)=>new Option(r.label,i)));select.value='''+str(selected)+''';
document.querySelector('#back').href='/?id='+encodeURIComponent(new URLSearchParams(location.search).get('id')||'');
function closePlayer(){host.replaceChildren()}
function play(){closePlayer();const row=rows[Number(select.value)],frame=document.createElement('iframe');frame.title=row.label;frame.src='/combat-review-media/'+row.path;host.append(frame)}
select.onchange=play;document.querySelector('#restart').onclick=play;document.querySelector('#stop').onclick=closePlayer;window.addEventListener('pagehide',closePlayer);play();
</script></html>'''

def get(handler):
 url=urlparse(handler.path)
 if url.path=='/combat-review':
  raw=page(parse_qs(url.query).get('id',[''])[0]).encode('utf-8');mime='text/html; charset=utf-8'
 elif url.path.startswith('/combat-review-media/'):
  path=resolve_media(url.path[len('/combat-review-media/'):]);raw=path.read_bytes();mime=mimetypes.guess_type(path)[0] or 'application/octet-stream'
 else:return False
 handler.send_response(200);handler.send_header('Content-Type',mime);handler.send_header('Content-Length',str(len(raw)));handler.send_header('X-Content-Type-Options','nosniff');handler.end_headers();handler.wfile.write(raw);return True
