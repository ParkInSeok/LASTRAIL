const assert=require('node:assert/strict');
const {FxClock,selectFx}=require('./web/fx-timeline.js');
const project={frames:Array.from({length:24},()=>({duration_ms:40})),fx_timeline:{stages:[{start_ms:0,asset:'p'},{start_ms:450,asset:'shot'},{start_ms:600,asset:'off'}]}};
for(const rate of [1,.5]){
 const clock=new FxClock(960);clock.seek(449);clock.play(100,rate);
 assert.equal(selectFx(project,clock.read(100)).asset,'p');
 assert.equal(selectFx(project,clock.read(100+1/rate)).asset,'shot');
 clock.pause(100+1/rate);assert.equal(clock.read(9000),450);
 clock.play(9000,rate);assert.equal(clock.read(9000+1/rate),451);
 assert.equal(selectFx(project,clock.read(9000+1/rate)).asset,'shot');
 clock.seek(959);clock.play(10000,rate);assert.equal(clock.read(10000+1/rate),0);
}
assert.equal(selectFx(project,449).asset,'p');assert.equal(selectFx(project,450).asset,'shot');assert.equal(selectFx(project,451).asset,'shot');
assert.equal(selectFx({frames:project.frames},450),null);
console.log('FX boundary, 1x/.5x, pause/resume, seek/loop selection passed');

// Exercise the actual View callback ownership without a browser or graphics claim.
const {FxTimelineView}=require('./web/fx-timeline.js');
const listeners=new Map();global.window={addEventListener:(n,f)=>listeners.set(n,f),removeEventListener:(n,f)=>{if(listeners.get(n)===f)listeners.delete(n);}};
global.cancelAnimationFrame=()=>{};global.requestAnimationFrame=()=>1;
const elements=new Map(),root={innerHTML:'',querySelector(key){if(!elements.has(key))elements.set(key,{value:1});return elements.get(key);}};
let calls=0;const view=new FxTimelineView(root,async()=>{calls++;});
view.project=project;view.clock=new FxClock(960);view.ready=true;view.draw=()=>{};
root.querySelector('.fx-rows').children=project.fx_timeline.stages.map(row=>({querySelector:()=>({value:row.start_ms})}));
root.querySelector('.fx-save').onclick();assert.equal(calls,1);
root.querySelector('.fx-play').onclick();root.querySelector('.fx-pause').onclick();assert.equal(calls,1);
view.dispose();assert.equal(root.querySelector('.fx-save').onclick,null);assert.equal(listeners.size,0);
console.log('View save delegates once; playback emits no commands; disposal removes owned handlers');
