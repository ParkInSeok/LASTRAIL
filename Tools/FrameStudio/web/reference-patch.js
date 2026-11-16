/* View delegates one explicit command; server owns composition and validation. */
function patchContext(project,index){
 const f=project?.frames[index],v=f?.versions.at(-1);
 return v?{id:project.id,index,source_index:index,source_revision:project.revision,source_version:f.versions.length-1,source_asset:v.asset}:null;
}
async function patchRequest(context,current,read,donor,mask,reference,note){
 if(!context)throw Error('결과 프레임을 먼저 선택하세요.');
 const [donor_png,mask_png]=await Promise.all([read(donor),read(mask)]);
 if(JSON.stringify(context)!==JSON.stringify(current()))throw Error('파일을 읽는 동안 프레임이 바뀌었습니다. 다시 선택하세요.');
 return {...context,donor_png,mask_png,reference_asset:reference,generation_note:note};
}
async function surfaceRequest(context,current,read,mask,reference,json){
 if(!context)throw Error('결과 프레임을 먼저 선택하세요.');
 let plan;try{plan=JSON.parse(json);}catch{throw Error('면 대응 JSON을 확인하세요.');}
 if(!plan?.surface_map||typeof plan.reference_sha256!=='string')throw Error('기준 SHA와 surface_map이 필요합니다.');
 const mask_png=await read(mask);
 if(JSON.stringify(context)!==JSON.stringify(current()))throw Error('파일을 읽는 동안 프레임이 바뀌었습니다. 다시 선택하세요.');
 return {...context,mask_png,reference_asset:reference,reference_sha256:plan.reference_sha256,surface_map:plan.surface_map};
}
class ReferencePatchView{
 constructor(host,apply,notice,applySurface){
  this.host=host;this.apply=apply;this.notice=notice;this.context=null;this.busy=false;
  host.innerHTML='<h2>원화 참고 국소 교정</h2><p>원화를 참고해 만든 교정본과 흑백 마스크를 선택합니다. 현재 프레임과 같은 크기만 받으며, 검정 영역과 전체 투명도는 원본 그대로 보존합니다.</p><p data-part="target"></p><label>교정본 PNG <input data-part="donor" type="file" accept="image/png"></label><label>수정 영역 PNG <input data-part="mask" type="file" accept="image/png"></label><label>사용한 원화 <select data-part="reference"></select></label><label>실제 생성 참조·교정 방식 <textarea data-part="note" maxlength="4000"></textarea></label><button data-part="apply">선택 프레임에 새 버전 적용</button><p>자동 생성이나 미술 승인이 아닙니다. 경계 검수 후 기존 버전 선택·복원으로 되돌릴 수 있습니다.</p>';
  host.insertAdjacentHTML('beforeend','<h3>면 대응 · 명암 경계 고정 적용</h3><p>위 수정 영역 PNG와 기준 원화를 사용합니다. 대응점·삼각형·기준 SHA가 포함된 JSON으로 색을 옮기며 그림의 위치·투명도는 유지합니다. 자동 면 추적이나 미술 승인이 아닙니다.</p><label>면 대응 JSON<textarea data-part="surface-map" maxlength="100000"></textarea></label><button data-part="surface-apply">저장한 대응으로 명암 적용</button>');
  host.insertAdjacentHTML('beforeend','<p>가닥 시험: surface_map.strands에 id, columns(양쪽 경계와 명암 경계 이름), source_rows, target_rows를 넣습니다. 각 행은 뿌리→끝 순서, 같은 열은 같은 명암 경계입니다. 붓결 RGB는 행 진행 방향을 따라 전달됩니다. 수동 대응이며 새로 드러난 면·광택 변화는 별도 검수가 필요합니다.</p>');
  this.el=key=>host.querySelector(`[data-part="${key}"]`);
  this.surfaceClick=async()=>{if(this.busy||!applySurface)return;this.busy=true;this.el('surface-apply').disabled=true;this.el('apply').disabled=true;
   try{const read=file=>new Promise((resolve,reject)=>{if(!file)return reject(Error('수정 영역 PNG를 선택하세요.'));const reader=new FileReader();reader.onload=()=>resolve(reader.result.split(',')[1]);reader.onerror=()=>reject(Error('파일 읽기 실패'));reader.readAsDataURL(file);});
    const data=await surfaceRequest(this.context,()=>this.context,read,this.el('mask').files[0],this.el('reference').value,this.el('surface-map').value);
    await applySurface(data);this.notice('면 대응과 명암을 고정 적용했습니다. 경계와 동작을 비교하세요.');
   }catch(e){this.notice(e.message,true);}finally{this.busy=false;this.el('apply').disabled=!this.context;this.el('surface-apply').disabled=!this.context;}
  };this.el('surface-apply').addEventListener('click',this.surfaceClick);
  this.click=async()=>{if(this.busy)return;this.busy=true;this.el('apply').disabled=true;
   try{const read=file=>new Promise((resolve,reject)=>{if(!file)return reject(Error('교정본과 마스크를 선택하세요.'));const r=new FileReader();r.onload=()=>resolve(r.result.split(',')[1]);r.onerror=()=>reject(Error('파일 읽기 실패'));r.readAsDataURL(file);});
    const data=await patchRequest(this.context,()=>this.context,read,this.el('donor').files[0],this.el('mask').files[0],this.el('reference').value,this.el('note').value);
    await this.apply(data);this.notice('새 국소 교정 버전을 저장했습니다. 경계를 검수하세요.');
   }catch(e){this.notice(e.message,true);}finally{this.busy=false;this.el('apply').disabled=!this.context;this.el('surface-apply').disabled=!this.context;}
  };this.el('apply').addEventListener('click',this.click);
 }
 render(project,index){
  const next=patchContext(project,index);
  if(JSON.stringify(next)!==JSON.stringify(this.context)){
   this.context=next;this.el('donor').value='';this.el('mask').value='';
   this.el('reference').replaceChildren(...Object.entries(project?.refs||{}).filter(([,v])=>v).map(([role,asset])=>new Option(role,asset)));
  }
  this.el('target').textContent=next?`대상 ${index+1}번 · 버전 ${next.source_version+1}`:'프레임을 선택하세요.';
  this.el('apply').disabled=this.busy||!next;
  this.el('surface-apply').disabled=this.busy||!next;
 }
 destroy(){this.context=null;this.el('apply').removeEventListener('click',this.click);this.el('surface-apply').removeEventListener('click',this.surfaceClick);}
}
if(typeof module!=='undefined')module.exports={patchContext,patchRequest,surfaceRequest};
