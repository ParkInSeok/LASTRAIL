import {escapeHtml as e} from './html.mjs';
import {readingHref,validReadingQuery} from './reading-route.mjs';
const copy={
 idle:['공식 자료를 준비하고 있습니다','공개된 자료가 준비되면 이곳에서 안내하겠습니다.'],
 loading:['자료를 불러오고 있습니다','잠시만 기다려 주세요.'],
 unavailable:['공식 자료를 준비하고 있습니다','지금은 자료를 확인할 수 없습니다. 공개 준비가 끝나면 안내하겠습니다.'],
 empty:['표시할 자료가 없습니다','선택한 범위에 공개된 자료가 없습니다.'],
 'not-found':['자료를 찾을 수 없습니다','주소와 적용 버전을 확인해 주세요.'],
 invalid:['올바르지 않은 자료 주소입니다','자료 목록으로 돌아가 다시 선택해 주세요.'],
 error:['자료를 불러오지 못했습니다','연결을 확인한 뒤 다시 시도해 주세요.']
};
export function readingPanel(state={phase:'unavailable',query:null,retryAt:null}) {
 const [title,description]=copy[state.phase]??copy.error;
 const query=state.query,valid=validReadingQuery(query);
 const version=query?.version?'<p class="version-request">요청한 버전 <strong>'+e(query.version)+'</strong> · 공개 여부 확인 전</p>':'<p class="version-request">적용 버전은 자료와 함께 안내됩니다.</p>';
 const address=valid&&query.version?'<div class="permalink"><a href="'+e(readingHref(query))+'" data-nav>이 버전 주소</a><span>이 주소는 자료의 공개나 이용 가능 여부를 보장하지 않습니다.</span></div>':'';
 const returnLink=query?.section?'<a href="/'+e(query.section)+'" data-nav>자료 목록으로 돌아가기</a>':'';
 const wait=state.retryAt&&state.retryAt>Date.now()?'<p>재시도 가능 시각: <time>'+e(new Date(state.retryAt).toLocaleTimeString('ko-KR'))+'</time></p>':'';
 const retry=state.phase==='error'?'<button type="button" data-read-retry>다시 시도</button>'+wait+'<p id="retry-feedback" role="status"></p>':'';
 return '<section class="reading reader-panel" aria-labelledby="reading-title" aria-busy="'+(state.phase==='loading')+'">'+version+address+
  '<div class="empty"><span class="empty-symbol" aria-hidden="true">'+(state.phase==='loading'?'…':'＋')+'</span><h2 id="reading-title">'+title+'</h2><p>'+description+'</p>'+
  (['invalid','not-found'].includes(state.phase)?returnLink:'')+retry+'</div>'+
  (query?.entry?'<p class="requested-entry">요청한 자료: '+e(query.entry)+'</p>':'')+'</section>';
}

