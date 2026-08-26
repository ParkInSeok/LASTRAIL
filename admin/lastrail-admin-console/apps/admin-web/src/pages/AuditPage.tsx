import { FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";

type AuditFilters = {
  action: string;
  resourceId: string;
  result: "" | "SUCCESS" | "FAILURE";
};

function compactSummary(value: unknown): string {
  if (value === null || typeof value !== "object") return String(value);
  return JSON.stringify(value);
}

export function AuditPage() {
  const [draft, setDraft] = useState<AuditFilters>({
    action: "",
    resourceId: "",
    result: ""
  });
  const [filters, setFilters] = useState<AuditFilters>(draft);
  const query = useQuery({
    queryKey: ["audit", filters],
    queryFn: () =>
      adminApi.auditLogs({
        action: filters.action.trim() || undefined,
        resourceId: filters.resourceId.trim() || undefined,
        result: filters.result || undefined
      })
  });

  function submitFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFilters(draft);
  }

  return (
    <main className="content">
      <header>
        <h1>Audit</h1>
        <p>Admin actions and operational history</p>
      </header>
      <form className="toolbar" onSubmit={submitFilters}>
        <input
          aria-label="Action"
          placeholder="Action"
          value={draft.action}
          onChange={(event) => setDraft({ ...draft, action: event.target.value })}
        />
        <input
          aria-label="Resource ID"
          placeholder="Resource ID"
          value={draft.resourceId}
          onChange={(event) => setDraft({ ...draft, resourceId: event.target.value })}
        />
        <select
          aria-label="Result"
          value={draft.result}
          onChange={(event) =>
            setDraft({ ...draft, result: event.target.value as AuditFilters["result"] })
          }
        >
          <option value="">Any result</option>
          <option value="SUCCESS">Success</option>
          <option value="FAILURE">Failure</option>
        </select>
        <button type="submit">Apply</button>
      </form>
      {query.isLoading ? <p>Loading audit logs</p> : null}
      {query.error ? <p className="error">Audit request failed</p> : null}
      {query.data?.items.length === 0 ? <p>No audit logs found</p> : null}
      {query.data && query.data.items.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>Time</th>
              <th>Actor</th>
              <th>Action</th>
              <th>Resource</th>
              <th>Result</th>
              <th>Summary</th>
            </tr>
          </thead>
          <tbody>
            {query.data.items.map((item) => (
              <tr key={item.id}>
                <td>{new Date(item.createdAt).toLocaleString()}</td>
                <td>
                  {item.principalDisplayName}
                  <small>{item.role}</small>
                </td>
                <td>
                  {item.action}
                  <small>{item.permission}</small>
                </td>
                <td>
                  {item.resourceType}
                  <small>{item.resourceId ?? "-"}</small>
                </td>
                <td>{item.result}</td>
                <td className="audit-summary">{compactSummary(item.changeSummary)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </main>
  );
}
