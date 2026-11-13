// Owns playback intent and asynchronous media lifetime; no routing or DOM creation.
export class Playback {
 #media;#mode;#visible;#hidden=false;#reduced=false;#manual=false;#opted=false;
 #disposed=false;#pending=false;#epoch=0;#state='idle';#notify=()=>{};#abort=new AbortController();
 constructor(media,mode){
  this.#media=media;this.#mode=mode;this.#visible=mode==='bgm';
  const options={signal:this.#abort.signal};
  media.addEventListener('play',()=>{if(!this.#wanted()){media.pause();return;}this.#set('playing');},options);
  media.addEventListener('pause',()=>{if(!['blocked','error'].includes(this.#state))this.#set('paused');},options);
  media.addEventListener('error',()=>{this.#manual=true;this.#opted=false;this.#stop();this.#set('error');},options);
 }
 get state(){return this.#state;}
 watch(callback){this.#notify=callback;callback(this.#state);}
 #set(value){this.#state=value;this.#notify(value);}
 #wanted(){return !this.#disposed&&!this.#hidden&&this.#visible&&(this.#opted||(this.#mode==='video'&&!this.#manual&&!this.#reduced));}
 setVisible(value){this.#visible=value;this.#sync();}
 setHidden(value){this.#hidden=value;this.#sync();}
 setReduced(value){if(value&&!this.#reduced)this.#opted=false;this.#reduced=value;this.#sync();}
 toggle(){
  if(this.#disposed)return;
  if(this.#state==='playing'||this.#pending){this.#manual=true;this.#opted=false;}
  else {this.#opted=true;this.#manual=false;}
  this.#sync();
 }
 #stop(){this.#epoch++;this.#pending=false;this.#media.pause();}
 #sync(){
  if(this.#disposed)return;
  if(!this.#wanted()){this.#stop();return;}
  if(this.#pending||!this.#media.paused)return;
  this.#pending=true;const epoch=++this.#epoch;this.#set('loading');
  let promise;try{promise=this.#media.play();}catch(error){promise=Promise.reject(error);}
  Promise.resolve(promise).then(()=>{
   if(epoch!==this.#epoch){if(!this.#wanted())this.#media.pause();return;}
   this.#pending=false;if(!this.#wanted()){this.#media.pause();return;}this.#set('playing');
  },()=>{
   if(epoch!==this.#epoch)return;
   this.#pending=false;this.#manual=true;this.#opted=false;this.#media.pause();this.#set('blocked');
  });
 }
 dispose(){this.#disposed=true;this.#notify=()=>{};this.#abort.abort();this.#stop();}
}
