export const dailyKeys = ["sessionCreatedAccounts", "signups", "adRewardsVerified", "adRewardsGranted", "passPurchases", "adRevenue"] as const;
export type DailyKey = typeof dailyKeys[number];
type Values = Record<DailyKey, number | null>;
export type DailyMetrics = { available: Record<DailyKey, boolean>; points: Array<Values & { date: string }>; sevenDay: Values; thirtyDay: Values };
function record(value: unknown): Record<string, unknown> { return value && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {}; }
export function mapDailyMetrics(value: unknown): DailyMetrics | null {
  if (!value || typeof value !== "object") return null;
  const raw = record(value), source = record(raw.sourceAvailability);
  const available = Object.fromEntries(dailyKeys.map(key => [key, source[key.startsWith("adRewards") ? "adRewards" : key] === true])) as DailyMetrics["available"];
  const values = (value: unknown) => {
    const row = record(value);
    return Object.fromEntries(dailyKeys.map(key => [key, available[key] && typeof row[key] === "number" && Number.isFinite(row[key]) && (row[key] as number) >= 0 ? row[key] : null])) as Values;
  };
  const points = Array.isArray(raw.points) ? raw.points.flatMap(value => {
    const row = record(value);
    return typeof row.date === "string" && /^\d{4}-\d{2}-\d{2}$/.test(row.date) ? [{ date: row.date, ...values(row) }] : [];
  }).sort((a, b) => a.date.localeCompare(b.date)) : [];
  return { available, points, sevenDay: values(raw.sevenDay), thirtyDay: values(raw.thirtyDay) };
}
export function dailySeries(data: DailyMetrics | null | undefined, key: DailyKey, days: 7 | 30, calculatedAt: string) {
  if (!data?.available[key]) return [];
  const now = new Date(calculatedAt);
  if (!Number.isFinite(now.getTime())) return [];
  const kst = new Date(now.getTime() + 9 * 3600000);
  const end = Date.UTC(kst.getUTCFullYear(), kst.getUTCMonth(), kst.getUTCDate());
  const from = new Date(end - (days - 1) * 86400000).toISOString().slice(0, 10), to = new Date(end).toISOString().slice(0, 10);
  return data.points.filter(row => row.date >= from && row.date <= to && row[key] !== null).map(row => ({ date: row.date, value: row[key]! }));
}
