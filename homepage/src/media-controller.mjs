import {Playback} from './media-playback.mjs';
// Owns browser subscriptions and media instances. BGM survives internal route renders.
export class HomepageMediaController {
 #config;#audio=null;#music=null;#video=null;#observer=null;#surface=null;#life=null;#motion=null;
 constructor(config){this.#config=config;}
 #bind(){
  if(this.#life)return;
  this.#life=new AbortController();this.#motion=window.matchMedia('(prefers-reduced-motion: reduce)');
  document.addEventListener('visibilitychange',()=>{this.#video?.setHidden(document.hidden);this.#music?.setHidden(document.hidden);},{signal:this.#life.signal});
  this.#motion.addEventListener('change',event=>this.#video?.setReduced(event.matches),{signal:this.#life.signal});
  if(this.#config.bgm){this.#audio=new Audio(this.#config.bgm.src);this.#audio.loop=true;this.#audio.preload='none';this.#audio.volume=.25;this.#music=new Playback(this.#audio,'bgm');this.#music.setHidden(document.hidden);}
 }
 #control(root,playback,kind){
  const button=root.querySelector(`[data-${kind}-toggle]`),status=root.querySelector(`[data-${kind}-status]`);
  if(!button)return;
  const music=kind==='bgm';
  playback.watch(state=>{
   const active=state==='playing'||state==='loading';button.setAttribute('aria-pressed',String(active));
   button.textContent=music?(active?'BGM 끄기':'BGM 켜기'):(active?'영상 일시정지':'영상 재생');
   status.textContent=state==='error'?'미디어를 불러오지 못했습니다. 다시 시도해 주세요.':state==='blocked'?'재생 버튼을 눌러 다시 시도해 주세요.':music?'':'무음 플레이 영상';
  });
  button.addEventListener('click',()=>playback.toggle(),{signal:this.#surface.signal});
 }
 detach(){this.#surface?.abort();this.#observer?.disconnect();this.#observer=null;this.#video?.dispose();this.#video=null;this.#music?.watch(()=>{});}
 mount(root){
  this.detach();
  if(!this.#config.gameplay&&!this.#config.bgm)return;
  this.#bind();this.#surface=new AbortController();
  if(this.#music)this.#control(root,this.#music,'bgm');
  const element=root.querySelector('[data-gameplay]');
  if(element){
   element.muted=true;this.#video=new Playback(element,'video');this.#video.setReduced(this.#motion.matches);this.#video.setHidden(document.hidden);
   this.#control(root,this.#video,'video');
   if('IntersectionObserver' in window){
    const player=this.#video;
    this.#observer=new IntersectionObserver(entries=>{const entry=entries[0];player.setVisible(entry.isIntersecting&&entry.intersectionRatio>=.35);},{threshold:[0,.35]});
    this.#observer.observe(element);
   }else {this.#video.setReduced(true);this.#video.setVisible(true);}
  }
 }
 dispose(){this.detach();this.#life?.abort();this.#life=null;this.#music?.dispose();this.#music=null;if(this.#audio){this.#audio.removeAttribute('src');this.#audio.load();this.#audio=null;}}
}
