const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm');
const context=vm.createContext({});
vm.runInContext(fs.readFileSync(__dirname+'/web/clip-workflow.js','utf8'),context);
const files=names=>names.map(name=>({name}));
test('numbered frame files keep numeric order independent of file chooser order',()=>{
  const result=context.numberedPNGs(files(['frame-03.png','frame-01.png','frame-02.png']),'frame',3);
  assert.equal(result.get(0).name,'frame-01.png');assert.equal(result.get(2).name,'frame-03.png');
});
test('missing or duplicate frame slots cannot silently shift timing',()=>{
  for(const names of [['frame-01.png'],['frame-01.png','frame-1.png'],['frame-00.png','frame-02.png'],['frame-01.png','frame-03.png']])assert.throws(()=>context.numberedPNGs(files(names),'frame',2));
});
test('sparse independent FX retain the original exposure slot',()=>{
  const result=context.numberedPNGs(files(['fx-07.png','fx-06.png']),'fx',12,false);
  assert.equal(result.get(5).name,'fx-06.png');assert.equal(result.get(6).name,'fx-07.png');assert.equal(result.has(0),false);
  assert.throws(()=>context.numberedPNGs(files(['frame-06.png']),'fx',12,false));
});
