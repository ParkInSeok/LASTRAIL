/* View delegates commands; the server owns inspection and adoption decisions. */
class ResultGuideView {
 constructor(root,command){
  this.root=root;this.command=command;
  root.innerHTML='<h3>결과 가이드 검사 · 선택 프레임</h3><p data-status role="status"></p><ul data-reasons></ul><p data-version></p><pre data-measurements></pre><button data-inspect>현재 후보 검사</button><p>발은 승인 기준 좌표와 오차 0px로 검사합니다. 옷 윤곽은 자동 판정하지 않으며 시각 확인 전에는 채택을 막습니다.</p><label><input type="checkbox" data-confirm>현재 후보의 옷 윤곽·몸 회전·바람을 직접 확인했습니다.</label><label>시각 확인 근거<textarea data-note rows="2" maxlength="2000"></textarea></label><button data-review>이 후보의 옷 시각 확인 기록</button>';
  this.get=name=>root.querySelector('[data-'+name+']');
  this.get('inspect').onclick=()=>command('result_guide_inspect',{index:this.index});
  this.get('review').onclick=()=>command('result_guide_review',{index:this.index,token:this.token,confirmed:this.get('confirm').checked,note:this.get('note').value});
  this.get('confirm').onchange=()=>this.updateReview();
 }
 updateReview(){this.get('review').disabled=!this.reviewable||!this.get('confirm').checked;}
 render(project,index){
  const guide=project?.result_guides?.[String(index)],f=project?.frames[index],gate=f?.result_guide_gate,r=f?.result_guide_report;
  this.root.hidden=!guide;if(!guide)return;
  this.index=index;this.token=r?.token;this.reviewable=gate?.current&&gate.status==='unverified'&&r?.feet_status==='passed'&&!project.active;
  this.get('confirm').checked=false;this.get('confirm').disabled=!this.reviewable;this.get('note').value='';this.get('note').disabled=!this.reviewable;this.updateReview();
  this.get('inspect').disabled=!f.versions.length||!!project.active;
  this.get('status').textContent=gate?.can_adopt?'검사 조건 충족 · 미술 승인은 별도입니다.':'채택 차단 · '+(gate?.status==='failed'?'기준 미달':'미검증 또는 검사 만료');
  this.get('reasons').replaceChildren(...(gate?.reasons||[]).map(text=>{const li=document.createElement('li');li.textContent=text;return li}));
  this.get('version').textContent=`${index+1}번 · 기준 ${guide.version} · 옷 참고: ${guide.garment_source}`;
  this.get('measurements').textContent=r?JSON.stringify({기준해상도:guide.canvas,후보해상도:r.actual_canvas,발:r.feet,옷:r.garment_status,검사후보SHA256:r.candidate_sha256},null,2):'검사 기록 없음';
 }
 dispose(){for(const name of ['inspect','review'])this.get(name).onclick=null;this.get('confirm').onchange=null;}
}
