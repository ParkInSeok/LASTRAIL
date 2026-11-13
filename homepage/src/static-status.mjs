import {escapeHtml as e,page} from './views.mjs';
import {statusPresentation} from './status.mjs';
export function document(body,title='LAST RAIL — 상태 안내'){
 return '<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+title+'</title><link rel="icon" href="data:,"><link rel="stylesheet" href="/styles.css"></head><body>'+body+'</body></html>';
}
export function staticSurface(path,status,qaName=''){
 const state=statusPresentation(status), prefix=qaName?'/__qa/'+qaName:'';
 const qa=qaName?'<div class="qa-label">LOCAL QA FIXTURE · 실제 운영 상태가 아닙니다 · '+e(qaName)+'</div>':'';
 const noticeLink=state.blockPage?'':'<a href="'+prefix+'/notices">공지</a>';
 const nav='<nav class="static-nav" aria-label="상태 안내 탐색"><a href="'+prefix+'/status">상태 새로고침 ↻</a>'+noticeLink+'<a href="/">기본 미리보기</a></nav>';
 if(state.blockPage && path!=='/status')return document(qa+'<main class="maintenance"><p class="eyebrow">LAST RAIL / SERVICE NOTICE</p><h1>홈페이지 점검 중입니다</h1><p>'+e(state.summary)+'</p><div class="status-grid"><article><h2>게임</h2><p>'+e(state.game)+'</p></article><article><h2>홈페이지</h2><p>'+e(state.web)+'</p></article></div><p>시작·종료 예정: 확인된 일정 없음</p><p>마지막 갱신: '+e(state.updatedAt??'확인된 기록 없음')+'</p>'+nav+'<p>문의 제출은 현재 이용할 수 없습니다. 접수된 것으로 처리되지 않습니다.</p></main>');
 const data={kind:qaName?'fixture':'unconnected',status};
 let body=page(path,data);
 // No JS needed on status and QA routes. Scope local fixture links to its scenario.
 body=body.replaceAll('<button data-filter=', '<button disabled data-filter=').replaceAll('data-refresh','disabled').replaceAll('href="/','href="'+prefix+'/');
 if(qaName && body.includes('data-filter'))body='<p>정적 검증 화면에서는 분류 선택이 비활성화됩니다. 기본 미리보기에서 동작을 확인하세요.</p>'+body;
 return document(qa+'<main><div class="static-banner">'+e(state.summary)+'</div>'+nav+body+'</main>');
}



