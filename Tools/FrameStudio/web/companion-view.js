// Existing approved Eve drone renderer; dedicated tool review view.
const CONFIG={2:{pivot:[660,610],muzzle:[138,580],rear:[850,460],left:[485,415],right:[690,885],target:[-1800,1000]},4:{pivot:[626,650],muzzle:[625,220],rear:[628,640],left:[330,720],right:[918,720],target:[626,-2200]}};
function pose(direction,time,neutral=false,target=null,flightTime=time){
 const a={...CONFIG[direction],target:target??CONFIG[direction].target},phase=flightTime%3200/3200*Math.PI*2,offset=neutral?[0,0]:[Math.sin(phase)*55,Math.sin(phase)*18];
 const pivot=[a.pivot[0]+offset[0],a.pivot[1]+offset[1]],u=[a.muzzle[0]-a.rear[0],a.muzzle[1]-a.rear[1]],len=Math.hypot(...u);u[0]/=len;u[1]/=len;
 const q=[a.muzzle[0]-a.pivot[0],a.muzzle[1]-a.pivot[1]],v=[a.target[0]-pivot[0],a.target[1]-pivot[1]];
 const angle=neutral?0:Math.atan2(v[1],v[0])-Math.atan2(u[1],u[0])-Math.asin((u[0]*q[1]-u[1]*q[0])/Math.hypot(...v));
 const c=Math.cos(angle),s=Math.sin(angle),axis=[c*u[0]-s*u[1],s*u[0]+c*u[1]],age=time%800,recoil=neutral?0:age<160?10*Math.sin(age/160*Math.PI):0;
 const muzzle=[pivot[0]+c*q[0]-s*q[1]-axis[0]*recoil,pivot[1]+s*q[0]+c*q[1]-axis[1]*recoil];
 return{a,offset,pivot,angle,axis,muzzle,recoil,age,tilt:neutral?0:Math.cos(phase)*.018,pod:neutral?0:Math.sin(phase)*.028,flash:!neutral&&age<75};
}



const FX=Object.freeze({chargeScale:3.611111111111111,beamOuter:116.66666666666667,beamCore:36.111111111111114,beamOutline:152.77777777777777,beamGlow:26});
class DroneView{constructor(images){this.images=images;}
 drawPart(c,img,pivot,angle,reg=[1,0,0]){c.save();c.translate(...pivot);c.rotate(angle);c.translate(-pivot[0],-pivot[1]);c.translate(reg[1],reg[2]);c.scale(reg[0],reg[0]);c.drawImage(img,0,0);c.restore();}
 render(c,d,state,target){const p=pose(d,state.time,false,target,state.motionTime??state.time),im=this.images[d];c.save();c.save();c.translate(...p.offset);c.translate(...p.a.pivot);c.rotate(p.tilt);c.translate(-p.a.pivot[0],-p.a.pivot[1]);
 if(state.layers.left)this.drawPart(c,im.left,p.a.left,p.pod,d===4?[.8,5,115]:[1,0,0]);
 if(state.layers.body)c.drawImage(im.body,0,0);
 if(state.layers.right)this.drawPart(c,im.right,p.a.right,-p.pod);c.restore();
 if(state.layers.head){c.save();c.translate(...p.pivot);c.rotate(p.angle);const ax=p.a.muzzle[0]-p.a.rear[0],ay=p.a.muzzle[1]-p.a.rear[1],l=Math.hypot(ax,ay);c.translate(-ax/l*p.recoil,-ay/l*p.recoil);c.translate(-p.a.pivot[0],-p.a.pivot[1]);if(d===2){c.translate(38,12);c.scale(.8,.8);}else{c.translate(125.6,43);c.scale(.8,.8);}c.drawImage(im.head,0,0);c.restore();
 if(!state.neutral&&p.age>=560){const u=(p.age-560)/240,x=p.muzzle[0],y=p.muzzle[1];c.save();c.translate(x,y);c.scale(FX.chargeScale,FX.chargeScale);c.translate(-x,-y);c.shadowColor='#16212c';c.shadowBlur=5;const g=c.createRadialGradient(x,y,0,x,y,42+u*48);g.addColorStop(0,`rgba(255,255,220,${.4+u*.6})`);g.addColorStop(.3,`rgba(255,187,40,${.2+u*.5})`);g.addColorStop(1,'rgba(255,160,20,0)');c.fillStyle=g;c.beginPath();c.arc(x,y,42+u*48,0,Math.PI*2);c.fill();c.strokeStyle='#18202d';c.lineWidth=10+u*5;c.beginPath();c.arc(x,y,85*(1-u)+18,0,Math.PI*2);c.stroke();c.strokeStyle=`rgba(255,235,155,${.65+.35*u})`;c.lineWidth=5+u*5;c.beginPath();c.arc(x,y,85*(1-u)+18,0,Math.PI*2);c.stroke();for(let i=0;i<6;i++){const a=i*Math.PI/3+.2,rad=125*(1-u)+15;c.fillStyle='#fff3ba';c.beginPath();c.arc(x+Math.cos(a)*rad,y+Math.sin(a)*rad,7+u*5,0,Math.PI*2);c.fill();}c.fillStyle='#fffbdc';c.beginPath();c.arc(x,y,6+u*13,0,Math.PI*2);c.fill();c.restore();}
 if(!state.neutral&&p.flash){c.fillStyle='#fff0ad';c.beginPath();c.arc(...p.muzzle,18.055555555555557,0,Math.PI*2);c.fill();}
 if(!state.neutral&&p.age<120){c.save();c.globalAlpha=1-p.age/150;c.lineCap='round';c.beginPath();c.moveTo(...p.muzzle);c.lineTo(...p.a.target);c.strokeStyle='#242332';c.lineWidth=FX.beamOutline;c.stroke();c.strokeStyle='#ffb52e';c.lineWidth=FX.beamOuter;c.shadowColor='#ffbc38';c.shadowBlur=FX.beamGlow;c.stroke();c.shadowBlur=0;c.strokeStyle='#fff8d6';c.lineWidth=FX.beamCore;c.stroke();c.restore();}}

 c.restore();return p;}
}


const HAND_FX_SCALE=.75;
class GloveEffectView{
 render(c,time,[x,y]){
 c.save();c.translate(x,y);c.scale(HAND_FX_SCALE,HAND_FX_SCALE);c.translate(-x,-y);
 const age=time%800;
 if(age>=320&&age<560){const u=(age-320)/240;this.glow(c,x,y,9+u*7,.3+u*.35);c.save();for(let i=0;i<6;i++){const a=i*Math.PI/3+.3,r=23*(1-u)+3;c.fillStyle='#fff3ba';c.shadowColor='#ffb52e';c.shadowBlur=4*HAND_FX_SCALE;c.beginPath();c.arc(x+Math.cos(a)*r,y+Math.sin(a)*r,1.7+u,0,Math.PI*2);c.fill();}c.restore();}
 if(age>=560&&age<680){const u=(age-560)/120;c.save();c.globalAlpha=1-u;c.lineWidth=2;c.strokeStyle='#ffcd68';c.shadowColor='#ffb52e';c.shadowBlur=5*HAND_FX_SCALE;c.beginPath();c.ellipse(x,y,5+u*16,3+u*10,-.4,0,Math.PI*2);c.stroke();c.restore();}
 if(age>=560)this.glow(c,x,y,8,.25);
 if(age<120)this.glow(c,x,y,14,1-age/120);
 c.restore();
 }
 glow(c,x,y,r,a){c.save();const g=c.createRadialGradient(x,y,0,x,y,r);g.addColorStop(0,'rgba(255,248,214,'+a+')');g.addColorStop(.3,'rgba(255,181,46,'+a*.8+')');g.addColorStop(1,'rgba(255,181,46,0)');c.fillStyle=g;c.beginPath();c.arc(x,y,r,0,Math.PI*2);c.fill();c.restore();}
}

class CompanionReview {
 constructor(root){this.root=root;this.epoch=0;this.key='';this.time=0;this.playing=true;this.layers={body:true,head:true,left:true,right:true};this.fx=true;this.glove=true;
 root.innerHTML='<h2>드론·장갑·레이저 통합 재생</h2><p>현재 인물 프레임을 직접 사용합니다. 드론 파츠와 효과는 독립 표시됩니다. 인물 PNG 내보내기에는 포함되지 않습니다.</p><button class="run">일시정지</button><button class="fire">발사 확인</button><label><input type="checkbox" class="laser" checked>충전·레이저</label><label><input type="checkbox" class="glove" checked>장갑 효과</label><span class="parts"></span><canvas width="760" height="600" style="width:100%" aria-label="드론 통합 화면"></canvas><p class="status"></p>';
 this.c=root.querySelector('canvas').getContext('2d');this.status=root.querySelector('.status');this.abort=new AbortController();const on=(el,event,fn)=>el.addEventListener(event,fn,{signal:this.abort.signal});
 on(root.querySelector('.run'),'click',()=>{this.playing=!this.playing;root.querySelector('.run').textContent=this.playing?'일시정지':'재생'});on(root.querySelector('.fire'),'click',()=>{this.time=this.project.companion.timing.fire+30;this.playing=false;root.querySelector('.run').textContent='재생'});
 on(root.querySelector('.laser'),'change',e=>this.fx=e.target.checked);on(root.querySelector('.glove'),'change',e=>this.glove=e.target.checked);
 for(const[k,label]of Object.entries({body:'드론 본체',head:'포신',left:'왼 포드',right:'오른 포드'})){const l=document.createElement('label'),input=document.createElement('input');input.type='checkbox';input.checked=true;l.append(input,document.createTextNode(label));root.querySelector('.parts').append(l);on(input,'change',e=>this.layers[k]=e.target.checked);}
 let last=performance.now();const tick=now=>{if(this.project&&this.ready){if(this.playing)this.time=(this.time+Math.min(100,now-last))%this.total;this.draw();}last=now;this.raf=requestAnimationFrame(tick)};this.raf=requestAnimationFrame(tick);
 }
 async setProject(p){const key=JSON.stringify([p.id,p.companion,p.frames.map(f=>[f.versions.at(-1)?.asset,f.duration_ms])]);if(key===this.key)return;this.key=key;this.project=p;this.ready=false;const epoch=++this.epoch;this.root.hidden=!p.companion;if(!p.companion)return;
 this.status.textContent='통합 리소스 불러오는 중';this.time=0;this.total=p.frames.reduce((n,f)=>n+f.duration_ms,0);
 const load=async name=>{if(!name)throw Error('인물 프레임 누락');const im=new Image();im.src='/assets/'+name;await im.decode();return im};
 try{const actors=await Promise.all(p.frames.map(f=>load(f.versions.at(-1)?.asset)));const im={};await Promise.all(Object.entries(p.companion.parts).map(async([k,v])=>im[k]=await load(v)));if(epoch!==this.epoch)return;this.actors=actors;this.drone=new DroneView({2:im});this.gloveView=new GloveEffectView();this.ready=true;}catch(e){if(epoch===this.epoch)this.status.textContent='연결 오류: '+e.message;}
 }
 draw(){const c=this.c,t=this.time,q=this.project.companion.timing;let end=0,index=this.project.frames.findIndex(f=>(end+=f.duration_ms)>t);if(index<0)index=0;
 c.fillStyle='#34434f';c.fillRect(0,0,760,600);c.drawImage(this.actors[index],300,30,520,520*1245/1263);
 const charging=t>=q.charge&&t<q.fire,firing=t>=q.fire&&t<q.end,clock=charging?560+(t-q.charge)*240/(q.fire-q.charge):firing?t-q.fire:240;
 c.save();c.translate(100,320);c.scale(.18,.18);this.drone.render(c,2,{time:clock,motionTime:t,neutral:!this.fx,layers:this.layers},[-500,600]);c.restore();
 if(this.glove&&t>=q.command&&t<q.end){const gc=t<q.charge?320+(t-q.command)*240/(q.charge-q.command):clock;this.gloveView.render(c,gc,[300+215*520/1263,30+300*520/1263]);}
 this.status.textContent=`${index+1}/12 · ${(t/1000).toFixed(2)}초 · ${charging?'충전':firing?'레이저 발사':t<q.command?'준비':'지휘·복귀'}`;
 }
 dispose(){++this.epoch;cancelAnimationFrame(this.raf);this.abort.abort();this.ready=false;}
}
