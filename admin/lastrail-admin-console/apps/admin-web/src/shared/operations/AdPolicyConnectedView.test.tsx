import {describe,expect,it,vi} from "vitest";
import {renderToStaticMarkup} from "react-dom/server";
import {MemoryRouter} from "react-router-dom";
import {AdPolicyConnectedView} from "./AdPolicyConnectedView";

describe("approved policy layout",()=>{
 it("shows current policy and draft in one card without unrelated readiness cards",()=>{
  const html=renderToStaticMarkup(<MemoryRouter><AdPolicyConnectedView
   environment="DEV" writeAllowed read={{kind:"ready",policy:{environment:"DEV",policyStatus:"CONFIGURED",adsEnabled:true,runtimePolicyRevision:4,serverTime:100,canWrite:true,canEnable:false,enableBlockReason:"not_ready"}}}
   draft={{adsEnabled:null,reason:""}} errors={[]} command={{kind:"idle"}} editable
   review={null} reviewDraft={vi.fn()} change={vi.fn()} save={vi.fn()} reload={async()=>{}} retry={vi.fn()} canRetry={false}
  /></MemoryRouter>);
  expect(html).toContain("새로고침");
  expect(html).toContain("변경 내용 검토");
  expect(html).not.toContain("영구 광고 스킵 혜택 가용성");
  expect(html).not.toContain("첫 데모");
  expect(html).not.toContain("기본 보상과 적법한 영구권");
 });
});
