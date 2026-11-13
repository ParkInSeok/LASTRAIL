import test from 'node:test';
import assert from 'node:assert/strict';
import {Playback} from '../src/media-playback.mjs';
import {gameplayMedia,bgmControl} from '../src/media-view.mjs';
class Media extends EventTarget {
 paused=true; calls=0; pauseCalls=0;
 play(){this.calls++;this.paused=false;this.dispatchEvent(new Event('play'));return Promise.resolve();}
 pause(){this.pauseCalls++;this.paused=true;this.dispatchEvent(new Event('pause'));}
}
const settle=()=>new Promise(r=>setImmediate(r));
test('video starts only visible, pauses outside/tab hidden, resumes and respects manual pause',async()=>{
 const m=new Media(),p=new Playback(m,'video');p.setVisible(false);assert.equal(m.calls,0);
 p.setVisible(true);await settle();assert.equal(m.calls,1);
 p.setVisible(true);assert.equal(m.calls,1);
 p.setHidden(true);assert.equal(m.paused,true);p.setHidden(false);await settle();assert.equal(m.calls,2);
 p.toggle();p.setVisible(false);p.setVisible(true);assert.equal(m.calls,2);p.dispose();
});
test('reduced motion requires manual play and automatic failure offers retry',async()=>{
 const m=new Media(),p=new Playback(m,'video');p.setReduced(true);p.setVisible(true);assert.equal(m.calls,0);
 p.toggle();await settle();assert.equal(m.calls,1);p.dispose();
 const f=new Media();f.play=()=>Promise.reject(new Error('blocked'));const q=new Playback(f,'video');q.setVisible(true);await settle();assert.equal(q.state,'blocked');
 f.play=Media.prototype.play;q.toggle();await settle();assert.equal(q.state,'playing');q.dispose();
});
test('BGM defaults off and late play after dispose cannot restart it',async()=>{
 const m=new Media(),p=new Playback(m,'bgm');p.setVisible(true);assert.equal(m.calls,0);
 let done;m.play=()=>{m.calls++;return new Promise(r=>done=r);};p.toggle();assert.equal(m.calls,1);
 p.dispose();m.paused=false;done();await settle();assert.equal(m.paused,true);p.toggle();assert.equal(m.calls,1);
});
test('BGM pauses on hidden, resumes only opted-in, error is recoverable',async()=>{
 const m=new Media(),p=new Playback(m,'bgm');p.toggle();await settle();p.setHidden(true);assert.equal(m.paused,true);
 p.setHidden(false);await settle();assert.equal(m.calls,2);m.dispatchEvent(new Event('error'));assert.equal(p.state,'error');
 p.setHidden(true);p.setHidden(false);assert.equal(m.calls,2);p.toggle();await settle();assert.equal(p.state,'playing');p.dispose();
});
test('unconfigured media has no dead control; configured video preserves landscape and poster',()=>{
 assert.equal(gameplayMedia(null),'');assert.equal(bgmControl(null),'');
 const html=gameplayMedia({src:'/media/combat.mp4',poster:'/art/combat.webp'});
 assert.match(html,/width="1920" height="1080"/);assert.match(html,/muted/);assert.match(html,/playsinline/);assert.match(html,/loop/);assert.match(html,/preload="none"/);assert.match(html,/poster="\/art\/combat.webp"/);
 assert.doesNotMatch(html,/autoplay/);assert.match(bgmControl({src:'/media/theme.ogg'}),/BGM 켜기/);
});

import {HomepageMediaController} from '../src/media-controller.mjs';
import {mediaAssets} from '../src/media-config.mjs';
test('configured media mounts once, keeps BGM through routes and disposes all callbacks',async()=>{
 const old={window:globalThis.window,document:globalThis.document,Audio:globalThis.Audio,IntersectionObserver:globalThis.IntersectionObserver};
 const doc=new EventTarget();doc.hidden=false;const motion=new EventTarget();motion.matches=false;
 const audios=[],observers=[];
 class AudioMock extends Media{constructor(){super();audios.push(this);}removeAttribute(){}load(){}}
 class Observer{constructor(callback){this.callback=callback;this.disconnected=false;observers.push(this);}observe(){}disconnect(){this.disconnected=true;}}
 const element=()=>{const el=new EventTarget();el.setAttribute=()=>{};return el;};
 const video=new Media(),button=element(),videoButton=element(),status={},videoStatus={};
 const root={querySelector:s=>({'[data-gameplay]':video,'[data-bgm-toggle]':button,'[data-bgm-status]':status,'[data-video-toggle]':videoButton,'[data-video-status]':videoStatus}[s])};
 let c;
 try{
  globalThis.window={matchMedia:()=>motion,IntersectionObserver:Observer};globalThis.document=doc;globalThis.Audio=AudioMock;globalThis.IntersectionObserver=Observer;
  c=new HomepageMediaController({bgm:{src:'/media/theme.ogg'},gameplay:{src:'/media/combat.mp4'}});
  c.mount(root);c.mount(root);assert.equal(audios.length,1);assert.equal(observers[0].disconnected,true);
  button.dispatchEvent(new Event('click'));await settle();assert.equal(audios[0].calls,1);assert.equal(audios[0].paused,false);
  c.mount(root);assert.equal(audios[0].calls,1);assert.equal(audios[0].paused,false);
  observers.at(-1).callback([{isIntersecting:true,intersectionRatio:.8}]);await settle();assert.equal(video.calls,1);
  doc.hidden=true;doc.dispatchEvent(new Event('visibilitychange'));assert.equal(audios[0].paused,true);assert.equal(video.paused,true);
  c.dispose();doc.hidden=false;doc.dispatchEvent(new Event('visibilitychange'));button.dispatchEvent(new Event('click'));assert.equal(audios[0].calls,1);
  assert.ok(observers.every(x=>x.disconnected));
 }finally{c?.dispose();Object.assign(globalThis,old);}
});
test('media allowlist rejects traversal and requires a real poster path',()=>{
 assert.deepEqual(mediaAssets({gameplay:null,bgm:null}),[]);
 assert.throws(()=>mediaAssets({bgm:{src:'/media/../secret.wav'}}));
 assert.throws(()=>mediaAssets({gameplay:{src:'/media/combat.mp4'}}));
 assert.equal(mediaAssets({gameplay:{src:'/media/combat.mp4',poster:'/art/combat.webp'},bgm:{src:'/media/theme.ogg'}}).length,3);
});
