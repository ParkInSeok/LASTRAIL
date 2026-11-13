import {describe,it,expect} from 'vitest';import {renderToStaticMarkup} from 'react-dom/server';
import {BuildManagementReview} from './BuildManagementReview';import {BuildInputDraft} from './BuildInputDraft';
describe('disconnected build review',()=>{
 it('exposes explicit unavailable execution and separate unknown evidence',()=>{const html=renderToStaticMarkup(<BuildManagementReview state={new BuildInputDraft('actor').snapshot()} onChange={()=>{}}/>);for(const label of ['실행 불가','작업 상태 미연결','단계별 로그 미공급','산출물 미공급','설치 검수 미확인','실Player 검수 미확인','배포·스토어 공개 미확인'])expect(html).toContain(label);expect(html).toContain('disabled');expect(html).not.toContain('href=');});
 it('escapes untrusted draft text and never creates external artifact links',()=>{const m=new BuildInputDraft('actor');m.change('note','<script>alert(1)</script>');const html=renderToStaticMarkup(<BuildManagementReview state={m.snapshot()} onChange={()=>{}}/>);expect(html).not.toContain('<script>');expect(html).toContain('&lt;script&gt;');expect(html).not.toContain('<a ');});
});
