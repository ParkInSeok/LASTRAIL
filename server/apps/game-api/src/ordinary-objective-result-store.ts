import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { OrdinaryFirstObjectiveVerifier, firstObjectiveEvidenceSchema, type FirstObjectiveEvidenceOwner } from './combat-first-objective-verifier';
import { OperationMapStore, type OperationMapVerifier } from './operation-map-store';

type Context = Parameters<FirstObjectiveEvidenceOwner['prepare']>[0];
const json = (value: unknown) => canonicalJson(z.json().parse(value));
const fail = (reason: string): never => { throw new ActivityError(409, 'ordinary_objective_' + reason); };

/** Internal trusted execution boundary, never a raw Unity checkpoint/HP upload.
 * Acceptance and COMPLETE share one transaction so mutable hold/source guards cannot expire between them. */
export class OrdinaryObjectiveResultStore implements OperationMapVerifier {
  constructor(private readonly db: D1Database, private readonly source?: FirstObjectiveEvidenceOwner) {}
  async prepare(context: Context) {
    if (context.command.kind !== 'COMPLETE') return fail('command_unsupported');
    const reference = context.command.evidenceRef, hash = await sha256Hex(json(context));
    if (!this.source) return fail('evidence_unavailable');
    let supplied: Awaited<ReturnType<FirstObjectiveEvidenceOwner['prepare']>> | undefined;
    const verified = await new OrdinaryFirstObjectiveVerifier(this.db, { prepare: async checked => {
      supplied = await this.source!.prepare(checked);
      return supplied;
    } }).prepare(context);
    if (!supplied) return fail('evidence_invalid');
    const evidence = firstObjectiveEvidenceSchema.parse(supplied.evidence);
    return { proof: verified.proof, assertions: [...verified.assertions,
      this.db.prepare('INSERT INTO ordinary_objective_results VALUES(?,?,?,?,?,?)')
        .bind(context.runId, reference, context.accountId, hash, json(evidence), json(supplied.holdIds))] };
  }
  async accept(context: Context) {
    if (context.command.kind !== 'COMPLETE') return fail('command_unsupported');
    const reference = context.command.evidenceRef, hash = await sha256Hex(json(context));
    const read = async () => {
      const row = await this.db.prepare('SELECT account_id,request_hash FROM ordinary_objective_results WHERE run_id=? AND evidence_ref=?')
        .bind(context.runId, reference).first<{ account_id: string; request_hash: string }>();
      if (row && (row.account_id !== context.accountId || row.request_hash !== hash)) return fail('result_conflict');
      return row;
    };
    await read();
    const store = new OperationMapStore(this.db, { prepare: async current => {
      if (await sha256Hex(json(current)) !== hash) return fail('result_conflict');
      return this.prepare(current);
    } });
    const receipt = await store.commit(context.accountId, context.runId, context.command);
    // A historical COMPLETE is not permission to backfill a missing acceptance record.
    if (!await read()) return fail('result_unavailable');
    return { version: 'ordinary.objective-result-acceptance.v1.20261102', evidenceRef: reference, requestHash: hash, receipt };
  }
}
