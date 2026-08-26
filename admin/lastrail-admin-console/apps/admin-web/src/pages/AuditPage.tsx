import { FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import type { AuditLogsResponse } from "@lastrail/admin-contracts";
import { adminApi } from "../shared/api/client";

type AuditFilters = {
  action: string;
  resourceId: string;
  result: "" | "SUCCESS" | "FAILURE";
};

type AuditLogItem = AuditLogsResponse["items"][number];

export function compactAuditSummary(value: unknown): string {
  if (value === null || typeof value !== "object") return String(value);
  return JSON.stringify(value);
}

export function auditResourceHref(resourceType: string, resourceId: string | null): string | null {
  if (!resourceId) return null;
  if (resourceType === "account") return `/players/${encodeURIComponent(resourceId)}`;
  if (resourceType === "run") return `/runs/${encodeURIComponent(resourceId)}`;
  return null;
}

function auditResultFromSearchParam(value: string | null): AuditFilters["result"] {
  if (value === "SUCCESS" || value === "FAILURE") return value;
  return "";
}

export function auditFiltersFromSearchParams(searchParams: URLSearchParams): AuditFilters {
  return {
    action: searchParams.get("action")?.trim() ?? "",
    resourceId: searchParams.get("resourceId")?.trim() ?? "",
    result: auditResultFromSearchParam(searchParams.get("result"))
  };
}

export function auditSearchParamsFromFilters(filters: AuditFilters): URLSearchParams {
  const searchParams = new URLSearchParams();
  const action = filters.action.trim();
  const resourceId = filters.resourceId.trim();
  if (action) searchParams.set("action", action);
  if (resourceId) searchParams.set("resourceId", resourceId);
  if (filters.result) searchParams.set("result", filters.result);
  return searchParams;
}

export function AuditPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const currentFilters = auditFiltersFromSearchParams(searchParams);
  const [draft, setDraft] = useState<AuditFilters>(currentFilters);
  const [filters, setFilters] = useState<AuditFilters>(draft);
  const [selectedAuditLog, setSelectedAuditLog] = useState<AuditLogItem | null>(null);
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
    setSearchParams(auditSearchParamsFromFilters(draft));
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
        <>
          <table>
            <thead>
              <tr>
                <th>Time</th>
                <th>Actor</th>
                <th>Action</th>
                <th>Resource</th>
                <th>Result</th>
                <th>Summary</th>
                <th>Detail</th>
              </tr>
            </thead>
            <tbody>
              {query.data.items.map((item) => {
                const resourceHref = auditResourceHref(item.resourceType, item.resourceId);
                return (
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
                      <small>
                        {resourceHref ? (
                          <Link to={resourceHref}>{item.resourceId}</Link>
                        ) : (
                          item.resourceId ?? "-"
                        )}
                      </small>
                    </td>
                    <td>{item.result}</td>
                    <td className="audit-summary">{compactAuditSummary(item.changeSummary)}</td>
                    <td>
                      <button type="button" onClick={() => setSelectedAuditLog(item)}>
                        View
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {selectedAuditLog ? (
            <aside className="detail-panel" aria-label="Audit detail">
              <header className="detail-panel-header">
                <div>
                  <h2>{selectedAuditLog.action}</h2>
                  <p>{selectedAuditLog.id}</p>
                </div>
                <button type="button" onClick={() => setSelectedAuditLog(null)}>
                  Close
                </button>
              </header>
              <dl className="detail-list">
                <dt>Actor</dt>
                <dd>
                  {selectedAuditLog.principalDisplayName} ({selectedAuditLog.role})
                </dd>
                <dt>Permission</dt>
                <dd>{selectedAuditLog.permission}</dd>
                <dt>Resource</dt>
                <dd>
                  {selectedAuditLog.resourceType} / {selectedAuditLog.resourceId ?? "-"}
                </dd>
                <dt>Request</dt>
                <dd>{selectedAuditLog.requestId}</dd>
                <dt>Confirmation</dt>
                <dd>{selectedAuditLog.confirmationId ?? "-"}</dd>
                <dt>Payload hash</dt>
                <dd>{selectedAuditLog.payloadHash ?? "-"}</dd>
                <dt>Result</dt>
                <dd>
                  {selectedAuditLog.result}
                  {selectedAuditLog.errorCode ? ` (${selectedAuditLog.errorCode})` : ""}
                </dd>
                <dt>Created</dt>
                <dd>{new Date(selectedAuditLog.createdAt).toLocaleString()}</dd>
              </dl>
              <pre>{JSON.stringify(selectedAuditLog.changeSummary, null, 2)}</pre>
            </aside>
          ) : null}
        </>
      ) : null}
    </main>
  );
}
