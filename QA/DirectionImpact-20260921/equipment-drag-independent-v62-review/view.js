(()=>{'use strict';
const F=DRAG_FIXTURE;const rows=new Map(F.items.map(i=>[i.id,Object.freeze({...i})]));
const glyphs={'철벽':'<path d="M12 3 5 6v6c0 4 3 7 7 9 4-2 7-5 7-9V6Z"/>','화력':'<path d="m12 3-4 6 2 3-3 4 5 5 5-5-3-4 2-3Z"/>','전술 제어':'<path d="M6 3h12M6 21h12M7 4v3l5 5-5 5v3M17 4v3l-5 5 5 5v3"/>'};
const badge=t=>'<span class="set-badge" aria-label="'+t+' 세트"><svg viewBox="0 0 24 24">'+glyphs[t]+'</svg></span>';
const card=(i,ghost=false)=>'<button class="gear-card" data-item="'+i.id+'" aria-label="'+i.name+' '+i.rank+'성'+(i.locked?' 잠금':'')+'" '+(ghost?'tabindex="-1"':'')+'>'+badge(i.tag)+'<span class="gear-art gear-'+i.kind+'"></span><span class="stars">'+('★'.repeat(i.rank))+'</span><strong>'+i.name+'</strong>'+(i.locked?'<svg class="padlock" viewBox="0 0 24 24" aria-label="잠금"><path d="M7 10V7a5 5 0 0 1 10 0v3"/><rect x="5" y="10" width="14" height="11" rx="2"/></svg>':'')+(ghost?'<span class="reason">드래그 중</span>':'')+'</button>';
class DragPreviewModel{
 #source=null;#target=null;
 get source(){return this.#source}get target(){return this.#target}
 begin(id){const i=rows.get(id);if(!i||i.locked)return false;this.#source=i;return true}
 reason(id){const t=rows.get(id),s=this.#source;if(!t||!s||t.id===s.id)return {eligible:false,label:''};if(t.locked)return {eligible:false,label:'잠금'};if(t.rank===5)return {eligible:false,label:'최고 성급'};if(t.rank!==s.rank)return {eligible:false,label:'성급 불일치'};if(t.category!==s.category)return {eligible:false,label:'종류 불일치'};return {eligible:true,label:'합성 '+t.rank+'성 → '+(t.rank+1)+'성'}}
 hover(id){this.#target=id&&rows.has(id)?rows.get(id):null}
 finish(){const s=this.#source,t=this.#target,ok=t&&this.reason(t.id).eligible;this.cancel();return ok?{source:s,target:t}:null}
 cancel(){this.#source=null;this.#target=null}
}
class DragView{
 constructor(){this.root=document.querySelector('#screen');this.ghost=null}
 render(){this.root.innerHTML='<section class="art-panel drag-panel"><header><h1>작전 장비</h1></header><h2>보관함 <span>8 / 30</span></h2><div class="inventory-scroll"><div class="gear-grid drag-grid">'+F.items.map(i=>card(i)).join('')+Array.from({length:22},()=>'<div class="gear-card empty-cell"><svg viewBox="0 0 48 48"><path d="M12 5h24l7 8v22l-7 8H12l-7-8V13Z"/><path d="M17 24h14M24 17v14"/></svg></div>').join('')+'</div></div></section>'}
 feedback(m,x,y){if(!this.ghost){this.ghost=document.createElement('div');this.ghost.className='drag-ghost';this.ghost.innerHTML=card(m.source,true);document.body.append(this.ghost)}
 const size=innerWidth<=430?98:108;this.ghost.style.left=Math.max(4,Math.min(innerWidth-size-4,x+16))+'px';this.ghost.style.top=Math.max(4,Math.min(innerHeight-160,y-165))+'px';
 for(const b of this.root.querySelectorAll('[data-item]')){const r=m.reason(b.dataset.item);b.classList.toggle('moving',b.dataset.item===m.source.id);b.classList.toggle('target-yes',r.eligible);b.classList.toggle('target-no',!!r.label&&!r.eligible);b.classList.toggle('target-current',b.dataset.item===m.target?.id);let label=b.querySelector('.reason');if(r.label){if(!label){label=document.createElement('span');label.className='reason';b.append(label)}label.textContent=r.label}else label?.remove()}
 }
 clear(){this.ghost?.remove();this.ghost=null;for(const b of this.root.querySelectorAll('[data-item]')){b.classList.remove('moving','target-yes','target-no','target-current');b.querySelector('.reason')?.remove()}}
 preview(p){this.clear();const veil=document.createElement('div');veil.className='preview-veil';veil.innerHTML='<section class="preview-body" role="dialog" aria-modal="true" aria-label="합성 미리보기"><h1>합성</h1>'+card({...p.target,id:'preview-main',rank:3},true).replace('<span class="reason">드래그 중</span>','')+'<p>'+p.target.tag+'</p><p>방어력 25% → 60%</p><div class="preview-actions"><button class="action secondary" data-preview="cancel">취소</button><button class="action" data-preview="confirm">합성</button></div></section>';document.body.append(veil)}
 closePreview(){document.querySelector('.preview-veil')?.remove()}
}
class DragController{
 constructor(m,v){this.m=m;this.v=v;this.pending=null;this.dropPreviews=0;this.alive=true;this.v.render();this.down=e=>{if(e.pointerType==='touch'||e.button!==0)return;const b=e.target.closest('[data-item]');if(!b||!rows.has(b.dataset.item))return;this.pending={id:b.dataset.item,x:e.clientX,y:e.clientY};};this.move=e=>{if(!this.pending)return;if(!this.m.source){if(Math.hypot(e.clientX-this.pending.x,e.clientY-this.pending.y)<8)return;if(!this.m.begin(this.pending.id)){this.pending=null;return}}e.preventDefault();const b=document.elementFromPoint(e.clientX,e.clientY)?.closest('[data-item]');this.m.hover(b?.dataset.item);this.v.feedback(this.m,e.clientX,e.clientY)};this.up=()=>{if(!this.pending)return;this.pending=null;const result=this.m.finish();this.v.clear();if(result){this.dropPreviews++;this.v.preview(result)}};this.cancel=()=>{this.pending=null;this.m.cancel();this.v.clear()};this.key=e=>{if(e.key==='Escape'){this.cancel();this.v.closePreview()}};this.preview=e=>{const b=e.target.closest('[data-preview]');if(!b)return;this.v.closePreview();if(b.dataset.preview==='confirm')document.querySelector('#review-message').textContent='합성 실행은 기존 게임 명령 연결 대상입니다. 이 검토본은 장비를 소모하거나 결과를 추첨하지 않습니다.'};document.addEventListener('pointerdown',this.down);document.addEventListener('pointermove',this.move,{passive:false});document.addEventListener('pointerup',this.up);document.addEventListener('pointercancel',this.cancel);document.addEventListener('keydown',this.key);document.addEventListener('click',this.preview)}
 showDemo(targetId){this.cancel();this.m.begin('fixture-source');this.m.hover(targetId);const b=this.v.root.querySelector('[data-item="'+targetId+'"]');const r=b.getBoundingClientRect();this.v.feedback(this.m,r.x+r.width/2+40,r.y+r.height/2)}
 dispose(){this.cancel();this.v.closePreview();document.removeEventListener('pointerdown',this.down);document.removeEventListener('pointermove',this.move);document.removeEventListener('pointerup',this.up);document.removeEventListener('pointercancel',this.cancel);document.removeEventListener('keydown',this.key);document.removeEventListener('click',this.preview);this.alive=false}
}
const m=new DragPreviewModel(),v=new DragView(),c=new DragController(m,v);
const mode=document.body.dataset.state;if(mode==='valid')c.showDemo('fixture-target');if(mode==='invalid')c.showDemo('locked');
document.querySelector('#reset').addEventListener('click',()=>{c.cancel();v.closePreview()});
window.addEventListener('pagehide',()=>c.dispose(),{once:true});
window.DRAG_REVIEW={snapshot:()=>({used:F.used,source:m.source?.id||null,target:m.target?.id||null,previews:c.dropPreviews,alive:c.alive}),dispose:()=>c.dispose()};
})();
