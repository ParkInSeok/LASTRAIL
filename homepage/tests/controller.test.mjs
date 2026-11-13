import test from 'node:test';
import assert from 'node:assert/strict';
import {HomepageController} from '../src/controller.mjs';
import {escapeHtml} from '../src/views.mjs';
test('bind twice delivers one navigation event; unbind stops callbacks',()=>{
 const original=globalThis.window;globalThis.window=new EventTarget();
 const root=new EventTarget();let renders=0;
 class Probe extends HomepageController {render(){renders++;}}
 try {const c=new Probe(root,{});c.bind();c.bind();renders=0;window.dispatchEvent(new Event('popstate'));assert.equal(renders,1);c.unbind();window.dispatchEvent(new Event('popstate'));assert.equal(renders,1);}
 finally{globalThis.window=original;}
});
test('untrusted status prose is escaped as text',()=>assert.equal(escapeHtml('<img src=x onerror="x">&'), '&lt;img src=x onerror=&quot;x&quot;&gt;&amp;'));


test('FAQ input binds once, resets with focus, and stops after unbind',()=>{
 const original=globalThis.window;globalThis.window=new EventTarget();
 const root=new EventTarget();const items=Array.from({length:4},(_,i)=>({dataset:{faqIndex:String(i)},hidden:false}));
 let updates=0,focusCount=0;
 const counter={set textContent(value){updates++;this.value=value;}};
 const empty={hidden:true};
 const input={value:'결제',matches:selector=>selector==='[data-faq-search]',focus(){focusCount++;}};
 root.querySelectorAll=()=>items;
 root.querySelector=selector=>({'#faq-count':counter,'#faq-empty':empty,'[data-faq-search]':input}[selector]);
 class Probe extends HomepageController {render(){}}
 const dispatch=(name,target)=>{const event=new Event(name);Object.defineProperty(event,'target',{value:target});root.dispatchEvent(event);};
 const controller=new Probe(root,{});
 try{
  controller.bind();controller.bind();dispatch('input',input);
  assert.equal(updates,1);assert.deepEqual(items.map(x=>x.hidden),[true,true,true,false]);
  input.value='없는검색어';dispatch('input',input);assert.equal(empty.hidden,false);
  dispatch('click',{closest:selector=>selector==='[data-faq-reset]'?{}:null});
  assert.equal(input.value,'');assert.equal(focusCount,1);assert.equal(empty.hidden,true);
  assert.ok(items.every(x=>!x.hidden));
  const before=updates;controller.unbind();dispatch('input',input);assert.equal(updates,before);
 }finally{controller.unbind();globalThis.window=original;}
});
