/* View-only review state. Approval and frame data remain owned by Studio. */
class PoseReview {
  constructor(root, onSelect, onAction, onMuzzle) {
    this.root=root; this.onSelect=onSelect; this.project=null;
    this.images=new Map(); this.pending=new Set(); this.epoch=0;
    this.anchor=[.59,.47]; this.height=114; this.background=null;this.showFX=true;this.onMuzzle=onMuzzle;
    root.innerHTML=`<h2>주요 자세 · 전투 크기 사전 검토</h2>
      <p class="muted">세 자세와 전투 배치를 검토한 뒤 최종 승인하세요. 캐릭터 준비 승인과 애니메이션 최종 승인은 별도로 관리합니다.</p>
      <div class="pose-cards"></div><div class="actions">
      <label>전투 배경 PNG/JPG <input class="pose-background" type="file" accept="image/png,image/jpeg"></label>
      <label>인물 높이(px) <input class="pose-height" type="number" min="24" max="600" value="114"></label>
      <button class="pose-default">기본 전투 배경</button><button class="pose-save">현재 배치 PNG 저장</button><button class="pose-clear">배경 지우기</button></div>
      <p class="muted">배경을 눌러 발 위치를 옮긴 뒤 배치를 저장하세요. 아래 반복 영상은 저장된 배치로 제작합니다.</p><div class="actions"><label><input class="pose-fx" type="checkbox" checked>원화 FX 표시</label><button class="pose-muzzle">총구 클릭 지정</button></div>
      <canvas class="pose-battle" width="700" height="440" aria-label="선택 프레임 전투 배치"></canvas><p class="pose-status muted"></p>
      <div class="actions"><button class="battle-persist">배치 저장</button><button class="battle-export">저장한 배치로 반복 영상 제작</button><button class="clip-approve">현재 애니메이션·배치 최종 승인</button></div>
      <p class="clip-status" role="status"></p><p class="battle-status muted"></p><img class="battle-playback" hidden alt="저장한 전투 배치 반복 영상" style="width:470px;max-width:100%"><div class="battle-downloads"></div>`;
    root.querySelector('.battle-persist').onclick=()=>{
      if(!this.background){this.status.textContent='전투 배경을 먼저 선택하세요.';return}
      const c=document.createElement('canvas');c.width=this.background.width;c.height=this.background.height;c.getContext('2d').drawImage(this.background,0,0);
      onAction('battle_settings',{png:c.toDataURL('image/png').split(',')[1],anchor:[...this.anchor],height:this.height});
    };
    root.querySelector('.battle-export').onclick=()=>onAction('battle_export');
    root.querySelector('.clip-approve').onclick=()=>onAction('approve_clip');
    root.querySelector('.pose-default').onclick=async()=>{const token=++this.epoch,im=new Image();im.src='/assets/b4261bb89ef6443c8ef3cfe8ee4b8418.png';try{await im.decode();if(token!==this.epoch)return;this.background=im;this.canvas.width=im.width;this.canvas.height=im.height;this.anchor=[553/im.width,790/im.height];this.markDirty();this.draw()}catch(e){this.status.textContent='기본 배경을 읽지 못했습니다.'}};
    root.querySelector('.pose-save').onclick=()=>{this.canvas.toBlob(blob=>{if(!blob)return;const url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=`battle-review-${this.project?.id||'preview'}-frame${(this.index??0)+1}.png`;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000)},'image/png')};
    this.cards=root.querySelector('.pose-cards'); this.canvas=root.querySelector('canvas');
    this.status=root.querySelector('.pose-status');
    root.querySelector('.pose-fx').onchange=e=>{this.showFX=e.target.checked;this.draw()};
    root.querySelector('.pose-muzzle').onclick=()=>{this.pickMuzzle=true;this.status.textContent='선택한 사격 프레임의 총구를 누르세요. 발 위치는 바뀌지 않습니다.';};
    this.canvas.onclick=e=>{
      const r=this.canvas.getBoundingClientRect(),x=(e.clientX-r.left)/r.width,y=(e.clientY-r.top)/r.height;
      if(this.pickMuzzle){
        const ref=this.images.get(this.asset(0));if(!ref?.box){this.status.textContent='원화 준비 후 총구를 지정하세요.';return}
        const [x0,y0,x1,y1]=ref.box,scale=this.height/(y1-y0),px=this.anchor[0]*this.canvas.width-(x0+x1)*.5*scale,py=this.anchor[1]*this.canvas.height-y1*scale;
        const u=(x*this.canvas.width-px)/(ref.im.width*scale),v=(y*this.canvas.height-py)/(ref.im.height*scale);
        if(u<0||u>1||v<0||v>1){this.status.textContent='인물 캔버스 안의 총구를 선택하세요.';return}
        this.pickMuzzle=false;this.onMuzzle?.(u,v);this.status.textContent='총구를 지정했습니다. 위 탄환 설정을 저장하세요.';return;
      }
      this.anchor=[x,y];this.markDirty();this.draw();
    };
    root.querySelector('.pose-height').onchange=e=>{this.height=Math.max(24,Math.min(600,Number(e.target.value)||114));e.target.value=this.height;this.markDirty();this.draw()};
    root.querySelector('.pose-background').onchange=async e=>{
      const file=e.target.files[0];if(!file)return;
      if(file.size>24*1024*1024){this.status.textContent='배경은 24MB 이하로 선택하세요.';return}
      const token=++this.epoch,url=URL.createObjectURL(file),im=new Image();
      try {im.src=url;await im.decode();if(token!==this.epoch)return;
        if(im.width*im.height>16000000)throw Error('배경이 너무 큽니다. 1600만 픽셀 이하로 선택하세요.');
        this.background=im;this.canvas.width=im.width;this.canvas.height=im.height;this.markDirty();this.draw();
      }catch(err){if(token===this.epoch)this.status.textContent=err.message}
      finally{URL.revokeObjectURL(url);e.target.value=''}
    };
    root.querySelector('.pose-clear').onclick=()=>{++this.epoch;this.background=null;this.canvas.width=700;this.canvas.height=440;this.markDirty();this.draw()};
  }
  markDirty(){this.dirty=true;this.root.querySelector('.clip-approve').disabled=true;this.root.querySelector('.battle-export').disabled=true;this.root.querySelector('.battle-status').textContent='저장하지 않은 배치 변경이 있습니다. 먼저 배치를 저장하세요.';}
  setProject(p) {
    if(this.project?.id!==p.id){++this.epoch;this.images.clear();this.pending.clear();this.background=null;this.canvas.width=700;this.canvas.height=440;this.savedKey=null;this.dirty=false;this.pickMuzzle=false;this.picks=[0,Math.min(1,p.frames.length-1),Math.floor((p.frames.length-1)/2)]}
    this.project=p;this.cards.replaceChildren();this.root.querySelector('.battle-export').disabled=!p.battle_settings;
    const cfg=p.battle_settings,key=JSON.stringify([p.id,cfg]);
    if(cfg&&key!==this.savedKey){
      this.savedKey=key;this.dirty=false;this.anchor=[...cfg.anchor];this.height=cfg.height;this.root.querySelector('.pose-height').value=this.height;
      const token=++this.epoch,im=new Image();im.src='/assets/'+cfg.background;
      im.decode().then(()=>{if(token!==this.epoch)return;this.background=im;this.canvas.width=im.width;this.canvas.height=im.height;this.draw()}).catch(()=>{if(token===this.epoch)this.status.textContent='저장된 배경을 읽지 못했습니다.'});
    }
    this.root.querySelector('.clip-status').textContent=p.clip_approved?'애니메이션·전투 배치 최종 승인 완료':p.clip_approval?'변경된 결과 · 최종 재승인 필요':'최종 미술 승인 대기';
    this.root.querySelector('.clip-approve').disabled=!p.battle_current||p.clip_approved;
    this.root.querySelector('.battle-status').textContent=p.battle_current?'현재 버전 전투 배치 · 다른 요원은 배경 속 정지 이미지 · 게임 적용 아님':p.battle_export?'이전 버전 배치입니다. 현재 결과로 반복 영상을 다시 제작하세요.':'배치 저장 후 반복 영상을 제작하세요.';
    const preview=this.root.querySelector('.battle-playback'),links=this.root.querySelector('.battle-downloads');links.replaceChildren();preview.hidden=!p.battle_current;
    if(p.battle_current){preview.src='/assets/'+p.battle_export.apng;for(const [k,label] of [['apng','전투 원본 재생'],['gif','전투 GIF'],['zip','전투 프레임·독립 FX 묶음'],['fx_manifest','발사 시간표']]){if(!p.battle_export[k])continue;const a=document.createElement('a');a.href='/assets/'+p.battle_export[k];a.textContent=label;a.target='_blank';links.append(a)}}else preview.removeAttribute('src');
    if(this.dirty)this.markDirty();
    ['준비 / 기준','중간','동작 정점'].forEach((label,slot)=>{
      const card=document.createElement('div'),title=document.createElement('label'),select=document.createElement('select'),button=document.createElement('button'),im=document.createElement('img'),status=document.createElement('p');
      card.className='pose-card';title.textContent=label;im.alt=label;status.className='muted';
      select.setAttribute('aria-label',label+' 프레임');
      select.append(...p.frames.map((f,i)=>new Option(`${i+1} · ${f.phase}`,i)));
      this.picks[slot]=Math.min(this.picks[slot],p.frames.length-1);select.value=this.picks[slot];
      const refresh=()=>{const f=p.frames[this.picks[slot]],asset=f.versions.at(-1)?.asset||f.guide||(this.picks[slot]===0?p.refs.first:null);
        im.hidden=!asset;if(asset)im.src='/assets/'+asset;else im.removeAttribute('src');
        status.textContent=asset?(f.versions.length?((f.approved||p.clip_approved)?'승인된 결과':'미승인 결과'):'참고 원화 · 결과 아님'):'아직 그림 없음';button.disabled=!asset};
      select.onchange=()=>{this.picks[slot]=Number(select.value);refresh();this.onSelect(this.picks[slot])};
      button.onclick=()=>this.onSelect(this.picks[slot]);button.append(im);title.append(select);card.append(title,button,status);this.cards.append(card);refresh();
    });
  }
  showFrame(index){this.index=index;this.draw()}
  asset(index){const p=this.project,f=p?.frames[index];return f?.versions.at(-1)?.asset||f?.guide||(index===0?p?.refs.first:null)}
  load(asset){
    if(!asset||this.images.has(asset)||this.pending.has(asset))return;
    const pid=this.project.id;this.pending.add(asset);const im=new Image();im.src='/assets/'+asset;
    im.decode().then(()=>{if(this.project?.id!==pid)return;
      const c=document.createElement('canvas');c.width=im.width;c.height=im.height;const ctx=c.getContext('2d',{willReadFrequently:true});ctx.drawImage(im,0,0);
      const d=ctx.getImageData(0,0,im.width,im.height).data;let x0=im.width,y0=im.height,x1=-1,y1=-1;
      for(let y=0;y<im.height;y++)for(let x=0;x<im.width;x++)if(d[(y*im.width+x)*4+3]>128){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y)}
      this.images.set(asset,{im,box:x1>=0?[x0,y0,x1+1,y1+1]:null});this.pending.delete(asset);this.draw();
    }).catch(()=>{if(this.project?.id===pid){this.pending.delete(asset);this.status.textContent='프레임 이미지를 읽지 못했습니다. 새로고침 후 확인하세요.'}});
  }
  draw(){
    const ctx=this.canvas.getContext('2d'),w=this.canvas.width,h=this.canvas.height;
    ctx.fillStyle='#59615e';ctx.fillRect(0,0,w,h);if(this.background)ctx.drawImage(this.background,0,0,w,h);
    const asset=this.asset(this.index??0),base=this.asset(0);this.load(asset);this.load(base);
    const item=this.images.get(asset),ref=this.images.get(base);
    if(!item||!ref){this.status.textContent=asset?'이미지 준비 중…':'선택 프레임에 그림이 없습니다.';return}
    if(!ref.box){this.status.textContent='첫 프레임에 표시할 인물이 없습니다.';return}
    const [x0,y0,x1,y1]=ref.box,scale=this.height/(y1-y0);
    ctx.drawImage(item.im,this.anchor[0]*w-(x0+x1)/2*scale,this.anchor[1]*h-y1*scale,item.im.width*scale,item.im.height*scale);
    const fx=this.project?.frames[this.index??0]?.fx_asset;
    if(this.showFX&&fx){this.load(fx);const layer=this.images.get(fx);if(layer)ctx.drawImage(layer.im,this.anchor[0]*w-(x0+x1)/2*scale,this.anchor[1]*h-y1*scale,layer.im.width*scale,layer.im.height*scale)}
    this.status.textContent=`${(this.index??0)+1}번 · 첫 프레임 인물 높이 ${this.height}px 기준 · 모든 프레임 동일 배율/접지 기준 · 참고 배치 (게임 적용 아님)`;
  }
}
