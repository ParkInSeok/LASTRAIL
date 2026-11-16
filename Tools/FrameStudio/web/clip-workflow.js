/* View inputs delegate existing Studio commands. No inferred direction or approval. */
class LinkedResourceView {
 constructor(root){this.root=root;}
 setProject(project){
  this.root.replaceChildren();const bundle=project.linked_resources;this.root.hidden=!bundle;if(!bundle)return;
  const title=document.createElement('h3');title.textContent='연결 리소스 · 장갑과 드론';
  const note=document.createElement('p');note.textContent=bundle.note||'';this.root.append(title,note);
  const add=(label,href)=>{const a=document.createElement('a');a.textContent=label+' ';a.href=href;a.target='_blank';a.rel='noopener';this.root.append(a);};
  if(/^[a-f0-9]{32}\.zip$/.test(bundle.archive))add('독립 리소스·타이밍 묶음 받기','/assets/'+bundle.archive);
  try{const u=new URL(bundle.preview);if(u.protocol==='http:'&&u.hostname==='127.0.0.1'&&!u.username&&!u.password)add('장갑·드론 연결 미리보기',u.href);}catch{}
  if(bundle.idle_project&&/^[a-f0-9]{32}$/.test(bundle.idle_project))add('기존 대기 모션 열기','/?id='+bundle.idle_project);
 }
}
function numberedPNGs(files, prefix, count, required=true) {
  const found=new Map(),pattern=new RegExp(`^${prefix}[-_](\\d+)\\.png$`,'i');
  for(const file of files){
    const match=file.name.match(pattern),index=match?Number(match[1])-1:-1;
    if(index<0||index>=count||found.has(index))throw Error(`${prefix}-01.png 형식으로 중복 없이 1~${count}번을 선택하세요.`);
    found.set(index,file);
  }
  if(required&&found.size!==count)throw Error(`시간표와 같은 ${count}장의 원화를 선택하세요.`);
  return found;
}
async function pngData(file){
  if(!file)return null;
  return new Promise((resolve,reject)=>{const reader=new FileReader();reader.onload=()=>resolve(reader.result.split(',')[1]);reader.onerror=()=>reject(Error('파일을 읽지 못했습니다.'));reader.readAsDataURL(file)});
}
class ClipWorkflow {
  constructor(root,onAction,onOpen){
    this.root=root;this.onAction=onAction;this.onOpen=onOpen;this.epoch=0;
    root.innerHTML=`<h2>방향별 제작 · 외부 작화 가져오기</h2>
      <div class="actions"><label>캐릭터 <input class="clip-character" maxlength="100"></label><label>동작 <input class="clip-motion" maxlength="100" placeholder="공격 / Idle"></label><label>게임 방향 <select class="clip-direction"><option value="1">1 · 앞쪽 상대 객차</option><option value="2">2 · 옆 상대 객차</option><option value="3">3 · 뒤쪽 상대 객차</option><option value="4">4 · 우리 열차 정면</option></select></label><button class="clip-identify">방향 등록</button></div>
      <div class="direction-board" aria-label="방향별 제작 상태"></div>
      <p class="muted">방향을 등록한 프로젝트만 비교합니다. 미등록 방향을 이름으로 추측하거나 승인 처리하지 않습니다.</p>
      <div class="actions"><label>원화 여러 장 <input class="clip-actors" type="file" accept="image/png" multiple></label><label>독립 FX 여러 장 (선택) <input class="clip-fx" type="file" accept="image/png" multiple></label><button class="clip-import">시간표대로 한 번에 가져오기</button></div>
      <p class="muted">원화 frame-01.png… / 같은 캔버스의 FX fx-01.png… · 현재 시간표의 순서와 시간을 유지합니다. 기존 결과는 이력에 보존합니다. 전체 PNG 24MB 이하.</p><div class="actions"><label>선택 프레임 FX <input class="frame-fx-current" type="file" accept="image/png"></label><button class="frame-fx-save">선택 프레임 FX 교체</button><button class="frame-fx-clear">선택 프레임 FX 제거</button><span class="frame-fx-status muted"></span></div>
      <details><summary>전투 탄환 · 단발 사격 설정</summary><div class="actions"><label><input class="shot-enabled" type="checkbox">탄환 사용</label><label>독립 탄환 PNG <input class="shot-image" type="file" accept="image/png"></label><label>발사 프레임 <input class="shot-frame" type="number" min="1" value="6"></label></div>
      <p class="muted">총구 위치는 아래 전투 배치에서 ‘총구 클릭 지정’으로 잡습니다. 탄환 PNG는 가로로 오른쪽을 향한 그림을 사용하세요.</p>
      <div class="actions"><label>총구 가로 (0~1) <input class="shot-x" type="number" min="0" max="1" step=".001" value=".5"></label><label>총구 세로 (0~1) <input class="shot-y" type="number" min="0" max="1" step=".001" value=".15"></label><label>방향 (오른쪽 0°, 위 -90°) <input class="shot-angle" type="number" min="-180" max="180" step=".1" value="-90"></label></div>
      <div class="actions"><label>속도 (px/초) <input class="shot-speed" type="number" min="1" max="3000" value="550"></label><label>비행 시간 (ms) <input class="shot-duration" type="number" min="20" max="3000" step="10" value="220"></label><label>길이 (px) <input class="shot-length" type="number" min="1" max="120" value="15"></label><label>두께 (px) <input class="shot-width" type="number" min="1" max="40" value="3"></label><button class="shot-save">탄환 설정 저장</button></div></details><p class="workflow-status muted" role="status"></p>`;
    this.q=selector=>root.querySelector(selector);
    if(typeof FxTimelineView!=='undefined'){
      const timeline=document.createElement('div');root.append(timeline);
      this.fxTimeline=new FxTimelineView(timeline,(name,data)=>onAction(name,data));
    }
    const effects=document.createElement('div');root.append(effects);
    this.effects=new EffectDesignView(effects,style=>this.busy(async()=>{
      if(!this.project)throw Error('프로젝트를 먼저 선택하세요.');
      const epoch=this.epoch;
      await onAction('effect_design',{style,projectile:this.projectileInput()});
      if(epoch===this.epoch)this.q('.workflow-status').textContent='탄환·명중 효과를 제작하고 저장했습니다. 아래 전투 반복 영상으로 확인하세요.';
    }));
    this.q('.clip-identify').onclick=()=>this.busy(()=>onAction('clip_identity',{character:this.q('.clip-character').value,motion:this.q('.clip-motion').value,direction:Number(this.q('.clip-direction').value)}));
    this.q('.frame-fx-save').onclick=()=>this.busy(async()=>{const file=this.q('.frame-fx-current').files[0];if(!file)throw Error('교체할 FX PNG를 선택하세요.');if(file.size>24*1024*1024)throw Error('FX는 24MB 이하입니다.');const epoch=this.epoch,index=this.index,png=await pngData(file);if(epoch!==this.epoch)return;await onAction('frame_fx',{index,png});this.q('.frame-fx-current').value='';});
    this.q('.frame-fx-clear').onclick=()=>this.busy(()=>onAction('frame_fx',{index:this.index,png:null}));
    this.q('.clip-import').onclick=()=>this.busy(async()=>{
      if(!this.project)throw Error('프로젝트를 먼저 선택하세요.');
      const p=this.project,epoch=this.epoch,actors=numberedPNGs(this.q('.clip-actors').files,'frame',p.frames.length),fx=numberedPNGs(this.q('.clip-fx').files,'fx',p.frames.length,false);
      if([...actors.values(),...fx.values()].reduce((total,file)=>total+file.size,0)>24*1024*1024)throw Error('전체 PNG는 24MB 이하로 선택하세요.');
      const rows=[];
      for(let i=0;i<p.frames.length;i++)rows.push({duration_ms:p.frames[i].duration_ms,phase:p.frames[i].phase,kind:p.frames[i].kind,notes:p.frames[i].notes,png:await pngData(actors.get(i)),fx_png:await pngData(fx.get(i))});
      if(epoch!==this.epoch)return;
      await onAction('import_clip',{frames:rows});
      if(epoch!==this.epoch)return;
      this.q('.clip-actors').value='';this.q('.clip-fx').value='';this.q('.workflow-status').textContent='원화와 FX를 함께 등록했습니다. 사람 검토 후 승인하세요.';
    });
    this.q('.shot-save').onclick=()=>this.busy(async()=>{
      if(!this.project)throw Error('프로젝트를 먼저 선택하세요.');
      const epoch=this.epoch,enabled=this.q('.shot-enabled').checked;
      const data={enabled,png:await pngData(this.q('.shot-image').files[0]),fire_frame:Number(this.q('.shot-frame').value)-1,muzzle:[Number(this.q('.shot-x').value),Number(this.q('.shot-y').value)],angle_deg:Number(this.q('.shot-angle').value),speed:Number(this.q('.shot-speed').value),duration_ms:Number(this.q('.shot-duration').value),length:Number(this.q('.shot-length').value),width:Number(this.q('.shot-width').value)};
      if(epoch!==this.epoch)return;
      await onAction('projectile_settings',data);if(epoch!==this.epoch)return;this.q('.shot-image').value='';this.q('.workflow-status').textContent='탄환 설정을 저장했습니다. 전투 반복 영상을 다시 제작해 확인하세요.';
    });
  }
  async busy(callback){
    if(this.pending)return;
    this.pending=true;const epoch=this.epoch;
    this.root.querySelectorAll('button').forEach(button=>button.disabled=true);
    try{await callback()}catch(error){if(epoch===this.epoch)this.q('.workflow-status').textContent=error.message}
    finally{this.pending=false;this.root.querySelectorAll('button').forEach(button=>button.disabled=false)}
  }
  projectileInput(){return {enabled:this.q('.shot-enabled').checked,fire_frame:Number(this.q('.shot-frame').value)-1,muzzle:[Number(this.q('.shot-x').value),Number(this.q('.shot-y').value)],angle_deg:Number(this.q('.shot-angle').value),speed:Number(this.q('.shot-speed').value),duration_ms:Number(this.q('.shot-duration').value),length:Number(this.q('.shot-length').value),width:Number(this.q('.shot-width').value)};}
  setProject(p,projects){
    if(this.project?.id!==p.id){++this.epoch;this.cfgKey=null;this.q('.clip-actors').value='';this.q('.clip-fx').value='';this.q('.shot-image').value='';this.q('.workflow-status').textContent='';}
    this.project=p;projects=projects.map(item=>item.id===p.id?{...item,...p,filled:p.frames.filter(frame=>frame.versions.length).length,frame_count:p.frames.length}:item);
    this.effects.setProject(p);
    this.fxTimeline?.setProject(p);
    const identity=p.clip_identity||{};
    this.q('.clip-character').value=identity.character||'';this.q('.clip-motion').value=identity.motion||'';this.q('.clip-direction').value=identity.direction||1;
    this.q('.shot-frame').max=p.frames.length;
    const key=JSON.stringify([p.id,p.battle_projectile]);
    if(key!==this.cfgKey){
      this.cfgKey=key;const cfg=p.battle_projectile||{};
      this.q('.shot-enabled').checked=!!cfg.enabled;
      for(const [selector,value] of Object.entries({frame:(cfg.fire_frame??Math.min(5,p.frames.length-1))+1,x:cfg.muzzle?.[0]??.5,y:cfg.muzzle?.[1]??.15,angle:cfg.angle_deg??-90,speed:cfg.speed??550,duration:cfg.duration_ms??220,length:cfg.length??15,width:cfg.width??3}))this.q('.shot-'+selector).value=value;
    }
    const board=this.q('.direction-board');board.replaceChildren();
    for(let direction=1;direction<=4;direction++){
      const card=document.createElement('div'),title=document.createElement('strong');card.className='direction-card';title.textContent=direction+'번';card.append(title);
      const matches=identity.character&&identity.motion?projects.filter(item=>item.clip_identity?.character===identity.character&&item.clip_identity?.motion===identity.motion&&item.clip_identity?.direction===direction):[];
      if(!matches.length){const text=document.createElement('p');text.textContent='미등록 / 미확인';card.append(text)}
      for(const item of matches){const button=document.createElement('button');button.textContent=`${item.name} · ${item.clip_approved?'승인':item.battle_current?'배치 검토':`${item.filled}/${item.frame_count} 작화`}`;button.onclick=()=>this.onOpen(item.id);card.append(button)}
      board.append(card);
    }
  }
  setMuzzle(x,y){this.q('.shot-x').value=x.toFixed(5);this.q('.shot-y').value=y.toFixed(5);this.q('details').open=true;this.q('.workflow-status').textContent='총구를 지정했습니다. 탄환 설정 저장 후 전투 영상을 다시 제작하세요.';}
  setFrame(index){this.index=index;this.q('.frame-fx-status').textContent=this.project?.fx_timeline?'독립 FX 적용 중 · 정밀 재생 패널과 전투 내보내기에서 확인':`${index+1}번 · ${this.project?.frames[index]?.fx_asset?'FX 등록됨':'FX 없음'}`;}
}

/* Approved reference library: navigation delegates to the existing controller. */
class ApprovedLibrary {
 constructor(root, records, onOpen){
  this.records=records;this.root=root;
  const title=document.createElement('h2');title.textContent='승인본 모음 · 레이나 / 카일 / 이브 / 세라';
  const note=document.createElement('p');note.textContent='방향별 대기·공격 승인본을 엽니다. 기존 프레임 수를 유지했습니다. 사용자 시각 승인과 도구 자동 검사·Unity 적용 상태는 별도입니다.';
  this.status=document.createElement('p');this.status.setAttribute('role','status');root.append(title,note,this.status);
  for(const character of [...new Set(records.map(r=>r.character))]){
   const group=document.createElement('details');group.open=character==='세라';
   const summary=document.createElement('summary');summary.textContent=character;group.append(summary);
   for(let direction=1;direction<=4;direction++){
    const row=document.createElement('div');row.style.cssText='display:flex;gap:8px;flex-wrap:wrap;margin:10px 0';
    for(const r of records.filter(r=>r.character===character&&r.direction===direction)){
     const button=document.createElement('button');button.type='button';button.textContent=`${character} ${direction}번 ${r.motion} · ${r.assets.length}프레임`;
     button.addEventListener('click',()=>onOpen(r.id));row.append(button);
    }group.append(row);
   }root.append(group);
  }
  for(const [label,slug] of [['네 요원 동시 전투 검수','four-agent-final-review-v03'],['레이나·카일 포함 전투 효과 보기','five-agent-combat-fx-v02'],['이브 1~4번 바람·드론 연결 보기','eve-camera-all-review-v01']]){
   const line=document.createElement('p'),link=document.createElement('a');link.textContent=label;link.href='http://127.0.0.1:8786/'+slug+'/';link.target='_blank';link.rel='noopener';line.append(link);root.append(line);
  }
  const preview=document.createElement('a');preview.href='http://127.0.0.1:8786/sera-all-directions-battle-v01/';preview.target='_blank';preview.rel='noopener';preview.textContent='세라 수정·마법진·발사 전투 연결 보기';root.append(preview);
  const info=document.createElement('p');info.textContent='효과는 인물과 독립 리소스입니다. 전투 미리보기의 연속 드론·수정 연출과 제작도구 프레임 효과 표시는 구분해 검수합니다.';root.append(info);
 }
 setProject(project){
  const r=this.records.find(r=>r.id===project.id);
  if(!r){this.status.textContent='현재 프로젝트는 이 승인본 모음에 포함되지 않습니다.';return;}
  const current=project.frames.map(f=>f.versions.at(-1)?.asset);
  const unchanged=JSON.stringify(current)===JSON.stringify(r.assets);
  this.status.textContent=`${r.character} ${r.direction}번 ${r.motion} · `+(unchanged?'정리 시점 인물 프레임과 일치':'정리 이후 프레임 변경됨 · 승인본과 다시 비교 필요')+` · 등록 프레임 효과 ${r.effects.frameFx}장`;
 }
}
