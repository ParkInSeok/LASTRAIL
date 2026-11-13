import {expect,it,vi} from "vitest";
import {OpsErrorLogsController} from "./opsErrorLogsController";
const scope={principalId:"operator",environment:"DEV",authorizationRevision:"revision"};
const transport=()=>({opsErrorLogs:vi.fn(async():Promise<any>=>({items:[{id:"event"}]})),opsErrorLogSummary:vi.fn(async():Promise<any>=>({eventCount:1})),opsErrorLogGroups:vi.fn(async():Promise<any>=>({totalGroupCount:1})),opsErrorLogDetail:vi.fn(async():Promise<any>=>({item:{id:"event"}})),opsErrorLogInvestigation:vi.fn(async():Promise<any>=>({})),opsErrorLogInvestigationCommand:vi.fn(async():Promise<any>=>({}))});
it("reuses same filter reads and keeps list, whole-filter summary and groups arguments identical",async()=>{
 const api=transport(),c=new OpsErrorLogsController(scope,["OPS_ERROR_LOG_READ"],api);const filters={environment:"DEV",phase:"save",contractVersion:"v1",limit:"25",cursor:"25"};
 await c.reads.list(filters);await c.reads.summary(filters);await c.reads.groups(filters);await c.reads.list({...filters});await c.reads.detail("event");await c.reads.detail("event");
 expect(api.opsErrorLogs).toHaveBeenCalledTimes(1);expect(api.opsErrorLogDetail).toHaveBeenCalledTimes(1);expect(api.opsErrorLogSummary).toHaveBeenCalledWith(filters);expect(api.opsErrorLogGroups).toHaveBeenCalledWith(filters);await c.reads.list(filters,true);expect(api.opsErrorLogs).toHaveBeenCalledTimes(2);
});
it("a 403 revokes both consumers and removes cached records",async()=>{
 const api=transport(),c=new OpsErrorLogsController(scope,["OPS_ERROR_LOG_READ","OPS_ERROR_LOG_WRITE"],api);await c.reads.detail("event");api.opsErrorLogSummary.mockRejectedValueOnce({status:403});await expect(c.reads.summary({})).rejects.toBeDefined();expect(c.canInvestigate).toBe(false);expect(c.reads.peek("detail","event")).toBeUndefined();await expect(c.reads.detail("event")).rejects.toThrow("OPS_MODEL_DISPOSED");
});
it("does not derive WRITE from READ or role-like labels",()=>{const c=new OpsErrorLogsController(scope,["OPS_ERROR_LOG_READ","OPERATOR"],transport());expect(c.canInvestigate).toBe(false);});
it("isolates account/environment/authorization revisions and cancels late read effects on disposal",async()=>{
 const api=transport();let complete!:(value:any)=>void;api.opsErrorLogs.mockImplementationOnce(()=>new Promise(resolve=>{complete=resolve}));const c=new OpsErrorLogsController(scope,["OPS_ERROR_LOG_READ"],api),change=vi.fn();c.subscribe(change);const loading=c.reads.list({});c.dispose();complete({items:[{id:"event"}]});await loading;expect(c.reads.peek("list",{})).toBeUndefined();expect(change).not.toHaveBeenCalled();expect(c.matchesScope({...scope,environment:"PRD"})).toBe(false);
});

it("401 invalidates both consumers and prevents reusing private cached detail",async()=>{
 const api=transport(),c=new OpsErrorLogsController(scope,["OPS_ERROR_LOG_READ","OPS_ERROR_LOG_WRITE"],api);await c.reads.detail("event");api.opsErrorLogSummary.mockRejectedValueOnce({status:401});await expect(c.reads.summary({})).rejects.toBeDefined();expect(c.canInvestigate).toBe(false);expect(c.reads.peek("detail","event")).toBeUndefined();await expect(c.reads.detail("event")).rejects.toThrow("OPS_MODEL_DISPOSED");
});
