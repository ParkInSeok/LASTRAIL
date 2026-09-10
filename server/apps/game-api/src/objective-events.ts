import { qualifyObjectiveFinish, type ObjectiveMode } from "./recurring-policy";
const stories = ["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop", "opn_04_scrapstar_show", "opn_05_night_boarders", "opn_06_broken_line"];
export async function objectiveFinishEvent(db: D1Database, input: { accountId: string; runId: string; operationId: string; contentVersion: string | null; outcome: string; completedWaves: number; fullCompletion: boolean }) {
  const binding = await db.prepare("SELECT mode,version FROM objective_operation_bindings WHERE operation_id=? AND content_version=?").bind(input.operationId, input.contentVersion).first<{ mode: ObjectiveMode; version: string }>();
  // The existing six story identities are not ordinary category IDs. Known source
  // mappings cannot be overridden to bypass the pending story-defeat contract.
  const known = stories.includes(input.operationId) ? "STORY_15" : input.operationId === "raid_raider_king_engine" ? "STORY_BOSS" : null;
  const mode: ObjectiveMode = known ?? binding?.mode ?? "MAPPING_MISSING";
  const qualification = qualifyObjectiveFinish(mode, input.outcome, input.completedWaves, input.fullCompletion);
  return db.prepare(`INSERT INTO objective_events(account_id,kind,source_id,occurred_at,mode,binding_version,status,ends,waves,source_json)
    SELECT ?1,'FINISH',?2,CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER),?3,?4,?5,?6,?7,?8
    FROM runs r JOIN run_final_results f ON f.run_id=r.id WHERE r.id=?2 AND r.account_id=?1
    ON CONFLICT(account_id,kind,source_id) DO NOTHING`)
    .bind(input.accountId, input.runId, mode, known ? "existing-story-identities-v1" : binding?.version ?? null,
      qualification.status, qualification.ends, qualification.waves, JSON.stringify(input));
}
export function objectiveLoginEvent(db: D1Database, accountId: string) {
  return db.prepare(`INSERT INTO objective_events(account_id,kind,source_id,occurred_at,mode,binding_version,status,ends,waves,source_json)
    SELECT ?,'LOGIN',CAST(CAST((clock.now-68400000)/86400000 AS INTEGER) AS TEXT),clock.now,'LOGIN','server-login-v1','ELIGIBLE',0,0,'{}'
    FROM (SELECT CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER) AS now) clock
    WHERE 1 ON CONFLICT(account_id,kind,source_id) DO NOTHING`).bind(accountId);
}
