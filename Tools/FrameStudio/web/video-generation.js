const el=id=>document.getElementById(id);
class VideoGenerationController{
 constructor(){this.pid=new URLSearchParams(location.search).get('id');this.media={};this.token='';this.job=null;this.timer=null;this.abort=new AbortController();this.disposed=false;this.busy=false;}
 async request(url,body,raw=false){const r=await fetch(url,{signal:this.abort.signal,...(body?{method:'POST',headers:{'X-Studio-Token':this.token,'Content-Type':raw?'application/octet-stream':'application/json'},body:raw?body:JSON.stringify(body)}:{})});const s=await r.json();if(!r.ok)throw Error(s.error||'요청 실패');return s;}
 async init(){
  if(!/^[0-9a-f]{32}$/.test(this.pid||''))throw Error('컷씬 프로젝트에서 열어 주세요.');
  const state=await this.request('/api/state?id='+this.pid);this.token=state.token;el('name').textContent=state.project.name;
  this.media=(await this.request('/api/cutscene?id='+this.pid)).media;this.drawImages();
  for(const id of ['back','edit'])el(id).href='/cutscene?id='+this.pid;
  el('image').onchange=()=>this.showImage();
  el('generate').onclick=()=>this.guard(async()=>{const s=await this.request('/api/video-generation?id='+this.pid,{action:'generate',asset:el('image').value,prompt:el('prompt').value,seed:Number(el('seed').value)});this.show(s.job);this.poll();});
  el('cancel').onclick=()=>this.guard(async()=>{const s=await this.request('/api/video-generation?id='+this.pid,{action:'cancel'});this.show(s.job);this.poll();});
  el('upload').onchange=()=>this.guard(async()=>{const f=el('upload').files[0];if(!f)return;if(f.size>32*1024*1024)throw Error('이미지는 32MB 이하입니다.');const m=await this.request('/api/cutscene-import?id='+this.pid+'&name='+encodeURIComponent(f.name),f,true);this.media[m.id]=m;this.drawImages(m.id);el('upload').value='';});
  window.addEventListener('pagehide',()=>{this.disposed=true;clearTimeout(this.timer);this.abort.abort();el('result').pause();},{once:true});
  // A BFCache restore revives an aborted controller; rebuild from the saved job receipt.
  window.addEventListener('pageshow',e=>{if(e.persisted)location.reload();});
  const job=(await this.request('/api/video-generation?id='+this.pid)).job;
  if(job?.request){el('image').value=job.request.asset;el('prompt').value=job.request.prompt;el('seed').value=job.request.seed;this.showImage();}
  this.show(job);this.poll();
 }
 drawImages(selected){el('image').replaceChildren();for(const m of Object.values(this.media).filter(m=>m.kind==='image')){const o=document.createElement('option');o.value=m.id;o.textContent=m.name;el('image').append(o);}if(selected)el('image').value=selected;this.showImage();}
 url(file){return '/cutscene-media?id='+this.pid+'&file='+encodeURIComponent(file);}
 showImage(){const m=this.media[el('image').value];if(m)el('reference').src=this.url(m.file);}
 async guard(fn){if(this.busy)return;this.busy=true;this.buttons();try{await fn();}catch(e){if(!this.disposed)el('status').textContent=e.message;}finally{this.busy=false;this.buttons();}}
 buttons(){const active=['running','queued','cancelling'].includes(this.job?.status);el('generate').disabled=this.busy||active||!el('image').value;el('cancel').disabled=this.busy||!['running','queued'].includes(this.job?.status);for(const id of ['upload','image','prompt','seed'])el(id).disabled=this.busy||active;}
 show(job){this.job=job;el('status').textContent=job?.message||'생성 준비 완료 · ComfyUI가 실행 중이어야 합니다.';if(job?.media){this.media[job.media.id]=job.media;const url=this.url(job.media.file);if(el('result').getAttribute('src')!==url)el('result').src=url;el('download').href=url;el('download').hidden=false;el('details').textContent=`${job.media.width}×${job.media.height} · ${job.media.duration.toFixed(2)}초 · 로컬 AI 생성 · 미디어 등록 완료`;}this.buttons();}
 poll(){clearTimeout(this.timer);if(this.disposed||!['running','queued','cancelling'].includes(this.job?.status))return;this.timer=setTimeout(async()=>{try{this.show((await this.request('/api/video-generation?id='+this.pid)).job);this.poll();}catch(e){if(!this.disposed)el('status').textContent='상태 확인 중단: '+e.message+' · 다시 열어 저장 결과를 확인하세요.';}},2000);}
}
const controller=new VideoGenerationController();controller.init().catch(e=>{el('status').textContent=e.message;el('generate').disabled=true;});
