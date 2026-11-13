import {HomepageMediaController} from './media-controller.mjs';
import {homepageMedia} from './media-config.mjs';
import {faq} from './content.mjs';
import {searchFaq} from './faq-search.mjs';
import {renderApp} from './views.mjs';
import {readingPanel} from './reading-view.mjs';
import {parseReadingLocation} from './reading-route.mjs';
import {ReadingModel,UnavailableReadingPort} from './reading-model.mjs';

export class HomepageController {
 #media=new HomepageMediaController(homepageMedia); #root; #source; #lifetime; #reader; #unsubscribe;
 constructor(root,source,reader=new ReadingModel(new UnavailableReadingPort())){
  this.#root=root;this.#source=source;this.#reader=reader;
 }
 bind(){
  this.unbind();this.#lifetime=new AbortController();
  const options={signal:this.#lifetime.signal};
  this.#unsubscribe=this.#reader.subscribe(state=>this.#updateReading(state));
  this.#root.addEventListener('click',event=>{
   if(event.target.closest('[data-faq-reset]')){
    const input=this.#root.querySelector('[data-faq-search]');
    input.value='';this.#updateFaq('');input.focus();return;
   }
   const menu=event.target.closest('[data-menu]');
   if(menu){this.#setMenu(menu.getAttribute('aria-expanded')!=='true');return;}
   const a=event.target.closest('a[data-nav]');
   if(a&&parseReadingLocation(new URL(location.href)).qa)return;
   if(a&&!event.ctrlKey&&!event.metaKey&&!event.shiftKey&&!event.altKey&&event.button===0){
    event.preventDefault();history.pushState({},'',a.getAttribute('href'));this.render(true);return;
   }
   const filter=event.target.closest('[data-filter]');
   if(filter){
    for(const button of filter.parentElement.querySelectorAll('button'))button.setAttribute('aria-pressed',String(button===filter));
    this.#root.querySelector('#filter-result').textContent=filter.dataset.filter+' · 자료를 준비하고 있습니다';
   }
   if(event.target.closest('[data-read-retry]')){
    if(this.#reader.state.retryAt>Date.now()){
     const feedback=this.#root.querySelector('#retry-feedback');
     if(feedback)feedback.textContent='잠시 후 다시 시도해 주세요.';
    }else this.#reader.retry();
   }
   if(event.target.closest('[data-refresh]')){
    this.#root.querySelector('#refresh-result').textContent='지금은 최신 상태를 확인할 수 없습니다. 잠시 후 다시 확인해 주세요.';
   }
  },options);
  this.#root.addEventListener('input',event=>{
   if(event.target.matches('[data-faq-search]'))this.#updateFaq(event.target.value);
  },options);
  this.#root.addEventListener('keydown',event=>{
   if(event.key==='Escape'&&this.#root.querySelector('[data-menu]')?.getAttribute('aria-expanded')==='true'){
    this.#setMenu(false);this.#root.querySelector('[data-menu]').focus();
   }
  },options);
  window.addEventListener('popstate',()=>this.render(true),options);
  this.render();
 }
 #updateFaq(query){
  const matches=new Set(searchFaq(faq,query));
  for(const item of this.#root.querySelectorAll('[data-faq-index]')){
   item.hidden=!matches.has(Number(item.dataset.faqIndex));
  }
  this.#root.querySelector('#faq-count').textContent=query.trim()?`검색 결과 ${matches.size}개 / 전체 ${faq.length}개`:`전체 질문 ${faq.length}개`;
  this.#root.querySelector('#faq-empty').hidden=matches.size!==0;
 }
 #setMenu(open){
  this.#root.querySelector('[data-menu]').setAttribute('aria-expanded',String(open));
  this.#root.querySelector('#site-navigation').classList.toggle('is-open',open);
 }
 #updateReading(state){
  const panel=this.#root.querySelector('#reading-content');
  if(!panel)return;
  const restoreFocus=panel.contains(document.activeElement);
  panel.innerHTML=readingPanel(state);
  const announcement=this.#root.querySelector('#reader-announcement');
  if(announcement)announcement.textContent=panel.querySelector('h2')?.textContent??'';
  if(restoreFocus){
   const focusTarget=panel.querySelector('[data-read-retry]')??panel.querySelector('h2');
   if(focusTarget?.tagName==='H2')focusTarget.setAttribute('tabindex','-1');
   focusTarget?.focus();
  }
 }
 render(focus=false){
  this.#reader.cancel();
  const route=parseReadingLocation(new URL(location.href));
  const data={...this.#source.read(),reading:route.query?{phase:'idle',query:route.query,retryAt:null}:null};
  this.#media.detach();
  this.#root.innerHTML=renderApp(route.path,data);
  this.#media.mount(this.#root);
  const faqControls=this.#root.querySelector('[data-faq-controls]');
  if(faqControls)faqControls.hidden=false;
  document.title=(this.#root.querySelector('h1')?.textContent??'공식 홈페이지')+' | LAST RAIL';
  if(focus){this.#root.querySelector('main').focus();window.scrollTo(0,0);}
  if(route.query)this.#reader.load(route.query,{invalid:route.invalid});
 }
 unbind(){this.#media.dispose();this.#lifetime?.abort();this.#unsubscribe?.();this.#unsubscribe=null;this.#reader.cancel();}
}




