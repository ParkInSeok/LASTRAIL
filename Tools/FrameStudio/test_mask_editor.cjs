const assert=require('node:assert/strict');
const fs=require('node:fs');
const vm=require('node:vm');
let finishDecode;
class PendingImage {
  decode(){return new Promise(resolve=>{finishDecode=resolve})}
}
const context={Image:PendingImage};
vm.runInNewContext(fs.readFileSync(__dirname+'/web/mask-editor.js','utf8')+';this.Editor=MaskEditorView',context);
(async()=>{
  const editor=Object.create(context.Editor.prototype);
  Object.assign(editor,{generation:0,canvas:{width:64,height:64,getContext:()=>({clearRect(){}})},el:()=>({textContent:''})});
  const old={projectId:'first',index:0};
  const pending=editor.load(old,'first.png',null);
  // A frame switch must be able to see and invalidate even a pending image load.
  assert.equal(editor.context,old);
  await editor.load(null,null,null);
  finishDecode();await pending;
  assert.equal(editor.context,null);
  assert.equal(editor.source,null);
  assert.equal(editor.points.length,0);
  console.log('PASS: pending mask load cannot revive an obsolete frame');
})().catch(error=>{console.error(error);process.exitCode=1});
