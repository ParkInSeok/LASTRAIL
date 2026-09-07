// Opt-in display fixtures only. Never used by remote seeds or game writes.
export async function seedRunVersionDisplayCases(db: any, d1: any, sourceId: string) {
  const run = await d1.prepare("SELECT * FROM runs WHERE id=?").bind(sourceId).first();
  const snapshot = await d1.prepare("SELECT * FROM run_snapshots WHERE run_id=?").bind(sourceId).first();
  const final = await d1.prepare("SELECT * FROM run_final_results WHERE run_id=?").bind(sourceId).first();
  const insert = (table: string, row: Record<string, unknown>) => {
    const keys = Object.keys(row);
    db.run(`INSERT INTO ${table} (${keys.join(",")}) VALUES (${keys.map(() => "?").join(",")})`, Object.values(row));
  };
  for (const kind of ["long", "legacy", "unfinished"]) {
    const id = `local-version-${kind}`;
    const start = JSON.parse(snapshot.snapshot_json);
    const finish = JSON.parse(final.payload_json);
    if (kind === "legacy") {
      delete start.runVersionSnapshot;
      delete start.runStartVersionSnapshot;
      delete finish.runFinishVersionSnapshot;
    }
    if (kind === "long") {
      for (const evidence of [start.runStartVersionSnapshot, start.runVersionSnapshot, finish.runFinishVersionSnapshot]) {
        if (!evidence) continue;
        evidence.clientInstalled.appVersion = "local-layout-" + "long-version-".repeat(12);
        evidence.serverAcceptedDeployment.runtimeDeploymentId = "local-worker-" + "reference-".repeat(12);
      }
    }
    insert("runs", { ...run, id, status: kind === "unfinished" ? "ACTIVE" : run.status, finished_at: kind === "unfinished" ? null : run.finished_at });
    insert("run_snapshots", { ...snapshot, run_id: id, snapshot_json: JSON.stringify(start) });
    if (kind !== "unfinished") insert("run_final_results", { ...final, run_id: id, payload_json: JSON.stringify(finish) });
  }
}
