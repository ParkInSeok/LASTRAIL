import {describe,it,expect,vi} from 'vitest';import {renderToStaticMarkup} from 'react-dom/server';
import {LiveOpsWorkspaceView,type LiveOpsActions} from './LiveOpsWorkspacePanel';import {LiveOpsWorkspace} from './LiveOpsWorkspace';
const actions:LiveOpsActions={open:vi.fn(),select:vi.fn(),close:vi.fn(),refresh:vi.fn(),next:vi.fn(),review:vi.fn(),history:vi.fn()};
describe('liveops disconnected review view',()=>{
 it('labels actual connection limits instead of issuing commands',()=>{const m=new LiveOpsWorkspace('actor','DEV',{list:vi.fn(),detail:vi.fn()});const html=renderToStaticMarkup(<LiveOpsWorkspaceView state={m.snapshot()} environment="DEV" actions={actions} auditAllowed={false}/>);expect(html).toContain('검토용 구성안');expect(html).toContain('계약 공급 후');expect(html).not.toContain('지급 실행');expect(html).not.toContain('변경 이력 열기');});
 it('renders failed read separately from an empty result',()=>{const m=new LiveOpsWorkspace('actor','DEV',{list:vi.fn(),detail:vi.fn()});const html=renderToStaticMarkup(<LiveOpsWorkspaceView state={{...m.snapshot(),list:{phase:'error'}}} environment="DEV" actions={actions} auditAllowed={false}/>);expect(html).toContain('확인할 수 없습니다');expect(html).not.toContain('등록된 항목이 없습니다');});
});
