import { FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import type { AuditLogsResponse } from "@lastrail/admin-contracts";
import { adminApi } from "../shared/api/client";
import {
  auditActionLabel,
  auditResultLabel,
  permissionLabel,
  resourceTypeLabel,
  roleLabel
} from "../shared/labels/adminLabels";

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
        <h1>감사 로그</h1>
        <p>운영자 작업과 운영 이력</p>
      </header>
      <form className="toolbar" onSubmit={submitFilters}>
        <input
          aria-label="작업"
          placeholder="작업"
          value={draft.action}
          onChange={(event) => setDraft({ ...draft, action: event.target.value })}
        />
        <input
          aria-label="대상 ID"
          placeholder="대상 ID"
          value={draft.resourceId}
          onChange={(event) => setDraft({ ...draft, resourceId: event.target.value })}
        />
        <select
          aria-label="결과"
          value={draft.result}
          onChange={(event) =>
            setDraft({ ...draft, result: event.target.value as AuditFilters["result"] })
          }
        >
          <option value="">전체 결과</option>
          <option value="SUCCESS">성공</option>
          <option value="FAILURE">실패</option>
        </select>
        <button type="submit">적용</button>
      </form>
      {query.isLoading ? <p>감사 로그를 불러오는 중</p> : null}
      {query.error ? <p className="error">감사 로그 요청 실패</p> : null}
      {query.data?.items.length === 0 ? <p>감사 로그가 없습니다</p> : null}
      {query.data && query.data.items.length > 0 ? (
        <>
          <table>
            <thead>
              <tr>
                <th>시각</th>
                <th>작업자</th>
                <th>작업</th>
                <th>대상</th>
                <th>결과</th>
                <th>요약</th>
                <th>상세</th>
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
                      <small>{roleLabel(item.role)}</small>
                    </td>
                    <td>
                      {auditActionLabel(item.action)}
                      <small>{permissionLabel(item.permission)}</small>
                    </td>
                    <td>
                      {resourceTypeLabel(item.resourceType)}
                      <small>
                        {resourceHref ? (
                          <Link to={resourceHref}>{item.resourceId}</Link>
                        ) : (
                          item.resourceId ?? "-"
                        )}
                      </small>
                    </td>
                    <td>{auditResultLabel(item.result)}</td>
                    <td className="audit-summary">{compactAuditSummary(item.changeSummary)}</td>
                    <td>
                      <button type="button" onClick={() => setSelectedAuditLog(item)}>
                        보기
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {selectedAuditLog ? (
            <aside className="detail-panel" aria-label="감사 로그 상세">
              <header className="detail-panel-header">
                <div>
                  <h2>{auditActionLabel(selectedAuditLog.action)}</h2>
                  <p>{selectedAuditLog.id}</p>
                </div>
                <button type="button" onClick={() => setSelectedAuditLog(null)}>
                  닫기
                </button>
              </header>
              <dl className="detail-list">
                <dt>작업자</dt>
                <dd>
                  {selectedAuditLog.principalDisplayName} ({roleLabel(selectedAuditLog.role)})
                </dd>
                <dt>권한</dt>
                <dd>{permissionLabel(selectedAuditLog.permission)}</dd>
                <dt>대상</dt>
                <dd>
                  {resourceTypeLabel(selectedAuditLog.resourceType)} / {selectedAuditLog.resourceId ?? "-"}
                </dd>
                <dt>요청</dt>
                <dd>{selectedAuditLog.requestId}</dd>
                <dt>확인</dt>
                <dd>{selectedAuditLog.confirmationId ?? "-"}</dd>
                <dt>페이로드 해시</dt>
                <dd>{selectedAuditLog.payloadHash ?? "-"}</dd>
                <dt>결과</dt>
                <dd>
                  {auditResultLabel(selectedAuditLog.result)}
                  {selectedAuditLog.errorCode ? ` (${selectedAuditLog.errorCode})` : ""}
                </dd>
                <dt>생성 시각</dt>
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
