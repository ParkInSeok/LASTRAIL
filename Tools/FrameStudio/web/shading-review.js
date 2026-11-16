/* Temporary selection belongs to this view; Studio owns evidence and approval. */
function shadingReviewView(panel,review,project,index,onSave){
 const section=document.createElement('details');section.open=true;
 const heading=document.createElement('summary');heading.textContent='명암 면·경계 대조 · 목표 승인본은 오른쪽';section.append(heading);panel.append(section);
 const rows=review.rows.filter(r=>r.index===index);
 if(!rows.length){section.append('이 단계의 대상 프레임을 선택하세요.');return}
 const select=document.createElement('select');select.setAttribute('aria-label','검수 부위');
 rows.forEach(r=>select.add(new Option(`${r.name} · ${r.passed?'통과':r.current?'미통과':'미검수/변경됨'}`,r.region)));
 const content=document.createElement('div');section.append(select,content);
 function show(){
  content.replaceChildren();const row=rows.find(r=>r.region===select.value),e=row.current?row.evidence:{};
  const note=document.createElement('p');note.textContent='양쪽에서 같은 부위를 드래그해 선택하세요. 아래 확대는 원본 픽셀을 표시합니다. 경계의 위치·폭·부드러움을 직접 비교하며 자동 품질 판정은 아닙니다.';content.append(note);
  if(!row.ready){content.append('현재 선화·밑색·명암과 합성본을 먼저 등록하세요.');return}
  const pair=document.createElement('div');pair.style.cssText='display:grid;grid-template-columns:1fr 1fr;gap:12px';content.append(pair);
  const frame=project.production_flow.frames[String(index)];
  const boxes={};
  for(const [key,title,asset] of [['candidate_box','승인 밑색에 명암처리',frame.composite.at(-1).asset],['target_box','목표 승인본',project.production_flow.references.reference_color.at(-1).asset]]){
   const wrap=document.createElement('div'),label=document.createElement('strong');label.textContent=title;
   const canvas=document.createElement('canvas'),zoom=document.createElement('canvas');
   canvas.style.cssText='width:100%;background:#262a33;touch-action:none;cursor:crosshair';zoom.style.cssText='width:100%;background:#262a33';zoom.width=480;zoom.height=240;
   wrap.append(label,canvas,zoom);pair.append(wrap);
   const inputs=['왼쪽','위','오른쪽','아래'].map((name,i)=>{
    const lab=document.createElement('label'),el=document.createElement('input');lab.textContent=name+' ';el.type='number';el.min=0;el.max=1;el.step=.001;el.style.width='75px';el.setAttribute('aria-label',title+' '+name);el.value=e[key]?.[i]??'';lab.append(el);wrap.append(lab);return el;
   });
   const img=new Image();let start=null;
   boxes[key]=()=>inputs.map(el=>el.value===''?null:Number(el.value));
   function draw(){
    if(!img.naturalWidth)return;const ctx=canvas.getContext('2d');ctx.clearRect(0,0,canvas.width,canvas.height);ctx.drawImage(img,0,0,canvas.width,canvas.height);
    const b=boxes[key](),z=zoom.getContext('2d');z.clearRect(0,0,480,240);
    if(b.some(v=>v===null)||!(b[2]>b[0]&&b[3]>b[1]))return;
    ctx.strokeStyle='#00ddff';ctx.lineWidth=2;ctx.strokeRect(b[0]*canvas.width,b[1]*canvas.height,(b[2]-b[0])*canvas.width,(b[3]-b[1])*canvas.height);
    const w=(b[2]-b[0])*img.width,h=(b[3]-b[1])*img.height,scale=Math.min(480/w,240/h);
    z.drawImage(img,b[0]*img.width,b[1]*img.height,w,h,(480-w*scale)/2,(240-h*scale)/2,w*scale,h*scale);
   }
   inputs.forEach(el=>el.oninput=draw);
   const point=ev=>{const r=canvas.getBoundingClientRect();return [Math.max(0,Math.min(1,(ev.clientX-r.left)/r.width)),Math.max(0,Math.min(1,(ev.clientY-r.top)/r.height))]};
   canvas.onpointerdown=ev=>{start=point(ev);canvas.setPointerCapture(ev.pointerId)};
   canvas.onpointermove=ev=>{if(!start)return;const end=point(ev),b=[Math.min(start[0],end[0]),Math.min(start[1],end[1]),Math.max(start[0],end[0]),Math.max(start[1],end[1])];inputs.forEach((el,i)=>el.value=b[i].toFixed(4));draw()};
   canvas.onpointerup=canvas.onpointercancel=()=>{start=null};
   img.onload=()=>{if(!canvas.isConnected)return;canvas.width=480;canvas.height=Math.round(480*img.height/img.width);draw()};
   img.onerror=()=>{wrap.append('원본 이미지 로드 실패 — 검수하지 마세요.')};img.src='/assets/'+asset;
  }
  const fields={};for(const key of review.fields){const label=document.createElement('label'),area=document.createElement('textarea');label.textContent=key;area.value=e.observations?.[key]||'';area.placeholder='위치·차이·허용 근거. 해당 없으면 이유';area.maxLength=4000;area.style.width='100%';label.append(area);content.append(label);fields[key]=area}
  const verdict=document.createElement('select');verdict.setAttribute('aria-label','명암 면 검수 판정');verdict.add(new Option('판정 선택',''));verdict.add(new Option('미통과 · 보정 필요','false'));verdict.add(new Option('통과 · 실제 대조 완료','true'));verdict.value=typeof e.passed==='boolean'?String(e.passed):'';
  const save=document.createElement('button');save.textContent='이 부위 비교 저장 · 사용자 승인 아님';
  save.onclick=()=>onSave(()=>{if(!verdict.value)throw Error('실제 대조 후 판정을 선택하세요.');return {index,region:row.region,basis:row.basis,candidate_box:boxes.candidate_box(),target_box:boxes.target_box(),observations:Object.fromEntries(Object.entries(fields).map(([k,v])=>[k,v.value])),passed:verdict.value==='true'}});
  content.append(verdict,save);
 }
 select.onchange=show;show();
}
