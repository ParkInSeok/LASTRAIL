import {AnimationDraft, POSE, samplePose, worlds, drawSurface} from './animation-model.js';
const $=id=>document.getElementById(id);
class MotionController {
 constructor(){this.pid=new URLSearchParams(location.search).get('id');this.abort=new AbortController();this.images=new Map();this.frame=0;this.selected=null;this.busy=false;this.playing=false;this.disposed=false;}
 notice(text){$('status').textContent=text;}
 url(file){return '/cutscene-media?id='+this.pid+'&file='+encodeURIComponent(file);}
 async api(action,data={}){
  const r=await fetch('/api/animation?id='+this.pid,action?{method:'POST',signal:this.abort.signal,headers:{'Content-Type':'application/json','X-Studio-Token':this.token},body:JSON.stringify({action,...data})}:{signal:this.abort.signal});
  const s=await r.json();if(!r.ok)throw Error(s.error||'요청 실패');return s;
 }
 async guard(fn){if(this.busy)return;this.busy=true;this.buttons();try{await fn();}catch(e){if(!this.disposed)this.notice(e.message);}finally{this.busy=false;this.buttons();}}
 buttons(){
  if(!this.draft)return;
  for(const e of document.querySelectorAll('#editor input,#editor select,#editor button,#undo,#redo,#save,#preview,#export'))e.disabled=this.busy;
  const running=['queued','running'].includes(this.job?.status);
  $('preview').disabled=$('export').disabled=this.busy||running;
  $('cancel').disabled=this.busy||!running;
  $('apply').disabled=this.busy||this.draft.dirty||this.job?.status!=='complete'||this.job.preview||this.job.revision!==this.draft.revision;
 }
 async init(){
  if(!/^[a-f0-9]{32}$/.test(this.pid||''))throw Error('프로젝트를 먼저 선택하세요.');
  const [r,s]=await Promise.all([fetch('/api/state?id='+this.pid,{signal:this.abort.signal}),this.api()]);
  const state=await r.json();if(!r.ok)throw Error(state.error);
  this.token=state.token;this.draft=new AnimationDraft(s);this.media=s.media;this.cutRevision=s.cutsceneRevision;
  $('projectName').textContent=state.project.name;$('back').href='/cutscene?id='+this.pid;
  const canvas=$('canvas');canvas.width=(this.draft.scene.width||1080)/2;canvas.height=(this.draft.scene.height||1920)/2;canvas.style.aspectRatio=`${canvas.width}/${canvas.height}`;canvas.style.width='100%';document.querySelector('figcaption span').textContent=`${canvas.width*2} × ${canvas.height*2}`;
  this.selected=this.draft.scene.parts.find(p=>p.id==='forearm')?.id||this.draft.scene.parts[0]?.id;
  this.bind();this.mediaOptions();this.refresh();this.showJob(s.job);
  const hasCharacterArt=this.draft.scene.parts.some(p=>p.kind==='sprite'&&p.id!=='background');
  document.querySelector('.notice').textContent=hasCharacterArt?'등록된 독립 작화 파츠를 편집합니다. 위치·회전·크기와 자세 키를 바꾸고 출력할 수 있습니다.':'처음 보이는 도형은 관절 연습용입니다. 실제 캐릭터에는 독립된 파츠 PNG와 필요한 손·표정 작화를 등록하세요.';
  this.notice(hasCharacterArt?'독립 작화 동작 편집 준비 완료':'동작 제작 준비 완료 · 도형은 관절 연습용입니다.');
  if(['queued','running'].includes(s.job?.status))this.poll();
 }
 options(select,items,value){select.replaceChildren(...items.map(([id,label])=>{const o=document.createElement('option');o.value=id;o.textContent=label;return o;}));select.value=value||'';}
 mediaOptions(){
  const images=Object.values(this.media).filter(m=>m.kind==='image').map(m=>[m.id,m.name]);
  this.options($('assetSelect'),images,$('assetSelect').value||images[0]?.[0]);
  this.options($('referenceSelect'),[['','비교 영상 없음'],...Object.values(this.media).filter(m=>m.kind==='video').map(m=>[m.id,m.name])],this.draft.scene.reference);
  const ref=this.media[this.draft.scene.reference];const v=$('reference');
  v.style.display=ref?'':'none';document.querySelector('.monitors').style.gridTemplateColumns=ref?'':'1fr';
  if(ref){if(v.getAttribute('src')!==this.url(ref.file))v.src=this.url(ref.file);}else{v.removeAttribute('src');v.load();}
  for(const m of Object.values(this.media).filter(m=>m.kind==='image')){
   if(this.images.has(m.id))continue;
   const im=new Image();this.images.set(m.id,im);im.onload=()=>{if(!this.disposed)this.draw();};im.src=this.url(m.file);
  }
 }
 change(fn){if(this.busy)return;try{this.draft.change(fn);this.refresh();this.notice('변경됨 · 저장 후 출력에 반영됩니다.');}catch(e){this.notice(e.message);}}
 key(changes){if(this.busy)return;try{this.draft.key(this.selected,this.frame,changes);this.refresh();}catch(e){this.notice(e.message);}}
 part(){return this.draft.scene.parts.find(p=>p.id===this.selected);}
 refresh(){
  const s=this.draft.scene,p=this.part();this.frame=Math.min(this.frame,s.frames-1);
  $('frames').value=s.frames;$('background').value=s.background;$('frame').max=s.frames-1;
  $('parts').replaceChildren(...s.parts.map(p=>{const b=document.createElement('button');b.textContent=(p.parent?'↳ ':'')+p.name;b.className=p.id===this.selected?'selected':'';b.onclick=()=>{this.selected=p.id;this.refresh();};return b;}));
  $('partForm').hidden=$('poseForm').hidden=!p;
  if(p){
   for(const el of $('partForm').elements){if(!el.name||el.name==='parent')continue;if(el.type==='checkbox')el.checked=p[el.name];else el.value=p[el.name]??(el.name==='bend_start'?.5:'');if(['width','height'].includes(el.name)){el.min=p.kind==='group'?0:1;}}
   const descendants=new Set([p.id]);let n;do{n=descendants.size;for(const q of s.parts)if(descendants.has(q.parent))descendants.add(q.id);}while(n!==descendants.size);
   this.options($('partForm').elements.parent,[['','장면 기준'],...s.parts.filter(q=>!descendants.has(q.id)).map(q=>[q.id,q.name])],p.parent);
   this.options($('poseForm').elements.asset,[['','작화 없음'],...Object.values(this.media).filter(m=>m.kind==='image').map(m=>[m.id,m.name])],samplePose(p,this.frame).asset);
  }
  this.frameView();this.buttons();
 }
 frameView(){
  $('frame').value=this.frame;$('time').textContent=(this.frame/30).toFixed(2)+'초 · '+this.frame+'프레임';
  const p=this.part();$('keys').replaceChildren();
  if(p){const pose=samplePose(p,this.frame);for(const el of $('poseForm').elements)if(el.name)el.value=pose[el.name]??'';
   for(const k of p.keys){const b=document.createElement('button');b.textContent=k.frame+'f';b.onclick=()=>{this.stop();this.frame=k.frame;this.frameView();};$('keys').append(b);}
  }
  const v=$('reference');if(v.readyState>=1&&Number.isFinite(v.duration)){const t=Math.min(this.frame/30,Math.max(0,v.duration-.001));if(Math.abs(v.currentTime-t)>.025)v.currentTime=t;}
  this.draw();
 }
 draw(){
  const c=$('canvas').getContext('2d'),s=this.draft.scene;c.setTransform(1,0,0,1,0,0);c.globalAlpha=1;c.fillStyle=s.background;c.fillRect(0,0,$('canvas').width,$('canvas').height);
  const poses=worlds(s,this.frame);
  for(const p of s.parts){if(p.kind==='group')continue;const v=poses.get(p.id);c.setTransform(...v.matrix.map(x=>x*.5));c.globalAlpha=v.opacity;c.translate(-p.width*p.pivot_x,-p.height*p.pivot_y);c.fillStyle=c.strokeStyle=p.color;
   drawSurface(c,p,v.bend,()=>{
   if(p.kind==='sprite'){const im=this.images.get(v.asset);if(im?.complete&&im.naturalWidth)c.drawImage(im,0,0,p.width,p.height);}
   else if(p.kind==='rect')c.fillRect(0,0,p.width,p.height);
   else if(p.kind==='ellipse'){c.beginPath();c.ellipse(p.width/2,p.height/2,p.width/2,p.height/2,0,0,Math.PI*2);c.fill();}
   else {c.lineWidth=Math.max(1,Math.round(Math.min(p.width,p.height)*.035));for(const deg of [5,95,185,275]){c.beginPath();c.ellipse(p.width/2,p.height/2,Math.max(0,(p.width-c.lineWidth)/2),Math.max(0,(p.height-c.lineWidth)/2),0,deg*Math.PI/180,(deg+65)*Math.PI/180);c.stroke();}}
   });
  }
  const v=poses.get(this.selected);if(v){c.setTransform(1,0,0,1,0,0);c.globalAlpha=1;c.strokeStyle='#ffdb89';c.lineWidth=2;c.beginPath();c.arc(v.matrix[4]/2,v.matrix[5]/2,6,0,Math.PI*2);c.stroke();}
 }
 stop(){this.playing=false;cancelAnimationFrame(this.raf);$('play').textContent='재생';}
 play(){if(this.playing){this.stop();return;}this.playing=true;$('play').textContent='정지';const start=performance.now(),initial=this.frame;
  const tick=now=>{if(!this.playing||this.disposed)return;let f=initial+Math.floor((now-start)*.03);if(f>=this.draft.scene.frames){if(!$('loop').checked){this.frame=this.draft.scene.frames-1;this.stop();this.frameView();return;}f%=this.draft.scene.frames;}this.frame=f;this.frameView();this.raf=requestAnimationFrame(tick);};this.raf=requestAnimationFrame(tick);
 }
 bind(){
  $('play').onclick=()=>this.play();$('rewind').onclick=()=>{this.stop();this.frame=0;this.frameView();};
  $('frame').oninput=()=>{this.stop();this.frame=Number($('frame').value);this.frameView();};
  for(const id of ['undo','redo'])$(id).onclick=()=>{this.draft[id]();this.mediaOptions();this.refresh();};
  $('partForm').onsubmit=$('poseForm').onsubmit=e=>e.preventDefault();
  $('partForm').onchange=e=>{const el=e.target;if(!el.name||!el.checkValidity())return;this.change(s=>{const p=s.parts.find(p=>p.id===this.selected);p[el.name]=el.type==='checkbox'?el.checked:el.type==='number'?Number(el.value):el.value||null;});};
  $('poseForm').onchange=e=>{const el=e.target;if(el.name&&el.checkValidity())this.key({[el.name]:el.type==='number'?Number(el.value):el.value||null});};
  $('keyAdd').onclick=()=>this.key({});$('keyRemove').onclick=()=>this.change(s=>{if(this.frame===0)throw Error('첫 자세 키는 유지해야 합니다.');const p=s.parts.find(p=>p.id===this.selected);p.keys=p.keys.filter(k=>k.frame!==this.frame);});
  $('frames').onchange=()=>this.change(s=>{const n=Number($('frames').value);if(!Number.isInteger(n)||n<1||n>900||s.parts.some(p=>p.keys.some(k=>k.frame>=n)))throw Error('길이와 남아 있는 자세 키를 확인하세요.');s.frames=n;});
  $('background').onchange=()=>this.change(s=>s.background=$('background').value);
  $('referenceSelect').onchange=()=>{this.change(s=>s.reference=$('referenceSelect').value||null);this.mediaOptions();};
  $('add').onclick=()=>this.change(s=>{if(s.parts.length>=32)throw Error('파츠는 32개까지입니다.');const kind=$('addKind').value,asset=kind==='sprite'?$('assetSelect').value:null;if(kind==='sprite'&&!asset)throw Error('이미지를 먼저 등록하세요.');const id='part'+crypto.randomUUID().replaceAll('-','');s.parts.push({id,name:'새 파츠',parent:null,kind,color:'#caa46a',width:kind==='group'?0:200,height:kind==='group'?0:200,pivot_x:.5,pivot_y:.5,visible:true,keys:[{...POSE,frame:0,x:(s.width||1080)/2,y:(s.height||1920)/2,asset}]});this.selected=id;});
  $('remove').onclick=()=>this.change(s=>{const ids=new Set([this.selected]);let n;do{n=ids.size;for(const p of s.parts)if(ids.has(p.parent))ids.add(p.id);}while(n!==ids.size);s.parts=s.parts.filter(p=>!ids.has(p.id));this.selected=s.parts[0]?.id;});
  for(const [id,delta] of [['up',-1],['down',1]])$(id).onclick=()=>this.change(s=>{const i=s.parts.findIndex(p=>p.id===this.selected),j=i+delta;if(i>=0&&j>=0&&j<s.parts.length)[s.parts[i],s.parts[j]]=[s.parts[j],s.parts[i]];});
  $('files').onchange=()=>this.guard(async()=>{for(const file of $('files').files){if(file.size>32*1024*1024)throw Error('파츠 이미지는 32MB 이하로 등록하세요.');const r=await fetch('/api/cutscene-import?id='+this.pid+'&name='+encodeURIComponent(file.name),{method:'POST',signal:this.abort.signal,headers:{'X-Studio-Token':this.token},body:file});const m=await r.json();if(!r.ok)throw Error(m.error);this.media[m.id]=m;}$('files').value='';this.mediaOptions();this.refresh();});
  $('save').onclick=()=>this.guard(()=>this.save());
  for(const [id,preview] of [['preview',true],['export',false]])$(id).onclick=()=>this.guard(async()=>{this.stop();await this.save();this.showJob(await this.api('render',{revision:this.draft.revision,preview}));this.poll();});
  $('cancel').onclick=()=>this.guard(async()=>{this.showJob((await this.api('cancel')).job);this.poll();});
  $('apply').onclick=()=>this.guard(async()=>{const s=await this.api('apply',{revision:this.draft.revision,cutsceneRevision:this.cutRevision});this.cutRevision=s.revision;this.notice('컷씬에 적용했습니다. 영상·음향 편집에서 최종 합성을 출력하세요.');});
  const canvas=$('canvas');let drag=null;
  canvas.onpointerdown=e=>{if(this.busy||!this.part())return;this.stop();const rect=canvas.getBoundingClientRect();drag={x:e.clientX,y:e.clientY,scale:(this.draft.scene.width||1080)/rect.width,pose:samplePose(this.part(),this.frame),parent:this.part().parent?worlds(this.draft.scene,this.frame).get(this.part().parent).matrix:[1,0,0,1,0,0]};canvas.setPointerCapture(e.pointerId);};
  canvas.onpointerup=e=>{if(!drag)return;const d=drag;drag=null;const [a,b,c,f]=d.parent,det=a*f-b*c,dx=(e.clientX-d.x)*d.scale,dy=(e.clientY-d.y)*d.scale;this.key({x:d.pose.x+(f*dx-c*dy)/det,y:d.pose.y+(-b*dx+a*dy)/det});};
  canvas.onpointercancel=()=>{drag=null;};
  document.addEventListener('visibilitychange',()=>{if(document.hidden)this.stop();},{signal:this.abort.signal});
  window.addEventListener('beforeunload',e=>{if(this.draft.dirty){e.preventDefault();e.returnValue='';}},{signal:this.abort.signal});
  window.addEventListener('pagehide',()=>{this.disposed=true;this.stop();clearTimeout(this.timer);this.abort.abort();for(const im of this.images.values())im.onload=null;},{once:true});
 }
 async save(){if(!this.draft.dirty)return;const s=await this.api('save',{revision:this.draft.revision,scene:this.draft.scene});this.draft.load(s);this.cutRevision=s.cutsceneRevision;this.refresh();this.notice('동작 저장 완료 · 버전 '+s.revision);}
 showJob(job){
  this.job=job;$('progress').value=job?.progress||0;
  if(job?.status!=='complete'){$('result').pause();$('result').hidden=true;$('download').hidden=true;}
  if(job){$('resultLabel').textContent=(job.media?(job.media.width+' × '+job.media.height):(job.preview?'미리보기':'전체 해상도 출력'))+' · '+job.status+(job.stale?' · 이전 버전':'')+(job.error?' · '+job.error:'');
   if(job.status==='complete'&&job.media){const url=this.url(job.media.file);if($('result').getAttribute('src')!==url)$('result').src=url;$('result').hidden=false;$('download').hidden=false;$('download').href=url;}
  }this.buttons();
 }
 poll(){clearTimeout(this.timer);if(this.disposed)return;this.timer=setTimeout(async()=>{try{const s=await this.api();this.showJob(s.job);if(['queued','running'].includes(s.job?.status))this.poll();}catch(e){if(!this.disposed)this.notice(e.message);}},750);}
}
const controller=new MotionController();controller.init().catch(e=>controller.notice(e.message));

