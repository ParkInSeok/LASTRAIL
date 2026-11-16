/* Clip time owns presentation; pause/seek never dispatch gameplay commands. */
class FxClock {
 constructor(duration){this.duration=duration;this.position=0;this.running=false;this.rate=1;this.origin=0;}
 read(now){return ((this.position+(this.running?(now-this.origin)*this.rate:0))%this.duration+this.duration)%this.duration;}
 pause(now){this.position=this.read(now);this.running=false;}
 play(now,rate=1){if(this.running)this.pause(now);this.origin=now;this.rate=rate;this.running=true;}
 seek(ms){this.position=((ms%this.duration)+this.duration)%this.duration;this.running=false;}
}
function selectFx(project,time){return project.fx_timeline?.stages.findLast(row=>row.start_ms<=time)||null;}
class FxTimelineView {
 constructor(root,onAction){
  this.root=root;this.onAction=onAction;this.epoch=0;this.key=null;this.raf=null;
  root.innerHTML=`<h3>독립 FX 시간표 · 정밀 재생</h3><p>배우 시간표를 유지하고 FX 시작 시각을 따로 적용합니다. 아래는 배우와 독립 FX의 전투 크기 검토입니다.</p><canvas width="480" height="270" style="max-width:100%;background:#202532"></canvas><div><button class="fx-play">재생</button><button class="fx-pause">일시정지</button><select class="fx-rate" aria-label="독립 FX 재생 속도"><option value="1">1배</option><option value="0.5">0.5배</option></select><input class="fx-seek" aria-label="독립 FX 시각" type="range" min="0" step="1"><output></output></div><details><summary>FX 시작 시각 편집</summary><div class="fx-rows"></div><button class="fx-save">시간표 저장</button></details><p class="fx-status" role="status"></p>`;
  this.q=s=>root.querySelector(s);
  this.q('.fx-play').onclick=()=>{if(!this.ready)return;this.clock.play(performance.now(),Number(this.q('.fx-rate').value));this.tick();};
  this.q('.fx-pause').onclick=()=>this.pause();
  this.q('.fx-rate').onchange=()=>{if(this.clock?.running)this.clock.play(performance.now(),Number(this.q('.fx-rate').value));};
  this.q('.fx-seek').oninput=()=>{if(!this.ready)return;this.pause();this.clock.seek(Number(this.q('.fx-seek').value));this.draw(this.clock.position);};
  this.q('.fx-save').onclick=async()=>{
   const epoch=this.epoch;this.pause();
   try{await onAction('fx_timeline',{stages:this.project.fx_timeline.stages.map((row,i)=>({...row,start_ms:Number(this.q('.fx-rows').children[i].querySelector('input').value)})),occlude_actor_alpha:this.project.fx_timeline.occlude_actor_alpha});if(epoch===this.epoch)this.q('.fx-status').textContent='시간표 저장됨';}
   catch(error){if(epoch===this.epoch)this.q('.fx-status').textContent=error.message;}
  };
  this.onPageHide=()=>this.pause();window.addEventListener('pagehide',this.onPageHide);
 }
 pause(){if(this.clock)this.clock.pause(performance.now());cancelAnimationFrame(this.raf);this.raf=null;}
 dispose(){this.pause();this.epoch++;this.images=null;this.ready=false;this.maskCanvas=null;this.maskContext=null;for(const s of ['.fx-play','.fx-pause','.fx-rate','.fx-seek','.fx-save']){const el=this.q(s);el.onclick=null;el.oninput=null;el.onchange=null;}window.removeEventListener('pagehide',this.onPageHide);}
 async setProject(p){
  const key=JSON.stringify([p.id,p.fx_timeline,p.frames.map(f=>[f.versions.at(-1)?.asset,f.duration_ms]),p.battle_settings]);
  if(key===this.key)return;this.key=key;this.pause();const epoch=++this.epoch;this.project=p;this.ready=false;this.images=null;this.root.hidden=!p.fx_timeline;
  if(!p.fx_timeline)return;
  this.clock=new FxClock(p.frames.reduce((n,f)=>n+f.duration_ms,0));this.q('.fx-seek').max=this.clock.duration-1;
  const rows=this.q('.fx-rows');rows.replaceChildren();
  for(const row of p.fx_timeline.stages){const label=document.createElement('label');label.textContent=row.label+' ';const input=document.createElement('input');input.type='number';input.step='10';input.min='0';input.max=this.clock.duration-10;input.value=row.start_ms;label.append(input);rows.append(label);}
  this.q('.fx-status').textContent='이미지 준비 중';
  try{
   const assets=new Set([...p.frames.map(f=>f.versions.at(-1).asset),...p.fx_timeline.stages.map(r=>r.asset)]);
   const bg=p.battle_settings?.background;if(bg)assets.add(bg);
   const pairs=await Promise.all([...assets].map(asset=>new Promise((resolve,reject)=>{const image=new Image();image.onload=()=>resolve([asset,image]);image.onerror=()=>reject(Error('FX 검토 이미지를 읽지 못했습니다.'));image.src='/assets/'+asset;})));
   if(epoch!==this.epoch)return;this.images=new Map(pairs);this.prepare();this.ready=true;this.q('.fx-status').textContent='준비됨 · 시각은 ms 단위';this.draw(0);
  }catch(error){if(epoch===this.epoch)this.q('.fx-status').textContent=error.message;}
 }
 prepare(){
  const p=this.project,first=this.images.get(p.frames[0].versions.at(-1).asset),scratch=document.createElement('canvas');scratch.width=first.width;scratch.height=first.height;
  const ctx=scratch.getContext('2d',{willReadFrequently:true});ctx.drawImage(first,0,0);const pixels=ctx.getImageData(0,0,first.width,first.height).data;
  let left=first.width,top=first.height,right=0,bottom=0;
  for(let y=0;y<first.height;y++)for(let x=0;x<first.width;x++)if(pixels[(y*first.width+x)*4+3]>128){left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x+1);bottom=Math.max(bottom,y+1);}
  const cfg=p.battle_settings||{},bg=this.images.get(cfg.background),scale=(cfg.height||114)/(bottom-top),anchor=cfg.anchor||[.5,.8];
  const pos=[Math.round(anchor[0]*(bg?.width||480)-(left+right)*.5*scale),Math.round(anchor[1]*(bg?.height||270)-bottom*scale)];
  this.layout={width:Math.round(first.width*scale),height:Math.round(first.height*scale),pos,crop:[Math.round(pos[0]+(left+right)*.5*scale-240),Math.round(pos[1]+bottom*scale-230)],bg};
  this.maskCanvas=scratch;this.maskContext=ctx;this.lastMaskKey=null;
 }
 draw(time){
  if(!this.ready)return;const p=this.project,l=this.layout,ctx=this.q('canvas').getContext('2d');let end=0,index=p.frames.findIndex(f=>(end+=f.duration_ms)>time);if(index<0)index=p.frames.length-1;
  const actor=this.images.get(p.frames[index].versions.at(-1).asset),row=selectFx(p,time),fx=this.images.get(row.asset);ctx.clearRect(0,0,480,270);
  if(l.bg)ctx.drawImage(l.bg,-l.crop[0],-l.crop[1]);const x=l.pos[0]-l.crop[0],y=l.pos[1]-l.crop[1];ctx.drawImage(actor,x,y,l.width,l.height);
  let overlay=fx;const threshold=p.fx_timeline.occlude_actor_alpha;
  if(threshold!=null){
   const key=index+':'+row.asset;if(key!==this.lastMaskKey){const mc=this.maskContext,w=actor.width,h=actor.height;mc.clearRect(0,0,w,h);mc.drawImage(actor,0,0);const a=mc.getImageData(0,0,w,h).data;mc.clearRect(0,0,w,h);mc.drawImage(fx,0,0);const out=mc.getImageData(0,0,w,h);for(let i=3;i<a.length;i+=4)if(a[i]>=threshold)out.data[i]=0;mc.putImageData(out,0,0);this.lastMaskKey=key;}overlay=this.maskCanvas;
  }
  ctx.drawImage(overlay,x,y,l.width,l.height);this.q('.fx-seek').value=Math.floor(time);this.q('output').textContent=`${Math.floor(time)}ms · 배우 ${index+1} · ${row.label}`;
 }
 tick(){cancelAnimationFrame(this.raf);this.draw(this.clock.read(performance.now()));if(this.clock.running)this.raf=requestAnimationFrame(()=>this.tick());}
}
if(typeof module!=='undefined')module.exports={FxClock,selectFx,FxTimelineView};
