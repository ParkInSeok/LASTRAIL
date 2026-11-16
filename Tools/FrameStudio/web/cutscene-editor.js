const $=id=>document.getElementById(id);
const timeKeys=new Set(["start","length","source","fade_in","fade_out"]);
class EditDraft {
 constructor(saved){this.revision=saved.revision;this.timeline=structuredClone(saved.timeline);this.media=saved.media;this.dirty=false}
 change(){this.dirty=true}
 add(asset,track){
  const m=this.media[asset];if(!m)throw Error("미디어를 먼저 추가하세요.");
  if((track==="visual")!==(m.kind==="image"||m.kind==="video"))throw Error("영상·이미지는 화면, 음원은 BGM/효과음 트랙을 선택하세요.");
  const end=track==="visual"?Math.max(0,...this.timeline.clips.filter(c=>c.track==="visual").map(c=>c.start+c.length)):0;
  const start=end>=this.timeline.frames?0:end;
  const length=Math.min(this.timeline.frames-start,m.kind==="image"?30:Math.floor(m.duration*30));
  this.timeline.clips.push({asset,track,start,length,source:0,x:.5,y:.5,end_x:.5,end_y:.5,scale:1,end_scale:1,opacity:1,gain:1,fade_in:0,fade_out:0});this.change();
 }
}
class EditorController {
 constructor(){
  this.pid=new URLSearchParams(location.search).get("id");this.draft=null;this.selected=-1;
  this.token="";this.timer=null;this.disposed=false;this.busy=false;this.job=null;this.abort=new AbortController();
 }
 async api(action,data={}){
  const response=await fetch("/api/cutscene?id="+encodeURIComponent(this.pid),action?{
   method:"POST",signal:this.abort.signal,headers:{"Content-Type":"application/json","X-Studio-Token":this.token},body:JSON.stringify({action,...data})
  }:{signal:this.abort.signal});
  const result=await response.json();if(!response.ok)throw Error(result.error||"요청 실패");return result;
 }
 notice(message){$("status").textContent=message}
 async guard(fn){
  if(this.busy)return;this.busy=true;this.buttons();
  try{await fn()}catch(e){if(!this.disposed)this.notice(e.message)}finally{this.busy=false;this.buttons()}
 }
 buttons(){
  for(const id of ["save","preview","export"])$(id).disabled=this.busy||!this.draft;
  for(const el of document.querySelectorAll(".edit input,.edit select,.edit button"))el.disabled=this.busy||!this.draft;
 }
 mediaUrl(file){return "/cutscene-media?id="+encodeURIComponent(this.pid)+"&file="+encodeURIComponent(file)}
 async init(){
  if(!/^[0-9a-f]{32}$/.test(this.pid||""))throw Error("프레임 제작 화면에서 프로젝트를 선택한 뒤 열어 주세요.");
  const r=await fetch("/api/state?id="+this.pid,{signal:this.abort.signal});const s=await r.json();
  if(!r.ok)throw Error(s.error);this.token=s.token;
  $("aiVideo").href="/video-generation?id="+this.pid;$("back").href="/?id="+this.pid;$("motion").href="/animation?id="+this.pid;$("projectName").textContent=s.project.name;
  const saved=await this.api();this.draft=new EditDraft(saved);this.bind();this.draw();this.showJob(saved.job);
  this.notice("편집 준비 완료 · 자동 재생하지 않습니다.");this.buttons();
  if(saved.job&&["running","queued"].includes(saved.job.status))this.poll();
 }
 changed(){this.draft.change();$("player").pause();$("previewState").textContent="편집 변경됨 · 미리보기/출력을 다시 만들면 반영됩니다.";this.drawList()}
 bind(){
  $("files").onchange=()=>this.guard(async()=>{
   for(const file of $("files").files){
    if(file.size>512*1024*1024)throw Error("파일당512MB 이하입니다.");
    this.notice(file.name+" 등록 중…");
    const r=await fetch("/api/cutscene-import?id="+this.pid+"&name="+encodeURIComponent(file.name),
     {method:"POST",signal:this.abort.signal,headers:{"X-Studio-Token":this.token,"Content-Type":"application/octet-stream"},body:file});
    const m=await r.json();if(!r.ok)throw Error(m.error);this.draft.media[m.id]=m;
   }$("files").value="";this.drawMedia();this.notice("미디어를 등록했습니다. 트랙을 골라 배치하세요.");
  });
  $("add").onclick=()=>this.guard(async()=>{this.draft.add($("media").value,$("track").value);this.selected=this.draft.timeline.clips.length-1;this.changed();this.drawForm()});
  $("viewMedia").onclick=()=>this.guard(()=>this.previewMedia());
  $("viewOutput").onclick=()=>{if(this.job?.status==="complete")this.showJob(this.job);else this.notice("아직 저장된 편집 출력이 없습니다.")};
  $("save").onclick=()=>this.guard(()=>this.save());
  $("preview").onclick=()=>this.guard(()=>this.render(true));
  $("export").onclick=()=>this.guard(()=>this.render(false));
  $("cancel").onclick=()=>this.guard(async()=>{const s=await this.api("cancel");this.showJob(s.job);this.notice("출력 중단 요청");this.poll()});
  for(const id of ["duration","label","bgm","sfx"])$(id).onchange=()=>{
   this.draft.timeline[id==="duration"?"frames":id]=id==="label"?$(id).value:id==="duration"?Math.round(Number($(id).value)*30):Number($(id).value);
   this.changed();
  };
  $("clipForm").onsubmit=e=>e.preventDefault();
  $("clipForm").onchange=e=>{
   const key=e.target.name,c=this.draft.timeline.clips[this.selected];if(!c||!key)return;
   c[key]=timeKeys.has(key)?Math.round(Number(e.target.value)*30):Number(e.target.value);this.changed();
  };
  $("remove").onclick=()=>{if(this.selected<0)return;this.draft.timeline.clips.splice(this.selected,1);this.selected=-1;this.changed();this.drawForm()};
  for(const [id,offset] of [["up",-1],["down",1]])$(id).onclick=()=>{
   const list=this.draft.timeline.clips,a=this.selected,b=a+offset;if(a<0||b<0||b>=list.length)return;
   [list[a],list[b]]=[list[b],list[a]];this.selected=b;this.changed();this.drawForm();
  };
  $("restart").onclick=()=>{const v=$("player");if(!v.getAttribute("src"))return;v.currentTime=0;v.play().catch(e=>this.notice(e.message))};
  $("skip").onclick=()=>{const v=$("player");v.pause();if(Number.isFinite(v.duration))v.currentTime=v.duration};
  $("loop").onchange=()=>{$("player").loop=$("loop").checked};
  document.addEventListener("visibilitychange",()=>{if(document.hidden)$("player").pause()},{signal:this.abort.signal});
  window.addEventListener("beforeunload",e=>{if(this.draft?.dirty){e.preventDefault();e.returnValue=""}},{signal:this.abort.signal});
  window.addEventListener("pagehide",()=>{this.disposed=true;clearTimeout(this.timer);$("player").pause();this.abort.abort()},{once:true});
 }
 async save(){
  if(!this.draft.dirty)return;
  const s=await this.api("save",{timeline:this.draft.timeline,revision:this.draft.revision});
  this.draft=new EditDraft(s);this.draw();this.showJob(s.job);this.notice("편집 저장 완료 · 버전 "+s.revision);
 }
 async previewMedia(){
  const m=this.draft.media[$("media").value];
  if(!m||m.kind!=="video")throw Error("등록된 영상을 선택하세요.");
  if(this.job&&["queued","running"].includes(this.job.status))throw Error("출력이 끝난 뒤 원본을 확인하세요.");
  const v=$("player");v.pause();v.src=this.mediaUrl(m.file);v.style.height="auto";v.style.aspectRatio="auto";v.load();
  $("download").hidden=true;
  $("previewState").textContent="등록 원본 · "+m.name+" · 편집 배치·최종 기준 지정 아님";
  this.notice("원본 비교 재생 · 편집 시간표와 승인 상태는 바뀌지 않습니다.");
  await v.play();
 }
 async render(preview){
  await this.save();$("player").pause();
  const job=await this.api("render",{revision:this.draft.revision,preview});
  this.showJob(job);this.poll();
 }
 poll(){
  clearTimeout(this.timer);if(this.disposed)return;
  this.timer=setTimeout(async()=>{
   try{const s=await this.api();this.showJob(s.job);if(s.job&&["queued","running"].includes(s.job.status))this.poll()}
   catch(e){if(!this.disposed)this.notice(e.message)}
  },700);
 }
 showJob(job){
  this.job=job;if(!job)return;
  $("progress").value=job.progress||0;
  $("version").textContent=JSON.stringify({revision:job.revision,status:job.status,stale:job.stale,preview:job.preview},null,2);
  $("cancel").disabled=!["queued","running"].includes(job.status);
  const ready=job.status==="complete";$("download").hidden=!ready;
  if(ready){
   const url=this.mediaUrl(job.file);$("download").href=url;$("download").textContent=job.preview?"미리보기 MP4 다운로드":"완성 MP4 다운로드";
   const v=$("player");if(v.getAttribute("src")!==url){v.pause();v.src=url;v.load()}
   v.style.height='auto';v.style.aspectRatio='auto';
   $("previewState").textContent=(job.preview?"미리보기":"완성 출력")+" · "+(job.stale||this.draft.dirty?"이전 편집본 — 다시 출력 필요":"저장 버전 "+job.revision)+" · "+(job.label||"이전 출력");
  }
  this.notice(job.status==="failed"?job.error:({queued:"출력 준비 중",running:"영상·음향 합성 중",complete:"출력 완료 · 재생해 확인하세요.",cancelled:"출력을 중단했습니다.",interrupted:"서버 재시작으로 출력이 중단됐습니다."}[job.status]||job.status));
 }
 draw(){const t=this.draft.timeline;$("duration").value=(t.frames/30).toFixed(3);for(const k of ["label","bgm","sfx"])$(k).value=t[k];this.drawMedia();this.drawList();this.drawForm()}
 drawMedia(){
  const old=$("media").value;$("media").replaceChildren(...Object.values(this.draft.media).map(m=>new Option(`${m.name} · ${m.kind}`,m.id)));
  if(this.draft.media[old])$("media").value=old;
 }
 drawList(){
  const t=this.draft.timeline;$("clips").replaceChildren();$("ruler").replaceChildren();
  for(const track of ["visual","BGM","SFX"]){
   const lane=document.createElement("div");lane.className="lane "+track;lane.title=track;
   t.clips.forEach((c,i)=>{if(c.track!==track)return;const span=document.createElement("span");span.style.left=(c.start/t.frames*100)+"%";span.style.width=(c.length/t.frames*100)+"%";span.textContent=`${i+1} ${this.draft.media[c.asset]?.name}`;lane.append(span)});$("ruler").append(lane);
  }
  t.clips.forEach((c,i)=>{
   const row=document.createElement("tr");if(i===this.selected)row.className="selected";
   const cell=document.createElement("td"),button=document.createElement("button");button.textContent=String(i+1);
   button.onclick=()=>{this.selected=i;this.drawList();this.drawForm()};cell.append(button);row.append(cell);
   for(const text of [`${c.track} · ${this.draft.media[c.asset]?.name}`,(c.start/30).toFixed(3),(c.length/30).toFixed(3)]){const td=document.createElement("td");td.textContent=text;row.append(td)}
   $("clips").append(row);
  });
 }
 drawForm(){
  const c=this.draft.timeline.clips[this.selected];$("clipForm").hidden=!c;if(!c)return;
  $("clipName").textContent=this.draft.media[c.asset]?.name||"";
  for(const input of $("clipForm").querySelectorAll("input[name]"))input.value=timeKeys.has(input.name)?(c[input.name]/30).toFixed(3):c[input.name];
 }
}
const editor=new EditorController();editor.init().catch(e=>editor.notice(e.message));
