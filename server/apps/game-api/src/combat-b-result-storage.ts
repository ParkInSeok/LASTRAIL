import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {bResultV2BodyLimit,bResultFullChunkUnits,bResultFullChunkBytes,bResultFullChunkCount} from './combat-b-result-budget';
const json=(v:unknown)=>canonicalJson(z.json().parse(v)),bytes=(v:string)=>new TextEncoder().encode(v).length;
export type BResultStorage={reportJson:string;chunks:{ordinal:number;text:string}[]};
const wholeVersion='combat.b-report-storage.v2.20261006';
const metaSchema=z.strictObject({version:z.literal(wholeVersion),chunkCount:z.number().int().min(1).max(bResultFullChunkCount),reportBytes:z.number().int().min(1).max(bResultV2BodyLimit),reportHash:z.string().regex(/^[a-f0-9]{64}$/u)});
const indexSchema=z.object({version:z.literal('combat.b-result-report.v2.20261006'),requestId:z.string(),reportOriginMapRevision:z.number(),checkpointHash:z.string(),nodeSession:z.object({node:z.object({nodeId:z.string()}),runtime:z.object({commands:z.array(z.object({eventId:z.string()})).min(1).max(10000)})})});
function stub(report:unknown,metadata:z.infer<typeof metaSchema>){const b=indexSchema.parse(report),commands:unknown[]=Array(b.nodeSession.runtime.commands.length).fill(null);commands[commands.length-1]={eventId:b.nodeSession.runtime.commands.at(-1)!.eventId};
 // Preserve existing node/cursor/final-event SQL unique indexes across old and new storage.
 return json({version:b.version,requestId:b.requestId,reportOriginMapRevision:b.reportOriginMapRevision,checkpointHash:b.checkpointHash,nodeSession:{node:{nodeId:b.nodeSession.node.nodeId},runtime:{commands}},reportStorage:metadata});
}
const conflict=():never=>{throw new ActivityError(409,'combat_b_result_report_storage_conflict');};
/** Full original v2 report including BOTH native/journal copies; private sparse index
 * plus immutable bounded fragments in the caller's same original report transaction.
 * canonicalReport is the caller's already computed complete request-hash input. */
export async function prepareBResultStorage(report:unknown,canonicalReport?:string):Promise<BResultStorage>{
 if(z.object({version:z.string()}).parse(report).version!=='combat.b-result-report.v2.20261006')return {reportJson:canonicalReport??json(report),chunks:[]};
 const text=canonicalReport??json(report),reportBytes=bytes(text);if(reportBytes>bResultV2BodyLimit)return conflict();
 const chunks:BResultStorage['chunks']=[];for(let i=0;i<text.length;){let end=Math.min(i+bResultFullChunkUnits,text.length);if(end<text.length&&/[\uD800-\uDBFF]/u.test(text[end-1]!))end--;const part=text.slice(i,end);if(bytes(part)>bResultFullChunkBytes)return conflict();chunks.push({ordinal:chunks.length,text:part});i=end;}
 const metadata=metaSchema.parse({version:wholeVersion,chunkCount:chunks.length,reportBytes,reportHash:await sha256Hex(text)});
 return {reportJson:stub(report,metadata),chunks};
}
export async function recoverBResultStorage(db:D1Database,runId:string,ref:string,reportJson:string,expectedRequestHash:string){
 const b=z.record(z.string(),z.json()).parse(JSON.parse(reportJson)),chunks:BResultStorage['chunks']=[];
 if(b.reportStorage!==undefined){const parsed=metaSchema.safeParse(b.reportStorage);if(!parsed.success||b.journalStorage!==undefined)return conflict();const m=parsed.data;
  const rows=await db.prepare('SELECT ordinal,text_json FROM b_result_journal_chunks WHERE run_id=? AND evidence_ref=? ORDER BY ordinal').bind(runId,ref).all<{ordinal:number;text_json:string}>();
  if(rows.results.length!==m.chunkCount||rows.results.some((v,i)=>v.ordinal!==i||v.text_json.length>bResultFullChunkUnits||bytes(v.text_json)>bResultFullChunkBytes))return conflict();
  for(const row of rows.results)chunks.push({ordinal:row.ordinal,text:row.text_json});const text=chunks.map(v=>v.text).join('');
  if(bytes(text)!==m.reportBytes||m.reportHash!==expectedRequestHash||await sha256Hex(text)!==expectedRequestHash)return conflict();
  let report:unknown;try{report=JSON.parse(text);if(stub(report,m)!==reportJson)return conflict();}catch{return conflict();}
  return {report:z.record(z.string(),z.json()).parse(report),storage:{reportJson,chunks}};
 }
 // Historical journal-only v2 rows remain exactly recoverable. No rewrite or downgrade.
 if(b.journalStorage!==undefined){const m=z.strictObject({version:z.literal('combat.b-journal-storage.v1.20261006'),chunkCount:z.number().int().min(1).max(32),journalHash:z.string()}).parse(b.journalStorage);
 const rows=await db.prepare('SELECT ordinal,text_json FROM b_result_journal_chunks WHERE run_id=? AND evidence_ref=? ORDER BY ordinal').bind(runId,ref).all<{ordinal:number;text_json:string}>();
 if(rows.results.length!==m.chunkCount||rows.results.some((v,i)=>v.ordinal!==i))throw new ActivityError(409,'combat_b_result_journal_storage_conflict');
 for(const row of rows.results)chunks.push({ordinal:row.ordinal,text:row.text_json});const text=chunks.map(v=>v.text).join('');if(m.journalHash!=='sha256:'+await sha256Hex(text))throw new ActivityError(409,'combat_b_result_journal_storage_conflict');
 const n=z.object({runtime:z.record(z.string(),z.json())}).passthrough().parse(b.nodeSession);n.runtime.originalEvents=z.json().parse(JSON.parse(text));b.nodeSession=z.json().parse(n);delete b.journalStorage;
 }
 if(await sha256Hex(json(b))!==expectedRequestHash)return conflict();
 return {report:b,storage:{reportJson,chunks}};
}