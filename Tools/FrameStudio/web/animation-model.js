// Pose math matches animation_model.py. The draft owns all mutable scene state.
export const POSE={x:0,y:0,rotation:0,sx:1,sy:1,opacity:1,bend:0,asset:null,ease:'smooth'};
export function bendOffset(y,start,amount){return amount*Math.max(0,(y-start)/(1-start))**2;}
export function drawSurface(ctx,part,amount,paint){
  if(Math.abs(amount)<1e-8){paint();return;}
  const start=part.bend_start??.5;
  for(let band=0;band<96;band++){
    const y0=band*part.height/96,y1=(band+1)*part.height/96;
    const d0=bendOffset(y0/part.height,start,amount),d1=bendOffset(y1/part.height,start,amount);
    const shear=(d1-d0)/(y1-y0);
    ctx.save();ctx.transform(1,0,shear,1,d0-shear*y0,0);
    ctx.beginPath();ctx.rect(0,y0,part.width,y1-y0+.01);ctx.clip();
    paint();ctx.restore();
  }
}
export function samplePose(part,frame){
  let a=part.keys[0],b=a;
  for(const key of part.keys.slice(1)){if(frame<key.frame){b=key;break;}a=key;b=key;}
  const out={...POSE,...a};
  if(b.frame===a.frame||out.ease==='hold')return out;
  let t=Math.max(0,Math.min(1,(frame-a.frame)/(b.frame-a.frame)));
  if(out.ease==='smooth')t=t*t*(3-2*t);
  for(const k of ['x','y','rotation','sx','sy','opacity','bend'])out[k]+=((b[k]??POSE[k])-out[k])*t;
  return out;
}
export function multiply(a,b){return [
  a[0]*b[0]+a[2]*b[1],a[1]*b[0]+a[3]*b[1],a[0]*b[2]+a[2]*b[3],
  a[1]*b[2]+a[3]*b[3],a[0]*b[4]+a[2]*b[5]+a[4],a[1]*b[4]+a[3]*b[5]+a[5]];}
export function worlds(scene,frame){
  const parts=new Map(scene.parts.map(p=>[p.id,p])),result=new Map();
  function visit(id,trail=new Set()){
    if(result.has(id))return result.get(id);
    if(trail.has(id)||!parts.has(id))throw Error('관절 연결이 순환하거나 부모가 없습니다.');
    trail=new Set(trail).add(id);
    const p=parts.get(id),pose=samplePose(p,frame),r=pose.rotation*Math.PI/180,c=Math.cos(r),s=Math.sin(r);
    let matrix=[c*pose.sx,s*pose.sx,-s*pose.sy,c*pose.sy,pose.x,pose.y],opacity=p.visible?pose.opacity:0;
    if(p.parent){const parent=visit(p.parent,trail);matrix=multiply(parent.matrix,matrix);opacity*=parent.opacity;}
    const v={matrix,opacity,asset:pose.asset,bend:pose.bend};result.set(id,v);return v;
  }
  for(const id of parts.keys())visit(id);return result;
}
export class AnimationDraft {
  constructor(state){this.load(state);this.undoStack=[];this.redoStack=[];}
  load(state){this.scene=structuredClone(state.scene);this.revision=state.revision;this.savedScene=JSON.stringify(this.scene);this.dirty=false;}
  changed(){this.dirty=JSON.stringify(this.scene)!==this.savedScene;}
  change(fn){
    const previous=structuredClone(this.scene);
    try{fn(this.scene);worlds(this.scene,0);}
    catch(e){this.scene=previous;throw e;}
    this.undoStack.push(previous);if(this.undoStack.length>30)this.undoStack.shift();
    this.redoStack=[];this.changed();
  }
  key(id,frame,changes){
    this.change(scene=>{
      const p=scene.parts.find(p=>p.id===id);
      if(!p||frame<0||frame>=scene.frames)throw Error('파츠와 시각을 확인하세요.');
      const pose={...samplePose(p,frame),...changes,frame};
      if(Object.entries(pose).some(([k,v])=>['x','y','rotation','sx','sy','opacity','bend'].includes(k)&&!Number.isFinite(v)))throw Error('자세 숫자를 확인하세요.');
      const i=p.keys.findIndex(k=>k.frame===frame);
      if(i<0){if(p.keys.length>=120)throw Error('자세 키는120개 이하입니다.');p.keys.push(pose);}
      else p.keys[i]=pose;
      p.keys.sort((a,b)=>a.frame-b.frame);
    });
  }
  undo(){if(!this.undoStack.length)return;this.redoStack.push(this.scene);this.scene=this.undoStack.pop();this.changed();}
  redo(){if(!this.redoStack.length)return;this.undoStack.push(this.scene);this.scene=this.redoStack.pop();this.changed();}
}
