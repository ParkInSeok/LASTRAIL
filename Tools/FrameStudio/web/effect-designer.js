/* FX view emits one command; generation, validation and storage belong to Studio. */
class EffectDesignView {
  constructor(root,onCreate){
    this.root=root;this.onCreate=onCreate;this.key=null;
    root.innerHTML=`<h3>탄환 · 명중 효과 제작</h3>
      <p class="muted">모양과 색을 골라 독립 효과를 만듭니다. 물방울탄과 명중 크기 2.2가 레이나 승인 기준입니다. 카일 승인 청록탄과 카일 명중 모양을 선택할 수 있습니다. 카일 원본 탄환은 색·발광 변경을 적용하지 않습니다. 다른 모양은 검토용 기본 형태입니다.</p>
      <div class="actions"><label>탄환 모양 <select class="design-shape"><option value="kyle">카일 승인 청록탄 · 원본 색 고정</option><option value="teardrop">짧은 물방울탄</option><option value="orb">압축 광탄</option><option value="ember">불씨 덩어리탄</option><option value="ring">고리형 에너지탄</option><option value="arcane">응축 마력탄</option></select></label><label>외곽 색 <input type="color" style="width:54px;height:36px;padding:3px" class="design-color" value="#ffb430"></label><label>중심 색 <input type="color" style="width:54px;height:36px;padding:3px" class="design-core" value="#ffffec"></label><label>탄환 발광 <input type="number" class="design-glow" min="0" max="4" step=".1" value="1.2"></label></div>
      <div class="actions"><label><input type="checkbox" class="design-impact-enabled" checked>명중 효과 사용</label><label>명중 모양 <select class="design-impact-pattern"><option value="reina">레이나 파편</option><option value="kyle">카일 청록 섬광 · 기본 크기 1</option></select></label><label>명중 크기 <input type="number" class="design-impact-scale" min=".5" max="3" step=".1" value="2.2"></label><label>명중 지속시간 (ms) <input type="number" class="design-impact-duration" min="40" max="1000" step="20" value="160"></label><label>명중 색 <input type="color" style="width:54px;height:36px;padding:3px" class="design-impact-color" value="#ffbf3d"></label><label>명중 중심 색 <input type="color" style="width:54px;height:36px;padding:3px" class="design-impact-core" value="#ffffef"></label><button class="design-create">효과 제작·저장</button></div>
      <p class="muted">위의 탄환 사용·발사 위치·시점으로 생성합니다. 명중은 비행이 끝난 지점에 표시됩니다. 저장 후 전투 반복 영상을 다시 제작하세요. 실제 적 충돌 판정은 아닙니다.</p>
      <div class="design-preview" hidden><div class="actions"><figure><img class="design-projectile" style="min-width:48px;image-rendering:auto" alt="제작한 탄환"><figcaption>탄환</figcaption></figure><figure><img class="design-impact" width="192" height="192" alt="명중 효과 반복 미리보기"><figcaption>명중 효과</figcaption></figure></div><a class="design-download" download>독립 PNG·설정 내려받기</a></div>`;
    this.q=s=>root.querySelector(s);
    this.q('.design-impact-pattern').onchange=()=>{if(this.q('.design-impact-pattern').value==='kyle'){this.q('.design-impact-scale').value=1;this.q('.design-impact-color').value='#5be9ff';this.q('.design-impact-core').value='#dfffff';this.q('.design-impact-duration').value=160;}};
    this.q('.design-create').onclick=()=>this.onCreate(this.read());
  }
  read(){return {impact_pattern:this.q('.design-impact-pattern').value,impact_color:this.q('.design-impact-color').value,impact_core:this.q('.design-impact-core').value,shape:this.q('.design-shape').value,color:this.q('.design-color').value,core:this.q('.design-core').value,glow:Number(this.q('.design-glow').value),impact_enabled:this.q('.design-impact-enabled').checked,impact_scale:Number(this.q('.design-impact-scale').value),impact_duration_ms:Number(this.q('.design-impact-duration').value)};}
  setProject(p){
    const key=JSON.stringify([p.id,p.effect_style,p.effect_assets]);if(key===this.key)return;this.key=key;
    const s=p.effect_style||{};
    for(const [key,value] of Object.entries({'impact-pattern':s.impact_pattern??'reina','impact-color':s.impact_color??'#ffbf3d','impact-core':s.impact_core??'#ffffef',shape:s.shape??'teardrop',color:s.color??'#ffb430',core:s.core??'#ffffec',glow:s.glow??1.2,'impact-scale':s.impact_scale??2.2,'impact-duration':s.impact_duration_ms??160}))this.q('.design-'+key).value=value;
    this.q('.design-impact-enabled').checked=s.impact_enabled??true;
    const a=p.effect_assets;this.q('.design-preview').hidden=!a;
    if(a){this.q('.design-projectile').src='/assets/'+a.projectile;this.q('.design-impact').src='/assets/'+a.impact_preview;this.q('.design-download').href='/assets/'+a.zip;}
    else{this.q('.design-projectile').removeAttribute('src');this.q('.design-impact').removeAttribute('src');this.q('.design-download').removeAttribute('href');}
  }
  destroy(){this.q('.design-impact-pattern').onchange=null;this.q('.design-create').onclick=null;this.onCreate=null;}
}
