const assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm');
const elements=new Map(),events={},timers=new Map();let timerId=0,postCount=0,resolvePost;
function element(){return {value:'',children:[],checked:false,append(o){this.children.push(o);if(!this.value)this.value=o.value;},replaceChildren(){this.children=[];this.value='';}};}
const document={getElementById(id){if(!elements.has(id))elements.set(id,element());return elements.get(id);},createElement:element};
const source={id:'a'.repeat(32),name:'approved',revision:3,references:[{key:'first',asset:'ref.png',label:'원화'}],job:null};
const context={document,URLSearchParams,location:{search:'?id='+source.id},window:{addEventListener(n,f){events[n]=f;}},
 setTimeout(f){const id=++timerId;timers.set(id,f);return id;},clearTimeout(id){timers.delete(id);},
 async fetch(path,options){
  if(options.method==='POST'){postCount++;return new Promise(resolve=>{resolvePost=()=>resolve({ok:true,json:async()=>({job:{status:'running',message:'생성 중'}})});});}
  return {ok:true,json:async()=>path==='/api/state'?{token:'token',projects:[source]}:source};
 }};
vm.createContext(context);
let code=fs.readFileSync(__dirname+'/web/local-motion.js','utf8').replace('new MotionController().init()', '(globalThis.controller=new MotionController()).init()');vm.runInContext(code,context);
(async()=>{
 await new Promise(setImmediate);const c=context.controller;
 document.getElementById('approved').checked=true;document.getElementById('prompt').value='breathing';document.getElementById('seed').value='42';
 const a=c.generate();await Promise.resolve();await c.generate();assert.equal(postCount,1);resolvePost();await a;
 assert.equal(c.busy,true);assert.equal(timers.size,1);events.pagehide();assert.equal(timers.size,0);
 events.pageshow({persisted:true});await new Promise(setImmediate);assert.equal(c.closed,false);assert.equal(c.busy,false);
 context.fetch=async()=>{throw Error('load failed')};await assert.rejects(c.select('b'.repeat(32)));
 assert.equal(c.state,null);assert.equal(elements.get('generate').disabled,true);
 console.log('Single generate command, polling disposal, restored-page refresh and failed-source guard passed');
})().catch(e=>{console.error(e);process.exitCode=1});
