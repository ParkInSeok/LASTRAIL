import {it,expect,vi} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {MemoryRouter} from 'react-router-dom';
import {RewardPreviewView} from './RewardPreviewView';
import {RewardPreviewModel} from './RewardPreviewModel';
it('disconnected actual preview View never represents approval or payment actions',()=>{const m=new RewardPreviewModel('actor','DEV',{capabilities:vi.fn(),preview:vi.fn()});const html=renderToStaticMarkup(<MemoryRouter><RewardPreviewView state={m.snapshot()} onPreview={()=>{}} onRefresh={()=>{}}/></MemoryRouter>);expect(html).toContain('읽기 전용');expect(html).toContain('검토승인·지급 실행 불가');expect(html).not.toContain('지급했습니다');expect(html).not.toContain('지급 실행</button>');});
