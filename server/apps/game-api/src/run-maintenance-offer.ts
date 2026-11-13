import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { OperationMapStore } from './operation-map-store';
import { mapReceipt } from './operation-map-state';
import { RunSuppliesStore } from './run-supplies-start';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const service = z.strictObject({ kind: z.enum(['LOCOMOTIVE_REPAIR', 'CARRIAGE_REPAIR', 'CARRIAGE_RESTORE']),
  price: z.number().int().positive(), stock: z.number().int().positive() });
const servicesSchema = z.array(service).length(3);
const repairServices = servicesSchema.parse([
  { kind: 'LOCOMOTIVE_REPAIR', price: 20, stock: 2 },
  { kind: 'CARRIAGE_REPAIR', price: 15, stock: 2 },
  { kind: 'CARRIAGE_RESTORE', price: 30, stock: 1 },
]);
type OfferRow = { station_id: string; source_ref: string; source_version: string;
  services_json: string; payload_hash: string };

export interface RunMaintenanceSourceVerifier {
  /** Prove actual safe station arrival, station identity and supported profile in
   * the same batch. No production adapter or public route is installed. */
  prepare(context: { accountId: string; runId: string; stationId: string; sourceRef: string }):
    Promise<{ sourceVersion: string; assertions: D1PreparedStatement[] }>;
}

/** Fixes only approved repair stock; payment, target HP and service effects are separate transactions. */
export class RunMaintenanceOfferStore {
  constructor(private readonly db: D1Database, private readonly verifier?: RunMaintenanceSourceVerifier) {}

  private original(runId: string, stationId: string): Promise<OfferRow | null> {
    return this.db.prepare(`SELECT station_id,source_ref,source_version,services_json,payload_hash
      FROM run_maintenance_offers WHERE run_id=? AND station_id=?`).bind(runId, stationId).first<OfferRow>();
  }

  async read(accountId: string, runId: string, stationId: string) {
    const pin = await new RunSuppliesStore(this.db).read(accountId, runId);
    if (!pin) throw new ActivityError(409, 'run_supplies_legacy_unsupported');
    const row = await this.original(runId, stationId);
    if (!row) throw new ActivityError(409, 'run_maintenance_offer_not_found');
    try {
      const services = servicesSchema.parse(JSON.parse(row.services_json));
      if (canonicalJson(services) !== canonicalJson(repairServices)
        || await sha256Hex(canonicalJson({ sourceVersion: row.source_version, services })) !== row.payload_hash) {
        throw new Error('maintenance offer mismatch');
      }
      return { version: 'run.maintenance-offer.v1' as const, stationId: row.station_id,
        sourceRef: row.source_ref, sourceVersion: row.source_version, services };
    } catch { throw new ActivityError(503, 'run_maintenance_storage_invalid'); }
  }

  async publish(accountId: string, runId: string, stationId: string, sourceRef: string) {
    if (!id.safeParse(stationId).success || !id.safeParse(sourceRef).success) {
      throw new ActivityError(400, 'run_maintenance_request_invalid');
    }
    const pin = await new RunSuppliesStore(this.db).read(accountId, runId);
    if (!pin) throw new ActivityError(409, 'run_supplies_legacy_unsupported');
    const existing = await this.original(runId, stationId);
    if (existing) {
      if (existing.source_ref !== sourceRef) throw new ActivityError(409, 'run_maintenance_source_conflict');
      return this.read(accountId, runId, stationId);
    }
    const usedSource = await this.db.prepare('SELECT station_id FROM run_maintenance_offers WHERE run_id=? AND source_ref=?')
      .bind(runId, sourceRef).first<{ station_id: string }>();
    if (usedSource) throw new ActivityError(409, 'run_maintenance_source_conflict');
    const run = await this.db.prepare('SELECT status,snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?')
      .bind(runId, accountId).first<{ status: string; snapshot_json: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'run_supplies_run_not_active');
    const map = await new OperationMapStore(this.db).read(accountId, runId);
    if (map.state.phase !== 'ACTIVE' || map.view.currentNodeId !== stationId
      || map.view.nodes.find(node => node.nodeId === stationId)?.eventKind !== 'MAINTENANCE') {
      throw new ActivityError(409, 'run_maintenance_station_unavailable');
    }
    const mapRow = await this.db.prepare(`SELECT revision,plan_json,start_snapshot_json,state_json
      FROM operation_map_states WHERE run_id=?`).bind(runId)
      .first<{ revision: number; plan_json: string; start_snapshot_json: string; state_json: string }>();
    if (!mapRow || mapRow.start_snapshot_json !== run.snapshot_json || mapRow.revision !== map.revision
      || canonicalJson(JSON.parse(mapRow.state_json)) !== canonicalJson(map.state)) {
      throw new ActivityError(409, 'run_maintenance_state_conflict');
    }
    const receipts = await this.db.prepare(`SELECT event_id,response_json FROM operation_map_receipts
      WHERE run_id=? AND json_extract(response_json,'$.verificationId')=?`).bind(runId, sourceRef)
      .all<{ event_id: string; response_json: string }>();
    const arrivals = receipts.results.filter(row => {
      const receipt = mapReceipt.safeParse(JSON.parse(row.response_json));
      return receipt.success && receipt.data.eventId === row.event_id && receipt.data.state.phase === 'ACTIVE'
        && canonicalJson(receipt.data.state.traversedEdgeIds) === canonicalJson(map.state.traversedEdgeIds);
    });
    if (arrivals.length !== 1) throw new ActivityError(409, 'run_maintenance_arrival_unverified');
    const arrival = arrivals[0]!;
    if (!this.verifier) throw new ActivityError(409, 'run_maintenance_source_required');
    const proof = await this.verifier.prepare({ accountId, runId, stationId, sourceRef });
    if (!proof || !id.safeParse(proof.sourceVersion).success || !Array.isArray(proof.assertions)
      || proof.assertions.length === 0) throw new ActivityError(409, 'run_maintenance_source_required');
    const servicesJson = canonicalJson(repairServices);
    const hash = await sha256Hex(canonicalJson({ sourceVersion: proof.sourceVersion, services: repairServices }));
    try {
      await this.db.batch([
        ...proof.assertions,
        this.db.prepare(`INSERT INTO run_maintenance_offer_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_supplies_state w ON w.run_id=r.id
            JOIN run_supplies_ledger l ON l.run_id=r.id AND l.revision=w.revision
            JOIN operation_map_states m ON m.run_id=r.id
            JOIN operation_map_receipts ar ON ar.run_id=r.id AND ar.event_id=?
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND s.snapshot_json=? AND w.revision=? AND w.balance=? AND l.balance_after=w.balance
              AND m.revision=? AND m.state_json=? AND m.plan_json=? AND m.start_snapshot_json=s.snapshot_json
              AND ar.response_json=?
              AND w.policy_version=json_extract(s.snapshot_json,'$.runSupplies.version')
              AND w.do04_rank=json_extract(s.snapshot_json,'$.runSupplies.do04Rank')
              AND w.initial_balance=json_extract(s.snapshot_json,'$.runSupplies.initialBalance')
              AND NOT EXISTS(SELECT 1 FROM run_maintenance_offers o WHERE o.run_id=r.id
                AND (o.station_id=? OR o.source_ref=?))) THEN 1 ELSE 0 END`)
          .bind(arrival.event_id, runId, accountId, run.snapshot_json, pin.revision, pin.balance,
            mapRow.revision, mapRow.state_json, mapRow.plan_json, arrival.response_json, stationId, sourceRef),
        this.db.prepare(`INSERT INTO run_maintenance_offers
          (run_id,station_id,source_ref,source_version,services_json,payload_hash) VALUES(?,?,?,?,?,?)`)
          .bind(runId, stationId, sourceRef, proof.sourceVersion, servicesJson, hash),
        this.db.prepare('DELETE FROM run_maintenance_offer_write_guard'),
      ]);
    } catch (error) {
      const raced = await this.original(runId, stationId);
      if (raced) {
        if (raced.source_ref !== sourceRef || raced.payload_hash !== hash) {
          throw new ActivityError(409, 'run_maintenance_source_conflict');
        }
        return this.read(accountId, runId, stationId);
      }
      const collidedSource = await this.db.prepare('SELECT station_id FROM run_maintenance_offers WHERE run_id=? AND source_ref=?')
        .bind(runId, sourceRef).first<{ station_id: string }>();
      if (collidedSource) throw new ActivityError(409, 'run_maintenance_source_conflict');
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'run_maintenance_state_conflict');
      throw error;
    }
    return this.read(accountId, runId, stationId);
  }
}
