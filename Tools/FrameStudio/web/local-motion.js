/* View/controller use the existing Studio state and one explicitly requested job. */
(() => {
  const $ = id => document.getElementById(id);
  class MotionController {
    constructor(){this.pid='';this.token='';this.state=null;this.timer=null;this.generation=0;this.busy=false;this.inflight=false;this.closed=false;}
    async api(path,body){const r=await fetch(path,{method:body?'POST':'GET',headers:body?{'Content-Type':'application/json','X-Studio-Token':this.token}:{},body:body?JSON.stringify(body):undefined});const data=await r.json();if(!r.ok)throw Error(data.error||'요청 실패');return data;}
    error(e){$('error').textContent=e.message;}
    async init(){
      const state=await this.api('/api/state');this.token=state.token;
      for(const p of state.projects){const o=document.createElement('option');o.value=p.id;o.textContent=p.name;$('project').append(o);}
      $('project').onchange=()=>this.select($('project').value).catch(e=>this.error(e));
      $('reference').onchange=()=>{this.showReference();$('approved').checked=false;};
      $('generate').onclick=()=>this.generate().catch(e=>this.error(e));
      $('cancel').onclick=()=>this.cancel().catch(e=>this.error(e));
      window.addEventListener('pagehide',()=>{this.closed=true;this.generation++;clearTimeout(this.timer);});
      window.addEventListener('pageshow',e=>{if(e.persisted){this.closed=false;if(this.pid)this.refresh().catch(error=>this.error(error));}});
      const id=new URLSearchParams(location.search).get('id');
      if(state.projects.length)await this.select(state.projects.some(p=>p.id===id)?id:state.projects[0].id);
    }
    async select(pid){
      clearTimeout(this.timer);const generation=++this.generation;this.pid=pid;this.busy=false;this.state=null;
      $('reference').replaceChildren();$('source').hidden=true;$('result').hidden=true;$('generate').disabled=true;
      $('project').value=pid;$('back').href='/?id='+pid;$('approved').checked=false;$('error').textContent='';
      const data=await this.api('/api/local-motion?id='+pid);if(generation!==this.generation)return;
      this.state=data;$('reference').replaceChildren();
      for(const ref of data.references){const o=document.createElement('option');o.value=ref.key;o.textContent=ref.label;$('reference').append(o);}
      this.showReference();this.render(data.job);
    }
    showReference(){const ref=this.state?.references.find(r=>r.key===$('reference').value);if(ref){$('source').src='/assets/'+ref.asset;$('source').hidden=false;}else{$('source').hidden=true;}}
    render(job){
      this.busy=!!job&&['queued','running','extracting','cancelling'].includes(job.status);
      $('generate').disabled=this.inflight||this.busy||!this.state?.references.length||this.state?.production_method==='frame_authoring';$('cancel').disabled=!this.busy;
      $('project').disabled=this.inflight||this.busy;$('reference').disabled=this.inflight||this.busy;
      $('status').textContent=job?job.message:'원화와 동작 설명을 선택해 생성하세요.';
      if(this.state?.production_method==='frame_authoring')$('status').textContent='현재 제작방식은 프레임 작화입니다. 돌아가기에서 로컬 AI 모션(LTX)을 명시적으로 선택해야 새 영상을 생성할 수 있습니다.';
      $('result').hidden=!(job?.status==='complete');
      if(job?.status==='complete'){
        $('usedSource').src='/assets/'+job.source.asset;$('preview').src='/assets/'+job.preview;$('sheet').src='/assets/'+job.sheet;
        $('candidate').href='/?id='+job.candidate_id;$('archive').href='/assets/'+job.archive;
        $('method').textContent=`연속 영상 ${job.result.source_frames}장에서 12장 추출 · 원본 영상 보존 · 투명도/미술 미승인`;
      }
      clearTimeout(this.timer);if(this.busy&&!this.closed)this.timer=setTimeout(()=>this.refresh().catch(e=>this.error(e)),2000);
    }
    async refresh(){const generation=this.generation;const data=await this.api('/api/local-motion?id='+this.pid);if(generation!==this.generation||this.closed)return;this.state.revision=data.revision;this.state.production_method=data.production_method;this.render(data.job);}
    async generate(){
      if(this.inflight||this.busy)return;
      if(this.state?.production_method==='frame_authoring')throw Error('제작방식을 로컬 AI 모션(LTX)으로 먼저 선택하세요.');
      if(!$('approved').checked)throw Error('선택한 원화의 승인을 확인하세요.');
      if(!$('prompt').value.trim())throw Error('움직임 설명을 입력하세요.');
      this.inflight=true;$('generate').disabled=true;$('project').disabled=true;$('error').textContent='';
      try{const r=await this.api('/api/local-motion?id='+this.pid,{action:'generate',revision:this.state.revision,reference:$('reference').value,approved_reference:true,prompt:$('prompt').value,seed:Number($('seed').value),profile:$('profile').value});this.inflight=false;this.render(r.job);}
      finally{this.inflight=false;$('generate').disabled=this.busy||this.state?.production_method==='frame_authoring';$('project').disabled=this.busy;}
    }
    async cancel(){const r=await this.api('/api/local-motion?id='+this.pid,{action:'cancel'});this.render(r.job);}
  }
  new MotionController().init().catch(e=>{$('error').textContent=e.message;});
})();
