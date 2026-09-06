export function opsLogSearch(
  category: string | undefined,
  search: string | undefined,
  serverVersion?: string,
  appVersion?: string,
  appVersionCode?: string,
) {
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
  const frontendAppVersion = appVersion?.trim();
  if (frontendAppVersion) {
    if (frontendAppVersion.length > 64) throw new Error("invalid_app_version");
    clauses.push("json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.appVersion') = ?");
    values.push(frontendAppVersion);
  }
  const frontendAppVersionCode = appVersionCode?.trim();
  if (frontendAppVersionCode) {
    if (!/^\d{1,19}$/.test(frontendAppVersionCode)) throw new Error("invalid_app_version_code");
    clauses.push("json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.appVersionCode') = ?");
    values.push(frontendAppVersionCode);
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
