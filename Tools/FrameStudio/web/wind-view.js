/* Wind settings view: backend owns profiles and generation rules; no pixel transforms. */
class WindView {
  constructor(root,onPreview,onSave){
    this.root=root;this.onPreview=onPreview;this.onSave=onSave;this.epoch=0;this.requestId=0;this.index=0;
    root.innerHTML=`<h2>바람 세기 · 부위별 뒤따름</h2><p class="muted">승인 원화의 평상시 바람에 더할 돌풍과 복귀를 정합니다. 아래 곡선은 작화 가이드이며 현재 PNG는 그대로입니다.</p>
      <label><input class="wind-enabled" type="checkbox">바람 작화 기준 사용</label>
      <div class="actions"><label class="wind-strength-label">추가 돌풍 세기 <output class="wind-strength-value">50</output><input class="wind-strength" type="range" min="0" max="120" step="1" value="50"></label><button data-wind-strength="25">약하게</button><button data-wind-strength="50">보통</button><button data-wind-strength="80">강하게</button><button data-wind-strength="120">강 120</button></div>
      <div class="actions"><label>열차 진행 방향 (화면 기준)<select class="wind-forward"><option value="-90">위로 ↑</option><option value="0">오른쪽 →</option><option value="90">아래로 ↓</option><option value="180">왼쪽 ←</option><option value="custom">직접 각도 지정</option></select></label><label>진행 각도 (오른쪽 0°, 위 -90°)<input class="wind-angle" type="number" min="-180" max="180" step=".1" value="-90"></label><label>보조 잔흔들림 (0~0.25)<input class="wind-flutter" type="number" min="0" max=".25" step=".01" value=".12"></label></div>
      <p class="muted">바람은 열차 진행의 반대 방향입니다. 게임 방향 번호로 풍향을 자동 추측하지 않습니다.</p>
      <div class="wind-parts"></div><div class="actions"><label>이번 재작화 대상<select class="wind-region"><option value="hair">머리</option><option value="ribbon">리본</option><option value="coat">코트</option><option value="mixed">여러 부위 · 마스크 안</option></select></label><button class="wind-save">바람 기준 저장</button></div>
      <canvas class="wind-curve" width="600" height="260" aria-label="프레임 시간에 따른 바람 세기 곡선"></canvas><p class="wind-frame muted"></p><p class="wind-status" role="status"></p>
      <p class="muted">저장 후 요청서 만들기 또는 재작화를 실행해야 그림에 반영됩니다. 부위 선택은 마스크를 자동으로 만들지 않습니다. 머리 길이·접속면·가림·발은 유지하고 자세를 새로 그리도록 지시합니다.</p>
      <p class="muted">제작 순서: 기준 → 최대 바람 → 끝가닥이 뒤따르는 복귀 자세를 먼저 비교하세요. 가닥별 회전·굽힘으로 실제 흔들림 폭을 확인한 뒤 양쪽 자세를 참조해 중간을 그립니다. 강도 120 저장만으로 작화 완료가 되지 않습니다. 기존 속도를 유지하고 전체 인접 전환·마지막→처음, 뿌리·가닥 겹침·선 농도를 검수하세요. 이 안내는 자동 품질 판정이 아닙니다.</p>`;
    this.q=selector=>root.querySelector(selector);
    for(const [part,label] of [['hair','머리'],['ribbon','리본'],['coat','코트']]){
      const row=document.createElement('div');row.className='wind-part';row.innerHTML=`<strong>${label}</strong><label>반응 배율 (0~2)<input class="wind-${part}-gain" type="number" min="0" max="2" step=".1"></label><label>뒤따름 (주기 비율 0~0.4)<input class="wind-${part}-delay" type="number" min="0" max=".4" step=".01"></label>`;this.q('.wind-parts').append(row);
    }
    root.querySelectorAll('input,select').forEach(input=>input.addEventListener('input',()=>{
      if(input===this.q('.wind-strength'))this.q('.wind-enabled').checked=true;
      if(input===this.q('.wind-forward')&&input.value!=='custom')this.q('.wind-angle').value=input.value;
      this.refreshDirection();this.schedule();
    }));
    root.querySelectorAll('[data-wind-strength]').forEach(button=>button.onclick=()=>{this.q('.wind-strength').value=button.dataset.windStrength;this.q('.wind-enabled').checked=true;this.schedule()});
    this.q('.wind-save').onclick=async()=>{
      if(!this.project||this.saving)return;
      const epoch=this.epoch;this.saving=true;this.q('.wind-save').disabled=true;
      try{await onSave(this.settings());if(epoch===this.epoch)this.q('.wind-status').textContent='바람 기준을 저장했습니다. 현재 PNG는 보존되며 다음 재작화 요청부터 반영됩니다.'}
      catch(error){if(epoch===this.epoch)this.q('.wind-status').textContent=error.message}
      finally{this.saving=false;this.q('.wind-save').disabled=false}
    };
  }
  settings(){
    const parts={};for(const part of ['hair','ribbon','coat'])parts[part]={gain:Number(this.q(`.wind-${part}-gain`).value),delay:Number(this.q(`.wind-${part}-delay`).value)};
    return {enabled:this.q('.wind-enabled').checked,strength:Number(this.q('.wind-strength').value),forward_angle:Number(this.q('.wind-angle').value),flutter:Number(this.q('.wind-flutter').value),region:this.q('.wind-region').value,parts};
  }
  refreshDirection(){const angle=Number(this.q('.wind-angle').value);this.q('.wind-forward').value=[-90,0,90,180].includes(angle)?String(angle):'custom';}
  schedule(){
    this.q('.wind-strength-value').textContent=this.q('.wind-strength').value;this.q('.wind-status').textContent='저장하지 않은 바람 기준 · 가이드 확인 중…';
    clearTimeout(this.timer);const id=++this.requestId,epoch=this.epoch;
    this.timer=setTimeout(async()=>{
      try{const preview=await this.onPreview(this.settings());if(id!==this.requestId||epoch!==this.epoch)return;this.preview=preview;this.draw();this.q('.wind-status').textContent='저장하지 않은 바람 기준 · PNG는 변경되지 않았습니다.'}
      catch(error){if(id===this.requestId&&epoch===this.epoch)this.q('.wind-status').textContent=error.message}
    },180);
  }
  setProject(p){
    const changed=this.project?.id!==p.id,key=JSON.stringify([p.id,p.wind_settings,p.frames.map(frame=>frame.duration_ms)]);
    this.project=p;
    if(changed){++this.epoch;++this.requestId;clearTimeout(this.timer);this.savedKey=null;this.preview=null;this.index=0;}
    if(key!==this.savedKey){
      ++this.requestId;clearTimeout(this.timer);
      this.savedKey=key;const cfg=p.wind_settings||p.wind_defaults;
      if(!cfg)return;
      this.q('.wind-enabled').checked=cfg.enabled;
      this.q('.wind-strength').value=cfg.strength;this.q('.wind-strength-value').textContent=cfg.strength;
      this.q('.wind-angle').value=cfg.forward_angle;this.q('.wind-flutter').value=cfg.flutter;this.q('.wind-region').value=cfg.region;this.refreshDirection();
      for(const part of ['hair','ribbon','coat'])for(const field of ['gain','delay'])this.q(`.wind-${part}-${field}`).value=cfg.parts[part][field];
      this.preview=p.wind_preview||null;this.q('.wind-status').textContent=p.wind_settings?'저장된 바람 기준 · 현재 PNG를 변형하지 않습니다.':'바람 세기 기준 미등록 · 기존 흩날림 설정 유지';
      if(!this.preview){const epoch=this.epoch,id=++this.requestId;this.onPreview(cfg).then(preview=>{if(epoch===this.epoch&&id===this.requestId){this.preview=preview;this.draw()}}).catch(error=>{if(epoch===this.epoch)this.q('.wind-status').textContent=error.message})}
      this.draw();
    }
  }
  showFrame(index){this.index=index;this.draw()}
  draw(){
    const canvas=this.q('.wind-curve'),ctx=canvas.getContext('2d'),w=canvas.width,h=canvas.height;
    ctx.fillStyle='#111b25';ctx.fillRect(0,0,w,h);
    if(!this.preview){this.q('.wind-frame').textContent='가이드 준비 중…';return}
    const preview=this.preview,left=48,top=58,right=w-20,bottom=h-42,active=preview.frames.at(-1).time_ms/Math.max(1,preview.duration_ms);
    ctx.font='12px sans-serif';ctx.strokeStyle='#324554';ctx.fillStyle='#afc1d0';
    for(let i=0;i<=4;i++){const y=bottom-(bottom-top)*i/4;ctx.beginPath();ctx.moveTo(left,y);ctx.lineTo(right,y);ctx.stroke();ctx.fillText(String(i*25)+'%',5,y+4)}
    const colors={hair:'#6cdbc6',ribbon:'#efcf70',coat:'#f38b93'},labels={hair:'머리',ribbon:'리본',coat:'코트'};
    for(const [i,part] of ['hair','ribbon','coat'].entries()){
      ctx.strokeStyle=colors[part];ctx.lineWidth=2;ctx.beginPath();
      preview.samples.forEach((sample,index)=>{const x=left+(right-left)*sample.phase*active,y=bottom-(bottom-top)*sample.parts[part].intensity;index?ctx.lineTo(x,y):ctx.moveTo(x,y)});ctx.lineTo(right,bottom);ctx.stroke();ctx.fillStyle=colors[part];ctx.fillText(labels[part],left+i*85,25);
    }
    const frame=preview.frames[Math.min(this.index,preview.frames.length-1)];
    if(frame){const x=left+(right-left)*frame.time_ms/Math.max(1,preview.duration_ms);ctx.strokeStyle='#fff';ctx.lineWidth=1;ctx.beginPath();ctx.moveTo(x,top);ctx.lineTo(x,bottom);ctx.stroke();
      this.q('.wind-frame').textContent=`${this.index+1}번 · ${frame.time_ms}ms · `+Object.entries(frame.parts).map(([name,value])=>`${labels[name]} ${Math.round(value.intensity*100)}%`).join(' / ')+` · 바람 방향 (${preview.wind.map(v=>v.toFixed(2)).join(', ')})`;}
    ctx.fillStyle='#afc1d0';ctx.fillText('0ms',left,bottom+25);ctx.fillText(preview.duration_ms+'ms',right-55,bottom+25);
    for(const [label,vector,x,color] of [['열차',preview.forward,420,'#85bfff'],['바람',preview.wind,535,'#efcf70']]){
      ctx.fillStyle=color;ctx.fillText(label,x-45,28);ctx.strokeStyle=color;ctx.lineWidth=2;const end=[x+vector[0]*17,25+vector[1]*17];ctx.beginPath();ctx.moveTo(x,25);ctx.lineTo(...end);ctx.lineTo(end[0]-vector[0]*6-vector[1]*4,end[1]-vector[1]*6+vector[0]*4);ctx.moveTo(...end);ctx.lineTo(end[0]-vector[0]*6+vector[1]*4,end[1]-vector[1]*6-vector[0]*4);ctx.stroke();
    }
  }
  dispose(){++this.epoch;++this.requestId;clearTimeout(this.timer)}
}
