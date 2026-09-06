import { launchGachaDataDraft, launchRunRewardsConfig } from "../../../../../server/packages/balance/src/index.ts";

type Database = { run(sql: string, values?: unknown[]): void; exec(sql: string): Array<{ values: unknown[][] }> };

// Preparation only; actual runs and pulls are created by Game API HTTP requests.
export function seedGameReadEvidence(db: Database, accountId: string) {
  const now = Date.now();
  db.run("INSERT INTO balance_config_drafts (id,base_version,schema_version,required_content_version,config_json,checksum,revision,status,created_by,created_at,updated_at) VALUES ('local-read-draft',NULL,1,1,'{}','local-only',1,'PUBLISHED','local-only',?,?)", [now, now]);
  db.run("INSERT INTO balance_config_versions (version,schema_version,required_content_version,checksum,config_json,published_at,published_by,draft_id) VALUES (901,1,1,'local-only',?,?,'local-only','local-read-draft')", [JSON.stringify({ runRewards: launchRunRewardsConfig }), now]);
  db.run("INSERT INTO account_items (account_id,item_id,quantity,updated_at) VALUES (?,'cur_command_ticket',10,?)", [accountId, now]);
  db.run("INSERT INTO gacha_banners (id,banner_type,status,balance_config_version,pickup_operator_id,starts_at,ends_at,pity_config_json,pool_snapshot_json,created_at,updated_at) VALUES ('local-read-banner','PICKUP','ACTIVE',901,'op_vex',?,NULL,?,?,?,?)", [now - 1, JSON.stringify({ elite: 30, pickup: 60, rare: 10 }), JSON.stringify(launchGachaDataDraft.banners[0].pool), now, now]);
  db.run("INSERT INTO account_mutations (id,account_id,sequence,mutation_type,idempotency_key,created_at) SELECT 'local-preview-mutation',?,COALESCE(MAX(sequence),0)+1,'LOCAL_PREVIEW_SEED','local-preview-seed',? FROM account_mutations WHERE account_id=?", [accountId, now, accountId]);
  db.run("INSERT INTO operation_previews (id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at) VALUES ('local-read-preview',?,'local-preview-mutation','opn_03_water_stop','III',901,?,'ACTIVE',?,?)", [accountId, JSON.stringify({ operationId: "opn_03_water_stop", actId: "act_01_wasteland_railway", regionId: "region_wasteland_railway", encounterType: "ORDINARY_15_WAVE", startAugmentPackageRef: "local-start-augments", startTacticPackageRef: "local-start-tactics" }), now + 900000, now]);
}
