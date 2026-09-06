export function opsLogSearch(category: string | undefined, search: string | undefined) {
  if (category && category !== "server" && category !== "frontend") throw new Error("invalid_log_category");
  const q = search?.trim() ?? "";
  if (q.length > 200) throw new Error("search_too_long");
  const clauses: string[] = [];
  const values: string[] = [];
  // Current producers are server-only. Client ingestion is not connected yet.
  if (category === "frontend") clauses.push("1 = 0");
  if (q) {
    const fields = ["id", "error_code", "message_summary", "request_id", "service", "source", "account_id", "run_id", "trace_ref", "correlation_json"];
    clauses.push(`(${fields.map((field) => `instr(lower(COALESCE(${field}, '')), lower(?)) > 0`).join(" OR ")})`);
    values.push(...fields.map(() => q));
  }
  return { clauses, values };
}
