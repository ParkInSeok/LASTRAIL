import {expect,it,vi} from "vitest";
import * as module from "./opsInvestigationContract";
import {OpsInvestigationModel} from "./opsInvestigationModel";
const state=(version=0,status="open")=>({errorLogId:"event",status,version,reason:version?"investigate":null,fixBuild:null,evidence:null,verificationResult:null,updatedBy:version?"operator":null,updatedAt:version?1:null,resolvedAt:null});
const read=(version=0,status="open")=>({contractVersion:module.OPS_INVESTIGATION_VERSION,investigation:state(version,status),recurrence:{available:false,reason:"NOT_RESOLVED_WITH_BUILD"}});
const receipt=(version=1)=>({accepted:true,contractVersion:module.OPS_INVESTIGATION_VERSION,permission:"OPS_ERROR_LOG_WRITE",auditId:"audit",investigation:state(version,"acknowledged")});
const command={status:"acknowledged" as const,expectedVersion:0,reason:"investigate",fixBuild:null,evidence:null,verificationResult:null};
const scope={principalId:"operator",environment:"DEV",authorizationRevision:"rev1"};
const api=()=>({read:vi.fn(async()=>read()),write:vi.fn(async()=>receipt())});
it("validates strict command fields, versions and resolution evidence",()=>{
 expect(module.opsInvestigationCommandSchema.safeParse({...command,extra:true}).success).toBe(false);
 expect(module.opsInvestigationCommandSchema.safeParse({...command,status:"resolved"}).success).toBe(false);
 expect(module.opsInvestigationCommandSchema.safeParse({...command,expectedVersion:0.5}).success).toBe(false);
 expect(module.opsInvestigationCommandSchema.safeParse({...command,status:"verifying",evidence:"wrong"}).success).toBe(false);
 expect(module.opsInvestigationCommandSchema.safeParse({...command,status:"resolved",fixBuild:{appVersion:null,appVersionCode:"123",serverVersion:null},evidence:"QA passed",verificationResult:"PASS"}).success).toBe(true);
});
it("reuses valid detail and concurrent loads, and updates from receipt with zero followup GET",async()=>{
 const transport=api(),m=new OpsInvestigationModel(scope,transport);await Promise.all([m.load("event"),m.load("event")]);await m.load("event");expect(transport.read).toHaveBeenCalledTimes(1);
 await m.submit("event",command,"original");expect(m.get("event")?.investigation.version).toBe(1);await m.load("event");expect(transport.read).toHaveBeenCalledTimes(1);
});
it("keeps uncertain original request and blocks a different command until original-key recovery",async()=>{
 const transport=api();transport.write.mockRejectedValueOnce({status:503,errorCode:"ops_investigation_outcome_unknown"});const m=new OpsInvestigationModel(scope,transport);await expect(m.submit("event",command,"original")).rejects.toBeDefined();expect(m.pending?.phase).toBe("uncertain");
 await expect(m.submit("event",{...command,reason:"changed"},"new")).rejects.toThrow("OPS_PENDING_RECOVERY_REQUIRED");
 await m.recover();expect(transport.write.mock.calls[1]).toEqual(["event",command,"original"]);expect(m.pending).toBeNull();expect(m.get("event")?.investigation.version).toBe(1);
});
it("applies conflict current but never rewinds from original immutable receipt",async()=>{
 const transport=api();transport.write.mockRejectedValueOnce({status:409,errorCode:"ops_investigation_version_conflict",current:state(3,"verifying")});const m=new OpsInvestigationModel(scope,transport);
 await expect(m.submit("event",command,"original")).rejects.toBeDefined();expect(m.get("event")?.investigation.version).toBe(3);
 await m.submit("event",command,"replay-key");expect(m.get("event")?.investigation.version).toBe(3);expect(m.lastReceipt?.investigation.version).toBe(1);
});
it("clears protected data on 403 and ignores a previously started read",async()=>{
 let complete!:(v:unknown)=>void;const transport=api();transport.read.mockImplementationOnce(()=>new Promise(resolve=>{complete=resolve as any}));transport.write.mockRejectedValueOnce({status:403});const m=new OpsInvestigationModel(scope,transport);const loading=m.load("event");await expect(m.submit("event",command,"original")).rejects.toBeDefined();complete(read());await loading;expect(m.get("event")).toBeUndefined();expect(m.pending).toBeNull();
});
it("disposal prevents late writes, notifications and resubscription",async()=>{
 let complete!:(v:unknown)=>void;const transport=api();transport.read.mockImplementationOnce(()=>new Promise(resolve=>{complete=resolve as any}));const m=new OpsInvestigationModel(scope,transport),listener=vi.fn();m.subscribe(listener);const loading=m.load("event");m.dispose();complete(read());await loading;expect(m.get("event")).toBeUndefined();expect(listener).not.toHaveBeenCalled();expect(()=>m.subscribe(listener)).toThrow("OPS_MODEL_DISPOSED");
});

it("an explicit recurrence refresh accepts equal-version projection without a success-triggered read",async()=>{
 const transport=api(),m=new OpsInvestigationModel(scope,transport);await m.submit("event",command,"original");expect(m.get("event")?.recurrence).toBeNull();
 transport.read.mockResolvedValueOnce({...read(1,"acknowledged"),recurrence:{available:false,reason:"NOT_RESOLVED_WITH_BUILD"}});await m.load("event",true);expect(m.get("event")?.recurrence).toEqual({available:false,reason:"NOT_RESOLVED_WITH_BUILD"});expect(transport.read).toHaveBeenCalledTimes(1);
});
it("403 invalidates further use of this authorization generation",async()=>{
 const transport=api();transport.write.mockRejectedValueOnce({status:403});const m=new OpsInvestigationModel(scope,transport);await expect(m.submit("event",command,"original")).rejects.toBeDefined();await expect(m.submit("event",command,"new")).rejects.toThrow("OPS_AUTHORIZATION_INVALIDATED");expect(transport.write).toHaveBeenCalledTimes(1);
});

it("401 clears uncertain pending and cached state and isolates an older in-flight response",async()=>{
 let complete!:(v:unknown)=>void;const transport=api(),m=new OpsInvestigationModel(scope,transport);await m.load("event");transport.read.mockImplementationOnce(()=>new Promise(resolve=>{complete=resolve as any}));const late=m.load("event",true);
 transport.write.mockRejectedValueOnce({status:503});await expect(m.submit("event",command,"original")).rejects.toBeDefined();expect(m.pending?.phase).toBe("uncertain");transport.write.mockRejectedValueOnce({status:401});await expect(m.recover()).rejects.toBeDefined();complete(read());await late;expect(m.pending).toBeNull();expect(m.get("event")).toBeUndefined();await expect(m.submit("event",command,"new")).rejects.toThrow("OPS_AUTHORIZATION_INVALIDATED");
});
