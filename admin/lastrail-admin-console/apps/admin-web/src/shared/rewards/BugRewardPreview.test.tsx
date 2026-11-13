import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {BugRewardPreview} from './BugRewardPreview';
import {parseBugRewardDraft} from './bugReward';
describe('disconnected reward preview',()=>{
 it('renders separate estimates and unresolved eligibility without executable controls',()=>{
  const html=renderToStaticMarkup(<BugRewardPreview draft={parseBugRewardDraft({grade:'MAJOR',contribution:'FIRST',previousGratitudePaid:null})}/>);
  for(const text of ['감사 보상','전원 보상','피해 복구','3,000','기지급액 미확인','자격 미확정','각각 별도 지급','연결 전'])expect(html).toContain(text);
  expect(html).not.toContain('<button');expect(html).not.toContain('<form');expect(html).not.toContain('지급했습니다');
 });
});
