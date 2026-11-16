/* View owns DOM and playback; server owns durable state and approvals. */
const $=id=>document.getElementById(id);
const state={project:null,projects:[],index:0,token:'',timer:null,poll:null,playing:false};
const productionFlowButton=document.createElement('button');productionFlowButton.textContent='선화·부위·명암 단계 제작';
productionFlowButton.onclick=async()=>{if(!state.project)return notice('프로젝트를 먼저 선택하세요.',true);try{const r=await fetch('/production-flow');if(!r.ok)throw Error('단계 제작 화면은 FrameStudio 서버 재시작 후 사용할 수 있습니다. 실행 중인 제작을 먼저 마치세요.');stop();location.href='/production-flow?id='+state.project.id}catch(e){notice(e.message,true)}};
document.querySelector('header').append(productionFlowButton);
const patchPanel=document.createElement('div');patchPanel.className='panel';document.body.append(patchPanel);
const referencePatch=new ReferencePatchView(patchPanel,data=>action('apply_reference_patch',data),notice,data=>action('apply_surface_shading',data));
const resultGuidePanel=document.createElement('section');resultGuidePanel.className='panel';$('checks').after(resultGuidePanel);
const resultGuideView=new ResultGuideView(resultGuidePanel,guard((name,data)=>action(name,data)));
const cutsceneButton=document.createElement('button');cutsceneButton.textContent='컷씬 영상·음향 편집';
cutsceneButton.onclick=()=>{if(!state.project)return notice('프로젝트를 먼저 선택하세요.',true);stop();location.href='/cutscene?id='+state.project.id};
document.querySelector('header').append(cutsceneButton);
const combatReviewButton=document.createElement('button');combatReviewButton.textContent='전투 연출 함께 재생';
combatReviewButton.onclick=()=>{stop();location.href='/combat-review?id='+encodeURIComponent(state.project?.id||'')};
document.querySelector('header').append(combatReviewButton);
const localMotionButton=document.createElement('button');localMotionButton.textContent='로컬 AI 모션(LTX) 화면';
localMotionButton.onclick=()=>{if(!state.project)return notice('원본 프로젝트를 먼저 선택하세요.',true);stop();location.href='/local-motion?id='+state.project.id};
document.querySelector('header').append(localMotionButton);
const methodPanel=document.createElement('section');methodPanel.className='panel';
methodPanel.innerHTML='<h2>제작방식</h2><label>현재 프로젝트 <select aria-label="제작방식"></select></label> <button>선택한 제작 화면 열기</button><p class="method-description"></p><p class="muted">프레임 작화의 이미지 엔진(local/Codex)과 별도 설정입니다. 선택만으로 생성하거나 기존 원화·시간표·승인을 변경하지 않습니다.</p>';
document.querySelector('header').after(methodPanel);
const methodSelect=methodPanel.querySelector('select'),methodOpen=methodPanel.querySelector('button');let methodPending=false;
methodSelect.onchange=guard(async()=>{if(methodPending)return;methodPending=true;methodSelect.disabled=true;methodOpen.disabled=true;try{await action('production_method',{method:methodSelect.value});notice('제작방식을 저장했습니다. 기존 원화와 시간표는 유지됩니다.');}finally{methodPending=false;renderProductionMethod();}});
methodOpen.onclick=guard(()=>{if(!state.project)throw Error('프로젝트를 먼저 선택하세요.');stop();const p=state.project,route=p.production_methods?.[p.production_method]?.route;if(route==='/local-motion')location.href=route+'?id='+p.id;else document.getElementById('generate').scrollIntoView({block:'center'});});
function renderProductionMethod(){
 const p=state.project,available=p?.production_methods;methodPanel.hidden=!available;if(!available)return;
 methodSelect.replaceChildren(...Object.entries(available).map(([key,value])=>new Option(value.label,key)));methodSelect.value=p.production_method;
 methodSelect.disabled=methodPending||!!p.active;methodOpen.disabled=methodPending;
 methodPanel.querySelector('.method-description').textContent=available[p.production_method].description;
 for(const id of ['generate','draft','batch','batchDraft','refineOne','refineAll'])$(id).disabled=p.production_method!=='frame_authoring'||methodPending;
}
const approvedLibrary=new ApprovedLibrary($('approvedLibrary'),JSON.parse($('approvedCatalog').textContent),guard(id=>{state.index=0;return load(id)}));
const companionReview=new CompanionReview($('companionReview'));
const linkedResources=new LinkedResourceView($('linkedResources'));
const clipWorkflow=new ClipWorkflow($('clipWorkflow'),async(name,data)=>{await action(name,data);const result=await api('/api/state?id='+state.project.id);state.projects=currentProjects(result.projects);render();},guard(id=>{state.index=0;return load(id)}));
const windView=new WindView($('windView'),settings=>api('/api/wind-preview',{id:state.project.id,settings}),settings=>action('wind_settings',{settings}));
const poseReview=new PoseReview($('poseReview'),index=>{stop();state.index=index;frame()},guard(async(name,data={})=>{await action(name,data);const result=await api('/api/state?id='+state.project.id);state.projects=currentProjects(result.projects);render();notice(name==='approve_clip'?'애니메이션과 전투 배치를 최종 승인했습니다. 캐릭터 준비 상태는 별도입니다.':'전투 배치 작업을 저장했습니다.')}),(x,y)=>clipWorkflow.setMuzzle(x,y));
const roles={front:'정면',back:'후면',left:'좌측면',right:'우측면',first:'게임시점 첫 프레임',hidden_ref:'숨은면 참고 (선택)'};
function notice(text,error=false){$('notice').textContent=text;$('notice').classList.toggle('error',error)}
async function api(path,data){const r=await fetch(path,data?{method:'POST',headers:{'Content-Type':'application/json','X-Studio-Token':state.token},body:JSON.stringify(data)}:{});const b=await r.json();if(!r.ok)throw Error(b.error);return b}
function guard(fn){return async(...a)=>{try{await fn(...a)}catch(e){notice(e.message,true)}}}
function stop(){clearTimeout(state.timer);state.timer=null;state.playing=false;$('play').textContent='재생'}
function currentProjects(projects){return projects.filter(p=>!p.name.startsWith('[구디자인 보존] '))}
async function load(id){stop();clearTimeout(state.poll);const b=await api('/api/state'+(id?'?id='+encodeURIComponent(id):''));state.token=b.token;$('engine').value=b.engine.selected;$('engine').disabled=b.engine.busy;b.projects=currentProjects(b.projects);state.project=b.project;state.projects=b.projects;$('projects').replaceChildren(...b.projects.map(p=>new Option(p.name,p.id)));if(id)$('projects').value=id;else if(b.projects.length)return load(b.projects[0].id);render();if(b.project?.active)poll()}
function poll(){clearTimeout(state.poll);state.poll=setTimeout(guard(async()=>{const b=await api('/api/state?id='+state.project.id);state.project=b.project;renderRequests();if(b.project.active)poll();else{render();notice('생성 작업이 끝났습니다. 결과 또는 오류를 확인하세요.')}}),3000)}
async function action(action,extra={}){if(!state.project)throw Error('프로젝트를 먼저 만드세요.');stop();const pid=state.project.id,result=await api('/api/action',{id:pid,action,...extra});if(state.project?.id!==pid)return result;state.project=result;render();if(state.project.active)poll();return state.project}
function img(el,asset){el.hidden=!asset;if(asset)el.src='/assets/'+asset;else el.removeAttribute('src')}
function frame(){const p=state.project;if(!p)return;state.index=Math.min(state.index,p.frames.length-1);referencePatch.render(p,state.index);resultGuideView.render(p,state.index);$('approveFrame').disabled=!!p.frames[state.index]?.result_guide_gate&&!p.frames[state.index].result_guide_gate.can_adopt;poseReview.showFrame(state.index);clipWorkflow.setFrame(state.index);windView.showFrame(state.index);const f=p.frames[state.index],v=f.versions.at(-1);$('guideStatus').textContent=f.guide?'동작 참고 등록됨 · 로컬 시험은 이 참고를 재작화합니다.':'동작 참고 없음';img($('result'),v?.asset);$('empty').hidden=!!v;$('frameLabel').textContent=`${state.index+1} / ${p.frames.length} · ${f.phase} · ${f.duration_ms}ms`;$('notes').value=f.notes;$('versions').replaceChildren(...f.versions.map((v,i)=>new Option(`버전 ${i+1} · ${v.method}`,i)));if(v)$('versions').value=f.versions.length-1;$('checks').textContent=v?JSON.stringify({미술승인:f.approved,검사:v.checks,앞프레임비교:f.comparison,전체연속검사:p.temporal_review},null,2):'아직 작화 결과가 없습니다.';[...$('frames').children].forEach((el,i)=>el.classList.toggle('selected',i===state.index))}
function renderRequests(){const p=state.project;$('batchStatus').textContent=p?.batch?`상태: ${{running:'생성 중',failed:'실패 · 이어하기 가능',cancelled:'중단됨',interrupted:'재시작됨',awaiting_key_approval:'주요 포즈 승인 대기',needs_review:'전체 결과 검토 대기'}[p.batch.state]||p.batch.state} · 이번 완료 ${p.batch.done.length} · 남음 ${p.batch.pending.length} · ${p.batch.error||''}`:'';$('requests').textContent=p?JSON.stringify(p.requests.slice(-10),null,2):'';if(p?.active)notice(`${p.active.frame+1}번 프레임 작화 중 · ${p.active.engine==='local'?'로컬 GPU':'Codex 구독'} · 자동 재시도 없음`)}
function render(){renderProductionMethod();renderTemporal();const p=state.project;if(!p){referencePatch.render(null,0);resultGuideView.render(null,0);return;}approvedLibrary.setProject(p);companionReview.setProject(p);linkedResources.setProject(p);clipWorkflow.setProject(p,state.projects);windView.setProject(p);poseReview.setProject(p);$('refinePrompt').value=p.refinement_prompt||'';$('refineSilhouette').checked=!!p.refinement_silhouette;$('refineWind').checked=p.wind_settings?.enabled??!!p.refinement_wind;$('refineWind').disabled=!!p.wind_settings;$('refineLag').disabled=!!p.wind_settings;$('refineLockEndpoints').checked=!!p.refinement_lock_endpoints;$('refineLag').value=p.refinement_lag||0;$('refineStatus').textContent=p.refinement_mask?'수정 영역 등록됨':'수정 영역 미등록';for(const k of ['identity','motion','hidden'])$(k).value=p[k];img($('reference'),p.refs.first);$('readiness').textContent=p.character_approved?'캐릭터 준비 승인됨':p.missing.length?'등록 필요: '+p.missing.join(', '):'자료 준비 완료 · 미술 확인 후 승인하세요.';
 $('refs').replaceChildren(...Object.entries(roles).map(([role,label])=>{const d=document.createElement('div');d.className='ref';const im=document.createElement('img');im.alt=label;img(im,p.refs[role]);const l=document.createElement('label');l.textContent=label;const input=document.createElement('input');input.type='file';input.accept='image/png';input.onchange=guard(async()=>{if(input.files[0])await upload(input.files[0],{target:'reference',role})});l.append(input);d.append(im,l);return d}));
 $('frames').replaceChildren(...p.frames.map((f,i)=>{const b=document.createElement('button');b.className='frame'+((f.approved||p.clip_approved)?' approved':'')+(f.versions.at(-1)?.checks.blocking.length?' warning':'');b.textContent=`${i+1} ${f.kind==='key'?'주요':'중간'} ${f.versions.length?'●':'○'}`;b.onclick=()=>{stop();state.index=i;frame()};return b}));
 $('reviewDownloads').replaceChildren();if(p.review_export){for(const [k,label] of [['apng','원본 크기 무손실 재생'],['gif','미승인 반복 GIF'],['sheet','검토 시트'],['zip','검토 묶음']]){if(!p.review_export[k])continue;const a=document.createElement('a');a.href='/assets/'+p.review_export[k];a.textContent=(p.clip_approved&&k==='gif'?'승인한 반복 GIF':label)+' ';a.target='_blank';$('reviewDownloads').append(a)}}drawSchedule(p.frames);renderRequests();frame();$('downloads').replaceChildren();if(p.export&&p.export.status!=='stale'){for(const [k,label] of [['apng','원본 크기 무손실 재생'],['gif','재생 GIF'],['sheet','PNG 시트'],['zip','프레임 묶음']]){if(!p.export[k])continue;const a=document.createElement('a');a.href='/assets/'+p.export[k];a.textContent=label;a.target='_blank';$('downloads').append(a)}}
}
function drawSchedule(rows){$('schedule').replaceChildren(...rows.map((r,i)=>{const d=document.createElement('div');d.className='row';const n=document.createElement('span');n.textContent=i+1;const phase=document.createElement('input');phase.value=r.phase;phase.setAttribute('aria-label',`${i+1}번 단계`);const ms=document.createElement('input');ms.type='number';ms.value=r.duration_ms;ms.step=10;ms.setAttribute('aria-label',`${i+1}번 시간`);const kind=document.createElement('select');kind.append(new Option('주요','key'),new Option('중간','between'));kind.value=r.kind;const del=document.createElement('button');del.textContent='×';del.onclick=()=>d.remove();d.dataset.notes=r.notes||'';d.append(n,phase,ms,kind,del);return d}))}
function schedule(){return [...$('schedule').children].map(d=>({phase:d.children[1].value,duration_ms:Number(d.children[2].value),kind:d.children[3].value,notes:d.dataset.notes}))}
async function upload(file,target){if(file.size>24*1024*1024)throw Error('24MB 이하 PNG만 가능합니다.');const url=await new Promise((res,rej)=>{const r=new FileReader();r.onload=()=>res(r.result);r.onerror=rej;r.readAsDataURL(file)});await action('upload',{...target,png:url.split(',')[1]});notice('PNG 등록 완료. 원본은 새 버전으로 보존됩니다.')}
$('engine').onchange=guard(async()=>{try{await api('/api/engine',{selected:$('engine').value});$('connection').textContent='연결 미확인';}finally{await load(state.project?.id)}});$('draft').onclick=guard(()=>action('draft',{index:state.index}));
$('batch').onclick=guard(()=>action('batch'));$('batchDraft').onclick=guard(()=>action('batch_draft'));$('reviewExport').onclick=guard(()=>action('review_export'));
$('create').onclick=guard(async()=>{const p=await api('/api/create',{name:$('newName').value});state.index=0;await load(p.id);notice('새 프로젝트를 만들었습니다.')});$('projects').onchange=guard(()=>{state.index=0;return load($('projects').value)});$('refresh').onclick=guard(()=>load(state.project?.id));
$('save').onclick=guard(async()=>{await action('save',Object.fromEntries(['identity','motion','hidden'].map(k=>[k,$(k).value])));notice('제작 기준을 저장했습니다.')});$('approveCharacter').onclick=guard(async()=>{await action('approve_character');notice('캐릭터 준비 승인 기록 완료.')});
$('addRow').onclick=()=>drawSchedule([...schedule(),{phase:'복귀',duration_ms:120,kind:'key'}]);$('setSchedule').onclick=guard(()=>action('schedule',{rows:schedule()}));$('prev').onclick=()=>{if(state.project){stop();state.index=(state.index+state.project.frames.length-1)%state.project.frames.length;frame()}};$('next').onclick=()=>{if(state.project){stop();state.index=(state.index+1)%state.project.frames.length;frame()}};
$('play').onclick=()=>{if(state.playing)return stop();if(!state.project)return;state.playing=true;$('play').textContent='정지';const tick=()=>{frame();state.timer=setTimeout(()=>{state.index=(state.index+1)%state.project.frames.length;tick()},$('speed').value==='100ms'?100:state.project.frames[state.index].duration_ms/Number($('speed').value))};tick()};$('small').onchange=()=>document.querySelectorAll('.stage').forEach(e=>e.classList.toggle('small',$('small').checked));
$('saveNotes').onclick=guard(()=>action('notes',{index:state.index,notes:$('notes').value}));$('request').onclick=guard(()=>action('request',{index:state.index}));$('generate').onclick=guard(async()=>{await action('notes',{index:state.index,notes:$('notes').value});await action('generate',{index:state.index})});$('cancel').onclick=guard(()=>action('cancel'));
$('importGuide').onchange=guard(async()=>{if($('importGuide').files[0])await upload($('importGuide').files[0],{target:'guide',index:state.index});$('importGuide').value=''});$('importFrame').onchange=guard(async()=>{if($('importFrame').files[0])await upload($('importFrame').files[0],{target:'frame',index:state.index});$('importFrame').value=''});$('approveFrame').onclick=guard(()=>action('approve_frame',{index:state.index}));$('restore').onclick=guard(()=>action('restore',{index:state.index,version:Number($('versions').value)}));$('inspect').onclick=guard(()=>action('inspect'));$('export').onclick=guard(()=>action('export'));$('exportApproved').onclick=guard(()=>action('export',{approved_only:true}));$('check').onclick=guard(async()=>{const b=await api('/api/connection');$('connection').textContent=b.message;notice(b.message,!b.ready)});window.addEventListener('pagehide',()=>{stop();clearTimeout(state.poll);windView.dispose();companionReview.dispose();resultGuideView.dispose()});guard(()=>load(new URLSearchParams(location.search).get('id')))();

$('saveRefine').onclick=guard(async()=>{const file=$('refineMask').files[0];let png;if(file){if(file.size>24*1024*1024)throw Error('24MB 이하 PNG만 가능합니다.');const data=await new Promise((resolve,reject)=>{const r=new FileReader();r.onload=()=>resolve(r.result);r.onerror=reject;r.readAsDataURL(file)});png=data.split(',')[1]}await action('refinement_settings',{prompt:$('refinePrompt').value,png,wind:$('refineWind').checked,silhouette:$('refineSilhouette').checked,lock_endpoints:$('refineLockEndpoints').checked,lag:Number($('refineLag').value)});$('refineMask').value='';notice('국소 수정 기준을 저장했습니다.')});
$('refineOne').onclick=guard(()=>action('refine',{index:state.index}));
$('refineAll').onclick=guard(()=>action('refine_all'));

$('cleanEdges').onclick=guard(()=>action('clean_edges',{index:state.index}));
$('cleanEdgesAll').onclick=guard(()=>action('clean_edges_all'));

$('detailOne').onclick=guard(()=>action('finish_detail',{index:state.index}));
$('detailAll').onclick=guard(()=>action('finish_detail_all'));

$('retime').onclick=guard(async()=>{await action('retime',{durations:schedule().map(r=>r.duration_ms)});notice('그림을 보존하고 시간을 변경했습니다. 반복 영상을 재출력해 확인하세요.')});

$('smoothAlpha').onclick=guard(()=>action('smooth_alpha_edge',{index:state.index}));

function renderTemporal(){
 const target=$('temporalResult');target.replaceChildren();
 const out=state.project?.temporal_output;if(!out)return; $('temporalReference').value=out.reference+1; if(out.landmarks)$('temporalLandmarks').value=JSON.stringify(out.landmarks);
 $('temporalMode').value=out.rig?'rigid':'flow';if(out.rig){$('rigContinuous').checked=!!out.rig.continuous_joint;$('temporalRig').value=JSON.stringify(out.rig);$('rigRadius').value=out.rig.joint_radius;const keys=out.rig.thumb_keys;const i=keys.findIndex((k,i)=>i>0&&k[1]!==keys[i-1][1]);if(i>0)$('rigThumbMs').value=keys[i][0]-keys[i-1][0];}
 const note=document.createElement('p');note.textContent=out.stale?'원본이 변경된 과거 보정 출력입니다. 다시 제작하세요.':'별도 보정 검토본 · 원본 프레임/승인 유지';target.append(note);
 for(const [label,key]of [['보정 APNG','apng'],['보정 GIF','gif'],['프레임 ZIP','zip']]){
  const a=document.createElement('a');a.href='/assets/'+out.outputs[key];a.textContent=label+' ';a.target='_blank';target.append(a);
 }
 const im=document.createElement('img');im.src='/assets/'+out.outputs.apng;im.style.maxWidth='320px';im.alt='보정 반복 검토';target.append(im);
}
$('temporalRun').onclick=guard(async()=>{
 const button=$('temporalRun');button.disabled=true;try{
 const file=$('temporalMask').files[0];let mask_png;
 if(file){const bytes=new Uint8Array(await file.arrayBuffer());let raw='';for(const byte of bytes)raw+=String.fromCharCode(byte);mask_png=btoa(raw);}
 notice('움직임 보간 중입니다. 완료까지 기다려 주세요.');
 let rig;if($('temporalMode').value==='rigid'){rig=JSON.parse($('temporalRig').value);rig.joint_radius=Number($('rigRadius').value);rig.continuous_joint=$('rigContinuous').checked;const duration=Number($('rigThumbMs').value);if(!Number.isFinite(duration)||duration<20||duration>160)throw Error('엄지 전환은 20~160ms입니다.');for(let i=1;i<rig.thumb_keys.length;i++)if(rig.thumb_keys[i][1]!==rig.thumb_keys[i-1][1])rig.thumb_keys[i-1][0]=rig.thumb_keys[i][0]-duration;}
 await action('temporal_review',{rig,landmarks:$('temporalLandmarks').value.trim()?JSON.parse($('temporalLandmarks').value):undefined,mask_png,reference:Number($('temporalReference').value)-1,step_ms:Number($('temporalStep').value),transition_ms:Number($('temporalTransition').value)});
 notice('원본을 보존하고 보정 검토 출력을 저장했습니다.');
 }finally{button.disabled=false;}
});
