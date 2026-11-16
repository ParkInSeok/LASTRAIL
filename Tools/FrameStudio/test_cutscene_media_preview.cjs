const assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm');
const elements={media:{value:'candidate'},download:{hidden:false},previewState:{},status:{},player:{style:{},pause(){},load(){},async play(){this.played=true}}};
const context={document:{getElementById:id=>elements[id]},location:{search:'?id='+ 'a'.repeat(32)},URLSearchParams,AbortController};
vm.createContext(context);
const code=fs.readFileSync(__dirname+'/web/cutscene-editor.js','utf8').replace('const editor=new EditorController();editor.init().catch(e=>editor.notice(e.message));','globalThis.controller=new EditorController();');
vm.runInContext(code,context);
(async()=>{
 const c=context.controller;c.draft={revision:4,dirty:false,timeline:{clips:[]},media:{candidate:{kind:'video',name:'세미최종 2번',file:'b'.repeat(32)+'.mp4'}}};
 const before=JSON.stringify(c.draft);await c.previewMedia();
 assert.equal(JSON.stringify(c.draft),before);assert.equal(elements.player.played,true);assert.match(elements.player.src,/cutscene-media/);assert.match(elements.previewState.textContent,/세미최종 2번/);assert.equal(elements.download.hidden,true);
 c.job={status:'running'};await assert.rejects(c.previewMedia(),/출력이 끝난/);
 c.job=null;elements.media.value='missing';await assert.rejects(c.previewMedia(),/영상을 선택/);
 console.log('Original preview leaves timeline/revision/approval unchanged; invalid media and running render guarded.');
})().catch(e=>{console.error(e);process.exitCode=1});
