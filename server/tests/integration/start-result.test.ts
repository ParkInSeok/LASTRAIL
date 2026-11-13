import { Buffer } from "node:buffer";
import { createServer } from "node:http";
import { writeFileSync } from "node:fs";
import { z } from "zod";
import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { setup } from "../helpers/departure-fixture";

const path = "/v1/runs/start-result";
describe("read-only start receipt lookup without run ID", () => {
  it('preserves an old run receipt after another run starts without reverting current fuel',async()=>{
    const f=await setup(true);
    try{
      const parsePreview=z.object({operationPreviewId:z.string()}),parseStart=z.object({runId:z.string()}).passthrough();
      const oldPreview=parsePreview.parse(await(await f.preview()).json());
      const old=parseStart.parse(await(await f.start(oldPreview.operationPreviewId,'old-run-key')).json());
      f.sql.run("UPDATE runs SET status='COMPLETED' WHERE id=?",[old.runId]);
      const nextPreview=parsePreview.parse(await(await f.preview()).json());
      const next=parseStart.parse(await(await f.start(nextPreview.operationPreviewId,'new-run-key')).json());
      expect(next.runId).not.toBe(old.runId);
      const before=f.sql.export();
      expect(await(await f.post('/v1/runs/start',{operationPreviewId:oldPreview.operationPreviewId},'old-run-key')).json()).toEqual(old);
      expect((await f.start(nextPreview.operationPreviewId,'old-run-key')).status).toBe(409);
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
      expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(2);expect(f.scalar('SELECT COUNT(*) FROM fuel_ledger')).toBe(2);
      expect(f.scalar('SELECT fuel FROM fuel_state')).toBe(10);
    }finally{f.sql.close();}
  });
  it.each(['public','core'] as const)('rejects inconsistent account and run identity on %s START replay without creating another run',async surface=>{
    for(const corruption of ["UPDATE runs SET account_id='b'", "UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.departureResponse.runId','another-run')", "UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.departureResponse.operationPreviewId','another-preview')"]){
      const f=await setup(true);
      try{
        const p=z.object({operationPreviewId:z.string()}).parse(await(await f.preview()).json());
        expect((await f.start(p.operationPreviewId,'original-run')).status).toBe(200);
        f.sql.run(corruption);const before=f.sql.export();
        const response=surface==='public'?await f.post('/v1/runs/start',{operationPreviewId:p.operationPreviewId},'original-run'):await f.start(p.operationPreviewId,'original-run');
        expect(response.status).toBeGreaterThanOrEqual(400);
        expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
        expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);expect(f.scalar('SELECT COUNT(*) FROM fuel_ledger')).toBe(1);
      }finally{f.sql.close();}
    }
  });
  it("recovers the same committed run over real HTTP after local entry failure and selection changes", async () => {
    const f = await setup(true);
    const p = z.object({operationPreviewId:z.string()}).parse(await (await f.preview()).json());
    const committed = await f.start(p.operationPreviewId, "entry-failed-original");
    expect(committed.status).toBe(200);
    const original = z.object({ runId: z.string(), admission: z.object({formationRevision:z.number(),loadoutRevision:z.number()}).passthrough() }).passthrough().parse(await committed.json());
    // A failed local scene transition has no server mutation or refund command.
    f.sql.run("UPDATE operation_previews SET expires_at=0");
    f.sql.run("UPDATE account_formation SET revision=revision+1");
    const before = f.sql.export(), trace: Array<{method:string;path:string;status:number;bytes:number}> = [];
    const recoveryPath = `/v1/runs/${original.runId}/start-state`;
    const server = createServer(async (req,res) => {
      if (!(req.method==='POST' && req.url===path) && !(req.method==='GET' && req.url===recoveryPath)) { res.writeHead(404);res.end();return; }
      try {
        const chunks:Buffer[]=[];for await(const chunk of req)chunks.push(Buffer.from(chunk));
        const headers=new Headers();for(const [key,value] of Object.entries(req.headers))if(typeof value==='string')headers.set(key,value);
        const response=await app.fetch(new Request('http://127.0.0.1'+req.url,{method:req.method,headers,...(req.method==='POST'?{body:Buffer.concat(chunks)}:{})}),f.env);
        const bytes=Buffer.from(await response.arrayBuffer());trace.push({method:req.method!,path:req.url!,status:response.status,bytes:bytes.length});
        res.writeHead(response.status,Object.fromEntries(response.headers));res.end(bytes);
      }catch{res.writeHead(500);res.end();}
    });
    try {
      await new Promise<void>((ok,no)=>{server.once('error',no);server.listen(0,'127.0.0.1',()=>{server.off('error',no);ok();});});
      const address=server.address();if(!address||typeof address==='string')throw Error('bind');const base='http://127.0.0.1:'+address.port;
      for(let attempt=0;attempt<2;attempt++){
        const response=await fetch(base+path,{method:'POST',headers:f.headers,body:JSON.stringify({startIdempotencyKey:'entry-failed-original',operationPreviewId:p.operationPreviewId})});
        expect(response.status).toBe(200);expect(await response.json()).toMatchObject({lookupStatus:'COMMITTED',runId:original.runId,startResponse:original});
      }
      const byRun=await fetch(base+recoveryPath,{headers:f.headers});expect(byRun.status).toBe(200);
      expect(await byRun.json()).toMatchObject({runStatus:'ACTIVE',recoveryStatus:'AVAILABLE',startResponse:original});
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
      expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);expect(f.scalar('SELECT COUNT(*) FROM fuel_ledger')).toBe(1);
      expect(f.scalar('SELECT fuel FROM fuel_state')).toBe(20);
      writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-departure-readiness-http-20261021.json',JSON.stringify({
        classification:'SUPPORTED_LOCAL_CORE_COMMIT_THEN_REAL_AUTHENTICATED_HTTP_RECOVERY_SYNTHETIC_MEMORY_ONLY',trace,
        originalAdmission:original.admission,originalRunRecovered:true,databaseUnchangedByRecovery:true,runs:1,fuelDebits:1,fuelAfter:20,
        publicStartEnabled:false,sceneTransition:'NOT_RUN_CLIENT_FAILURE_SCENARIO',processRestart:'NOT_RUN',player:'NOT_RUN',memory:'NOT_MEASURED'
      },null,2));
    }finally{server.closeAllConnections();if(server.listening)await new Promise<void>(ok=>server.close(()=>ok()));f.sql.close();}
  });
  it("reports current recovery phase and original start over HTTP without granting resume authority or writing state", async () => {
    const f=await setup(true);
    const p=z.object({operationPreviewId:z.string()}).parse(await (await f.preview()).json());
    const original=z.object({runId:z.string()}).passthrough().parse(await (await f.start(p.operationPreviewId,'restore-resume-original')).json());
    const route=`/v1/runs/${original.runId}/lifecycle`;
    const trace:Array<{status:number;bytes:number;phase:string}>=[];
    const server=createServer(async(req,res)=>{
      if(req.method!=='GET'||req.url!==route){res.writeHead(404);res.end();return;}
      try{
        const headers=new Headers();for(const [key,value] of Object.entries(req.headers))if(typeof value==='string')headers.set(key,value);
        const response=await app.fetch(new Request('http://127.0.0.1'+route,{headers}),f.env);
        const bytes=Buffer.from(await response.arrayBuffer());
        res.writeHead(response.status,Object.fromEntries(response.headers));res.end(bytes);
      }catch{res.writeHead(500);res.end();}
    });
    try{
      await new Promise<void>((ok,no)=>{server.once('error',no);server.listen(0,'127.0.0.1',()=>{server.off('error',no);ok();});});
      const address=server.address();if(!address||typeof address==='string')throw Error('bind');
      const url='http://127.0.0.1:'+address.port+route;
      // Synthetic phase changes exercise recovery routing, not actual combat completion.
      for(const [phase,nextAction] of [['ACTIVE','RECOVER_COMBAT_CONSUMER'],['FINISH_PENDING','RESUME_FINISH'],['QUARANTINED','REVIEW_REQUIRED']] as const){
        f.sql.run('UPDATE runs SET status=? WHERE id=?',[phase,original.runId]);
        const before=f.sql.export();
        for(let retry=0;retry<2;retry++){
          const response=await fetch(url,{headers:f.headers}),text=await response.text();
          expect(response.status).toBe(200);expect(response.headers.get('cache-control')).toBe('no-store');
          const state=JSON.parse(text);
          expect(state).toMatchObject({runId:original.runId,runStatus:phase,startResponse:original,nextAction,
            grantsOnRead:false,newAttemptAuthorized:false,augmentRecovery:{runtimeApplied:null,newCommandAuthorized:false,localCheckpointStatus:'NOT_OBSERVED'}});
          expect(Number.isSafeInteger(state.serverTime)).toBe(true);
          expect(state.serverTime).toBeGreaterThan(0);
          expect(state).not.toHaveProperty('combatElapsedMs');
          expect(state).not.toHaveProperty('resumeAuthorized');
          trace.push({status:response.status,bytes:Buffer.byteLength(text),phase});
        }
        expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
      }
      f.sql.run("UPDATE auth_sessions SET status='REVOKED'");
      expect((await fetch(url,{headers:f.headers})).status).toBe(401);
      expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);
      expect(f.scalar('SELECT COUNT(*) FROM fuel_ledger')).toBe(1);
      writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-restore-resume-http-20261021.json',JSON.stringify({
        classification:'REAL_HTTP_AUTH_SYNTHETIC_SQL_PHASES_MEMORY_ONLY',trace,readMutations:0,originalStartPreserved:true,
        resumeButtonRequests:'NOT_MEASURED_UNITY_OWNER',serverTime:'WALL_CLOCK_NOT_COMBAT_ELAPSED',
        player:'NOT_RUN',processRestart:'NOT_RUN',memory:'NOT_MEASURED'
      },null,2));
    }finally{server.closeAllConnections();if(server.listening)await new Promise<void>(ok=>server.close(()=>ok()));f.sql.close();}
  });
  it("recovers a committed response lost by the client without another start or any database change", async () => {
    const f = await setup(true), p = z.object({operationPreviewId:z.string()}).parse(await (await f.preview()).json());
    await f.start(p.operationPreviewId, "lost-response"); // Journal never obtained the run ID.
    const original = JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots"))).departureResponse;
    const before = f.sql.export();
    const response = await f.post(path, { startIdempotencyKey: "lost-response", operationPreviewId: p.operationPreviewId });
    expect(response.status).toBe(200); expect(response.headers.get("Cache-Control")).toBe("no-store");
    expect(await response.json()).toEqual({ accepted: true, lookupStatus: "COMMITTED", runId: original.runId, runStatus: "ACTIVE", startResponse: original, errorCode: null });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1); expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(20);
  });
  it("distinguishes unknown/in-flight lookup from a later commit and never starts on lookup", async () => {
    const f = await setup(true), p = z.object({operationPreviewId:z.string()}).parse(await (await f.preview()).json());
    const body = { startIdempotencyKey: "pending", operationPreviewId: p.operationPreviewId };
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "NOT_FOUND", runId: null, startResponse: null });
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    const pending = f.start(p.operationPreviewId, "pending");
    const during = z.object({lookupStatus:z.string()}).parse(await (await f.post(path, body)).json()); expect(["NOT_FOUND", "COMMITTED"]).toContain(during.lookupStatus);
    await pending;
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "COMMITTED" });
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
  });
  it("isolates account keys and rejects missing/revoked authentication", async () => {
    const f = await setup(true), p = z.object({operationPreviewId:z.string()}).parse(await (await f.preview()).json()); await f.start(p.operationPreviewId, "private-key");
    f.sql.run("UPDATE idempotency_records SET account_id='b' WHERE operation='run_start'");
    const body = { startIdempotencyKey: "private-key", operationPreviewId: p.operationPreviewId };
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "NOT_FOUND", runId: null });
    f.sql.run("UPDATE idempotency_records SET account_id='a' WHERE operation='run_start'");
    f.sql.run("UPDATE runs SET account_id='b'");
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "SNAPSHOT_UNAVAILABLE", runId: null, runStatus: null, startResponse: null });
    expect((await app.request(path, { method: "POST", body: JSON.stringify(body) }, f.env)).status).toBe(401);
    f.sql.run("UPDATE auth_sessions SET status='REVOKED'"); expect((await f.post(path, body)).status).toBe(401);
  });
  it("does not disclose another preview or stored response on same-key mismatch", async () => {
    const f = await setup(true), p = z.object({operationPreviewId:z.string()}).parse(await (await f.preview()).json()); await f.start(p.operationPreviewId, "secret-key");
    const before = f.sql.export();
    const response = await f.post(path, { startIdempotencyKey: "secret-key", operationPreviewId: "different_preview" });
    expect(response.status).toBe(409);
    expect(await response.json()).toEqual({ accepted: false, lookupStatus: "CONFLICT", runId: null, runStatus: null, startResponse: null, errorCode: "start_preview_mismatch" });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  });
  it("reports missing/corrupt snapshots distinctly from no receipt", async () => {
    const f = await setup(true), p = z.object({operationPreviewId:z.string()}).parse(await (await f.preview()).json()); await f.start(p.operationPreviewId, "committed");
    const body = { startIdempotencyKey: "committed", operationPreviewId: p.operationPreviewId };
    f.sql.run("UPDATE run_snapshots SET snapshot_json='invalid'");
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "SNAPSHOT_UNAVAILABLE", startResponse: null });
    f.sql.run("DELETE FROM run_snapshots");
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "SNAPSHOT_UNAVAILABLE", startResponse: null });
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
  });
  it("returns sanitized temporary failure and rejects URL keys and extra fields", async () => {
    const f = await setup(); const body = { startIdempotencyKey: "never-log-this-key", operationPreviewId: "preview_x" };
    expect((await f.post(path + "?startIdempotencyKey=secret", body)).status).toBe(400);
    expect((await f.post(path, { ...body, accountId: "b" })).status).toBe(400);
    const db = f.env.DB, prepare = db.prepare.bind(db);
    db.prepare = ((sql: string) => {
      if (sql.includes("FROM idempotency_records")) throw new Error("temporary failure never-log-this-key");
      return prepare(sql);
    }) as typeof db.prepare;
    const response = await f.post(path, body); expect(response.status).toBe(503);
    const text = await response.text(); expect(text).toContain('"lookupStatus":"TEMPORARILY_UNAVAILABLE"'); expect(text).not.toContain(body.startIdempotencyKey);
    expect(f.scalar("SELECT COUNT(*) FROM ops_error_logs")).toBe(0);
  });
});
