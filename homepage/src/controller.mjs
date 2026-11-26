import {renderApp} from './views.mjs';
import {UnconnectedPublicSource} from './read-source.mjs';
export class HomepageController {
  #root; #source; #lifetime;
  constructor(root,source){this.#root=root;this.#source=source;}
  bind(){
    this.unbind();this.#lifetime=new AbortController();
    const options={signal:this.#lifetime.signal};
    this.#root.addEventListener('click',event=>{
      const a=event.target.closest('a[data-nav]');
      if(a && !event.ctrlKey&&!event.metaKey&&!event.shiftKey&&!event.altKey&&event.button===0){event.preventDefault();history.pushState({},'',a.getAttribute('href'));this.render(true);return;}
      const filter=event.target.closest('[data-filter]');
      if(filter){for(const button of filter.parentElement.querySelectorAll('button'))button.setAttribute('aria-pressed',String(button===filter));this.#root.querySelector('#filter-result').textContent=`${filter.dataset.filter} · 연결된 공개 자료 없음`;}
      if(event.target.closest('[data-refresh]')){this.render();this.#root.querySelector('#refresh-result').textContent='연결된 상태 조회 경로가 없습니다. 현재 상태를 확인할 수 없습니다.';this.#root.querySelector('[data-refresh]').focus();}
    },options);
    window.addEventListener('popstate',()=>this.render(true),options);this.render();
  }
  render(focus=false){this.#root.innerHTML=renderApp(location.pathname,this.#source.read());const h=this.#root.querySelector('h1');document.title=`${h?.textContent??'공식 홈페이지'} | LAST RAIL`;if(focus){this.#root.querySelector('main').focus();window.scrollTo(0,0);}}
  unbind(){this.#lifetime?.abort();}
}
const controller=new HomepageController(document.querySelector('#app'),new UnconnectedPublicSource());
controller.bind();
window.addEventListener('pagehide',()=>controller.unbind());
window.addEventListener('pageshow',event=>{if(event.persisted)controller.bind();});
