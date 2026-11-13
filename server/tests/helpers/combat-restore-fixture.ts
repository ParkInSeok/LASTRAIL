import type {HostRestoreVerifier} from "../../apps/game-api/src/combat-host-resume";
import {z} from "zod";
import {sha256Hex} from "@lastrail/common";
/** Synthetic evidence-only fixtures explicitly seed an opaque checkpoint as test data.
 * No complete-state or transaction verification is provided by this helper.
 * Production preparation must never copy this unconditional guard/fixture insertion. */
export function guardedRestoreFixture(db:D1Database,fixture:{verify(input:Parameters<HostRestoreVerifier["prepare"]>[0]):Promise<unknown>}):HostRestoreVerifier{
 return {prepare:async input=>{
  const evidence=await fixture.verify(input),checkpointId=z.object({checkpointId:z.string()}).parse(evidence).checkpointId;
  await db.prepare("INSERT OR IGNORE INTO combat_checkpoints VALUES(?,?,?,?,?,?)").bind(input.runId,checkpointId,"synthetic-fixture",await sha256Hex("synthetic-fixture"),input.currentContextJson,input.startSnapshotJson).run();
  return {evidence:input.augmentJournal.hasEntries?{...z.record(z.string(),z.unknown()).parse(evidence),augmentCursor:input.augmentJournal.cursor}:evidence,assertions:[
   db.prepare("INSERT INTO combat_host_write_guard(valid) VALUES(1)")
  ]};
 }};
}
