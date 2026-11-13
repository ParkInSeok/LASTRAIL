import { sha256Hex } from "@lastrail/common";
import { parseLaunchEconomy } from "../../../packages/balance/src/launch-economy";
import { ActivityError } from "./activity-policy";

export async function readEconomyBinding(db: D1Database, balanceVersion?: number) {
  const selected = balanceVersion ?? (await db.prepare("SELECT version FROM balance_config_active WHERE singleton_id=1")
    .first<{ version: number }>())?.version;
  if (selected === undefined) return null;
  const row = await db.prepare(`SELECT v.version,v.sha256,v.config_json FROM economy_version_bindings b
    JOIN economy_versions v ON v.version=b.economy_version WHERE b.balance_config_version=?`)
    .bind(selected).first<{ version: string; sha256: string; config_json: string }>();
  if (!row) return null;
  try {
    if (await sha256Hex(row.config_json) !== row.sha256) throw new Error("hash mismatch");
    return { config: parseLaunchEconomy(JSON.parse(row.config_json), row.version), version: row.version,
      sha256: row.sha256, balanceVersion: selected };
  } catch { throw new ActivityError(503, "economy_binding_invalid"); }
}
