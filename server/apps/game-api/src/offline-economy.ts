import { launchOfflineRewardsConfig, type OfflineRewardsConfig, type OfflineStoryProgressKey } from "../../../packages/balance/src/index";
import { readEconomyBinding } from "./economy-binding";

// Existing story nodes, not the four operation categories. Never derive these IDs
// from the UI catalog or silently replace existing account progression keys.
const progress: OfflineStoryProgressKey[] = ["none", "opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop",
  "opn_04_scrapstar_show", "opn_05_night_boarders", "opn_06_broken_line", "raid_raider_king_engine"];
const currencies = ["cur_credits", "cur_machine_parts", "cur_tactical_data", "cur_operator_exp"];

export async function readOfflineEconomy(db: D1Database) {
  const active = await db.prepare("SELECT version FROM balance_config_active WHERE singleton_id=1").first<{ version: number }>();
  const binding = active ? await readEconomyBinding(db, active.version) : null;
  const metadata = { balanceVersion: active?.version ?? null, economyVersion: binding?.version ?? null, economyHash: binding?.sha256 ?? null };
  if (!binding) return { config: launchOfflineRewardsConfig, metadata };
  const offline = binding.config.offline;
  const config: OfflineRewardsConfig = {
    ...launchOfflineRewardsConfig,
    minimumOfflineMinutes: offline.minimumSeconds / 60,
    maxClaimableHours: Math.max(...offline.capHours),
    storyProgressMultipliersBps: Object.fromEntries(progress.map((id, index) => [id, offline.progressBps[index]!])) as Record<OfflineStoryProgressKey, number>,
    storageCapHours: Object.fromEntries(progress.map((id, index) => [id, offline.capHours[index]!])) as Record<OfflineStoryProgressKey, number>,
    baseHourlyRewards: currencies.map((id, index) => ({ kind: "CURRENCY", id, quantity: offline.hourly[index]! })),
  };
  return { config, metadata };
}

export function offlinePolicyGuard(db: D1Database, metadata: Awaited<ReturnType<typeof readOfflineEconomy>>["metadata"]) {
  return db.prepare(`INSERT INTO growth_write_guard(valid) SELECT CASE WHEN
    COALESCE((SELECT version FROM balance_config_active WHERE singleton_id=1),0)=?
    AND COALESCE((SELECT economy_version FROM economy_version_bindings WHERE balance_config_version=?),'')=?
    THEN 1 ELSE 0 END`).bind(metadata.balanceVersion ?? 0, metadata.balanceVersion, metadata.economyVersion ?? "");
}
