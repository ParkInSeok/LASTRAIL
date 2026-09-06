export function opsLogSearch(category: string | undefined, search: string | undefined, serverVersion?: string) {
  if (category && category !== "server" && category !== "frontend") throw new Error("invalid_log_category");
  const q = search?.trim() ?? "";
  if (q.length > 200) throw new Error("search_too_long");
  const clauses: string[] = [];
  const values: string[] = [];
  const version = serverVersion?.trim();
  if (version) {
    if (!/^[A-Za-z0-9._-]{1,100}$/.test(version)) throw new Error("invalid_server_version");
    clauses.push("json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.serverVersion') = ?");
    values.push(version);
  }
  if (category === "frontend") {
    clauses.push("json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.clientDiagnosticCategory') = 'frontend'");
  }
  if (q) {
    const fields = ["id", "error_code", "message_summary", "request_id", "service", "source", "account_id", "run_id", "trace_ref", "correlation_json"];
    clauses.push(`(${fields.map((field) => `instr(lower(COALESCE(${field}, '')), lower(?)) > 0`).join(" OR ")})`);
    values.push(...fields.map(() => q));
  }
  return { clauses, values };
}
